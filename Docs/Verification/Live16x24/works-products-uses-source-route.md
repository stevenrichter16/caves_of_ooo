# North works ordinary-use route — 2026-10-24 16:01 continuation

Read-only source preflight; no Unity, world mutation, repository edits or git actions. Continue the verified real plot F5 checkpoint rather than generating a replacement world. Current checkpoint details supplied by the completed plot pass are zone `Overworld.11.8.0`, player `(18,7)`, tick1550,36drams,40/40HP, one carried resin and one previously brewed burning coating. Check actual loaded state before using those values. The plot packet's reachable ordinary gate is accepted; this route targets the existing seven works products, not another plot replay or new art area.

## Efficient bounded order

1. Walk south into `Overworld.11.9.0`. If present near the northern entry, inspect and harvest the BerryBush before crossing the site. Next visit the old crate in the west, then move toward the eastern forage/still cluster. Observe actual threats/stock first. Coordinates below are historical seed64 observations, not fresh location authority.
2. At each live forage source, stand within one cell and use **L → target cell → Enter → Harvest**, or **C → direction → Harvest**. Actual Harvestable plants remove themselves and normally put products directly into inventory. Keep the raw MendleafSprig until the concentration supply decision is settled.
3. Examine each actual carried product through **I → Tab until Inventory is active → item → Enter → Examine**. Popup shortcuts depend on row order: select the displayed label with arrows/Enter. For world art, use ordinary **Drop**, close menus, then **L → its cell → Enter → `<< everything here` → exact product → Examine**, and Take afterward. Do not substitute the plant, top occupant, pile summary, or pickup/container F1 text for the exact product reader. A normal drop is valid ground art evidence but is not a harvest-overflow claim.
4. Within the still's3×3 neighborhood, if one carried stack actually contains at least2 MendleafSprig, select **`still: 2 mendleaf -> 1 vital:2`**. Capture raw sprig first, then the ConcentratedMendleaf result before brewing it. Two separate stacks are not equivalent to the selected stack's count; normally acquired equivalent stock merges, but verify actual inventory.
5. **I → Tab until Crafting is active → B → C → Up/Down to intended reagent → Space → Enter** makes one flask. C clears old picks; clear between recipes. Do not press Shift+Enter unless intentionally batching. Record actual input decrement, output display name, Tonic healing fields and optional BrewItem Form/EffectsRaw. One-flask brewing does not require a still; non-Food multi-flask batches do. The current Crafting path does not itself advance a turn, so record rather than invent a turn cost.
6. Eat one actual WildBerries after its portrait(s). At40/40HP, a unit consumed plus eating message establishes food use; no HP gain is expected from clamped healing. If a safe active tonic-use gate is useful, StoneburrSeed's stoneskin tonic is the beneficial choice: examine first, then use its displayed drink/apply action and observe the actual status. Do not drink a frozen or burning coating merely to close a visual gate.
7. Save/load the genuinely changed inventory, depletion and brew metadata; retain exact distinctions between acquired/used/visually inspected. If only one sprig remains available, report concentration's finite-supply shortfall instead of rerolling or granting anything.

## Historical positions and finite ownership

From `Docs/Verification/Live16x24/north-works-native-entry.json` and the archived seed64 census:

| Historical owner | Zone11.9 cell | Genuine current-source behavior |
| --- | --- | --- |
| BerryBush |60,1|Harvest produces1–3 WildBerries; normally packed. Source removed.|
| Crate |21,5|Historical unlocked container held GoldCoin×4. Current contents must be checked.|
| MendleafPlant |55,19|Harvest produces1–2 MendleafSprig; two are **not guaranteed**.|
| StoneburrPatch |60,18|Harvest produces1–2 StoneburrSeed. This burr is a binding reagent, not a plantable seed.|
| AlchemyStill |62,17|Nearby still enables the two-sprig recipe and non-Food batches.|
| FrostLichenPatch |64,18|Harvest produces1–2 FrostLichen.|
| MarlbackPatchbearer |68,17|Historical initial position only; actor can move/use finite stock.|

The old north-vegetation berry harvest was a separate historical audit. Do not infer either current depletion or current availability from that earlier isolated session. Actual current owners and receipts decide.

## Real finite-supply fallback

The detached seed64 census records **Farmer Kesset Reedfall at `Overworld.12.10.0 (66,5)` carrying MendleafSprig×1**. This is a legitimate purchase candidate: TradeSystem exposes carried items with Commerce, and the conversation injects the trade option. Travel there normally only if the works plant gives one and the detour is worthwhile. Use adjacent **Chat → `[Let's trade.]` → trader pane → sprig → Enter → confirm Yes**; inspect actual current stock and price. Do not assume the farmer remains at that coordinate, friendly, or stocked. This is historical supplemental random stock, not a guaranteed FarmerStock entry.

