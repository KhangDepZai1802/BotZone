import type { PoolClient } from 'pg';
import type { Actor, InvoiceListItem, InvoiceListResult, InvoiceQuery } from '@/lib/contracts';
import { readSnapshot } from './db';
import { AppError } from './errors';
import { buildInvoicePredicate, normalizeInvoiceQuery, validateUuid } from './invoice-filter';

/** Every aggregate branch is reduced before joining a header: one row per invoice. */
export const INVOICE_FACTS_CTE = `invoice_facts AS (
  SELECT i0.*, COALESCE(lines.item_count,0)::text AS item_count,
    COALESCE(credits.total,0)::text AS credit_note_total_vnd,
    (i0.grand_total_vnd-COALESCE(credits.total,0))::text AS net_vnd,
    COALESCE(receipts.total,0)::text AS received_vnd,
    COALESCE(refunded.total,0)::text AS refunded_vnd,
    CASE WHEN COALESCE(refunded.total,0)>0 AND refunded.total>=COALESCE(receipts.total,0) THEN 'refunded'
         WHEN COALESCE(refunded.total,0)>0 THEN 'partially_refunded'
         WHEN i0.grand_total_vnd=0 OR COALESCE(receipts.total,0)>=i0.grand_total_vnd THEN 'paid'
         WHEN COALESCE(receipts.total,0)>0 THEN 'partially_paid'
         ELSE 'unpaid' END AS payment_status
  FROM invoices i0
  LEFT JOIN LATERAL (SELECT sum(quantity) AS item_count FROM invoice_items WHERE invoice_id=i0.id AND line_type='product') lines ON TRUE
  LEFT JOIN LATERAL (SELECT sum(total_vnd) AS total FROM credit_notes WHERE invoice_id=i0.id AND status='issued') credits ON TRUE
  LEFT JOIN LATERAL (SELECT sum(amount_vnd) AS total FROM payment_receipts WHERE order_id=i0.order_id) receipts ON TRUE
  LEFT JOIN LATERAL (SELECT sum(amount_vnd) AS total FROM refunds WHERE order_id=i0.order_id AND status='completed') refunded ON TRUE
)`;

type Snapshot = Record<string, unknown>;
type InvoiceRow = {
  id: string; invoice_no: string | null; order_id: string; lifecycle: 'draft' | 'issued';
  issued_at: Date | string | null; customer_id: string; responsible_staff_id: string | null;
  issued_by_user_id: string | null; subtotal_vnd: string; discount_total_vnd: string;
  grand_total_vnd: string; customer_snapshot: Snapshot; staff_snapshot: Snapshot;
  item_count: string; credit_note_total_vnd: string; net_vnd: string; payment_status: string;
  received_vnd: string; refunded_vnd: string;
};
const iso = (value: Date | string | null) => value === null ? null : new Date(value).toISOString();
const nameOf = (snapshot: Snapshot, fallback: string) => String(snapshot.name ?? snapshot.displayName ?? snapshot.display_name ?? fallback);

export function mapInvoice(row: InvoiceRow): InvoiceListItem {
  return { id: row.id, invoiceNo: row.invoice_no, orderId: row.order_id, issuedAt: iso(row.issued_at),
    customerName: nameOf(row.customer_snapshot, 'Khách hàng'), staffName: nameOf(row.staff_snapshot, 'Chưa phân công'),
    responsibleStaffId: row.responsible_staff_id, itemCount: Number(row.item_count),
    subtotalVnd: row.subtotal_vnd, discountTotalVnd: row.discount_total_vnd, grandTotalVnd: row.grand_total_vnd,
    creditNoteTotalVnd: row.credit_note_total_vnd, netVnd: row.net_vnd, paymentStatus: row.payment_status };
}

export async function snapshotReadAt(client: PoolClient): Promise<string> {
  const result = await client.query<{read_at: Date}>('SELECT transaction_timestamp() AS read_at');
  return result.rows[0].read_at.toISOString();
}

