import { chromium, expect, request } from '@playwright/test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';

// Main: public navigation and GET only, except the explicitly authorized invalid-CSRF probe.
// Account mode runs against the isolated clone because StoreProvider auto-syncs cart/wishlist.
const accountOnly = process.argv.includes('--account-only');
const routesOnly = process.argv.includes('--routes-only');
const apiOnly = process.argv.includes('--api-only');
const servicesOnly = process.argv.includes('--services-only');
const runSuffix = process.env.BOTZONE_AUDIT_SUFFIX || (routesOnly ? '-routes-recheck' : apiOnly ? '-api-recheck' : servicesOnly ? '-services' : '');
const base = accountOnly ? process.env.BOTZONE_ACCOUNT_WEB_URL || 'http://127.0.0.1:3001' : process.env.BOTZONE_WEB_URL || 'http://127.0.0.1:3000';
const output = `artifacts/localization/${accountOnly ? 'account-clone' : 'public-main'}${runSuffix}`;
const results = [], issues = [], pageErrors = [], requests = [], observations = [];
const viCharacters = /[ăâđêôơưĂÂĐÊÔƠƯ\u1ea0-\u1ef9]/u;
const rawInternal = /\b(?:priceDisclosure|technicalSpecifications|trackingMode|standardWarrantyMonths|order_staff|warehouse_staff|content_staff|in_transit|awaiting_payment|display_size|memory_options|productIds|fixture|banking|serialized)\b/u;
const watchedFiles = ['src/components/client.tsx', 'src/components/storefront.tsx', 'src/components/account-services.tsx', 'src/components/commerce.tsx', 'src/components/store-shell.tsx', 'src/components/product-specifications.tsx'];
const hashes = async () => Object.fromEntries(await Promise.all(watchedFiles.map(async file => [file, createHash('sha256').update(await readFile(file)).digest('hex')])));
const sourceBefore = await hashes();
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const http = await request.newContext({ baseURL: base });
let activePage;

