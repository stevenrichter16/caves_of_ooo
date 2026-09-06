# Mechanics flow and responsiveness — 2026-09-05

Status: WAVE1 IMPLEMENTED/REGRESSION-VERIFIED (LIVE MOUSE UNVERIFIED); WAVE2 COMPLETE; WAVE3 COMPLETE; WAVE4 COMPLETE; WAVE5 COMPLETE; WAVE6 COMPLETE; CRAFTING AVAILABILITY/FIRST-USE REPAIR COMPLETE; CRAFTED OUTPUT/LOCAL ROLLBACK REPAIR COMPLETE; CAMERA CLEANUP APPLIED/VERIFIED; NEW-GAME CHECKPOINT REPAIR COMPLETE; HOTBAR SAVE SELECTION REPAIR COMPLETE. User explicitly requested identifying mechanics that
need smoother integration and fully implementing the supported improvements, without
intervention. This extends the ongoing whole-game audit; it does not cancel its
remaining repairs. Baseline latest completed repair1ef093a1,8470tests,GA02i native44.

## Outcome and scope

Make ordinary actions easier to complete and understand: consistent controls,
selection continuity, useful refusal feedback, fewer unnecessary repeated menu steps,
and measured rendering/input responsiveness where source evidence identifies a cost.
Choose bounded changes from actual game producers/consumers and native flows. Keep
intentional resource costs, station roles and tactical rules unless a documented
behavior contradicts the intended mechanic. Do not mistake future lore/content for
a disconnected or broken feature. Existing protected spell/art work stays protected.

## Evidence under review

| Existing evidence | Candidate smoother behavior | Next verification |
|---|---|---|
| A23: action-key collisions and reserved navigation keys in crowded menus | Every shown shortcut dispatches the shown action consistently | Verify shared draw/dispatch mapping and actual authored crowded choices |
| A24: inventory failures can close an action popup and put the reason only in the console | Keep context and show an actionable failure reason where the player acted | Inspect popup state, user-visible feedback, retry and successful close paths |
| A48: equip splitting creates null-ID items later omitted by station/individual target pickers | Split items remain selectable throughout normal equip/unequip/drop/crafting flows | Actual split producer, save bounds and fresh-ID tests |
| Completed GA02h native: batches, split/remerge, one-quench and mark continuity | Preserve truthful recipients and selection through follow-up actions | Already verified; do not duplicate a completed repair |
| A25: HotbarRenderer rebuilds cells/maps every frame | Avoid unnecessary unchanged-frame work while preserving immediate updates | Measure baseline in real rendering, define invalidation contract,75s verification |
| A34–A37: movement/target dispatch inconsistencies in skills | Previewed target, actual movement and action cost agree | Validate each actual skill entry/countercheck before selecting scope |

These are evidence-backed candidates, not implementation claims or a final set of
recommendations. A delegated independent read-only review is identifying the smallest
coherent changes and exact files/tests/native checks. Root continues GA02i in parallel.

## Delivery gates

For each accepted change: disk plan and reference correction table; actual RED and
positive control; minimum implementation; dedicated adversarial and independent
cold-eye review; real-control scenario for player-visible behavior;75second measured
sweep when altering ordinary per-frame/per-turn work; full suite and same-commit docs.
Record verified improvements in WORK-LOG-2026-09-05.md as they pass. Report observed
input/state/frame behavior separately from visual feel that headless checks cannot
judge. Plans will be implemented under the user's standing authorization.

## Accepted implementation sequence — source review complete

All six bounded improvements below are accepted under the user's request to fully
implement supported suggestions. Start after GA02i close-out, smallest shared surface
first; keep the remaining whole-game audit repairs active. Performance candidate A25
still requires measurement before any optimization claim.

1. **One selected component per slot.** Crafting panel currently toggles marker Parts
   directly; it can mark Steel and Iron together. Panel preview chooses first, station
   CraftKit chooses last. Route through existing ToggleCraftMarkCommand radio behavior.
   RED: chooseSteel→Iron and require onlyIron/preview/product/payment agreement; same-row
   unpick, independent slots and multiple reagents are controls. Actual keyboard native.
2. **Crafting owns its pointer input.** InventoryUI has no crafting branch in generic
   hover/click. A visible Oak row at grid13,9 overlaps hidden equipment Head geometry
   and can switch panels on hover. Share visible row geometry (including section
   spacers/scroll) between render/hit testing; keep headers/result area inert.
   RED exact coordinate+hover/click, spacers/scroll and actual equipment controls.
   Native mouse needs real legacy pointer input, not an assumed queued keyboard proof.
3. **Visible failure with a retry.** Preserve failed item popup context, render the
   reason, and clear it on successful retry; align panel forge/brew/tinker status.
   Actual full-container put/refusal→make-room→retry pins state/payment/UI. Pickup and
   trade already have status surfaces; preserve them rather than duplicating.
4. **Shortcuts match their labels.** Shared row-key mapping excludes reserved controls
   and serves both label generation and dispatch. Ordinary7+loot exposes G collision.
   Correction: Elder_Well_1 has12authored choices but mutually exclusive predicates
   cap authored visible rows at7; automatic Attack makes8(+optionaltrade9); it does not prove ordinary J/K
   reach. Large dialogue fixtures must be labelled staged controls. Preserve reveal
   behavior (first key reveals), navigation, close keys and exact item/choice selection.
5. **Split items remain addressable (A48).** Assign fresh clone IDs; separately scope
   missing-ID old-save repair. Test actual equip→unequip, forged station and dropped
   pile pickers, save reference graph/owners. Entity.cs/SaveSystem.cs are protected;
   use only proven incremental hunks, never adopt prior unrelated work.
6. **Direct “Separate one” action.** No current UI split row exists; modification
   singletons are reachable through equip/unequip, but that detour is unnecessary.
   Append one carried unit without immediately merging it back, preserve total mass,
   quantities, ownership and unique IDs; retain current mod costs/caps/singleton gates.
   Nonowners/singletons refuse, overweight separation remains non-increasing, rollback
   restores exact originals. Actual native separate→Sharp leaves sibling unchanged.

First wave targets only InventoryUI.cs/InventoryUI.Crafting.cs and existing toggle
command consumers; both UI files are git-clean and outside protected manifest.
References verified by independent reader: crafting toggle263–273, first picked
component160–165 versus ForgePart155–169 last-match, InventoryUI288/323–368/490–509
mouse paths, InventoryScreenData350–405 Head placement, crafting413–431 rendered
spacers. Mod singleton gates: Sharp40–44, armorutility30–35, mineral113–117.
InventoryUI1218–1312 has no split verb; DropPartial has no current UI caller. These
are source-confirmed candidates for actual RED, not claimed native results yet.

## Wave1 pre-implementation verification corrections

