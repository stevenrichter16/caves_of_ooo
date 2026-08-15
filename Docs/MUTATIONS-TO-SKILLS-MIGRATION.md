# Mutations → Skills: Consolidation Plan (v2)

> Status: **READY FOR REVIEW** — v1 was reviewed by 4 adversarial critics
> (57 findings: 10 blockers, 30 major, 17 minor; all four returned
> *revision-required*). Every blocker was re-verified first-hand before
> this rewrite. v1's §1 contained **four false premises**, corrected
> below and preserved in §8 as the record.
>
> Goal: delete the mutation system; everything it does becomes a skill.
> **USER DIRECTIVE: existing saves do not matter.**

---

## 0. What the critique changed

The single most important finding, which inverts v1's framing:

> **The skill system in its current state cannot host the mutations.**
> Four capabilities that every mutation relies on do not exist on the
> skill side. v1 assumed "copy behaviour verbatim"; that is impossible
> until the substrate is upgraded. **M0 is now the first milestone and
> is a prerequisite, not a nicety.**

Corrected false premises from v1 §1:

| v1 claim | Truth (verified first-hand) |
|---|---|
| F4: "No concrete mutation implements `IRankedMutation`" | **False.** `UnstableGenomeMutation : BaseMutation, IRankedMutation` (line 8). |
| F3: "Only 8 read `Level`; the reachable ones are flat — zero balance drift" | **False where it matters most.** The two powers on the Player blueprint are *both* level-scaled: FlamingHands deals `Level × 1d4` (line 110), Calm's duration is `BaseDuration + Level×10` (line 67). |
| F5: "MP granted +1/level" | **Wrong direction.** `Entity.GainMP` returns false unless the entity has an `MP` stat (line 139-149); the Player blueprint defines `SP` but **no `MP`**. MP is never granted at all. So "+2 SP to keep progression constant" would be a **buff**, not a wash. |
| F1/F2: "43 concrete classes, 14 unreachable" | **42 concrete** (`IRankedMutation.cs` is an interface — v1 even listed it as an unreachable *mutation*, which was sloppy). And `GameBootstrap.GrantShowcaseSpellMutations` (line 1033) grants 6 of the "unreachable" set under DevMode. |
| F11: "Migration fixes targeting by construction" | **Overstated.** `SkillLine` carries its own confirmed corpse/ground-loot targeting defect, and the AoE creature-only defect lives in `SpellTargeting`, which skills already call. The dividend is *partial*. |

---

## 1. Corrected facts

- **42 concrete mutation classes.** 29 reachable in normal play (27 grimoire-taught, 2 on the Player blueprint). 13 unreachable, **6 of which DevMode grants** (`FireBolt`, `IceShard`, `PoisonSpit`, `PrismaticBeam`, `FrostNova`, `ChainLightning`).
- **Only 6 of the 11 Rites extend `ConsumingRiteBase`** (ShatteredRime, StillHeart, VerdigrisBloom, HollowCoin, SunderingWord, BloodletterLedger). The other 5 — StormAnvil, HangingBolt, RenderedSteam, ScaldingVeil, Fulmination — extend `BaseMutation` directly and hand-roll their own ink handling, so they do **not** inherit the base's two invariants.
- **`MutationDamageHelpers` is not mutation debris** — it is the canonical spell-damage pipeline (resistance tagging + Spellcraft modifiers) that `BaseSkillPart`, `SkillEventDispatcher` and `SpellcraftSkill` all point at. It **moves and is renamed; it is not folded away**.
- **Level-scaling is a real design question**, not a non-issue — because it affects exactly the two starting powers.
- AI needs no changes (verified: `AIHelpers` fires `GameEvent.New(ability.Command)`; `SkillsPart.HandleEvent` routes any `Command*` event). No NPC blueprint grants mutations.

---

## 2. M0 — Skill-system substrate (PREREQUISITE)

Six defects/gaps, each with a RED test. **No power may be ported before
this lands.**

