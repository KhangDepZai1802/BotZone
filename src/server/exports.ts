import { createHash, randomUUID } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import ExcelJS from 'exceljs';
import PDFDocument from 'pdfkit';
import type { Actor, InvoiceListItem, InvoiceQuery } from '@/lib/contracts';
import { pool, readSnapshot } from './db';
import { AppError } from './errors';
import { buildInvoicePredicate, normalizeInvoiceQuery, validateUuid } from './invoice-filter';
import {
  INVOICE_FACTS_CTE, mapInvoice, normalizeGrouping, readInvoiceDetail, readInvoiceItems,
  readInvoiceList, readInvoiceStatistics, snapshotReadAt,
  type InvoiceDetail, type InvoiceDetailItem, type InvoiceGrouping, type InvoiceStatistics,
} from './invoices';

type ExportFormat = 'xlsx' | 'pdf';
type ExportInput = {query?: InvoiceQuery; invoiceId?: string; formats: ExportFormat[]; kind: 'detail' | 'list' | 'report'; groupBy?: InvoiceGrouping['groupBy']; granularity?: InvoiceGrouping['granularity']; metric?: InvoiceGrouping['metric']; dimensionId?: string; productLevel?: 'model' | 'sku'; locale?: 'vi' | 'en'};
type ExportData = {kind: ExportInput['kind']; query: InvoiceQuery; readAt: string; rowCount: number; totalVnd: string; invoices: InvoiceListItem[]; items: InvoiceDetailItem[]; detail?: InvoiceDetail; statistics?: InvoiceStatistics; locale: 'vi' | 'en'};
type FileEntry = {filename: string; bytes: number; checksum: string; contentType: string};
type ExportManifest = {files: Partial<Record<ExportFormat, FileEntry>>; invoiceIds: string[]; datasetChecksum: string; kind: ExportInput['kind']; locale: 'vi' | 'en'};
const contentTypes = {xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', pdf: 'application/pdf'};
const sha = (value: Buffer | string) => createHash('sha256').update(value).digest('hex');
const money = (value: string, locale: 'vi' | 'en' = 'vi') => `${new Intl.NumberFormat(locale === 'vi' ? 'vi-VN' : 'en-US').format(BigInt(value))} VND`;
const time = (value: string | Date | null, locale: 'vi' | 'en' = 'vi') => value === null ? '—' : new Intl.DateTimeFormat(locale === 'vi' ? 'vi-VN' : 'en-GB', {timeZone:'Asia/Ho_Chi_Minh',dateStyle:'short',timeStyle:'short'}).format(new Date(value));
const text = (value: unknown) => value === null || value === undefined ? '' : typeof value === 'object' ? JSON.stringify(value) : String(value);

function validateExportInput(raw: unknown): ExportInput {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) throw new AppError('VALIDATION_ERROR', 'Yêu cầu xuất không hợp lệ.');
  const value = raw as Record<string, unknown>;
  if (!['detail', 'list', 'report'].includes(String(value.kind))) throw new AppError('VALIDATION_ERROR', 'Chọn xuất chi tiết, danh sách hoặc thống kê.');
  if (!Array.isArray(value.formats) || value.formats.length < 1 || value.formats.length > 2 || value.formats.some(format => format !== 'xlsx' && format !== 'pdf')) throw new AppError('VALIDATION_ERROR', 'Định dạng xuất phải là XLSX hoặc PDF.');
  const input = {...value, formats: [...new Set(value.formats)], query: normalizeInvoiceQuery(value.query ?? {})} as ExportInput;
  if (input.locale !== undefined && input.locale !== 'vi' && input.locale !== 'en') throw new AppError('VALIDATION_ERROR', 'Ngôn ngữ không hợp lệ.');
  if (input.kind === 'detail') input.invoiceId = validateUuid(input.invoiceId);
  else if (input.invoiceId !== undefined) throw new AppError('VALIDATION_ERROR', 'Mã hóa đơn chỉ dùng cho xuất chi tiết.');
  if (input.kind === 'report') Object.assign(input, normalizeGrouping(input));
  return input;
}

