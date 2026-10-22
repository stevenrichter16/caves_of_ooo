# Beetle jar — supplied native sprite

Status: imported, connected to existing 2D art, and verified in Unity Play mode,
2026-10-21.

User request: add the supplied 16×24 beetle jar sprite to the game. Preserve the
PNG/Piskel, add it to the existing flat study and connect the existing 2D fixture
resource. No Qud assets or code are used.

## Verification sweep and plan

| Check | Finding and decision |
| --- | --- |
| Supplied files | Both PNGs and the latest Piskel have identical pixels: 16×24 RGBA, 176 opaque pixels, ten opaque colors, binary alpha. Use `beetle-jar.png`. |
| Existing identity | `BeetleJar` already exists with light and examine parts. No blueprint/gameplay changes are needed. |
| 2D routing | `EnvironmentSpriteRenderer.FixtureSprites` already maps BeetleJar to `Sprites/Environment/beetle_jar`. Replace that PNG while retaining its metadata/GUID. Loader accepts the taller native frame without rescaling. |
| Existing 2D import | Single, Point, 16 PPU, no mipmaps/compression/NPOT resizing, centered pivot. Preserve these established settings. |
| Study import | Separate native sprite/prefab with bottom-center pivot, white tint and existing unlit material, matching the other study sprites. |
| Existing 3D/portrait | Routed separately through Olderdeep art. Preserve this style; updating the 2D resource does not replace the existing 3D model or its Examine portrait. |
| Lighting/tint | Ordinary 2D fixtures retain the current glyph/light tint; the flat study displays exact authored colors. Do not change lighting semantics as part of this art import. |

1. Verify source identity and save the editable originals plus previous 2D PNG.
2. Import native art, update only the existing fixture PNG and add a fifth study
   prefab/cell at the same native pixel scale.
3. Run existing terrain render coverage tests, inspect actual Unity Play and
   compare every screenshot pixel against the source composite. Restore editor
   settings, review the diff and publish with updated living docs.

## Results and self-review

- Source PNG, duplicate export and Piskel match at native 16×24. Both Unity PNGs
  are byte-identical copies. Original Piskel and prior 16×16 fixture image are
  retained in `ArtSource/BeetleJar16x24`.
- Existing fixture loads a full 16×24 Sprite at 16 PPU, centered pivot (8,12),
  retaining GUID `91711e9a2082c5f8721ffb16d83e143c`. Its `.meta` is unchanged.
- The bottom-centered study prefab is the fifth adjacent cell. Group is centered
  at world X -2/-1/0/1/2, shared cell baseline Y -0.75 and scale 1. No source
  resizing, cropping, altered camera or new runtime script.
- Actual 1280×800 Game frame matches every source/background pixel at exact 8×
  scale, with zero differing pixels. Play stopped; Game view index restored to
  0; saved study clean.

| Verification | Result |
| --- | --- |
| Existing terrain render suite | 13/14 pass. The coverage census reports existing `FrontierClothScreen` and `CausticFilm` gaps. |
| Baseline counter-check | Temporarily restore the original 16×16 fixture PNG: the same census case fails with the identical two names. Restore and reimport the new PNG afterward. This failure is independent of the jar art. |
| Targeted resource/3D preservation | 21/21 actual Unity EditMode cases pass, none skipped: existing vertical-art loading and exact Olderdeep identity/model checks. |
| Source and imports | Native canvas, binary alpha, ten colors, matching Piskel, byte-identical PNGs and existing fixture GUID verified. |
| Actual Play | Five sprites, no game bootstrap; exact full-frame source composite. |

- ⚪ Pre-existing render-coverage census gaps are recorded rather than expanding
  this asset import into unrelated terrain changes. No new failure was found.
- 🔵 The original 3D jar model/Examine art remains preserved. The new art is used
  by the existing 2D resource and the flat study, which is the scope shipped.
- 🧪 Can verify: source/export identity, native fixture resource loading, mapping,
  import/GUID preservation, 3D library regressions, saved prefab/scene placement,
  actual exact Game rendering and editor restoration.
- 🧪 Cannot claim: new jar mechanics, exact source colors under ordinary gameplay
  tint/lighting, replacing the existing 3D Examine portrait, or Steam Deck tests.
  No blueprint or gameplay source changed.

[Source and edit instructions](../ArtSource/BeetleJar16x24/README.md) ·
[Original resource observation](Verification/SeedKeeper16x24/beetle-jar-before.json) ·
[Terrain tests](Verification/SeedKeeper16x24/beetle-jar-terrain-tests.json) ·
[Baseline failure proof](Verification/SeedKeeper16x24/beetle-jar-legacy-baseline.json) ·
[21 passing targeted cases](Verification/SeedKeeper16x24/beetle-jar-targeted-tests.json) ·
[Live observer](Verification/SeedKeeper16x24/beetle-jar-live.json) ·
[Exact pixel proof](Verification/SeedKeeper16x24/beetle-jar-capture.json).

![Beetle jar in the actual Unity scene](Verification/SeedKeeper16x24/five-sprites-beetle-jar-game.png)

Files: original PNG/Piskel/backup and verification, native study PNG/prefab and
metadata, existing fixture PNG, saved study scene, this living doc and study
updates, Unity receipts and actual Game capture. No runtime code changed.
