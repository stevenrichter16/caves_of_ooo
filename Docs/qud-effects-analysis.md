# Caves of Qud Effects/Status Conditions System Analysis

## Architecture Overview

Effects are first-class event participants -- they extend `IComponent<GameObject>` (same base as Parts), meaning they can register for and handle events identically to Parts. They live in an `EffectRack` collection on each GameObject, parallel to the Parts collection.

---

## 1. Core Effect Class

```csharp
[Serializable]
public class Effect : IComponent<GameObject>
{
    public GameObject _Object;        // entity this effect is on
    public StatShifter _StatShifter;  // lazy helper for temp stat mods
    public Guid ID;                   // unique per instance
    public string DisplayName;        // colored string, e.g. "{{R|burning}}"
    public int Duration;              // turns remaining; 0 = expired; 9999 = indefinite
}
```

### Effect Type Bitmask

Effects are categorized via bitmask from `GetEffectType()`:

**Mechanism types (what it affects):**
| Flag | Value | Description |
|------|-------|-------------|
| TYPE_GENERAL | 1 | General |
| TYPE_MENTAL | 2 | Requires Brain |
| TYPE_METABOLIC | 4 | Requires Stomach |
| TYPE_RESPIRATORY | 8 | Requires respiratory system |
| TYPE_CIRCULATORY | 16 | Requires "Bleeds" property |
| TYPE_CONTACT | 32 | Blocked by gas/liquid/plasma phase |
| TYPE_FIELD | 64 | Field-based |
| TYPE_ACTIVITY | 128 | Activity-affecting |
| TYPE_DIMENSIONAL | 256 | Dimensional |
| TYPE_CHEMICAL | 512 | Chemical |
| TYPE_STRUCTURAL | 1024 | Structural |
| TYPE_SONIC | 2048 | Sonic |
| TYPE_TEMPORAL | 4096 | Temporal |
| TYPE_NEUROLOGICAL | 8192 | Neurological |
| TYPE_DISEASE | 16384 | Disease |
| TYPE_PSIONIC | 32768 | Psionic |
| TYPE_POISON | 65536 | Poison |
| TYPE_EQUIPMENT | 131072 | Equipment-based |

**Meta types (classification):**
| Flag | Value | Description |
|------|-------|-------------|
| TYPE_MINOR | 16777216 | Minor effect |
| TYPE_NEGATIVE | 33554432 | Harmful |
| TYPE_REMOVABLE | 67108864 | Can be cleansed |
| TYPE_VOLUNTARY | 134217728 | Self-applied |

Used for: applicability checks (mental needs Brain, metabolic needs Stomach), blanket removal (`RemoveEffectsOfType(mask)`), immunity filtering.

---

## 2. Effect Lifecycle

### Adding an Effect

```
GameObject.ApplyEffect(effect, owner)
  → Check "NoEffects" tag
  → Fire ApplyEffectEvent (parts can block, e.g. MentalShield)
  → Split stack if effect can't apply to stacks
  → Set effect._Object = this
  → Call effect.Apply(this)          // effect-specific checks, can return false
  → Add to Effects collection
  → effect.ApplyRegistrar(this)      // register event handlers
  → effect.Applied(this)             // notification hook
  → Fire EffectAppliedEvent
```

### Removing an Effect

```
GameObject.RemoveEffect(effect)
  → effect.Remove(this)             // effect-specific cleanup
  → effect.ApplyUnregistrar(this)   // unregister all event handlers
  → effect._Object = null
  → Fire EffectRemovedEvent
```

### Expiration (per-turn cleanup)

At the end of every turn, `CleanEffects()` runs:

```csharp
for each effect in Effects:
    if effect.Duration <= 0:
        effect.Expired()      // notification hook
        RemoveEffect(effect)
```

Called from `GameObject.HandleEvent(EndTurnEvent)`.

---

## 3. Duration Countdown Mechanisms

### A. Standard Duration Countdown (opt-in)

Effects that return `true` from `UseStandardDurationCountdown()` get automatic ticking:
- **Creatures with brains**: Duration decrements in `BeforeBeginTakeActionEvent` (before their turn)
- **Objects without brains**: Duration decrements in `EndTurnEvent`

Used by: Terrified, Paralyzed, BoostStatistic, cooking effects.

### B. Manual Duration Management

