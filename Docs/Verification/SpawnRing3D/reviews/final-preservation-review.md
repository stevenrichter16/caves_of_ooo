# Final Village/ring preservation and native-retry review

Read-only repository review, 2026-09-10. No Assets/Docs/settings mutations, reverts, staging, Unity launch, MCP restart, or credential changes. Machine-readable inventory: `/tmp/codex-v3d-final-preservation-audit.json`; six protected-source diffs: `/tmp/codex-v3d-final-owned-hunks.diff`.

## Preservation result

The original `/tmp/codex-village3d-before/manifest.json` contains **2,145 preexisting dirty/untracked paths**. All still exist. **2,137 are byte-identical; eight differ**. Every changed source hunk was compared with the actual original bytes; the two flat `owned-overlay-base` source copies match their original manifest hashes exactly.

| Baseline path that changed | Reviewed scope |
|---|---|
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | Owned optional v7 tile-session framing/byte helpers, pre-publication parse, exact restored snapshot and completed load-hook preservation across reconstruction. Original unrelated save work retained. |
| `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` | Owned fresh-game Morrowfast selection, native audit seed seam, exact six-cell garden requirement, (40,23) start search. ApplyLoadedGame unchanged. |
| `Assets/Scripts/Presentation/Cameras/CameraFollow.cs` | Owned ready Village/ring presenter checks, ring native rectangle fit/clamp and legacy framing bypass. |
| `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs` | Owned presenter construction/binding/cleanup, scoped native fallback/claims/picking/water/tint, dirty invalidation and native light refresh. |
| `Assets/Scripts/Gameplay/World/MorrowfastDoorPart.cs` | Owned stationary door full FOV/light invalidation, two added lines plus formatting. |
| `Assets/Scripts/Presentation/Rendering/AnimatedEntityRenderer.cs` | Owned ExecuteAlways lifecycle and additive visual-hook registration/removal; preserved existing renderer body. |
| `Docs/BLENDER-VILLAGE-3D-PLAN.md` | Owned living plan/evidence updates. |
| `Assets/UnityMCP/Log/mcp.log` | Generated editor/MCP log; not authored feature code. |

Current tracked changes not present in that dirty-path baseline are exactly:

1. `Assets/Scripts/Gameplay/World/LightMap.cs`: owned Morrowfast door-state cache key (paired stationary-door tests/plan evidence).
2. `Assets/Settings/UniversalRP.asset`: intentional append of the new renderer GUID `ffee1e172837943818538e4230ac82e2`, leaving default renderer index 0; soft shadows 0→1. No other pipeline fields changed.
3. **`ProjectSettings/QualitySettings.asset`: unrelated engine serialization drift**, discussed below.

All other previously dirty spell, input, entity, anatomy/combat, terrain, save-test, scene-art and authoring files in the baseline are unchanged. This does not imply the entire preexisting workspace was committed or clean; its original untracked infrastructure remains untracked and must not be swept into a falsely self-contained feature commit.

New paths belong to the declared Village/ring art and sources, runtime/presenter/bootstrap/garden/tile serialization, tests, evidence, living documentation and durable tools. New `Tools/{Village3D,SpawnRing3D}/__pycache__/*.pyc` are generated runner byproducts, not source deliverables; do not stage them. Existing InitTestScene artifacts were already present in the saved baseline and were left untouched.

## GUID audit

- **3,457 Assets metadata files; 3,457 unique valid lowercase 32-hex GUIDs.** No malformed metadata GUID, duplicate GUID, or new-vs-existing collision.
- **820 newly introduced untracked metadata files** relative to the original dirty baseline plus tracked asset set. Largest families: SpawnRing468, Village279, tests33, presentation17. Complete paths in the JSON inventory.
- No baseline metadata file changed or disappeared; none of the eight changed baseline paths is metadata.
- Additionally compared **all 3,423 metadata files captured by the prior Village native runner's source inventory** with the current files after final ring reimport: **zero changed, zero missing**. Thus the final refinement did not churn those already imported GUIDs or importer metadata.
- These are filesystem/GUID preservation checks, not a new visual/topology/import acceptance claim. The root's actual import reports remain authoritative for mesh/rig validation.

## QualitySettings exact restoration proof

