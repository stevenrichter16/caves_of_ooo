# MACHINE V1 DOSSIER: the fractionating still (V1 = "settling still")

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9` (spurgrass docs on top of `9316f9330`). No project file, Unity run, test, build or git write was made by this dossier. Root `Lore/` is the only lore authority; `Docs/Lore/` was not used. Designed against `design/ARCHITECTURE-SPEC.md` (the spec), `Docs/MACHINE-ROSTER-VISION.md:30-33` (the user's pitch), `Docs/MACHINE-SYSTEMS-DESIGN.md:60-65,86,101-111` and `lore.md` M2 / section 6 / L-register.

Evidence tags: **[V]** read in this session (path:line given). **[R]** taken from a prior research report, not re-read. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle step settles it.

---

## 0. Verdict (read this first)

1. **The machine has no demonstrated unmet purpose in any town today, and V1 must not be sold as a town-coverage machine.** Every one of the 20 settlement dossiers was written against this machine: 14 rate it poor, 6 plausible-weak or conditional, 0 strong (section 2.1). No product of a liquid still (clean water, lamp oil, ward oil, ink) lacks an ordinary supply that is cheaper and closer (section 2.2). The pitch's central idea (a tank, mixtures, a three-way choice of what survives, fuel, time, waste receivers) needs systems that do not exist.
2. **There is one real, local, verifiable triple** where feed, consumer and missing alternative coincide: the Sodden **dressing shelter 15.7**. It has bands of `MirePool` (bog-mire), a `SumpsieveCrop` planted dry, and *no fresh water in its own plan* (section 2.3). The need is small (about 2 water units per crop cycle) and Sumphold's endless well is one zone north. So even there the still is an optional convenience, never a gate.
3. **Smallest honest V1: a stateless "settling still" fixture.** The player carries a flask of `bog-mire` or `brine` (drawn at a pool with the shipped M1a Fill action), stands at the fixture, and spends **one action** to settle every draw that fits into a carried receiver (empty flask, flask of water with room, or waterskin). `bog-mire` 3 units -> 1 `water`; `brine` 4 units -> 1 `water`; the remainder of each draw is **declared lost** (no item, pool, coating or waste object). No tank, no job, no fuel, no fee, no timer, no saved field. Rows are data; the Part is stateless; the first placement is a fresh-only plan edit in 15.7 following the oil-strainer precedent.
4. **Classification: `wave1-existing-foundations`** for V1 as defined: its only hard dependency on the spec is S1 (saved-Part golden, test-only); S3/S5 are preferred but have one-line fallbacks (section 5). The **full pitch is wave3** (composition/provenance model, fuel and elapsed work, receivers and waste, authored finite feeds with real consumers per fraction, canon sign-off on any rare-reagent or solvent fraction).
5. **Two things the owner must hear:** (a) the Beating's water scarcity is a stated design point (`Docs/CHUNK-ACTIVITY.md:163-165`) and brine is abundant per *place* even though each pool cell is finite (section 1, row C1), so V1 must never be placed where brine cells are, and its brine row is a latent, carry-in-only path; (b) the "machine in every town" goal is better served by other machines, and the census should record the still as `not a town machine` (section 7.4).

---

## 1. Verified ground truth and corrections to premises

| # | Premise (source) | Verdict | Evidence |
|---|---|---|---|
| C1 | "Feed is finite: pools are 60/90 units" (`Docs/MACHINE-SYSTEMS-DESIGN.md:146`, `Docs/MACHINE-LIQUID-HANDLING.md:36`, `settlement-salt-vault.md:184`) | **True per cell, misleading per place.** `SoddenDistrictPlan.Pool` places a `MirePool` on every cell of a rectangle (stepped corners), each 60 units: the shelter has three bands (`Pool(5+variation,4,22,20)`, `Pool(60,7,74-variation,21)`, `Pool(47,3,64,6)`), hundreds of cells. A `BrineLens` is 2-4 discs radius 2-3, 70% filled with 90-unit `BrinePool` cells. Pools never regenerate (`LiquidPoolPart` has no growth; `TileStateSourcePart` renews only the ground coating). So place-level feed is effectively abundant; the real limiters are the per-flask 12 units, one Fill action per flask, walking, and the loss ratio. **Scarcity of feed cannot be V1's balance dial.** | [V] `SoddenDistrictPlan.cs:42,96-100`; `SoddenDistrictBuilder.cs:176`; `BeatingFormationBuilder.cs:270-292`; `LiquidPoolPart.cs:35-45`; `LiquidVesselService.cs:93-124` |
| C2 | Prior reports: liquid processes can ride the material-process engine | **False.** `MaterialProcessRegistry.Portable` requires the blueprint to carry the `Item` tag, `Takeable` physics, no `Solid`; rows are item blueprints; the plan resolves a carried item source plus `ForgeWorkReceipt.Resolve` or a cutting tool. Liquid is a `Volume`/`LiquidId` scalar pair on a vessel, not an item. | [V] `MaterialProcessRegistry.cs:110-137`; `MaterialProcessPlan.cs` `TryCreate` |
| C3 | Arch reports: a new station kind needs a new 3x3 scan or `StationQuery` | **Not for V1.** The still is the *menu target*: its verbs are rows on its own world menu, gated by the world-menu reach gate (`ActionRequiresReach` at `InputHandler.cs:3202-3210`), exactly like `OilStrainingPart` (rows on the bench, enumerating the carried inputs) and `AlchemyStillPart` (rows on the still). No other verb needs to find a settling still. `StationQuery` is needed only if an item process row later says "needs a settling still nearby" (two-consumer rule). | [V] `OilStrainingPart.cs:60-72`; `AlchemyStillPart.cs:33-48`; `InputHandler.cs:3198-3210` |
| C4 | Spec S7 generic post-builder can place it | **Only in the 16 shared-pipeline villages.** The shelter 15.7 is not a village; it is a `SoddenDistrictPlan` zone. Its fixtures are placed by the plan and validated by `SoddenDistrictBuilder` (flag `includeOilStrainer` via `OilStrainingPart.ContentAvailable(factory)`). V1's first placement therefore follows that precedent, not S7. | [V] `SoddenDistrictPlan.cs:24-34`; `SoddenDistrictBuilder.cs:48`; spec C3 builder text |
| C5 | A world-menu command needs two OR-chain edits | **One line for V1.** The world chain at `InputHandler.cs:3459-3470` already includes `WorldResourceActions.IsCommand(...)` (and `PreparationActions.IsCommand`), and `WorldResourceActions.IsCommand` itself ORs `LiquidTransferActions.IsCommand` (`WorldResourceActions.cs:8`). A new `SettleLiquid|` prefix added to that shared helper (or declared `ActionTime.OneAction` once S3 lands) is charged one action. The pack menu is not involved: the still is a world fixture. | [V] `InputHandler.cs:3459-3470`; `WorldResourceActions.cs:8` |
| C6 | Water in a flask and in a waterskin are different units | **Same unit.** `WaterTransferActions.Vessel/SetUnits` treats a `WaterskinPart` charge and a `LiquidVesselPart` `water` unit as 1:1 (one drink). So one settled `water` unit is one drink and one `CropWateringService` unit. | [V] `WaterTransferActions.cs:9-22` |
| C7 | A waterskin or flask can be the receiver | **Yes, with limits.** Default `WaterskinPart.Capacity = 3`; `LiquidFlask` capacity 12 (blueprint 44503, Capacity 12); `GreybladderCup` capacity 6. Skins are water-only; a `LiquidVessel` receiver must be empty or already `water`. M1a's own `Vessel()` excludes skins (`!owner.HasPart<WaterskinPart>()`), so V1 needs one extra receiver kind. | [V] `WaterskinPart.cs:9-10`; `LiquidTransferActions.cs:30-47`; Objects.json:44503,55603 |
| C8 | Drinking cures "thirst" generally | **Only in the Beating, and only after exposure.** `BeatingGlareSystem` applies `ParchedEffect` (stacks to 3; -1 Str/-1 Agi each) after 10 consecutive exposed turns per stack in the Height band; **any head item resets the counter** (`HasHeadCover`), as does an interior cell. A waterskin drink removes one stack (`ReduceOneStack`). Cisterns hold 3 draws and are deliberately finite: "Endless water in about half the wild Beating would undo the exposure that is the region's point." | [V] `BeatingGlareSystem.cs:29,58,82`; `WaterVesselService.cs:99`; `ParchedEffect.cs`; `Docs/CHUNK-ACTIVITY.md:160-165` |
| C9 | Crops need water the Sodden does not have | **Verified for the shelter.** `SumpsieveCrop`: `TicksPerStage 24`, three stages, yield 2 `SumpsievePad` + 1 `SumpsieveSeed`; the builder plants one `ripe-sumpsieve` (stage 2) and one `dry-sumpsieve` (stage 0, `MoistureTicks 0`); seed text: "dry growth pauses". Hand-watering is `CropWateringService`, 40 moisture ticks per unit, and **requires `LiquidId == "water"`**. `Shelter()` places no `Well` or cistern. Alternatives: Conjure Rain (seed text), carrying water from Sumphold (zone 15.6, adjacent). The *ripe* crop already yields pads without any water. | [V] `SoddenDistrictPlan.cs:42-52`; `SoddenDistrictBuilder.cs:87`; `CropWateringService.cs:11,149-166`; Objects.json:50443,50452-50530 |
| C10 | The roster pitch implies a tank with mixtures | **Yes, and V1 deliberately has none.** Every liquid vessel/pool/contained source in the codebase is single-identity (`LiquidId`+`Volume`); mixed cells are refused (`LiquidSourceSafety`). The design doc's two options (authored composition per canonical liquid vs an opt-in feed carrying provenance) are resolved here as *authored composition per registered liquid id*. | [V] `LiquidVesselPart.cs:9-11`; `LiquidVesselService.cs:326-360`; `MACHINE-SYSTEMS-DESIGN.md:107` |
| C11 | M1d set the precedent for loss | **Yes.** The strainer declares an explicit unrecoverable recipe loss with no waste item, no coating and no cleanup job; an independent design review rejected zero-value carried dregs ("disposal clicks without a useful decision"). V1 copies that choice. | [V] `Docs/MACHINE-OIL-CLARIFICATION.md:15` |
| C12 | Spec claims the still kind is `PresenceOnly` | Irrelevant to V1 (no scan). Note for later: `AlchemyStillPart` still has no readiness gate and no `BlocksFunction` check. | [V] `AlchemyStillPart.cs:104-131`; spec C5 |
| C13 | "No in-world mention exists for any shipped machine" | Still true; V1 adds none (readers only). The Apothecary line "the still by the window works if you'd rather brew your own" is dialogue about the *reagent* still, not about settling. | [R] `presentation.md` finding 7; `lore.md` section 2 row 11 |

---

## 2. Purpose assessment (the honest part)

### 2.1 Town fit (from the 20 settlement dossiers [R]; every verdict line is in `settlement-<id>.md` under "Fractionating still")

- **Poor (14):** Sill (two stills already, repair stock is plain shop stock), Morrowfast, Gantry, Tine, Cinderhold, Marrowstye, Quillhold (poor, conditional), Tally, Wellmeet, First Tent, Drowned Ledger (distilling peat water used by the preserved is a Pale Curation crime, L5), Last Counter, Olderdeep, Spivenor. The reasons repeat: endless wells, zero `LiquidPoolPart` asserted by tests, no feed, ordinary supplies cheaper.
- **Plausible, weak or conditional (6):** Sumphold (best identity fit, "no verified unmet need", `settlement-sumphold.md:161`), Slip (the only locally grounded chain is pitchpod-to-lamp-oil, which is an *item* process, not liquid; `settlement-slip.md:164`), Posy (weak; alternatives real), Salt-Vault (weak; water not scarce; Pale-Salt is a protected material identity), Quiet's Door (conditional; sari venom to ward-fluid needs a mixture/waste contract and a consumer that does not exist; installed machines by outsiders are forbidden there), Lampwell (conditional; the Drosera ring and patch are protected, not feedstock).
- **None of the six is served by V1's feeds.** V1's recipe set (mire, brine to water) has a purpose at **zero of the 20 places** under the dossiers' own criteria. Counting the still toward "a machine in every town" would be dishonest.

### 2.2 Product fit (does any output lack an ordinary supply?)

| Product | Existing supply | Verdict |
|---|---|---|
| clean water | endless village wells; finite cisterns (3 draws); fresh pools; waterskin; head cover and interiors prevent Parched at all | no unmet need except the shelter crop (2.3) |
| LampOil | merchant stock (`TradeStockBuilder.cs:37` [R]), 3 loot entries, M1d strainer (2 FrogOil -> 1) | no unmet need; M1d recorded "a small fuel deficit only" (`MACHINE-OIL-CLARIFICATION.md:43`) |
| WardOil | `MerchantRepairStock` (`VillagePopulationBuilder.cs:790-798`) | plain stock; Sill's comic-fraud idiom must stay (lore M2 landmine) |
| InkVial | Scribe restocks; `BotanicalInkDeskPart` | no unmet need; ink minting touches rental currency [R] |
| salt | Beating pans; economy EC5 prices by distance | **forbidden output** (`Adversarial_NoWaysiteMintsSalt`, L9) |

### 2.3 The one real triple (Sodden dressing shelter, zone `Overworld.15.7.0`)

Feed local: `MirePool` bands (`bog-mire`, 60/cell) within a few cells of the crop plot. Consumer local: the `dry-sumpsieve` crop at (50,9) needs about 2 `water` units for 3 stages x 24 ticks at 40 ticks per unit [I from C9]. No fresh water in the zone. Chain beyond the machine: water -> crop growth -> 2 `SumpsievePad` + 1 `SumpsieveSeed` -> the dressing bench (1 pad + 1 knotflax cord + 2 drams, a paid service by the keeper) -> a field dressing that treats ordinary poison and bleeding [V `SoddenPreparationPart.cs:47-49`]. It is a closed, local, optional loop. Honest size: tiny. Honest alternative: carry water from Sumphold or cast Conjure Rain; doing nothing loses one extra pad.

### 2.4 What must exist before the machine has a larger purpose (the "first" list)

1. **An authored feed with a consumer per fraction.** Today the only liquid-vessel consumers are pour (ground film), douse, drink (water only), crop watering (water only). A second fraction (a "rare reagent", a "solvent") needs a real consumer, or it is flavour. Candidates the dossiers name (venom to ward-fluid, Drosera mucilage, pitch) are items or protected; none is a liquid with a consumer.
2. **A composition/provenance contract** if recipes are to "depend on what actually went into the tank" (design doc option 2, `MACHINE-SYSTEMS-DESIGN.md:107`).
3. **Fuel/work and elapsed time** (spec S15/S16, stored-work canon S18). Heat without time or fuel is fiction; the design doc lists "fuel/work" as a requirement for a justified installation (`:103-105`).
4. **Receivers and waste** (spec S17, which names this machine as its natural first consumer, C10 line 433-435).
5. **An owner decision on Beating water** and, if desired, a portable/installable form (spec S19) so the machine can stand where feed and need coexist.

---

## 3. V1 definition: the settling still

### 3.1 What it is

- A **fixed, stateless furniture fixture** (blueprint `SettlingStill`, non-solid like `AlchemyStill`/`TinkersForge`, layer 5, Furniture tag, one new Part `LiquidSettlingPart` with **no public fields**). Its verbs are rows on its own world menu (reach gate: within strike reach, plus `WorldResourceActions.Nearby` and a visible, explored cell as in `ContainedLiquidSourcePart.Context` [V `:35-47`]).
- It holds nothing between uses. All quantities live in the player's carried flasks and skin, which already persist through existing fields. **No new saved field, no new saved Part state, no relapse retention need** (spec C2 `Retention: None` would be valid if it were ever placed by S7).
- It is **not** the shipped `AlchemyStill` and does not touch brewing, reagent marks or batches. Attaching the Part to the `AlchemyStill` blueprint is deliberately **not** V1 (section 9, owner decision D4).

### 3.2 Player verbs (exact)

World menu on the fixture (look-mode `x`/Enter; rows only when the actor qualifies, otherwise nothing is shown, as for every other fixture):

| Row (draft text, voice gate pending) | When shown | Effect |
|---|---|---|
| `settle <feed>: <k> draws (<src before>-><after>) -> <k*out> water (<receiver label> <before>-><after>/<cap>); <lost> <feed> lost to the dregs · 1 action` | carried feed vessel with a row, a legal receiver, and k >= 1 draws fit | commits k draws |
| `settle one draw of <feed> (<in> -> <out> water ...) · 1 action` | same, and k > 1 | commits exactly 1 draw |

`k = min(floor(feedVolume / FeedUnits), floor(receiverFree / OutputUnits))`. Offer the all-that-fit row first, one-draw second (mirrors `LiquidTransferActions.AddActions` ordering and priorities 22/21 [V `:68-79`]). Each eligible (feed vessel, receiver) pair is one row, receivers numbered locally in the menu ("to #n") as in M1a [V `:76`]. No row is ever created that would commit zero draws.

With S3 landed, unavailable-but-explainable cases show **disabled rows** with a reason (codes in 3.7); without S3 they stay silent and the reader carries the explanation.

### 3.3 Inputs and outputs (units)

Units are **abstract pool units**, the same scalar as `LiquidPoolPart.Volume` and `LiquidVesselPart.Volume`; they are not drams, mass, or a chemistry claim. The rows are authored game conversions for *canonical registered liquids only*.

| Row id | Feed (`FeedLiquid`) | Per draw: feed in | Per draw: water out | Per draw: lost | Flask of 12 gives | Provisional (tune in actual play) |
|---|---|---|---|---|---|---|
| `settle-bog-mire` | `bog-mire` | 3 | 1 | 2 | 4 draws, 4 water, 8 lost | yes |
| `settle-brine` | `brine` | 4 | 1 | 3 | 3 draws, 3 water, 9 lost | yes (see 3.10) |

Receivers: (a) an empty `LiquidVesselPart` (`Volume 0`, `LiquidId ""`): becomes `water`; (b) a `LiquidVesselPart` already `water` with room; (c) a `WaterskinPart` with room (units = drinks; capacity 3 by default). Never a vessel holding any other liquid, never the source itself, never an equipped, stacked (`StackCount != 1`) or non-carried vessel (`WorldResourceActions.Carried`, `LiquidVesselService.Validate` semantics [V `:274-286`]).

Fit with real vessels: a 12-unit brine flask is exactly one full default waterskin (3 drinks) and one full Parched cure (`MaxStacks 3`). A 12-unit mire flask is 4 water, about 2 crops' worth. These happen to line up; they are a reason to keep the ratios, not a claim of balance.

### 3.4 Loss and waste destination

- The `FeedUnits - OutputUnits` per draw are **spent in the pan and unrecoverable**. No item, pool, coating, entity or counter is created; the zone's `TileState` and entity count are unchanged (RED-pinned). The row text and the reader both disclose the loss (as M1d's does).
- Leftover feed smaller than one draw stays in the source flask (12 mire -> 4 draws exact; 11 -> 3 draws, 2 left).
- The *world-side* effect of acquiring the feed is the existing M1a Fill: the pool cell's `Volume` falls (finite, never refills), nothing is coated, and the pool's independent coating source is untouched [V `LiquidVesselService.cs:93-124`].
- Retained physical waste (a receiver, a residue batch) is **deferred**, not forgotten (section 8). The M1d review is the evidence that zero-value dregs add clicks without a decision.

### 3.5 Cost, time, fuel

- **One ordinary action per successful commit** (all-that-fit or one draw alike). Declared `ActionTime.OneAction` when S3 exists; until then add the `SettleLiquid|` prefix to `WorldResourceActions.IsCommand` (one line, `WorldResourceActions.cs:8`). A refused or vetoed attempt spends nothing.
- **No fuel, no fee, no timer, no elapsed job, no heat, no operator.** "Work" is the actor's own hands at the fixture; the in-world explanation must not mention heat or distillation (L16). This is an explicit non-claim: the design doc's fuel/work requirement for a *justified installation* is not met, and V1 is not that installation (`MACHINE-SYSTEMS-DESIGN.md:101-105`).
- The real costs are the 67-75% feed loss, the flask supply (Sill ~22 drams, WellKeeper stock at Sill/Wellmeet, 3 loot entries; Sumphold and the shelter have no WellKeeper [R `settlement-sumphold.md:64`]), the Fill action per flask, and walking.

### 3.6 What the preview shows

Everything the player is about to commit is in the row text and reproduced in the readers, computed by the **same pure `Plan` function** that the commit re-evaluates (preview equals payment):

- feed liquid display name (`LiquidRegistry.Get(id).DisplayName`), draws k, source before->after, product `water`, receiver label and before->after/capacity, units lost, cost "1 action".
- Examine (`x`) and the full F1/document reader on the fixture list the recipe table ("bog mire 3 -> 1 water; brine 4 -> 1 water"), what receivers qualify, the loss statement, "no fuel, no fee, no timer", "settles only what is listed", and "holds nothing between uses". With S5 the lines come from `IDescribeState`; before S5 they are added by hand to `ExaminablePart.BuildDescription` and `ExamineDocumentBuilder.Build` (the same two hand-edited provider lists the strainer used [V spec section 1 row 6]).

Draft player strings (every one needs the R10 blind gate; none has passed it): Examine: "A shallow pan under a cloth hood, with a spout at one end. It settles what the bog and the brine give up and lets the clear part run off. Most of every draw stays behind." Document, Uses: "Carry a flask of bog mire or brine, and a flask or waterskin to take the water. One action settles every draw that fits. Bog mire gives one water for three; brine one for four. The rest is lost in the pan." Limits: "It settles only what is listed. It makes no salt, no oil and no ink. It needs no fuel and holds nothing between uses."

### 3.7 Blocked, failed, interrupted, saved states

| State | Detection | Player sees | Spent | Diag (`liquid`, default-on [V `Diag.cs:119-120`]) |
|---|---|---|---|---|
| nothing settleable carried | no vessel with a row's feed | no rows; reader says what to carry | n/a | none |
| feed short (< `FeedUnits`) | `Volume < FeedUnits` | disabled `short` (S3) or no row | nothing | `SettleRejected{reason:"short"}` only on a forced attempt |
| no legal receiver | none qualifies | disabled `no-receiver` | nothing | same pattern |
| receiver full | `free < OutputUnits` | disabled `receiver-full` | nothing | same |
| stale selection | encoded volumes/ids differ at commit | "The flasks changed; nothing is settled." | nothing | `SettleRejected{reason:"stale"}` |
| feed or receiver not carried / equipped / stacked / foreign | `WorldResourceActions.Carried` fails | refusal text | nothing | `{reason:"not-carried"}` |
| actor dead, stunned, asleep, action-blocked | `PreparationActions.CurrentActor`, `IsActionBlocked` | refusal | nothing | `{reason:"actor"}` |
| out of reach / fixture removed or swapped | reach gate; `BeforeCommit` identity checks | "out of reach" / refusal | nothing | `{reason:"station"}` |
| callback (`AfterInventoryAction`, `BeforeCommit`) mutates a participant | `tx.BeforeCommit` revalidates every scalar and identity | refusal, all scalars restored | nothing | `{reason:"changed"}` |
| exception after staging | `InventoryTransaction` rollback restores source, receiver | nothing changed | nothing | rollback only |
| committed | `AfterCommit` | "You settle N draws: ... water." | 1 action | `SettleCommitted{still,feed,draws,fed,out,lost,receiver}` |
| save/reload | no V1 state exists | flasks/skin as saved | n/a | n/a |
| old save, cached shelter | no fixture exists there | unchanged | n/a | n/a |

There is **no interrupted-mid-job state**: the action is instantaneous and transactional. Interruption in the design doc's sense (a job stopped partway) is a V2 concern that arrives with S16.

### 3.8 Alternatives the player keeps (at least two genuinely different, plus nothing)

1. **Ordinary water, no machine:** a well, a cistern, a fresh pool (`WaterVesselService.FindSource` accepts only fresh water [V `:113-133`]); at the shelter, a flask filled at Sumphold's endless well one zone north.
2. **Avoid the need:** Conjure Rain keeps crop soil moist [V Objects.json:50443]; in the Beating a head item or any interior prevents Parched outright [V `BeatingGlareSystem.cs:58`]; tonics relieve a stack [R `ParchedEffect` doc].
3. **Spend the feed as itself:** pour `bog-mire` (acid DOT 1/turn, Agility -2) or `brine` (conductive) as a ground film through the M1a pour path, or leave the pool as the route hazard it is. This is a real competing use of the same finite flask, with existing consumers on both sides, so "what survives" is a genuine choice without inventing a fraction.
4. **Do nothing:** nothing is locked. The shelter's `ripe-sumpsieve` already yields 2 pads and a seed without water; no quest, concern, route or gate requires the still.

### 3.9 Consequence outside its own menu

- **Crop chain (four systems):** settled water -> `CropWateringService` (+40 moisture ticks per unit; growth resumes) -> ripe crop yields 2 `SumpsievePad` + 1 `SumpsieveSeed` (the seed makes the loop renewable) -> the paid dressing service (2 drams to the keeper) -> field dressing for poison and bleeding.
- **Parched:** water in a skin is an ordinary drink; `ReduceOneStack` removes one Str/Agi penalty stack [V `WaterVesselService.cs:99`].
- **Fire:** the same water is what `DouseWorldActions` consumes (ordinary water, no special tag) [R].
- **World pool:** the feed pool's `Volume` is permanently lower; its footing reflects actual volume [R `Docs/AUDIO-POLISH-20.md:132`] while its coating source is unchanged (M1a: "zero collectible volume is not drained footing").
- **Economy:** none. No item, dram, salt or stock is created; no price or fee exists.

### 3.10 Beating water tension (owner decision D3)

The brine row exists because a settling still that refuses brine would break the common-sense expectation (principle 2) and because a second feed proves the contract is not a one-recipe special case. It is made *safe* by **placement discipline**, not by scarcity: V1 ships exactly one placement (the shelter, mire only in its zone) and a rule that a settling still is never placed in a zone containing `BrinePool` cells or a `BeatingFormation` BrineLens (RED `PlacementRefusesBrineBearingZones`). A player can still carry brine flasks to the shelter, which is a long, silly trip, not a threat to the Beating. Ratio 4:1 keeps one flask = one skin = one cure, which is the player's full per-cycle demand (Parched caps at 3 stacks). If the owner prefers zero risk, ship the mire row only and defer brine to the first designed desert placement (D3).

---

## 4. Contract sketch (for the implementing engineer; not compiled)

```csharp
// namespace CavesOfOoo.Core (saved type names are permanent: spec C1, hygiene lint)
public sealed class LiquidSettlingPart : Part      // stateless: no public fields, parameterless ctor
{
    public override string Name => "LiquidSettling";
    public override bool HandleEvent(GameEvent e)   // GetInventoryActions -> LiquidSettlingService.AddActions
    {                                               // InventoryAction (prefix "SettleLiquid|") -> LiquidSettlingService.TryAct
        ...                                         // later: IDescribeState.Describe(DescribeContext) (spec C6)
    }
}

