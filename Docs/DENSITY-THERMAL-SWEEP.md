# C8 thermal follow-up — verification sweep and gated plan

**Status:** native baseline and steam scheduling repair verified; material-unit reconciliation, thermal contact and scalding remain open. The historical sweep and failed attempts below explain the bounded repair. No global fire-scale change has been published, and C8 is not complete.

## Corrections verified from current source

| Premise | Verified current contract | Consequence |
|---|---|---|
| Every MaterialPart field uses0–100, per its comment | Thermal volatility uses FlameTemperature−Volatility×100 and initial intensity1+Volatility; wet porosity uses1+Porosity and1−Porosity×0.5; brittleness checks0.5/0.9 | The blanket comment is false. Do not multiply every field merely to satisfy it. |
| The original≈85 fractional combustion count remains current | Explicit authored Material fields now include64 positive combustion values≤1,4 values>1 and63 zeros | Resolve inheritance and publish exact surgical changes; the original estimate is not the current denominator. |
| TarSeep's volatility30 is stronger oil | Thermal threshold becomes200−3000=−2800; its initial20 is already above this, so ordinary positive heating never takes the required below→above crossing | Test the actual TarSeep against a fractionally authored control before a data correction. |
| Wet objects always lose water | PeatBog.Porosity80 and SteamVent100 produce negative evaporation factors | A wet prop can gain moisture at EndTurn; this is a neighboring scale defect, not just fire spread. |
| Reaction-created SteamCloud carries SteamEffect | water_plus_fire spawns SteamCloud, whose blueprint has Thermal120 and Lifespan3 but no SteamEffect; the reaction applies SteamEffect to the original burning/wet entity | Distinguish visible short-lived cloud from the original actor's steam status when testing. |
| Non-burning steam props receive their normal effect cycle | TickMaterialEntities admits burning props to BeginTakeAction+EndTurn; passive props only get EndTurn, and SteamEffect alone is absent from passive inclusion | Cooling/wetting in SteamEffect.OnTurnStart cannot run for a passive prop; an ambient steam-only prop may not decay either. |
| Vaporized automatically creates steam | Thermal emits Vaporized, but the source search finds no gameplay listener | SteamEffect's documentation overstates this source. Do not design scalding around an unverified vapor event route. |

The explicit field audit is preserved in `Verification/DensityCompletion/Environment/thermal-material-authored-audit.json` with the Objects hash and every authored value. Counts are explicit fields, not inherited resolved entity counts:

| Field | Explicit | Zero | Positive≤1 | >1 |
|---|---:|---:|---:|---:|
|Combustibility |131 |63 |64 |4 |
|Conductivity |46 |10 |25 |11 |
|Porosity |5 |0 |3 |2 |
|Volatility |4 |0 |3 |1 |
|Brittleness |45 |1 |42 |2 |

## Bounded proposed sequence

1. **Baseline first.** Root runs ordinary-stat ignition, water extinguish, oil versus inert ground, and escape controls with current production; retain raw actions, state, native screenshots and bounds. No global balance change is inferred from a unit test.
2. **Repair existing steam scheduling before adding scalding.** Use actual water_plus_fire output, extinguish the original source, then advance the real non-creature material tick. Pair steam-only/thermal-displaced, burning/non-burning, creature/non-creature, and expired/live controls. The smallest repair must tick each existing non-creature effect exactly once without giving creatures a second turn, processing a removed entity or expanding an unbounded scan. Whether the spawned visible cloud should itself own SteamEffect is a separate authored behavior decision; the current implementation attaches it to the original source.
3. **Adopt explicit per-field units before migration.** The narrowest compatibility-preserving choice is Combustibility0–100, with the existing fractional Volatility/Porosity/Brittleness formulas documented accurately and their isolated percentage outliers corrected. Conductivity has its own existing percentage consumers and fractional authored data; account for it explicitly rather than implying the electrical mismatch is solved by a fire change. A single all-percentage migration is possible, but it needs broader consumer and save counter-checks and should not be hidden inside this slice.
4. **Normalize fire stock only after the native gate.** For reviewed authored fractional combustion, convert once in Objects, not heuristically at runtime. Positive values below0.5 remain below the tile50 threshold after conversion; e.g.Tree0.45→45 still need not carry tile fire. Preserve zero/inert and authored percentage controls. Charred's multiplicative reduction is scale-neutral; AcidicEffect's absolute0.05×Corrosion decrement is not and requires a paired semantics decision/test. Existing saves retain their stored part values unless root deliberately approves a separate migration.
5. **Connect thermal models as a later bounded milestone.** Tile Heat and ThermalPart temperature still need a reviewed transfer policy to avoid double damage, repeated re-ignition and recursive propagation. Hot/cool steam scalding follows only after steam's actual lifetime/source/tick is proved. Do not add damage to today's purely cooling/wetting status before that design.

