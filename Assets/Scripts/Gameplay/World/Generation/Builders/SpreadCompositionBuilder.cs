using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Realizes recovered-country land use once, using native entities.
    /// Explicitly selected only for ordinary Spread surface generation.</summary>
    public sealed class SpreadCompositionBuilder : IZoneBuilder
    {
        public string Name=>"SpreadComposition";
        public int Priority=>2000;
        public Formation FormationOverride=Formation.None;
        public SpreadCompositionPlan Plan {get;private set;}
        private readonly int seed;
        public SpreadCompositionBuilder(int worldSeed){seed=worldSeed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory==null||zone.EntityCount!=0)
            {
                Diag.Record("worldgen","SpreadCompositionRejected",payload:new{reason=zone==null?"missing-zone":factory==null?"missing-factory":"nonempty-zone"});
                return false;
            }
            Plan=SpreadCompositionPlan.Create(zone.ZoneID,seed,FormationOverride);
            int objects=0,water=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,Plan.GroundAt(x,y),x,y)==null)return Reject("missing-ground");
                if(Plan.IsWater(x,y)||Plan.IsApproach(x,y))zone.GenReservedCells.Add((x,y));
                if(Plan.IsWater(x,y)){zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);water++;}
                string bp=Plan.ObjectAt(x,y);
                if(bp==null)continue;
                if(BuilderSpawn.TryPlace(zone,factory,bp,x,y)==null)return Reject("missing-object");
                objects++;
            }
            Diag.Record("worldgen","SpreadCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                formation=Plan.Formation.ToString(),condition=Plan.Condition,objects,water});
            return true;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","SpreadCompositionRejected",payload:new{reason});return false;}
    }
}
