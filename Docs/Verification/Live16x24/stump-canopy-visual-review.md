# Stump canopy fauna — bounded independent image review

Reviewed 2026-10-27. All eight original 1280×800 PNGs listed below were opened and visually inspected, including each matched before/after pair. This is an image review, not a new Unity run, interaction test, or source edit.

**Verdict: bounded visual integration pass for these three static creature bodies and their three reader portraits. No new material blocker is visible in the supplied views. Whole-area polish and fullness remain incomplete.**

## Matched evidence and visible result

Root identifies this as the same earned RimForest `Overworld.2.3.0` fixture at turn 8240, player `(40,12)`, HP 29, with the same owners and positions. The screenshots visibly retain HP 29/40, player focus `(40,12)`, terrain, vegetation, solid forms and HUD placement. The reader focus cells are Singer `(17,8)`, Sentinel `(19,18)` and Sky-Sari `(63,16)`. The log advances between captures; these are matched scene views, not byte-identical entire frames. Owner IDs, turn and graph authority are root's capture context, not independently derivable from these pixels.

| Pair | Independent visual finding |
|---|---|
| 01 RimForest | The old `f`, `l` and `E` glyphs become three different compact bodies at their prior positions. The replacements sit on the existing floor without a new black tile rectangle, vertical stretching, displaced center or conspicuous footprint growth. The player, terrain and neighboring vegetation remain visually stable. |
| 02 Summit Singer | The squat frog silhouette and dark dorsal marking replace the letter in both world and reader. In the enlarged portrait, the folded dark W is legible against the olive-brown body; its interior detail is naturally much subtler at native world size. The outline still separates the body from the muted stone floor. This is useful species identification, not a claim that every marking can be read without examining it. |
| 03 Brocchinia Sentinel | The portrait shows a distinct raised tail, legs, head and bright yellow eye. Its body uses a foliage-related green-brown palette, but its asymmetric outline distinguishes it from the nearby round/heart-shaped bushes in this matched view. The eye is clear in the reader and a small highlight in the world; foliage-adjacent camouflage remains a scale/readability caveat. This view does not prove body readability while standing on a TankBrocchinia. |
| 04 Sky-Sari | Pale lifted wings, dark wing interiors and a narrow central body replace `E`. Both wing tips remain visible, including the full-width silhouette in the reader. The body is square and proportionate rather than stretched to fill a 16×24 cell. In the world, the pale wings and darker edge contrast with the floor more clearly than the two olive creatures. The visible body is a static bird pose, not evidence of flight animation or a larger physical footprint. |

All three portraits fit the existing reader space with no observed collision with the name, description, separators or controls. The square-source portraits display useful additional detail without implying a changed gameplay size. Actual relationship labels show **Hostile**; source default attitudes must not replace this observed state. Native portraits have authored palettes rather than the old flat glyph colors. That difference is expected for the native body and is not evidence of a lost status cue.

## Whole-view composition and limits

The wide RimForest view remains dominated by a broad, quiet stone field, small repeated tree groups, scattered low vegetation and a few restrained solid masses. The new creatures improve recognizability and reduce three conspicuous letter fallbacks. They do not materially increase environment density or repair the spacing of vegetation. Large open intervals are still prominent, and the remaining black-backed red glyph features and other uncovered glyph objects are conspicuous against the otherwise quiet native field. These are separate environmental coverage/composition debts; adding creature sprites alone does not make this viewport fully polished.

The scene is sufficiently readable for this bounded integration checkpoint: the three bodies are findable on the open floor, their portraits explain their identities, and no new obstruction or UI regression is visible. Do not count this as all Stump fauna/areas complete, full-chunk furnishing accepted, all actor-over-prop cases verified, or proof that the entire 80×25 chunk has appropriate density. The visible viewport is the evidence boundary; unseen or fogged cells are not evidence of bare terrain.

No changes to accepted source pixels are requested from this review. Any later composition work should address real remaining owner coverage and deliberate formation spacing, not add decorative objects merely to fill these screenshots. Damage, Wet cues, movement, reader authorization, optional-resource behavior, persistence, packaged execution, Linux/Deck performance and other biomes are outside this image-only verdict. Root's native test results are separate evidence and were not rerun here.

## Inspected capture identity

All paths are under `/Users/steven/caves-of-ooo/Docs/Verification/Live16x24/` and all are 1280×800.

