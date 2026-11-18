import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {mkdir,writeFile} from 'node:fs/promises';

// Explicit sample generation only. No seed, command, fixture runner or migration.
process.env.BOTZONE_WEB_URL ||= process.env.BOTZONE_API_URL || 'http://127.0.0.1:5080';
const {client,base}=await import('../web/live-api.mjs');
const owner=await client('owner@botzone.local');
try{
 const refs=Array.from({length:8},(_,i)=>`G${String(i+1).padStart(2,'0')}`);
 const query={filter:{kind:'group',logic:'OR',children:refs.map(value=>({kind:'rule',field:'invoiceNo',op:'=',value}))},sort:[{field:'invoiceNo',direction:'asc'}],page:1,pageSize:20};
 const listed=await owner.post('/api/admin/invoices/search',query);
 assert.equal(listed.matchedCount,8);assert.equal(listed.matchedTotalVnd,'97130000');assert.deepEqual(listed.items.map(i=>i.invoiceNo),refs);
 const request={kind:'list',query,formats:['xlsx','pdf'],locale:'vi'};
 const exported=await owner.post('/api/invoices/exports',request);
 assert.equal(exported.rowCount,8);assert.equal(exported.totalVnd,'97130000');
 await mkdir('examples/exports',{recursive:true});const files={};
 for(const format of request.formats){
  const response=await owner.http.get(exported.links[format]);assert.equal(response.status(),200);const data=await response.body();
  assert.ok(format==='pdf'?data.subarray(0,4).toString()==='%PDF':data[0]===0x50&&data[1]===0x4b);
  const path=`examples/exports/golden.${format}`;await writeFile(path,data);files[format]={path,bytes:data.length,sha256:createHash('sha256').update(data).digest('hex')};
 }
 await writeFile('examples/exports/manifest.json',JSON.stringify({schemaVersion:1,status:'downloaded; awaiting independent file verification',generatedAt:new Date().toISOString(),sourceApi:base,source:'Authenticated owner API; exact G01–G08 filter; no seed or business commands',request,exportId:exported.exportId,readAt:exported.readAt,rowCount:exported.rowCount,totalVnd:exported.totalVnd,invoiceRefs:refs,files},null,2));
 console.log(JSON.stringify({exportId:exported.exportId,rowCount:exported.rowCount,totalVnd:exported.totalVnd,files}));
 await owner.post('/api/auth/logout',{});
}finally{await owner.dispose();}
