import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
import {client} from './live-api.mjs';
const actors=[],results=[];
try{
 const warehouse=await client('warehouse@botzone.local'),customer=await client('customer@botzone.local'),other=await client('customer2@botzone.local'),anonymous=await client(),content=await client('content@botzone.local');actors.push(warehouse,customer,other,anonymous,content);
 const bytes=await readFile('artifacts/web/login-mobile.png');
 const token=(await warehouse.get('/api/auth/csrf')).token;
 const uploaded=await warehouse.http.post('/api/media',{headers:{'X-CSRF-TOKEN':token},multipart:{visibility:'private',referenceType:'order',referenceId:'4916f93c-0269-43e5-95a6-65b337126313',file:{name:'staff-inspection.png',mimeType:'image/png',buffer:bytes}}});
 assert.equal(uploaded.status(),200,await uploaded.text());const asset=await uploaded.json();
 assert.equal((await customer.http.get(asset.previewUrl)).status(),200);
 assert.equal((await warehouse.http.get(asset.previewUrl)).status(),200);
 assert.equal((await other.http.get(asset.previewUrl)).status(),403);
 assert.equal((await content.http.get(asset.previewUrl)).status(),403);
 assert.equal((await anonymous.http.get(asset.previewUrl)).status(),401);
 results.push({mediaId:asset.id,staffUploadedOwnedCaseCustomer:200,warehouse:200,otherCustomer:403,contentStaff:403,anonymous:401});
 const model=await readFile('public/models/phone-illustration.glb');const jsonLength=model.readUInt32LE(12);const json=JSON.parse(model.subarray(20,20+jsonLength).toString());json.buffers[0].uri='https://example.com/external.bin';
 let encoded=Buffer.from(JSON.stringify(json));encoded=Buffer.concat([encoded,Buffer.alloc((4-encoded.length%4)%4,0x20)]);const remainder=model.subarray(20+jsonLength);const unsafe=Buffer.alloc(20+encoded.length+remainder.length);model.subarray(0,20).copy(unsafe);unsafe.writeUInt32LE(unsafe.length,8);unsafe.writeUInt32LE(encoded.length,12);encoded.copy(unsafe,20);remainder.copy(unsafe,20+encoded.length);
 const csrf=(await content.get('/api/auth/csrf')).token;
 const rejected=await content.http.post('/api/media',{headers:{'X-CSRF-TOKEN':csrf},multipart:{visibility:'public',sourceUrl:'original:BotZone validation test',licenseNote:'CC0 original test geometry',file:{name:'unsafe.glb',mimeType:'model/gltf-binary',buffer:unsafe}}});
 assert.equal(rejected.status(),400,await rejected.text());results.push({externalGlbUri:400,error:(await rejected.json()).error.code});
 console.log(JSON.stringify(results,null,2));
}finally{await writeFile('artifacts/web/media-scope-results.json',JSON.stringify({at:new Date().toISOString(),results},null,2));await Promise.all(actors.map(a=>a.dispose()));}
