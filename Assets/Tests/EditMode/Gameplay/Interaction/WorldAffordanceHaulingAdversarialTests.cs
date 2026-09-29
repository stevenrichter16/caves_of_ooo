using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAffordanceHaulingAdversarialTests
    {
        [TestCase("inverse")] [TestCase("inverse-owner")] [TestCase("grip-owner")]
        [TestCase("hidden")] [TestCase("handling-removed")] [TestCase("carried")]
        public void StaleReleaseSourceIsRefusedWithoutRepairingOrRefundingTheGrip(string mutation)
        {
            using (var f = new WorldAffordanceHaulingTests.Fixture())
            {
                f.Grab(); f.Menu(HandlingPart.ReleaseCommand);
                var grip = f.Player.GetPart<DragPart>(); var held = f.Load.GetPart<DraggedPart>();
                int speed = f.Player.GetStatValue("Speed");
                switch (mutation)
                {
                    case "inverse": held.Dragger = new Entity { ID = "other" }; break;
                    case "inverse-owner": held.ParentEntity = f.Player; break;
                    case "grip-owner": grip.ParentEntity = f.Load; break;
                    case "hidden": f.Load.GetPart<RenderPart>().Visible = false; break;
                    case "handling-removed": f.Load.RemovePart(f.Load.GetPart<HandlingPart>()); break;
                    case "carried": f.Load.GetPart<PhysicsPart>().InInventory = f.Player; break;
                }
                Assert.IsNull(f.Find());
                Assert.AreSame(grip, f.Player.GetPart<DragPart>()); Assert.AreSame(held, f.Load.GetPart<DraggedPart>());
                Assert.AreEqual(speed, f.Player.GetStatValue("Speed"));
            }
        }

        [Test]
        public void CachedHaulCueCannotSurviveStrengthLossOrAnotherGrip()
        {
            using (var f = new WorldAffordanceHaulingTests.Fixture())
            {
                var cue = f.Cue(HandlingPart.HaulCommand);
                f.Player.GetStat("Strength").BaseValue = 1;
                Assert.False(WorldAffordanceQuery.Current(f.Player, f.Zone, cue));
                f.Player.GetStat("Strength").BaseValue = 18;
                Assert.True(WorldAffordanceQuery.Current(f.Player, f.Zone, cue));
                f.Player.AddPart(new DragPart { Dragged = new Entity { ID = "separate-load" } });
                Assert.False(WorldAffordanceQuery.Current(f.Player, f.Zone, cue));
            }
        }
    }
}
