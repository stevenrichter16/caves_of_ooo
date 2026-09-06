# GA03h / A11 — mortal dismemberment death lifecycle

Status: COMPLETE,2026-09-06.9417→9484GREEN (+67); focused246GREEN; native24PASS; two valid75s input profiles;0CS. Historical source preparation and log below are reconciled in Verification/GameSystemAudit/GA03h-REPORT.md.

Read-only preparation, 2026-09-06. No Assets/Docs changes, Unity calls, or tests run by this reviewer. Root owns RED execution and implementation. Current source includes GA03f equipment cleanup and protected, preexisting CombatSystem presentation changes; neither should be replaced wholesale. GA03f focused 456 and native 111 PASS are parent-reported, not independent execution here.

## Intended bounded invariant

When current CoO rules commit death through mortal limb loss, the victim has a committed dead state, the actual source receives existing death attribution, the player reaches the existing death modal, and the Axe passive does not add its ordinary surviving-victim bleed after that death has completed. The death lifecycle remains exactly once and ordinary nonmortal injury remains alive, attributed, and bleeding. Existing killing-blow weapon/class/gas/enhancement riders remain intact.

A12 (death inside BeginTakeAction and subsequent scheduler dispatch/input) is a separate queue item. This plan only observes existing HandleDeath deregistration; it does not repair or claim coverage of the scheduler loop.

## Verified source and corrections

