# Authoring a Skill

How to add or modify a weapon-class skill (or any skill) in CoO,
post-WSP3 system rewrite. **Modifying an existing skill = editing
that skill's `.cs` file.** Authoring a new one = one new `.cs` + one
JSON entry. No central dispatcher to update.

This guide walks three patterns end-to-end:

1. **Passive on-hit** — fires when a successful melee hit lands
2. **Passive on-miss / defender-side** — fires when a swing misses
3. **Active ability** — player presses a key, fires a command

Plus the architecture-at-a-glance + a list of every virtual you can
override.

---

## Architecture at a glance

```
                         CombatSystem.PerformSingleAttack
                                     │
                ┌────────────────────┼────────────────────┐
                │                    │                    │
        miss path             hit path                 (other)
                │                    │
                ▼                    ▼
        SkillEventDispatcher.AttackerMeleeMiss
        SkillEventDispatcher.DefenderAfterAttackMissed
        SkillEventDispatcher.AttackerAfterAttack
        SkillEventDispatcher.WeaponMadeCriticalHit  (only on Critical)
                │
                ▼
     iterates attacker's (or defender's) SkillsPart.SkillList
                │
                ▼
   each BaseSkillPart's matching virtual override
```

`BaseSkillPart` (in `Assets/Scripts/Gameplay/Skills/BaseSkillPart.cs`) is
the abstract base every skill subclasses. It exposes virtuals for the
common combat events; default implementations are no-op so subclasses
override only the ones they care about.

`SkillEventDispatcher` (`SkillEventDispatcher.cs`) is the static
router CombatSystem fires events through. **Don't modify it to add
a skill.** Add the skill class + JSON entry; the dispatcher already
routes to it.

`SkillEventContext` (`SkillEventContext.cs`) is the param object
carried into every event. Has Attacker, Defender, Weapon, WeaponEntity,
Damage, ActualDamage, Zone, Rng, Properties.

`SkillsPart.TryRouteSkillCommand(...)` (`SkillsPart.cs`) is the entry
point for active-ability commands — input/AI calls it with a
command string + zone + rng; SkillsPart looks up the owning skill
and invokes its `OnCommand(ctx)`.

---

## Pattern 1 — Passive on-hit proc

Adds a chance-gated effect-apply on successful melee hits. Used by:
Cudgel_Bludgeon, LongBlades_Lacerate, ShortBlades_Jab,
ShortBlades_Bloodletter, Cudgel_Hammer, Cudgel_ShatteringBlows,
ShortBlades_Hobble, Axe_Cleave.

### Example — `Cudgel_Bludgeon`

```csharp
// Assets/Scripts/Gameplay/Skills/Cudgel_Bludgeon.cs
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    public class Cudgel_Bludgeon : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Bludgeon);

        public const int CHANCE_PERCENT = 50;
        public const int DURATION_MIN = 3;
        public const int DURATION_MAX = 4;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            // Gate on damage class + actual-damage > 0 + null-safety.
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            // Chance roll.
            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            // Apply effect.
            int duration = ctx.Rng.Next(DURATION_MIN, DURATION_MAX + 1);
            ctx.Defender.ApplyEffect(new StunnedEffect(duration), ctx.Attacker, ctx.Zone);
        }
    }
}
```

### JSON entry (in `Assets/Resources/Content/Data/Skills/Cudgel.json`)

```json
{
  "Name": "Bludgeon",
  "Class": "Cudgel_Bludgeon",
  "Cost": 1,
  "Description": "Cudgel-class hits gain a substantial chance to apply Stunned.",
  "Foreground": "y",
  "Detail": "Y"
}
```

Add this to the `Powers[]` array of the matching tree's entry. The
`Class` field must match the C# class name exactly (used by reflection).

### To modify the skill
Edit `Cudgel_Bludgeon.cs`. Change `CHANCE_PERCENT`, swap
`StunnedEffect` for a different effect, change the gate from
`"Cudgel"` to `"Bludgeoning"`, etc. No other file needs to change.

---

## Pattern 2 — On-miss / defender-side passive

Cudgel_Backswing fires from `OnAttackerMeleeMiss`. ShortBlades_Rejoinder
fires from `OnDefenderAfterAttackMissed`. Both call
`CombatSystem.PerformSingleAttack` for the re-attack/counter-attack;
both use an instance-level `_recurring` flag as a recursion guard.

### Example — `Cudgel_Backswing`

