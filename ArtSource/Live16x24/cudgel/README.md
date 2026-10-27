# Cudgel: native source packet

Current pass: source authoring and source verification complete; root owns runtime
integration and actual gameplay acceptance. No Unity resource was written by this
packet. Root's existing `concept.png`, prompt and `references.md` are unchanged.

## Extraction and visual decision

The real Examine capture identifies the exact Cudgel owner whose pale slash is
being replaced. The generated concept supplies a broad blunt head and a narrow
smooth diagonal grip. Its printed native-size claim, softened portrait, invented
title layout and inconsistent generated pixels are not production geometry.

The new original source uses integer cells authored directly in
`Tools/Live16x24/build_cudgel.py`: one connected wooden club with a blunt striking
end, tapered shoulder and short smooth grip. The lower-left to upper-right
diagonal matches the accepted equipment grammar. The 19-pixel height is shorter
than the approved 22-pixel LongSword. It has no metal head, wrapping, rivets,
spikes, cracks, glow, status decoration or motion frame. Four existing wood
colors make flat, restrained masses: `#342E25`, `#5F4830`, `#80603B`, `#9B7848`.

| Contract | Value |
| --- | --- |
| Exact owner/state | `Cudgel`, neutral item identity |
| Native frame | One 16×24 transparent PNG, binary alpha |
| Occupied shape | 74 pixels; top-left exclusive bounds `[2,3,15,22]` |
| Named parts | Striking head37, tapered shoulder16, smooth grip21 pixels |
| Intended resource | `Sprites/Live16x24/cudgel` |
| Import metadata | 16PPU, center pivot, Point, Clamp, no mipmaps, uncompressed |
| Current export boundary | Source packet only; root later copies PNG and metadata |

This sprite describes the real blueprint's stout RawWood club. Sound, damaged,
repaired, renamed, carried and equipped states retain one neutral identity;
simulation text, authoritative custody and reaction overlays remain responsible
for those meanings. Nothing in this packet establishes runtime admission.

## Editable production and reproduction

`native.blend` contains the native scene and both contact scenes. The three named
semantic parts are editable planar integer-cell mesh groups, with one quad per
native pixel. `source-cells.json` records every cell and its part/color. The
generator never loads, samples, traces, crops or resizes a raster. Native and
1×/8× contacts are direct Blender renders of the same original geometry.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_cudgel.py -- --defer-unity
python3 -B ArtSource/Live16x24/cudgel/verify.py
```

`--defer-unity` explicitly documents the active-Play constraint. Packet-only
export is also the safe default: there is no Assets-writing branch. The importer
metadata is created as `native.png.meta` within this packet; its GUID is retained
on rebuild. Root can copy these two files after its runtime RED and safe import
point. Only then may root optionally run the verifier with `--check-unity`.

## Verification and review

- Genuine RED was run before source/generator production existed. The saved
  `source-red.txt` reports the absent eight required packet artifacts.
- GREEN checks every rendered native pixel against its authored cell, every
  corresponding direct 1×/8× contact pixel, the four exact colors, binary alpha,
  connected silhouette, wide blunt head/narrow grip, shape bounds, manifest,
  packet metadata and18 preserved reference/source hashes. No mismatch.
- `blend-check.txt` records a separate successful reopen of the saved Blend,
  all74 editable quads and the three native/contact scenes.
- `first-build-hashes.json` and `rebuild-verification.json` record two independent
  Blender runs. Decoded native/contact pixels are identical; source cells and
  metadata bytes are identical. The native PNG file hash differs across runs;
  contacts are byte-identical. No image rewriting was used to force file-byte
  determinism. The manifest pins the final direct native render.
- Author inspected the actual native and8× contact: the broader striking end
  and smooth thin grip read as one blunt wooden implement. No extra detail or
  new variant was justified. Contacts are source inspection, not gameplay.
- Independent read-only review by `/root/inventory_stat_review` inspected the
  original native and both contacts plus generator, manifest and verifier:
  no visual or source/export blocker. The reviewer confirmed the broad blunt
  wooden head, narrow grip, quiet warm palette and packet-only export. They did
  not rerun tools or inspect the frame in actual gameplay.

Current decoded RGBA SHA256:
`ac0ad47893a4c84635e9a9e5b08efda4c6214547c242b4bcebd3b1c4a2527637`.
Current final native file SHA256 is in `manifest.json` and `verification.json`.

Root must still verify exact-owner mapping, real world/reader readability,
occlusion, pickup/equip/drop and continued state authority. This source check
does not prove Unity import, ordinary input, full scene consistency, Linux
execution, Steam Deck performance or player acceptance.

## Unity metadata normalization correction

Root copied the frame and authored metadata into Assets and imported it. The
optional runtime-copy check originally compared metadata bytes; Unity appended
default fields and trailing whitespace while retaining every authored value.
`unity-metadata-red.txt` preserves that actual failed assertion. The source PNG
still matched the runtime PNG exactly.

`metadata_contract.py` now parses both metadata documents safely, using PyYAML
when installed or Ruby/Psych otherwise. The current machine has no PyYAML;
Ruby/Psych3.1.0 was used. It requires all69 authored mapping keys in their original
hierarchy, all51 scalar values,10 lists with unchanged length/order, and11
explicit empty containers. Additional mapping defaults and YAML whitespace are
accepted; changed GUID/spriteID, geometry, platform overrides, import values or
missing authored fields are rejected. No Assets file is modified.

`metadata-contract-red.txt` records the initial missing comparator before its
implementation. `metadata-contract-green.txt` records8 passing test groups,
including separate mutations/removals of every authored scalar/key, populated
empty geometry, additional platform entries, and boolean/numeric confusion.
`unity-metadata-green.txt` and current `verification.json` record the passing
actual imported-file comparison. Re-run with:

```sh
python3 -B ArtSource/Live16x24/cudgel/metadata_contract_tests.py
python3 -B ArtSource/Live16x24/cudgel/verify.py --check-unity
```

These receipts establish source/runtime bytes and the authored import contract.
Root's parent living document records runtime tests and gameplay acceptance.
