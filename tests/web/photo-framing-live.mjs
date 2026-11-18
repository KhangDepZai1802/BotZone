import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], writes = [], errors = [];
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
page.on('pageerror', error => errors.push(error.message));
page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
const scaleOf = locator => locator.evaluate(element => new DOMMatrixReadOnly(getComputedStyle(element).transform).a);
await mkdir('artifacts/product-photos', { recursive: true });
try {
  const catalogue = await (await page.request.get(base + '/api/catalogue?locale=en')).json();
  for (const [code, expectedScale] of Object.entries({ IPA4: 2.4, IPP5: 2.4, MBA5: 1.3, MINI6: 1.7 })) {
    const product = catalogue.products.find(item => item.modelCode === code);
    assert.ok(product, code);
    assert.equal(product.photos.find(photo => photo.path === product.poster).framingScale, expectedScale);
    await page.goto(base + `/en/products?q=${code}`, { waitUntil: 'networkidle' });
    const card = page.locator('.product-card').filter({ has: page.locator(`h3 a[href$="/${product.slug}"]`) });
    await expect(card).toHaveCount(1);
    await expect.poll(() => scaleOf(card.locator('.product-art img'))).toBe(expectedScale);
    await expect(card.locator('.product-art')).toHaveCSS('background-color', 'rgb(255, 255, 255)');
    await expect(card.locator('.product-art')).toHaveCSS('overflow', 'hidden');
    await card.locator('h3 a').click();
    await expect.poll(() => scaleOf(page.locator('.detail-gallery img'))).toBe(expectedScale);
    await page.getByRole('button', { name: 'Add to bag', exact: true }).click();
    results.push({ modelCode: code, framingScale: expectedScale, cardAndDetail: true });
  }
  await page.goto(base + '/en/cart', { waitUntil: 'networkidle' });
  await expect(page.locator('.cart-line')).toHaveCount(4);
  for (const result of results) {
    const product = catalogue.products.find(item => item.modelCode === result.modelCode);
    const line = page.locator('.cart-line').filter({ has: page.getByRole('heading', { name: product.name, exact: true }) });
    await expect.poll(() => scaleOf(line.locator('.cart-image img'))).toBe(result.framingScale);
    await expect(line.locator('.cart-image')).toHaveCSS('overflow', 'hidden');
    result.cart = true;
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: 'artifacts/product-photos/framing-cart-mobile.png', fullPage: true });

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.emulateMedia({ reducedMotion: 'no-preference' });
  await page.goto(base + '/en/products?category=ipad', { waitUntil: 'networkidle' });
  const framed = page.locator('.product-card').filter({ has: page.locator('img[style*="2.4"]') }).first();
  await framed.locator('.product-art').hover();
  await expect.poll(() => scaleOf(framed.locator('img'))).toBeCloseTo(2.4 * 1.035, 4);
  await page.mouse.move(0, 0);
  await expect.poll(() => scaleOf(framed.locator('img'))).toBe(2.4);
  await page.screenshot({ path: 'artifacts/product-photos/framing-ipad-desktop.png', fullPage: true });
  results.push({ hoverComposesWithFraming: true, reducedMotionKeepsStaticFraming: true, guestCartOnly: true });
  assert.deepEqual(writes, []);
  assert.deepEqual(errors, []);
  console.log(JSON.stringify({ passed: results.length, writes, errors }));
} finally {
  await writeFile('artifacts/product-photos/framing-results.json', JSON.stringify({ at: new Date().toISOString(), results, writes, errors }, null, 2));
  await browser.close();
}
