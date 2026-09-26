using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Stages the complete native glade before publishing it to an empty
    /// authorized zone. No renderer, scene object or gameplay RNG owns this layout.</summary>
    public sealed class ReferenceGladeBuilder:IZoneBuilder
    {
        public string Name=>"ReferenceGlade";
        public int Priority=>2000;
        private readonly int seed;
        private static readonly string[] supplies={"HealingTonic","Torch","DriedMeat"};
        private static readonly string[] required =
        {
            "Grass", "Wall", "GlowQuartzVein", "Chest", "WoodenBarrel", "Torch",
            "Signpost", "MushroomRing", "MarlbackScrabbler", "MarlbackGleaner",
            "Warden", "Villager", "PetDog", "BrokenColumn", "Reeds", "Rubble",
            "Bush", "HealingTonic", "DriedMeat"
        };
        public ReferenceGladeBuilder(int seed){this.seed=seed;}
        /// <summary>Read-only content-pack capability check. A minimal/modded
        /// pack can keep its normal Spread pipeline when this optional scene's
        /// native owners are unavailable. No entity creation or RNG consumption.</summary>
        public static bool SupportsContent(EntityFactory factory)
        {
            if (factory == null) return false;
            foreach (string name in required)
                if (!factory.Blueprints.TryGetValue(name, out var blueprint)
                    || blueprint == null || !blueprint.Parts.ContainsKey("Render")) return false;
            return factory.Blueprints["Chest"].Parts.ContainsKey("Container")
                && factory.Blueprints["Wall"].Parts.ContainsKey("Destructible")
                && factory.Blueprints["GlowQuartzVein"].Parts.ContainsKey("Harvestable");
        }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory==null||!ReferenceGladePlan.IsActive(zone)||zone.EntityCount!=0)
                return Reject(zone,"invalid-or-occupied-zone");
            if (!SupportsContent(factory)) return Reject(zone,"unsupported-content");
            var plan=ReferenceGladePlan.Create(seed);
            foreach(var p in plan.Placements)if(!factory.Blueprints.ContainsKey(p.Blueprint))return Reject(zone,"missing-"+p.Blueprint);
            foreach(string bp in supplies)if(!factory.Blueprints.ContainsKey(bp))return Reject(zone,"missing-"+bp);
            var staged=new List<Entity>(plan.Placements.Count);
            try
            {
                foreach(var p in plan.Placements)
                {
                    var e=factory.CreateEntity(p.Blueprint);
                    if(e==null)return Reject(zone,"factory-refused");
                    var render=e.GetPart<RenderPart>();
                    if(render==null)return Reject(zone,"missing-native-render");
                    if(p.VisualID.Length>0)render.VisualID=p.VisualID;
                    if(p.Blueprint=="Wall"||p.Blueprint=="GlowQuartzVein")
                        e.SetIntProperty("ReferenceGladeQuarterTurns",p.VisualID=="reference-glade-lit-wall"||p.X==53||p.X==47||p.X==23&&p.Y>11&&p.Y<22?1:0);
                    if(p.Blueprint=="Wall"&&!e.HasPart<DestructiblePart>())return Reject(zone,"missing-native-destruction");
                    if(p.Blueprint=="GlowQuartzVein"&&!e.HasPart<HarvestablePart>())return Reject(zone,"missing-native-harvest");
                    if(p.VisualID=="reference-glade-lit-wall")
                        e.AddPart(new LightSourcePart{LightColor="&G",Radius=2,Intensity=.35f});
                    if(p.Blueprint=="Chest")
                    {
                        var container=e.GetPart<ContainerPart>();
                        if(container==null)return Reject(zone,"missing-native-container");
                        foreach(string bp in supplies)
                        {var item=factory.CreateEntity(bp);if(item==null||!container.AddItem(item))return Reject(zone,"container-refused");}
                    }
                    staged.Add(e);
                }
            }
            catch(Exception error)
            {
                // Nothing has been published yet. Initialization callbacks are
                // content, and their refusal must not escape the builder contract.
                return Reject(zone,"staging-exception-"+error.GetType().Name);
            }
            for(int i=0;i<staged.Count;i++)
            {
                var p=plan.Placements[i];
                if(!zone.AddEntity(staged[i],p.X,p.Y))
                {for(int j=0;j<i;j++)zone.RemoveEntity(staged[j]);return Reject(zone,"placement-refused");}
            }
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(ReferenceGladePlan.IsApproach(x,y)||Math.Max(Math.Abs(x-40),Math.Abs(y-12))<=2)
                    zone.GenReservedCells.Add((x,y));
            Diag.Record("worldgen","ReferenceGladeBuilt",payload:new{zoneId=zone.ZoneID,seed,owners=staged.Count});
            return true;
        }
        private static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","ReferenceGladeRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
