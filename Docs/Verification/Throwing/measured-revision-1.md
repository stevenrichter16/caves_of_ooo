# Measured Throw: first bounded balance revision

Status: **Retain +4 after corrected native verification and unchanged-world comparison.** This is revision 1 of the maximum 3 substantive tuning revisions in A3-D12. Correctness fixes and the one precision expansion were not balance revisions.

## Why the original +2 needs a revision

The selected 100-seed baseline passed its execution tests (`precision100-physical-green.xml`, 5/5), but Measured did not establish its promised armored, scarce-projectile objective benefit. These are matched 2 SP worlds using the actual commands, purchases, scheduler, enemies and finite supplies; they are not naturally earned progression or randomized population estimates.

| Existing W1 condition | Measured objective /100 | Root-only objective /100 | Puncture objective /100 | Measured / root survivors | Survivor HP, Measured / root |
|---|---:|---:|---:|---|---|
| Viper, AV0, two daggers | 94 | 85 | 90 | 100 / 100 | 43.27 / 40.90 |
| Scrabbler, AV0, two daggers | 61 | 55 | 92 | 100 / 100 | 42.19 / 40.36 |
| Viper, AV4, one dagger | 40 | 44 | 74 | 81 / 74 | 20.88 / 17.55 |
| Scrabbler, AV4, one dagger | 37 | 39 | 79 | 77 / 73 | 24.53 / 22.81 |

The initial, frozen AV8 single-dagger groups also showed no objective gain at 20 seeds: Scrabbler 0/20 for ordinary/root/Measured, Viper 1/20 for each. Extra survivors or higher survivor HP are secondary benefits, but cannot be relabeled successful extraction or credited to faster deaths. Survivor-only averages are selected populations and do not establish a causal HP improvement by themselves. The strongest observed objective advantage occurs in low armor, the opposite of the intended niche.

## One falsifiable hypothesis and one linked parameter

**Hypothesis:** a bonus of +4 to penetration **and its effective cap**, instead of +2, will make the full paid preparation action worthwhile against existing AV4/AV8 scarce-projectile cases, while immediate throws and intact melee alternatives retain ordinary urgency/opportunity advantages.

The two additions are one linked penetration parameter; changing only penetration while leaving its cap unchanged would not test the intended mechanic. No accuracy, damage-die, cooldown, action cost, range, equipment limit, source, target, armor, AI, world layout or policy changes are authorized in this revision. Preparation still costs its own action, the actual throw another, and the next incompatible action consumes it. The bonus remains exact intended-owner only; interception and ordinary/unprepared throws receive no benefit.

This candidate deliberately widens Audit 3's original bonus range of 1–3 to **4**, following the explicit project-manager decision after the 100-seed result. That is a scope change, not a claim that +4 was originally frozen. Risk: a larger bonus may strengthen low-armor outcomes or a cheap specialist dip too much. Existing low-armor and Puncture/root arms remain unchanged so the review can reject that result.

## Verification before deciding retention

1. Native RED first: exact four-point snapshot assertion; actual command diagnostics at finite caps 0/2 and legacy uncapped -1, paired prepared/unprepared; authored/runtime copy, unchanged cost and cooldown.
2. Implement one central bonus value and update authored/preview/runtime descriptions together. Keep all unrelated production behavior unchanged.
3. Native correctness sweep for Throwing, thrown combat, source intent and input after the change.
4. Replay all frozen W1 conditions at 0/2/4/6 SP and 20 seeds, plus the same selected 100-seed groups. Separate `-measured-r1` receipts preserve the +2 baseline. No new arena or favorable seed selection.
5. Compare objectives, living exits, actual survivors' HP, finite supplies and action counts together. Declare retain/revise/cut explicitly against armored/scarce benefit plus an ordinary-condition reversal. Do not claim all precision intervals became narrow, or that controlled outcomes prove naturally earned pacing.

The remaining 2 revision slots are a hard maximum, not a requirement to spend them. If this bounded change still fails to produce a useful role, prefer cutting/merging the node to manufacturing a favorable demonstration.

## Fixture import correction

