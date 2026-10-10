# Save capture throughput: second slate

Status: portable descriptor implementation, review and native regression are complete. Final-source job `1128918628c74d2f9d3c41a5864c8e4c` completed 241 tests: 240 passed, zero failed and one allocation-counter test skipped. The earlier broad job `59fe6ab7ac134b4795dc6d1ec31cb9b9` completed 1,903 tests: 1,902 passed, zero failed and one allocation-counter test explicitly skipped. Earlier broad receipt: `Verification/SteamDeckPerformance/2026-10-10/final-combined-native.json`. The isolated descriptor/adversarial runner passed 38/38. The corrected standalone paired probe passed in job `78c7ef2ffa804de1b810f37a351e0303`. Native allocation counts remain unavailable. First-slate changes are committed as `bf3c2468b`. The parent task owns native Unity compilation, tests and profiling. Qud reference: none; preserve CoO's existing version-7 bytes and gameplay behavior.

## Measured remaining problem

The corrected native sample in `Verification/SteamDeckPerformance/2026-10-10/save-profile-slate1.json` verifies the actual capture callback, generated-world payload size, and roundtrip zone/owner counts. Unity 6000.3.4f1 on macOS 26 measured:

| Workload | Placed owners | Uncompressed bytes | Capture range | Median capture | Median memory gzip |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 generated zone | 2,272 | 1,685,990–1,686,030 | 28.01–49.76 ms | 29.98 ms | 5.56 ms |
| 20 generated zones | 45,596 | 35,511,603–35,511,643 | 432.91–509.01 ms | 500.20 ms | 101.05 ms |

Twenty-zone median capture throughput is approximately 71 MB/s. Moving gzip and file writes to a worker removed those stages from ordinary travel, but a roughly half-second live-graph capture remains. These three samples establish a current workload, not a paired historical improvement, tail latency distribution, build timing or Steam Deck result. The prior 282–402-byte probe receipt is invalid and remains explicitly rejected in `STEAM-DECK-PERF-SAVE.md`.

## Verification sweep

| Assumption | Code evidence and correction |
| --- | --- |
| Cached FieldInfo arrays remove reflection cost | `SaveGraphSerializer.WritePublicFields` still calls `FieldInfo.GetValue` for every saved field. Value fields are boxed. Every field then enters `WriteFieldValue`, which calls `Nullable.GetUnderlyingType` and walks type comparisons again. The existing cache only removes field enumeration/filtering. |
| Only field reads matter | `SaveWriter.WriteString` repeatedly UTF-8 encodes identical field names and assembly-qualified Part names. `SavePart` obtains `AssemblyQualifiedName` per instance. Payloads contain that repeated metadata because the current wire format requires it. Cache its encoded bytes, not its occurrences. |
| All collection work is scalar work | Array `GetValue` and nongeneric `IList` indexing also box; HashSet serialization copies into `List<object>`. Nested object/collection/reference paths have distinct wire framing and traversal order. They should initially retain the proven serializer fallback. |
| A type descriptor guarantees improvement | Cached dispatch still leaves reflection reads and boxing. The isolated experiment below shows a smaller benefit than direct typed access plus encoded names; native measurements must choose the path. |
| Expression-compiled accessors are free and portable | The project includes an Android IL2CPP configuration. Runtime expression compilation cannot be the sole path; compiling each field also creates a measurable first-use cost. Do not replace it silently with an interpreted expression and claim equivalent throughput. |
| An input pause freezes the saved graph | `LightSourceFlickerPart.HandleEvent("Render")` writes public `LightSourcePart.Intensity`; render is dispatched by `ZoneRenderer`. Other render handlers update Part state, FX and callbacks. Input suppression alone is insufficient for capture spanning frames. |
| Capturing bytes may reorder Parts or owners | Global reference tokens, queue order, dictionary enumeration, Part hooks, inherited-field ordering and nullable/collection framing are all wire contracts. Preserve them exactly. |
| An inactive zone is unchanged | Membership/movement versions do not cover public fields, stats, effects, collections or tile state. No dirty-zone reuse is proposed. |

