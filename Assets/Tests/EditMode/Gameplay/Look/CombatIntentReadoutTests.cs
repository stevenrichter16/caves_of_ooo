using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal sealed class CombatIntentFixture : IDisposable
    {
        public readonly DensityCombatFixture Core = new DensityCombatFixture();
        public readonly Entity Actor, Player;
        public readonly Part Commitment;
        public Zone Zone => Core.Zone;
        public CombatIntentFixture()
        {
            Actor = Core.Actor(skills: ""); Player = Core.Target(12, 10);
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.CommittedMeleePart");
            Assert.NotNull(type, "Missing committed melee source.");
            Commitment = (Part)Activator.CreateInstance(type); Actor.AddPart(Commitment);
            type.GetField("AttackName").SetValue(Commitment, "tendril lash");
            type.GetField("Reach").SetValue(Commitment, 2);
            Actor.GetPart<BrainPart>().PushGoal(new KillGoal(Player));
            for (int x = 9; x <= 13; x++) for (int y = 9; y <= 11; y++) Reveal(x, y);
        }
        public void Reveal(int x, int y) { var cell = Zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        public void Begin() => Assert.True((bool)Commitment.GetType().GetMethod("TryBegin").Invoke(Commitment, new object[] { Player, Zone }));
        public void Turn() { Actor.FireEventAndRelease(GameEvent.New("TakeTurn")); Actor.FireEventAndRelease(GameEvent.New("EndTurn")); }
        public object Query(string name, params object[] args)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.CombatIntentReadout");
            Assert.NotNull(type, "Missing visible combat intent readout.");
            var method = type.GetMethod(name); Assert.NotNull(method, "Missing " + name); return method.Invoke(null, args);
        }
        public string ActorLine() => (string)Query("ActorLine", Actor, Zone);
        public string ThreatLine(Cell cell = null) => (string)Query("ThreatLine", Zone, cell ?? Zone.GetCell(11, 10));
        public void Dispose() => Core.Dispose();
    }

    public sealed class CombatIntentReadoutTests
    {
        sealed class ActionQueryTrap : Effect
        {
            public int Calls;
            public override string DisplayName => "query trap";
            public override bool AllowAction(Entity owner) { Calls++; return true; }
        }
        [Test] public void WindupDescribesItsDirectionAndThreatButIdleAndRecoveryDoNotThreaten()
        {
            using (var f = new CombatIntentFixture())
            {
                Assert.IsNull(f.ActorLine()); Assert.IsNull(f.ThreatLine()); f.Begin();
                StringAssert.Contains("east", f.ActorLine().ToLowerInvariant());
                StringAssert.Contains("tendril", f.ActorLine().ToLowerInvariant());
                StringAssert.Contains("lash", f.ThreatLine().ToLowerInvariant());
                Assert.IsNull(f.ThreatLine(f.Zone.GetCell(11, 11)), "A sidestep is outside the fixed ray.");
                f.Turn(); StringAssert.Contains("recover", f.ActorLine().ToLowerInvariant()); Assert.IsNull(f.ThreatLine());
                f.Turn(); Assert.IsNull(f.ActorLine());
            }
        }
        [TestCase("fog")][TestCase("unexplored")][TestCase("render-hidden")][TestCase("foreign-render")]
        [TestCase("foreign-physics")][TestCase("carried")][TestCase("equipped")][TestCase("removed")][TestCase("dead")]
        public void HiddenOrMalformedOwnerCannotRevealItsIntentEvenOnVisibleRay(string change)
        {
            using (var f = new CombatIntentFixture())
            {
                f.Begin(); Assert.NotNull(f.ActorLine()); Assert.NotNull(f.ThreatLine());
                if (change == "fog") f.Zone.GetEntityCell(f.Actor).IsVisible = false;
                if (change == "unexplored") f.Zone.GetEntityCell(f.Actor).Explored = false;
                if (change == "render-hidden") f.Actor.GetPart<RenderPart>().Visible = false;
                if (change == "foreign-render") f.Actor.GetPart<RenderPart>().ParentEntity = f.Player;
                if (change == "foreign-physics") f.Actor.GetPart<PhysicsPart>().ParentEntity = f.Player;
                if (change == "carried") f.Actor.GetPart<PhysicsPart>().InInventory = f.Player;
                if (change == "equipped") f.Actor.GetPart<PhysicsPart>().Equipped = f.Player;
                if (change == "removed") f.Zone.RemoveEntity(f.Actor);
                if (change == "dead") f.Actor.GetStat("Hitpoints").BaseValue = 0;
                Assert.IsNull(f.ActorLine()); Assert.IsNull(f.ThreatLine());
            }
        }
        [TestCase("fog")][TestCase("unexplored")][TestCase("foreign")]
        public void ThreatCellMustBeVisibleExploredAndActuallyOwnedByZone(string change)
        {
            using (var f = new CombatIntentFixture())
            {
                f.Begin(); var cell = f.Zone.GetCell(11, 10); Assert.NotNull(f.ThreatLine(cell));
                if (change == "fog") cell.IsVisible = false;
                if (change == "unexplored") cell.Explored = false;
                if (change == "foreign") { cell = new Zone(f.Zone.ZoneID).GetCell(11, 10); cell.IsVisible = cell.Explored = true; }
                Assert.IsNull(f.ThreatLine(cell));
            }
        }
        [Test] public void QueriesArePureAndDoNotAdvanceOrDispatchTheCommitment()
        {
            using (var f = new CombatIntentFixture())
            {
                f.Begin(); var fields = f.Commitment.GetType().GetFields(); var before = fields.Select(field => field.GetValue(f.Commitment)).ToArray(); int hp = f.Player.GetStatValue("Hitpoints");
                var effectGuard = new ActionQueryTrap { Duration = 5 }; Assert.True(f.Actor.ApplyEffect(effectGuard)); effectGuard.Calls = 0;
                var guard = new WorldAffordanceQueryTests.Trap(); f.Actor.AddPart(guard);
                for (int i = 0; i < 30; i++) { Assert.NotNull(f.ActorLine()); Assert.NotNull(f.ThreatLine()); }
                CollectionAssert.AreEqual(before, fields.Select(field => field.GetValue(f.Commitment)).ToArray()); Assert.AreEqual(hp, f.Player.GetStatValue("Hitpoints")); Assert.Zero(guard.Calls); Assert.Zero(effectGuard.Calls, "Readouts must not invoke virtual effect eligibility.");
            }
        }
        [Test] public void ActualFocusAndWorldExaminePlaceIntentBeforeFlavorWithoutDebugMode()
        {
            using (var f = new CombatIntentFixture())
            {
                f.Begin(); var examine = f.Actor.GetPart<ExaminablePart>(); examine.Text = "An old interrupted intake.";
                string intent = f.ActorLine(); var snapshot = LookQueryService.BuildSnapshot(f.Player, f.Zone, 10, 10);
                Assert.Contains(intent, snapshot.DetailLines.ToArray());
                string text = examine.BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(f.Actor), f.Player);
                StringAssert.Contains(intent, text); Assert.Less(text.IndexOf(intent, StringComparison.Ordinal), text.IndexOf(examine.Text, StringComparison.Ordinal));
                Assert.Contains(f.ThreatLine(), LookQueryService.BuildSnapshot(f.Player, f.Zone, 11, 10).DetailLines.ToArray());
                f.Zone.GetEntityCell(f.Actor).IsVisible = false;
                Assert.False(LookQueryService.BuildSnapshot(f.Player, f.Zone, 10, 10).DetailLines.Any(line => line.Contains("tendril")));
                Assert.False(examine.BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(f.Actor), f.Player).Contains(intent));
            }
        }
    }
}