async function check(name, fn) {
  try { const evidence = await fn(); results.push({ name, pass: true, ...(evidence ? { evidence } : {}) }); }
  catch (error) {
    results.push({ name, pass: false, message: error.message });
    issues.push({ kind: 'assertion', name, url: activePage?.url(), message: error.message });
    await activePage?.screenshot({ path: `${output}/failure-${results.length}.png`, fullPage: true }).catch(() => {});
    console.log(JSON.stringify({ failed: name, message: error.message.split('\n')[0] }));
  }
  await writeFile(`${output}/checkpoint.json`, JSON.stringify({ results, issues, pageErrors }, null, 2));
  if (results.length % 10 === 0) console.log(JSON.stringify({ progress: results.length, failures: results.filter(x => !x.pass).length, languageCandidates: issues.filter(x => x.kind === 'language').length }));
}
function observe(page) {
  activePage = page;
  page.setDefaultTimeout(12000);
  page.on('pageerror', error => pageErrors.push({ url: page.url(), message: error.message }));
  page.on('request', req => {
    if (req.url().includes('/api/') && !['GET', 'HEAD', 'OPTIONS'].includes(req.method())) {
      let body; try { body = req.postDataJSON(); } catch {}
      requests.push({ method: req.method(), url: req.url(), command: body?.command });
    }
  });
}
async function settle(page, locale, route) {
  await expect(page.locator('html')).toHaveAttribute('lang', locale);
  await expect(page.locator('main')).toBeVisible();
  if (route.startsWith('/products/')) await expect(page.locator('.product-detail h1')).toBeVisible();
  else if (route.startsWith('/products') || route.startsWith('/wishlist')) await expect(page.locator('.catalogue-page h1')).toBeVisible();
  else if (route === '') await expect(page.locator('main h1').first()).toBeVisible();
  else await expect(page.locator('main h1:visible, main h2:visible, main h3:visible, main [role="alert"]:visible').first()).toBeVisible();
  await page.waitForTimeout(90);
}
async function navigate(page, locale, route) {
  await page.goto(`${base}/${locale}${route}`, { waitUntil: 'networkidle' });
  await settle(page, locale, route);
}
async function switchLanguage(page, locale, route) {
  const before = new URL(page.url());
  await page.locator('a.language').click();
  await expect.poll(() => new URL(page.url()).pathname.split('/')[1]).toBe(locale);
  await settle(page, locale, route);
  const after = new URL(page.url());
  assert.equal(after.pathname.replace(/^\/(en|vi)/, ''), before.pathname.replace(/^\/(en|vi)/, ''));
  assert.equal(after.search, before.search, 'Locale switch preserves the complete query');
}
async function inspect(page, locale, tag, screenshot = false, exemptions = []) {
  const snapshot = await page.evaluate(() => {
    const visible = element => {
      const style = getComputedStyle(element);
      return !!element.getClientRects().length && style.display !== 'none' && style.visibility !== 'hidden';
    };
    const texts = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    let current;
    while ((current = walker.nextNode())) {
      const parent = current.parentElement, value = current.textContent?.replace(/\s+/g, ' ').trim();
      if (parent && value && !parent.closest('script,style,canvas') && visible(parent)) texts.push({ text: value, element: parent.tagName.toLowerCase(), className: parent.className });
    }
    const attributes = [...document.querySelectorAll('[aria-label],[placeholder],img[alt]')].filter(visible).flatMap(element => ['aria-label', 'placeholder', 'alt'].filter(name => element.hasAttribute(name)).map(name => ({ text: element.getAttribute(name), attribute: name, element: element.tagName.toLowerCase() })));
    return { title: document.title, lang: document.documentElement.lang, width: innerWidth, scrollWidth: document.documentElement.scrollWidth, texts, attributes };
  });
  assert.equal(snapshot.lang, locale);
  assert.ok(snapshot.title.includes('BotZone'), 'Document title has the brand');
  assert.ok(locale === 'en' ? !viCharacters.test(snapshot.title) : snapshot.title.includes('Một thế giới'), 'Document title follows locale');
  assert.ok(snapshot.scrollWidth <= snapshot.width + 1, `No horizontal overflow (${snapshot.scrollWidth}/${snapshot.width})`);
  const findings = [...snapshot.texts, ...snapshot.attributes].filter(item => {
    let checkedText = item.text;
    for (const value of exemptions) if (typeof value === 'string' && value) checkedText = checkedText.replaceAll(value, '');
    // The language switch names its destination in that destination language.
    if (item.attribute === 'aria-label' && item.text === 'Chuyển sang tiếng Việt') return false;
    return locale === 'en' ? viCharacters.test(checkedText) : rawInternal.test(checkedText);
  });
  for (const item of findings) issues.push({ kind: 'language', tag, locale, url: page.url(), ...item });
  const evidence = { tag, locale, url: page.url(), title: snapshot.title, width: snapshot.width, candidateCount: findings.length };
  observations.push(evidence);
  await writeFile(`${output}/${tag}-${locale}.json`, JSON.stringify(snapshot, null, 2));
  if (screenshot || findings.length) await page.screenshot({ path: `${output}/${tag}-${locale}.png`, fullPage: true });
  return evidence;
}
async function apiGet(path, locale = 'en') {
  const response = await http.get(path, { headers: { 'Accept-Language': locale } });
  const body = await response.json();
  assert.ok(response.ok(), `${path}: ${response.status()} ${JSON.stringify(body)}`);
  return body;
}

