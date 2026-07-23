# Combat Audit Bug-Fix Plan — 2026-07

> Living plan implementing the specific findings from `COMBAT-SYSTEM-AUDIT-2026-07.md`
> the user named for autonomous fixing: ShatterArmorEffect/GetPartAV,
> Dismemberment double-fire, Cudgel_Slam/GroundPound pipeline bypass,
> CharredEffect/HibernatingEffect/FungalInfectionEffect state corruption,
> the two stun-save regressions (StunnedEffect.OnStack sentinel, OnHitEffectFactory
> missing SaveTarget wiring), all 8 confirmed observability gaps, Qud-parity
> drift in off-hand attacks and on-hit timing, and 4 performance findings
> (Body.GetParts allocation, GetDV/GetAV closures, WeaponSlot struct
> conversion, gas-diag missing IsChannelEnabled guard).

**Status:** 📋 IN PROGRESS.
**Origin:** user directive, 2026-07-22/23 — "make a plan to fix these bugs and
implement the bug fix plan without my intervention," followed by the user's
own pasted summary of the audit's top findings, defining this plan's exact
scope. Standing authorization: implement autonomously, no check-ins required
for anything within this named scope.
**Verification method:** 4 parallel read-only research agents re-verified
every cited finding against current HEAD (`6216cbe9`) before this plan was
written — the audit doc's own appendix warns its line numbers were already
stale when written, and more commits landed since (`Docs/THROWN-MUTATION-COMBAT-PLAN.md`'s
SM5b added a retaliation hook inside `CombatSystem.ApplyDamage`).

---

## 0. Pre-implementation verification sweep — corrections table

| # | Audit claim | Verification result |
|---|---|---|
| 1 | `CombatSystem.cs` is 1334 lines (audit's own scope-pass count) | **Now 1350 lines.** The +16 comes entirely from commit `713ee5a8` (thrown/mutation retaliation hook), inserted inside `ApplyDamage` between the resistance step and `DamageDealt`. Every citation the audit made for code **after** that insertion point (~old line 750 onward — `HandleDeath`, `CheckCombatDismemberment`, `GetPartAV`) drifted by exactly +16 lines. Citations before that point (the on-hit dispatch block, `GetAV`, `GetDV`, `OnHitClassEffects`/`OnHitWeaponEffects`/`StatusEffectsPart` gates, the off-hand mechanic) are still byte-for-byte accurate. This plan cites current, re-verified line numbers throughout, not the audit's originals. |
| 2 | §5's gas-diag guard finding names 5 specific call sites (`PoisonedByGasEffect.cs` ×2, `OnHitGasEmit.cs`, `FungalInfectionEffect.cs` ×2) | **Confirmed true, but the gap is bigger than audited.** A full grep of every `Diag.Record("gas", ...)` call site in the repo (`GasStunPart.cs`, `IObjectGasBehaviorPart.cs` ×3, `GasCryoPart.cs`, `BurnOffGasPart.cs` ×2, `GasPlasmaPart.cs`, `GasMaskPart.cs` ×2, `GasSleepPart.cs`, `GasImmunityPart.cs`, `GasSystem.cs` ×4, `GasGrenadePart.cs`, `GasFactory.cs` ×5, `GasFungalSporesPart.cs` ×2, `GasPoisonPart.cs`, `GasConfusionPart.cs`) shows **zero** `IsChannelEnabled("gas")` guards anywhere in the entire `"gas"` category, not just the 5 audited sites. **Scope decision:** fix the whole category, not just the 5 — partial consistency (5 fixed, N left) would be worse than the status quo per this project's own Q2 cross-feature-consistency principle, and the fix is mechanical/uniform either way. |
| 3 | §2 implies `HibernatingEffect` is a clean "already correctly fixed" reference example (cited only for Finding 3, the CharredEffect save/load bug) | **Nuance the audit doesn't flag:** `HibernatingEffect` is simultaneously (a) the **correct** reference example for Finding 3's bug class (public fields survive save/load, with an explicit `SL.6.4` code comment citing this exact failure mode) **and** (b) itself **still broken** by Finding 4's separate bug class (captures `GetStatValue`'s composite `BaseValue+Bonus-Penalty+Boost` instead of raw `BaseValue`). A fix for one must not be assumed to cover the other — SM1 and SM2 below are correctly scoped as separate sub-milestones for exactly this reason. |
| 4 | §2's Dismemberment finding frames the double-`HandleDeath` failure as a hypothetical ("a Pale-Salt weapon kills an Undead enemy...") | **Confirmed as a concretely live, shippable trigger, not just a hypothetical.** `EnhancementTagBonusBase.OnAttackerHit` (`Assets/Scripts/Gameplay/Items/EnhancementTagBonusBase.cs:83`) calls `CombatSystem.ApplyDamage(defender, BonusDamage, attacker, zone)` synchronously during the on-hit dispatch block — this is the real, currently-equippable implementation backing **both** `EnhancementPaleSalt` (Undead-tag bonus) and `EnhancementChoirIron` (Fungal-tag bonus). Raises this finding's practical severity; no design change to the fix, just confirms it's worth prioritizing. |
| 5 | §3's on-hit-dispatch-requires-survival finding doesn't specify exactly how early weapon-mod effects should move | **Clarified via direct Qud citation re-read:** Qud's `WeaponHit` "fires before `Penetrations` is even finalized" (`Combat.cs:1175-1186`) — i.e., strictly before the penetration roll, not merely before the *damage-amount* early-return. The fix (SM7) moves class/weapon/gas/enhancement dispatch to fire immediately once the to-hit roll connects, independent of penetration count or final damage amount. This is a genuine, gameplay-visible behavior change (procs can now fire on a fully-blocked or lethal hit) — flagged explicitly in SM7's own section below, not silently absorbed. |

