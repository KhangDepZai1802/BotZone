# BotZone — actual PostgreSQL data dictionary

Captured `2026-11-13T08:20:29.145581+00:00` from `botzone_baseline_test_20261113135743_c0d401c6` as `botzone` using a repeatable-read, read-only transaction. Public schema: **100 tables, 649 columns, 182 foreign keys, 218 indexes**. Metadata only; no customer/order rows or credentials exported.

Snapshot SHA-256: `2d35084bc09497a070afe3a7f8b69159b7ab0980e1212054e42e8f163d853cad`.

Content update on 13 November 2026: `products.poster` now contains verified `/product-photos/` raster paths for the 35 catalogue models; original defaults below remain unchanged. Updates are recorded in `audit_events`. Landing JSON references the two versioned v003 bundled models/posters through the existing CMS revision/publication tables. This update adds no tables, columns or migrations; the captured structural metadata remains applicable.

The later storefront update adds `products.specs.technicalSpecifications` with modelCode/name/verifiedAt/sources/groups/highlightIds and bilingual label/value objects. All existing JSON keys remain stored; public API projection excludes fixture bookkeeping and selects the requested language.609 fields across35models were imported transactionally with before/after audit. Registration uses existing `user.phone_number`, `customer_profiles.phone` and `addresses`; remember-me changes cookie persistence only, with the existing8-hour `auth_sessions.expires_at` limit. No relational schema change was made. See [implementation details](../storefront-ux-localization.md).

Source of truth: [schema-snapshot.json](schema-snapshot.json), captured by [capture-schema.sql](capture-schema.sql). EF model: `src/BotZone.Infrastructure/Data/BotZoneDbContext.cs`, partial mappings and `Migrations/BotZoneDbContextModelSnapshot.cs`. Database defaults/checks/triggers below are catalog evidence; do not infer them from C# nullable annotations.

## Migration history

- `20261113070723_AdoptLegacyAndIdentity` — EF `10.0.7`
- `20261113072619_IntegrityRepairAndPreview` — EF `10.0.7`
- `20261113073844_WarrantyQuantityAndLedgerSources` — EF `10.0.7`
- `20261113075041_PolicySnapshotsAndCustody` — EF `10.0.7`
- `20261113080440_IdentityNamesAndFinancialHistory` — EF `10.0.7`

## Reading this dictionary

`NULL=Yes` means PostgreSQL permits null. `—` means no default/comment/constraint entry exists. Constraint types: `p` primary key, `f` foreign key, `u` unique, `c` CHECK, `n` NOT NULL, `x` exclusion. FK definitions include update/delete actions; `DEFERRABLE`/`INITIALLY DEFERRED` are recorded separately. Partial/functional indexes are preserved verbatim. `bigint` VND values cross the API as decimal strings. PostgreSQL enum labels are listed at the end.

## Table index

- **System & audit:** [__EFMigrationsHistory](#__efmigrationshistory), [app_settings](#app_settings), [audit_events](#audit_events), [idempotency_records](#idempotency_records), [notifications](#notifications), [outbox_events](#outbox_events), [schema_migrations](#schema_migrations), [setting_versions](#setting_versions)
- **Identity & people:** [account](#account), [addresses](#addresses), [auth_sessions](#auth_sessions), [customer_profiles](#customer_profiles), [identity_role_claims](#identity_role_claims), [identity_roles](#identity_roles), [identity_user_claims](#identity_user_claims), [identity_user_logins](#identity_user_logins), [identity_user_roles](#identity_user_roles), [identity_user_tokens](#identity_user_tokens), [session](#session), [staff_profiles](#staff_profiles), [user](#user), [verification](#verification)
- **Catalogue:** [attribute_definitions](#attribute_definitions), [categories](#categories), [product_attribute_values](#product_attribute_values), [product_compatibilities](#product_compatibilities), [product_media](#product_media), [product_translations](#product_translations), [product_variants](#product_variants), [products](#products), [variant_attribute_values](#variant_attribute_values)
- **Cart & promotions:** [benefit_holds](#benefit_holds), [cart_items](#cart_items), [carts](#carts), [promotion_redemptions](#promotion_redemptions), [promotion_targets](#promotion_targets), [promotion_versions](#promotion_versions), [promotions](#promotions), [wishlists](#wishlists)
- **Invoices & refunds:** [credit_note_item_units](#credit_note_item_units), [credit_note_items](#credit_note_items), [credit_notes](#credit_notes), [invoice_exports](#invoice_exports), [invoice_items](#invoice_items), [invoices](#invoices), [refund_allocations](#refund_allocations), [refunds](#refunds)
- **Inventory & purchasing:** [goods_receipt_items](#goods_receipt_items), [goods_receipts](#goods_receipts), [inventory_allocations](#inventory_allocations), [inventory_identifiers](#inventory_identifiers), [inventory_lots](#inventory_lots), [inventory_quarantine](#inventory_quarantine), [inventory_reservations](#inventory_reservations), [inventory_units](#inventory_units), [purchase_order_items](#purchase_order_items), [purchase_orders](#purchase_orders), [stock_adjustments](#stock_adjustments), [stock_movements](#stock_movements), [suppliers](#suppliers), [warehouses](#warehouses)
- **Landing & media:** [landing_drafts](#landing_drafts), [landing_media_refs](#landing_media_refs), [landing_pages](#landing_pages), [landing_preview_grants](#landing_preview_grants), [landing_publications](#landing_publications), [landing_versions](#landing_versions), [media_assets](#media_assets)
- **Customer ledgers:** [loyalty_accounts](#loyalty_accounts), [loyalty_entries](#loyalty_entries), [store_credit_accounts](#store_credit_accounts), [store_credit_entries](#store_credit_entries)
- **Orders & payments:** [order_adjustments](#order_adjustments), [order_item_unit_amounts](#order_item_unit_amounts), [order_items](#order_items), [order_tender_allocations](#order_tender_allocations), [orders](#orders), [payment_attempts](#payment_attempts), [payment_events](#payment_events), [payment_receipts](#payment_receipts), [shipment_events](#shipment_events), [shipments](#shipments)
- **Returns & trade in:** [return_evidence](#return_evidence), [return_item_units](#return_item_units), [return_items](#return_items), [return_requests](#return_requests), [trade_in_evidence](#trade_in_evidence), [trade_in_inspections](#trade_in_inspections), [trade_in_items](#trade_in_items), [trade_in_quote_items](#trade_in_quote_items), [trade_in_quotes](#trade_in_quotes), [trade_in_requests](#trade_in_requests), [trade_in_shipments](#trade_in_shipments)
- **Warranty:** [warranty_claims](#warranty_claims), [warranty_entitlements](#warranty_entitlements), [warranty_events](#warranty_events), [warranty_evidence](#warranty_evidence), [warranty_plan_versions](#warranty_plan_versions), [warranty_plans](#warranty_plans), [warranty_replacements](#warranty_replacements)

## __EFMigrationsHistory

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| MigrationId | character varying(150) | No | — | PK: PK___EFMigrationsHistory | — | — |
| ProductVersion | character varying(32) | No | — | — | — | — |

### Constraints

**`PK___EFMigrationsHistory`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY ("MigrationId")
```

**`__EFMigrationsHistory_MigrationId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL "MigrationId"
```

**`__EFMigrationsHistory_ProductVersion_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL "ProductVersion"
```


### Indexes

**`PK___EFMigrationsHistory`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK___EFMigrationsHistory" ON public."__EFMigrationsHistory" USING btree ("MigrationId")
```


### Triggers

None.

## account

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | text | No | — | PK: account_pkey | — | — |
| account_id | text | No | — | UNIQUE: account_provider_id_account_id_key | — | — |
| provider_id | text | No | — | UNIQUE: account_provider_id_account_id_key | — | — |
| user_id | text | No | — | FK → user: account_user_id_fkey | — | — |
| access_token | text | Yes | — | — | — | — |
| refresh_token | text | Yes | — | — | — | — |
| id_token | text | Yes | — | — | — | — |
| access_token_expires_at | timestamp with time zone | Yes | — | — | — | — |
| refresh_token_expires_at | timestamp with time zone | Yes | — | — | — | — |
| scope | text | Yes | — | — | — | — |
| password | text | Yes | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| updated_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`account_account_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL account_id
```

**`account_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`account_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`account_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`account_provider_id_account_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (provider_id, account_id)
```

**`account_provider_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL provider_id
```

**`account_updated_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL updated_at
```

**`account_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`account_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`account_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX account_pkey ON public.account USING btree (id)
```

**`account_provider_id_account_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX account_provider_id_account_id_key ON public.account USING btree (provider_id, account_id)
```


### Triggers

None.

## addresses

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: addresses_pkey | — | — |
| user_id | text | No | — | FK → user: addresses_user_id_fkey | — | — |
| recipient | text | No | — | — | — | — |
| phone | text | No | — | — | — | — |
| province | text | No | — | — | — | — |
| ward | text | No | — | — | — | — |
| address_line | text | No | — | — | — | — |
| is_default | boolean | No | false | — | — | — |

### Constraints

**`addresses_address_line_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL address_line
```

**`addresses_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`addresses_is_default_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL is_default
```

**`addresses_phone_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL phone
```

**`addresses_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`addresses_province_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL province
```

**`addresses_recipient_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL recipient
```

**`addresses_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`addresses_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```

**`addresses_ward_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL ward
```


### Indexes

**`address_default_idx`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX address_default_idx ON public.addresses USING btree (user_id) WHERE is_default
```

**`addresses_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX addresses_pkey ON public.addresses USING btree (id)
```


### Triggers

None.

## app_settings

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| key | text | No | — | PK: app_settings_pkey | — | — |
| value | jsonb | No | — | — | — | — |
| version | integer | No | 1 | — | — | — |

### Constraints

**`app_settings_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL key
```

**`app_settings_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (key)
```

**`app_settings_value_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL value
```

**`app_settings_version_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version
```


### Indexes

**`app_settings_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX app_settings_pkey ON public.app_settings USING btree (key)
```


### Triggers

None.

## attribute_definitions

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: attribute_definitions_pkey | — | — |
| category_id | uuid | No | — | UNIQUE: attribute_definitions_category_id_code_key; FK → categories: attribute_definitions_category_id_fkey | — | — |
| code | text | No | — | UNIQUE: attribute_definitions_category_id_code_key | — | — |
| value_type | text | No | — | — | — | — |
| unit | text | Yes | — | — | — | — |
| compare_enabled | boolean | No | true | — | — | — |

### Constraints

**`attribute_definitions_category_id_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (category_id, code)
```

**`attribute_definitions_category_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (category_id) REFERENCES categories(id)
```

**`attribute_definitions_category_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL category_id
```

**`attribute_definitions_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`attribute_definitions_compare_enabled_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL compare_enabled
```

**`attribute_definitions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`attribute_definitions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`attribute_definitions_value_type_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (value_type = ANY (ARRAY['text'::text, 'number'::text, 'boolean'::text, 'option'::text]))
```

**`attribute_definitions_value_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL value_type
```


### Indexes

**`attribute_definitions_category_id_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX attribute_definitions_category_id_code_key ON public.attribute_definitions USING btree (category_id, code)
```

**`attribute_definitions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX attribute_definitions_pkey ON public.attribute_definitions USING btree (id)
```


### Triggers

None.

## audit_events

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: audit_events_pkey | — | — |
| actor_user_id | text | Yes | — | FK → user: audit_events_actor_user_id_fkey | — | — |
| action | text | No | — | — | — | — |
| entity_type | text | No | — | — | — | — |
| entity_id | text | Yes | — | — | — | — |
| before_data | jsonb | Yes | — | — | — | — |
| after_data | jsonb | Yes | — | — | — | — |
| reason | text | Yes | — | — | — | — |
| occurred_at | timestamp with time zone | No | now() | — | — | — |
| correlation_id | text | Yes | — | — | — | — |

### Constraints

**`audit_events_action_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL action
```

**`audit_events_actor_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (actor_user_id) REFERENCES "user"(id)
```

**`audit_events_entity_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL entity_type
```

**`audit_events_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`audit_events_occurred_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL occurred_at
```

**`audit_events_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`audit_events_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX audit_events_pkey ON public.audit_events USING btree (id)
```


### Triggers

**`audit_append_only`** — enabled `O`.

```sql
CREATE TRIGGER audit_append_only BEFORE DELETE OR UPDATE ON audit_events FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## auth_sessions

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | — | PK: PK_auth_sessions | — | — |
| user_id | text | No | — | FK → user: FK_auth_sessions_user_user_id | — | — |
| client_type | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| revoked_at | timestamp with time zone | Yes | — | — | — | — |

### Constraints

**`FK_auth_sessions_user_user_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE RESTRICT
```

**`PK_auth_sessions`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`auth_sessions_client_type_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (client_type = ANY (ARRAY['web'::text, 'winforms'::text]))
```

**`auth_sessions_client_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL client_type
```

**`auth_sessions_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`auth_sessions_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`auth_sessions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`auth_sessions_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`IX_auth_sessions_user_id_expires_at`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_auth_sessions_user_id_expires_at" ON public.auth_sessions USING btree (user_id, expires_at)
```

**`PK_auth_sessions`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_auth_sessions" ON public.auth_sessions USING btree (id)
```


### Triggers

None.

## benefit_holds

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: benefit_holds_pkey | — | — |
| customer_id | text | No | — | FK → user: benefit_holds_customer_id_fkey; FK → orders: benefit_holds_order_id_customer_id_fkey | — | — |
| order_id | uuid | No | — | FK → orders: benefit_holds_order_id_customer_id_fkey | — | — |
| kind | text | No | — | — | — | — |
| amount | bigint | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| status | text | No | — | — | — | — |

### Constraints

**`benefit_holds_amount_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount > 0)
```

**`benefit_holds_amount_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount
```

**`benefit_holds_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`benefit_holds_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`benefit_holds_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`benefit_holds_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`benefit_holds_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['points'::text, 'store_credit'::text]))
```

**`benefit_holds_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`benefit_holds_order_id_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id, customer_id) REFERENCES orders(id, customer_id)
```

**`benefit_holds_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`benefit_holds_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`benefit_holds_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['active'::text, 'consumed'::text, 'released'::text]))
```

**`benefit_holds_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`benefit_hold_active_idx`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX benefit_hold_active_idx ON public.benefit_holds USING btree (order_id, kind) WHERE (status = 'active'::text)
```

**`benefit_holds_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX benefit_holds_pkey ON public.benefit_holds USING btree (id)
```


### Triggers

None.

## cart_items

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: cart_items_pkey | — | — |
| cart_id | uuid | No | — | FK → carts: cart_items_cart_id_fkey; UNIQUE: cart_items_cart_id_variant_id_care_key | — | — |
| variant_id | uuid | No | — | UNIQUE: cart_items_cart_id_variant_id_care_key; FK → product_variants: cart_items_variant_id_fkey | — | — |
| quantity | integer | No | — | — | — | — |
| care | boolean | No | false | UNIQUE: cart_items_cart_id_variant_id_care_key | — | — |

### Constraints

**`cart_items_care_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL care
```

**`cart_items_cart_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (cart_id) REFERENCES carts(id) ON DELETE CASCADE
```

**`cart_items_cart_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL cart_id
```

**`cart_items_cart_id_variant_id_care_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (cart_id, variant_id, care)
```

**`cart_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`cart_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`cart_items_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0 AND quantity <= 100)
```

**`cart_items_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`cart_items_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`cart_items_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```


### Indexes

**`cart_items_cart_id_variant_id_care_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX cart_items_cart_id_variant_id_care_key ON public.cart_items USING btree (cart_id, variant_id, care)
```

**`cart_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX cart_items_pkey ON public.cart_items USING btree (id)
```


### Triggers

None.

## carts

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: carts_pkey | — | — |
| customer_id | text | No | — | FK → user: carts_customer_id_fkey; UNIQUE: carts_customer_id_key | — | — |
| applied_promotion_version_id | uuid | Yes | — | FK → promotion_versions: carts_applied_promotion_version_id_fkey | — | — |
| revision | integer | No | 0 | — | — | — |

### Constraints

**`carts_applied_promotion_version_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (applied_promotion_version_id) REFERENCES promotion_versions(id)
```

**`carts_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`carts_customer_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (customer_id)
```

**`carts_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`carts_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`carts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`carts_revision_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL revision
```


### Indexes

**`carts_customer_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX carts_customer_id_key ON public.carts USING btree (customer_id)
```

**`carts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX carts_pkey ON public.carts USING btree (id)
```


### Triggers

None.

## categories

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: categories_pkey | — | — |
| code | text | No | — | UNIQUE: categories_code_key | — | — |
| slug | text | No | — | UNIQUE: categories_slug_key | — | — |
| name_vi | text | No | — | — | — | — |
| name_en | text | No | — | — | — | — |
| active | boolean | No | true | — | — | — |

### Constraints

**`categories_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`categories_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code)
```

**`categories_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`categories_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`categories_name_en_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name_en
```

**`categories_name_vi_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name_vi
```

**`categories_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`categories_slug_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (slug)
```

**`categories_slug_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL slug
```


### Indexes

**`categories_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX categories_code_key ON public.categories USING btree (code)
```

**`categories_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX categories_pkey ON public.categories USING btree (id)
```

**`categories_slug_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX categories_slug_key ON public.categories USING btree (slug)
```


### Triggers

None.

## credit_note_item_units

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| credit_note_item_id | uuid | No | — | FK → credit_note_items: credit_note_item_units_credit_note_item_id_fkey; PK: credit_note_item_units_pkey | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: credit_note_item_units_order_item_unit_amount_id_fkey; PK: credit_note_item_units_pkey | — | — |
| amount_vnd | bigint | No | — | — | — | — |

### Constraints

**`credit_note_item_units_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd >= 0)
```

**`credit_note_item_units_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`credit_note_item_units_credit_note_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (credit_note_item_id) REFERENCES credit_note_items(id)
```

**`credit_note_item_units_credit_note_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL credit_note_item_id
```

**`credit_note_item_units_order_item_unit_amount_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id)
```

**`credit_note_item_units_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`credit_note_item_units_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (credit_note_item_id, order_item_unit_amount_id)
```

**`credit_unit_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`credit_note_item_units_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_note_item_units_pkey ON public.credit_note_item_units USING btree (credit_note_item_id, order_item_unit_amount_id)
```


### Triggers

**`credit_unit_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER credit_unit_totals AFTER INSERT OR DELETE OR UPDATE ON credit_note_item_units DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note()
```

**`credit_units_immutable`** — enabled `O`.

```sql
CREATE TRIGGER credit_units_immutable BEFORE INSERT OR DELETE OR UPDATE ON credit_note_item_units FOR EACH ROW EXECUTE FUNCTION guard_credit_child()
```


## credit_note_items

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: credit_note_items_id_invoice_item_id_key; PK: credit_note_items_pkey | — | — |
| credit_note_id | uuid | No | — | FK → credit_notes: credit_note_items_credit_note_id_fkey | — | — |
| invoice_item_id | uuid | No | — | UNIQUE: credit_note_items_id_invoice_item_id_key; FK → invoice_items: credit_note_items_invoice_item_id_fkey | — | — |
| quantity | integer | Yes | — | — | — | — |
| amount_vnd | bigint | No | — | — | — | — |

### Constraints

**`credit_item_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`credit_note_items_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd >= 0)
```

**`credit_note_items_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`credit_note_items_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0 OR amount_vnd > 0)
```

**`credit_note_items_credit_note_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (credit_note_id) REFERENCES credit_notes(id)
```

**`credit_note_items_credit_note_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL credit_note_id
```

**`credit_note_items_id_invoice_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, invoice_item_id)
```

**`credit_note_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`credit_note_items_invoice_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (invoice_item_id) REFERENCES invoice_items(id)
```

**`credit_note_items_invoice_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL invoice_item_id
```

**`credit_note_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`credit_note_items_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`credit_positive_quantity_or_amount`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (COALESCE(quantity > 0, false) OR amount_vnd > 0)
```


### Indexes

**`credit_note_items_id_invoice_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_note_items_id_invoice_item_id_key ON public.credit_note_items USING btree (id, invoice_item_id)
```

**`credit_note_items_original_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX credit_note_items_original_idx ON public.credit_note_items USING btree (invoice_item_id)
```

**`credit_note_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_note_items_pkey ON public.credit_note_items USING btree (id)
```


### Triggers

**`credit_item_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER credit_item_totals AFTER INSERT OR DELETE OR UPDATE ON credit_note_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note()
```

**`credit_items_immutable`** — enabled `O`.

```sql
CREATE TRIGGER credit_items_immutable BEFORE INSERT OR DELETE OR UPDATE ON credit_note_items FOR EACH ROW EXECUTE FUNCTION guard_credit_child()
```


## credit_notes

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: credit_notes_id_order_id_key; PK: credit_notes_pkey | — | — |
| credit_note_no | text | No | — | UNIQUE: credit_notes_credit_note_no_key | — | — |
| invoice_id | uuid | No | — | FK → invoices: credit_notes_invoice_id_order_id_fkey | — | — |
| order_id | uuid | No | — | UNIQUE: credit_notes_id_order_id_key; FK → invoices: credit_notes_invoice_id_order_id_fkey; FK → return_requests: credit_notes_return_request_id_order_id_fkey; FK → shipments: credit_notes_shipment_id_order_id_fkey | — | — |
| status | text | No | — | — | — | — |
| issued_at | timestamp with time zone | Yes | — | — | — | — |
| return_request_id | uuid | Yes | — | FK → return_requests: credit_notes_return_request_id_order_id_fkey | — | — |
| warranty_claim_id | uuid | Yes | — | FK → warranty_claims: credit_notes_warranty_claim_id_fkey | — | — |
| shipment_id | uuid | Yes | — | FK → shipments: credit_notes_shipment_id_order_id_fkey | — | — |
| reason | text | No | — | — | — | — |
| total_vnd | bigint | No | — | — | — | — |
| approved_by_user_id | text | No | — | FK → user: credit_notes_approved_by_user_id_fkey | — | — |
| event_key | text | No | — | UNIQUE: credit_notes_event_key_key | — | — |

### Constraints

**`credit_notes_approved_by_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (approved_by_user_id) REFERENCES "user"(id)
```

**`credit_notes_approved_by_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL approved_by_user_id
```

**`credit_notes_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (num_nonnulls(return_request_id, warranty_claim_id, shipment_id) = 1)
```

**`credit_notes_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status <> 'issued'::text OR issued_at IS NOT NULL)
```

**`credit_notes_credit_note_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (credit_note_no)
```

**`credit_notes_credit_note_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL credit_note_no
```

**`credit_notes_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`credit_notes_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`credit_notes_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`credit_notes_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`credit_notes_invoice_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL invoice_id
```

**`credit_notes_invoice_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (invoice_id, order_id) REFERENCES invoices(id, order_id)
```

**`credit_notes_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`credit_notes_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`credit_notes_reason_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reason
```

**`credit_notes_return_request_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (return_request_id, order_id) REFERENCES return_requests(id, order_id)
```

**`credit_notes_shipment_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (shipment_id, order_id) REFERENCES shipments(id, order_id)
```

**`credit_notes_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['draft'::text, 'issued'::text]))
```

**`credit_notes_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`credit_notes_total_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (total_vnd >= 0)
```

**`credit_notes_total_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL total_vnd
```

**`credit_notes_warranty_claim_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (warranty_claim_id) REFERENCES warranty_claims(id)
```

**`credit_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`credit_notes_credit_note_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_notes_credit_note_no_key ON public.credit_notes USING btree (credit_note_no)
```

**`credit_notes_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_notes_event_key_key ON public.credit_notes USING btree (event_key)
```

**`credit_notes_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_notes_id_order_id_key ON public.credit_notes USING btree (id, order_id)
```

**`credit_notes_invoice_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX credit_notes_invoice_idx ON public.credit_notes USING btree (invoice_id, issued_at)
```

**`credit_notes_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX credit_notes_pkey ON public.credit_notes USING btree (id)
```


### Triggers

**`credit_note_immutable`** — enabled `O`.

```sql
CREATE TRIGGER credit_note_immutable BEFORE DELETE OR UPDATE ON credit_notes FOR EACH ROW EXECUTE FUNCTION guard_credit_note()
```

**`credit_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER credit_totals AFTER INSERT OR UPDATE ON credit_notes DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_credit_note()
```


## customer_profiles

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | PK: customer_profiles_pkey; FK → user: customer_profiles_user_id_fkey | — | — |
| display_name | text | No | — | — | — | — |
| phone | text | Yes | — | — | — | — |

### Constraints

**`customer_profiles_display_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL display_name
```

**`customer_profiles_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id)
```

**`customer_profiles_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`customer_profiles_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`customer_profiles_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX customer_profiles_pkey ON public.customer_profiles USING btree (user_id)
```


### Triggers

None.

## goods_receipt_items

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: goods_receipt_items_pkey | — | — |
| receipt_id | uuid | No | — | FK → goods_receipts: goods_receipt_items_receipt_id_purchase_order_id_fkey | — | — |
| purchase_order_id | uuid | No | — | FK → purchase_order_items: goods_receipt_items_purchase_order_item_id_purchase_order__fkey; FK → goods_receipts: goods_receipt_items_receipt_id_purchase_order_id_fkey | — | — |
| purchase_order_item_id | uuid | No | — | FK → purchase_order_items: goods_receipt_items_purchase_order_item_id_purchase_order__fkey | — | — |
| received_qty | integer | No | — | — | — | — |
| unit_cost_vnd | bigint | No | — | — | — | — |

### Constraints

**`goods_receipt_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`goods_receipt_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`goods_receipt_items_purchase_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL purchase_order_id
```

**`goods_receipt_items_purchase_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL purchase_order_item_id
```

**`goods_receipt_items_purchase_order_item_id_purchase_order__fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (purchase_order_item_id, purchase_order_id) REFERENCES purchase_order_items(id, purchase_order_id)
```

**`goods_receipt_items_receipt_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL receipt_id
```

**`goods_receipt_items_receipt_id_purchase_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (receipt_id, purchase_order_id) REFERENCES goods_receipts(id, purchase_order_id)
```

**`goods_receipt_items_received_qty_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (received_qty > 0)
```

**`goods_receipt_items_received_qty_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL received_qty
```

**`goods_receipt_items_unit_cost_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_cost_vnd >= 0)
```

**`goods_receipt_items_unit_cost_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_cost_vnd
```


### Indexes

**`goods_receipt_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX goods_receipt_items_pkey ON public.goods_receipt_items USING btree (id)
```


### Triggers

**`goods_receipt_item_immutable`** — enabled `O`.

```sql
CREATE TRIGGER goods_receipt_item_immutable BEFORE DELETE OR UPDATE ON goods_receipt_items FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```

**`receipt_item_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER receipt_item_source_guard BEFORE INSERT OR UPDATE ON goods_receipt_items FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources()
```


## goods_receipts

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: goods_receipts_id_purchase_order_id_key; PK: goods_receipts_pkey | — | — |
| purchase_order_id | uuid | No | — | UNIQUE: goods_receipts_id_purchase_order_id_key; FK → purchase_orders: goods_receipts_purchase_order_id_fkey | — | — |
| receipt_no | text | No | — | UNIQUE: goods_receipts_receipt_no_key | — | — |
| received_by | text | No | — | FK → user: goods_receipts_received_by_fkey | — | — |
| received_at | timestamp with time zone | No | now() | — | — | — |
| event_key | text | No | — | UNIQUE: goods_receipts_event_key_key | — | — |

### Constraints

**`goods_receipts_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`goods_receipts_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`goods_receipts_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`goods_receipts_id_purchase_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, purchase_order_id)
```

