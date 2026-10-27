# Stump fauna — bounded static native coverage preflight

2026-10-27 UTC. **Current status: first five delivered; next three passed native and bounded Editor comparison, source push/package pending.** First-five source `0ddd8b6abf89a22275db7c09dde39bbb47392c03` and delivery `d87669092c56d1f23bb99b7c33bbde8eff59ee69` are verified on main. Their 457 native passes/one known allocation-counter skip, bounded Editor/Mac play and Linux archive integrity are complete; Linux/physical Deck play and regional polish are not accepted.

The current finite slice is **SummitSinger, BrocchiniaSentinel and SkySari**, using unchanged existing 16×16 textures centered in the flat16×24 grid and authorized examine view. No padded raster is needed: a centered1×1world-unit sprite at16PPU occupies the same pixels as four transparent rows above/below within a1×1.5cell. Reader layout preserves square aspect and integer scaling; it may enlarge these differently from16×24 portraits. Existing five-species injection remains compatible. MawToad needs a separate crisp treatment; all-world sprite coverage and the later serious polish phase remain open. Historical proposals/checkpoints below are retained with their original scope.

## Evidence and scoped coverage

The actual five-area [Stump baseline census](Verification/Live16x24/stump-before-owner-census.json) contains 18 physical fauna owners across nine blueprint identities. These are census owners, not 18 individually approached/examined encounters. The five material trial views are reviewed in [stump-trial-composition-review.md](Verification/Live16x24/stump-trial-composition-review.md). In particular, the Gorge view shows weak green actor glyphs against water; RimForest/Summit show fauna letters among newly native materials. Those captures establish a real delivery gap but not a close visual baseline for each species.

| Exact identity | Census count | Actual baseline formation(s) | Source identity and essential distinction |
|---|---:|---|---|
| CascadeFather | 1 | CascadeGorge | Tiny pale-striped frog carrying young; passive and lore-silent. |
| GlasspaneFrog | 2 | CascadeGorge | Translucent green frog; distinct from CascadeFather despite shared `f`. |
| YellowfootWayfarer | 1 | Grainfield | Yellow-footed tortoise with a shell, not a toad despite shared `t`. |
| Wardline | 2 | Grainfield, ButtressRidge | Olive colubrid; distinguish from SariSnake despite shared `s`. |
| SariSnake | 2 | Grainfield, ButtressRidge | Heavy pit viper with wet-stone coloration. |
| MawToad | 2 | Grainfield | Broad mouth and squat body; also appears in Sodden. |
| SummitSinger | 5 | SummitScrub, RimForest | Brown frog with a dark W on its back. |
| BrocchiniaSentinel | 2 | SummitScrub, RimForest | Lichen-colored lizard with a yellow eye, distinct from a Tank rosette. |
| SkySari | 1 | RimForest | Pale eagle with dark underwings; do not turn its lore size into an invented multicell footprint. |

Canonical source anchors are `Assets/Resources/Content/Blueprints/Objects.json:34617` (MawToad), `:35733` (SariSnake), `:35790` (Wardline), `:35826` (CascadeFather), `:35860` (GlasspaneFrog), `:35894` (YellowfootWayfarer), `:36519` (SummitSinger), `:36550` (BrocchiniaSentinel), and `:36582` (SkySari). Stump band populations are in `Assets/Scripts/Data/Tables/PopulationTable.cs:228–292`.

## Actual source-art review against the approved style

The reviewer opened all nine source images and the approved [opening cast contact](../ArtSource/Live16x24/cast/opening-cast-contact.png). The approved cast favors compact, crisp silhouettes, restrained palettes, clear body mass and feet/baseline. Source dimensions and pixel statistics below were read from existing files; no images were edited or exported during this audit.

