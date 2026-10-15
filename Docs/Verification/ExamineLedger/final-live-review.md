# Final native field-ledger review

Unity 6000.3.4f1, macOS, 15 October 2026 UTC. The actual GameView was captured in a private, disposable save using `ExamineLedgerShowcase`. Screenshots are game renders, not imagegen concepts. All eight final PNG dimensions were read from the files.

## Observed behavior and visual review

| Capture | Review |
| --- | --- |
| `final-well-1280x800.png` | Amber title and portrait occupy separate header areas. Current damage precedes prose; Masonry and the two-fire-clay repair requirement remain readable. The shortened Repair label no longer wraps. |
| `final-fire-clay-1280x800.png` | The new compact ochre clay pieces replace the former crystal cluster; the world recipe and portrait both resolve `repair-cultivation-fire-clay`. |
| `final-inventory-sword-1280x800.png` | Actual inventory action → Examine produces the portable sword model. The fullscreen inventory stays underneath; mechanics, prose and facts fit within the panel. |
| `final-level-up-deck-1280x800.png` | Actual `CheckLevelUp` changes the actor to level 2, shows maximum HP +2 / 42, current health 42/42 and SP +1 / 1. This actor has no MP stat, so no MP grant is invented. Controller footer shows A Close and B Back. |
| `final-full-page-deck-1280x720.png` | Controlled 70-line stress notice spans three pages. Its full 25-row first page, footer and counter stay inside the frame, clear of the hotbar. This is synthetic boundary content, not a new game document. |
| `final-seamleaf-1920x1080.png` | Mature wet seamleaf model, ripe/harvest instructions and authored prose display together. Desktop scaling preserves the same header/body/footer hierarchy. |
| `final-curation-body-1280x800.png` | Actual generated Marrowstye case owner Meret Pell resolves the proper salt-cured body model and named authored description. The artificial showcase body had correctly used fallback instead. |
| `final-curation-world-1280x800.png` | Closing the reader restores the generated yard. Body/model verification used an audit teleport into this zone, not a claimed player-route walkthrough. |

Earlier `live-*.png` captures additionally cover rope well, gate, knotflax, hearthbulb, actors, chest and equipment. The earlier fire-clay and level-up pictures predate their final polish; the corresponding `final-*` files supersede them. The first review found crowded heading spacing and a wrapped repair label; both were reproduced in native tests and corrected before these final captures.

## Input, state and resource checks

The private session began at turn tick 10. Actual InventoryUI item action and InputHandler announcement routing opened the sword reader at tick 10. A virtual Input System gamepad then sent B: the reader closed, InventoryUI remained open and InputHandler returned to `InventoryOpen`, still tick 10. After multiple frames, the portrait capture count remained 3 (the well, clay and sword captures), and closing released its owned render target.

The three-page notice received RB → page 1, release, LB → page 0, release, A → page 1, release, B → closed/Normal. Every observation remained tick 10. These events were injected into Unity's real Input System and handled through InputHandler; they were not physical Deck input. Six native EditMode cases additionally cover D-pad, held buttons, A on the last page and short notices. Raw final observations are in `final-live-input.json`.

The console check returned Metal memoryless-depth load/store messages and an autosave message, with no observed game exception. The isolated audit owned that autosave. Shutdown completed with code 0; the private save root was removed, save-root override and original game preference restored, virtual pad removed, original seed restored, and GameView returned to its original index 3 / zero custom sizes. Unity was left in Edit mode.

## Regression evidence

`final-sweep.xml`: 1,072 native EditMode cases, 1,071 passed, one failed solely because the old library-count pin expected 37 repair models instead of 38. The intentional added FireClay model is the 38th. After updating that exact pin and its test name, `final-pin.xml` passed 1/1. Thus the selected 1,072 current cases are green across the sweep plus targeted rerun; this is not a claim that the original sweep was wholly green or that the entire repository test suite ran.

The source/content suites separately pass 19 geometry/source tests and seven description-audit tests. The parsed blueprint delta is 124 Description-only edits and 588 unchanged blueprints.

## Can verify / cannot verify

**Can verify:** native tests, observed current ownership/state, actual model selection, no-turn/menu restoration for exercised paths, bounded live popup captures, resource release, controller event behavior, visible layout and silhouette in the saved macOS game renders.

**Cannot verify:** physical Steam Deck ergonomics, Linux GPU/performance, every biome/object/state combination, or that every statically referenced blueprint spawns. No Deck build was produced in this request. Eight target-first art objects are completed; the 288 authored carried-model mappings reuse existing recipes and are not 288 new models. The prose census still finds 135 named gaps. See `portrait-coverage.md` and the living document for remaining scope.