| File | SHA-256 |
|---|---|
| stump-canopy-before-01-rimforest.png | `43832f1f87fd36d13995bc60c77579bc6112118f08596659386cd680a9626853` |
| stump-canopy-before-02-singer-reader.png | `b9164dd8bdafee4ab5211de61dcc2914852607871403e110e931324b1c61a400` |
| stump-canopy-before-03-sentinel-reader.png | `a2d69048305d384b2f379af17067f350ff74ae31abe469196ecad68321b3daee` |
| stump-canopy-before-04-sky-reader.png | `324f35a9fb03b3b62a797b4f58dc074ab17b4b03503984b3b13eb9fa2bff922e` |
| stump-canopy-after-01-rimforest.png | `8e26f67347a677acf8512c30d09d8219eabe2ba833061ae8ffd92ea925b55a3d` |
| stump-canopy-after-02-singer-reader.png | `8ec3dd379522121827510cc8f9869424b3c43781dde4efb38fddcbb9e3063b72` |
| stump-canopy-after-03-sentinel-reader.png | `1476b931a4d842a98b816a622241d6078a58188eddf5af46d7e121b865485300` |
| stump-canopy-after-04-sky-reader.png | `2f1afa67cbd889c0a6facf1218e228abf2e101a2af401d8ad230f5220c0848ab` |

## Additional travel, Tank and fresh-Continue review

Opened and visually inspected all seven additional originals, `stump-canopy-after-05-rim-west.png` through `11-fresh-continue.png`, and read `stump-canopy-after-receipt.json`. Independently recomputed all eleven after-capture SHA-256 hashes: every value matches the receipt, and every image is 1280×800.

- **05 west / 06 east:** The complementary normal-zoom views extend the evidence across the RimForest chunk's width. The moving Singer and Sky-Sari remain intact, compact native bodies at their new visible positions; the Sentinel also stays readable on open stone. These are different times after normal movement, not static before/after pixel comparisons. The wider evidence confirms the earlier composition debt rather than concealing it: low vegetation comes in small repeated groups, large open floor intervals remain, and uncovered glyph props/black-backed effects remain noticeable. Covering the width in multiple views is useful evidence; it is not an all-cell or all-state fullness audit.
- **07 Summit:** Several Singers and the Sentinel appear alongside the different Summit arrangement of large solid masses, a few trees and tank plants. Native identities remain distinct without a stretched body or black backing. This view is visibly sparser than RimForest; the central floor expanse remains especially plain, and the solid boundaries have restrained contrast. The fauna slice is not a remedy for those existing whole-scene composition issues.
- **08 Tank / 09 sheltered reader:** The Sentinel is still visibly a lizard at `(54,5)` after the reported normal retreat onto the real Tank. The reader identifies that same selected creature, shows HP 10/10 and Hostile, and lists both `tank-brocchinia` and `brocchinia-sentinel` in cell contents. Its portrait remains unchanged and clear. The receipt names the actual selected owner `67304` at layer 10 above Tank `65605` at layer 2, with the Well remaining on the Tank. This supports the intended selected-body priority: the animal does not disappear into a plant sprite. It does not show every underlying plant pixel simultaneously, nor prove all possible canopy/creature overlaps. It closes the earlier report's specific “not yet viewed on Tank” limitation for this actual instance.
- **10 saved / 11 fresh Continue:** Both show player `(50,2)`, HP 29/40, the visible Sentinel and the same three Singer positions around the surrounding terrain. Independently compared decoded RGB world rectangle `[0,40,940,640]`: **all 564,000 pixels are identical**. The sidebar/log is excluded from that comparison. This is strong visual continuity for this earned checkpoint, alongside the receipt's recorded same Sentinel ID, `(54,5)`, visibility and HP 10. Root's normal F5/fresh-session Continue process and turn 9040 are receipt evidence; this reviewer did not operate the application or reload the save.

**Final judgment remains a bounded integration pass for the three reused native species, their readers, this actual Tank priority case, travel into Summit and this fresh-Continue presentation.** No new material blocker or art revision is requested. The added views strengthen the placement/persistence evidence while confirming that sparse terrain composition, subtle solid contrast, uncovered props/crops and broader world coverage remain separate work. This is not a region-completion count, all-seed/area acceptance, or a claim of observed damage flashes, sustained performance or new packaged delivery. The receipt explicitly keeps its tool-interrupted stationary timing sample and package status limited; no performance conclusion is drawn here.
