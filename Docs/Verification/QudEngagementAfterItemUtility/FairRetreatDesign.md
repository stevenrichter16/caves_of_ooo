# D1 readiness: bounded, fair retreat before freezing cover

Prepared 2026-10-09 during coordinator-owned graphics/ordinary-Play release gates. Read-only source audit plus scratch test draft; **no Assets changes, production changes, Unity calls, native RED/GREEN, commits or completed gameplay claim**. The current A/B milestones remain the release priority.

## Decision

Ship fair retreat as its own small D1 before the freezing-cover role. This fixes an existing player-visible inconsistency today: after a wounded enemy has seen the player, an intervening wall or smoke can conceal a detour from pursuit but cannot conceal that detour from its current retreat direction. The same rule will then make the future opponent's own cover credible. Do not hold a completed fairness fix for a more complex throwing/source/balance feature.

D1 changes only `FleeGoal`, new native tests/metadata, and the living document. No new inventory Part/blueprint, population or source enrichment, cover throwing, projectile/gas changes, Kill/Bored behavior, save schema, path planner, skill or generic perception framework. The existing movement helper still determines practical routes. Classify the new memory as **CoO-original, consistent with CoO pursuit**, not literal Qud parity.

## Source corrections and verified integration boundaries

| Premise | Actual source | Consequence |
| --- | --- | --- |
| Retreat already knows only what it has seen. | [FleeGoal:38–55](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/FleeGoal.cs:38) treats first, then reads `GetEntityPosition(FleeFrom)` unconditionally for movement and cornered melee. | This is the concrete gap; no hidden-location query may choose the new retreat direction. |
| Copying Kill requires a new common framework. | [KillGoal:24–28,65–85](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs:24) already owns six-action observed-contact memory and clamps bad saved budgets. | Implement the same small local policy in Flee. Sharing a complex base now would enlarge the regression surface without helping this one repair. |
| Low-health Kill directly creates Flee and transfers knowledge. | [KillGoal:105–113](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs:105) abandons and fails to its parent. [BoredGoal:75–103](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs:75) subsequently acquires an actually visible hostile and creates Flee. | **Preserve this.** D1 does not invent a memory handoff, immediately retreat a just-hidden injured attacker or refactor the stack. A specific control pins the existing path. |
| Only initial construction must initialize memory. | [SaveSystem.SaveGoal/LoadGoal:1867–1894](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Save/SaveSystem.cs:1867) writes named public fields and bypasses constructors. Missing fields in old streams stay zero. | New fields must be public mutable and all-zero must mean no observations. No constructor-default budget may fabricate old knowledge. No save-format bump or SaveSystem edit is needed. |
| A point or actor anchor is enough to record sight. | [AIHelpers.TryGetVisibleTargetCell:100–123](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/AIHelpers.cs:100) enumerates native occupied cells without allocation and returns an actual visible body contact. | Record the returned contact, not the target's hidden anchor. Use the same helper and the brain's SightRadius for updates. |
| Being invisible to the player means invisible to the enemy. | The same sight helper uses real body contacts, distance and LOS; it does not use `Cell.IsVisible`. [HasLineOfSight:54–95](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/AIHelpers.cs:54) checks intermediate solid/opaque cells and allows endpoint contact. | Player FOV gates only text. Adjacent contact in mist stays observable and can support cornered self-defense. |
| Retreat is already sophisticated pathfinding. | [AIHelpers.TryStepAway:457–472](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/AIHelpers.cs:457) is greedy away movement plus two diagonal-axis fallbacks. | Keep that helper. Do not claim D1 adds route optimization, danger scoring, safe-tile selection or anti-loop navigation. |
| Max age counts successful steps. | [BrainPart:674–712](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/BrainPart.cs:674) removes finished goals before incrementing ages and executes newly pushed children in the same opportunity. [FleeGoal:23–29](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/FleeGoal.cs:23) uses `Age > MaxTurns`, plus strict `ShouldFlee`. | Keep the existing age/health boundary. The independent unseen allowance counts goal opportunities, including blocked moves and successful treatment. Do not re-charge turns or dispatch BeginTakeAction. |
| Stun must decrement the new counter. | [TurnManager:340–367](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Turns/TurnManager.cs:340) blocks at BeginTakeAction, ends that turn and never sends the AI TakeTurn. | A blocked scheduled turn spends native energy/effect duration but does not execute Flee or age its memory. Keep this existing control behavior. |
| All retreat-like goals need changing. | [FleeLocationGoal](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/FleeLocationGoal.cs:6) and [RetreatGoal](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/RetreatGoal.cs:7) use authored safe waypoints, not a live threat direction. | Leave both unchanged. This audit is specifically entity-repulsion FleeGoal, not every environmental guardian/grazer policy. |
| Qud's source promises this exact memory policy. | Local [Qud Flee:94 onward](/Users/steven/qud-decompiled-project/XRL.World.AI.GoalHandlers/Flee.cs:94) reads the actual target cell, offers retreat abilities and compares candidate-cell danger/navigation; it does not implement this CoO six-action record. | Correct the Flee summary's broad parity wording when editing it. Do not copy Qud code, enemy content or a larger planner. |

