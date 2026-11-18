import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
process.env.BOTZONE_WEB_URL ||= 'http://127.0.0.1:5080';
const { client } = await import('../web/live-api.mjs');
const owner = await client('owner@botzone.local');
try {
  const refs = Array.from({ length: 8 }, (_, i) => `G${String(i + 1).padStart(2, '0')}`);
  const query = { filter: { kind: 'group', logic: 'OR', children: refs.map(value => ({ kind: 'rule', field: 'invoiceNo', op: '=', value })) },
    sort: [{ field: 'invoiceNo', direction: 'asc' }], page: 1, pageSize: 20 };
  const list = await owner.post('/api/admin/invoices/search', query);
  assert.equal(list.matchedCount, 8);
  assert.deepEqual(list.items.map(item => item.invoiceNo), refs);
  const total = field => list.items.reduce((sum, item) => sum + BigInt(item[field]), 0n).toString();
  const totals = { issuedVnd: total('grandTotalVnd'), creditNotesVnd: total('creditNoteTotalVnd'), netVnd: total('netVnd') };
  assert.deepEqual(totals, { issuedVnd: '97130000', creditNotesVnd: '3500000', netVnd: '93630000' });
  await mkdir('artifacts/product-photos', { recursive: true });
  await writeFile('artifacts/product-photos/golden-preserved.json', JSON.stringify({ at: new Date().toISOString(), passed: true,
    scope: 'Authenticated read-only invoice query after catalogue photo and landing content updates; no seed/export/business mutation', query, count: list.matchedCount, ...totals }, null, 2));
  console.log(JSON.stringify({ count: list.matchedCount, ...totals }));
  await owner.post('/api/auth/logout', {});
} finally { await owner.dispose(); }
