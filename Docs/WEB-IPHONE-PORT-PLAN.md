# iPhone browser version — feasibility and implementation plan

Status: implementation in progress, 2026-11-03. Browser persistence, touch shell, build tooling and automated checks now exist; see [implementation checkpoint](WEB-IPHONE-IMPLEMENTATION.md). Unity Web build and physical iPhone qualification remain blocked.

Baseline: main at `8c4573ae51a58916884e23efa282859d0669d645` (“Deliver Deck-sized controls and display qualification builds”, 2026-11-02). Initial planning used pinned GitHub source while the executor was unavailable. The executor subsequently came online; main was pulled with `--ff-only` and confirmed unchanged. Implementation is isolated on `codex/iphone-web-implementation`. The user explicitly authorized sustained implementation; native/runtime release gates still apply.

## 1. Decision

Add a **Unity Web build of the existing game**, using its current flat 16×24 presentation, responsive phone UI, touch input, and browser persistence. Keep the desktop Unity game and its keyboard/controller presentation. Share gameplay, world generation, content, actions, and save serialization.

Unity 6000.3 officially lists iOS Safari support, so this is a plausible route; it is not proof that this particular game fits phone memory or latency limits. The first deliverable is a measured real-iPhone slice. Commit to the full port only after it passes.

| Approach | Reuse and cost | Decision |
| --- | --- | --- |
| Existing Unity project → WebAssembly/WebGL 2, flat renderer | Highest reuse; needs platform adapters and substantial phone UI work | First approach |
| Extract shared C# simulation → browser WASM with a new web renderer | Keeps one rules implementation, but requires core separation, different loading/rendering/UI, and parity work | Fallback if measured Unity runtime overhead remains unacceptable |
| Independent JavaScript/TypeScript recreation | Reimplements gameplay, save graph, content semantics, and bug fixes twice | Do not start here |
| Native Unity iOS build | Reuses most touch work and avoids browser constraints, but does not satisfy web delivery | Separate future option if desired |

A PWA is packaging for the Web build. It does not remove Safari memory limits or make saves permanently safe.

## 2. Product scope and working assumptions

- A persistent campaign RPG with the same turn costs, combat, inventory, factions, exploration and authored content as the desktop build.
- First validation target: Safari on a physical iPhone 13-class device, plus a smaller or older supported device. Proposed support floor is iOS/Safari 18.2+, subject to actual tests; Unity's broader documented minimum is not the game's support promise.
- Landscape first for the initial slice. Before release, portrait must remain playable with a narrower camera and full-screen menus; do not depend on orientation lock or fullscreen APIs.
- Browser-tab launch first; Add to Home Screen and offline-after-download behavior follow.
- Local saves plus file import/export in v1. Automatic cross-device cloud synchronization is a separately scoped later service.
- Preserve approved flat art. Smaller viewports change camera coverage, not actor sight, simulation radius, time, or difficulty.
- No browser accounts/backend required for the initial local game. Host over HTTPS at a stable top-level origin.
- This is a port plan, not authorization inferred from old docs to continue unrelated graphics or lore work.

## 3. Verified baseline and corrections

Evidence is pinned to the baseline above. Documentation records are historical evidence, not new test results.

