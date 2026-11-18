import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { basename, join, resolve } from 'node:path';
import { mkdir, readFile, writeFile } from 'node:fs/promises';

// Read-only browser acceptance: no login, commands, cart, CMS or business mutations.
const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const root = resolve('.');
const output = resolve(process.env.BOTZONE_3D_ARTIFACTS || 'artifacts/3d-v003');
await mkdir(output, { recursive: true });
const report = { at: new Date().toISOString(), base, browser: 'Installed Microsoft Edge, headless',
  scope: 'Actual WebGL on this Windows host. Mobile viewports/touch are browser emulation, not physical mobile hardware.',
  results: [], failures: [], assets: [], limitations: ['No physical iOS/Android hardware measured.', 'Observed host frame timings are not a device FPS guarantee.'] };
const sha = data => createHash('sha256').update(data).digest('hex');
const manifest = JSON.parse(await readFile(join(root, 'docs/3d/v003/source/asset-manifest.json'), 'utf8'));
const expectedFiles = new Map();
async function saveReport() { await writeFile(join(output, 'results.json'), JSON.stringify(report, null, 2)); }
async function run(name, action) {
  if (process.env.BOTZONE_3D_CASE && !name.startsWith('Local ') && !new RegExp(process.env.BOTZONE_3D_CASE, 'i').test(name)) return;
  const started = Date.now();
  try { const detail = await action(); report.results.push({ name, passed: true, elapsedMs: Date.now() - started, ...detail }); console.log('PASS ' + name); }
  catch (error) { report.failures.push({ name, message: error.message, stack: error.stack }); console.error('FAIL ' + name + ': ' + error.message); }
  await saveReport();
}
function parseGlb(bytes) {
  assert.equal(bytes.readUInt32LE(0), 0x46546c67); assert.equal(bytes.readUInt32LE(4), 2); assert.equal(bytes.readUInt32LE(8), bytes.length);
  const length = bytes.readUInt32LE(12); assert.equal(bytes.readUInt32LE(16), 0x4e4f534a);
  return JSON.parse(bytes.subarray(20, 20 + length).toString('utf8'));
}
function uvBounds(bytes, gltf) {
  const binStart = 20 + bytes.readUInt32LE(12) + 8, result = {};
  for (const node of gltf.nodes.filter(n => /^Screen/.test(n.name || '') && n.mesh !== undefined)) {
    for (const primitive of gltf.meshes[node.mesh].primitives) {
      const index = primitive.attributes.TEXCOORD_0; if (index === undefined) continue;
      const accessor = gltf.accessors[index], view = gltf.bufferViews[accessor.bufferView];
      assert.equal(accessor.componentType, 5126); assert.equal(accessor.type, 'VEC2');
      const start = binStart + (view.byteOffset || 0) + (accessor.byteOffset || 0), stride = view.byteStride || 8;
      const min = [Infinity, Infinity], max = [-Infinity, -Infinity];
      for (let i = 0; i < accessor.count; i++) for (let axis = 0; axis < 2; axis++) {
        const value = bytes.readFloatLE(start + i * stride + axis * 4); min[axis] = Math.min(min[axis], value); max[axis] = Math.max(max[axis], value);
      }
      result[node.name] = { min, max, count: accessor.count };
    }
  }
  return result;
}
await run('Local v003 hashes and full animation/material asset contract', async () => {
  assert.equal(manifest.version, 'v003');
  for (const model of manifest.assets) {
    for (const [variant, asset] of Object.entries(model.files)) {
      const file = basename(asset.path), bytes = await readFile(join(root, 'public/models/v003', file));
      assert.equal(bytes.length, asset.bytes); assert.equal(sha(bytes), asset.sha256);
      const gltf = parseGlb(bytes);
      assert.ok((gltf.buffers || []).every(x => !x.uri), 'Embedded buffers only');
      assert.ok((gltf.images || []).every(x => !x.uri && x.bufferView !== undefined), 'Embedded textures only');
      const screens = gltf.materials.filter(x => ['MAT_ScreenOuter', 'MAT_ScreenInner'].includes(x.name));
      for (const material of screens) {
        assert.deepEqual(material.pbrMetallicRoughness.baseColorFactor.slice(0, 3), [0, 0, 0]);
        assert.equal(material.pbrMetallicRoughness.baseColorTexture, undefined); assert.ok(material.emissiveTexture);
      }
      let animation;
      if (model.id === 'iphone-duo') {
        animation = gltf.animations.find(x => x.name === 'Duo_OpenClose'); assert.ok(animation);
        const channels = animation.channels.map(c => ({ node: gltf.nodes[c.target.node].name, path: c.target.path, interpolation: animation.samplers[c.sampler].interpolation }));
        assert.deepEqual(channels.map(x => x.node + ':' + x.path).sort(), ['ScreenOuter:scale', 'HingePivot:rotation', 'HingeSpine:translation', 'ScreenInnerFlex:weights'].sort());
        assert.equal(channels.find(x => x.node === 'ScreenOuter').interpolation, 'STEP');
        assert.ok(gltf.nodes.some(x => x.name === 'ScreenOuterGlass'));
        assert.equal(Math.max(...animation.samplers.map(x => gltf.accessors[x.input].max[0])), 2);
        assert.equal(screens.length, 2);
        animation = channels;
      }
      const screenUvBounds = uvBounds(bytes, gltf);
      if (model.id === 'iphone-duo') for (const name of ['ScreenInnerFixed', 'ScreenInnerFlex', 'ScreenInnerMoving']) {
        assert.ok(screenUvBounds[name]); assert.ok(screenUvBounds[name].max[0] - screenUvBounds[name].min[0] < .6, 'Inner pieces use portions of a shared atlas, not a repeated full wallpaper');
      }
      expectedFiles.set(file, asset.sha256); report.assets.push({ file, variant, bytes: bytes.length, sha256: sha(bytes), animation, screenUvBounds, source: asset.path });
    }
    const poster = await readFile(join(root, 'public/models/v003', basename(model.poster.path)));
    assert.equal(sha(poster), model.poster.sha256);
  }
  return { files: report.assets.length, posters: manifest.assets.length, note: 'Static verification complements, and does not replace, the browser cases below.' };
});

