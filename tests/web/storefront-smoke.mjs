import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
import {mkdir,writeFile,readFile} from 'node:fs/promises';
const base=process.env.BOTZONE_WEB_URL||'http://127.0.0.1:3000';
const browser=await chromium.launch({channel:'msedge',headless:true});
const context=await browser.newContext({viewport:{width:1365,height:900},reducedMotion:'reduce'});
const page=await context.newPage();
const errors=[];page.on('pageerror',error=>errors.push(error.message));
const results=[];
await mkdir('artifacts/web',{recursive:true});
try{
  for(const width of [360,390,768,1365]){
    await page.setViewportSize({width,height:900});
    for(const route of ['/vi/login','/en/cart','/vi/compare','/en/care','/vi/account','/en/policy/returns']){
      const response=await page.goto(base+route,{waitUntil:'networkidle'});
      assert.equal(response.status(),200,route);
      await page.locator('main').waitFor();
      const dimensions=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,lang:document.documentElement.lang}));
      assert.ok(dimensions.scroll<=width+1,`${route} overflows at ${width}: ${dimensions.scroll}`);
      assert.equal(dimensions.lang,route.startsWith('/vi')?'vi':'en');
      results.push({route,width,status:response.status(),horizontalOverflow:false});
    }
  }
  await page.setViewportSize({width:390,height:844});
  await page.goto(base+'/vi/products?category=iphone',{waitUntil:'networkidle'});
  await page.getByRole('button',{name:'Mở menu',exact:true}).click();
  await page.locator('.mobile-navigation').waitFor();
  await page.getByRole('button',{name:'Mở menu',exact:true}).click();
  await page.locator('a.language').click();
  await page.waitForURL('**/en/products?category=iphone');
  await page.getByRole('combobox',{name:'Display currency',exact:true}).click();
  await page.getByRole('option',{name:'USD',exact:true}).click();
  await page.reload({waitUntil:'networkidle'});
  assert.equal(await page.getByRole('combobox',{name:'Display currency',exact:true}).textContent(),'USD');
  await page.goto(base+'/vi/login?next=checkout',{waitUntil:'networkidle'});
  assert.equal(await page.locator('.site-header,.site-footer,.mobile-navigation').count(),0,'Authentication uses its own shell');
  await page.locator('a.language').click();
  await page.waitForURL('**/en/login?next=checkout');
  await page.getByLabel('Email',{exact:true}).fill('invalid@example.test');
  await page.screenshot({path:'artifacts/web/login-mobile.png',fullPage:true});
  await page.setViewportSize({width:1365,height:900});
  await page.goto(base+'/en/care',{waitUntil:'networkidle'});
  await page.screenshot({path:'artifacts/web/care-desktop.png',fullPage:true});
  for(const name of ['phone-illustration','duo-illustration']){
    const bytes=await readFile(`public/models/${name}.glb`);
    assert.equal(bytes.readUInt32LE(0),0x46546c67);assert.equal(bytes.readUInt32LE(4),2);assert.equal(bytes.readUInt32LE(8),bytes.length);
    const json=JSON.parse(bytes.subarray(20,20+bytes.readUInt32LE(12)).toString());
    assert.ok(json.buffers.every(buffer=>!buffer.uri));
    assert.ok((json.images||[]).every(image=>!image.uri));
    if(name.startsWith('duo'))assert.ok(json.nodes.some(node=>node.name==='Hinge'));
    results.push({asset:name,validSelfContainedGLB:true,bytes:bytes.length});
  }
  assert.deepEqual(errors,[],'Uncaught browser exceptions');
  await writeFile('artifacts/web/smoke-results.json',JSON.stringify({executedAt:new Date().toISOString(),base,scope:'Real browser navigation, responsive unauthenticated states, language/currency persistence, self-contained GLB structure. No API business-flow assertion.',results,pageErrors:errors},null,2));
  console.log(`Passed ${results.length} responsive/asset checks plus navigation and preference checks.`);
}finally{await browser.close();}
