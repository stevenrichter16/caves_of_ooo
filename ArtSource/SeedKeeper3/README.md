# Seed Keeper 3

Active gameplay artwork selected by the user on 2026-10-18. Originals were named
`seek keeper 3.png` and `seek keeper 3` in Downloads. The latter is an editable
Piskel document, preserved here with its normal `.piskel` extension.

The PNG and Piskel are unmodified copies. Their pixel buffers match exactly:
16×24 RGBA, 144 opaque pixels, six colors, binary transparency. No smoothing,
resizing or recoloring is performed. The corrected earlier design remains in
`ArtSource/SeedKeeper16x24` as an alternative.

Export after editing the PNG in Piskel:

```sh
python3 Tools/SeedKeeperFlat/export_native_sprite.py \
  ArtSource/SeedKeeper3/seed-keeper-3.png \
  ArtSource/SeedKeeper3/seed-keeper-flat.json
```

Copy the PNG to `Assets/Art/SeedKeeper3/seed-keeper-3.png`, retaining its metadata.
In an idle Unity editor, rebuild the gameplay mesh with:

```csharp
CavesOfOoo.Editor.SeedKeeperFlatBuilder.Build(
    "ArtSource/SeedKeeper3/seed-keeper-flat.json",
    "Docs/Verification/SeedKeeper16x24/game-import.json");
```

The native renderer uses one flat quad per opaque pixel and the existing
fog-aware palette shader. World rendering and Examine share this source.
