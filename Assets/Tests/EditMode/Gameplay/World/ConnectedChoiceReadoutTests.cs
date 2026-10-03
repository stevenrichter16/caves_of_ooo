using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Players can compare the real investments before spending their
    /// first clay or ingredients. Reading that information performs no work.</summary>
    public sealed class ConnectedChoiceReadoutTests
    {
        static Entity Notice(Zone zone) => zone.GetReadOnlyEntities()
            .Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "notice");

        [Test] public void NewGladeNoticeExplainsTheCompetingPanBeforeFirstClayExpenditure()
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = manager.GetZone(GleanersDistrict.SurfaceID);
                Assert.NotNull(zone);
                var notice = Notice(zone);
                string text = notice.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(notice)).ToLowerInvariant();
                Assert.That(text, Does.Contain("well"));
                Assert.That(text, Does.Contain("kitchen"));
                Assert.That(text, Does.Match(@"pan[^.]*two measures of fire clay"),
                    "The competing cost must be visible at spawn, before the well consumes the first two clay.");
                Assert.That(text, Does.Contain("field meal"));
                Assert.That(text, Does.Contain("oven"));
                Assert.False(zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "well")
                    .GetPart<RepairablePart>().Repaired);
                Assert.False(manager.CachedZones.ContainsKey(KitchenBatchPart.KitchenZoneID),
                    "Historical directions cannot generate or query remote current stock.");
            }
        }

        [Test] public void LiteralLegacyManifestAndSavedNoticeRetainTheirOriginalDirections()
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var world = new Entity { BlueprintName = "World" };
                world.Properties[SpreadExplorationPlan.PropertyKey] = "10|64|0";
                var restore = typeof(SpreadExplorationPlan).GetMethod("Restore", BindingFlags.Static | BindingFlags.NonPublic);
                var plan = (SpreadExplorationPlan)restore.Invoke(null, new object[] { manager, world });
                typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, plan);
                Assert.AreEqual(10, manager.Exploration.Version);
                var zone = manager.GetZone(GleanersDistrict.SurfaceID);
                Assert.NotNull(zone);
                var notice = Notice(zone);
                string original = notice.GetPart<ExaminablePart>().Text;
                Assert.That(original, Does.Contain("WELL LINING SPLIT"));
                Assert.That(original, Does.Not.Contain("batch pan"));
                var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(notice);
                Assert.AreEqual(original, loaded.GetPart<ExaminablePart>().Text,
                    "New service directions must not rewrite an old saved notice.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void IdlePanExamineExplainsMealBenefitAndTradeoffBeforePayment(bool broken)
        {
            using (var f = new ConnectedKitchenFixture())
            {
                f.Pan.GetPart<RepairablePart>().Repaired = !broken;
                int tick = WorldClock.CurrentTick;
                string text = f.Pan.GetPart<ExaminablePart>()
                    .BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(f.Pan), f.Player).ToLowerInvariant();
                var actions = WorldInteractionSystem.GatherActions(f.Pan, f.Player);
                Assert.AreEqual(!broken, actions.Any(a => a.Command == KitchenBatchPart.StartCommand));
                Assert.That(text, Does.Contain("two emberwheat"));
                Assert.That(text, Does.Contain("one claspbean pulp"));
                Assert.That(text, Does.Contain("two drams"));
                Assert.That(text, Does.Contain("120 world ticks"));
                Assert.That(text, Does.Contain("3d4"));
                Assert.That(text, Does.Contain("bleeding"));
                Assert.That(text, Does.Contain("one action"));
                Assert.That(text, Does.Contain("more healing"));
                Assert.That(text, Does.Contain("free"));
                Assert.AreEqual(tick, WorldClock.CurrentTick);
                f.Unspent();
            }
        }
    }
}
