# Native ordinary bed acceptance

Private harness; root owns Unity and publication. Uses the reviewed153-case bed candidate, preserving current C10 save hooks.

## Sweep corrections

- Existing ordinary VillageBuilder places Bed at each room northwest interior corner. Select an actual generated ordinary-village pipeline and existing Bed; create no source furniture. Authored Morrowfast beds are a different source.
- Bed default Owner is empty, and live source search found no Bed.Owner assignment. A native owner/save countercheck therefore needs an explicitly labelled bed-only metadata fixture, using an actual existing NPC ID. It does not claim naturally owned-bed prevalence.
- Ordinary new-game player starts at40HP. Do not inject injury or Bleeding merely to demonstrate healing. Native evidence covers real time/admission/no-inn/no-price behavior; core tests cover healing/cure/death/reservation.
- RestSystem advances the clock instantly60 ticks or to the next300-tick band, without simulated NPC turns or an added tactical action. Actual native energy and tick comparisons must verify this.
- Same-cell interaction goes through C→Period→underfoot exact bed→Sleep. Adjacent bed use must refuse without advancing time.
- Rooted/full blocked/dead/occupied/reentrant behaviors remain covered by core tests; no actors are spawned, disabled or moved to manufacture those native scenarios.
- Save proof uses changed on-disk F5 checkpoint, a real movement input and explicit owner-fixture mutation, then F6 replacement player/bed graph and exact owner/tick/energy/position/HP/gear restore.

## Finite plan and execution prompt

Start one disposable seed64 ordinary new game. Inspect at most32 actual ordinary VillageBuilder surface pipelines in stable map order; record every candidate and refused source. Select first free unowned actual bed with no hostile within8, a real nonplayer owner candidate and an adjacent legal approach. One labelled start-position transfer places unchanged actor beside the real bed. Every later movement, interaction and save/load uses native keys. Do not change world source, stats, health, inventory, currency, NPC AI or modifier rolls.

Use C from adjacent tile to refuse sleep; walk onto bed and prove underfoot selection, exact60-tick rest, next-band rest, no WellRested, unchanged starter currency/HP/gear and cleared temporary reservation. Label and set only that bed's Owner to the existing NPC ID, then prove native owner refusal. F5, walk off the bed and clear only this diagnostic owner field, F6 and prove exact owner/bed identity plus position/tick/energy/actor graph restored; native sleep remains refused. Preserve all first failures.

Required native checks are exact named controls, at least6captures and per-key HP/state/messages. Strong isolated launcher copies reviewed editor-update restoration retry. Compiler-only evidence cannot prove input, rendering, native time or save behavior.

## Independent review and readiness

The tightened harness compiled against actual runtime and editor references with zero errors. Independent review found no concrete false-positive blocker in the required exact underfoot picker,60/next-band deltas, unchanged energy/gear/coins, native checkpoint movement plus explicit owner mutation, and replacement actor/bed graphs. Reports clarify that ordinary HandleZoneTransition may register real schedules: there is no manual scheduler bypass or NPC schedule injection. This is compiler/source review only; root's actual native run remains required.