| # | Gap | Evidence | Fix |
|---|---|---|---|
| **S1** | **`OnCommand` returns `void`; cooldown applied unconditionally after it.** Every mutation gates cooldown + turn consumption + the "fails to resolve" message on a bool. A verbatim port turns every free mis-cast (no ink, no target, nothing to cleanse) into a full turn + full cooldown. The docstring at `SkillsPart.cs:280-283` claiming OnCommand can zero the cooldown is **false** — line 284 overwrites it. | `BaseSkillPart.cs:294`; `SkillsPart.cs:278-287` | `OnCommand` → `bool`; apply cooldown only on success; propagate to `e.Handled`. Delete the false docstring. |
| **S2** | **`SkillEventContext` has no `TargetCell`, `SourceCell` or `Range`.** `SkillsPart.HandleEvent` lifts only Zone/RNG/dx/dy. 3 mutations read `TargetCell`, 14 files read `SourceCell`, ConjureWater reads `Range`. Every AdjacentCell and SelfCentered power is unportable without this. | `SkillEventContext.cs:23-90`; `SkillsPart.cs:402-405` | Add the three fields; lift them; thread through `TryRouteSkillCommand`. |
| **S3** | **No `BlocksTurnAdvance` channel.** 15 mutation call sites write it to hold the turn until ASCII FX finish. No skill has ever needed it (zero skills call `AsciiFxBus`). Ported powers would let monsters act mid-animation. | `DirectionalProjectileMutationBase.cs:65` +14; consumed `InputHandler.cs:3151,3164-3170` | Add `ctx.BlocksTurnAdvance`; write back onto the event after `OnCommand`. |
| **S4** | **`SkillsPart.AddSkill` hard-codes `sourceMutationClass: ""`.** `GrimoireTooltipData` + `ActivatedAbility.SourceMutationClass` drive the hotbar display name, colour, mechanics line, ability tooltips, **and the grimoire slot-picker filter**. Every ported power would become colourless, description-less, and **invisible in the picker** — unrebindable except via the M-key manager. | `SkillsPart.cs:134`; `InventoryUI.cs:3443,3300,3334,3655`; `HotbarStateBuilder.cs:24`; `GrimoireTooltipData.cs:45-56` | Rename `SourceMutationClass` → `SourcePowerClass`; add to `ActivatedAbilitySpec`; populate from `skill.GetType().Name`; move `GrimoireTooltipData` to Skills/ re-keyed on skill class names. |
| **S5** | **`AddAbility` has no duplicate-`Command` guard**, and dispatch is keyed on the command string with `FireEvent` short-circuiting on first `Handled`. Two powers sharing a command = two hotbar slots, one cooldown → **infinite-cast exploit**. | `ActivatedAbilitiesPart.cs:46-67`; `Entity.cs:255-265` | Reject/log duplicate Commands; RED test that two Parts cannot register the same Command. |
| **S6** | **Negative `Cost` is an SP-printing exploit.** Gate is `SpBefore < cost`; commit is `spStat.BaseValue -= cost`. `SkillData.Cost` defaults to **-999** — a row omitting Cost grants +999 SP. | `BuySkillAction.cs:134-139,169-171`; `SkillData.cs:62` | Add explicit `TeachOnly` flag + `NotPurchasable` failure reason; reject `cost < 0` outright. **Drop v1's `Cost: -1` sentinel idea entirely.** |

---

## 3. Design decisions (revised)

### D1 — Per-power **atomic swap**, not additive-then-delete
v1's "M1 is purely additive" is unsafe (S5). The critics proposed
distinct temporary command names + a rename pass in M4; **I diverge**:
each commit ports one power *and* deletes its mutation *and* ports its
tests. No coexistence window, no duplicate commands, no rename churn,
still independently revertable. Saves don't matter, so nothing requires
coexistence.

### D2 — Grimoires teach skills (unchanged, but scope corrected)
`GrimoirePart.DoRead`'s mutation branch (`GrimoirePart.cs:54-86`) maps
one-for-one onto `SkillsPart.HasSkill`/`AddSkill`. **Additional consumer
found by the critique:** `ConversationActions` clones
`GrimoirePart.MutationClassName`/`MutationLevel` onto newly-created
grimoires — dialogue-granted spells break silently if the field is
renamed without updating it.

Teach-only nodes use an explicit `TeachOnly` flag (S6), never a negative
cost.

### D3 — Tree assignments (revised)
Elemental powers as in v1, **except**: Conjure Rain, Drying Breeze and
Hearthwarm are non-combat utility and were arbitrarily filed under
combat trees, skewing tree sizes to 15-vs-4. **Recommendation: a
`Husbandry` (or `Weatherwork`) tree** for the three, keeping the
elemental trees combat-coherent.

**Rites split into two milestones** (M2a/M2b) because only 6 share a base.