| Existing image | Source review / proposed treatment | Acceptance boundary |
|---|---|---|
| `Assets/Resources/Sprites/Actors/sari_snake_sheet.png` | **Exact reuse acceptable for a bounded trial.** Existing 64×384 sheet is genuinely composed of 16×24 cells. First idle cell has an 11×16 opaque body and five visible RGBA colors. Keep its own stone-colored body; do not substitute Wardline. | Current native-background contrast and actual silhouette distinction still need gameplay review. |
| `Assets/Resources/Sprites/Actors/wardline_sheet.png` | **Exact reuse acceptable for a bounded trial.** Same native cell dimensions, its own olive palette and five-color body. | Similar pose to SariSnake makes a side-by-side identity check necessary; shared glyph/shape cannot be the selector. |
| `Assets/Resources/Sprites/Actors/cascade_father_sheet.png` | **Exact reuse acceptable for a bounded trial.** Native cell, compact pale body, six visible colors. | Confirm it remains legible against actual SprayPool and distinguishable from GlasspaneFrog at normal zoom. Do not promise readable juveniles until observed. |
| `Assets/Resources/Sprites/Actors/glasspane_frog_sheet.png` | **Exact reuse acceptable for a bounded trial.** Native cell, distinct green body, six visible colors. | Current green water is the critical contrast test. The 16×11 occupied silhouette should remain small without becoming an indistinct dot. |
| `Assets/Resources/Sprites/Actors/yellowfoot_wayfarer_sheet.png` | **Exact reuse acceptable for a bounded trial.** Native cell with a compact 14×13 shell/body and six visible colors. The shell differentiates it from the frog/toad family. | Verify the feet/shell and scale beside the player and the stone ridges. |
| `Assets/Resources/Sprites/Environment/summit_singer.png` | **Needs native-cell adaptation, not a demonstrated full redraw.** Existing 16×16, five-color frog. A separately authored lossless padded 16×24 derivative is a candidate. | Keep the original untouched; verify its dark W/body contrast on the corrected stone and beside Tree. |
| `Assets/Resources/Sprites/Environment/brocchinia_sentinel.png` | **Needs native-cell adaptation, not a demonstrated full redraw.** Existing 16×16, five-color lizard. Candidate lossless padded derivative. | Preserve a readable yellow eye and lizard silhouette without inventing invisibility or making it look like Tank foliage. |
| `Assets/Resources/Sprites/Environment/sky_sari.png` | **Needs native-cell adaptation and a scale check.** Existing 16×16, seven-color bird reaches the frame width. Candidate lossless padded derivative. | Preserve the real one-cell footprint; validate recognition as a bird rather than an oversized decorative landmark. |
| `Assets/Resources/Sprites/Environment/maw_toad.png` | **Needs a new crisp native treatment.** Existing 16×16 source has 80 visible RGBA colors and visibly softer shading than the limited-palette sources and approved cast. Use its mouth/body identity as reference. | New concept/native source and actual comparison required; no permission to overwrite the old sprite. |

“Acceptable for a bounded trial” is a source-art recommendation, **not** accepted in-game native delivery. The five sheet resources are mapped by `Assets/Resources/Content/Data/EntityVisuals.json:201–276`; actual slicing uses `EntityVisualCatalog.cs:43–44,146–175`: four columns, sixteen rows, 16×24 cells. The PNG importer's legacy sprite rectangles are not the runtime slicing contract. A proposed single static south-idle cell is the top-left image cell, Unity rect `(0,360,16,24)`; confirm the chosen facing against `EntityVisualFacing` before freezing extraction. Preserve original PNGs, metadata and all other poses.

The chosen top row is confirmed as South by `Assets/Scripts/Gameplay/Entities/EntityVisualFacing.cs:9`; West/East/North follow. This verifies the proposed extraction coordinate, not in-game acceptance of that pose.

The earlier provisional nine-cell144×24 derivative atlas is **not the current implementation choice**. The first five will borrow their existing sheet textures and create only five cached static Sprite/Tile pairs; no PNG extraction or new atlas is needed. The three16×16 adaptations and MawToad remain separate later source work. A named failure in an actual silhouette/contrast comparison may justify a separately reviewed derivative, while preserving all prior sources.

## Verification sweep — corrections before implementation

| Premise to avoid | Source-verified correction |
|---|---|
| All nine missing world sprites need nine new drawings | Five have true 16×24 actor-sheet cells; four have legacy 16×16 environment art. Flat delivery is a separate issue. |
| The legacy actor registry already provides these bodies in flat mode | `FlatPixelActorMotionCatalog.cs:61–74` admits only Player/PetDog/MarlbackScrabbler/MarlbackGleaner. `AnimatedEntityRenderer.FlatPixel.cs:186–191` uses that and the local Patchbearer catalog. These nine are not currently admitted by the flat native catalogs. |
| The new static body can ignore current render events | **Prospective blocker:** `DamageFlashPart.cs:56–59` changes the actual Render event color after TakeDamage, but `ZoneRenderer.FlatPixelPaint.cs:40` and `EnvironmentSpriteRenderer.cs:351–359` use `flat.ForegroundColor` for native art. A simple white native body would swallow that essential cue. Tests must require a narrowly preserved event-color/hurt cue without firing a second Render event. |
| Creature owners resemble material-less scenery | Creature inheritance includes Body, Brain, DamageFlash, Corpse, Flesh Material and Thermal (`Objects.json:2700–2828`). Do not copy scenery's no-Material/no-Thermal predicate. |
| Healthy/passive/default stats are required to recognize the body | Normal damage, attitude, faction or encounter scaling can change while the same species remains. Do not require exact original HP, damage dice or hostility merely to draw a neutral body. Preserve actual status/damage cues. |
| Habitat controls repaint-time visibility | Habitat is a placement filter: CascadeFather requires SprayPool; Singer is near Tree; Sentinel near Tank (`StumpFaunaHabitat.cs:12–15`). Animals can move. Do not hide an existing actor after it leaves its initial habitat. |
| Urqu/Ecology flags should toggle existing sprites | `PopulationTable.cs:236,260,272,287,292` uses these as population gates. They do not authorize hiding current cached owners. |
| The sentinel hides/disappears in the Tank | `AIBromeliadRetreatPart.cs:24–31,72–79` makes ordinary movement toward cover. It never changes Render.Visible. Keep selected visible actor ownership above the plant. |
| A creature corpse is the same actor with zero HP | `CombatSystem.HandleDeath` fires Died, then removes the actor; `CorpsePart.cs:176–199,239–252` creates a separate corpse, recording SourceBlueprint/SourceID and optional Harvestable. Never give a corpse the living body because its SourceBlueprint matches. |
| Every corpse already has matching native art | `FlatPixelAftermathCatalog.cs:39–41` only recognizes Magpie/Viper remains and VenomGland. These nine species' corpse art remains a distinct gap. Death/removal must be truthful; this first slice must not claim complete species lifecycle art. |
| A portrait API grants authority to show a creature | `ZoneRenderer.FlatPixel.cs:49–60` and `ExaminePortraitRenderer.cs:71–81` require a current authorized document/context. Current world subject/selection, disclosure and custody remain separate from semantic sprite lookup. |

