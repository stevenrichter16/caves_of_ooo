# Mechanics flow and responsiveness — 2026-09-05

Status: WAVE1 IMPLEMENTED/REGRESSION-VERIFIED (LIVE MOUSE UNVERIFIED); WAVE2 SOURCE SWEEP COMPLETE. User explicitly requested identifying mechanics that
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
   appear to cap visible rows at7(+optionaltrade8); it does not prove ordinary J/K
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
