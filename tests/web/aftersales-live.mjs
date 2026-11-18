import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { base, client } from './live-api.mjs';

const previous = JSON.parse(await readFile('artifacts/web/commerce-results.json','utf8'));
const orderId = process.env.BOTZONE_ORDER_ID || previous.results.find(x=>x.createdOrder)?.createdOrder;
const invoiceId = process.env.BOTZONE_INVOICE_ID || previous.results.find(x=>x.handoffInvoice)?.handoffInvoice;
assert.ok(orderId && invoiceId, 'Run commerce-live first');
const actors=[];
const browser=await chromium.launch({channel:'msedge',headless:true});
const customer=await client('customer@botzone.local');actors.push(customer);
const context=await browser.newContext({storageState:await customer.http.storageState(),viewport:{width:1366,height:900},reducedMotion:'reduce'});
const page=await context.newPage(), results=[], errors=[];
page.setDefaultTimeout(20000);
page.on('pageerror',error=>errors.push(error.message));
await mkdir('artifacts/web',{recursive:true});
try {
 await page.goto(`${base}/en/invoices/${invoiceId}`,{waitUntil:'networkidle'});
 console.log('Invoice loaded; creating export');
 if (!process.env.BOTZONE_SKIP_EXPORT) {
 const exportResponse=page.waitForResponse(r=>r.url().endsWith('/api/invoices/exports')&&r.request().method()==='POST');
 await page.getByRole('button',{name:'Create XLSX & PDF',exact:true}).click();
 const exported=await exportResponse;assert.equal(exported.status(),200,await exported.text());
 for(const format of ['PDF','XLSX']) {
  const link=page.getByRole('link',{name:format,exact:true});await expect(link).toBeVisible({timeout:60000});
  const [download]=await Promise.all([page.waitForEvent('download'),link.click()]);
  const path=`artifacts/web/customer-invoice.${format.toLowerCase()}`;await download.saveAs(path);
  const bytes=await readFile(path);assert.ok(bytes.length>500);
  assert.equal(bytes.subarray(0,format==='PDF'?4:2).toString(),format==='PDF'?'%PDF':'PK');
  results.push({download:format,bytes:bytes.length});
  console.log(`Downloaded ${format}: ${bytes.length} bytes`);
 }
 } else results.push('Exports skipped for focused aftersales retest; prior API failure retained');
 const existingReturn=(await customer.get('/api/account')).returns.find(r=>r.order_id===orderId);
 if(!existingReturn) {
 await page.goto(`${base}/en/orders/${orderId}`,{waitUntil:'networkidle'});
 await page.getByRole('button',{name:'Request return',exact:true}).click();
 const form=page.locator('form');await form.getByRole('checkbox').first().check();
 const uploadResponse=page.waitForResponse(r=>r.url().endsWith('/api/media')&&r.request().method()==='POST');
 await form.locator('input[type=file]').setInputFiles('artifacts/web/login-mobile.png');
 const uploadedResponse=await uploadResponse;assert.equal(uploadedResponse.status(),200);const uploaded=await uploadedResponse.json();
 console.log(`Private evidence uploaded: ${uploaded.id}`);
 await expect(form.getByRole('button',{name:'Submit request',exact:true})).toBeEnabled();
 const deniedClient=await client('customer2@botzone.local');actors.push(deniedClient);
 const anon=await client();actors.push(anon);
 assert.equal((await customer.http.get(uploaded.previewUrl)).status(),200);
 assert.ok([403,404].includes((await deniedClient.http.get(uploaded.previewUrl)).status()));
 assert.equal((await anon.http.get(uploaded.previewUrl)).status(),401);
 results.push('Private return evidence: owner allowed, other customer and anonymous denied');
 const requestResponse=page.waitForResponse(r=>r.url().endsWith('/api/commands')&&r.request().postDataJSON()?.command==='request_return');
 await form.getByRole('button',{name:'Submit request',exact:true}).click();
 const requested=await requestResponse;assert.equal(requested.status(),200,await requested.text());const returnResult=await requested.json();
 await expect(page.getByRole('status')).toContainText('Return request submitted.');results.push({returnRequested:returnResult.returnId,evidenceId:uploaded.id});
 } else { assert.equal(existingReturn.state,'requested');results.push({existingReturnVerified:existingReturn.id,state:existingReturn.state}); }
 await page.goto(base+'/en/trade-in',{waitUntil:'networkidle'});
 const catalogue=await customer.get('/api/catalogue?locale=en');const product=catalogue.products.find(p=>p.modelCode==='P13'||p.slug==='iphone13');assert.ok(product);
 await page.getByRole('combobox',{name:'Apple product',exact:true}).click();await page.getByRole('option',{name:product.name,exact:true}).click();
 await page.getByRole('combobox',{name:'Configuration (optional)',exact:true}).click();await page.getByRole('option',{name:Object.values(product.variants[0].options).join(' · '),exact:true}).click();
 await page.getByLabel('IMEI / Serial (if applicable)',{exact:true}).fill(`WEB${Date.now()}`);
 const tradeResponse=page.waitForResponse(r=>r.url().endsWith('/api/commands')&&r.request().postDataJSON()?.command==='request_trade_in');
 await page.getByRole('button',{name:'Submit trade-in request',exact:true}).click();
 const trade=await tradeResponse;assert.equal(trade.status(),200,await trade.text());results.push({tradeIn:await trade.json()});
 await expect(page.getByRole('heading',{name:'Trade-in request submitted',exact:true})).toBeVisible();
 await page.goto(base+'/en/account',{waitUntil:'networkidle'});
 await expect(page.locator('main')).toContainText('Silver');
 await page.screenshot({path:'artifacts/web/account-live-desktop.png',fullPage:true});
 assert.deepEqual(errors,[]);
 console.log(JSON.stringify(results,null,2));
} catch(error) {results.push({failure:error.message});await page.screenshot({path:'artifacts/web/aftersales-failure.png',fullPage:true});throw error;}
finally {await writeFile('artifacts/web/aftersales-results.json',JSON.stringify({at:new Date().toISOString(),results,pageErrors:errors},null,2));await Promise.all(actors.map(a=>a.dispose()));await browser.close();}