**`goods_receipts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`goods_receipts_purchase_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id)
```

**`goods_receipts_purchase_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL purchase_order_id
```

**`goods_receipts_receipt_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (receipt_no)
```

**`goods_receipts_receipt_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL receipt_no
```

**`goods_receipts_received_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL received_at
```

**`goods_receipts_received_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (received_by) REFERENCES "user"(id)
```

**`goods_receipts_received_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL received_by
```


### Indexes

**`goods_receipts_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX goods_receipts_event_key_key ON public.goods_receipts USING btree (event_key)
```

**`goods_receipts_id_purchase_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX goods_receipts_id_purchase_order_id_key ON public.goods_receipts USING btree (id, purchase_order_id)
```

**`goods_receipts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX goods_receipts_pkey ON public.goods_receipts USING btree (id)
```

**`goods_receipts_receipt_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX goods_receipts_receipt_no_key ON public.goods_receipts USING btree (receipt_no)
```


### Triggers

**`goods_receipt_immutable`** — enabled `O`.

```sql
CREATE TRIGGER goods_receipt_immutable BEFORE DELETE OR UPDATE ON goods_receipts FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## idempotency_records

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| scope | text | No | — | PK: idempotency_records_pkey | — | — |
| key | text | No | — | PK: idempotency_records_pkey | — | — |
| request_hash | text | No | — | — | — | — |
| result | jsonb | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`idempotency_records_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`idempotency_records_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL key
```

**`idempotency_records_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (scope, key)
```

**`idempotency_records_request_hash_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_hash
```

**`idempotency_records_result_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL result
```

**`idempotency_records_scope_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL scope
```


### Indexes

**`idempotency_records_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX idempotency_records_pkey ON public.idempotency_records USING btree (scope, key)
```


### Triggers

None.

## identity_role_claims

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | integer | No | — | PK: PK_identity_role_claims | d | — |
| role_id | text | No | — | FK → identity_roles: FK_identity_role_claims_identity_roles_role_id | — | — |
| claim_type | text | Yes | — | — | — | — |
| claim_value | text | Yes | — | — | — | — |

### Constraints

**`FK_identity_role_claims_identity_roles_role_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (role_id) REFERENCES identity_roles(id) ON DELETE CASCADE
```

**`PK_identity_role_claims`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`identity_role_claims_Id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`identity_role_claims_RoleId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL role_id
```


### Indexes

**`IX_identity_role_claims_role_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_identity_role_claims_role_id" ON public.identity_role_claims USING btree (role_id)
```

**`PK_identity_role_claims`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_role_claims" ON public.identity_role_claims USING btree (id)
```


### Triggers

None.

## identity_roles

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | text | No | — | PK: PK_identity_roles | — | — |
| name | character varying(256) | Yes | — | — | — | — |
| normalized_name | character varying(256) | Yes | — | — | — | — |
| concurrency_stamp | text | Yes | — | — | — | — |

### Constraints

**`PK_identity_roles`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`identity_roles_Id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```


### Indexes

**`PK_identity_roles`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_roles" ON public.identity_roles USING btree (id)
```

**`RoleNameIndex`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX "RoleNameIndex" ON public.identity_roles USING btree (normalized_name)
```


### Triggers

None.

## identity_user_claims

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | integer | No | — | PK: PK_identity_user_claims | d | — |
| user_id | text | No | — | FK → user: FK_identity_user_claims_user_user_id | — | — |
| claim_type | text | Yes | — | — | — | — |
| claim_value | text | Yes | — | — | — | — |

### Constraints

**`FK_identity_user_claims_user_user_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`PK_identity_user_claims`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`identity_user_claims_Id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`identity_user_claims_UserId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`IX_identity_user_claims_user_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_identity_user_claims_user_id" ON public.identity_user_claims USING btree (user_id)
```

**`PK_identity_user_claims`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_user_claims" ON public.identity_user_claims USING btree (id)
```


### Triggers

None.

## identity_user_logins

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| login_provider | text | No | — | PK: PK_identity_user_logins | — | — |
| provider_key | text | No | — | PK: PK_identity_user_logins | — | — |
| provider_display_name | text | Yes | — | — | — | — |
| user_id | text | No | — | FK → user: FK_identity_user_logins_user_user_id | — | — |

### Constraints

**`FK_identity_user_logins_user_user_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`PK_identity_user_logins`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (login_provider, provider_key)
```

**`identity_user_logins_LoginProvider_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL login_provider
```

**`identity_user_logins_ProviderKey_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL provider_key
```

**`identity_user_logins_UserId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`IX_identity_user_logins_user_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_identity_user_logins_user_id" ON public.identity_user_logins USING btree (user_id)
```

**`PK_identity_user_logins`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_user_logins" ON public.identity_user_logins USING btree (login_provider, provider_key)
```


### Triggers

None.

## identity_user_roles

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | FK → user: FK_identity_user_roles_user_user_id; PK: PK_identity_user_roles | — | — |
| role_id | text | No | — | FK → identity_roles: FK_identity_user_roles_identity_roles_role_id; PK: PK_identity_user_roles | — | — |

### Constraints

**`FK_identity_user_roles_identity_roles_role_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (role_id) REFERENCES identity_roles(id) ON DELETE CASCADE
```

**`FK_identity_user_roles_user_user_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`PK_identity_user_roles`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id, role_id)
```

**`identity_user_roles_RoleId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL role_id
```

**`identity_user_roles_UserId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`IX_identity_user_roles_role_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_identity_user_roles_role_id" ON public.identity_user_roles USING btree (role_id)
```

**`PK_identity_user_roles`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_user_roles" ON public.identity_user_roles USING btree (user_id, role_id)
```


### Triggers

None.

## identity_user_tokens

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | FK → user: FK_identity_user_tokens_user_user_id; PK: PK_identity_user_tokens | — | — |
| login_provider | text | No | — | PK: PK_identity_user_tokens | — | — |
| name | text | No | — | PK: PK_identity_user_tokens | — | — |
| value | text | Yes | — | — | — | — |

### Constraints

**`FK_identity_user_tokens_user_user_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`PK_identity_user_tokens`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id, login_provider, name)
```

**`identity_user_tokens_LoginProvider_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL login_provider
```

**`identity_user_tokens_Name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`identity_user_tokens_UserId_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`PK_identity_user_tokens`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_identity_user_tokens" ON public.identity_user_tokens USING btree (user_id, login_provider, name)
```


### Triggers

None.

## inventory_allocations

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: inventory_allocations_pkey | — | — |
| order_item_id | uuid | No | — | FK → order_items: inventory_allocations_order_item_id_fkey; FK → order_item_unit_amounts: inventory_allocations_order_item_unit_amount_id_order_item_fkey | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: inventory_allocations_order_item_unit_amount_id_order_item_fkey | — | — |
| lot_id | uuid | No | — | FK → inventory_units: inventory_allocations_inventory_unit_id_lot_id_fkey; FK → inventory_lots: inventory_allocations_lot_id_fkey | — | — |
| inventory_unit_id | uuid | Yes | — | FK → inventory_units: inventory_allocations_inventory_unit_id_lot_id_fkey | — | — |
| quantity | integer | No | — | — | — | — |
| cost_vnd | bigint | No | — | — | — | — |
| state | text | No | — | — | — | — |

### Constraints

**`inventory_allocations_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (inventory_unit_id IS NULL OR quantity = 1)
```

**`inventory_allocations_cost_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (cost_vnd >= 0)
```

**`inventory_allocations_cost_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL cost_vnd
```

**`inventory_allocations_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_allocations_inventory_unit_id_lot_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inventory_unit_id, lot_id) REFERENCES inventory_units(id, lot_id)
```

**`inventory_allocations_lot_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (lot_id) REFERENCES inventory_lots(id)
```

**`inventory_allocations_lot_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lot_id
```

**`inventory_allocations_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id) REFERENCES order_items(id)
```

**`inventory_allocations_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`inventory_allocations_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`inventory_allocations_order_item_unit_amount_id_order_item_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id, order_item_id) REFERENCES order_item_unit_amounts(id, order_item_id)
```

**`inventory_allocations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`inventory_allocations_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`inventory_allocations_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`inventory_allocations_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['reserved'::text, 'shipped'::text, 'returned'::text, 'released'::text, 'replaced'::text]))
```

**`inventory_allocations_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```


### Indexes

**`allocation_unit_active_idx`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX allocation_unit_active_idx ON public.inventory_allocations USING btree (order_item_unit_amount_id) WHERE (state = ANY (ARRAY['reserved'::text, 'shipped'::text]))
```

**`inventory_allocations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_allocations_pkey ON public.inventory_allocations USING btree (id)
```

**`inventory_unit_active_idx`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_unit_active_idx ON public.inventory_allocations USING btree (inventory_unit_id) WHERE ((inventory_unit_id IS NOT NULL) AND (state = ANY (ARRAY['reserved'::text, 'shipped'::text])))
```


### Triggers

**`allocation_history_guard`** — enabled `O`.

```sql
CREATE TRIGGER allocation_history_guard BEFORE DELETE OR UPDATE ON inventory_allocations FOR EACH ROW EXECUTE FUNCTION guard_inventory_history()
```

**`allocation_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER allocation_source_guard BEFORE INSERT OR UPDATE ON inventory_allocations FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources()
```


## inventory_identifiers

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: inventory_identifiers_pkey | — | — |
| inventory_unit_id | uuid | No | — | FK → inventory_units: inventory_identifiers_inventory_unit_id_fkey | — | — |
| kind | text | No | — | UNIQUE: inventory_identifiers_kind_normalized_value_key | — | — |
| normalized_value | text | No | — | UNIQUE: inventory_identifiers_kind_normalized_value_key | — | — |

### Constraints

**`inventory_identifiers_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_identifiers_inventory_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inventory_unit_id) REFERENCES inventory_units(id)
```

**`inventory_identifiers_inventory_unit_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL inventory_unit_id
```

**`inventory_identifiers_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['serial'::text, 'imei'::text]))
```

**`inventory_identifiers_kind_normalized_value_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (kind, normalized_value)
```

**`inventory_identifiers_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`inventory_identifiers_normalized_value_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL normalized_value
```

**`inventory_identifiers_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`inventory_identifiers_kind_normalized_value_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_identifiers_kind_normalized_value_key ON public.inventory_identifiers USING btree (kind, normalized_value)
```

**`inventory_identifiers_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_identifiers_pkey ON public.inventory_identifiers USING btree (id)
```


### Triggers

None.

## inventory_lots

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: inventory_lots_id_variant_id_key; PK: inventory_lots_pkey | — | — |
| warehouse_id | uuid | No | — | FK → warehouses: inventory_lots_warehouse_id_fkey | — | — |
| variant_id | uuid | No | — | UNIQUE: inventory_lots_id_variant_id_key; FK → product_variants: inventory_lots_variant_id_fkey | — | — |
| receipt_item_id | uuid | No | — | FK → goods_receipt_items: inventory_lots_receipt_item_id_fkey; UNIQUE: inventory_lots_receipt_item_id_key | — | — |
| received_at | timestamp with time zone | No | now() | — | — | — |
| unit_cost_vnd | bigint | No | — | — | — | — |
| qty_on_hand | integer | No | — | — | — | — |
| qty_reserved | integer | No | 0 | — | — | — |

### Constraints

**`inventory_lots_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (qty_reserved >= 0 AND qty_reserved <= qty_on_hand)
```

**`inventory_lots_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_lots_id_variant_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, variant_id)
```

**`inventory_lots_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`inventory_lots_qty_on_hand_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (qty_on_hand >= 0)
```

**`inventory_lots_qty_on_hand_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL qty_on_hand
```

**`inventory_lots_qty_reserved_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL qty_reserved
```

**`inventory_lots_receipt_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (receipt_item_id) REFERENCES goods_receipt_items(id)
```

**`inventory_lots_receipt_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (receipt_item_id)
```

**`inventory_lots_receipt_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL receipt_item_id
```

**`inventory_lots_received_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL received_at
```

**`inventory_lots_unit_cost_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_cost_vnd >= 0)
```

**`inventory_lots_unit_cost_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_cost_vnd
```

