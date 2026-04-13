# Hybrid `GripType` + `UsesSlots` Plan

## Decision
Keep `UsesSlots` as the authoritative low-level equip occupancy system, and add `GripType` as the high-level handling system for pickup, carry, throw, UI, and AI.

That means:
- `UsesSlots` answers: what body parts does this item occupy?
- `GripType` answers: how is this object handled in the world?

This preserves the current anatomy/equip planner in `EquippablePart`, `EquipPlan`, and the slot rules, while giving weapons and throwable props one shared handling model.

## Data Model

### New enum
Add a small enum in gameplay code, for example:
- `OneHand`
- `TwoHand`
- `Awkward`
- `DragOnly`

Candidate file:
- `Assets/Scripts/Gameplay/Items/GripType.cs`

### New shared item part
Add a new part for handling and throwing metadata, separate from `EquippablePart`.

Candidate shape:

```csharp
public sealed class HandlingPart : Part
{
    public GripType GripType = GripType.OneHand;
    public bool Carryable = true;
    public bool Throwable = true;
    public int Weight = 1;
    public string BulkClass = "Light";
    public int MinLiftStrength = 0;
    public int MinThrowStrength = 0;
    public int CarryMovePenalty = 0;
    public int ThrowAccuracyPenalty = 0;
    public int ThrowForceBonus = 0;
}
```

Candidate file:
- `Assets/Scripts/Gameplay/Items/HandlingPart.cs`

### Existing `EquippablePart` stays
Do not remove:
- `Slot`
- `UsesSlots`

Add a helper only:

```csharp
public string[] GetDerivedSlotsFromGripType(HandlingPart handling)
```

But keep explicit `UsesSlots` authoritative.

### Blueprint authoring rules
For weapons and throwable objects:

- If explicit `UsesSlots` is present, use it.
- If `UsesSlots` is absent and `GripType` exists, derive defaults:
  - `OneHand` -> `Hand`
  - `TwoHand` -> `Hand,Hand`
  - `Awkward` -> `Hand,Hand`
  - `DragOnly` -> no hand equip by default
- If neither exists, preserve current fallback behavior.

Example weapon:

```json
{
  "Name": "Warhammer",
  "Parts": [
    { "Name": "Equippable", "Params": [{ "Key": "Slot", "Value": "Hand" }] },
    { "Name": "Handling", "Params": [
      { "Key": "GripType", "Value": "TwoHand" },
      { "Key": "Carryable", "Value": "true" },
      { "Key": "Throwable", "Value": "true" },
      { "Key": "Weight", "Value": "16" },
      { "Key": "BulkClass", "Value": "Heavy" }
    ]}
  ]
}
```

Example environmental prop:

```json
{
  "Name": "BurningLog",
  "Parts": [
    { "Name": "Handling", "Params": [
      { "Key": "GripType", "Value": "TwoHand" },
      { "Key": "Carryable", "Value": "true" },
      { "Key": "Throwable", "Value": "true" },
      { "Key": "Weight", "Value": "18" },
      { "Key": "BulkClass", "Value": "Heavy" }
    ]}
  ]
}
```

Example drag-only prop:

```json
{
  "Name": "StoneStatueChunk",
  "Parts": [
    { "Name": "Handling", "Params": [
      { "Key": "GripType", "Value": "DragOnly" },
      { "Key": "Carryable", "Value": "true" },
      { "Key": "Throwable", "Value": "false" },
      { "Key": "Weight", "Value": "40" },
      { "Key": "BulkClass", "Value": "Massive" }
    ]}
  ]
}
```

## Runtime Responsibilities

### Equip flow
Do not rewrite the planner.

Keep current flow:
- `EquippablePart` -> slot array
- planner claims body parts
- inventory equips to one or many parts

Small change only:
- when `UsesSlots` is empty, allow `EquippablePart` to derive slot defaults from `HandlingPart.GripType`

This keeps all existing anatomy support intact.

### Throw / carry flow
Use `HandlingPart` as the shared entry point for:
- can pick up
- can carry
- can throw
- required strength
- penalties
- AI utility
- UI labels

That means both:
- a dagger
- a warhammer
- a crate
- a burning log

all go through the same handling query surface.

### Suggested query service
Add a small service/helper instead of scattering logic.

Candidate file:
- `Assets/Scripts/Gameplay/Items/HandlingService.cs`

