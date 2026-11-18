import type {NextRequest} from 'next/server';
export async function proxyApi(request:NextRequest){
 const configured=new URL(process.env.BOTZONE_API_URL||'http://127.0.0.1:5080');
 if(!['127.0.0.1','localhost','[::1]'].includes(configured.hostname))throw new Error('BotZone local API must use a loopback address.');
 const target=new URL(request.nextUrl.pathname+request.nextUrl.search,configured);
 const headers=new Headers(request.headers);
 for(const key of ['host','connection','content-length','transfer-encoding'])headers.delete(key);
 headers.set('accept-encoding','identity');
 try{
  const response=await fetch(target,{method:request.method,headers,body:['GET','HEAD'].includes(request.method)?undefined:await request.arrayBuffer(),cache:'no-store',redirect:'manual',signal:request.signal});
  const output=new Headers(response.headers);
  for(const key of ['connection','transfer-encoding','content-encoding','content-length','set-cookie'])output.delete(key);
  for(const cookie of response.headers.getSetCookie())output.append('set-cookie',cookie);
  output.set('cache-control','no-store');
  return new Response(response.body,{status:response.status,headers:output});
 }catch(error){if(request.signal.aborted)throw error;const english=request.headers.get('accept-language')?.toLowerCase().startsWith('en');return Response.json({error:{code:'API_UNAVAILABLE',message:english?'The local BotZone service is unavailable. Please try again.':'API BotZone local chưa sẵn sàng. Vui lòng thử lại.'}},{status:503,headers:{'cache-control':'no-store'}});}
}
