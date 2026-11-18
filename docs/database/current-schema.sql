--
-- PostgreSQL database dump
--

\restrict 8onSh8xUKB9OguQJM3eED05OqkmESAN7RyJySZR4H935rFSBJqtQBCkWmzzc0Qs

-- Dumped from database version 18.1 (Debian 18.1-1.pgdg12+2)
-- Dumped by pg_dump version 18.1 (Debian 18.1-1.pgdg12+2)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- Name: check_credit_note(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.check_credit_note() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE cid uuid; cn credit_notes; r record; BEGIN
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


ALTER FUNCTION public.check_credit_note() OWNER TO botzone;

--
-- Name: check_invoice_totals(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.check_invoice_totals() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE iid uuid; inv invoices; BEGIN
 iid:=CASE WHEN TG_TABLE_NAME='invoices' THEN COALESCE(NEW.id,OLD.id) ELSE COALESCE(NEW.invoice_id,OLD.invoice_id) END;
 SELECT * INTO inv FROM invoices WHERE id=iid FOR UPDATE;
 IF inv.id IS NULL OR inv.lifecycle='draft' THEN RETURN NULL; END IF;
 IF ROW(inv.subtotal_vnd,inv.discount_total_vnd,inv.grand_total_vnd) IS DISTINCT FROM (SELECT ROW(COALESCE(sum(quantity::bigint*unit_price_vnd),0)::bigint,COALESCE(sum(voucher_discount_vnd+points_discount_vnd),0)::bigint,COALESCE(sum(net_amount_vnd),0)::bigint) FROM invoice_items WHERE invoice_id=iid) OR (SELECT count(*) FROM invoice_items WHERE invoice_id=iid)<>(SELECT count(*) FROM order_items WHERE order_id=inv.order_id) THEN RAISE EXCEPTION 'INVOICE_TOTAL_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM invoice_items ii JOIN order_items oi ON oi.id=ii.order_item_id LEFT JOIN invoice_items parent ON parent.id=ii.parent_invoice_item_id WHERE ii.invoice_id=iid AND (ROW(ii.quantity,ii.unit_price_vnd,ii.voucher_discount_vnd,ii.points_discount_vnd,ii.net_amount_vnd,ii.line_type,ii.variant_id,ii.product_id) IS DISTINCT FROM ROW(oi.quantity,oi.unit_price_vnd,oi.voucher_discount_vnd,oi.points_discount_vnd,oi.net_amount_vnd,oi.line_type,oi.variant_id,oi.product_id) OR (ii.line_type='warranty' AND (parent.line_type<>'product' OR parent.order_item_id<>oi.parent_item_id)))) THEN RAISE EXCEPTION 'INVOICE_SOURCE_MISMATCH' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;


ALTER FUNCTION public.check_invoice_totals() OWNER TO botzone;

--
-- Name: check_order_totals(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.check_order_totals() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE oid uuid; o orders; r record; n bigint; BEGIN
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


ALTER FUNCTION public.check_order_totals() OWNER TO botzone;

--
-- Name: check_refund(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.check_refund() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE rid uuid; rf refunds; BEGIN
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


ALTER FUNCTION public.check_refund() OWNER TO botzone;

--
-- Name: check_tender(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.check_tender() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE rid uuid; receipt payment_receipts; BEGIN
 rid:=CASE WHEN TG_TABLE_NAME='payment_receipts' THEN NEW.id ELSE NEW.receipt_id END;
 SELECT * INTO receipt FROM payment_receipts WHERE id=rid;
 PERFORM 1 FROM orders WHERE id=receipt.order_id FOR UPDATE;
 IF receipt.amount_vnd<>(SELECT COALESCE(sum(amount_vnd),0) FROM order_tender_allocations WHERE receipt_id=rid) THEN RAISE EXCEPTION 'RECEIPT_ALLOCATION_MISMATCH' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id JOIN order_items oi ON oi.id=u.order_item_id WHERE a.receipt_id=rid AND oi.order_id<>receipt.order_id) THEN RAISE EXCEPTION 'TENDER_WRONG_ORDER' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM order_tender_allocations a JOIN payment_receipts pr ON pr.id=a.receipt_id JOIN order_item_unit_amounts u ON u.id=a.order_item_unit_amount_id WHERE pr.order_id=receipt.order_id GROUP BY u.id,pr.tender_kind HAVING sum(a.amount_vnd)>CASE WHEN pr.tender_kind='store_credit' THEN u.credit_planned_vnd ELSE u.cash_planned_vnd END) THEN RAISE EXCEPTION 'TENDER_OVERFUNDED' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;


ALTER FUNCTION public.check_tender() OWNER TO botzone;

--
-- Name: cms_permission(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.cms_permission() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN
 IF current_setting('botzone.actor_role',true) IS DISTINCT FROM 'content_staff' THEN RAISE EXCEPTION 'CMS_CONTENT_ONLY' USING ERRCODE='42501'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.cms_permission() OWNER TO botzone;

--
-- Name: guard_credit_child(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_credit_child() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE cid uuid; BEGIN
 IF TG_TABLE_NAME='credit_note_items' THEN cid:=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_id ELSE NEW.credit_note_id END;
 ELSE SELECT credit_note_id INTO cid FROM credit_note_items WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.credit_note_item_id ELSE NEW.credit_note_item_id END; END IF;
 PERFORM 1 FROM credit_notes WHERE id=cid FOR UPDATE;
 IF EXISTS(SELECT 1 FROM credit_notes WHERE id=cid AND status='issued') THEN RAISE EXCEPTION 'ISSUED_CREDIT_LINES_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.guard_credit_child() OWNER TO botzone;

--
-- Name: guard_credit_note(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_credit_note() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN
 IF OLD.status='issued' THEN RAISE EXCEPTION 'ISSUED_CREDIT_NOTE_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.guard_credit_note() OWNER TO botzone;

--
-- Name: guard_invoice(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_invoice() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN
 IF TG_OP<>'INSERT' AND OLD.lifecycle='issued' THEN RAISE EXCEPTION 'ISSUED_INVOICE_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.guard_invoice() OWNER TO botzone;

--
-- Name: guard_invoice_item(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_invoice_item() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE parent invoices; BEGIN
 SELECT * INTO parent FROM invoices WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.invoice_id ELSE NEW.invoice_id END FOR UPDATE;
 IF parent.lifecycle='issued' OR (TG_OP='UPDATE' AND EXISTS(SELECT 1 FROM invoices WHERE id=OLD.invoice_id AND lifecycle='issued')) THEN RAISE EXCEPTION 'ISSUED_LINES_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.guard_invoice_item() OWNER TO botzone;

--
-- Name: guard_order_header(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_order_header() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN
 IF OLD.order_status<>'building' AND (NEW.order_status='building' OR ROW(NEW.customer_id,NEW.total_vnd,NEW.payment_method,NEW.address_snapshot,NEW.currency,NEW.exchange_rate_vnd,NEW.policy_snapshot) IS DISTINCT FROM ROW(OLD.customer_id,OLD.total_vnd,OLD.payment_method,OLD.address_snapshot,OLD.currency,OLD.exchange_rate_vnd,OLD.policy_snapshot)) THEN RAISE EXCEPTION 'ORDER_HEADER_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;


ALTER FUNCTION public.guard_order_header() OWNER TO botzone;

--
-- Name: guard_order_snapshot(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.guard_order_snapshot() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ DECLARE oid uuid; oldoid uuid; BEGIN
 IF TG_TABLE_NAME='order_items' THEN oid:=CASE WHEN TG_OP='DELETE' THEN OLD.order_id ELSE NEW.order_id END; IF TG_OP='UPDATE' THEN oldoid:=OLD.order_id; END IF;
 ELSE SELECT order_id INTO oid FROM order_items WHERE id=CASE WHEN TG_OP='DELETE' THEN OLD.order_item_id ELSE NEW.order_item_id END; IF TG_OP='UPDATE' THEN SELECT order_id INTO oldoid FROM order_items WHERE id=OLD.order_item_id; END IF; END IF;
 PERFORM 1 FROM orders WHERE id=oid FOR UPDATE;
 IF EXISTS(SELECT 1 FROM orders WHERE id IN (oid,oldoid) AND order_status<>'building') THEN RAISE EXCEPTION 'ORDER_SNAPSHOT_IMMUTABLE' USING ERRCODE='23514'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.guard_order_snapshot() OWNER TO botzone;

--
-- Name: reject_mutation(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.reject_mutation() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN RAISE EXCEPTION 'IMMUTABLE: %',TG_TABLE_NAME USING ERRCODE='23514'; END $$;


ALTER FUNCTION public.reject_mutation() OWNER TO botzone;

--
-- Name: used_promotion_immutable(); Type: FUNCTION; Schema: public; Owner: botzone
--

CREATE FUNCTION public.used_promotion_immutable() RETURNS trigger
    LANGUAGE plpgsql
    AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM promotion_redemptions WHERE promotion_version_id=OLD.id) THEN RAISE EXCEPTION 'USED_PROMOTION_IMMUTABLE' USING ERRCODE='23514'; END IF; RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;


ALTER FUNCTION public.used_promotion_immutable() OWNER TO botzone;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: account; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.account (
    id text NOT NULL,
    account_id text NOT NULL,
    provider_id text NOT NULL,
    user_id text NOT NULL,
    access_token text,
    refresh_token text,
    id_token text,
    access_token_expires_at timestamp with time zone,
    refresh_token_expires_at timestamp with time zone,
    scope text,
    password text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.account OWNER TO botzone;

--
-- Name: addresses; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.addresses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    user_id text NOT NULL,
    recipient text NOT NULL,
    phone text NOT NULL,
    province text NOT NULL,
    ward text NOT NULL,
    address_line text NOT NULL,
    is_default boolean DEFAULT false NOT NULL
);


ALTER TABLE public.addresses OWNER TO botzone;

--
-- Name: app_settings; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.app_settings (
    key text NOT NULL,
    value jsonb NOT NULL,
    version integer DEFAULT 1 NOT NULL
);


ALTER TABLE public.app_settings OWNER TO botzone;

--
-- Name: attribute_definitions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.attribute_definitions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    category_id uuid NOT NULL,
    code text NOT NULL,
    value_type text NOT NULL,
    unit text,
    compare_enabled boolean DEFAULT true NOT NULL,
    CONSTRAINT attribute_definitions_value_type_check CHECK ((value_type = ANY (ARRAY['text'::text, 'number'::text, 'boolean'::text, 'option'::text])))
);


ALTER TABLE public.attribute_definitions OWNER TO botzone;

--
-- Name: audit_events; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.audit_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    actor_user_id text,
    action text NOT NULL,
    entity_type text NOT NULL,
    entity_id text,
    before_data jsonb,
    after_data jsonb,
    reason text,
    occurred_at timestamp with time zone DEFAULT now() NOT NULL,
    correlation_id text
);


ALTER TABLE public.audit_events OWNER TO botzone;

--
-- Name: benefit_holds; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.benefit_holds (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    customer_id text NOT NULL,
    order_id uuid NOT NULL,
    kind text NOT NULL,
    amount bigint NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    status text NOT NULL,
    CONSTRAINT benefit_holds_amount_check CHECK ((amount > 0)),
    CONSTRAINT benefit_holds_kind_check CHECK ((kind = ANY (ARRAY['points'::text, 'store_credit'::text]))),
    CONSTRAINT benefit_holds_status_check CHECK ((status = ANY (ARRAY['active'::text, 'consumed'::text, 'released'::text])))
);


ALTER TABLE public.benefit_holds OWNER TO botzone;

--
-- Name: cart_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.cart_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    cart_id uuid NOT NULL,
    variant_id uuid NOT NULL,
    quantity integer NOT NULL,
    care boolean DEFAULT false NOT NULL,
    CONSTRAINT cart_items_quantity_check CHECK (((quantity > 0) AND (quantity <= 100)))
);


ALTER TABLE public.cart_items OWNER TO botzone;

--
-- Name: carts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.carts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    customer_id text NOT NULL,
    applied_promotion_version_id uuid,
    revision integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.carts OWNER TO botzone;

--
-- Name: categories; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.categories (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code text NOT NULL,
    slug text NOT NULL,
    name_vi text NOT NULL,
    name_en text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


ALTER TABLE public.categories OWNER TO botzone;

--
-- Name: credit_note_item_units; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.credit_note_item_units (
    credit_note_item_id uuid NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    amount_vnd bigint NOT NULL,
    CONSTRAINT credit_note_item_units_amount_vnd_check CHECK ((amount_vnd >= 0))
);


ALTER TABLE public.credit_note_item_units OWNER TO botzone;

--
-- Name: credit_note_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.credit_note_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    credit_note_id uuid NOT NULL,
    invoice_item_id uuid NOT NULL,
    quantity integer,
    amount_vnd bigint NOT NULL,
    CONSTRAINT credit_note_items_amount_vnd_check CHECK ((amount_vnd >= 0)),
    CONSTRAINT credit_note_items_check CHECK (((quantity > 0) OR (amount_vnd > 0))),
    CONSTRAINT credit_note_items_quantity_check CHECK ((quantity > 0))
);


ALTER TABLE public.credit_note_items OWNER TO botzone;

--
-- Name: credit_notes; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.credit_notes (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    credit_note_no text NOT NULL,
    invoice_id uuid NOT NULL,
    order_id uuid NOT NULL,
    status text NOT NULL,
    issued_at timestamp with time zone,
    return_request_id uuid,
    warranty_claim_id uuid,
    shipment_id uuid,
    reason text NOT NULL,
    total_vnd bigint NOT NULL,
    approved_by_user_id text NOT NULL,
    event_key text NOT NULL,
    CONSTRAINT credit_notes_check CHECK ((num_nonnulls(return_request_id, warranty_claim_id, shipment_id) = 1)),
    CONSTRAINT credit_notes_check1 CHECK (((status <> 'issued'::text) OR (issued_at IS NOT NULL))),
    CONSTRAINT credit_notes_status_check CHECK ((status = ANY (ARRAY['draft'::text, 'issued'::text]))),
    CONSTRAINT credit_notes_total_vnd_check CHECK ((total_vnd >= 0))
);


ALTER TABLE public.credit_notes OWNER TO botzone;

--
-- Name: customer_profiles; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.customer_profiles (
    user_id text NOT NULL,
    display_name text NOT NULL,
    phone text
);


ALTER TABLE public.customer_profiles OWNER TO botzone;

--
-- Name: goods_receipt_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.goods_receipt_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    receipt_id uuid NOT NULL,
    purchase_order_id uuid NOT NULL,
    purchase_order_item_id uuid NOT NULL,
    received_qty integer NOT NULL,
    unit_cost_vnd bigint NOT NULL,
    CONSTRAINT goods_receipt_items_received_qty_check CHECK ((received_qty > 0)),
    CONSTRAINT goods_receipt_items_unit_cost_vnd_check CHECK ((unit_cost_vnd >= 0))
);


ALTER TABLE public.goods_receipt_items OWNER TO botzone;

--
-- Name: goods_receipts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.goods_receipts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    purchase_order_id uuid NOT NULL,
    receipt_no text NOT NULL,
    received_by text NOT NULL,
    received_at timestamp with time zone DEFAULT now() NOT NULL,
    event_key text NOT NULL
);


ALTER TABLE public.goods_receipts OWNER TO botzone;

--
-- Name: idempotency_records; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.idempotency_records (
    scope text NOT NULL,
    key text NOT NULL,
    request_hash text NOT NULL,
    result jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.idempotency_records OWNER TO botzone;

--
-- Name: inventory_allocations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.inventory_allocations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_id uuid NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    lot_id uuid NOT NULL,
    inventory_unit_id uuid,
    quantity integer NOT NULL,
    cost_vnd bigint NOT NULL,
    state text NOT NULL,
    CONSTRAINT inventory_allocations_check CHECK (((inventory_unit_id IS NULL) OR (quantity = 1))),
    CONSTRAINT inventory_allocations_cost_vnd_check CHECK ((cost_vnd >= 0)),
    CONSTRAINT inventory_allocations_quantity_check CHECK ((quantity > 0)),
    CONSTRAINT inventory_allocations_state_check CHECK ((state = ANY (ARRAY['reserved'::text, 'shipped'::text, 'returned'::text, 'released'::text, 'replaced'::text])))
);


ALTER TABLE public.inventory_allocations OWNER TO botzone;

--
-- Name: inventory_identifiers; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.inventory_identifiers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    inventory_unit_id uuid NOT NULL,
    kind text NOT NULL,
    normalized_value text NOT NULL,
    CONSTRAINT inventory_identifiers_kind_check CHECK ((kind = ANY (ARRAY['serial'::text, 'imei'::text])))
);


ALTER TABLE public.inventory_identifiers OWNER TO botzone;

--
-- Name: inventory_lots; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.inventory_lots (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    warehouse_id uuid NOT NULL,
    variant_id uuid NOT NULL,
    receipt_item_id uuid NOT NULL,
    received_at timestamp with time zone DEFAULT now() NOT NULL,
    unit_cost_vnd bigint NOT NULL,
    qty_on_hand integer NOT NULL,
    qty_reserved integer DEFAULT 0 NOT NULL,
    CONSTRAINT inventory_lots_check CHECK (((qty_reserved >= 0) AND (qty_reserved <= qty_on_hand))),
    CONSTRAINT inventory_lots_qty_on_hand_check CHECK ((qty_on_hand >= 0)),
    CONSTRAINT inventory_lots_unit_cost_vnd_check CHECK ((unit_cost_vnd >= 0))
);


ALTER TABLE public.inventory_lots OWNER TO botzone;

--
-- Name: inventory_reservations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.inventory_reservations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_id uuid NOT NULL,
    variant_id uuid NOT NULL,
    quantity integer NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    status text NOT NULL,
    CONSTRAINT inventory_reservations_quantity_check CHECK ((quantity > 0)),
    CONSTRAINT inventory_reservations_status_check CHECK ((status = ANY (ARRAY['active'::text, 'released'::text, 'consumed'::text, 'expired'::text])))
);


ALTER TABLE public.inventory_reservations OWNER TO botzone;

--
-- Name: inventory_units; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.inventory_units (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    lot_id uuid NOT NULL,
    condition_status text DEFAULT 'new'::text NOT NULL,
    custody_status text DEFAULT 'available'::text NOT NULL,
    CONSTRAINT inventory_units_condition_status_check CHECK ((condition_status = ANY (ARRAY['new'::text, 'used'::text, 'faulty'::text, 'opened'::text]))),
    CONSTRAINT inventory_units_custody_status_check CHECK ((custody_status = ANY (ARRAY['available'::text, 'reserved'::text, 'shipped'::text, 'quarantine'::text, 'service'::text])))
);


ALTER TABLE public.inventory_units OWNER TO botzone;

--
-- Name: invoice_exports; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.invoice_exports (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    requested_by_user_id text NOT NULL,
    query_json jsonb NOT NULL,
    read_at timestamp with time zone NOT NULL,
    row_count integer NOT NULL,
    total_vnd bigint NOT NULL,
    status text NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    file_manifest jsonb DEFAULT '{}'::jsonb NOT NULL,
    role_snapshot text NOT NULL,
    CONSTRAINT invoice_exports_row_count_check CHECK ((row_count >= 0)),
    CONSTRAINT invoice_exports_status_check CHECK ((status = ANY (ARRAY['building'::text, 'ready'::text, 'failed'::text])))
);


ALTER TABLE public.invoice_exports OWNER TO botzone;

--
-- Name: invoice_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.invoice_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    invoice_id uuid NOT NULL,
    order_id uuid NOT NULL,
    order_item_id uuid NOT NULL,
    line_no integer NOT NULL,
    line_type text NOT NULL,
    product_id uuid,
    variant_id uuid,
    parent_invoice_item_id uuid,
    sku_snapshot text NOT NULL,
    name_snapshot text NOT NULL,
    variant_snapshot jsonb NOT NULL,
    quantity integer NOT NULL,
    unit_price_vnd bigint NOT NULL,
    voucher_discount_vnd bigint NOT NULL,
    points_discount_vnd bigint NOT NULL,
    net_amount_vnd bigint NOT NULL,
    CONSTRAINT invoice_items_check CHECK (((net_amount_vnd >= 0) AND (net_amount_vnd = ((((quantity)::bigint * unit_price_vnd) - voucher_discount_vnd) - points_discount_vnd)))),
    CONSTRAINT invoice_items_check1 CHECK (((line_type <> 'product'::text) OR ((product_id IS NOT NULL) AND (variant_id IS NOT NULL)))),
    CONSTRAINT invoice_items_check2 CHECK (((line_type = 'warranty'::text) = (parent_invoice_item_id IS NOT NULL))),
    CONSTRAINT invoice_items_check3 CHECK (((line_type <> 'shipping'::text) OR (quantity = 1))),
    CONSTRAINT invoice_items_line_no_check CHECK ((line_no > 0)),
    CONSTRAINT invoice_items_line_type_check CHECK ((line_type = ANY (ARRAY['product'::text, 'warranty'::text, 'shipping'::text]))),
    CONSTRAINT invoice_items_points_discount_vnd_check CHECK ((points_discount_vnd >= 0)),
    CONSTRAINT invoice_items_quantity_check CHECK ((quantity > 0)),
    CONSTRAINT invoice_items_unit_price_vnd_check CHECK ((unit_price_vnd >= 0)),
    CONSTRAINT invoice_items_voucher_discount_vnd_check CHECK ((voucher_discount_vnd >= 0))
);


ALTER TABLE public.invoice_items OWNER TO botzone;

--
-- Name: invoice_number_seq; Type: SEQUENCE; Schema: public; Owner: botzone
--

CREATE SEQUENCE public.invoice_number_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.invoice_number_seq OWNER TO botzone;

--
-- Name: invoices; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.invoices (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    invoice_no text,
    order_id uuid NOT NULL,
    lifecycle text NOT NULL,
    issued_at timestamp with time zone,
    customer_id text NOT NULL,
    responsible_staff_id text,
    issued_by_user_id text,
    currency text DEFAULT 'VND'::text NOT NULL,
    subtotal_vnd bigint NOT NULL,
    discount_total_vnd bigint NOT NULL,
    grand_total_vnd bigint NOT NULL,
    customer_snapshot jsonb NOT NULL,
    staff_snapshot jsonb NOT NULL,
    CONSTRAINT invoices_check CHECK (((grand_total_vnd >= 0) AND (grand_total_vnd = (subtotal_vnd - discount_total_vnd)))),
    CONSTRAINT invoices_check1 CHECK (((lifecycle <> 'issued'::text) OR ((invoice_no IS NOT NULL) AND (issued_at IS NOT NULL) AND (responsible_staff_id IS NOT NULL) AND (issued_by_user_id IS NOT NULL)))),
    CONSTRAINT invoices_currency_check CHECK ((currency = 'VND'::text)),
    CONSTRAINT invoices_discount_total_vnd_check CHECK ((discount_total_vnd >= 0)),
    CONSTRAINT invoices_lifecycle_check CHECK ((lifecycle = ANY (ARRAY['draft'::text, 'issued'::text]))),
    CONSTRAINT invoices_subtotal_vnd_check CHECK ((subtotal_vnd >= 0))
);


ALTER TABLE public.invoices OWNER TO botzone;

--
-- Name: landing_drafts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.landing_drafts (
    page_id uuid NOT NULL,
    revision integer DEFAULT 0 NOT NULL,
    sections jsonb NOT NULL,
    updated_by text,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.landing_drafts OWNER TO botzone;

--
-- Name: landing_media_refs; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.landing_media_refs (
    landing_version_id uuid NOT NULL,
    media_id uuid NOT NULL
);


ALTER TABLE public.landing_media_refs OWNER TO botzone;

--
-- Name: landing_pages; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.landing_pages (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    slug text NOT NULL
);


ALTER TABLE public.landing_pages OWNER TO botzone;

--
-- Name: landing_publications; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.landing_publications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    page_id uuid NOT NULL,
    version_id uuid NOT NULL,
    published_by text NOT NULL,
    published_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.landing_publications OWNER TO botzone;

--
-- Name: landing_versions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.landing_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    page_id uuid NOT NULL,
    version_no integer NOT NULL,
    sections jsonb NOT NULL,
    created_by text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.landing_versions OWNER TO botzone;

--
-- Name: loyalty_accounts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.loyalty_accounts (
    user_id text NOT NULL,
    balance_points integer DEFAULT 0 NOT NULL,
    net_spend_vnd bigint DEFAULT 0 NOT NULL,
    CONSTRAINT loyalty_accounts_net_spend_vnd_check CHECK ((net_spend_vnd >= 0))
);


ALTER TABLE public.loyalty_accounts OWNER TO botzone;

--
-- Name: loyalty_entries; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.loyalty_entries (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    user_id text NOT NULL,
    delta integer NOT NULL,
    kind text NOT NULL,
    order_id uuid NOT NULL,
    unit_id uuid,
    event_key text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT loyalty_entries_delta_check CHECK ((delta <> 0)),
    CONSTRAINT loyalty_entries_kind_check CHECK ((kind = ANY (ARRAY['earn'::text, 'redeem'::text, 'restore'::text, 'reverse'::text])))
);


ALTER TABLE public.loyalty_entries OWNER TO botzone;

--
-- Name: media_assets; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.media_assets (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    storage_key text NOT NULL,
    visibility text NOT NULL,
    mime text NOT NULL,
    size_bytes bigint NOT NULL,
    checksum text NOT NULL,
    validation_status text NOT NULL,
    source_url text,
    license_note text,
    owner_user_id text,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT media_assets_size_bytes_check CHECK ((size_bytes > 0)),
    CONSTRAINT media_assets_validation_status_check CHECK ((validation_status = ANY (ARRAY['valid'::text, 'invalid'::text, 'pending'::text]))),
    CONSTRAINT media_assets_visibility_check CHECK ((visibility = ANY (ARRAY['public'::text, 'private'::text, 'staging'::text])))
);


ALTER TABLE public.media_assets OWNER TO botzone;

--
-- Name: notifications; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.notifications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    recipient_user_id text NOT NULL,
    type text NOT NULL,
    message text NOT NULL,
    entity_id uuid,
    read_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.notifications OWNER TO botzone;

--
-- Name: order_adjustments; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.order_adjustments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_id uuid NOT NULL,
    kind text NOT NULL,
    promotion_redemption_id uuid,
    amount_vnd bigint NOT NULL,
    source_event_key text NOT NULL,
    CONSTRAINT order_adjustments_amount_vnd_check CHECK ((amount_vnd >= 0)),
    CONSTRAINT order_adjustments_kind_check CHECK ((kind = ANY (ARRAY['voucher'::text, 'points'::text])))
);


ALTER TABLE public.order_adjustments OWNER TO botzone;

--
-- Name: order_item_unit_amounts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.order_item_unit_amounts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_id uuid NOT NULL,
    unit_ordinal integer NOT NULL,
    parent_unit_amount_id uuid,
    gross_vnd bigint NOT NULL,
    voucher_discount_vnd bigint NOT NULL,
    redeemed_points integer DEFAULT 0 NOT NULL,
    points_discount_vnd bigint NOT NULL,
    net_amount_vnd bigint NOT NULL,
    credit_planned_vnd bigint NOT NULL,
    cash_planned_vnd bigint NOT NULL,
    CONSTRAINT order_item_unit_amounts_check CHECK ((points_discount_vnd = ((redeemed_points)::bigint * 1000))),
    CONSTRAINT order_item_unit_amounts_check1 CHECK (((net_amount_vnd >= 0) AND (net_amount_vnd = ((gross_vnd - voucher_discount_vnd) - points_discount_vnd)))),
    CONSTRAINT order_item_unit_amounts_check2 CHECK (((cash_planned_vnd >= 0) AND ((cash_planned_vnd + credit_planned_vnd) = net_amount_vnd))),
    CONSTRAINT order_item_unit_amounts_credit_planned_vnd_check CHECK ((credit_planned_vnd >= 0)),
    CONSTRAINT order_item_unit_amounts_gross_vnd_check CHECK ((gross_vnd >= 0)),
    CONSTRAINT order_item_unit_amounts_redeemed_points_check CHECK ((redeemed_points >= 0)),
    CONSTRAINT order_item_unit_amounts_unit_ordinal_check CHECK ((unit_ordinal > 0)),
    CONSTRAINT order_item_unit_amounts_voucher_discount_vnd_check CHECK ((voucher_discount_vnd >= 0))
);


ALTER TABLE public.order_item_unit_amounts OWNER TO botzone;

--
-- Name: order_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.order_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    line_no integer NOT NULL,
    line_type text NOT NULL,
    product_id uuid,
    variant_id uuid,
    parent_item_id uuid,
    quantity integer NOT NULL,
    unit_price_vnd bigint NOT NULL,
    voucher_discount_vnd bigint DEFAULT 0 NOT NULL,
    points_discount_vnd bigint DEFAULT 0 NOT NULL,
    net_amount_vnd bigint NOT NULL,
    snapshot jsonb NOT NULL,
    CONSTRAINT order_items_check CHECK (((net_amount_vnd >= 0) AND (net_amount_vnd = ((((quantity)::bigint * unit_price_vnd) - voucher_discount_vnd) - points_discount_vnd)))),
    CONSTRAINT order_items_check1 CHECK (((line_type <> 'product'::text) OR ((product_id IS NOT NULL) AND (variant_id IS NOT NULL)))),
    CONSTRAINT order_items_check2 CHECK (((line_type = 'warranty'::text) = (parent_item_id IS NOT NULL))),
    CONSTRAINT order_items_check3 CHECK (((line_type <> 'shipping'::text) OR (quantity = 1))),
    CONSTRAINT order_items_line_no_check CHECK ((line_no > 0)),
    CONSTRAINT order_items_line_type_check CHECK ((line_type = ANY (ARRAY['product'::text, 'warranty'::text, 'shipping'::text]))),
    CONSTRAINT order_items_points_discount_vnd_check CHECK ((points_discount_vnd >= 0)),
    CONSTRAINT order_items_quantity_check CHECK ((quantity > 0)),
    CONSTRAINT order_items_unit_price_vnd_check CHECK ((unit_price_vnd >= 0)),
    CONSTRAINT order_items_voucher_discount_vnd_check CHECK ((voucher_discount_vnd >= 0))
);


