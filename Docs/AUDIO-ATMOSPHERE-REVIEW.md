# Atmosphere follow-up: independent source review

Status: recommendations and acceptance candidates, 2026-10-11. No production
changes or Unity execution by this review. The coordinator owns the saved
iteration prompts, RED/GREEN, and a live game check after **each** iteration.
Build on the completed place-audio layer and its 1478/1478 native regression
receipt; do not present these recommendations as shipped or auditioned.

## Verified gaps

| Finding | Current source evidence | Player consequence / qualification |
| --- | --- | --- |
| A roof removes almost all environmental continuity. | `WorldAudioSoundscape.SelectBedCore` returns null for a surface interior before assigning habitat; source discovery also excludes the other side of the interior flag. [Selection](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L106), [source gate](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L125). | An empty room loses its ambience after the crossfade. Music continues, so this is not total game silence. A nearby indoor wooden door or actual fire can still produce an occasional detail. |
| Water erases the regional bed at full fixed strength. | A nearest eligible flowing-water owner within radius six sets `AmbienceId = "water"`, irrespective of distance. [Water override](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L117). The catalog's water gain is 0.2; night-field is 0.18; ambience sources have no position-dependent pan/gain. | A river edge at night loses the continuous insect bed. The separate insect detail remains eligible, so it is inaccurate to claim insects are disabled. Acoustic masking is a mix-review question; regional-bed replacement and absent attenuation are confirmed code behavior. |
| All details compete for a single slow random opportunity. | Initial wait 5–10 seconds, then 10–22 seconds, doubled in Overwrit and multiplied by 1.8 in danger; one random-offset cursor chooses among all point categories and habitat. [Scheduling](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L72). | Adding trees, water and a fire can delay the frog or habitat cue even though those remain visible. This is unbounded selection unfairness, not a proven deterministic permanent starvation bug. The existing two-voice capacity is normally unused because the longest shipped detail is 3.02 seconds. |
| Place variation remains broad. | Spread, Jungle and Grovelands use the same beds; Beating, Overwrit, Stump and Ruins use dry-wind; `marsh` references the exact dry_wind clip at gain 0.09. [Selection](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L112), [catalog](../Assets/Resources/Audio/World/places-catalog.json#L4). | Biomes differ partly through music and object presence, but Sodden's ambience is not yet a distinct wetland recording. Time-of-day changes do not distinguish the dry regions. |
| Nearby movement does not move an already-playing detail. | Distance and X pan are calculated only at `TryPlayDetail`; player updates change remaining time and bus volume. [Emission](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L80), [playback](../Assets/Scripts/Presentation/Rendering/WorldAudioPlayer.cs#L111). | A frog moving within visible range keeps its previous pan until that short clip ends. Losing visibility/range correctly stops it. The current clips are short, making this a polish gap; it becomes significant for longer layers. |
| Several visible landscape types have no distinct texture. | Tree, Reeds and wooden doors all classify as Wood; DryBrush and standing crops have no branch. [Classification](../Assets/Scripts/Presentation/Rendering/WorldAudioSoundscape.cs#L130). | A reed bank, an emberwheat field and a wooden room cannot yet differ through their actual nearby vegetation. Adding a generic sound to every prop would make this worse. |

Source verification corrections: `Cell.IsInterior` is an actual per-cell roof
flag, not a room ID or building material. Underground cells also use it, so
depth must continue to take precedence. The audio runtime currently reads no
POI profile. `CurrentWindSpeed` defaults to zero and no dynamic weather authority
was established by the prior sweep. A breeze-like recording is cosmetic air,
not evidence of a simulated storm. No additional wildlife, work schedules or
hidden source authority is needed for these four iterations.

## Four cumulative iterations

### 1. Give shelter an intentional quiet identity

Keep the current room boundary and exterior wildlife suppression. Add a quiet
non-wildlife room-air bed for actual surface interiors, using a licensed or
original neutral recording; do not repurpose a dripping cave recording as a
dry room. Keep underground ambience distinct. Existing visible hearth/wood
details provide local identity without inventing inhabitants or work. No crowd
loop based only on a village POI, and no hammering because a forge exists.

Acceptance candidates: outdoors → roof → outdoors changes the bed without
restarting music; interior animals outside the roof remain inaudible; an empty
room has quiet air but no people/work; a finite hot-but-unlit cooking source
remains silent; cave depth still wins. Enter a real Sill or Sumphold room in
the live game, wait long enough for the fade and sparse detail, then leave.
Capture the selected bed, source count and sample advancement; listening
judgment is separate from a nonzero waveform assertion.

### 2. Make sparse details fair and legible

Separate regional habitat opportunities from current point-source opportunities,
while keeping at most two decorative voices in the existing twelve shared cue
slots. Use a deterministic fair rotation among eligible point categories with
per-category spacing and cosmetic jitter, not a fresh random category every
time. Quiet Overwrit and visible danger still lengthen intervals. Failed play,
mute, menus and exhausted capacity must not create a deferred burst.

Before allowing overlap, give each accepted decorative playback a bounded
voice token/generation and its own witness. The current two witness slots are
cleared together and are not tied to voice expiry/reuse. That is adequate for
the shipped non-overlapping cadence; shortening the cadence without changing
this ownership would leave stale witnesses or an unwitnessed third playback.
Actions must retain the ability to reclaim a decorative slot, invalidating
that exact token. This is a prerequisite, not a request for more AudioSources.

Acceptance candidates: with stable visible frog, water, foliage and habitat,
each allowed category is served within a specified bounded number of accepted
opportunities; disabling only the frog removes frog cues; doubling identical
frogs does not double output; an interrupted/reused voice cannot be controlled
by its former witness; one lost witness stops only its own voice. Sixteen
sources remain allocated, no idle discovery scan occurs, and gameplay RNG is
unchanged. Live-test the same inhabited riverbank for at least a minute before
and after, checking that water/wood do not monopolize its details.

### 3. Keep the region audible beside real water

Retain the regional bed and add at most one source-owned flowing-water layer
through one of the existing shared decorative slots. Keep the other decorative
slot available for animal/habitat/structure details. The new water layer may
loop only while its exact visible witness remains authoritative; standing
puddles, brine and mire still use sparse lapping instead of a river loop. It
must yield to actions, resume only after revalidation, and stop immediately
on hiding, draining, removal, room change or menu entry.

Use a small distance attenuation curve and capped stereo pan derived from the
current source/listener cells. Update only retained voice parameters while
idle, with brief smoothing for motion; this is not 3D occlusion or gameplay
hearing. The same token API can update a moving animal's short detail. Do not
restart a playing waveform to change its gain or pan. Choose gain by actual
mix review; catalog gains alone do not measure perceived loudness.

Acceptance candidates: nighttime regional insects and valid flowing water are
both present; farther visible water is quieter than closer water; left/right
movement crosses center smoothly without resetting sample position; moving
out of range or hiding retires water immediately; a nearer standing puddle
does not hide a farther valid flow witness; action saturation preempts water
without exceeding sixteen sources. Live-test approaching and leaving a real
river at night, then douse/remove a controlled source in the audit fixture.

### 4. Distinguish nearby landscape, then tune the regional palette

Split close vegetation into restrained leaf/reed rustle, dry brush, and standing
grain where actual visible owners support it. Reuse the bounded discovery
pass and retain only one closest witness per approved texture family; density
may alter a capped weight, never allocate a chorus. Keep wooden door creaks
as structure cues. A standing crop needs its owned `CropPart` at actual stage
1 or 2, or exact `RipeCropRow` with owned `FieldHarvestPart.Harvested == false`.
Seed stage, removed crop and harvested stubble refuse the standing-grain cue;
audio never reconciles growth or harvesting. Prefer a dedicated dry-foliage
recording over labeling a door creak as rustling grain.

Give Sodden a real sparse non-animal wetland-air bed rather than a gain alias
of dry wind. Tune Grovelands to a quieter enclosed-vegetation texture and
Overwrit to longer empty spaces; do not fabricate Choir singing or a lore
alarm without implementing their actual source/event. Preserve recognizable
silence in unoccupied bog and dry wilderness. Prioritize these distinctions
over adding every proposed species at once.

Acceptance candidates: change only crop stage/harvest state and its texture
eligibility changes; cutting/clearing the last nearby stand removes its voice;
hidden vegetation cannot set a chunk identity; dry brush never yields a wet
sound; four copies of one tree do not become four calls. Live-walk a planted
Spread field, Sodden reed edge, Beating brush patch and quiet Overwrit chunk.
Document what differs and what remains shared, including reused recordings.

## Shared completion gate

Each iteration's saved prompt must name the previous live result, its concrete
audible goal, testable refusal conditions, voice/work budgets and the next live
route. Execute native RED before production, then relevant GREEN and the live
route before proceeding. Keep recordings/receipts separate per iteration.
Retain the 169-cell/512-owner discovery cap, fixed small witness arrays, zero
idle full scans, no gameplay RNG/time writes, exact visible-source authority,
independent music/ambience/SFX mute and the sixteen-source cap. Record any
change to those bounds explicitly. Native playback/sample or output-energy
checks establish that audio was rendered; only actual listening establishes
whether the layer is pleasant, identifiable and appropriately quiet. Mac
editor checks do not establish physical Steam Deck behavior.

Additional verified references: [crop stage](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L39),
[finite field harvest](../Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs#L10),
[DryBrush](../Assets/Resources/Content/Blueprints/Objects.json#L31097),
[RipeCropRow](../Assets/Resources/Content/Blueprints/Objects.json#L32347),
[GroveSeep](../Assets/Resources/Content/Blueprints/Objects.json#L34051),
[world sound intent](FELLING-WORLD-DESIGN.md#L259),
[prior source/authority sweep](AUDIO-PLACE-SWEEP.md),
[completed native regression](Verification/PlaceAudio/broad-green.xml).
