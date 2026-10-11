using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CavePresentationAddressCacheAdversarialTests
    {
        EntityEquipmentContentFixture content;
        OverworldZoneManager manager;
        [SetUp] public void SetUp()
        {
            content = new EntityEquipmentContentFixture();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
        }
        [TearDown] public void TearDown() => content.Dispose();

        [TestCase("WorldMap")] [TestCase("Overworld.1.1")]
        [TestCase("Overworld.01.1.1")] [TestCase("Overworld.1.01.1")]
        [TestCase("Overworld.1.1.01")] [TestCase("Overworld.1.1.-1")]
        [TestCase("Overworld.1.1.0")] [TestCase("Overworld.1.1.1.extra")]
        [TestCase("Overworld.-1.1.1")] [TestCase("Overworld.9999.1.1")]
        [TestCase("Overworld.1.9999.1")] [TestCase("Overworld.2147483648.1.1")]
        [TestCase("Overworld.1.1.2147483648")] [TestCase("overworld.1.1.1")]
        [TestCase("Overworld.+1.1.1")] [TestCase("Overworld.1.1.1 ")]
        public void MalformedAliasesAndSurfaceAddressesNeverBecomeCaveAuthority(string id)
        {
            var zone = new Zone(id); manager.SetActiveZone(zone);
            for (int i = 0; i < 3; i++) Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone), id);
            Assert.AreEqual(BiomeCropPlan.IsOrdinaryCave(manager, id), BiomeCropRecipes.IsOrdinaryCave(zone));
        }

        [TestCase("Overworld.3.6.1")]
        [TestCase("Overworld.3.3.1")]
        public void AuthoredProtectedColumnsRemainExcludedOnRepeatedQueries(string id)
        {
            var at = WorldMap.FromZoneID(id); manager.WorldMap.SetPOI(at.x, at.y, null);
            var zone = new Zone(id); manager.SetActiveZone(zone);
            Assert.IsFalse(BiomeCropPlan.IsOrdinaryCave(manager, id));
            for (int i = 0; i < 3; i++) Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone));
        }

        [TestCase(false)] [TestCase(true)]
        public void DetachedSameAddressDoesNotBorrowAttachedGraphAuthority(bool firstWarmAttached)
        {
            var at = WorldMap.FromZoneID(BiomeCropPlan.CaveColumns(manager).First());
            string id = WorldMap.ToZoneID(at.x, at.y, 1);
            var attached = new Zone(id); var detached = new Zone(id); manager.SetActiveZone(attached);
            if (firstWarmAttached) Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(attached));
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(detached));
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(attached));
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(detached));
        }
    }
}
