import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_TEST_API || 'http://127.0.0.1:5080';
const before = JSON.parse(await readFile('artifacts/specifications/catalogue-before.json', 'utf8'));
const applied = JSON.parse(await readFile('artifacts/specifications/import-applied.json', 'utf8'));
const results = [];
const get = async path => {
  const response = await fetch(base + path);
  assert.equal(response.status, 200, path);
  return response.json();
};
const en = await get('/api/catalogue?locale=en'), vi = await get('/api/catalogue?locale=vi');
assert.equal(en.products.length, 35); assert.equal(vi.products.length, 35);
const stable = ({ specs, technicalSpecs, ...product }) => ({ ...product, compatibleProducts: [...(product.compatibleProducts || [])].sort() });
assert.deepEqual(en.products.map(stable), before.products.map(stable));
const { products: oldProducts, ...oldSettings } = before;
const { products: newProducts, ...newSettings } = en;
assert.deepEqual(newSettings, oldSettings);
results.push({ check: 'All existing IDs, names, options, prices, stock, photos, compatibility and settings preserved', passed: true });
assert.equal(applied.changes.length, 35);
for (const change of applied.changes) {
  const { technicalSpecifications, ...retained } = change.after;
  assert.deepEqual(retained, change.before);
  assert.equal(technicalSpecifications.modelCode, change.modelCode);
}
results.push({ check: 'Existing per-product JSON metadata retained beside new technical content', passed: true });
let total = 0;
for (const product of en.products) {
  const local = vi.products.find(p => p.id === product.id);
  const detail = (await get(`/api/catalogue/${product.slug}?locale=en`)).product;
  assert.deepEqual(detail.technicalSpecs, product.technicalSpecs);
  const specs = product.technicalSpecs;
  assert.ok(specs?.sources.length);
  const fields = specs.groups.flatMap(g => g.items);
  assert.ok(fields.length >= 6, product.modelCode);
  total += fields.length;
  assert.equal(new Set(fields.map(item => item.id)).size, fields.length);
  assert.equal(local.technicalSpecs.groups.flatMap(g => g.items).length, fields.length);
  assert.notEqual(local.technicalSpecs.groups[0].label, specs.groups[0].label);
  assert.ok(!Object.hasOwn(product.specs, 'fixture') && !Object.hasOwn(product.specs, 'priceDisclosure'));
  for (const source of specs.sources) assert.match(new URL(source.url).hostname, /(^|\.)apple\.com$/);
  results.push({ model: product.modelCode, specifications: fields.length, passed: true });
}
assert.equal(total, 609);
const duo = en.products.find(p => p.modelCode === 'DUO');
assert.deepEqual(duo.variants.map(v => v.optionLabels.color).sort(), ['Night Sky', 'Star White']);
assert.deepEqual(duo.variants.map(v => v.options.color).sort(), ['Trắng Ánh Sao', 'Trời Đêm'].sort());
const p13 = en.products.find(p => p.modelCode === 'P13');
const items = p13.technicalSpecs.groups.flatMap(group => group.items);
assert.match(items.find(item => item.id === 'chip').value, /A15/);
assert.match(items.find(item => item.id === 'display_size').value, /6\.1/);
results.push({ check: 'Duo localized color labels leave raw SKU keys intact; iPhone 13 has A15 and 6.1-inch specifications', passed: true });
for (const language of ['vi', 'en']) {
  const response = await fetch(base + '/api/catalogue/does-not-exist?locale=' + language, { headers: { 'Accept-Language': language } });
  assert.equal(response.status, 404);
  const body = await response.json();
  assert.equal(body.error.code, 'NOT_FOUND');
  assert.match(body.error.message, language === 'en' ? /product could not be found/ : /Không tìm thấy/);
}
results.push({ check: 'Public API errors follow the selected VI/EN language', passed: true });
await writeFile('artifacts/specifications/live-results.json', JSON.stringify({ at: new Date().toISOString(), base, passed: results.length, models: 35, specifications: total, results }, null, 2));
console.log(JSON.stringify({ passed: results.length, models: 35, specifications: total }));
