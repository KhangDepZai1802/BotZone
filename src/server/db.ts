import 'dotenv/config';
import pg from 'pg';
import {drizzle} from 'drizzle-orm/node-postgres';
import * as schema from '../db/schema';
import type {Actor} from '../lib/contracts';
export const pool=new pg.Pool({connectionString:process.env.DATABASE_URL,max:12});
export const db=drizzle(pool,{schema});
export type Client=pg.PoolClient;
export async function transaction<T>(fn:(client:Client)=>Promise<T>,actor?:Actor):Promise<T>{
 const c=await pool.connect();try{await c.query('BEGIN');if(actor)await c.query("SELECT set_config('botzone.actor_role',$1,true),set_config('botzone.actor_id',$2,true)",[actor.role,actor.id]);const result=await fn(c);await c.query('COMMIT');return result;}catch(e){await c.query('ROLLBACK');throw e;}finally{c.release();}
}
export async function readSnapshot<T>(fn:(client:Client)=>Promise<T>):Promise<T>{const c=await pool.connect();try{await c.query('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');const result=await fn(c);await c.query('COMMIT');return result;}catch(e){await c.query('ROLLBACK');throw e;}finally{c.release();}}
