# Beating native material adapter — independent contract review

2026-10-27. Source-only review and scratch implementation. No active project edits, Unity operations, Git actions or native test results. Root owns actual six-formation baselines, imagegen/native art and import after RED. Scratch implementation: `/tmp/FlatPixelBeatingMaterialsCatalog.cs` (356 lines at initial handoff). This supersedes the provisional25-frame idea in the original preflight: root fixed32 frames, including essential depleted-but-wet brine states.

## Bounded contract

`FlatPixelBeatingMaterialsCatalog` follows the existing Sodden companion shape: borrowed current-owner selector; default Resources constructor; injected `(Texture2D atlas32, Texture2D vegetation4, Texture2D openingGround21, Texture2D anchors12, FlatPixelFallbackCatalog borrowedSelector)`; `IsSupportedZone`, foreground/ground/subject methods, deterministic variants, cached Tile/Sprite ownership and idempotent disposal. Independent missing/malformed groups never trigger Resources fallback. New public `IsRepresentedGroundOwner(zone,x,y,owner)` is solely for suppressing a represented literal floor glyph, including a reversed Sand/Crust layer tie. It never authorizes hiding a covering actor/item/structure.

Scope requires canonical authored ordinary `BeatingCompositionPlan.IsWildernessZone`, a real bound manager, exact current CachedZones reference, correctly shaped current map arrays, actual Beating biome, null POI and no `LairStacks.HasSavedColumn`. Static address metadata may be cached; mutable world approval may not. `BeatingCompositionPlan.Create` supports forced previews and is not authority. No generator invocation, lair-ledger creation, world-state advance or global `IsAdmitted` widening is allowed. Sources: `BeatingCompositionPlan.cs:32–60`, `OverworldZoneManager.cs:114–194,243–248,747–755`, `LairStacks.cs:194–205`, current Sodden adapter57–71.

World lookup requires the actual current explored+visible cell and fallback's same selected visible owner. Selection already handles current placement/custody/disclosure and layer tie ordering (`FlatPixelFallbackCatalog.cs:56–95,125–151`). Native additionally rejects custom VisualID/VisualVariant, semantic part duplicates/foreign parents, nontrivial footprints, item/player/creature aliases and active effect color overrides. `StatusEffectsPart.GetRenderColorOverride` is a read-only query; do not fire a Render event here. Neutral artwork is white native RGB; actual runtime lighting remains the renderer's job.

## Corrections which matter during implementation

| Family / source | Minimum semantic distinction | Why a generic Sodden copy is wrong |
|---|---|---|
|Sand7853 / SandstoneFloor8761 / SaltCrust33695 / RoadStone33484|Passable layer0 Terrain. Sand/floor glyph+variants match actual source; crust `+` with `+-|`; road `=`. No required Material/Thermal/Destructible.|Requiring stone material rejects real floor entities. Crust is a real passable surface over Sand, not a new obstacle or a pool.|
|DuneCrest33719|Solid layer1 Terrain, `^`, no required Material/Thermal/Destructible.|Do not invent destructibility/stone/heat parts to make sprite guards pass.|
|SandstoneWall7895 and inherited Wall3280|Solid Wall, layer0 `#`, Stone Material, intact Destructible, neutral Thermal. Terrain tag is not required.|Inherited wall is not the same blueprint shape as PeatBank.|
|Rock7954|Solid PhysicalObject, layer1 `o`, Stone Material and Thermal, no declared Destructible.|A blanket Intact guard rejects real rocks. Do not add destruction merely for art.|
|DryBrush32213|Passable layer1 Terrain/Vegetation, DryPlant, intactHP4, neutral Thermal.|White native pixels cannot conceal burning/wet/frozen/etc owner tint. Borrow exact north-vegetation frame3 only.|
|PaleSaltVein28982|Solid mineral vein, layer4 `*`, real unspent Harvestable→PaleSalt1–2.|Blanket Harvestable refusal drops the meaningful activity to punctuation. No invented permanent spent node.|
|Bones33746|Passable layer1 Terrain `%`, unspent Harvestable→Bone1–2.|It is a harvestable world source, not a living actor/corpse and not a decoration to scatter for fullness.|
|Saltbriar33885|Passable layer4 Vegetation `;`, Plant Material, intactHP5, Thermal, unspent Harvestable→SaltbriarSprig1–2.|Do not relabel DryBrush or cactus. Harvesting/removal remains the real transaction.|
|Signpost29696|Solid Physics, layer1 `I`, sole owned RegionalSignpostPart; no Solid tag requirement.|It supplies current regional directions on examine (`RegionalSignpostPart.cs:5–22`); do not bake invented arrow/text into runtime identity.|
|BrinePool31946|Layer1 Terrain `~`, non-solid Brine Material, neutral Thermal, actual poolLiquidIdbrine, Volume>=0, current source EffectiveCoatingbrine and sole live brine tile coating.|No Destructible required. Source's literal Coatingwater resolves actual brine via LiquidRegistry, not an independent freshwater film.|

All blueprint numbers refer to `Assets/Resources/Content/Blueprints/Objects.json` at inspection. Material percentages/fractions are historically mixed (`MaterialPart.cs:14–28`); the render adapter should not normalize material mechanics or require guessed numerical values. Exact semantic identity and owned parts are the art gate.

