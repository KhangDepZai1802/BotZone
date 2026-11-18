import test from 'node:test';
import assert from 'node:assert/strict';
import type { Actor, FilterNode } from '../../src/lib/contracts';
import { buildInvoicePredicate, normalizeInstant, normalizeInvoiceQuery } from '../../src/server/invoice-filter';

const owner: Actor = {id:'owner',name:'Owner',email:'owner@botzone.test',role:'owner',verified:true};
const staff: Actor = {...owner,id:'NV01',role:'order_staff'};
const productId='11111111-1111-1111-1111-111111111111';
const query = (filter?: FilterNode) => normalizeInvoiceQuery({filter});

test('parameters contain untrusted text; SQL identifiers and order are server whitelisted', () => {
  const payload="' OR 1=1; DROP TABLE invoices; --";
  const built=buildInvoicePredicate(query({kind:'rule',field:'invoiceNo',op:'=',value:payload}),owner);
  assert.equal(built.text.includes(payload),false);
  assert.equal(built.values.at(-1),payload);
  assert.throws(()=>normalizeInvoiceQuery({sort:[{field:'issued_at; DROP TABLE invoices',direction:'asc'}]}),/sắp xếp/);
  assert.throws(()=>normalizeInvoiceQuery({filter:{kind:'rule',field:'__proto__',op:'=',value:'x'}}),/không được hỗ trợ/);
});

test('report staff scope is outside grouped OR and NOT expressions', () => {
  const filter:FilterNode={kind:'group',logic:'OR',children:[{kind:'rule',field:'responsibleStaffId',op:'=',value:'NV02'},{kind:'not',child:{kind:'rule',field:'grandTotalVnd',op:'>',value:'1000000'}}]};
  const built=buildInvoicePredicate(query(filter),staff,{report:true});
  assert.match(built.text,/^\(i\.responsible_staff_id = \$1\) AND/);
  assert.equal(built.values[0],'NV01');
  assert.match(built.text,/ OR \(NOT /);
});

test('customer scope requires explicit detail capability; content and warehouse denied', () => {
  const customer={...owner,id:'customer-1',role:'customer' as const};
  assert.throws(()=>buildInvoicePredicate(query(),customer),/không có quyền/);
  assert.match(buildInvoicePredicate(query(),customer,{allowCustomer:true}).text,/i\.customer_id = \$1/);
  assert.throws(()=>buildInvoicePredicate(query(),{...owner,role:'content_staff'}),/không có quyền/);
  assert.throws(()=>buildInvoicePredicate(query(),{...owner,role:'warehouse_staff'}),/không có quyền/);
});

test('not_contains uses NOT EXISTS, never an unequal line predicate', () => {
  const built=buildInvoicePredicate(query({kind:'product',productId,mode:'not_contains'}),owner);
  assert.match(built.text,/NOT EXISTS \(SELECT 1 FROM invoice_items/);
  assert.match(built.text,/filter_item\.product_id = \$2::uuid/);
  assert.equal(built.values[1],productId);
});

test('Vietnam calendar equality is a half-open day including year boundary', () => {
  const built=buildInvoicePredicate(query({kind:'rule',field:'issuedAt',op:'=',value:'2026-01-01'}),owner);
  assert.deepEqual(built.values,['issued','2025-12-31T17:00:00.000Z','2026-01-01T17:00:00.000Z']);
  assert.match(built.text,/i\.issued_at >= \$2::timestamptz AND i\.issued_at < \$3::timestamptz/);
});

test('Vietnam date operators use correct next-day bounds', () => {
  const expectations={'>':['>=','2026-01-01T17:00:00.000Z'],'<':['<','2025-12-31T17:00:00.000Z'],'>=':['>=','2025-12-31T17:00:00.000Z'],'<=':['<','2026-01-01T17:00:00.000Z']} as const;
  for(const op of ['>','<','>=','<='] as const){
    const built=buildInvoicePredicate(query({kind:'rule',field:'issuedAt',op,value:'2026-01-01'}),owner);
    assert.ok(built.text.includes(`i.issued_at ${expectations[op][0]} $2::timestamptz`));
    assert.equal(built.values[1],expectations[op][1]);
  }
  assert.throws(()=>normalizeInstant('2026-02-30'),/không hợp lệ/);
  assert.throws(()=>normalizeInstant('2026-01-01T00:00:00'),/múi giờ/);
});

test('BIGINT precision preserved and unsafe JavaScript numbers rejected', () => {
  const built=buildInvoicePredicate(query({kind:'rule',field:'grandTotalVnd',op:'>',value:'9223372036854775807'}),owner);
  assert.equal(built.values[1],'9223372036854775807');
  assert.match(built.text,/::bigint/);
  assert.throws(()=>query({kind:'rule',field:'grandTotalVnd',op:'=',value:9007199254740992}),/chuỗi/);
  assert.throws(()=>query({kind:'rule',field:'grandTotalVnd',op:'=',value:'9223372036854775808'}),/không hợp lệ/);
});

test('NULL is explicit and maintains PostgreSQL three-valued NOT semantics', () => {
  const built=buildInvoicePredicate(query({kind:'null',field:'responsibleStaffId',isNull:true}),owner,{lifecycle:'any'});
  assert.match(built.text,/i\.responsible_staff_id IS NULL/);
  assert.equal(built.values.length,0);
  assert.throws(()=>normalizeInvoiceQuery({filter:{kind:'rule',field:'invoiceNo',op:'=',value:null}}),/văn bản/);
});

test('bounded filter depth and leaf count reject pathological input', () => {
  let filter:FilterNode={kind:'rule',field:'grandTotalVnd',op:'=',value:'0'};
  for(let i=0;i<4;i++) filter={kind:'not',child:filter};
  assert.throws(()=>query(filter),/bốn cấp/);
  assert.throws(()=>query({kind:'group',logic:'AND',children:Array.from({length:31},()=>({kind:'rule' as const,field:'grandTotalVnd' as const,op:'=' as const,value:'0'}))}),/Nhóm điều kiện/);
});

test('sort always includes stable id tie-breaker and event reports omit issued window', () => {
  const normalized=normalizeInvoiceQuery({fromInclusive:'2025-05-01',toExclusive:'2025-06-01',sort:[{field:'grandTotalVnd',direction:'asc'}]});
  const built=buildInvoicePredicate(normalized,owner,{includeWindow:false,report:true});
  assert.equal(built.orderBy,'i.grand_total_vnd ASC NULLS LAST, i.id ASC');
  assert.equal(built.text.includes('issued_at'),false);
  assert.equal(normalized.fromInclusive,'2025-04-30T17:00:00.000Z');
});
