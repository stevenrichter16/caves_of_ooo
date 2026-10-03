# Predator diversion: independent practical/presentation review

Reviewed 2026-10-03 against `Docs/PREDATOR-DIVERSION-DESIGN.md`, after the initial production publication and before the ordinary native journey. Review role: read-only production audit except the explicitly authorized native-driver approach-proof tightening. No Unity, git, source grants, runtime actor edits or independent claim of completed native play.

## Outcome

No remaining significant correctness issue found in the inspected priority, hidden-information, physical-food, presentation or source-admission boundaries. The ordinary route remains to be demonstrated by the root-owned native run; source feasibility is not a substitute for that result.

One native evidence weakness was resolved: `diversion_native_approach` originally accepted any hunter displacement since throwing. It now also requires `MeatApproachRemaining < MaximumMeatApproachActions`, so an unrelated original-hunt move cannot masquerade as a paid diversion approach. A second-food counter is accepted only while the same original hunt remains Pursuing/Searching, its BoredGoal owns the behavior and it has no unrelated current target. A terminal or already-preempted predator cannot supply that counter.

## Q1 — Verified premises and practical usefulness

- The Duelist's real starting definition supplies exactly two DriedMeat, each a valuable 3d4 healing ration. RawMeat competes with 1d4 healing, cooking and sale. The new capability consumes an existing useful item; no grant, reward or additional loot source is needed. Sources: `Assets/Resources/Content/Data/Builds/StartingBuilds.json`, exact RawMeat/DriedMeat entries in `Assets/Resources/Content/Blueprints/Objects.json`.
- Meat previously lacked HandlingPart, and `HandlingService.IsThrowable` requires that part. The root-owned data change now gives only the two accepted foods actual one-hand carry/throw handling at weight2. Duelist Strength16 supplies a16-cell range under the existing formula; this permits throwing from beyond the hunter's sight radius8 into its meat notice radius6. The driver verifies a fresh first-impact ray to empty ground, never a hit on an animal.
- Food descriptions honestly say a hunting furrowstalker *may* stop once, tell the player to remain out of sight, and explicitly distinguish throwing at the animal as an attack. They do not promise taming, a saved grazer or permanent safety. Legacy retained hunters remain unchanged by design.
- Compared with the existing grazer, this is a new intervention: ordinary Reedback already flees nearby player/hostiles and consumes one separately reserved crop row. A new grain-bait synonym would add little. Meat diversion changes the hunter's current position and spends its finite attention while leaving the prey and original hunt intact.

## Q2 — Authority, persistence and hidden information

Inspected `Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs` in full, plus BoredGoal, BrainPart, SpreadActorContext, SpreadGrazerPart, FactionManager, FindPath and the current source-admission call.

- `TakeIdleAction` checks actor context, visible outside-quarry threats, current unrelated target, low health and home leash before diversion. The threat predicate is not merely faction hostility: `FactionManager.GetFeeling` includes both participants' PersonalEnemies before faction/party calculations. Higher goals retain Brain action ownership; the new event handler only ends this role's commitment.
- Public meat requires an exact accepted blueprint, real current ground owner, food/physics/stack ownership, positive quantity, no private/service/quest provenance and safe source cell. Notice scans only local cells within radius6 or shorter sight. Both initial notice and bound actions use actual LOS. Movement uses the existing path and paid action; feeding and movement do not run together.
- Binding keeps an exact Entity reference, ID and anchor. Removed, carried, moved or replaced food cannot be followed by hidden coordinates. The diversion path does not inspect live hidden quarry coordinates or refresh original last-seen/pursuit/search fields. Normal hunt continuation later reacquires only through its existing visibility rules.
- Two progress actions emit Interaction while actual food and animal are current; the following action spends the allowance before removal/dirty callbacks and removes exactly one unit. A multi-unit stack retains its owner. No player Food healing or extra loot is invoked. An active role's same-actor reentrant action is guarded.
- Saved fields use the normal Part persistence surface. Generation passes enabled only from the current enabled manifest13; optional Configure/TryPlace defaults preserve old callers and lower retained generations. Source: `SpreadExplorationHunt.TryPlace` and `Generation/Builders/SpreadExplorationBuilder.Hunt`.
- The remaining general inventory transaction, malformed-save and competing-hunter cases are also owned by the services reviewer and its focused mechanics suite. This note does not substitute source inspection for those native results.

## Q3 — Presentation and visible consequence

- Existing imported Furrowstalker art provides Idle, Walk, Interact, Attack and Hit; existing portable art already represents both meat blueprints. No creature, mesh or material registry was added for this feature.
- Active Look prose describes investigation or feeding only when the role, brain and exact public source remain authoritative. It does not disclose coordinates or hidden quarry state. Completed diversion prose is conditional on a current original pair; ordinary terminal-hunt prose remains separate.
- Added `PredatorDiversionPresentationTests`: four controlled paired cases (RawMeat singleton / DriedMeat stack2 × enabled / legacy). They use actual factory owners, BoredGoal turn dispatch, exact current submitted models and explicit presenter refresh. They require two current-owner progress hooks, then either singleton view removal or the same remaining stack view with count−1. They disclose synthetic placement/reveal and do not claim ordinary input or natural visibility.
- Added two isolated launcher/initializer guards. Root reported the expected native RED for both guards and enabled render cases; root's subsequent72-case run passed these cases, with one separate diagnostics-isolation failure assigned to the services agent. I did not run Unity or independently inspect that full result file.