Read references: `SaveSystem.cs` (writer, session orchestration, entity queue, Part dispatch, field metadata and value readers/writers); `SessionTileStateSerializer.cs`; `RenderPart.cs`; `PhysicsPart.cs`; `ThermalPart.cs`; `LightSourceFlickerPart.cs`; `DamageFlashPart.cs`; settlement render handlers; `ProjectSettings.asset`; `PERF-FOUNDATION.md`.

## First slice: independently attribute capture work

Before changing the serializer, add an explicit detached native profile test next to the existing corrected probe. Reuse the same one/twenty-zone content and preconditions. In test code only, mirror `GameSessionState.Save`'s short orchestration, invoking the existing production section writers, with one timer around each of: world binding, header/reference setup and zone manager, turn/log/reputation, queued entity bodies, tile-state footer, and metadata. The internal tile writer can be invoked through one cached reflection method outside the entity loop. Verify the resulting uncompressed bytes exactly match ordinary `state.Save` before accepting any phase timings. Keep field-level stopwatches out of the hot loop.

After serialization, inspect the writer's entity queue to census actual graph bodies (including inventory references), Part types, public field categories, and recurring name/type metadata bytes. This establishes whether Render/Physics/Thermal are truly high-volume owners rather than assuming their blueprint prevalence equals their serialized cost. Run a cold process/domain sample separately from warmed samples. Record allocations, bytes, and max/median whole-capture latency; phase totals should approximately account for whole capture.

This slice is a probe, not a claimed optimization. Its exact-byte guard is essential: the previous vacuous probe demonstrated that content generation beside the save is not proof the save traversed it.

## Next production slice: immutable field descriptors and metadata bytes

Subject to the phase/census result, implement a descriptor per serializable field, preserving the existing field arrays and fixed effect/goal categories. The descriptor contains its original FieldInfo, canonical encoded field-name bytes, preclassified write behavior, and a typed write action where safely available. Name bytes are built once with the same existing BinaryWriter/WriteString encoding, including the null flag and 7-bit UTF-8 length; do not hand-invent a second encoding. Cache assembly-qualified type-name bytes per Type too. Never cache mutable field values or strings supplied by the game graph.

The first portable typed accessors should be ordinary compiled C# for the small set of field owners confirmed dominant by the census. For example, a RenderPart field descriptor can use `writer.WriteString(((RenderPart)obj).DisplayName)`; primitive values bypass boxing and generic type dispatch. Resolve accessors by declaring type AND exact field, not name alone, so shadowed derived fields are safe. New/unrecognized fields automatically retain the reflection fallback. This is compatible with Mono and AOT, has cheap cold setup, and avoids a runtime compilation dependency. A generic descriptor still caches primitive/nullable/collection classification for fallback fields. Do not attempt all nested collection specializations in this slice.

Expression-compiled typed field actions remain an optional separately measured candidate. If tried, require a defined AOT-safe path and independently measure cold compilation for all types encountered in a real first save. The experiment's 8.17 ms for just one 13-field type argues against unbounded synchronous accessor compilation during the first autosave. Eagerly compiling all game types would merely move the hitch to startup.

Illustrative descriptor contract (names are a proposal, not a shipped API):

```csharp
sealed class SerializableFieldWriter
{
    readonly FieldInfo field;          // exact legacy field identity/order
    readonly byte[] encodedName;       // immutable canonical wire bytes
    readonly Action<object, SaveWriter> writeCurrentValue;
}
```

Acceptance tests precede production and native RED releases the implementation. Preserve a test-only legacy field writer as an independent byte oracle. Compare optimized and legacy output for default/changed public fields, inherited and shadowed fields, nullable null/value, every supported scalar, enum conversion boundaries, Unicode and null/empty strings, entity alias/cycle references, arrays/lists/sets, polymorphic nested objects, effect/goal exclusions and mutable include-predicate closures. Unknown/new fields must take the fallback; direct mutation between saves must change bytes. Hooks still execute once in the same order on the caller thread. Existing load/save/lifecycle tests remain required.