Root read InventoryUI Open/Rebuild/input/hover/MouseToGrid/render, full crafting
build/input/list rendering, shared ToggleCraftMarkCommand and actual ForgePart
CraftKit. Read PERF-FOUNDATION before touching pointer's ordinary frame path.

| Correction / constraint | Implementation and test consequence |
|---|---|
| Craft headers insert a blank row before later sections | Share37-row logical-index map across rendering/hit testing; x47 arrows and result area inert |
| Current scroll clamp counts logical rows rather than drawn heights | Keep selected row visible using actual map, including section spacers |
| Unconditional hover would undo keyboard movement under a parked pointer | Gate crafting hover on changed pointer grid; clicks always resolve current visible row |
| Tests need not synthesize legacy mouse to reproduce old hover | Offset a private fixture MainCamera so the actual current pointer maps to the desired grid; this is EditMode evidence only |
| Project has both input backends, but InventoryUI pointer reads legacy only | Native mouse must be verified through real desktop input; do not claim queued MouseState reaches legacy |
| Craft panel and popup marks share exclusive-group command | Reuse command instead of duplicating radio rules; reagent multi-selection stays unchanged |

Plan no per-frame collection allocation: allocate37-entry row map once per UI;
rebuild only on data/mode/scroll changes; hover scans a bounded map entry, redraws
only after an actual selection change. Record75second native ProfilerRecorder
sweeps before and after changed pointer/render work. Metrics demonstrate actual
work and spikes, not subjective feel or a promised speedup. Existing keyboard,
station, popup and inactive-panel controls remain covered. Both target UI files
remain outside the protected manifest. User explicitly authorized implementing
these supported corrections without intervention.


Wave1 initial22RED:2pass/20fail,03:04:54–55UTC,zeroC#errors. Actual
radio/ownership/hover failures reproduced alongside not-yet-authored geometry/click
helpers. Equipment countercheck used nonexistent Label; corrected to actual
ShortLabel. Added8native staging cases before benchmark/driver implementation.
Baseline now1ef093a1,8470GREEN. No production UI change yet.


Wave1 staging30RED:3pass/27fail,03:06:49–50UTC,zeroC#errors. Bench then
implemented without UI changes. First75second native baseline failed its compound
check but omitted measurement diagnostics before throwing. Retained raw report/log;
strengthened harness to write metrics/frame/phase and observed-vs-attempted input
counts before checking, increased bounded capture capacity, and rerun baseline.
This is an invalid baseline until the cause and workload are established.

Independent cold-eye found a real legacy-state gap: old crafting UI could save two
marked blades/quenches. A new radio command alone leaves that already-saved kit
ambiguous on opening. Added2REDcases before implementation; normalize exclusive
marks while rebuilding crafting rows, retaining the last inventory entry to match
existing station selection and the exact survivor Part. Reagents stay multi-select.
No save-format change. This bounded repair remains within the two UI files.

Wave1 legacy32RED:11pass/21fail,03:14:09–11UTC,zeroC#errors; all8
staging cases now pass. Both old duplicate-mark cases reproduced. Performance
recorder metrics use nanoseconds for script markers and bytes for whole-frame GC;
raw capture includes temporary harness allocation/input costs and a one-frame
sampling phase offset. No UI-only allocation or absolute speedup claim.

Wave1 valid pre-UI baseline:75.00008825s,360885frames,70/70navigation and
68/68toggle changes; all recorder handles valid, panel4 retained,exit0,0C#errors.
Frame count demonstrates prior120000capacity insufficient for this workload; new
bounded500000capture retained raw timestamps/phase and whole-frame samples. Input
max1.078208ms,inventoryRendermax0.948875ms;GCmax36.29MBincludes capture setup.
Minimum two-file UI implementation follows this verified baseline.

Wave1 minimum172/172GREEN,03:17:00–06UTC,zeroC#errors. Two UI files now
share actual37line layout, radio command ownership/selection, last-marked legacy
normalization and moved-grid crafting hover. Added dedicated adversarial matrix
covering long-list spacers, exact-fit overflow, rendered tiles, stale ownership,
legacy continuity, batch quench and unchanged-hover redraw/allocation gates.

Wave1 dedicated+neighbors207/207GREEN,03:20:25–33UTC,zeroC#errors.
35dedicated cases include actual rendered selection tiles and zero-allocation/zero-
redraw parked hover. Review strengthened batch-quench assertions to count tempered
and plain UNITS rather than entities (the latter could miss a whole-stack bug).
Wording correction: last-marked normalization matches existing component station
order; duplicate quenches previously caused no quench, so last-marked quench is
the explicit canonical repair policy, not a claimed pre-existing station winner.

Wave1 native20/20PASS,75.000117583s measured,363350frames,69/69navigation
and69/69toggle actions. Added2token-graph roundtrip controls for legacy mark
normalization; initial desktop launch stopped on wrong test-helper namespace.
Raw3CS0234loglines retained; corrected actual helper namespace before relaunch.
No desktop pointer outcome claimed from this compile failure.


Desktop pointer run14b0f2774cc840059c0fbe48574a2bb8 stopped at crafting-tab
preflight (2observations,1failure), before any pointer exercise. Raw lacked the
intermediate panel value, so no exact failure cause claimed solely from that log.
Inspection identifies same stationary-hover snapback in the Equipment/Inventory
and popup paths. Added3REDcases (both panel directions, popup cursor), with real
movement return controls. Extend moved-grid guard to the whole inventory hover
handler; click dispatch remains independent. Added each-tab native checks to make
any repeated launch failure diagnostic. This is a same-file flow-smoothing scope
extension prompted by actual desktop setup, not a waived gate.

Wave1 hover72RED:69pass/3fail,03:30:31–34UTC,zeroC#errors. Both parked
Equipment/Inventory tab directions and popup keyboard cursor reproduced snapback.
Promoted unchanged-grid return across hover after cache update; direct click paths
continue independently resolving hits. New save-graph controls passed in this run.


Wave1 hover+neighbors218/218GREEN,03:32:26–35UTC,zeroC#errors.
Second desktop run1953714aaee64a86a10c2335873aad16 failed first simulated
Tab preflight, before pointer. Retained raw; this does not invalidate the actual
RED→GREEN hover fixes or prove their cause was the desktop failure. Desktop mode
now observes actual CUA keyboard and mouse input throughout, with no virtual
keyboard device, queued keys or reflection-driven selection. Headless native uses
its separately verified virtual keyboard path. Final pointer gate remains open.

