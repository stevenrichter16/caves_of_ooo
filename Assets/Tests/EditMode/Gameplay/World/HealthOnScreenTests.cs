using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The health readout actually reaching the two screens that should
    /// show it: look mode, and the interact menu opened with 'c'.
    ///
    /// <para><c>HealthReadoutTests</c> proves the string is right.
    /// <b>This file proves it is displayed</b> — the two are separate
    /// failures, and a correct string nobody renders is the more likely
    /// of the two to go unnoticed.</para>
    /// </summary>
    public class HealthOnScreenTests
    {
        [SetUp]
        public void SetUp() => FactionManager.Initialize();

        [TearDown]
        public void TearDown() => FactionManager.Reset();

        /// <summary>
        /// A RenderPart is what makes an entity VISIBLE to
        /// <c>LookQueryService</c> — without one it is not in the
        /// snapshot's object list at all and no detail line is produced,
        /// regardless of what health it has. (Learned the hard way: the
        /// first version of these fixtures omitted it and even the
        /// creature case failed.)
        /// </summary>
        private static Entity Visible(string name, string glyph)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = "&y",
                RenderLayer = 10
            });
            return e;
        }

        private static Entity Player()
        {
            var e = Visible("Player", "@");
            e.SetTag("Creature");
            e.SetTag("Player");
            e.AddPart(new PhysicsPart());
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            return e;
        }

        private static Entity Barrel(int hp = 3, int max = 8)
        {
            var e = Visible("WoodenBarrel", "0");
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new DestructiblePart { HP = hp, MaxHP = max });
            e.AddPart(new ExaminablePart());
            return e;
        }

        // ════════════════════════════════════════════════════════
        // Look mode
        // ════════════════════════════════════════════════════════

        [Test]
        public void LookMode_ShowsAnObjectsRemainingHitpoints()
        {
            var zone = new Zone("Z");
            var player = Player();
            var barrel = Barrel(hp: 3, max: 8);
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(barrel, 11, 10);

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 11, 10);

            Assert.IsTrue(snapshot.DetailLines.Any(l => l.Contains("HP 3/8")),
                "look mode should report how much the barrel has left; got: "
                + string.Join(" / ", snapshot.DetailLines));
        }

        [Test]
        public void LookMode_StillShowsCreatureHitpointsAndRelation()
        {
            // Counter-check: the readout replaced a Creature-gated block,
            // so the creature path has to keep working — including the
            // relation label, which sat inside the same branch.
            var zone = new Zone("Z");
            var player = Player();
            var snapjaw = Visible("Snapjaw", "s");
            snapjaw.SetTag("Creature");
            snapjaw.SetTag("Faction", "Snapjaws");
            snapjaw.AddPart(new PhysicsPart { Solid = true });
            snapjaw.Statistics["Hitpoints"] = new Stat
            { Owner = snapjaw, Name = "Hitpoints", BaseValue = 6, Min = 0, Max = 10 };

            zone.AddEntity(player, 10, 10);
            zone.AddEntity(snapjaw, 11, 10);

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 11, 10);

            Assert.IsTrue(snapshot.DetailLines.Any(l => l.Contains("HP 6/10")), "hp");
            Assert.IsTrue(snapshot.DetailLines.Any(l => l.Contains("hostile")), "relation");
        }

        [Test]
        public void LookMode_ShowsNoHealthLineForAThingThatHasNone()
        {
            var zone = new Zone("Z");
            var player = Player();
            var coin = Visible("GoldCoin", "*");
            coin.AddPart(new PhysicsPart { Takeable = true });
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(coin, 11, 10);

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 11, 10);

            Assert.IsFalse(snapshot.DetailLines.Any(l => l.Contains("HP")),
                "a coin has no hitpoints to report");
        }

        // ════════════════════════════════════════════════════════
        // The interact menu
        // ════════════════════════════════════════════════════════

        [Test]
        public void InteractMenu_TitleCarriesTheHitpoints()
        {
            var zone = new Zone("Z");
            var barrel = Barrel(hp: 3, max: 8);
            zone.AddEntity(barrel, 5, 5);

            string title = WorldActionMenuUI.BuildTitleFor(zone.GetCell(5, 5), barrel);

            StringAssert.Contains("HP 3/8", title);
            StringAssert.Contains("barrel", title.ToLowerInvariant(), "and still says what it is");
        }

        [Test]
        public void InteractMenu_SaysUnbreakableRatherThanANumber()
        {
            var zone = new Zone("Z");
            var stairs = Barrel(hp: 40, max: 40);
            stairs.GetPart<DestructiblePart>().Indestructible = true;
            zone.AddEntity(stairs, 5, 5);

            string title = WorldActionMenuUI.BuildTitleFor(zone.GetCell(5, 5), stairs);

            StringAssert.Contains(HealthReadout.UnbreakableLabel, title);
            StringAssert.DoesNotContain("40/40", title);
        }

        [Test]
        public void InteractMenu_APileGetsNoHitpointsAppended()
        {
            // On a pile the title is "A pile of items, including: …", and a
            // health figure hanging off the end would read as belonging to
            // the list rather than to one member of it.
            var zone = new Zone("Z");
            var barrel = Barrel();
            var coin = Visible("GoldCoin", "*");
            coin.AddPart(new PhysicsPart { Takeable = true });
            zone.AddEntity(barrel, 5, 5);
            zone.AddEntity(coin, 5, 5);

            var cell = zone.GetCell(5, 5);
            Assert.IsTrue(WorldInteractionSystem.IsPileCell(cell), "precondition: a pile");

            Assert.IsFalse(WorldActionMenuUI.BuildTitleFor(cell, barrel).Contains("HP"),
                "no health on a pile summary");
        }

        [Test]
        public void InteractMenu_TitleIsUnchangedForSomethingWithoutHealth()
        {
            // Counter-check on the append: an ordinary item's title must
            // come out exactly as it did before this feature.
            var zone = new Zone("Z");
            var coin = Visible("GoldCoin", "*");
            coin.AddPart(new PhysicsPart { Takeable = true });
            zone.AddEntity(coin, 5, 5);
            var cell = zone.GetCell(5, 5);

            Assert.AreEqual(WorldInteractionSystem.DescribeCell(cell),
                WorldActionMenuUI.BuildTitleFor(cell, coin));
        }
    }
}
