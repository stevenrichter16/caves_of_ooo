# Weapon-class skills (WS — Qud-parity entry tier)

Continuation of the skill-tree v1 (`Docs/SKILL-TREE-QUD-PARITY.md`).
Adds one tree per weapon class with a tree-root + a foundational
power, ALL priced at **1 SP** with no Minimum / Requires / Exclusion —
the user's directive: "every weapon-type skill should have no
requirement other than costing 1sp." Affordable from any starting
state; lets a freshly-leveled player buy several at once and feel
each weapon class develop a distinct combat identity.

## Pre-impl verification sweep

| Premise | Status | Source |
|---|---|---|
| CoO weapons declare sub-class attributes via `MeleeWeaponPart.Attributes` (e.g. "Cutting LongBlades") | ✅ | `Objects.json:1160` LongSword, `:1195` Battleaxe, `:1337` Mace |
| CoO weapons have these attribute classes: Cudgel (Mace/Cudgel/Warhammer/OldWorldPipe), Axe (Battleaxe/Hatchet), LongBlades (LongSword/Greatsword/ShortSword/Claymore/Sporeblade), Piercing (Dagger/Spear/ChoirSpine — no sub-class) | ✅ | `Objects.json` weapon survey |
| `OnHitClassEffects.Apply(damage, actualDamage, defender, attacker, zone, rng)` is called from `CombatSystem.PerformSingleAttack` post-`ApplyDamage`, inside the existing `if (hpAfter > 0)` block | ✅ | `OnHitClassEffects.cs:59`; `CombatSystem.cs` (verified by lineage) |
| `Damage.HasAttribute(name)` and `Damage.IsBludgeoningDamage()` exist | ✅ | `Damage.cs:99-103` |
| `StunnedEffect`, `BleedingEffect`, `ConfusedEffect` exist + ApplyEffect path is `target.ApplyEffect(effect, source, zone)` | ✅ | `Effects/Concrete/`, `Entity.cs:291-293` |
| `SkillsPart.HasSkill(string class)` is the canonical "does actor own this skill" check | ✅ | `SkillsPart.cs` (used by `BuySkillAction.HasSkill`) |
| `BaseSkillPart` is the abstract base for concrete skill C# classes; `AcrobaticsSkill` / `AcrobaticsDodgePower` are existing precedents | ✅ | `BaseSkillPart.cs`, `AcrobaticsSkill.cs`, `AcrobaticsDodgePower.cs` |
| Qud's per-weapon-class skills (Cudgel_Bludgeon, Axe_Cleave, LongBladesSwipe, ShortBlades_Jab) live in `qud-decompiled-project/XRL.World.Parts.Skill/` | ✅ | `find /Users/steven/qud-decompiled-project/XRL.World.Parts.Skill -name "Cudgel*.cs"` |
| Player blueprint has `SkillsPart` (fix landed `8cddf54`) | ✅ | `Objects.json:238` |

**No false premises.** `BleedingEffect(int saveTarget=15, string damageDice="1d2")` ctor — verified for `LongBlades_Lacerate`. `StunnedEffect(int duration=2)` ctor — verified for `Cudgel_Bludgeon`. `Zone` adjacent-entity lookup — to verify when implementing `Axe_Cleave` (smallest-risk 🟡; tackled in WS.3 not WS.2).

## Per-weapon-class skill matrix (faithful Qud port; CoO mechanics)

The Qud source informed the SEMANTICS; CoO implementations are
fresh code using existing CoO infrastructure (StunnedEffect /
BleedingEffect / Damage.HasAttribute / SkillsPart). Skill names use
common gaming terminology (Bludgeon, Cleave, Lacerate, Jab) that
appears across many roguelikes/RPGs.

