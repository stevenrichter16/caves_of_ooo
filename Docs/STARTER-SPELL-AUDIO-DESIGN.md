# Starter spell sound design

Status: revision1 rejected by the user as sounding too similar; revision2 rebuilt from individually selected recordings/Foley. Approved Ember Spit remains unchanged. Current analysis, source credits, mixes and layer auditions: [revision2](../ArtSource/Audio/StarterSpellDesign/revision2/README.md).

## Verified scope

`StartingSpellKit.SpellClasses` grants Ember Spit, Flaming Hands, Jet Blast, Ground Surge, Rime Grip and Calm. Its older “four primers” prose is not the actual six-entry grant. Conjure Rain is learned from the carried Watering Grimoire; include a separately labelled utility design. The current skill implementations and native study timing were checked before authoring. This phase creates editable sound drafts and previews outside Assets; runtime integration is a later phase.

## Shared direction

Each sound has four overlapping layers: low physical body, recognizable material texture, fine movement detail, and an outcome-specific finish. Layers build together and overlap through contact instead of sounding like separate drum hits. Depth comes from sustained low/mid energy as well as bass. Small speakers must still convey material and motion. Keep transients rounded, high-frequency texture controlled, tails short enough for repeated play, and three organic variants per spell. No voice lines, melodic score, named deity/faction association or new reagent requirement.

The approved Ember uses deep blowing wind, burning logs and a crushing-coal impact. Other spells share its fullness but have their own material and dynamic character. Gain is shared across each spell's variants/layers rather than normalizing every stem separately. Design target: peaks at most −3dBFS with four-times oversampling; perceived loudness remains a listening judgement. Defaults aim slightly softer for Calm/Rain. Mono source maintains the existing centered playback and useful small-speaker compatibility.

## Designs

| Spell | Low body | Main texture | Fine detail | Contact and decay |
| --- | --- | --- | --- | --- |
| Flaming Hands | A furnace inhaling, then a broad hot exhalation | Dense overlapping sheets of roaring flame | Irregular ember and burning-fiber crackles riding the same swell | A rounded pressure bloom, followed by fire folding back into a coarse hiss; shorter and wider in feel than Ember's traveling wind/log crush |
| Jet Blast | Heavy water surging through a constriction | A thick turbulent sheet of water, carrying weight through the middle frequencies | Overlapping cavities, bubbles and entrained spray | A dense water slap that breaks into falling droplets and a low draining rush |
| Ground Surge | Rough current vibrating through stone and earth | Interwoven irregular electrical arcs, with a rough granular edge | Grit skittering over the charged surface | A grounded electrical fracture, then a short, uneven discharge fading into the earth |
| Rime Grip | Cold air pulled inward under pressure | Thick ice flexing and groaning, with subdued inharmonic resonance | Closely overlapping frost grains and hairline fractures | Ice tightening around the target, finishing with a restrained lock and settling crystals; tension closes inward |
| Calm | Warm, deep, slow exhalation | Soft hollow wood/reed resonance, initially slightly unsettled | Quiet fibers brushing and fine airy movement | The tension opens into a gentler, stable resonance and fades; intimate, nonviolent and without a heavy impact |
| Conjure Rain — starter-book utility | A low soft canopy of moving air | Closely packed rainfall with a gradual onset | Rounded droplets on broad leaves and soil | Leaf taps and a soft draining/pattering tail; modest localized rainfall |

## Timing and outcome semantics

Gather starts with accepted presentation; release reference .220s. Audition contact times come from current native studies: Hands/Rime/Rain .220s, Jet .270s, Surge/Calm .320s. Runtime must take actual contact timing from the selected renderer, as Ember does. Audition durations are natural sound tails, not added cast or turn waits. Export each finish both as an editable full-timeline layer and as a separate contact-relative stem.

- Hands: one chosen adjacent cell. Valid empty casts still release the fire body; a creature/body-hit accent must not be inferred from the empty cast. No wide-area damage cue.
- Jet: ground-only casts still release water; body splash/displacement accents require actual outcomes. Use one main water mass and bounded small accents for the cone, not a full-volume impact for every entity or footprint cell.
- Surge: earth/current sound follows the line and ground charge. A lingering target-electric cue requires actually applied Electrified. A blocked push must not sound like a successful slide. No sky thunder or invented chain strikes.
- Rime: cold contact and ice grip success are separate. A failed Frozen application omits the successful locking finish; killed targets may fracture rather than lock. Water-only freezing uses the actual frozen cells. Dry empty refusals do not produce an accepted ice cast.
- Calm: only actual new Pacified gets the resolved finish. Already-peaceful, brainless, missed or refused outcomes cannot announce new pacification. No sleep, healing or recruitment sound.
- Rain: the moisture/crop patter follows actual crop results. No valid crops means a small gesture/air release, with no crop-success patter. The effect does not apply combat Wet, create floor puddles, grow crops or establish permanent weather.

## Production and verification plan

1. Save design and material/timing checks before synthesis.
2. Run a missing-delivery RED against the required sound files.
3. Produce three four-layer variants per spell, contact-relative finish clips, individual previews and one labelled-order review reel. Preserve original source masters and deterministic generation seeds.
4. Verify formats, source hashes, exact layer recombination, meaningful layer contribution, low/mid body, finite/unclipped samples, quiet boundaries, meaningful variant differences and correct contact-relative placement. Counterchecks must reject absent layers, clipped/silent audio, mismatched contact timing and identical variants.
5. Review material distinctions and outcome semantics. Numeric verification does not establish perceived realism, comfort or in-game synchronization. Audio is not imported into Unity in this design phase.

