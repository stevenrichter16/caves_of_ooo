# Capture a useful graphics-review package for Caves of Ooo

## Your assignment

Capture current in-game screenshots of **Caves of Ooo**, the persistent Unity/C# RPG in [stevenrichter16/caves_of_ooo](https://github.com/stevenrichter16/caves_of_ooo). Give each screenshot enough context that another agent can judge the graphics accurately without guessing what the scene contains, how it was produced, or whether it represents ordinary play.

The purpose is to review the game's visual quality as a whole: regional identity, scenery, creatures, items, equipment, lighting, effects, camera, interface, and the clarity of gameplay information. This is an evidence-gathering assignment. **Do not redesign the graphics, fix defects, modify production code or content, or implement recommendations.**

Use plain English in your captions. Explain what the reviewer is looking at and why it matters. A filename such as `test-17.png` and a caption such as “Sodden, good readability” are not sufficient.

Choose representative situations autonomously. Ask only when access, authorization, or protection of an existing player/editor session genuinely blocks the work.

## Background and the questions this package must answer

A previous read-only graphics audit inspected main at `5bb8e249d1fc69d9213f684bace7a8628e11b4ad` and 27 retained images. Many world images were from September 2026, while newer UI images were from October. Those images used different cameras, lighting, resolutions, and capture methods. They were useful, but they were not one fresh, comparable sample of the game.

The audit raised these questions. Treat them as things to investigate, not conclusions your screenshots must prove:

1. Can the player, threats, useful objects, and hazards be recognized quickly against the scenery? Green poison effects over green, textured ground were one concern.
2. Do regions feel like different places in one game, or like different generations of art? Regional differences are desirable. In particular, the Overwrit's deliberate emptiness must not be treated as missing decoration.
3. Can important settlements be recognized from their layout and working spaces before reading their names? Drowned Ledger is an especially useful case because the player previously had trouble identifying that destination.
4. Do characters differ through recognizable shapes, proportions, poses, and equipment, or mainly through small color changes?
5. Do world objects communicate their actual state: open, closed, broken, repaired, ripe, harvested, wet, dangerous, or usable?
6. Do inventory, skills, dialogue, examination, the journal, map, and targeting feel related and remain comfortable to read?
7. Does the interface show the correct controls for the input method being used?

The old audit also found a source-level journal font issue: the current-stage `>` marker and `/` punctuation used a narrow font atlas that did not paint those characters. Recheck the current source and capture the relevant journal rows if reachable. Do not assume the defect still exists, and do not repair it during this task.

At that checkpoint the game already used native 3D by default, with 2D/glyph fallback paths. Eligible ordinary caves also had native 3D support. Do not repeat older claims that the whole game is ASCII-first or that all ordinary caves are 2D. Verify the current implementation and settings.

## 1. Establish exactly what you are capturing

Read the repository instructions, including applicable `AGENTS.md` files, `CLAUDE.md`, and `Docs/PROJECT-IDENTITY.md`. Follow the project's session-safety and source-verification rules. This assignment does not authorize the implementation steps described in its development methodology.

Record:

- The current main commit, the checked-out commit and branch, and whether they differ.
- Whether the worktree is clean. If it is dirty, identify which existing changes could affect the captured appearance. Do not discard or modify them.
- The Unity version, platform, actual game scene, and whether the capture comes from an Editor Game view or a standalone player.
- How you established that the running scene/assembly matches the source being cited. If you cannot verify that, label it explicitly.
- The baseline graphics settings, camera/zoom, UI scale, output resolution, and input method.
- The isolated save/world used for the review: seed, player build, relevant equipment, and whether it is a new game or a disposable copy.

Do not silently pull, change branches, rebuild assets, update packages, or reimport the project to make it match main. If the available build differs, describe the difference and capture that build honestly, or request the narrow authorization needed to obtain the intended version.

Create a new output directory outside production content, for example `GraphicsReviewCapture-<timestamp>`. Keep earlier screenshots and reports intact. Do not commit or push.

