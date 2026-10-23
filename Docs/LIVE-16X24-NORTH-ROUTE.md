# Live 16×24 — north route and seed plot

Status: exact scope, ordinary greybox journey, concept/parts reference and the first native vegetation packet are integrated and reviewed in ordinary Play, 2026-10-23. Native north actors/works are next, followed by Nella's crops, reserve cues and products. Continues the accepted glade/cellar work (`e332cf040`) and screenshot-delivery preference (`f136ba4ee`). Normal release activation remains a separate gate. Follow [the live art direction](LIVE-16X24-ART-DIRECTION.md), [connected-site inventory](LIVE-16X24-CONNECTED-SITES.md) and [vegetation implementation/evidence](LIVE-16X24-NORTH-VEGETATION.md).

## Design and implementation prompt

Extend the opt-in native presentation through the real north connector `Overworld.11.9.0` to Nella's actual seed plot `Overworld.11.8.0`. Preserve the current owners, population, material sources, permissions, collision and saved depletion. Use the two-seed census already captured from actual generation; include ordinary surroundings, not just authored destination props. Greybox and walk the real route, capture it, reinterpret it with imagegen and approved native references, extract owner/state parts, then produce editable native sprites and integrate them. Show the user screenshots at visible milestones. A rough frame or a concept is not accepted native art.

Nella's approved Seed Keeper 3 pixels remain byte-identical. The plot should read as four open public planting spots beside two tied reserve beds and a modest reserve tray. Cords belong to validated ground and persist after harvest, without claiming permission has been granted. The connector's actual alembic and forage should read as a working stop within quieter wilderness. No extra stock, planters, lanterns, roof, enemies or background services may be invented for visual richness.

## Verified source corrections before implementation

| Source | Correction and implementation consequence |
| --- | --- |
| `FlatPixelGreyboxCatalog.IsAdmitted` | At the source sweep, admitted glade+cellar only. Add the two exact north addresses behind current managed graph, map/biome and no-POI authority. Other surface sites/depths remain excluded. |
| `SpreadPresentationScope.IsActive` | Surface admission is broader than this route and does not reject POIs. Never replace the exact allowlist with this broad predicate alone. |
| `SpreadExplorationResidents` | Seed-dependent safe placement means resident/crop positions must come from the real generated graph. Do not move Nella to the opening or assume fixed cell coordinates. |
| Same builder and `CropPart` | Public gladroot/emberwheat use the older seed/sprout-to-produce lifecycle. Reserve marlroot/pitchpod use mature harvest and stage 2. One universal three-stage crop contract would lie. |
| `LocalGatheringClaimPart.Bound` | The agreement binds exact keeper, two soils, tray, world identity and coordinates. Rendering must validate these references and must not read permission as a claim to free stock. |
| `ConnectedSpread3DLibrary.ResolveReserveBed`, `Zone.GetReadOnlyEntitiesWithPart<T>` | Existing presentation scans the zone for the keeper. The zone already maintains a relevant-part owner index after one seed pass. Use indexed claim owners, direct soil references and current `Bound` validation; do not copy the old per-cell whole-zone search. Off-cell claim changes still need explicit repaint review. |
| Resident ground preparation | Only the two reserve beds receive `CultivatedSoilPart`; the four public crops are actual plantable ground but are not newly marked private/prepared terrain. Do not paint universal cord borders or imply all rows are reserved. |
| Exploration versions | Connected resident generation begins at version 11; enriched north drying yard at version 17. Neither is a rendering gate. Destroyed, refused or legacy literal graphs retain their admitted presentation without regenerating newer content. |
| Measured surroundings | Seeds 64/65 include 32/34 north trees and 124/165 hedges, plus different incidental hazards, creatures and containers. Nella plus crop art alone cannot be presented as complete route coverage. |
| `LiveFlatPixelGreyboxTools` | Existing isolated launcher configures seed 64 and real SampleScene without moving/spawning/revealing entities. Reuse it and ordinary border travel; no art-tour teleport. |
| Actual two-seed census | Both sites and their surroundings are generated and preserved in `connected-two-seed-census.json.gz`. That receipt describes content; it does not establish newly enabled rendering or current Play behavior. |

## Small reviewable stages

