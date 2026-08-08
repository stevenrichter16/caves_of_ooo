using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM4 — save/load round-trip pins. CropPart uses public
    /// fields only, so SaveSystem's Tier-3 reflection covers it — these
    /// tests PIN that contract (a future private-field refactor would
    /// silently break mid-growth saves; see the HibernatingEffect
    /// precedent in Docs/SAVE-LOAD-AUDIT.md SL.6.4).
    /// </summary>
    [TestFixture]
    public class CropRoundTripTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        [Test]
        public void CropPart_MidGrowthMidMoisture_RoundTrips()
        {
            var crop = _factory.CreateEntity("CandyCarrotCrop");
            var part = crop.GetPart<CropPart>();
            part.GrowthStage = 1;
            part.TicksInStage = 7;
            part.MoistureTicks = 23;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(crop);
            var loadedPart = loaded.GetPart<CropPart>();

            Assert.IsNotNull(loadedPart);
            Assert.AreEqual(1, loadedPart.GrowthStage, "mid-growth stage survives");
            Assert.AreEqual(7, loadedPart.TicksInStage, "in-stage progress survives");
            Assert.AreEqual(23, loadedPart.MoistureTicks, "remaining moisture survives");
            Assert.AreEqual(20, loadedPart.TicksPerStage, "blueprint params survive");
            Assert.AreEqual("CandyCarrot", loadedPart.YieldBlueprint);
            Assert.AreEqual(2, loadedPart.YieldCount); // ALPHA economy: yield bumped 1->2
        }

        [Test]
        public void WetSoilBackground_RoundTrips_AndDriesOutCorrectlyAfterLoad()
        {
            // The wet-soil look lives on RenderPart.BackgroundColor; a
            // save taken mid-moist must come back visibly wet AND still
            // dry out correctly when the remaining moisture drains.
            var crop = _factory.CreateEntity("CandyCarrotCrop");
            crop.GetPart<CropPart>().Water(3);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(crop);
            var loadedRender = loaded.GetPart<RenderPart>();
            var loadedCrop = loaded.GetPart<CropPart>();

            Assert.AreEqual(CropPart.WET_SOIL_BG, loadedRender.BackgroundColor,
                "wet-soil background survives the round-trip");
            Assert.AreEqual(3, loadedCrop.MoistureTicks);

            // Drain the remaining moisture in a zone: bg must clear.
            var zone = new Zone("post-load");
            zone.AddEntity(loaded, 5, 5);
            CropSystem.Factory = _factory;
            try
            {
                for (int i = 0; i < 3; i++)
                    CropSystem.OnTickEnd(zone);
            }
            finally
            {
                CropSystem.Factory = null;
            }

            Assert.IsTrue(string.IsNullOrEmpty(loadedRender.BackgroundColor),
                "post-load dry-out still clears the wet-soil look");
        }

        [Test]
        public void StageGlyphSwap_SurvivesRoundTrip()
        {
            // A sprouted crop saved after its glyph/color swap must come
            // back showing the sprout, not the seed.
            var crop = _factory.CreateEntity("CandyCarrotCrop");
            crop.GetPart<CropPart>().Water(100);
            var zone = new Zone("z");
            zone.AddEntity(crop, 5, 5);
            CropSystem.Factory = _factory;
            try
            {
                for (int i = 0; i < 20; i++)
                    CropSystem.OnTickEnd(zone);
            }
            finally
            {
                CropSystem.Factory = null;
            }
            Assert.AreEqual("t", crop.GetPart<RenderPart>().RenderString, "sanity: sprouted");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(crop);

            Assert.AreEqual("t", loaded.GetPart<RenderPart>().RenderString);
            Assert.AreEqual("&g", loaded.GetPart<RenderPart>().ColorString);
            Assert.AreEqual(1, loaded.GetPart<CropPart>().GrowthStage);
        }

        [Test]
        public void CropSystemPart_RoundTrips_OnWorldEntity()
        {
            // The TickEnd router must survive on a saved world entity so
            // crops keep growing after load (pre-feature saves get the
            // defensive re-attach in GameBootstrap.ApplyLoadedGame).
            var world = new Entity { ID = "world", BlueprintName = "World" };
            world.SetTag("WorldEntity");
            world.AddPart(new CropSystemPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(world);

            Assert.IsNotNull(loaded.GetPart<CropSystemPart>(),
                "CropSystemPart survives the world entity round-trip");
        }
    }
}
