using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Scenarios.Custom
{
 /// <summary>Real keyboard plant lifecycle in unchanged cold-generated patches.
 /// Two disclosed player travel shortcuts, or one in cave-only mode; no fixture objects or stat grants.</summary>
 public sealed partial class ReferenceGladeNativePlayer
 {
  private bool _biomeCropsOnly,_botanicalCaveOnly;
  public void ConfigureBotanicalCaveOnly(){_botanicalCaveOnly=true;}
  private string BiomeCropsMode=>_botanicalCaveOnly?"native-biome-cave-crops":"native-biome-crops";
  private static readonly string[] BotanicalCaveChecks={"botany_cave_source","botany_cave_harvest","botany_cave_light","botany_finish"};
  private static readonly string[] BiomeCropsChecks={"botany_generated_marlroot","botany_harvest_original","botany_pickup_original","botany_process_clay","botany_plant_seed","botany_learn_rain","botany_water","botany_sprout","botany_ripe","botany_repeat_harvest","botany_checkpoint","botany_cave_source","botany_cave_harvest","botany_cave_light","botany_finish"};
  private bool BiomeCropsComplete
  {
   get
   {
    var checks=_botanicalCaveOnly?BotanicalCaveChecks:BiomeCropsChecks;
    return checks.All(n=>_audit.Count(a=>a=="PASS "+n)==1)&&_audit.Count==checks.Length+1&&_biomeShortcuts==(_botanicalCaveOnly?1:2)&&_screenshots.Count>=(_botanicalCaveOnly?3:9);
   }
  }
  private string BiomeCropsCanVerify=>_botanicalCaveOnly?"Isolated ordinary seed64 player; actual connected ordinary cave Lampvein patch, native current-owner cave/crop rendering, keyboard harvest and physical seed/produce pickup. The harvest is stowed through the real equipment UI if automatically equipped, then dropped with keys to demonstrate actual carried-versus-ground lighting. Source and physical stair/path evidence are observed. Screenshots need visual review.":"Isolated ordinary seed64 player; real generated Marlroot patch harvested through native C actions, physical yield/seed picked up, carried clod processed into actual FireClay, returned seed replanted and watered using the original learned Conjure Rain; paid waits reach visible sprout and standing ripe, repeat harvest, F5/F6 saves exact source depletion and inventory. A real connected ordinary cave patch supplies Lampvein, harvested and dropped through keys to demonstrate an actual portable light. Current owners/models and source coordinates are recorded; screenshots need visual review.";
  private string BiomeCropsCannotVerify=>_botanicalCaveOnly?"One explicitly labelled travel shortcut selects an unchanged generated cave patch with actual incoming physical stairs and a usable arrival path. Local actions use real keys and live NPC scheduling. No grants, player boosts, stage edits, source replacement or forced light. This cave-only receipt does not establish the incomplete surface growth route, natural cave discovery, all35 utilities or subjective visual quality.":"Two explicitly labelled travel shortcuts select real generated surface and connected cave patches. Cold source selection pre-generates a bounded set of actual plan sites and cave depths; it grants no crop, seed, material, stage, moisture, equipment, health, turns or RNG. All local movement/actions use keys with live NPC scheduling. No natural discovery, all35 utilities, all-seed placement, offscreen growth, equipment art, economic balance or subjective model-quality claim. Unsafe approaches or danger stop the route honestly. The prepared clay is verified as real output; this route does not claim a subsequent well repair.";
  private IEnumerator RunBiomeCropsAudit()
  {
   bool crops=Diag.IsChannelEnabled("crop"),events=Diag.IsChannelEnabled("event");Diag.SetChannel("crop",true);Diag.SetChannel("event",true);
   try
   {
    if(_botanicalCaveOnly)
    {
     var actor=_input.PlayerEntity;Require(BiomeManager?.WorldSeed==64&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&actor.GetStat("Hitpoints").Max==40,"ordinary isolated botanical cave player");
     yield return BotanicalCaveRoute(actor.ID);
    }
    else yield return BiomeCropsRoute();
   }finally{Diag.SetChannel("crop",crops);Diag.SetChannel("event",events);}
  }
  private (Zone zone,Entity plant,Cell approach) FindBotanicalSource(string blueprint,bool cave)
  {
   if(!cave)
   {
    (Zone zone,Entity plant,Cell approach) best=(null,null,null);int separation=-1;
    foreach(var site in BiomeCropPlan.SurfaceSites(BiomeManager).Where(s=>BiomeCropCatalog.ForBiome(s.Biome)[s.SpeciesIndex].CropBlueprint==blueprint))
    {
     var zone=BiomeManager.GetZone(site.ZoneID);var found=BotanicalOwner(zone,blueprint,2);
     if(found.plant!=null&&found.separation>separation){best=(zone,found.plant,found.approach);separation=found.separation;}
    }
    return best;
   }
   else foreach(string column in BiomeCropPlan.CaveColumns(BiomeManager))
   {
    var surface=BiomeManager.GetZone(column);if(surface==null||!surface.GetReadOnlyEntities().Any(e=>e.HasPart<StairsDownPart>()))continue;
    var pos=WorldMap.FromZoneID(column);
    // Ordinary manager generation owns/stores each stair connection; do not
    // manufacture incoming edges or replace the actual cave layout.
    for(int depth=1;depth<=5;depth++)
    {
     var zone=BiomeManager.GetZone(WorldMap.ToZoneID(pos.x,pos.y,depth));if(zone==null)break;
     var found=BotanicalOwner(zone,blueprint,2);if(found.plant!=null&&ConnectedBotanicalApproach(zone,found.approach))return(zone,found.plant,found.approach);
    }
   }
   return(null,null,null);
  }
  private bool ConnectedBotanicalApproach(Zone zone,Cell approach)
  {
   foreach(var edge in BiomeManager.GetConnectionsTo(zone.ZoneID,"StairsDown"))
   {
    if(!BiomeManager.CachedZones.TryGetValue(edge.SourceZoneID,out var above)
     ||above.GetCell(edge.SourceX,edge.SourceY)?.HasObjectWithPart<StairsDownPart>()!=true
     ||zone.GetCell(edge.TargetX,edge.TargetY)?.HasObjectWithPart<StairsUpPart>()!=true)continue;
    if(FindPath.Search(zone,edge.TargetX,edge.TargetY,approach.X,approach.Y,actor:_input.PlayerEntity).Usable)return true;
   }
   return false;
  }
  private (Entity plant,Cell approach,int separation) BotanicalOwner(Zone zone,string blueprint,int clearance)
  {
   if(zone==null)return(null,null,-1);
   var threats=BiomeThreats(zone);(Entity plant,Cell approach,int separation) best=(null,null,-1);
   foreach(var crop in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==blueprint&&e.GetPart<CropPart>()?.GrowthStage==2))
   {
    var cell=zone.GetEntityCell(crop);if(!BiomeSafe(zone,cell,clearance,threats)||!CultivatedSoilPart.IsCultivated(zone,cell))continue;
    foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
    {
     var approach=zone.GetCell(cell.X+d.Item1,cell.Y+d.Item2);if(!BiomeSafe(zone,approach,clearance,threats))continue;
     int separation=threats.Length==0?int.MaxValue:threats.Min(e=>Math.Min(SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y),SpatialQuery.DistanceToCell(zone,e,approach.X,approach.Y)));
     if(separation>best.separation)best=(crop,approach,separation);
    }
   }
   if(best.plant!=null)
   {
    var at=zone.GetEntityCell(best.plant);
    _biomeSteps.Add("BOTANICAL SOURCE "+zone.ZoneID+"@"+at.X+","+at.Y+" approach="+best.approach.X+","+best.approach.Y+" minimumHostileDistance="+(best.separation==int.MaxValue?"none":best.separation.ToString())+" requiredClearance="+clearance+"; unchanged ordinary threats");
   }
   return best;
  }
  private IEnumerator BiomeCropsRoute()
  {
   var actor=_input.PlayerEntity;string actorID=actor.ID;Require(BiomeManager?.WorldSeed==64&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&actor.GetStat("Hitpoints").Max==40,"ordinary isolated botanical player");
   var source=FindBotanicalSource("MarlrootCrop",false);Require(source.zone!=null,"safe actual ripe Marlroot source in the finite native plan");
   yield return BiomeTravel(source.zone,source.approach,"actual generated Marlroot source; untouched plants, soil, contents and NPCs");
   var zone=source.zone;var original=source.plant;var bed=zone.GetEntityCell(original);var part=original.GetPart<CropPart>();
   var row=BiomeCropCatalog.ByBlueprint(original.BlueprintName);int yieldCount=part.YieldCount,seedCount=part.SeedYieldCount;
   string originalID=original.ID;_descriptions.Add(new Description{subject="Actual Marlroot patch",source=originalID,text=zone.ZoneID+"@"+bed.X+","+bed.Y+"; catalogue species "+row.Id+"; actual ripe source; no stage changes."});
   Check("botany_generated_marlroot",part.HarvestAtMaturity&&part.GrowthStage==2&&BiomeDrawn(original)&&BiomeCropRecipes.ResolveModel(zone,original)=="biome-crop-marlroot-2-dry");
   yield return CurationExamine(original,"botany-01-ripe-source-and-use");
   int beforeYield=ResidentUnits(actor,row.YieldBlueprint),beforeSeed=ResidentUnits(actor,row.SeedBlueprint);
   yield return ResidentWorldAction(original,"HarvestCultivatedCrop");yield return ResidentCloseMenus();
   var physical=bed.Objects.Where(e=>e.BlueprintName==row.YieldBlueprint||e.BlueprintName==row.SeedBlueprint).ToArray();
   Check("botany_harvest_original",zone.GetEntityCell(original)==null&&physical.Count(e=>e.BlueprintName==row.YieldBlueprint)==yieldCount&&physical.Count(e=>e.BlueprintName==row.SeedBlueprint)==seedCount&&CultivatedSoilPart.IsCultivated(zone,bed));
   yield return Capture("botany-02-physical-harvest");yield return WalkTo(bed.X,bed.Y);yield return RepairNativePickup();
   Check("botany_pickup_original",ResidentUnits(actor,row.YieldBlueprint)==beforeYield+yieldCount&&ResidentUnits(actor,row.SeedBlueprint)==beforeSeed+seedCount&&physical.All(e=>zone.GetEntityCell(e)==null));
   var raw=actor.GetPart<InventoryPart>().Objects.First(e=>e.BlueprintName==row.YieldBlueprint);var process=raw.GetPart<BotanicalProcessingPart>();Require(process!=null&&process.OutputBlueprint=="FireClay","real clod recipe");
   int rawBefore=ResidentUnits(actor,row.YieldBlueprint),clayBefore=ResidentUnits(actor,process.OutputBlueprint),count=process.OutputCount;var trace=Diag.Snapshot(Diag.BufferCapacity).Select(r=>r.TraceId).ToHashSet();
   yield return ItemAction(raw,BotanicalProcessingPart.Command);yield return ResidentCloseMenus();
   Check("botany_process_clay",ResidentUnits(actor,row.YieldBlueprint)==rawBefore-1&&ResidentUnits(actor,"FireClay")==clayBefore+count&&Diag.Snapshot(Diag.BufferCapacity).Any(r=>!trace.Contains(r.TraceId)&&r.Kind=="BotanicalProcessed"&&r.ActorId==actor.ID));
   yield return Capture("botany-03-useful-prepared-clay");
   var seed=actor.GetPart<InventoryPart>().Objects.First(e=>e.BlueprintName==row.SeedBlueprint);int seeds=ResidentUnits(actor,row.SeedBlueprint);
   yield return ItemAction(seed,"PlantSeed");yield return ResidentCloseMenus();
   var planted=bed.Objects.SingleOrDefault(e=>e.BlueprintName==row.CropBlueprint);Check("botany_plant_seed",planted!=null&&planted.ID!=originalID&&planted.GetPart<CropPart>().GrowthStage==0&&planted.GetPart<CropPart>().MoistureTicks==0&&ResidentUnits(actor,row.SeedBlueprint)==seeds-1);
   Require(planted!=null,"actual seed planting result");yield return Capture("botany-04-planted-returned-seed");
   var abilities=actor.GetPart<ActivatedAbilitiesPart>();var book=actor.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="WateringGrimoire");
   if(!abilities.AbilityList.Any(a=>a.Command=="CommandConjureRain")){yield return ItemAction(book,"ReadGrimoire");yield return ResidentCloseMenus();}
   var rain=abilities.AbilityList.SingleOrDefault(a=>a.Command=="CommandConjureRain");Check("botany_learn_rain",rain!=null&&actor.GetPart<InventoryPart>().Objects.Contains(book));Require(rain!=null,"actual original watering-book ability");int slot=abilities.GetSlotForAbility(rain.ID);Require(slot>=0&&slot<10,"native assigned rain hotbar slot");
   var crop=planted.GetPart<CropPart>();int waits=0,casts=0;bool sprout=false;Key rainKey=(Key)Enum.Parse(typeof(Key),"Digit"+((slot+1)%10));
   yield return Tap(rainKey);yield return CombatWaitForFx();yield return ResidentCloseMenus();casts++;
   Check("botany_water",crop.MoistureTicks>0&&crop.MoistureTicks<=40&&rain.CooldownRemaining>0);yield return Capture("botany-05-real-rain-and-damp-soil");
   while(crop.GrowthStage<2&&waits<200)
   {
    bool sameZone=ReferenceEquals(_input.CurrentZone,zone),samePlant=ReferenceEquals(zone.GetEntityCell(planted),bed),sameActor=ReferenceEquals(_input.PlayerEntity,actor);
    string state=State();int hp=actor.GetStatValue("Hitpoints");bool dead=CombatSystem.IsDeathHandled(actor);
    bool safe=sameZone&&samePlant&&sameActor&&state=="Normal"&&hp>10&&!dead;
    if(!safe)
    {
     var actual=zone.GetEntityCell(planted);var playerCell=Cell();
     string context="GROWTH STOP sameZone="+sameZone+" samePlant="+samePlant+" sameActor="+sameActor+" state="+state+" hp="+hp+" deathHandled="+dead+" waits="+waits+" casts="+casts+" tick="+_input.TurnManager.TickCount+" player="+(playerCell==null?"absent":playerCell.X+","+playerCell.Y)+" crop="+planted.ID+" actualCell="+(actual==null?"absent":actual.X+","+actual.Y)+" expectedCell="+bed.X+","+bed.Y+" stage="+crop.GrowthStage+" growth="+crop.TicksInStage+" moisture="+crop.MoistureTicks;
     _biomeSteps.Add(context);
     var reference=playerCell??bed;
     if(_input.CurrentZone!=null)foreach(var threat in BiomeThreats(_input.CurrentZone).OrderBy(e=>SpatialQuery.DistanceToCell(_input.CurrentZone,e,reference.X,reference.Y)).Take(5))
      _biomeSteps.Add("GROWTH THREAT "+threat.BlueprintName+" id="+threat.ID+" distanceFrom="+(playerCell==null?"originalBed":"player")+" distance="+SpatialQuery.DistanceToCell(_input.CurrentZone,threat,reference.X,reference.Y)+" targetingPlayer="+ReferenceEquals(threat.GetPart<BrainPart>()?.Target,_input.PlayerEntity));
     foreach(string message in MessageLog.GetRecent(12))_biomeSteps.Add("GROWTH LOG "+message);
     WriteReport();yield return Capture("botany-growth-stopped");Require(false,"safe ordinary growth wait; "+context);
    }
    if(crop.MoistureTicks==0&&rain.CooldownRemaining==0){Require(casts<8,"bounded rewatering");yield return Tap(rainKey);yield return CombatWaitForFx();yield return ResidentCloseMenus();casts++;}
    else{yield return Tap(Key.Period);waits++;yield return CombatWaitForFx();}
    if(!sprout&&crop.GrowthStage==1){Check("botany_sprout",BiomeDrawn(planted)&&crop.TicksInStage<crop.TicksPerStage);yield return Capture("botany-06-native-sprout");sprout=true;}
   }
   Check("botany_ripe",sprout&&crop.GrowthStage==2&&BiomeDrawn(planted)&&!bed.Objects.Any(e=>e.BlueprintName==row.YieldBlueprint));Require(crop.GrowthStage==2,"paid active-zone growth reaches standing maturity");
   _descriptions.Add(new Description{subject="Real regional plant growth",source=planted.ID,text="Marlroot ticks/stage="+crop.TicksPerStage+"; paid waits="+waits+"; rain casts="+casts+"; no clock/stage/moisture changes."});yield return Capture("botany-07-standing-mature");
   beforeYield=ResidentUnits(actor,row.YieldBlueprint);beforeSeed=ResidentUnits(actor,row.SeedBlueprint);string plantedID=planted.ID;
   yield return ResidentWorldAction(planted,"HarvestCultivatedCrop");yield return ResidentCloseMenus();yield return RepairNativePickup();
   Check("botany_repeat_harvest",zone.GetEntityCell(planted)==null&&ResidentUnits(actor,row.YieldBlueprint)==beforeYield+yieldCount&&ResidentUnits(actor,row.SeedBlueprint)==beforeSeed+seedCount&&CultivatedSoilPart.IsCultivated(zone,bed));
   yield return BiomeCropsCheckpoint(new[]{originalID,plantedID});yield return Capture("botany-08-reloaded-depleted-bed");
   yield return BotanicalCaveRoute(actorID);
  }
  private IEnumerator BotanicalCaveRoute(string actorID)
  {
   var actor=_input.PlayerEntity;var cave=FindBotanicalSource("LampveinCrop",true);Require(cave.zone!=null,"safe actually connected ordinary cave Lampvein source");
   yield return BiomeTravel(cave.zone,cave.approach,"real connected ordinary cave Lampvein patch; source generation and exact coordinates recorded, no synthetic cave");
   var caveBed=cave.zone.GetEntityCell(cave.plant);var cavePart=cave.plant.GetPart<CropPart>();var caveView=FindFirstObjectByType<SpawnRing3DPresenter>();
   _descriptions.Add(new Description{subject="Actual cave crop source",source=cave.plant.ID,text=cave.zone.ZoneID+"@"+caveBed.X+","+caveBed.Y+"; real StairsUp="+cave.zone.GetReadOnlyEntities().Any(e=>e.HasPart<StairsUpPart>())});
   Check("botany_cave_source",BiomeCropRecipes.IsOrdinaryCave(cave.zone)&&caveView!=null&&caveView.CurrentZone==cave.zone&&caveView.VoxelPresentationActive&&BiomeDrawn(cave.plant));yield return CurationExamine(cave.plant,"botany-09-cave-lampvein");
   int lights=BotanicalOwnedUnits(actor,cavePart.YieldBlueprint),caveSeeds=ResidentUnits(actor,cavePart.SeedYieldBlueprint);
   yield return ResidentWorldAction(cave.plant,"HarvestCultivatedCrop");yield return ResidentCloseMenus();yield return WalkTo(caveBed.X,caveBed.Y);yield return RepairNativePickup();
   Check("botany_cave_harvest",cave.zone.GetEntityCell(cave.plant)==null&&BotanicalOwnedUnits(actor,cavePart.YieldBlueprint)==lights+cavePart.YieldCount&&ResidentUnits(actor,cavePart.SeedYieldBlueprint)==caveSeeds+cavePart.SeedYieldCount);
   var lamp=BiomeOwnedItems(actor).First(e=>e.BlueprintName==cavePart.YieldBlueprint);
   if(InventorySystem.IsEquipped(actor,lamp))yield return BotanicalStowLamp(lamp);
   Require(actor.GetPart<InventoryPart>().Objects.Contains(lamp)&&!InventorySystem.IsEquipped(actor,lamp),"actual carried lamp before dark-vs-dropped comparison");
   var lightMap=new LightMap();lightMap.Compute(cave.zone);float dark=lightMap.GetBrightness(Cell().X,Cell().Y);
   yield return ItemAction(lamp,"drop");yield return ResidentCloseMenus();lightMap.Compute(cave.zone);
   Check("botany_cave_light",cave.zone.GetEntityCell(lamp)==Cell()&&lamp.GetPart<LightSourcePart>()?.Enabled==true&&BiomeDrawn(lamp)&&lightMap.GetBrightness(Cell().X,Cell().Y)>dark);yield return Capture("botany-10-harvested-light-in-cave");
   Check("botany_finish",actor.ID==actorID&&State()=="Normal"&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&_discoveryTonicsUsed==0);
  }
  private static int BotanicalOwnedUnits(Entity actor,string blueprint)=>BiomeOwnedItems(actor).Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
  private IEnumerator BotanicalStowLamp(Entity lamp)
  {
   yield return Tap(Key.I);Require(State()=="InventoryOpen"&&InvField<int>("_panel")==0,"actual equipment panel");
   var slots=InvField<List<InventoryScreenData.EquipmentSlot>>("_equipSlots");int start=InvField<int>("_equipCursorIndex"),target=slots.FindIndex(s=>ReferenceEquals(s.EquippedItem,lamp));Require(target>=0,"actual lamp paperdoll slot");
   // Read-only route through the displayed paperdoll coordinates. Input remains
   // ordinary arrow keys plus E; no cursor, body or inventory state is assigned.
   var queue=new Queue<int>();var routes=new Dictionary<int,List<Key>>{{start,new List<Key>()}};queue.Enqueue(start);
   var directions=new[]{(dx:0,dy:-1,key:Key.UpArrow),(dx:0,dy:1,key:Key.DownArrow),(dx:-1,dy:0,key:Key.LeftArrow),(dx:1,dy:0,key:Key.RightArrow)};
   while(queue.Count>0&&!routes.ContainsKey(target))
   {
    int current=queue.Dequeue();var at=slots[current];foreach(var d in directions)
    {
     int best=-1;float distance=float.MaxValue;
     for(int i=0;i<slots.Count;i++)
     {var slot=slots[i];int dx=slot.GridX-at.GridX,dy=slot.GridY-at.GridY;if(i==current||d.dx<0&&dx>=0||d.dx>0&&dx<=0||d.dy<0&&dy>=0||d.dy>0&&dy<=0)continue;float score=d.dx!=0?Math.Abs(dx)+Math.Abs(dy)*2:Math.Abs(dy)+Math.Abs(dx)*2;if(score<distance){distance=score;best=i;}}
     if(best>=0&&!routes.ContainsKey(best)){routes[best]=new List<Key>(routes[current]){d.key};queue.Enqueue(best);}
    }
   }
   Require(routes.ContainsKey(target),"bounded native paperdoll route to lamp");foreach(var key in routes[target])yield return Tap(key);
   Require(InvField<int>("_panel")==0&&InvField<int>("_equipCursorIndex")==target,"keys selected the exact equipped lamp");yield return Tap(Key.E);yield return ResidentCloseMenus();
   Require(!InventorySystem.IsEquipped(_input.PlayerEntity,lamp)&&_input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(lamp),"native E stows automatically equipped lamp");
  }
  private IEnumerator BiomeCropsCheckpoint(string[] gone)
  {
   var before=_input.PlayerEntity;var zone=_input.CurrentZone;var at=Cell();string field=RepairNativeFieldState(zone),items=BiomeItemCollection(before);int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(before),hp=before.GetStatValue("Hitpoints");
   var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"isolated existing quicksave");string path=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),old=CheckpointHash(path);
   yield return Tap(Key.F5);string saved=CheckpointHash(path);Require(saved!=old&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==zone.ZoneID,"actual current patch F5 save");
   var next=new[]{(1,0),(-1,0),(0,1),(0,-1)}.Select(d=>zone.GetCell(at.X+d.Item1,at.Y+d.Item2)).FirstOrDefault(c=>BiomeSafe(zone,c,0));Require(next!=null,"safe real post-save step");yield return Tap(Direction(next.X-at.X,next.Y-at.Y));Require(Cell()==next&&CheckpointHash(path)==saved,"ordinary step without checkpoint overwrite");
   yield return Tap(Key.F6);double start=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(before,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-start<8,"real F6 graph replacement");yield return null;}
   var loaded=_input.CurrentZone;var actor=_input.PlayerEntity;
   Check("botany_checkpoint",loaded.ZoneID==zone.ZoneID&&!ReferenceEquals(loaded,zone)&&actor.ID==before.ID&&Cell().X==at.X&&Cell().Y==at.Y&&RepairNativeFieldState(loaded)==field&&BiomeItemCollection(actor)==items&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy&&actor.GetStatValue("Hitpoints")==hp&&CheckpointHash(path)==saved&&!loaded.GetReadOnlyEntities().Any(e=>gone.Contains(e.ID)));
  }
 }
}
