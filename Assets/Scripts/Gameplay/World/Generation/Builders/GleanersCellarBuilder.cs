using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Cold composition for the glade's first underground store. The caller owns
    /// authorization, connections and retention; this builder never patches an existing graph.
    /// All physical owners and their finite stock are staged before any zone publication.</summary>
    public sealed class GleanersCellarBuilder : IZoneBuilder
    {
        public const string ZoneID="Overworld.11.10.1", RoleKey="GleanersCellar.Role";
        public const int StairsX=40, StairsY=12;
        public string Name=>"GleanersCellar";
        public int Priority=>2000;
        readonly int seed;
        readonly bool connected;
        readonly string worldKey;
        static readonly string[] Required={"Floor","StoneWall","StairsUp","Crate","FireClay","FallenBeam","Signpost",
            "MarlbackScrabbler","ShatteredRimeGrimoire","Buckler","Dagger","Hatchet","Cudgel","LeatherCap","LeatherGloves"};
        public GleanersCellarBuilder(int seed):this(seed,false){}
        public GleanersCellarBuilder(int seed, bool connected, string worldKey=null){this.seed=seed;this.connected=connected;this.worldKey=worldKey;}

        /// <summary>Read-only content admission; malformed runtime owners still fail staging.
        /// Both finite reward packages are required so content availability never rerolls a seed.</summary>
        public static bool SupportsContent(EntityFactory factory)
        {
            if(factory==null)return false;
            foreach(string name in Required)
                if(!factory.Blueprints.TryGetValue(name,out var bp)||bp==null||!bp.Parts.ContainsKey("Render")||!bp.Parts.ContainsKey("Physics"))return false;
            return factory.Blueprints["StairsUp"].Parts.ContainsKey("StairsUp")
                &&factory.Blueprints["Crate"].Parts.ContainsKey("Container")
                &&factory.Blueprints["FallenBeam"].Parts.ContainsKey("Handling")
                &&factory.Blueprints["MarlbackScrabbler"].Parts.ContainsKey("Brain")
                &&factory.Blueprints["MarlbackScrabbler"].Parts.ContainsKey("Loadout")
                &&factory.Blueprints["ShatteredRimeGrimoire"].Parts.ContainsKey("Grimoire")
                &&factory.Blueprints["ShatteredRimeGrimoire"].Parts.ContainsKey("GrimoireCharge")
                &&factory.Blueprints["Buckler"].Parts.ContainsKey("Equippable")
                &&factory.Blueprints["Buckler"].Parts.ContainsKey("Armor");
        }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||zone.ZoneID!=ZoneID||zone.EntityCount!=0||!SupportsContent(factory))return Reject(zone,"invalid-empty-zone-or-content");
            if(connected&&!factory.Blueprints.ContainsKey("SootrootCrop"))return Reject(zone,"missing-sootroot");
            var reservationBefore=new HashSet<(int x,int y)>(zone.GenReservedCells);
            var staged=new List<(Entity owner,int x,int y)>();var createdIds=new HashSet<string>(StringComparer.Ordinal);
            var createdOwners=new Dictionary<Entity,(string blueprint,string id)>();
            var published=new List<Entity>();bool complete=false;
            var oldFactory=LoadoutPart.Factory;var oldRng=LoadoutPart.Rng;
            try
            {
                // The guard retains its ordinary loadout and AI. Its private creation stream
                // must not consume the surface generator or another actor's loadout randomness.
                LoadoutPart.Factory=factory;LoadoutPart.Rng=new Random(unchecked(seed^0x63474c));
                bool north=(unchecked((uint)seed)&1u)==0;
                int sideY=north?5:19;
                int storeX=62+(int)(unchecked((uint)seed)%3u)*2;
                int guardX=54+(int)(unchecked((uint)seed)%3u);
                int MirrorY(int y)=>north?y:24-y;
                var open=new bool[Zone.Width,Zone.Height];
                void Room(int x0,int y0,int x1,int y1)
                {for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)open[x,MirrorY(y)]=true;}
                Room(38,10,43,14);                    // Clear landing, return and choice.
                Room(42,11,storeX,13);               // Short, exposed three-wide approach.
                Room(40,5,41,11);                    // Turn behind the direct corridor wall.
                Room(40,5,storeX,5);                 // Longer covered side passage.
                Room(storeX-2,5,storeX+2,14);         // Store has two approaches, no locked exit.
                Room(46,2,46,5);Room(46,2,50,2);Room(50,2,50,5); // Permanent beam detour.

                Entity Make(string blueprint)
                {
                    if(zone.EntityCount!=0||!reservationBefore.SetEquals(zone.GenReservedCells))throw new InvalidOperationException("changed-empty-source");
                    var e=factory.CreateEntity(blueprint);
                    if(!Fresh(e,blueprint)||!createdIds.Add(e.ID)||zone.EntityCount!=0||!reservationBefore.SetEquals(zone.GenReservedCells))
                        throw new InvalidOperationException("invalid-created-owner");
                    createdOwners.Add(e,(blueprint,e.ID));return e;
                }
                Entity Place(string blueprint,string role,int x,int y)
                {var e=Make(blueprint);e.Properties[RoleKey]=role;staged.Add((e,x,y));return e;}
                // Every cell has one actual wall or floor owner. Thick walls, rather than
                // diagonal collision assumptions, separate the side passage from the guard.
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    string bp=open[x,y]?"Floor":"StoneWall";var e=Make(bp);var physical=e.GetPart<PhysicsPart>();
                    if(physical.Solid==open[x,y]||physical.Takeable||(!open[x,y]&&!e.HasTag("Solid"))||(open[x,y]&&e.HasTag("Solid")))
                        throw new InvalidOperationException("invalid-terrain-collision");
                    staged.Add((e,x,y));
                }
                var stairs=Place("StairsUp","stairs",StairsX,StairsY);
                if(!stairs.HasPart<StairsUpPart>()||stairs.GetPart<PhysicsPart>().Solid)throw new InvalidOperationException("invalid-stairs");
                var notice=Place("Signpost","notice",39,11);
                Describe(notice,"gleaners' cellar tally","Fire clay was kept in the far supply crate for the well above. The broad passage is shortest, but fresh marlback scratches cross it. A narrow service passage turns "+(north?"north":"south")+" behind the wall. Its fallen beam can be hauled from the western shoulder; the winding passage beyond that shoulder stays open. The stair returns to the glade. These marks promise supplies, not safety.");
                var beam=Place("FallenBeam","beam",48,sideY);
                if(!beam.GetPart<PhysicsPart>().Solid||beam.GetPart<HandlingPart>()==null||beam.GetPart<PhysicsPart>().Takeable)
                    throw new InvalidOperationException("invalid-haulable-beam");
                Describe(beam,"fallen store-room beam","This loose roof timber blocks the short section of the service passage. Take hold from its western shoulder and pull it back toward the winding alcove, or follow that alcove around it. The stone walls interrupt the broad passage's sightline; moving enemies can still find their way here.");
                var guard=Place("MarlbackScrabbler","guard",guardX,12);
                if(!guard.HasTag("Creature")||guard.GetStatValue("Hitpoints")!=15||guard.GetPart<BrainPart>()?.SightRadius!=10)
                    throw new InvalidOperationException("invalid-native-guard");
                var cache=Place("Crate","supplies",storeX,MirrorY(7));
                var container=cache.GetPart<ContainerPart>();
                if(container==null||container.Contents.Count!=0||container.IsLocked||!cache.GetPart<PhysicsPart>().Solid)
                    throw new InvalidOperationException("invalid-supply-container");
                Describe(cache,"gleaners' supply crate","A shallow crate under an intact stretch of ceiling. Wrapped fire clay was put aside for the broken masonry well above; the remaining equipment belongs to the abandoned store. Take only what you can carry. Once emptied, the crate stays empty.");
                string reward=north?"ShatteredRimeGrimoire":"Buckler";
                // Two separate fresh clay owners are checked before native stacking, so a
                // malformed duplicate ID cannot disappear into a successful merge.
                var goods=new[]{Make("FireClay"),Make("FireClay"),Make(reward)};
                foreach(var item in goods)
                    if(!item.GetPart<PhysicsPart>().Takeable||item.HasTag("Creature")
                        ||item.GetPart<StackerPart>() is StackerPart stack&&stack.StackCount!=1)
                        throw new InvalidOperationException("invalid-portable-supply");
                if(north)
                {
                    var charge=goods[2].GetPart<GrimoireChargePart>();
                    if(goods[2].GetPart<GrimoirePart>()?.SkillClassName!="Rites_ShatteredRime"||charge?.Charges!=10||charge.MaxCharges!=10)
                        throw new InvalidOperationException("invalid-charged-discovery");
                }
                else if(goods[2].GetPart<EquippablePart>()==null||goods[2].GetPart<ArmorPart>()?.AV!=1)
                    throw new InvalidOperationException("invalid-shield-discovery");
                if(connected && !string.IsNullOrEmpty(worldKey)
                    && (!ConnectedSpreadProgress.BindClay(goods[0],worldKey,0)||!ConnectedSpreadProgress.BindClay(goods[1],worldKey,1)))
                    throw new InvalidOperationException("invalid-clay-origin");
                foreach(var item in goods)if(!container.AddItem(item))throw new InvalidOperationException("supply-container-refused");
                var roots=new List<Entity>();
                if(connected)
                {
                    for(int i=0;i<2;i++)
                    {
                        int x=storeX+1,y=MirrorY(9+i);
                        var root=Place("SootrootCrop",i==0?"sootroot-ripe":"sootroot-dry",x,y);
                        var crop=root.GetPart<CropPart>();
                        if(crop==null||!crop.HarvestAtMaturity||crop.YieldBlueprint!="SootrootPulp"||crop.YieldCount!=2||crop.SeedYieldBlueprint!="SootrootSeed"||crop.SeedYieldCount!=1)
                            throw new InvalidOperationException("invalid-sootroot");
                        crop.GrowthStage=i==0?2:0;crop.TicksInStage=0;crop.MoistureTicks=0;
                        root.GetPart<RenderPart>().RenderString=crop.GlyphForStage(crop.GrowthStage).ToString();
                        root.GetPart<RenderPart>().ColorString=crop.ColorForStage(crop.GrowthStage);
                        var ground=staged[y*Zone.Width+x].owner;
                        if(ground.BlueprintName!="Floor"||!ground.HasTag("Terrain"))throw new InvalidOperationException("invalid-growing-bed");
                        ground.SetTag("Plantable");ground.AddPart(new CultivatedSoilPart());
                        if(i==1 && !string.IsNullOrEmpty(worldKey) && !ConnectedSpreadProgress.BindDryCrop(root,worldKey))throw new InvalidOperationException("invalid-dry-crop-origin");
                        roots.Add(root);
                    }
                    Describe(notice,"gleaners' cellar tally",notice.GetPart<ExaminablePart>().Text+" Two sootroot beds survive behind the store: one ripe, one dry. Water the dry seedling; it will keep growing while you travel. Harvested pulp can smother a burn, or take two pulp and pitchpod resin to Ivrin's public ink desk at Marrowstye, southeast of the glade.");
                }
                // Creation hooks may modify an earlier staged owner. Verify the final
                // collision, useful supplies and fixed identities after the LAST hook,
                // rather than trusting the checks made when that owner was first created.
                bool TerrainMatches()
                {
                    for(int i=0;i<Zone.Width*Zone.Height;i++)
                    {
                        var placement=staged[i];var e=placement.owner;if(e.GetProperty(RoleKey)!=null)return false;
                        bool floor=open[placement.x,placement.y];var p=e.GetPart<PhysicsPart>();
                        if(e.BlueprintName!=(floor?"Floor":"StoneWall")||p==null||p.Solid==floor||p.Takeable
                            ||e.HasTag("Solid")==floor||floor&&!e.HasTag("Terrain"))return false;
                    }
                    return true;
                }
                bool RoleMatches(Entity e,string role,string blueprint)=>e.GetProperty(RoleKey)==role&&Fresh(e,blueprint);
                var finalHandling=beam.GetPart<HandlingPart>();var finalBrain=guard.GetPart<BrainPart>();
                if(roots.Where((e,i)=>!Fresh(e,"SootrootCrop")||e.GetPart<CropPart>()?.GrowthStage!=(i==0?2:0)||e.GetPart<CropPart>()?.MoistureTicks!=0).Any()
                    ||!TerrainMatches()||createdOwners.Any(pair=>pair.Key.ID!=pair.Value.id||pair.Key.BlueprintName!=pair.Value.blueprint)
                    ||!RoleMatches(stairs,"stairs","StairsUp")||!stairs.HasPart<StairsUpPart>()||stairs.GetPart<PhysicsPart>().Solid||stairs.HasTag("Solid")
                    ||!RoleMatches(notice,"notice","Signpost")||!notice.GetPart<PhysicsPart>().Solid||notice.GetPart<PhysicsPart>().Takeable
                    ||!RoleMatches(beam,"beam","FallenBeam")||!beam.GetPart<PhysicsPart>().Solid||beam.GetPart<PhysicsPart>().Takeable
                    ||finalHandling==null||finalHandling.Weight!=60||beam.GetPart<PhysicsPart>().Weight!=60||finalHandling.MinLiftStrength!=0
                    ||finalHandling.Carryable||finalHandling.Throwable
                    ||!RoleMatches(guard,"guard","MarlbackScrabbler")||guard.GetPart<PhysicsPart>().Takeable||!guard.HasTag("Creature")||guard.GetStatValue("Hitpoints")!=15||finalBrain?.SightRadius!=10
                    ||!RoleMatches(cache,"supplies","Crate")||cache.GetPart<PhysicsPart>().Takeable||cache.GetPart<ContainerPart>()!=container||container.IsLocked||!cache.GetPart<PhysicsPart>().Solid
                    ||container.Contents.Any(e=>e.BlueprintName!="FireClay"&&e.BlueprintName!=reward)
                    ||container.Contents.Where(e=>e.BlueprintName=="FireClay").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)!=2
                    ||container.Contents.Count(e=>e.BlueprintName==reward)!=1||!ValidGraph(staged.Select(p=>p.owner)))
                    throw new InvalidOperationException("invalid-staged-graph");
                if(zone.EntityCount!=0||!reservationBefore.SetEquals(zone.GenReservedCells))throw new InvalidOperationException("changed-source-before-publication");
                foreach(var placement in staged)
                {
                    if(!zone.AddEntity(placement.owner,placement.x,placement.y))throw new InvalidOperationException("placement-refused");
                    published.Add(placement.owner);
                }
                // Reservations protect just the clear landing. No code reads or rewrites
                // connections, managers, another depth, tile coating or cached graphs here.
                for(int y=11;y<=13;y++)for(int x=39;x<=42;x++)if(!zone.GetCell(x,y).BlocksMovement())zone.GenReservedCells.Add((x,y));
                complete=true;
                Diag.Record("worldgen","GleanersCellarBuilt",payload:new{zoneId=zone.ZoneID,seed,side=north?"north":"south",reward,clay=2,storeX,guardX});
                return true;
            }
            catch(Exception error){return Reject(zone,"staging-or-publication-"+error.Message);}
            finally
            {
                LoadoutPart.Factory=oldFactory;LoadoutPart.Rng=oldRng;
                if(!complete)
                    foreach(var e in published.AsEnumerable().Reverse())if(e.SpatialZone==zone)zone.RemoveEntity(e);
            }
        }
        static bool Fresh(Entity e,string blueprint)=>e!=null&&e.BlueprintName==blueprint&&!string.IsNullOrEmpty(e.ID)&&e.SpatialZone==null
            &&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==e&&p.InInventory==null&&p.Equipped==null
            &&e.GetPart<RenderPart>()?.Visible==true&&!e.HasPart<SpatialFootprintPart>()
            &&e.Parts.All(part=>part!=null&&ReferenceEquals(part.ParentEntity,e))&&e.Statistics.Values.All(stat=>ReferenceEquals(stat.Owner,e));
        static void Describe(Entity e,string name,string text)
        {
            e.GetPart<RenderPart>().DisplayName=name;
            var examine=e.GetPart<ExaminablePart>();if(examine==null){examine=new ExaminablePart();e.AddPart(examine);}examine.Text=text;
        }
        static bool ValidGraph(IEnumerable<Entity> roots)
        {
            var seen=new HashSet<Entity>();var ids=new HashSet<string>(StringComparer.Ordinal);
            var queue=new Queue<(Entity item,Entity parent,bool equipped)>(roots.Select(e=>(e,(Entity)null,false)));
            while(queue.Count>0)
            {
                var (e,parent,equipped)=queue.Dequeue();var p=e?.GetPart<PhysicsPart>();
                if(e==null||!seen.Add(e)||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID)||e.SpatialZone!=null||p==null
                    ||e.HasPart<SpatialFootprintPart>()||e.Parts.Any(part=>part==null||part.ParentEntity!=e))return false;
                if(parent==null){if(p.InInventory!=null||p.Equipped!=null)return false;}
                else if(equipped){if(p.Equipped!=parent||(p.InInventory!=null&&p.InInventory!=parent))return false;}
                else if(p.InInventory!=parent||p.Equipped!=null)return false;
                var inventory=e.GetPart<InventoryPart>();
                var worn=(inventory?.EquippedItems.Values??Enumerable.Empty<Entity>())
                    .Concat(e.GetPart<Body>()?.GetParts().Where(b=>b.Equipped!=null).Select(b=>b.Equipped)??Enumerable.Empty<Entity>()).Distinct().ToArray();
                foreach(var item in e.GetPart<ContainerPart>()?.Contents??Enumerable.Empty<Entity>())queue.Enqueue((item,e,false));
                foreach(var item in inventory?.Objects??Enumerable.Empty<Entity>())queue.Enqueue((item,e,false));
                foreach(var item in worn)queue.Enqueue((item,e,true));
            }
            return true;
        }
        static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","GleanersCellarRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
