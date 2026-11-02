# Whole-game visual completion and delivery

Status: executing. Owner: Codex root; root serializes Unity and Git. Authorized by the
user on 2026-11-01: make a comprehensive professional plan and execute autonomously.
Baseline: `1f68a9ba87f3bbaf37284c0a6ea6b43892719acd`. This plan supersedes old queue
ordering, not their evidence or preservation obligations. Historical Claude ownership
notes remain historical; the current editor was observed idle, not playing, not
compiling, with a clean SampleScene. Preserve concurrent work and recheck before use.

## 1. Product outcome and scope

Deliver a coherent playable flat pixel-art world, with environmental identity,
readable materials and hazards, purposeful composition, restrained depth shading,
and working native sprites in ordinary play. Finish delivery of the already integrated
art as well as closing the remaining gaps. A contact sheet, passing tests or an asset
count is not acceptance. This is original CoO presentation, not a Qud-parity claim.

In scope: all registered surface biomes/formations, settlements and interiors,
ordinary underground material bands, sinkhole stacks, dungeons/lairs, special sites,
opening anchors, their visible actors/props/equipment and essential states; world,
inventory and Examine identity; camera, transitions, fog/memory; real chunk population;
subtle shadows; regression, performance, packaging and reproducible evidence.

Out of scope: new crop mechanics, economic/progression features, optional animation
refinement, a 3D conversion, faceted cave edges, broad rendering-engine replacement,
storage cleanup. The shale study stays disabled and preserved. Existing approved art
is revised only for a named defect. Reuse approved sources wherever their identity fits.

Completion is finite and auditable, not a promise about every possible procedural seed:
every registered area family must be inventoried, every authored named site audited,
every material/depth band exercised, plus representative generated samples and automated
multi-seed controls. Unseen combinations remain explicit limitations. No blanket
world-polish claim may be inferred from representative samples.

## 2. Requirements and verification contract

| ID | Requirement | Required evidence |
|---|---|---|
| R01 | Preserve flat native16×24 layout, square pixels, Point imports, approved smaller body formats, straight walls and reversible legacy mode. | Import/source audit; actual normal-zoom and explicit zoom comparison; mode toggle. |
| R02 | Remove accidental ASCII/placeholders from selected ordinary-world owners and terrain. Intentional glyphs require a named reason. | Runtime-selected-owner census, terrain audit, actual capture, exception register. |
| R03 | Each biome reads through material, palette, silhouettes and spatial structure; floors are quieter than vertical forms. | Matched actual views with named defects resolved; independent visual review. |
| R04 | World, inventory, worn and Examine art refer to the same real owner and state where applicable. | Reader/use/transfer capture and targeted runtime tests, including unknown-state fallback. |
| R05 | Never reveal hidden entities, contents or terrain through art, joins, shadows or memory. | Hidden/current/remembered controls, source and receiver disclosure tests, actual FOV transitions. |
| R06 | Material-only edits preserve geometry, collision, game state, RNG and save semantics. | Before/after owner and route controls, state transitions and reload. |
| R07 | Layout edits use existing owners, NPC roles and real interactions; preserve routes, doors, stairs, reserves and encounter balance. | Generation/navigation tests, whole-chunk route and interaction walkthrough, cached-save control. |
| R08 | More chunks feel substantial without uniform density. Retain intentional sparse regional character. | Separate composition ledger; dense/quiet patch review; targeted population measurements and actual views. |
| R09 | Shadows use coherent direction, low contrast and short pixel offsets; remain behind actors/cues and off UI. | Matched on/off capture, occlusion/disclosure/lifecycle controls and measured profile. |
| R10 | Optional/missing resources decline safely; disposal, travel, pause and legacy mode clear owned render layers. | Missing/malformed resource and repeated lifecycle tests. |
| R11 | Native gameplay remains responsive; no new allocations in hot paths or unbounded caches. | Relevant allocation checks plus 60–90s actual profile with environment stated. |
| R12 | Normal saves, approved editable sources, unrelated work and required builds survive. | Start/end preservation receipts and exact owned staging. No cleanup. |
| R13 | Source, tests, import metadata, editable assets and living records ship together. | Reviewed commit, remote verification, build/source receipt. |
| R14 | Claims distinguish authored, integrated, observed, polished and packaged/hardware-tested. | Separate status axes and linked receipts; no count-based promotion. |

