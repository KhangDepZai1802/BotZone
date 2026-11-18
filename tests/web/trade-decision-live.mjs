import {chromium,expect} from '@playwright/test';
import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
import {base,client} from './live-api.mjs';
const actors=[],results=[],errors=[];
const browser=await chromium.launch({channel:'msedge',headless:true});
try{
 const customer=await client('customer@botzone.local'),warehouse=await client('warehouse@botzone.local'),owner=await client('owner@botzone.local');actors.push(customer,warehouse,owner);
 const prior=JSON.parse(await readFile('artifacts/web/aftersales-results.json','utf8'));const requestId=prior.results.find(x=>x.tradeIn)?.tradeIn.requestId;assert.ok(requestId);
 const before=await customer.get('/api/account');const request=before.tradeIns.find(r=>r.id===requestId);const itemId=request.items[0].id;
 let quote;
 if(request.state==='requested'){
  await warehouse.command('receive_trade_in',{requestId,note:'Browser integration reception verified'});
  await warehouse.command('inspect_trade_in',{itemId,condition:{powersOn:true,activationLock:false,identifierMatches:true,hardware:'working',screen:'used'}});
  quote=await owner.command('quote_trade_in',{requestId,expiresAt:new Date(Date.now()+86400000).toISOString(),items:[{itemId,offeredVnd:'500000'}]});
 }else{assert.equal(request.state,'quoted');quote={quoteId:request.quotes[0].id};}
 const account=await customer.get('/api/account'),quoted=account.tradeIns.find(r=>r.id===requestId).quotes.find(q=>q.id===quote.quoteId);assert.equal(quoted.items.length,1);
 const context=await browser.newContext({storageState:await customer.http.storageState(),viewport:{width:1366,height:900},reducedMotion:'reduce'});const page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',e=>errors.push(e.message));
 await page.goto(base+'/en/account',{waitUntil:'networkidle'});await page.getByRole('tab',{name:'Trade in',exact:true}).click();
 const responsePromise=page.waitForResponse(r=>r.url().endsWith('/api/commands')&&r.request().postDataJSON()?.command==='decide_trade_in');
 await page.getByRole('button',{name:'Accept',exact:true}).click();const response=await responsePromise;assert.equal(response.status(),200,await response.text());
 const accepted=(await customer.get('/api/account')).tradeIns.find(r=>r.id===requestId).quotes.find(q=>q.id===quote.quoteId).items[0];assert.equal(accepted.decision,'accepted');
 const finalized=await owner.command('finalize_trade_in',{itemId,quoteItemId:accepted.id});assert.equal(finalized.creditVnd,'500000');
 const after=await customer.get('/api/account');assert.equal(BigInt(after.creditVnd)-BigInt(before.creditVnd),500000n);
 await page.getByRole('button',{name:'Refresh',exact:true}).click();await expect(page.locator('main')).toContainText('500.000');
 await page.screenshot({path:'artifacts/web/trade-accepted.png',fullPage:true});
 results.push({requestId,quoteId:quote.quoteId,customerUiDecision:'accepted',creditDeltaVnd:'500000'});assert.deepEqual(errors,[]);console.log(JSON.stringify(results,null,2));
}finally{await writeFile('artifacts/web/trade-decision-results.json',JSON.stringify({at:new Date().toISOString(),results,pageErrors:errors},null,2));await Promise.all(actors.map(a=>a.dispose()));await browser.close();}
