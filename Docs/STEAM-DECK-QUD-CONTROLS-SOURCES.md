# Qud controller source record

Verified 2026-10-10. This records functional bindings, not copied Qud implementation
code. It supports the bounded adaptation in [STEAM-DECK-QUD-CONTROLS.md](STEAM-DECK-QUD-CONTROLS.md).
The installed defaults are stronger evidence for current button assignments than
old tutorials or customized community layouts. They do not establish complete
Qud behavior or parity with Caves of Ooo.

## Source identity and confidence

| Source | Identity | What it establishes |
| --- | --- | --- |
| Installed Qud plaintext defaults | Steam app 333640, installed build **24626113**; `CoQ.app/Contents/Resources/Data/StreamingAssets/Base/Commands.xml` | Exact default action/button assignments in this installed build, including modifier and modal layers. This is local primary evidence, not an online table or proof it is the newest available build. |
| Installed file digest | SHA-256 `15041531378847eba3e3a7efbb5212c5234e4698d880f1358b02eb58500430b7` | Identifies the exact inspected default table. |
| Existing decompiled source | `/Users/steven/qud-decompiled-project`, assembly **2.0.210.24** (`Properties/AssemblyInfo.cs:8`) | Supplemental evidence for direction selection, modifier loading and dispatch. This older source is not asserted to match every behavior in installed build 24626113. No new decompilation was performed. |
| Freehold release notes | Dated developer posts linked below | Public primary corroboration for specific control changes and behavior. |
| Community wiki table | Linked by its author in July 2025 | Discovery evidence only: the wiki/table image was not readable through the available browser/HTTP tools. Its contents are not claimed as verified. |

Exact installed path inspected:
`/Users/steven/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/StreamingAssets/Base/Commands.xml`.
Steam build identity comes from adjacent `steamapps/appmanifest_333640.acf`.
`DefaultKeymap.json` and `RewiredKeymap.bin` are keyboard map evidence and were not
used to infer the gamepad defaults.

## Installed adventure bindings

Names use the Xbox/Steam Deck face positions: A south, B east, X west, Y north.
L3/R3 mean stick clicks; Menu is Start and View is Select. LT is the alternate
modifier. Nintendo-specific alternate face labels in the XML are not Deck labels.
Line numbers below refer to the inspected `Commands.xml`.

| Control | Qud action | XML line |
| --- | --- | --- |
| Left stick | Indicate direction | 217–220 |
| RT | Take a step | 232 |
| A | Contextual use | 417 |
| B | Wait until healed | 482 |
| X | Use selected ability | 660 |
| Y | Move to edge | 438 |
| LB | Attack nearest | 468 |
| RB | Fire missile weapon | 450 |
| L3 | Move to point of interest | 434 |
| Right stick | Look direction | 427 |
| R3 | Reload | 454 |
| Menu | Character / attributes | 682 |
| View | System menu | 825 |
| D-pad up / down | Move up / down between levels | 151 / 157 |
| D-pad left / right | Previous / next ability | 657 / 654 |
| LT + A | Interact nearby / get from indicated direction | 443 |
| LT + B | Wait menu | 489 |
| LT + X | Abilities screen | 667 |
| LT + Y | Autoexplore | 497 |
| LT + LB | Directional force attack | 235 |
| LT + RB | Throw | 462 |
| LT + RT | Highlight / tooltips | 514 |
| LT + R3 | Replace energy cell | 458 |
| LT + Menu | Help | 385 |
| LT + D-pad up / down | Previous / next ability page | 647 / 651 |
| LT + D-pad left / right | Zoom in / out | 799 / 802 |

The older source supplies the direction semantics: `GameManager.cs` around 2626
dispatches neutral RT as wait, a directional press as movement, and held repeat as
automatic movement. A and LT+A can include the indicated direction; right-stick
input enters look. `ControlManager.cs:2114` resolves diagonal directions before
cardinals using an axis threshold. `XRL.UI/CommandBindingManager.cs:1135` reads the
XML gamepad bindings and `:1699` loads the commands data; `:912` and `:1740` identify
LT as the default alternate modifier. These are version-qualified behavior checks,
not measured current-device timings. Repeat settings can override source constants;
no exact current Qud repeat interval is asserted here.