**`inventory_lots_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`inventory_lots_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```

**`inventory_lots_warehouse_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (warehouse_id) REFERENCES warehouses(id)
```

**`inventory_lots_warehouse_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL warehouse_id
```


### Indexes

**`inventory_lots_id_variant_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_lots_id_variant_id_key ON public.inventory_lots USING btree (id, variant_id)
```

**`inventory_lots_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_lots_pkey ON public.inventory_lots USING btree (id)
```

**`inventory_lots_receipt_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_lots_receipt_item_id_key ON public.inventory_lots USING btree (receipt_item_id)
```

**`lots_fifo_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX lots_fifo_idx ON public.inventory_lots USING btree (variant_id, received_at, id)
```


### Triggers

**`lot_history_guard`** — enabled `O`.

```sql
CREATE TRIGGER lot_history_guard BEFORE DELETE OR UPDATE ON inventory_lots FOR EACH ROW EXECUTE FUNCTION guard_inventory_history()
```

**`lot_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER lot_source_guard BEFORE INSERT OR UPDATE ON inventory_lots FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources()
```


## inventory_quarantine

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: PK_inventory_quarantine | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: FK_inventory_quarantine_order_item_unit_amounts_order_item_uni~ | — | — |
| lot_id | uuid | No | — | FK → inventory_lots: FK_inventory_quarantine_inventory_lots_lot_id | — | — |
| quantity | integer | No | — | — | — | — |
| state | text | No | — | — | — | — |
| reason | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`FK_inventory_quarantine_inventory_lots_lot_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (lot_id) REFERENCES inventory_lots(id) ON DELETE RESTRICT
```

**`FK_inventory_quarantine_order_item_unit_amounts_order_item_uni~`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id) ON DELETE RESTRICT
```

**`PK_inventory_quarantine`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`inventory_quarantine_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`inventory_quarantine_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_quarantine_lot_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lot_id
```

**`inventory_quarantine_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`inventory_quarantine_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`inventory_quarantine_reason_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reason
```

**`inventory_quarantine_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```

**`quarantine_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity = 1)
```

**`quarantine_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['quarantine'::text, 'service'::text, 'released'::text, 'disposed'::text]))
```


### Indexes

**`IX_inventory_quarantine_lot_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_inventory_quarantine_lot_id" ON public.inventory_quarantine USING btree (lot_id)
```

**`IX_inventory_quarantine_order_item_unit_amount_id_lot_id`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX "IX_inventory_quarantine_order_item_unit_amount_id_lot_id" ON public.inventory_quarantine USING btree (order_item_unit_amount_id, lot_id)
```

**`PK_inventory_quarantine`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_inventory_quarantine" ON public.inventory_quarantine USING btree (id)
```


### Triggers

**`quarantine_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER quarantine_source_guard BEFORE INSERT OR UPDATE ON inventory_quarantine FOR EACH ROW EXECUTE FUNCTION guard_quarantine_source()
```


## inventory_reservations

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: inventory_reservations_pkey | — | — |
| order_item_id | uuid | No | — | FK → order_items: inventory_reservations_order_item_id_fkey | — | — |
| variant_id | uuid | No | — | FK → product_variants: inventory_reservations_variant_id_fkey | — | — |
| quantity | integer | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| status | text | No | — | — | — | — |

### Constraints

**`inventory_reservations_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`inventory_reservations_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_reservations_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id) REFERENCES order_items(id)
```

**`inventory_reservations_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`inventory_reservations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`inventory_reservations_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`inventory_reservations_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`inventory_reservations_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['active'::text, 'released'::text, 'consumed'::text, 'expired'::text]))
```

**`inventory_reservations_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`inventory_reservations_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`inventory_reservations_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```


### Indexes

**`inventory_reservations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_reservations_pkey ON public.inventory_reservations USING btree (id)
```

**`reservations_expiry_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX reservations_expiry_idx ON public.inventory_reservations USING btree (status, expires_at)
```


### Triggers

**`reservation_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER reservation_source_guard BEFORE INSERT OR UPDATE ON inventory_reservations FOR EACH ROW EXECUTE FUNCTION guard_inventory_sources()
```


## inventory_units

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: inventory_units_id_lot_id_key; PK: inventory_units_pkey | — | — |
| lot_id | uuid | No | — | UNIQUE: inventory_units_id_lot_id_key; FK → inventory_lots: inventory_units_lot_id_fkey | — | — |
| condition_status | text | No | 'new'::text | — | — | — |
| custody_status | text | No | 'available'::text | — | — | — |

### Constraints

**`inventory_units_condition_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (condition_status = ANY (ARRAY['new'::text, 'used'::text, 'faulty'::text, 'opened'::text]))
```

**`inventory_units_condition_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL condition_status
```

**`inventory_units_custody_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (custody_status = ANY (ARRAY['available'::text, 'reserved'::text, 'shipped'::text, 'quarantine'::text, 'service'::text]))
```

**`inventory_units_custody_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL custody_status
```

**`inventory_units_id_lot_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, lot_id)
```

**`inventory_units_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`inventory_units_lot_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (lot_id) REFERENCES inventory_lots(id)
```

**`inventory_units_lot_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lot_id
```

**`inventory_units_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`inventory_units_id_lot_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_units_id_lot_id_key ON public.inventory_units USING btree (id, lot_id)
```

**`inventory_units_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX inventory_units_pkey ON public.inventory_units USING btree (id)
```


### Triggers

None.

## invoice_exports

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: invoice_exports_pkey | — | — |
| requested_by_user_id | text | No | — | FK → user: invoice_exports_requested_by_user_id_fkey | — | — |
| query_json | jsonb | No | — | — | — | — |
| read_at | timestamp with time zone | No | — | — | — | — |
| row_count | integer | No | — | — | — | — |
| total_vnd | bigint | No | — | — | — | — |
| status | text | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| file_manifest | jsonb | No | '{}'::jsonb | — | — | — |
| role_snapshot | text | No | — | — | — | — |

### Constraints

**`invoice_exports_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`invoice_exports_file_manifest_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL file_manifest
```

**`invoice_exports_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`invoice_exports_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`invoice_exports_query_json_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL query_json
```

**`invoice_exports_read_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL read_at
```

**`invoice_exports_requested_by_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (requested_by_user_id) REFERENCES "user"(id)
```

**`invoice_exports_requested_by_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL requested_by_user_id
```

**`invoice_exports_role_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL role_snapshot
```

**`invoice_exports_row_count_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (row_count >= 0)
```

**`invoice_exports_row_count_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL row_count
```

**`invoice_exports_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['building'::text, 'ready'::text, 'failed'::text]))
```

**`invoice_exports_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`invoice_exports_total_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL total_vnd
```


### Indexes

**`invoice_exports_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX invoice_exports_pkey ON public.invoice_exports USING btree (id)
```


### Triggers

None.

## invoice_items

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: invoice_items_id_invoice_id_key; PK: invoice_items_pkey | — | — |
| invoice_id | uuid | No | — | UNIQUE: invoice_items_id_invoice_id_key; UNIQUE: invoice_items_invoice_id_line_no_key; FK → invoices: invoice_items_invoice_id_order_id_fkey; FK → invoice_items: invoice_items_parent_invoice_item_id_invoice_id_fkey | — | — |
| order_id | uuid | No | — | FK → invoices: invoice_items_invoice_id_order_id_fkey; FK → order_items: invoice_items_order_item_id_order_id_fkey | — | — |
| order_item_id | uuid | No | — | UNIQUE: invoice_items_order_item_id_key; FK → order_items: invoice_items_order_item_id_order_id_fkey | — | — |
| line_no | integer | No | — | UNIQUE: invoice_items_invoice_id_line_no_key | — | — |
| line_type | text | No | — | — | — | — |
| product_id | uuid | Yes | — | FK → products: invoice_items_product_id_fkey; FK → product_variants: invoice_items_variant_id_product_id_fkey | — | — |
| variant_id | uuid | Yes | — | FK → product_variants: invoice_items_variant_id_product_id_fkey | — | — |
| parent_invoice_item_id | uuid | Yes | — | FK → invoice_items: invoice_items_parent_invoice_item_id_invoice_id_fkey | — | — |
| sku_snapshot | text | No | — | — | — | — |
| name_snapshot | text | No | — | — | — | — |
| variant_snapshot | jsonb | No | — | — | — | — |
| quantity | integer | No | — | — | — | — |
| unit_price_vnd | bigint | No | — | — | — | — |
| voucher_discount_vnd | bigint | No | — | — | — | — |
| points_discount_vnd | bigint | No | — | — | — | — |
| net_amount_vnd | bigint | No | — | — | — | — |

### Constraints

**`invoice_items_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (net_amount_vnd >= 0 AND net_amount_vnd = (quantity::bigint * unit_price_vnd - voucher_discount_vnd - points_discount_vnd))
```

**`invoice_items_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type <> 'product'::text OR product_id IS NOT NULL AND variant_id IS NOT NULL)
```

**`invoice_items_check2`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK ((line_type = 'warranty'::text) = (parent_invoice_item_id IS NOT NULL))
```

**`invoice_items_check3`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type <> 'shipping'::text OR quantity = 1)
```

**`invoice_items_id_invoice_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, invoice_id)
```

**`invoice_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`invoice_items_invoice_id_line_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (invoice_id, line_no)
```

**`invoice_items_invoice_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL invoice_id
```

**`invoice_items_invoice_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (invoice_id, order_id) REFERENCES invoices(id, order_id)
```

**`invoice_items_line_no_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_no > 0)
```

**`invoice_items_line_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL line_no
```

**`invoice_items_line_type_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type = ANY (ARRAY['product'::text, 'warranty'::text, 'shipping'::text]))
```

**`invoice_items_line_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL line_type
```

**`invoice_items_name_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name_snapshot
```

**`invoice_items_net_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL net_amount_vnd
```

**`invoice_items_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`invoice_items_order_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_item_id)
```

**`invoice_items_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`invoice_items_order_item_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id, order_id) REFERENCES order_items(id, order_id)
```

**`invoice_items_parent_invoice_item_id_invoice_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (parent_invoice_item_id, invoice_id) REFERENCES invoice_items(id, invoice_id)
```

**`invoice_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`invoice_items_points_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (points_discount_vnd >= 0)
```

**`invoice_items_points_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL points_discount_vnd
```

**`invoice_items_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`invoice_items_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`invoice_items_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`invoice_items_sku_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL sku_snapshot
```

**`invoice_items_unit_price_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_price_vnd >= 0)
```

**`invoice_items_unit_price_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_price_vnd
```

**`invoice_items_variant_id_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id, product_id) REFERENCES product_variants(id, product_id)
```

**`invoice_items_variant_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_snapshot
```

**`invoice_items_voucher_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (voucher_discount_vnd >= 0)
```

**`invoice_items_voucher_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL voucher_discount_vnd
```

**`invoice_lines_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`invoice_items_id_invoice_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoice_items_id_invoice_id_key ON public.invoice_items USING btree (id, invoice_id)
```

**`invoice_items_invoice_id_line_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoice_items_invoice_id_line_no_key ON public.invoice_items USING btree (invoice_id, line_no)
```

**`invoice_items_invoice_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoice_items_invoice_idx ON public.invoice_items USING btree (invoice_id)
```

**`invoice_items_order_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoice_items_order_item_id_key ON public.invoice_items USING btree (order_item_id)
```

**`invoice_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX invoice_items_pkey ON public.invoice_items USING btree (id)
```

**`invoice_items_product_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoice_items_product_idx ON public.invoice_items USING btree (product_id, invoice_id)
```

**`invoice_items_variant_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoice_items_variant_idx ON public.invoice_items USING btree (variant_id, invoice_id)
```


### Triggers

**`invoice_item_immutable`** — enabled `O`.

```sql
CREATE TRIGGER invoice_item_immutable BEFORE INSERT OR DELETE OR UPDATE ON invoice_items FOR EACH ROW EXECUTE FUNCTION guard_invoice_item()
```

**`invoice_lines_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER invoice_lines_totals AFTER INSERT OR DELETE OR UPDATE ON invoice_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_invoice_totals()
```


## invoices

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: invoices_id_order_id_key; PK: invoices_pkey | — | — |
| invoice_no | text | Yes | — | UNIQUE: invoices_invoice_no_key | — | — |
| order_id | uuid | No | — | UNIQUE: invoices_id_order_id_key; FK → orders: invoices_order_id_customer_id_fkey; FK → orders: invoices_order_id_fkey; UNIQUE: invoices_order_id_key | — | — |
| lifecycle | text | No | — | — | — | — |
| issued_at | timestamp with time zone | Yes | — | — | — | — |
| customer_id | text | No | — | FK → user: invoices_customer_id_fkey; FK → orders: invoices_order_id_customer_id_fkey | — | — |
| responsible_staff_id | text | Yes | — | FK → staff_profiles: invoices_responsible_staff_id_fkey | — | — |
| issued_by_user_id | text | Yes | — | FK → user: invoices_issued_by_user_id_fkey | — | — |
| currency | text | No | 'VND'::text | — | — | — |
| subtotal_vnd | bigint | No | — | — | — | — |
| discount_total_vnd | bigint | No | — | — | — | — |
| grand_total_vnd | bigint | No | — | — | — | — |
| customer_snapshot | jsonb | No | — | — | — | — |
| staff_snapshot | jsonb | No | — | — | — | — |

### Constraints

**`invoice_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`invoices_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (grand_total_vnd >= 0 AND grand_total_vnd = (subtotal_vnd - discount_total_vnd))
```

**`invoices_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (lifecycle <> 'issued'::text OR invoice_no IS NOT NULL AND issued_at IS NOT NULL AND responsible_staff_id IS NOT NULL AND issued_by_user_id IS NOT NULL)
```

**`invoices_currency_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (currency = 'VND'::text)
```

**`invoices_currency_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL currency
```

**`invoices_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`invoices_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`invoices_customer_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_snapshot
```

**`invoices_discount_total_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (discount_total_vnd >= 0)
```

**`invoices_discount_total_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL discount_total_vnd
```

**`invoices_grand_total_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL grand_total_vnd
```

**`invoices_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`invoices_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`invoices_invoice_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (invoice_no)
```

**`invoices_issued_by_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (issued_by_user_id) REFERENCES "user"(id)
```

**`invoices_lifecycle_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (lifecycle = ANY (ARRAY['draft'::text, 'issued'::text]))
```

**`invoices_lifecycle_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lifecycle
```

**`invoices_order_id_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id, customer_id) REFERENCES orders(id, customer_id)
```

**`invoices_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`invoices_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_id)
```

**`invoices_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`invoices_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`invoices_responsible_staff_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (responsible_staff_id) REFERENCES staff_profiles(user_id)
```

**`invoices_staff_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL staff_snapshot
```

**`invoices_subtotal_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (subtotal_vnd >= 0)
```

**`invoices_subtotal_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL subtotal_vnd
```


### Indexes

**`invoices_customer_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoices_customer_idx ON public.invoices USING btree (customer_id, issued_at, id)
```

**`invoices_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoices_id_order_id_key ON public.invoices USING btree (id, order_id)
```

**`invoices_invoice_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoices_invoice_no_key ON public.invoices USING btree (invoice_no)
```

**`invoices_issued_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoices_issued_idx ON public.invoices USING btree (issued_at, id) WHERE (lifecycle = 'issued'::text)
```

**`invoices_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX invoices_order_id_key ON public.invoices USING btree (order_id)
```

**`invoices_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX invoices_pkey ON public.invoices USING btree (id)
```

**`invoices_staff_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX invoices_staff_idx ON public.invoices USING btree (responsible_staff_id, issued_at, id)
```


### Triggers

**`invoice_immutable`** — enabled `O`.

```sql
CREATE TRIGGER invoice_immutable BEFORE DELETE OR UPDATE ON invoices FOR EACH ROW EXECUTE FUNCTION guard_invoice()
```

**`invoice_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER invoice_totals AFTER INSERT OR UPDATE ON invoices DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_invoice_totals()
```


## landing_drafts

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| page_id | uuid | No | — | FK → landing_pages: landing_drafts_page_id_fkey; PK: landing_drafts_pkey | — | — |
| revision | integer | No | 0 | — | — | — |
| sections | jsonb | No | — | — | — | — |
| updated_by | text | Yes | — | FK → user: landing_drafts_updated_by_fkey | — | — |
| updated_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`landing_drafts_page_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (page_id) REFERENCES landing_pages(id)
```

**`landing_drafts_page_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL page_id
```

**`landing_drafts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (page_id)
```

**`landing_drafts_revision_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL revision
```

**`landing_drafts_sections_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL sections
```

**`landing_drafts_updated_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL updated_at
```

**`landing_drafts_updated_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (updated_by) REFERENCES "user"(id)
```


### Indexes

**`landing_drafts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_drafts_pkey ON public.landing_drafts USING btree (page_id)
```


### Triggers

**`landing_draft_permission`** — enabled `O`.

```sql
CREATE TRIGGER landing_draft_permission BEFORE INSERT OR DELETE OR UPDATE ON landing_drafts FOR EACH ROW EXECUTE FUNCTION cms_permission()
```


## landing_media_refs

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| landing_version_id | uuid | No | — | FK → landing_versions: landing_media_refs_landing_version_id_fkey; PK: landing_media_refs_pkey | — | — |
| media_id | uuid | No | — | FK → media_assets: landing_media_refs_media_id_fkey; PK: landing_media_refs_pkey | — | — |

### Constraints

**`landing_media_refs_landing_version_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (landing_version_id) REFERENCES landing_versions(id)
```

**`landing_media_refs_landing_version_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL landing_version_id
```

**`landing_media_refs_media_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (media_id) REFERENCES media_assets(id)
```

**`landing_media_refs_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL media_id
```

**`landing_media_refs_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (landing_version_id, media_id)
```


### Indexes

**`landing_media_refs_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_media_refs_pkey ON public.landing_media_refs USING btree (landing_version_id, media_id)
```


### Triggers

None.

## landing_pages

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: landing_pages_pkey | — | — |
| slug | text | No | — | UNIQUE: landing_pages_slug_key | — | — |

### Constraints

**`landing_pages_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`landing_pages_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`landing_pages_slug_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (slug)
```

**`landing_pages_slug_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL slug
```


### Indexes

**`landing_pages_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_pages_pkey ON public.landing_pages USING btree (id)
```

**`landing_pages_slug_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_pages_slug_key ON public.landing_pages USING btree (slug)
```


### Triggers

None.

## landing_preview_grants

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | — | PK: PK_landing_preview_grants | — | — |
| code_hash | text | No | — | — | — | — |
| page_id | uuid | No | — | FK → landing_drafts: FK_landing_preview_grants_landing_drafts_page_id; FK → landing_pages: FK_landing_preview_grants_landing_pages_page_id | — | — |
| draft_revision | integer | No | — | — | — | — |
| version_id | uuid | Yes | — | FK → landing_versions: FK_landing_preview_grants_landing_versions_version_id | — | — |
| issued_by | text | No | — | FK → user: FK_landing_preview_grants_user_issued_by | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| redeemed_at | timestamp with time zone | Yes | — | — | — | — |
| revoked_at | timestamp with time zone | Yes | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| sections_snapshot | jsonb | No | '[]'::jsonb | — | — | — |
| session_hash | text | Yes | — | — | — | — |

### Constraints

**`FK_landing_preview_grants_landing_drafts_page_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (page_id) REFERENCES landing_drafts(page_id) ON DELETE RESTRICT
```

**`FK_landing_preview_grants_landing_pages_page_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (page_id) REFERENCES landing_pages(id) ON DELETE RESTRICT
```

**`FK_landing_preview_grants_landing_versions_version_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (version_id) REFERENCES landing_versions(id) ON DELETE RESTRICT
```

**`FK_landing_preview_grants_user_issued_by`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (issued_by) REFERENCES "user"(id) ON DELETE RESTRICT
```

**`PK_landing_preview_grants`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`landing_preview_grants_code_hash_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code_hash
```

**`landing_preview_grants_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`landing_preview_grants_draft_revision_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL draft_revision
```

**`landing_preview_grants_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`landing_preview_grants_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`landing_preview_grants_issued_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL issued_by
```

**`landing_preview_grants_page_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL page_id
```

**`landing_preview_grants_sections_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL sections_snapshot
```


### Indexes

