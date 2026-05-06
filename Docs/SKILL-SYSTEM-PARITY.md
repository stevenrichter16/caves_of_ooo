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
| WSP3.7 | (this commit) | `Docs/AUTHORING-SKILLS.md` worked-examples guide; impl log + roadmap update | n/a |

**Final state of the skill SYSTEM:**

- 5 trees registered (Acrobatics + 4 weapon classes)
- 22 skill classes across all tiers:
  - 5 tree-roots
  - 4 tree-root crit hooks (in the tree-root classes' `OnWeaponMadeCriticalHit`)
  - 5 power on-hit procs (Bludgeon / Cleave / Lacerate / Jab / Bloodletter)
  - 3 +to-hit passives (Expertise × 3 weapon classes)
  - 3 advanced passives (Hammer, ShatteringBlows, Hobble passive)
  - 2 on-miss / on-dodge passives (Backswing, Rejoinder)
  - 2 active abilities (Conk, Berserk)
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

**Adding a new combat event** (if you need a hook the existing 5
virtuals don't cover): add a new virtual on `BaseSkillPart`, add a
new entry-point on `SkillEventDispatcher`, wire the call site.
Pattern is mechanical — `WeaponMadeCriticalHit` is the most recent
copy-template.
