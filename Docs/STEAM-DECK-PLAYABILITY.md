# Steam Deck playability

## Outcome (2026-10-10)

The black world was reproduced in a Linux standalone diagnostic player: OpenGL
drew the character picker, sidebar and hotbar while the chunk stayed black.
The **same executable and assets** rendered the 3D world with Vulkan, including
after continuing a save. Linux player settings now explicitly select Vulkan,
with no silent fallback to the demonstrated failing OpenGL path. No player launch
option is needed. Mac and Windows graphics settings are unchanged.

Native standard-gamepad controls now feed the existing input boundary. Keyboard,
mouse, simulation turns, menu dispatch and save behavior retain their existing
paths. This is game code, not a remotely installed Steam account layout.

Qud reference: none; platform compatibility and original game controls, not a
Qud mechanic port.

## Plan and verification sweep

1. Write native Input System event tests before implementing gamepad bindings.
2. Supply eight-direction movement, normal menus, ability slots and travel through
   existing input dispatch. Latch modifier actions until release.
3. Add controller guidance in help, startup, build selection and pause prompts.
4. Reproduce the black viewport locally; compare graphics backends on the same
   Linux build before changing its default.
5. Run input/menu/render-surface regressions, review, rebuild and package Linux.

| Premise | Evidence / correction |
|---|---|
| No initial input means bootstrap failed | Keyboard-mapped D-pad advances the user's game/log. |
| Native gamepad input already exists | Original InputHelper only reads keyboards. |
| Every standalone player is broken | Mac standalone visibly renders at 1280x800. |
| Mapping stick axes to arrows preserves diagonals | Cardinal-first InputHandler branches drop diagonals; explicit gamepad vector needed. |
| Modifier bindings can follow current held LT every frame | Releasing LT can trigger unintended ordinary actions; latch each press. |
| Confirm alone reaches every starting menu | Save/death gates only accept letter keys; add context-specific A/X adapters. |
| Successful Linux build establishes rendering | Actual Linux OpenGL run reproduced black world; Vulkan restores it. |

## Controls

| Control | Action |
|---|---|
| D-pad / left stick | Move, aim, navigate menus; eight directions supported |
| A / B | Confirm or cast selected ability / back |
| X / Y | Inventory / interact |
| LB / RB / RT | Look / pick up / wait or select underfoot |
| Menu / View | Pause / help or item details |
| Left / right stick click | Skills / abilities |
| Right stick left/right | Previous/next hotbar slot |
| Right stick up/down | Page readers |
| LT + A/B/X/Y/LB/RB/Menu/View/LS/RS | Ability slots 1/2/3/4/5/6/7/8/9/0 |
| LT + direction up/down | Stairs or world-map travel |
| LT + direction left/right | Quests / factions |
| A / X at save or death prompt | Continue/load / new game/restart |

Release the travel direction between actions. Stick hysteresis suppresses drift.
An ability chord never changes into inventory/confirm/wait when LT is released.
Save/death mapping exists only while that gate owns input, preserving ordinary
X inventory and A confirm behavior elsewhere.

Text entry and vendor-specific rear buttons are outside the standard Gamepad
interface. The normal Steam Gamepad layout supplies this interface; custom
keyboard/mouse-only Steam layouts can still suppress native gamepad buttons.

## Implementation

`NativeGamepadInput` samples the actual Input System Gamepad once per input update,
without injecting keyboard events or advancing input updates. `InputHelper` combines
those bindings with existing keyboard reads. InputHandler reads the explicit
movement vector before its legacy cardinal chain. Choice adapters translate A/X
only inside boot and death gates; controllers keep their load-failure safeguards.

The Linux build policy is the serialized equivalent of:

```csharp
PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64,
    new[] { GraphicsDeviceType.Vulkan });
```

Linux builds now require Vulkan. The shader/compositor internals were not changed:
this fixes backend selection; it does not claim to repair Unity's OpenGL path.

## Evidence

Receipts are under `Docs/Verification/SteamDeck/`.

- Controller RED: job `57926e81b9014a59b05ea5195592db36` fails against the original
  keyboard-only input boundary. First GREEN: `9675642dfcda4b099481aa42e992efaa`, 37/37.
