import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/web/auth-address-motion-20261117';
await mkdir(out, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
let page;
const cases = ['vi', 'en'].flatMap(locale => [390, 1366].map(width => ({ locale, width, reduced: false })));
cases.push({ locale: 'vi', width: 390, reduced: true });
try {
  for (const entry of cases) {
    const { locale, width, reduced } = entry;
    page = await browser.newPage({ viewport: { width, height: width === 390 ? 844 : 768 }, reducedMotion: reduced ? 'reduce' : 'no-preference' });
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
    await page.goto(`${base}/${locale}/register`, { waitUntil: 'networkidle' });
    const checkbox = page.locator('input[name="saveAddress"]'), panel = page.locator('#auth-address');
    const fieldset = panel.locator('fieldset'), recipient = panel.locator('#auth-address-recipient');
    await expect(checkbox).not.toBeChecked(); await expect(panel).toHaveAttribute('aria-hidden', 'true');
    await expect(fieldset).toHaveJSProperty('disabled', true); await expect(recipient).toBeDisabled(); await expect(panel).toHaveAttribute('inert', '');
    assert.equal((await panel.boundingBox()).height, 0);
    await page.locator('#auth-name').fill('Address Motion QA'); await page.locator('#auth-phone').fill('0901234567');
    await checkbox.scrollIntoViewIfNeeded(); await checkbox.focus();
    const sample = () => checkbox.evaluate((element, duration) => new Promise(resolve => {
      const panel = document.getElementById('auth-address'), content = panel.firstElementChild.firstElementChild;
      const values = [], started = performance.now(); element.click();
      const frame = now => {
        values.push({ ms: now - started, height: panel.getBoundingClientRect().height, opacity: Number(getComputedStyle(content).opacity), inert: panel.inert, disabled: panel.querySelector('fieldset').disabled });
        if (now - started < duration) requestAnimationFrame(frame); else resolve(values);
      };
      requestAnimationFrame(frame);
    }), reduced ? 90 : 430);
    const expansion = await sample(); const fullHeight = expansion.at(-1).height;
    assert.ok(fullHeight > 250); await expect(checkbox).toBeFocused(); await expect(fieldset).toHaveJSProperty('disabled', false); await expect(recipient).toBeEnabled();
    await expect(recipient).toHaveValue('Address Motion QA'); await expect(panel.locator('#auth-address-phone')).toHaveValue('0901234567');
    const intermediate = samples => samples.some(item => item.height > .5 && item.height < fullHeight - .5 && item.opacity > 0 && item.opacity < 1);
    assert.equal(intermediate(expansion), !reduced, 'Expansion follows motion preference with real height and opacity frames');
    await page.keyboard.press('Tab'); await expect(recipient).toBeFocused();
    await panel.locator('#auth-address-province').fill('Retained province');
    const name = `${locale}-${width}-${reduced ? 'reduced' : 'normal'}`;
    await panel.screenshot({ path: `${out}/${name}-open.png` });
    await recipient.focus(); const collapse = await sample();
    await expect(checkbox).toBeFocused(); await expect(fieldset).toHaveJSProperty('disabled', true); await expect(recipient).toBeDisabled(); await expect(panel).toHaveAttribute('inert', '');
    assert.equal(intermediate(collapse), !reduced, 'Collapse follows motion preference with real height and opacity frames');
    assert.equal(collapse.at(-1).height, 0); assert.equal(collapse.at(-1).opacity, 0);
    assert.ok(collapse.every(item => item.inert && item.disabled), 'Closing content becomes inert and disabled immediately');
    const collapsedSpacing = await checkbox.evaluate(element => {
      const label = element.closest('label'), option = label.parentElement;
      return { label: label.getBoundingClientRect().height, wrapper: option.getBoundingClientRect().height };
    });
    assert.ok(Math.abs(collapsedSpacing.wrapper - collapsedSpacing.label) <= 1, 'Collapsed section reserves no extra gap');
    await page.keyboard.press('Tab'); assert.equal(await page.evaluate(() => document.activeElement.type), 'submit', 'Tab skips all hidden address fields');
    const rapid = await checkbox.evaluate(element => new Promise(resolve => {
      const panel = document.getElementById('auth-address'), started = performance.now(), frames = [];
      for (let i = 0; i < 5; i++) setTimeout(() => element.click(), i * 65);
      const frame = now => { frames.push(panel.getBoundingClientRect().height); if (now - started < 720) requestAnimationFrame(frame); else resolve(frames); };
      requestAnimationFrame(frame);
    }));
    await expect(checkbox).toBeChecked(); await expect(fieldset).toHaveJSProperty('disabled', false); await expect(recipient).toBeEnabled();
    await expect(panel.locator('#auth-address-province')).toHaveValue('Retained province');
    assert.ok(rapid.every(height => height >= 0 && height <= fullHeight + 1)); assert.ok(Math.abs(rapid.at(-1) - fullHeight) <= 1);
    // Email/password deliberately remain empty, so both submissions stop in local validation.
    await page.locator('button[type="submit"]').click();
    await expect(page.locator('#auth-address-ward')).toHaveAttribute('aria-invalid', 'true');
    await checkbox.uncheck(); await expect(fieldset).toHaveJSProperty('disabled', true); await expect(recipient).toBeDisabled();
    await page.locator('button[type="submit"]').click();
    await expect(page.locator('[role="alert"] a[href^="#auth-address-"]')).toHaveCount(0);
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await expect(page.locator('#auth-password')).toHaveAttribute('minlength', '6');
    const transitions = await panel.evaluate(element => ({ grid: getComputedStyle(element).transitionDuration, content: getComputedStyle(element.firstElementChild.firstElementChild).transitionDuration }));
    if (reduced) assert.ok(transitions.grid.split(',').every(value => parseFloat(value) <= .001), 'Reduced motion is immediate, including the shared 0.01ms global clamp');
    results.push({ ...entry, fullHeight, expansion, collapse, rapid, collapsedSpacing, transitions, focus: 'checkbox retained/restored; hidden fields skipped', validation: 'open required, closed excluded; values retained' });
    console.log(`PASS ${name}`); await page.close(); page = undefined;
  }
  assert.deepEqual(errors, []); assert.deepEqual(writes, []);
  console.log(JSON.stringify({ passed: results.length, cases: results.map(item => ({ locale: item.locale, width: item.width, reduced: item.reduced, height: item.fullHeight, expandedFrames: item.expansion.filter(x => x.opacity > 0 && x.opacity < 1).length, collapsedFrames: item.collapse.filter(x => x.opacity > 0 && x.opacity < 1).length })), errors, writes }));
} catch (error) {
  results.push({ failed: error.message }); await page?.screenshot({ path: `${out}/failure.png`, fullPage: true }).catch(() => {}); throw error;
} finally {
  await writeFile(`${out}/results.json`, JSON.stringify({ at: new Date().toISOString(), base, results, errors, writes }, null, 2));
  await browser.close();
}