## Smallest proposed contract

Retain constructor `(Entity fleeFrom, int maxTurns = 20)`, `FleeFrom`, `MaxTurns`, `CanFight() == false`, exact normal health/age completion and existing treatment priority. Add saved public fields:

```csharp
public const int MaximumUnseenActions = 6;
public bool HasLastSeen, RetreatingUnseen, Abandoned;
public int LastSeenX, LastSeenY, UnseenRemaining;
```

`HasLastSeen == false` is authoritative regardless of coordinates or positive saved count. A new or old zero-state goal facing a hidden target abandons safely instead of consulting its live position or moving away from `(0,0)`. No constructor or load hook seeds observation. A visible normal Bored→Flee child observes and moves immediately as before.

TakeAction ordering:

1. Guard missing owner/brain/zone/current actor/threat and already-abandoned state. Preserve the scheduler's existing age/health completion boundary rather than adding a second after-increment age test that silently changes its last allowed action.
2. Ask `TryGetVisibleTargetCell` once. Visible: record returned cell, set HasLastSeen, refresh allowance to6 and end RetreatingUnseen. Hidden: require recorded in-bounds coordinates and positive allowance; clamp to6, announce the first lost-sight transition, then decrement once. Neither branch reads hidden current coordinates.
3. Run existing emergency water, then medicine. A committed treatment returns and replaces the entire opportunity. Hidden memory has already aged; if this used its final unseen opportunity, mark the goal abandoned after the committed action. Do not continue into movement/attack. Refused treatment falls through normally.
4. Use the recorded contact for `TryStepAway` in both visible and hidden branches. If blocked, fight only with **currently observed** actual adjacent contact (`SpatialQuery.Distance == 1`), never merely because the historical coordinate was adjacent. Existing mist endpoint contact remains legal. The native attack API does not itself impose a universal range gate; this caller must not attack a distant hidden target because a remembered cell is next to it.
5. After the final hidden opportunity, abandon immediately. Finished includes Abandoned. Clear Brain.Target only if it still equals this FleeFrom, and retain faction/personal hostility. Another goal's target must survive. Subsequent Bored can reacquire the threat if it really becomes visible again.

Keep cleanup narrowly aligned with existing Kill: popping/clearing this Flee removes only its matching current target, and never cancels an independent already-paid commitment. Do not mutate a newer target or global AI state because an old goal finishes. GetDetails retains the existing `from`/`age` text and can append remembered contact and allowance while unseen. No saved zone-key or cross-goal authority framework is introduced; normal disappearance from CurrentZone already ends the local goal. Cross-world/simultaneous teleport transfer is not newly supported by this slice.

Diagnostics: record `RetreatLostSight`, `RetreatReacquired`, and `RetreatAbandoned` with reason, lastSeen coordinates and remaining allowance under `ai`. Gate text through the existing [MessageLog.AddObserved](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Events/MessageLog.cs:54); the first two transitions should be readable once, not spam on every unseen step. Use modest language such as “loses sight of … and keeps retreating” and “sees … again.” No marker or unseen-target reveal. No new per-cell scan or per-turn collection is needed; scalar updates and existing allocation-free sight query only. Transition diagnostics must use the existing channel guard.

