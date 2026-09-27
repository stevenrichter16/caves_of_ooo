using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _biomeOnly;
        private readonly List<string> _biomeSteps = new List<string>();
        private int _biomeShortcuts;
        private Guid _biomeOriginalCalm;
        private int _biomeDefensiveCalmUses;
        private const int MaxBiomeDefensiveCalmUses = 8;
        private static readonly string[] BiomeRequired = {
            "biome_initial_profile", "original_dagger_ground_owner", "original_dagger_picked_up", "biome_native_harvest_paid_and_finite",
            "biome_checkpoint_saved", "biome_checkpoint_mutated", "biome_checkpoint_restored",
            "biome_keyboard_east_exit", "biome_neighbor_profile", "biome_keyboard_return",
            "biome_generated_village", "biome_village_profile", "biome_door_close_paid", "biome_door_open_paid",
            "biome_generated_lair", "biome_keyboard_descends", "biome_lair_profile", "biome_keyboard_ascends",
            "biome_foreign_profile_off", "biome_original_glade_return", "biome_ordinary_finish",
            "biome_sixty_second_movement" };
        private bool BiomeComplete => _audit.Count == BiomeRequired.Length + 1
            && BiomeRequired.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _screenshots.Count >= 10 && _biomeShortcuts == 4;
        private OverworldZoneManager BiomeManager => _input.ZoneManager as OverworldZoneManager;
        private IEnumerator RunBiomeAudit()
        {
            Require(BiomeManager != null && BiomeManager.WorldSeed == 64,"existing isolated seed64 cohort; no post-bootstrap reseed");
            var actor = _input.PlayerEntity;
            Require(actor.GetStatValue("Hitpoints") == 40 && actor.GetStatValue("Strength") == 18
                && actor.GetStatValue("Agility") == 18 && actor.GetStatValue("Toughness") == 18
                && TradeSystem.GetDrams(actor) == 50 && !actor.HasTag("Invulnerable"),"ordinary unmodified starter");
            _biomeOriginalCalm = actor.GetPart<ActivatedAbilitiesPart>()?.AbilityList
                .SingleOrDefault(a => a.Command == "CommandCalm")?.ID ?? Guid.Empty;
            Require(_biomeOriginalCalm != Guid.Empty,"actual original starting Calm exists before any route action");
            string initialItems = BiomeItemCollection(actor), actorID = actor.ID;
            Check("biome_initial_profile", BiomeProfile(true));
            var dagger = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Dagger");
            string daggerID = dagger.ID;
            Require(dagger.GetPart<PhysicsPart>().Equipped == null && InventorySystem.GetTakeableItemsAtFeet(actor,_input.CurrentZone).Count == 0,"unoccupied item-drop site and original carried dagger");
            var at = Cell(); int x = at.X, y = at.Y;
            yield return ItemAction(dagger,"drop"); yield return BiomeCloseMenus();
            Check("original_dagger_ground_owner", _input.CurrentZone.GetEntityCell(dagger) == at
                && dagger.GetPart<PhysicsPart>().InInventory == null && dagger.GetPart<PhysicsPart>().Equipped == null
                && !actor.GetPart<InventoryPart>().Objects.Contains(dagger) && BiomePortable(dagger));
            yield return Capture("biome-02-original-dagger-ground");
            Require(InventorySystem.GetTakeableItemsAtFeet(actor,_input.CurrentZone).SequenceEqual(new[]{dagger}),"G can only acquire the actual dropped owner");
            yield return Tap(Key.G);
            BiomeCase("original_dagger_picked_up",
                ("normal", State()=="Normal"), ("sameOriginalCollection", BiomeItemCollection(actor)==initialItems),
                ("exactCarriedOrBodyOwner", BiomeOwnsItem(actor,dagger)),
                ("absentFromGround", _input.CurrentZone.GetEntityCell(dagger)==null));
            yield return WalkTo(79,12); yield return Tap(Key.D);
            Check("biome_keyboard_east_exit",_input.CurrentZone.ZoneID=="Overworld.12.10.0" && actor.ID==actorID);
            Check("biome_neighbor_profile",BiomeProfile(true));yield return Capture("biome-03-real-east-neighbor");
            yield return BiomeHarvestNeighbor();
            string expectedGear = CombatGear(_input.PlayerEntity);
            yield return BiomeCheckpoint(expectedGear, daggerID);
            actor = _input.PlayerEntity;
            yield return BiomeReturnWest();
            Check("biome_keyboard_return",_input.CurrentZone.ZoneID==ReferenceGladePlan.ZoneID && BiomeProfile(true));
            yield return Capture("biome-04-real-west-return");
            var glade = _input.CurrentZone; var returnCell = Cell();
            Entity door; Cell approach; var village = BiomeVillage(out door,out approach);
            Require(village!=null && door!=null,"bounded actual Spread VillageDoor source; no substitute source is manufactured");
            string doorID=door.ID;yield return BiomeTravel(village,approach,"actual generated Spread village door approach");
            Check("biome_generated_village",BiomeManager.WorldMap.GetPOI(WorldMap.FromZoneID(village.ZoneID).x,WorldMap.FromZoneID(village.ZoneID).y)?.Type==POIType.Village
                && door.BlueprintName=="VillageDoor" && village.GetEntityCell(door)!=null && !door.GetPart<DoorPart>().IsClosed);
            Check("biome_village_profile",BiomeProfile(true) && BiomeDrawn(door));yield return Capture("biome-05-generated-village-open-door");
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            yield return BiomeWorldAction(door,DoorPart.CloseCommand);
            Check("biome_door_close_paid",door.ID==doorID && door.GetPart<DoorPart>().IsClosed && Cell()==approach && BiomeOneAction(tick,energy) && BiomeDoorModel(door,true));
            yield return Capture("biome-06-native-closed-door");
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);
            yield return BiomeWorldAction(door,DoorPart.OpenCommand);
            Check("biome_door_open_paid",!door.GetPart<DoorPart>().IsClosed && Cell()==approach && BiomeOneAction(tick,energy) && BiomeDoorModel(door,false));
            Entity down; Zone floor; var surface=BiomeLair(out down,out floor);
            Require(surface!=null && down!=null && floor!=null,"actual Spread lair with clear paired stair footprints; ordinary lair danger remains active");
            var plan=LairStacks.Inspect(BiomeManager,surface.ZoneID);string surfaceID=surface.ZoneID, floorID=floor.ZoneID;
            yield return BiomeTravel(surface,surface.GetEntityCell(down),"actual generated Spread lair stair approach; first floor generation is a labelled source preflight");
            Check("biome_generated_lair",plan!=null && !plan.Legacy && plan.Biome==BiomeType.Spread && (plan.GeneratedMask&3)==3
                && LairStacks.IsCommittedFloor(BiomeManager,floor,BiomeType.Spread));
            yield return Capture("biome-07-real-lair-surface");
            yield return Tap(Key.LeftShift,Key.Period);
            Check("biome_keyboard_descends",State()=="Normal" && _input.CurrentZone.ZoneID==floorID
                && _input.CurrentZone.GetEntityCell(actor)!=null && LairStacks.IsCommittedFloor(BiomeManager,_input.CurrentZone,BiomeType.Spread));
            BiomeLairRisk(_input.CurrentZone,Cell(),"after-native-descent");
            yield return BiomeCalmPursuingThreat();
            Check("biome_lair_profile",BiomeProfile(true));yield return Capture("biome-08-committed-lair-floor");
            var up=_input.CurrentZone.GetReadOnlyEntities().Single(e=>e.HasPart<StairsUpPart>());
            Require(_input.CurrentZone.GetOccupiedCells(up).Contains(Cell()),"real arrival on existing paired up stair");
            yield return Tap(Key.LeftShift,Key.Comma);
            Check("biome_keyboard_ascends",_input.CurrentZone.ZoneID==surfaceID && _input.CurrentZone.GetEntityCell(down)!=null);
            Zone foreign;Cell foreignCell;BiomeForeign(out foreign,out foreignCell);
            Require(foreign!=null,"bounded actual foreign rendered zone source");yield return BiomeTravel(foreign,foreignCell,"foreign-biome negative profile control");
            Check("biome_foreign_profile_off",BiomeProfile(false));yield return Capture("biome-09-foreign-profile-control");
            Require(BiomeSafe(glade,returnCell,2),"original return position still physically safe; no world edits");yield return BiomeTravel(glade,returnCell,"return to original real glade owner graph");
            Check("biome_original_glade_return",ReferenceEquals(glade,_input.CurrentZone)&&_input.PlayerEntity.ID==actorID&&BiomeProfile(true));
            yield return WalkTo(40,12);
            BeginNativeMarkerProfile();double began=Time.realtimeSinceStartupAsDouble;_profiling=true;
            while(Time.realtimeSinceStartupAsDouble-began<60)
            {
                int px=Cell().X,py=Cell().Y;Require(BiomeSafe(_input.CurrentZone,_input.CurrentZone.GetCell(px+1,py),2),"profile's actual east step remains clear");
                yield return Tap(Key.D);Require(Cell().X==px+1&&Cell().Y==py,"real profile east step");
                yield return Tap(Key.A);Require(Cell().X==px&&Cell().Y==py,"real profile west step");
            }
            _profileSeconds=Time.realtimeSinceStartupAsDouble-began;_profiling=false;EndNativeMarkerProfile();
            Check("biome_sixty_second_movement",_profileSeconds>=60&&_frameTimes.Count>100);
            Check("biome_ordinary_finish",State()=="Normal"&&actor.ID==actorID&&!CombatSystem.IsDeathHandled(actor)
                &&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled
                &&!actor.HasPart<BitLockerPart>()&&CombatGear(actor)==expectedGear&&TradeSystem.GetDrams(actor)==50);
            yield return Capture("biome-10-finite-movement-finish");
        }
        private string _biomeHarvestedID;
        private IEnumerator BiomeHarvestNeighbor()
        {
            var zone=_input.CurrentZone;var actor=_input.PlayerEntity;
            Entity source=null;Cell approach=null;
            // A living creature can block an approach after any paid step. Choose
            // the shortest currently safe route afresh; never move a world owner
            // or retain a stale target merely because it was reachable earlier.
            for(int step=0;step<160;step++)
            {
                Require(State()=="Normal"&&!CombatSystem.IsDeathHandled(actor),"live ordinary actor while approaching harvest");
                int defensiveCastsBefore=_biomeDefensiveCalmUses;
                yield return BiomeCalmPursuingThreat();
                if(_biomeDefensiveCalmUses>defensiveCastsBefore)continue;
                source=null;approach=null;List<(int dx,int dy)> best=null;
                foreach(var candidate in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bones"||e.BlueprintName=="BerryBush")
                    .OrderBy(e=>SpatialQuery.Distance(zone,actor,e)).ThenBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var harvest=candidate.GetPart<HarvestablePart>();if(harvest==null||harvest.Harvested||harvest.YieldChance!=100)continue;
                    var at=zone.GetEntityCell(candidate);if(at==null)continue;
                    foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                    {
                        var near=zone.GetCell(at.X+d.Item1,at.Y+d.Item2);if(!BiomeSafe(zone,near,2))continue;
                        if(best!=null&&Math.Max(Math.Abs(near.X-Cell().X),Math.Abs(near.Y-Cell().Y))>=best.Count)continue;
                        var path=BiomeSafePath(zone,Cell(),near);
                        if(path==null||(best!=null&&path.Count>=best.Count))continue;
                        source=candidate;approach=near;best=path;
                    }
                }
                if(source==null)BiomeRouteDiagnostic(zone);
                Require(source!=null,"real visited neighbor contains a currently reachable finite Bones/BerryBush source; no staged replacement");
                _biomeSteps.Add("adaptive-harvest "+zone.ZoneID+"@"+Cell().X+","+Cell().Y+" source="+source.ID+":"+source.BlueprintName+" approach="+approach.X+","+approach.Y+" remaining="+best.Count);
                if(best.Count==0)break;
                var from=Cell();var delta=best[0];
                Require(BiomeSafe(zone,zone.GetCell(from.X+delta.dx,from.Y+delta.dy),2),"next native harvest footprint remains safe");
                yield return Tap(Direction(delta.dx,delta.dy));
                Require(Cell().X==from.X+delta.dx&&Cell().Y==from.Y+delta.dy,"actual native harvest step reaches selected safe footprint");
            }
            Require(source!=null&&ReferenceEquals(Cell(),approach),"finite adaptive harvest route reaches a real source");
            Require(BiomeSafe(zone,Cell(),2),"real harvest approach still clear");
            var part=source.GetPart<HarvestablePart>();string product=part.YieldBlueprint;int min=part.YieldMin,max=part.YieldMax;
            var inventory=actor.GetPart<InventoryPart>();var before=inventory.Objects.ToArray();
            Require(!before.Any(e=>e.BlueprintName==product),"harvest yield absent before native acquisition");
            int unitsBefore=BiomeProductUnits(zone,product),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            _biomeHarvestedID=source.ID;
            _biomeSteps.Add("HARVEST exact generated "+zone.ZoneID+":"+source.ID+":"+source.BlueprintName+" yields "+product+" range="+min+".."+max+" chance=100; no roll override");
            yield return BiomeWorldAction(source,"Harvest");
            int gained=BiomeProductUnits(zone,product)-unitsBefore;
            Check("biome_native_harvest_paid_and_finite",part.Harvested&&zone.GetEntityCell(source)==null&&BiomeOneAction(tick,energy)
                &&gained>=min&&gained<=max&&before.All(e=>inventory.Objects.Contains(e))
                &&inventory.Objects.Except(before).All(e=>e.BlueprintName==product)
                &&inventory.Objects.Where(e=>e.BlueprintName==product).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==gained);
            yield return Capture("biome-03b-native-finite-harvest");
        }
        private int BiomeProductUnits(Zone zone,string blueprint)=>zone.GetReadOnlyEntities().Concat(_input.PlayerEntity.GetPart<InventoryPart>().Objects)
            .Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private IEnumerator BiomeCheckpoint(string gear,string daggerID)
        {
            var actor=_input.PlayerEntity;var at=Cell();string zone=_input.CurrentZone.ZoneID;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"existing N checkpoint");
            string path=System.IO.Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),oldHash=CheckpointHash(path),id=actor.ID;
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor),hp=actor.GetStatValue("Hitpoints");
            long serial=MessageLog.NextSerialValue;yield return Tap(Key.F5);string hash=CheckpointHash(path);
            Check("biome_checkpoint_saved",hash!=oldHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."
                &&SaveGameService.GetSaveInfo("Quick")?.GameID==info.GameID&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==zone);
            var delta=new[]{(1,0),(-1,0),(0,1),(0,-1)}.FirstOrDefault(d=>BiomeSafe(_input.CurrentZone,_input.CurrentZone.GetCell(at.X+d.Item1,at.Y+d.Item2),2));
            Require(delta!=(0,0),"genuine safe post-checkpoint keyboard step");
            yield return Tap(Direction(delta.Item1,delta.Item2));Check("biome_checkpoint_mutated",Cell().X==at.X+delta.Item1&&Cell().Y==at.Y+delta.Item2&&CheckpointHash(path)==hash);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(actor,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native F6 graph replacement");yield return null;}
            BiomeCase("biome_checkpoint_restored", ("actorId",_input.PlayerEntity.ID==id),
                ("zone",_input.CurrentZone.ZoneID==zone), ("position",Cell().X==at.X&&Cell().Y==at.Y),
                ("hp",_input.PlayerEntity.GetStatValue("Hitpoints")==hp), ("tick",_input.TurnManager.TickCount==tick),
                ("energy",_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy), ("exactSavedGear",CombatGear(_input.PlayerEntity)==gear),
                ("originalDaggerOwnership",BiomeHasOwnedItemId(_input.PlayerEntity,daggerID)),
                ("groundAbsence",!_input.CurrentZone.GetReadOnlyEntities().Any(e=>e.ID==daggerID||e.ID==_biomeHarvestedID)),
                ("unchangedSaveBytes",CheckpointHash(path)==hash), ("profile",BiomeProfile(true)));
            yield return Capture("biome-02b-restored-original-gear");
        }
        private static IEnumerable<Entity> BiomeOwnedItems(Entity actor)
        {
            var inventory=actor.GetPart<InventoryPart>();
            return inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct();
        }
        private static string BiomeItemCollection(Entity actor)=>string.Join("|",BiomeOwnedItems(actor)
            .OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
        private static bool BiomeOwnsItem(Entity actor,Entity item)
        {
            if(actor==null||item==null)return false;
            var inventory=actor.GetPart<InventoryPart>();var physics=item.GetPart<PhysicsPart>();
            if(inventory==null||physics==null||!ReferenceEquals(physics.ParentEntity,item))return false;
            int carried=inventory.Objects.Count(e=>ReferenceEquals(e,item));
            bool equipped=inventory.EquippedItems.Values.Any(e=>ReferenceEquals(e,item));
            bool body=actor.GetPart<Body>()?.GetParts().Any(p=>ReferenceEquals(p.Equipped,item))==true;
            return (carried==1&&!equipped&&!body&&ReferenceEquals(physics.InInventory,actor)&&physics.Equipped==null)
                ||(carried==0&&equipped&&body&&physics.InInventory==null&&ReferenceEquals(physics.Equipped,actor));
        }
        private static bool BiomeHasOwnedItemId(Entity actor,string id)
        {
            var matches=BiomeOwnedItems(actor).Where(e=>e.ID==id).ToArray();
            return matches.Length==1&&BiomeOwnsItem(actor,matches[0]);
        }
        private void BiomeCase(string name,params (string name,bool passed)[] assertions)
        {
            _biomeSteps.Add("CHECK "+name+" "+string.Join(";",assertions.Select(a=>a.name+"="+a.passed))
                +"; actualGear="+CombatGear(_input.PlayerEntity));
            Check(name,assertions.All(a=>a.passed));
        }
        // Recompute from the real current owner graph before every keyboard step.
        // Eight-way BFS keeps the existing full-footprint/hazard/threat policy;
        // it neither relaxes that policy nor carves or moves any native owner.
        private List<(int dx,int dy)> BiomeSafePath(Zone zone,Cell from,Cell target)
        {
            if(zone==null||from==null||target==null)return null;
            int width=Zone.Width,total=Zone.Width*Zone.Height,start=from.Y*width+from.X,end=target.Y*width+target.X;
            var previous=Enumerable.Repeat(-2,total).ToArray();var queue=new Queue<int>();
            // No yield occurs during this search; one actual threat snapshot is valid for the whole BFS.
            var threats=BiomeThreats(zone);
            previous[start]=-1;queue.Enqueue(start);
            var directions=new[]{(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
            while(queue.Count>0)
            {
                int current=queue.Dequeue();if(current==end)break;
                int cx=current%width,cy=current/width;
                foreach(var delta in directions)
                {
                    int x=cx+delta.Item1,y=cy+delta.Item2;
                    if(x<0||x>=width||y<0||y>=Zone.Height)continue;
                    int next=y*width+x;if(previous[next]!=-2)continue;
                    // Match native FindPath's corner rule. A blocked edge does
                    // not mark this destination visited: another edge may reach it.
                    if(delta.Item1!=0&&delta.Item2!=0)
                    {
                        var actor=_input.PlayerEntity;
                        bool wide=actor.HasPart<SpatialFootprintPart>();
                        bool blockedX=wide?!zone.CanPlaceFootprint(actor,x,cy):zone.GetCell(x,cy).IsSolid();
                        bool blockedY=wide?!zone.CanPlaceFootprint(actor,cx,y):zone.GetCell(cx,y).IsSolid();
                        if(blockedX&&blockedY)continue;
                    }
                    if(!BiomeSafe(zone,zone.GetCell(x,y),2,threats)){previous[next]=-3;continue;}
                    previous[next]=current;queue.Enqueue(next);
                }
            }
            if(previous[end]<-1)return null;
            var result=new List<(int dx,int dy)>();
            for(int at=end;previous[at]>=0;at=previous[at])
            {int before=previous[at];result.Add((at%width-before%width,at/width-before/width));}
            result.Reverse();return result;
        }
        private void BiomeRouteDiagnostic(Zone zone,Cell goal=null)
        {
            var current=Cell();
            if(zone==null||current==null){_biomeSteps.Add("ROUTE DIAGNOSTIC missing current zone/actor anchor");WriteReport();return;}
            var threats=BiomeThreats(zone);
            _biomeSteps.Add("ROUTE DIAGNOSTIC zone="+zone.ZoneID+" current="+current.X+","+current.Y+" goal="+(goal==null?"source-selection":goal.X+","+goal.Y)+" auditPolicy=full-footprint,no-items-or-hazards,threat-distance>2,FindPath-corners");
            if(goal!=null)
            {
                BiomeCellDiagnostic(zone,goal,threats,"goal");
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)})BiomeCellDiagnostic(zone,zone.GetCell(goal.X+d.Item1,goal.Y+d.Item2),threats,"near-goal");
            }
            _biomeSteps.Add("ROUTE DIAGNOSTIC actual threats="+string.Join("|",threats.Select(e=>e.ID+":"+e.BlueprintName+"@"+zone.GetEntityPosition(e)+" distance="+SpatialQuery.Distance(zone,_input.PlayerEntity,e))));
            var targets=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bones"||e.BlueprintName=="BerryBush").ToArray();
            foreach(var target in targets)
            {
                var at=zone.GetEntityCell(target);var part=target.GetPart<HarvestablePart>();
                _biomeSteps.Add("SOURCE "+target.ID+":"+target.BlueprintName+"@"+at.X+","+at.Y+" harvested="+part?.Harvested+" chance="+part?.YieldChance);
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)})BiomeCellDiagnostic(zone,zone.GetCell(at.X+d.Item1,at.Y+d.Item2),threats,"approach");
            }
            for(int y=current.Y-1;y<=current.Y+1;y++)for(int x=current.X-1;x<=current.X+1;x++)BiomeCellDiagnostic(zone,zone.GetCell(x,y),threats,"near-player");
            WriteReport();
        }
        private void BiomeCellDiagnostic(Zone zone,Cell cell,Entity[] threats,string label)
        {
            if(cell==null){_biomeSteps.Add(label+": outside-zone");return;}
            var state=zone.TileState.Get(cell.X,cell.Y);
            _biomeSteps.Add(label+"@"+cell.X+","+cell.Y+" safe="+BiomeSafe(zone,cell,2,threats)+" footprint="+zone.CanPlaceFootprint(_input.PlayerEntity,cell.X,cell.Y)
                +" state="+(state==null?"none":"heat="+state.Heat+",cold="+state.Cold+",charge="+state.Charge+",cloud="+state.Cloud+",coatings="+state.Coatings.Count)
                +" owners="+string.Join("|",cell.Occupants.Select(e=>e.ID+":"+e.BlueprintName+":creature="+e.HasTag("Creature")+":takeable="+e.GetPart<PhysicsPart>()?.Takeable+":trigger="+e.HasPart<TriggerOnStepPart>()+":liquid="+e.HasPart<LiquidPoolPart>()+":gas="+e.HasPart<GasPoolPart>()+":burning="+(e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))
                +" threats="+string.Join("|",threats.Where(e=>SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=2).Select(e=>e.ID+":"+e.BlueprintName)));
        }
        private bool BiomeProfile(bool expected)
        {
            var view=_input.ZoneRenderer?.SpawnRing3D;var zone=_input.CurrentZone;
            bool active=SpreadPresentationScope.IsActive(zone);
            Require(view!=null&&ReferenceEquals(view.CurrentZone,zone)&&view.IsReady&&view.PresentationVisible&&!view.FullReveal,"live current-zone presenter for profile observation");
            GameObject playerRoot;string model;Require(view.TryGetEntityView(_input.PlayerEntity,out playerRoot,out model)&&BiomeDrawn(_input.PlayerEntity),"actual player body submitted");
            var approved=ReferenceGladeVoxelLibrary.Load().ActorPaints.Single(p=>p.ModelId=="ring-player").Painted;
            bool painted=playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.sharedMesh==approved);
            float exposure=view.ActiveSurface.MaterialFor(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial).GetFloat("_Exposure");
            bool bound=(bool)Field(view,"boundSpreadStyle");
            _biomeSteps.Add("profile zone="+zone.ZoneID+" expected="+expected+" authority="+active+" bound="+bound+" approvedMesh="+painted+" exposure="+exposure+" player="+_input.PlayerEntity.ID);WriteReport();
            return active==expected&&bound==expected&&painted==expected&&(expected?exposure>=1.1f&&exposure<=1.8f:Mathf.Approximately(exposure,2.2f));
        }
        private bool BiomeDrawn(Entity owner)
        {
            var view=_input.ZoneRenderer?.SpawnRing3D;GameObject root;string model;
            bool drawn=view!=null&&ReferenceEquals(view.CurrentZone,_input.CurrentZone)&&view.TryGetEntityView(owner,out root,out model)
                &&BiomeSubmitted(root)
                &&view.IsRenderedEntity(owner);
            _biomeSteps.Add("drawn owner="+owner.ID+" blueprint="+owner.BlueprintName+" current="+(_input.CurrentZone.GetEntityCell(owner)!=null)+" drawn="+drawn);return drawn;
        }
        private bool BiomePortable(Entity owner)
        {
            var library=SpreadPortable3DLibrary.Load();string expected=null;
            Require(library!=null&&SpreadPortable3DLibrary.TryRecipe(owner,out expected),"actual portable registry recipe");
            var entry=library.Find(expected);var presenter=_input.ZoneRenderer.SpawnRing3D;GameObject root;string actual;
            bool found=entry!=null&&presenter.TryGetEntityView(owner,out root,out actual);
            if(!found){_biomeSteps.Add("portable owner="+owner.ID+" expected="+expected+" no current model");return false;}
            presenter.TryGetEntityView(owner,out root,out actual);
            var material=presenter.ActiveSurface.MaterialFor(library.Material);
            bool adopted=actual==expected&&BiomeDrawn(owner)&&root.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==entry.Mesh
                &&f.GetComponent<Renderer>() is Renderer renderer&&renderer.enabled&&!renderer.forceRenderingOff&&renderer.gameObject.activeInHierarchy
                &&renderer.sharedMaterials.Length==1&&renderer.sharedMaterial==material&&material.GetTexture("_BaseMap")==library.Palette);
            _biomeSteps.Add("portable owner="+owner.ID+" expected="+expected+" actual="+actual+" exactMeshAndOwnedPalette="+adopted);return adopted;
        }
        private static bool BiomeSubmitted(GameObject root)=>root!=null&&root.activeInHierarchy
            &&root.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy
                &&r.sharedMaterials.Length>0&&r.sharedMaterials.All(m=>m!=null)
                &&((r is SkinnedMeshRenderer skin&&skin.sharedMesh!=null&&skin.sharedMesh.vertexCount>0)
                    ||(r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount??0)>0));
        private bool BiomeDoorModel(Entity door,bool closed)
        {GameObject root;string model;var view=_input.ZoneRenderer.SpawnRing3D;return BiomeDrawn(door)&&view.TryGetEntityView(door,out root,out model)&&model.StartsWith(closed?"stillleaf-door-":"stillleaf-open-door-",StringComparison.Ordinal);}
        private bool BiomeOneAction(int tick,int energy)=>_input.TurnManager.TickCount>tick
            &&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy-TurnManager.ActionThreshold+(_input.TurnManager.TickCount-tick)*_input.PlayerEntity.GetStatValue("Speed",TurnManager.DefaultSpeed);
        private Entity[] BiomeThreats(Zone zone)
        {
            var actor=_input.PlayerEntity;
            return zone.GetReadOnlyEntities().Where(e=>e!=actor&&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)
                &&(e.GetStat("Hitpoints")==null||e.GetStatValue("Hitpoints")>0)
                &&!ReferenceGladeRouteControl.HasLiveCalm(zone,e)
                &&(FactionManager.IsHostile(e,actor)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor)==true||e.GetPart<BrainPart>()?.Target==actor)).ToArray();
        }
        private IEnumerator BiomeCalmPursuingThreat()
        {
            // Use the original ready defensive spell while an actual pursuing
            // owner is still on a legal visible ray, before movement lets it
            // trap the unchanged safe component. No cooldown waits/world edits.
            var zone=_input.CurrentZone;var actor=_input.PlayerEntity;var origin=Cell();
            if(_biomeDefensiveCalmUses>=MaxBiomeDefensiveCalmUses)yield break;
            foreach(var target in BiomeThreats(zone).Where(e=>ReferenceEquals(e.GetPart<BrainPart>()?.Target,actor)
                &&SpatialQuery.Distance(zone,actor,e)<=CavesOfOoo.Skills.Spellcraft_Calm.RANGE)
                .OrderBy(e=>SpatialQuery.Distance(zone,actor,e)).ThenBy(e=>e.ID,StringComparer.Ordinal))
            {
                if(!ReferenceGladeRouteControl.TryCalm(zone,actor,target,_biomeOriginalCalm,out int slot,out int dx,out int dy))continue;
                var ability=actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(slot);
                int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
                string gear=CombatGear(actor),targetID=target.ID;
                _biomeSteps.Add("DEFENSIVE CALM begin exact owner="+targetID+"@"+zone.GetEntityCell(target).X+","+zone.GetEntityCell(target).Y
                    +" originalAbility="+_biomeOriginalCalm+" slot="+slot+" from="+origin.X+","+origin.Y+" tick="+tick+" energy="+energy);
                try
                {
                    yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));
                    Require(State()=="AwaitingDirection"&&_input.TurnManager.TickCount==tick
                        &&_input.TurnManager.GetEnergy(actor)==energy,"native original Calm asks for direction before payment");
                    Require(ReferenceEquals(_input.CurrentZone,zone)&&ReferenceEquals(_input.PlayerEntity,actor)&&ReferenceEquals(Cell(),origin)
                        &&ReferenceGladeRouteControl.TryCalm(zone,actor,target,_biomeOriginalCalm,out int freshSlot,out int freshDx,out int freshDy)
                        &&freshSlot==slot&&freshDx==dx&&freshDy==dy,"exact live original target and first impact still match before direction");
                    yield return Tap(Direction(dx,dy));
                    double began=Time.realtimeSinceStartupAsDouble;
                    while(State()=="WaitingForFxResolution"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true)
                    {Require(Time.realtimeSinceStartupAsDouble-began<8&&!CombatSystem.IsDeathHandled(actor),"finite native Calm FX resolution");yield return null;}
                    Require(State()=="Normal"&&ReferenceEquals(_input.CurrentZone,zone)&&ReferenceEquals(_input.PlayerEntity,actor)
                        &&ReferenceEquals(Cell(),origin)&&BiomeOneAction(tick,energy)&&CombatGear(actor)==gear
                        &&ReferenceEquals(actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(slot),ability)
                        &&ability.ID==_biomeOriginalCalm&&ability.CooldownRemaining>0
                        &&target.ID==targetID&&ReferenceGladeRouteControl.HasLiveCalm(zone,target),
                        "native original Calm paid one stationary action and pacified the exact current owner without changing gear");
                    _biomeDefensiveCalmUses++;
                }
                finally
                {
                    var goal=target.GetPart<BrainPart>()?.PeekGoal() as NoFightGoal;
                    _biomeSteps.Add("DEFENSIVE CALM observed owner="+targetID+" live="+(zone.GetEntityCell(target)!=null)
                        +" goal="+(goal==null?"none":goal.Age+"/"+goal.Duration+" wander="+goal.Wander+" finished="+goal.Finished())
                        +" cooldown="+ability.CooldownRemaining+" provenUses="+_biomeDefensiveCalmUses+" state="+BiomeState());
                    WriteReport();
                }
                yield break;
            }
            _biomeSteps.Add("DEFENSIVE CALM unavailable: no ready original ability with a visible exact first-impact pursuing threat within native Calm range; ordinary action/failure retained");
        }
        private bool BiomeSafe(Zone zone,Cell cell,int clearance)=>BiomeSafe(zone,cell,clearance,BiomeThreats(zone));
        private bool BiomeSafe(Zone zone,Cell cell,int clearance,Entity[] threats)
        {
            var actor=_input.PlayerEntity;
            if(cell==null||!zone.CanPlaceFootprint(actor,cell.X,cell.Y))return false;
            foreach(var occupied in zone.GetOccupiedCells(actor,cell.X,cell.Y))
            {
                if(occupied==null||occupied.Occupants.Any(e=>e!=actor&&(e.HasTag("Creature")||e.GetPart<PhysicsPart>()?.Takeable==true
                    ||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()
                    ||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var state=zone.TileState.Get(occupied.X,occupied.Y);
                if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)||state.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,occupied.X,occupied.Y)<=clearance))return false;
            }
            return true;
        }
        private Zone BiomeVillage(out Entity selected,out Cell approach)
        {
            selected=null;approach=null;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                if(BiomeManager.WorldMap.GetBiome(x,y)!=BiomeType.Spread||BiomeManager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village)continue;
                string id=WorldMap.ToZoneID(x,y,0);var zone=BiomeManager.GetZone(id);if(zone==null)continue;
                _biomeSteps.Add("village-source inspected "+id);
                foreach(var door in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="VillageDoor").OrderBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var part=door.GetPart<DoorPart>();if(part==null||part.IsClosed||!string.IsNullOrEmpty(part.OwnerId))continue;
                    var at=zone.GetEntityCell(door);if(!BiomeSafe(zone,at,12))continue;
                    foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                    {var near=zone.GetCell(at.X+d.Item1,at.Y+d.Item2);if(BiomeSafe(zone,near,12)){selected=door;approach=near;return zone;}}
                }
            }return null;
        }
        private Zone BiomeLair(out Entity selected,out Zone floor)
        {
            selected=null;floor=null;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                if(BiomeManager.WorldMap.GetBiome(x,y)!=BiomeType.Spread||BiomeManager.WorldMap.GetPOI(x,y)?.Type!=POIType.Lair)continue;
                string id=WorldMap.ToZoneID(x,y,0);var zone=BiomeManager.GetZone(id);var plan=LairStacks.Inspect(BiomeManager,id);
                _biomeSteps.Add("lair-source inspected "+id);
                if(zone==null||plan==null||plan.Legacy||plan.FinalDepth<1)continue;
                var down=zone.GetReadOnlyEntities().SingleOrDefault(e=>e.HasPart<StairsDownPart>());
                if(down!=null)BiomeLairRisk(zone,zone.GetEntityCell(down),"surface-source-preflight");
                if(down==null||!BiomeSafe(zone,zone.GetEntityCell(down),2))continue;
                var next=BiomeManager.GetZone(plan.ZoneAt(1));if(next==null||!LairStacks.IsCommittedFloor(BiomeManager,next,BiomeType.Spread))continue;
                var up=next.GetReadOnlyEntities().SingleOrDefault(e=>e.HasPart<StairsUpPart>());
                if(up!=null)BiomeLairRisk(next,next.GetEntityCell(up),"first-floor-source-preflight");
                // Source selection requires an unoccupied, hazard-free arrival.
                // It admits real lair enemies nearby; normal scheduler and
                // original defense/HP stop decide whether descent succeeds.
                if(up!=null&&BiomeSafe(next,next.GetEntityCell(up),0)){selected=down;floor=next;return zone;}
            }return null;
        }
        private void BiomeLairRisk(Zone zone,Cell at,string label)
        {
            Require(at!=null,"existing lair stair anchor");
            var threats=BiomeThreats(zone);BiomeCellDiagnostic(zone,at,threats,label);
            _biomeSteps.Add(label+" nearby-native-owners="+string.Join("|",zone.GetReadOnlyEntities()
                .Where(e=>e.HasTag("Creature")&&SpatialQuery.DistanceToCell(zone,e,at.X,at.Y)<=8)
                .OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":distance="
                    +SpatialQuery.DistanceToCell(zone,e,at.X,at.Y)+":threat="+threats.Contains(e)+":target="+e.GetPart<BrainPart>()?.Target?.ID)));
            WriteReport();
        }
        private void BiomeForeign(out Zone selected,out Cell cell)
        {
            selected=null;cell=null;
            foreach(string id in new[]{"Overworld.2.5.0","Overworld.3.5.0","Overworld.4.5.0","Overworld.2.6.0","Overworld.4.6.0"})
            {
                var pos=WorldMap.FromZoneID(id);if(BiomeManager.WorldMap.GetBiome(pos.x,pos.y)==BiomeType.Spread)continue;
                var zone=BiomeManager.GetZone(id);if(zone==null||!AreaCompositionScope.Allows(zone))continue;
                for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
                    if(BiomeSafe(zone,zone.GetCell(x,y),12)){selected=zone;cell=zone.GetCell(x,y);return;}
            }
        }
        private IEnumerator BiomeTravel(Zone zone,Cell cell,string reason)
        {
            Require(State()=="Normal"&&BiomeSafe(zone,cell,2),"labelled source approach remains physically safe");
            var actor=_input.PlayerEntity;string gear=CombatGear(actor);int hp=actor.GetStatValue("Hitpoints"),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            var old=_input.CurrentZone;Require(old.TryTransferEntityTo(actor,zone,cell.X,cell.Y),"labelled travel preserves actual player owner");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=zone,NewPlayerX=cell.X,NewPlayerY=cell.Y}});
            _biomeShortcuts++;_input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("SpreadBiomeNativeAuditTravel");
            // A source shortcut still needs the normal next-frame visibility
            // and render submission after SetZone. Observe after that frame,
            // without advancing turns or forcing render/FOV state ourselves.
            yield return null; yield return new WaitForEndOfFrame();
            Require(actor.GetStatValue("Hitpoints")==hp&&CombatGear(actor)==gear&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy,"travel does not advance or alter actor state");
            _biomeSteps.Add("LABELLED TRAVEL "+_biomeShortcuts+" "+old.ZoneID+" -> "+zone.ZoneID+"@"+cell.X+","+cell.Y+" "+reason);WriteReport();
        }
        private IEnumerator BiomeReturnWest()
        {
            var zone=_input.CurrentZone;
            var destination=BiomeManager.GetZone(ReferenceGladePlan.ZoneID);
            Require(zone.ZoneID=="Overworld.12.10.0"&&destination!=null,"real visited east neighbor and original glade");
            for(int step=0;step<160;step++)
            {
                Require(ReferenceEquals(_input.CurrentZone,zone)&&State()=="Normal","same live return zone");
                int casts=_biomeDefensiveCalmUses;yield return BiomeCalmPursuingThreat();
                if(_biomeDefensiveCalmUses>casts)continue;
                var at=Cell();List<(int dx,int dy)> best=null;Cell exit=null;
                // A wandering creature may occupy yesterday's border cell.
                // Choose a currently safe paired edge, never move that owner.
                for(int y=0;y<Zone.Height;y++)
                {
                    // Chebyshev distance is an admissible lower bound for
                    // eight-direction moves. Skip BFS only if this edge cannot
                    // improve the already proven safe route; ties stay stable.
                    if(best!=null&&Math.Max(at.X,Math.Abs(at.Y-y))>=best.Count)continue;
                    var candidate=zone.GetCell(0,y);
                    if(!BiomeSafe(destination,destination.GetCell(Zone.Width-1,y),2))continue;
                    var path=BiomeSafePath(zone,at,candidate);
                    if(path!=null&&(best==null||path.Count<best.Count)){best=path;exit=candidate;}
                }
                if(best==null)BiomeRouteDiagnostic(zone,zone.GetCell(0,at.Y));
                Require(best!=null,"finite safe route to an actual paired western boundary");
                _biomeSteps.Add("adaptive-west-return from="+at.X+","+at.Y+" edge="+exit.X+","+exit.Y+" remaining="+best.Count);
                if(best.Count==0)
                {
                    yield return Tap(Key.A);
                    Require(ReferenceEquals(_input.CurrentZone,destination)&&Cell().X==Zone.Width-1&&Cell().Y==exit.Y,"native exact paired western crossing");
                    yield break;
                }
                var d=best[0];Require(BiomeSafe(zone,zone.GetCell(at.X+d.dx,at.Y+d.dy),2),"actual next return footprint remains safe");
                yield return Tap(Direction(d.dx,d.dy));
                Require(Cell().X==at.X+d.dx&&Cell().Y==at.Y+d.dy,"actual paid return step reaches chosen footprint");
            }
            throw new InvalidOperationException("Finite native western return exceeded160 steps.");
        }
        private IEnumerator BiomeWorldAction(Entity target,string command)
        {
            var from=Cell();var to=SpatialQuery.ClosestCell(_input.CurrentZone,target,from.X,from.Y);
            Require(to!=null&&SpatialQuery.Distance(_input.CurrentZone,_input.PlayerEntity,target)==1,"actual adjacent world owner");
            yield return Tap(Key.C);Require(State()=="AwaitingTalkDirection","native C direction prompt");yield return Tap(Direction(to.X-from.X,to.Y-from.Y));
            Require(State()=="WorldActionMenuOpen","native adjacent menu");
            if(_input.WorldActionMenuUI.SelectedCellIsPile)
                yield return BiomeMenuAction(WorldInteractionSystem.PickCellCommand);
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)
                ||((List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions")).Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+target.ID))
                yield return BiomeMenuAction(WorldInteractionSystem.PickTargetCommandPrefix+target.ID);
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target),"exact native target owner");yield return BiomeMenuAction(command);yield return BiomeCloseMenus();
        }
        private IEnumerator BiomeMenuAction(string command)
        {
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);Require(index>=0,"offered native command "+command);
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }
        private IEnumerator BiomeCloseMenus()
        {for(int i=0;State()!="Normal"&&i<5;i++)yield return Tap(Key.Escape);Require(State()=="Normal","native menu closes");}
        private string BiomeState()=>_input?.PlayerEntity==null?"no player":_input.CurrentZone.ZoneID+"@"+Cell()?.X+","+Cell()?.Y+" hp="+_input.PlayerEntity.GetStatValue("Hitpoints")+" tick="+_input.TurnManager.TickCount+" energy="+_input.TurnManager.GetEnergy(_input.PlayerEntity)+" input="+State();
    }
}
