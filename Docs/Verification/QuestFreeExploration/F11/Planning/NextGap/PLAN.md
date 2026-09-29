# UNAPPROVED FUTURE MILESTONE — finite-water feasibility and next priority

29 September 2026. Planning only. No release candidate, shared edits, Unity work, Git changes, new world source, or adopted gameplay behavior. F11 packages remain frozen. Source hashes and private current-contract probe evidence accompany this note.

## Decision for prioritization

**Lower F4 alternate-bank access below the Spread experience gate and the conditional Beating scarcity pilot.** Basic finite drawing is complete. Another bank layout can matter when a player already wants portable water and real nearby danger distinguishes approaches, but the present Spread has no general thirst, water-dependent cooking, irrigation-by-pouring, or increased weight from filling the same vessel. Those are not valid reasons to invest in a water detour. Do not add those systems merely to justify the layout.

The smallest consequential future content slice is **one ordinary Beating Saltbriar collection opportunity**: an exact already-rolled finite bush on an optional, readable excursion from a preserved route. Taking 1–2 sprigs, spending exposure turns, using already-carried protection/water, waiting for a different time, and continuing without the food have native consequences. This belongs to the existing X5 pilot, not a new survival rewrite. It remains conditional on the parent Spread acceptance gate or a separate explicit change of scope. This audit does not establish current Saltbriar frequency, actual generated useful geometry, regional visual completeness, or player awareness; those are first gates, not assumptions.

If work must remain in the Spread, finish a bounded ordinary exploration comparison and act on its first consequential finding. Do not manufacture a new water choice or redo already-shipped F1 loose salvage/F7 sight variants. F8 frequency is a legitimate later economy question, but ordinary travellers already roll one-in-eight eligible entries, cap at three successes, and appear at visible distance4–7. Zero F8 family winners in three old frozen source cohorts is not evidence that players meet zero traders.

## Corrected water contract

| Actual action/state | Consequence | Planning implication |
|---|---|---|
| Carried Waterskin Fill beside real pure finite pool | Transfers min(missing capacity, actual volume); ordinary capacity3; source depletes | Already shipped. Preserve exact source, purity, rollback and depletion. |
| Drink one waterskin charge | Relieves one Parched stack; does not Wet, extinguish, or heal | Useful for existing Beating exposure/preparation, not routine Spread thirst. |
| Carried LiquidFlask Fill/Pour | Conserved arbitrary liquid; no Drink action; pouring onto actors publishes contact and tile reactions | Water has real thermal/electrical utility. It is conditional on equipment and current situation. |
| CookingService.TryCook | Uses carried raw recipe and nearby valid heat source; no water requirement | Cannot describe water as cooking supply. |
| Pour water onto a crop | Creates real pool/coating; does not call CropPart.Water | Irrigation is not implemented through a flask. Conjure Rain already supplies the supported farming loop and is in the starter access kit. Do not add a redundant loop without a separate material-choice proposal. |
| Full versus empty same vessel | Same Handling/Physics weight | Do not claim filling increases burden. Carrying another vessel still has its ordinary fixed weight. |
| Standing on water coating with Parched | Clears all existing stacks, independently of finite pool volume | Wet river terrain is already relief; an empty basin beside it does not create physiological water scarcity. Preserve this rule. |
| Fill distance | Same or any adjacent footprint cell, including diagonal; no LOS/passable-source-cell requirement | Any route calculation must target the entire legal filling frontier. Do not invent a cardinal-only bottleneck behind a wall. |
| Full/empty SpreadDrawPoint | Distinct existing model/readout, exact saved volume; empty owner remains | No new basin/empty art required. Wet background is not extra drink stock. |
| Inventory versus world action | Fill/Drink are vessel inventory actions; source has examine/readout | Context hints may point to an actual carried vessel. Do not invent a source-world Fill button or claim free query executes an action. |

Important anchors: `WaterVesselService.cs:10–115`; `LiquidVesselService.cs:80–213,228–251`; `WaterskinPart.cs:14–28`; `CookingService.cs:14–132`; `HandlingService.cs:48–60`; `ParchedEffect.cs:71–110`; `CropPart.cs:80`; sole production caller `Hydromancy_ConjureRain.cs:90`; starter kit `Presentation/Bootstrap/GameBootstrap.cs:1124`. All paths are repository-relative; source-hashes.json stores absolute paths.

