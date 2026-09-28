# Original Spread field gate — private source candidate

Status: two original static source models authored; no shared files, Unity import, renderer binding or gameplay change. Source validation is8/8 GREEN after6 absent-source failures/1 palette control, then1 distinct-color coplanar-face failure/7 controls. Native library/recipe RED and actual camera acceptance remain future gates.

## Frozen identity and visible state

- Gameplay owner: exact `SpreadFieldGate : VillageDoor`, authored by root/combat. Keep existing DoorPart/Physics contract; VisualID, VisualVariant and GlyphVariants empty. Actual glyph is`+` closed or`/` open. QuarterTurns0–3 comes from the generated aperture axis.
- `spread-field-gate-closed`: two short timber posts with endgrain caps and three horizontal rails. Unequal post heights(.77/.73 beforecaps) distinguish a small field boundary from a monumental stone doorway.
- `spread-field-gate-open`: identical posts, hinges and materials; the same leaf rotated90degrees around its actual source hinge to lie along the left edge. The centre has a .54-unit wide visual corridor through the whole cell. This is a state swap, not a swinging-animation promise.
- One-cell envelope: all geometry stays within horizontal±.49 and height≤.85, including every quarter-turn.13 cuboids/156 triangles per state; one approved palette material; no rig, clips, sockets, emissive lights or collision components.
- Damage remains source-owned structural durability10; a partly damaged gate must still use its current door state model. Destruction removes the owner and leaves the true gap. No rubble/remains/reward/drop asset.

## Source and review ownership

`build_source.py` emits only an explicit requested kit path. `export_source.py` uses the existing `ArtSource/SpawnRing3D/mesh_kit.py` primitives/export helper read-only, with exact24-swatch UVs from `ArtSource/ReferenceGlade3D/kit.json`. Original geometry is in this pack, not renamed VillageDoor art. Export output is limited to the explicitly supplied private directory.

Three source images show closed(left) and open(right): isolated forms, front hedge context, and oblique hedge context. Hedge geometry is an unchanged source copy of `spread-environment-hedge-0`, instantiated solely in the review scene and never exported into either gate. Lighting/camera are Blender source-review approximations. These frames cannot establish native pixels, directional map readability, menu costs, current collisions, saved state or player discovery.

Self-review found overlapping same-plane differently colored rail/upright faces. A failing source counter preceded extending rails .65→.66 and hinges .054→.06; exact same-leaf transform, gap and cell bounds still pass. `kit-before-face-repair.json`/`face-red.log` preserve that bounded correction. A separate first fixture selector mistakenly counted post-face details as posts; narrowed exact two post names before final source verification. No geometry requirement was weakened.

## Minimal future adoption plan — not yet implemented

Use one optional two-entry library (`SpreadFieldGate3D/Library` proposed) borrowing the exact approved glade material. Import only these two FBXs and generate one static mesh/prefab per state plus library metadata. Preserve all unrelated source files/resources. Persistent outputs are inert Transform/MeshFilter/MeshRenderer only; validate vertex buffers, indices, UVs, normal/bounds integrity, exact materials, source checksum and no extra runtime/physics components.

Add an explicit current Spread-owner recipe that only admits exact `SpreadFieldGate`, real matching DoorPart/Physics/Render backlinks, actual zone/cell membership and visibility, no carried/equipped state, no custom art/footprint/scene-owned barrier parts, nonTakeable/Physics.Solid=false/no Solid tag, matching current glyph and valid quarter-turns. Preserve prior named refusals; the new exact child’s currently unmodeled result is the intended repair. Reuse established pose/renderer paths, independent stateful views and all-piece style evidence. No position hash, generic door alias or cross-biome style claim.

Before any production binding, author native paired tests against the real new child after root’s content publication: actual Open/Close state+quarter-turns and same owner; ordinary VillageDoor stays unchanged; damaged-but-live still rendered; hidden/removed/foreign/stale/custom/malformed owners remain refused. Pin exact imported mesh/material and native current state after save replacement graph. Structural removal/save tests belong to combat’s source slice; reuse them rather than duplicating gameplay.

Then root imports and captures a small actual generated field aperture from both approach directions, closed/open/broken gap, actual existing hedge and preserved bypass. Verify a visible open passage at all authored directions without deleting adjacent crop/hedge or changing camera/palette. An original source gallery is not sufficient to call this player-facing acceptance complete.

## Current boundaries

No animation, lock, ownership/key system, extra loot, stronger door, hedge opacity, rubble, general village redesign or new physics footprint is included. Normal DoorPart currently says"open door"/"close door"; renaming UI language to"gate" would be a separate source/readability decision, not hidden in this art slice. Root reviews this source direction before import. Beam identity tests remain a separate tranche.