/** Export snapshot is completed before any font/file rendering begins. */
export async function materializeInvoiceExport(raw: unknown, actor: Actor): Promise<{input: ExportInput; data: ExportData}> {
  const input = validateExportInput(raw);
  if (actor.role === 'customer' && input.kind !== 'detail') throw new AppError('FORBIDDEN', 'Khách hàng chỉ được xuất hóa đơn của mình.', 403);
  if (!['customer', 'owner', 'order_staff'].includes(actor.role)) throw new AppError('FORBIDDEN', 'Bạn không có quyền xuất hóa đơn.', 403);
  const data = await readSnapshot(async client => {
    const query = input.query!;
    const base = {kind: input.kind, query, locale: input.locale ?? 'vi'} as const;
    if (input.kind === 'detail') {
      const detail = await readInvoiceDetail(client, input.invoiceId!, actor);
      return {...base, readAt: detail.readAt, rowCount: 1, totalVnd: detail.invoice.grandTotalVnd, invoices: [detail.invoice], items: detail.items, detail};
    }
    if (input.kind === 'report') {
      const statistics = await readInvoiceStatistics(client, query, normalizeGrouping(input), actor);
      if (statistics.invoiceIds.length > 10000 || statistics.rows.length > 100000) throw new AppError('EXPORT_LIMIT', 'Báo cáo quá lớn. Hãy thu hẹp điều kiện.');
      // The report event window may include adjustments for older invoices. Read its exact IDs,
      // without incorrectly applying the issued-at window a second time.
      const predicate = buildInvoicePredicate(query, actor, {report: true, includeWindow: false, lifecycle: 'issued'});
      const values = [...predicate.values, statistics.invoiceIds];
      const result = await client.query(`WITH ${INVOICE_FACTS_CTE} SELECT i.* FROM invoice_facts i WHERE ${predicate.text} AND i.id=ANY($${values.length}::uuid[]) ORDER BY ${predicate.orderBy}`, values);
      const invoices = result.rows.map(mapInvoice);
      return {...base, readAt: statistics.readAt, rowCount: invoices.length, totalVnd: statistics.totalVnd, invoices,
        items: await readInvoiceItems(client, statistics.invoiceIds), statistics};
    }
    const list = await readInvoiceList(client, query, actor, {all: true});
    return {...base, readAt: await snapshotReadAt(client), rowCount: list.matchedCount, totalVnd: list.matchedTotalVnd,
      invoices: list.items, items: await readInvoiceItems(client, list.items.map(invoice => invoice.id))};
  });
  return {input, data};
}

function excelInteger(value: string): number | string {
  const number = BigInt(value);
  // Excel numeric cells preserve at most 15 significant decimal digits.
  return number >= -999999999999999n && number <= 999999999999999n ? Number(number) : value;
}
function tableSheet(workbook: ExcelJS.Workbook, name: string, headers: string[], widths: number[]) {
  const sheet = workbook.addWorksheet(name, {views:[{state:'frozen',ySplit:1}],pageSetup:{paperSize:9,orientation:'landscape',fitToPage:true,fitToWidth:1,fitToHeight:0}});
  sheet.columns = headers.map((header, index) => ({header, key: String(index), width: widths[index] ?? 20}));
  sheet.getRow(1).font = {name:'Noto Sans',bold:true,color:{argb:'FFFFFFFF'}};
  sheet.getRow(1).fill = {type:'pattern',pattern:'solid',fgColor:{argb:'FF18181B'}};
  sheet.getRow(1).height = 30;
  sheet.autoFilter = {from:{row:1,column:1},to:{row:1,column:headers.length}};
  sheet.properties.defaultRowHeight = 25;
  return sheet;
}
function appendRow(sheet: ExcelJS.Worksheet, cells: Array<string | number>, moneyColumns: number[] = []) {
  const row = sheet.addRow(cells);
  row.alignment = {vertical:'top',wrapText:true};
  row.font = {name:'Noto Sans',size:10};
  cells.forEach((value, index) => {
    const cell = row.getCell(index + 1);
    // Explicit string assignment never constructs an Excel formula object.
    cell.value = value;
    cell.numFmt = typeof value === 'string' ? '@' : moneyColumns.includes(index) ? '#,##0' : '0';
    if (moneyColumns.includes(index) && typeof value === 'string') cell.note = 'Giữ nguyên chuỗi số nguyên: vượt miền số chính xác 15 chữ số của Excel.';
  });
  return row;
}

