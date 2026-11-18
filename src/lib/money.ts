// Every financial calculation uses integer VND and rational arithmetic.
export function distribute(total:bigint,weights:bigint[]):bigint[]{
 if(total<0n||weights.some(x=>x<0n))throw new Error('Negative allocation');
 const sum=weights.reduce((a,b)=>a+b,0n);if(!sum){if(total)throw new Error('Cannot allocate to zero');return weights.map(()=>0n);}
 const result=weights.map(w=>total*w/sum);let remaining=total-result.reduce((a,b)=>a+b,0n);
 const order=weights.map((w,i)=>({i,remainder:total*w%sum})).sort((a,b)=>a.remainder===b.remainder?a.i-b.i:a.remainder>b.remainder?-1:1);
 for(const {i} of order){if(!remaining)break;result[i]++;remaining--;}
 return result;
}
export function allocatePoints(requested:number,net:bigint[]):number[]{
 const capacity=net.map(v=>v/1000n);const usable=BigInt(requested)<capacity.reduce((a,b)=>a+b,0n)?BigInt(requested):capacity.reduce((a,b)=>a+b,0n);
 return distribute(usable,capacity).map(Number);
}
export const sumMoney=(values:bigint[])=>values.reduce((a,b)=>a+b,0n);
