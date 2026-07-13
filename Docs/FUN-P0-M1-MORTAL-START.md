# FUN-P0 · M1 — The Mortal Start

> **Status:** In progress · Parent: `FUN-P0-SPINE.md`
> **Goal:** The player can die, numbers matter, and every acquisition
> system (XP, SP, MP, drams, tonics, loot) has felt value — by gating
> the debug loadout and feeding the already-built progression plumbing.

---

## Verification sweep corrections (§1.2) — what changed the plan

| # | Analysis claim | Sweep finding | Plan impact |
|---|---|---|---|
| C1 | Creatures "fall through to DefaultFist 1d2" | **Worse: `Body.RegenerateDefaultEquipment` is never called on the spawn path** (only callers: ExtraArm/Regeneration mutations + debug-F8). `GatherMeleeWeapons` finds no `_DefaultBehavior`, combat takes the `weapons.Count==0` branch → ONE hardcoded-"1d2" punch (CombatSystem.cs:162). **Even Snapjaws never swing their 1d4 claws.** Also: entity-level `MeleeWeaponPart` on creatures (Glowmaw 2d4, SleepingTroll 2d6…) is dead code — `PerformLegacyAttack` requires `body == null`, every creature has a Body. | M1.b needs an **engine fix**: materialize default behaviors at spawn (`InitializeAnatomy` → `body.UpdateBodyParts()`), accepting the two-swing consequence (primary + off-hand −2, off-hand always swings — CombatSystem.cs:27,169). Damage budgets sized for ~2× dice. |
| C2 | "Gate the debug loadout" | `InitializePlayerStartingTinkering` **attaches BitLockerPart** (GameBootstrap.cs:833-838) — the blueprint has none. Gating it leaves the player with no BitLocker; `ItemEnhancementShowcase` silently skips (:101-109). | Starter kit must attach an **empty** BitLockerPart. |
| C3 | "Set HP ~40" | Objects.json:247 has **Value 500 AND Max 500** — both must change. No test pins 500. LevelingSystem grants +2 Max/level. | Edit both fields. |
| C4 | Flag pattern | `GraphicsPolish.IsEnabled` is `const` — untestable both-branches. Mutable precedent: `AIDebug.AIInspectorEnabled` (static bool, scenario-flipped, test-reset in finally). | `StartingLoadout.DebugGrantsEnabled` = **static bool, default false**. |
| C5 | Gate placement | DoStart order: CreateEntity(:247) → grants(:256-258) → **PlacePlayerInOpenCell(:259)** → spawns(:260-261). Spawn methods read player position. | Gate wraps :256-258 and :260-261 separately; :259 stays unconditional. |
| C6 | "62 of 64 mutations ignore rank" | 30 concrete mutations; **25 ignore rank** (5 consume Level: FlamingHands, Regeneration, Telepathy, Calm, UnstableGenome). Projectile dice are flat per-class overrides. | Rank-scaling deferred to P1 (MutationsScreen workstream); M1.d ships only the MP stat so the +1 MP/level grant and `SpendMPToIncreaseMutation` stop no-oping. |
| C7 | MP fix shape | `Entity.GainMP` requires a "MP" **Stat** (not IntProp); pure content fix: add `{Name:"MP",Value:0,Min:0,Max:999}` to Player Stats. Old saves rebuild stats from file → **existing saves keep lacking MP** (no migration in P0; documented). Sidebar switches "MP -" → "MP 0" (no test pins the dash). | One-line JSON + content-pin test. |
| C8 | XP inheritance | Base-Creature XPValue 10 is Objects.json:144 (not :149). **SnapjawChieftain inherits 15 from Snapjaw**, other three bosses inherit 10. Stats-array shape: `{Name, Value, Min, Max}`, Max defaults 999 via DTO. | Boss values are explicit `XPValue` stat entries per boss. |
| C9 | Requires field | Validation is BuySkillAction.cs:155-161 (`MeetsRequires`, comma-separated **class names**, AND semantics); **PowerData only** — root SkillData has no Requires field (silently dropped by JsonUtility). | Requires chains authored on Powers only; tree-root gating out of scope (code change — P1 if wanted). |
| C10 | Re-costing fallout | Five real-registry pin tests assert `Cost == 1` (Wsp6:744, Wsp7:343, Wsp71:410+423, Wsp72:312, Wsp73:267); `SkillTreeShowcase.cs:40` prebuys with SP 500 budget. | Update pins in same commit; recompute showcase budget. |
| C11 | Killer gate | Only `killer.HasTag("Player")` earns kill XP (CombatSystem.cs:1052-1054). | XP pass affects player kills only — no NPC-farming interactions. |

## Scope

