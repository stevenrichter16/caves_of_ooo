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
| **Current sub-milestone** | SL.6 — Effect round-trip audit (in progress) |
| **Last updated** | 2026-05-10 |
| **Total tests added** | 14 (SL.2) + 10 (SL.3) + 6 (SL.4) + 7 (SL.5) = 37 |
| **Total Part types audited** | 18 / ~62 (SL.2: 9; SL.3: 2; SL.4: 1; SL.5: 6 — DamageFlash, MeleeWeapon, 4× settlement-site) |
| **Real bugs found** | 0 (Part audit) — SL.6 verification sweep flags 1 likely Effect bug |
| **Real bugs fixed** | 0 |
| **Contracts pinned** | 5 (SL.2) + 6 (SL.3) + 4 (SL.4) + 4 (SL.5) = 19 |
| **Latest commit** | `278558b` (SL.5 merge) |

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

### SL.3 — Tier-3 Parts with Entity references ✅ COMPLETE

**Scope:** Audit Tier-3 (reflection-serialized) Parts that hold
`public Entity Foo` fields or generic collections of Entity. The
save/load reference resolution is the highest-suspicion surface.

**Re-scoped during execution:** the SL.3 preliminary target list
mixed Tier-1 (explicit handler), Tier-3 (reflection), and Effect
paths. The Effect-path targets (`BurningEffect.IgnitionSource`,
`HookedEffect.Hooker`, `SittingEffect.Furniture`, base
`Effect.Owner`) use a separate save flow with constructor bypass
via `FormatterServices` — re-routed to SL.6. Tier-1 explicit
handlers (`BrainPart.Target`, `BrainPart.PersonalEnemies`,
`InventoryPart.Objects` / `EquippedItems`, `Body._Equipped` etc.)
re-routed to SL.7. SL.3 focuses on the pure Tier-3 reflection path.

**Targets (final):**
- `PhysicsPart.InInventory` ✅ (Entity, deferred from SL.2)
- `PhysicsPart.Equipped` ✅ (Entity, deferred from SL.2)
- `ContainerPart.Contents` ✅ (List&lt;Entity&gt;)
- `ContainerPart` simple fields (Preposition, Locked, MaxItems) ✅

**Result:** All round-trip cleanly **provided the Entity-body
queue is flushed**. **10 tests, 0 bugs found**, 6 contracts pinned.

