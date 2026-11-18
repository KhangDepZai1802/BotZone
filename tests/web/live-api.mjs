import {request} from '@playwright/test';
export const base=process.env.BOTZONE_WEB_URL||'http://127.0.0.1:3000';
export async function client(email){
 const http=await request.newContext({baseURL:base});let csrf=(await(await http.get('/api/auth/csrf')).json()).token;
 async function post(url,data,expected=200){const response=await http.post(url,{data,headers:{'X-CSRF-TOKEN':csrf}});const body=await response.json();if(response.status()!==expected)throw new Error(`${url} ${response.status()}: ${JSON.stringify(body)}`);return body;}
 if(email){await post('/api/auth/login',{email,password:'BotZoneDemo!2026',clientType:'web'});csrf=(await(await http.get('/api/auth/csrf')).json()).token;}
 return{http,post,command:(command,input,key=crypto.randomUUID(),expected=200)=>post('/api/commands',{command,key,input},expected),get:async(url)=>{const response=await http.get(url);const body=await response.json();if(!response.ok())throw Error(`${url} ${response.status()} ${JSON.stringify(body)}`);return body;},dispose:()=>http.dispose()};
}
