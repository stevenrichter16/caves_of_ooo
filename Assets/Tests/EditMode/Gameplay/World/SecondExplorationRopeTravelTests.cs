using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SecondExplorationRopeTravelTests : SecondExplorationFixture
    {
        private const string UpperZoneId = "Overworld.2.7.1";
        private const string LowerZoneId = "Overworld.2.7.2";
        private const int LandingX = 23;
        private const int LandingY = 18;
        private const string RigCommand = "RigRopeShortcut";
        private const string Cord = "KnotflaxCord";
        private const string LegacyInstructions = "Two knotflax cord will rig a reciprocal rope to the corresponding lower terrace. "
            + "First visit both existing levels and confirm their landings; then use the ordinary stair controls at the installed line.";

        [Test]
        public void UnvisitedFarLandingPointsToOrdinaryStairsWithoutLoadingIt()
        {
            var (_, upperAnchor, _, manager) = MakeLandings(lowerVisited: false);
            var examine = upperAnchor.GetPart<ExaminablePart>();
            string flavor = examine.Text + "\nSomeone scratched an arrow into the pin.";
            string saved = examine.Text = flavor + "\n" + LegacyInstructions;
            int tick = Clock.TickCount;

            string description = ReadAnchor(Zone, upperAnchor);

            Assert.That(description, Does.Contain(flavor));
            Assert.That(description, Does.Not.Contain(LegacyInstructions));
            Assert.That(description, Does.Contain("ordinary stairs").IgnoreCase);
            Assert.That(examine.Text, Is.EqualTo(saved));
            Assert.That(manager.CachedZones.Keys, Is.EquivalentTo(new[] { UpperZoneId }));
            Assert.That(manager.ActiveZone, Is.SameAs(Zone));
            Assert.That(manager.GetConnections(UpperZoneId), Is.Empty);
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LowerLandingRigMenuRequiresTwoUsableCarriedCord(int carried)
        {
            var (lower, _, lowerAnchor, manager) = MakeLandings();
            StandBy(lower, manager);
            var loose = Place(Cord, LandingX - 1, LandingY + 1);
            loose.GetPart<StackerPart>().StackCount = 2;
            if (carried == 1)
                Carry(Cord);
            var unusable = Factory.CreateEntity(Cord);
            unusable.GetPart<StackerPart>().StackCount = 2;
            unusable.GetPart<RenderPart>().DisplayName = "frayed cord";
            Assert.True(Pack.AddObject(unusable));
            unusable.GetPart<PhysicsPart>().Takeable = false;

            string description = ReadAnchor(lower, lowerAnchor);
            Assert.That(description, Does.Contain("knotflax cord").IgnoreCase);
            Assert.That(description, Does.Contain("need two").IgnoreCase);
            var disabled = CellMenu(LandingX, LandingY).Single(action => action.Name == RigCommand);
            Assert.That(disabled.IsDisabled, Is.True);
            Assert.That(disabled.Command, Does.StartWith(InventoryAction.DisabledCommandPrefix));
            Assert.That(disabled.DisabledReason, Does.Contain("cord").IgnoreCase);
            Assert.False(Act(lowerAnchor, RigCommand));
            Assert.That(manager.GetConnections(LowerZoneId), Is.Empty);
            Assert.That(Units(loose), Is.EqualTo(2));
            Assert.That(Units(unusable), Is.EqualTo(2));
            Assert.That(Pack.Objects.Where(item => item.BlueprintName == Cord && item != unusable).Sum(Units), Is.EqualTo(carried));

            for (int i = carried; i < 2; i++)
                Carry(Cord);
            var ready = CellMenu(LandingX, LandingY).Single(action => action.Name == RigCommand);
            Assert.That(ready.IsDisabled, Is.False);
            Assert.That(ready.Command, Is.EqualTo(RigCommand));
            Assert.True(ChooseRow(ready, LandingX, LandingY));
            Assert.That(Units(loose), Is.EqualTo(2));
            Assert.That(Units(unusable), Is.EqualTo(2));
            Assert.That(Pack.Objects.Where(item => item.BlueprintName == Cord), Is.EquivalentTo(new[] { unusable }));
        }

        [Test]
        public void ExistingTaggedStairsCannotDivertTheInstalledRopeRoundTrip()
        {
            var (lower, upperAnchor, lowerAnchor, manager) = MakeLandings();
            var upper = Zone;
            var oldDown = Place("StairsDown", 40, 10);
            var oldUp = Factory.CreateEntity("StairsUp");
            Assert.True(lower.AddEntity(oldUp, 40, 10));
            Assert.True(oldDown.HasTag("StairsDown"));
            Assert.True(oldUp.HasTag("StairsUp"));
            var upperExamine = upperAnchor.GetPart<ExaminablePart>();
            var lowerExamine = lowerAnchor.GetPart<ExaminablePart>();
            string upperSaved = upperExamine.Text += "\n" + LegacyInstructions;
            string lowerSaved = lowerExamine.Text += "\n" + LegacyInstructions;
            var spool = Carry(Cord);
            spool.GetPart<StackerPart>().StackCount = 3;

            Assert.True(Act(upperAnchor, RigCommand));
            Assert.False(Act(upperAnchor, RigCommand));
            Assert.That(Pack.Objects, Does.Contain(spool));
            Assert.That(Units(spool), Is.EqualTo(1));
            string upperDescription = ReadAnchor(upper, upperAnchor);
            Assert.That(upperDescription, Does.Contain("descend").IgnoreCase);
            Assert.That(upperDescription, Does.Not.Contain(LegacyInstructions));
            Assert.That(upperExamine.Text, Is.EqualTo(upperSaved));
            Assert.True(upper.MoveEntity(Actor, LandingX, LandingY));

            var down = ZoneTransitionSystem.TransitionPlayerVertical(Actor, upper, true, LandingX, LandingY, manager);
            Assert.True(down.Success);
            Assert.That(down.NewZone, Is.SameAs(lower));
            Assert.That((down.NewPlayerX, down.NewPlayerY), Is.EqualTo((LandingX, LandingY)),
                "The existing distant staircase must not capture the rope landing.");

            manager.SetActiveZone(lower);
            Zone = SettlementRuntime.ActiveZone = lower;
            Assert.True(lower.MoveEntity(Actor, LandingX - 1, LandingY));
            string lowerDescription = ReadAnchor(lower, lowerAnchor);
            Assert.That(lowerDescription, Does.Contain("climb").IgnoreCase);
            Assert.That(lowerDescription, Does.Not.Contain(LegacyInstructions));
            Assert.That(lowerExamine.Text, Is.EqualTo(lowerSaved));
            Assert.True(lower.MoveEntity(Actor, LandingX, LandingY));

            var up = ZoneTransitionSystem.TransitionPlayerVertical(Actor, lower, false, LandingX, LandingY, manager);
            Assert.True(up.Success);
            Assert.That(up.NewZone, Is.SameAs(upper));
            Assert.That((up.NewPlayerX, up.NewPlayerY), Is.EqualTo((LandingX, LandingY)));
            Assert.NotNull(upper.GetEntityCell(oldDown));
            Assert.NotNull(lower.GetEntityCell(oldUp));
            Assert.That(Units(spool), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InstalledReadoutWithholdsDirectionWhenReciprocalAuthorityChanges(bool differentWorld)
        {
            var (lower, upperAnchor, lowerAnchor, manager) = MakeLandings();
            Carry(Cord).GetPart<StackerPart>().StackCount = 2;
            Assert.True(Act(upperAnchor, RigCommand));
            Assert.That(ReadAnchor(Zone, upperAnchor), Does.Contain("descend").IgnoreCase);
            if (differentWorld)
            {
                var otherWorld = OverworldZoneManager.CreateDetached(Factory, 128);
                otherWorld.SetActiveZone(lower);
                Assert.That(manager.CachedZones[LowerZoneId], Is.SameAs(lower));
            }
            else
            {
                var reverse = manager.GetConnections(LowerZoneId).Single(route => route.SourceZoneID == LowerZoneId);
                manager.RemoveConnection(reverse);
                manager.RegisterConnection(new ZoneConnection
                {
                    SourceZoneID = LowerZoneId,
                    SourceX = LandingX,
                    SourceY = LandingY,
                    TargetZoneID = UpperZoneId,
                    TargetX = LandingX + 1,
                    TargetY = LandingY,
                    Type = "StairsUp"
                });
            }

            Assert.That(ReadAnchor(Zone, upperAnchor), Does.Not.Contain("descend").IgnoreCase);
            Assert.True(upperAnchor.GetPart<RopeShortcutPart>().Installed);
            Assert.True(lowerAnchor.GetPart<RopeShortcutPart>().Installed);
        }

        private (Zone Lower, Entity UpperAnchor, Entity LowerAnchor, OverworldZoneManager Manager) MakeLandings(bool lowerVisited = true)
        {
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = SettlementRuntime.ActiveZone = new Zone(UpperZoneId);
            Assert.True(Zone.AddEntity(Actor, LandingX - 1, LandingY));
            var upperAnchor = AddAnchor(Zone, LowerZoneId, down: true);
            var lower = new Zone(LowerZoneId);
            var lowerAnchor = AddAnchor(lower, UpperZoneId, down: false);
            var worlds = new Dictionary<string, Zone> { { UpperZoneId, Zone } };
            if (lowerVisited)
                worlds.Add(LowerZoneId, lower);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64);
            manager.ReplaceLoadedState(worlds, UpperZoneId, new Dictionary<string, List<ZoneConnection>>());
            return (lower, upperAnchor, lowerAnchor, manager);
        }

        private Entity AddAnchor(Zone zone, string otherZoneId, bool down)
        {
            var anchor = Factory.CreateEntity("RopeAnchor");
            Assert.True(zone.AddEntity(anchor, LandingX, LandingY));
            anchor.AddPart(new RopeShortcutPart
            {
                ZoneID = zone.ZoneID,
                OtherZoneID = otherZoneId,
                X = LandingX,
                Y = LandingY,
                OtherX = LandingX,
                OtherY = LandingY,
                Down = down
            });
            return anchor;
        }

        private void StandBy(Zone zone, OverworldZoneManager manager)
        {
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = SettlementRuntime.ActiveZone = zone;
            manager.SetActiveZone(zone);
            Assert.True(zone.AddEntity(Actor, LandingX - 1, LandingY));
        }

        private string ReadAnchor(Zone zone, Entity anchor)
        {
            var cell = zone.GetEntityCell(anchor);
            cell.Explored = cell.IsVisible = true;
            var document = ExamineDocumentBuilder.BuildWorld(anchor, zone, cell, Actor);
            Assert.That(document, Is.Not.Null);
            return document.ToPlainText();
        }
    }
}