1. **Scope foundation.** Write native failing tests for the two current graphs, map/POI/canonical-address/cached identity counters, retained destroyed sites, no generation and renderer/fullscreen transitions. Update only obsolete north-as-outside test pins. Implement the exact expansion and run affected native controls. Existing fallback is explicitly the playable greybox until its art is accepted.
2. **Actual route and captures.** Fresh ordinary seed 64 game; walk north from glade through the connector into the plot. Record entry, actual worksite, public versus reserve arrangement and ordinary examine/trade/menu return. Do not force refused placements or change actors for screenshots. Capture 1280×800 Game output and read-only state evidence. Restore the isolated editor session before imports.
3. **Concept and native parts.** Use the actual captures, approved keeper and accepted palette as imagegen inputs. Save exact prompts, outputs and critiques. Establish the complete measured frame list including terrain/props/crops and actual harvested products; preserve base artwork. Author native integer-cell sources and direct-rendered contact sheets. Inspect silhouettes at native size, color count, margins, binary alpha and actual state distinctions.
4. **Runtime and claimed beds.** RED first for current owner/state selection, foreign-reskin fallback, optional resources/lifetime, disappearance, fog and actual resource binding. Claim cues must persist on empty validated reserve beds, remain absent on public/invalid/hidden beds and never perform a per-frame zone search. Trade/permissions remain gameplay-owned. Reuse shared cached atlases/materials.
5. **Repeat ordinary journey and review.** Inspect the same route with native art, interact with Nella and real crops/tray where the current player can do so, test fullscreen return, water/growth/revisit and save/load where feasible. Compare actual before/after captures, measure rendering cost separately from content census, fix significant faults, record honest limits and push exact owned paths after fetch/rebase.

## Acceptance and honesty bounds

- Every new visual maps to an actual owned blueprint/state. Explicit fallback remains for unknown late equipment and unsupported owners; do not claim universal item coverage.
- Admission does not depend on Nella remaining alive, crop presence or supplies. Removed owners do not regenerate on travel/reload.
- Current fog and rectangular cell selection remain intact; fullscreen menus retain square geometry and return correctly.
- Art sources and original 3D/study restore paths stay recoverable. No change to normal launch defaults in this packet.
- Native tests establish their assertions; actual screenshots establish observed appearance, and ordinary inputs establish exercised flows. Neither establishes physical Steam Deck feel or performance.

## Continuation / self-review

Historical scope checkpoint: scope and ordinary greybox were accepted for art production; concepts and extracted parts were saved. Native RED ran95 cases (70 passed,25 expected missing-north-admission failures), followed by95/95 GREEN after the exact scope expansion. Receipts: `Verification/Live16x24/north-scope-{red,green}*`. The new33-case `FlatPixelNorthScopeTests` was written before production changes; two obsolete managed-north exclusion pins were removed from the earlier cellar fixture, whose outside witness now uses the unadmitted east connector. Standalone unattached north rejection remains unchanged. Independent cold review found no actionable P1/P2 in the exact scope change. The117-case fullscreen, motion, portrait and ground-state consumer regression also passed: **212 distinct native cases, zero failures/skips** across those two GREEN runs. No gameplay rules or spawns changed. The later vegetation checkpoint is recorded below; these totals are not cumulative unique test counts.

Implementation is an exact address switch followed by current `SpreadPresentationScope` graph/map validation and the corresponding `(11,y)` null-POI test. Glade authority remains separate. It adds no zone generation, owner scan, resource allocation or permission mutation. Existing native sprites are reused where their selectors already match; all other north owners retain explicit rough fallback.

### Historical greybox ordinary-input journey

The isolated launcher used real SampleScene/GameBootstrap, seed64 and1280×800. N started a fresh game. All movement, look, selection, combat response and purchasing used ordinary keyboard controls through CUA. Read-only diagnostics and ScreenCapture recorded the same graph; no teleports, revealed fog, inserted owners, injected supplies or HP changes were used.

