# Performance Investigation Protocol

Target:
- Desktop PC
- 60 FPS baseline
- Prioritize frame hitches and worst-frame spikes

Primary scenarios:
1. Boot to control ready
2. Idle gameplay baseline
3. Movement and redraw stress
4. Inventory and UI stress
5. Look, targeting, and throw stress
6. Combat and FX stress
7. Settlement and NPC density stress

Capture rules:
- Record at least 3 runs per scenario.
- Save captures with `scenario_commit_build_yyyyMMdd_HHmmss`.
- Record:
  - average ms
  - P95 ms
  - P99 ms
  - max frame ms
  - GC alloc per frame
  - total allocations
  - top 5 markers by self time
  - top 5 markers by total time

Instrumentation checklist:
- `COO.Bootstrap.*`
- `COO.ZoneRenderer.*`
- `COO.AsciiFx.*`
- `COO.UI.*`
- `COO.Input.Update`
- `COO.Turns.*`
- `COO.Combat.*`

Investigation order:
1. Zone rendering
2. ASCII FX
3. UI rendering
4. Input-driven invalidation
5. Turn, AI, and combat
6. Bootstrap and content loading

Backlog template:
- hotspot
- evidence
- root cause
- likely fix
- expected gain
- implementation risk
- regression risk
- validation scenario