Relevant files: `Gameplay/Materials/MaterialPart.cs`, `ThermalPart.cs`, `MaterialSimSystem.cs`, `MaterialReactionResolver.cs`; `Gameplay/Effects/Concrete/SteamEffect.cs`, `WetEffect.cs`, `AcidicEffect.cs`; `Gameplay/World/Map/TilePropagationSystem.cs`; actual Objects and MaterialReactions/water_plus_fire.json. Existing fractional volatility/porosity tests in MaterialPrimitivesPhaseATests are deliberate consumer contracts, not evidence that the broad comment is authoritative.

## Honesty and next evidence

Private diagnostics can prove exact source creation, event delivery, crossing arithmetic and field evolution. They cannot prove visual readability, frame timing, survival difficulty, player escape or whether a normalized forest fire is enjoyable. Raw observations and controls will be appended before any repair proposal is considered ready. No new production feature, blueprint patch, Unity run or commit is included in this sweep.

## Private steam scheduling milestone

Actual water_plus_fire integration produced **3RED/5control-pass** in the first8 cases: ambient and thermally displaced extinguished sources did not cool/wet their neighboring creature, and ambient steam-only status never decayed. The visual SteamCloud remained a separate3-turn-lifespan object with no SteamEffect, as the source sweep found. A direct owner BeginTakeAction proves the existing SteamEffect behavior itself works.

The private candidate admits non-creature SteamEffect holders to the existing active BeginTakeAction/EndTurn path. It changes no numerical heat, density, burn, moisture or lifespan parameter, adds no scald damage, does not attach steam to the visual cloud, and does not touch any material/content scale. Snapshot selection remains one existing cell scan; an entity with both Burning and Steam is admitted once, and creatures remain excluded.

Two review stages hardened the candidate before acceptance. First, a matched hot RawMeat control proved that merely moving steam holders out of the passive list would suppress cooking. The candidate preserves their existing post-EndTurn material reactions while leaving burning reactions to BurningEffect's existing start callback. Second, a24-case dedicated adversarial fixture reproduced3 stale-lifetime errors: a removed active snapshot member still got a turn, a removed passive member still got EndTurn, and self-removal during start still got EndTurn. Zone membership is now checked at those dispatch boundaries, and reactions are skipped after removal.

Final focused result is **34/34 GREEN** (10 integration/control and24 dedicated adversarial). Exact nearby comparison includes225 existing material, burning, tile propagation, hazard, source, save and torch cases: **before259total/244passed/15failed; after259/259passed;0newly failing,15newly passing**, with identical case names. Raw receipts and the old source are preserved under `Verification/DensityCompletion/Environment/steam-*` and `MaterialSimSystem.cs.before-steam.txt`.

Two rejected fixture drafts are retained honestly: a100-degree non-creature neighbor dried its newly applied0.08moisture in its own passive EndTurn, so the final cooling/wetting target is explicitly a neighboring creature whose own scheduler is not advanced; the direct-event control retains the exact2-degree/0.08 expectations. An early synthetic burning control lacked combustible material and was refused before dispatch; it now asserts BurningEffect actually exists before counting turns. Neither correction changed production behavior.

