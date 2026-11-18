-- Existing units retain the original 1,000 VND snapshot through the additive column default.
DO $$ DECLARE c record; BEGIN
 FOR c IN SELECT conname FROM pg_constraint WHERE conrelid='order_item_unit_amounts'::regclass AND contype='c' AND pg_get_constraintdef(oid) LIKE '%points_discount_vnd%redeemed_points%' LOOP
  EXECUTE format('ALTER TABLE order_item_unit_amounts DROP CONSTRAINT %I',c.conname);
 END LOOP;
END $$;
ALTER TABLE order_item_unit_amounts ADD CONSTRAINT point_value_snapshot_positive CHECK(point_value_vnd>0);
ALTER TABLE order_item_unit_amounts ADD CONSTRAINT points_snapshot_amount CHECK(points_discount_vnd=redeemed_points::bigint*point_value_vnd);
DROP INDEX IF EXISTS trade_in_identifier_active_idx;
CREATE UNIQUE INDEX trade_in_identifier_active_idx ON trade_in_items(identifier) WHERE identifier IS NOT NULL AND state NOT IN ('completed','returned');
