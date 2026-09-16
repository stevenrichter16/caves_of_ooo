using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GantryCompositionTests
    {
        public const string Id="Overworld.7.8.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        [SetUp] public void Load()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void Reset()=>LootTableRegistry.ResetForTests();
        [Test] public void NativeRoadCrossingKeepsItsActualMapAndExplicitProfile()
        {
            var m=new OverworldZoneManager(Factory(),64);var poi=m.WorldMap.GetPOI(7,8);
            Assert.AreEqual("CrossroadsExchange",GantryCompositionPlan.ProfileID);Assert.AreEqual(GantryCompositionPlan.ProfileID,poi.Profile);
            Assert.AreEqual(POIType.Village,poi.Type);Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(7,8));Assert.AreEqual(1,WorldMapAuthoring.TierAt(7,8));
            Assert.IsTrue(WorldMapAuthoring.IsRoad(7,8));Assert.IsFalse(WorldMapAuthoring.IsRiver(7,8));
            Assert.AreEqual(GantryCompositionPlan.Create(Id,64).Signature(),GantryCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(GantryCompositionPlan.Create(Id,64).Signature(),GantryCompositionPlan.Create(Id,1729).Signature());
        }
        [TestCase(null)][TestCase("")][TestCase("Overworld.7.8.1")][TestCase("Overworld.07.8.0")]
        [TestCase("Overworld.7.08.0")][TestCase("Overworld.7.8.00")][TestCase("overworld.7.8.0")][TestCase("Overworld.7.8.0 ")][TestCase("Overworld.8.8.0")]
        public void OnlyTheExactFreshSurfaceAddressIsClaimed(string id)
        {Assert.IsFalse(GantryCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>GantryCompositionPlan.Create(id,64));}
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void CrossingConnectsFourUnequalWorkingRoomsAndLeavesPublicSpace(int seed)
        {
            var z=new Zone(Id);var b=new GantryCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));Assert.AreEqual(1000,b.Priority);
            var p=b.Plan;Assert.AreSame(z,b.RealizedZone);CollectionAssert.AreEquivalent(new[]{"ExchangeFloor","RegistryOffice","CaravanRest","Household"},p.Rooms.Select(r=>r.Role));
            Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            var reach=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(reach);int open=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                Assert.AreEqual(1,z.GetCell(x,y).Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                Assert.AreEqual(p.IsInterior(x,y),z.GetCell(x,y).IsInterior);
                if(p.IsApproach(x,y)){Assert.IsTrue(reach.Contains((x,y)));Assert.IsFalse(z.GetCell(x,y).BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(!p.IsInterior(x,y)&&p.ObjectAt(x,y)==null)open++;
            }
            Assert.Greater(open,800);Assert.IsTrue(p.IsReserved(40,12));Assert.IsNull(p.ObjectAt(40,12));
            foreach(var room in p.Rooms){Assert.IsTrue(reach.Contains((room.DoorX,room.DoorY)));Assert.IsTrue(p.IsApproach(room.DoorX,room.DoorY));}
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="GantryRegistryDesk"));Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="GantryExchangeCounter"));
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Bed"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
        }
        [Test] public void DifferentFormationNamesChangeActualRoleRelationships()
        {
            var names=new HashSet<string>();var pairs=new HashSet<string>();
            foreach(int seed in Enumerable.Range(0,64))
            {
                var p=GantryCompositionPlan.Create(Id,seed);names.Add(p.FormationName);
                var ex=p.Rooms.Single(r=>r.Role=="ExchangeFloor");var rest=p.Rooms.Single(r=>r.Role=="CaravanRest");
                pairs.Add((ex.X<40)+":"+(ex.Y<12)+":"+(rest.Y<12));
            }
            Assert.AreEqual(3,names.Count);Assert.AreEqual(3,pairs.Count,"Formation labels must represent different useful adjacencies, not wall jitter.");
        }
        [Test] public void ProfilePublishesRealOrdinaryClerkAndHospitalityOnlyOnce()
        {
            var f=Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="GantryRegistrar"));var profile=new GantryProfileBuilder(b);Assert.AreEqual(3860,profile.Priority);Assert.IsTrue(profile.BuildZone(z,f,new Random(1)));
            var clerk=z.GetAllEntities().Single(e=>e.BlueprintName=="GantryRegistrar");Assert.AreEqual("GantryRegistrar_1",clerk.GetPart<ConversationPart>().ConversationID);Assert.AreEqual("PaleCuration",clerk.GetTag("Faction"));Assert.AreEqual(Id,clerk.GetProperty("SettlementId"));
            Assert.IsFalse(clerk.HasPart<TraderPart>());Assert.IsFalse(clerk.HasPart<WantsMineralPart>());Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="TentRightHost"));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="GuestClothPole"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="FilerClerk"||e.BlueprintName=="ConcordFactor"));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(profile.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void FullVillagePreservesServicesAndEveryDryStandingAreaAfterLatePopulation(int seed)
        {
            var m=new OverworldZoneManager(Factory(),seed);var pipeline=CinderholdCompositionTests.Pipeline(m,Id);
            Assert.AreEqual(1,pipeline.Builders.OfType<GantryCompositionBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<GantryProfileBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<GantryArrivalReservationBuilder>().Count());
            Assert.IsFalse(pipeline.Builders.OfType<RiverChunkBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<VillagePopulationBuilder>().Single().RespectInteriorReservations);
            var z=m.GetZone(Id);foreach(var bp in new[]{"Merchant","Quartermaster","Scribe","Innkeeper","GantryRegistrar","TentRightHost"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(1,z.GetCell(40,12).Objects.Count(e=>e.BlueprintName=="Well"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
            foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
            var seen=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(seen);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen.Contains((x,y)),"Late pocket "+x+","+y);
        }
        [TestCase("GantryTimberWall","Wood",false)][TestCase("GantryRegistryDesk","Wood",false)]
        [TestCase("GantryExchangeCounter","Wood",true)][TestCase("GantryWayboard","Wood",false)]
        public void NewFixturesAreNativeDestructibleOwnersWithHonestMaterials(string bp,string material,bool storage)
        {
            var e=Factory().CreateEntity(bp);Assert.NotNull(e);Assert.AreEqual(material,e.GetPart<MaterialPart>().MaterialID);var d=e.GetPart<DestructiblePart>();Assert.NotNull(d);Assert.Greater(d.HP,0);Assert.IsFalse(d.Indestructible);Assert.IsFalse(d.Gone);
            Assert.AreEqual(storage,e.HasPart<ContainerPart>());Assert.IsFalse(e.HasPart<SealedLibraryBarrierPart>());Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);Assert.IsTrue(e.GetPart<PhysicsPart>().Solid);
        }
        [TestCase("GantryTimberWall")][TestCase("GantryRegistryDesk")][TestCase("GantryExchangeCounter")][TestCase("GantryWayboard")]
        public void NativeDestructionRemovesOnePhysicalOwnerAndNeverRegrowsIt(string bp)
        {
            var f=Factory();var z=new OverworldZoneManager(f,64).GetZone(Id);var owner=z.GetAllEntities().First(e=>e.BlueprintName==bp);var position=z.GetEntityPosition(owner);
            var box=owner.GetPart<ContainerPart>();Entity contents=null;if(box!=null){contents=f.CreateEntity("InkVial");Assert.IsTrue(box.AddItem(contents));}
            int count=z.EntityCount;Assert.IsTrue(DestructionSystem.IsBreakable(owner));Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,10000,null,z));Assert.IsNull(z.GetEntityCell(owner));
            if(contents!=null)Assert.AreEqual(position,z.GetEntityPosition(contents));
            var remaining=z.GetAllEntities().ToArray();DestructionSystem.Damage(owner,10000,null,z);CollectionAssert.AreEquivalent(remaining,z.GetAllEntities());
            Assert.IsFalse(new GantryCompositionBuilder(64).BuildZone(z,f,new Random(64)));Assert.IsFalse(z.GetCell(position.x,position.y).Objects.Contains(owner));
        }
        [Test] public void RegistrarExplainsTheCrossingWithoutTakingCourierGoodsOrGrantingStanding()
        {
            var previous=ConversationActions.Factory;
            try
            {
                ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();ConversationManager.EndConversation();PlayerReputation.Reset();
                var f=Factory();ConversationActions.Factory=f;ConversationLoader.LoadFromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/Gantry.json")));
                var z=new OverworldZoneManager(f,64).GetZone(Id);var clerk=z.GetAllEntities().Single(e=>e.BlueprintName=="GantryRegistrar");var player=CinderholdCompositionTests.Player();var inv=player.GetPart<InventoryPart>();var carried=f.CreateEntity("InkVial");Assert.IsTrue(inv.AddObject(carried));int drams=TradeSystem.GetDrams(player);
                ConversationManager.StartConversation(clerk,player);Assert.IsTrue(CinderholdCompositionTests.Choose("Is this an intake office?"));Assert.IsTrue(CinderholdCompositionTests.Choose("Understood."));Assert.IsTrue(CinderholdCompositionTests.Choose("Who shares this crossing?"));Assert.IsTrue(CinderholdCompositionTests.Choose("Back."));
                Assert.AreEqual(0,PlayerReputation.Get("PaleCuration"));Assert.AreEqual(drams,TradeSystem.GetDrams(player));Assert.IsTrue(inv.Objects.Contains(carried));Assert.IsFalse(clerk.HasPart<WantsMineralPart>());
            }
            finally{ConversationManager.EndConversation();ConversationLoader.Reset();ConversationActions.Factory=previous;PlayerReputation.Reset();}
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void ExchangeIsAnOpenFrontedWorkingFloorWhileRecordsKeepAPrivateThreshold(int seed)
        {
            var p=GantryCompositionPlan.Create(Id,seed);var market=p.Rooms.Single(r=>r.Role=="ExchangeFloor");var office=p.Rooms.Single(r=>r.Role=="RegistryOffice");
            Assert.GreaterOrEqual(Enumerable.Range(market.X,market.Width).Count(x=>p.ObjectAt(x,market.DoorY)==null),market.Width-2,"Trade should spill into the crossing instead of hiding behind a bedroom doorway.");
            Assert.AreEqual(3,Enumerable.Range(office.X,office.Width).Count(x=>p.ObjectAt(x,office.DoorY)==null));
        }
    }
}
