import 'dotenv/config';
import pg from 'pg';
import {readFile,writeFile,mkdir,readdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
const c=new pg.Client({connectionString:process.env.MIGRATION_DATABASE_URL});await c.connect();
const queries={
 columns:`SELECT md5(string_agg(table_name||':'||column_name||':'||udt_name||':'||is_nullable||':'||coalesce(column_default,''),E'\n' ORDER BY table_name,ordinal_position)) AS fingerprint FROM information_schema.columns WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory'`,
 constraints:`SELECT md5(string_agg(t.relname||':'||c.conname||':'||pg_get_constraintdef(c.oid),E'\n' ORDER BY t.relname,c.conname)) AS fingerprint FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='public' AND t.relname<>'__EFMigrationsHistory'`,
 indexes:`SELECT md5(string_agg(indexdef,E'\n' ORDER BY indexname)) AS fingerprint FROM pg_indexes WHERE schemaname='public' AND tablename<>'__EFMigrationsHistory'`,
 triggers:`SELECT md5(string_agg(pg_get_triggerdef(t.oid),E'\n' ORDER BY c.relname,t.tgname)) AS fingerprint FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND NOT t.tgisinternal AND c.relname<>'__EFMigrationsHistory'`,
 functions:`SELECT md5(string_agg(pg_get_functiondef(p.oid),E'\n' ORDER BY p.proname,pg_get_function_identity_arguments(p.oid))) AS fingerprint FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.prokind='f'`
};
try{
 const manifest={capturedDate:'2026-11-13',checks:{},migrations:[]};
 for(const [name,query]of Object.entries(queries))manifest.checks[name]={query,fingerprint:(await c.query(query)).rows[0].fingerprint};
 for(const name of ['0001_core.sql','0002_guards.sql']){const bytes=await readFile(`migrations/${name}`);manifest.migrations.push({name,sha256:createHash('sha256').update(bytes).digest('hex')});}
 await mkdir('docs/database',{recursive:true});await writeFile('docs/database/ef-baseline-manifest.json',JSON.stringify(manifest,null,2)+'\n');
 let assertions="DO $baseline$ DECLARE actual text; BEGIN\n";
 for(const [name,{query,fingerprint}]of Object.entries(manifest.checks))assertions+=`SELECT fingerprint INTO actual FROM (${query}) q; IF actual IS DISTINCT FROM '${fingerprint}' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: ${name}'; END IF;\n`;
 for(const m of manifest.migrations)assertions+=`IF NOT EXISTS(SELECT 1 FROM schema_migrations WHERE name='${m.name}' AND checksum='${m.sha256}') THEN RAISE EXCEPTION 'BASELINE_MIGRATION_MISMATCH'; END IF;\n`;
 assertions+='END $baseline$;';await mkdir('src/BotZone.Infrastructure/Data/Sql',{recursive:true});await writeFile('src/BotZone.Infrastructure/Data/Sql/baseline-assert.sql',assertions);console.log(JSON.stringify(manifest.checks,null,2));
}finally{await c.end();}