ALTER TABLE public.order_items OWNER TO botzone;

--
-- Name: order_tender_allocations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.order_tender_allocations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    receipt_id uuid NOT NULL,
    amount_vnd bigint NOT NULL,
    CONSTRAINT order_tender_allocations_amount_vnd_check CHECK ((amount_vnd > 0))
);


ALTER TABLE public.order_tender_allocations OWNER TO botzone;

--
-- Name: orders; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_no text NOT NULL,
    customer_id text NOT NULL,
    responsible_staff_id text,
    order_status text NOT NULL,
    payment_method text NOT NULL,
    address_snapshot jsonb NOT NULL,
    total_vnd bigint NOT NULL,
    currency text DEFAULT 'VND'::text NOT NULL,
    exchange_rate_vnd numeric DEFAULT 25000 NOT NULL,
    policy_snapshot jsonb DEFAULT '{}'::jsonb NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    delivered_at timestamp with time zone,
    CONSTRAINT orders_currency_check CHECK ((currency = 'VND'::text)),
    CONSTRAINT orders_exchange_rate_vnd_check CHECK ((exchange_rate_vnd > (0)::numeric)),
    CONSTRAINT orders_order_status_check CHECK ((order_status = ANY (ARRAY['building'::text, 'pending'::text, 'confirmed'::text, 'handed_off'::text, 'completed'::text, 'cancelled'::text, 'expired'::text, 'reconciliation'::text]))),
    CONSTRAINT orders_payment_method_check CHECK ((payment_method = ANY (ARRAY['cod'::text, 'banking'::text, 'momo'::text, 'visa'::text]))),
    CONSTRAINT orders_total_vnd_check CHECK ((total_vnd >= 0))
);


