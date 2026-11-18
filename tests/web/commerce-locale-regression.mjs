import {chromium,expect,request} from '@playwright/test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {mkdir,writeFile} from 'node:fs/promises';

const base=process.env.BOTZONE_AUTH_WEB_URL??'http://127.0.0.1:3001';
assert.ok(['localhost','127.0.0.1'].includes(new URL(base).hostname)&&new URL(base).port==='3001','Authenticated regression uses the explicit clone only');
const output='artifacts/web/commerce-locale-regression';await mkdir(output,{recursive:true});
const browser=await chromium.launch({channel:'msedge',headless:true});
const results=[],errors=[],commands=[];
const routes=[
 '/orders/557429fb-3c9e-49cc-97f2-a1096aa5a946',
 '/orders/10709b3c-7550-4dd6-a73a-a5cfc5058a6c',
 '/orders/45ddb0ed-e133-4c93-b25d-2e33628c270f',
 '/invoices/6a7dc04b-54c1-40be-881d-d94e2c125eda',
 '/invoices/e0671b6c-c481-4413-8c61-e19ac99475ea',
];
let page;
async function check(name,run){try{const evidence=await run();results.push({name,passed:true,...(evidence?{evidence}:{})});console.log('PASS '+name);}catch(error){results.push({name,passed:false,error:error.message});throw error;}}
async function language(locale,mode){await page.locator('a.language').click();await page.waitForURL(url=>url.pathname===`/${locale}/${mode}`);await expect(page.locator('html')).toHaveAttribute('lang',locale);}
async function noSecretsInStorage(secret){assert.ok(!(await page.evaluate(()=>JSON.stringify({...localStorage,...sessionStorage}))).includes(secret));assert.ok(!page.url().includes(secret));}
const http=await request.newContext({baseURL:base});
try{
 const anonymous=await browser.newContext({viewport:{width:390,height:844},reducedMotion:'reduce'});page=await anonymous.newPage();page.on('pageerror',error=>errors.push(error.message));
 await check('Unsubmitted login email and remember preference survive EN/VI/EN; passwords never transfer',async()=>{
  await page.goto(base+'/en/login?next=checkout',{waitUntil:'networkidle'});await page.locator('#auth-email').fill('locale-draft@example.invalid');await page.locator('#auth-password').fill('TransientUiSecret!2026');await page.locator('[name="rememberMe"]').check();
  await language('vi','login');await expect(page.locator('#auth-email')).toHaveValue('locale-draft@example.invalid');await expect(page.locator('#auth-password')).toHaveValue('');await expect(page.locator('[name="rememberMe"]')).toBeChecked();
  await page.locator('#auth-password').fill('SecondTransient!2026');await language('en','login');await expect(page.locator('#auth-email')).toHaveValue('locale-draft@example.invalid');await expect(page.locator('#auth-password')).toHaveValue('');await noSecretsInStorage('TransientUiSecret!2026');await noSecretsInStorage('SecondTransient!2026');await noSecretsInStorage('locale-draft@example.invalid');
  await page.screenshot({path:`${output}/login-draft-en-390.png`,fullPage:true});
  await page.getByRole('link',{name:'Back to the store',exact:true}).click();await page.waitForURL('**/en/products');await page.goBack({waitUntil:'networkidle'});await expect(page.locator('#auth-email')).toHaveValue('');await expect(page.locator('[name="rememberMe"]')).not.toBeChecked();
 });
 await check('Registration identity/address drafts survive locale switch in memory; password/confirm clear',async()=>{
  await page.setViewportSize({width:1440,height:1000});await page.goto(base+'/en/register',{waitUntil:'networkidle'});await page.locator('#auth-name').fill('Locale Draft Customer');await page.locator('#auth-email').fill('registration-draft@example.invalid');await page.locator('#auth-phone').fill('0901234567');await page.locator('#auth-password').fill('RegistrationSecret!2026');await page.locator('#auth-confirmPassword').fill('RegistrationSecret!2026');await page.locator('[name="saveAddress"]').check();
  await page.locator('#auth-address-province').fill('Ho Chi Minh City');await page.locator('#auth-address-ward').fill('Ben Thanh');await page.locator('#auth-address-addressLine').fill('123 Draft Street');
  for(const locale of ['vi','en']){await language(locale,'register');for(const [id,value]of Object.entries({name:'Locale Draft Customer',email:'registration-draft@example.invalid',phone:'0901234567','address-recipient':'Locale Draft Customer','address-phone':'0901234567','address-province':'Ho Chi Minh City','address-ward':'Ben Thanh','address-addressLine':'123 Draft Street'}))await expect(page.locator('#auth-'+id)).toHaveValue(value);await expect(page.locator('[name="saveAddress"]')).toBeChecked();await expect(page.locator('#auth-password')).toHaveValue('');await expect(page.locator('#auth-confirmPassword')).toHaveValue('');}
  await noSecretsInStorage('RegistrationSecret!2026');await noSecretsInStorage('registration-draft@example.invalid');await page.screenshot({path:`${output}/register-draft-en-1440.png`,fullPage:true});
 });await anonymous.close();

 const csrf=await(await http.get('/api/auth/csrf')).json();const login=await http.post('/api/auth/login',{headers:{'X-CSRF-TOKEN':csrf.token,'Accept-Language':'en'},data:{email:'customer@botzone.local',password:'BotZoneDemo!2026',clientType:'web'}});assert.equal(login.status(),200);
 const before=new Map();for(const route of routes){const response=await http.get('/api'+route);assert.equal(response.status(),200);before.set(route,(await response.json()).items);}
 const context=await browser.newContext({storageState:await http.storageState(),viewport:{width:390,height:844},reducedMotion:'reduce'});page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));page.on('request',req=>{if(req.method()==='POST'&&req.url().endsWith('/api/commands'))commands.push(req.postDataJSON()?.command);});
 for(const width of [390,1440])for(const locale of ['en','vi'])for(const [index,route] of routes.entries())await check(`${width}px ${locale} ${route}`,async()=>{
  await page.setViewportSize({width,height:width===390?844:1000});await page.goto(base+'/'+locale+route,{waitUntil:'networkidle'});await expect(page.locator('[data-line-kind]').first()).toBeVisible();
  const dimensions=await page.evaluate(()=>({viewport:innerWidth,document:document.documentElement.scrollWidth}));assert.ok(dimensions.document<=width+1,JSON.stringify(dimensions));
  const shipping=page.locator('[data-line-kind="shipping"]');if(await shipping.count()){await expect(shipping.locator('strong')).toHaveText(locale==='en'?'Delivery in Vietnam':'Giao hàng Việt Nam');await expect(shipping.locator('.muted')).toHaveText(locale==='en'?'Delivery service':'Dịch vụ vận chuyển');}
  const scroller=page.locator('.table-scroll').first();const scrolling=await scroller.evaluate(el=>({client:el.clientWidth,content:el.scrollWidth}));
  if(scrolling.content>scrolling.client+1){
   await scroller.focus();await page.keyboard.press('ArrowRight');await expect.poll(()=>scroller.evaluate(el=>el.scrollLeft)).toBeGreaterThan(0);
   let previous=-1,stable=0;await expect.poll(async()=>{const left=await scroller.evaluate(el=>el.scrollLeft);stable=left===previous?stable+1:0;previous=left;return stable;},{intervals:[60]}).toBeGreaterThanOrEqual(3);
   await scroller.evaluate(el=>{el.scrollTo({left:0,behavior:'instant'});el.blur();});await expect.poll(()=>scroller.evaluate(el=>el.scrollLeft)).toBe(0);
  }
  await page.screenshot({path:`${output}/${width}-${locale}-${index}.png`,fullPage:true});return {...dimensions,table:scrolling};
 });
 await check('GET payloads keep all existing line-item snapshots and totals unchanged',async()=>{
  const evidence=[];for(const route of routes){const response=await http.get('/api'+route);assert.equal(response.status(),200);const items=(await response.json()).items;assert.deepEqual(items,before.get(route));evidence.push({route,items:items.length,sha256:createHash('sha256').update(JSON.stringify(items)).digest('hex')});}return evidence;
 });
 assert.ok(commands.every(command=>['save_cart','sync_wishlist'].includes(command)),'No order, checkout, payment or other business mutations');assert.deepEqual(errors,[]);await context.close();
}catch(error){console.error(error.message);await page?.screenshot({path:`${output}/failure.png`,fullPage:true}).catch(()=>{});process.exitCode=1;}
finally{await writeFile(`${output}/results.json`,JSON.stringify({at:new Date().toISOString(),base,results,pageErrors:errors,automaticSyncCommands:commands,explicitBusinessMutations:0},null,2));await http.dispose();await browser.close();}