export async function renderInvoiceXlsx(data: ExportData): Promise<Buffer> {
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'BotZone local'; workbook.created = new Date(data.readAt); workbook.modified = new Date(data.readAt);
  const en = data.locale === 'en';
  const headers = en ? ['Invoice','Issued (Vietnam)','Customer','Staff','Products','Subtotal VND','Discount VND','Invoice total VND','Credit notes VND','Invoice net VND','Payment state','Invoice ID']
    : ['Mã hóa đơn','Phát hành (giờ Việt Nam)','Khách hàng','Nhân viên','Số món','Trước giảm VND','Giảm VND','Tổng hóa đơn VND','Chứng từ giảm VND','Ròng hóa đơn VND','Thanh toán','ID hóa đơn'];
  const invoices = tableSheet(workbook, 'HoaDon', headers, [26,24,28,24,12,22,20,23,23,23,22,40]);
  for (const item of data.invoices) appendRow(invoices, [item.invoiceNo ?? 'DRAFT',time(item.issuedAt,data.locale),item.customerName,item.staffName,item.itemCount,
    excelInteger(item.subtotalVnd),excelInteger(item.discountTotalVnd),excelInteger(item.grandTotalVnd),excelInteger(item.creditNoteTotalVnd),excelInteger(item.netVnd),item.paymentStatus,item.id], [5,6,7,8,9]);
  const totals = data.invoices.reduce((sum, item) => sum + BigInt(item.grandTotalVnd), 0n).toString();
  appendRow(invoices, [en ? 'INVOICE TOTAL' : 'TỔNG HÓA ĐƠN','','','',data.invoices.reduce((count, item) => count + item.itemCount,0),'','',excelInteger(totals),'','','',''], [7]).font = {bold:true};
  const itemHeaders = en ? ['Invoice','Line','Type','SKU','Product / service','Configuration','Quantity','Unit price VND','Voucher VND','Points VND','Net VND','Serial / IMEI','Invoice ID','Order item ID']
    : ['Mã hóa đơn','Dòng','Loại','SKU','Sản phẩm / dịch vụ','Cấu hình','Số lượng','Đơn giá VND','Voucher VND','Điểm VND','Thành tiền VND','Serial / IMEI','ID hóa đơn','ID dòng đơn'];
  const items = tableSheet(workbook, 'ChiTietHoaDon', itemHeaders, [26,10,16,28,36,36,12,22,20,20,22,40,40,40]);
  const invoiceNumbers = new Map(data.invoices.map(invoice => [invoice.id, invoice.invoiceNo ?? 'DRAFT']));
  for (const item of data.items) appendRow(items, [invoiceNumbers.get(item.invoiceId) ?? '',item.lineNo,item.lineType,item.sku,item.name,JSON.stringify(item.variant),item.quantity,
    excelInteger(item.unitPriceVnd),excelInteger(item.voucherDiscountVnd),excelInteger(item.pointsDiscountVnd),excelInteger(item.netAmountVnd),item.serials.join('\n'),item.invoiceId,item.orderItemId], [7,8,9,10]);
  const filter = tableSheet(workbook, 'DieuKienLoc', [en ? 'Property' : 'Thuộc tính',en ? 'Value' : 'Giá trị'], [30,110]);
  const conditions: Array<[string,string | number]> = [['kind',data.kind],['readAt',data.readAt],['timezone','Asia/Ho_Chi_Minh'],['currency','VND'],['invoiceCount',data.rowCount],['reportOrListTotalVnd',excelInteger(data.totalVnd)],['query',JSON.stringify(data.query)],['note',en ? 'Demo sales document. USD is display-only. Original invoices are unchanged by subsequent adjustments.' : 'Chứng từ bán hàng mô phỏng. VND là tiền chuẩn. Chứng từ giảm/hoàn không sửa hóa đơn gốc.'],['excelPrecision',en ? 'Money beyond 15 decimal digits is preserved as text.' : 'Tiền vượt 15 chữ số được giữ dạng text để không mất chính xác.']];
  for (const condition of conditions) appendRow(filter, condition);
  if (data.statistics) {
    appendRow(filter,['grouping',JSON.stringify({groupBy:data.statistics.groupBy,granularity:data.statistics.granularity,metric:data.statistics.metric,from:data.statistics.fromInclusive,to:data.statistics.toExclusive})]);
    const report = tableSheet(workbook,'ThongKe',en ? ['Period','Object ID','Object','Invoices','Quantity','Value VND','Demo cash VND','Store credit VND'] : ['Kỳ','ID đối tượng','Đối tượng','Số hóa đơn','Số lượng','Giá trị VND','Tiền mô phỏng VND','Số dư mua hàng VND'],[18,40,38,15,15,25,25,25]);
    for (const row of data.statistics.rows) appendRow(report,[row.period,row.dimensionId ?? '',row.dimensionName,row.invoiceCount,row.quantity,excelInteger(row.totalVnd),excelInteger(row.cashDemoVnd),excelInteger(row.storeCreditVnd)],[5,6,7]);
    appendRow(report,[en ? 'TOTAL' : 'TỔNG','','','','',excelInteger(data.statistics.totalVnd),'',''],[5]).font = {bold:true};
  }
  if (data.detail) {
    const detail = data.detail;
    const document = tableSheet(workbook,'ThongTinChungTu',en ? ['Property','Value'] : ['Thuộc tính','Giá trị'],[30,110]);
    for (const [key,value] of Object.entries({invoiceNo:detail.invoice.invoiceNo,orderNo:detail.invoice.orderNo,customer:detail.invoice.customerSnapshot,staff:detail.invoice.staffSnapshot,address:detail.invoice.addressSnapshot,issuedBy:detail.invoice.issuedByUserId,exchangeRateVnd:detail.invoice.exchangeRateVnd,promotions:detail.promotions})) appendRow(document,[key,text(value)]);
    const settlements = tableSheet(workbook,'ThuVaHoan',en ? ['Type','ID','Source / status','Amount VND','Date','Allocations'] : ['Loại','ID','Nguồn / trạng thái','Số tiền VND','Thời điểm','Phân bổ'],[20,40,24,24,24,110]);
    for (const receipt of detail.receipts) appendRow(settlements,['receipt',text(receipt.id),text(receipt.tender_kind),excelInteger(text(receipt.amount_vnd)),time(text(receipt.received_at),data.locale),text(receipt.allocations)],[3]);
    for (const refund of detail.refunds) appendRow(settlements,['refund',text(refund.id),text(refund.status),excelInteger(text(refund.amount_vnd)),refund.completed_at ? time(text(refund.completed_at),data.locale) : '',text(refund.allocations)],[3]);
    const adjustments = tableSheet(workbook,'ChungTuGiam',en ? ['Code','Status','Date','Reason','Amount VND','Items'] : ['Mã','Trạng thái','Thời điểm','Lý do','Số tiền VND','Dòng'],[26,20,24,60,24,110]);
    for (const note of detail.creditNotes) appendRow(adjustments,[text(note.credit_note_no),text(note.status),note.issued_at ? time(text(note.issued_at),data.locale) : '',text(note.reason),excelInteger(text(note.total_vnd)),text(note.items)],[4]);
    const units = tableSheet(workbook,'PhanBoDonVi',en ? ['Unit','Order item','Ordinal','Gross VND','Voucher VND','Points','Points VND','Net VND','Planned credit VND','Planned cash VND'] : ['Unit','Dòng đơn','Ordinal','Gốc VND','Voucher VND','Điểm','Điểm VND','Net VND','Credit dự kiến VND','Tiền dự kiến VND'],[40,40,12,24,24,12,24,24,24,24]);
    for (const unit of detail.units) appendRow(units,[text(unit.id),text(unit.order_item_id),Number(unit.unit_ordinal),excelInteger(text(unit.gross_vnd)),excelInteger(text(unit.voucher_discount_vnd)),Number(unit.redeemed_points),excelInteger(text(unit.points_discount_vnd)),excelInteger(text(unit.net_amount_vnd)),excelInteger(text(unit.credit_planned_vnd)),excelInteger(text(unit.cash_planned_vnd))],[3,4,6,7,8,9]);
  }
  return Buffer.from(await workbook.xlsx.writeBuffer());
}

