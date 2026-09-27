using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadRareEncounterAdversarialTests
    {
        DensityLootTestScope scope; OverworldZoneManager manager;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(scope.Factory, 64); }
        [TearDown] public void Teardown() { RarePlacementMutationPart.Callback=null; scope.Dispose(); }
        static void SetPlan(OverworldZoneManager m, SpreadRareEncounterPlan p)
            => typeof(OverworldZoneManager).GetProperty("RareEncounters").SetValue(m, p);

        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void FreshAddressIsStableEligibleAndDoesNotGenerateAnyZone(int seed)
        {
            var m = OverworldZoneManager.CreateDetached(scope.Factory, seed);
            string id = m.RareEncounters.PairZoneID;
            Assert.IsNotEmpty(id); Assert.IsTrue(SpreadRareEncounterPlan.IsEligible(m, id));
            Assert.AreEqual(id, SpreadRareEncounterPlan.Create(m).PairZoneID);
            Assert.AreEqual(0, m.CachedZoneCount);
            Assert.IsTrue(m.RareEncounters.Selects(m, id));
            Assert.IsFalse(m.RareEncounters.Selects(m, ReferenceGladePlan.ZoneID));
        }
        [TestCase("biome")] [TestCase("poi")]
        public void ChangedMapRefusesTheFrozenAddressWithoutChoosingAnother(string change)
        {
            string id = manager.RareEncounters.PairZoneID; var p = WorldMap.FromZoneID(id);
            if (change == "biome") manager.WorldMap.Tiles[p.x, p.y] = BiomeType.Beating;
            else manager.WorldMap.SetPOI(p.x, p.y, new PointOfInterest(POIType.MerchantCamp, "owned camp"));
            Assert.IsFalse(manager.RareEncounters.Selects(manager, id)); Assert.AreEqual(id, manager.RareEncounters.PairZoneID);
        }
        [TestCase(null)] [TestCase("")] [TestCase("WorldMap")] [TestCase("Overworld.2.4")]
        [TestCase("Overworld.2.4.1")] [TestCase("Overworld.-1.5.0")] [TestCase("Overworld.11.10.0")]
        public void InvalidAndOwnedAddressesAreIneligible(string id)
            => Assert.IsFalse(SpreadRareEncounterPlan.IsEligible(manager, id));

        [TestCase(null)] [TestCase("")] [TestCase("2|Overworld.1.1.0")]
        [TestCase("1|bad")] [TestCase("1|Overworld.2.4")]
        [TestCase("1|Overworld.1.1.1")] [TestCase("1|Overworld.01.1.0")]
        public void OldOrMalformedMetadataCannotInitializeRareContent(string value)
        {
            var world = new Entity(); if (value != null) world.Properties[SpreadRareEncounterPlan.PropertyKey] = value;
            var plan = SpreadRareEncounterPlan.Restore(world);
            Assert.IsFalse(plan.Initialized); Assert.IsEmpty(plan.PairZoneID);
        }
        [Test]
        public void VersionedEmptySelectionSurvivesWithoutBackfill()
        {
            for (int y=0;y<20;y++) for (int x=0;x<20;x++) manager.WorldMap.Tiles[x,y] = BiomeType.Beating;
            SetPlan(manager, SpreadRareEncounterPlan.Create(manager));
            Assert.IsTrue(manager.RareEncounters.Initialized); Assert.IsEmpty(manager.RareEncounters.PairZoneID);
            var world = SpreadRareEncounterPlan.BindForSave(manager, null);
            var restored = SpreadRareEncounterPlan.Restore(world);
            Assert.IsTrue(restored.Initialized); Assert.IsEmpty(restored.PairZoneID);
            SetPlan(manager, SpreadRareEncounterPlan.Restore(null));
            Assert.IsNull(SpreadRareEncounterPlan.BindForSave(manager, null));
        }

        Zone Pocket()
        {
            var z = new Zone(manager.RareEncounters.PairZoneID);
            var hedge = scope.Factory.CreateEntity("Hedge"); Assert.IsTrue(z.AddEntity(hedge,5,2));
            return z;
        }
        public sealed class RarePlacementMutationPart : Part
        {
            public static Action Callback;
            public override string Name=>"RarePlacementMutation";
            public override bool HandleEvent(GameEvent e){ if(e.ID=="ObjectCreated") Callback?.Invoke();return true; }
        }
        [Test]
        public void ForeignFactoryCannotProduceUnderAnotherManagersSelection()
        {
            var foreign=new EntityFactory{Blueprints=scope.Factory.Blueprints};LoadoutPart.Factory=foreign;
            var z=Pocket();
            Assert.IsFalse(new SpreadRareEncounterBuilder(manager).TryPlace(z,foreign));
            Assert.IsFalse(z.GetReadOnlyEntities().Any(e=>e.HasTag("Creature")));
        }
        [Test]
        public void CreationCallbackClosingDistantApproachCannotCommitAnUnreachablePair()
        {
            var z=Pocket();scope.Factory.RegisterPartType<RarePlacementMutationPart>("RarePlacementMutation");
            scope.Factory.Blueprints[SpreadRareEncounterPlan.PairLeader].Parts["RarePlacementMutation"]=new Dictionary<string,string>();
            RarePlacementMutationPart.Callback=()=>z.ForEachCell((c,x,y)=>{
                if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)z.AddEntity(scope.Factory.CreateEntity("Hedge"),x,y);
            });
            Assert.IsFalse(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            Assert.IsFalse(z.GetReadOnlyEntities().Any(e=>e.HasTag("Creature")));
        }
        [TestCase("SpreadHurdleCutter", "biome")]
        [TestCase("SpreadDitchMate", "poi")]
        [TestCase("SpreadHurdleCutter", "unchanged")]
        [TestCase("SpreadDitchMate", "unchanged")]
        public void CreationCallbackMustRetainCurrentPairSourceAuthority(string blueprint, string change)
        {
            var zone = Pocket();
            var before = zone.GetReadOnlyEntities().ToArray();
            string selected = manager.RareEncounters.PairZoneID;
            var address = WorldMap.FromZoneID(selected);
            int callbacks = 0;
            scope.Factory.RegisterPartType<RarePlacementMutationPart>("RarePlacementMutation");
            scope.Factory.Blueprints[blueprint].Parts["RarePlacementMutation"] = new Dictionary<string, string>();
            RarePlacementMutationPart.Callback = () =>
            {
                callbacks++;
                if (change == "biome") manager.WorldMap.Tiles[address.x, address.y] = BiomeType.Beating;
                if (change == "poi") manager.WorldMap.SetPOI(address.x, address.y,
                    new PointOfInterest(POIType.MerchantCamp, "callback-owned camp"));
            };

            bool placed = new SpreadRareEncounterBuilder(manager).TryPlace(zone, scope.Factory);
            Assert.AreEqual(1, callbacks, "The real creation callback must execute.");
            Assert.AreEqual(selected, manager.RareEncounters.PairZoneID, "Refusal must not reroll selection.");
            Assert.AreEqual(change == "unchanged", placed);
            if (change == "unchanged")
            {
                var pair = zone.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).ToArray();
                Assert.AreEqual(2, pair.Length);
                Assert.IsTrue(pair.All(e => e.GetProperty(SpreadRareEncounterBuilder.SourceKey) == selected));
                Assert.AreEqual(3, pair.Sum(e => DensityLootTestScope.Gear(e).Count()));
                Assert.IsTrue(before.All(e => zone.GetEntityCell(e) != null));
            }
            else
            {
                Assert.IsFalse(manager.RareEncounters.Selects(manager, selected));
                CollectionAssert.AreEquivalent(before, zone.GetReadOnlyEntities(), "Refusal must preserve every pre-existing owner.");
                Assert.IsFalse(zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)));
            }
        }
        [Test]
        public void EnclosedHedgePocketCannotBecomeAnUnreachableRewardSite()
        {
            var z = Pocket();
            z.ForEachCell((c,x,y) => { if (x == 0 || y == 0 || x == Zone.Width-1 || y == Zone.Height-1)
                z.AddEntity(scope.Factory.CreateEntity("Hedge"),x,y); });
            var before=z.GetReadOnlyEntities().ToArray();
            Assert.IsFalse(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            CollectionAssert.AreEquivalent(before,z.GetReadOnlyEntities());
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void ActualSelectedGeneratedSourceReplacesItsGroupAndPreservesCachedOwners(int seed)
        {
            scope.Seed(seed);
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed);
            var z=m.GetZone(m.RareEncounters.PairZoneID);
            var pair=z.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).ToArray();
            Assert.AreEqual(2,pair.Length);
            Assert.IsFalse(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Viper"||e.BlueprintName=="MarlbackScrabbler"));
            Assert.AreSame(z,m.GetZone(z.ZoneID));
            CollectionAssert.AreEquivalent(pair,m.GetZone(z.ZoneID).GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)));
        }
        [TestCase("SpreadHurdleCutter")] [TestCase("SpreadDitchMate")]
        public void PermanentDescriptionDoesNotPromiseRemovedGearOrAnUnchangedRoute(string blueprint)
        {
            var actor=scope.Factory.CreateEntity(blueprint);
            foreach(var item in DensityLootTestScope.Gear(actor).ToArray())
            { InventorySystem.UnequipItem(actor,item); actor.GetPart<InventoryPart>().RemoveObject(item); }
            string text=actor.GetPart<ExaminablePart>().Description;
            StringAssert.DoesNotContain("sits low",text); StringAssert.DoesNotContain("Its short sword",text);
            StringAssert.DoesNotContain("grips a",text); StringAssert.DoesNotContain("leaves an open flank",text);
        }
        [Test]
        public void PairCommitsRealGearAndDoesNotRepeatOnTheSameGraph()
        {
            var z = Pocket(); var b = new SpreadRareEncounterBuilder(manager);
            Assert.IsTrue(b.TryPlace(z, scope.Factory));
            var pair = z.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).ToArray();
            Assert.AreEqual(2,pair.Length); var ids=pair.Select(e=>e.ID).ToArray();
            Assert.IsFalse(b.TryPlace(z,scope.Factory));
            CollectionAssert.AreEquivalent(ids,z.GetReadOnlyEntities().Where(e=>e.HasTag("Creature")).Select(e=>e.ID));
            Assert.AreEqual(3,pair.Sum(e=>DensityLootTestScope.Gear(e).Count()));
        }
        [TestCase("content")] [TestCase("reservation")] [TestCase("water")] [TestCase("neighbor")]
        public void RefusedPreflightKeepsExistingOwnersAndNoPartialPair(string reason)
        {
            var z=Pocket();
            if(reason=="content")scope.Factory.Blueprints.Remove("LeatherCap");
            if(reason=="reservation")z.ForEachCell((c,x,y)=>z.GenReservedCells.Add((x,y)));
            if(reason=="water")z.ForEachCell((c,x,y)=>{var e=new Entity();e.AddPart(new LiquidPoolPart());z.AddEntity(e,x,y);});
            if(reason=="neighbor"){var e=scope.Factory.CreateEntity("MarlbackScrabbler");z.AddEntity(e,5,3);}
            var owners=z.GetReadOnlyEntities().ToArray();
            Assert.IsFalse(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
        }
        [Test]
        public void SuccessfulReplacementKeepsAmbientRowsAndRemovesOnlyTheOrdinaryGroup()
        {
            var z=Pocket();var table=PopulationTable.SpreadTier1();
            var b=new PopulationBuilder(table){SpreadEncounter=new SpreadRareEncounterBuilder(manager)};
            Assert.IsTrue(b.BuildZone(z,scope.Factory,new Random(64)));
            Assert.AreEqual(2,z.GetReadOnlyEntities().Count(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)));
            Assert.IsFalse(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Viper"||e.BlueprintName=="MarlbackScrabbler"));
            Assert.IsTrue(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Magpie"));
            Assert.AreEqual(2,table.Entries.Count(e=>e.EncounterGroup=="SpreadTier1Encounter"));
        }
        [Test]
        public void UnselectedSourcePreservesExactPopulationAndCallerRng()
        {
            string id="Overworld.11.9.0";Assert.AreNotEqual(id,manager.RareEncounters.PairZoneID);
            var a=new Zone(id);var b=new Zone(id);var ra=new Random(64);var rb=new Random(64);
            new PopulationBuilder(PopulationTable.SpreadTier1()).BuildZone(a,scope.Factory,ra);
            new PopulationBuilder(PopulationTable.SpreadTier1()){SpreadEncounter=new SpreadRareEncounterBuilder(manager)}.BuildZone(b,scope.Factory,rb);
            CollectionAssert.AreEqual(a.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+a.GetEntityPosition(e)),b.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+b.GetEntityPosition(e)));
            Assert.AreEqual(ra.Next(),rb.Next());
        }
        [Test]
        public void FullSavePreservesSelectionActorAndActualEquippedItemIdentity()
        {
            var z=Pocket();Assert.IsTrue(new SpreadRareEncounterBuilder(manager).TryPlace(z,scope.Factory));
            manager.SetActiveZone(z);var player=scope.Factory.CreateEntity("Player");z.AddEntity(player,40,20);
            var leader=z.GetReadOnlyEntities().Single(e=>e.BlueprintName==SpreadRareEncounterPlan.PairLeader);
            var gear=DensityLootTestScope.Gear(leader).Select(e=>e.ID).OrderBy(x=>x).ToArray();
            var turns=new TurnManager();turns.RestoreSavedState(0,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
            var restored=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("first-hour-test","first-hour",manager,turns,player));
            Assert.AreEqual(manager.RareEncounters.PairZoneID,restored.ZoneManager.RareEncounters.PairZoneID);
            var saved=restored.ZoneManager.GetZone(z.ZoneID).GetReadOnlyEntities().Single(e=>e.ID==leader.ID);
            Assert.AreNotSame(leader,saved);CollectionAssert.AreEqual(gear,DensityLootTestScope.Gear(saved).Select(e=>e.ID).OrderBy(x=>x));
            Assert.IsTrue(DensityLootTestScope.Gear(saved).All(e=>ReferenceEquals(e.GetPart<PhysicsPart>().Equipped,saved)));
        }
    }
}
