# Starting-builds probe (not compiled by Unity)

Throwaway measurement code behind `Docs/Design/STARTING-BUILDS.md`. It lives
under `Docs/`, so Unity ignores it. It compiles against current `main`
through `Tools/EditModeRunner` (an editor-less runner on `main`, not on this
branch).

| File | What it measures |
|---|---|
| `BuildProbeTests.cs` | Per-swing hit rate and damage through the real `CombatSystem` for each weapon, gear and attribute set (1,000 swings per cell) |
| `BuildFightSim.cs` | Whole fights through the real `TurnManager`, enemy AI, skills, effects and the gas tick, with a scripted player policy per build, over six scenarios |
| `per-swing-results.txt` | Output of the probe |
| `fight-sim-final.txt` | Output of the sim with the final four builds and variants |
| `fight-sim-quench-exhibit.txt` | An earlier run that includes the "Quench" caster exhibit |

## Re-run

1. On `main` (commit `158f2229` or later), copy the `.cs` file you want into a
   scratch folder outside `Assets/`.
2. In `Tools/EditModeRunner`, write `tests.props` containing an
   `<ItemGroup><Compile Include="/abs/path/BuildFightSim.cs" /></ItemGroup>`.
3. `PROBE_OUT=/tmp/out.txt ./run.sh "class =~ /BuildFightSim/"`
   (set `TRACE="C Stormcaller|S3"` for a per-round trace to `trace.txt`).

## Limits (read before quoting numbers)

- Scripted player: stands and casts, waits for enemies to step adjacent, never
  kites, retreats, drinks tonics or uses terrain.
- 60 runs per cell: about ±6 points.
- One gas tick per player turn is assumed.
- Enemy placement is hand-set, not the real glade builder; the Spread
  territory warning is not modelled.
- Not run in the Unity editor.
