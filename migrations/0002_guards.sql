CREATE FUNCTION reject_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'IMMUTABLE: %',TG_TABLE_NAME USING ERRCODE='23514'; END $$;
CREATE TRIGGER stock_append_only BEFORE UPDATE OR DELETE ON stock_movements FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER receipts_append_only BEFORE UPDATE OR DELETE ON payment_receipts FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER tenders_append_only BEFORE UPDATE OR DELETE ON order_tender_allocations FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER credit_ledger_append_only BEFORE UPDATE OR DELETE ON store_credit_entries FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER loyalty_append_only BEFORE UPDATE OR DELETE ON loyalty_entries FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER audit_append_only BEFORE UPDATE OR DELETE ON audit_events FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER landing_version_immutable BEFORE UPDATE OR DELETE ON landing_versions FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER publication_append_only BEFORE UPDATE OR DELETE ON landing_publications FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE TRIGGER settings_history_immutable BEFORE UPDATE OR DELETE ON setting_versions FOR EACH ROW EXECUTE FUNCTION reject_mutation();
CREATE FUNCTION guard_invoice() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF TG_OP<>'INSERT' AND OLD.lifecycle='issued' THEN RAISE EXCEPTION 'ISSUED_INVOICE_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER invoice_immutable BEFORE UPDATE OR DELETE ON invoices FOR EACH ROW EXECUTE FUNCTION guard_invoice();
CREATE FUNCTION guard_invoice_item() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE parent invoices; BEGIN
 SELECT * INTO parent FROM invoices WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.invoice_id ELSE NEW.invoice_id END FOR UPDATE;
 IF parent.lifecycle='issued' OR (TG_OP='UPDATE' AND EXISTS(SELECT 1 FROM invoices WHERE id=OLD.invoice_id AND lifecycle='issued')) THEN RAISE EXCEPTION 'ISSUED_LINES_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER invoice_item_immutable BEFORE INSERT OR UPDATE OR DELETE ON invoice_items FOR EACH ROW EXECUTE FUNCTION guard_invoice_item();
CREATE FUNCTION guard_credit_note() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF OLD.status='issued' THEN RAISE EXCEPTION 'ISSUED_CREDIT_NOTE_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER credit_note_immutable BEFORE UPDATE OR DELETE ON credit_notes FOR EACH ROW EXECUTE FUNCTION guard_credit_note();
CREATE FUNCTION guard_credit_child() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE cid uuid; BEGIN
 IF TG_TABLE_NAME='credit_note_items' THEN cid:=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_id ELSE NEW.credit_note_id END;
 ELSE SELECT credit_note_id INTO cid FROM credit_note_items WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_item_id ELSE NEW.credit_note_item_id END; END IF;
 PERFORM 1 FROM credit_notes WHERE id=cid FOR UPDATE;
 IF EXISTS(SELECT 1 FROM credit_notes WHERE id=cid AND status='issued') THEN RAISE EXCEPTION 'ISSUED_CREDIT_LINES_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER credit_items_immutable BEFORE INSERT OR UPDATE OR DELETE ON credit_note_items FOR EACH ROW EXECUTE FUNCTION guard_credit_child();
CREATE TRIGGER credit_units_immutable BEFORE INSERT OR UPDATE OR DELETE ON credit_note_item_units FOR EACH ROW EXECUTE FUNCTION guard_credit_child();
CREATE FUNCTION guard_order_snapshot() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE oid uuid; oldoid uuid; BEGIN
 IF TG_TABLE_NAME='order_items' THEN oid:=CASE WHEN TG_OP='DELETE' THEN OLD.order_id ELSE NEW.order_id END; IF TG_OP='UPDATE' THEN oldoid:=OLD.order_id; END IF;
 ELSE SELECT order_id INTO oid FROM order_items WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.order_item_id ELSE NEW.order_item_id END; IF TG_OP='UPDATE' THEN SELECT order_id INTO oldoid FROM order_items WHERE id=OLD.order_item_id; END IF; END IF;
 PERFORM 1 FROM orders WHERE id=oid FOR UPDATE;
 IF EXISTS(SELECT 1 FROM orders WHERE id IN (oid,oldoid) AND order_status<>'building') THEN RAISE EXCEPTION 'ORDER_SNAPSHOT_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER order_item_snapshot BEFORE INSERT OR UPDATE OR DELETE ON order_items FOR EACH ROW EXECUTE FUNCTION guard_order_snapshot();