Generation already uses one exact compatible finite water owner or creates one capacity3 draw point on a admitted RiverMeadow bank. A spent compatible source refuses, rather than regenerating supply. `Builders/SpreadExplorationBuilder.cs:240` preserves entry connectivity and one dry approach, but does not prove two approaches with different useful costs. The finite source's normal public part fields and existing cached/full-save tests preserve volume. Current art is source- and receiving-region-scoped; do not assume other-biome models are covered because a Spread asset exists.

## Bounded F4 alternative, only if observations make it worthwhile

This is a fallback design, not the recommended next implementation.

1. Preserve the existing water owner/three-unit allowance and its position where possible. Add no vessel, animal, hostile, loot, refill, or arbitrary safe-water promise. Do not add a second ecology timer resembling grazing.
2. Before terrain work, freeze all selected F4 addresses in seeds1/64/1729 and census actual relevant vessel access, existing hostile locations/LOS, all legal Fill cells, ordinary eight-direction paid routes and unaffected border/required-route connectivity. Count source availability separately from useful risky geometry. Observe actual desired water use; test-script knowledge is not discovery.
3. Admit a variant only when a short route really crosses a current threat's detection/attack exposure while a longer route reaches the same filling frontier with materially fewer exposed steps, and both are optional. A Tree may break sight; its shadow does not reset glare. Enemy movement remains live, so geometry is an opportunity, not a safety guarantee. No added danger just to rescue F4.
4. Reuse exact existing opaque terrain first. If sources/geometry are scarce, report the fixed denominator and defer. Do not search seeds until a photogenic scene appears or add scenery without a functional difference.
5. Test exact source/whole-packet ownership, callback mutation/rollback, changed or exhausted pool, unlike overlap, diagonal shortcut, blocked/foreign owner, no vessel/full vessel, actual Fill debit, cache/return/full save. Native paid-input observation must show the route and action. Human preference/awareness remains separate.

A small contextual hint is worthwhile only after observed confusion and only for a current visible source plus usable carried vessel. This would be lower-cost than new bank terrain; it must retain authoritative inventory action/refusal behavior.

## Proposed next content milestone: X5 finite Beating forage

### Smallest slice and player consequence

One fresh-world ordinary surface T1 Beating pilot family, using **one already-generated Saltbriar**. Preserve other bushes and all population/stock. Keep optional cache composition out of the first slice: a second reward type would make source and reward accounting harder without proving the first interaction.

The native source yields1–2 SaltbriarSprig, each with Food healing1d2, static weight1 and Commerce value3 (`Objects.json:29739–29805`). At Height, ten consecutive exposed player turns in the surface Beating apply one Parched stack, up to three; each reduces Strength and Agility by1 (`BeatingGlareSystem.cs:25–85`, `ParchedEffect.cs:22–64`). A real head-slot item or an actual interior cell resets exposure; night/dawn/outside biome also reset. **Rocks, dunes, line-of-sight cover and blue water imagery are not shade.** Any equipped head item is sufficient; that preparation is an intended counter, not a reason to strengthen the hazard. NPCs do not accrue this glare exposure.

The opportunity is not guaranteed danger or forced drinking. A prepared player can gather comfortably. An unprepared player may accept a measurable optional excursion at Height, use their real supplies, come back later, or leave finite food behind. Ordinary wildlife remains independent and unchanged. Harvest removes the source through the actual finite transaction; no invented stubble/regrowth or compensating reward.

### Before code: three gates

