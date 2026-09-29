# Controlled native baseline pursuit — independent review

Run `b6c113bf59b24d58ad9b71c75e933204` establishes the intended native pursuit failure on unchanged FindPath. It finished after 8.7862644 seconds and 31 validated paid inputs: one east pull and 30 waits. Nine checks passed, then `ordinary_pursuer_fairly_uses_bypass` failed. The report's failures=2 reflects the failed check plus captured fatal condition, not two independent gameplay defects. This is distinct from the earlier serialization, first-frame visibility and wrong wait-key observer failures.

## Scene and current authority

The isolated controlled graph is explicitly staged, not a natural site: 2,000 real factory Grass owners, eight Hedge owners at x37/y8–16 except the y12 throat, one FallenBeam initially (36,12), one ordinary MarlbackScrabbler (33,12), and the unchanged original player transferred to (37,12). Native entry added no extra owners. The queue records original player2698 at1000 energy and pursuer4714 at0; no observer turn/goal/path grant occurred.

Initial FOV was false before the first render boundary, while current owner, Render.Visible, presenter identity and represented owner were true. The subsequent normal end-of-frame capture allowed unchanged visual checks to pass. The actual submitted beam was `ring-fallen-beam-2`, quarter-turn0, one approved palette piece; the pursuer was `ring-snapjaw`. This is the corrected observation timing, not a renderer production repair.

Actual C/direction/G grab and release were free. The paid east pull placed player(38,12) and the same load(37,12), closing the throat. Recorded drag speed76 returned to ordinary100 after release. Each subsequent wait rechecked exact same beam and hedge identities, positions and authored facts. The barrier was physically blocking without the Solid sight tag; the actual LOS control passed. Read-only physical eight-direction BFS measured10 steps to the player and was never supplied to the pursuer.

## Real scheduling and goal ordering

Every one of the31 paid windows contains an exact player action marker, one completed native turn receipt, and one Begin/End pair for the original pursuer. The east pull advanced tick10→24 at speed76 (1000 +14×76 −1064 =1000 action cost). Thirty ordinary-speed waits each advanced10 ticks with unchanged1064 player energy, ending tick324. PlayerHP40 and pursuerHP15 remain unchanged throughout. A Dawn→Height band change at tick304 is ordinary time progression, not interference.

The actor acquired the original player during the pull and moved33→34; wait1 moved34→35, wait2 moved35→36. These are the only three recorded native moves, all unforced, adjacent and physically legal. It remains(36,12) for waits2 through30, never crossing x37 beyond either hedge end or reaching contact at(38,12).

The prior source-review caution about retained versus active KillGoal does not undermine this run. `BrainPart.GetGoalsSnapshot()` preserves list order (BrainPart.cs474–476), while the last entry is active (459,668). Every post-pull and post-wait snapshot contains only `[BoredGoal, KillGoal]`, with KillGoal last, its target equal to player2698, and currentPursuit true. KillGoal Age progresses0→30, with no overriding goal, target switch, conversation, death or additional actor. The pursuer demonstrably receives turns and attempts ordinary pursuit rather than being unscheduled or diverted.

## Exact comparison inputs and pixels

Root's `baseline-ready-inputs.json` and `candidate-inputs.json` contain the same11 recorded paths. Ten hashes are identical; only `FindPath.cs` changes from `a17334766c9cf75dcd535d7cc09024774672ece05c28737d99297daca493f724` to `968ce0542beb732fc9adbebcaceb9ec659d4745aca9a2316cebe97d83478fddb`. The observer partial is identically `f11f38b6fc78c45d134f7f7d9c082e61b676c7da3d0d2b4cd145151875cd84ec`, and current launcher/core/partial hashes match that candidate record. This verifies the recorded comparison set, not an unrecorded whole-repository equality claim. Candidate Play is still pending this review.

Individually viewed01-before-grab,03-released-obstruction and99-failure. The small native beam visibly moves into the hedge opening; the original player is on its east side and the pursuer subsequently sits immediately west. Open space around both hedge ends remains visible. Haul text, actual grab/release log and HP40 are readable. These stills support the geometry/state report; they do not independently prove the31-turn duration or any tactical benefit. No art change is warranted.

Root's baseline restoration receipt independently reports playing=false, no active isolation/observer, save root/preferences restored, and Main/SampleScene clean. No shared source, Unity or Git changes were made for this read-only review. This proves the bounded controlled baseline defect only; ordinary discovery/frequency, LOS cover, net defensive advantage, save/load and fixed-run success remain outside this result.
