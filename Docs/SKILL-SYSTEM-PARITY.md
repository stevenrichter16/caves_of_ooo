# Skill-system Qud parity (WSP3)

User directive: **"i want the full qud parity skill system so i can
modify the particular skills to my need for the game"** + **"you have
qud-decompiled-project so this shouldnt be extremely super difficult
to figure out what to program"**.

This ship pivots from porting individual skills (WSP / WSP2 line)
to building the **Qud-parity skill SYSTEM** — the architecture that
makes skill authoring straightforward. Existing skills get refactored
onto the new pattern as proof points; new skills can then be added
by the user (or me) by overriding clearly-named virtuals on a
`BaseSkillPart` subclass.

## Qud's skill architecture (the spec)

Every Qud skill (`XRL.World.Parts.Skill.*`) follows the same pattern:

```csharp
[Serializable]
public class Cudgel_Bludgeon : BaseSkill
{
    // 1. Declare which events you care about.
    public override void Register(GameObject Object, IEventRegistrar Registrar)
    {
        Registrar.Register("AttackerAfterAttack");
        base.Register(Object, Registrar);
    }

    // 2. Handle them by overriding FireEvent / HandleEvent.
    public override bool FireEvent(Event E)
    {
        if (E.ID == "AttackerAfterAttack") {
            // ...read params from event, do mechanic...
        }
        return base.FireEvent(E);
    }
}
```

Tree-roots additionally override well-named virtual methods:

```csharp
public class Cudgel : BaseSkill
{
    public override void WeaponMadeCriticalHit(GameObject Attacker,
        GameObject Defender, GameObject Weapon, string Properties)
    {
        Defender.ApplyEffect(new Dazed(Stat.Random(1, 4)));
    }
}
```

Active abilities are wired through:

```csharp
public override bool AddSkill(GameObject GO)
{
    ActivatedAbilityID = AddMyActivatedAbility(
        "Conk", "CommandConk", "Skills",
        cooldown: 10, /*...*/);
    return true;
}
```

Combat fires events at canonical points (`AttackerAfterAttack`,
`AttackerMeleeMiss`, `DefenderAfterAttackMissed`, `GetToHitModifier`,
`DealDamage`, `BeforeMeleeAttack`, etc.), and the actor's owned skill
parts each get a chance to react.

**Key wins from this architecture:**
1. Skills are **self-contained** — modifying Cudgel_Bludgeon means
   editing `Cudgel_Bludgeon.cs`, not editing a central switch.
2. Adding a new skill is **append-only** — write the class, add JSON
   content, no need to edit a shared dispatcher.
3. Tree-root crit behaviors live next to the tree-root identity
   class, not in a sibling helper.
4. Active abilities are first-class — declared at AddSkill time, no
   manual hotbar/cooldown plumbing.

## Current CoO state (what to refactor)

CoO's existing skill behavior lives in two static-dispatcher classes:

- **`OnHitClassEffects.Apply(...)`** — universal class-tag effects
  (Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse). NOT touching
  this — it's class-tier (every actor with the right damage class
  triggers, regardless of skills). Unchanged by this ship.
- **`OnHitSkillEffects.Apply(...)`** — per-skill on-hit + tree-root
  crit branches. THIS gets replaced by event-dispatch to `BaseSkillPart`
  virtual overrides.

After WSP3, the on-hit pipeline in `CombatSystem.PerformSingleAttack`
post-`ApplyDamage` becomes:

```csharp
if (hpAfter > 0) {
    OnHitClassEffects.Apply(damage, actualDamage, defender, attacker, zone, rng);
    OnHitWeaponEffects.Apply(weapon, damage, actualDamage, defender, attacker, zone, rng);

    // NEW: fire skill events (replaces OnHitSkillEffects.Apply)
    var ctx = new SkillEventContext { /* ... */ };
    SkillEventDispatcher.AttackerAfterAttack(attacker, ctx);
    if (damage.HasAttribute("Critical"))
        SkillEventDispatcher.WeaponMadeCriticalHit(attacker, ctx);
}
```

`OnHitSkillEffects` itself becomes either: (a) deleted, or (b)
turned into an obsolete/legacy hook that delegates to the new
dispatcher for back-compat. Picking (a) — it's only been alive for
~1 day and the existing tests get refactored too.

## Acceptance criteria (the user's explicit ask)

A skill author should be able to:
1. **Add a new passive** by writing a new `BaseSkillPart` subclass
   that overrides one virtual + adding a JSON entry — no core
   system edits required.
