UPDATE warranty_replacements r SET order_item_unit_amount_id=e.order_item_unit_amount_id FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id WHERE c.id=r.claim_id AND r.order_item_unit_amount_id IS NULL;
CREATE FUNCTION guard_warranty_replacement_source() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE original uuid; variant uuid; oldvariant uuid; newvariant uuid; mode text; BEGIN
 SELECT e.order_item_unit_amount_id,i.variant_id,v.tracking_mode INTO original,variant,mode FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id JOIN product_variants v ON v.id=i.variant_id WHERE c.id=NEW.claim_id;
 IF original IS NULL OR (NEW.order_item_unit_amount_id IS NOT NULL AND NEW.order_item_unit_amount_id<>original) THEN RAISE EXCEPTION 'REPLACEMENT_WRONG_ENTITLEMENT' USING ERRCODE='23514'; END IF; NEW.order_item_unit_amount_id:=original;
 IF mode='serialized' THEN
  SELECT l.variant_id INTO oldvariant FROM inventory_units u JOIN inventory_lots l ON l.id=u.lot_id WHERE u.id=NEW.old_unit_id;
  SELECT l.variant_id INTO newvariant FROM inventory_units u JOIN inventory_lots l ON l.id=u.lot_id WHERE u.id=NEW.new_unit_id;
  IF NOT EXISTS(SELECT 1 FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id WHERE c.id=NEW.claim_id AND e.inventory_unit_id=NEW.old_unit_id) THEN RAISE EXCEPTION 'REPLACEMENT_WRONG_OLD_SERIAL' USING ERRCODE='23514'; END IF;
 ELSE
  SELECT variant_id INTO oldvariant FROM inventory_lots WHERE id=NEW.old_lot_id; SELECT variant_id INTO newvariant FROM inventory_lots WHERE id=NEW.new_lot_id;
  IF NOT EXISTS(SELECT 1 FROM inventory_allocations WHERE order_item_unit_amount_id=original AND lot_id=NEW.old_lot_id AND state IN ('shipped','returned','replaced')) THEN RAISE EXCEPTION 'REPLACEMENT_WRONG_OLD_LOT' USING ERRCODE='23514'; END IF;
 END IF;
 IF oldvariant IS DISTINCT FROM variant OR newvariant IS DISTINCT FROM variant THEN RAISE EXCEPTION 'REPLACEMENT_REQUIRES_SAME_SKU' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER replacement_source_guard BEFORE INSERT ON warranty_replacements FOR EACH ROW EXECUTE FUNCTION guard_warranty_replacement_source();
CREATE TRIGGER replacement_immutable BEFORE UPDATE OR DELETE ON warranty_replacements FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE FUNCTION guard_quarantine_source() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF NOT EXISTS(SELECT 1 FROM inventory_allocations WHERE order_item_unit_amount_id=NEW.order_item_unit_amount_id AND lot_id=NEW.lot_id) THEN RAISE EXCEPTION 'QUARANTINE_WRONG_SOURCE' USING ERRCODE='23514'; END IF;
 IF TG_OP='UPDATE' AND ROW(OLD.order_item_unit_amount_id,OLD.lot_id,OLD.quantity) IS DISTINCT FROM ROW(NEW.order_item_unit_amount_id,NEW.lot_id,NEW.quantity) THEN RAISE EXCEPTION 'QUARANTINE_SOURCE_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER quarantine_source_guard BEFORE INSERT OR UPDATE ON inventory_quarantine FOR EACH ROW EXECUTE FUNCTION guard_quarantine_source();
CREATE FUNCTION check_ledger_source() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_TABLE_NAME='store_credit_entries' THEN
  IF NEW.kind='trade_in' AND NOT EXISTS(SELECT 1 FROM trade_in_quote_items qi JOIN trade_in_items i ON i.id=qi.item_id JOIN trade_in_requests r ON r.id=i.request_id WHERE qi.id=NEW.quote_item_id AND i.id=NEW.trade_in_item_id AND r.customer_id=NEW.user_id AND qi.decision='accepted' AND i.state='completed' AND qi.offered_vnd=NEW.delta_vnd) THEN RAISE EXCEPTION 'TRADE_CREDIT_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
  IF NEW.kind IN ('spend','refund') AND NOT EXISTS(SELECT 1 FROM orders WHERE id=NEW.order_id AND customer_id=NEW.user_id) THEN RAISE EXCEPTION 'CREDIT_LEDGER_WRONG_CUSTOMER' USING ERRCODE='23514'; END IF;
  IF NEW.kind='refund' AND NOT EXISTS(SELECT 1 FROM refunds rf WHERE rf.id=NEW.refund_id AND rf.order_id=NEW.order_id AND rf.status='completed' AND NEW.delta_vnd=(SELECT COALESCE(sum(ra.amount_vnd),0) FROM refund_allocations ra JOIN payment_receipts pr ON pr.id=ra.payment_receipt_id WHERE ra.refund_id=rf.id AND pr.tender_kind='store_credit')) THEN RAISE EXCEPTION 'REFUND_CREDIT_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 ELSE
  IF NOT EXISTS(SELECT 1 FROM orders WHERE id=NEW.order_id AND customer_id=NEW.user_id) OR (NEW.unit_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE u.id=NEW.unit_id AND i.order_id=NEW.order_id)) THEN RAISE EXCEPTION 'LOYALTY_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 END IF; RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER credit_ledger_source AFTER INSERT ON store_credit_entries DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_ledger_source();
CREATE CONSTRAINT TRIGGER loyalty_ledger_source AFTER INSERT ON loyalty_entries DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_ledger_source();
GRANT SELECT,INSERT,UPDATE,DELETE ON inventory_quarantine TO botzone_app;