## Minimal runtime proposal and essential states

Propose a small exact-owner `FlatPixelStumpFaunaCatalog` with cached static Tile/Sprite references, `HasKnownRole(Entity)` and a pure semantic resolver. Reuse the established Greybox world selection and authorized reader path. Do not open `IsAdmitted`, add a parallel renderer, invoke gameplay from a catalog or instantiate all legacy motion assets just to obtain one static cell.

Recognition must require exact blueprint and canonical glyph/visual identity, legitimate owned Render/Physics/Body/Brain, visible non-carried/non-equipped live ownership and the supported one-cell footprint. Explicit VisualID/VisualVariant/reskins, wrong/duplicate/foreign essential parts, stale registration, hidden/remembered/noncurrent cells, emptied stacks and unsupported enlarged bodies retain truthful fallback. The real multicell MawToad pilot is not covered by a one-cell silhouette. Do not select by glyph, display name, ancestry or corpse provenance.

The body should remain portable across legitimate current world graphs, including normal travel or an actor's relocation. This is exact species portability, not admission of regional terrain everywhere. MawToad has actual Sodden population/copse sources (`PopulationTable.cs:590,605`; `SoddenCompositionPlan.cs:187`); GlasspaneFrog and YellowfootWayfarer have explicit Felling/Morrowfast sources (`FellingScenePopulation.cs:40–42`; `MorrowfastSceneRuntime.cs:178`). Those are useful cross-area counterchecks and do not certify the other areas' full graphics.

Essential state behavior:

- Neutral living body and movement to a new cell; old cell/claim must clear. Standing on water, floor or Tank must not erase that substrate or its hazard mark.
- Damage and visible status changes must retain their real cue. Add a narrowly scoped submitted-color/hurt solution for these static actors, using the existing one-event paint result or a measured essential cue. Do not broaden all existing native material tint behavior to solve this actor issue.
- Hidden, remembered, removed or dead actor must not retain a live sprite or stale portrait. An actual death must leave the separate corpse/loot owners through their existing truthful routes. Generic native corpse art is deferred explicitly; no living-body disguise.
- Revisit/save/restart preserves identity and current state. World/examine use the same chosen body. Carried corpses/products use their own custody-authorized identity; no new living-creature inventory feature is implied.

## Proposed RED/countercheck scope

Use existing public renderer and examine fixtures. Keep tests finite and meaningful; do not create another general framework.

1. Five exact positive bodies and authorized world/examine equality; paired same-glyph wrong species/explicit reskin refusal. Test the current physical owner, not a synthetic texture-only selector. The other four identities are explicitly outside this implementation.
2. Current graph/disclosure/selection, covering-owner changes, custody, invalid footprint and optional resource/disposal counters. A missing/malformed one-species sheet must keep that species' glyph while the other four resources and accepted terrain/portable items remain intact; borrowed textures survive disposal.
3. Submitted full-to-dirty movement and death/removal clear the previous cell and preserve real ground/hazard layers. A real death's corpse owner must not resolve as a living animal; preserve the Corpse factory hook in the fixture.
4. Actual TakeDamage followed by the real single Render event produces a visible essential cue; paired quiet repaint settles correctly. Test a valid applied color-changing status with an explicit successful-application precondition; do not assume every material permits every effect. Portrait queries and neighbor reclaim must not consume additional Render events or flash frames.
5. Exact species portability in a non-Stump current graph; no broad opening/terrain admission. First-five source-grounded controls are GlasspaneFrog/YellowfootWayfarer in Felling or Morrowfast, including legitimate renamed/peaceful resident variants. MawToad/Sodden is deferred with that species.

Run genuine native RED before runtime edits, implement the smallest necessary route, run the bounded GREEN and related reader/fog/actor regressions, then independent review. Record actual results only after root executes them. No test count or pass is claimed here.

## Playable production and acceptance sequence

