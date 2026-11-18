import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/3d-v003/hero-pointer';
await mkdir(out, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { at: new Date().toISOString(), base, browser: await browser.version(), results: [], errors: [], writes: [] };
const observe = page => {
  page.on('pageerror', error => report.errors.push(error.message));
  page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) report.writes.push(request.url()); });
};
const snapshot = (page, mode = 'inline') => page.evaluate(mode => Object.entries(window.__BOTZONE_3D__ || {}).find(([key]) => key.endsWith(':' + mode))?.[1], mode);
const ready = scene => expect(scene.locator('.scene-container')).toHaveAttribute('data-ready', 'true', { timeout: 60000 });
async function settled(page, mode = 'inline') {
  await expect.poll(async () => {
    const state = await snapshot(page, mode);
    return state?.presentation ? Math.max(...['x', 'y'].map(axis => Math.abs(state.presentation.pointerTarget[axis] - state.presentation.pointerSmoothed[axis]))) : 10;
  }).toBeLessThan(.015);
  return snapshot(page, mode);
}
function projectedAabbUpperBound(state) {
  const { min, max } = state.worldBounds, [width, height] = state.renderer.viewport;
  const tangent = Math.tan(15 * Math.PI / 180), cameraZ = state.camera.position[2];
  const points = [];
  for (const x of [min[0], max[0]]) for (const y of [min[1], max[1]]) for (const z of [min[2], max[2]]) {
    points.push([x / ((cameraZ - z) * tangent * width / height), y / ((cameraZ - z) * tangent)]);
  }
  const maxNdc = Math.max(...points.flat().map(Math.abs));
  return maxNdc;
}
async function canvasFrame(page, scene, name) {
  // A world-axis bounding box projects extra empty corners when the phone
  // rotates. Check the rendered silhouette instead of treating that box as pixels.
  const png = await scene.locator('canvas').screenshot({ path: `${out}/${name}-canvas.png`, style: '.scene-controls { visibility: hidden !important; }' });
  const pixels = await page.evaluate(async source => {
    const image = new Image(); image.src = source; await image.decode();
    const canvas = document.createElement('canvas'); canvas.width = image.width; canvas.height = image.height;
    const ctx = canvas.getContext('2d'); ctx.drawImage(image, 0, 0);
    const data = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    const bg = [data[0], data[1], data[2]];
    let foreground = 0, atEdge = 0, minX = canvas.width, minY = canvas.height, maxX = 0, maxY = 0;
    for (let y = 0; y < canvas.height; y += 2) for (let x = 0; x < canvas.width; x += 2) {
      const at = (y * canvas.width + x) * 4;
      if (Math.max(...bg.map((channel, i) => Math.abs(data[at + i] - channel))) < 35) continue;
      foreground++; minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y);
      if (x < 6 || y < 6 || x > canvas.width - 7 || y > canvas.height - 7) atEdge++;
    }
    return { foreground, atEdge, bounds: [minX, minY, maxX, maxY], size: [canvas.width, canvas.height] };
  }, 'data:image/png;base64,' + png.toString('base64'));
  assert.ok(pixels.foreground > 100, 'Actual phone pixels are rendered');
  assert.equal(pixels.atEdge, 0, 'Rendered phone silhouette stays clear of the canvas edges');
  return pixels;
}
try {
  for (const [width, height] of [[1366, 768], [1920, 1080]]) {
    const context = await browser.newContext({ viewport: { width, height }, reducedMotion: 'no-preference',
      ...(width === 1366 ? { recordVideo: { dir: out + '/recording', size: { width, height } } } : {}) });
    const page = await context.newPage(); observe(page);
    try {
      await page.goto(`${base}/vi?debug3d=1`, { waitUntil: 'networkidle' });
      for (const selector of ['.hero', '.duo-section']) {
        const scene = page.locator(selector);
        if (selector !== '.hero') await scene.scrollIntoViewIfNeeded();
        await ready(scene);
        const bounds = await scene.boundingBox();
        const top = Math.max(110, bounds.y + 35), bottom = Math.min(height - 35, bounds.y + bounds.height - 35);
        const leftPoint = { x: bounds.x + bounds.width * .08, y: top + (bottom - top) * .15 };
        const rightPoint = { x: bounds.x + bounds.width * .92, y: top + (bottom - top) * .85 };
        await page.mouse.move(leftPoint.x, leftPoint.y, { steps: 12 });
        await expect.poll(async () => (await snapshot(page))?.presentation?.pointerTarget.x).toBeLessThan(-.8);
        const left = await settled(page), leftBounds = projectedAabbUpperBound(left);
        const leftPixels = await canvasFrame(page, scene, `${selector.slice(1)}-${width}-left`);
        await page.screenshot({ path: `${out}/${selector.slice(1)}-${width}-left.png` });
        await page.mouse.move(rightPoint.x, rightPoint.y);
        await expect.poll(async () => (await snapshot(page))?.presentation?.pointerTarget.x).toBeGreaterThan(.8);
        const intermediate = await snapshot(page);
        assert.ok(Math.abs(intermediate.presentation.pointerSmoothed.x - intermediate.presentation.pointerTarget.x) > .05, 'Motion eases into a new target instead of snapping');
        const right = await settled(page), rightBounds = projectedAabbUpperBound(right);
        const rightPixels = await canvasFrame(page, scene, `${selector.slice(1)}-${width}-right`);
        const yawSpan = Math.abs(right.presentation.rotation[1] - left.presentation.rotation[1]);
        const pitchSpan = Math.abs(right.presentation.rotation[0] - left.presentation.rotation[0]);
        assert.ok(yawSpan > .23 && pitchSpan > .07, 'Yaw and pitch are clearly stronger than the former small tilt');
        assert.ok(right.presentation.position[0] - left.presentation.position[0] > .15, 'Position follows the pointer as well as rotation');
        assert.deepEqual(right.camera, left.camera, 'Pointer motion does not zoom or move the camera');
        assert.deepEqual(right.screens, left.screens, 'Authored screen materials and UVs are unchanged');
        if (left.clip) assert.deepEqual(right.clip, left.clip, 'Duo keeps its complete authored fold pose');
        await page.screenshot({ path: `${out}/${selector.slice(1)}-${width}-right.png` });
        await page.mouse.move(50, 20, { steps: 12 });
        await expect.poll(async () => (await snapshot(page))?.presentation?.pointerTarget.x).toBe(0);
        const returned = await settled(page);
        assert.ok(Math.abs(returned.presentation.position[0]) < .002, 'Leaving the hero returns the phone smoothly to center');
        report.results.push({ name: `${selector} ${width} pointer sweep`, passed: true, yawSpanDegrees: yawSpan * 180 / Math.PI,
          pitchSpanDegrees: pitchSpan * 180 / Math.PI, projectedAabbUpperBound: Math.max(leftBounds, rightBounds), leftPixels, rightPixels,
          intermediate: intermediate.presentation, returned: returned.presentation, camera: returned.camera, fps: right.renderer.fps });
      }
      if (width === 1366) {
        await page.locator('.hero').scrollIntoViewIfNeeded(); await ready(page.locator('.hero'));
        await page.locator('.hero').getByTestId('explore-3d').click();
        const dialog = page.getByRole('dialog');
        await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready', 'true', { timeout: 60000 });
        const before = await snapshot(page, 'explore'), box = await dialog.locator('canvas').boundingBox();
        await page.mouse.move(box.x + box.width * .4, box.y + box.height * .4);
        await page.mouse.wheel(0, -600); await page.waitForTimeout(200);
        const afterWheel = await snapshot(page, 'explore');
        assert.deepEqual(afterWheel.camera.position, before.camera.position, 'Exploration zoom remains disabled');
        await page.mouse.down(); await page.mouse.move(box.x + box.width * .65, box.y + box.height * .5, { steps: 12 }); await page.mouse.up();
        await page.waitForTimeout(450);
        const afterDrag = await snapshot(page, 'explore');
        assert.notDeepEqual(afterDrag.camera.quaternion, before.camera.quaternion, 'Exploration drag still rotates');
        assert.deepEqual(afterDrag.presentation.pointerTarget, { x: 0, y: 0 }, 'Hero pointer movement is suspended while exploring');
        await page.keyboard.press('Escape');
        await expect(dialog).not.toBeVisible(); await ready(page.locator('.hero'));
        report.results.push({ name: 'Explore remains isolated; drag works and zoom stays disabled', passed: true });
      }
    } finally {
      const video = page.video(); await context.close();
      if (video) await video.saveAs(out + '/pointer-interaction.webm');
    }
  }
  const touch = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true, deviceScaleFactor: 2 });
  try {
    const page = await touch.newPage(); observe(page);
    await page.goto(`${base}/vi?debug3d=1`, { waitUntil: 'networkidle' });
    await page.locator('.hero .scene-container').scrollIntoViewIfNeeded(); await ready(page.locator('.hero'));
    const box = await page.locator('.hero .scene-container').boundingBox(), cdp = await touch.newCDPSession(page);
    const x = box.x + box.width * .5, y = Math.min(700, box.y + box.height * .65), before = await page.evaluate(() => scrollY);
    await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
    for (let i = 1; i <= 8; i++) { await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x, y: y - i * 20 }] }); await page.waitForTimeout(20); }
    const whileTouch = await snapshot(page);
    if (whileTouch) assert.deepEqual(whileTouch.presentation.pointerTarget, { x: 0, y: 0 }, 'Touch never drives mouse parallax');
    await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
    await expect.poll(() => page.evaluate(() => scrollY)).toBeGreaterThan(before + 30);
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No mobile overflow');
    report.results.push({ name: '390px touch scroll remains natural; mouse motion is not applied', passed: true });
  } finally { await touch.close(); }
  const reduced = await browser.newContext({ viewport: { width: 1440, height: 900 }, reducedMotion: 'reduce' });
  try {
    const page = await reduced.newPage(); observe(page);
    await page.goto(`${base}/en?debug3d=1`, { waitUntil: 'networkidle' });
    await page.mouse.move(200, 300); await page.mouse.move(1250, 650);
    await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1');
    await expect(page.locator('.hero canvas')).toHaveCount(0);
    await expect(page.locator('.hero').getByRole('link', { name: 'Discover now', exact: true })).toBeVisible();
    report.results.push({ name: 'Reduced motion keeps the stable poster and working product links', passed: true });
  } finally { await reduced.close(); }
  assert.deepEqual(report.errors, []); assert.deepEqual(report.writes, []);
} catch (error) { report.failure = error.stack; process.exitCode = 1; }
finally {
  await browser.close();
  await writeFile(out + '/results.json', JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ passed: report.results.length, failure: report.failure, errors: report.errors, writes: report.writes }, null, 2));
}