export async function readInvoiceList(client: PoolClient, query: InvoiceQuery, actor: Actor, options: {all?: boolean; invoiceId?: string; reportScope?: boolean; maxRows?: number} = {}): Promise<InvoiceListResult> {
  const predicate = buildInvoicePredicate(query, actor, { allowCustomer: Boolean(options.invoiceId), report: options.reportScope, lifecycle: options.invoiceId ? 'any' : undefined });
  const values = [...predicate.values];
  let where = predicate.text;
  if (options.invoiceId) { values.push(validateUuid(options.invoiceId)); where += ` AND i.id=$${values.length}::uuid`; }
  const totals = await client.query<{count: string; total: string}>(`WITH ${INVOICE_FACTS_CTE} SELECT count(*)::text AS count,COALESCE(sum(i.grand_total_vnd),0)::text AS total FROM invoice_facts i WHERE ${where}`, values);
  const matchedCount = Number(totals.rows[0].count);
  if (options.all && matchedCount > (options.maxRows ?? 10000)) throw new AppError('EXPORT_LIMIT', 'Tập kết quả vượt giới hạn 10.000 hóa đơn. Hãy thu hẹp điều kiện.');
  let pagination = '';
  if (!options.all) {
    values.push(predicate.query.pageSize, (predicate.query.page - 1) * predicate.query.pageSize);
    pagination = ` LIMIT $${values.length - 1} OFFSET $${values.length}`;
  }
  const rows = await client.query<InvoiceRow>(`WITH ${INVOICE_FACTS_CTE} SELECT i.* FROM invoice_facts i WHERE ${where} ORDER BY ${predicate.orderBy}${pagination}`, values);
  const items: InvoiceListItem[] = rows.rows.map(mapInvoice);
  return { items, matchedCount, matchedTotalVnd: totals.rows[0].total, readAt: await snapshotReadAt(client), page: predicate.query.page, pageSize: predicate.query.pageSize };
}

export async function listInvoices(query: InvoiceQuery, actor: Actor): Promise<InvoiceListResult> {
  return readSnapshot(client => readInvoiceList(client, query, actor));
}

export type InvoiceDetailItem = {
  id: string; invoiceId: string; orderItemId: string; lineNo: number; lineType: string;
  productId: string | null; variantId: string | null; parentInvoiceItemId: string | null;
  sku: string; name: string; variant: Snapshot; quantity: number; unitPriceVnd: string;
  voucherDiscountVnd: string; pointsDiscountVnd: string; netAmountVnd: string;
  serials: string[];
};
type ItemRow = { id: string; invoice_id: string; order_item_id: string; line_no: number; line_type: string;
  product_id: string | null; variant_id: string | null; parent_invoice_item_id: string | null;
  sku_snapshot: string; name_snapshot: string; variant_snapshot: Snapshot; quantity: number;
  unit_price_vnd: string; voucher_discount_vnd: string; points_discount_vnd: string; net_amount_vnd: string; serials: string[] };

export async function readInvoiceItems(client: PoolClient, ids: string[]): Promise<InvoiceDetailItem[]> {
  if (!ids.length) return [];
  const result = await client.query<ItemRow>(`SELECT ii.*,
    COALESCE((SELECT array_agg(DISTINCT identifiers.normalized_value ORDER BY identifiers.normalized_value)
      FROM inventory_allocations allocation JOIN inventory_identifiers identifiers ON identifiers.inventory_unit_id=allocation.inventory_unit_id
      WHERE allocation.order_item_id=ii.order_item_id AND allocation.state IN ('shipped','returned','replaced')),ARRAY[]::text[]) AS serials
    FROM invoice_items ii WHERE ii.invoice_id=ANY($1::uuid[]) ORDER BY ii.invoice_id,ii.line_no,ii.id`, [ids]);
  return result.rows.map(row => ({ id: row.id, invoiceId: row.invoice_id, orderItemId: row.order_item_id,
    lineNo: row.line_no, lineType: row.line_type, productId: row.product_id, variantId: row.variant_id,
    parentInvoiceItemId: row.parent_invoice_item_id, sku: row.sku_snapshot, name: row.name_snapshot,
    variant: row.variant_snapshot, quantity: row.quantity, unitPriceVnd: row.unit_price_vnd,
    voucherDiscountVnd: row.voucher_discount_vnd, pointsDiscountVnd: row.points_discount_vnd,
    netAmountVnd: row.net_amount_vnd, serials: row.serials }));
}

