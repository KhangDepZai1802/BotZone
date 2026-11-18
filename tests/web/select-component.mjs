import { build } from 'esbuild';
import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir } from 'node:fs/promises';

const bundle = await build({ entryPoints: ['tests/web/select-fixture.tsx'], bundle: true, write: false, format: 'iife', platform: 'browser', jsx: 'automatic', define: { 'process.env': '{}', 'process.env.NODE_ENV': '"production"' } });
const css = (await readFile('src/styles/globals.css', 'utf8')).replace('@import "tailwindcss";', '').replace(/@font-face\s*\{[^}]*\}/g, '');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [];
await mkdir('artifacts/web', { recursive: true });
try {
  for (const width of [390, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    page.on('pageerror', error => errors.push(error.message));
    await page.setContent('<html lang="en"><head></head><body><div id="fixture"></div></body></html>');
    await page.addStyleTag({ content: css }); await page.addScriptTag({ content: bundle.outputFiles[0].text });
    const opener = page.getByRole('button', { name: 'Open selection form', exact: true });
    await opener.click(); await expect(page.getByRole('dialog')).toBeVisible();
    const product = page.getByRole('combobox', { name: 'Product', exact: true });
    const config = page.getByRole('combobox', { name: 'Configuration', exact: true });
    await expect(config).toBeDisabled();
    await page.getByRole('button', { name: 'Submit locally' }).click();
    await expect(product).toHaveAttribute('aria-invalid', 'true'); await expect(product).toBeFocused();
    await expect(page.getByRole('alert')).toHaveText('Please choose an option.');
    await product.click();
    await expect(page.getByRole('option', { name: 'Unavailable option', exact: true })).toHaveAttribute('aria-disabled', 'true');
    const bounds = await page.getByRole('listbox').boundingBox();
    assert.ok(bounds.x >= 0 && bounds.y >= 0 && bounds.x + bounds.width <= width + 1 && bounds.y + bounds.height <= 901);
    await expect(page.getByRole('option', { name: 'Choose a product', exact: true })).toBeFocused();
    await page.keyboard.press('b');
    await expect(page.getByRole('option', { name: 'Beta', exact: true })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(product).toContainText('Beta'); await expect(config).toBeEnabled();
    await config.click(); await page.getByRole('option', { name: 'Large configuration', exact: true }).click();
    await page.getByRole('button', { name: 'Submit locally' }).click();
    await expect(page.getByLabel('Submitted values')).toHaveText('{"product":"beta","configuration":"beta-large"}');
    await config.click(); await page.getByRole('option', { name: 'Not specified', exact: true }).click();
    await page.getByRole('button', { name: 'Submit locally' }).click();
    await expect(page.getByLabel('Submitted values')).toHaveText('{"product":"beta","configuration":""}');
    await product.click(); await page.keyboard.press('Escape');
    await expect(product).toBeFocused(); await expect(page.getByRole('dialog')).toBeVisible();
    await page.screenshot({ path: `artifacts/web/select-form-${width}.png`, fullPage: true });
    await page.keyboard.press('Escape'); await expect(page.getByRole('dialog')).toHaveCount(0); await expect(opener).toBeFocused();
    results.push({ width, passed: true, checks: 'Native required validation and FormData; dependent disabled/empty options; long popup bounded; typeahead; Escape closes select first, then dialog; focus restored' });
    await page.close();
  }
  assert.deepEqual(errors, []); console.log(JSON.stringify({ passed: results.length, errors }));
} catch (error) {
  results.push({ passed: false, failure: error.message });
  throw error;
} finally {
  await writeFile('artifacts/web/select-component-results.json', JSON.stringify({ at: new Date().toISOString(), scope: 'Actual shared Select/Field/Modal in isolated Edge component fixture; no API or login', results, errors }, null, 2));
  await browser.close();
}