## 3. Inventory and evidence model

Use `ArtSource/Live16x24/world-sprite-coverage.json` for historical coverage and its
`world_polish_phase` for historical bounded reviews. Add the current execution matrix
alongside it; never overwrite old results to manufacture completion.

Required matrix dimensions:

Current matrix: `ArtSource/Live16x24/visual-execution-matrix.json`, with reproduction
notes in `Docs/GRAPHICS-EXECUTION-MATRIX.md` (551 source-grounded scope records initialized; runtime audit in progress). Root owns
integration and all acceptance; delegates may author bounded inventories or reviews.

- Six authored surface biomes: Spread, Sodden, Beating, Stump, Grovelands, Overwrit;
  enumerate every registered formation and all 400 authored surface coordinates.
- Seventeen settlements, with each reachable interior and its service/door/water states.
- Six sinkhole stacks: mouth and every registered authored floor, plus exits and return.
- Six ordinary underground material bands, boundary travel and representative lairs.
- Every registered special-site/other-area family, including Morrowfast and Felling-Site.
- Six connected opening anchors as regression references, not the entire world.
- Shared actor/NPC/item/prop families and state/visibility combinations crossing regions.

For each row record stable area/family ID, source registration, seed/zone/depth, material
and owner families, coverage state, polish state, population state, shadow applicability,
named defects and severity, baseline/after/reader/route/reload evidence, test receipt,
reviewer, revision, next action and limitations. Allow not-applicable with an explicit
reason. Coverage states: not-audited, missing, fallback, authored, integrated, observed.
Polish states: not-audited, in-progress, reviewed, bounded-accepted. Population and
delivery remain separate; one successful packet cannot advance the entire row.

The old glyph census is a triage aid: it skips Terrain, includes covered owners,
short-circuits some actor types and samples depths1–3. Preserve its output, correct
interpretation and supplement with selected-owner/material checks. Do not claim it
measures all visible gaps or resources. Never spend repeated sessions rebuilding a census
when existing evidence already identifies the next large defect.

## 4. Work breakdown, order and exit criteria

| Work package | Deliverable and ordering | Exit criteria |
|---|---|---|
| V0 Baseline/control | Preserve dirty-state hashes; reconcile records, source/build revision and native test health; inventory all scopes. | Matrix populated from registrations; known failures/unknowns named; no foreign changes staged. |
| V1 Rendering gaps | First ordinary Spread field ground/rows and Felling-Site ground; then regional opening-only identities, sinkhole/lair materials, remaining visible ASCII. Rank by actual screen area, frequency and reuse. | Selected families native in ordinary game, truthful backing and readers; RED/GREEN/counter-cases; actual play/reload. |
| V2 Integrated-art acceptance | Exercise large creature/NPC/item/prop batch, Morrowfast and site art already on main. Correct state, identity or resource defects found. | Representative interaction/state paths observed; all named sites reviewed; no unsupported blanket acceptance. |
| V3 Materials | Region-by-region floor/ground/wall joins, transitions, repetition, hazards and memory. Reuse art; generate new concepts only where needed. | Every formation/material/site row reviewed, named serious defects fixed, actual before/after evidence. |
| V4 Composition | Alternate with materials; start where rendering is fixed but family-owned Spread layouts remain sparse, then other visibly unfinished chunks. | Meaningful focal groups, routes/entrances clear, dense and quiet patches; actual navigation/interactions and multi-seed controls. |
| V5 Shadows | Keep Sill/Beating pilots stable; trial ordinary settlement wall receivers, then disclosed vertical props and suitable regional walls. | Each expansion improves actual scene; no disclosure/cue/lifecycle regression; before/after profile; reversible. |
| V6 Release | Batch accepted slices into current Mac/Linux packages, normal launch/Continue/travel and rollback evidence. | Exact source in build receipt, Mac smoke passes; Linux archive/build checked; execute Linux/Deck when available and retain honest limits. |
| V7 Closure | Cross-area independent review, reconcile matrix/ledgers/continuations and final defects. | All release-critical requirements passed; no open P0/P1/P2; remaining optional/hardware limits explicitly listed. |

