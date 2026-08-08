# STARTING TOWN — Kyakukya Grows Up

> Status: ✅ SHIPPED (same-day plan → implementation, 2026-08-08).
> Requested after first playtest: "a large starting town with all
> crafting tools available within the town … scattered among the town
> like they would be in a real town. we also need shops … different
> shops for different items, weapons, enchantments, tonics, etc."
> Companion log: `Docs/BIOME-OVERHAUL-LOG.md` (same rails).

## 1. Design

The starting village becomes a TOWN: more houses, and five purpose-built
shops scattered among them, each a stamp-built building with its own
keeper, themed stock, and (where it fits the trade) its crafting
station under the same roof — the smith owns the forge, the apothecary
owns the still. The existing public square (well, grimoire chest,
square-side still+forge, oven, lantern, shrine, compass stones) stays
untouched — those carry the settlement-repair quest ladder and their
own tests; a town square with public workbenches PLUS pro shops is
real-town texture, not redundancy.

### The five shops

| Shop | Keeper (new blueprint) | Wallet | Station in-house | Stock table |
|---|---|---|---|---|
| The Smithy | Weaponsmith | 300 | TinkersForge | WeaponsmithStock — weapon ladder + forge components |
| The Bulwark | Armorer | 300 | — | ArmorerStock — full armor ladder incl. 4 NEW pieces + rare PlateArmor shelf |
| The Alembic | Apothecary | 250 | AlchemyStill | ApothecaryStock — tonics, cures, brew reagents |
| The Inkwell | Arcanist | 400 | — | ArcanistStock — grimoires, schematics, minerals, ink ("enchantments") |
| The Larder | Provisioner | 200 | — | ProvisionerStock — food, torches, oil, seeds |

All keepers: Villager lineage (faction, Brain, GivesRep), own
conversations (`Shopkeepers.json`, trade via the auto-injected
[Let's trade.]), `NoRandomStock` tag so TradeStockBuilder doesn't mix
random junk into themed shelves.

### New items (the armor ladder the plan promised in §4.3, never shipped)
LeatherCap (Head AV1, 8) · IronshodBoots (Feet AV2 SP−5, 28) ·
WardedCloak (Back DV2, 40) · IronBuckler (Hand AV2/DV1, 45).
Head/Feet/Back/Hand finally have more than one item each.

### Mechanics added
1. **`shop:Blueprint:Table` stamp marker** — spawns the keeper and
   rolls their stock table into their inventory at build time.
2. **`StampCatalog.Town()`** — five shop stamps (Chance 100) placed by
   a LandmarkBuilder at priority **3860** (AFTER the river at 3850 so
   the water is on the map before footprint checks) with
   `maxStructures: 5` (new ctor param; the ambient cap of 2 stays the
   default). Starting village only (`CreateVillagePipeline` gates on
   the zone ID).
3. **Water guard for ALL stamps** — FootprintClear rejects cells
   holding a LiquidPoolPart entity (pre-existing exposure: a hermit
   hut could straddle the river).
4. **VillagePopulationBuilder respects `GenReservedCells`** — same fix
   PopulationBuilder got in A2, so the oven/compass stones/NPCs can't
   land inside a shop.
5. **Large-town layout** — VillageBuilder gains `largeTown`: organic
   houses 3-5 → 6-9 (the shops are interspersed among them).
6. **Shop restock** — `TraderRestockSystem.Factory` (bootstrap-wired,
   null = old drams-only behavior). Keepers carry a `ShopStockTable`
   string property; on the 300-turn restock tick, a keeper whose
   shelf has run low (< 3 items) re-rolls their table. Shops stay
   alive across a long game.

## 2. Files
- MOD `Objects.json`: 5 keepers + 4 armor pieces (290 objects)
- NEW `Conversations/Shopkeepers.json`: 5 conversations
- MOD `LootTables.json`: 5 stock tables (19 total)
- MOD `LandmarkBuilder.cs`: shop: marker, Town() catalog, maxStructures,
  water guard
- MOD `VillageBuilder.cs`: largeTown · `OverworldZoneManager.cs`: wiring
- MOD `VillagePopulationBuilder.cs`: reserved-cell filter
- MOD `TradeStockBuilder.cs`: NoRandomStock skip
- MOD `TraderRestockSystem.cs`: Factory + shelf refill ·
  `GameBootstrap.cs`: wire
- NEW `Tests/.../StartingTownTests.cs`

## 3. Deliberate scope choices
- Square-side public still/forge KEPT alongside shop stations
  (settlement tests pin their presence; real towns have both).
- Shop stock lives on the keeper (the proven trade UI), not in
  display containers — no theft mechanic exists to make shelf-chests
  meaningful. MarketStall props give the rooms their shop look.
- Non-starting villages stay hamlets (counter-checked).
