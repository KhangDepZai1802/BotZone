import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';

const base = process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const out = 'artifacts/web/semantic-compare';
await mkdir(out, { recursive: true });
const catalogue = await (await fetch(base + '/api/catalogue?locale=en')).json();
const product = code => {
  const result = catalogue.products.find(item => item.modelCode === code);
  assert.ok(result, `Actual catalogue contains ${code}`);
  return result;
};
const cases = [
  { codes: ['IPA3', 'IPA4'], advantages: { chip: [1], cpu: [], gpu: [], memory_options: [1], resolution: [], brightness: [] } },
  { codes: ['IPA4', 'IPP5'], advantages: { chip: [1], cpu: [1], gpu: [1], memory_options: [1], resolution: [1], brightness: [1], refresh_rate: [] } },
  { codes: ['IPA3', 'IPA4', 'IPP5'], advantages: { chip: [2], cpu: [2], gpu: [2], memory_options: [2], resolution: [2], brightness: [2], refresh_rate: [] } },
];
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const results = [], errors = [], writes = [];
let page;
const ranked = field => page.locator(`tr[data-spec-id="${field}"] td`).evaluateAll(cells => cells.flatMap((cell, index) => cell.dataset.advantage === 'true' ? [index] : []));
try {
  for (const entry of cases) for (const locale of ['vi', 'en']) for (const [width, height] of [[390, 844], [1366, 768]]) {
    const selected = entry.codes.map(product);
    page = await browser.newPage({ viewport: { width, height }, reducedMotion: 'reduce' });
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (request.url().includes('/api/') && !['GET', 'HEAD'].includes(request.method())) writes.push(request.url()); });
    await page.addInitScript(ids => { if (localStorage.getItem('botzone.compare') === null) localStorage.setItem('botzone.compare', JSON.stringify(ids)); }, selected.map(item => item.id));
    await page.goto(`${base}/${locale}/compare`, { waitUntil: 'networkidle' });
    await expect(page.locator('.compare-table thead th')).toHaveCount(selected.length + 1);
    for (const [field, expected] of Object.entries(entry.advantages)) assert.deepEqual(await ranked(field), expected, `${entry.codes.join('/')} ${locale} ${field}`);
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No viewport overflow');
    const visual = await page.locator('td[data-advantage="true"]').evaluateAll(cells => cells.map(cell => {
      const style = getComputedStyle(cell), numbers = style.backgroundColor.match(/\d+(?:\.\d+)?/g).map(Number);
      const reason = cell.querySelector('details>summary,span');
      return { neutral: numbers[0] === numbers[1] && numbers[1] === numbers[2], weight: getComputedStyle(cell.querySelector('strong')).fontWeight, summary: reason?.textContent?.trim() };
    }));
    assert.ok(visual.length > 0 && visual.every(cell => cell.neutral && cell.weight === '700' && cell.summary), 'Every advantage is bold neutral and has a concise visible reason');
    const region = page.getByRole('region').filter({ has: page.locator('.compare-table') });
    await region.evaluate(element => element.scrollIntoView({ block: 'start', behavior: 'instant' }));
    const geometry = await region.evaluate(element => {
      const regionBox = element.getBoundingClientRect(), header = element.querySelector('thead th').getBoundingClientRect();
      return { headerHeight: header.height, visibleDataRows: [...element.querySelectorAll('tbody tr')].filter(row => row.querySelector('th[scope="row"]')).filter(row => {
        const box = row.getBoundingClientRect();
        return box.top >= header.bottom - 1 && box.bottom <= Math.min(innerHeight - 2, regionBox.bottom - 2);
      }).length };
    });
    assert.ok(geometry.headerHeight <= (width < 768 ? 170 : 160), 'Compact pinned header');
    if (width === 1366) assert.ok(geometry.visibleDataRows >= 7, `Seven full data rows: ${entry.codes.join('/')} ${locale}, got ${geometry.visibleDataRows}`);
    const name = `${entry.codes.join('-').toLowerCase()}-${locale}-${width}`;
    await page.screenshot({ path: `${out}/${name}-initial.png` });
    const performanceRow = page.locator('tr[data-spec-id="chip"]');
    await region.evaluate((element, rowTop) => { element.scrollTop += rowTop - element.getBoundingClientRect().top - 150; }, (await performanceRow.boundingBox()).y);
    const pinned = await region.evaluate(element => {
      const top = element.getBoundingClientRect().top;
      return { scrollTop: element.scrollTop, offsets: [...element.querySelectorAll('thead th')].map(cell => cell.getBoundingClientRect().top - top) };
    });
    assert.ok(pinned.scrollTop > 0 && pinned.offsets.every(offset => Math.abs(offset) <= 3), 'Product headers remain pinned while reading lower rows');
    if (width === 390) await region.evaluate(element => { element.scrollLeft = element.scrollWidth; });
    await page.screenshot({ path: `${out}/${name}-performance.png` });
    const explanation = page.locator('tr[data-spec-id="chip"] td[data-advantage="true"] details').first();
    await expect(explanation).toHaveCount(1);
    const summary = explanation.locator('summary');
    await summary.focus(); await page.keyboard.press('Enter');
    await expect(explanation).toHaveAttribute('open', '');
    await expect(explanation.locator('span')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(explanation).not.toHaveAttribute('open', '');
    await expect(summary).toBeFocused();
    await summary.click(); await expect(explanation).toHaveAttribute('open', '');
    await summary.click(); await expect(explanation).not.toHaveAttribute('open', '');
    if (entry.codes.includes('IPP5')) {
      const context = page.locator('tr[data-spec-id="refresh_rate"] th details');
      const contextTrigger = context.locator('summary');
      await contextTrigger.focus(); await page.keyboard.press('Enter');
      await expect(context).toHaveAttribute('open', '');
      await expect(context.locator('span')).toContainText('60Hz');
      await page.keyboard.press('Escape');
      await expect(context).not.toHaveAttribute('open', '');
      await expect(contextTrigger).toBeFocused();
      await contextTrigger.click(); await expect(context).toHaveAttribute('open', '');
      await contextTrigger.click(); await expect(context).not.toHaveAttribute('open', '');
    }
    if (entry.codes.length === 3) {
      const toggle = page.getByRole('switch');
      await toggle.focus(); await page.keyboard.press('Space');
      await expect(toggle).toHaveAttribute('aria-checked', 'true');
      await expect(page.locator('tr[data-different="false"]')).toHaveCount(0);
      await page.keyboard.press('Space');
      const remove = page.locator('.compare-table thead th').last().getByRole('button');
      await expect(remove).toHaveAccessibleName(locale === 'vi' ? /^Bỏ .+ khỏi so sánh$/ : /^Remove .+ from comparison$/);
      await remove.click();
      await expect(page.locator('.compare-table thead th')).toHaveCount(3);
      assert.deepEqual(await ranked('cpu'), []); assert.deepEqual(await ranked('memory_options'), [1]);
    }
    results.push({ codes: entry.codes, locale, width, height, ...geometry, pinned, advantages: entry.advantages, visibleReasons: visual.length, disclosures: 'keyboard/touch/Escape pass' });
    await page.close(); page = undefined;
  }
  assert.deepEqual(errors, []); assert.deepEqual(writes, []);
  console.log(JSON.stringify({ passed: results.length, rows: results.filter(item => item.width === 1366).map(item => ({ products: item.codes, locale: item.locale, rows: item.visibleDataRows })), errors, writes }));
} catch (error) {
  results.push({ failed: error.message });
  await page?.screenshot({ path: `${out}/failure.png`, fullPage: true }).catch(() => {});
  throw error;
} finally {
  await writeFile(`${out}/results.json`, JSON.stringify({ at: new Date().toISOString(), base, results, errors, writes }, null, 2));
  await browser.close();
}
