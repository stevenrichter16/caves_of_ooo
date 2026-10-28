# Shared specialists — original editable native bodies

Source frozen for root's integration trial. No runtime PNG or metadata was published by this builder, and no prior art was changed. `scene-prompt-01.txt` and `scene-reference-01.png` remain byte-identical protected inputs.

| Frame | Owner | Native rectangle | Occupied bounds | Baseline |
|---|---|---|---|---|
| 0 | Tinker | 0,0,16,24 | x3–12, y3–22 | y22 |
| 1 | WellKeeper | 16,0,16,24 | x3–12, y3–22 | y22 |

Each body has 134 opaque pixels, binary alpha and an independent clothing identity. Atlas dimensions are32×24; requested runtime geometry is centered16PPU/FullRect with Point/Clamp, no mipmaps or compression. The import specification is advisory for root's publication; this packet creates no gameplay mechanics.

The builder authors named integer cells into268 editable unit quads across64 named part/color groups. Both fresh saved Blender files were reopened and checked against exact parts, palette emission values, integer geometry, frame instances and native dimensions. Native PNG, enlarged/native contacts and source-cell JSON rebuilt byte-identically in two fresh Blender processes. Saved `.blend` bytes differ, so binary Blend reproducibility is not claimed; both exact geometry proofs are retained. Reopened rendering changes only Blender's `File` text stamp from `<untitled>` to the saved path; every other PNG chunk, including image data, matches. Both PNGs are retained without rewriting away that provenance.

Verification decodes all768 atlas pixels and74,880 direct-contact pixels, including the unchanged approved Villager comparison, and rejects six deliberate corruptions: removed face, partial alpha, wrong apron color, opaque outside padding, swapped professions and wrong dimensions. Twenty exact input hashes and six normalized metadata hashes remained equal. Metadata tolerance is trailing whitespace only; the builder does not rewrite those inputs. `source-red.json` retains the initial missing-atlas failure before generation. Native Unity tests, actual gameplay appearance, performance and whole-area fullness are separate root-owned gates.

From the repository root, use the exact installed Blender path:

```sh
# One-time only in a new packet: asserts no prior protection map and records source RED.
python3 Tools/Live16x24/build_shared_specialists.py --snapshot
# Existing packet reproduction:
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shared_specialists.py -- --build
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shared-specialists/editable.blend --threads 4 --python Tools/Live16x24/build_shared_specialists.py -- --inspect-blend
python3 Tools/Live16x24/build_shared_specialists.py --verify
```

Do not rerun `--snapshot` over an existing packet. `verification/build-1/` retains the first render/source/Blend and its reopened proof; `verification/reproducibility.json` compares the two fresh builds. `frozen-handoff.json` binds the current source packet and builder. No `--publish` operation exists.

See `PARTS-AND-REFERENCE.md` for the concept critique and actual-play watch items. Contacts are direct geometry source studies, not game screenshots. Coverage is not polish acceptance.
