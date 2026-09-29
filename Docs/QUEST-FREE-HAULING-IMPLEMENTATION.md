# Useful hauling shortcuts

Status: version7 implementation and native local acceptance complete after the cooling-workspot release `d0fb582c`, ready for publication. Full native regression completed with21,858 passes and4 timing failures; a same-condition published-code comparison reproduces the two timing failures selected for comparison. No all-green full-suite claim is made. The existing hauling mechanic, original beam/barrel models and saved beam appearance already work; this slice gives them a meaningful use in ordinary exploration.

## Priority and player outcome

The next priority is a consequential environmental choice using an existing mechanic. Some newly generated Spread hedgerows may contain an original rolled beam or barrel across a useful opening. A player can take the open detour, pull the obstruction onto a shoulder and leave a shorter passage, or use an independently available destructive solution. No quest, waypoint, extra reward or mandatory haul puzzle is required. Existing saves keep their literal assignments and changed terrain.

This is original Caves of Ooo content inspired by the requested emphasis on systemic exploration, not a claim of reproducing a particular Qud encounter or hauling implementation.

## Pre-implementation source sweep

| Premise | Verified contract / correction |
|---|---|
| Heavy objects are missing from normal play | `HaulablePropBuilder` already rolls one object with 350/1000 probability, after population and stocking. The gap is useful placement, not another item definition. |
| A loose beam is an available blueprint | The current blueprint is `FallenBeam`. Spread also rolls `HaulBarrel` and `MillStone`. The latter exceeds an ordinary Strength18 player's hauling limit and remains untouched. |
| A blocked opening is useful if the cardinal detour is long | Real players move diagonally. Measure the complete proposed layout with eight-direction movement, including vacated donor cells and the parked load. |
| Grabbing and releasing consume turns | The current Haul/LetGo commands are free; paid movement has a speed penalty. Compare actual scheduler work, not invented extra action costs. |
| Solid props provide cover | Solidity establishes movement obstruction, not sight or projectile cover. This slice makes no cover promise. |
| Terrain can place this before the haul roll | Doing so would consume or move the existing random stream or leave changed terrain without a load. Compose after the exact successful roll using original producer receipts. |
| The current field-passage receipt must be replaced | Reuse the existing per-Hedge terrain receipts and one shared exact-owner receipt contract. Add only the actual haul producer recognition. |

Additional verified corrections: FallenBeam has no DestructiblePart; HaulBarrel and Hedge retain their actual Break behavior. The comparison corpus must use prospective version7 assignments, including changed same-family adjacency, rather than filtering old version6 addresses. The generation implementation and independent mechanics review record further corrections below. Source anchors include `HaulablePropBuilder`, `SpreadCompositionBuilder`, `SpreadExplorationPlan`, `SpreadGenerationReceipt`, `OverworldZoneManager`, `DragRules`, `DragSystem`, `HandlingPart`, `MovementSystem` and their existing tests.

## Bounded implementation plan

