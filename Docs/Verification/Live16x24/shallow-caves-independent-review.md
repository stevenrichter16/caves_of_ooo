# Shallow-cave foundation — bounded source and native review

Current checkpoint: 2026-10-27 17:53 UTC. **395 distinct current native passes
and a bounded Editor material/readability/state/travel checkpoint, including
fresh depth1 and depth2 Continue, are verified.** Source commit/push and new
packages remain pending. This is not whole-cave, whole-underground or serious
visual-polish acceptance. Earlier checkpoints below are retained historical
findings; the latest actual addendum supersedes their then-pending outcomes.

## Reviewer roles and independence

- `/root/world_coverage_ledger` authored the scratch companion and routing diff.
  Its runtime examination is **self-review**, not independent implementation
  approval. It independently cold-read the test author's saved fixture and
  parsed the retained XML and source receipts. It did not run Unity or Git.
- `/root/portable_equipment_final_review` authored the 44-case fixture. Its
  fixture examination is test self-review. Its runtime/art examination is
  independent of the companion and native source authors. Root reported no
  significant runtime finding; the reviewer's direct scratch-contract feedback
  also confirmed the agreed constructor, independent groups and ground/reader
  contract, with no edge-mask requirement. This attribution does not turn either
  author's own work into an independent review.
- Root owned native execution, exact Assets import, actual gameplay controls and
  capture. The runtime was absent from Assets through genuine RED. A production
  draft had already been prepared outside Assets under root's explicit
  direction; that drafting-order deviation is disclosed rather than described
  as strict no-code-before-RED compliance.

## Source findings and safeguards

Reviewed files:

- `Assets/Scripts/Presentation/Rendering/FlatPixelShallowCavesCatalog.cs`
  SHA256 `5e5d68512cd78bc472fee12e1d77d370869ff8084b14089f70abe718e51c9a52`.
- Its narrow `FlatPixelGreyboxCatalog.cs` ground, selected foreground, subject,
  lazy-resource and disposal hooks.
- `Assets/Tests/EditMode/FlatPixelShallowCavesTests.cs`, against actual blueprint,
  destruction, vertical travel, cell ownership and renderer APIs.
- `Docs/GRAPHICS-SHALLOW-CAVES.md` and the source verification/handoff receipts
  under `ArtSource/Live16x24/shallow-caves/`.

Admission is conservative current-column eligibility. Canonical depth1/2 IDs
come from an immutable finite lookup; every query rechecks attached manager,
exact cached graph, current map dimensions/POI and the saved lair keyed by the
**surface** ID. Registered sinkhole, Root, pilot and authored wilderness columns
are excluded. `WorldLocationContext.For` and cached reference equality are
current authority, not proof of the graph's original generator. The save format
has no such provenance field; no new ledger or simulation policy was added.
Root/pilot/authored whole-column exclusions deliberately prune some otherwise
ordinary depths. No render call invokes generation, lair adoption or travel.

Ground belongs only to a real, disclosed, neutral `SandstoneFloor`. The local
occupant traversal is bounded to the current cell; no whole-zone census occurs
per query. Wet/heat/charge/cultivation or competing ground retains fallback.
A selected stair, rubble, stalagmite or actor keeps its own foreground; only the
exact selected floor glyph is suppressed after native ground succeeds. The
same fallback selector decides equal-layer ownership, including insertion-order
ties. No empty cell receives invented floor pixels.

The five foreground identities require current placement, selected visible
ownership, exact relevant parts and semantic state. SandstoneWall's surviving
structural HP1..30 is distinct from a missing creature Hitpoints stat. Real
DestructionSystem damage/removal places the real Rubble; the catalog never
simulates that transition. Subject sprites retain the current world frame and
authority. Hidden, remembered, stale, custom, malformed or custodied owners
refuse native art. Unrelated tile overlays are not globally suppressed.

Six resource groups fail independently. The new wall2 atlas, Beating floor12–15,
16×16 rubble/stalagmite and existing down/up cells become cached FullRect views;
explicit injected nulls do not reload resources. Disposal destroys owned views,
not borrowed textures. New routing preserves earlier portable actor/item art
when this companion misses. No new Render-event dispatch, edge mask, lighting
or grading change was introduced. **Existing native painting bypasses cell-light
tint**, so native cave contrast must be assessed in actual AFTER views rather
than inferred from ambient values or contacts.

