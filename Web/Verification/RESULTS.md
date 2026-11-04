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
