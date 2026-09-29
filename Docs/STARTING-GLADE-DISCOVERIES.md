# Starting glade: visible discoveries

Status: complete after `bd2e6639a`. Three new starting-area discoveries and their prop refinements passed focused native Unity checks and the actual in-game walkthrough.

The player's first area bypasses ordinary Spread composition, so recent exploration additions are absent from the first screen. This pass changes that actual area rather than adding another rare encounter or verification framework.

## Plan and implementation prompt

Ship three recognizable places in the starting glade: a substantial reed pond with a finite draw basin and dry banks; sheltered crop strips with three finite grain harvests and a native cooking fire; a broken western store-room with a movable beam shortcut and an open alternate route. Keep the original chest, quartz, villagers, enemies, spawn and four exits. Reuse the approved 3D palette and models; inspect actual rendered composition before adding assets. Add concise local examine text to the features and existing sign, without quest arrows or automatic rewards.

Implementation prompt: build these places in ReferenceGladePlan and stage their native owners in ReferenceGladeBuilder. Run failing content/interaction/route tests first, then the targeted glade and neighboring feature tests. Reuse the existing native glade walkthrough for screenshots and save/travel checks; do not create a new audit framework. Fix meaningful failures, document minor limitations, and publish the working content and this living record together.

## Verified scope and corrections

- Fresh start is `Overworld.11.10.0`, local `(40,12)`. Ordinary new games use a varying seed; seed64 is only an audit choice.
- ReferenceGladeBuilder owns the complete starting layout. It already stages owners before publication and refuses occupied zones. New areas belong there, before random dressing.
- West is Sill. North is Fallow; east/south are FlowerMeadow. They keep their existing content in this slice.
- FieldHarvest leaves spent stubble. SpreadDrawPoint has three drams; decorative wet ground is not an unlimited refill. Campfire uses existing heat/fuel/cooking/rest rules, with finite cooking enabled for this new station.
- Beams are physical obstructions, not sight shields. The western `y16` through-route stays open, and the alternative is traversable with the beam untouched.
- Existing saves keep their literal cached glade. This is fresh-game content; no reset or implicit saved-world migration.
- No external Qud source parity is claimed. This is original level composition using existing mechanics.

## Performance and verification boundaries

Content changes run once during zone generation. The prop kit adds twelve contact-source shapes at presentation initialization; the existing contact pass and its per-frame contributor/raster bounds are unchanged. No new per-frame scan, listener, cache or AI is added. Existing batched terrain, water, vegetation and prop models handle presentation. Focused real Unity tests and the existing native walkthrough will provide acceptance; no new full-suite or frame-time claim is planned.

## Shipped composition and directions

From new-game arrival `(40,12)`:

| Place | Where | Useful change |
|---|---|---|
| Reed pond | Northwest, center `(34,5)`; basin `(36,9)` | Broad wet ground and pale reeds, a dry bank, three finite drams, and an empty takeable waterskin at `(37,10)` |
| Gleaners' shelter | Northeast; fire `(42,8)`, fields `(44–50,10–14)` | Low ruined walls and twenty crop strips; three ripe owners yield one emberwheat each, which can be cooked beside the hot fire |
| Broken store-room | West/southwest; beam `(26,19)` | A new wall partition and short doorway. Pull from `(27,19)` toward the east to open it; the northern `y16` passage stays open without hauling |

The sign at `(43,11)` gives local directions. Examine the basin, fire or beam for their practical uses. The native 3D kit handles water, crop strips, stubble and the vessel. Live inspection identified flat fire and timber fallbacks. The glade kit now adds detailed stone-ring hot/cooled coals and cut timber, four variants each. Timber keeps its grain after hauling; coal appearance follows actual heat. Original forty model meshes/prefabs and the twenty-four-color palette remain unchanged.

Utility budget added: one empty three-dram waterskin, three drams of water, three grain harvests, and one finite cooking fire using existing fuel/heat rules. Original chest supplies, quartz, people and hostile placements remain. No free refill, renewable crop loop, quest marker, or extra enemy wave is added.

## Self-review and results

- 🟡 Fixed: the peer review caught new north–south walls inheriting horizontal model orientation. Added correct quarter-turns for the western partition and the shelter return, with four orientation tests.
- 🟡 Fixed: the basin was usable only if the player already carried a suitable vessel. An empty native waterskin now lies on its dry approach; a failing pickup-content test preceded this addition.
- 🟡 Fixed after live inspection: a generic riverbank owner required river-only animation metadata and obscured the pond with its fallback geometry. A failing content test preceded removing that redundant owner; the pond now uses existing ground with permanent water coating. The first native attempt retained six passing checks, then stopped at an unclosed Look menu; its report also records the repeated bank-animation exceptions. The walkthrough now closes that menu with Escape.
- 🟡 Fixed after model import: the renderer’s contact-source cap still allowed only eighty models. The imported fifty-two glade variants plus forty approved environment variants exceed that cap; thirteen native regression cases failed. Raised the exact bound to ninety-two without changing per-frame contributor or raster limits.
- ⚪ The pond's surrounding wet terrain is scenery; only its clearly described basin supplies drinking water. Existing saves keep cached terrain. A new game is required to see this new starting layout.
- 🧪 Ordinary exploration balance and player comprehension require playtesting beyond this short walkthrough. No claim that three authored places solve repetition throughout the world.

