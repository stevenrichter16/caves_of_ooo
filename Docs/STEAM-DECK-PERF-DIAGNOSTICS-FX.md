# Bounded diagnostic and effect work

Status: plan/sweep and native RED recorded; all 43 new diagnostic/FX cases GREEN in the central broad run, 2026-10-10. Qud reference: none. These are presentation/diagnostic costs; no gameplay or save contract changes.

## Plan and readiness

🟢 Existing Diag, PerformanceDiagnostics and WorldFxCoordinator tests cover immutable record-time evidence, frame snapshots, queue admission, timeout and presentation recovery. 🟢 No new assets. 🧪 Native hardware timing is outside this bounded implementation; counters establish avoided work only.

1. Keep completed performance values in reusable storage. Materialize one detached snapshot lazily per completed frame only when requested; subsequent frames cannot mutate retained snapshots.
2. Add an explicit detailed-capture policy, enabled by default in editor/development and opt-in in retail. Suppress only named routine/detail streams, keep rejection/failure/refusal and normal action evidence; explicit channel opt-in retains diagnostic tools. Never defer retained payload serialization or retain live mutable payload references.
3. Keep the existing first backend advance and contact-recovery clock. Run the zero-delta repaint only after newly admitted queue content. Existing direct Play calls are painted by the normal first pass, and auras/state rebuild before that pass remain visible.

## Verification sweep and corrections

| Source read | Verified contract / correction |
| --- | --- |
| `PerformanceDiagnostics.BeginFrame`, `EndFrame`, snapshot constructor and tests | EndFrame eagerly allocates snapshot + dictionary even if nobody requests it. Detached prior snapshot must survive BeginFrame and future EndFrame. |
| `Diag.Record`, `SetChannel`, `ResetAll`, `DiagTests`, `Docs/AI-OBSERVABILITY.md` | Retained PayloadJson intentionally captures mutable values immediately. Do not replace eager serialization with object retention. Channel disable must still override the default policy. |
| `TurnManager` begin/end hooks | NPC records already use off-by-default `turn-verbose`; player `turn` anchors are meaningful and retained. |
| `CropTime`, diagnostic callsite kind inventory | Reconciliation records are recurring detail; worldgen successes/benchmark streams are detail. Rejections, failures/refusals and gameplay actions stay observable. Some callsites allocate arguments before Record; new kind-aware guard allows incremental adoption. |
| `WorldFxCoordinator.Update`, Play, RebuildAuras, CancelAll | Backend advance precedes admission, protecting new requests from old frame hitches. Second zero-delta pass is only necessary for freshly admitted content. |
| `AsciiFxRenderer.Update`, AcceptRequest | Update renders; AuraStop is accepted even across zone reference changes. Keep that state cleanup path. |
| Existing WorldFxCoordinatorTests | First frame, missing-art fallback, off-mode state cues, bus resets, zone changes, hard timeout and hitch handling have prior coverage to rerun. |

## Divergences

The policy is intentionally conservative: named routine streams are filtered, not all gameplay diagnostics. No global logging shutdown, schema redesign, asynchronous payload serialization, backend rewrite or simulation timing change.

## Test/implementation log

- Root observed 13/13 RED on missing policy/snapshot/backend diagnostics (`dcd7bb69e60e4e2186ba72ec5e2c4dcf`) and then released implementation.
- Implemented completed-frame reusable storage with lazy detached publication. SnapshotMaterializationCount measures actual snapshots, preserving retention across BeginFrame/EndFrame/reset.
- Implemented DetailedCaptureEnabled (editor/development default true, retail false), explicit per-channel detail opt-in, and IsRecordEnabled. Named detail streams are worldgen successes (except interactive nest disturbance), benchmark successes, CropTimeReconciled, PropagationWave, gas spread/merge, CarryPenaltyRefreshed and NativeSpellPrepared. Rejection/failure/refusal/error/veto/blocked/mismatch evidence remains; explicit channel disable remains authoritative. Retained JSON is still eager.
- Coordinator performs one backend advance/paint group normally, an additional zero-delta group only after admitted queued content. AuraStop retains its cross-zone cleanup exception. Empty queues allocate no batches as before.
- Added 13 work/regression cases, 21 dedicated adversarial cases and nine launch-policy cases. Root confirmed all 43 GREEN in the 1,742-case central broad run (`e2b7486c6ca147138ec56075d2a974eb`; receipt `Docs/Verification/SteamDeckPerformance/2026-10-10/broad-1.json`). That broader run had separate rendering regressions, subsequently fixed and rerun; no claim that its entire aggregate was green. `git diff --check` passes. No numerical speed gain claimed.

## In-phase self-review

- 🧪 Validate end-user visual/feel parity and native contact recovery through root's live checks; automated script assertions cannot establish perceptual parity.
- ⚪ Retained evidence remains eager and detached; absent detailed records in retail are an intentional diagnostic policy, not gameplay changes.

Files: Diag.cs, PerformanceDiagnostics.cs, WorldFxCoordinator.cs, new tests/.meta and this document. Root owns GameBootstrap log-mirror integration.

### Pre-GREEN review

- 🔵 Snapshot symmetry: CurrentFrame resets independently; EndFrame copies into private completed storage; only getter allocates a new detached object. Published objects are never reused internally.
- 🔵 Diagnostic symmetry: SetChannel(false) overrides all policy; ResetAll clears explicit overrides and restores the build/launch default. Filter occurs before GUID generation/serialization, preserving causal and payload semantics for retained records.
- 🔵 FX order: previous casts advance before admission; newly admitted work receives only zero delta. Hard wall timeout, native contact recovery and initial state reconstruction remain in their previous positions.
- ⚪ Existing payload argument allocations require callsite IsRecordEnabled adoption; central filtering does not pretend C# can avoid pre-call argument evaluation. Root owns CropTime and bootstrap mirror integration.

### Launch opt-in and cold-eye correction

A runtime setter alone is not a usable retail launch policy. Added nine pure-policy tests for `COO_DIAGNOSTICS=1` (retail opt-in) and unchanged editor/development defaults. Cold-eye callsite review also found `worldgen/VoxelMeshUnmapped`, genuine missing-art evidence whose name lacks Failed/Rejected. Added its regression before changing the filter. Root recorded all nine launch REDs and the unmapped failure RED (`1900d7900f1341b0a062e21bbcf3aff8`), then authorized fixes. Initial capture and ResetAll now derive defaults from the explicit environment option plus build type; the filter retains Unmapped/Missing failures. Adversarial suite now has 21 cases.