1. Execute missing-behavior tests for opt-in haul-source capture and the new situation. Preserve the original roll, pool, placement attempts and following random value, including empty and failed rolls. Matching preexisting objects cannot substitute for the captured owner.
2. Privately implement a complete proposed layout using one original beam/barrel and twelve original, undamaged standard Hedges. The same straight-pull layout supports four orientations. Move no actors, inventory, stock, terrain budget or unrelated props; preserve reserved roads, exits, stairs, hazards and owned content.
3. Prove useful geometry before committing: a thirteen-cell boundary with one aperture, a broad dry parking shoulder and an open detour. Guarantee two straight haul steps, a clear parked route of at most six steps, an eight-direction detour of 14–60 steps and at least six steps' benefit after conservative hauling cost. Limit eligible centers to32 and rotations to4; preserve essential connectivity both before and after parking.
4. Capture and revalidate exact source identities before, during and after relocation. On a clean refusal restore only still-owned unchanged moved instances. Independent mutation is never overwritten. Retain ordinary destructibility and saved positions, grip references and speed restoration.
5. Append `HeavySalvage=11` in a new-world version7 candidate. Hedgerow selection changes from three to four variants; literal versions2–6 keep their old branches. Do not register an unproven family merely to fill a catalog slot.
6. Freeze the complete selected corpus for seeds1,64,1729 before generation. Compare candidate and same-version composition-disabled sources, report every selected/refused address, source type and reason, and measure cold generation time. Candidate release floor: at least three commitments across two seeds and at least half of genuinely compatible source packets. If it fails, diagnose source scarcity versus unsuitable geometry and revise the decision explicitly; no forced roll, seed shopping or hidden relaxation.
7. Root runs native RED/GREEN, relevant neighbors and an isolated actual-input witness at the first qualifying site in the predetermined seed64 cohort. Record detour, free Haul/LetGo, two paid pulls, same-ID/model parking and saved aftermath. Keep actual threats active; an interruption is retained as incomplete evidence. Review screenshots. Do not build a campaign-playing bot to make a marginal observation pass.
8. Run paired adversarial and Q1–Q4 review, update this ledger with exact evidence and limitations, then run the full native regression on frozen inputs and publish only owned files after fetch/rebase.

## Readiness and deliberate limits

- Ready: real haulable content, handling commands, reduced-speed movement, original models, saved beam identity and existing lifecycle tests.
- Integrated candidate: source capture, useful bounded placement, historical version controls and tests.
- Required before activation: actual fixed-corpus availability and source-preservation proof.
- Required before verified publication: native action/save/render evidence and regression checks.
- Deferred: new cover behavior, pushing, smooth hauling animation, a corner-pull variant, broad regional rollout and unrelated low-impact defects.

## Acceptance and review log

- A machine/editor restart erased the earlier temporary scratch directory. Shared plan/tests survived. Private work now uses persistent scratch storage; erased receipts are not claimed as retained evidence.
- Reconstructed private hauling-mechanics audit passes20/20, zero failures/skips in0.442929s, with all seven watched production inputs unchanged. No gameplay defect was confirmed. Two ordinary paid pulls take27 scheduler ticks for the Speed76 beam and29 for the Speed70 barrel, versus20 without a grip. Save/release preserves one exact owner and independently applied speed penalties. This is a logic pre-check, not native input/render proof.
- Executed native source/version/mode RED:27 cases,7 passed controls,20 expected missing-feature failures,0 skipped in2.4571096s (job `4f78c3aa95f641f282331d5d4b056e1e`; `Hauling/Integration/native-source-mode-red.json`). The existing five historical-family and two scene-restoration controls passed. The source mutation counter uses actual Physics.Weight, not an absent beam DestructiblePart.
- Native layout/helper RED:15 expected failures,0 skips in1.7812107s (job `2d42b35ce2a741729535aaf5d6677417`; `Hauling/Integration/native-layout-red.json`). These are missing-source/helper baseline failures, not claims of a broken shipped mechanic.
- Callback sweep correction: current single-cell MoveEntity/AddEntity does not dispatch a footprint callback. Mutation tests use the actual supplied generation-authority callback after a real move, rather than inventing an event seam.
- The isolated live mode, cue and generation placement have now been adopted; native generation acceptance is next.

Private execution remains a pre-check; the later native checkpoints below establish generated shortcuts and one successful local live witness. Ordinary discovery and long-form exploration remain unmeasured.

## Contextual hauling affordance

The existing subtle action hint system currently omits Haul/LetGo even though the real adjacent action menu supports them. Extend that same one-action hint to current visible applicable owners, preserving existing ordinary priorities and keys. Read-only availability must respect actual weight, reach and reciprocal grip; a hidden, stale, foreign or too-heavy owner cannot advertise a haul action. Do not call mutating menu callbacks in a per-frame query. This is part of making the new environmental choice legible; no new waypoint or marker architecture is planned.

