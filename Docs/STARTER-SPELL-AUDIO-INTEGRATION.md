# Approved starter sound integration

Status: baseline verified; initial implementation and adversarial tests authored; focused run pending. This implements the user's approved recorded-material revision2 for Flaming Hands, Jet Blast, Ground Surge, Rime Grip, Calm and Conjure Rain. Approved Ember remains in place. Original CoO presentation work; no Qud parity claim or gameplay/save change.

## Readiness and scope

🟢 Approved 18 mixes,60 stems, source provenance and182 hashes are frozen in `ArtSource/Audio/StarterSpellDesign/revision2`. 🟢 Copied gameplay results and renderer contact clocks already exist. 🟡 Full-timeline stems need event-relative lossless exports and conditional gates. 🟡 Native DSP and full regression verification remain. ⚪ Subjective listening is the user's approval; technical checks cannot establish listening quality.

Milestones: capture baseline; fail causal runtime tests; export runtime clips; implement bounded shared presentation audio; focused/adversarial tests; independent cold-eye review/fixes; actual native spell casts and60–90-second profile; full suite comparison; ordinary Unity reopen and work log.

## Preimplementation verification sweep

Read: CLAUDE.md; performance foundation; existing Ember player/importer/tests/coordinator/native audit; revision2 README/design/manifest/source verification; SpellFxSequence/Capture; all six gameplay skill implementations; CropPart.Water; TileReactionSystem freeze_water; native/sprite contact clocks.

| Premise | Verified correction / implementation consequence |
| --- | --- |
| Whole approved WAV can be played on any cast | Mixes demonstrate success. Gate success layers on copied outcomes; keep gestures available on valid unsuccessful casts. |
| Ember's LastContactCell works for every spell | It is path-last and (-1,-1) for pathless Flaming Hands/Rain. Generic outcome checks use actual copied geometry; clock comes from selected backend. |
| Rain has a watering result field | It has captured crop targets immediately followed by Water(40), not an explicit reaction. AffectedCells has a source fallback on empty casts and is insufficient. Target-based gate matches shipped native visuals. |
| Rime applies 'Frozen' | Copied effect ID is 'FrozenEffect'; freeze-water ground reaction is Kind='reaction', Value='freeze_water', Amount>0. |
| Push attempts mean movement | Only copied Moved proves success; both contact/final cells must be visible. |
| Zero damage means Calm failure | Success explicitly records applied 'Pacified'. Already peaceful/brainless records rejected Pacified. |
| Rain is a newly learned starting spell | Rain is learned from the carried grimoire. Do not change grants or hotbar. |
| New audio pool can independently use full volume | Shared active voice budget must include approved Ember to prevent overlapping spell groups doubling gain. |

## Runtime contract

One additional prepared player owns four reusable voices with up to eight sources each (four layer prefixes + suffixes). JSON catalog and all PCM48k mono clips load at zone binding. Nothing discovers assets/components or allocates sources in the per-frame hotpath. All voices use the existing F10 SoundVolume, selected backend's contact clock, cosmetic seed variants, and bounded audio pitch. Tails never own a turn wait. Cancellation symmetry covers mode/visibility, native surface, queue clear, zone, mute/pause, timeout and disposal.

Neutral full-timeline layers split over a20ms complementary fade beginning at authored reference contact. Prefix is cast-relative; zero-start suffix begins at actual contact. Exact PCM integer reconstruction is verified at reference timing; shifted timing deliberately retimes the same material. Outcome-only layers are already silent before reference contact and are suffix-only. Shared layer gain preserves the approved balance; any safety attenuation is common and documented.

| Sound | Contact-only gate |
| --- | --- |
| Flaming Hands | Accepted visible flame; afterburn is the spell's own fire tail, not proof of persistent Burning. |
| Jet Blast wet slap | Visible actual target in copied cone/path geometry. Water pressure/runoff remain on ground-only casts. |
| Ground Surge grit | Visible copied path target actually displaced to a visible destination. Stone accent is ground-contact Foley, not terrain destruction. |
| Rime lock/fragments | Living endpoint target with applied FrozenEffect, or positive visible freeze_water reaction along copied path. |
| Calm overtone | Living endpoint target with applied Pacified. |
| Rain all layers | Visible copied watered crop target in affected geometry; one rainfall voice regardless of crop count. |

## Verification and implementation log

- Fresh baseline snapshot records hashes for all existing Assets and ProjectSettings, current git status, and preserved bytes for intentional edits and Unity's known QualitySettings rewrite. Native editor was idle with clean Main scene before ordinary quit.
- A00 baseline:11,435 total,11,404 pass,31 failures identical by name and message to the previous Ember final run;0 compiler errors. A01 RED: new StarterSpellAudioPlayer missing (CS0246), fresh receipt recorded before runtime implementation.
- Added a16-second raw audio timeout for the new player: the2.35-second Calm mix needs9.4seconds at the supported0.25animation speed. Visual waits retain their existing bound.
- Dedicated tests cover positive and flipped-flag outcomes, backend clocks/hitches, layering/variant/headroom, visibility, pool limits, cancellation and nonblocking tails. Dedicated adversarial file remains a separate gate.

## Self-review / divergences

