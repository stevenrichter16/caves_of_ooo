using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
 /// <summary>One optional new-world situation from exact ordinary sources. This
 /// runs only in the captured cold attempt; graph attachment never rebuilds it.</summary>
 public sealed class SpreadExplorationBuilder:IZoneBuilder
 {
  public string Name=>"SpreadExploration";public int Priority{get;}
  public string LastResult{get;private set;}="";
  readonly OverworldZoneManager manager;readonly SpreadExplorationPlan plan;
  readonly SpreadCompositionBuilder terrain;readonly PopulationBuilder population;readonly ContainerBuilder containers;readonly HaulablePropBuilder haul;
  const int MaxTrials=256;
  public SpreadExplorationBuilder(OverworldZoneManager manager,SpreadCompositionBuilder terrain,PopulationBuilder population,ContainerBuilder containers,HaulablePropBuilder haul=null,bool earlyHunt=false)
  {
   Priority=earlyHunt?4001:4300;
   this.manager=manager;plan=manager?.Exploration;this.terrain=terrain;this.population=population;this.containers=containers;this.haul=haul;
   if(population!=null){population.CaptureSourceReceipts=true;population.ExplorationManager=manager;}
   if(containers!=null)containers.CaptureSourceReceipts=true;
  }
  bool Current(Zone z,EntityFactory f,SpreadExplorationEntry entry)=>z!=null&&ReferenceEquals(f,manager?.Factory)
   &&plan!=null&&plan.TryGetGenerationEntry(manager,z,out var now)&&ReferenceEquals(now,entry)
   &&terrain?.SourceZone==z&&terrain.Plan?.ZoneID==z.ZoneID&&terrain.Plan.Seed==manager.WorldSeed&&terrain.Plan.Topology==entry.Topology;
  public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
  {
   LastResult="";
   if(plan==null||!plan.TryGetGenerationEntry(manager,zone,out var entry)||entry.Family==SpreadExplorationFamily.None)return true;
   if(!Current(zone,factory,entry))return Refuse(zone,entry,"authority");
   switch(entry.Family)
   {
    case SpreadExplorationFamily.RoadSpill:return Road(zone,factory,entry);
    case SpreadExplorationFamily.OccupiedBank:return Actor(zone,factory,entry,false);
    case SpreadExplorationFamily.LastGleanings:return Actor(zone,factory,entry,true);
    case SpreadExplorationFamily.WateringMargin:return Water(zone,factory,entry);
    case SpreadExplorationFamily.SnakeForage:return Encounter(zone,factory,entry,true);
    case SpreadExplorationFamily.WorkGang:return Encounter(zone,factory,entry,false);
    case SpreadExplorationFamily.CollectorReturn:return Collector(zone,factory,entry);
    case SpreadExplorationFamily.FieldPassage:return Passage(zone,factory,entry);
    case SpreadExplorationFamily.CoolingWorkPatch:return Cooking(zone,factory,entry);
    case SpreadExplorationFamily.HeavySalvage:return Hauling(zone,factory,entry);
    case SpreadExplorationFamily.HuntThroughCover:return Hunt(zone,factory,entry);
    default:return Refuse(zone,entry,"unsupported-family");
   }
  }
  bool Hunt(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(Priority!=4001||terrain.Plan.Formation!=Formation.Fallow||!terrain.CaptureCoverSources)return Refuse(z,entry,"hunt-source-boundary");
   var owners=new HashSet<Entity>(z.GetReadOnlyEntities());
   var original=SpreadGenerationReceipt.CaptureFinalState(z,owners.Where(e=>!DoorPart.IsBareGround(e)));
   bool covered=SpreadExplorationPlan.Rank(manager.WorldSeed,z.ZoneID,"hunt-variant")%2==0;
   if(SpreadExplorationHunt.TryPlace(z,f,terrain,population,covered,()=>Current(z,f,entry),out var hunter,out var grazer,out var final))
    return Commit(z,f,entry,new[]{hunter,grazer},final);
   if(!Current(z,f,entry)||!original()||!owners.SetEquals(z.GetReadOnlyEntities()))return false;
   return Refuse(z,entry,"no-original-pair-or-useful-hunt-layout");
  }
  bool Hauling(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(terrain.Plan.Formation!=Formation.Hedgerow||!terrain.CapturePassageSources||haul?.SourceReceipt==null)return Refuse(z,entry,"no-ordinary-haul-source");
   var owners=new HashSet<Entity>(z.GetReadOnlyEntities());
   var original=SpreadGenerationReceipt.CaptureFinalState(z,z.GetReadOnlyEntities().Where(e=>!DoorPart.IsBareGround(e)));
   if(SpreadExplorationHauling.TryPlace(z,terrain,haul,()=>Current(z,f,entry),out var load,out var final))
    return Commit(z,f,entry,new[]{load},final);
   if(!Current(z,f,entry)||!original()||!owners.SetEquals(z.GetReadOnlyEntities()))return false;
   return Refuse(z,entry,"no-useful-heavy-salvage-layout");
  }
  bool Cooking(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(terrain.Plan.Formation!=Formation.FieldStrips||!terrain.CaptureCookingSources)return Refuse(z,entry,"cooking-source");
   var candidates=terrain.CookingSources.Where(r=>r.IsCurrent&&r.Owners.Count==1&&SpreadExplorationCooking.Eligible(z,r.Owners[0]))
    .OrderBy(r=>z.GetEntityPosition(r.Owners[0]).y).ThenBy(r=>z.GetEntityPosition(r.Owners[0]).x).Take(32).ToArray();
   foreach(var receipt in candidates)
   {
    var original=SpreadGenerationReceipt.CaptureFinalState(z,z.GetReadOnlyEntities().Where(e=>!DoorPart.IsBareGround(e)));
    bool preparing=true;
    bool Authority()=>Current(z,f,entry)&&terrain.OwnsCookingReceipt(receipt)&&(!preparing||original());
    if(SpreadExplorationCooking.TryPlace(z,f,terrain,receipt.Owners[0],Authority,out var source,out var final))
     {preparing=false;return Commit(z,f,entry,new[]{source},()=>Authority()&&final());}
    if(!Current(z,f,entry)||!original())return false;
   }
   return Refuse(z,entry,"no-useful-cooling-work-patch");
  }
  bool Passage(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(terrain.Plan.Formation!=Formation.Hedgerow||!terrain.CapturePassageSources)return Refuse(z,entry,"passage-source");
   // Eligibility precedes the source cap; edge/owned/spent scenery cannot starve
   // a valid interior source. There are at most two orientations per candidate.
   var candidates=terrain.PassageSources.Where(r=>r.IsCurrent&&r.Owners.Count==1&&SpreadExplorationPassage.Eligible(z,r.Owners[0]))
    .OrderBy(r=>z.GetEntityPosition(r.Owners[0]).y).ThenBy(r=>z.GetEntityPosition(r.Owners[0]).x).Take(32).ToArray();
   foreach(var receipt in candidates)
   {
    var hedge=receipt.Owners[0];
    var all=z.GetReadOnlyEntities().Where(e=>e==hedge||!DoorPart.IsBareGround(e)).ToArray();
    var original=SpreadGenerationReceipt.CaptureFinalState(z,all);
    var others=SpreadGenerationReceipt.CaptureFinalState(z,all.Where(e=>e!=hedge));
    bool preparing=true;
    // Our factory/substitution cannot change any existing packet. Later normal
    // OnZoneGenerated work (including resident naming) is independently owned;
    // final passage authority covers the gate, supports and useful geometry.
    bool Authority()=>Current(z,f,entry)&&terrain.OwnsPassageReceipt(receipt)&&(!preparing||others());
    if(SpreadExplorationPassage.TryPlace(z,f,terrain,hedge,Authority,out var gate,out var final))
     {preparing=false;return Commit(z,f,entry,new[]{gate},()=>Authority()&&final());}
    if(!Current(z,f,entry)||!original())return false;
   }
   return Refuse(z,entry,"no-useful-field-passage");
  }
  bool Collector(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   var replacement=population?.AmbientReplacementReceipt;var ambient=population?.AmbientSourceReceipt;
   var loose=population?.LooseSourceReceipt;var homes=containers?.SourceReceipt;
   if(!Source(replacement,z,f)||!Source(loose,z,f)||!Source(homes,z,f))return Refuse(z,entry,"collector-source");
   if(replacement.Owners.Count!=1||replacement.Owners[0].BlueprintName!="Tatterjay")return Refuse(z,entry,"no-exact-collector-allowance");
   var actor=replacement.Owners[0];
   if(ambient==null||ambient.Zone!=z||!ReferenceEquals(ambient.Factory,f)||ambient.Revision!=replacement.Revision
     ||ambient.Owners.Count(e=>e==actor)!=1)return Refuse(z,entry,"collector-ambient-source");
   var items=loose.Owners.Where(e=>LooseOwner(z,e)&&(e.BlueprintName=="Hatchet"||e.BlueprintName=="Cudgel"||e.BlueprintName=="LeatherBoots"))
     .OrderBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x).Take(32).ToArray();
   var caches=homes.Owners.Where(e=>CacheOwner(z,e)).OrderBy(e=>Math.Abs(z.GetEntityPosition(e).y-Zone.Height/2))
     .ThenBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x).Take(32).ToArray();
   if(items.Length==0||caches.Length==0)return Refuse(z,entry,"no-rolled-collector-goods-or-home");
   // Capture at4300, after ordinary Magpie stocking. The original whole ambient
   // receipt remains stale; only the exact replacement authorizes this actor.
   var original=SpreadGenerationReceipt.CaptureFinalState(z,ambient.Owners.Concat(loose.Owners).Concat(homes.Owners));
   bool SourceAuthority()=>Current(z,f,entry)&&population.CaptureSourceReceipts&&containers.CaptureSourceReceipts
    &&ReferenceEquals(population.AmbientReplacementReceipt,replacement)&&ReferenceEquals(population.AmbientSourceReceipt,ambient)
    &&ReferenceEquals(population.LooseSourceReceipt,loose)&&ReferenceEquals(containers.SourceReceipt,homes);
   if(!SourceAuthority()||!original()||!replacement.TryConsume()||!loose.TryConsume()||!homes.TryConsume())return Refuse(z,entry,"changed-collector-source");
   int trials=0;
   foreach(var home in caches)foreach(var item in items)
   {
    if(++trials>32)return Refuse(z,entry,"collector-source-budget");
    var others=SpreadGenerationReceipt.CaptureFinalState(z,ambient.Owners.Concat(loose.Owners).Concat(homes.Owners).Where(e=>e!=actor&&e!=item));
    bool Authority()=>SourceAuthority()&&others();
    if(SpreadExplorationActorPlacement.TryCollectorReturn(z,actor,home,item,Authority,out var final))
    {
     if(final==null)return false;
     return Commit(z,f,entry,new[]{actor,home,item},()=>Authority()&&final());
    }
    // Honest optional refusal must retain the entire original packet, including
    // positions. A failed callback cannot leave uncommitted source movement.
    if(!SourceAuthority()||!original())return false;
   }
   return Refuse(z,entry,"no-safe-collector-layout");
  }
  bool Encounter(Zone z,EntityFactory f,SpreadExplorationEntry entry,bool snakes)
  {
   var receipt=population?.SourceReceipt;var forage=population?.ForageSourceReceipt;
   if(!Source(receipt,z,f)||(snakes&&!Source(forage,z,f)))return Refuse(z,entry,"encounter-source");
   var actors=receipt.Owners.ToArray();
   if(snakes?(actors.Length<1||actors.Length>2||actors.Any(e=>e.BlueprintName!="Viper"))
    :(actors.Length!=2||actors.Any(e=>e.BlueprintName!="MarlbackScrabbler")))return Refuse(z,entry,"no-exact-encounter-allowance");
   var foods=snakes?forage.Owners.OrderBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x).Take(32).ToArray():Array.Empty<Entity>();
   if(snakes&&foods.Length==0)return Refuse(z,entry,"no-rolled-forage");
   // Food and any non-selected packet owners remain at their original anchors.
   // The helper owns intentional actor movement and its assist-only part.
   var foodUnchanged=SpreadGenerationReceipt.CaptureFinalState(z,snakes?forage.Owners:Array.Empty<Entity>());
   bool Authority()=>Current(z,f,entry)&&population.CaptureSourceReceipts&&ReferenceEquals(population.SourceReceipt,receipt)
    &&(!snakes||ReferenceEquals(population.ForageSourceReceipt,forage))&&foodUnchanged();
   if(!Authority()||!receipt.TryConsume()||(snakes&&!forage.TryConsume()))return Refuse(z,entry,"changed-encounter-source");
   Func<bool> final=null;bool placed=false;
   if(snakes)
   {
    foreach(var food in foods)
    {
     placed=SpreadExplorationActorPlacement.TrySnakeForage(z,actors,food,Authority,out final);
     if(placed)break;
     if(!Authority()||!receipt.MatchesOwnedState()||!forage.MatchesOwnedState())return false;
    }
   }
   else placed=SpreadExplorationActorPlacement.TryWorkGang(z,actors,(entry.ActorSeed&1)!=0,Authority,out final);
   if(!placed)
   {
    if(!Authority()||!receipt.MatchesOwnedState()||(snakes&&!forage.MatchesOwnedState()))return false;
    return Refuse(z,entry,"no-safe-encounter-layout");
   }
   if(final==null)return false;
   return Commit(z,f,entry,actors,()=>Authority()&&final());
  }
  bool Actor(Zone z,EntityFactory f,SpreadExplorationEntry entry,bool grazer)
  {
   var receipt=grazer?population?.AmbientReplacementReceipt:population?.SourceReceipt;
   var ambient=grazer?population?.AmbientSourceReceipt:null;
   if(!Source(receipt,z,f))return Refuse(z,entry,"source");
   var candidates=receipt.Owners.Where(e=>e.BlueprintName==(grazer?"ReedbackGrazer":"MarlbackScrabbler")).ToArray();
   if(candidates.Length!=1||(!grazer&&receipt.Owners.Count!=1))return Refuse(z,entry,"no-exact-role-allowance");
   Entity actor=candidates[0];
   if(grazer&&(receipt.Owners.Count!=1||ambient==null||ambient.Zone!=z||!ReferenceEquals(ambient.Factory,f)
    ||ambient.Revision!=receipt.Revision||ambient.Owners.Count(e=>e==actor)!=1))return Refuse(z,entry,"ambient-source");
   // Ordinary stocking runs after Population. Preserve the other current owners
   // and their completed stock, without refreshing the stale population receipt.
   var othersUnchanged=SpreadGenerationReceipt.CaptureFinalState(z,(grazer?ambient.Owners:receipt.Owners).Where(e=>e!=actor));
   bool Authority()=>Current(z,f,entry)&&othersUnchanged()&&population.CaptureSourceReceipts
    &&ReferenceEquals(receipt,grazer?population.AmbientReplacementReceipt:population.SourceReceipt)
    &&(!grazer||ReferenceEquals(ambient,population.AmbientSourceReceipt));
   if(!Authority()||!receipt.TryConsume())return Refuse(z,entry,"changed-source");
   bool placed=grazer?SpreadExplorationActorPlacement.TryGleanings(z,actor,Authority):SpreadExplorationActorPlacement.TryTerritory(z,actor,Authority);
   if(!placed)
   {
    // A callback may have changed stock/ownership; never accept a partially altered packet as an optional refusal.
    if(!Authority()||!receipt.MatchesOwnedState())return false;
    return Refuse(z,entry,"no-safe-role-layout");
   }
   var sources=grazer?new[]{actor,actor.GetPart<SpreadGrazerPart>().Food,actor.GetPart<SpreadGrazerPart>().ReservedRow}
    :new[]{actor,actor.GetPart<SpreadTerritoryPart>().Post};
   var finalLayout=SpreadExplorationActorPlacement.CaptureFinalGeometry(z,actor);
   if(finalLayout==null)return false;
   return Commit(z,f,entry,sources,()=>Authority()&&finalLayout());
  }
  bool Road(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   var loose=population?.LooseSourceReceipt;var stock=containers?.SourceReceipt;
   var candidates=new List<(Entity owner,SpreadGenerationReceipt receipt)>();
   void Loose(){if(Source(loose,z,f))foreach(var e in loose.Owners)if(LooseOwner(z,e))candidates.Add((e,loose));}
   void Caches(){if(Source(stock,z,f))foreach(var e in stock.Owners)if(CacheOwner(z,e))candidates.Add((e,stock));}
   if((entry.RewardSeed&1)==0){Loose();Caches();}else{Caches();Loose();}
   int trials=0;
   foreach(var source in candidates)
   {
    var owner=source.owner;var receipt=source.receipt;var origin=z.GetEntityPosition(owner);
    var othersUnchanged=SpreadGenerationReceipt.CaptureFinalState(z,receipt.Owners.Where(e=>e!=owner));
    var geometry=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{owner});
    for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
    {
     if(!geometry.Place(x,y)||origin==(x,y))continue;
     if(++trials>MaxTrials)return Refuse(z,entry,"layout-budget");
     var destination=(x,y);
     if(!geometry.PreservesRoutes(new[]{destination})||!SpreadWildernessSituationBuilder.Cargo(z,geometry,destination))continue;
     if(!Current(z,f,entry)||!Source(receipt,z,f)||!receipt.TryConsume())return Refuse(z,entry,"changed-source");
     bool moved=false,committed=false;
     try
     {
      moved=z.MoveEntity(owner,x,y);if(!moved)return Refuse(z,entry,"move-refused");
      if(!Current(z,f,entry)||!receipt.MatchesOwnedState()||z.GetEntityPosition(owner)!=destination)return false;
      var finalGeometry=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{owner});
      if(!finalGeometry.Place(x,y)||!finalGeometry.PreservesAgainst(geometry,new[]{destination})||!SpreadWildernessSituationBuilder.Cargo(z,finalGeometry,destination))return false;
      bool FinalLayout()
      {
       var current=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{owner});
       return current.Place(destination.x,destination.y)&&current.PreservesAgainst(geometry,new[]{destination})
        &&SpreadWildernessSituationBuilder.Cargo(z,current,destination);
      }
      committed=Commit(z,f,entry,new[]{owner},()=>receipt.MatchesOwnedState()&&othersUnchanged()&&FinalLayout());return committed;
     }
     finally{if(!committed&&moved&&owner.SpatialZone==z&&z.GetEntityPosition(owner)==destination&&z.CanPlaceFootprint(owner,origin.x,origin.y))z.MoveEntity(owner,origin.x,origin.y);}
    }
   }
   return Refuse(z,entry,"no-rolled-road-source");
  }
  bool Water(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(terrain.Plan.Formation!=Formation.RiverMeadow)return Refuse(z,entry,"no-bank-source");
   // Reuse an exact already-produced finite source without renaming, moving or
   // refilling it. A spent compatible source is aftermath, not a new allowance.
   foreach(var existing in z.GetReadOnlyEntities().Where(e=>FiniteWater(z,e,f))
     .OrderBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x))
   {
    var at=z.GetEntityPosition(existing);
    var original=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{existing});
    if(!WaterLayout(z,existing,at,original))continue;
    if(existing.GetPart<LiquidPoolPart>().Volume==0)return Refuse(z,entry,"existing-source-spent");
    return Commit(z,f,entry,new[]{existing},()=>FiniteWater(z,existing,f)&&WaterLayout(z,existing,at,original));
   }
   if(!f.Blueprints.ContainsKey("SpreadDrawPoint")
     ||!f.Blueprints.TryGetValue("Waterskin",out var vessel)||!vessel.Parts.TryGetValue("Waterskin",out var skin)
     ||!skin.TryGetValue("Capacity",out string encodedCapacity)||!int.TryParse(encodedCapacity,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int budget)||budget<=0)return Refuse(z,entry,"no-bank-source");
   var geometry=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>());
   (int x,int y)? chosen=null;int trials=0;
   for(int y=3;y<Zone.Height-3&&!chosen.HasValue;y++)for(int x=3;x<Zone.Width-3;x++)
   {
    if(!geometry.Place(x,y)||!geometry.BorderReach[x,y]||!BankWater(z,x,y))continue;
    if(++trials>MaxTrials)return Refuse(z,entry,"layout-budget");
    if(!geometry.PreservesRoutes(new[]{(x,y)}))continue;chosen=(x,y);break;
   }
   if(!chosen.HasValue)return Refuse(z,entry,"no-dry-bank-pocket");
   var destination=chosen.Value;var source=f.CreateEntity("SpreadDrawPoint");
   var pool=source?.GetPart<LiquidPoolPart>();var physical=source?.GetPart<PhysicsPart>();
   if(!Current(z,f,entry)||source?.BlueprintName!="SpreadDrawPoint"||pool?.ParentEntity!=source||pool.LiquidId!="water"||pool.Volume!=budget
     ||physical?.ParentEntity!=source||physical.Takeable||physical.Solid||source.SpatialZone!=null
     ||!new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>()).Place(destination.x,destination.y))return Refuse(z,entry,"changed-water-preparation");
   bool committed=false;
   try
   {
    if(!z.AddEntity(source,destination.x,destination.y))return Refuse(z,entry,"water-placement-refused");
    if(!Current(z,f,entry)||source.GetPart<LiquidPoolPart>()!=pool||pool.Volume!=budget||!FiniteWater(z,source,f))return false;
    committed=Commit(z,f,entry,new[]{source},()=>FiniteWater(z,source,f)&&WaterLayout(z,source,destination,geometry));return committed;
   }
   finally{if(!committed&&source.SpatialZone==z&&z.GetEntityPosition(source)==destination)z.RemoveEntity(source);}
  }
  bool BankWater(Zone z,int x,int y)
  {
   for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
    if(Math.Abs(dx)+Math.Abs(dy)==1&&terrain.Plan.IsWater(x+dx,y+dy)&&z.TileState.HasCoating(x+dx,y+dy,"water"))return true;
   return false;
  }
  bool WaterLayout(Zone z,Entity source,(int x,int y) at,SpreadWildernessSituationBuilder.Geometry original)
  {
   if(z.GetEntityPosition(source)!=at||!BankWater(z,at.x,at.y))return false;
   var cell=z.GetCell(at.x,at.y);var state=z.TileState.Get(at.x,at.y);
   // Pool projection wets this anchor, so Geometry.Place cannot admit it.
   // Preserve its other placement rules explicitly and ignore only this pool.
   if(cell==null||cell.IsInterior||z.GenReservedCells.Contains(at)||at.x<2||at.y<2||at.x>=Zone.Width-2||at.y>=Zone.Height-2
     ||cell.Occupants.Any(e=>e!=source&&!DoorPart.IsBareGround(e))
     ||(state!=null&&(state.Heat!=0||state.Cold!=0||state.Charge!=0||!string.IsNullOrEmpty(state.Cloud)||state.Residues.Count!=0
       ||state.Coatings.Any(c=>c.Id!="water"))))return false;
   var current=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{source});
   if(!current.PreservesAgainst(original,new[]{at}))return false;
   // The finite source itself may be wet. Its usable dry approach must remain
   // connected to an actual border; the wet coating never becomes free volume.
   for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
    if(Math.Abs(dx)+Math.Abs(dy)==1&&current.In(at.x+dx,at.y+dy)&&current.BorderReach[at.x+dx,at.y+dy])return true;
   return false;
  }
  static bool FiniteWater(Zone z,Entity e,EntityFactory factory)
  {
   if(!GroundOwner(z,e)||e.GetPart<PhysicsPart>().Takeable||e.GetPart<PhysicsPart>().Solid||e.HasTag("Solid")
     ||e.HasTag("Creature")||e.HasTag("Item")||!e.HasTag("Terrain")||e.HasPart<WellPart>()||e.HasPart<TileStateSourcePart>()
     ||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>())return false;
   var pool=e.GetPart<LiquidPoolPart>();var render=e.GetPart<RenderPart>();var water=LiquidRegistry.Get("water");
   string glyph=water?.Glyph,color=water?.Color;
   // Core cold generation may precede registry initialization. Preserve the
   // factory's authored water appearance then; never initialize a global registry.
   if(water==null)
   {
    if(factory==null||!factory.Blueprints.TryGetValue(e.BlueprintName,out var blueprint)
      ||!blueprint.Parts.TryGetValue("LiquidPool",out var authoredPool)||!authoredPool.TryGetValue("LiquidId",out string liquid)||liquid!="water"
      ||!blueprint.Parts.TryGetValue("Render",out var authoredRender)||!authoredRender.TryGetValue("RenderString",out glyph)
      ||!authoredRender.TryGetValue("ColorString",out color))return false;
   }
   var at=z.GetEntityPosition(e);
   return pool!=null&&pool.ParentEntity==e&&pool.LiquidId=="water"&&pool.Volume>=0
    &&render.Visible&&render.RenderString==glyph&&render.ColorString==color
    &&string.IsNullOrEmpty(render.VisualID)&&string.IsNullOrEmpty(render.VisualVariant)&&string.IsNullOrEmpty(render.GlyphVariants)
    &&LiquidSourceSafety.IsUnmixedCell(z,at.x,at.y,"water");
  }
  bool Commit(Zone z,EntityFactory f,SpreadExplorationEntry entry,IEnumerable<Entity> owners,Func<bool> source)
  {
   var exact=owners.ToArray();var unchanged=SpreadGenerationReceipt.CaptureFinalState(z,exact);
   bool Valid()=>Current(z,f,entry)&&source()&&unchanged();
   if(!plan.TryMarkPlacementCommitted(manager,z,entry,Valid))return false;
   LastResult=entry.Family.ToString();
   if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadExplorationCommitted",payload:new{zone=z.ZoneID,family=LastResult,owners=exact.Select(e=>e.ID).ToArray()});return true;
  }
  static bool Source(SpreadGenerationReceipt receipt,Zone z,EntityFactory f)=>receipt?.IsCurrent==true&&receipt.Zone==z&&ReferenceEquals(receipt.Factory,f);
  static bool GroundOwner(Zone z,Entity e)=>e!=null&&e.SpatialZone==z&&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==e
   &&p.InInventory==null&&p.Equipped==null&&!e.HasPart<SpatialFootprintPart>()&&z.GetOccupiedCells(e).Count==1&&e.GetPart<RenderPart>()?.ParentEntity==e;
  static bool LooseOwner(Zone z,Entity e)=>GroundOwner(z,e)&&e.HasTag("Item")&&e.GetPart<PhysicsPart>().Takeable;
  static bool CacheOwner(Zone z,Entity e)=>GroundOwner(z,e)&&(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&!e.GetPart<PhysicsPart>().Takeable
   &&e.GetPart<ContainerPart>() is ContainerPart c&&c.ParentEntity==e&&!c.IsLocked&&!e.HasPart<DoorPart>();
  bool Refuse(Zone z,SpreadExplorationEntry entry,string reason)
  {LastResult="refused:"+reason;if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadExplorationRefused",payload:new{zone=z?.ZoneID,family=entry.Family.ToString(),reason});return true;}
 }
}
