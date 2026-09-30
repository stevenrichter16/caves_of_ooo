using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
namespace CavesOfOoo.Core
{
 /// <summary>Three cold-generation worksites. Stock is moved with its exact existing contents;
 /// only a complete two-owner ordinary hostile roll can become a mixed pair. No saved replay.</summary>
 public static class SpreadExplorationWorksites
 {
  /// <summary>Presentation/debug identity on new site owners; never authority to acquire a source.</summary>
  public const string RoleKey="SpreadWorksite.Role";
  static readonly (int x,int y) Arrival=(40,12);
  const int MaxTrials=256;
  public static bool TryPlace(Zone zone,EntityFactory factory,SpreadCompositionBuilder terrain,
   PopulationBuilder population,ContainerBuilder containers,SpreadExplorationFamily family,Func<bool> authority,
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
   bool pair=!alembic&&actors.Owners.Count==2;
   var replaced=pair?actors.Owners.ToArray():Array.Empty<Entity>();var selected=new HashSet<Entity>(replaced);if(cache!=null)selected.Add(cache);
   string caster=forge?"MarlbackCindercaller":"MarlbackSoursprayer";
   var specs=new List<(string bp,string role,int x,int y)>();
   if(alembic){specs.Add(("AlchemyStill","still",0,0));specs.Add(("StoneburrPatch","binding-forage",-2,1));specs.Add(("FrostLichenPatch","cold-forage",2,1));}
   else if(forge){specs.Add(("TinkersForge","forge",0,0));specs.Add(("StoneburrPatch","binding-forage",2,1));}
   else{specs.Add(("FrostLichenPatch","cold-forage",-2,1));specs.Add(("SpikeTrap","trap",0,2));}
   foreach(var p in new[]{(-2,-2),(-1,-2),(2,-2)})specs.Add(("StoneWall","broken-wall",p.Item1,p.Item2));
   if(pair){specs.Add((caster,"ranged",2,-1));specs.Add(("MarlbackScrabbler","melee",-1,1));}
   if(specs.Any(s=>!factory.Blueprints.ContainsKey(s.bp))||(pair&&LoadoutPart.Factory!=factory))return false;
   var all=new HashSet<Entity>(zone.GetReadOnlyEntities());var initial=SpreadGenerationReceipt.CaptureFinalState(zone,all);
   var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,all.Where(e=>!selected.Contains(e)));
   var before=new SpreadWildernessSituationBuilder.Geometry(zone,selected);
   var anchor=zone.GetEntityPosition(cache??actors.Owners[0]);
   var offsets=specs.Select(s=>(s.x,s.y)).Concat(cache==null?Array.Empty<(int,int)>():new[]{(0,0)}).ToArray();
   var positions=Array.Empty<(int x,int y)>();(int x,int y) origin=default;int rotation=0,trials=0;
   foreach(var at in from y in Enumerable.Range(4,Zone.Height-8) from x in Enumerable.Range(4,Zone.Width-8) orderby Distance(anchor,(x,y)),y,x select(x,y))
   {
    for(int turn=0;turn<2;turn++)
    {
     var candidate=offsets.Select(p=>At(at,p,turn)).ToArray();
     if(candidate.Any(p=>Distance(p,Arrival)<=6||!before.Place(p.x,p.y)))continue;
     if(++trials>MaxTrials)return false;
     if(!Fits(zone,before,before,candidate,specs,pair,store,at,turn,plan))continue;
     positions=candidate;origin=at;rotation=turn;break;
    }
    if(positions.Length>0)break;
   }
   if(positions.Length==0||!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities()))return false;
   var staged=new List<Entity>();var added=new HashSet<Entity>();var removed=new HashSet<Entity>();
   var oldPositions=selected.ToDictionary(e=>e,zone.GetEntityPosition);
   bool Provenance()=>Identity();
   bool Others()=>unchanged()&&all.Except(removed).Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
   bool Owned()=>Provenance()&&actors.MatchesOwnedState(removed)&&(!store||stock.MatchesOwnedState(removed));
   // Factories execute callbacks. Every creation must leave the original packet untouched.
   try
   {
    foreach(var spec in specs)
    {
     var e=factory.CreateEntity(spec.bp);
     if(!Fresh(e,spec.bp)||!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities()))return false;
     e.Properties[RoleKey]=spec.role;
     if(spec.role=="melee")
     {var tactics=e.GetPart<CombatTacticsPart>();if(tactics==null){tactics=new CombatTacticsPart{SkillClasses="",AbilityChance=0};e.AddPart(tactics);}tactics.AssistAllies=true;tactics.AssistRadius=6;}
     if(spec.role=="ranged"&&(!e.HasTag("Creature")||e.GetPart<BrainPart>()==null||e.GetPart<BrainPart>().SightRadius<1||e.GetPart<BrainPart>().SightRadius>6||e.GetPart<CombatTacticsPart>()?.AssistAllies!=true
      ||e.GetPart<SkillsPart>()?.HasSkill(forge?"Pyromancy_EmberSpit":"Corrosion_AcidSpray")!=true||e.GetPart<ActivatedAbilitiesPart>()==null))return false;
     if((spec.role=="still"&&!e.HasPart<AlchemyStillPart>())||(spec.role=="forge"&&!e.HasPart<ForgePart>())||(spec.role=="trap"&&!e.HasPart<SpikeTrapTriggerPart>())
      ||(spec.role=="broken-wall"&&e.GetPart<PhysicsPart>()?.Solid!=true)||((spec.role=="binding-forage"||spec.role=="cold-forage")&&!e.HasPart<HarvestablePart>()))return false;
     if(e.HasPart<HarvestablePart>()){var h=e.GetPart<HarvestablePart>();if(h.Harvested||h.YieldChance!=100||h.YieldMin<1||h.YieldMax<h.YieldMin||h.YieldMax>2||!factory.Blueprints.ContainsKey(h.YieldBlueprint))return false;}
     string cue=spec.role=="still"?"The field alembic still works. Single flasks can be brewed in the field; this still also prepares batches. Stoneburr carries the binding used in stoneskin brews; frost lichen carries cold for a freezing coating."
      :spec.role=="forge"?"The roadside forge still works. Carried weapon components can be forged or used to re-forge a weapon. A brewed coating can quench a weapon here; frost lichen supplies the cold for freezing coatings."
      :spec.role=="trap"?"Exposed spike teeth cross the straight approach to the store. An open gap leads around them. Anything stepping on the teeth springs this trap once."
      :spec.role=="binding-forage"?"A small, finite stand of stoneburr. Gather its seeds and brew one flask anywhere for a stoneskin tonic. A still can prepare batches."
      :spec.role=="cold-forage"?"A small, finite stand of frost lichen. Brew its cold into a freezing coating, then quench a melee weapon beside a forge. Drinking the coating freezes the drinker.":null;
     if(cue!=null){var examine=e.GetPart<ExaminablePart>();if(examine==null){examine=new ExaminablePart();e.AddPart(examine);}examine.Text=cue;}
     staged.Add(e);
    }
   }
   catch(Exception){return false;}
   if(!UniqueGraph(zone,staged)||!authority()||!Sources()||!initial()||!all.SetEquals(zone.GetReadOnlyEntities()))return false;
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
     if(!authority()||!Owned()||!Others()||!AddedState()||!new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>()).Place(at.x,at.y))return false;
     if(!zone.AddEntity(e,at.x,at.y))return false;
     if(e==cache)removed.Remove(e);else added.Add(e);
     placedProof[e]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
     if(!authority()||!Owned()||!Others()||!AddedState())return false;
    }
    var finalPacket=SpreadGenerationReceipt.CaptureFinalState(zone,packet);
    bool Final()=>authority()&&Provenance()&&actors.MatchesOwnedState(new HashSet<Entity>(replaced))
     &&(!store||stock.MatchesOwnedState())&&finalPacket()&&detached.Values.All(p=>p())
     &&Fits(zone,new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>(packet)),before,positions,specs,pair,store,origin,rotation,plan);
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
  static (int x,int y) At((int x,int y)at,(int x,int y)p,int turn)=>turn==0?(at.x+p.x,at.y+p.y):(at.x-p.y,at.y+p.x);
  static bool Fits(Zone z,SpreadWildernessSituationBuilder.Geometry g,SpreadWildernessSituationBuilder.Geometry before,(int x,int y)[] positions,
   List<(string bp,string role,int x,int y)> specs,bool pair,bool store,(int x,int y)origin,int rotation,SpreadCompositionPlan plan)
  {
   if(positions.Any(p=>Distance(p,Arrival)<=6||!g.Place(p.x,p.y))||!g.PreservesAgainst(before,positions))return false;
   var blocked=new HashSet<(int x,int y)>(positions);
   var threat=specs.Select((s,i)=>(s.role,at:positions[i])).Where(s=>s.role=="ranged"||s.role=="melee").ToArray();
   bool Avoid(int x,int y)=>threat.Any(t=>Distance((x,y),t.at)<=(t.role=="ranged"?6:10));
   var safe=g.Flood(blocked,Arrival,int.MaxValue,Avoid);
   var reachable=g.Flood(blocked,Arrival,int.MaxValue,null);
   if(!safe[Arrival.x,Arrival.y])return false;
   foreach(var p in new[]{(x:0,y:plan.WestY),(x:Zone.Width-1,y:plan.EastY),(x:plan.NorthX,y:0),(x:plan.SouthX,y:Zone.Height-1)})
    if(before.BorderReach[p.x,p.y]&&!safe[p.x,p.y])return false;
   // Every functional owner retains two physical approaches; neither harvesting
   // nor opening the actual cache forces the player onto the visible trap.
   for(int i=0;i<positions.Length;i++)
   {
    if(i<specs.Count&&(specs[i].role=="broken-wall"||specs[i].role=="melee"||specs[i].role=="ranged"||specs[i].role=="trap"))continue;
    var p=positions[i];if(new[]{(p.x-1,p.y),(p.x+1,p.y),(p.x,p.y-1),(p.x,p.y+1)}.Count(n=>g.Walk(n.Item1,n.Item2,blocked)&&reachable[n.Item1,n.Item2])<2)return false;
   }
   if(store)
   {
    var near=At(origin,(0,1),rotation);var far=At(origin,(0,3),rotation);
    if(!g.Walk(near.x,near.y,blocked)||!g.Walk(far.x,far.y,blocked)||!reachable[near.x,near.y]||!reachable[far.x,far.y]||!g.Flood(blocked,far,6,null)[near.x,near.y])return false;
   }
   return true;
  }
 }
}