type PdfTableColumn = {title: string; width: number; align?: 'left' | 'right'};
function pdfTable(doc: PDFKit.PDFDocument, columns: PdfTableColumn[], rows: string[][], title: string, font: string) {
  const left = doc.page.margins.left;
  const bottom = () => doc.page.height - doc.page.margins.bottom - 22;
  const header = () => {
    doc.font(font).fontSize(11).fillColor('#18181b').text(title,left,doc.y,{width:doc.page.width-left-doc.page.margins.right}); doc.moveDown(0.45);
    const top = doc.y; let x = left;
    const height = Math.max(28,...columns.map(column => doc.fontSize(8).heightOfString(column.title,{width:column.width-10})+14));
    doc.rect(left,top,columns.reduce((sum,column) => sum+column.width,0),height).fill('#ededed');
    columns.forEach(column => {doc.fillColor('#18181b').fontSize(8).text(column.title,x+5,top+6,{width:column.width-10,align:column.align});x+=column.width;});
    doc.y=top+height; doc.x=left;
  };
  if (doc.y + 85 > bottom()) doc.addPage();
  header();
  for (const cells of rows) {
    doc.font(font).fontSize(8);
    const rowHeight = Math.max(28,...cells.map((cell,index) => doc.heightOfString(cell || '—',{width:columns[index].width-10})+14));
    if (doc.y + rowHeight > bottom()) {doc.addPage();header();}
    // Oversize text is rendered in a flowing record to avoid clipping a cell.
    if (rowHeight > bottom()-doc.y) {
      for (let index=0;index<cells.length;index++) doc.fontSize(9).text(`${columns[index].title}: ${cells[index] || '—'}`,left,doc.y,{width:columns.reduce((sum,column) => sum+column.width,0)}).moveDown(0.3);
      doc.moveDown(0.5); header(); continue;
    }
    const top=doc.y; let x=left;
    cells.forEach((cell,index) => {doc.fontSize(8).fillColor('#202024').text(cell || '—',x+5,top+7,{width:columns[index].width-10,align:columns[index].align});x+=columns[index].width;});
    doc.moveTo(left,top+rowHeight).lineTo(x,top+rowHeight).strokeColor('#dedede').lineWidth(0.4).stroke();
    doc.y=top+rowHeight;doc.x=left;
  }
  doc.moveDown(1);
}

