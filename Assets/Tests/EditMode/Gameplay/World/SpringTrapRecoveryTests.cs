using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpringTrapRecoveryTests : FiftyWorldFixture
    {
        [SetUp] public void Reveal() { foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true; }
        [TearDown] public void ClearProbe() => FiftyWorldCreationProbe.Created = null;
        Entity Device(string kind = "heat") { var d = Place("SpringTrapLaid"); d.GetPart<SpringTrapPart>().Kind = kind; return d; }
        string Row(Entity d) => Actions(d).Single(a => a.Name == "RecoverSpringTrap" && !a.IsDisabled).Command;
        Entity[] Kits() => Pack.Objects.Where(e => e.BlueprintName == "SpringTrap").ToArray();
        [TestCase("heat", "FireMoss")][TestCase("cold", "FrostLichen")][TestCase("charge", "SparkRoot")]
        public void RecoveryRetainsExactlyOneLoadedKit(string kind, string cartridge)
        {
            var d = Device(kind); string command = Row(d); Assert.True(Act(d, command));
            Assert.IsNull(Zone.GetEntityCell(d)); Assert.AreEqual(1, Kits().Length);
            Assert.AreEqual(kind, Kits()[0].GetPart<SpringTrapKitPart>().Kind);
            Assert.AreEqual(cartridge, Kits()[0].GetPart<SpringTrapKitPart>().Loaded);
            Assert.False(Pack.Objects.Any(e => e.BlueprintName == cartridge));
            Assert.False(Act(d, command)); Assert.AreEqual(1, Kits().Length);
        }
        [TestCase("unknown")][TestCase("unarmed")][TestCase("hidden")][TestCase("far")]
        public void UnsupportedOrUnreachableDeviceHasNoRecovery(string change)
        {
            var d = Device();
            if (change == "unknown") d.GetPart<SpringTrapPart>().Kind = "poison";
            if (change == "unarmed") d.GetPart<SpringTrapPart>().Armed = false;
            if (change == "hidden") d.GetPart<RenderPart>().Visible = false;
            if (change == "far") Zone.MoveEntity(d, 17, 10);
            Assert.False(Actions(d).Any(a => a.Name == "RecoverSpringTrap" && !a.IsDisabled));
        }
        [Test] public void CancelledRecoveryKeepsOriginalWithoutAnyReplacement()
        {
            var d = Device(); string command = Row(d); FailAfter();
            Assert.False(Act(d, command)); Assert.AreEqual(0, Kits().Length); Assert.AreSame(d, Zone.GetCell(11, 10).Objects.Single(e => e == d));
            Assert.True(d.GetPart<SpringTrapPart>().Armed);
        }
        [Test] public void FullPackDoesNotLoseTheDevice()
        {
            var d = Device(); string command = Row(d); Pack.MaxWeight = Pack.GetCarriedWeight();
            Assert.False(Act(d, command)); Assert.AreEqual(0, Kits().Length); Assert.NotNull(Zone.GetEntityCell(d));
        }
        [Test] public void IndependentlyTriggeredDeviceDuringCreationIsNeverRestored()
        {
            var d = Device(); string command = Row(d);
            Factory.RegisterPartType<FiftyWorldCreationProbe>(); Factory.Blueprints["SpringTrap"].Parts["FiftyWorldCreationProbe"] = new Dictionary<string, string>();
            FiftyWorldCreationProbe.Created = _ => {
                FiftyWorldCreationProbe.Created = null;
                var e = GameEvent.New("EntityEnteredCell"); e.SetParameter("Actor", Actor); e.SetParameter("Cell", Zone.GetEntityCell(d));
                d.FireEventAndRelease(e);
            };
            Assert.False(Act(d, command)); Assert.IsNull(Zone.GetEntityCell(d)); Assert.AreEqual(0, Kits().Length);
        }
        [Test] public void RecoveredLoadPersistsAndCanBeLaidAgain()
        {
            var d = Device("cold"); Assert.True(Act(d, Row(d))); string id = Kits().Single().ID;
            var manager = OverworldZoneManager.CreateDetached(Factory, 64);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(Clock.TickCount, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("trap-recovery", "retained-load", manager, Clock, Actor));
            Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            var kit = Pack.Objects.Single(e => e.ID == id); Assert.AreEqual("FrostLichen", kit.GetPart<SpringTrapKitPart>().Loaded);
            var lay = Actions(kit).Single(a => a.Name == "LaySpringTrap" && a.Display.EndsWith(" east"));
            Assert.True(Act(kit, lay.Command)); Assert.False(Pack.Objects.Contains(kit));
            Assert.AreEqual(1, Zone.GetCell(11, 10).Objects.Count(e => e.HasPart<SpringTrapPart>()));
            Assert.AreEqual("cold", Zone.GetCell(11, 10).Objects.Single(e => e.HasPart<SpringTrapPart>()).GetPart<SpringTrapPart>().Kind);
        }
        [Test] public void Review_TriggeredAfterOwnRemovalCannotAlsoReturnALoadedKit()
        {
            var d = Device(); var visitor = Place("MarlbackScrabbler", 11, 11);
            Actor.AddPart(new RecoveryProbe { Run = () => {
                Assert.True(Zone.AddEntity(d, 11, 10));
                Assert.True(MovementSystem.TryMove(visitor, Zone, 0, -1));
            } });
            string command = Row(d);
            Assert.False(Act(d, command)); Assert.IsNull(Zone.GetEntityCell(d)); Assert.AreEqual(0, Kits().Length);
        }
        [Test] public void Review_ForcedDropOfProvisionalKitCannotDuplicateTheDevice()
        {
            var d = Device();
            Actor.AddPart(new RecoveryProbe { Run = () => {
                var kit = Kits().Single(); Assert.True(Pack.RemoveObject(kit)); Assert.True(Zone.AddEntity(kit, 10, 11));
            } });
            string command = Row(d);
            Assert.False(Act(d, command)); Assert.NotNull(Zone.GetEntityCell(d)); Assert.AreEqual(0, Kits().Length);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "SpringTrap"));
        }
        public sealed class RecoveryProbe : Part
        {
            public Action Run;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") Run(); return true; }
        }
    }
}