export type InvoiceDetail = {
  invoice: InvoiceListItem & { lifecycle: string; customerId: string; issuedByUserId: string | null; customerSnapshot: Snapshot; staffSnapshot: Snapshot; orderNo: string; addressSnapshot: Snapshot; receivedVnd: string; refundedVnd: string; exchangeRateVnd: string };
  items: InvoiceDetailItem[]; receipts: Snapshot[]; creditNotes: Snapshot[]; refunds: Snapshot[];
  units: Snapshot[]; attempts: Snapshot[]; promotions: Snapshot[]; readAt: string;
};
export async function readInvoiceDetail(client: PoolClient, id: string, actor: Actor): Promise<InvoiceDetail> {
  validateUuid(id);
  const predicate = buildInvoicePredicate(normalizeInvoiceQuery({}), actor, { allowCustomer: true, lifecycle: actor.role === 'customer' ? 'issued' : 'any' });
  const values = [...predicate.values, id];
  const result = await client.query<InvoiceRow & {order_no: string; address_snapshot: Snapshot; exchange_rate_vnd: string}>(`WITH ${INVOICE_FACTS_CTE} SELECT i.*,o.order_no,o.address_snapshot,o.exchange_rate_vnd::text FROM invoice_facts i JOIN orders o ON o.id=i.order_id WHERE ${predicate.text} AND i.id=$${values.length}::uuid`, values);
  const row = result.rows[0];
  if (!row) throw new AppError('NOT_FOUND', 'Không tìm thấy hóa đơn trong phạm vi được phép.', 404);
  const items = await readInvoiceItems(client, [id]);
  const receipts = await client.query(`SELECT r.id,r.tender_kind,r.amount_vnd::text,r.received_at,
    COALESCE((SELECT jsonb_agg(jsonb_build_object('unitId',a.order_item_unit_amount_id,'amountVnd',a.amount_vnd::text)) FROM order_tender_allocations a WHERE a.receipt_id=r.id),'[]'::jsonb) AS allocations
    FROM payment_receipts r WHERE r.order_id=$1 ORDER BY r.received_at,r.id`, [row.order_id]);
  const creditNotes = await client.query(`SELECT c.id,c.credit_note_no,c.status,c.issued_at,c.reason,c.total_vnd::text,c.return_request_id,c.warranty_claim_id,c.shipment_id,
    COALESCE((SELECT jsonb_agg(jsonb_build_object('invoiceItemId',ci.invoice_item_id,'quantity',ci.quantity,'amountVnd',ci.amount_vnd::text)) FROM credit_note_items ci WHERE ci.credit_note_id=c.id),'[]'::jsonb) AS items
    FROM credit_notes c WHERE c.invoice_id=$1 ${actor.role === 'customer' ? "AND c.status='issued'" : ''} ORDER BY c.issued_at,c.id`, [id]);
  const refunds = await client.query(`SELECT r.id,r.credit_note_id,r.status,r.amount_vnd::text,r.completed_at,
    COALESCE((SELECT jsonb_agg(jsonb_build_object('unitId',a.order_item_unit_amount_id,'receiptId',a.payment_receipt_id,'tenderKind',p.tender_kind,'amountVnd',a.amount_vnd::text)) FROM refund_allocations a JOIN payment_receipts p ON p.id=a.payment_receipt_id WHERE a.refund_id=r.id),'[]'::jsonb) AS allocations
    FROM refunds r WHERE r.order_id=$1 ORDER BY r.completed_at,r.id`, [row.order_id]);
  const units = await client.query(`SELECT u.id,u.order_item_id,u.unit_ordinal,u.parent_unit_amount_id,u.gross_vnd::text,u.voucher_discount_vnd::text,u.redeemed_points,u.points_discount_vnd::text,u.net_amount_vnd::text,u.credit_planned_vnd::text,u.cash_planned_vnd::text FROM order_item_unit_amounts u JOIN order_items oi ON oi.id=u.order_item_id WHERE oi.order_id=$1 ORDER BY oi.line_no,u.unit_ordinal`, [row.order_id]);
  const attempts = await client.query(`SELECT id,method,status,requested_amount_vnd::text,created_at FROM payment_attempts WHERE order_id=$1 ORDER BY created_at,id`, [row.order_id]);
  const promotions = await client.query(`SELECT p.code_normalized,v.version_no,r.status,r.amount_vnd::text FROM promotion_redemptions r JOIN promotion_versions v ON v.id=r.promotion_version_id JOIN promotions p ON p.id=r.promotion_id WHERE r.order_id=$1`, [row.order_id]);
  return { invoice: { ...mapInvoice(row), lifecycle: row.lifecycle, customerId: row.customer_id, issuedByUserId: row.issued_by_user_id,
    customerSnapshot: row.customer_snapshot, staffSnapshot: row.staff_snapshot, orderNo: row.order_no,
    addressSnapshot: row.address_snapshot, receivedVnd: row.received_vnd, refundedVnd: row.refunded_vnd, exchangeRateVnd: row.exchange_rate_vnd },
    items, receipts: receipts.rows, creditNotes: creditNotes.rows, refunds: refunds.rows,
    units: units.rows, attempts: attempts.rows, promotions: promotions.rows, readAt: await snapshotReadAt(client) };
}
export async function getInvoiceDetail(id: string, actor: Actor): Promise<InvoiceDetail> {
  return readSnapshot(client => readInvoiceDetail(client, id, actor));
}

