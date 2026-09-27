# Complete the existing pet fetch loop

Status: core implementation published by root and actual Unity 751/751 GREEN on 27 September 2026; independent bounded peer review clear. The unchanged primary 20 native fixture first produced 13 RED / 7 controls. Root then published the thirteen-path package. The native generated-dog/original-dagger route was attempted and failed its outward-wait premise after four passed setup/admission checks. End-to-end keyboard delivery, mid-return load and repeated delivery are DEFERRED — LOW PRIORITY at the user’s direction; they are not complete. Root owns publication and editor gates; the runner and reference compiler remain separate evidence.

## Intended behavior

An existing allied pet dog notices an actually landed thrown item, walks to it, carries that exact object separately, follows the thrower's current position, and drops it on the dog's own adjacent cell. Repeated throws should remain possible. Normal pickup, hoarding, recruitment, party transit and death loot retain their existing meanings. This completes CoO's live `AIRetriever` promise; no Qud parity or new content source is claimed.

## Verified corrections

| Initial premise | Current source and consequence |
|---|---|
| The TODO only needs a final drop | `GoFetchGoal.DoPickup` invokes ordinary `PickupCommand`: compatible resident stacks consume the incoming identity, GoldCoin becomes purse credit, and an eligible empty body slot auto-equips. Dropping the original reference may fail; dropping the surviving resident may take pre-existing property. Isolated acquisition is a required part of the change. |
| A dog cannot equip the thrown weapon | PetDog inherits Creature's unspecified Body; EntityFactory's fallback is Humanoid. Its presentation rig does not determine gameplay anatomy. Preserve anatomy, but bypass auto-equip only in the new retrieval acquisition mode. |
| Pickup always transfers one unit | ThrowItemCommand normally produces one physical thrown unit, but PickupCommand acquires the full current stack. Record the exact landed quantity and reject a changed quantity before acquisition; never silently acquire later merged additions. |
| ReturnHome means return to owner | It means the dog's recorded StartingCell. Keep `ReturnHome=false` for retrieval and add an explicit return-to-thrower mode. Do not rewrite AIHoarder's home loop or loot behavior. |
| Current fetch progress survives saving | SaveGoal writes public mutable fields plus Age; LoadGoal uses GetUninitializedObject. `_phase` and `_walkAttempts` are absent. A saved carried fetch restarts WalkToItem and cannot find its item on the floor. Persist required progress using the existing supported field stream, without a general serializer change. |
| Changing private fields is merely an inspector refactor | AIDebugTests deliberately pins private `_walkAttempts` for the old inspector use. The new need is persistence, not inspector access. Replace only that stale encapsulation pin with a saved-state/GetDetails control and document the reason. |
| Null throwers are correctly rejected | AIRetriever's present condition checks alliance only when thrower is nonnull, so null passes even with AlliesOnly=true. Actual ThrowItemCommand supplies a thrower. A return mode needs an exact live current thrower for either AlliesOnly setting; environmental orphan throws should remain untouched. |
| The event's LandingCell establishes authority | The current part checks distance but not that the cell is the current zone's cell or that the item is still there. Validate exact graph membership, Physics ownership/backlinks and source quantity. Revalidate after the event and before pickup because the throw transaction may roll back or another dog may win. |
| Recruitment is needed to enable this ordinary content | Villagers' initial player reputation is50 and IsAllied accepts>=50. PetDog already uses Villagers/AlliesOnly, with notice10, inventory capacity10 and Strength10. No recruit/rep grant is needed. |
| PetDog is dev-only | ReferenceGladePlan has a real fixed PetDog source at33,23, and SpreadTier1 includes optional PetDog groups. Use the actual generated dog; its later position may differ after normal scheduling. |
| There is no legitimate starting throw object | The ordinary Dagger is throwable, weighs4 and is within the dog's capacity/lift strength. A native route can throw the original starter item. The old PetDogFetchesBone scenario grants a bone/spawns a dog and is not suitable acquisition evidence. |
| Throw completion means every broadcasted source survived | ItemLanded is emitted after placement but before the outer transaction commits. A rollback may remove the landed source after a goal is queued. Each step must refuse a stale source rather than manufacture an item. |
| Every interruption should teleport the item home | Dead/detached/foreign recipient or exhausted return budget should permit only a normal DropCommand at the living dog's actual current cell. A dead/detached dog must leave existing death/ownership handling intact. A vetoed drop is reported and bounded, not forced. |
| Followers cannot interact with this goal | A fetch pushed onto an already recruited dog's stack runs above persistent FollowLeaderGoal and can finish normally. Recruiting during a fetch pushes a new persistent follow above it, potentially burying the fetch. This requires a real paired scheduling witness before choosing a narrow ordering correction; do not rewrite follower priorities from the TODO alone. |

