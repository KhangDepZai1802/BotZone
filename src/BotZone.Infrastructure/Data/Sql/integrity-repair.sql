-- Additive EF repair: original legacy files remain immutable.
ALTER TABLE credit_note_items ADD CONSTRAINT credit_positive_quantity_or_amount CHECK(COALESCE(quantity>0,false) OR amount_vnd>0);
CREATE FUNCTION guard_refund_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_OP='DELETE' OR OLD.status IN ('completed','failed') OR ROW(NEW.order_id,NEW.credit_note_id,NEW.amount_vnd,NEW.approved_by,NEW.idempotency_key) IS DISTINCT FROM ROW(OLD.order_id,OLD.credit_note_id,OLD.amount_vnd,OLD.approved_by,OLD.idempotency_key) THEN RAISE EXCEPTION 'REFUND_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER refund_mutation_guard BEFORE UPDATE OR DELETE ON refunds FOR EACH ROW EXECUTE FUNCTION guard_refund_mutation();
CREATE TRIGGER refund_allocations_immutable BEFORE UPDATE OR DELETE ON refund_allocations FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE FUNCTION guard_promotion_target() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE oldid uuid; newid uuid; BEGIN
 IF TG_OP<>'INSERT' THEN oldid:=OLD.promotion_version_id; END IF; IF TG_OP<>'DELETE' THEN newid:=NEW.promotion_version_id; END IF;
 IF EXISTS(SELECT 1 FROM promotion_redemptions WHERE promotion_version_id IN(oldid,newid)) THEN RAISE EXCEPTION 'USED_PROMOTION_TARGET_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER promotion_target_guard BEFORE INSERT OR UPDATE OR DELETE ON promotion_targets FOR EACH ROW EXECUTE FUNCTION guard_promotion_target();
CREATE FUNCTION guard_inventory_sources() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE qty integer; mode text; BEGIN
 IF TG_TABLE_NAME='inventory_allocations' THEN
  IF NOT EXISTS(SELECT 1 FROM order_items i JOIN inventory_lots l ON l.variant_id=i.variant_id JOIN product_variants v ON v.id=i.variant_id WHERE i.id=NEW.order_item_id AND l.id=NEW.lot_id AND i.line_type='product' AND NEW.quantity=1 AND NEW.cost_vnd=l.unit_cost_vnd AND ((v.tracking_mode='serialized')=(NEW.inventory_unit_id IS NOT NULL))) THEN RAISE EXCEPTION 'ALLOCATION_SKU_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 ELSIF TG_TABLE_NAME='inventory_reservations' THEN
  IF NOT EXISTS(SELECT 1 FROM order_items WHERE id=NEW.order_item_id AND variant_id=NEW.variant_id AND quantity=NEW.quantity AND line_type='product') THEN RAISE EXCEPTION 'RESERVATION_SKU_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 ELSIF TG_TABLE_NAME='goods_receipt_items' THEN
  PERFORM 1 FROM purchase_order_items WHERE id=NEW.purchase_order_item_id FOR UPDATE;
  IF NOT EXISTS(SELECT 1 FROM purchase_order_items WHERE id=NEW.purchase_order_item_id AND unit_cost_vnd=NEW.unit_cost_vnd) OR (SELECT COALESCE(sum(received_qty),0) FROM goods_receipt_items WHERE purchase_order_item_id=NEW.purchase_order_item_id AND id<>NEW.id)+NEW.received_qty>(SELECT quantity FROM purchase_order_items WHERE id=NEW.purchase_order_item_id) THEN RAISE EXCEPTION 'GOODS_RECEIPT_SOURCE_OR_QUANTITY' USING ERRCODE='23514'; END IF;
 ELSIF TG_TABLE_NAME='inventory_lots' THEN
  IF NOT EXISTS(SELECT 1 FROM goods_receipt_items ri JOIN purchase_order_items pi ON pi.id=ri.purchase_order_item_id WHERE ri.id=NEW.receipt_item_id AND pi.variant_id=NEW.variant_id AND ri.unit_cost_vnd=NEW.unit_cost_vnd) THEN RAISE EXCEPTION 'LOT_SKU_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 END IF; RETURN NEW;