The private change list is only `MaterialSimSystem.cs` and two new fixtures/metas. Reaction registry/factory state is snapshotted and restored by the new integration fixture, rather than leaving global initialization altered. There is no new persistent field; save format is unchanged, and existing material round-trip checks are included. The reviewed candidate is now published after the corrected native baseline and actual native RED; native GREEN and the dedicated scheduling Play probe subsequently passed as recorded below. This is a correction to an existing CoO status tick, not a claim of Qud parity or completed thermal-model unification.


Independent read-only review by `render_completion` found no blocking defect in the bounded candidate and confirmed canonical-anchor scanning does not duplicate footprint owners. Existing exception/event-pool behavior is unchanged and outside this diff. Root is preparing the native baseline; the private action/launcher plan is `/tmp/coo-thermal-native-baseline-plan.md`. It uses actual starter Flaming Hands and native item/targeting/movement controls with ordinary40HP and explicitly staged sources. It rejects several misleading shortcuts: Conjure Rain only waters crops, Waterskin has no Pour action, lit Torch is not destructive BurningEffect, and FireTonic bypasses the thermal crossing that TarSeep needs to exercise.


## First native baseline: partial, with hostile interference

Run `5eb272eabc644b23a9507d90a50c73c6` records seven of eight scenario
routes, then aborts before another wait at ordinary HP8. It is not a completed
thermal baseline. A naturally generated Marlback enters the lane during multiple
longer controls, so their health losses are confounded; no fire-balance conclusion
or scale migration follows from them. Initial pure oil contact and inert-floor
controls and actual wet/dry plant observations are retained in the full report.
The last source remained burning after the water action and requires endpoint
and timing review, not an assumed extinguish claim.

The launcher reported one precondition failure and its corresponding logged
error (two aggregate failures), restored all sampled prior scene/save/input
settings, and cleaned its isolated save root. A repaired bounded location search
must choose greater distance from live creatures without removing them, changing
AI/stats, granting health or suppressing ordinary turns. The first screenshots,
all observations/native keys and actual editor log byte range remain preserved.


### Native baseline harness correction after the first run

The first isolated native run (`5eb272eabc644b23a9507d90a50c73c6`) recorded seven routes and stopped at the ordinary HP8 guard during the eighth route. Its original report/log/screenshots remain preserved. A Marlback Scrabbler that started outside the five-cell observation radius approached during long waits, and the steam route put its source one cell away while ConjureWater's actual endpoint is two cells away. These are harness confounds, so the run does not justify global fire-scale changes.

The corrected harness searches at most24 generated Spread zones, keeps all entities and stats, chooses the clear lane with greatest distance to every current hostile body, and requires at least32 cells (ties prefer fewer hostiles). It records every whole-zone hostile ID/distance. After igniting the steam source, a normal native movement key retreats one cell and asserts the burning source is exactly two cells ahead before casting water. The shared launcher retains root's separately verified restoration retry; this publication changes only the runtime harness plus this evidence. Offline native-reference compile has zero errors; the corrected replay result is recorded below. Receipt: `Docs/Verification/DensityCompletion/Environment/thermal-harness-repair.json`.


## Corrected native baseline: source-aware findings

Run `da16bf85c81741e2adccccdb9a544eaa` completed **8/8 routes, zero failures and zero runtime errors in 56.906 seconds**, with 19 captures. It used `Overworld.7.2.0`; the only recorded hostile was a distant Viper (at least 71 cells from the player in this run). Root retained the actual editor log range and verified exact launcher/settings restoration. The raw report is `Verification/DensityCompletion/Environment/NativeThermalBaseline/da16bf85c81741e2adccccdb9a544eaa/report.json`; the source-hashed extraction and analysis is `Verification/DensityCompletion/Environment/thermal-native-baseline-analysis.json`.