CREATE TRIGGER order_unit_snapshot BEFORE INSERT OR UPDATE OR DELETE ON order_item_unit_amounts FOR EACH ROW EXECUTE FUNCTION guard_order_snapshot();
CREATE FUNCTION guard_order_header() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF OLD.order_status<>'building' AND (NEW.order_status='building' OR ROW(NEW.customer_id,NEW.total_vnd,NEW.payment_method,NEW.address_snapshot,NEW.currency,NEW.exchange_rate_vnd,NEW.policy_snapshot) IS DISTINCT FROM ROW(OLD.customer_id,OLD.total_vnd,OLD.payment_method,OLD.address_snapshot,OLD.currency,OLD.exchange_rate_vnd,OLD.policy_snapshot)) THEN RAISE EXCEPTION 'ORDER_HEADER_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER order_header_snapshot BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION guard_order_header();
CREATE FUNCTION check_order_totals() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE oid uuid; o orders; r record; n bigint; BEGIN
 IF TG_TABLE_NAME='orders' THEN oid:=NEW.id;
 ELSIF TG_TABLE_NAME='order_items' THEN oid:=COALESCE(NEW.order_id,OLD.order_id);
 ELSE SELECT order_id INTO oid FROM order_items WHERE id=COALESCE(NEW.order_item_id,OLD.order_item_id); END IF;
 SELECT * INTO o FROM orders WHERE id=oid FOR UPDATE;
 IF o.id IS NULL THEN RETURN NULL; END IF;
 IF o.order_status='building' THEN RAISE EXCEPTION 'UNFINISHED_ORDER' USING ERRCODE='23514'; END IF;
 IF o.total_vnd<>(SELECT COALESCE(sum(net_amount_vnd),0) FROM order_items WHERE order_id=oid) OR NOT EXISTS(SELECT 1 FROM order_items WHERE order_id=oid) THEN RAISE EXCEPTION 'ORDER_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 FOR r IN SELECT * FROM order_items WHERE order_id=oid LOOP
  SELECT count(*) INTO n FROM order_item_unit_amounts WHERE order_item_id=r.id;
  IF n<>r.quantity OR EXISTS(SELECT 1 FROM order_item_unit_amounts WHERE order_item_id=r.id AND unit_ordinal>r.quantity) OR ROW(r.quantity::bigint*r.unit_price_vnd,r.voucher_discount_vnd,r.points_discount_vnd,r.net_amount_vnd) IS DISTINCT FROM (SELECT ROW(sum(gross_vnd)::bigint,sum(voucher_discount_vnd)::bigint,sum(points_discount_vnd)::bigint,sum(net_amount_vnd)::bigint) FROM order_item_unit_amounts WHERE order_item_id=r.id) THEN RAISE EXCEPTION 'UNIT_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
  IF r.line_type='warranty' AND NOT EXISTS(SELECT 1 FROM order_items p WHERE p.id=r.parent_item_id AND p.line_type='product' AND p.order_id=oid) THEN RAISE EXCEPTION 'CARE_PARENT_INVALID' USING ERRCODE='23514'; END IF;
  IF EXISTS(SELECT 1 FROM order_item_unit_amounts u LEFT JOIN order_item_unit_amounts pu ON pu.id=u.parent_unit_amount_id WHERE u.order_item_id=r.id AND ((r.line_type='warranty' AND (pu.id IS NULL OR pu.order_item_id<>r.parent_item_id)) OR (r.line_type<>'warranty' AND pu.id IS NOT NULL))) THEN RAISE EXCEPTION 'CARE_UNIT_PARENT_INVALID' USING ERRCODE='23514'; END IF;
 END LOOP; RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER order_totals AFTER INSERT OR UPDATE ON orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals();
CREATE CONSTRAINT TRIGGER order_lines_totals AFTER INSERT OR UPDATE OR DELETE ON order_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals();
CREATE CONSTRAINT TRIGGER order_units_totals AFTER INSERT OR UPDATE OR DELETE ON order_item_unit_amounts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals();
CREATE FUNCTION check_invoice_totals() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE iid uuid; inv invoices; BEGIN
 iid:=CASE WHEN TG_TABLE_NAME='invoices' THEN COALESCE(NEW.id,OLD.id) ELSE COALESCE(NEW.invoice_id,OLD.invoice_id) END;
 SELECT * INTO inv FROM invoices WHERE id=iid FOR UPDATE;
 IF inv.id IS NULL OR inv.lifecycle='draft' THEN RETURN NULL; END IF;
 IF ROW(inv.subtotal_vnd,inv.discount_total_vnd,inv.grand_total_vnd) IS DISTINCT FROM (SELECT ROW(COALESCE(sum(quantity::bigint*unit_price_vnd),0)::bigint,COALESCE(sum(voucher_discount_vnd+points_discount_vnd),0)::bigint,COALESCE(sum(net_amount_vnd),0)::bigint) FROM invoice_items WHERE invoice_id=iid) OR (SELECT count(*) FROM invoice_items WHERE invoice_id=iid)<>(SELECT count(*) FROM order_items WHERE order_id=inv.order_id) THEN RAISE EXCEPTION 'INVOICE_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM invoice_items ii JOIN order_items oi ON oi.id=ii.order_item_id LEFT JOIN invoice_items parent ON parent.id=ii.parent_invoice_item_id WHERE ii.invoice_id=iid AND (ROW(ii.quantity,ii.unit_price_vnd,ii.voucher_discount_vnd,ii.points_discount_vnd,ii.net_amount_vnd,ii.line_type,ii.variant_id,ii.product_id) IS DISTINCT FROM ROW(oi.quantity,oi.unit_price_vnd,oi.voucher_discount_vnd,oi.points_discount_vnd,oi.net_amount_vnd,oi.line_type,oi.variant_id,oi.product_id) OR (ii.line_type='warranty' AND (parent.line_type<>'product' OR parent.order_item_id<>oi.parent_item_id)))) THEN RAISE EXCEPTION 'INVOICE_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER invoice_totals AFTER INSERT OR UPDATE ON invoices DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_invoice_totals();