export async function renderInvoicePdf(data: ExportData): Promise<Buffer> {
  const en = data.locale === 'en';
  const font = path.resolve(process.env.PDF_FONT_PATH ?? 'public/fonts/NotoSans-Regular.ttf');
  await readFile(font); // Missing local font is an actionable failure, never a broken-font PDF.
  const doc = new PDFDocument({size:'A4',layout:data.kind==='detail'?'portrait':'landscape',margins:{top:40,bottom:40,left:36,right:36},bufferPages:true,info:{Title:en?'BotZone sales document':'Chứng từ bán hàng BotZone',Author:'BotZone local',CreationDate:new Date(data.readAt)}});
  const chunks: Buffer[]=[];
  const result = new Promise<Buffer>((resolve,reject) => {doc.on('data',(chunk:Buffer)=>chunks.push(chunk));doc.on('end',()=>resolve(Buffer.concat(chunks)));doc.on('error',reject);});
  doc.registerFont('BotZone',font).font('BotZone').fillColor('#18181b').fontSize(23).text('BotZone');
  const title = data.kind==='detail' ? `${en?'SALES DOCUMENT':'PHIẾU BÁN HÀNG'} · ${data.detail!.invoice.invoiceNo ?? 'DRAFT'}` : data.kind==='report' ? (en?'INVOICE STATISTICS':'THỐNG KÊ HÓA ĐƠN') : (en?'INVOICE LIST':'DANH SÁCH HÓA ĐƠN');
  doc.moveDown(0.4).fontSize(14).text(title).moveDown(0.5).fontSize(9).fillColor('#444444')
    .text(en?'Local demonstration document — not a statutory electronic invoice.':'Chứng từ mô phỏng local — không phải hóa đơn điện tử hợp pháp.')
    .text(`${en?'Data read':'Dữ liệu đọc'}: ${time(data.readAt,data.locale)} (Asia/Ho_Chi_Minh)`)
    .text(`${en?'Invoices':'Hóa đơn'}: ${data.rowCount} · ${en?'Value':'Giá trị'}: ${money(data.totalVnd,data.locale)}`).moveDown(0.8);
  const width=doc.page.width-doc.page.margins.left-doc.page.margins.right;
  if (data.detail) {
    const detail=data.detail;
    doc.fontSize(10).text(`${en?'Order':'Đơn hàng'}: ${detail.invoice.orderNo}`)
      .text(`${en?'Customer':'Khách hàng'}: ${detail.invoice.customerName}`)
      .text(`${en?'Address':'Địa chỉ'}: ${Object.values(detail.invoice.addressSnapshot).map(text).join(', ')}`)
      .text(`${en?'Responsible staff':'Nhân viên phụ trách'}: ${detail.invoice.staffName}`)
      .text(`${en?'Issued':'Phát hành'}: ${time(detail.invoice.issuedAt,data.locale)}`)
      .text(`${en?'Promotions':'Khuyến mãi'}: ${detail.promotions.length ? detail.promotions.map(p=>`${text(p.code_normalized)} v${text(p.version_no)}: ${money(text(p.amount_vnd),data.locale)}`).join('; ') : '—'}`).moveDown();
    const cols:PdfTableColumn[]=[{title:en?'Product / SKU / serial':'Sản phẩm / SKU / serial',width:width*0.39},{title:en?'Qty':'SL',width:width*0.07,align:'right'},{title:en?'Unit VND':'Đơn giá VND',width:width*0.18,align:'right'},{title:en?'Discount VND':'Giảm VND',width:width*0.17,align:'right'},{title:en?'Net VND':'Thành tiền VND',width:width*0.19,align:'right'}];
    pdfTable(doc,cols,detail.items.map(item=>[`${item.name}\n${item.sku}\n${Object.values(item.variant).join(' · ')}${item.serials.length?'\n'+item.serials.join('\n'):''}`,String(item.quantity),money(item.unitPriceVnd,data.locale).replace(' VND',''),money((BigInt(item.voucherDiscountVnd)+BigInt(item.pointsDiscountVnd)).toString(),data.locale).replace(' VND',''),money(item.netAmountVnd,data.locale).replace(' VND','')]),en?'Purchased items':'Chi tiết hàng mua','BotZone');
    if(doc.y+100>doc.page.height-doc.page.margins.bottom-22)doc.addPage();
    doc.fontSize(11).text(`${en?'Original invoice total':'Tổng hóa đơn gốc'}: ${money(detail.invoice.grandTotalVnd,data.locale)}`)
      .fontSize(9).text(`${en?'Subtotal / discount':'Trước giảm / giảm'}: ${money(detail.invoice.subtotalVnd,data.locale)} / ${money(detail.invoice.discountTotalVnd,data.locale)}`)
      .text(`${en?'Issued adjustments / current invoice net':'Điều chỉnh đã phát hành / ròng hiện tại'}: ${money(detail.invoice.creditNoteTotalVnd,data.locale)} / ${money(detail.invoice.netVnd,data.locale)}`)
      .text(`${en?'Received / refunded at read time':'Đã thu / đã hoàn tại thời điểm đọc'}: ${money(detail.invoice.receivedVnd,data.locale)} / ${money(detail.invoice.refundedVnd,data.locale)}`).moveDown();
    pdfTable(doc,[{title:en?'Type / source':'Loại / nguồn',width:width*0.27},{title:en?'Date / status':'Thời điểm / trạng thái',width:width*0.35},{title:'VND',width:width*0.38,align:'right'}],
      [...detail.receipts.map(r=>[`${en?'Receipt':'Khoản thu'} · ${text(r.tender_kind)}`,time(text(r.received_at),data.locale),money(text(r.amount_vnd),data.locale)]),...detail.refunds.map(r=>[`${en?'Refund':'Khoản hoàn'}`,`${text(r.status)}${r.completed_at?' · '+time(text(r.completed_at),data.locale):''}`,money(text(r.amount_vnd),data.locale)])],en?'Settlement at read time':'Thu và hoàn tại thời điểm đọc','BotZone');
    if(detail.creditNotes.length) pdfTable(doc,[{title:en?'Credit note':'Chứng từ giảm',width:width*0.24},{title:en?'Reason / date':'Lý do / ngày',width:width*0.48},{title:'VND',width:width*0.28,align:'right'}],detail.creditNotes.map(c=>[text(c.credit_note_no),`${text(c.reason)}\n${c.issued_at?time(text(c.issued_at),data.locale):text(c.status)}`,money(text(c.total_vnd),data.locale)]),en?'Adjustments':'Điều chỉnh','BotZone');
  } else if (data.statistics) {
    const stats=data.statistics;
    doc.fontSize(9).text(`${stats.groupBy} · ${stats.granularity} · ${stats.metric}`).text(`${time(stats.fromInclusive,data.locale)} ≤ ${en?'event time':'thời điểm phát sinh'} < ${time(stats.toExclusive,data.locale)}`).moveDown();
    pdfTable(doc,[{title:en?'Period':'Kỳ',width:width*0.13},{title:en?'Object':'Đối tượng',width:width*0.27},{title:en?'Invoices':'Hóa đơn',width:width*0.09,align:'right'},{title:'VND',width:width*0.18,align:'right'},{title:en?'Demo cash':'Tiền mô phỏng',width:width*0.17,align:'right'},{title:en?'Store credit':'Số dư mua hàng',width:width*0.16,align:'right'}],stats.rows.map(row=>[row.period,row.dimensionName,String(row.invoiceCount),money(row.totalVnd,data.locale),money(row.cashDemoVnd,data.locale),money(row.storeCreditVnd,data.locale)]),en?'Statistics':'Bảng thống kê','BotZone');
  } else {
    pdfTable(doc,[{title:en?'Invoice / issued':'Mã / ngày phát hành',width:width*0.2},{title:en?'Customer':'Khách hàng',width:width*0.2},{title:en?'Staff':'Nhân viên',width:width*0.16},{title:en?'Total VND':'Tổng VND',width:width*0.16,align:'right'},{title:en?'Adjustment VND':'Giảm VND',width:width*0.14,align:'right'},{title:en?'Net VND':'Ròng VND',width:width*0.14,align:'right'}],data.invoices.map(invoice=>[`${invoice.invoiceNo??'DRAFT'}\n${time(invoice.issuedAt,data.locale)}`,invoice.customerName,invoice.staffName,money(invoice.grandTotalVnd,data.locale),money(invoice.creditNoteTotalVnd,data.locale),money(invoice.netVnd,data.locale)]),en?'Matching invoices':'Hóa đơn phù hợp','BotZone');
  }
  if(doc.y+90>doc.page.height-doc.page.margins.bottom-22)doc.addPage();
  doc.fontSize(11).text(`${en?'TOTAL':'TỔNG'}: ${money(data.totalVnd,data.locale)}`).moveDown(0.8).fontSize(8)
    .text(`${en?'Filter (validated)':'Điều kiện lọc (đã kiểm tra)'}: ${JSON.stringify(data.query)}`,{width})
    .text(en?'All matching results are included, independent of the current UI page.':'Bao gồm toàn bộ kết quả phù hợp, không phụ thuộc trang đang xem.');
  const pages=doc.bufferedPageRange();
  for(let pageIndex=pages.start;pageIndex<pages.start+pages.count;pageIndex++){
    doc.switchToPage(pageIndex);doc.font('BotZone').fontSize(8).fillColor('#666666')
      .text(`BotZone · ${en?'Page':'Trang'} ${pageIndex+1}/${pages.count}`,doc.page.margins.left,doc.page.height-27,{width:doc.page.width-doc.page.margins.left-doc.page.margins.right,align:'right',lineBreak:false});
  }
  doc.end();
  return result;
}

