# Caves of Ooo — fresh graphics-review capture

Captured 17 October 2026. **52 reviewed, lossless originals: 33 primary images and 19 supplements.** This package gathers evidence only. No graphics fixes, production edits, asset rebuilds, reimports, package updates, commits or pushes were performed.

Start with the [visual index](index.html), then use the [captions](captions.md) and [manifest](manifest.json). Open an original PNG to judge text or small objects; the index's browser thumbnails are navigation aids. [Findings](findings.md) separates visible defects, source-supported explanations and unanswered questions.

## Source and runtime identity

- Repository: `stevenrichter16/caves_of_ooo`, local `/Users/steven/caves-of-ooo`.
- Branch, checked-out commit and remotely checked `origin/main` at both start and finish: **`5bb8e249d1fc69d9213f684bace7a8628e11b4ad`**. No pull or branch change was made.
- Tracked worktree was clean before and after. The whole worktree was **not** clean: thousands of pre-existing untracked verification reports/screenshots were present. They were left intact. Existing native capture workflows wrote their normal new run outputs; selected fresh outputs and reports were copied into this package.
- Unity **6000.3.4f1**, macOS Editor, Metal, Game view. Main manual scene: `Assets/Scenes/Main/SampleScene.unity`. Several existing controlled routes use `Assets/Scenes/Main/ReferenceGlade.unity`; each image identifies its actual scene/session.
- Loaded game assembly MVID: `fc7426b5-ff3a-4954-95cc-f4287406ca45`. Its SHA-256, modification time, current Measured Throw field reflection and check that no tracked C# source was newer are in [assembly identity](evidence/assembly-identity.json). Unity was compile-idle. This establishes a strong correspondence to the clean source without forcing a recompile; it is not a reproducible-build certificate.
- Instructions read: applicable repository guidance, `CLAUDE.md`, `Docs/PROJECT-IDENTITY.md`, the Unity skill and the [capture assignment](source-prompt.md).

## Capture methods and comparison rules

**Ordinary journeys:** two fresh, private seed64 sessions used normal keyboard/mouse controls through CUA. The first selected Classic through the real starting-choice UI; the second used the existing audit's original starter kit with the chooser suppressed. They are separate actors/worlds, not a continued save. All six regional screenshots and the Drowned Ledger journey came from the first session. The second accepted Padok's real quest and recorded a real scribe report for journal coverage. Destinations were selected using source knowledge, so this does not demonstrate unaided discovery. No stats, items, currency, teleports, AI suppression, invulnerability or clock freezing were introduced into these manual worlds.

**Visibility exception:** SampleScene serializes `RevealEntire3DZone=1`. BASE-01 through BASE-04 preserve that startup background and are staged supplements, not ordinary discovery evidence. After these menus, the flag was set **false at runtime only** for the manual regional and journal captures. The scene file was not changed. Source-default `FovRadius=999` remained unchanged, so visible open terrain uses broad line of sight. No hidden entities were revealed to improve a primary image. Full-screen menus can disable the world presenter; their `nativeRing=false` metadata does not mean the underlying zone lacks 3D.

**Existing controlled routes:** combat, caves, field work and lair captures use previously implemented input routes in isolated sessions. Actual combat, harvesting, repairs and door interactions use normal game inputs. Some routes transfer the actor to a real generated destination; each caption lists the shortcut. The unchanged world after a shortcut can show appearance and interaction, but cannot prove natural discovery or the journey to it.

**Staged supplements:** the throwing fixture grants supplies/skills/stats and places an AI-free durable target in a cleared lane. The skills route stages10 SP, then buys ranks through the UI. Their results do not represent an earned player build. The shop is real generated Sella stock, but its route inherits SampleScene full reveal and uses a travel shortcut, so it is conservatively staged. Temporary controllers are virtual Input System devices on the Mac.

Primary originals are **1280×800**. Selected examination, short and long dialogue, journal and HUD originals are **1280×720**. Dimensions are read from PNG headers; images were not resized or retouched. Screenshots use the existing end-of-frame capture helper. Manual metadata is read in the same callback. Older route helpers do not emit all per-frame metadata: those unknowns stay explicit.

Manual graphics settings: native3D on, full detail, world scale1, shadows on, spell effects Full, animation speed1, flash0.15, shake0, CRT off. Gameplay zoom multiplier1.2; normal800p world render texture940×680, with separate340px sidebar and120px hotbar. Actual per-image camera rectangles, menu cameras and720p changes are recorded. ReferenceGlade routes use serialized zoom0.82 and lack an independent numeric camera snapshot. **Do not compare actor sizes across those scenes as if the camera matched.** No custom zoom or close-up was used to make small objects easier to see.

