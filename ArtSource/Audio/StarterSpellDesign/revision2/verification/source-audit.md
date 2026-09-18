# Revision2 source and material audit

Audit date: 2026-09-11. Scope: source provenance, decoded signal integrity, material identity and DESIGN.md/gameplay agreement. The reviewer is independent of the mixer, fire selection and ice/calm selection; the water/rain group was originally selected by this reviewer and is explicitly a self-check. Final rendered audio, user preference and in-game behavior are outside this source pass.

## Verified results

- **26 audio sources decode** through FFmpeg to finite, nonzero float PCM. No source is an empty file or wholly silent.
- All **18 distinct source/upstream pages inspected** expose a per-asset CC0 license widget. Direct links and page hashes are retained in `source-license-audit.json`. Pages include both themightyglider's rock derivative and SoundCollectah's explicitly CC0 upstream recording.
- All three source ledgers contain 26 audio entries; all file hashes, creator/source/download fields and stated CC0 licenses match. Ice/calm archive members exactly match the preserved archives, and its saved source HTML hashes match the ledger.
- `source-signal-audit.json` records complete decode diagnostics, amplitudes, DC, silence margins and downmix measurements. Its loudness numbers are diagnostics, not targets imposed equally on all spells.

## Actionable source-handling findings

1. **Keep decoded intermediates floating point.** `rock_break.ogg` reaches 1.1262 (+1.03 dBFS) when preserving its stereo channels at48kHz, with161 channel samples at/above full scale. FFmpeg's default stereo-to-mono matrix can sum correlated content at another roughly3 dB gain; the other agent's approximately1.59 peak is therefore consistent with a different downmix convention. Explicit arithmetic channel averaging and gain reduction before PCM writing avoids accidental clipping. The near-full-scale ice WAVs also need mix headroom. This is not a final-output failure; source decoding simply must not silently clamp the transient.
2. **Jet source crops must reach the water.** Wave01 has its strongest body around0.95–1.25s and0.355s of nearly silent tail. Wave02 is strongest around0.4–1.05s and ends with0.258s of near-silence. Stretching a quiet end to fill a body layer would create dead space or amplify recording noise.
3. **Keep provenance precise.** The rain, bowl, exhale, plank-creak and bamboo-chime MP3s are public HQ previews, not the original lossless uploads. They remain CC0 recordings but should not be labelled lossless originals or96kHz masters. Upsampling them to the project rate does not restore discarded information. The final ledger must preserve this distinction.
4. **Rime's bart samples are designed derivatives.** The source page describes Audacity-made ice spell sounds derived from Stephan at pdsounds. The linked upstream slug is `bones_breaking_wood_fire_ice_crackling`; it does not establish a pure field recording of forming ice. These are useful brittle Foley, but do not claim they are newly recorded physical ice. Prefer separated hard shatter events over making that crackling derivative a dominant continuous bed. IgnasD labels the other sources ice shattering but supplies no recording-method detail.
5. **Subtle source grain should survive the layering.** The soft water drop OGG peaks at -25.8dBFS. If it is normalized independently to the same peak as an impact, recording noise can become dominant. Crop an actual drop before applying gain, and keep it subordinate to the main splash/patter.
6. **Mono checks are acceptable, not perfect preservation.** Rain loses about2.3dB of RMS under arithmetic mono summation, bowl about1.2dB, wave01 about0.7dB. No catastrophic polarity cancellation found. Match mono audition/output gain deliberately rather than mistaking this loss for an absent layer.

## Per-spell material and gameplay review

| Spell | Distinct material anchor | Match to verified gameplay | Risk to avoid |
|---|---|---|---|
| Flaming Hands | Immediate match catch and coarse flame flare, dry burning residuals | Adjacent-cell heat/fire that can still cast into empty space | A crushing-log impact would recopy Ember; fireplace source used as an atmospheric loop would lack outward force |
| Jet Blast | Hollow liquid gather, coherent rush, broad wet slap, broken spray | Two-cell wetting cone, token damage, attempted shove, six-turn water coating | Ocean wave is a liquid-mass proxy rather than an actual pressure nozzle; shorten its approach. It must not sound like distant surf or a fire-whoosh |
| Ground Surge | Real Tesla buzz and irregular arc snaps over short abrasive ground movement | Four-cell electrical line and floor charge, damage, attempted shove, conditional Electrified | Large rock collapse falsely implies guaranteed terrain demolition. Keep the grounding layer short; don't disguise electricity as an earthy drum pattern |
| Rime Grip | Inward brittle stress followed by distinct hard closure and small settling fractures | Single nearest target cold damage, attempted Frozen; water-only empty-line branch; killed target shatters | A broad outward shatter on every cast would imply an explosion, and a lock sound on resisted freeze would give false success feedback |
| Calm | Sustained real bowl resonance and quiet human exhalation | Non-damaging single-target pulse, NoFightGoal50, no stacked pacify or brainless-target success | No healing sparkle, recruit fanfare, snore or injured gasp. Keep intimate breath low and bowl's struck transient out of the smooth body |
| Conjure Rain | Dense natural foliage droplets with intermittent larger runoff | Radius3 crop moisture top-up40; consumes cast even with no crops | No thunder, lightning, combat Wet, persistent weather or floor-puddle claims. Keep fine patter dominant; don't share Jet's large wave body |

The six proposed anchors and gestures are materially distinct on paper, with no need for a common rumble/noise bed. Their final audible distinction must still be judged on the actual rendered mixes at comparable listening loudness. Numerical uniqueness of file hashes, peaks or spectra does not prove recognizable spell identity.

## Fidelity findings in DESIGN.md

The inspected mechanical summaries agree with the source code. Rime's phrase “freezing water under/along valid ground branches” correctly avoids promising an ice trail over dry rock. Rain's radius3 query is a Chebyshev square and only handles CropPart owners. Calm resolves a NoFightGoal rather than applying a normal Pacified Effect object; the named captured result Pacified is nevertheless the correct presentation event. Ground Surge's attempted shove can fail independently of its40% Electrified roll. No production code needs alteration for this asset design task.

Source code inspected: `Pyromancy_FlamingHands.cs`, `Hydromancy_JetBlast.cs`, `Galvanism_GroundSurge.cs`, `Cryomancy_RimeGrip.cs`, `Spellcraft_Calm.cs`, `Hydromancy_ConjureRain.cs`, all in `Assets/Scripts/Gameplay/Skills/`.

## Honesty bounds

Can verify: source pages and stated licenses, local hashes, successful float decode, signal/silence properties, distinct physical source assignments, and agreement with currently inspected mechanics.

Cannot verify from this pass: how enjoyable the mix feels, whether a player identifies each sound without a label, original recordists' entire recording chain, or runtime result-conditional playback. The revision is an audition asset set, not a runtime implementation.

Final supplemental check: the explicit wooden-plank proxy and bamboo chimes decode finite/nonzero; their primary Freesound pages also state CC0. The pressure proxy is clearly labelled in the source ledger, so a user-facing description should call it creaking material/ice-pressure Foley rather than a verified ice groan recording. Bamboo is a quieter supporting texture, not a third dominant impact.