Desktop real-input run33de6be240c74a7d9fe2f019636c3cac timed out during
root context recovery before Crafting; retained raw248.43994425s,1failed
observation,0C#errors. Rerun d97db4f4b22541a4ad956243ffebad2b reached
Crafting with actual CUA N,I,three separate Tabs; SpaceSteel→Down→SpaceIron
visibly and observably replaced Steel without an intervening clear. That state
check passed. Pointer scroll/right-click/left-click/drag moved the visible cursor
but legacy Input.mousePosition remained1881,894 (grid79,7), never reaching
Oak13,9. The180second pointer phase therefore failed honestly:2observations,
1failure,252.649266458s,0C#errors. This is NOT a passed native pointer gate.
Project Both input backends and new-system focus settings do not establish legacy
mouse event delivery. Cause beyond that boundary remains unproven. Retain the
manual observer and explicit live limitation; keyboard native, drawn tile geometry,
click handler and hover counterchecks provide the verified coverage. No claim that
CUA moved the game pointer, or that raw state alone proves keyboard provenance.

Cold-eye observer fixes: inner Oak phase previously transiently emitted complete
before forge; renamed oak_click_complete. Desktop phase allowances total540s, so
launcher now permits600s including bootstrap (headless remains360s). Removed two
unreachable pointer conditionals from the headless branch. These are harness
corrections, not game-mechanic fixes. Final post-global-hover75second capture and
full suite follow. Protected scene recovery was preserved when Unity offered it.

## Wave2 — visible failure and contextual retry (source sweep complete)

Goal: a refused action keeps its menu and shows the reason at the bottom of inventory;
a retry succeeds once and clears the warning. Runtime remains in the two UI files.
Existing command payment/rollback/target rules remain authoritative. This is CoO
interaction consistency, not a new Qud-parity claim. Content readiness is green: actual
Sack(MaxItems6), Dagger, seeds, forge/brew/tinker content already ship with sprites.
No new content or art is needed. A47 selection/receipt correctness remains separate.

Root read ExecuteItemAction, command helpers, popup/body/displacement completion,
RenderDetailLine and Forge/Brew implementation alongside PutInContainerCommand and
GA02c's actual native route. Independent source review supplies counterchecks.

| Source correction | Planned behavior/test |
|---|---|
| Popup closes unconditionally despite bool helpers | Close only after success; retain exact item/action/cursor on refusal |
| Command helper failure is console-only | Shared UI status prioritizes detail row44 without changing legend43 or hit geometry |
| Full Sack means six distinct entries; merge may still succeed | Use six actual distinct fillers plus Dagger; full-merge positive control, no UI fullness preflight |
| Put menu discovers same-cell containers | Actual same-cell Sack for native and menu tests, not adjacent-only fixture |
| Equipment/body/displacement finish paths also discard bool | Apply outcome handling symmetrically; preserve subpicker/throw/examine handoffs |
| Mod target popup already stays open on refusal | Add visible reason and success clearing; no redundant retention rewrite |
| Forge commits before independent optional quench | Partial status explicitly says forging succeeded/quench failed; do not imply refund or quench-only Enter retry |
| Brew UI excludes invalid/mishap/sludge previews before command | Preserve that gate and resource behavior |
| Batch commands can succeed partially with empty ErrorMessage | Clear prior refusal without inventing a full-request success count |
| Generic item Parts may not expose detailed rejection | Show command result/fallback; do not scrape global log history |
| Historical GA02c driver asserts failed popup closes | Update reusable driver assertion/Escape count; retain archived historical raw evidence |

Status survives redraw/rebuild within the same context, clears on success, inventory
open/close or deliberate context change. New action replaces the previous outcome.
Counterchecks cover full→make-room→retry, full-merge, locked→unlock, failed planting,
missing forge picks, off-station batch versus valid single, invalid brew, tinker capacity
refund/retry, missing Sharp bits, forge/quench partial outcome, and actual status tiles.
Native uses the proven GA02c world-pick→open Sack→take filler→return to put flow; first
assert exact retained popup and visible status before leaving to make room. Dedicated
adversarial and cold-eye gates follow minimum GREEN. No added per-frame scanning: UI
status is set at actions and drawn with existing renders; if scope changes into frame
work, measure the required75second baseline first.

Wave1 final:8542/8542GREEN,03:53:43–03:55:28UTC,105.0671965s,0C#errors.
Native24/24PASS,775c6f2fbb674d21a6700a924b584b5c,75.000108s measured,
366913frames,70/70navigation,69/69toggles,exit0,no logged native exceptions.
Two-file runtime scope,72tests(24regression/40adversarial/8staging),independent
review complete. Actual desktop mouse delivery remains unverified, with failed
raw evidence retained. Wave2 begins against this completed regression baseline.

FLOW2 first compile stopped with2unique CS0246errors(6loglines): paid Sharp
mod uses ModSharp tag/penetration/count, not a Part named EnhancementSharp.
Independent review also corrected seed menu command PlantSeed versus label Plant,
and DrawText omits space tiles. Raw compile retained; corrected fixtures rerun
before any production implementation. No valid XML claimed from compile failure.

FLOW2 corrected24RED:24fail,04:03:04–06UTC,0C#errors. Actual popup
closure/status rendering failures coexist with missing-status assertions in positive
controls; this is not24distinctbugs. Added10symmetry cases before implementation
for equip-slot/manual-body/unequip/displacement completion, using real actor veto
hooks with blocked/unblocked controls and exact originating context on retry.

Symmetry fixture compile correction: Part overrides HandleEvent, not FireEvent;
BodyPart lives in Core.Anatomy. Retained3CS0506loglines and corrected before
rerunning34RED. No production change or valid XML from the failed compile.

FLOW2 symmetry34RED:34fail,04:06:48–50UTC,0C#errors. Confirmed both
origin popups and displacement confirmation are discarded on refusal. Minimum
implementation now retains exact context on false outcomes and displays shared
status at detail row44. Status context checks run only with existing renders;
no new input polling or per-frame collection work. Shared command bool signatures
and all payment/target rules remain unchanged. Composite quench wording is left
for its dedicated RED→fix follow-up, not silently treated as covered by these34.

FLOW2 minimum143/143GREEN,04:09:10–15UTC,5.2397952s,0C#errors.
Dedicated matrix now probes repeated refusals, exact equipped transfer retry, reason
replacement/fallback, mode/context boundaries, handoffs, missing dependencies, partial
batches, committed forge/unpaid quench and stale modification/crafting selections.
Independent review found crafting-panel mark failure still bypasses shared status;
added RED before routing it through the existing bool helper. Popup/craft cursor
movement preserves context; different item/recipe/equipment slot or mode clears it.

FLOW2 dedicated66RED:62pass/4fail,04:13:49–52UTC,0C#errors. Three
confirmed feedback gaps: stale crafting marks, successful mark clearing, and committed
forge/unpaid quench status. One test false premise: consuming the final unit removes
the Entity but preserves its detached StackCount1; assert inventory absence, not0.
Use actual checked one-unit preconsumption for the cached partial-batch setup.
Minimum fixes route crafting marks through shared feedback and label the partial
forge outcome explicitly. Added6further controls for tinker capacity refund/retry,
mod target availability and partial/full brew. These are scoped UI observations, not
new A47/G41 transaction guarantees.

