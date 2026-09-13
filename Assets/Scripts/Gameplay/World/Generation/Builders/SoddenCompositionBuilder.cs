using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Native floodland realization, explicitly selected only by the
    /// ordinary wilderness arm. Never rebuilds existing or player-modified zones.</summary>
    public sealed class SoddenCompositionBuilder : IZoneBuilder
    {
        public string Name => "SoddenComposition";
        public int Priority => 2000;
        public Formation FormationOverride=Formation.None;
        public SoddenCompositionPlan Plan {get;private set;}
        private readonly int seed;
        public SoddenCompositionBuilder(int worldSeed) {seed=worldSeed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory==null||zone.EntityCount!=0)
                return Reject(zone==null?"missing-zone":factory==null?"missing-factory":"nonempty-zone");
            var plan=SoddenCompositionPlan.Create(zone.ZoneID,seed,FormationOverride);
            // Check all required identities before writing any cells. A missing
            // content pack must not leave a partly materialized swamp behind.
            var required=new HashSet<string>{"Grass"};
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(plan.ObjectAt(x,y)!=null)required.Add(plan.ObjectAt(x,y));
            foreach(string bp in required)
                if(factory.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return Reject("missing-blueprint:"+bp);
            int objects=0,water=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,plan.GroundAt(x,y),x,y)==null)return Reject("ground-placement");
                if(plan.IsWet(x,y)||plan.IsApproach(x,y))zone.GenReservedCells.Add((x,y));
                if(plan.IsWet(x,y))water++;
                if(plan.IsShallowWater(x,y))zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);
                string bp=plan.ObjectAt(x,y);if(bp==null)continue;
                var entity=BuilderSpawn.TryPlace(zone,factory,bp,x,y);
                if(entity==null)return Reject("object-placement:"+bp);
                entity.GetPart<TileStateSourcePart>()?.Seed(zone,x,y);objects++;
            }
            Plan=plan;
            Diag.Record("worldgen","SoddenCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                formation=plan.Formation.ToString(),condition=plan.Condition,objects,water});
            return true;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","SoddenCompositionRejected",payload:new{reason});return false;}
    }
}
