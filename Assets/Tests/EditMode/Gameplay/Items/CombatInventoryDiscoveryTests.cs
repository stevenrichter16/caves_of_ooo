using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CombatInventoryDiscoveryTests : FiftyWorldFixture
    {
        [Test]
        public void StandingVeilpuffExplainsItsHarvestsSightUtility()
        {
            var crop = Place("VeilpuffCrop");
            var description = crop.GetPart<ExaminablePart>().BuildExamineLine().ToLowerInvariant();
            StringAssert.Contains("sight", description);
            StringAssert.Contains("cold", description);
        }

        [Test]
        public void CropCatalogueExplainsVeilpuffSightUtility()
        {
            var rows = BiomeCropCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Farming/BiomeCrops.json")), out var errors);
            Assert.IsEmpty(errors);
            var description = rows.Single(row => row.Id == "Veilpuff").UseText.ToLowerInvariant();
            StringAssert.Contains("sight", description);
            StringAssert.Contains("cold", description);
        }

        [TestCase("", 0)]
        [TestCase("veil-mist", 4)]
        public void CarriedVeilpuffHelpExplainsCoverEvenWithLegacyExamineText(string cloud, int turns)
        {
            var item = Carry("VeilpuffBladder");
            var payload = item.GetPart<GasGrenadePart>();
            payload.SightCloud = cloud;
            payload.SightCloudTurns = turns;
            item.GetPart<ExaminablePart>().Description = "Old saved description: freezing mist.";

            Assert.True(MaterialUseDescription.TryDescribe(Actor, item, Factory, out var description));
            description = description.ToLowerInvariant();
            StringAssert.Contains("sight", description);
            StringAssert.Contains("four", description);
            StringAssert.Contains("cold", description);
            StringAssert.Contains("projectile", description);
            Assert.Contains(item, Pack.Objects, "Inspection must not consume the carried bladder.");
            Assert.AreEqual(cloud, payload.SightCloud, "Guidance must not migrate saved payload fields.");
            Assert.AreEqual(turns, payload.SightCloudTurns);
        }

        [TestCase("", -1)]
        [TestCase("veil-mist", 0)]
        [TestCase("veil-mist", 7)]
        [TestCase("steam", 4)]
        public void ChangedOrDisabledPayloadDoesNotReceiveNormalVeilHelp(string cloud, int turns)
        {
            var item = Carry("VeilpuffBladder");
            var payload = item.GetPart<GasGrenadePart>();
            payload.SightCloud = cloud;
            payload.SightCloudTurns = turns;
            Assert.False(MaterialUseDescription.TryDescribe(Actor, item, Factory, out var description));
            Assert.Null(description);
        }

        [TestCase("other-item")]
        [TestCase("other-gas")]
        [TestCase("missing-payload")]
        [TestCase("not-carried")]
        public void VeilHelpRequiresTheActualCarriedVeilpuffPayload(string fault)
        {
            var item = Carry("VeilpuffBladder");
            var payload = item.GetPart<GasGrenadePart>();
            if (fault == "other-item") item.BlueprintName = "AnotherCryoGrenade";
            if (fault == "other-gas") payload.GasId = "sleep-vapor";
            if (fault == "missing-payload") Assert.True(item.RemovePart(payload));
            if (fault == "not-carried") Assert.True(Pack.RemoveObject(item));
            Assert.False(MaterialUseDescription.TryDescribe(Actor, item, Factory, out var description));
            Assert.Null(description);
        }
    }
}
