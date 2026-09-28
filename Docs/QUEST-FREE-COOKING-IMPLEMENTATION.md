# Quest-free cooking: useful harvested grain and truthful sources

Status: core, original prepared-food presentation, bounded native input/save witness and full native regression pass. The full Unity sweep is 21,584/21,584, zero failures/skips, in 1,772.1113718 seconds. Field gates and stable hauled beams previously shipped in `856690a6`. The parent [environmental exploration plan](QUEST-FREE-ENVIRONMENT-IMPLEMENTATION.md) remains authoritative. This is original Caves of Ooo content, with Qud as exploration inspiration, not a source-parity claim.

## Purpose and bounded sequence

Connect finite field harvesting to a useful existing action. A gathered emberwheat sheaf can become toasted grain at a valid cooking station. The transformation preserves quantity and weight, raises healing from 2d4 to 3d4, and base commerce from 10 to 12. This is a deliberate content addition, not a new ingredient grant, harvest increase, hunger system or quoted sale-income guarantee.

1. Reproduce actual source/ingredient identity defects and missing grain recipe in the existing cooking path. Add only exact current ownership validation and an opt-in finite-source rule. Preserve established stations and inventory transactions.
2. Add the one-to-one recipe and an independent prepared-food blueprint, with an original portable grain model. Prove actual harvested input, cooking, eating, dropping, quantity and saving; preserve the raw sheaf and other food models.
3. Separately activate a finite residual-coal workspot only after source placement, current heat/fuel readout, particle/light behavior, actual arrival timing and saved aftermath are verified. A warmed mesh is not a functional campsite.

## Verification sweep and source corrections

| Assumption | Actual source and decision |
|---|---|
| Ordinary raw food will make arbitrary new fires useful | Spread loose stock and ordinary crates/sacks chiefly supply prepared food. Magpie apples are not an ordinary talkable trade source. Use actually harvested emberwheat through an explicit recipe addition. |
| Emberwheat already cooks | Its existing blueprint has Food but no Cookable. Add Cookable only; preserve RipeCropRow's one-unit yield and spent state. |
| Prepared grain can inherit the raw item | That would inherit Cookable and permit repeated preparation. The new ToastedEmberwheat is an independent FoodItem with no recipe. |
| Remaining adjacent proves the cooking source is unchanged | Output factories can remove/replace CampfirePart or move the chosen source to another adjacent cell. Capture and revalidate the same owner, anchor and actual part references. Do not substitute a different nearby fire. |
| Same-valued input parts are equivalent | Output callbacks can replace the selected Cookable or Stacker. Keep exact current recipe, stack, physics and inventory ownership, as well as quantity and destination. |
| All old cooking stations should become finite | Existing authored and thermal-less stations have heat-independent semantics. Public saved FiniteCooking defaults false; only a future explicitly authored source opts in. |
| Fuel200 and temperature500 imply active burning | Burning and fuel consumption are separate. The finite cooking rule requires positive finite fuel and temperature at least the named 150-degree cooking threshold; it does not ignite, consume fuel per recipe or promise a refuel action. |
| Each scheduler tick cools all scenery | The actual production material pass follows InputHandler.EndTurnAndProcess once. Direct material passes do not prove elapsed player actions, travel, resting or offscreen cooling. Test useful approach timing separately. |
| Unavailable cooking means cold | A hot source with no usable fuel still has heat. Presentation must distinguish cooking readiness from thermal state; do not show a false cold claim. |
| Any CampfirePart is merely a cooking source | It also offers free resting, flicker, a crackling line and generic ember registration. The proposed residual-coal workspot will explicitly opt out of resting and automatic flame cues; old-save/default rest compatibility needs a real missing-field wire test. No rest/clock-system rewrite is planned. |

## Current core contract

The exact chosen station and input identities are captured before output factories. Revalidate current membership, backlinks, the same anchor, recipe, quantity and eligibility before consuming anything. Independent callback changes remain in place when cooking refuses. Existing inventory snapshot, claim, rollback and after-commit paths remain responsible for transfers and messages.