Whole-world paired evidence must serialize the same detached state in both arms, alternating arm order, with byte identity verified before timing rows. Use the phase/census profile plus 1/20-zone whole capture; show cold setup independently. A microbenchmark or a descriptor cache reference-equality assertion alone is insufficient. Keep the change only if native evidence shows a meaningful whole-capture/GC benefit without a first-save regression. Save completion, coalescing, identity, root changes, atomic commit and load/quit semantics stay unchanged.

## Isolated mechanism evidence, not Unity performance

An independent .NET runner at `/tmp/coo-save-descriptor-probe` compiles the production `RenderPart.cs` with minimal Part/event/enum stubs. It serializes 45,596 synthetic RenderParts into a pre-sized stream using the existing field-name/value framing. All four arms produce exactly 9,347,180 identical bytes. Five samples follow a warmup per arm; tiered compilation is disabled to remove the visibly shifting JIT tier seen in the exploratory run. The source and raw result are archived in `Verification/SteamDeckPerformance/2026-10-10/save-descriptor-mechanism-probe.cs` and `.json`.

| Field loop | Median elapsed | Per-loop managed allocation |
| --- | ---: | ---: |
| Existing reflection + per-value type dispatch | 19.77 ms | 4,377,216 bytes |
| Cached dispatch, still FieldInfo.GetValue | 16.78 ms | 4,377,216 bytes |
| Typed expression actions, current name encoding | 15.89 ms | 0 bytes |
| Typed actions + cached encoded field names | 11.11 ms | 0 bytes |

Cold descriptor setup was 0.83 ms, expression compilation 8.17 ms and name encoding 1.21 ms on this isolated process. These are mechanism results on .NET 10.0.5, not native Unity/Mono or IL2CPP: synthetic repeated RenderParts, no graph identity queue, hooks, custom serializers, dictionaries, world binding, tile state or grown session. Sequential arms and a fixed pre-sized stream further limit transfer to real capture. The valid conclusion is that typed access can remove observed boxing allocations and static-name reuse can remove encoding work. No whole-save percentage improvement is inferred.

## Cross-frame capture remains a later decision

If whole-world capture is still over budget after measured local improvements, bounded main-thread serialization needs an explicit application-wide capture state. Its contract must cover gameplay turns, rendering events that mutate Parts, UI actions that change inventory/equipment/stats, world-binding hooks, content callbacks and scene/reset/load/quit paths. Pure presentation may keep animating from already captured presentation state, but arbitrary entity Render callbacks cannot run against the serialized graph between slices. All synchronous boundaries must finish or reject the pending capture deterministically before changing its graph or root. A single large entity/custom serializer is itself a potential unsplittable slice and must be measured.

The next slate does not implement this freeze contract or worker traversal of a live graph. It first removes measured repeated serialization work without changing when the graph is observed.

## Self-review and implementation log

- 🟢 Verified the existing 20-zone sample actually serializes and roundtrips 45,596 placed owners; accepted native job `83272dc347de44d9934505dfbd771075`.
- 🟢 First-slate terrain ordering also passed all nine native cases in that job; unrelated simulation/guidance cases passed too (56/56 total).
- 🟡 The prior tiny-payload timing receipt is explicitly rejected, with fixture registration corrected and negative preconditions added.
- 🟢 Native phase/census attribution confirms dominant entity-body cost and high Render/Physics occurrence. The corrected standalone paired probe repeats the serializer improvement; absolute sample variability remains a limitation.
- ⚪ No inactive-zone byte caching, wire-format change, live-graph worker serialization or input-only freeze is included.
- Authored `SteamDeckSaveDescriptorTests` (11 cases): existing byte framing/counter-checks plus expected RED for plan reuse, warm common-field boxing elimination, and a writer-local legacy verification mode.
- Authored `SteamDeckSaveCapturePhaseProbe.ProfileExistingCapturePhases`: invokes production section writers, compares every resulting byte against ordinary Save, validates generated-world roundtrip counts, and records body/Part census, allocations and section times. `CompareLegacyAndDescriptorCapture` will alternate arms after the writer-local mode exists.
- Corrected the probe before accepting baseline evidence: NUnit generic `CollectionAssert.AreEqual` boxes/comparisons across a 35.5 MB payload blocked editor responsiveness. Exact validation now uses a direct byte-index scan and only creates an assertion on mismatch. Both probes yield between samples and log progress. Comparison and oracle copying remain outside the measured serialization interval.
- No commits or staging by this agent. Production followed the recorded isolated RED release after the native probe timeout; the workflow divergence is detailed below.

