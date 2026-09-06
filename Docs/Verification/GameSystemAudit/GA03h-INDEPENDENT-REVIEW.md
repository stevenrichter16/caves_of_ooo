# GA03h / A11 — independent source and test review

Read-only review, 2026-09-06. No repository edits, Unity launches, or test execution by this reviewer. This report supplements `/tmp/codex-ga03h-semantics-review.md`; root owns execution and final results. Protected preexisting CombatSystem/InputHandler/spell changes were not classified as A11 changes.

## Verdict at this checkpoint

The minimum A11 implementation correctly threads the first committed killer, normalizes the existing HP base before death observers, keeps death committed despite surviving stat modifiers, and prevents Axe from adding a new bleed after its mortal cut completes death. The level-up change guards only HP refill and preserves other rewards. The actual post-permission callback RED has a correctly placed fix in current source.

Two bounded items remain before a source-clear verdict: the existing same-melee death gates still use numeric HP only, and the newly added PaleSalt pair needs explicit EnhancementFactory isolation. Both were sent to root. Root is already preparing the same-melee RED; this is A11 work within one combat call, not A12 scheduler work. No blanket suppression of the four killing-blow riders is recommended.

Execution history supplied by root, not independently executed here: corrected initial16 was7PASS/9FAIL, minimum plus neighbors102GREEN. The first dedicated runtime total56 was53PASS/3FAIL with zero compiler errors: one real callback bug and two false witness fixture assumptions. The earlier Random ambiguity was a compile-only correction, not a behavioral RED. Later source additions and native runs require root's final result; those results must not be inferred from this review.

## Concrete remaining items

### 1. Committed positive-HP participants can pass existing same-melee stop checks

`Assets/Scripts/Gameplay/Combat/CombatSystem.cs:157` and `:165`, in `PerformBodyPartAwareAttack`, check only computed HP for defender and attacker before each weapon. `HandleDeath` intentionally preserves Bonus/Boost/Min, so an entity can already be removed and marked dead while computed HP remains positive. A later weapon can consume offhand chance RNG and potentially swing at or as that dead entity. This violates the existing comments and stop semantics; it is not a request to redesign turn scheduling.

The same attacker-only omission exists at `CombatSystem.cs:465`, immediately after `ApplyDamage` in `PerformSingleAttack`. Its comment expressly suppresses narration and on-hit dispatch after retaliation kills the attacker. A positive modifier lets a committed attacker pass that gate too. Updating only the outer loop would stop the next weapon but still permit the current dead attacker's later narration and callbacks.

Minimal repair after RED: OR `IsDeathHandled` into those three existing death checks, preserving the numeric checks and their return/break positions. Do not add a defender-survival gate before class/weapon/gas/enhancement dispatch. A living attacker must still receive the intentionally shipped killing-blow rider behavior. Root's native trailing offhand99 allowance on the positive-bonus-dead case currently records the defect; remove that allowance from the corrected expectation rather than treating it as acceptable final behavior.

Strong counterchecks: full actual primary attack kills defender with/without positive Bonus; neither case consumes offhand chance RNG after commitment. Actual retaliation kills attacker with/without positive Bonus; no late primary narration/riders and no offhand attempt follow. A paired uncommitted living positive-Bonus actor/target must still roll and swing the second weapon. Capture observations and assert outside callbacks. Do not alter the already-landed primary damage merely because retaliation commits attacker death.

### 2. PaleSalt test depends on a borrowed registry

`GameAuditMortalDeathAdversarialTests.cs:130–144` calls `new PaleSaltTinkerModification().Apply` without isolating EnhancementFactory. The nested MortalDeath/EquipmentLifecycle/HotbarSave/NewGameSave fixtures do not snapshot this registry. `EnhancementFactoryTests.Setup` calls `ResetForTests`, which sets `_initialized=true` and leaves tests with a deliberately partial registry; `EnsureInitialized` is then a no-op. The new real-content pair can therefore pass fresh and fail after unrelated factory tests. Conversely, a cold invocation can populate the borrowed registry and leave that state behind.

Minimal fixture repair: save the contents of both readonly dictionaries (`_byClassName`, `_byDisplayName`) and the `_initialized` flag, explicitly register the actual `EnhancementPaleSalt` type, and restore contents plus flag in finally (or use an existing exact scope). This does not justify changing production initialization: the shim already calls EnsureInitialized under its documented suppression contract. Keep the real modifier rather than replacing it with a fake killing enhancement.

## Confirmed RED and fixture corrections

