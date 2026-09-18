# Morrowfast component visual review

Reviewed the final rebuilt artwork on 2026-09-06. **No material extraction residue or hidden-surface failure remains in the inspected views.** This is an art-package review, not native Unity verification.

The review compared the approved reference with `build/all-removed.png`, inspected 17 representative individual removal/reveal crops, and revisited the corrected upper-stall crop after the final rebuild. Enlarged source comparisons were used to diagnose narrow contours. The package provides 66 individual inspection crops; this review does not claim a separate enlarged inspection of all 66.

| Area checked | Final observation |
| --- | --- |
| Upper provision stall | Both thin side-line residues and the under-counter basket edges are removed. The final western-post correction was rechecked in its individual crop and the full all-removed image. Neighboring barrel and basket remain when only the stall is lifted. |
| Violet food stall | Removed counter and basket bottoms leave credible ground. Its separate side barrel, bucket and vendor remain independently visible. |
| Guesthouse flower box and Long Loop worktable | Previously omitted planter feet are gone. Repaired ground no longer receives the old dark object-shadow color transfer. Nearby wall details and the worktable's neighboring barrel are retained. |
| Entrance arch | The fourth hanging token and its diagonal cord no longer leave a curved fragment on the road. Removing the eastern guard separately reveals a completed gatepost. |
| Cistern | The former hard dark circular paving patch is gone. The replacement cobbles and ground blend into the surrounding road network at scene scale. |
| Overlapping outdoor objects | Removing the provisioner's rope basket completes the surviving barrel wall; removing food-stall side containers completes the counter supports. Removing the kindling crate reveals the stove edge. Removing the stove reveals the containers behind it, while the independent crate remains. These are the correct surviving materials rather than bare ground substituted inside wood or stone. |
| Interior furniture | Guesthouse warming-stove and kitchen-hearth removals leave continuous wooden flooring. The southern guest stool and reserve rope reveal clean flooring at their revised placements. |
| Roof-hidden rooms | Guesthouse and kitchen reveal readable furniture arrangements, coherent floors, masonry rims and usable-looking original door positions. The full all-removed view retains the five intentional wall shells over empty rooms. |

The recorded package verification reports 71 semantic owners, 145 RGBA layers and zero differing pixels across the 1,572,864-pixel intact reference. The existing final logs record 9 Python extraction-contract tests and 25 Node preview-contract tests passing. These numerical checks support source integrity and preview behavior; they are separate from the visual observations above.

## Remaining art and implementation limits

- The source is a high overhead RPG illustration with shallow front facades. Roof removal uses an authored cutaway convention; it is not free-camera 3D geometry.
- Hidden ground, room surfaces and concealed neighboring fragments are newly inferred artwork. Their provenance is distinct from the unchanged visible source pixels.
- Residents and creatures are single source poses. Arbitrary relocation, directional animation and runtime shadows need further asset or renderer work. Original contact collars belong to the original ground placement.
- The five wall shells, perimeter fences, rock masses and indistinct environmental growth remain fixed. Stall stock displays and arch ornaments are grouped under their semantic owners rather than falsely presented as individual inventory items.
- Dialogue and stock cards are design previews. Commerce, quests, services, native AI, Unity collision, persistence and the actual inter-zone player route remain the separate Unity handoff.

## Reviewed final artifact fingerprints

SHA-256 values bind this review to the final rebuilt images and authoring, after the western-post correction:

```text
authoring/outdoor-objects.json
9499f6554dc11e02eb66fe25b6ac1fe6033ecd267b4e03ccf282ea1dc4529667
build/manifest.json
36d3105c8d3a3599453ac9e85c59735aa4f30df3e00304041539fd8f35e3162e
build/all-removed.png
2596998dbba35f24c868075bdf91ee48968d8e6698278e16c9a65dd79e58195a
build/review/removals/provisioners-stall.png
e71ca1935ca0433a69988ad0abf30402f705654a3b8a4e0759ace2b20dc7ab1e
```
