# HUD720 capture review

All three included original images were opened with `view_image` and their PNG dimensions verified as **1280×720**. They are fresh, distinct and nonblack. Source originals and the complete route report were copied without modification to `evidence/routes/hud720/291c5327ceaf46aba2f5bb59eec37a3f/`. Selected identical copies live in `screenshots/controlled/`; SHA-256 hashes and per-image file times are in `hud720-entries.json`.

This existing route completed all 19 checks with zero failures or unexpected errors. It uses an isolated seed64 starter in `Assets/Scenes/Main/ReferenceGlade.unity`, with the ordinary six powers and dagger kit. It adds a **virtual gamepad**, makes normal input calls, and leaves NPC scheduling active. It does not grant gear, change stats, teleport, suppress enemies or execute abilities directly. It does make 35 real empty-slot attempts to create inspectable history; that deliberate scripted activity must remain disclosed.

These are separate camera conditions from the primary manual session. The existing route records output and camera viewport dimensions, but no per-image runtime zoom/quality snapshot. ReferenceGlade serializes zoom0.82 and full-reveal false; SampleScene's manual-session zoom1.2 must not be copied onto these frames. The visible HUD says Dawn. Reported hotbar is974×108 and sidebar306×720, both full-resolution overlays. Exact runtime ambient/light values are unknown. This is desktop Editor evidence, not Steam Deck hardware testing.

## HUD-01-controller-aim-720

`screenshots/controlled/HUD-01-controller-aim-720.png`. The original Ember Spit is awaiting a direction. Its first hotbar card turns cyan and says AIM; a controller hint above the bar says LS direction, A/RT confirm and B cancel. This is a virtual controller on the Mac, not a Steam Deck.

Immediate sequence: Cycle the six existing powers with virtual D-pad; Press virtual X to prepare Ember Spit; Capture before stick direction selection; next B cancels without a turn.

Observed: The cyan AIM card and readable A/RT/B hints identify the pending state. The frame has no actual route or target-cell selection yet; it is not a throw preview.

## HUD-02-cast-aftermath-720

`screenshots/controlled/HUD-02-cast-aftermath-720.png`. After one real Ground Surge along a visible empty-ground ray, the fourth hotbar card shows red CD2. The log says the surge crackles across bare ground, separates another actor's melee event, and groups repeated empty-slot attempts. The player remains40/40 on the original cell. The effects have finished by this capture.

Immediate sequence: Make34 real empty-slot key attempts to populate history; Select Ground Surge by virtual D-pad, choose a legal empty ray with the stick and confirm with RT; Wait for effects to finish, make one free empty-slot attempt, then capture.

Observed: Red CD2 is distinct from READY. Grouped log separators distinguish the cast, NPC melee and later free message. The state/result log is visible, but no distinct electrical footprint or impact is visually established by this still.

## HUD-03-ability-reader-720

`screenshots/controlled/HUD-03-ability-reader-720.png`. The Field notes reader describes the original Flaming Hands power: adjacent-cell flame, damage and ground fire, cooldown, Pyromancy requirement and learned/ready status. Controller A Close and B Back prompts fit below the text. This is a real ability-details reader, not the full skill-tree selector.

Immediate sequence: Open the ability manager with virtual LT+X; Press keyboard D to open the selected ability's real details; Capture reader without advancing a turn; Escape closes reader then ability manager.

Observed: All content and footer fit inside the opaque panel. Body lines are densely spaced; the mixed pale text and gold/green headings remain distinguishable. A/B guidance remains displayed after the keyboard D input because a virtualpad stays connected.

## Bounded findings

- **Targeting guidance fits at720p.** The pending Ember Spit card is cyan and marked AIM; LS/A/RT/B instructions fit. This capture precedes selecting a direction. It shows no confirmed target, route or landing information and cannot substitute for the requested throw body/ground comparison.
- **Cooldown and log grouping are visible.** The real Ground Surge card shows CD2 and the log reports the cast and a separately grouped NPC melee event. The source waits for effects to finish before the screenshot (`ReferenceGladeNativePlayer.HudPolish.cs:99–107`), so this is aftermath evidence. It does not show the spell impact, nor is a persistent electrical footprint visually clear. The report's actual ground-mutation claim is source/test evidence; no affected-cell intensity or age snapshot accompanies this frame.
- **Reader content fits, but is dense.** The full Flaming Hands text and A/B footer fit in the opaque Field notes panel, with narrow line spacing. It is the ability reader rather than the full skill-tree selector or level-up screen.
- **Controller hints follow connection here.** The details image follows a keyboard D press, while a virtualpad remains connected; A/B hints stay visible. The route removes the pad only during final cleanup. This does not demonstrate last-used-input switching or a matched keyboard/controller menu pair.
- **Do not overcount populated slots.** There are six original occupied power cards and four empty cards. Ten frames do not equal ten equipped skills.

The route report verifies removal of the virtual gamepad and restoration of the previous pad. Global save/scene/display restoration must still be verified by the operator at the end of the entire assignment. Motion timing, impact readability, flicker and physical control comfort remain unassessed by these stills.
