using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Optional small cold-generation patches, after ordinary occupants.
    /// Exact source terrain is preserved. No cached migration or world RNG calls.</summary>
    public sealed class BiomeCropPlacement : IZoneBuilder
    {
        public string Name => "BiomeCropPlacement";
        public int Priority => 4310;
        readonly OverworldZoneManager manager;
        readonly BiomeCropSite site;
        public BiomeCropPlacement(OverworldZoneManager manager,BiomeCropSite site)
        {this.manager=manager;this.site=site;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory!=manager?.Factory)return false;
            var original=zone.GetReadOnlyEntities().ToArray();var proof=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            if(TryInstall(manager,zone,site))return true;
            // A factory callback that changed another owner must not get its
            // changed cold graph accepted as an ordinary optional refusal.
            return proof() && original.ToHashSet().SetEquals(zone.GetReadOnlyEntities());
        }

        public static bool TryInstall(OverworldZoneManager manager,Zone zone,BiomeCropSite site)
        {
            if(manager?.Factory==null||zone==null||site==null||site.ZoneID!=zone.ZoneID||site.WorldSeed!=manager.WorldSeed
                ||manager.CachedZones.ContainsKey(zone.ZoneID)||HasPatch(zone))return false;
            var current=BiomeCropPlan.ForZone(manager,zone.ZoneID);
            if(current==null||current.Key!=site.Key||current.Biome!=site.Biome)return false;
            var catalogue=BiomeCropCatalog.ForBiome(site.Biome);
            if(catalogue.Count!=5||site.SpeciesIndex<0||site.SpeciesIndex>=catalogue.Count)return false;
            var crop=catalogue[site.SpeciesIndex];var factory=manager.Factory;
            if(!factory.Blueprints.ContainsKey(crop.CropBlueprint)||!factory.Blueprints.ContainsKey(crop.SeedBlueprint)
                ||!factory.Blueprints.ContainsKey(crop.YieldBlueprint))return Reject(zone,"missing-content");
            var original=zone.GetReadOnlyEntities().ToArray();var source=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            var reached=Reach(manager,zone,site.Biome==BiomeType.Cave);
            var rim=site.Biome==BiomeType.Overwrit?OverwritCompositionPlan.Create(zone.ZoneID,manager.WorldSeed):null;
            var candidates=new List<Cell>();
            for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
            {
                var cell=zone.GetCell(x,y);
                if(reached.Contains((x,y))&&Bare(zone,cell,site.Biome==BiomeType.Cave)
                    &&(rim==null||rim.IsRim(x,y))&&!original.Any(e=>e.HasTag("Creature")&&Distance(zone.GetEntityPosition(e),(x,y))<5))candidates.Add(cell);
            }
            uint rank=BiomeCropPlan.Rank(manager.WorldSeed,zone.ZoneID,"patch");
            int ax=10+(int)(rank%60),ay=4+(int)((rank>>8)%17);
            candidates=candidates.OrderBy(c=>Distance((ax,ay),(c.X,c.Y))).ThenBy(c=>c.Y).ThenBy(c=>c.X).ToList();
            Cell first=null,second=null;
            foreach(var a in candidates)
            {
                second=candidates.FirstOrDefault(b=>b!=a&&Distance((a.X,a.Y),(b.X,b.Y))>=2&&Distance((a.X,a.Y),(b.X,b.Y))<=4);
                if(second!=null){first=a;break;}
            }
            if(first==null)return Reject(zone,"no-safe-connected-ground");
            var cells=new[]{first,second};var staged=new List<Entity>();var added=new List<Entity>();
            var soils=new List<(Entity terrain,CultivatedSoilPart part,bool plantable)>();bool success=false;
            try
            {
                for(int i=0;i<2;i++)
                {
                    var owner=factory.CreateEntity(crop.CropBlueprint);
                    if(!Fresh(owner,crop.CropBlueprint)||!source())return Reject(zone,"invalid-crop-source");
                    var part=owner.GetPart<CropPart>();
                    if(!WorkingCrop(owner,crop))return Reject(zone,"invalid-crop-definition");
                    part.GrowthStage=i==0?2:(int)((rank>>16)%2);part.TicksInStage=0;part.MoistureTicks=0;
                    var render=owner.GetPart<RenderPart>();render.RenderString=part.GlyphForStage(part.GrowthStage).ToString();render.ColorString=part.ColorForStage(part.GrowthStage);
                    staged.Add(owner);
                }
                // ObjectCreated can reach another staged plant. Revalidate the
                // whole packet after the last callback, before publishing any.
                if(staged.Select(e=>e.ID).Distinct().Count()!=2||staged.Any(e=>original.Any(old=>old.ID==e.ID))||!source()
                    ||staged.Where((e,i)=>!Fresh(e,crop.CropBlueprint)||!WorkingCrop(e,crop)
                        ||e.GetPart<CropPart>().GrowthStage!=(i==0?2:(int)((rank>>16)%2))).Any())return Reject(zone,"changed-crop-source");
                for(int i=0;i<2;i++)
                {
                    if(!Bare(zone,cells[i],site.Biome==BiomeType.Cave)||!Fresh(staged[i],crop.CropBlueprint)||!WorkingCrop(staged[i],crop)||!source()
                        ||!zone.AddEntity(staged[i],cells[i].X,cells[i].Y))return Reject(zone,"placement-refused");
                    added.Add(staged[i]);
                }
                foreach(var cell in cells)
                {
                    var terrain=cell.Objects.Single(e=>e.HasTag("Terrain"));var soil=new CultivatedSoilPart();bool had=terrain.HasTag("Plantable");
                    terrain.SetTag("Plantable");zone.NotifyEntityTagAdded(terrain,"Plantable");terrain.AddPart(soil);soils.Add((terrain,soil,had));
                }
                var receipt=new Entity{ID=site.Key,BlueprintName="BiomeCropPatchRecord"};
                receipt.SetTag(WorldInteractionSystem.WorldMetadataTag);
                receipt.AddPart(new BiomeCropPatchPart{SiteKey=site.Key,CropBlueprint=crop.CropBlueprint});
                if(zone.GetReadOnlyEntities().Any(e=>e.ID==receipt.ID)||!zone.AddEntity(receipt,first.X,first.Y))return Reject(zone,"receipt-refused");
                added.Add(receipt);success=true;
                Diag.Record("worldgen","BiomeCropPatchPlaced",target:staged[0],payload:new{zoneId=zone.ZoneID,biome=site.Biome.ToString(),species=crop.Id,beds=2});
                return true;
            }
            finally
            {
                if(!success)
                {
                    foreach(var soil in soils){soil.terrain.RemovePart(soil.part);if(!soil.plantable){soil.terrain.Tags.Remove("Plantable");zone.NotifyEntityTagRemoved(soil.terrain,"Plantable");}}
                    foreach(var owner in added.AsEnumerable().Reverse())if(owner.SpatialZone==zone)zone.RemoveEntity(owner);
                }
            }
        }
        static bool Fresh(Entity owner,string blueprint)=>owner!=null&&owner.BlueprintName==blueprint&&!string.IsNullOrEmpty(owner.ID)
            &&owner.SpatialZone==null&&owner.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==owner&&!p.Takeable&&!p.Solid
            &&p.InInventory==null&&p.Equipped==null&&owner.GetPart<RenderPart>()?.Visible==true&&!owner.HasPart<SpatialFootprintPart>()
            &&owner.Parts.All(part=>part!=null&&part.ParentEntity==owner);
        static bool WorkingCrop(Entity owner,BiomeCropDefinition definition)
        {
            var crop=owner?.GetPart<CropPart>();
            return owner?.HasTag("Crop")==true&&crop!=null&&crop.ParentEntity==owner&&crop.HarvestAtMaturity
                &&crop.YieldBlueprint==definition.YieldBlueprint&&crop.YieldCount==definition.YieldCount&&crop.YieldCount>0
                &&crop.SeedYieldBlueprint==definition.SeedBlueprint&&crop.SeedYieldCount==1
                &&crop.TicksPerStage==definition.TicksPerStage&&crop.TicksPerStage>0
                &&crop.StageGlyphsRaw==definition.StageGlyphsRaw&&crop.StageColorsRaw==definition.StageColorsRaw
                &&crop.TicksInStage==0&&crop.MoistureTicks==0;
        }
        static bool Bare(Zone zone,Cell cell,bool cave)
        {
            if(cell==null||(!cave&&cell.IsInterior)||cell.BlocksMovement()||zone.GenReservedCells.Contains((cell.X,cell.Y))
                ||zone.TileState.Has(cell.X,cell.Y)||BarrenGroundRules.IsBarren(cell)||cell.Objects.Count!=1||cell.Occupants.Count!=1)return false;
            var terrain=cell.Objects[0];
            return DoorPart.IsBareGround(terrain)&&terrain.SpatialZone==zone&&zone.GetEntityCell(terrain)==cell
                &&terrain.GetPart<PhysicsPart>()?.ParentEntity==terrain&&!terrain.HasPart<CultivatedSoilPart>()
                &&new[]{"Grass","Sand","TepuiStone","OverwritGround","Floor","SandstoneFloor","LimestoneFloor","ShaleFloor","SlateFloor","QuartziteFloor","ObsidianFloor"}.Contains(terrain.BlueprintName);
        }
        static HashSet<(int,int)> Reach(OverworldZoneManager manager,Zone zone,bool cave)
        {
            var reached=new HashSet<(int,int)>();var queue=new Queue<Cell>();Cell start=null;
            if(cave)
            {
                foreach(var edge in manager.GetConnectionsTo(zone.ZoneID,"StairsDown"))
                {
                    var candidate=zone.GetCell(edge.TargetX,edge.TargetY);
                    if(candidate?.HasObjectWithPart<StairsUpPart>()==true&&!candidate.BlocksMovement()){start=candidate;break;}
                }
            }
            else
            {
                for(int r=0;r<Zone.Width&&start==null;r++)
                    for(int y=Math.Max(0,12-r);y<=Math.Min(24,12+r)&&start==null;y++)
                        for(int x=Math.Max(0,40-r);x<=Math.Min(79,40+r);x++)
                            if(!zone.GetCell(x,y).BlocksMovement()){start=zone.GetCell(x,y);break;}
            }
            if(start==null)return reached;
            reached.Add((start.X,start.Y));queue.Enqueue(start);
            var dirs=new[]{(1,0),(-1,0),(0,1),(0,-1)};
            while(queue.Count>0){var at=queue.Dequeue();foreach(var d in dirs){var n=zone.GetCell(at.X+d.Item1,at.Y+d.Item2);if(n!=null&&!n.BlocksMovement()&&reached.Add((n.X,n.Y)))queue.Enqueue(n);}}
            return reached;
        }
        static int Distance((int x,int y)a,(int x,int y)b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
        public static bool HasPatch(Zone zone)=>zone?.GetReadOnlyEntities().Any(e=>e.HasPart<BiomeCropPatchPart>())==true;
        internal static bool Retain(OverworldZoneManager manager,string id)=>manager?.CachedZones.TryGetValue(id,out var zone)==true&&HasPatch(zone);
        static bool Reject(Zone zone,string reason){Diag.Record("worldgen","BiomeCropPatchRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
