# Annex source art review

The five added voxel forms are a broken-open service gate, recovery cabinet,
maintenance rack, inspection slab, and an annex placard with a two-chamber diagram.
Existing healthy gate states are reused after the real timber repair and for the
other two ordinary gates. The existing fifteen model definitions and the approved
24-cell palette remain semantically unchanged; see `source-preservation.json`.

The source test suite was observed RED (three failures among five tests) before
adding geometry, then GREEN (five of five). New native tests were separately
observed RED before production selection changes; root retains that Unity report.

`curation-annex-oblique.png` and `curation-annex-top.png` render the exact five
new cuboid definitions in an isolated Blender process. The oblique image was
inspected: unequal broken posts and the fallen leaf read as a damaged passable
frame; the slab's restraints, the cabinet's latched face, the rack's materials card,
and the placard's linked chamber diagram remain distinct. Models fit one native
cell and contain no collision, script, rigid body, or animator behavior.

These source renders verify silhouette and authored material placement, not the
Unity camera, in-game lighting, discovery or subjective readability at game scale.
The rack and cabinet are static furniture forms, as with existing containers;
actual finite supplies and depletion are determined by their native contents and
inspection UI, not a rendered stock count. The gate's visible broken/healthy and
open/closed states do observe actual repair and door state.

Native import: `Caves Of Ooo/Art/Build Curation Annex Models`.
Native art fixture: `CavesOfOoo.Tests.CurationAnnexArtTests` (15 cases).
