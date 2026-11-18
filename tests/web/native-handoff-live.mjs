import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {base,client} from './live-api.mjs';
const verify=process.argv.includes('--verify');
const browser=await chromium.launch({channel:'msedge',headless:true});
const state='artifacts/web/native-browser-state.json';
const checkpoint='artifacts/web/native-handoff-pending.json';
await mkdir('artifacts/web',{recursive:true});
const context=await browser.newContext({viewport:{width:1366,height:900},reducedMotion:'reduce',...(verify?{storageState:state}:{})});
const page=await context.newPage();const errors=[];page.on('pageerror',error=>errors.push(error.message));
try{
 if(!verify){
  const customer=await client('customer@botzone.local');const catalogue=await customer.get('/api/catalogue?locale=en');const product=catalogue.products.find(p=>p.slug==='ap2');assert.ok(product);
  await customer.command('save_cart',{items:[{variantId:product.variants[0].id,quantity:1,care:false}],promotionCode:''});await customer.dispose();
  await page.goto(base+'/en/products/ap2',{waitUntil:'networkidle'});await page.getByRole('button',{name:'Add to bag',exact:true}).click();await page.goto(base+'/en/checkout',{waitUntil:'networkidle'});await page.getByRole('link',{name:'Sign in / Create account',exact:true}).click();await page.getByLabel('Email',{exact:true}).fill('customer@botzone.local');await page.getByLabel('Password',{exact:true}).fill('BotZoneDemo!2026');await page.locator('form').getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/en/checkout');
  await page.getByLabel('Recipient',{exact:true}).fill('Khách thử web tới WinForms');await page.getByLabel('Phone number',{exact:true}).fill('0901234567');await page.getByLabel('Province / City',{exact:true}).fill('Thành phố Hồ Chí Minh');await page.getByLabel('Ward',{exact:true}).fill('Phường Sài Gòn');await page.getByLabel('Street address',{exact:true}).fill('123 Nguyễn Huệ');
  await page.getByRole('button',{name:'Review order',exact:true}).click();await expect(page.getByRole('button',{name:'Confirm order',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Confirm order',exact:true}).click();await page.waitForURL(/\/en\/orders\/[^/]+$/);
  const result={createdAt:new Date().toISOString(),orderId:page.url().split('/').at(-1),url:page.url(),status:'waiting-for-native-fulfillment',method:'cod'};
  await context.storageState({path:state});await writeFile(checkpoint,JSON.stringify(result,null,2));await page.screenshot({path:'artifacts/web/native-order-created.png',fullPage:true});console.log(JSON.stringify(result));
 }else{
  const pending=JSON.parse(await readFile(checkpoint,'utf8'));await page.goto(pending.url,{waitUntil:'networkidle'});await page.getByRole('link',{name:'View invoice',exact:true}).click();await page.waitForURL(/\/en\/invoices\/[^/]+$/);await expect(page.locator('main')).toContainText('BZ-HD-');
  const result={...pending,verifiedAt:new Date().toISOString(),status:'customer-opened-invoice-after-native-fulfillment',invoiceId:page.url().split('/').at(-1),pageErrors:errors};assert.notEqual(result.invoiceId,pending.orderId);assert.deepEqual(errors,[]);await page.screenshot({path:'artifacts/web/native-issued-invoice.png',fullPage:true});await writeFile('artifacts/web/native-handoff-results.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }
}catch(error){await page.screenshot({path:'artifacts/web/native-handoff-failure.png',fullPage:true});throw error;}
finally{await browser.close();}
