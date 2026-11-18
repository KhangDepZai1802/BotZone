# BotZone storefront design system

The customer website uses a quiet black and white layout with Vietnamese-capable sans serif typography. Burgundy introduces the first hero; a cool silver/blue palette introduces Duo second. CMS content determines section order. Administrative work belongs exclusively to the Windows Forms application.

## Foundation

Tokens live in `src/styles/globals.css`. Background `#ffffff`, foreground `#171719`, muted background `#f5f5f6`, secondary text `#636367`, border `#e3e3e5`, primary `#151517`, danger `#a42c35`, success `#24654b`. Burgundy `#6b263d` and ice `#a4bac9` are representative interface colors, not official Apple color measurements. `asset-sources.json` records the distinction and local color samples.

Noto Sans is served locally from `public/fonts/NotoSans-Regular.ttf` under OFL. Regular is a static 400-weight font; browsers synthesize heavier display weights. The same TTF is available to server-side PDF export. Body text uses 14px with a 1.6 line height; headings use fluid sizing. Spacing follows an 8px base with 18px standard surface radii, restrained shadows and wide editorial margins.

## Components and interaction

`src/components/ui.tsx` provides Radix Dialog/Slot and shadcn-style Button, Input, Select, Textarea, Field, Badge, Empty, Busy and ErrorState primitives. Buttons have visible keyboard focus. Inputs have persistent labels; errors are live alerts and success notices use status. Modal focus containment and Escape handling come from Radix; the 3D viewer restores trigger focus and the page scroll position. Tables keep overflow within their containing surface.

The header preserves route/query parameters on language changes; currency is independent and stored locally. VND strings remain integer values for every API mutation. USD is a display conversion only. Empty, loading, unavailable-stock and backend-error states keep navigation usable. No form simulates successful backend work.

### Storefront interaction update — 13 November 2026

All shared Select callers now use `@radix-ui/react-select` 2.4.1 through `select.tsx`, preserving native options/value/onChange and FormData. The visible trigger/listbox supports typeahead, keyboard arrows/Home/End, Escape, focus return, disabled options and collision-aware portals. A visually hidden native control preserves required validation and empty values; invalid submission focuses the trigger and exposes a message. Header, catalogue, checkout, after-sales and preview controls share the same monochrome surface, selected Burgundy text and 44px option targets.

Catalogue/wishlist pagination uses six cards below 768px and twelve above it. The URL carries page, search, category, sort and stock. Filter changes reset page; native history preserves Back/Forward. `url-state.ts` synchronizes controlled inputs and locale links immediately with the browser URL while Next commits its navigation transition. Out-of-range pages clamp after real catalogue loading; page links focus/scroll to the result count, and the count is announced politely. Product cards use the API poster; detail/cart use optional API photo metadata matched to the selected variant color.

Motion uses 100–220ms opacity/transform transitions for page entry, menu/dialog entry/exit and presses, with a 320ms image hover. No route-transition experiment, scroll lock, or scroll pinning was added. Reduced motion removes these animations and interaction transforms. Touch does not rely on hover. Dialog dismissal restores the opener without changing scroll position.

`tests/web/interactions-live.mjs` passed 13 checks against the real local API on 13 November: all seven BA widths, desktop/mobile pagination, history, filters/language, touch dropdowns, portal bounds, dialog focus and reduced motion, with no mutating API requests and no uncaught browser errors. `tests/web/select-component.mjs` passed at 390/1440px using the actual shared components in an isolated Edge fixture, covering required/empty/FormData, dependent disabled controls, typeahead and nested Escape/focus. Evidence is `artifacts/web/interactions-results.json` and `select-component-results.json`; these checks do not claim physical-phone testing.

The v003 landing layout reserves a full poster stage before the dynamically imported scene mounts. Pro Max has a large light-background stage beside its copy on laptop; Duo has a dark, wider landscape stage. Mobile stacks text, CTA and model in normal document flow. Scene heights remain at least 370px (Pro Max) and 340px (Duo) on mobile. CMS title/subtitle/product CTA data and the same public/preview renderer are retained. Model/animation runtime acceptance is documented separately by the v003 integration tests.

## Product imagery and motion

Catalogue cards use verified Apple photography with neutral white frames, contain sizing and individually recorded framing for unusually large source-canvas margins. `data/product-photos.json` supplies provenance and color mappings through the C# API. Product detail and bag images follow the selected variant. The catalogue renders 6 items per mobile page and 12 per desktop page; URL state preserves search, category, availability, sort and page across navigation and language changes.

Radix Select replaces browser-native popup menus throughout the customer interface. Its portal, keyboard selection, typeahead, Escape/focus return, disabled options, empty values and form validation share the same typography and rounded neutral surfaces. Buttons, cards, route changes, dropdowns, dialogs and pagination use restrained opacity/transform feedback; reduced motion removes automatic movement while retaining image framing and focus feedback.

