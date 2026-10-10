# Steam Deck save staging

Status: save staging, adversarial and lifecycle cases passed in the parent task's broad 1,742-case native Unity run. The first performance probe receipt is invalid because it serialized a different fixture session. A corrected probe with explicit capture registration and roundtrip workload assertions awaits native measurement.
Qud reference: none; save implementation and performance only.

## Scope and readiness

🟢 Preserve v7 session bytes, identity tokens, hooks, tile extension and existing loaders.
🟢 Capture mutable game state and metadata on caller/main thread, then compress and commit immutable bytes on a serial worker.
🟢 Keep at most one active and one latest pending autosave; pump durable completion on main thread. Synchronous save/load/new game/runtime/root/identity changes drain first.
🟢 Cache reflection metadata by type and explicit serialization category, preserving inherited-field order and category exclusions.
🟡 Full graph serialization still runs during capture. Measure independently before claiming transition-frame resolution; this milestone removes gzip/disk cost only.
⚪ Inactive chunk reuse is deferred because the existing format and mutation coverage cannot safely validate it. A later explicit mutation/capture contract is required.

## Verification sweep corrections

| Premise | Verified correction |
| --- | --- |
| Capture returns detached state | `GameSessionState.Capture` keeps live entity/manager/turn references. Only serialized bytes are immutable. |
| A worker can call `state.Save` | `Save` binds local people/lair/encounter/wayhouse/exploration state and invokes Part hooks. It must run on the main thread. |
| Zone.EntityVersion validates unchanged chunks | `Zone` documents it as membership/movement only; public fields, stats, cells, effects and tile state can change independently. |
| Chunk payloads can simply be concatenated | `SaveWriter` allocates one session-wide reference-token graph, including cross-zone refs. Independent chunk tokens would alter the wire contract. |
| Existing replacement is atomic | Existing code deletes the destination before moving temporary output. A pre-commit failure also restores `.bak`, potentially replacing a newer valid save with stale data. Use replace-with-backup without deleting the live file. |
| Async acceptance means saved | Acceptance means immutable capture queued. Durable success/failure and PlayerPrefs happen only via main-thread pump/flush. |

## Milestones and API

1. RED safety/ordering/format/counter-check tests, then bounded serial writer and immutable capture.
2. Cached reflection metadata and adversarial filtering/roundtrip checks.
3. Root-owned travel/pump/quit wiring; full relevant Unity regression and native capture timing.

`RequestQuickSave()` captures/queues and returns acceptance. `PumpPendingSaves()` publishes completed results on the main thread. `FlushPendingSaves()` blocks until all queued writes finish and publishes outcomes; false reports any undelivered failure. Existing manual operations retain synchronous durable bool semantics. `HasPendingSave` reports queued/running/completion work.

Performance strategy: PERF-FOUNDATION reflection cache; bounded pending snapshots; no live graph traversal on worker. Capture/compression/commit timings are available through `LastCaptureMilliseconds` and `LastCompletedSavePerformance` (`CaptureMilliseconds`, `CompressionMilliseconds`, `CommitMilliseconds`, `UncompressedBytes`, `Succeeded`). Capture measures serialization plus metadata, excluding the runtime capture delegate. Compression uses a worker-owned memory buffer; commit measures directory creation, fsynced payload and metadata temp files, atomic replacement and backups. Preference flushing remains main-thread work and is not included. These elapsed measurements include scheduling/IO effects and are not CPU counters. No Steam Deck or FPS speedup is claimed.

## Test and implementation log

- Read CLAUDE.md, PERF-FOUNDATION.md, audit save finding, save serializers/service, Zone version contract, prior save fixtures and adversarial methodology.
- Planned RED tests use reflection for unavailable APIs so baseline production still compiles. Existing atomic helper is exercised directly with injected stream failures, in an isolated temporary directory.
- Native RED: all 8 staging tests failed for intended missing APIs/queue/cache; root also ran the combined audit fixtures, with failure output capped. Two unsupported NUnit attributes were removed before the valid run.
- Implemented detached buffer ownership, serial/coalescing worker, atomic replacement, synchronous lifecycle drains, main-thread completion/prefs, and reflection caches.
- Added 3 post-implementation hypothesis checks: inactive Part/tile mutation without membership-version change, changing filter closure, and publication-only-on-completion stage metrics. These are regression pins, not claimed preimplementation RED.

## Divergences and self-review