| Class | Tree-root (1 SP) | Power (1 SP) | Trigger | Effect |
|---|---|---|---|---|
| **Cudgel** | `CudgelSkill` (flavor) | `Cudgel_Bludgeon` | hit + `damage.HasAttribute("Cudgel")` | 35% chance → `StunnedEffect(3)` on defender |
| **Axe** | `AxeSkill` (flavor) | `Axe_Cleave` | hit + `damage.HasAttribute("Axe")` | 30% chance → half-damage to one adjacent enemy of defender |
| **LongBlades** | `LongBladesSkill` (flavor) | `LongBlades_Lacerate` | hit + `damage.HasAttribute("LongBlades")` | 35% chance → `BleedingEffect(saveTarget=15, "1d3")` on defender (additive over OnHitClassEffects' 25% Cutting→Bleed) |
| **ShortBlades** | `ShortBladesSkill` (flavor) | `ShortBlades_Jab` | hit + `damage.HasAttribute("Piercing")` | +1 damage on hit (always; passive flat boost) |

**Why "ShortBlades" tree on Piercing trigger:** Qud calls the
short-blade tree "ShortBlades"; CoO weapons today carry the
"Piercing" damage class but no "ShortBlades" sub-class attribute.
Rather than back-fill the Piercing weapons' Attributes string
(blueprint risk), the skill triggers on `Piercing` directly. Tree
name preserves Qud terminology; trigger uses CoO's existing tag.

**Stacking:** Multi-class weapons (e.g. Mace = "Bludgeoning Cudgel")
fire BOTH the OnHitClassEffects Bludgeoning→Stun roll (15%, 2T) AND
the Cudgel_Bludgeon roll (35%, 3T) on the same hit. Per
`StunnedEffect.OnStack` (extends duration), this stacks correctly.

## Sub-milestones (smallest-blast-radius first)

| # | Title | Purpose | New surface |
|---|---|---|---|
| WS.0 | This plan | Plan to disk | `Docs/WEAPON-SKILLS.md` |
| WS.1 | `OnHitSkillEffects` scaffold | Pure-empty static class + 1-line CombatSystem hook + 1 no-op test (RED→GREEN). Risk: hook position. | `OnHitSkillEffects.cs`, +1 line `CombatSystem.cs`, +1 test |
| WS.2 | Cudgel tree + Bludgeon | JSON content + 2 C# stubs + Bludgeon branch in OnHitSkillEffects + 3 tests (apply, not-owned counter-check, no-actualDamage no-op) | `Cudgel.json`, `CudgelSkill.cs`, `Cudgel_Bludgeon.cs`, +OnHitSkillEffects branch, +3 tests |
| WS.3 | Axe tree + Cleave | Same shape. Cleave needs adjacent-enemy lookup helper — verify `Zone` API in pre-impl read of WS.3 | `Axe.json`, `AxeSkill.cs`, `Axe_Cleave.cs`, +OnHitSkillEffects branch, +3 tests |
| WS.4 | LongBlades tree + Lacerate | Same shape | `LongBlades.json`, `LongBladesSkill.cs`, `LongBlades_Lacerate.cs`, +OnHitSkillEffects branch, +3 tests |
| WS.5 | ShortBlades tree + Jab | Same shape, but Jab is a flat damage boost rather than a status — passes via different code path; verify | `ShortBlades.json`, `ShortBladesSkill.cs`, `ShortBlades_Jab.cs`, +OnHitSkillEffects branch, +3 tests |
| WS.6 | Showcase + cold-eye + roadmap | Update `SkillTreeShowcase` so popup shows all 5 trees; cold-eye delegation; CONTENT-ROADMAP update | Mod `SkillTreeShowcase.cs`; mod `CONTENT-ROADMAP.md`; doc-implementation log in this file |

**~12 new tests added (3 per power × 4 powers).** Counter-check
discipline (CLAUDE.md §3.4): every "skill applies effect" test pairs
with "actor without skill in identical setup → effect NOT applied."

## Per-skill C# class shape

```csharp
// Tree-root (flavor; no behavior).
public class CudgelSkill : BaseSkillPart
{
    public override string ClassName => nameof(CudgelSkill);
}

// Power: identity-only stub. The actual on-hit logic lives in
// OnHitSkillEffects.Apply, which checks SkillsPart.HasSkill().
// This mirrors the existing AcrobaticsDodgePower pattern, except
// where Dodge stat-shifts on add, on-hit powers don't do anything
// at AddSkill time — they fire when combat resolves.
public class Cudgel_Bludgeon : BaseSkillPart
{
    public override string ClassName => nameof(Cudgel_Bludgeon);
}
```

## OnHitSkillEffects shape

```csharp
public static class OnHitSkillEffects
{
    public const int CUDGEL_BLUDGEON_CHANCE_PERCENT = 35;
    public const int CUDGEL_BLUDGEON_DURATION = 3;

    public const int AXE_CLEAVE_CHANCE_PERCENT = 30;

    public const int LONGBLADES_LACERATE_CHANCE_PERCENT = 35;
    public const int LONGBLADES_LACERATE_SAVE_TARGET = 15;
    public const string LONGBLADES_LACERATE_DAMAGE_DICE = "1d3";

    public const int SHORTBLADES_JAB_DAMAGE_BONUS = 1;

    public static void Apply(Damage damage, int actualDamage,
        Entity defender, Entity attacker, Zone zone, Random rng)
    {
        if (damage == null || defender == null || attacker == null || rng == null) return;
        if (actualDamage <= 0) return;

        var skills = attacker.GetPart<SkillsPart>();
        if (skills == null) return;

        if (skills.HasSkill(nameof(Cudgel_Bludgeon))
            && damage.HasAttribute("Cudgel"))
        {
            TryCudgelBludgeon(defender, attacker, zone, rng);
        }
        if (skills.HasSkill(nameof(Axe_Cleave))
            && damage.HasAttribute("Axe"))
        {
            TryAxeCleave(damage, actualDamage, defender, attacker, zone, rng);
        }
        // … LongBlades_Lacerate, ShortBlades_Jab
    }
}
```

## Hook into CombatSystem

```csharp
if (hpAfter > 0)
{
    OnHitClassEffects.Apply(damage, actualDamage, defender, attacker, zone, rng);
    OnHitSkillEffects.Apply(damage, actualDamage, defender, attacker, zone, rng);   // ← WS.1
    if (hitPart != null)
        CheckCombatDismemberment(defender, defenderBody, hitPart, actualDamage, zone, rng);
}
```

One line. Same scope as OnHitClassEffects, fired immediately after.

## Pre-flagged self-review findings

- **🟡 ShortBlades_Jab is a flat damage boost not a status apply** —
  applies BEFORE `actualDamage` is computed, so the OnHitSkillEffects
  signature (which gets `actualDamage` post-resistance) doesn't fit
  it cleanly. WS.5 will either move Jab to a different hook (pre-
  damage modifier) or reframe it (e.g. on Piercing hit, +1 future
  attack to-hit for 1 turn via a buff effect). Decide in WS.5
  pre-impl.
- **🟡 Axe_Cleave adjacent-enemy lookup** — needs `Zone.GetEntitiesAt(x,y)`
  or equivalent. Read Zone API in WS.3 pre-impl.
- **🔵 No tree-root skills with mechanics** — all 4 tree-roots are
  flavor-only. Could shift +1 to-hit or +1 PenBonus per class. v1
  prefers minimal scope; can layer in a v2 expansion.
- **🔵 RNG sourcing** — `OnHitSkillEffects.Apply` should use the
  same `rng` parameter as `OnHitClassEffects` for test determinism.
  Already in plan.
- **⚪ Powers with no `Requires` field on the parent tree** — per
  user's "no requirement other than 1 SP cost" instruction, even
  the parent-tree requirement is dropped. `Cudgel_Bludgeon` has
  `Requires = ""`, so it's buyable without `CudgelSkill` first.
  Slight semantic oddity ("you bought Bludgeon without knowing
  Cudgel weapons") but matches the user's spec. Documented here.
- **🧪 Multi-class weapon stacking unverified** — claim: "Mace
  fires both Bludgeoning class hook AND Cudgel_Bludgeon skill hook."
  Will add a stacking test in WS.2 to lock this in.

## Verification (post-implementation)

```
run_tests EditMode group_names=[
  "OnHitSkillEffectsTests",
  "OnHitClassEffectsTests",
  "SkillsPartTests", "BuySkillActionTests",
  "SkillsScreenStateBuilderTests", "SkillsScreenLiveBlueprintTests",
  "ScenarioCustomSmokeTests"
]
```

Expected: 100+/100+ GREEN.

**Live verify:** open `SkillTreeShowcase`, press X, confirm 5 trees
appear (Acrobatics + Cudgel + Axe + LongBlades + ShortBlades) with
all 9 skills (1 tree-root + 1 power per tree, plus Acrobatics's
Dodge). Buy Cudgel_Bludgeon for 1 SP, equip a Mace, swing at a
Snapjaw, watch the message log for stun applications.

## What gets observable to the player after this ship

| Today | After WS |
|---|---|
| 1 tree (Acrobatics), 1 power (Dodge — DV passive). Per-weapon damage classes apply OnHitClassEffects (Stun/Bleed/Confuse). | + 4 trees (Cudgel/Axe/LongBlades/ShortBlades). + 4 weapon-specific powers boosting your weapon's existing class hook. Each new skill is 1 SP — buyable from level 1 onward. |
| Bludgeoning weapons stun at 15%, weapons feel mostly interchangeable beyond raw damage. | Cudgel hits stun MORE (35% on Cudgel-attribute hits vs. 15% on plain Bludgeoning). Axes cleave (hit adjacent enemies). LongBlades bleed harder. Daggers pierce armor. **Weapon class becomes a meaningful build choice.** |