The supplied v003 WebP posters are the landing's first paint and remain visible under reduced motion, missing WebGL or model-load failure. The dynamically imported R3F scene loads only for the active visible hero. A shared scene selector keeps one canvas active; exploration suspends inline scenes. Page visibility and intersection changes suspend unneeded rendering. Pointer tilt affects a presentation parent, separate from animated GLB nodes. Free rotation is confined to explicit exploration while inline canvas uses `touch-action: pan-y`.

`public/models/v003` contains four self-contained GLBs and two matching posters from the user-supplied Blender handoff. The explicit CMS identifiers are `v003:iphone-18-pro-max` and `v003:iphone-duo`; legacy identifiers remain compatible. Runtime verifies each GLB SHA-256, preserves original paint by default and uses the full `Duo_OpenClose` clip including rotation, translation, morph and STEP scale. The OLED and permanent outer-glass material contracts are recorded in [the integration document](3d/v003/integration.md). Old illustrative GLBs and their generator are preserved for history.

## Storefront modules and backend boundary

`storefront.tsx` routes catalogue, product detail and the storefront pages. `commerce.tsx` implements cart, quote confirmation, Identity auth, orders, payment demos, returns and customer invoices. `account-services.tsx` implements profile/address, store credit and loyalty ledgers, notifications, wishlist/compare, trade-in and warranty. `landing-renderer.tsx` is shared by published landing and the restricted CMS preview. Preview mobile/tablet widths use an iframe so the same responsive media queries run at the selected viewport width.

`client.tsx` centralizes same-origin API calls, antiforgery tokens, sessions, local guest cart, account cart merge, wishlist and compare. All mutations carry X-CSRF-TOKEN, and sensitive retries preserve command keys. Evidence is uploaded as private media associated with an owned order or entitlement. The browser receives only preview links and sends evidence IDs with the service request. Administrative authorization, prices, stock, final quotes, invoicing and financial ledgers are enforced by the ASP.NET API.

## Verification scope

Record actual browser and API results in the project test reports; this document is a design/implementation specification, not an assertion that acceptance tests passed. A disabled backend should produce explicit errors and unauthenticated states, never fixture-backed success.

### Verified locally on 13 November 2026

The following checks ran against Next port 3000, the ASP.NET API on port 5080, and the migrated PostgreSQL clone. Browser checks used installed Microsoft Edge through Playwright, without mocked commerce responses. JSON reports and screenshots are in `artifacts/web`.

| Script | Observed result |
| --- | --- |
| `tests/web/storefront-smoke.mjs` | 26 responsive/asset checks, navigation and local currency persistence passed. |
| `tests/web/landing-live.mjs` | Both locales passed all seven BA widths, plus intermediate resize widths. Mobile touch input scrolls naturally; one live GLB canvas, exploration, color, keyboard rotation, hinge, reset, focus/scroll return, reduced motion and unavailable-WebGL fallback passed. |
| `tests/web/catalogue-live.mjs` | Search/category/sort/availability survive language changes; three-product/same-category compare constraints; unavailable variant disabled; variant and guest wishlist persistence passed. |
| `tests/web/account-keyboard-live.mjs` | Account tabs support arrow keys, Home/End, roving focus, selected state and a labelled tab panel. |
| `tests/web/commerce-live.mjs` | Actual guest-cart max merge, customer login, quote/review/COD order, staff confirmation, warehouse handoff, customer invoice, cross-customer denial, delivery and COD reconciliation passed. |
| `tests/web/aftersales-live.mjs` | Customer buttons downloaded real PDF/XLSX; a delivered order produced a return with private evidence; trade-in submission passed. Owner/other-customer/anonymous evidence access matched policy. |
| `tests/web/trade-decision-live.mjs` | Warehouse reception/inspection and owner quote were followed by customer acceptance in the web UI; finalization added exactly 500,000 VND store credit. |
| `tests/web/cms-live.mjs` | Content save/publish/restore, owner write denial, stale revision rejection, immutable history, draft isolation, exact-revision preview, one-use redemption, HttpOnly restricted session, URL-code removal and revocation passed. Final published content has four sections. |
| `tests/web/media-scope-live.mjs` | A customer can read staff-uploaded proof for their own case; other customers/content staff/anonymous users are denied. External GLB resource URI rejected with HTTP 400. |
| `tests/web/MediaValidation.cs` | Eleven structural image/GLB validation cases passed, including local models, PNG CRC, cycles, truncated buffers and external resources. |

`npm run typecheck` passed after the integration fixes. Screenshots inspected include `landing-desktop.png`, `landing-mobile.png`, `duo-explore.png`, `cms-mobile-preview.png`, `customer-invoice-desktop.png` and `account-live-desktop.png`. The reproducible initial CMS document is `data/landing-demo.json`; catalogue source limitations and demo pricing are in `data/catalogue-source-notes.md`.

