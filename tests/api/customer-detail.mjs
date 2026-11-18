import assert from 'node:assert/strict';
import {writeFile,mkdir} from 'node:fs/promises';
import {client} from '../web/live-api.mjs';
const results=[];
try{
 for(const email of ['owner@botzone.local','nv01@botzone.local']){
  const actor=await client(email);
  try{
   const detail=await actor.get('/api/admin/customers/demo-customer');assert.equal(detail.customer.id,'demo-customer');
   assert.equal(typeof detail.balances.credit_vnd,'string');assert.equal(typeof detail.balances.balance_points,'number');assert.ok(['Member','Silver','Gold','Platinum'].includes(detail.balances.tier));
   for(const field of ['addresses','orders','creditEntries','loyaltyEntries','returns','tradeIns','claims'])assert.ok(Array.isArray(detail[field]),field);
   assert.ok(detail.orders.length>=8);assert.ok(detail.creditEntries.length>0);assert.ok(detail.loyaltyEntries.length>0);assert.equal(detail.historyLimit,200);assert.ok(detail.readAt);
   const list=await actor.get('/api/admin/data/customers');const row=list.rows.find(x=>x.id==='demo-customer');assert.equal(row.credit_vnd,detail.balances.credit_vnd);assert.equal(row.balance_points,detail.balances.balance_points);assert.equal(row.tier,detail.balances.tier);
   assert.equal((await actor.http.get('/api/admin/customers/unknown-customer')).status(),404);results.push({role:email.split('@')[0],passed:true,orderCount:detail.orders.length,creditEntries:detail.creditEntries.length,loyaltyEntries:detail.loyaltyEntries.length});
   await actor.post('/api/auth/logout',{});
  }finally{await actor.dispose();}
 }
 for(const email of ['warehouse@botzone.local','content@botzone.local','customer@botzone.local']){
  const actor=await client(email);try{assert.equal((await actor.http.get('/api/admin/customers/demo-customer')).status(),403);results.push({role:email.split('@')[0],denied:403,passed:true});await actor.post('/api/auth/logout',{});}finally{await actor.dispose();}
 }
 console.log(JSON.stringify({passed:results.length}));
}catch(error){results.push({passed:false,error:error.message});throw error;}
finally{await mkdir('artifacts/api',{recursive:true});await writeFile('artifacts/api/customer-detail-results.json',JSON.stringify({at:new Date().toISOString(),results},null,2));}