1. **Parent acceptance:** complete/record the remaining Spread experience comparison, or root explicitly chooses to start a regional pilot while that remains partial. The user has authorized meaningful terrain/model changes, but that does not erase the established acceptance gate.
2. **Source and geometry:** freeze the complete eligible/selected surface T1 Beating addresses for seeds1/64/1729 before generating. Capture exact ordinary Saltbriar receipts, all existing approaches/exits, current formations and original source positions. Prefer in-place useful sources. A bounded optional relocation must preserve exact owner, yield and every unrelated owner; no injected bushes. A shortest eight-direction route to *any legal Harvest cell* must make the optional trip cost real. Normal travel itself is exposed, so compare actual source excursion versus bypass to the same exit, not a stationary imaginary safe baseline. Freeze a minimum useful delta before code after the baseline tells us what is feasible. Report cap/refusal/source absence separately.
3. **Presentation/behavior:** inspect an actual source, legal approach, normal Harvest, real sprig pickup/use, empty aftermath and return. Establish one positive Height/barehead case and dawn/head-cover controls through ordinary player actions. Do not force health loss, strip the player's hat, set midday in the live acceptance run, inject water/food, freeze NPCs, or choose only a winning scene. Controlled tests may separately establish contracts and must be labeled.

### Implementation boundaries, if later approved

- An explicit receiving-biome/normal-generation contract and versioned saved assignment; do not widen every Spread-only guard or backfill visited/old worlds.
- Original source and source RNG remain authoritative. One committed optional situation, unchanged original per-zone resource value, no new hostile/gear/death reward. Exclude authored POIs, lairs, stairs, protected/rare/quest owners and mandatory exit cells.
- Use actual physical routes/LOS only. Do not mark decorative terrain `IsInterior` to manufacture shade. Any new real shelter architecture would need a separate source/topology budget and is outside the smallest slice.
- Existing finite Harvest transaction, capacity overflow, food effect and save stream own the outcome. No new cooldown/regrowth/global thirst/liquid-weight/animal-water system.
- Failing-first source/geometry/action tests plus a separate bug-class fixture: absent/spent/replaced/wrong-factory/wrong-zone receipt, callback change, rollback nonclobber, diagonal shortcut, no second claim, all exits/bypasses, native counter-equipment/time, exact yield/overflow, return/load without source recreation. Performance compared in actual normal entry/turn conditions.
- Release only after fixed-corpus feasibility, native focused/neighbour regression, actual receiving-camera interaction/aftermath and full required regression. If source or material-choice floor fails, retain a note and return to the next observed high-value gap rather than build a broad regional framework.

### Art and communication

`BeatingVoxelLibrary` has four variants of twelve static families, including exact Saltbriar→briar. A SaltbriarSprig portable model and SunStriker/Scorpion visitor models exist in Spread libraries, but that is not proof of Beating actor/corpse/gear coverage. Audit the actual receiving camera first. Reuse the readable briar and real dropped/held sprig when compatible; author only missing regional mappings/silhouettes/poses exposed by that audit. A harvested bush disappears under current rules; do not display a surviving ripe prop. No new NPC or dramatic set-piece is required.

Use factual local Harvest/readout cues and the existing visible clock/effect state. Avoid route arrows, map-wide objective markers, quest framing or labels claiming universal safety. Record unknown awareness honestly.

## Fresh evidence and limits

Private current-code probes: **25/25 passed, zero failed/skipped,0.494691s**. This includes8 new planning probes and17 existing liquid/draw-point controls. The new probes confirm fixed fill weight with extra-vessel counter, blocked-source adjacency versus distance2, wet-coating relief versus dry, Drink without Wet, water Pour without crop irrigation versus direct Crop.Water positive, and inventory Fill versus source-world absence.

Separate existing Parched/glare controls: **17/17 passed, zero failed/skipped,0.084786s**. These include exposed Height, dawn, real interior, head cover, reset, biome and water relief behavior. The assembly discovered42 cases after adding the fixture; only17 were selected in this second run. Do not report42 executed.

Files: `current-contract-results.xml`, `current-contract-run.log`, `glare-control-results.xml`, `glare-control-run.log`, `FiniteWaterCurrentContractProbes.cs`, runner/build logs, `source-hashes.json`. The first private compile had a fixture-only GameEvent.Release signature mistake; it was corrected before either execution. This is not a product RED or proposed-feature validation.

These are small .NET runner contract checks against current shared source, with its existing runtime/hash patches. No Unity/native input, generated F4/Beating census, rendered-model inspection, real-time exposure walk, save roundtrip, full regression, or player-awareness test was performed for this audit. Existing documented native/save evidence is retained context, not a new result. No build artifacts or tests are proposed for adoption.
