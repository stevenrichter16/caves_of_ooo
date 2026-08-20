using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3.2 (Docs/FELLING-W3-PLAN.md §3) — the Bog-Taken. Canon: the
    /// bodies are "the drowned of the cataclysm, still emerging from
    /// peat a thousand years later" (Lore/History/01_Spine.md:169), the
    /// worked bog is "a centuries-deep cemetery whose contents are
    /// visible" (02_Geography.md:69), and the Drowned Ledger's three
    /// pre-Felling preserved are one of the world's three thin sources
    /// on what came before (03_History.md:28).
    ///
    /// <para>The text gates are the strictest in the game (plan R3):
    /// no "mummy" anywhere in shipped examine copy — enforced game-wide
    /// here, permanently — state described without adjudicating
    /// consciousness, fragments attributed (a Recension marker), never
    /// confirmed (the Naro gate).</para>
    /// </summary>
    public class BogTakenTests
    {
        private static EntityFactory _factory;
        private static OverworldZoneManager _mgr;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));
            _mgr = new OverworldZoneManager(_factory, worldSeed: 42);
        }

        [OneTimeTearDown]
        public void TearDownOnce()
        {
            LootTableRegistry.ResetForTests();
            FactionManager.Reset();
        }

        private static Zone OpenBog(string id = "Overworld.16.3.0")
        {
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var grass = _factory.CreateEntity("Grass");
                    if (grass != null) zone.AddEntity(grass, x, y);
                }
            return zone;
        }

        private static Zone Built(Formation f, int seed)
        {
            var zone = OpenBog();
            new SoddenFormationBuilder { Override = f }
                .BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════
        // The ordinary drowned — seated in the worked faces
        // ════════════════════════════════════════════════════════

        [TestCase(Formation.PeatCuts)]
        [TestCase(Formation.BogFace)]
        public void TheCuts_SurfaceAtMostTwoBodies_AndSometimesOne(Formation f)
        {
            int total = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                int n = CountOf(Built(f, seed), "BogTakenBody");
                Assert.LessOrEqual(n, 2, $"{f} seed {seed}: the bog gives sparingly");
                total += n;
            }
            Assert.Greater(total, 0,
                $"{f}: thirty zones of worked peat and not one body — the seating never fires");
        }

        [Test]
        public void EveryBody_LiesAgainstAStandingBank()
        {
            // The invariant behind "seated in the faces": a body appears
            // where the peat is cut, not scattered anywhere wet. Runs
            // after the reachability repair, so the bank beside it is one
            // that SURVIVED.
            for (int seed = 0; seed < 12; seed++)
            {
                var zone = Built(Formation.PeatCuts, seed);
                foreach (var e in zone.GetAllEntities())
                {
                    if (e.BlueprintName != "BogTakenBody") continue;
                    var cell = zone.GetEntityCell(e);
                    bool bankAdjacent = false;
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var neighbor = zone.GetCell(cell.X + dx, cell.Y + dy);
                        if (neighbor == null) continue;
                        foreach (var n in neighbor.Objects)
                            if (n.BlueprintName == "PeatBank") bankAdjacent = true;
                    }
                    Assert.IsTrue(bankAdjacent,
                        $"seed {seed}: a body at ({cell.X},{cell.Y}) with no bank beside it");
                }
            }
        }

        [TestCase(Formation.OpenMire)]
        [TestCase(Formation.ReedMaze)]
        [TestCase(Formation.DrownedCopse)]
        [TestCase(Formation.Causeway)]
        public void TheUnworkedBog_KeepsItsDead(Formation f)
        {
            // Counter-check: only the CUT formations expose bodies. The
            // open mire holds its own — "bog-bodies become visible at
            // depth", and nobody has dug here.
            for (int seed = 0; seed < 6; seed++)
                Assert.AreEqual(0, CountOf(Built(f, seed), "BogTakenBody"),
                    $"{f} seed {seed}: an unworked formation surfaced a body");
        }

        // ════════════════════════════════════════════════════════
        // The Drowned Ledger's three
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheDrownedLedger_HoldsExactlyThreePreFellingBodies()
        {
            var zone = _mgr.GetZone(OverworldZoneManager.DrownedLedgerZoneID);
            Assert.IsNotNull(zone, "the Ledger should generate");
            Assert.AreEqual(3, CountOf(zone, "PreFellingBody"),
                "one of the world's three thin sources on pre-Felling: " +
                "exactly three bodies, hand-placed");
        }

        [Test]
        public void NoOtherZone_EverGrowsAPreFellingBody()
        {
            // Counter-check on the name-keyed gating: ordinary Sodden
            // wilderness, and Sumphold (the OTHER bog-country place,
            // where Bog-Taken are casually known but the pre-Felling
            // three do NOT live), must generate none.
            foreach (var id in new[]
            {
                "Overworld.17.0.0",   // ambient Sodden (DrownedCopse)
                "Overworld.14.0.0",   // ambient Sodden (PeatCuts)
                "Overworld.15.6.0",   // Sumphold
            })
                Assert.AreEqual(0, CountOf(_mgr.GetZone(id), "PreFellingBody"),
                    id + " must not hold a pre-Felling body");
        }

        // ════════════════════════════════════════════════════════
        // The text gates (plan R3) — permanent, game-wide
        // ════════════════════════════════════════════════════════

        [Test]
        public void NoShippedExamineText_UsesTheBannedWord()
        {
            // The register's frame is "found, not made" — the word
            // "mummy" (and its derivatives) imports the wrong museum.
            // Game-wide and permanent: any future blueprint that ships it
            // fails here, whatever feature it arrives with.
            var offenders = new List<string>();
            foreach (var name in _factory.Blueprints.Keys)
            {
                Entity e;
                try { e = _factory.CreateEntity(name); }
                catch (System.Exception) { continue; }
                var text = e?.GetPart<ExaminablePart>()?.Text;
                if (string.IsNullOrEmpty(text)) continue;
                if (text.ToLowerInvariant().Contains("mummi") ||
                    text.ToLowerInvariant().Contains("mummy"))
                    offenders.Add(name);
            }
            CollectionAssert.IsEmpty(offenders,
                "examine copy uses the banned word: " + string.Join(", ", offenders));
        }

        [Test]
        public void ThePreFellingText_AttributesItsFragments_AndConfirmsNothing()
        {
            var body = _factory.CreateEntity("PreFellingBody");
            string text = body.GetPart<ExaminablePart>().Text;

            StringAssert.Contains("Recension", text,
                "the fragments are the expedition's reading, and the tag says so " +
                "— attribution is the load-bearing word");
            StringAssert.DoesNotContain("Naro", text,
                "the Naro gate: the body confirms nothing about the seventh");
            StringAssert.DoesNotContain("seventh", text,
                "the Naro gate: not even the question");
        }

        [Test]
        public void TheBodies_AreFoundNotTaken()
        {
            // Non-takeable (the ambient dead are not inventory — W3.6's
            // SEALED body is the courier's cargo, a different blueprint)
            // and non-solid (you stop at a body; it never walls a
            // trench-edge passage the reachability repair can't see).
            foreach (var name in new[] { "BogTakenBody", "PreFellingBody" })
            {
                var e = _factory.CreateEntity(name);
                var phys = e.GetPart<PhysicsPart>();
                Assert.IsFalse(phys.Takeable, name + " must not be pocketable");
                Assert.IsFalse(phys.Solid, name + " must not block movement");
            }
        }

        [Test]
        public void ExaminingABody_SpeaksItsText()
        {
            // End-to-end through the InventoryAction wire — pins that the
            // authored copy actually reaches the player (the SM0 lesson:
            // 40 blueprints once shipped with silently dropped prose).
            var body = _factory.CreateEntity("BogTakenBody");
            MessageLog.Clear();
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            body.FireEvent(ev);
            ev.Release();

            StringAssert.Contains("the peat kept", MessageLog.GetLast(),
                "the examine line carries the authored copy");
        }
    }
}
