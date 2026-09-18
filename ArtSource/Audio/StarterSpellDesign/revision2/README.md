# Starter sound revision2 — individual recorded-material studies

Revision1 was rejected as too similar. These mixes replace its shared noise/swell approach with distinct recordings and Foley: combustion; moving water; Tesla arcs and stone; creaking/brittle ice; bowl/exhale; foliage rain. They are audition designs outside Unity. The approved Ember and current game audio are unchanged.

[Mechanical breakdown and brainstorm](DESIGN.md) · [Source credits](SOURCES.md)

## Listen

[Combined14-second reel](preview/recorded_material_reel.wav). Order and timings:

| Start | Spell | Full mix | Individual layers, then mix |
| --- | --- | --- | --- |
| 0.25s | Flaming Hands | [Listen](preview/flaming_hands.wav) | [Hear components](preview/flaming_hands_layers_then_mix.wav) |
| 2.30s | Jet Blast | [Listen](preview/jet_blast.wav) | [Hear components](preview/jet_blast_layers_then_mix.wav) |
| 4.50s | Ground Surge | [Listen](preview/ground_surge.wav) | [Hear components](preview/ground_surge_layers_then_mix.wav) |
| 6.25s | Rime Grip | [Listen](preview/rime_grip.wav) | [Hear components](preview/rime_grip_layers_then_mix.wav) |
| 8.40s | Calm | [Listen](preview/calm.wav) | [Hear components](preview/calm_layers_then_mix.wav) |
| 11.25s | Conjure Rain | [Listen](preview/conjure_rain.wav) | [Hear components](preview/conjure_rain_layers_then_mix.wav) |

The layer reels retain actual mix gain: each stem plays alone, followed by the unchanged full composite. They are not separately boosted. Individual stem links and their order:

### Flaming Hands

- [palms catch](layers/flaming_hands_01_palms_catch.wav): Two small match flares prepare the hands.
- [combustion body](layers/flaming_hands_01_combustion_body.wav): Slowed real fire gives the expanding flame physical weight.
- [dry afterburn](layers/flaming_hands_01_dry_afterburn.wav): Natural fireplace snaps continue as the sheet loses pressure.

### Jet Blast

- [liquid gather](layers/jet_blast_01_liquid_gather.wav): Hollow moving liquid rather than air.
- [pressurized water](layers/jet_blast_01_pressurized_water.wav): The actual water mass accelerates through release.
- [wet slap](layers/jet_blast_01_wet_slap.wav): One broad water splash at physical contact.
- [bubbles and runoff](layers/jet_blast_01_bubbles_and_runoff.wav): Liquid cavities and small drops remain after the large slap.

### Ground Surge

- [charging current](layers/ground_surge_01_charging_current.wav): Rough Tesla current establishes in a short held electrical note.
- [advancing arcs](layers/ground_surge_01_advancing_arcs.wav): Connected electrical snaps advance over the four-cell study path.
- [ground fracture](layers/ground_surge_01_ground_fracture.wav): A single stone-contact Foley accent gives ground weight; it does not report terrain destruction.
- [grit displacement](layers/ground_surge_01_grit_displacement.wav): Short irregular stone/gravel movement trails the charge.

### Rime Grip

- [inward ice stress](layers/rime_grip_01_inward_ice_stress.wav): A low wooden-creak Foley proxy supplies pressure; reversed ice-pack fracture draws inward.
- [brittle lock](layers/rime_grip_01_brittle_lock.wav): Hard ice fractures close in a tight uneven pair.
- [settling fragments](layers/rime_grip_01_settling_fragments.wav): A few higher brittle pieces settle after the restrained closure.

### Calm

- [warm bowl body](layers/calm_01_warm_bowl_body.wav): A real rubbed bowl sings softly, with its hard strike excluded.
- [resolved overtone](layers/calm_01_resolved_overtone.wav): The same material opens into a quieter related resonance at actual pacification.
- [human exhale](layers/calm_01_human_exhale.wav): A real gentle exhalation releases the tension without an impact.

### Conjure Rain

- [leaf rain](layers/conjure_rain_01_leaf_rain.wav): Real fine rain on foliage is the defining body; no bass wind.
- [close leaf patter](layers/conjure_rain_01_close_leaf_patter.wav): A quieter low-mid foliage layer gives raindrops body without a water slam.
- [individual drips](layers/conjure_rain_01_individual_drips.wav): Small selected recorded water contacts remain as the rainfall thins.

## Production and review

18 final mixes (three variations each),60 full-timeline stems,13 listening previews, and corresponding float masters. There is no fixed four-layer formula: some spells use three, others four. Every layer is derived from attributed sources; no shared generated-noise or oscillator bed was added.

Run `OPENBLAS_NUM_THREADS=1 python3 mix.py` then `OPENBLAS_NUM_THREADS=1 python3 verify.py` here. `verification/verify_drip_crops.py` separately checks that rain's selected discrete-drop spans contain actual event activity, not amplified background gaps. The original drafts and intermediate revision2 previews are preserved.

Review improvements: source-level soft peak control prevents isolated fire/arc/gravel clicks from making the whole sound too quiet; Calm uses the bowl's post-rub tail; Rime gains an explicitly labelled wooden-pressure creak under ice fractures. Independent review found two Rain crops contained mostly background noise. A source-activity test reproduced those mistakes before correcting the crops to actual droplet events.

Technical verification checks formats, finite/unclipped samples, quiet boundaries, exact stem reconstruction, source hashes and original-gain review reels. These checks do not establish subjective listening quality. Current mixes demonstrate successful outcomes; runtime integration must gate Rime's lock, Calm's resolution and crop patter on actual results, and must not infer terrain destruction from Ground Surge's stone Foley. Full-timeline layers are not yet a finished event-relative game sound library.

Independent final audit: all182 hashes,91 PCM24 exports and108 recorded-source operations verified; source reconstruction within0.5 PCM24 LSB. Layer sums and preview sequencing match. Peak headroom is at least3.2dB with4× oversampling. No recording is shared across different spells. Receipt: [independent-final.json](verification/independent-final.json). These are technical results, not a claim of subjective listening approval.


Current runtime status: the approved revision2 sounds are now integrated and verified. The frozen audition manifest/masters retain their historical creation status and hashes. See `/Users/steven/caves-of-ooo/Docs/STARTER-SPELL-AUDIO-INTEGRATION.md` for outcome gates, full regression and native acceptance.