- Saved-game gate RED: `0619e92bfb0f4cb4b9d94a1351c20c5a`, 2/2 failing before adapters.
  Updated controller GREEN: `0acb7ad8698a450e893d5ac73dbf5019`, 39/39.
- Graphics-default RED: `73b81c4a786d4b72b342ef2c5e09fcde` confirms automatic API
  selection before the settings change.
- Final native Unity regression: `6ff713523de2476498c79512606b424f`, **264/264 passed**,
  27.12 seconds. Includes input, boot/death/build menus, inventory/readers, look,
  native camera tilt and render-surface lifetime/projection tests.
- `linux-opengl-black.png` / `linux-opengl-player.log`: live Linux gameplay with
  black world and working interface.
- `linux-vulkan-world.png` / `linux-vulkan-player.log`: the same diagnostic binary
  with Vulkan shows the world and continues the saved character.

### What the Linux run can and cannot prove

The original Mono package aborts inside Mono under both Rosetta and QEMU on this
Apple Silicon Mac, before bootstrap. Disabling incremental GC did not resolve
that emulator limit. It is distinct from the Deck's live-game black viewport.
An **IL2CPP diagnostic build** bypassed that limit and exposed the graphics failure.
Only the graphics selection changed between its OpenGL and Vulkan runs.

The test used Ubuntu 22.04, Xvfb at 1280x800 and Mesa software graphics, not a physical
Deck. Selecting the software Vulkan device explicitly was necessary in the test
container; that device index is not baked into the game. Container audio hardware
is absent, so its audio warnings are not audio verification.

The delivered game retains its existing **Mono** scripting backend and incremental
GC. Temporary compiler packages and the diagnostic compiler shim are removed from
project source. Native Unity device-event tests establish binding behavior; they
do not prove a physical Deck controller, Steam Input configuration, audio or
hardware performance. No offline-runner count is used as GPU evidence.

## In-phase self-review

- 🟡 Fixed: cardinal-first keyboard dispatch lost gamepad diagonals. Eight-direction
  movement and targeting tests cover every direction and release.
- 🟡 Fixed: changing LT while holding a button could trigger a second action.
  Press-time latching and counter-checks prevent ordinary-action leakage.
- 🟡 Fixed: saved-game/death letter-only gates stranded a controller player.
  Scoped adapters and one-choice/LT-chord counter-checks cover both gates.
- 🟡 Fixed: automatic Linux graphics chose the black-world OpenGL path.
  Linux Vulkan policy is regression-tested, with actual failing/passing screenshots.
- 🧪 Deferred verification: physical Deck controller/graphics/performance and the
  final Mono executable on x86 Linux hardware. Local Mono emulation cannot run it.
- ⚪ Deliberate: Linux Vulkan requirement; no fallback to the failing backend.

Cold-eye pass: checked adapter scope, modifier release symmetry, disconnect clearing,
keyboard coexistence, unchanged save dispatch, platform-only settings and guidance
against actual mappings. No remaining notable implementation findings.

## Files

- NEW `Assets/Scripts/Presentation/Input/NativeGamepadInput.cs` (+ meta).
- MOD `InputHelper.cs`, `InputHandler.cs`, `SaveLoadInputAdapters.cs`.
- MOD `ControlsReference.cs`, `BootMenuController.cs`, `DeathScreenController.cs`,
  `StartingBuildMenuUI.cs`, `PauseMenuUI.cs`.
- MOD `ProjectSettings/ProjectSettings.asset` (Linux graphics API only).
- NEW `NativeGamepadInputTests.cs`, `SteamDeckBuildSettingsTests.cs` (+ metas).
- NEW this living document and the verification receipts.

Packaging result is recorded in the release folder's build report, build-info and
checksums. Save files live outside the extracted game folder.

## Release build

`build-f3e9db46ef`: Linux Mono, Vulkan-only, normal SampleScene, non-development;
37.79 seconds, 0 errors, 4 warnings. Output:
`Builds/SteamDeck/2026-10-10-vulkan-controller/CavesOfOoo/`.
Launching this exact final executable without `-force-vulkan` selects and initializes
Vulkan; the local Mono emulator then hits its known startup assertion. This checks
the packaged graphics default while preserving the full-playability limit above.