| Source | Actual contract / correction |
|---|---|
| `Docs/GAME-SYSTEM-AUDIT-2026-09-05.md:162,237` | A11 records positive-HP mortal death, null killer, missing modal and postmortem Axe bleed. The protected killing-blow rider behavior is explicitly intentional. |
| `Assets/Scripts/Gameplay/Anatomy/Body.cs:221–288` | Public signature is `bool Dismember(BodyPart part, Zone zone = null)`. BeforeDismember has `Part`; after detachment/GA03f equipment cleanup, AfterDismember has `Part`, boxed `Mortal`, optionally `SeveredLimbEntity`. Finally an explicitly selected Mortal part with parent entity AND non-null zone calls `HandleDeath(ParentEntity, null, zone)`. No HP mutation or source exists. |
| `Assets/Scripts/Gameplay/Anatomy/AnatomyFactory.cs:59–70,198` | Current humanoid root Body is Mortal but cannot be explicitly severed; Head is a Mortal appendage. This is a real authored anatomy path. |
| `Assets/Scripts/Gameplay/Anatomy/BodyPart.cs:450–465` | IsSeverable excludes Abstract, non-Appendage, Integral, DependsOn and RequiresType. Mortal is NOT an exclusion. SeverRequiresDecapitate simply returns Mortal. Thus ordinary combat can sever Head without the Axe marker, subject to its higher threshold/chance. |
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:550–582,1588–1645` | Public helper is `CheckCombatDismemberment(Entity defender, Body body, BodyPart hitPart, int damage, Zone zone, Random rng)`. The actual caller possesses attacker, but neither helper nor Body receives it. Mortal doubles the damage threshold; chance caps at 50%. The helper has a live HP guard before rolling and the CanBeDismembered veto. |
| `Assets/Scripts/Gameplay/Skills/Axe_Dismember.cs:58–95` | 3% Axe hit passive after positive ActualDamage. With owned Axe_Decapitate it can choose Mortal. It calls Body.Dismember then applies Bleeding unconditionally. No live/death-marker guard at entry or after dismemberment. |
| `Assets/Resources/Content/Data/Skills/Axe.json:35–47` | Dismember and Decapitate are obtainable authored skill powers (Cost 1 each), not dormant API-only classes. Decapitate's current description is marker-only. Battleaxe and Hatchet blueprints carry Cutting Axe. |
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:506–548` | Four class/weapon/gas/item-enhancement hit dispatchers intentionally run on killing blows. Do not put a new broad survivor early return before them. `hpAfter` was captured before these calls; an enhancement can kill before the later skill dispatch inside the cached survivor branch. |
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1275–1405` | `_DeathHandled` private const tag is checked and then set BEFORE log, XP, drops, Died, witnesses and removal. HandleDeath never changes HP. It preserves NoDropOnDeath/Temporary drop suppression, emits Died while zone position still exists, then removes from Zone and TurnManager.Active. Preserve this exactly-once guard and order. |
| `Assets/Scripts/Presentation/Input/InputHandler.cs:304–343` | Death activation is actual HP polling, not a player Died subscription. It runs after the boot modal and before WaitingForInput/CurrentActor gates. Uses `PlayerEntity.GetStatValue("Hitpoints",1)`; requires player, current zone and turn manager refs to reach it. |
| `Assets/Scripts/Gameplay/Stats/Stat.cs:27–39` | Computed Value is BaseValue+Bonus−Penalty+Boost, clamped to Min/Max. Assigning Value only assigns BaseValue. Merely zeroing BaseValue does not universally make this UI HP predicate true. No ordinary positive HP modifier producer was established in the bounded effects/items scan; modifier/min cases are API adversarial coverage, not a second proven world-content exploit. |
| `Assets/Scripts/Presentation/UI/DeathScreenController.cs` | Existing modal owns L load and R restart. Successful L deactivates; no-save/load failure remains active. It does NOT emit the F6 `Game loaded.` prose. Existing tests' “player-Died listener” description is stale. |
| `Assets/Scripts/Presentation/Input/InputHandler.cs:1210–1242` | F8 debug dismember is DevMode-gated and deliberately selects the first nonmortal appendage. It cannot serve as the claimed ordinary mortal-death trigger. |

Ordinary reach: a real nonlethal body-aware melee hit can pass the Head threshold/chance, or an owned Axe_Dismember + Axe_Decapitate can proc against the current humanoid Head. NPC victims inherit real Body, GivesRep, Corpse and combat stats. Player death can result from regular combat dismemberment even without a specially authored NPC Axe skill loadout. Direct source-less Body calls and malformed/stat-modifier fixtures are separate API controls.

### Existing consumers of killer and death ordering

* `LevelingSystem.AwardKillXP(Entity killer, Entity victim, Zone zone)` (`Stats/LevelingSystem.cs:24–38`) reads victim XPValue, increments killer Experience, then checks level-up. HandleDeath only calls it for a Player-tagged killer. Keep initial XP below level threshold except in the dedicated level-up control.
* `AI/GivesRepPart.cs:24–49` consumes Died.Killer, changes only player-kill reputation, subtracts its Value from victim faction and awards half to factions hostile to it. Actual Creature inheritance supplies GivesRep Value 10; use actual faction setup and inspect preconditions.
* `Entities/CorpsePart.cs:128–203` receives typed object Zone/Killer, needs the still-resident death cell, and stores SourceID plus non-self KillerID/KillerBlueprint. `SuppressCorpseDrops` is independent of HandleDeath's equipment/loot policy. Factory and deterministic chance must be set for nonvacuous tests (Villager/Player CreatureCorpse chance 100; Snapjaw is 70).
* `Storylets/FinishObjectiveWhenSlain.cs:32–37` passes Died.Killer into `StoryletPart.Current?.FinishObjective(Quest,Objective,actor:killer)`. SetFactWhenSlain/AddFactWhenSlain observe death but are not the killer filter themselves. Do not claim every death objective inherently requires a player killer.
* `Effects/StatusEffectsPart.cs:410–413` removes all effects with CAUSE_OWNER_DIED. Axe's current bleed is applied after this cleanup has returned.
* `CombatSystem.BroadcastDeathWitnessed` at 1433–1465 applies WitnessedEffect(20) to nearby visible passive creatures, radius **8**. It skips the killer by identity. This is not a DeathWitnessed GameEvent and does not carry an effect source; test the actual effect and exclusion.
* Existing `DragPart` Died handling releases hauling; GA03e owns that lifecycle. A control can pin it without reopening that wave.

## Qud comparison: scoped and exact

Read local `/Users/steven/qud-decompiled-project` files only; no code or assets copied.

* `XRL.World.Parts.Skill/Axe_Dismember.cs:85–104` branches vital severing to `Axe_Decapitate.Decapitate`; ordinary branch calls `Body.Dismember(LostPart, Attacker, ...)` and bleeds afterward.
* `XRL.World.Parts.Skill/Axe_Decapitate.cs:88–97` calls Body.Dismember with Attacker, then applies its bleed, then calls `Defender.Die(Attacker, ...)` if dismembered mortal parts exist and **no mortal parts remain**. Its bleed precedes death; it does not simply omit bleed. This corrects an early preliminary inference that the branch suppressed bleed altogether.
* `XRL.World.Parts/Body.cs:2490–2617` threads Actor through before/after dismember events and performs unequip-before-cut. GA03f deliberately differs with detach-before-equipment-callbacks; do not undo that protection.
* `XRL.World/GameObject.cs:14491–14503,14542–14544,14627–14640` has a Dying guard, vetoable death events, explicit player death/checkpoint dispatch, and attributed kill/XP/removal events. This is not CoO's HP-poll modal design.

Classification: CoO-original death-state consistency repair, matching the source-attribution and bleed-before-completed-death invariant of the referenced family. NOT an exact Qud death port. Preserve CoO's existing “selected Mortal part dies” rule; do not introduce Qud's last-mortal-part rule, death vetoes, toggle behavior, two-hand proc chances, or active Dismember in A11.

## Smallest compatible production shape

1. Append optional `Entity source = null` to Body.Dismember after zone. Append optional source to CheckCombatDismemberment after rng. Existing positional/named two/six-argument calls stay valid. Thread the actual attacker at PerformSingleAttack's helper call and ctx.Attacker from Axe. Before/AfterDismember Source payload is optional API expansion, not required merely to reach HandleDeath; add only if its contract and tests are intentional. Died already has the required Killer key.
2. Keep death normalization centralized at the first accepted HandleDeath, immediately after setting `_DeathHandled` and before log/XP/drop/Died callbacks. For an existing HP stat, set its BaseValue nonpositive without resetting Max, Bonus, Penalty or Boost (e.g. preserve existing negative base via Math.Min). No fabricated extra damage event, second XP path or second energy charge. A central normalization also closes direct HandleDeath's currently inconsistent positive-HP state. Keep absent-stat API callers harmless.
3. Explicitly choose how the UI recognizes committed death. Robust small option: existing HP predicate OR the committed death marker, ideally through a single deliberately named predicate if root prefers avoiding a literal marker string in UI. Do not zero unrelated stat modifiers just to satisfy UI polling. Add positive-HP/marker and modifier controls before changing this consumer. Ordinary positive-HP unmarked actors must remain playable.
4. In Axe_Dismember, revalidate that this defender has not committed death before consuming proc RNG, and recheck after accepted Body.Dismember before its bleed. Preserve absent-HP synthetic body compatibility if using a numeric guard (missing HP is not proof of death). The same check handles an enhancement killing the defender before the cached survivor skill dispatch. Do not change all four killing-blow rider dispatchers or arbitrary skill dispatcher semantics.
5. Source/death callbacks must preserve first committed death as the attribution winner. If an AfterDismember listener independently kills with source B before the outer source A reaches HandleDeath, the existing sentinel must prevent a second kill/credit override. Do not move sentinel assignment after events. No broad callback rollback or resurrection design is proposed.

Zone-null Body.Dismember currently performs anatomy-only cleanup and does NOT trigger death. Many existing Axe anatomy tests use zone=null. Preserve that compatibility unless root explicitly expands the contract with its own REDs; central HandleDeath's zone-null case is different and already runs the death lifecycle without zone operations. Direct Body on an already-dead entity can still detach a part: the existing CanBeDismembered test pins exactly-one death log rather than blanket anatomy refusal. Avoid silently changing that API while protecting actual combat/skill producers.

`CheckUnsupportedPartLoss` is not Dismember and currently does not commit death for an implicitly lost mortal dependent. No shipped dependency layout establishing ordinary reach was found in this bounded scan. Keep as separately labeled API scope unless a real authored counterexample is established. Likewise foreign part ownership, arbitrary hook exceptions/revival, and A12 scheduler revalidation remain out of scope.

## Initial RED and counter-check plan (14 concrete rows)

Use fresh fixture per row and prove head is attached/Mortal/IsSeverable, both actors are resident, HP starts positive, and required parts/factory are live. For helper/proc tests use a deterministic Random subclass returning a legal passing roll; do not loop until a desired result on one mutated victim. Keep a full attack integration test in addition to direct public-helper tests.

1. `MortalLoss_CommitsNonpositiveHpBeforeDied` — sourced or currently source-less attached humanoid Head, live zone; observe HP and `_DeathHandled` inside Died, then removal. Current HP assertion RED.
2. `NonmortalLoss_DoesNotCommitDeath` — identical Hand/Arm: positive HP, still resident, no Died/XP/modal; control.
3. `MortalLoss_PreservesActualKillerAndAwardsXp` — proposed optional source, low XP victim/player source; exact Died.Killer ref and one XP delta. New signature can intentionally produce initial compile RED, but runtime attribution assertion must subsequently execute.
4. `UnsourcedMortalLoss_DoesNotInventCredit` — null source, one death with null Killer and no unrelated player XP; state normalization still expected.
5. `CombatHelper_MortalLossThreadsAttacker` — public helper receives passing RNG, nonlethal threshold-sized actual damage and source; victim Head removed with exact attributed Died.
6. `ActualBodyMelee_MortalLossThreadsAttacker` — actual PerformMeleeAttack with deterministic full attack RNG, real authored weapon/anatomy; assert damage remained nonlethal before mortal loss and head cut actually occurred. This verifies the caller cannot forget the new argument.
7. `AxeDecapitation_DoesNotReapplyBleedingAfterDeath` — own both skills, mortal-only severable candidate fixture, real Zone/StatusEffectsPart; positive ActualDamage Axe context; no effects after Died and source correct. Current postmortem bleed RED.
8. `AxeNonmortalProc_StillAppliesBleeding` — same forced proc but nonmortal candidate; live target contains actual BleedingEffect with source; control.
9. `AxeWithoutDecapitate_LeavesMortalOnlyBodyIntact` — same candidate setup minus marker, no cut/kill/bleed; control.
10. `PlayerMortalLoss_ActivatesActualInputDeathModal` — real InputHandler Update with valid zone/turn refs, boot inactive, no queued load/restart key; inspect private controller IsActive and one prompt. Do not manually Activate in this test.
11. `LivingPlayer_DoesNotActivateDeathModal` — same fixture and Update without mortality; control.
12. `CommittedDeath_PositiveComputedHpCannotMaskModal` — HP Bonus>0 (and optionally Min>0 separate later), committed death; existing HP-only check RED even after BaseValue fix. Explicit API-adversarial label.
13. `OrdinaryHpDamage_PreservesKillerAndExactlyOnceDeath` — ApplyDamage lethal control checks same callbacks and no duplicate credits when HandleDeath subsequently called again.
14. `DismemberVeto_PreservesHpAnatomyEquipmentAndCredit` — BeforeDismember returns false; no death side effects. Reuse accepted case with one flag flipped, not an unrelated unsupported body.

## Dedicated adversarial matrix (32 meaningful cases)

These are proposals, not a claim of failed execution. Root should prioritize confirmed lifecycle seams, retaining controls per invariant.

**State and UI (1–8)**

1. Existing negative HP is not raised to positive/healthy by normalization; Max unchanged.
2. Positive BaseValue + Bonus: Died sees committed marker, actual input modal activates; modifiers unchanged.
3. Positive Min bound: committed marker still activates modal despite Stat.Value floor; API case.
4. Body-less direct HandleDeath with HP normalizes and fires once; positive live counterpart remains unchanged.
5. Direct HandleDeath without HP stat does not throw and fires once; no invented stat required.
6. Zone-null direct HandleDeath normalizes and attributes but does not attempt a ground placement; distinguish zone-null Body-only control.
7. Zone-null mortal Body.Dismember retains the chosen existing anatomy-only contract and has no Died; prevent accidental test-suite-driven semantics drift.
8. Dead player removed from Zone/turn queue still reaches InputHandler modal before WaitingForInput gate; living non-current actor remains gated normally.

**Attribution and exactly-once consumers (9–16)**

9. Player source gains exact victim XP once; a second HandleDeath with a different source cannot steal credit.
10. Nonplayer source remains exact Died.Killer but no unrelated player Experience/reputation gain.
11. Null source emits null Killer and no corpse killer metadata; non-null control records exact ID/blueprint.
12. Guaranteed actual CreatureCorpse stores victim SourceID and source KillerID while victim cell is still valid; SuppressCorpseDrops counter creates none.
13. Real GivesRep receives the sourced death once and mutates the actual victim faction by exact Value; NPC source control stays unchanged.
14. Passive killer does not receive WitnessedEffect; separate visible passive bystander within radius 8 does. Null-source control includes the otherwise identical bystander.
15. Died listener recursively calls HandleDeath same target; one Died/XP/corpse/diagnostic, no recursion overflow.
16. AfterDismember listener kills via actual ApplyDamage with source B; outer source A does not overwrite the first committed death or issue second attribution.

**Axe/full-combat boundaries (17–24)**

17. Defender already killed by an actual earlier ApplyDamage: Axe skips proc, anatomy events, RNG and new bleed; live counterpart procs.
18. Actual item enhancement bonus damage kills after hpAfter was captured: subsequent Axe does not sever/rebleed, while the enhancement itself still fires. Use a shipped damage enhancement rather than globally mocking combat survival.
19. Full ordinary killing blow still dispatches the intentionally supported class/weapon/gas/enhancement riders (retain existing SM7 controls; no blanket survivor gate).
20. Axe miss/no ActualDamage and wrong damage attribute do not roll or bleed; two parameter rows if useful, one invariant.
21. Axe chance-fail leaves head/HP/effects intact; deterministic chance-pass control confirms setup.
22. Accepted nonmortal dismember whose AfterDismember observer kills the victim does not receive a late Axe bleed; same observer disabled produces bleed.
23. BeforeDismember veto after forced Axe proc leaves no bleed/kill; rejected command does not fabricate a successful proc receipt.
24. Existing CanBeDismembered veto and damage threshold remain honored after source threading; source receives no death credit for a rejected cut.

**Cleanup, save, and callback boundaries (25–32)**

25. Mortal Head with an actually equipped/enhanced head item clears exact slots/cache/contribution once, then other death gear drops once. Reuse GA03f invariants, do not invent raw equipped aliases.
26. NoDropOnDeath/Temporary controls retain existing death-drop policy while HP and Died still commit. Selected-limb equipment already cleaned by Body is an existing order distinction—do not claim those tags suppress the entire preceding dismember operation.
27. Active status effects present before mortality are removed with owner-died reason once; no new Axe bleed afterward.
28. Current reciprocal hauling link releases through Died and no later pull occurs; existing GA03e control, not scheduler A12.
29. Already-dead target's later direct Body.Dismember does not duplicate HandleDeath; preserve current direct API semantics/test, while actual combat helper refuses dead target.
30. Healthy isolated checkpoint → mortal death → real L load restores fresh player/body refs, attached head, positive HP and inactive death modal; old dead ref remains marked. Save alias verification, no resurrection tag clearing.
31. First death callback observes committed state before message/XP/drop callbacks can recursively query or attempt damage; exact-old victim remains the target. Arbitrary throw/rollback not claimed.
32. Two independent victims die from nested callbacks: each gets its own source/HP/once-only lifecycle; no static global guard may suppress the second victim.

Optional hypothesis only, not required expansion: CanBeDismembered callback kills the target and returns true before the helper's final Body call. Existing guard is only pre-callback. Establish RED and ordinary relevance before deciding if a second local guard belongs in A11. Likewise self-kill XP level-up may heal the dying actor through existing LevelingSystem; do not assert a new no-self-credit policy without separate scope/RED. Committed-death UI recognition should remain reliable regardless.

## Concrete reuse and fixture isolation

* `Assets/Tests/EditMode/Gameplay/Anatomy/GameAuditEquipmentLifecycleTests.cs` exposes internal `EquipmentLifecycleFixture`, Actor/Body/Inventory/Zone, `Item(string)`, `EquipDuelistBuckler()`, `Detached(Entity)`, `Ground(Entity)`, `RoundTrip()`. It scopes HotbarSaveFixture(false,false), visual/render delegates, MessageLog.OnMessage and EquipmentChangeBus; disables LootDropSystem.Factory/CorpsePart.Factory. For corpse/loot tests explicitly set the relevant factory after fixture creation and restore through the owner scope.
* Existing `AxeDismemberTests` and `AxeDecapitateTests` have useful candidate shapes but use zone=null; they prove limb selection, not death. The Decapitate-alone test merely checks a new body without actually invoking a dispatcher; new absence controls must invoke `SkillEventDispatcher.AttackerAfterAttack(actor,ctx)` so the no-op is nonvacuous.
* `CanBeDismemberedTests` pins public helper veto/chance and already-dead behavior. Its mortal direct-call test at 287 expects one kill message, not prevention of all dead-body anatomy work.
* `Presentation/UI/DeathScreenControllerTests` has PRIVATE fake IInputProbe/ISaveLoadService/ISceneRestarter classes. Recreate small fakes rather than assuming cross-fixture access. Its direct controller tests cannot establish InputHandler's activation predicate.
* Real InputHandler fixture must set PlayerEntity, CurrentZone and TurnManager, make boot modal inactive, restore input devices/static UI flags/hooks, and invoke a genuine Update without a fresh L/R key. Otherwise source death can succeed while the fixture never reaches activation.
* Use diag damage/DeathHandled filtering by exact actor/target IDs and receipt count (channel restored after test), not any global prior record. Before/after corpse and XP counts must use fresh state.
* Root should preserve the existing dirty CombatSystem hunks by surgical patch only. No branch/reset/replacement from HEAD. Append arguments rather than replacing existing public signatures at their front.

## Bounded native follow-through

Reuse `GameAuditEquipmentLifecycleBenchPlayer.cs`'s real death polling helper (`_deathScreenController` IsActive), queued-key Tap, isolated checkpoint path, fresh graph assertions and L recovery. The GA03f driver already learned that F6 is suppressed while dead and successful modal L has no GameLoaded prose; retain that distinction.

Minimum next bench: healthy F5 checkpoint, actual staged mortality trigger, observe nonpositive base/committed state plus prompt, exact killer/XP/corpse or player modal, F6 suppressed while modal, L restores a different healthy object/body. If mortality is invoked by a scenario hook/API, label it as service integration plus native modal input—not native combat. To claim actual native combat use a real adjacent bump attack with deterministic full RNG/anatomy setup and prove the damage itself was nonlethal before the cut. F8 is not such a trigger. A one-shot sampled Axe proc without asserted preconditions is not reliable proof.

Can verify: source and target identities, corpse metadata, XP/reputation deltas, logical HP/marker/anatomy, effect absence/presence, real input controller state, fresh save aliases, exact callback counts. Cannot verify from these assertions: visual severed-limb art, camera/impact feel, general combat balance, all callback exceptions, arbitrary modded mortality, or A12 scheduling correctness.


## Root decisions / implementation log

2026-09-06, baseline b8dd575f,9417GREEN. Source plan above is historical preparation.
Root verified Body/Combat/Axe/UI/Stat/Leveling and actual prior fixtures. Preserve
zone-null Body anatomy-only and the four intentional killing-blow riders. Append
optional source arguments without expanding dismember event payload. Expose
CombatSystem.IsDeathHandled(Entity), meaning first death lifecycle committed, not
all callbacks complete; use it beside existing numeric UI/helper checks and in
Axe before its RNG and after a successful cut. Normalize existing HP base with
Math.Min(current,0) after the sentinel, preserving all modifiers/floors/negative
values. Direct Body after committed death remains its old anatomy API contract.

Additional actual producer: Player inherits XPValue10. Self poison damage carries
the grenade creator as source. At Experience105/Level1, self-death can award10XP
and LevelingSystem currently heals the committed victim. Preserve XP/level/MaxHP/
MP/SP policy, but skip only level-up refill when death is committed. Pair with
living level-up heal. This is a bounded stock callback repair; arbitrary callback
HP writes remain outside the guarantee. Delayed BleedingEffect damage still uses
null source and is separately queued, not claimed fixed by immediate attribution.

Initial runtime tests will run against unchanged production. Reflection invokes
the old/new optional source API so missing source can fail as runtime attribution,
not merely a compiler error. Native generator is independently prepared under/tmp;
root will review/apply after RED and minimum GREEN. Protected Combat/Input files
have whole before copies; commit only attributable incremental hunks.

Initial16 runtime RED:6PASS/10FAIL,0CS,11:36:30Z,.410217s. Death/UI/source/
postmortem bleed/self-level-heal failures reproduced. The living level control
assumed factory Player already had MP; Player has SP but no MP, and source has optional grants.
Corrected to explicit MP2/SP3 economy fixture values before production repair.

Corrected16RED:7PASS/9FAIL,0CS,11:37:22Z,.4072162s. Applied minimum6-file
repair (DeathScreenController comments only); focused102GREEN,0CS,11:38:54–55Z,
1.3927816s. Dedicated compilation first found ambiguous System/Unity Random; raw
compile log retained, explicit System.Random alias added. No stale XML claimed.
Added pure predicate, earliest death-message reentry and missing-HP Axe controls.

Dedicated56:53PASS/3FAIL,0CS,11:43:41–42Z,.8215731s. Genuine permission-hook
death still proceeds to cut; add a post-CanBeDismembered death recheck after RED.
Two witness fixtures incorrectly assumed Villager.Passive; positive preconditions
caught this. Replace with actual SummitSinger whose authored Brain.Passive=true,
then rerun. Full actual Battleaxe9/10 threshold pair and grenade creator poison
self-death both passed. Added mortal post-cut first-independent-killer control.

Corrected57:56PASS/1FAIL,0CS,11:45:12Z,.6829652s; actual passive witness
controls now pass and permission-callback death remains the sole confirmed RED.
Added final helper death/HP recheck before fired receipt and Body call. Native
generator fully read by root (all3sources plus isolation template), now applied.

Focused237GREEN,0CS,11:46:12–14Z,1.849238s. Actual PaleSalt/SkeletalSentry
matching-tag lethal vs nonmatching forced-Axe pair added;59totalGREEN,0CS,
11:48:30–31Z,.9622337s. Emitted bonus amount4 consumes3remaining HP; no overclaim.
Review requires scoping EnhancementFactory dictionaries/initialized state around
this pair; added explicit actual-type registration/restoration for isolated runs.
Native24PASS/0unexpected/validteardown, run97855483b6fa4b198630c6b49a857467;
pre-offhand-review artifact retained. Does not prove every continuing weapon gate.

Six new same-call participant rows:3PASS/3FAIL,0CS,11:52:05–06Z,.3462004s.
Two positive-modifier committed-death continuations reproduced. Living modifier
control clamped100before/after and hid actual damage, so its expected class RNG
was invalid; raise fixture Max200 to establish the same positive hit delta.
This is combat-call continuation, not deferredA12 BeginTakeAction scheduling.

Corrected participant6:4PASS/2FAIL,0CS,11:53:50Z,.3114984s. A guarded edit
initially missed the existing attacker-null prefix and stopped before writing;
an unchanged-source focused run is retained separately. Added marker checks at
three existing same-melee stops: both participants before each weapon, and
attacker after ApplyDamage. Two living allowed-offhand positives must actually
land the secondary weapon, not merely roll its chance. Native now forbids trailing
secondary RNG after committed death. Preliminary positive-bonus native calls
already had none, so the review's inferred native-defect wording is corrected.


Final focused246/246GREEN,0CS,11:56:45–47Z,1.4341572s.67new tests comprise
16initial and51dedicated, including8same-call participant rows. Independent final
source review is clear after three guard and registry-isolation corrections.
Final functional native24PASS,0unexpected, run62e876934d984879a07b406921706d31,
4.9974543s; shutdown5.0166785s, runtime unregistered/root held then removed.
Raw JSON/log and preliminary native report retained. Full suite executing.

## Performance and claim boundaries

Existing InputHandler.Update gains one constant-time tag query in its current
polling branch. No collection/cache/listener/renderer responsibility is added to
ordinary frames; combat/death checks stay in their existing calls. Source was
prepared before profiling, so the baseline will be measured by temporarily
restoring only the owned Input predicate, preserving every other source byte,
then reapplying it for an identical75s idle/discrete/held native workload. This
is a regression profile, not an optimization or a speedup claim. PERF-FOUNDATION
and existing profiler/recording patterns were read before instrumenting.
Raw maxima, percentiles, GC and wall-frame samples plus accepted input/turn checks
will be retained; instrumentation and inherited engine work are included. No
claim that a tag lookup causes measured differences or that allocations are zero.

## User steering after the source repair

The user explicitly requested more equipment for entities. GA03i will follow this
wave with source-verified authored loadouts for suitable existing roles/anatomies,
real existing sprite-backed gear and checked shop/loot integration. No current
content producer is retroactively claimed for GA03g. The remaining A12/system-audit
queue stays active after the equipment content wave.

## Resumed verification and sprite steering — 2026-09-06

The user explicitly resumed coding after the play-session pause and requested
sprite improvements as problems are found. The pre-pause full run completed
9484/9484GREEN,0CS,11:59:08–12:01:36UTC,147.6313981s; its compressed XML is retained.
No code changed during the play/review pause. Unity was already out of Play when
resumption began and was closed before Assets edits.

The performance-only branch now profiles the existing healthy arena for25seconds
each of idle, discrete arrows and held arrows, with a northwest closed route
that avoids both NPCs. It adds no functional Check groups and preserves the24
mortality groups. Required markers, actual accepted moves, held repeats, HP/Speed,
turn state, record capacity and private-save teardown are checked. Root read the
complete generator and source templates before applying it. The baseline wrapper
retains an exact post-fix source backup and restores the input predicate in finally.
Final native compatibility and full-suite checks follow these harness changes.

## Final exit — 2026-09-06

Full9484GREEN/0CS,2026-09-06 14:02:42Z–2026-09-06 14:05:13Z,150.8348561s.
Final functional native24PASS after profiling harness addition, complete teardown.
Both healthy-input75s profiles accepted every move; source restoration is hashed.
GUID2458/0collisions.67new tests=16initial+51dedicated.
Source/independent reviews, corrected hypotheses and honesty bounds are retained
in GA03h-REPORT.md. Six production paths, two test classes, three native classes
with metadata and living docs/proof ship together. Protected prior work remains
unstaged; no sprite/content edits. Next: GA03i equipment content and item art.