## Files

- `ArtSource/Audio/StarterSpellDesign/`: reproducible source, manifests, stems and audition previews.
- This document: design, scope corrections, outcome requirements and verification log.

## Review/log

- Verified all six actual kit grants and learnable Rain distinction; read current skill bodies and native study timing. No gameplay or Unity scene edit is required for this design phase.

- Created18 successful-outcome draft mixes,72 editable layers,18 contact-relative finishes and7 previews, with float masters. Audio is mono48kHz24-bit PCM and deterministic; no downloads were needed.
- Missing-delivery RED is retained in `verification/red.txt`. Final `verification/green.json`:18 mixes pass,230 audio hashes match,26 counterchecks reject missing/silent layers, shifted contact, duplicate variants, clipping and silence. Worst4× peak is−3.2dBFS.
- Cold-eye review found two promised verifier controls initially absent: shifted contact and identical variants. Added and ran both for every design; all12 new falsification cases reject the intended corruption. No sound files needed changing.
- ⚪ The auditions present successful outcomes. Rime's lock, Calm's resolution and Rain's crop patter still require actual-result gating during future integration. These drafts do not claim that implementation.
- 🧪 Listening quality, material realism and comfort are for auditory review. Numeric layer/spectral checks cannot establish them. Unity's scene, approved Ember pack and runtime are unchanged by this phase; no Unity test run is needed for these standalone audio/document additions.


## Revision2 — material identity correction

The previous six designs had different layer labels but shared a filtered-noise bass bed, broad swell and noise impact. Passing spectral/format tests did not establish the distinct identities the user requested. That rejected revision is preserved as history, not current auditory approval.

Individually re-read each spell's actual effect and result branches, then wrote [the physical-component brainstorm](../ArtSource/Audio/StarterSpellDesign/revision2/DESIGN.md) before mixing. Source research found26 files from18 inspected publication/upstream pages; independent review verified CC0 evidence, hashes and decodes.21 source files are actually used in the mixes. Ice-pack capture technique is unspecified, a wooden-plank creak is clearly identified as a pressure Foley proxy, and several Freesound sources are public HQ preview encodes rather than originals.

| Spell | Distinct construction now used |
| --- | --- |
| Flaming Hands | Match ignition at the palms, sustained recorded combustion, dry fireplace afterburn. No traveling-coal finish. |
| Jet Blast | Hollow liquid gather, a trimmed rushing-water mass, a wet slap, then bubbles/runoff. |
| Ground Surge | Real Tesla current and advancing arc snaps, supported by stone/gravel contact Foley. This sound does not claim the skill destroys terrain. |
| Rime Grip | Low material pressure-creak, reversed ice-pack stress, a compact brittle closure and settling fragments. |
| Calm | Real rubbed bowl's post-rub resonance, a quieter related overtone at success, and a soft recorded exhale. No impact. |
| Conjure Rain | Actual close foliage rain, quieter leaf patter and discrete drops. No wind bass or large splash. |

### Iteration and review

- Missing-delivery RED before the revision2 mixer, retained in `revision2/verification/red.json` and `.txt`.
- First source-based mix exposed excessive recording crest factors: a13ms fire crackle could dominate a whole variation's peak and force all layers down. Added documented gentle soft peak control to the recorded-source events before shared mix gain; preserved the prior previews. This keeps combustion/water/material body audible without generating a common noise floor.
- Calm moved to the actual post-rub bowl tail; Rime's pressure creak is documented as a wood proxy, not falsely claimed to be an ice recording.
- Independent final review found two supposed Rain drip crops were quiet background. `verify_drip_crops.py` reproduced6 wrong event selections across3 variants RED. Moved source crops1.58→2.06s and3.10→3.66s; all15 drip-event selections then passed their source-relative activity gate. Earlier previews/manifest preserved.
- Current delivery:18 mixes,60 editable full-timeline stems,13 previews and float masters. Six layer-audition reels play components separately at their actual gain, then the exact full mix. Combined reel is14 seconds.
- Root verification:18 mixes pass,20 corruption counterchecks,182 audio-file hashes, exact stem sums and review-reel reconstruction; no fixed low-frequency fraction or forced four-layer template. Final independent delivery verification is recorded separately when complete.
- Runtime/Assets/Unity scene were not edited; no Unity-suite rerun applies to these standalone audio/document changes. Future integration must still handle success/refusal/ground-only outcomes and must not extend turn waits. Subjective auditory success remains unclaimed until listening review.

- Final independent audit passed against exact manifest SHA256 `0f32e12303f04e5691b3f801b57760064d68d987d7f6cc51fa5ea6fea7289ab7`:182 audio hashes,91 PCM24 exports,18 mixes+60 layers,108 reconstructed source operations from21 CC0 sources; source-operation reconstruction within0.5 PCM24 LSB. Layer sum error≤2.38e−7. Mix peaks≤−3.199999dBFS with4× oversampling; endpoints are zero. All previews/reels preserve exact sample order and actual mix gains. No recording is shared across different spells. The earlier Rain crop issue is retained as resolved in the independent receipt. Approved Ember's9 imported WAV hashes still match its approved manifest.


Current runtime status: the approved revision2 sounds are now integrated and verified. The frozen audition manifest/masters retain their historical creation status and hashes. See `/Users/steven/caves-of-ooo/Docs/STARTER-SPELL-AUDIO-INTEGRATION.md` for outcome gates, full regression and native acceptance.
