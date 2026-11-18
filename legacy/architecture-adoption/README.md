# Historical architecture-adoption scripts

These scripts record one-time work performed on 2026-11-13 while adopting the two existing SQL migrations into EF Core. They are retained for traceability and are not runnable maintenance tools. Their old relative paths intentionally refer to the original workspace layout.

Do not regenerate the baseline fingerprints from a changed database or rerun source-rewrite scripts. Current migrations are owned solely by EF Core and invoked through `scripts/migrate-ef.ps1`. Current read-only schema documentation uses `docs/database/capture-schema.py`; backup comparison uses `tests/BotZone.DatabaseVerification`.
