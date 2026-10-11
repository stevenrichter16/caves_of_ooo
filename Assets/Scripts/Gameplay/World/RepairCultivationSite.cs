using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A finite allotment on fresh western Morrowfast field generation.
    /// Existing terrain and expedition owners remain literal. No load-time refill.</summary>
    public static class RepairCultivationSite
    {
        public const string ZoneID="Overworld.2.6.0", RoleKey="RepairCultivation.Role";
        private const string AnchorID="repair-cultivation:allotment-notice";
        private static readonly (string bp,string role,int x,int y,int stage)[] Specs={
            ("RepairLinedWell","lining",-4,-2,-1),("RepairRopeWell","rope",4,-2,-1),
            ("RepairWoodenGate","gate",0,3,-1),("RepairClayBank","clay",-4,1,-1),
            ("RepairTimberPile","timber",4,1,-1),("RepairCordBundle","cord",4,3,-1),
            ("KnotflaxCrop","knotflax-ripe",-2,0,2),("HearthbulbCrop","hearthbulb-ripe",0,0,2),("SeamleafCrop","seamleaf-ripe",2,0,2),
            ("KnotflaxCrop","knotflax-seed",-2,2,0),("HearthbulbCrop","hearthbulb-seed",0,2,0),("SeamleafCrop","seamleaf-seed",2,2,0),
            ("Signpost","notice",0,-2,-1)};
        public static bool TryInstall(Zone zone,EntityFactory factory)
        {
            if(zone?.ZoneID!=ZoneID || factory==null || zone.GetReadOnlyEntities().Any(e=>e.GetProperty(RoleKey)!=null)
                || Specs.Any(s=>!factory.Blueprints.ContainsKey(s.bp)))return false;
            var before=zone.GetReadOnlyEntities().ToArray();var source=SpreadGenerationReceipt.CaptureFinalState(zone,before);
            var reachable=Reach(zone,new HashSet<(int,int)>());var cells=new Cell[Specs.Length];
            var occupied=new HashSet<Cell>();var blocked=new HashSet<(int,int)>();
            // Keep it near the eastward town path while allowing real flora and routes to
            // determine the layout. Never clear the field to force a showcase rectangle.
            for(int i=0;i<Specs.Length;i++)
            {
                var spec=Specs[i];int wantedX=62+spec.x,wantedY=11+spec.y;
                var options=from y in Enumerable.Range(3,Zone.Height-6) from x in Enumerable.Range(44,Zone.Width-48)
                    let c=zone.GetCell(x,y) where !occupied.Contains(c) && reachable.Contains((x,y)) && Bare(c)
                    && Math.Abs(x-wantedX)+Math.Abs(y-wantedY)<=9
                    && !before.Any(e=>e.HasTag("Creature")&&Distance(zone.GetEntityPosition(e),(x,y))<=4)
                    orderby Math.Abs(x-wantedX)+Math.Abs(y-wantedY),y,x select c;
                foreach(var candidate in options)
                {
                    if(i<2)
                    {
                        var trial=new HashSet<(int,int)>(blocked){(candidate.X,candidate.Y)};var after=Reach(zone,trial);
                        if(reachable.Any(p=>!trial.Contains(p)&&!after.Contains(p)))continue;
                        blocked=trial;
                    }
                    cells[i]=candidate;occupied.Add(candidate);break;
                }
                if(cells[i]==null)return Reject("no-safe-site");
            }
            var staged=new List<Entity>();var added=new List<Entity>();var soils=new List<(Entity,CultivatedSoilPart,bool)>();bool success=false;
            try
            {
                for(int i=0;i<Specs.Length;i++)
                {
                    var spec=Specs[i];var e=factory.CreateEntity(spec.bp);
                    if(!Fresh(e,spec.bp)||!source())return Reject("invalid-source");
                    e.Properties[RoleKey]=spec.role;
                    if(spec.stage>=0)
                    {
                        var crop=e.GetPart<CropPart>();if(crop==null||!crop.HarvestAtMaturity)return Reject("invalid-crop");
                        crop.GrowthStage=spec.stage;crop.TicksInStage=0;crop.MoistureTicks=0;
                        var render=e.GetPart<RenderPart>();render.RenderString=crop.GlyphForStage(spec.stage).ToString();render.ColorString=crop.ColorForStage(spec.stage);
                    }
                    if(spec.role=="notice")
                    {
                        e.ID=AnchorID;e.GetPart<RenderPart>().DisplayName="allotment notice";
                        e.GetPart<ExaminablePart>().Text="The allotment west of Morrowfast. A split catch-well lining takes two fire clay; the sound hoist well needs one knotflax cord. Two lengths of salvaged timber brace the jammed gate. Exposed clay, fallen timber and a dry cord bundle lie nearby. Examine a damaged object, then Repair it with carried supplies. The tilled rows grow knotflax for cord, hearthbulbs for roasting and seamleaf for vital brews. Harvest a ripe plant, pick up its produce and seed, then plant again in its empty bed. The seed keeper and Morrowfast provisioner sell seeds and inert sludge for compost. Plant a seed in a prepared bed, then carry one sludge to the fresh crop and choose compost before any wet growth: once per crop, growth stages take 25 percent less time. Compost supplies no water. Conjure Rain moistens planted crops; dry crops pause, while moist crops grow as time passes even while you are away.";
                    }
                    staged.Add(e);
                }
                if(staged.Select(e=>e.ID).Distinct().Count()!=staged.Count || staged.Any(e=>before.Any(old=>old.ID==e.ID)) || !source())return Reject("changed-source");
                for(int i=0;i<staged.Count;i++)
                {
                    if(!Bare(cells[i]) || !Fresh(staged[i],Specs[i].bp) || !source())return Reject("changed-placement");
                    if(!zone.AddEntity(staged[i],cells[i].X,cells[i].Y))return Reject("placement-refused");added.Add(staged[i]);
                }
                // Publish cultivation only after every staged owner is safely placed.
                for(int i=0;i<Specs.Length;i++)if(Specs[i].stage>=0)
                {
                    var terrain=cells[i].Objects.Single(e=>e.HasTag("Terrain"));bool had=terrain.HasTag("Plantable");
                    var marker=new CultivatedSoilPart();terrain.SetTag("Plantable");zone.NotifyEntityTagAdded(terrain,"Plantable");terrain.AddPart(marker);soils.Add((terrain,marker,had));
                }
                success=true;Diag.Record("worldgen","RepairCultivationPlaced",target:staged.Last(),payload:new{zoneId=zone.ZoneID,owners=added.Count,beds=soils.Count});return true;
            }
            finally
            {
                if(!success)
                {
                    foreach(var soil in soils){soil.Item1.RemovePart(soil.Item2);if(!soil.Item3){soil.Item1.Tags.Remove("Plantable");zone.NotifyEntityTagRemoved(soil.Item1,"Plantable");}}
                    foreach(var e in added.AsEnumerable().Reverse())if(e.SpatialZone==zone)zone.RemoveEntity(e);
                }
            }
        }
        private static bool Fresh(Entity e,string bp)=>e!=null&&e.BlueprintName==bp&&e.SpatialZone==null&&e.GetPart<PhysicsPart>() is PhysicsPart p
            &&p.ParentEntity==e&&p.InInventory==null&&p.Equipped==null&&!p.Takeable&&e.GetPart<RenderPart>()?.Visible==true&&e.Parts.All(part=>part.ParentEntity==e);
        private static bool Bare(Cell c)=>c!=null&&!c.IsInterior&&!c.BlocksMovement()&&!BarrenGroundRules.IsBarren(c)
            &&c.Objects.Count==1&&c.Objects[0].BlueprintName=="Grass"&&c.Objects[0].HasTag("Terrain")&&!c.Objects[0].HasPart<CultivatedSoilPart>();
        private static int Distance((int x,int y)a,(int x,int y)b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
        private static HashSet<(int,int)> Reach(Zone zone,HashSet<(int,int)> excluded)
        {
            var result=new HashSet<(int,int)>();var queue=new Queue<Cell>();
            var start=zone.GetCell(Zone.Width-1,Zone.Height/2);
            if(!start.BlocksMovement()&&!excluded.Contains((start.X,start.Y))){result.Add((start.X,start.Y));queue.Enqueue(start);}
            var dirs=new[]{(1,0),(-1,0),(0,1),(0,-1)};
            while(queue.Count>0){var c=queue.Dequeue();foreach(var d in dirs){var n=zone.GetCell(c.X+d.Item1,c.Y+d.Item2);if(n!=null&&!n.BlocksMovement()&&!excluded.Contains((n.X,n.Y))&&result.Add((n.X,n.Y)))queue.Enqueue(n);}}
            return result;
        }
        internal static bool Retain(OverworldZoneManager manager,string zoneID)=>manager?.CachedZones.TryGetValue(zoneID,out var zone)==true
            && zone.GetReadOnlyEntities().Any(e=>e.ID==AnchorID || e.HasPart<CultivatedSoilPart>());
        private static bool Reject(string reason){Diag.Record("worldgen","RepairCultivationRejected",payload:new{reason});return false;}
    }
}