async function publicAudit() {
  const expected = JSON.parse(await readFile('data/product-specifications.json', 'utf8'));
  const catalogues = {};
  for (const locale of ['en', 'vi']) {
    await check(`API ${locale}: 35 models / 609 exact localized fields, labels, photos and options`, async () => {
      const data = await apiGet(`/api/catalogue?locale=${locale}`, locale); catalogues[locale] = data;
      assert.equal(data.products.length, 35);
      let fields = 0;
      const languageCandidates = [];
      for (const reference of expected) {
        const model = data.products.find(p => p.modelCode === reference.modelCode);
        assert.ok(model, reference.modelCode);
        assert.ok(model.name?.trim());
        if (model.name !== reference.name) observations.push({ kind: 'preservedCatalogueDisplayName', locale, modelCode: model.modelCode, actual: model.name, reference: reference.name });
        assert.equal(model.technicalSpecs.verifiedAt, reference.verifiedAt);
        assert.deepEqual(model.technicalSpecs.highlightIds, reference.highlightIds);
        for (const group of reference.groups) {
          const actual = model.technicalSpecs.groups.find(g => g.id === group.id);
          assert.equal(actual.label, group.label[locale]);
          for (const item of group.items) {
            const field = actual.items.find(i => i.id === item.id);
            assert.equal(field.label, item.label[locale]); assert.equal(field.value, item.value[locale]); fields++;
          }
        }
        for (const source of model.technicalSpecs.sources) assert.ok(new URL(source.url).hostname.endsWith('apple.com'));
        assert.ok(model.photos?.length >= 1, `Photo metadata ${model.modelCode}`);
        for (const photo of model.photos) assert.ok(photo.alt?.trim());
        for (const variant of model.variants) {
          assert.deepEqual(Object.keys(variant.optionLabels).sort(), Object.keys(variant.options).sort());
          if (locale === 'en') for (const [key, value] of Object.entries(variant.optionLabels)) if (viCharacters.test(value)) languageCandidates.push({ model: model.modelCode, key, value });
        }
        if (locale === 'en') for (const photo of model.photos) if (viCharacters.test(photo.alt)) languageCandidates.push({ model: model.modelCode, photoAlt: photo.alt });
      }
      assert.equal(fields, 609);
      assert.deepEqual(languageCandidates, []);
      await writeFile(`${output}/catalogue-${locale}.json`, JSON.stringify(data, null, 2));
      return { models: data.products.length, localizedFields: fields, optionAndPhotoCandidates: languageCandidates };
    });
  }
  await check('API: read-only missing product and invalid CSRF return bilingual messages', async () => {
    const evidence = [];
    for (const locale of ['en', 'vi']) {
      const missing = await http.get(`/api/catalogue/not-a-real-model?locale=${locale}`, { headers: { 'Accept-Language': locale } });
      const missingBody = await missing.json(); assert.equal(missing.status(), 404);
      const csrf = await http.post('/api/commands', { headers: { 'Accept-Language': locale, 'X-CSRF-TOKEN': 'invalid-localization-probe' }, data: { command: '__invalid_localization_probe__', key: crypto.randomUUID(), input: {} } });
      const csrfBody = await csrf.json(); assert.ok([400, 403].includes(csrf.status())); assert.equal(csrfBody.error.code, 'CSRF_INVALID');
      const messages = [missingBody.error.message, csrfBody.error.message];
      for (const message of messages) assert.equal(viCharacters.test(message), locale === 'vi');
      evidence.push({ locale, missing: { status: missing.status(), body: missingBody }, invalidCsrf: { status: csrf.status(), body: csrfBody } });
    }
    await writeFile(`${output}/api-errors.json`, JSON.stringify(evidence, null, 2));
    return evidence;
  });

  if (apiOnly) return;

  const products = catalogues.en?.products || (await apiGet('/api/catalogue?locale=en')).products;
  for (const width of [390, 1440]) {
    const context = await browser.newContext({ viewport: { width, height: width === 390 ? 844 : 1000 }, isMobile: width === 390, hasTouch: width === 390, reducedMotion: 'reduce' });
    const page = await context.newPage(); observe(page);
    const routes = ['', '/products?sort=price-desc&page=2', '/compare', '/wishlist', '/cart', '/checkout', '/login?next=checkout', '/register', '/forgot-password', '/reset-password', '/verify-email', '/change-password', '/trade-in', '/care', '/policy/shipping', '/policy/returns', '/policy/privacy', '/not-a-real-page'];
    for (let index = 0; index < routes.length; index++) {
      const route = routes[index], tag = `${width}-route-${index}`;
      await check(`${width}px ${route || '/'}: EN → VI → EN`, async () => {
        await navigate(page, 'en', route); await inspect(page, 'en', `${tag}-first`, index < 2 || [6, 8, 12, 13, 17].includes(index));
        await switchLanguage(page, 'vi', route); await inspect(page, 'vi', tag, [0, 1, 6, 12, 13].includes(index));
        await switchLanguage(page, 'en', route); await inspect(page, 'en', `${tag}-back`);
      });
    }
    for (const product of routesOnly ? [] : products) {
      const selected = product.variants.at(-1), route = `/products/${product.slug}?variant=${selected.id}`;
      const tag = `${width}-product-${product.modelCode}`;
      await check(`${width}px ${product.modelCode}: detail/variant/specifications EN → VI → EN`, async () => {
        await navigate(page, 'en', route);
        await expect(page.locator('.variant-option[aria-pressed="true"]')).toContainText(Object.values(selected.optionLabels).join(' · '));
        await inspect(page, 'en', `${tag}-first`, ['P18MAX', 'DUO', 'IPA4', 'POWER20'].includes(product.modelCode));
        await switchLanguage(page, 'vi', route); await inspect(page, 'vi', tag);
        await switchLanguage(page, 'en', route); await inspect(page, 'en', `${tag}-back`);
        await expect(page.locator('.variant-option[aria-pressed="true"]')).toContainText(Object.values(selected.optionLabels).join(' · '));
        assert.equal(new URL(page.url()).searchParams.get('variant'), selected.id);
      });
    }
    await check(`${width}px: search/sort/category and browser history survive language switching`, async () => {
      const route = '/products?q=iPhone&category=iphone&sort=price-asc&stock=available';
      await navigate(page, 'en', route);
      const selected = await page.locator('.product-card h3').allTextContents(); assert.ok(selected.length);
      await switchLanguage(page, 'vi', route); assert.deepEqual(await page.locator('.product-card h3').allTextContents(), selected);
      await switchLanguage(page, 'en', route); await page.goBack(); await settle(page, 'vi', route);
      assert.equal(new URL(page.url()).search, new URL(`${base}/en${route}`).search);
      await page.goForward(); await settle(page, 'en', route);
      await expect(page.getByRole('textbox', { name: 'Search products', exact: true })).toHaveValue('iPhone');
    });
    await check(`${width}px: unsubmitted login email survives EN → VI → EN`, async () => {
      const route = '/login?next=checkout'; await navigate(page, 'en', route);
      await page.locator('#auth-email').fill('localization.audit@example.invalid');
      await switchLanguage(page, 'vi', route);
      const viEmail = await page.locator('#auth-email').inputValue();
      await switchLanguage(page, 'en', route);
      const enEmail = await page.locator('#auth-email').inputValue();
      assert.equal(viEmail, 'localization.audit@example.invalid', 'Unsubmitted email should persist on locale change');
      assert.equal(enEmail, 'localization.audit@example.invalid');
    });
    await context.close();
  }
  await check('Public-main browser made no mutating API requests', async () => assert.deepEqual(requests, []));
}

