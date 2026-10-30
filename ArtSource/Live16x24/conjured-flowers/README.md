# Conjured flowers — original FlowerField sprite

Two original 16×24 frames in `conjured-flowers.png` (32×24) for `FlowerField`. Its examine text describes the flowers as petals "someone hummed up for the season, a carved token half-buried underneath"; the patch has a 200-turn lifespan and a re-asserted petals residue.

**Design.**
- three loose magenta flower heads on thin stems;
- a low leaf band joining them to the ground;
- a pale carved token half-buried at the base.

It is deliberately unlike the approved CharmFlowers bells (east-kitchen frame 14); the handoff forbids treating the two as one identity. The frames differ only in petal light and pollen placement (12 pixels), alternating by (x+y)&1. Binary alpha lets the real grass show around the sprite.

**Reference.** The offline Spread meadow and Lampwell renders, the approved CharmFlowers bells (for contrast), and the blueprint text. No image-generation concept was available in this session.

**Checks.** `--verify` checks every native pixel, one contiguous footprint, bounds, three pollen hearts per frame, and the contact sheets on lawn and town floor. `--inspect-blend` reopens 108 unit quads.

**Known polish debt.** A dense meadow repeats the same clump and token in every cell. A sparser or varied layout would read better at normal zoom.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_conjured_flowers.py -- --build
python3 Tools/Live16x24/build_conjured_flowers.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/conjured-flowers/editable.blend --threads 4 --python Tools/Live16x24/build_conjured_flowers.py -- --inspect-blend
```
