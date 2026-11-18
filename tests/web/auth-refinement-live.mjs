import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {mkdir,writeFile} from 'node:fs/promises';

const base=process.env.BOTZONE_WEB_URL??'http://127.0.0.1:3000';
const output='artifacts/web/refinement-auth';await mkdir(output,{recursive:true});
const browser=await chromium.launch({channel:'msedge',headless:true});
const context=await browser.newContext({reducedMotion:'reduce'});
const page=await context.newPage(),results=[],pageErrors=[],blockedWrites=[];
page.on('pageerror',error=>pageErrors.push(error.message));
// These are visual/invalid-form checks on main. Prevent accidental mutations before transmission.
await context.route('**/api/**',async route=>{
 const req=route.request();if(!['GET','HEAD','OPTIONS'].includes(req.method())){blockedWrites.push({method:req.method(),path:new URL(req.url()).pathname});await route.abort('blockedbyclient');}else await route.continue();
});
const viewports=[{width:1366,height:768},{width:1440,height:900},{width:390,height:844}];
async function check(name,fn){try{const evidence=await fn();results.push({name,passed:true,...(evidence?{evidence}:{})});console.log('PASS '+name);}catch(error){results.push({name,passed:false,error:error.message});throw error;}}
async function inspect(){return page.evaluate(()=>{
 const area=document.querySelector('[data-auth-page]'),form=document.querySelector('form'),button=form?.querySelector('button[type=submit]'),story=area.querySelector('aside'),shell=document.querySelector('header').parentElement;
 return {width:innerWidth,height:innerHeight,scrollWidth:document.documentElement.scrollWidth,scrollHeight:document.documentElement.scrollHeight,shellBackground:getComputedStyle(shell).backgroundImage,backgroundAttachment:getComputedStyle(shell).backgroundAttachment,storyBackground:getComputedStyle(story).backgroundColor,storyDisplay:getComputedStyle(story).display,submitBottom:button?.getBoundingClientRect().bottom};
});}
try{
 for(const viewport of viewports)for(const locale of ['vi','en'])for(const route of ['login','register'])await check(`${route} ${locale} ${viewport.width}x${viewport.height}`,async()=>{
  await page.setViewportSize(viewport);const response=await page.goto(`${base}/${locale}/${route}`,{waitUntil:'networkidle'});assert.equal(response.status(),200);await expect(page.locator('[data-auth-page]')).toHaveAttribute('data-auth-page',route);await expect(page.locator('html')).toHaveAttribute('lang',locale);
  assert.equal(await page.locator('.site-header,.announcement,.site-footer').count(),0);
  const evidence=await inspect();assert.ok(evidence.scrollWidth<=viewport.width+1,JSON.stringify(evidence));assert.match(evidence.shellBackground,/gradient/);assert.ok(evidence.backgroundAttachment.split(',').every(value=>value.trim()==='scroll'),'Gradient covers the complete document');assert.equal(evidence.storyBackground,'rgba(0, 0, 0, 0)');if(route==='login')assert.ok(evidence.submitBottom<=viewport.height,'Primary sign-in action is visible without scrolling');
  await page.screenshot({path:`${output}/${route}-${locale}-${viewport.width}x${viewport.height}.png`,fullPage:true});
  const password=page.locator('#auth-password'),eye=page.locator('button[aria-controls="auth-password"]');await password.fill('Visibility sample only');const target=await eye.boundingBox();assert.ok(target.width>=44&&target.height>=44);await eye.click();await expect(password).toHaveAttribute('type','text');await expect(eye).toHaveAttribute('aria-pressed','true');await eye.click();await expect(password).toHaveAttribute('type','password');await password.fill('');
  if(route==='login'){const remember=page.locator('[name="rememberMe"]');await expect(remember).not.toBeChecked();await remember.check();await expect(remember).toBeChecked();await remember.uncheck();}
  else{await page.locator('[name="saveAddress"]').check();await expect(page.locator('#auth-address')).toBeVisible();assert.ok((await inspect()).scrollWidth<=viewport.width+1);if(locale==='vi'&&[1366,390].includes(viewport.width))await page.screenshot({path:`${output}/register-address-${locale}-${viewport.width}.png`,fullPage:true});await page.locator('[name="saveAddress"]').uncheck();await expect(page.locator('#auth-address')).toHaveCount(0);}
  await page.locator('form button[type="submit"]').click();await expect(page.locator('[data-auth-page] [role="alert"]')).toBeFocused();assert.ok((await inspect()).scrollWidth<=viewport.width+1);
  if(locale==='en'&&viewport.width===390)await page.screenshot({path:`${output}/${route}-validation-en-390.png`,fullPage:true});
  return {...evidence,passwordEyeTarget:target.width,invalidFormFocused:true};
 });
 assert.deepEqual(blockedWrites,[],'No mutating API request was attempted');assert.deepEqual(pageErrors,[],'No uncaught browser exceptions');
}catch(error){console.error(error.message);await page.screenshot({path:`${output}/failure.png`,fullPage:true});process.exitCode=1;}
finally{await writeFile(`${output}/results.json`,JSON.stringify({at:new Date().toISOString(),base,scope:'Read-only visual/auth-control checks; all non-read API calls blocked before transmission.',results,pageErrors,blockedWrites},null,2));await browser.close();}
