# Throwing & Environmental Objects Mechanic

## Concept Summary
Create a universal **grab, carry, and throw** system that applies to the player, allies, and enemies. Any nearby environmental object can become an improvised weapon, utility tool, or elemental delivery method during combat and exploration.

This supports emergent moments such as:
- Throwing a burning log from a campfire at an ice enemy.
- Throwing rocks or light debris to interrupt enemy casts.
- Using thrown objects as a fallback action while a grimoire spell is on cooldown.

---

## Core Interaction Loop
All entities can:
1. **Pick up** nearby objects.
2. **Carry/hold** objects with possible movement/combat penalties.
3. **Throw** objects with force based on stats and object weight.

This makes the world itself part of combat decision-making rather than background decoration.

---

## Weight System (Required Baseline)
If items and objects do not currently have weights, add a baseline weight to every pickup-capable object.

### Suggested Initial Weight Classes
- **Tiny**: 1–2 (dagger, bottle)
- **Light**: 3–5 (branch, skull)
- **Medium**: 6–10 (chair, crate)
- **Heavy**: 11–18 (barrel, boulder chunk)
- **Massive**: 19+ (statue fragment)

These values can be placeholders and tuned later.

---

## Strength / Stat Gating
Use Strength (or equivalent physical power stat) to gate interaction quality.

### Suggested Rules
- **Can Lift** if `Strength >= LiftRequirement`
- **Can Throw** if `Strength >= ThrowRequirement`

If below thresholds:
- Too low to lift: cannot pick up.
- Can lift but not throw: may drag, reposition, or perform weak short-range toss.

This keeps heavy-object use meaningful and preserves build identity.

---

## Throw Force, Range, and Damage
Lighter objects should be throwable with higher speed/range, while heavier objects can deliver stronger close impact if the entity is strong enough.

### Example Formula
`ThrowForce = Strength * ThrowSkill * TechniqueModifier - (Weight * WeightPenalty)`

Use `ThrowForce` to derive:
- Projectile speed
- Max range
- Accuracy stability
- Stagger/impact behavior

Impact result can combine:
- **Mass component** (weight)
- **Velocity component** (throw force)
- **Material/shape modifier** (sharp, blunt, brittle)

---

## Elemental and Status Transfer
Environmental objects can carry status states and transfer them on impact.

### Supported Object States
- Burning
- Frozen
- Electrified
- Poison-coated
- Oily
- Blessed/Cursed (if applicable)

### Example Interaction
A **burning log** thrown at an **ice-tagged enemy** may apply:
- Impact damage
- Fire damage over time
- Bonus melt/vulnerability effects
- Possible removal of frost armor layer

This directly rewards environmental awareness and timing.

---

## Cooldown Bridging (Grimoire Synergy)
Throwing should be a meaningful tactical action while waiting for spell cooldowns.

### Design Intent
- Prevent dead time between ability casts.
- Reward adaptation to local environment.
- Create alternate skill expression outside spell rotation.

### Optional Synergy Perks
- Next throw after casting gets bonus force.
- Throwing while grimoire is cooling down reduces cooldown slightly.
- Elemental throws can prime or consume spell interactions.

---

## AI Behavior Rules
For systemic consistency, enemies and allies should use the same mechanic.

### Simple AI Priorities
1. If best ability is unavailable and target is in throw range, search for throwable object.
2. Prefer elemental counters (e.g., burning object vs. ice target).
3. Prefer light objects for interrupts and quick hits.
4. Prefer heavy objects for armored/stationary targets.
5. Avoid pickup actions when exposed or under high threat.

This helps combat feel fair and readable because all sides follow shared rules.

---

## UX and Readability
Players need clear feedback about what is possible.

### UI Feedback
- Highlight throwable objects in range.
- Show weight class and required Strength.
- Display current object state (burning/frozen/etc.).
- Show throw arc preview with quality color cues:
  - Green: full-strength throw
  - Yellow: weak/unstable throw
  - Red: cannot throw

### Failure Messaging
Example: `Too heavy (Need STR 12, have STR 9)`

---

## Progression Hooks
Perks/talents can deepen the mechanic over time.

### Example Perks
- **Power Clean**: Lift one weight class higher.
- **Quick Hands**: Faster pickup and throw windup.
- **Pitcher’s Form**: Increased range with light/medium objects.
- **Brutal Heave**: Heavy throws gain knockdown chance.
- **Elemental Grip**: Safe handling of hazardous objects.
- **Ricochet Savant**: Improved bounce shots.

---

## MVP Implementation Plan
Ship in phases.

### Phase 1 (Minimum Viable)
- Add weight values to all pickup-capable objects.
- Add Strength checks for pickup/throw.
- Add throw arc, force, and impact resolution.
- Add basic status transfer (at least burning + frozen).
- Add basic AI throwable usage during cooldown windows.

### Phase 2 (Depth)
- Add richer elemental combinations.
- Add throw-specific perks/talents.
- Add material-specific breakage and ricochet behaviors.
- Improve AI contextual choices and role-based preferences.

---

## Balancing Notes
- Start with generous throw accessibility to make the system fun early.
- Tune damage and stagger separately from spell damage to avoid replacing spell identity.
- Ensure heavy-object dominance is limited by setup time, carry penalties, and positional risk.
- Preserve build diversity by letting non-strength builds still use light tactical throws.