ALTER TABLE public.orders OWNER TO botzone;

--
-- Name: outbox_events; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.outbox_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_key text NOT NULL,
    type text NOT NULL,
    payload jsonb NOT NULL,
    status text DEFAULT 'pending'::text NOT NULL,
    attempts integer DEFAULT 0 NOT NULL,
    next_attempt_at timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT outbox_events_status_check CHECK ((status = ANY (ARRAY['pending'::text, 'processing'::text, 'completed'::text, 'failed'::text])))
);


ALTER TABLE public.outbox_events OWNER TO botzone;

--
-- Name: payment_attempts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.payment_attempts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    method text NOT NULL,
    status text NOT NULL,
    requested_amount_vnd bigint NOT NULL,
    request_key text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT payment_attempts_method_check CHECK ((method = ANY (ARRAY['cod'::text, 'banking'::text, 'momo'::text, 'visa'::text]))),
    CONSTRAINT payment_attempts_requested_amount_vnd_check CHECK ((requested_amount_vnd > 0)),
    CONSTRAINT payment_attempts_status_check CHECK ((status = ANY (ARRAY['pending'::text, 'success'::text, 'failed'::text, 'cancelled'::text, 'expired'::text, 'reconciliation'::text])))
);


ALTER TABLE public.payment_attempts OWNER TO botzone;

--
-- Name: payment_events; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.payment_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    payment_attempt_id uuid NOT NULL,
    event_key text NOT NULL,
    event_type text NOT NULL,
    payload_digest text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.payment_events OWNER TO botzone;

--
-- Name: payment_receipts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.payment_receipts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    payment_attempt_id uuid,
    tender_kind text NOT NULL,
    amount_vnd bigint NOT NULL,
    received_at timestamp with time zone DEFAULT now() NOT NULL,
    source_event_key text NOT NULL,
    CONSTRAINT payment_receipts_amount_vnd_check CHECK ((amount_vnd > 0)),
    CONSTRAINT payment_receipts_tender_kind_check CHECK ((tender_kind = ANY (ARRAY['cash_demo'::text, 'store_credit'::text])))
);


ALTER TABLE public.payment_receipts OWNER TO botzone;

--
-- Name: product_attribute_values; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.product_attribute_values (
    product_id uuid NOT NULL,
    attribute_id uuid NOT NULL,
    value_text text,
    value_number numeric,
    value_boolean boolean,
    CONSTRAINT product_attribute_values_check CHECK ((num_nonnulls(value_text, value_number, value_boolean) = 1))
);


ALTER TABLE public.product_attribute_values OWNER TO botzone;

--
-- Name: product_compatibilities; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.product_compatibilities (
    accessory_product_id uuid NOT NULL,
    target_product_id uuid NOT NULL,
    CONSTRAINT product_compatibilities_check CHECK ((accessory_product_id <> target_product_id))
);


ALTER TABLE public.product_compatibilities OWNER TO botzone;

--
-- Name: product_media; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.product_media (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    product_id uuid NOT NULL,
    variant_id uuid,
    media_id uuid NOT NULL,
    "position" integer NOT NULL,
    role text NOT NULL
);


ALTER TABLE public.product_media OWNER TO botzone;

--
-- Name: product_translations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.product_translations (
    product_id uuid NOT NULL,
    locale text NOT NULL,
    name text NOT NULL,
    description text NOT NULL,
    seo_title text NOT NULL,
    content_status text DEFAULT 'published'::text NOT NULL,
    CONSTRAINT product_translations_content_status_check CHECK ((content_status = ANY (ARRAY['draft'::text, 'published'::text]))),
    CONSTRAINT product_translations_locale_check CHECK ((locale = ANY (ARRAY['vi'::text, 'en'::text])))
);


ALTER TABLE public.product_translations OWNER TO botzone;

--
-- Name: product_variants; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.product_variants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    product_id uuid NOT NULL,
    sku text NOT NULL,
    option_signature text NOT NULL,
    options jsonb DEFAULT '{}'::jsonb NOT NULL,
    price_vnd bigint NOT NULL,
    tracking_mode text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    standard_warranty_months integer DEFAULT 12 NOT NULL,
    care_fee_vnd bigint,
    CONSTRAINT product_variants_care_fee_vnd_check CHECK ((care_fee_vnd >= 0)),
    CONSTRAINT product_variants_price_vnd_check CHECK ((price_vnd >= 0)),
    CONSTRAINT product_variants_standard_warranty_months_check CHECK ((standard_warranty_months >= 0)),
    CONSTRAINT product_variants_tracking_mode_check CHECK ((tracking_mode = ANY (ARRAY['serialized'::text, 'quantity'::text])))
);


ALTER TABLE public.product_variants OWNER TO botzone;

--
-- Name: products; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.products (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    category_id uuid NOT NULL,
    slug text NOT NULL,
    model_code text NOT NULL,
    business_status text DEFAULT 'active'::text NOT NULL,
    generation text,
    source_url text NOT NULL,
    specs jsonb DEFAULT '{}'::jsonb NOT NULL,
    poster text DEFAULT '/media/phone-burgundy.svg'::text NOT NULL,
    CONSTRAINT products_business_status_check CHECK ((business_status = ANY (ARRAY['draft'::text, 'active'::text, 'discontinued'::text, 'archived'::text])))
);


ALTER TABLE public.products OWNER TO botzone;

--
-- Name: promotion_redemptions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.promotion_redemptions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    promotion_id uuid NOT NULL,
    promotion_version_id uuid NOT NULL,
    customer_id text NOT NULL,
    order_id uuid NOT NULL,
    status text NOT NULL,
    amount_vnd bigint NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    idempotency_key text NOT NULL,
    CONSTRAINT promotion_redemptions_amount_vnd_check CHECK ((amount_vnd >= 0)),
    CONSTRAINT promotion_redemptions_status_check CHECK ((status = ANY (ARRAY['reserved'::text, 'consumed'::text, 'released'::text])))
);


ALTER TABLE public.promotion_redemptions OWNER TO botzone;

--
-- Name: promotion_targets; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.promotion_targets (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    promotion_version_id uuid NOT NULL,
    variant_id uuid,
    product_id uuid,
    category_id uuid,
    CONSTRAINT promotion_targets_check CHECK ((num_nonnulls(variant_id, product_id, category_id) = 1))
);


ALTER TABLE public.promotion_targets OWNER TO botzone;

--
-- Name: promotion_versions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.promotion_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    promotion_id uuid NOT NULL,
    version_no integer NOT NULL,
    discount_type text NOT NULL,
    value bigint NOT NULL,
    max_discount_vnd bigint,
    min_purchase_vnd bigint DEFAULT 0 NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    ends_at timestamp with time zone NOT NULL,
    all_products boolean DEFAULT true NOT NULL,
    CONSTRAINT promotion_versions_check CHECK ((starts_at < ends_at)),
    CONSTRAINT promotion_versions_check1 CHECK (((discount_type <> 'percentage'::text) OR (value <= 100))),
    CONSTRAINT promotion_versions_discount_type_check CHECK ((discount_type = ANY (ARRAY['fixed'::text, 'percentage'::text]))),
    CONSTRAINT promotion_versions_max_discount_vnd_check CHECK ((max_discount_vnd >= 0)),
    CONSTRAINT promotion_versions_min_purchase_vnd_check CHECK ((min_purchase_vnd >= 0)),
    CONSTRAINT promotion_versions_value_check CHECK ((value > 0)),
    CONSTRAINT promotion_versions_version_no_check CHECK ((version_no > 0))
);


ALTER TABLE public.promotion_versions OWNER TO botzone;

--
-- Name: promotions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.promotions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code_normalized text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    max_uses integer,
    max_uses_per_customer integer,
    CONSTRAINT promotions_code_normalized_check CHECK ((code_normalized = upper(TRIM(BOTH FROM code_normalized)))),
    CONSTRAINT promotions_max_uses_check CHECK ((max_uses > 0)),
    CONSTRAINT promotions_max_uses_per_customer_check CHECK ((max_uses_per_customer > 0))
);


ALTER TABLE public.promotions OWNER TO botzone;

--
-- Name: purchase_order_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.purchase_order_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    purchase_order_id uuid NOT NULL,
    variant_id uuid NOT NULL,
    quantity integer NOT NULL,
    unit_cost_vnd bigint NOT NULL,
    CONSTRAINT purchase_order_items_quantity_check CHECK ((quantity > 0)),
    CONSTRAINT purchase_order_items_unit_cost_vnd_check CHECK ((unit_cost_vnd >= 0))
);


ALTER TABLE public.purchase_order_items OWNER TO botzone;

--
-- Name: purchase_orders; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.purchase_orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    supplier_id uuid NOT NULL,
    code text NOT NULL,
    status text NOT NULL,
    created_by text NOT NULL,
    approved_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT purchase_orders_status_check CHECK ((status = ANY (ARRAY['draft'::text, 'approved'::text, 'part_received'::text, 'received'::text, 'cancelled'::text])))
);


ALTER TABLE public.purchase_orders OWNER TO botzone;

--
-- Name: refund_allocations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.refund_allocations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    refund_id uuid NOT NULL,
    payment_receipt_id uuid NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    amount_vnd bigint NOT NULL,
    CONSTRAINT refund_allocations_amount_vnd_check CHECK ((amount_vnd > 0))
);


ALTER TABLE public.refund_allocations OWNER TO botzone;

--
-- Name: refunds; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.refunds (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    credit_note_id uuid,
    status text NOT NULL,
    amount_vnd bigint NOT NULL,
    approved_by text NOT NULL,
    completed_at timestamp with time zone,
    idempotency_key text NOT NULL,
    CONSTRAINT refunds_amount_vnd_check CHECK ((amount_vnd > 0)),
    CONSTRAINT refunds_check CHECK (((status <> 'completed'::text) OR (completed_at IS NOT NULL))),
    CONSTRAINT refunds_status_check CHECK ((status = ANY (ARRAY['approved'::text, 'pending'::text, 'completed'::text, 'failed'::text])))
);


ALTER TABLE public.refunds OWNER TO botzone;

--
-- Name: return_evidence; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.return_evidence (
    return_request_id uuid NOT NULL,
    media_id uuid NOT NULL
);


ALTER TABLE public.return_evidence OWNER TO botzone;

--
-- Name: return_item_units; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.return_item_units (
    return_item_id uuid NOT NULL,
    order_item_id uuid NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    inventory_unit_id uuid
);


ALTER TABLE public.return_item_units OWNER TO botzone;

--
-- Name: return_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.return_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    return_request_id uuid NOT NULL,
    order_id uuid NOT NULL,
    order_item_id uuid NOT NULL,
    qty integer NOT NULL,
    actual_received_identifier text,
    CONSTRAINT return_items_qty_check CHECK ((qty > 0))
);


ALTER TABLE public.return_items OWNER TO botzone;

--
-- Name: return_requests; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.return_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    customer_id text NOT NULL,
    state text NOT NULL,
    reason text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    inspection_result text,
    inspected_by text,
    inspected_at timestamp with time zone,
    fee_vnd bigint DEFAULT 30000 NOT NULL,
    CONSTRAINT return_requests_fee_vnd_check CHECK ((fee_vnd >= 0)),
    CONSTRAINT return_requests_reason_check CHECK ((reason = ANY (ARRAY['defective'::text, 'wrong_item'::text]))),
    CONSTRAINT return_requests_state_check CHECK ((state = ANY (ARRAY['requested'::text, 'approved'::text, 'received'::text, 'inspected'::text, 'rejected'::text, 'credited'::text, 'closed'::text])))
);


ALTER TABLE public.return_requests OWNER TO botzone;

--
-- Name: schema_migrations; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.schema_migrations (
    name text NOT NULL,
    checksum text NOT NULL,
    applied_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.schema_migrations OWNER TO botzone;

--
-- Name: session; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.session (
    id text NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    token text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    ip_address text,
    user_agent text,
    user_id text NOT NULL
);


ALTER TABLE public.session OWNER TO botzone;

--
-- Name: setting_versions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.setting_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    key text NOT NULL,
    version_no integer NOT NULL,
    value jsonb NOT NULL,
    changed_by text,
    changed_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.setting_versions OWNER TO botzone;

--
-- Name: shipment_events; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.shipment_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    shipment_id uuid NOT NULL,
    status text NOT NULL,
    occurred_at timestamp with time zone DEFAULT now() NOT NULL,
    event_key text NOT NULL
);


ALTER TABLE public.shipment_events OWNER TO botzone;

--
-- Name: shipments; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.shipments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    tracking_no text NOT NULL,
    status text NOT NULL,
    retry_of uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT shipments_status_check CHECK ((status = ANY (ARRAY['handed_off'::text, 'in_transit'::text, 'delivered'::text, 'failed'::text, 'returned'::text, 'closed'::text])))
);


ALTER TABLE public.shipments OWNER TO botzone;

--
-- Name: staff_profiles; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.staff_profiles (
    user_id text NOT NULL,
    staff_code text NOT NULL,
    role text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    CONSTRAINT staff_profiles_role_check CHECK ((role = ANY (ARRAY['owner'::text, 'order_staff'::text, 'warehouse_staff'::text, 'content_staff'::text])))
);


ALTER TABLE public.staff_profiles OWNER TO botzone;

--
-- Name: stock_adjustments; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.stock_adjustments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    lot_id uuid NOT NULL,
    quantity_delta integer NOT NULL,
    reason text NOT NULL,
    requested_by text NOT NULL,
    approved_by text,
    status text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT stock_adjustments_quantity_delta_check CHECK ((quantity_delta <> 0)),
    CONSTRAINT stock_adjustments_status_check CHECK ((status = ANY (ARRAY['requested'::text, 'approved'::text, 'rejected'::text])))
);


ALTER TABLE public.stock_adjustments OWNER TO botzone;

--
-- Name: stock_movements; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.stock_movements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    variant_id uuid NOT NULL,
    lot_id uuid NOT NULL,
    unit_id uuid,
    quantity_delta integer NOT NULL,
    kind text NOT NULL,
    source_event_key text NOT NULL,
    actor_user_id text,
    reason text DEFAULT ''::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT stock_movements_kind_check CHECK ((kind = ANY (ARRAY['receive'::text, 'reserve'::text, 'release'::text, 'ship'::text, 'return'::text, 'adjust'::text, 'replacement'::text])))
);


ALTER TABLE public.stock_movements OWNER TO botzone;

--
-- Name: store_credit_accounts; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.store_credit_accounts (
    user_id text NOT NULL,
    balance_vnd bigint DEFAULT 0 NOT NULL,
    CONSTRAINT store_credit_accounts_balance_vnd_check CHECK ((balance_vnd >= 0))
);


ALTER TABLE public.store_credit_accounts OWNER TO botzone;

--
-- Name: store_credit_entries; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.store_credit_entries (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    user_id text NOT NULL,
    delta_vnd bigint NOT NULL,
    kind text NOT NULL,
    trade_in_item_id uuid,
    quote_item_id uuid,
    order_id uuid,
    refund_id uuid,
    event_key text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT store_credit_entries_check CHECK ((((kind = 'trade_in'::text) AND (trade_in_item_id IS NOT NULL) AND (quote_item_id IS NOT NULL) AND (order_id IS NULL) AND (refund_id IS NULL) AND (delta_vnd > 0)) OR ((kind = 'spend'::text) AND (order_id IS NOT NULL) AND (trade_in_item_id IS NULL) AND (refund_id IS NULL) AND (delta_vnd < 0)) OR ((kind = 'refund'::text) AND (refund_id IS NOT NULL) AND (order_id IS NOT NULL) AND (trade_in_item_id IS NULL) AND (delta_vnd > 0)))),
    CONSTRAINT store_credit_entries_delta_vnd_check CHECK ((delta_vnd <> 0)),
    CONSTRAINT store_credit_entries_kind_check CHECK ((kind = ANY (ARRAY['trade_in'::text, 'spend'::text, 'refund'::text])))
);


