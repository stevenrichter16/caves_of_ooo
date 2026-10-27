# Stump first native trial — independent composition review

Status: **five formation views reviewed; stone palette correction required before visual acceptance.** This is a review of actual Editor gameplay captures, not a claim that the full region, native test suite, packaged build or physical Steam Deck has passed. The target is CoO-original flat native 16×24 art; no Qud parity is claimed.

## Evidence and limits

The reviewer visually inspected these exact 1280×800 captures supplied by the root gameplay owner:

| Capture | Actual view | Finding |
|---|---|---|
| [01 RimForest](stump-after-01-rim-forest-trial.png) | Native stone floor, dome groups, borrowed trees/bushes, Tank rosettes, remaining glyph actors and hazards | StoneDome bodies barely separate from the floor. The prominent patch around screen x478–558/y520–592 reads as a floor patch rather than solid rock. |
| [05 SummitScrub](stump-after-05-summit-scrub-trial.png) | Open route between large dome islands and sparse trees/Tanks | Strongest fullness question. Dome islands around x526–670/y136–304 and x222–366/y448–568 are too faint, leaving the route visually unstructured despite real placed masses. |
| [06 ButtressRidge](stump-after-06-buttress-ridge-trial.png) | Long rock fingers, clear existing gaps, vein clusters and remaining glyph pockets | Ridge shapes provide useful composition, but their current value treatment can read as dark floor stains rather than solid bodies. |
| [07 Grainfield](stump-after-07-grainfield-trial.png) | Broad horizontal ridges, vegetation groups, mineral clusters and intervening routes | Existing bands and gaps are legible. Strengthen the solid-body distinction without making every cell edge a bright stripe. |
| [08 CascadeGorge](stump-after-08-cascade-gorge-trial.png) | Green wet areas, dry necks, wall boundaries and visible actors | Water/dry separation is clear. Stone walls still belong to the weak floor/body value hierarchy. The large dark lower field is undisclosed/fog space, not evidence of an empty chunk. |

Additional same-area service captures were inspected: [02 Tank menu](stump-after-02-tank-menu-trial.png), [03 Tank reader](stump-after-03-tank-reader-trial.png), and [04 Tank drink](stump-after-04-tank-drink-trial.png). They show a coherent native rosette, the draw-water option, native reader portrait and the displayed successful drink message. They are **not** three additional formations or independent proof of every service state. No further Tank visual defect was identified.

These are centered gameplay viewports, approximately 59 columns of an 80×25 zone, not complete zone panoramas. Occluded, offscreen and unexplored cells are outside the visual judgment. The five views span all ordinary formation types but do not constitute acceptance of all 18 eligible ordinary areas, special sites, underground areas, different times/lighting or all essential states. Root operated ordinary gameplay; this reviewer did not operate Unity or replay the inputs independently.

## Named defects and bounded correction

- **🟡 Solid StoneDome versus walkable floor:** in RimForest and especially SummitScrub, faint rectangular bodies conceal the collision distinction. Correct the **new** floor/dome/ridge/wall value hierarchy under the existing Cave grading. Calm floor interiors plus a restrained solid-body/edge distinction should make contiguous masses readable.
- **🟡 Ridge/body material clarity:** the actual Buttress and Grainfield arrangements are useful, but dark strips can be mistaken for floor shading. Preserve the formation silhouettes and gaps. Bright borders around every cell or high-frequency variant noise would create an equally harmful tiled stripe effect.
- **🧪 Whole-view fullness remains open:** SummitScrub warrants another full-view assessment after the material correction. The present weak dome contrast hides existing composition, so these captures do not yet justify adding density, decorative spawns or changing generation.
- **⚪ Separate coverage gaps remain:** actor glyphs, crop/stair/prop pockets and black hazard/coating cells remain visible. Green actor glyphs are particularly weak against Gorge water. Preserve their current semantic cues; do not hide them to make a screenshot look finished. Further native actor/prop/state coverage is separate production work.

There is no identified defect in these views that warrants repainting the accepted borrowed trees/bushes, changing water/Tank/mineral-seam frames, altering biome grading, or changing mechanics/generation. Root's proposed correction is limited to the newly authored stone palette, followed by matched actual gameplay comparison.

## Acceptance still required

Replay the five formation views with the corrected new atlas. Check solid versus walkable readability, continuous mass interiors, preservation of narrow routes and water/foreground distinction. Reassess SummitScrub composition only after its domes read clearly. Preserve the actual footprint, owner selection, collision, hazards and service/harvest state throughout. Successful native tests alone will not resolve these visible findings.

## Review record

The review found a bounded material-readability defect, not a demonstrated generation-density defect. No runtime, art, test, biome-grading, source placement or shared living-record changes were made by this review. The only file authored is this review record. Screenshots are historical first-trial evidence and should remain unchanged after the palette correction.

## Source-only recommendation for the next finite kit