No corrections invalidate any item in-scope; all 21 findings are confirmed live in current code. Proceeding per the user's standing authorization.

---

## 1. Scope — 21 items across 6 groups

Directly matches the user's own pasted summary, cross-referenced to the audit doc's section numbers:

**Group A — Effects state corruption (audit §2, 4 items):**
- A1. `CharredEffect` private-field save/load bug (🔴, `CharredEffect.cs:14-15,34-42`)
- A2. `HibernatingEffect`/`FungalInfectionEffect` `BaseValue` corruption (🔴, two files, same bug pattern)
- A3. `ShatterArmorEffect`/`GetPartAV` — Shatter has zero effect on ~all real combat (🔴, `CombatSystem.cs:263,602-639,1275-1292`)
- A4. Dismemberment can re-fire `HandleDeath` on an already-dead target (🔴, `CombatSystem.cs:426-485,1062-1143,1306-1337`; `Body.cs:267-271`)

**Group B — On-hit dispatch / Qud parity (audit §3/§6, 3 items):**
- B1. Every on-hit dispatcher gated on `hpAfter > 0`, unlike Qud's `WeaponHit` timing (🔴, `CombatSystem.cs:314-318,360-364,426-485`)
- B2. `Cudgel_Slam`/`Cudgel_GroundPound` bypass the entire on-hit pipeline (🔴, `Cudgel_Slam.cs:160`, `Cudgel_GroundPound.cs:118`)
- B3. Off-hand attacks are a different (always-swing + flat penalty) mechanic from Qud's (~15%-chance-to-swing) with an overclaiming comment (🟡, `CombatSystem.cs:23-45,169-170`)

**Group C — Stun-save regressions (audit §8, 2 items):**
- C1. `StunnedEffect.OnStack` treats `SaveTarget=0` as the numerically weakest DC, not its own sentinel (🔴, `StunnedEffect.cs:80-81`)
- C2. `OnHitEffectFactory`'s `"stunned"` case never wires `Magnitude → SaveTarget` (🔴, `OnHitEffectFactory.cs:68-71`)

