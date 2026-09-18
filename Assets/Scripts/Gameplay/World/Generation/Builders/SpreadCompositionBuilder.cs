using System;
using System.Collections.Generic;
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
            var ripe=SelectGleanings(Plan);
            if(ripe.Count>0&&!factory.Blueprints.ContainsKey("RipeCropRow"))return Reject("missing-ripe-row");
            int objects=0,water=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,Plan.GroundAt(x,y),x,y)==null)return Reject("missing-ground");
                if(Plan.IsWater(x,y)||Plan.IsApproach(x,y))zone.GenReservedCells.Add((x,y));
                if(Plan.IsWater(x,y)){zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);water++;}
                string bp=Plan.ObjectAt(x,y);
                if(bp==null)continue;
                if(ripe.Contains(y*Zone.Width+x))bp="RipeCropRow";
                if(BuilderSpawn.TryPlace(zone,factory,bp,x,y)==null)return Reject("missing-object");
                objects++;
            }
            Diag.Record("worldgen","SpreadCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                formation=Plan.Formation.ToString(),condition=Plan.Condition,objects,water});
            return true;
        }
        // Rank the existing planned rows, never alter their footprint or consume
        // caller RNG. The same seed retains its few gleanings after regeneration.
        private static HashSet<int> SelectGleanings(SpreadCompositionPlan plan)
        {
            var candidates=new List<int>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(plan.ObjectAt(x,y)=="CropRow")candidates.Add(y*Zone.Width+x);
            candidates.Sort((a,b)=>
            {
                int rank=plan.Roll(a%Zone.Width,a/Zone.Width,409).CompareTo(plan.Roll(b%Zone.Width,b/Zone.Width,409));
                return rank!=0?rank:a.CompareTo(b);
            });
            int budget=plan.Condition=="tended"?3:plan.Condition=="returning scrub"?2:1;
            var result=new HashSet<int>();
            for(int i=0;i<Math.Min(budget,candidates.Count);i++)result.Add(candidates[i]);
            return result;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","SpreadCompositionRejected",payload:new{reason});return false;}
    }
}
