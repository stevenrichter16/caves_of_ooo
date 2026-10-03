using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Cold generation only: a repair service and a real cellar entrance
    /// in the existing glade. Loaded graphs are never retrofitted. These two small
    /// district zones are retained even after every visible owner is destroyed.</summary>
    public static class GleanersDistrict
    {
        public const string RoleKey="GleanersDistrict.Role";
        public const string SurfaceID=ReferenceGladePlan.ZoneID;
        public const int EntranceX=28, EntranceY=19;
        static readonly (string blueprint,string role,int x,int y)[] placements=
        {
            ("RepairLinedWell","well",41,8),
            ("StairsDown","stairs",EntranceX,EntranceY),
            ("Signpost","notice",41,10)
        };

        /// <summary>Fixed bounded retention, not generation authority. Retaining
        /// a legacy graph preserves it literally and cannot add new owners.</summary>
        public static bool Retain(string id)=>id==SurfaceID||id==GleanersCellarBuilder.ZoneID;

        public static bool SupportsColumn(OverworldZoneManager manager)
            =>manager?.WorldMap!=null&&manager.WorldMap.GetBiome(11,10)==BiomeType.Spread
                &&manager.WorldMap.GetPOI(11,10)==null&&ReferenceGladeBuilder.SupportsContent(manager.Factory)
                &&GleanersCellarBuilder.SupportsContent(manager.Factory)
                &&placements.All(p=>manager.Factory.Blueprints.ContainsKey(p.blueprint));

        /// <summary>A cellar needs an actual, current surface endpoint and exact
        /// saved route. An address alone must not adopt an old saved glade.</summary>
        public static bool CanBuildCellar(OverworldZoneManager manager)
        {
            if(!SupportsColumn(manager)||!manager.CachedZones.TryGetValue(SurfaceID,out var surface)
                ||surface.ZoneID!=SurfaceID)return false;
            return surface.GetCell(EntranceX,EntranceY).Objects.Any(e=>e.GetProperty(RoleKey)=="stairs"&&e.HasPart<StairsDownPart>())
                &&manager.GetConnections(SurfaceID).Any(c=>c.SourceZoneID==SurfaceID&&c.SourceX==EntranceX&&c.SourceY==EntranceY
                    &&c.TargetZoneID==GleanersCellarBuilder.ZoneID&&c.TargetX==GleanersCellarBuilder.StairsX
                    &&c.TargetY==GleanersCellarBuilder.StairsY&&c.Type=="StairsDown");
        }

        /// <summary>Stage the whole addition before publishing. A cached cellar,
        /// occupied placement, missing dependency or callback mutation preserves
        /// the original glade and registers no route.</summary>
        public static bool TryInstall(Zone zone,OverworldZoneManager manager)
        {
            if(zone?.ZoneID!=SurfaceID||!SupportsColumn(manager)
                ||manager.CachedZones.ContainsKey(SurfaceID)||manager.CachedZones.ContainsKey(GleanersCellarBuilder.ZoneID)
                ||zone.GetReadOnlyEntities().Any(e=>e.GetProperty(RoleKey)!=null))return false;
            bool Clear()=>placements.All(p=>zone.GetCell(p.x,p.y).Objects.All(e=>e.BlueprintName=="Grass"||e.BlueprintName=="RoadStone"));
            if(!Clear())return Reject(zone,"occupied-placement");
            var original=new HashSet<Entity>(zone.GetReadOnlyEntities());
            var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            var ids=new HashSet<string>(original.Select(e=>e.ID));
            var staged=new List<Entity>();var added=new List<Entity>();
            var detachedProofs=new List<Func<bool>>();
            var placedProofs=new List<Func<bool>>();
            bool Authority()=>SupportsColumn(manager)&&!manager.CachedZones.ContainsKey(SurfaceID)
                &&!manager.CachedZones.ContainsKey(GleanersCellarBuilder.ZoneID);
            bool Before()=>Authority()&&unchanged()&&original.SetEquals(zone.GetReadOnlyEntities())&&Clear()
                &&detachedProofs.All(proof=>proof());
            try
            {
                foreach(var p in placements)
                {
                    var e=manager.Factory.CreateEntity(p.blueprint);
                    if(e==null||e.BlueprintName!=p.blueprint||e.SpatialZone!=null||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID)
                        ||e.Parts.Any(part=>part==null||part.ParentEntity!=e)||e.GetPart<PhysicsPart>() is not PhysicsPart physics
                        ||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null||e.GetPart<RenderPart>()?.Visible!=true
                        ||e.HasPart<SpatialFootprintPart>()||!Before())
                        return Reject(zone,"invalid-staged-owner");
                    e.Properties[RoleKey]=p.role;
                    if(p.role=="well")
                    {
                        var recipe=RepairRecipeRegistry.Get("clay-well-lining");
                        if(e.GetPart<RepairablePart>()?.RecipeId!="clay-well-lining"||e.GetPart<WellPart>()?.IsUsable!=false
                            ||recipe==null||e.Parts.Count(part=>part is RepairablePart)!=1||e.Parts.Count(part=>part is CompositionPart)!=1
                            ||e.GetPart<CompositionPart>()?.Contains(recipe.Composition)!=true)
                            return Reject(zone,"invalid-well");
                        if(manager.Exploration?.Version>=11 && !ConnectedSpreadProgress.BindRepair(e,manager.Exploration.WorldKey))return Reject(zone,"invalid-well-origin");
                        e.GetPart<RenderPart>().DisplayName="gleaners' lined well";
                        Describe(e,"Fire clay seals this well's fired lining. A sound lining allows drinking and vessel filling; repairing a split takes two measures of fire clay. The old supply cellar in the western ruin stored repair clay. The reed basin nearby holds only a few drinks; it cannot refill this well.");
                    }
                    else if(p.role=="stairs")
                    {
                        if(!e.HasPart<StairsDownPart>()||physics.Solid||e.HasTag("Solid"))return Reject(zone,"invalid-stair");
                        e.GetPart<RenderPart>().DisplayName="steps to the gleaners' supply cellar";
                        Describe(e,"Worn steps lead to the gleaners' old supply store. Clay for the cracked well was kept below. Scratching travels up the nearer passage; a longer service passage runs around the store. The landing remains the way back.");
                    }
                    else
                    {
                        e.GetPart<RenderPart>().DisplayName="gleaners' working notice";
                        string text="WELL LINING SPLIT. Two measures of fire clay needed. Stores below the western ruin; leave the landing clear. A second hand adds: the field alembic is one stretch north, the forge one east. Frost lichen can be brewed into a freezing coating and used to quench a melee weapon at a forge, at a cost to its durability. Put the weapon in your pack first. Sill is west; the wayside kitchen is southeast. Marrowstye's receiving hall lies one stretch farther south from the kitchen.";
                        if(manager.Exploration?.Version>=11)
                            text+=" Another hand notes: the kitchen's batch pan also needs two measures of fire clay, for wrapped field meals. Its public oven and cot need no repair. Clay spent there cannot line this well until more is found.";
                        Describe(e,text);
                    }
                    staged.Add(e);
                    detachedProofs.Add(SpreadGenerationReceipt.CaptureDetachedState(e));
                }
                if(!Before())return Reject(zone,"changed-before-publication");
                for(int i=0;i<staged.Count;i++)
                {
                    if(!Authority()||!unchanged()||!original.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                        ||!placedProofs.All(proof=>proof())||!detachedProofs.Skip(i).All(proof=>proof()))return Reject(zone,"changed-during-publication");
                    var p=placements[i];if(!zone.AddEntity(staged[i],p.x,p.y))return Reject(zone,"placement-refused");added.Add(staged[i]);
                    placedProofs.Add(SpreadGenerationReceipt.CaptureFinalState(zone,new[]{staged[i]}));
                }
                if(!Authority()||!unchanged()||!original.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                    ||!placedProofs.All(proof=>proof()))return Reject(zone,"changed-after-publication");
                manager.RegisterConnection(new ZoneConnection{SourceZoneID=SurfaceID,SourceX=EntranceX,SourceY=EntranceY,
                    TargetZoneID=GleanersCellarBuilder.ZoneID,TargetX=GleanersCellarBuilder.StairsX,TargetY=GleanersCellarBuilder.StairsY,Type="StairsDown"});
                added.Clear();
                Diag.Record("worldgen","GleanersDistrictInstalled",payload:new{zoneId=zone.ZoneID,cellar=GleanersCellarBuilder.ZoneID});
                return true;
            }
            catch(Exception error){return Reject(zone,"staging-"+error.GetType().Name);}
            finally{foreach(var e in added)if(e.SpatialZone==zone)zone.RemoveEntity(e);}
        }
        static void Describe(Entity e,string text)
        {
            var part=e.GetPart<ExaminablePart>();if(part==null){part=new ExaminablePart();e.AddPart(part);}part.Text=text;
        }
        static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","GleanersDistrictRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