public sealed class LiquidSettlingDefinition { string Id, FeedLiquid, OutputLiquid, ActionText, LossText; int FeedUnits, OutputUnits; }

public static class LiquidSettlingRegistry   // model: MaterialProcessRegistry (per-registry loader, all-or-nothing)
{
    public static void LoadDefaults();  public static void ResetForTests();
    public static IReadOnlyList<string> InitializeFromJson(string json);   // any error -> no rows, reader shows an idle still
    public static LiquidSettlingDefinition ForFeed(string liquidId);
}

internal sealed class SettlePlan   // pure; built by the menu AND re-evaluated at commit (preview == payment)
{   Entity Actor, Still, Source, Receiver; LiquidSettlingDefinition Row; int Draws, FeedBefore, FeedCap, ReceiverBefore, ReceiverCap;
    ReceiverKind Kind; /* Flask | Skin */  string ZoneId;  string Encode(); static bool TryParse(...); bool Current(); }

public static class LiquidSettlingService { AddActions(...); TryAct(actor, still, zone, command, tx); }
```

Data file `Assets/Resources/Content/Data/LiquidSettling.json` (JsonUtility-compatible; optional fields are strings/arrays):

```json
{ "Version": 1,
  "AllowedLiquids": ["water", "bog-mire", "brine"],
  "Rows": [
    { "Id": "settle-bog-mire", "FeedLiquid": "bog-mire", "FeedUnits": 3, "OutputLiquid": "water", "OutputUnits": 1,
      "ActionText": "settle bog mire", "LossText": "the rest is lost in the pan" },
    { "Id": "settle-brine",    "FeedLiquid": "brine",    "FeedUnits": 4, "OutputLiquid": "water", "OutputUnits": 1,
      "ActionText": "settle brine",    "LossText": "the rest is lost in the pan" } ] }