if (process.argv.includes('--assets-only')) { process.exitCode = report.failures.length ? 1 : 0; }
else {
  const browser = await chromium.launch({ channel: 'msedge', headless: !process.argv.includes('--headed') });
  report.browserVersion = browser.version();
  const allContexts = new Set();
  async function context(options = {}) { const ctx = await browser.newContext(options); allContexts.add(ctx); return ctx; }
  async function close(ctx) { await ctx.close(); allContexts.delete(ctx); }
  const diagnostics = page => page.evaluate(() => {
    const source = window.__BOTZONE_3D__;
    return typeof source?.snapshot === 'function' ? source.snapshot() : source || null;
  });
  const glbPath = url => new URL(url).pathname;
  async function instrument(page) {
    const observation = { pageErrors: [], consoleErrors: [], modelRequests: [], failedRequests: [], httpErrors: [], servedHashes: [], canceledBodyReads: [] }, pending = [];
    page.setDefaultTimeout(30000);
    page.on('pageerror', error => observation.pageErrors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') observation.consoleErrors.push(message.text()); });
    page.on('request', request => { if (/\.glb(?:\?|$)/.test(request.url())) observation.modelRequests.push({ url: request.url(), at: Date.now() }); });
    page.on('requestfailed', request => observation.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
    page.on('response', response => {
      if (response.status() >= 400) observation.httpErrors.push({ url: response.url(), status: response.status() });
      if (/\.glb(?:\?|$)/.test(response.url()) && response.ok()) pending.push((async () => {
        let timer, bytes;
        try { bytes = await Promise.race([response.body(), new Promise((_, reject) => { timer = setTimeout(() => reject(new Error('Body observation timed out after model request was interrupted')), 5000); })]); }
        catch (error) { observation.canceledBodyReads.push({ url: response.url(), error: error.message, failure: response.request().failure()?.errorText }); return; }
        finally { clearTimeout(timer); }
        const file = basename(glbPath(response.url()));
        const hash = sha(bytes); assert.equal(hash, expectedFiles.get(file), 'Served GLB hash ' + file);
        observation.servedHashes.push({ file, sha256: hash, bytes: bytes.length });
      })().then(() => null, error => error));
    });
    return { observation, finish: async () => { const results = await Promise.all(pending); for (const error of results) if (error) throw error;
      observation.modelTimings = await page.evaluate(() => performance.getEntriesByType('resource').filter(x => /\.glb(?:\?|$)/.test(x.name)).map(x => ({ url: x.name, durationMs: x.duration, transferSize: x.transferSize, encodedBodySize: x.encodedBodySize, decodedBodySize: x.decodedBodySize })));
      return observation; } };
  }
  async function settle(page, ms = 200) { await page.waitForTimeout(ms); }
  async function setRange(locator, value) { await locator.fill(String(value)); await locator.dispatchEvent('input'); await locator.dispatchEvent('change'); }
  async function shot(page, name, locator) {
    const png = await (locator || page).screenshot({ path: join(output, name + '.png'), ...(locator ? {} : { fullPage: true }) });
    if (locator && await locator.evaluate(el => el.tagName === 'CANVAS')) {
      // Decode the compositor screenshot, not WebGL's cleared drawing buffer.
      const pixels = await page.evaluate(async source => {
        const image = new Image(); image.src = source; await image.decode();
        const canvas = document.createElement('canvas'); canvas.width = image.width; canvas.height = image.height;
        const ctx = canvas.getContext('2d'); ctx.drawImage(image, 0, 0); const rgba = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
        let count = 0, sum = 0, sumSquare = 0, dark = 0; const colors = new Set();
        for (let i = 0; i < rgba.length; i += 16) {
          const luminance = .2126 * rgba[i] + .7152 * rgba[i + 1] + .0722 * rgba[i + 2];
          count++; sum += luminance; sumSquare += luminance * luminance; if (luminance < 120) dark++;
          colors.add(`${rgba[i] >> 4},${rgba[i + 1] >> 4},${rgba[i + 2] >> 4}`);
        }
        return { width: image.width, height: image.height, luminanceStdDev: Math.sqrt(sumSquare / count - (sum / count) ** 2), darkPixelFraction: dark / count, quantizedColors: colors.size };
      }, 'data:image/png;base64,' + png.toString('base64'));
      report.captureChecks ??= []; report.captureChecks.push({ name, ...pixels });
      assert.ok(pixels.luminanceStdDev > 10 && pixels.darkPixelFraction > .015 && pixels.quantizedColors > 25, 'Actual rendered model pixels must be present in ' + name + ': ' + JSON.stringify(pixels));
    }
  }
  async function ready(scene) { await expect(scene.locator('.scene-poster')).toHaveCSS('opacity', '0', { timeout: 60000 }); }
  async function openExplore(page, scene, locale) {
    await scene.getByRole('button', { name: locale === 'vi' ? 'Khám phá 3D' : 'Explore in 3D', exact: true }).click();
    const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible();
    await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready', 'true', { timeout: 60000 }); return dialog;
  }
  function assertRequests(requests, variant, count) {
    const paths = requests.map(x => glbPath(x.url));
    assert.equal(paths.length, count, 'Exactly one initial request per displayed model: ' + paths.join(','));
    for (const path of paths) { assert.ok(path.includes('/v003/'), path); assert.ok(path.endsWith('-' + variant + '.glb'), path); }
    assert.equal(new Set(paths).size, count, 'No duplicate initial model fetch');
  }
  async function renderer(page) {
    return page.locator('canvas').first().evaluate(canvas => {
      const gl = canvas.getContext('webgl2') || canvas.getContext('webgl'); if (!gl) return null;
      const ext = gl.getExtension('WEBGL_debug_renderer_info');
      return { version: gl.getParameter(gl.VERSION), vendor: gl.getParameter(gl.VENDOR), renderer: gl.getParameter(gl.RENDERER), unmaskedVendor: ext ? gl.getParameter(ext.UNMASKED_VENDOR_WEBGL) : null, unmaskedRenderer: ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : null, drawingBuffer: [gl.drawingBufferWidth, gl.drawingBufferHeight], css: [canvas.clientWidth, canvas.clientHeight], hostDevicePixelRatio: devicePixelRatio };
    });
  }
  async function frameSample(page) {
    return page.evaluate(() => new Promise(resolve => {
      const timestamps = [], start = performance.now();
      function next(time) { timestamps.push(time); if (time - start < 1500) requestAnimationFrame(next); else {
        const gaps = timestamps.slice(1).map((t, i) => t - timestamps[i]).sort((a, b) => a - b);
        resolve({ observation: 'Browser requestAnimationFrame cadence, not physical-mobile FPS', sampleMs: timestamps.at(-1) - timestamps[0], callbackCount: timestamps.length, medianIntervalMs: gaps[Math.floor(gaps.length / 2)], p95IntervalMs: gaps[Math.floor(gaps.length * .95)], maxIntervalMs: Math.max(...gaps) });
      } } requestAnimationFrame(next);
    }));
  }
  async function snapshot(page, mode = 'explore') {
    const registry = await diagnostics(page); assert.ok(registry, 'Read-only ?debug3d=1 diagnostics are available');
    const entry = Object.entries(registry).find(([key]) => key.endsWith(':' + mode)); assert.ok(entry, 'Active ' + mode + ' scene diagnostics');
    return entry[1];
  }
  function screenContract(state) {
    assert.ok(state?.screens?.materials?.length, 'Actual runtime OLED materials observed');
    for (const material of state.screens.materials) {
      assert.deepEqual(material.color, [0, 0, 0]); assert.equal(material.map, null); assert.equal(material.toneMapped, false);
      assert.ok(material.emissiveMap); assert.equal(material.emissiveMap.colorSpace, 'srgb'); assert.equal(material.emissiveMap.flipY, false);
      assert.equal(material.emissiveMap.minFilter, 1008); assert.equal(material.emissiveMap.magFilter, 1006);
      assert.ok(material.emissiveMap.anisotropy >= 1 && material.emissiveMap.anisotropy <= 8);
      assert.ok(Math.abs(material.emissiveIntensity - (state.clip ? 1 : 1.1)) < 1e-6, 'Original GLB OLED emission strength');
    }
    if (state.glass) for (const material of state.glass.materials) {
      assert.equal(material.toneMapped, true); assert.equal(material.emissiveMap, null); assert.deepEqual(material.emissive, [0, 0, 0]);
    }
    const source = report.assets.find(x => x.file === basename(glbPath(state.modelUrl.startsWith('http') ? state.modelUrl : base + state.modelUrl)));
    assert.ok(source);
    for (const uv of state.screens.uvBounds) {
      const expected = source.screenUvBounds[uv.node]; assert.ok(expected); assert.equal(uv.count, expected.count);
      assert.deepEqual(uv.bounds, [...expected.min, ...expected.max], 'Runtime UV bounds match the unmodified GLB for ' + uv.node);
    }
    assert.ok(state.renderer.dpr <= (state.mobile ? 1.5 : 2));
  }
  async function foldPose(page, dialog, percent) {
    await setRange(dialog.getByTestId('duo-fold'), percent);
    await expect.poll(async () => (await snapshot(page)).clip.time).toBeCloseTo(percent / 50, 4);
    const state = await snapshot(page); assert.ok(state.outer.scale.every(v => Math.abs(v - (percent < 100 / 60 ? 1 : 0)) < .00001), 'Outer-screen STEP scale at ' + percent + '%');
    assert.equal(state.glass.visible, true, 'Dark glass remains present'); assert.ok(state.glass.scale.every(v => v > 0), 'Dark glass not hidden by screen scale');
    return state;
  }
  async function deviceSide(dialog, side) {
    const button = dialog.getByTestId('duo-side-view');
    if (await button.getAttribute('data-view') !== side) await button.click();
    await expect(button).toHaveAttribute('data-view', side);
  }

  try {
    // Each viewport gets a new HTTP/browser cache, so initial variant selection is observable.
    for (const locale of ['vi', 'en']) for (const width of [390, 430, 1366, 1440]) {
      await run(`Responsive ${locale} ${width}px, selected v003 GLBs and lazy loading`, async () => {
        const mobile = width < 768, height = mobile ? (width === 430 ? 932 : 844) : (width === 1366 ? 768 : 900);
        const ctx = await context({ viewport: { width, height }, isMobile: mobile, hasTouch: mobile, deviceScaleFactor: mobile ? 3 : 1 });
        const page = await ctx.newPage(), monitor = await instrument(page);
        try {
          await page.goto(`${base}/${locale}?debug3d=1`, { waitUntil: 'networkidle' });
          const hero = page.locator('.hero'), duo = page.locator('.duo-section'); await ready(hero);
          await expect(page.locator('canvas')).toHaveCount(1);
          assertRequests(monitor.observation.modelRequests, mobile ? 'mobile' : 'desktop', 1);
          assert.ok(monitor.observation.modelRequests[0].url.includes('iphone-18-pro-max'));
          assert.equal(await page.locator('html').getAttribute('lang'), locale);
          assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'No horizontal overflow');
          assert.equal(await hero.locator('.scene-container').evaluate(el => getComputedStyle(el).touchAction), 'pan-y');
          const initialRenderer = await renderer(page), initialDiagnostics = await diagnostics(page); screenContract(await snapshot(page, 'inline'));
          await shot(page, `landing-${locale}-${width}`); await shot(page, `promax-canvas-${locale}-${width}`, hero.locator('canvas'));
          await duo.scrollIntoViewIfNeeded(); await ready(duo); await expect(page.locator('canvas')).toHaveCount(1);
          const afterScrollPaths = monitor.observation.modelRequests.map(x => glbPath(x.url));
          assert.equal(new Set(afterScrollPaths).size, 2, 'Only the two selected variants after scrolling');
          assert.ok(afterScrollPaths.every(path => path.includes('/v003/') && path.endsWith(`-${mobile ? 'mobile' : 'desktop'}.glb`)));
          assert.equal(afterScrollPaths.filter(path => path.includes('iphone-duo')).length, 1, 'Duo initially fetched once when revealed');
          const duoDiagnostics = await diagnostics(page); screenContract(await snapshot(page, 'inline')); await shot(page, `duo-inline-${locale}-${width}`, duo);
          const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
          assert.equal(new Set(observed.servedHashes.map(x => x.file)).size, 2, 'Both visible GLBs were independently hashed from HTTP response bodies');
          return { locale, viewport: { width, height }, mobileEmulation: mobile, initialRenderer, initialDiagnostics, duoDiagnostics, ...observed };
        } finally { await close(ctx); }
      });
    }
    await run('Laptop exploration fits all controls without scrolling', async () => {
      const layouts = [];
      for (const [width, height] of [[1024, 600], [1093, 614], [1280, 720], [1366, 768], [1440, 900]]) {
        const ctx = await context({ viewport: { width, height } }), page = await ctx.newPage(), monitor = await instrument(page);
        try {
          await page.goto(`${base}/vi?debug3d=1`, { waitUntil: 'networkidle' });
          for (const [kind, selector] of [['promax', '.hero'], ['duo', '.duo-section']]) {
            const scene = page.locator(selector); await scene.scrollIntoViewIfNeeded();
            const dialog = await openExplore(page, scene, 'vi');
            if (kind === 'duo') { await deviceSide(dialog, 'outer'); await deviceSide(dialog, 'inner'); }
            const layout = await dialog.evaluate(el => {
              const bounds = el.getBoundingClientRect(), canvas = el.querySelector('canvas').getBoundingClientRect();
              return { clientHeight: el.clientHeight, scrollHeight: el.scrollHeight, viewport: [innerWidth, innerHeight],
                dialog: { top: bounds.top, bottom: bounds.bottom }, canvas: { width: canvas.width, height: canvas.height },
                controls: [...el.querySelectorAll('button,input')].map(control => { const r = control.getBoundingClientRect(); return { text: control.getAttribute('aria-label') || control.textContent, top: r.top, bottom: r.bottom, left: r.left, right: r.right }; }) };
            });
            assert.ok(layout.scrollHeight <= layout.clientHeight + 1, `${kind} ${width}x${height}: dialog must not scroll`);
            assert.ok(layout.canvas.height >= 220, 'Model retains useful visible height');
            for (const control of layout.controls) assert.ok(control.top >= layout.dialog.top && control.bottom <= layout.dialog.bottom && control.bottom <= height && control.left >= 0 && control.right <= width, `Control is fully visible: ${control.text}`);
            await shot(page, `laptop-${kind}-${width}x${height}`, dialog);
            layouts.push({ kind, width, height, ...layout });
            await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0);
          }
          const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
        } finally { await close(ctx); }
      }
      return { layouts };
    });
    for (const [locale, width] of [['en', 1440], ['vi', 390]]) await run(`Duo full animation, hold/reverse/glass/materials and exploration ${width}px`, async () => {
      const mobile = width < 768, ctx = await context({ viewport: { width, height: mobile ? 844 : 900 }, isMobile: mobile, hasTouch: mobile, deviceScaleFactor: mobile ? 3 : 1 });
      const page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(`${base}/${locale}?debug3d=1`, { waitUntil: 'networkidle' }); const duo = page.locator('.duo-section'); await duo.scrollIntoViewIfNeeded(); await ready(duo);
        const trigger = duo.getByRole('button', { name: locale === 'vi' ? 'Khám phá 3D' : 'Explore in 3D', exact: true });
        const scrollBefore = await page.evaluate(() => scrollY), dialog = await openExplore(page, duo, locale);
        const loaded = await snapshot(page); screenContract(loaded); assert.equal(loaded.version, 'v003'); assert.equal(loaded.clip.name, 'Duo_OpenClose'); assert.equal(loaded.clip.duration, 2);
        assert.equal(loaded.clip.tracks.length, 4, 'Full original clip, including rotation, translation, morph and scale');
        const poses = {};
        for (const percent of [0, 1, 2, 25, 50, 100]) {
          if (percent === 0) await deviceSide(dialog, 'outer');
          if (percent === 25) await deviceSide(dialog, 'inner');
          poses[percent] = await foldPose(page, dialog, percent);
          if ([0, 50, 100].includes(percent)) await shot(page, `duo-${percent}-${locale}-${width}`, dialog.locator('canvas'));
        }
        const endPose = await snapshot(page); await settle(page, 1300); const heldPose = await snapshot(page);
        assert.equal(heldPose.clip.time, 2, 'Hold final pose at clip duration'); assert.deepEqual(heldPose.outer.scale, endPose.outer.scale); assert.deepEqual(heldPose.hinge, endPose.hinge);
        assert.ok(heldPose.renderer.calls > 0 && heldPose.renderer.triangles > 0, 'Actual held pose remains in the rendered view');
        for (let cycle = 0; cycle < 5; cycle++) for (const percent of [50, 0, 50, 100]) await foldPose(page, dialog, percent);
        await foldPose(page, dialog, 50); await deviceSide(dialog, 'outer'); await settle(page, 250);
        const outerView = await snapshot(page); assert.ok(outerView.outer.scale.every(v => v === 0)); assert.equal(outerView.glass.visible, true);
        await shot(page, `duo-50-outer-glass-${locale}-${width}`, dialog.locator('canvas'));
        await deviceSide(dialog, 'inner'); await foldPose(page, dialog, 100); await settle(page, 250);
        await shot(page, `duo-inner-texture-${locale}-${width}`, dialog.locator('canvas'));
        assert.notDeepEqual(poses[0].hinge, poses[100].hinge, 'Hinge changes with actual GLB animation');
        assert.ok(new Set(Object.values(poses).map(x => JSON.stringify(x.spine))).size > 1, 'Spine translation track applied');
        assert.ok(new Set(Object.values(poses).map(x => JSON.stringify(x.screens.morphInfluences))).size > 1, 'Flexible-screen morph track applied');
        const materialsBefore = (await snapshot(page)).screens;
        const colors = dialog.locator('[aria-pressed]');
        if (await colors.count() > 1) { await colors.nth(1).click(); await settle(page, 120); assert.deepEqual((await snapshot(page)).screens, materialsBefore, 'Body color does not recolor or replace screen textures/UV'); }
        const actualRenderer = await renderer(page), beforeFrames = (await snapshot(page)).renderer;
        const cadence = await frameSample(page), afterFrames = (await snapshot(page)).renderer;
        await shot(page, `duo-explore-${locale}-${width}`, dialog);
        await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0); await expect(trigger).toBeFocused();
        assert.ok(Math.abs(await page.evaluate(() => scrollY) - scrollBefore) < 3, 'Explore restores page scroll');
        await expect(page.locator('canvas')).toHaveCount(1);
        await page.locator('.benefits').scrollIntoViewIfNeeded(); await expect(page.locator('canvas')).toHaveCount(0);
        const offscreenDiagnostics = await diagnostics(page);
        assert.equal(Object.keys(offscreenDiagnostics || {}).length, 0, 'Unmount removes live scene diagnostics');
        const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
        const requestedModels = observed.modelRequests.map(x => glbPath(x.url));
        assert.ok(new Set(requestedModels).size >= 1 && new Set(requestedModels).size <= 2, 'At most the selected two v003 variants used; scrolling directly to Duo may skip Pro Max entirely');
        assert.ok(requestedModels.some(path => path.includes('iphone-duo')), 'Duo was loaded for the actual exploration');
        assert.ok(requestedModels.every(path => path.includes('/v003/') && path.endsWith(`-${mobile ? 'mobile' : 'desktop'}.glb`)));
        return { poses, heldPose, outerView, fiveReverseCycles: true, actualRenderer, cadence, renderCounterBefore: beforeFrames, renderCounterAfter: afterFrames, offscreenDiagnostics, ...observed };
      } finally { await close(ctx); }
    });
    for (const [locale, width] of [['en', 1440], ['vi', 390]]) await run(`Exploration fixed zoom, adaptive backdrop and simplified controls ${width}px`, async () => {
      const mobile = width < 768, ctx = await context({ viewport: { width, height: mobile ? 844 : 900 }, isMobile: mobile, hasTouch: mobile });
      const page = await ctx.newPage(), monitor = await instrument(page), checks = [];
      try {
        await page.goto(`${base}/${locale}?debug3d=1`, { waitUntil: 'networkidle' });
        await expect(page.getByRole('button', { name: /Pause motion|Resume motion|Dừng chuyển động|Chạy chuyển động/ })).toHaveCount(0);
        for (const [kind, selector, lightColors, darkColor] of [
          ['promax', '.hero', locale === 'en' ? ['Silver', 'Glacier'] : ['Bạc', 'Băng Thanh'], locale === 'en' ? 'Black' : 'Đen'],
          ['duo', '.duo-section', [locale === 'en' ? 'Star White' : 'Trắng Ánh Sao'], locale === 'en' ? 'Night Sky' : 'Trời Đêm'],
        ]) {
          const scene = page.locator(selector); await scene.scrollIntoViewIfNeeded(); await ready(scene);
          const dialog = await openExplore(page, scene, locale), backdrop = dialog.locator('.explore-canvas'), canvas = backdrop.locator('canvas');
          await expect(dialog.locator('[data-testid="duo-closed"], [data-testid="duo-half"], [data-testid="duo-open"]')).toHaveCount(0);
          // Read the settled controls camera, after the first model frame signals ready.
          await settle(page, 300);
          const initial = await snapshot(page), radius = state => Math.hypot(...state.camera.position);
          const bounds = await canvas.boundingBox(); assert.ok(bounds);
          await page.mouse.move(bounds.x + bounds.width / 2, bounds.y + Math.min(120, bounds.height / 2));
          for (const delta of [-850, 850]) { await page.mouse.wheel(0, delta); await settle(page, 220); assert.ok(Math.abs(radius(await snapshot(page)) - radius(initial)) < 1e-6, 'Wheel cannot change camera distance'); }
          await canvas.scrollIntoViewIfNeeded(); const dragBounds = await canvas.boundingBox();
          await page.mouse.move(dragBounds.x + dragBounds.width * .35, dragBounds.y + 110); await page.mouse.down();
          await page.mouse.move(dragBounds.x + dragBounds.width * .65, dragBounds.y + 150, { steps: 10 }); await page.mouse.up(); await settle(page, 500);
          const rotated = await snapshot(page); assert.notDeepEqual(rotated.camera.quaternion, initial.camera.quaternion, 'Drag still rotates');
          assert.ok(Math.abs(radius(rotated) - radius(initial)) < 1e-6, 'Rotation preserves framing distance');
          const fades = [];
          for (const name of lightColors) {
            await dialog.getByTestId('color-original').click(); await settle(page, 700);
            const button = dialog.getByRole('button', { name, exact: true }); await button.scrollIntoViewIfNeeded();
            // Observe actual intermediate rendered opacity, not just declared CSS duration.
            const samples = await button.evaluate(button => new Promise(resolve => {
              const background = button.closest('[role="dialog"]').querySelector('.explore-canvas'), values = [], started = performance.now();
              button.click();
              function sample() { values.push(Number(getComputedStyle(background, '::before').opacity)); if (performance.now() - started < 750) requestAnimationFrame(sample); else resolve(values); }
              requestAnimationFrame(sample);
            }));
            assert.ok(samples.some(value => value > 0 && value < 1), 'Backdrop visibly fades through intermediate opacity');
            assert.equal(samples.at(-1), 1); await expect(backdrop).toHaveAttribute('data-backdrop', 'dark');
            assert.deepEqual((await snapshot(page)).screens, initial.screens, 'Backdrop/paint preserves screen materials and UV');
            await shot(page, `${kind}-${name.replaceAll(' ', '-')}-dark-${width}`, dialog); fades.push({ name, samples });
          }
          await dialog.getByRole('button', { name: darkColor, exact: true }).click();
          await expect.poll(() => backdrop.evaluate(el => getComputedStyle(el, '::before').opacity)).toBe('0');
          if (kind === 'duo') { await foldPose(page, dialog, 50); await foldPose(page, dialog, 100); }
          await page.emulateMedia({ reducedMotion: 'reduce' });
          await dialog.getByRole('button', { name: lightColors[0], exact: true }).click();
          await expect.poll(() => backdrop.evaluate(el => getComputedStyle(el, '::before').opacity)).toBe('1');
          // The shared reduced-motion rule clamps all transitions to .01ms.
          assert.ok(await backdrop.evaluate(el => parseFloat(getComputedStyle(el, '::before').transitionDuration) <= .001));
          await page.emulateMedia({ reducedMotion: 'no-preference' });
          await dialog.getByRole('button', { name: locale === 'en' ? 'Reset view' : 'Đặt lại góc nhìn', exact: true }).click();
          await expect(backdrop).toHaveAttribute('data-ready', 'true');
          await settle(page, 300);
          const resetState = await snapshot(page);
          assert.ok(Math.abs(radius(resetState) - radius(initial)) < 1e-6, `Reset keeps original responsive framing: ${kind}, ${JSON.stringify({ initial: initial.camera, reset: resetState.camera, initialViewport: initial.renderer.viewport, resetViewport: resetState.renderer.viewport })}`);
          await expect(backdrop).toHaveAttribute('data-backdrop', 'light');
          checks.push({ kind, originalCamera: initial.camera, rotatedCamera: rotated.camera, fades, reducedMotion: true });
          await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0);
        }
        const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
        return { locale, width, checks, ...observed };
      } finally { await close(ctx); }
    });
    await run('Pro Max materials, rendered frame sample, keyboard, drag and reset', async () => {
      const ctx = await context({ viewport: { width: 1440, height: 900 } }), page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); const hero = page.locator('.hero'); await ready(hero);
        const before = await snapshot(page, 'inline'), cadence = await frameSample(page), after = await snapshot(page, 'inline');
        const observedRenderFps = after.renderer.fps;
        await expect(hero.getByRole('button', { name: /Pause motion|Resume motion/ })).toHaveCount(0);
        const dialog = await openExplore(page, hero, 'en'), initial = await snapshot(page); screenContract(initial);
        await dialog.locator('.swatches button').first().click(); await settle(page, 150);
        assert.deepEqual((await snapshot(page)).screens, initial.screens, 'Pro Max OLED remains unchanged by paint');
        const rotation = dialog.getByLabel('Keyboard rotation'), originalAngle = await rotation.inputValue(); await rotation.focus(); await rotation.press('ArrowRight');
        assert.notEqual(await rotation.inputValue(), originalAngle);
        const bounds = await dialog.locator('canvas').boundingBox(); assert.ok(bounds);
        await page.mouse.move(bounds.x + bounds.width * .35, bounds.y + bounds.height * .45); await page.mouse.down();
        await page.mouse.move(bounds.x + bounds.width * .6, bounds.y + bounds.height * .6, { steps: 12 }); await page.mouse.up(); await settle(page, 200);
        await shot(page, 'promax-explore-rotated', dialog.locator('canvas'));
        await dialog.getByRole('button', { name: 'Reset view', exact: true }).click(); await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready', 'true');
        await expect(dialog.getByTestId('color-original')).toHaveAttribute('aria-pressed', 'true'); assert.equal(await rotation.inputValue(), '0');
        await shot(page, 'promax-explore-reset', dialog); screenContract(await snapshot(page));
        const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
        return { before, after, cadence, observedRenderFps, ...observed };
      } finally { await close(ctx); }
    });
    await run('Touch scroll remains available outside explicit exploration', async () => {
      const ctx = await context({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true, deviceScaleFactor: 3 });
      const page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await ready(page.locator('.hero'));
        const canvas = page.locator('canvas'), bounds = await canvas.boundingBox(); assert.ok(bounds);
        const cdp = await ctx.newCDPSession(page), x = Math.round(Math.max(30, Math.min(360, bounds.x + bounds.width / 2))), startY = Math.round(Math.min(750, bounds.y + bounds.height * .8));
        const before = await page.evaluate(() => scrollY);
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y: startY }] });
        for (let delta = 40; delta <= 280; delta += 40) await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x, y: startY - delta }] });
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
        await expect.poll(() => page.evaluate(() => scrollY)).toBeGreaterThan(before + 50);
        const after = await page.evaluate(() => scrollY); await shot(page, 'touch-scroll-mobile');
        assert.deepEqual(monitor.observation.pageErrors, []); return { before, after, input: 'CDP touch events on emulated mobile viewport' };
      } finally { await close(ctx); }
    });
    await run('Visibility signal unmounts rendering and resumes the selected scene', async () => {
      const ctx = await context({ viewport: { width: 1440, height: 900 } }), page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await ready(page.locator('.hero'));
        const before = await snapshot(page, 'inline');
        await page.evaluate(() => {
          Object.defineProperty(document, 'hidden', { configurable: true, get: () => true });
          Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'hidden' });
          document.dispatchEvent(new Event('visibilitychange'));
        });
        await expect(page.locator('canvas')).toHaveCount(0); await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1');
        const hidden = await diagnostics(page);
        await page.evaluate(() => { delete document.hidden; delete document.visibilityState; document.dispatchEvent(new Event('visibilitychange')); });
        await ready(page.locator('.hero')); const resumed = await snapshot(page, 'inline');
        assert.deepEqual(monitor.observation.pageErrors, []);
        return { method: 'Controlled document visibility API signal in headless Edge, not an OS background-tab measurement', before, hidden, resumed };
      } finally { await close(ctx); }
    });
    await run('Reduced motion keeps posters and allows deliberate 3D exploration', async () => {
      const ctx = await context({ viewport: { width: 430, height: 932 }, isMobile: true, hasTouch: true, reducedMotion: 'reduce' });
      const page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await expect(page.locator('.hero h1')).toBeVisible();
        await expect(page.locator('canvas')).toHaveCount(0); assert.equal(monitor.observation.modelRequests.length, 0);
        await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1'); await shot(page, 'reduced-motion-posters');
        const dialog = await openExplore(page, page.locator('.hero'), 'en'); await expect(dialog.locator('canvas')).toHaveCount(1);
        assertRequests(monitor.observation.modelRequests, 'mobile', 1); await shot(page, 'reduced-motion-deliberate-explore', dialog);
        await page.keyboard.press('Escape'); await expect(page.locator('canvas')).toHaveCount(0);
        assert.deepEqual(monitor.observation.pageErrors, []); return { automaticModelRequests: 0, deliberateExplore: true, diagnostics: await diagnostics(page) };
      } finally { await close(ctx); }
    });
    await run('Loading poster remains visible until model fetch completes', async () => {
      const ctx = await context({ viewport: { width: 1440, height: 900 } }), page = await ctx.newPage(); await instrument(page);
      let release; const barrier = new Promise(resolve => { release = resolve; }); let intercepted = 0;
      await page.route('**/*.glb*', async route => { intercepted++; await barrier; await route.continue(); });
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'domcontentloaded' });
        await expect.poll(() => intercepted).toBe(1); await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1');
        await expect(page.locator('.hero .actions a').first()).toBeVisible(); await shot(page, 'loading-poster', page.locator('.hero'));
        release(); await ready(page.locator('.hero')); return { loadingPosterVisible: true, productCtaAvailable: true };
      } finally { release(); await close(ctx); }
    });
    await run('Model-load failure preserves poster, message and product links', async () => {
      const ctx = await context({ viewport: { width: 390, height: 844 } }), page = await ctx.newPage(), monitor = await instrument(page);
      await page.route('**/*.glb*', route => route.abort('failed'));
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1');
        await expect.poll(() => monitor.observation.failedRequests.filter(x => x.url.includes('.glb')).length).toBeGreaterThan(0);
        await page.locator('.hero').getByRole('button', { name: 'Explore in 3D', exact: true }).click();
        const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible(); await expect(dialog.locator('img')).toBeVisible();
        await expect(dialog.locator('.help-text')).toBeVisible(); await shot(page, 'model-failure-fallback', dialog); await page.keyboard.press('Escape');
        await expect(page.locator('.hero .actions a').first()).toBeVisible();
        return { intentionallyBlockedModel: true, ...await monitor.finish() };
      } finally { await close(ctx); }
    });
    await run('Unavailable WebGL keeps readable landing and poster exploration fallback', async () => {
      const ctx = await context({ viewport: { width: 430, height: 932 } });
      await ctx.addInitScript(() => { const get = HTMLCanvasElement.prototype.getContext; HTMLCanvasElement.prototype.getContext = function (type, ...args) { return String(type).startsWith('webgl') ? null : get.call(this, type, ...args); }; });
      const page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1');
        await page.locator('.hero').getByRole('button', { name: 'Explore in 3D', exact: true }).click(); const dialog = page.getByRole('dialog');
        await expect(dialog.locator('img')).toBeVisible(); await expect(dialog.locator('.help-text')).toBeVisible(); await expect(page.locator('canvas')).toHaveCount(0);
        await shot(page, 'webgl-unavailable-fallback', dialog); await page.keyboard.press('Escape'); await expect(page.locator('.hero h1')).toBeVisible();
        assert.deepEqual(monitor.observation.pageErrors, []); return { webglContextCreationIntentionallyDisabled: true, ...await monitor.finish() };
      } finally { await close(ctx); }
    });
    await run('WebGL context-loss event switches the live model to its poster', async () => {
      const ctx = await context({ viewport: { width: 1440, height: 900 } }), page = await ctx.newPage(), monitor = await instrument(page);
      try {
        await page.goto(base + '/en?debug3d=1', { waitUntil: 'networkidle' }); await ready(page.locator('.hero'));
        const supported = await page.locator('canvas').evaluate(canvas => {
          const extension = canvas.getContext('webgl2')?.getExtension('WEBGL_lose_context');
          if (!extension) return false; extension.loseContext(); return true;
        }); assert.equal(supported, true, 'Controlled real WebGL context-loss extension is available');
        await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity', '1'); await expect(page.locator('canvas')).toHaveCount(0);
        await expect(page.locator('.hero .actions a').first()).toBeVisible(); await shot(page, 'webgl-context-lost-poster', page.locator('.hero'));
        assert.deepEqual(monitor.observation.pageErrors, []); return { contextLoss: 'WEBGL_lose_context extension event, not a physical GPU reset', ...await monitor.finish() };
      } finally { await close(ctx); }
    });
    await run('Complete landing captures after natural scrolling loads catalogue images', async () => {
      const captures = [];
      for (const [locale, width] of [['vi', 390], ['en', 1440]]) {
        const mobile = width < 768, ctx = await context({ viewport: { width, height: mobile ? 844 : 900 }, isMobile: mobile, hasTouch: mobile, deviceScaleFactor: mobile ? 3 : 1 });
        const page = await ctx.newPage(), monitor = await instrument(page);
        try {
          await page.goto(`${base}/${locale}?debug3d=1`, { waitUntil: 'networkidle' }); await ready(page.locator('.hero'));
          for (const image of await page.locator('.collection-section .product-art img').all()) {
            await image.scrollIntoViewIfNeeded(); await expect.poll(() => image.evaluate(el => el.complete && el.naturalWidth > 0)).toBe(true);
          }
          await page.evaluate(() => window.scrollTo(0, 0)); await ready(page.locator('.hero'));
          await shot(page, `landing-complete-${locale}-${width}`); captures.push({ locale, width, loadedCatalogueImages: await page.locator('.collection-section .product-art img').count() });
          const observed = await monitor.finish(); assert.deepEqual(observed.pageErrors, []); assert.deepEqual(observed.httpErrors, []);
        } finally { await close(ctx); }
      }
      return { captures, note: 'Full-page screenshots after visiting lazy-loaded images; inactive 3D section intentionally displays its supplied poster.' };
    });
  } finally { for (const ctx of allContexts) await ctx.close(); await browser.close(); await saveReport(); }
  process.exitCode = report.failures.length ? 1 : 0;
}
