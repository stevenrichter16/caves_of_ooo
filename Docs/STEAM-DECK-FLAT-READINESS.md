# Steam Deck readiness: current flat presentation

Status: bounded Editor qualification passed; updated packages pending, 2026-11-02. This records engineering readiness; it is not Valve certification or a physical-device pass.

## Requirements and acceptance

- Produce a Linux x86-64 player with its executable permission, complete dependencies/data and verified archive. Keep the explicit Vulkan backend; automatic OpenGL previously failed the world-rendering gate.
- Support a 1280×800 display with the complete HUD, menus, text, targets and interaction cues visible. Compare the ordinary 1× native view with the accessible 2× zoom. World zoom must preserve the HUD rectangle and cost no game turn.
- Use native gamepad output for eight-direction aiming, paid movement/wait, contextual use, inventory, abilities/target cancellation, pause/settings, travel cancellation and save/load. Confirm held-input quarantine so closing a menu cannot silently move the actor. Keyboard coexistence remains supported.
- Actionable instructions must identify the connected controller's controls and remain legible against scenery. Keep keyboard instructions when no controller is connected. Display-only fixes must preserve actual action owners, costs, fog, saved state and legacy presentation.
- Use an isolated ordinary GameBootstrap scene, current flat art and exact runtime viewport assertions. Synthetic Gamepad events exercise production Input System dispatch; they do not establish Steam Input, physical buttons, ergonomics or Linux execution.
- Before claiming device acceptance: run the current package on a real Deck in Gaming Mode with native gamepad output; verify boot/Continue/New Game, controls, inventory/equipment/readers, targeting/attack, travel, save/fresh reload, hotplug, 1280×800 text and 1×/2× readability. Measure sustained frame times, stalls, temperature/power and suspend/resume. Record OS/build/controller configuration and evidence. No hardware pass is inferred from Mac or Editor results.

Valve's public guidance identifies default Deck-compatible resolution, readable text and controller support as requirements: [Deck Verified](https://www.steamdeck.com/en/verified), [developer compatibility](https://partner.steamgames.com/doc/steamhardware/compat), [controller recommendations](https://partner.steamgames.com/doc/steamhardware/recommendations). The default Steam Input configuration should emit a standard gamepad. Keyboard-only mappings exercise a different input path.

## Reproduction and evidence

Run Editor menu `Caves Of Ooo/Scenarios/World/Steam Deck Flat Controller Audit 800p`. The older `Steam Deck Qud Controller Audit` remains the Previous/1080p control. The new route owns isolated saves, presentation override, fixed GameView, temporary devices and preferences, then restores them.

`Docs/Verification/SteamDeckFlat/focused-198.xml`: 198/198 native tests for bindings, menu commands, targeting, camera framing, zoom and Vulkan build settings. These are test receipts, not gameplay.

Initial actual run `Native/2a6420fdd38e49f797741db3ad0c69a9`: 40/40 checks, ten actual 1280×800 screenshots, ordinary seed64 actor. It exposed dim/unbacked Graphics and Abilities footers, keyboard wording in Equipment, and keyboard-only nearby interaction hints. Its boot used keyboard N and it did not verify save/load or zoom; the extended route addresses those limits.

One intermediate attempt `d584236d7bd440ceb8dbd14863954b79` lost nonserialized audit state during an overlapping assembly reload. It was explicitly stopped/restored and is excluded. Do not infer success from its player, missing report or stale assembly; wait for compile/update completion before launch.

## Implemented corrections

- Back Graphics/Pause/Audio and Abilities footer rows with the existing modal background and use readable light gray. Clear the complete old footprint when pages shrink or close.
- Equipment instructions use A for a connected controller and Enter otherwise. Sidebar affordance wording preserves the core opportunity and specifies neutral left stick, because directed A prioritizes the indicated cell.
- Fix the audit's stale Graphics selection: Handheld preset is item 3, not the first item (which toggles presentation). Assert that applying it keeps flat mode at 1280×800.
- Extend the actual route with controller X at boot, LT+D-pad left/right integer zoom, and View-menu Save/Load after a real paid step. Compare restored identity, location, HP, scheduler and checkpoint bytes. Boot-device cleanup is registered before its first yield so interruption also restores it.

## Controls and limits

Left stick aims; RT moves or waits when neutral. A uses the indicated/nearby object. Menu opens character actions; View opens the system menu. LT+X opens abilities; X targets the selected ability; B cancels. Y starts walking, B cancels it. LT+D-pad left zooms in; right zooms out. These are native bindings, not a published Steam Input layout.

The 1× default is intentional (16×24 physical-pixel cells at 800p), with 2× available without resizing the HUD. Physical handheld comfort remains a hardware review gate. No geometry, NPC population, environmental art, shadow rules or gameplay mechanics are changed by this slice. Whole-game graphics acceptance and performance R11 remain open.

## Verified final route

Run `d14fa09d9c52426c981e9aa2f7ee2b2e`: 46/46 checks, 14 actual 1280×800 captures, zero unexpected runtime errors. Controller boot, eight free facing directions, paid RT step/wait, contextual sign menu, inventory quarantine, graphics preset, ability targeting/cancel, highlight, wait menu, walk/cancel, 1×→2×→1× zoom and real Save/Load graph replacement passed. Editor returned idle with null save override. Source review found no remaining P1/P2 after correction.

The visible footer defects produced 4/4 RED failures; the independently found shrinking-ability-menu bug produced its own RED. Final relevant UI/affordance suite: 83/83 GREEN. These suites overlap the initial 198 tests and are not an additive unique-test count. Receipts and before/after screenshots are retained in `Docs/Verification/SteamDeckFlat/`.

Independent final visual review: the reported P2s are resolved; five reviewed captures show no new blocking P1/P2. The 2× view improves player/sign/fixture recognition with intact sidebar, guidance and hotbar. This is bounded 800p acceptance, not a physical comfort or full-game claim.
