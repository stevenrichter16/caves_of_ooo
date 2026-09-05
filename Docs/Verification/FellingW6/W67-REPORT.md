# W6.7 close-out verification — complete

Baseline: `9c4f44f4`, 7614/7614 tests, W6.6 native12/12. Scope and the
pre-implementation corrections table are in `Docs/FELLING-W6-PLAN.md`.

## Corrective conversation gate

The original W6.4 pre-draft voice gate was not recorded. The W6.7 intent card
was written before revision, then an independent fresh-context reader saw
only the existing unlabelled elder/tender prose and a neutral eight-culture
choice list. This is a corrective gate, not a retroactive original pass.

Original verdict, summarized from the independent report: both speakers were
assigned to culture E (underground kin/light/tended hearth), at moderate
confidence; fungal culture F was the strongest alternative. Cultural markers
did more work than voice, and kin were largely absent. The intent bounds
failed: the prose leaned on tutorials, exposition, solemn maxims and jobs
rather than people. No humor was found. Stronger moments were the elder's
respect for the guest's private dream and the tender's manual work/apprenticeship.

Specific failures: the trust speech was a maxim followed by a quest checklist;
rest was a sequence of mechanical instructions; the opening/wall/bloom leaned
on lore explanation; the tender's “service” line sounded doctrinal, its
“elsewhere” passage explained cultures to the player, its threat summarized
a faction rule, and “no second payment” exposed progression bookkeeping.

Resolution: rewrote the failing prose once, without regrading. The elder now
makes room, minds a guest's sleeve near a lamp, gives directions in ordinary
speech and offers private time after waking. The tender works on a row her
mother taught her and thanks the guest for staying. The required directions,
consent, danger warning and hearth consequences remain understandable. All
conversation IDs, predicates, actions and choices are unchanged. There is no
claim that blind recognition, humor or subjective voice quality was proved
for the revision; that would contradict the no-regrading gate.

## Combat and habitat repair

`W67-content-repair-red.xml.gz`: zero compile errors,13 checks,10 expected
failures. Actual melee recorded1d2 fallback/fists for both snakes; actual
population placed CascadeFather away from spray and on a pool-free map.
Three counterchecks already passed. The minimal repair gives only the two
snakes supported Simple anatomy plus the opt-in natural-attack tag, and adds
an exact SprayPool predicate to the existing cold population habitat hook.

No runtime hot path is added. CascadeFather placement is constrained at
creation; ordinary later wandering is unchanged. Existing sprites remain.

## Cross-wave hypotheses and review corrections

The dedicated file combines migration/discovery, actual harvest→conversation→
saved rest, stale underfoot menu actions after barrenness, saved sentinel
cover removal, passage unload/save/travel, site generation order and the
boundary below authored floors. The first sweep confirmed the saved reverse
index defect before automatic repair could hide it. Unloading now compares
complete connection values using the same identity as duplicate registration.
No save-format change is involved.

Fixture corrections are recorded separately from game defects: TurnManager.World
is static (the initial fixture compile failed and no stale XML was accepted);
the seventh blueprint is SeventhPosition; injury changes Hitpoints.BaseValue,
not the stat's persistent Penalty field. The first harvest/rest hypothesis
correctly refused nearby generated snapjaws. Safe cases now explicitly clear
those threats; paired unsafe cases retain a nearby snapjaw and require refusal.
The existing through-wall safety rule is preserved. The generated Cascade test
now owns/restores narrative state and checks healthy/damaged ecology, avoiding
a prior fixture's world flag contaminating its nonvacuous species count.

A second real finding is missing return stairs when the zone below an authored
floor is generated first. Both Olderdeep and Stillleaf reproduce it. The
preexisting top-down guard covered only depths1–2; depth3 exists first without
the incoming edge that StairsUpBuilder consumes. Repair is pending; do not
call the close-out complete until its regression and preservation controls pass.

Metadata audit at this checkpoint:2304 unique asset GUIDs, zero collisions.
No new art is needed for these corrections.

Stair-protocol RED40 checks:30 passed and10 failed, covering both actual
site boundaries, deep-first floors3/12/1000, saved coordinate remapping,
cached missing counterparts and one-way travel. The next expanded run is
109/109 GREEN, including existing SinkholeStack, UndergroundGeneration and
SealedLibraryRules coverage. A final six-hypothesis extension checks saved
deep-first parent regeneration and world-clock versus owner-effect timing.

Independent post-fix review found no additional player-facing defect. It
traced saved pending edges, source unload recovery, remapped source coordinates,
surviving cached child markers and both removed-marker refusal paths. Bounds:
direct reruns of StairsDownBuilder are not an idempotence API (production
retries clear fresh terrain); multiple-route selection is not newly claimed.
Previously cached floors missing their stairs are not auto-restamped. No
recursive ancestor materialization was added. Native evidence follows.

Final hypothesis gate46/46 passes (the separate scenario assertion was the
intended14-versus10 RED before adding its four travel checks). Full suite
first run7673/7674, zero C# errors: only the user-recorded fungal self-cloud
flaky test failed. Raw failure is retained as W67-full-known-flake.xml.gz.
Native execution and a full repeat follow; no fungal gameplay change is
being attributed to the W6 patch.

## Native evidence and honesty bounds

Run c05c31497fc04128993300cc7ca6a90f, native launcher exit0, zero C# errors.
All14 deterministic checks pass: two authored snake attacks after body
maintenance, spray/standing-water population controls, two site round trips
and two removed-return refusals, plus the six existing retreat/nest controls.
The following native keyboard-wait workload records75.001322583seconds,
73265frames,4970ticks and zero capture failures.

Raw evidence: W67-live-profile.json, W67-live-frames.csv.gz and
W67-active-metrics.json. Active scheduler mean2.0275ms,p994.6049ms,max11.5943ms;
retreat mean0.05565ms,p990.08492ms,max0.097875ms. Renderer active mean0.7166ms,
p991.1131ms,max9.7641ms. Whole-editor GC is retained without feature attribution.

Can verify: native script-observable combat rolls/effects, population placement,
actual world generation and transfers/refusals, AI scheduling, native input
rounds and the recorded warm-workload CPU samples. The new cold worldgen
operations occur before profiling; this is not a measured cold-generation
speedup or a direct attribution of the whole workload to this patch.

Cannot verify: art readability, composition, motion feel or sound from the
headless run. No new visual feel claim replaces the already-recorded live
look-pass debt. Previously cached floors with missing markers remain intact;
the safe refusal does not pretend to infer whether removal was deliberate.

## Final gate

Full suite **7674/7674 GREEN**, zero C# errors,20:49:39–20:51:08UTC.
The raw final result is W67-full-green.xml.gz. All60 new checks and the
existing suite pass after the last production/native scenario change.
No new player-facing defect remains from the two review angles. The
implementation log and daily ledger ship in the same commit as the fixes.