1. The material source/delivery gates are complete within their bounds. Preserve those captures and the unchanged current geometry; root now captures close first-five fauna baselines with normal controls.
2. Approach and examine the real fauna owners needed for identity review via normal gameplay, with current zone/owner receipts. Use the reviewed five formation views as context, not proof of all nine close-up identities.
3. For any new or corrected body, use actual gameplay plus the approved cast as imagegen inputs; preserve exact prompts, references, outputs and critique. No concept output is itself production art.
4. Extract the finite source/state list. Freeze native cells, baselines and source-pixel reuse hashes; preserve all prior sheets and static sprites. Produce editable source and Point/no-mipmap imports.
5. Integrate after RED; play the same places. Compare small frogs against Gorge water, snakes versus ridge ground, Summit bodies versus corrected domes, and actor-over-Tank selection. Verify a real movement/damage/reader/death-or-removal sequence without changing habitats or spawn counts for screenshots.
6. Inspect whole gameplay views for readable, appropriately occupied habitat groups. Do not use new actor sprites to claim the still-missing crops, stairs, props, hazards, NPCs or special sites are complete. No generation-density changes are proposed by this phase.
7. Verify earned save/revisit and matched packaged delivery; share actual in-game screenshots. Root owns Unity/builds/Git and records actual source/remote commits and platform limits.

## Performance, self-review and continuation

Use a fixed first-five identity lookup and constructor-time cached Tile/Sprite resources; no hot-path LINQ, per-owner texture generation, Render-event redispatch or new per-frame component. Reuse dirty-cell and current-owner mechanisms. Profile actual submitted gameplay after integration; source review does not prove allocation/frame-time/device performance.

- **🟡 Planned blocker:** preserve the existing damage/status cue when replacing a glyph with a static native body; do not merge a cue-swallowing implementation.
- **🧪 Pending visual acceptance:** five exact reuse candidates, three native-cell adaptations and the new MawToad treatment need actual game comparison. No native fauna is accepted by this plan.
- **⚪ Explicit limits:** no nuanced animation, new AI/spawning/combat, corpse art expansion, multicell-pilot art, whole-region completion or Linux/Deck execution claim. All remaining biomes and areas remain in the coverage queue.

Implementation log: source blueprints, populations/habitats, death/corpse flow, static/animated native admission, reader authority and actual existing art were reviewed. Only this plan was authored; no runtime, tests or images changed. Current phase is **first-five preflight complete, actual close baseline underway; no fauna acceptance claimed**. The material source and delivery gates are already complete. Next action is baseline review, genuine native RED and the minimal direct-sheet integration; source-only reuse requires no new concept or PNG unless an actual named visual defect demands a correction. Future implementation must update this document and the coverage records in the same reviewed commit.


## First-five unchanged native reuse contract — 2026-10-27

Root explicitly selected CascadeFather, GlasspaneFrog, YellowfootWayfarer,
Wardline and SariSnake as the next slice. The earlier census contains eight
owners across these five identities, not eight close-up interactions. Actual
close baselines are being captured by root now; none are inferred from this
source inspection. This documentation pass writes only this phase document.
No new PNG, metadata, runtime or tests were authored or accepted here.

### Direct cached sprites selected; extraction is unnecessary

`build_comparison_plan` independently agreed to direct borrowed sheet textures,
with one constructor-cached Sprite and Tile per species. Every actual source
PNG was opened in this pass: the frog pair has pale versus green compact bodies,
the tortoise has a recognizable shell and feet, and the two snakes retain their
distinct olive versus wet-stone palettes. They remain candidates for actual
water/stone contrast review; recognizable source art is not gameplay acceptance.

All five files are64×384, with Point filtering, no mipmaps, sRGB and uncompressed
imports; `isReadable` isfalse and need not change. Use the South-idle first frame:
Unity rectangle **`(0,360,16,24)`**, equivalent to decoded PNG top-left
`(0,0)-(16,24)`. Create a **centered(.5,.5),PPU16,FullRect** Sprite for the static
tile path, matching `FlatPixelIncidentalActorCatalog.cs:90–96`. Do not directly
reuse a legacy `GetFrame` Sprite: `EntityVisualCatalog.cs:43–47` uses default
pivotY=0 for its animated placement, and `:146–175` slices the full64-frame sheet.
The five cached static sprites need the same pixels, not that placement contract
or320 generated legacy frame objects.

The ordinary examine path already derives normalized UV from `sprite.rect`
(`ExaminePortraitRenderer.cs:197–204`), so a sheet subrect does not require a
separate portrait image. Runtime `Sprite.Create` reads the texture asset without
CPU pixel extraction. The adapter owns only its five created Tiles/Sprites and
must leave all borrowed textures, PNGs, metadata and motion definitions intact.
Each optional resource is independent: one absent/malformed sheet refuses only
that body's native art, with no implicit fallback resource load.

A five-cell80×24 derivative could reduce loaded source texels, but is not needed
for correctness. Five64×384RGBA sheets represent about480KiB of theoretical raw
texels versus7.5KiB for such a derivative; these are arithmetic source sizes,
not measured Unity/GPU memory or a performance claim. Avoid adding extraction,
new import state and duplicate editable pixels without an actual need. Revisit
only for a named measured resource or visual defect after gameplay comparison.

### Exact resource and state mapping

