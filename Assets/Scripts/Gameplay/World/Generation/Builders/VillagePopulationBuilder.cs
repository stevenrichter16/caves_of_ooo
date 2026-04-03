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

            // Deterministic NPC roles
            Entity elder = PlaceNPC(zone, factory, rng, openCells, "Elder", settlementId);
            if (mainWell != null)
                SetConversation(elder, "Elder_Well_1");

            // 1 Merchant (always)
            PlaceNPC(zone, factory, rng, openCells, "Merchant", settlementId);

            // 0-1 Tinker (70% chance)
            if (rng.Next(100) < 70)
                PlaceNPC(zone, factory, rng, openCells, "Tinker", settlementId);

            // 0-1 Warden (60% chance)
            if (rng.Next(100) < 60)
                PlaceNPC(zone, factory, rng, openCells, "Warden", settlementId);

            if (mainWell != null)
            {
                PlaceNPC(zone, factory, rng, openCells, "WellKeeper", settlementId);
                PlaceNPC(zone, factory, rng, openCells, "Farmer", settlementId);
            }

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