| Capture / tested flow | Observed result |
| --- | --- |
| [North entry](Verification/Live16x24/north-greybox-entry.png) | From glade `(40,12)`,13 Up presses crossed into `11.9.0` at `(40,24)`,40HP, flat mode active. Rectangular world and existing sidebar/hotbar remained aligned. |
| [Works](Verification/Live16x24/north-greybox-works.png) |21 Right then6 Up reached `(61,18)` alongside the actual still. Solid masonry remained solid; existing quiet lanes and hedge openings are navigable. Unknown source plants/still/shelves and birds/viper/patchbearer remain rough markers, an explicit art gap. |
| [Still examine](Verification/Live16x24/north-greybox-still-examine.png) | L, Up, Right, Enter, X selected actual still `(62,17)` and opened its correct brewing description. Portrait was a rough square, not accepted native still art. |
| Ordinary danger/route counter-check | After closing the popup, two Up steps reached `(61,16)`; three additional Up presses struck the wall rather than passing it. The approaching patchbearer hit the player from40→25HP. Skill6, Enter, Down cast actual Calm and made it peaceful. Three Left,6 Up,3 Up,12 Left,8 Up then used the open gap and crossed into `11.8.0` at `(46,24)`. The failed straight route is retained as evidence; do not describe this as a no-damage safe route. |
| [Nella plot](Verification/Live16x24/north-greybox-plot.png) |26 Left and14 Up reached `(20,10)` beside the actual keeper `(19,9)`. Native field stubble repeats outside the public/reserve cluster; actual generic trees and keeper remain placeholders. |
| [Reserve terms](Verification/Live16x24/north-greybox-reserve-terms.png) | L, Up, Left, Enter, Q asked about tied rows. Real dialogue says two tied beds/tray are reserved, eight drams or Orven's introduction grants rights, and four open beds are already public. The physical cord is not yet drawn: this is the next state-aware cue, not proof of its integration. |
| [Purchase](Verification/Live16x24/north-greybox-trade.png) and [world return](Verification/Live16x24/north-greybox-trade-return.png) | Enter,C opened chat; E opened full-screen trade. Down, Enter, Y bought the actual two gladroot seeds for17drams. Player50→33, keeper40→57, player seed count4→6; keeper's seed row disappeared. Escape restored the rectangular world and correct log. No permissions bought and no crop taken. |

Each PNG has a sibling JSON with observed owners/visibility and inventory. PNG headers independently confirm1280×800; the editor API reported a different dock size, retained separately in receipts. Owner diagnostics deliberately include hidden owners and are not a player-disclosure or rendered-visibility claim. [Cleanup](Verification/Live16x24/north-greybox-cleanup.json) confirms Play stopped, clean SampleScene restored and all isolated-session/view/compile flags false.

### Concepts and extracted parts

Built-in imagegen used the actual plot/works screenshots, approved Seed Keeper3 and accepted base/anchor contact sheets. Exact prompts are saved beside outputs in `ArtSource/Live16x24/concepts`:

- [Plot concept](../ArtSource/Live16x24/concepts/north-plot-concept.png), [prompt](../ArtSource/Live16x24/concepts/north-plot-prompt.txt): useful separation of four public seeds versus two corded reserves and one empty tray. Generated keeper details and object coordinates are **not authoritative**; original PNG and actual owners remain authoritative.
- [Works concept](../ArtSource/Live16x24/concepts/north-works-concept.png), [prompt](../ArtSource/Live16x24/concepts/north-works-prompt.txt): readable still/empty compartments/three distinct forage forms, connected hedge and separate tree silhouettes. Generated scene densifies foliage and rearranges actors/walls; do not copy that layout or add its incidental tufts.
- [Extracted reference parts](../ArtSource/Live16x24/concepts/north-parts-reference.png), [prompt](../ArtSource/Live16x24/concepts/north-parts-prompt.txt):16 isolated silhouettes including the two healing-cradle states. The sheet has excessive pixel detail and alpha-edge artifacts; **never slice/import/downsample it as final art**. Reconstruct native integer-cell originals with binary alpha and the approved palette. The extraction is reference only.

References R03/R04 from the existing inspected [reference library](ART-REFERENCE-LIBRARY.md) informed practical storage and bent seedhead silhouettes. No external photo is embedded in production art. The [38-identity contract audit](LIVE-16X24-NORTH-CONTRACTS.md) and [native parts proposal](../ArtSource/Live16x24/concepts/north-parts-plan.md) map measured source owners,38 placed blueprints, later products and required state variants. A concept is not a replacement world background.

### Native vegetation integration and ordinary-input acceptance

The [vegetation packet](LIVE-16X24-NORTH-VEGETATION.md) adds four original native cells for Tree, Hedge, BerryBush and DryBrush, plus exact ordinary-owner reuse of accepted Bush frame5 and Reeds frames3/4. The independently optional 64×24 atlas uses cached sprites/tiles; explicit glade/reskin behavior, collision, player sight, current fog and actual harvest ownership remain unchanged. Independent source/contact and code reviews found no actionable P1/P2. This covers six vegetation identities, not all 38 measured route identities.

Native Unity evidence progressed from [49 intended RED failures](Verification/Live16x24/north-vegetation-red.xml), through [48 passes plus one expected missing-resource failure](Verification/Live16x24/north-vegetation-resource-red.xml), to [204/204 GREEN](Verification/Live16x24/north-vegetation-green.xml) and [108/108 affected-consumer GREEN](Verification/Live16x24/north-vegetation-consumers.xml): **312 distinct cases, zero failures/skips** in the final two runs. The resource-positive test verifies actual imported frames; synthetic routing alone is not the integration claim.

