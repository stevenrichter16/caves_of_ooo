# GA03i independent paired combat-profile review

Verdict: **SOURCE/ARTIFACT CLEAR for this bounded paired workload; no profile must-fix found.** This is an independent source and artifact review, not a fresh Unity run, a full equipment-wave performance claim, or a replacement for the functional/adversarial/full-suite gates. I made no repository or Unity changes.

## Evidence and source boundary

Reviewed the actual `GameAuditEquipmentContentBenchPlayer.cs`, `GameAuditEquipmentContentBenchBatch.cs`, `NativeSaveIsolation.cs`, the owned combat guard patch, both source-provenance manifests, both raw frame/strike CSV.gz files, both performance JSON files, both final native JSON files, and both archived logs under `Docs/Verification/GameSystemAudit/GA03i-*`.

- BEFORE run: `aa9ab41e94194f5eab0c61ce5a973c0e`.
- AFTER run: `ea9e69e041ea4069b14e0452a56a2648`.
- The two manifests cover the same 11 files. Their only differing hash is `CombatSystem.cs`. Reversing exactly the archived occupied-secondary-hand guard in the current source reconstructs BEFORE SHA-256 `b56da8716142227a8baa5aee115a4a8547ea39d2054105dea358dc73608148a2`. AFTER is `dcd714f4da08507906bdfb57046f212076f5606301b97fcc015d0889b1902baa`.
- Ten current files directly match the AFTER manifest. `LoadoutPart.cs` subsequently changed two documentation lines: the carry syntax example and its natural-default initialization explanation. Reversing exactly those two comments recreates the recorded capture hash `71ed82e2909c0e56ff4c4a0dc9c80578d7dbbc4709ab6114c240a351dea2589f`; there is no intervening executable change in that file.
- Factory natural initialization and the known-recipe/missing-only load repair are already present in BOTH source variants. These captures do not compare dead natural initialization with restored initialization. The profile branches and equipment content are identical between the recorded variants.
- The archived guard affects only an occupied hand that is not the item's first equipment slot. A first occupied slot still follows the historical equipped-weapon path and may retain its existing nonweapon/shield natural fallback. No shield-rule rewrite is hidden in this patch.
- This proves the bounded recorded-source comparison. The 11-file manifests are not hashes of the entire project or compiled binaries. Protected pending combat/visual changes are included in both reconstructed `CombatSystem.cs` versions and must not be described as newly profiled GA03i improvements.

## Raw workload validation

I independently decompressed and parsed every row, verified contiguous unique row indices, strictly increasing timestamps, nonnegative values, per-phase counts, and all strike validity/HP/tick/RNG counters. No mismatches were found.

| Variant / phase | Seconds | Frames | Accepted calls | Hit rolls / offhand rolls | Miss messages | Positive combat-marker frames |
|---|---:|---:|---:|---:|---:|---:|
| Before idle | 25.000565 | 24,015 | 0 | 0 / 0 | 0 | 0 |
| Before one hand | 25.097677 | 24,476 | 249 | 498 / 249 | 498 | 249 |
| Before two hands | 25.075436 | 23,796 | 243 | 486 / 243 | 486 | 243 |
| After idle | 25.000526 | 24,023 | 0 | 0 / 0 | 0 | 0 |
| After one hand | 25.058186 | 23,488 | 242 | 484 / 242 | 484 | 242 |
| After two hands | 25.093496 | 24,143 | 249 | 249 / 0 | 249 | 249 |

BEFORE captured **75.1736779 seconds, 72,287 frames, 492 calls**. AFTER captured **75.1522081 seconds, 71,654 frames, 491 calls**. Each attack phase exceeds the 200-call floor; all requests were accepted, all recorded strikes were valid, neither buffer overflowed, and each phase lasted at least 25 seconds and less than 28 seconds. The phase durations exclude tiny uncaptured boundary gaps, so they need not sum exactly to measuredSeconds.

Every recorded target HP pair is 20 → 20, every strike tick is 10 → 10, and each phase retains spectator energy 1000 → 1000. The raw minimum between-strike intervals are 0.100301/0.100295 seconds before and 0.100294/0.100220 seconds after for one/two hands. `Turns.Tick` and `ApplyDamage` are zero on every frame; the idle combat marker is zero, and each attack phase has exactly one positive combat-marker frame for each accepted public call.

The setup uses real live-factory Player/Villager/ShortSword/Greatsword entities and ordinary inventory/equip APIs. It does not manually regenerate natural weapons. Factory fists, source weapon definitions, shared Greatsword ownership over two hand slots, first-slot flags, live identity, positive HP, positions, registration, lack of status effects, and normal spectator input state are checked. Warm attacks occur before capture. Only NPC brains and extra Player tags are removed, and the generated arena is flattened.

