# Shared portable expedition essentials

**Status — 2026-10-28:** implemented and bounded Editor-accepted:139/139 native passes, five actual before captures and13 after captures, including normal pickup/Read/Drink/Eat and fresh Continue163. Exact HealingTonic, DriedMeat and WateringGrimoire now retain their existing approved bodies outside the opening. Source publication and coherent package delivery are root's remaining gates; current packages remain `fa553ca36`. This is three-item coverage, not whole-area or worldwide visual acceptance. Initial preflight/RED notes below remain chronological evidence.

## Purpose and boundary

Give the exact HealingTonic, DriedMeat and WateringGrimoire consistent existing item bodies outside the opening. These are common carried expedition supplies with real use commands; this closes a portable presentation gap without inventing stock, travelling to undisclosed owners or making another isolated asset packet. It does not improve whole-chunk fullness or certify any area as polished.

Reuse approved art without changing its PNGs, imports or editable sources. The tonic and meat are existing native opening-items frames; the book is the complete existing square Environment item_book. The established approved-art reuse exception applies: no new imagegen render, traced pixels, fabricated editable source or animation is required. Source provenance for the book's original authoring file has not been established. Qud is a presentation reference only; no copied art or gameplay-parity claim.

## Verification sweep and exact contracts

| Owner | Verified current mechanics and appearance | Planned neutral body |
| --- | --- | --- |
| HealingTonic | `Objects.json` exact blueprint at4035 inherits TonicItem. Owned Render `!`, `&G`, layer5; takeable, non-solid Item, weight1/categoryTonics. Owned Tonic has Healing `4d6+4`, Effect `Healing`, inherited Drink=true. `TonicPart` supplies Drink/ApplyTonic, consumes one carried unit and owns thrown payload behavior. | Existing opening-items frame7, rect(112,0,16,24). Stoppered bottle, not a displayed dose count, reusable empty vessel or permanent healing aura. |
| DriedMeat | Exact blueprint at11785 inherits FoodItem. Render `%`, `&r`, layer5; takeable non-solid Item/Food, weight2/categoryFood. Owned Food Healing `3d4`, Cooking `Meal`; owned Handling explicitly carryable/throwable OneHand. Eat consumes one unit. Existing bait text is limited to eligible hunting furrowstalkers; throwing at an actor remains an attack. | Existing opening-items frame8, rect(128,0,16,24). Tied strips identify food, not number of portions or a guaranteed lure outcome. |
| WateringGrimoire | Exact blueprint at6393 inherits Item. Render `\"`, `&B`, layer5; takeable non-solid Item/Grimoire, weight2/categoryBooks. Owned Grimoire.SkillClassName is **Hydromancy_ConjureRain**. Read teaches that skill or reports already known; the book is not consumed. It is not the older knowledge-property implementation. | Existing Environment/item_book complete rect(0,0,16,16), square pixels at16PPU. No stretching into24pixels, rain cloud, glow, open/read animation or skill-ready indicator. |

Relevant source: `FlatPixelOpeningArtCatalog.ItemFrameOf` already maps exact tonic/meat to7/8 using owned semantic parts; `EnvironmentSpriteRenderer.ResolveItemBody` uses item_book for legacy grimoires. Modern `FlatPixelGreyboxCatalog` currently admits opening-item art only in local graphs (plus explicit unrelated portable routes). Do **not** copy the legacy substring `Contains("Grimoire")` policy into the new exact-identity slice.

| Earlier assumption | Source correction / required action |
| --- | --- |
| All portable starting supplies already use native art globally | Modern world and current-zone reader routes still apply the opening-item locality gate. Legacy subject routing differs. Cover all four established Greybox world/reader seams consistently. |
| The same 16×24 frame can be obtained from imported sub-sprites | Existing import rectangles are cropped: tonic117,1,6,10; meat132,1,8,12; book2,3,12,9. Build runtime Sprite views from the borrowed Texture2D and complete frame rectangles, preserving import bytes. |
| WateringGrimoire grants a knowledge-property flag | Current authored Grimoire teaches Hydromancy_ConjureRain. Preserve this skill grant and already-known refusal; inspection must not Read or cast. |
| HealingTonic's regional-fallback tests are accidental failures | `FlatPixelGlobalPresentationTests.RegionalPropAndFiniteSupplyActorUseMatchingGlyphOutsideThenNativeInside` explicitly pins the old locality policy. Replace only its tonic expectation after intentional RED; retain MarlbackPatchbearer's regional/finite-supply control. |
| One test is the only old locality pin | Also review `CarriedRegionalItemUsesViewerZonePolicyWhileInventoryOwnsSquareCanvas` and `EquipmentPortabilityDoesNotAdmitRegionalGroundServicesOrMedicine`. Preserve custody/square-canvas and AlchemyStill/Patchbearer assertions; change only obsolete tonic naming/expectations after RED. |