async function accountAudit() {
  assert.equal(new URL(base).port, '3001', 'Authenticated browser scope must target clone port 3001');
  const csrf = (await apiGet('/api/auth/csrf')).token;
  const login = await http.post('/api/auth/login', { headers: { 'Accept-Language': 'en', 'X-CSRF-TOKEN': csrf }, data: { email: 'customer@botzone.local', password: 'BotZoneDemo!2026', clientType: 'web' } });
  assert.ok(login.ok(), `Clone session login ${login.status()}`);
  const account = await apiGet('/api/account'), orders = await apiGet('/api/orders');
  const customerName = account.profile?.name ?? account.profile?.display_name ?? account.profile?.displayName;
  const originalStrings = object => Object.values(object || {}).filter(value => typeof value === 'string' && viCharacters.test(value));
  const commonExemptions = [customerName, ...(account.addresses || []).flatMap(originalStrings)];
  for (const width of [390, 1440]) {
    const context = await browser.newContext({ viewport: { width, height: width === 390 ? 844 : 1000 }, storageState: await http.storageState(), reducedMotion: 'reduce' });
    const page = await context.newPage(); observe(page);
    for (const route of ['/trade-in', '/care']) await check(`${width}px clone ${route}: actual signed-in forms and dropdown options localized`, async () => {
      for (const locale of ['en', 'vi', 'en']) {
        await navigate(page, locale, route);
        const combos = page.getByRole('combobox');
        for (let index = 0; index < await combos.count(); index++) {
          const combo = combos.nth(index);
          if (!(await combo.isVisible()) || !(await combo.isEnabled())) continue;
          await combo.click();
          await inspect(page, locale, `${width}-service-${route.slice(1)}-menu-${index}-${observations.length}`, false, commonExemptions);
          await page.keyboard.press('Escape');
        }
        await inspect(page, locale, `${width}-service-${route.slice(1)}-${observations.length}`, true, commonExemptions);
      }
    });
    if (servicesOnly) { await context.close(); continue; }
    await check(`${width}px clone account overview/profile/notifications EN → VI → EN`, async () => {
      for (const locale of ['en', 'vi', 'en']) {
        await navigate(page, locale, '/account');
        await expect(page.getByRole('tablist')).toBeVisible();
        for (const tab of ['overview', 'credit', 'profile', 'notifications', 'tradeIns', 'returns', 'warranty']) {
          await page.locator(`#account-tab-${tab}`).click();
          await inspect(page, locale, `${width}-account-${tab}-${observations.length}`, ['profile', 'notifications'].includes(tab), commonExemptions);
        }
      }
    });
    await check(`${width}px clone selected account tab survives locale change`, async () => {
      await navigate(page, 'en', '/account'); await page.locator('#account-tab-profile').click();
      await switchLanguage(page, 'vi', '/account');
      await expect(page.locator('#account-tab-profile')).toHaveAttribute('aria-selected', 'true');
      await switchLanguage(page, 'en', '/account');
      await expect(page.locator('#account-tab-profile')).toHaveAttribute('aria-selected', 'true');
      await page.goBack(); await settle(page, 'vi', '/account');
      await expect(page.locator('#account-tab-profile')).toHaveAttribute('aria-selected', 'true');
      await page.goForward(); await settle(page, 'en', '/account');
      await expect(page.locator('#account-tab-profile')).toHaveAttribute('aria-selected', 'true');
    });
    const routes = ['/orders'], routeExemptions = new Map();
    for (const order of orders.orders.slice(0, 3)) {
      routes.push(`/orders/${order.id}`);
      const detail = await apiGet(`/api/orders/${order.id}`);
      routeExemptions.set(`/orders/${order.id}`, originalStrings(detail.order?.addressSnapshot ?? detail.order?.address_snapshot));
      if (detail.invoice?.id) {
        const invoicePath = `/invoices/${detail.invoice.id}`;
        routes.push(invoicePath);
        const invoice = (await apiGet(`/api${invoicePath}`)).invoice;
        routeExemptions.set(invoicePath, [...originalStrings(invoice.customerSnapshot ?? invoice.customer_snapshot), ...originalStrings(invoice.staffSnapshot ?? invoice.staff_snapshot)]);
      }
    }
    for (let index = 0; index < routes.length; index++) await check(`${width}px clone ${routes[index]} localized read-only view`, async () => {
      for (const locale of ['en', 'vi', 'en']) {
        await navigate(page, locale, routes[index]);
        await inspect(page, locale, `${width}-record-${index}-${observations.length}`, true, [...commonExemptions, ...(routeExemptions.get(routes[index]) || [])]);
      }
    });
    await context.close();
  }
  await check('Clone browser only automatic cart/wishlist synchronization; no business commands', async () => {
    const allowed = ['save_cart', 'sync_wishlist'];
    assert.deepEqual(requests.filter(item => !allowed.includes(item.command)), []);
    return requests;
  });
}

try {
  if (accountOnly) await accountAudit(); else await publicAudit();
  await check('No uncaught browser exceptions', async () => assert.deepEqual(pageErrors, []));
} catch (error) {
  issues.push({ kind: 'harness', message: error.stack });
  process.exitCode = 1;
} finally {
  const sourceAfter = await hashes();
  const sourceChanged = watchedFiles.filter(file => sourceBefore[file] !== sourceAfter[file]);
  const report = { at: new Date().toISOString(), mode: accountOnly ? 'account-clone' : 'public-main', base, results, issues, pageErrors, observedBrowserWrites: requests, observations, sourceBefore, sourceAfter, sourceChanged, auditEditedApplicationSource: false };
  await writeFile(`${output}/results.json`, JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ completed: results.length, passed: results.filter(x => x.pass).length, failed: results.filter(x => !x.pass).length, languageCandidates: issues.filter(x => x.kind === 'language').length, pageErrors: pageErrors.length, sourceChanged }));
  await browser.close(); await http.dispose();
}
