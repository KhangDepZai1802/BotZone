-- BotZone BA v1.1: read-only owner verification against the seeded test clone.
-- This file does not grant API access or replace actor-scoped service queries.
-- Golden IDs are isolated from pagination and browser-created invoices.
-- Exact production SQL + bound Npgsql types + actual plans: ../../tests/BotZone.IntegrationTests/query-plans.json
BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY;

-- Q01 / DB-01: promotion version and durable redemption status/amount.
SELECT p.code_normalized, v.version_no, r.status, count(*) AS redemption_count,
       sum(r.amount_vnd) AS allocated_discount_vnd
FROM promotion_redemptions r
JOIN promotions p ON p.id=r.promotion_id
JOIN promotion_versions v ON v.id=r.promotion_version_id
JOIN orders o ON o.id=r.order_id
WHERE o.order_no='DEMO-G08'
GROUP BY p.code_normalized,v.version_no,r.status;

-- Q02 / DB-02, DB-04: 8 issued, 97,130,000 VND. No D01 draft or PAG rows.
SELECT count(*) AS invoice_count, sum(grand_total_vnd) AS total_vnd
FROM invoices
WHERE lifecycle='issued'
  AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08');

-- Q03 / DB-03: one row per invoice. Child aggregates cannot multiply totals.
SELECT i.invoice_no, i.grand_total_vnd,
       COALESCE(c.total_vnd,0) AS adjustments_vnd,
       i.grand_total_vnd-COALESCE(c.total_vnd,0) AS net_vnd
FROM invoices i
LEFT JOIN LATERAL (
    SELECT sum(total_vnd) AS total_vnd FROM credit_notes
    WHERE invoice_id=i.id AND status='issued'
) c ON TRUE
WHERE i.lifecycle='issued'
  AND i.invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
ORDER BY i.issued_at,i.id;

-- Q05A / DB-05A: 36 year-qualified months including empty months = 0.
WITH months AS (
    SELECT generate_series(timestamp '2024-01-01',timestamp '2026-12-01',interval '1 month') AS period
), facts AS (
    SELECT date_trunc('month',issued_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AS period,
           sum(grand_total_vnd) AS total_vnd
    FROM invoices
    WHERE lifecycle='issued'
      AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
    GROUP BY 1
)
SELECT to_char(m.period,'YYYY-MM') AS period,COALESCE(f.total_vnd,0) AS total_vnd
FROM months m LEFT JOIN facts f ON f.period=m.period ORDER BY m.period;

-- Q05B / DB-05B: NV01 42,800,000; NV02 44,500,000; NV03 9,830,000.
SELECT responsible_staff_id, sum(grand_total_vnd) AS total_vnd
FROM invoices
WHERE lifecycle='issued'
  AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
GROUP BY responsible_staff_id ORDER BY responsible_staff_id;

-- Q05C / DB-05C: product 94,800,000; warranty 2,300,000; shipping 30,000.
SELECT ii.line_type,p.model_code,sum(ii.net_amount_vnd) AS total_vnd
FROM invoices i JOIN invoice_items ii ON ii.invoice_id=i.id
LEFT JOIN products p ON p.id=ii.product_id
WHERE i.lifecycle='issued'
  AND i.invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
GROUP BY ii.line_type,p.model_code ORDER BY ii.line_type,p.model_code;

-- Q06 / DB-06: G02,G03,G07 = 46,800,000. Product NOT is a NOT EXISTS.
SELECT i.invoice_no,i.grand_total_vnd
FROM invoices i
WHERE i.lifecycle='issued'
  AND i.invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
  AND (i.grand_total_vnd>10000000 AND i.grand_total_vnd<20000000)
  AND (i.responsible_staff_id='demo-nv01' OR i.responsible_staff_id='demo-nv02')
  AND NOT EXISTS (
      SELECT 1 FROM invoice_items ii JOIN products p ON p.id=ii.product_id
      WHERE ii.invoice_id=i.id AND ii.line_type='product' AND p.model_code='AP2'
  )
ORDER BY i.invoice_no,i.id;

-- Q07 / DB-07: export metadata, one dataset checksum, real file checksums.
-- Inspect only in an authorized DB maintenance session; app downloads recheck actor.
SELECT id,read_at,row_count,total_vnd,status,
       file_manifest->>'datasetChecksum' AS dataset_checksum,
       file_manifest->'files'->'xlsx'->>'checksum' AS xlsx_checksum,
       file_manifest->'files'->'pdf'->>'checksum' AS pdf_checksum
FROM invoice_exports ORDER BY read_at DESC,id DESC LIMIT 5;

-- Q08 / DB-T19: CN01 belongs to May, even though G04 issued in April.
SELECT c.credit_note_no,i.invoice_no,
       i.issued_at AT TIME ZONE 'Asia/Ho_Chi_Minh' AS invoice_date,
       c.issued_at AT TIME ZONE 'Asia/Ho_Chi_Minh' AS adjustment_date,c.total_vnd
FROM credit_notes c JOIN invoices i ON i.id=c.invoice_id
WHERE c.status='issued' AND c.credit_note_no IN ('CN01','CN02')
ORDER BY c.issued_at,c.id;

COMMIT;