Suggested API:

```csharp
public static class HandlingService
{
    public static GripType GetGripType(Entity item);
    public static bool IsCarryable(Entity item);
    public static bool IsThrowable(Entity item);
    public static int GetWeight(Entity item);
    public static bool CanLift(Entity actor, Entity item, out string reason);
    public static bool CanThrow(Entity actor, Entity item, out string reason);
    public static string[] GetDefaultSlots(Entity item);
}
```

## Authoring Rules

### Designer-facing rule
Designers should think in this order:
1. What is this object's handling class?
2. Is it carryable / throwable?
3. Does it need a custom slot override?

### Defaulting policy
Use defaults for most content:
- dagger: `GripType.OneHand`
- longsword: `GripType.OneHand`
- warhammer: `GripType.TwoHand`
- great axe: `GripType.TwoHand`
- barrel: `GripType.Awkward`
- statue chunk: `GripType.DragOnly`

Only author explicit `UsesSlots` for exceptions:
- offhand-only items
- floating items
- abstract anatomy interactions
- creatures with unusual body part requirements

## Implementation Steps

### Phase 1: Introduce `GripType` safely
- Add `GripType` enum.
- Add `HandlingPart`.
- Add parsing support in blueprint loading.
- Add `HandlingService`.
- No behavior change yet for existing items unless they opt in.

### Phase 2: Bridge `GripType` into equip defaults
- Update `EquippablePart` so missing `UsesSlots` can derive from `HandlingPart`.
- Keep explicit `UsesSlots` higher priority.
- Add warnings for invalid combos:
  - `GripType.TwoHand` + `UsesSlots=Hand`
  - `GripType.DragOnly` + equippable hand slots
  - non-carryable + throwable

### Phase 3: Move throw/carry rules onto `HandlingPart`
- Implement lift and throw gates using `HandlingPart.Weight`, `GripType`, and actor Strength.
- Update throw targeting/command code to use `HandlingService`.
- Replace hardcoded weapon vs prop assumptions with handling queries.

### Phase 4: Migrate content
- Add `HandlingPart` to core weapon blueprints in `Assets/Resources/Content/Blueprints/Objects.json`.
- Start with:
  - daggers
  - swords
  - axes
  - hammers
  - shields
  - a few environmental props
- Keep existing `UsesSlots` on all current two-hand weapons during migration.
- Only remove redundant `UsesSlots` later if you want the data cleaner.

### Phase 5: UI and AI
- Inventory/equip UI shows grip class.
- Throw preview uses `GripType` for messaging:
  - `one-hand`
  - `two-hand`
  - `awkward`
  - `drag only`
- AI throwable evaluation uses the same handling queries.

## Validation Rules
Add a content validator so bad combinations are caught early.

Examples:
- `GripType.DragOnly` cannot also be `Throwable=true`
- `GripType.TwoHand` with no `EquippablePart` is fine for props
- `EquippablePart` without explicit `UsesSlots` and no `GripType` falls back to current behavior
- `GripType.Awkward` should usually have nonzero penalties
- `Carryable=false` and `Throwable=true` should fail validation

## Pros of This Hybrid
- Unifies weapons and throwable props under one handling language.
- Preserves the current body/anatomy system.
- Keeps weird anatomies and future mutations safe.
- Makes content easier to author.
- Gives AI/UI one consistent source of truth for handling feel.

## Cons / Costs
- Temporary duplication during migration.
- Need clear precedence rules so designers are not confused.
- Validation is required, otherwise `GripType` and `UsesSlots` will drift.

## Test Plan
- `EquippablePart` derives `Hand` from `GripType.OneHand` when `UsesSlots` is missing.
- `EquippablePart` derives `Hand,Hand` from `GripType.TwoHand`.
- Explicit `UsesSlots` overrides derived defaults.
- `DragOnly` items cannot equip to hand slots by default.
- Existing warhammer/dagger behavior remains unchanged during migration.
- Throw/carry gates use `HandlingPart`, not ad hoc weapon checks.
- Content validator catches contradictory data.

## Recommended Migration Default
Do this conservatively:
- keep all current weapon `UsesSlots`
- add `GripType` and `HandlingPart`
- use `GripType` immediately for throw/carry/UI/AI
- only later let `GripType` auto-derive equip slots for simpler items

That gives you unification without risking regressions in the working equip system.
