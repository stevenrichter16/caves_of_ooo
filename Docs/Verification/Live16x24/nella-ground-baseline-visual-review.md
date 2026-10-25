# Nella ground — independent baseline visual review

2026-10-25 UTC. Read-only review; no Unity, asset, runtime or test changes. **Accept reuse of existing frames 18/19/20 as a bounded candidate, pending the actual after views. This is not a whole-scene or release approval.**

## Evidence inspected

Viewed the actual 1280×800 `Docs/Verification/Live16x24/prepared-bed-restored-focus.png`, its owner/state JSON, the accepted ground contact/context, and the Nella imagegen concept. Read `Docs/GRAPHICS-G3-NELLA-GROUND.md`, ground README/source geometry and current ground selector, plus the reserve-cord source contract.

The actual capture shows the selected cell (18,6) as another brown, stubbled field square. The FOCUS readout correctly says prepared planting bed, but the picture offers little soil-condition distinction from the surrounding old rows. The receipt identifies cultivated Grass 5524 beneath CropRow 5525 with no crop, versus adjacent cultivated Grass 5523 beneath MarlrootCrop 2714, stage 0/moisture 0. This supports changing ground material only; it does not support relocating crops or inventing a new permission marker.

## Existing-pixel assessment

- **Frame 18, old earth:** small irregular, mostly horizontal flecks remain quiet. Reusing this below the real cut/ripe row silhouettes would give the broad brown fragments some material without making them look like recently prepared beds. Avoid the concept's raised-looking, high-contrast clumps: those strengthen the already dominant field fragments and compete with the tiny keeper/crop grouping.
- **Frame 19, dry prepared soil:** two interrupted vertical furrows form a deliberate pattern distinct from frame 18's scattered flecks. This is the appropriate visual cue for an empty bed that can be replanted. It contains no planted-seed or mature-yield claim. Its base is exactly the same `#45341F` as old earth, so the distinction depends on narrow pattern geometry, not a different fill color. At 16×24 the one-pixel furrows and sparse ridges may be subtle beneath existing stubble, cords or crop silhouettes. That is an actual-scale acceptance risk, not yet a demonstrated defect.
- **Frame 20, wet prepared soil:** it preserves frame 19's exact geometry and darkens every palette channel (`#252319` base versus dry `#45341F`). That is a coherent moisture cue and introduces no false standing-water/depth claim. In the enlarged study it is substantially darker and lower contrast than dry soil, close in overall darkness to the green world. The live view must establish that the ordered furrows remain recognizable and do not read as an undisclosed/blank patch. Do not infer wet empty-bed persistence: current gameplay stores moisture on the current crop.
- **Hierarchy:** the accepted keeper, plants, neutral reserve twine and tray should remain the stronger identity/ownership cues. Soil should explain preparation and current moisture only. There is no visual justification here for another sprite packet, brighter outlines, new props, changing the keeper, or widening the reserve enclosure.

## Required actual-view acceptance

1. At normal 1280×800 and native 1× view, compare a selected and an unselected empty prepared reserve bed with an adjacent old row. The ordered soil should survive without depending on the white selection outline or only on zoom. A close 2× view is useful diagnosis, not a substitute for the native-scale check.
2. Show the same genuine prepared bed with a planted crop before and after ordinary Rain. Confirm the dark frame still reads as worked soil and the unchanged crop/correct neutral cord remain readable. Keep foreground stubble and crop visibility truthful; do not erase owners for a cleaner screenshot.
3. Show the harvested/picked-up empty bed returning to dry prepared ground, plus leave/return and save/load. These are integration/state gates; static frame review cannot establish them.
4. Retain the four public crops' unprepared state. Neither a prepared appearance nor a brighter cord should imply permission to harvest. Roads, hazards, fog/remembered cells and unrelated zones must retain their prior meanings.

## Bounds and remaining weakness

The proposed material pass directly addresses the missing prepared-bed distinction. It will not make the repeated field-fragment layout or the small service grouping into a complete scene by itself; these are existing composition limits, not reasons to add unrelated objects during this pass. Source contact/context images are studies without fog, actor overlap or live tint; the imagegen concept is guidance only. There is no current measured pixel/GPU regression, actual wet transition proof, frame-time result or physical Deck result from this review.

No material source-art blocker warrants editing the accepted pixels before the after capture. If the native after view fails the dry-furrow distinction or loses the wet bed into the dark ground, report that exact failure before considering a narrowly scoped existing-frame revision.

## Inspected image hashes

- `Docs/Verification/Live16x24/prepared-bed-restored-focus.png`: `0c692c6b56025296a62ef3e1e98bdbcfbd01d6cc91c7cf5d4255fa2e552d5b13`
- `ArtSource/Live16x24/Ground/opening-ground-contact.png`: `48453dc276ea6a244b4c03becd1b72e69679ea596a486907ca81db1c8c4d7671`
- `ArtSource/Live16x24/Ground/opening-ground-context.png`: `acd4e884807e766bb518bafa2717eaef375fb782e23830ef76cde1baa32152dd`
- `ArtSource/Live16x24/concepts/nella-ground/concept.png`: `84965db4e9321570e6776bee46ea6faed06ffe9e4041fe2ef07a96237cd70b92`
- `Assets/Resources/Sprites/Live16x24/opening-ground.png`: `b35f9d6a4b7051eb6971a00195a069f58d08a7d89e3060533745ee899a66dede`