Sources read: AIRetrieverPart, AIHoarderPart, GoFetchGoal, GoalHandler, BrainPart goal stack/TakeTurn, MoveToGoal, FollowLeaderGoal, RecruitedEffect, ItemLandedEvent, ThrowItemCommand, PickupCommand, DropCommand/DropPartialCommand, InventoryPart/StackerPart/InventoryTransferSnapshot/InventoryCommandExecutor, InventorySystem, SpatialQuery/FindPath/MovementSystem, FactionManager, EntityFactory, SaveSystem goal/field stream, actual Objects/Factions/PopulationTable/ReferenceGladePlan and existing AI/debug/follower fixtures. Exact current source hashes are in `sweep-preimages.json`.

## Proposed minimal design

### 1. Retain ordinary acquisition semantics; add explicit retrieval acquisition

Keep the public ordinary `PickupCommand(Entity item)` path byte-for-behavior compatible. Add a narrow internal factory/constructor for retrieval with expected landed quantity. It uses all existing validation, physical source, lift/capacity, transfer claim, BeforePickup/BeforeBeingPickedUp, Taken/AfterPickup and rollback paths. Only this mode retains a separate actual inventory entry (no stack merge), keeps physical GoldCoin as the original object (no purse credit), and does not auto-equip.

InventoryPart gets a narrow internal nonmerging add path through its existing capacity/backlink/handling-penalty implementation. Do not directly edit Objects from AI, split/clone items, change Stacker.CanStackWith globally, suppress quest hooks, change weight or allocate a second item. A same-blueprint resident remains unchanged. A supplied exact expected quantity must still match after pickup veto callbacks and before removal. After success, the goal requires the exact original instance in the dog's real carried collection with the expected positive quantity and current Physics owner; otherwise it relinquishes authority and records the changed-source refusal. It must not steal from a callback's new owner or attempt to reconstruct consumed contents.

Counter-check normal Pickup remains merge/gold-credit/auto-equip as before; retrieval refusal must preserve ground position, source count, resident counts, purse, gear and capacity modifiers. Include thrown gold as physical property, not new currency.

### 2. Explicit saved goal modes and finite progress

Keep the existing constructor and ReturnHome public field. Add a constructor/factory for exact `ReturnTo` entity + explicit retrieval mode + original alliance policy. Keep legacy mode zero, so absent old fields do not accidentally create a recipient or enable retrieval. Required phase/counters/expected quantity are public saved fields (with documented serialization purpose), not private flags or constructor-only defaults. Phase values are validated; malformed saved state fails conservatively.

The legacy one-way/home code retains its ordinary acquisition/home semantics, while its phase/attempt count become saveable. If an old snapshot has no new progress fields, a still-ground source can begin the old behavior. A same-reference dog-owned source can be recognized only for an existing ReturnHome request, to resume best-effort home; a legacy one-way saved retriever has no saved thrower and cannot be retroactively returned. Do not invent a recipient or rebuild saved parts/stats.

Retrieval uses WalkToItem -> Acquire -> ReturnToThrower -> Drop -> Done. Each executed goal action performs at most one actual move/open-door, pickup or drop. It resolves the live target and its physical footprint every return action. Recompute an actor-passable approach cell at physical distance<=1; use the existing pathfinding and `TryMoveDetailed`, including stationary opened-door actions. Do not walk into the thrower or push a long-lived child with stale target coordinates. No gameplay RNG is needed for tie-breaking: use deterministic distance/coordinate order.