END $$;
CREATE TRIGGER allocation_source_guard BEFORE INSERT OR UPDATE ON inventory_allocations FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources();
CREATE TRIGGER reservation_source_guard BEFORE INSERT OR UPDATE ON inventory_reservations FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources();
CREATE TRIGGER receipt_item_source_guard BEFORE INSERT OR UPDATE ON goods_receipt_items FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources();
CREATE TRIGGER lot_source_guard BEFORE INSERT OR UPDATE ON inventory_lots FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources();
CREATE OR REPLACE FUNCTION check_invoice_totals() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE iid uuid; inv invoices; BEGIN
 IF TG_TABLE_NAME='invoices' THEN iid:=COALESCE(NEW.id,OLD.id); ELSE iid:=COALESCE(NEW.invoice_id,OLD.invoice_id); END IF;
 SELECT * INTO inv FROM invoices WHERE id=iid FOR UPDATE;
 IF inv.id IS NULL OR inv.lifecycle='draft' THEN RETURN NULL; END IF;
 IF ROW(inv.subtotal_vnd,inv.discount_total_vnd,inv.grand_total_vnd) IS DISTINCT FROM (SELECT ROW(COALESCE(sum(quantity::bigint*unit_price_vnd),0)::bigint,COALESCE(sum(voucher_discount_vnd+points_discount_vnd),0)::bigint,COALESCE(sum(net_amount_vnd),0)::bigint) FROM invoice_items WHERE invoice_id=iid) OR (SELECT count(*) FROM invoice_items WHERE invoice_id=iid)<>(SELECT count(*) FROM order_items WHERE order_id=inv.order_id) THEN RAISE EXCEPTION 'INVOICE_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM invoice_items ii JOIN order_items oi ON oi.id=ii.order_item_id LEFT JOIN invoice_items parent ON parent.id=ii.parent_invoice_item_id WHERE ii.invoice_id=iid AND (ROW(ii.quantity,ii.unit_price_vnd,ii.voucher_discount_vnd,ii.points_discount_vnd,ii.net_amount_vnd,ii.line_type,ii.variant_id,ii.product_id) IS DISTINCT FROM ROW(oi.quantity,oi.unit_price_vnd,oi.voucher_discount_vnd,oi.points_discount_vnd,oi.net_amount_vnd,oi.line_type,oi.variant_id,oi.product_id) OR (ii.line_type='warranty' AND (parent.line_type<>'product' OR parent.order_item_id<>oi.parent_item_id)))) THEN RAISE EXCEPTION 'INVOICE_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE OR REPLACE FUNCTION check_tender() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE rid uuid; receipt payment_receipts; BEGIN
 IF TG_TABLE_NAME='payment_receipts' THEN rid:=NEW.id; ELSE rid:=NEW.receipt_id; END IF;
 SELECT * INTO receipt FROM payment_receipts WHERE id=rid;
 PERFORM 1 FROM orders WHERE id=receipt.order_id FOR UPDATE;
 IF receipt.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM order_tender_allocations WHERE receipt_id=rid) THEN RAISE EXCEPTION 'RECEIPT_ALLOCATION_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE a.receipt_id=rid AND oi.order_id<>receipt.order_id) THEN RAISE EXCEPTION 'TENDER_WRONG_ORDER' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN payment_receipts pr ON pr.id=a.receipt_id JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id WHERE pr.order_id=receipt.order_id GROUP BY u.id,pr.tender_kind HAVING sum(a.amount_vnd)>CASE WHEN pr.tender_kind='store_credit' THEN u.credit_planned_vnd ELSE u.cash_planned_vnd END) THEN RAISE EXCEPTION 'TENDER_OVERFUNDED' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE OR REPLACE FUNCTION check_refund() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE rid uuid; rf refunds; BEGIN
 IF TG_TABLE_NAME='refunds' THEN rid:=NEW.id; ELSE rid:=NEW.refund_id; END IF;
 SELECT * INTO rf FROM refunds WHERE id=rid;
 PERFORM 1 FROM orders WHERE id=rf.order_id FOR UPDATE;
 IF rf.credit_note_id IS NULL AND EXISTS(SELECT 1 FROM invoices WHERE order_id=rf.order_id AND lifecycle='issued') THEN RAISE EXCEPTION 'REFUND_REQUIRES_CREDIT_NOTE' USING ERRCODE='23514'; END IF;
 IF rf.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM refund_allocations WHERE refund_id=rid) THEN RAISE EXCEPTION 'REFUND_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM refund_allocations ra JOIN payment_receipts pr ON pr.id=ra.payment_receipt_id JOIN order_item_unit_amounts u ON u.id=ra.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE ra.refund_id=rid AND (pr.order_id<>rf.order_id OR oi.order_id<>rf.order_id OR NOT EXISTS(SELECT 1 FROM order_tender_allocations ta WHERE ta.receipt_id=pr.id AND ta.order_item_unit_amount_id=u.id) OR (rf.credit_note_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id WHERE ci.credit_note_id=rf.credit_note_id AND cu.order_item_unit_amount_id=u.id)))) THEN RAISE EXCEPTION 'REFUND_WRONG_SOURCE' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM refund_allocations ra JOIN refunds r ON r.id=ra.refund_id JOIN order_tender_allocations ta ON ta.receipt_id=ra.payment_receipt_id AND ta.order_item_unit_amount_id=ra.order_item_unit_amount_id WHERE r.order_id=rf.order_id AND r.status<>'failed' GROUP BY ta.id HAVING sum(ra.amount_vnd)>ta.amount_vnd) THEN RAISE EXCEPTION 'REFUND_EXCEEDS_SOURCE' USING ERRCODE='23514'; END IF;
 IF rf.credit_note_id IS NOT NULL AND (SELECT COALESCE(sum(amount_vnd),0) FROM refunds WHERE credit_note_id=rf.credit_note_id AND status<>'failed')>(SELECT total_vnd FROM credit_notes WHERE id=rf.credit_note_id AND status='issued') THEN RAISE EXCEPTION 'REFUND_EXCEEDS_CREDIT' USING ERRCODE='23514'; END IF;
 IF rf.credit_note_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM credit_notes WHERE id=rf.credit_note_id AND status='issued') THEN RAISE EXCEPTION 'REFUND_REQUIRES_ISSUED_CREDIT' USING ERRCODE='23514'; END IF;
 IF rf.credit_note_id IS NULL AND NOT EXISTS(SELECT 1 FROM orders WHERE id=rf.order_id AND order_status IN ('cancelled','reconciliation','expired')) THEN RAISE EXCEPTION 'REFUND_REQUIRES_CANCELLED_UNISSUED_ORDER' USING ERRCODE='23514'; END IF;
 IF rf.credit_note_id IS NOT NULL AND EXISTS(SELECT 1 FROM refund_allocations ra JOIN refunds r ON r.id=ra.refund_id JOIN credit_note_item_units cu ON cu.order_item_unit_amount_id=ra.order_item_unit_amount_id JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id AND ci.credit_note_id=r.credit_note_id WHERE r.credit_note_id=rf.credit_note_id AND r.status<>'failed' GROUP BY cu.credit_note_item_id,cu.order_item_unit_amount_id,cu.amount_vnd HAVING sum(ra.amount_vnd)>cu.amount_vnd) THEN RAISE EXCEPTION 'REFUND_EXCEEDS_CREDIT_UNIT' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE OR REPLACE FUNCTION guard_credit_child() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE cid uuid; oldcid uuid; BEGIN
 IF TG_TABLE_NAME='credit_note_items' THEN cid:=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_id ELSE NEW.credit_note_id END;
 ELSE SELECT credit_note_id INTO cid FROM credit_note_items WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_item_id ELSE NEW.credit_note_item_id END; END IF;
 IF TG_OP='UPDATE' THEN IF TG_TABLE_NAME='credit_note_items' THEN oldcid:=OLD.credit_note_id; ELSE SELECT credit_note_id INTO oldcid FROM credit_note_items WHERE id=OLD.credit_note_item_id; END IF; END IF;
 PERFORM 1 FROM credit_notes WHERE id IN(cid,oldcid) ORDER BY id FOR UPDATE;
 IF EXISTS(SELECT 1 FROM credit_notes WHERE id IN(cid,oldcid) AND status='issued') THEN RAISE EXCEPTION 'ISSUED_CREDIT_LINES_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
