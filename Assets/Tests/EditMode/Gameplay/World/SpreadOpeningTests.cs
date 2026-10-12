using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadOpeningTests
    {
        sealed class ZeroRoll : Random { public override int Next(int maxValue) => 0; }
        HaulingContentScope scope;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); }
        [TearDown] public void Cleanup() { scope.Dispose(); }
        static Entity Role(Zone zone, string key, string role) => zone.GetReadOnlyEntities().Single(e => e.GetProperty(key) == role);

        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void FreshWorldGuaranteesManualAndKeepsOriginalSecondary(int seed)
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            manager.GetZone(GleanersDistrict.SurfaceID);
            var cellar = manager.GetZone(GleanersCellarBuilder.ZoneID);
            var goods = Role(cellar, GleanersCellarBuilder.RoleKey, "supplies").GetPart<ContainerPart>().Contents;
            Assert.AreEqual(1, goods.Count(e => e.BlueprintName == "DitchkeepersFootwork"));
            Assert.AreEqual(2, goods.Where(e => e.BlueprintName == "FireClay").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.AreEqual(1, goods.Count(e => e.BlueprintName == (seed % 2 == 0 ? "ShatteredRimeGrimoire" : "Buckler")));
            Assert.NotNull(Role(cellar, GleanersCellarBuilder.RoleKey, "guard").Parts.SingleOrDefault(p => p.Name == "MeleeTraumaProfile"));
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void FreshGladeCrossingHasClearLandingAndWalkAround(int seed)
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var z = manager.GetZone(GleanersDistrict.SurfaceID);
            var barrier = Role(z, GleanersDistrict.RoleKey, "crossing");
            var at = z.GetEntityPosition(barrier);
            Assert.True(barrier.GetPart<PhysicsPart>().Solid);
            FieldOfView.Compute(z, at.x - 1, at.y, 8);
            Assert.True(z.GetCell(at.x + 1, at.y).IsVisible, "Low crossing must expose the cursor landing.");
            barrier.SetTag("Wall");
            FieldOfView.Compute(z, at.x - 1, at.y, 8);
            Assert.False(z.GetCell(at.x + 1, at.y).IsVisible, "Counter: a full-height wall conceals the landing.");
            barrier.Tags.Remove("Wall");
            Assert.False(z.GetCell(at.x - 1, at.y).BlocksMovement());
            Assert.False(z.GetCell(at.x + 1, at.y).BlocksMovement());
            var reach = ConnectivityBuilder.FloodFill(z, 40, 12);
            Assert.True(reach[at.x - 1, at.y]); Assert.True(reach[at.x + 1, at.y]);
            Assert.True(reach[GleanersDistrict.EntranceX, GleanersDistrict.EntranceY]);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)]
        public void IncidentalTraumaCapabilityBelongsToAttacker(bool allowed, bool onDefender)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.MeleeTraumaProfilePart");
            Assert.NotNull(type, "An explicit saved attacker capability is required.");
            var attacker = scope.Factory.CreateEntity("MarlbackScrabbler");
            var defender = scope.Factory.CreateEntity("Player");
            var part = (Part)Activator.CreateInstance(type);
            type.GetField("AllowIncidentalDismemberment").SetValue(part, allowed);
            (onDefender ? defender : attacker).AddPart(part);
            var body = defender.GetPart<Body>();
            var hand = body.GetParts().First(p => p.Type == "Hand");
            int before = body.CountParts("Hand");
            CombatSystem.CheckCombatDismemberment(defender, body, hand, 1000, null, new ZeroRoll(), attacker);
            Assert.AreEqual(before - (allowed || onDefender ? 1 : 0), body.CountParts("Hand"));
        }
        [Test]
        public void LegacyDirectBuilderDoesNotUpgradeRewards()
        {
            var z = new Zone(GleanersCellarBuilder.ZoneID);
            Assert.True(new GleanersCellarBuilder(64).BuildZone(z, scope.Factory, new Random(1)));
            Assert.False(Role(z, GleanersCellarBuilder.RoleKey, "supplies").GetPart<ContainerPart>().Contents.Any(e => e.BlueprintName == "DitchkeepersFootwork"));
            Assert.False(Role(z, GleanersCellarBuilder.RoleKey, "guard").Parts.Any(p => p.Name == "MeleeTraumaProfile"));
        }
    }
}
