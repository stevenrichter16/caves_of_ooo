using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Optional shelter on cold LastGleanings field margins. The original land,
    /// hazards and finite rows retain first claim; loaded graphs are never dressed.</summary>
    public sealed class SpreadFieldMarginBuilder : IZoneBuilder
    {
        public string Name=>"SpreadFieldMargin";
        public int Priority=>3950;
        readonly OverworldZoneManager manager;
        readonly SpreadExplorationPlan manifest;
        readonly SpreadCompositionBuilder terrain;
        SpreadCompositionPlan completed;
        public SpreadFieldMarginBuilder(OverworldZoneManager manager,SpreadCompositionBuilder terrain)
        {this.manager=manager;manifest=manager?.Exploration;this.terrain=terrain;}
        static readonly (int x,int y,string bp)[][] Shapes={
            new[]{(0,0,"Tree"),(1,2,"Tree"),(-1,0,"Bush"),(1,0,"Bush"),(0,1,"Bush"),(-1,2,"Bush")},
            new[]{(0,0,"Tree"),(-1,0,"Bush"),(1,1,"Bush"),(0,2,"Bush"),(-1,2,"Bush")},
            new[]{(-1,0,"Bush"),(0,0,"Bush"),(1,1,"Bush"),(0,2,"Bush")}};

        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            var plan=terrain?.Plan;
            bool Current()=>zone!=null&&manager!=null&&ReferenceEquals(factory,manager.Factory)
                &&ReferenceEquals(manifest,manager.Exploration)&&terrain.SourceZone==zone&&terrain.Plan==plan
                &&plan!=null&&plan.ZoneID==zone.ZoneID&&plan.Seed==manager.WorldSeed
                &&plan.Formation==Formation.FieldStrips&&plan.Topology==SpreadExplorationTopology.BankCrossing
                &&manifest.TryGetGenerationEntry(manager,zone,out var entry)
                &&entry.Family==SpreadExplorationFamily.LastGleanings&&entry.Topology==plan.Topology;
            if(!Current()||ReferenceEquals(completed,plan))return true;
            completed=plan;
            if(!factory.Blueprints.ContainsKey("Tree")||!factory.Blueprints.ContainsKey("Bush"))return true;
            var initial=new HashSet<Entity>(zone.GetReadOnlyEntities());
            var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,initial);
            string tileState=zone.TileState.ToSaveString();var reserves=new HashSet<(int x,int y)>(zone.GenReservedCells);
            bool Original()=>Current()&&unchanged()&&initial.SetEquals(zone.GetReadOnlyEntities())
                &&tileState==zone.TileState.ToSaveString()&&reserves.SetEquals(zone.GenReservedCells);
            // Validate all payloads before publishing any group. In particular a Barren
            // owner could erase existing Grass during AddEntity, beyond additive rollback.
            var staged=new Entity[Shapes.Length][];
            for(int g=0;g<Shapes.Length;g++)
            {
                staged[g]=new Entity[Shapes[g].Length];
                for(int i=0;i<Shapes[g].Length;i++)
                {var e=factory.CreateEntity(Shapes[g][i].bp);if(!Original())return false;if(!Payload(e,Shapes[g][i].bp))return true;staged[g][i]=e;}
            }
            if(!Original())return false;
            var ids=new HashSet<string>(initial.Select(e=>e.ID),StringComparer.Ordinal);
            for(int g=0;g<Shapes.Length;g++)for(int i=0;i<Shapes[g].Length;i++)
                if(!Payload(staged[g][i],Shapes[g][i].bp)||!ids.Add(staged[g][i].ID))return true;
            var anchors=new[]{(x:32,y:7),(x:41,y:7),(x:54,y:17)};
            for(int g=0;g<Shapes.Length;g++)
            {
                var original=zone.GetReadOnlyEntities().ToArray();
                var intact=SpreadGenerationReceipt.CaptureFinalState(zone,original);
                var geometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
                var origin=anchors[g];
                var candidates=from y in Enumerable.Range(origin.y-1,3) from x in Enumerable.Range(origin.x-1,3)
                    orderby Math.Abs(x-origin.x)+Math.Abs(y-origin.y),plan.Roll(x,y,977+g),y,x select (x,y);
                foreach(var at in candidates)
                {
                    var positions=Shapes[g].Select(s=>(x:at.x+s.x,y:at.y+s.y)).ToArray();
                    if(!positions.All(p=>Fits(zone,plan,geometry,p.x,p.y)))continue;
                    var solids=positions.Where((p,i)=>staged[g][i].GetPart<PhysicsPart>().Solid).ToArray();
                    if(!geometry.PreservesRoutes(solids))continue;
                    var reach=geometry.Flood(new HashSet<(int x,int y)>(solids),null,int.MaxValue,null);
                    bool connected=true;
                    for(int y=0;y<Zone.Height&&connected;y++)for(int x=0;x<Zone.Width;x++)
                        if(geometry.BorderReach[x,y]&&!reach[x,y]&&!solids.Contains((x,y))){connected=false;break;}
                    if(!connected||!Current()||!intact())continue;
                    var added=new List<Entity>();bool complete=true;
                    for(int i=0;i<positions.Length;i++)
                    {var p=positions[i];if(!zone.AddEntity(staged[g][i],p.x,p.y)){complete=false;break;}added.Add(staged[g][i]);}
                    if(!complete||!Current()||!intact())
                    {foreach(var e in added)if(zone.GetEntityCell(e)!=null)zone.RemoveEntity(e);break;}
                    break;
                }
            }
            return true;
        }
        static bool Fits(Zone z,SpreadCompositionPlan p,SpreadWildernessSituationBuilder.Geometry geometry,int x,int y)
        {
            if(!geometry.Place(x,y)||p.IsApproach(x,y)||p.IsLawn(x,y)||p.IsWater(x,y)||p.ObjectAt(x,y)!=null)return false;
            for(int i=0;i<p.ParcelCount;i++)if(p.GetParcel(i).Contains(x,y))return false;
            var c=z.GetCell(x,y);if(c.Objects.Count!=1)return false;
            var ground=c.Objects[0];
            return ground.BlueprintName=="Grass"&&ground.HasTag("Terrain")&&ground.HasTag("Plantable")
                &&!ground.HasTag("Barren")&&!ground.HasPart<CropPart>()&&!ground.HasPart<CultivatedSoilPart>()
                &&!ground.HasPart<FieldHarvestPart>()&&!ground.HasPart<HarvestablePart>()&&!ground.HasPart<SpatialFootprintPart>();
        }
        static bool Payload(Entity e,string bp)
        {
            var p=e?.GetPart<PhysicsPart>();
            return e!=null&&!string.IsNullOrEmpty(e.ID)&&e.BlueprintName==bp&&e.SpatialZone==null&&p!=null&&p.ParentEntity==e
                &&!p.Takeable&&p.InInventory==null&&p.Equipped==null&&p.Solid==(bp=="Tree")
                &&!e.HasTag("Barren")&&!e.HasTag("Creature")&&!e.HasTag("Item")
                &&!(bp=="Bush"&&e.HasTag("Solid"))&&e.GetPart<RenderPart>()?.Visible==true
                &&e.Parts.All(part=>part!=null&&part.ParentEntity==e)
                &&!e.HasPart<CropPart>()&&!e.HasPart<CultivatedSoilPart>()&&!e.HasPart<FieldHarvestPart>()
                &&!e.HasPart<HarvestablePart>()&&!e.HasPart<InventoryPart>()&&!e.HasPart<ContainerPart>()
                &&!e.HasPart<TraderPart>()&&!e.HasPart<ConversationPart>()&&!e.HasPart<SpatialFootprintPart>()&&!e.HasPart<ThrowingStatePart>()
                &&!e.HasPart<TileStateSourcePart>()&&!e.HasPart<LiquidPoolPart>()&&!e.HasPart<GasPoolPart>()
                &&!e.HasPart<TriggerOnStepPart>()&&!e.HasPart<DoorPart>()&&!e.HasPart<SealedLibraryBarrierPart>()
                &&!e.HasPart<StairsUpPart>()&&!e.HasPart<StairsDownPart>();
        }
    }
}
