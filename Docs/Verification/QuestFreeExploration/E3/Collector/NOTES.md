# F6 collector — bounded implementation and Q1–Q4 review

Status: private candidate only; root owns publication, data/composer/art and native execution. No Unity call, shared Assets write, staging or commit was performed by this unit. CoO-original behavior, not Qud parity.

## What changed

One `SpreadCollectorPart` owns a finite configured trip to an exact existing loose tool/boots owner and exact existing cache. It handles AIBored after ordinary higher-priority behavior; pickup, approach and deposit each consume an ordinary actor action without a child goal. Pickup uses private current-role capability, existing inventory transaction/snapshot and identity-preserving AddRetrievedObject. Successful completion events run after commit. Deposit delegates to the existing PutInContainer command; no separate transfer engine or generic hoarder/player behavior changes. Exact entity/scalar/enum public fields use the current save protocol. CurrentCarriedItem tells the truth about current positive non-equipped ownership, independently of strict original-duty admission and stopped state.

PutInContainer now retains player “You put” copy and names a visible NPC. NPC success/full/locked messages require both actual actor/container ground owners in current visible cells, visible owned Render parts and the associated manager's current zone when one exists. This adds no audibility model. Transfer, validation, amount, timing, claims, merge, rollback and diagnostics remain in their original order. It does not retrofit other generic inventory commands' NPC messages. Collector never sends an equipped item through this command.

Root owns the original Tatterjay blueprint and original art. Its NoRandomStock avoids minting Magpie trade stock on the replacement; that is an explicit stock subtraction, not neutrality. This part creates no loot or supplies. Ordinary death drops and LootDropSystem remain unchanged; the death control isolates carried-item spill and does not claim no ordinary death loot.

## Executed test-first evidence

| Evidence | Result / truthful boundary |
|---|---|
| red.xml | Initial28:27 missing-role API failures,1 ordinary no-role control PASS. |
| green-first.xml | Initial28/28 PASS. |
| adversarial-red.xml |47:44 PASS,3 genuine carry-query failures after party/passivity/foreign-zone duty interruption. |
| owner-red.xml |49:47 PASS,2 genuine missing/borrowed Inventory carry-query failures. |
| final-green.xml | First49 collector +23 neighboring Spread roles =72/72 PASS. |
| narrow-red-final.xml |89:75 PASS,14 genuine new failures:4 carry metadata/count/blueprint,2 foreign-role callback writes,8 NPC text/visibility. |
| narrow-green.xml |88/89; the remaining equipped negative was a fixture premise: equipping from3 split off1 while the original2 remained legitimately carried. Corrected negative uses an actual1-unit source and keeps real native equip. |
| invisible-red.xml |12 narration:10 PASS,2 Render.Visible=false leaks reproduced before guard. |
| final-package-green.xml |381/381:56collector,12narration,23Spread actor,284InventorySystem,6ItemTaken. |
| runtime-compile.log / tests-compile.log | Actual Unity-reference runtime and selected test compilation0 errors. This is compilation, not Unity test execution. |

The broader attempted neighbor selection did not compile because that private runner lacks existing DestroyImmediate and ScenarioContext.Verify stubs; neighbors-build.log is retained. No production defect is inferred and no stub work was expanded. A test-only AddRetrievedObject call was corrected to public AddObject plus exact identity check after the real test-assembly reference compiler refused internal access. Initial fixture compile failures (missing Data import and assigning read-only IsLocked) were corrected before assertion RED; lock tests use actual LockPart. No factory/manager/art changes are included.

The standalone runner is isolated, uses COO_REPO and stable GetHashCode substitutions already documented in its patch.py; its world RNG is not a Unity-seed equivalence claim. Tests use constructed exact owners and real commands/scheduler/save; they do not prove ordinary generated availability, visual quality, complete player flow, the collector's source receipt integration, or native timing. Root's separate five content cases are not counted in381.

## Q1 — symmetry

Pickup before-hooks revalidate the exact captured actor/role/home/item/configuration; completion hooks observe committed state. Deposit uses original transaction and claims. Failure stops only the still-owned role, and phase undo refuses foreign role ownership, preserving callback work rather than writing into another actor. Capacity partial-merge refusal, compatible full merge, independent postcommit drop and observer exception are paired. Presentation does not hide still-held goods merely because duty was interrupted or the goods changed legitimately.

## Q2 — cross-feature consistency

No general hoarder/GoFetch changes; no scans, RNG, item minting, faction/HP/stat/loot grants. Saved phase does not require transient animation state. Loaded Seeking/Carrying/Deposited/Stopped graphs use replacement entity references. Current actor/body/inventory/Physics membership governs visible carried identity. Player deposit wording and ordinary transaction/diagnostic paths remain unchanged; NPC feedback uses existing current FOV/visibility conventions, with no remote cached-zone leak.

## Q3 — counter/adversarial checks

Exact whole-source acquisition versus special/currency/unsupported/current-owner refusals; no autoequip versus actual equipped refusal; same-stack resident inventory, full merge and partial-capacity refusal; before hook veto/quantity/actor/home/foreign transfer/throw; postcommit drop/throw/reentrant AIBored; role moved to foreign actor on before-hook or deposit observer; current carry wrong owner/inventory/body/death/removed owner; actual death drop once; competing collectors; finite movement cap; higher goals and recruited actor; replacement save graphs in all states. Player/visible NPC/unseen/fog/inactive/detached/Render-hidden deposit controls preserve actual transfer outcome.

## Q4 — source and claims

The role alone is not generation authorization: root composer must prove actual ambient/loose/cache receipts, exact current geometry and final validator before Configure/commit. Header and PLAN name this limit. Pickup completions intentionally occur after commit, unlike ordinary acquisition command callbacks; this prevents a failed completion observer from duplicating already transferred goods. NPC narration remains precommit exactly where the original message was, so observer exceptions still roll back through the current engine. Direct GetDisplayName is pure current-source reads (Entity.cs467–480); a proposed name-event callback hypothesis was withdrawn after reading the actual method. Native execution and source/art integration remain required, not manufactured from core GREEN.