The first new test source incorrectly referenced Newtonsoft, which the EditMode test assembly does not reference. It was corrected to the project's Unity `JsonUtility` API without changing assembly dependencies. A mistakenly started test job during that failed import executed the old 25-test compiled fixture and passed; `measured-tuning-stale-assembly-rejected.xml` is retained but **rejected as RED/GREEN evidence for this revision**. The revised fixture must report 32 tests and actual expected assertion failures before gameplay tuning proceeds.

## Implemented after semantic RED

`measured-tuning-red.xml` contains the required 32 cases: 27 passed and exactly 5 failed (the existing exact bonus assertion, three prepared cap cases and the description assertion). The three unprepared cap controls passed. Only after that result was `ThrowingRules.MeasuredPenetrationBonus` set to 4 and used by the launch snapshot, runtime rank description and native targeting description; the authored skill description was updated to match. The actual cooldown/cost and all other parameters remain unchanged.

The five original +2 W1 archives—budgets 0/2/4/6 and selected 100 seeds—are additionally frozen under `comparisons/baseline-measured-plus2/`, with decompressed hashes in `comparison-archive-manifest.json`. Original-baseline readers prefer those frozen files even if a later broad run executes the original method names under +4. Revision wrappers write `W1-{budget}-measured-r1` and `W1-2-precision100-measured-r1`; they do not overwrite the original evidence.

## First revision comparison rejected: explicit RNG was overwritten

The first revision execution passed its tests, but independent outcome review found 442 of 5,520 non-owner replay controls differed. A banked, ordinary unarmed-melee arm changed even though it did not own Measured. The newly added comparison-only stun witness indiscriminately replaced `StunnedEffect.Rng`, including the legitimate deterministic combat stream explicitly supplied by `OnHitClassEffects`. That altered later draw consumption; it is a harness confound, not evidence that Measured or game balance changed non-owners.

These first revision receipts are rejected for causal before/after attribution and retained separately. The witness must preserve known supplied world/brain streams and seed only fallback effect streams, with a paired control, before repeating the exact same revision matrix. This correction is not a second balance revision: no additional gameplay parameter changes. Retention was held pending the corrected evidence recorded below.

## Interrupted broad sweep

A subsequent affected-system sweep selected 1,451 cases but the Unity process exited before a terminal receipt. The observed partial progress included one failure in `A3InactiveThrowingPurchaseCannotChangeSameSeedMeleeAndFiniteHealing`: its countercheck did not observe actual finite healing under the overbroad stun witness. The assertion is retained unchanged. The partial run is **incomplete**, with no final XML and no all-green claim; the exit cause is not established by the missing receipt. Root coordinates the narrow preserving-stream RED/GREEN and the 28 affected comparison cases, plus independent correctness/native checks. Earlier 1,330-case and 72-case production receipts remain historical evidence, not a substitute claim that 1,451 revised cases passed.

## Independent review of the RNG correction

The proposed fixture-only correction was independently checked against `OnHitClassEffects.TryApplyStunned`, `StatusEffectsPart.CheckBeforeApply` and `ElectrifiedEffect.OnApply`. Combat passes its existing RNG into the stun constructor and the application event preserves the source entity. The correction preserves world/tonic streams, registered enemy-brain streams (including enemies added after the first witness), and an allied/dog source's current brain stream. Actual electrical contact supplies neither RNG nor source, so its fallback is deterministically seeded before saves. The five new paired cases assert exact reference identity for world/own-brain/later-other-brain/source-brain versus fallback; existing actual-lightning and non-lightning controls retain real finite consumption and effects.

No concrete blocker was found in that bounded review. This helper cannot infer arbitrary constructor provenance: an unregistered RNG is treated as fallback. Therefore it is a controlled-fixture seam with explicitly registered streams, not a general production RNG rewrite or proof that every future effect source is seeded. Final full-record replay checks remain required.

The correction's true native RED is now recorded in `explicit-stun-stream-red.xml`: five cases, four expected explicit-stream failures and one passing fallback control. The reviewed correction was applied only afterward. This run used Unity 6000.3.4f1 in batch/nographics mode because the regular editor bridge had not reconnected; it is actual Unity test execution, without new visual acceptance. The corrected GREEN and comparison receipts are recorded in the next section.