export async function createInvoiceExport(raw: unknown, actor: Actor) {
  const {input,data}=await materializeInvoiceExport(raw,actor);
  const exportId=randomUUID();
  const expiresAt=new Date(Date.now()+24*60*60*1000);
  await pool.query(`INSERT INTO invoice_exports(id,requested_by_user_id,query_json,read_at,row_count,total_vnd,status,expires_at,role_snapshot) VALUES($1,$2,$3,$4,$5,$6,'building',$7,$8)`,[exportId,actor.id,JSON.stringify(input),data.readAt,data.rowCount,data.totalVnd,expiresAt,actor.role]);
  const directory=path.resolve(process.env.UPLOAD_DIR??'uploads','export-private');
  const manifest:ExportManifest={files:{},invoiceIds:data.invoices.map(invoice=>invoice.id),datasetChecksum:sha(JSON.stringify(data)),kind:data.kind,locale:data.locale};
  try {
    await mkdir(directory,{recursive:true});
    for(const format of input.formats){
      const buffer=format==='xlsx'?await renderInvoiceXlsx(data):await renderInvoicePdf(data);
      const filename=`${exportId}.${format}`;
      await writeFile(path.join(directory,filename),buffer,{flag:'wx'});
      manifest.files[format]={filename,bytes:buffer.byteLength,checksum:sha(buffer),contentType:contentTypes[format]};
    }
    await pool.query(`UPDATE invoice_exports SET status='ready',file_manifest=$2 WHERE id=$1`,[exportId,JSON.stringify(manifest)]);
    const links:Partial<Record<ExportFormat,string>>={};
    for(const format of input.formats)links[format]=`/api/invoices/exports/${exportId}/${format}`;
    return {exportId,rowCount:data.rowCount,totalVnd:data.totalVnd,readAt:data.readAt,links};
  } catch(error) {
    await pool.query(`UPDATE invoice_exports SET status='failed',file_manifest=$2 WHERE id=$1`,[exportId,JSON.stringify(manifest)]);
    throw error;
  }
}

