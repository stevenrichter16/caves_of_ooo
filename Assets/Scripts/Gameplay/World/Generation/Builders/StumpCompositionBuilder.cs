using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Realizes a fresh native Stump once. Reserves landforms, arrivals
    /// and habitat before hazards/stamps, without granting new water mechanics.</summary>
    public sealed class StumpCompositionBuilder : IZoneBuilder
    {
        public string Name => "StumpComposition";
        public int Priority => 2000;
        public Formation FormationOverride = Formation.None;
        public StumpCompositionPlan Plan { get; private set; }
        internal Zone SourceZone { get; private set; }
        private readonly int seed;
        public StumpCompositionBuilder(int worldSeed) { seed=worldSeed; }

        /// <summary>Clone the native ambient stamps with one open, reserved cell
        /// around their footprints. LandmarkBuilder checks this apron before
        /// placing and reserves it afterward, keeping later stamps and props
        /// away from entrances. The shared catalog remains untouched.</summary>
        public static IReadOnlyList<StructureStamp> CreateLandmarkCatalog()
        {
            var result=new List<StructureStamp>();
            foreach(var source in StampCatalog.For(BiomeType.Stump))
            {
                var rows=new string[source.Height+2];
                rows[0]=rows[rows.Length-1]=new string('.',source.Width+2);
                for(int y=0;y<source.Height;y++)
                    rows[y+1]="."+source.Rows[y].PadRight(source.Width,'.')+".";
                result.Add(new StructureStamp {
                    Name=source.Name, Rows=rows,
                    Legend=new Dictionary<char,string>(source.Legend),
                    Chance=source.Chance, MinTier=source.MinTier,
                    ClearsVegetation=source.ClearsVegetation
                });
            }
            return result;
        }

        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;SourceZone=null;
            if(zone==null||factory==null||zone.EntityCount!=0)
                return Reject(zone==null?"missing-zone":factory==null?"missing-factory":"nonempty-zone");
            if(!StumpCompositionPlan.IsWildernessZone(zone.ZoneID))return Reject("ineligible-zone");
            var plan=StumpCompositionPlan.Create(zone.ZoneID,seed,FormationOverride);
            var required=new HashSet<string>{"TepuiStone"};
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(plan.ObjectAt(x,y)!=null)required.Add(plan.ObjectAt(x,y));
            foreach(string bp in required)
                if(factory.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return Reject("missing-blueprint:"+bp);
            int objects=0,habitats=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,"TepuiStone",x,y)==null)return Reject("ground-placement");
                string bp=plan.ObjectAt(x,y);
                if(plan.IsApproach(x,y)||plan.IsHabitat(x,y)||bp!=null)zone.GenReservedCells.Add((x,y));
                if(plan.IsHabitat(x,y))habitats++;
                if(bp==null)continue;
                if(BuilderSpawn.TryPlace(zone,factory,bp,x,y)==null)return Reject("object-placement:"+bp);
                objects++;
                if(bp=="TepuiboneVein")
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)zone.GenReservedCells.Add((x+dx,y+dy));
            }
            Plan=plan;SourceZone=zone;
            Diag.Record("worldgen","StumpCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                band=plan.Band.ToString(),formation=plan.Formation.ToString(),objects,habitats});
            return true;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","StumpCompositionRejected",payload:new{reason});return false;}
    }

    /// <summary>The existing band population, with a narrow reservation window.
    /// Only this terrain's still-valid habitat cells are exposed; authored stamp
    /// cells and approach routes stay excluded. Reservations restore on failure.</summary>
    public sealed class StumpHabitatPopulationBuilder : IZoneBuilder
    {
        public string Name=>"StumpHabitatPopulation";
        public int Priority=>4000;
        private readonly StumpCompositionBuilder terrain;
        /// <summary>Normal native band population configuration. Habitat filtering
        /// remains its responsibility; the wrapper only scopes reservations.</summary>
        public readonly PopulationBuilder Population;
        public StumpHabitatPopulationBuilder(StumpCompositionBuilder terrain,PopulationTable table)
        {
            this.terrain=terrain??throw new ArgumentNullException(nameof(terrain));
            Population=new PopulationBuilder(table){HabitatFilter=StumpFaunaHabitat.Allows};
        }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory==null||rng==null||terrain.Plan==null||!ReferenceEquals(terrain.SourceZone,zone))
            {Diag.Record("worldgen","StumpHabitatPopulationRejected",payload:new{reason="missing-native-plan"});return false;}
            var opened=new List<(int x,int y)>();
            try
            {
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(!terrain.Plan.IsHabitat(x,y))continue;
                    var cell=zone.GetCell(x,y);
                    bool stairs=false;
                    foreach(var entity in cell.Objects)
                        if(entity.HasPart<StairsDownPart>()||entity.HasPart<StairsUpPart>()) {stairs=true;break;}
                    if(stairs)continue;
                    bool valid=StumpFaunaHabitat.Allows(terrain.Plan.Band==StumpBand.Foothills?"CascadeFather":"SummitSinger",cell)
                        || terrain.Plan.Band==StumpBand.Summit&&StumpFaunaHabitat.Allows("BrocchiniaSentinel",cell);
                    if(valid&&!cell.BlocksMovement()&&zone.GenReservedCells.Remove((x,y)))opened.Add((x,y));
                }
                bool result=Population.BuildZone(zone,factory,rng);
                Diag.Record("worldgen","StumpHabitatPopulationApplied",payload:new{zoneId=zone.ZoneID,opened=opened.Count});
                return result;
            }
            finally {foreach(var c in opened)zone.GenReservedCells.Add(c);}
        }
    }
}
