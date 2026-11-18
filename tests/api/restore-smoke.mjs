import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
const base=process.env.BOTZONE_TEST_API||'http://127.0.0.1:5081';
const snapshot=JSON.parse(await readFile('docs/database/restore-verification.json','utf8'));
const checks=[];
class Client {
 cookies=new Map();csrf='';
 async call(path,body,expected=200){const headers={cookie:[...this.cookies].map(([k,v])=>`${k}=${v}`).join('; ')};if(body!==undefined){headers['content-type']='application/json';headers['X-CSRF-TOKEN']=this.csrf;}const r=await fetch(base+path,{method:body===undefined?'GET':'POST',headers,body:body===undefined?undefined:JSON.stringify(body)});for(const cookie of r.headers.getSetCookie()){const pair=cookie.split(';',1)[0],p=pair.indexOf('=');this.cookies.set(pair.slice(0,p),pair.slice(p+1));}assert.equal(r.status,expected,path+': '+r.status);return r;}
 async token(){this.csrf=(await(await this.call('/api/auth/csrf')).json()).token;}
 async login(email){await this.token();await this.call('/api/auth/login',{email,password:'BotZoneDemo!2026',clientType:'web'});await this.token();}
}
try{
 const owner=new Client(),customer=new Client(),other=new Client(),anonymous=new Client();
 await owner.login('owner@botzone.local');await customer.login('customer@botzone.local');await other.login('customer2@botzone.local');checks.push('Identity login on restored users with restored Data Protection directory');
 const query={page:1,pageSize:100,quickSearch:'G0'};
 const list=await(await owner.call('/api/admin/invoices/search',query)).json();const golden=list.items.filter(x=>/^G0[1-8]$/.test(x.invoiceNo));assert.equal(golden.length,8);assert.equal(golden.reduce((sum,x)=>sum+BigInt(x.grandTotalVnd),0n),97130000n);checks.push('Restored C# List<InvoiceListItemDto>: eight golden, 97130000 VND');
 const exported=await(await customer.call('/api/invoices/exports',{kind:'detail',invoiceId:golden[0].id,formats:['xlsx','pdf'],locale:'vi'})).json();
 for(const [format,path]of Object.entries(exported.links)){const r=await customer.call(path);const b=Buffer.from(await r.arrayBuffer());assert.equal(b.subarray(0,format==='pdf'?4:2).toString(),format==='pdf'?'%PDF':'PK');await other.call(path,undefined,404);}
 checks.push('New XLSX/PDF from restored DB and uploads: valid signatures; another customer denied');
 for(const media of snapshot.mediaChecks){const r=await owner.call('/api/media/'+media.id);const b=Buffer.from(await r.arrayBuffer());assert.equal(createHash('sha256').update(b).digest('hex'),media.sha256);if(media.storageKey.startsWith('evidence-private/')){await anonymous.call('/api/media/'+media.id,undefined,401);await other.call('/api/media/'+media.id,undefined,403);}}
 checks.push('Every restored media file returns matching SHA256; private files reject anonymous and other customer');
 for(const actor of [owner,customer,other])await actor.call('/api/auth/logout',{});
 console.log(JSON.stringify({passed:checks.length,database:snapshot.database}));
}catch(error){checks.push({failure:error.message});throw error;}
finally{await mkdir('artifacts/api',{recursive:true});await writeFile('artifacts/api/restore-results.json',JSON.stringify({at:new Date().toISOString(),database:snapshot.database,checks},null,2));}