## Q4 — Ordinary acceptance boundary

`SpreadDiscoveryNativePlayer.PredatorDiversion.cs` reuses the existing isolated launcher, real Duelist selection, ordinary map travel, inventory throw targeting and paid wait keys. Main-partial additions only select/report/clean up the new mode.

The predeclared candidate is seed64 `Overworld.12.6.0`. The latest retained F11 native source (`Docs/Verification/QuestFreeExploration/F11/Native/f3d80a1c7d164a8593a3292a55a4a339/report.json`) placed the hunter at49,18 and quarry45,14, near ordinary descent40,12. Older standalone F11 observations used different coordinates and were explicitly excluded from native geometry assumptions. Current v13 owners and positions are recorded afresh; no source retry or hidden transfer is present.

The route first seeks clear ground within6 of the hunter and at least10 from the player, directing the animal away from player threat range. At most8 ordinary safe approach steps,16 observation waits and2 repeat-probe waits are permitted. No NPC/food/HP/faction/time/RNG changes are used. If current geometry or behavior prevents this, the scenario retains an explicit failure rather than setting up success.

Acceptance requires the actual one-unit throw, a paid diversion approach, both current-owner Interaction hooks, a naturally running native Interact frame, consumed exact food owner, one original carried ration remaining, later original-hunt continuation, and native F5/F6 exact replacement conservation. The second actual ration is thrown only after the checkpoint as an unsaved spent-allowance probe; F6 restores the saved retained ration. A disclosed controlled fixture exists only in the automated presentation tests, not behind an ordinary-mode fallback.

Still pending at this review boundary: ordinary route completion; independent screenshot/pixel inspection; native proof that this one selected encounter affords the full diversion and return. Not claimed: all-seed usefulness, unaided discovery, human readability from a passing mesh check, long-session economy, balance, performance, guaranteed quarry survival or Qud source parity.

### First ordinary run and bounded driver correction

Native run `db987bd6f84f47b383c88232bd03d3f5` passed ordinary build/provisions/generated-hunt checks, then stopped before any throw: actual player40,12, hunter48,4, quarry45,4, tick110. The descent paid one real hunter action; the post-arrival player-hunter distance was exactly8, so the original blanket outside-threat precondition refused. HP40 and both original meat units remained untouched. The report did not capture Brain target/goals, so it does not prove the hunter had already acquired combat. This is retained native evidence, not discarded as a generation failure.

Root authorized a native-driver-only correction: at most2 ordinary withdrawal steps, counted within the existing total8 approach cap. It only permits a still-active original hunt without player/personal/unrelated targeting or higher goals, selects a current safe unoccupied outside-sight neighboring cell, uses the existing paid StepTo and rechecks the actual role afterward. It never clears targets, alters goals, changes actor positions directly or suppresses combat. Arrival now records home/phase/budgets/last-seen/sight/target/goals/personal hostility/distance/LOS and captures a screenshot before local input. A further native run must establish whether that real withdrawal creates an opportunity; no success is asserted from this code correction.

### Final native boundary and cleanup review

Journey tuning stopped after `07018f0b1b704c4ebd4bcb423fbc64bd`, at the user's direction to move on from peripheral harness work. Preserve every failed RunId. The final attempt generated the original hunt and used ordinary movement; all physically valid nearby bait landings fell within the live ambient PetDog's actual NoticeRadius10. Its changing current position was logged. No ration was thrown in that final attempt, and the bounded noncombat-hunt guard stopped further input.

Earlier run `9b9cc9fc96de4d70a42f861bf099b4c1` supplies partial ordinary evidence: actual original DriedMeat throw onto clear ground; exact native meat admission; paid approach with original hunt history preserved; then PetDog FetchAdmission and ordinary fetch movement caused the predator to choose PetDog as a genuine hostile and abort with `threat-priority`. No dog, food or hunter was moved, controlled, granted or suppressed by the driver. This demonstrates integration and correct interruption, not completed ordinary feeding or save/load. Root reports the controlled native feeding/save suite GREEN at104 cases; that evidence remains explicitly separate from ordinary play.

Final cleanup/isolation cold review found no major issue in the delivered mode path. Initialization refuses absent isolated saves before input or diagnostics changes. The mode reuses existing launcher save/prefs/bootstrap/scene ownership. Feeding callback subscription is removed both by journey-finally and idempotent Cleanup; its frame coroutine is stopped. Abort/exception disposal routes through Finish/Cleanup, keyboard/settings/background restoration, then existing launcher teardown. The scoped ai diagnostic channel is restored from its captured value by OnDestroy alongside the existing channel set. Other modes explicitly store predatorDiversion=false when launched. No gameplay behavior or source selection was changed during this final review.

Delivery claims must retain the ordinary unmet checks. No further route expansion, source reroll, actor control, priority relaxation or additional harness work is recommended for this milestone.
