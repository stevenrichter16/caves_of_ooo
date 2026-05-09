# Save/Load Round-Trip Audit Across Parts

> **Living plan + findings document.** Updated as the audit progresses.
> See `ADVERSARIAL_TESTING.md` (root) for the methodology this audit
> follows — particularly **Strategy B (existing features)** which
> calls out save/load reflection paths as the **highest bug-yield
> surface for legacy code**.

---

## Status banner

| Field | Value |
|---|---|
| **Current sub-milestone** | SL.2 — Tier-3 simple-Part baseline ✅ COMPLETE |
| **Last updated** | 2026-05-09 |
| **Total tests added** | 14 (`Tier3SimplePartRoundTripTests.cs`) |
| **Total Part types audited** | 9 / ~62 (Commerce, Physics*, Render, Equippable, Stacker, Examinable, Lifespan, Fuel, Material) |
| **Real bugs found** | 0 |
| **Real bugs fixed** | 0 |
| **Contracts pinned** | 4 — see Findings log |
| **Latest commit** | TBD on SL.2 merge |

\* `PhysicsPart` simple fields verified; `InInventory` + `Equipped` Entity references deferred to SL.3.

---

## Goal

Pin save/load round-trip invariants for every `Part` subclass in the
codebase. Catch:
- **🔴 Hard bugs** — fields silently dropped via reflection
  serialization (private fields, unsupported types, object references
  that don't re-resolve)
- **🟡 Latent contracts** — Parts whose serialization works today
  but isn't pinned by a test, leaving future contributors free to
  break it
- **⚪ Unverified surfaces** — explicit save handlers in `SaveSystem.cs`
  that have no test pinning their round-trip fidelity

The previous rental-system audit (`RentalPartFields_RoundTrip` test)
proved the catch-all `WritePublicFields` reflection path works for
*simple* types (`int`, `string`). This audit verifies the path works
across the full Part-type matrix — and finds the cases where it
doesn't.

---

## Why this matters (anchored to ADVERSARIAL_TESTING.md)

Per `ADVERSARIAL_TESTING.md` §"Strategies for existing features":

> **Reflection-based serialization is the highest-bug-yield surface
> for existing code.** When `Part` types lack explicit save handlers,
> the catch-all reflection path silently breaks for object-reference
> fields, generic collections, and non-default-constructible types.
> **Always probe save/load on existing features.**

A save/load corruption bug is the worst kind because:
1. It bites *days after* the bad save was created (player has
   already invested time into the save)
2. It's silent (no exception, no error log; just lost state)
3. It compounds (the corrupt save accumulates further mutations
   on top of the corrupted base)

This audit proactively probes the full Part-type matrix to surface
any of those bugs *before* they ship in saves.

---

## Pre-impl verification sweep (per CLAUDE.md §1.2)

Read carried out before writing this plan:

### Save-system architecture

`SaveSystem.cs:1105-1130` — `SavePart`/`LoadPart` dispatch:

```csharp
private static void SavePart(Part part, SaveWriter writer)
{
    writer.WriteString(GetTypeName(part.GetType()));
    part.OnBeforeSave(writer);

    if      (part is StatusEffectsPart status)        SaveStatusEffectsPart(...)
    else if (part is BitLockerPart bits)              SaveBitLockerPart(...)
    else if (part is InventoryPart inventory)         SaveInventoryPart(...)
    else if (part is ActivatedAbilitiesPart abilities) SaveActivatedAbilitiesPart(...)
    else if (part is Body body)                       SaveBody(...)
    else if (part is BrainPart brain)                 SaveBrainPart(...)
    else if (part is MutationsPart mutations)         SaveMutationsPart(...)
    else if (part is ISaveSerializable serializable)  serializable.Save(...)
    else                                              WritePublicFields(part, writer);

    part.OnAfterSave(writer);
}
```

### `WritePublicFields` reflection contract

`SaveSystem.cs:GetSerializablePublicFields` uses:
```csharp
BindingFlags.Instance | BindingFlags.Public
```

**Implication: ONLY public instance fields round-trip.** Private,
internal, protected, or static fields are silently skipped. This is
the highest bug-yield surface because:
- A Part might use `[NonSerialized]` annotation thinking it's a
  marker; the reflection ignores the annotation and just serializes
  every public field.
- A Part might rely on private state (e.g., `_cachedSpec`,
  `_lazyInit`) that never round-trips.
- A Part might have a public *property* with a private backing
  field — properties are NOT serialized, but the backing field is
  (`<PropName>k__BackingField`).

### `WriteFieldValue` type matrix

Supported field types (from `SaveSystem.cs:WriteFieldValue`):
- ✅ `int`, `long`, `float`, `double`, `bool`, `char`, `string`
- ✅ `Guid`
- ✅ `Nullable<T>`
- ✅ Enums (cast to int)
- ✅ `Entity` (via `WriteEntityReference` — stores ID, resolves
  on load via `EntityFactory`)
- ✅ Arrays
- ✅ `IList<T>` (List<T>)
- ✅ `HashSet<T>` — **CORRECTED in SL.2.** The audit plan flagged this
  as "likely unsupported" but `SaveSystem.cs:1635, 1685, 1750` does
  support it. See Finding #1 below.
- ⚠️ `IDictionary<K,V>` — need to verify
- ❌ Tuples — likely unsupported
- ❌ Custom struct types — depends
- ❌ Custom class types (non-Entity, non-Part) — depends

### Effect serialization (a specialized path)

`SaveSystem.cs:1178-1185` — `SaveEffect`:
```csharp
writer.WriteString(GetTypeName(effect.GetType()));
effect.OnBeforeSave(writer);
writer.Write(effect.Duration);
WritePublicFields(effect, writer, field =>
    field.Name != nameof(Effect.Owner) && field.Name != nameof(Effect.Duration));
effect.OnAfterSave(writer);
```

Effects use reflection too, with `Owner` + `Duration` excluded
(handled separately). Same bug-class surface as `Part` reflection —
plus the fact that effect ctors can have parameters
(`SmolderingEffect`, `FrozenEffect`, `PoisonedEffect`), so load
uses `FormatterServices` to bypass constructors.

### Part inventory

**Direct `: Part` subclasses (50):**
```
AcidicDaggerDemoProbePart, ActivatedAbilitiesPart*, ArmorPart,
BitLockerPart*, Body*, BrainPart*, CampfirePart, CommercePart,
ContainerPart, ConversationPart, CorpsePart, CureTonicPart,
DamageFlashPart, ElementalDemoProbePart, EquippablePart,
ExaminablePart, FlamingSwordDemoProbePart, FoodPart, FuelPart,
GivesRepPart, GlowmawAmbushPart, GrimoirePart, HouseDramaPart,
InventoryPart*, LanternSitePart, LifespanPart, LightSourcePart,
MaterialPart, MeleeWeaponPart, MutationsPart*, OnHitEventProbePart,
OvenSitePart, PhysicsPart, RenderPart, RentalPart, SanctuaryPart,
SeveredLimbPart, ShowcaseIndestructiblePart, ShowcaseStoneSkinPart,
SkillsPart, StackerPart, StairsDownPart, StairsUpPart,
StatusEffectsPart*, StatusTonicPart, ThermalPart,
ThunderHammerDemoProbePart, TinkerItemPart, TonicPart, WellSitePart
```
\* = has explicit save handler in `SaveSystem.cs`

**Indirect Part subclasses (12+):**
- AIBehaviorPart family: AIHoarderPart, BedPart, AIWellVisitorPart,
  AIAmbushPart, AIUndertakerPart, AIGuardPart, ChairPart,
  AIRetrieverPart, AIPetterPart, AIFleeToShrinePart,
  AISelfPreservationPart, AILayRunePart
- BaseSkillPart family: 38 skill classes (4 weapon trees + 5 magic
  + Acrobatics + 25+ powers)
- TriggerOnStepPart family: RuneFlame/Frost/PoisonTrigger,
  SpikeTrap, FireTrap, TripWire, PressurePlate

**Total Part hierarchy ≈ 100+ types.**

### Tier classification (per save handler)

| Tier | Save handler | Risk profile | Count |
|---|---|---|---|
| **1** | Explicit handler in SaveSystem.cs | Low — author wrote custom logic; bugs are usually missing fields, not type errors | 7 |
| **2** | `ISaveSerializable` (custom Save/Load) | Low — author opted into custom logic | 0 known |
| **3** | `WritePublicFields` reflection | High — depends on whether all state is in public fields with supported types | ~93+ |

Tier 3 is the bulk. This is where bugs hide.

---

## Bug-class taxonomy (specific to save/load)

Extension of the 18-surface taxonomy in `ADVERSARIAL_TESTING.md`,
narrowed to save/load specifics.

| # | Surface | Probe | Likely bug |
|---|---|---|---|
| **SL-1** | Public field round-trip | Field set → save → load → field equal? | None expected (the rental audit proved this works for simple types) |
| **SL-2** | Private field NOT round-tripping | Private field set → save → load → private field == default? | EXPECTED to "fail" (private fields are silently skipped — verify the contract) |
| **SL-3** | Property round-trip | Property set → save → load → property equal? | Properties don't round-trip unless they have a public backing field; needs case-by-case verification |
| **SL-4** | `Entity` reference round-trip | Part has `public Entity Foo` → save → load → does `Foo` re-resolve to the same logical entity? | Suspicious surface — `WriteEntityReference` stores the ID; `ReadEntityReference` resolves via `EntityFactory`. If the referenced entity isn't in the load context, what happens? |
| **SL-5** | Generic collection round-trip | `List<int>`, `List<string>`, `List<Entity>`, `List<CustomStruct>` — verify each | `List<int>`/`List<string>` likely fine; `List<Entity>` might break references; `List<CustomStruct>` likely unsupported |
| **SL-6** | Enum round-trip | Enum field with all values | Should work (cast to int); verify with negative-int-value edge case if any enum uses one |
| **SL-7** | Object reference (non-Entity) | A Part that holds a reference to another Part or arbitrary object | Likely silently nulled |
| **SL-8** | Cross-Part reference integrity | Body references items in InventoryPart's EquippedItems dict — survives round-trip? | Depends on order — Body load needs InventoryPart to already exist |
| **SL-9** | Mid-state save | Part with state that's "in progress" (cooldown ticking, effect mid-tick, drag in progress) | The "moment" survives, but downstream timer math might use stale values |
| **SL-10** | Effect.IgnitionSource (Entity ref) | BurningEffect saves IgnitionSource — does load resolve it? | Critical bug-yield surface; HookedEffect.Hooker similar |
| **SL-11** | NonSerialized fields | `[NonSerialized]` annotated fields — does the reflection actually skip them? | Depends on the annotation handling in GetSerializablePublicFields |
| **SL-12** | Part with explicit handler | Verify the explicit handlers (StatusEffectsPart, InventoryPart, etc.) preserve their full state matrix | Tests likely missing for some sub-state |
| **SL-13** | Part with private state machine | Parts that maintain `_state` enum or `_cachedX` — check that load reconstructs reachable state | If reachable state isn't re-derived in OnAfterLoad, post-load behavior diverges |
| **SL-14** | Empty/default Part | Save+load a fresh-default Part — round-trip equality | Should work; counter-check that distinguishes "real bug" from "default value coincidence" |
| **SL-15** | Part with constructor args | Activator.CreateInstance fails on Parts with non-default ctors. Effects use FormatterServices bypass; verify Parts don't have this issue | If a Part has only parameterized ctor, load throws MissingMethodException |
| **SL-16** | Part-with-Part reference | A Part that references another Part on the same entity | Likely needs custom serialization or post-load resolution |

---

## Sub-milestones (smallest-blast-radius-first)

Per CLAUDE.md §1.4: each commits as one reviewable change,
independently revertable, ships one complete testable behavior.

### SL.1 — Plan to disk ✅ COMPLETE

This document. Sets up the audit, classifies Parts, defines the
bug-class taxonomy, plans the sub-milestones.

### SL.2 — Save/load test harness + Tier-3 simple-Part baseline ✅ COMPLETE

**Scope:** Build the reusable `RoundTripEntity` helper +
`RoundTripPart` helper + audit the 5-10 simplest Tier-3 Parts
(string + int fields only — `RentalPart`-class).

**Why first:** Establishes the test infrastructure that every
later sub-milestone uses, plus pins the simplest cases first to
build confidence.

**Targets:**
- `RentalPart` (already done in `RentalSystemDeepAdversarialTests`)
- `CommercePart` ✅
- `PhysicsPart` ✅ (simple fields; Entity refs → SL.3)
- `RenderPart` ✅
- `EquippablePart` ✅
- `MaterialPart` ✅ (incl. `HashSet<string>` probe — see Findings #1, #2)
- `LifespanPart` ✅
- `FuelPart` ✅
- `StackerPart` ✅
- `ExaminablePart` ✅ (incl. unicode + special chars probe)

**Result:** All round-trip cleanly. **14 tests, 14 GREEN, 0 bugs found.**

**Deliverables:**
- `Assets/Tests/EditMode/TestSupport/PartRoundTripHelper.cs` — reusable
  `RoundTripEntity` + `RoundTripEntityWithFactory` helpers.
- `Assets/Tests/EditMode/Gameplay/Save/Tier3SimplePartRoundTripTests.cs`
  — 14 adversarial round-trip tests covering 9 Parts.

**Findings (see Findings log below):**
- 🔵 #1 — `HashSet<T>` IS supported by reflection serializer (audit plan correction).
- 🔵 #2 — `SaveSystem.cs:656` uses `entity.Parts.Add` direct, NOT `AddPart`,
  so `Initialize()` is NOT called on load. Saved cache wins over re-derivation.
- ⚪ #3 — `PhysicsPart.InInventory` + `Equipped` Entity refs deferred to SL.3.
- ⚪ #4 — `MaterialPart` constants (5 floats) round-trip exactly at IEEE 754
  boundary values (75.5, 100, 0.25, 1.75, 0).

### SL.3 — Tier-3 Parts with Entity references (HIGH BUG YIELD)

**Scope:** Audit Parts that hold `public Entity Foo` fields. The
save/load reference resolution is the highest-suspicion surface.

**Targets (preliminary scan):**
- `BurningEffect.IgnitionSource` (Effect, not Part — but same path)
- `HookedEffect.Hooker` (Effect)
- `RentalPart.LessorBlueprintName` (string, not Entity — already verified)
- `LightSourcePart` — check for entity refs
- `ConversationPart` — likely references speaker/listener
- `GlowmawAmbushPart` — likely references the player
- `SanctuaryPart` — references shrine entities?
- Trigger parts that reference an "owner" or "trap-layer"

**Expected:** Some references survive (via WriteEntityReference);
some may not (private storage, lookup-on-load patterns). Real bugs
likely.

### SL.4 — Tier-3 Parts with collections

**Scope:** Audit Parts holding `List<>`, `Dict<>`, arrays.

**Targets (preliminary scan):**
- `Body` (already explicit handler — verify completeness)
- `InventoryPart` (explicit; verify EquippedItems dict round-trip)
- `MaterialPart` (lists of material tags)
- `CampfirePart`, `OvenSitePart`, `WellSitePart` (settlement state)
- `HouseDramaPart` (likely complex state)
- `CorpsePart` (any inventory? loot list?)

**Expected:** Collection types are mostly supported via WriteFieldValue,
but order-dependent state (e.g., List<X> where order matters) might
flip on load. Some struct collections may break.

### SL.5 — Tier-3 Parts with private/internal state

**Scope:** Audit Parts that use private fields for state that's
NOT captured by public-field reflection.

**Targets (preliminary scan):** any Part with `_lazyInit`,
`_cachedX`, `private XState _state` patterns.

**Expected:** Private state is silently lost. The fix pattern is
either (a) make the field public, (b) use OnAfterLoad to re-derive,
(c) implement ISaveSerializable.

**Highest probability of finding 🔴 bugs.**

### SL.6 — Effect round-trip audit

**Scope:** All `Effect` subclasses (~20).

**Targets:**
- BurningEffect (Intensity, IgnitionSource Entity ref, Rng — all 3)
- BleedingEffect, PoisonedEffect (damageDice strings)
- StunnedEffect, FrozenEffect, ParalyzedEffect (action-blocking)
- HibernatingEffect (PriorHeatResistance, PriorColdResistance)
- ShatterArmorEffect (StackCount)
- HookedEffect (Hooker entity ref)
- RootedEffect (just Duration; simple)
- BerserkEffect (stat shifts)
- CharredEffect, SmolderingEffect, AcidicEffect, ElectrifiedEffect, WetEffect
- ConfusedEffect, HobbledEffect, BrokenEffect

**Special focus:** entity references (IgnitionSource, Hooker) —
do they round-trip via WriteEntityReference?

### SL.7 — Tier-1 explicit handlers — round-trip completeness

**Scope:** Verify explicit handlers preserve full state.

**Targets:**
- `SaveStatusEffectsPart` / `LoadStatusEffectsPart`
- `SaveInventoryPart` / `LoadInventoryPart` (especially EquippedItems)
- `SaveActivatedAbilitiesPart` (cooldowns, hotkey bindings)
- `SaveBody` (anatomy tree, equipment slots)
- `SaveBrainPart` (AI state, current goal, goal stack)
- `SaveMutationsPart` (mutations + their state)
- `SaveBitLockerPart` (already explicit; minor)

**Why last:** explicit handlers were written by humans aware of the
state shape, so bug yield is lower. But the surface area is large —
do an end-of-audit pass to verify all public state is captured.

### SL.8 — Cross-Part reference integrity + load order

**Scope:** Tests verifying relationships across Parts survive
round-trip.

**Targets:**
- Body's BodyPart tree ↔ InventoryPart's EquippedItems
- Effect.Owner ↔ entity it's attached to
- ActivatedAbilitiesPart ability Guids ↔ SkillsPart ActivatedAbilityID
  field on each skill (proven to persist via WSP3.5 cold-eye fix)
- Conversation refs back to NPC entities

### SL.9 — Mid-state save scenarios

**Scope:** Save WHILE state is in flux:
- Cooldown ticking down (CooldownRemaining > 0)
- Effect mid-duration (Duration = 3)
- HookedEffect mid-drag (turns remaining)
- HeartFlame charges spent (1 of 3 used)
- Burning at non-default intensity

**Why valuable:** the "happy path" round-trip might work but
mid-state has more surface area. A buggy impl might reset
"transient" state to defaults (charges go back to 3, cooldown
goes back to 0).

### SL.10 — Cold-eye + adversarial sweep + final document update

Final pass per CLAUDE.md §"Cold-eye review":
- Q1: Do save/load handlers mirror their save vs load shapes?
- Q2: Cross-Part schema consistency
- Q3: Counter-check completeness for round-trip pairs
- Q4: Doc-vs-impl drift

Finalize this doc with all findings + total counts.

---

## Implementation pattern

### Test file naming

```
Assets/Tests/EditMode/Gameplay/Save/<Tier>RoundTripTests.cs
```

- `Tier3SimplePartRoundTripTests.cs` (SL.2)
- `Tier3EntityReferenceRoundTripTests.cs` (SL.3)
- `Tier3CollectionRoundTripTests.cs` (SL.4)
- `Tier3PrivateStateRoundTripTests.cs` (SL.5)
- `EffectRoundTripTests.cs` (SL.6)
- `Tier1ExplicitHandlerRoundTripTests.cs` (SL.7)
- `CrossPartReferenceRoundTripTests.cs` (SL.8)
- `MidStateRoundTripTests.cs` (SL.9)

### Reusable helpers

Following the pattern proven in `RentalSystemDeepAdversarialTests`:

```csharp
private static Entity RoundTripEntity(Entity src)
{
    using var stream = new MemoryStream();
    var writer = new SaveWriter(stream);
    SaveGraphSerializer.SaveEntityBody(src, writer);
    stream.Position = 0;
    var reader = new SaveReader(stream, factory: null);
    var loaded = new Entity();
    SaveGraphSerializer.LoadEntityBody(loaded, reader);
    return loaded;
}
```

For Parts that need a real EntityFactory (Entity-reference resolution):

```csharp
private static Entity RoundTripEntityWithFactory(Entity src,
    System.Func<EntityFactory> factoryProvider)
{
    using var stream = new MemoryStream();
    var writer = new SaveWriter(stream);
    SaveGraphSerializer.SaveEntityBody(src, writer);
    stream.Position = 0;
    var reader = new SaveReader(stream, factory: factoryProvider());
    var loaded = new Entity();
    SaveGraphSerializer.LoadEntityBody(loaded, reader);
    return loaded;
}
```

### Test naming convention (per ADVERSARIAL_TESTING.md)

```
Adversarial_<Part>_<Field/Behavior>_<RoundTrips|Lost>
```

Examples:
- `Adversarial_RentalPart_InkPaid_RoundTrips`
- `Adversarial_BurningEffect_IgnitionSource_RoundTrips`
- `Adversarial_GlowmawAmbushPart_PrivateAmbushState_LostByReflection`
  (documents the bug, even before fix)

---

## Findings log

(Populated as the audit progresses. Each finding has severity,
description, fix status.)

| # | Severity | Part / Field | Description | Status |
|---|---|---|---|---|
| 1 | 🔵 | (audit plan, not a bug) `HashSet<T>` support | Audit plan listed `HashSet<T>` as "likely unsupported" by `WriteFieldValue`. Source survey at `SaveSystem.cs:1635, 1685, 1750` proves the type IS supported (it's an explicit branch in `CanSerializeType` + paired write/read paths). The probe test `MaterialPart.MaterialTags` round-trips correctly. | Plan corrected. No code change needed. |
| 2 | 🔵 | `SaveSystem.cs:656` (load path) `Initialize()` does NOT run on load | The save load path uses `entity.Parts.Add(part)` direct, NOT `entity.AddPart(part)`. Only `AddPart` invokes `Initialize()`. **Implication:** any Part with derived state (`MaterialPart.MaterialTags` HashSet derived from `MaterialTagsRaw`, etc.) round-trips the SAVED state, NOT a re-derivation from the source. If the saved HashSet is mutated out of sync with the raw, the saved version wins. Pinned by `Adversarial_MaterialPart_LoadDoesNotCallInitialize_SavedCacheWinsOverDerivation`. | Contract pinned. No code change needed. |
| 3 | ⚪ | `PhysicsPart.InInventory` / `PhysicsPart.Equipped` (`Entity` refs) | Skipped from SL.2 because Entity-reference round-trip needs an `EntityFactory` to resolve on load. Deferred to SL.3. | Deferred → SL.3. |
| 4 | ⚪ | `FuelPart` 5-float boundary | Verified that `FuelMass=75.5f`, `MaxFuel=100f`, `BurnRate=0.25f`, `HeatOutput=1.75f`, `ExhaustProduct="AshPile"` round-trip with `Assert.AreEqual` (exact float equality, no tolerance). All five values are IEEE 754-representable. | Contract pinned. |
| 5 | ⚪ | `ExaminablePart.Text` special-char round-trip | `"\"quoted\" with\nnewline + unicode: ñ ☆ → ✦"` round-trips byte-perfect. Counter-checks the SaveWriter UTF-8 string encoding for embedded quotes, newlines, multi-byte unicode codepoints. | Contract pinned. |

---

## Cumulative test inventory

(Updated per sub-milestone.)

| Sub-milestone | File | Tests | Real bugs found |
|---|---|---|---|
| SL.1 | (plan only) | 0 | 0 |
| SL.2 | Tier3SimplePartRoundTripTests.cs | **14** | **0** |
| SL.3 | Tier3EntityReferenceRoundTripTests.cs | TBD | TBD |
| SL.4 | Tier3CollectionRoundTripTests.cs | TBD | TBD |
| SL.5 | Tier3PrivateStateRoundTripTests.cs | TBD | TBD |
| SL.6 | EffectRoundTripTests.cs | TBD | TBD |
| SL.7 | Tier1ExplicitHandlerRoundTripTests.cs | TBD | TBD |
| SL.8 | CrossPartReferenceRoundTripTests.cs | TBD | TBD |
| SL.9 | MidStateRoundTripTests.cs | TBD | TBD |
| **TOTAL** | | **14** | **0** |

---

## Self-review log (per sub-milestone)

(Populated at the end of each sub-milestone. Q1-Q4 from cold-eye
review + adversarial-sweep findings.)

### SL.1 — plan-to-disk

**Q1 Symmetry:** N/A (no code yet)
**Q2 Cross-feature consistency:** Plan structure mirrors prior
audit docs (`Docs/COMBAT-PARITY-PORT-REVIEW.md`); status banner +
sub-milestone log + findings table all match the established
pattern.
**Q3 Counter-check completeness:** N/A (no tests yet)
**Q4 Doc-vs-impl:** Plan cites `SaveSystem.cs:1105-1130` +
`SaveSystem.cs:WriteFieldValue` — verified line numbers + matrix
of supported types BEFORE writing the plan.

**Adversarial-sweep self-check:** Per ADVERSARIAL_TESTING.md
"Strategy B for existing features," I prioritized **save/load
reflection (surface #3)** as the highest-yield surface. That
matches the audit scope. Sub-milestones are ordered to do the
highest-bug-yield surfaces (private state, entity references,
collections) BEFORE the explicit handlers (lower yield).

---

### SL.2 — Tier-3 simple-Part baseline

**Q1 Symmetry (mirror checks):** The two `RoundTrip*` helpers in
`PartRoundTripHelper.cs` are intentionally near-identical aside
from the `factory:` argument — a future SL.3 contributor adding
an Entity-reference test must not have to re-discover the
plumbing. The helpers passed the "swap categories in your head"
test: read either one and the surrounding pipeline still makes
sense.

**Q2 Cross-feature consistency:** Test naming follows
`Adversarial_<Part>_<Field/Behavior>_<RoundTrips|Lost>` from
ADVERSARIAL_TESTING.md. Reviewed all 14 tests against the table —
all conform. The `_RoundTrips` / `_DerivesToHashSet_RoundTripsBoth`
/ `_LoadDoesNotCallInitialize_SavedCacheWinsOverDerivation` suffixes
make the asserted contract immediately legible from the test name.

**Q3 Counter-check completeness:** Each "round-trips correctly"
positive assertion has a counter-check:
- `RenderPart.Visible = false` (non-default) — counter-checks a
  buggy impl that always returned the default `true`.
- `Adversarial_DefaultPart_RoundTripsToSameDefaults` —
  counter-checks a buggy impl that fabricates non-default values
  on load.
- `Adversarial_MaterialPart_LoadDoesNotCallInitialize_SavedCacheWinsOverDerivation`
  — counter-checks the alternative where `Initialize` runs on load.
  If a future change adds an `entity.AddPart(part)` call to the
  load path, this test breaks visibly.

**Q4 Doc-vs-impl drift:** This document's Tier 3 / `WriteFieldValue`
matrix originally listed `HashSet<T>` as "likely unsupported." The
SL.2 source survey at `SaveSystem.cs:1635, 1685, 1750` corrected
that. The matrix is now updated. The drift was caught BEFORE
shipping any test that relied on the wrong assumption.

**Adversarial-sweep self-check:** Probed bug surfaces SL-1
(public field round-trip, 14 tests), SL-5 (HashSet<string>
collection — 2 tests), SL-14 (default-value counter-check, 1 test),
plus indirectly SL-13 (private state via Initialize-on-load probe,
1 test). 4 of 16 surfaces pinned with counter-checks; remaining 12
slated for SL.3-SL.10.

**Lessons learned (false-premise corrections):**
1. Initial audit-plan claim "HashSet<T> likely unsupported" was
   wrong — caught by source survey, fixed in plan + matrix.
2. Initial test for HashSet round-trip used empty `MaterialTagsRaw`
   + `MaterialTags.Add(...)` directly. The test went RED on the
   first run; debugging revealed `entity.AddPart(part)` calls
   `Initialize()` which calls `MaterialTags.Clear()` before parsing
   the (empty) raw. **The test was wrong, not the production code.**
   Replaced with two corrected tests that exercise the actual
   production pattern (`MaterialTagsRaw` non-empty + Initialize
   parses on AddPart) plus an adversarial probe of the
   Initialize-doesn't-run-on-load contract.

---

## Commit history

| Commit | Sub-milestone | Notes |
|---|---|---|
| TBD | SL.1 | Plan to disk |
| TBD | SL.2 | Tier-3 simple-Part baseline (14 tests, 9 Parts, 0 bugs, 5 contracts pinned) |

---

## Risks + mitigations

**Risk: I might not have access to a working `EntityFactory` in
EditMode tests.** The current `RoundTripEntity` helper passes
`factory: null` to `SaveReader`, which works for simple Parts but
might fail when an `Entity` reference field needs resolution.
- **Mitigation:** SL.3 will explicitly probe this. If null factory
  is insufficient, build a minimal in-memory factory for tests.

**Risk: Some Parts may have construction dependencies I'm not
aware of.** A `Part` whose constructor sets up state that the
default-constructor path doesn't replicate.
- **Mitigation:** SL.5 specifically targets private state. If a
  Part fails round-trip because the load path doesn't re-init,
  that's a finding and we add `OnAfterLoad`.

**Risk: This audit might surface MANY 🔴 bugs at once.** If
private state is universally lost, the fix is a many-Part change.
- **Mitigation:** Each sub-milestone is independently revertable.
  If bugs are pervasive, ship them in priority order rather than
  blocking on a single mega-fix.

**Risk: Doc bloat.** This document will grow as findings accumulate.
- **Mitigation:** Keep the status banner at the top current.
  Findings log uses tabular format (terse). Move detailed prose
  to per-sub-milestone sections.

---

## Out of scope

- **Save file format compatibility across versions.** This audit
  only verifies round-trip within a single version. Cross-version
  compatibility is a separate concern.
- **Performance.** Save/load speed is not measured here. If a
  given Part takes 100ms to round-trip, that's fine for this audit
  (correctness over speed).
- **Disk I/O paths.** Stream/file errors, partial writes, gzip
  validation — all tested elsewhere. This audit uses
  `MemoryStream` exclusively.
- **Save-graph cycles.** If two Parts reference each other, the
  current save format may have issues — out of scope for first pass.

---

## Appendix: minimum-viable verification (per ADVERSARIAL_TESTING.md)

If time pressure forces a stop early, the minimum-viable subset:

1. **SL.2** — simple Tier-3 Parts (proves the reflection path)
2. **SL.3** — Entity references (highest-suspicion surface)
3. **SL.6 (Effects)** — `BurningEffect.IgnitionSource` round-trip
   specifically (highest-traffic Effect with an entity ref)

That's roughly 15-20 tests catching the most likely bug classes
in 1-2 hours. The full audit catches 90%+ of the surface in
~6-8 hours.

---

*End of plan. Updated as audit progresses. Companion to
`ADVERSARIAL_TESTING.md` (project root) and `CLAUDE.md`
§"Adversarial test sweep."*