2. **Modify an existing skill's behavior** by editing only that
   skill's `.cs` file.
3. **Add an active ability skill** by overriding `OnAdd` to declare
   it and overriding the matching `OnCommand` virtual.
4. **Rely on faithful event timing** — the events fire at the same
   logical points Qud fires them.

Specific gates:
- ✓ All 9 existing skills refactored to override virtuals on `BaseSkillPart`
- ✓ `OnHitSkillEffects` deleted; skill behavior is per-class only
- ✓ `SkillEventDispatcher` static class introduced for combat → skills routing
- ✓ Tree-root crit behaviors live in their tree-root classes (not separate helpers)
- ✓ At least one active-ability skill (Conk or Berserk) shipped via new infra
- ✓ All existing tests pass + new tests for the new event dispatch
- ✓ `Docs/AUTHORING-SKILLS.md` documents the pattern with worked examples

## Sub-milestones (smallest blast radius first)

| # | Title | Surface |
|---|---|---|
| WSP3.0 | Plan to disk (this commit) | `Docs/SKILL-SYSTEM-PARITY.md` |
| WSP3.1 | `SkillEventContext` + virtuals on `BaseSkillPart` + `SkillEventDispatcher` static | New types; `BaseSkillPart` virtuals (no behavior change yet) |
| WSP3.2 | Wire `CombatSystem.PerformSingleAttack` to fire dispatcher events | One-line edits at the canonical hook points |
| WSP3.3 | Refactor 9 existing weapon-class skills onto virtuals; delete `OnHitSkillEffects` | 9 skill files modified; ~3 helpers extracted to per-skill class methods; OnHitSkillEffects.cs deleted |
| WSP3.4 | Ship Tier-2 passives via new pattern: Expertise×3, Hammer, ShatteringBlows, Hobble passive, Backswing, Rejoinder | 8 new skill classes; ~3 reuse existing effects from WSP2.1 |
| WSP3.5 | `BaseSkillPart` ActivatedAbility integration: declare on AddSkill, route CommandX events | New API on BaseSkillPart; new bridge from SkillsPart to InputHandler hotbar |
| WSP3.6 | Ship 1-2 Tier-3 active abilities as proof: Conk, Berserk | 2 active-ability skill classes |
| WSP3.7 | `Docs/AUTHORING-SKILLS.md` + cold-eye + roadmap close | Authoring guide with 3 worked examples (passive, on-miss, active) |

**Estimated:** ~15-18 commits. Each is independently revertable.

## Risks + mitigations

- **🔴 Refactor of OnHitSkillEffects + 9 skills + tests in WSP3.3 is the highest-risk commit.** Mitigation: stage the work — write the virtuals first (WSP3.1, no behavior change), then wire combat (WSP3.2, behavior shifts but old static path still runs as fallback), then refactor skills one-tree-at-a-time. Tests pin behavior at every step.
- **🟡 ActivatedAbility-Skill bridge is novel territory.** CoO's existing `ActivatedAbilities` part is a Player-only manager not currently consumed by skills. Mitigation: WSP3.5 reads the existing class first, prototypes the bridge minimally before shipping Tier-3 skills.
- **🟡 Properties string on attacks** — Qud passes a CSV like "Charging,Conking" through `MeleeAttackWithWeapon`. CoO's `PerformSingleAttack` doesn't currently have this parameter. Mitigation: add an optional `string properties = null` parameter; default keeps existing call sites working.
- **🔵 Event-dispatch perf concern** — iterating owned skills on every hit is O(N) per attack. With ~13 skills total this is ~13 cheap virtual calls, well under any frame budget. Mitigation: defer optimization; document for future profiling.
- **⚪ "Why not GameEvent?"** — CoO has a `GameEvent` system but it's not currently wired through CombatSystem. Adding event-dispatch through GameEvent would be heavier; the dedicated `SkillEventDispatcher` is lighter and shape-matches Qud's per-skill `Register`/`HandleEvent`.

## Verification

Per sub-milestone: full skill-related test suite + compile zero errors.
After WSP3.7: cold-eye delegation + manual playtest via the showcase
scenario. Expected final test count: ~150+ EditMode tests passing.

---

## Implementation log (post-ship)