ALTER TABLE public.store_credit_entries OWNER TO botzone;

--
-- Name: suppliers; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.suppliers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code text NOT NULL,
    name text NOT NULL,
    contact text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


ALTER TABLE public.suppliers OWNER TO botzone;

--
-- Name: trade_in_evidence; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_evidence (
    inspection_id uuid NOT NULL,
    media_id uuid NOT NULL
);


ALTER TABLE public.trade_in_evidence OWNER TO botzone;

--
-- Name: trade_in_inspections; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_inspections (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    item_id uuid NOT NULL,
    inspector_staff_id text NOT NULL,
    checklist_version integer NOT NULL,
    assessed_condition jsonb NOT NULL,
    inspected_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.trade_in_inspections OWNER TO botzone;

--
-- Name: trade_in_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    product_id uuid NOT NULL,
    variant_id uuid,
    qty integer NOT NULL,
    identifier text,
    internal_code text NOT NULL,
    condition_snapshot jsonb NOT NULL,
    state text NOT NULL,
    inventory_unit_id uuid,
    CONSTRAINT trade_in_items_qty_check CHECK ((qty > 0)),
    CONSTRAINT trade_in_items_state_check CHECK ((state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'quoted'::text, 'accepted'::text, 'rejected'::text, 'completed'::text, 'returned'::text, 'exception'::text])))
);


ALTER TABLE public.trade_in_items OWNER TO botzone;

--
-- Name: trade_in_quote_items; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_quote_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    quote_id uuid NOT NULL,
    request_id uuid NOT NULL,
    item_id uuid NOT NULL,
    offered_vnd bigint NOT NULL,
    decision text NOT NULL,
    decided_at timestamp with time zone,
    CONSTRAINT trade_in_quote_items_decision_check CHECK ((decision = ANY (ARRAY['pending'::text, 'accepted'::text, 'rejected'::text]))),
    CONSTRAINT trade_in_quote_items_offered_vnd_check CHECK ((offered_vnd > 0))
);


ALTER TABLE public.trade_in_quote_items OWNER TO botzone;

--
-- Name: trade_in_quotes; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_quotes (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    version_no integer NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    quoted_by text NOT NULL,
    approved_by text
);


ALTER TABLE public.trade_in_quotes OWNER TO botzone;

--
-- Name: trade_in_requests; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    customer_id text NOT NULL,
    reception_method text NOT NULL,
    state text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT trade_in_requests_reception_method_check CHECK ((reception_method = ANY (ARRAY['store'::text, 'remote'::text]))),
    CONSTRAINT trade_in_requests_state_check CHECK ((state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'quoted'::text, 'contact_pending'::text, 'completed'::text, 'returned'::text, 'exception'::text])))
);


ALTER TABLE public.trade_in_requests OWNER TO botzone;

--
-- Name: trade_in_shipments; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.trade_in_shipments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    direction text NOT NULL,
    tracking_no text NOT NULL,
    fee_vnd bigint NOT NULL,
    state text NOT NULL,
    CONSTRAINT trade_in_shipments_direction_check CHECK ((direction = ANY (ARRAY['inbound'::text, 'outbound'::text]))),
    CONSTRAINT trade_in_shipments_fee_vnd_check CHECK ((fee_vnd >= 0)),
    CONSTRAINT trade_in_shipments_state_check CHECK ((state = ANY (ARRAY['created'::text, 'in_transit'::text, 'delivered'::text, 'exception'::text])))
);


ALTER TABLE public.trade_in_shipments OWNER TO botzone;

--
-- Name: user; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public."user" (
    id text NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    email_verified boolean DEFAULT false NOT NULL,
    image text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."user" OWNER TO botzone;

--
-- Name: variant_attribute_values; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.variant_attribute_values (
    variant_id uuid NOT NULL,
    attribute_id uuid NOT NULL,
    value_text text,
    value_number numeric,
    value_boolean boolean,
    CONSTRAINT variant_attribute_values_check CHECK ((num_nonnulls(value_text, value_number, value_boolean) = 1))
);


ALTER TABLE public.variant_attribute_values OWNER TO botzone;

--
-- Name: variant_stock; Type: VIEW; Schema: public; Owner: botzone
--

CREATE VIEW public.variant_stock AS
 SELECT id AS variant_id,
        CASE
            WHEN (tracking_mode = 'serialized'::text) THEN ( SELECT count(*) AS count
               FROM (public.inventory_units u
                 JOIN public.inventory_lots l ON ((l.id = u.lot_id)))
              WHERE ((l.variant_id = v.id) AND (u.custody_status = 'available'::text) AND (u.condition_status = 'new'::text)))
            ELSE COALESCE(( SELECT sum((l.qty_on_hand - l.qty_reserved)) AS sum
               FROM public.inventory_lots l
              WHERE (l.variant_id = v.id)), (0)::bigint)
        END AS available
   FROM public.product_variants v;


ALTER VIEW public.variant_stock OWNER TO botzone;

--
-- Name: verification; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.verification (
    id text NOT NULL,
    identifier text NOT NULL,
    value text NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.verification OWNER TO botzone;

--
-- Name: warehouses; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warehouses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code text NOT NULL,
    name text NOT NULL,
    CONSTRAINT warehouses_code_check CHECK ((code = 'MAIN'::text))
);


ALTER TABLE public.warehouses OWNER TO botzone;

--
-- Name: warranty_claims; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_claims (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    entitlement_id uuid NOT NULL,
    state text NOT NULL,
    description text NOT NULL,
    damage_kind text NOT NULL,
    inspection_result text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT warranty_claims_damage_kind_check CHECK ((damage_kind = ANY (ARRAY['hardware'::text, 'accidental'::text, 'liquid'::text]))),
    CONSTRAINT warranty_claims_state_check CHECK ((state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'repairing'::text, 'repaired'::text, 'replace'::text, 'replaced'::text, 'refund'::text, 'refunded'::text, 'rejected'::text, 'closed'::text])))
);


ALTER TABLE public.warranty_claims OWNER TO botzone;

--
-- Name: warranty_entitlements; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_entitlements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_item_unit_amount_id uuid NOT NULL,
    plan_version_id uuid NOT NULL,
    inventory_unit_id uuid,
    starts_on date,
    ends_on date,
    state text NOT NULL,
    used boolean DEFAULT false NOT NULL,
    CONSTRAINT warranty_entitlements_check CHECK (((ends_on IS NULL) OR (starts_on < ends_on))),
    CONSTRAINT warranty_entitlements_state_check CHECK ((state = ANY (ARRAY['pending'::text, 'active'::text, 'cancelled'::text, 'expired'::text, 'refunded'::text])))
);


ALTER TABLE public.warranty_entitlements OWNER TO botzone;

--
-- Name: warranty_events; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    claim_id uuid NOT NULL,
    actor_id text NOT NULL,
    event text NOT NULL,
    note text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.warranty_events OWNER TO botzone;

--
-- Name: warranty_evidence; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_evidence (
    claim_id uuid NOT NULL,
    media_id uuid NOT NULL
);


ALTER TABLE public.warranty_evidence OWNER TO botzone;

--
-- Name: warranty_plan_versions; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_plan_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    plan_id uuid NOT NULL,
    version_no integer NOT NULL,
    term_months integer NOT NULL,
    fee_vnd bigint NOT NULL,
    policy_snapshot jsonb NOT NULL,
    CONSTRAINT warranty_plan_versions_fee_vnd_check CHECK ((fee_vnd >= 0)),
    CONSTRAINT warranty_plan_versions_term_months_check CHECK ((term_months = 12))
);


ALTER TABLE public.warranty_plan_versions OWNER TO botzone;

--
-- Name: warranty_plans; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_plans (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code text NOT NULL,
    active boolean DEFAULT true NOT NULL
);


ALTER TABLE public.warranty_plans OWNER TO botzone;

--
-- Name: warranty_replacements; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.warranty_replacements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    claim_id uuid NOT NULL,
    old_unit_id uuid NOT NULL,
    new_unit_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT warranty_replacements_check CHECK ((old_unit_id <> new_unit_id))
);


ALTER TABLE public.warranty_replacements OWNER TO botzone;

--
-- Name: wishlists; Type: TABLE; Schema: public; Owner: botzone
--

CREATE TABLE public.wishlists (
    user_id text NOT NULL,
    product_id uuid NOT NULL
);


ALTER TABLE public.wishlists OWNER TO botzone;

--
-- Name: account account_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.account
    ADD CONSTRAINT account_pkey PRIMARY KEY (id);


--
-- Name: account account_provider_id_account_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.account
    ADD CONSTRAINT account_provider_id_account_id_key UNIQUE (provider_id, account_id);


--
-- Name: addresses addresses_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.addresses
    ADD CONSTRAINT addresses_pkey PRIMARY KEY (id);


--
-- Name: app_settings app_settings_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.app_settings
    ADD CONSTRAINT app_settings_pkey PRIMARY KEY (key);


--
-- Name: attribute_definitions attribute_definitions_category_id_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.attribute_definitions
    ADD CONSTRAINT attribute_definitions_category_id_code_key UNIQUE (category_id, code);


--
-- Name: attribute_definitions attribute_definitions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.attribute_definitions
    ADD CONSTRAINT attribute_definitions_pkey PRIMARY KEY (id);


--
-- Name: audit_events audit_events_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.audit_events
    ADD CONSTRAINT audit_events_pkey PRIMARY KEY (id);


--
-- Name: benefit_holds benefit_holds_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.benefit_holds
    ADD CONSTRAINT benefit_holds_pkey PRIMARY KEY (id);


--
-- Name: cart_items cart_items_cart_id_variant_id_care_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_cart_id_variant_id_care_key UNIQUE (cart_id, variant_id, care);


--
-- Name: cart_items cart_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_pkey PRIMARY KEY (id);


--
-- Name: carts carts_customer_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.carts
    ADD CONSTRAINT carts_customer_id_key UNIQUE (customer_id);


--
-- Name: carts carts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.carts
    ADD CONSTRAINT carts_pkey PRIMARY KEY (id);


--
-- Name: categories categories_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_code_key UNIQUE (code);


--
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (id);


--
-- Name: categories categories_slug_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_slug_key UNIQUE (slug);


--
-- Name: credit_note_item_units credit_note_item_units_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_item_units
    ADD CONSTRAINT credit_note_item_units_pkey PRIMARY KEY (credit_note_item_id, order_item_unit_amount_id);


--
-- Name: credit_note_items credit_note_items_id_invoice_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_items
    ADD CONSTRAINT credit_note_items_id_invoice_item_id_key UNIQUE (id, invoice_item_id);


--
-- Name: credit_note_items credit_note_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_items
    ADD CONSTRAINT credit_note_items_pkey PRIMARY KEY (id);


--
-- Name: credit_notes credit_notes_credit_note_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_credit_note_no_key UNIQUE (credit_note_no);


--
-- Name: credit_notes credit_notes_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_event_key_key UNIQUE (event_key);


--
-- Name: credit_notes credit_notes_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_id_order_id_key UNIQUE (id, order_id);


--
-- Name: credit_notes credit_notes_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_pkey PRIMARY KEY (id);


--
-- Name: customer_profiles customer_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.customer_profiles
    ADD CONSTRAINT customer_profiles_pkey PRIMARY KEY (user_id);


--
-- Name: goods_receipt_items goods_receipt_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipt_items
    ADD CONSTRAINT goods_receipt_items_pkey PRIMARY KEY (id);


--
-- Name: goods_receipts goods_receipts_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_event_key_key UNIQUE (event_key);


--
-- Name: goods_receipts goods_receipts_id_purchase_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_id_purchase_order_id_key UNIQUE (id, purchase_order_id);


--
-- Name: goods_receipts goods_receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_pkey PRIMARY KEY (id);


--
-- Name: goods_receipts goods_receipts_receipt_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_receipt_no_key UNIQUE (receipt_no);


--
-- Name: idempotency_records idempotency_records_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.idempotency_records
    ADD CONSTRAINT idempotency_records_pkey PRIMARY KEY (scope, key);


--
-- Name: inventory_allocations inventory_allocations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_allocations
    ADD CONSTRAINT inventory_allocations_pkey PRIMARY KEY (id);


--
-- Name: inventory_identifiers inventory_identifiers_kind_normalized_value_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_identifiers
    ADD CONSTRAINT inventory_identifiers_kind_normalized_value_key UNIQUE (kind, normalized_value);


--
-- Name: inventory_identifiers inventory_identifiers_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_identifiers
    ADD CONSTRAINT inventory_identifiers_pkey PRIMARY KEY (id);


--
-- Name: inventory_lots inventory_lots_id_variant_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_id_variant_id_key UNIQUE (id, variant_id);


--
-- Name: inventory_lots inventory_lots_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_pkey PRIMARY KEY (id);


--
-- Name: inventory_lots inventory_lots_receipt_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_receipt_item_id_key UNIQUE (receipt_item_id);


--
-- Name: inventory_reservations inventory_reservations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_reservations
    ADD CONSTRAINT inventory_reservations_pkey PRIMARY KEY (id);


--
-- Name: inventory_units inventory_units_id_lot_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_units
    ADD CONSTRAINT inventory_units_id_lot_id_key UNIQUE (id, lot_id);


--
-- Name: inventory_units inventory_units_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_units
    ADD CONSTRAINT inventory_units_pkey PRIMARY KEY (id);


--
-- Name: invoice_exports invoice_exports_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_exports
    ADD CONSTRAINT invoice_exports_pkey PRIMARY KEY (id);


--
-- Name: invoice_items invoice_items_id_invoice_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_id_invoice_id_key UNIQUE (id, invoice_id);


--
-- Name: invoice_items invoice_items_invoice_id_line_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_invoice_id_line_no_key UNIQUE (invoice_id, line_no);


--
-- Name: invoice_items invoice_items_order_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_order_item_id_key UNIQUE (order_item_id);


--
-- Name: invoice_items invoice_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_pkey PRIMARY KEY (id);


--
-- Name: invoices invoices_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_id_order_id_key UNIQUE (id, order_id);


--
-- Name: invoices invoices_invoice_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_invoice_no_key UNIQUE (invoice_no);


--
-- Name: invoices invoices_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_order_id_key UNIQUE (order_id);


--
-- Name: invoices invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_pkey PRIMARY KEY (id);


--
-- Name: landing_drafts landing_drafts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_drafts
    ADD CONSTRAINT landing_drafts_pkey PRIMARY KEY (page_id);


--
-- Name: landing_media_refs landing_media_refs_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_media_refs
    ADD CONSTRAINT landing_media_refs_pkey PRIMARY KEY (landing_version_id, media_id);


--
-- Name: landing_pages landing_pages_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_pages
    ADD CONSTRAINT landing_pages_pkey PRIMARY KEY (id);


--
-- Name: landing_pages landing_pages_slug_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_pages
    ADD CONSTRAINT landing_pages_slug_key UNIQUE (slug);


--
-- Name: landing_publications landing_publications_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_publications
    ADD CONSTRAINT landing_publications_pkey PRIMARY KEY (id);


--
-- Name: landing_versions landing_versions_id_page_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_versions
    ADD CONSTRAINT landing_versions_id_page_id_key UNIQUE (id, page_id);


