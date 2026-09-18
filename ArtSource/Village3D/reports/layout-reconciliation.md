# Village layout reconciliation

Existing native Morrowfast definition and owner IDs. The screenshot guides appearance, not collision or save identity.

All 71 native owner IDs are represented. Ground covers 80 by 25 cells; the reference composition occupies X 21.25–58.75 and Z 0–25.

## Deliberate image differences

- **native-west-creek**: The reference shows a western cobbled branch without a large obvious creek; the shipped native map contains water there. All 67 native water cells remain water, rendered as a teal channel with shore detail and the native bridge.
- **native-room-extents**: Five shells/roofs use the shipped native interior rectangles and doors. Their image-space size and spacing differ from the screenshot polygons, especially the larger guesthouse and archive.
- **interiors-authored**: The reference depicts roofs. Beds, work surfaces, shelves, storage and hearths beneath them are authored views of existing room owners, not inferred new gameplay.
- **vegetation-and-gardens**: Additional fences, shrubs, rocks and planters are static scenery only. They have no native loot, collision, watering, destruction or persistence semantics.
- **source-style**: Reusable low-poly rounded geometry, a 64-swatch atlas, studio shadows and compact variation families approximate the reference. No screenshot plane or photoreal texture/AO bake is used.
- **preview-player**: The showcase hooded player exists only in the Blender composition, is omitted from persistent owner placements, and must be replaced by the live player view in Unity.
- **authorized-starting-garden**: User-authorized village new-game start adds six real native Plantable cells at X41/X42,Y21..23, visualized as bare inset earth in an existing terrain-detail mesh. The reference image has no such starting plot; no crops or new art owners are invented. Cosmetic right-verge paving is trimmed around the exact cells while the X40 road stays clear.

## Exact room placement

| Room | Native interior cells | Door cell | Entry cell | Shell X bounds | Shell Z bounds |
|---|---|---|---|---|---|
| keeper-gatehouse | X 32–34, Y 2–4 | (34, 5) | (34, 6) | 31.54–35.46 | 19.54–23.46 |
| dry-hem-guesthouse | X 28–33, Y 8–12 | (32, 13) | (32, 14) | 27.54–34.46 | 11.54–17.46 |
| long-loop-ropeshop | X 29–33, Y 16–19 | (31, 20) | (31, 21) | 28.54–34.46 | 4.54–9.46 |
| return-desk-archive | X 47–51, Y 2–5 | (49, 6) | (49, 7) | 46.54–52.46 | 18.54–23.46 |
| second-bowl-kitchen | X 50–53, Y 16–19 | (51, 20) | (51, 21) | 49.54–54.46 | 4.54–9.46 |

The companion JSON lists every source image foot, native anchor and footprint, runtime model origin and offset, room visibility, and static placement. Source image feet and model centres are different concepts; their coordinate difference is not automatically an alignment defect.

This ledger proves correspondence to source definitions only. Actual Unity materials, axis conversion, visibility, interaction and performance require integration tests and live review.
