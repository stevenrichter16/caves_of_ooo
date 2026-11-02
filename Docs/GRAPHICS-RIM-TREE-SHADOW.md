# Rim Forest tree contact shadows

Bounded Editor acceptance for ordinary `Overworld.2.3.0`, seed65. This adds subtle grounding to existing trees; it closes no ASCII family or whole-area polish gate.

## Requirement and implementation
Reuse the approved Spread contact mask (10×4 native pixels,18%black,offset +1/−10 nativepixels relative to body) below the unchanged tree. No new textures, layers, collision, terrain, generation, owners, save schema or lighting engine.

`FlatPixelStumpMaterialsCatalog.CanReceiveTreeContactShadow` requires exact ordinary current2.3, disclosed selected/placed canonical Tree with successfully resolved native foreground, one genuine native TepuiStone receiver and empty tile state. Any other disclosed physical occupant withdraws contact. Hidden occupants cannot disclose themselves. Greybox delegates to already-created catalogs only. Existing renderer handles optional mask validation, native-only painting, toggle and owned-tile disposal.

## Acceptance and reproduction
1. Use isolated `Play Live16x24 Greybox`; copy `RimForest/AfterCheckpoint`, set active fixture ID, then actual C/Continue. Baseline/current40,12HP40Dawn,1280×800; 1,043/639,200worldpixels differ and UI is exact.
2. Current run: actual Up×2,Left reaches39,10HP40. Look north39,9→context→Examine verifies native tree identity and solid boundary. Attempting north leaves39,10. No injected movement/reveal or F12.
3. F5 saved turn2490 under actual gameID `a2e4390711a54b869eced598fbfbbac0`. Copy that game's Quick pair into AfterCheckpoint. Fresh isolated Continue restores39,10HP40;0/639,200worldpixels differ. UI log differs24,523pixels.
4. Native23RED failures precede implementation; final51/51passed with full summary (new23 plus existingSpread28). Counters cover disclosure, authority, art/resource absence, competitors, tile state, toggles, incremental/full repaint and no gameplay mutation. ExistingSpread mask/lifetime controls are reused.
5. Independent source and visual review found no actionableP1/P2. Visual verdict: slight grounding at normal zoom, without dark clutter; effect does not solve sparse scene composition.

## Corrections and honest limits
- First test request ran before the new file was imported and selected0tests; excluded. Full asset refresh then discovered the class and genuine23RED.
- Initial southward route reached40,17 then died from snakes/sky-sari. `tree-action.png` retains death. It is not successful navigation evidence. Fresh checkpoint/short northroute succeeded. Failed death-screenL recovery was not investigated as part of this visual change.
- First checkpoint copy mistakenly copied the original fixture directory, stillturn2450. Inspection caught it; replaced with actual turn2490pair before freshContinue. Use metadata GameID rather than assuming fixture-directory ID after Continue.
- Idle baseline23.55s vs active103.06s are unmatched. Renderer p95.1088→.0915ms; maxima2.90→134.21ms; afterMainmax1,981.51ms. The latter includes encounter/death/tool captures. No performance improvement or R11closure.
- No whole-area, population, other-biome, other-light-state, packaged or physicalDeck acceptance. Tiny tree silhouettes, orderly understory rows, broad pink clearings and glyphs remain. The red mark28,12 is actual CreatureCorpse (confirmed owner), a possible subsequent shared presentation gap.
- Current88c7 Mac/Linux builds exclude both Dunegrade97b3 and this change. Batch after publication.

Evidence: [verification](Verification/GraphicsExecution/RimTreeShadow/verification.json), before/after/saved/reload PNGs, tree-menu/reader/block/walk captures, native receipts, profiles and earned AfterCheckpoint. StopPlay completed; SaveRootOverride null and scene clean. No storage deletion; unrelated files preserved.