Named limits proposed: preserve the existing two100-action outward path attempts; at most100 executed return actions, at most3 consecutive blocked return actions, and at most2 ordinary drop attempts. These are safety caps, not promised shortest paths. Persist all counters; a saved/reloaded loop must not reset its allowance. Ordinary transient combat can suspend the goal without forcing the dog through danger. Age is already incremented while buried; add a finite overall age cap for a resumed overdue retrieval to choose local drop rather than a fresh chase. Inactive/unloaded zones do not imply elapsed scheduled actions.

### 3. Authority and failure behavior

At admission require the exact part/brain/inventory/Physics owner, living non-death-handled dog, real current zone+cell, exact live thrower distinct from the dog/item, real same-zone thrower membership, exact current landing cell/item membership, positive unchanged item quantity and Takeable/Carryable. Honor AlliesOnly=true via current alliance; false retains deliberate hostile-thrower support, but never accepts a null/foreign/dead recipient. Radius remains current authored notice radius.

Before acquisition, losing recipient/source authority cancels without taking anything. After acquisition, recipient death/removal/zone separation or newly disallowed alliance switches to local drop. A returned item externally consumed, transferred, equipped or quantity-mutated is no longer an authorized fetch payload; finish with a reason without touching its new owner/stock. A living attached dog uses normal drop on its own cell. Drop veto/refused placement retries only within the fixed budget, then reports retained/refused; it does not bypass hooks. Dog death/detachment never triggers synthetic floor placement, resurrection or cross-zone teleport.

Duplicate ItemLanded callbacks keep one active fetch. Competing dogs resolve through actual source ownership and transaction claims, so only one can acquire. No global owner table or new world ledger is needed.

### 4. Recruitment/interruptions: test first, constrain any addition

The first candidate will include an actual `RecruitedEffect` before-fetch control and a recruit-during-return RED. If the latter confirms permanent burial, the proposed correction is a narrow stack insertion of the new persistent follow beneath an existing *active retrieval subtree* rather than above it. It must leave the same fetch/child instances, ParentHandler relationships, callback counts and other combat goals intact; no remove/re-push (which would fire OnPop/OnPush), no full stack restore used as a general mutation. Put this in a dedicated internal Brain insertion helper used only by RecruitedEffect if required. Existing hoarder/default fetch and ordinary recruitment stay unchanged. Root review of this additional surface is required before production implementation.

Explicit wholesale goal clearing remains task cancellation. Do not introduce inventory writes in arbitrary GoalHandler.OnPop or shared death/clear/save paths. Report any real ordinary caller that clears a live acquired retrieval; if one makes the authorized loop lose property, add a specific paired fix rather than a generic unsafe cleanup callback.

### 5. Observable outcomes

Use the existing enabled-only AI diagnostic channel for admission/refusal, acquisition, return action, local-fallback reason and committed/refused drop. Records contain exact dog/thrower/item IDs, phase, quantity and remaining bounds; payload allocation is gated. Keep visible messages short and accurate, using existing actual pickup/drop feedback. Do not claim arrival on a refused drop. No new UI, recruitment unlock, content blueprint or model is required.

## Test-first sequence and acceptance