Q1–Q4 review: placement is staged before terrain is committed; the new owners use the same native interaction paths as their wilderness equivalents; near/far harvest, hot/cold cooking and full/empty basin pairs reject illegal use; the doc names actual coordinates, finite budgets and saved-world limits. Original travel routes and reward owners are asserted across three seeds. No Qud source-contract parity is claimed.

Real Unity EditMode evidence: initial 24 cases gave **3 passed / 21 failed** before the layout implementation. The next run exposed a fixture omission (the native harvest factory was not installed in test setup); this was corrected in the fixture, not gameplay. The later 29-case detail run gave **26 passed / 3 failed**, exposing two wall orientations and the missing empty vessel before those changes. Pre-art six-fixture regression run: **137 passed / 0 failed / 0 skipped**, including **29 new cases** at that stage. The subsequent pond counter brings content coverage to thirty, and six model cases cover thermal state, hidden-owner refusal, stable hauled geometry and unchanged outside-glade controls. The model RED was **2 passed / 4 failed**. Thirty source-kit checks passed after adding the twelve variants. These are native Unity results, not the headless runner.

Native second walkthrough: all **17 checks passed**, **0 runtime errors**, including actual keyboard pickup/draw, harvest, cooking, beam hauling, F5/F6 saved depletion/movement, and zone exit/return. The existing report completion gate still expected eleven checks; updated it to seventeen (eighteen in profiling mode). Final ten-fixture regression after import and the contact-cap correction: **204 passed / 0 failed / 0 skipped**, including **36 new cases**. The source-kit suite also passes **30/30**. The next model-enabled native run passed the eight startup/new-action checks but stopped during the subsequent combat-exposed walk. Its receipt is retained. The bounded final pass allows up to two actual original starting tonic units through native inventory keys and dismisses naturally earned advancement announcements, using existing helpers. It does not grant equipment, health, invulnerability or altered enemy behavior. Final repeat: **17/17 checks passed, complete=true, 0 runtime errors**, in36.20seconds. It did not need a healing tonic. The prior interrupted route remains evidence of ordinary combat variability, not a guarantee of safe traversal.

Files: original generator/source-kit JSON and imported twelve prop variants; library, recipe, importer and contact-source bound; updated existing art/count pins; `StartingGladePropArtTests.cs` and meta; `ReferenceGladePlan.cs` composes the three places; `ReferenceGladeBuilder.cs` stages native parts, descriptions, wet ground and model orientation; `StartingGladeDiscoveryTests.cs` and its meta cover content, mechanics and counters; `ReferenceGladeNativePlayer.cs` extends the existing isolated walkthrough with actual pickup/draw/harvest/cook/haul keys and post-load state checks.


## Visual acceptance and evidence

Inspected the actual final new-game arrival and the full-reveal overview. The pond and reed ring, dry basin approach, twenty field strips, shelter walls and stone-ring cooking fire are immediately distinguishable. Timber now has broad exposed grain and broken ends instead of a plain slab. The older forty mesh/prefab files remained byte-identical after import; the existing material only gained serialized zero/default shader properties. No imported actor geometry changed.

[Actual new-game arrival](Verification/StartingGladeDiscoveries/Native/01-gameplay-arrival.png) · [World overview, fog revealed for composition inspection](Verification/StartingGladeDiscoveries/Native/03-world-only-full-reveal.png) · [Moved timber](Verification/StartingGladeDiscoveries/Native/discovery-03-opened-ruin-shortcut.png) · [Final native report](Verification/StartingGladeDiscoveries/Native/report.json) · [204 native EditMode cases](Verification/StartingGladeDiscoveries/unity-editmode-final.json).

Can verify: real native owners and keyboard actions, finite resource use, moved collision, checkpoint replacement with retained state, travel/return, and submitted in-game appearance in these captures. Unity returned to the previous SampleScene in Edit mode after its isolated test-save session.

Cannot verify: natural player discovery, long-term encounter balance, standalone-build performance, or variety throughout the rest of the world. This batch changes the actual starter area; it does not revise every biome or migrate existing saved chunks. Subsequent content batches should keep prioritizing equally visible changes in ordinary exploration over more audit infrastructure.
