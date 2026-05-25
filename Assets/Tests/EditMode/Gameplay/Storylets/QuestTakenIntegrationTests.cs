using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// END-TO-END integration for Q5.2/Q5.3 (rule-7 validation): drives the
    /// FULL production chain — the real PickupCommand / TakeFromContainerCommand
    /// fire the M1 "Taken" event, which the item's CompleteObjectiveOnTaken /
    /// QuestStarter Part hooks, which routes to the real StoryletPart. This
    /// closes the seam the per-milestone tests leave open (M1 proves the
    /// command fires Taken; M2/M3 fire a synthetic Taken — this proves the
    /// whole chain with the Part actually ON the item picked up by the real
    /// command). Everything here is production code, not stubs.
    ///
    /// Honesty bound: this exercises the command layer the 'g'-key / pickup
    /// UI invokes; it does NOT separately drive the live input/bootstrap
    /// layer (which only wires StoryletPart.LocalPlayer = _player, confirmed
    /// at GameBootstrap.cs:262) or the quest-log visual (Q1 covered that).
    /// </summary>
    public class QuestTakenIntegrationTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        private static Entity Player()
        {
            var a = new Entity { ID = "player", BlueprintName = "Player" };
            a.Tags["Creature"] = "";
            a.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            a.AddPart(new InventoryPart { MaxWeight = 150 });
            return a;
        }

        private static StoryletPart RegisterQuest(string id, params string[] objs)
        {
            var sd = new StoryletData { ID = id, Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "s0" };
            foreach (var o in objs) s0.Objectives.Add(new QuestObjectiveData { ID = o });
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
            StoryletRegistry.Register(sd);
            var sp = new StoryletPart();
            StoryletPart.Current = sp;
            return sp;
        }

        [Test]
        public void RealPickup_CompleteObjectiveOnTaken_FinishesObjective()
        {
            var zone = new Zone();
            var sp = RegisterQuest("Q", "find_relic", "other");
            var player = Player();
            StoryletPart.LocalPlayer = player; // the picker IS the player
            zone.AddEntity(player, 5, 5);
            sp.StartQuest(new QuestState { QuestId = "Q" });

            var relic = new Entity { ID = "relic", BlueprintName = "Enchiridion" };
            relic.Tags["Item"] = "";
            relic.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            relic.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "find_relic" });
            zone.AddEntity(relic, 5, 5);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(relic), new InventoryContext(player, zone));

            Assert.IsTrue(result.Success, "the real pickup succeeds");
            Assert.IsTrue(sp.IsObjectiveFinished("Q", "find_relic"),
                "real pickup → Taken → CompleteObjectiveOnTaken → objective finished (full chain)");
        }

        [Test]
        public void RealPickup_QuestStarter_StartsQuest()
        {
            var zone = new Zone();
            var sp = RegisterQuest("Q", "obj");
            var player = Player();
            StoryletPart.LocalPlayer = player;
            zone.AddEntity(player, 5, 5);

            var scroll = new Entity { ID = "scroll", BlueprintName = "QuestScroll" };
            scroll.Tags["Item"] = "";
            scroll.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            scroll.AddPart(new QuestStarter { Quest = "Q" });
            zone.AddEntity(scroll, 5, 5);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(scroll), new InventoryContext(player, zone));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(sp.IsQuestActive("Q"),
                "real pickup → Taken → QuestStarter → quest started (full chain)");
            Assert.IsTrue(scroll.GetPart<QuestStarter>().Activated, "starter spent itself");
        }

        [Test]
        public void RealPickup_ByNonPlayerActor_DoesNotAdvance()
        {
            // Counter-check through the real command: an NPC picking the item
            // up (NPC != LocalPlayer) must not finish the objective.
            var zone = new Zone();
            var sp = RegisterQuest("Q", "find_relic");
            var player = Player();
            StoryletPart.LocalPlayer = player; // player exists, but the NPC picks it up
            var npc = Player(); npc.ID = "npc";
            zone.AddEntity(npc, 6, 6);
            sp.StartQuest(new QuestState { QuestId = "Q" });

            var relic = new Entity { ID = "relic" };
            relic.Tags["Item"] = "";
            relic.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            relic.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "find_relic" });
            zone.AddEntity(relic, 6, 6);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(relic), new InventoryContext(npc, zone));

            Assert.IsTrue(result.Success, "the NPC still picks it up");
            Assert.IsFalse(sp.IsObjectiveFinished("Q", "find_relic"),
                "but a non-player pickup must not finish the objective");
        }
    }
}