1. Write behavioral tests against current APIs that demonstrate: actual ItemLanded -> pickup -> item never returns; compatible carried stack consumes the incoming identity; GoldCoin changes dog purse; weapon pickup equips; saved post-pickup/home phase restarts incorrectly; moving thrower never receives it. Pair alliance/radius/duplicate/no-source and ordinary hoarder/normal pickup controls. Run and archive actual assertion RED before adding any API.
2. After root approves this plan and the RED evidence, implement only the acquisition + saved goal mode + event wiring. Run matched GREEN, then per-finding adversarial cases for forged landing cells/backlinks, quantity mutation, after-hook transfer, claim races, death/detachment, moving/large-footprint recipients, blocked routes/doors and finite saved bounds.
3. Probe recruitment interruption separately. Any insertion change requires actual RED and focused existing follower/recruit/goal-stack/save controls, not the speculative TODO. Do not broaden into general follower scheduling.
4. Save round trips at outward, carried, returning, fallback and completed stages use the real serializer and replacement graphs. Exact item identity/count and unchanged residents/purse/gear are required. Replaying completion never drops twice. Old-field-absent cases explicitly pin legacy no-recipient behavior.
5. Independent Q1–Q4 review and matched neighboring inventory/AI/follower/save differential in the private runner; native-reference compilation is distinct evidence. Root later publishes tests for actual Unity RED/GREEN and owns any production/native window. Do not write a new test for every branch merely to increase counts.

Native route, only after core/native GREEN: an isolated launcher follows the existing current C9/C7 save/scene restoration pattern. Use a generated ordinary world and the real PetDog source; report actual dog ID/current location, ordinary player stats and original dagger ID/count. If a start-location shortcut is needed, label the single live-player approach explicitly; never stage/teleport dog or item. Send real inventory Throw input along a physically clear ray; record actual landed unit, item event and admitted dog. Advance real paid movement/wait turns until the exact source is carried, then move the player so the return destination actually changes. Use F5 while returning, make a real paid world change, F6, and require replacement dog/player/item/goal graph with exact phase/budget/current ownership. Allow only finite normal actions to finish; require the exact original unit on the dog's cell adjacent to the *current* player, unchanged dog resident stock/purse/gear, then a native pickup and a second legitimate throw/return if route survival allows. Player pickup may auto-equip the exact dagger and is not a harness failure. No HP, item, XP, currency, faction, cooldown or path grants; ordinary hazards and failed throws remain honest failures.

Script-observable: current source authority, exact acquisition/return/drop conservation, paid actor scheduling, finite state, live target movement, replacement save graph and repeated-use behavior. Not proved by runner/compiler: native input/timing, visual dog motion, user discoverability, all seeds/animals, combat balance or malformed historical missing-recipient reconstruction. Native pictures must be viewed before visual claims.

## Exact core ownership

Primary existing production: `Gameplay/AI/AIRetrieverPart.cs`, `Gameplay/AI/Goals/GoFetchGoal.cs`, `Gameplay/Inventory/Commands/Acquisition/PickupCommand.cs`, `Gameplay/Inventory/InventoryPart.cs`. A small internal `Gameplay/AI/BrainPart.cs` + `Gameplay/Effects/Concrete/RecruitedEffect.cs` hunk is conditional on the separately confirmed interruption defect. SaveSystem, Objects.json, faction data, AIHoarder, ThrowItemCommand and general pickup/drop facade remain unchanged unless a concrete paired failure proves otherwise.

New focused core/adversarial fixtures and fresh metas; existing AIDebug/AIBehavior/M3 pin corrections only where the old explicit one-way/private-state premise changed. Later native helper/player/launcher/test files get exact paths and hashes in a separate manifest. Living doc will be `Docs/DENSITY-PET-RETRIEVAL.md`, copied from this reviewed plan and updated in the same eventual commit. Historical receipts stay immutable. Root owns publication, Unity, staging and main pushes.

## Original pre-implementation self-review (historical)

🟡 Confirmed source gaps: one-way retrieval, incoming identity loss under ordinary acquisition, private saved progress loss, under-validated landed source/recipient. Actual behavioral RED still required.
🧪 Native dog route and visual/feel acceptance pending; ordinary tool source established, no grant needed.
⚪ Explicit limits: legacy saved one-way goals lack recipients; arbitrary external source mutation and vetoed drop cannot be force-repaired; inactive scheduling cannot guarantee wall-clock delivery. Mid-recruitment stack burial is a grounded hypothesis awaiting its own RED and narrow scope review.


## Implementation and scope corrections

