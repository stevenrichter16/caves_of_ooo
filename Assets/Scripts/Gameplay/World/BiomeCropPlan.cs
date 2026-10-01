using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>One immutable, seed-stable small crop patch. SpeciesIndex refers
    /// to the catalogue's five species for this ecology, never access order.</summary>
    public sealed class BiomeCropSite
    {
        public string ZoneID { get; }
        public BiomeType Biome { get; }
        public int SpeciesIndex { get; }
        public int WorldSeed { get; }
        public string Key => "biome-crops:v1:" + WorldSeed + ":" + ZoneID + ":" + SpeciesIndex;
        internal BiomeCropSite(string id, BiomeType biome, int index, int seed)
        { ZoneID=id; Biome=biome; SpeciesIndex=index; WorldSeed=seed; }
    }

    /// <summary>Finite cold-generation allocation. Reads no cached zone contents,
    /// creates no zones and consumes no gameplay RNG. Old saved graphs stay literal.</summary>
    public static class BiomeCropPlan
    {
        static readonly BiomeType[] SurfaceBiomes = { BiomeType.Spread, BiomeType.Sodden,
            BiomeType.Beating, BiomeType.Grovelands, BiomeType.Overwrit, BiomeType.Stump };

        public static IReadOnlyList<BiomeCropSite> SurfaceSites(OverworldZoneManager manager)
        {
            var result=new List<BiomeCropSite>();
            if(manager?.WorldMap?.Tiles==null || manager.WorldMap.POIs==null)return result.AsReadOnly();
            foreach(var biome in SurfaceBiomes)
            {
                var candidates=new List<string>();
                for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
                {
                    string id=WorldMap.ToZoneID(x,y,0);
                    if(manager.WorldMap.GetBiome(x,y)==biome && EligibleSurface(manager,id,biome))candidates.Add(id);
                }
                candidates.Sort((a,b)=>Compare(manager.WorldSeed,a,b,"surface"));
                int count=Math.Min(candidates.Count,biome==BiomeType.Overwrit||biome==BiomeType.Stump?5:10);
                int rotate=(int)(Rank(manager.WorldSeed,biome.ToString(),"species")%5);
                for(int i=0;i<count;i++)result.Add(new BiomeCropSite(candidates[i],biome,(i+rotate)%5,manager.WorldSeed));
            }
            return result.AsReadOnly();
        }

        /// <summary>A finite set of ordinary columns. A cave site additionally
        /// requires a surviving physical up stair and incoming route at placement.</summary>
        public static IReadOnlyList<string> CaveColumns(OverworldZoneManager manager)
        {
            var result=new List<string>();
            if(manager?.WorldMap==null)return result.AsReadOnly();
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                string surface=WorldMap.ToZoneID(x,y,0);
                if(IsOrdinaryCave(manager,WorldMap.ToZoneID(x,y,1)))result.Add(surface);
            }
            result.Sort((a,b)=>Compare(manager.WorldSeed,a,b,"cave-column"));
            return result.Take(16).ToList().AsReadOnly();
        }

        public static BiomeCropSite ForZone(OverworldZoneManager manager,string zoneID)
        {
            if(!Canonical(zoneID,out int x,out int y,out int depth)||manager?.WorldMap==null)return null;
            if(depth==0)return SurfaceSites(manager).FirstOrDefault(s=>s.ZoneID==zoneID);
            if(depth>5 || !IsOrdinaryCave(manager,zoneID) || !CaveColumns(manager).Contains(WorldMap.ToZoneID(x,y,0)))return null;
            int rotate=(int)(Rank(manager.WorldSeed,WorldMap.ToZoneID(x,y,0),"cave-species")%5);
            return new BiomeCropSite(zoneID,BiomeType.Cave,(depth-1+rotate)%5,manager.WorldSeed);
        }

        /// <summary>Receiving-manager authority for ordinary underground ecology.
        /// This read-only predicate does not grant placement in authored columns.</summary>
        public static bool IsOrdinaryCave(OverworldZoneManager manager,string zoneID)
        {
            if(!Canonical(zoneID,out int x,out int y,out int depth)||depth<=0
                ||manager?.WorldMap?.Tiles==null||manager.WorldMap.POIs==null)return false;
            string surface=WorldMap.ToZoneID(x,y,0);
            return manager.WorldMap.GetPOI(x,y)==null && !LairStacks.HasSavedColumn(manager,surface) && !Protected(surface)
                && !WorldMapAuthoring.PlaceAt(x,y).HasValue && !SinkholeSites.IsMouth(x,y);
        }

        static bool EligibleSurface(OverworldZoneManager manager,string id,BiomeType biome)
        {
            var p=WorldMap.FromZoneID(id);
            if(Protected(id)||LairStacks.HasSavedColumn(manager,id)||manager.WorldMap.GetPOI(p.x,p.y)!=null||WorldMapAuthoring.PlaceAt(p.x,p.y).HasValue
                ||SinkholeSites.IsMouth(p.x,p.y)||WorldMapAuthoring.BiomeAt(p.x,p.y)!=biome)return false;
            if(id==manager.RareEncounters?.PairZoneID||id==manager.RareEncounters?.ViperZoneID||id==manager.Wayhouse?.ZoneID)return false;
            switch(biome)
            {
                case BiomeType.Spread:
                    if(!SpreadCompositionPlan.IsWildernessZone(id))return false;
                    if(manager.Exploration?.Enabled==true)
                    {var e=manager.Exploration.Entries.FirstOrDefault(row=>row.ZoneID==id);return e!=null&&e.PlacementEligible&&e.Family==SpreadExplorationFamily.None;}
                    return string.IsNullOrEmpty(SpreadWildernessSituationPlan.Select(manager,id,manager.Wayhouse?.ZoneID));
                case BiomeType.Sodden:return SoddenCompositionPlan.IsWildernessZone(id);
                case BiomeType.Beating:return BeatingCompositionPlan.IsWildernessZone(id);
                case BiomeType.Grovelands:return GrovelandsCompositionPlan.IsWildernessZone(id);
                case BiomeType.Overwrit:return OverwritCompositionPlan.IsRimZone(id);
                case BiomeType.Stump:return StumpCompositionPlan.IsWildernessZone(id);
                default:return false;
            }
        }
        static bool Protected(string surface)
        {
            if(surface==MultiCellPilotRuntime.ZoneID||surface==RepairCultivationSite.ZoneID||surface==FellingSiteBuilder.ZoneID
                ||surface=="Overworld.3.3.0"||surface==OverworldZoneManager.AbandonedCounterZoneA||surface==OverworldZoneManager.AbandonedCounterZoneB
                ||Array.IndexOf(OverworldZoneManager.AuthoredWildernessZoneIDs,surface)>=0)return true;
            foreach(var d in RegionalSituations.Definitions)if(surface==d.SourceZoneId||surface==d.RecipientZoneId)return true;
            return false;
        }
        static bool Canonical(string id,out int x,out int y,out int depth)
        {
            x=y=depth=-1;if(string.IsNullOrEmpty(id)||!WorldMap.IsOverworldZoneID(id))return false;
            var p=WorldMap.FromZoneID(id);x=p.x;y=p.y;depth=p.z;
            return WorldMapAuthoring.InBounds(x,y)&&depth>=0&&id==WorldMap.ToZoneID(x,y,depth);
        }
        static int Compare(int seed,string a,string b,string salt)
        {int c=Rank(seed,a,salt).CompareTo(Rank(seed,b,salt));return c!=0?c:string.CompareOrdinal(a,b);}
        internal static uint Rank(int seed,string id,string salt)
        {unchecked{uint h=2166136261u^(uint)seed;foreach(char c in "BiomeCrops.v1|"+salt+"|"+id)h=(h^c)*16777619u;h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16);}}
    }
}
