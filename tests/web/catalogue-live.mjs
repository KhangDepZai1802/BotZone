import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {writeFile} from 'node:fs/promises';
import {base} from './live-api.mjs';
const browser=await chromium.launch({channel:'msedge',headless:true});
const page=await browser.newPage({viewport:{width:1366,height:900},reducedMotion:'reduce'}),results=[],errors=[];
page.setDefaultTimeout(20000);page.on('pageerror',e=>errors.push(e.message));
try {
 await page.goto(base+'/en/products?category=iphone',{waitUntil:'networkidle'});
 const cards=page.locator('.product-card');await expect(cards.first()).toBeVisible();assert.ok(await cards.count()>=6);
 await page.getByLabel('Search products',{exact:true}).fill('iPhone 18');await expect(cards).toHaveCount(2);
 await page.getByRole('combobox',{name:'Sort',exact:true}).click();await page.getByRole('option',{name:'Price: high to low',exact:true}).click();
 await page.getByRole('combobox',{name:'Availability',exact:true}).click();await page.getByRole('option',{name:'In stock',exact:true}).click();
 await expect.poll(()=>new URL(page.url()).searchParams.get('stock')).toBe('available');
 await page.getByRole('link',{name:'VI',exact:true}).click();await page.waitForURL('**/vi/products?**');
 const params=new URL(page.url()).searchParams;assert.equal(params.get('q'),'iPhone 18');assert.equal(params.get('sort'),'price-desc');assert.equal(params.get('category'),'iphone');assert.equal(params.get('stock'),'available');
 await expect(page.getByLabel('Tìm sản phẩm',{exact:true})).toHaveValue('iPhone 18');results.push('Search/category/sort/availability retained when switching language');
 await page.goto(base+'/en/products?category=iphone',{waitUntil:'networkidle'});
 for(let i=0;i<3;i++)await cards.nth(i).getByRole('button',{name:'Compare',exact:true}).click();
 await cards.nth(3).getByRole('button',{name:'Compare',exact:true}).click();await expect(page.getByTestId('compare-dock')).toContainText('You have 3 products');
 assert.equal(await page.evaluate(()=>JSON.parse(localStorage.getItem('botzone.compare')).length),3);
 await cards.nth(2).getByRole('button',{name:'Compare',exact:true}).click();
 await page.goto(base+'/en/products?category=mac',{waitUntil:'networkidle'});await cards.first().getByRole('button',{name:'Compare',exact:true}).click();await expect(page.getByTestId('compare-dock')).toContainText('Choose products in the same category');
 await page.goto(base+'/en/compare',{waitUntil:'networkidle'});assert.equal(await page.locator('.compare-table thead th').count(),3);results.push('Compare enforces three-product maximum and same category; comparison table renders');
 await page.goto(base+'/en/products/iphone-18-pro-max',{waitUntil:'networkidle'});
 const variants=page.locator('.variant-option');assert.equal(await variants.count(),2);await variants.filter({hasText:'Out of stock'}).click();await expect(page.getByRole('button',{name:'Add to bag',exact:true})).toBeDisabled();await expect.poll(()=>new URL(page.url()).searchParams.get('variant')).toBeTruthy();const variant=new URL(page.url()).searchParams.get('variant');
 await page.getByRole('link',{name:'VI',exact:true}).click();await page.waitForURL('**/vi/products/iphone-18-pro-max?**');assert.equal(new URL(page.url()).searchParams.get('variant'),variant);await expect(page.getByRole('button',{name:'Thêm vào giỏ hàng',exact:true})).toBeDisabled();
 await page.getByRole('button',{name:'Yêu thích',exact:true}).click();await page.reload({waitUntil:'networkidle'});await expect(page.getByRole('button',{name:'Yêu thích',exact:true})).toHaveAttribute('aria-pressed','true');
 await page.goto(base+'/vi/wishlist',{waitUntil:'networkidle'});await expect(page.locator('.product-card')).toHaveCount(1);await expect(page.locator('.product-card h3')).toContainText('iPhone 18 Pro Max');
 results.push('Out-of-stock variant disables purchase; variant survives language switch; guest wishlist survives reload');
 assert.deepEqual(errors,[]);console.log(JSON.stringify(results,null,2));
}catch(error){results.push({failure:error.message});await page.screenshot({path:'artifacts/web/catalogue-failure.png',fullPage:true});throw error;}
finally{await writeFile('artifacts/web/catalogue-results.json',JSON.stringify({at:new Date().toISOString(),results,pageErrors:errors},null,2));await browser.close();}