`ProjectSettings/QualitySettings.asset` is absent from both original `git-status.z` and `tracked.diff`. The current index equals HEAD. HEAD `1d2b5be8b7b9a67d8bd333b1ff6340f24d3505b3` dates from 2026-09-06, before the 2026-09-09 snapshot. Reversing only six per-quality `serializedVersion: 4→5` edits, six `meshLodThreshold: 1` additions and one `Nintendo Switch 2: 5` platform mapping reproduces HEAD **byte-for-byte**. No quality selection, shadow, vSync, texture, terrain or other user setting changed.

- Original: **9,889 bytes**, SHA-256 `e7dc185b977703ec43789f2e73cf2f44f31a73bb42bd594f69d5a799d65b2883`.
- Drift observed: **10,058 bytes**, SHA-256 `7ec339d3e5e2b8cc73548e69256f6151782bb68a4afb64f865fd94704dbf0de3`.
- Exact original bytes are copied to `/tmp/codex-qualitysettings-original.asset` for root review.

Recommendation: root may safely restore **only this file** from HEAD or that copy and verify the original hash before the final source freeze. Do not restore UniversalRP: its two changes are explicitly intended. I did not restore either file. A later Unity launch could serialize the migration again; the final source-hash gate should expose that rather than silently accepting it.

## Failed native regression: actual classification

Raw immutable attempt:
`Docs/Verification/Village3D/regressions/20260910T063009Z-native-c1baaf9e492648cca40bdaf458bdd979/`.

The wrapper status remains **FAIL**. The same-run gameplay report `ee7aae1e179d4f7aba5a72b71389e4a0` has **35/35 named PASS**, zero scoped unexpected errors, actual garden/owner/gear/roof/save/cast checks, seven 1080p captures, all three native measured phases, process exit0 and all exact run/root/scene/view/seed/preference/shutdown checks. Source inventories before/after are identical; no timeout or forced kill occurred. All30 files in `archive-manifest.json` still match their recorded hashes.

The full log independently explains rejection:

| `unity.log` lines | Evidence and boundary |
|---|---|
| 1018–1107, actual error at1063/1069 | MCP WebSocket establishment to localhost8080 failed once; stack is `MCPForUnity...WebSocketTransportClient`, not gameplay. Async `TrySetException`/`SetException` stack frames account for12 diagnostic rows; they are not12 independent thrown gameplay exceptions. |
| 1111, 1359, 1472 | Later session registration, HTTP bridge startup success and explicit WebSocket connection verification show recovery. The server snapshot also records successful connection/tool registration. No claim is made about the transient connection's precise external cause. |
| 6004,6103 | Native workload and scene/view/private-save cleanup each log exit0. |
| 6121–6154 | Unity Package Manager AssetStoreOAuth fails to obtain a Unity ID auth code during Editor exit. One error log plus a follow-on product-update log, both after the cleanup receipt. No CavesOfOoo runtime exception is identified. |
| Around6083 onward | Unity CoreBusinessMetrics SQLite `database is locked` messages and shutdown thread-finalization notices are additional editor environment noise, not renderer assertions. |

No C# compile error, shader compilation error, native rendering MissingReferenceException, crash/fatal marker or failed gameplay assertion was found. The wrapper's14 `exceptions` rows must not be reported as14 gameplay exceptions. Nor should this attempt be relabeled overall PASS: it genuinely violated the stricter complete-process no-error gate.

## Retry readiness

`Tools/Village3D/{run_common.py,run_gpu.py,run_native.py,validate_results.py}` are byte-identical to the reviewed `/tmp` handoff and the failed attempt's source snapshot. All four parse successfully. The fixed native output names were originally absent and are absent again; the restoration receipt is true for all three, while the produced raw files remain archived. No previously accepted file was overwritten. Seven GUID screenshots remain preserved with mapped archive copies.

The tool is ready for a **new sequential retry** using `python3 Tools/Village3D/run_native.py --execute` after root's current native/profile work exits. It retains concurrent-editor refusal, the requested MCP2+5-second sequence, a unique run directory, strict whole-process diagnostics, source hashes, same-run result validation and exact prior-output restoration. At this audit's process snapshot there was no Unity process and no Temp/UnityLockfile; this is a time-specific observation, not permission to race another launcher.

No environment-error allowlist, credential change, Package Manager shutdown suppression, MCP service patch or runner edit was made. A repeat environmental error can still legitimately fail the overall gate; source-readiness is not a guarantee of a clean external-service run. Also, the existing runner documentation says MCP is left available, but Unity's own shutdown hook stops the server in this trace (line6175); the next runner correctly restarts it, so this is a wording caveat rather than a retry blocker.
