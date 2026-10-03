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
        private readonly bool fieldwork;
        public const string FieldworkRoleKey="GleanersFieldwork.Role";
        private static readonly string[] supplies={"HealingTonic","Torch","DriedMeat"};
        private static readonly string[] required =
        {
            "Grass", "Wall", "GlowQuartzVein", "Chest", "WoodenBarrel", "Torch",
            "Signpost", "MushroomRing", "MarlbackScrabbler", "MarlbackGleaner",
            "Warden", "Villager", "PetDog", "BrokenColumn", "Reeds", "Rubble",
            "Bush", "HealingTonic", "DriedMeat", "RoadStone", "CropRow",
            "RipeCropRow", "SpreadDrawPoint", "Campfire", "FallenBeam", "Emberwheat", "Waterskin"
        };
        public ReferenceGladeBuilder(int seed):this(seed,false){}
        public ReferenceGladeBuilder(int seed,bool fieldwork){this.seed=seed;this.fieldwork=fieldwork;}
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
                && factory.Blueprints["GlowQuartzVein"].Parts.ContainsKey("Harvestable")
                && factory.Blueprints["RipeCropRow"].Parts.ContainsKey("FieldHarvest")
                && factory.Blueprints["SpreadDrawPoint"].Parts.ContainsKey("LiquidPool")
                && factory.Blueprints["Campfire"].Parts.ContainsKey("Campfire")
                && factory.Blueprints["Campfire"].Parts.ContainsKey("Thermal")
                && factory.Blueprints["Campfire"].Parts.ContainsKey("Fuel")
                && factory.Blueprints["FallenBeam"].Parts.ContainsKey("Handling")
                && factory.Blueprints["Waterskin"].Parts.ContainsKey("Waterskin");
        }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory==null||!ReferenceGladePlan.IsActive(zone)||zone.EntityCount!=0)
                return Reject(zone,"invalid-or-occupied-zone");
            if (!SupportsContent(factory)) return Reject(zone,"unsupported-content");
            if(fieldwork&&(!factory.Blueprints.ContainsKey("DrawgourdSeed")||!factory.Blueprints.ContainsKey("DrawgourdShell")
                ||!factory.Blueprints.ContainsKey("SalvagedTimber")))return Reject(zone,"missing-fieldwork-supply");
            var plan=ReferenceGladePlan.Create(seed,fieldwork);
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
                        e.SetIntProperty("ReferenceGladeQuarterTurns",p.VisualID=="reference-glade-lit-wall"||p.X==53||p.X==47
                            ||p.X==23&&p.Y>11&&p.Y<22||p.X==26&&p.Y>=14&&p.Y<=21||p.X==50&&p.Y==7?1:0);
                    if(p.Blueprint=="Wall"&&!e.HasPart<DestructiblePart>())return Reject(zone,"missing-native-destruction");
                    if(p.Blueprint=="GlowQuartzVein"&&!e.HasPart<HarvestablePart>())return Reject(zone,"missing-native-harvest");
                    if(p.Blueprint=="RipeCropRow"&&!e.HasPart<FieldHarvestPart>())return Reject(zone,"missing-native-grain");
                    if(p.Blueprint=="SpreadDrawPoint"&&!e.HasPart<LiquidPoolPart>())return Reject(zone,"missing-native-water");
                    if(p.Blueprint=="FallenBeam"&&!e.HasPart<HandlingPart>())return Reject(zone,"missing-native-hauling");
                    if(p.Blueprint=="Waterskin"&&!e.HasPart<WaterskinPart>())return Reject(zone,"missing-native-vessel");
                    if(p.VisualID=="reference-glade-lit-wall")
                        e.AddPart(new LightSourcePart{LightColor="&G",Radius=2,Intensity=.35f});
                    if(p.Blueprint=="Chest")
                    {
                        var container=e.GetPart<ContainerPart>();
                        if(container==null)return Reject(zone,"missing-native-container");
                        foreach(string bp in supplies)
                        {var item=factory.CreateEntity(bp);if(item==null||!container.AddItem(item))return Reject(zone,"container-refused");}
                    }
                    DescribeDiscovery(e,p);
                    if(fieldwork)
                    {
                        if(p.Blueprint=="GleanersBuckledWicket")
                        {
                            if(!SpreadExplorationPassage.ValidGate(e,false,true))return Reject(zone,"invalid-fieldwork-wicket");
                            e.Properties[FieldworkRoleKey]="wicket";
                            render.DisplayName="garden wicket";
                            e.GetPart<ExaminablePart>().Text+=" A drawgourd bed lies north of this shelter wall. Its ripe shell carries water; keep its seed for the prepared bed. A loose pallet in the supply cellar can provide timber if dismantled. The path around the west end stays open.";
                        }
                        if(p.X==46&&p.Y==4&&p.Blueprint=="Grass")
                        {
                            if(!e.HasTag("Terrain")||e.GetPart<PhysicsPart>()?.Solid!=false||e.GetPart<PhysicsPart>().Takeable
                                ||e.HasPart<CultivatedSoilPart>())return Reject(zone,"invalid-fieldwork-ground");
                            e.SetTag("Plantable");e.AddPart(new CultivatedSoilPart());e.Properties[FieldworkRoleKey]="soil";
                        }
                        if(p.Blueprint=="DrawgourdCrop")
                        {
                            var crop=e.GetPart<CropPart>();
                            if(!ValidGarden(e,false))return Reject(zone,"invalid-fieldwork-crop");
                            crop.GrowthStage=2;
                            render.RenderString=crop.GlyphForStage(2).ToString();render.ColorString=crop.ColorForStage(2);
                            e.Properties[FieldworkRoleKey]="garden";
                        }
                        if(p.Blueprint=="Signpost"&&p.X==43)
                            e.GetPart<ExaminablePart>().Text+=" A drawgourd bed grows north of the shelter wall. Its ripe shell carries water; keep its seed for the same prepared bed. The buckled wicket needs two salvaged timber, or walk around the west end. A loose pallet in the cellar can supply timber if you dismantle it. Fill a carried water vessel beside fresh water, then tend an adjacent unripe crop on prepared soil.";
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
            if(fieldwork)
            {
                // Later factory callbacks cannot silently change an earlier new owner.
                int wickets=0,gardens=0,soils=0;
                var ids=new HashSet<string>(StringComparer.Ordinal);var owners=new HashSet<Entity>();
                foreach(var e in staged)
                {
                    if(!owners.Add(e)||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID)||e.SpatialZone!=null)return Reject(zone,"invalid-fieldwork-graph");
                    string role=e.GetProperty(FieldworkRoleKey);
                    if(role=="wicket"){wickets++;if(!SpreadExplorationPassage.ValidGate(e,false,true))return Reject(zone,"changed-fieldwork-wicket");}
                    if(role=="garden"){gardens++;if(!ValidGarden(e,true))return Reject(zone,"changed-fieldwork-crop");}
                    if(role=="soil")
                    {soils++;if(e.BlueprintName!="Grass"||!e.HasTag("Terrain")||!e.HasTag("Plantable")||e.GetPart<CultivatedSoilPart>()?.ParentEntity!=e
                        ||e.GetPart<PhysicsPart>()?.Solid!=false||e.GetPart<PhysicsPart>().Takeable)return Reject(zone,"changed-fieldwork-ground");}
                }
                if(wickets!=1||gardens!=1||soils!=1)return Reject(zone,"missing-fieldwork-owner");
            }
            for(int i=0;i<staged.Count;i++)
            {
                var p=plan.Placements[i];
                if(!zone.AddEntity(staged[i],p.X,p.Y))
                {for(int j=0;j<i;j++)zone.RemoveEntity(staged[j]);return Reject(zone,"placement-refused");}
            }
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(ReferenceGladePlan.IsPond(x,y))
                {zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);zone.GenReservedCells.Add((x,y));}
                if(ReferenceGladePlan.IsApproach(x,y)||Math.Max(Math.Abs(x-40),Math.Abs(y-12))<=2)
                    zone.GenReservedCells.Add((x,y));
            }
            Diag.Record("worldgen","ReferenceGladeBuilt",payload:new{zoneId=zone.ZoneID,seed,owners=staged.Count,
                discoveries="reed-pond,working-shelter,ruin-shortcut",grain=3,waterDrams=3});
            return true;
        }
        private static bool ValidGarden(Entity e,bool ripe)
        {
            var c=e?.GetPart<CropPart>();var p=e?.GetPart<PhysicsPart>();
            if(e==null)return false;
            foreach(var part in e.Parts)if(part==null||part.ParentEntity!=e)return false;
            return e.BlueprintName=="DrawgourdCrop"&&e.HasTag("Crop")&&c?.ParentEntity==e&&c.HarvestAtMaturity
                &&c.GrowthStage==(ripe?2:0)&&c.TicksInStage==0&&c.MoistureTicks==0
                &&c.YieldBlueprint=="DrawgourdShell"&&c.YieldCount==1&&c.SeedYieldBlueprint=="DrawgourdSeed"&&c.SeedYieldCount==1
                &&p?.ParentEntity==e&&!p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
                &&e.GetPart<RenderPart>()?.ParentEntity==e&&e.SpatialZone==null;
        }
        private static void DescribeDiscovery(Entity entity,ReferenceGladePlan.Placement placement)
        {
            string text=null;
            switch(placement.Blueprint)
            {
                case "Campfire":
                    entity.GetPart<CampfirePart>().FiniteCooking=true;
                    entity.GetPart<RenderPart>().DisplayName="gleaners' cooking fire";
                    text="A low ruined wall shelters this working fire. Ripe heads remain in the nearby emberwheat strips. Gather grain by hand, then cook it beside the fire while its fuel and heat last. Rest here only when the surroundings are safe.";
                    break;
                case "SpreadDrawPoint":
                    entity.GetPart<RenderPart>().DisplayName="reed-bank water basin";
                    text="A stone-lined pocket collects a little clear water at the reed pond's edge. Fill a carried vessel here; the basin holds only three drams. The surrounding wet ground cannot replenish it.";
                    break;
                case "FallenBeam":
                    text="This fallen roof timber blocks the short doorway through the old store-room. Take hold from the clear eastern shoulder and pull it aside, or follow the open passage to the north. Moving the beam changes the route; it does not hide you from watchful eyes.";
                    break;
                case "Signpost":
                    text=placement.X==43
                        ?"Three worn cuts point to the reed basin northwest, the gleaners' fire north, and the broken store-room west. Beyond the western edge lies Sill. Ripe grain, a few clear drinks, and a loose roof timber reward a closer look."
                        :"SILL lies west through the ruined wall. The north track runs into fallow ground; the east and south open into flower meadows. Below the post, an older hand has scratched: clear water by the reeds; grain beside the sheltered fire.";
                    break;
            }
            if(text!=null)
            {
                var examine=entity.GetPart<ExaminablePart>();
                if(examine==null){examine=new ExaminablePart();entity.AddPart(examine);}
                examine.Text=text;
            }
        }
        private static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","ReferenceGladeRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