These results do not establish production load performance or acceptance on physical mobile hardware. Blender was unavailable, so the original GLB illustrations have not been inspected in Blender. JPEG/WebP validation checks file structure and dimensions rather than fully decoding pixels. Warranty UI is implemented and API-backed; this report does not claim a completed customer warranty settlement browser test. Administrative UI tests belong to the separate WinForms reports.

After the dropdown/pagination/photo/landing layout changes, `npm run typecheck`, `tests/web/catalogue-live.mjs`, `npm run test:e2e` (26 checks plus navigation/preferences) and `tests/web/interactions-live.mjs` (13 checks, no uncaught browser errors or API writes) passed again against the running local application. Reduced-motion v003 poster layouts were captured at 390, 430, 1366 and 1440px in `artifacts/web/landing-polish`, with no horizontal overflow. Interactive GLB rendering is covered by the dedicated v003 suite rather than these poster layout captures.

### Shopping feedback, comparison and account pages

Product specifications use bilingual labels and stable field IDs from the API. Detail pages show highlights and grouped specifications; comparison aligns the same fields, shades differences and offers a differences-only filter. Store configurations/prices remain separate from manufacturer-supported configurations. Sources are linked per model. Scrollable comparison tables retain product context and expose keyboard focus, a scrollbar and a scrolling hint.

The comparison dock shows up to three product thumbnails, immediate selection feedback and actions to remove one, clear all or open the matrix. The bag action transitions through pending/success, updates the header badge and opens a compact image confirmation near the bag. It does not lock scrolling or move focus. Escape, close and navigation dismiss it; hover/focus pause dismissal. Reduced motion removes movement. The normal-motion interaction was recorded in [the local Edge clip](../artifacts/web/compare-bag/shopping-feedback.webm).

Authentication uses a dedicated brand/back/language shell. Login and registration are separate routes; inputs support password visibility, confirmation, inline errors and a focusable error summary. Phone is required in the new registration form; delivery address is optional. “Remember me” persists only the authenticated cookie within the existing eight-hour session limit, never the password. Non-secret drafts survive an explicit language switch on the same auth screen in short-lived memory; passwords and tokens do not transfer. Order/invoice tables scroll internally on narrow screens.

The final VI/EN audit, 14 comparison layouts, 16 auth layouts and commerce regression are linked from [storefront implementation and evidence](storefront-ux-localization.md). These checks supplement the earlier reports without replacing their untested BA limitations.

### Laptop and comparison refinement

The 3D dialog now uses viewport-height grid rows so the model adapts to the space left by its heading and controls. A single side-switch action replaces the two Duo side buttons. Laptop reset/sliders remain visible without dialog scrolling; normal mobile scrolling remains available.

Comparison uses a compact sticky product identity row and a strictly monochrome surface: white headers, pale gray groups/differences, dark text and a thin black inset beside advantages. Bold values have a short named reason; keyboard/touch-accessible disclosures explain conditions without expanding every row by default. The engine matches units, measurement scenarios, chip families and configuration ranges for CPU/GPU, capacity, display, connectivity, physical and battery specifications. Manufacturer maxima remain separate from shop configurations; missing values and ambiguous trade-offs do not acquire invented winners. See [the comparison contract](product-comparison.md).

Auth has a continuous light champagne/rose/lilac background, an unboxed editorial introduction and a translucent white form panel. The same eye/remember/address/validation controls remain; no new assets or authentication behavior were introduced. Current verification is linked from [the refinement notes](storefront-ux-localization.md).

### Hero motion — 14 November 2026

The two inline phones respond to the mouse across the entire hero, including its copy: a more visible bounded tilt, slight roll and positional parallax, with approximately half a second to settle. The body returns softly to its resting pose when the pointer leaves. Text/buttons stay in place; no camera zoom is introduced. Passive input, fine-pointer gating, reduced-motion posters and touch scrolling remain. [Observed browser evidence](../artifacts/3d-v003/hero-pointer/README.md) covers both phones at laptop and desktop widths.

### Registration disclosure and control alignment — 17 November 2026

Registration's optional address section expands and collapses over320ms with a220ms fade and6px settling motion. The content stays mounted to preserve typed values and to reverse smoothly during repeated toggles. Closed content is disabled, inert and hidden from assistive technology; focus is returned to the checkbox when necessary. Reduced motion removes transitions. Password hints now state only the six-character minimum, matching the updated Identity policy.

In the3D exploration dialog, slider labels and values share one line and both range inputs use44px rows. Paint, sliders and actions align on desktop; mobile separates those groups into balanced rows within the viewport. Visible rotation copy is shorter while its keyboard-accessible label remains descriptive.