| Route | What the actual current owner shows | What it does not establish |
| --- | --- | --- |
| Oil contact, dry/wet actor | Both ordinary actors fall from 40 to 28 HP on entry and remain 28 after escape. Wet actor moisture is 1.0 before fire and 0.99 at entry. OilSlick remains present but has no BurningEffect. Inert-floor counterpart remains 40 HP. | The result is the existing source/contact route, not proof that all wetness is ineffective against fire or that twelve damage is balanced. |
| Dry Bush | Immediately after casting, HP is 0, `targetPresent=false`, and the cell contains only its ground. | The later unchanged 388.14 temperature and 1.5 burn values come from the retained removed object reference. They are **not a live fire or a failed world tick**. |
| Wet Bush | Actual source moisture is 0.48 before fire. The owner stays present at HP 5, reaches 310 below its 320 ignition threshold, has no burn, and cools to 257.13 over four waits. | A single paired prop/control does not validate a general material-scale migration or a forest-fire balance. |
| TarSeep | The live owner warms from 20 to 137.70 while effective flame remains −2800 and no burn appears. | This confirms the previously sourced volatility-unit pathology; it does not choose the corrected ignition/balance value. |
| OilSeep | The live source ignites at 4.3, reaches 5, remains present while temperature rises from 443.95 to 1306.10, and loses BurningEffect by the third wait. Player HP reaches 33. | Unlike the removed Bush, these are live source readings. They still do not prove prolonged safety, ideal heat scaling or the exact provenance of every damage point. |
| Exact water/steam source | The native retreat aligns the real source with Conjure Water. Burning falls from 5 to 3 after water, returns to 5 on wait 1, and is absent by wait 2. Source SteamEffect density declines from 0.9 to 0.1 over eight waits. Two new local clouds begin at lifetime 2/3 and both disappear by wait 3. The source is still hot at 628.50 on wait 8. | Water does not immediately extinguish this source. The final SteamEffect is still present; complete dissipation is not observed. Visible cloud lifespan is separate from source status. |

**Steam correction remains justified but is not diagnosed by disappearance alone.** This native source is thermally displaced throughout, so the existing passive EndTurn path already decays SteamEffect. The omitted BeginTakeAction cooling/wetting, and the lifetime of an ambient steam-only prop, remain the precise failures reproduced by the private tests. The two-cell player retreat deliberately keeps the player outside the one-cell steam aura; this scene therefore does not measure that neighbor effect. The candidate changes only admission/event scheduling, retains current magnitudes, and must be native-tested against those specific controls after publication approval.

**Next bounded work.** First review/publish the already tested 34-case steam scheduling repair and add a native non-burning steam owner with a live adjacent thermal/moisture probe plus an ambient lifetime control. Keep source/status/cloud identities distinct and skip removed owners. Separately, prepare failing content/consumer tests for the documented percentage outliers (TarSeep volatility, PeatBog/SteamVent porosity, and brittleness where actually consumed), choose explicit per-field units, then make a surgical data proposal. The broad combustion migration remains a separate balance milestone: it changes spread/fuel exposure across many objects, must account for AcidicEffect's absolute decrement and existing saves, and is not authorized merely by these eight completed routes.

The native baseline can verify the reported scripted actions and current-owner state. The sources, tonic supplies and grimoire were explicitly staged; acquisition, natural encounter frequency, whole-world balance, enjoyment and long-session safety remain unverified. Screenshot existence alone does not establish visual correctness. The original aborted run remains preserved as a distinct, confounded attempt.

## Steam publication checkpoint

The corrected native baseline is preserved above. Actual Unity RED ran all 34 steam cases: **15 intended failures and 19 passing controls**, matching the private corpus. The reviewed runtime change is now published in `Assets/Scripts/Gameplay/Materials/MaterialSimSystem.cs`; the two fixtures were published first and failed against the old runtime. `Verification/DensityCompletion/Environment/steam-publication.json` records exact before/after runtime hashes and the prior 259-case matched differential. Native GREEN is recorded below.

This milestone changes only existing effect scheduling and removes stale owners from a tick snapshot. No heat, burn, moisture, damage, material scale, visual-cloud attachment or serialization format is changed. The separate four-case native scheduling probe uses explicitly staged hot/ambient and retained/removed-steam controls; it is diagnostic evidence, not natural acquisition or broad fire-balance acceptance.

