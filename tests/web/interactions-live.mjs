import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
const observe = page => {
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push({ method: request.method(), url: request.url() }); });
};
const choose = async (page, label, option) => {
  await page.getByRole('combobox', { name: label, exact: true }).click();
  await page.getByRole('option', { name: option, exact: true }).click();
};
let page;
await mkdir('artifacts/web', { recursive: true });
try {
  const desktop = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  page = await desktop.newPage(); observe(page);
  await page.goto(base + '/en/products', { waitUntil: 'networkidle' });
  const catalogue = await (await page.request.get(base + '/api/catalogue?locale=en')).json();
  const total = catalogue.products.length;
  assert.ok(total > 12, 'Live catalogue has enough products to exercise pagination');
  await expect(page.locator('.catalogue-grid .product-card')).toHaveCount(12);
  await expect(page.locator('.result-count')).toContainText(`of ${total} products`);
  const firstPage = await page.locator('.product-card h3').allTextContents();
  await page.getByRole('link', { name: 'Next page', exact: true }).click();
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '2');
  await expect(page.locator('.catalogue-grid .product-card')).toHaveCount(Math.min(12, total - 12));
  assert.notDeepEqual(await page.locator('.product-card h3').allTextContents(), firstPage);
  await expect(page.getByRole('region', { name: 'Product results' })).toBeFocused();
  await page.goBack(); await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '1');
  await page.goForward(); await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '2');
  results.push('Desktop: 12 per page; different products; range/current-page; focus at results; Back/Forward restores page');

  const sort = page.getByRole('combobox', { name: 'Sort', exact: true });
  await sort.focus(); await sort.press('Enter');
  await expect(page.getByRole('listbox')).toBeVisible();
  assert.equal(await page.getByRole('listbox').evaluate(element => !!element.closest('main')), false, 'Dropdown portals out of main');
  await page.keyboard.press('Escape'); await expect(sort).toBeFocused();
  await expect(page.getByRole('listbox')).toHaveCount(0);
  await sort.press('Enter');
  await expect(page.getByRole('option', { name: 'Featured', exact: true })).toBeFocused();
  await page.keyboard.press('End');
  await expect(page.getByRole('option', { name: 'Price: high to low', exact: true })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(sort).toContainText('Price: high to low');
  await expect.poll(() => new URL(page.url()).searchParams.get('sort')).toBe('price-desc');
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '1');
  await choose(page, 'Availability', 'In stock');
  await page.getByRole('button', { name: 'iPhone', exact: true }).click();
  await page.getByRole('textbox', { name: 'Search products', exact: true }).fill('iPhone 18');
  assert.equal(new URL(page.url()).searchParams.get('page'), null);
  await page.getByRole('link', { name: 'VI', exact: true }).click();
  await page.waitForURL('**/vi/products?**');
  const translated = new URL(page.url()).searchParams;
  for (const [key, value] of Object.entries({ sort: 'price-desc', stock: 'available', category: 'iphone', q: 'iPhone 18' })) assert.equal(translated.get(key), value);
  await expect(page.locator('.catalogue-page h1')).toContainText('Thế giới Apple.');
  assert.equal((await page.locator('.catalogue-page').innerText()).includes('s?n ph?m'), false);
  results.push('Radix keyboard Enter/End/Escape and focus return; portal; filters reset page and survive language change with correct Vietnamese');

  await page.goto(base + '/en/products?page=9999&sort=price-asc', { waitUntil: 'networkidle' });
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', String(Math.ceil(total / 12)));
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe(String(Math.ceil(total / 12)));
  await page.getByRole('textbox', { name: 'Search products', exact: true }).fill('no-product-matches-this');
  await expect(page.getByRole('heading', { name: 'No matching products' })).toBeVisible();
  assert.equal(new URL(page.url()).searchParams.get('page'), null);
  await expect(page.locator('.result-count')).toHaveText('Showing 0 of 0 products');
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(page.locator('.catalogue-grid .product-card')).toHaveCount(12);
  results.push('Out-of-range page is canonicalized; search clears page; empty results and clear filters work');

  // Exercise dialog focus and reduced motion without purchasing or changing CMS.
  await page.goto(base + '/en', { waitUntil: 'networkidle' });
  const explore = page.getByRole('button', { name: 'Explore in 3D', exact: true }).first();
  await explore.click(); await expect(page.getByRole('dialog')).toBeVisible();
  assert.equal(await page.getByRole('dialog').evaluate(el => getComputedStyle(el).animationName), 'dialog-arrive');
  await page.keyboard.press('Escape'); await expect(page.getByRole('dialog')).toHaveCount(0); await expect(explore).toBeFocused();
  results.push('Dialog enter animation, Escape dismiss, opener focus restored');
  await desktop.close();

  const mobile = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
  page = await mobile.newPage(); observe(page);
  await page.goto(base + '/en/products?sort=price-asc&page=2', { waitUntil: 'networkidle' });
  await expect(page.locator('.catalogue-grid .product-card')).toHaveCount(6);
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '2');
  await page.getByRole('link', { name: 'Next page', exact: true }).tap();
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '3');
  await page.goBack(); await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '2');
  const currency = page.getByRole('combobox', { name: 'Display currency' });
  await currency.tap(); await page.getByRole('option', { name: 'USD', exact: true }).tap();
  await expect(currency).toContainText('USD');
  await page.reload({ waitUntil: 'networkidle' }); await expect(currency).toContainText('USD');
  await page.getByRole('combobox', { name: 'Sort', exact: true }).tap();
  await page.getByRole('option', { name: 'Price: high to low', exact: true }).tap();
  await expect(page.locator('.catalogue-grid')).toHaveAttribute('data-page', '1');
  await page.getByRole('combobox', { name: 'Availability', exact: true }).tap();
  await page.screenshot({ path: 'artifacts/web/dropdown-mobile.png', fullPage: false });
  const menuBox = await page.getByRole('listbox').boundingBox();
  assert.ok(menuBox.x >= 0 && menuBox.x + menuBox.width <= 390, 'Mobile dropdown fits screen');
  await page.keyboard.press('Escape');
  await page.screenshot({ path: 'artifacts/web/pagination-mobile.png', fullPage: true });
  results.push('Touch/mobile: 6 per page, Back, currency persistence, menu collision bounds, sort resets page');

  for (const width of [360, 390, 430, 768, 1024, 1366, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto(base + '/vi/products', { waitUntil: 'networkidle' });
    await expect(page.locator('.catalogue-grid .product-card')).toHaveCount(width < 768 ? 6 : 12);
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `No overflow at ${width}`);
    const nav = await page.locator('.catalogue-pagination').boundingBox();
    assert.ok(nav.x >= 0 && nav.x + nav.width <= width + 1, `Pagination fits ${width}`);
    results.push({ width, cardsPerPage: width < 768 ? 6 : 12, overflow: false });
  }
  await page.screenshot({ path: 'artifacts/web/pagination-desktop.png', fullPage: true });
  await mobile.close();

  const reduced = await browser.newContext({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce' });
  page = await reduced.newPage(); observe(page);
  await page.goto(base + '/en/products', { waitUntil: 'networkidle' });
  await expect(page.locator('.catalogue-grid')).toHaveCSS('animation-name', 'none');
  await page.getByRole('combobox', { name: 'Sort', exact: true }).click();
  await expect(page.getByRole('listbox')).toHaveCSS('animation-name', 'none');
  await page.keyboard.press('Escape');
  await page.locator('.product-card').first().hover();
  await expect(page.locator('.product-card').first()).toHaveCSS('transform', 'none');
  await page.getByRole('button', { name: 'Open menu', exact: true }).click();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Open menu', exact: true })).toBeFocused();
  await expect(page.locator('.mobile-navigation')).toHaveCount(0);
  results.push('Reduced motion: no grid/menu animation or card transform; mobile navigation Escape/focus works');
  await reduced.close();
  assert.deepEqual(errors, [], 'Uncaught browser errors');
  assert.deepEqual(writes, [], 'Only guest navigation; no mutating API requests');
  console.log(JSON.stringify({ passed: results.length, errors, writes }));
} catch (error) {
  results.push({ failed: error.message });
  await page?.screenshot({ path: 'artifacts/web/interactions-failure.png', fullPage: true }).catch(() => {});
  throw error;
} finally {
  await writeFile('artifacts/web/interactions-results.json', JSON.stringify({ at: new Date().toISOString(), base, results, errors, writes }, null, 2));
  await browser.close();
}