| Exact living owner | Borrowed Resources path | Canonical glyph | Native state in this slice | Required actual comparison |
|---|---|---|---|---|
| CascadeFather | `Sprites/Actors/cascade_father_sheet` | `f` | South-idle0, pale compact body | On real SprayPool, distinct from GlasspaneFrog; juveniles are not promised readable without inspection. |
| GlasspaneFrog | `Sprites/Actors/glasspane_frog_sheet` | `f` | South-idle0, green compact body | Actual water contrast and small-body visibility. |
| YellowfootWayfarer | `Sprites/Actors/yellowfoot_wayfarer_sheet` | `t` | South-idle0, shell and yellow feet | Actual Grainfield scale/readability, distinct from toad glyph siblings. |
| Wardline | `Sprites/Actors/wardline_sheet` | `s` | South-idle0, olive snake | Same-view distinction from SariSnake against stone/vegetation. |
| SariSnake | `Sprites/Actors/sari_snake_sheet` | `s` | South-idle0, wet-stone snake | Distinct identity and essential damage cue against the current graded ground. |

Idle/walking/attacking display this same selected static body in this first
coverage slice. Existing original motion assets remain unchanged and available
to their prior presentation. This does not claim new animation or replace AI,
habitat/spawn rules, facing data, health or combat mechanics. Hurt/status
**feedback must remain visible**, even though no new hurt frames are authored.
Dead/removed owners cannot resolve as living bodies; their separate corpse/loot
owners retain truthful existing fallback. World and authorized examine use the
same source rectangle, with no fabricated living-creature inventory feature.

### Essential Render-color control

Current `ZoneRenderer.FlatPixelPaint.cs:35–42` dispatches one real Render event
but chooses `flat.ForegroundColor` for native foreground. The environment
reclaim path at `EnvironmentSpriteRenderer.cs:351–359` similarly bypasses the
computed receipt tint. `DamageFlashPart.cs:33–59` sets three red Render-event
frames after TakeDamage. Simply adding white native bodies would consume those
frames while hiding their cue, a blocking regression.

The runtime/test agent's planned narrow opt-in is
`FlatPixelGreyboxCell.PreserveRenderColorCue`, defaultfalse. For these first-five
bodies, a changed valid current Render-event color supplies the cue; an unchanged
semantic glyph color leaves the native pixel palette white/untinted. Carry the
same one-event result through the existing paint receipt for neighbor reclaim;
never redispatch from catalog, portrait or reclaim. This is a proposed contract,
not an implemented or tested result. Preserve all existing native material and
non-opted-in actor behavior. Native full/dirty/neighbor/quiet controls must verify
actual event count, visible damage cue and return to neutral; the existing
`FlatPixelGlobalPresentationTests.cs:82–98` is the prior glyph reference control.
A color-changing status test must prove that the real effect applied before
asserting its presentation. Neutral damage/renaming/faction changes alone do not
make the actor a different species.

### Current source pins

The following full PNG and metadata hashes were read without changing files.
The South-idle hash is SHA256 of decoded top-down16×24RGBA bytes, including
transparency. It records unchanged source pixels for later comparison; it is not
a Unity render or imported-texture hash.

| Owner | PNG SHA256 | South-idle RGBA SHA256 | Metadata SHA256 |
|---|---|---|---|
| CascadeFather | `0ed8191e5cd474bd4638e222845ad9512798b02eb36dbe31c40d793e1b7c40b3` | `f69f497608ae2386e28ef8139537a7ee4e777f9c9d704768a7f2029b286289a9` | `d1c9bb1fe74ac76e000e533f08c779a2718e24a5111442d91b19f102985e5725` |
| GlasspaneFrog | `62c2f930270156a86cd1a6566b855c5a255e4e0100e6deef4d2b9481df45f55e` | `0baf1b37a1566b5d073d240ba19d011c69407a6deaa99d798cd0216d025ac261` | `5b90d7c121322de0f3c5eed0ee76e5a242d04505a39f75f736b28ba669dcee85` |
| YellowfootWayfarer | `8f40e10710db5e0c920a23651bda03f3cd564a2ea8a4d2353a6864fe74926941` | `273c0e8d33dbf17cef414649a6bb309c90fb53845eb7ed7646ff4c1eaf8102fa` | `aeac1ea22d4da3027798f9a7c43eeb7c563c38d27505bfa3dedfa51a08e0d593` |
| Wardline | `298efe125ede340e1bba522818d7d4154afd9f0b04478a46cb02a051fdc74150` | `cc69dfc0d473701c1fc6fb917a9e3762a6890fc53ad4d301899b1a26b77523c2` | `36bf26a352d5f319b95139446e0731fa397191c5842b76d3db0d5977a60ca394` |
| SariSnake | `f23f498efe3b472febc8788c5a9553e279f55e7806e72d78ad9333e9a3f806ca` | `40af85e31ca8184e73cc2536bdc1e8bae1913d5e15273cf4eff1ebe7ded94aca` | `0a5677dc97fba5f3447f1e0edb113257486f8d8f5b58620e00b525dd8bb6529b` |

### Later scope remains explicit

SummitSinger, BrocchiniaSentinel and SkySari retain their existing16×16 sources
and await separate reviewed lossless native-cell adaptations. MawToad needs a
new crisp native treatment and its Sodden/multicell/corpse boundaries remain
separate. This first-five route does not silently admit any of those four.
Keep their10census owners and the full nine-identity queue visible; no species,
Stump, Sodden or all-world completion is claimed by this sub-slice.

