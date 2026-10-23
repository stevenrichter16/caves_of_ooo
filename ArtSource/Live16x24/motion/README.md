# Opening motion source

Sixteen native 16×24 derivative poses for the accepted opening cast. The atlas is 256×24, transparent, binary alpha, square pixels, Point filtering and 16 pixels per unit. It contains no idle duplicates or expanded direction sheets.

| Frames | Actor | Existing idle |
| --- | --- | --- |
| 0–3 | Player | `base/opening-base.png`, frame 0 |
| 4–7 | PetDog | `cast/opening-cast.png`, frame 1 |
| 8–11 | MarlbackScrabbler | `cast/opening-cast.png`, frame 2 |
| 12–15 | MarlbackGleaner | `cast/opening-cast.png`, frame 3 |

Each group is ordered **walk A, walk B, melee, hurt**. The runtime actor pivot is bottom center `(0.5, 0)`. Every pose keeps its lowest opaque pixel at top-left row22, with bottom row23 transparent. Existing Tilemap idle images have centered pivots; an animated adapter must create its own appropriate sprite pivots without changing those originals.

The walk poses change actual leg/arm clusters, including open gaps. Identity-defining hood, dog head and shale plate geometry stays fixed during walks. Melee poses are equipment-neutral. The dog stretches into an open-jaw bite; the squat scrabbler grasps low; the taller gleaner raises its leading pincer. Hurt poses pull limbs inward and change balance without baked hit indicators, flashes or shadows. These are canonical poses; they do not claim four directional views.

## Source and review

`Tools/Live16x24/build_opening_motion.py` imports the accepted base/cast **cell geometry** read-only, copies it, and authors new named limb meshes. It never reads or edits a raster. Blender renders `opening-motion.png` directly at native resolution and renders the same geometry at integer 8× scale in `opening-motion-contact.png`. `opening-motion.blend` contains separately named editable frame collections and both cameras. `source-cells.json` records every authored cell and its original idle source; `manifest.json` records frame order, palette, bounds and atlas hash.

Reference: `../concepts/motion-parts-v1.png` and its exact prompt. Retained motifs are committed reaches and asymmetric recoils. Rejected motifs are the generated nearly identical walk frames, gradients, added facial details and oversized reach.

The first native render received an independent visual review with no static-art blocker: the reviewer could distinguish all walk pairs, the low dog bite, and the four hurt poses. A second source/render pass closed small coat gaps left by moved visible cells and removed one detached rust seam pixel on the gleaner. The scrabbler's attack is deliberately a small forward claw extension; its readability must be judged with real timing in Play before adding ornament or extra frames.

## Rebuild and verify

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_opening_motion.py
python3 ArtSource/Live16x24/motion/verify.py
```

`verify.py` performs read-only image analysis. It checks exact native/source and 8× contact pixels, binary alpha, original actor palettes, preserved walk identity clusters, changed leg silhouettes, unique poses, rejection of rigid idle translations, stable baseline and unchanged accepted PNGs. It writes only `verification.json`.

This directory is artwork source and static verification. It is **not** evidence of Unity import, hook-driven playback, visibility safety, motion timing, collision behavior or in-game acceptance. Those require the separately tested runtime adapter and real Game captures.