V1–V5 are interleaved by visible impact, not a waterfall. Do not wait for universal
sprite coverage before environmental polish or composition. Do not spend the entire
budget on caves. Each planning pass compares at least one surface, settlement/interior
and underground/site candidate, and selects the strongest evidenced need. After two
coverage/material slices reassess composition and shadows explicitly. Pending acceptance
is finished before accumulating another large disconnected atlas.

Priority: P0 data loss/crash/disclosure; P1 blocked play or misleading hazards/identity;
P2 visible broken/missing regional presentation or serious composition regression;
P3 minor repetition/optional refinements. Fix P0/P1 immediately. Close P2 before accepting
the affected slice. Intentional sparsity and intentional symbols are dispositions with
evidence, never convenient substitutes for unfinished work.

## 5. Per-slice production procedure and definition of done

1. Read the area record and current code; verify referenced owner/state/resource contracts.
   Save corrections before implementation. Name the observable defect and choose the
   smallest useful change. State invariants, counter-cases and exact files owned.
2. Freeze an isolated real GameBootstrap baseline: seed, zone, state/save copy, camera,
   resolution, mode and routes. Review the whole chunk through several normal-zoom views;
   an overview is supplementary. Do not call a detached catalog render gameplay.
3. For new art, follow actual capture → imagegen reinterpretation → critique/parts list →
   original editable native assets → integration. Preserve prompts, source and imports.
   Reuse established approved art without regenerating it just to satisfy ceremony.
4. For changed behavior, write meaningful failing tests, run and retain RED, implement,
   run GREEN and adjacent regression. Pair positive with hidden/wrong-owner/state/resource
   counter-cases. Use adversarial/mutation checks where state/placement/render lifecycle
   branches justify them. Never treat a zero-test success as a pass.
5. Capture the same actual route after changes. Exercise relevant movement, block, door,
   stairs, interaction, pickup/transfer, Examine, damage/removal, visibility and memory.
   F5 and a fresh Continue must preserve relevant owners/state; investigate unexplained
   differences rather than declaring whole-scene equality. Diagnostic invulnerability or
   fixtures are labeled and cannot establish encounter balance or natural acquisition.
6. Review art at native size and gameplay zoom, plus whole-chunk composition. Independently
   review source and visuals, fix significant findings, and repeat only affected checks.
7. Profile relevant behavior; update slice/area records in the same commit. Exact staging,
   inspect staged diff, fetch and integrate upstream safely, push main and verify remote.
8. Accept the bounded slice only when requirements, evidence and review agree. Record
   remaining area debt explicitly. A code-integrated slice may be published with an open
   acceptance gate, but is never described as observed or polished.

## 6. Acceptance checklists

### Coverage/material

- Registered owner and terrain match chosen native image; no silent opening-only limit.
- Required empty/full, open/closed, spent/ready, broken/intact and wet/dry states are
  truthful; unsupported transients retain useful fallback/overlay rather than disappear.
- No black backing, accidental glyph, floating pivot, texture bleed or palette mismatch
  in the accepted views. Floor contrast remains below actors and interaction cues.
- Straight wall boundaries and joins are consistent; door/stair/readability survives
  silhouettes and shadows. Neighbor reads obey disclosure; memory never reads live hidden state.
- World and relevant readers agree; fallback and old mode remain operational.

### Area composition/population

- Entire traversable chunk audited at normal zoom: entry, center, edges, landmarks,
  interiors and exit. Include dense, quiet and intentionally sparse views.
- Density supports ecological or inhabited purpose; avoid repeated stamps and uniform
  carpets. The spawn area is a richness reference, not a quota for every biome.
- At least one clear focal structure/group where appropriate; routes and service approach
  cells readable. New services have real supported owners; no free loot/currency inflation.