export type InvoiceGrouping = { groupBy: 'period' | 'staff' | 'product'; granularity: 'month' | 'quarter' | 'year'; metric: 'issued' | 'adjustments' | 'net' | 'receipts' | 'refunds'; dimensionId?: string; productLevel?: 'model' | 'sku' };
export type InvoiceStatisticRow = { period: string; dimensionId: string | null; dimensionName: string; invoiceCount: number; quantity: number; totalVnd: string; cashDemoVnd: string; storeCreditVnd: string };
export type InvoiceStatistics = { rows: InvoiceStatisticRow[]; totalVnd: string; readAt: string; groupBy: InvoiceGrouping['groupBy']; granularity: InvoiceGrouping['granularity']; metric: InvoiceGrouping['metric']; fromInclusive: string; toExclusive: string; invoiceIds: string[] };

export function normalizeGrouping(raw: unknown): InvoiceGrouping {
  if (!raw || typeof raw !== 'object') throw new AppError('VALIDATION_ERROR', 'Cấu hình thống kê không hợp lệ.');
  const value = raw as Record<string, unknown>;
  const groupBy = value.groupBy ?? 'period'; const granularity = value.granularity ?? 'month'; const metric = value.metric ?? 'issued';
  if (!['period', 'staff', 'product'].includes(String(groupBy)) || !['month', 'quarter', 'year'].includes(String(granularity)) || !['issued', 'adjustments', 'net', 'receipts', 'refunds'].includes(String(metric))) throw new AppError('VALIDATION_ERROR', 'Chiều hoặc chỉ tiêu thống kê không được hỗ trợ.');
  if (value.productLevel !== undefined && value.productLevel !== 'model' && value.productLevel !== 'sku') throw new AppError('VALIDATION_ERROR', 'Chọn thống kê Model hoặc SKU.');
  if (value.dimensionId !== undefined && (typeof value.dimensionId !== 'string' || value.dimensionId.length > 300)) throw new AppError('VALIDATION_ERROR', 'Đối tượng thống kê không hợp lệ.');
  if (value.dimensionId !== undefined && groupBy === 'product') validateUuid(value.dimensionId);
  return { groupBy, granularity, metric, dimensionId: value.dimensionId, productLevel: value.productLevel ?? 'model' } as InvoiceGrouping;
}

