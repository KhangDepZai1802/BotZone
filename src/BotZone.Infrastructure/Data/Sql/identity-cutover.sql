DO $$ BEGIN
 IF EXISTS(SELECT 1 FROM "user" GROUP BY upper(email) HAVING count(*)>1) THEN RAISE EXCEPTION 'Identity email normalization conflict: manual resolution required'; END IF;
END $$;
UPDATE "user" SET user_name=email,normalized_user_name=upper(email),normalized_email=upper(email),security_stamp=gen_random_uuid()::text,concurrency_stamp=gen_random_uuid()::text,password_reset_required=true,lockout_enabled=true WHERE user_name IS NULL;
INSERT INTO app_settings(key,value) VALUES('schema_owner','"ef_core"') ON CONFLICT(key) DO UPDATE SET value=excluded.value;
DO $$ BEGIN IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='botzone_app') THEN
 GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA public TO botzone_app;
 GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA public TO botzone_app;
 REVOKE ALL ON schema_migrations,"__EFMigrationsHistory",account,session,verification FROM botzone_app;
 REVOKE INSERT,UPDATE,DELETE ON identity_roles,identity_user_roles,identity_role_claims FROM botzone_app;
END IF; END $$;
