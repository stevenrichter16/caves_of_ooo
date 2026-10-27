# Stump fauna — bounded static native coverage preflight

2026-10-27 UTC. **Status: source/image review and plan only. No fauna production art, runtime integration, tests or acceptance have been produced by this phase.** Root is completing the Stump solid-perimeter correction first. This plan does not alter that work or its delivery status.

The next finite slice is nine exact living fauna identities, with readable static bodies in the flat 16×24 world and authorized examine view. Preserve working animation and legacy sources; defer new nuanced animation. This is CoO-original visual integration, not a Qud parity claim. Worldwide sprite coverage and chunk fullness remain the larger objective; these nine actors do not close Stump, Sodden or any entire biome.

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

A small nine-cell derivative atlas (provisionally 144×24) is sufficient if the five reuse candidates and three padding candidates pass actual comparison. Five cells would preserve existing source pixels, three would preserve their original 16×16 pixels with transparent padding, and MawToad would be new native art. This is a proposed production scope, not an authored asset or fixed acceptance decision. An individual failed silhouette/contrast check may justify a named derivative correction without redrawing the rest.

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

1. Nine exact positive bodies and authorized world/examine equality; paired same-glyph wrong species/explicit reskin refusal. Test the current physical owner, not a synthetic texture-only selector.
2. Current graph/disclosure/selection, covering-owner changes, custody, invalid footprint and optional resource/disposal counters. Missing fauna atlas must keep glyph identity and leave accepted terrain/portable items intact.
3. Submitted full-to-dirty movement and death/removal clear the previous cell and preserve real ground/hazard layers. A real death's corpse owner must not resolve as a living animal; preserve the Corpse factory hook in the fixture.
4. Actual TakeDamage followed by the real single Render event produces a visible essential cue; paired quiet repaint settles correctly. Test a valid applied color-changing status with an explicit successful-application precondition; do not assume every material permits every effect. Portrait queries and neighbor reclaim must not consume additional Render events or flash frames.
5. Exact species portability in a non-Stump current graph; no broad opening/terrain admission. An actual or source-grounded MawToad Sodden fixture is the highest-value shared-world case.

Run genuine native RED before runtime edits, implement the smallest necessary route, run the bounded GREEN and related reader/fog/actor regressions, then independent review. Record actual results only after root executes them. No test count or pass is claimed here.

## Playable production and acceptance sequence

1. Finish the solid-perimeter material correction. Reuse the actual five-area baseline geometry while unchanged; preserve old captures.
2. Approach and examine the real fauna owners needed for identity review via normal gameplay, with current zone/owner receipts. Use the reviewed five formation views as context, not proof of all nine close-up identities.
3. For any new or corrected body, use actual gameplay plus the approved cast as imagegen inputs; preserve exact prompts, references, outputs and critique. No concept output is itself production art.
4. Extract the finite source/state list. Freeze native cells, baselines and source-pixel reuse hashes; preserve all prior sheets and static sprites. Produce editable source and Point/no-mipmap imports.
5. Integrate after RED; play the same places. Compare small frogs against Gorge water, snakes versus ridge ground, Summit bodies versus corrected domes, and actor-over-Tank selection. Verify a real movement/damage/reader/death-or-removal sequence without changing habitats or spawn counts for screenshots.
6. Inspect whole gameplay views for readable, appropriately occupied habitat groups. Do not use new actor sprites to claim the still-missing crops, stairs, props, hazards, NPCs or special sites are complete. No generation-density changes are proposed by this phase.
7. Verify earned save/revisit and matched packaged delivery; share actual in-game screenshots. Root owns Unity/builds/Git and records actual source/remote commits and platform limits.

## Performance, self-review and continuation

Use a fixed nine-identity lookup and constructor-time cached Tile/Sprite resources; no hot-path LINQ, per-owner texture generation, Render-event redispatch or new per-frame component. Reuse dirty-cell and current-owner mechanisms. Profile actual submitted gameplay after integration; source review does not prove allocation/frame-time/device performance.

- **🟡 Planned blocker:** preserve the existing damage/status cue when replacing a glyph with a static native body; do not merge a cue-swallowing implementation.
- **🧪 Pending visual acceptance:** five exact reuse candidates, three native-cell adaptations and the new MawToad treatment need actual game comparison. No native fauna is accepted by this plan.
- **⚪ Explicit limits:** no nuanced animation, new AI/spawning/combat, corpse art expansion, multicell-pilot art, whole-region completion or Linux/Deck execution claim. All remaining biomes and areas remain in the coverage queue.

Implementation log: source blueprints, populations/habitats, death/corpse flow, static/animated native admission, reader authority and actual existing art were reviewed. Only this plan was authored; no runtime, tests or images changed. Current phase is **preflight complete, production not started**. Next action is root's material correction/delivery followed by the actual fauna baseline and finite reuse/concept trial. Future implementation must update this document and the coverage records in the same reviewed commit.