```csharp
public class Cudgel_Backswing : BaseSkillPart
{
    public override string Name => nameof(Cudgel_Backswing);
    public const int CHANCE_PERCENT = 25;

    [System.NonSerialized] private bool _recurring;

    public override void OnAttackerMeleeMiss(SkillEventContext ctx)
    {
        if (_recurring) return;          // recursion guard
        if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;
        if (ctx.Weapon == null || string.IsNullOrEmpty(ctx.Weapon.Attributes)) return;
        if (!ctx.Weapon.Attributes.Contains("Cudgel")) return;
        if (ctx.Rng == null || ctx.Zone == null) return;

        if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

        _recurring = true;
        try {
            CombatSystem.PerformSingleAttack(
                attacker: ctx.Attacker, defender: ctx.Defender,
                weapon: ctx.Weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(Backswing)");
        } finally { _recurring = false; }
    }
}
```

The `_recurring` flag prevents Backswing-of-a-Backswing infinite
recursion. Use the same pattern for any skill that re-enters the
combat path.

---

## Pattern 3 — Active ability

Adds a player-triggerable command (cooldown, hotbar slot). Used by
Cudgel_Conk, Axe_Berserk.

### Example — `Axe_Berserk`

```csharp
public class Axe_Berserk : BaseSkillPart
{
    public override string Name => nameof(Axe_Berserk);
    public const int COOLDOWN = 100;
    public const int DURATION = 5;

    // 1. Declare the ability — runs at AddSkill time.
    public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
    {
        return new ActivatedAbilitySpec
        {
            DisplayName = "Berserk!",
            Command = "CommandAxeBerserk",
            Class = "Skills",
            TargetingMode = AbilityTargetingMode.SelfCentered,
            Range = 1,
            Cooldown = COOLDOWN,
        };
    }

    // 2. Handle the command — runs when the player triggers it.
    public override void OnCommand(SkillEventContext ctx)
    {
        // Null-guard idiom for active abilities: explicit form mirrors
        // the shipped skills (Cudgel_Conk, Axe_Berserk). On-hit skills
        // use the chained `if (ctx?.Damage == null || ...)` form because
        // every on-hit skill needs Damage. Active-ability and on-miss
        // skills don't have a Damage object (the miss path doesn't roll
        // damage; activated abilities run before any swing) so they use
        // the explicit `ctx == null || ctx.Attacker == null` form on the
        // fields they actually consume. Rule of thumb: fail-fast on the
        // fields your skill reads.
        if (ctx == null || ctx.Attacker == null) return;
        var actor = ctx.Attacker;

        // Gate on weapon class.
        var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
        if (weapon == null) {
            MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to go berserk.");
            return;
        }

        // Apply the buff.
        actor.ApplyEffect(new BerserkEffect(DURATION), actor, ctx.Zone);
    }
}
```

`SkillsPart.AddSkill` calls `DeclareActivatedAbility` automatically;
non-null spec → registers on the actor's `ActivatedAbilitiesPart`.
Cooldown is applied automatically after `OnCommand` returns.

The input system / AI dispatches commands via
`actor.GetPart<SkillsPart>().TryRouteSkillCommand(commandString,
zone, rng)`.

### JSON entry

Same shape as passive — Cost / Description / glyphs in JSON, behavior
in `.cs`. The JSON doesn't know it's an active ability.

---

## All available virtual hooks on `BaseSkillPart`

| Override | Fires from | Use for |
|---|---|---|
| `OnAttackerAfterAttack(ctx)` | post-damage block, `hpAfter > 0` | on-hit procs (damage class gating, chance roll, effect apply) |
| `OnAttackerMeleeMiss(ctx)` | miss path, before early-return | on-miss skills (Backswing) |
| `OnDefenderAfterAttackMissed(ctx)` | miss path, on the defender's skill list | dodge counters (Rejoinder) |
| `OnWeaponMadeCriticalHit(ctx)` | post-damage block, only when `Critical` attribute set | tree-root crit-only effects |
| `OnGetToHitModifier(actor, weapon)` | hit-roll calc, totalled across all owned skills | Expertise +to-hit |
| `DeclareActivatedAbility(actor)` | `SkillsPart.AddSkill` after the AddSkill lifecycle hook | declare a command + cooldown for active abilities |
| `OnCommand(ctx)` | `SkillsPart.TryRouteSkillCommand` when a matching command fires + cooldown elapsed | execute the active ability |
| `AddSkill(entity)` | `SkillsPart.AddSkill` at attach time | apply passive stat-shifts (`StatShifter.SetStatShift`), register custom hooks |
| `RemoveSkill(entity)` | `SkillsPart.RemoveSkill` before detach | undo whatever AddSkill did |