FLOW2 dedicated+neighbors181/181GREEN,04:16:10–17UTC,7.503607s,0C#errors.
34regression+38dedicated cases now pass. Added8native staging checks before bench
implementation. Bounded native copy is necessary: prior benches/drivers are sealed
and Apply starts their keyboard driver, so wrapping would create competing inputs.
New combined arena uses full Sack→actual loot repair→put retry; incomplete forge,
invalid FireMoss brew and missing B bit each get real keyboard repair/retry. Native
Sack retry reopens the menu; same-instance retry is EditMode coverage. Success may
restore ordinary red detail (for example no bits for another craft); empty action
status does not promise a blank/non-red footer. Final75second measurement will use
FLOW1's last committed75second run as the immediately preceding UI baseline.

FLOW2 staging80RED:72pass/8missing-scenario failures,04:18:30–33UTC,
0C#errors. Final cold-eye adds one real C-key status-lifetime hypothesis before
fix: explicit Clear picks currently keeps stale failure while successful Pick
clears it. Dedicated RED/control precedes the one-line action-entry correction.

FLOW2 clear39RED:38pass/1failure,04:19:36–39UTC,0C#errors. Clear picks
now clears old status at action entry. Added combined native arena/driver/launcher
after8staging REDs; it only observes UI state via reflection and drives actual input.
Historical transfer driver now expects retained refusal and two Escapes; its new
reruns write FLOW2-transfer-native.json, preserving the original GA02c evidence.

FLOW2 final focused241GREEN,04:22:54–04:23:01UTC,7.2221512s,0C#errors.
Combined native51/51PASS,3906820dbf0e446ea646329a926696ab,90.842086042s,
exit0,no logged exceptions.75.165754333s measured,363171frames,70/70navigation,
69/69toggles. Existing rendered failure glyphs/legend were observed after real inputs;
actual Sack loot and disassembly repaired capacity/bits before retry. Raw/phase
comparison retained. Compatibility rerun and full suite follow; no speedup claim.

FLOW2 COMPLETE: historical transfer compatibility35/35PASS,
c688c7e8fa8c44bd8865f2e81da46865,10.5798875s. Full8623/8623GREEN,
04:28:14–04:30:03UTC,108.4742895s,0compiler errors. Added81tests.
2380unique asset GUIDs,0collisions;37owned paths with0protected overlap.
Next FLOW3: shared truthful shortcut mapping and actual activation release gating.

## FLOW3 — truthful menu shortcuts

Status: PRE-IMPLEMENTATION SWEEP COMPLETE. Baselinebb9b785e,8623GREEN.
CoO-original control repair; no Qud parity claim. Existing content/art sufficient.

| Premise/reference | Verified contract / correction before implementation |
|---|---|
| PickupUI HandleInput/Render | Row7 G closes first; rows10/11 J/K navigate. Actual producer accepts any2+takeable ground items. Seven distinct carried-and-dropped shipped items reproduce it. |
| Container loot and ContainerPickerUI | Chest holds10 distinct entries; Sack6 cannot prove seventh-item loot. Seven nearby containers are a staged crowd control, not claimed ordinary generated placement. |
| DestructiblePart GetInventoryActions | Break advertises K, consumed by world-menu navigation. Indestructible flag removes the action. |
| AlchemyStillPart GetInventoryActions | BrewMix b and BrewMixBatch B case-fold to the same key. Real marked GlimmerBrine3 supports single then batch payment. |
| ForgePart | f/F collision exists in actorless fallback; not claimed ordinary actor-aware forge defect. |
| WorldActionMenuUI Open/HandleInput/Render | Preserve original action refs/order/raw Key. Reserve valid unique preferred letters before filling missing/duplicate/reserved rows; CraftNoop headings stay unlabeled. |
| InputHandler HandleWorldActionMenuInput/ExecuteWorldActionSelection | Capture actual letter activation before ConsumeSelection. Set release gate there; retain the existing4-argument execution seam for callers/tests. Enter/keypad/mouse have no letter release gate. |
| DialogueUI reveal/dispatch/render | Three consumers must share positional mapping; ChoiceData has no shortcut field. First mapped press while revealing only finishes text. |
| Elder_Well_1 + ConversationManager.RefreshVisibleChoices | At most7 authored visible plus automatic Attack=8, optional trade=9. Ten-row dialogue regression is staged9choices+Attack, not a twelve-visible ordinary claim. |
| EditModeTests.asmdef/InputHelper | Add explicit Unity.InputSystem reference for actual queued keyboard HandleInput tests. Save/restore input settings and keyboard; reflection observes UI state only except fixture staging. |

Implementation snippet/contract: shared internal MenuShortcutMap positional alphabet
excludes GJK for pickup/container and JK for world/dialogue. Positional lookup uses
absolute list index, returns no key beyond alphabet capacity; no per-frame collection.
World map is rebuilt only on Open, first reserving valid authored preferences and then
assigning fallback keys in row order. Display, dispatch and dialogue reveal all consume
the same mapping. Leave exhausted rows available through existing navigation/Enter.
Pickup hint becomes [key]take, with existing Tab/Enter/Esc/G functions retained.

Milestones: actual RED dispatch+render/control tests; minimum implementation;20–60
dedicated adversarial cases and independent cold-eye; native pickup/Break/still/held-key
route with raw observations; full suite and same-commit living documentation.
Expected touched runtime: four menus, new helper and two small InputHandler hunks.
InputHandler contains protected spell work: save current bytes and stage only our diff.
No blueprint/art/save/payment rules changed. No arbitrary action reordering.

Scope-prune: other menus' existing parked-hover behavior stays recorded for a later
smoothing wave; native keyboard parks pointer outside. Existing dialogue10-row draw
limit/absent scrolling is separate debt; this wave repairs shown shortcuts, not a new
dialogue layout. Keyboard correctness does not prove subjective smoothness or desktop
mouse delivery. No performance speedup claim; mapping has no new frame allocation.

Pre-implementation self-review: 🟡 release gate, preferred-key starvation and revealing
dialogue are explicit tests; 🔵 ordinary reach paired with staged boundary controls;
⚪ large dialogue/crowded-container fixtures labeled accurately.