## Native evidence and test review

| Retained run | Result | UTC interval |
| --- | --- | --- |
| `shallow-caves-native-red.xml` | 0 passed, 44 failed; production adapter absent | 16:59:32–16:59:46 |
| `shallow-caves-native-first-green-attempt.xml` | 43 passed, 1 failed | 17:02:59–17:03:05 |
| `shallow-caves-green.xml` | 44 passed, 0 failed/skipped/inconclusive | 17:06:24–17:06:31 |
| `shallow-caves-related-green.xml` | 339 passed, 0 failed/skipped/inconclusive | 17:08:38–17:09:04 |

The first GREEN trial failed in
`OwnerlessAndRubbleOnlyCellsNeverAcquireAnInventedSandstoneFloor`: the test called
`Native(Add(Floor,X-1,Y))`, placing the object at `X-1,Y` but checking the helper's
default `X,Y`. Root changed only that call to
`Native(Add(Floor,X-1,Y),X-1,Y)`. No production relaxation was made. The original
RED fixture remains in `shallow-caves-red-fixture.cs.txt`.

The 44 focused and 339 related results contain **383 distinct passing test
identities with zero overlap**. Raw XML, not MCP's false startup-timeout metadata,
is authoritative. XML hashes:

- RED: `871b14bb4fce2ff1f5872134a7c8c94af4f7d542cff68588611caf533f273125`.
- First trial: `7b35e209ca2c99a855abf4ae82c8abd3069e1b250a04bbb75cd627afbab86ab2`.
- Focused GREEN: `689c34d25e579efbcd37c808eb5b572709d157b7c6dc9d4d57d3a8f0e5d163b5`.
- Related GREEN: `ab22203e619817883811640e63e97555ec14d25a542c2877699e4e19d2af4666`.

The independent fixture cold-read found no concrete API/fixture blocker in the
saved current source. Positive native preconditions precede refusal counters;
coverage includes real natural depths, current graph/map/lair revocation,
disclosure/custody, actual floor/selected body separation, optional groups,
structural damage to real Rubble with retained water, submitted incremental
paint, ordinary vertical roundtrip and removed-return refusal. Teardown restores
render/audio/visual callbacks, the liquid registry, destruction factory,
renderer bindings and captured performance counters. This source review does
not claim an allocation benchmark or replace native execution.

## Art and gameplay limits

New wall source/import SHA256:
`4bb598677352f0ccd8b5be8a67f31e79d6aeab0704aa96db9c826b0d4b8dc576`.
It is a32×24 atlas of two16×24 opaque native cells, four flat earth colors and
quiet interior fractures. The source receipt verifies768 native pixels,
324,480 direct-contact pixels, identical base edges, protected input checks,
reopened editable geometry and negative corruption controls. These are source
checks, not graded gameplay acceptance. Existing five borrowed families remain
unchanged. Source-art receipts intentionally retain their at-authoring
`runtime_imported:false` and `gameplay_acceptance:false`; current native runtime
evidence is recorded separately above.

Nine actual BEFORE captures and `shallow-caves-before-play.json` establish normal
surface-to-depth1 travel, real owners, wall damage and a fatal hostile encounter.
The pre-encounter `Fixtures/shallow-caves-entry83` remains turn10730, player56,7,
HP23. Depth2 and a successful return were not reached in that baseline. Root is
starting actual AFTER work; this review does not invent those results.

Current delivered source remains Grovelands
`4dbdc9be95658341260efa10307dadb784ba8392`, with evidence commit
`83b455e4140ad2fd7f7391402b2d33dad7ac56dc` verified on origin/main by root.
New cave source push, packages, Linux/Deck execution and sustained performance
are not claimed. Existing55 inventory and5 Stump area polish statuses remain
unchanged. Cave actors, hazards, loot, full chunk composition and all remaining
world coverage/polish stay open. No storage cleanup was performed.


## Actual first-trial addendum — black-hole readability defect

The ledger reviewer independently viewed actual
`shallow-caves-after-01-entry.png`: near-black individual cells inside the tan
walkable routes, including visible rubble piles on dark squares, resemble pits.
Native wall/floor presentation works, but the first actual view fails the visual
readability gate. The independent runtime/art reviewer also reported this
specific defect after comparing after01 with before02. No source-code/API finding
is substituted for the failed gameplay view.

