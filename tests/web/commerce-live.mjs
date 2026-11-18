import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {writeFile,mkdir} from 'node:fs/promises';
import {base,client} from './live-api.mjs';
const actors=[];const browser=await chromium.launch({channel:'msedge',headless:true});const context=await browser.newContext({viewport:{width:1365,height:900},reducedMotion:'reduce'});const page=await context.newPage();const errors=[];page.on('pageerror',error=>errors.push(error.message));const results=[];
await mkdir('artifacts/web',{recursive:true});
try{
 const customer=await client('customer@botzone.local');actors.push(customer);const catalogue=await customer.get('/api/catalogue?locale=en');const product=catalogue.products.find(p=>p.slug==='ap2');assert.ok(product);
 await customer.command('save_cart',{items:[{variantId:product.variants[0].id,quantity:1,care:false}],promotionCode:''});
 await page.goto(base+'/en/products/ap2',{waitUntil:'networkidle'});await page.getByRole('button',{name:'Add to bag',exact:true}).click();
 await page.goto(base+'/en/checkout',{waitUntil:'networkidle'});await page.waitForURL('**/en/login?next=*');
 await page.getByLabel('Email',{exact:true}).fill('customer@botzone.local');await page.getByLabel('Password',{exact:true}).fill('BotZoneDemo!2026');await page.locator('form').getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/en/checkout');
 await page.getByLabel('Recipient',{exact:true}).fill('BotZone Browser Test');await page.getByLabel('Phone number',{exact:true}).fill('0901234567');await page.getByLabel('Province / City',{exact:true}).fill('Thành phố Hồ Chí Minh');await page.getByLabel('Ward',{exact:true}).fill('Bến Thành');await page.getByLabel('Street address',{exact:true}).fill('123 Demo Street');
 const merged=await customer.get('/api/cart');assert.equal(merged.items.find(x=>x.variantId===product.variants[0].id).quantity,1);results.push('Guest/cart merge uses maximum quantity without duplication');
 await page.getByRole('button',{name:'Review order',exact:true}).click();await expect(page.getByRole('button',{name:'Confirm order',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Confirm order',exact:true}).click();await page.waitForURL(/\/en\/orders\/[^/]+$/);const orderId=page.url().split('/').at(-1);results.push({createdOrder:orderId,method:'cod'});
 const staff=await client('nv01@botzone.local'),warehouse=await client('warehouse@botzone.local');actors.push(staff,warehouse);await staff.command('confirm_order',{orderId});const handoff=await warehouse.command('handoff',{orderId});results.push({handoffInvoice:handoff.invoiceId});
 await page.reload({waitUntil:'networkidle'});await page.getByRole('link',{name:'View invoice',exact:true}).click();await expect(page.locator('main')).toContainText(handoff.invoiceNo);await page.screenshot({path:'artifacts/web/customer-invoice-desktop.png',fullPage:true});
 const invoice=await customer.get('/api/invoices/'+handoff.invoiceId);assert.ok(invoice.invoice);const denied=await(await client('customer2@botzone.local')).http.get('/api/invoices/'+handoff.invoiceId);assert.ok([403,404].includes(denied.status()));results.push('Customer invoice visible; second customer denied');
 await staff.command('shipment_event',{orderId,status:'in_transit'});await staff.command('shipment_event',{orderId,status:'delivered'});const payment=await staff.command('start_payment',{orderId});await staff.command('record_payment',{attemptId:payment.id,outcome:'success',amountVnd:payment.requested_amount_vnd});const final=await customer.get('/api/orders/'+orderId);assert.equal(final.order.order_status,'completed');results.push('COD delivered and receipt reconciled to completed');
 assert.deepEqual(errors,[]);console.log(JSON.stringify(results,null,2));
}catch(error){results.push({failure:error.message});await page.screenshot({path:'artifacts/web/commerce-failure.png',fullPage:true});throw error;}
finally{await writeFile('artifacts/web/commerce-results.json',JSON.stringify({at:new Date().toISOString(),results,pageErrors:errors},null,2));await Promise.all(actors.map(a=>a.dispose()));await browser.close();}