`TradeStockBuilder` includes MendleafSprig, FrostLichen and StoneburrSeed in its2–5 random supplemental items. Existing nearby traders may therefore have usable surplus, but this is an observation opportunity, not permission to regenerate/restock them. `HermitStock` has a100% initial sprig entry, while `ApothecaryStock` has60%; **no Hermit was confirmed in the six-zone seed64 census**, so neither supplies a verified short local detour. No authored shop/loot entry for ConcentratedMendleaf was found. If the real farmer's sprig is unavailable and no existing observed trader has one, leave the concentration gate explicitly pending.

## Brew truth and useful small recipes

| One selected ingredient | Actual relevant outcome |
| --- | --- |
| StoneburrSeed |Stoneskin tonic; binding:2, typically Stoneskin:2; effect factory's default30-turn duration.|
| FrostLichen |Frozen coating; cold:2. Read its actual result; do not imply safety from the neutral flask.|
| ConcentratedMendleaf |Mending tonic with2d4 healing. Healing-only output has no BrewItem part; capture Tonic.Healing instead.|
| MendleafSprig |Mending tonic with1d4 healing; consuming the only raw sprig prevents concentration.|
| PitchpodResin |Burning coating; already observed in the plot pass.|
| Resin + StoneburrSeed |Combined Burning/Stoneskin coating if those real ingredients are available. Optional, not necessary for seven identity coverage.|

Heat forbids the frost rule: resin+lichen does not prove a fire-and-ice brew. Normal single Enter consumes one unit from every marked reagent stack. The same BrewedTonic blueprint covers several outcomes; record display name/form/effects rather than treating blueprint equality as recipe proof. Explicit Food forms use fallback art by design; the known healing-only lost-Form metadata gap is outside this art-use packet. No need to broaden this pass into potion redesign.

## Exact ground coin route

Ordinary container Take and ground Pickup both convert GoldCoin to **5drams per unit** and remove the coin; they do not create carried GoldCoin. Inspect the actual crate contents before taking. For native ground coin art, leave the coins inside, stand adjacent and select the existing **Break** action repeatedly through the normal menu until the crate breaks. The real Crate has10HP/default zero hardness, but damage varies; do not assume a fixed swing count. Each strike closes the menu and spends a turn. This permanently destroys the crate within the isolated run and advances nearby actors.

DestructionSystem spills the same contained item/stack into the crate cell and clears its inventory custody without pickup conversion. Select that exact ground GoldCoin through Look/`everything here`, Examine before Take, then verify drams increase by5×actual stack count and no carried coin. Historical4coins would add20drams, but use the actual current count. A container/pickup F1 description is text-only and does not close the portrait gate. If actual contents or safety prevent this route, retain explicit ground-coin visual debt.

## Source anchors

- `Assets/Scripts/Gameplay/Items/HarvestablePart.cs:127–178`: random finite yield, pack-first output, source removal.
- `Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs:42–54`; `PreparationActions.cs:59–61`: exact2-unit selected-stack recipe and still validation.
- `Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs:104–125`: actual3×3 proximity test.
- `Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs:284–317,407–434`; `Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs:103–122`: actual controls and single/batch still rules.
- `Assets/Resources/Content/Data/Alchemy/BrewRules.json`; `Assets/Scripts/Gameplay/Alchemy/BrewingService.cs:320–365`; `BrewItemPart.cs:30–75`: actual recipe/form/healing metadata.
- `Assets/Scripts/Gameplay/Items/FoodPart.cs:38–85`; `TonicPart.cs:37–69`; `TonicEffectFactory.cs:77–82`: food consumption, actual displayed tonic action and Stoneskin duration.
- `Assets/Scripts/Presentation/UI/InventoryUI.cs:736–743,1228–1231,1359–1362`: Drop and Separate-one controls. Do not assume an item's mnemonic is its current popup row key.
- `Assets/Scripts/Presentation/Input/InputHandler.Clarity.cs:37–47`: pickup/container F1 is a text decision reader, not native subject art.
- `Assets/Scripts/Presentation/Input/InputHandler.cs:3286–3296`; `Assets/Scripts/Gameplay/World/DestructionSystem.cs:339–355`; `Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/TakeFromContainerCommand.cs:115–145`: ordinary breaking, real spill, exact currency conversion.
- `Assets/Scripts/Gameplay/Economy/TradeSystem.cs:385–401`; `Assets/Scripts/Gameplay/Conversations/ConversationManager.cs:215`; `Assets/Scripts/Presentation/UI/TradeUI.cs:292`: carried sellable stock and actual trade route.
- `Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs:25–39,85–100`; `Assets/Resources/Content/Data/Loot/LootTables.json:739,2568,2881`: random supplementary stock versus authored Farmer/Apothecary/Hermit stock.
- `Docs/Verification/Live16x24/connected-two-seed-census-summary.json:6004`: historical farmer's one sprig; raw census and historical native entry provide positions, never current acceptance.

This handoff is source/controls evidence only. No new ordinary screenshot, current inventory or native test acceptance is claimed. Works doc's latest16:01 continuation supersedes its historical locked/plot-pending wording.