The later isolated seed64 run repeated ordinary north travel with the native vegetation:

| Capture / flow | Observed result |
| --- | --- |
| [Native north entry](Verification/Live16x24/north-vegetation-native-entry.png) | Actual admitted connector uses distinct trees, dense hedges and low bushes instead of the previous vegetation placeholders. The earlier greybox/baseline captures remain preserved for comparison. |
| [Hedge collision](Verification/Live16x24/north-vegetation-hedge-collision.png) | Ordinary movement met the real hedge boundary; the capture records the player at `(56,10)`. Art did not turn it into a passage or change sight tags. |
| [Berry examine](Verification/Live16x24/north-vegetation-berry-examine.png) and [harvest](Verification/Live16x24/north-vegetation-berry-harvest.png) | Examined the actual BerryBush at `(60,1)` and harvested from `(60,2)`. The source disappeared and the player gained one WildBerries. No permanent spent bush or decorative berry item was introduced. |
| [Native plot arrival](Verification/Live16x24/north-vegetation-native-plot.png) | Ordinary travel crossed into `11.8.0` at `(60,24)`; 40 Left and 14 Up reached `(20,10)` beside Nella. Native tree/reed coverage is visible. Nella, unsupported actors/works and planted crop states remain rough. |

The collision, harvest and plot receipts record20HP; this was not a danger-free demonstration. Native frame/resource tests cover DryBrush, but this seed64 route has no initial DryBrush and does not establish an observed live brush/fire interaction. Each capture has a sibling observational JSON; recorded hidden owners are diagnostic data, not rendered disclosures. [Cleanup](Verification/Live16x24/north-vegetation-native-cleanup.json) confirms Play/pending/compile/isolation/view flags false and SampleScene restored. Performance receipts and their bounds belong to the vegetation packet; this route record makes no Steam Deck performance claim.

### Review and next action

- 🟡 Corrected before implementation: broader Spread admission would admit POI replacements; each exact north address now checks its own POI and current cached graph. Native counter-cases fail closed.
- 🟡 Corrected design premise: source crop stages differ, only two soils are reserved, and keeper/tray changes must invalidate remote cord cells. The future runtime design uses the maintained claim-part index, not the old per-cell full-zone search.
- 🔵 Independent code review found no actionable P1/P2. Symmetry: both north sites share identical current-graph/POI authority; glade/cellar and fullscreen exit retain their earlier contracts. Native tests cover positive and flipped map/identity/owner/fog conditions.
- 🧪 Not yet accepted: remaining north native actors/works/animation, Nella's approved sprite binding, planted crops/products, persistent cords, lower-crop moisture under another actor, full crop-growth/permission/revisit loop, whole-route profiling and release activation. New snake/bird silhouettes must replace humanoid rough markers. The vegetation packet's accepted native reconstruction does not make the generated concept/extraction a production atlas.
- ⚪ Ordinary route found blocking walls and enemy danger as expected; art must make both more legible, not remove them to make a screenshot easier.

**Can verify:** historical scope/lifecycle tests plus the later312-case vegetation/consumer checks; actual keyboard travel through both northern borders, examine, Calm and finite purchase in the greybox run; later native vegetation, real hedge collision and BerryBush harvest/removal; captured plot arrival and isolated cleanup. **Cannot verify:** complete north appearance, new actor animation quality, complete crop/permission loop, all-seed live coverage, physical Steam Deck controls/performance or normal release activation.

**Next bounded production packet:** native north actors and source-specific works, then approved keeper binding plus Nella's crop/tray/cord states and direct products. Six ordinary vegetation identities are already integrated; remaining incidental hazards, containers and outcomes stay in the measured parts ledger. Reuse the actual greybox and native captures while geometry stays unchanged. Follow RED→native source/contact→integration→GREEN→ordinary before/after capture. Do not repeat the census or another scope-only packet. Approved keeper/cook bytes and prior3D remain recoverable. East/kitchen still await their own scope/art/route. This is original CoO presentation guided by the user's flat Qud-like preference; no Qud source or sprite is copied.

Historical scope/concept packet changed catalog scope and renderer comments; north tests/meta and obsolete cellar-pin correction; plan/contracts/parts audit; capture/receipt/test evidence; imagegen prompts/concepts and living handoffs. The later vegetation packet adds its source assets, optional runtime atlas/routes, tests and acceptance receipts, detailed in [its own record](LIVE-16X24-NORTH-VEGETATION.md). Neither packet changes blueprints, generation, saves, gameplay rules or normal launch defaults.