The diagnostic Play harness is published as `DensitySteamNativePlayer` and `DensitySteamNativeBatch` (four files including metas). It creates source steam through actual native fire/water actions, then explicitly controls source temperature/status and places an adjacent real-item thermometer. Read-only review caught a potential vacuous hot/ambient label: probe preconditions now require measured hot-above-ambient or exact ambient, the requested retained/removed status, and a dry, unheated neighbor cell. Offline current native-reference compilation has zero errors. The subsequent native execution and bounded visual review are recorded below. Manifest: `Verification/DensityCompletion/Environment/steam-native-probe-readiness.json`.

## Native steam scheduling acceptance and Q1–Q4 review

Run `c60045d3558347aca9e198a37748277a` completed **4/4 cases, 22/22 checks, zero failures or runtime errors in 44.985 seconds**, with 13 captures and exact launcher/settings restoration. The full actual editor log byte range is archived with the report under `Verification/DensityCompletion/Environment/NativeSteamScheduling/c60045d3558347aca9e198a37748277a/`. Native EditMode also passed all **34 steam cases and 24 save controls** in `Integration/native-steam-sixth-first.xml.gz`; the combined 242-case run had six unrelated art failures, so this is not an all-green claim for that larger run.

The actual hot source began the controlled measurement at 706.2705°C; its paired ambient source began at exactly 25°C. Each retained reaction-created SteamEffect began at density0.7 and became0.6 after one native wait. The adjacent current Dagger thermometer fell from40 to39 and gained0.06 moisture in both steam cases. Both matched no-steam controls stayed40 with no WetEffect. Source SteamEffect was absent by the eleventh wait in both retained cases. Current source/probe membership was checked, and the ordinary actor stayed40/40 throughout the four pairs. No new effect magnitude, damage, material scale or cloud attachment was introduced.

- **Q1 — Are these current objects rather than stale references?** The measurement explicitly requires both source and probe to remain current zone owners after the first native wait and records `targetPresent`/`probePresent` throughout. This is distinct from the removed Bush in the earlier baseline, whose retained values were not counted as a live fire.
- **Q2 — Could the positive effect come from unrelated heat or water?** The probe cell must be dry and unheated; the real-item probe has explicitly zero ambient decay. Exact hot/ambient and retained/removed-status preconditions prevent vacuous classification. The matched no-steam pairs stay unchanged while both steam pairs cool/wet. The temporary parameter assignments are openly diagnostic staging.
- **Q3 — What existing paths and failure boundaries were checked?** Private exact-corpus259/259 GREEN retained existing material/save/torch controls; the native34 includes burning/non-burning admission, non-creature versus creature dispatch, cooking reactions, removal during a tick, expiration and footprint ownership. Twenty-four native save controls pass. The fix adds no serialized field and does not grant creatures a second turn.
- **Q4 — What remains unproved?** This finite staged route proves the measured scheduling behavior, including ambient-only lifetime. It does not establish natural acquisition, broad fire balance, scalding, persistent clouds, long-session safety or enjoyment. Root inspected `hot-steam-controls` and `ambient-steam-one-wait`: ordinary40/40 HUD and source/reaction log were readable. Those pictures do not establish the thermal numbers; the other11 captures have not been inspected. There was no same-harness pre-fix Play run, so the before/after causal evidence is the exact private differential and actual native unit RED→GREEN, paired with the final native control scene.

Commit-ready steam manifest: `Verification/DensityCompletion/Environment/steam-publication.json`. Runtime scope is exactly one file (`MaterialSimSystem.cs`), two fixtures with metas, four diagnostic harness files with metas counted together, this living doc and bounded verification receipts. Door, liquid and percentage-scale work remain separate milestones.

### Remaining unit reconciliation: read-only sweep

`Environment/thermal-unit-inventory.json` records the exact current blueprint hash
and every direct Material Combustibility/Volatility parameter. Combustibility has
63 zero,64 positive fractional and4 percentage values. Volatility has3 fractional
values and TarSeep's30. This confirms mixed authoring; it is not a completed
migration. The successful native baseline above satisfies the observation gate,
but does not validate a proposed new balance.