## Immutable art and import pins

Full file SHA256 values below are the preflight bytes, not regenerated assets. Runtime must not alter filters, import slices, alpha, pixels or dimensions.

| File | SHA256 |
| --- | --- |
| `Assets/Resources/Sprites/Live16x24/opening-items.png` (192×24) | `1ead2618c2d82d2ca7cae8765ab09f690255998ad0bb5b36027d7835fc292394` |
| `Assets/Resources/Sprites/Live16x24/opening-items.png.meta` | `74200c8a3b973c03952e818bffb2b17340f9fc944884e1124b50fa33f3d25930` |
| `Assets/Resources/Sprites/Environment/item_book.png` (16×16) | `5a15f96a509ada25d3400f023ce63fb7072f4671d1091bd5e5a17474d260cbec` |
| `Assets/Resources/Sprites/Environment/item_book.png.meta` | `8402ae9743b0d39fbfe5fb4824596aa30d96e57418928acf1a717d155e397815` |
| `ArtSource/Live16x24/items/opening-items.blend` | `22db7696c80f4e16634583b95271780c1fdc55792d91786ea724483db84dfc4d` |
| `ArtSource/Live16x24/items/source-cells.json` | `eef2eb0d2cdb7a06b5509968efeaaee0755d5aa70ccef640e99bbefbb0d04131` |
| `Tools/Live16x24/build_opening_items.py` | `7e75f495b63dc6dad39191bde747cb24a877b2f633e5a609bb9fdfdce1a54d98` |

Both PNG imports currently use Point/filterMode0,16PPU,no mipmaps,uncompressed default texture format and clamp wrap. All three complete-frame alpha sets are binary0/255. Native RGBA byte hashes (PNG top-down rows, unmodified transparent pixels included): tonic frame7 `10251b6e1182dc68f94a313ad82deda5a36f54df85301bdfe005c2dd835c93f6`; meat frame8 `39696ca829ccd452b2a557c1c715b16f5da55b0188f1e6fd61ea4c335c44ee28`; complete book `e158c28f783cb040afb15a431b73a5c4368c9eac54bbf9fb71ee5406fcc83a32`. Visible bounds are respectively6×10,8×12 and12×9. The tonic/meat touch native image row22; the approved book remains a compact square view. Contact and native book were visually inspected during preflight; actual reader/world scale and grading remain root's acceptance gate.

## Earned baseline without a travel hunt

Read-only reuse of `earned-resin-state-audit.py`'s existing bounded format8 loader confirmed `Fixtures/sodden-frogs-after157/Quick.sav.gz`. No save writes, gameplay loading or world-owner census were performed by this preflight.

- Player2695, OpenMire `Overworld.18.8.0`, position40,10, turn2350, HP38/40; saved2026-10-28T09:40:11.6150030Z.
- The player's actual InventoryPart list and matching Physics.InInventory references contain WateringGrimoire2696×1, HealingTonic2701×2 and DriedMeat2702×2. All are currently carried, not undisclosed ground owners. LongSword2699/Buckler2700 are separately equipped controls, not additions to this scope.
- Quick.sav.gz SHA256 `2b8e6881ec51c1bde8827cc687b121110c976b4f094d59c1ea55f8f22381e65b`; Quick.json SHA256 `4f6a7448506682cb1f018f8f17572b8ca5855ccf202650c9eb5b98ab87589b2a`.
- Earlier earned Sill fixture143 also records these identities in `shared-weaponsmith-reopen-player.json`; it is a fallback safe-settlement route, not a requirement to retravel there. Root's actual Continue157 inventory observation now independently agrees with the saved inventory. Individual current reader and deliberate-drop screenshots are still pending.

Use normal inventory/F1, drop, step aside, ground examination and pickup. Do not create objects, inject currency/health, reveal hidden cells or teleport. A normal Drink may spend one existing tonic to restore actual missing HP; Eat spends one meat; Read may teach or report already known. There is no need to force damage or hunt a predator to approve neutral item art. Preserve a legitimate pre-use checkpoint so both unconsumed bodies and actual consumption can be reviewed honestly.

## Minimal implementation after native RED

Prefer one small `FlatPixelExpeditionEssentialsCatalog` with two independently optional borrowed resources: opening-items and item_book. Cache just three complete-frame Sprite/Tile views once. Preserve default resource loading versus explicit missing/malformed injection; no implicit reload on refusal. Own/dispose only generated views, never supplied textures. Existing constructors and family routes remain compatible.