After the stone correction is verified, prioritize **nine exact fauna bodies**, with one readable static native identity each and no new nuanced animation. The five-area [baseline census](stump-before-owner-census.json) contains 18 owners across these identities: SummitSinger 5; GlasspaneFrog, Wardline, SariSnake, MawToad and BrocchiniaSentinel 2 each; CascadeFather, YellowfootWayfarer and SkySari 1 each. These are whole-census counts, not counts of animals individually identified onscreen by this review.

| Group | Actual ownership / potential reuse | Source anchors |
|---|---|---|
| Gorge frogs | CascadeFather and GlasspaneFrog already have separate legacy 64×384 actor sheets containing 16×24 cells. Evaluate a quiet existing idle cell unchanged before drawing replacements. Distinguish the pale-striped father carrying young from the translucent green frog; matching glyph `f` is insufficient identity. | `Objects.json:35826,35860`; `EntityVisuals.json:235,250`; `EntityVisualCatalog.cs:43–44,146–175` |
| Slopes and shared fauna | SariSnake, Wardline and YellowfootWayfarer also have existing 64×384 / 16×24-cell actor sheets. Preserve the heavy pit viper versus olive colubrid versus footstool-sized tortoise silhouettes. MawToad currently has a legacy 16×16 environment sprite and needs native-cell/quality review. | `Objects.json:35733,35790,35894,34617`; `EntityVisuals.json:205,220,265`; `EnvironmentSpriteRenderer.cs:183–188` |
| Summit fauna | SummitSinger, BrocchiniaSentinel and SkySari have existing legacy 16×16 environment sprites, not an accepted flat16×24 delivery. Evaluate the brown W-backed frog, lichen lizard with yellow eye and pale eagle as three distinct bodies. | `Objects.json:36519,36550,36582`; `EnvironmentSpriteRenderer.cs:190–192` |

All blueprint anchors refer to `Assets/Resources/Content/Blueprints/Objects.json`; visual-registry anchors to `Assets/Resources/Content/Data/EntityVisuals.json`; catalog/renderer anchors to `Assets/Scripts/Presentation/Rendering/`. The five legacy sheet files are under `Assets/Resources/Sprites/Actors/`; the four 16×16 files are under `Assets/Resources/Sprites/Environment/`. Dimensions were read from the actual PNG headers and the images were inspected. Legacy source existence is **not** accepted flat-world integration: these nine identities are not named in the current `FlatPixel*` native catalog family. `FlatPixelActorMotionCatalog.cs:61–74` only admits the existing four portable opening actor identities. Existing art must remain intact; any padding/extraction or redraw remains a future reviewed production decision.

This is a useful shared family beyond Stump: actual source places MawToad in Sodden populations and the ordinary copse (`PopulationTable.cs:590,605`; `SoddenCompositionPlan.cs:187`), while Felling and Morrowfast explicitly place GlasspaneFrog and YellowfootWayfarer (`FellingScenePopulation.cs:40–42`; `MorrowfastSceneRuntime.cs:178`). Treat exact native fauna identity as portable when current owner/graph/disclosure authority permits; do not globally admit unrelated regional art. A nine-owner slice still leaves other biome actors, NPCs, props, crops, stairs, hazards and special sites open.

High-value preimplementation and acceptance checks for that future slice:

- Start from the real five formation captures and named glyph owners; obtain a close current-owner/examine baseline where necessary. Compare existing candidate idle cells under the actual stone and water grading before deciding which need new native source. No bulk redraw assumption follows from fallback use.
- Scope by exact blueprint, canonical visual identity and owned Render/Physics/Body/Brain semantics. Preserve one-cell registration, current selected owner, disclosure, reader authority and resource-failure fallback. Test same-glyph identities and explicit reskins, stale/custodied owners, hidden/remembered cells, noncurrent/multicell footprints and actor removal; do not reuse the real multicell MawToad pilot as a one-cell proof.
- Habitat and ecology flags are **placement rules**, not repaint-time visibility rules. `StumpFaunaHabitat.cs:12–15` places CascadeFather on SprayPool, Sentinel near Tank and Singer near Tree. `PopulationTable.cs:236,260,272,287,292` gates initial populations on EcologyDamaged/UrquActive. Existing actors can move, and cached owners must not disappear merely because a habitat or spawn flag differs later.
- Brocchinia retreat makes ordinary moves toward a Tank; it does not toggle invisibility (`AIBromeliadRetreatPart.cs:24–31,72–79`). Keep the actor above the actual plant/water ground when selected. Retain real damage/status/hazard cues and allow neutral body identity through ordinary health or attitude changes instead of requiring pristine default stats.
- Use the same accepted body in world and authorized examine. Verify normal movement, cover/uncover, a status/damage response, save/revisit and cross-area identity with the root-owned playable pipeline. Defer extra poses and sound work. Do not claim fullness closure from nine newly resolved glyphs.

This recommendation authors no runtime, sprites or tests and changes no generation, habitat, hostility, combat or save behavior.
