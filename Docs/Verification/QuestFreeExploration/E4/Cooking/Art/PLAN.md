# F10 presentation: toasted grain and a truthful residual-heat workspot

Status: private source audit and approved toasted-grain design. Root approved one original browned-grain cluster on2026-09-28; the actual content and missing-binding native gate remain parent-owned. No shared Assets, Unity, workspot placement or general fire changes. Workspot identity is not yet chosen, so its forms and hooks remain plan-only.

## Existing source and correction

| Exact source | Confirmed current behavior | Narrow implication |
|---|---|---|
| `Objects.json:3382–3448`; `SpreadPortableRecipes.cs:45,311–324`; `ArtSource/SpreadPortable3D/designs.json:229–233` | Raw Emberwheat is an edible2d4, value10, weight1 sheaf; exact approved floor model `spread-portable-emberwheat`. Current source Unity bounds are about0.50×0.086×0.42; source was adopted from its real item FBX. | Preserve this sheaf and other foods byte-for-byte. New prepared food must not reuse a field/crop owner or silently rename the raw sheaf. |
| `SpreadPortable3DLibrary.cs:41–89`; `SpreadPortableModelIds.cs:7–40` | Current portable pack validates a complete fixed model-ID set and one source hash. | Prefer a tiny optional original cooking-art namespace for the added form over regenerating all existing portables. Apply the same receiving-Spread/current-owner/Takeable/no-custom-visual rules; no inventory mutation or universal food alias. |
| Private standalone content proposal | ToastedEmberwheat directly inherits FoodItem, `%`/`&y`, layer5, weight1, Healing3d4, CookingMeal, Commerce12, no Cookable. Emberwheat gains only `Cookable.Into=ToastedEmberwheat`. | Exact one-to-one prepared grain, no bowl, bag, extra item, quantity or reward. Cooking itself does not heal. Use actual core output identity in later model tests. |
| `CookingService.cs:14–80`; `CookablePart.cs:12–24`; `InventoryUI.cs:1427–1435` | Cooking is a carried-stack command; success closes inventory and charges the normal everyday action. Existing station selection was any adjacent CampfirePart. | Art must not invent a world “Cook” owner action, direct raw-food consumption, free action or new menu. Root's separate tested core changes own finite admission. |
| Private core `CookingService.MinimumFiniteCookingTemperature` | New exact opt-in rule is150°C, positive finite FuelMass, current owned parts and `CampfirePart.FiniteCooking=true`. BurningEffect is not required. Defaultfalse preserves authored and old-save semantics. | Reuse the final named constant, not another numeric threshold/readiness heuristic. This is source capability; it does not make every warm object a station. |
| `CampfirePart.cs:58–94` | Every current CampfirePart red/yellow flickers on Render and emits the one-shot “crackles warmly” proximity line. | A new residual-heat/cold owner requires a finite-only truthful branch; old campfires remain unchanged. Do not call the planned part field alone a presentation fix. |
| `ZoneRenderer.cs:627–642`; `CampfireEmberRenderer.cs:64–77,93–104,148–186` | Every CampfirePart is registered for free-floating embers. Spawn samples cached anchors without checking current temperature, fuel, visibility or owner membership. | For a finite residual-coal source, never inherit automatic endless embers merely by sharing CampfirePart. Prefer no ember emission for the new exact source initially. No broad particle rewrite. |
| `LightSourceFlickerPart.cs:79–109`; `Objects.json:18368–18499` | Old Campfire inherits an actual light/flicker plus Thermal500 and Fuel200. Flicker honors Enabled but captures its base intensity once; fuel/temperature is not its readiness policy. | A new inert coal-bed blueprint should not inherit an always-on light/flicker accidentally. Static palette form plus explicit readout suffices; actual Burning, if later present, remains canonical effect feedback. |
| `ArtSource/SpawnRing3D/build_ring.py:394–398`; `SpawnRing3D/Library.asset:47`; `SpreadNativeStyle3DLibrary.cs:31–41` | Existing campfire art is a fixed stone ring, charred logs and three upright ember shapes. Successful native static art preserves its ID/geometry. | Do not use the unchanged ember model as proof of a cold workspot or relabel the copied campfire a redesign. A new exact owner can receive a small original pair only after its contract is approved. |
| `ThermalPart.cs:231–249`; `FuelPart.cs:27–47` | Local EndTurn cooling decays toward ambient. Fuel consumption is a separate ConsumeFuel event, normally from Burning. | No fuel-per-cook charge, perpetual burning, exhaust spawning, elapsed offscreen cooling, refuel or touch-hazard claim comes from the visual pair. |

## Approved original ToastedEmberwheat form