- New generation preserves authored reservations, reachable exits, collision, hazards,
  encounter balance, determinism and old cached saves. Minimum three fixed seeds for
  changed procedural rules plus adversarial crowded/blocked/reserved cases.
- Owner-count targets diagnose distribution; they do not independently establish quality.
  Record why a sparse formation remains sparse or fix its actual composition.

### Light/shadow

- One documented direction; start from existing2px south/16% wall pilot, not automatic
  arbitrary new values. Short contact shade stays faint and supports vertical material.
- Real currently disclosed source and receiver only; no hidden neighbor or occupant leak.
- No shade on UI, unexplored cells, hazards/water, doors/cues or occupied receivers unless
  explicitly designed and verified. Pause/travel/legacy/toggle/removal clear correctly.
- Matched on/off gameplay images show an improvement and preserved readability.

### Performance, regression and delivery

- No new console exceptions or deterministic test failures. Existing failures receive
  exact test names, baseline revision and disposition; an unexplained failure is not waived.
- Use actual60–90s walking/interacting/travel windows, record hardware/resolution/mode,
  p50/p95/p99/max, batches and allocations where measurable. Compare same route/settings.
- Investigate >10% sustained renderer regression or >0.5ms p95 added renderer cost;
  reject unexplained new spikes or unbounded growth. These are investigation thresholds,
  not invented performance measurements. Confirm final frame budget in packaged gameplay.
- Current Mac package normal-launch/Continue, opening↔settlement↔surface↔underground
  travel, one service/reader, save/restart, zoom and legacy return pass. Linux build and
  archive integrity required; physical Deck execution stays separately unverified if absent.
- Package source revision/checksums, commands, known issues and rollback path retained;
  prior required packages/saves survive. Final commits confirmed remotely.

## 7. Risks, dependencies and autonomous decisions

| Risk | Response |
|---|---|
| Dirty shared checkout or another editor owner | Preserve initial hashes; root alone tests/publishes; recheck active operations; continue independent work while a real conflict persists. |
| Stale tests/assembly or empty filter | Confirm compile/readiness, source type and nonzero discovered count; store job/XML. |
| Census misclassifies hidden/covered owners or omits terrain | Label census scope; use selected-owner probes and actual views; don't use it as completion percentage. |
| Broad art work outruns acceptance | WIP limit: one root integration/capture slice, two bounded audits; finish acceptance before next atlas. |
| Imagegen output not production-ready | Keep it a reference; author editable native pixels and critique real gameplay. |
| More objects obscure gameplay or change economy | Existing supported owners, no fake services/loot, route/stock/encounter controls. |
| Save or build contamination | Dedicated fixture roots and manifests; no normal-save mutation or cache deletion. |
| Hardware/unavailable tool blocks a gate | Record exact blocked gate, choose available fallback honestly, continue independent scope; don't invent acceptance. |
| Credit/time waste | Compact reads, narrow delegation, shared evidence, batch builds, no repeat full suites without changes. |

No routine design, tool, commit or publication confirmation is needed under the user's
authorization. A discovered premise correction is documented and resolved with evidence.
Do not restart already verified work. Do not expand unrelated economic/crop/animation scope.

## 8. Reporting, continuation and completion

Hourly reports: completed/in-progress/blocked slices with region/materials, captures,
tests and main/build status; separate coverage, polish, population and shadows; concrete
next-hour goals. Persist actual report UTC to prevent duplicates. Share actual screenshots
at visible milestones, labeled by location and whether Editor or packaged. Routine storage
inspection is quiet and never deletes. Progress updates explain meaningful findings.

Each continuation must state: current commit and exact owned dirty files; Unity/play/save
isolation state; active slice and pass; last tests/captures/review; failed/blocked gates;
next runnable action; deferred debt; last report time. User approval is not an exit gate.

Final closure requires the inventory audited, all release-critical R01–R14 gates satisfied,
all registered scope rows dispositioned with evidence, serious visual defects resolved,
current packages verified to available-platform limits and independent review complete.
Keep unsampled procedural combinations/hardware limitations explicit. Until then this
plan remains executing; a single packet or exhausted turn is not project completion.