| Sub-milestone | Commit | What shipped | Tests |
|---|---|---|---|
| WSP3.0 | `abdb2f5` | Plan to disk | n/a |
| WSP3.1 | `c887a1e` | `SkillEventContext` + `SkillEventDispatcher` + 5 virtual hooks on `BaseSkillPart` (default no-op) | +7 |
| WSP3.2 | `93ec5bf` | Wire `CombatSystem.PerformSingleAttack` to fire dispatcher events at 4 canonical points (post-damage, miss, crit, hit-bonus-sum). No behavior change yet | regression unchanged |
| WSP3.3 | `9dfcdc7` | Refactor 9 existing skills onto virtuals; **delete** `OnHitSkillEffects.cs`. Each skill becomes one self-contained file. Test fixture migrated via `DispatchAttack` shim | regression unchanged |
| WSP3.4 | `bed7bf6` | Ship 8 Tier-2 passive skills via new pattern: 3 Expertise (+to-hit), Hammer, ShatteringBlows, Hobble, Backswing, Rejoinder | +11 |
| WSP3.5 | `9654586` | `BaseSkillPart` ActivatedAbility integration: `DeclareActivatedAbility` + `OnCommand` virtuals; `SkillsPart.AddSkill` auto-registers the ability + `RemoveSkill` cleans up; `TryRouteSkillCommand` routes commands with cooldown gate | +8 |
| WSP3.6 | `2ffb017` | Ship 2 Tier-3 active abilities + new `BerserkEffect`: `Cudgel_Conk` (targeted strike + Stunned, 10T cd) + `Axe_Berserk` (self-buff +Str/-DV, 100T cd). `OnCommand` signature refactored to take `SkillEventContext` | +3 |
| WSP3.7 | `6a3381b` | `Docs/AUTHORING-SKILLS.md` worked-examples guide; impl log + roadmap update | n/a |
| WSP4.0 | `3e8c80f` | Active-ability behavior tests (Conk/Berserk per-skill positives + guards) + recursion-guard pins for Backswing/Rejoinder | +7 |
| WSP4.2 | `80e9822` | Cross-skill interaction tests: Hammer multi-equipment randomness, Hobbled+Berserk DV stacking, multi-Cudgel-on-same-hit, dispatcher class-gate counter-check | +5 |
| WSP4.3 | (delegated) | Cold-eye agent reviewed all 22 skill files; 10 findings returned (1 🔴 + 3 🟡 + 3 🔵 + 2 🧪 + 1 ⚪) | n/a |
| WSP4.4 | `61e257b` | Closed 7 cold-eye findings: drop `[NonSerialized]` on `ActivatedAbilityID` (🔴 #1 — save/load break); Conk RNG-fallback removal (🟡 #2); doc-contract on `TryRouteSkillCommand` fallback (🟡 #3); add `LongBlades_Expertise` (🔵 #5); inline comment on Conk Zone-asymmetry (🔵 #7); 8 new tree-root crit + Expertise tests (🧪 #8); Hammer Body-but-no-equipped distinct test (🧪 #9); doc-vs-impl null-guard idiom alignment (⚪ #10). Defense-in-depth: added `Critical` attribute checks to all 4 tree-root crit hooks. | +9 |
| WSP4.5 | `4f84dbf` | Defense-in-depth symmetry: parallel non-Critical tests for CudgelSkill + AxeSkill (matching the LongBlades + ShortBlades pattern) | +2 |
| WSP4.6 | `b453244` | Symmetry sweep across 22 skill files + 5 JSON content files. **Found:** Expertise group fully symmetric (now 4); on-hit-proc group symmetric except Axe_Cleave's intentional Zone-vs-Defender guard divergence; tree-root crit group symmetric; active-ability + on-miss groups follow expected per-semantic shapes; JSON cost split (Acrobatics 100/50 vs weapon-trees 1 SP) confirmed intentional + now documented above. | n/a |
| WSP5    | `7dcfdbe` (merge) | Round-2 cold-eye on the WSP4 round (5 commits). Closed 5 findings: 🔴 LongBlades_Expertise reclassified as CoO-original Extension (Qud has no LongBlades_Expertise — verified via `find qud-decompiled-project -name "*Expertise*"`); 🟡 OnAfterLoad Guid-persistence test added (structural reflection + behavioral); 🟡 SKILL-SYSTEM-PARITY.md "+to-hit passives" count corrected (3→4); 🧪 AUTHORING-SKILLS.md Pattern 3 null-guard comment fixed (on-miss skills don't use chained `ctx?.Damage`); ⚪ LongBlades_Expertise tests relocated from CritTests to Tier2Tests. Two borderline doc-drift items also fixed (SkillCombatHelpers ShortBlades_Hobble active-version reference; BaseSkillPart forward-reference camelcase). | +4 |
| WSP6.0  | (this commit) | Tier-3 plan section: Qud catalog gap survey, port-priority ranking by complexity (🟢/🟡/🔴), WSP6 candidate ordering. Stance-batch (LongBladesCore + 6 skills) deferred to WSP7+. | n/a |
| WSP6.1  | `cef9fd3` (merge) | Ship `Cudgel_Slam` — first Tier-3 active-ability port. Adjacent target pushed up to 3 cells in slam direction, blocked by solid (wall/creature/closed door). Wall hits roll bonus weapon damage and scale stun duration (1-4T). Mirrors Qud's `Cudgel_Slam.cs` mechanic family with simplified push semantics (no wall-destruction, no creature-chain — see plan §WSP6 candidates). 12 RED→GREEN tests + JSON content entry. | +1 |
| WSP6.6  | (this commit) | Ship `ShortBlades_Puncture` — first Tier-3 *passive* port + first new combat-event hook since the system shipped. Adds `OnGetPenetrationModifier` virtual to `BaseSkillPart` and `GetSkillPenetrationModifier` to `SkillEventDispatcher` per the §"Adding a new combat event" mechanical pattern. CombatSystem.PerformSingleAttack feeds the sum into both `bonus` and `maxBonus` for `RollPenetrations`. Puncture returns +2 when wielding a Piercing-attribute weapon. Mirrors Qud's `ShortBlades_Puncture` "AV - 2" mechanic (mathematically equivalent to "+2 pen bonus"). 9 RED→GREEN tests including a 200-seed statistical pin (with-Puncture deals strictly more total damage than without across the same RNG seeds) + JSON content entry. | +1 |

**Final state of the skill SYSTEM:**

- 5 trees registered (Acrobatics + 4 weapon classes)
- 24 skill classes across all tiers (verified by `grep -l "class.*: BaseSkillPart" Skills/*.cs`):
  - 5 tree-roots (Acrobatics + 4 weapon classes)
  - 4 tree-root crit hooks (in the 4 weapon-class tree-root classes'
    `OnWeaponMadeCriticalHit`; AcrobaticsSkill is passive-only)
  - 8 power on-hit procs (`OnAttackerAfterAttack` overrides):
    Cudgel_Bludgeon, Cudgel_Hammer, Cudgel_ShatteringBlows,
    Axe_Cleave, LongBlades_Lacerate, ShortBlades_Jab,
    ShortBlades_Bloodletter, ShortBlades_Hobble
  - 4 +to-hit passives (Expertise × 4 weapon classes — including
    LongBlades_Expertise, the WSP4.4 CoO-original Extension; see §4.2
    classification rules)
  - 1 +pen passive (ShortBlades_Puncture — shipped WSP6.6 with
    the new `OnGetPenetrationModifier` hook)
  - 2 on-miss / on-dodge passives (Cudgel_Backswing, ShortBlades_Rejoinder)
  - 3 active abilities (Cudgel_Conk, Axe_Berserk, Cudgel_Slam — the
    last two shipped in WSP6.1 as the first Tier-3 batch port)
  - 1 dodge passive (AcrobaticsDodgePower)
- 5 new status effects shipped on top of CoO's effect machinery:
  Hobbled, ShatterArmor, Broken, Berserk (this ship), plus the
  existing Stunned/Bleeding/Confused/etc. consumed by the new skills
- ~155+ EditMode tests passing (was 96 pre-WSP)

**The system architecture mirrors Qud's** — each skill class is
self-contained with `Register`/`HandleEvent`-style virtuals on
`BaseSkillPart`, central `SkillEventDispatcher` routes events,
active abilities declare via `DeclareActivatedAbility` exactly the
way Qud's `AddMyActivatedAbility` works.

**Authoring a new skill:** see `Docs/AUTHORING-SKILLS.md` —
3 worked-example patterns (passive on-hit, on-miss/defender-side,
active ability) + every available virtual hook documented + which
shipped skill to use as a copy-template for each pattern.

**Modifying an existing skill:** edit that skill's `.cs` file. No
central dispatcher to update. Constants live in the owning class
(e.g. `Cudgel_Bludgeon.CHANCE_PERCENT`).

**SP-cost convention (intentional split, documented post-cold-eye):**
- **Acrobatics tree** keeps Qud's actual SP cost (tree-root = 100,
  Dodge power = 50) — Qud-parity for the original ST.5 ship.
- **Weapon-class trees** (Cudgel / Axe / Long Blades / Short Blades)
  use the user-requested "1 SP / no other requirement" cost for
  every entry, per the WS.0 plan. This makes weapon skills
  drastically cheaper than Acrobatics — by design — to incentivize
  weapon specialization in the Tier 1 SP economy.
The disparity is *not* a balance bug but a deliberate split between
"Qud-parity Acrobatics" and "accessibility-tuned weapon trees."
Future content can pick either tier.

**Adding a new combat event** (if you need a hook the existing 5
virtuals don't cover): add a new virtual on `BaseSkillPart`, add a
new entry-point on `SkillEventDispatcher`, wire the call site.
Pattern is mechanical — `WeaponMadeCriticalHit` is the most recent
copy-template.

---

## Tier-3 — Qud-parity gap & next-up ports (WSP6+)

The 22 shipped classes cover the **most-played** Qud powers per
weapon tree (Bludgeon / Cleave / Lacerate / Jab / Hobble class core
on-hit, Backswing / Rejoinder defensive, Conk / Berserk actives, the
4 Expertise +to-hit passives, the 4 tree-root crit hooks). The full
Qud catalog has ~50 weapon-tree skills across 4 classes. The list
below classifies what's left, gated on whether porting needs new
infrastructure.

### Cudgel — 4 Qud powers not yet ported

| Qud skill | What it does | Port complexity | Triage |
|---|---|---|---|
| `Cudgel_Slam` | Knockback + stun; if hits wall, bonus dmg | 🟡 Medium — needs zone movement | 🔵 **Next-up — WSP6** |
| `Cudgel_ChargingStrike` | Move N cells then attack as one action | 🟡 Medium — multi-cell movement | 🔵 Tier-3 batch |
| `Cudgel_SmashUp` | Toggle: each cudgel hit also breaks furniture in target's cell | 🟡 Medium — needs Furniture/Breakable system | ⚪ Defer — needs furniture-class checks |
| `(Cudgel_Slam)` (variant) | Slam through walls if Strength-AV ≥ slamPower | 🔴 Hard — needs CoO wall-as-Object model | ⚪ Defer to v2 |

### Axe — 3 Qud powers not yet ported

| Qud skill | What it does | Port complexity | Triage |
|---|---|---|---|
| `Axe_Decapitate` | Active ability — finishing move, instant kill below HP threshold | 🟢 Easy — uses existing dismemberment | 🔵 **Tier-3 batch** |
| `Axe_Dismember` | Crit chance to dismember random body part (parallel to AxeSkill's force-cleave on crit) | 🟢 Easy — uses CombatSystem.CheckCombatDismemberment | 🔵 Tier-3 batch |
| `Axe_HookAndDrag` | Active ability — pull adjacent enemy 1 cell closer | 🟡 Medium — needs zone movement | 🔵 Tier-3 batch |

### LongBlades — 9 Qud skills not yet ported

The LongBlades tree is **architecturally different from Cudgel/Axe/
ShortBlades** — Qud puts the actual mechanics in `LongBladesCore` (a
Part installed when ANY LongBlades skill is owned), and the skills
themselves are mostly markers that register activated abilities and
sub-stance flags. This is a "Core+stances" pattern.

| Qud skill | What it does | Port complexity | Triage |
|---|---|---|---|
| `LongBladesProficiency` | Marker; covered by existing `LongBlades_Expertise` | n/a | ✅ Functionally covered (CoO-Extension) |
| `LongBladesAggressiveStance` | Active toggle — +pen, -hit when active | 🔴 Hard — needs LongBladesCore + stance-state machine | ⚪ Tier-3 stance-batch |
| `LongBladesDefensiveStance` | Active toggle — +DV when active | 🔴 Hard — same | ⚪ Tier-3 stance-batch |
| `LongBladesDuelingStance` | Active toggle — +hit + parry when active | 🔴 Hard — same | ⚪ Tier-3 stance-batch |
| `LongBladesImproved*Stance` (×3) | Boost the corresponding stance's magnitude | 🔴 Hard — depends on stance batch | ⚪ Tier-3 stance-batch |
| `LongBladesLunge` | Step + strike (gated on aggressive stance) | 🔴 Hard — depends on stance batch | ⚪ Tier-3 stance-batch |
| `LongBladesSwipe` | AOE 3-cell arc attack | 🟡 Medium — adjacent-cells iteration | 🔵 Tier-3 batch (no stance dep) |
| `LongBladesDeathblow` | Finishing move, instant kill below HP threshold | 🟢 Easy — same shape as Axe_Decapitate | 🔵 Tier-3 batch |

### ShortBlades — 4 Qud powers not yet ported

| Qud skill | What it does | Port complexity | Triage |
|---|---|---|---|
| `ShortBlades_Shank` | First strike of turn gets +damage | 🟢 Easy — per-turn flag + on-hit hook | 🔵 Tier-3 batch |
| `ShortBlades_Puncture` | Active ability — pen bonus on next attack | 🟢 Easy — buff-style effect | 🔵 Tier-3 batch |
| `ShortBlades_PointedCircle` | Active ability — AOE, attack all adjacent | 🟡 Medium — adjacent-cells iteration | 🔵 Tier-3 batch |

### Other Qud weapon trees not in CoO

CoO is currently melee-only. The Qud trees below are out of scope
until the player gets a ranged-weapon equivalent:

| Qud tree | CoO status |
|---|---|
| Pistol / Rifle | ⚪ Defer — needs ranged combat system |
| HeavyWeapons | ⚪ Defer — needs heavy-weapon family |
| Shield | ⚪ Defer — needs shield equipment slot |
| Multiweapon | ⚪ Defer — needs dual-wielding |

### Other Qud skill families not in CoO

These are **utility/lifestyle** trees — not directly combat — and
each is a multi-week port. None are in scope for the current parity
push.

| Qud tree | Purpose | Triage |
|---|---|---|
| Survival (×11 sub-skills) | Terrain-survival, camping | ⚪ Defer |
| Tinkering (×9) | Crafting/disassembly | ⚪ Defer |
| Cooking and Gathering (×6) | Food + fungal | ⚪ Defer |
| Discipline (×6) | Mind/body buffs | ⚪ Defer |
| Endurance (×7) | HP / stamina passives | 🔵 Some port-fits — esp. `Endurance_ShakeItOff` |
| Customs (×3) | Etiquette/trade | ⚪ Defer |
| Persuasion (×7) | Social/diplomacy | ⚪ Defer |
| Physic (×4) | Medical | ⚪ Defer |
| Tactics (×9) | Movement tricks | 🔵 Some port-fits — esp. `Tactics_Charge` |
| TenfoldPath (×9) | Mental discipline | ⚪ Defer |
| Nonlinearity (×1) | Time travel | ⚪ Defer |

### WSP6 — Tier-3 batch (in flight)

**Goal:** ship 3-5 high-value Tier-3 active abilities that don't
require new architectural layers (no LongBladesCore, no Furniture
system, no ranged combat, no stance machine).

**WSP6 candidates** (in priority order):

1. ✅ **Cudgel_Slam** (shipped WSP6.1) — knockback active. Mechanic:
   adjacent target pushed up to 3 cells in slam direction; cells
   blocked by solid terrain count as wall hits → bonus weapon-roll
   damage + Stunned (1-4T scaling with cells crossed + walls hit).
   Simplified-Qud port — Qud's wall-destruction + creature-chain
   variants deferred to v2 (need wall-AV system + chain-recursion
   semantics). 50T cooldown, requires Cudgel weapon equipped.
2. ✅ **ShortBlades_Puncture** (shipped WSP6.6) — first Tier-3 passive
   port. +2 penetration on every melee swing made with a Piercing-
   attribute weapon. Required adding the new `OnGetPenetrationModifier`
   hook (the first new combat virtual since the system shipped — the
   §"Adding a new combat event" mechanical pattern is now exercised).
   Match-classification per Qud's `ShortBlades_Puncture.cs`. The "AV - 2"
   framing in Qud is mathematically equivalent to the "+2 pen" framing
   in CoO; we use the latter because it's clearer at the call site.
3. ⏭️ **Axe_Decapitate** — finishing move active (next ship)
3. ⏭️ **Axe_Dismember** — crit-chance dismember passive
4. ⏭️ **Axe_HookAndDrag** — pull-adjacent active
5. ⏭️ **LongBladesDeathblow** — finishing move active
6. ⏭️ **ShortBlades_Shank** — first-hit-of-turn passive
7. ⏭️ **ShortBlades_Puncture** — pen-buff active
8. ⏭️ **ShortBlades_PointedCircle** — AOE adjacent active
9. ⏭️ **LongBladesSwipe** — 3-cell arc AOE active

The stance-batch (LongBladesCore + 3 stances + 3 improved + Lunge)
is deferred to **WSP7+** as a separate multi-commit feature.