**In:** M1.a loadout flag + statline · M1.b spawn materialization + natural-weapon content pass · M1.c XPValue pass · M1.d MP stat · M1.e skill re-costing + Requires.
**Out (pruned, rationale):** rank-scaling projectiles (25/30 mutations ignore rank — that's the P1 MutationsScreen beat); save-migration for MP (RPG saves predating P0 are dev saves); root-skill Requires (needs code, P1); off-hand swing-chance gate (Qud parity question — ship two-swing, tune via dice budget).

## Content readiness

- 🟢 All engine surfaces exist (flag pattern, stats pipeline, NaturalWeapon prop + factory, XP path, MP economy, Requires validation).
- 🟢 Blueprint JSON shapes confirmed (stats array / props / IntProps).
- 🟡 Two-swing fallout on message-log/diag-count tests — bounded by full-suite run per sub-milestone.
- ⚪ No new art/content assets needed.

## Sub-milestones (smallest blast radius first)

### M1.a — `StartingLoadout` + real statline
- New `Assets/Scripts/Gameplay/Entities/StartingLoadout.cs`:
  `public static bool DebugGrantsEnabled = false;` +
  `ApplyStarterKit(Entity player, EntityFactory factory)` → Dagger + 2×HealingTonic
  into InventoryPart + ensure **empty** BitLockerPart (C2).
- `GameBootstrap.DoStart`: `ApplyStarterKit` always; the five debug calls behind
  `if (StartingLoadout.DebugGrantsEnabled)` split around `PlacePlayerInOpenCell` (C5).
- Objects.json Player: Hitpoints 40/40 (C3), Strength/Agility/Toughness 18→16,
  Drams 50→10. Ink 50 unchanged (rental loop must stay reachable pre-P1-faucet).
- Tests (`StartingLoadoutTests.cs`, new): kit contents; BitLockerPart attached &
  empty; counter-checks: no FireBoltMutation / no recipes / no 8-tonic pile with
  flag false; flag-true branch grants (flipped in try/finally per AIDebug pattern).
  Blueprint pins (LevelingSystemTests pattern): HP 40/40, Drams 10.

### M1.b — Materialize natural weapons at spawn + weapon pass
- Engine: `EntityFactory.InitializeAnatomy` calls `body.UpdateBodyParts()` after
  the NaturalWeapon-prop override loop (C1). This makes ALL humanoids two-swing
  (primary, off-hand −2 always-swings).
- Factory: new cases in `NaturalWeaponFactory` (dice budgeted for 2× swings):
  BearClaw 1d4+p1, GolemFist 1d6+p2, SpiderFang 1d3 (+existing venom hooks later),
  ApeFist 1d4, SentryBlade 1d5+p1, ProwlerClaw 1d5+p1, StalkerClaw 1d5+p1,
  GuardianFist 1d6+p2, WurmBite 1d6+p1, BatBite 1d2, SlimePseudopod 1d3,
  ViperFang 1d2, ScorpionSting 1d3+p1, ScavengerClaw 1d3, BanditKnife 1d4.
- Content: `NaturalWeapon` Props on all 31 bare hostiles (worksheet in sweep
  answer B); Glowmaw/SleepingTroll/MimicChest/AmbushBandit get props matching
  their (dead) MeleeWeapon parts — parts left in place (content tests spawn them).
- Tests: factory-case pins (WeaponAttributesContentTests pattern); spawn
  materialization test (blueprint creature → hands materialized, GatherMeleeWeapons
  returns 2 slots); content pins for representative Props (ResistanceStatsContentTests
  pattern); counter-check: creature WITHOUT prop still materializes DefaultFist 1d2×2.
- Known fallout to fix same-commit: BodyPartSystemTests.DefaultBehavior pins,
  any AI/scenario diag-count drift surfaced by the full suite.

### M1.c — XPValue pass
- Explicit `XPValue` stats: bosses SnapjawChieftain 150 / DesertProwler 200 /
  JungleStalker 200 / AncientGuardian 400; wilderness tier-1 10-25, tier-2 40-80
  (worksheet). SleepingTroll 60→75, ChoirTendril 100 (80 HP lore heavy).
- Tests: content pins per boss + representative wilderness; counter-check:
  base-Creature default stays 10.

### M1.d — MP stat
- Player blueprint Stats += `{Name:"MP", Value:0, Min:0, Max:999}` (C7).
- Tests: blueprint pin; level-up grants MP=1 on blueprint player (RED today —
  proves the no-op); counter-check: UseMP fails at 0.

### M1.e — Skill re-costing + Requires chains
- All trees: root Cost 1, mid powers 2, capstones 3-4; Acrobatics from 100/50×4
  → 1/2s (Dodge 2, Tumble 2, EvasiveRoll 3, Vault 2).
- Requires (Powers only, class names, C9): e.g. Pyromancy_Pyroclasm requires
  Pyromancy_Cinder; Pyromancy_HeartFlame requires Pyromancy_ScorchRetort;
  Axe_Decapitate requires Axe_Dismember; ShortBlades_Shank requires
  ShortBlades_Puncture; analogous per tree (full table in implementation log).
- Update the five real-registry pin tests + SkillTreeShowcase SP budget (C10).
- Tests: new content-pin test for costs+Requires; RED gate test: buying a
  capstone without prereq → MissingPrereq.

## Performance note
No per-frame paths touched. `UpdateBodyParts` at spawn is once-per-entity-creation
(zone gen); factory Create allocations are per-spawn, not per-turn.

## Divergences from Qud
- Off-hand always swings at −2 (no Qud 15% offhand gate) — pre-existing CoO
  contract, now player-visible; dice budgets compensate. Documented, not changed.
- Skill costs use CoO scale (1/2/3-4) not Qud SP economy — CoO earns ~1 SP/level.

## Implementation log
(filled per sub-milestone)
