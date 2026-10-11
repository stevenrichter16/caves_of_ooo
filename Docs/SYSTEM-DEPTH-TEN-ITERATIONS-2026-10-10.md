# System depth: ten implementation iterations

Status: audit complete enough to select a bounded slate; implementation in progress.
Baseline: `1da5592c3` on main, 2026-10-10. This is an RPG with persistent characters and worlds.

## Audit and decisions

The review follows current player paths through combat, followers, trade,
inventory, liquids, cultivation, settlement conversation, and journal display.
Historical gap lists are evidence of intent, not proof that a gap remains.
The density tranches, quest stage dispatch, Stillleaf archive interactions,
equipment weapon families and many item utilities already ship. They are not
counted again. This is a prioritized audit, not a claim that every system or
Markdown file has been exhaustively verified.

References reviewed include `PROJECT-IDENTITY.md`, `CONTENT-ROADMAP.md`,
`ALPHA-READINESS.md`, `QUD-DENSITY-GAP-ANALYSIS.md`, `DENSITY-PHASE-1.md`,
`FOLLOWERS.md`, `QUEST-LOG-UI.md`, `QUD-ENGAGEMENT-AFTER-ITEM-UTILITY.md`,
the recent item utility and preparation documents, `CLAUDE.md` and the
standalone runner README. Exact code premises appear below and in per-slice logs.

### Verification sweep / corrections

| Earlier premise | Current code evidence | Decision |
|---|---|---|
| Missing natural DV, hostile population and loot | Density Phase 1 now documents completed tranches and native evidence | Do not repeat this work. |
| Followers already assist their leader | `FollowLeaderGoal` requires a leader `KillGoal`; human attacks call `CombatSystem` directly | Complete the human-player connection, preserving bounded perception. |
| Followers can simply move toward the player | Following uses greedy movement despite existing actor-aware route search | Use existing navigation around obstacles; test the full follow goal. |
| Stay is deferred | No persistent companion order prevents zone transit | Implement a complete command, persistence and transit slice. |
| Quest rows exist in the snapshot | UI clips excess rows and only notes can page | Make every quest/objective and history row reachable. |
| Shop keys are useful stock | Stock tables contain `IronKey`, whose `NoTrade` tag rejects purchase | Add an ordinary spare without weakening bound-key protection. |
| Merchants replenish low shelves | All held objects count, including unrelated player-sold junk | Count relevant stocked goods and retain bounded refill timing. |
| Compost is available from failed brewing | Authored reagent combinations produce no inert sludge and no ordinary source exists | Add a finite ordinary source and clue for the existing paid crop action. |
| Brine/mire are collectible | Their finite pools conflict with their own water coating projection | Reconcile source identity while preserving water-related world reactions and mixed-liquid refusal. |
| Donating the purification copy teaches the keeper | Conversation only consumes a tagged item | Make the promise produce persistent, relevant well care; reject inappropriate copies and repeat payment. |
| Non-damaging restraint is an attack | Frostbind applies its effect without the hostility used by harmful item actions | Provoke only on successfully committed hostile restraint; preserve party/cancel/veto cases. |

## Scope and content readiness

🟢 Existing ordinary merchants, crop beds, pools, recruited companions,
wellkeepers, spell learning and journal entries supply the player-facing paths.
🟡 Compost supply and spare key need content; use existing models and item
families, surgical JSON edits and exact changed-blueprint comparison.
🟡 Companion commands need both keyboard/world-menu and native controller reach.
⚪ No new firearm framework, universal liquid mixing, full companion equipment
manager, or expanded tinkering unlock. BitLocker remains dev-only by explicit
user direction. Misleading tinkering schematic availability and cloak AV
comparison remain separate audit findings, not silently declared fixed here.
⚪ These are CoO extensions to existing systems; no new Qud parity claim.

## Ten iterations and acceptance contracts

| # | Deliverable and player payoff | Required checks | Status |
|---|---|---|---|
| 1 | Buy ordinary spare keys from the shops that stock them; quest keys stay protected | Actual trade, correct locks, stock source, bound-key refusal | Planned |
| 2 | Returning to a shop can replenish its useful wares even after selling unrelated objects | Interval, matching stock, junk retention, bounded quantity, persistence | Planned |
| 3 | Find compost through ordinary cultivation supply and use it on new planted crops | Real source, exact payment, once-only growth benefit, water still required | Planned |
| 4 | Collect brine and mire in the flask and use their actual liquid properties | Authored pools, finite volume, mixed-cell refusal, rollback and reaction counterchecks | Planned |
| 5 | A suitable purification copy creates a lasting wellkeeper benefit | Exact copy eligibility, payment, persistent settlement result, refusal and repeat guards | Planned |
| 6 | Recruited allies recognize the player's committed melee attack | Human canonical attack path, miss/veto, hidden target, expiry/death and allegiance | Planned |
| 7 | Successful harmful restraint draws an appropriate response | Full command, payment/cooldown, hostile/party, effect veto and no-effect cases | Planned |
| 8 | Companions route around buildings and respect terrain costs | Full follow goal, obstacle/hazard/forced-route and unreachable cases | Planned |
| 9 | Tell companions to stay, leave and return, then resume following | Normal action menu, leader-only authorization, save/load, zone transit, dismissal | Planned |
| 10 | Read a long quest journal through its final objective and history entries | Wrapped rows, paging boundaries, notes independence, controller keys, no world turn | Planned |

Dependencies: iteration 9 builds on the follow behavior of 6/8; coordinate
changes to the shared goal file. Other slices can proceed independently.
Each iteration is a behavior milestone, not a count of tests or file edits.

## Implementation and verification protocol

For each slice: inspect current APIs/content; write and execute a failing
behavior test; implement; run green and counterchecks; review actor ownership,
payment, save/load and actual UI reach; record evidence here or a linked slice
log in `Verification/SystemDepthTenIterations`; commit the code and living log
together using CLAUDE §2.3. Never commit unrelated logs, package changes or art.

Isolated standalone runners may prove gameplay RED/GREEN without competing
for Unity. Native Unity compilation, targeted regression tests and a Play-mode
sanity sweep are final gates. Standalone stubs cannot establish rendering,
controller behavior, native serialization or play feel. An unavailable editor
or hardware limitation must be stated, never presented as verified.

After the slate: independent cold review, fix significant findings, six or more
cross-system probes if review finds no bugs, reconcile the audit/plan with what
actually shipped, fetch/rebase and push main under existing authorization.

## Implementation log

Pending. Per-iteration receipts will distinguish observed RED tests from
additional already-passing counterchecks and name unresolved limitations.