FLOW3 first RED21:1pass/20fail,04:42:22–23UTC,1.3201951s,0compilererrors.
Rendered G/J/K/Break collisions reproduce. Fixture correction before runtime work:
Unity InputManager.ShouldRunUpdate skips Manual player updates outside PlayMode;
queued EditMode keyboard must use InputUpdateType.Editor. Existing uppercase B glyph
is not a lowercase shortcut; the test reads no lowercase key, correctly rejecting it.
Independent review additionally found closing pickup/container/dialogue can leak a
held selection letter into normal movement. Source confirms they bypass the existing
world release gate. Add paired real dispatch/held/release REDs before extending the
same activation provenance to all four affected menus; no claim of other UI coverage.

FLOW3 fixture correction: raw Editor updates change values but do not advance the
device player-step counter used by wasPressedThisFrame (InputManager4155/4165,
InputDevice368,ButtonControl283). Retain three failed fixture reports plus compile
log; none proves dispatch. Use Unity's public composed InputTestFixture, which saves
and restores the input runtime and provides isolated queued player updates. Added
explicit Unity.InputSystem.TestFramework test reference; no production input override.
Actual queued input must pass its precondition before any gameplay assertion counts.

FLOW3 valid dispatch RED27:11pass/16fail,04:49:48–50UTC,1.650369s,
0compilererrors. Input preconditions now pass. Three closing-menu held A cases
actually moved from10,10 to7,10; paired Enter cases remained stationary and fresh
movement controls passed. World Enter incorrectly gated authored K; remapped A had
no selection. Dialogue J revealed instead of navigating, and L failed to select.
Rendered G/J/K/Break and still case-fold collisions confirmed. Minimum shared-map
implementation now follows these REDs; held-key capture extends to all four menus.

FLOW3 minimum52:50pass/2fixture expectation failures,04:50:51–53UTC,
1.7166655s,0compilererrors. Pickup excludes G as well as JK: absolute rows9/10
(zero-based) map M/N, while dialogue row9 maps L. Correct the test arithmetic;
production mapping and actual closing held-key controls pass. Positional input
loops stop at alphabet exhaustion, preserving the prior bounded per-frame scan.

FLOW3 dedicated+neighbors83GREEN,04:51:56–04:52:00UTC,3.4494628s,
0compilererrors.27regression+31adversarial cover data identity, two-pass reservation,
case folding, navigation, scrolling/exhaustion and held-key handoffs. Native staging
66RED:58pass/8missing-bench failures,04:54:35–38UTC,3.1478145s.
After staging RED, added disposable real-drop/seven-Sack/Chest/still/staged-dialogue
scenario and native driver. Its75second workload uses actual world-menu navigation
and Examine→reopen. Prior FLOW2 capture is historical context, not an identical
workload or causal performance comparison; no speedup claim.

FLOW3 focused172GREEN,04:58:18–25UTC,7.3841479s,0compilererrors.
Native first run2563b1ad1c8a4aa0823e65cb5121ae8d passed11checks, then audit-only
observation threw: Chest structural HP lives in DestructiblePart.HP/MaxHP, not a
creature Hitpoints Stat. Real pickup keys and held Break selection passed before
that faulty observation. Correct observer; retain failed JSON/log including known
A31destroyed-camera teardown. This is not a production damage defect.

Native secondbc9f33dd1af545d1a536c9a570a14a15:19pass/1failure,4.920816458s,
0compilererrors. Driver wrongly reopened after CraftToggle, whose real InputHandler
branch already reopens the station. Its extra C correctly dispatched the newly shown
C toggle and unmarked Glimmer, so Brew accurately refused empty mix. Remove redundant
interact sequence and assert reopened station; NPC interaction likewise needs actual
Chat menu action before dialogue. These are driver route corrections, not changed
command rules. Failed JSON/log retained.

Native third40f3b50cb5a54eb3a98eb21d6f0e8f59:43pass/1route failure,
11.118388458s,0compilererrors. Actual single/batch payments, mapped dialogue
reveal/attack confirmation and benign closure, same-cell container selection, held
keys and fresh H movement all passed. Driver used fresh L to return east, but
InputHandler612 opens Look before its held vi-L movement branch. Use unambiguous
RightArrow for the route; record the existing vi-L/Look collision in the broad audit.
No production remapping of normal Look controls is included in FLOW3. Raw retained.

## FLOW4 — split-item addressability (next wave, pre-implementation)

Status: SOURCE SWEEP COMPLETE; implementation waits for FLOW3 close-out. Accepted
step5/A48. CoO-original repair; no Qud parity claim. Existing item art/content sufficient.

| Reference/premise | Source-confirmed contract / correction |
|---|---|
| Entity.CloneForStack405–443 | New Entity has no ID; AddPart calls Initialize before return. Allocate GUID-N at construction, before these hooks. Source identity stays unchanged. |
| StackerPart.SplitStack/RemoveOne | Positive split clones; singleton RemoveOne returns same Entity. Invalid SplitStack leaves source intact. Do not alter quantity semantics or unknown-payload cloning. |
| EquipCommand/InventoryPart.UnequipFromBodyPart | Equip of Dagger2 splits one; unequip appends without merging, so a normal null-ID carried singleton is reproducible without Sharp. |
| ForgePart/CraftingMarkPart.AddToggleRows | Station weapon rows require nonempty IDs. Dagger proves temper picker reach, not reforge eligibility; real ForgedWeapon must prove reforge. |
| WorldInteractionSystem individual picker | Skips null/empty IDs and re-resolves opaque string IDs. Drop split item and exercise exact PickTarget command. |
| WeaponCraftingOperation.PrepareUnit74 | A46 already assigns GUID-N after cloning. Central allocation makes this second assignment redundant; remove it so Initialize and caller see one identity. Do not claim A46's already-working target reach is newly fixed. |
| SaveGraphSerializer.LoadEntityBody767 | Repair null/empty immediately after saved ID read, before parts/hooks. Placeholder-only assignment would be overwritten. |
| SaveReader tokens / FormatVersion7 | Tokens restore aliases/owners/cycles independently of ID. Preserve v7 and every nonempty ID, including whitespace/custom/numeric/GUID. Not a migration of unsupported versions or duplicate nonempty IDs. |
| PartRoundTripHelper | Use token-graph helper for OnAfterLoad/FinalizeLoad and owner cycles. Save writer must not mutate legacy sources. Independently loading old missing-ID bytes may mint different IDs; stability starts with saving repaired state. |

Minimal implementation: clone initializer adds fresh GUID-N; remove A46 overwrite;
load null/empty repair adds GUID-N. Three small runtime hunks, no factory numeric
counter changes, no ID requirement in stack compatibility, no saved fields/version.
Entity.cs and SaveSystem.cs contain protected spell changes; stage only our incremental
hunks. WeaponCraftingOperation is owned prior repair code.

TDD matrix: fresh identities available during initialization; split/RemoveOne and
invalid/singleton controls; actual equip→unequip, partial/whole drop and stack/singleton
throw; actual station temper/reforge and world resolution; rollback and merge identity;
legacy missing IDs, aliases/cycles, opaque IDs, writer immutability and second-roundtrip
stability. Then20–60dedicated adversarial cases, independent cold-eye and native
exact-item station selection/transformation/drop/picker. Existing direct inventory
actions already use references and are not claimed newly repaired.