**`IX_landing_preview_grants_code_hash`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX "IX_landing_preview_grants_code_hash" ON public.landing_preview_grants USING btree (code_hash)
```

**`IX_landing_preview_grants_issued_by`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_landing_preview_grants_issued_by" ON public.landing_preview_grants USING btree (issued_by)
```

**`IX_landing_preview_grants_page_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_landing_preview_grants_page_id" ON public.landing_preview_grants USING btree (page_id)
```

**`IX_landing_preview_grants_session_hash`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX "IX_landing_preview_grants_session_hash" ON public.landing_preview_grants USING btree (session_hash)
```

**`IX_landing_preview_grants_version_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_landing_preview_grants_version_id" ON public.landing_preview_grants USING btree (version_id)
```

**`PK_landing_preview_grants`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX "PK_landing_preview_grants" ON public.landing_preview_grants USING btree (id)
```


### Triggers

None.

## landing_publications

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: landing_publications_pkey | — | — |
| page_id | uuid | No | — | FK → landing_versions: landing_publications_version_id_page_id_fkey | — | — |
| version_id | uuid | No | — | FK → landing_versions: landing_publications_version_id_page_id_fkey | — | — |
| published_by | text | No | — | FK → user: landing_publications_published_by_fkey | — | — |
| published_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`landing_publications_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`landing_publications_page_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL page_id
```

**`landing_publications_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`landing_publications_published_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL published_at
```

**`landing_publications_published_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (published_by) REFERENCES "user"(id)
```

**`landing_publications_published_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL published_by
```

**`landing_publications_version_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_id
```

**`landing_publications_version_id_page_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (version_id, page_id) REFERENCES landing_versions(id, page_id)
```


### Indexes

**`landing_publications_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_publications_pkey ON public.landing_publications USING btree (id)
```


### Triggers

**`landing_publication_permission`** — enabled `O`.

```sql
CREATE TRIGGER landing_publication_permission BEFORE INSERT ON landing_publications FOR EACH ROW EXECUTE FUNCTION cms_permission()
```

**`publication_append_only`** — enabled `O`.

```sql
CREATE TRIGGER publication_append_only BEFORE DELETE OR UPDATE ON landing_publications FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## landing_versions

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: landing_versions_id_page_id_key; PK: landing_versions_pkey | — | — |
| page_id | uuid | No | — | UNIQUE: landing_versions_id_page_id_key; FK → landing_pages: landing_versions_page_id_fkey; UNIQUE: landing_versions_page_id_version_no_key | — | — |
| version_no | integer | No | — | UNIQUE: landing_versions_page_id_version_no_key | — | — |
| sections | jsonb | No | — | — | — | — |
| created_by | text | No | — | FK → user: landing_versions_created_by_fkey | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`landing_versions_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`landing_versions_created_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (created_by) REFERENCES "user"(id)
```

**`landing_versions_created_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_by
```

**`landing_versions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`landing_versions_id_page_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, page_id)
```

**`landing_versions_page_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (page_id) REFERENCES landing_pages(id)
```

**`landing_versions_page_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL page_id
```

**`landing_versions_page_id_version_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (page_id, version_no)
```

**`landing_versions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`landing_versions_sections_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL sections
```

**`landing_versions_version_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_no
```


### Indexes

**`landing_versions_id_page_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_versions_id_page_id_key ON public.landing_versions USING btree (id, page_id)
```

**`landing_versions_page_id_version_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_versions_page_id_version_no_key ON public.landing_versions USING btree (page_id, version_no)
```

**`landing_versions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX landing_versions_pkey ON public.landing_versions USING btree (id)
```


### Triggers

**`landing_version_immutable`** — enabled `O`.

```sql
CREATE TRIGGER landing_version_immutable BEFORE DELETE OR UPDATE ON landing_versions FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```

**`landing_version_permission`** — enabled `O`.

```sql
CREATE TRIGGER landing_version_permission BEFORE INSERT ON landing_versions FOR EACH ROW EXECUTE FUNCTION cms_permission()
```


## loyalty_accounts

Domain: Customer ledgers.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | PK: loyalty_accounts_pkey; FK → user: loyalty_accounts_user_id_fkey | — | — |
| balance_points | integer | No | 0 | — | — | — |
| net_spend_vnd | bigint | No | 0 | — | — | — |

### Constraints

**`loyalty_accounts_balance_points_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL balance_points
```

**`loyalty_accounts_net_spend_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (net_spend_vnd >= 0)
```

**`loyalty_accounts_net_spend_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL net_spend_vnd
```

**`loyalty_accounts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id)
```

**`loyalty_accounts_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`loyalty_accounts_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`loyalty_accounts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX loyalty_accounts_pkey ON public.loyalty_accounts USING btree (user_id)
```


### Triggers

None.

## loyalty_entries

Domain: Customer ledgers.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: loyalty_entries_pkey | — | — |
| user_id | text | No | — | FK → loyalty_accounts: loyalty_entries_user_id_fkey | — | — |
| delta | integer | No | — | — | — | — |
| kind | text | No | — | — | — | — |
| order_id | uuid | No | — | FK → orders: loyalty_entries_order_id_fkey | — | — |
| unit_id | uuid | Yes | — | FK → order_item_unit_amounts: loyalty_entries_unit_id_fkey | — | — |
| event_key | text | No | — | UNIQUE: loyalty_entries_event_key_key | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`loyalty_entries_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`loyalty_entries_delta_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (delta <> 0)
```

**`loyalty_entries_delta_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL delta
```

**`loyalty_entries_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`loyalty_entries_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`loyalty_entries_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`loyalty_entries_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['earn'::text, 'redeem'::text, 'restore'::text, 'reverse'::text]))
```

**`loyalty_entries_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`loyalty_entries_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`loyalty_entries_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`loyalty_entries_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`loyalty_entries_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (unit_id) REFERENCES order_item_unit_amounts(id)
```

**`loyalty_entries_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES loyalty_accounts(user_id)
```

**`loyalty_entries_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```

**`loyalty_ledger_source`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`loyalty_entries_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX loyalty_entries_event_key_key ON public.loyalty_entries USING btree (event_key)
```

**`loyalty_entries_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX loyalty_entries_pkey ON public.loyalty_entries USING btree (id)
```


### Triggers

**`loyalty_append_only`** — enabled `O`.

```sql
CREATE TRIGGER loyalty_append_only BEFORE DELETE OR UPDATE ON loyalty_entries FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```

**`loyalty_ledger_source`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER loyalty_ledger_source AFTER INSERT ON loyalty_entries DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_ledger_source()
```


## media_assets

Domain: Landing & media.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: media_assets_pkey | — | — |
| storage_key | text | No | — | UNIQUE: media_assets_storage_key_key | — | — |
| visibility | text | No | — | — | — | — |
| mime | text | No | — | — | — | — |
| size_bytes | bigint | No | — | — | — | — |
| checksum | text | No | — | — | — | — |
| validation_status | text | No | — | — | — | — |
| source_url | text | Yes | — | — | — | — |
| license_note | text | Yes | — | — | — | — |
| owner_user_id | text | Yes | — | FK → user: media_assets_owner_user_id_fkey | — | — |
| metadata | jsonb | No | '{}'::jsonb | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`media_assets_checksum_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL checksum
```

**`media_assets_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`media_assets_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`media_assets_metadata_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL metadata
```

**`media_assets_mime_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL mime
```

**`media_assets_owner_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (owner_user_id) REFERENCES "user"(id)
```

**`media_assets_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`media_assets_size_bytes_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (size_bytes > 0)
```

**`media_assets_size_bytes_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL size_bytes
```

**`media_assets_storage_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (storage_key)
```

**`media_assets_storage_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL storage_key
```

**`media_assets_validation_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (validation_status = ANY (ARRAY['valid'::text, 'invalid'::text, 'pending'::text]))
```

**`media_assets_validation_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL validation_status
```

**`media_assets_visibility_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (visibility = ANY (ARRAY['public'::text, 'private'::text, 'staging'::text]))
```

**`media_assets_visibility_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL visibility
```


### Indexes

**`media_assets_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX media_assets_pkey ON public.media_assets USING btree (id)
```

**`media_assets_storage_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX media_assets_storage_key_key ON public.media_assets USING btree (storage_key)
```


### Triggers

None.

## notifications

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: notifications_pkey | — | — |
| recipient_user_id | text | No | — | FK → user: notifications_recipient_user_id_fkey | — | — |
| type | text | No | — | — | — | — |
| message | text | No | — | — | — | — |
| entity_id | uuid | Yes | — | — | — | — |
| read_at | timestamp with time zone | Yes | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`notifications_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`notifications_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`notifications_message_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL message
```

**`notifications_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`notifications_recipient_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (recipient_user_id) REFERENCES "user"(id)
```

**`notifications_recipient_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL recipient_user_id
```

**`notifications_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL type
```


### Indexes

**`notifications_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX notifications_pkey ON public.notifications USING btree (id)
```


### Triggers

None.

## order_adjustments

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: order_adjustments_pkey | — | — |
| order_item_id | uuid | No | — | FK → order_items: order_adjustments_order_item_id_fkey | — | — |
| kind | text | No | — | — | — | — |
| promotion_redemption_id | uuid | Yes | — | FK → promotion_redemptions: order_adjustments_promotion_redemption_id_fkey | — | — |
| amount_vnd | bigint | No | — | — | — | — |
| source_event_key | text | No | — | UNIQUE: order_adjustments_source_event_key_key | — | — |

### Constraints

**`order_adjustments_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd >= 0)
```

**`order_adjustments_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`order_adjustments_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`order_adjustments_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['voucher'::text, 'points'::text]))
```

**`order_adjustments_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`order_adjustments_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id) REFERENCES order_items(id)
```

**`order_adjustments_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`order_adjustments_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`order_adjustments_promotion_redemption_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (promotion_redemption_id) REFERENCES promotion_redemptions(id)
```

**`order_adjustments_source_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (source_event_key)
```

**`order_adjustments_source_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL source_event_key
```


### Indexes

**`order_adjustments_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX order_adjustments_pkey ON public.order_adjustments USING btree (id)
```

**`order_adjustments_source_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_adjustments_source_event_key_key ON public.order_adjustments USING btree (source_event_key)
```


### Triggers

**`order_adjustment_immutable`** — enabled `O`.

```sql
CREATE TRIGGER order_adjustment_immutable BEFORE DELETE OR UPDATE ON order_adjustments FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## order_item_unit_amounts

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: order_item_unit_amounts_id_order_item_id_key; PK: order_item_unit_amounts_pkey | — | — |
| order_item_id | uuid | No | — | UNIQUE: order_item_unit_amounts_id_order_item_id_key; FK → order_items: order_item_unit_amounts_order_item_id_fkey; UNIQUE: order_item_unit_amounts_order_item_id_unit_ordinal_key | — | — |
| unit_ordinal | integer | No | — | UNIQUE: order_item_unit_amounts_order_item_id_unit_ordinal_key | — | — |
| parent_unit_amount_id | uuid | Yes | — | FK → order_item_unit_amounts: order_item_unit_amounts_parent_unit_amount_id_fkey | — | — |
| gross_vnd | bigint | No | — | — | — | — |
| voucher_discount_vnd | bigint | No | — | — | — | — |
| redeemed_points | integer | No | 0 | — | — | — |
| points_discount_vnd | bigint | No | — | — | — | — |
| net_amount_vnd | bigint | No | — | — | — | — |
| credit_planned_vnd | bigint | No | — | — | — | — |
| cash_planned_vnd | bigint | No | — | — | — | — |
| point_value_vnd | bigint | No | 1000 | — | — | — |

### Constraints

**`order_item_unit_amounts_cash_planned_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL cash_planned_vnd
```

**`order_item_unit_amounts_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (net_amount_vnd >= 0 AND net_amount_vnd = (gross_vnd - voucher_discount_vnd - points_discount_vnd))
```

**`order_item_unit_amounts_check2`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (cash_planned_vnd >= 0 AND (cash_planned_vnd + credit_planned_vnd) = net_amount_vnd)
```

**`order_item_unit_amounts_credit_planned_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (credit_planned_vnd >= 0)
```

**`order_item_unit_amounts_credit_planned_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL credit_planned_vnd
```

**`order_item_unit_amounts_gross_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (gross_vnd >= 0)
```

**`order_item_unit_amounts_gross_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL gross_vnd
```

**`order_item_unit_amounts_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`order_item_unit_amounts_id_order_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_item_id)
```

**`order_item_unit_amounts_net_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL net_amount_vnd
```

**`order_item_unit_amounts_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id) REFERENCES order_items(id)
```

**`order_item_unit_amounts_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`order_item_unit_amounts_order_item_id_unit_ordinal_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_item_id, unit_ordinal)
```

**`order_item_unit_amounts_parent_unit_amount_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (parent_unit_amount_id) REFERENCES order_item_unit_amounts(id)
```

**`order_item_unit_amounts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`order_item_unit_amounts_point_value_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL point_value_vnd
```

**`order_item_unit_amounts_points_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL points_discount_vnd
```

**`order_item_unit_amounts_redeemed_points_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (redeemed_points >= 0)
```

**`order_item_unit_amounts_redeemed_points_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL redeemed_points
```

**`order_item_unit_amounts_unit_ordinal_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_ordinal > 0)
```

**`order_item_unit_amounts_unit_ordinal_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_ordinal
```

**`order_item_unit_amounts_voucher_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (voucher_discount_vnd >= 0)
```

**`order_item_unit_amounts_voucher_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL voucher_discount_vnd
```

**`order_units_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`point_value_snapshot_positive`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (point_value_vnd > 0)
```

**`points_snapshot_amount`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (points_discount_vnd = (redeemed_points::bigint * point_value_vnd))
```


### Indexes

**`order_item_unit_amounts_id_order_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_item_unit_amounts_id_order_item_id_key ON public.order_item_unit_amounts USING btree (id, order_item_id)
```

**`order_item_unit_amounts_order_item_id_unit_ordinal_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_item_unit_amounts_order_item_id_unit_ordinal_key ON public.order_item_unit_amounts USING btree (order_item_id, unit_ordinal)
```

**`order_item_unit_amounts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX order_item_unit_amounts_pkey ON public.order_item_unit_amounts USING btree (id)
```


### Triggers

**`order_unit_snapshot`** — enabled `O`.

```sql
CREATE TRIGGER order_unit_snapshot BEFORE INSERT OR DELETE OR UPDATE ON order_item_unit_amounts FOR EACH ROW EXECUTE FUNCTION guard_order_snapshot()
```

**`order_units_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER order_units_totals AFTER INSERT OR DELETE OR UPDATE ON order_item_unit_amounts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals()
```


## order_items

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: order_items_id_order_id_key; PK: order_items_pkey | — | — |
| order_id | uuid | No | — | UNIQUE: order_items_id_order_id_key; FK → orders: order_items_order_id_fkey; UNIQUE: order_items_order_id_line_no_key; FK → order_items: order_items_parent_item_id_order_id_fkey | — | — |
| line_no | integer | No | — | UNIQUE: order_items_order_id_line_no_key | — | — |
| line_type | text | No | — | — | — | — |
| product_id | uuid | Yes | — | FK → products: order_items_product_id_fkey; FK → product_variants: order_items_variant_id_product_id_fkey | — | — |
| variant_id | uuid | Yes | — | FK → product_variants: order_items_variant_id_product_id_fkey | — | — |
| parent_item_id | uuid | Yes | — | FK → order_items: order_items_parent_item_id_order_id_fkey | — | — |
| quantity | integer | No | — | — | — | — |
| unit_price_vnd | bigint | No | — | — | — | — |
| voucher_discount_vnd | bigint | No | 0 | — | — | — |
| points_discount_vnd | bigint | No | 0 | — | — | — |
| net_amount_vnd | bigint | No | — | — | — | — |
| snapshot | jsonb | No | — | — | — | — |

### Constraints

**`order_items_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (net_amount_vnd >= 0 AND net_amount_vnd = (quantity::bigint * unit_price_vnd - voucher_discount_vnd - points_discount_vnd))
```

**`order_items_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type <> 'product'::text OR product_id IS NOT NULL AND variant_id IS NOT NULL)
```

**`order_items_check2`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK ((line_type = 'warranty'::text) = (parent_item_id IS NOT NULL))
```

**`order_items_check3`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type <> 'shipping'::text OR quantity = 1)
```

**`order_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`order_items_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`order_items_line_no_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_no > 0)
```

**`order_items_line_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL line_no
```

**`order_items_line_type_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (line_type = ANY (ARRAY['product'::text, 'warranty'::text, 'shipping'::text]))
```

**`order_items_line_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL line_type
```

**`order_items_net_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL net_amount_vnd
```

**`order_items_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`order_items_order_id_line_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_id, line_no)
```

**`order_items_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`order_items_parent_item_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (parent_item_id, order_id) REFERENCES order_items(id, order_id)
```

**`order_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`order_items_points_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (points_discount_vnd >= 0)
```

**`order_items_points_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL points_discount_vnd
```

**`order_items_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`order_items_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`order_items_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`order_items_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL snapshot
```

**`order_items_unit_price_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_price_vnd >= 0)
```

**`order_items_unit_price_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_price_vnd
```

**`order_items_variant_id_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id, product_id) REFERENCES product_variants(id, product_id)
```

**`order_items_voucher_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (voucher_discount_vnd >= 0)
```

**`order_items_voucher_discount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL voucher_discount_vnd
```

**`order_lines_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`order_items_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_items_id_order_id_key ON public.order_items USING btree (id, order_id)
```

**`order_items_order_id_line_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_items_order_id_line_no_key ON public.order_items USING btree (order_id, line_no)
```

**`order_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX order_items_pkey ON public.order_items USING btree (id)
```


### Triggers

**`order_item_snapshot`** — enabled `O`.

```sql
CREATE TRIGGER order_item_snapshot BEFORE INSERT OR DELETE OR UPDATE ON order_items FOR EACH ROW EXECUTE FUNCTION guard_order_snapshot()
```

**`order_lines_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER order_lines_totals AFTER INSERT OR DELETE OR UPDATE ON order_items DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals()
```


## order_tender_allocations

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: order_tender_allocations_pkey | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: order_tender_allocations_order_item_unit_amount_id_fkey; UNIQUE: order_tender_allocations_receipt_id_order_item_unit_amount__key | — | — |
| receipt_id | uuid | No | — | FK → payment_receipts: order_tender_allocations_receipt_id_fkey; UNIQUE: order_tender_allocations_receipt_id_order_item_unit_amount__key | — | — |
| amount_vnd | bigint | No | — | — | — | — |

### Constraints

**`order_tender_allocations_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd > 0)
```

**`order_tender_allocations_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`order_tender_allocations_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`order_tender_allocations_order_item_unit_amount_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id)
```

**`order_tender_allocations_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`order_tender_allocations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`order_tender_allocations_receipt_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (receipt_id) REFERENCES payment_receipts(id)
```

**`order_tender_allocations_receipt_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL receipt_id
```

**`order_tender_allocations_receipt_id_order_item_unit_amount__key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (receipt_id, order_item_unit_amount_id)
```

**`tender_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`order_tender_allocations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX order_tender_allocations_pkey ON public.order_tender_allocations USING btree (id)
```

**`order_tender_allocations_receipt_id_order_item_unit_amount__key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX order_tender_allocations_receipt_id_order_item_unit_amount__key ON public.order_tender_allocations USING btree (receipt_id, order_item_unit_amount_id)
```


### Triggers

**`tender_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER tender_totals AFTER INSERT ON order_tender_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_tender()
```

**`tenders_append_only`** — enabled `O`.

