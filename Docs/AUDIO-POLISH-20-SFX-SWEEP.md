# Utility and environmental action sound sweep

Status: source and recording audit, 2026-10-11. Eight proposed contributions to [the next twenty iterations](AUDIO-POLISH-20.md), not completed iterations. No new clip, catalog entry, gameplay hook or test result is claimed here. The preceding four atmosphere iterations retain their own gates; their source-aware details, work cues and combat focus are not counted again. This sweep made no Assets changes. The separately authorized prior-iteration combat observer was imported after this audit began.

## Verified starting point

The three source manifests contain exactly **22 + 31 + 20 = 73 clips**. Current music remains three complete licensed compositions. The third expansion already supplies leaf/reed rustle, sheltered wood, stone shift, water droplets and actual forge/repair/plant-harvest recordings. Rebranding those textures would not be another improvement.

`Assets/Scripts/Presentation/Rendering/WorldAudioEventRouter.cs` subscribes to movement, typed melee, self-use, generic semantic actions and typed work. It has no dedicated water, cooking, brewing, torch, generic mineral harvesting or throwing callback. `InventorySystem.ExecuteCommand` maps pickup, drop and container transfer to the same leather-handling sound, and equip/unequip to one belt sound. Generic self-use plays the existing cloth cue when the current actor/world is visible. Some menu actions therefore have no dedicated response; they must not gain both a new action cue and an accidental generic duplicate.

Keep the existing sixteen-source pool, bounded local soundscape, visible/current-owner rules, independent volume settings and presentation-only randomness. Retain original recordings and all three historical catalogs. The additions below are **proposed semantic IDs**, not an approved enum or catalog contract.

## Source readiness

These are primary author/asset pages. Existing archive members below were inspected by name in the narrow `/tmp/coo-atmosphere-audio-sources` cache and compared with current source selections. No new download, conversion or subjective listening was performed for this sweep.

