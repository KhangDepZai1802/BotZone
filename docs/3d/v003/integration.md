# BotZone v003 landing integration

Source package supplied by Khang: `C:\Users\khang\OneDrive\Documents\iphone model 3d\BotZone_3D_Assets_v003\BotZone_3D_Assets_v003`. Its files were read and copied without editing the originals or rebuilding Blender. The copied manifest SHA-256 is `a178b405377b634f3760cad168d0e8ddf1d2a64152198d892258e20f2a2a248a`. Source contracts remain in `source/`; their original statements about unverified browser runtime describe the asset-authoring environment, not later BotZone checks.

## Files and ownership

- `public/models/v003/`: four GLBs (mobile/desktop for each phone), two WebP posters. Textures remain embedded; master PNGs and standalone preview HTML are not served or embedded in the landing.
- `src/components/v003-assets.ts`: versioned URLs, expected hashes and semantic paint presets.
- `src/components/model-runtime.ts`: GLTFLoader, full animation mixer, material handling, observations and resource disposal.
- `src/components/product-scene.tsx`: adaptive selection, lazy scene lifecycle, presentation camera/light, interaction and poster fallback.
- `src/components/landing-renderer.tsx` and `src/styles/globals.css`: shared published/preview renderer, responsive editorial layout and controls.
- `BundledHeroAssets.cs` / `LandingService.cs`: C# validates versioned model/poster allowlists and hashes when saving or publishing CMS content. No migration or additional backend was introduced.
- `LandingView.cs`: the existing native WinForms editor lists both v003 models/posters and retains legacy choices.

CMS identifiers: `v003:iphone-18-pro-max`, `v003:iphone-duo`. Default hero poses are Pro Max front/lock screen and Duo fully open/inner lock screen (`fold=180` in the existing CMS degree convention). The exploration slider expresses the same progress as 0–100%. Default materials preserve the artist's original paint; selecting a color applies only named chassis materials.

## Animation and material contract

`Duo_OpenClose` is sampled over its complete 0–2-second duration with `LoopOnce` and `clampWhenFinished`. All four tracks are retained: `HingePivot.quaternion`, `HingeSpine.position`, `ScreenInnerFlex.morphTargetInfluences`, `ScreenOuter.scale`. The action owns these nodes. The presentation parent supplies small orientation changes independently. Endpoint sampling does not wrap to time zero; reverse scrubbing restores the STEP-controlled outer screen.

`ScreenOuterGlass` remains present when the emissive screen is scaled to zero. `MAT_ScreenOuter` and `MAT_ScreenInner` retain black base color, embedded emissive maps and supplied emission strengths; only these screen materials have `toneMapped=false`. Textures use sRGB, `flipY=false`, mipmaps and anisotropy bounded by the GPU and 8. Inner UVs remain unchanged across fixed/flex/moving pieces. Body presets do not tint OLED or outer glass. The clock/date/control artwork is static, as supplied.

The viewport selects the mobile or desktop GLB before fetching. Only the active visible inline scene renders; exploration suspends inline scenes, hidden tabs stop them, and unmount releases geometry, materials, textures, image bitmaps and mixer bindings. DPR is capped at 1.5 on mobile and 2 on desktop. Inline touch preserves vertical scrolling. Exploration supports drag rotation with wheel/pinch zoom disabled; the existing responsive camera distance and reset remain. Loading, asset failures, unavailable WebGL and reduced motion retain a poster and accessible product links.

The exploration backdrop crossfades to a charcoal studio gradient for Pro Max Silver/Glacier and Duo Star White, then returns to light for original/dark paints. The 650 ms opacity transition runs behind the transparent WebGL canvas without changing lighting, materials or geometry; reduced motion switches immediately. The inline pause button and Duo 0/50/100% shortcut buttons were removed at Khang's request. The continuous fold slider, inner/outer view, keyboard rotation and reset remain.

## Running and verification

Open `http://localhost:3000/vi` or `/en` after `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/start-local.ps1 -Production`. The script reuses an already healthy local service; stop its tracked services with `scripts/stop-local.ps1` first when rebuilding a running version. `?debug3d=1` enables read-only copies of actual model/renderer state in `window.__BOTZONE_3D__` for QA; no model-mutating test API is exposed.

`node tests/web/landing-v003-live.mjs --assets-only` checks copied bytes/hash and the GLB contract. `node tests/web/landing-v003-live.mjs` performs real Edge/WebGL checks and writes evidence under `artifacts/3d-v003`. Structural validation alone is not browser acceptance. Browser results and measured performance are recorded after the observed run; physical-phone performance is not inferred from viewport emulation.

The local CMS update used the existing content-staff session/CSRF/revision workflow: published version 2, draft revision 3, four sections. Only the two hero asset selections, default fold and color controls changed. Text, product links, other sections and previous publications remain. `artifacts/3d-v003/landing-plan.json` and `landing-published.json` record the exact changes. No admin web UI, Supabase, Vercel, external deployment or remote push was added.

## Observed acceptance — 13 November 2026

The production run passed **20/20 cases**, with 19 decoded canvas captures confirming rendered model content. See [the final report](../../../artifacts/3d-v003/final/README.md) and [runtime results](../../../artifacts/3d-v003/final/results.json). Earlier root/center-fix reports are diagnostic history, including a blank Duo exploration defect corrected before this final run. Model draw-call/triangle checks, decoded pixels and direct visual inspection supplement the asset validator.

