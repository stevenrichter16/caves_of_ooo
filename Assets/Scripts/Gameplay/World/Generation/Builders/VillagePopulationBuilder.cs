using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Populates a village zone with role-based NPCs (Elder, Merchant, Warden, Villagers)
    /// and decorative objects (Campfire, Well, MarketStall).
    /// Deterministic roles -- not random PopulationTable rolls.
    /// Priority: VERY_LATE (4000) — after all terrain and connectivity.
    /// </summary>
    public class VillagePopulationBuilder : IZoneBuilder
    {
        public string Name => "VillagePopulationBuilder";
        public int Priority => 4000;
        private const string StartingVillageZoneId = "Overworld.10.10.0";

        private PointOfInterest _poi;
        private SettlementManager _settlementManager;
        private static readonly (int dx, int dy)[] StartingChestOffsets =
        {
            (3, -1),
            (3, 0),
            (3, 1),
            (2, -1),
            (2, 1),
            (4, -1),
            (4, 0),
            (4, 1)
        };

        public VillagePopulationBuilder(PointOfInterest poi, SettlementManager settlementManager = null)
        {
            _poi = poi;
            _settlementManager = settlementManager;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return true;

            string settlementId = zone.ZoneID;
            var settlement = _settlementManager?.GetOrCreateSettlement(settlementId, _poi);
            RepairableSiteState mainWell = settlement?.GetSite(SettlementSiteDefinitions.MainWellSiteId);

            PlaceMainWell(zone, factory, openCells, settlementId, mainWell);

            Entity grimoireChest = null;
            if (zone.ZoneID == StartingVillageZoneId)
                grimoireChest = PlaceGrimoireChest(zone, factory, rng, openCells);

            // Place decor after the well and starting chest so the spawn hub stays stable.
            var decorTable = PopulationTable.VillageDecor();
            var decorBlueprints = decorTable.Roll(rng);
            foreach (var bp in decorBlueprints)
            {
                if (bp == "Well")
                    continue;
                Entity decor = PlaceEntity(zone, factory, rng, openCells, bp);
                if (bp == "Campfire" && decor != null)
                    SetupCampfire(zone, factory, decor, openCells);
            }

            RepairableSiteState ovenSite = settlement?.GetSite(SettlementSiteDefinitions.VillageOvenSiteId);
            PlaceOven(zone, factory, rng, openCells, settlementId, ovenSite);

            RepairableSiteState lanternSite = settlement?.GetSite(SettlementSiteDefinitions.VillageLanternSiteId);
            PlaceLantern(zone, factory, rng, openCells, settlementId, lanternSite);

            // Place a chest with grimoires.
            if (grimoireChest == null)
                grimoireChest = PlaceGrimoireChest(zone, factory, rng, openCells);

            // Place deterministic wooden barrel layouts to demonstrate fire propagation
            PlaceBarrelLayouts(zone, factory, rng, openCells);

            // Phase E integration sandbox: only in the starting zone to avoid cluttering all villages.
            if (zone.ZoneID == "Overworld.10.10.0")
            {
                PlaceDebugMaterialSandbox(zone, factory, rng, openCells);
                PlaceCompassStones(zone, factory, openCells, grimoireChest);
            }

            // Deterministic NPC roles
            Entity elder = PlaceNPC(zone, factory, rng, openCells, "Elder", settlementId);
            if (mainWell != null)
                SetConversation(elder, "Elder_Well_1");

            // 1 Merchant (always)
            Entity merchant = PlaceNPC(zone, factory, rng, openCells, "Merchant", settlementId);
            StockMerchant(merchant, factory);

            // 0-1 Tinker (70% chance)
            if (rng.Next(100) < 70)
                PlaceNPC(zone, factory, rng, openCells, "Tinker", settlementId);

            // Warden (always if lantern site exists, else 60% chance)
            if (lanternSite != null)
            {
                Entity warden = PlaceNPC(zone, factory, rng, openCells, "Warden", settlementId);
                SetConversation(warden, "Warden_Lantern_1");
            }
            else if (rng.Next(100) < 60)
            {
                PlaceNPC(zone, factory, rng, openCells, "Warden", settlementId);
            }

            if (mainWell != null)
            {
                PlaceNPC(zone, factory, rng, openCells, "WellKeeper", settlementId);
                PlaceNPC(zone, factory, rng, openCells, "Farmer", settlementId);
            }

            // 1 Scribe (always)
            Entity scribe = PlaceNPC(zone, factory, rng, openCells, "Scribe", settlementId);
            SetConversation(scribe, "Scribe_1");

            // 2-4 Villagers
            int villagerCount = rng.Next(2, 5);
            for (int i = 0; i < villagerCount; i++)
                PlaceNPC(zone, factory, rng, openCells, "Villager", settlementId);

            return true;
        }

        private Entity PlaceNPC(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string blueprint, string settlementId)
        {
            Entity npc = PlaceEntity(zone, factory, rng, openCells, blueprint);
            if (npc != null && _poi?.Faction != null)
            {
                // Override faction to match POI
                npc.SetTag("Faction", _poi.Faction);
            }

            if (npc != null && !string.IsNullOrEmpty(settlementId))
                npc.Properties["SettlementId"] = settlementId;

            return npc;
        }

        private Entity PlaceEntity(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string blueprint)
        {
            if (openCells.Count == 0) return null;

            int idx = rng.Next(openCells.Count);
            var (x, y) = openCells[idx];

            Entity entity = TryCreateEntity(factory, blueprint);
            if (entity != null)
                zone.AddEntity(entity, x, y);

            openCells.RemoveAt(idx);
            return entity;
        }

        private Entity PlaceGrimoireChest(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells)
        {
            Entity chest = zone.ZoneID == StartingVillageZoneId
                ? PlaceEntityNearVillageSquare(zone, factory, openCells, "Chest", StartingChestOffsets)
                : PlaceEntity(zone, factory, rng, openCells, "Chest");
            if (chest == null) return null;

            var container = chest.GetPart<ContainerPart>();
            if (container == null) return chest;

            Entity grimoire = TryCreateEntity(factory, "PurifyWaterGrimoire");
            if (grimoire != null)
                container.AddItem(grimoire);

            Entity mendingGrimoire = TryCreateEntity(factory, "MendingRiteGrimoire");
            if (mendingGrimoire != null)
                container.AddItem(mendingGrimoire);

            Entity kindleGrimoire = TryCreateEntity(factory, "KindleRiteGrimoire");
            if (kindleGrimoire != null)
                container.AddItem(kindleGrimoire);

            Entity kindleSpell = TryCreateEntity(factory, "KindleGrimoire");
            if (kindleSpell != null)
                container.AddItem(kindleSpell);

            Entity quenchSpell = TryCreateEntity(factory, "QuenchGrimoire");
            if (quenchSpell != null)
                container.AddItem(quenchSpell);

            Entity conflagrationSpell = TryCreateEntity(factory, "ConflagrationGrimoire");
            if (conflagrationSpell != null)
                container.AddItem(conflagrationSpell);

            Entity iceLanceSpell = TryCreateEntity(factory, "IceLanceGrimoire");
            if (iceLanceSpell != null)
                container.AddItem(iceLanceSpell);

            Entity acidSpraySpell = TryCreateEntity(factory, "AcidSprayGrimoire");
            if (acidSpraySpell != null)
                container.AddItem(acidSpraySpell);

            Entity arcBoltSpell = TryCreateEntity(factory, "ArcBoltGrimoire");
            if (arcBoltSpell != null)
                container.AddItem(arcBoltSpell);

            Entity rimeNovaSpell = TryCreateEntity(factory, "RimeNovaGrimoire");
            if (rimeNovaSpell != null)
                container.AddItem(rimeNovaSpell);

            Entity thunderclapSpell = TryCreateEntity(factory, "ThunderclapGrimoire");
            if (thunderclapSpell != null)
                container.AddItem(thunderclapSpell);

            Entity emberVeinSpell = TryCreateEntity(factory, "EmberVeinGrimoire");
            if (emberVeinSpell != null)
                container.AddItem(emberVeinSpell);

            Entity kindleFlameSpell = TryCreateEntity(factory, "KindleFlameGrimoire");
            if (kindleFlameSpell != null)
                container.AddItem(kindleFlameSpell);

            Entity dryingBreezeSpell = TryCreateEntity(factory, "DryingBreezeGrimoire");
            if (dryingBreezeSpell != null)
                container.AddItem(dryingBreezeSpell);

            Entity hearthwarmSpell = TryCreateEntity(factory, "HearthwarmGrimoire");
            if (hearthwarmSpell != null)
                container.AddItem(hearthwarmSpell);

            Entity conjureWaterSpell = TryCreateEntity(factory, "ConjureWaterGrimoire");
            if (conjureWaterSpell != null)
                container.AddItem(conjureWaterSpell);

            Entity chillDraftSpell = TryCreateEntity(factory, "ChillDraftGrimoire");
            if (chillDraftSpell != null)
                container.AddItem(chillDraftSpell);

            Entity wardGleamSpell = TryCreateEntity(factory, "WardGleamGrimoire");
            if (wardGleamSpell != null)
                container.AddItem(wardGleamSpell);

            return chest;
        }

        private Entity PlaceEntityNearVillageSquare(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            string blueprint,
            IReadOnlyList<(int dx, int dy)> preferredOffsets)
        {
            var (centerX, centerY) = VillageBuilder.GetVillageSquareCenter();
            if (preferredOffsets != null)
            {
                for (int i = 0; i < preferredOffsets.Count; i++)
                {
                    int targetX = centerX + preferredOffsets[i].dx;
                    int targetY = centerY + preferredOffsets[i].dy;
                    Entity placed = TryPlaceEntityAt(zone, factory, openCells, blueprint, targetX, targetY);
                    if (placed != null)
                        return placed;
                }
            }

            for (int radius = 0; radius < Math.Max(Zone.Width, Zone.Height); radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        int targetX = centerX + dx;
                        int targetY = centerY + dy;
                        Entity placed = TryPlaceEntityAt(zone, factory, openCells, blueprint, targetX, targetY);
                        if (placed != null)
                            return placed;
                    }
                }
            }

            return null;
        }

        private Entity TryPlaceEntityAt(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            string blueprint,
            int x,
            int y)
        {
            if (!zone.InBounds(x, y))
                return null;

            int openIndex = openCells.IndexOf((x, y));
            if (openIndex < 0)
                return null;

            Cell cell = zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable())
                return null;

            Entity entity = TryCreateEntity(factory, blueprint);
            if (entity == null)
                return null;

            zone.AddEntity(entity, x, y);
            openCells.RemoveAt(openIndex);
            return entity;
        }

        // Elemental Crossroads spawn hub polish: 4 compass stones around the
        // grimoire chest, one per cardinal direction. Each is its own blueprint
        // (CompassStoneNorth/East/South/West) so the look query picks up the
        // direction via DisplayName — no per-instance description plumbing.
        // Walks 2 cells outward from the chest in each cardinal; if that tile
        // is blocked, silently skips (compass stones are decorative, missing
        // one is harmless).
        private static readonly (int dx, int dy, string blueprint)[] CompassStoneOffsets =
        {
            ( 0, -2, "CompassStoneNorth"),
            ( 2,  0, "CompassStoneEast"),
            ( 0,  2, "CompassStoneSouth"),
            (-2,  0, "CompassStoneWest"),
        };

        private void PlaceCompassStones(Zone zone, EntityFactory factory,
            List<(int x, int y)> openCells, Entity anchor)
        {
            if (zone == null || factory == null || anchor == null) return;

            Cell anchorCell = zone.GetEntityCell(anchor);
            if (anchorCell == null) return;

            for (int i = 0; i < CompassStoneOffsets.Length; i++)
            {
                var (dx, dy, blueprint) = CompassStoneOffsets[i];
                int x = anchorCell.X + dx;
                int y = anchorCell.Y + dy;
                if (!zone.InBounds(x, y)) continue;

                Cell cell = zone.GetCell(x, y);
                if (cell == null || !cell.IsPassable()) continue;

                Entity stone = TryCreateEntity(factory, blueprint);
                if (stone == null) continue;

                zone.AddEntity(stone, x, y);
                openCells.Remove((x, y));
            }
        }

        // Wooden barrel layouts for fire-propagation showcase. Each layout is a list
        // of (dx, dy) offsets from an anchor cell. Layouts are placed with a buffer
        // between them so igniting one cluster cannot chain into another.
        private static readonly (int dx, int dy)[][] BarrelLayouts =
        {
            // 1. Chain — 4 in a row (linear propagation)
            new (int dx, int dy)[] { (0, 0), (1, 0), (2, 0), (3, 0) },
            // 2. Cluster — 2x2 pack (rapid mass ignition)
            new (int dx, int dy)[] { (0, 0), (1, 0), (0, 1), (1, 1) },
            // 3. Cross — 5 barrels in a plus (branching propagation)
            new (int dx, int dy)[] { (1, 0), (0, 1), (1, 1), (2, 1), (1, 2) },
            // 4. Diagonal — 4 stepping down (Chebyshev / 8-direction propagation)
            new (int dx, int dy)[] { (0, 0), (1, 1), (2, 2), (3, 3) },
            // 5. Gap — two pairs separated by 1 empty cell (isolation demo)
            new (int dx, int dy)[] { (0, 0), (1, 0), (3, 0), (4, 0) }
        };

        private const int BarrelLayoutBuffer = 2;
        private const int BarrelLayoutPlacementAttempts = 50;
        private const int MaterialSandboxPlacementAttempts = 50;

        // Phase E material sandbox -- exercises every grimoire and every material
        // reaction in one contiguous plot. See Plan.md Phase E checklist.
        //
        // Layout (5 wide x 3 tall):
        //
        //   col 0        col 1         col 2         col 3      col 4
        //   Torch        WoodenBarrel  WaterPuddle   Dagger     ChainMail
        //   RawMeat      RawMeat       Starapple     LongSword  IronHelmet
        //   LanternOil   LanternOil    LanternOil    -          -
        //
        // Row 0  - ignition/water/metal block (Kindle Flame, Conjure Water, Drying
        //          Breeze, Ice Lance, Ward Gleam, Chill Draft).
        // Row 1  - cooking targets (Hearthwarm, oil-chain cooking) + metal cluster
        //          continuation (Arc Bolt, Thunderclap).
        // Row 2  - oil fuse: lighting LanternOil(0,2) chains along row 2 and
        //          radiates up into row 1 to cook the food (Plan.md:289 showcase).
        private static readonly (string blueprint, int dx, int dy)[] MaterialSandboxLayout =
        {
            ("Torch",        0, 0),
            ("WoodenBarrel", 1, 0),
            ("WaterPuddle",  2, 0),
            ("Dagger",       3, 0),
            ("ChainMail",    4, 0),
            ("RawMeat",      0, 1),
            ("RawMeat",      1, 1),
            ("Starapple",    2, 1),
            ("LongSword",    3, 1),
            ("IronHelmet",   4, 1),
            ("LanternOil",   0, 2),
            ("LanternOil",   1, 2),
            ("LanternOil",   2, 2),
        };

        private void PlaceBarrelLayouts(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells)
        {
            var reservedCells = new HashSet<(int x, int y)>();

            for (int i = 0; i < BarrelLayouts.Length; i++)
                TryPlaceBarrelLayout(zone, factory, rng, openCells, BarrelLayouts[i], reservedCells);
        }

        private bool TryPlaceBarrelLayout(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, (int dx, int dy)[] layout,
            HashSet<(int x, int y)> reservedCells)
        {
            for (int attempt = 0; attempt < BarrelLayoutPlacementAttempts; attempt++)
            {
                if (openCells.Count == 0)
                    return false;

                int idx = rng.Next(openCells.Count);
                var (anchorX, anchorY) = openCells[idx];

                if (!CanPlaceLayoutAt(zone, anchorX, anchorY, layout, reservedCells))
                    continue;

                // Place barrels and reserve their buffered footprint
                for (int i = 0; i < layout.Length; i++)
                {
                    int x = anchorX + layout[i].dx;
                    int y = anchorY + layout[i].dy;

                    Entity barrel = TryCreateEntity(factory, "WoodenBarrel");
                    if (barrel != null)
                        zone.AddEntity(barrel, x, y);

                    openCells.Remove((x, y));

                    for (int by = -BarrelLayoutBuffer; by <= BarrelLayoutBuffer; by++)
                        for (int bx = -BarrelLayoutBuffer; bx <= BarrelLayoutBuffer; bx++)
                            reservedCells.Add((x + bx, y + by));
                }

                return true;
            }

            return false;
        }

        private bool CanPlaceLayoutAt(Zone zone, int anchorX, int anchorY,
            (int dx, int dy)[] layout, HashSet<(int x, int y)> reservedCells)
        {
            for (int i = 0; i < layout.Length; i++)
            {
                int x = anchorX + layout[i].dx;
                int y = anchorY + layout[i].dy;

                if (!zone.InBounds(x, y))
                    return false;

                Cell cell = zone.GetCell(x, y);
                if (cell == null || !cell.IsPassable())
                    return false;

                if (reservedCells.Contains((x, y)))
                    return false;
            }

            return true;
        }

        private void PlaceDebugMaterialSandbox(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells)
        {
            for (int attempt = 0; attempt < MaterialSandboxPlacementAttempts; attempt++)
            {
                if (openCells.Count == 0)
                    return;

                int idx = rng.Next(openCells.Count);
                var (anchorX, anchorY) = openCells[idx];
                if (!CanPlaceDebugMaterialSandboxAt(zone, anchorX, anchorY))
                    continue;

                for (int i = 0; i < MaterialSandboxLayout.Length; i++)
                {
                    var entry = MaterialSandboxLayout[i];
                    int x = anchorX + entry.dx;
                    int y = anchorY + entry.dy;
                    Entity entity = TryCreateEntity(factory, entry.blueprint);
                    if (entity != null)
                        zone.AddEntity(entity, x, y);

                    openCells.Remove((x, y));
                }

                return;
            }
        }

        private bool CanPlaceDebugMaterialSandboxAt(Zone zone, int anchorX, int anchorY)
        {
            for (int i = 0; i < MaterialSandboxLayout.Length; i++)
            {
                var entry = MaterialSandboxLayout[i];
                int x = anchorX + entry.dx;
                int y = anchorY + entry.dy;
                if (!zone.InBounds(x, y))
                    return false;

                Cell cell = zone.GetCell(x, y);
                if (cell == null || !cell.IsPassable())
                    return false;
            }

            return true;
        }

        private static readonly (int dx, int dy)[] CardinalOffsets =
            { (0, -1), (1, 0), (0, 1), (-1, 0) };

        private void PlaceMainWell(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            string settlementId,
            RepairableSiteState mainWell)
        {
            var (wellX, wellY) = VillageBuilder.GetVillageSquareCenter();
            if (!zone.InBounds(wellX, wellY))
                return;

            Entity entity = TryCreateEntity(factory, "Well");
            if (entity == null)
                return;

            if (!string.IsNullOrEmpty(settlementId))
                entity.Properties["SettlementId"] = settlementId;

            if (mainWell != null)
            {
                entity.Properties["SettlementSiteId"] = mainWell.SiteId;

                var wellPart = new WellSitePart();
                wellPart.SettlementId = settlementId;
                wellPart.SiteId = mainWell.SiteId;
                entity.AddPart(wellPart);

                SettlementSiteVisuals.ApplyToEntity(entity, mainWell);
            }

            zone.AddEntity(entity, wellX, wellY);
            openCells.Remove((wellX, wellY));

            // Place ground markers on cardinal adjacent cells
            if (mainWell != null)
                PlaceWellGroundMarkers(zone, factory, openCells, wellX, wellY, settlementId, mainWell);
        }

        private void PlaceWellGroundMarkers(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            int wellX,
            int wellY,
            string settlementId,
            RepairableSiteState mainWell)
        {
            if (!factory.Blueprints.ContainsKey("WellGroundMarker"))
                return;

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = wellX + CardinalOffsets[i].dx;
                int my = wellY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = TryCreateEntity(factory, "WellGroundMarker");
                if (marker == null)
                    continue;

                if (!string.IsNullOrEmpty(settlementId))
                    marker.Properties["SettlementId"] = settlementId;
                marker.Properties["SettlementSiteId"] = mainWell.SiteId;
                marker.SetTag("WellGroundMarker", "");

                SettlementSiteVisuals.ApplyToEntity(marker, mainWell);
                zone.AddEntity(marker, mx, my);
            }
        }

        private void PlaceOven(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string settlementId, RepairableSiteState ovenSite)
        {
            if (openCells.Count == 0)
                return;

            Entity entity = PlaceEntity(zone, factory, rng, openCells, "Oven");
            if (entity == null)
                return;

            if (!string.IsNullOrEmpty(settlementId))
                entity.Properties["SettlementId"] = settlementId;

            if (ovenSite != null)
            {
                entity.Properties["SettlementSiteId"] = ovenSite.SiteId;

                var ovenPart = new OvenSitePart();
                ovenPart.SettlementId = settlementId;
                ovenPart.SiteId = ovenSite.SiteId;
                entity.AddPart(ovenPart);

                SettlementSiteVisuals.ApplyToEntity(entity, ovenSite);

                Cell ovenCell = zone.GetEntityCell(entity);
                if (ovenCell != null)
                    PlaceOvenGroundMarkers(zone, factory, openCells, ovenCell.X, ovenCell.Y, settlementId, ovenSite);
            }
        }

        private void PlaceOvenGroundMarkers(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            int ovenX,
            int ovenY,
            string settlementId,
            RepairableSiteState ovenSite)
        {
            if (!factory.Blueprints.ContainsKey("OvenGroundMarker"))
                return;

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = ovenX + CardinalOffsets[i].dx;
                int my = ovenY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = TryCreateEntity(factory, "OvenGroundMarker");
                if (marker == null)
                    continue;

                if (!string.IsNullOrEmpty(settlementId))
                    marker.Properties["SettlementId"] = settlementId;
                marker.Properties["SettlementSiteId"] = ovenSite.SiteId;
                marker.SetTag("OvenGroundMarker", "");

                SettlementSiteVisuals.ApplyToEntity(marker, ovenSite);
                zone.AddEntity(marker, mx, my);
            }
        }

        private void PlaceLantern(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string settlementId, RepairableSiteState lanternSite)
        {
            if (openCells.Count == 0)
                return;

            Entity entity = PlaceEntity(zone, factory, rng, openCells, "WatchLantern");
            if (entity == null)
                return;

            if (!string.IsNullOrEmpty(settlementId))
                entity.Properties["SettlementId"] = settlementId;

            if (lanternSite != null)
            {
                entity.Properties["SettlementSiteId"] = lanternSite.SiteId;

                var lanternPart = new LanternSitePart();
                lanternPart.SettlementId = settlementId;
                lanternPart.SiteId = lanternSite.SiteId;
                entity.AddPart(lanternPart);

                SettlementSiteVisuals.ApplyToEntity(entity, lanternSite);

                Cell lanternCell = zone.GetEntityCell(entity);
                if (lanternCell != null)
                    PlaceLanternGroundMarkers(zone, factory, openCells, lanternCell.X, lanternCell.Y, settlementId, lanternSite);
            }
        }

        private void PlaceLanternGroundMarkers(
            Zone zone,
            EntityFactory factory,
            List<(int x, int y)> openCells,
            int lanternX,
            int lanternY,
            string settlementId,
            RepairableSiteState lanternSite)
        {
            if (!factory.Blueprints.ContainsKey("LanternGroundMarker"))
                return;

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = lanternX + CardinalOffsets[i].dx;
                int my = lanternY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = TryCreateEntity(factory, "LanternGroundMarker");
                if (marker == null)
                    continue;

                if (!string.IsNullOrEmpty(settlementId))
                    marker.Properties["SettlementId"] = settlementId;
                marker.Properties["SettlementSiteId"] = lanternSite.SiteId;
                marker.SetTag("LanternGroundMarker", "");

                SettlementSiteVisuals.ApplyToEntity(marker, lanternSite);
                zone.AddEntity(marker, mx, my);
            }
        }

        private void SetupCampfire(Zone zone, EntityFactory factory, Entity campfire,
            List<(int x, int y)> openCells)
        {
            campfire.AddPart(new CampfirePart());

            Cell cell = zone.GetEntityCell(campfire);
            if (cell == null)
                return;

            if (!factory.Blueprints.ContainsKey("CampfireGroundMarker"))
                return;

            // Place warm-glow ground markers on cardinal cells
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = cell.X + CardinalOffsets[i].dx;
                int my = cell.Y + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell markerCell = zone.GetCell(mx, my);
                if (markerCell == null || !markerCell.IsPassable())
                    continue;

                Entity marker = TryCreateEntity(factory, "CampfireGroundMarker");
                if (marker == null)
                    continue;

                zone.AddEntity(marker, mx, my);
            }
        }

        private static readonly string[] MerchantRepairStock =
        {
            "SilverSand",
            "FireClay",
            "WardOil",
            "Antidote",
            "BurnSalve",
            "HealingTonic"
        };

        private void StockMerchant(Entity merchant, EntityFactory factory)
        {
            if (merchant == null)
                return;

            var inventory = merchant.GetPart<InventoryPart>();
            if (inventory == null)
                return;

            for (int i = 0; i < MerchantRepairStock.Length; i++)
            {
                Entity item = TryCreateEntity(factory, MerchantRepairStock[i]);
                if (item != null)
                    inventory.AddObject(item);
            }
        }

        private static Entity TryCreateEntity(EntityFactory factory, string blueprint)
        {
            if (factory == null || string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint))
                return null;

            return factory.CreateEntity(blueprint);
        }

        private void SetConversation(Entity entity, string conversationId)
        {
            var conversation = entity?.GetPart<ConversationPart>();
            if (conversation != null && !string.IsNullOrEmpty(conversationId))
                conversation.ConversationID = conversationId;
        }

        private List<(int x, int y)> GatherOpenCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                    cells.Add((x, y));
            });
            return cells;
        }
    }
}