```sql
CREATE TRIGGER tenders_append_only BEFORE DELETE OR UPDATE ON order_tender_allocations FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## orders

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: orders_id_customer_id_key; PK: orders_pkey | — | — |
| order_no | text | No | — | UNIQUE: orders_order_no_key | — | — |
| customer_id | text | No | — | FK → user: orders_customer_id_fkey; UNIQUE: orders_id_customer_id_key | — | — |
| responsible_staff_id | text | Yes | — | FK → staff_profiles: orders_responsible_staff_id_fkey | — | — |
| order_status | text | No | — | — | — | — |
| payment_method | text | No | — | — | — | — |
| address_snapshot | jsonb | No | — | — | — | — |
| total_vnd | bigint | No | — | — | — | — |
| currency | text | No | 'VND'::text | — | — | — |
| exchange_rate_vnd | numeric | No | 25000 | — | — | — |
| policy_snapshot | jsonb | No | '{}'::jsonb | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| delivered_at | timestamp with time zone | Yes | — | — | — | — |

### Constraints

**`order_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`orders_address_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL address_snapshot
```

**`orders_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`orders_currency_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (currency = 'VND'::text)
```

**`orders_currency_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL currency
```

**`orders_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`orders_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`orders_exchange_rate_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (exchange_rate_vnd > 0::numeric)
```

**`orders_exchange_rate_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL exchange_rate_vnd
```

**`orders_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`orders_id_customer_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, customer_id)
```

**`orders_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`orders_order_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_no)
```

**`orders_order_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_no
```

**`orders_order_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (order_status = ANY (ARRAY['building'::text, 'pending'::text, 'confirmed'::text, 'handed_off'::text, 'completed'::text, 'cancelled'::text, 'expired'::text, 'reconciliation'::text]))
```

**`orders_order_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_status
```

**`orders_payment_method_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (payment_method = ANY (ARRAY['cod'::text, 'banking'::text, 'momo'::text, 'visa'::text]))
```

**`orders_payment_method_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL payment_method
```

**`orders_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`orders_policy_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL policy_snapshot
```

**`orders_responsible_staff_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (responsible_staff_id) REFERENCES staff_profiles(user_id)
```

**`orders_total_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (total_vnd >= 0)
```

**`orders_total_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL total_vnd
```


### Indexes

**`orders_customer_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX orders_customer_idx ON public.orders USING btree (customer_id, created_at)
```

**`orders_id_customer_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX orders_id_customer_id_key ON public.orders USING btree (id, customer_id)
```

**`orders_order_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX orders_order_no_key ON public.orders USING btree (order_no)
```

**`orders_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX orders_pkey ON public.orders USING btree (id)
```


### Triggers

**`order_header_snapshot`** — enabled `O`.

```sql
CREATE TRIGGER order_header_snapshot BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION guard_order_header()
```

**`order_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER order_totals AFTER INSERT OR UPDATE ON orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_order_totals()
```


## outbox_events

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: outbox_events_pkey | — | — |
| event_key | text | No | — | UNIQUE: outbox_events_event_key_key | — | — |
| type | text | No | — | — | — | — |
| payload | jsonb | No | — | — | — | — |
| status | text | No | 'pending'::text | — | — | — |
| attempts | integer | No | 0 | — | — | — |
| next_attempt_at | timestamp with time zone | No | now() | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`outbox_events_attempts_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL attempts
```

**`outbox_events_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`outbox_events_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`outbox_events_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`outbox_events_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`outbox_events_next_attempt_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL next_attempt_at
```

**`outbox_events_payload_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL payload
```

**`outbox_events_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`outbox_events_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['pending'::text, 'processing'::text, 'completed'::text, 'failed'::text]))
```

**`outbox_events_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`outbox_events_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL type
```


### Indexes

**`outbox_events_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX outbox_events_event_key_key ON public.outbox_events USING btree (event_key)
```

**`outbox_events_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX outbox_events_pkey ON public.outbox_events USING btree (id)
```


### Triggers

None.

## payment_attempts

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: payment_attempts_id_order_id_key; PK: payment_attempts_pkey | — | — |
| order_id | uuid | No | — | UNIQUE: payment_attempts_id_order_id_key; FK → orders: payment_attempts_order_id_fkey | — | — |
| method | text | No | — | — | — | — |
| status | text | No | — | — | — | — |
| requested_amount_vnd | bigint | No | — | — | — | — |
| request_key | text | No | — | UNIQUE: payment_attempts_request_key_key | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`payment_attempts_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`payment_attempts_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`payment_attempts_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`payment_attempts_method_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (method = ANY (ARRAY['cod'::text, 'banking'::text, 'momo'::text, 'visa'::text]))
```

**`payment_attempts_method_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL method
```

**`payment_attempts_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`payment_attempts_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`payment_attempts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`payment_attempts_request_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (request_key)
```

**`payment_attempts_request_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_key
```

**`payment_attempts_requested_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (requested_amount_vnd > 0)
```

**`payment_attempts_requested_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL requested_amount_vnd
```

**`payment_attempts_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['pending'::text, 'success'::text, 'failed'::text, 'cancelled'::text, 'expired'::text, 'reconciliation'::text]))
```

**`payment_attempts_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`payment_attempts_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_attempts_id_order_id_key ON public.payment_attempts USING btree (id, order_id)
```

**`payment_attempts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_attempts_pkey ON public.payment_attempts USING btree (id)
```

**`payment_attempts_request_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_attempts_request_key_key ON public.payment_attempts USING btree (request_key)
```


### Triggers

None.

## payment_events

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: payment_events_pkey | — | — |
| payment_attempt_id | uuid | No | — | FK → payment_attempts: payment_events_payment_attempt_id_fkey | — | — |
| event_key | text | No | — | UNIQUE: payment_events_event_key_key | — | — |
| event_type | text | No | — | — | — | — |
| payload_digest | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`payment_events_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`payment_events_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`payment_events_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`payment_events_event_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_type
```

**`payment_events_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`payment_events_payload_digest_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL payload_digest
```

**`payment_events_payment_attempt_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (payment_attempt_id) REFERENCES payment_attempts(id)
```

**`payment_events_payment_attempt_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL payment_attempt_id
```

**`payment_events_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`payment_events_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_events_event_key_key ON public.payment_events USING btree (event_key)
```

**`payment_events_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_events_pkey ON public.payment_events USING btree (id)
```


### Triggers

None.

## payment_receipts

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: payment_receipts_id_order_id_key; PK: payment_receipts_pkey | — | — |
| order_id | uuid | No | — | UNIQUE: payment_receipts_id_order_id_key; FK → orders: payment_receipts_order_id_fkey; FK → payment_attempts: payment_receipts_payment_attempt_id_order_id_fkey | — | — |
| payment_attempt_id | uuid | Yes | — | FK → payment_attempts: payment_receipts_payment_attempt_id_order_id_fkey | — | — |
| tender_kind | text | No | — | — | — | — |
| amount_vnd | bigint | No | — | — | — | — |
| received_at | timestamp with time zone | No | now() | — | — | — |
| source_event_key | text | No | — | UNIQUE: payment_receipts_source_event_key_key | — | — |

### Constraints

**`payment_receipts_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd > 0)
```

**`payment_receipts_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`payment_receipts_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`payment_receipts_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`payment_receipts_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`payment_receipts_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`payment_receipts_payment_attempt_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (payment_attempt_id, order_id) REFERENCES payment_attempts(id, order_id)
```

**`payment_receipts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`payment_receipts_received_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL received_at
```

**`payment_receipts_source_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (source_event_key)
```

**`payment_receipts_source_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL source_event_key
```

**`payment_receipts_tender_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (tender_kind = ANY (ARRAY['cash_demo'::text, 'store_credit'::text]))
```

**`payment_receipts_tender_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL tender_kind
```

**`receipt_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```


### Indexes

**`payment_receipts_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_receipts_id_order_id_key ON public.payment_receipts USING btree (id, order_id)
```

**`payment_receipts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_receipts_pkey ON public.payment_receipts USING btree (id)
```

**`payment_receipts_source_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX payment_receipts_source_event_key_key ON public.payment_receipts USING btree (source_event_key)
```


### Triggers

**`receipt_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER receipt_totals AFTER INSERT ON payment_receipts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_tender()
```

**`receipts_append_only`** — enabled `O`.

```sql
CREATE TRIGGER receipts_append_only BEFORE DELETE OR UPDATE ON payment_receipts FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## product_attribute_values

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| product_id | uuid | No | — | PK: product_attribute_values_pkey; FK → products: product_attribute_values_product_id_fkey | — | — |
| attribute_id | uuid | No | — | FK → attribute_definitions: product_attribute_values_attribute_id_fkey; PK: product_attribute_values_pkey | — | — |
| value_text | text | Yes | — | — | — | — |
| value_number | numeric | Yes | — | — | — | — |
| value_boolean | boolean | Yes | — | — | — | — |

### Constraints

**`product_attribute_values_attribute_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (attribute_id) REFERENCES attribute_definitions(id)
```

**`product_attribute_values_attribute_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL attribute_id
```

**`product_attribute_values_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (num_nonnulls(value_text, value_number, value_boolean) = 1)
```

**`product_attribute_values_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (product_id, attribute_id)
```

**`product_attribute_values_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`product_attribute_values_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```


### Indexes

**`product_attribute_values_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX product_attribute_values_pkey ON public.product_attribute_values USING btree (product_id, attribute_id)
```


### Triggers

None.

## product_compatibilities

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| accessory_product_id | uuid | No | — | FK → products: product_compatibilities_accessory_product_id_fkey; PK: product_compatibilities_pkey | — | — |
| target_product_id | uuid | No | — | PK: product_compatibilities_pkey; FK → products: product_compatibilities_target_product_id_fkey | — | — |

### Constraints

**`product_compatibilities_accessory_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (accessory_product_id) REFERENCES products(id)
```

**`product_compatibilities_accessory_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL accessory_product_id
```

**`product_compatibilities_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (accessory_product_id <> target_product_id)
```

**`product_compatibilities_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (accessory_product_id, target_product_id)
```

**`product_compatibilities_target_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (target_product_id) REFERENCES products(id)
```

**`product_compatibilities_target_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL target_product_id
```


### Indexes

**`product_compatibilities_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX product_compatibilities_pkey ON public.product_compatibilities USING btree (accessory_product_id, target_product_id)
```


### Triggers

None.

## product_media

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: product_media_pkey | — | — |
| product_id | uuid | No | — | FK → products: product_media_product_id_fkey; FK → product_variants: product_media_variant_id_product_id_fkey | — | — |
| variant_id | uuid | Yes | — | FK → product_variants: product_media_variant_id_product_id_fkey | — | — |
| media_id | uuid | No | — | FK → media_assets: product_media_media_id_fkey | — | — |
| position | integer | No | — | — | — | — |
| role | text | No | — | — | — | — |

### Constraints

**`product_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`product_media_media_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (media_id) REFERENCES media_assets(id)
```

**`product_media_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL media_id
```

**`product_media_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`product_media_position_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL "position"
```

**`product_media_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`product_media_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```

**`product_media_role_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL role
```

**`product_media_variant_id_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id, product_id) REFERENCES product_variants(id, product_id)
```


### Indexes

**`product_media_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX product_media_pkey ON public.product_media USING btree (id)
```


### Triggers

None.

## product_translations

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| product_id | uuid | No | — | PK: product_translations_pkey; FK → products: product_translations_product_id_fkey | — | — |
| locale | text | No | — | PK: product_translations_pkey | — | — |
| name | text | No | — | — | — | — |
| description | text | No | — | — | — | — |
| seo_title | text | No | — | — | — | — |
| content_status | text | No | 'published'::text | — | — | — |

### Constraints

**`product_translations_content_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (content_status = ANY (ARRAY['draft'::text, 'published'::text]))
```

**`product_translations_content_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL content_status
```

**`product_translations_description_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL description
```

**`product_translations_locale_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (locale = ANY (ARRAY['vi'::text, 'en'::text]))
```

**`product_translations_locale_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL locale
```

**`product_translations_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`product_translations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (product_id, locale)
```

**`product_translations_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`product_translations_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```

**`product_translations_seo_title_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL seo_title
```


### Indexes

**`product_translations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX product_translations_pkey ON public.product_translations USING btree (product_id, locale)
```


### Triggers

None.

## product_variants

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: product_variants_id_product_id_key; PK: product_variants_pkey | — | — |
| product_id | uuid | No | — | UNIQUE: product_variants_id_product_id_key; FK → products: product_variants_product_id_fkey; UNIQUE: product_variants_product_id_option_signature_key | — | — |
| sku | text | No | — | UNIQUE: product_variants_sku_key | — | — |
| option_signature | text | No | — | UNIQUE: product_variants_product_id_option_signature_key | — | — |
| options | jsonb | No | '{}'::jsonb | — | — | — |
| price_vnd | bigint | No | — | — | — | — |
| tracking_mode | text | No | — | — | — | — |
| active | boolean | No | true | — | — | — |
| standard_warranty_months | integer | No | 12 | — | — | — |
| care_fee_vnd | bigint | Yes | — | — | — | — |

### Constraints

**`product_variants_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`product_variants_care_fee_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (care_fee_vnd >= 0)
```

**`product_variants_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`product_variants_id_product_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, product_id)
```

**`product_variants_option_signature_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL option_signature
```

**`product_variants_options_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL options
```

**`product_variants_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`product_variants_price_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (price_vnd >= 0)
```

**`product_variants_price_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL price_vnd
```

**`product_variants_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`product_variants_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```

**`product_variants_product_id_option_signature_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (product_id, option_signature)
```

**`product_variants_sku_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (sku)
```

**`product_variants_sku_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL sku
```

**`product_variants_standard_warranty_months_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (standard_warranty_months >= 0)
```

**`product_variants_standard_warranty_months_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL standard_warranty_months
```

**`product_variants_tracking_mode_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (tracking_mode = ANY (ARRAY['serialized'::text, 'quantity'::text]))
```

**`product_variants_tracking_mode_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL tracking_mode
```


### Indexes

**`product_variants_id_product_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX product_variants_id_product_id_key ON public.product_variants USING btree (id, product_id)
```

**`product_variants_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX product_variants_pkey ON public.product_variants USING btree (id)
```

**`product_variants_product_id_option_signature_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX product_variants_product_id_option_signature_key ON public.product_variants USING btree (product_id, option_signature)
```

**`product_variants_sku_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX product_variants_sku_key ON public.product_variants USING btree (sku)
```


### Triggers

None.

## products

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: products_pkey | — | — |
| category_id | uuid | No | — | FK → categories: products_category_id_fkey | — | — |
| slug | text | No | — | UNIQUE: products_slug_key | — | — |
| model_code | text | No | — | UNIQUE: products_model_code_key | — | — |
| business_status | text | No | 'active'::text | — | — | — |
| generation | text | Yes | — | — | — | — |
| source_url | text | No | — | — | — | — |
| specs | jsonb | No | '{}'::jsonb | — | — | — |
| poster | text | No | '/media/phone-burgundy.svg'::text | — | — | — |

### Constraints

**`products_business_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (business_status = ANY (ARRAY['draft'::text, 'active'::text, 'discontinued'::text, 'archived'::text]))
```

**`products_business_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL business_status
```

**`products_category_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (category_id) REFERENCES categories(id)
```

**`products_category_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL category_id
```

**`products_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`products_model_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (model_code)
```

**`products_model_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL model_code
```

**`products_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`products_poster_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL poster
```

**`products_slug_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (slug)
```

**`products_slug_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL slug
```

**`products_source_url_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL source_url
```

**`products_specs_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL specs
```


### Indexes

**`products_model_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX products_model_code_key ON public.products USING btree (model_code)
```

**`products_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX products_pkey ON public.products USING btree (id)
```

**`products_slug_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX products_slug_key ON public.products USING btree (slug)
```


### Triggers

None.

## promotion_redemptions

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: promotion_redemptions_pkey | — | — |
| promotion_id | uuid | No | — | UNIQUE: promotion_redemptions_order_id_promotion_id_key; FK → promotions: promotion_redemptions_promotion_id_fkey; FK → promotion_versions: promotion_redemptions_promotion_version_id_promotion_id_fkey | — | — |
| promotion_version_id | uuid | No | — | FK → promotion_versions: promotion_redemptions_promotion_version_id_promotion_id_fkey | — | — |
| customer_id | text | No | — | FK → user: promotion_redemptions_customer_id_fkey; FK → orders: promotion_redemptions_order_id_customer_id_fkey | — | — |
| order_id | uuid | No | — | FK → orders: promotion_redemptions_order_id_customer_id_fkey; UNIQUE: promotion_redemptions_order_id_promotion_id_key | — | — |
| status | text | No | — | — | — | — |
| amount_vnd | bigint | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| idempotency_key | text | No | — | UNIQUE: promotion_redemptions_idempotency_key_key | — | — |

### Constraints

**`promotion_redemptions_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd >= 0)
```

**`promotion_redemptions_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`promotion_redemptions_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`promotion_redemptions_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`promotion_redemptions_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`promotion_redemptions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`promotion_redemptions_idempotency_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (idempotency_key)
```

**`promotion_redemptions_idempotency_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL idempotency_key
```

**`promotion_redemptions_order_id_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id, customer_id) REFERENCES orders(id, customer_id)
```

**`promotion_redemptions_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`promotion_redemptions_order_id_promotion_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_id, promotion_id)
```

**`promotion_redemptions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`promotion_redemptions_promotion_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (promotion_id) REFERENCES promotions(id)
```

**`promotion_redemptions_promotion_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL promotion_id
```

**`promotion_redemptions_promotion_version_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL promotion_version_id
```

**`promotion_redemptions_promotion_version_id_promotion_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (promotion_version_id, promotion_id) REFERENCES promotion_versions(id, promotion_id)
```

**`promotion_redemptions_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['reserved'::text, 'consumed'::text, 'released'::text]))
```

**`promotion_redemptions_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`promotion_redemptions_idempotency_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_redemptions_idempotency_key_key ON public.promotion_redemptions USING btree (idempotency_key)
```

**`promotion_redemptions_order_id_promotion_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_redemptions_order_id_promotion_id_key ON public.promotion_redemptions USING btree (order_id, promotion_id)
```

**`promotion_redemptions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_redemptions_pkey ON public.promotion_redemptions USING btree (id)
```

**`redemptions_quota_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX redemptions_quota_idx ON public.promotion_redemptions USING btree (promotion_id, status, customer_id)
```


### Triggers

None.

## promotion_targets

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: promotion_targets_pkey | — | — |
| promotion_version_id | uuid | No | — | FK → promotion_versions: promotion_targets_promotion_version_id_fkey | — | — |
| variant_id | uuid | Yes | — | FK → product_variants: promotion_targets_variant_id_fkey | — | — |
| product_id | uuid | Yes | — | FK → products: promotion_targets_product_id_fkey | — | — |
| category_id | uuid | Yes | — | FK → categories: promotion_targets_category_id_fkey | — | — |

### Constraints

**`promotion_targets_category_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (category_id) REFERENCES categories(id)
```

**`promotion_targets_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (num_nonnulls(variant_id, product_id, category_id) = 1)
```

**`promotion_targets_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`promotion_targets_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`promotion_targets_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`promotion_targets_promotion_version_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (promotion_version_id) REFERENCES promotion_versions(id)
```

**`promotion_targets_promotion_version_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL promotion_version_id
```

**`promotion_targets_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```


### Indexes

**`promotion_targets_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_targets_pkey ON public.promotion_targets USING btree (id)
```


### Triggers

**`promotion_target_guard`** — enabled `O`.

```sql
CREATE TRIGGER promotion_target_guard BEFORE INSERT OR DELETE OR UPDATE ON promotion_targets FOR EACH ROW EXECUTE FUNCTION guard_promotion_target()
```


## promotion_versions

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: promotion_versions_id_promotion_id_key; PK: promotion_versions_pkey | — | — |
| promotion_id | uuid | No | — | UNIQUE: promotion_versions_id_promotion_id_key; FK → promotions: promotion_versions_promotion_id_fkey; UNIQUE: promotion_versions_promotion_id_version_no_key | — | — |
| version_no | integer | No | — | UNIQUE: promotion_versions_promotion_id_version_no_key | — | — |
| discount_type | text | No | — | — | — | — |
| value | bigint | No | — | — | — | — |
| max_discount_vnd | bigint | Yes | — | — | — | — |
| min_purchase_vnd | bigint | No | 0 | — | — | — |
| starts_at | timestamp with time zone | No | — | — | — | — |
| ends_at | timestamp with time zone | No | — | — | — | — |
| all_products | boolean | No | true | — | — | — |