### D4 — MP: delete outright, no SP compensation
MP is never granted (no `MP` stat exists on any blueprint), so removing
it changes nothing. **v1's "+2 SP/level to keep progression constant" is
withdrawn — it would be a straight buff.** Note MP *is* rendered on the
persistent sidebar and character sheet, so removal is a small UI sweep.

### D5 — Level-scaling: decision required
FlamingHands and Calm are level-scaled and are the two starting powers.
Options: (a) freeze at level-1 numbers (nerf: FlamingHands 1d4 instead
of Level×1d4); (b) re-express as flat values tuned to mid-level
equivalence; (c) add tiered tree rows. **Recommendation: (b)** — pick
flat numbers matching roughly level-3 output, documented as a
deliberate rebalance. **Flagged for the user.**

### D6 — What is deleted without replacement
Morphotypes (Esper/Chimera), genome instability (Unstable/Irritable),
ExtraArm, Regeneration, Telepathy. All unreachable outside DevMode; all
roguelike framing in an RPG. `GrantShowcaseSpellMutations` is deleted
with them (or repointed at the ported skills — trivial either way).

---

## 4. Milestones

- **M0** — Substrate (S1-S6). RED tests first. *Blocking.*
- **M1** — 18 elemental/utility powers, atomic swap per power. Port
  `DirectionalProjectileMutationBase` → a skill base **first**.
  ⚠ Its trace hits **one** entity (`trace.HitEntity`); `SkillLine.Collect`
  returns **all**. Porting must preserve single-target semantics — do
  **not** naively "route through SkillLine".
- **M2a** — `ConsumingRiteBase` + its 6 true subclasses.
- **M2b** — the 5 hand-rolled ink rites, with a divergence table against
  the base's two invariants; state explicitly whether convergence is in
  scope (it changes behaviour) or deferred.
- **M3** — Repoint grimoires (27 blueprints + `ConversationActions`);
  Player start → `StartingSpellKit`.
- **M4** — Delete the residue: `MutationsPart`, registry, definitions,
  trackers, `Mutations.json`, save hooks, MP stat + sidebar/sheet display,
  `GetMutationListSummary` (zero call sites — safe),
  `GrantShowcaseSpellMutations`. **`MutationDamageHelpers` moves, not deleted.**
- **M5** — Sweep + adversarial + live PlayMode verification of a
  grimoire-taught power.

### Consumers the v1 sweep missed (now explicit)
`FarmingAccessGrant.HasFarmingAccess` gates the farming loop on
`HasMutation("ConjureRainMutation")` — a hard-coded string that breaks
compile at M4 and changes *who receives the farming kit* if repointed
naively. Plus `ConversationActions`, `ActivatedAbility.SourceMutationClass`,
`GrimoireTooltipData`, the MP sidebar/sheet, `PlayerVerifier.HasMutation`
and `ScenarioTestHarness`.

---

## 4b. Inventory-sweep additions (v2.1)

The 4-agent port-dossier sweep landed after v2 was written
(`Docs/MUTATIONS-PORT-DOSSIERS.md`, 79 dossiers). It confirmed the
critics and added these:

**Changes a decision:**

- **S6 has a better answer than a new flag.** Teach-only nodes are
  achievable **with zero code changes**: author the row with
  `"Requires": "<sentinel that matches no class>"` + `"Flags": 2`
  (FLAG_OBFUSCATED). It renders `???` in dark gray, is unbuyable
  (MissingPrereq), and correctly flips to the real name + "owned" once a
  grimoire teaches it, because `EvaluateRowState` checks `isOwned`
  first. **Adopt this instead of the `TeachOnly` flag.** Still add the
  `cost < 0` guard — the SP faucet is real regardless.

**New blockers:**

- **B7 — M4 is all-or-nothing and mid-migration greenness is
  unobservable.** `EditModeTests.asmdef` is the *only* test assembly. A
  single dangling `MutationsPart` reference yields **zero test results,
  not 26 failures**. All 26 breaking test files must be fixed in the
  same commit as the deletion.
- **B8 — `MutationSystemTests.cs` is 24% not-a-mutation-test.** Lines
  55–427 are 22 `ActivatedAbilitiesPart` tests (slot binding, cooldown
  ticking, legacy migration, overflow). `ActivatedAbilitiesPart`
  **survives**. Deleting the file wholesale silently drops live hotbar
  infrastructure coverage — split it first.