Real time of day, fog, materials and regional lighting were preserved. The manual trip advances from Dawn into later bands; Cinderhold/Stump include dark conditions. Compare the recorded lighting before attributing every difference to art. Overwrit's sparse terrain is intentional and was not decorated for review.

## Coverage

| Area | Selected evidence | Scope |
|---|---|---|
| Six biomes | SPR, SOD, GRO, STU, OVE, BEA | Normal journeys; Spread and Sodden have contrasting samples. |
| Drowned Ledger and another settlement | SOD-01..03, SET-01 | Real arrival, working spaces, reading tent, Cinderhold. |
| Cave and inhabited lair | CAV-01/02, LAI-01/02 | Generated cave with harvested light source; surface/downstairs transition. Transfers disclosed. |
| Same item across contexts | ITEM-01..06 | Exact first-session Dagger2704, inventory/examine800/720, dropped and re-equipped. Tiny equipped blade remains visually unclear. |
| Melee and throwing | CMB-01/02, THR-01..04 | Actual melee situation/outcome; staged body/ground and prepared keyboard/controller throw views. |
| Meaningful states | WRK-03/04, CAV-01/02 | Same gate closed/open; ripe crop source to actual harvested ground item. Crop pair differs in overlay/cell and is not a clean matched lighting comparison. |
| Readers and dialogue | ITEM, NPC-01, UI-01..05 | Item and real creature examination, quest offer, short topic and settled long wilderness reports at800/720. |
| Journal and map | JRN-01..03, MAP-01 | Accepted current quest,0/3 progress, actual travel note, separate map player/Look positions. |
| Skills, trade, menus | SKL, TRD, BASE | Rank/requirements/long description, real trade stock, start/build/pause/settings. |
| Dense720p controls | HUD-01..03 | Six starter powers, pending direction, paid-cast aftermath and ability reader. Virtual controller only. |

## Gaps and partial evidence

- **Harmful gas/liquid on busy green terrain and a matched different background:** unavailable. The existing liquid route stopped at inventory examination before oil/acid. Water is not offered as a poison substitute. Effect intensity, age and affected-cell comparison are therefore unassessed.
- **Visible spell impact/persistent footprint and attack animation:** not captured successfully. Ground Surge's screenshot is after effects settle, showing cooldown/log consequences. A failed combat pose check is preserved. Still aftermath cannot establish motion or impact quality.
- **Two clearly different creature silhouettes in one ordinary encounter:** not adequately sampled. Some frames contain multiple species, but identification relies partly on metadata; this is not a clean shape comparison. Human role distinctions are also only partially legible at normal scale.
- **Expanded/multipage creature examination, container, player death/recovery, naturally earned hotbar beyond six diverse powers:** unsampled. The combat file once named `player-defeat-outcome` shows an enemy killed and player alive32/40, not a player death screen. Long single-page dialogue is covered; multi-page dialogue is not claimed.
- **Physical Steam Deck, sound, motion comfort, flicker, frame pacing and performance:** unassessed. Desktop captures at Deck resolution and virtual controller input are not hardware tests. No recording harness was created.

Existing route outcomes are evidence, not a claim that every test passed: cave5/5; throwing40/40; HUD19 checks with no failures. Combat failed its attack-pose capture; work stopped at its later safe-approach precondition; the biome route later failed a movement precondition; the liquid route stopped before harmful-liquid coverage. Skills has one stale13-root coverage check against14 authored roots and also preserves a separately visible blank-rail frame. Raw reports remain under `evidence/routes/`.

## Restoration and integrity

The Editor was originally idle, with no active player session. Each review world used the existing `NativeSaveIsolation` ownership token and private save root; autosaves stayed there. The supported cleanup removed only its own disposable root and restored last-game selection.

[Final restoration receipt](evidence/restoration.json): Editor idle, SampleScene clean, isolation inactive with exit code0, save override null, original last-game ID restored, audit seed0/build-choicefalse restored, original Game view index0 restored, capture-owned view sizes removed, original graphics settings retained, only Keyboard/Mouse/Pen devices present. The temporary runtime full-reveal override ended with Play shutdown. Tracked source remains clean and matches main. The user's actual save was never loaded or moved for these captures.

Every selected original has a unique ID, caption, session, PNG dimensions and SHA-256 in `manifest.json`. Every selected image was opened and inspected. Excluded early-map, typewriter-transition and preflight images remain in evidence only; raw route folders also contain unselected outputs. They are **not** extra reviewed originals. The package's52-image inventory is the `screenshots/` tree and manifest, not every PNG under evidence.

The [coverage review](evidence/final-coverage-review.md), individual visual reviews and [operator log](operator-log.md) preserve provenance. [Package validation](evidence/package-validation.json) records final file/hash/reference checks.