The independent reviewer confirmed the six-owner runtime/routing had no
significant defect within its stated prior contract: current authority,
disclosed owner semantics, actual-floor guards, surviving structural HP,
independent optional groups and disposal. Their art review matched all19 frozen
source dependencies and the unchanged32×24,768-opaque-pixel,4-color source/import
SHA4bb598677352f0ccd8b5be8a67f31e79d6aeab0704aa96db9c826b0d4b8dc576.
Their fixture review remains self-review, as disclosed above. These findings do
not accept the black-hole appearance or whole-area composition.

Capture02's `up-stair-menu` filename is misleading: root reports the focus/menu
attempt missed and it is an ordinary view. Capture03 verifies the actual up-stair
reader. Root archived the owned session as
`e4200c135a14440b89f97f2576fa935d-cave-first-trial-preserved` and finished to idle.
No actual depth2/return/restart or packaged cave result is added by these views.

The agreed next step is a separate owner-free **AmbientBackdrop**, preserving
`LocalGround` as the actual floor contract. Proposed API:
`TryResolveCellBackdrop(Zone,int,int,out Tile,out Color)`. Reuse the cached white
background tile with approved floor-base RGB128,106,76. Alter only the two
background tint predicates; leave water/other mark suppression, foreground,
physical owner/null, collision, subject art and floor glyph suppression intact.
A valid visible rubble owner or cell without disclosed substrate can gain a quiet
backing without inventing a floor. Hidden/foreign/custodied occupancy must not
change that backing: raw occupancy count and collision are not eligibility gates.
Current scope/disclosure, conflicting terrain/cultivation/source meanings,
authored backgrounds and actual tile-state cues remain required counterchecks.

New native RED, production and actual correction/replay are **pending**. The383
retained passes predate this repair. No production draft for the new correction
was authored by this reviewer. The six owned records are frozen pending root's
next verified facts; no runtime, Unity or Git action was taken here.


## Root-authored backdrop implementation — independent cold review and native result

After strengthened RED12failed/44passed at17:22:59–17:23:14UTC, root authored
`TryResolveCellBackdrop`, the separate optional `AmbientBackdrop` field and the
two background tint changes. The ledger agent independently read all four saved
runtime files; it did **not** author this repair and found no significant source
blocker. The earlier adapter self-review attribution remains unchanged.

The companion reuses current scope/disclosure and neutral TileState guards. It
checks valid placed, visible owned Render/Physics before dereferencing Render;
real floors retain their own path, authored backgrounds and conflicting meanings
refuse backing, and hidden/custodied blockers do not affect eligibility. The
cached selector background is borrowed, remains alive through companion
Dispose, and unavailable/disposed selectors refuse safely. There is no new
texture creation, whole-zone scan, event dispatch, parser or simulation write.
Only background tint uses `LocalGround || AmbientBackdrop`; existing GroundKind,
floor glyph suppression, owner/null, collision, foreground/reader and represented
water/brine/mire rules remain unchanged. Existing unrelated constructors default
the new flag to false.

Final focused GREEN56/56 (original44 plus12 backdrop cases) ran
17:27:05–17:27:19UTC, no failures/skips/inconclusives. XML SHA256:
`189ebe9792c41cc8e7d765a2c2e6d84b604aa611a83d6affefc341d1c5cdd5cb`.
The retained55/1 first attempt compared injected floor art against the global
Resources texture via the old helper. The saved fixture now verifies the actual
injected input, floor/owner and reader parity directly. No production change was
needed for that assertion. The source correction for explicit black also
retains a `ParseBackground("^k") == Color.black` precondition; `^K` is dark gray.

The339 related passes predate this repair; post-change related execution remains
pending. The native result and source pass do not establish actual visual
resolution of the black-hole defect, depth2/return, restart or packaged delivery.
Root's actual hourly report was sent17:27:22UTC, next18:27:22UTC: Grovelands
main/package delivery complete; cave visual/deeper/restart/main/package gates
in progress; remaining underground/world coverage and later serious polish open.
No polish status advances and no cleanup deletion follow from this checkpoint.


## Post-backdrop related result — 17:30:03 UTC