### Constraints

**`promotion_versions_all_products_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL all_products
```

**`promotion_versions_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (starts_at < ends_at)
```

**`promotion_versions_check1`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (discount_type <> 'percentage'::text OR value <= 100)
```

**`promotion_versions_discount_type_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (discount_type = ANY (ARRAY['fixed'::text, 'percentage'::text]))
```

**`promotion_versions_discount_type_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL discount_type
```

**`promotion_versions_ends_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL ends_at
```

**`promotion_versions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`promotion_versions_id_promotion_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, promotion_id)
```

**`promotion_versions_max_discount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (max_discount_vnd >= 0)
```

**`promotion_versions_min_purchase_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (min_purchase_vnd >= 0)
```

**`promotion_versions_min_purchase_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL min_purchase_vnd
```

**`promotion_versions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`promotion_versions_promotion_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (promotion_id) REFERENCES promotions(id)
```

**`promotion_versions_promotion_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL promotion_id
```

**`promotion_versions_promotion_id_version_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (promotion_id, version_no)
```

**`promotion_versions_starts_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL starts_at
```

**`promotion_versions_value_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (value > 0)
```

**`promotion_versions_value_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL value
```

**`promotion_versions_version_no_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (version_no > 0)
```

**`promotion_versions_version_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_no
```


### Indexes

**`promotion_versions_id_promotion_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_versions_id_promotion_id_key ON public.promotion_versions USING btree (id, promotion_id)
```

**`promotion_versions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_versions_pkey ON public.promotion_versions USING btree (id)
```

**`promotion_versions_promotion_id_version_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX promotion_versions_promotion_id_version_no_key ON public.promotion_versions USING btree (promotion_id, version_no)
```


### Triggers

**`promotion_version_guard`** — enabled `O`.

```sql
CREATE TRIGGER promotion_version_guard BEFORE DELETE OR UPDATE ON promotion_versions FOR EACH ROW EXECUTE FUNCTION used_promotion_immutable()
```


## promotions

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: promotions_pkey | — | — |
| code_normalized | text | No | — | UNIQUE: promotions_code_normalized_key | — | — |
| active | boolean | No | true | — | — | — |
| max_uses | integer | Yes | — | — | — | — |
| max_uses_per_customer | integer | Yes | — | — | — | — |

### Constraints

**`promotions_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`promotions_code_normalized_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (code_normalized = upper(TRIM(BOTH FROM code_normalized)))
```

**`promotions_code_normalized_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code_normalized)
```

**`promotions_code_normalized_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code_normalized
```

**`promotions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`promotions_max_uses_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (max_uses > 0)
```

**`promotions_max_uses_per_customer_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (max_uses_per_customer > 0)
```

**`promotions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`promotions_code_normalized_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX promotions_code_normalized_key ON public.promotions USING btree (code_normalized)
```

**`promotions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX promotions_pkey ON public.promotions USING btree (id)
```


### Triggers

None.

## purchase_order_items

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: purchase_order_items_id_purchase_order_id_key; PK: purchase_order_items_pkey | — | — |
| purchase_order_id | uuid | No | — | UNIQUE: purchase_order_items_id_purchase_order_id_key; FK → purchase_orders: purchase_order_items_purchase_order_id_fkey | — | — |
| variant_id | uuid | No | — | FK → product_variants: purchase_order_items_variant_id_fkey | — | — |
| quantity | integer | No | — | — | — | — |
| unit_cost_vnd | bigint | No | — | — | — | — |

### Constraints

**`purchase_order_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`purchase_order_items_id_purchase_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, purchase_order_id)
```

**`purchase_order_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`purchase_order_items_purchase_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id)
```

**`purchase_order_items_purchase_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL purchase_order_id
```

**`purchase_order_items_quantity_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity > 0)
```

**`purchase_order_items_quantity_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity
```

**`purchase_order_items_unit_cost_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (unit_cost_vnd >= 0)
```

**`purchase_order_items_unit_cost_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL unit_cost_vnd
```

**`purchase_order_items_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`purchase_order_items_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```


### Indexes

**`purchase_order_items_id_purchase_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX purchase_order_items_id_purchase_order_id_key ON public.purchase_order_items USING btree (id, purchase_order_id)
```

**`purchase_order_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX purchase_order_items_pkey ON public.purchase_order_items USING btree (id)
```


### Triggers

None.

## purchase_orders

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: purchase_orders_pkey | — | — |
| supplier_id | uuid | No | — | FK → suppliers: purchase_orders_supplier_id_fkey | — | — |
| code | text | No | — | UNIQUE: purchase_orders_code_key | — | — |
| status | text | No | — | — | — | — |
| created_by | text | No | — | FK → user: purchase_orders_created_by_fkey | — | — |
| approved_by | text | Yes | — | FK → user: purchase_orders_approved_by_fkey | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`purchase_orders_approved_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (approved_by) REFERENCES "user"(id)
```

**`purchase_orders_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code)
```

**`purchase_orders_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`purchase_orders_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`purchase_orders_created_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (created_by) REFERENCES "user"(id)
```

**`purchase_orders_created_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_by
```

**`purchase_orders_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`purchase_orders_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`purchase_orders_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['draft'::text, 'approved'::text, 'part_received'::text, 'received'::text, 'cancelled'::text]))
```

**`purchase_orders_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`purchase_orders_supplier_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
```

**`purchase_orders_supplier_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL supplier_id
```


### Indexes

**`purchase_orders_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX purchase_orders_code_key ON public.purchase_orders USING btree (code)
```

**`purchase_orders_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX purchase_orders_pkey ON public.purchase_orders USING btree (id)
```


### Triggers

None.

## refund_allocations

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: refund_allocations_pkey | — | — |
| refund_id | uuid | No | — | FK → refunds: refund_allocations_refund_id_fkey; UNIQUE: refund_allocations_refund_id_payment_receipt_id_order_item__key | — | — |
| payment_receipt_id | uuid | No | — | FK → payment_receipts: refund_allocations_payment_receipt_id_fkey; UNIQUE: refund_allocations_refund_id_payment_receipt_id_order_item__key | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: refund_allocations_order_item_unit_amount_id_fkey; UNIQUE: refund_allocations_refund_id_payment_receipt_id_order_item__key | — | — |
| amount_vnd | bigint | No | — | — | — | — |

### Constraints

**`refund_allocation_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`refund_allocations_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd > 0)
```

**`refund_allocations_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`refund_allocations_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`refund_allocations_order_item_unit_amount_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id)
```

**`refund_allocations_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`refund_allocations_payment_receipt_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (payment_receipt_id) REFERENCES payment_receipts(id)
```

**`refund_allocations_payment_receipt_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL payment_receipt_id
```

**`refund_allocations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`refund_allocations_refund_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (refund_id) REFERENCES refunds(id)
```

**`refund_allocations_refund_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL refund_id
```

**`refund_allocations_refund_id_payment_receipt_id_order_item__key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (refund_id, payment_receipt_id, order_item_unit_amount_id)
```


### Indexes

**`refund_allocations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX refund_allocations_pkey ON public.refund_allocations USING btree (id)
```

**`refund_allocations_refund_id_payment_receipt_id_order_item__key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX refund_allocations_refund_id_payment_receipt_id_order_item__key ON public.refund_allocations USING btree (refund_id, payment_receipt_id, order_item_unit_amount_id)
```


### Triggers

**`refund_allocation_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER refund_allocation_totals AFTER INSERT OR UPDATE ON refund_allocations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_refund()
```

**`refund_allocations_immutable`** — enabled `O`.

```sql
CREATE TRIGGER refund_allocations_immutable BEFORE DELETE OR UPDATE ON refund_allocations FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## refunds

Domain: Invoices & refunds.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: refunds_pkey | — | — |
| order_id | uuid | No | — | FK → credit_notes: refunds_credit_note_id_order_id_fkey; FK → orders: refunds_order_id_fkey | — | — |
| credit_note_id | uuid | Yes | — | FK → credit_notes: refunds_credit_note_id_order_id_fkey | — | — |
| status | text | No | — | — | — | — |
| amount_vnd | bigint | No | — | — | — | — |
| approved_by | text | No | — | FK → user: refunds_approved_by_fkey | — | — |
| completed_at | timestamp with time zone | Yes | — | — | — | — |
| idempotency_key | text | No | — | UNIQUE: refunds_idempotency_key_key | — | — |

### Constraints

**`refund_totals`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`refunds_amount_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (amount_vnd > 0)
```

**`refunds_amount_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL amount_vnd
```

**`refunds_approved_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (approved_by) REFERENCES "user"(id)
```

**`refunds_approved_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL approved_by
```

**`refunds_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status <> 'completed'::text OR completed_at IS NOT NULL)
```

**`refunds_credit_note_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (credit_note_id, order_id) REFERENCES credit_notes(id, order_id)
```

**`refunds_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`refunds_idempotency_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (idempotency_key)
```

**`refunds_idempotency_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL idempotency_key
```

**`refunds_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`refunds_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`refunds_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`refunds_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['approved'::text, 'pending'::text, 'completed'::text, 'failed'::text]))
```

**`refunds_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`refunds_idempotency_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX refunds_idempotency_key_key ON public.refunds USING btree (idempotency_key)
```

**`refunds_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX refunds_pkey ON public.refunds USING btree (id)
```


### Triggers

**`refund_mutation_guard`** — enabled `O`.

```sql
CREATE TRIGGER refund_mutation_guard BEFORE DELETE OR UPDATE ON refunds FOR EACH ROW EXECUTE FUNCTION guard_refund_mutation()
```

**`refund_totals`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER refund_totals AFTER INSERT OR UPDATE ON refunds DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_refund()
```


## return_evidence

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| return_request_id | uuid | No | — | PK: return_evidence_pkey; FK → return_requests: return_evidence_return_request_id_fkey | — | — |
| media_id | uuid | No | — | FK → media_assets: return_evidence_media_id_fkey; PK: return_evidence_pkey | — | — |

### Constraints

**`return_evidence_media_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (media_id) REFERENCES media_assets(id)
```

**`return_evidence_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL media_id
```

**`return_evidence_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (return_request_id, media_id)
```

**`return_evidence_return_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (return_request_id) REFERENCES return_requests(id)
```

**`return_evidence_return_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL return_request_id
```


### Indexes

**`return_evidence_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX return_evidence_pkey ON public.return_evidence USING btree (return_request_id, media_id)
```


### Triggers

None.

## return_item_units

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| return_item_id | uuid | No | — | PK: return_item_units_pkey; FK → return_items: return_item_units_return_item_id_order_item_id_fkey | — | — |
| order_item_id | uuid | No | — | FK → order_item_unit_amounts: return_item_units_order_item_unit_amount_id_order_item_id_fkey; FK → return_items: return_item_units_return_item_id_order_item_id_fkey | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: return_item_units_order_item_unit_amount_id_order_item_id_fkey; PK: return_item_units_pkey | — | — |
| inventory_unit_id | uuid | Yes | — | FK → inventory_units: return_item_units_inventory_unit_id_fkey | — | — |

### Constraints

**`return_item_units_inventory_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inventory_unit_id) REFERENCES inventory_units(id)
```

**`return_item_units_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`return_item_units_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`return_item_units_order_item_unit_amount_id_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id, order_item_id) REFERENCES order_item_unit_amounts(id, order_item_id)
```

**`return_item_units_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (return_item_id, order_item_unit_amount_id)
```

**`return_item_units_return_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL return_item_id
```

**`return_item_units_return_item_id_order_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (return_item_id, order_item_id) REFERENCES return_items(id, order_item_id)
```


### Indexes

**`return_item_units_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX return_item_units_pkey ON public.return_item_units USING btree (return_item_id, order_item_unit_amount_id)
```


### Triggers

None.

## return_items

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: return_items_id_order_item_id_key; PK: return_items_pkey | — | — |
| return_request_id | uuid | No | — | FK → return_requests: return_items_return_request_id_order_id_fkey | — | — |
| order_id | uuid | No | — | FK → order_items: return_items_order_item_id_order_id_fkey; FK → return_requests: return_items_return_request_id_order_id_fkey | — | — |
| order_item_id | uuid | No | — | UNIQUE: return_items_id_order_item_id_key; FK → order_items: return_items_order_item_id_order_id_fkey | — | — |
| qty | integer | No | — | — | — | — |
| actual_received_identifier | text | Yes | — | — | — | — |

### Constraints

**`return_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`return_items_id_order_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_item_id)
```

**`return_items_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`return_items_order_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_id
```

**`return_items_order_item_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_id, order_id) REFERENCES order_items(id, order_id)
```

**`return_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`return_items_qty_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (qty > 0)
```

**`return_items_qty_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL qty
```

**`return_items_return_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL return_request_id
```

**`return_items_return_request_id_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (return_request_id, order_id) REFERENCES return_requests(id, order_id)
```


### Indexes

**`return_items_id_order_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX return_items_id_order_item_id_key ON public.return_items USING btree (id, order_item_id)
```

**`return_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX return_items_pkey ON public.return_items USING btree (id)
```


### Triggers

None.

## return_requests

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: return_requests_id_order_id_key; PK: return_requests_pkey | — | — |
| order_id | uuid | No | — | UNIQUE: return_requests_id_order_id_key; FK → orders: return_requests_order_id_customer_id_fkey | — | — |
| customer_id | text | No | — | FK → orders: return_requests_order_id_customer_id_fkey | — | — |
| state | text | No | — | — | — | — |
| reason | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| inspection_result | text | Yes | — | — | — | — |
| inspected_by | text | Yes | — | FK → user: return_requests_inspected_by_fkey | — | — |
| inspected_at | timestamp with time zone | Yes | — | — | — | — |
| fee_vnd | bigint | No | 30000 | — | — | — |

### Constraints

**`return_requests_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`return_requests_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`return_requests_fee_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (fee_vnd >= 0)
```

**`return_requests_fee_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL fee_vnd
```

**`return_requests_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`return_requests_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`return_requests_inspected_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inspected_by) REFERENCES "user"(id)
```

**`return_requests_order_id_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id, customer_id) REFERENCES orders(id, customer_id)
```

**`return_requests_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`return_requests_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`return_requests_reason_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (reason = ANY (ARRAY['defective'::text, 'wrong_item'::text]))
```

**`return_requests_reason_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reason
```

**`return_requests_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['requested'::text, 'approved'::text, 'received'::text, 'inspected'::text, 'rejected'::text, 'credited'::text, 'closed'::text]))
```

**`return_requests_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```


### Indexes

**`return_requests_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX return_requests_id_order_id_key ON public.return_requests USING btree (id, order_id)
```

**`return_requests_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX return_requests_pkey ON public.return_requests USING btree (id)
```


### Triggers

None.

## schema_migrations

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| name | text | No | — | PK: schema_migrations_pkey | — | — |
| checksum | text | No | — | — | — | — |
| applied_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`schema_migrations_applied_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL applied_at
```

**`schema_migrations_checksum_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL checksum
```

**`schema_migrations_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`schema_migrations_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (name)
```


### Indexes

**`schema_migrations_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX schema_migrations_pkey ON public.schema_migrations USING btree (name)
```


### Triggers

None.

## session

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | text | No | — | PK: session_pkey | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| token | text | No | — | UNIQUE: session_token_key | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| updated_at | timestamp with time zone | No | now() | — | — | — |
| ip_address | text | Yes | — | — | — | — |
| user_agent | text | Yes | — | — | — | — |
| user_id | text | No | — | FK → user: session_user_id_fkey | — | — |

### Constraints

**`session_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`session_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`session_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`session_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`session_token_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (token)
```

**`session_token_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL token
```

**`session_updated_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL updated_at
```

**`session_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id) ON DELETE CASCADE
```

**`session_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`session_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX session_pkey ON public.session USING btree (id)
```

**`session_token_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX session_token_key ON public.session USING btree (token)
```

**`session_user_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX session_user_idx ON public.session USING btree (user_id)
```


### Triggers

None.

## setting_versions

Domain: System & audit.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: setting_versions_pkey | — | — |
| key | text | No | — | FK → app_settings: setting_versions_key_fkey; UNIQUE: setting_versions_key_version_no_key | — | — |
| version_no | integer | No | — | UNIQUE: setting_versions_key_version_no_key | — | — |
| value | jsonb | No | — | — | — | — |
| changed_by | text | Yes | — | FK → user: setting_versions_changed_by_fkey | — | — |
| changed_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`setting_versions_changed_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL changed_at
```

**`setting_versions_changed_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (changed_by) REFERENCES "user"(id)
```

**`setting_versions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`setting_versions_key_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (key) REFERENCES app_settings(key)
```

**`setting_versions_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL key
```

**`setting_versions_key_version_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (key, version_no)
```

**`setting_versions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`setting_versions_value_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL value
```

**`setting_versions_version_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_no
```


### Indexes

**`setting_versions_key_version_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX setting_versions_key_version_no_key ON public.setting_versions USING btree (key, version_no)
```

**`setting_versions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX setting_versions_pkey ON public.setting_versions USING btree (id)
```


### Triggers

**`settings_history_immutable`** — enabled `O`.

```sql
CREATE TRIGGER settings_history_immutable BEFORE DELETE OR UPDATE ON setting_versions FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## shipment_events

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: shipment_events_pkey | — | — |
| shipment_id | uuid | No | — | FK → shipments: shipment_events_shipment_id_fkey | — | — |
| status | text | No | — | — | — | — |
| occurred_at | timestamp with time zone | No | now() | — | — | — |
| event_key | text | No | — | UNIQUE: shipment_events_event_key_key | — | — |

### Constraints

**`shipment_events_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`shipment_events_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`shipment_events_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`shipment_events_occurred_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL occurred_at
```

**`shipment_events_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`shipment_events_shipment_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (shipment_id) REFERENCES shipments(id)
```

**`shipment_events_shipment_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL shipment_id
```

**`shipment_events_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`shipment_events_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX shipment_events_event_key_key ON public.shipment_events USING btree (event_key)
```

**`shipment_events_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX shipment_events_pkey ON public.shipment_events USING btree (id)
```


### Triggers

None.

## shipments

Domain: Orders & payments.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: shipments_id_order_id_key; PK: shipments_pkey | — | — |
| order_id | uuid | No | — | UNIQUE: shipments_id_order_id_key; FK → orders: shipments_order_id_fkey | — | — |
| tracking_no | text | No | — | UNIQUE: shipments_tracking_no_key | — | — |
| status | text | No | — | — | — | — |
| retry_of | uuid | Yes | — | FK → shipments: shipments_retry_of_fkey | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`shipments_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`shipments_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`shipments_id_order_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, order_id)
```

**`shipments_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`shipments_order_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_id
```

**`shipments_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`shipments_retry_of_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (retry_of) REFERENCES shipments(id)
```

**`shipments_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['handed_off'::text, 'in_transit'::text, 'delivered'::text, 'failed'::text, 'returned'::text, 'closed'::text]))
```

**`shipments_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```

**`shipments_tracking_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (tracking_no)
```

**`shipments_tracking_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL tracking_no
```


### Indexes

**`shipments_id_order_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX shipments_id_order_id_key ON public.shipments USING btree (id, order_id)
```

**`shipments_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX shipments_pkey ON public.shipments USING btree (id)
```

**`shipments_tracking_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX shipments_tracking_no_key ON public.shipments USING btree (tracking_no)
```


### Triggers

None.

## staff_profiles

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | PK: staff_profiles_pkey; FK → user: staff_profiles_user_id_fkey | — | — |
| staff_code | text | No | — | UNIQUE: staff_profiles_staff_code_key | — | — |
| role | text | No | — | — | — | — |
| active | boolean | No | true | — | — | — |

### Constraints

**`staff_profiles_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`staff_profiles_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id)
```

**`staff_profiles_role_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (role = ANY (ARRAY['owner'::text, 'order_staff'::text, 'warehouse_staff'::text, 'content_staff'::text]))
```

