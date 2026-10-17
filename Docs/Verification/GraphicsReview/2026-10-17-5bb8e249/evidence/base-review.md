# BASE image review

Reviewed all four original PNGs through `view_image`, and their corresponding JSON evidence, on 17 October 2026. All are complete, nonblack, distinct 1280×800 PNGs. No image or repository content was changed.

Shared conditions: `manual-64`, source `5bb8e249d1fc69d9213f684bace7a8628e11b4ad`, Unity 6000.3.4f1 macOS Editor Game view, SampleScene, seed64, Spread / Hedgerow, `Overworld.11.10.0`, logical world (11,10), depth0, player tile (40,12), tick10, Dawn. Native3D and voxel rendering are active. Full detail,100% world resolution, shadows on, full spell effects, CRT off; gameplay zoom multiplier1.2. Actual world render target940×680, sidebar340×800, hotbar940×120. The matching evidence JSON records cameras, inventory and identities. No controller is connected; controls were keyboard/mouse through CUA.

**Visibility qualification:** All four background views retain SampleScene's inherited `RevealEntire3DZone=1`, confirmed in per-image metadata and source. These are legitimate startup/menu captures but must not be used to demonstrate normal fog, visibility or discovery. The operator subsequently disabled this flag in the disposable runtime for actual-FOV world captures; no source or scene was saved. If folder separation is required, classify these as baseline-menu/full-reveal-background supplements, not ordinary gameplay world evidence.

## BASE-01 — The actual start/continue presentation

File: `screenshots/ordinary/BASE-01-start.png`. Captured2026-10-17T12:56:42.9314970Z; before choosing a starting build.

The start screen consists of the rendered Spread background and ordinary HUD, with New Game and Continue instructions in the right-hand log. There is no separate logo or title panel in this frame. The ten skill boxes are empty because the starting build has not yet been selected. The player is the small bright green figure near the center; crop rows, a well, notice boards, low walls, pale reeds and yellow-brown ground objects surround it.

This image tests how clearly the initial choice is presented. The next available action is C to continue the private marker save or N to start a new game. Both keyboard and controller instructions are visible even though metadata reports no controller. The private marker explains “Save detected”; this is not the user's existing campaign. The inherited full-reveal background prevents conclusions about ordinary player sight.

## BASE-02 — Starting-build comparison with The Duelist selected

File: `screenshots/ordinary/BASE-02-build-choice.png`. Captured2026-10-17T12:57:04.2226840Z; after opening New Game, before confirming a build.

The black, white-bordered selector overlays the world and leaves the right HUD visible. Seven choices appear on the left, including The Hurler and Classic. The Duelist's description on the right lists HP, DV, speed, attributes, initial gear, skills, weakness and a suggested first experiment. Yellow names, cyan stats and red weakness text establish distinct reading levels without removing the pixel-font style.

The next action is to browse with Up/Down, jump with1–7, and press Enter to begin. The complete description fits this1280×800 frame. The control hint sits below the opaque panel on the detailed green terrain; its dark gray text is substantially harder to read than the content inside the panel. The empty hotbar remains the pre-build state, not a failed loadout. The highlighted Duelist is a preview; Classic is selected later in BASE-03/04.

## BASE-03 — Pause menu over the selected Classic loadout

File: `screenshots/ordinary/BASE-03-pause.png`. Captured2026-10-17T12:57:27.2035750Z; after selecting Classic and opening Pause.

The compact central menu lists Save game, Load game, Controls, Graphics, Audio and Quit, with Save selected. Six distinct starter spell icons now fill the first six hotbar boxes; Ember Spit is highlighted. The log confirms that a dagger was equipped, Classic was selected and an initial checkpoint was saved inside the isolated save root. Evidence identifies the actual equipped Dagger as entity2704.

The question is whether the small popup, persistent HUD and populated hotbar work together at handheld resolution. All menu rows and all ten hotbar boxes fit. The selected item and active spell are easy to locate because of bright text and the yellow hotbar frame. The footer `[Enter]select [Tab]close` is outside the opaque panel and has weak contrast over vegetation. This still does not demonstrate controller input or game performance.

## BASE-04 — Current graphics settings, without applying a preset

File: `screenshots/ordinary/BASE-04-graphics.png`. Captured2026-10-17T12:57:48.0406680Z; after navigating from Pause to Graphics.

The Graphics menu shows World resolution100%, World shadows On, World detail Full and Spell effects Full, agreeing with the runtime evidence. “Handheld preset” is only the highlighted action; it has not been applied. The real native3D world remains underneath, with the same six-spell hotbar and sidebar.

This view establishes the quality settings used for comparison, not performance on a handheld. Enter would apply/change the selected item; Esc would return. The settings labels are readable and untruncated, but their dark gray control footer again blends into the detailed terrain outside the black panel. CRT is not shown in this menu; the runtime capture separately establishes that it is off.

## Directly visible findings and supported explanations

1. **Low-contrast menu instructions:** BASE-02,03 and04 place footer hints on the world outside the black panel. The hints remain present but are hard to distinguish against green vegetation at native1280×800. This is a visible concern, not a claim of input failure.
2. **Mixed input guidance at startup:** BASE-01 shows both keyboard and controller start instructions with no controller connected. `Assets/Scripts/Presentation/UI/BootMenuController.cs:54–55` logs both strings unconditionally; the capture does not imply a connected Deck.
3. **Initial choice lives in the log:** BASE-01 presents New Game/Continue as ordinary log text, among movement, map and journal help, rather than a dedicated title menu. Whether this is sufficiently prominent is a design-review question.
4. **Inherited full reveal:** all four JSON records have `renderer.fullReveal=true`, consistent with SampleScene's serialized value. Retain this evidence and qualification; do not infer ordinary discoverability from the attractive complete background.
5. **Readable selected content, subdued supporting hints:** bright menu labels and the highlighted spell remain clear; the supporting footers are the contrast weak point. The six starting icons are distinct in these frames, but this sample cannot assess fallback/repeated icons in a more heavily populated build.
6. **Map wording needs semantic context:** the log says “enter selected destination.” Source `WorldMapTraversal.Descend` resolves the player's occupied world-map cell, not a detached Look cursor. A separate actual map screenshot is needed to assess whether that wording causes visible ambiguity.

Timing, motion comfort, flicker, physical Steam Deck ergonomics and frame pacing are unassessed by these stills.
