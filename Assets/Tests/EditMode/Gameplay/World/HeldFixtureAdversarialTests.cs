using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for set down, take up and hand over (CLAUDE.md gate): hypotheses about what a player or a hostile input could do that the happy path never exercises.
    /// Each case names the bug class it probes; a failure means a real defect, not a wrong test.
    /// </summary>
    public sealed class HeldFixtureAdversarialTests : FiftyWorldFixture
    {
        const string Kind = "test-box";
        const string Definitions = "{\"Version\":1,\"Fixtures\":[{\"Kind\":\"test-box\",\"Class\":\"Carried\",\"Blueprint\":\"Chest\",\"Solid\":true,\"GroundClass\":\"OpenGround\",\"Rounds\":1}]}";
        OverworldZoneManager manager; string wildId; Zone wild;
        const int AX = 20, AY = 12;

        [SetUp] public void SetupAdversarial()
        {
            PackableFixtureRegistry.InitializeFromJson(Definitions); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownAdversarial() { PackableFixtureRegistry.ResetForTests(); }

        Entity Kit(bool carry = true, bool load = false)
        {
            var kit = Factory.CreateEntity("Chest"); var p = kit.GetPart<PhysicsPart>(); p.Takeable = true; p.Solid = false; p.Weight = 10;
            kit.AddPart(new PackableFixturePart { Kind = Kind }); if (load) kit.AddPart(new ContainerLoadPart()); if (carry) Assert.True(Pack.AddObject(kit)); return kit;
        }
        string Row(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;
        string Refusals() => string.Join(";", Diag.Snapshot(100).Where(r => r.Kind == "FixtureRefused").Select(r => r.PayloadJson));

        [Test] public void OverflowingCoordinatesAreRefusedNotThrown_NumericBoundary()
        {
            var kit = Kit();
            foreach (string x in new[] { "-2147483628", "2147483647", "-2147483648", "9999999999999" })
                Assert.DoesNotThrow(() => Act(kit, "SetDown|" + Uri.EscapeDataString(wildId) + "|" + AX + "|" + AY + "|" + x + "|" + AY + "|" + kit.ID), x);
            Assert.True(Pack.Objects.Contains(kit)); Assert.AreEqual(0, PlacementCheck.CountInZone(wild));
        }

        [Test] public void AStackOfKitsIsNotOneFixture_StackingSemantics()
        {
            var kit = Kit(); kit.AddPart(new StackerPart { StackCount = 2 });
            var row = Actions(kit).Single(a => a.Name == "SetDownFixture"); Assert.True(row.IsDisabled); Assert.AreEqual(PlacementCheck.State, row.DisabledCode);
            Assert.False(Act(kit, "SetDown|" + Uri.EscapeDataString(wildId) + "|" + AX + "|" + AY + "|" + (AX + 1) + "|" + AY + "|" + kit.ID)); Assert.True(Pack.Objects.Contains(kit));
        }

        [Test] public void AnEquippedKitCannotBeSetDown_StateAtomicity()
        {
            var kit = Kit(); kit.GetPart<PhysicsPart>().Equipped = Actor;
            var row = Actions(kit).SingleOrDefault(a => a.Name == "SetDownFixture"); Assert.True(row == null || row.IsDisabled);
            Assert.False(Act(kit, "SetDown|" + Uri.EscapeDataString(wildId) + "|" + AX + "|" + AY + "|" + (AX + 1) + "|" + AY + "|" + kit.ID)); Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.ID == kit.ID));
        }

        [Test] public void ADraggedKitCannotBeSetDown()
        {
            var kit = Kit(); kit.AddPart(new DraggedPart()); var row = Actions(kit).Single(a => a.Name == "SetDownFixture"); Assert.True(row.IsDisabled); Assert.AreEqual(PlacementCheck.State, row.DisabledCode);
        }

        [Test] public void ReentrantSetDownCannotPlaceTheSameKitTwice_CrossActorStateRace()
        {
            var kit = Kit(); string east = Row(kit, "east"), west = Row(kit, "west"); bool nested = true; var probe = new NestedProbe();
            probe.Callback = () => { probe.Callback = null; nested = Act(kit, west); }; Actor.AddPart(probe);
            Assert.True(Act(kit, east)); Assert.False(nested, "the second placement of the same entity must refuse");
            Assert.AreEqual(1, wild.GetReadOnlyEntities().Count(e => e.ID == kit.ID)); Assert.AreEqual(1, PlacementCheck.CountInZone(wild));
        }
        sealed class NestedProbe : Part
        {
            public Action Callback;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") Callback?.Invoke(); return true; }
        }

        [Test] public void TakeUpAndHandOverTwiceAreRefusedTheSecondTime_Idempotence()
        {
            var kit = Kit(); Assert.True(Act(kit, Row(kit))); Assert.True(Act(kit, "TakeUp|" + Tail(kit))); Assert.False(Act(kit, "TakeUp|" + Tail(kit)), "it is in the pack now");
            Assert.True(Act(kit, Row(kit, "west"))); Assert.True(Act(kit, "HandOver|" + Tail(kit))); Assert.False(Act(kit, "HandOver|" + Tail(kit)), "it is no longer held");
            Assert.False(Act(kit, "TakeUp|" + Tail(kit)));
        }

        [Test] public void InstalledEventFiresOnlyAfterACommitAndNeverOnARolledBackPlacement_DiagDispatch()
        {
            var kit = Kit(); var seen = new EventCounter(); kit.AddPart(seen); FailAfter();
            Assert.False(Act(kit, Row(kit))); Assert.AreEqual(0, seen.Installed, "a rolled-back placement must not announce an install");
            Actor.RemovePart(Actor.GetPart<FiftyWorldThrowAfterPart>()); Assert.True(Act(kit, Row(kit))); Assert.AreEqual(1, seen.Installed);
            Assert.True(Act(kit, "TakeUp|" + Tail(kit))); Assert.AreEqual(1, seen.Uninstalled); Assert.AreEqual(3, seen.BeforeTotal, "a before-event is a question asked ahead of the work: BeforeInstall twice (the attempt that rolled back, then the one that committed) and BeforeUninstall once");
        }
        sealed class EventCounter : Part
        {
            public int Installed, Uninstalled, BeforeTotal;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Installed") Installed++; else if (e.ID == "Uninstalled") Uninstalled++; else if (e.ID == "BeforeInstall" || e.ID == "BeforeUninstall") BeforeTotal++;
                return true;
            }
        }

        [Test] public void EveryRefusalCodeIsRecordedWithItsCommandAndReason_DiagContract()
        {
            var kit = Kit(); Act(kit, "SetDown|" + Uri.EscapeDataString(wildId) + "|1|1|2|2|" + kit.ID);   // stale
            Act(kit, "TakeUp|" + Tail(kit));   // not standing
            StringAssert.Contains("stale-selection", Refusals()); StringAssert.Contains("\"command\":\"SetDown\"", Refusals()); StringAssert.Contains("\"command\":\"TakeUp\"", Refusals());
        }

        [Test] public void ASpotNextToTheEdgeIsRefusedForTheEdgeMarginAndTheRowSaysWhy_BoundaryInputs()
        {
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 3, 12)); var kit = Kit();
            var rows = Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();
            Assert.True(rows.All(r => !r.Display.Contains("west")), "x=2 is inside the 3-cell edge margin");
            Assert.True(rows.Any(r => r.Display.Contains("east")), "the counter: the east cell is legal");
        }

        [Test] public void AContainerKitWithoutContainerLoadLaundersWeight_AndTheValidatorSaysSo()
        {
            var plain = Kit(); var heavy = Factory.CreateEntity("Dagger"); heavy.GetPart<PhysicsPart>().Weight = 150; Assert.True(plain.GetPart<ContainerPart>().AddItem(heavy));
            Assert.Less(InventoryPart.GetItemWeight(plain), 100, "without ContainerLoad the contents weigh nothing: a documented laundering path");
            Assert.That(PackableFixtureRegistry.Validate(Factory), Has.Some.StartWith("P13"));
        }

        [Test] public void AContainerLoadKitsContentsWeighAndATakeUpThatCannotCarryThemRefusesWithoutSpilling()
        {
            var kit = Kit(load: true); Assert.True(Act(kit, Row(kit))); var heavy = Factory.CreateEntity("Dagger"); heavy.GetPart<PhysicsPart>().Weight = 150; Assert.True(kit.GetPart<ContainerPart>().AddItem(heavy));
            int with = InventoryPart.GetItemWeight(kit); Assert.Greater(with, 140, "with ContainerLoad the contents weigh");
            Pack.MaxWeight = 100; Assert.False(Act(kit, "TakeUp|" + Tail(kit))); StringAssert.Contains("overweight-load", Refusals());
            CollectionAssert.Contains(kit.GetPart<ContainerPart>().Contents, heavy); Assert.True(kit.HasPart<InstallationMarkerPart>()); Assert.AreSame(kit, wild.GetCell(AX + 1, AY).Objects.Single(e => e.ID == kit.ID));
            Pack.MaxWeight = 500; Assert.True(Act(kit, "TakeUp|" + Tail(kit)), "the counter: a pack that can carry it takes it up with the contents");
            CollectionAssert.Contains(kit.GetPart<ContainerPart>().Contents, heavy);
        }

        [Test] public void MarkerIdentityIsDerivedFromTheEntityAndStableAcrossTakeUpAndReSetDown()
        {
            var kit = Kit(); Assert.True(Act(kit, Row(kit))); string first = kit.GetPart<InstallationMarkerPart>().InstallationId; Assert.True(Act(kit, "TakeUp|" + Tail(kit)));
            Assert.True(Act(kit, Row(kit, "west"))); Assert.AreEqual(first, kit.GetPart<InstallationMarkerPart>().InstallationId); Assert.AreEqual("player:" + kit.ID, first);
        }

        [Test] public void AGeneratedMarkerOnAKitIsOverwrittenOnlyBySetDownAndNeverCountsAgainstThePlayersCap()
        {
            var kit = Kit(); kit.AddPart(new InstallationMarkerPart { Origin = "Generated", RetainZone = false, InstallationId = "authored" });
            Assert.True(Act(kit, Row(kit))); Assert.AreEqual("Player", kit.GetPart<InstallationMarkerPart>().Origin); Assert.AreEqual(1, PlacementCheck.CountInZone(wild));
            Assert.True(Act(kit, "TakeUp|" + Tail(kit))); Assert.False(kit.HasPart<InstallationMarkerPart>(), "taking up releases the marker entirely");
        }

        [Test] public void ARefusalNeverSpendsAnythingAndNeverChangesTheKit_ProbabilityOfPartialMutation()
        {
            var kit = Kit(); var parts = kit.Parts.Select(p => p.GetType()).ToArray(); var tags = kit.Tags.Keys.OrderBy(k => k).ToArray();
            for (int i = 0; i < 20; i++) Act(kit, "SetDown|" + Uri.EscapeDataString(wildId) + "|" + AX + "|" + AY + "|" + (AX + (i % 3) + 5) + "|" + AY + "|" + kit.ID);
            CollectionAssert.AreEqual(parts, kit.Parts.Select(p => p.GetType()).ToArray()); CollectionAssert.AreEqual(tags, kit.Tags.Keys.OrderBy(k => k).ToArray()); Assert.True(Pack.Objects.Contains(kit));
        }
    }
}