## Implemented slice and verification receipts

`SaveFieldWriters.cs` now holds immutable plans for ordinary/effect/goal categories, shared exact-field descriptors and canonical encoded name/type bytes. RenderPart, PhysicsPart and ThermalPart use ordinary C# direct field access; new fields and other types retain preclassified reflection or the existing nested dispatcher. Declaring-type checks preserve shadowed base/derived fields. Hooks and session-wide token order are unchanged. `SaveWriter.UseLegacyFieldWriters` is internal and per-instance, enabling the original field/type-name loop for controlled verification without a global mode. No runtime expression compilation, graph-value cache or wire-format change was introduced.

The original synchronous phase test exceeded its native timeout inside expensive generic byte collection comparison. After changing it to raw-byte scanning and yielding between samples, the parent authorized isolated-runner RED to avoid blocking on editor recovery. The isolated first run passed eight existing byte checks and failed three new requirements: absent plans, absent writer-local mode and 432,000 bytes of warm-loop allocations versus a 2,048-byte ceiling. Implementation then passed 11/11. This is a documented native-to-isolated RED workflow divergence, not a claim that the stalled native phase run supplied valid timing evidence.

The dedicated adversarial sweep found one new defect: arbitrary include predicates ran under the metadata lock, unlike the original metadata helper. A test that made a nested worker metadata query failed before correction; predicates now execute outside that lock. The expanded suite covers 27 adversarial cases: every direct field changing after warmup, base/derived shadow reads, unknown-field fallback and exclusions, nullable absence/presence, polymorphic type discriminators, enum limits/overflow behavior, Unicode names and multi-byte length prefixes, null/empty/populated collections, aliased cyclic entity references, hooks/order/thread and pre-save hook mutations, and fixed/mutable filter branches. Together with the original 11 cases, isolated GREEN is 38/38. The native 38-case save selection completed with no failures reported (job prefix `c32c1`). Its enclosing 61-case job had one unrelated helper-fixture failure; this is not a claim that the entire job passed. That earlier MCP result was null and did not expose an observed ignored count, so the selection was not labeled 38/38 passed. The final integrated receipt below now confirms the exact skipped allocation test. Receipts and a compact summary are under `Verification/SteamDeckPerformance/2026-10-10/save-descriptor-*`.

Both the parent and a separate rendering agent independently reviewed the production diff and found no additional actionable correctness regressions in field selection/order, framing, typed readers, fallback behavior, hook/reference order or cache locking. The lock finding above was fixed before this review was closed.

### Final integrated native regression

The final native EditMode job `59fe6ab7ac134b4795dc6d1ec31cb9b9` succeeded with 1,903 total, 1,902 passed, zero failed and one skipped in 1,191.27 seconds. The authoritative `Verification/SteamDeckPerformance/2026-10-10/final-combined-native.json` identifies `SteamDeckSaveDescriptorTests.CommonPrimitiveFieldLoopsDoNotAllocateBoxesAfterWarmup` as `Skipped:Ignored`, with the message that this runtime does not expose a usable per-thread allocation counter. This resolves the earlier missing ignored-count evidence and closes the integrated regression gate. It does not turn the unavailable allocation measurements into zero allocations or change the paired throughput bounds below.

### Paired native throughput evidence and limits

The corrected standalone probe passed native Unity in job `78c7ef2ffa804de1b810f37a351e0303`. Raw receipt: `Verification/SteamDeckPerformance/2026-10-10/save-paired-slate2-final.json`. It releases previous output/stream/queue references before yielding and explicitly checks whether the allocation counter works. Each paired arm serializes the same actual graph, and every resulting byte matches ordinary Save. Five samples per arm alternate order. The probe validates 2,272/45,596 placed owners, 2,311/46,136 serialized graph bodies and approximately 1.69/35.51 MB for 1/20 zones.