## Final corrected result and retention decision

`explicit-stun-and-skills-green.xml` passed **41/41** native Unity cases in 2.73 seconds. `explicit-stream-comparisons-green.xml` then passed **28/28** affected comparison cases in 121.32 seconds, including the unchanged inactive-purchase/actual-healing countercheck. These are actual Unity 6000.3.4f1 batch/nographics runs, not the discontinued runner or fresh rendered play. The interrupted 1,451-selected sweep remains incomplete. The earlier 1,330/1,330 broad and 72/72 camera receipts, the new focused cases and the earlier rendered 33/33 routes must be reported by their actual scopes rather than relabeled one final broad green run.

The corrected revision receipts contain 8,320 executed trial records, with the selected 100-seed runs including repeated first-20 seeds. All **5,520 full-matrix non-owner controls and 1,200 selected-100 non-owner controls match the preserved +2 records**, normalizing only the documented additive intermediate HP token. These sets overlap and are not independent sample totals. Original, rejected and corrected versions remain separately hashed and compressed.

**Decision: retain the single +4 revision for a bounded scarce-projectile survival/health role.** No second tuning revision, extra seeds or favorable new arena is used. The benefit is not a claim that Measured wins armored extraction objectives or beats an intact melee build.

| Matched 2 SP condition, 100 seeds | Measured +4 | Root-only ordinary | Interpretation |
| --- | --- | --- | --- |
| Scrabbler, AV4, one dagger | 43 objectives; 84 alive; mean endpoint HP 26.21 with deaths counted as zero | 39 objectives; 73 alive; endpoint HP 16.65 | Scarce-projectile endpoint survival/health benefit; objective difference +4 points has interval [−9.5,17.3], still imprecise |
| Viper, AV4, one dagger | 46 objectives; 87 alive; endpoint HP 26.45 | 44 objectives; 74 alive; endpoint HP 12.99 | Same bounded benefit; objective difference +2 points has interval [−11.6,15.5], still imprecise |
| Scrabbler, AV0, two daggers | 65 objectives; successful objectives average 12.00 paid actions | Same-cost Puncture: 92 objectives; successful objectives average 7.98 actions | Ordinary low-armor/urgency alternative preference; paid preparation is not universally best |
| Viper, AV0, two daggers | 99 objectives; 100 alive | Root-only 85 objectives; 100 alive; Puncture 90 objectives | A favorable low-armor matchup is retained honestly rather than portrayed as an armored-only advantage |

The remaining-health calculation includes dead actors as zero; it does not improve merely by dropping deaths from a survivor average. As another descriptive check, the 69 Scrabbler seeds alive in both arms average +8.19 HP for Measured, and the 73 shared Viper survivors average +12.77 HP. Different actions consume different RNG draws, so these matched world-seed checks are not identical-luck causal estimates. Alive-only discordances are 15 Measured versus 4 root for Scrabbler and 14 versus 1 for Viper. Every living partial/timeout remains a partial/timeout, not extraction; the full rows retain them.

**Limits and no-dominance:** Puncture also wins the AV4 objective counts (79 versus 43 for Scrabbler; 74 versus 46 for Viper). Existing AV8 single-dagger conditions at 20 seeds show no objective improvement (Scrabbler zero for root/Measured; Viper one for each); that extreme is not solved by the revision. The same-cost Puncture reversal disproves universal superiority in the tested W1 conditions, but this is not an exhaustive claim about every build. Six of eight displayed +4 objective-difference intervals remain wider than 20 points. Survivor health and lower successful-objective action counts cannot be substituted for the primary objective.

The final recommendation satisfies the accepted TH1 requirement of an armored/scarce comparative **benefit** with an ordinary-condition reversal through observed survival/remaining health, while explicitly leaving the narrower armored-objective superiority claim unestablished. Naturally earned Measured/4–6 SP pacing, renewable supplies, actual mouse and physical Steam Deck acceptance remain unverified. The preview/runtime/authored text and penetration/cap correctness are tested; no new rendered +4 capture is claimed.
