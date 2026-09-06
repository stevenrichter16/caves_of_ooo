# Mechanics flow and responsiveness — 2026-09-05

Status: REVIEW IN PROGRESS. User explicitly requested identifying mechanics that
need smoother integration and fully implementing the supported improvements, without
intervention. This extends the ongoing whole-game audit; it does not cancel its
remaining repairs. Baseline latest completed repair d219936a,8395tests,GA02h native40.
Current GA02i planting/mineral correctness wave will finish its gates first.

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