## Next decision

After this slice, compare predator/prey interaction, meaningful alternate situations, readable environmental affordances and expansion to another biome. Prioritize a complete player-visible consequence over multiplying cosmetic layouts. Preserve the user's instruction to document low-value problems and move on.

Cue native RED:44 cases,34 passing controls and10 expected missing-cue failures,0 skipped in2.3127685s (job `ec95cf673e164ebb9e8fb5ae22119b7f`; `Hauling/Integration/native-cue-red.json`). This includes all17 existing native UI cases. Query changes remain private until the narrow implementation passes.

## Integration checkpoint

- Expanded native generation RED:65 cases,12 passing historical controls,53 expected absent-feature failures,0 skips in4.8259775s (job `b0cc825ae6f44897871e77c63afe4162`). The complete65-case receipt is retained before production adoption.
- Contextual cue and native launcher integration GREEN:129/129 passed,0 skipped in4.418949s (job `232f5836ca2a42c89e7cf092d88094a7`). This includes the original query/UI neighbors and scene-restoration guards. The new cue fixture initially omitted the Speed stat maximum; explicit1000 bounds now prove the intended100 speed instead of the default30 clamp. Corrected matched private RED and GREEN are retained.
- Placement review confirmed two independent mutation-window gaps in the private candidate: an unrelated off-route owner added during relocation, and the same addition in the final authority callback. Paired tests exposed each before publication; full owner-set validation now guards both commit boundaries. Later accepted-state validation still permits ordinary off-route content.
- The mode uses the original beam/barrel models, measures the full eight-direction detour, performs native grab/two pulls/release/short crossing, and saves the actual parked state. It suspends no NPC turns. Actual Play and pixel review remain pending.

Final private generation checkpoint:228/228 passed,0 skipped in71.683s, comprising67 focused cases and161 existing version/catalog/pipeline neighbors. Dedicated24 adversarial cases include a third confirmed callback window: the first authorization call could change the graph before the initial snapshot. The candidate now captures before that callback; paired private RED preceded repair. Native baseline extra2 cases failed on the absent source/helper,0 skips in1.1420469s (job `ca235abf14af4c348f8d1e209555c8aa`). These baseline missing-feature failures are distinct from the private candidate's mutation-specific RED receipts.

## Performance and presentation

This composition runs only during cold chunk generation. It admits at most32 eligible centers and4 orientations each, with bounded whole-zone physical searches; no per-turn reconstruction, global ecosystem or new cache is introduced. Fixed corpus timings are retained as single samples, not a statistically meaningful performance claim. Haul/LetGo reuses the existing bounded visible-neighbor query and one corner/HUD hint; no world scan, new per-frame collections or menu callbacks are added. Existing handling can format a refusal string, so zero allocation is not claimed. Original approved beam/barrel/hedge models already cover the new situation; actual batched ownership and movement are inspected by the native observer. New bespoke models are reserved for a mechanic that needs a new visible identity.

## Native generation and first live attempt

Native focused integration234/234 passed,0 skipped in129.9184665s (job `0766767c95d24680908b9b44e1384439`). The complete prospective version7 corpus contains15 selected sites; actual native rolls provide5 compatible beam/barrel sites and all5 commit, across seeds1/64/1729 as3/1/1. Three millstones and seven absent rolls remain ordinary refusals; no replacement roll or stock was added. This satisfies the frozen source floor. Native source maps differ from the private patched-hash runner's6 commitments.

First actual-input witness `69943f81cda740bca84e1617239611f1` reached seed64 `Overworld.15.9.0`, beam at(31,11), measured27-step bypass and predicted4-step clear crossing. Normal Haul cue, real free grab and first paid pull all passed; the observer then stopped after1 paid input on an overbroad immutable-state assertion. Source review and an executed private16-case probe isolate ordinary RenderPart.VisualFacing changes as the false premise (6 expected RED,10 controls). ID, weight, glyph and model are still pinned; this is an observer correction, not a confirmed gameplay hauling defect. Original failure/report/images are retained. The remaining live sequence is still unverified at this checkpoint.