The candidate completes the actual landed-event loop with one exact carried object and a saved finite state machine. `PickupCommand.ForRetrieval` uses the normal command transaction, capacity/lift admission, vetoes, Taken/AfterPickup hooks and rollback. It preserves the landed identity by using a separate carried entry, keeping GoldCoin physical and skipping automatic equip only during retrieval. Once dropped, normal player pickup merges, converts gold and auto-equips exactly as before.

`GoFetchGoal` records the source quantity and current thrower, rechecks exact graph/part ownership before each action and after pickup veto callbacks, and uses the existing actor/contact-target path API. It follows the thrower's current physical body; return death/detachment/alliance loss or exhausted bounds produces a normal local drop only while the dog remains alive and attached. External payload transfers/quantity changes/equipping are never undone or stolen back. Two refused drops terminate with a diagnostic, leaving the held item intact. No generic OnPop inventory mutation was introduced.

`CurrentPhase`, counters, mode, exact item/thrower references and remaining bounds are public saved fields. Genuine old field-absent wire tests prove constructor defaults are not assumed. A legacy carried home fetch resumes home; a legacy one-way fetch still has no invented recipient. No save-format version, part rebuild, entity clone or world ledger was added.

| Reviewed plan or old assumption | Actual change and evidence |
|---|---|
| Preserve old outward child-loop shape for retrieval | The old default/hoarder child loop remains. Explicit retrieval recomputes existing `FindPath(...contactTarget)` each action, avoiding a stale moving-owner child. Its total outward bound is still2×100 actions; three consecutive unavailable paths terminate. Return bound100, drop bound2, age bound400. No gameplay RNG is added. |
| Recruitment insertion was conditional | Actual recruit-during-return RED confirmed a new persistent FollowLeaderGoal permanently buried retrieval. Root approved `BrainPart.PushFollowGoal`, which inserts only the new follow below an owned unfinished retrieval subtree. The same existing goals/order/ParentHandlers and callback counts survive; default, hoarder, finished/foreign-parent and dismissal controls pass. |
| A vetoed drop can simply be retried next turn | Actual adversarial RED showed the thrower can move after the first refusal. Drop retry now rechecks the current recipient and resumes walking before another drop. |
| Pre-command actor validation covers pickup callbacks | Actual callback RED showed a BeforePickup hook could invalidate the dog's Physics owner and still transfer. The command now repeats the same current live-member and exact ground-source checks after both veto hooks. Source-zone/item-backlink controls pass too. |
| Existing AIBehavior dog fixture represented a real PetDog | Its synthetic dog had HP/Brain/Inventory but no Physics. The first606-case differential exposed that false premise. Both matched fixture cohorts add the actual required physical component; no production guard was loosened. |
| Private-only debug field pin should remain | Actual extracted old-pin RED confirms the intentional conflict. The final existing fixture pins mutable public saved progress and unchanged GetDetails, rejecting a display-only property. Native full AIDebugTests remains an editor gate. |

The only production paths are AIRetrieverPart, GoFetchGoal, PickupCommand, InventoryPart, BrainPart and RecruitedEffect. Existing test edits are AIDebugTests, AIBehaviorPartTests and M3CoverageGapTests. Two new focused/adversarial fixture files each have a fresh meta. Exact current preimages/candidate hashes are in [candidate-manifest.json](Verification/DensityCompletion/Completeness/PetRetrieval/candidate-manifest.json). No SaveSystem, Objects.json, faction, AIHoarder, ThrowItemCommand or ordinary DropCommand changes are included.

## Test evidence and bounds

All receipts below are under `Docs/Verification/DensityCompletion/Completeness/PetRetrieval/` and the full per-case failures/counts are in `test-results.json`.

