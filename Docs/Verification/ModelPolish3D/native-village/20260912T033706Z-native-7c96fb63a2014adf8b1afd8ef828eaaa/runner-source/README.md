# Village regression runners

These wrappers are prepared for the final Village checks after ring/shared-surface integration. The GPU gate has executed successfully; native attempts retain separate gameplay and whole-process log results. Default invocation prints a plan and has no process or filesystem side effects. Only `--execute` runs an editor.

Run sequentially after other editors/batches have exited and all source/asset edits are frozen:

```sh
python3 /Users/steven/caves-of-ooo/Tools/Village3D/run_gpu.py --execute
python3 /Users/steven/caves-of-ooo/Tools/Village3D/run_native.py --execute
```

Omit `--execute` to inspect the exact command/archive plan. Each command returns 0 only after its complete validation gate. Run the second command only after reading the first result. No concurrent Unity execution has been delegated to this agent.

If an unheld `Temp/UnityLockfile` remains, the default refuses. After review, `--archive-stale-lock` permits backing up that file, verifying no Unity process and no `lsof` holder, then removing that stale lock. This never permits killing an existing editor. The runner separately acquires a project-keyed `/tmp` lock shared by both wrappers; unrelated launchers must still respect the project freeze.

## Verified entrypoints and source corrections

| Check | Entrypoint / output contract |
|---|---|
| GPU12 | `CavesOfOoo.Editor.Village3DShaderGpuProbe.RunFromCommandLine`, `-batchmode -quit`, supported `-village3dGpuReport <unique attempt>/artifacts/shader-gpu.json`. Its PNGs use the same new directory. |
| Native35 | `CavesOfOoo.Editor.Village3DNativeAuditBatch.RunAfter`, native rendering Editor without `-batchmode`, `-quit`, or `-nographics`. The batch owns its asynchronous cleanup and exit. |
| MCP | Refuse any existing Unity process, terminate only exact `mcp-for-unity` executable-token processes, wait 2 seconds, start `uv run mcp-for-unity --transport http` from `/Users/steven/unity-mcp/Server`, wait 5 seconds, require the process still running, then check again for Unity. The runner does not stop the restarted service; Unity may stop it through its own shutdown hook. The next run restarts it before launching Unity. |
| Accepted evidence | Prior scripts used fixed accepted paths and killed editors by name. These wrappers refuse concurrent editors and never overwrite a past attempt. GPU uses its supported unique path. Native output preservation is described below. |
| Late errors | `Village3DNativeAuditBatch.Finish` removes its log observer before final destruction. The wrapper scans the complete process log and stdout after exit, including C# and shader errors, exception/fatal lines and `Debug.LogError` stack entries. Compiler errors are printed before result counts. No blanket known-exception allowlist. |

Source anchors: `Assets/Editor/Art/Village3DShaderGpuProbe.cs:54`, `Assets/Editor/Scenarios/Village3DNativeAuditBatch.cs` (`RunAfter`, `Finish`, `PollCleanup`), `Assets/Scripts/Scenarios/Custom/Village3DNativeAudit.cs:169` (three measured phases), `:479` (GUID screenshots), `:751` (fixed JSON), `:754` (late teardown report).

## Native fixed-output preservation

The native entrypoint does not accept output arguments. The wrapper does not change Assets, redirect directories, create symlinks, or patch the entrypoint. It reserves exactly these names under `Docs/Verification/Village3D`:

- `V3D-after-native.json`
- `V3D-after-cleanup.json`
- `V3D-after-frames.csv`

Existing files are hashed and atomically renamed into the unique attempt's `preserved-fixed` directory before launch. After Unity exits, newly produced fixed outputs move into `artifacts`; old files are copied back with their original bytes/timestamps and verified by hash. Backups remain archived. A partial reservation is recoverable, and each old file is independently restored even if another restoration or screenshot archival fails. An ambiguity fails closed and records the remaining recovery location.

Screenshots are already named with the native run's 32-hex GUID. Newly created screenshots stay in their original location so the untouched raw JSON's absolute paths remain valid; hash-identical copies are archived with `screenshot-map.json`. Old images are never replaced. The validator requires the seven exact expected labels with the *new* native GUID, 1920×1080 PNGs, and matching archived hashes.

Do not manually remove a failed native save scope or claim restoration from a wrapper timeout. The editor owns save isolation. An interrupted run is a failure even if some assertions passed. The wrapper may terminate only the editor PID it launched after its own deadline; forced termination invalidates the run. If any editor remains alive, restoration of fixed names is deferred to avoid racing its writes. `preserved-fixed` and `fixed-before.json` provide recovery evidence.

## Evidence and acceptance

Default archives: `Docs/Verification/Village3D/regressions/<UTC>-<gpu|native>-<wrapper GUID>/`. `--archive-root` is supported; native atomic preservation requires the same filesystem as the repository.

Each attempt records the exact launch plan, runner source snapshot, before/after SHA-256 inventories of source/assets/settings, owned PID, MCP startup receipt and immutable final log snapshot, full raw Unity log/stdout plus deterministic gzip copies, log diagnostics, raw reports/PNGs/CSV, fixed-file restoration receipt, validation checks, `runner-result.json`, and a final `archive-manifest.json` of every archived file. The live MCP log has a separate unique `/tmp/codex-village-regression-mcp/<GUID>.log` path so later MCP writes cannot change the archived snapshot.

The source inventory includes Scripts, Editor, Tests, Shaders, Art3D, Resources, Settings, the main scene, ProjectSettings and Packages. Any change during the attempt fails the gate; Library/Temp/UserSettings and verification outputs are excluded. Raw artifacts are retained for failed attempts too. Recovered prior reports are excluded from validation; missing new reports cannot silently reuse an accepted result.

GPU validation requires status PASS, exactly the current 12 named cases, zero failures/scoped logs, four restoration booleans, the actual 3D/2D renderer split and every reported PNG at the new path and its recorded raw-texture hash field. The GPU report hashes raw texture bytes (`Village3DShaderGpuProbe.cs:271`); this wrapper trusts the reviewed GPU assertions and does not recompute pixel hashes. The archive manifest separately hashes encoded PNG files. Native validation requires all 35 named PASS assertions, fresh run ID, actual seed 729490642/Morrowfast (40,23)/six exact garden receipts, workload and shutdown flags, matching cleanup root/run/seed, absent owned save directory, three complete native phases and every raw CSV row, plus seven same-run screenshots. Process exit must also be 0, source hashes unchanged, no full-log errors, and previous fixed files restored.

These remain **GPU shader acceptance** and **native Village35 regression**, not a new benchmark or a claim about art quality, pixel-perfect animation, stable 60 fps, or speedup. Native35 retains its existing three 25-second measured phases; optional GPU/render counters are not required to be nonzero. Scene/GameView booleans retain the existing entrypoint's restoration semantics; the wrapper does not claim a new exact window-layout comparison. Review screenshots manually. There is still a narrow process-start race with unrelated launchers; the explicit freeze and Unity's own project lock remain required.