| Workload | Legacy median (range) | Current median (range) | Within-run median reduction |
| --- | ---: | ---: | ---: |
| 1 zone | 30.62 ms (29.85–31.02) | 14.29 ms (7.61–16.56) | 53.3% |
| 20 zones | 2,279.27 ms (2,143.78–3,442.20) | 699.59 ms (618.54–890.10) | 69.3% |

Entity-body serialization dominates: its median is 28.68/13.23 ms for legacy/current at one zone and 2,252.52/633.89 ms at twenty zones. The actual serialized census contains 46,133 RenderParts, 45,973 PhysicsParts, 45,971 ExaminableParts, 3,287 MaterialParts and 3,248 ThermalParts. This confirms that the selected Render/Physics paths cover many serialized owners. Examinable and other owners continue through fallback; extending direct accessors is not part of this slice.

The earlier paired receipt, `save-paired-slate2.json`, and combined native pass receipt, `slate2-contact-save-green.json`, are retained unmodified. That run measured one-zone medians of 29.64/15.62 ms (legacy/current; 47.3% reduction), and twenty-zone medians of 2,404.34/836.22 ms (65.2% reduction). Its current twenty-zone range was 701.28–1,794.33 ms. The repeat supports a reduction within both paired runs; it does not establish a stable absolute capture time or a Steam Deck frame-time result.

Absolute timings vary greatly from the first-slate 433–509 ms sample and the corrected run's 136.42 ms descriptor-cache-cold twenty-zone sample. They must not be presented as comparable historical before/after frame timings. Prior editor GC pressure, allocation outside measured sections and the first probe's retained buffers are possible contributors; their individual contribution was not measured. Corrected descriptor-cache-cold samples were 6.90/136.42 ms for 1/20 zones, compared with 7.56/431.14 ms in the earlier paired run. Existing reflection metadata and JIT were already warm, so these single observations do not establish process-cold startup cost or prove first-save latency.

The native per-thread allocation API returned zero for every arm of the original receipt. Those raw zero fields are **unavailable measurements**, not evidence of zero allocations. The corrected probe checks a known 128 KB allocation and reports `allocationCounterAvailable=false` and `allocatedBytes=-1` for every row. The final integrated native receipt confirms the work regression test took its explicit ignore path on that runtime; the .NET boxing reduction remains valid isolated evidence only.

Review identified and corrected avoidable benchmark retention: the iterator kept the previous output copy, stream buffer and writer queue across a yield and into the next capture. The corrected standalone receipt includes releasing those references before yielding. The probe still uses fresh growing MemoryStreams as production does, retains an exact oracle and validates a loaded world, so it is not a minimal allocation microbenchmark. No forced GC was added. Can verify: exact graph bytes, generated-world census, section times and repeated paired relative improvement on this Unity Editor. Cannot verify: physical Steam Deck, release-player behavior, stable frame latency, complete process-cold startup, native allocation volume or safe live-world serialization across frames.

Files added/changed in this slice: `SaveSystem.cs`, new `SaveFieldWriters.cs`, `SteamDeckSaveDescriptorTests.cs`, `SteamDeckSaveDescriptorAdversarialTests.cs`, `SteamDeckSaveCapturePhaseProbe.cs`, fresh script metadata, this document and verification artifacts. No commits or staging by this agent.


### Final-source regression after rendering candidate withdrawal

Native Unity job `1128918628c74d2f9d3c41a5864c8e4c` completed 241 cases:
**240 passed, zero failed, one skipped** (unsupported per-thread allocation
counter). This includes current save descriptor/adversarial tests, native render
work/adversarial tests, contact geometry/native/raster checks, fallback invalidation,
water rendering and the legacy environment/stale-background suites. Receipt:
`Verification/SteamDeckPerformance/2026-10-10/final-post-withdrawal-native.json`.
Rendering sources now match first-slate commit `bf3c2468b` exactly. The accepted
save serializer is unchanged from its byte-identity and paired timing verification.
Counts overlap earlier suites and must not be added together.