## Installed menu and modal bindings

These are **Qud context-dependent defaults**, not a promise that every Caves of Ooo
screen implements every binding. Multiple assignments to a button belong to
different active layers.

| Context / control | Qud action | XML line |
| --- | --- | --- |
| Menu A / B | Accept / cancel | 243 / 247 |
| Menu left stick / D-pad | Navigate | 261–282 |
| Menu right stick | Details navigation | 289–307 |
| Menu LT + left stick or D-pad up/down | Page up/down | 316–326 |
| Menu LB / RB | Previous / next page | 339 / 333 |
| Menu LT + D-pad left/right | Previous / next category | 349 / 344 |
| Menu LT + Y / LT + X | Insert / delete | 356 / 362 |
| Menu X / Y | Increase / decrease value | 368 / 373 |
| Menu RT | Options, or take all in its applicable layer | 390 / 405 |
| Menu View | Toggle | 395 |
| Inventory RB | Store items | 410 |
| Conversation Menu | Start trade | 511 |
| Targeting Y / LB / R3 | Missile weapon menu / self / target lock | 566 / 571 / 578 |
| Attributes Y / X | Buy mutation / show status effects | 721 / 725 |
| Trade RB / LB | Add / remove trade selection | 733–743 |
| Trade RT or A / Menu / LT+A | All items / offer / vendor actions | 748–758 |
| Character creation X / Y / D-pad left / LT+A | Random / reset / options / mutation variant | 780–792 |

## Public corroboration and rejected assumptions

