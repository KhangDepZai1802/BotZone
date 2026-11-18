import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {writeFile} from 'node:fs/promises';
import {base,client} from './live-api.mjs';
const customer=await client('customer@botzone.local');const browser=await chromium.launch({channel:'msedge',headless:true});const results=[];
try{
 const context=await browser.newContext({storageState:await customer.http.storageState(),reducedMotion:'reduce'});const page=await context.newPage();await page.goto(base+'/en/account',{waitUntil:'networkidle'});
 const overview=page.getByRole('tab',{name:'Overview',exact:true});await overview.focus();await overview.press('ArrowRight');const credit=page.getByRole('tab',{name:'Credit & points',exact:true});await expect(credit).toBeFocused();await expect(credit).toHaveAttribute('aria-selected','true');
 await credit.press('End');await expect(page.getByRole('tab',{name:'Notifications',exact:true})).toBeFocused();await page.keyboard.press('Home');await expect(overview).toBeFocused();await expect(page.getByRole('tabpanel')).toHaveAttribute('aria-labelledby','account-tab-overview');
 await page.getByRole('tab',{name:'Profile & addresses',exact:true}).click();await expect(page.getByRole('heading',{name:'Your profile',exact:true})).toBeVisible();
 assert.equal(await page.locator('[role=tab][tabindex="0"]').count(),1);results.push('Account tabs support arrows/Home/End, roving focus, selected state, linked panel and profile access');
 console.log(JSON.stringify(results));
}finally{await writeFile('artifacts/web/account-keyboard-results.json',JSON.stringify({at:new Date().toISOString(),results},null,2));await browser.close();await customer.dispose();}