--
-- Name: landing_versions landing_versions_page_id_version_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_versions
    ADD CONSTRAINT landing_versions_page_id_version_no_key UNIQUE (page_id, version_no);


--
-- Name: landing_versions landing_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_versions
    ADD CONSTRAINT landing_versions_pkey PRIMARY KEY (id);


--
-- Name: loyalty_accounts loyalty_accounts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_accounts
    ADD CONSTRAINT loyalty_accounts_pkey PRIMARY KEY (user_id);


--
-- Name: loyalty_entries loyalty_entries_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_entries
    ADD CONSTRAINT loyalty_entries_event_key_key UNIQUE (event_key);


--
-- Name: loyalty_entries loyalty_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_entries
    ADD CONSTRAINT loyalty_entries_pkey PRIMARY KEY (id);


--
-- Name: media_assets media_assets_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_pkey PRIMARY KEY (id);


--
-- Name: media_assets media_assets_storage_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_storage_key_key UNIQUE (storage_key);


--
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (id);


--
-- Name: order_adjustments order_adjustments_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_adjustments
    ADD CONSTRAINT order_adjustments_pkey PRIMARY KEY (id);


--
-- Name: order_adjustments order_adjustments_source_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_adjustments
    ADD CONSTRAINT order_adjustments_source_event_key_key UNIQUE (source_event_key);


--
-- Name: order_item_unit_amounts order_item_unit_amounts_id_order_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_item_unit_amounts
    ADD CONSTRAINT order_item_unit_amounts_id_order_item_id_key UNIQUE (id, order_item_id);


--
-- Name: order_item_unit_amounts order_item_unit_amounts_order_item_id_unit_ordinal_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_item_unit_amounts
    ADD CONSTRAINT order_item_unit_amounts_order_item_id_unit_ordinal_key UNIQUE (order_item_id, unit_ordinal);


--
-- Name: order_item_unit_amounts order_item_unit_amounts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_item_unit_amounts
    ADD CONSTRAINT order_item_unit_amounts_pkey PRIMARY KEY (id);


--
-- Name: order_items order_items_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_id_order_id_key UNIQUE (id, order_id);


--
-- Name: order_items order_items_order_id_line_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_order_id_line_no_key UNIQUE (order_id, line_no);


--
-- Name: order_items order_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_pkey PRIMARY KEY (id);


--
-- Name: order_tender_allocations order_tender_allocations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_tender_allocations
    ADD CONSTRAINT order_tender_allocations_pkey PRIMARY KEY (id);


--
-- Name: order_tender_allocations order_tender_allocations_receipt_id_order_item_unit_amount__key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_tender_allocations
    ADD CONSTRAINT order_tender_allocations_receipt_id_order_item_unit_amount__key UNIQUE (receipt_id, order_item_unit_amount_id);


--
-- Name: orders orders_id_customer_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_id_customer_id_key UNIQUE (id, customer_id);


--
-- Name: orders orders_order_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_order_no_key UNIQUE (order_no);


--
-- Name: orders orders_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_pkey PRIMARY KEY (id);


--
-- Name: outbox_events outbox_events_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.outbox_events
    ADD CONSTRAINT outbox_events_event_key_key UNIQUE (event_key);


--
-- Name: outbox_events outbox_events_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.outbox_events
    ADD CONSTRAINT outbox_events_pkey PRIMARY KEY (id);


--
-- Name: payment_attempts payment_attempts_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_attempts
    ADD CONSTRAINT payment_attempts_id_order_id_key UNIQUE (id, order_id);


--
-- Name: payment_attempts payment_attempts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_attempts
    ADD CONSTRAINT payment_attempts_pkey PRIMARY KEY (id);


--
-- Name: payment_attempts payment_attempts_request_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_attempts
    ADD CONSTRAINT payment_attempts_request_key_key UNIQUE (request_key);


--
-- Name: payment_events payment_events_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_events
    ADD CONSTRAINT payment_events_event_key_key UNIQUE (event_key);


--
-- Name: payment_events payment_events_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_events
    ADD CONSTRAINT payment_events_pkey PRIMARY KEY (id);


--
-- Name: payment_receipts payment_receipts_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_receipts
    ADD CONSTRAINT payment_receipts_id_order_id_key UNIQUE (id, order_id);


--
-- Name: payment_receipts payment_receipts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_receipts
    ADD CONSTRAINT payment_receipts_pkey PRIMARY KEY (id);


--
-- Name: payment_receipts payment_receipts_source_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_receipts
    ADD CONSTRAINT payment_receipts_source_event_key_key UNIQUE (source_event_key);


--
-- Name: product_attribute_values product_attribute_values_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_attribute_values
    ADD CONSTRAINT product_attribute_values_pkey PRIMARY KEY (product_id, attribute_id);


--
-- Name: product_compatibilities product_compatibilities_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_compatibilities
    ADD CONSTRAINT product_compatibilities_pkey PRIMARY KEY (accessory_product_id, target_product_id);


--
-- Name: product_media product_media_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_media
    ADD CONSTRAINT product_media_pkey PRIMARY KEY (id);


--
-- Name: product_translations product_translations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_translations
    ADD CONSTRAINT product_translations_pkey PRIMARY KEY (product_id, locale);


--
-- Name: product_variants product_variants_id_product_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_variants
    ADD CONSTRAINT product_variants_id_product_id_key UNIQUE (id, product_id);


--
-- Name: product_variants product_variants_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_variants
    ADD CONSTRAINT product_variants_pkey PRIMARY KEY (id);


--
-- Name: product_variants product_variants_product_id_option_signature_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_variants
    ADD CONSTRAINT product_variants_product_id_option_signature_key UNIQUE (product_id, option_signature);


--
-- Name: product_variants product_variants_sku_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_variants
    ADD CONSTRAINT product_variants_sku_key UNIQUE (sku);


--
-- Name: products products_model_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_model_code_key UNIQUE (model_code);


--
-- Name: products products_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (id);


--
-- Name: products products_slug_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_slug_key UNIQUE (slug);


--
-- Name: promotion_redemptions promotion_redemptions_idempotency_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_idempotency_key_key UNIQUE (idempotency_key);


--
-- Name: promotion_redemptions promotion_redemptions_order_id_promotion_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_order_id_promotion_id_key UNIQUE (order_id, promotion_id);


--
-- Name: promotion_redemptions promotion_redemptions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_pkey PRIMARY KEY (id);


--
-- Name: promotion_targets promotion_targets_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_targets
    ADD CONSTRAINT promotion_targets_pkey PRIMARY KEY (id);


--
-- Name: promotion_versions promotion_versions_id_promotion_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_versions
    ADD CONSTRAINT promotion_versions_id_promotion_id_key UNIQUE (id, promotion_id);


--
-- Name: promotion_versions promotion_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_versions
    ADD CONSTRAINT promotion_versions_pkey PRIMARY KEY (id);


--
-- Name: promotion_versions promotion_versions_promotion_id_version_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_versions
    ADD CONSTRAINT promotion_versions_promotion_id_version_no_key UNIQUE (promotion_id, version_no);


--
-- Name: promotions promotions_code_normalized_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT promotions_code_normalized_key UNIQUE (code_normalized);


--
-- Name: promotions promotions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT promotions_pkey PRIMARY KEY (id);


--
-- Name: purchase_order_items purchase_order_items_id_purchase_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_order_items
    ADD CONSTRAINT purchase_order_items_id_purchase_order_id_key UNIQUE (id, purchase_order_id);


--
-- Name: purchase_order_items purchase_order_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_order_items
    ADD CONSTRAINT purchase_order_items_pkey PRIMARY KEY (id);


--
-- Name: purchase_orders purchase_orders_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_orders
    ADD CONSTRAINT purchase_orders_code_key UNIQUE (code);


--
-- Name: purchase_orders purchase_orders_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_orders
    ADD CONSTRAINT purchase_orders_pkey PRIMARY KEY (id);


--
-- Name: refund_allocations refund_allocations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refund_allocations
    ADD CONSTRAINT refund_allocations_pkey PRIMARY KEY (id);


--
-- Name: refund_allocations refund_allocations_refund_id_payment_receipt_id_order_item__key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refund_allocations
    ADD CONSTRAINT refund_allocations_refund_id_payment_receipt_id_order_item__key UNIQUE (refund_id, payment_receipt_id, order_item_unit_amount_id);


--
-- Name: refunds refunds_idempotency_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refunds
    ADD CONSTRAINT refunds_idempotency_key_key UNIQUE (idempotency_key);


--
-- Name: refunds refunds_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refunds
    ADD CONSTRAINT refunds_pkey PRIMARY KEY (id);


--
-- Name: return_evidence return_evidence_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_evidence
    ADD CONSTRAINT return_evidence_pkey PRIMARY KEY (return_request_id, media_id);


--
-- Name: return_item_units return_item_units_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_item_units
    ADD CONSTRAINT return_item_units_pkey PRIMARY KEY (return_item_id, order_item_unit_amount_id);


--
-- Name: return_items return_items_id_order_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_items
    ADD CONSTRAINT return_items_id_order_item_id_key UNIQUE (id, order_item_id);


--
-- Name: return_items return_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_items
    ADD CONSTRAINT return_items_pkey PRIMARY KEY (id);


--
-- Name: return_requests return_requests_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_requests
    ADD CONSTRAINT return_requests_id_order_id_key UNIQUE (id, order_id);