Scope-prune: arbitrary clone payload aliases, duplicate nonempty IDs and v8 migration
remain separate. Direct Separate one is accepted step6, after this identity repair.

FLOW3 intermediate native passed and captured75seconds (raw archived separately).
Independent review found reopen counter only observed final open state; strengthen
it to require closed/Normal after E and exact Still reopened. Rename legacy toggle
fields to reopen. Add final-item pickup letter hold natively; intermediate Tab-all
proof remains archived. These are verification improvements before final close-out.

FLOW4 additional sweep correction: existing Tier3EntityReferenceRoundTripTests bare
helper intentionally leaves referenced entities as empty placeholders, because no
referenced bodies are read. Body-read-only ID repair preserves that contract. The
BurningEffect null-source test concerns a null entity reference, not an empty ID;
null tokens remain null. Do not broaden migration to placeholder creation.

Strict-reopen run88704dcaff744870a5677e364f57c94a:47functional checks passed,
measurement failed correctly.75.001341458s,79273frames,68/68navigation but
0/46confirmed reopens. ExaminablePart63 authors X, not the driver's assumed E.
The old final-open-only counter had indeed false-passed. Read actual rendered
Examine binding before measurement, retain closed/Normal→exact-Still checks.
Failed JSON/perf/CSV/log retained; earlier intermediate reopen count is invalid
evidence and is not used for close-out. No runtime change from this correction.

FLOW3 final native50/50PASS,fd51189b03b04facbfb18f9406646008,
88.335126666s,exit0,0compilererrors,no logged exceptions.75.217160291s,
78823frames,70/70navigation and46/46actual closed→exact-Still reopened cycles.
Input max.782958ms,p99.004208ms; whole-frame GCmax8,706,710bytes.
All four menu closing-letter paths now have native hold/release checks.2387unique
GUIDs,0collisions. Full suite running; independent observer finding fixed.

FLOW3 COMPLETE: full8689/8689GREEN,05:13:34–05:15:32UTC,118.2246955s,
0compilererrors. Added66tests (27regression,31adversarial,8staging). Native50PASS,
final75second workload70navigation/46confirmed reopen cycles.2387uniqueGUIDs,
0collisions. FLOW4 next, with source corrections/plan already recorded above.

FLOW4 initial RED:32tests,13passed19failed,0compilererrors,
05:19:28–05:19:30UTC,1.7655235s. Raw FLOW4-red.xml.gz retained.
18 cases expose missing identity; one singleton reforge failure is a fixture
mistake: the two-out TryReforge overload returns the displaced component,
not the affected weapon (WeaponForgingService203–211). Use the three-out
overload and assert both exact weapon and returned SteelBladeComponent.
Replace NUnit IsNotEmpty(null) argument errors with explicit null/empty
identity assertions, then rerun unchanged production to retain a clean RED.

FLOW4 corrected RED:32tests,14passed18failed,0compilererrors,
05:24:57–05:24:58UTC,1.8201597s. All18failures now assert missing identity;
14 controls already pass. Apply the three planned runtime hunks next.

FLOW4 minimum100/100GREEN,05:25:56–05:26:00UTC,4.3057709s; dedicated
31adversarial plus neighbors166/166GREEN,05:28:12–05:28:19UTC,7.4533531s.
Both0compilererrors. Native staging tests now precede its scenario. Independent
review requested raw carried/equipped counts before Distinct in the save-cycle
check; strengthened to2carried/1equipped. No production finding.

FLOW4 staging RED9/9expected missing-scenario failures,0compilererrors,
05:29:05UTC,.3032237s. Native scenario/player/editor launcher added afterward.
Independent review: no runtime finding; four stronger save cases increase dedicated
matrix31→35. Raw counts and rollback wording corrected. Expanded focused run active.

## FLOW5 — direct Separate one (source sweep complete; after FLOW4)

CoO-original usability improvement, accepted step6. Existing Dagger/Sharp art and
recipes are sufficient; no new content, Part, station requirement or save format.

| Source/premise | Verified correction and decision |
|---|---|
| InventoryPart.AddObject51/AddCraftedUnit | Both merge immediately. A narrow transaction-owned append without merging is required to retain an unchanged singleton beside its source. |
| InventoryPart.Contains475 | Includes equipment. Require actual Objects membership and exclude equipped references; missing/singleton/nonowner requests refuse. |
| StackerPart.SplitStack100 | Decrements/refreshes before cloning. Prepare clone before mutation, then revalidate; initialization exceptions leave source unchanged. |
| InventoryTransaction.Do | Enrolls undo after apply. Use Do(null,undo) before mutations and InventoryTransferSnapshot for exact list/count/ownership restoration. |
| CloneForStack/CraftingMarkPart | Clone copies marks. Preserve source mark; remove the new unit's mark to avoid two exclusive weapon/component selections. |
| InventoryScreenData sorting | Sorts by ID, so append order is not display order. Return exact new Entity and focus by reference. |
| InventoryUI.ReopenItemActionPopupFor293 | Existing exact-reference focus seam can present the separated singleton. Preserve FLOW2 refusal status/popup on failure. |
| Inventory action timing | Existing inventory/Sharp actions cost zero world turns; preserve that contract. |
| Tinkering Sharp | Actual mod_sharp_melee costs BC, requires singleton. No need to change Sharp's rule. Dagger3 leaves source2 unambiguously ineligible. |

Minimal plan: SeparateOneCommand with exact recipient; source claim; clone/one-unit
preparation before mutation; recheck source; snapshot and pre-enrolled rollback;
one decrement, append with owner refs and carry refresh. Total mass unchanged,
including already-overweight packs. Add carried-only Separate one popup action and
exact recipient focus. Later normal acquisitions may merge; no persistent no-stack flag.

RED/counterchecks: Dagger3→2+1 and2→1+1; unrelated identical stack untouched;
null/missing inventory, ground/container/foreign/equipped/missing Stacker and1/0/-1
refuse; stale commands/reentrancy; unlimited/exact-limit/overweight mass/handling;
clone exception and outer rollback; marked/unmarked source; paid Sharp on exact
new singleton only; popup visibility/refusal/focus and unchanged clock. Dedicated
20–60adversarial cases, independent review and native proof follow.

Native plan: Dagger3, empty equipment, known real Sharp recipe, BC bits. Actual
inventory→Separate one→Mods→Sharp→exact new singleton. Assert original2unchanged,
new1Sharp/+1PenBonus/ModificationCount1, one payment, distinct IDs, unchanged mass.
The existing GameAuditReforgeModsBenchPlayer.Modify helper supplies the keyboard
route. Arbitrary unknown Part deep cloning remains the existing separate contract.

