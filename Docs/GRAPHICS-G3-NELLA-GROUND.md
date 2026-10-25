# G3 — Nella's working soil

2026-10-25 UTC. **Design and test preparation; not integrated or accepted.**

## Problem and bounded plan

The actual `prepared-bed-restored-focus.png` capture shows the selected empty
reserve bed at18,6 amid broad brown old-field fragments. The text correctly
identifies prepared soil, but its ground material does not distinguish it from
the surrounding cut rows. Preserve the approved Keeper, crop stages, tray,
cords, owners, coordinates, permissions and finite harvests. This is original
Caves of Ooo presentation work, not a Qud implementation parity claim.

Reuse the existing native ground family: old field earth (frame18), prepared dry
furrows (19), prepared wet furrows (20). Extend only the exact admitted current
Nella graph `Overworld.11.8.0`; retain the existing glade behavior. Do not expand
pond banks or road art as a side effect. Do not mark the four unprepared public
crops as cultivated. A prepared bed is a soil condition, not permission to take
its crop; the existing neutral cord and interaction menu retain that distinction.

## Pre-implementation verification sweep

| Premise | Verified correction |
| --- | --- |
| New soil sprites are needed | `FlatPixelOpeningGroundCatalog` already owns cached frames18/19/20; editable source is `ArtSource/Live16x24/Ground/build_ground.py` and `opening-ground.blend`. Reuse accepted bytes first. |
| Native ground already applies here | Both the catalog and `FlatPixelGreyboxCatalog.TryResolvePresentation` restrict it to `ReferenceGladePlan.IsActive`. |
| All Nella crops occupy cultivated soil | `SpreadExplorationResidents` attaches `CultivatedSoilPart` only to the two reserve beds. Public crops must retain their current generic moisture treatment. |
| Soil wetness persists after harvesting | Present implementation derives wetness from the current owned crop's `MoistureTicks`. A removed crop cannot keep an empty bed wet. |
| Broad brown strips are disposable decoration | Some `RipeCropRow` owners have real finite `FieldHarvestPart` yield. Change presentation, not owners or harvest behavior. |
| A current screenshot requires replaying all geometry | The prepared-bed screenshot and receipt are from the latest accepted readout pass. The intervening runtime change only alters cold northern works hedges at11,9, not Nella's saved graph at11,8. Reuse this verified composition for design; require new actual input for acceptance. |
| Unity is free for this audit | Preflight found normal Play active with no save isolation. Preserve it; request save/stop permission and do source/design/test preparation outside Assets while pending. |

## Production pipeline

1. **Playable baseline:** actual1280×800 `prepared-bed-restored-focus.png` and
   its adjacent receipt. Seed64, Nella,18,7, empty prepared bed18,6 after actual
   harvest/pickup/F6. Approved native material reference is
   `ArtSource/Live16x24/Ground/opening-ground-context.png`, a labeled source study.
2. **Imagegen reinterpretation:** exact prompt in
   `ArtSource/Live16x24/concepts/nella-ground/prompt.txt`; built-in imagegen output
   recorded alongside it. The output makes old earth too bumpy/prominent,
   softens pixels and alters UI. Reject generated pixels and that material
   emphasis. Retain only the dry-furrow versus irregular-earth distinction
   already present in native source. No generated image is gameplay evidence.
3. **Parts/state mapping:** old canonical CropRow/RipeCropRow→18; real prepared
   plain Grass or supported field owner→19; one current visible owned wet crop
   above prepared soil→20. Crops without cultivated soil, unsupported custom
   visuals, hidden/stale owners, ambiguous moisture and hazards keep truthful
   existing fallback. Actor/drop cover must not erase valid lower soil.
4. **Native production:** reuse existing exact16×24 frames and borrowed atlas;
   no approved pixel changes, new texture download, per-frame allocation or
   gameplay mutation planned. Change only bounded selection/integration.
5. **RED/GREEN and review:** first run authored tests against current code,
   then minimal runtime change. Pair every positive with ownership/visibility/
   scope or state counter-check; include default world integration and relevant
   existing ground/claim suites. Independent review before commit.
6. **Actual acceptance:** resume the earned fresh-origin after-North checkpoint;
   reach Nella through ordinary input, pay8drams, inspect the same public versus
   reserve beds, harvest/pickup, plant an earned seed, Rain, leave/return and
   save/load. Capture native arrival and close views. Compare ground hierarchy,
   exact owner state and selection. Reject the change if texture adds noise
   without making the genuine prepared beds clearer.

## Current review and limitations

- 🔵 Source/visual audit identifies one bounded material-hierarchy issue.
- ⚪ Reuse existing pixels before creating another asset packet; approved cast
  and neutral ownership cords remain unchanged.
- 🧪 No RED/GREEN run or new gameplay capture for this proposal yet. Native test
  drafts remain outside Assets until the current normal Unity session is free.
- 🧪 No new package, frame-time, draw-call, physical Deck or full-scene acceptance.
  Disk headroom is about1.45GiB; avoid another build without checking it.

## Continuation

Current pass: source sweep/concept/test preparation. Normal Unity Play is not
owned by this isolated audit; no gameplay input or stop was sent. Pending
save/stop permission must be resolved before dependent live/native editor work.
The earned after-North checkpoint remains untouched. After this material pass,
continue the fresh-origin glade→east forge→kitchen route and seed65 visual
counter-pass. A source/design record alone does not close these gates.
