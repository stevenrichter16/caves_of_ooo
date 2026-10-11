# Iteration 9 — saved companion stay/follow orders

Status: standalone RED → GREEN complete (64/64). Native input and arranged Play verification remain pending; root owns Unity.

## Contract

The recruiting leader can choose `stay here` or `follow me` from the actual companion's ordinary world action menu (keyboard or native controller). Staying keeps recruitment, party slots, reputation and exact entity identity, prevents autonomous following/assist and zone transit, and survives save/load. Returning and choosing follow resumes the same companion. Dismissal removes the order with recruitment. Commands require the live recruiter and follower in the same zone within normal one-cell reach, with living entities and matching party ownership; stale menu choices must revalidate.

This is the bounded opt-out promised by `Docs/FOLLOWERS.md` F.4, not a new Qud parity claim or a universal companion management screen. Stay holds position; it does not erase unrelated ongoing combat/effect goals or grant immunity.

## Source verification / corrections

| Premise | Source result | Implementation decision |
|---|---|---|
| Effects receive arbitrary inventory events | `Effect` has specific callbacks; `StatusEffectsPart` does not forward arbitrary events | Route menu discovery/execution from existing `BrainPart.HandleEvent` into a small companion-order helper. |
| A new Brain field automatically saves | `SaveSystem.SaveBrainPart` is specialized | Use one public boolean on `RecruitedEffect`, serialized by existing reflective effect fields; no save version change. |
| Removing recruitment should also clear order | Existing effect owns leader and follow-goal teardown | Store the order with that effect rather than a permanent entity flag. |
| World action menu always uses an inventory transaction | Its generic branch fires `InventoryAction` directly and costs no turn | Preserve that free order-command policy. Helper supports ordinary inventory transaction rollback when invoked there, and revalidates direct calls. |
| Transit checks follower intent | `TransitPartyMembers` checks source-zone membership and arrival capacity only | Skip exact followers that have a live stay order. |
| Resume can assume the follow goal exists | Existing timeout can remove the goal | Resume restores one matching follow goal when absent, without duplicating a retained one. |

Performance: one effect/party lookup before existing follow/transit work, no new frame loop or path cache. Menu work is on demand. See `Docs/PERF-FOUNDATION.md`.

## Test plan

Core RED/GREEN: discover and execute real world actions; alternate labels; exact recruiter authorization, adjacency, living/stale ownership, repeated command refusal; stay over many scheduled turns; leave/revisit without teleport; resume movement and transit; token-graph round-trip including exact recruiter reference; dismiss/recruit clears stay. Native root-owned tests will prove controller-reachable normal action menu and no world-time advance. Standalone cannot establish Unity rendering/input feel.

## Implementation log

- Recorded this plan and API corrections before production. This is a CoO integration of the existing recruitment contract; no new Qud parity claim.
- Observed `iteration09-red.xml`: all 15 initial behavior cases failed because normal companion actions were absent.
- Added `CompanionOrders` and routed existing Brain inventory events through it. The effect owns one saved `StayHere` boolean; existing reflective effect serialization preserves it, with old saves defaulting to follow.
- Orders use the ordinary context menu and remain free, matching its conversational action policy. Exact current recruiter, live adjacent world bodies, effect, roster, Brain and matching follow-goal identity are revalidated before commit. Invalid or stale commands leave the order unchanged.
- Following idles persistently while staying; zone transit skips that member. Resume restores a missing matching follow owner without duplicating an existing goal. Dismissal naturally removes the saved order with recruitment.
- Observed `iteration09-assist-red.xml`: 18 cases, 16 passed and 2 failed because a staying companion still joined a new fight. Added the one-line responder gate in `CompanionCombat.Rally`. A staying victim remains eligible to be defended by other following allies.
- Independent review found a legitimate transaction callback could replace the follower's Brain after the command captured its goal. Observed `iteration09-callback-red.xml` (1 failure), then pinned the captured Brain and matching follow goal in precommit validation. The replacement Brain remains untouched when the command is refused.
- Final standalone `iteration09-green.xml`: **64/64 passed** — 19 order cases, 10 navigation cases, 22 existing follow-goal cases and 13 existing recruitment-effect cases. Runtime: .NET 10.0.5 on macOS in an isolated copy of `Tools/EditModeRunner`; this is not native Unity evidence.
- Corrected the navigation fixture's `Random` construction to explicit `System.Random` after native assembly compilation exposed an ambiguity hidden by the standalone stubs. No behavior assertion changed.
- Added three native `CompanionOrderInputTests` cases using actual Input System events and the ordinary `InputHandler` world-action surface: keyboard/directional and LT+A/A menu routes, B cancellation, stale dismissal and free tick/energy cost. Execution remains pending.

## Self-review

- 🟢 Authorization is exact current recruitment ownership; nearby strangers, changed leaders, dead participants, removed bodies, missing effects and repeat commands are negative controls.
- 🟢 Actual leave/revisit/resume transit and token-graph save round trips prove retained entity/leader references; dismissal and rerecruitment return to follow.
- 🟢 Stay is a travel and new-assistance order. Existing unrelated combat, item-retrieval and effect goals are not canceled, and companions gain no immunity. This bounded behavior is documented in the public helper and avoids destroying goal subtrees.
- 🟢 Callback replacement and throwing observers have exercised rollback rather than relying only on the ordinary menu path.
- 🟢 No per-frame scan, new navigation cache, save version change, or independently saved duplicate order owner.
- ⚪ Native input compilation/execution and the visible walk/menu probe remain root-owned pending gates. The arranged Play probe is included with iteration 10; it does not claim natural recruitment acquisition or physical Deck feel.

## Files

`CompanionOrders.cs` and meta; `BrainPart.cs`, `RecruitedEffect.cs`, `FollowLeaderGoal.cs`, `ZoneTransitionSystem.cs`; the responder-only gate in `CompanionCombat.cs`; `CompanionOrderTests.cs` and `CompanionOrderInputTests.cs` with metas; the explicit Random fixture correction in `CompanionNavigationTests.cs`; this log and four XML receipts.