--
-- Name: return_requests return_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_requests
    ADD CONSTRAINT return_requests_pkey PRIMARY KEY (id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (name);


--
-- Name: session session_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.session
    ADD CONSTRAINT session_pkey PRIMARY KEY (id);


--
-- Name: session session_token_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.session
    ADD CONSTRAINT session_token_key UNIQUE (token);


--
-- Name: setting_versions setting_versions_key_version_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.setting_versions
    ADD CONSTRAINT setting_versions_key_version_no_key UNIQUE (key, version_no);


--
-- Name: setting_versions setting_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.setting_versions
    ADD CONSTRAINT setting_versions_pkey PRIMARY KEY (id);


--
-- Name: shipment_events shipment_events_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipment_events
    ADD CONSTRAINT shipment_events_event_key_key UNIQUE (event_key);


--
-- Name: shipment_events shipment_events_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipment_events
    ADD CONSTRAINT shipment_events_pkey PRIMARY KEY (id);


--
-- Name: shipments shipments_id_order_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipments
    ADD CONSTRAINT shipments_id_order_id_key UNIQUE (id, order_id);


--
-- Name: shipments shipments_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipments
    ADD CONSTRAINT shipments_pkey PRIMARY KEY (id);


--
-- Name: shipments shipments_tracking_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipments
    ADD CONSTRAINT shipments_tracking_no_key UNIQUE (tracking_no);


--
-- Name: staff_profiles staff_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.staff_profiles
    ADD CONSTRAINT staff_profiles_pkey PRIMARY KEY (user_id);


--
-- Name: staff_profiles staff_profiles_staff_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.staff_profiles
    ADD CONSTRAINT staff_profiles_staff_code_key UNIQUE (staff_code);


--
-- Name: stock_adjustments stock_adjustments_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_pkey PRIMARY KEY (id);


--
-- Name: stock_movements stock_movements_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_pkey PRIMARY KEY (id);


--
-- Name: stock_movements stock_movements_source_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_source_event_key_key UNIQUE (source_event_key);


--
-- Name: store_credit_accounts store_credit_accounts_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_accounts
    ADD CONSTRAINT store_credit_accounts_pkey PRIMARY KEY (user_id);


--
-- Name: store_credit_entries store_credit_entries_event_key_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_event_key_key UNIQUE (event_key);


--
-- Name: store_credit_entries store_credit_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_pkey PRIMARY KEY (id);


--
-- Name: suppliers suppliers_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.suppliers
    ADD CONSTRAINT suppliers_code_key UNIQUE (code);


--
-- Name: suppliers suppliers_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.suppliers
    ADD CONSTRAINT suppliers_pkey PRIMARY KEY (id);


--
-- Name: trade_in_evidence trade_in_evidence_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_evidence
    ADD CONSTRAINT trade_in_evidence_pkey PRIMARY KEY (inspection_id, media_id);


--
-- Name: trade_in_inspections trade_in_inspections_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_inspections
    ADD CONSTRAINT trade_in_inspections_pkey PRIMARY KEY (id);


--
-- Name: trade_in_items trade_in_items_id_request_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_id_request_id_key UNIQUE (id, request_id);


--
-- Name: trade_in_items trade_in_items_internal_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_internal_code_key UNIQUE (internal_code);


--
-- Name: trade_in_items trade_in_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_pkey PRIMARY KEY (id);


--
-- Name: trade_in_quote_items trade_in_quote_items_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quote_items
    ADD CONSTRAINT trade_in_quote_items_pkey PRIMARY KEY (id);


--
-- Name: trade_in_quote_items trade_in_quote_items_quote_id_item_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quote_items
    ADD CONSTRAINT trade_in_quote_items_quote_id_item_id_key UNIQUE (quote_id, item_id);


--
-- Name: trade_in_quotes trade_in_quotes_id_request_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_id_request_id_key UNIQUE (id, request_id);


--
-- Name: trade_in_quotes trade_in_quotes_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_pkey PRIMARY KEY (id);


--
-- Name: trade_in_quotes trade_in_quotes_request_id_version_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_request_id_version_no_key UNIQUE (request_id, version_no);


--
-- Name: trade_in_requests trade_in_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_requests
    ADD CONSTRAINT trade_in_requests_pkey PRIMARY KEY (id);


--
-- Name: trade_in_shipments trade_in_shipments_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_shipments
    ADD CONSTRAINT trade_in_shipments_pkey PRIMARY KEY (id);


--
-- Name: trade_in_shipments trade_in_shipments_tracking_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_shipments
    ADD CONSTRAINT trade_in_shipments_tracking_no_key UNIQUE (tracking_no);


--
-- Name: user user_email_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_email_key UNIQUE (email);


--
-- Name: user user_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_pkey PRIMARY KEY (id);


--
-- Name: variant_attribute_values variant_attribute_values_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.variant_attribute_values
    ADD CONSTRAINT variant_attribute_values_pkey PRIMARY KEY (variant_id, attribute_id);


--
-- Name: verification verification_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.verification
    ADD CONSTRAINT verification_pkey PRIMARY KEY (id);


--
-- Name: warehouses warehouses_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warehouses
    ADD CONSTRAINT warehouses_code_key UNIQUE (code);


--
-- Name: warehouses warehouses_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warehouses
    ADD CONSTRAINT warehouses_pkey PRIMARY KEY (id);


--
-- Name: warranty_claims warranty_claims_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_claims
    ADD CONSTRAINT warranty_claims_pkey PRIMARY KEY (id);


--
-- Name: warranty_entitlements warranty_entitlements_order_item_unit_amount_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_entitlements
    ADD CONSTRAINT warranty_entitlements_order_item_unit_amount_id_key UNIQUE (order_item_unit_amount_id);


--
-- Name: warranty_entitlements warranty_entitlements_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_entitlements
    ADD CONSTRAINT warranty_entitlements_pkey PRIMARY KEY (id);


--
-- Name: warranty_events warranty_events_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_events
    ADD CONSTRAINT warranty_events_pkey PRIMARY KEY (id);


--
-- Name: warranty_evidence warranty_evidence_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_evidence
    ADD CONSTRAINT warranty_evidence_pkey PRIMARY KEY (claim_id, media_id);


--
-- Name: warranty_plan_versions warranty_plan_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_plan_versions
    ADD CONSTRAINT warranty_plan_versions_pkey PRIMARY KEY (id);


--
-- Name: warranty_plan_versions warranty_plan_versions_plan_id_version_no_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_plan_versions
    ADD CONSTRAINT warranty_plan_versions_plan_id_version_no_key UNIQUE (plan_id, version_no);


--
-- Name: warranty_plans warranty_plans_code_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_plans
    ADD CONSTRAINT warranty_plans_code_key UNIQUE (code);


--
-- Name: warranty_plans warranty_plans_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_plans
    ADD CONSTRAINT warranty_plans_pkey PRIMARY KEY (id);


--
-- Name: warranty_replacements warranty_replacements_claim_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_claim_id_key UNIQUE (claim_id);


--
-- Name: warranty_replacements warranty_replacements_new_unit_id_key; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_new_unit_id_key UNIQUE (new_unit_id);


--
-- Name: warranty_replacements warranty_replacements_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_pkey PRIMARY KEY (id);


--
-- Name: wishlists wishlists_pkey; Type: CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.wishlists
    ADD CONSTRAINT wishlists_pkey PRIMARY KEY (user_id, product_id);


--
-- Name: address_default_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX address_default_idx ON public.addresses USING btree (user_id) WHERE is_default;


--
-- Name: allocation_unit_active_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX allocation_unit_active_idx ON public.inventory_allocations USING btree (order_item_unit_amount_id) WHERE (state = ANY (ARRAY['reserved'::text, 'shipped'::text]));


--
-- Name: benefit_hold_active_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX benefit_hold_active_idx ON public.benefit_holds USING btree (order_id, kind) WHERE (status = 'active'::text);


--
-- Name: credit_note_items_original_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX credit_note_items_original_idx ON public.credit_note_items USING btree (invoice_item_id);


--
-- Name: credit_notes_invoice_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX credit_notes_invoice_idx ON public.credit_notes USING btree (invoice_id, issued_at);


--
-- Name: inventory_unit_active_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX inventory_unit_active_idx ON public.inventory_allocations USING btree (inventory_unit_id) WHERE ((inventory_unit_id IS NOT NULL) AND (state = ANY (ARRAY['reserved'::text, 'shipped'::text])));


--
-- Name: invoice_items_invoice_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoice_items_invoice_idx ON public.invoice_items USING btree (invoice_id);


--
-- Name: invoice_items_product_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoice_items_product_idx ON public.invoice_items USING btree (product_id, invoice_id);


--
-- Name: invoice_items_variant_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoice_items_variant_idx ON public.invoice_items USING btree (variant_id, invoice_id);


--
-- Name: invoices_customer_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoices_customer_idx ON public.invoices USING btree (customer_id, issued_at, id);


--
-- Name: invoices_issued_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoices_issued_idx ON public.invoices USING btree (issued_at, id) WHERE (lifecycle = 'issued'::text);


--
-- Name: invoices_staff_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX invoices_staff_idx ON public.invoices USING btree (responsible_staff_id, issued_at, id);


--
-- Name: lots_fifo_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX lots_fifo_idx ON public.inventory_lots USING btree (variant_id, received_at, id);


--
-- Name: orders_customer_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX orders_customer_idx ON public.orders USING btree (customer_id, created_at);


--
-- Name: redemptions_quota_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX redemptions_quota_idx ON public.promotion_redemptions USING btree (promotion_id, status, customer_id);


--
-- Name: reservations_expiry_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX reservations_expiry_idx ON public.inventory_reservations USING btree (status, expires_at);


--
-- Name: session_user_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX session_user_idx ON public.session USING btree (user_id);


--
-- Name: trade_in_credit_once; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX trade_in_credit_once ON public.store_credit_entries USING btree (trade_in_item_id) WHERE (kind = 'trade_in'::text);


--
-- Name: trade_in_identifier_active_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE UNIQUE INDEX trade_in_identifier_active_idx ON public.trade_in_items USING btree (identifier) WHERE ((identifier IS NOT NULL) AND (state <> ALL (ARRAY['completed'::text, 'returned'::text, 'rejected'::text])));


--
-- Name: verification_identifier_idx; Type: INDEX; Schema: public; Owner: botzone
--

CREATE INDEX verification_identifier_idx ON public.verification USING btree (identifier);


--
-- Name: audit_events audit_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER audit_append_only BEFORE DELETE OR UPDATE ON public.audit_events FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: credit_note_items credit_item_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER credit_item_totals AFTER INSERT OR DELETE OR UPDATE ON public.credit_note_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_credit_note();


--
-- Name: credit_note_items credit_items_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER credit_items_immutable BEFORE INSERT OR DELETE OR UPDATE ON public.credit_note_items FOR EACH ROW EXECUTE FUNCTION public.guard_credit_child();


--
-- Name: store_credit_entries credit_ledger_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER credit_ledger_append_only BEFORE DELETE OR UPDATE ON public.store_credit_entries FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: credit_notes credit_note_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER credit_note_immutable BEFORE DELETE OR UPDATE ON public.credit_notes FOR EACH ROW EXECUTE FUNCTION public.guard_credit_note();


--
-- Name: credit_notes credit_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER credit_totals AFTER INSERT OR UPDATE ON public.credit_notes DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_credit_note();


--
-- Name: credit_note_item_units credit_unit_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER credit_unit_totals AFTER INSERT OR DELETE OR UPDATE ON public.credit_note_item_units DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_credit_note();


--
-- Name: credit_note_item_units credit_units_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER credit_units_immutable BEFORE INSERT OR DELETE OR UPDATE ON public.credit_note_item_units FOR EACH ROW EXECUTE FUNCTION public.guard_credit_child();


--
-- Name: invoices invoice_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER invoice_immutable BEFORE DELETE OR UPDATE ON public.invoices FOR EACH ROW EXECUTE FUNCTION public.guard_invoice();


--
-- Name: invoice_items invoice_item_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER invoice_item_immutable BEFORE INSERT OR DELETE OR UPDATE ON public.invoice_items FOR EACH ROW EXECUTE FUNCTION public.guard_invoice_item();


--
-- Name: invoice_items invoice_lines_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER invoice_lines_totals AFTER INSERT OR DELETE OR UPDATE ON public.invoice_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_invoice_totals();


--
-- Name: invoices invoice_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER invoice_totals AFTER INSERT OR UPDATE ON public.invoices DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_invoice_totals();


--
-- Name: landing_drafts landing_draft_permission; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER landing_draft_permission BEFORE INSERT OR DELETE OR UPDATE ON public.landing_drafts FOR EACH ROW EXECUTE FUNCTION public.cms_permission();


--
-- Name: landing_publications landing_publication_permission; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER landing_publication_permission BEFORE INSERT ON public.landing_publications FOR EACH ROW EXECUTE FUNCTION public.cms_permission();


--
-- Name: landing_versions landing_version_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER landing_version_immutable BEFORE DELETE OR UPDATE ON public.landing_versions FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: landing_versions landing_version_permission; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER landing_version_permission BEFORE INSERT ON public.landing_versions FOR EACH ROW EXECUTE FUNCTION public.cms_permission();


--
-- Name: loyalty_entries loyalty_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER loyalty_append_only BEFORE DELETE OR UPDATE ON public.loyalty_entries FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: orders order_header_snapshot; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER order_header_snapshot BEFORE UPDATE ON public.orders FOR EACH ROW EXECUTE FUNCTION public.guard_order_header();


--
-- Name: order_items order_item_snapshot; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER order_item_snapshot BEFORE INSERT OR DELETE OR UPDATE ON public.order_items FOR EACH ROW EXECUTE FUNCTION public.guard_order_snapshot();


--
-- Name: order_items order_lines_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER order_lines_totals AFTER INSERT OR DELETE OR UPDATE ON public.order_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_order_totals();


--
-- Name: orders order_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER order_totals AFTER INSERT OR UPDATE ON public.orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_order_totals();


--
-- Name: order_item_unit_amounts order_unit_snapshot; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER order_unit_snapshot BEFORE INSERT OR DELETE OR UPDATE ON public.order_item_unit_amounts FOR EACH ROW EXECUTE FUNCTION public.guard_order_snapshot();


--
-- Name: order_item_unit_amounts order_units_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER order_units_totals AFTER INSERT OR DELETE OR UPDATE ON public.order_item_unit_amounts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_order_totals();


--
-- Name: promotion_versions promotion_version_guard; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER promotion_version_guard BEFORE DELETE OR UPDATE ON public.promotion_versions FOR EACH ROW EXECUTE FUNCTION public.used_promotion_immutable();


--
-- Name: landing_publications publication_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER publication_append_only BEFORE DELETE OR UPDATE ON public.landing_publications FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: payment_receipts receipt_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER receipt_totals AFTER INSERT ON public.payment_receipts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_tender();


--
-- Name: payment_receipts receipts_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER receipts_append_only BEFORE DELETE OR UPDATE ON public.payment_receipts FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: refund_allocations refund_allocation_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER refund_allocation_totals AFTER INSERT OR UPDATE ON public.refund_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_refund();


--
-- Name: refunds refund_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER refund_totals AFTER INSERT OR UPDATE ON public.refunds DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_refund();


--
-- Name: setting_versions settings_history_immutable; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER settings_history_immutable BEFORE DELETE OR UPDATE ON public.setting_versions FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: stock_movements stock_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER stock_append_only BEFORE DELETE OR UPDATE ON public.stock_movements FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: order_tender_allocations tender_totals; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE CONSTRAINT TRIGGER tender_totals AFTER INSERT ON public.order_tender_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_tender();


--
-- Name: order_tender_allocations tenders_append_only; Type: TRIGGER; Schema: public; Owner: botzone
--

CREATE TRIGGER tenders_append_only BEFORE DELETE OR UPDATE ON public.order_tender_allocations FOR EACH ROW EXECUTE FUNCTION public.reject_mutation();


--
-- Name: account account_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.account
    ADD CONSTRAINT account_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id) ON DELETE CASCADE;


--
-- Name: addresses addresses_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.addresses
    ADD CONSTRAINT addresses_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: attribute_definitions attribute_definitions_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.attribute_definitions
    ADD CONSTRAINT attribute_definitions_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(id);


--
-- Name: audit_events audit_events_actor_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.audit_events
    ADD CONSTRAINT audit_events_actor_user_id_fkey FOREIGN KEY (actor_user_id) REFERENCES public."user"(id);


--
-- Name: benefit_holds benefit_holds_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.benefit_holds
    ADD CONSTRAINT benefit_holds_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: benefit_holds benefit_holds_order_id_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.benefit_holds
    ADD CONSTRAINT benefit_holds_order_id_customer_id_fkey FOREIGN KEY (order_id, customer_id) REFERENCES public.orders(id, customer_id);


--
-- Name: cart_items cart_items_cart_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_cart_id_fkey FOREIGN KEY (cart_id) REFERENCES public.carts(id) ON DELETE CASCADE;


--
-- Name: cart_items cart_items_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: carts carts_applied_promotion_version_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.carts
    ADD CONSTRAINT carts_applied_promotion_version_id_fkey FOREIGN KEY (applied_promotion_version_id) REFERENCES public.promotion_versions(id);


--
-- Name: carts carts_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.carts
    ADD CONSTRAINT carts_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: credit_note_item_units credit_note_item_units_credit_note_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_item_units
    ADD CONSTRAINT credit_note_item_units_credit_note_item_id_fkey FOREIGN KEY (credit_note_item_id) REFERENCES public.credit_note_items(id);


