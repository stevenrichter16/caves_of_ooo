using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
namespace CavesOfOoo.Core
{
 /// <summary>Three cold-generation worksites. Stock is moved with its exact existing contents;
 /// ordinary hostile receipts authorize count-preserving replacements. No saved replay.</summary>
 public static class SpreadExplorationWorksites
 {
  /// <summary>Presentation/debug identity on new site owners; never authority to acquire a source.</summary>
  public const string RoleKey="SpreadWorksite.Role";
  static readonly (int x,int y) Arrival=(40,12);
  const int MaxTrials=256;
  public static bool TryPlace(Zone zone,EntityFactory factory,SpreadCompositionBuilder terrain,
   PopulationBuilder population,ContainerBuilder containers,SpreadExplorationFamily family,Func<bool> authority,
   out Entity[] owners,out Func<bool> final)
   =>TryPlaceCore(zone,factory,terrain,population,containers,family,authority,true,out owners,out final);
  static bool TryPlaceCore(Zone zone,EntityFactory factory,SpreadCompositionBuilder terrain,
   PopulationBuilder population,ContainerBuilder containers,SpreadExplorationFamily family,Func<bool> authority,bool allowDrying,
   out Entity[] owners,out Func<bool> final)
  {
   owners=null;final=null;
   bool alembic=family==SpreadExplorationFamily.FieldAlembic,forge=family==SpreadExplorationFamily.TemperingShelter,store=family==SpreadExplorationFamily.TrappersStore;
   if((!alembic&&!forge&&!store)||zone==null||factory==null||authority==null)return false;
   var plan=terrain?.Plan;var actors=population?.SourceReceipt;var stock=store?containers?.SourceReceipt:null;
   bool Identity()=>terrain?.SourceZone==zone&&terrain.Plan==plan&&plan?.ZoneID==zone.ZoneID
    &&population?.CaptureSourceReceipts==true&&population.SourceReceipt==actors&&actors?.Zone==zone&&actors.Factory==factory
    &&(!store||(containers?.CaptureSourceReceipts==true&&containers.SourceReceipt==stock&&stock?.Zone==zone&&stock.Factory==factory));
   bool Sources()=>Identity()&&actors.IsCurrent&&actors.Owners.Count>=1&&actors.Owners.Count<=2
    &&actors.Owners.Select(e=>e.BlueprintName).Distinct().Count()==1&&actors.Owners.All(e=>OrdinaryActor(zone,e))&&(!store||stock.IsCurrent);
   if(!Sources()||!authority()||!Sources()||zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey(RoleKey)))return false;
   var cache=store?stock.Owners.FirstOrDefault(e=>Cache(zone,e)):null;if(store&&cache==null)return false;
   // Optional enrichment is confined to the ungenerated northern alembic.
   // Missing content keeps its earlier service before any factory or receipt is used.
   bool drying=allowDrying&&alembic&&zone.ZoneID=="Overworld.11.9.0"&&LoadoutPart.Factory==factory
    &&new[]{"MarlbackPatchbearer","HealingTonic","MendleafPlant","MendleafSprig","AlchemyShelf","BrewedTonic"}.All(factory.Blueprints.ContainsKey);
   bool pair=!alembic&&actors.Owners.Count==2;
   // Only this freshly authored pair gains a literal, optional emergency supply.
   // Missing content retains the earlier shelter; malformed content refuses below.
   bool emergencyWater=forge&&pair&&zone.ZoneID=="Overworld.15.10.0"&&factory.Blueprints.ContainsKey("SunbladderShell");
   var replaced=drying?actors.Owners.OrderBy(e=>Distance(zone.GetEntityPosition(e),Arrival)).Take(1).ToArray():pair?actors.Owners.ToArray():Array.Empty<Entity>();var selected=new HashSet<Entity>(replaced);if(cache!=null)selected.Add(cache);
   string caster=forge?"MarlbackCindercaller":"MarlbackSoursprayer";
   var specs=new List<(string bp,string role,int x,int y)>();
   if(alembic){specs.Add(("AlchemyStill","still",0,0));specs.Add(("StoneburrPatch","binding-forage",-2,1));specs.Add(("FrostLichenPatch","cold-forage",2,1));}
   else if(forge){specs.Add(("TinkersForge","forge",0,0));specs.Add(("StoneburrPatch","binding-forage",2,1));}
   else
   {
    specs.Add(("FrostLichenPatch","cold-forage",-4,2));specs.Add(("SpikeTrap","trap",3,0));specs.Add(("FallenBeam","haul-bypass",-3,0));
    for(int y=-2;y<=2;y++)for(int x=-3;x<=3;x++)
     if((Math.Abs(x)==3||Math.Abs(y)==2)&&!(y==0&&Math.Abs(x)==3))specs.Add(("StoneWall","broken-wall",x,y));
   }
   if(!store)foreach(var p in new[]{(-2,-2),(-1,-2),(2,-2)})specs.Add(("StoneWall","broken-wall",p.Item1,p.Item2));
   if(drying)
   {
    specs.Add(("MendleafPlant","medicine-forage",-7,2));
    specs.Add(("AlchemyShelf","drying-shelf",4,-1));specs.Add(("AlchemyShelf","drying-shelf",4,1));
    specs.Add(("MarlbackPatchbearer","patchbearer",6,0));
    foreach(int side in new[]{-1,1})foreach(var p in new[]{(3,3),(4,3),(5,3),(5,2)})specs.Add(("StoneWall","broken-wall",p.Item1,p.Item2*side));
   }
   if(pair){specs.Add((caster,"ranged",store?1:2,-1));specs.Add(("MarlbackScrabbler","melee",-1,1));}
   if(specs.Any(s=>!factory.Blueprints.ContainsKey(s.bp))||(pair&&LoadoutPart.Factory!=factory))return false;
   var all=new HashSet<Entity>(zone.GetReadOnlyEntities());var initial=SpreadGenerationReceipt.CaptureFinalState(zone,all);
   var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,all.Where(e=>!selected.Contains(e)));
   var before=new SpreadWildernessSituationBuilder.Geometry(zone,selected);
   var retainedThreats=drying?actors.Owners.Where(e=>!selected.Contains(e)).ToArray():Array.Empty<Entity>();
   var anchor=zone.GetEntityPosition(cache??(drying?replaced[0]:actors.Owners[0]));
   var offsets=specs.Select(s=>(s.x,s.y)).Concat(cache==null?Array.Empty<(int,int)>():new[]{(0,0)}).ToArray();
   var positions=Array.Empty<(int x,int y)>();(int x,int y) origin=default;int rotation=0,trials=0;
   foreach(var at in from y in Enumerable.Range(4,Zone.Height-8) from x in Enumerable.Range(4,Zone.Width-8) orderby Distance(anchor,(x,y)),y,x select(x,y))
   {
    for(int turn=0;turn<(drying?4:2);turn++)
    {
     var candidate=offsets.Select(p=>At(at,p,turn)).ToArray();
     if(candidate.Any(p=>Distance(p,Arrival)<=6||!before.Place(p.x,p.y)))continue;
     if(drying&&!DryingBounds(before,at,turn))continue;
     if(++trials>MaxTrials)break;
     if(!Fits(zone,before,before,candidate,specs,pair,store,drying,retainedThreats,at,turn,plan))continue;
     positions=candidate;origin=at;rotation=turn;break;
    }
    if(positions.Length>0||trials>MaxTrials)break;
   }
   if(positions.Length==0&&!drying)return false;
   if(!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities()))return false;
   // No enriched shape fits. Only this pre-staging branch can retry the old packet;
   // failed factory/ownership transactions below never consume another attempt.
   if(positions.Length==0)return drying&&TryPlaceCore(zone,factory,terrain,population,containers,family,authority,false,out owners,out final);
   var staged=new List<Entity>();var added=new HashSet<Entity>();var removed=new HashSet<Entity>();
   EmergencyWaterStock waterStock=null;
   bool SupplyShape()=>!emergencyWater||waterStock?.Matches()==true;
   bool StoreShape()=>!store||(staged.Count==specs.Count&&staged.Select((e,i)=>StoreOwner(e,specs[i].bp,specs[i].role)).All(valid=>valid));
   bool DryingShape()=>!drying||(staged.Count==specs.Count&&staged.Select((e,i)=>DryingOwner(e,specs[i].bp,specs[i].role)).All(valid=>valid));
   var oldPositions=selected.ToDictionary(e=>e,zone.GetEntityPosition);
   bool Provenance()=>Identity();
   bool Others()=>unchanged()&&all.Except(removed).Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
   bool Owned()=>Provenance()&&actors.MatchesOwnedState(removed)&&(!store||stock.MatchesOwnedState(removed));
   // Factories execute callbacks. Every creation must leave the original packet untouched.
   try
   {
    // Create the supply first: its callback cannot rewrite an already prepared
    // caster. Keep the exact detached proof until its actual inventory accepts it.
    if(emergencyWater&&(!EmergencyWaterStock.TryCreate(factory,out waterStock)||!authority()||!Sources()||!initial()
     ||!all.SetEquals(zone.GetReadOnlyEntities())))return false;
    foreach(var spec in specs)
    {
     var e=factory.CreateEntity(spec.bp);
     if(!Fresh(e,spec.bp)||!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities()))return false;
     e.Properties[RoleKey]=spec.role;
     if(spec.role=="melee")
     {var tactics=e.GetPart<CombatTacticsPart>();if(tactics==null){tactics=new CombatTacticsPart{SkillClasses="",AbilityChance=0};e.AddPart(tactics);}tactics.AssistAllies=true;tactics.AssistRadius=6;}
     if(spec.role=="ranged"&&(!e.HasTag("Creature")||e.GetPart<BrainPart>()==null||e.GetPart<BrainPart>().SightRadius<1||e.GetPart<BrainPart>().SightRadius>6||e.GetPart<CombatTacticsPart>()?.AssistAllies!=true
      ||e.GetPart<SkillsPart>()?.HasSkill(forge?"Pyromancy_EmberSpit":"Corrosion_AcidSpray")!=true||e.GetPart<ActivatedAbilitiesPart>()==null))return false;
     if(emergencyWater&&spec.role=="ranged"&&!waterStock.Attach(e))return false;
     if((spec.role=="still"&&!e.HasPart<AlchemyStillPart>())||(spec.role=="forge"&&!e.HasPart<ForgePart>())||(spec.role=="trap"&&(!e.HasPart<SpikeTrapTriggerPart>()||!e.HasPart<TrapJammingPart>()))
      ||(spec.role=="broken-wall"&&e.GetPart<PhysicsPart>()?.Solid!=true)||((spec.role=="binding-forage"||spec.role=="cold-forage")&&!e.HasPart<HarvestablePart>()))return false;
     if(store&&!StoreOwner(e,spec.bp,spec.role))return false;
     if(drying&&!DryingOwner(e,spec.bp,spec.role))return false;
     if(e.HasPart<HarvestablePart>()){var h=e.GetPart<HarvestablePart>();if(h.Harvested||h.YieldChance!=100||h.YieldMin<1||h.YieldMax<h.YieldMin||h.YieldMax>2||!factory.Blueprints.ContainsKey(h.YieldBlueprint))return false;}
     string cue=spec.role=="still"?"The field alembic still works. Single flasks can be brewed in the field; this still also prepares batches. Stoneburr carries the binding used in stoneskin brews; frost lichen carries cold for a freezing coating."
      :spec.role=="forge"?"The roadside forge still works. Carried weapon components can be forged or used to re-forge a weapon. A brewed coating can quench a weapon here; frost lichen supplies the cold for freezing coatings."
      :spec.role=="trap"?"Exposed spike teeth guard the direct entrance to the dispatch yard. One carried salvaged timber can jam this mechanism. A fallen beam blocks the service opening on the other side. While armed, anything stepping on the teeth springs this trap once."
      :spec.role=="haul-bypass"?"A fallen roof beam blocks the dispatch yard's service opening. There is room outside to haul it back, turn aside, and release it clear of the entrance. A heavy load slows its hauler; the supply cache remains inside."
      :spec.role=="binding-forage"?"A small, finite stand of stoneburr. Gather its seeds and brew one flask anywhere for a stoneskin tonic. A still can prepare batches."
      :spec.role=="cold-forage"?"A small, finite stand of frost lichen. Brew its cold into a freezing coating, then quench a melee weapon beside a forge. Drinking the coating freezes the drinker.":null;
     if(spec.role=="medicine-forage")cue="A small, finite stand of mendleaf at the drying yard's outer margin. Gather its sprigs and brew one at a time anywhere for a weak mending tonic. A patchbearer works the deeper yard; its bottle harness shows what stronger medicine remains.";
     if(spec.role=="drying-shelf")cue="A broad drying shelf divides the working bay, blocking passage and sight through its frame. Broken stone windbreaks shelter the bays; the central aisle and rear remain open.";
     if(cue!=null){var examine=e.GetPart<ExaminablePart>();if(examine==null){examine=new ExaminablePart();e.AddPart(examine);}examine.Text=cue;}
     staged.Add(e);
    }
   }
   catch(Exception){return false;}
   if(!StoreShape()||!DryingShape()||!UniqueGraph(zone,staged)||!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities())||!SupplyShape())return false;
   if(!actors.TryConsume()||(store&&!stock.TryConsume()))return false;
   var detached=replaced.ToDictionary(e=>e,actors.CaptureDetachedOwnerState);
   var placedProof=new Dictionary<Entity,Func<bool>>();bool success=false;
   var packet=staged.Concat(cache==null?Array.Empty<Entity>():new[]{cache}).ToArray();
   bool AddedState()=>placedProof.Values.All(p=>p());
   try
   {
    foreach(var e in replaced){if(!authority()||!Owned()||!Others()||!zone.RemoveEntity(e))return false;removed.Add(e);}
    if(cache!=null){if(!authority()||!Owned()||!Others()||!zone.RemoveEntity(cache))return false;removed.Add(cache);}
    for(int i=0;i<packet.Length;i++)
    {
     var e=packet[i];var at=positions[i];
     if(!authority()||!Owned()||!Others()||!AddedState()||!SupplyShape()||!new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>()).Place(at.x,at.y))return false;
     if(!zone.AddEntity(e,at.x,at.y))return false;
     if(e==cache)removed.Remove(e);else added.Add(e);
     placedProof[e]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
     if(!authority()||!Owned()||!Others()||!AddedState()||!SupplyShape())return false;
    }
    var finalPacket=SpreadGenerationReceipt.CaptureFinalState(zone,packet);
    bool Final()=>authority()&&Provenance()&&StoreShape()&&DryingShape()&&SupplyShape()&&actors.MatchesOwnedState(new HashSet<Entity>(replaced))
     &&(!store||stock.MatchesOwnedState())&&finalPacket()&&detached.Values.All(p=>p())
     &&Fits(zone,new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>(packet)),before,positions,specs,pair,store,drying,retainedThreats,origin,rotation,plan);
    if(!Final()||!Others())return false;owners=packet;final=Final;success=true;return true;
   }
   finally
   {
    if(!success)
    {
     // Do not reclaim owners independently changed or transferred by a callback.
     foreach(var e in packet.Reverse())if(placedProof.TryGetValue(e,out var proof)&&proof())
      {zone.RemoveEntity(e);added.Remove(e);if(e==cache)removed.Add(e);}
     foreach(var e in selected)
      if(removed.Contains(e)&&e.SpatialZone==null&&(e==cache?stock.MatchesOwnedState(new HashSet<Entity>{cache}):detached.TryGetValue(e,out var detachedProof)&&detachedProof())
       &&new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>()).Place(oldPositions[e].x,oldPositions[e].y))
       zone.AddEntity(e,oldPositions[e].x,oldPositions[e].y);
    }
   }
  }
  static bool Fresh(Entity e,string bp)=>e!=null&&e.BlueprintName==bp&&!string.IsNullOrEmpty(e.ID)&&e.SpatialZone==null
   &&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==e&&p.InInventory==null&&p.Equipped==null
   &&e.GetPart<RenderPart>()?.ParentEntity==e&&!e.HasPart<SpatialFootprintPart>()&&e.Parts.All(part=>part!=null&&part.ParentEntity==e);
  // A local generation receipt, not a saved stock counter. Runtime use consumes
  // the ordinary Waterskin charges and leaves the real shell in the inventory.
  sealed class EmergencyWaterStock
  {
   const string Shell="SunbladderShell";
   readonly Entity item;readonly string itemId;readonly WaterskinPart skin;readonly PhysicsPart physical;
   readonly Part[] parts;readonly Func<bool> detached;
   Entity actor;string actorId;InventoryPart inventory;TacticalSupplyPart policy;
   EmergencyWaterStock(Entity item)
   {
    this.item=item;itemId=item.ID;skin=item.GetPart<WaterskinPart>();physical=item.GetPart<PhysicsPart>();
    parts=item.Parts.ToArray();detached=SpreadGenerationReceipt.CaptureDetachedState(item);
   }
   internal static bool TryCreate(EntityFactory factory,out EmergencyWaterStock stock)
   {
    stock=null;var item=factory.CreateEntity(Shell);
    if(!Fresh(item,Shell)||item.GetPart<PhysicsPart>().Takeable!=true
     ||item.GetPart<WaterskinPart>() is not WaterskinPart skin||skin.ParentEntity!=item||skin.Capacity!=2||skin.Charges!=1
     ||item.Parts.Count(p=>p is WaterskinPart)!=1||item.Parts.Count(p=>p is PhysicsPart)!=1||item.HasPart<LiquidVesselPart>()
     ||(item.GetPart<StackerPart>()?.StackCount??1)!=1)return false;
    var candidate=new EmergencyWaterStock(item);if(!candidate.detached())return false;
    stock=candidate;return true;
   }
   internal bool Attach(Entity owner)
   {
    if(actor!=null||!detached()||owner?.BlueprintName!="MarlbackCindercaller"||owner.GetProperty(RoleKey)!="ranged"
     ||owner.GetPart<InventoryPart>() is not InventoryPart pack||pack.ParentEntity!=owner
     ||owner.Parts.Count(p=>p is InventoryPart)!=1||owner.HasPart<TacticalSupplyPart>()
     ||pack.Objects.Any(e=>e?.BlueprintName==Shell)||!pack.AddObject(item))return false;
    actor=owner;actorId=owner.ID;inventory=pack;policy=new TacticalSupplyPart{SelfDousing=true};owner.AddPart(policy);
    return Matches();
   }
   internal bool Matches()=>actor!=null&&actor.ID==actorId&&actor.BlueprintName=="MarlbackCindercaller"&&actor.GetProperty(RoleKey)=="ranged"
    &&actor.GetPart<InventoryPart>()==inventory&&inventory.ParentEntity==actor&&actor.Parts.Count(p=>p is InventoryPart)==1
    &&actor.GetPart<TacticalSupplyPart>()==policy&&policy.ParentEntity==actor&&policy.SelfDousing&&actor.Parts.Count(p=>p is TacticalSupplyPart)==1
    &&item.ID==itemId&&item.BlueprintName==Shell&&item.Parts.SequenceEqual(parts)&&parts.All(p=>p.ParentEntity==item)
    &&item.GetPart<PhysicsPart>()==physical&&item.GetPart<WaterskinPart>()==skin
    &&inventory.Objects.Count(e=>e?.BlueprintName==Shell)==1
    &&WaterTransferActions.Vessel(actor,item,out int units,out int capacity)&&units==1&&capacity==2;
  }
  // Later factory/addition callbacks can mutate a previously staged owner.
  // A snapshot of such a mutation is not proof of the promised physical route.
  // Check the same semantics at creation, after staging and at final acceptance.
  static bool StoreOwner(Entity e,string blueprint,string role)
  {
   if(e==null||e.BlueprintName!=blueprint||e.GetProperty(RoleKey)!=role||e.HasPart<SpatialFootprintPart>()
    ||e.Parts.Any(p=>p==null||p.ParentEntity!=e))return false;
   if(role!="broken-wall"&&role!="haul-bypass"&&role!="trap")return true;
   var physical=e.GetPart<PhysicsPart>();
   if(physical?.ParentEntity!=e||physical.Takeable||physical.InInventory!=null||physical.Equipped!=null||e.HasTag("Creature"))return false;
   if(role=="broken-wall")return physical.Solid;
   if(role=="haul-bypass")return physical.Solid&&e.GetPart<HandlingPart>() is HandlingPart handling
    &&handling.Weight==60&&handling.MinLiftStrength==0&&!handling.Carryable&&!e.HasPart<HarvestablePart>()&&!e.HasPart<DestructiblePart>();
   return !physical.Solid&&!e.HasTag("Solid")&&TrapJammingPart.IsSupported(e)&&!TrapJammingPart.IsJammed(e)
    &&e.GetPart<SpikeTrapTriggerPart>()?.ConsumeOnTrigger==true;
  }
  static bool DryingOwner(Entity e,string blueprint,string role)
  {
   if(e==null||e.BlueprintName!=blueprint||e.GetProperty(RoleKey)!=role||e.HasPart<SpatialFootprintPart>()
    ||e.Parts.Any(p=>p==null||p.ParentEntity!=e))return false;
   var physical=e.GetPart<PhysicsPart>();
   if(physical?.ParentEntity!=e||physical.InInventory!=null||physical.Equipped!=null)return false;
   if(role=="patchbearer")
   {
    var medicine=e.GetPart<FieldMedicinePart>();var tonic=medicine?.FindCarriedMedicine();var inventory=e.GetPart<InventoryPart>();
    return e.HasTag("Creature")&&e.GetStatValue("Hitpoints")==20&&e.GetStat("Hitpoints")?.Max==20
     &&e.GetPart<BrainPart>()?.SightRadius==10&&(e.GetPart<CombatTacticsPart>()?.AssistRadius??0)<=10
     &&medicine!=null&&medicine.UseAtOrBelowPercent==40&&medicine.TonicBlueprint=="HealingTonic"&&tonic?.GetPart<TonicPart>()?.Healing=="4d6+4"
     &&inventory!=null&&inventory.Objects.Where(item=>item?.BlueprintName=="HealingTonic").Sum(item=>item.GetPart<StackerPart>()?.StackCount??1)==1;
   }
   if(role=="drying-shelf")return !e.HasTag("Creature")&&physical.Solid&&!physical.Takeable&&e.HasTag("Solid")
    &&e.GetPart<ContainerPart>() is ContainerPart container&&!container.IsLocked&&container.Contents.Count==0;
   if(role=="medicine-forage")return !e.HasTag("Creature")&&!physical.Solid&&!physical.Takeable&&!e.HasPart<CropPart>()
    &&e.GetPart<HarvestablePart>() is HarvestablePart harvest&&!harvest.Harvested&&harvest.YieldBlueprint=="MendleafSprig"
    &&harvest.YieldChance==100&&harvest.YieldMin==1&&harvest.YieldMax==2;
   if(role=="broken-wall")return physical.Solid&&e.HasTag("Solid");
   return true;
  }
  static bool OrdinaryActor(Zone z,Entity e)=>e!=null&&(e.BlueprintName=="Viper"||e.BlueprintName=="MarlbackScrabbler")
   &&e.SpatialZone==z&&z.GetEntityCell(e)!=null&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0
   &&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.InInventory==null&&p.Equipped==null&&!e.HasPart<SpatialFootprintPart>()
   &&e.GetPart<BrainPart>() is BrainPart b&&b.Target==null&&b.PartyLeader==null&&b.PartyMembers.Count==0&&!b.HasGoalOtherThan("BoredGoal")
   &&!e.HasTag("Player")&&!e.HasTag("Pet")&&!e.HasTag("Companion")&&!e.HasTag("Owned")&&!e.HasTag("Unique")&&!e.HasTag("QuestItem")
   &&!e.HasPart<TraderPart>()&&!e.HasPart<ConversationPart>()&&!e.HasPart<SpreadTerritoryPart>()&&!e.HasPart<SpreadCollectorPart>()
   &&!e.HasPart<DragPart>()&&!e.HasPart<DraggedPart>()&&!z.GenReservedCells.Contains(z.GetEntityPosition(e))&&!z.GetEntityCell(e).IsInterior&&!CombatSystem.IsDeathHandled(e);
  static bool Cache(Zone z,Entity e)=>e!=null&&(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&e.SpatialZone==z
   &&e.GetPart<ContainerPart>() is ContainerPart c&&c.ParentEntity==e&&!c.IsLocked&&e.GetPart<PhysicsPart>() is PhysicsPart p&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
   &&!e.HasTag("Owned")&&!e.HasTag("QuestItem")&&!e.HasTag("Unique")&&!e.HasPart<SpatialFootprintPart>()&&!e.HasPart<DraggedPart>()
   &&z.GetEntityCell(e)!=null&&!z.GetEntityCell(e).IsInterior&&!z.GenReservedCells.Contains(z.GetEntityPosition(e));
  static bool UniqueGraph(Zone z,IEnumerable<Entity> roots)
  {
   var seen=new HashSet<Entity>();var ids=new HashSet<string>(z.GetReadOnlyEntities().Select(e=>e.ID));var queue=new Queue<Entity>(roots);
   while(queue.Count>0)
   {
    var e=queue.Dequeue();if(e==null||!seen.Add(e)||seen.Count>128||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID)||e.SpatialZone!=null||e.Parts.Any(p=>p==null||p.ParentEntity!=e))return false;
    var inventory=e.GetPart<InventoryPart>();if(inventory!=null)
    {
     foreach(var item in inventory.Objects){if(item?.GetPart<PhysicsPart>()?.InInventory!=e)return false;queue.Enqueue(item);}
     foreach(var item in inventory.EquippedItems.Values.Distinct()){if(item?.GetPart<PhysicsPart>()?.Equipped!=e)return false;if(!inventory.Objects.Contains(item))queue.Enqueue(item);}
    }
    var container=e.GetPart<ContainerPart>();if(container!=null)foreach(var item in container.Contents){if(item?.GetPart<PhysicsPart>()?.InInventory!=e)return false;queue.Enqueue(item);}
   }
   return true;
  }
  static int Distance((int x,int y)a,(int x,int y)b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
  static (int x,int y) At((int x,int y)at,(int x,int y)p,int turn)=>turn==0?(at.x+p.x,at.y+p.y)
   :turn==1?(at.x-p.y,at.y+p.x):turn==2?(at.x-p.x,at.y-p.y):(at.x+p.y,at.y-p.x);
  static bool Fits(Zone z,SpreadWildernessSituationBuilder.Geometry g,SpreadWildernessSituationBuilder.Geometry before,(int x,int y)[] positions,
   List<(string bp,string role,int x,int y)> specs,bool pair,bool store,bool drying,Entity[] retainedThreats,(int x,int y)origin,int rotation,SpreadCompositionPlan plan)
  {
   if(positions.Any(p=>Distance(p,Arrival)<=6||!g.Place(p.x,p.y))||!g.PreservesAgainst(before,positions))return false;
   var blocked=new HashSet<(int x,int y)>(positions);
   var threat=specs.Select((s,i)=>(s.role,at:positions[i])).Where(s=>s.role=="ranged"||s.role=="melee"||s.role=="patchbearer").ToArray();
   bool Avoid(int x,int y)=>threat.Any(t=>Distance((x,y),t.at)<=(t.role=="ranged"?6:10))
    ||retainedThreats.Any(e=>Distance((x,y),z.GetEntityPosition(e))<=Math.Max(e.GetPart<BrainPart>().SightRadius,e.GetPart<CombatTacticsPart>()?.AssistRadius??0));
   var safe=g.Flood(blocked,Arrival,int.MaxValue,Avoid);
   var reachable=g.Flood(blocked,Arrival,int.MaxValue,null);
   if(!safe[Arrival.x,Arrival.y])return false;
   var exits=new[]{(x:0,y:plan.WestY),(x:Zone.Width-1,y:plan.EastY),(x:plan.NorthX,y:0),(x:plan.SouthX,y:Zone.Height-1)};
   // The original critical routes stay physically connected above. A retained
   // hostile may already watch a border; the shallow outcome needs one safe
   // withdrawal, not a promise to make every ordinary border peaceful.
   if(drying){if(!exits.Any(p=>before.BorderReach[p.x,p.y]&&safe[p.x,p.y]))return false;}
   else foreach(var p in exits)if(before.BorderReach[p.x,p.y]&&!safe[p.x,p.y])return false;
   if(store)return FitsStore(g,positions,specs,origin,rotation);
   if(drying&&!FitsDrying(g,blocked,safe,reachable,origin,rotation))return false;
   // Every functional owner retains two physical approaches; neither harvesting
   // nor opening the actual cache forces the player onto the visible trap.
   for(int i=0;i<positions.Length;i++)
   {
    if(i<specs.Count&&(specs[i].role=="broken-wall"||specs[i].role=="melee"||specs[i].role=="ranged"||specs[i].role=="patchbearer"||specs[i].role=="trap"))continue;
    var p=positions[i];if(new[]{(p.x-1,p.y),(p.x+1,p.y),(p.x,p.y-1),(p.x,p.y+1)}.Count(n=>g.Walk(n.Item1,n.Item2,blocked)&&reachable[n.Item1,n.Item2])<2)return false;
   }
   return true;
  }
  static bool DryingBounds(SpreadWildernessSituationBuilder.Geometry g,(int x,int y)origin,int rotation)
  {
   // This bounds the work area; scenery outside the actual owners/approaches
   // is left alone and need not be empty.
   foreach(var corner in new[]{(-8,-4),(-8,4),(8,-4),(8,4)})
   {var p=At(origin,corner,rotation);if(!g.In(p.x,p.y))return false;}
   return true;
  }
  static bool FitsDrying(SpreadWildernessSituationBuilder.Geometry g,HashSet<(int x,int y)> blocked,bool[,] safe,bool[,] reachable,(int x,int y)origin,int rotation)
  {
   if(!DryingBounds(g,origin,rotation))return false;
   foreach(var local in new[]{(3,0),(4,0),(5,0),(6,-1),(6,1),(7,0),(3,-1),(5,-1),(3,1),(5,1),(4,-4),(4,-2)})
   {var p=At(origin,local,rotation);if(!g.Walk(p.x,p.y,blocked)||!reachable[p.x,p.y])return false;}
   var herb=At(origin,(-7,2),rotation);
   if(new[]{(herb.x-1,herb.y),(herb.x+1,herb.y),(herb.x,herb.y-1),(herb.x,herb.y+1)}.Count(p=>g.In(p.Item1,p.Item2)&&safe[p.Item1,p.Item2])<2)return false;
   // The aisle is one route; closing its midpoint must still leave a normal
   // cardinal approach to the rear working space and a way out again.
   var alternate=new HashSet<(int x,int y)>(blocked){At(origin,(4,0),rotation)};
   var rear=At(origin,(7,0),rotation);return g.Flood(alternate,Arrival,int.MaxValue,null)[rear.x,rear.y];
  }
  // Store access has two priced alternatives, rather than the other worksites'
  // free two-approach contract. Read-only generation proofs include diagonal
  // movement/reach, while the authored two-step haul also leaves a cardinal path.
  static bool FitsStore(SpreadWildernessSituationBuilder.Geometry g,(int x,int y)[] positions,
   List<(string bp,string role,int x,int y)> specs,(int x,int y)origin,int rotation)
  {
   bool Clear(int x,int y){var p=At(origin,(x,y),rotation);return g.Place(p.x,p.y);}
   for(int y=-2;y<=2;y++)for(int x=-3;x<=3;x++)if(!Clear(x,y))return false;
   for(int y=-1;y<=1;y++)for(int x=-5;x<=-4;x++)if(!Clear(x,y))return false;
   if(!Clear(4,0))return false;
   var blocked=new HashSet<(int x,int y)>(positions);
   var cache=At(origin,(0,0),rotation);var trap=At(origin,(3,0),rotation);var beam=At(origin,(-3,0),rotation);
   var grab=At(origin,(-4,0),rotation);var pull=At(origin,(-5,0),rotation);var aside=At(origin,(-5,-1),rotation);
   var direct=At(origin,(4,0),rotation);var forage=At(origin,(-4,2),rotation);
   var closed=ReachStore(g,blocked,Arrival,true);
   bool Touch(bool[,] reached,(int x,int y)p)
   {for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(g.In(p.x+dx,p.y+dy)&&reached[p.x+dx,p.y+dy])return true;return false;}
   if(Touch(closed,cache)||!closed[grab.x,grab.y]||!closed[pull.x,pull.y]||!closed[aside.x,aside.y]||!closed[direct.x,direct.y])return false;
   if(new[]{(forage.x-1,forage.y),(forage.x+1,forage.y),(forage.x,forage.y-1),(forage.x,forage.y+1)}.Count(p=>g.In(p.Item1,p.Item2)&&closed[p.Item1,p.Item2])<2)return false;
   blocked.Remove(trap);
   if(!Touch(ReachStore(g,blocked,Arrival,false),cache))return false;
   blocked.Add(trap);blocked.Remove(beam);blocked.Add(pull);
   return Touch(ReachStore(g,blocked,aside,false),cache);
  }
  static bool[,] ReachStore(SpreadWildernessSituationBuilder.Geometry g,HashSet<(int x,int y)> blocked,(int x,int y)start,bool diagonals)
  {
   var reached=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();
   void Add(int x,int y){if(!g.Walk(x,y,blocked)||reached[x,y])return;reached[x,y]=true;queue.Enqueue((x,y));}
   Add(start.x,start.y);
   while(queue.Count>0)
   {
    var p=queue.Dequeue();
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if((dx!=0||dy!=0)&&(diagonals||dx==0||dy==0))Add(p.x+dx,p.y+dy);
   }
   return reached;
  }
 }
}