export async function downloadExport(id: string, format: string, actor: Actor): Promise<{buffer: Buffer; filename: string; contentType: string}> {
  validateUuid(id);
  if(format!=='xlsx'&&format!=='pdf')throw new AppError('NOT_FOUND','Định dạng không tồn tại.',404);
  const result=await pool.query<{requested_by_user_id:string;role_snapshot:string;status:string;expires_at:Date;query_json:ExportInput;file_manifest:ExportManifest}>(`SELECT requested_by_user_id,role_snapshot,status,expires_at,query_json,file_manifest FROM invoice_exports WHERE id=$1`,[id]);
  const row=result.rows[0];
  if(!row||row.requested_by_user_id!==actor.id)throw new AppError('NOT_FOUND','Không tìm thấy tệp xuất trong phạm vi của bạn.',404);
  if(row.role_snapshot!==actor.role||!['customer','owner','order_staff'].includes(actor.role))throw new AppError('FORBIDDEN','Quyền truy cập tệp xuất đã thay đổi.',403);
  if(actor.role!=='customer'){
    const staff=await pool.query<{role:string;active:boolean}>('SELECT role,active FROM staff_profiles WHERE user_id=$1',[actor.id]);
    if(!staff.rows[0]?.active||staff.rows[0].role!==actor.role)throw new AppError('FORBIDDEN','Tài khoản nhân viên không còn quyền truy cập.',403);
  }
  if(row.status!=='ready'||new Date(row.expires_at).getTime()<=Date.now())throw new AppError('EXPORT_EXPIRED','Tệp chưa sẵn sàng hoặc đã hết hạn. Hãy tạo lượt xuất mới.',410);
  if(actor.role==='customer'){
    if(row.query_json.kind!=='detail'||!row.query_json.invoiceId)throw new AppError('FORBIDDEN','Chỉ được tải hóa đơn của mình.',403);
    const owned=await pool.query('SELECT id FROM invoices WHERE id=$1 AND customer_id=$2 AND lifecycle=\'issued\'',[row.query_json.invoiceId,actor.id]);
    if(!owned.rowCount)throw new AppError('FORBIDDEN','Không còn quyền đọc hóa đơn.',403);
  }
  if(actor.role==='order_staff'&&row.query_json.kind==='report'&&row.file_manifest.invoiceIds.length){
    const outside=await pool.query('SELECT id FROM invoices WHERE id=ANY($1::uuid[]) AND responsible_staff_id IS DISTINCT FROM $2 LIMIT 1',[row.file_manifest.invoiceIds,actor.id]);
    if(outside.rowCount)throw new AppError('FORBIDDEN','Tệp nằm ngoài phạm vi báo cáo hiện tại.',403);
  }
  const file=row.file_manifest.files[format];
  if(!file||file.filename!==`${id}.${format}`)throw new AppError('NOT_FOUND','Tệp xuất không tồn tại.',404);
  const buffer=await readFile(path.resolve(process.env.UPLOAD_DIR??'uploads','export-private',file.filename));
  if(buffer.byteLength!==file.bytes||sha(buffer)!==file.checksum)throw new AppError('EXPORT_CORRUPT','Tệp xuất không khớp checksum; hãy tạo lượt xuất mới.',409);
  return {buffer,filename:`BotZone-${id}.${format}`,contentType:contentTypes[format]};
}
