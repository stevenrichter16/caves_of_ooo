using System;
using System.Collections.Generic;
using System.Globalization;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Bounded entry encounters, not offscreen caravan simulation.
    /// Rolls and identities live in existing player save properties; the actual
    /// trader/inventory stays in the normal cached zone graph.</summary>
    public static class WorldTravellers
    {
        public const int MaximumEncounters = 3;
        public const string OriginProperty = "TravellerOrigin";
        public const string DestinationProperty = "TravellerDestination";
        private const string ReceiptProperty = "TravellerReceipt";
        private const string RollPrefix = "TravellerRoll:";
        private const string CountPrefix = "TravellerCount:";
        private const string SpawnPrefix = "TravellerSpawn:";

        /// <summary>Call after successful player transfer and before registering
        /// the zone's actors/rendering/autosave. It never touches a global scheduler.
        /// A valid eligible entry consumes its roll even if placement is refused.</summary>
        public static bool OnZoneEntered(Entity player, Zone zone)
        {
            if (!WorldAmbience.IsPresentPlayer(player, zone)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.Factory == null || !ReferenceEquals(manager.ActiveZone, zone)
                || !manager.CachedZones.TryGetValue(zone.ZoneID, out var owned) || !ReferenceEquals(owned, zone)) return false;
            var (x, y, depth) = WorldMap.FromZoneID(zone.ZoneID);
            if (!WorldMapAuthoring.InBounds(x, y) || depth != 0 || WorldMapAuthoring.TierAt(x, y) > 3
                || manager.WorldMap.GetPOI(x, y) != null) return false;
            string seed = manager.WorldSeed.ToString(CultureInfo.InvariantCulture);
            string canonical = WorldMap.ToZoneID(x, y, 0);
            string countKey = CountPrefix + seed, rollKey = RollPrefix + seed + ":" + canonical;
            int count = player.GetIntProperty(countKey);
            if (count < 0 || count >= MaximumEncounters || player.IntProperties.ContainsKey(rollKey)) return false;
            player.IntProperties[rollKey] = 1;
            uint sample = WorldRemarks.Hash(seed + ":traveller:" + canonical);
            if (sample % 8 != 0) { Record(player, zone, "miss", null); return false; }
            if (!FindRoute(manager, x, y, out string origin, out string destination))
            { Record(player, zone, "missing-route", null); return false; }
            if (!FindStandingCell(player, zone, sample, out int sx, out int sy))
            { Record(player, zone, "no-standing-space", null); return false; }
            string id = "traveller:" + seed + ":" + canonical;
            foreach (var cached in manager.CachedZones.Values)
                foreach (var entity in cached.GetReadOnlyEntities())
                    if (entity.ID == id) { Record(player, zone, "identity-already-present", id); return false; }
            Entity actor;
            var oldLoadoutFactory = LoadoutPart.Factory; var oldTraderFactory = TraderPart.Factory;
            var oldLoadoutRng = LoadoutPart.Rng; var oldTraderRng = TraderPart.Rng;
            try
            {
                // Real factory stock and equipment, from a private authored
                // encounter seed. Ambient travel never advances combat/loot RNGs.
                LoadoutPart.Factory = TraderPart.Factory = manager.Factory;
                LoadoutPart.Rng = new Random(unchecked((int)sample));
                TraderPart.Rng = new Random(unchecked((int)(sample ^ 0x7135u)));
                actor = manager.Factory.CreateEntity("Merchant");
            }
            catch (Exception)
            { Record(player, zone, "factory-refused", null); return false; }
            finally
            {
                LoadoutPart.Factory = oldLoadoutFactory; TraderPart.Factory = oldTraderFactory;
                LoadoutPart.Rng = oldLoadoutRng; TraderPart.Rng = oldTraderRng;
            }
            if (actor?.GetPart<InventoryPart>() == null || actor.GetPart<InventoryPart>().Objects.Count == 0
                || FactionManager.IsHostile(actor, player) || FactionManager.IsHostile(player, actor))
            { Record(player, zone, "unavailable-trader", null); return false; }
            actor.ID = id;
            actor.Properties[OriginProperty] = origin; actor.Properties[DestinationProperty] = destination;
            actor.Properties[ReceiptProperty] = SpawnPrefix + seed + ":" + canonical;
            var render = actor.GetPart<RenderPart>(); if (render != null) render.DisplayName = "wayfaring merchant";
            // Keep normal timed restock, but entering does not create an immediate
            // second opening roll or purse top-up. This stamp is save-persistent.
            actor.IntProperties[TraderRestockSystem.LastRestockProp] = WorldClock.CurrentTick;
            if (!zone.AddEntity(actor, sx, sy)) { Record(player, zone, "placement-refused", null); return false; }
            var brain = actor.GetPart<BrainPart>(); if (brain != null) brain.CurrentZone = zone;
            player.IntProperties[countKey] = count + 1;
            player.Properties[SpawnPrefix + seed + ":" + canonical] = id;
            Record(player, zone, "placed", id);
            return true;
        }

        private static bool FindRoute(OverworldZoneManager manager, int x, int y, out string origin, out string destination)
        {
            origin = destination = null;
            int first = int.MaxValue, second = int.MaxValue;
            for (int px = 0; px < WorldMap.Width; px++) for (int py = 0; py < WorldMap.Height; py++)
            {
                var poi = manager.WorldMap.GetPOI(px, py);
                if (poi?.Type != POIType.Village || string.IsNullOrWhiteSpace(poi.Name)) continue;
                int distance = Math.Abs(px - x) + Math.Abs(py - y);
                if (distance < first)
                { second = first; destination = origin; first = distance; origin = WorldMap.ToZoneID(px, py, 0); }
                else if (distance < second)
                { second = distance; destination = WorldMap.ToZoneID(px, py, 0); }
            }
            return origin != null && destination != null && origin != destination;
        }

        private static bool FindStandingCell(Entity player, Zone zone, uint sample, out int x, out int y)
        {
            var origin = zone.GetEntityCell(player); x = y = -1;
            // Four small rings: at most176 candidates. Reverse traversal by hash
            // for variety without an unbounded search or another RNG stream.
            int sign = (sample & 8) == 0 ? 1 : -1;
            for (int radius = 4; radius <= 7; radius++)
                for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                    int tx = origin.X + dx * sign, ty = origin.Y + dy * sign;
                    if (!StandingCell(zone, tx, ty) || !AIHelpers.HasLineOfSight(zone, origin.X, origin.Y, tx, ty)) continue;
                    int exits = 0;
                    foreach (var step in Cardinal)
                        if (StandingCell(zone, tx + step.x, ty + step.y)) exits++;
                    if (exits < 3) continue;
                    x = tx; y = ty; return true;
                }
            return false;
        }
        private static readonly (int x, int y)[] Cardinal = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        private static bool StandingCell(Zone zone, int x, int y)
        {
            if (x < 2 || y < 2 || x >= Zone.Width - 2 || y >= Zone.Height - 2 || zone.GenReservedCells.Contains((x, y))) return false;
            var cell = zone.GetCell(x, y);
            if (cell.IsInterior || cell.BlocksMovement() || zone.TileState.Has(x, y)) return false;
            foreach (var item in cell.Occupants)
                if (!item.HasTag("Terrain") || item.HasPart<CampfirePart>() || item.HasPart<WellSitePart>()
                    || item.HasPart<StairsDownPart>() || item.HasPart<StairsUpPart>() || item.HasPart<LiquidPoolPart>()) return false;
            return true;
        }

        /// <summary>Read-only route lead for this live merchant's ordinary dialogue.
        /// Uses current actual POIs and returns the authored fallback if ownership,
        /// living participants or either route endpoint no longer validate.</summary>
        public static string DescribeConversation(Entity speaker, Entity listener, string conversation, string node, string fallback)
        {
            if (speaker == null || speaker.BlueprintName != "Merchant" || conversation != "Merchant_1"
                || (node != "Start" && node != "Sources")) return fallback;
            var zone = speaker.SpatialZone;
            if (!WorldAmbience.IsPresentPlayer(listener, zone) || speaker.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(speaker) || zone.GetEntityCell(speaker)?.Objects.Contains(speaker) != true
                || FactionManager.IsHostile(speaker, listener) || FactionManager.IsHostile(listener, speaker)) return fallback;
            var manager = WorldLocationContext.For(zone);
            if (manager == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var owned) || !ReferenceEquals(owned, zone)) return fallback;
            string receipt = speaker.GetProperty(ReceiptProperty);
            string prefix = SpawnPrefix + manager.WorldSeed.ToString(CultureInfo.InvariantCulture) + ":";
            if (receipt == null || !receipt.StartsWith(prefix, StringComparison.Ordinal)
                || listener.GetProperty(receipt) != speaker.ID) return fallback;
            string a = VillageName(manager, speaker.GetProperty(OriginProperty));
            string b = VillageName(manager, speaker.GetProperty(DestinationProperty));
            if (a == null || b == null || speaker.GetProperty(OriginProperty) == speaker.GetProperty(DestinationProperty)) return fallback;
            return node == "Start" ? "I'm on the road between " + a + " and " + b + ". I can trade while I rest."
                : "I travel between " + a + " and " + b + ". What I have for sale is in my pack.";
        }
        private static string VillageName(OverworldZoneManager manager, string id)
        {
            var (x, y, z) = WorldMap.FromZoneID(id);
            if (!WorldMapAuthoring.InBounds(x, y) || z != 0 || id != WorldMap.ToZoneID(x, y, 0)) return null;
            var poi = manager.WorldMap.GetPOI(x, y);
            return poi?.Type == POIType.Village && !string.IsNullOrWhiteSpace(poi.Name) ? poi.Name : null;
        }
        private static void Record(Entity player, Zone zone, string reason, string id)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "TravellerEntryRoll", actor: player,
                    payload: new { zone = zone.ZoneID, reason, entityId = id });
        }
    }
}