- [Freehold, July 12 2024, 207.78](https://freeholdgames.itch.io/cavesofqud/devlog/764265/feature-friday-july-12-2024)
  confirms Y edge travel, L3 points of interest, LB nearest attack, LT+D-pad ability
  pages and zoom.
- [Freehold, August 2 2024, 207.87](https://freeholdgames.itch.io/cavesofqud/devlog/775784/feature-friday-august-2-2024)
  confirms LT+RT highlight also reaches inventory item tooltips.
- [Freehold, August 16 2024, 207.93](https://freeholdgames.itch.io/cavesofqud/devlog/783104/feature-friday-august-16-2024)
  confirms right-stick scrolling in choice dialogs.
- [Freehold, unified-input beta, June 2023](https://freeholdgames.itch.io/cavesofqud/devlog/552024/gamepad-support-new-abilities-screen-beta-is-now-live)
  documents unified input and danger-sensitive held movement.
- [Freehold, Deep Jungle release, 2022](https://freeholdgames.itch.io/cavesofqud/devlog/348340/the-deep-jungle-feature-arc-is-here)
  documents alternate-modifier + LB force attack.
- [Wiki author's July 2025 post](https://www.reddit.com/r/cavesofqud/comments/1m0q7f5/steam_deck_controls_now_on_wiki/)
  identifies a newer consolidated wiki table. Access to the actual table was
  unavailable, so no row above relies on an unseen screenshot.
- [Hangedman's first-hand guide](https://www.hangedmandesign.com/into-the-caves-of-qud/)
  is useful context but conflicts with installed defaults for B and Y/LT+Y.
  The author's [feedback post](https://steamcommunity.com/app/333640/discussions/1/4846526727960355264/)
  discusses customized autoexplore bindings. It is not an untouched-default oracle.

| Initial assumption | Verified correction |
| --- | --- |
| B is one-turn wait | Installed B requests recovery; neutral RT is the supplemental-source one-turn wait. |
| Y autoexplores | Default Y walks to an edge; LT+Y autoexplores. |
| Menu pauses | Menu opens character; View opens system/pause. |
| Right stick selects abilities | Right stick looks; D-pad left/right selects abilities. |
| LT + left zooms out | Installed LT + left zooms in; right zooms out. |
| Old global key aliases can represent the layout | Direction indication must be separate from paid movement and modal navigation. |

## Caves of Ooo adaptation and verification plan

The adaptation follows the verified physical layout while preserving this game's
existing action rules. B opens nearby bed/campfire recovery when injured; ordinary
waiting does not heal and full health is a no-op. LT+B offers one, ten or one hundred
turns of waiting with interruption. Menu opens a character submenu; View pauses.
RB, R3 and LT+R3 report unavailable missile/reload/cell systems. The hotbar has one
ten-slot page; LT+X reaches all abilities, where Y opens a slot-assignment chooser.
This menu-specific Y action is a CoO adaptation. LT+RB selects a carried throwable.
Travel remains bounded to the current zone, without automatic looting or attacks.
LT+RT supplies visible-world labels; menu LT+RT reaches existing item details.

The player-facing help and package guide now follow actual CoO dispatch, retaining
keyboard help and historical graphics/performance evidence. The focused
`QudControllerHelpTests` regression covers misleading old movement/chord guidance,
recovery/wait distinctions, unavailable systems, reachable character/pause/help paths
and connected/disconnected boot guidance. Native RED job
`3a283dc480d64b9fbabf1cfa6c632998` observed four intended failures and one passing
counter-check before changing `ControlsReference`. Native GREEN job
`03862ce6b8e64a5aab1d63bd7329eaba` passed **344/344**, including this fixture and
the hotbar fixture below; see
[native-editmode-green.json](Verification/SteamDeckQudControls/native-editmode-green.json).
Final integration receipts remain owned by the coordinating task.
Text/source inspection alone cannot verify controller feel or physical Deck behavior.

Self-review: 🟡 corrected old/custom mappings against the installed default table;
⚪ documented unsupported game systems and modal differences; 🧪 physical Deck feel
and exact current Qud runtime behavior remain outside the evidence above.

### Follow-up: persistent hotbar hint

The verification sweep found `HotbarStateBuilder` deliberately caches an immutable
gameplay snapshot whose hint still names keyboard cycle/cast keys. Device connection
is presentation state, so the bounded fix belongs in `GameplayHotbarRenderer`:
choose D-pad/X guidance for a connected gamepad, A/RT/B guidance when the snapshot
has a pending ability, and preserve the supplied keyboard hint otherwise. Resolve
the displayed hint before the retained-header comparison so connecting/disconnecting
repaints only the header even when the snapshot instance is reused. Do not change
the builder, snapshot semantics, slot hotkeys, gameplay dispatch or HUD caches.

RED-first fixture `QudControllerHotbarHintTests` uses real tilemaps, actual device
addition/removal and real builder snapshots. It checks rendered glyphs and retained
hint, keyboard/pending counter-cases, connection invalidation without changing the
snapshot, untouched background, and no redundant repaint on an equal second frame.
Native RED job `c6db50219f004313bd29e74c7c097070` observed the expected three
controller failures and two passing keyboard counter-cases. The minimum renderer
change now resolves the device-specific hint before header invalidation; the
344/344 native GREEN above includes all five cases. The in-game reader also documents the abilities-screen
Y slot chooser, matching the root-owned binding implementation.

### Follow-up: generic choice popup presentation

The native visual pass exposed character/wait choices displayed with the player's
world-cell description, hitpoints and ground status. `WorldActionMenuUI.Open` will
accept a trailing optional `title` argument; nonblank titles identify generic
choices, suppressing world status and pile-summary presentation. Existing calls
retain their world-object title/status behavior. Root owns wiring its controller
menu title through this API. Only display changes are in scope: controller footer
uses A/B and advertises LT+RT details only for actual world-action menus; keyboard
world hints remain intact. Real tilemap tests precede production, covering device
counter-cases and returning from a custom choice menu to a world menu.
Native RED job `a921f2e833034432953a7e4ec85c2e1e` observed six intended failures
and one passing keyboard counter-case. The optional title and device-aware footer
are implemented without changing input handling. Final native job `ffbf73765b284474b9bfcd8ea01a80f4` passed all 407 cases, including these seven popup cases; native Play run `e5dde5d513c84863a527c54330459975` passed 39 checks and its wait-menu capture was visually reviewed.
