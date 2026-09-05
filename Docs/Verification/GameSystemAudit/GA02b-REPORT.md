# GA02b — single-unit handovers and required dialogue outcomes

Status: COMPLETE. Full7866/7866 GREEN,22:36:28–22:37:58UTC; zero C# errors.

## Outcome

A17/A18: repairs, teaching and donations consume one positive carried unit.
Required action refusal keeps the current dialogue node and prevents later
reward actions. Teaching checks/spends its required copy before improving the
site. Full-pack gifts and grimoire copies use checked ground delivery, and two
quest offers now deliver cargo before starting the quest. Gift/copy prose is
accurate for both carried and ground delivery. Positive-unit predicates match
execution, and handover messages describe one unit rather than the remainder.

The new TryExecute/TryExecuteAll API reports required action outcomes; legacy
void Execute/ExecuteAll retain their established contract. Current22 authored
required-action lists each have exactly one required action first. This is
ordered refusal, not rollback of arbitrary earlier effects or StoryletPart
transitions. CoO keeps donation consumption, carried-list selection and checked
ground delivery. Qud's local TakeItem source corroborates quantity1 and blocking
the success element when required quantity cannot be supplied; no NPC-receipt
or comprehensive Qud-parity claim is made.

## Verification evidence

Every run first checked its compiler log: zero C# errors.

| Gate | Raw archive | Result |
|---|---|---|
| Initial RED | GA02b-red.xml.gz |23:15 failures,8 controls;22:07:11UTC |
| Expanded RED | GA02b-expanded-red.xml.gz |31:19 failures,12 controls;22:09:57UTC |
| Minimum GREEN + neighbors | GA02b-green.xml.gz |118/118;22:13:45–46UTC |
| Adversarial RED | GA02b-adversarial-red.xml.gz |62:56 pass,6 fail;22:18:41–42UTC |
| Corrected adversarial GREEN | GA02b-adversarial-green.xml.gz |62/62;22:19:59–22:20:00UTC |
| Native staging RED | GA02b-bench-red.xml.gz |6 missing-type failures;22:27:05UTC |
| Final focused | GA02b-bench-green.xml.gz |68/68;22:30:19UTC |
| First full | GA02b-full-fungal-flake.xml.gz |7865/7866; only pre-recorded fungal self-cloud flaky failure |
| Final full, unchanged repeat | GA02b-full-green.xml.gz |7866/7866;22:36:28–22:37:58UTC |

New cases:31 regression +31 dedicated adversarial +6 scenario/diagnostic =68.
Adversarial cases cover zero-first stacks, save token graphs, required-versus-
legacy action sequencing, registration/reset, placement refusal, real courier
payment ordering, teaching boundaries/free caretaker control, outcome records,
nested result isolation and postcommit retry. Many are already-correct pins;
they are not31 separate bug discoveries.

The adversarial RED contained one real new prose failure: Palimpsest said the
gift was in the player's hand after feet delivery. Five failures were incorrect
fixtures: the courier body is unique/nonstacking, and FlowerField has zero
weight. Corrected courier cases use one authored body; the zero-count case
explicitly adds a malformed extension stacker. Generic vegetation-delivery
controls use an already-over-capacity pack to actually reach ground placement.
They do not claim a normally carried flower or authored stack of courier bodies.

## Native evidence and honesty bounds

Final report: `GA02b-native-green.json`, run
`71429a9c069d48cf9c57d7831e8f1929`: **31/31 PASS**, zero failures,
7.724744625seconds, batch exit0. Matching raw log is archived.

Actual keyboard C/direction opens each NPC's world menu; arrows/Enter select
Chat and actual authored choices. Full-pack Scribe copy lands at the player's
feet, retains the original and literal KnowsMendingRite. Farmer repair spends
FireClay3→2, preserves the guide, stabilizes the oven and grants its first+10
reward. Repeating repair emits a fresh exact rejection record, preserves every
site field/quantity/reputation and produces no additional site/dirty event.
The dialogue remains cancellable. Reflection only observes private UI state.

The first native run (`GA02b-native-queue-red.*`) reached ten successful
observations then failed because the driver attempted to drain announcements
while dialogue held them queued. Production displays them after dialogue ends.
The corrected30-case run is retained as `GA02b-native-queue-control.*`; the final
31-case run adds proof that the repeat Enter was actually dispatched, avoiding
a false pass if input were ignored. Knowledge checks were also tightened from
equality to the literal authored property.

**Can verify:** native keyboard/menu routing, real content identity, delivered
copy location and knowledge, unit payment, stage/reward/refusal/cancellation,
diagnostic evidence and batch completion. **Cannot verify:** rendered pixels,
visual feel, native spell-class copy/study, missing-ground handouts, courier
completion or Warden teaching (the latter paths have EditMode coverage).
Manual menu cleanup has source review, not a separate GUI execution claim.

No ordinary per-frame/per-turn production work or new save field was added.
The temporary audit coroutine restores input settings/device, diagnostic
preference and callback subscriptions. The launcher uses an isolated save slot
and restores its prior preference; no performance-improvement claim is made.

## Cold-eye review

- 🟡 Fixed: actual quest offers started before cargo delivery; reordered costs.
- 🟡 Fixed: Palimpsest hand-location prose after ground fallback.
- 🟡 Fixed: native announcement timing assumption and vacuous refusal check.
- 🔵 Fixed: knowledge equality permitted empty values; now exact authored value.
- ⚪ Kept: void action compatibility, donation semantics and free caretaker.
- 🧪 Boundary: arbitrary mutation inside precommit MessageLog callbacks is not
  a general transaction contract; no production listener beyond logging was
  found. Postcommit retry and nested result isolation are explicitly tested.

Independent taxonomy/content-Qud and native reviewers cleared all must-fix
findings after corrections. Root reviewed all production hunks, ownership and
quantity symmetry, actual authored chains and documentation. GUID audit:
2318 unique GUIDs, zero collisions. All38 owned paths were absent from the
1027-path preexisting/concurrent-work snapshot; no shared dirty file is adopted.
