using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadLatchcoilSourceTests
    {
        const string Child = "SpreadLatchcoil", OptionalKey = "SpreadRareEncounter.ViperSelection.v1";
        DensityLootTestScope scope; OverworldZoneManager manager;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(scope.Factory, 64); }
        [TearDown] public void Cleanup() { LatchcoilMutationPart.Callback = null; scope?.Dispose(); }
        static string Viper(SpreadRareEncounterPlan p) => typeof(SpreadRareEncounterPlan).GetProperty("ViperZoneID")?.GetValue(p) as string ?? "";
        static bool OptionalInitialized(SpreadRareEncounterPlan p) => (bool?)typeof(SpreadRareEncounterPlan).GetProperty("ViperInitialized")?.GetValue(p) ?? false;
        static void SetPlan(OverworldZoneManager m, SpreadRareEncounterPlan p) => typeof(OverworldZoneManager).GetProperty("RareEncounters").SetValue(m, p);
        static int Distance(string a, string b) { var p = WorldMap.FromZoneID(a); var q = WorldMap.FromZoneID(b); return Math.Max(Math.Abs(p.x-q.x), Math.Abs(p.y-q.y)); }
        static Entity Metadata(string pair, string optional)
        { var world = new Entity(); world.Properties[SpreadRareEncounterPlan.PropertyKey] = "1|"+pair; if(optional != null) world.Properties[OptionalKey] = optional; return world; }
        Entity Actor()
        { Assert.True(scope.Factory.Blueprints.ContainsKey(Child), "An explicit Viper child is required."); return scope.Factory.CreateEntity(Child); }
        string Selected()
        { string id = Viper(manager.RareEncounters); Assert.IsNotEmpty(id, "Fresh optional frozen address must be present for this source seed."); return id; }
        Zone Pocket()
        { var z = new Zone(Selected()); Assert.True(z.AddEntity(scope.Factory.CreateEntity("Hedge"),12,6)); return z; }
        static string Graph(Zone z) => z.TileState.ToSaveString()+"|"+string.Join(";",z.GetReadOnlyEntities().Select(e=>e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)).OrderBy(x=>x));

        [Test] public void ChildUsesRealViperStatsBiteHarvestAndDamageSightAmbush()
        {
            var child = Actor(); var parent = scope.Factory.CreateEntity("Viper");
            foreach(string stat in new[]{"Hitpoints","Agility","Speed","XPValue"}) Assert.AreEqual(parent.GetStatValue(stat),child.GetStatValue(stat),stat);
            Assert.AreEqual(8,child.GetStatValue("Hitpoints")); Assert.AreEqual("ViperBite",child.GetProperty("NaturalWeapon"));
            Assert.AreEqual(parent.GetPart<ArmorPart>().AV,child.GetPart<ArmorPart>().AV); Assert.AreEqual(parent.GetPart<ArmorPart>().DV,child.GetPart<ArmorPart>().DV);
            var c=child.GetPart<CorpsePart>(); var p=parent.GetPart<CorpsePart>();
            Assert.AreEqual(p.CorpseBlueprint,c.CorpseBlueprint); Assert.AreEqual("VenomGland",c.HarvestBlueprint);
            Assert.AreEqual(1,c.HarvestMin); Assert.AreEqual(1,c.HarvestMax); Assert.AreEqual(75,c.HarvestChance);
            Assert.AreEqual(3,child.GetPart<BrainPart>().SightRadius); var ambush=child.GetPart<AIAmbushPart>();Assert.NotNull(ambush);
            Assert.True(ambush.WakeOnDamage); Assert.True(ambush.WakeOnHostileInSight); Assert.True(ambush.DormantPushed);
            Assert.AreEqual(0,DensityLootTestScope.Gear(child).Count()); Assert.AreEqual("~",child.GetPart<RenderPart>().RenderString);
        }
        [Test] public void OrdinaryViperKeepsItsExistingUnmodifiedWakeAndRewardContract()
        {
            var p=scope.Factory.CreateEntity("Viper");Assert.AreEqual(8,p.GetStatValue("Hitpoints"));Assert.IsNull(p.GetPart<AIAmbushPart>());
            Assert.AreEqual(10,p.GetPart<BrainPart>().SightRadius);Assert.AreEqual("ViperBite",p.GetProperty("NaturalWeapon"));Assert.AreEqual(75,p.GetPart<CorpsePart>().HarvestChance);
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void OptionalSelectionIsFrozenHedgerowDistinctAndDoesNotGenerateZones(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed); var plan=m.RareEncounters; string id=Viper(plan);
            Assert.IsNotEmpty(id);Assert.True(OptionalInitialized(plan));Assert.True(SpreadRareEncounterPlan.IsEligible(m,id));
            Assert.AreEqual(Formation.Hedgerow,FormationSelector.For(BiomeType.Spread,id));Assert.Greater(Distance(id,plan.PairZoneID),1);
            Assert.True(plan.Selects(m,id));Assert.AreEqual(id,Viper(SpreadRareEncounterPlan.Create(m)));Assert.AreEqual(0,m.CachedZoneCount);
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void PairSelectionAndItsSavedValueRemainExactlyTheLegacyContract(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed);uint best=uint.MaxValue;string expected="";
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {string id=WorldMap.ToZoneID(x,y,0);if(!SpreadRareEncounterPlan.IsEligible(m,id))continue;var f=FormationSelector.For(BiomeType.Spread,id);if(f!=Formation.Hedgerow&&f!=Formation.OldRoad)continue;
                uint h;unchecked{h=2166136261^(uint)seed;foreach(char c in "SpreadRare.v1.pair|"+id)h=(h^c)*16777619;}if(h<best){best=h;expected=id;}}
            Assert.AreEqual(expected,m.RareEncounters.PairZoneID);Assert.AreEqual("1|"+expected,SpreadRareEncounterPlan.BindForSave(m,null).Properties[SpreadRareEncounterPlan.PropertyKey]);
        }
        [Test] public void OldSavedPairWithoutOptionalKeyDoesNotBackfillOrDisableThePair()
        {
            var restored=SpreadRareEncounterPlan.Restore(Metadata(manager.RareEncounters.PairZoneID,null));
            Assert.True(restored.Initialized);Assert.False(OptionalInitialized(restored));Assert.IsEmpty(Viper(restored));Assert.AreEqual(manager.RareEncounters.PairZoneID,restored.PairZoneID);
            SetPlan(manager,restored);Assert.False(SpreadRareEncounterPlan.BindForSave(manager,null).Properties.ContainsKey(OptionalKey));
        }
        [TestCase("")][TestCase("Overworld.2.5.0")]
        public void CanonicalOptionalMetadataRoundTripsFrozenEvenIfCurrentlyIneligible(string id)
        {
            var restored=SpreadRareEncounterPlan.Restore(Metadata(manager.RareEncounters.PairZoneID,"1|"+id));Assert.True(OptionalInitialized(restored));Assert.AreEqual(id,Viper(restored));
            SetPlan(manager,restored);Assert.AreEqual("1|"+id,SpreadRareEncounterPlan.BindForSave(manager,null).Properties[OptionalKey]);
            if(id.Length>0)Assert.False(restored.Selects(manager,id),"This foreign/non-Hedgerow address may be preserved but cannot spawn.");
        }
        [TestCase("")][TestCase("2|Overworld.2.5.0")][TestCase("1|bad")][TestCase("1|Overworld.02.5.0")]
        [TestCase("1|Overworld.2.5.1")][TestCase("1|Overworld.2147483648.5.0")][TestCase("1|Overworld.-1.5.0")]
        [TestCase("1|Overworld.2.5.0|extra")][TestCase("1|Overworld.2.5")]
        public void MalformedOptionalMetadataNeverThrowsOrDisablesAnExistingPair(string value)
        {
            SpreadRareEncounterPlan restored=null;Assert.DoesNotThrow(()=>restored=SpreadRareEncounterPlan.Restore(Metadata(manager.RareEncounters.PairZoneID,value)));
            Assert.True(restored.Initialized);Assert.AreEqual(manager.RareEncounters.PairZoneID,restored.PairZoneID);Assert.False(OptionalInitialized(restored));Assert.IsEmpty(Viper(restored));
        }
        [TestCase("biome")][TestCase("poi")]
        public void ChangedMapRefusesOptionalAddressWithoutReroll(string change)
        {
            string id=Selected();var p=WorldMap.FromZoneID(id);if(change=="biome")manager.WorldMap.Tiles[p.x,p.y]=BiomeType.Beating;
            else manager.WorldMap.SetPOI(p.x,p.y,new PointOfInterest(POIType.MerchantCamp,"owned camp"));
            Assert.False(manager.RareEncounters.Selects(manager,id));Assert.AreEqual(id,Viper(manager.RareEncounters));Assert.False(new SpreadRareEncounterBuilder(manager).TryPlace(new Zone(id),scope.Factory));
        }
        [Test] public void RealWarningPrecedesWakeDistanceAndExistingDryLaneBypassesThePocket()
        {
            var z=Pocket();var before=z.GetReadOnlyEntities().ToArray();Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            var snake=z.GetReadOnlyEntities().Single(e=>e.BlueprintName==Child);var sign=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Signpost");
            Assert.AreNotEqual(snake.ID,sign.ID);Assert.AreEqual(z.ZoneID,snake.GetProperty(SpreadRareEncounterBuilder.SourceKey));Assert.AreEqual(z.ZoneID,sign.GetProperty(SpreadRareEncounterBuilder.SourceKey));
            Assert.NotNull(sign.GetPart<RegionalSignpostPart>());Assert.True(sign.GetPart<PhysicsPart>().Solid);Assert.True(before.All(e=>z.GetEntityCell(e)!=null));
            var p=z.GetEntityPosition(snake);var s=z.GetEntityPosition(sign);Assert.AreEqual((p.x-5,p.y),s);Assert.Greater(AIHelpers.ChebyshevDistance(p.x,p.y,s.x,s.y),snake.GetPart<BrainPart>().SightRadius);
            var text=sign.GetPart<ExaminablePart>().Description;StringAssert.Contains("vipers",text);StringAssert.Contains("poison",text);StringAssert.Contains("south",text);StringAssert.DoesNotContain("is waiting",text);
            Assert.True(SafePath(z,(s.x-1,s.y),(p.x+5,p.y),p));Assert.True(new[]{snake,sign}.All(e=>!z.GenReservedCells.Contains(z.GetEntityPosition(e))));
            var owners=z.GetReadOnlyEntities().ToArray();Assert.False(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
        }
        static bool SafePath(Zone z,(int x,int y)start,(int x,int y)end,(int x,int y)snake)
        {
            var q=new Queue<(int x,int y)>();var seen=new HashSet<(int x,int y)>();q.Enqueue(start);seen.Add(start);
            while(q.Count>0){var p=q.Dequeue();if(p==end)return true;for(int d=0;d<4;d++){var n=(x:p.x+(d==0?1:d==1?-1:0),y:p.y+(d==2?1:d==3?-1:0));var c=z.GetCell(n.x,n.y);
                if(c==null||seen.Contains(n)||c.BlocksMovement()||AIHelpers.ChebyshevDistance(n.x,n.y,snake.x,snake.y)<=3||c.Occupants.Any(e=>e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>()))continue;seen.Add(n);q.Enqueue(n);}}
            return false;
        }
        [TestCase("content")][TestCase("reservation")][TestCase("water")][TestCase("sealed")]
        [TestCase("heat")][TestCase("coating")][TestCase("cloud")]
        public void RefusedPocketPreservesAllExistingOwnersAndDoesNotCommitHalfASource(string change)
        {
            var z=Pocket();if(change=="content")scope.Factory.Blueprints.Remove("Signpost");
            if(change=="reservation")z.ForEachCell((c,x,y)=>z.GenReservedCells.Add((x,y)));
            if(change=="water")z.ForEachCell((c,x,y)=>z.AddEntity(scope.Factory.CreateEntity("WaterPuddle"),x,y));
            if(change=="sealed")z.ForEachCell((c,x,y)=>{if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)z.AddEntity(scope.Factory.CreateEntity("Hedge"),x,y);});
            if(change=="heat")z.ForEachCell((c,x,y)=>z.TileState.AddHeat(x,y,1));
            if(change=="coating")z.ForEachCell((c,x,y)=>z.TileState.WriteCoating(x,y,"water",3));
            if(change=="cloud")z.ForEachCell((c,x,y)=>z.TileState.WriteCloud(x,y,"smoke",3));
            string before=Graph(z);Assert.False(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));Assert.AreEqual(before,Graph(z));
        }
        public sealed class LatchcoilMutationPart:Part
        {public static Action<Entity> Callback;public override string Name=>"LatchcoilMutation";public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
        [TestCase("map")][TestCase("carried")][TestCase("id")][TestCase("route")][TestCase("awake")][TestCase("footprint")]
        public void StagingCallbackCannotSmuggleChangedAuthorityOwnerOrBlockedRoute(string change)
        {
            var z=Pocket();Assert.True(scope.Factory.Blueprints.ContainsKey(Child));scope.Factory.RegisterPartType<LatchcoilMutationPart>("LatchcoilMutation");
            scope.Factory.Blueprints[Child].Parts["LatchcoilMutation"]=new Dictionary<string,string>();
            LatchcoilMutationPart.Callback=e=>{if(change=="map"){var p=WorldMap.FromZoneID(z.ZoneID);manager.WorldMap.Tiles[p.x,p.y]=BiomeType.Beating;}
                if(change=="footprint")e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});if(change=="awake")e.GetPart<BrainPart>().ClearGoals();if(change=="carried")e.GetPart<PhysicsPart>().InInventory=new Entity();if(change=="id")e.ID="";
                if(change=="route")z.ForEachCell((c,x,y)=>{if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)z.AddEntity(scope.Factory.CreateEntity("Hedge"),x,y);});};
            var before=z.GetReadOnlyEntities().ToArray();int version=z.EntityVersion;Assert.False(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));if(change=="footprint")Assert.AreEqual(version,z.EntityVersion,"Malformed staged body must refuse before inserting a temporary owner.");Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName==Child||e.BlueprintName=="Signpost"));Assert.True(before.All(e=>z.GetEntityCell(e)!=null));
        }
        [TestCase(false)][TestCase(true)]
        public void SavedOptionalOverlapOrAdjacencyCannotTakeOverTheOriginalPair(bool adjacent)
        {
            string pair=manager.RareEncounters.PairZoneID, optional=pair;
            if(adjacent)
            {
                var p=WorldMap.FromZoneID(pair);
                optional=Enumerable.Range(-1,3).SelectMany(dx=>Enumerable.Range(-1,3).Select(dy=>WorldMap.ToZoneID(p.x+dx,p.y+dy,0)))
                    .First(id=>id!=pair&&SpreadRareEncounterPlan.IsEligible(manager,id)&&FormationSelector.For(BiomeType.Spread,id)==Formation.Hedgerow);
            }
            SetPlan(manager,SpreadRareEncounterPlan.Restore(Metadata(pair,"1|"+optional)));
            var z=new Zone(pair);z.AddEntity(scope.Factory.CreateEntity("Hedge"),5,2);z.AddEntity(scope.Factory.CreateEntity("Hedge"),12,6);
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            CollectionAssert.AreEquivalent(new[]{SpreadRareEncounterPlan.PairLeader,SpreadRareEncounterPlan.PairMate},z.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).Select(e=>e.BlueprintName));
            Assert.AreEqual(optional,Viper(manager.RareEncounters));
            if(adjacent)Assert.False(manager.RareEncounters.Selects(manager,optional));
        }
        [Test] public void ActualFamilySelectionCommitAndRefusalAreDiagnosedWithoutChangingThePairRecord()
        {
            bool old=Diag.IsChannelEnabled("worldgen");var before=new HashSet<string>(Diag.Snapshot(Diag.BufferCapacity).Select(e=>e.TraceId));Diag.SetChannel("worldgen",true);
            try
            {
                manager=OverworldZoneManager.CreateDetached(scope.Factory,64);var z=Pocket();var b=new SpreadRareEncounterBuilder(manager);
                Assert.True(b.TryPlace(z,scope.Factory));Assert.False(b.TryPlace(z,scope.Factory));
                var rows=Diag.Snapshot(Diag.BufferCapacity).Where(e=>!before.Contains(e.TraceId)&&e.Category=="worldgen").ToArray();
                foreach(string kind in new[]{"SpreadRareSelected","SpreadRareCommitted","SpreadRareRejected"})
                {var match=rows.Where(e=>e.Kind==kind&&e.PayloadJson.Contains("chalk-ring-viper")).ToArray();Assert.AreEqual(1,match.Length,kind);StringAssert.Contains(z.ZoneID,match[0].PayloadJson);}
                var pair=rows.Single(e=>e.Kind=="SpreadRareSelected"&&e.PayloadJson.Contains("ditch-cutters"));StringAssert.Contains(manager.RareEncounters.PairZoneID,pair.PayloadJson);
            }
            finally{Diag.SetChannel("worldgen",old);}
        }

        [Test] public void FrozenSelectionAndExactSourceOwnersSurviveSaveWithoutRevisitDuplication()
        {
            var z=Pocket();Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));var source=z.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).Select(e=>e.ID).OrderBy(x=>x).ToArray();
            manager.SetActiveZone(z);var player=scope.Factory.CreateEntity("Player");z.AddEntity(player,40,20);var turns=new TurnManager();turns.RestoreSavedState(0,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
            var saved=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("latchcoil","latchcoil",manager,turns,player));Assert.AreEqual(Viper(manager.RareEncounters),Viper(saved.ZoneManager.RareEncounters));
            var restored=saved.ZoneManager.GetZone(z.ZoneID);CollectionAssert.AreEqual(source,restored.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).Select(e=>e.ID).OrderBy(x=>x));Assert.AreSame(restored,saved.ZoneManager.GetZone(z.ZoneID));
        }
    }
}
