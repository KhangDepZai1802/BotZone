import type { Actor, FilterNode, InvoiceQuery, InvoiceSortField } from '@/lib/contracts';
import { AppError } from './errors';

const comparisonOperators = new Set(['>', '<', '=', '<>', '>=', '<=']);
const fields = {
  invoiceNo: 'i.invoice_no', issuedAt: 'i.issued_at',
  grandTotalVnd: 'i.grand_total_vnd', discountTotalVnd: 'i.discount_total_vnd',
  responsibleStaffId: 'i.responsible_staff_id', customerId: 'i.customer_id',
  lifecycle: 'i.lifecycle', paymentStatus: 'i.payment_status',
} as const;
const nullableFields = new Set(['issuedAt', 'invoiceNo', 'responsibleStaffId']);
const sortFields: Record<InvoiceSortField, string> = {
  issuedAt: 'i.issued_at', invoiceNo: 'i.invoice_no', grandTotalVnd: 'i.grand_total_vnd',
};
const dateOnly = /^\d{4}-\d{2}-\d{2}$/;
const timestamp = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,6})?)?(?:Z|[+-]\d{2}:\d{2})$/;
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const invalid = (message: string): never => { throw new AppError('VALIDATION_ERROR', message); };
function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return invalid('Bộ lọc phải là đối tượng hợp lệ.');
  return value as Record<string, unknown>;
}
export function validateUuid(value: unknown): string {
  if (typeof value !== 'string' || !uuid.test(value)) return invalid('Mã bản ghi không hợp lệ.');
  return value;
}
/** Calendar dates always mean Vietnam dates; timestamps must carry a zone. */
export function normalizeInstant(value: unknown, label = 'Ngày'): string {
  if (typeof value !== 'string' || (!dateOnly.test(value) && !timestamp.test(value))) return invalid(`${label} cần ngày YYYY-MM-DD hoặc timestamp có múi giờ.`);
  const day = value.slice(0, 10);
  const calendar = new Date(`${day}T00:00:00Z`);
  if (!Number.isFinite(calendar.getTime()) || calendar.toISOString().slice(0, 10) !== day) return invalid(`${label} không hợp lệ.`);
  const parsed = new Date(dateOnly.test(value) ? `${value}T00:00:00+07:00` : value);
  if (!Number.isFinite(parsed.getTime())) return invalid(`${label} không hợp lệ.`);
  return parsed.toISOString();
}

function normalizeFilter(raw: unknown, depth: number, budget: { leaves: number; nodes: number }): FilterNode {
  if (depth > 4 || ++budget.nodes > 90) return invalid('Bộ lọc tối đa bốn cấp và ba mươi điều kiện.');
  const node = object(raw);
  if (node.kind === 'group') {
    if ((node.logic !== 'AND' && node.logic !== 'OR') || !Array.isArray(node.children) || !node.children.length || node.children.length > 30) return invalid('Nhóm điều kiện cần AND/OR và ít nhất một điều kiện.');
    return { kind: 'group', logic: node.logic, children: node.children.map(child => normalizeFilter(child, depth + 1, budget)) };
  }
  if (node.kind === 'not') return { kind: 'not', child: normalizeFilter(node.child, depth + 1, budget) };
  if (++budget.leaves > 30) return invalid('Bộ lọc tối đa ba mươi điều kiện.');
  if (node.kind === 'product') {
    if (node.mode !== 'contains' && node.mode !== 'not_contains') return invalid('Điều kiện sản phẩm không hợp lệ.');
    return { kind: 'product', productId: validateUuid(node.productId), mode: node.mode };
  }
  if (node.kind === 'null') {
    if (typeof node.field !== 'string' || !nullableFields.has(node.field) || typeof node.isNull !== 'boolean') return invalid('Trường trống không hợp lệ.');
    return { kind: 'null', field: node.field as 'issuedAt' | 'invoiceNo' | 'responsibleStaffId', isNull: node.isNull };
  }
  if (node.kind !== 'rule' || typeof node.field !== 'string' || !Object.hasOwn(fields, node.field) || typeof node.op !== 'string' || !comparisonOperators.has(node.op)) return invalid('Trường hoặc toán tử không được hỗ trợ.');
  const field = node.field as keyof typeof fields;
  let value = node.value;
  if (field === 'grandTotalVnd' || field === 'discountTotalVnd') {
    if (typeof value === 'number') {
      if (!Number.isSafeInteger(value)) return invalid('Tiền lớn phải gửi dưới dạng chuỗi số nguyên VND.');
      value = String(value);
    }
    if (typeof value !== 'string' || !/^-?\d{1,19}$/.test(value) || BigInt(value) < -9223372036854775808n || BigInt(value) > 9223372036854775807n) return invalid('Số tiền VND không hợp lệ.');
    value = BigInt(value).toString();
  } else if (field === 'issuedAt') {
    normalizeInstant(value);
  } else {
    if (typeof value !== 'string' || value.length > 300) return invalid('Giá trị điều kiện phải là văn bản, tối đa 300 ký tự.');
    if (field === 'lifecycle' && !['draft', 'issued'].includes(value)) return invalid('Trạng thái hóa đơn không hợp lệ.');
    if (field === 'paymentStatus' && !['unpaid', 'partially_paid', 'paid', 'partially_refunded', 'refunded'].includes(value)) return invalid('Trạng thái thanh toán không hợp lệ.');
  }
  return { kind: 'rule', field, op: node.op as Extract<FilterNode, {kind: 'rule'}>['op'], value: value as string };
}