Plain native generation timing (15 fixed addresses,3 repetitions each) confirms the candidate's cold cost rather than attributing it to the test census: committed median1252.83ms,maximum1426.75ms; returning to the same cached graph took at most0.0092ms. Repeated per-owner uniqueness scans inside exact-state validation are a plausible cause. One bounded optimization attempt may share ID counts within a single validation call; no cache or weakened check may survive a callback. Baseline45samples are retained under `Hauling/TimingBaseline`. This is first-entry chunk latency, not measured frame-rate degradation.


## Final local acceptance and regression freeze

Final native focused regression passes257/257, zero failures/skips in102.7367019s (job `1ef1c415cf1a4f1599009fac74c9aa9e`). This combines the234 generation/neighbor controls with16 actual-runtime observer-facing cases and7 exact receipt-validation controls. Final native census again commits all5 compatible sites across three seeds; the10 ordinary absent/millstone refusals remain unchanged.

The corrected actual-input witness `5464ae43d4904d95a806adad11ae4618` passes21/21 checks, zero failures,9 paid inputs in7.9396835s. At the same predetermined seed64 `Overworld.15.9.0`, the original beam moves from(31,11) to(29,11), the player completes two paid pulls and a four-step clear crossing, then leaves, saves the inactive graph, makes an unsaved map move, reloads, and returns to the same parked beam/open gap. Ordinary NPC scheduling remains active. Haul and LetGo are observed as free actual menu commands; the existing corner and submitted sidebar hint switch to the appropriate action. The original player is transferred once to the admitted approach for this local witness; no source, item, health, strength, RNG or clock is granted. All seven report/images are retained in `E4/HeavySalvage/Native/5464ae43d4904d95a806adad11ae4618/`.

**Can verify:** source identity, native input, actual reciprocal grip and speed, paid movement/facing, physical gap, retained inactive graph and reconstructed save, submitted approved model pieces, current corner/HUD glyphs, and teardown to the original Main scene in Edit mode. Root viewed obstructed, held, parked, crossed and loaded frames: the small brown beam relocates west of the hedge aperture and remains there after load; the local gap and changing action text are visible in the approved 3D presentation.

**Cannot verify:** ordinary discovery, distant awareness, user preference, every orientation's pixel readability, a long exploration journey, smooth dragging animation, or actual walking time for the27-step bypass. That bypass is measured geometry; only the shorter route is walked. The beam is small and dark at this camera distance; no stronger universal readability claim is made. The first incomplete observer attempt is retained rather than overwritten.

A bounded performance correction computes duplicate-ID counts once within each synchronous exact-state proof. Nothing survives an authority callback or enters a shared cache. Seven semantic controls were already GREEN before this optimization and remain GREEN afterward; their baseline is not misrepresented as RED. The same45 native samples preserve every disposition and entity count:15 committed samples fall from median1252.8275ms/max1426.7546ms to median497.4915ms/max956.7237ms. Optimized cache hits peak at0.0128ms. These single-machine cold-generation samples do not prove frame rate or a universal latency budget; the remaining near-second tail is documented rather than prompting a broad rewrite. Exact CSVs and comparison are under `Hauling/TimingBaseline` and `Hauling/TimingOptimized`.

### In-phase self-review, Q1–Q4

