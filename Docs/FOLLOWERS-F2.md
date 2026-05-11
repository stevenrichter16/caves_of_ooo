# Followers System — Phase F.2: Recruitment

> **Living plan + findings document.** Phase F.2 adds the verb that
> populates the F.1 data substrate: a player skill that recruits an NPC,
> producing the same shape as Qud's `Persuasion_Proselytize → Proselytized`
> flow. NO slot system, NO faction-rep-as-follower content yet — those
> land in F.3. See [`FOLLOWERS-ROADMAP.md`](FOLLOWERS-ROADMAP.md) for the
> multi-phase overview and [`FOLLOWERS.md`](FOLLOWERS.md) for the F.1 plan.

---

## Status banner

| Field | Value |
|---|---|
| **Phase** | F.2 — Recruitment ⏳ IN PROGRESS |
| **Last updated** | 2026-05-10 |
| **Branch** | `feat/followers-f2-recruitment` |
| **Sub-milestones complete** | 4 / 5 |
| **Real bugs found** | 0 |
| **Contracts pinned** | 36 (RecruitedEffect 12 + Persuasion_Recruit 15 + Persuasion_Dismiss 9) |
| **Tests added** | 36 EditMode tests across 3 new fixtures |

---

## Goal

Ship the **recruitment verb**: a player-owned activated ability that
mental-attacks an adjacent NPC; on success, installs `RecruitedEffect`
on the target which (via F.1.2's `SetPartyLeader`) makes the target
follow the player. Plus the symmetric **dismiss** verb that removes
the effect and cleanly releases the follower.

Mirror Qud's `Persuasion_Proselytize` + `Proselytized` flow as the
reference architecture (per user direction: "reimplement Qud's
follower system cleanly, then modify and adjust to fit my game and
lore"). CoO-specific adaptations are flagged with 🟡 or ⚪ markers.

---

## Reference — Qud's pattern

| File | Symbol | What it does |
|---|---|---|
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:142-226` | `AttemptProselytization()` | Veto chain → pick target → roll mental attack → apply `Proselytized` effect → consume 1000 energy + 25-turn cooldown |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:228-236` | `Proselytize(MentalAttackEvent)` | On penetration: `defender.ApplyEffect(new Proselytized(attacker))` |
| `XRL.World.Effects/Proselytized.cs:86-112` | `Apply(GameObject)` | Validates Proselytizer + Brain, fires `ApplyProselytize` event, plays sound, adds opinion, **`SetAlliedLeader<AllyProselytize>(Proselytizer)`** (the link itself), calls `SyncTarget` to enforce slot limit |
| `XRL.World.Effects/Proselytized.cs:114-124` | `Remove(GameObject)` | If still under the proselytizer's leadership: clears `PartyLeader`, clears goal stack, removes `AllyProselytize` allegiance |
| `XRL.World.Effects/Proselytized.cs:63-84` | `HandleEvent(GetInventoryActionsEvent)` + `HandleEvent(InventoryActionEvent)` | Surfaces "Dismiss" action when the Proselytizer looks at the proselyte; on click → removes effect + plays sound + consumes 100 energy + fires `DismissedFromService` event |

The Qud pattern is **a Skill that produces an Effect that owns the
link + the dismiss UX**. The Effect is the dispatcher — UI surfaces
just call `target.RemoveEffect<Proselytized>()`. F.2 mirrors this
architecturally; the UI surface choice is documented below.

---

## Verification sweep — Pre-impl corrections table

Per CLAUDE.md §1.2. All file:line citations into the current CoO tree.

| What I assumed | What's there | Status |
|---|---|---|
| `Effect` base class supports `Owner` Entity ref + Apply/Remove hooks + reflection save | `Effect.cs:51` (Owner), `:126-130` (Apply→OnApply), `:140-143` (Remove→OnRemove), `:247-253` (OnBeforeSave/OnAfterLoad). Owner is excluded from `WritePublicFields` (Effect.Owner skipped at SaveSystem.cs:1622), so any new Recruiter Entity ref needs its own public field. | ⚪ confirmed |
| `BurningEffect` / `BerserkEffect` are reference templates for OnApply/OnRemove + OnStack | `BerserkEffect.cs:38-68` — same lifecycle, stat-Bonus mutation in OnApply, restored in OnRemove, OnStack refreshes duration. RecruitedEffect mirrors this shape. | ⚪ confirmed |
| `BaseSkillPart` supports `ActivatedAbilitySpec` with `Cooldown`, `Class`, `TargetingMode=AdjacentCell`, `Range=1` | `Cudgel_Slam.cs:46-57` and `Cudgel_Conk.cs:29-37` both use this exact shape. Cooldown is applied by `SkillsPart` on successful `OnCommand`. | ⚪ confirmed |
| Adjacent-target picker exists | `SkillCombatHelpers.FindAdjacentCleaveTarget(actor, actor, zone)` (Cudgel_Conk.cs:69) — returns first adjacent Creature in 8-dir N→NW order. Reusable. | ⚪ confirmed |
| `BrainPart` has goal-stack mutation API for `RecruitedEffect.Apply` to push `FollowLeaderGoal` | `BrainPart.cs:493-505` — `PushGoal(goal)` calls `goal.OnPush()`, `RemoveGoal(goal)` calls `goal.OnPop()`. `HasGoal<T>()` exists. No locking constraints. | ⚪ confirmed |
| Goal stack survives serialization via reflection — `FollowLeaderGoal` doesn't need a manual save dispatch | `SaveSystem.cs:1512-1539` — `SaveGoal` uses `WritePublicFields` which dispatches via `WriteFieldValue` → `WriteEntityReference` for `typeof(Entity)` fields (line 1685). `FollowLeaderGoal.Leader` (Entity field) round-trips automatically with SL.8 identity preserved. | ⚪ confirmed |
| `Ego` stat exists and is blueprinted | `Objects.json:142, 1286, 1721, 1878, ...` (10+ creatures with `Ego` value). Used in `TradeSystem.cs:45`, `TelepathyMutation.cs:33`, `InventoryScreenData.cs:167`. | ⚪ confirmed |
| `Level` stat exists | `BaseMutation.cs:143` (`GetStatValue("Level", 1)`), `UnstableGenomeMutation.cs:41`, `SaveSystem.cs:388` save it as `PlayerLevel`. Defense roll viable. | ⚪ confirmed |
| `StatUtils.GetModifier(entity, statName)` returns `(score - 16) / 2` | `StatUtils.cs:16-28`. D&D-style modifier — perfect for the d20-based roll. | ⚪ confirmed |
| Hostility check is centralized in `FactionManager.GetFeeling` | `FactionManager.cs:175-206` — F.1.4 already injects the `ArePartyAligned` exception there. `IsHostile` returns true if feeling ≤ `HOSTILE_THRESHOLD` (-10). | ⚪ confirmed |
| Diag emission pattern | Existing pattern: `Diag.EmitSkillRouted(...)` / `Diag.EmitSkillRejected(...)`. See `Cudgel_Slam.cs:71-72` (`EmitSkillRejectedDiag(ctx, "reason_string")`) — already provided by `BaseSkillPart`. | ⚪ confirmed |
| `Forgive` step already in F.1.2 — `SetPartyLeader` calls `PersonalEnemies.Remove(newLeader)` on the recruit | `BrainPart.SetPartyLeader` (F.1.2 ship). `RecruitedEffect.Apply` should NOT duplicate this — relying on the data substrate keeps the contract in one place. | ⚪ confirmed |

**No false premises detected.** The Explore agent's prerequisite
survey had one error (claimed `Level` stat was missing); cross-check
confirmed it exists. All other surfaces are ready.

---

## 5 Design Lockdowns

Per the F.2 prerequisite analysis. These are pinned in-doc BEFORE any
code is written, so the sub-milestones implement to a known spec.

### Lockdown #1 — Roll formula

**Decision:** d20 + Ego-modifier vs. dynamic DC; Qud-flavored.

```
roll  = d20 + StatUtils.GetModifier(attacker, "Ego")
DC    = RECRUIT_BASE_DC + max(defender.Level - attacker.Level, 0) + alreadyAffectedMod
hit   = roll ≥ DC
```

| Constant | Value | Rationale |
|---|---|---|
| `RECRUIT_BASE_DC` | 10 | TTRPG-standard moderate DC; an Ego-15 character (modifier +0 in CoO's range, modifier is `(score-16)/2`) needs to roll ≥10 against a same-level target — coinflip baseline |
| `RECRUIT_COOLDOWN_TURNS` | 25 | Direct Qud parity (`Persuasion_Proselytize.COOLDOWN = 25`) |
| `RECRUIT_ENERGY_COST` | 1000 | Direct Qud parity (`ParentObject.UseEnergy(1000, ...)`) — though CoO turn system may differ; map to 1 full turn |
| `alreadyAffectedMod` | +0 | F.2 v1 simplification: deny the recast outright (see Veto #4). Mod-bonus pattern from Qud preserved as a constant for F.5+ when multiple paths land. |

**Why d20 vs Qud's `MentalAttack(... "1d8-6", penetrations=2, ...)`:**
Qud's roll is a damage-style penetration check using the MA stat,
which CoO doesn't have. The d20/DC pattern preserves the
*intent* (Ego vs Level differential gates recruitment) in CoO's
existing dice idiom — same shape used for skill ranks, save throws,
and ability checks elsewhere in the engine. **🟡 Divergent** per
CLAUDE.md §4.2: same gameplay direction, simpler arithmetic.

### Lockdown #2 — Veto list (in evaluation order)

A `Persuasion_Recruit.OnCommand` must reject and emit
`category=skill kind=RecruitRejected reason=<string>` for each of:

| Order | Veto | reason= | Qud parity |
|---|---|---|---|
| 1 | `ctx.Attacker == null \|\| ctx.Zone == null \|\| ctx.Rng == null` | `null_context` | implicit |
| 2 | No adjacent Creature found | `no_target` | match (line 159) |
| 3 | `target == attacker` | `self_target` | match (line 168) |
| 4 | `target.GetPart<BrainPart>() == null` | `target_no_brain` | match — Qud `Object.Brain == null` (Proselytized.cs:92) |
| 5 | `target.GetEffect<RecruitedEffect>() != null` (already recruited by anyone) | `already_recruited` | **🟡 stricter than Qud** — Qud allows over-recruit with +1 DC (line 184-186); F.2 v1 denies outright. Re-evaluate when multiple recruitment paths land. |
| 6 | `target.GetPart<BrainPart>().PartyLeader != null && != attacker` | `follows_another` | match-ish — Qud line 192 prompts "are you sure" via popup; CoO has no recruitment popup, so deny outright |
| 7 | `FactionManager.GetFeeling(target, attacker) <= FactionManager.HOSTILE_THRESHOLD` | `target_hostile` | **🟡 stricter than Qud** — Qud doesn't gate on hostility (relies on combat/conversation possibility). CoO design choice: can't recruit something actively trying to kill you. Players can resolve hostility first (de-escalation skills, faction rep) then recruit. |
| 8 | `target.GetPart<BrainPart>().PersonalEnemies.Contains(attacker)` | `personal_grudge` | match — Qud's `CheckInfluence` (line 196) covers similar territory |

If none of 1-8 trip, the roll proceeds (Lockdown #1). On roll failure,
emit `RecruitRejected reason=roll_failed payload={roll, dc}`. On roll
success, apply `RecruitedEffect`.

**Veto features explicitly NOT ported:**

| Qud feature | CoO status | Rationale |
|---|---|---|
| Missing-tongue check (line 144-148) | ⚪ skip | CoO has no tongue mechanic |
| `CheckFrozen` (line 149, 180) | ⚪ skip | CoO Frozen effect could plug in here later (F.5+) |
| `IsCombatObject() && HasStat("MA") && HasStat("Level")` filter (line 158) | partial | CoO targets are creatures (Tags["Creature"]) with Level stat; MA filter inapplicable |
| `IsOriginalPlayerBody` / `HasCopyRelationship` (line 168) | ⚪ skip | CoO has no body-swap mechanic |
| `ConversationScript.IsPhysicalConversationPossible` (line 188) | ⚪ skip | CoO targets all have BrainPart by definition |
| `SifrahRecruitment` minigame option (line 215-217) | ⚪ skip | CoO has no minigames yet |

### Lockdown #3 — Forgive contract

**Decision:** Re-use F.1.2's existing `SetPartyLeader` Forgive step.
Do NOT duplicate.

```csharp
// In RecruitedEffect.OnApply:
target.GetPart<BrainPart>().SetPartyLeader(Recruiter);
// SetPartyLeader internally:
//   - PersonalEnemies.Remove(newLeader)  ← Forgive (F.1.2 ship)
//   - Bidirectional mirror (F.1.2 ship)
//   - Cycle detection (F.1.2 ship)
```

`Persuasion_Recruit`'s `Veto #8` (PersonalEnemies grudge) intentionally
**fails BEFORE** SetPartyLeader can call Forgive. Rationale: a target
that personally hates you shouldn't be coerce-convertible via a single
skill cast. The Forgive step exists to handle *previously-hostile-but-
no-longer-actively-grudging* targets (e.g. you Calmed them last turn).

⚪ This is direct Qud-spirit parity. Qud's `Forgive` runs inside
`SetPartyLeader` regardless of how the leader was set — the
gatekeeping happens in the skill, not the effect.

### Lockdown #4 — Dismiss surface

**Decision:** Mirror Qud architecturally: **the Effect is the
dispatcher**. F.2 ships ONE trigger surface (an activated ability);
future surfaces (right-click menu, conversation action) call the
same dispatcher method.

```csharp
public class RecruitedEffect : Effect
{
    public Entity Recruiter;

    /// <summary>Public dispatch point for all dismiss surfaces.
    /// UI surfaces (activated ability, right-click menu, conversation
    /// action) call this; the effect handles the rest.</summary>
    public void Dismiss(Entity dismisser)
    {
        // (1) authorization: only the recruiter can dismiss
        if (dismisser != Recruiter) return;
        // (2) follower's brain pop FollowLeaderGoal + clear leader
        var brain = ParentEntity?.GetPart<BrainPart>();
        brain?.SetPartyLeader(null);
        // (3) effect removes self
        ParentEntity?.RemoveEffect(this);
    }
}
```

**Trigger surface (F.2.4):** New skill `Persuasion_Dismiss` with the
same `ActivatedAbilitySpec` shape — adjacent-cell targeting, picks
adjacent Creature with `PartyLeader == attacker`, calls
`target.GetEffect<RecruitedEffect>()?.Dismiss(attacker)`. Symmetric
veto chain (target null, target not your follower, etc.) with diag
records.

**Why not the inventory-actions surface (Qud's path):** CoO doesn't
yet have a "right-click on entity → context menu" surface for NPCs.
Building that is a separate UI shipment (F.5+). The activated-ability
path uses already-present infra and produces an equivalent player
flow ("I press a button to dismiss"). When the right-click surface
lands, it calls the same `RecruitedEffect.Dismiss()` method — zero
re-wiring needed.

**🟡 Divergent from Qud:** Qud's dismiss is one ability ("Dismiss
Companions" — line 67 of Proselytized.cs reads `WorksAtDistance:
true`). CoO F.2 v1 keeps dismiss adjacent-only for symmetry with
recruit. Distance-dismiss can land later as a constant tweak.

**🟡 Energy cost:** Qud's dismiss costs 100 energy
(`CompanionDirectionEnergyCost(..., 100, "Dismiss")` line 79). F.2
mirrors: `RECRUIT_ENERGY_COST = 1000`, `DISMISS_ENERGY_COST = 100`.
No cooldown on dismiss (Qud has none either).

### Lockdown #5 — Diag records

Per CLAUDE.md "every gate emits a diag record". `Persuasion_Recruit`
and `Persuasion_Dismiss` each emit:

| Category | Kind | When | Payload |
|---|---|---|---|
| `skill` | `CommandRouted` | OnCommand invoked | `command=Persuasion_Recruit`, `actor=<id>` |
| `skill` | `RecruitRejected` | any veto from Lockdown #2 fires | `reason=<string>`, `actor=<id>`, `target=<id-or-null>` |
| `skill` | `Recruited` | roll succeeded + RecruitedEffect applied | `actor=<id>`, `target=<id>`, `roll=<int>`, `dc=<int>` |
| `skill` | `Dismissed` | RecruitedEffect.Dismiss completes | `actor=<id>`, `target=<id>` |
| `skill` | `DismissRejected` | veto on dismiss command | `reason=<string>`, `actor=<id>`, `target=<id-or-null>` |

Verification flow per CLAUDE.md "Observability":
```
diag_assert category=skill kind=Recruited
  → if matched=false, the success path never fires → investigate veto chain
  → if matched=true, the success path fires → investigate downstream (FollowLeaderGoal push, save round-trip)
```

Tests pin emissions per the SkillsPart precedent
(`SkillsPartTests.HandleEvent_SuccessfulRoute_EmitsCommandRoutedDiag`).

---

## Sub-milestones — smallest blast radius first

Per CLAUDE.md §1.4. Each commits as one reviewable change,
independently revertable, ships one complete testable behavior.

### F.2.1 — Plan + verification sweep + design lockdowns (this commit)

Doc only. No code changes. Pin the 5 lockdowns above so F.2.2-F.2.4
implement to a fixed spec.

### F.2.2 — `RecruitedEffect` (Apply/Remove/Dismiss + goal-stack + round-trip)

**New file:** `Assets/Scripts/Gameplay/Effects/Concrete/RecruitedEffect.cs`

```csharp
public class RecruitedEffect : Effect
{
    public override string DisplayName => "recruited";

    /// <summary>Entity that recruited this follower. Authorizes dismiss
    /// and is the leader written into BrainPart.PartyLeader on Apply.</summary>
    public Entity Recruiter;

    /// <summary>Reference to the FollowLeaderGoal pushed on Apply, so
    /// OnRemove can pop it without ambiguity (the goal could be N-deep
    /// in the stack if other goals layered on top).</summary>
    public FollowLeaderGoal PushedGoal;

    public RecruitedEffect() { Duration = -1; } // permanent until Dismiss
    public RecruitedEffect(Entity recruiter) : this() { Recruiter = recruiter; }

    public override void OnApply(Entity target)
    {
        if (Recruiter == null || target == null) return;
        var brain = target.GetPart<BrainPart>();
        if (brain == null) return;
        brain.SetPartyLeader(Recruiter);    // F.1.2 — Forgive + bidirectional mirror
        PushedGoal = new FollowLeaderGoal { Leader = Recruiter };
        brain.PushGoal(PushedGoal);
        MessageLog.Add(target.GetDisplayName() + " joins " + Recruiter.GetDisplayName() + "!");
    }

    public override void OnRemove(Entity target)
    {
        var brain = target?.GetPart<BrainPart>();
        if (brain == null) return;
        if (PushedGoal != null) brain.RemoveGoal(PushedGoal);
        // Only clear leader if the recruiter is still the current leader.
        // Defends against a sequence where the target was re-recruited by
        // someone else mid-effect: in that case the new recruiter's effect
        // owns the link.
        if (brain.PartyLeader == Recruiter) brain.SetPartyLeader(null);
    }

    public void Dismiss(Entity dismisser)
    {
        if (dismisser == null || dismisser != Recruiter) return;
        ParentEntity?.RemoveEffect(this);   // triggers OnRemove
    }

    public override bool OnStack(Effect incoming) => false;  // non-stacking; re-cast handled at skill veto level
}
```

**TDD test surface** (~10-12 tests in new `Effects/RecruitedEffectTests.cs`):
- `OnApply_SetsPartyLeader_ToRecruiter`
- `OnApply_PushesFollowLeaderGoal_OntoBrain`
- `OnApply_NullRecruiter_NoOp`
- `OnApply_TargetWithoutBrain_NoOp`
- `OnRemove_ClearsPartyLeader_IfRecruiterStillLeader`
- `OnRemove_DoesNotClearLeader_IfReRecruitedByOther` (counter-check)
- `OnRemove_PopsFollowLeaderGoal_FromStack`
- `Dismiss_ByRecruiter_RemovesEffect`
- `Dismiss_ByNonRecruiter_NoOp` (counter-check — authorization)
- `Dismiss_ByNull_NoOp` (null-safety)
- `RecruitedEffect_SurvivesRoundTrip_WithRecruiterIdentity_Preserved` (SL.8 contract via Recruiter Entity field)
- `RecruitedEffect_AppliedTwice_OnStack_ReturnsFalse` (forces re-apply, prior effect remains)

### F.2.3 — `Persuasion_Recruit` skill (activated ability + roll + vetos + diag)

**New file:** `Assets/Scripts/Gameplay/Skills/Persuasion_Recruit.cs`

Skeleton:
```csharp
public class Persuasion_Recruit : BaseSkillPart
{
    public override string Name => nameof(Persuasion_Recruit);

    public const int COOLDOWN = 25;
    public const int BASE_DC = 10;
    public const int ENERGY_COST = 1000;

    public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        => new ActivatedAbilitySpec {
            DisplayName = "Recruit",
            Command = "CommandRecruit",
            Class = "Persuasion",
            TargetingMode = AbilityTargetingMode.AdjacentCell,
            Range = 1,
            Cooldown = COOLDOWN,
        };

    public override void OnCommand(SkillEventContext ctx)
    {
        // Veto chain per Lockdown #2, each emits RecruitRejected diag
        if (ctx?.Attacker == null || ctx.Zone == null || ctx.Rng == null) {
            EmitSkillRejectedDiag(ctx, "null_context"); return;
        }
        var actor = ctx.Attacker;
        var target = SkillCombatHelpers.FindAdjacentCleaveTarget(actor, actor, ctx.Zone);
        if (target == null) { EmitSkillRejectedDiag(ctx, "no_target"); return; }
        if (target == actor) { EmitSkillRejectedDiag(ctx, "self_target"); return; }
        var brain = target.GetPart<BrainPart>();
        if (brain == null) { EmitSkillRejectedDiag(ctx, "target_no_brain"); return; }
        if (target.GetEffect<RecruitedEffect>() != null) { EmitSkillRejectedDiag(ctx, "already_recruited"); return; }
        if (brain.PartyLeader != null && brain.PartyLeader != actor) {
            EmitSkillRejectedDiag(ctx, "follows_another"); return;
        }
        if (FactionManager.GetFeeling(target, actor) <= FactionManager.HOSTILE_THRESHOLD) {
            EmitSkillRejectedDiag(ctx, "target_hostile"); return;
        }
        if (brain.PersonalEnemies.Contains(actor)) {
            EmitSkillRejectedDiag(ctx, "personal_grudge"); return;
        }

        // Roll
        int roll = ctx.Rng.Next(1, 21) + StatUtils.GetModifier(actor, "Ego");
        int dc = BASE_DC + System.Math.Max(
            target.GetStatValue("Level", 1) - actor.GetStatValue("Level", 1), 0);
        if (roll < dc) {
            EmitSkillRejectedDiag(ctx, "roll_failed", payload: new { roll, dc });
            MessageLog.Add(target.GetDisplayName() + " is unconvinced by your pleas.");
            return;
        }

        // Apply
        target.ApplyEffect(new RecruitedEffect(actor), actor, ctx.Zone);
        Diag.EmitSkill("Recruited", actor, payload: new { target = target.ID, roll, dc });
    }
}
```

**TDD test surface** (~12-15 tests in `Skills/PersuasionRecruitTests.cs`):
- All 8 veto cases (each → no effect applied + correct rejection reason)
- Roll success → RecruitedEffect applied + Recruited diag emitted
- Roll failure → no effect + roll_failed diag with payload
- Cooldown applied on success (via BaseSkillPart contract)
- Counter-check: high-Ego attacker beats high-Level defender consistently across many seeds (positive Ego advantage)
- Counter-check: low-Ego attacker mostly fails against high-Level defender
- Deterministic with seeded Rng

### F.2.4 — `Persuasion_Dismiss` skill (symmetric dismiss verb)

**New file:** `Assets/Scripts/Gameplay/Skills/Persuasion_Dismiss.cs`

Same shape as Persuasion_Recruit but:
- No cooldown
- `ENERGY_COST = 100`
- Picks adjacent Creature with `target.GetEffect<RecruitedEffect>()?.Recruiter == actor`
- Calls `effect.Dismiss(actor)`
- Diag records: `Dismissed` / `DismissRejected reason=<string>`

**TDD test surface** (~6-8 tests in `Skills/PersuasionDismissTests.cs`):
- Dismiss your follower → effect removed, PartyLeader cleared
- Dismiss non-follower (someone else's) → DismissRejected reason=not_your_follower
- Dismiss target with no RecruitedEffect → DismissRejected reason=no_recruited_effect
- Dismiss with no adjacent target → DismissRejected reason=no_target
- Counter-check: pre-dismiss the follower has FollowLeaderGoal on stack; post-dismiss it's gone
- Counter-check: pre-dismiss FactionManager.GetFeeling(follower, you) == ALLIED_FEELING; post-dismiss reverts to faction default

### F.2.5 — Adversarial sweep + cold-eye review + merge

Per CLAUDE.md "Adversarial test sweep" gate — F.2 hits 4+ taxonomy surfaces:
- **State atomicity** (Apply must fully install OR fully bail; partial state would be a bug)
- **Cross-actor flows** (Recruiter ≠ Recruit; recruit may have other followers; chain recruit)
- **Save/load reach** (RecruitedEffect.Recruiter must round-trip with SL.8 identity)
- **Stacking semantics** (re-apply attempt, dismiss-then-re-recruit, double-dismiss)
- **Anti-exploit gates** (each veto stays correct under unusual inputs)
- **Probability boundaries** (Ego ±N edge cases)
- **Diag dispatch invariants** (every gate emits exactly one record)

**Adversarial fixture:** `Effects/RecruitedEffectAdversarialTests.cs` + `Skills/PersuasionRecruitAdversarialTests.cs` (~20-30 tests). Coverage:

| Surface | Probe |
|---|---|
| State atomicity | Veto #4 (no brain) fires → no SetPartyLeader, no goal push, no PartyMembers entry |
| Cross-actor | A recruits B; B recruits C — A's PartyMembers correctly reflects only B (per F.1's bidirectional contract) |
| Save/load reach | Recruit, save, load — RecruitedEffect.Recruiter is SAME reference as the actor in the post-load graph |
| Stacking | Recruit twice (same recruiter, same target) — Veto #5 fires; original effect unchanged |
| Anti-exploit | Recruit, dismiss, immediately re-recruit — succeeds (no lingering effect or veto trip) |
| Anti-exploit | Hostile target → Veto #7 → recruit rejected; target's hostility unchanged |
| Probability | Ego=20 attacker vs Level=1 defender — never misses across 100 seeds |
| Probability | Ego=8 attacker vs Level=20 defender — rarely succeeds, deterministic per seed |
| Diag dispatch | Each veto fires exactly one `RecruitRejected` record (not zero, not two) |
| Diag dispatch | Roll-failed path fires `RecruitRejected reason=roll_failed`, NOT `Recruited` |
| Null-safety | null actor, null target, null zone, null rng — graceful rejections, no NRE |
| Goal stack interaction | Recruit, then push a KillGoal on top — FollowLeaderGoal still pops correctly on dismiss |
| Mid-state save | Recruit, save mid-FollowLeaderGoal pursuit (Age=N) — round-trip preserves goal Age |

Cold-eye review per CLAUDE.md §Q1-Q4:
- Q1 symmetry — Recruit's OnApply has a mirror in OnRemove?
- Q2 cross-feature consistency — Persuasion_Recruit and Persuasion_Dismiss share veto-emission shape?
- Q3 counter-check completeness — every veto reason has both a positive (fires when veto trips) and a negative (does NOT fire when veto doesn't trip) test?
- Q4 doc-vs-impl drift — final constants match Lockdown #1's table?

**Merge to main** after cold-eye 0 substantive findings + adversarial 0 bugs.

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| F.2.1 Plan + verification sweep + design lockdowns | ✅ | — | `dc34e09` |
| F.2.2 RecruitedEffect | ✅ | 12 | `6dbb6c7` |
| F.2.3 Persuasion_Recruit skill | ✅ | 15 | `245eec3` |
| F.2.4 Persuasion_Dismiss skill | ✅ | 9 | (this commit) |
| F.2.5 Adversarial sweep + cold-eye + merge | ⏳ planned | — | — |
| **TOTAL** | **0 / 5** | **—** | — |

---

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `Effect` base class with OnApply/OnRemove + Owner serialization-skip | `Effects/Effect.cs:51, 126-143, 247-253` | `RecruitedEffect` shape |
| `BerserkEffect` template (state mutation in OnApply, restored OnRemove) | `Effects/Concrete/BerserkEffect.cs` | RecruitedEffect template |
| `BaseSkillPart.DeclareActivatedAbility + OnCommand` | `Skills/BaseSkillPart.cs:276-296` | Persuasion_Recruit + Persuasion_Dismiss |
| `Cudgel_Slam` / `Cudgel_Conk` template (AdjacentCell + adjacent target picker) | `Skills/Cudgel_Slam.cs:46-114`, `Cudgel_Conk.cs:29-69` | Persuasion_Recruit shape |
| `SkillCombatHelpers.FindAdjacentCleaveTarget` | `Skills/SkillCombatHelpers.cs` | Adjacent target picker (8-dir N→NW iteration) |
| `BrainPart.SetPartyLeader(newLeader)` + Forgive | `AI/BrainPart.cs` (F.1.2) | The link itself |
| `BrainPart.PushGoal / RemoveGoal / HasGoal<T>` | `AI/BrainPart.cs:493-505` (F.1.5) | Goal-stack auto-wire |
| `FollowLeaderGoal` | `AI/Goals/FollowLeaderGoal.cs` (F.1.5) | The goal we push |
| `FactionManager.GetFeeling + HOSTILE_THRESHOLD + ALLIED_FEELING` | `AI/FactionManager.cs` (F.1.4) | Veto #7 — hostile-target check |
| `StatUtils.GetModifier(entity, "Ego")` | `Stats/StatUtils.cs:16-28` | Roll math |
| `entity.GetStatValue("Level", default)` | `Entity.cs:100` | DC math |
| `EmitSkillRejectedDiag(ctx, reason)` | `BaseSkillPart` | Veto diag emission |
| `Diag.EmitSkill(...)` | `Shared/Utilities/Diag.cs` | Success diag emission |
| `PartRoundTripHelper.RoundTripEntityViaTokenGraph` | `Tests/TestSupport/PartRoundTripHelper.cs` | Save/load round-trip tests |

---

## Self-review pre-flagged findings

Designed-in tradeoffs to surface BEFORE committing (per CLAUDE.md §5):

- **🟡 d20-vs-MentalAttack divergence** — Qud uses a damage-style
  penetration roll (`1d8-6 + Ego mod` vs MA stat). CoO has no MA stat
  and no Penetrations concept. F.2 uses d20 + Ego-mod vs dynamic DC,
  same gameplay-direction (Ego beats Level differential). Flagged as
  Divergent (CLAUDE.md §4.2).
- **🟡 Recruit-on-existing-follower denied (Veto #5)** — Qud allows
  with +1 DC. CoO v1 simplifies to outright deny. Re-evaluate when
  Beguile / Rebuke land in F.5+ (the +1 DC stack interaction
  becomes more relevant when multiple effect types can coexist).
- **🟡 Hostile-target denied (Veto #7)** — Qud doesn't gate on this.
  CoO design choice: stops the "spam-recruit during combat" exploit
  and pushes players toward de-escalation skills (Calm mutation, etc.)
  as the recruit pre-step. Re-evaluate after playtest.
- **🟡 Dismiss is adjacent-only** — Qud's `Dismiss` is
  `WorksAtDistance: true`. F.2 v1 keeps it adjacent for symmetry with
  Recruit. Distance-dismiss = constant tweak when wanted.
- **🔵 No `IAllyReason` typed-allegiance yet** — F.1 deferred this to
  F.2; F.2 design lockdown is to ship one path (Recruit) without
  typed allegiance, then add the type-parameter slot when a second
  path (Beguile/Rebuke) lands in F.5+. The `RecruitedEffect` shape
  is forward-compatible with this: it carries the same `Recruiter`
  field that a future `BeguiledEffect.Beguiler` would carry, and
  both call the same `SetPartyLeader(...)` primitive.
- **🔵 Energy costs hardcoded** — CoO's turn system may interpret
  "1000 energy" differently than Qud's. If the playtest reveals the
  recruit ability fires too fast/slow, tune `ENERGY_COST` constant.
- **⚪ Conversation surface for dismiss** — Qud's dismiss is on the
  inventory-actions menu; CoO will eventually want an in-conversation
  "I release you" option. Deferred — same `RecruitedEffect.Dismiss()`
  dispatcher handles it when the conversation surface is added.
- **⚪ Sifrah minigame** — Qud option for minigame-driven recruitment.
  CoO has no minigame engine; deferred.

---

## Open design questions (carry forward)

- **Does the recruit ability cost both energy AND a turn?** CoO's
  action-cost model may differ from Qud's energy points. F.2.3
  implementation will clarify; tune as needed.
- **What's the visible UI cue for "Recruit"?** Currently relies on
  the activated-ability menu showing "Recruit". A future polish
  pass adds a "🤝" indicator in the abilities bar.
- **Mass-dismiss?** If the slot system (F.3) caps at e.g. 4 and the
  player wants to drop 3 in one go — is that a hotkey, a menu, or
  spammed F.2.4 ability? Deferred to F.3 design.

---

*Updated as each sub-milestone ships.*