## 2. Protect the player's session

Inspect the current editor/player state before doing anything that could interrupt it. Do not stop, restart, enter, or reset an active user session without approval. Do not move the user's character, overwrite their saves, change their inventory, or stage enemies in their world.

Use an existing supported isolated capture workflow, a disposable save, or a separate review session. Verify how autosaves, last-game selection, display preferences, and cleanup are isolated. At the end, restore settings you changed and record the result. If safe isolation is unavailable, pause the runtime portion and report the specific blocker.

Use normal game inputs for the primary screenshots. Existing read-only inspection tools may provide metadata. Do not fire gameplay events through inspection tools. Do not add a new capture harness or production code merely to finish the package.

If an existing controlled fixture or preview tool is necessary, use it only in the isolated review environment, record its changes, and label those images as staged. A preview may supplement an ordinary gameplay image; it must not replace it without explanation.

## 3. Use consistent capture conditions

### Primary set

Use **1280×800** for the main comparison set, where supported. Use the normal gameplay camera and a consistent zoom. Preserve each region's real lighting, palette, fog, and material treatment. Consistent capture settings do not mean forcing every region to have identical sunlight or color.

Capture the complete game frame, including the HUD. Keep lossless PNG originals at native resolution. Do not stretch, sharpen, recolor, enhance, or upscale them. Do not use AI-generated images or Blender concept renders as evidence of current gameplay.

For reading screens and dense UI, add selected **1280×720** checks. These should include dialogue, examination, the journal, and a populated hotbar/targeting view. If a resolution is unsupported, record the actual supported size rather than relabeling a resized image.

Record whether CRT, world-resolution scaling, low detail, shadows, and spell effects are enabled. A 1280×800 window with a reduced-resolution world texture is a different condition from a full-resolution world at the same window size.

### Extra views

A close-up or overview can explain an object or layout, but retain the ordinary gameplay view beside it. Name the extra view accordingly. Crops and annotations must be separate derived files; preserve the full original. Any contact sheet is a navigation aid, not a replacement for readable full-size images.

Never remove fog or reveal hidden entities to improve a primary image. If a full-reveal preview is useful for composition, put it in the staged supplement and explicitly say that it cannot demonstrate ordinary visibility or discovery.

## 4. Required screenshots

Aim for roughly **30–40 useful primary images**, with a small number of matched variants where necessary. One image may satisfy several requirements. Do not create repetitive pictures merely to reach a number. If a case is unavailable, list it as missing and explain why rather than manufacturing evidence.

Before the regional set, capture the available title/start screen and graphics/settings screen if they can be opened safely. These establish the presentation baseline; they do not replace the settings manifest. Include one before/after surface-to-cave or biome-boundary pair when the ordinary route already supplies it, so the reviewer can judge the transition between visual treatments.

### A. Six surface regions

Capture one ordinary gameplay view in each current canonical surface biome:

- **Spread:** worked fields, hedges, roads, or river country. Include the player, vegetation, and at least one real useful object or creature.
- **Sodden:** wet ground, reeds or dead trees, and a visible dry route or crossing.
- **Beating:** exposed terrain with ridges, salt, a route, or another actual regional formation.
- **Grovelands:** clustered growth and open circulation. Include a creature or inhabitant if naturally present.
- **Overwrit:** its intended sparse terrain and a meaningful boundary, trace, or figure where present. Do not populate it for the screenshot.
- **Stump:** fossil-like stone, grain, ledges, or pools, with a readable route and scale reference.

Select typical chunks, not only the most attractive showcase scenes. Record the actual formation and why you selected it. For the Spread and Sodden, add a second contrasting situation if the first image cannot show both scenery and ordinary gameplay clutter.

### B. Settlements and arrival recognition

Capture:

1. **Drowned Ledger on arrival**, through the player's actual visibility. The frame should help a reviewer ask whether the place can be recognized as an excavation and recording site.
2. **A Drowned Ledger working area**, showing the real relationship between people, boards, equipment, excavation, or records. Choose what actually exists; do not rearrange it.
3. **A contrasting settlement**, such as Quillhold or Cinderhold if present and reachable in the current build. Show its entrance, public route, and characteristic activity or architecture.

