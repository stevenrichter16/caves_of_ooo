using System;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Three bounded, cold-only expedition sites. Retention preserves
    /// literal graphs even when every original owner has been removed.</summary>
    public static class SoddenDistrict
    {
        public const int FirstVersion=14;
        public static bool Retain(string id)=>SoddenDistrictPlan.IsSupportedZone(id);

        // Visited sites keep their original saved notices. Correct only their stock route directions at read time.
        internal static string DescribeCutbankRoutes(Entity owner, string text)
        {
            Zone zone = owner.SpatialZone;
            if (text == null || zone?.ZoneID != SoddenDistrictPlan.CrossingZoneID)
                return text;

            var cell = zone.GetEntityPosition(owner);
            if (owner.BlueprintName == "SoddenRouteNotice" && cell == (18, 11)
                && owner.GetProperty(SoddenDistrictBuilder.RoleKey) == "notice")
            {
                return text.Replace("Raised boards turn north around the cut; the southern bank also stays dry. Both detours are longer.",
                    "The north boards cross watched ground when the passage post is staffed. Ask the watcher before entering the marked stretch. The south bank is dry and free. Both detours are longer than the wet crossing.");
            }

            if (owner.BlueprintName == "SoddenPassagePost" && cell == (36, 2)
                && owner.GetProperty(SecondExplorationSites.RoleKey) == "cutbank-post")
            {
                return text.Replace("A short dry crossing is watched from this post. Four drams buy one local passage. The longer dry boardwalks and the wet crossing remain open.",
                    "When the post is staffed, four drams buy one passage over the watched north boards. The south bank is a free dry detour. The direct mire crossing stays wet and open.");
            }

            return text;
        }

        /// <summary>Only the current generated crossing's real western notice
        /// selects its first map arrival. Remembered returns take precedence in
        /// WorldMapTraversal; this never edits or rebuilds a destination graph.</summary>
        public static bool TryFirstArrival(Zone zone,ZoneManager owner,out int x,out int y)
        {
            x=20;y=12;
            if(owner is not OverworldZoneManager manager||zone?.ZoneID!=SoddenDistrictPlan.CrossingZoneID
                ||!Eligible(manager,zone.ZoneID)||!manager.CachedZones.TryGetValue(zone.ZoneID,out var current)||current!=zone)return false;
            return zone.GetCell(18,11).Objects.Any(e=>e.BlueprintName=="SoddenRouteNotice"
                &&e.GetProperty(SoddenDistrictBuilder.RoleKey)=="notice"&&e.SpatialZone==zone
                &&e.GetPart<PhysicsPart>()?.ParentEntity==e&&!e.GetPart<PhysicsPart>().Takeable
                &&e.GetPart<PhysicsPart>().InInventory==null&&e.GetPart<ExaminablePart>()?.ParentEntity==e);
        }
        public static bool Eligible(OverworldZoneManager manager,string id)
        {
            if(manager?.Exploration?.Enabled!=true||manager.Exploration.Version<FirstVersion
                ||manager.WorldMap==null||!SoddenDistrictPlan.IsSupportedZone(id))return false;
            var p=WorldMap.FromZoneID(id);
            return manager.WorldMap.GetBiome(p.x,p.y)==BiomeType.Sodden&&manager.WorldMap.GetPOI(p.x,p.y)==null
                &&SoddenDistrictBuilder.SupportsContent(manager.Factory);
        }

        internal sealed class Attempt
        {
            internal readonly SoddenDistrictBuilder Builder;
            readonly OverworldZoneManager manager;readonly SpreadExplorationPlan plan;readonly WorldMap map;readonly string id;
            internal Attempt(OverworldZoneManager owner,string zoneID)
            {
                manager=owner;plan=owner.Exploration;map=owner.WorldMap;id=zoneID;
                Builder=new SoddenDistrictBuilder(owner.WorldSeed){ConfigureStop=Configure};
            }
            void Configure(Zone zone,Entity worker)
            {
                if(!Current())throw new InvalidOperationException("Sodden admission changed during generation.");
                var bench=zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenDressingBench");
                if(bench.GetPart<SoddenPreparationPart>()?.Configure(zone,worker)!=true)
                    throw new InvalidOperationException("Sodden keeper could not bind the dressing bench.");
            }
            bool Current()=>ReferenceEquals(plan,manager.Exploration)&&ReferenceEquals(map,manager.WorldMap)&&Eligible(manager,id);
            internal bool Commit(Zone zone)=>Current()&&zone?.ZoneID==id&&Builder.SourceZone==zone&&Builder.ValidateFinal(zone);
        }

        /// <summary>A cold new-world lead on the existing town record owner;
        /// it never creates or retrofits a saved town or a missing district.</summary>
        internal static void DescribeTownRoute(Zone zone,OverworldZoneManager manager)
        {
            if(zone?.ZoneID!=SumpholdCompositionPlan.ZoneID||!Eligible(manager,SoddenDistrictPlan.StopZoneID))return;
            foreach(var rolls in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="TollRolls"))
            {
                var examine=rolls.GetPart<ExaminablePart>();if(examine==null)continue;
                examine.Text+="\nA fresh work slip reads: SELLA'S DRESSING SHELTER — one stretch south of Sumphold. Bench brace broken: two sound timber lengths needed. Old cutting works: two stretches east of the shelter, beyond the cutbank crossing. Keep to the dry bow if you can spare the walk. Bring sumpsieve pads and knotflax cord for wound dressings.";
            }
        }
    }
}