| Observation | Evidence | Port implication |
| --- | --- | --- |
| Editor is Unity 6000.3.4f1 | [ProjectSettings/ProjectVersion.txt:1](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/ProjectSettings/ProjectVersion.txt#L1) | Install this version and Web Build Support first. Upgrade only for an evidenced blocking issue with a desktop regression gate. |
| Latest qualified presentation is flat 16×24 with 1×/2× zoom at 1280×800 | [Docs/STEAM-DECK-FLAT-READINESS.md](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Docs/STEAM-DECK-FLAT-READINESS.md) | Reuse flat rendering; older voxel-era descriptions do not define current scope. Editor/Mac evidence is not a phone or WebGL pass. |
| Build depends on a machine-local editor package | [Packages/manifest.json:3](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Packages/manifest.json#L3) | Resolve the local unity-mcp dependency reproducibly in the build environment; retain normal developer tooling. |
| Typed world command vocabulary already exists | [Assets/Scripts/Presentation/Input/NativeGamepadInput.cs:10](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Input/NativeGamepadInput.cs#L10) and [Assets/Scripts/Presentation/Input/InputHandler.Controller.cs:95](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Input/InputHandler.Controller.cs#L95) | Reuse command semantics. Extract a device-neutral dispatcher rather than simulating keyboard input. |
| Loadout names poll physical keyboard keys | [Assets/Scripts/Presentation/UI/InventoryUI.Loadouts.cs:120](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/UI/InventoryUI.Loadouts.cs#L120) | Add an actual mobile text input field/bridge; viewport resizing alone cannot provide software-keyboard editing. |
| Fixed modal grid and persistent side/bottom panels | [Assets/Scripts/Presentation/Rendering/CenteredPopupLayout.cs:12](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Rendering/CenteredPopupLayout.cs#L12), [Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs:117](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs#L117) | Add responsive phone layout. Shrinking the desktop canvas is insufficient. |
| Saves use Task.Run and a synchronous wait | [Assets/Scripts/Gameplay/Save/BackgroundSaveQueue.cs:40](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Gameplay/Save/BackgroundSaveQueue.cs#L40) and [Assets/Scripts/Gameplay/Save/BackgroundSaveQueue.cs:89](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Gameplay/Save/BackgroundSaveQueue.cs#L89) | Browser needs a non-blocking, non-managed-thread save path. |
| Durable save currently means filesystem commit | [Assets/Scripts/Gameplay/Save/SaveSystem.cs:910](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Gameplay/Save/SaveSystem.cs#L910) | C# file creation/flush/replace is not evidence of completed browser persistence. |
| Current save format is 8 | [Assets/Scripts/Gameplay/Save/SaveSystem.cs:48](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Gameplay/Save/SaveSystem.cs#L48) | Older save docs saying version 7 are historical. Promise cross-platform import only for explicitly tested compatible build/content/schema combinations. |
| Parts are registered and restored reflectively | [Assets/Scripts/Data/Factories/EntityFactory.cs:69](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Data/Factories/EntityFactory.cs#L69), [Assets/Scripts/Gameplay/Save/SaveSystem.cs:1649](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Gameplay/Save/SaveSystem.cs#L1649) | Validate IL2CPP stripping and dynamically constructed content with preservation metadata or generated registrations. Reflection itself is not a proven blocker. |
| Audio eagerly loads all rows of six catalogs | [Assets/Scripts/Presentation/Rendering/WorldAudioLibrary.cs:54](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Rendering/WorldAudioLibrary.cs#L54) | Measure download/decode memory; introduce prepared regional audio sets or lazy loading if needed. |
| Startup loads resources and generates initial state synchronously | [Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs:107](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs#L107) | Measure cold launch; yield between safe preparation phases if startup blocks the browser. |
| Existing performance rules favor dirty cells and cached UI snapshots | [Docs/PERF-FOUNDATION.md](https://github.com/stevenrichter16/caves_of_ooo/blob/8c4573ae51a58916884e23efa282859d0669d645/Docs/PERF-FOUNDATION.md) | Carry these rules into phone UI; profile Web builds rather than inferring from Editor. |

Corrections to tempting assumptions:
- `CONTROL-FLOW-MOBILE.md` and `DATA-FLOW-MOBILE.md` are phone-readable architecture guides, not implemented touch controls.
- Deck qualification does not prove Safari shader support, touch ergonomics, or sustained iPhone performance. Its Vulkan dependency makes a WebGL visual smoke test especially important.
- `SaveFieldWriters.cs` deliberately uses ordinary C# accessors; do not invent a Reflection.Emit blocker. The actual risks are preservation, dynamic construction and save roundtrips.
- `Tools/EditModeRunner` can help audit core separation, but uses Unity stubs and test substitutions. It is not a production browser runtime.
- Historical save profiling includes a roughly 35.5 MB raw snapshot with twenty cached zones. This is a stress-workload warning, not the current phone footprint or timing.
- Source/asset repository size and desktop archive size are not Web download size. Obtain an actual Web BuildReport.
- Fire/reload/replace-cell commands currently report unavailable systems in `InputHandler.Controller.cs`. Preserve truthful responses for reserved commands; implementing missile weapons or energy cells is outside this port.

## 4. Shared architecture

Maintain one repository and one simulation. Add platform boundaries only where observed differences require them.

Proposed ownership:
- Shared gameplay/data: existing Entities/Parts/GameEvents, turns, actions, world generation, content IDs, graph serializer.
- Desktop: existing keyboard/controller adapters, desktop UI, threaded filesystem save backend.
- Phone Web: touch adapter, responsive panels, browser lifecycle bridge, browser save backend, Web build/content profile.
- Small HTML/JavaScript shell: loading/retry screen, browser feature checks, audio activation, file import/export, safe-area/viewport reporting, persistence bridge, PWA lifecycle.

Proposed command flow:

`Touch gesture → contextual player intent → shared action dispatcher → existing action/turn resolution → state snapshot → phone presentation`

Proposed save flow:

`stable simulation boundary → owned snapshot → browser persistence request → durable acknowledgement → saved indicator`

These are design boundaries, not new implemented APIs. Keep the existing desktop queue as a backend. Do not migrate the whole gameplay assembly away from Unity just to begin the port.

## 5. Phone control and layout contract

A phone version must complete the opening, a fight, a trade, travel, inventory management, and saving without an external keyboard.

| Need | Phone behavior |
| --- | --- |
| Movement and waiting | Eight-direction control with deliberate step semantics; explicit Wait. Optional repeat has a threshold and stops on release/danger. |
| World selection | Tap selects or inspects. Show a disambiguation list for stacked occupants and small targets. Selection spends no turn. |
| Attacking/abilities | Select target and see range/affected cells, then explicit confirm; visible Cancel. Implemented force attack and throw remain reachable. Reserved fire/reload commands retain truthful unavailable responses; no new weapon system is implied. |
| Interaction | Contextual Act with readable choices for doors, people, containers, items and services; stairs get explicit actions. |
| Travel | Reuse existing auto-walk/explore and danger interruption. A persistent Stop button cancels immediately. |
| Map | Free pan and zoom. Camera changes never expose undiscovered information or alter simulation visibility. |
| Inventory/equipment/trade | Full-screen touch rows, clear item actions and quantities, equipment-slot selection; avoid tiny grid hit targets. |
| Status/abilities | Compact status strip and a few visible quick slots; expandable access to all existing assignments and abilities. |
| Dialogue/readers/journal | Scrollable text with large selectable rows; no hover, function keys, or keyboard modifiers needed. |
| Save/settings/recovery | Touch-accessible pause menu, durable-save status, Continue/New Game, death recovery, export/import and audio controls. |

Use at least 44 CSS-pixel targets, generally 48 where space permits; default readable text around 16 CSS pixels with user scaling. These are proposed UI targets, measured in effective CSS space, not framebuffer pixels.

Account for notches/home indicator, browser bars, on-screen keyboard, landscape/portrait changes and browser zoom. Cap world rendering resolution independently of text size. A narrower camera follows the player; there is no requirement to fit all 80 columns on screen.

Provide a real software-keyboard path for loadout naming and any other text entry: an HTML input bridge or supported Unity mobile text field focused from a user gesture. Transfer edited values/composition events rather than synthesizing physical letter keys; support selection, deletion, paste, confirm/cancel and existing length/validation rules. While editing, quarantine world commands and ensure the field stays above the keyboard. Verify on a real iPhone.

Phone panels should consume existing state/action models. Where desktop panels combine rendering and actions, extract small presenters/models incrementally. Start with the project's existing UI stack; avoid an unrelated UI framework migration.

Gesture invariants:
- A pointer gesture produces at most one intended action. Ignore compatibility mouse events already consumed as touch.
- Opening/closing a modal, dragging, scrolling, pinching or cancelling must not spend a turn.
- Pointer cancellation, focus loss, rotation and resume release/quarantine all held input.
- Resolve already committed turn/action debt before checkpointing; do not cancel halfway through a paid action.
- Resume paused/neutral; never replay accumulated movement or let UI touches reach the world.
- Test physical touch. Synthetic pointer tests and desktop device emulation are supporting evidence only.

## 6. Browser builds and compatibility

1. Make a clean-machine dependency resolution path. Pin or vendor the developer MCP package at a verified revision, or use an isolated documented build manifest that excludes editor tooling after dependency analysis. Do not silently alter the user's working editor setup.
2. Add a Web build profile/script using the current gameplay scene `Assets/Scenes/Main/SampleScene.unity`, flat presentation and production-safe diagnostics. Retain desktop profiles.
3. Use IL2CPP/WebAssembly and WebGL 2; initially avoid experimental native threading or a mandatory cross-origin-isolated deployment.
4. Audit player-included assemblies/plugins for System.Threading, blocking waits, filesystem assumptions, sockets, native DLLs and Editor-only dependencies. Distinguish reachable runtime paths from tests/tools.
5. Add/link preservation for Parts, Effects, skills and save-restored concrete types. Exercise the full content construction set and representative complex saves with release stripping.
6. Verify URP/shaders, atlas sampling, text, fog, light/shadow masks and render ordering on Safari. Package presence alone does not prove a VFX/runtime incompatibility.
7. Require an explicit user gesture to start audio. Test mute, app switching and audio resume with normal game input.
8. Check safe canvas resizing, keyboard focus, text entry, feature detection and context-loss recovery.
9. Host real release artifacts with correct MIME types and Content-Encoding for compressed WASM/JS/data. Test the exact HTTPS host, not just Unity Build and Run.

M0 may pull forward the minimum M1/M2 save-queue/platform work required to travel without invoking the unsupported desktop worker path. Any temporary nonpersistent save mode must be explicit, isolated and excluded from save acceptance. Full durable storage remains the M2 gate.

The first spike may use an explicitly temporary minimal touch shell; it must not count as complete mobile UI. It must use actual game scenes/content and standard actions rather than a separate toy demo.

## 7. Save safety and suspension

Persistence is release-critical for a persistent campaign.

Choose a browser backend during the spike. Preferred contract: retain the existing serialized payload and metadata, then store them together with a generation ID in an IndexedDB transaction through a small JS bridge. An IDBFS backend is acceptable only if startup hydration, asynchronous sync completion and recovery are demonstrated with equivalent guarantees. Do not combine two competing persistence owners.

- Read available saves before showing Continue. Save state has Pending, Durable and Failed states; success is announced only after persistence acknowledgement.
- Retain the current and previous valid checkpoint. Commit payload, metadata, checksum and generation coherently. A failed new write must not erase the prior checkpoint.
- Schedule checkpoints after settled actions and important transitions; coalesce queued autosaves. Benchmark capture cost before setting frequency.
- On hide/blur, stop new actions and request the latest stable checkpoint when execution is possible. Normal autosaving must tolerate termination without a final pagehide/quit callback.
- The live graph must be captured consistently. If capture is sliced across frames, freeze relevant simulation mutations or capture from an immutable snapshot; a background worker cannot traverse live Unity/game state safely.
- Compression must not require C# worker threads. Benchmark synchronous bounded compression versus yielding/browser-supported compression while retaining a compatible payload format.
- Detect quota/storage failures, show clear recovery/export choices, and request persistent storage when supported. Home Screen installation does not justify promising permanent retention.
- Add user-controlled save export/import before release. Validate size, schema, content/build compatibility, checksum and graph integrity before replacing current state; bound decompression to avoid memory exhaustion.
- Start desktop↔browser transfer with the same tested build/content/schema. Preserve entity identity, inventory references, effects, RNG/scheduler state and cached-zone changes. Do not promise universal backward compatibility.
- Multiple tabs and installed/tab contexts must not silently overwrite one another. Use a writer session/generation guard and explicit conflict handling. Test whether storage is shared in the chosen Safari contexts.
- Keep the save namespace stable across releases. Artifact cache cleanup must never delete campaign data.
- Cloud sync is later work: authentication, conflict resolution and server retention would require a separate plan.

## 8. Performance and content packaging

Apply `Docs/PERF-FOUNDATION.md`: dirty-cell updates, bounded reusable buffers, cache initialization outside hot paths, UI snapshot fingerprints, and no per-frame whole-world scans.

Measure:
- Compressed transferred bytes, startup decompression/compile, time to first usable game.
- WASM heap high-water, textures/audio and other browser memory where observable.
- Input-to-completed-action p50/p95/p99/max, render frame times and zone-generation stalls.
- Save capture/compress/commit costs for fresh, twenty-zone and larger representative campaigns.
- Sustained 30–60 minute play including travel, populated combat, material reactions, repeated menus and saves.

Initial acceptance targets, to revise explicitly after the spike:
- Cold start ≤20 seconds on a documented 20 Mbps connection; warm start ≤5 seconds.
- Target ≤25 MB compressed initial transfer. Larger results require a loading/packaging decision rather than silently calling startup acceptable.
- Responsive 30 fps presentation with ordinary paid-action latency p95 ≤100 ms. Record maxima; long work must show progress and accept cancellation where safe.
- Investigate any non-interruptible UI stall over 250 ms.
- Establish a device-specific measured memory budget with margin; require no tab termination or unbounded retained growth in the long session. Do not use a supposed universal iPhone memory cap.
- Capture and report snapshot work separately from asynchronous storage latency.

Likely work, contingent on measurements:
- Strip unused scenes, diagnostics and assets from the Web profile without removing gameplay content.
- Measure `Resources` inclusion; separate large audio/optional presentation data from initial download. Prepare regional audio outside action/render callbacks.
- Stage content into versioned bundles only if the build report justifies it. Loading a campaign's content must remain complete and deterministic.
- Limit decoded audio, texture atlas residency, pixel ratio and costly post-processing. Keep approved flat art and legibility.
- Yield startup and zone-generation preparation at safe boundaries, preserving RNG ordering and final state. No extra world turns while waiting.
- If long-campaign zone retention dominates memory, add a separately verified serialize/unload/reload path for inactive zones. Never discard changed zones to meet a budget.
- Do not treat desktop profiling receipts as browser measurements.

## 9. Delivery and offline behavior

Use static HTTPS hosting/CDN with a stable origin and a custom Unity Web template. A Start button, progress/retry state and actionable startup errors precede gameplay.

Add manifest/icons and Add to Home Screen guidance after the ordinary browser path works. A service worker may cache a complete version for offline use. An update must not mix old WASM with new data or replace an active campaign session: finish downloading a coherent release, then offer reload at a safe point.

Offline play is promised only for content actually downloaded. Show download completeness and recover from interrupted cache installation. Keep a last working release where storage allows; prune versioned assets separately from saves. Test bad networks, encoding errors, missing bundles and rollback.

No production deployment is performed by this plan.

## 10. Milestones and gates

Agent-work-hour allocations are provisional, not human typing days or delivery dates. Builds, licensed editor setup, physical-device access and review turnaround are additional constraints. Re-estimate after M0.

| Milestone | Planned work | Exit gate | Agent-work hours |
| --- | --- | --- | ---: |
| M0 — Feasibility spike | Reproducible checkout/dependencies, Web build profile, minimal touch, actual flat opening on Safari, startup/render/memory measurements | Physical iPhone boots, takes turns, travels, renders correctly; blocker inventory and preliminary budgets recorded | 6–12 |
| M1 — Runtime compatibility | Thread/file/plugin guards, shared platform seams, IL2CPP preservation, content creation, startup loading fixes | Release Web build exercises standard actions/content without runtime/AOT failures; desktop baseline passes | 8–16 |
| M2 — Durable campaigns | Browser backend, stable snapshots, lifecycle, recovery, export/import, writer conflict protection | Confirmed save survives reload/termination; failed/corrupt writes retain prior valid save; actual cross-platform compatible roundtrip | 16–32 |
| M3 — Complete touch play | Shared action dispatch, responsive HUD/panels, targeting/gestures, all menus and text entry, portrait/landscape | Entire representative opening loop completed by touch, including loadout naming with the iPhone software keyboard; no external keyboard, accidental turns or inaccessible implemented actions | 24–48 |
| M4 — Size/performance | Build report, measured asset/audio changes, safe staging, grown-campaign load/save/memory work | Agreed budgets met on lowest supported test device and long-session route | 12–28 |
| M5 — Web distribution | Hosted loader, headers, install/offline/update handling, diagnostics and rollback | Exact hosted release passes cold/warm/offline/update/recovery paths | 6–12 |
| M6 — Qualification | Physical device matrix, gameplay coverage, adversarial save/input tests, desktop/controller regressions | Evidence-backed support matrix and known-limitations record; no release-blocking save loss/input traps | 12–24 |

Total planning allocation: **84–172 agent-work hours**, with wide uncertainty around mature saves, browser memory and UI extraction. A compiled Web page alone is not a finished phone port.

M0/M1/M2 form the technical critical path. M3 can proceed in parallel once shared action/storage boundaries stabilize. Run device tests from M0 onward, not only at the end.

Decision after early gates:
- Continue with Unity Web when the real opening plus grown-save workload is viable after bounded optimization.
- If the limiting cost is identified as removable assets/UI/stalls, address that cost first.
- If inherent Unity/browser overhead still breaks the agreed device budget, stop expanding this path and scope a shared-core browser renderer with reproducibility and parity tests.
- If browser delivery proves impractical, present measured evidence and the native-iOS alternative; do not silently change the requested delivery platform.

## 11. Acceptance matrix

- **Devices:** baseline iPhone 13 class, a smaller/older device admitted to the support floor, and a recent iPhone; current supported Safari plus the proposed minimum where available. Record physical model, OS, browser, viewport, build SHA and hosting config.
- **Sessions:** browser tab and Home Screen; portrait/landscape, safe area, software keyboard, audio, background/app switch/lock, killed-page recovery, WebGL context loss.
- **Actions:** eight directions/wait, dialogue, readers, stacked-object selection, pickup/drop, equipment, trade/quantities, all implemented targeting forms and throw, truthful unavailable responses for reserved fire/reload/cell commands, stairs/map, travel interruption, crafting/skills, journal, save/load and death recovery.
- **Simulation:** equivalent seeded action sequences preserve relevant final state across desktop and Web. Compare IDs, items, effects, costs, discovered information and scheduler state; investigate differences rather than assuming platform determinism.
- **Content:** generated/reflectively created Parts and representative late-game effects/skills restore under release IL2CPP stripping.
- **Storage failures:** interrupted save, failed transaction, quota denied/full, missing/evicted database, corrupt newest generation, incompatible import, concurrent tab, app update and failed asset update.
- **Scale:** fresh start, twenty explored zones and a larger real campaign; dense combat/material effects, repeated transitions, long idle/resume and 30–60 minute play.
- **Desktop:** current keyboard/gamepad routes, 1280×800 layouts, normal save files, fresh Continue, flat rendering and representative Mac/Linux build checks. Physical hardware claims require hardware evidence.

Implementation follows the repository's RED→GREEN→adversarial→review process for new production behavior. Include counterchecks for no touch/no action, cancelled gestures, failed writes and stale generations. Use source tests, browser integration tests and real device gameplay for their respective claims. The initial plan was documentation-only. Current automated results and their platform limits are recorded in the implementation checkpoint; they do not satisfy native or physical-device gates.

## 12. Review, divergences and handoff

Qud reference: this is a CoO platform/UI adaptation. It preserves the existing Qud-like turn/action model; it claims no new Qud feature parity.

Scope choices:
- Prefer existing flat rendering over a new rendering engine.
- Prefer a shared Unity Web target over a second implementation of the game.
- Full phone action coverage, durable saves and import/export are release scope.
- Account/cloud sync, multiplayer, App Store submission and an independent TypeScript rewrite are deferred.
- Source implementation is underway; actual Unity build and platform validation remain pending.

Findings addressed in this plan: current flat baseline, Deck-versus-iPhone evidence limits, real save threading blocker, current schema 8, non-runtime “MOBILE” docs, AOT reflection risk without claiming reflection is forbidden, and the distinction between browser filesystem writes and durable storage.

Implementation log:
- 2026-11-03: pinned latest remote main; local executor unavailable; inspected current repository rules/source; completed two independent bounded source audits and primary-source browser review.
- Independent plan review: corrected the reserved fire/reload scope, added an explicit mobile text input implementation/gate, and clarified minimum save compatibility needed by M0. These are planning fixes; no runtime acceptance is claimed.
- Historical files changed for the initial planning delivery: `Docs/WEB-IPHONE-PORT-PLAN.md` only.
- Validation: source claims cross-checked; plan reviewed for feasibility and testable gates. Build size, phone performance, Web correctness and actual ergonomics are unverified.

Current next task: reconnect Unity 6000.3.4f1 + Web Build Support, produce the exact-source Web build with `Tools/WebPort/prepare_project.py`, and execute M0 on a physical iPhone. Independent source work and verification have advanced; see [WEB-IPHONE-IMPLEMENTATION.md](WEB-IPHONE-IMPLEMENTATION.md) for the implementation, storage design refinement, test results and remaining work.

## Primary platform references

Checked 2026-11-03. Living Unity 6.3 documentation may describe later 6000.3 patches; verify every required setting/API against installed 6000.3.4f1 during M0.

- [Unity 6.3 browser compatibility](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/intro/browsercompatibility): mobile Safari support; use a top-level page.
- [Unity 6.3 technical limitations](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/intro/technical-overview): managed-thread and AOT/network restrictions.
- [Unity 6.3 memory](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/develop/memory): heap, asset and browser allocation constraints.
- [Unity 6.3 input](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/develop/input): touch and browser keyboard/focus behavior.
- [WebKit storage policy](https://webkit.org/blog/14403/updates-to-storage-policy/): best-effort persistence, quotas, eviction and persistence requests.