FLOW4 expanded focused102GREEN,0CS,05:32:06–05:32:09UTC,3.6492076s.
Native63/63PASS,run2c8a55557725443e8c786172e1f03c4d,30.622404583s,exit0,
0CS. Actual keyboard equip/unequip→exact station transform→individual ground picker
passed for Dagger and paid forged stock. Raw log retains2existing A31 destroyed-camera
exceptions after successful report during shutdown; exit0 is not an exception-free
shutdown claim.2393uniqueGUIDs/0collisions. Final source review0must-fix; full running.

FLOW4 first full8765:8759passed6failed,0CS,05:35:02–05:37:03UTC,
120.9815569s.1known fungal self-cloud flake;5older W6map preservation assertions
compared null source IDs with repaired loaded IDs. Source sweep correction:
map markers are body-loaded entities too. Existing-identity tests must stage an
actual opaque ID; missing-ID controls must expect repair while preserving metadata.
Four W6adversarial fixtures now assign saved IDs; FellingSiteSave adds paired
existing/missing controls(+1case). No runtime change. Full repeat follows.

FLOW4 COMPLETE:8766/8766GREEN,05:39:07–05:41:08UTC,121.2167921s,
0CS/failed/skipped/inconclusive.77newcases, native63PASS,2393uniqueGUIDs.
Full failed evidence and map fixture correction retained. Independent review0must-fix.
FLOW5 source sweep/RED plan above is next; no FLOW5production changes yet.

FLOW4 committed e4acbd8c; FLOW5 begins from8766GREEN. Root reread command
executor/context/results, transfer snapshot, transaction, inventory insert/weight/
equipment APIs, crafting marks and actual UI/Sharp route before initial29REDcases.
Additional correction: GetAllEquipped and InventorySystem.IsEquipped only inspect
the legacy cache. New separation validation also checks actual Body references and
Physics ownership, rather than assuming the cache alone proves carriage. Clone
probe side effects precede the separation snapshot; refusals preserve independently
completed work. No FLOW5production changes before RED.

FLOW5 RED29cases:2already-passing singleton/zero-row controls,27missing-command/
menu failures,05:43:28–05:43:29UTC,1.6268074s,0compilererrors. Raw FLOW5-red.xml.gz.
Minimum implementation uses one new command and a small popup branch. The no-merge
append remains private to the transaction-owned command, avoiding an unnecessary
new general inventory insertion API. It validates actual carried/equipped references,
claims before clone initialization, revalidates afterward, captures immediately before
its own mutations, preserves source mark, and returns exact recipient/focus.

FLOW5 minimum95/95GREEN,05:45:58–05:46:03UTC,4.7816438s,0CS.
30dedicated adversarial cases now exercise clone exceptions/revalidation, shared
transaction nesting, late failures, independent work during preparation/publication,
participant claims, repeated separation, mark ownership, save/remerge, ID-sort focus,
retry and unchanged turn timing. No extra runtime change before this gate.

FLOW5 adversarial+neighbors81/81GREEN,05:48:51–05:48:54UTC,3.2006368s,
0CS. Independent review found an API documentation overclaim: validation is a pure
query and does not clear a previously committed command result. The popup creates a
fresh command per attempt and reads SeparatedItem only on success. Document that
per-attempt/result contract explicitly; add a positive query-preservation pin rather
than mutate results from Validate. Dedicated matrix30→31. No runtime behavior change.
Native staging6cases written before its scenario; verified BitLocker.GetKnownRecipes
rather than assuming a KnownRecipes property.

FLOW5 native staging RED6/6missing-scenario failures,05:51:22UTC,.2421046s,
0CS. Scenario/player/disposable launcher added afterward. Independent source
cold-eye0production must-fix. Strengthened rollback with retained recipient ownership
checks; added heavier prepared-clone refusal versus unchanged-weight control, so the
new mass-refusal branch is directly exercised. Dedicated matrix31→33.

FLOW5 expanded134GREEN,05:53:59–05:54:06UTC,6.9690878s,0CS.
Native16/16PASS,2a08cb6be3d34dfaaf00d6858758bbb6,5.014162292s,exit0,
0CS. Actual separate→exact focused singleton→paidSharp, remainder/mass/time checks
passed.2401uniqueGUIDs/0collisions. Native raw retains2known A31shutdown camera
exceptions after success. Independent reviewer found a staging-fixture Active-turn
leak; capture/restore added before full suite. No runtime behavior change.

## FLOW6/A49 — reserve normal L for Look (source sweep complete; after FLOW5)

Existing ALPHA-READINESS item12 debt, reconfirmed during FLOW3; not a new discovery.
CoO-original control consistency repair. No new art/content or key binding.

| Source/premise | Verified correction / decision |
|---|---|
| InputHandler489 rate gate;612 fresh L | A press swallowed by rate limiting can become held L afterward, bypassing Look and moving east. Remove only the held normal east fallback. Do not promise delayed Look activation. |
| InputHandler364–368 Look dispatch | Look is handled above normal rate gate and uses GetDirectionKeyDown1953/3488. Preserve modal L cursor movement. |
| GetMoveInput3509–3532 | Uses held WASD/arrows/numpad plus alternate vi keys. Full hjklyubn claim conflicts with L Look; remove only L in this normal movement helper and correct comments. |
| ControlsReference26/31/57 | Already advertises WASD/arrows/numpad and L Look. Keep the shipped displayed binding. |
| Docs/Status/IMPLEMENTED67 | Historical full-vi claim needs correction. MCP_PlayMode_Testing_Strategy37 documents modal targeting and remains valid. |
| ShortcutFixture isolated InputTestFixture | Repeated manual Update calls in one InputSystem frame retain wasPressedThisFrame. Advance InputSystem.Update without release, assert L.isPressed && !wasPressedThisFrame, then test held input. |
| Existing Look tests | Direct Enter/Move/Exit calls prove helpers, not real key dispatch. New tests must queue keys through InputHandler.Update. |

Minimum: one normal held-L disjunct removal, three source comment corrections and
historical control-doc correction. Keep fresh L Look, all advertised movement keys,
other existing alternate keys and all modal L direction/choice behavior.

RED matrix: rate-limited fresh L then genuine held frame; L+Escape exit with L held;
fresh L enters Look without spending time; release then L in Look moves cursor;
D/Right/numpad6 move normally and repeat; rate controls/nonmovement preserved.
Then dedicated20–60adversarial cases, native keyboard hold/cursor/movement controls,
independent review and full suite. InputHandler has protected spell work; stage only
incremental hunks against a pre-wave snapshot. No FPS/speedup or mouse-delivery claim.