export async function readInvoiceStatistics(client: PoolClient, rawQuery: InvoiceQuery, rawGrouping: InvoiceGrouping, actor: Actor): Promise<InvoiceStatistics> {
  const query = normalizeInvoiceQuery(rawQuery);
  const grouping = normalizeGrouping(rawGrouping);
  const predicate = buildInvoicePredicate(query, actor, { report: true, includeWindow: false, lifecycle: 'issued' });
  const values = [...predicate.values];
  const param = (value: string) => { values.push(value); return `$${values.length}`; };
  const fromInclusive = query.fromInclusive ?? '2023-12-31T17:00:00.000Z';
  const toExclusive = query.toExclusive ?? '2026-12-31T17:00:00.000Z';
  if (new Date(toExclusive).getTime() <= new Date(fromInclusive).getTime() || new Date(toExclusive).getTime() - new Date(fromInclusive).getTime() > 366 * 86400000 * 100) throw new AppError('VALIDATION_ERROR', 'Khoảng báo cáo phải lớn hơn 0 và tối đa 100 năm.');
  const from = param(fromInclusive); const to = param(toExclusive);
  const isProduct = grouping.groupBy === 'product';
  const productColumn = grouping.productLevel === 'sku' ? 'ii.variant_id' : 'ii.product_id';
  const dimId = grouping.groupBy === 'period' ? "''::text" : grouping.groupBy === 'staff' ? 'i.responsible_staff_id' : `${productColumn}::text`;
  const dimName = grouping.groupBy === 'period' ? "'Tất cả hóa đơn'::text" : grouping.groupBy === 'staff' ? "COALESCE(i.staff_snapshot->>'name',i.responsible_staff_id)" : grouping.productLevel === 'sku' ? "ii.name_snapshot || ' · ' || ii.sku_snapshot" : 'ii.name_snapshot';
  const productJoin = isProduct ? " JOIN invoice_items ii ON ii.invoice_id=i.id AND ii.line_type='product'" : '';
  const issued = `SELECT i.id AS invoice_id,i.issued_at AS event_at,${dimId} AS dim_id,${dimName} AS dim_name,${isProduct ? 'ii.net_amount_vnd' : 'i.grand_total_vnd'}::bigint AS amount,${isProduct ? 'ii.quantity' : '0'}::bigint AS quantity,NULL::text AS tender,'issued'::text AS kind FROM filtered i${productJoin}`;
  const adjustments = `SELECT i.id,c.issued_at,${dimId},${dimName},${isProduct ? 'ci.amount_vnd' : 'c.total_vnd'}::bigint,${isProduct ? 'COALESCE(ci.quantity,0)' : '0'}::bigint,NULL::text,'adjustments'::text FROM filtered i JOIN credit_notes c ON c.invoice_id=i.id AND c.status='issued'${isProduct ? " JOIN credit_note_items ci ON ci.credit_note_id=c.id JOIN invoice_items ii ON ii.id=ci.invoice_item_id AND ii.line_type='product'" : ''}`;
  const receipts = `SELECT i.id,r.received_at,${dimId},${dimName},${isProduct ? 'allocation.amount_vnd' : 'r.amount_vnd'}::bigint,0::bigint,r.tender_kind,'receipts'::text FROM filtered i JOIN payment_receipts r ON r.order_id=i.order_id${isProduct ? " JOIN order_tender_allocations allocation ON allocation.receipt_id=r.id JOIN order_item_unit_amounts u ON u.id=allocation.order_item_unit_amount_id JOIN invoice_items ii ON ii.order_item_id=u.order_item_id AND ii.line_type='product'" : ''}`;
  // Refunds are aggregated by allocation, keeping the original tender source.
  const refunds = `SELECT i.id,r.completed_at,${dimId},${dimName},allocation.amount_vnd::bigint,0::bigint,receipt.tender_kind,'refunds'::text FROM filtered i JOIN refunds r ON r.order_id=i.order_id AND r.status='completed' JOIN refund_allocations allocation ON allocation.refund_id=r.id JOIN payment_receipts receipt ON receipt.id=allocation.payment_receipt_id${isProduct ? " JOIN order_item_unit_amounts u ON u.id=allocation.order_item_unit_amount_id JOIN invoice_items ii ON ii.order_item_id=u.order_item_id AND ii.line_type='product'" : ''}`;
  const events = grouping.metric === 'net' ? `${issued} UNION ALL ${adjustments}` : ({ issued, adjustments, receipts, refunds })[grouping.metric];
  const dimensionFilter = grouping.dimensionId ? ` AND dim_id=${param(grouping.dimensionId)}` : '';
  // Granularity is validated above and never supplied as arbitrary SQL.
  const granularity = grouping.granularity;
  const interval = granularity === 'quarter' ? '3 months' : granularity === 'year' ? '1 year' : '1 month';
  const signed = grouping.metric === 'net' ? "CASE WHEN kind='adjustments' THEN -amount ELSE amount END" : 'amount';
  const quantity = grouping.metric === 'net' ? "CASE WHEN kind='adjustments' THEN -quantity ELSE quantity END" : 'quantity';
  let dimensions = grouping.groupBy === 'period' ? "SELECT ''::text AS dim_id,'Tất cả hóa đơn'::text AS dim_name" : 'SELECT dim_id,(array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] AS dim_name FROM window_events GROUP BY dim_id';
  if (grouping.dimensionId) {
    const selected = param(grouping.dimensionId);
    if (grouping.groupBy === 'staff') dimensions = `SELECT ${selected}::text AS dim_id,COALESCE((SELECT (array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] FROM window_events),(SELECT name FROM "user" WHERE id=${selected}),${selected}) AS dim_name`;
    if (isProduct) dimensions = grouping.productLevel === 'sku'
      ? `SELECT ${selected}::text AS dim_id,COALESCE((SELECT (array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] FROM window_events),(SELECT sku FROM product_variants WHERE id=${selected}::uuid),${selected}) AS dim_name`
      : `SELECT ${selected}::text AS dim_id,COALESCE((SELECT (array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] FROM window_events),(SELECT name FROM product_translations WHERE product_id=${selected}::uuid AND locale='vi'),${selected}) AS dim_name`;
    if (actor.role === 'order_staff' && grouping.groupBy === 'staff' && grouping.dimensionId !== actor.id) throw new AppError('FORBIDDEN', 'Chỉ được xem thống kê của nhân viên đang đăng nhập.', 403);
  }
  const sql = `WITH ${INVOICE_FACTS_CTE}, filtered AS (SELECT i.* FROM invoice_facts i WHERE ${predicate.text}),
    events AS (${events}), window_events AS (SELECT * FROM events WHERE event_at>=${from}::timestamptz AND event_at<${to}::timestamptz${dimensionFilter}),
    periods AS (SELECT generate_series(date_trunc('${granularity}',${from}::timestamptz AT TIME ZONE 'Asia/Ho_Chi_Minh'),date_trunc('${granularity}',(${to}::timestamptz AT TIME ZONE 'Asia/Ho_Chi_Minh')-interval '1 microsecond'),interval '${interval}') AS period),
    dimensions AS (${dimensions}), facts AS (
      SELECT date_trunc('${granularity}',event_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AS period,dim_id,
        count(DISTINCT invoice_id)::integer AS invoice_count,sum(${quantity}) AS quantity,sum(${signed}) AS total,
        COALESCE(sum(amount) FILTER(WHERE tender='cash_demo'),0) AS cash_total,
        COALESCE(sum(amount) FILTER(WHERE tender='store_credit'),0) AS credit_total FROM window_events GROUP BY 1,2
    ) SELECT to_char(p.period,'YYYY-MM-DD') AS period,d.dim_id,d.dim_name,
      COALESCE(f.invoice_count,0) AS invoice_count,COALESCE(f.quantity,0)::text AS quantity,COALESCE(f.total,0)::text AS total,
      COALESCE(f.cash_total,0)::text AS cash_total,COALESCE(f.credit_total,0)::text AS credit_total,
      (SELECT COALESCE(array_agg(DISTINCT invoice_id),ARRAY[]::uuid[]) FROM window_events) AS invoice_ids
    FROM periods p CROSS JOIN dimensions d LEFT JOIN facts f ON f.period=p.period AND f.dim_id=d.dim_id ORDER BY p.period,d.dim_id`;
  const result = await client.query<{period: string; dim_id: string; dim_name: string; invoice_count: number; quantity: string; total: string; cash_total: string; credit_total: string; invoice_ids: string[]}>(sql, values);
  const rows: InvoiceStatisticRow[] = result.rows.map(row => ({ period: row.period, dimensionId: row.dim_id || null, dimensionName: row.dim_name,
    invoiceCount: row.invoice_count, quantity: Number(row.quantity), totalVnd: row.total, cashDemoVnd: row.cash_total, storeCreditVnd: row.credit_total }));
  return { rows, totalVnd: rows.reduce((sum, row) => sum + BigInt(row.totalVnd), 0n).toString(), readAt: await snapshotReadAt(client),
    groupBy: grouping.groupBy, granularity, metric: grouping.metric, fromInclusive, toExclusive, invoiceIds: result.rows[0]?.invoice_ids ?? [] };
}

export async function getInvoiceStatistics(query: InvoiceQuery, grouping: InvoiceGrouping, actor: Actor): Promise<InvoiceStatistics> {
  return readSnapshot(client => readInvoiceStatistics(client, query, grouping, actor));
}