**Major contract correction logged (Finding #6):**
The bare `RoundTripEntity` helper (used in SL.2) does NOT
round-trip Entity refs to populated entities — it produces empty
placeholders. The full `RoundTripEntityWithBodies` helper (added
in SL.3) flushes `WriteQueuedEntityBodies` on save and
`ReadEntityBodies` on load, which is what production does. This
distinction is now pinned by tests so that future contributors
can't conflate the two.

**Deliverables:**
- Modified `Assets/Tests/EditMode/TestSupport/PartRoundTripHelper.cs`
  — added `RoundTripEntityWithBodies`; corrected misleading
  `RoundTripEntityWithFactory` docstring (the factory is NOT
  consulted by `ReadEntityReference`).
- New `Assets/Tests/EditMode/Gameplay/Save/Tier3EntityReferenceRoundTripTests.cs`
  — 10 adversarial tests: bare-helper placeholder contract (1),
  full-helper Entity-body round-trip (2 for `PhysicsPart`),
  null-Entity counter-check (1), token-dedup (1) + counter-check
  for distinct entities (1), `ContainerPart.Contents` full round-
  trip (1) + empty counter-check (1), `ContainerPart` simple-
  fields (1), cross-Part token-dedup (1).

**Findings (see Findings log below):**
- 🟡 #6 — `RoundTripEntity` does NOT round-trip Entity-ref bodies.
  This was hidden behind the SL.2 "PhysicsPart Entity refs deferred"
  note; SL.3 surfaced and pinned the contract.
- 🔵 #7 — `EntityFactory` is NOT consulted by
  `ReadEntityReference`. Used solely by `LoadOverworldZoneManager`
  for full-game restore. Helper docstring corrected.
- 🔵 #8 — Token dedup spans the whole save graph, not per-Part.
  Two refs to the same source entity (whether on the same Part or
  across Parts) load as a single Entity instance.
- 🔵 #9 — Token=0 sentinel is reserved for null. Null Entity refs
  round-trip as null (NOT as an empty placeholder).
- 🔵 #10 — `List<Entity>` count=0 round-trips as a non-null empty
  list, not as null. The `IList<T>` write path encodes `count` as
  -1 for null vs 0 for empty (`SaveSystem.cs:1677, 1741-1743`).
- 🔵 #11 — `ContainerPart.Contents` (List&lt;Entity&gt;) elements
  use `WriteCollectionElement` which routes to `WriteEntityReference`
  for `Entity`-typed elements (`SaveSystem.cs:1767-1770`).

### SL.4 — Tier-3 Parts with collections ✅ COMPLETE

**Scope:** Audit pure-Tier-3 Parts holding `List<>`, `Dict<>`,
arrays, or `HashSet<>` of NON-Entity types.

**Re-scoped during execution:** the preliminary list mixed many
non-Tier-3 candidates. Source survey showed:
- `Body`, `InventoryPart` are Tier-1 explicit handlers → SL.7
- `MaterialPart.MaterialTags` (HashSet&lt;string&gt;) was already
  pinned in SL.2 ✓
- `ContainerPart.Contents` (List&lt;Entity&gt;) was already pinned in
  SL.3 ✓
- `MeleeWeaponPart.OnHitEffectsCachedSpecs` is a PROPERTY (not a
  field), backing field `_cachedOnHitEffectSpecs` is private →
  reflection skips both. The cache is lazily reparsed from
  `OnHitEffectsRaw` after load. **No round-trip concern.**
- `CampfirePart` / `OvenSitePart` / `WellSitePart` / `CorpsePart`
  do NOT have public collection fields per source survey — flagged
  as ⚪ "no SL.4 surface" (revisit in SL.5 for private state).
- `HouseDramaPart` does have collections; deferred to a follow-up
  HouseDrama-specific audit due to its size and the standalone
  HouseDramaRuntime / DataLoader subsystem.

**Targets (final):**
- `SkillsPart.SkillList` (List&lt;BaseSkillPart&gt;) ✅ — Tier-3
  with custom-class elements, depends on `OnAfterLoad` to rebuild
  from `ParentEntity.Parts`.

**Result:** 6 tests, 0 bugs found, 4 contracts pinned + helper
extended.

**Major helper extension (Finding #12):**
The previous helpers (`RoundTripEntity`, `RoundTripEntityWithBodies`)
both call `LoadEntityBody` directly on the primary entity, which
**bypasses `OnAfterLoad` and `FinalizeLoad` for that entity**
(those hooks run only inside `ReadEntityBodies`, which iterates
`SaveReader._loadedEntities` — a list populated only by
`ReadEntityReference` calls). For Parts whose correctness depends
on those hooks (`SkillsPart`, `MutationsPart`), this gap means the
loaded state has identity divergence between the convenience cache
and the entity's Parts list.

SL.4 added `RoundTripEntityViaTokenGraph` which queues the source
as a token (via `WriteEntityReference`) and reads it back via
`ReadEntityReference` + `ReadEntityBodies` — exactly mirroring
production's `GameSessionState.Restore` flow. This helper invokes
the post-load hooks correctly.

**Deliverables:**
- Modified `Assets/Tests/EditMode/TestSupport/PartRoundTripHelper.cs`
  — added `RoundTripEntityViaTokenGraph` (production-faithful
  primary-entity round-trip).
- New `Assets/Tests/EditMode/Gameplay/Save/Tier3CollectionRoundTripTests.cs`
  — 6 adversarial tests: empty SkillList round-trip (1), single-skill
  round-trip with identity resolution (1), concrete-subtype preservation
  (1), ParentEntity wiring (1), helper-distinction adversarial (1),
  multi-skill / duplicate-rejection (1).

**Findings (see Findings log below):**
- 🟡 #12 — `RoundTripEntity` and `RoundTripEntityWithBodies` do NOT
  invoke `OnAfterLoad`/`FinalizeLoad` on the primary entity's Parts.
  Test-infra concern — production flow always primes via
  `ReadEntityReference`. Pinned by adversarial test that exposes
  the SkillList identity divergence under WithBodies; fixed by
  `RoundTripEntityViaTokenGraph`.
- 🔵 #13 — `WriteTypedObject`/`ReadTypedObject` (`SaveSystem.cs:1786-1820`)
  preserves the concrete C# type for custom-class collection
  elements. Pinned by `_PreservesConcreteSubtype_NotJustBaseSkillPart`.
- 🔵 #14 — Loaded `BaseSkillPart.ParentEntity` correctly references
  the loaded entity (not the source, not null). The Part-system load
  path sets ParentEntity at `SaveSystem.cs:655`. Pinned by
  `_LoadedSkill_HasParentEntityWired`.
- 🔵 #15 — `SkillsPart.AddSkill` rejects duplicate skill types
  (returns false). Pin pinned as a side-effect of the multi-skill
  test — relevant to save/load because if AddSkill weren't
  duplicate-safe, OnAfterLoad's rebuild-from-Parts would produce
  duplicates after a save/load cycle.

### SL.5 — Tier-3 Parts with private/internal state ✅ COMPLETE

**Scope:** Audit Parts that use private fields for state that's
NOT captured by public-field reflection.

**Audit-doc prediction:** "Highest probability of finding 🔴 bugs."

**Result: 0 bugs found.** All probed private state was correctly
designed:
- Derived caches (e.g., `MeleeWeaponPart._cachedOnHitEffectSpecs`)
  rebuild lazily from public state; correct.
- Visual frame counters (`_renderFrameCounter`) reset on load;
  correct (no continuity needed).
- One-shot proximity message flags (`_proximityMessageShown`)
  reset on load; **intentional welcome-back behavior** per the
  source-comment design.
- Field-initialized enum caches (`_lastAppliedStage =
  RepairStage.Fouled`) reset to the initializer value (NOT enum
  default); correct because `Activator.CreateInstance` runs the
  default constructor including field initializers.
- `DamageFlashPart._flashFramesRemaining` resets on load — fresh
  full-duration flash on next TakeDamage event, correct.

**Contracts pinned (4):**
- 🔵 #16 — Private fields skipped by reflection serializer; reset
  to default-or-initializer on load (per `BindingFlags.Instance |
  BindingFlags.Public` at SaveSystem.cs:1593).
- 🔵 #17 — `Activator.CreateInstance` runs the constructor +
  field initializers, so private fields with initializers get
  their initializer value (NOT type defaults). Pinned by
  `_LanternSitePart_LastAppliedStage_ResetsToFieldInitializer`.
- 🔵 #18 — Lazy-rebuild caches that derive from public state
  (e.g., `MeleeWeaponPart.OnHitEffectsCachedSpecs`) work
  correctly post-load: cache is null after reflection load, first
  access triggers parse from public raw string. Pinned by
  `_MeleeWeaponPart_PrivateLazyCache_RebuildsAfterLoad`.
- 🔵 #19 — Settlement-site Parts (`LanternSitePart`,
  `OvenSitePart`, `WellSitePart`) all share the same
  private-state-resets-on-load pattern. Public fields
  (`SettlementId`, `SiteId`) preserved; 4 private fields per Part
  reset. Pinned by 3 separate tests + 1 cross-cutting field-
  initializer test.

**Targets audited:**
- `DamageFlashPart` ✅ — `_flashFramesRemaining` private int
- `LanternSitePart` ✅ — 4 private fields (counter, bool flag,
  enum, bool)
- `OvenSitePart` ✅ — 4 private fields
- `WellSitePart` ✅ — 4 private fields
- `MeleeWeaponPart` ✅ — `_cachedOnHitEffectSpecs` +
  `_cachedOnHitEffectsRawSnapshot` lazy cache pair
- `CampfirePart` ⚪ — same shape as Lantern/Oven/Well; covered by
  the contract pinned for the others (not separately tested to
  avoid redundancy)

**Deliverables:**
- New `Assets/Tests/EditMode/Gameplay/Save/Tier3PrivateStateRoundTripTests.cs`
  — 7 adversarial tests using reflection helpers
  (`GetPrivateField` / `SetPrivateField`) to peek at
  reflection-skipped state.

**Lesson:** the audit-doc's "highest bug probability" prediction
turned out to be wrong for this codebase. All private state
encountered was either derived (rebuilds correctly) or visual
ephemera (resets are correct). This is a strong signal that the
project's save-load discipline is solid — but the contract pinning
remains valuable as regression protection.

### SL.6 — Effect round-trip audit (in progress)

**Scope:** All `Effect` subclasses (~20).

**Verification sweep (SaveSystem.cs:1178-1204):**
```csharp
SaveEffect:                      LoadEffect:
  WriteString(typeName)            ResolveType(reader.ReadString())
  effect.OnBeforeSave(writer)      FormatterServices.GetUninitializedObject  ⚠ ctor bypassed
  Write(effect.Duration)           effect.Duration = reader.ReadInt()
  WritePublicFields(filter Owner   ReadPublicFields  (no OnAfterLoad hook!)
                    + Duration)
  effect.OnAfterSave(writer)
```

`GetSerializablePublicFields` (SaveSystem.cs:1590-1607) walks
`type.GetFields(Public | Instance)` only — **private fields and
private-setter property backing fields are silently skipped on save**.
`LoadEffect` has **no load-side hook** to mirror the save-side
`OnAfterSave`, so any custom binary written via OnAfterSave can't
be read back.

**Tier classification:**
- **Tier-A simple** (just Duration, no payload, no entity refs):
  RootedEffect, BrokenEffect, SmolderingEffect, ConfusedEffect,
  HobbledEffect, StunnedEffect, ParalyzedEffect — 7 effects
- **Tier-A payload** (public fields + System.Random + damageDice):
  WetEffect (Moisture), AcidicEffect (Corrosion), ElectrifiedEffect
  (Charge), BleedingEffect (DamageDice + SaveTarget + Rng),
  BerserkEffect, ShatterArmorEffect (StackCount) — 6 effects
- **Tier-B entity refs** (creature pointer that must re-resolve):
  BurningEffect (IgnitionSource), HookedEffect (Hooker) — 2 effects
- **Tier-C non-default ctor / private state**:
  PoisonedEffect, FrozenEffect (Cold), HibernatingEffect
  (PriorHeatResistance/PriorColdResistance — **private setters**),
  CharredEffect (`_originalCombustibility` — fully private) — 4 effects

**🔴 Likely bug** (to be confirmed via RED test in SL.6.4):
`HibernatingEffect` declares `public int PriorHeatResistance { get;
private set; }` and `public int PriorColdResistance { get; private
set; }`. Their compiler-generated backing fields are private, so
WritePublicFields skips them. Loading a hibernating creature drops
its prior resistances → on wake-up it can't restore them.

**Sub-milestones:**
- **SL.6.1** — Tier-A simple (7 effects, ~12 tests). Each: round-trip
  Duration, stack-extends-duration semantic, one counter-check.
- **SL.6.2** — Tier-A payload (6 effects, ~14 tests). Verify
  payload fields survive (Moisture, Corrosion, Charge, DamageDice,
  StackCount, BerserkEffect stat shifts). Pin System.Random
  contract (saved as null, callers tolerate).
- **SL.6.3** — Tier-B entity refs (2 effects, ~6 tests). Mirror
  Tier3EntityReferenceRoundTripTests pattern: save world with
  attacker+defender, defender has BurningEffect.IgnitionSource =
  attacker, save+load entire graph, verify IgnitionSource still
  points at the same entity ID.
- **SL.6.4** — Tier-C (4 effects, ~10 tests). HibernatingEffect:
  RED test → ship + production fix (convert properties to public
  fields OR add OnAfterLoad hook). CharredEffect: pin design intent
  (private state intentionally not preserved; OnApply re-captures).

**Latest test infra:** Effect round-trip needs the
`RoundTripEntityViaTokenGraph` helper from SL.4 (queues source
entity + descendants, flushes WriteQueuedEntityBodies, and
re-resolves references on load). Effects-on-entity tests use
that path; pure-effect-shape tests can use a lighter helper.

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
| 6 | 🟡 | `RoundTripEntity` helper (test infrastructure) does NOT round-trip Entity-ref bodies | The bare helper writes the primary entity's body but NOT the queued referenced-entity bodies (no `WriteQueuedEntityBodies` / `ReadEntityBodies` calls). On load, Entity refs become empty placeholder Entities (token-stamped, ID=null, no Parts). **This is a TEST-INFRASTRUCTURE concern, not a production save bug** — production `SaveSystem` always calls the full pair via `GameSessionState.Capture/Restore`. SL.3 added `RoundTripEntityWithBodies` for the full graph round-trip and pinned the placeholder contract for the bare helper. | Helper extended (`SL.3 commit`); contract pinned by `Adversarial_PhysicsPart_InInventory_BareHelper_LoadsAsPlaceholder`. |
| 7 | 🔵 | `EntityFactory` argument to `SaveReader` is NOT consulted by `ReadEntityReference` | `SaveSystem.cs:174-187` reads tokens from a per-reader `_entityTokens` cache. The factory is stored but only consulted by `LoadOverworldZoneManager` (`SaveSystem.cs:721`) for full-game restore. The audit-plan helper docstring previously implied the factory was needed for cross-entity ref resolution — corrected in SL.3. | Docstring corrected; misleading claim removed. |
| 8 | 🔵 | Token dedup spans the entire save graph (not per-Part) | If entity X is referenced from PhysicsPart.InInventory AND ContainerPart.Contents[0], both refs share a single token via `_entityTokens`. On load, both fields point to the same loaded Entity instance (`AreSame` reference equality). Pinned by `Adversarial_ContainerWithItemAlsoReferencedByPhysicsParent_DedupesAcrossParts`. | Contract pinned. |
| 9 | 🔵 | Token=0 sentinel reserved for null Entity ref | Null Entity values write `0` as the token; on load, `ReadEntityReference` returns `null` for token=0 (does NOT create a placeholder). Pinned by `Adversarial_PhysicsPart_NullEntityRefs_RoundTripAsNull`. | Contract pinned. |
| 10 | 🔵 | `List<Entity>` count=0 round-trips as empty list, not null | `WriteFieldValue` IList path writes `-1` for null and the actual `count` for non-null (including 0). Read path returns null for count=-1 and a fresh `Activator.CreateInstance(type)` for count≥0. Pinned by `Adversarial_ContainerPart_EmptyContents_RoundTripsEmpty`. | Contract pinned. |
| 11 | 🔵 | `List<Entity>` elements route through `WriteEntityReference` | `WriteCollectionElement` (`SaveSystem.cs:1767-1770`) routes `Entity`-typed elements to `WriteEntityReference`, so Entity items in lists get tokenized + queued for body-write the same way as direct `Entity` field refs. Pinned by `Adversarial_ContainerPart_Contents_FullHelper_RoundTripsAllItems`. | Contract pinned. |
| 12 | 🟡 | `RoundTripEntity` + `RoundTripEntityWithBodies` helpers (test infra) do NOT invoke `OnAfterLoad`/`FinalizeLoad` on the primary entity | Both helpers call `LoadEntityBody` directly on a fresh Entity. Per `SaveSystem.cs:189-235`, `OnAfterLoad`/`FinalizeLoad` run only inside `ReadEntityBodies`, which iterates `_loadedEntities` (entities reached via `ReadEntityReference` — NOT the primary). For Parts whose correctness depends on these hooks (e.g. `SkillsPart.OnAfterLoad` rebuilds `SkillList` from `ParentEntity.Parts`), this gap produces silent identity divergence between the convenience cache and the entity's Parts. **Test-infra concern, not a production bug** — production's `GameSessionState.Restore` always primes the primary via `ReadEntityReference` so the hooks fire. | Helper extended (`SL.4 commit`): `RoundTripEntityViaTokenGraph` matches production by queuing the primary as a token. Pinned by `Adversarial_RoundTripEntityWithBodies_DoesNotInvokeOnAfterLoad_OnPrimary` + production-faithful tests using the new helper. |
| 13 | 🔵 | Custom-class collection elements preserve concrete C# subtype on round-trip | `WriteTypedObject` (`SaveSystem.cs:1786-1801`) writes `GetTypeName(actualType)` when the actual type differs from the declared element type, so reflection-loaded `List<BaseSkillPart>` recovers `AcrobaticsSkill` instances (not abstract base instances). Pinned by `Adversarial_SkillsPart_PreservesConcreteSubtype_NotJustBaseSkillPart`. | Contract pinned. |
| 14 | 🔵 | Loaded Parts have `ParentEntity` correctly wired to the loaded entity | `SaveSystem.cs:655` sets `part.ParentEntity = entity` during `LoadEntityBody`'s Part-loading loop. Pinned by `Adversarial_SkillsPart_LoadedSkill_HasParentEntityWired`. | Contract pinned. |
| 15 | 🔵 | `SkillsPart.AddSkill` rejects duplicate skill types | `AddSkill` returns false when the entity already owns a skill of the same type. Relevant to save/load because OnAfterLoad rebuilds SkillList from ParentEntity.Parts — if AddSkill weren't duplicate-safe, the rebuild could produce duplicates. Pinned (as side-effect) by `Adversarial_SkillsPart_MultipleSkills_AllRoundTrip`. | Contract pinned. |
| 16 | 🔵 | Private/internal/protected fields skipped by reflection serializer | `SaveSystem.cs:1593` uses `BindingFlags.Instance \| BindingFlags.Public`, so any non-public field is silently dropped. After load, private fields are at their type-default values OR their field-initializer values (the latter via `Activator.CreateInstance`). | Contract pinned by 7 tests in `Tier3PrivateStateRoundTripTests`. |
| 17 | 🔵 | `Activator.CreateInstance` runs default constructor + field initializers on load | `SaveSystem.cs:1138` constructs Parts via `Activator.CreateInstance(type)` which runs the default ctor (and any field initializers). So `private RepairStage _lastAppliedStage = RepairStage.Fouled;` gets `Fouled` after load, NOT `RepairStage.None` (enum default). | Pinned by `Adversarial_LanternSitePart_LastAppliedStage_ResetsToFieldInitializer`. |
| 18 | 🔵 | Lazy-rebuild caches survive load via public-state derivation | Parts that cache derived state in private fields (e.g., `MeleeWeaponPart._cachedOnHitEffectSpecs` derived from public `OnHitEffectsRaw`) work correctly post-load: cache is null after reflection load → first property-getter access triggers parse → cache populated. | Pinned by `Adversarial_MeleeWeaponPart_PrivateLazyCache_RebuildsAfterLoad`. |
| 19 | 🔵 | Settlement-site Parts share private-state-resets-on-load pattern | `LanternSitePart`, `OvenSitePart`, `WellSitePart` (and presumably `CampfirePart` by structural symmetry — not separately tested) all have identical private fields (`_renderFrameCounter`, `_proximityMessageShown`, `_lastAppliedStage`, `_auraStarted`). All round-trip identically: public IDs preserved, private state resets. | Pinned by 3 separate tests in `Tier3PrivateStateRoundTripTests`. |

---

## Cumulative test inventory

(Updated per sub-milestone.)

| Sub-milestone | File | Tests | Real bugs found |
|---|---|---|---|
| SL.1 | (plan only) | 0 | 0 |
| SL.2 | Tier3SimplePartRoundTripTests.cs | **14** | **0** |
| SL.3 | Tier3EntityReferenceRoundTripTests.cs | **10** | **0** |
| SL.4 | Tier3CollectionRoundTripTests.cs | **6** | **0** |
| SL.5 | Tier3PrivateStateRoundTripTests.cs | **7** | **0** |
| SL.6 | EffectRoundTripTests.cs | TBD | TBD |
| SL.7 | Tier1ExplicitHandlerRoundTripTests.cs | TBD | TBD |
| SL.8 | CrossPartReferenceRoundTripTests.cs | TBD | TBD |
| SL.9 | MidStateRoundTripTests.cs | TBD | TBD |
| **TOTAL** | | **37** | **0** |

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

### SL.3 — Tier-3 Entity-ref round-trip

**Q1 Symmetry (mirror checks):** The SL.3 helper
`RoundTripEntityWithBodies` mirrors the production save flow used
by `GameSessionState.Capture` / `Restore`:
`SaveEntityBody` → `WriteQueuedEntityBodies` (save side) and
`LoadEntityBody` → `ReadEntityBodies` (load side). Read both calls
side-by-side: the queue/body-flush calls happen at the same
relative position in both directions. Mirror check passes.

**Q2 Cross-feature consistency:** Test naming follows
`Adversarial_<Part>_<Field/Behavior>_<RoundTrips|...>` from
ADVERSARIAL_TESTING.md. New suffix introduced this round:
`_BareHelper_LoadsAsPlaceholder` and `_FullHelper_RoundTripsBody`
to disambiguate which helper variant the test exercises. This
makes the helper-dependency obvious from the test name (and the
contract distinction obvious from the test pair).

**Q3 Counter-check completeness:** Each positive assertion has
a counter-check:
- Bare-helper placeholder pinned by
  `_BareHelper_LoadsAsPlaceholder`; full-helper body resolution
  pinned by `_FullHelper_RoundTripsBody`. Cross-pair: would catch
  a buggy impl that flipped helpers.
- `_NullEntityRefs_RoundTripAsNull` counter-checks a buggy
  impl that fabricates placeholders for token=0.
- `_DifferentEntityRefs_LoadAsDistinctInstances` counter-checks
  a buggy impl that always dedupes (would conflate distinct
  entities).
- `_EmptyContents_RoundTripsEmpty` counter-checks a buggy impl
  that collapses count=0 to null on load.
- `_ContainerWithItemAlsoReferencedByPhysicsParent_DedupesAcrossParts`
  counter-checks per-Part token tables (would produce two
  instances).

**Q4 Doc-vs-impl drift:** This document's helper-API description
in §"Implementation pattern" originally implied the factory
argument was needed for cross-entity ref resolution. Source
survey at `SaveSystem.cs:174-187, 721` proves the factory is NOT
consulted by `ReadEntityReference` and is used solely by
`LoadOverworldZoneManager`. The audit-plan helper section is
unchanged here (it still describes the original `RoundTripEntity`
+ `RoundTripEntityWithFactory` pair); the new full helper has
been documented in the helper file's docstring + Findings #6/#7.
Recommend a follow-up update of the §"Implementation pattern"
section in a future SL pass — flagged as ⚪ deferred.

**Adversarial-sweep self-check:** Probed bug surfaces SL-4
(Entity reference round-trip — primary target, 5 tests), SL-5
(Generic collection of Entity, 3 tests), SL-7 (Object reference
non-Entity — verified placeholder behavior, 1 test), and an
implicit cross-Part cohesion probe (1 test). 4 of 16 surfaces
covered or extended; SL-1 + SL-5/14 also remain pinned from SL.2.

**Lessons learned (false-premise corrections):**
1. Initial preliminary target list mixed Tier-1 + Tier-3 + Effect
   paths. Source survey of SaveSystem.cs handler dispatch (line
   1105+) clarified the boundary: Effects use `SaveEffect` /
   `LoadEffect` with `FormatterServices` ctor bypass; Tier-1 has
   explicit handlers; Tier-3 hits `WritePublicFields` reflection.
   Re-scoped to pure Tier-3 BEFORE writing tests.
2. Initial helper docstring claim "factory resolves cross-entity
   references on load" was wrong — caught by source-grepping
   `reader.Factory` usage. Corrected in helper docstring + audit
   doc Findings #7.
3. RED→GREEN cycle was COMPRESSED in SL.3: tests + helper edit
   shipped together. Acceptable because (a) the helper is a thin
   pass-through to two known-correct production methods; (b) the
   contract distinction (bare vs full) is the actual
   contribution, and that's pinned by the bare-helper placeholder
   test. Documented as 🟡 in commit body.

---

### SL.4 — Tier-3 collection round-trip

**Q1 Symmetry (mirror checks):** The new
`RoundTripEntityViaTokenGraph` helper mirrors production's
`GameSessionState.Restore` flow — read both side-by-side and the
queue/dequeue/post-load-hook calls happen at the same relative
position. The helper now offers three round-trip modes (bare,
WithBodies, ViaTokenGraph) ordered by faithfulness; future
contributors can pick the right one for their Part's needs.

**Q2 Cross-feature consistency:** Test naming follows the
established pattern. New finding-type encoded into test names:
`_DoesNotInvokeOnAfterLoad_OnPrimary` makes the negated contract
(what the helper DOES NOT do) immediately legible — useful as
breaking-test bait for a future helper change.

**Q3 Counter-check completeness:**
- Empty SkillList counter-checks "buggy impl that always returns
  `[]` regardless of saved state" (would pass single-skill but
  not multi-skill).
