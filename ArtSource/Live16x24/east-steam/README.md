# East steam tile-state cue

Source pass complete and approved by root for an in-game trial. Actual runtime
admission, import, priority and gameplay acceptance remain root-owned. This
packet writes no Assets files and does not modify the approved vent body.

Root's `concept.png`, `prompt.txt` and `references.json` are unchanged. The actual
scene input is `Docs/Verification/Live16x24/east-steam-02-before-world-2x.png`.
The concept's narrow muted rising curl and negative space inform the design.
Reject its sixth invented wisp over the tonic, grate bevel/repaint, ground,
flower, actor and UI drift, and inconsistent enlarged pixels. No generated
raster pixels were traced, sampled, cropped, resized or cleaned for production.

## Source contract and meaning

One original sparse asymmetric wisp occupies21 square pixels in a7×11 area of
the upper-middle16×24 cell. Its named editable parts are upper tail, middle curl
and lower thread. Three existing east-kitchen palette constants—`#3C5148`,
`#526657`, `#7D8E71`—provide restrained gray-green masses. Bottom nine rows remain
fully transparent, with no overlap against the approved grate at rows15–22.

| Property | Value |
| --- | --- |
| Owner kind | Tile-state cue; not a world entity or inventory subject |
| Intended state | Exact `Cloud=steam` with positive `CloudTurns`; root controls scope and priority |
| Intended resource | `Sprites/Live16x24/east-steam` |
| Native frame | One16×24 frame, binary alpha,21 opaque pixels |
| Bounds | `[5,3,12,14]`, top-left coordinates, exclusive maximum |
| Import | 16PPU, center pivot, Point/Clamp, no mipmaps/compression |
| Contact context | Existing exact SteamVent frame16, unchanged and absent from the new native PNG |

The cue reports steam tile state; it does not establish source provenance, hot
vent temperature, contact damage, obscured sight, duration, quantity, an extra
owner or an available service. Do not tie it to SteamVent identity or replace
the distinct SteamCloud entity. There is no heat color, glow, smoke blanket,
shadow, active ticker or animated frame. The existing vent reader stays intact.

## Reproduction and evidence

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_east_steam.py -- --defer-unity
python3 -B ArtSource/Live16x24/east-steam/verify.py
```

The generator always exports only this source packet; `--defer-unity` is an
explicit reminder. `native.blend` contains editable named integer-cell planes
and three scenes. `source-cells.json` records every native cell. `native.png`,
`contact-native.png` and `contact.png` are direct geometry renders. Contacts
re-render the approved grate source geometry only to judge separation; they are
not gameplay screenshots. `native.png.meta` is packet-local source metadata for
root to copy after RED and a safe import point.

The verifier failed on eight absent artifacts before source production; see
`source-red.txt`. `source-green.txt`/`verification.json` establish exact native
and1×/8× contact pixels, palette/alpha, dimensions/sparsity, lower transparent
rows, zero grate overlap, valid importer settings and11 preserved input hashes.
It reuses the reviewed Cudgel semantic YAML comparator instead of duplicating
that framework. After root copies/imports, `verify.py --check-unity` additionally
checks PNG byte equality and every authored metadata value/structure while
allowing Unity's added default fields and whitespace.

Two independent Blender builds yielded byte-identical native PNG, contacts,
source cells and metadata in this run. `first-build-hashes.json` and
`rebuild-verification.json` record the bounded reproduction result; this does
not promise future Blender versions will preserve PNG metadata bytes.
`blend-check.txt` records reopening the saved Blend and all21 editable wisp
quads across its three scenes.

Author inspected native and both contacts; the wisp stays quiet and leaves the
grate distinct. Root independently inspected native/8× contacts and approved
the current design for an in-game trial. Source art is now frozen pending a
named integration defect. Root must still inspect real1×/2× world paint, actor
cover, priority/expiry, fog, save/restart and normal player input.

Final direct native PNG SHA256:
`02703f7b4db68f73c7b50e6cc48fba7a92eb3ee115d50dc8178fe52d89c50aa5`.

Decoded RGBA SHA256:
`d539adde13a92fcbdcc8bde7180cafc4763d86ce00d56a8c63f364d50d32a535`.