The next unit step must handle consumers together: TilePropagationSystem compares
Combustibility with50, ThermalPart currently multiplies raw Volatility by100 and
adds the raw value to ignition intensity, CharredEffect scales proportionally,
and AcidicEffect subtracts `.05 * Corrosion` in absolute units. A blueprint-only
rewrite would change these relationships incorrectly. Preserve the existing
percentage contract of LiquidDefinition and the independent conductivity work.
Require a failing actual-factory TarSeep ignition control, paired wood/flesh/oil/
water tile propagation, proportional char/acid controls and old-save disposition
before choosing the final conversion. Then repeat the same ordinary-stat native
fire/water cases and compare source identity, damage, escape and fuel lifetime.
Thermal contact and hot-steam scalding remain separate behavior steps; the shipped
steam scheduler does not silently implement either.


## C8.2 private outlier correction, before publication

The next bounded slice corrects five fresh-blueprint material values to their existing fractional consumers: TarSeep Volatility30→.3, PeatBog Porosity80→.8, SteamVent Porosity100→1, FrostVent Brittleness70→.7 and IceSheet Brittleness90→.9. It changes neither combustion/conductivity values nor any runtime formula. Existing serialized material values remain unchanged on load.

A numeric-only candidate exposed a second actual Tar bug: at180°C and corrected.3 volatility, its `Liquid,Tar,Fuel` tags still fail ObjectStatusMatrix's explicit flammability gate. The proposal therefore adds `Flammable` to that one authored material; it does not widen the global Fuel category. The first heat fixture also needed Tar's actual2.5 heat capacity, which is recorded as a fixture correction, then rerun against the unchanged baseline.

The final private28 cases reproduce12 intended failures with16 passing controls, then pass28/28 after the six exact tokens in five blueprints. Controls include ordinary fractional materials, no heat, removing the actual flammability tag, hot/ambient drying, partial/catastrophic shattering and five pairs of current/legacy graph serialization. Exact parsed-diff and raw receipts are under `Environment/MaterialOutliers`; the nearby137-case differential, independent review, actual Unity RED→GREEN and same unchanged thermal Play replay remain gates. No shared production data or MaterialPart source changed in this step. Broad combustion normalization, contact heat and steam scalding remain separate open work.

### C8.2 actual Unity RED and surgical publication

The combined native494 selection records all12 intended material failures and16 passing controls in `Integration/native-seventh-liquid-bed-door-material-red.xml.gz`. The independently reviewed six-token patch was then applied to the current501-blueprint file; parsed comparison proves496 other blueprints and every other field unchanged (`Environment/MaterialOutliers/shared-publication/parsed-diff.json`). MaterialPart changed only its misleading unit comments. Actual Unity GREEN and the unchanged native thermal replay remain pending; no global combustion/contact/scald claim is made.

### Native follow-up selection: 193/193 GREEN

Actual Unity job `4ccacd6728a04704aab763829b3c2f23` completed all 193 cases with zero failures or skips: 137 material/outlier/neighbor checks, 26 poured-liquid rendering checks, 4 seventh-grass geometry checks and 26 combat-witness checks. Raw XML is preserved at `Verification/DensityCompletion/Integration/native-seventh-liquid-material-combat-green.xml.gz`. This closes those focused unit gates; the separate Play routes and visual comparison are still required.

## C8.2 current native replay after unit corrections

The unchanged native route completed all8 paired cases with zero failures or
unexpected errors in46.03seconds: run `3e20737c56474ec1a9e4c315944bc8dd`.
The full report,19 captures, native key observations, full log range and exact
editor restoration are retained under `Environment/NativeThermalBaseline`.
This uses explicitly staged sources/supplies and actual ordinary40HP player
inputs; it is not natural acquisition or whole-world balance evidence.

TarSeep now reports volatility0.3 and an effective170-degree ignition threshold.
One actual Flaming Hands input raised it to137.70; it stayed below threshold,
cooled to131.07 after three ordinary waits, remained present and did not gain
Burning. The player remained40/40HP. Thus the dimensional outlier is corrected,
but this run does **not** prove tar ignition or close the broader thermal/contact
integration work. Water/fire still produced the observed steam reaction.
