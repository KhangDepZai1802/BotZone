import 'dotenv/config';
import pg from 'pg';
import {readFile,readdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
const c=new pg.Client({connectionString:process.env.MIGRATION_DATABASE_URL||process.env.DATABASE_URL});
await c.connect();
try{await c.query('SELECT pg_advisory_lock(2026111301)');await c.query('CREATE TABLE IF NOT EXISTS schema_migrations(name text PRIMARY KEY,checksum text NOT NULL,applied_at timestamptz NOT NULL DEFAULT now())');
 for(const name of (await readdir('migrations')).filter(x=>x.endsWith('.sql')).sort()){const sql=await readFile(`migrations/${name}`,'utf8');const hash=createHash('sha256').update(sql).digest('hex');const old=await c.query('SELECT checksum FROM schema_migrations WHERE name=$1',[name]);if(old.rows.length){if(old.rows[0].checksum!==hash)throw new Error(`Migration checksum changed: ${name}`);console.log(`Already applied ${name}`);continue;}await c.query('BEGIN');try{await c.query(sql);await c.query('INSERT INTO schema_migrations(name,checksum) VALUES($1,$2)',[name,hash]);await c.query('COMMIT');console.log(`Applied ${name}`);}catch(e){await c.query('ROLLBACK');throw e;}}
 if((await c.query("SELECT 1 FROM pg_roles WHERE rolname='botzone_app'")).rowCount){await c.query('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO botzone_app');await c.query('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO botzone_app');await c.query('REVOKE ALL ON schema_migrations FROM botzone_app');}
}finally{await c.query('SELECT pg_advisory_unlock(2026111301)');await c.end();}