- Subtype preservation counter-checks a buggy reflection serializer
  that always reads back as the declared base type (would FAIL on
  the abstract `BaseSkillPart` since it can't be instantiated).
- ParentEntity wiring counter-checks a buggy load path that
  forgets `part.ParentEntity = entity`.
- Helper-distinction adversarial test counter-checks a future
  change that "fixes" WithBodies to invoke OnAfterLoad (which
  would silently change the SL.2/SL.3 helper semantics).

**Q4 Doc-vs-impl drift:** Audit doc's preliminary SL.4 target list
included `Body`, `InventoryPart`, `MaterialPart`, `CampfirePart`,
`OvenSitePart`, `WellSitePart`, `HouseDramaPart`, `CorpsePart`.
Source survey re-scoped to `SkillsPart` only — most of the others
are Tier-1 (SL.7) or already covered. **Significantly narrower
final scope, with rationale for each exclusion.**

**Adversarial-sweep self-check:** Probed bug surfaces SL-5 (Generic
collection of custom-class), SL-13 (Part with private/derived state
via OnAfterLoad), SL-16 (Part-with-Part reference / identity
resolution). 6 tests, 4 contracts pinned, 1 test infra concern
documented and mitigated.

**Lessons learned (false-premise corrections):**
1. Initial assumption that `MeleeWeaponPart.OnHitEffectsCachedSpecs`
   would round-trip via reflection was wrong — verified via source
   read that it's a property + private backing field, both invisible
   to the reflection serializer. The lazy-cache pattern is the
   correct design for this case.
2. Discovered (NEW) that the previous `RoundTripEntity` /
   `RoundTripEntityWithBodies` helpers don't run `OnAfterLoad` on
   the primary entity — a gap that didn't matter for SL.2/SL.3 Parts
   but matters for `SkillsPart`. Added `RoundTripEntityViaTokenGraph`
   which routes the primary through the production token-graph load
   flow. **All existing SL.2/SL.3 tests still pass with the OLD
   helpers — they're correct for OnAfterLoad-independent Parts.**

---

## Commit history

| Commit | Sub-milestone | Notes |
|---|---|---|
| `cb10cb1` (merge) / `4ac83c6` | SL.1 | Plan to disk |
| `87ba763` (merge) / `1f79feb` | SL.2 | Tier-3 simple-Part baseline (14 tests, 9 Parts, 0 bugs, 5 contracts pinned) |
| `5f90a16` (merge) / `458b23a` | SL.3 | Tier-3 Entity-ref round-trip (10 tests, 2 Parts, 0 bugs, 6 contracts pinned, helper extended) |
| `63d4dfa` (merge) / `5577675` | SL.4 | Tier-3 collection round-trip (6 tests, 1 Part — SkillsPart, 0 bugs, 4 contracts pinned, helper extended w/ ViaTokenGraph) |
| `278558b` (merge) / `d546488` | SL.5 | Tier-3 private/internal state (7 tests, 6 Parts probed, 0 bugs found despite "highest probability" prediction, 4 contracts pinned) |

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