**Group D — Observability (audit §7, 8 gates, 1 sub-milestone):**
- D1-D8: `BeforeMeleeAttack` veto, `BeforeTakeDamage` veto/full-resist, `ApplyDamage`'s 2 guards, `HandleDeath`'s kill lifecycle, `CheckCombatDismemberment`'s 4 outcomes, `OnHitClassEffects`'s 3 rolls, `OnHitWeaponEffects`'s roll+unknown-name drop, `StatusEffectsPart`'s `OnStack`+2 pre-apply gates.

**Group E — Performance (audit §5, 4 items):**
- E1. `Body.GetParts()` allocates fresh + full tree-walk twice per attack (🔴)
- E2. `GetDV`/`GetAV` allocate a closure+delegate per attack (🟡)
- E3. `WeaponSlot` is a class — one heap alloc per weapon per attack (🔵)
- E4. 5+ gas-category diag sites (scope-expanded to the whole category, per §0 correction #2) missing `IsChannelEnabled` (🟡)

---

## 2. Sub-milestones (smallest blast radius first)

Ordering rationale: isolated single-effect-class fixes first (A1, A2, C1, C2 — each touches one or two self-contained files with no cross-dependency), then the two AV/armor-adjacent fixes (A3, A4 — both touch `CombatSystem.cs` but are independent of each other and of the bigger dispatch restructure), then the highest-risk structural change (B1, since B2 depends on its corrected timing), then B2, then the explicit balance port (B3), then the uniform additive work (D, E — lowest behavioral risk, done last so they instrument/optimize the already-corrected pipeline rather than a soon-to-change one).

1. **SM1 — CharredEffect save/load fix (A1).** Make `_originalCombustibility`/`_hasStoredOriginal` public fields (mirrors `HibernatingEffect`'s own `SL.6.4`-commented precedent). Test: apply CharredEffect, round-trip through `SaveGraphSerializer`, remove the effect, assert `Combustibility` is restored to its original value (impossible to assert correctly today — this is a genuine RED test, not just a re-pin).

2. **SM2 — HibernatingEffect + FungalInfectionEffect BaseValue fix (A2).** Change both effects' capture/restore from `GetStatValue(...)` (composite `Base+Bonus-Penalty+Boost`) to `stat.BaseValue` directly (mirrors `CoatedInPlasmaEffect`'s already-correct pattern in the same folder). Test: apply a nonzero `Bonus` to the buffed stat before applying the effect, confirm `OnRemove` restores `BaseValue` to its pre-apply value without baking in the `Bonus` — the exact scenario the audit found zero existing test fixtures exercise.

3. **SM3 — StunnedEffect.OnStack sentinel fix (C1).** Treat `SaveTarget == 0` as its own "no save" sentinel, not the numerically weakest DC: `if (SaveTarget == 0 || stun.SaveTarget == 0) SaveTarget = 0; else SaveTarget = Math.Max(SaveTarget, stun.SaveTarget);`. Test: stack a `saveTarget=0` stun (e.g. mirroring `Cudgel_Slam`'s construction) with a `saveTarget=16` stun (mirroring `OnHitClassEffects.TryApplyStunned`'s Bludgeoning proc) in both orders, assert the merged `SaveTarget` stays `0` either way — the exact collision the audit traced through real Cudgel-class call sites.

4. **SM4 — OnHitEffectFactory stunned-case Magnitude wiring (C2).** Mirror the already-fixed `"bleeding"` case exactly: `saveTarget: spec.Magnitude > 0f ? (int)spec.Magnitude : 0`. Test mirrors `OnHitEffectFactory_BleedingSpec_UsesMagnitude_NotDurationAsSaveTarget` (`CombatContentAdversarialTests.cs:588-610`) for a `"Stunned,30,,2,16"` spec.

5. **SM5 — ShatterArmorEffect/GetPartAV fix (A3).** Factor the Shatter subtraction + non-negative clamp out of `GetAV` into a shared private helper both `GetAV` and `GetPartAV` call. Test: a `PerformSingleAttack`-level (not `GetAV`-direct) test — Body-having defender, active `ShatterArmorEffect`, confirm the very next attack's penetration roll is computed against the reduced AV. This is the first test in the whole codebase to exercise this interaction at the `PerformSingleAttack` level rather than calling `GetAV` directly.

6. **SM6 — Dismemberment double-fire fix (A4).** Add an alive-check guard at the top of `HandleDeath` (mirrors `ApplyDamage`'s existing already-dead guard) and/or at the top of `CheckCombatDismemberment`. Test: simulate the concrete live trigger — an on-hit item enhancement (`EnhancementTagBonusBase`-style bonus damage) kills the defender mid-dispatch, then a forced dismemberment roll fires; assert `HandleDeath`'s kill lifecycle (XP award, loot drop, `Died` event) runs exactly once, not twice.

7. **✅ SHIPPED 2026-07-23 (narrowed scope) — SM7 — On-hit-dispatch-requires-survival timing fix (B1).** The highest-risk item in this plan. **As implemented, narrower than the original wording above:** removed ONLY the `hpAfter > 0` (survivor) requirement wrapping `OnHitClassEffects`/`OnHitWeaponEffects`/`OnHitGasEmit`/`ItemEnhancementDispatch` — these 4 now fire on a killing blow too, matching Qud's `WeaponHit` timing not requiring survival. **Explicitly did NOT** move these dispatchers before the `penetrations == 0`/`damage.Amount <= 0` early-returns (the literal "fires before Penetrations is finalized" half). Reason found during implementation: `OnHitClassEffects`/`OnHitWeaponEffects` already self-gate on `actualDamage<=0` internally, so by the time execution reaches this point the method's own earlier early-returns already guarantee `actualDamage>0` — meaning the "fires on a fully-blocked 0-penetration hit" behavior is unreachable either way without ALSO restructuring the early-return points themselves, which would make `OnHitGasEmit` (which does NOT self-gate on damage, per §3's own separate finding) start firing on 0-damage hits — an expansion into the explicitly out-of-scope "OnHitGasEmit missing damage-gate" finding (§4). Narrowing to "remove only the survivor-gate" delivers the unambiguous, verifiably-safe half of the fix without that scope creep. Kept `AttackerAfterAttack`/`WeaponMadeCriticalHit` skill dispatch and `CheckCombatDismemberment` gated on `hpAfter > 0` survival, unchanged, matching the plan's own "keep skill-level dispatch and dismemberment gated on survival." Tests: a killing blow still dispatches `IItemEnhancement.OnAttackerHit` (RED before the fix); counter-check — skill dispatch (`AttackerAfterAttack`) stays gated on survival, confirmed already-passing (pin, not RED→GREEN). **Tests: 592/592 GREEN across every combat/skill/effect/thrown/mutation suite touched this session — no regression** from this core-method restructure.

8. **SM8 — Cudgel_Slam/Cudgel_GroundPound pipeline bypass fix (B2).** Route both through `CombatSystem.PerformSingleAttack` (mirroring `Axe_Whirlwind`'s per-target-in-a-loop AOE pattern for GroundPound) instead of the raw `ApplyDamage(Entity,int,Entity,Zone)` overload — inherits SM7's corrected timing for free. Requires a design decision already flagged by the audit (guaranteed-hit-no-procs vs. rolled-hit-with-procs) — resolved here as: route through the real pipeline (a to-hit roll now applies), matching every other active ability in every weapon class; document the behavior change in both skills' doc-comments (mirroring `Axe_Whirlwind.cs:161-164`'s existing pattern of stating its hits roll normal on-hit hooks). Tests: a Cudgel_Bludgeon-carrying, Lifesteal-wielding character's Slam/GroundPound now triggers the stun proc and Lifesteal heal; counter-check — a to-hit roll can now genuinely miss (previously guaranteed-hit).

9. **SM9 — Off-hand attack Qud-parity port (B3).** Replace the flat `-2`-to-hit-always-swings model with Qud's real mechanic: a per-turn chance (`RuleSettings.BASE_SECONDARY_ATTACK_CHANCE = 15`, scaling with dual-wield investment) that the off-hand attacks at all; if it does, no separate accuracy penalty. Update the overclaiming comment. Tests: statistical hit-rate check at baseline investment (~15%); counter-check — when the off-hand doesn't roll to attack, no weapon-list entry / no swing / no message for it that turn.

10. **SM10 — Observability: 8 diag gates (D1-D8).** One sub-milestone, 8 new `Diag.Record` call sites (all additive, matching each gate's nearest already-instrumented sibling's category/kind/payload style, per the verification research's per-gate style-reference citations). New kinds: `MeleeAttackVetoed` (D1), `BeforeTakeDamageVetoed`/`FullyResisted` (D2, two related kinds), `ApplyDamageRejected` with a `reason` field (D3), `DeathHandled` (D4), `Dismemberment` with an outcome field (D5), `ClassEffectRolled` ×3-in-one-kind or per-effect (D6), `WeaponEffectRolled` + `WeaponEffectUnknownName` (D7, two kinds — a probability-miss and a data-content-bug are materially different failure modes), `OnStackAbsorbed` + reject-gate kinds (D8). One test per gate confirming it fires with the expected payload shape on the relevant branch, plus a counter-check that it does NOT fire on the sibling (non-triggering) branch.

11. **SM11 — Performance: Body.GetParts() scratch-list fix (E1).** Add a `Body.GetParts(List<BodyPart> result)` pass-through overload (mirrors the existing `BodyPart.GetParts(result)` the wrapper never exposed), add static scratch lists at both call sites (`GatherMeleeWeapons`, `SelectHitLocation`), mirroring `_gatherWeaponsScratch`'s existing doc-comment caveats (turn-serial safety argument). Zero behavior change — pure allocation elimination. Tests: existing behavior-preserving tests must stay green; no new assertions needed beyond confirming the scratch list is actually reused (e.g., a call-count/identity check if feasible, or simply trusting the mechanical mirror of an already-proven pattern).

12. **SM12 — Performance: GetDV/GetAV closure fix (E2).** Replace the `body.ForeachEquippedObject` closure-capturing lambda in both methods with a plain for-loop over a new `Body.GetEquippedParts(List<BodyPart> result)` pass-through (exposing the currently-dead-code `BodyPart.GetEquippedParts` that already supports a result-list param). Zero behavior change.

13. **SM13 — Performance: WeaponSlot struct conversion (E3).** Convert `WeaponSlot` from `class` to `struct` in `CombatSystem.cs`. Requires auditing every consumer of the returned `List<WeaponSlot>` for by-reference-mutation assumptions first (the verification research flagged one in-place mutation site, `result[0].IsPrimary = true` via list-indexer — safe for structs too, but must be re-confirmed against the current call graph before converting).

14. **SM14 — Performance/observability: gas-category IsChannelEnabled guard, codebase-wide (E4).** Per §0 correction #2, wrap every currently-unguarded `Diag.Record("gas", ...)` call site in `if (Diag.IsChannelEnabled("gas"))` — not just the 5 originally audited, the entire category. Mechanical, zero behavior change (channel defaults on either way; this only elides the payload allocation when a future change turns the category off).

15. **SM15 — Final adversarial sweep + living doc close-out.** Per CLAUDE.md's mandatory gate (this feature touches state atomicity — A4's death-lifecycle idempotency; cross-actor flows — the dispatch-timing change affects attacker/defender/weapon-enhancement interactions; probabilistic/RNG-gated behavior — B3's chance-to-swing, C1/C3's stun-save merges; diag emission contracts — all of Group D). Dedicated adversarial test file, then final commit.

---

## 3. Adversarial sweep applicability (CLAUDE.md gate check)

This plan touches ≥2 taxonomy surfaces on nearly every sub-milestone — the dedicated sweep (SM15) runs once after all 14 fix sub-milestones land:
- **State atomicity / rollback:** SM6 (dismemberment double-fire), SM7 (on-hit timing restructure's interaction with dismemberment/skill-dispatch survival gating).
- **Cross-actor flows:** SM7/SM8 (attacker/defender/weapon-enhancement interactions at new timing), SM5 (Shatter stacking across multiple attacks).
- **Probabilistic/RNG-gated behavior:** SM3 (stun-save merge), SM9 (off-hand chance-to-swing boundaries at 0%/100%-equivalent investment).
- **Diag emission contracts:** all of SM10 (fires-exactly-once-per-gate pins), SM14 (guard doesn't change behavior, only allocation).
- **Save/load reach:** SM1 (CharredEffect round-trip), SM2 (Hibernating/FungalInfection round-trip with nonzero Bonus/Penalty).

---

## 4. Explicitly out of scope (not named in the user's authorization)

Per §7 of the audit and the user's own summary, these confirmed findings are **not** part of this plan and are left untouched:
- 🟡 `BurningEffect.OnStack` drops the re-igniter's identity (kill-credit misattribution)
- 🟡 `ConfusedEffect`'s non-stacking guard bypassable via `ForceApplyEffect`
- 🔵 `PaperSkinEffect`'s log double-count + zero test coverage
- 🔵 `FrozenEffect`/`AcidicEffect` mislabel natural-recovery as `duration_expired`
- 🔵 `HandleDeath`'s kill-XP self-kill guard
- 🟡 (plausible) `ApplyDamage`'s unclamped `Hitpoints.BaseValue`
- 🟡 `ChargingStrike`/`Backstab`'s bonus-damage bypass (quieter cousin of B2)
- 🟡 `OnHitGasEmit`'s missing damage-gate parameter (cousin of B1, narrower)
- 🔵 `ItemEnhancementDispatch`'s docstring naming the wrong predecessor
- 🔵 `Cudgel_Disarm`'s undelivered re-equip-delay doc claim
- 🟡 No per-weapon post-roll veto/mutate hook (Qud parity, explicitly latent/deferred by the audit itself)
- All §1 completeness-critic gaps except retaliation (already shipped via `THROWN-MUTATION-COMBAT-PLAN.md` SM5b) and the thrown/mutation accuracy gap (already shipped via that same plan) — AI threat model, follower-assist, save/load-of-combat-state-unchecked remain untouched.

If any of these turn out to interact with an in-scope fix during implementation, that interaction will be documented as a scope-divergence note in the relevant sub-milestone's commit, not silently expanded into.

---

## Appendix: exact current signatures (post-verification-sweep)

- `CombatSystem.GetAV(Entity entity)` — `CombatSystem.cs:602`
- `CombatSystem.GetPartAV(Entity entity, BodyPart hitPart)` — `CombatSystem.cs:1275`
- `CombatSystem.SelectHitLocation(Body body, Random rng)` — `CombatSystem.cs:1242`
- `CombatSystem.HandleDeath(Entity target, Entity killer, Zone zone)` — `CombatSystem.cs:1062`
- `CombatSystem.CheckCombatDismemberment(Entity defender, Body body, BodyPart hitPart, int damage, Zone zone, Random rng)` — `CombatSystem.cs:1306`
- `CombatSystem.PerformSingleAttack(Entity attacker, Entity defender, MeleeWeaponPart weapon, bool isPrimary, Zone zone, Random rng, string attackSourceDesc = null)` — `CombatSystem.cs:148`
- `Body.Dismember(BodyPart part, Zone zone = null)` — `Body.cs:207`
- `StunnedEffect(int duration = 2, int saveTarget = 0, System.Random rng = null)` — `StunnedEffect.cs:29`
- `OnHitEffectFactory.Create(OnHitEffectSpec spec, Entity source, Random rng)` — `OnHitEffectFactory.cs`
- `BodyPart.GetParts(List<BodyPart> result = null)` — `BodyPart.cs:356`
- `BodyPart.GetEquippedParts(List<BodyPart> result = null)` — `BodyPart.cs:604` (currently dead code, zero callers)
- `Body.ForeachEquippedObject(Action<Entity, BodyPart> action)` — `Body.cs:710`