Only an opted-in finite source requires current owned Fuel/Thermal parts, finite positive FuelMass and temperature at least CookingService.MinimumFiniteCookingTemperature. Legacy stations retain their established heat-independent cooking behavior. No finite workspot blueprint or default cooling change is part of this core slice.

The new food model is a low cluster of original browned kernels in the approved palette, with no bowl, bonus container, particle system or rig. The first upright source read like stones and was rejected; one bounded revision flattened and elongated the kernels. Source approval is separate from actual native-camera acceptance.

## Evidence ledger

- Executed private matched core39: 4 controls pass and 35 failures before implementation, then 39/39 pass. Five failures demonstrate real station/input callback identity defects; other failures establish missing finite opt-in or grain content. Raw failures are retained.
- Private candidate plus 97 existing cooking/everyday/rest neighbors: 136/136 pass. Actual Unity-reference runtime and EditMode assemblies compile with zero errors. These are not native execution claims.
- Actual Unity core RED: 39 total, 4 pass / 35 fail / 0 skip (4.230062 seconds). After adopting the three production files, the same core plus 97 existing neighbors passed 136/136. The combined 152-test native run deliberately retained 4 missing-art failures and 12 art controls passing (16.6510205 seconds); its exact full receipt is retained. The following independent art gate closed those four expected failures.
- Original model import succeeded: one 160-triangle mesh/prefab, one optional library, ten exact created asset/metadata files. All 1,894 borrowed source/output hashes stayed unchanged. The active Main scene stayed unchanged and clean. Subsequent native 16 art cases plus 110 lookup/style/portable/gate/voxel neighbors pass 126/126, zero failures/skips (32.8956421 seconds).
- Native observer mode first failed 2/2 for its absent launcher/initializer, then passed with 60 existing mode/restoration neighbors: 62/62, zero failures/skips (0.094741 seconds). The test covers the explicit isolated-save requirement before input/world setup.
- Exact test/data/evidence manifests are retained under `Verification/QuestFreeExploration/E4/Cooking/`. New Assets scripts include fresh metadata; Objects.json was edited surgically; parsed comparison proves only Emberwheat changed and ToastedEmberwheat was added (510→511 blueprints).

## Actual native input and viewed game frames

Run `04c1be32c4584126b222d5f7846efd95` completed 12/12 checks, zero failures, six paid actions in 11.2492847 seconds. It starts an ordinary seed64 new game, selects the first actual qualifying field among at most eight canonical FieldStrips addresses, and uses two disclosed original-player-only setup transfers. The actual field is `Overworld.10.15.0`, approach (55,4); the existing Sill campfire is `Overworld.10.10.0` at (11,16). No food, station, actor, health, equipment, currency, RNG or time was granted or edited to obtain the result.

**Can verify:** native world-menu Harvest consumes the current finite row and packs exactly one Emberwheat. The inventory Cook command at the actual existing fire costs one action and makes exactly one ToastedEmberwheat. Free Drop and a paid step expose its exact approved ground model. Native F5 saves the ground food and cached stubble; subsequent Take and movement change the current state; F6 replaces both graphs and restores the same food ID/quantity, spent row, player stats/gear/currency/position/clock. Native Take then leaves the restored prepared food carried and usable. Main was restored clean, Play stopped, and the isolated save override cleared; the user's saves were not replaced.

Root viewed the actual field, dropped food, loaded food and final inventory frames. Independent art review viewed dropped/loaded/inventory frames. The small gold/brown kernel cluster is visible west of the player/fire and keeps its local appearance through loading. The inventory clearly names toasted emberwheat. This accepts its near-player presentation at the captured camera.

**Cannot verify from this run:** ordinary discovery or walking from the field to Sill, distant recognition at the full-zone zoom, a raw-versus-prepared ground comparison, human preference, native Eat/healing, or a new finite workspot's lifetime. The model is tiny at this zoom. Actual Eat and raw-model preservation are separately covered by the focused tests. No additional observer polishing is needed to publish this useful bounded slice.