```

Load validation (each its own error; whole catalogue rejected on any error, matching `MaterialProcessRegistry`): malformed JSON; 0 or > 16 rows; duplicate `Id`; non-token `Id`/liquid; `FeedUnits` outside 2..12 (flask capacity); `OutputUnits` < 1 or >= `FeedUnits` (strict loss, `ToUnits <= FromUnits` per spec C10); feed == output; feed or output not in `AllowedLiquids` (**an allowlist, not a denylist**, so any new liquid, including every L19 liquid, needs a data edit and an owner decision); unknown to `LiquidRegistry`; an output that is also some row's feed (no cycles, no ping-pong); `ActionText` > 120 or `LossText` > 200 chars; null row.

Command: `SettleLiquid|stillId|sourceId|receiverId|rowId|draws|srcVol|srcCap|rcvKind|rcvBefore|rcvCap|zoneId` (every field escaped; every number re-checked at commit, the M1a stale-selection pattern [V `LiquidTransferActions.cs:81-133`]).

Commit algorithm (M1a shape): validate context -> parse -> resolve exact carried entities -> rebuild `SettlePlan` and require equality with the encoded numbers -> claim actor, still, source, receiver -> `tx.Do(apply, restore)` (source `Volume -= draws*FeedUnits`, `LiquidId = ""` at 0; receiver `+= draws*OutputUnits`, `LiquidId = "water"` for a flask, `Charges +=` for a skin) -> `tx.BeforeCommit` re-proves identity, Parts, cells, capacities and every scalar -> `tx.AfterCommit` publishes diag and message.

Reused shipped code (no behaviour change to any of it): `LiquidVesselPart`, `LiquidRegistry`, `LiquidVesselService.Validate` and `LiquidSourceSafety` semantics, `LiquidTransferActions` encode/stale/transaction pattern, `WaterTransferActions` unit semantics, `WorldResourceActions` (`Carried`, `ExactCarried`, `Nearby`, `Decode`), `PreparationActions.CurrentActor`, `InventoryTransaction`, `Diag`. Small refactor with a parity oracle: promote M1a's private `Vessel`/`Unique<T>` helpers to an internal shared helper (their only second consumer is this machine); M1a's `MachineLiquidTransferTests` is the oracle.

---

## 5. Spec fit, dependencies and gaps

| Spec item | Needed by V1? | Note |
|---|---|---|
| S1 saved-Part golden + hygiene lint | **Yes (hard)** | new Part in `CavesOfOoo.Core`, zero fields, unique class name (`LiquidSettlingPart`, `SettlingStill`, `LiquidSettling*` are free [V grep]) |
| S2 retention | No | stateless |
| S3 declared time + disabled rows | Preferred | fallback: one prefix in `WorldResourceActions.IsCommand`; without disabled rows, refusals are readable only in the reader |
| S4 station contract | **No** | the still is the menu target (C3); add `IStationCapability` kind `settling` only when an item row needs to find it |
| S5 reader contract | Preferred | fallback: hand-edit the two provider lists; delete when S5 lands |
| S6/S7/S8 installation registry, post-builder, census | **No for V1's own placement** | first placement is a plan edit (C4). S7 matters only if the owner later wants a village row; every dossier says there is no purpose to place one |
| S12-S18 | No | no multi-stack claim (vessels are unstacked), no quote channel, no job, no receivers, no stored work |

**Where the machine needs something the spec lacks** (state to the owner):

1. **No liquid-process contract.** The spec's C8 engine is item-only (C2); S17 (receivers/waste/conversions) is a sketch waiting for its first consumer and names no consumer. V1 intentionally does *not* build S17: it needs only carried vessels and a declared loss. Its registry validator (strict loss, no cycles, allowlist) should be written as the prototype for S17's conversion validator so the second consumer can lift it.
2. **No placement path for non-village authored plans.** S7 covers the 16 shared-pipeline villages; the Sodden district, the Beating wayside and the Sill glade ring are plan/worksite-placed. Either keep plan edits per precedent (V1's choice) or add a small "plan installation" extension later.
3. **Census blind spot.** The coverage census classifies the 20 *places*; the shelter is not one, so V1 does not move `Pending`. The still should be recorded as `not a town machine` with the 14/6 split (7.4).
4. **E07 admission list.** If a later still version gains a tank, its Part must be added to the state-bearing list so `Retention: None` is rejected for it.
5. **No spec guard for "never place near brine".** V1 states it as a RED test and a plan-level constant; if S7 ever places stills it needs a `ForbidNearBlueprints` field (additive).

---

## 6. RED-first tests (names; each paired with a counter-check; slices in section 7)

**Registry (STILL-1)** `LiquidSettlingRegistryTests`: `ShippedRowsLoadAndMatchAuthoredTable` (mire 3->1, brine 4->1); one test per validation error with a valid-neighbour control: `MalformedJsonRejectsWholeCatalogue`, `ZeroRowsRejected`, `MoreThanSixteenRowsRejected`, `DuplicateIdRejected`, `NonTokenIdRejected`, `FeedUnitsOneRejected`, `FeedUnitsAboveFlaskCapacityRejected`, `OutputEqualFeedUnitsRejected` (no free conversion), `OutputZeroRejected`, `FeedEqualsOutputLiquidRejected`, `UnknownLiquidRejected`, `LiquidOutsideAllowlistRejected`, `ProtectedL19LiquidsNeverAcceptedAsFeedOrOutput` (each of the eight: memory-bath, held-breath-lacquer, felling-counter-resin, bower-resin-amber, veined-pulse-mycelium, tepuibone-slurry, choir-wort, choir-mirror-mucilage), `OutputThatIsAnotherRowsFeedRejected` (no cycles), `TextLengthBoundsRejected`, `NullRowRejected`; `NoRowFeedsOnWater`, `NoSaltOrItemOutputInV1`; `ResetForTestsClearsRows`; `FailedLoadLeavesAnIdleStillNotACrash`.

**Execution (STILL-2)** `LiquidSettlingActionTests`: `AllFitDrawsConsumeFeedAndAddWater` (12 mire -> 0, receiver 0->4); `OneDrawRowOnlyWhenMoreThanOneFits`; `EmptyFeedClearsLiquidId`; `EmptyFlaskReceiverBecomesWater`; `WaterFlaskReceiverAccumulates`; `WaterskinReceiverGainsChargesCappedAtCapacity`; `PartialLastDrawLeavesRemainder` (11 mire -> 3 draws, 2 left); `DrawsLimitedByReceiverFree`; counters: `ReceiverHoldingBrineRefused`, `ReceiverIsSourceRefused`, `FeedWaterHasNoRow`, `OilAcidHoneyPitchInkHaveNoRow`, `EquippedVesselRefused`, `StackedVesselRefused`, `NotCarriedVesselRefused`, `SkinAsFeedRefused`; `ShortFeedRefusesNothingSpent`; `StaleSourceVolumeRefused`, `StaleReceiverVolumeRefused`, `StaleRowIdRefused`, `ForeignZoneRefused`; `OutOfReachRefusedByWorldMenu`; `DeadStunnedAsleepBlockedActorRefused`; `ExactlyOneActionChargedThroughWorldMenu` (real turn charge via the reflection fixtures `OpenWorldActionMenuFor`/`ExecuteWorldActionSelection`); `RefusalChargesNoTurn`; `PreviewEqualsPayment` (row numbers equal committed deltas); `ConservationProperty` (random volumes, capacities and receiver kinds: `fed == out + lost` and no unit created); `NoEntityPoolCoatingOrTileStateChange`; `BeforeCommitVetoRestoresEveryScalar`; `AfterCommitThrowRestoresEveryScalar`; `CallbackMovesStillSourceOrReceiverRefusesAndRestores`; `TwoAttemptsSamePreviewSecondRefused` (double-submit); `SaveRoundTripKeepsFlasksAndFixture` (token graph); `OldSaveWithoutPartLoadsUnchanged`; `DiagCommittedAndRejectedPinned`; `ExistingAlchemyStillUnchangedInV1` (no settle rows, brewing rows identical).

**Adversarial (STILL-2, `LiquidSettlingAdversarialTests`)**: parser inputs for the command (null, empty, fewer/more fields, non-numeric, negative, huge, double-escaped ids, whitespace); cross-instance (two stills, two actors, same flask id after a drop-and-repick); anti-exploit (settle then pour back to refill a pool cannot gain units: pour of `water` into a mire cell is refused `mixed-destination`; pour of `bog-mire` back in only returns what was drawn; loops never net positive); boundary (draws 0, int overflow of `draws*FeedUnits`); probability n/a (no RNG: `NoRngUsed` source pin); diag dispatch on every refusal.

**Cross-system (STILL-2/5)**: `SettledWaterWatersSumpsieveCropThroughCropWateringService` (MoistureTicks +40); `SettledWaterInSkinReducesOneParchedStack`; `SettledWaterIsOrdinaryWaterForDouse`; `DrawnPoolVolumeStaysLowerAndNeverRefills` (M1a path, counter: coating source unchanged); `OrdinaryPourOfMireStillWorksWithoutAStill` (alternative preserved).

**Reader (STILL-3)** `LiquidSettlingReaderTests`: `QuickReaderListsRecipesAndLoss`, `DocumentReaderStatesNoFuelFeeTimer`, `ReaderNamesQualifyingReceivers`, `ReaderCopyUsesNoForbiddenVocabulary` (rejects "distil", "fraction", "solvent", "reagent", "condenser", "charge", "mana", "salt" as a product), `ReaderIdleWhenCatalogueFailed`; with S5: `StillAppearsInBothReadersWithoutEditingEitherBuilder`; with S3: `DisabledRowsCarryReasonCodes` (`short`, `no-receiver`, `receiver-full`), `DisabledRowSpendsNothing`.

**Placement (STILL-4)** `LiquidSettlingPlacementTests`: `FreshShelterGetsExactlyOneStillAtTheChosenCellEverySeed` (seeds 64, 704860946, 1729 plus the five `variation` values); `StillCellIsPassableReachableAndBlocksNoLane`; `OtherShelterOwnersIdenticalWithFlagOff` (compare blueprint+position sets, never IDs); `OtherSoddenZonesUnchanged` (16.7, 17.7, Sumphold); `MissingBlueprintOmitsOnlyTheStill`; `MalformedBlueprintOmitsOnlyTheStill`; `CreationCallbackInjectionOmitsOnlyTheStill`; `CachedShelterNotRetrofitted`; `OlderExplorationVersionGetsNoStill`; `PlacementRefusesBrineBearingZones`; `ExemptPlacesNeverReceiveAStill` (Drowned Ledger, Tent-Right places, Choir places, catacomb villages); `ExistingSoddenDistrictExactCountTestsUpdatedIntentionally` ([U] which suites pin shelter owner counts).

---

## 7. Slices, cadence, acceptance

Cadence: one cycle = one isolated-clone EditMode run (about 5 min). Every slice is RED-first, carries counter-checks, an adversarial file where two or more taxonomy surfaces apply, diag pins, a cold-eye pass with both audit angles, and a living-doc paragraph in `Docs/MACHINE-LIQUID-SETTLING.md` in the same commit.

| Slice | Content | Needs | Cycles |
|---|---|---|---|
| STILL-0 | promote M1a's private vessel validators to an internal shared helper; M1a suite is the oracle; no behaviour change | none | 1 |
| STILL-1 | `LiquidSettling.json`, registry, validator (all errors above) | S1 | 1 |
| STILL-2 | `LiquidSettlingPart`, `SettlePlan`, service, blueprint `SettlingStill`, charge hook (prefix line or `ActionTime`), diag; actions + adversarial + cross-system tests | STILL-1 | 2-3 |
| STILL-3 | reader lines (S5 or hand-listed), disabled reasons (S3 or silent) | STILL-2 | 1-2 |
| STILL-4 | shelter plan flag, builder validator, fresh-only placement, cached control, brine-zone refusal | STILL-2 | 2 |
| STILL-5 | actual play and docs (below) | all | n/a |

Total about 7-9 isolated cycles (35-45 min of test time) plus actual play. All slices need the owner to lift `CLAUDE.md`'s "No new runtime changes" hold. No slice needs the S10/S12 independent-critique gates (no permission, no weapon payload, no equipped input, no multi-stack claim); an independent *lore/voice* review of every string is still required (L17).

**Actual-play acceptance** (fresh world plus cached-save control; CUA-only evidence limits stated as in prior records): from a fresh seed (64 and 704860946) reach Sumphold, then the shelter 15.7 on the ordinary route; obtain a flask through an ordinary route (Sill purchase/WellKeeper stock/loot) *before* arriving, because Sumphold has no WellKeeper; stand beside a `MirePool` band cell on dry shore and Fill (existing M1a); open the fixture menu; read the preview; cancel once (state unchanged); settle; read the receiver; water the dry sumpsieve crop through the ordinary watering menu; observe the pool cell's lower volume; F5; fresh Continue; verify flask/skin/crop moisture and fixture. Counter-controls: an older cached shelter (visited before the change) shows no fixture; ordinary pour of mire still works without it; `AlchemyStill` rows unchanged at Sill. Brine row: native tests plus one actual-play settle with a carried brine flask (earned, not injected). Evidence statements required: glyph presentation only (no approved art; graphics cancelled), Linux/Deck unexecuted, no packaged performance claim, the 2-unit crop demand is [I] until observed.

### 7.4 Census recommendation (for the spec's S6/S8 tables)

Record `still` as **not a town machine**: `Poor` in 14 places (list in 2.1), `Pending: needs authored feed and consumer` for Sumphold, Slip, Posy, Salt-Vault, Quiet's Door, Lampwell, with the named blocker for each from its dossier. Do not use the still to reduce the `Pending` count. The S8 "still at Posy/Quillhold" rows are placements of the **existing alembic** for reagent brewing and are unaffected.

---

## 8. Pitch behaviours deferred, and why

| Pitch behaviour (`MACHINE-ROSTER-VISION.md:30-33`) | V1 | Why deferred / what it needs |
|---|---|---|
| separate contaminated water, oils, sludge, sap into **different fractions** | one fraction (water) from two feeds | a second fraction needs a real consumer (2.4.1) and canon sign-off; oil/sap/pitch/ink/honey are deliberately not settleable (each an allowlist edit) |
| choose between usable water, a **rare reagent**, a **concentrated solvent** | the choice is feed-vs-product (pour/leave vs settle), not a three-way fraction | a reagent/solvent liquid with a consumer does not exist; L19 forbids producing or refining toward the lore-loaded liquids; minting salt is forbidden (L9, EC5) |
| **recipes depend on what actually went into the tank** | recipes depend on which registered liquid is in the carried flask | a tank is persistent state (retention, readers, art) and a composition/provenance contract (design doc option 2) |
| **fuel/work**, bounded jobs, interrupted jobs | none | spec S15/S16/S18; no fuel accounting contract; do not fake heat |
| **waste receivers**, retained residue | declared loss | spec S17; M1d review rejected zero-value dregs |
| a **swamp workshop and a desert workshop** with different purposes | two feeds in data; one real placement (swamp) | the desert placement is the Beating water decision (D3) and likely needs a portable/installable form (spec S19) |
| **visible state** (the machine reflects what is inside) | none | stateless; art route deferred (graphics cancelled; spec presentation notes) |
| staffed/priced service (Concord assay-still, Tent-Right "given not sold") | Open, no operator, no fee | spec S10 terms; Tent-Right water is never priced (R7/L3) |
| NPC use | none | spec S20; V1 is player-only |
| direct draw from an adjacent pool (a pump-like feed link) | flask-only | couples a fixture to a pool (positional authority) and edges into machine 7; needs a source-link contract |
| local consequence (outlet wets the yard) | pool volume only | no outlet model |

---

## 9. Owner decisions and risks

**Decisions (this dossier does not decide them):** D1 lift the runtime-change hold for this slice set. D2 accept that V1's purpose is thin and that the still does not count toward town coverage (7.4), or park the machine until a purpose is authored. D3 Beating water: keep brine at 4:1 as a latent carry-in row, or ship mire only. D4 optionally attach `LiquidSettlingPart` to the shipped `AlchemyStill` blueprint later (V1.1): pro, every future alembic (Sill, field alembics, S8 rows) gains the verb with zero placement and the capability is composed onto instances as the principles ask; con, it changes a shipped blueprint, adds a carry-in-only service where the dossiers say there is no feed, and some of the ~12 test files naming `"AlchemyStill"` may pin its Part list [U: scan first]. D5 the in-world name ("settling still" is a proposal; faction variants in the voice bank: Concord "assay-still", Recension "ink works", Curation "intake still", catacomb "ward-pot", Tent-Right "brine pan-still"; none passed the gate). D6 the exact shelter cell, chosen from an actual baseline.

**Risks (ranked):** (1) building a machine with no demand, mitigated by a tiny scope, no gate and no coverage claim. (2) Brine abundance undermining Beating scarcity, mitigated by placement discipline (3.10). (3) Shelter plan tests that pin owner counts or reservations [U]. (4) Flask supply: a player at the shelter without a flask has nothing to settle, so the reader must say what to carry; Sumphold and the shelter have no flask vendor. (5) Voice: no machine string has a recorded blind-gate pass (L17). (6) Glyph-only presentation in the shelter.

---

## 10. Canon fit

- **Name:** "settling still" (draft). The word "still" is already shipped for `AlchemyStill` ("separates a mixture one patient drop at a time", Objects.json:5579 [V]); "settling" is plain-craft register for the Sodden peat-cutters and avoids the design-doc vocabulary ("fractionating", "solvent", "reagent", "condenser" are off-register, L16/R10).
- **Operator roles:** none required (self-service, like the strainer: "Its use is shared; it asks no fee"). Shipped roles that may *explain* it later through one voice-gated dialogue line: **PeatCutter** (the shelter's keeper, "Sella, dressing keeper", `SoddenPreparationPart.cs:30-31`), **WellKeeper** (flask supply and water care), **Apothecary** (Sill's reagent still). Not operators: TentRightHost/SaltMaster (water is given, never sold, R7), Curation roles (no specimen processing).
- **Locality:** the Sodden shelter circuit, `sumpsieve` and the dressing keeper are game-coined local content, not in root `Lore/` [V grep: no "sumpsieve" in `Lore/`]; label it local, as Morrowfast is labelled. Sumphold's peat-cutters and bog gateway are canon (`Lore/History/02_Geography.md:69`).
- **Landmines (ids from `lore.md` section 7) and how V1 avoids them:** **L19** allowlist `{water, bog-mire, brine}`, validator rejects every lore-loaded liquid as feed or output, tested per liquid. **L9** no salt, Pale-Salt, Choir-Iron or any material-identity output; brine's salt is lost in the pan, never produced. **L3** no fee, no price, never placed in Tent-Right places (Wellmeet, First Tent) or at a well; there is no verb that pours into a well [I: grep `WellPart` pour paths in the first cycle]. **L5** never at the Drowned Ledger; feed is only the registered liquid id, never bodies or "peat water used by the preserved"; no Bog-Taken product. **L4** not placed at the Deepest Cathedral or any Choir ground; the device uses no flame. **L8** not placed in catacomb villages (Quiet's Door forbids outsiders' installed machines; the patch and ring are not feedstock). **L1/Mystery #8** no text may mention frogs; a flask of Ginmere's mire is indistinguishable from any mire and is accepted as a plain liquid id [I]. **L16** no technology nouns. **L17** every string needs the blind voice gate. **L18** V1 is Open, like the shipped forge/still; the permission seam stays unused. **Sill ward-oil comic fraud:** V1 never produces `WardOil`, `LampOil` or any oil.

---

## 11. Verification record

**Read this session [V]:** `Docs/MACHINE-ROSTER-VISION.md` (all); `Docs/MACHINE-SYSTEMS-DESIGN.md:55-166`; `Docs/SYSTEMS-DESIGN-PRINCIPLES.md` (all); `Docs/MACHINE-OIL-CLARIFICATION.md`, `MACHINE-LIQUID-HANDLING.md`, `MACHINE-CONTAINED-OIL-RECOVERY.md` (all); `Docs/BIOME-AFFORDANCES.md:8-40`, `Docs/CHUNK-ACTIVITY.md:160-168`; `ARCHITECTURE-SPEC.md` (all); `lore.md:1-112,196-369`; `enumerate.md` (all); the still verdict line of all 20 settlement dossiers; `AlchemyStillPart.cs`; `LiquidVesselPart.cs`; `LiquidVesselService.cs`; `LiquidTransferActions.cs`; `WaterTransferActions.cs`; `WaterskinPart.cs`; `WaterVesselService.cs:1-133`; `ParchedEffect.cs`; `BeatingGlareSystem.cs`; `ContainedLiquidSourcePart.cs`; `LiquidPoolPart.cs`; `FuelPart.cs`, `TorchRefuelPart.cs`; `MaterialProcessRegistry.cs`; `MaterialProcessInputPart.cs`; `MaterialProcessPlan.cs`; `PreparationActions.cs`; `CropWateringService.cs:1-80,140-175`; `SoddenPreparationPart.cs:1-80`; `SoddenDistrictPlan.cs:1-110`; `SoddenDistrictBuilder.cs` lines 48, 87, 176; `BeatingFormationBuilder.cs:262-292`; `OilStrainingPart.cs:56-110`; `WorldResourceActions.cs:1-60`; `InputHandler.cs:3198-3212,3455-3475`; `Diag.cs:115-135`; Objects.json blueprints `AlchemyStill`, `LampOil`, `WardOil`, `LiquidFlask`, `GreybladderCup`, `BrinePool`, `SumpsieveSeed/Crop`; liquid definitions `brine`, `bog-mire`, `oil`; `LootTables.json` LampOil/LiquidFlask hits; concerns `Quillhold.json`.

**Not read ([U], each settled by a named test or first-cycle step):** exact shelter cell and reservation interplay (`StillCellIsPassableReachableAndBlocksNoLane`); which suites pin shelter owner counts; whether any test pins the `AlchemyStill` Part list (D4 scan); whether any pour path can fill a `Well`; `DouseWorldActions` internals and the `WorldAudioCue.WaterPour` cue as a reusable cue; `TonicPart` relief rule; `AUDIO-POLISH-20.md:132` footing claim (taken from a grep line); whether `liquid` diag records appear under the settings the tests use (category is default-on).

**Claims I rely on that came from reports [R] and were not re-read:** `TradeStockBuilder.cs:37` LampOil stock; `VillagePopulationBuilder.cs:790-798` repair stock; `settlement-*.md` evidence for places other than the still verdict lines; `presentation.md` finding 7.

No test, build, Unity run or git write was performed.
