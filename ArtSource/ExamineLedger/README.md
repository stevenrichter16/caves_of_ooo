# Field-ledger object art

Eight individual targets were generated with the built-in imagegen tool before any corresponding world-art changes. `target-prompts.json` records the complete prompts, object identities, paths, dimensions and checksums. Each PNG is an independent object target, not a sliced sprite sheet. The targets are visual guidance; the popup displays the game's actual model or current sprite.

## Implemented source cohort

| Object | Target | Actual world-art changes |
| --- | --- | --- |
| `RepairLinedWell` | `targets/lined-well.png` | Three masonry courses, exposed fire-clay inner lining before repair, missing segment retained in the damaged state. |
| `RepairRopeWell` | `targets/rope-well.png` | Masonry courses and four winding loops; the broken short drop remains distinct from the restored rope and suspended bucket. |
| `RepairWoodenGate` | `targets/wooden-gate.png` | Visible metal hinge studs across broken, closed and open states; existing leaf motion retained. |
| `KnotflaxCrop` | `targets/knotflax.png` | Mature pale fiber tassels and a broader knot; immature plants retain their smaller forms. |
| `HearthbulbCrop` | `targets/hearthbulb.png` | Broad stepped exposed bulbs and upright split leaves at maturity. |
| `SeamleafCrop` | `targets/seamleaf.png` | Upright central leaf, pale seam and three real geometric cross stitches per mature plant. |
| `SaltCuredBody` | `targets/salt-cured-body.png` | Salt crystals and wrist tally cord; the two case-number marks and original distinct body poses are preserved. |
| `FireClay` | `targets/fire-clay.png` | Three compact chipped clay pieces with small pale inclusions and shallow cracks. Native comparison confirmed the old item was a pale crystal cluster. The old large portable pack is untouched. |

The first six objects and FireClay use `ArtSource/RepairCultivation3D/build_source.py` and its existing Unity importer. The body uses `ArtSource/CurationYard3D/build_yard.py`. The parsed diff (`world-art-diff.json`) contains exactly 15 changed model variants, one added FireClay model and 42 unchanged models across these two packs, with no palette changes. The maximum is 92 cuboids per model, below the existing limit of 100; FireClay uses 19. Mature wet/dry crop geometry remains identical with different soil colors. Target-image glow around seamleaf and the preserved body is deliberately not represented as emission or light in game.

## Portrait implementation

`ExaminePortraitRenderer` resolves the current native recipe and individual prefab. It never clones a combined ground patch, which could include neighboring objects. It applies the same scoped mesh/palette replacements, creates owned material copies with independent portrait lighting, and captures only on request. A single lazily allocated 192×192 point-filtered target is reused; there is no per-frame render loop and no multi-object texture cache. `Clear` releases owned graphics resources. The calling reader must authorize its transient subject before requesting the image.

Missing native model support uses the subject's current sprite/glyph, preserving its tint. Null or hidden subjects produce no image. Unknown Unicode falls back to the existing question glyph without rebuilding the world atlas. This is an original Caves of Ooo presentation feature, not a claimed port of Qud's renderer.

Review tests cover visible non-anchor footprint cells without exposing hidden neighbors. A native failing test also reproduced a capture leaving its render target active; capture now restores the prior target even when rendering fails.

`GetPortrait(AnnouncementDocument)` revalidates the original world subject or carried/equipped ownership. Carried entries use the viewer's physically current presentation zone, then the existing portable item identities or shared crop/repair portable identity helpers. They never enter a cell to obtain art. Supported items therefore use the same current model on the floor and in the inventory, including brew-form changes; invalid visual identities and unsupported receiving regions retain the current sprite/glyph. A detached viewer receives only the owned item's fallback. Lost ownership receives no image. The 17 inventory test cases use real native content, zone graphs and imported assets without rebinding the full world renderer for every one-object check.

## Rebuild and evidence

```sh
python3 ArtSource/RepairCultivation3D/build_source.py
python3 ArtSource/CurationYard3D/build_yard.py
python3 -m unittest ArtSource/ExamineLedger/test_world_refinements.py ArtSource/RepairCultivation3D/test_source.py ArtSource/CurationYard3D/test_yard.py
```

Unity, idle Edit mode:

```csharp
CavesOfOoo.Editor.RepairCultivationBuilder.Build("Docs/Verification/ExamineLedger/repair-art-import.json");
CavesOfOoo.Editor.CurationYard3DBuilder.Build("Docs/Verification/ExamineLedger/curation-art-import.json");
```

The seven initial source constraints failed before implementation (`world-art-red.txt`). Offline inspection then found open well corners and clay lining buried behind the masonry. Two additional tests reproduced those defects (`world-art-review-red.txt`); the models now close each corner and expose clay on the inner shaft face. FireClay's missing clay model and updated pack-size pin failed before authoring (`fire-clay-red.txt`). Review also reproduced two added hand blocks buried inside the preserved body's existing folded arm (`body-occlusion-red.txt`); redundant additions were removed and the original poses retained. All 19 new/existing source tests pass (`world-art-green.txt`). Root-coordinated native test and import receipts live under `Docs/Verification/ExamineLedger`. `preview_refinements.py` renders the exact changed source geometry in Blender for offline inspection; that image is not a Unity screenshot.

Source constraints establish bounded geometry, deliberate silhouettes, repair-state differences, maturity cues and preserved case identities. They do not establish final screen readability, GPU capture correctness, animation behavior, player reachability or Steam Deck performance. Native screenshots and player-path tests remain separate acceptance checks in `Docs/EXAMINE-LEDGER.md`.
