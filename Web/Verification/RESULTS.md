# Standalone browser release verification

Tested 4 November 2026. Static release `9d3d97b9121a324f`; source pinned to main `e5a782a2e8d91354798b917e3aceed36b4c5f42f`. That commit changes documentation only relative to `8c4573a`, the native-contract run’s core revision. Imported gameplay/content and the tested browser facade are identical.

| Layer | Result | What the evidence establishes |
| --- | --- | --- |
| Actual core/facade under .NET 8 | 27/27 pass | Seven starting builds, movement/turn ownership, free inspection, inventory ownership, damage/max HP, complete save restoration and malformed-load atomicity, recovery from interrupted turns, opening cellar travel and finite clay retrieval, well repair persistence, grouped pickup, ordinary/stale/measured throws, native brewing |
| Storage and JS coordinator | 30/30 pass | Atomic IndexedDB commits and aborts, generation recovery, multi-tab conflicts, backup validation/bounds, failed restore ownership, deferred background save, current-run export after quota failure, continuation identity |
| Actual WebAssembly in Chromium | Portrait and landscape pass | Real UI bootstrap, inspection, pack, compass movement, checkpoint, fresh-page restore, backup export, offline reopening; portrait also imports into a clean browser profile |
| Actual WebAssembly in WebKit | Portrait and landscape pass | Same gameplay and persistence flow; clean-profile import also passes in portrait |
| Phone layout | 390×844 and 844×390 pass | No horizontal overflow; primary compass/action controls meet 44px minimum and remain in view |
| Browser errors | Zero in final suite | No page, console, or HTTP errors in the four final cases |

The final browser suite executes the real downloaded .NET assemblies; it does not mock the engine, network responses, IndexedDB, or DOM. The coordinator tests separately use adapters and are labeled as such. Screenshots were inspected for map and inspection readability.

Initial portrait acceptance failed: a synthesized touch click dismissed the inspection overlay opened by the same tap. Backdrop dismissal now requires pointerdown on the backdrop. The final four-case suite verifies the correction. Native review also corrected max-health display, interrupted-turn recovery, grouped-loot timing, partial-action dirty tracking, skills refresh and exact throw commitment.

Receipts are in `evidence/`. Browser evidence covers mobile viewport emulation on Linux, not physical iPhone hardware, iOS Safari lifecycle behavior, or device performance. The original world/content runs, but every late-game quest, ability, crafting variant and region has not been browser-qualified. No desktop visual/audio or save-format parity claim is made.

Build succeeds with existing core warnings (reflection-populated fields, member hiding, and obsolete serialization APIs). Imported source whitespace is preserved. Browser adapter syntax and patch-whitespace checks pass.

## Repeated-touch fix — 4 November 2026

Release `cc5955d5d7843d31` blocks browser gestures on the compass, disables button text selection/callouts, and keeps one click activation path. Gesture suppression is scoped away from scrollable menus and reading panels.

The actual-WASM browser suite now passes 50 checks in Chromium/WebKit at both phone orientations. Each case sends four directional taps 75–129 ms apart: exactly four turns, return to the same position, viewport scale 1, unchanged viewport width, and empty text selection. Keyboard Space still spends exactly one wait turn. Existing save/restore, import/export and offline checks pass; browser error counts remain zero. All 30 storage/coordinator checks pass. No gameplay-core source changed, so native gameplay contracts were not rerun.

Receipt: `evidence/touch-browser-results.json`. This is browser emulation, not a physical iPhone Safari gesture test. Existing installations need their open game clients closed before the waiting offline update can activate.

## Landscape map and controls — 4 November 2026

Release `b2c41edad8ae06e7` gives the map the full available landscape height. A compact side rail keeps HP, focus, movement, actions and save status visible. Zone/tick and the latest narration overlay the map; aiming confirmation replaces the action shortcuts beside it. Very short windows arrange movement and actions side by side. A container query measures the height remaining after safe-area padding so targeting controls cannot cover movement buttons. Rotation cancels an unfinished map gesture and preserves manually selected zoom.

