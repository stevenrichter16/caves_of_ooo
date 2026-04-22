# Throwable Objects & Strength-Gated Physics Combat

## Design Goal
Enable **all entities** (player, allies, enemies) to pick up environmental objects and throw them as a tactical fallback when abilities are unavailable (e.g., grimoire cooldowns).

## Core Loop
1. **Scan nearby objects** with `Carryable` + `Throwable` tags.
2. **Attempt pickup** based on Strength-vs-weight rules.
3. **Hold object** in arms/hands as a temporary equipped state.
4. **Throw object** with force derived from surplus Strength and throw skill modifiers.
5. **Resolve impact** as kinetic + material payload (burning, freezing, toxic, etc.).
6. **Apply secondary effects** to target, tile, and nearby actors.

---

## Universal Weight System
If objects do not currently have weight, define a baseline for every item/world prop.

### Weight fields
- `WeightKg` (float): true mass used for pickup/throw checks.
- `BulkClass` (Tiny, Light, Medium, Heavy, Massive): animation and handling category.
- `GripType` (OneHand, TwoHand, Awkward, DragOnly): controls pickup speed and stance.

### Fast defaulting strategy
- Items missing explicit weight receive defaults by category (book, bottle, sword, crate, log, statue fragment).
- Existing inventory stackables can use per-unit weight.
- Environment props can expose destructured weights (e.g., broken pillar chunk becomes throwable rubble pieces).

---

## Strength-Gated Pickup & Throw Rules

### Pickup gate
`CanLift = (StrengthScore * LiftFactor) >= EffectiveWeight`

Where `EffectiveWeight = WeightKg * condition modifiers` (wet, frozen, burning, awkward shape).

If failed:
- Cannot pick up (or only drag if `DragOnly`).
- UI feedback: “Too heavy for your current Strength.”

### Throw gate
`CanThrow = CanLift AND ThrowableTag == true`

Large/awkward objects may require charge-up or two hands even if lift succeeds.

### Throw force scaling
Lighter objects should be throwable with more force when Strength exceeds requirement:

`Surplus = max(0, StrengthCapacity - EffectiveWeight)`

`ThrowVelocity = BaseVelocity * (1 + Surplus * VelocityScale)`

`ImpactKinetic = WeightKg * ThrowVelocity * KineticScale`

This creates the desired behavior:
- Heavy object: short range, high stagger if it lands.
- Light object: long range, high hit chance, can still do strong impact with high Strength.

---

## Material Payload System (Elemental Logic)
Thrown objects carry material state into combat.

### Suggested payload tags
- `OnFire`
- `Frozen`
- `Electrified`
- `Toxic`
- `Oily`
- `Explosive`

### Example interaction
**Burning log -> Ice enemy**
- On hit: kinetic damage + heat payload.
- If target has `Ice` tag, apply bonus melt/shatter damage.
- Nearby tiles can gain temporary `FireSurface` state.

This supports the “use environment while fire grimoire is cooling down” fantasy.

---

## Combat Use Cases

### Player fallback during cooldowns
- While grimoire is cooling down, player throws nearby environmental objects.
- Encourages battlefield scanning and opportunistic play.

### Enemy behavior
- Brutes prioritize heavy throws at short range.
- Skirmishers throw light debris from cover.
- Elemental enemies prefer objects matching their element state.

### Ally behavior
- Allies can feed combos: throw oil jar first, player ignites follow-up.

---

## AI Decision Heuristics
Score each candidate object:

`Score = (HitChance * TotalDamageValue + UtilityValue - RiskValue) / ActionCost`

Where:
- `TotalDamageValue = kinetic + elemental bonus + status potential`
- `UtilityValue` includes knockback, terrain denial, interrupt chance
- `RiskValue` includes friendly-fire, self-burn, losing cover

AI should also consider:
- Distance to object pickup
- Time to equip/throw
- Whether ability cooldowns are active

---

## Status + Crowd Control Hooks
Thrown impacts can drive tactical control, not just damage:
- Knockback / knockdown based on impact threshold
- Stagger and cast interruption
- Limb injury chance for very high kinetic impacts
- Ground effects (fire patch, ice slick, poison puddle)

---

## Animation & UX Notes
- Distinct pickup tiers: quick scoop, strain lift, overhead heave.
- Clear silhouette when carrying (readable in combat clutter).
- Throw arc preview tinted by confidence (green = likely hit, red = blocked).
- On failed pickup, show strength requirement and current effective capacity.

---

## Data Model Sketch
```yaml
ThrowableObject:
  id: burning_log
  weightKg: 18.0
  bulkClass: Heavy
  gripType: TwoHand
  throwable: true
  baseVelocity: 8.0
  kineticScale: 1.2
  materialTags: [Wood, Fire]
  onHit:
    - applyDamage: {type: Blunt, scale: Kinetic}
    - applyStatus: Burning
    - conditionalBonus:
        ifTargetTag: Ice
        bonusType: ThermalShatter
```

---

## Balance Guardrails
- Cap max throw range so ranged weapons/spells keep identity.
- Add stamina/energy cost that rises with weight.
- Prevent infinite environmental ammo by limiting pickable prop density or respawn rates.
- Let some bosses resist knockback to avoid hard-lock loops.

---

## Incremental Implementation Plan
1. **Add weight defaults** to all item/object definitions.
2. Add `Carryable` / `Throwable` components and pickup state machine.
3. Implement Strength-gated lift + throw formulas.
4. Route throw impacts through existing damage + status systems.
5. Add material payload interactions (`Fire` vs `Ice` first).
6. Expand AI object-throw behavior trees.
7. Tune range/damage/stamina and add encounter content built around this system.

---

## Quick Prototype Scenarios
1. **Campfire duel**: player and ice enemy both contest burning logs as improvised weapons.
2. **Cooldown bridge**: fire grimoire unavailable for 8s, throwable objects sustain pressure.
3. **Strength fantasy test**: high-Strength build throws pebbles like bullets; low-Strength build uses bottles/tools.
4. **Ally combo test**: ally throws oil, player follows with spark spell.
