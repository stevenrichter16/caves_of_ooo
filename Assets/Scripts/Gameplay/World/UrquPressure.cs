using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B2 (Docs/UPPER-TIER-BESTIARY.md §3.1) — where Urqu is active. Canon
    /// (Lore/History/05_Spirits.md): Urqu manifests "as consequence, not trigger … at
    /// catastrophic conditions — a faction's collapse, a city falling, a mass-death event";
    /// the Felling-Site is the highest bleed point, "the sari gradient peaking at the
    /// Site" (FELLING-WORLD-DESIGN §3.6).
    ///
    /// <para>A zone is active when any of these holds:</para>
    /// <list type="bullet">
    /// <item>the story's global <see cref="StoryFact"/> is set;</item>
    /// <item>it is a Stump cell within <see cref="GradientRadius"/> of the Felling-Site
    /// (the gradient, at any depth);</item>
    /// <item>a local manifestation covers it: <see cref="SettlementDeaths"/> persons dead
    /// in one settlement zone, or <see cref="AnyZoneDeaths"/> anywhere, within one world
    /// day manifests that zone and its eight neighbours for <see cref="ManifestDays"/> days.</item>
    /// </list>
    ///
    /// <para>All state is narrative facts, so it saves with the world. Population entries
    /// gated on <c>UrquActive</c> consult <see cref="IsActiveAt"/> for the zone being
    /// generated; zones already generated receive their Urqu fauna on a real arrival
    /// (<see cref="OnZoneEntered"/>), once per manifestation.</para>
    ///
    /// <para>Diag: urqu/Manifested, urqu/ArrivalFauna.</para>
    /// </summary>
    public static class UrquPressure
    {
        public const string StoryFact = "UrquActive";
        public const int GradientRadius = 2;
        public const int SettlementDeaths = 4;
        public const int AnyZoneDeaths = 10;
        public const int ManifestDays = 3;
        const string ManifestPrefix = "urqu.manifest.";
        const string DeathPrefix = "urqu.deaths.";
        const string FaunaPrefix = "urqu.fauna.";
        public const string ArrivalLine = "The air here says sari, sari, and does not finish the word.";

        static NarrativeStatePart State => NarrativeStatePart.Current;

        /// <summary>The canonical surface ID for any depth of a world cell, or null.</summary>
        static string Surface(string zoneId, out int x, out int y)
        {
            x = y = -1;
            if (string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return null;
            var (zx, zy, _) = WorldMap.FromZoneID(zoneId);
            if (!WorldMapAuthoring.InBounds(zx, zy)) return null;
            x = zx; y = zy;
            return WorldMap.ToZoneID(zx, zy, 0);
        }

        /// <summary>Is the cell within the Felling-Site's gradient? Open Stump ground only:
        /// authored sites (the Felling-Site, the Root, places and sinkholes such as Stillleaf)
        /// keep their own composed population and flags.</summary>
        public static bool InGradient(int x, int y)
            => WorldMapAuthoring.InBounds(x, y) && WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump
               && Math.Max(Math.Abs(x - FellingSiteBuilder.WorldX), Math.Abs(y - FellingSiteBuilder.WorldY)) <= GradientRadius
               && !AuthoredSite(x, y);

        static bool AuthoredSite(int x, int y)
        {
            if (x == FellingSiteBuilder.WorldX && y == FellingSiteBuilder.WorldY) return true;
            if (x == RootSiteBuilder.WorldX && y == RootSiteBuilder.WorldY) return true;
            foreach (var p in WorldMapAuthoring.Places) if (p.X == x && p.Y == y) return true;
            foreach (var site in SinkholeSites.All) if (site.X == x && site.Y == y) return true;
            return false;
        }

        public static bool IsActiveAt(string zoneId)
        {
            var state = State;
            if (state != null && state.GetFact(StoryFact) > 0) return true;
            string surface = Surface(zoneId, out int x, out int y);
            if (surface == null) return false;
            if (InGradient(x, y)) return true;
            return state != null && state.GetFact(ManifestPrefix + surface) > WorldClock.CurrentTick;
        }

        /// <summary>The key of the manifestation now covering this zone (0 if none, -1
        /// for the gradient or the story flag), so arrival fauna is placed once each.</summary>
        static int ManifestationKey(string zoneId)
        {
            string surface = Surface(zoneId, out int x, out int y);
            if (surface != null)
            {
                int expiry = State?.GetFact(ManifestPrefix + surface) ?? 0;
                if (expiry > WorldClock.CurrentTick) return expiry;
            }
            return IsActiveAt(zoneId) ? -1 : 0;
        }

        /// <summary>Called by <see cref="CombatSystem.HandleDeath"/> after every death.
        /// Counts persons only (factioned, not Beasts: <see cref="UnderTheClothEffect.IsPerson"/>).</summary>
        public static void OnDeath(Entity victim, Zone zone)
        {
            var state = State;
            if (state == null || victim == null || zone == null || !victim.HasTag("Creature") || victim.HasTag("Player")) return;
            if (!UnderTheClothEffect.IsPerson(victim)) return;
            string surface = Surface(zone.ZoneID, out int x, out int y);
            if (surface == null) return;
            int day = WorldClock.CurrentTick / WorldClock.DayLengthTicks;
            string key = DeathPrefix + zone.ZoneID + "." + day;
            state.AddFact(key, 1);
            int deaths = state.GetFact(key);
            bool settlement = PlaceSiteCatalog.PlaceAt(surface) != null;
            if (deaths == (settlement ? SettlementDeaths : AnyZoneDeaths)) Manifest(x, y, settlement ? "settlement_deaths" : "mass_death");
        }

        /// <summary>Manifest the cell and its eight neighbours for <see cref="ManifestDays"/>.
        /// A later manifestation extends, never shortens, an earlier one.</summary>
        public static void Manifest(int x, int y, string cause)
        {
            var state = State;
            if (state == null || !WorldMapAuthoring.InBounds(x, y)) return;
            int until = WorldClock.CurrentTick + ManifestDays * WorldClock.DayLengthTicks;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!WorldMapAuthoring.InBounds(x + dx, y + dy)) continue;
                    string fact = ManifestPrefix + WorldMap.ToZoneID(x + dx, y + dy, 0);
                    if (state.GetFact(fact) < until) state.SetFact(fact, until);
                }
            if (Diag.IsChannelEnabled("urqu"))
                Diag.Record("urqu", "Manifested", payload: new { x, y, cause, until });
        }

        /// <summary>On a real arrival in an active zone, place its Urqu fauna once per
        /// manifestation: a Sari-Snake clutch, and on the Stump or in the Beating a
        /// Sky-Sari overhead. Returns how many were placed.</summary>
        public static int OnZoneEntered(Entity player, Zone zone)
        {
            var state = State;
            if (state == null || player == null || zone == null || !player.HasTag("Player")) return 0;
            int key = ManifestationKey(zone.ZoneID);
            if (key == 0) return 0;
            string seen = FaunaPrefix + zone.ZoneID;
            if (state.GetFact(seen) == key) return 0;
            state.SetFact(seen, key);
            var factory = WorldLocationContext.For(zone)?.Factory ?? ConversationActions.Factory;
            if (factory == null) return 0;
            Surface(zone.ZoneID, out int x, out int y);
            var at = zone.GetEntityCell(player);
            uint h = WorldRemarks.Hash(zone.ZoneID + ":urqu:" + key);
            int placed = 0;
            int snakes = 2 + (int)(h % 2);
            for (int i = 0; i < snakes; i++) placed += Place(zone, factory, "SariSnake", at, 4 + i, "urqu:" + key + ":s" + i);
            var biome = WorldMapAuthoring.InBounds(x, y) ? WorldMapAuthoring.BiomeAt(x, y) : BiomeType.Cave;
            if (biome == BiomeType.Stump || biome == BiomeType.Beating)
                placed += Place(zone, factory, "SkySari", at, 7, "urqu:" + key + ":sky");
            if (placed > 0) MessageLog.Add(ArrivalLine);
            if (Diag.IsChannelEnabled("urqu"))
                Diag.Record("urqu", "ArrivalFauna", actor: player, payload: new { zone = zone.ZoneID, key, placed });
            return placed;
        }

        static int Place(Zone zone, Data.EntityFactory factory, string blueprint, Cell near, int ring, string id)
        {
            if (near == null) return 0;
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == id) return 0;
            for (int r = ring; r <= ring + 4; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    if (!WorldTravellers.StandingCell(zone, near.X + dx, near.Y + dy)) continue;
                    var e = factory.CreateEntity(blueprint);
                    if (e == null) return 0;
                    e.ID = id;
                    if (!zone.AddEntity(e, near.X + dx, near.Y + dy)) return 0;
                    var brain = e.GetPart<BrainPart>(); if (brain != null) brain.CurrentZone = zone;
                    return 1;
                }
            return 0;
        }
    }
}
