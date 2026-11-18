CREATE FUNCTION guard_trade_quote_history() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'TRADE_QUOTE_HISTORY_IMMUTABLE' USING ERRCODE='23514'; END IF;
 IF TG_TABLE_NAME='trade_in_quotes' THEN
  IF ROW(NEW.id,NEW.request_id,NEW.version_no,NEW.expires_at,NEW.quoted_by) IS DISTINCT FROM ROW(OLD.id,OLD.request_id,OLD.version_no,OLD.expires_at,OLD.quoted_by) OR (OLD.approved_by IS NOT NULL AND NEW.approved_by IS DISTINCT FROM OLD.approved_by) THEN RAISE EXCEPTION 'TRADE_QUOTE_HISTORY_IMMUTABLE' USING ERRCODE='23514'; END IF;
 ELSE
  IF ROW(NEW.id,NEW.quote_id,NEW.request_id,NEW.item_id,NEW.offered_vnd) IS DISTINCT FROM ROW(OLD.id,OLD.quote_id,OLD.request_id,OLD.item_id,OLD.offered_vnd) OR (OLD.decision<>'pending' AND ROW(NEW.decision,NEW.decided_at) IS DISTINCT FROM ROW(OLD.decision,OLD.decided_at)) THEN RAISE EXCEPTION 'TRADE_QUOTE_ITEM_IMMUTABLE' USING ERRCODE='23514'; END IF;
  IF NEW.decision<>'pending' AND NEW.decided_at IS NULL THEN RAISE EXCEPTION 'TRADE_DECISION_REQUIRES_TIME' USING ERRCODE='23514'; END IF;
 END IF; RETURN NEW;
END $$;
CREATE TRIGGER trade_quote_history_guard BEFORE UPDATE OR DELETE ON trade_in_quotes FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_history();
CREATE TRIGGER trade_quote_item_history_guard BEFORE UPDATE OR DELETE ON trade_in_quote_items FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_history();
CREATE FUNCTION guard_trade_quote_append() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM trade_in_quote_items WHERE quote_id=NEW.quote_id AND decision<>'pending') THEN RAISE EXCEPTION 'TRADE_QUOTE_ALREADY_DECIDED' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER trade_quote_append_guard BEFORE INSERT ON trade_in_quote_items FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_append();
CREATE FUNCTION guard_inventory_history() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'INVENTORY_SOURCE_IMMUTABLE' USING ERRCODE='23514'; END IF;
 IF TG_TABLE_NAME='inventory_allocations' THEN
  IF ROW(NEW.id,NEW.order_item_id,NEW.order_item_unit_amount_id,NEW.lot_id,NEW.inventory_unit_id,NEW.quantity,NEW.cost_vnd) IS DISTINCT FROM ROW(OLD.id,OLD.order_item_id,OLD.order_item_unit_amount_id,OLD.lot_id,OLD.inventory_unit_id,OLD.quantity,OLD.cost_vnd) THEN RAISE EXCEPTION 'ALLOCATION_HISTORY_IMMUTABLE' USING ERRCODE='23514'; END IF;
 ELSE
  IF ROW(NEW.id,NEW.warehouse_id,NEW.variant_id,NEW.receipt_item_id,NEW.received_at,NEW.unit_cost_vnd) IS DISTINCT FROM ROW(OLD.id,OLD.warehouse_id,OLD.variant_id,OLD.receipt_item_id,OLD.received_at,OLD.unit_cost_vnd) THEN RAISE EXCEPTION 'LOT_HISTORY_IMMUTABLE' USING ERRCODE='23514'; END IF;
 END IF; RETURN NEW;
END $$;
CREATE TRIGGER allocation_history_guard BEFORE UPDATE OR DELETE ON inventory_allocations FOR EACH ROW EXECUTE FUNCTION guard_inventory_history();
CREATE TRIGGER lot_history_guard BEFORE UPDATE OR DELETE ON inventory_lots FOR EACH ROW EXECUTE FUNCTION guard_inventory_history();
CREATE TRIGGER goods_receipt_immutable BEFORE UPDATE OR DELETE ON goods_receipts FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER goods_receipt_item_immutable BEFORE UPDATE OR DELETE ON goods_receipt_items FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER order_adjustment_immutable BEFORE UPDATE OR DELETE ON order_adjustments FOR EACH ROW EXECUTE FUNCTION reject_mutation();
