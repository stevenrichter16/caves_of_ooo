using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// C4: applies <see cref="PlaceConditionCatalog"/> while a condition holds in its place.
    /// <list type="bullet">
    /// <item>Prices: <see cref="PriceModifier"/> is multiplied into trade prices for traders
    /// standing in the place (TradeSystem).</item>
    /// <item>Stock: on arrival, each listed resident is topped up to its count, at most once
    /// per <see cref="TraderRestockSystem.RestockIntervalTurns"/> (int-property stamp).</item>
    /// <item>Arrivals: placed once on a real arrival, with stable ids.</item>
    /// <item>Lines: residents say <see cref="LineFor"/> as an ambient remark; neighbouring
    /// places listed in HeardIn say the news.</item>
    /// </list>
    /// Diag: concern/PlaceStocked, concern/PlaceArrival, event/PlaceRemark.
    /// </summary>
    public static class PlaceConsequences
    {
        public const int ChancePercent = 4;
        public const int Radius = 6;
        const string StockStamp = "PlaceStock:";

        static bool Holds(string zoneId, string condition)
            => SettlementManager.Current != null && SettlementManager.Current.HasCondition(zoneId, condition);

        static string ZoneOf(string place)
        {
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == place) return WorldMap.ToZoneID(p.X, p.Y, 0);
            return null;
        }

        /// <summary>The product of the price factors of every condition holding where the
        /// trader stands; 1 elsewhere.</summary>
        public static double PriceModifier(Entity trader)
        {
            string zoneId = trader?.SpatialZone?.ZoneID;
            string place = PlaceSiteCatalog.PlaceAt(zoneId);
            if (place == null) return 1.0;
            double m = 1.0;
            foreach (var c in PlaceConditionCatalog.All)
                if (c.Place == place && c.Price > 0 && c.Price != 1.0 && Holds(zoneId, c.Condition)) m *= c.Price;
            return m;
        }

        /// <summary>Stock top-ups and, on a real arrival, new residents. Returns how many
        /// things were added.</summary>
        public static int OnZoneEntered(Entity player, Zone zone, bool arriving)
        {
            string place = PlaceSiteCatalog.PlaceAt(zone?.ZoneID);
            if (player == null || place == null || SettlementManager.Current == null) return 0;
            int added = 0, turn = SettlementManager.Current.GetCurrentTurn();
            var factory = ConversationActions.Factory;
            foreach (var c in PlaceConditionCatalog.All)
            {
                if (c.Place != place || !Holds(zone.ZoneID, c.Condition)) continue;
                if (c.Stock != null && factory != null)
                    foreach (var s in c.Stock)
                        foreach (var owner in zone.GetAllEntities())
                        {
                            if (owner.BlueprintName != s.Owner || owner.GetPart<InventoryPart>() == null) continue;
                            string stamp = StockStamp + c.Condition + ":" + s.Blueprint;
                            int last = owner.GetIntProperty(stamp, int.MinValue);
                            if (last != int.MinValue && (long)turn - last <= TraderRestockSystem.RestockIntervalTurns) continue;
                            int have = CavesOfOoo.Concerns.ConcernItems.CountCarried(owner, s.Blueprint);
                            for (int i = have; i < s.Count; i++)
                            {
                                var item = factory.CreateEntity(s.Blueprint);
                                if (item != null && owner.GetPart<InventoryPart>().AddObject(item)) added++;
                            }
                            owner.SetIntProperty(stamp, turn);
                            Record("PlaceStocked", c, owner.BlueprintName + ":" + s.Blueprint);
                        }
                if (arriving && c.Arrivals != null && factory != null)
                    for (int a = 0; a < c.Arrivals.Count; a++)
                        for (int i = 0; i < c.Arrivals[a].Count; i++)
                        {
                            string id = "place:" + c.Condition + ":" + a + ":" + i;
                            if (Contains(zone, id)) continue;
                            var who = factory.CreateEntity(c.Arrivals[a].Blueprint);
                            if (who == null || !FindCell(zone, out int x, out int y)) continue;
                            who.ID = id;
                            if (zone.AddEntity(who, x, y)) { added++; Record("PlaceArrival", c, who.BlueprintName); }
                        }
            }
            return added;
        }

        /// <summary>What a resident of this zone's place would say now: the first holding
        /// condition's own remark, else news heard from another place; null if nothing.</summary>
        public static string LineFor(string zoneId, int pick = 0)
        {
            string place = PlaceSiteCatalog.PlaceAt(zoneId);
            if (place == null) return null;
            foreach (var c in PlaceConditionCatalog.All)
                if (c.Place == place && c.Remarks != null && c.Remarks.Count > 0 && Holds(zoneId, c.Condition))
                    return c.Remarks[Math.Abs(pick) % c.Remarks.Count];
            foreach (var c in PlaceConditionCatalog.All)
            {
                if (c.HeardIn == null || !c.HeardIn.Contains(place) || string.IsNullOrEmpty(c.Heard)) continue;
                string there = ZoneOf(c.Place);
                if (there != null && Holds(there, c.Condition)) return c.Heard;
            }
            return null;
        }

        /// <summary>An ambient remark by a resident near the player (called after
        /// WorldRemarks declines the turn's budget). <paramref name="force"/> skips the chance.</summary>
        public static bool TryRemark(Entity player, Zone zone, bool force = false)
        {
            if (player == null || zone == null) return false;
            int turn = player.GetIntProperty(WorldAmbience.TurnProperty);
            if (!force && (!WorldAmbience.IsPresentPlayer(player, zone) || !WorldAmbience.MayEmit(player)
                || turn < 0 || Hash(zone.ZoneID + ":place:" + turn) % 100 >= ChancePercent)) return false;
            string line = LineFor(zone.ZoneID, (int)(Hash(zone.ZoneID + ":line:" + turn) & 0x7fff));
            if (line == null) return false;
            Entity speaker = null; int best = int.MaxValue;
            foreach (var e in zone.GetAllEntities())
            {
                if (ReferenceEquals(e, player) || e.GetPart<ConversationPart>() == null || !e.HasTag("Creature")
                    || e.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(e)
                    || FactionManager.IsHostile(e, player) || string.IsNullOrEmpty(e.GetPart<RenderPart>()?.DisplayName)) continue;
                if (!force && zone.GetEntityCell(e)?.IsVisible != true) continue; // seen, not through walls
                int d = SpatialQuery.Distance(zone, player, e);
                if (d <= Radius && d < best) { best = d; speaker = e; }
            }
            if (speaker == null) return false;
            if (!force) WorldAmbience.MarkEmission(player);
            Diag.Record("event", "PlaceRemark", actor: speaker, target: player, payload: new { zone = zone.ZoneID, line });
            MessageLog.Add(speaker.GetDisplayName() + " says, \"" + line + "\"");
            return true;
        }

        static bool Contains(Zone zone, string id)
        {
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == id) return true;
            return false;
        }

        static bool FindCell(Zone zone, out int x, out int y)
        {
            int cx = Zone.Width / 2, cy = Zone.Height / 2;
            for (int r = 1; r <= Zone.Width / 2; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    x = cx + dx; y = cy + dy;
                    if (x < 2 || y < 2 || x >= Zone.Width - 2 || y >= Zone.Height - 2) continue;
                    var cell = zone.GetCell(x, y);
                    if (cell == null || cell.BlocksMovement() || cell.IsInterior || zone.TileState.Has(x, y)) continue;
                    bool clear = true;
                    foreach (var o in cell.Occupants) if (!o.HasTag("Terrain")) { clear = false; break; }
                    if (clear) return true;
                }
            x = y = -1; return false;
        }

        static uint Hash(string s)
        {
            unchecked { uint h = 2166136261; foreach (char ch in s) { h ^= ch; h *= 16777619; } return h; }
        }

        static void Record(string kind, PlaceConditionData c, string detail)
        {
            if (!Diag.IsChannelEnabled("concern")) return;
            Diag.Record(category: "concern", kind: kind, payload: new { place = c.Place, condition = c.Condition, detail });
        }
    }
}