One low cluster of coarse, split, browned grains with a few pale exposed interiors and dark toasted edges. Retain a readable asymmetric edible silhouette, about half a cell across and less than0.1cell high. No container, new stem/sheaf, loose particle system, steam, emission or rig. Use only the approved24-color glade palette and one inert static mesh/material. Proposed exact model `spread-toasted-emberwheat` in `SpreadCooking3D/Library`; namespace/model names are scoped implementation proposals, not current resources.

The source kit and native import are separate gates. Tests must first show missing original source/binding, preserve original food/source bytes, then prove current spawned/dropped/saved cooked owner uses the exact persistent model/material and accepts no hidden, carried, stale, foreign or custom identity. Native camera comparison must show the cluster differs from the old sheaf and remains visible on actual ground. A source review render does not prove ordinary acquisition or gameplay pixels.

## Workspot state proposal: avoid “unavailable means cold”

Root has not named the future workspot blueprint. Once named, use one compact pale stone/ash bed and low dark char fragments; the base geometry persists in every state. A restrained warm palette accent may indicate sufficient residual heat, while a neutral ash variant shows temperature below the shared cooking threshold. No tall flame or permanent light.

The source state has at least three meaningful combinations even if only two geometric forms are needed:

| Actual exact finite owner | Art/readout/action meaning |
|---|---|
|Temperature≥shared threshold; FuelMass>0|Heat-capable form; describe actual remaining heat/fuel and current cooking availability. No Burning claim.|
|Temperature below threshold; FuelMass>0|Cooler ash form; too cool to cook now. “Unavailable” must not promise exhaustion/removal or a refuel verb.|
|Temperature≥threshold; FuelMass≤0|Still-hot form or explicitly neutral unavailable form; readout must say no usable fuel, not falsely “cold.” Cooking refuses.|
|Malformed/replaced/removed/foreign parts or owner|No approved state claim; current source refusal/cleanup must follow normal authority.|

Do not turn this table into a new universal thermal readiness service. Root may select a small exact-source helper for cooking art/readout only after core adoption. Part/light/ember suppression, owner persistence and rendered refresh after cooling are required explicit follow-up tests before activation. Preserve the actual generic Thermal/Fuel/Burning systems.

## Bounded gates

1. Original source tests and exported static form; current raw/other-food source hashes unchanged. No workspot geometry until exact identity is approved.
2. Real ToastedEmberwheat child published, then actual native missing-binding RED; reference-compilable tests can be prepared first. Never count a missing child/setup failure as missing art.
3. Narrow optional library/binding import; one adopted mesh/palette, actual recipe owner, stack quantities untouched, pickup/drop/hidden/removal/save pairs. Keep original native failure and receiving biome guards.
4. Same gameplay camera raw versus prepared food, then actual harvested→cooked→dropped/used owner if the content route is ready. No injected ingredient in an ordinary-acquisition claim.
5. Only after workspot approval: actual local heat transition with exact current owner, no false embers/light/readout, hot/no-fuel counter, cached/save state and practical approach timing. Old authored campfires remain paired controls.

## Review boundary

The source audit confirms present mappings and identifies finite-source presentation work that would be required. It does not assert a current cold-site bug because no new opted-in workspot is activated yet. Cooking core is independently implemented/tested by standalone; root executes Unity and owns data/source adoption. The proposed original form supports that bounded extension without global food/fire redesign.

## Source iteration and Q1–Q4

The first source-only test run had6 missing-form failures and1 borrowed-input hash control. The original eight-kernel source then passed7 source cases and exported, but root rejected its upright column/stone reading in the preview. The first kit/build script and two frames are preserved under RejectedUprightPreview. A new low/elongated geometry witness executed1RED/7controls before revision. The revised kernel heights are.04–.06 and lengths.195–.245; same eight original pieces and160 triangles, with the source envelope narrowed to below.1 high. All8 current source cases pass. The native fixture's authored height envelope was updated before any publication/native run to match this explicit source correction; no executed native assertion was weakened.

The16-case binding fixture compiles against actual current Unity references with0 errors and has a clear bounded peer read. It uses the actual Cook output and Drop, real pickup/full-save replacement, all-piece style and exact original-food controls. Movement now explicitly selects a different ground patch; it does not claim native paid input or source-site placement. Native execution remains pending after actual content adoption; expected missing-art baseline is4 feature failures with12 controls, subject to actual results.

- **Q1:** the model identifies prepared loose browned grain, distinct from the existing tied/raw sheaf. Root's first visual rejection drove one bounded source correction; native small-camera readability remains unverified.
- **Q2:** one original mesh; no bowl, new quantity, hidden ingredient, emission or rig. Existing raw and other-food source/model bytes are hash-preserved. No shared Assets or generic fire changes.
- **Q3:** private source RED/GREEN and actual-reference compile only. Native missing-binding/import/scene proof, real harvested cooking and workspot activation remain separate gates.
- **Q4:** no production binding before meaningful native RED. The workspot design explicitly distinguishes insufficient fuel from lower temperature; the proposed name SpreadCookingCoals is coordinated but not implemented here.
