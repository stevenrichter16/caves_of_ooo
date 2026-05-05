# Weapon-Skills Parity (WSP) — Qud functional mirror

Continuation of `Docs/WEAPON-SKILLS.md`. The first ship (WS.1-6b)
gave each weapon class a tree-root and one foundational on-hit
power, all 1 SP, no requirements. This ship makes the trees
observably more **Qud-faithful** by:

1. Wiring **per-tree crit-specific behaviors** on the tree-roots
   themselves — so owning a tree-root for 1 SP now does something
   observable in combat, not just unlocks tree identity.
2. Re-tuning the existing `Cudgel_Bludgeon` chance + duration to
   match Qud's actual values verbatim (50% / 1-4T random).
3. Adding **`ShortBlades_Bloodletter`** as a second ShortBlades
   power — frequent light-Bleed proc on every Piercing hit.

Active abilities, equipment-shatter mechanics, off-hand attack
mechanics, stance toggles, dismemberment auto-triggers, and the
Expertise +to-hit passives are **deferred** — each requires
substantial new infra (ActivatedAbilities integration, a Broken
effect, a to-hit modifier event hook, dual-wielding, etc.) and
their own follow-on milestone. Honest scope wins over half-shipping.

## Pre-impl verification sweep

| Premise | Status | Source |
|---|---|---|
| `damage.HasAttribute("Critical")` is the canonical CoO crit-marker, set in `CombatSystem.cs:233-234` when a natural-20 lands | ✅ | `CombatSystem.cs` (verified earlier this session) |
| `OnHitSkillEffects.Apply()` fires post-`ApplyDamage` inside `if (hpAfter > 0)`; full damage object + actualDamage are in scope | ✅ | `CombatSystem.cs:307` |
| `StunnedEffect(int duration)` ctor exists; `OnStack` sums durations | ✅ | `StunnedEffect.cs:12, 35-43` |
| `BleedingEffect(int saveTarget=15, string damageDice="1d2", System.Random rng=null)` ctor exists | ✅ | `BleedingEffect.cs:17` |
| `TryAxeCleave` already targets adjacent Creature with the chance gate; can be re-used by passing chance=100 for crit-force-cleave | ✅ | `OnHitSkillEffects.cs:179-216` |
| Existing tree-root C# classes (`CudgelSkill` / `AxeSkill` / `LongBladesSkill` / `ShortBladesSkill`) are identity-only stubs — adding crit-hook branches in `OnHitSkillEffects.Apply` (gated on `HasSkill(nameof(CudgelSkill))` etc.) doesn't require touching the stubs | ✅ | `CudgelSkill.cs`, `AxeSkill.cs`, `LongBladesSkill.cs`, `ShortBladesSkill.cs` |