| Source | License and useful unused material | Readiness |
| --- | --- | --- |
| [Vehicle, Fantasy Sound Effects: Tinysized](https://opengameart.org/content/fantasy-sound-effects-tinysized-sfx) | CC0; actual water pouring, vial/bottle handling, bowls/spoons, pouch/quiver and page recordings | Cached `tinysized.zip`; specific candidates below exist and are unused in the 73-clip palette. Audition and trim still required. |
| [rubberduck, 80 CC0 RPG SFX](https://opengameart.org/content/80-cc0-rpg-sfx) | CC0; additional stone, wood and metal handling/contact takes | Cached archive. `stones_03/04.ogg` and `item_wood_01/02.ogg` are available alternatives to previously selected takes. |
| [rubberduck, 100 CC0 metal and wood SFX](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) | CC0; opening, closing, creaking and material contacts | Cached archive; useful only where a real mechanism justifies the sound. |
| [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio) | CC0, fifty source files; existing handling baseline | Reuse where accurate. Do not count an alias to an existing clip as a new recording. |
| [Vehicle, Fantasy Weapons and Apparel SFX Library](https://opengameart.org/content/fantasy-weapons-and-apparel-sfx-library) | CC0, organic weapon/clothing recordings | Page verified; this additional archive has not been downloaded or member-audited. Reserve if the existing cache lacks an appropriate contact. |
| [loganzsound, Firestarter pack](https://freesound.org/people/loganzsound/packs/46436/) | Pack page identifies CC0 and includes tumbleweed torch ignition takes | Candidate only. Individual file/license/download checks and listening remain. Download requests login; no access workaround attempted. Modern lighter recordings are not appropriate for lighting a torch from an existing fire. |

## Eight candidate iterations

### S1 — Footsteps follow actual remaining liquid

**Evidence.** `WorldAudioEventRouter.cs:49–61` returns `StepWater` for any physical occupant with `LiquidPoolPart`, before considering the floor. It never checks volume. `Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs` has authoritative `Volume` and `LiquidId`; its contact path rejects depleted pools. The existing ordinary glade route really drains `SpreadDrawPoint` to zero (`ReferenceGladeNativePlayer.cs:299–302`).

**Small slice.** Reject empty/nonpositive pools before choosing the existing water-step family. Decide water/brine/mire handling explicitly from actual liquid identity; do not imply freshwater or infer liquid depth that the model does not contain. Floor fallback remains unchanged. A later wooden-floor variant is optional and needs actual floor data plus an appropriate recording, not a renamed concrete step.

**Prompt.** Make a depleted pool sound like its underlying floor while retaining splashes for actual liquid; preserve physical multi-cell occupancy and forced-movement refusals.

**Test/live gate.** Extend `WorldAudioEventTests`/`WorldAudioMaterialAdversarialTests`: same actor/cell/floor with positive versus zero volume, restored positive counter, pool behind/adjacent rather than underfoot, forced motion and hidden world. In a fresh game, obtain the real vessel and drain the real glade draw point through input, then walk an actually passable source footprint and dry neighboring floor. Check passability before choosing this route; do not move a blocker to manufacture it. Record selected existing clip and real movement, with no game-state injection.

### S2 — Water handling has a vessel identity

**Evidence.** `Items/WaterVesselService.cs:29–46,86–88` already defers draw/drink diagnostics and messages until commit. `Items/WaterTransferActions.cs:31–45` commits exact source/target units. Neither has a dedicated sound. `Items/DouseWorldActions.cs:23–52` separately proves one water spent and actual extinguishing/wetting; do not treat every pour as a douse.

**Small slice.** Typed committed `WaterFill`, `WaterDrink` and `WaterPour` receipts with actual actor/contact, one restrained cue per successful action. Cached Tinysized `water-pour-01.wav`, `water-vial-fill-01.wav` and `bottle-glass-uncork-01.wav` are genuine alternatives. Audition whether a glass vial sound fits a skin; prefer neutral water movement over false glass clinks. Dousing can later choose a distinct transient only after real extinguishing evidence.

**Prompt.** Let drawing, drinking and transferring actual water sound different from cloth handling, exactly once after success and never on a refused or rolled-back action.

**Test/live gate.** Empty/full vessel, dry/non-water source, stale scope, callback rollback, canceled picker, mute and successful restoration counters. Use `WalkDiscoveries`' actual empty waterskin acquisition and basin draw, then a real drink. A transfer requires a legitimately acquired second compatible vessel. Keep exact charges, pool units and action cost assertions; observe actual imported playback while the real inventory menu completes.

### S3 — Cooking uses preparation sounds, not an idle kitchen loop

**Evidence.** `Items/CookingService.cs:14–74` verifies a current hot station, converts actual food through a transaction and publishes `FoodCooked` only after commit at line70. Its finite-temperature source checks must remain authoritative. There is no audio hook.

**Small slice.** One subdued `CookPrepare` phrase at the verified committed station. Tinysized `wood-bowl-spoon-01/02.wav` offer distinct unused takes. This supports preparation/utensil handling; it does **not** establish frying, sizzling or boiling for every recipe. Do not add continuous cooking noise to an empty cookhouse.

**Prompt.** Give a successfully cooked meal a quiet preparation response grounded in its real station, while a cold stove, missing ingredient or failed transaction stays silent.

**Test/live gate.** Extend the `DensityCookingReceiptReviewTests` and `FiniteCookingSourceTests` patterns with successful commit versus cold source, source moved during callbacks, capacity rejection and stale food. Existing ordinary glade `WalkDiscoveries` harvests real Emberwheat and invokes `Cook` at the shelter (`ReferenceGladeNativePlayer.cs:305–314`); use that unchanged acquisition path for a fresh audible run. Confirm product and consumed input as well as playing clip.

### S4 — Brewing has restrained glass/liquid preparation

**Evidence.** `Alchemy/BrewingService.cs:125–202` consumes actual selected reagents and commits a result at line191; `Inventory/Commands/Actions/BrewReagentsCommand.cs:129–168` handles an actual batch and mishaps. Batch iterations commit independently. There is no brewing audio.

**Small slice.** `BrewPrepare` uses vial/cork/liquid Foley from the unused Tinysized recordings. Publish at a committed result boundary with an explicit batch admission policy: avoid an overlapping burst for every batch unit. A mishap/sludge result must not receive a success fanfare. The service may run inside a larger command; verify the actual outer transaction relationship before selecting the hook, rather than assuming the inner `Commit()` is globally final.

**Prompt.** Give a real brew an identifiable but quiet vessel/preparation response, preserving failed-batch and mishap semantics without changing RNG, reagents or time.

**Test/live gate.** `BrewingServiceTests`, `BrewingBatchTests`, `BrewingAdversarialTests` and `BrewReagentsBatchCommandTests` supply success, partial batches and refusal surfaces. Fresh `RunSpecialistAudit` already finds a real generated field alembic, harvests its Stoneburr/FrostLichen, marks an actual reagent and invokes `BrewMix` (`ReferenceGladeNativePlayer.cs:191–229`). Retain its explicitly declared initial travel shortcut, real scheduler and stock checks. No ingredient grant or forced brew result.

### S5 — Torch lighting, snuffing and water dousing differ

**Evidence.** `Items/TorchLightPart.cs:63–118` requires actual nearby fire/fuel and rejects wet/frozen lighting; its `AfterCommit` callbacks already name `TorchLit`/`TorchExtinguished`. `DouseWorldActions` distinguishes spending water from hand extinguishing. None supplies dedicated audio.

**Small slice.** `TorchLight`/`TorchSnuff`, with a separate actual water-douse response only when justified. The Firestarter torch source is a research candidate, not production-ready. Existing PagDev fire crackle is an ambient texture already shipped, not a fresh ignition take. Never add a modern lighter click to the existing-fire action or a water hiss to dry hand snuffing.

**Prompt.** Make an actually lit or extinguished torch audibly legible using an appropriate short transient, preserving every fuel, wetness and fire-source refusal.

**Test/live gate.** `DensityTorchLightTests` plus rollback, repeated already-lit/out actions and hidden-owner counterparts. The ordinary glade chest really contains a Torch (`ReferenceGladeNativePlayer.cs:107–112`). Acquire it with native loot input, reach an actual fire, then light/snuff through ordinary actions. Keep acquisition and ignition evidence separate. Source audition/download approval is a prerequisite; defer this slice if no credible transient is available.

### S6 — Carried items retain their material when handled

**Evidence.** `Gameplay/Inventory/InventorySystem.cs:24–43` emits the same `Pickup` for pickup, container take, drop, partial drop and container put; equip/unequip share `Equip`. These are correct commit boundaries but erase the actual item's identity. Current baseline recordings are small-leather handling and a belt.

**Small slice.** Preserve the exact acted-on item/material in a bounded successful receipt; choose soft/leather, wood or rigid-metal handling with generic fallback. Candidate unused recordings: Tinysized `cloth-pouch-shake-01/02.wav`, `quiver-leather-squeeze-01/02.wav`; rubberduck `item_wood_01/02.ogg` and suitable metal contacts. Do not infer metal from the word “armor.” Capture the actual split/transferred object before merging can remove it; never search inventory afterward and guess.

**Prompt.** Let a blade, wooden object and soft carried item have appropriate handling sounds while keeping failed transfer/equip actions silent and bulk pickup restrained.

**Test/live gate.** Actual commit/refusal, stack splitting/merging, equipped ownership, material callback mutation, rollback and bulk pickup rate bounds. Use the ordinary starter dagger equip/unequip plus genuinely acquired chest items and a real drop/pickup cycle. The live check must verify item ownership and unchanged payment in addition to family selection; an arbitrary menu click is not sufficient evidence.

### S7 — Mineral harvesting differs from a plant harvest

**Evidence.** `Items/HarvestablePart.cs:18,27,184–194` supports a general yield blueprint and spent state, publishing successful harvest after commit. It includes GlowQuartzVein as well as reagent plants. The recent typed `WorldAudioWork` harvest path covers FieldHarvest/Crop; it does not currently cover this generic part. Blindly assigning the recent plant-rustle cue here would give quartz foliage sounds.

**Small slice.** A committed `HarvestMineral` loose-stone contact for verified mineral sources, and appropriate existing organic handling only for verified plants. Unused rubberduck `stones_03/04.ogg` or `item_stone_03/04` are candidate distinct takes. The mechanic does not require a pickaxe, so avoid a specific pickaxe/anvil claim. Decide explicitly whether a committed no-yield physical attempt still sounds; refusal and a previously spent source must not.

**Prompt.** Make the finite quartz seam sound like mineral gathering, with source-aware organic fallback and no replay from saved spent objects.

**Test/live gate.** `DensityHarvestSecurityTests` and the connected harvest rollback tests supply real claim/ownership boundaries. The existing native glade route really executes `Harvest` on GlowQuartzVein and verifies produced quartz (`ReferenceGladeNativePlayer.cs:115–120`), then saves/loads its removal. Run that path fresh, verify cue before removal via its committed receipt, and verify no replay on F6. Preserve organic harvest as a material counter.

### S8 — Throws distinguish release from actual contact

**Evidence.** `Inventory/Commands/Disposition/ThrowItemCommand.cs` traces at line159, emits its visual projectile at169, then branches between tonic shatter, gas payload, accuracy miss, actual damage and blocked travel. There is no typed audio receipt. `ItemLandedEvent.Broadcast` at367 fires after placement but **before the enclosing command commits**; it is not a safe final audio notification by itself. Consumed-on-impact objects do not use that event at all.

**Small slice.** First bind a quiet `ThrowRelease` to a successful real throw. Ground/material contact or `VesselBreak` requires a separate actual terminal outcome, current visible contact and committed ownership; do not infer a hit from target selection or use all generic damage. Existing appropriate impact recordings can be reused; additional glass shatter has not been selected. Timing should respect the existing projectile presentation instead of making a distant contact audible before the visible travel.

**Prompt.** Give actual throwing a clear release and truthful terminal response, while canceled targeting, misses without contact and rollback cannot masquerade as hits.

**Test/live gate.** `CombatInventoryThrowTests`, `ThrownMutationCombatAdversarialTests` and throwable-tonic diagnostics provide positive/miss/veto/ownership cases. Enter the existing native controller `LT+RB` carried-item picker with an actually acquired throwable, target ordinary floor/obstacle, and verify consumed/landed item plus imported playback. Keep canceled picker silent. No injected target HP, free ammunition or forced hit roll. This is the highest implementation-risk candidate and should follow the simpler utility receipts.

## Reserves, pruning and musical research

- Physical reading/page turning is a smaller reserve: `ReadableDocumentPart.TryRead` queues a real document; `AnnouncementUI.GoToPage` changes a visible page; `WorldAudioCue.Page` already exists but is not routed. Do not play a book sound for every generic announcement. A real multi-page document and unchanged-page boundary counter are required if selected.
- Containers do not expose a reliable persistent lid-open state merely because their loot menu opens. Do not add an “open/close chest” pair by mirroring UI entry/exit without a verified physical mechanic.
- New forge/repair/plant work, local leaf/reed/wood/stone details, wind and frog/bird source witnesses were already handled by the previous palette. They are excluded from this list.
- Music ownership belongs to the companion music sweep. One primary optional candidate is Scott Buckley's [Signal to Noise](https://www.scottbuckley.com.au/library/signal-to-noise/), whose page offers a full mix and a no-piano-melody version. Its described warm synth/low strings could be auditioned for a sparse regional role; that is an inference, not a listening result. [Filaments](https://www.scottbuckley.com.au/library/filaments/) describes a dramatic ending, so it is a lower-priority continuous exploration candidate. Both require the composer's [CC-BY usage/attribution terms](https://www.scottbuckley.com.au/library/using-this-music/), current readable credits and streamer/music-mute guidance. No music was downloaded, generated, selected or added here.

## Proposed execution order and honesty bounds

Prioritize S1, S2, S3, S6 and S7: concrete reachable gaps and cached material. S4 needs careful batch/outer-commit review. S5 depends on an appropriately licensed and auditioned transient; S8 has the broadest timing/transaction risk. Merge this list with the music/world sweeps before freezing twenty prompts; eight proposals are not eight accepted or completed iterations.

For each selected iteration, save its narrowed prompt, run observed RED before production, verify refusal/rollback/hidden-source counterparts, then run a **fresh actual input route after that iteration**. Keep raw run IDs, exact state changes, imported clip identity/playing state and fixed-source bounds. A clip existing in a catalog or a synthetic waveform test does not prove actual gameplay wiring. Source volumes, waveform metrics and screenshots do not establish subjective sound quality or physical Steam Deck playback. Listening and musical selection remain explicitly unverified until actually performed.

Files changed by this sweep: this document only. Qud supplies aesthetic context, not copied sounds or a parity claim.
