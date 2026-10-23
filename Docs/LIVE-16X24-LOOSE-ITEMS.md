# Live 16×24 — loose opening items

Status: source/design verified; native art in progress; runtime integration NOT IMPLEMENTED. Overall authority: [live art direction](LIVE-16X24-ART-DIRECTION.md). This is original CoO presentation work, with no new items, spawns or gameplay effects.

## Pipeline record and purpose

Actual ordinary-input harvest at46,4 produced a shell and a seed; the later seed owner covered the shell under the existing render-layer tie rule. `Verification/Live16x24/native-states-gourd-harvest.png/json` records it. Built-in imagegen used that real screenshot and accepted anchor/state contacts to make `ArtSource/Live16x24/concepts/loose-items-v1.png`; exact prompt and critique are beside it. Keep the open gourd neck, tied strips, compact material shapes and collapsed plates. Reject shaded pixels, three-pronged item quartz and isometric logs. Native reconstruction lives in `ArtSource/Live16x24/items` with original integer Blender geometry, binary alpha and compact16×24 cells. Concepts are not runtime assets.

## Source sweep and exact owner list

| Frame / blueprint | Available source | Real use and boundary |
| --- | --- | --- |
| 0 Emberwheat | Three ripe opening rows; gather/overflow | Food2d4; Cookable.Into=ToastedEmberwheat. Loose grain, no rooted row. |
| 1 ToastedEmberwheat | Actual carried cooking output | Food3d4 and20 heat resistance for100 actor turns; replaces previous meal. No flame. |
| 2 DrawgourdShell | Harvest opening DrawgourdCrop | Retained3-charge Waterskin. Starts empty, no visible contents claim. |
| 3 DrawgourdSeed | Same harvest | Seed.CropBlueprint=DrawgourdCrop; requires prepared soil. Distinct from planted stage0 mound. |
| 4 Mushroom | Two opening rings | Food1d4; Cookable.Into=RoastedMushroom. Source disappears on harvest; loose cap differs from ring. |
| 5 RoastedMushroom | Actual carried cooking output | Food3d4 and20 acid resistance for100 turns; replaces previous meal. No poison claim. |
| 6 GlowQuartz | Opening veins | MineralPreparation; forge infusion or consumed to create separate CrackedGlowQuartz. Raw item has no light source. |
| 7 HealingTonic | Real starter/chest supplies | Tonic healing4d6+4, apply or throw; consumed dose, no empty-bottle owner. |
| 8 DriedMeat | Real starter/chest supplies | Food3d4; bait for eligible furrowstalkers elsewhere. Does not promise opening dog/marlback bait response. |
| 9 FireClay | Two finite cellar-crate units | Two repair well or kitchen pan; one smothers an existing flame. Matte clay. |
| 10 SalvagedTimber | Two from dismantled cellar pallet | Wicket/wood repair, eligible trap jam, haft or finite cooking fuel; feeding is not ignition. |
| 11 MarlbackCorpse | Actual death,70% creation chance | Carryable10-weight Corpse with source provenance. No default food, butchery yield or decay. |

Read sources: ReferenceGladeBuilder, GleanersCellarBuilder, CropYieldService, CookingService, CorpsePart, FoodPart, CookablePart, SeedPart, MineralPreparationPart, TonicPart, StackerPart, WorldResourceActions, Cell.GetTopVisibleObject and current flat catalogs. None of these twelve is an initial foreground owner in the fresh opening. They appear through real harvesting, dropped supplies, overflow, cooking-then-drop or death. No extra display spawns are needed.

Corrections: ground AddEntity does not merge stacks; later equal-layer owner wins. Ten items stack (99 maximum, except timber20); shell and corpse do not. Cooking outputs go into inventory atomically and roll back if capacity fails; they do not scatter beside fire. CrackedGlowQuartz is a separate non-takeable radius4/30-turn owner, outside this group. Recipe readiness, charges and stack amounts do not require fake overlay icons. Display one actual selected owner, not a painted combined pile.

## Implementation design

1. Add an independently optional borrowed192×24 item atlas to the current flat catalog, with12 cached sprites/tiles, white tint, centered pivot and16PPU. Earlier injection overloads deliberately omit the new group; missing/malformed resource retains the existing visible fallback. Keep original atlas groups unchanged; no sprite generation in cell rendering and no new world traversal.
2. Route exact blueprint plus owned visible Render, takeable Physics and valid nonempty Stacker when present. Verify the actual semantic part contracts in the table before choosing distinctive function-bearing imagery. Unknown/reskinned items remain explicit fallback. Clay/timber need no invented material part. Corpse requires actual Corpse identity and opening source provenance; no dead-creature alias or baked equipment.
3. Keep spatial ownership at the world boundary: admitted current zone/cell, actual selected top owner, current visibility, no carried/equipped duplicate. A future authorized examine request may reuse semantic artwork for a genuinely carried owner without pretending it occupies the world. Do not loosen the world gate for that use case. `Cell.GetTopVisibleObject` alone does not validate Physics ownership or inventory state; test these counters at the native selection boundary.
4. Leave consumption, container transfer, cooking, harvest, pickup and drop mechanics unchanged. Ordinary world removal/dirty hooks own disappearance; add a narrow invalidation only if a meaningful renderer RED demonstrates a missing update. Preserve existing tie/layer behavior and lower-owner reveal after pickup.

## Tests and acceptance

Write/run native RED first for: twelve exact frames and owned semantic parts; real atlas missing; independent missing/malformed groups; rectangle/pivot/Point settings and cache reuse; disposed catalog leaves borrowed textures alive. Add current-world counters for carried/equipped/stale owner, invisible/unexplored/memory, zero/foreign stack, removed/replaced entity and unknown blueprint. Test real harvest→ground pair→pickup→seed consumption; real food cooking and drop; clay/timber acquisition where feasible; authentic death provenance rather than fabricating corpse availability. Runtime inventory artwork is a separate gate.

After GREEN, ordinary input should harvest and inspect/drop/recollect a gourd shell/seed, harvest a mushroom and quartz, and drop/recollect one real starter tonic/meat. Capture actual1280×800 output and read-only state. Do not conjure materials, guarantee a corpse roll, or force a crop to maturity for screenshots. Cellar supplies can be verified during the connected journey. Source/native texture integrity cannot prove gameplay routing, and Editor results cannot prove Deck hardware performance.

Self-review before implementation: 🟡 corrected raw-quartz light and cooked-food scatter assumptions; 🟡 separated ground owner from authorized carried portrait; 🧪 runtime, resource import and ordinary-input acceptance remain pending. Files changed so far: this plan and separate concept/source group; no production changes from this document.