Material delivery/evidence `16d98e625` is verified on main by root, following
source `98e3e167b8789608f783701490822a024ffa9963`; this task did not operate Git.
The separate mandatory all-world area polish phase remains required and its
`not_audited` records are not advanced by this plan. No hourly-report timestamp
is advanced here. Baseline/control captures, genuine native RED, minimal runtime,
GREEN/related regressions, independent review, real after/restart and package
checks still gate first-five acceptance.


### Active continuation and hourly report — 2026-10-27 13:27 UTC

Root sent the requested itemized hourly update: completed Stump material source
98e3e167b and delivery16d98e625 on main,663 native passes and bounded Mac
travel/readers/save/restart, Linux archive-only verification; first-five fauna
baselines/tests in progress; next goals unchanged native-cell integration and
real world/reader/damage/status comparison. Other species/props/crops/hazards and
all-world coverage remain open. Dedicated area polish remains separate and
optional animation deferred. **Last actual hourly report13:27UTC; next14:27UTC.**

Root owns isolated Play token1a734e64020d4141a5e5a8ca8b1d4afc, using the earned
stump-final68 fixture. Initial Continue attempted the launcher marker before
active save selection was refreshed; the resulting empty-marker gzip error is
an operator setup issue, not an earned-save corruption. The real fixture then
loaded normally. Actual Gorge approach and cascade-father world menu/reader are
captured as stump-fauna-before-01..04. A two-Escape batch left Look and performed
ordinary movement/opened Skills; that interim image is explicitly retained as
stump-fauna-interim-05-skills, not a frog reader. Refreshed Look selection then
opened the real visible GlasspaneFrog60396 at19,7, captured06. No source art or
production runtime changed at this boundary; tests are being authored for RED.


## First-five integration checkpoint — 2026-10-27 13:51 UTC

**Current pass: integrated and observed in bounded ordinary Editor play; source
push and fresh package delivery pending.** Five unchanged South-idle cells now
replace glyph bodies for CascadeFather, GlasspaneFrog, YellowfootWayfarer,
Wardline and SariSnake. They use the same art in authorized examine portraits.
No new PNG or animation was produced: the approved exact cells are reused,
centered in the native grid, with all ten original PNG/meta hashes unchanged.
This is the documented reuse path, not an omitted new-art pipeline.

The narrow catalog creates five cached Sprite/Tile pairs, borrows original
textures and rejects malformed/repurposed/dead/custodied/multicell bodies.
Current-graph, selected-owner, visibility and authorized-reader gates remain
upstream. A native-body opt-in preserves the real one-event damage/status color
cue and carries it in the same cell receipt as the independently retained lit
glyph tint; reader queries and neighbor reclaims consume no extra Render events.
The original legacy presentation, source motion and other native kits remain.

### Verification and corrections

- Genuine pre-implementation RED:44/44failed, native runtime absent; retained
  XML, raw receipt and the exact original test source. Two stale-reader assertions
  were then corrected to exercise the authorized document boundary rather than
  an intentionally semantic helper; no runtime behavior was loosened.
- An initial GREEN retry still used the pre-import assembly:44failed again.
  Retained separately in `stump-fauna-preimport-retest.xml`. Explicit asset refresh
  loaded the frozen scripts; the next native run passed44/44 at13:41:38–42UTC.
- Ten related native suites:413passed,0failed,1skipped. Combined457distinct
  passes and1skip. The skip is an unsupported allocation-counter check; it is
  not a passing zero-allocation proof. XMLs and both independent reviews retained.
- Actual1280×800 before readers for all five plus after01–11 inspected. Ordinary
  Buttress→Grainfield→Gorge travel, selection/readers, actual combat/death and
  fresh-session Continue verified. No entity injection/teleport/reveal used.
- Sari65085 died through ordinary combat and became separate corpse69511 at24,10.
  Player38→29HP with bleeding then stopping; Wardline retained its native body
  and actual Witnessed state. A blocked approach struck a tree repeatedly before
  routing around it; this is not successful fauna damage evidence.
- Actual wet frogs remain native with a blue body cue; neutral reader portraits
  retain their pale/green identity. The transient three-frame hurt cue is tested
  natively, not claimed as captured in live screenshots.
- F5 earned save after71:turn8120,HP29/40,Overworld.2.5.0. A new isolatedPlay
  normally Continued the same save with identical player/visible frog state,
  inspected in after11. Normal saves/38foreign baseline files verified unchanged.
  Four isolated session roots remain archived outside the project, not deleted.

### Self-review and limits

🔵 Independent source/runtime and actual before/after reviews found no blocking
identity/contrast defect for this bounded five-body slice. Wet frogs converge
in dark-blue coloring and Sari is subdued; these are named later polish debts.
🧪 A single stationary Editor sample recorded34batches/34draws,9810triangles,
19640vertices and UnityStats.frameTime0.001712375seconds. This is neither a
comparative profile nor packaged/Deck performance evidence. No allocation claim.
⚪ Existing Metal/depth and finalizer warnings and the excluded setup/test-import
attempts remain in evidence; no clean-log claim.

