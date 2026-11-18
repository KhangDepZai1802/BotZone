import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/web/scene-controls-20261117';
await mkdir(out, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
let context, page;
const snapshot = () => page.evaluate(() => Object.entries(window.__BOTZONE_3D__ || {}).find(([key]) => key.endsWith(':explore'))?.[1]);
try {
  for (const locale of ['vi', 'en']) for (const [width, height] of [[390, 844], [430, 932], [768, 1024], [1024, 768], [1366, 768], [1440, 900]]) {
    context = await browser.newContext({ viewport: { width, height }, reducedMotion: 'reduce', hasTouch: width < 768, isMobile: width < 768 });
    page = await context.newPage(); page.setDefaultTimeout(30000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
    await page.goto(`${base}/${locale}?debug3d=1`, { waitUntil: 'networkidle' });
    for (const kind of width === 390 || width === 1366 ? ['duo', 'promax'] : ['duo']) {
      const scene = page.locator('.scene-container').filter({ has: page.locator(`img.scene-poster[src*="${kind === 'duo' ? 'iphone-duo' : 'iphone-18-pro-max'}"]`) }).first();
      const open = scene.getByTestId('explore-3d'); await open.click();
      const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible();
      await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready', 'true', { timeout: 60000 });
      await expect(page.locator('canvas')).toHaveCount(1);
      const rotation = dialog.getByRole('slider', { name: locale === 'vi' ? 'Xoay bằng bàn phím' : 'Keyboard rotation', exact: true });
      const geometry = await dialog.evaluate(element => {
        const box = element.getBoundingClientRect(), controls = element.querySelector('.explore-controls');
        const ranges = [...controls.querySelectorAll('input[type="range"]')].map(input => {
          const label = input.closest('label'), heading = label.firstElementChild;
          const text = heading.querySelector('span').getBoundingClientRect(), value = heading.querySelector('output').getBoundingClientRect(), slider = input.getBoundingClientRect();
          return { headingTop: text.top, outputTop: value.top, headingHeight: text.height, outputHeight: value.height, center: slider.top + slider.height / 2, height: slider.height, value: heading.querySelector('output').textContent };
        });
        const actions = [...controls.querySelectorAll('button')].filter(button => !button.classList.contains('swatch')).map(button => {
          const rect = button.getBoundingClientRect(); return { text: button.textContent, center: rect.top + rect.height / 2, height: rect.height, visible: rect.left >= box.left && rect.right <= box.right + 1 && rect.top >= box.top && rect.bottom <= box.bottom + 1 };
        });
        return { width: box.width, height: box.height, top: box.top, bottom: box.bottom, clientHeight: element.clientHeight, scrollHeight: element.scrollHeight, viewportHeight: innerHeight, ranges, actions, canvasHeight: element.querySelector('.explore-canvas').getBoundingClientRect().height, documentWidth: document.documentElement.scrollWidth, viewportWidth: innerWidth };
      });
      assert.ok(geometry.documentWidth <= width, 'No page horizontal overflow');
      assert.ok(geometry.scrollHeight <= geometry.clientHeight + 1, 'Dialog has no internal vertical scrolling');
      assert.ok(geometry.top >= 0 && geometry.bottom <= height, 'Dialog fits the viewport');
      assert.ok(geometry.canvasHeight >= 220, 'Model retains a useful viewing area');
      assert.ok(geometry.actions.every(button => button.visible && button.height >= 44), 'Every action is visible and has a 44px target');
      for (const range of geometry.ranges) assert.ok(Math.abs(range.headingTop - range.outputTop) <= 1 && range.height >= 44 && range.headingHeight <= 19 && range.outputHeight <= 19, 'Label and value share one line with a 44px slider');
      if (kind === 'duo') assert.ok(Math.abs(geometry.ranges[0].center - geometry.ranges[1].center) <= 1, 'Both slider rails align');
      if (width >= 1000) assert.ok(geometry.actions.every(button => Math.abs(button.center - geometry.ranges[0].center) <= 1), 'Desktop buttons align with slider rails');
      await rotation.focus(); await rotation.press('ArrowRight'); await expect(rotation).toHaveValue('1');
      await expect(rotation.locator('..').locator('output')).toHaveText('1°');
      if (kind === 'duo') {
        const fold = dialog.getByTestId('duo-fold'); await fold.fill('25');
        await expect(fold.locator('..').locator('output')).toHaveText('25%');
        await expect.poll(async () => (await snapshot())?.clip?.time).toBeCloseTo(.5, 4);
        await dialog.getByTestId('duo-side-view').click(); await expect(dialog.getByTestId('duo-side-view')).toHaveAttribute('data-view', 'outer');
      }
      await dialog.locator('.swatches button').first().click(); await expect(dialog.locator('.swatches button').first()).toHaveAttribute('aria-pressed', 'true');
      await dialog.getByRole('button', { name: locale === 'vi' ? 'Đặt lại góc nhìn' : 'Reset view', exact: true }).click();
      await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready', 'true', { timeout: 60000 });
      await expect(rotation).toHaveValue('0'); await expect(dialog.getByTestId('color-original')).toHaveAttribute('aria-pressed', 'true');
      if (kind === 'duo') { await expect(dialog.getByTestId('duo-fold')).toHaveValue('100'); await expect(dialog.getByTestId('duo-side-view')).toHaveAttribute('data-view', 'inner'); }
      await page.waitForTimeout(150);
      const before = await snapshot(), radius = state => Math.hypot(...state.camera.position);
      const canvas = await dialog.locator('canvas').boundingBox(); await page.mouse.move(canvas.x + canvas.width / 2, canvas.y + canvas.height / 2); await page.mouse.wheel(0, -600);
      await page.waitForTimeout(150); assert.ok(Math.abs(radius(await snapshot()) - radius(before)) < 1e-6, 'Wheel zoom stays disabled');
      await dialog.screenshot({ path: `${out}/${kind}-${locale}-${width}.png` });
      await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0); await expect(open).toBeFocused();
      results.push({ kind, locale, width, height, ...geometry, interactions: 'keyboard rotation/fold/side/color/reset/Escape/focus/zoom-off pass' });
      console.log(`PASS ${kind} ${locale} ${width}`);
    }
    await context.close(); context = undefined; page = undefined;
  }
  assert.deepEqual(errors, []); assert.deepEqual(writes, []);
  console.log(JSON.stringify({ passed: results.length, errors, writes }));
} catch (error) {
  results.push({ failed: error.message });
  await page?.screenshot({ path: `${out}/failure.png`, fullPage: true }).catch(() => {});
  throw error;
} finally {
  await writeFile(`${out}/results.json`, JSON.stringify({ at: new Date().toISOString(), base, results, errors, writes, limitation: 'Mobile sizes and touch are browser emulation, not physical devices.' }, null, 2));
  await context?.close(); await browser.close();
}