Effects handle their own countdown in event handlers:
- **Stun**: Decrements only if save fails each turn
- **Poisoned**: Decrements in `BeforeBeginTakeActionEvent`
- **Bleeding**: Never auto-decrements; uses save-based recovery

### C. Indefinite (Duration = 9999)

Never auto-expires. Used for equipment bonuses, permanent conditions.

---

## 4. StatShifter — Temporary Stat Modifications

The canonical way effects modify stats. Each effect gets a lazy-initialized `StatShifter`.

```csharp
StatShifter:
  SetStatShift(target, statName, amount)   // create or update shift
  RemoveStatShifts()                        // remove ALL shifts this created
```

Each shift is tracked by Guid so it can be precisely added/removed without interfering with other effects modifying the same stat.

**Common pattern in effects:**
```csharp
Apply():   StatShifter.SetStatShift("DV", -12)    // Asleep: DV -12
Remove():  StatShifter.RemoveStatShifts(Object)    // clean restoration
```

**Examples:**
- Stun: DV → 0
- Confused: DV -Level, MA -Level, Willpower/Intelligence/Ego -MentalPenalty
- Paralyzed: DV → 0
- Asleep: DV -12

---

## 5. Concrete Effects

### Poisoned
- **Type**: NEGATIVE | REMOVABLE | METABOLIC | CIRCULATORY | POISON
- **Duration**: 4-10 turns, manual countdown
- **Damage**: `(DamageIncrement)/2` per turn (e.g. "3d3"/2)
- **Side effects**: Blocks regeneration entirely, halves all healing
- **On expiry**: Chains to Ill effect
- **Stacking**: Extends duration/upgrades damage of existing poison (no duplicate)
- **Render**: Animated green heart glyph, frames 35-45 of 60-frame cycle

### Burning
- **Type**: NEGATIVE | CHEMICAL
- **Lightweight**: Primarily a display marker
- **Damage**: Driven by Physics temperature system, not the effect itself
- **Temperature-driven**: Flame point → damage scales with temp delta (1-6 range)

### Bleeding
- **Type**: NEGATIVE | REMOVABLE | CIRCULATORY | ACTIVITY
- **Duration**: Always 1 (never expires by countdown)
- **Recovery**: Save-based — `MakeSave("Toughness", SaveTarget)` each turn; SaveTarget decreases by 1/turn
- **Stacking**: Upgrades existing bleed's damage/save if Stack=true, or allows multiple bleeds
- **Damage**: Configurable dice string (e.g. "1d2-1"), applied via TakeDamage each turn
- **Side effects**: Creates blood splashes/pools in cell
- **Bandaging**: Has `Bandaged` flag; responds to "Recuperating" event
- **Render**: Red heart glyph, frames 45-55

### Stun
- **Type**: NEGATIVE | REMOVABLE | ACTIVITY | MENTAL
- **Action**: Calls `ForfeitTurn()` in `BeginTakeActionEvent` — prevents all action
- **Save**: `MakeSave("Toughness", SaveTarget, "Stun")` each turn; if passed, Duration = 0
- **Stacking**: Extends duration + upgrades save target
- **Stats**: DV → 0 via StatShifter
- **Mobility**: Returns false for "IsMobile"
- **Render**: "!" with "&C^c" (cyan), frames 10-25

### Confused
- **Type**: NEGATIVE | REMOVABLE | ACTIVITY | MENTAL
- **No stacking**: Returns false if already confused
- **Behavior hijack**: 50% chance random movement, 10% chance attack random nearby creature
- **Stats**: DV -Level, MA -Level, Willpower/Intelligence/Ego -MentalPenalty
- **Render**: "?" with "&R" (red)

### Frozen
- **Type**: NEGATIVE | STRUCTURAL
- **Simple**: Status marker + movement mode change
- **Temperature-driven**: Physics system handles freeze/thaw cycle
- **Stack-safe**: Can apply to entire item stack

### Terrified
- **Type**: NEGATIVE | REMOVABLE | ACTIVITY | MENTAL (optionally + PSIONIC)
- **Standard duration countdown**
- **AI**: Pushes Flee goal onto Brain's goal stack
- **Source tracking**: Terrified OF a specific object or location
- **On remove**: Fails the flee goal