CREATE CONSTRAINT TRIGGER invoice_lines_totals AFTER INSERT OR UPDATE OR DELETE ON invoice_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_invoice_totals();
CREATE FUNCTION check_tender() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE rid uuid; receipt payment_receipts; BEGIN
 rid:=CASE WHEN TG_TABLE_NAME='payment_receipts' THEN NEW.id ELSE NEW.receipt_id END;
 SELECT * INTO receipt FROM payment_receipts WHERE id=rid;
 PERFORM 1 FROM orders WHERE id=receipt.order_id FOR UPDATE;
 IF receipt.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM order_tender_allocations WHERE receipt_id=rid) THEN RAISE EXCEPTION 'RECEIPT_ALLOCATION_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE a.receipt_id=rid AND oi.order_id<>receipt.order_id) THEN RAISE EXCEPTION 'TENDER_WRONG_ORDER' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN payment_receipts pr ON pr.id=a.receipt_id JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id WHERE pr.order_id=receipt.order_id GROUP BY u.id,pr.tender_kind HAVING sum(a.amount_vnd)>CASE WHEN pr.tender_kind='store_credit' THEN u.credit_planned_vnd ELSE u.cash_planned_vnd END) THEN RAISE EXCEPTION 'TENDER_OVERFUNDED' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER receipt_totals AFTER INSERT ON payment_receipts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_tender();
CREATE CONSTRAINT TRIGGER tender_totals AFTER INSERT ON order_tender_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_tender();
CREATE FUNCTION check_credit_note() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE cid uuid; cn credit_notes; r record; BEGIN
 IF TG_TABLE_NAME='credit_notes' THEN cid:=NEW.id; ELSIF TG_TABLE_NAME='credit_note_items' THEN cid:=COALESCE(NEW.credit_note_id,OLD.credit_note_id); ELSE SELECT credit_note_id INTO cid FROM credit_note_items WHERE id=COALESCE(NEW.credit_note_item_id,OLD.credit_note_item_id); END IF;
 SELECT * INTO cn FROM credit_notes WHERE id=cid;
 IF cn.id IS NULL THEN RETURN NULL; END IF;
 PERFORM 1 FROM invoices WHERE id=cn.invoice_id FOR UPDATE;
 IF NOT EXISTS(SELECT 1 FROM invoices WHERE id=cn.invoice_id AND lifecycle='issued') THEN RAISE EXCEPTION 'CREDIT_REQUIRES_ISSUED_INVOICE' USING ERRCODE='23514'; END IF;
 IF cn.warranty_claim_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM warranty_claims wc JOIN warranty_entitlements e ON e.id=wc.entitlement_id JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE wc.id=cn.warranty_claim_id AND oi.order_id=cn.order_id) THEN RAISE EXCEPTION 'WARRANTY_WRONG_ORDER' USING ERRCODE='23514'; END IF;
 IF cn.status='issued' AND cn.total_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM credit_note_items WHERE credit_note_id=cid) THEN RAISE EXCEPTION 'CREDIT_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 FOR r IN SELECT cni.*,ii.invoice_id,ii.order_item_id FROM credit_note_items cni JOIN invoice_items ii ON ii.id=cni.invoice_item_id WHERE credit_note_id=cid LOOP
  IF r.invoice_id<>cn.invoice_id THEN RAISE EXCEPTION 'CREDIT_WRONG_INVOICE' USING ERRCODE='23514'; END IF;
  IF EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN order_item_unit_amounts u ON u.id=cu.order_item_unit_amount_id WHERE cu.credit_note_item_id=r.id AND u.order_item_id<>r.order_item_id) THEN RAISE EXCEPTION 'CREDIT_WRONG_UNIT' USING ERRCODE='23514'; END IF;
  IF cn.status='issued' AND (r.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM credit_note_item_units WHERE credit_note_item_id=r.id) OR (r.quantity IS NOT NULL AND r.quantity<>(SELECT count(*) FROM credit_note_item_units WHERE credit_note_item_id=r.id))) THEN RAISE EXCEPTION 'CREDIT_UNITS_MISMATCH' USING ERRCODE='23514'; END IF;
 END LOOP;
 IF EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id JOIN credit_notes c ON c.id=ci.credit_note_id JOIN order_item_unit_amounts u ON u.id=cu.order_item_unit_amount_id WHERE c.invoice_id=cn.invoice_id GROUP BY u.id HAVING sum(cu.amount_vnd)>u.net_amount_vnd OR count(*)>1) THEN RAISE EXCEPTION 'CREDIT_EXCEEDS_UNIT' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER credit_totals AFTER INSERT OR UPDATE ON credit_notes DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note();
