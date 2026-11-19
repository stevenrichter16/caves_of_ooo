# MACHINE V1 DOSSIER: Treatment bath (roster item 12)

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. All names, numbers and strings below are drafts for the implementing engineer and the owner; every authored string needs the blind voice gate (lore R10/L17).

Evidence tags: **[V]** I read the cited line in this pass. **[R]** taken from a prior report (`lore.md`, `settlement-*.md`, `enumerate.md`, `ARCHITECTURE-SPEC.md`) and not re-read. **[I]** inference or design. **[U]** unverified; the named RED test settles it. Derived arithmetic is marked [I] with its inputs; none of it was executed.

Inputs read: `design/ARCHITECTURE-SPEC.md` (all), `Docs/MACHINE-ROSTER-VISION.md` (item 12 and the themes list), `Docs/MACHINE-SYSTEMS-DESIGN.md` (all), `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `lore.md` (sections 0-3, M12 dossier, 5-8, 10-11), the 20 `settlement-*.md` "Treatment bath" rows, `enumerate.md`, and the code named below.

---

## 0. Verdict (read this first)

1. **Classification: `wave2-needs-shared-contracts`.** V1 needs architecture-spec slices S1 (save golden), S3 (declared action time and disabled rows), S4 (station contract), S6/S7 (registry and generic placement) before its own slices, plus one small shared contract the spec lacks (an equipped-item turn hook, section 8, gap G1). It does not need jobs, receivers, stored work, service terms or the process engine's multi-stack claims. It is not a first-wave machine: the first wave stays the existing forge/still placements the spec already schedules.
2. **Purpose: thin but real, and narrower than the pitch.** One hazard is verifiably dangerous and under-served: **acid spray from the Marlback Soursprayer that guards the wayside worksites near the start** (about 37 HP of damage-over-time per hit unresisted, about 18 with a +35 resistance; section 2.2). Every other pitched target is already served, impossible to improve, or has no model (section 2.1). The machine therefore earns a place only as a *small, optional, gear-only preparation station*, not as "a bath in every town".
3. **"Every town" is not honest for this machine.** Of 20 per-place ratings, two places have a verified local reason (section 9), most are "poor" with stated reasons, and four are forbidden by canon. The bath must not be used to satisfy the "every town" ratchet; other machines carry that census. Do not create `Pending` rows for it.
4. **The roster's pitch has two halves; only one can be built honestly now.** *Temporary protective treatment before a known destination* can ship (as a finite, gear-borne, bounded steep). *Removing a specific contamination / helping equipment shed spores* cannot: no item-level contamination or spore state exists anywhere in the code (section 1, F6). That half is deferred with a named prerequisite.
5. **Canon forces gear-only and a different name.** Every Pale Curation preservation method is a "bath" and the Bower Cleaner's enzyme bath resurfaces preserved bodies [R: `lore.md` M12; `08_MaterialCulture.md:86-94`; `05_BowerFolk.md:69`]. V1 acts only on a carried garment, is allow-listed by Part (so a person, body or specimen is structurally ineligible), and is called a **steeping trough** in the world. The word "bath" never appears in player text.
6. **Recommendation to the owner:** schedule the kernel after the S1-S7 spine (about 6 isolated cycles for the shared parts, about 7 for the bath itself, section 10), place it at one or two towns chosen by evidence, and accept that it is a modest, optional convenience that the existing apron and meal alternatives keep honest.

---

## 1. Verified ground truth, corrections to prior reports

| # | Fact | Evidence | Consequence |
|---|---|---|---|
| F1 | There is **no item-level contamination, corrosion or spore state** on equipment. Acid hurts *creatures*: `AcidicEffect` deals flat HP damage each turn to a target whose `MaterialPart` has the `Organic` tag and lowers its Combustibility; it never touches a garment. | `Effects/Concrete/AcidicEffect.cs:36-66` [V]; grep of `Gameplay` for corrod/contaminat/foul/rust finds only settlement-repair and skill text [V] | The pitch's "equipment shed clinging spores" and "remove a specific contamination" have nothing to act on. Deferred (section 13). |
| F2 | **Partial acid resistance cannot reduce a 1-point tick.** `ApplyResistanceFor` computes `(int)(amount*(100-resist)/100f)` and then forces a minimum of 1 unless `resist >= 100`. | `Combat/CombatSystem.cs:1318-1345` [V] | The Sodden `bog-mire` coat (`PerTurnDamage {1, Acid}`) is immune to any steep below 100: the roster's most obvious "acid mire" target gains nothing. The shipped apron text admits it ("one-point acid hits still hurt"). |
| F3 | The Sodden mire coat is a 1-point tick; the `acid` liquid is a 3-point tick; a pool contact coat is `min(Volume, Strength+Toughness)` units and dries by `Fluidity+Evaporativity` per turn (acid 20+15). | `Data/LiquidDefinitions/bog-mire.json`, `acid.json` [V]; `Materials/LiquidPoolPart.cs:123-139` [V]; `Materials/LiquidCoveredEffect.cs:210-243, 257-300` [V] | An acid-pool crossing costs roughly 1-2 ticks of 3: a few HP. Pools are a weak purpose. |
| F4 | **The real acid threat is the Marlback Soursprayer's `Corrosion_AcidSpray`**: 1d4 Acid direct, plus `AcidicEffect(0.8)`, which ticks `1+floor(corrosion*4)` HP each turn while corrosion falls 0.05 per turn. | `Skills/Corrosion_AcidSpray.cs:11-35` [V]; `AcidicEffect.cs:19-66` [V] | See the damage table in 2.2. [I] sum about 37 HP over about 16 turns against a 40-HP character. |
| F5 | Non-forge Spread worksite families (`FieldAlembic`, `TrappersStore`) take `MarlbackSoursprayer` as their ranged guard; the validator requires `Corrosion_AcidSpray` on it. | `World/Generation/Builders/SpreadExplorationWorksites.cs:46, 173-174` [V]; the Sill glade ring lists 11.9 field alembic and 11.11 trappers' store [R: `enumerate.md` section 5.5] | A start-adjacent acid hazard exists in fresh worlds. Whether a Soursprayer actually spawns at those two zones in a given seed is [U] (RED `SoursprayerPresentAtSillGladeWorksites`, actual play). **This contradicts the Sill dossier's "no environmental exposure at Sill"** [R: `settlement-sill.md:158`]. |
| F6 | Existing alternatives for acid: `AcidworkerApron` (Body slot, +50 acid, AV 1, DV -1, speed -5); `RoastedMushroom` meal (+20 `AcidResistance`, 100 of your turns, heals 3d4); avoidance of visible tile hazards. The apron is named only in the Peat Works locker stocking and scenarios; no trade list names it. | `Objects.json` AcidworkerApron [V]; `Effects/Concrete/PreparedMealEffect.cs:14-30` [V]; `Objects.json:43899` RoastedMushroom [V]; `Exploration/SecondExplorationSites.cs:111` [V] | The apron is one far, finite find (fresh worlds); the meal is weaker and needs cooking. Tag-driven random loot could also drop it [U]. |
| F7 | Fungal spores are a *creature* effect: `GasFungalSporesPart` -> `FungalInfectionEffect` (multi-stage, up to 3 dmg/turn, -3 Toughness). Cure: `BrinebuttonPaste` (`CureTonic`). Prevention: `FilterHood`, `GasMaskPart`, `GasImmunityPart`. The Bloom cure is the Choir ritual by design. | `Effects/Concrete/FungalInfectionEffect.cs:1-60` [V]; `Gameplay/Materials/GasMaskPart.cs`, `GasImmunityPart.cs` [V]; `Objects.json` BrinebuttonPaste [V]; Bloom path [R: `settlement-marrowstye.md:274`, `settlement-olderdeep.md:173`] | **Corrects** the Lampwell dossier's "no Choir/spore exposure mechanic exists" [R: `settlement-lampwell.md:138`]: it exists (Spivenor's dossier has it right). A bath must not touch infection or Bloom (L12). |
| F8 | The per-biome crop table already supplies one "prepare for the region" item per hazard: Sumpsieve (poison), Brinebutton (fungal), Sootroot (burning), Shadefan hood (Beating glare), stoneskin waxes, Claspbean/Margincress (bleeding). All are *apply in the field* or *wear*. | `Data/Farming/BiomeCrops.json` rows [V] | The unserved niche is **prevention that costs no field action and lasts longer than a wax**, not another cure. |
| F9 | A person-borne liquid coat is the wrong carrier for a protection. `LiquidCoveredEffect.OnStack` lets a larger incoming contact amount replace the dominant liquid id and reverse the old coat's stat mods. Entering an acid pool would overwrite a protective coat. | `LiquidCoveredEffect.cs:182-208` [V]; `LiquidPoolPart.cs:123-139` [V] | The protection must live on the **garment**, not in the single-slot body coat. This agrees with canon's "act on gear, not persons". |
| F10 | The shipped item-modification framework: `IItemEnhancement : Part` with `Apply/Remove/OnEquipped/OnUnequipped`, `ItemEnhancing.Apply(item, name, tier, wielder)` (slot cap `MAX_ENHANCEMENTS_PER_ITEM = 2`), `EnhancementFactory` auto-registration, dispatch from `EquipCommand`/`UnequipCommand`, reader lines in both Examine paths, and stack-identity coupling. Enhancement Parts live on the **item** and never see actor events. | `Items/IItemEnhancement.cs`, `Items/ItemEnhancing.cs:56-110`, `Items/ItemEnhancementDispatch.cs:1-95`, `Entities/ExaminablePart.cs:181-189`, `Entities/ExamineDocumentBuilder.cs:127`, `Items/StackPayloadIdentity.cs:44,125` [V] | The garment-borne state can reuse all of it. A **free reader line** on the treated garment follows from `GetEffectDescription()` with no S5 work. |
| F11 | Wearer-side ticking of equipped items is hand-wired per type: `InventoryPart.HandleEvent("EndTurn")` loops equipped items for `TorchLightPart` only; `ForwardEquippedGasMask` forwards two events for `GasMaskPart` only. There is no generic equipped-item hook. | `Inventory/InventoryPart.cs:612-640` [V] | Gap G1 in section 8. Three consumers (torch, mask, steep) justify one small interface. |
| F12 | Stacked-item cloning copies every public field of every Part (`Entity.CloneForStack`). | `Entities/Entity.cs:432-470` [V] | A steeped item must never be cloned: V1 admits only non-stacked garments (`StackCount == 1`, as forge infusion does). |
| F13 | `MineralPreparationPart` is the shipped precedent for "consume one carried reagent, attach a Part to a carried item, restore on failure": the reagent carries the verb, `ForgePart.IsNearForge` gates it, a `restore` delegate undoes the Part, `ModificationCount` and display name. It is permanent, offensive (Pale-Salt-edged weapon vs Undead) and weapon-only. | `Preparation/MineralPreparationPart.cs:7-52` [V]; `Items/EnhancementPaleSalt.cs` [V] | The bath is **not** a second infusion. Pale-Salt's shipped meaning is anti-Undead edge; using it for a defensive steep would collapse materials (L9, R8). |
| F14 | The shipped dressing bench is a *zone-pinned, hard-coded, staffed* service: `StopZoneID = "Overworld.15.7.0"`, positional `StationX/StationY/ZoneID/WorkerID`, a two-dram fee to the keeper. It treats *existing* poison/bleeding and prevents nothing. | `World/SoddenPreparationPart.cs:12-30`, `Items/SoddenDressingPart.cs:14-60` [V] | It must not be the base. Its fee-to-present-worker idiom is the precedent for later service terms (spec S10), not for V1. |
| F15 | `FiniteLiquidFilmPart` and tile coatings are exposure counters, not collectible volume; `BodyCoatingCleanupActions` already wicks oil/pitch/honey with one `PrismreedPith`. | `Materials/FiniteLiquidFilmPart.cs:1-60`, `Items/BodyCoatingCleanupActions.cs:1-40` [V] | No bath may mint volume from films. Ordinary cleanup of those three coats stays as is. |
| F16 | No machine foundation for "treatment" exists in code: the dossier for merchant camps found 0 hits for treatment-bath code, and my reads agree [R: `settlement-merchant-camps.md:116`]. The roster doc itself says "targeted treatment eligibility, effects, alternatives and persistence remain open" [V: `MACHINE-SYSTEMS-DESIGN.md:96`]. | | V1 is new content on shipped primitives, not a hook onto an existing machine. |

---

## 2. Purpose test (principle 1: establish the opportunity before the apparatus)

### 2.1 The pitched targets against shipped content

| Pitched target | Verdict | Why |
|---|---|---|
| "a corrosive secretion" (acid) | **Real, narrow** | Sprayer DoT is large (2.2); apron is one far find; meal is weak. Pools and mire are not a purpose (F2, F3). |
| "equipment shed clinging spores" | **No model** | F1, F7. A bath cannot act on a state that does not exist. |
| "remove a specific contamination" | **Mostly served, rest unmodelled** | Body coats: oil/pitch/honey have a pith wick (F15); mire/acid/brine coats dry in a few turns (F3). Poison/bleeding: dressing bench, pads, pulp. Fungal infection: paste. Bloom: Choir ritual, deliberately. |
| heat, cold, electricity | **Served** | `KilnfeltApron` (+50 heat), `ColdwardCloak`, `GroundwireScreen` (+50 electric), prepared meals, `BurnSalve` [V: Objects.json; R: `settlement-cinderhold.md:224`]. No new need demonstrated. |
| treating a living person | **Forbidden by canon** | Catcher/Casting fault line, Tent-Right sanctuary, Choir sealing crime [R: `lore.md` M12, L4, L5, L7]. Not V1, not V2. |

### 2.2 The one hazard with a real unmet need: acid spray

[I] Per tick damage at corrosion 0.80, 0.75 ... 0.05 is `1+floor(corr*4)` = 4,4,3,3,3,3,3,2,2,2,2,2,1,1,1,1 (16 ticks). Float drift may shift a tick or two [U]. Each tick passes through `ApplyResistanceFor` (F2). Hand-derived totals:

| Source of acid resistance | Resist | Total DoT from one spray hit | Pool tick (3) |
|---|---|---|---|
| none | 0 | about 37 | 3 |
| `RoastedMushroom` meal | +20 | about 25 | 2 |
| **steep (V1 default)** | **+35** | **about 18** | **1** |
| `AcidworkerApron` | +50 | about 18 | 1 |
| steep + apron | +85 | 16 (every tick floors at 1) | 1 |
| steep + apron + meal | +105 (stat Max clamps; at 100 acid is nullified) | 0 | 0 |

Reading: truncation puts the knee at +34, so +35 matches the apron's practical effect against 1d4-class acid for 100 worn turns, while the meal at +20 is visibly weaker. An unresisted hit can remove most of a 40-HP character's health [I]. The existing answers are a locker apron far away or a cooked meal; both are valid and both stay (section 5). The preparation role (apply once, in safety, no field action) is the gap.

Three honest weaknesses to carry into acceptance: (1) the stack of three sources reaches immunity, an emergent combination the owner may or may not want (section 12, D6); (2) the 1-point mire is untouched, so the bath must never be sold as "mire protection"; (3) frequency of Soursprayers is [U] (RED `SoursprayerPresentAtSillGladeWorksites`).

**Premise tests that can falsify this section before any code (TB0):** `AcidicEffectTicksOnPlayerBlueprint` (the player must carry the `Organic` tag or the DoT never fires, `AcidicEffect.cs:36-42`), `AcidSprayDotTotalTable` (re-derives the table above through the real `CombatSystem` path), `AcidResistanceStatMaxClamp`.

### 2.3 Is there a demand today?

Partially. Early fresh worlds put a Soursprayer-guarded alembic and store beside the start town [V code; U frequency]. Players can contest them by avoiding the spray, killing the caster (12 HP, sight radius 6, preferred range 3), eating a mushroom meal, or skipping the yard (a bypass is authored: spike entrance, fallen beam, crossing) [V: `SpreadExplorationWorksites.cs:49-58`]. A preparation that makes the contest cheaper is a legitimate additional approach, not a requirement. The machine must therefore never gate anything.

---

## 3. V1: the steeping trough (gear-borne, bounded, optional)

### 3.1 Name, voice, register (all drafts; blind gate pending)

- World name: **steeping trough** (blueprint `SteepingTrough`). Row text uses the verb **steep**. Lore's own recommendations for gear are "steep, dip, dressing, wash"; "bath" is avoided because it is the Curation/Bower preservation word [R: `lore.md` M12, section 8].
- Examine draft: "A long timber trough, tarred inside, with a lidded pot of clay slip at one end. Leather and cloth armor can be steeped here. It is not made for people, and it keeps nothing."
- Menu row draft: `steep <garment> (1 pitchpod resin, 1 fire clay; +35 acid resistance for 100 turns worn)`.
- Garment reader draft: "Steeped against acid: +35 acid resistance, 83 more turns of wear. One-point hits still hurt." (plain, no thesis sentence, no "free", no technology words; R10, L15, L16).
- Operator roles: **none required**. Shared use, like the oil strainer ("Its use is shared; it asks no fee" [R: `lore.md` section 2]). Optional explainers by existing roles: Apothecary/Tinker where present, WellKeeper, PeatCutter. A Concord fee idiom, Tent-Right gift idiom or Curation "withheld" idiom belongs to spec S10/S11 and is out of V1.

### 3.2 Parts, data, and what changes (additive saves only)

New in `CavesOfOoo.Core` (saved type name is assembly-qualified and permanent; spec F17):

1. `EnhancementSteeped : IItemEnhancement` (instance state on the **garment**). Public fields: `Kind` (string, row id, `"acid-pitch-clay"`), `TurnsLeft` (int), `TurnsTotal` (int), `Receipt` (string `"AcidResistance:35"`, exact contribution while worn; same convention as `EquippablePart.AppliedEquipBonuses`), `AppliedBonus` (bool, Lacquered's eager-flag guard). Hooks: `Applicable` (allow-list below), `OnEquipped` / `OnUnequipped` (apply/remove through `StatModifiers.Replace(..., createMissingElementalResistances: true)` and the saved receipt; never replay on load, `StatModifiers.cs:1-12` [V]), `OnWearerTurnEnd` (new virtual, gap G1) decrements `TurnsLeft` while worn and, at zero, reverses the receipt and removes itself via `ItemEnhancing.Remove`. `GetEffectDescription()` supplies the reader line.
2. `SteepTroughPart : Part, IStationCapability` (spec C5; `Kinds = {"steep"}`, `Reach = 1`, `Readiness = Strict`). It owns the verb rows and the transaction. It holds **no saved state** (no zone id, no coordinates, no operator id; S1 hygiene), so it relocates and installs cleanly later.
3. `SteepRecipeRegistry` (code class, loads `Assets/Resources/Content/Data/SteepRecipes.json`, modelled on `MaterialProcessRegistry`; per-row drop on invalid). One production row, one test row.

```json
{ "Version": 1, "Rows": [ {
  "Id": "acid-pitch-clay", "Kind": "acid",
  "Reagents": [ {"Blueprint":"PitchpodResin","Units":1}, {"Blueprint":"FireClay","Units":1} ],
  "Stat": "AcidResistance", "Amount": 35, "WearTurns": 100,
  "Eligible": { "RequiresPart": "ArmorPart", "AnyMaterialTag": ["Organic","Cloth"] },
  "Text": "turns acid"
} ] }
```

Row validation (each a distinct error, per spec R5): unknown reagent blueprint, `Units` outside 1..4, more than 3 reagents, `Amount` outside 1..60, `WearTurns` outside 10..500, `Stat` not one of the four elemental resistances, empty `AnyMaterialTag`, reagent list containing a forbidden material (section 7: `PaleSalt`, `ChoirIron`, `GlowQuartz`, `PaleSaltVein`, any Memory-Marble/Mute-Stone/Tepuibone item, any liquid id in the L19 list), duplicate id, output Part type unknown.

4. Blueprint `SteepingTrough` (`PhysicalObject`): `Render` (draft glyph `U`, colour `&c`, layer 5; unique glyph+colour among machine blueprints, S23 ratchet; `n` is already shared by four stations, `=` by two, spec risk 5), `Physics {Solid:false}` (non-solid like the forge, so spec S7 route and standing-cell checks are vacuous; a solid trough is a V2 choice), `Examinable`, `SteepTroughPart`, tag `Furniture`. **No `RepairablePart`, no `ContainerPart`, no `InventoryPart`** (it stores nothing; spec E07 therefore admits `Retention: None`).
5. Data: `Installations.json` rows (section 4) and `SteepRecipes.json`.

**No change** to `AcidicEffect`, `ApplyResistanceFor`, `LiquidCoveredEffect`, `PreparedMealEffect`, `AcidworkerApron`, `EquippablePart`, `MineralPreparationPart` or any saved field of an existing Part.

**Eligibility is an allow-list by Part, not a deny-list by name**: the target must carry `ArmorPart`, a `MaterialPart` tag in the row's list, `StackCount == 1`, no `Creature` tag, not `Takeable == false`, carried by the actor and **unworn**. So a corpse, `SaltCuredBody`, specimen, plaque, creature or worn garment can never qualify, whatever its material tags. Fewer than two enhancements already attached (shared cap `MAX_ENHANCEMENTS_PER_ITEM`, `ItemEnhancing.cs:24`).

### 3.3 Player verbs, inputs, outputs, loss, cost, time

| Verb | Where | Inputs (finite) | Output | Loss / waste | Cost |
|---|---|---|---|---|---|
| **Examine** the trough | `x`, F1, Look | none | text, the eligible-garment rule and the reagent list | none | free |
| **Steep** `<garment>` | world action menu on the adjacent trough; one row per eligible carried unworn garment, plus one disabled self-documenting row when none qualifies | 1 `PitchpodResin` + 1 `FireClay` (consumed through `TryConsumeOne`, stack-aware), the garment is **not** consumed | `EnhancementSteeped{Kind, 35, 100/100}` on that garment | reagents fully consumed whether or not acid is ever met; **no refund**; a refresh of a steep with turns left discards the remainder; no byproduct item exists | one declared `OneAction` (spec C4), no fuel, no fee, no water, no elapsed soak |
| Equip / unequip | ordinary | none | `OnEquipped` applies +35 `AcidResistance` by saved receipt; `OnUnequipped` removes it | none | existing equip rules |
| Wear | passive | wearer's own turns | `TurnsLeft` decrements per wearer `EndTurn` while worn; **paused while not worn** | at zero the steep removes itself and frees its enhancement slot; the garment is otherwise unchanged | none |
| Read | `x` / F1 on the garment | none | "Steeped against acid: ... turns left" via `IItemEnhancement.GetEffectDescription()` | none | free |
| Leave it alone | n/a | | | | nothing is lost |

Units are explicit: **1 resin + 1 clay -> +35 resistance points x 100 wearer turns on one garment.** World-map travel advances the world clock but fires no local `EndTurn`, so travel does not wear a steep down; this mirrors `PreparedMealEffect`'s "of your turns" wording [V: `PreparedMealEffect.cs:46`]. The policy is stated in the preview and the reader.

Refresh rule (decided, owner may flip): steeping a garment that already carries a steep **replaces** it (turns reset to full, one reagent set consumed, preview states "replaces 37 turns"); there is no stacking and no cap exploit. Two kinds on one garment is out of scope (V1 has one kind).

### 3.4 Preview and reader

- Each Steep row is its own preview: reagents, effect, duration, "paused while not worn", and "replaces N turns" when relevant. This is the spec's declared-time row (S3); no new preview surface.
- Disabled rows (S3 `AddDisabled`, S4 near-but-unready) name the cause with stable codes: `no-garment` ("nothing carried fits: leather or cloth armor, not worn, not stacked"), `short-resin`, `short-clay`, `worn` ("take it off first"), `stacked`, `slot-cap` ("this piece already holds two treatments"), `ineligible` (material), `no-station` (not adjacent, rows omitted as today). Each emits `furniture/SteepRefused {code, garment, reagentShort}`.
- Trough reader: static `Examinable` text (S5 not required). Garment reader: automatic through the enhancement reader (F10). Neither reader may state anything about persons, bodies or specimens beyond the one plain exclusion line.
- Honest-limits line on both: "Acid still does at least 1 damage per hit." (verifiable against F2).

### 3.5 Failed, blocked, interrupted, saved

| State | Behaviour |
|---|---|
| Reagent missing / garment ineligible | Disabled row with the code above; no input touched. |
| Transaction failure after payment begins | `InventoryTransaction` rollback restores reagent stacks, the `Parts` list, `ModificationCount` if used, and display name, using the `MineralPreparationPart` restore-delegate pattern [V: lines 26-38]. Result: nothing consumed, no Part left on the item. |
| Callback moves/removes the trough or garment before commit | `StationReceipt.Begin` re-proves identity and `Near` at `BeforeCommit` (spec C5); refusal with `StationRefused`. |
| Actor dies / garment destroyed mid-wear | The Part dies with the entity; nothing to refund; no other state referenced. |
| Garment dropped, sold, given, stolen | The state travels with the entity; paused while not worn; sale value unchanged (RED pins this). |
| Equipped by a companion or NPC | Hook receives the actual wearer; ticks on that wearer's turns; unequip pauses. |
| Save/load with a steep worn, unworn, expired, mid-lapse | Reflection save of public fields (`Kind`, `TurnsLeft`, `TurnsTotal`, `Receipt`, `AppliedBonus`); `OnAfterLoad` clamps `TurnsLeft` to `0..TurnsTotal` and sanitises strings; the receipt is **never replayed** (the wearer's saved `Bonus` already contains it). Old saves lack the Part and are unaffected. |
| Zone relapse / regeneration | The trough is stateless (`Retention: None`), regenerates at the same deterministic cell; garments live in packs, so nothing is lost. |
| Absent trough in an old, already-visited town | Honest limit: no retrofit (spec C3). |

### 3.6 Finite-accounting invariants (each is a RED test)

1. Exactly one resin and one clay are consumed per committed steep; a stack of N loses exactly 1 from each.
2. The garment is never consumed, cloned, split or merged; `StackPayloadIdentity` distinguishes steeped from plain.
3. Wearer `AcidResistance` returns to its exact prior value after unequip, expiry, death, save/load round trips and any order of equip/unequip (receipt discipline).
4. The sum of stat contributions from apron, meal and steep is the plain sum, with no double-apply and no loss of the others.
5. `TurnsLeft` never increases except by an explicit refresh, never goes negative, and never ticks while unworn or on a different actor's turn.
6. A failed or rolled-back steep leaves reagents and the item's `Parts` list byte-for-byte as before.
7. No steep changes `ArmorPart.AV`, `Commerce.Value`, weight, `Combustibility` or any other garment field.

### 3.7 Rejected on purpose

- **A body/person coat** via `LiquidCoveredEffect` with a new liquid row: cheapest to build, but a single-slot coat is overwritten by the very pool it should defend against (F9), and it crosses canon's person/bath line.
- **Permanent steeps** (a second infusion): duplicates forge enhancement and ignores the pitch's "temporary".
- **Exposure-charge expiry** (spend a charge when acid is actually resisted): more honest, but needs a combat-path forward (the GasMask precedent) and a hot-path hook. Kept as the V1.5 alternative (D2).
- **Soak time / jobs / a held garment**: needs S15/S16; V1 is one action.
- **Using Pale-Salt, Choir-Iron, brine, water or any L19 liquid** as the reagent: material collapse (L9) or lore-loaded liquids (L19); water is also a Tent-Right gift, never priced (L3).
- **Reusing `SoddenPreparationPart`** (F14).
- **A liquid reservoir "bath"** with finite brine/water units: wells are endless in the villages that would host it, so a unit meter would be manufactured scarcity (design doc principle 1).

---

## 4. Where the player finds it, and the placements

Placement uses the spec's generic post-builder (priority 4650, deterministic, fresh-only, optional, `Retention: None` because the trough is stateless). The row names a free `Alternative` as the schema demands (E09).

| Place | Rating | Basis | V1 action |
|---|---|---|---|
| **Sill** (start) | **conditional** | F5: Soursprayer guards at the glade-ring alembic and store, adjacent to the start. Sill's own dossier rates the bath "poor" because it did not see F5 [R: `settlement-sill.md:158`]. | Add the row only after actual play confirms a Soursprayer at 11.9/11.11 in two fresh seeds (RED `SoursprayerPresentAtSillGladeWorksites`, then `PlacementPlacesTroughAtSill`). Sill is "Present" in the census (legacy forge/still); an extra row there must not disturb `SillCourtComposition` exact-count tests [U]. |
| **Sumphold** | **plausible, limited** | Staging town for the Sodden circuit and its single locker apron; sells `FireClay`; real prevention gap (sumpsieve treats, does not prevent) [R: `settlement-sumphold.md:149,165`]. But the circuit's acid is mostly 1-point mire (F2): the honest value there is pools and any Soursprayer, not the mire. | Placeable; lower priority than Sill. `SumpholdFurnishingBuilder` (4600) runs first and can return false on drift; the generic builder never does. |
| Salt-Vault | strong by canon, **no mechanical hook** | Its "strong" rests on the missing infusion station (P1) and "salt is fungus-toxic" [R: `settlement-salt-vault.md:194`]. V1 neither infuses nor uses Pale-Salt (F13), and no spore/decay state exists (F1). | **Not placed.** Revisit when a gear-level spore or decay exposure exists. |
| Cinderhold | plausible | `KilnfeltApron` is the ordinary heat alternative; no local acid [R: `settlement-cinderhold.md:224`] | Not V1; candidate for a heat kind later. |
| Posy | plausible, object-only | Bower Cleaner's craft is the canonical bath of *bodies*; a gear-only trough needs explicit Bower permission and competes with the Elder's finite resin [R: `settlement-posy.md:224-226`] | Not V1. |
| Marrowstye | plausible, restricted | Gear only, never near the slab or disused wing [R: `settlement-marrowstye.md:274`] | Not V1. |
| Tally, Last Counter, Tine, Gantry, Slip, Wellmeet, First Tent, Quiet's Door, Drowned Ledger, Quillhold, Morrowfast | poor / forbidden variants | No local acid, no demonstrated exposure, canon fences (L3, L5, L7, R7) | Not placed. No `Pending` row. |
| Deepest Cathedral, Olderdeep, Spivenor, Lampwell | forbidden / conditional | Choir sealing crime, plume, patch, spore-wedded consent [R] | Exempt for the trough; catacomb enzyme-spray is its own canon idiom (V2+). |

**Discovery** is by glyph, Look and `x`, plus the self-documenting disabled row when adjacent and carrying nothing eligible. Mention by a resident is spec S24 (voice-gated) and is not required. The reagent loop in the start region is [I] plausible without new content: Spread crops `Pitchpod` (resin) and `Marlroot` (clod, then "Prepare" to fire clay) are shipped and farmable [V: `BiomeCrops.json`; `Objects.json` MarlrootClod], and the Sill glade ring has a seed-keeper plot (11.8) [R: `enumerate.md`]; seed stock identity is [U].

Old worlds: towns already visited keep their old layout; an unvisited town receives the trough on first visit; nothing is retrofitted (spec C3).

---

## 5. Alternatives the player keeps

| Approach | Cost / limit | Why it stays valid |
|---|---|---|
| **Leave it alone** | nothing | Pools are visible tile hazards and `TerrainNavigationWeight` already prices them; the Soursprayer yards have authored bypasses (spike entrance, beam, crossing). No lock or quest depends on acid. |
| **Wear `AcidworkerApron`** | one far locker find (finite), Body slot, DV -1, speed -5, +50 | Free of reagents and time limits, permanent. Better than a steep against heavy acid; worse for armor and speed. |
| **Eat `RoastedMushroom`** | cook it; +20 for 100 turns; heals 3d4 | No slot, no trough; weaker; replaces any other meal effect. |
| **Kill or outrange the caster** | 12 HP, sight 6, spray range 4, cooldown 10 [V] | Pure skill; no preparation. |
| **Keep the reagents** | | `FireClay` repairs the rig (2 clay), wells and batch pans; `PitchpodResin` feeds the Posy Elder and burning-coating brews. These are real competing uses. |

Genuinely different: avoid, wear a slotted permanent item, consume a timed meal, remove the source, spend the same inputs elsewhere. The steep adds one more with a distinct cost shape (reagents + one enhancement slot + a wear clock, no field action).

---

## 6. Consequences outside its own menu

1. **Combat**: acid DoT drops from about 37 to about 18 HP per spray hit through the same `CombatSystem.ApplyResistances` path the apron, the meal and creature stats already use (stats `CaveSlime +50`, `Scorpion -50` are untouched). This changes whether the start-adjacent alembic/store yards are comfortable to contest. Measured by a self-auditing matrix (section 11, T7).
2. **Economy and custody**: consumes `FireClay` and `PitchpodResin`, competing with rig repair and the Posy request. No money is minted or destroyed; no sale-value change (invariant 7). `FireClay` is sold at Sumphold [R] and craftable from `MarlrootClod` [V].
3. **Enhancement slots**: occupies one of two slots on the garment while present, competing with `EnhancementLacquered` and forge infusion; the slot frees on expiry.
4. **Stat stacking**: sums with apron and meal; three sources reach the +100 clamp (D6).
5. **Persistence**: the state rides the garment through save, drop, trade, companion use and death of the wearer.
6. **AI**: unchanged. `TerrainNavigationWeight` reads `AcidResistance` for creatures; the player's stat does not feed it, so no companion pathing change [V: `AI/TerrainNavigationWeight.cs:78,95`] (RED confirms no regression).

---

## 7. Canon: names, operators, landmines

Landmines (ids from `lore.md` section 7) and the V1 handling:

| Id | Risk | V1 handling |
|---|---|---|
| L5, L7, R7, L4 (sealing) | A bath reachable for persons, bodies or specimens (Catcher/Casting, Pale Curation crimes, Choir sealing, Tent-Right sanctuary) | Allow-list by Part; no Creature, no corpse, no worn item; "steeping trough", never "bath"; tests `CorpseNeverEligible`, `SaltCuredBodyNeverEligible`, `WornGarmentNeverEligible`, `ReaderNeverMentionsPersons` |
| L12 | A bath curing Bloom or fungal infection | Never touches `BloomedEffect`, `FungalInfectionEffect`, `CureTonic`; test `SteepNeverRemovesEffects` (apply with bloom/infection present, nothing removed) |
| L19, L9, R8 | Lore-loaded liquids or collapsed mineral identities as reagents | Recipe validator rejects `memory-bath`, `held-breath-lacquer`, `felling-counter-resin`, `bower-resin-amber`, `veined-pulse-mycelium`, `tepuibone-slurry`, `choir-*`, and `PaleSalt`, `ChoirIron`, `GlowQuartz`, Memory-Marble, Mute-Stone, Tepuibone items; reagents are botanical resin and clay |
| L3 | Pricing or metering water; guest-right | No water is used; no fee; no Tent-Right placement |
| L1, R1-R4, L11 | Reading or explaining protected subjects, attributing intent to Urqu | Text is physical only; no "repels Urqu", no Bloom gauge |
| L2, L10, R5 | Power/charm storage | The steep is a passive, temporary resistance with a clock; no stored work, no charge |
| L15, L16, L17, R10 | Voice | Drafts avoid "free", technology words and thesis sentences; blind gate required before ship |
| L8, L7 | Ancestral bone/plaque, resin as Resin-Cast supply | Not used; `PitchpodResin` is botanical pitch (shipped, `lore.md` section 3) and is not `bower-resin-amber` |

Operators and roles (existing blueprints only): none are required; optional explainers Apothecary, Tinker, WellKeeper, PeatCutter [R: `lore.md` section 11 item 6]. Roles canon needs but code lacks (Drosera-Keeper, Cleaner) are not introduced.

---

## 8. Fit with the architecture spec, and what the spec lacks

Uses as written: C1 (new Parts enter the golden; hygiene: `CavesOfOoo.Core`, public default constructor, no `*ZoneID`/`Station*` fields), C2 (marker; `Retention: None`), C3 (registry, post-builder, census), C4 (declared `OneAction`, disabled rows), C5 (`IStationCapability`, `StationQuery`/`StationReceipt.Begin`), diag vocabulary (`worldgen/Installation*`, `furniture/StationRefused`).

Gaps the spec does not cover (each is small and has its own RED set):

| Gap | Need | Proposed minimal contract | Why not reuse |
|---|---|---|---|
| **G1 equipped-item turn hook** | A garment must tick on its wearer's turns | `IItemEnhancement.OnWearerTurnEnd(Entity wearer, Entity item, Zone zone)` (virtual, default no-op) + `ItemEnhancementDispatch.DispatchOnWearerTurnEnd` called from `InventoryPart.HandleEvent("EndTurn")` beside the torch loop, using a pooled scratch list and a de-duplicated equipped set (an item can occupy two slots, `InventoryPart.cs:624-633` [V]) | Torch and mask forwarding are hand-wired per type (F11); a third consumer justifies one hook. R1 two-consumer: first use is the steep, second is a test-assembly enhancement; the existing torch loop is the characterization oracle and is **not** migrated. |
| **G2 in-place item modification receipt** | Attach a Part to a carried item with rollback | A small static `ItemModificationReceipt` extracted from `MineralPreparationPart`'s restore pattern (Parts list, `ModificationCount`, display name, pack snapshot) | The spec's process engine only maps items to items. Extraction is behaviour-preserving; `MineralPreparationPart` keeps its own copy until a parity test proves equivalence. |
| **G3 second per-registry table** | `SteepRecipes.json` is the second new definition table | Copy the `MaterialProcessRegistry` shape again for V1; the spec's own trigger ("extract `DefinitionTable<,>` on the second table") **fires here** | Owner decision D8: extract now, or accept one more copy. |
| **G4 protection capability** | Canon's "ProtectedSubject" rule (lore section 10) | V1 uses the Part allow-list of 3.2; no new framework | Adding a framework would fail the two-consumer rule. |

Not needed: S5 (the trough reader is static and the garment reader is free via F10), S10 terms, S12 claims (two distinct reagent blueprints, one unit each), S13/S14 (no `MaterialProcessRegistry` row), S15/S16 (instant action), S17/S18 (no receiver or stored work).

Home/base hooks (spec section 9): no zone ids or coordinates on any new Part; the trough is stateless and `Installable` later (Origin `Player`, retention by the marker); state lives on carried garments, so a base trough or a carried "steeping kit" needs no migration; station identity is by entity id and kind token; `ServiceTermsPart.Access = "Owner"` stays fail-closed.

---

## 9. Census and "every town"

The bath's honest coverage is a **fit table, not a ratchet**: Installed at one or two places, Exempt or NotFit elsewhere with a reason. It must not appear in the `Pending` count (spec section 3 of C3) because most places have no purpose for it; marking them `Pending` would promise machines that canon or the world do not support. The 20-place classification table is in sections 4 and 7; places with canon fences (First Tent, Quiet's Door, catacomb villages, Cathedral) are already `Exempt` in the spec's data and need no extra row.

Per-place ratings were read from the 20 settlement dossiers [R]. Two of their claims are corrected here: Sill (F5) and Lampwell's "no spore exposure" (F7).

---

## 10. Slices (prefix TB; each RED-first; sized for the ~5-minute isolated cycle)

Shared-spine prerequisites from the spec (not repeated): S1, S2, S3, S4, S6, S7. TB slices below run after S4 (station contract) and S3 (disabled rows); TB5 needs S6/S7.

| Slice | Value | Cycles | Depends |
|---|---|---|---|
| **TB0** premise tests (test-only) | falsify section 2 before code: player Organic, acid DoT table, stat clamp, `CloneForStack` hazard, slot-cap semantics | 1 | none |
| **TB1** equipped-item turn hook (G1) | `OnWearerTurnEnd` dispatch; torch/mask behaviour pinned unchanged | 1 | S1 |
| **TB2** `EnhancementSteeped` + receipts + registry rows | save-stable, exact-receipt gear state with expiry and reader line | 2 | TB1, S1 |
| **TB3** trough Part, verb rows, transaction (G2) | one-action steep with rollback, disabled reasons | 2 | S3, S4, TB2 |
| **TB4** blueprint, glyph, reader text, diag pins | placeable, readable station; unique glyph/colour | 1 | TB3 |
| **TB5** placement rows (data) + census | deterministic fresh-only placement at the evidence-chosen town(s) | 1 + play | S6, S7, TB4 |
| **TB6** self-auditing matrix `TreatmentSteepBench` | one-launch audit of the damage table (CLAUDE.md "deterministic self-auditing" rules: control row, loud skips, runId stamp) | 1 + play | TB3 |
| **TB7** actual-play acceptance | fresh world and cached-save control (below) | play | all |

Actual-play acceptance (limits stated in every receipt: glyph presentation only, graphics cancelled; CUA-only images; Linux/Deck unexecuted): fresh seeds 64 and 704860946; walk to the placed town; Look and `x`; carry eligible leather armor plus resin and clay; steep; read the garment; equip; take the acid spray from a Soursprayer (or the bench scenario); read the reduced damage; unequip and re-equip; F5; fresh Continue; the steep and the trough persist. Cached-save control: an older save with the town already visited shows no trough and unchanged behaviour.

---

## 11. RED-first test names (C = counter-check, A = adversarial surface)

**TB0 premise**
- `AcidicEffectTicksOnPlayerBlueprint` (RED if player lacks `Organic`; settles F4)
- `AcidSprayDotTotalTable_MatchesDocumentedValues` (0, +20, +35, +50, +85, +105)
- `AcidResistanceStatClampAtMax_NoOverflow` (A)
- `CloneForStackCopiesEnhancementState_SoSteepedGarmentsMustNotStack` (A)
- `PartialResistanceNeverReducesOnePointTick` (pins F2; C: resist 100 does)

**TB1 hook**
- `WearerTurnEndFiresOncePerEquippedItem`; `WearerTurnEndSkipsUnequippedItems` (C); `ItemInTwoSlotsTicksOnce` (A)
- `HookMayRemoveItsOwnEnhancementWithoutSkipping` (A, Bug #3 class); `TorchTickBehaviourUnchanged`; `GasMaskForwardingUnchanged`
- `NoAllocationWhenNoEnhancementOverrides` (design property; allocation harness is known-inconclusive, so asserted by source pin)

**TB2 state**
- `SteepedAppliesExactReceiptOnEquip`; `UnequipRestoresExactPriorResistance`; `EquipUnequipEquipIsIdempotent` (A); `SaveLoadWhileWornDoesNotReplayReceipt`; `SaveLoadWhileUnwornKeepsTurns`
- `TurnsDecrementOnlyOnWearerTurnsWhileWorn`; `TurnsPausedWhenUnworn` (C); `ExpiryRemovesPartAndReversesReceipt`; `ExpiryFreesEnhancementSlot`
- `StackedWithApronAndMealIsPlainSum`; `RoundTripPublicFieldsViaSaveGraph`; `OldSaveWithoutPartLoadsUnchanged`; `HygieneLintPasses` (S1)
- `GetEffectDescriptionShowsTurnsLeft`; reader parity in both Examine paths (`ExaminablePart`, `ExamineDocumentBuilder`)
- A: wearer dies while steeped; garment destroyed worn; `TurnsLeft` corrupted negative or above total (clamped on load); empty/whitespace `Kind`; unknown `Kind` on load (inert, never throws)

**TB3 verb**
- `SteepConsumesExactlyOneResinAndOneClay`; `SteepNeverConsumesTheGarment`; `StackOfReagentsLosesOneEach` (A)
- `SteepRefusedWhenGarmentWorn`, `...Stacked`, `...IneligibleMaterial`, `...NoArmorPart`, `...AtSlotCap`, `...ReagentShort` each with a passing counter
- `SteepRollsBackReagentsAndPartsOnFailure`; `CallbackMovingTroughRefuses`; `CallbackSwappingGarmentRefuses` (A)
- `RefreshReplacesRemainderAndConsumesOnce`; `DoubleSubmitConsumesOnce` (A); `DeclaredOneActionChargesOneTurn`; `DisabledRowSpendsNoTurn`
- Canon: `CorpseNeverEligible`; `SaltCuredBodyNeverEligible`; `CreatureNeverEligible`; `PaleSaltRecipeRejectedAtLoad`; `LoreLoadedLiquidRecipeRejectedAtLoad`; `SteepNeverRemovesBloomOrInfection` (C: a real cure still works)
- `SteepDoesNotChangeAvValueOrWeight` (invariant 7)

**TB4-TB5 presentation and placement**
- `TroughBlueprintIsNonSolidAndStateless` (no Repairable/Container/Inventory; no `*ZoneID` field)
- `GlyphColourPairUniqueAmongMachines`; `ExamineTextNeverMentionsPersons`; `NoBathWordInPlayerText`
- `StationQueryResolvesTroughForKindSteep`; `TroughNotAStationForOtherKinds` (C)
- Per-row placement (spec S7 matrix): deterministic cell for seeds 64, 704860946, 1729 and a 24-seed sweep; rng untouched; flood/route preserved; `OtherOwnersIdenticalWithRowRemoved`; replay and retry place once; cached zone never retrofitted; relapse regenerates at the same cell
- `SoursprayerPresentAtSillGladeWorksites` (precondition for the Sill row)

**TB6 matrix (self-auditing)** `TreatmentSteepBench`: control row (no protection), meal, apron, steep, steep+apron, steep+meal+apron; each cell emits one `craft` diag record with `runId`; subject precondition failures emit `...Skipped`, never a neutral number.

**Diag pins**: `craft/Steeped {garment, kind, amount, turns, reagents}`, `craft/SteepLapsed {garment, cause}`, `furniture/SteepRefused {code}`, `worldgen/InstallationPlaced`.

---

## 12. Owner decisions and risks

Decisions:
- **D1** Accept "thin purpose, wave 2" and the one-hazard scope; or park until a spore/decay exposure exists.
- **D2** Expiry policy: wearer-turn clock (default, no combat change) versus exposure-charge (needs a combat forward).
- **D3** Count one enhancement slot against the garment (default) versus a separate non-slot Part.
- **D4** Reagent identity: default 1 `PitchpodResin` + 1 `FireClay` (botanical, local to the Spread, with real competing uses). Alternative: `ShalebeanWax` (Beating farm yield; competes with its own 6-turn stoneskin use). Pick by an availability audit (RED: `ReagentsObtainableWithinTwoChunksOfEachPlacement`).
- **D5** Sill placement is conditional on actual-play evidence; Sumphold lower priority.
- **D6** Three-source acid immunity at +100: allow (default; emergent, earned by three slots/effects) or cap the steep's contribution.
- **D7** Naming ("steeping trough") and every authored string through the blind voice gate.
- **D8** Extract `DefinitionTable<,>` now (the spec's second-table trigger) or copy once more.
- **D9** Numbers (+35, 100 turns, 1+1 reagents) are tuning candidates; acceptance asserts the derived damage table, not the raw numbers.

Risks (ranked): (1) purpose is modest; the owner may judge the cost not worth it. (2) G1 touches `InventoryPart`, a per-turn path shared with torches. (3) First-cycle [U] items: the player's `Organic` tag, float drift in the DoT table, Soursprayer frequency. (4) Enhancement-slot coupling surprises (`LegendaryLairEncounters`, `EquipmentComparisonService`, `StackPayloadIdentity`) [U]. (5) Placement omissions in crowded towns (omitted with diag). (6) Voice and naming debt (L17). (7) Presentation debt: glyph only; graphics cancelled.

---

## 13. Deferred pitch behaviours and why

| Pitch behaviour | Why deferred | Prerequisite |
|---|---|---|
| remove a specific contamination | no gear-level contamination state (F1); body coats already dry or have ordinary cleanup (F3, F15) | a saved item/body contamination model with its own exposure source |
| equipment sheds clinging spores | spore exposure is a creature gas effect with an existing cure and prevention (F7) | a gear-borne spore state; must not touch Bloom (L12) |
| treat a living person | canon forbids a machine path (R7, L4, L5, L7) | a separate consent-gated service with its own canon design |
| heat, cold, electric kinds | existing finished garments and meals serve them (2.1) | a demonstrated unmet need per kind; rows are data, no code change |
| soak time / held garment | needs jobs and reconciliation (S15/S16) | first timed machine |
| several garments per visit | needs multi-stack claims (S12) | S12 |
| metal armor, hide, other materials | needs a different reagent and material model | per-row eligibility already supports it as data |
| terms, fees, operator presence | S10 critique gate | S10 |
| installed or player-built trough, carried steeping kit | home/base design | S19; no migration needed (state on garments) |
| NPC use / production | S20 | S20 |
| "return to the bath after every outing" | never required: no upkeep, clean lapse; ordinary healing and cures are untouched | n/a |

---

## 14. Honesty bounds

Can state (script-observable once built): the damage table through the real `CombatSystem` path, receipt exactness, save/load round trips, placement determinism, refusal codes, diag emissions.

Cannot state from this design: whether the feature feels worthwhile, whether Soursprayers appear where F5 implies, the glyph's distinctness on screen, any packaged-performance figure, Linux/Deck behaviour. The 37/18 damage totals are hand-derived from code reading and must be replaced by the TB0/TB6 measurements. This dossier did not read `Docs/Lore` (alternate world) and did not run any tool that writes to the project.
