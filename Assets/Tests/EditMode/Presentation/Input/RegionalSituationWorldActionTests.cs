using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class RegionalSituationWorldActionTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;
        private GameObject inputObject;
        private InputHandler input;
        private TurnManager turns;

        private static readonly string[] BindingIds =
        {
            "morrowfast-iron", "cinderhold-iron", "gantry-grain",
            "sumphold-oil", "wellmeet-filters"
        };

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
            if(inputObject!=null)UnityEngine.Object.DestroyImmediate(inputObject);
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }

        // RS25 showed that the real C-menu action dispatcher bypasses the
        // inventory-command transaction. These invoke that exact private seam
        // with actions gathered from the actual native actor, not a substitute
        // event/command helper. Both original town and ordinary native merchant.
        [TestCase("morrowfast-iron")][TestCase("gantry-grain")]
        public void NativeWorldMenuPreviewsAcceptsAndReleasesWithoutChargingGenericActionTurn(string id)
        {
            var b=Bind(id);StandBy(b.zone,b.recipient);BuildInput(b.zone);
            int tick=turns.TickCount;var probe=new ActionProbe();player.AddPart(probe);
            Select(b.zone,b.recipient,"RegionalRequest:read");
            Assert.IsFalse(b.request.Accepted);Assert.IsEmpty(RegionalSituationNotes.Read(player));
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone));
            Select(b.zone,b.recipient,"RegionalRequest:accept");
            Assert.IsTrue(b.request.Accepted,"Explicit C acceptance must undertake the native request.");
            Assert.AreEqual(QuestCueState.Active,b.request.GetCueState(player,b.zone));
            Assert.AreEqual(1,RegionalSituationNotes.Read(player).Count);
            Select(b.zone,b.recipient,"RegionalRequest:release");
            Assert.IsFalse(b.request.Accepted);Assert.IsFalse(b.request.Completed);
            Assert.AreEqual(3,probe.Before);Assert.AreEqual(3,probe.After);
            Assert.AreEqual(tick,turns.TickCount,"These C actions retain the generic zero-turn policy.");
        }

        [TestCase(false)][TestCase(true)]
        public void NativeWorldMenuDeliveryCommitsOnlyAfterSuccessfulNativePostAction(bool failAfter)
        {
            var d=RegionalSituations.Find("gantry-grain");var b=Bind(d.Id);StandBy(b.zone,b.recipient);
            // Acceptance is established independently so this test reaches the
            // delivery seam even while the separate preview/accept case is still RED.
            Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));Carry(d.ItemBlueprint,d.ItemCount);
            BuildInput(b.zone);var probe=new ActionProbe{FailDelivery=failAfter};player.AddPart(probe);
            int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,d.ItemBlueprint),tick=turns.TickCount;
            MessageLog.Clear();Select(b.zone,b.recipient,"RegionalRequest:deliver");
            Assert.AreEqual(1,probe.Before);Assert.AreEqual(1,probe.After,"Native AfterInventoryAction must execute.");
            Assert.AreEqual(!failAfter,b.request.Completed);Assert.AreEqual(failAfter,b.request.Accepted);
            Assert.AreEqual(failAfter?d.ItemCount:0,Units(player,d.ItemBlueprint));
            Assert.AreEqual(stock+(failAfter?0:d.ItemCount),Units(b.recipient,d.ItemBlueprint));
            Assert.AreEqual(money+(failAfter?0:d.RewardDrams),TradeSystem.GetDrams(player));
            Assert.AreEqual(!failAfter,MessageLog.GetMessages().Any(m=>m.StartsWith("Delivered:",StringComparison.Ordinal)));
            Assert.AreEqual(tick,turns.TickCount);
            if(failAfter)
            {
                probe.FailDelivery=false;Select(b.zone,b.recipient,"RegionalRequest:deliver");
                Assert.IsTrue(b.request.Completed);Assert.AreEqual(money+d.RewardDrams,TradeSystem.GetDrams(player));
            }
        }

        [Test] public void OrdinaryExamineStillUsesItsDirectWorldEventAndRemainsFree()
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);BuildInput(b.zone);
            var probe=new ActionProbe();player.AddPart(probe);int tick=turns.TickCount;MessageLog.Clear();
            Select(b.zone,b.recipient,"Examine");
            Assert.Greater(MessageLog.Count,0,"The ordinary native examine still executes.");
            Assert.AreEqual(0,probe.Before);Assert.AreEqual(0,probe.After,"Do not route unrelated verbs through new command hooks.");
            Assert.AreEqual(tick,turns.TickCount);Assert.IsFalse(b.request.Accepted);
        }

        [TestCase("read")][TestCase("accept")] public void StaleRequestMenuCannotBypassTheWorldReachGate(string actionName)
        {
            var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);BuildInput(b.zone);
            var action=WorldInteractionSystem.GatherActions(b.recipient,player).Single(x=>x.Command=="RegionalRequest:"+actionName);
            var owner=b.zone.GetEntityCell(b.recipient);
            var far=Enumerable.Range(0,Zone.Width*Zone.Height).Select(i=>b.zone.GetCell(i%Zone.Width,i/Zone.Width))
                .First(c=>!c.BlocksMovement()&&Math.Abs(c.X-owner.X)+Math.Abs(c.Y-owner.Y)>12);
            Assert.IsTrue(b.zone.MoveEntity(player,far.X,far.Y));var probe=new ActionProbe();player.AddPart(probe);
            int tick=turns.TickCount;InvokeSelection(action,b.recipient,owner);
            Assert.IsFalse(b.request.Accepted);Assert.IsEmpty(RegionalSituationNotes.Read(player));
            Assert.AreEqual(0,probe.Before);Assert.AreEqual(0,probe.After);Assert.AreEqual(tick,turns.TickCount);
        }

        private void BuildInput(Zone zone)
        {
            inputObject=new GameObject("regional-native-world-action-test");input=inputObject.AddComponent<InputHandler>();
            input.PlayerEntity=player;input.CurrentZone=zone;input.ZoneManager=manager;input.WorldMap=manager.WorldMap;
            turns=new TurnManager();turns.AddEntity(player);turns.ProcessUntilPlayerTurn();input.TurnManager=turns;
            var field=typeof(InputHandler).GetField("_worldActionMenuReturnState",BindingFlags.Instance|BindingFlags.NonPublic);
            field.SetValue(input,Enum.Parse(field.FieldType,"Normal"));
        }
        private void Select(Zone zone,Entity recipient,string command)
        {
            var action=WorldInteractionSystem.GatherActions(recipient,player).Single(a=>a.Command==command);
            InvokeSelection(action,recipient,zone.GetEntityCell(recipient));
        }
        private void InvokeSelection(InventoryAction action,Entity target,Cell cell)
        {
            var method=typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method);method.Invoke(input,new object[]{action,target,cell,false});
            var state=typeof(InputHandler).GetField("_inputState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
            Assert.AreEqual("Normal",state.ToString(),"Every completion/refusal returns to the originating C context.");
        }
        private sealed class ActionProbe:Part
        {
            public override string Name=>"RegionalWorldMenuProbe";
            public int Before,After;public bool FailDelivery;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="BeforeInventoryAction")Before++;
                if(e.ID=="AfterInventoryAction")
                {
                    After++;
                    if(FailDelivery&&e.GetStringParameter("Command")=="RegionalRequest:deliver")throw new InvalidOperationException("Native C post-action refusal.");
                }
                return true;
            }
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

        private void Carry(string blueprint, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = factory.CreateEntity(blueprint); Assert.NotNull(item, blueprint);
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item), blueprint);
            }
        }

        private static int Units(Entity owner, string blueprint) => owner.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint && ReferenceEquals(e.GetPart<PhysicsPart>()?.InInventory, owner))
            .Sum(e => Math.Max(0, e.GetPart<StackerPart>()?.StackCount ?? 1));
    }
}
