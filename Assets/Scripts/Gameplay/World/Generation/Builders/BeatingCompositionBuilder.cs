using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Native arid-country realization, explicitly selected only by the
    /// ordinary wilderness arm. Never rebuilds existing or player-modified zones.</summary>
    public sealed class BeatingCompositionBuilder : IZoneBuilder
    {
        public string Name => "BeatingComposition";
        public int Priority => 2000;
        public Formation FormationOverride=Formation.None;
        public BeatingCompositionPlan Plan {get;private set;}
        private readonly int seed;
        public BeatingCompositionBuilder(int worldSeed) {seed=worldSeed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory==null||zone.EntityCount!=0)
                return Reject(zone==null?"missing-zone":factory==null?"missing-factory":"nonempty-zone");
            var plan=BeatingCompositionPlan.Create(zone.ZoneID,seed,FormationOverride);
            // Check all required identities before writing any cells. A missing
            // content pack must not leave a partly materialized desert behind.
            var required=new HashSet<string>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {required.Add(plan.GroundAt(x,y));if(plan.ObjectAt(x,y)!=null)required.Add(plan.ObjectAt(x,y));}
            foreach(string bp in required)
                if(factory.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return Reject("missing-blueprint:"+bp);
            int objects=0,water=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,plan.GroundAt(x,y),x,y)==null)return Reject("ground-placement");
                if(plan.IsWater(x,y)||plan.IsApproach(x,y))zone.GenReservedCells.Add((x,y));
                if(plan.IsWater(x,y))water++;
                string bp=plan.ObjectAt(x,y);if(bp==null)continue;
                var entity=BuilderSpawn.TryPlace(zone,factory,bp,x,y);
                if(entity==null)return Reject("object-placement:"+bp);
                entity.GetPart<TileStateSourcePart>()?.Seed(zone,x,y);objects++;
            }
            Plan=plan;
            Diag.Record("worldgen","BeatingCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                formation=plan.Formation.ToString(),condition=plan.Condition,objects,water});
            return true;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","BeatingCompositionRejected",payload:new{reason});return false;}
    }
}