* `CanBeDismembered` can kill the victim and return true. The initial helper then fired BeforeDismember and cut the removed victim. Root reports the dedicated kill=true case reproduced this. Current helper now rechecks committed/numeric death after the accepted permission event and before its fired diagnostic/Body call. The live callback counter still proceeds. Correct local position; no generic event dispatcher mutation required.
* The two witness failures were fixture-only: Villager does not author Brain.Passive=true. Current source uses actual SummitSinger and asserts Passive on both killer and witness before cutting. Preserve the historical failure classification; do not describe two witness production fixes.
* Actual Player has no MP stat. The initial level-up tests were corrected to distinguish an explicitly MP-bearing fixture from the actual missing-MP player. Dedicated missing-MP controls preserve absence while checking other rewards. This is independent of the real self-XP HP-refill defect.
* `MortalPostCutIndependentDeathWinsBeforeOuterSourceCommits` now supplies source B from AfterDismember. Body's later source-A HandleDeath call is a no-op, and the recorded Died killer remains B with no A XP. This is the correct first-commit semantics; source threading must not overwrite an independently completed death.

## Production contract checks

`Body.Dismember(part, zone=null, source=null)` appends its argument instead of adding an ambiguous overload. Existing callers remain compatible. Its sole A11 behavioral change passes source into the existing mortal death call; GA03f detach-first equipment ownership/cleanup stays intact. Zone-null cuts retain their preexisting anatomy-only behavior. Direct Body calls on an already-dead entity retain their documented low-level anatomy behavior; ordinary combat producers are where this wave refuses corpse cutting.

`HandleDeath` commits `_DeathHandled`, then applies Math.Min to the existing HP base, before the first killed-by message, XP, drop, or Died observer. It preserves a negative base and all modifiers/max bounds, and does not synthesize a missing HP stat or DamageDealt event. The pure null-safe public predicate reports commitment, not numeric HP or completion of every callback. The new truth-table and first-message tests correctly distinguish those states and prove recursive source changes cannot steal credit.

The public predicate is also used at InputHandler's existing death activation point before WaitingForInput gates. The actual Update fixture checks activation despite committed positive computed HP; living and ordinary nonpositive-HP controls remain meaningful. This is controller activation evidence, not by itself native keyboard evidence.

`Axe_Dismember` rejects committed/numerically dead defenders before chance RNG and rechecks after Body returns, before bleeding. Its missing-stat helper deliberately does not equate absent HP with death. The new missing-HP direct dispatcher control pins that compatibility. Nonmortal surviving cuts still apply the original35/1d2 bleed and source; Axe_Decapitate still expands candidates rather than implementing a separate attack.

`LevelingSystem.CheckLevelUp` still adds2 max HP and awards XP/levels/MP/SP; only refilling the base is skipped for a committed entity. This fixes stock self-kill XP crossing115 while preserving the game's self-credit policy. Living level-ups still heal. No resurrection policy or custom callback exception handling was introduced.

## Full ordinary-melee RNG validity

The damage9/10 pair is a genuine PerformMeleeAttack route with an actually equipped Battleaxe, not direct helper-only evidence. Both actors' Agility16 and defender natural AV6 are explicitly staged and checked; attacker Strength16 gives no extra strength modifier. Hit d20=10 lands against DV6 without critical auto-penetration. The three d10 penetration rolls9,1,1 produce exactly one penetration with Battleaxe pen bonus3 against AV6. Two d6 damage rolls4+5 or5+5 then produce9 or10.

The selected head offset is computed from actual body target weights and separately prechecked. Cutting-class bleed rolls99 so it cannot obscure the limb result. At HP/Max20, the mortal threshold is10: the9 case stays alive with attached head and no credit, while10 leaves10 HP immediately before a successful mortal cut and credits the real attacker. Two-handed hand aliases are gathered once through FirstSlotForEquipped. Root reports both cases passed. This establishes the full caller-to-source forwarding at the exact threshold, not general RNG parity.

## Real enhancement path: proposal now present in source

The requested bounded enhancement control is now authored as `RealPaleSaltKillPrecedesCachedSurvivorAxeDispatch`:

* Real Battleaxe receives real `PaleSaltTinkerModification.Apply`, which produces tier2 `EnhancementPaleSalt` with flat BonusDamage4.
* Actual SkeletalSentry authors MaterialPart's Undead tag. The paired fixture removes only that material tag, not Entity.Tags. Its HP13, agility and natural armor are explicitly staged; this is controlled actual content rather than an unmodified encounter-stat claim.
* Primary one-penetration damage10 leaves3 HP. The real enhancement runs before the cached `hpAfter>0` skill block and its four-point damage event completes death. Axe must return before its own chance/candidate RNG, leaving the head attached. In the material-tag-removed control, no enhancement damage occurs and the forced Axe/Decapitate path cuts the head.
* The pair asserts actual DamageDealt event amounts `{10,4}` versus `{10}`, primary HP3, RNG single-call counts2 versus4, exact killer, one Died, cut distinction, no postmortem bleed, and zone removal. Those observations distinguish two deaths with different causes in the same flow; a final-HP-only assertion would not.

