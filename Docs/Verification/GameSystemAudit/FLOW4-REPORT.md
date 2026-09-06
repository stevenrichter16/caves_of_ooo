# FLOW4 — split-item identity and supported-save repair

Status: COMPLETE. Baseline8689tests, HEADa97e6dd6; final8766/8766GREEN.
CoO-original repair; no Qud parity claim. Source plan/corrections are in
Docs/MECHANICS-FLOW-SMOOTHING-2026-09-05.md.

## Behavior and implementation

A weapon split by equipping, dropping part of a stack or throwing one unit used
to receive no ID. Direct reference operations could still work, but station
CraftToggle rows and individual ground-pile rows omit empty IDs. CloneForStack
now allocates GUID-N before Part.Initialize. Source identity is unchanged.
WeaponCraftingOperation no longer replaces that new ID after initialization.

LoadEntityBody repairs only null/empty saved IDs, before any post-load hook.
Every nonempty opaque ID is preserved. Token references still restore owner,
equipment, brain and turn aliases; unread placeholders remain empty. No fields
or version changed (v7). The writer does not mutate the old source. Two independent
loads of old empty-ID bytes may allocate different IDs; saving the repaired state
establishes stable identity for later loads. Duplicate nonempty IDs are separate.

## Raw evidence and corrections

All runs check compiler errors before parsing fresh XML; MCP starts and settles
before Unity. Times are UTC, durations from raw NUnit XML. Failed evidence remains.

| Run/archive | Total/pass/fail | Time / duration | Meaning |
|---|---|---|---|
| FLOW4-red.xml.gz |32/13/19|05:19:28–05:19:30;1.7655235s|18 missing-ID cases;1 incorrect reforge out-parameter assumption |
| FLOW4-corrected-red.xml.gz |32/14/18|05:24:57–05:24:58;1.8201597s|Clean missing-ID assertions before production change |
| FLOW4-minimum.xml.gz |100/100/0|05:25:56–05:26:00;4.3057709s|Regression plus weapon/token-reference neighbors |
| FLOW4-adversarial.xml.gz |166/166/0|05:28:12–05:28:19;7.4533531s|31 dedicated attempted-break cases plus neighbors |
| FLOW4-staging-red.xml.gz |9/0/9|05:29:05;0.3032237s|Native scenario absent before authoring |

All listed runs0compilererrors. Initial NUnit IsNotEmpty(null) argument exceptions
were replaced by explicit null/empty assertions. TryReforge's two-out overload
returns the displaced component; the corrected test uses the affected-weapon
plus displaced-component overload and checks both. These were fixture corrections,
not additional gameplay bugs.

## Adversarial gate and cold-eye review

35dedicated cases probe ten player-flow hypotheses: repeated splits; IDs observed
during crafting initialization; exact failed equip/drop/throw rollback; synthetic
vegetation placement refusal; same-blueprint world lookup; merging and cross-actor
transfer; opaque saved strings; independent legacy loads versus repaired resaves;
token aliases/nulls; and duplicate nonempty IDs. Additional full-session and load-hook
cases check aliases across cells/brain/equipment/turns and referenced-body ordering.
The FlowerField fixture is synthetic API coverage, not an ordinary pickup claim.

Independent source review found no must-fix in the three runtime hunks or original
32regression+31adversarial cases. Suggestions added: raw carried/equipped counts
before Distinct; full session null/empty cases; earlier load hooks seeing a later
referenced repaired ID; bare-body primary repair with unread owner placeholder.
Rollback wording now says no clone remains reachable, not that no callback could
have observed transient publication. Final expanded/native source review complete,0must-fix.

## Can verify / cannot verify

Can verify: exact item references, IDs, quantities, paid recipes, selection commands,
owner/equipment/turn aliases and preserved save format through script assertions.
Native scenario drives InputHandler with keyboard input; reflection only observes
menus/cursors. Its real actions brew a quench, forge two paid weapons, equip/unequip
one Dagger and one forged weapon, select/transform the exact split units at a forge,
and drop/open each unit through the individual ground picker.

