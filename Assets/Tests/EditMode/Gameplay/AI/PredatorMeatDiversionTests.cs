using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    // Synthetic geometry and placement; real factory owners, BoredGoal turns,
    // physical food and replacement-graph saves. These are not native-input claims.
    public sealed partial class PredatorMeatDiversionTests
    {
        SpreadExplorationActorTests.Scope scope;
        EntityFactory factory;
        Zone zone;
        Entity hunter, prey, player, meat;
        SpreadPredatorPart role;

        [SetUp] public void Setup()
        {
            scope = new SpreadExplorationActorTests.Scope();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            zone = new Zone("Overworld.8.8.0");
            hunter = Place("Furrowstalker", 10, 10);
            prey = Place("ReedbackGrazer", 10, 16);
            player = Place("Player", 60, 20);
            role = hunter.GetPart<SpreadPredatorPart>();
        }
        [TearDown] public void Cleanup() => scope?.Dispose();

        Entity Place(string blueprint, int x, int y)
        {
            var owner = factory.CreateEntity(blueprint);
            Assert.NotNull(owner, blueprint); Assert.True(zone.AddEntity(owner, x, y));
            var brain = owner.GetPart<BrainPart>();
            if (brain != null) { brain.CurrentZone = zone; brain.Rng = new Random(1); }
            return owner;
        }
        void Configure(bool enabled = true)
        {
            var method = typeof(SpreadPredatorPart).GetMethod("Configure");
            // Before implementation, execute the old admission so positive
            // behavioral cases fail on ignored meat while identical controls run.
            object[] args = method.GetParameters().Length == 3
                ? new object[] { zone, prey, enabled } : new object[] { zone, prey };
            Assert.True((bool)method.Invoke(role, args), "actual local pair admitted");
        }
        T Field<T>(string name, T absent = default) => typeof(SpreadPredatorPart).GetField(name) is FieldInfo field
            ? (T)field.GetValue(role) : absent;
        void Set(string name, object value)
        {
            var field = typeof(SpreadPredatorPart).GetField(name);
            Assert.NotNull(field, "Missing saved field " + name); field.SetValue(role, value);
        }
        string MeatPhase => typeof(SpreadPredatorPart).GetField("MeatDiversionPhase")?.GetValue(role)?.ToString() ?? "None";
        static void Turn(Entity actor) => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        int Units(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        Entity Meat(string blueprint = "RawMeat", int count = 1, int x = 11, int y = 10)
        {
            meat = Place(blueprint, x, y); Assert.NotNull(meat.GetPart<StackerPart>());
            meat.GetPart<StackerPart>().StackCount = count; return meat;
        }
        string HuntState() => string.Join("|", role.Phase, role.HasLastSeen, role.LastSeenX, role.LastSeenY,
            role.PursuitRemaining, role.SearchRemaining, role.Prey?.ID, prey.GetPart<SpreadGrazerPart>().Hunter?.ID);

        [TestCase("RawMeat", 1, true)] [TestCase("RawMeat", 1, false)]
        [TestCase("DriedMeat", 3, true)] [TestCase("DriedMeat", 3, false)]
        public void ThreeAdjacentActionsSpendExactlyOneUnitOnlyWhenEnabled(string blueprint, int count, bool enabled)
        {
            Configure(enabled); Meat(blueprint, count);
            string before = HuntState(); int hp = hunter.GetStatValue("Hitpoints"), poses = 0;
            EntityVisualHooks.InteractionCallback = (actor, target, current) =>
            { if (actor == hunter) { Assert.AreSame(meat, target); Assert.NotNull(current.GetEntityCell(target)); poses++; } };
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.AreEqual(enabled && count == 1, zone.GetEntityCell(meat) == null);
            Assert.AreEqual(enabled && count > 1 ? count - 1 : count, Units(meat));
            Assert.AreEqual(enabled ? 2 : 0, poses);
            Assert.AreEqual(hp, hunter.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, hunter.GetPart<InventoryPart>().Objects.Count);
            if (!enabled) { Assert.AreNotEqual(before, HuntState()); return; }
            Assert.AreEqual(before, HuntState(), "diversion leaves original hunt memory and budgets intact");
            Assert.True(Field<bool>("MeatDiversionAttempted")); Assert.AreEqual("Consumed", MeatPhase);
            Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
            var second = Place(blueprint, 10, 11);
            Turn(hunter); Assert.NotNull(zone.GetEntityCell(second)); Assert.AreEqual(1, Units(second));
            Assert.Less(role.PursuitRemaining, 24, "spent diversion resumes ordinary finite hunt");
        }

        [Test] public void VisibleMeatApproachUsesOneRealMovementAndKeepsQuarryMemory()
        {
            Configure(); Meat(x: 15); string before = HuntState();
            Turn(hunter);
            Assert.AreEqual((11, 10), zone.GetEntityPosition(hunter));
            Assert.AreEqual(before, HuntState()); Assert.AreEqual("Approaching", MeatPhase);
            Assert.AreSame(meat, Field<Entity>("MeatDiversionTarget"));
            Assert.AreEqual(5, Field<int>("MeatApproachRemaining"));
        }

        [Test] public void SearchMemoryResumesWithoutRefillingTheOriginalAllowance()
        {
            Configure(); Meat(); role.Phase = SpreadHuntPhase.Searching;
            role.HasLastSeen = true; role.LastSeenX = 10; role.LastSeenY = 15;
            role.PursuitRemaining = 11; role.SearchRemaining = 3;
            string before = HuntState();
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.AreEqual(before, HuntState()); Assert.AreEqual("Consumed", MeatPhase);
            Turn(hunter); Assert.AreEqual(10, role.PursuitRemaining);
        }

        [TestCase("CookedMeat")] [TestCase("Emberwheat")] [TestCase("owned-tag")]
        [TestCase("owner-property")] [TestCase("quest")] [TestCase("carried")]
        [TestCase("container")] [TestCase("equipped-pointer")] [TestCase("foreign")]
        [TestCase("empty")] [TestCase("burning")] [TestCase("heat")]
        [TestCase("distance-seven")] [TestCase("hidden")]
        public void IneligibleSourceDoesNotSpendAdmissionOrConsumeFood(string mode)
        {
            Configure(); Meat(mode == "CookedMeat" || mode == "Emberwheat" ? mode : "RawMeat");
            if (mode == "owned-tag") meat.Tags["Owned"] = "";
            if (mode == "owner-property") meat.Properties["Owner"] = player.ID;
            if (mode == "quest") meat.Tags["QuestItem"] = "";
            if (mode == "carried") { Assert.True(zone.RemoveEntity(meat)); Assert.True(player.GetPart<InventoryPart>().AddObject(meat)); }
            if (mode == "container")
            { var chest = Place("Crate", 11, 11); Assert.True(zone.RemoveEntity(meat)); chest.GetPart<ContainerPart>().Contents.Add(meat); meat.GetPart<PhysicsPart>().InInventory = chest; }
            if (mode == "equipped-pointer") meat.GetPart<PhysicsPart>().Equipped = player;
            if (mode == "foreign") { Assert.True(zone.RemoveEntity(meat)); Assert.True(new Zone("foreign").AddEntity(meat, 11, 10)); }
            if (mode == "empty") meat.GetPart<StackerPart>().StackCount = 0;
            if (mode == "burning") meat.ApplyEffect(new BurningEffect());
            if (mode == "heat") zone.TileState.AddHeat(11, 10, 1);
            if (mode == "distance-seven") Assert.True(zone.MoveEntity(meat, 17, 10));
            if (mode == "hidden") { Assert.True(zone.MoveEntity(meat, 13, 10)); Place("Tree", 12, 10); }
            int count = Units(meat); var position = zone.GetEntityCell(meat);
            Turn(hunter);
            Assert.False(Field<bool>("MeatDiversionAttempted")); Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
            Assert.AreSame(position, zone.GetEntityCell(meat)); Assert.AreEqual(count, Units(meat));
        }

        [TestCase("calm")] [TestCase("work")] [TestCase("conversation")]
        [TestCase("party")] [TestCase("hostile")] [TestCase("retreat")]
        public void HigherPriorityStatePreventsBaitAdmission(string mode)
        {
            Configure(); Meat(); var brain = hunter.GetPart<BrainPart>();
            if (mode == "calm") brain.PushGoal(new NoFightGoal(9));
            if (mode == "work") brain.PushGoal(new WaitGoal(9));
            if (mode == "conversation") brain.InConversation = true;
            if (mode == "party") { brain.SetPartyLeader(player); brain.PushGoal(new FollowLeaderGoal(player)); }
            if (mode == "hostile") Assert.True(zone.MoveEntity(player, 10, 11));
            if (mode == "retreat") hunter.GetStat("Hitpoints").BaseValue = 2;
            Turn(hunter);
            Assert.False(Field<bool>("MeatDiversionAttempted")); Assert.AreEqual(1, Units(meat));
            Assert.NotNull(zone.GetEntityCell(meat)); Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
        }

        [TestCase("moved")] [TestCase("picked-up")] [TestCase("replaced")]
        [TestCase("burning")] [TestCase("owned")] [TestCase("empty")]
        public void InvalidatedExactBaitEndsOneAttemptWithoutTakingAReplacement(string mode)
        {
            Configure(); Meat(); Turn(hunter);
            Assert.AreSame(meat, Field<Entity>("MeatDiversionTarget"));
            Assert.AreEqual(1, Field<int>("MeatFeedProgress"));
            if (mode == "moved") Assert.True(zone.MoveEntity(meat, 12, 10));
            if (mode == "picked-up") { Assert.True(zone.RemoveEntity(meat)); Assert.True(player.GetPart<InventoryPart>().AddObject(meat)); }
            if (mode == "replaced") Assert.True(zone.RemoveEntity(meat));
            if (mode == "burning") meat.ApplyEffect(new BurningEffect());
            if (mode == "owned") meat.Properties["OwnerID"] = player.ID;
            if (mode == "empty") meat.GetPart<StackerPart>().StackCount = 0;
            var replacement = Place("RawMeat", 11, 10);
            Turn(hunter);
            Assert.AreEqual("Aborted", MeatPhase); Assert.True(Field<bool>("MeatDiversionAttempted"));
            Assert.IsNull(Field<Entity>("MeatDiversionTarget")); Assert.NotNull(zone.GetEntityCell(replacement));
            Assert.AreEqual(1, Units(replacement));
            Turn(hunter); Assert.NotNull(zone.GetEntityCell(replacement)); Assert.AreEqual(1, Units(replacement));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ReplacementSaveKeepsExactFoodProgressAndSpentAllowance(int progress)
        {
            Configure(); Meat("DriedMeat", 2);
            for (int i = 0; i < progress; i++) Turn(hunter);
            var oldHunter = hunter; var oldMeat = meat; string before = HuntState();
            RoundTrip(); Assert.AreNotSame(oldHunter, hunter); Assert.AreNotSame(oldMeat, meat);
            Assert.True(Field<bool>("MeatDiversionEnabled")); Assert.AreEqual(before, HuntState());
            Assert.AreEqual(progress > 0, Field<bool>("MeatDiversionAttempted"));
            if (progress > 0 && progress < 3) Assert.AreSame(meat, Field<Entity>("MeatDiversionTarget"));
            for (int i = progress; i < 3; i++) Turn(hunter);
            Assert.AreEqual(1, Units(meat)); Assert.AreEqual("Consumed", MeatPhase);
            RoundTrip(); Turn(hunter); Assert.AreEqual(1, Units(meat)); Assert.AreEqual("Consumed", MeatPhase);
        }

        [Test] public void FeedingPresentationRemovalCannotConsumeAMatchingReplacement()
        {
            Configure(); Meat(); Entity replacement = null;
            EntityVisualHooks.InteractionCallback = (actor, target, current) =>
            {
                if (actor != hunter || target != meat) return;
                Assert.True(current.RemoveEntity(meat)); replacement = Place("RawMeat", 11, 10);
            };
            Turn(hunter); Assert.NotNull(replacement, "actual current-owner feed gesture occurred");
            Turn(hunter); Assert.AreEqual("Aborted", MeatPhase);
            Assert.NotNull(zone.GetEntityCell(replacement)); Assert.AreEqual(1, Units(replacement));
        }

        [Test] public void CurrentLookNamesTheActualMeatActivityWithoutSpendingATurn()
        {
            Configure(); Meat(); Turn(hunter); string before = HuntState(); int progress = Field<int>("MeatFeedProgress");
            StringAssert.Contains("meat", role.DescribeState()?.ToLowerInvariant());
            Assert.AreEqual(before, HuntState()); Assert.AreEqual(progress, Field<int>("MeatFeedProgress"));
            hunter.GetPart<BrainPart>().PushGoal(new NoFightGoal(9)); Assert.IsNull(role.DescribeState());
        }

        void RoundTrip()
        {
            string hunterId = hunter.ID, preyId = prey.ID, meatId = meat.ID;
            var manager = OverworldZoneManager.CreateDetached(factory, 64); manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("predator-meat-diversion", "prototype", manager, new TurnManager(), player);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, factory));
                zone = loaded.ZoneManager.ActiveZone; player = loaded.Player;
            }
            hunter = zone.GetReadOnlyEntities().Single(e => e.ID == hunterId);
            prey = zone.GetReadOnlyEntities().Single(e => e.ID == preyId);
            meat = zone.GetReadOnlyEntities().Single(e => e.ID == meatId);
            role = hunter.GetPart<SpreadPredatorPart>();
            hunter.GetPart<BrainPart>().CurrentZone = zone; prey.GetPart<BrainPart>().CurrentZone = zone;
        }
    }
}
