import {chromium, expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {mkdir, writeFile} from 'node:fs/promises';

const base=process.env.BOTZONE_AUTH_WEB_URL??'http://127.0.0.1:3001';
const mutate=process.env.BOTZONE_AUTH_CLONE==='1';
if(mutate) assert.ok(['localhost','127.0.0.1'].includes(new URL(base).hostname)&&new URL(base).port==='3001','Auth mutations require the explicitly prepared clone web runtime on port 3001.');
const output=process.env.BOTZONE_AUTH_ARTIFACTS??'artifacts/web/auth-ui';
await mkdir(output,{recursive:true});
const browser=await chromium.launch({channel:'msedge',headless:true});
const context=await browser.newContext({viewport:{width:1440,height:1000},reducedMotion:'reduce'});
const page=await context.newPage();
const results=[],errors=[];
page.on('pageerror',error=>errors.push(error.message));
async function test(name,run){try{await run();results.push({name,passed:true});console.log('PASS '+name);}catch(error){results.push({name,passed:false,error:error.message});throw error;}}
async function navigate(route){const response=await page.goto(base+route,{waitUntil:'networkidle'});assert.equal(response.status(),200);await page.locator('[data-auth-page]').waitFor();}
async function call(route,body,expected=200){return page.evaluate(async({route,body,expected})=>{
  const token=body===undefined?null:(await(await fetch('/api/auth/csrf',{credentials:'same-origin'})).json()).token;
  const response=await fetch(route,{method:body===undefined?'GET':'POST',credentials:'same-origin',headers:{'Accept-Language':'en',...(body===undefined?{}:{'Content-Type':'application/json','X-CSRF-TOKEN':token})},...(body===undefined?{}:{body:JSON.stringify(body)})});
  const data=await response.json();if(response.status!==expected)throw Error(`${route}: expected ${expected}, received ${response.status}: ${data.error?.code??''}`);return data;
},{route,body,expected});}
async function mail(email,kind){for(let i=0;i<30;i++){
  const response=await fetch('http://127.0.0.1:8025/api/v1/messages');assert.equal(response.status,200);
  for(const summary of (await response.json()).messages??[]){if(!(summary.To??[]).some(to=>to.Address===email))continue;
    const detail=await(await fetch('http://127.0.0.1:8025/api/v1/message/'+summary.ID)).json();
    const link=(detail.Text??'').match(/https?:\/\/[^\s]+/)?.[0];if(link?.includes(kind))return {url:new URL(link),subject:detail.Subject};
  }await new Promise(resolve=>setTimeout(resolve,1000));
}throw Error('No matching test email delivered for '+kind);}
async function submit(name){await page.locator('form').getByRole('button',{name,exact:true}).click();}
async function login(email,password,remember=false){await navigate('/en/login');await page.locator('#auth-email').fill(email);await page.locator('#auth-password').fill(password);await page.locator('[name="rememberMe"]').setChecked(remember);await submit('Sign in');await page.waitForURL('**/en/account');}
async function secondSession(email,password){const peer=await browser.newContext();const token=await(await peer.request.get(base+'/api/auth/csrf')).json();const response=await peer.request.post(base+'/api/auth/login',{headers:{'X-CSRF-TOKEN':token.token,'Accept-Language':'en'},data:{email,password,clientType:'web'}});assert.equal(response.status(),200);return peer;}
async function register(email,password,withAddress){await navigate('/en/register');await page.locator('#auth-name').fill('Auth UI Customer');await page.locator('#auth-email').fill(email);await page.locator('#auth-phone').fill('0901234567');await page.locator('#auth-password').fill(password);await page.locator('#auth-confirmPassword').fill(password);
  if(withAddress){await page.locator('[name="saveAddress"]').check();await page.locator('#auth-address-province').fill('Ho Chi Minh City');await page.locator('#auth-address-ward').fill('Ben Thanh');await page.locator('#auth-address-addressLine').fill('123 Demo Street');}
  const request=page.waitForRequest(request=>request.url().endsWith('/api/auth/register')&&request.method()==='POST');await submit('Create account');const body=(await request).postDataJSON();assert.equal(body.phone,'0901234567');assert.equal(Object.hasOwn(body,'role'),false);assert.equal(Object.hasOwn(body,'address'),withAddress);if(withAddress)assert.equal(body.address.country,'VN');await expect(page.locator('[data-auth-page]').getByRole('status')).toContainText('Your account is ready.');
}
try{
 await test('Login/register layouts, localization and standalone shell at 390/430/1366/1440',async()=>{
  for(const width of [390,430,1366,1440])for(const locale of ['vi','en'])for(const route of ['login','register']){
   await page.setViewportSize({width,height:width<500?844:1000});await navigate(`/${locale}/${route}?next=checkout`);
   assert.equal(await page.locator('.site-header,.site-footer,.announcement').count(),0);
   assert.equal(await page.locator('html').getAttribute('lang'),locale);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${locale}/${route} overflows at ${width}`);
   const expected=locale==='vi'?(route==='login'?'Chào bạn trở lại.':'Bắt đầu kết nối.'):(route==='login'?'Welcome back.':'Make yourself at home.');
   await expect(page.getByRole('heading',{level:1})).toHaveText(expected);
   await page.screenshot({path:`${output}/${route}-${locale}-${width}.png`,fullPage:true});
  }
 });
 await test('Password eye, no saved password, remember checkbox and return path navigation',async()=>{
  await navigate('/en/login?next=checkout');const password=page.locator('#auth-password');await password.fill('VisibilityTest!');
  const toggle=page.getByRole('button',{name:'Show password',exact:true});const box=await toggle.boundingBox();assert.ok(box.width>=44&&box.height>=44);await toggle.click();await expect(password).toHaveAttribute('type','text');await expect(page.getByRole('button',{name:'Hide password',exact:true})).toHaveAttribute('aria-pressed','true');await page.getByRole('button',{name:'Hide password',exact:true}).click();await expect(password).toHaveAttribute('type','password');
  await expect(page.locator('[name="rememberMe"]')).not.toBeChecked();await expect(page.locator('main')).toContainText('up to 8 hours');
  assert.ok(!(await page.evaluate(()=>JSON.stringify({...localStorage,...sessionStorage}))).includes('VisibilityTest!'));
  await page.getByRole('link',{name:'Create an account',exact:true}).click();await page.waitForURL('**/en/register?next=%2Fen%2Fcheckout');await expect(page.locator('#auth-password')).toHaveValue('');
  await page.locator('a.language').click();await page.waitForURL(url=>url.pathname==='/vi/register');assert.ok(['checkout','/en/checkout'].includes(new URL(page.url()).searchParams.get('next')));await expect(page.getByRole('heading',{level:1})).toHaveText('Bắt đầu kết nối.');
 });
 await test('Client validation focuses actionable summary and optional address has native labels',async()=>{
  await navigate('/en/register');let authPosts=0;const observer=request=>{if(request.url().includes('/api/auth/')&&request.method()==='POST')authPosts++;};page.on('request',observer);
  await submit('Create account');await expect(page.locator('[data-auth-page]').getByRole('alert')).toBeFocused();await page.locator('[data-auth-page]').getByRole('alert').getByRole('link').filter({hasText:'Phone number:'}).click();await expect(page.locator('#auth-phone')).toBeFocused();
  await page.locator('[name="saveAddress"]').check();await expect(page.getByRole('group',{name:'Delivery address',exact:true})).toBeVisible();await expect(page.getByLabel('Country',{exact:true})).toHaveValue('Vietnam');await submit('Create account');await expect(page.locator('[data-auth-page]').getByRole('alert')).toContainText('Street address:');
  assert.equal(authPosts,0,'Invalid forms must not call backend');page.off('request',observer);await page.screenshot({path:`${output}/register-validation.png`,fullPage:true});
 });
 await test('Recovery and missing-token pages stay localized',async()=>{
  for(const locale of ['vi','en'])for(const route of ['forgot-password','reset-password','verify-email','change-password']){
   await navigate(`/${locale}/${route}`);if(['reset-password','verify-email'].includes(route))await expect(page.locator('[data-auth-page]').getByRole('alert')).toContainText(locale==='vi'?'Liên kết chưa đầy đủ':'This link is incomplete');
   if(route==='change-password')await expect(page.locator('main')).toContainText(locale==='vi'?'Đăng nhập trước khi đổi mật khẩu':'Sign in before changing your password');
  }
 });
 if(mutate){
  const shortPolicy=process.env.BOTZONE_AUTH_SHORT_PASSWORDS==='1';
  const suffix=Date.now(),email=`auth-ui-${suffix}@botzone.local`,addressEmail=`auth-ui-address-${suffix}@botzone.local`,password=shortPolicy?'123456':'AuthUiStart!2026',changed=shortPolicy?'aaaaaa':'AuthUiChanged!2026',reset=shortPolicy?'ABCDEF':'AuthUiReset!2026';
  if(shortPolicy)await test('Six-character policy: bilingual hints and rejection below six on client and API',async()=>{
    for(const locale of ['vi','en']){
      await navigate(`/${locale}/register`);
      await expect(page.locator('#auth-password-hint')).toHaveText(locale==='vi'?'Ít nhất 6 ký tự.':'At least 6 characters.');
      await page.locator('#auth-name').fill('Short Password Test');await page.locator('#auth-email').fill(`short-${locale}-${suffix}@botzone.local`);await page.locator('#auth-phone').fill('0901234567');
      await page.locator('#auth-password').fill('12345');await page.locator('#auth-confirmPassword').fill('12345');
      await submit(locale==='vi'?'Tạo tài khoản':'Create account');await expect(page.locator('#auth-password')).toHaveAttribute('aria-invalid','true');
      await expect(page.locator('#auth-password-error')).toContainText('6');
      const result=await page.evaluate(async locale=>{
        const token=(await(await fetch('/api/auth/csrf')).json()).token;
        const response=await fetch('/api/auth/register',{method:'POST',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':token,'Accept-Language':locale},body:JSON.stringify({name:'Short Password Test',email:`short-api-${locale}-${Date.now()}@botzone.local`,phone:'0901234567',password:'12345'})});
        const body=await response.json();return {status:response.status,code:body.error?.code,message:body.error?.message};
      },locale);
      assert.equal(result.status,400);assert.equal(result.code,'PASSWORD_POLICY');assert.equal(result.message,locale==='vi'?'Mật khẩu phải có ít nhất 6 ký tự.':'Use a password with at least 6 characters.');
      await page.screenshot({path:`${output}/six-character-policy-${locale}.png`,fullPage:true});
    }
  });
  await test('Real registration without address; English verification mail; public customer role',async()=>{
   await register(email,password,false);const delivered=await mail(email,'verify-email');assert.equal(delivered.url.pathname,'/en/verify-email');assert.match(delivered.subject,/Verify your BotZone email/);
   await navigate(delivered.url.pathname+delivered.url.search);await submit('Verify email');await expect(page.locator('[data-auth-page]').getByRole('status')).toContainText('Your email is verified.');
   await login(email,password);const session=await call('/api/session');assert.equal(session.actor.role,'customer');assert.equal(session.actor.verified,true);const account=await call('/api/account');assert.equal(account.profile.phone,'0901234567');assert.equal(account.addresses.length,0);
   const cookie=(await context.cookies(base)).find(cookie=>cookie.name==='BotZone.Session');assert.equal(cookie.expires,-1);assert.equal(cookie.httpOnly,true);await call('/api/auth/logout',{});assert.equal((await call('/api/session')).actor,null);
  });
  await test('Real registration with address and bounded persistent remember cookie',async()=>{
   await register(addressEmail,password,true);await login(addressEmail,password,true);const session=await call('/api/session');assert.equal(session.actor.role,'customer');const account=await call('/api/account');assert.equal(account.profile.phone,'0901234567');assert.equal(account.addresses.length,1);assert.equal(account.addresses[0].address_line??account.addresses[0].addressLine,'123 Demo Street');
   const cookie=(await context.cookies(base)).find(cookie=>cookie.name==='BotZone.Session'),remaining=cookie.expires-Date.now()/1000;assert.ok(remaining>7.9*3600&&remaining<=8*3600+5);assert.equal(cookie.httpOnly,true);results.push({name:'Remember cookie properties',passed:true,httpOnly:cookie.httpOnly,sameSite:cookie.sameSite,remainingSeconds:Math.round(remaining)});
  });
  await test('Change password ends sessions and sign-in uses new password',async()=>{
   const peer=await secondSession(addressEmail,password);try{await navigate('/en/change-password');await page.locator('#auth-currentPassword').fill(password);await page.locator('#auth-password').fill(changed);await page.locator('#auth-confirmPassword').fill(changed);await submit('Save new password');await expect(page.locator('[data-auth-page]').getByRole('status')).toContainText('all sessions have ended');assert.equal((await call('/api/session')).actor,null);assert.equal((await(await peer.request.get(base+'/api/session')).json()).actor,null);await login(addressEmail,changed);await call('/api/auth/logout',{});}finally{await peer.close();}
  });
  await test('Forgot/reset email UI, replay rejection, session revocation and safe redirect',async()=>{
   const peer=await secondSession(addressEmail,changed);try{await login(addressEmail,changed);await navigate('/en/forgot-password');await page.locator('#auth-email').fill(addressEmail);await submit('Send reset link');await expect(page.locator('[data-auth-page]').getByRole('status')).toContainText('If the account exists');const delivered=await mail(addressEmail,'reset-password');assert.equal(delivered.url.pathname,'/en/reset-password');assert.match(delivered.subject,/Reset your BotZone password/);
   await navigate(delivered.url.pathname+delivered.url.search);await page.locator('#auth-password').fill(reset);await page.locator('#auth-confirmPassword').fill(reset);await submit('Save new password');await expect(page.locator('[data-auth-page]').getByRole('status')).toContainText('Your password has been reset');assert.equal((await call('/api/session')).actor,null);
   assert.equal((await(await peer.request.get(base+'/api/session')).json()).actor,null);await call('/api/auth/reset-password',{userId:delivered.url.searchParams.get('userId'),token:delivered.url.searchParams.get('token'),password:reset},400);
   await navigate('/en/login?next=https%3A%2F%2Fevil.example');await page.locator('#auth-email').fill(addressEmail);await page.locator('#auth-password').fill(reset);await submit('Sign in');await page.waitForURL('**/en/account');assert.equal(new URL(page.url()).origin,new URL(base).origin);await call('/api/auth/logout',{});}finally{await peer.close();}
  });
  await test('English invalid login error, signup role injection rejected, WinForms cookie stays nonpersistent',async()=>{
   await navigate('/en/login');await page.locator('#auth-email').fill(`missing-${suffix}@botzone.local`);await page.locator('#auth-password').fill(password);await submit('Sign in');await expect(page.locator('[data-auth-page]').getByRole('alert')).toContainText('The email or password is incorrect');
   const injected=await call('/api/auth/register',{name:'Blocked role test',email:`blocked-${suffix}@botzone.local`,phone:'0901234567',password,role:'owner'},400);assert.equal(injected.error.code,'VALIDATION_ERROR');
   const staff=await call('/api/auth/login',{email:'nv01@botzone.local',password:'BotZoneDemo!2026',clientType:'winforms',rememberMe:true});assert.equal(staff.actor.role,'order_staff');assert.equal((await context.cookies(base)).find(cookie=>cookie.name==='BotZone.Session').expires,-1);await call('/api/auth/logout',{});
  });
 }
 assert.deepEqual(errors,[],'No uncaught browser exceptions');
}catch(error){console.error(error.message);await page.screenshot({path:`${output}/failure.png`,fullPage:true});process.exitCode=1;}
finally{await writeFile(`${output}/results.json`,JSON.stringify({executedAt:new Date().toISOString(),base,mutationsOnExplicitClone:mutate,results,pageErrors:errors},null,2));await browser.close();}
