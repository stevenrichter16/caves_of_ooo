# Live 16×24 — loose opening items

Status: implemented and imported; 46/46 item cases pass in the 629-case native integration sweep. Ordinary Play verified a starter tonic drop, ground portrait, pickup and carried portrait. The complete harvest/cooking/cellar journey remains pending. Overall authority: [live art direction](LIVE-16X24-ART-DIRECTION.md). This is original CoO presentation work, with no new items, spawns or gameplay effects.

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

## Ownership review before production

The strengthened native fixture ran44 cases:1 passing hidden-owner counter and43 failures. Six failures exercise the existing four-atlas API and prove a real generic-item phantom after inventory/equipment/foreign Render/foreign Physics/empty or foreign stack mutation. The other failures cover the missing fifth atlas API/resource. Receipt: `Verification/Live16x24/opening-motion-items-strengthened-red.xml`. An earlier stale-assembly run with a fixture access error is explicitly excluded.

The correction is bounded to portable, non-creature owners in the existing cell enumeration, preserving current non-item unknown/foreign-part fallback semantics. Reject invalid loose ownership before either native art or procedural glyph selection, then select the next valid layer/tie owner. Untakeable but genuinely world-owned objects keep truthful fallback. Semantic item art stays placement-independent for future authorized portraits; it does not authorize a portrait itself. Explicit foreign VisualID is a reskin and must fall back; legacy glyph/color edits alone do not redefine blueprint identity. A corpse requires the Corpse tag and nonblank deceased SourceID plus marlback SourceBlueprint; the corpse does not contain its deceased producer's CorpsePart.

Second ownership RED:46 item cases yielded44pass, one expected missing resource and one real stale-old-cell duplicate. The valid multi-cell portable counter passed. `opening-items-examine-red.xml` also records the52 intended missing-API examine failures. The item boundary now requires current footprint membership using `SpatialQuery.DistanceToCell==0`, preserving legitimate covered cells and excluding stale anchor-list entries. No global Cell/gameplay rule changed.

## Integration and ordinary-input review — 2026-10-23

The actual Resources atlas has the reviewed source hash, Point/Clamp filtering, no mipmaps and no compression (`opening-items-import.json`). The final combined native receipt `opening-items-examine-green.xml` records629/629 passing, zero skipped, including all46 item cases and all twelve default-resource bindings. The preceding pre-import98-case run had exactly one missing-resource failure; importing the accepted atlas closed it.

In an isolated fresh seed64 game, ordinary keyboard movement let the existing NPC combat drop Mushroom and DriedMeat at39,20. The player dropped the real two-tonic starter stack at40,20, moved aside, selected that individual object from the pile and examined it, then returned and picked it up through G/Tab. The tonic vanished from the ground and retained count2 in inventory; its carried Examine showed the same native frame. No entity, loot roll or supplies were injected. Evidence: `native-items-tonic-dropped.png`, `native-examine-tonic-ground.png/json`, `native-examine-tonic-carried.png/json`. The latter records paused inventory, `opening-items` frame7 and `IsModel=false`.

Root and independent visual review accepted the crisp bottle silhouette and matching world/portrait proportions. Native tests cover the remaining exact item bindings, disposal and ownership countercases. This playthrough does **not** establish every twelve-item acquisition/use journey: cooked foods, cellar clay/timber, new gourd-product frames and a successful corpse roll still need their own ordinary journey checks. Earlier harvest captures establish the source mechanics, not this later item's complete visual acceptance. Variable dropped weapons/leather still use explicit fallback art.

🟡 Fixed before acceptance: invalid carried/equipped/stale portable owners could leave phantom fallback items; the boundary now rejects them before either native or generic art. 🔵 No unresolved P1/P2 in independent ownership/lifetime review. 🧪 Complete journey, controller and physical Deck performance remain release gates.
