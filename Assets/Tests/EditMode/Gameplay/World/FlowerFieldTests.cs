using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/FELLING-W1-W2-PLAN.md SM3 — the bleed-wilt instrument. Canon:
    /// "Festival-flowers wilt before noon" is "the Thinning's most
    /// intimate symptom" (Lore/History/09_Magic.md:157). A FlowerField
    /// reads <see cref="Zone.UrquBleedLevel"/> the first turn it stands
    /// (<see cref="FlowerCharmPart"/>) and shortens its own life — proven
    /// here directly, even though nothing writes bleed above 0 yet.
    ///
    /// <para>Cold-eye pass (§2.3): the first cut computed the duration in
    /// the formation builder, so the FestivalField STAMP's flowers never
    /// wilted; and it leased the petal residue for the whole duration,
    /// which — because a source re-asserts every turn and a refresh keeps
    /// the longer lease — left ghost petals on the ground for a full
    /// season after the flowers were gone. Both are pinned below.</para>
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

        [SetUp]
        public void SetUp() => Diag.ResetAll();

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

        /// <summary>One MaterialSim-style EndTurn on an entity, with the
        /// zone attached — what the passive tick sends every flower.</summary>
        private static void EndTurn(Entity e, Zone zone)
        {
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Zone", (object)zone);
            e.FireEvent(ev);
            ev.Release();
        }

        private static Entity FirstFlower(Zone zone)
        {
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "FlowerField") return e;
            return null;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        private static Zone MeadowAt(float bleed, string id = "Overworld.6.5.0")
        {
            var zone = OpenField(id, bleed);
            new SpreadFormationBuilder { Override = Formation.FlowerMeadow }
                .BuildZone(zone, _factory, new Random(7));
            Assert.Greater(CountOf(zone, "FlowerField"), 0, "the meadow placed no FlowerField");
            return zone;
        }

        // ════════════════════════════════════════════════════════════
        // The instrument
        // ════════════════════════════════════════════════════════════

        [Test]
        public void AtZeroBleed_AFlowerGetsTheFullBaseDuration()
        {
            var zone = MeadowAt(0f);
            var flower = FirstFlower(zone);
            var charm = flower.GetPart<FlowerCharmPart>();

            EndTurn(flower, zone);   // roots on its first turn, then Lifespan ticks once

            Assert.IsTrue(charm.Rooted);
            Assert.AreEqual(charm.BaseDuration - 1, flower.GetPart<LifespanPart>().TurnsRemaining,
                "the charm sets the season, then this same tick's Lifespan countdown takes one");
        }

        [Test]
        public void AtHighBleed_AFlowerWiltsEarly()
        {
            var clean = MeadowAt(0f, "Overworld.6.5.0");
            var bled = MeadowAt(1f, "Overworld.6.6.0");
            var cleanFlower = FirstFlower(clean);
            var bledFlower = FirstFlower(bled);

            EndTurn(cleanFlower, clean);
            EndTurn(bledFlower, bled);

            Assert.Less(bledFlower.GetPart<LifespanPart>().TurnsRemaining,
                cleanFlower.GetPart<LifespanPart>().TurnsRemaining,
                "a bleeding zone's flowers wilt sooner than a clean zone's");
        }

        [Test]
        public void ComputeDuration_NeverGoesBelowTheFloor_AndNeverOverflows()
        {
            var charm = new FlowerCharmPart();
            Assert.AreEqual(charm.MinDuration, charm.ComputeDuration(999f));
            Assert.AreEqual(charm.MinDuration, charm.ComputeDuration(float.MaxValue),
                "a float→int overflow would wrap 'wilt completely' into a bonus season");
            Assert.AreEqual(charm.BaseDuration, charm.ComputeDuration(0f));
        }

        [Test]
        public void ComputeDuration_NegativeOrNaNBleed_ReadsAsZero()
        {
            // Nothing lengthens the season: a bad writer must not hand
            // out bonus turns.
            var charm = new FlowerCharmPart();
            Assert.AreEqual(charm.BaseDuration, charm.ComputeDuration(-5f));
            Assert.AreEqual(charm.BaseDuration, charm.ComputeDuration(float.NaN));
        }

        [Test]
        public void RootsOnce_ASecondTurnDoesNotResetTheSeason()
        {
            // Counter-check for the lazy first-tick design: without the
            // Rooted latch every tick would re-hand the flower a full
            // season and it would never wilt at all.
            var zone = MeadowAt(0f);
            var flower = FirstFlower(zone);
            EndTurn(flower, zone);
            EndTurn(flower, zone);
            EndTurn(flower, zone);

            Assert.AreEqual(flower.GetPart<FlowerCharmPart>().BaseDuration - 3,
                flower.GetPart<LifespanPart>().TurnsRemaining);
        }

        [Test]
        public void EndTurnWithoutAZone_DoesNotRoot_TriesAgainNextTick()
        {
            var flower = _factory.CreateEntity("FlowerField");
            var ev = GameEvent.New("EndTurn");   // no Zone parameter
            flower.FireEvent(ev);
            ev.Release();

            Assert.IsFalse(flower.GetPart<FlowerCharmPart>().Rooted);
        }

        // ════════════════════════════════════════════════════════════
        // Both placement paths behave the same — the reason the entity owns it
        // ════════════════════════════════════════════════════════════

        [Test]
        public void AStampPlacedFlower_WiltsExactlyLikeAFormationFlower()
        {
            // The FestivalField stamp goes through the generic
            // LandmarkBuilder, which knows nothing about bleed. If the
            // formula lived in SpreadFormationBuilder (as it first did),
            // this flower would keep its JSON default forever.
            var zone = OpenField("Overworld.6.6.0", bleedLevel: 1f);
            var flower = _factory.CreateEntity("FlowerField");
            zone.AddEntity(flower, 20, 12);   // however it got there

            EndTurn(flower, zone);

            var charm = flower.GetPart<FlowerCharmPart>();
            Assert.AreEqual(charm.ComputeDuration(1f) - 1,
                flower.GetPart<LifespanPart>().TurnsRemaining);
        }

        [Test]
        public void FlowerMeadowFormation_PlacesFlowerFieldNotCharmFlowers()
        {
            var zone = MeadowAt(0f);
            Assert.AreEqual(0, CountOf(zone, "CharmFlowers"),
                "the formation should no longer place the mechanically-inert scenery version");
        }

        // ════════════════════════════════════════════════════════════
        // The petal mark on the ground
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GroundLine_ShowsPetalsWhereAFlowerFieldStands()
        {
            var zone = MeadowAt(0f);
            var flower = FirstFlower(zone);
            var pos = zone.GetEntityPosition(flower);
            var cell = zone.GetCell(pos.x, pos.y);
            cell.Explored = true;
            cell.IsVisible = true;

            flower.GetPart<TileStateSourcePart>().Seed(zone, pos.x, pos.y);

            StringAssert.Contains("petals", CellStatusReadout.GroundLine(zone, cell));
        }

        [Test]
        public void PetalsFadeShortlyAfterTheFlowersAreGone()
        {
            // The residue is a SHORT lease the flower renews each turn —
            // like every other TileStateSource — not a copy of the
            // flower's lifespan. Otherwise the ground says "petals" for a
            // whole season after the meadow has wilted away.
            var zone = MeadowAt(0f);
            var flower = FirstFlower(zone);
            var pos = zone.GetEntityPosition(flower);
            var source = flower.GetPart<TileStateSourcePart>();
            Assert.LessOrEqual(source.ResidueTurns, 4, "a lease, not a lifespan");

            source.Seed(zone, pos.x, pos.y);
            Assert.IsTrue(zone.TileState.HasResidue(pos.x, pos.y, "petals"));

            zone.RemoveEntity(flower);            // the flowers are gone
            for (int i = 0; i < 6; i++) zone.TileState.Tick();

            Assert.IsFalse(zone.TileState.HasResidue(pos.x, pos.y, "petals"),
                "no flowers, no petals — within a handful of turns");
        }

        // ════════════════════════════════════════════════════════════
        // Hypothesis audit (Docs/FELLING-W1-W2-PLAN.md §2.3, second pass):
        // player flows the per-SM tests never simulated
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ASavedMidLifeFlower_DoesNotRerootIntoAFreshSeason()
        {
            // THE reason FlowerCharmPart.Rooted is a public field: parts
            // round-trip public fields via reflection (SaveSystem.cs:1270
            // WritePublicFields). If Rooted were private, every loaded
            // flower would re-root on its first post-load turn and hand
            // itself a full season — a wilted meadow un-wilting whenever
            // the player reloads. Save mid-life, load, tick: the countdown
            // must CONTINUE, not restart.
            var zone = MeadowAt(1f, "Overworld.6.6.0");   // bleeding → short season
            var flower = FirstFlower(zone);
            EndTurn(flower, zone);                          // roots short
            int midLife = flower.GetPart<LifespanPart>().TurnsRemaining;

            var loaded = PartRoundTripHelper.RoundTripEntity(flower);
            var charm = loaded.GetPart<FlowerCharmPart>();
            Assert.IsTrue(charm.Rooted, "the latch must survive the save");
            Assert.AreEqual(midLife, loaded.GetPart<LifespanPart>().TurnsRemaining);

            // The post-load tick continues the countdown — in a CLEAN zone,
            // which is the trap: an un-latched charm would read bleed 0 here
            // and reset the season to full.
            var cleanZone = new Zone("Overworld.6.5.0");
            EndTurn(loaded, cleanZone);
            Assert.AreEqual(midLife - 1, loaded.GetPart<LifespanPart>().TurnsRemaining,
                "a loaded flower continues its old season; it never re-reads the zone");
        }

        [Test]
        public void ACharmWithoutALifespanPart_RootsAsANoOp_NoCrash()
        {
            // Blueprint-misauthoring adversarial: FlowerCharm present,
            // Lifespan forgotten. The charm must latch and do nothing —
            // not throw on the missing part.
            var e = new Entity { BlueprintName = "Misauthored" };
            e.AddPart(new FlowerCharmPart());
            var zone = new Zone("Z");
            zone.AddEntity(e, 5, 5);

            Assert.DoesNotThrow(() => EndTurn(e, zone));
            Assert.IsTrue(e.GetPart<FlowerCharmPart>().Rooted);
        }

        // ════════════════════════════════════════════════════════════
        // Observability — the instrument speaks only when it has something to say
        // ════════════════════════════════════════════════════════════

        [Test]
        public void CharmWiltedDiag_OnlyWhenTheSeasonWasActuallyShortened()
        {
            var clean = MeadowAt(0f, "Overworld.6.5.0");
            EndTurn(FirstFlower(clean), clean);
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "event", Kind = "CharmWilted", Limit = 5 }).Records.Count,
                "at bleed 0 nothing wilted — eighty flowers must not say so eighty times");

            var bled = MeadowAt(0.5f, "Overworld.6.6.0");
            EndTurn(FirstFlower(bled), bled);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "event", Kind = "CharmWilted", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"bleedLevel\":0.5", recs[0].PayloadJson);
        }
    }
}
