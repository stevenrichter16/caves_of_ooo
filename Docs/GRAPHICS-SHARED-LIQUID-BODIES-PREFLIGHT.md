# Shared liquid bodies: finite next-slice preflight

Source-only plan, queued after the current publication. Recommend **TarSeep + BrinePool**, two shared identities with existing immutable sprites and disclosed ordinary cave evidence. No new art, implementation, tests, gameplay inspection or coverage acceptance is claimed here.

## Verified contract and correction table

| Owner / premise | Source-corrected contract |
|---|---|
| BrinePool | `Assets/Resources/Content/Blueprints/Objects.json:31946`: non-solid Terrain; `~`, `&c`, layer1; Material Brine, conductivity95, combustibility0, Liquid/Water/Conductor tags. LiquidPool `brine`, initial Volume90; Thermal15, freeze−8, heat capacity2. TileStateSource authored `water`, lease4. No structural HP. |
| Brine source means fresh water | False: `Assets/Scripts/Gameplay/World/Map/TileStateSourcePart.cs:43–56` resolves the owned aqueous pool's actual liquid through EffectiveCoating, so this source renews **brine**. Do not select art by the raw `water` string alone. |
| TarSeep | `Objects.json:32107`: non-solid Terrain; `~`, `&K`, layer1; Material Tar, combustibility95, conductivity0, volatility0.3, Liquid/Tar/Fuel/Flammable tags. Actual LiquidPool is **oil**, initial Volume95; Thermal20, flame200, heat capacity2.5; source oil, lease5. No structural HP. |
| Empty stock means dry | False: `Zone.cs:796–806` projects ordinary pool coating without a Volume check; source renewal is separate. Observe real live coating. Do not erase hazards or depict depleted stock as dry. Initial shared admission may conservatively retain fallback at zero stock; Beating's existing zero-stock wet frames must remain unchanged. |
| Native pool automatically hides hazard waves | False: body coverage and prioritized tile-state representation are separate. Preserve mixed liquids, cold/ice, charge, fire/residue/cloud cues and current owner-driven effects. No blanket overlay suppression. |

## Immutable reuse and context authority

- Tar: `Assets/Resources/Sprites/Environment/tar_seep.png`,16×16, full rect. Exact same bytes as `ArtSource/MultiCellPilot3D/sprites/tar_seep.png`; SHA256 `f61a417b6559468ee44b5aa479f4f3f6134d4b3652b579601b917776d598015e`. Existing provenance: `ArtSource/MultiCellPilot3D/render_sprites.py:20` and its preserved source packet. Reusing that raster does not introduce 3D runtime geometry.
- Brine: `Assets/Resources/Sprites/Live16x24/kitchen-brine.png`,16×24, full rect. Matches `ArtSource/Live16x24/kitchen-brine/native.png`; SHA256 `c48258f0cd5722f0197bda2f11778f4908507399bd1873a4680aac2b7a4d8428`. Preserve editable `native.blend`, `source-cells.json` and verifier. Both candidates were viewed; actual graded visibility remains a gate.
- `FlatPixelKitchenBrineCatalog.cs:34–94` already owns admitted East12.11 pure-live-brine eligibility and reader behavior. `FlatPixelBeatingMaterialsCatalog.cs:51–67,179–188` owns its live regional admission and full20–23/depleted-still-wet24–27 frames. These contexts remain authoritative, including refusal/missing-resource fallback. New shared routing must not bypass those guards or replace those regional frames.
- Elsewhere, use cached centered FullRect16PPU views; texture groups fail independently and remain borrowed. Preserve exact current/disclosed/selected world owner, authorized reader identity, unique owned semantic parts, finite valid states, custody and footprint checks. A current explicit reader may inspect a disclosed pool beneath an actor; world selection remains the actor.

## Existing baseline and minimal next gates

`Docs/Verification/Live16x24/slate-materials-before-census.json` records two **visible** BrinePools at depth7 and four visible TarSeeps at depth8. The corresponding before04-stairs7,05-entry8 and06-wide8 PNGs plus earned fixtures98/114 provide genuine approaches, not current reader acceptance. First obtain ordinary current world/examine views for each; do not navigate from hidden census positions.

Then save a compact source-correction/parts-reuse record and genuine RED before production: paired natural/authoritative-regional selection, stock versus coating, mixture/thermal/effect refusal, optional resources/disposal, current disclosure/custody/actor-cover, and actual submitted hazard priority. Queries must not seed coatings, draw liquid or mutate stock. Finish normal travel, save/restart and bounded actual comparison/profile.

CharredHusk is a separate promising actor follow-up with existing16×16 art and observed combat. IceSheet/FrostVent need separate body-art decisions; Frost still lacks a disclosed baseline. No all-world or area-polish status advances from this plan.

Parked2026-10-28: user reaffirmed whole-game visual impact. Shared settlement residents and Beating wildlife precede this cave-supported liquid slice. No implementation or actual acceptance here.