Remaining: SummitSinger, BrocchiniaSentinel and SkySari native-cell adaptations,
MawToad treatment, other regional actors/props/crops/hazards, all other biomes,
settlements, interiors, underground and special sites. Broad sparse centers,
subtle blockers and black-backed unmapped objects remain coverage/polish debt.
No area polish state advances from not_audited and no nuanced animation is added.
Next: exact owned source/evidence push, fresh package checks, then the remaining
regional sprite families. Hourlyreport13:27→14:27UTC remains current.

Files: NEW FlatPixelStumpFaunaCatalog.cs/.meta, FlatPixelStumpFaunaTests.cs/.meta;
MOD FlatPixelGreyboxCatalog, ZoneRenderer.FlatPixelPaint, EnvironmentSpriteRenderer;
verification captures/fixtures/receipts/reviews and the same-commit living records.
See `Docs/Verification/Live16x24/stump-fauna-after-receipt.json` for exact source
and image hashes, route/actions, operator corrections and limits.


### First-five Stump delivery verified — 2026-10-27 14:03 UTC

Source `0ddd8b6abf89a22275db7c09dde39bbb47392c03` is verified on main.
Fresh Mac/Linux builds have zero errors and four warnings each. The new Mac
app passed bounded Gorge→Grainfield travel, actual frog/Wayfarer readers, F5,
full quit and fresh-process Continue. Six actual screenshots and earned
`Fixtures/packaged-stump-fauna72` retain turn8200,HP29/40. Linux archive integrity
passed independent review; Linux and physical Deck gameplay remain unverified.
See the detailed checkpoint above and the `packaged-stump-fauna-*`
receipts. Normal saves/preferences, all captured tracked source and previous
builds remain preserved; storage is inspection-only. No new runtime after the
457 native passes/one skipped allocation control. Remaining four Stump fauna,
Grovelands actors and all-world families remain queued; separate area-polish
states remain unchanged. Last actual hourly report13:27UTC; next14:27UTC.

