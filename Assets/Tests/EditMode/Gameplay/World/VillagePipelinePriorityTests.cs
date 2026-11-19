using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>The generic installation post-builder needs a priority that no village pipeline uses (spec S6/S7: 4650).</summary>
    public sealed class VillagePipelinePriorityTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static string[] SharedPipelinePlaces() => WorldMapAuthoring.Places.Where(p => p.Name != "Morrowfast").Select(p => p.Name).ToArray();

        static ZoneGenerationPipeline Pipeline(OverworldZoneManager manager, string place)
        {
            var poi = WorldMapAuthoring.Places.Single(p => p.Name == place);
            string zoneId = "Overworld." + poi.X + "." + poi.Y + ".0";
            return (ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone", Hidden).Invoke(manager, new object[] { zoneId });
        }

        [Test] public void Priority4650IsFreeInEverySharedVillagePipeline()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                foreach (var place in SharedPipelinePlaces())
                {
                    var builders = Pipeline(manager, place).Builders.Where(b => !(b is InstallationPostBuilder)).ToArray(); Assert.IsNotEmpty(builders, place);
                    Assert.That(builders.Select(b => b.Priority), Has.None.EqualTo(4650), place);
                    Assert.That(builders.Select(b => b.Priority).Max(), Is.LessThanOrEqualTo(4600), place + ": nothing runs after the Sumphold/Quillhold furnishing builders");
                }
            }
        }

        [Test] public void VillagePipelinePrioritiesAreUniqueExceptTheDocumentedTie()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                foreach (var place in SharedPipelinePlaces())
                {
                    var ties = Pipeline(manager, place).Builders.GroupBy(b => b.Priority).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
                    Assert.That(ties, Is.SubsetOf(new[] { 4100 }), place + " ties at " + string.Join(",", ties) + " (ContainerBuilder/TradeStockBuilder tie at 4100 is the only documented tie)");
                }
            }
        }
    }
}
