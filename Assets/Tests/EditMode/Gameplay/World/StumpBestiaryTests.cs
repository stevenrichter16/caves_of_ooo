using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W6.3a (Docs/FELLING-W6-PLAN.md §3) — what lives at each height,
    /// and §7.6 state-reactive spawning.
    ///
    /// <para>Canon: "roughly a third of the bestiary exists to indicate
    /// a band or a state" (FELLING-WORLD-DESIGN §3.6), and "static
    /// tables kill the design" (§7.6). Two indicator species carry the
    /// machinery's live wirings, both straight from the bestiary
    /// source: the Sari-Snake is "Urqu's signs in flesh" with
    /// "increased spawning frequency during Urqu's manifest periods",
    /// and the Cascade-Father's ABSENCE is the ecological alarm — "a
    /// village whose nearby cascade no longer hosts Cascade-Fathers is
    /// a village in ecological trouble".</para>
    /// </summary>
    public class StumpBestiaryTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp() => NarrativeStatePart.Current = null;

        [TearDown]
        public void TearDown() => NarrativeStatePart.Current = null;

        private static NarrativeStatePart WorldWithFlags(params string[] set)
        {
            var state = new NarrativeStatePart();
            foreach (var f in set) state.SetFact(f, 1);
            NarrativeStatePart.Current = state;
            return state;
        }

        private static bool Rolled(PopulationTable table, string blueprint, int tries = 40)
        {
            for (int seed = 0; seed < tries; seed++)
                if (table.Roll(new Random(seed)).Contains(blueprint)) return true;
            return false;
        }

        // ════════════════════════════════════════════════════════════
        //   §7.6 — the predicate machinery
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ARequiredFlagGatesTheEntry()
        {
            var table = new PopulationTable { Name = "T" };
            table.Entries.Add(new PopulationEntry
            {
                BlueprintName = "SariSnake", Weight = 1, MinCount = 2, MaxCount = 2,
                RequiresWorldFlag = "UrquActive",
            });

            WorldWithFlags();                       // flag clear
            Assert.IsFalse(Rolled(table, "SariSnake"),
                "Urqu is quiet; its signs in flesh are not here");

            WorldWithFlags("UrquActive");           // flag set
            Assert.IsTrue(Rolled(table, "SariSnake"),
                "Urqu is manifest; the snakes are the first tell");
        }

        [Test]
        public void AForbiddenFlagSuppressesTheEntry()
        {
            var table = new PopulationTable { Name = "T" };
            table.Entries.Add(new PopulationEntry
            {
                BlueprintName = "CascadeFather", Weight = 1, MinCount = 2, MaxCount = 2,
                ForbidsWorldFlag = "EcologyDamaged",
            });

            WorldWithFlags();
            Assert.IsTrue(Rolled(table, "CascadeFather"),
                "a healthy cascade has its indicator");

            WorldWithFlags("EcologyDamaged");
            Assert.IsFalse(Rolled(table, "CascadeFather"),
                "and its absence IS the alarm");
        }

        [Test]
        public void NoWorldStateMeansUnflaggedEntriesStillRoll()
        {
            // Worldgen runs before (and in tests, without) a narrative
            // state. An unflagged entry must be unaffected, and a
            // REQUIRED flag with no world to read must fail closed —
            // never spawn Urqu's signs into a world that has no Urqu.
            NarrativeStatePart.Current = null;
            var table = new PopulationTable { Name = "T" };
            table.Entries.Add(new PopulationEntry
            { BlueprintName = "Plain", Weight = 1, MinCount = 1, MaxCount = 1 });
            table.Entries.Add(new PopulationEntry
            { BlueprintName = "Gated", Weight = 1, MinCount = 1, MaxCount = 1,
              RequiresWorldFlag = "UrquActive" });
            table.Entries.Add(new PopulationEntry
            { BlueprintName = "Warded", Weight = 1, MinCount = 1, MaxCount = 1,
              ForbidsWorldFlag = "EcologyDamaged" });

            var rolled = table.Roll(new Random(1));
            Assert.Contains("Plain", rolled, "an unflagged entry is untouched");
            Assert.IsFalse(rolled.Contains("Gated"), "a required flag fails closed");
            Assert.Contains("Warded", rolled, "a forbidden flag with no world is clear");
        }

        [Test]
        public void ExistingTablesAreUnaffected()
        {
            // Counter-check: the predicate is opt-in. Every shipped
            // table has null predicates and must roll exactly as it did.
            var cave = PopulationTable.GetBiomeTable(BiomeType.Cave, 1);
            Assert.Greater(cave.Roll(new Random(4)).Count, 0,
                "the cave still populates with no world state at all");
        }

        // ════════════════════════════════════════════════════════════
        //   The bands have their own tables
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheStumpNoLongerBorrowsTheCave()
        {
            // The sweep's 🔴: GetBiomeTable(Stump, *) returned
            // CaveTier1/2/3 — the mountain was populated by borrowed
            // cave content.
            foreach (var band in new[] { StumpBand.Foothills, StumpBand.Slopes, StumpBand.Summit })
            {
                var table = PopulationTable.GetStumpTable(band, 3);
                Assert.IsNotNull(table);
                StringAssert.Contains("Stump", table.Name, band.ToString());
                foreach (var e in table.Entries)
                    Assert.AreNotEqual("Snapjaw", e.BlueprintName,
                        band + " does not spawn cave content");
            }
        }

        [Test]
        public void EachBandHasItsOwnFauna()
        {
            // Canon sites the bestiary by band. The foothills get the
            // cascade species; the summit gets the singers.
            var foot = PopulationTable.GetStumpTable(StumpBand.Foothills, 3);
            Assert.IsTrue(Rolled(foot, "CascadeFather"),
                "the spray zone's indicator lives in the foothills");

            var summit = PopulationTable.GetStumpTable(StumpBand.Summit, 3);
            Assert.IsFalse(Rolled(summit, "CascadeFather"),
                "and not at the canopy, where there is no cascade");
        }

        [Test]
        public void EveryBandTableNamesRealBlueprints()
        {
            // The guard that catches a typo before a live zone does:
            // EntityFactory LOGS AN ERROR on an unknown blueprint, and
            // BuilderSpawn fails soft, so a misspelled entry is a
            // silently empty band.
            foreach (var band in new[] { StumpBand.Foothills, StumpBand.Slopes, StumpBand.Summit })
                for (int tier = 1; tier <= 3; tier++)
                    foreach (var e in PopulationTable.GetStumpTable(band, tier).Entries)
                        Assert.IsTrue(_factory.Blueprints.ContainsKey(e.BlueprintName),
                            $"{band}/tier{tier} names '{e.BlueprintName}', which does not exist");
        }

        // ════════════════════════════════════════════════════════════
        //   The creatures themselves
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheLowlandWaveExists_WithArt()
        {
            // The standing rule: no creature ships glyph-only.
            foreach (var bp in new[] { "SariSnake", "Wardline", "CascadeFather",
                                       "GlasspaneFrog", "YellowfootWayfarer" })
            {
                Assert.IsTrue(_factory.Blueprints.ContainsKey(bp), bp + " ships");
                string file = null;
                foreach (var (b, f, _) in EnvironmentSpriteRenderer.CreatureSprites)
                    if (b == bp) file = f;
                Assert.IsNotNull(file, bp + " is mapped to art");
                Assert.IsNotNull(
                    UnityEngine.Resources.Load<UnityEngine.Sprite>(
                        "Sprites/Environment/" + file),
                    bp + " maps to " + file + ", which must load");
            }
        }

        [Test]
        public void TheWardlineIsPassive_AndTheSariSnakeIsNot()
        {
            // Canon's structural opposition, as mechanics: the Wardline
            // is "passive toward the player by default"; the Sari-Snake
            // is a sit-and-wait ambush that strikes.
            var ward = _factory.CreateEntity("Wardline").GetPart<BrainPart>();
            Assert.IsNotNull(ward);
            Assert.IsTrue(ward.Passive, "the Wardline wards; it does not hunt you");

            var snake = _factory.CreateEntity("SariSnake").GetPart<BrainPart>();
            Assert.IsNotNull(snake);
            Assert.IsFalse(snake.Passive, "Urqu's sign strikes");
        }

        [Test]
        public void TheGentleOnesDoNotFightYou()
        {
            // Cascade-Father: "passive throughout combat". Glasspane
            // Frog: an aesthetic species. Yellowfoot: "effectively
            // non-combat", high natural armour from the shell.
            foreach (var bp in new[] { "CascadeFather", "GlasspaneFrog", "YellowfootWayfarer" })
            {
                var brain = _factory.CreateEntity(bp).GetPart<BrainPart>();
                Assert.IsNotNull(brain, bp + " has a brain");
                Assert.IsTrue(brain.Passive, bp + " is passive");
            }
        }

        [Test]
        public void TheSariSnakeIsUrqusSign_AndTheCascadeFatherIsTheAlarm()
        {
            // The two live §7.6 wirings, asserted on the SHIPPED tables
            // rather than a fixture — the machinery is only worth
            // having if real content uses it.
            var slopes = PopulationTable.GetStumpTable(StumpBand.Slopes, 3);
            PopulationEntry snake = null;
            foreach (var e in slopes.Entries)
                if (e.BlueprintName == "SariSnake") snake = e;
            Assert.IsNotNull(snake, "the slopes carry the Sari-Snake");
            Assert.AreEqual("UrquActive", snake.RequiresWorldFlag);

            var foot = PopulationTable.GetStumpTable(StumpBand.Foothills, 3);
            PopulationEntry father = null;
            foreach (var e in foot.Entries)
                if (e.BlueprintName == "CascadeFather") father = e;
            Assert.IsNotNull(father, "the foothills carry the Cascade-Father");
            Assert.AreEqual("EcologyDamaged", father.ForbidsWorldFlag);
        }

        [Test]
        public void AQuietWorldHasNoSariSnakes_ButStillHasAMountain()
        {
            // The end-to-end shape of the design: with Urqu quiet the
            // slopes still populate — the indicator's absence must not
            // empty the band.
            WorldWithFlags();
            var slopes = PopulationTable.GetStumpTable(StumpBand.Slopes, 3);
            bool anySnake = false, anythingElse = false;
            for (int seed = 0; seed < 30; seed++)
                foreach (var bp in slopes.Roll(new Random(seed)))
                {
                    if (bp == "SariSnake") anySnake = true; else anythingElse = true;
                }
            Assert.IsFalse(anySnake, "no signs, because there is nothing to sign");
            Assert.IsTrue(anythingElse, "but the mountain still has its animals");
        }
    }
}