Pending implementation. No commit yet; mixed pre-existing work remains outside this change.

### Initial review corrections

- A02 imported the frozen resources and ran139 focused cases:57passed,82failed,0compiler errors. The loader wrongly required a contact suffix for every layer. Ground Surge's charging_current ends before contact in variants1/3; it has an authored suffix only in variant2. Loader now accepts a nonempty prefix-only layer and rejects any conditional prefix. Expected Surge suffix counts are3/4/3(success),2/3/2(blocked). No silent placeholder audio was invented.
- Independent real-cast review found two geometry assumptions to fix: a diagonal Jet wing can be three cells away on one axis, and nearby ambient reactions can appear in the same capture without being hit by Jet. Use explicit copied selection provenance rather than re-running/inventing a cone.
- Rime can really freeze water beneath a large scenery object's original anchor, off its physical-contact path. Preserve that original anchor in the transient copied result so contact sounds can relate the actual ground reaction to the selected owner.
- Hypothesis tests now measure allocations on actual contact, not just pre-contact frames: Core.Point inherits ValueType.Equals and coordinate comparisons can box. Fix only after the focused RED receipt.
- Export verification:85 PCM24 clips,60 exact original layer reconstructions,5,757 retiming/subset combinations and a conservative any-offset bound0.860441 after uniform0.7768969gain(−2.19dB). This changes overall level equally, preserving internal balance. Approved source hashes remain intact.

- A03 review RED:142focused cases,138pass,4fail,0compilererrors. Confirmed diagonal Jet omission, two ambient-reaction false Jet impacts, and omitted off-anchor Rime ice. Warmed contact-allocation tests all passed already; they pin correct behavior, not an allocation fix.
- Fix uses copied original AnchorCell and explicit path/area-selection IsDirectTarget provenance. Capture marks only EndPathAt/TargetOnPath/TargetInAffectedCells; false also covers unspecified legacy TargetAt usage, so Rain keeps its dedicated crop-target contract. No targeting/damage/movement algorithm is rerun or changed. These fields are transient presentation data, not saves.

- A04 GREEN:269/269 focused audio/capture/starter-3D cases,0 compiler errors. Independent taxonomy and actual-mechanics reviews found no remaining must-fix after the corrections. Additional moved-Surge provenance pair will run in final suite.
- Commit boundary: the existing coordinator, Ember player and capture source were all already untracked before this turn. Their complete contents belong partly to prior work. Preserved snapshots and per-turn diffs make this integration reviewable; no index/staging changes will absorb that unrelated work.

- N01 live run: all19 initial real-cast success/control rows passed, including all7sounds,7muted counterparts, water-only Rime, dry refusal, already-peaceful Calm, blocked Surge and empty Rain. One later profile Jet variant3 row had advancing prefix sources but a zero listener peak during observed prefix frames; contact output and natural cleanup succeeded. Audit intentionally failed, retained the receipt and restored private-save isolation with exit1. Added source-level DSP and frame-gap diagnostics before repeating; no claim yet that this was a playback bug or a sampling miss.

## Accepted integration — final gates

A05 source-loss RED:150/151passed. Destroying an old Ember AudioSource then starting a different spell before Update canceled the new sound through shared mixing. Added the missing null guard in Ember RefreshMix; counterpart Starter-to-Ember already passed. A06 full:11,506/11,537passed,31 failures identical by name and message to A00; all102 new tests pass, zero C# errors. Dedicated adversarial suite contains60cases; cold-eye geometry fixes and source-loss fix are covered by actual RED receipts.

N02 was an audit contamination failure: ordinary keyboard input routed an extra Rime cast into the shared counters. N03 isolates only InputHandler.Update after normal boot and restores it on cleanup, leaving normal ZoneRenderer FX ownership intact. N03 (`da9e8ec65003454cb8cd6224f83a584f`) passed all37 actual-command rows, including19 success/control rows and18 profile casts.32 Starter +12 Ember sources remained fixed; natural tails cleared; no unexpected errors.61.003s/17,276samples measured2.753µs mean and85.458µs maximum Starter update time. Earlier N01 short-prefix listener miss did not recur; its cause was not established and no unsupported playback fix is claimed.

Final independent file audit:182 approved audio hashes,86 imported hashes (85WAVs plus catalog),9 original Ember WAVs,85 importer configurations, zero GUID collisions, exact main-scene preservation. Restored only the verified automatic QualitySettings schema rewrite. Source audition manifest remains frozen, including historical status text; runtime status is here.

Can verify: normal boot and real routed commands, gameplay outcome gates, real source sample advancement and sampled Unity mixer output, mute controls, fixed source pool, cleanup and measured CPU marker. Cannot verify from these records: physical speakers/headphones, subjective timbre/comfort, visual quality or raw keyboard handling in the isolated final audit. No additional hot-path optimization was needed.

Files: StarterSpellAudioPlayer, StarterSpellAudioImporter, shared coordinator and Ember gain lifecycle, transient SpellFxSequence provenance,102 tests, native audit,85 imported clips/catalog/credits and runtime exporter. No gameplay targeting, save schema, camera or spell art changed. Existing untracked source contributions are preserved; no whole-file staging/commit absorbs them.
