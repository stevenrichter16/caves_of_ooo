using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A mortal choice is the centre of this place. The native oath
    /// belongs to the guest, shade belongs to interiors, and poles confer neither.</summary>
    public class FirstTentCompositionTests
    {
        public const string Id="Overworld.5.17.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        public static readonly string[] Profile={"TentRightHost","TentRightHost","GuestClothPole","GuestClothPole","GuestClothPole","GuestClothPole","GuestClothPole","SaltMaster","Well"};
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        [Test] public void ExactFirstOathPlaceKeepsNativeRoadAndTierWhileSeedChangesUsefulLayout()
        {
            Assert.IsTrue(FirstTentCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(FirstTentCompositionPlan.Create(Id,64).Signature(),FirstTentCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(FirstTentCompositionPlan.Create(Id,64).Signature(),FirstTentCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Beating,WorldMapAuthoring.BiomeAt(5,17));Assert.AreEqual(3,WorldMapAuthoring.TierAt(5,17));Assert.IsTrue(WorldMapAuthoring.IsRoad(5,17));Assert.IsFalse(WorldMapAuthoring.IsRiver(5,17));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.5.17.1")] [TestCase("Overworld.5.17.-1")]
        [TestCase("Overworld.05.17.0")] [TestCase("Overworld.5.17.00")] [TestCase(" Overworld.5.17.0")]
        [TestCase("Overworld.5.17.0 ")] [TestCase("overworld.5.17.0")] [TestCase("Overworld.8.16.0")]
        [TestCase("Overworld.999999999999.17.0")] [TestCase("Overworld.5.17")]
        public void FiniteAddressNeverSpreadsFirstOathIntoOrdinaryClansOrDepths(string id)
        {Assert.IsFalse(FirstTentCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>FirstTentCompositionPlan.Create(id,64));}
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void OpenOathCourtIsDistinctFromShadedGuestAndSaltShelters(int seed)
        {
            var z=new Zone(Id);var b=new FirstTentCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));var p=b.Plan;Assert.AreSame(z,b.RealizedZone);
            CollectionAssert.AreEquivalent(new[]{"GuestThreshold","PilgrimRest","SaltService","Household"},p.Rooms.Select(r=>r.Role));
            Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            CollectionAssert.AreEquivalent(Profile,p.Profile.Select(o=>o.Blueprint));
            Assert.AreEqual(4,p.Profile.Count(o=>o.Blueprint=="GuestClothPole"&&p.IsOathCourt(o.X,o.Y)));
            Assert.AreEqual(1,p.Profile.Count(o=>o.Blueprint=="TentRightHost"&&p.IsOathCourt(o.X,o.Y)));
            Assert.AreEqual(0,z.GetAllEntities().Count(e=>Profile.Contains(e.BlueprintName)),"Profile publishes later, after cave roll.");
            var reach=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(reach);int court=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                if(p.IsOathCourt(x,y)){court++;Assert.IsFalse(c.IsInterior);Assert.IsNull(p.ObjectAt(x,y));Assert.IsTrue(p.IsReserved(x,y));Assert.IsTrue(reach.Contains((x,y)));}
                if(p.IsApproach(x,y)){Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(reach.Contains((x,y)));Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
            }
            Assert.GreaterOrEqual(court,85);Assert.IsTrue(p.IsOathCourt(p.OathX,p.OathY));Assert.IsFalse(z.GetCell(40,12).BlocksMovement());Assert.IsTrue(p.IsReserved(40,12));
            foreach(var room in p.Rooms)
            {
                Assert.IsTrue(reach.Contains((room.DoorX,room.DoorY)));Assert.IsTrue(p.IsApproach(room.DoorX,room.DoorY));
                for(int y=room.Y+1;y<room.Y+room.Height-1;y++)for(int x=room.X+1;x<room.X+room.Width-1;x++)
                {Assert.IsTrue(cellsInterior(z,x,y));Assert.AreEqual("StoneFloor",p.GroundAt(x,y));}
            }
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Bed"));Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Crate"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
        }
        private static bool cellsInterior(Zone z,int x,int y)=>z.GetCell(x,y).IsInterior;
        [Test] public void NineProfileOwnersPublishOnceWithTwoOrdinaryHostsAndNoFabricatedPoleAura()
        {
            var z=new Zone(Id);var f=Factory();var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new FirstTentProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(Profile,z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)).Select(e=>e.BlueprintName));
            foreach(var host in z.GetAllEntities().Where(e=>e.BlueprintName=="TentRightHost"))
            {Assert.AreEqual("TentRightHost_1",host.GetPart<ConversationPart>().ConversationID);Assert.IsTrue(host.HasPart<AISelfPreservationPart>());Assert.IsFalse(host.HasPart<TraderPart>());Assert.AreEqual("TentRight",host.GetTag("Faction"));}
            foreach(var pole in z.GetAllEntities().Where(e=>e.BlueprintName=="GuestClothPole"))
            {Assert.IsFalse(pole.GetPart<PhysicsPart>().Solid);Assert.IsFalse(pole.GetPart<PhysicsPart>().Takeable);Assert.IsFalse(pole.HasPart<DestructiblePart>());Assert.IsFalse(pole.HasPart<ConversationPart>());}
            foreach(var e in z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)))Assert.AreEqual(Id,e.GetProperty("SettlementId"));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FullNativeVillageKeepsTwoWellsRealServicesAndReachableOathFrontages(int seed)
        {
            var m=new OverworldZoneManager(Factory(),seed);var pipeline=CinderholdCompositionTests.Pipeline(m,Id);
            Assert.AreEqual(1,pipeline.Builders.OfType<FirstTentCompositionBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<FirstTentProfileBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<FirstTentArrivalReservationBuilder>().Count());
            Assert.IsFalse(pipeline.Builders.OfType<RiverChunkBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<VillagePopulationBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<TradeStockBuilder>().Any());
            var z=m.GetZone(Id);var p=FirstTentCompositionPlan.Create(Id,seed);
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="Well"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="TentRightHost"));Assert.AreEqual(5,z.GetAllEntities().Count(e=>e.BlueprintName=="GuestClothPole"));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="SaltMaster"));
            Assert.AreEqual(1,z.GetCell(40,12).Objects.Count(e=>e.BlueprintName=="Well"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
            foreach(var bp in new[]{"Merchant","Quartermaster","Scribe","Elder","Innkeeper"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.IsFalse(z.GetAllEntities().Where(e=>e.BlueprintName=="Shrine").Any(e=>{var at=z.GetEntityPosition(e);return p.IsOathCourt(at.x,at.y);}),"Ordinary native shrine cannot become the first-oath monument.");
            var services=z.GetAllEntities().Where(e=>Profile.Contains(e.BlueprintName)||new[]{"Shrine","Merchant","Quartermaster"}.Contains(e.BlueprintName)).Select(e=>(e.BlueprintName,z.GetEntityPosition(e))).ToArray();
            foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
            var reached=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(reached);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(reached.Contains((x,y)),"Late pocket "+x+","+y);
            foreach(var service in services)Assert.IsTrue(CinderholdCompositionTests.Neighbors(service.Item2.x,service.Item2.y).Any(reached.Contains),service.BlueprintName);
            foreach(var r in p.Rooms)Assert.IsFalse(z.GetCell(r.DoorX,r.DoorY).BlocksMovement());
        }
        [Test] public void RealPlacedHostsAllowRefusalClaimRefreshAndClockExpiryWithoutAProximityAura()
        {
            WithGuest((f,z,player)=>
            {
                var hosts=z.GetAllEntities().Where(e=>e.BlueprintName=="TentRightHost").ToArray();Assert.AreEqual(2,hosts.Length);
                Assert.IsFalse(player.HasEffect<UnderTheClothEffect>());ConversationManager.StartConversation(hosts[0],player);Assert.IsTrue(CinderholdCompositionTests.Choose("No —"));Assert.IsFalse(player.HasEffect<UnderTheClothEffect>());ConversationManager.EndConversation();
                Claim(hosts[0],player);var oath=player.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>();Assert.NotNull(oath);Assert.AreEqual(WorldClock.CurrentTick+UnderTheClothEffect.OathTicks,oath.ExpiryTick);
                var beast=new Entity();beast.SetTag("Faction","Beasts");Assert.IsTrue(UnderTheClothEffect.Protects(hosts[1],player));Assert.IsFalse(UnderTheClothEffect.Protects(beast,player));
                oath.ExpiryTick=WorldClock.CurrentTick+1;Claim(hosts[1],player);Assert.AreSame(oath,player.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>());Assert.AreEqual(WorldClock.CurrentTick+UnderTheClothEffect.OathTicks,oath.ExpiryTick);
                oath.ExpiryTick=0;var ev=GameEvent.New("EndTurn");ev.SetParameter("Zone",(object)z);player.FireEvent(ev);ev.Release();Assert.IsFalse(player.HasEffect<UnderTheClothEffect>());Assert.IsFalse(UnderTheClothEffect.Protects(hosts[0],player));Assert.AreEqual(0,PlayerReputation.Get("TentRight"));
            });
        }
        [Test] public void PlacedSaltMasterExchangesOnlyRealSaltForStandingAndNeverMintsDrams()
        {
            WithGuest((f,z,player)=>
            {
                var master=z.GetAllEntities().Single(e=>e.BlueprintName=="SaltMaster");var inv=player.GetPart<InventoryPart>();var wrong=f.CreateEntity("ChoirIron");Assert.NotNull(wrong);Assert.IsTrue(inv.AddObject(wrong));
                ConversationManager.StartConversation(master,player);Assert.IsFalse(CinderholdCompositionTests.ChoiceVisible("Weigh this salt."));Assert.IsFalse(MineralTradeService.TryTrade(player,master,"ChoirIron"));Assert.IsTrue(inv.Objects.Contains(wrong));ConversationManager.EndConversation();
                Assert.IsTrue(inv.AddObject(f.CreateEntity("PaleSalt")));int drams=TradeSystem.GetDrams(player);ConversationManager.StartConversation(master,player);Assert.IsTrue(CinderholdCompositionTests.Choose("Weigh this salt."));
                Assert.IsFalse(inv.Objects.Any(e=>e.BlueprintName=="PaleSalt"));Assert.AreEqual(5,PlayerReputation.Get("TentRight"));Assert.AreEqual(drams,TradeSystem.GetDrams(player));Assert.IsFalse(MineralTradeService.TryTrade(player,master,"PaleSalt"));Assert.AreEqual(5,PlayerReputation.Get("TentRight"));
            });
        }
        public static void Claim(Entity host,Entity player)
        {ConversationManager.StartConversation(host,player);Assert.IsTrue(CinderholdCompositionTests.Choose("I sit, and I am your guest."));ConversationManager.EndConversation();}
        public static void WithGuest(Action<EntityFactory,Zone,Entity> run)
        {
            var oldFactory=ConversationActions.Factory;var oldZone=SettlementRuntime.ActiveZone;
            try
            {
                ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();ConversationManager.EndConversation();PlayerReputation.Reset();FactionManager.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Factions.json")));
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/FriendlyNPCs.json")));
                var f=Factory();ConversationActions.Factory=f;var z=new OverworldZoneManager(f,64).GetZone(Id);SettlementRuntime.ActiveZone=z;var player=CinderholdCompositionTests.Player();player.SetTag("Creature");var at=DrownedLedgerCompositionTests.DryReach(z).First();Assert.IsTrue(z.AddEntity(player,at.Item1,at.Item2));run(f,z,player);
            }
            finally{ConversationManager.EndConversation();ConversationLoader.Reset();PlayerReputation.Reset();FactionManager.Reset();ConversationActions.Factory=oldFactory;SettlementRuntime.ActiveZone=oldZone;}
        }
    }
}
