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

        private PointOfInterest _poi;
        private SettlementManager _settlementManager;

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

            // Place decor first, but keep the well deterministic.
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

            PlaceMainWell(zone, factory, openCells, settlementId, mainWell);

            RepairableSiteState ovenSite = settlement?.GetSite(SettlementSiteDefinitions.VillageOvenSiteId);
            PlaceOven(zone, factory, rng, openCells, settlementId, ovenSite);

            RepairableSiteState lanternSite = settlement?.GetSite(SettlementSiteDefinitions.VillageLanternSiteId);
            PlaceLantern(zone, factory, rng, openCells, settlementId, lanternSite);

            // Place a chest with grimoires
            PlaceGrimoireChest(zone, factory, rng, openCells);

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
            PlaceNPC(zone, factory, rng, openCells, "Scribe", settlementId);

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

            Entity entity = factory.CreateEntity(blueprint);
            if (entity != null)
                zone.AddEntity(entity, x, y);

            openCells.RemoveAt(idx);
            return entity;
        }

        private void PlaceGrimoireChest(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells)
        {
            Entity chest = PlaceEntity(zone, factory, rng, openCells, "Chest");
            if (chest == null) return;

            var container = chest.GetPart<ContainerPart>();
            if (container == null) return;

            Entity grimoire = factory.CreateEntity("PurifyWaterGrimoire");
            if (grimoire != null)
                container.AddItem(grimoire);

            Entity mendingGrimoire = factory.CreateEntity("MendingRiteGrimoire");
            if (mendingGrimoire != null)
                container.AddItem(mendingGrimoire);

            Entity kindleGrimoire = factory.CreateEntity("KindleRiteGrimoire");
            if (kindleGrimoire != null)
                container.AddItem(kindleGrimoire);
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

            Entity entity = factory.CreateEntity("Well");
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
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = wellX + CardinalOffsets[i].dx;
                int my = wellY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = factory.CreateEntity("WellGroundMarker");
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
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = ovenX + CardinalOffsets[i].dx;
                int my = ovenY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = factory.CreateEntity("OvenGroundMarker");
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
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int mx = lanternX + CardinalOffsets[i].dx;
                int my = lanternY + CardinalOffsets[i].dy;
                if (!zone.InBounds(mx, my))
                    continue;

                Cell cell = zone.GetCell(mx, my);
                if (cell == null || !cell.IsPassable())
                    continue;

                Entity marker = factory.CreateEntity("LanternGroundMarker");
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

                Entity marker = factory.CreateEntity("CampfireGroundMarker");
                if (marker == null)
                    continue;

                zone.AddEntity(marker, mx, my);
            }
        }

        private static readonly string[] MerchantRepairStock =
        {
            "SilverSand",
            "FireClay",
            "WardOil"
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
                Entity item = factory.CreateEntity(MerchantRepairStock[i]);
                if (item != null)
                    inventory.AddObject(item);
            }
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
