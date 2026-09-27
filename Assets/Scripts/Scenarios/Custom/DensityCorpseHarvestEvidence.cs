using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Read-only witnesses for the bounded native SunStriker audit.
    /// Counts alone do not establish source identity or a committed native action.</summary>
    public static class DensityCorpseHarvestEvidence
    {
        public const string MarkerKind = "DensityCorpseHarvestKeyStart";

        public static bool IsExactCorpse(Entity corpse, Zone zone, string player,
            string source, int x, int y)
        {
            if (corpse == null || zone == null || string.IsNullOrEmpty(corpse.ID)
                || string.IsNullOrEmpty(player) || string.IsNullOrEmpty(source)
                || corpse.ID == source || corpse.ID == player || source == player
                || corpse.BlueprintName != "CreatureCorpse") return false;
            var cell = zone.GetEntityCell(corpse);
            var physics = corpse.GetPart<PhysicsPart>();
            var harvest = corpse.GetPart<HarvestablePart>();
            return cell != null && cell.X == x && cell.Y == y && cell.Objects.Contains(corpse)
                && physics != null && ReferenceEquals(physics.ParentEntity, corpse) && physics.Takeable
                && physics.InInventory == null && physics.Equipped == null
                && harvest != null && ReferenceEquals(harvest.ParentEntity, corpse) && !harvest.Harvested
                && harvest.YieldBlueprint == "RawMeat" && harvest.YieldMin == 1
                && harvest.YieldMax == 2 && harvest.YieldChance == 100
                && corpse.GetProperty("SourceID") == source && corpse.GetProperty("SourceBlueprint") == "SunStriker"
                && corpse.GetProperty("KillerID") == player && corpse.GetProperty("KillerBlueprint") == "Player";
        }

        public static bool RestoredYieldGraph(Entity previousPlayer, Zone previousZone, Entity player, Zone zone,
            IReadOnlyList<Zone> cached, string sourceId, string corpseId, string earnedId, int units)
        {
            if (previousPlayer == null || previousZone == null || player == null || zone == null
                || ReferenceEquals(player, previousPlayer) || ReferenceEquals(zone, previousZone)
                || player.ID != previousPlayer.ID || zone.ZoneID != previousZone.ZoneID
                || cached == null || !cached.Contains(zone) || cached.Any(z => z == null)
                || string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(corpseId)
                || string.IsNullOrEmpty(earnedId) || units < 1 || units > 2) return false;
            var at = zone.GetEntityCell(player);
            var inventory = player.GetPart<InventoryPart>();
            if (at == null || !at.Objects.Contains(player) || inventory == null
                || !ReferenceEquals(inventory.ParentEntity, player)) return false;
            var products = inventory.Objects.Where(e => e.BlueprintName == "RawMeat").ToArray();
            if (products.Length != 1 || products[0].ID != earnedId
                || (products[0].GetPart<StackerPart>()?.StackCount ?? 1) != units) return false;
            var physics = products[0].GetPart<PhysicsPart>();
            if (physics == null || !ReferenceEquals(physics.ParentEntity, products[0])
                || !ReferenceEquals(physics.InInventory, player) || physics.Equipped != null
                || inventory.EquippedItems.Values.Contains(products[0])) return false;
            bool RemovedSource(Entity e) => e.ID == sourceId || e.ID == corpseId || e.GetProperty("SourceID") == sourceId;
            if (inventory.Objects.Any(RemovedSource) || inventory.EquippedItems.Values.Any(RemovedSource)) return false;
            foreach (var current in cached)
                if (current.GetReadOnlyEntities().Any(e => RemovedSource(e) || e.ID == earnedId)) return false;
            return true;
        }

        public static bool CommittedHarvest(IReadOnlyList<Diag.Entry> rows, string marker,
            string player, string corpse, int packedBefore, int packedAfter,
            int floorBefore, int floorAfter, bool spent, bool absent)
        {
            if (rows == null || !spent || !absent || string.IsNullOrEmpty(marker)
                || string.IsNullOrEmpty(player) || string.IsNullOrEmpty(corpse) || player == corpse
                || packedBefore < 0 || packedAfter < packedBefore || floorBefore < 0 || floorAfter < floorBefore)
                return false;
            int start = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].TraceId != marker) continue;
                if (start >= 0 || rows[i].Category != "scenario" || rows[i].Kind != MarkerKind
                    || rows[i].ActorId != player || rows[i].TargetId != corpse) return false;
                start = i;
            }
            if (start < 0) return false;
            Diag.Entry? commit = null;
            for (int i = start + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Category != "loot" || row.Kind != "Harvested"
                    || row.ActorId != player || row.TargetId != corpse) continue;
                if (commit.HasValue) return false;
                commit = row;
            }
            if (!commit.HasValue) return false;
            try
            {
                var data = JObject.Parse(commit.Value.PayloadJson);
                if (data["source"]?.Type != JTokenType.String || (string)data["source"] != "CreatureCorpse"
                    || data["yield"]?.Type != JTokenType.String || (string)data["yield"] != "RawMeat"
                    || data["rollPassed"]?.Type != JTokenType.Boolean || !(bool)data["rollPassed"]
                    || data["count"]?.Type != JTokenType.Integer || data["dropped"]?.Type != JTokenType.Integer)
                    return false;
                long packed = (long)data["count"], floor = (long)data["dropped"];
                return packed >= 0 && packed <= 2 && floor >= 0 && floor <= 2
                    && packed + floor >= 1 && packed + floor <= 2
                    && (long)packedAfter - packedBefore == packed && (long)floorAfter - floorBefore == floor;
            }
            catch (Exception error) when (error is Newtonsoft.Json.JsonException
                || error is InvalidCastException || error is OverflowException || error is ArgumentException)
            { return false; }
        }
    }
}