Verify place identities and coordinates against current content. Historical coordinates are leads, not permission to assume a location is unchanged. If you use a travel shortcut to reach a place, record it. The resulting frame can establish appearance but cannot prove discoverability or an unaided journey.

### C. Underground spaces

Capture an eligible **ordinary cave** and a **lair or inhabited underground location**. Include the player, a route, and the real source of light where possible.

Record whether each uses native 3D, sprites, glyphs, or a mixture. Explain how that was verified. Do not infer the renderer from the zone name alone. If a lamp or luminous item is relevant, distinguish light that comes from the player from light that belongs to the environment.

### D. Characters, creatures, and equipment

Capture a normal-scale encounter with at least two substantially different creature shapes, and a settlement view with distinguishable human roles where available. A separate existing actor gallery is useful as a supplement, clearly marked as a gallery.

For one representative item, capture the same item:

- On the ground at ordinary camera scale.
- In inventory or examination.
- Equipped on its actual owner, if that item has a visible equipped representation.

Explain any missing equipped representation rather than selecting a different item to hide it. Use the same identifiable item throughout the sequence. This tests whether its shape and material remain understandable across contexts.

### E. Combat, hazards, and targeting

Capture:

- A readable melee situation with player and enemy in the same frame.
- A spell impact or persistent spell result. State exactly which moment is pictured.
- A throw preview showing the intended target and route/contact/landing information that the current game actually provides. If available, capture body versus ground targeting as a matched pair.
- A harmful gas or liquid against busy green terrain, preferably the poison case raised by the earlier audit.
- A comparable hazard on a different background, if naturally available or safely reproducible in an existing isolated fixture.

For hazard comparisons, record effect identity, amount/intensity if available, lighting, effect quality, and time since creation. Do not compare unrelated effects at different stages and attribute every difference to background contrast.

Explain what is a creature, a ground item, a projectile, a hazard, and decoration. State which cells are actually affected when that can be established from legitimate inspection. A particle's visible extent is not automatically its mechanical footprint.

For low objects such as a dropped dagger or fragile vessel, show their true low profile. Do not introduce artificial height or collision to make them easier to see. If the proposed fabrication system is not implemented, do not stage fabricated tonics and present them as existing gameplay.

### F. Meaningful state changes

Capture two useful before/after pairs through ordinary supported interactions in the disposable session:

- A door closed and open.
- A ripe crop and its harvested state, or a broken and repaired world object.

Keep camera, resolution, and lighting as similar as practical. Record the action between frames. If repairing a bench does not itself supply a worker or restore an entire service, say so. The caption must not promise more functionality than the object actually has.

### G. Interface coverage

Capture these real screens with useful, representative content:

- Ordinary HUD and a busier combat HUD. Reuse earlier frames if appropriate.
- Inventory and equipment, including readable item names and totals.
- Examination of an item and a creature. Include an expanded section or additional page if the current interface supports it.
- Skills with a selected skill, requirements, and a reasonably long description.
- Dialogue with meaningful speech and choices. Include a longer or paged conversation.
- Journal with a current stage and, where available, objective progress such as `3/5`. Also capture travel notes if they use another tab or presentation.
- World map with the player position, selected/look position if applicable, and a known destination. Record which position an enter-site action would use.

Use a populated hotbar containing more than the six familiar starter spells if a suitable existing review character is available. This can expose repeated fallback icons and long-name wrapping. Do not create an unsupported build solely to populate the bar.

Add representative trade/container, pause/settings, starting-choice, and death/recovery screens when readily available. A death screen may be captured through an existing disposable fixture or recorded capture route; never kill the user's real character for evidence. Mark any unsampled menu explicitly.

For control guidance, capture at least one menu in keyboard/mouse use and the same menu after controller input, if supported. Record physical versus virtual controller and whether the game reports connection or last-used input. Do not claim Steam Deck hardware testing from a desktop capture at Deck resolution.