--
-- Name: credit_note_item_units credit_note_item_units_order_item_unit_amount_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_item_units
    ADD CONSTRAINT credit_note_item_units_order_item_unit_amount_id_fkey FOREIGN KEY (order_item_unit_amount_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: credit_note_items credit_note_items_credit_note_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_items
    ADD CONSTRAINT credit_note_items_credit_note_id_fkey FOREIGN KEY (credit_note_id) REFERENCES public.credit_notes(id);


--
-- Name: credit_note_items credit_note_items_invoice_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_note_items
    ADD CONSTRAINT credit_note_items_invoice_item_id_fkey FOREIGN KEY (invoice_item_id) REFERENCES public.invoice_items(id);


--
-- Name: credit_notes credit_notes_approved_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_approved_by_user_id_fkey FOREIGN KEY (approved_by_user_id) REFERENCES public."user"(id);


--
-- Name: credit_notes credit_notes_invoice_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_invoice_id_order_id_fkey FOREIGN KEY (invoice_id, order_id) REFERENCES public.invoices(id, order_id);


--
-- Name: credit_notes credit_notes_return_request_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_return_request_id_order_id_fkey FOREIGN KEY (return_request_id, order_id) REFERENCES public.return_requests(id, order_id);


--
-- Name: credit_notes credit_notes_shipment_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_shipment_id_order_id_fkey FOREIGN KEY (shipment_id, order_id) REFERENCES public.shipments(id, order_id);


--
-- Name: credit_notes credit_notes_warranty_claim_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.credit_notes
    ADD CONSTRAINT credit_notes_warranty_claim_id_fkey FOREIGN KEY (warranty_claim_id) REFERENCES public.warranty_claims(id);


--
-- Name: customer_profiles customer_profiles_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.customer_profiles
    ADD CONSTRAINT customer_profiles_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: goods_receipt_items goods_receipt_items_purchase_order_item_id_purchase_order__fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipt_items
    ADD CONSTRAINT goods_receipt_items_purchase_order_item_id_purchase_order__fkey FOREIGN KEY (purchase_order_item_id, purchase_order_id) REFERENCES public.purchase_order_items(id, purchase_order_id);


--
-- Name: goods_receipt_items goods_receipt_items_receipt_id_purchase_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipt_items
    ADD CONSTRAINT goods_receipt_items_receipt_id_purchase_order_id_fkey FOREIGN KEY (receipt_id, purchase_order_id) REFERENCES public.goods_receipts(id, purchase_order_id);


--
-- Name: goods_receipts goods_receipts_purchase_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_purchase_order_id_fkey FOREIGN KEY (purchase_order_id) REFERENCES public.purchase_orders(id);


--
-- Name: goods_receipts goods_receipts_received_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.goods_receipts
    ADD CONSTRAINT goods_receipts_received_by_fkey FOREIGN KEY (received_by) REFERENCES public."user"(id);


--
-- Name: inventory_allocations inventory_allocations_inventory_unit_id_lot_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_allocations
    ADD CONSTRAINT inventory_allocations_inventory_unit_id_lot_id_fkey FOREIGN KEY (inventory_unit_id, lot_id) REFERENCES public.inventory_units(id, lot_id);


--
-- Name: inventory_allocations inventory_allocations_lot_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_allocations
    ADD CONSTRAINT inventory_allocations_lot_id_fkey FOREIGN KEY (lot_id) REFERENCES public.inventory_lots(id);


--
-- Name: inventory_allocations inventory_allocations_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_allocations
    ADD CONSTRAINT inventory_allocations_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.order_items(id);


--
-- Name: inventory_allocations inventory_allocations_order_item_unit_amount_id_order_item_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_allocations
    ADD CONSTRAINT inventory_allocations_order_item_unit_amount_id_order_item_fkey FOREIGN KEY (order_item_unit_amount_id, order_item_id) REFERENCES public.order_item_unit_amounts(id, order_item_id);


--
-- Name: inventory_identifiers inventory_identifiers_inventory_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_identifiers
    ADD CONSTRAINT inventory_identifiers_inventory_unit_id_fkey FOREIGN KEY (inventory_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: inventory_lots inventory_lots_receipt_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_receipt_item_id_fkey FOREIGN KEY (receipt_item_id) REFERENCES public.goods_receipt_items(id);


--
-- Name: inventory_lots inventory_lots_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: inventory_lots inventory_lots_warehouse_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_lots
    ADD CONSTRAINT inventory_lots_warehouse_id_fkey FOREIGN KEY (warehouse_id) REFERENCES public.warehouses(id);


--
-- Name: inventory_reservations inventory_reservations_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_reservations
    ADD CONSTRAINT inventory_reservations_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.order_items(id);


--
-- Name: inventory_reservations inventory_reservations_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_reservations
    ADD CONSTRAINT inventory_reservations_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: inventory_units inventory_units_lot_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.inventory_units
    ADD CONSTRAINT inventory_units_lot_id_fkey FOREIGN KEY (lot_id) REFERENCES public.inventory_lots(id);


--
-- Name: invoice_exports invoice_exports_requested_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_exports
    ADD CONSTRAINT invoice_exports_requested_by_user_id_fkey FOREIGN KEY (requested_by_user_id) REFERENCES public."user"(id);


--
-- Name: invoice_items invoice_items_invoice_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_invoice_id_order_id_fkey FOREIGN KEY (invoice_id, order_id) REFERENCES public.invoices(id, order_id);


--
-- Name: invoice_items invoice_items_order_item_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_order_item_id_order_id_fkey FOREIGN KEY (order_item_id, order_id) REFERENCES public.order_items(id, order_id);


--
-- Name: invoice_items invoice_items_parent_invoice_item_id_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_parent_invoice_item_id_invoice_id_fkey FOREIGN KEY (parent_invoice_item_id, invoice_id) REFERENCES public.invoice_items(id, invoice_id);


--
-- Name: invoice_items invoice_items_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: invoice_items invoice_items_variant_id_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoice_items
    ADD CONSTRAINT invoice_items_variant_id_product_id_fkey FOREIGN KEY (variant_id, product_id) REFERENCES public.product_variants(id, product_id);


--
-- Name: invoices invoices_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: invoices invoices_issued_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_issued_by_user_id_fkey FOREIGN KEY (issued_by_user_id) REFERENCES public."user"(id);


--
-- Name: invoices invoices_order_id_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_order_id_customer_id_fkey FOREIGN KEY (order_id, customer_id) REFERENCES public.orders(id, customer_id);


--
-- Name: invoices invoices_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: invoices invoices_responsible_staff_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_responsible_staff_id_fkey FOREIGN KEY (responsible_staff_id) REFERENCES public.staff_profiles(user_id);


--
-- Name: landing_drafts landing_drafts_page_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_drafts
    ADD CONSTRAINT landing_drafts_page_id_fkey FOREIGN KEY (page_id) REFERENCES public.landing_pages(id);


--
-- Name: landing_drafts landing_drafts_updated_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_drafts
    ADD CONSTRAINT landing_drafts_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES public."user"(id);


--
-- Name: landing_media_refs landing_media_refs_landing_version_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_media_refs
    ADD CONSTRAINT landing_media_refs_landing_version_id_fkey FOREIGN KEY (landing_version_id) REFERENCES public.landing_versions(id);


--
-- Name: landing_media_refs landing_media_refs_media_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_media_refs
    ADD CONSTRAINT landing_media_refs_media_id_fkey FOREIGN KEY (media_id) REFERENCES public.media_assets(id);


--
-- Name: landing_publications landing_publications_published_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_publications
    ADD CONSTRAINT landing_publications_published_by_fkey FOREIGN KEY (published_by) REFERENCES public."user"(id);


--
-- Name: landing_publications landing_publications_version_id_page_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_publications
    ADD CONSTRAINT landing_publications_version_id_page_id_fkey FOREIGN KEY (version_id, page_id) REFERENCES public.landing_versions(id, page_id);


--
-- Name: landing_versions landing_versions_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_versions
    ADD CONSTRAINT landing_versions_created_by_fkey FOREIGN KEY (created_by) REFERENCES public."user"(id);


--
-- Name: landing_versions landing_versions_page_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.landing_versions
    ADD CONSTRAINT landing_versions_page_id_fkey FOREIGN KEY (page_id) REFERENCES public.landing_pages(id);


--
-- Name: loyalty_accounts loyalty_accounts_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_accounts
    ADD CONSTRAINT loyalty_accounts_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: loyalty_entries loyalty_entries_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_entries
    ADD CONSTRAINT loyalty_entries_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: loyalty_entries loyalty_entries_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_entries
    ADD CONSTRAINT loyalty_entries_unit_id_fkey FOREIGN KEY (unit_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: loyalty_entries loyalty_entries_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.loyalty_entries
    ADD CONSTRAINT loyalty_entries_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.loyalty_accounts(user_id);


--
-- Name: media_assets media_assets_owner_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_owner_user_id_fkey FOREIGN KEY (owner_user_id) REFERENCES public."user"(id);


--
-- Name: notifications notifications_recipient_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_recipient_user_id_fkey FOREIGN KEY (recipient_user_id) REFERENCES public."user"(id);


--
-- Name: order_adjustments order_adjustments_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_adjustments
    ADD CONSTRAINT order_adjustments_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.order_items(id);


--
-- Name: order_adjustments order_adjustments_promotion_redemption_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_adjustments
    ADD CONSTRAINT order_adjustments_promotion_redemption_id_fkey FOREIGN KEY (promotion_redemption_id) REFERENCES public.promotion_redemptions(id);


--
-- Name: order_item_unit_amounts order_item_unit_amounts_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_item_unit_amounts
    ADD CONSTRAINT order_item_unit_amounts_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.order_items(id);


--
-- Name: order_item_unit_amounts order_item_unit_amounts_parent_unit_amount_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_item_unit_amounts
    ADD CONSTRAINT order_item_unit_amounts_parent_unit_amount_id_fkey FOREIGN KEY (parent_unit_amount_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: order_items order_items_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: order_items order_items_parent_item_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_parent_item_id_order_id_fkey FOREIGN KEY (parent_item_id, order_id) REFERENCES public.order_items(id, order_id);


--
-- Name: order_items order_items_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: order_items order_items_variant_id_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_variant_id_product_id_fkey FOREIGN KEY (variant_id, product_id) REFERENCES public.product_variants(id, product_id);


--
-- Name: order_tender_allocations order_tender_allocations_order_item_unit_amount_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_tender_allocations
    ADD CONSTRAINT order_tender_allocations_order_item_unit_amount_id_fkey FOREIGN KEY (order_item_unit_amount_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: order_tender_allocations order_tender_allocations_receipt_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.order_tender_allocations
    ADD CONSTRAINT order_tender_allocations_receipt_id_fkey FOREIGN KEY (receipt_id) REFERENCES public.payment_receipts(id);


--
-- Name: orders orders_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: orders orders_responsible_staff_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_responsible_staff_id_fkey FOREIGN KEY (responsible_staff_id) REFERENCES public.staff_profiles(user_id);


--
-- Name: payment_attempts payment_attempts_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_attempts
    ADD CONSTRAINT payment_attempts_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: payment_events payment_events_payment_attempt_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_events
    ADD CONSTRAINT payment_events_payment_attempt_id_fkey FOREIGN KEY (payment_attempt_id) REFERENCES public.payment_attempts(id);


--
-- Name: payment_receipts payment_receipts_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_receipts
    ADD CONSTRAINT payment_receipts_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: payment_receipts payment_receipts_payment_attempt_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.payment_receipts
    ADD CONSTRAINT payment_receipts_payment_attempt_id_order_id_fkey FOREIGN KEY (payment_attempt_id, order_id) REFERENCES public.payment_attempts(id, order_id);


--
-- Name: product_attribute_values product_attribute_values_attribute_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_attribute_values
    ADD CONSTRAINT product_attribute_values_attribute_id_fkey FOREIGN KEY (attribute_id) REFERENCES public.attribute_definitions(id);


--
-- Name: product_attribute_values product_attribute_values_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_attribute_values
    ADD CONSTRAINT product_attribute_values_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: product_compatibilities product_compatibilities_accessory_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_compatibilities
    ADD CONSTRAINT product_compatibilities_accessory_product_id_fkey FOREIGN KEY (accessory_product_id) REFERENCES public.products(id);


--
-- Name: product_compatibilities product_compatibilities_target_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_compatibilities
    ADD CONSTRAINT product_compatibilities_target_product_id_fkey FOREIGN KEY (target_product_id) REFERENCES public.products(id);


--
-- Name: product_media product_media_media_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_media
    ADD CONSTRAINT product_media_media_id_fkey FOREIGN KEY (media_id) REFERENCES public.media_assets(id);


--
-- Name: product_media product_media_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_media
    ADD CONSTRAINT product_media_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: product_media product_media_variant_id_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_media
    ADD CONSTRAINT product_media_variant_id_product_id_fkey FOREIGN KEY (variant_id, product_id) REFERENCES public.product_variants(id, product_id);


--
-- Name: product_translations product_translations_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_translations
    ADD CONSTRAINT product_translations_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: product_variants product_variants_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.product_variants
    ADD CONSTRAINT product_variants_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: products products_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(id);


--
-- Name: promotion_redemptions promotion_redemptions_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: promotion_redemptions promotion_redemptions_order_id_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_order_id_customer_id_fkey FOREIGN KEY (order_id, customer_id) REFERENCES public.orders(id, customer_id);


--
-- Name: promotion_redemptions promotion_redemptions_promotion_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_promotion_id_fkey FOREIGN KEY (promotion_id) REFERENCES public.promotions(id);


--
-- Name: promotion_redemptions promotion_redemptions_promotion_version_id_promotion_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_redemptions
    ADD CONSTRAINT promotion_redemptions_promotion_version_id_promotion_id_fkey FOREIGN KEY (promotion_version_id, promotion_id) REFERENCES public.promotion_versions(id, promotion_id);


--
-- Name: promotion_targets promotion_targets_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_targets
    ADD CONSTRAINT promotion_targets_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(id);


--
-- Name: promotion_targets promotion_targets_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_targets
    ADD CONSTRAINT promotion_targets_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: promotion_targets promotion_targets_promotion_version_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_targets
    ADD CONSTRAINT promotion_targets_promotion_version_id_fkey FOREIGN KEY (promotion_version_id) REFERENCES public.promotion_versions(id);


--
-- Name: promotion_targets promotion_targets_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_targets
    ADD CONSTRAINT promotion_targets_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: promotion_versions promotion_versions_promotion_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.promotion_versions
    ADD CONSTRAINT promotion_versions_promotion_id_fkey FOREIGN KEY (promotion_id) REFERENCES public.promotions(id);


--
-- Name: purchase_order_items purchase_order_items_purchase_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_order_items
    ADD CONSTRAINT purchase_order_items_purchase_order_id_fkey FOREIGN KEY (purchase_order_id) REFERENCES public.purchase_orders(id);


--
-- Name: purchase_order_items purchase_order_items_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_order_items
    ADD CONSTRAINT purchase_order_items_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: purchase_orders purchase_orders_approved_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_orders
    ADD CONSTRAINT purchase_orders_approved_by_fkey FOREIGN KEY (approved_by) REFERENCES public."user"(id);


--
-- Name: purchase_orders purchase_orders_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_orders
    ADD CONSTRAINT purchase_orders_created_by_fkey FOREIGN KEY (created_by) REFERENCES public."user"(id);


--
-- Name: purchase_orders purchase_orders_supplier_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.purchase_orders
    ADD CONSTRAINT purchase_orders_supplier_id_fkey FOREIGN KEY (supplier_id) REFERENCES public.suppliers(id);


--
-- Name: refund_allocations refund_allocations_order_item_unit_amount_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refund_allocations
    ADD CONSTRAINT refund_allocations_order_item_unit_amount_id_fkey FOREIGN KEY (order_item_unit_amount_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: refund_allocations refund_allocations_payment_receipt_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refund_allocations
    ADD CONSTRAINT refund_allocations_payment_receipt_id_fkey FOREIGN KEY (payment_receipt_id) REFERENCES public.payment_receipts(id);


--
-- Name: refund_allocations refund_allocations_refund_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refund_allocations
    ADD CONSTRAINT refund_allocations_refund_id_fkey FOREIGN KEY (refund_id) REFERENCES public.refunds(id);


--
-- Name: refunds refunds_approved_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refunds
    ADD CONSTRAINT refunds_approved_by_fkey FOREIGN KEY (approved_by) REFERENCES public."user"(id);


--
-- Name: refunds refunds_credit_note_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refunds
    ADD CONSTRAINT refunds_credit_note_id_order_id_fkey FOREIGN KEY (credit_note_id, order_id) REFERENCES public.credit_notes(id, order_id);


--
-- Name: refunds refunds_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.refunds
    ADD CONSTRAINT refunds_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: return_evidence return_evidence_media_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_evidence
    ADD CONSTRAINT return_evidence_media_id_fkey FOREIGN KEY (media_id) REFERENCES public.media_assets(id);


--
-- Name: return_evidence return_evidence_return_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_evidence
    ADD CONSTRAINT return_evidence_return_request_id_fkey FOREIGN KEY (return_request_id) REFERENCES public.return_requests(id);


--
-- Name: return_item_units return_item_units_inventory_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_item_units
    ADD CONSTRAINT return_item_units_inventory_unit_id_fkey FOREIGN KEY (inventory_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: return_item_units return_item_units_order_item_unit_amount_id_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_item_units
    ADD CONSTRAINT return_item_units_order_item_unit_amount_id_order_item_id_fkey FOREIGN KEY (order_item_unit_amount_id, order_item_id) REFERENCES public.order_item_unit_amounts(id, order_item_id);


--
-- Name: return_item_units return_item_units_return_item_id_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_item_units
    ADD CONSTRAINT return_item_units_return_item_id_order_item_id_fkey FOREIGN KEY (return_item_id, order_item_id) REFERENCES public.return_items(id, order_item_id);


--
-- Name: return_items return_items_order_item_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_items
    ADD CONSTRAINT return_items_order_item_id_order_id_fkey FOREIGN KEY (order_item_id, order_id) REFERENCES public.order_items(id, order_id);


--
-- Name: return_items return_items_return_request_id_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_items
    ADD CONSTRAINT return_items_return_request_id_order_id_fkey FOREIGN KEY (return_request_id, order_id) REFERENCES public.return_requests(id, order_id);


--
-- Name: return_requests return_requests_inspected_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_requests
    ADD CONSTRAINT return_requests_inspected_by_fkey FOREIGN KEY (inspected_by) REFERENCES public."user"(id);


--
-- Name: return_requests return_requests_order_id_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.return_requests
    ADD CONSTRAINT return_requests_order_id_customer_id_fkey FOREIGN KEY (order_id, customer_id) REFERENCES public.orders(id, customer_id);


--
-- Name: session session_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.session
    ADD CONSTRAINT session_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id) ON DELETE CASCADE;


--
-- Name: setting_versions setting_versions_changed_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.setting_versions
    ADD CONSTRAINT setting_versions_changed_by_fkey FOREIGN KEY (changed_by) REFERENCES public."user"(id);


--
-- Name: setting_versions setting_versions_key_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.setting_versions
    ADD CONSTRAINT setting_versions_key_fkey FOREIGN KEY (key) REFERENCES public.app_settings(key);


--
-- Name: shipment_events shipment_events_shipment_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipment_events
    ADD CONSTRAINT shipment_events_shipment_id_fkey FOREIGN KEY (shipment_id) REFERENCES public.shipments(id);


--
-- Name: shipments shipments_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipments
    ADD CONSTRAINT shipments_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: shipments shipments_retry_of_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.shipments
    ADD CONSTRAINT shipments_retry_of_fkey FOREIGN KEY (retry_of) REFERENCES public.shipments(id);


--
-- Name: staff_profiles staff_profiles_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.staff_profiles
    ADD CONSTRAINT staff_profiles_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: stock_adjustments stock_adjustments_approved_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_approved_by_fkey FOREIGN KEY (approved_by) REFERENCES public."user"(id);


--
-- Name: stock_adjustments stock_adjustments_lot_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_lot_id_fkey FOREIGN KEY (lot_id) REFERENCES public.inventory_lots(id);


--
-- Name: stock_adjustments stock_adjustments_requested_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_adjustments
    ADD CONSTRAINT stock_adjustments_requested_by_fkey FOREIGN KEY (requested_by) REFERENCES public."user"(id);


--
-- Name: stock_movements stock_movements_actor_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_actor_user_id_fkey FOREIGN KEY (actor_user_id) REFERENCES public."user"(id);


--
-- Name: stock_movements stock_movements_lot_id_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_lot_id_variant_id_fkey FOREIGN KEY (lot_id, variant_id) REFERENCES public.inventory_lots(id, variant_id);


--
-- Name: stock_movements stock_movements_unit_id_lot_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_unit_id_lot_id_fkey FOREIGN KEY (unit_id, lot_id) REFERENCES public.inventory_units(id, lot_id);


--
-- Name: stock_movements stock_movements_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.stock_movements
    ADD CONSTRAINT stock_movements_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: store_credit_accounts store_credit_accounts_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_accounts
    ADD CONSTRAINT store_credit_accounts_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: store_credit_entries store_credit_entries_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.orders(id);


--
-- Name: store_credit_entries store_credit_entries_quote_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_quote_item_id_fkey FOREIGN KEY (quote_item_id) REFERENCES public.trade_in_quote_items(id);


--
-- Name: store_credit_entries store_credit_entries_refund_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_refund_id_fkey FOREIGN KEY (refund_id) REFERENCES public.refunds(id);


--
-- Name: store_credit_entries store_credit_entries_trade_in_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_trade_in_item_id_fkey FOREIGN KEY (trade_in_item_id) REFERENCES public.trade_in_items(id);


--
-- Name: store_credit_entries store_credit_entries_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.store_credit_entries
    ADD CONSTRAINT store_credit_entries_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.store_credit_accounts(user_id);


--
-- Name: trade_in_evidence trade_in_evidence_inspection_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_evidence
    ADD CONSTRAINT trade_in_evidence_inspection_id_fkey FOREIGN KEY (inspection_id) REFERENCES public.trade_in_inspections(id);


--
-- Name: trade_in_evidence trade_in_evidence_media_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_evidence
    ADD CONSTRAINT trade_in_evidence_media_id_fkey FOREIGN KEY (media_id) REFERENCES public.media_assets(id);


--
-- Name: trade_in_inspections trade_in_inspections_inspector_staff_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_inspections
    ADD CONSTRAINT trade_in_inspections_inspector_staff_id_fkey FOREIGN KEY (inspector_staff_id) REFERENCES public.staff_profiles(user_id);


--
-- Name: trade_in_inspections trade_in_inspections_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_inspections
    ADD CONSTRAINT trade_in_inspections_item_id_fkey FOREIGN KEY (item_id) REFERENCES public.trade_in_items(id);


--
-- Name: trade_in_items trade_in_items_inventory_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_inventory_unit_id_fkey FOREIGN KEY (inventory_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: trade_in_items trade_in_items_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: trade_in_items trade_in_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.trade_in_requests(id);


--
-- Name: trade_in_items trade_in_items_variant_id_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_items
    ADD CONSTRAINT trade_in_items_variant_id_product_id_fkey FOREIGN KEY (variant_id, product_id) REFERENCES public.product_variants(id, product_id);


--
-- Name: trade_in_quote_items trade_in_quote_items_item_id_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quote_items
    ADD CONSTRAINT trade_in_quote_items_item_id_request_id_fkey FOREIGN KEY (item_id, request_id) REFERENCES public.trade_in_items(id, request_id);


--
-- Name: trade_in_quote_items trade_in_quote_items_quote_id_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quote_items
    ADD CONSTRAINT trade_in_quote_items_quote_id_request_id_fkey FOREIGN KEY (quote_id, request_id) REFERENCES public.trade_in_quotes(id, request_id);


--
-- Name: trade_in_quotes trade_in_quotes_approved_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_approved_by_fkey FOREIGN KEY (approved_by) REFERENCES public."user"(id);


--
-- Name: trade_in_quotes trade_in_quotes_quoted_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_quoted_by_fkey FOREIGN KEY (quoted_by) REFERENCES public."user"(id);


--
-- Name: trade_in_quotes trade_in_quotes_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_quotes
    ADD CONSTRAINT trade_in_quotes_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.trade_in_requests(id);


--
-- Name: trade_in_requests trade_in_requests_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_requests
    ADD CONSTRAINT trade_in_requests_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public."user"(id);


--
-- Name: trade_in_shipments trade_in_shipments_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.trade_in_shipments
    ADD CONSTRAINT trade_in_shipments_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.trade_in_requests(id);


--
-- Name: variant_attribute_values variant_attribute_values_attribute_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.variant_attribute_values
    ADD CONSTRAINT variant_attribute_values_attribute_id_fkey FOREIGN KEY (attribute_id) REFERENCES public.attribute_definitions(id);


--
-- Name: variant_attribute_values variant_attribute_values_variant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.variant_attribute_values
    ADD CONSTRAINT variant_attribute_values_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES public.product_variants(id);


--
-- Name: warranty_claims warranty_claims_entitlement_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_claims
    ADD CONSTRAINT warranty_claims_entitlement_id_fkey FOREIGN KEY (entitlement_id) REFERENCES public.warranty_entitlements(id);


--
-- Name: warranty_entitlements warranty_entitlements_inventory_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_entitlements
    ADD CONSTRAINT warranty_entitlements_inventory_unit_id_fkey FOREIGN KEY (inventory_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: warranty_entitlements warranty_entitlements_order_item_unit_amount_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_entitlements
    ADD CONSTRAINT warranty_entitlements_order_item_unit_amount_id_fkey FOREIGN KEY (order_item_unit_amount_id) REFERENCES public.order_item_unit_amounts(id);


--
-- Name: warranty_entitlements warranty_entitlements_plan_version_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_entitlements
    ADD CONSTRAINT warranty_entitlements_plan_version_id_fkey FOREIGN KEY (plan_version_id) REFERENCES public.warranty_plan_versions(id);


--
-- Name: warranty_events warranty_events_actor_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_events
    ADD CONSTRAINT warranty_events_actor_id_fkey FOREIGN KEY (actor_id) REFERENCES public."user"(id);


--
-- Name: warranty_events warranty_events_claim_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_events
    ADD CONSTRAINT warranty_events_claim_id_fkey FOREIGN KEY (claim_id) REFERENCES public.warranty_claims(id);


--
-- Name: warranty_evidence warranty_evidence_claim_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_evidence
    ADD CONSTRAINT warranty_evidence_claim_id_fkey FOREIGN KEY (claim_id) REFERENCES public.warranty_claims(id);


--
-- Name: warranty_evidence warranty_evidence_media_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_evidence
    ADD CONSTRAINT warranty_evidence_media_id_fkey FOREIGN KEY (media_id) REFERENCES public.media_assets(id);


--
-- Name: warranty_plan_versions warranty_plan_versions_plan_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_plan_versions
    ADD CONSTRAINT warranty_plan_versions_plan_id_fkey FOREIGN KEY (plan_id) REFERENCES public.warranty_plans(id);


--
-- Name: warranty_replacements warranty_replacements_claim_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_claim_id_fkey FOREIGN KEY (claim_id) REFERENCES public.warranty_claims(id);


--
-- Name: warranty_replacements warranty_replacements_new_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_new_unit_id_fkey FOREIGN KEY (new_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: warranty_replacements warranty_replacements_old_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.warranty_replacements
    ADD CONSTRAINT warranty_replacements_old_unit_id_fkey FOREIGN KEY (old_unit_id) REFERENCES public.inventory_units(id);


--
-- Name: wishlists wishlists_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.wishlists
    ADD CONSTRAINT wishlists_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);


--
-- Name: wishlists wishlists_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: botzone
--

ALTER TABLE ONLY public.wishlists
    ADD CONSTRAINT wishlists_user_id_fkey FOREIGN KEY (user_id) REFERENCES public."user"(id);


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: pg_database_owner
--

GRANT ALL ON SCHEMA public TO botzone;
GRANT USAGE ON SCHEMA public TO botzone_app;


--
-- Name: TABLE account; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.account TO botzone_app;


--
-- Name: TABLE addresses; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.addresses TO botzone_app;


--
-- Name: TABLE app_settings; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.app_settings TO botzone_app;


--
-- Name: TABLE attribute_definitions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.attribute_definitions TO botzone_app;


--
-- Name: TABLE audit_events; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audit_events TO botzone_app;


--
-- Name: TABLE benefit_holds; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.benefit_holds TO botzone_app;


--
-- Name: TABLE cart_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.cart_items TO botzone_app;


--
-- Name: TABLE carts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.carts TO botzone_app;


--
-- Name: TABLE categories; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.categories TO botzone_app;


--
-- Name: TABLE credit_note_item_units; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.credit_note_item_units TO botzone_app;


--
-- Name: TABLE credit_note_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.credit_note_items TO botzone_app;


--
-- Name: TABLE credit_notes; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.credit_notes TO botzone_app;


--
-- Name: TABLE customer_profiles; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.customer_profiles TO botzone_app;


--
-- Name: TABLE goods_receipt_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.goods_receipt_items TO botzone_app;


--
-- Name: TABLE goods_receipts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.goods_receipts TO botzone_app;


--
-- Name: TABLE idempotency_records; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.idempotency_records TO botzone_app;


--
-- Name: TABLE inventory_allocations; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.inventory_allocations TO botzone_app;


--
-- Name: TABLE inventory_identifiers; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.inventory_identifiers TO botzone_app;


--
-- Name: TABLE inventory_lots; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.inventory_lots TO botzone_app;


--
-- Name: TABLE inventory_reservations; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.inventory_reservations TO botzone_app;


--
-- Name: TABLE inventory_units; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.inventory_units TO botzone_app;


--
-- Name: TABLE invoice_exports; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.invoice_exports TO botzone_app;


--
-- Name: TABLE invoice_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.invoice_items TO botzone_app;


--
-- Name: SEQUENCE invoice_number_seq; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,USAGE ON SEQUENCE public.invoice_number_seq TO botzone_app;


--
-- Name: TABLE invoices; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.invoices TO botzone_app;


--
-- Name: TABLE landing_drafts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.landing_drafts TO botzone_app;


--
-- Name: TABLE landing_media_refs; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.landing_media_refs TO botzone_app;


--
-- Name: TABLE landing_pages; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.landing_pages TO botzone_app;


--
-- Name: TABLE landing_publications; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.landing_publications TO botzone_app;


--
-- Name: TABLE landing_versions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.landing_versions TO botzone_app;


--
-- Name: TABLE loyalty_accounts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.loyalty_accounts TO botzone_app;


--
-- Name: TABLE loyalty_entries; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.loyalty_entries TO botzone_app;


--
-- Name: TABLE media_assets; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.media_assets TO botzone_app;


--
-- Name: TABLE notifications; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.notifications TO botzone_app;


--
-- Name: TABLE order_adjustments; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.order_adjustments TO botzone_app;


--
-- Name: TABLE order_item_unit_amounts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.order_item_unit_amounts TO botzone_app;


--
-- Name: TABLE order_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.order_items TO botzone_app;


--
-- Name: TABLE order_tender_allocations; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.order_tender_allocations TO botzone_app;


--
-- Name: TABLE orders; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.orders TO botzone_app;


--
-- Name: TABLE outbox_events; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.outbox_events TO botzone_app;


--
-- Name: TABLE payment_attempts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.payment_attempts TO botzone_app;


--
-- Name: TABLE payment_events; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.payment_events TO botzone_app;


--
-- Name: TABLE payment_receipts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.payment_receipts TO botzone_app;


--
-- Name: TABLE product_attribute_values; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.product_attribute_values TO botzone_app;


--
-- Name: TABLE product_compatibilities; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.product_compatibilities TO botzone_app;


--
-- Name: TABLE product_media; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.product_media TO botzone_app;


--
-- Name: TABLE product_translations; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.product_translations TO botzone_app;


--
-- Name: TABLE product_variants; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.product_variants TO botzone_app;


--
-- Name: TABLE products; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.products TO botzone_app;


--
-- Name: TABLE promotion_redemptions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.promotion_redemptions TO botzone_app;


--
-- Name: TABLE promotion_targets; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.promotion_targets TO botzone_app;


--
-- Name: TABLE promotion_versions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.promotion_versions TO botzone_app;


--
-- Name: TABLE promotions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.promotions TO botzone_app;


--
-- Name: TABLE purchase_order_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.purchase_order_items TO botzone_app;


--
-- Name: TABLE purchase_orders; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.purchase_orders TO botzone_app;


--
-- Name: TABLE refund_allocations; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.refund_allocations TO botzone_app;


--
-- Name: TABLE refunds; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.refunds TO botzone_app;


--
-- Name: TABLE return_evidence; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.return_evidence TO botzone_app;


--
-- Name: TABLE return_item_units; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.return_item_units TO botzone_app;


--
-- Name: TABLE return_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.return_items TO botzone_app;


--
-- Name: TABLE return_requests; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.return_requests TO botzone_app;


--
-- Name: TABLE session; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.session TO botzone_app;


--
-- Name: TABLE setting_versions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.setting_versions TO botzone_app;


--
-- Name: TABLE shipment_events; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.shipment_events TO botzone_app;


--
-- Name: TABLE shipments; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.shipments TO botzone_app;


--
-- Name: TABLE staff_profiles; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.staff_profiles TO botzone_app;


--
-- Name: TABLE stock_adjustments; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.stock_adjustments TO botzone_app;


--
-- Name: TABLE stock_movements; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.stock_movements TO botzone_app;


--
-- Name: TABLE store_credit_accounts; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.store_credit_accounts TO botzone_app;


--
-- Name: TABLE store_credit_entries; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.store_credit_entries TO botzone_app;


--
-- Name: TABLE suppliers; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.suppliers TO botzone_app;


--
-- Name: TABLE trade_in_evidence; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_evidence TO botzone_app;


--
-- Name: TABLE trade_in_inspections; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_inspections TO botzone_app;


--
-- Name: TABLE trade_in_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_items TO botzone_app;


--
-- Name: TABLE trade_in_quote_items; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_quote_items TO botzone_app;


--
-- Name: TABLE trade_in_quotes; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_quotes TO botzone_app;


--
-- Name: TABLE trade_in_requests; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_requests TO botzone_app;


--
-- Name: TABLE trade_in_shipments; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.trade_in_shipments TO botzone_app;


--
-- Name: TABLE "user"; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."user" TO botzone_app;


--
-- Name: TABLE variant_attribute_values; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.variant_attribute_values TO botzone_app;


--
-- Name: TABLE variant_stock; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.variant_stock TO botzone_app;


--
-- Name: TABLE verification; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.verification TO botzone_app;


--
-- Name: TABLE warehouses; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warehouses TO botzone_app;


--
-- Name: TABLE warranty_claims; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_claims TO botzone_app;


--
-- Name: TABLE warranty_entitlements; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_entitlements TO botzone_app;


--
-- Name: TABLE warranty_events; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_events TO botzone_app;


--
-- Name: TABLE warranty_evidence; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_evidence TO botzone_app;


--
-- Name: TABLE warranty_plan_versions; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_plan_versions TO botzone_app;


--
-- Name: TABLE warranty_plans; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_plans TO botzone_app;


--
-- Name: TABLE warranty_replacements; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.warranty_replacements TO botzone_app;


--
-- Name: TABLE wishlists; Type: ACL; Schema: public; Owner: botzone
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.wishlists TO botzone_app;


--
-- Name: DEFAULT PRIVILEGES FOR SEQUENCES; Type: DEFAULT ACL; Schema: public; Owner: botzone
--

ALTER DEFAULT PRIVILEGES FOR ROLE botzone IN SCHEMA public GRANT SELECT,USAGE ON SEQUENCES TO botzone_app;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: botzone
--

ALTER DEFAULT PRIVILEGES FOR ROLE botzone IN SCHEMA public GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO botzone_app;


--
-- PostgreSQL database dump complete
--

\unrestrict 8onSh8xUKB9OguQJM3eED05OqkmESAN7RyJySZR4H935rFSBJqtQBCkWmzzc0Qs