Current Mac: `Builds/Verification/2026-10-27-stump-fauna-0ddd8b6ab-current/CavesOfOoo.app`.
Current Linux archive: `Builds/SteamDeck/2026-10-27-stump-fauna-0ddd8b6ab-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,
128,791,440bytes,SHA256 `97b85d1387a650a329b971fc0428c24e6a2f8dfac61500e740f173b6e96db69a`.
Independent archive review verified189files/13directories/202members and internal
checksums, paths, permissions and credits. The package helper preserves exact
44+414 XMLs:457passed/1knownskip, rather than silently reporting458passed.

Actual Mac exact executable PIDs88691 and89135 were verified separately; only
normal controls used. Two areas/two reader interactions are accepted, not all
five species' packaged interactions. Saved/restarted decodedJPEG world rectangle
[0,96,940,696] matches564000pixels; this is not lossless renderer-buffer evidence.
Restart log has0Exception lines,6premature-thread notices and1ComputeBuffer
cleanup notice, so no clean-log claim. Independent image review found no new
body/UI regression and retains wet-frog/subdued-Sari/fullness limits.

Protection correction: the first full snapshot overlapped Mac build startup
and correctly refused changed status. It was not used as proof. Before any
player launch, root backed up/restored the three known generated settings,
completed a stable fresh snapshot, then built Linux and played Mac. Final
restoration confirms50,772tracked file hashes,44preexisting dirty paths,
normalEditor saves and standalone absence/preferences. Previous packages and
all saved test roots remain preserved. Free space~423.8GB; no cleanup performed.
Current Unity state is clean Edit with no active test run. Delivery records
are ready for their separate exact reviewed main push; runtime remains0ddd8b6ab.


## Next-three pre-implementation correction record — 2026-10-27 14:13 UTC

Current pass: actual ordinary RimForest baseline captured, native integration not yet written. Root normally Continued the earned packaged-fauna72 fixture, traveled Grainfield2,2→RimForest2,3 and opened all three real readers. No simulation injection, teleport or visibility change. Player40,12HP29; Singer69476at17,8HP8, Sentinel69477at19,18HP10, SkySari69478at63,16HP58. Current UI labels all three Hostile; authored passive defaults must not be substituted for observed relationship state. Four `stump-canopy-before-01..04`1280×800 captures retain the actual letter bodies and generic glyph portraits.

| Preflight assumption | Saved source correction before runtime |
|---|---|
| Three16×16 bodies need three new paddedPNG adaptations | `FlatPixelWorldProjection.cs:66` uses1×1.5cells; centered fullrect16×16 at16PPU is lossless and unstretched. `AnnouncementUI.cs:291` preserves portrait aspect. Use unchanged source textures; no new imagegen/editable derivatives required for exact approved reuse. |
| Imported sprite slices can be trusted | These three importers retain stale stalactite_0 names/rectangles. Load Texture2D and explicitly create fullrect sprites; preservePNG/meta bytes. |
| Add the three to the five-input injection constructor implicitly | Keep the five-input constructor forwarding null for optional new slots. Default/eight-input constructors admit only independently valid16×16 sources; first five retain strict64×384 sheet checks. |
| A Tank hides the Sentinel | `AIBromeliadRetreatPart` only moves toward cover. Layer10Sentinel remains selected above passable layer2Tank. No hide/tank/habitat/Urqu repaint gate. |
| SkySari lore demands enlarged footprint | Current owner is one cell; preserve it. No invented multicell body or animation. |

Sources: unchanged `Sprites/Environment/{summit_singer,brocchinia_sentinel,sky_sari}` (binary alpha,5/5/7colors, Point/Clamp, no mips). Exact glyphs f/l/E; owned Body/Brain/Render/Physics and existing live guards apply. Concepts/parts: immutable approved source reuse; no newly generated pixels. Review actual Singer dark back, Sentinel yellow eye versus Tank, SkySari wing silhouette before acceptance. Keep sparse Rim/Summit centers, subtle dome blockers, crop/hazard/prop gaps and corpse identities explicit.

Plan: finite RED fixture first; minimal existing fauna catalog extension; GREEN/first-five/related counters; matched actual world/readers, movement/travel and save/Continue. Root owns Unity; test draft stays outside Assets during current Play. Independent review precedes exact owned push. No whole-biome polish advancement, no performance or packaged claim for these three yet. Last hourly report13:27UTC; next14:27UTC.


Native next-three RED19/19 failed at14:17UTC before implementation, expected missing bodies/8-input constructor. Initial GREEN1pass/18fail at14:18UTC exposed a test-premise error: Unity Sprite.bounds includes0.2z thickness. The intended square-pixel invariant is the x/y1×1extent, not renderer culling thickness. Corrected only the assertion to project bounds intoVector2; runtime unchanged. Both original test source and initial result are retained.


Hourly actual report14:27UTC sent: first-five main/package delivery complete; next-three native19+related266=285pass, actual matchingRimworld/readers and western/eastern movement views observed. Summit ordinary approach caused Sentinel67304from53,4to54,5 atop real Tank65605 (Well), remains visibleHP10. Final review/persistence/push pending. Goals finish/push/package next-three, then MawToad/Grovelands/all-world families; area polish separate and optional animation deferred. Next report15:27UTC.


## Next-three integration and actual-play checkpoint — 2026-10-27 14:30 UTC

Current pass: three more exact native bodies and portraits integrated and reviewed. All six PNG/meta files are unchanged. The only production change is the existing fauna catalog: an optional eight-input constructor with strict 16×16 new slots; the five-input constructor remains compatible. Three centered, cached FullRect sprites/tiles share the existing semantic guards and real event-color handling. No new renderer, scene decoration, AI, damage balance or animation. The first-five sheet mappings, previous presentation and approved sources remain intact.

Genuine RED: 19 failures before runtime implementation. Initial GREEN: one pass, 18 failures exposed only an incorrect Z-bounds test premise. After correcting the X/Y invariant, all 19 passed. Seven related suites passed 266 cases, including all 44 original fauna cases. Final total: 285 distinct passes, zero failures or skips. Original RED source, all XMLs and independent review are retained; prior executions are not added to this total.

Actual normal Continue loaded the exact before73 RimForest save with the same IDs, coordinates and HP. Four matched captures show the bodies and all three readers. Ordinary walking (20 west, one north, 33 east) produced normal-zoom western/eastern views spanning the chunk width; Singer and Sky Sari moved under their normal AI. Ordinary map travel reached SummitScrub3,4. Approaching via ten north and ten east steps moved Sentinel67304 from53,4 onto Tank54,5. Its native body remained visible and its reader showed the Tank among the cell contents. Tank65605 retains WellPart/layer2; Sentinel remains layer10, HP10. F5 saved earned after74 at turn9040, HP29/40. A fresh isolated Continue reproduced player50,2 and Sentinel54,5. Eleven after captures and owner receipts are retained. Test roots were archived; normal saves and38 foreign baseline files were verified unchanged. Storage inspection found about423.7GB free; nothing was deleted.

Self-review: 🔵 Independent runtime and actual-image reviews found no blocking regression in this bounded slice. Singer's dark W, Sentinel's yellow eye/limbs and Sky Sari's pale wing outline are visible. ⚪ Sparse Rim/Summit spaces, subtle blocking domes and remaining glyph props, hazards and corpses remain coverage and polish debt; no polish state advanced. 🧪 One Editor reader sample recorded44 batches/draws,12299 triangles,24613 vertices and0.741197169s frame time. This tool-interrupted sample is not a comparative profile or sustained-performance claim. Hurt/Wet cues are native-tested, not captured visually for these three. Fresh packaged, Linux and Deck gameplay remain unverified at this source checkpoint.

Files: modified FlatPixelStumpFaunaCatalog.cs; new FlatPixelStumpSummitFaunaTests.cs/.meta; matched captures, earned fixtures, receipts and reviews; package helper and same-commit living records. Next: reviewed source push, fresh Mac/Linux package gates, then crisp MawToad and Grovelands creatures plus remaining world families. The separate area-by-area polish phase remains required after broad coverage. Last actual hourly report14:27UTC; next15:27UTC.