## 5. Context required for every image

Create one manifest entry and one human-readable caption per original image. Shared settings may live in a named session record, but every image must reference that record and list any overrides.

Record these fields:

- **Image ID and filename:** stable and descriptive.
- **Question:** what the image is intended to help assess.
- **Session/source:** capture timestamp, build/commit record, Editor or player, platform.
- **Location:** biome, named place, formation, zone ID, depth, logical world coordinates, and player tile where available. Label any raw/display coordinates separately.
- **Viewing conditions:** output dimensions, world-resolution scale, camera/zoom, UI scale, quality settings, time of day, local light, and visibility state.
- **Input:** keyboard/mouse, physical controller, virtual controller, or scripted input.
- **Scene contents:** relevant creatures, items, equipment, terrain, hazards, and actual states. Include stable technical identities in metadata where available; do not cover the ordinary screenshot with debug labels.
- **Capture method:** ordinary input journey, controlled input route, staged fixture, or full-reveal preview. List travel shortcuts, spawned objects, frozen time, invulnerability, changed resources, or other alterations. Say “none” only when checked.
- **Immediate sequence:** what happened just before the frame, whether the game is waiting for input, and what action is available next.
- **Related images:** before/after, alternative resolution, alternate input, or close-up IDs.
- **Limits:** what this image cannot establish and what remains uncertain.

Use `unknown` or `not applicable` when appropriate. Do not guess missing settings or copy metadata from a previous frame after the conditions changed.

### Example caption style

> **SOD-02 — Dry crossing through the mire**
>
> This is an ordinary gameplay view of the Sodden, with the player beside a plank crossing. The pale forked shapes are dead trees; the darker patches are water. The review question is whether the dry crossing and player remain easy to find without opening Examine.
>
> Captured in the isolated review save at the normal camera, 1280×800, with native 3D and shadows enabled. The exact zone, seed, time, and settings are in the linked manifest entry. The player reached this cell through normal movement after a recorded world-map transfer. Fog is unchanged and nothing was spawned or rearranged.
>
> This frame shows the crossing's appearance under these conditions. It does not prove all mire layouts are navigable, that the water is harmless, or that the player would find this crossing without guidance.

This is an illustrative caption, not evidence of an actual capture. Replace every detail with verified facts from the real image.

## 6. Inspect what you actually captured

Open every image before including it. Confirm that it shows the intended scene, is not black or stale, and has the recorded dimensions. If a picture exposes a visual defect, keep it. Do not quietly select a prettier replacement that conceals the problem.

Separate ordinary gameplay images from staged supplements. Distinguish current captures from historical screenshots retained for comparison. Verify timestamps and run identity so an older file cannot be mistaken for the latest output.

A short optional clip can supplement the images if recording is already supported and safe: one movement/melee exchange, one spell, and one overlapping-effect situation. Do not expand this task into building recording tools. If only stills are available, explicitly leave timing, motion comfort, flicker, and frame pacing unassessed.

## 7. Deliverables

Return a self-contained package containing:

1. `README.md`: build identity, capture methods, shared settings, coverage, omissions, and session-restoration result.
2. `screenshots/`: full-resolution original PNGs, with ordinary and staged captures separated.
3. `captions.md`: plain-English context for every image, grouped by review question.
4. `manifest.json`: the structured metadata described above. Include original-file hashes so files and captions can be matched reliably.
5. Optional separate crops, annotated copies, contact sheets, or clips, always linked back to originals.
6. A short findings list: directly visible concerns, source-supported explanations, and questions requiring more evidence. Do not turn this into an implementation plan or claim every suspected cause is proven.

Finish with a concise summary of what was captured, which important gaps remain, and whether the protected session/settings were restored successfully. State clearly that no graphics fixes or production changes were made.

The package is ready when another reviewer can understand every scene, compare images under known conditions, and tell ordinary play apart from controlled demonstrations without asking you to reconstruct the capture session from memory.
