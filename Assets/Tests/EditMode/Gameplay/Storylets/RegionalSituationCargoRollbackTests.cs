using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class RegionalSituationCargoRollbackTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;

        [SetUp]
        public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            playerZone = null;
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            player.GetPart<InventoryPart>().MaxWeight = -1;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }


        // Fixture setup adds only an ordinary storage basket. Acquisition,
        // storage, retrieval and the complete saved graph use native commands.
        // A negative cue lookup while cargo is nested must not latch forever.
        [Test]
        public void FailedNativeTakeAfterTakenKeepsCueMissingAndRetryRecoversSameOwner()
        {
            var d=RegionalSituations.Find("sumphold-oil"); var b=Bind(d.Id);
            var source=manager.GetZone(d.SourceZoneId); var cargo=Source(source,d.Id);
            var at=source.GetEntityCell(cargo); MovePlayer(source,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,source).Success);
            var basket=factory.CreateEntity("WovenBasket"); Assert.NotNull(basket);
            var contents=basket.GetPart<ContainerPart>(); Assert.NotNull(contents);
            Assert.IsFalse(contents.Locked); Assert.AreEqual(0,contents.Contents.Count);
            Assert.IsTrue(source.AddEntity(basket,at.X,at.Y),"Ordinary basket is the only fixture placement.");
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PutInContainerCommand(basket,cargo),player,source).Success);
            Assert.IsTrue(contents.Contents.Contains(cargo));
            Assert.AreSame(basket,cargo.GetPart<PhysicsPart>().InInventory);
            string cargoId=cargo.ID,basketId=basket.ID,playerId=player.ID;
            StandBy(b.zone,b.recipient); RoundTripWorld(playerId);
            b=Bind(d.Id); StandBy(b.zone,b.recipient); source=manager.GetZone(d.SourceZoneId);
            basket=source.GetAllEntities().Single(e=>e.ID==basketId);
            contents=basket.GetPart<ContainerPart>();
            cargo=contents.Contents.Single(e=>e.ID==cargoId);
            Assert.AreSame(basket,cargo.GetPart<PhysicsPart>().InInventory,"Native saved graph restores nested ownership.");
            int cached=manager.CachedZoneCount,money=TradeSystem.GetDrams(player);
            Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone));
            Assert.IsFalse(b.request.CanAct(player,b.zone,"accept"));
            Assert.AreEqual(cached,manager.CachedZoneCount,"A cue cannot generate chunks to search for cargo.");
            at=source.GetEntityCell(basket); MovePlayer(source,at.X,at.Y);
            var probe=new TakenProbe();cargo.AddPart(probe);
            Assert.IsFalse(InventorySystem.ExecuteCommand(new FailAfter(new TakeFromContainerCommand(basket,cargo)),player,source).Success);
            Assert.AreEqual(1,probe.Calls,"The refused outer command must have reached Taken before rollback.");
            Assert.IsTrue(contents.Contents.Contains(cargo));
            Assert.AreSame(basket,cargo.GetPart<PhysicsPart>().InInventory);
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(cargo));
            StandBy(b.zone,b.recipient);
            Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone),"A cached pre-commit reference must not mean acquired cargo after rollback.");
            Assert.IsFalse(b.request.CanAct(player,b.zone,"accept"));
            at=source.GetEntityCell(basket);MovePlayer(source,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(basket,cargo),player,source).Success);
            Assert.AreEqual(2,probe.Calls);
            Assert.IsFalse(contents.Contents.Contains(cargo));
            Assert.AreSame(player,cargo.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1,player.GetPart<InventoryPart>().Objects.Count(e=>e.ID==cargoId));
            StandBy(b.zone,b.recipient);
            Assert.IsTrue(b.request.CanAct(player,b.zone,"accept"),"The native action sees the actual restored consignment.");
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone),"After native container retrieval the cue must agree with the real available action.");
            Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
            Assert.AreEqual(QuestCueState.Active,b.request.GetCueState(player,b.zone));
            Assert.IsTrue(b.request.TryAct(player,b.zone,"release"));
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone));
            Assert.AreEqual(cached,manager.CachedZoneCount);
            Assert.AreEqual(money,TradeSystem.GetDrams(player)); Assert.IsFalse(b.request.Completed);
            Assert.IsFalse(source.GetAllEntities().Any(e=>e.ID==cargoId),"Retrieval never regenerates a second cargo owner.");
        }
        private sealed class TakenProbe:Part
        {
            public override string Name=>"RegionalCargoTakenRollbackProbe";
            public int Calls;
            public override bool HandleEvent(GameEvent e){if(e.ID=="Taken")Calls++;return true;}
        }
        private sealed class FailAfter:IInventoryCommand
        {
            private readonly IInventoryCommand inner;
            public FailAfter(IInventoryCommand inner){this.inner=inner;}
            public string Name=>"RegionalCargoOuterRefusal";
            public InventoryValidationResult Validate(InventoryContext context)=>inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context,InventoryTransaction transaction)
            {
                var result=inner.Execute(context,transaction);
                return result.Success?InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,"Injected outer refusal."):result;
            }
        }
        private void RoundTripWorld(string playerId)
        {
            using(var stream=new MemoryStream())
            {
                // The session writes queued entity bodies and runs native load
                // hooks; the manager subrecord alone contains unresolved refs.
                var saved=GameSessionState.Capture("regional-adversarial", "test", manager, null, player);
                saved.Save(new SaveWriter(stream));stream.Position=0;
                var restored=GameSessionState.Load(new SaveReader(stream,factory));
                manager=restored.ZoneManager;
                Assert.AreEqual(playerId,restored.Player.ID);
            }
            playerZone=manager.CachedZones.Values.Single(z=>z.GetAllEntities().Any(e=>e.ID==playerId));
            player=playerZone.GetAllEntities().Single(e=>e.ID==playerId);
            StoryletPart.LocalPlayer=player;manager.SetActiveZone(playerZone);SettlementRuntime.ActiveZone=playerZone;
        }
        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); Assert.NotNull(d, id);
            var z = manager.GetZone(d.RecipientZoneId);
            var matches = z.GetAllEntities().Where(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Native recipient binding " + id);
            return (z, matches[0], matches[0].GetPart<RegionalRequestPart>());
        }

        private static Entity Source(Zone zone, string id)
        {
            string sourceId = RegionalSituations.SourceId(RegionalSituations.Find(id), Seed);
            var owners = zone.GetAllEntities().Where(e => e.ID == sourceId).ToArray();
            Assert.AreEqual(1, owners.Length, "Actual generated primary source for " + id);
            return owners[0];
        }

        private void StandBy(Zone zone, Entity owner)
        {
            var at = zone.GetEntityCell(owner); Assert.NotNull(at);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y)
                .Select(p => zone.GetCell(p.x, p.y)).FirstOrDefault(c => c != null && !c.BlocksMovement());
            Assert.NotNull(standing, "Native recipient needs a standing frontage.");
            MovePlayer(zone, standing.X, standing.Y);
        }

        private void MovePlayer(Zone zone, int x, int y)
        {
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, x, y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }

    }
}