Receipts and eight frames are under `Verification/QuestFreeExploration/E4/Cooking/Native/04c1be32c4584126b222d5f7846efd95/`. The completed full native sweep retained all 34 frozen owned Assets inputs in `E4/Integration/native-cooking-full-suite-inputs.json`; its complete result is retained in `E4/Integration/native-cooking-full-suite-green.json`. Earlier full-suite results do not substitute for this sweep.

## Later workspot activation, not shipped by this core slice

Proposed exact identity: SpreadCookingCoals, explicitly authored residual heat without inherited permanent LightSource/Flicker. It provides cooking utility rather than a rest service. Existing torch lighting may use genuine thermal ignition conditions; do not redefine heat to suppress that supported interaction. Display cooking readiness separately from hot/cold appearance.

A prospective source projection found actual FieldStrips grain and reachable bare station positions in the fixed seeds, with the current F3 principal composition disabled solely for that projection. This is not registered v6 generation or native availability evidence. Keep one explicit station allowance, protected arrivals and required paths, original grain yield, and literal saved versions2–5. Native arrival timing and cold-state persistence must precede activation.

The retained [activation plan and census](Verification/QuestFreeExploration/E4/Cooking/ActivationPlan/ACTIVATION-PLAN.md) examined 89 eligible fields and 193 real rows. Conservative static paths avoiding the current nearby hostiles admitted 19/13/17 fields within 60 modeled material passes in seeds 1/64/1729. That supports initially keeping the existing cooling rate and refusing overly long approaches. It does not simulate NPC movement, prove safe travel or establish native arrival timing.

## Review and remaining acceptance

Root and independent source reviews found no blocker in the private core's exact station/input proof, default-false compatibility or one-to-one transformation. Every new refusal runs before item writes; legacy/finite branches share ownership validation. Paired tests cover heat/fuel boundaries, malformed/replaced parts, moved source, unchanged callbacks, absent station, actual field harvest, prepared-food use, capacity, repeat-recipe refusal and saved replacement graphs.

### Q1–Q4 and severity review

- Q1: admission and post-factory revalidation precede all inventory writes; existing rollback/after-commit order stays intact. Both legacy and finite sources share exact authority. Model catalog/prefab/style dispatches use the same single ID; pickup removes the ground form and drop/load restore it.
- Q2: cooking does not heal the player or grant another container. It is one-for-one, with the ordinary food weight and stacking contract. Only explicit new finite sources use the temperature threshold; old stations and saved objects keep their established behavior. No rest or material-clock rewrite was folded into this slice.
- Q3: actual unchanged callbacks are paired with moved/replaced station and ingredient parts; malformed/spent/nonfinite sources refuse; full saved replacement, no-station conservation, capacity and raw/other-food controls pass. Hidden/foreign/carried/custom-appearance objects cannot acquire the new ground form. Native real inputs separately prove payment and persistence.
- Q4: the pre-core source audits and RED artifacts remain historical evidence. Current native results are recorded above. The living doc distinguishes an available grain recipe from unregistered workspot placement and distinguishes tested model identity from distant readability.

🟡 No unresolved significant core/source/quantity/save defect found by root and independent review. The full native regression passes 21,584/21,584 with zero failures/skips; all 34 frozen inputs remain unchanged. ⚪ Full-zone food readability is limited by its small scale; distant recognition is deferred. Existing save-stream grain is not retrofitted with a recipe. The importer retains explicit partial outputs if a later import step fails; no transactional asset rollback is claimed.

Publication gates are complete: focused RED/GREEN, actual input/save witness, viewed frames, full native sweep, unchanged frozen inputs and exact owned-file ledger. Source registration, rest opt-out, truthful coals presentation, ordinary discovery, second variants and wider E4 remain separate unfinished work.

## Files and implementation record

Production changes are `CookingService`, `CampfirePart` and the two surgical blueprint deltas; the optional `SpreadCooking3DLibrary`, editor importer and five exact presentation dispatch hooks; and the existing isolated native audit's cooking mode. New tests cover 39 core cases, 16 art cases and 2 native mode guards. Original source art/export and explicit generated asset receipts are retained alongside tests, raw RED/GREEN output, independent reviews and future activation measurements. The root's exact publication path ledger excludes unrelated local work and Unity MCP logs.