Harvestable checks are exact original yield1–2/chance100/unspent for this slice. Actual success removes the source; a spent retained reference should not show fresh resources (`HarvestablePart.cs:126–164`). Rendering must not toggle Harvested or place substitute spent art.

## Ground composition and state

Ground lookup scans real current occupants; it does not replay BeatingCompositionPlan, infer invisible rooms or paint planned owners into removed cells. Skip hidden, foreign, detached or custodied owners before matching blueprint names, so hidden duplicate Sand/Crust cannot erase disclosed material. One actual base (Sand, SandstoneFloor or RoadStone) is allowed. One SaltCrust may overlay Sand and takes priority irrespective of insertion order; multiple engineered bases or crust over an engineered base are ambiguous and retain fallback. Actual dry overlays DryBrush/DuneCrest/Bones may sit above Sand without forcing black backing. Other meaningful terrain, cultivation, pools/sources, gases and ground markers retain their current presentation.

No dry material is substituted across nonempty independent tile state. This conservative first contract preserves coatings, residue, charge, heat/cold and clouds. Ground and selected subject are distinct: native ground under an actor must not remove the actor's foreground. If the actual selected owner is Sand on a reverse same-layer tie below current Crust, `IsRepresentedGroundOwner` suppresses the sand glyph only after the crust texture successfully resolves. Reader still requires the same actual selected subject; it must not depict Crust as the portrait of Sand.

## Brine and submitted overlays

Atlas20–23 represent full brine;24–27 represent depleted but still wet brine. A zero-volume LiquidPool can retain/reassert wet coating (`TileStateSourcePart.cs:48–56,125–134`; `LiquidPoolPart.cs:123–127`). Negative volume, wrong liquid/material/source, mixture, expired/absent coating and ice refuse. An independent water film remains plain water. Active owner color effects and non-neutral owner thermal state also refuse.

Root should OR Beating's actual successful native BrinePool submission into the existing `RepresentedBrine` flag, not set it merely for blueprint/zone/resource existence. Reuse `FlatPixelKitchenBrineCatalog.IsSoleLiveBrine` for exact independent coating truth; do not reuse its Kitchen-only scope. Both normal and dirty paint paths pass this flag (`ZoneRenderer.cs:1266–1272`, `ZoneRenderer.FlatPixelPaint.cs:35`). The overlay suppresses only the duplicate sole brine mark; real residue/veil-mist, charge, heat/cold and other clouds remain according to existing priority (`ZoneRenderer.cs:1283–1400`). Missing resource, cover/uncover and fog must restore the honest fallback. Do not declare a native body when it was covered or refused.

## Frozen atlas and safe borrowing

Root's32-frame512×24 contract: Sand0–3; SaltCrust4–7; Dune8–11; SandstoneFloor12–15; Wall16–17; Rock18–19; fullBrine20–23; depletedStillWet24–27; PaleSaltVein28; Bones29; Saltbriar30–31. Borrow unchanged vegetation4/frame3, openingGround21/16–17 and anchors12/frame8. The adapter owns new cached Sprite/Tile objects, not borrowed textures/selectors. Point-filtered16×24 import and native palette acceptance belong to root. No connected mask/neighbor system was introduced: avoid a sixteen-mask framework unless actual dune seams require it.

## High-value test pairs delegated to the test reviewer

1. Six naturally generated formation positives versus current biome/POI/saved-lair/current graph/canonical-address mutation; read-only map/ledger state remains unchanged.
2. Actual ground priorities, crust removal exposing sand, both crust/sand insertion orders, actor coverage and valid dry-overlay underlays; visible malformed/competing material vetoes, hidden/foreign/custodied duplicates do not alter ground.
3. Exact original families including Rock/Dune missing parts and wall without Terrain; wrong solidity/glyph/part parent or duplicate semantic part refuses.
4. Real unspent harvest source versus changed yield/Harvested/removal; valid harvest transaction actually consumes the source and does not leave a native phantom.
5. Full and zero-volume brine from real registry/source Seed versus independent water/mixed/expired/absent/negative-volume states; zero remains wet.
6. Owner Wet/Burning/Frozen/Electrified tint refuses; independent cell heat/charge/residue/mist remain visible above successfully submitted brine. Exercise the public renderer, cover/uncover and dirty submission.
7. Required-resource absence/wrong dimensions refuses that family only; borrowed groups remain available. Disposed catalog does not destroy borrowed textures/selector and cannot keep returning cached native images.
8. Same selected-visible-owner portrait succeeds; hidden/covered/custodied/removed/replaced graph subject refuses. No new inventory authorization battery is needed.

Do not claim zero allocations when the runtime positive control remains broken; existing inconclusive probes stay honest. No native compile/test execution was performed in this subtask. These source checks do not accept full Beating chunks: root's matched six actual gameplay views must still judge repetition, road/shoreline clarity, landmarks, missing actors/props and meaningful spatial fullness. Named settlements, special sites, lairs, plain water, other liquids, remaining flora/actors and all other biome queues remain separate coverage work.
