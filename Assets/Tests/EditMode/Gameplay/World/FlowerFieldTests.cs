using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/FELLING-W1-W2-PLAN.md SM3 — the bleed-wilt instrument. Canon:
    /// "Festival-flowers wilt before noon" is "the Thinning's most
    /// intimate symptom" (Lore/History/09_Magic.md:157). FlowerMeadow's
    /// conjured patch now rolls its lifespan against
    /// <see cref="Zone.UrquBleedLevel"/> — provably wired even though
    /// nothing writes that field above 0 yet (W7's bleed mask).
    /// </summary>
    public class FlowerFieldTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone OpenField(string id, float bleedLevel)
        {
            var zone = new Zone(id) { UrquBleedLevel = bleedLevel };
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var grass = _factory.CreateEntity("Grass");
                    if (grass != null) zone.AddEntity(grass, x, y);
                }
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        [Test]
        public void AtZeroBleed_FlowersGetTheFullBaseDuration()
        {
            var zone = OpenField("Overworld.6.5.0", bleedLevel: 0f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));

            bool foundOne = false;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "FlowerField") continue;
                foundOne = true;
                Assert.AreEqual(SpreadFormationBuilder.FlowerBaseDuration,
                    e.GetPart<LifespanPart>().TurnsRemaining);
                Assert.AreEqual(SpreadFormationBuilder.FlowerBaseDuration,
                    e.GetPart<TileStateSourcePart>().ResidueTurns);
            }
            Assert.IsTrue(foundOne, "the meadow placed no FlowerField at all");
        }

        [Test]
        public void AtHighBleed_FlowersWiltEarly()
        {
            var clean = OpenField("Overworld.6.5.0", bleedLevel: 0f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(clean, _factory, new Random(7));
            int cleanDuration = FirstFlowerDuration(clean);

            var bled = OpenField("Overworld.6.6.0", bleedLevel: 1f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(bled, _factory, new Random(7));
            int bledDuration = FirstFlowerDuration(bled);

            Assert.Less(bledDuration, cleanDuration,
                "a bleeding zone's flowers should wilt sooner than a clean zone's");
        }

        [Test]
        public void DurationNeverGoesBelowTheFloor()
        {
            var zone = OpenField("Overworld.6.5.0", bleedLevel: 999f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));

            Assert.GreaterOrEqual(FirstFlowerDuration(zone), SpreadFormationBuilder.FlowerMinDuration,
                "canon: a wilting charm still blooms, briefly — it must never hit zero/negative");
        }

        [Test]
        public void FlowerMeadowFormation_PlacesFlowerFieldNotCharmFlowers()
        {
            var zone = OpenField("Overworld.6.5.0", bleedLevel: 0f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));

            Assert.Greater(CountOf(zone, "FlowerField"), 0);
            Assert.AreEqual(0, CountOf(zone, "CharmFlowers"),
                "the formation should no longer place the mechanically-inert scenery version");
        }

        [Test]
        public void ResidueDurationMatchesTheEntitysOwnLifespan()
        {
            // Counter-check: the two numbers set at spawn are computed
            // once and applied to both parts — guards against a future
            // change to one formula and not the other.
            var zone = OpenField("Overworld.6.6.0", bleedLevel: 0.5f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));

            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "FlowerField") continue;
                Assert.AreEqual(
                    e.GetPart<TileStateSourcePart>().ResidueTurns,
                    e.GetPart<LifespanPart>().TurnsRemaining,
                    "residue lease and entity lifespan must agree — scenery and status say the same thing");
            }
        }

        [Test]
        public void GroundLine_ShowsPetalsWhereAFlowerFieldStands()
        {
            var zone = OpenField("Overworld.6.5.0", bleedLevel: 0f);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));

            Entity flower = null;
            (int x, int y) pos = (-1, -1);
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "FlowerField") continue;
                flower = e;
                pos = zone.GetEntityPosition(e);
                break;
            }
            Assert.IsNotNull(flower, "no FlowerField placed to check");

            var cell = zone.GetCell(pos.x, pos.y);
            cell.Explored = true;
            cell.IsVisible = true;
            flower.GetPart<TileStateSourcePart>().Seed(zone, pos.x, pos.y);

            StringAssert.Contains("petals", CellStatusReadout.GroundLine(zone, cell));
        }

        private static int FirstFlowerDuration(Zone zone)
        {
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "FlowerField")
                    return e.GetPart<LifespanPart>().TurnsRemaining;
            Assert.Fail("no FlowerField placed to measure");
            return -1;
        }
    }
}
