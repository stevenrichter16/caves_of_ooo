# Caves of Ooo — standalone web expedition

Play: https://caves-of-ooo-web.stevenaaaaaa.chatgpt.site

A playable, touch-friendly browser edition of Caves of Ooo. The existing Unity project stays independent. This edition runs the real C# gameplay and JSON content as .NET WebAssembly, with a canvas map and HTML interface. It needs no Unity installation, Unity player, account backend, or gameplay server.

Open the deployed site in Safari on iPhone; Share → Add to Home Screen gives it a standalone window. Desktop keyboard controls also work. The first visit downloads roughly 30 MB before HTTP compression. After the service worker finishes caching the release, the game can reopen offline while the browser retains that cache.

## Playing

- Use the eight-direction compass or arrows / H J K L / Y U B N. Center dot or period waits.
- Tap a tile to inspect for free; drag to pan. Bump hostile creatures to attack.
- Turn your phone sideways for a full-height map, with health, focus and controls in a compact side rail. Aiming puts Cancel/Confirm beside the map. Rotating preserves your zoom and takes no turn.
- Pack holds equipment, consumables, grouped pickup, crafting and alchemy. Nearby interactions expose conversations, trading, gathering, repairs and world actions.
- Abilities target visible cells. Throws have a preview and explicit confirmation; Measured Throw prepares for one turn and launches on a separately confirmed turn.
- Skills and attributes use the native requirements and purchase confirmation. The journal shows objectives and completed quests.
- Stand on stairs, then use More → Ascend/Descend. Cross a zone edge to travel.
- Keyboard shortcuts: I pack, A abilities, E interactions, Shift+K skills, Shift+J journal, M visited places, C character, F5 checkpoint, Escape close.

Read the notice in Gleaners’ Glade. The well needs fire clay; the cellar and neighboring settlement of Sill are part of the original world.

## Checkpoints

Checkpoints are stored in IndexedDB on this device and origin, with generation checks, atomic commits and a previous valid revision. Save after several mutations, on zone transitions, manually, and best-effort when backgrounding. Wait for **Saved on this device** before closing. iOS may terminate a page before an asynchronous background save finishes.

Export downloads a same-edition backup, including the current living run even when browser storage is full. Import validates the complete simulation graph before committing records. A death or engine fault preserves the last healthy checkpoint. Different browsers/origins have separate saves. Safari storage eviction and clearing browser data can remove them; keep exported backups. Desktop Unity save interoperability is not promised.

## Build and verify

Requirements: .NET 8 SDK, Python 3; Node 22+ for verification. No Unity tooling is needed.

```sh
python3 import-core.py ..
python3 scripts/build.py
python3 -m http.server 8098 --directory dist
dotnet run --project Verification/ContractTests.csproj
npm ci
npm run test:storage
npx playwright install chromium webkit
npm run test:browser
npm run test:landscape
```

`dist/` is the deployable static site. `scripts/build.py` generates a content-versioned service worker from the exact release. An update waits for existing game tabs to close; it cannot replace assemblies under a running game. Serve `.wasm` as `application/wasm` over HTTPS in production. The published Site keeps its own private hosting identity. This directory contains the portable source; no deployment credential is stored in the repository.

`Verification/browser-smoke.cjs` and `Verification/landscape-smoke.cjs` accept `COO_SMOKE_URL` and `COO_SMOKE_OUTPUT`. The landscape suite covers three phone sizes, menus, aiming, movement, checkpoints and orientation changes using the real WASM game. Mobile viewport/WebKit tests do not establish physical iPhone performance or OS background lifecycle behavior. See `Verification/RESULTS.md` for this release’s evidence and remaining limits.

## Architecture and future updates

- `Core/Source` and `Core/Content`: imported gameplay and data, pinned by `source-provenance.json`.
- `Core/Platform`: minimal compatibility types and embedded JSON loading. These are ordinary C# code, not the Unity runtime. The approximate Perlin implementation and stable string hashes follow the existing headless adaptation; procedural seeds need not produce identical desktop maps.
- `Core/Bridge`: synchronous intent facade, native action/turn ownership, visibility, dialogue, skills, inventory and snapshot projection.
- `Client`: thin JS-invokable bridge, canvas renderer, mobile DOM interface, checkpoints and backup coordinator.
- `Verification`: real-core contracts, storage-coordinator fault tests, and actual WASM browser acceptance.

To update the imported core from a local checkout, review the upstream diff, then run:

```sh
python3 import-core.py /path/to/caves_of_ooo --commit FULL_COMMIT_SHA
python3 scripts/build.py
```

The importer requests only selected Git blobs, which also works with partial clones. It does not replace browser adapters. It applies six bounded source adaptations: five stable string hash substitutions and complete-session validation before publishing an imported save. The platform layer stays separately owned. Keep the exact upstream commit and run the contracts before publishing.

This is a first playable browser edition, not a claim of complete presentation parity. It retains original starting builds, authored opening, world generation, AI, combat, quests, inventory, trading, abilities, progression and crafting rules. The original opening art is used with glyph fallback elsewhere. Native lighting, animated effects/audio, controller-specific menus, full region art treatment and advanced crafting/ability-slot presentation remain future work. The core and full content are present, but every late-game interaction has not been browser-qualified.

The useful-affliction and misplaced-season ideas discussed in chat have not been added as new lore during this port. This build uses the repository’s authored content.