- ⚪ Safe chunk caching is intentionally not implemented with incomplete versions. Full capture remains an explicit performance limitation.
- 🧪 Device/native timings and play-state lifecycle behavior remain to be verified by root-owned Unity workflow.
- No commits or staging in this task.

Files: this document; `SaveSystem.cs`, `BackgroundSaveQueue.cs`, `SteamDeckSaveStagingTests.cs`, `SteamDeckSaveStagingAdversarialTests.cs`, `SteamDeckSaveLifecycleTests.cs`, `SteamDeckSavePerformanceProbe.cs` and fresh script metadata. InputHandler integration is owned by the parent task.

## Implementation contract and review

The queue replaces only the newest pending capture; the running commit completes first. Request acceptance is not a promise that an intermediate pending checkpoint will survive later requests. Identity/runtime/root changes drain before they can change the destination or capture binding. Main-thread pumping logs `Autosaved.` only after success and reports errors without touching game state on the worker.

`File.Replace` atomically switches an existing destination and retains its previous bytes as `.bak`; first saves use `File.Move`. A failed temp write removes only temp output and never copies an older backup over the current checkpoint. Save and metadata are individually atomic, not a two-file transaction; an interruption between replacements can leave older metadata beside a valid newer save. This pre-existing format limitation remains explicit.

The private MemoryStream buffer transfers to capture ownership, avoiding a second uncompressed copy. Data bytes and captured strings never change after enqueue; worker timing fields are published through the queue completion lock. Reflection preserves original enumeration order and excludes owner backlinks. Fixed effect/goal categories have separate caches; arbitrary predicates rerun because closures may change. Read-field lookup preserves derived-before-base precedence and caches misses.

- 🟡 Fixed: old failure handling could regress the current checkpoint to a stale backup before any replacement occurred.
- 🟡 Fixed: synchronous saves now also contain capture-delegate exceptions.
- ⚪ Lifecycle drains may block during explicit load/reset/quit; ordinary travel does not wait for gzip/disk.
- 🧪 Full graph capture still happens on the caller thread; no claim that long-world serialization hitches are eliminated.
- 🧪 Native timings and platform atomic-file behavior remain subject to root verification.

### Isolated queue stress evidence

A temporary independent .NET runner at `/tmp/coo-deck-save-queue-check` compiled the production queue directly and passed 200 iterations. Each iteration gates the active request, submits 99 newer values, verifies exactly the active and final value commit, checks worker/main completion thread affinity, alternates a write failure, and confirms failure delivery is not sticky. This is pure queue evidence, not Unity/gameplay or Steam Deck performance evidence. Owned-file whitespace checks passed.

### Lifecycle follow-up (test-first)

All 31 original staging/adversarial cases passed native Unity in job `6579a486ab3847f4bdd049c273825e6e` (root receipt `mixed-green-2.json`; unrelated settings/rendering cases intentionally remained RED in that combined run).

Four additional tests in `SteamDeckSaveLifecycleTests` precede the integration changes: completed first-checkpoint availability must deliver pending state before the load menu gate; InputHandler.Update must pump even with no player; OnDestroy and OnApplicationQuit must drain accepted work. The availability test waits for the worker without pumping, making its missing completion/prefs assertion deterministic. All four confirmed native RED in job `dcd7bb69e60e4e2186ba72ec5e2c4dcf`. HasSave now drains before reporting checkpoint availability; root owns the InputHandler pump/quit/destruction wiring.

An explicit detached native sample, `SteamDeckSavePerformanceProbe.MeasureOneAndTwentyGeneratedZones`, generates actual current-exploration content with production loot wiring for seed 64 at x=8..12, y=8..11. It records 1/20 cached-zone stages with one warmup plus three saves each, writing only inside the fixture's temporary save root. The JSON report is available via `LastReportJson` and a `[DeckSavePerformance]` console entry. This observes current save-stage costs, not a paired improvement or Steam Deck frame times.

The first probe receipt is rejected: it reported 2,272/45,596 generated owners but only 282–402 serialized bytes and approximately 0.02 ms capture. `DensityLootTestScope` installed a nested save fixture after the intended session was registered, so the save callback never used those generated zones. Those timings provide no world-save performance evidence. The corrected probe installs its save fixture last, explicitly registers the generated manager, verifies the capture callback executes, rejects payloads smaller than 32 bytes per placed owner, and loads warmup/final checkpoints to assert every zone and placed-owner count. Timing rows are published only after those checks pass.
