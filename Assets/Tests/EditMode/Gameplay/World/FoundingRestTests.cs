using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FoundingRestTests
    {
        private const string Sleep = "SleepOnFoundingPlume";
        private EntityFactory _factory;
        private Entity _player, _plume;
        private Zone _zone;
        private TurnManager _turns;
        private NarrativeStatePart _oldNarrative;
        private int _before;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Set("CatacombFolk", 50); MessageLog.Clear();
            _oldNarrative = NarrativeStatePart.Current; NarrativeStatePart.Current = new NarrativeStatePart();
            _zone = new Zone("FoundingRest");
            _player = new Entity { BlueprintName = "Player" };
            _player.SetTag("Player"); _player.SetTag("Creature");
            _player.AddPart(new RenderPart { RenderLayer = 10 });
            _player.AddPart(new InventoryPart()); _player.AddPart(new ExaminablePart());
            _player.AddPart(new StatusEffectsPart());
            _player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 5, Min = 0, Max = 40 };
            _player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 200 };
            _zone.AddEntity(_player, 10, 10);
            _plume = _factory.CreateEntity("FoundingPlume"); _zone.AddEntity(_plume, 10, 10);
            _turns = new TurnManager(); _turns.AddEntity(_player); _turns.ProcessUntilPlayerTurn(); _before = _turns.TickCount;
        }
        [TearDown] public void TearDown()
        {
            NarrativeStatePart.Current = _oldNarrative; FactionManager.Reset();
            foreach (var input in Object.FindObjectsByType<InputHandler>(FindObjectsSortMode.None)) Object.DestroyImmediate(input.gameObject);
            foreach (var menu in Object.FindObjectsByType<WorldActionMenuUI>(FindObjectsSortMode.None)) Object.DestroyImmediate(menu.gameObject);
        }
        private void Dispatch(Entity actor = null, Zone zone = null, string command = Sleep)
        {
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Command", command);
            e.SetParameter("Actor", (object)(actor ?? _player)); e.SetParameter("Zone", (object)(zone ?? _zone));
            _plume.FireEventAndRelease(e);
        }
        private void AssertOutcome(bool success)
        {
            Assert.AreEqual(_before + (success ? 60 : 0), _turns.TickCount, "clock charge");
            Assert.AreEqual(success ? 40 : 5, _player.GetStat("Hitpoints").Value, "healing");
            Assert.AreEqual(success ? 1 : 0, NarrativeStatePart.Current.GetFact("RootedMet"), "meeting fact");
        }
        [TestCase(50, true)] [TestCase(49, false)] [TestCase(-150, false)]
        public void TrustedSleepHasRealRestCostAndMeeting_TrustCannotBeBypassed(int rep, bool success)
        {
            PlayerReputation.Set("CatacombFolk", rep);
            int energy = _turns.GetEnergy(_player); Dispatch(); AssertOutcome(success);
            Assert.AreEqual(energy, _turns.GetEnergy(_player)); Assert.IsTrue(_turns.WaitingForInput);
        }
        [TestCase(0, true)] [TestCase(1, false)] [TestCase(20, false)]
        public void MustLieOnTheActualPlume_NotMerelyWithinInteractionReach(int distance, bool success)
        {
            _zone.RemoveEntity(_player); _zone.AddEntity(_player, 10 + distance, 10);
            Dispatch(); AssertOutcome(success);
        }
        [TestCase(true)] [TestCase(false)]
        public void RemovedOrForeignZonePlumeCannotRest(bool removed)
        {
            if (removed) _zone.RemoveEntity(_plume);
            Dispatch(zone: removed ? _zone : new Zone("Other")); AssertOutcome(false);
        }
        [Test] public void NonPlayerCannotTriggerGlobalMeeting()
        {
            _player.Tags.Remove("Player"); Dispatch(); AssertOutcome(false);
        }
        [Test] public void UnrelatedInventoryCommandDoesNotSleep()
        {
            Dispatch(command: "Examine"); AssertOutcome(false);
        }
        [TestCase(8, false)] [TestCase(9, true)]
        public void HostileSafetyUsesTheRealRestBoundary(int distance, bool success)
        {
            _zone.AddEntity(_factory.CreateEntity("Snapjaw"), 10 + distance, 10);
            Dispatch(); AssertOutcome(success);
        }
        [Test] public void RepeatSleepDoesNotRepeatTheFirstMeetingEvent()
        {
            Dispatch(); Dispatch();
            Assert.AreEqual(_before + 120, _turns.TickCount);
            Assert.AreEqual(1, NarrativeStatePart.Current.EventLog.Count(s => s == "RootedMet"));
        }
        [TestCase(true)] [TestCase(false)]
        public void UnderfootPickerRemainsReachableWithAndWithoutLoot(bool loot)
        {
            if (loot)
            {
                var item = new Entity { BlueprintName = "Bone" }; item.AddPart(new PhysicsPart { Takeable = true });
                _zone.AddEntity(item, 10, 10);
            }
            var input = BuildInput(); Call(input, "InteractInDirection", 0, 0);
            var actions = Actions(input);
            Assert.IsTrue(actions.Any(a => a.Command == WorldInteractionSystem.PickTargetCommandPrefix + _plume.ID));
            if (loot) Assert.AreEqual(WorldInteractionSystem.ViewPileCommand, actions[0].Command);
            var pick = actions.Single(a => a.Command == WorldInteractionSystem.PickTargetCommandPrefix + _plume.ID);
            Call(input, "ExecuteWorldActionSelection", pick, _player, _zone.GetCell(10, 10), loot);
            var sleep = Actions(input).Single(a => a.Command == Sleep);
            Call(input, "ExecuteWorldActionSelection", sleep, _plume, _zone.GetCell(10, 10), false);
            AssertOutcome(true); Assert.IsTrue(_turns.WaitingForInput);
            Assert.AreEqual("Normal", typeof(InputHandler).GetField("_inputState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input).ToString());
        }
        [Test] public void UntaggedTerrainDoesNotAddUnderfootNavigation()
        {
            _plume.Tags.Remove("UnderfootInteractable");
            var input = BuildInput(); Call(input, "InteractInDirection", 0, 0);
            Assert.IsFalse(Actions(input).Any(a => a.Command == WorldInteractionSystem.PickTargetCommandPrefix + _plume.ID));
            AssertOutcome(false);
        }
        private InputHandler BuildInput()
        {
            var input = new GameObject("FoundingInputTest").AddComponent<InputHandler>();
            input.PlayerEntity = _player; input.CurrentZone = _zone; input.TurnManager = _turns;
            input.WorldActionMenuUI = new GameObject("FoundingMenuTest").AddComponent<WorldActionMenuUI>(); return input;
        }
        private static List<InventoryAction> Actions(InputHandler input) => (List<InventoryAction>)typeof(WorldActionMenuUI)
            .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input.WorldActionMenuUI);
        private static void Call(object obj, string name, params object[] args) => obj.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    }
}