Installed Edge on Windows used actual WebGL 2 through ANGLE Intel Iris Xe / Direct3D11. VI/EN layouts were checked at 390, 430, 1366 and 1440px. The suite exercised all four animation tracks, STEP threshold, closed/half/open poses, sustained 100%, five reverse cycles for both variants, outer glass, original material/UV handling, touch scrolling, exploration, reduced motion, loading/failure fallback, unavailable/lost WebGL, single active scene and controlled document visibility. The visibility check simulates the browser API signal; it is not an OS background-tab benchmark.

The Pro Max desktop sample observed 59.99 rendered frames/s over 983.5ms, 20 draw calls and 123,554 rendered triangles. A separate approximately 1.5-second browser RAF sample had a 16.7ms median and 16.8ms p95 interval. Fresh-context model fetch/hash/parse setup took 122–373ms across the eight responsive cases. These short host measurements do not establish sustained or physical-phone performance. Mobile touch/DPR was emulated, not measured on Android/iOS hardware.

Reviewed whole-page [mobile](../../../artifacts/3d-v003/final/landing-complete-vi-390.png) and [laptop](../../../artifacts/3d-v003/final/landing-complete-en-1440.png) captures, Pro Max, Duo 0/50/100%, outer black glass and fallbacks. An inactive section shows its poster intentionally. A fine crease/subpixel dotted line remains visible at some Duo angles; the authored geometry, UV and PBR remain unchanged. No model/poster HTTP failure or page exception occurred in normal cases. A separate favicon 404 was fixed with `src/app/favicon.ico`; a subsequent production rebuild and [Edge check](../../../artifacts/3d-v003/final/favicon-verification.json) returned 200 with zero console errors. The deliberately failed model request remains a negative test, not a normal network success.

To reproduce the complete browser run without changing business data:

```powershell
$env:BOTZONE_3D_CASE = ''
$env:BOTZONE_3D_ARTIFACTS = 'artifacts/3d-v003/final'
node tests/web/landing-v003-live.mjs
```

Next typecheck/production build and the full .NET solution Release build passed. The native CMS additions were compiled and published on Windows; new asset-choice interactions were not separately exercised in the WinForms runtime during this integration task.

### Subsequent exploration-control update

After removing zoom/pause/fold shortcuts and adding adaptive backgrounds, the targeted WebGL checks passed for both phones at 1440px EN and 390px VI: unchanged camera radius under wheel input, working rotation/reset, actual intermediate fade opacity, light/dark paint mapping, reduced motion and unchanged screen materials. Duo full-animation regression passed at both widths; Pro Max materials/keyboard/drag/reset also passed. Typecheck and Next production build passed; port 3000 serves this build. [Evidence and screenshots](../../../artifacts/3d-v003/exploration-controls/README.md) distinguish the corrected reduced-motion test assertion from application behavior. The earlier complete 20-case report remains historical; this change was verified with the targeted cases rather than a full rerun.

The subsequent laptop layout adjustment makes the exploration dialog fit the available viewport height. One side-switch button replaces the inner/outer pair; the reset and sliders remain visible without dialog scrolling. Both phones passed bounds/scroll checks at 1024×600, 1093×614, 1280×720, 1366×768 and 1440×900. [Laptop refinement evidence](../../../artifacts/3d-v003/laptop-refinement/README.md) includes screenshots and the final mobile/desktop wheel/background/reset regression. The mobile layout keeps normal scrolling when needed.

### Hero pointer response — 14 November 2026

Both inline phones now respond across their complete hero section, including the copy, using passive mouse-pointer listeners. Presentation yaw is bounded at ±0.22rad (about12.6°), pitch at ±0.11rad (6.3°), roll at ±0.028rad and lateral/vertical translation at ±0.10/0.07 normalized scene units. The response uses frame-time exponential damping (`1 - exp(-6 × delta)`, delta capped at50ms), with no React render per mouse movement. The existing idle preset remains independent. Leaving the hero, scrolling, blur, scene deactivation or opening exploration resets the target; the visible model eases back toward its original pose.

The effect applies only to a mouse with fine-pointer/hover capability. Touch scrolling and reduced-motion posters remain. The camera does not zoom or move; only the presentation parent changes. The authored GLB rig, fold clip, materials, textures and CMS settings are unchanged. The read-only `debug3d` snapshot adds the parent pose and target/smoothed pointer values for observation.

[Seven browser checks](../../../artifacts/3d-v003/hero-pointer/README.md) passed on the final production build: Pro Max/Duo at1366×768 and1920×1080, exploration isolation/drag/zoom lock,390px emulated touch and reduced motion. Actual pointer sweeps changed yaw by approximately22° from one side to the other; rendered-silhouette captures stayed clear of canvas edges. This is a focused interaction check, not a rerun of the complete original20-case suite or a sustained performance claim.

### Control alignment — 17 November 2026

Fold percentage and rotation degrees now share their label line. Both sliders use44px input rows; desktop paint/actions align with the range centers. Mobile has separate paint, slider and action rows inside the viewport-sized dialog. The accessible keyboard-rotation label remains; the visible label is shortened to “Góc xoay / Rotation”. [16 browser checks](../../../artifacts/web/scene-controls-20261117/results.json) passed across VI/EN and390–1440px, including no modal clipping/scrolling, real clip sampling, color/side/reset, keyboard and unchanged zoom lock. No hero pointer, model, camera or material logic changed.