CREATE CONSTRAINT TRIGGER credit_item_totals AFTER INSERT OR UPDATE OR DELETE ON credit_note_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note();
CREATE CONSTRAINT TRIGGER credit_unit_totals AFTER INSERT OR UPDATE OR DELETE ON credit_note_item_units DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note();
CREATE FUNCTION check_refund() RETURNS trigger LANGUAGE plpgsql AS $$ DECLARE rid uuid; rf refunds; BEGIN
 rid:=CASE WHEN TG_TABLE_NAME='refunds' THEN NEW.id ELSE NEW.refund_id END;
 SELECT * INTO rf FROM refunds WHERE id=rid;
 PERFORM 1 FROM orders WHERE id=rf.order_id FOR UPDATE;
 IF rf.credit_note_id IS NULL AND EXISTS(SELECT 1 FROM invoices WHERE order_id=rf.order_id AND lifecycle='issued') THEN RAISE EXCEPTION 'REFUND_REQUIRES_CREDIT_NOTE' USING ERRCODE='23514'; END IF;
 IF rf.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM refund_allocations WHERE refund_id=rid) THEN RAISE EXCEPTION 'REFUND_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM refund_allocations ra JOIN payment_receipts pr ON pr.id=ra.payment_receipt_id JOIN order_item_unit_amounts u ON u.id=ra.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE ra.refund_id=rid AND (pr.order_id<>rf.order_id OR oi.order_id<>rf.order_id OR NOT EXISTS(SELECT 1 FROM order_tender_allocations ta WHERE ta.receipt_id=pr.id AND ta.order_item_unit_amount_id=u.id) OR (rf.credit_note_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id WHERE ci.credit_note_id=rf.credit_note_id AND cu.order_item_unit_amount_id=u.id)))) THEN RAISE EXCEPTION 'REFUND_WRONG_SOURCE' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM refund_allocations ra JOIN refunds r ON r.id=ra.refund_id JOIN order_tender_allocations ta ON ta.receipt_id=ra.payment_receipt_id AND ta.order_item_unit_amount_id=ra.order_item_unit_amount_id WHERE r.order_id=rf.order_id AND r.status<>'failed' GROUP BY ta.id HAVING sum(ra.amount_vnd)>ta.amount_vnd) THEN RAISE EXCEPTION 'REFUND_EXCEEDS_SOURCE' USING ERRCODE='23514'; END IF;
 IF rf.credit_note_id IS NOT NULL AND (SELECT COALESCE(sum(amount_vnd),0) FROM refunds WHERE credit_note_id=rf.credit_note_id AND status<>'failed')>(SELECT total_vnd FROM credit_notes WHERE id=rf.credit_note_id AND status='issued') THEN RAISE EXCEPTION 'REFUND_EXCEEDS_CREDIT' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER refund_totals AFTER INSERT OR UPDATE ON refunds DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_refund();
CREATE CONSTRAINT TRIGGER refund_allocation_totals AFTER INSERT OR UPDATE ON refund_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_refund();
CREATE FUNCTION used_promotion_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM promotion_redemptions WHERE promotion_version_id=OLD.id) THEN RAISE EXCEPTION 'USED_PROMOTION_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER promotion_version_guard BEFORE UPDATE OR DELETE ON promotion_versions FOR EACH ROW EXECUTE FUNCTION used_promotion_immutable();
CREATE FUNCTION cms_permission() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF current_setting('botzone.actor_role',true) IS DISTINCT FROM 'content_staff' THEN RAISE EXCEPTION 'CMS_CONTENT_ONLY' USING ERRCODE='42501'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER landing_draft_permission BEFORE INSERT OR UPDATE OR DELETE ON landing_drafts FOR EACH ROW EXECUTE FUNCTION cms_permission();
CREATE TRIGGER landing_version_permission BEFORE INSERT ON landing_versions FOR EACH ROW EXECUTE FUNCTION cms_permission();
CREATE TRIGGER landing_publication_permission BEFORE INSERT ON landing_publications FOR EACH ROW EXECUTE FUNCTION cms_permission();