| Emulated viewport | Previous canvas | New canvas | Map area increase |
| --- | --- | --- | --- |
| 844×390 | 652×249 | 674×376 | 56% |
| 667×375 | 475×234 | 497×361 | 61% |
| 568×320 | 376×179 | 398×306 | 81% |

The browser presentation omits the native CP437 turn-divider entries from the mini-log, journal and death recap, retaining the actual latest narration. Simulation messages and checkpoint data remain unchanged.

The final landscape suite passes **63 checks across six actual-WASM Chromium/WebKit cases**. It verifies button centers and edges, free scrollable menus, actual Jet Blast targeting/cancellation/casting, movement, exact checkpoint restoration, readable narration, and portrait/landscape rotation retaining zoom. Simulated 47px side/21px bottom padding and 301/315/320px short-window bottom insets pass; the latter reproduced and verified the fix for targeting controls overlapping the compass. A separate WebKit 844×275 check passes with a 622×261 canvas and reachable movement/actions/aiming controls.

The existing portrait/landscape browser regression passes all **50 checks**, including repeated direction taps, one-turn keyboard activation, backup export/import and offline reopening. All **30 storage/coordinator checks** pass. Final suites report zero runtime, console or HTTP errors. No native gameplay source changed.

Receipts: `evidence/landscape-browser-results.json`, `evidence/landscape-regression-results.json`, `evidence/landscape-short-window-results.json`, and `evidence/landscape-storage-results.txt`. Screenshots include the standard map, smaller-screen aiming and the very short window. These are browser-emulation and CSS-padding fixtures, not physical iPhone or OS safe-area validation.

## World travel and cartography — 4 November 2026

Release `1c2547bf70c6e172` exposes the original world map and guarded surface/map traversal, disclosed landmarks and regional art. The view fits the logical 20×20 map, retains native zone coordinates for touch input, and preserves local zoom when returning. More → World map opens the guide; Set out enters the map, and Enter area explores the place beneath the marker. New checkpoints remain reliable; no historical-save conversion was required or added.

All **37 core contracts** pass: the original 27 plus ten new travel/disclosure contracts. The new ten failed against the previous bridge before implementation. They cover native transition and map-step time, exact immediate return position, persistent Sill travel, stale/underground refusals, four borders, map save/load, and undiscovered sinkhole names/colors across terrain, inspection and the guide. Strategic-map walking does not itself discover those sites. The core remembers the last surface location, not a return position per region.

All **49 actual-WASM travel checks** pass across Chromium/WebKit and portrait/landscape phone viewports, with zero page, console, HTTP or request errors. They verify every map cell center receives taps, free remote inspection, normal travel and fresh-page continuation while on the map. WebKit also covers 568×320. A separate overlapping 13-check WebKit case confirms short-window Jet Blast aim/cancel preserves both the turn and all 400 map-cell hit targets; no extra production correction was needed.

The existing **50 gameplay/browser regression checks**, **63 landscape checks**, and **30 storage/coordinator checks** pass. The 50-check local-game suite ran on candidate `4e6dbcbf3bb840f0`; the only subsequent production change reserves toolbar space in world-map framing, covered by the final travel suite. Native gameplay source and local-game behavior did not change between those candidates. Independent travel-bridge review found no remaining actionable timing, disclosure or restoration issue.

Actual screenshot review caught the portrait zoom toolbar covering map cells despite geometric fit; the final view reserves its occupied band and adds cell hit testing. Region backgrounds use five copied original atlases plus the existing Sill atlas, under 15 KB additional PNG data. This does not claim complete local-region art coverage, physical iPhone validation, or qualification of every late-game interaction. See `Docs/WEB-MIGRATION.md` for the remaining migration work.

Receipts: `evidence/world-core-results.json`, `evidence/world-core-red-results.json`, `evidence/world-browser-results.json`, `evidence/world-aim-cancel-results.json`, `evidence/world-regression-results.json`, `evidence/world-landscape-results.json`, and `evidence/world-storage-results.txt`.