Release hard gates: zero unexplained glyph/fallback results among selected canonical
visible owners and underlying terrain in the audited matrix. Intentional symbols,
unsupported altered states, missing-resource controls, covered owners and unsampled
content have separate counts; prose disposition cannot waive ordinary missing art.
At most one published integration slice may await actual acceptance; clear that backlog
before V6/V7 closure. Baseline test failures must list exact names, impact and whether
they block the slice/build; relevant unresolved failures block release. Packaged frame
budget is pending verification against the existing product performance target; do not
mark R11 accepted until the target, hardware, resolution and measured result are recorded.

Independent plan review (2026-11-01): strengthened matrix traceability, zero unexplained
fallback gate, failed-test impact policy, integration-backlog limit and explicit pending
packaged-performance target. All are requirements, not claims of verification.

## 9. Execution log

- 2026-11-01: baseline/main reconciled at1f68a9ba8. Existing75 tracked modifications and
  untracked material are foreign until proven otherwise. Unity idle/clean; no active tests.
  Two bounded read-only audits started. Historical glyph census filter first returned0
  tests due to wrong namespace; corrected to `CavesOfOoo.Tests.GlyphCensusBenchTests`.
  No zero-test result accepted. Current census requested; coverage limitations above recorded.
- First slice selection: ordinary Spread field-row rendering competes with Felling ground.
  FieldStrips10.15 has documented241 glyph cells and approved existing field art: fix actual
  presentation before adding clutter. This does not authorize cultivation mechanics work.

- 2026-11-01T20:31Z: V0 source matrix initialized:551 overlapping scope records,
  400 authored surface coordinates,17 settlements and all registered material/site
  families. Historical17 polish reviews preserved; no new whole-area acceptance.
- V1 ordinary dry Spread field rows now have actual before/after/2x, Examine, real
  harvest and freshContinue evidence. Native196/196 pass, including incremental/full
  harvest repaint, optional resources and adjacent catalogs. Independent source/visual
  review found0 newP1/P2. See `GRAPHICS-SPREAD-FIELD-ROWS.md`.
- Remaining first-slice gates: matched60–90s profile and package execution; do not
  call the area complete. Existing stubble repetition, sparse center and unrelated
  glyphs are recorded. The next implementation candidate remains Felling ground;
  clear the current acceptance backlog before expanding disconnected work.
- Automation `overnight-clearing-art-pipeline` updated and confirmed ACTIVE at its
  existing10-minute cadence, pointing to this plan/matrix and current continuation.
  Root-owned Unity test sessions finished; all75 preexisting modified tracked files
  retain their initial hashes. No cleanup.

- 2026-11-01T21:01Z: Felling1,638dry-cell slice observed in actual gameplay, native163/163, independent review clear, freshContinue preserved. Separate current matrix records retain fallback/in-progress for the whole site.
- Bounded manual profile extension records native raw-unit percentiles without gameplay writes;71sFelling/81sSpread completed. Spikes, unmatched baselines and current packages keep R11/V6 open; see Profiles/README. This does not waive the backlog or expand full-area acceptance.
- Last actual hourly report2026-11-01T20:59:02Z; next21:59:02Z. Normal isolation restored; storage inspection only.

-2026-11-02: bounded Spread10.15 Tree root-contact V5 slice observed;154native, actual1×/2×/reader/walk/F5/freshContinue and independent review. No V0–V7 closure; R11 remains open. See GRAPHICS-TREE-CONTACT-SHADOW.md. Batch current delivery next.

## Current Deck compatibility gate (2026-11-02)

Preserve the requirements and evidence limits in `Docs/STEAM-DECK-FLAT-READINESS.md`. Future changes affecting input, cameras or UI must retain 1280×800 readability, controller access and free zoom/menu actions. Reuse the current isolated flat controller route and relevant focused regressions; do not rerun unrelated suites for art-only edits. Linux packaging and Editor/Mac qualification do not close the physical Deck, Steam Input or sustained-performance gates.