The current related run is now complete:339 passed, zero failures/skips/inconclusives,
17:29:38–17:30:03UTC, retained `shallow-caves-backdrop-related-green.xml` SHA256
`ad2353650ad520a64d6df46e16b86a5a654448b353c54fba216dc14d8d31c345`.
Independent read-only parsing verified339 unique related test identities,56 unique
focused identities and zero overlap, giving395 distinct current passes. This
supersedes the preceding rerun-pending statement without reclassifying the older
pre-backdrop receipt. The bounded independent runtime cold review has no new
finding. Actual visual correction, deeper travel/return, restart, source push and
packages remain pending; native results do not substitute for those gates.
Hourly timestamps remain17:27:22UTC/18:27:22UTC and all polish states are unchanged.


## Actual corrected replay — bounded review, depth2 restart pending

The ledger reviewer independently viewed corrected entry04 and depth2 view25.
The former keeps rubble and visually ownerless walkable cells on a coherent
quiet brown field, while the darker wall boundaries and pointed Stalagmite
remain legible. The source/runtime reviewer independently compared actual01/04
and reported the named black-pit defect corrected: sample746,264 changed from
RGB6,4,3 to94,67,41, matching unchanged adjacent actual floor at730,264. Within
crop0,40,940,680,17,789 previously near-black pixels now match the dominant floor.
This is not whole-frame identity; torch/postprocessing drift remains. Repeated
floor texture, glyph content and chunk composition remain polish debt.

Root's ordinary controls and state observations establish readers06/07, normal
surface ascent08 and reentry09, actual wall48,13 structural28→16→5→removal with
real Rubble72942 in11/12, and movement onto that cell.13 rejected Skills and14
Pause are explicitly excluded as rubble/save proof. Correct-GameID autosave
fixture84,turn10980, survives fresh normal Continue15 at48,13,HP23;16 is the
actual Rubble reader. No invented owner or simulation repair was needed.

Ordinary surface/world-map travel then reached16,13 and its depth1 in18/19.
Existing tonic use raised HP23→40; wall41,9 destruction and real Scrabbler combat
left HP33. Normal descent25 reached16.13.2 and ascent26 returned to16.13.1.
Actual F5 fixture85 is saved atdepth2,turn11910,HP33. Root is performing its fresh
Continue; that result is pending. These control/state facts are root-reported;
this reviewer did not operate Unity or infer them solely from screenshots.

The saved profile records90.286573seconds and18,378 samples per recorder with
no capacity overflow. It includes actual Editor movement/idle/hostile interruption
and MCP/CUA instrumentation; no matched-before, sustained package FPS or Deck
claim is made.395 current native passes remain unchanged. Root's consolidated
after-play receipt, final depth2 persistence, source push and packages are still
pending. All55+5 polish records and17:27:22/18:27:22 hourly clocks are unchanged.


## Final Editor checkpoint — root evidence and persistence, 17:53 UTC

Root's saved `shallow-caves-after-play.json` now provides the full actual route,
29 retained actual/rejected-correctly-labeled captures, and fixture83/84/85
metadata/hashes. The ledger reviewer read the final receipt and independently
matched every listed image and fixture hash to the saved file.29 remains an
evidence inventory, including failed/rejected captures, not29 accepted views.

Root verified fresh normal Continue27 from fixture85: Overworld.16.13.2,
player40,12,HP33, savedturn11910. Normal Look/menu28 and examine29 on the visible
down stair39,12 confirm its native reader. These final control/state facts are
root observations; this reviewer did not operate Unity. Together with the
actual earlier wreckage84 restart, readers and normal vertical roundtrips,
they close the bounded Editor material/state/travel/persistence checkpoint.

The independently authored `shallow-caves-final-visual-review.md` is now durable.
It opened seven specified actual views and found the named pit-like backing
defect corrected, with useful material integration in the sampled ordinary
depths1/2. Repeated floor/rubble vocabulary, memory glyph boundaries, uncovered
actors/containers/hazards, room landmarks and whole-chunk composition remain
explicit debt. It does not accept whole areas as polished or complete.

Root reports normal Editor/standalone saves match the retained baseline and44
foreign tracked paths match the Maw baseline. Isolated roots were archived and
finished; finalPlayfalse/rootnull/clean scene was confirmed. No broader preference
identity is inferred.395 native passes remain unchanged; source commit/push,
new packages, Linux execution and physical Deck verification remain pending.
All55+5 polish states and17:27:22/18:27:22 hourly clocks remain unchanged.