- Q1: first/per-move/final authority guards are symmetric; rollback restores only unchanged still-owned moves. Actual release removes only its grip penalty. Held, parked and removed graphs retain ordinary save semantics. Render-facing changes are validated separately from immutable identity and model facts.
- Q2: source receipts reuse existing factory/zone/revision authority; only fresh version7 worlds select appended HeavySalvage11. Literal versions2–6 remain pinned. Hints reuse the existing neighboring-cell query and actual command availability. No new stock, actor, reward or marker service is introduced.
- Q3: paired and dedicated24-case generation adversaries cover identity/decoys, mutation windows, refusal/rollback, full eight-direction utility, protected routes, ordinary source/RNG preservation and saved aftermath. Native16 facing controls and7 performance controls close the narrow observer/optimization checks. Detailed independent cold-eye review is retained under `Hauling/Review/`.
- Q4: this is original CoO exploration content, not source-verified Qud parity. The private hashed runner's6 sites and native5 sites are distinguished. The earlier existing-gap feasibility refusal is historical; explicit original-Hedge relocation is now the admitted design. Only one straight-pull layout with four orientations ships; a second decision-changing variant, new cover behavior and wider biome rollout remain open.

No unresolved significant source, duplication, route or saving issue was found in these local checks. Full native unfiltered EditMode job `895ee97feb1248bbb16e729428caab31` completed with21,862 tests:21,858 passed,4 failed,0 skipped in5789.7111409s. The41 owned Assets files remained frozen; all12 new script metadata GUIDs were checked and Objects.json remained byte-identical. All119 net-new tests passed. The exact name comparison has128 added and9 removed names because nine existing three-seed cases were deliberately renamed; no unexplained removal remains.

### Full-suite timing limitation and matched baseline

The full run failed the unchanged disabled-diagnostics microbenchmark (294.706ns/call versus the200ns threshold) and the three legacy Spread native-geometry census time limits (seeds64/1/1729:212.747/215.275/213.118s versus180s). The three census reports nevertheless completed all142 surfaces plus1/1/2 lair floors, with313232/313350/316241 owners and zero internal geometry/style failures. These fixtures use the two-argument legacy world constructor; they do not establish version7 placement coverage. The separate five-site native corpus and actual-input witness above provide that evidence.

A quiet four-test candidate repeat produced2 passes/2 timing failures in225.7953345s. Root then backed up every frozen F9 Asset and temporarily restored exactly their published `d0fb582c` preimages, including removing only the backed-up new files. The same four tests on those published sources produced2 passes/2 identical timing failures in230.933168s. The matched subset has **zero newly failing tests**: disabled diagnostics still exceeded its threshold, and seed64 legacy coverage took229.738s on published code versus224.456s on the candidate. Both census reports completed142 surfaces/1 lair floor with zero internal geometry/style failures. Their total owners differ (baseline313257, candidate313232); this is a timing comparison, not a claim of identical generated graphs. The25-owner difference is confined to an optional wayhouse at `Overworld.11.7.0`: baseline adds22 walls, crate, signpost, door and wayhouse anchor, while the candidate retains one additional Marlback. The candidate's exact blueprint multiset at that site also occurs in the published successful seed64 census `ecac1e05`; four seed64 reports already committed at `d0fb582c` lack this wayhouse. All three dedicated native wayhouse tests pass in the current full run. A new F9 legacy-path change was not identified; the census lacks refusal/position/RNG context needed to establish why this one baseline repeat differs. That fixture-state/realization question is explicitly deferred, not labelled proven randomness. Seeds1/1729 were not rerun against the same-session baseline. All41 candidate Assets were restored byte-for-byte afterward; refresh completed and the editor returned idle with no console errors.

The historical full baseline was21,743/21,743 passing. Accordingly, the raw full-suite comparison truthfully lists four newly failing timing cases; only the matched four-test repeat has an empty newly-failing list. No timeout threshold, diagnostic setting or editor background setting was loosened. The Mac was locked and the editor unfocused during the repeats; a causal platform explanation has not been established. Timing-harness/platform diagnosis is deferred because the published-code repeat reproduces both sampled failures and the gameplay/render assertions pass. This does not establish a timing-budget pass or general performance equivalence. Raw results, exact source overlay hashes, paired comparison and compressed census reports are retained under `Hauling/Integration/` and `SpreadBiome/NativeCoverage/`.
