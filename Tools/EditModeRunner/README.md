# EditModeRunner — EditMode tests without the Unity editor

Compiles the Unity-free game core (`Assets/Scripts/Gameplay`, `Data`,
`Shared`, `Scenarios/Core`, `Scenarios/Builders`, four small
`Presentation/Rendering` helpers) against a minimal `UnityEngine` stub, adds
every EditMode test file that compiles in that setting, and runs them with
NUnitLite on plain .NET 8.

It exists for sessions that have no editor (cloud agents, CI without a Unity
licence). **It is a pre-check, not a replacement for the editor run** (see
"Honesty bounds"). Unity ignores this folder because it is outside `Assets/`.

## Use

```sh
cd Tools/EditModeRunner
python3 select_tests.py --all          # choose every test file that compiles (~2 min)
./run.sh                               # build + run all selected tests (~5 min)
./run.sh "class =~ /LairBossBiomeTests/"   # any NUnit --where expression
```

`select_tests.py a.cs b.cs …` compiles only the named test files, which is
faster when iterating on one suite.

### Proving a change (the RED→GREEN diff)

Stubbed-environment failures are identical before and after a gameplay
change, so compare two runs instead of reading the raw failure count:

```sh
git stash push -- <production files>      # tests stay, code reverts
./run.sh "class =~ /Tests/" before.xml
git stash pop
./run.sh "class =~ /Tests/" after.xml
python3 diff_results.py before.xml after.xml   # exits 1 if anything newly fails
```

`NEWLY FAILING` must be empty; `NEWLY PASSING` should be exactly the new or
updated tests' positive assertions.

## How it stays faithful

| Concern | Handling |
|---|---|
| `Resources.Load` / `LoadAll` | Reads `Assets/Resources/<path>.(json\|txt\|bytes…)` from disk |
| `Application.dataPath` | `<repo>/Assets` |
| `JsonUtility` | Newtonsoft with a fields-only contract (public or `[SerializeField]`), like Unity |
| `string.GetHashCode()` seeding | .NET randomizes it per process; Unity's Mono does not. `patch.py` compiles copies of the five files that hash strings with a stable hash, so runs are deterministic. The values are **not** Unity's, so an exact seed may generate a different zone than the editor |
| Test isolation | `--workers=1`, one process per run |
| Scene / MonoBehaviour code | Not compiled. `Stubs/CooPresentationShims.cs` gives the core null/no-op stand-ins (`HitStopController.Instance` is null, as in an EditMode run with no scene) |
| `HotbarSaveFixture` | Its home file needs Unity, so `Stubs/TestFixtureShims.cs` re-declares its static `RoundTrip` verbatim and makes the scope object inert (it does not snapshot/restore statics) |

## Honesty bounds

**Can verify:** game-rule logic, content JSON loading, blueprint
inheritance, world and zone generation (deterministic, not Unity-identical
seeds), save round-trips, diag emission. At the time of writing, 658 of
940 EditMode test files compile here, about 9,700 tests.

**Cannot verify:** anything in the 282 excluded files (rendering, input,
UI, editor tooling, native Play-mode audits); exact Unity RNG outcomes for a
given seed; `Mathf.PerlinNoise` values (stubbed); Unity serialization edge
cases (reference loops are an error under Newtonsoft); timing or feel.

About 297 tests fail here for environmental reasons (native scenario
launchers not compiled, `CavesOfOoo.Editor.*` tools absent, art/voxel paths,
the JsonUtility reference-loop difference). They fail identically before and
after gameplay changes, which is why the diff above is the gate.

**The editor run remains the source of truth.** Anything this runner passes
should still be run in Unity (`mcp__unity__run_tests mode=EditMode`) before
it is called verified.
