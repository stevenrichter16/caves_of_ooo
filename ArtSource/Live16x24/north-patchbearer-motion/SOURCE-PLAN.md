# North Patchbearer motion source

Status: original native source produced, decode-verified and approved by the
parent for an actual in-game trial,2026-10-27. Actual before05/06 captures and
the saved imagegen concept preceded final pose authoring. See `README.md` for
the completed source checks and exact handoff. No Unity import or gameplay
acceptance is claimed by this packet.

Runtime agreement with `inventory_stat_review`: one128×24 atlas, eight16×24
frames. Indices0–3 are full WalkA, WalkB, Attack, Hurt;4–7 are empty equivalents.
Resource: `Sprites/Live16x24/north-patchbearer-motion`. The renderer borrows
approved `north-works` idle7/8 unchanged for idle and recovery. It owns bottom
anchor(.5,0),16PPU,100ms walk,160ms attack,140ms hurt,40ms recovery. No new
self-use state, fixed weapon or portable-actor admission expansion.

Source atlas: `north-patchbearer-motion.png`; editable Blender source and
named integer-cell parts remain within this directory. The generator writes
only here, even if `--defer-unity` is omitted. Root owns capture/concept,
Assets copy, runtime source, native Unity tests and actual game acceptance.

Approved body geometry comes from `build_north_works.make_frames()[7:9]`,
which derives the accepted original Gleaner anatomy. Existing atlases and
their generators must remain byte-identical. Pose grammar may reuse named
source limb construction from `build_opening_motion.gleaner_frames`, with
Patchbearer straps/cradle restored and palette kept to its six existing colors.
The source will not read PNGs to construct poses, resize a concept, trace a
render, change the approved idle or substitute unstrapped Gleaner pixels.

Two direct contact scenes at1× and8× show full and empty rows. Each row is
approved idle, WalkA, WalkB, Attack, Hurt. A quiet existing green background
is contact-only. All native cells are exact square opaque quads or transparent;
rendered PNGs are decoded for comparison and never edited or reserialized.

Before geometry output, run `python3 .../verify.py` and retain the genuine
missing-output RED. GREEN must establish exact source/native/contact pixels,
all eight distinct derivatives, state pairs differing only at the real healing
slot, correct frame order, baseline/alpha/palette, semantic importer settings
and preservation of approved source. Inspect the contact visually and obtain
the parent's in-game-trial approval before handing off source hashes.

The anatomy, scope and timing do not authorize a bottle-specific use gesture:
the real generic self-use event may also mean an antidote. No such pose is in
this initial packet.