FLOW5 COMPLETE:8834/8834GREEN,05:56:35–05:58:42UTC,126.1659211s,
0CS/failed/skipped/inconclusive.68newcases, native16PASS,2401uniqueGUIDs.
Accepted direct Separate one step now ships; FLOW6/A49 source sweep above is next,
followed by the remaining system-audit repair queue. Mouse/visual bounds unchanged.

FLOW5 committed; FLOW6 begins from8834GREEN. Root reread EnterLookMode,
ExitLookMode, HandleLookModeInput, mouse-follow and WorldCursorState. Cursor inputs
are edge-triggered; stationary mouse is ignored. Initial13tests queue real input
updates and explicitly distinguish held L from a fresh press. No production edit yet.

FLOW6 RED13:10passed3failed,06:02:19–06:02:20UTC,.9280037s,
0CS. All3failures actually move10,10→11,10 on genuine held input: normal heldL,
rate-limited press then heldL, and Escape while L remains held. Fresh Look, modal
cursor, advertised east repeats and release controls already pass. Apply minimum.

FLOW6 minimum53/53GREEN,06:04:06–06:04:09UTC,2.575117s,0CS.
34dedicated adversarial cases now cover held-L with intended diagonals, all supported
cardinal movement groups/aliases, six modifiers, real C→east interaction directions
and the shipped help table. They distinguish normal movement from modal L consumers.

FLOW6 adversarial+neighbors110/110GREEN,06:05:44–06:05:50UTC,5.6412708s,
0CS. Independent source review0must-fix. Native source correction: Chest is solid,
so keep C→L interaction beside it and move south into a clear lane before east-repeat
controls. WorldCursorState.X/Y/Active are properties; cast the reflected state rather
than using the field-only helper. Native rate-window race is omitted from its claim;
deterministic queued EditMode tests cover it. Five staging tests precede the bench.

FLOW6 staging RED5/5missing-scenario failures,06:08:57UTC,.2500505s,
0CS. Added disposable LookKeyBench/Player/Batch afterward. Native driver holds L
continuously across Escape, checks Chest HP as well as actor/time, preserves modal
L cursor and C→L selection, then moves to clear ground before three east-repeat
controls. Reflection only observes; all gameplay actions use queued keyboard input.

FLOW6 focused57GREEN,06:16:11–06:16:14UTC,2.8942102s,0CS.
Two intended shortcut filter names were stale/nonexistent; actual selected count is
52new FLOW6+5Look tests, not a broader shortcut retest. Earlier110and upcoming full
provide neighbor coverage. Native6d04c04d262f4442ba91d1c07977152021/21PASS,
6.374252083s,exit0/0CS. Independent cold-eye0must-fix;2407GUIDs/0collisions.
Raw log retains2known A31shutdown camera exceptions after gameplay success.

FLOW6 COMPLETE:8886/8886GREEN,06:17:55–06:20:02UTC,127.2927773s,
0CS/failed/skipped/inconclusive.52newcases, native21PASS,2407GUIDs/0collisions.
Normal held L never becomes movement; modal L and advertised east repeats retained.
A47brew/tinker availability/payment truth follows; exact output receipts remain a
separate subsequent slice. Broader audit repairs and FLOW1mouse bounds stay active.


## GA02j — crafting availability and first-use repair complete

After FLOW6, the broader A47repair aligns selected stock, preview/batch quantity,
automatic mineral choice and actual payment. Empty new picks are hidden; stale
marked picks remain explicitly labelled and removable without losing other picks.
Review found same-name cleanup ambiguity; RED labels fixed it. Native found A50:
mineral compatibility checked an uninitialized enhancement registry. The shared
shim now initializes before querying, so all3infusions work on first use.

94newtests, full8980GREEN; unchanged native scenario15PASS after production fix.
Raw first-native failure and allRED/control runs retained in GA02j-REPORT.md.
2414GUIDs/0collisions; known A31shutdown errors separate. No subjective-feel, mouse
or speedup claim. Next A47slice addresses exact crafted recipients/local rollback.

## GA02k — exact crafted stacks and capacity recovery complete

Crafted output now resolves to the actual carried recipient, and failed local
brew/build operations restore exact items and payment. Removed same-blueprint
rollback guessing and obsolete brew rollback ledger. Frozen selection, preparation
eligibility/payment rechecks and bounded same-character re-entry make repeated
crafting reliable while independent completed work remains intact.

Full9066GREEN (+86tests), final strengthened86GREEN, native25PASS: two brews,
two dagger builds, capacity refusal, exact tonic-stack Drop, successful retry
into the original dagger. Service return-reference evidence is separately labelled.
No new content/art/resource-cost/save fields or performance/physical-input/feel
claim. Full details and raw failures in GA02k-REPORT.md. Remaining whole-game
repair queue continues with A31camera cleanup; general A41atomicity is separate.

## GA03a — uninterrupted effect cleanup

A destroyed camera no longer throws before effect playback/particles and both pending
queues are cancelled. Live-camera cancellation still resets shake; accented casts
can continue when their camera disappears.29camera tests include20dedicated adversarial
cases; native44checks exercise actual Unity lifetimes. Full9097/9097GREEN includes
2additional controls stabilizing the existing fungal exposure test; no infection rule
changed. No visual-feel or performance improvement is inferred from engine checks.

The runtime guard is applied to the shared renderer. Its surrounding uncommitted
animation wiring remains protected: GA03a records the exact patch rather than
committing that other work. See Verification/GameSystemAudit/GA03a-REPORT.md.
Next smoother flow: New Game immediately selects its own checkpoint and gives a
truthful retry message if initial saving fails.

## GA03b — a fresh character immediately gets its own save

New Game binds the fresh expedition before its first checkpoint. Every load control
then addresses that character; failure keeps the fresh binding and explains F5 retry.
Continue preserves the earlier expedition and its priority. Old saves/metadata/
backups remain unchanged. Recursive initial checkpoints refuse safely.

Shared native-test save isolation now includes all fresh GUID directories and keeps
the disposable destination through normal shutdown.71new tests; full9168/9168GREEN,
native61/61PASS, compatibility33/33PASS. Initial-file/metadata atomicity remains
separate; no visual-feel or speedup claim. See GA03b-REPORT.md for complete bounds.
Next: hotbar selection survives save/load without casting or changing action costs.

## Completed: selected hotbar slot survives save/load (GA03c/A07)

Actual occupied selection now saves and restores against the loaded actor with
immediate display-state synchronization. Cooldown selections stay selected; empty
hotbars clear and invalid indices use the first occupied slot. No cast/targeting/
action cost is introduced.43new tests; full9211GREEN; native22PASS;0CS.
Review and failed fixture evidence live in GA03c-REPORT.md. Next: malformed-save
decode isolation, then remaining supported smoothing and whole-game repairs.
