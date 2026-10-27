# North Patchbearer motion derivatives

Status: source, runtime import and bounded ordinary Editor encounter accepted,
2026-10-27. Eight original native motion frames extend the existing Patchbearer.
Actual full/empty walk, attack, hurt, recovery, death, return and save evidence
is recorded in Docs/GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md. Packaged playback
and whole-scene acceptance remain separate gates.

The approved full/empty idle pair stays in `north-works.png` at7/8. This
packet never overwrites that atlas or any previous generator. The original
body scale, blunt face, tall shale ridge, patched linen harness and healing
cradle remain the identity anchors. Full and empty derivatives differ at
exactly the eight existing bottle cells, with both body and harness moving
together for Hurt. An empty healing slot does not mean Antidote or other
equipment is absent.

## Reproduce and inspect

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_north_patchbearer_motion.py -- --defer-unity
python3 ArtSource/Live16x24/north-patchbearer-motion/verify.py
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/north-patchbearer-motion/native.blend --python ArtSource/Live16x24/north-patchbearer-motion/inspect_blend.py
```

The generator always writes only this source packet, even if the explicit
`--defer-unity` flag is omitted. It reads the original named geometry from
`build_north_works.make_frames()[7:9]` and uses the established native Blender
plane helpers. No raster is sampled, traced, resized, edited or reserialized.
PNG decoding is used only for verification. `save_version=0` prevents Blender
backup files; none were produced during these two builds.

- `native.blend`: eight derivative collections with named editable integer
  quads, the two read-only borrowed idle collections, and three render scenes.
- `north-patchbearer-motion.png`:128×24 atlas, eight16×24 frames.
- `north-patchbearer-motion.png.meta`: fresh GUID, Point, Clamp,16PPU,
  no mipmaps/compression/physics outline. Runtime slicing supplies actor
  pivot(.5,0); the texture's source single-sprite pivot is centered as in
  existing atlas metadata.
- `source-cells.json`: exact top-left-origin cells, part and palette names.
- `manifest.json`: source/resource/order/pivot/reference hashes and limits.
- `contact-native.png`: direct128×64 render, full row above empty row.
- `contact.png`: direct1024×512 render of the same geometry at8×. Neither
  contact is a raster enlargement. Columns: approved idle, WalkA, WalkB,
  Attack, Hurt. Dark green background exists only in these contact scenes.
- `verify.py`: decode-only pixel, state, preservation and semantic metadata
  checks. Optional `--check-unity` reads the parent's eventual imported copy;
  it does not import or modify it. Metadata comparison reuses the existing
  reviewed Cudgel contract rather than a new YAML framework.
- `inspect_blend.py`: opens the saved source read-only, checks named geometry
  and instancing, and writes `blend-inspection.json` without resaving Blender.

## Runtime handoff

Resource: `Sprites/Live16x24/north-patchbearer-motion`.
Blueprint: exact `MarlbackPatchbearer`, with current owned-part/appearance,
local-scene, top-owner, visibility and custody checks performed by runtime.
This packet grants no admission or discovery authority.

| Index | Healing slot | Pose | Motion intent |
| --- | --- | --- | --- |
|0|Full|WalkA|Rearward hind contact, near claw under body, far claw forward.|
|1|Full|WalkB|Hind foot inward, near claw forward, opposite wrist folded back.|
|2|Full|Attack|Short leading grasp beside the blunt face, other claw braced.|
|3|Full|Hurt|Torso, straps and cradle move one cell back/down; support bends.|
|4|Empty|WalkA|Same pose as0; only healing bottle absent.|
|5|Empty|WalkB|Same pose as1; only healing bottle absent.|
|6|Empty|Attack|Same pose as2; only healing bottle absent.|
|7|Empty|Hurt|Same pose as3; only healing bottle absent.|

Idle and recovery borrow approved North works7(full)/8(empty), never a copied
new idle. The agreed frame player retains100ms walk,160ms attack,140ms hurt
and40ms recovery. Canonical poses face east; west flips and north/south retain
the last horizontal orientation, matching the existing compact motion policy.
There is no new directional row, permanent weapon, death pose, guard pose or
self-use pose. The real generic SelfUse event also covers other medicine, so
it is not a bottle-specific drinking contract.

The parent must preserve motion/reaction state when inventory changes the
asset's full/empty view, keep bounded current-owner reconciliation and retain
static approved art if this atlas is missing. The four globally portable
actor identities remain unchanged. See
[the encounter plan](../../../Docs/GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md).

## Actual reference and concept decisions

Actual baseline inputs are
`Docs/Verification/Live16x24/north-motion-before-06-zoomed-empty.png` and
`north-motion-before-05-examine.png`. They show the real seed224401440 North
encounter reached by ordinary travel. The empty healing cradle remains
consistent between the world and reader; the reader correctly reports a
remaining Antidote. These captures are evidence for the pre-change state,
not proof of improved motion.

`imagegen-prompt.txt` and `imagegen-motion-concept.png` are the parent's exact
design prompt/output. Accepted: paired full/empty identity, alternating foot
contacts, a forward equipment-neutral grasp and compact hurt recoil. Rejected:
extra illustrative detailing, oversized bottle, shaded background, added
equipment and the concept's larger compressed-height change. Only the hurt
body shifts one native cell back/down; all feet retain row22 baseline and
row23 stays transparent. Walks and attack keep the approved upper profile.

Final geometry was authored after that actual-capture/concept review. The
generator copies original named empty-idle geometry in memory, replaces
specific limb cells, moves the complete hurt torso/harness consistently, and
adds the original eight medicine cells at the corresponding cradle position
for the full variant. It never changes the idle source or samples the concept.
Existing Gleaner motion grammar informed the limb construction; no unstrapped
Gleaner runtime art substitutes for the Patchbearer.

## Verification and review

`source-red.txt` records the genuine missing-output failure before any pose
output. `source-green.txt` / `verification.json` record the final pass:

- Exact128×24 atlas, eight distinct derivatives, native16×24 cells and binary
  alpha. Full states use six existing colors; empty states use four.
- Zero source/native and both contact/native pixel mismatches.
- Exactly eight healing-only changed cells for each full/empty pose pair.
  The body, hands, straps and remaining inventory meaning are identical.
- Every pose differs from its approved idle, retains row22 baseline, has a
  transparent last row, and includes the patched harness and cradle.
- All163 captured approved art/generator/input hashes remain unchanged.
- Semantic import contract covers69 mapping keys,51 scalar values,10 lists
  and11 explicit empty containers. Parent runtime copy passes `verify.py --check-unity`.
- Saved Blender re-open confirms eight named frames,1,260 derivative integer
  quads, three orthographic scenes and correct native/contact instancing.
- Two independent background builds produce identical decoded native and
  contact pixels. Contacts, metadata and source-cell files are byte-identical.
  Native PNG file hashes differ between renders despite equal decoded pixels;
  no metadata stripping was performed. `build-pass1-hashes.json` and
  `build-pass2-hashes.json` retain both results.

The source author inspected native and8× contacts. The parent independently
inspected both and approved this restrained leg cycle, short claw strike and
compact hurt pose for an in-game trial. No pose or idle changes were requested.
An independent agent also inspected both original contacts and found no
material identity/palette issue or art change needed. It explicitly retained
ordinary time-resolved1×/2× playback as the acceptance gate because native
motion is deliberately subtle. This approval is source review, not a
substitute for actual game motion.

Final handoff PNG SHA256:
`af7f5c2a2429bf75dd768bd0eb612ccc6db6e847d373f6e3ab4545a5af330d0c`.
Decoded RGBA SHA256:
`68014a91deee81517e22bc5edcb8328053d03e3e13049eb96364c16c678853ea`.
Metadata SHA256:
`d108ccd0e03a048227ebd2ee9ddc31e22fc1c91f6f50dba9fa9c0c86cf745ede`.
Metadata GUID:`05421f7ef4b64bfba2b5c3e41ece7333`.

Can verify here: original editable geometry, rendered native pixels,
state-pair differences, reproducible decoded output, metadata and preservation
of approved references. Cannot verify here: actual Unity import, owner/state
selection, visibility, reaction timing, encounter feel, package parity or
physical Steam Deck performance. No Assets files, Unity state or Git state
were modified by this source task.

Root integration acceptance: see the encounter plan and `north-motion-editor-play-summary.json`;278native passes,1allocation-counter skip. Source-task limits above describe source-author work; root separately imported and playtested the actual unchanged-checkpoint encounter. Package/Deck/whole-opening gates remain open.