**`staff_profiles_role_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL role
```

**`staff_profiles_staff_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (staff_code)
```

**`staff_profiles_staff_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL staff_code
```

**`staff_profiles_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`staff_profiles_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`staff_profiles_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX staff_profiles_pkey ON public.staff_profiles USING btree (user_id)
```

**`staff_profiles_staff_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX staff_profiles_staff_code_key ON public.staff_profiles USING btree (staff_code)
```


### Triggers

None.

## stock_adjustments

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: stock_adjustments_pkey | — | — |
| lot_id | uuid | No | — | FK → inventory_lots: stock_adjustments_lot_id_fkey | — | — |
| quantity_delta | integer | No | — | — | — | — |
| reason | text | No | — | — | — | — |
| requested_by | text | No | — | FK → user: stock_adjustments_requested_by_fkey | — | — |
| approved_by | text | Yes | — | FK → user: stock_adjustments_approved_by_fkey | — | — |
| status | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`stock_adjustments_approved_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (approved_by) REFERENCES "user"(id)
```

**`stock_adjustments_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`stock_adjustments_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`stock_adjustments_lot_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (lot_id) REFERENCES inventory_lots(id)
```

**`stock_adjustments_lot_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lot_id
```

**`stock_adjustments_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`stock_adjustments_quantity_delta_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (quantity_delta <> 0)
```

**`stock_adjustments_quantity_delta_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity_delta
```

**`stock_adjustments_reason_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reason
```

**`stock_adjustments_requested_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (requested_by) REFERENCES "user"(id)
```

**`stock_adjustments_requested_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL requested_by
```

**`stock_adjustments_status_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (status = ANY (ARRAY['requested'::text, 'approved'::text, 'rejected'::text]))
```

**`stock_adjustments_status_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL status
```


### Indexes

**`stock_adjustments_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX stock_adjustments_pkey ON public.stock_adjustments USING btree (id)
```


### Triggers

None.

## stock_movements

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: stock_movements_pkey | — | — |
| variant_id | uuid | No | — | FK → inventory_lots: stock_movements_lot_id_variant_id_fkey; FK → product_variants: stock_movements_variant_id_fkey | — | — |
| lot_id | uuid | No | — | FK → inventory_lots: stock_movements_lot_id_variant_id_fkey; FK → inventory_units: stock_movements_unit_id_lot_id_fkey | — | — |
| unit_id | uuid | Yes | — | FK → inventory_units: stock_movements_unit_id_lot_id_fkey | — | — |
| quantity_delta | integer | No | — | — | — | — |
| kind | text | No | — | — | — | — |
| source_event_key | text | No | — | UNIQUE: stock_movements_source_event_key_key | — | — |
| actor_user_id | text | Yes | — | FK → user: stock_movements_actor_user_id_fkey | — | — |
| reason | text | No | ''::text | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`stock_movements_actor_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (actor_user_id) REFERENCES "user"(id)
```

**`stock_movements_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`stock_movements_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`stock_movements_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['receive'::text, 'reserve'::text, 'release'::text, 'ship'::text, 'return'::text, 'adjust'::text, 'replacement'::text]))
```

**`stock_movements_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`stock_movements_lot_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lot_id
```

**`stock_movements_lot_id_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (lot_id, variant_id) REFERENCES inventory_lots(id, variant_id)
```

**`stock_movements_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`stock_movements_quantity_delta_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quantity_delta
```

**`stock_movements_reason_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reason
```

**`stock_movements_source_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (source_event_key)
```

**`stock_movements_source_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL source_event_key
```

**`stock_movements_unit_id_lot_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (unit_id, lot_id) REFERENCES inventory_units(id, lot_id)
```

**`stock_movements_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`stock_movements_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```


### Indexes

**`stock_movements_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX stock_movements_pkey ON public.stock_movements USING btree (id)
```

**`stock_movements_source_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX stock_movements_source_event_key_key ON public.stock_movements USING btree (source_event_key)
```


### Triggers

**`stock_append_only`** — enabled `O`.

```sql
CREATE TRIGGER stock_append_only BEFORE DELETE OR UPDATE ON stock_movements FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```


## store_credit_accounts

Domain: Customer ledgers.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | PK: store_credit_accounts_pkey; FK → user: store_credit_accounts_user_id_fkey | — | — |
| balance_vnd | bigint | No | 0 | — | — | — |

### Constraints

**`store_credit_accounts_balance_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (balance_vnd >= 0)
```

**`store_credit_accounts_balance_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL balance_vnd
```

**`store_credit_accounts_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id)
```

**`store_credit_accounts_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`store_credit_accounts_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`store_credit_accounts_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX store_credit_accounts_pkey ON public.store_credit_accounts USING btree (user_id)
```


### Triggers

None.

## store_credit_entries

Domain: Customer ledgers.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: store_credit_entries_pkey | — | — |
| user_id | text | No | — | FK → store_credit_accounts: store_credit_entries_user_id_fkey | — | — |
| delta_vnd | bigint | No | — | — | — | — |
| kind | text | No | — | — | — | — |
| trade_in_item_id | uuid | Yes | — | FK → trade_in_items: store_credit_entries_trade_in_item_id_fkey | — | — |
| quote_item_id | uuid | Yes | — | FK → trade_in_quote_items: store_credit_entries_quote_item_id_fkey | — | — |
| order_id | uuid | Yes | — | FK → orders: store_credit_entries_order_id_fkey | — | — |
| refund_id | uuid | Yes | — | FK → refunds: store_credit_entries_refund_id_fkey | — | — |
| event_key | text | No | — | UNIQUE: store_credit_entries_event_key_key | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`credit_ledger_source`** — type `t`, validated `True`, deferrable `True`, initially deferred `True`.

```sql
TRIGGER DEFERRABLE INITIALLY DEFERRED
```

**`store_credit_entries_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = 'trade_in'::text AND trade_in_item_id IS NOT NULL AND quote_item_id IS NOT NULL AND order_id IS NULL AND refund_id IS NULL AND delta_vnd > 0 OR kind = 'spend'::text AND order_id IS NOT NULL AND trade_in_item_id IS NULL AND refund_id IS NULL AND delta_vnd < 0 OR kind = 'refund'::text AND refund_id IS NOT NULL AND order_id IS NOT NULL AND trade_in_item_id IS NULL AND delta_vnd > 0)
```

**`store_credit_entries_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`store_credit_entries_delta_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (delta_vnd <> 0)
```

**`store_credit_entries_delta_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL delta_vnd
```

**`store_credit_entries_event_key_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (event_key)
```

**`store_credit_entries_event_key_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event_key
```

**`store_credit_entries_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`store_credit_entries_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (kind = ANY (ARRAY['trade_in'::text, 'spend'::text, 'refund'::text]))
```

**`store_credit_entries_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL kind
```

**`store_credit_entries_order_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_id) REFERENCES orders(id)
```

**`store_credit_entries_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`store_credit_entries_quote_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (quote_item_id) REFERENCES trade_in_quote_items(id)
```

**`store_credit_entries_refund_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (refund_id) REFERENCES refunds(id)
```

**`store_credit_entries_trade_in_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (trade_in_item_id) REFERENCES trade_in_items(id)
```

**`store_credit_entries_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES store_credit_accounts(user_id)
```

**`store_credit_entries_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`store_credit_entries_event_key_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX store_credit_entries_event_key_key ON public.store_credit_entries USING btree (event_key)
```

**`store_credit_entries_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX store_credit_entries_pkey ON public.store_credit_entries USING btree (id)
```

**`trade_in_credit_once`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_credit_once ON public.store_credit_entries USING btree (trade_in_item_id) WHERE (kind = 'trade_in'::text)
```


### Triggers

**`credit_ledger_append_only`** — enabled `O`.

```sql
CREATE TRIGGER credit_ledger_append_only BEFORE DELETE OR UPDATE ON store_credit_entries FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```

**`credit_ledger_source`** — enabled `O`.

```sql
CREATE CONSTRAINT TRIGGER credit_ledger_source AFTER INSERT ON store_credit_entries DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_ledger_source()
```


## suppliers

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: suppliers_pkey | — | — |
| code | text | No | — | UNIQUE: suppliers_code_key | — | — |
| name | text | No | — | — | — | — |
| contact | text | No | — | — | — | — |
| active | boolean | No | true | — | — | — |

### Constraints

**`suppliers_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`suppliers_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code)
```

**`suppliers_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`suppliers_contact_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL contact
```

**`suppliers_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`suppliers_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`suppliers_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`suppliers_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX suppliers_code_key ON public.suppliers USING btree (code)
```

**`suppliers_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX suppliers_pkey ON public.suppliers USING btree (id)
```


### Triggers

None.

## trade_in_evidence

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| inspection_id | uuid | No | — | FK → trade_in_inspections: trade_in_evidence_inspection_id_fkey; PK: trade_in_evidence_pkey | — | — |
| media_id | uuid | No | — | FK → media_assets: trade_in_evidence_media_id_fkey; PK: trade_in_evidence_pkey | — | — |

### Constraints

**`trade_in_evidence_inspection_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inspection_id) REFERENCES trade_in_inspections(id)
```

**`trade_in_evidence_inspection_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL inspection_id
```

**`trade_in_evidence_media_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (media_id) REFERENCES media_assets(id)
```

**`trade_in_evidence_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL media_id
```

**`trade_in_evidence_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (inspection_id, media_id)
```


### Indexes

**`trade_in_evidence_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_evidence_pkey ON public.trade_in_evidence USING btree (inspection_id, media_id)
```


### Triggers

None.

## trade_in_inspections

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: trade_in_inspections_pkey | — | — |
| item_id | uuid | No | — | FK → trade_in_items: trade_in_inspections_item_id_fkey | — | — |
| inspector_staff_id | text | No | — | FK → staff_profiles: trade_in_inspections_inspector_staff_id_fkey | — | — |
| checklist_version | integer | No | — | — | — | — |
| assessed_condition | jsonb | No | — | — | — | — |
| inspected_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`trade_in_inspections_assessed_condition_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL assessed_condition
```

**`trade_in_inspections_checklist_version_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL checklist_version
```

**`trade_in_inspections_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_inspections_inspected_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL inspected_at
```

**`trade_in_inspections_inspector_staff_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inspector_staff_id) REFERENCES staff_profiles(user_id)
```

**`trade_in_inspections_inspector_staff_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL inspector_staff_id
```

**`trade_in_inspections_item_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (item_id) REFERENCES trade_in_items(id)
```

**`trade_in_inspections_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL item_id
```

**`trade_in_inspections_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`trade_in_inspections_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_inspections_pkey ON public.trade_in_inspections USING btree (id)
```


### Triggers

None.

## trade_in_items

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: trade_in_items_id_request_id_key; PK: trade_in_items_pkey | — | — |
| request_id | uuid | No | — | UNIQUE: trade_in_items_id_request_id_key; FK → trade_in_requests: trade_in_items_request_id_fkey | — | — |
| product_id | uuid | No | — | FK → products: trade_in_items_product_id_fkey; FK → product_variants: trade_in_items_variant_id_product_id_fkey | — | — |
| variant_id | uuid | Yes | — | FK → product_variants: trade_in_items_variant_id_product_id_fkey | — | — |
| qty | integer | No | — | — | — | — |
| identifier | text | Yes | — | — | — | — |
| internal_code | text | No | — | UNIQUE: trade_in_items_internal_code_key | — | — |
| condition_snapshot | jsonb | No | — | — | — | — |
| state | text | No | — | — | — | — |
| inventory_unit_id | uuid | Yes | — | FK → inventory_units: trade_in_items_inventory_unit_id_fkey | — | — |

### Constraints

**`trade_in_items_condition_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL condition_snapshot
```

**`trade_in_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_items_id_request_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, request_id)
```

**`trade_in_items_internal_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (internal_code)
```

**`trade_in_items_internal_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL internal_code
```

**`trade_in_items_inventory_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inventory_unit_id) REFERENCES inventory_units(id)
```

**`trade_in_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`trade_in_items_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`trade_in_items_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```

**`trade_in_items_qty_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (qty > 0)
```

**`trade_in_items_qty_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL qty
```

**`trade_in_items_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (request_id) REFERENCES trade_in_requests(id)
```

**`trade_in_items_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_id
```

**`trade_in_items_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'quoted'::text, 'accepted'::text, 'rejected'::text, 'completed'::text, 'returned'::text, 'exception'::text]))
```

**`trade_in_items_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```

**`trade_in_items_variant_id_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id, product_id) REFERENCES product_variants(id, product_id)
```


### Indexes

**`trade_in_identifier_active_idx`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_identifier_active_idx ON public.trade_in_items USING btree (identifier) WHERE ((identifier IS NOT NULL) AND (state <> ALL (ARRAY['completed'::text, 'returned'::text])))
```

**`trade_in_items_id_request_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_items_id_request_id_key ON public.trade_in_items USING btree (id, request_id)
```

**`trade_in_items_internal_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_items_internal_code_key ON public.trade_in_items USING btree (internal_code)
```

**`trade_in_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_items_pkey ON public.trade_in_items USING btree (id)
```


### Triggers

None.

## trade_in_quote_items

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: trade_in_quote_items_pkey | — | — |
| quote_id | uuid | No | — | UNIQUE: trade_in_quote_items_quote_id_item_id_key; FK → trade_in_quotes: trade_in_quote_items_quote_id_request_id_fkey | — | — |
| request_id | uuid | No | — | FK → trade_in_items: trade_in_quote_items_item_id_request_id_fkey; FK → trade_in_quotes: trade_in_quote_items_quote_id_request_id_fkey | — | — |
| item_id | uuid | No | — | FK → trade_in_items: trade_in_quote_items_item_id_request_id_fkey; UNIQUE: trade_in_quote_items_quote_id_item_id_key | — | — |
| offered_vnd | bigint | No | — | — | — | — |
| decision | text | No | — | — | — | — |
| decided_at | timestamp with time zone | Yes | — | — | — | — |

### Constraints

**`trade_in_quote_items_decision_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (decision = ANY (ARRAY['pending'::text, 'accepted'::text, 'rejected'::text]))
```

**`trade_in_quote_items_decision_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL decision
```

**`trade_in_quote_items_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_quote_items_item_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL item_id
```

**`trade_in_quote_items_item_id_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (item_id, request_id) REFERENCES trade_in_items(id, request_id)
```

**`trade_in_quote_items_offered_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (offered_vnd > 0)
```

**`trade_in_quote_items_offered_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL offered_vnd
```

**`trade_in_quote_items_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`trade_in_quote_items_quote_id_item_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (quote_id, item_id)
```

**`trade_in_quote_items_quote_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quote_id
```

**`trade_in_quote_items_quote_id_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (quote_id, request_id) REFERENCES trade_in_quotes(id, request_id)
```

**`trade_in_quote_items_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_id
```


### Indexes

**`trade_in_quote_items_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_quote_items_pkey ON public.trade_in_quote_items USING btree (id)
```

**`trade_in_quote_items_quote_id_item_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_quote_items_quote_id_item_id_key ON public.trade_in_quote_items USING btree (quote_id, item_id)
```


### Triggers

**`trade_quote_append_guard`** — enabled `O`.

```sql
CREATE TRIGGER trade_quote_append_guard BEFORE INSERT ON trade_in_quote_items FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_append()
```

**`trade_quote_item_history_guard`** — enabled `O`.

```sql
CREATE TRIGGER trade_quote_item_history_guard BEFORE DELETE OR UPDATE ON trade_in_quote_items FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_history()
```


## trade_in_quotes

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | UNIQUE: trade_in_quotes_id_request_id_key; PK: trade_in_quotes_pkey | — | — |
| request_id | uuid | No | — | UNIQUE: trade_in_quotes_id_request_id_key; FK → trade_in_requests: trade_in_quotes_request_id_fkey; UNIQUE: trade_in_quotes_request_id_version_no_key | — | — |
| version_no | integer | No | — | UNIQUE: trade_in_quotes_request_id_version_no_key | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| quoted_by | text | No | — | FK → user: trade_in_quotes_quoted_by_fkey | — | — |
| approved_by | text | Yes | — | FK → user: trade_in_quotes_approved_by_fkey | — | — |

### Constraints

**`trade_in_quotes_approved_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (approved_by) REFERENCES "user"(id)
```

**`trade_in_quotes_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`trade_in_quotes_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_quotes_id_request_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (id, request_id)
```

**`trade_in_quotes_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`trade_in_quotes_quoted_by_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (quoted_by) REFERENCES "user"(id)
```

**`trade_in_quotes_quoted_by_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL quoted_by
```

**`trade_in_quotes_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (request_id) REFERENCES trade_in_requests(id)
```

**`trade_in_quotes_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_id
```

**`trade_in_quotes_request_id_version_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (request_id, version_no)
```

**`trade_in_quotes_version_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_no
```


### Indexes

**`trade_in_quotes_id_request_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_quotes_id_request_id_key ON public.trade_in_quotes USING btree (id, request_id)
```

**`trade_in_quotes_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_quotes_pkey ON public.trade_in_quotes USING btree (id)
```

**`trade_in_quotes_request_id_version_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_quotes_request_id_version_no_key ON public.trade_in_quotes USING btree (request_id, version_no)
```


### Triggers

**`trade_quote_history_guard`** — enabled `O`.

```sql
CREATE TRIGGER trade_quote_history_guard BEFORE DELETE OR UPDATE ON trade_in_quotes FOR EACH ROW EXECUTE FUNCTION guard_trade_quote_history()
```


## trade_in_requests

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: trade_in_requests_pkey | — | — |
| customer_id | text | No | — | FK → user: trade_in_requests_customer_id_fkey | — | — |
| reception_method | text | No | — | — | — | — |
| state | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`trade_in_requests_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`trade_in_requests_customer_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (customer_id) REFERENCES "user"(id)
```

**`trade_in_requests_customer_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL customer_id
```

**`trade_in_requests_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_requests_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`trade_in_requests_reception_method_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (reception_method = ANY (ARRAY['store'::text, 'remote'::text]))
```

**`trade_in_requests_reception_method_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL reception_method
```

**`trade_in_requests_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'quoted'::text, 'contact_pending'::text, 'completed'::text, 'returned'::text, 'exception'::text]))
```

**`trade_in_requests_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```


### Indexes

**`trade_in_requests_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_requests_pkey ON public.trade_in_requests USING btree (id)
```


### Triggers

None.

## trade_in_shipments

Domain: Returns & trade in.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: trade_in_shipments_pkey | — | — |
| request_id | uuid | No | — | FK → trade_in_requests: trade_in_shipments_request_id_fkey | — | — |
| direction | text | No | — | — | — | — |
| tracking_no | text | No | — | UNIQUE: trade_in_shipments_tracking_no_key | — | — |
| fee_vnd | bigint | No | — | — | — | — |
| state | text | No | — | — | — | — |

### Constraints

**`trade_in_shipments_direction_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (direction = ANY (ARRAY['inbound'::text, 'outbound'::text]))
```

**`trade_in_shipments_direction_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL direction
```

**`trade_in_shipments_fee_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (fee_vnd >= 0)
```

**`trade_in_shipments_fee_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL fee_vnd
```

**`trade_in_shipments_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`trade_in_shipments_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`trade_in_shipments_request_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (request_id) REFERENCES trade_in_requests(id)
```

**`trade_in_shipments_request_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL request_id
```

**`trade_in_shipments_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['created'::text, 'in_transit'::text, 'delivered'::text, 'exception'::text]))
```

**`trade_in_shipments_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```

**`trade_in_shipments_tracking_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (tracking_no)
```

**`trade_in_shipments_tracking_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL tracking_no
```


### Indexes

**`trade_in_shipments_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_shipments_pkey ON public.trade_in_shipments USING btree (id)
```

**`trade_in_shipments_tracking_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX trade_in_shipments_tracking_no_key ON public.trade_in_shipments USING btree (tracking_no)
```


### Triggers

None.

## user

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | text | No | — | PK: user_pkey | — | — |
| name | text | No | — | — | — | — |
| email | text | No | — | UNIQUE: user_email_key | — | — |
| email_verified | boolean | No | false | — | — | — |
| image | text | Yes | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| updated_at | timestamp with time zone | No | now() | — | — | — |
| access_failed_count | integer | No | 0 | — | — | — |
| concurrency_stamp | text | Yes | — | — | — | — |
| lockout_enabled | boolean | No | false | — | — | — |
| lockout_end | timestamp with time zone | Yes | — | — | — | — |
| normalized_email | character varying(256) | Yes | — | — | — | — |
| normalized_user_name | character varying(256) | Yes | — | — | — | — |
| password_hash | text | Yes | — | — | — | — |
| password_reset_required | boolean | No | false | — | — | — |
| phone_number | text | Yes | — | — | — | — |
| phone_number_confirmed | boolean | No | false | — | — | — |
| security_stamp | text | Yes | — | — | — | — |
| two_factor_enabled | boolean | No | false | — | — | — |
| user_name | character varying(256) | Yes | — | — | — | — |

### Constraints

**`user_access_failed_count_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL access_failed_count
```

**`user_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`user_email_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (email)
```