export function normalizeInvoiceQuery(raw: unknown = {}): InvoiceQuery {
  const input = object(raw);
  const page = input.page ?? 1;
  const pageSize = input.pageSize ?? 20;
  if (!Number.isSafeInteger(page) || Number(page) < 1 || Number(page) > 1000000 || !Number.isSafeInteger(pageSize) || Number(pageSize) < 1 || Number(pageSize) > 100) return invalid('Trang phải từ 1; số dòng mỗi trang từ 1 đến 100.');
  const rawSort = input.sort ?? [{ field: 'issuedAt', direction: 'desc' }];
  if (!Array.isArray(rawSort) || rawSort.length > 3) return invalid('Thứ tự sắp xếp không hợp lệ.');
  const sort = rawSort.map(rawEntry => {
    const entry = object(rawEntry);
    if (typeof entry.field !== 'string' || !Object.hasOwn(sortFields, entry.field) || (entry.direction !== 'asc' && entry.direction !== 'desc')) return invalid('Trường sắp xếp không được hỗ trợ.');
    return { field: entry.field as InvoiceSortField, direction: entry.direction as 'asc' | 'desc' };
  });
  const fromInclusive = input.fromInclusive === undefined ? undefined : normalizeInstant(input.fromInclusive, 'Từ ngày');
  const toExclusive = input.toExclusive === undefined ? undefined : normalizeInstant(input.toExclusive, 'Đến ngày');
  if (fromInclusive && toExclusive && fromInclusive >= toExclusive) return invalid('Ngày kết thúc phải sau ngày bắt đầu.');
  if (input.lifecycle !== undefined && input.lifecycle !== 'draft' && input.lifecycle !== 'issued') return invalid('Trạng thái hóa đơn không hợp lệ.');
  return { page: Number(page), pageSize: Number(pageSize), sort,
    lifecycle: input.lifecycle as InvoiceQuery['lifecycle'], fromInclusive, toExclusive,
    filter: input.filter === undefined ? undefined : normalizeFilter(input.filter, 1, { leaves: 0, nodes: 0 }),
  };
}

export type PredicateOptions = { includeWindow?: boolean; report?: boolean; allowCustomer?: boolean; lifecycle?: 'draft' | 'issued' | 'any' };
/** Scope is always an outer AND and is generated only from the authenticated actor. */
export function buildInvoicePredicate(query: InvoiceQuery, actor: Actor, options: PredicateOptions = {}) {
  const normalized = normalizeInvoiceQuery(query);
  const values: Array<string | number | boolean> = [];
  const param = (value: string | number | boolean) => { values.push(value); return `$${values.length}`; };
  const scope: string[] = [];
  if (actor.role === 'customer' && options.allowCustomer && !options.report) scope.push(`i.customer_id = ${param(actor.id)}`);
  else if (actor.role === 'owner') scope.push('TRUE');
  else if (actor.role === 'order_staff') scope.push(options.report ? `i.responsible_staff_id = ${param(actor.id)}` : 'TRUE');
  else throw new AppError('FORBIDDEN', 'Bạn không có quyền đọc dữ liệu hóa đơn này.', 403);
  const lifecycle = options.lifecycle ?? normalized.lifecycle ?? 'issued';
  if (lifecycle !== 'any') scope.push(`i.lifecycle = ${param(lifecycle)}`);
  if (options.includeWindow !== false) {
    if (normalized.fromInclusive) scope.push(`i.issued_at >= ${param(normalized.fromInclusive)}::timestamptz`);
    if (normalized.toExclusive) scope.push(`i.issued_at < ${param(normalized.toExclusive)}::timestamptz`);
  }
  function compile(node: FilterNode): string {
    if (node.kind === 'group') return `(${node.children.map(compile).join(` ${node.logic} `)})`;
    if (node.kind === 'not') return `(NOT (${compile(node.child)}))`;
    if (node.kind === 'null') return `${fields[node.field]} IS ${node.isNull ? '' : 'NOT '}NULL`;
    if (node.kind === 'product') return `${node.mode === 'not_contains' ? 'NOT ' : ''}EXISTS (SELECT 1 FROM invoice_items filter_item WHERE filter_item.invoice_id = i.id AND filter_item.line_type = 'product' AND filter_item.product_id = ${param(node.productId)}::uuid)`;
    const column = fields[node.field];
    if (node.field === 'issuedAt') {
      const value = String(node.value);
      const start = normalizeInstant(value);
      if (dateOnly.test(value)) {
        const next = new Date(new Date(start).getTime() + 86400000).toISOString();
        if (node.op === '=' || node.op === '<>') {
          const expr = `(${column} >= ${param(start)}::timestamptz AND ${column} < ${param(next)}::timestamptz)`;
          return node.op === '<>' ? `(NOT ${expr})` : expr;
        }
        const op = ({ '>': '>=', '<': '<', '>=': '>=', '<=': '<' } as const)[node.op];
        return `${column} ${op} ${param(node.op === '>' || node.op === '<=' ? next : start)}::timestamptz`;
      }
      return `${column} ${node.op} ${param(start)}::timestamptz`;
    }
    return `${column} ${node.op} ${param(node.value)}${node.field === 'grandTotalVnd' || node.field === 'discountTotalVnd' ? '::bigint' : ''}`;
  }
  if (normalized.filter) scope.push(`(${compile(normalized.filter)})`);
  const ordering = normalized.sort.map(sort => `${sortFields[sort.field]} ${sort.direction.toUpperCase()} NULLS LAST`);
  ordering.push(`i.id ${normalized.sort[0]?.direction === 'asc' ? 'ASC' : 'DESC'}`);
  return { text: scope.map(part => `(${part})`).join(' AND '), values, orderBy: ordering.join(', '), query: normalized };
}
