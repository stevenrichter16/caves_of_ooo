using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WesternVoxelSpawnTests
    {
        [TestCase("Overworld.2.6.0")]
        [TestCase("Overworld.3.6.0")]
        public void ConfiguredFreshStartUsesRequestedChunkAndPlacesPlayerOnOpenGround(string zoneId)
        {
            using (var f = new MorrowfastStartFixture())
            {
                var field = typeof(GameBootstrap).GetField("FreshGameZoneID");
                Assert.NotNull(field, "Fresh-game zone must be configurable independently of authored town identity.");
                field.SetValue(f.Bootstrap, zoneId);
                f.Generate();
                f.Place();
                Assert.AreEqual(zoneId, f.Zone.ZoneID);
                Assert.AreSame(f.Zone, f.Manager.ActiveZone);
                Assert.IsFalse(f.Zone.GetEntityCell(f.Player).BlocksMovement(f.Player));
                Assert.AreEqual("Overworld.3.6.0", MorrowfastSceneRuntime.ZoneID);
                if (zoneId == "Overworld.2.6.0")
                    Assert.IsFalse(MorrowfastSceneRuntime.IsActive(f.Zone));
                else
                    Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player));
            }
        }
    }
}