Cannot verify: subjective visual feel, desktop mouse ingestion, all historical save
variants, arbitrary Part deep cloning or general callback rollback. No speedup claim.
These sparse clone/load changes do not alter a per-frame loop; no new hot-path
performance comparison is claimed. Existing art is reused; no new content/assets.

## Files / ownership

Runtime: Entity.cs, WeaponCraftingOperation.cs, SaveSystem.cs (three small hunks).
Tests: GameAuditSplitIdentityTests, GameAuditSplitIdentityAdversarialTests,
GameAuditSplitIdentityBenchTests. Native scenario/player/editor launcher:
GameAuditSplitIdentityBench*. Living plan, audit queue, daily work log and this report.

Entity/SaveSystem contain preexisting protected spell work. Stage only incremental
FLOW4 patches against saved pre-wave full bytes, preserving that work unstaged.
Native launcher uses a disposable GUID save slot, preserves prior preference,
unregisters synthetic runtime and deletes only its own slot. Final full/GUID/ownership
checks recorded below; commit follows.

## Final focused/native gates

Expanded focused102/102GREEN,05:32:06–05:32:09UTC,3.6492076s,0compilererrors.
Total new cases76:32regression+35adversarial+9staging. Final source cold-eye:
0must-fix across runtime/tests/native. Final cheap assertion strengtheners added
CurrentActor/WaitingForInput and all repaired IDs nonempty; full suite includes them.

Native run2c8a55557725443e8c786172e1f03c4d: **63/63PASS**,30.622404583s,
exit0,0compilererrors. Raw FLOW4-native.json and FLOW4-native.log.gz retained.
The run does not exercise save migration/throw/rollback; those are EditMode coverage.
2393unique asset GUIDs,0collisions.

⚪ Existing A31 teardown debt recurred after the successful report/launcher exit:
2MissingReferenceException entries, ZoneRenderer315 camera callback from
WorldFxCoordinator.CancelAll during OnDisable/OnDestroy. This is protected preexisting
spell presentation work already recorded in the broader queue. Exit0 does not mean
an exception-free shutdown; native gameplay assertions passed before teardown.
No claim of fixing A31 in this three-hunk identity wave.

## Full-suite correction gate

Initial full8765:8759passed6failed,05:35:02–05:37:03UTC,120.9815569s,
0compilererrors. Raw FLOW4-full.xml.gz retained. One failure is the known
FungalInfectionContagionTests self-cloud flake. Five failures are old W6map tests
comparing generated marker.ID(null) against the loaded marker, now correctly repaired.
The preservation fixtures did not supply an existing identity. Correct four W6
adversarial cases to use an explicit opaque saved ID and retain their equality
assertion. Split FellingSiteSave's case into existing-ID and missing-ID controls:
existing stays exact; missing becomes GUID-N; all appearance/custom-property/layer/
object-count/ground checks remain. This adds1test (total new77), with no further
runtime change. The initial source sweep missed these indirect null-ID comparisons.

## Close-out

**8766/8766GREEN**,05:39:07–05:41:08UTC,121.2167921s,0compilererrors,
0failed/skipped/inconclusive. FLOW4-final-full.xml.gz retained; known fungal test
passed on the repeat with its source unchanged. New77cases:32regression,
35dedicated adversarial,9staging and1additional map-save countercheck.
Native63PASS;2393uniqueGUIDs/0collisions. Independent review confirmed all prior
W6assertions survive the fixture correction. No remaining FLOW4must-fix.

Self-review: 🟡 missing clone/save identity fixed after clean RED; indirect W6
null-ID test assumption corrected transparently. 🔵 redundant crafting assignment
removed; exact-ID initialization/merge symmetry pinned. ⚪ known A31shutdown debt
and unrelated protected spell changes remain. 🧪 script-observable gameplay/save
checks do not establish subjective visual feel or mouse delivery.

Ownership gate:29whole paths with0overlap against the1027-path protected manifest,
plus one incremental hunk each in Entity.cs/SaveSystem.cs.31staged files;
`git diff --cached --check` clean. Protected spell changes remain unstaged.
