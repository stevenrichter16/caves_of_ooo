using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Core/native save and planner fixture; not an ordinary player journey.
    public sealed class SpreadExplorationManifestTests
    {
        const string Key = "SpreadExploration.Manifest";
        DensityLootTestScope scope;
        IDictionary loot; DictionaryEntry[] oldLoot; object oldInitialized;
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        [SetUp] public void Setup()
        {
            loot = (IDictionary)typeof(LootTableRegistry).GetField("_byName", All).GetValue(null);
            var entries = new List<DictionaryEntry>(); foreach (DictionaryEntry e in loot) entries.Add(e);
            oldLoot = entries.ToArray(); oldInitialized = typeof(LootTableRegistry).GetField("_initialized", All).GetValue(null);
            scope = new DensityLootTestScope();
        }
        [TearDown] public void Cleanup()
        {
            try { scope?.Dispose(); }
            finally { if (loot != null && oldLoot != null) { loot.Clear(); foreach (var e in oldLoot) loot.Add(e.Key,e.Value); typeof(LootTableRegistry).GetField("_initialized",All).SetValue(null,oldInitialized); } }
        }
        static Type PlanType()
        { var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationPlan"); Assert.NotNull(t,"A bounded exploration manifest is missing."); return t; }
        static object Invoke(MethodInfo m, object owner, params object[] args)
        { Assert.NotNull(m,"Missing exploration API."); try { return m.Invoke(owner,args); } catch(TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
        static object Value(object owner,string name)
        { Assert.NotNull(owner); var p=owner.GetType().GetProperty(name,All); Assert.NotNull(p,"Missing "+name); return p.GetValue(owner); }
        static object Plan(OverworldZoneManager m) => Value(m,"Exploration");
        static object Call(object owner,string name,params object[] args)
        { var method=owner.GetType().GetMethods(All).SingleOrDefault(x=>x.Name==name&&x.GetParameters().Length==args.Length); return Invoke(method,owner,args); }
        static object Static(string name, params object[] args)
        { var method=PlanType().GetMethods(All).SingleOrDefault(x=>x.Name==name&&x.GetParameters().Length==args.Length); return Invoke(method,null,args); }
        OverworldZoneManager Fresh(int seed=64)
        { var method=typeof(OverworldZoneManager).GetMethod("CreateDetached",new[]{typeof(EntityFactory),typeof(int),typeof(bool)}); return (OverworldZoneManager)Invoke(method,null,scope.Factory,seed,true); }
        static IEnumerable<object> Entries(object p) => ((IEnumerable)Value(p,"Entries")).Cast<object>();
        static object Find(object p,string id) => Call(p,"Find",id);
        static string ID(object e)=>(string)Value(e,"ZoneID");
        static bool Flag(object e,string name)=>e!=null&&(bool)Value(e,name);
        static string Family(object e)=>Value(e,"Family").ToString();
        static string Signature(object p)=>string.Join("\n",Entries(p).Select(e=>ID(e)+"|"+Value(e,"PlacementEligible")+"|"+Value(e,"PersistenceCovered")+"|"+Value(e,"Family")+"|"+Value(e,"Topology")+"|"+Value(e,"ActorSeed")+"|"+Value(e,"RewardSeed")));
        static void SetPlan(OverworldZoneManager m,object p)=>typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(m,p);
        static string[] Adjacent(string id)
        { var p=WorldMap.FromZoneID(id); return new[]{WorldMap.ToZoneID(p.x-1,p.y),WorldMap.ToZoneID(p.x+1,p.y),WorldMap.ToZoneID(p.x,p.y-1),WorldMap.ToZoneID(p.x,p.y+1)}; }
        static string SourceId(object plan)=>ID(Entries(plan).First(e=>Flag(e,"PlacementEligible")));
        static string Wire(OverworldZoneManager m)
        { var world=(Entity)Static("BindForSave",m,new Entity{BlueprintName="World"}); Assert.True(world.Properties.ContainsKey(Key)); return world.Properties[Key]; }

        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void FreshExplicitManifestIsFiniteColdAndDoesNotConsumeSourceRng(int seed)
        {
            var rng=new Random(714); LoadoutPart.Rng=rng;
            var m=Fresh(seed); var p=Plan(m); Assert.True((bool)Value(p,"Enabled")); Assert.AreEqual(4,Value(p,"Version"));
            var rows=Entries(p).ToArray(); Assert.Greater(rows.Length,0); Assert.LessOrEqual(rows.Length,WorldMap.Width*WorldMap.Height);
            Assert.AreEqual(rows.Length,rows.Select(ID).Distinct().Count()); Assert.AreEqual(0,m.CachedZoneCount);
            Assert.AreEqual(new Random(714).Next(),rng.Next(),"Planner reads may not spend actor loadout draws.");
            foreach(var e in rows) { var at=WorldMap.FromZoneID(ID(e)); Assert.AreEqual(ID(e),WorldMap.ToZoneID(at.x,at.y,0)); Assert.True(Flag(e,"PersistenceCovered")); }
            Assert.LessOrEqual((int)Value(p,"AllocationChecks"),WorldMap.Width*WorldMap.Height*4*3,"At most one initial and two repair family passes.");
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void AssignmentIsFrozenAcrossReverseReadsAndActualGenerationOrder(int seed)
        {
            var a=Fresh(seed); var b=Fresh(seed); string expected=Signature(Plan(a)); Assert.AreEqual(expected,Signature(Plan(b)));
            var ids=Entries(Plan(a)).Where(e=>Flag(e,"PlacementEligible")).Select(ID).ToArray();
            foreach(string id in ids.Reverse()) Assert.AreEqual(id,ID(Find(Plan(a),id)));
            Assert.AreEqual(0,a.CachedZoneCount); foreach(string id in ids.Take(2)) Assert.NotNull(a.GetZone(id));
            foreach(string id in ids.Take(2).Reverse()) Assert.NotNull(b.GetZone(id));
            Assert.AreEqual(expected,Signature(Plan(a))); Assert.AreEqual(expected,Signature(Plan(b)));
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ActiveCatalogIsClosedAndSameNonquietFamilyNeverTouchesAnEdge(int seed)
        {
            var p=Plan(Fresh(seed)); var rows=Entries(p).ToArray(); var allowed=new[]{"None","RoadSpill","OccupiedBank","LastGleanings","WateringMargin","SnakeForage","WorkGang","CollectorReturn","RoadsideExchange"};
            Assert.True(rows.Any(e=>Flag(e,"PlacementEligible")&&Family(e)=="None")); Assert.Greater(rows.Count(e=>Family(e)!="None"),0);
            foreach(var e in rows) { CollectionAssert.Contains(allowed,Family(e)); if(Family(e)=="None")continue;
                foreach(string neighbor in Adjacent(ID(e))) { var other=Find(p,neighbor); if(other!=null)Assert.AreNotEqual(Family(e),Family(other),ID(e)+" / "+neighbor); }
            }
            var enumType=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationFamily"); Assert.NotNull(enumType); CollectionAssert.AreEquivalent(allowed,Enum.GetNames(enumType));
            var topology=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationTopology"); Assert.NotNull(topology); CollectionAssert.AreEquivalent(new[]{"Legacy","OffsetLanes","BrokenEnclosures","BankCrossing"},Enum.GetNames(topology));
        }
        [Test] public void ProtectedOrdinaryGraphsRetainWithoutReceivingNewPlacement()
        {
            var m=Fresh();var p=Plan(m);
            foreach(string id in new[]{ReferenceGladePlan.ZoneID,m.RareEncounters.PairZoneID,m.RareEncounters.ViperZoneID,m.Wayhouse.ZoneID})
            { var e=Find(p,id);Assert.NotNull(e,id);Assert.True(Flag(e,"PersistenceCovered"),id);Assert.False(Flag(e,"PlacementEligible"),id);Assert.AreEqual("None",Family(e)); }
            foreach(string id in OverworldZoneManager.AuthoredWildernessZoneIDs.Concat(RegionalSituations.Definitions.SelectMany(d=>new[]{d.SourceZoneId,d.RecipientZoneId})))Assert.False(Flag(Find(p,id),"PlacementEligible"),id);
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++) if(m.WorldMap.GetPOI(x,y)!=null)Assert.False(Flag(Find(p,WorldMap.ToZoneID(x,y)),"PersistenceCovered"));
        }
        [TestCase("biome")][TestCase("poi")]
        public void CreationUsesActualMapAndFrozenPlanDoesNotMutateWhenMapLaterChanges(string kind)
        {
            var m=Fresh();var before=Plan(m);string id=SourceId(before);string signature=Signature(before);var at=WorldMap.FromZoneID(id);
            if(kind=="biome")m.WorldMap.Tiles[at.x,at.y]=BiomeType.Beating;else m.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.MerchantCamp,"occupied"));
            var after=Static("Create",m);Assert.False(Flag(Find(after,id),"PersistenceCovered"));Assert.AreEqual(signature,Signature(before));
            object[] args={m,id,null};Assert.False((bool)Invoke(before.GetType().GetMethod("TryGetPlacement"),before,args));Assert.AreEqual(0,m.CachedZoneCount);
        }
        [Test] public void NoEligibleCellsProducesValidEmptyManifestWithoutRerollOnLoad()
        {
            var m=Fresh();for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)m.WorldMap.Tiles[x,y]=BiomeType.Beating;
            SetPlan(m,Static("Create",m));Assert.IsEmpty(Entries(Plan(m)));Assert.True((bool)Value(Plan(m),"Enabled"));
            var state=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("empty-manifest","fixture",m,null,null));
            Assert.True((bool)Value(Plan(state.ZoneManager),"Enabled"));Assert.IsEmpty(Entries(Plan(state.ZoneManager)));Assert.AreEqual(0,state.ZoneManager.CachedZoneCount);
        }
        [TestCase(null)][TestCase("")][TestCase("Overworld.01.1.0")][TestCase("Overworld.1.1.1")][TestCase("Overworld.20.1.0")]
        public void InvalidReadDoesNotCreateOrResolveAnAddress(string id)
        {var m=Fresh();Assert.IsNull(Find(Plan(m),id));Assert.AreEqual(0,m.CachedZoneCount);}

        [Test] public void DefaultLegacyWorldStillUnloadsAndDoesNotAcquireManifestOnSave()
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64);var zone=new Zone("Overworld.11.9.0");m.SetActiveZone(zone);
            var state=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("legacy","fixture",m,null,null));
            Assert.False(state.World?.Properties.ContainsKey(Key)==true);
            state.ZoneManager.UnloadZone(zone.ZoneID);Assert.False(state.ZoneManager.CachedZones.ContainsKey(zone.ZoneID));
        }
        [Test] public void MissingLegacyMetadataCannotKeepRestoreConstructorsFreshPlan()
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64);
            var state=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("old-no-manifest","fixture",m,null,null));
            Assert.False((bool)Value(Plan(state.ZoneManager),"Enabled"));Assert.AreEqual(0,state.ZoneManager.CachedZoneCount);
            var saved=(Entity)Static("BindForSave",state.ZoneManager,state.World);Assert.False(saved?.Properties.ContainsKey(Key)==true);
        }
        [TestCase("")][TestCase("999|64|0")][TestCase("2|64|1\nOverworld.999.1.0|3|1|0|0")]
        public void CorruptOrFutureMetadataRejectsWholeCandidateInsteadOfLegacyFallback(string value)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64);var world=new Entity{BlueprintName="World"};world.Properties[Key]=value;
            Assert.Throws<InvalidDataException>(()=>HotbarSaveFixture.RoundTrip(GameSessionState.Capture("bad-manifest","fixture",m,null,null,world:world)));
        }
        [Test] public void OptedInOrdinaryGraphAndConnectionsSurviveExplicitUnloadAfterMutation()
        {
            var m=Fresh();string id=SourceId(Plan(m));var z=m.GetZone(id);Assert.NotNull(z);var entity=z.GetReadOnlyEntities().First(e=>e.HasPart<ContainerPart>());
            var cell=z.GetEntityPosition(entity);Assert.True(z.RemoveEntity(entity));var connection=new ZoneConnection{SourceZoneID=id,SourceX=1,SourceY=1,TargetZoneID="Overworld.0.0.0",TargetX=2,TargetY=2,Type="test"};m.RegisterConnection(connection);
            m.SetActiveZone(z);m.UnloadZone(id);Assert.AreSame(z,m.ActiveZone);Assert.AreSame(z,m.GetZone(id));Assert.IsNull(z.GetEntityCell(entity));Assert.Contains(connection,m.GetConnections(id));Assert.AreNotEqual((-1,-1),cell);
        }
        [Test] public void SavedAwayGraphRetainsExactPartialStockAndDoesNotRefillAfterUnload()
        {
            var m=Fresh();string id=SourceId(Plan(m));var z=m.GetZone(id);var cache=z.GetReadOnlyEntities().First(e=>e.GetPart<ContainerPart>()?.Contents.Count>0);var cp=cache.GetPart<ContainerPart>();
            var taken=cp.Contents[0];Assert.True(cp.RemoveItem(taken));string remaining=string.Join("|",cp.Contents.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1)));var at=z.GetEntityPosition(cache);
            var player=scope.Factory.CreateEntity("Player");var inventory=player.GetPart<InventoryPart>();
            Assert.False(inventory.Objects.Any(e=>e.GetPart<StackerPart>()?.CanStackWith(taken)==true),"This exact-ID source must not merge with the actual starter kit.");
            Assert.True(inventory.AddObject(taken));Assert.True(inventory.Objects.Any(e=>ReferenceEquals(e,taken)));var away=new Zone("Overworld.0.0.0");Assert.True(away.AddEntity(player,1,1));m.SetActiveZone(away);
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("manifest-partial","fixture",m,null,player));var restored=loaded.ZoneManager.GetZone(id);Assert.AreNotSame(z,restored);
            var current=restored.GetReadOnlyEntities().Single(e=>e.ID==cache.ID);Assert.AreEqual(at,restored.GetEntityPosition(current));Assert.AreEqual(remaining,string.Join("|",current.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1))));
            Assert.True(loaded.Player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==taken.ID));loaded.ZoneManager.UnloadZone(id);Assert.AreSame(restored,loaded.ZoneManager.GetZone(id));
        }
        [TestCase("removed")][TestCase("same-id-clone")]
        public void LostInstalledGraphCannotRegenerateOrSerializeAsAReplacement(string mode)
        {
            var m=Fresh();string id=SourceId(Plan(m));var original=m.GetZone(id);Assert.NotNull(original);
            if(mode=="removed")m.CachedZones.Remove(id);else m.CachedZones[id]=new Zone(id);
            Assert.Throws<InvalidDataException>(()=>m.GetZone(id));Assert.Throws<InvalidDataException>(()=>Static("BindForSave",m,new Entity()));
        }
        [Test] public void AnUncommittedSameIdGraphCannotGainRetentionByBeingAttached()
        {
            var m=Fresh();string id=SourceId(Plan(m));var staged=new Zone(id);m.SetActiveZone(staged);m.UnloadZone(id);Assert.False(m.CachedZones.ContainsKey(id));
            Assert.NotNull(m.GetZone(id));Assert.AreNotSame(staged,m.GetZone(id));
        }
        [Test] public void CompletedGraphRemainsRetainedAfterItsCurrentMapEligibilityChanges()
        {
            var m=Fresh();string id=SourceId(Plan(m));var z=m.GetZone(id);var at=WorldMap.FromZoneID(id);m.WorldMap.Tiles[at.x,at.y]=BiomeType.Beating;
            m.UnloadZone(id);Assert.AreSame(z,m.GetZone(id));var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("changed-map","fixture",m,null,null));var restored=loaded.ZoneManager.GetZone(id);loaded.ZoneManager.UnloadZone(id);Assert.AreSame(restored,loaded.ZoneManager.GetZone(id));
        }
    }
}
