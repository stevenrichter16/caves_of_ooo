# Rime Grip and Calm — real source material, revision 2

These sources give the spells different physical identities: Rime is **stressed material suddenly locking**, while Calm is **an exhale releasing into organic resonance**. The files here are source material, not final mixes or imported game assets. No generic noise/swoosh bed was created.

The [source ledger](source-ledger.json) contains eleven unchanged audio files from six CC0 publications, exact download URLs, SHA-256 hashes, creator credits, primary-page evidence and format details. [Decode verification](source-verification.json) checks every file. Freesound files are the publicly offered HQ preview MP3 encodes; no account was used and the login-only original WAV files were not downloaded. The source pages explicitly apply CC0 to the sounds. OpenGameArt ZIPs and extracted members are both retained.

## Mechanics read from the game

### Rime Grip

Verified in `Assets/Scripts/Gameplay/Skills/Cryomancy_RimeGrip.cs`: range 5, cooldown 35, one nearest body, 4 Cold damage and base Frozen strength 0.5. Wet targets amplify freezing through the effect system. The grip is deliberately single-target because Frozen prevents actions. A lethal hit takes the shatter branch; both surviving and shattered targets still receive the ground cold/reaction pass at their captured cell.

With no body, Rime can successfully freeze water-coated cells in the actual traversed line. A dry empty line is a free refusal and leaves no cold residue. Thus a large shatter on every cast would misrepresent the ordinary grip, and a full-line fracture on a body hit would suggest an area attack the spell does not perform.

Proposed three-part identity:

1. **Compression:** a short bent-plank creak, explicitly used as an ice-pressure proxy. Pitch/EQ can make it smaller and tenser, but retain its irregular material strain. Avoid a sub-bass boom or drawn-out monster groan.
2. **Lock:** one dry ice snap at contact, with a quieter secondary granular fracture just behind it. This is the defining attack, not an explosion.
3. **Settling:** a small diminishing cluster of ice chips. Stronger shattering belongs to an actual destroyed-target outcome; a freeze-only contact stays compact.

Use the actual captured outcomes when layering: a denied Frozen may still retain the neutral cold-hit crack, but must not announce a successful locking status. Water-only success gets a thin sheet/crinkle accent on the actual frozen-water cells, without creature hurt or a body impact. Dry refusal gets no successful spell hit. A transient sound should not pretend to run for the full status lifetime.

### Calm

Verified in `Assets/Scripts/Gameplay/Skills/Spellcraft_Calm.cs`: range 6, cooldown 20, damage dice `0`, and a fixed 50-turn `NoFightGoal` with `wander: false`. It requires a brain to pacify. A target already at peace does not receive another goal; the travelled cast is still consumed. This is nonviolent pacification, not damage, allegiance conversion or forced friendship.

Proposed three-part identity:

1. **Release:** the recorded human exhale, quietly rounded and unhurried. Use the real breath's envelope rather than substituting the same filtered noise used by attack spells.
2. **Resonance:** the singing bowl's sustained post-rub tone. Fade into it gently; avoid a loud struck-bell attack, triumph jingle or horror drone. This should carry most of Calm's recognizable tone.
3. **Wooden detail:** one or two restrained bamboo-chime knocks, tucked well beneath the bowl. Preserve hollow woody character; do not turn these into hard contact impacts or a busy shower of bright chimes.

Flight can retain its neutral breath/resonant movement when pacification is declined, but a clearly resolved peaceful ending should be conditional on actual `Pacified`. There is no combat thud and no 50-turn drone. Keep the gesture and tail gentle enough to distinguish Calm immediately from Rime's sharp locking transient.

## Concrete files and initial edit candidates

Times below are numerical waveform/spectrum selection points for the mixing pass, not a claim that subjective listening has already approved them. The parent handles auditioning and final mixes. All source bytes remain untouched.

| Source | Role and candidate use |
| --- | --- |
| `ice-shatters/IceShatters/LedasLuzta.ogg` | IgnasD ice break; 0.592 s, compact sharp beginning and decaying chips. Initial candidate for the main Rime snap. |
| `ice-shatters/IceShatters/LedasLuzta2.ogg` | 0.401 s alternate small lock/chip accent. Audition as a quieter secondary fragment, not a duplicated loud hit. |
| `ice-shatters/IceShatters/LedasLuzta33.ogg` | 1.324 s, larger energetic break. Reserve for actual shatter/destruction or restrained late fragments. |
| `ice-shatters/IceShatters/LedasLuzta4.ogg` | 1.040 s alternate multi-part fracture; first strong activity extends through roughly 0.4 s. |
| `ice-shatters/IceShatters/LedasLuzta5.ogg` | 0.842 s with a later strong event around 0.4–0.6 s; useful delayed chip selection if it reads as ice in audition. |
| `ice-spells/ice.wav` | Bart's processed ice design, 1.164 s; most activity in 0.10–0.50 s. Possible thin freezing accent. |
| `ice-spells/coldsnap.wav` | Bart's 2.649 s processed cold snap; most content is in the first 1.4 s. Avoid appending its near-silent remainder. |
| `wood-creak-ice-pressure-PROXY_SilentStrikeZ_public-hq-preview.mp3` | 9.613 s actual wooden plank creak, not a literal ice recording. Candidate strain events around 1.1, 3.8, 6.3 or 8.4 s; select one short tension arc. |
| `exhale_otherthings_public-hq-preview.mp3` | 1.243 s real exhale. Breath body starts around 0.20 s, peaks around 0.30–0.45 s, then falls toward 0.9 s. Trim initial room noise and keep an eased entry. |
| `rubbed-singing-bowl_ryancacophony_public-hq-preview.mp3` | 39.231 s actual bowl performance. Start by auditioning 26–31 s of post-rub resonance rather than the louder rubbing buildup. The dominant measured mode is roughly 316–319 Hz. This real sustained body should define Calm. |
| `bamboo-chimes_gmni_public-hq-preview.mp3` | 50.099 s real bamboo chimes. Candidate isolated events near 1.6, 3.4, 16.2, 21.8, 36.4 or 41.3 s; choose a soft woody one after auditioning, not the whole ambience. |

The IgnasD publication names ice breaking but does not explain its recording process; do not describe it as verified field-recorded ice. Bart explicitly identifies his sounds as Audacity designs from Stephan's pdsounds material. The wooden creak is deliberately and honestly labelled a proxy. The bowl, breath and bamboo sources describe their physical subjects directly.

Mix in floating point and leave input headroom: several ice files are near full scale, and lossy decode/resampling can produce peaks above 1.0. Do not clip the inputs before layering or normalize each small chip to the same loudness. Rime's close, irregular fracture and Calm's sustained pitched release should remain distinct even at matched perceived loudness and with the visuals hidden.

No gameplay, camera, timing, source Blender art, shared docs, runtime code or `Assets` were changed by this source-research pass.