Exact known identities take an authoritative path through both world and both subject routes. Require unique current owned Render/Physics/Examinable plus the correct unique owned Tonic/Food/Grimoire semantic part, genuine takeable non-solid Item meaning, valid optional positive stack and a current single-cell footprint. Keep safe mutable quantity, condition, price, effect/status, damage and normal Render-event cues presentation-neutral. Reject creature/player/terrain/crop/service/container/equipment aliases, missing/foreign essential parts and explicit VisualID/VisualVariant/GlyphVariants reskins. Do not impose equipment, Repairable, Thermal or composition parts these items do not have.

The proposed Grimoire contract requires the actual ConjureRain skill-teaching field, not merely any nonblank grimoire component. Tonic requires meaningful healing payload; Food requires the existing edible part. Preserve legitimate numeric tuning instead of freezing every healing die or commerce value. No new drinks, throwing behavior, bait promises, read mechanics or gameplay state are introduced.

The caller retains current graph, disclosed selected top owner, fog/memory, world custody and authorized carried/equipped subject authority. An exact known owner refused by the new adapter must not silently regain native art through the older local opening mapping. Missing resources use the truthful existing glyph fallback. Other tonics, BrewedTonic, RawMeat/CookedMeat, other grimoires, seeds, services and all approved unrelated art keep their own current routes.

## Tests and actual acceptance

Write a compact fixture through established portable catalog/real ZoneRenderer/reader helpers; prefer invariant pairs over redundant case inflation. Root confirms actual native RED before production. Initial cases should prove that each outside-opening owned world/reader body is the exact approved texture and complete rect, then preserve inside-opening equality and the square book aspect. Test explicit null/malformed resources independently, borrowed lifetime, alternate identities, semantic/reskin/stack refusals, selected/covered/hidden owners, stale graph/viewer and carried-to-drop-to-pickup custody. A real use counter should prove presentation neither consumes nor teaches, and that normal consumption/removal cannot leave a ghost. Existing global/portable tests provide broad unrelated-role controls; do not repeat them wholesale.

After GREEN, root uses the same earned checkpoint for actual three carried readers and ordinary dropped-world/ground-reader views, pickup, meaningful available use, F5/full Continue. Compare world pixels only when framing and exposed bodies genuinely match. Include a small rendered cue/condition control where already supported, without inventing one for the neutral book. Read-only source review and independent actual-image review precede publication. Record exact test identities/counts, failures/corrections and honest save/restart limits in this document.

Cached sprites and no repaint-side transactions are the performance bounds. No new timers, animation, polling, geometry, simulation events, RNG, atlas rebuilds or per-frame resource loads. Native CPU assertions do not prove GPU appearance, device controls, FPS or a performance improvement. Root owns Unity, live capture, Git and publication; no new package is required for this one small batch before coherent delivery grouping.

## Pre-implementation self-review / continuation

- 🟢 Source preflight found exact resources and legitimately carried owners for all three; no new loot or service dependency is needed.
- 🟡 Intentional behavior change: the old tonic regional-only policy must be replaced explicitly in focused and existing global tests after RED; leaving a permissive local fallback would defeat refusals.
- 🔵 Approved complete-frame book is16×16, intentionally not a stretched24-pixel sprite. Original editable book provenance remains unknown; approved bytes are the source.
- 🧪 Native RED/GREEN, actual three readers/drop bodies, use/removal and restart are pending. Do not infer acceptance from save presence or this plan.
- ⚪ Whole-world observation, scene fullness, all55+5 later area-polish scopes, current package gaps and physical Deck acceptance remain open.

Changed implementation files: this living document; one focused test fixture+fresh.meta; narrowly corrected obsolete GlobalPresentation tonic pins; one new small catalog+fresh.meta and four Greybox routes after native RED. Root also records immutable reuse provenance and actual evidence. No content JSON or approved art edits. The initial planning step changed only this document; the following checkpoints record subsequent authorized implementation.

### Test-source correction before intended RED

The first native import failed compilation: the test assembly cannot access runtime-internal `Entity.SpatialZone`. This is a fixture API mistake, not the intended missing-body RED. The reader helper now retains its constructor-supplied Zone and removes its own viewer through public `GetEntityCell`/`RemoveEntity`; it neither reflects nor mutates runtime ownership. The attempted job `e0bd196ec5394e5ab1f50c68c4878280` may discover no tests and must not count as regression evidence. Production remains untouched; root must reimport and observe the20 intended cases before implementation.

### Native RED and bounded implementation

