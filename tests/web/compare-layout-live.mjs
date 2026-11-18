import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const data = await (await fetch(base + '/api/catalogue?locale=en')).json();
const selected = data.products.filter(product => product.category.toLowerCase() === 'iphone').slice(0, 3);
assert.equal(selected.length, 3);
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
await mkdir('artifacts/web/compare-bag', { recursive: true });
try {
  for (const locale of ['vi', 'en']) for (const width of [360, 390, 430, 768, 1024, 1366, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 1000 }, reducedMotion: 'reduce' });
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
    // Seed only the guest's comparison selection, using actual API product IDs.
    await page.addInitScript(ids => localStorage.setItem('botzone.compare', JSON.stringify(ids)), selected.map(product => product.id));
    await page.goto(`${base}/${locale}/compare`, { waitUntil: 'networkidle' });
    await expect(page.locator('.compare-table thead th')).toHaveCount(4);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${locale} ${width}`);
    await expect(page.getByText(locale === 'vi' ? 'Cuộn trong bảng để xem toàn bộ sản phẩm và thông số.' : 'Scroll within the table to see all products and specifications.', { exact: true })).toBeVisible();
    results.push({ locale, width, products: 3, overflow: false });
    if (width === 390 || width === 1440) {
      await page.evaluate(() => { document.activeElement?.blur?.(); scrollTo(0, 0); });
      await page.screenshot({ path: `artifacts/web/compare-bag/compare-clean-${locale}-${width}.png`, fullPage: true });
      const region = page.getByRole('region').filter({ has: page.locator('.compare-table') });
      await region.scrollIntoViewIfNeeded(); await region.evaluate(element => { element.scrollTop = 450; });
      await region.screenshot({ path: `artifacts/web/compare-bag/compare-rows-${locale}-${width}.png` });
    }
    await page.close();
  }
  assert.deepEqual(errors, []); assert.deepEqual(writes, []);
  console.log(JSON.stringify({ passed: results.length, errors, writes }));
} finally {
  await writeFile('artifacts/web/compare-bag/layout-results.json', JSON.stringify({ at: new Date().toISOString(), base, results, errors, writes }, null, 2));
  await browser.close();
}
