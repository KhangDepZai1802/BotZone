import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {writeFile} from 'node:fs/promises';
import {base} from './live-api.mjs';
const browser=await chromium.launch({channel:'msedge',headless:true});
const errors=[],results=[];
try {
 const context=await browser.newContext({viewport:{width:1440,height:1000}});
 const page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',e=>errors.push(e.message));
 await page.goto(base+'/en',{waitUntil:'networkidle'});
 await expect(page.locator('.hero .scene-poster')).toHaveCSS('opacity','0',{timeout:20000});
 assert.equal(await page.locator('canvas').count(),1);
 assert.equal(await page.locator('.scene-container').first().evaluate(el=>getComputedStyle(el).touchAction),'pan-y');
 const first=page.getByRole('button',{name:'Explore in 3D',exact:true}).first();
 await first.scrollIntoViewIfNeeded();const scrollBefore=await page.evaluate(()=>scrollY);await first.click();
 const dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();await expect(page.locator('canvas')).toHaveCount(1);await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready','true');
 await dialog.getByRole('button',{name:'Color 2',exact:true}).click();await expect(dialog.getByRole('button',{name:'Color 2',exact:true})).toHaveAttribute('aria-pressed','true');
 const rotation=dialog.getByLabel('Keyboard rotation');const before=await rotation.inputValue();await rotation.focus();await rotation.press('ArrowRight');assert.notEqual(await rotation.inputValue(),before);
 const canvas=dialog.locator('canvas'),box=await canvas.boundingBox();assert.ok(box);await page.mouse.move(box.x+box.width*.4,box.y+box.height*.4);await page.mouse.down();await page.mouse.move(box.x+box.width*.6,box.y+box.height*.5,{steps:8});await page.mouse.up();
 await dialog.getByRole('button',{name:'Reset view',exact:true}).click();await expect(dialog.getByRole('button',{name:'Color 1',exact:true})).toHaveAttribute('aria-pressed','true');
 await dialog.getByRole('button',{name:'Đóng / Close',exact:true}).click();await expect(dialog).toHaveCount(0);await expect(first).toBeFocused();assert.ok(Math.abs(await page.evaluate(()=>scrollY)-scrollBefore)<3);
 results.push('One active canvas; pan-y; phone explore color/keyboard/drag/reset; close restores focus and scroll');
 const duo=page.locator('.duo-section');await duo.scrollIntoViewIfNeeded();await expect(duo.locator('.scene-poster')).toHaveCSS('opacity','0',{timeout:20000});await expect(page.locator('canvas')).toHaveCount(1);
 await duo.getByRole('button',{name:'Explore in 3D',exact:true}).click();const hinge=dialog.getByLabel('Hinge angle');await hinge.fill('150');assert.equal(await hinge.inputValue(),'150');await hinge.press('Home');assert.equal(await hinge.inputValue(),'0');await dialog.getByRole('button',{name:'Reset view',exact:true}).click();assert.equal(await hinge.inputValue(),'90');await expect(dialog.locator('.explore-canvas')).toHaveAttribute('data-ready','true');await page.screenshot({path:'artifacts/web/duo-explore.png'});await page.keyboard.press('Escape');
 results.push('Duo local GLB loads; hinge0..180 responds to keyboard; reset restores saved90degree configuration');
 await context.close();
 const reduced=await browser.newContext({reducedMotion:'reduce'}),rp=await reduced.newPage();rp.on('pageerror',e=>errors.push(e.message));
 for(const locale of ['vi','en'])for(const width of [360,390,430,768,1024,1366,1440]){
  await rp.setViewportSize({width,height:900});await rp.goto(`${base}/${locale}`,{waitUntil:'networkidle'});
  await expect(rp.locator('.hero h1')).toBeVisible();assert.equal(await rp.locator('canvas').count(),0);
  const dimensions=await rp.evaluate(()=>({screen:innerWidth,scroll:document.documentElement.scrollWidth}));assert.ok(dimensions.scroll<=width+1,`${locale}/${width}: horizontal overflow ${dimensions.scroll}`);
  assert.equal(await rp.locator('html').getAttribute('lang'),locale);
  results.push({locale,width,reducedMotion:'poster only',overflow:false});
  if(locale==='vi'&&width===390)await rp.screenshot({path:'artifacts/web/landing-mobile.png',fullPage:true});
 }
 for(const width of [377,503,641,900,1197]){
  await rp.setViewportSize({width,height:900});await rp.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
  assert.ok(await rp.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`Resize sweep overflow at ${width}`);
 }
 results.push('Continuous resize sweep377/503/641/900/1197px: no horizontal overflow');
 await reduced.close();
 const mobile=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true});const mp=await mobile.newPage();mp.on('pageerror',e=>errors.push(e.message));await mp.goto(base+'/en',{waitUntil:'networkidle'});await expect(mp.locator('.hero .scene-poster')).toHaveCSS('opacity','0',{timeout:20000});await expect(mp.locator('canvas')).toHaveCount(1);
 const cdp=await mobile.newCDPSession(mp);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:180,y:650}]});for(const y of [600,550,500,450,400,350])await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:180,y}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await expect.poll(()=>mp.evaluate(()=>scrollY)).toBeGreaterThan(50);results.push('Mobile390px plays one lightweight3D scene; actual touch swipe scrolls vertically');await mobile.close();
 const fallback=await browser.newContext({viewport:{width:390,height:844}});
 await fallback.addInitScript(()=>{const get=HTMLCanvasElement.prototype.getContext;HTMLCanvasElement.prototype.getContext=function(type,...args){return String(type).startsWith('webgl')?null:get.call(this,type,...args);};});
 const fp=await fallback.newPage();fp.on('pageerror',e=>errors.push(e.message));await fp.goto(base+'/en',{waitUntil:'networkidle'});await fp.getByRole('button',{name:'Explore in 3D',exact:true}).first().click();await expect(fp.getByText('WebGL is unavailable. Product information and purchasing remain available.',{exact:true})).toBeVisible();await expect(fp.locator('canvas')).toHaveCount(0);results.push('WebGL unavailable: poster fallback and product CTA remain available');
 await fallback.close();assert.deepEqual(errors,[]);console.log(JSON.stringify(results,null,2));
}finally{await writeFile('artifacts/web/landing-results.json',JSON.stringify({at:new Date().toISOString(),results,pageErrors:errors},null,2));await browser.close();}
