using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent failure hypotheses: changed native content, stale
    /// ownership, malformed addresses, later stamps and adversarial map authority.</summary>
    public class CinderholdCompositionAdversarialTests
    {
        private const string Id=CinderholdCompositionTests.Id;

        // A successful blueprint lookup is not successful realization. A missing
        // late owner must not leave the player stranded in a half-authored town.
        [TestCase("Grass")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("SandstoneWall")]
        [TestCase("Tree")] [TestCase("Bush")] [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")]
        [TestCase("ConcordFactor")] [TestCase("CinderholdNoticeBoard")] [TestCase("Chest")]
        [TestCase("Campfire")] [TestCase("TinkersForge")] [TestCase("SmithAnvil")] [TestCase("Weaponsmith")]
        public void MissingNativeDependencyRejectsBeforeAnyGroundOrReservationChanges(string bp)
        {
            var f=CinderholdCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));
            var b=new CinderholdCompositionBuilder(64);Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);
            CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }

        // Fail-soft ApplyParameters must not convert visible working services to
        // invisible, movable, empty or decorative impostors during staging.
        [TestCase("Grass","Render","RenderString","?")]
        [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("RoadStone","Physics","Solid","true")]
        [TestCase("SandstoneWall","Destructible","HP","0")]
        [TestCase("SandstoneWall","Material",null,null)]
        [TestCase("Tree","Destructible",null,null)]
        [TestCase("Bush","Material",null,null)]
        [TestCase("Bed","Bed",null,null)] [TestCase("Chair","Chair",null,null)]
        [TestCase("Crate","Container",null,null)] [TestCase("Chest","Container",null,null)]
        [TestCase("ConcordFactor","Conversation","ConversationID","Merchant_1")]
        [TestCase("ConcordFactor","Physics","Solid","false")]
        [TestCase("CinderholdNoticeBoard","Render","Visible","false")]
        [TestCase("Campfire","Fuel",null,null)] [TestCase("Campfire","LightSource",null,null)]
        [TestCase("TinkersForge","Forge",null,null)]
        [TestCase("SmithAnvil","Handling","Weight","1")]
        [TestCase("Weaponsmith","Trader","StockTable","MerchantStock")]
        [TestCase("Weaponsmith","Conversation","ConversationID","Merchant_1")]
        public void CorruptedNativePartOrFieldRejectsAtomically(string bp,string part,string key,string value)
        {
            var f=CinderholdCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(Id);z.GenReservedCells.Add((79,24));Assert.IsFalse(new CinderholdCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        [TestCase(-1,0)] [TestCase(0,-1)] [TestCase(80,0)] [TestCase(0,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void OutboundPlanQueriesDoNotLeakOrClampIntoTown(int x,int y)
        {
            var p=CinderholdCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));
            Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));
        }

        [Test] public void ExistingPlayerGraphAndDestroyedObjectsCannotBeReconstructedByBaseOrProfile()
        {
            CinderholdCompositionTests.LoadLoot();try
            {
                var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);var profile=new CinderholdProfileBuilder(b);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(profile.BuildZone(z,f,new Random(1)));
                var forge=z.GetAllEntities().Single(e=>e.BlueprintName=="TinkersForge");z.RemoveEntity(forge);var before=z.GetAllEntities().ToArray();
                Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(profile.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
                Assert.IsFalse(z.GetAllEntities().Contains(forge));
            }finally{CavesOfOoo.Data.LootTableRegistry.ResetForTests();}
        }
        [Test] public void ForeignZoneInstanceCannotConsumeAnotherPlansLateOwners()
        {
            var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var other=new Zone(Id);Assert.IsFalse(new CinderholdProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);
            Assert.IsFalse(b.BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);
        }
        // Refusing a foreign call must preserve the previous owner's valid
        // generation capability. Rejection may not poison its not-yet-run profile.
        [TestCase(false)] [TestCase(true)]
        public void RefusedBaseCallDoesNotInvalidateTheSuccessfulOwnersPendingProfile(bool foreign)
        {
            CinderholdCompositionTests.LoadLoot();try
            {
                var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var plan=b.Plan;
                Assert.IsFalse(b.BuildZone(foreign?new Zone(Id):z,f,new Random(2)));Assert.AreSame(plan,b.Plan);
                Assert.IsTrue(new CinderholdProfileBuilder(b).BuildZone(z,f,new Random(3)));
                Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="ConcordFactor"));
            }finally{CavesOfOoo.Data.LootTableRegistry.ResetForTests();}
        }
        [Test] public void NativeRetryOnTheSameClearedZoneDoesNotReuseOldProfileEntities()
        {
            CinderholdCompositionTests.LoadLoot();try
            {
                var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);var profile=new CinderholdProfileBuilder(b);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(profile.BuildZone(z,f,new Random(1)));var old=z.GetAllEntities().ToArray();foreach(var e in old)z.RemoveEntity(e);
                Assert.IsTrue(b.BuildZone(z,f,new Random(2)));Assert.IsTrue(profile.BuildZone(z,f,new Random(2)));Assert.IsFalse(z.GetAllEntities().Any(old.Contains));
                Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="ConcordFactor"));
            }finally{CavesOfOoo.Data.LootTableRegistry.ResetForTests();}
        }

        [TestCase(false)] [TestCase(true)]
        public void BlockedOrMalformedLastLateOwnerCannotPartiallyPopulateTheProfile(bool malformed)
        {
            var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var p=b.Plan.Profile.Last();if(malformed)f.Blueprints[p.Blueprint].Parts["Physics"]["Takeable"]="true";else Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),p.X,p.Y));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(new CinderholdProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }

        [TestCase(POIType.Village,"ConcordPost")] [TestCase(POIType.Lair,"PruningPost")]
        [TestCase(POIType.Village,null)] [TestCase(POIType.Village,"pruningpost")]
        public void SameCoordinatesCannotOverrideRuntimePlaceAuthority(POIType type,string profile)
        {
            var m=new OverworldZoneManager(CinderholdCompositionTests.Factory(),64);
            Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<CinderholdCompositionBuilder>().Any());
            m.WorldMap.SetPOI(6,6,new PointOfInterest(type,"Cinderhold",profile:profile));
            Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<CinderholdCompositionBuilder>().Any());
        }
        [Test] public void CorrectProfileSurvivesRenamingButCannotBeCopiedToAnotherAddress()
        {
            var m=new OverworldZoneManager(CinderholdCompositionTests.Factory(),64);m.WorldMap.GetPOI(6,6).Name="Renamed working town";
            Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<CinderholdCompositionBuilder>().Any());
            m.WorldMap.SetPOI(7,8,new PointOfInterest(POIType.Village,"Cinderhold",profile:"PruningPost"));
            Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.7.8.0").Builders.OfType<CinderholdCompositionBuilder>().Any());
        }

        // Later native population is intentionally retained; its random furniture
        // must not close the route to the promised services or any exterior entry.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void CompleteNativePipelinePreservesDoorsFrontagesAndAllFourDirections(int seed)
        {
            CinderholdCompositionTests.LoadLoot();try
            {
                var z=new OverworldZoneManager(CinderholdCompositionTests.Factory(),seed).GetZone(Id);var p=CinderholdCompositionPlan.Create(Id,seed);
                var services=z.GetAllEntities().Where(e=>new[]{"ConcordFactor","Weaponsmith","CinderholdNoticeBoard","TinkersForge","SmithAnvil","Well","Shrine","Merchant","Quartermaster"}.Contains(e.BlueprintName))
                    .Select(e=>(bp:e.BlueprintName,pos:z.GetEntityPosition(e))).ToArray();
                Assert.GreaterOrEqual(services.Length,9);
                foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
                CinderholdCompositionTests.AssertConnected(z);var seen=CinderholdCompositionTests.Flood(z);
                foreach(var r in p.Rooms)Assert.IsTrue(seen[r.DoorX,r.DoorY]);
                foreach(var s in services)Assert.IsTrue(CinderholdCompositionTests.Neighbors(s.pos.x,s.pos.y).Any(n=>z.InBounds(n.x,n.y)&&seen[n.x,n.y]),s.bp+" has no reachable standing frontage.");
            }finally{CavesOfOoo.Data.LootTableRegistry.ResetForTests();}
        }
        [Test] public void TwentyFourIndependentSeedsHaveNoBasePocketsOrFurnitureOnPublicApproaches()
        {
            var f=CinderholdCompositionTests.Factory();
            foreach(int seed in Enumerable.Range(0,24).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new CinderholdCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(9)));CinderholdCompositionTests.AssertConnected(z);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(b.Plan.ObjectAt(x,y)!=null)Assert.IsFalse(b.Plan.IsApproach(x,y),"A planned object interrupts public access, seed"+seed);
            }
        }

        [Test] public void ActualCaveRollsHaveReservedDryArrivalAndStandingCells()
        {
            int stairs=0;var f=CinderholdCompositionTests.Factory();
            foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(e);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(c.IsInterior);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    foreach(var n in CinderholdCompositionTests.Neighbors(c.X,c.Y))if(z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement())Assert.IsTrue(z.GenReservedCells.Contains(n));
                }
            }
            Assert.Greater(stairs,0,"The native random roll must be exercised, not vacuously skipped.");
        }

        [Test] public void ArrivalPassOnlyReservesActualOwnersAndEmitsSuccessOrExplicitRefusal()
        {
            Diag.ResetAll();try
            {
                var f=CinderholdCompositionTests.Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                var gate=new CinderholdArrivalReservationBuilder(b);Assert.AreEqual(3870,gate.Priority);var at=(x:5,y:12);Assert.IsFalse(z.GetCell(at.x,at.y).BlocksMovement());
                Assert.IsTrue(z.AddEntity(f.CreateEntity("StairsDown"),at.x,at.y));var before=z.GetAllEntities().ToArray();Assert.IsTrue(gate.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
                Assert.IsTrue(z.GenReservedCells.Contains(at));Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="CinderholdArrivalsReserved",Limit=10}).Records.Count);
                var foreign=new Zone(Id);foreign.GenReservedCells.Add((79,24));Assert.IsFalse(gate.BuildZone(foreign,f,new Random(1)));Assert.AreEqual(0,foreign.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},foreign.GenReservedCells);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="CinderholdArrivalsRejected",Limit=10}).Records.Count);
            }finally{Diag.ResetAll();}
        }
        [TestCase(false)] [TestCase(true)]
        public void PostingNeedsBothTheActiveErrandAndPhysicalWrit(bool active)
        {
            CinderholdCompositionTests.WithQuest((f,z,p)=>
            {
                if(active)CavesOfOoo.Storylets.StoryletPart.Current.StartQuest(new CavesOfOoo.Storylets.QuestState{QuestId="PruningContract",CurrentStageIndex=0});
                else p.GetPart<InventoryPart>().AddObject(f.CreateEntity("PruningWrit"));
                ConversationManager.StartConversation(f.CreateEntity("ChoirTendril"),p);Assert.IsFalse(CinderholdCompositionTests.ChoiceVisible("[Post]"));Assert.AreEqual(0,PlayerReputation.Get("RotChoir"));
            });
        }
    }
}