**`user_email_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL email
```

**`user_email_verified_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL email_verified
```

**`user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`user_lockout_enabled_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL lockout_enabled
```

**`user_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`user_password_reset_required_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL password_reset_required
```

**`user_phone_number_confirmed_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL phone_number_confirmed
```

**`user_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`user_two_factor_enabled_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL two_factor_enabled
```

**`user_updated_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL updated_at
```


### Indexes

**`EmailIndex`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "EmailIndex" ON public."user" USING btree (normalized_email)
```

**`UserNameIndex`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX "UserNameIndex" ON public."user" USING btree (normalized_user_name)
```

**`user_email_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX user_email_key ON public."user" USING btree (email)
```

**`user_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX user_pkey ON public."user" USING btree (id)
```


### Triggers

None.

## variant_attribute_values

Domain: Catalogue.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| variant_id | uuid | No | — | PK: variant_attribute_values_pkey; FK → product_variants: variant_attribute_values_variant_id_fkey | — | — |
| attribute_id | uuid | No | — | FK → attribute_definitions: variant_attribute_values_attribute_id_fkey; PK: variant_attribute_values_pkey | — | — |
| value_text | text | Yes | — | — | — | — |
| value_number | numeric | Yes | — | — | — | — |
| value_boolean | boolean | Yes | — | — | — | — |

### Constraints

**`variant_attribute_values_attribute_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (attribute_id) REFERENCES attribute_definitions(id)
```

**`variant_attribute_values_attribute_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL attribute_id
```

**`variant_attribute_values_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (num_nonnulls(value_text, value_number, value_boolean) = 1)
```

**`variant_attribute_values_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (variant_id, attribute_id)
```

**`variant_attribute_values_variant_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (variant_id) REFERENCES product_variants(id)
```

**`variant_attribute_values_variant_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL variant_id
```


### Indexes

**`variant_attribute_values_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX variant_attribute_values_pkey ON public.variant_attribute_values USING btree (variant_id, attribute_id)
```


### Triggers

None.

## verification

Domain: Identity & people.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | text | No | — | PK: verification_pkey | — | — |
| identifier | text | No | — | — | — | — |
| value | text | No | — | — | — | — |
| expires_at | timestamp with time zone | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| updated_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`verification_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`verification_expires_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL expires_at
```

**`verification_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`verification_identifier_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL identifier
```

**`verification_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`verification_updated_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL updated_at
```

**`verification_value_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL value
```


### Indexes

**`verification_identifier_idx`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX verification_identifier_idx ON public.verification USING btree (identifier)
```

**`verification_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX verification_pkey ON public.verification USING btree (id)
```


### Triggers

None.

## warehouses

Domain: Inventory & purchasing.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warehouses_pkey | — | — |
| code | text | No | — | UNIQUE: warehouses_code_key | — | — |
| name | text | No | — | — | — | — |

### Constraints

**`warehouses_code_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (code = 'MAIN'::text)
```

**`warehouses_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code)
```

**`warehouses_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`warehouses_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warehouses_name_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL name
```

**`warehouses_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`warehouses_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warehouses_code_key ON public.warehouses USING btree (code)
```

**`warehouses_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warehouses_pkey ON public.warehouses USING btree (id)
```


### Triggers

None.

## warranty_claims

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_claims_pkey | — | — |
| entitlement_id | uuid | No | — | FK → warranty_entitlements: warranty_claims_entitlement_id_fkey | — | — |
| state | text | No | — | — | — | — |
| description | text | No | — | — | — | — |
| damage_kind | text | No | — | — | — | — |
| inspection_result | text | Yes | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`warranty_claims_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`warranty_claims_damage_kind_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (damage_kind = ANY (ARRAY['hardware'::text, 'accidental'::text, 'liquid'::text]))
```

**`warranty_claims_damage_kind_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL damage_kind
```

**`warranty_claims_description_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL description
```

**`warranty_claims_entitlement_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (entitlement_id) REFERENCES warranty_entitlements(id)
```

**`warranty_claims_entitlement_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL entitlement_id
```

**`warranty_claims_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_claims_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`warranty_claims_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['requested'::text, 'received'::text, 'inspected'::text, 'repairing'::text, 'repaired'::text, 'replace'::text, 'replaced'::text, 'refund'::text, 'refunded'::text, 'rejected'::text, 'closed'::text]))
```

**`warranty_claims_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```


### Indexes

**`warranty_claims_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_claims_pkey ON public.warranty_claims USING btree (id)
```


### Triggers

None.

## warranty_entitlements

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_entitlements_pkey | — | — |
| order_item_unit_amount_id | uuid | No | — | FK → order_item_unit_amounts: warranty_entitlements_order_item_unit_amount_id_fkey; UNIQUE: warranty_entitlements_order_item_unit_amount_id_key | — | — |
| plan_version_id | uuid | No | — | FK → warranty_plan_versions: warranty_entitlements_plan_version_id_fkey | — | — |
| inventory_unit_id | uuid | Yes | — | FK → inventory_units: warranty_entitlements_inventory_unit_id_fkey | — | — |
| starts_on | date | Yes | — | — | — | — |
| ends_on | date | Yes | — | — | — | — |
| state | text | No | — | — | — | — |
| used | boolean | No | false | — | — | — |

### Constraints

**`warranty_entitlements_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (ends_on IS NULL OR starts_on < ends_on)
```

**`warranty_entitlements_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_entitlements_inventory_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (inventory_unit_id) REFERENCES inventory_units(id)
```

**`warranty_entitlements_order_item_unit_amount_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id)
```

**`warranty_entitlements_order_item_unit_amount_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (order_item_unit_amount_id)
```

**`warranty_entitlements_order_item_unit_amount_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL order_item_unit_amount_id
```

**`warranty_entitlements_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`warranty_entitlements_plan_version_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (plan_version_id) REFERENCES warranty_plan_versions(id)
```

**`warranty_entitlements_plan_version_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL plan_version_id
```

**`warranty_entitlements_state_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (state = ANY (ARRAY['pending'::text, 'active'::text, 'cancelled'::text, 'expired'::text, 'refunded'::text]))
```

**`warranty_entitlements_state_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL state
```

**`warranty_entitlements_used_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL used
```


### Indexes

**`warranty_entitlements_order_item_unit_amount_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_entitlements_order_item_unit_amount_id_key ON public.warranty_entitlements USING btree (order_item_unit_amount_id)
```

**`warranty_entitlements_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_entitlements_pkey ON public.warranty_entitlements USING btree (id)
```


### Triggers

None.

## warranty_events

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_events_pkey | — | — |
| claim_id | uuid | No | — | FK → warranty_claims: warranty_events_claim_id_fkey | — | — |
| actor_id | text | No | — | FK → user: warranty_events_actor_id_fkey | — | — |
| event | text | No | — | — | — | — |
| note | text | No | — | — | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |

### Constraints

**`warranty_events_actor_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (actor_id) REFERENCES "user"(id)
```

**`warranty_events_actor_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL actor_id
```

**`warranty_events_claim_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (claim_id) REFERENCES warranty_claims(id)
```

**`warranty_events_claim_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL claim_id
```

**`warranty_events_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`warranty_events_event_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL event
```

**`warranty_events_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_events_note_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL note
```

**`warranty_events_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`warranty_events_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_events_pkey ON public.warranty_events USING btree (id)
```


### Triggers

None.

## warranty_evidence

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| claim_id | uuid | No | — | FK → warranty_claims: warranty_evidence_claim_id_fkey; PK: warranty_evidence_pkey | — | — |
| media_id | uuid | No | — | FK → media_assets: warranty_evidence_media_id_fkey; PK: warranty_evidence_pkey | — | — |

### Constraints

**`warranty_evidence_claim_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (claim_id) REFERENCES warranty_claims(id)
```

**`warranty_evidence_claim_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL claim_id
```

**`warranty_evidence_media_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (media_id) REFERENCES media_assets(id)
```

**`warranty_evidence_media_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL media_id
```

**`warranty_evidence_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (claim_id, media_id)
```


### Indexes

**`warranty_evidence_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_evidence_pkey ON public.warranty_evidence USING btree (claim_id, media_id)
```


### Triggers

None.

## warranty_plan_versions

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_plan_versions_pkey | — | — |
| plan_id | uuid | No | — | FK → warranty_plans: warranty_plan_versions_plan_id_fkey; UNIQUE: warranty_plan_versions_plan_id_version_no_key | — | — |
| version_no | integer | No | — | UNIQUE: warranty_plan_versions_plan_id_version_no_key | — | — |
| term_months | integer | No | — | — | — | — |
| fee_vnd | bigint | No | — | — | — | — |
| policy_snapshot | jsonb | No | — | — | — | — |

### Constraints

**`warranty_plan_versions_fee_vnd_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (fee_vnd >= 0)
```

**`warranty_plan_versions_fee_vnd_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL fee_vnd
```

**`warranty_plan_versions_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_plan_versions_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```

**`warranty_plan_versions_plan_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (plan_id) REFERENCES warranty_plans(id)
```

**`warranty_plan_versions_plan_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL plan_id
```

**`warranty_plan_versions_plan_id_version_no_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (plan_id, version_no)
```

**`warranty_plan_versions_policy_snapshot_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL policy_snapshot
```

**`warranty_plan_versions_term_months_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (term_months = 12)
```

**`warranty_plan_versions_term_months_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL term_months
```

**`warranty_plan_versions_version_no_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL version_no
```


### Indexes

**`warranty_plan_versions_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_plan_versions_pkey ON public.warranty_plan_versions USING btree (id)
```

**`warranty_plan_versions_plan_id_version_no_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_plan_versions_plan_id_version_no_key ON public.warranty_plan_versions USING btree (plan_id, version_no)
```


### Triggers

None.

## warranty_plans

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_plans_pkey | — | — |
| code | text | No | — | UNIQUE: warranty_plans_code_key | — | — |
| active | boolean | No | true | — | — | — |

### Constraints

**`warranty_plans_active_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL active
```

**`warranty_plans_code_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (code)
```

**`warranty_plans_code_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL code
```

**`warranty_plans_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_plans_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`warranty_plans_code_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_plans_code_key ON public.warranty_plans USING btree (code)
```

**`warranty_plans_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_plans_pkey ON public.warranty_plans USING btree (id)
```


### Triggers

None.

## warranty_replacements

Domain: Warranty.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| id | uuid | No | gen_random_uuid() | PK: warranty_replacements_pkey | — | — |
| claim_id | uuid | No | — | FK → warranty_claims: warranty_replacements_claim_id_fkey; UNIQUE: warranty_replacements_claim_id_key | — | — |
| old_unit_id | uuid | Yes | — | FK → inventory_units: warranty_replacements_old_unit_id_fkey | — | — |
| new_unit_id | uuid | Yes | — | FK → inventory_units: warranty_replacements_new_unit_id_fkey; UNIQUE: warranty_replacements_new_unit_id_key | — | — |
| created_at | timestamp with time zone | No | now() | — | — | — |
| new_lot_id | uuid | Yes | — | FK → inventory_lots: FK_warranty_replacements_inventory_lots_new_lot_id | — | — |
| old_lot_id | uuid | Yes | — | FK → inventory_lots: FK_warranty_replacements_inventory_lots_old_lot_id | — | — |
| order_item_unit_amount_id | uuid | Yes | — | FK → order_item_unit_amounts: FK_warranty_replacements_order_item_unit_amounts_order_item_un~ | — | — |

### Constraints

**`FK_warranty_replacements_inventory_lots_new_lot_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (new_lot_id) REFERENCES inventory_lots(id) ON DELETE RESTRICT
```

**`FK_warranty_replacements_inventory_lots_old_lot_id`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (old_lot_id) REFERENCES inventory_lots(id) ON DELETE RESTRICT
```

**`FK_warranty_replacements_order_item_unit_amounts_order_item_un~`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (order_item_unit_amount_id) REFERENCES order_item_unit_amounts(id) ON DELETE RESTRICT
```

**`replacement_tracking_source_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (old_unit_id IS NOT NULL AND new_unit_id IS NOT NULL AND old_lot_id IS NULL AND new_lot_id IS NULL OR old_unit_id IS NULL AND new_unit_id IS NULL AND old_lot_id IS NOT NULL AND new_lot_id IS NOT NULL)
```

**`warranty_replacements_check`** — type `c`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
CHECK (old_unit_id <> new_unit_id)
```

**`warranty_replacements_claim_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (claim_id) REFERENCES warranty_claims(id)
```

**`warranty_replacements_claim_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (claim_id)
```

**`warranty_replacements_claim_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL claim_id
```

**`warranty_replacements_created_at_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL created_at
```

**`warranty_replacements_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL id
```

**`warranty_replacements_new_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (new_unit_id) REFERENCES inventory_units(id)
```

**`warranty_replacements_new_unit_id_key`** — type `u`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
UNIQUE (new_unit_id)
```

**`warranty_replacements_old_unit_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (old_unit_id) REFERENCES inventory_units(id)
```

**`warranty_replacements_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (id)
```


### Indexes

**`IX_warranty_replacements_new_lot_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_warranty_replacements_new_lot_id" ON public.warranty_replacements USING btree (new_lot_id)
```

**`IX_warranty_replacements_old_lot_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_warranty_replacements_old_lot_id" ON public.warranty_replacements USING btree (old_lot_id)
```

**`IX_warranty_replacements_order_item_unit_amount_id`** — unique `False`, primary `False`, valid `True`.

```sql
CREATE INDEX "IX_warranty_replacements_order_item_unit_amount_id" ON public.warranty_replacements USING btree (order_item_unit_amount_id)
```

**`warranty_replacements_claim_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_replacements_claim_id_key ON public.warranty_replacements USING btree (claim_id)
```

**`warranty_replacements_new_unit_id_key`** — unique `True`, primary `False`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_replacements_new_unit_id_key ON public.warranty_replacements USING btree (new_unit_id)
```

**`warranty_replacements_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX warranty_replacements_pkey ON public.warranty_replacements USING btree (id)
```


### Triggers

**`replacement_immutable`** — enabled `O`.

```sql
CREATE TRIGGER replacement_immutable BEFORE DELETE OR UPDATE ON warranty_replacements FOR EACH ROW EXECUTE FUNCTION reject_mutation()
```

**`replacement_source_guard`** — enabled `O`.

```sql
CREATE TRIGGER replacement_source_guard BEFORE INSERT ON warranty_replacements FOR EACH ROW EXECUTE FUNCTION guard_warranty_replacement_source()
```


## wishlists

Domain: Cart & promotions.

| Column | PostgreSQL type | NULL | Default | Key membership | Identity/generated | Comment |
| --- | --- | --- | --- | --- | --- | --- |
| user_id | text | No | — | PK: wishlists_pkey; FK → user: wishlists_user_id_fkey | — | — |
| product_id | uuid | No | — | PK: wishlists_pkey; FK → products: wishlists_product_id_fkey | — | — |

### Constraints

**`wishlists_pkey`** — type `p`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
PRIMARY KEY (user_id, product_id)
```

**`wishlists_product_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (product_id) REFERENCES products(id)
```

**`wishlists_product_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL product_id
```

**`wishlists_user_id_fkey`** — type `f`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
FOREIGN KEY (user_id) REFERENCES "user"(id)
```

**`wishlists_user_id_not_null`** — type `n`, validated `True`, deferrable `False`, initially deferred `False`.

```sql
NOT NULL user_id
```


### Indexes

**`wishlists_pkey`** — unique `True`, primary `True`, valid `True`.

```sql
CREATE UNIQUE INDEX wishlists_pkey ON public.wishlists USING btree (user_id, product_id)
```


### Triggers

None.

## Enum types


## Views

### variant_stock

```sql
 SELECT id AS variant_id,
        CASE
            WHEN tracking_mode = 'serialized'::text THEN ( SELECT count(*) AS count
               FROM inventory_units u
                 JOIN inventory_lots l ON l.id = u.lot_id
              WHERE l.variant_id = v.id AND u.custody_status = 'available'::text AND u.condition_status = 'new'::text)
            ELSE COALESCE(( SELECT sum(l.qty_on_hand - l.qty_reserved) AS sum
               FROM inventory_lots l
              WHERE l.variant_id = v.id), 0::bigint)
        END AS available
   FROM product_variants v;
```

## Database functions

Full function bodies are preserved in `schema-snapshot.json`; these include trigger guards that enforce financial/custody invariants beyond ordinary CHECK/FK constraints.

- `armor(bytea)`
- `armor(bytea, text[], text[])`
- `check_credit_note()`
- `check_invoice_totals()`
- `check_ledger_source()`
- `check_order_totals()`
- `check_refund()`
- `check_tender()`
- `cms_permission()`
- `crypt(text, text)`
- `dearmor(text)`
- `decrypt(bytea, bytea, text)`
- `decrypt_iv(bytea, bytea, bytea, text)`
- `digest(text, text)`
- `digest(bytea, text)`
- `encrypt(bytea, bytea, text)`
- `encrypt_iv(bytea, bytea, bytea, text)`
- `fips_mode()`
- `gen_random_bytes(integer)`
- `gen_random_uuid()`
- `gen_salt(text, integer)`
- `gen_salt(text)`
- `guard_credit_child()`
- `guard_credit_note()`
- `guard_inventory_history()`
- `guard_inventory_sources()`
- `guard_invoice()`
- `guard_invoice_item()`
- `guard_order_header()`
- `guard_order_snapshot()`
- `guard_promotion_target()`
- `guard_quarantine_source()`
- `guard_refund_mutation()`
- `guard_trade_quote_append()`
- `guard_trade_quote_history()`
- `guard_warranty_replacement_source()`
- `hmac(bytea, bytea, text)`
- `hmac(text, text, text)`
- `pgp_armor_headers(text, OUT key text, OUT value text)`
- `pgp_key_id(bytea)`
- `pgp_pub_decrypt(bytea, bytea)`
- `pgp_pub_decrypt(bytea, bytea, text)`
- `pgp_pub_decrypt(bytea, bytea, text, text)`
- `pgp_pub_decrypt_bytea(bytea, bytea, text)`
- `pgp_pub_decrypt_bytea(bytea, bytea)`
- `pgp_pub_decrypt_bytea(bytea, bytea, text, text)`
- `pgp_pub_encrypt(text, bytea, text)`
- `pgp_pub_encrypt(text, bytea)`
- `pgp_pub_encrypt_bytea(bytea, bytea)`
- `pgp_pub_encrypt_bytea(bytea, bytea, text)`
- `pgp_sym_decrypt(bytea, text, text)`
- `pgp_sym_decrypt(bytea, text)`
- `pgp_sym_decrypt_bytea(bytea, text, text)`
- `pgp_sym_decrypt_bytea(bytea, text)`
- `pgp_sym_encrypt(text, text)`
- `pgp_sym_encrypt(text, text, text)`
- `pgp_sym_encrypt_bytea(bytea, text)`
- `pgp_sym_encrypt_bytea(bytea, text, text)`
- `reject_mutation()`
- `used_promotion_immutable()`
