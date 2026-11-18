import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { base, client } from './live-api.mjs';

const results = [], errors = [], clients = [], orders = [];
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1365, height: 1000 }, reducedMotion: 'reduce' });
const page = await context.newPage();
page.on('pageerror', error => errors.push(error.message));
page.setDefaultTimeout(20000);
await mkdir('artifacts/web', { recursive: true });
const email = `checkout-${Date.now()}@botzone.local`, password = 'CheckoutTest!2026';
const shipping = { recipient: 'Checkout Recovery Customer', phone: '+84901234567', province: 'Hồ Chí Minh', ward: 'Bến Thành', addressLine: '123 Checkout Recovery Street', country: 'VN' };
let customer, staff, warehouse;
const commandInput = (variantId, quote, address, saveAddress = true) => ({ items: [{ variantId, quantity: 1, care: false }], points: 0, creditVnd: '0', promotionCode: '', quoteHash: quote.quoteHash, address, method: 'cod', saveAddress });
async function verificationMail() {
  for (let i = 0; i < 25; i++) {
    const response = await fetch('http://127.0.0.1:8025/api/v1/messages');
    const inbox = await response.json();
    for (const message of inbox.messages ?? []) {
      if (!(message.To ?? []).some(recipient => recipient.Address === email)) continue;
      const detail = await (await fetch(`http://127.0.0.1:8025/api/v1/message/${message.ID}`)).json();
      const text = detail.Text ?? '';
      const link = text.match(/http:\/\/[^\s]+/)?.[0];
      if (link?.includes('next=')) return new URL(link);
    }
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  throw Error('Checkout verification email did not arrive');
}
async function signedInClient(address) {
  if (address !== email) { const actor = await client(address); clients.push(actor); return actor; }
  const http = context.request;
  return {
    get: async path => { const response = await http.get(base + path); assert.ok(response.ok(), await response.text()); return response.json(); },
    post: async (path, input, expected = 200) => {
      const token = (await (await http.get(base + '/api/auth/csrf')).json()).token;
      const response = await http.post(base + path, { data: input, headers: { 'X-CSRF-TOKEN': token, 'Accept-Language': 'en' } });
      const body = await response.json(); assert.equal(response.status(), expected, JSON.stringify(body)); return body;
    },
    command: async (command, input, key = crypto.randomUUID(), expected = 200) => {
      const token = (await (await http.get(base + '/api/auth/csrf')).json()).token;
      const response = await http.post(base + '/api/commands', { data: { command, input, key }, headers: { 'X-CSRF-TOKEN': token, 'Accept-Language': 'en' } });
      const body = await response.json(); assert.equal(response.status(), expected, JSON.stringify(body)); return body;
    },
  };
}
try {
  const anonymous = await client(); clients.push(anonymous);
  await anonymous.post('/api/auth/register', { name: 'Checkout Recovery Account', email, password });
  const catalogue = await anonymous.get('/api/catalogue?locale=en');
  const product = catalogue.products.find(product => product.slug === 'ap2' && product.variants.some(variant => variant.available >= 8))
    ?? catalogue.products.find(product => product.variants[0]?.available >= 8);
  assert.ok(product, 'Need a stocked product for checkout regression');
  const variant = product.variants.find(variant => variant.available >= 8);
  await page.goto(base + '/en', { waitUntil: 'networkidle' });
  await page.evaluate(item => localStorage.setItem('botzone.cart', JSON.stringify([item])), { variantId: variant.id, quantity: 1, care: false });
  await page.goto(base + '/en/cart', { waitUntil: 'networkidle' });
  await page.getByRole('link', { name: 'Proceed to checkout', exact: true }).click();
  await page.waitForURL('**/en/login?next=*');
  assert.equal(new URL(page.url()).searchParams.get('next'), '/en/checkout');
  results.push('Guest checkout opens login with the checkout return destination');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.locator('form').getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL('**/en/checkout');
  customer = await signedInClient(email);
  staff = await signedInClient('nv01@botzone.local'); warehouse = await signedInClient('warehouse@botzone.local');
  for (const [label, value] of [['Recipient', shipping.recipient], ['Phone number', shipping.phone], ['Province / City', shipping.province], ['Ward', shipping.ward], ['Street address', shipping.addressLine]]) {
    await page.getByLabel(label, { exact: true }).fill(value);
  }
  await page.getByRole('combobox', { name: 'Demo payment method' }).click();
  await page.getByRole('option', { name: 'MoMo · Demo', exact: true }).click();
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.getByLabel('Recipient', { exact: true })).toHaveValue(shipping.recipient);
  await expect(page.getByLabel('Street address', { exact: true })).toHaveValue(shipping.addressLine);
  await expect(page.getByRole('combobox', { name: 'Demo payment method' })).toContainText('MoMo');
  await page.getByRole('combobox', { name: 'Demo payment method' }).click();
  await page.getByRole('option', { name: 'COD — pay on delivery', exact: true }).click();
  results.push('Reload restores shipping details and demo payment method');
  await page.getByRole('button', { name: 'Review order', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Confirm order', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Send verification email', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Verification link sent' })).toBeVisible();
  const verifyUrl = await verificationMail();
  assert.equal(verifyUrl.searchParams.get('next'), '/en/checkout');
  const verification = await context.newPage();
  await verification.goto(base + verifyUrl.pathname + verifyUrl.search, { waitUntil: 'networkidle' });
  await verification.getByRole('button', { name: 'Verify email', exact: true }).click();
  await verification.getByRole('link', { name: /Continue/ }).click();
  await verification.waitForURL('**/en/checkout');
  await verification.close();
  const checkVerification = page.getByRole('button', { name: 'I have verified — check again', exact: true });
  if (await checkVerification.isVisible()) await checkVerification.click();
  await expect(page.getByRole('button', { name: 'Confirm order', exact: true })).toBeEnabled();
  results.push('Unverified customer can resend verification and enable confirmation without losing the form');
  // Simulate an order committed by the API whose response never reaches the browser.
  let firstRequest;
  await page.route('**/api/commands', async route => {
    const body = route.request().postDataJSON();
    if (body.command !== 'place_order') return route.continue();
    firstRequest = body;
    const response = await route.fetch(); assert.equal(response.status(), 200, await response.text());
    const placed = await response.json(); orders.push(placed.orderId);
    await route.abort('failed');
  });
  await page.getByRole('button', { name: 'Confirm order', exact: true }).click();
  await expect(page.locator('main').getByRole('alert')).toContainText('Unable to connect');
  await page.unroute('**/api/commands');
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.getByLabel('Street address', { exact: true })).toHaveValue(shipping.addressLine);
  await page.getByRole('button', { name: 'Review order', exact: true }).click();
  let retryRequest;
  page.on('request', request => { if (request.url().endsWith('/api/commands') && request.postDataJSON()?.command === 'place_order') retryRequest = request.postDataJSON(); });
  await page.getByRole('button', { name: 'Confirm order', exact: true }).click();
  await page.waitForURL(/\/en\/orders\/[^/]+$/);
  const orderId = page.url().split('/').at(-1);
  assert.equal(orderId, orders[0]); assert.equal(firstRequest.key, retryRequest.key);
  await expect(page.getByRole('status').filter({ hasText: 'Order placed successfully' })).toBeVisible();
  assert.equal((await customer.get('/api/orders')).orders.filter(order => order.id === orderId).length, 1);
  const profile = await customer.get('/api/account');
  assert.equal(profile.profile.phone, shipping.phone);
  assert.equal(profile.profile.display_name, 'Checkout Recovery Account');
  assert.equal(profile.addresses.filter(address => address.address_line === shipping.addressLine).length, 1);
  assert.equal(await page.evaluate(userId => localStorage.getItem(`botzone.checkout.${userId}`), (await customer.get('/api/session')).actor.id), null);
  results.push('Lost response plus reload retries the same order; profile and one address are saved atomically');
  // Failed placements must not save addresses, and an explicit opt-out must be respected.
  const quoteInput = { items: [{ variantId: variant.id, quantity: 1, care: false }], points: 0, creditVnd: '0', promotionCode: '' };
  const quote = await customer.post('/api/quote', quoteInput);
  const badAddress = { ...shipping, addressLine: 'Do not persist failed order' };
  await customer.command('place_order', { ...commandInput(variant.id, quote, badAddress), quoteHash: 'outdated' }, crypto.randomUUID(), 409);
  assert.equal((await customer.get('/api/account')).addresses.length, profile.addresses.length);
  const optOut = await customer.command('place_order', commandInput(variant.id, quote, { ...shipping, addressLine: 'Do not save opted-out address' }, false)); orders.push(optOut.orderId);
  assert.equal((await customer.get('/api/account')).addresses.length, profile.addresses.length);
  await staff.command('cancel_order', { orderId: optOut.orderId });
  const repeatQuote = await customer.post('/api/quote', quoteInput);
  const repeat = await customer.command('place_order', commandInput(variant.id, repeatQuote, shipping)); orders.push(repeat.orderId);
  assert.equal((await customer.get('/api/account')).addresses.length, profile.addresses.length);
  await staff.command('cancel_order', { orderId: repeat.orderId });
  results.push('Failed placements roll back profile writes; opt-out and address deduplication work');
  await page.goto(base + '/en/account?tab=profile', { waitUntil: 'networkidle' });
  await expect(page.locator('main')).toContainText(shipping.addressLine);
  await page.evaluate(item => localStorage.setItem('botzone.cart', JSON.stringify([item])), { variantId: variant.id, quantity: 1, care: false });
  await page.goto(base + '/en/checkout', { waitUntil: 'networkidle' });
  await expect(page.getByLabel('Street address', { exact: true })).toHaveValue(shipping.addressLine);
  await page.goto(base + '/en/orders/' + orderId, { waitUntil: 'networkidle' });
  await expect(page.locator('main')).toContainText('Your invoice will appear here');
  await staff.command('confirm_order', { orderId });
  const invoice = await warehouse.command('handoff', { orderId });
  await page.goto(base + '/en/account?tab=invoices', { waitUntil: 'networkidle' });
  await page.getByRole('link').filter({ hasText: invoice.invoiceNo }).click();
  await expect(page.locator('main')).toContainText(invoice.invoiceNo);
  await page.getByRole('button', { name: 'Create XLSX & PDF', exact: true }).click();
  for (const format of ['PDF', 'XLSX']) {
    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('link', { name: format, exact: true }).click()]);
    assert.equal(await download.failure(), null); await download.saveAs(`artifacts/web/checkout-invoice.${format.toLowerCase()}`);
  }
  const other = await signedInClient('customer2@botzone.local');
  assert.ok(!(await other.get('/api/account')).invoices.some(item => item.id === invoice.invoiceId));
  assert.ok([403, 404].includes((await other.http.get('/api/invoices/' + invoice.invoiceId)).status()));
  results.push('Account lists only owned issued invoices; PDF and XLSX downloads succeed');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'artifacts/web/checkout-invoice-mobile.png', fullPage: true });
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await page.goto(base + '/vi/account?tab=invoices', { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Hóa đơn của bạn', exact: true })).toBeVisible();
  assert.deepEqual(errors, []);
  console.log(JSON.stringify(results, null, 2));
} catch (error) {
  results.push({ failure: error.message });
  await page.screenshot({ path: 'artifacts/web/checkout-recovery-failure.png', fullPage: true }).catch(() => {});
  throw error;
} finally {
  // Leave no pending inventory reservations from this regression run.
  if (staff) for (const orderId of orders) {
    try { const order = await customer.get('/api/orders/' + orderId); if (order.order.order_status === 'pending') await staff.command('cancel_order', { orderId }); } catch {}
  }
  await writeFile('artifacts/web/checkout-recovery-results.json', JSON.stringify({ email, orders, results, errors }, null, 2));
  await Promise.all(clients.map(client => client.dispose()));
  await browser.close();
}