**No false premises.** Single design-call: LongBlades's Qud crit
mechanic (+2 penetration rolls — extra damage dice rolled at hit
time) doesn't fit CoO's post-damage hook signature. Reframed as
**force-Bleed on crit with stronger dice** (`1d4` vs Lacerate's
`1d3` vs class hook's `1d2`) — same shape as ShortBlades's port,
parallel to Axe's force-cleave pattern. Documented inline.

## Tier classification

### Tier 1 — ship in this milestone (faithful, post-damage hook fits)

| Skill | Qud trigger | Qud effect | CoO port |
|---|---|---|---|
| `CudgelSkill` (tree-root) | crit with Cudgel weapon | Apply Dazed 1-4T | Apply `StunnedEffect` for `rng.Next(1,5)` turns (no Dazed in CoO) |
| `AxeSkill` (tree-root) | crit with Axe weapon | Force `Axe_Cleave.PerformCleave` at 100% | Reuse `TryAxeCleave` body, but skip the chance roll |
| `LongBladesSkill` (tree-root) | crit with LongBlades weapon | +2 penetration rolls (extra dice at hit time) | Apply `BleedingEffect("1d4", saveTarget=15)` — reframed since CoO has no penetration multi-roll system |
| `ShortBladesSkill` (tree-root) | crit with ShortBlades weapon | Apply Bleeding 1d2-1, save=20+Agility, no-stack | Apply `BleedingEffect("1d2", saveTarget=15)` — simplified save target |
| `Cudgel_Bludgeon` (existing) | every Cudgel hit | 50% Dazed 1-4T (random) | Re-tune existing branch: 50% (was 35%) + random 1-4T (was fixed 3T) |
| `ShortBlades_Bloodletter` (NEW power, 1 SP no-reqs) | every Piercing hit | 75% Bleed 1d2-1 if defender's Bleeding-count < 1+Attacker.AgilityMod | Simplified: 50% chance per Piercing hit to apply `BleedingEffect("1d2")` |

### Tier 2 — deferred (need new combat hooks or substrate)

| Skill | Why deferred |
|---|---|
| `Cudgel_Expertise` / `Axe_Expertise` / `ShortBlades_Expertise` | Need a `GetToHitModifierEvent`-style hook in CoO's hit-roll path. Combat's hit-roll currently doesn't expose a per-actor modifier event. Substrate-level addition. |
| `Cudgel_Hammer` (2% break random equipment) | Need a `BrokenEffect` + the `Body.GetEquippedParts` enumeration path. |
| `Cudgel_ShatteringBlows` (10% ShatterArmor) | Need a `ShatterArmorEffect` that decrements `ArmorPart.AV` for a duration. |
| `Cudgel_Backswing` (25% on miss → re-attack) | Need an `AttackerMeleeMiss` event hook. CoO's combat doesn't currently emit this. |
| `Axe_Decapitate` / `Axe_Dismember` (auto on penetration) | CoO's dismemberment is HP-threshold-driven, not penetration-count-driven. Different math. |
| `ShortBlades_Hobble` (slow on hit) | Need a `HobbledEffect` (move-speed reduction). |
| `ShortBlades_Rejoinder` (counter-attack on dodged) | Need a `DefenderAfterAttackMissed` event hook (i.e. "I dodged, free riposte"). |

### Tier 3 — out of scope (need feature systems CoO doesn't have)

| Skill | Blocker |
|---|---|
| All active abilities (`Cudgel_Conk`, `Cudgel_Slam`, `Axe_Berserk`, `LongBladesSwipe`, etc.) | Need `ActivatedAbility` integration with `SkillsPart`. Substantial UI + input + cooldown system. |
| `LongBlades` 3-stance system (Aggressive/Defensive/Duelist) | Need stance-toggle infra + AV/DV per-stance multipliers. |
| `LongBladesProficiency` | Qud's wielding-feat-tree mechanism — unrelated subsystem. |
| All dual-wielding-specific skills (off-hand to-hit, off-hand cooldown halving) | CoO has no dual-wielding system. |

## Sub-milestones (smallest blast radius first)

| # | Title | Surface | Tests |
|---|---|---|---|
| WSP.0 | Plan to disk (this commit) | `Docs/WEAPON-SKILLS-PARITY.md` | n/a |
| WSP.1 | 4 tree-root crit behaviors (Cudgel/Axe/LongBlades/ShortBlades) | `OnHitSkillEffects.cs` (+4 if-blocks + 2 helper methods) | +6 (positive + counter-checks per tree) |
| WSP.2 | `Cudgel_Bludgeon` re-tune to Qud values (50%/1-4T) | `OnHitSkillEffects.cs` (constants + helper) | +1 (pin new chance/duration values) |
| WSP.3 | `ShortBlades_Bloodletter` new power | `Bloodletter.json` content + `ShortBlades_Bloodletter.cs` stub + branch + tests | +3 (positive + 2 counter-checks) |
| WSP.4 | Showcase update + cold-eye + roadmap close | `SkillTreeShowcase.cs` + `WEAPON-SKILLS-PARITY.md` impl log | n/a |

~10 new tests. ~4 commits.

## Implementation hooks

**WSP.1** — each tree-root branch gates on `damage.HasAttribute("Critical")` + `damage.HasAttribute(matchingClass)` + `skills.HasSkill(nameof(MatchingTreeRootSkill))`. The existing 4 power-branches stay untouched; the 4 new branches fire ON TOP, so a Mace crit owned by a Cudgel-trained character with `Cudgel_Bludgeon` runs THREE rolls: 15% class-hook Stun + 35% Cudgel_Bludgeon Stun + 100% CudgelSkill crit Stun (1-4T). Stunned's OnStack sums durations — a single crit can compound to 6+T of stun.

**WSP.2** — flip two constants:
```csharp
// was
public const int CUDGEL_BLUDGEON_CHANCE_PERCENT = 35;
public const int CUDGEL_BLUDGEON_DURATION = 3;

// after
public const int CUDGEL_BLUDGEON_CHANCE_PERCENT = 50;
public const int CUDGEL_BLUDGEON_DURATION_MIN = 1;
public const int CUDGEL_BLUDGEON_DURATION_MAX = 4;  // exclusive
// helper rolls rng.Next(MIN, MAX+1) for the duration
```

**WSP.3** — new power follows the established pattern:
- `Resources/Content/Data/Skills/ShortBlades.json` gets a second power entry alongside `Jab`
- `Assets/Scripts/Gameplay/Skills/ShortBlades_Bloodletter.cs` (identity stub)
- `OnHitSkillEffects.Apply` gets a 5th if-block + helper

## Pre-flagged self-review findings

- **🟡 LongBlades crit mechanic divergence** — Qud uses +2 penetration
  rolls (extra dice at hit time, before damage compute). CoO ports
  this as force-Bleed with `1d4` dice on crit. The flavor (cuts deep
  + bleeds long) is preserved; the math diverges. Documented in the
  power's docstring + this plan.
- **🔵 Triple-stack on Mace crit** — owning Cudgel + Cudgel_Bludgeon
  with a Mace-on-crit can produce 3 Stunned rolls + the universal
  Bludgeoning class-hook 15% Stun = 4 rolls. Stunned's `OnStack`
  sums durations, so worst-case Stun can reach ~10T from a single
  crit. Per-roll independent so 4 hits stacking onto each other is
  possible but rare. If gameplay testing shows this is broken,
  introduce a per-skill deduplication layer; for v1, ship the
  faithful port and observe.
- **🧪 Coverage gap for Tier 2** — these skills (Expertise / Hammer /
  ShatteringBlows / Backswing / Hobble / Rejoinder) are documented as
  deferred; they'll need their own ship with their own infra.
  Tracked here, not in code.
- **⚪ ShortBlades_Bloodletter Agility-stacking-cap dropped** — Qud's
  version caps active Bleeding count by `1 + Attacker.StatMod("Agility")`.
  CoO drop the cap for v1 (Bleeding's own OnStack semantics determine
  stacking). Re-introduce if testing shows runaway bleed-stacking.

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

Expected: 110+/110+ GREEN.

**Live verify:** open `SkillTreeShowcase`, equip a Mace, swing at
the same target until a natural-20 lands, watch the message log:
should see Stunned applied repeatedly with stacking duration.
Switch to Battleaxe, find a target with an adjacent enemy, hit
until crit — both targets take damage. Switch to LongSword + hit
until crit — defender takes Bleeding 1d4 not 1d2. Etc.

## What gets observable to the player after this ship

| Today (post-WS.1-6) | After WSP.1-3 |
|---|---|
| Tree-root skills (CudgelSkill / AxeSkill / LongBladesSkill / ShortBladesSkill) bought for 1 SP each → no observable effect; only powers are observable. | Each tree-root grants a crit-only bonus tied to the matching weapon class. Buying CudgelSkill alone is now mechanically meaningful — every natural-20 with a mace stuns. |
| Cudgel_Bludgeon: 35% chance → Stunned 3T (CoO-original tuning). | Cudgel_Bludgeon: 50% / 1-4T random duration (verbatim Qud values). |
| ShortBlades has 1 power (Jab → 30% Confused 3T). | ShortBlades has 2 powers — Jab + Bloodletter (50% Bleed every Piercing hit). Two-skill specialization. |