## Prepared RED draft

`/tmp/coo-fair-retreat-tests.cs`: **239 lines,31 native cases**, fixture `CavesOfOoo.Tests.FairRetreatTests`. Not compiled or executed. It uses the existing FieldMedicineFixture, actual Brain TakeTurn, native movement/LOS, real supplies/effects and token-graph serializer. Reflection only accesses the absent public memory fields, so the test assembly can compile before production. There are no production mocks or invented helper APIs.

| Invariant / counter | Draft cases |
| --- | --- |
| Two opposite hidden detours both leave the enemy fleeing west from the same previously observed contact; visible detour instead changes its direction. |3 |
| Fresh goal and constructor-bypassed old zero-state goal cannot invent hidden knowledge. |2 |
| Six hidden actions expire; a blocked attempt also spends its allowance; visible reacquisition refreshes it. |3 |
| Player FOV affects only feedback; transitions emit once. |3 |
| A visible large-body contact is recorded instead of a hidden anchor. |1 |
| Cornered adjacent mist contact still attacks; a remembered adjacent coordinate cannot authorize attacking a now distant hidden body. |2 |
| Real paid water and medicine each replace the action and spend one memory opportunity; a scheduled stun does not execute/age the goal. |3 |
| Actual entity/goal token-graph save preserves identity, age, contact and remaining budget, then resumes movement after real fixture placement. |1 |
| Nonpositive saved budgets refuse(3); oversized budgets clamp(1); out-of-bounds saved contact refuses(4). |8 |
| Expiry preserves another goal's target. |1 |
| Existing strict health/age and removed-threat completion remain. |2 |
| Actual Bored acquisition still immediately creates/executes Flee. |1 |
| Existing hidden low-HP Kill→Bored behavior stays unchanged, with no invented Flee handoff. |1 |

Expected from source only:26 RED and5 already-passing controls; **actual native XML must decide**. Several new-contract tests necessarily fail first on absent fields; the two hidden-detour and two zero-knowledge cases additionally assert the real incorrect movement before any field lookup. “Legacy zero-state” tests constructor bypass plus absent initialized state, not a fixture claiming to contain historical serialized bytes. The save/resume case separately exercises the production token graph and native load hooks. Synthetic low-HP bodies isolate this policy; these tests are not proof that a human observed an ordinary retreat encounter.

Before import, root may choose clearer equivalent field/diagnostic names; then update the draft and plan together. Add fresh metadata when moved into Assets. Run native RED before production; fix only confirmed contract failures. Do not turn fixture/compiler correction into evidence of behavioral RED. After GREEN run GoalStackTests, AIDebugTests, Phase6GoalsTests, CombatInventoryPursuitTests/AdversarialTests, FieldMedicineTests/AdversarialTests, the finite tactical-water cases, and Tier1BrainPartTests. Native affected coverage suffices for this bounded goal change; do not restart unrelated long art suites.

## Ordinary acceptance and stopping point

After the current release gates finish, use one real wounded retreating opponent. Observe it once, break LOS with existing terrain or an obtainable cover item, and compare two hidden detours from the same starting save. Its steps must remain tied to the recorded position while hidden; showing yourself again may redirect it. Include a save/reload while memory is active. If an ordinary route is unavailable without widening source/content, keep the native controlled matrix explicitly labeled and add a bounded scenario rather than claim ordinary evidence.

Can verify in native tests/scenario: exact actor positions, observed contact, budget, one-action treatment, preserved graph references, save fields, target cleanup, control scheduling and message visibility. Cannot verify from those assertions alone: whether six actions feel right, whether the player notices the behavior, or whether the existing greedy route looks intelligent in every layout. Those are a short observational check, not a reason to expand into a planner.

Stop after D1's observed-position invariant and preserved counters are native GREEN and its limits are recorded. Resume D2 freezing cover only after separate source, safe-impact, transaction, one-use stock and attainable ordinary retreat-window checks. D1 is independently useful; no promise that the entire remaining Milestone D ships in this turn.
