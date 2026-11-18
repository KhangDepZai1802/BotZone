import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/web/refinement-compare';
await mkdir(out, { recursive: true });
const catalogue = await (await fetch(base + '/api/catalogue?locale=en')).json();
const byCode = code => { const product = catalogue.products.find(p => p.modelCode === code); assert.ok(product, code); return product; };
const [p13, p14, pro, duo] = ['P13', 'P14', 'P18MAX', 'DUO'].map(byCode);
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
let page;
const setup = async (products, locale, width, height) => {
  page = await browser.newPage({ viewport: { width, height }, reducedMotion: 'reduce' });
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
  await page.addInitScript(ids => { if (localStorage.getItem('botzone.compare') === null) localStorage.setItem('botzone.compare', JSON.stringify(ids)); }, products.map(p => p.id));
  await page.goto(`${base}/${locale}/compare`, { waitUntil: 'networkidle' });
  await expect(page.locator('.compare-table thead th')).toHaveCount(products.length + 1);
  return page.getByRole('region').filter({ has: page.locator('.compare-table') });
};
const winners = field => page.locator(`tr[data-spec-id="${field}"] td`).evaluateAll(cells => cells.flatMap((cell, index) => cell.dataset.advantage === 'true' ? [index] : []));
try {
  for (const locale of ['vi', 'en']) for (const [width, height] of [[390, 844], [1024, 900], [1366, 768], [1440, 900]]) {
    const region = await setup([p13, pro], locale, width, height);
    assert.deepEqual(await winners('variant-storage'), [1]);
    assert.deepEqual(await winners('storage_options'), [1]);
    assert.deepEqual(await winners('refresh_rate'), [], 'Unknown refresh rate is not zero');
    const unsupported = await page.locator('tr[data-spec-id]').evaluateAll(rows => rows.filter(row => row.querySelector('[data-advantage="true"]') && !['variant-storage', 'storage', 'storage_options', 'memory_options', 'refresh_rate'].includes(row.dataset.specId)).map(row => row.dataset.specId));
    assert.deepEqual(unsupported, [], 'Subjective fields never receive numeric ranking');
    await expect(page.locator('tr[data-spec-id="variant-storage"] td').nth(1).locator('strong')).toHaveCSS('font-weight', '700');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await region.evaluate(element => element.scrollIntoView({ block: 'start', behavior: 'instant' }));
    await expect.poll(() => region.evaluate(element => Math.abs(element.getBoundingClientRect().top) < innerHeight)).toBe(true);
    const geometry = await region.evaluate(element => {
      const region = element.getBoundingClientRect(), header = element.querySelector('thead').getBoundingClientRect();
      const bottom = Math.min(region.bottom - 2, innerHeight - 2);
      const rows = [...element.querySelectorAll('tbody tr')].filter(row => row.querySelector('th[scope="row"]')).map(row => row.getBoundingClientRect());
      return { headerHeight: header.height, regionHeight: region.height, visibleDataRows: rows.filter(row => row.top >= header.bottom - 1 && row.bottom <= bottom).length, headerTop: header.top, regionTop: region.top, scrollHeight: element.scrollHeight, clientHeight: element.clientHeight };
    });
    assert.ok(geometry.headerHeight <= (width < 768 ? 170 : 160), `Compact header at ${locale} ${width}: ${geometry.headerHeight}`);
    assert.ok(geometry.scrollHeight > geometry.clientHeight, 'The product table scrolls internally');
    if (width === 1366) assert.ok(geometry.visibleDataRows >= 7, `At least 7 full data rows visible at 1366x768; got ${geometry.visibleDataRows}`);
    await region.screenshot({ path: `${out}/table-${locale}-${width}-initial.png` });
    if (width === 390) {
      await region.evaluate(element => { element.scrollLeft = element.scrollWidth; });
      await region.screenshot({ path: `${out}/table-${locale}-${width}-advantage.png` });
      await region.evaluate(element => { element.scrollLeft = 0; });
    }
    const windowY = await page.evaluate(() => scrollY);
    await region.evaluate(element => { element.scrollTop = 420; });
    const after = await region.evaluate(element => ({ top: element.getBoundingClientRect().top, headerTops: [...element.querySelectorAll('thead th')].map(cell => cell.getBoundingClientRect().top), scrollTop: element.scrollTop }));
    assert.ok(after.scrollTop >= 400);
    assert.ok(after.headerTops.every(top => Math.abs(top - after.top) <= 3), 'Every product header cell remains pinned inside the table');
    assert.equal(await page.evaluate(() => scrollY), windowY, 'Inner scrolling does not move the page');
    await region.screenshot({ path: `${out}/table-${locale}-${width}-scrolled.png` });
    await page.evaluate(() => { document.activeElement?.blur?.(); scrollTo(0, 0); });
    await page.screenshot({ path: `${out}/page-${locale}-${width}.png`, fullPage: true });
    results.push({ locale, width, height, ...geometry, pinnedGeometry: after, advantages: 'P18MAX wins offered/manufacturer storage; unknown rate and subjective fields unranked', pinned: true, overflow: false });
    await page.close();
  }

  await setup([p13, p14], 'en', 1366, 768);
  assert.deepEqual(await winners('variant-storage'), []);
  assert.deepEqual(await winners('storage_options'), []);
  results.push({ case: 'Equal 128GB offered and equal 512GB manufacturer maximum receive no winner', passed: true });
  await page.close();

  const threeRegion = await setup([p13, pro, duo], 'en', 1366, 768);
  assert.deepEqual(await winners('variant-storage'), [1, 2]);
  assert.deepEqual(await winners('storage_options'), [1, 2]);
  assert.deepEqual(await winners('refresh_rate'), []);
  await threeRegion.evaluate(element => element.scrollIntoView({ block: 'start', behavior: 'instant' }));
  const threeGeometry = await threeRegion.evaluate(element => {
    const region = element.getBoundingClientRect(), header = element.querySelector('thead').getBoundingClientRect();
    return { headerHeight: header.height, visibleDataRows: [...element.querySelectorAll('tbody tr')].filter(row => row.querySelector('th[scope="row"]')).filter(row => { const box = row.getBoundingClientRect(); return box.top >= header.bottom - 1 && box.bottom <= Math.min(region.bottom - 2, innerHeight - 2); }).length };
  });
  assert.ok(threeGeometry.visibleDataRows >= 7, `Three products show at least 7 complete data rows: ${threeGeometry.visibleDataRows}`);
  assert.ok(threeGeometry.headerHeight <= 160);
  await threeRegion.screenshot({ path: `${out}/table-en-1366-three-products.png` });
  results.push({ case: 'Three products retain compact header and at least 7 full data rows at 1366x768', ...threeGeometry, passed: true });
  const toggle = page.getByRole('switch', { name: 'Show differences only', exact: true });
  await toggle.click(); await expect(page.locator('tr[data-different="false"]')).toHaveCount(0);
  await toggle.focus(); await page.keyboard.press('Space'); await expect(toggle).toHaveAttribute('aria-checked', 'false');
  await page.getByRole('button', { name: `Remove ${duo.name} from comparison`, exact: true }).click();
  await expect(page.locator('.compare-table thead th')).toHaveCount(3);
  assert.deepEqual(await winners('variant-storage'), [1]);
  await page.goto(base + '/en/products?category=iphone', { waitUntil: 'networkidle' });
  const card = product => page.locator('.product-card').filter({ has: page.locator(`h3 a[href$="/${product.slug}"]`) });
  await card(duo).getByRole('button', { name: 'Compare', exact: true }).click();
  await card(p14).getByRole('button', { name: 'Compare', exact: true }).click();
  await expect(page.getByTestId('compare-dock')).toContainText('You have 3 products');
  await page.getByTestId('compare-dock').getByRole('button', { name: `Remove ${duo.name} from comparison`, exact: true }).click();
  await page.goto(base + '/en/products?category=mac', { waitUntil: 'networkidle' });
  await page.locator('.product-card').first().getByRole('button', { name: 'Compare', exact: true }).click();
  await expect(page.getByTestId('compare-dock')).toContainText('same category');
  await page.getByRole('button', { name: 'Clear all', exact: true }).click();
  assert.deepEqual(await page.evaluate(() => JSON.parse(localStorage.getItem('botzone.compare'))), []);
  results.push({ case: 'Tied maxima above a lower known value both highlighted; keyboard toggle/remove/max3/category/clear preserved', passed: true });
  await page.close();
  assert.deepEqual(errors, []); assert.deepEqual(writes, []);
  console.log(JSON.stringify({ passed: results.length, laptopRows: results.filter(result => result.width === 1366).map(result => ({ locale: result.locale, rows: result.visibleDataRows, headerHeight: result.headerHeight })), errors, writes }));
} catch (error) {
  results.push({ failed: error.message });
  await page?.screenshot({ path: `${out}/failure.png`, fullPage: true }).catch(() => {});
  throw error;
} finally {
  await writeFile(`${out}/results.json`, JSON.stringify({ at: new Date().toISOString(), base, products: [p13.modelCode, pro.modelCode], results, errors, writes }, null, 2));
  await browser.close();
}