- **B9 — 6 rites are dead from the keybind, not 4, and 2,487 lines of
  green rite tests never noticed** because every test calls
  `rite.Cast(zone, dx, dy)` directly, bypassing `HandleEvent`. Zero
  `FireEvent`/`GameEvent.New` calls exist in the rite tests. **The port
  must add end-to-end event-dispatch tests** or the same bug class
  recurs behind the same blind spot.

**Pre-existing bugs surfaced (fix during the port, or file separately):**

- **6 of the 27 grimoire powers are ALREADY invisible in the grimoire
  picker** — `GrimoireTooltipData` has 21 records for 27 grimoires
  (missing: ChillDraft, ConjureWater, DryingBreeze, Hearthwarm,
  KindleFlame, WardGleam). S4's re-keying should close this.
- **Grimoires cannot be re-inked.** `GrimoireChargePart.Refill()` and
  `ChargesPerVial` have **zero production callers** — every rite book is
  a hard 10-cast consumable, contradicting its own docstring. *This
  answers backlog task #72 (grimoire ink renewability): today the answer
  is "not renewable, and not by design."*
- Rites are **element-blind about which book they drain** — casting
  Shattered Rime can drain the Storm Anvil book (pinned as known).
- `SkillsScreenUI.FormatFailure` prints `CostPaid` on the
  InsufficientSP path, which is only set on success → "You need 0sp".
- Tooltip cooldowns for Rendered Steam / Scalding Veil are **swapped**
  vs the code.

**Scope/UX warnings:**

- **Skills-screen UX cliff.** The screen already renders 79 rows
  (12 trees + 67 powers) into a **14-row viewport with single-row-step
  navigation only** — no paging, no collapse. +29 powers and a Rites
  tree takes it to ~108 rows ≈ 8 screenfuls. **Budget paging/collapse
  work into M5 or accept a real regression.**
- **Balance is the larger risk than mechanics.** Mutations were tuned in
  isolation and are systematically cheaper than their skill
  counterparts: EmberVein CD 12 vs RailSpike 45 / FlameJet 35;
  Conflagration CD 15 vs FlameJet 35; RimeNova CD 15 vs ColdSnap 30
  (and RimeNova delivers a ~24-turn lockout vs a 6-turn slow);
  Thunderclap CD 18 vs Overload. Porting numbers verbatim imports a
  power spike.
- **`ConsumingRiteBase` needs ~5 new hooks** before all 11 rites fit —
  chiefly multi-attribute damage (`DamageAttribute` is a single string
  but StormAnvil/HangingBolt/Fulmination need Electric+Lightning).
- **Do NOT global-rename `Mutation`.** The diag kind
  `"PreDamageMutation"` (`CombatSystem.cs:898`) is unrelated and is a
  live contract; ~31 further files use "mutation" as ordinary prose.
- **Opportunity:** `ActivatedAbility.Class` has exactly one reader (the
  ability-manager sort column). Setting `ActivatedAbilitySpec.Class` to
  the **tree name** ("Pyromancy", "Rites") instead of the flat "Skills"
  makes that screen group meaningfully for free.

## 5. Open questions for the user

1. **Level-scaling (D5)** — freeze, rebalance flat, or tiered rows?
2. **Utility tree (D3)** — new `Husbandry`/`Weatherwork` tree, or keep the
   three utility powers in elemental trees?
3. **Rite ink convergence (M2b)** — converge the 5 hand-rolled rites onto
   the base's invariants (behaviour change), or preserve as-is?
4. **Teach-only** — are grimoire powers also SP-buyable, or found-only?

## 6. Honesty bounds

- §1 is corrected against 4 critics + my own re-verification of every
  blocker. The 30 majors are folded in but not each individually
  re-verified by me.
- Tree assignments and SP costs remain design judgment.
- The 6-agent inventory fleet is still running; its per-power dossiers
  will add implementation detail but are not expected to change this
  plan's shape.

## 7. [WAIVED] Save compatibility
Retained: `SavePart` writes an unlength-prefixed body; `LoadPart` returns
null on unresolvable types without consuming bytes, desyncing the stream.
Waived by user directive. Underlying defect filed in
`SYSTEMS-AUDIT-2026-08.md` §6 P1.

## 8. v1's false premises — the record
F4 (IRankedMutation), F3 (flat mutations), F5 (MP granted), F1/F2
(43 classes / no DevMode path), F11 (correctness dividend by
construction). All asserted as verified in v1; all wrong. The pattern:
**I grepped for a symbol and inferred meaning from the match count
without reading each match.** `grep -l IRankedMutation` returned 3 files
and I concluded "no implementers" without opening them.
