import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {client} from '../web/live-api.mjs';
const checkpoint='artifacts/api/cart-before-restart.json';
const customer=await client('customer2@botzone.local');
try{
 if(process.argv.includes('--before')){
  const prior=await customer.get('/api/cart');const catalogue=await customer.get('/api/catalogue?locale=vi');const p=catalogue.products.find(x=>x.slug==='p13');assert.ok(p);
  const cart={items:[{variantId:p.variants[0].id,quantity:1,care:false}],promotionCode:'DEMO1M'};await customer.command('save_cart',cart);
  const quote=await customer.post('/api/quote',{...cart,points:0,creditVnd:'0'});assert.equal(quote.discountVnd,'1000000');
  const saved=await customer.get('/api/cart');await mkdir('artifacts/api',{recursive:true});await writeFile(checkpoint,JSON.stringify({at:new Date().toISOString(),saved,prior},null,2));console.log('Cart and DEMO1M persisted; ready for actual API process restart.');
 }else{
  const before=JSON.parse(await readFile(checkpoint,'utf8'));const after=await customer.get('/api/cart');assert.deepEqual(after,before.saved);
  const quote=await customer.post('/api/quote',{...after,points:0,creditVnd:'0'});assert.equal(quote.discountVnd,'1000000');
  await customer.command('save_cart',before.prior);
  await writeFile('artifacts/api/cart-restart-results.json',JSON.stringify({before:before.at,after:new Date().toISOString(),passed:true,promotionCode:after.promotionCode,variantId:after.items[0].variantId,discountVnd:quote.discountVnd,priorCartRestored:true},null,2));console.log('PASS cart/voucher/quote survive API process restart; prior cart restored.');
 }
 await customer.post('/api/auth/logout',{});
}finally{await customer.dispose();}
