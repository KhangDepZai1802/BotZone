DO $baseline$ DECLARE actual text; BEGIN
SELECT fingerprint INTO actual FROM (SELECT md5(string_agg(table_name||':'||column_name||':'||udt_name||':'||is_nullable||':'||coalesce(column_default,''),E'
' ORDER BY table_name,ordinal_position)) AS fingerprint FROM information_schema.columns WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory') q; IF actual IS DISTINCT FROM '4a179dc5e9dff96647ae98f25806b022' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: columns'; END IF;
SELECT fingerprint INTO actual FROM (SELECT md5(string_agg(t.relname||':'||c.conname||':'||pg_get_constraintdef(c.oid),E'
' ORDER BY t.relname,c.conname)) AS fingerprint FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE n.nspname='public' AND t.relname<>'__EFMigrationsHistory') q; IF actual IS DISTINCT FROM '8a50291847aa542143880d9d05a96a31' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: constraints'; END IF;
SELECT fingerprint INTO actual FROM (SELECT md5(string_agg(indexdef,E'
' ORDER BY indexname)) AS fingerprint FROM pg_indexes WHERE schemaname='public' AND tablename<>'__EFMigrationsHistory') q; IF actual IS DISTINCT FROM 'aed126980b469d5a7b79f0fa810f5c57' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: indexes'; END IF;
SELECT fingerprint INTO actual FROM (SELECT md5(string_agg(pg_get_triggerdef(t.oid),E'
' ORDER BY c.relname,t.tgname)) AS fingerprint FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND NOT t.tgisinternal AND c.relname<>'__EFMigrationsHistory') q; IF actual IS DISTINCT FROM '9c06ba4d3ec560bd7141a5b5998ee886' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: triggers'; END IF;
SELECT fingerprint INTO actual FROM (SELECT md5(string_agg(pg_get_functiondef(p.oid),E'
' ORDER BY p.proname,pg_get_function_identity_arguments(p.oid))) AS fingerprint FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.prokind='f') q; IF actual IS DISTINCT FROM 'e182dffadff2cdb1d63f613c9a1eb341' THEN RAISE EXCEPTION 'BASELINE_SCHEMA_DRIFT: functions'; END IF;
IF NOT EXISTS(SELECT 1 FROM schema_migrations WHERE name='0001_core.sql' AND checksum='a22cc34f8aa4415965825b95e7788459e7d8d225066be04013a743e96316e5a8') THEN RAISE EXCEPTION 'BASELINE_MIGRATION_MISMATCH'; END IF;
IF NOT EXISTS(SELECT 1 FROM schema_migrations WHERE name='0002_guards.sql' AND checksum='937c733b762cda5e63ba6c45c98eece383882f174144ffc28f92fc9ffc03a282') THEN RAISE EXCEPTION 'BASELINE_MIGRATION_MISMATCH'; END IF;
END $baseline$;