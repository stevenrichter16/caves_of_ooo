# Steam Deck: Qud-style native controls

Status, 2026-10-10: implementation and native review complete. Final focused
EditMode verification passed **407/407**, zero skipped; final native Play passed
**39/39**, with visual acceptance of the corrected labels and menus. The Linux build and archive verification are complete; source commit `44d8484c8` is pushed to main.
No physical Steam Deck test is claimed. This
replaces the earlier native binding plan in `STEAM-DECK-PLAYABILITY.md` and
requires no manually configured Steam keyboard layout. Keyboard controls remain
available alongside standard native gamepad input.

## Sources and confidence

[STEAM-DECK-QUD-CONTROLS-SOURCES.md](STEAM-DECK-QUD-CONTROLS-SOURCES.md) records the
source identities, complete inspected bindings and public corroboration. The
primary assignment evidence is the installed Qud build **24626113** plaintext
`StreamingAssets/Base/Commands.xml`, SHA-256
`15041531378847eba3e3a7efbb5212c5234e4698d880f1358b02eb58500430b7`.
Existing older source, assembly **2.0.210.24**, supplements direction/repeat
semantics; it is not assumed identical to the installed build. No Qud
implementation code is copied.

Developer notes corroborate [Y edge travel, L3 points of interest and LB nearest
attack](https://freeholdgames.itch.io/cavesofqud/devlog/764265/feature-friday-july-12-2024),
[LT+RT highlight/item details](https://freeholdgames.itch.io/cavesofqud/devlog/775784/feature-friday-august-2-2024),
[LT+LB force attack](https://freeholdgames.itch.io/cavesofqud/devlog/348340/the-deep-jungle-feature-arc-is-here)
and [danger-sensitive held movement](https://freeholdgames.itch.io/cavesofqud/devlog/552024/gamepad-support-new-abilities-screen-beta-is-now-live).
The community wiki table was inaccessible; no unseen row is treated as evidence.
Older/customized guides do not override the inspected defaults. These sources
establish the physical layout, not complete Qud behavior or feature parity.

## Final bindings

Names use Steam Deck/Xbox face positions. Menu means Start; View means Select.
The left trigger selects the alternate layer.

| Control | Caves of Ooo action |
| --- | --- |
| Left stick; RT | Select a direction for free; RT takes a step. Neutral RT waits one turn. Held RT repeats until danger and requires release to rearm after interruption. |
| A; LT+A | Use the indicated cell, or contextual underfoot/nearby action; LT+A offers nearby interaction when no direction is selected. |
| B; LT+B | Recover through a nearby bed/resting campfire when injured; LT+B opens one/10/100-turn waits and recovery options. Ordinary waiting does not heal. |
| D-pad left/right; X; LT+X | Select a hotbar slot; activate its ability; open all abilities. |
| Y; LT+Y; L3 | Walk toward a local edge; approach safe known frontiers; choose a visible reachable point of interest. |
| LB; LT+LB | Attack an adjacent visible hostile; force attack in the chosen direction. A missing direction opens explicit direction selection. |
| LT+RB | Select a carried throwable, then aim and confirm. |
| Right stick; LT+RT | Look; highlight visible points of interest. |
| D-pad up/down | Existing stairs and world-map actions. Release between actions. |
| LT+D-pad left/right | Zoom in/out. |
| LT+D-pad up/down | Explain the existing single ten-slot page and access to all abilities. No invented extra pages. |
| Menu; View; LT+Menu | Character menu; pause menu; controls reader. |
| RB; R3; LT+R3 | Explain unavailable missile-fire, reload and energy-cell systems. These do not trigger unrelated actions. |
| Menus | D-pad/left stick navigate; A confirms, B backs out. Y changes tabs where supported; LB/RB use existing page controls; right stick scrolls readers; LT+RT opens supported item details. |
| Ability manager | Y opens the selected ability's slot chooser. A confirms assignment or the clear option; B cancels. Assignment clears the ability's previous slot and replaces the destination, without creating duplicates. |
| Targeting | Sticks/D-pad select a cell; unmodified A or RT confirms, B cancels. LT+RT remains the details chord and never confirms a targeted action. |
| Boot/death/build | A continues/loads or begins the selected build; X requests the existing new-game/restart action; D-pad browses choices. |

The character menu reaches inventory/equipment, attributes/effects, skills,
abilities, quests, factions and controls. Pause retains save/load, graphics and
quit. A also accepts the existing neutral-creature attack confirmation; B
cancels without spending a turn. Controller bindings do not expose debug cheats.

## Deliberate adaptations and boundaries

Qud's B binding requests healing; CoO has no passive regeneration. B therefore
opens actual nearby bed/campfire recovery, preserving ownership, payment and
safety rules. Full health is a free no-op; without a service it explains available
recovery. It does not invent free healing or consume hundreds of futile turns.
The existing one-page hotbar, separate character screens and unsupported missile
systems are disclosed in help rather than presented as complete Qud equivalents.
Menu-specific Y assignment is a CoO adaptation.

Travel is conservative and local: known visible/explored route cells only, whole
actor footprints, existing diagonal rules, no positive terrain/gas hazard cost,
actual fire or active traps. It stops for visible threats, damage, blocked
movement, user input, actor/zone/session changes or its finite budget. It does not
auto-loot, open doors, attack, enter unknown space blindly or cross zones. Existing
steam safety can conservatively reject unseen adjacent hot steam without naming
or revealing it. World-map auto-travel is refused. See
[STEAM-DECK-QUD-TRAVEL.md](STEAM-DECK-QUD-TRAVEL.md) for planner contracts.

A POI menu builds **one bounded reachability map**, then tests approach candidates
against detached bits. The map is discarded after menu construction; actual
travel rechecks the live world before every step. No admission/path cache survives
world mutation. Native input is split into World/Menu/Targeting contexts;
keyboard-only movement fallback prevents free stick aiming from becoming walking.
Held buttons, sticks and modifier state must rearm after context/device changes;
actor, zone and TurnManager replacement also quarantine held controls.

## Verification sweep and fixes

| Finding | Resolution and meaningful counter-check |
| --- | --- |
| Flat gamepad-to-key aliases confused free direction selection, paid movement and modal confirmation | Typed world commands and explicit modal keys; all eight direction tests assert no cost before RT and a paid step afterward. Keyboard remains independently usable. |
| LT+RT leaked confirm during targeting | Route the modified trigger to details instead of Return; a targeting/throw regression asserts no confirmation. |
| Held LT or trigger could leak a new action across context/chord changes | Quarantine the modifier and latched controls until release, including LT introduced during held RT. Tests cover chord order, menu transitions and reconnect. |
| A hysteresis fixture treated queued raw stick values as processed values | Correct fixture inputs to account for Unity's deadzone processor before adapter hysteresis. This fixes the test premise; it does not weaken production thresholds. |
| Every POI approach candidate repeated danger scanning and BFS | One BuildReachable query per menu; 15 added cases pin known-cell admission, ownership after mutation, safety parity, bounds and purity. Live stepping stays fresh. |
| Neutral-creature confirmation did not accept controller A | Reuse the existing confirmation path; A accepts and B cancels with corresponding hostility/turn checks. |
| Ability assignment required keyboard numbers | Add Y slot chooser through existing assignment logic; moving a binding clears its old slot and replaces an occupied destination. This is replacement, not a swap of two abilities. |
| Force attack could miss/veto without committing hostility | Reuse ExecuteAttackOnNPC so aggression is recorded independently of damage, preserving existing attack cost. The regression explicitly vetoes the attack. |
| Nearest attack considered an invisible owner on a visible cell | Require both visible cell and visible RenderPart, with visible/hidden counter-cases. |
| Held RT survived actor/zone/TurnManager replacement | Refresh session identity before dispatch and quarantine current controls; rebind test requires neutral release before another paid step. |
| Retained hotbar still displayed keyboard hints with a connected controller | Resolve device/pending-ability guidance before header invalidation; test actual tile glyphs, same-snapshot connect/disconnect, keyboard counter-cases and equal-frame no-op. |
| Initial native walk route reached unsafe terrain | Correct the audit's approach using actual keyboard walking around the east wall. Both subsequent runs pass walk-start, paid-step and held-cancel checks without teleporting or weakening planner safety. |
| Highlight candidates existed but no labels were drawn | Actual draw instrumentation proved zero rendered labels. Row-major selection filled the first 64 candidates with off-camera plain grass, beginning at (0,0). Plain terrain Physics/Examinable alone no longer qualifies, while explicit harvest/service targets remain eligible. Cached candidates sort by proximity; rendering accepts at most 24 nonoverlapping on-screen labels, without the old first-64 cutoff. Final native Play passes the actual draw check, and visual review confirms 16 visible nonoverlapping labels. |
| Character/wait choice menus inherited “You see Grass” and inappropriate details hints | Optional menu-specific titles and context-appropriate footers followed a seven-case RED and now pass native GREEN. Existing world menus retain their normal status/details behavior. |

Independent reviews found no remaining actionable issue in the planner's
known-cell admission, body/diagonal rules, bounded search and detached map
ownership. Pre-execution review also corrected the native audit's held-button
fixture; subsequent Play runs still exposed the route and label problems below.
Reviews supplement executed tests; they do not establish completeness or hardware feel.

## Test-first evidence and methodology

New behavior was specified in fixtures before its production implementation.
Root owns Unity compile, test and Play execution. Pure planner checks also ran
against actual gameplay source in an isolated runner with Unity stubs; those are
supplementary and never substituted for native Input System verification.

| Milestone | Executed evidence and limits |
| --- | --- |
| Initial native input RED | `Verification/SteamDeckQudControls/red-compile.txt`: missing GamepadCommand/context/API before production changes. Job `c09ab1f70fe74f79bf0c223737ccdf1c` ran a stale 39-test assembly during compile errors; its pass is excluded. |
| Planner RED | `travel-red-compile.txt`: missing ControllerTravel stopped integration/test compilation. It also contains an unrelated menu-access error; this is compile RED, not assertion-level RED. |
| First integrated native run | Job `c9484798f29a4ca88e5cf9daf06f3be1`, 177 cases: two failures identified targeting LT+RT leakage and the processed-stick fixture error. All original 64 planner cases passed. Receipt `first-native-tests.json`. |
| Reachability RED and isolated GREEN | `reachability-red-compile.txt`: missing BuildReachable in new tests. After implementation, 79/79 planner cases passed offline, zero skipped; `controller-travel-reachability-offline-green.xml`. |
| Gameplay follow-up RED | Job `818ed095a441479baa4c65947178fecf`: 24 cases, five failures covering A confirmation, Y binding, force-attack veto hostility, held-trigger rebind and hidden nearest target. Production fixes followed this result. |
| Help RED | Job `3a283dc480d64b9fbabf1cfa6c632998`: five cases, four intended failures and one passing counter-case before help changes. |
| Hotbar RED | Job `c6db50219f004313bd29e74c7c097070`: five cases, three expected controller failures and two keyboard passes; `hotbar-native-red.json`. Earlier zero-case jobs resulted from a malformed 33-hex script metadata GUID; it was corrected to a valid UUID. Zero-case runs supply no test evidence. |
| Initial focused native GREEN | Job `03862ce6b8e64a5aab1d63bd7329eaba`: **344/344 passed across 17 classes**, including all 79 planner cases, native boundary, gameplay, help, hotbar and existing regressions. Receipt `native-editmode-green.json`. This predates the subsequent label/menu corrections. |
| Choice-menu follow-up RED | Job `a921f2e833034432953a7e4ec85c2e1e`: seven cases, six failures and one pass; `choices-native-red.json`. Failures cover truthful choice titles, controller footers and restoring ordinary world-menu state. |
| Terrain-interest follow-up RED | Job `d0fe6df1ad2c45e892ad13161f8011bb`: 34 cases, one failure for plain-terrain admission; remaining cases passed, including all seven choice-menu cases. Receipt `terrain-native-red.json`. |
| Final focused native GREEN | Job `ffbf73765b284474b9bfcd8ea01a80f4`: **407/407 passed, zero skipped, across 20 classes**, covering the earlier suite plus choice-menu, terrain-interest, ability and world-interaction regressions. Receipt `final-editmode-green.json`. |

Receipt paths in this table are under `Docs/Verification/SteamDeckQudControls/`.
Totals are reported from executed native results, without inferring passes from
compile success, test discovery, absent failure text or unchanged old assemblies.
The 407-case receipt supersedes the earlier focused EditMode checkpoint. It does
not replace the final Play/visual verification or Linux delivery gates.

## Native audit and delivery gates

The prepared `ReferenceGladeNativeBatch.LaunchController()` audit starts an
isolated ordinary seed-64 game and injects standard Gamepad states through the
production Input System/InputHandler. It checks free facing, paid RT/neutral
wait, keyboard coexistence, real interaction, character/inventory/graphics,
ability targeting cancellation, highlights and bounded travel interruption.
Scheduler tick/energy observations distinguish free input from paid actions.
Screenshots support visual review. It uses isolated saves and restores graphics
preferences and the previous current gamepad, removing its synthetic device.

| Native Play attempt | Observed result |
| --- | --- |
| `90f565cfee954571b0e07a6426d18afa` | Incomplete: 34 passing checks and a failed safe-northern-approach precondition. The label check counted candidates, not actual draws; capture 08 exposed its false confidence. This is not a 39-check pass. |
| `ffc95fbd98024157badb42ad1485bdd6` | 39 cases, one failure, incomplete. The corrected real keyboard route passes all travel checks, including paid movement and held-input cancellation. Actual label-draw instrumentation fails at zero labels. |
| `46c7171ceb264a0793d64e465e8afdb2` | 39 cases, one failure, incomplete. Travel remains passing. Diagnostics identify plain grass from (0,0) filling the first 64 off-camera candidates; the label-draw failure is reproduced. |
| `e5dde5d513c84863a527c54330459975` | Final: **39/39 passed**, complete true, zero failures/unexpected errors, ten captures, 9.56 seconds. Root visually accepted highlight capture 08 with 16 visible nonoverlapping labels, Wait capture 09 with its correct title/A/B footer, and ability/aim guidance in captures 06/07. |

Each report is archived at `Verification/SteamDeckQudControls/Native/<run-id>/report.json`.
The final run closes the route, actual label drawing and menu presentation
findings, alongside the 407-case EditMode GREEN. Unity's temporary post-compile
disconnect supplied no test evidence; the subsequent executed receipts do.
The report's `publicRouteComplete` field belongs to an older content-audit mode
and is not the controller completion criterion; this mode uses `complete`, its
39 checks and error counts.

Native Play and screenshot review are complete. The fresh Linux player, launcher/help package and checksums have been verified (delivery receipt below). The Play route is
finite and synthetic; it does not cast an ability or attack a creature, prove
complete Qud parity, test Steam Input remapping, measure physical Deck performance
or establish ergonomics. Native macOS test success is not Linux/Deck runtime
success. Do not claim delivery until those separate receipts exist.

## Changed surfaces

- Native dispatch and integration: `NativeGamepadInput.cs`, `InputHelper.cs`,
  `InputHandler.cs`, new `InputHandler.Controller.cs` and
  `InputHandler.ControllerHints.cs`.
- Planner: new `Gameplay/World/ControllerTravel.cs`; core and dedicated
  adversarial ControllerTravel fixtures, with fresh script metadata.
- Display/help: `ControlsReference.cs`, `HotbarRenderer.cs`, `WorldActionMenuUI.cs`,
  `Tools/SteamDeck/README-Steam-Deck.md` and `STEAM-DECK-PLAYABILITY.md`.
- Native fixtures: NativeGamepadInput core/adversarial, QudControllerGameplay,
  QudControllerHelp, QudControllerHotbarHint and QudControllerChoiceMenu tests.
- Play harness: `ReferenceGladeNativeBatch.cs`, `ReferenceGladeNativePlayer.cs`,
  its graphics-audit integration and new `ReferenceGladeNativePlayer.ControllerAudit.cs`.
- Evidence: this living plan, source record, travel contract and
  `Verification/SteamDeckQudControls/` receipts.

Self-review: 🟢 final focused native GREEN, planner review and native Play/visual
acceptance and verified Linux package; ⚪ explicit CoO adaptations and unsupported systems;
⚪ physical Deck feel/performance unverified.


## Linux delivery — 2026-10-10

- Source: `44d8484c87ff85512e3414f49a3925ca870cb792`, fetched/rebased against main before push. This documentation-only follow-up does not change the built source.
- Build job `build-1d5b0f420f`: Unity 6000.3.4f1, Linux x86_64, Mono, non-development, SampleScene, Vulkan. Succeeded in 22.25 seconds with zero errors and the same four pre-existing compiler warnings (effect-owner hiding, bootstrap unreachable branch, unused backstab field).
- Archive: `Builds/SteamDeck/2026-10-10-qud-controls/CavesOfOoo-SteamDeck-QudControls-44d8484c8.tar.gz` (101,148,945 bytes).
- SHA-256: `adc53ca75c2a76845cdcd62d062d3d4061a5ffe07ad95d839f770d5489f00d81`.
- All 161 payload files were read back from the archive and matched their SHA-256 manifest; member list and launcher/player executable permissions matched. Current README and launcher are included, along with build/test provenance.
- Receipts: `Verification/SteamDeckQudControls/build-report.json` and `package-checks.json`; the build folder also contains the full audit receipts and build-generated settings diff. Generated rendering/build settings were restored. Existing local Unity logs and Linux package configuration changes were preserved and excluded from these commits; packaging records the configuration difference.
- On Deck: replace the entire old game folder with this archive's `CavesOfOoo` folder. Keep normal Steam Input **Gamepad** output; an old keyboard-only override masks the built-in buttons. No hand-authored keyboard mappings are required. Saves remain in the existing separate save directory.
- Honesty bound: successful Linux compilation and verified packaging do not constitute running the binary on a physical Deck. Native macOS Input System/Play evidence is recorded above; Deck performance, ergonomics and Steam Input behavior remain unmeasured.