| Execution | Actual result | Meaning |
|---|---:|---|
| Current API before implementation |20:13RED/7controls | Missing return, resident merge, gold conversion, auto-equip, invalid source/recipient admission and private save progress are observable. |
| Initial candidate |20/20GREEN | Matched original feature/control cases. |
| Adversarial first pass |41:2RED/39PASS | Moved-recipient refused-drop retry and recruit-during-return burial were real failures. |
| Admission diagnostics before implementation |48:2RED/46PASS | Actual accepted/refused gates lacked records; then48/48GREEN. |
| Expanded old debug pin |58:1RED/57PASS | The deliberately replaced private-only inspector pin, with four genuine old-field-absent wire cases already passing. |
| Callback ownership probe |64:2RED/62PASS | One genuine actor-Physics callback ownership failure plus the still-extracted old debug pin. Both are identified individually in the raw XML. |
| Final focused unit |65/65GREEN |64 focused/recruitment/adversarial cases plus the exact saved-debug contract probe. Includes ordinary subsequent player pickup, competing dogs, offset footprint contact, callback mutation, thrown-transaction rollback, duplicate/repeated return and loaded exact identities/budgets. |
| Initial matched nearby cohort |606/606 baseline;605/606 candidate | One missing-Physics test premise, retained as evidence. |
| Corrected identical nearby cohort |606/606 before and606/606 after | Newly failing0, newly passing0 across12 existing AI/follower/recruitment/inventory fixtures. |
| Actual Unity-reference compilation | runtime0errors; five final fixture sources0errors | Full runtime and test reference compilation, not editor test execution. |

The private runner cannot compile two existing UI-dependent acquisition/transfer fixture files; they were excluded identically, while their adversarial command fixtures remain in the606. AIDebugTests depends on actual Unity LogAssert. The runner therefore executes an exact extracted single GetDetails contract method as `AIDebugSavedContractRunnerProbe`; the final full source still reference-compiles and is queued for native execution. This runner-only helper is a receipt, not an Assets candidate. Its earlier RED extraction retained the old method exactly; the final extraction contains the new persistence pin.

The runner patches hashing and is not evidence of Unity seed maps, native keyboard flow, animation, menu discoverability or ordinary combat survival. The proposed real generated PetDog/original starting Dagger route above remains pending. No generated-content success or native fetch is claimed.

## In-phase self-review

🟡 Fixed: source identity/quantity loss from ordinary acquisition, one-way return, lost private saved progress, under-validated landing graphs, callback-invalidated physical ownership, stale drop recipient and recruitment burial. Each has actual RED or matched current-API failure with positive/negative controls.

🔵 Ownership and symmetry: exact item transfers through the existing transactional receipt; ordinary acquisition is unchanged and explicitly tested again after return. Current live actor, actual inventory, source cell/Physics/Stacker/Handling backlinks, current recipient and exact quantity guard every relevant mutation. A competing dog, rolled-back throw or another real after-pickup owner cannot be reclaimed. No global owner cache, cloned stock or extra random draw is introduced.

🔵 Save/lifecycle: real entity-token replacement graphs preserve item/thrower/phase/budgets; explicit legacy absent fields retain bounded home/no-recipient policy. Follow insertion preserves existing goal instances and callbacks. Repeated completed turns leave one returned item. Drop refusal and wholesale external goal clearing are cancellation boundaries, not permission to bypass hooks.

🧪 Deferred — low priority: actual ordinary generated pet keyboard delivery/save/repeat. Independent final peer review and the 751-case native core/neighbor gate are complete. The attempted route passed setup/admission but not delivery; no motion or visual acceptance is claimed.

⚪ Deliberate bounds: old one-way saves do not contain a reconstructible thrower; dead/detached dogs and externally mutated payloads are not force-repaired. A vetoed local drop can leave an item carried after the finite goal ends. Suspended/inactive actors have no wall-clock delivery guarantee. These are explicit conservative outcomes, not infinite retry or hidden teleport behavior.


## Final peer-review repair and current readiness

Standalone's independent read identified a further pre-acquisition recipient seam: BeforePickup/BeforeBeingPickedUp can remove or kill the thrower, or change alliance, after the goal's initial recipient check. The actual69-case probe recorded3RED/66PASS. Retrieval-only PickupCommand now captures the exact recipient Entity and the configured alliance bool in ephemeral command fields and rechecks them before and after vetoes; no delegate, callback or new saved command state is introduced. The default public pickup path remains unchanged. An extra AlliesOnly=false counter proves that deliberate policy remains allowed with a live recipient.

