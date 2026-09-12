using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Realizes spatial intent with destructible native vegetation.
    /// Used only by the wilderness Grovelands pipeline, before its formations.</summary>
    public sealed class GrovelandsCompositionBuilder : IZoneBuilder
    {
        public string Name => "GrovelandsComposition";
        public int Priority => 2000;
        public Formation FormationOverride=Formation.None;
        public GrovelandsCompositionPlan Plan { get; private set; }
        private readonly int seed;
        public GrovelandsCompositionBuilder(int worldSeed) { seed=worldSeed; }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory==null) return false;
            // Reject non-empty input rather than removing somebody else's objects.
            if(zone.EntityCount!=0) return false;
            Plan=GrovelandsCompositionPlan.Create(zone.ZoneID,seed,FormationOverride);
            int trees=0,bushes=0,walls=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,"Grass",x,y)==null)return false;
                if(Plan.IsReserved(x,y))continue;
                double growth=Plan.Canopy(x,y) + (Plan.Character=="young fringe" ? -.04 : Plan.Character=="reclaiming thicket" ? .04 : 0);
                // Discrete masses with quiet margins; no uniformly sprinkled trees.
                // Leave boundary rows open so generated neighbors can always meet.
                if(x<2||y<2||x>=Zone.Width-2||y>=Zone.Height-2)continue;
                string bp=null;
                if(growth>.77 && Plan.Roll(x,y,31)<60) { bp="VineWall";walls++; }
                else if(growth>.60 && Plan.Roll(x,y,37)<18) { bp="Tree";trees++; }
                else if(growth>.49 && Plan.Roll(x,y,41)<10+Plan.Moisture(x,y)*10) { bp="Bush";bushes++; }
                if(bp!=null && BuilderSpawn.TryPlace(zone,factory,bp,x,y)==null)return false;
            }
            Diag.Record("worldgen","GrovelandsCompositionPlanned",payload:new {
                zoneId=zone.ZoneID,seed,formation=Plan.Formation.ToString(),character=Plan.Character,
                focalX=Plan.FocalX,focalY=Plan.FocalY,trees,bushes,walls });
            return true;
        }
    }
}