Two honesty details: four is the emitted DamageDealt amount, while13→3→0 means only three remaining HP are removed by the overkill; do not call it four actual HP lost. SkeletalSentry's actual XPValue is90, so a future credit assertion should use90 (or explicitly label a changed XP fixture), not the base victim fixture's10. The source applies the real modifier directly; it does not prove a paid native Tinker operation. The registry isolation item above still needs resolution.

## Qud comparison and bounded limits

The previously read local Qud Axe_Dismember/Decapitate chain carries Attacker into dismember/death. That supports source attribution, but this repair is CoO lifecycle consistency, not an exact Qud port. Qud's last-mortal-part/Decapitate checks and death UI dispatch differ from CoO's selected-Mortal rule and marker/HP polling; neither is expanded here. GA03f's deliberate detach-before-forced-cleanup order is also preserved.

The four class/weapon/gas/enhancement dispatchers intentionally run on killing blows by a living attacker. The Axe-specific and combat-helper checks must not be generalized into suppressing those callbacks. Existing delayed BleedingEffect source loss, arbitrary custom callback exceptions/raw resurrection mutations, null-target HandleDeath, unrelated skill survivor caching, and A12 BeginTakeAction scheduling are not silently repaired or certified by this wave. No claim of system-wide bug freedom, exact Qud death parity, final full-suite success, native art coverage, or performance improvement is made by this source review.

## Final source recheck and corrections — 2026-09-06

**Source-clear for the bounded A11 diff.** The two remaining items above are resolved in the reread source. This updates the checkpoint verdict, while leaving the historical findings and failed-test chronology intact. Final focused/native/full execution still belongs to root.

All three existing same-melee death checks now include `IsDeathHandled`: the defender and attacker loop gates at CombatSystem157/165, and the post-ApplyDamage attacker gate at465. The latter retains its existing `attacker != null` outer guard. Their positions and break/return behavior remain unchanged; the four killing-blow dispatchers are unchanged. The edited method therefore stops committed participants without suppressing a living attacker's intended killing-blow effects.

The eight participant cases are nonvacuous: four death rows (source/target, Bonus0/10), two living rows with offhand refusal, and two living rows that actually allow and perform the offhand hit. Real Dagger and ShortSword are command-equipped into two actual hands. Max200 avoids clamping the living positive-Bonus fixture into an apparent zero-damage result. DamageDealt counts prove one primary hit versus two actual hits; the RNG records distinguish the primary class chance, offhand admission chance, and secondary class chance. The death hooks deliberately commit through HandleDeath from real TakeDamage or DamageDealt event dispatch; these are controlled callback-lifecycle tests, not evidence that a particular authored retaliation skill was exercised. Their assertions are made after dispatch, not swallowed inside callbacks.

The PaleSalt pair now owns `MortalEnhancementScope`: it saves both readonly dictionary contents and the initialized flag, registers actual EnhancementPaleSalt, and restores the original dictionary objects' contents plus flag during disposal. This supports both a cold factory and a previously suppressed/partial test registry, without a production initialization change.

**Correction to this report's earlier native inference:** root inspected the first native24PASS report's actual positive-Bonus RNG calls, and there was **no trailing offhand call**. The earlier assertion that the native allowance “records the defect” was an unsupported inference from permissive driver code and is retracted. The runtime participant RED independently established the bug; native did not establish it. The current driver additionally asserts that neither committed participant can coexist with a recorded `trailing_offhand` call, while retaining the recorder's finite diagnostic fallback. This is a stronger future assertion, not retroactive evidence that the first native run failed.

Root-reported execution chronology: the corrected six participant RED cases were4PASS/2FAIL. The first attempted guarded-source replacement matched no text due to the retained attacker-null prefix, changed nothing, and its244-case242PASS/2FAIL result is correctly retained as unchanged-focused-RED. The final three guards and two successful-offhand controls are now on disk; final focused execution was running at this review. Initial native24PASS included proper teardown. This source recheck does not relabel those historical runs or claim a completed final gate.

## Root execution following final source review

Final focused246GREEN, including actual living second-weapon hits and explicit
PaleSalt registry isolation. Native24PASS/0unexpected after performance harness
addition; final full9484GREEN/0CS. Both75s healthy-input profiles are valid,
with source hashes and post-teardown private-save isolation verified. Historical
review checkpoints above remain intact; the unsupported first-native offhand
inference is explicitly retracted in the original review. See GA03h-REPORT.
