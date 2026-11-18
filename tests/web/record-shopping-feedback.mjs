import { chromium, expect } from '@playwright/test';
import { mkdir, rename, writeFile } from 'node:fs/promises';
const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/web/compare-bag';
await mkdir(out, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const writes = [], errors = [];
let context, video;
try {
  context = await browser.newContext({ viewport: { width: 1280, height: 800 }, recordVideo: { dir: out, size: { width: 1280, height: 800 } }, reducedMotion: 'no-preference' });
  const page = await context.newPage(); video = page.video();
  page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(base + '/vi/products?category=iphone', { waitUntil: 'networkidle' });
  await page.locator('.catalogue-results').scrollIntoViewIfNeeded();
  // These short holds let a viewer see the actual animation and resulting state.
  await page.waitForTimeout(550);
  const cards = page.locator('.catalogue-grid .product-card');
  await cards.nth(0).getByRole('button', { name: 'So sánh', exact: true }).click();
  await page.waitForTimeout(850);
  await cards.nth(1).getByRole('button', { name: 'So sánh', exact: true }).click();
  await page.waitForTimeout(850);
  await cards.nth(0).locator('h3 a').click();
  const add = page.getByTestId('add-to-bag'); await add.scrollIntoViewIfNeeded();
  await page.waitForTimeout(400); await add.click();
  await expect(page.getByTestId('bag-confirmation')).toBeVisible();
  await page.waitForTimeout(1250); await add.click(); await page.waitForTimeout(1600);
  await page.getByTestId('bag-confirmation').getByRole('button').first().click();
  await page.waitForTimeout(650);
  if (writes.length || errors.length) throw Error(JSON.stringify({ writes, errors }));
} finally { await context?.close(); await browser.close(); }
await rename(await video.path(), out + '/shopping-feedback.webm');
await writeFile(out + '/video-evidence.json', JSON.stringify({ at: new Date().toISOString(), base, scope: 'Real Edge production UI; two comparison selections and two guest-cart additions; normal motion; no API mutations', writes, errors }, null, 2));
console.log(out + '/shopping-feedback.webm');