The finite RNG sequence is nonvacuous: two-swing calls require exactly hit d20=1, offhand chance=0, hit d20=1; corrected two-hand calls require exactly one hit d20=1 and no offhand chance. Unexpected overloads/ranges or additional rolls fail. Thus the after branch cannot pass by never invoking combat, rejecting the attack, or silently retaining the forbidden extra punch. All attacks deliberately miss; no penetration, damage dice, rider accumulation, or deaths are in this workload.

## Independent statistics recomputation

I recalculated all **46 metric summaries** (23 per variant: 21 frame summaries and two full-call summaries) from raw rows using sorted nearest-rank p95/p99, max, count, and arithmetic mean. Every summary matches its JSON. Largest absolute discrepancy is 0.0000004862 in an engine-frame millisecond value, consistent with raw CSV's six-decimal formatting; no integer metric discrepancy exceeded floating-point arithmetic noise.

Full public-call timing, converted from nanoseconds to microseconds:

| Variant / weapon | Calls | Mean µs | p95 µs | p99 µs | Max µs |
|---|---:|---:|---:|---:|---:|
| Before one hand | 249 | 319.485 | 381.7 | 465.8 | 1,732.4 |
| After one hand | 242 | 340.583 | 400.3 | 612.3 | 3,378.2 |
| Before two hands | 243 | 341.687 | 396.4 | 588.5 | 4,006.1 |
| After two hands | 249 | 242.509 | 275.3 | 380.8 | 2,760.3 |

These numbers are descriptive. The one-hand control's mean/p95/p99 rose in this single pair. The corrected two-hand call performs less gameplay work: one fewer hit roll, chance roll, miss message and associated swing/event/FX work. Its lower observed call times are not an isolated measurement of guard cost or proof of a general speedup.

Both runs recorded targetFrameRate=-1 and vSyncCount=0. Whole-frame combat p95 is zero because fewer than 5% of frames execute an attack; that value must not be presented as zero-cost combat. The full-call stopwatch is the relevant attack-cost sample here. Engine frame duration measures wall time. Whole-frame marker values may be shifted by one completed frame at phase boundaries; they include native editor/render/input/observer context rather than only the changed gather branch.

Large wall-frame spikes remain: before two hands max **589.401 ms**, after one hand max **648.300 ms**. Other phase maxima are approximately 10–12 ms. Whole-frame GC maxima range from 82,662 bytes to 12,871,538 bytes before and 1,246,776 bytes to 8,844,976 bytes after. Neither absence of allocation nor frame smoothness is established. A single pair cannot assign these outliers to the guard or establish statistical equivalence, build FPS, scheduler behavior, keyboard-combat latency, balance, sprite quality or feel.

## Disposal, logging and private save teardown

- Checked `error CS` counts first: **0 for both archived logs**. Both final native reports record **0 failures, 0 unexpected errors, 0 functional cases**. Both logs contain native capture exit=0 and `NativeSaveIsolation` cleanup complete, exit=0.
- The archived logs are not literally free of every error-labelled line: each has a Unity Licensing startup `Access token is unavailable; failed to update` message. This is outside the runtime audit counter; both captures continue and finish. Do not broaden “0 CS / 0 unexpected runtime errors” into “no error text anywhere.”
- Final reports have `shutdownObserved`, `shutdownRootHeld`, and `shutdownSavingUnregistered` all true. Shutdown timestamps follow the runtime finish timestamps. Both private roots have actually been removed on disk:
  - `.../coo-native-save-audits/2f36d1a6711247aea05109e4293a8bdb` (before).
  - `.../coo-native-save-audits/fcd2a9891bdd4d08987e199f08d134e2` (after).
- Source disposes performance recorders on the normal/error iterator exit before finishing and again idempotently during OnDestroy. Teardown restores the input settings, temporary keyboard, background preference and diagnostic channel settings. The native helper unregisters saving before requesting Play shutdown, retains its owned destination through shutdown, restores the saved preference/root afterward, and deletes only its GUID-owned temporary directory. The profile also checks that its boot marker remains byte-identical, exactly two private quicksave paths exist during capture, and neither fresh/marker ID appears in the ordinary save directory.
- Runtime records prove the save-root and save-registration checks; restoration of every individual input/diagnostic setting is source-reviewed, not independently snapshotted in these artifacts.

## Compatibility and closure scope

The performance mode remains a separate branch, explicitly reports zero functional cases, and leaves the normal 23-group N/F5/F6/walk/G audit route available. It must not count these profile runs as 23 functional passes. Root owns the separate normal native compatibility run and the focused/full test gates; their results should be cited separately in final living documentation.

No additional profile correction is required. Preserve the raw artifacts and the source/provenance boundary, report the behavior change directly, and retain the timing limitations above. Recomputed machine-readable details are in `/tmp/codex-ga03i-profile-recomputed.json`; the read-only validator is `/tmp/codex_ga03i_recompute.py`.
