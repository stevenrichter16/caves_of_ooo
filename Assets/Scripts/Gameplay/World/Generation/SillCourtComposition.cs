using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Fresh Sill only: a facing pair of existing shops and an open
    /// approach to its existing well. No save retrofit or added service.</summary>
    internal sealed class SillCourtComposition
    {
        const string ZoneID = "Overworld.10.10.0";
        readonly OverworldZoneManager manager;
        readonly StructureStamp smithy, larder;
        readonly List<(int x, int y)> cells = new List<(int x, int y)>();
        static readonly (int x, int y)[] Aprons = {
            (37,6),(38,6),(41,6),(42,6),(37,7),(38,7),(39,7),(40,7),(41,7),(42,7) };
        Zone attemptZone;

        SillCourtComposition(OverworldZoneManager manager, StructureStamp smithy, StructureStamp larder)
        {
            this.manager = manager;
            this.smithy = smithy;
            // Never mutate the shared town stamp. Its door now faces the smithy.
            this.larder = new StructureStamp {
                Name = larder.Name, Chance = larder.Chance, MinTier = larder.MinTier,
                Rows = new string[larder.Height], Legend = new Dictionary<char,string>(larder.Legend) };
            for (int i = 0; i < larder.Height; i++)
            { var row = larder.Rows[i].ToCharArray(); Array.Reverse(row); this.larder.Rows[i] = new string(row); }
            AddRectangle(31,4,smithy.Width,smithy.Height);
            AddRectangle(43,4,larder.Width,larder.Height);
            AddRectangle(37,6,6,2);
            AddRectangle(39,8,2,4);
        }

        internal static SillCourtComposition Create(OverworldZoneManager manager, string zoneID)
        {
            if (!MatchesSite(manager, zoneID)) return null;
            var catalog = StampCatalog.Town();
            StructureStamp smithy = null, larder = null;
            foreach (var stamp in catalog)
            { if (stamp.Name == "TheSmithy") smithy = stamp; if (stamp.Name == "TheLarder") larder = stamp; }
            if (smithy == null || larder == null || smithy.Width != 6 || smithy.Height != 4
                || larder.Width != 5 || larder.Height != 4 || !KnownContent(manager.Factory, smithy, larder)) return null;
            return new SillCourtComposition(manager, smithy, larder);
        }

        static bool MatchesSite(OverworldZoneManager manager, string zoneID)
        {
            if (zoneID != ZoneID) return false;
            var map = manager?.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height
                || map.GetBiome(10,10) != BiomeType.Spread) return false;
            var poi = map.GetPOI(10,10);
            return poi != null && poi.Type == POIType.Village && poi.Name == "Sill"
                && poi.Faction == "Villagers" && string.IsNullOrEmpty(poi.Profile);
        }

        static bool KnownContent(EntityFactory factory, params StructureStamp[] stamps)
        {
            if (factory == null || !factory.Blueprints.ContainsKey("Grass")
                || !factory.Blueprints.ContainsKey("StoneFloor")) return false;
            foreach (var stamp in stamps)
            foreach (var marker in stamp.Legend.Values)
            {
                if (string.IsNullOrEmpty(marker)) continue;
                string blueprint = marker;
                if (marker.StartsWith("shop:"))
                { var fields = marker.Split(':'); if (fields.Length != 3) return false; blueprint = fields[1]; }
                else if (marker.Contains(":") || marker == "interior") return false;
                if (!factory.Blueprints.ContainsKey(blueprint)) return false;
            }
            return true;
        }

        void AddRectangle(int x, int y, int width, int height)
        { for (int dy = 0; dy < height; dy++) for (int dx = 0; dx < width; dx++) cells.Add((x+dx,y+dy)); }

        internal void BeginAttempt(Zone zone) { attemptZone = zone; }
        internal bool ExcludesHouse(int x, int y, int width, int height)
            => x < 49 && x + width > 30 && y < 11 && y + height > 3;
        internal bool Protects(Zone zone, int x, int y)
            => ReferenceEquals(zone, attemptZone) && cells.Contains((x,y));

        // The new preferred sites are stricter than generic stamps: even a
        // passable loose object or a nonblocking coating must be left alone.
        bool CaptureClearGround(Zone zone, out Entity[] grass)
        {
            grass = new Entity[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                var p = cells[i]; var cell = zone.GetCell(p.x,p.y);
                if (cell == null || cell.IsInterior || zone.GenReservedCells.Contains(p)
                    || zone.TileState.Has(p.x,p.y) || cell.Occupants.Count != 1) return false;
                var owner = cell.Occupants[0];
                if (owner.BlueprintName != "Grass" || owner.HasPart<SpatialFootprintPart>()
                    || zone.GetEntityCell(owner) != cell || cell.BlocksMovement()) return false;
                grass[i] = owner;
            }
            return true;
        }

        internal bool TryPlace(Zone zone, EntityFactory factory, Random rng)
        {
            if (!ReferenceEquals(zone, attemptZone) || !MatchesSite(manager, zone.ZoneID)
                || !ReferenceEquals(factory, manager.Factory) || !KnownContent(factory, smithy, larder)
                || !CaptureClearGround(zone, out var before)) return false;
            var staged = new List<(Entity owner, int x, int y)>();
            if (!Stage(staged, factory, rng, smithy,31,4) || !Stage(staged, factory,rng,larder,43,4)) return false;
            foreach (var p in Aprons)
            { var floor = factory.CreateEntity("StoneFloor"); if (!SafeNewOwner(floor)) return false; staged.Add((floor,p.x,p.y)); }
            // ObjectCreated/stock hooks run while staging. Revalidate both
            // entire footprints before committing either, including identity.
            if (!MatchesSite(manager, zone.ZoneID) || !CaptureClearGround(zone, out var after)) return false;
            for (int i = 0; i < before.Length; i++) if (!ReferenceEquals(before[i],after[i])) return false;
            int committed = 0;
            foreach (var entry in staged)
            {
                if (!zone.AddEntity(entry.owner,entry.x,entry.y))
                {
                    for (int i = committed-1; i >= 0; i--) zone.RemoveEntity(staged[i].owner);
                    return false;
                }
                committed++;
            }
            // Reserve only the completed pair and its real open approaches;
            // claiming them earlier would veto the pair itself.
            foreach (var p in cells) zone.GenReservedCells.Add(p);
            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("worldgen"))
            {
                CavesOfOoo.Diagnostics.Diag.Record("worldgen","StructurePlaced", payload: new { stamp=smithy.Name,zone=zone.ZoneID,ax=31,ay=4 });
                CavesOfOoo.Diagnostics.Diag.Record("worldgen","StructurePlaced", payload: new { stamp=larder.Name,zone=zone.ZoneID,ax=43,ay=4 });
            }
            return true;
        }

        static bool SafeNewOwner(Entity owner)
            => owner != null && owner.SpatialZone == null && !owner.HasTag("Barren")
                && !owner.HasPart<SpatialFootprintPart>() && !owner.HasPart<LiquidPoolPart>();

        static bool Stage(List<(Entity owner,int x,int y)> staged, EntityFactory factory,
            Random rng, StructureStamp stamp, int ax, int ay)
        {
            for (int y = 0; y < stamp.Height; y++)
            for (int x = 0; x < stamp.Rows[y].Length; x++)
            {
                char ch = stamp.Rows[y][x];
                if (ch == '.' || !stamp.Legend.TryGetValue(ch,out string marker) || string.IsNullOrEmpty(marker)) continue;
                string[] fields = marker.StartsWith("shop:") ? marker.Split(':') : null;
                var owner = factory.CreateEntity(fields == null ? marker : fields[1]);
                if (!SafeNewOwner(owner)) return false;
                if (fields != null) LandmarkBuilder.StockShopkeeper(owner,fields[2],factory,rng);
                staged.Add((owner,ax+x,ay+y));
            }
            return true;
        }
    }
}
