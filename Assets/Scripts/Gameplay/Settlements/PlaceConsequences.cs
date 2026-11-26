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

        static bool StockAvailable(Entity player, Zone zone, PlaceStockData stock, bool conditionHolds)
        {
            if (stock == null) return false;
            if (string.IsNullOrEmpty(stock.StableSite)) return conditionHolds;
            var world = WorldLocationContext.For(zone);
            return world != null && world.ActiveZone == zone && world.SettlementManager == SettlementManager.Current
                && WorldAmbience.IsPresentPlayer(player, zone)
                && world.SettlementManager.TryGetExistingSite(zone.ZoneID, stock.StableSite, out var site)
                && (site.Stage == RepairStage.StableRepair || site.Stage == RepairStage.ImprovedWithCaretaker);
        }

        static string ZoneOf(string place)
        {
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == place) return WorldMap.ToZoneID(p.X, p.Y, 0);
            return null;
        }

        /// <summary>Trade rates from saved local conditions and explicitly linked working sites.</summary>
        public static double PriceModifier(Entity trader) => PriceModifier(trader, out _);

        internal static double PriceModifier(Entity trader, out bool hasWorkingPriceSite)
        {
            hasWorkingPriceSite = false;
            var zone = trader?.SpatialZone;
            string place = PlaceSiteCatalog.PlaceAt(zone?.ZoneID);
            var settlements = SettlementManager.Current;
            if (place == null || settlements == null) return 1.0;
            double m = 1.0;
            foreach (var c in PlaceConditionCatalog.All)
            {
                if (c.Place != place) continue;
                hasWorkingPriceSite |= c.HasWorkingPrice;
                if (c.Price > 0 && c.Price != 1.0
                    && (settlements.HasExistingCondition(zone.ZoneID, c.Condition) || WorkingPriceSite(zone, c) != null))
                    m *= c.Price;
            }
            return m;
        }

        static RepairableSiteState WorkingPriceSite(Zone zone, PlaceConditionData row)
        {
            if (!row.HasWorkingPrice) return null;
            var world = WorldLocationContext.For(zone);
            if (world == null || world.ActiveZone != zone || world.SettlementManager != SettlementManager.Current
                || !world.CachedZones.TryGetValue(zone.ZoneID, out var active) || active != zone
                || !world.SettlementManager.TryGetExistingSite(zone.ZoneID, row.WorkingPriceSite, out var site))
                return null;
            int now = world.SettlementManager.GetCurrentTurn();
            bool patched = site.Stage == RepairStage.TemporarilyPurified && site.OutcomeTier == RepairOutcomeTier.Temporary
                && site.ResolvedByMethod == RepairMethodId.PatchedRepair && site.RelapseAtTurn is int expires && now < expires;
            bool mended = site.Stage == RepairStage.StableRepair && site.OutcomeTier == RepairOutcomeTier.Stable
                && site.ResolvedByMethod == RepairMethodId.ManualRepair;
            if ((!patched && !mended) || site.ResolvedAtTurn < 0 || now < site.ResolvedAtTurn) return null;

            var installation = InstallationRegistry.SiteBody(row.Place, row.WorkingPriceSite);
            if (installation == null) return null;
            bool found = false;
            foreach (var owner in zone.GetReadOnlyEntitiesWithPart<PlaceSiteObjectPart>())
            {
                var marker = owner.GetPart<InstallationMarkerPart>();
                if (marker?.InstallationId != installation.Id) continue;
                var linked = owner.GetPart<PlaceSiteObjectPart>();
                if (found || marker.ParentEntity != owner || marker.Origin != "Generated"
                    || linked?.ParentEntity != owner || linked.Place != row.Place || linked.SiteId != row.WorkingPriceSite
                    || owner.BlueprintName != installation.Blueprint || !WorldResourceActions.Ground(owner, zone)
                    || owner.GetPart<DestructiblePart>()?.Gone == true) return null;
                found = true;
            }
            return found ? site : null;
        }

        internal static string WorkingPriceNote(Entity trader)
        {
            var zone = trader?.SpatialZone;
            string place = PlaceSiteCatalog.PlaceAt(zone?.ZoneID);
            if (place == null) return "";
            foreach (var row in PlaceConditionCatalog.All)
                if (row.Place == place && WorkingPriceSite(zone, row) != null)
                    return "Local rates use the working " + (PlaceSiteCatalog.Get(place, row.WorkingPriceSite)?.Name ?? row.WorkingPriceSite) + ".";
            return "";
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
                if (c.Place != place) continue;
                bool conditionHolds = Holds(zone.ZoneID, c.Condition);
                if (c.Stock != null && factory != null)
                    foreach (var s in c.Stock)
                    {
                        if (!StockAvailable(player, zone, s, conditionHolds)) continue;
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
                    }
                if (conditionHolds && arriving && c.Arrivals != null && factory != null)
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
            // News heard from elsewhere: every holding piece takes its turn (two passes, no allocation).
            int heard = 0;
            foreach (var c in PlaceConditionCatalog.All) if (IsHeardHere(c, place)) heard++;
            if (heard == 0) return null;
            int want = Math.Abs(pick) % heard;
            foreach (var c in PlaceConditionCatalog.All)
                if (IsHeardHere(c, place) && want-- == 0) return c.Heard;
            return null;
        }

        static bool IsHeardHere(PlaceConditionData c, string place)
        {
            if (c.HeardIn == null || !c.HeardIn.Contains(place) || string.IsNullOrEmpty(c.Heard)) return false;
            string there = ZoneOf(c.Place);
            return there != null && Holds(there, c.Condition);
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
