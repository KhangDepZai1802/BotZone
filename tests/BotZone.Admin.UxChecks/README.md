# Native admin UX checks

Read-only business-data regression harness for the round 5 redesign. It uses normal local API authentication for the four demo roles, WinForms controls and guarded `Graphics.CopyFromScreen` captures. Authentication/logout requests are permitted; business commands are never submitted. The HTTP diagnostic audit records this distinction in `result.json`.

1. Run the local API and ensure the demo accounts already exist.
2. Publish the current admin using `scripts/build-admin.ps1 -Publish`.
3. Run `dotnet run --project tests/BotZone.Admin.UxChecks -c Release -- artifacts/winforms/ux-checks` from the repository root on the interactive Windows desktop.

Environment variables: `BOTZONE_API_URL` (default `http://127.0.0.1:5080`), `BOTZONE_SMOKE_PASSWORD` (the public local demo seed password by default), `BOTZONE_AUDIT_ASSEMBLY` (default `artifacts/admin/win-x64/BotZone.Admin.WinForms.dll`). No credentials or cookies are written to the report.

The loaded DLL must match the published DLL hash. Captures are rejected if the owned window is not foreground or is covered by another process. This is a desktop integration harness, not a headless unit test. Keep the desktop available for its duration. It does not seed data, change settings, place orders or publish content.

The report covers modules, roles, direct action forms, preservation of raw IDs/settings, chart totals/selection, signed/zero values, native date editing, dropdown/calendar appearance, sidebar hierarchy and button corner painting. Screenshots are additional review evidence; assertions alone are not a visual design acceptance.

Append `--overview-dialogs` to check only the Overview date and metric explanation dialogs, including visible footer/input/validation bounds, cancellation, inclusive single-day ranges, exact metric values, scrolling and native multiline text. These checks also run in the full workflow suite. Example: `dotnet run --project tests/BotZone.Admin.UxChecks -c Release -- artifacts/winforms/overview-dialogs/check --overview-dialogs`.
