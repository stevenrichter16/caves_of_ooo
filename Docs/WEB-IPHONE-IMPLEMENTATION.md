# iPhone Web implementation checkpoint — 2026-11-03

Status: **experimental source implementation; native build and physical-device
qualification blocked**. This is not a released or demonstrated playable phone
build. Main was pulled and fast-forward checked at
`8c4573ae51a58916884e23efa282859d0669d645`. Work is isolated on
`codex/iphone-web-implementation`; the desktop project remains available.

The user explicitly authorized planning, design and implementation. The earlier
[port plan](WEB-IPHONE-PORT-PLAN.md) remains the acceptance contract. This checkpoint
advances the independent work while M0 awaits a working Unity Editor connection.

## Implemented behavior

The existing Unity simulation, content and save format 8 are shared. The Web
bootstrap waits for browser checkpoint discovery before it can create a game.
An unavailable or corrupt database presents an error/recovery route instead of
being treated as an empty campaign. Browser save roots no longer trigger the
native audit's starting-build suppression.

The browser uses its own IndexedDB database and a dedicated MEMFS save root.
Each transaction stores a compressed payload, metadata, integrity hashes,
revision, and previous complete generation together. C# captures an owned
snapshot and uses a serial callback queue without `Task.Run` or blocking waits.
Canonical local files become visible only after transaction completion. Writes
from another tab are detected with revision comparison; the stale session must
reload. Publication failures invalidate storage for the current session.

Save acceptance says “Saving…”; success appears after acknowledgment. Pending
loads, resets, root changes and runtime replacements are gated. Deferred stable
requests retain every explicit slot, while ordinary autosaves keep the active
and newest pending snapshot. Desktop threading/file behavior is retained.

Backup export is read-only and does not refresh a live session's revision.
Import requires the same build, validates all entries before mutation, checks
gzip integrity and the game header, enforces aggregate size limits, and commits
the bundle atomically. Raw recovery export retains malformed stored data for
later recovery; it is intentionally not an ordinary import format. This is
browser-to-browser backup support, **not a qualified desktop save transfer UI**.

The browser shell has safe-area layout, 44px controls, eight directions, wait,
free map inspection, explicit directional confirmation, ability controls,
character/actions, stairs/travel, pause/save, recent messages and text naming.
A bounded, revision-scoped input mailbox feeds the existing InputHandler after
its turn and modal gates. Pointer holds cannot leak into a newer view. Phone
travel has explicit ownership; opening browser overlays cancels travel. The
background path clears pending FX state before settling its existing turn and
does not overwrite a living checkpoint with a dead character.

The DOM reader reflows the current game's menu text at 16px and sends the existing
menu keys. It reads both CP437 tilemaps and retained `SkillScreenText` labels;
colon/bracket footer actions and the inventory Commands menu remain reachable.
Loadout naming submits a browser text field. This is an initial presentation
adapter: actual item/equipment/trade/quest/dialogue selection, text ordering,
highlighting, scroll ergonomics and every modal must still be qualified in Unity.

A disposable-project exporter removes only the local MCP build dependency and
its five Editor adapters, pins the existing Newtonsoft version, and supplies
reflection preservation. The build entry pins Unity 6000.3.4f1 and selects the
actual `Assets/Scenes/Main/SampleScene.unity` with IL2CPP/WebGL 2. It generates hashed assets, provenance,
and the offline asset manifest. The worker caches the current build before
installation completes and waits for old game tabs to close before updating.

## Verification evidence

| Verification | Result | Scope |
| --- | --- | --- |
| Existing desktop save/menu baseline | 111/111 | Frozen main, .NET runner with opt-in Unity/input/log adapters |
| Combined current save/menu/input-contract tests | 127/127 | Same 111 plus 16 new cases; no Unity presentation execution |
| Browser storage, failure and backup tests | 24/24 | Node, real production JS, fake IndexedDB fault injection |
| Clean project preparation | 13/13 | Temporary Git repos, committed refs, untouched source, dependency/preservation/provenance checks |
| Browser shell/storage/offline tests | 42/42 | Chromium phone, WebKit iPhone-sized portrait and landscape; actual browser storage/service worker, mocked Unity/WebGL |
| Unity Editor/IL2CPP compilation | Blocked | No licensed local editor; both configured Unity MCP endpoints returned 404 |
| Physical iPhone gameplay, memory, startup, audio | Not run | No hardware/runtime evidence |
| Desktop native/player/controller regression | Not run this checkpoint | Headless tests do not replace the required native gate |

The broader pre-existing headless selector compiled 1,170 files and excluded 777.
A 200-case baseline subset had six reproducible failures: four claimed-supplies
generation assertions, one Felling-site null-zone case, and one allocation case
(72,000 bytes vs. 2,048). They were reproduced on frozen main in isolation; their
causes remain unconfirmed. The focused 111-case before/after comparison avoids
presenting excluded or failing baseline tests as passing.

Reproduction commands are in [Tools/WebPort/README.md](../Tools/WebPort/README.md).
Receipts are retained under `Docs/Verification/WebPhone/2026-11-03/`. WebKit here
is Playwright's Linux WebKit 27.2, not Safari on an iPhone. The cloud's missing
browser libraries were extracted locally; no desktop project or system-wide
package installation was needed.

## Review fixes and remaining gates

Independent review found and corrected volatile-file publication failure,
aliased backup buffers, lost deferred slots, browser-root audit suppression,
direction selection spending turns, background FX double settlement, inaccessible
colon footer actions, stale throw details, travel behind overlays, landscape
reader width, topmost modal mirroring, and first-visit offline asset caching. Tests cover the pure and
browser contracts; source review is the only evidence for the native input hooks.

A final committed-project dry run caught and corrected a stale scene path
missing the `Main/` directory; build entry, exporter, plan, and fixture now match
`EditorBuildSettings.asset`.

The first next step is a real build using the documented exporter and Unity
6000.3.4f1 + Web Build Support. Do not merge runtime changes or call the port
finished before native compile, desktop regression, and physical-phone gates pass.
The current two Unity connection URLs both return 404; reconnecting the user's
Editor is the concrete external dependency.

Remaining work is material:

- Measure the full Unity download, cold/warm startup, peak heap, audio allocations,
  grown-campaign save stalls, and 30–60 minute play on admitted physical devices.
- Audit all reflected content under IL2CPP. Broad assembly preservation is a
  first compatibility setting with an unmeasured download cost.
- Hydration currently reads and mirrors all retained campaigns and keeps their
  acknowledged snapshots. Bound or lazily load that set if device measurements
  require it; a corrupt unrecoverable campaign currently blocks ordinary boot
  while preserving recovery export.
- Complete native touch/lifecycle tests, inventory/trade/targeting/readers and
  actual software-keyboard ergonomics. The generic reader may need specialized
  layouts for dense menus. Native controller-specific footer wording with an
  attached gamepad also needs a mixed-input check.
- Verify actual Safari audio unlock, WebGL context loss, Home Screen icon/install,
  quota eviction, termination/reload, offline full Unity startup and update/rollback.
- Qualify same-build desktop/Web save transfer and seeded action parity. The
  current browser backup format does not claim automatic cross-device sync.

No lore, simulation balance, reserved missile-weapon features, or desktop asset
content was changed. No public release or phone support claim has been made.
