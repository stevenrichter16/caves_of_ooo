using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>The Ledger is an inhabited excavation with three irreplaceable
    /// witnesses. Composition must preserve their native ambiguity and the real
    /// courier departure, rather than invent body-reading or excavation verbs.</summary>
    public class DrownedLedgerCompositionTests
    {
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        public const string Id="Overworld.17.5.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        public static readonly string[] Profile={"PreFellingBody","PreFellingBody","PreFellingBody","SurveyStake","SurveyStake","SurveyStake","ReadingTable","RecensionScribe","CurationSorter"};
        [Test] public void ExactNamedCampPreservesCurrentMapBalanceAndVariesBySeed()
        {
            Assert.IsTrue(DrownedLedgerCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(DrownedLedgerCompositionPlan.Create(Id,64).Signature(),DrownedLedgerCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(DrownedLedgerCompositionPlan.Create(Id,64).Signature(),DrownedLedgerCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Sodden,WorldMapAuthoring.BiomeAt(17,5));Assert.AreEqual(2,WorldMapAuthoring.TierAt(17,5));
            Assert.IsFalse(WorldMapAuthoring.IsRoad(17,5));Assert.IsFalse(WorldMapAuthoring.IsRiver(17,5));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.17.5.1")] [TestCase("Overworld.17.5.-1")]
        [TestCase("Overworld.017.5.0")] [TestCase("Overworld.17.5.00")] [TestCase(" Overworld.17.5.0")]
        [TestCase("Overworld.17.5.0 ")] [TestCase("overworld.17.5.0")] [TestCase("Overworld.16.5.0")]
        [TestCase("Overworld.999999999999.5.0")] [TestCase("Overworld.17.5")]
        public void MalformedAndNeighboringAddressesCannotAcquireTheWitnessCamp(string id)
        {Assert.IsFalse(DrownedLedgerCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>DrownedLedgerCompositionPlan.Create(id,64));}

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void ThreeUnequalWetBaysRemainIntactBesideDryReadingAndServiceRooms(int seed)
        {
            var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);
            Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));Assert.AreSame(z,b.RealizedZone);var p=b.Plan;
            Assert.That(p.Rooms.Count,Is.InRange(3,4));Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Role).Distinct().Count(),3);
            var reading=p.Rooms.Single(r=>r.Role=="ReadingTent");Assert.That(reading.Width,Is.GreaterThan(12));
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="TentWall"));Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Bed"));
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Crate"));
            var components=WaterComponents(z);Assert.AreEqual(3,components.Count);Assert.IsTrue(components.All(c=>c.Count>=25));Assert.GreaterOrEqual(components.Select(c=>c.Count).Distinct().Count(),2);
            var reach=DryReach(z);AssertFourDirections(reach);
            foreach(var r in p.Rooms)
            {
                Assert.IsTrue(reach.Contains((r.DoorX,r.DoorY)),r.Role);Assert.IsTrue(p.IsApproach(r.DoorX,r.DoorY));
                for(int y=r.Y+1;y<r.Y+r.Height-1;y++)for(int x=r.X+1;x<r.X+r.Width-1;x++)
                {Assert.IsTrue(z.GetCell(x,y).IsInterior);Assert.AreEqual("StoneFloor",p.GroundAt(x,y));}
            }
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                if(p.IsWet(x,y)){Assert.IsTrue(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(p.IsReserved(x,y));}
                if(p.IsApproach(x,y)){Assert.IsTrue(reach.Contains((x,y)),"Approach "+x+","+y);Assert.IsFalse(p.IsWet(x,y));Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(!p.IsInterior(x,y))Assert.AreNotEqual("StoneFloor",p.GroundAt(x,y));
            }
            Assert.IsTrue(p.IsReserved(40,12));Assert.IsFalse(z.GetCell(40,12).BlocksMovement());Assert.IsFalse(p.IsWet(40,12));
        }

        [Test] public void ExactNineLateOwnersPreserveAdjacentTableAndSilentWitnesses()
        {
            var f=Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(Profile,b.Plan.Profile.Select(p=>p.Blueprint));Assert.IsFalse(z.GetAllEntities().Any(e=>Profile.Contains(e.BlueprintName)));
            var late=new DrownedLedgerProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(Profile,z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)).Select(e=>e.BlueprintName));
            var table=z.GetAllEntities().Single(e=>e.BlueprintName=="ReadingTable");var at=z.GetEntityPosition(table);
            var bodies=z.GetAllEntities().Where(e=>e.BlueprintName=="PreFellingBody").ToArray();
            Assert.AreEqual(1,bodies.Count(e=>z.GetEntityCell(e).IsInterior));
            var inside=bodies.Single(e=>z.GetEntityCell(e).IsInterior);var bodyAt=z.GetEntityPosition(inside);
            Assert.AreEqual(1,Math.Abs(at.x-bodyAt.x)+Math.Abs(at.y-bodyAt.y));Assert.AreNotEqual(at,bodyAt);
            foreach(var e in bodies)
            {Assert.AreEqual(80,e.GetPart<PhysicsPart>().Weight);Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);Assert.IsFalse(e.GetPart<PhysicsPart>().Solid);Assert.IsFalse(e.HasPart<ContainerPart>());Assert.IsFalse(e.HasPart<DestructiblePart>());Assert.IsFalse(e.HasPart<ConversationPart>());Assert.IsFalse(e.HasTag("Creature"));Assert.IsFalse(z.GetEntityCell(e).Objects.Any(o=>o.HasPart<LiquidPoolPart>()));}
            foreach(var bp in new[]{"RecensionScribe","CurationSorter"})
            {var actor=z.GetAllEntities().Single(e=>e.BlueprintName==bp);Assert.AreEqual(bp+"_1",actor.GetPart<ConversationPart>().ConversationID);Assert.IsTrue(actor.HasPart<AISelfPreservationPart>());Assert.IsFalse(actor.HasPart<TraderPart>());}
            foreach(var e in z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)))Assert.AreEqual(Id,e.GetProperty("SettlementId"));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalNativeManagerKeepsVillageServicesAndAccessibleWitnessFrontages(int seed)
        {
            var manager=new OverworldZoneManager(Factory(),seed);var pipeline=CinderholdCompositionTests.Pipeline(manager,Id);
            Assert.AreEqual(1,pipeline.Builders.OfType<DrownedLedgerCompositionBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<DrownedLedgerProfileBuilder>().Count());
            Assert.AreEqual(1,pipeline.Builders.OfType<DrownedLedgerArrivalReservationBuilder>().Count());Assert.IsFalse(pipeline.Builders.OfType<RiverChunkBuilder>().Any());
            Assert.IsTrue(pipeline.Builders.OfType<TradeStockBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<VillagePopulationBuilder>().Any());
            var z=manager.GetZone(Id);CollectionAssert.AreEquivalent(Profile,z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)).Select(e=>e.BlueprintName));
            foreach(var bp in new[]{"Merchant","Quartermaster","Scribe","Elder","Innkeeper"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(1,z.GetCell(40,12).Objects.Count(e=>e.BlueprintName=="Well"));
            var destinations=z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)||new[]{"Well","Shrine","Merchant","Quartermaster"}.Contains(e.BlueprintName)).Select(e=>(e.BlueprintName,z.GetEntityPosition(e))).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
            var seen=DryReach(z);AssertFourDirections(seen);
            foreach(var d in destinations)Assert.IsTrue(CinderholdCompositionTests.Neighbors(d.Item2.x,d.Item2.y).Any(seen.Contains),d.BlueprintName);
            Assert.AreEqual(3,WaterComponents(z).Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualPlacedSorterGivesOneSeparateSealedParcelToPackOrFeet(bool fullPack)
        {
            WithCourier((f,z,p)=>
            {
                var inv=p.GetPart<InventoryPart>();inv.MaxWeight=150;
                if(fullPack){var ballast=new Entity{BlueprintName="Ballast"};ballast.AddPart(new PhysicsPart{Weight=130,Takeable=true});Assert.IsTrue(inv.AddObject(ballast));}
                var originals=z.GetAllEntities().Where(e=>e.BlueprintName=="PreFellingBody").ToArray();var sorter=z.GetAllEntities().Single(e=>e.BlueprintName=="CurationSorter");
                Accept(sorter,p);Assert.IsTrue(StoryletPart.Current.IsQuestActive("BogBodyCourier"));
                var feet=z.GetEntityCell(p);var parcels=inv.Objects.Concat(feet.Objects).Where(e=>e.BlueprintName=="SealedBogTakenBody").Distinct().ToArray();Assert.AreEqual(1,parcels.Length);
                Assert.AreEqual(fullPack,feet.Objects.Contains(parcels[0]));Assert.AreEqual(!fullPack,inv.Objects.Contains(parcels[0]));
                Assert.AreEqual(30,parcels[0].GetPart<PhysicsPart>().Weight);Assert.IsTrue(parcels[0].GetPart<PhysicsPart>().Takeable);
                Assert.IsFalse(TradeSystem.CanBeTraded(parcels[0],p,z.GetAllEntities().Single(e=>e.BlueprintName=="Merchant"),"Sell"));
                CollectionAssert.AreEquivalent(originals,z.GetAllEntities().Where(e=>e.BlueprintName=="PreFellingBody"));
                ConversationManager.StartConversation(sorter,p);Assert.IsFalse(CinderholdCompositionTests.ChoiceVisible("Is there work?"));Assert.AreEqual(0,TradeSystem.GetDrams(p));
            });
        }
        [TestCase(1,false)] [TestCase(16,true)]
        public void RealSealedParcelCostsCarryingCapacityAndDroppingItRestoresMovement(int strength,bool canCarry)
        {
            WithCourier((f,z,p)=>
            {
                p.Statistics["Strength"].BaseValue=strength;Accept(z.GetAllEntities().Single(e=>e.BlueprintName=="CurationSorter"),p);
                var from=z.GetEntityPosition(p);Assert.IsFalse(z.GetCell(from.x+1,from.y).BlocksMovement());
                Assert.AreEqual(canCarry,MovementSystem.TryMove(p,z,1,0));
                var parcel=p.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="SealedBogTakenBody");p.GetPart<InventoryPart>().RemoveObject(parcel);
                var at=z.GetEntityPosition(p);Assert.IsTrue(z.AddEntity(parcel,at.x,at.y));Assert.IsTrue(MovementSystem.TryMove(p,z,-1,0));
                Assert.IsTrue(StoryletPart.Current.IsQuestActive("BogBodyCourier"));Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
            });
        }
        [Test] public void DecliningThePlacedSorterStartsNoQuestAndCreatesNoParcel()
        {
            WithCourier((f,z,p)=>
            {ConversationManager.StartConversation(z.GetAllEntities().Single(e=>e.BlueprintName=="CurationSorter"),p);Assert.IsTrue(CinderholdCompositionTests.Choose("Is there work?"));Assert.IsTrue(CinderholdCompositionTests.Choose("No -- and I say it aloud."));
                Assert.IsFalse(StoryletPart.Current.IsQuestActive("BogBodyCourier"));Assert.IsFalse(p.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="SealedBogTakenBody"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="SealedBogTakenBody"));Assert.AreEqual(0,PlayerReputation.Get("PaleCuration"));});
        }
        public static void Accept(Entity sorter,Entity player)
        {ConversationManager.StartConversation(sorter,player);Assert.IsTrue(CinderholdCompositionTests.Choose("Is there work?"));Assert.IsTrue(CinderholdCompositionTests.Choose("I will carry it."));ConversationManager.EndConversation();}
        public static void WithCourier(Action<EntityFactory,Zone,Entity> test)
        {
            var oldFactory=ConversationActions.Factory;var oldZone=SettlementRuntime.ActiveZone;
            try
            {
                ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();StoryletRegistry.Reset();ConversationManager.EndConversation();
                StoryletPart.Current=new StoryletPart();StoryletPart.LocalPlayer=null;NarrativeStatePart.Current=new NarrativeStatePart();PlayerReputation.Reset();
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/FriendlyNPCs.json")));
                StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Storylets/BogBodyCourier.json")));
                var f=Factory();ConversationActions.Factory=f;var z=new OverworldZoneManager(f,64).GetZone(Id);SettlementRuntime.ActiveZone=z;
                var p=CinderholdCompositionTests.Player();var where=DryReach(z).First(c=>c.Item1>1&&c.Item1<77&&!z.GetCell(c.Item1-1,c.Item2).BlocksMovement()&&!z.GetCell(c.Item1+1,c.Item2).BlocksMovement());
                Assert.IsTrue(z.AddEntity(p,where.Item1,where.Item2));test(f,z,p);
            }
            finally{ConversationManager.EndConversation();ConversationLoader.Reset();StoryletRegistry.Reset();StoryletPart.Current=null;StoryletPart.LocalPlayer=null;NarrativeStatePart.Current=null;PlayerReputation.Reset();ConversationActions.Factory=oldFactory;SettlementRuntime.ActiveZone=oldZone;}
        }
        public static HashSet<(int,int)> DryReach(Zone z)
        {
            bool Open(int x,int y)=>z.InBounds(x,y)&&!z.GetCell(x,y).BlocksMovement()&&!z.GetCell(x,y).Objects.Any(e=>e.HasPart<LiquidPoolPart>());
            var seen=new HashSet<(int,int)>();var q=new Queue<(int,int)>();int y0=Enumerable.Range(0,25).First(y=>Open(0,y));seen.Add((0,y0));q.Enqueue((0,y0));
            while(q.Count>0){var c=q.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(c.Item1,c.Item2))if(Open(n.x,n.y)&&seen.Add(n))q.Enqueue(n);}return seen;
        }
        public static void AssertFourDirections(HashSet<(int,int)> seen)
        {Assert.IsTrue(seen.Any(c=>c.Item1==0));Assert.IsTrue(seen.Any(c=>c.Item1==79));Assert.IsTrue(seen.Any(c=>c.Item2==0));Assert.IsTrue(seen.Any(c=>c.Item2==24));}
        public static List<HashSet<(int,int)>> WaterComponents(Zone z)
        {
            var remaining=new HashSet<(int,int)>(z.GetAllEntities().Where(e=>e.HasPart<LiquidPoolPart>()).Select(e=>z.GetEntityPosition(e)));var result=new List<HashSet<(int,int)>>();
            while(remaining.Count>0){var component=new HashSet<(int,int)>();var q=new Queue<(int,int)>();var start=remaining.First();remaining.Remove(start);q.Enqueue(start);component.Add(start);
                while(q.Count>0){var c=q.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(c.Item1,c.Item2))if(remaining.Remove(n)){component.Add(n);q.Enqueue(n);}}result.Add(component);}return result;
        }
    }
}
