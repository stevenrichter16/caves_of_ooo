using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FoundingVillageBenchTests
    {
        [Test] public void ScenarioStagesTheAuthoredPlaceWithAnUnmetDamagedVisitor()
        {
            var prior = NarrativeStatePart.Current;
            try
            {
                using (var harness = new ScenarioTestHarness())
                {
                    var ctx = harness.CreateContext(playerBlueprint: "Player"); var bench = new FoundingVillageBench();
                    bench.Apply(ctx);
                    Assert.AreEqual(1, ctx.Zone.GetAllEntities().Count(e => e.BlueprintName == "TheRooted"));
                    Assert.AreEqual(11, ctx.Zone.GetAllEntities().Count(e => e.BlueprintName == "FoundingPlume"));
                    Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
                    Assert.AreEqual(0, NarrativeStatePart.Current.GetFact("RootedMet"));
                    Assert.Less(ctx.PlayerEntity.GetStatValue("Hitpoints"), ctx.PlayerEntity.GetStat("Hitpoints").Max);
                    Assert.IsTrue(ctx.Turns.WaitingForInput);
                    Assert.IsTrue(ctx.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "Tepuibone"));
                    string first = bench.RunId; bench.Apply(ctx); Assert.AreNotEqual(first, bench.RunId);
                }
            }
            finally { NarrativeStatePart.Current = prior; }
        }
        [Test] public void MissingTenderCannotStageAPassingNativeAudit()
        {
            using (var harness = new ScenarioTestHarness())
            {
                harness.Factory.Blueprints.Remove("FoundingPlaqueTender");
                Assert.Throws<InvalidOperationException>(() => new FoundingVillageBench().Apply(harness.CreateContext()));
            }
        }
    }
}
