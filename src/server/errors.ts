import type {Actor,Role} from '../lib/contracts';
export class AppError extends Error{constructor(public code:string,message:string,public status=400){super(message);}}
export function requireRole(actor:Actor,...roles:Role[]){if(!roles.includes(actor.role))throw new AppError('FORBIDDEN','Bạn không có quyền thực hiện thao tác này.',403);}
export function ensure(value:unknown,code:string,message:string,status=409):asserts value{if(!value)throw new AppError(code,message,status);}