### Paralyzed
- **Type**: NEGATIVE | REMOVABLE | METABOLIC | MENTAL
- **Standard duration countdown**
- **Action**: Sets `PreventAction = true` in `BeginTakeActionEvent`
- **Stats**: DV → 0
- **Stacking**: Extends duration
- **Mobility**: Blocks IsMobile, CanChangeBodyPosition, CanMoveExtremities
- **Render**: "X" with "&C^c", frames 15-30

### Asleep
- **Stats**: DV -12
- **Combat**: Attackers get +4 penetration bonus
- **Wake on damage**: Wakes up when hit, chains to Dazed effect
- **Voluntary sleep**: Duration 9999 for resting
- **Chains to**: Dazed (forced wake), Prone (on application), Wakeful (natural wake)
- **Render**: "z" with "&C^c", frames 10-25

---

## 6. Stacking & Immunity

### Stacking Patterns

Each effect defines its own stacking behavior in `Apply()`:

| Pattern | Example | Behavior |
|---------|---------|----------|
| Extend + upgrade | Stun, Poisoned | Adds duration, upgrades save/damage of existing |
| No stacking | Confused | Returns false if already present |
| Custom stacking | Bleeding | Upgrades matching bleeds OR allows multiples |

### Immunity Layers

1. **Type-based**: Mental effects need Brain, metabolic needs Stomach, circulatory needs "Bleeds"
2. **Event-based (ApplyEffectEvent)**: Parts like MentalShield block named effects
3. **Effect-specific events**: e.g. `"CanApplyConfusion"`, `"CanApplySleep"`, `"ApplyPoison"`
4. **Saving throws**: On-hit effects require saves (e.g. Toughness save vs poison strength)

---

## 7. Combat Integration

### Applying Effects on Hit

Weapon/creature Parts register for hit events and apply effects:

```
StunOnHit:   registers "WeaponHit" / "WeaponThrowHit"
PoisonOnHit: registers "AttackerHit" (creatures) or "WeaponHit" (weapons)

On hit:
  → chance% roll
  → phase match check
  → defender.ApplyEffect(new Effect(...))
```

### Effects Modifying Combat

- **Asleep**: +4 penetration bonus to attackers via `GetDefenderHitDiceEvent`
- **Stun/Paralyzed**: DV = 0 via StatShifter makes target trivially hittable
- **Poisoned**: Halves healing via "Healing" event, blocks regen via "Regenerating" event

---

## 8. Rendering

Effects override `Render(RenderEvent E)` with frame-animated glyph/color swaps:

```csharp
int frame = CurrentFrame % 60;
if (frame > 10 && frame < 25) {
    E.Tile = null;
    E.RenderString = "!";        // override glyph
    E.ColorString = "&C^c";      // override color
}
```

This creates blinking/flashing where the effect glyph appears for part of the cycle and the normal entity glyph shows the rest. Each effect has its own frame window, glyph, and color.

| Effect | Glyph | Color | Frames |
|--------|-------|-------|--------|
| Stun | ! | &C^c (cyan) | 10-25 |
| Asleep | z | &C^c | 10-25 |
| Paralyzed | X | &C^c | 15-30 |
| Bleeding | ♥ (U+0003) | &r (red) | 45-55 |
| Poisoned | ♥ (U+0003) | &G^k (green) | 35-45 |
| Confused | ? | &R (red) | indicator overlay |

---

## 9. Serialization

Effects are saved/loaded with their GameObjects:

**Save**: type token → Guid → DisplayName → Duration → StatShifter data → effect-specific data
**Load**: reconstruct via `Activator.CreateInstance(type)` → restore fields → re-register event handlers

StatShifter persists its full `objectID → (statName → shiftGuid)` dictionary so stat modifications survive save/load.

---

## 10. Key Design Patterns for Implementation

1. **Effects are event participants** — same registration/handling as Parts
2. **Effects live parallel to Parts** — separate collection, iterated during rendering and event dispatch
3. **StatShifter for temp stats** — Guid-tracked shifts, precisely removable, survives save/load
4. **Stacking is effect-defined** — each effect decides in `Apply()` whether to stack, extend, or reject
5. **Immunity is layered** — type bitmask → event gates → effect-specific events → saving throws
6. **Duration is flexible** — auto (standard countdown), manual (save-based), or indefinite (9999)
7. **Rendering is frame-animated** — `CurrentFrame % 60` creates blinking effect indicators
8. **Combat integration via events** — weapon Parts apply effects on hit; effects modify combat stats
9. **Cleanup is centralized** — `CleanEffects()` at end of every turn handles expiration
