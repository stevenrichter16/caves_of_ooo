# Flaming Hands and Ground Surge: distinct material sound designs

Status: source discovery and mechanical verification complete, 2026-09-11. These are unmodified source recordings for the root task's revision-two mixes. No runtime or Unity asset changes in this subtask. TDD is not applicable to downloading and documenting audio source material; hashes, audio decode, source licenses, and mechanical claims were checked directly.

## Flaming Hands

The player directs a short, point-blank flame cone into one adjacent cell. The flame heats the ground (including ignitable oil), damages all eligible creatures and scenery there, and can transfer heat to ignite the recipients. Casting at an empty cell is still a valid flame burst. Range 1, 1d4 heat damage, cooldown 10.

Verified in `Assets/Scripts/Gameplay/Skills/Pyromancy_FlamingHands.cs`: targeting/range lines 30–39; ground fire 55–58; all elemental occupants 60–66; heat damage and ApplyHeat 82–99; empty-space success 104–105.

**Physical action:** ignition at the palms, a forceful broad sheet of burning gas for a brief instant, then local sizzling embers. This is a close flare of flame, so the sound should start promptly, spread, and burn out. A distant flying ember, long projectile tail, rock explosion, and crushing logs are unsuitable anchors for this spell.

**Separate layers and jobs:**

1. **Ignition catch.** `ignition.flac`, a real match ignition recorded by qubodup, supplies recognizable flammable material catching. Keep a clearly audible short scratch/catch followed by the flare; use a restrained duplicate at a different pitch and timing to imply the two palms, without an artificial stereo click.
2. **Broad flame body.** `fireplace_pagdev.wav` supplies irregular low-mid wood-fire motion, while the sustained noisy portion of the match flare supplies the outward pressure. Use a short dense crop, modest pitch lowering, and a fast opening envelope with an approximately 250–450 ms shoulder. The audible identity must be in the midrange flame itself; adding deep sound must not bury it beneath a generic sub-whoosh.
3. **Dry live embers.** `fire-1.wav` supplies discrete natural crackles. Let these become easier to hear as the flame body falls away, ending around 0.8–1.2 seconds. They should be brighter and more brittle than Ember Spit's deep crushed-coal ending.
4. **Contact scorch, conditional in future integration.** A short concentrated flare/cackle at the target when something is actually hit. Keep this distinct from the cast's broad flame body; an empty-space cast still makes ignition and flame. Any prolonged scenery-burning loop should follow actual burning state, not be implied by every cast.

**Shape to aim for:** `frr-KH / FWAAH / krr-tss`. The leading identity is abrupt, broad combustion. No omnipresent wind layer and no standard bass-drum transient.

## Ground Surge

The spell drives a physical electrical wave along up to four cells of ground. It damages every eligible target encountered (6 electric/lightning damage), attempts to shove survivors one cell away, has a 40% chance to leave each surviving target Electrified, and lays charge into crossed floor cells even when there is no body to hit. It does not require a wet/conductive chain; that is a different spell's behavior. Cooldown 30. Walls can limit the wave. The shove is attempted unconditionally but can be blocked; electrification is probabilistic.

Verified in `Assets/Scripts/Gameplay/Skills/Galvanism_GroundSurge.cs`: constants and status chance 31–46; directional targeting 48–58; floor charge and empty-path behavior 84–112; damage/status 117–142; farthest-first shove 145–159; immediate floor reactions 171–174. The code routes damage into destructible scenery as well as creatures, despite older summary comments referring only to creatures.

**Physical action:** a stressed charged patch of ground releases a rolling force along the floor. Pebbles shift, a gritty pressure front advances, hard electric arcs snap through it, and a short rough charge residue remains. The essential contrast is *contact with solid ground plus discontinuous electricity*, not sky thunder or another airy elemental rush.

**Separate layers and jobs:**

1. **Electrical tension and body.** `continuousspark.wav`, recorded from a small Tesla generator by Brian MacIntosh, has the authentic short buzzing arc texture. Build only the needed duration using overlapping, differently placed sections with smooth edits. Preserve its buzzing bands and irregular break-up; heavy low-pass processing that turns it into indistinct wind loses the source's purpose.
2. **Moving solid ground.** `rock_break.ogg` (a stone landing/breaking recording, edited by themightyglider from SoundCollectah) provides the low and midweight impulse. Arrange staggered fragments with small pitch/time variations as the wave advances. `gravel.ogg` supplies the abrasive moving debris beneath it. This layer should feel continuous enough to be a moving front, not four metronomic footfalls or a single explosion.
3. **Independent arc snaps.** `spark.wav`, also a real Tesla recording, adds sparse dry electrical punctuation across the advancing front and its contacts. Let the rough electric body occupy the holes between the snaps; do not replace the whole spell with repetitive zaps. Keep these attacks separated from the rock transients, so electricity and ground remain readable together.
4. **Shove and charged aftertaste.** The scrape/rolling tail of `gravel.ogg` can support successful displacement, with a brief soft diminishing Tesla buzz behind it. A dramatic held electrocution sound must be conditional on the status actually applying. The bare-ground preview should end as floor charge, without an invented body impact or guaranteed electrocution.

**Shape to aim for:** `zzzt-KRR / trr-RRK-zzAK / grr-zz`. More irregular, dry, gritty, and electrically pitched than Flaming Hands. Use several closely linked events rather than the same smooth crescendo/impact envelope as the other spells.

## Source selection, constraints, and review

- Eight decoded sound files landed, approximately 10.9 MB in total; the largest is one 29-second fireplace recording. The 77.7 KB footstep archive is preserved, with only gravel and stone extracted. No accounts, paid actions, or large libraries were used.
- All selected source pages explicitly say CC0 1.0. The breaking-rock upstream Freesound page was also checked and says CC0. `source-ledger.json` records creator, source page, download URL, license URL, provenance, exact SHA-256, and audio metadata for every selection.
- `ignition.flac` is an altered real match recording, not a recording of a magical flame. Ground Surge's electrical source is a real small Tesla generator; the source's physical scale is small, so pitch and composition should provide scale without erasing its acoustic character.
- The stone and gravel sounds are descriptive foley: gameplay does not promise terrain destruction or falling boulders. Keep the ground layer relatively compact; reserve major rock collapse for actual destruction.
- Counter-checks for the mix: solo the fire layers and they should identify combustion; solo the ground/electric layers and they should identify grit plus electricity. Remove all added sub reinforcement and each spell must remain identifiable. Compare at matched loudness, not merely matched peaks. If both reduce to the same whoosh, the mix failed regardless of numerical spectral differences.
- Technical checks establish correct downloads and decodable audio. This subtask does not claim an auditory judgment or user approval of the eventual mixes.
- Cold-eye check: no common source recording is assigned to both spells. No premixed generic magic, firearm, or sky-thunder asset was selected. No CC BY source was needed.

## Source pages

- Brian MacIntosh: [Electricity Sound Effects](https://opengameart.org/content/electricity-sound-effects-0).
- qubodup: [Flare ignition](https://opengameart.org/content/flare-ignition).
- AntumDeluge: [Fire Crackling](https://opengameart.org/content/fire-crackling/).
- PagDev: [Fireplace Sound loop](https://opengameart.org/content/fireplace-sound-loop).
- themightyglider / SoundCollectah: [Breaking Rock](https://opengameart.org/content/breaking-rock), [original falling/breaking stone](https://freesound.org/people/SoundCollectah/sounds/109360/).
- TinyWorlds: [Different steps on wood, stone, leaves, gravel and mud](https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud).
