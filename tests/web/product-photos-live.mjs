import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { chromium, expect, request } from '@playwright/test';
import { base } from './live-api.mjs';

// Read-only checks against the real catalogue/API and locally served image bytes.
const photos = JSON.parse(await readFile('data/product-photos.json', 'utf8'));
const allImages = photos.flatMap(photo => [photo, ...(photo.galleries ?? [])]);
const http = await request.newContext({ baseURL: base });
const results = [], errors = [];
let browser;
// Names/options are now localized and technical content is imported separately.
// Preserve the original photo-era commercial snapshot while testing those additions in specifications-live.mjs.
const withoutPoster = ({ poster, photos, specs, technicalSpecs, name, variants, ...product }) => ({
  ...product,
  variants: variants.map(({ optionLabels, ...variant }) => variant),
  compatibleProducts: [...(product.compatibleProducts ?? [])].sort(),
});
async function loadCardImages(page) {
  for (const image of await page.locator('.product-card img').all()) {
    await image.scrollIntoViewIfNeeded();
    await expect.poll(() => image.evaluate(node => node.complete && node.naturalWidth > 0)).toBe(true);
  }
  await page.evaluate(() => window.scrollTo(0, 0));
}
try {
  await mkdir('artifacts/product-photos', { recursive: true });
  const response = await http.get('/api/catalogue?locale=vi');
  assert.equal(response.status(), 200);
  const catalogue = await response.json();
  assert.equal(catalogue.products.length, photos.length);
  const beforeFile = process.env.BOTZONE_CATALOGUE_BEFORE;
  if (beforeFile) {
    const before = JSON.parse(await readFile(beforeFile, 'utf8'));
    assert.deepEqual(catalogue.products.map(withoutPoster), before.products.map(withoutPoster));
    const { products: currentProducts, ...currentSettings } = catalogue;
    const { products: previousProducts, ...previousSettings } = before;
    assert.deepEqual(currentSettings, previousSettings);
    results.push({ check: 'Catalogue IDs, raw variants, prices, stock, descriptions and compatibility preserved since photo import', passed: true });
  }
  for (const photo of photos) {
    const product = catalogue.products.find(p => p.modelCode === photo.modelCode);
    assert.ok(product, photo.modelCode);
    assert.equal(product.poster, photo.path, photo.modelCode);
    const detail = await http.get(`/api/catalogue/${product.slug}?locale=en`);
    assert.equal(detail.status(), 200);
    const english = (await detail.json()).product;
    assert.equal(english.poster, photo.path);
    assert.deepEqual(english.photos, [photo, ...(photo.galleries ?? [])].map(image => ({ path: image.path, alt: image.altEn, variantColors: image.variantColors, framingScale: image.framingScale ?? 1 })));
    for (const image of [photo, ...(photo.galleries ?? [])]) {
      const asset = await http.get(image.path);
      assert.equal(asset.status(), 200, image.path);
      assert.match(asset.headers()['content-type'], /^image\/(jpeg|png|webp)/);
      assert.equal(createHash('sha256').update(await asset.body()).digest('hex'), image.sha256.toLowerCase());
    }
    results.push({ modelCode: photo.modelCode, path: photo.path, passed: true });
  }
  browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(base + '/vi/products', { waitUntil: 'networkidle' });
  await expect(page.locator('.product-card').first()).toBeVisible();
  const decoded = await page.evaluate(async entries => Promise.all(entries.map(async photo => {
    const img = new Image();
    img.src = photo.path;
    await img.decode();
    return { path: photo.path, width: img.naturalWidth, height: img.naturalHeight };
  })), allImages);
  for (const image of decoded) {
    const source = allImages.find(photo => photo.path === image.path);
    assert.equal(image.width, source.width, image.path);
    assert.equal(image.height, source.height, image.path);
  }
  await loadCardImages(page);
  await page.screenshot({ path: 'artifacts/product-photos/catalogue-desktop.png', fullPage: true });
  await page.goto(base + '/vi/products?category=accessories&q=AirTag', { waitUntil: 'networkidle' });
  await expect(page.locator('.product-card')).toHaveCount(2);
  await loadCardImages(page);
  await page.screenshot({ path: 'artifacts/product-photos/airtag-generations.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + '/vi/products', { waitUntil: 'networkidle' });
  await expect(page.locator('.product-card').first()).toBeVisible();
  await loadCardImages(page);
  await page.screenshot({ path: 'artifacts/product-photos/catalogue-mobile.png', fullPage: true });
  for (const code of ['P18MAX', 'DUO']) {
    const product = catalogue.products.find(item => item.modelCode === code);
    const record = photos.find(item => item.modelCode === code);
    await page.goto(base + `/vi/products/${product.slug}`, { waitUntil: 'networkidle' });
    for (const variant of product.variants) {
      const image = [record, ...(record.galleries ?? [])].find(photo => photo.variantColors.includes(variant.options.color));
      await page.locator('.variant-option').filter({ hasText: variant.options.color }).click();
      await expect(page.locator('.detail-gallery img')).toHaveAttribute('src', image.path);
    }
    results.push({ check: `${code} changes its real photo with the selected color`, passed: true });
  }
  assert.deepEqual(errors, []);
  results.push({ check: 'Every image decodes in Edge at its declared dimensions; desktop/mobile and AirTag screenshots captured', passed: true });
  console.log(JSON.stringify({ passed: results.length, models: photos.length, photos: allImages.length }));
} catch (error) {
  results.push({ passed: false, error: error.message });
  throw error;
} finally {
  await mkdir('artifacts/product-photos', { recursive: true });
  await writeFile('artifacts/product-photos/live-results.json', JSON.stringify({ at: new Date().toISOString(), results, errors }, null, 2));
  await browser?.close();
  await http.dispose();
}
