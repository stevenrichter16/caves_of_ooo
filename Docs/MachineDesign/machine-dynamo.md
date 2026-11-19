# MACHINE V1 DOSSIER: Dynamo and charging bench (machine 9)

Status: design only. Repo `/Users/steven/caves-of-ooo`, HEAD `c0c65d6f9` as read (spec was written at `9316f9330`; the one newer commit is a doc record). No project file, Unity run, test, build or git write was made. All C#/JSON below is a sketch for the implementing engineer. Lore authority is root `Lore/` only (`Docs/Lore/` unused).

Evidence tags: **[V]** I read the cited line or ran the cited grep in this session. **[R]** taken from `mw/` research or the architecture spec without re-reading. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle step settles it.

Inputs read: `mw/design/ARCHITECTURE-SPEC.md` (all), `Docs/MACHINE-ROSTER-VISION.md` (all), `Docs/MACHINE-SYSTEMS-DESIGN.md` (all), `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `mw/lore.md` (sections 0-3, M9, 5-11), `mw/arch.md` (2.6-2.7, 6, 7, 9), the dynamo rows of all 25 `mw/settlement-*.md` (by grep), sibling dossiers `machine-thermal.md`, `machine-pump.md` (verdict and structure), plus the shipped code and content in section 2.

---

## 0. Verdict

1. **Classification: `wave3-needs-new-system`.** Not wave2: even with spec S1-S14 shipped the dynamo lacks three things the spec does not give it: a canon answer to "what is a charge" (`lore.md` section 11 item 1, spec owner decision 6), a stored-work contract (spec S18, canon-gated and "waits for its first consumer"), and a real consumer.
2. **The dynamo has no real purpose in the shipped world.** The design doc's own bar is "authored energy source, storage loss/capacity and at least one real consumer" (`MACHINE-SYSTEMS-DESIGN.md:93`) [V]. At HEAD there is no source, no store and no consumer. The one obvious consumer, light, is **render-only**: no Gameplay code reads `LightMap` brightness [V grep: the only `Gameplay/` hits are comments and the equipment-change bus], `FieldOfView.Compute` is radius-only [V `FieldOfView.cs:19-45`], and the ambient ladder comment in `OverworldZoneManager.cs:1372-1380` says content "demands a lamp" while a search of `Scripts`, `Content/Data` and `Conversations` for light-gated content finds nothing [V]. A lamp that runs on stored work would be cosmetic. Section 3 audits every other candidate.
3. **It is not a town machine and adds zero towns to the census.** All 25 settlement dossiers rate it "poor" [V grep: 25 of 25 files]. A town is covered by any machine, so the dynamo should simply stay out of the placement waves; this costs the "machine in every town" ratchet nothing.
4. **Smallest honest V1 (gated): a pair, like the pump's.** A fixed hand-wound **winding bench** that stores finite "winds" in a carried **strike-wheel** (spring), and exactly one consumer: the strike-wheel lights a lone torch or relights cooking coals when no flame is in reach. That consumer reuses two shipped systems through one new seam and completes a loop M1d just shipped (refuel a torch with clarified lamp oil, then be unable to relight it away from a fire). Stakes are deliberately modest and the acceptance test can fail (section 5.12).
5. **Recommended action: park it.** Do not spend a slice on this before the owner decides D1 (stored-work canon) and D2 (whether a modest ignition consumer is wanted at all). The pitch's real payoff, "spend your remaining charge on a scanner, a pump, or restoring the machine you just found", needs at least two consumers that do not exist (pump, resonator and cradle are all unbuilt); V1 cannot deliver that decision and says so.
6. **Corrections to the inputs.**
   - `arch.md:128` and `lore.md` say there is no charge or electricity in the game. True for stores; but the game ships three unrelated meanings of "charge": ink charges (`GrimoireChargePart`, `Charges`, `ChargesPerVial`) [V `GrimoireChargePart.cs:31-58`], an `ElectrifiedEffect` hazard that `GroundwireScreen` "grounds" [V `EquipmentUtilityActions.cs:45-47,60`], and a tile `Charge` channel (`ZoneTileState`, spell-written) [R `MACHINE-SYSTEMS-DESIGN.md:62`]. None is a store. The word must not be reused for the new store.
   - The shipped generic Tinker conversation talks about "Power cells, circuitry... a heating coil" (`FriendlyNPCs.json:225,275`, conversation `Tinker_1`) [V]. `lore.md` and the Marrowstye dossier treat that as superseded old-world voice; it is a landmine (section 5.10), not authority.
   - A `Tinker` is **not guaranteed** in a town: `VillagePopulationBuilder.cs:132-134` places one on a 70 percent roll [V]. No operator may be required.
   - The spec's S7 generic builder places exterior, non-solid forges. The winding bench is an interior workshop fixture, so V1 uses the Cinderhold plan-flag path instead (section 5.9).

---

## 1. The pitch, clause by clause

`Docs/MACHINE-ROSTER-VISION.md:71-75`: "Charge cells and equipment using hand power, fuel, or suitable environmental sources. Early operation should be straightforward: connect a source, insert a cell, select a job. Different generators offer clear tradeoffs in portability, noise, fuel, and output. This creates an expedition decision: spend your remaining charge on a scanner, a pump, or restoring the machine you just found." [V]

| Pitch clause | What would have to be true | What exists at HEAD | Verdict |
|---|---|---|---|
| **cells** (a portable store) | an item that holds a finite quantity and releases it | No store of any kind. `FuelPart` is combustible mass consumed by burning, with hand-coded feeds (timber +10, `CampfireWorkActions.cs:70-80`; lamp oil +25, `TorchRefuelPart.cs`) [V]. `GrimoireChargePart` is ink for rites (a magic cost, canon L10) [V]. | Absent. Section 4 G3. |
| **equipment** that uses it | portable gear with a power-hungry action | Portable finite gear: torch (`FuelMass` 50, `BurnRate` 0.3, about 166 turns per full torch; `Objects.json` Torch blueprint [V]), `CrackedGlowQuartz` (1 mineral, 30 turns, `EquipmentUtilityActions.cs` label [V]), grimoire ink. Nothing consumes stored work. | No consumer. |
| **hand power** | an effort resource whose spending matters | No stamina, hunger or thirst system (search of `Gameplay` for hunger/thirst/satiety/stamina/fatigue: only `WellDrinkingService`) [V]. `HarvestablePart` is a one-action yield with no tool or effort field [V]. The only effort mechanic is the rig crossing's two source rounds (`InputHandler.ExecuteRigCrossing`) [R]. | Effort costs only turns. Turns do have a real clock (relapse timers, kitchen due ticks) [R], so time is a legitimate if weak cost. |
| **fuel** | fuel-to-work conversion | None. Burning timber or oil to wind a spring is strictly worse than lighting a torch with it. | Absent and dominated. |
| **suitable environmental sources** | a capturable quantity | `SteamVent` is a status source with no reservoir (`machine-thermal.md` section 1) [R]; `MillStone` is scenery whose text says "Off its spindle it is just a very heavy circle" (`Objects.json:34221`) [V]; weir "current" is flavor text [R `settlement-tine.md:101`]; `SparkRoot` is a reagent tagged `conductive:3` with no use (`Objects.json:5983`) [V]. | Absent. A "conductive" source would be a new, canon-silent mechanism. |
| **straightforward: connect a source, insert a cell, select a job** | three objects and a job menu | Needs a source, a cell and a job registry. | V1 collapses to bench + one item + two verbs. |
| **different generators: portability, noise, fuel, output** | at least two generators | Zero. `Noise.Mark(actor)` exists (one int property; only `HuntsBySoundPart` listens) [V `Noise.cs`], used by hauling (`RigRelocation.cs:125`) and every step and swing [V]. | V1 ships one generator. The tradeoff axis needs a second. |
| **expedition decision: scanner, pump, or restoring** | at least two consumers sharing one store | Scanner = survey resonator (unbuilt), pump (unbuilt; `machine-pump.md` finds no real purpose either), restoration cradle (unbuilt; `machine-cradle.md:197`: "dynamo (9) only after the stored-work canon decision") [R/V]. | Cannot exist until two other machines do. Honest limit. |
| **finite conversion and refusal paths verified; charged equipment does useful work away from the generator** (design doc gate, `:93`) | the loop in section 5 | Nothing shipped. | The V1 loop is built to meet exactly this gate, at one consumer. |

Net: of nine clauses, none is met by shipped content; one (a modest consumer) can be met by a small, honest construction.

---

## 2. Verified foundations to reuse (and their limits)

| Foundation | File:line [V unless noted] | Reuse in V1 | Limit |
|---|---|---|---|
| Ignition rule | `IgnitionSources.Find(actor, zone, exclude)` returns a hot ground source within 1, else an equipped lit torch; `Hot()` requires aflame, not frozen, not wet over 0.35, fuel present (`IgnitionSources.cs`, whole file). Call sites: `CampfireWorkActions.cs:36` (menu), `:85` (act), `TorchLightPart.cs:221` (`FindFire`). The menu site (`:36`) only null-checks; the two act sites null-check, then `tx.TryClaim(source, ...)`, and never pay for the source. | The one place a new ignition provider plugs in, serving torch light and coals relight with no per-pairing authoring | Closed static rule; returns an `Entity`; no payment concept. Needs the G2 seam (section 7). |
| Torch light action | `TorchLightPart.TryAct`: rejects `no_fuel`, `frozen`, `wet`, then `no_fire` ("Stand beside a burning fire to light the torch."); claims actor, torch, source; transactional with undo (`TorchLightPart.cs`, `TryAct`) | Unchanged. The torch's own gates stay the authority | A lone torch that goes out away from fire cannot be relit (needs a flame or a second lit torch, `IgnitionSources.Find`). Wet or frozen torches cannot be lit at all, striker or not. |
| LampOil refuel | `TorchRefuelPart`: unlit torch, +25 fuel capped at `MaxFuel`, `CanRefuel` requires `light.Enabled == false` [V]; `MACHINE-TORCH-READOUT.md`: "Relight beside genuine burning fire" [V] | Gives the strike-wheel its verified purpose: the refuel loop M1d completed (`MACHINE-OIL-CLARIFICATION.md`: 2 FrogOil to 1 LampOil) ends in "now find a fire" | Alternatives are strong (section 5.7). |
| Equipment verbs | `EquipmentUtilityActions`: fingerprinted commands bound to origin, target and state, effects after commit; verbs `TouchTorch`, `FanGas`, `GroundCharge`, `BraceGrip`, `BraceBoots`, `CrackQuartz` [V] | Pattern for exact-bound item verbs. The new Parts own their rows (as `TorchRefuelPart` does) and do **not** edit this closed `Verbs` list | `TouchTorch` already pays 1 fuel to kindle a target; unaffected. |
| Transactions | `InventoryTransaction` claims, `Do/AfterCommit`, rollback (`TorchLightPart`, `TorchRefuelPart`, `CampfireWorkActions`) | Winding and striking debit and credit in the same transaction as the action | none |
| Noise | `Noise.Mark(Entity)` writes `NoiseTick`; `Noise.Recent(actor, window)`; `HuntsBySoundPart` on `PaleStalker` (`Objects.json:19350-19427`) [V] | Each crank marks noise: a real, verified consequence | Only eyeless hunters listen; the one shipped species lives at Cathedral, a Choir place where machines are exempt. The consequence is real but narrow for a bench; it matters for a later portable crank. |
| Cinderhold optional fixtures | `CinderholdCompositionPlan.Create(id, seed, includeRecoveryTray, includeReclamation)`; profile cells `TinkersForge` (work.X+5, work.Y+3), `SmithAnvil` (+10,+3), `Weaponsmith` (+5,+5), tray (+8,+3), reclamation (+12,+6) (`CinderholdCompositionPlan.cs:37-58`) [V]; builder's Weaponsmith "regional shelf" stock path (`CinderholdCompositionBuilder.cs:199-223`, grep) [V]; `CinderholdOilRecovery`: "Optional fresh-town salvage, never a cached-zone retrofit" (`:6`) [V] | The placement and supply precedent: a third opt-in flag, and the M3b leather-offcut regional-shelf path for the strike-wheel | Cinderhold only; other composed towns have their own plans. |
| Spec contracts | S1 golden, S3 declared time and disabled rows, S4 station query/receipt, S5 reader, S6 census | Body, readers, refusals | Section 7 lists the gaps. |
| Existing stock tables | `LootTables.json:602` `WeaponsmithStock`, `:2729` `TinkerStock` [V] | **Not used**: adding an entry advances the shared pipeline rng for every fresh world's shops (violates the spec's determinism rule) | n/a |

---

## 3. Purpose audit (principle 1: the opportunity before the apparatus)

Every candidate first consumer I could ground in verified rules, tested against: does it change a real outcome, is there a cheaper ordinary path, is it canon-safe, is the shared-core edit small.

| # | Candidate consumer | Real mechanical effect? | Ordinary alternatives | Canon | Verdict |
|---|---|---|---|---|---|
| C1 | **Strike-wheel: light a lone torch or relight coals with no flame in reach** | Yes: removes the only hard dependency of two shipped actions (`IgnitionSources` is the one gate) and closes the LampOil refuel loop away from fire | spare lit torch (burns fuel continuously), return to a campfire, buy torches (35 drams for four, `MACHINE-TORCH-READOUT.md`), stay lit | Mechanical wheel and spring; lighting a lamp "with a word" is everyday magic (`09_Magic.md:83`) and stays available, no charm is stored | **Recommended default.** Modest stakes. |
| C2 | Wound lamp (flameless, wet-proof light) | **No.** Light is render-only (section 0.2). It would be the "progression padding" the owner rejects | torch, campfire, `CrackQuartz` (30 turns) | Catacomb villages "flinch at the waste" of light (`08_MaterialCulture.md:47,77`); machine light there is an offense | Reject until light gains mechanical content (e.g. plaque-reading, as the ambient comment hopes). |
| C3 | Powered drive for the pump | Would shorten a hand-pump stroke | hand stroke | fine | Pump V1 is itself unqualified (`machine-pump.md` section 0). Park behind it. |
| C4 | Spring winch assist for hauling or rig relocation | Would change an accepted M1c contract ("two source-effort rounds") | existing hauling | n/a | Reject: alters accepted behaviour; capstan is parked (`machine-capstan.md:195`: no stored work in V1). |
| C5 | Spring-cocked thrower or crossbow | Real, but a new combat weapon class with balance review; no ranged-spring item exists (search of `Objects.json` for crossbow/sling/spring/clockwork/windlass: none) [V] | existing throws (`ThrownRecoveryPart`) | fine | Out of scope for a bench; separate combat design. |
| C6 | Spring "drill" or assist for a process row | Process rows have no effort axis; adding one degrades ordinary cold work (spec R7) | existing rows | n/a | Reject. |
| C7 | Tile "charge" writer or electric trap (use the existing Electric hazard) | Real hazard rules exist (`ElectrifiedEffect`, tile `Charge`, brine conducts) [V/R] | n/a | **No electricity in `Lore/`** (`lore.md` section 0.1, 3); Wet+Electric arc rider next to permanent water is a documented hazard (`settlement-drowned-ledger.md:213`) | Reject pending the owner's "is manufactured charge canon" answer (D1 option B). |

Only C1 passes. It passes narrowly, and the dossier says so.

**The decision the pitch is really about needs a second consumer.** One store, one consumer is a supply chain, not a choice. V1 therefore delivers the *loop* (source, conversion, finite store, refusal, away use, save) and not the *decision*. The first honest second consumer is one of: a powered pump stroke, a resonator pulse, a cradle's power-hungry substitution. None exists; each is another dossier's deferred version.

---

## 4. What must exist first (gates)

| Gate | What | Who/what resolves it |
|---|---|---|
| **G0** | Lift `CLAUDE.md`'s "No new runtime changes" for this work. The stored-work policies also sit behind "independent design critique before RED". | owner |
| **G1 (D1)** | Canon decision on stored work. Recommended: **option A**, stored mechanical work only, unit name "wind", no kind named charge/mana/glow/urqu/battery/quartz/pyrite, no feed carrying Glow-Quartz, Sari-Pyrite, hearth-patch or Founding-Plume identity. Option B (a reviewed natural electrical source) is not recommended: canon is silent and the Drowned Ledger arc hazard shows the cost. | owner (`lore.md` section 11 item 1) |
| **G2** | An ignition-provider seam in `IgnitionSources` (behaviour-preserving adapter slice D1 below). | engineering |
| **G3** | The smallest stored-work contract: `WorkStorePart` plus `StoredWork.Credit/Debit` in a transaction (spec S18 core, without feeds, jobs or conversions beyond one hand-to-wound row). | engineering, after G1 |
| **G4** | Owner picks the consumer (C1, or "wait for pump/resonator"), the strike-wheel's supply and price, and names (voice gate, L17). | owner |
| spec prerequisites | S1 (golden), S3 (declared `OneAction`, disabled rows), S4 (station contract), S5 (readers). S2 and S7 are **not** needed: V1 places by plan flag and the bench is stateless. | spec |

---

## 5. V1 definition (gated by G0-G4)

### 5.1 Names and voice (drafts; each needs an intent card and the blind gate, `lore.md` R10, L17)

- Fixture: **winding bench** (canon-safe voice-bank term for the surface/Tinker column, `lore.md` section 8 row 9) [R]. Item: **strike-wheel** (draft; avoid "spark", which collides with the `SparkRoot` reagent). Stored unit: **wind** (not "turn", which already means a game turn).
- Voice, from `lore.md` M9's draft Tinker line [R]: "It holds the work you put in. It does not hold a charm. Nothing winds a charm." Reader text stays mechanical: wheel, spring, pawl, crank, wound.
- **Forbidden in any player string and in data** (lint-tested): charge, charged, power, battery, cell, generator, dynamo, circuit, mana, glow, quartz, pyrite, Urqu. Never the word "Charges" as a field (it is `GrimoireChargePart`'s field).
- Operator: none required (Open bench). If the owner later wants a keeper, shipped roles that can operate a Tinker-class bench are Tinker, Weaponsmith, Armorer and Concord Factor (`lore.md` section 11 item 6) [R]; Cinderhold's plan has a Weaponsmith and a Factor (`CinderholdCompositionPlan.cs:51,57`) [V].

### 5.2 Fixtures and Parts

All new Parts in namespace `CavesOfOoo.Core`, unique simple class names (the factory registers by `Type.Name`, `EntityFactory.cs:77-92` [R]), public default-constructible fields, no field ending in `ZoneID`/`ReceiverID` or named `StationX/Y` (S1 lint).

```csharp
public sealed class WorkStorePart : Part                 // on the strike-wheel (spec S18 core)
{
    public override string Name => "WorkStore";
    public string Kind = "wound";                        // allow-listed at load; "charge"/"mana"/"glow"/... rejected
    public int Units;                                    // 0..Capacity, integers, no leak, no passive gain
    public int Capacity = 12;
}
public static class StoredWork                           // static service, no state
{
    public static bool CanCredit(Entity holder, string kind, int units, out string code);   // "full", "kind", "none"
    public static bool Credit(InventoryTransaction tx, Entity holder, string kind, int units); // refuses over capacity, never clamps
    public static bool CanDebit(Entity holder, string kind, int units, out string code);    // "short"
    public static bool Debit(InventoryTransaction tx, Entity holder, string kind, int units);
}
public interface IIgnitionProvider                       // G2 seam; implemented by WoundStrikerPart
{
    bool CanProvide(Entity actor, Zone zone, out string code);                 // pure
    bool Pay(InventoryTransaction tx, Entity actor, string command);           // debit inside the caller's transaction
}
public sealed class WoundStrikerPart : Part, IIgnitionProvider, IDescribeState
{
    public override string Name => "WoundStriker";
    public int StrikeCost = 2;                           // winds spent per light
}
public sealed class WindingBenchPart : Part, IStationCapability, IDescribeState   // kind "winding", Reach 1, Strict readiness
{
    public override string Name => "WindingBench";
    public int WindsPerAction = 1;
}
```

- `WindingBench` blueprint: non-solid like `TinkersForge` (`Objects.json:5597-5640` [V]) so it cannot block the small work room; layer 5; a unique glyph/colour pair among machine blueprints (the four stations share glyph `n` today [R]); no `RepairablePart`, no `ContainerPart`, no state: retention is not needed (stateless), `Retention: None` semantics.
- `StrikeWheel` item blueprint: Takeable, weight about 2, **no `StackerPart`** (a singleton, like a held torch's `CanReach` requires `StackCount == 1`); parts `Render`, `Physics`, `Examinable`, `WorkStore`, `WoundStriker`. Never joins `StackPayloadIdentity` because it cannot stack or split [V: that file groups mutable payloads and would otherwise need a new group]. A RED test pins the singleton.
- No new event names. `IStationCapability`, `IDescribeState` and `ActionTime` come from the spec (S3, S4, S5).

### 5.3 Player verbs

1. **Wind** (at the bench; row per eligible carried strike-wheel): "wind the strike-wheel (+1, 7 of 12)". Declared `ActionTime.OneAction`. Each action credits `WindsPerAction` (1) wind, marks noise, and consumes nothing else.
2. **Light** (the torch's existing row, unchanged): when no free flame is in reach and a carried strike-wheel holds at least `StrikeCost` winds, the torch's light action succeeds and the wheel pays 2 winds in the same transaction. The row label does not change; the refusal text changes only to name the alternative ("Stand beside a burning fire, or wind a strike-wheel at a winding bench.").
3. **Relight coals** (the cooking fire's existing row, unchanged): the same provider rule, when the coals have fuel and sit below 450 [V `CampfireWorkActions.cs:36,85`].
4. **Read** (`x`, F1): the strike-wheel shows winds and cost; the bench shows what it does and that it keeps no state.

Priority rule (verified-design, tested): `Find` consults providers **last**. A hot ground fire within 1 or an equipped lit torch is always chosen first, so the wheel never spends winds when a free flame is available.

### 5.4 Inputs, outputs, units, loss, cost, time

| Item | Value (design values, not derived) | Notes |
|---|---|---|
| Input to winding | the actor's action; no item | The only cost of winding is one paid action per wind and the noise stamp |
| Store | `WorkStore` kind `wound`, capacity 12 winds | Integers; refuses over capacity rather than clamping |
| Unit conversion | 1 wind action : 1 wind. Light costs 2 winds | So 2 actions of cranking buy one flame; the exchange is shown in the reader. This is the only "loss": there is no byproduct and no hidden waste destination. A spent wind is gone with the strike that used it |
| Output | one flame event on an existing torch or coals | The torch's `FuelMass` still burns at 0.3 per turn: **the wheel creates no light, no heat and no fuel** (counter-checked) |
| Leak / regeneration | none | No passive unwinding and no gain; no away simulation (honest limit, policy of spec C9) |
| Time | each crank is one scheduler action; a strike is the torch's own action | No `WorkJobPart`, no elapsed job |
| Fee / fuel | none | Open bench, no terms (spec S10 not needed) |
| Noise | `Noise.Mark(actor)` per crank | Precedent `RigRelocation.cs:125` [V] |

### 5.5 Preview and readers

- Wind row label: current and after (`7 of 12 -> 8`), "one action", "noisy". It is a row label because no `ProcessQuote` exists (S14 is out of scope; there is no process row).
- Strike-wheel reader (through `IDescribeState`, spec S5, so both `x` and the full document carry it): "Wound 7 of 12. One strike costs 2. It lights a torch or cooking coals when no flame is in reach. It makes no light of its own." [draft; voice gate]
- Bench reader: "Winds a strike-wheel, one wind to the action. Keeps no state." Plus the spec's installation line is not present (not generic-placed).
- Disabled rows (spec S3, C4) for a wheel at capacity: "The spring is fully wound." (`item-full`). When the actor stands beside the bench carrying a non-wound item nothing is listed (no clutter).

### 5.6 Blocked, failed, interrupted and saved states

| State | Code | Behaviour |
|---|---|---|
| Bench not adjacent | none | no rows, exactly as the forge today |
| Wheel full | `item-full` | disabled row with reason; nothing spent; menu stays open |
| Wheel stacked, equipped, foreign, or no longer carried | `item-not-single`, `item-not-carried` | row omitted or disabled; commit-time re-proof refuses with nothing changed |
| Bench or wheel replaced between listing and commit | `stale-selection`, `cell-mismatch` | `StationReceipt.Begin` re-proof refuses (spec C5); nothing spent |
| Another transaction holds the item | `in-progress` | refused; nothing spent |
| Actor blocked or dead | `actor-blocked` | refused (existing `IsActionBlocked` gate) |
| Light with free flame in reach | none | free flame used; **winds unspent** (counter-check) |
| Light with empty wheel | `striker-short` | torch refuses with the new alternative text; no winds spent; no flame |
| Torch wet, frozen or out of fuel | the torch's own codes | unchanged; the wheel cannot override them |
| Transaction rolls back | n/a | winds restored by `Debit`'s undo; no flame |
| Interrupted between cranks | n/a | each crank is its own paid action; winds already added stay. A safely stopped wheel stays safely stopped |
| Save, quit, reload | n/a | `Units` round-trips by reflection; no leak while away; fresh Continue shows the same winds |
| Old save with no striker | n/a | no Part, no change; no retrofit of cached Cinderhold (fresh-only, as the tray) |

Every refusal records a `Diag` record in a default-on category: `furniture/WindRefused`, `furniture/IgnitionRefused`, `craft/Wound`, `furniture/IgnitionProvided { source, kind, winds }` (spec R5).

### 5.7 Alternatives the player keeps (at least two genuinely different, plus leaving it alone)

| Alternative | What it costs | Why it stays valid |
|---|---|---|
| **Carry a second lit torch** (shipped; M1d0 verified a held spare relights a torch) | continuous fuel on the lit spare, a hand | The wheel does not replace it; it is cheaper in actions when the torch is idle |
| **Return to a fire** (campfire, cooking coals) | travel time | Costs no winds and no machine; the wheel never spends winds when a fire is in reach |
| **Buy torches** (35 drams for four, `MACHINE-TORCH-READOUT.md` [V]) | drams | Torch fuel stays the real light source; the wheel adds none |
| **Leave it alone** | nothing | The bench is Open and optional; no quest, repair or town service depends on it; nothing requires winding |
| Crack a glow-quartz for light | 1 mineral, 30 turns | A way to see, not to ignite; unchanged |

None of these is degraded: no torch, fuel or fire rule changes, and the shipped LampOil refuel loop works exactly as before.

### 5.8 Consequence outside its own menu (cross-system, verified)

1. **Shared ignition rule.** One provider serves the torch light action and the coals relight action through `IgnitionSources` (two shipped consumers, neither authored for the wheel). A coals relight then re-enables cooking (`CookingService` threshold 150, `MinimumFiniteCookingTemperature`) [R]. A lit torch also feeds the existing `TouchTorch` kindling verb (pays 1 fuel) [V], so the chain wheel -> torch -> kindle reaches tile fire rules the wheel never touches.
2. **Time and noise.** Each crank is a paid turn on the world clock (relapse timers, kitchen due ticks [R]) and a `Noise.Mark` stamp that eyeless hunters read [V]. The consequence is verified but narrow; stated, not inflated.
3. **Torch economy.** The wheel lets a player extinguish a lone torch to save fuel and relight it later (the torch drains only while lit [V `TorchLightPart.Tick`]). This is the verified real effect on fuel use.
4. **Custody.** Winds are finite per physical wheel; none are minted; a lost wheel loses its winds; a wheel is a singleton item with an ID, so duplication is a save-integrity matter pinned by tests.

### 5.9 Placement, operators, census fit

- **One site only: Cinderhold (`Overworld.6.6.0`), fresh towns only,** chosen for mechanism (it holds the Tinker's forge corner and three shipped fresh-only fixture precedents), not for need: the Cinderhold dossier rates the dynamo "poor... a pump drive could be hand/fuel work instead" (`settlement-cinderhold.md:221`) [V]; V1 supplies the consumer that makes the bench non-pointless there. Concord is the faction that most plausibly owns a "winding bench" (lore matrix: Concord O, surface villagers S) [R].
- **Path:** a third opt-in flag on `CinderholdCompositionPlan.Create` (after `includeRecoveryTray`, `includeReclamation`), one profile cell in the `ForgeWorkshop` row between the tray (+8,+3) and anvil (+10,+3) or beside the forge, chosen by a RED test against the room's occupant set [U exact cell]. The generic S7 post-builder is not used: that builder places exterior, non-solid forges and cannot place a room fixture (`ARCHITECTURE-SPEC.md` C3 step 5).
- **Supply of the strike-wheel:** one finite strike-wheel on the Cinderhold Weaponsmith's regional shelf, through the same path that places the M3b leather offcuts (`CinderholdCompositionBuilder.cs:209-223` [V grep], exact API [U]). **Not** a `WeaponsmithStock` or `TinkerStock` edit: that would advance the pipeline rng for every fresh town (spec determinism rule). Price is an owner decision (D4). If the wheel is lost or destroyed, V1 ends for that world; that limit is stated, not engineered around.
- **Retention:** bench stateless (nothing to preserve); the wheel is a carried item. A relapse regeneration of Cinderhold that re-rolls the shelf is the same accepted shop-stock re-roll as any shop, and Cinderhold is already retained when the shipped tray exists (`LegacyRetainedBlueprints`, spec C2) [R]. D5 begins with the spec's S2 Step-0 premise check for this town.
- **Census:** no change. `Pending`/`Present` classification of Cinderhold is unaffected; the dynamo is not an installation row. The other 24 places stay with whatever machine their dossiers choose; the dynamo is never the placement of record for a town.
- **Old worlds:** a visited Cinderhold gains nothing (fresh-only). An unvisited Cinderhold in an old world gains the bench on first visit, as the tray does.
- **Home/base hooks:** the store lives on the item, so any future home bench or portable crank reuses `WorkStorePart` unchanged; new Parts store no zone id or coordinates (S1 lint); `IStationAccessGuard` (S4) lets a home owner gate the bench later with no edit to it.

### 5.10 Canon landmines this must avoid (`lore.md` ids)

- **L10 / R5 mana-bar**: the wheel stores no charm, rite or conjured effect; `Kind` allow-list and forbidden-vocabulary lint make that a failing test; it never touches `GrimoireChargePart`. Everyday lamp-lighting "with a word" (`09_Magic.md:83`) is magic and stays available; text must never imply the wheel replaces or banks it.
- **L9 / R8 material collapse**: no Glow-Quartz, Tepuibone, Choir-Iron, Pale-Salt or Memory-Marble in the wheel, bench, recipe or reader; `CrackQuartz` stays a separate consumed mineral.
- **L2 / R6**: no source, text or consumer referencing Urqu, Sari-Pyrite, the Founding Plume or hearth patch.
- **L16 technology nouns**: no cell/charge/power/battery/generator/circuit in player strings, and no echo of the superseded Tinker "power cells, circuitry" conversation.
- **L4 Choir (burning is a crime)**: the wheel only reaches flame through `TorchLightPart.TryAct`; whatever grove-law consequences that path has today are unchanged. Whether lighting a torch inside Choir ground already triggers `GroveLaw.OnIgnite` is [U]; D4's parity test pins the current behaviour either way. No placement in any Choir place.
- **Catacomb light taboo / L8**: never placed in a catacomb village; "wasting light" is an offense (`08_MaterialCulture.md:77`).
- **L3 Tent-Right**: never placed at Wellmeet or the First Tent; no fee on water; none involved.
- **L17**: every authored string is a draft until a fresh-context blind pass.
- **Protected subjects**: the wheel and bench read no body, plaque or protected scene; they accept no feed.

### 5.11 Deferred pitch behaviours, and why

| Pitch behaviour | Why deferred | Trigger |
|---|---|---|
| Swappable **cells**, charging "equipment" generally | needs a socket/slot contract and a second consumer type; V1's spring is integral to its one consumer | second consumer approved |
| Fuel generator, environmental sources | no fuel-to-work contract; vents and wheels hold no capturable quantity (`machine-thermal.md`) | thermal exchanger V1 plus owner approval |
| Hand, fuel and environmental **tradeoffs** (portability, noise, output) | needs at least two generators; V1 has one | second generator |
| Portable crank | needs the install/portable state (spec S19) | home/base design |
| "Spend remaining charge on a scanner, a pump, or restoring" | needs two or more consumers; none exists | pump, resonator or cradle V1 |
| Elapsed or away unwinding, leaks | no away simulation by policy (spec C9) | owner decision |
| Terms, fees, a keeper | shipped forges stay Open (spec owner decision 7); S10 is critique-gated | S10 |
| Quote modifiers on winding | no process row, no S14 consumer | S14 |
| Light as mechanical content (so a lamp would matter) | separate design; would justify C2 | owner |

### 5.12 Tier and the purpose test (an acceptance gate that can fail)

**Tier: `wave3-needs-new-system`.** Actual-play acceptance (fresh world plus a cached-save control, F5, fresh Continue):

1. Buy/obtain the wheel; wind it at the bench (count actions); carry torch, LampOil and the wheel to a wild chunk away from any fire.
2. Extinguish and refuel the torch; relight with the wheel; confirm 2 winds spent, `FuelMass` unchanged by the wheel, and that with a hot fire in reach no winds are spent.
3. Relight cooking coals; cook; confirm.
4. Compare action counts with the three ordinary alternatives. **V1 is recorded as not purposeful and parked if** in real play a player neither needs nor prefers it over a second lit torch or a return to fire, or if it makes torches or LampOil pointless. Light and fuel economy are not to be undercut: if the wheel dominates the torch, shrink capacity or raise `StrikeCost`, never alter the torch.
5. Cached control: an older save with Cinderhold already visited shows no bench.

---

## 6. Slices and RED-first tests

Cadence: one cycle is one isolated-clone EditMode run (about 5 minutes). Every slice is RED-first (a compile error counts), has counters per positive assertion, an adversarial file where two or more taxonomy surfaces apply, diag pins, and a cold-eye pass with both angles. Spec S1 is a prerequisite for D2 onward.

### D0. Decisions (no code)
Record G0, G1 (option A recommended), G4 and the consumer choice in `Docs/MACHINE-DYNAMO.md`. No tests.

### D1. Ignition-provider seam (behaviour-preserving; 2 cycles; spec S1)
- `IgnitionFind_TruthTable_MatchesLegacyOracle`: copy the legacy `Find`/`Hot` into the test; at least 30 layouts (hot ground fire adjacent/at distance 2; cold; frozen; wet over/under 0.35; empty fuel; non-positive burn rate; equipped lit torch per failure clause; torch in pack not equipped; stack count 2; `exclude`; actor not current; zone mismatch). Counter: one clause flipped per row.
- `TorchLight_NoFire_RejectionTextUnchangedWhenNoProvider`; `CoalsRelight_Unchanged` (both existing call sites) over their existing suites untouched (torch 175-check set, campfire suites).
- `ProviderOfferedOnlyWhenNoFreeFlameInReach`; `ProviderPaymentNeverRunsWhenFreeFlameInReach` (counter).
- `ProviderPaymentRollsBackWithTransaction`; `ThrowingProviderIsIsolatedAndRefuses`; `NoSecondIgnitionScanLoop` (source pin).
- Diag pins: `furniture/IgnitionProvided`, `furniture/IgnitionRefused { code }`.
- Test providers only; no production provider yet.

### D2. Stored-work core (1-2 cycles; spec S1, D0 decision)
- `WorkStoreRoundTripsPublicFields`; `OldSaveWithoutPartLoadsWithoutStore`; hygiene lint passes; golden gains the new Parts as additions only.
- `CreditRefusesOverCapacityAndNeverClamps`; `DebitRefusesBelowZero`; `DebitAndCreditRollBackWithTransaction`; `UnitsAlwaysWithinZeroAndCapacity_PropertySweep`.
- `KindAllowListRejectsCharge_Mana_Glow_Urqu_Battery_Quartz_Pyrite_AtLoad`; `ProtectedFeedBlueprintsRejected` (GlowQuartz, SariPyrite, hearth patch, Founding Plume); `NoConversionCycle`.
- `NoPassiveGainAndNoLeakWhileAway`; `GrimoireChargePartUntouched` (no shared field or code path).
- Adversarial: null holder, wrong kind, `Capacity <= 0`, `Units` set above capacity in a loaded file, negative, int overflow.

### D3. Winding bench (2 cycles; spec S3, S4, S5, D2)
- `WindRowListedForEachCarriedWoundItemWithRoom`; `WindCreditsExactlyOneWindAndChargesOneAction`; `FullWheelShowsDisabledRowAndWindsNothing`; `WindNeverClampsOverCapacity`.
- `WindRefusesStackedEquippedForeignOrUncarriedItem`; `WindRefusedWhenBenchOrWheelReplacedBetweenListAndCommit` (receipt); `WindNotOfferedWithoutAdjacency` (counter); `TwoBenchesResolveDeterministically`.
- `WindMarksNoiseOnSuccessOnly` (counter: refusal marks none); `BenchHasNoStateAndNeedsNoKeeper`; `BenchWorksForActorWithoutTerms` (S10 absent).
- `WindRoutesThroughStationReceipt`; diag pins `craft/Wound`, `furniture/WindRefused`.
- Adversarial: duplicate IDs, a wheel in a hostile actor's pack, rapid double submit (one wind only), a throwing callback restores `Units`.

### D4. Strike-wheel (2 cycles; D1, D2, D3)
- `WheelLightsLoneTorchWithNoFlameInReach` (RED today: `no_fire`); `WheelSpendsExactlyStrikeCostOnLight`; `WheelNeverSpendsWhenFreeFlameInReach` (counter); `EmptyWheelRefusesWithAlternativeTextAndSpendsNothing`.
- `WheelCannotLightWetFrozenOrFuellessTorch` (the torch's gates win); `TorchFuelBurnsAsBeforeAfterWheelLight` (no free light, counter against fuel creation).
- `WheelRelightsCookingCoalsOnlyWithFuelAndBelow450`; `WheelNeverKindlesTilesOrActsAsTouchTorch`.
- `WheelNeverStacksSplitsOrMerges`; `WheelSaveRoundTripKeepsWinds`; `WheelReaderShowsWindsAndCost`; `WheelAndBenchTextsContainNoForbiddenVocabulary` (lint over all strings in the blueprints and Parts).
- `LightingTorchInsideChoirGroveBehavesAsBeforeWheel` (pins current grove-law behaviour, [U] settled by the first run).
- Adversarial: wheel in the pack of an NPC actor, torch in a stack of 2 (refused as today), simultaneous `RefuelTorch` and wheel use on one torch, zone change between list and commit.

### D5. Cinderhold fixture, supply and actual play (1 cycle + play; D3, D4)
- Step 0 premise: `CinderholdRegenerationPremise` per spec S2 (relapse regenerates a retained/unretained Cinderhold; decides whether the shelf re-roll duplicates the wheel).
- `FreshCinderholdHasOneWindingBenchAtDeterministicCell` (seeds 64, 704860946, 1729 plus a 24-seed sweep); `OtherCinderholdOwnersIdenticalWithFlagOff`; `FlagOffIsDefault_CachedZoneGainsNothing`; `BenchDoesNotBlockForgeAnvilOrWeaponsmithRoutes`; `RngUntouched` (shared pipeline); `ExactlyOneWheelOnTheShelfAndNotInTraderStockTables`; `GameSessionRoundTripThenGetZoneAddsNothing`.
- Actual play per section 5.12, with the evidence limits the project always states (glyph presentation only, graphics cancelled; Linux/Deck unexecuted; CUA-only evidence).

---

## 7. Spec gaps (where this machine needs something the spec lacks)

| Gap | What the spec has | What V1 needs | Resolution |
|---|---|---|---|
| G-D1 ignition seam | Nothing; `IgnitionSources` is an unlisted closed static rule | `IIgnitionProvider` with `CanProvide`/`Pay`; `Find` consults free flames first; two call sites gain one `Pay` line | slice D1, behaviour-preserving, oracle test |
| G-D2 stored work | Sketch only (S18, C10), canon-gated, "waits for first consumer" | `WorkStorePart`, `StoredWork.Credit/Debit` with transactional undo | slice D2 after owner decision G1; V1 is the first consumer the spec asked for |
| G-D3 hand-effort-to-store verb | Data-first sketch has a `hand -> wound` conversion row; the adopted spec has no such contract and `StoredWork.Feed` consumes an *item* | one hand-wind verb with a declared `OneAction` and a credit | implemented inside `WindingBenchPart` through S3/S4; no general conversion table |
| G-D4 station kind without a process row | `StationPart` (S13b) is designed for cold-work process benches | a station kind `winding` that offers a verb, not a process | `WindingBenchPart : IStationCapability` through S4's station query/receipt; no `StationPart` needed |
| G-D5 item-side readers | S5 reader contract | strike-wheel and bench lines | `IDescribeState`; no builder edit |
| G-D6 stack identity | none | wheel must never stack or split | singleton; RED test; no `StackPayloadIdentity` group |
| G-D7 supply path without rng | S7 builder places fixtures, not shelf stock | one deterministic shelf item at Cinderhold | reuse the M3b regional-shelf path; not a loot-table edit |
| G-D8 first-consumer decision | spec lists the dynamo under "Not now" with trigger "owner approves a kind" | the trigger | D0 |

Everything else in V1 uses spec contracts as written (S1, S3, S4, S5). No new event name, no new diag category.

---

## 8. Owner decisions and risks

1. **D1 stored-work canon.** Recommended option A (stored mechanical work only: "winds"). Option B (a reviewed natural electrical source) is not recommended.
2. **D2 build now or park.** Recommended: park until a second consumer exists. If built now, V1 is a modest ignition convenience with low stakes and a failable acceptance test.
3. **D3 consumer.** C1 (strike-wheel) or wait. C2 (light) is rejected until light has mechanical content.
4. **D4 supply and price** of the single shelf wheel; whether a destroyed wheel should be re-obtainable.
5. **D5 naming and voice** for "winding bench", "strike-wheel", "wind", plus the blind gate.
6. **D6 whether Cinderhold is the right site** versus no placement (mechanism proof only).
7. **Lift `CLAUDE.md` "No new runtime changes"** and the critique gate for stored-work policy.
8. **Superseded Tinker conversation** (`Tinker_1`, power cells, circuitry): fix or leave, separate from this machine.

Risks (ranked): (1) the machine's stakes are small enough that it may not earn its place (mitigated by the failing acceptance test); (2) D1 edits two shipped consumers' shared rule (mitigated by the oracle and the existing suites running unchanged); (3) torch/LampOil devaluation if the wheel dominates (mitigated by numbers; the torch is never altered); (4) canon drift into "charge" language (mitigated by lint); (5) old-save reach: none for existing worlds beyond new optional Parts; (6) isolated-clone cadence: D1 and D3 are the largest.

Not now: cells and swappable sockets, a fuel generator, environmental sources, a portable crank, any consumer other than C1, keepers and terms, any Glow-Quartz, Urqu or Sari-Pyrite source, any grimoire interaction, light as mechanical content, and retrofitting cached zones.

---

## 9. Verification record

Read this session [V]: `Docs/MACHINE-ROSTER-VISION.md`; `Docs/MACHINE-SYSTEMS-DESIGN.md`; `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`; `ARCHITECTURE-SPEC.md`; `TorchLightPart.cs`; `TorchRefuelPart.cs`; `FuelPart.cs`; `IgnitionSources.cs`; `CampfireWorkActions.cs` (header, menu, act); `EquipmentUtilityActions.cs` (header, `Verb`, `AddActions`, `TouchTorch`/`GroundCharge` branches); `Noise.cs`; `HuntsBySoundPart.cs`; `GrimoireChargePart.cs`; `StackPayloadIdentity.cs` (head); `HarvestablePart.cs` (fields); `LightMap.cs` (head); `FieldOfView.cs` (head); `OverworldZoneManager.cs:1372-1392`; `VillagePopulationBuilder.cs:100-150`; `CinderholdCompositionPlan.cs:28-70`; `CinderholdOilRecovery.cs`; `CinderholdCompositionBuilder.cs` (grep); `Objects.json` (TinkersForge, Torch, MillStone, SparkRoot, SunStriker, Tinker, PaleStalker); `LootTables.json` (`WeaponsmithStock`, `TinkerStock`); `MaterialProcesses.json`; `Tinkering/Recipes_V1.json` (10 recipes, two weapon builds, eight mods); `Conversations/FriendlyNPCs.json` (`Tinker_1`); `Lore/History/09_Magic.md:48-52,83,172`; `Lore/History/08_MaterialCulture.md:47,77,106-107`; `mw/lore.md`; `mw/arch.md`; dynamo rows of all 25 settlement dossiers (grep); sibling dossiers `machine-thermal.md`, `machine-pump.md`, `machine-capstan.md:195`, `machine-cradle.md:197`.

Greps run [V]: charge/battery/capacitor/power/dynamo/generator/crank/windup classes in `Assets/Scripts` (only `GrimoireChargePart` and unrelated); `Gameplay` for `GetBrightness`/`LightMap` (no Gameplay reader); `Gameplay` for hunger/thirst/stamina/fatigue (only `WellDrinkingService`); `Scripts`, `Content/Data`, `Conversations` for light-gated content (none); `Objects.json` for crossbow/sling/spring/clockwork/windlass/crank/grindstone/whetstone/flint/tinderbox (none relevant); `IgnitionSources.` call sites (three).

Not read [U], each settled by the named test: exact Cinderhold work-room cell for the bench; `CinderholdCompositionBuilder` regional-shelf API; whether torch lighting inside Choir ground already triggers `GroveLaw.OnIgnite`; whether a relapse regenerates Cinderhold's shelf; exact `JsonUtility` behaviour for the stored-work allow-list file; the glyph/colour pair uniqueness among machine blueprints; menu hotkey and disabled-row rendering (spec S3).

No test, build, Unity run or git write was performed.
