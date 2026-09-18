# Hydromancy sound redesign: mechanics and source layers

Status: sources downloaded and decoded with ffprobe; final mixes deliberately left to the parent task. All selected recordings are CC0. File hashes, observed download URLs, authors, license URLs and decoder details are in `source-ledger.json`. Rain uses the publicly served HQ MP3 preview exposed by Freesound, not the login-only original FLAC; preserve that format/provenance distinction.

## Jet Blast — forceful liquid that drenches and pushes

Verified code: `Assets/Scripts/Gameplay/Skills/Hydromancy_JetBlast.cs:29-49` defines range 2, 2 damage, moisture 0.8, push 1 cell, water ground coating for 6 turns. Lines 76-87 make an empty-target cast still splash/wet the ground. Lines 90-121 apply damage, wet surviving targets, then push furthest first. Lines 124-164 coat final target cells plus the cone center line. The spell sets up electrical followups, but does not itself produce electrical sound.

Essential physical sequence: **water collects in a tight cavity → a broad, fast liquid jet escapes → liquid slaps and shoves → spray and hollow gurgles drop away.** Its identity is recognizable liquid mass and discrete hollow water cavities, with a fast wet slap. It should not sound like Ember Spit wind plus low drum impact.

Layer plan:

1. **Gather / trapped air (0.00–0.23 s):** a short `bmaczero_bubbles_loop1.wav` slice, with modest time/pitch stretch down (0.75–0.85 rate) and very short fade. The bubbles are the foreground preparation cue; no generic rising sine bass.
2. **Pressurized liquid body (0.16–0.85 s):** use a rapid rising portion of `transitking_wave_01.flac` or wave02; trim to a dense swell. Moderate compression, highpass around 90 Hz, keep water grain in 500–3000 Hz. A quieter duplicate one octave lower may supply body, but the recognizable original remains dominant. The source is an ocean wave, used as a liquid surge proxy, not an actual hose recording.
3. **Wet shove contact (about 0.30 s, ultimately tied to visual contact):** one strong `thimras_ocean_splash.ogg` or `angeloyazar_water_drops.ogg` slap. Do not pile both at equal gain. Preserve its splash/air-pocket articulation instead of masking it under a kick-like thud.
4. **Spray and drain (0.45–1.25 s):** fragment the existing water drop recording with small timing differences; place one `bmaczero_bubbles_single1.wav` cavity late. Short tail, no thunder, no fire crackle, no long reverberant wash.

Suggested mix priority: liquid surge 0 dB relative; splash -2 dB; hollow gather -5 dB; ending drops -10 dB. Adjust by ear because input normalization differs. Attack is asymmetric and quick; the ending is granular, not a mirror of the attack.

## Conjure Rain — a close nourishing shower

Verified code: `Assets/Scripts/Gameplay/Skills/Hydromancy_ConjureRain.cs:23-39` defines self-centered radius 3, cooldown 5, moisture top-up40. Lines 71-92 act on CropPart owners once each and emit rain on watered crop tiles. Lines 95-103 consume the action even with no crops. It does not deal damage, make lightning, push actors, or invoke WetEffect on creatures. Do not imply a thunderstorm combat spell.

Essential physical sequence: **first soft drops land → rain spreads across nearby leaves → saturated leaves shed larger drops → shower settles.** Its identity is many tiny wet contacts in a broad local field. Depth comes from distance and near/far droplet sizes, not more bass.

Layer plan:

1. **First drops (0.00–0.32 s):** select two or three isolated larger droplet transients from `samsterbirdies_rain_on_leaves_hq.mp3`, with their actual rain context faintly retained. No violent hit cue. A water bubble single is optional at very low gain but should not make this feel underwater.
2. **Canopy shower (0.18–1.75 s):** an uninterrupted 1.5–2 second slice of `samsterbirdies_rain_on_leaves_hq.mp3` is the dominant layer. The author explicitly recorded leaves and bushes with Tascam DR100mkIII internal unidirectional mics. Keep its naturally irregular close patter, moderate highpass 120 Hz and lowpass 9 kHz; no slow-motion pitch treatment. Preserve stereo only if the eventual game's audio presentation supports it; otherwise inspect mono sum for cancellation before export.
3. **Larger runoff details (0.65–2.0 s):** extract a different passage of the leaf rain source, highpass about 600 Hz and gate only the few prominent ticks. Scatter these softly over the primary recording; avoid phase-aligning the exact same slice. These are crop leaf surfaces shedding drops, not a second full hiss bed.
4. **Recession (1.5–2.4 s):** let the primary rain ease downward while two or three close drops remain. Optional short dull room/foliage reflection under the natural recording. Do not add thunder, sweeping wind, an explosion, or a bass drop.

Suggested mix priority: real leaf shower 0 dB; near transients -8 dB; low-level reflected/distant rain -15 dB. Rain should be calmer and longer than Jet Blast, with no shared audible source between the dominant layers of these two spells.

## Distinctness and cold-eye checks

- Jet = large moving water body, clear forward slap, hollow bubbles, abrupt momentum.
- Rain = distributed small contacts, gradual density, intimate foliage patter, soft recession.
- Do not EQ every recording into the same dark whoosh. Preserve source midrange identity.
- Neither sound needs artificial subbass to satisfy layered/deep. Avoid the rejected common rumble + bandpassed noise + round impact recipe.
- Parent should audition final layers both individually and in the mix. Source selection is licensed and technically decoded; perceived final quality and resemblance must be judged from the rendered audio, not these descriptions.

## Source provenance

- [Water Waves — transitking](https://opengameart.org/content/water-waves): CC0 wave1 and wave2 FLAC, submitted by qubodup.
- [Object Dropping into Water — angeloyazar](https://opengameart.org/content/object-dropping-into-water-0): CC0 OGG splash.
- [Rain on leaves — SamsterBirdies](https://freesound.org/people/SamsterBirdies/sounds/584272/): CC0, recorded leaves/bushes, public HQ MP3 preview downloaded.
- [Bubble Sound Effects — BMacZero / Brian MacIntosh](https://opengameart.org/content/bubble-sound-effects): CC0 single1 and loop1 WAV. Optional author credit preserved in ledger.
- [Ocean splash — Thimras](https://opengameart.org/content/ocean-splash): CC0 OGG water impact.