Root subsequently confirmed job `e0fccd47f7d34638864dfe111965ec9c`:20/20 intended glyph/missing-adapter failures. `expedition-essentials-native-red.json` retains the full uncapped progress; result is null, so no complete XML is claimed. The first compile failure remains excluded. The20-case fixture is unchanged after this genuine RED.

The new catalog now caches only three neutral complete-frame views from two independent borrowed textures. Four existing Greybox routes preserve known-owner refusal before older local opening art; the shared one-Render-event color-cue path remains in use. No simulation or item actions occur while resolving artwork. Default construction can load the book resource; explicit injection never does. Root's source review caught an important compatibility seam before GREEN: existing five-texture Greybox callers supply opening-items explicitly. The lazy new adapter therefore borrows that already-supplied items atlas even with optional-resource loading off, while the omitted book remains unavailable. `FlatPixelOpeningItemsTests` belongs in the regression run.

Only the intentional tonic policy was migrated in existing GlobalPresentation tests: tonic now has the exact portable frame outside/inside; MarlbackPatchbearer's regional finite-supply behavior and AlchemyStill's service boundary remain asserted. The carried reader test retains custody/paused-canvas checks under an accurate name. No alternate item policy, constructor signature, approved PNG/import/content/mechanic changed.

Actual before evidence now includes five root captures and earned ground fixture162: ordinary drops followed by three east steps leave book2696 at40,10×1, meat2702 at41,10×2 and tonic2701 at42,10×2, player43,10 HP38 turn2380. Root archived the real GameID F5 output and finished Play. This establishes actual before ownership/placement, not acceptance of the new bodies. Native GREEN, independent runtime review, matching after captures/use/restart and publication remain pending.

### Completed bounded Editor acceptance

**Native:** job `5dcaf42cc8e74b929a7bf0007b67658a` passed139/139, zero failures/skips:20 new Essentials cases,52 GlobalPresentation,21 Greatdew and46 OpeningItems. These counts partition this one run; do not add its included fixtures again. The full result is `Verification/Live16x24/expedition-essentials-native-green.json`. The20 intended RED failures preceded production; the test-assembly compile correction and optional-atlas compatibility catch remain documented above. Independent source review found no actionable blocker; see `Verification/Live16x24/expedition-essentials-independent-review.md`.

**Actual ordinary play:** five before images include carried readers and the three deliberate ground drops in fixture162. Thirteen after images verify exposed ground bodies and authorized inventory readers, real pickup/use and fresh Continue. Separate ordinary ground F1 readers were not observed in this slice; world/ground reader parity is native-tested only. Pickup returned the same three IDs to player40,10; a real Bandfrog attack reduced HP38→36. Reading WateringGrimoire2696 learned Conjure Rain and retained the book×1. Drinking HealingTonic2701 changed its stack2→1 and HP36→40. Eating DriedMeat2702 changed its stack2→1; healing was capped at full HP, then another real Bandfrog attack left HP36. No spawned supply, health adjustment, currency injection or artificial safety claim is involved.

F5 saved the actual GameID output as earned fixture `expedition-essentials-used163`, turn2460. Root's fresh Continue163 retained player40,10/HP36, Rain learned, book2696×1, tonic2701×1 and meat2702×1. `expedition-essentials-restart.json` and after12 world/after13 tonic-reader captures support this bounded state/reader check. No exact pixel-equality comparison is claimed. Both isolated play runs were finished. One initial read-only evaluation used the wrong Skills namespace and failed to compile; correcting it to `CavesOfOoo.Skills` changed only the observation request, not runtime source or saved gameplay.

Receipts: `expedition-essentials-before-inventory.json`, `expedition-essentials-before-ground.json`, `expedition-essentials-after-pickup.json`, `expedition-essentials-after-read.json`, `expedition-essentials-after-drink.json`, `expedition-essentials-after-eat-save.json` and `expedition-essentials-restart.json`, all under `Docs/Verification/Live16x24`. The actual before inventory image is `expedition-essentials-before-01-inventory.png`; the earlier unhyphenated filename in planning was not an additional capture.

**Final self-review:** 🟢 exact three native body/reader identities and existing use commands are bounded Editor-verified; approved texture/import bytes remain pinned, with independent runtime source review. 🔵 source and reader authority do not create new stock, services, throwing rules, bait outcomes or rain on inspection. 🧪 no fresh standalone/Linux/physical Deck execution, worldwide observation, sustained performance or exact restart image equality. ⚪ other foods/medicines/books, chunk composition and all55+5 polish scopes remain open. Source publication and package grouping remain root-owned; do not mark these supplies delivered in current `fa553ca36` binaries.