The activated-ability lifecycle (`DeclareActivatedAbility` →
`ActivatedAbilitiesPart.AddAbility` → `OnCommand` → cooldown apply
→ `RemoveAbility` on RemoveSkill) is fully handled by `SkillsPart`
— skill subclasses don't need to call any of those directly.

---

## Helpers you can reuse

`SkillCombatHelpers` (`SkillCombatHelpers.cs`):
- `FindAdjacentCleaveTarget(defender, attacker, zone)` — first
  Creature in 8-direction order, excluding attacker. Used by
  Axe_Cleave / AxeSkill / Cudgel_Conk.
- `ExecuteCleave(actualDamage, defender, attacker, zone)` — find
  + half-damage.
- `FindEquippedWeaponOfClass(actor, requiredAttribute)` — first
  equipped melee weapon whose Attributes contain the substring.
  Used by active-ability skills to gate on weapon class.

`SkillEventDispatcher` (`SkillEventDispatcher.cs`):
- `GetSkillHitModifier(attacker, weapon)` — sum of every owned
  skill's `OnGetToHitModifier` return. CombatSystem already calls
  this in the hit-roll math; you don't need to invoke directly
  unless writing a non-combat path that wants the same modifier.

---

## What if I need a new event?

If the event you want to fire (e.g. "OnEquip", "OnTurnStart") doesn't
have a matching virtual on `BaseSkillPart`, the steps are:

1. Add a new virtual on `BaseSkillPart` with a default no-op body.
2. Add a new entry-point method on `SkillEventDispatcher` that
   iterates `SkillList` and calls the new virtual.
3. Wire the call site (CombatSystem, TurnManager, EquipmentSystem,
   wherever) to invoke `SkillEventDispatcher.YourNewEvent(...)`.

The pattern is mechanical; see `WeaponMadeCriticalHit` (the most
recent addition) as a copy-template.

---

## Common gotchas

- **Stub classes need a `Name` override.** `BaseSkillPart` inherits
  from `Part` which has `Name => GetType().Name` by default — so
  the override is redundant but conventional. Existing skills all
  have it; mirror them.
- **JSON `Class` field must match C# class name exactly.** It's
  resolved via reflection (`Type.GetType("CavesOfOoo.Skills." +
  className)`).
- **Rng matters for tests.** Forward `ctx.Rng` into effect ctors
  that take an rng (e.g. `BleedingEffect`) so seeded tests stay
  deterministic.
- **Don't forget the `Critical` attribute.** Tree-root crit hooks
  fire from `OnWeaponMadeCriticalHit`, NOT `OnAttackerAfterAttack`.
  Conversely: power skills using `OnAttackerAfterAttack` will fire
  on crit too (since crits are also AttackerAfterAttack events) —
  if you want a power that DOESN'T fire on crit, gate explicitly
  with `if (ctx.Damage.HasAttribute("Critical")) return;`.
- **Recursion guards on re-attack skills.** Backswing / Rejoinder
  call `PerformSingleAttack` which re-enters the dispatcher. Use
  an instance `_recurring` flag (per the Pattern 2 example) so a
  Backswing-triggered miss doesn't trigger another Backswing.
- **Ability cooldown is automatic.** `SkillsPart.TryRouteSkillCommand`
  applies `CooldownRemaining = MaxCooldown` after `OnCommand`
  returns. If your skill wants to suppress the cooldown (e.g. on a
  failed targeting popup), set `ability.CooldownRemaining = 0`
  before returning from `OnCommand`.

---

## Reference: shipped skills as templates

| Pattern | Reference skill |
|---|---|
| On-hit proc, status apply | `Cudgel_Bludgeon` |
| On-hit proc, stronger dice Bleed | `LongBlades_Lacerate` |
| On-hit proc, second-target damage | `Axe_Cleave` (uses SkillCombatHelpers) |
| On-hit proc, item-targeted effect | `Cudgel_Hammer` |
| Tree-root crit hook | `CudgelSkill`, `AxeSkill`, etc. |
| +to-hit passive | `Cudgel_Expertise` |
| On-miss re-attack | `Cudgel_Backswing` |
| On-defender-missed riposte | `ShortBlades_Rejoinder` |
| Active targeted strike | `Cudgel_Conk` |
| Active self-buff | `Axe_Berserk` |

Copy the closest pattern, rename, change the constants and effect,
add the JSON entry. Done.