Current private results:70/70 focused (69 feature/adversarial/recruitment cases plus the exact debug-method probe), and676/676 combined with the existing nearby cohort and final pin corrections. The earlier strict matched606 before/after differential remains newly failing0. Actual full runtime and the five final fixture sources compile with Unity's references. The compiler input was refreshed only to include root's newly published SteamContact dependency and current C2 sources; no unrelated source change is part of this candidate.

Root's actual primary20 native run `2a1f417dea554b78bf1a153cd686529f` confirmed13 intended failures and7 controls, archived as native20-red.json/xml.gz. It precedes any pet production publication. Root subsequently published the final package and the authoritative Unity XML reports 751/751 GREEN: 69 new cases plus 682 neighboring cases across 18 fixtures, 0 failed / 0 skipped in 4.244591 seconds. The original-dagger/generated-dog keyboard delivery/save/repeat gate is deferred at the user’s direction after the first bounded attempt described below. Standalone's final reread found no remaining concrete blocker in the bounded ownership/quantity/save/stack-insertion scope; this is a source review, not native acceptance.


## Actual native core gate

[native751-green.json](Verification/DensityCompletion/Completeness/PetRetrieval/native751-green.json) and its paired XML archive are authoritative. The MCP job metadata includes a delayed preceding initialization callback; the XML's exact eighteen-fixture list and all 751 case outcomes identify the actual run. Root recorded job `4fee313df4d54a2e810dff904b67a6cf`; an earlier initialization timeout with zero reported tests was not treated as a behavior result.

This closes native core/save/inventory/recruitment regression coverage, including the full existing AIDebug fixture that the standalone runner could only partially exercise. It does not establish keyboard fetch, a natural dog encounter, animation quality or all-seed viability. The private native route uses one labeled living-player approach to the actual generated seed64 glade PetDog, then only real throw, wait, move, pickup, F5 and F6 inputs with the original starting Dagger. No animal, item or simulation grant is planned.


## First actual keyboard route and grounded follow-up

Run `bb29379cffdc4b6e801377b8d047c997` passed ordinary startup, actual generated source, the labeled approach and native first-throw admission, then failed its 24-wait outward limit. The player remained40/40HP and dog15/15HP. The original Dagger remained on the ground and the same fetch remained active; it had not vanished. The archived log records the dog witnessing a nearby death and becoming shaken. Its real 20-turn WitnessedEffect pacing goal temporarily sat above the fetch. The diagnostic windows show three fetch steps before that interval and the fourth after it ended.

This is a finite harness premise failure, not evidence that retrieval lost the source. No gameplay priority or effect was changed. A proposed observation-only wait correction was not implemented or published. The user explicitly prioritized higher-impact ordinary-play failures over further dog-audit iteration, so this end-to-end gate is DEFERRED — LOW PRIORITY. The existing finite driver and failure receipt are retained; no accepted assertion or budget was relaxed. [First-run diagnosis](Verification/DensityCompletion/Completeness/PetRetrieval/NativeAcceptance/first-native-diagnosis.json) links the exact native observations; the successful core751 gate remains separate from this failed keyboard route.


## Checkpoint boundary after user priority steering

Included: the reviewed six-file retrieval/save-state/recruitment repair, its three existing test corrections and two new fixture/meta pairs; actual native primary20 RED, final751 GREEN; the isolated native driver/launcher and fresh metas; its two shared restoration-test rows and actual30-case restoration GREEN; the attempted route and exact scene/save/prefs restoration receipt. The failed native run established real generated dog/source access and actual original-Dagger throw admission only. It did not establish keyboard acquisition, return, mid-return save/load, pickup or repeat delivery.

Excluded: the later private Witnessed-suspension observer skeleton/test draft. It was never published and its incomplete fixture compile is not behavior evidence. No subsequent pet source or harness work is planned in this checkpoint. Dog fetch remaining low-priority evidence will not block the higher-impact audit.
