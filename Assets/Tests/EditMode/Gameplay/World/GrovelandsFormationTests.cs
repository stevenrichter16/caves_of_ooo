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
    /// W4.1 (Docs/FELLING-W4-PLAN.md §3) — the Grovelands' formations.
    /// The invariants the three sibling builders earned, plus this
    /// biome's own law: canon does the reachability argument for us —
    /// "Walk anywhere. Everything is path"
    /// (Lore/Codex/11_ChoirGroveSign.md) — so a grove ring that seals
    /// its own seep is a lore bug as much as a code bug. Solids placed
    /// AFTER the repair (the sign, the seated tendrils) prove their own
    /// placement: place, flood, keep only if everything stays reachable.
    /// </summary>
    public class GrovelandsFormationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone OpenLoam(string id = "Overworld.2.2.0")
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

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        private static Zone Built(Formation f, int seed = 7)
        {
            var zone = OpenLoam();
            new GrovelandsFormationBuilder { Override = f }
                .BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        // ════════════════════════════════════════════════════════
        // Selection
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryGrovelandsFormationIsReachable()
        {
            var seen = new HashSet<Formation>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    seen.Add(FormationSelector.For(BiomeType.Grovelands, $"Overworld.{x}.{y}.0"));

            foreach (Formation f in new[]
            {
                Formation.Grove, Formation.TendrilFen,
                Formation.FruitingWall, Formation.CompostingField,
            })
                CollectionAssert.Contains(seen, f, $"{f} never comes up anywhere on the map");
        }

        [Test]
        public void TheGrovelandsPool_DoesNotBleed()
        {
            // Every roll stays inside this biome's own formation block.
            for (int x = 0; x < 20; x++)
            {
                var g = FormationSelector.For(BiomeType.Grovelands, $"Overworld.{x}.5.0");
                Assert.GreaterOrEqual((int)g, (int)Formation.Grove,
                    $"the Grovelands rolled another biome's {g}");
            }
        }

        // ════════════════════════════════════════════════════════
        // Signatures + counter-check
        // ════════════════════════════════════════════════════════

        [TestCase(Formation.Grove, "MycelialColumn")]
        [TestCase(Formation.Grove, "GroveSeep")]
        [TestCase(Formation.Grove, "GroveSign")]
        [TestCase(Formation.TendrilFen, "ChoirTendril")]
        [TestCase(Formation.FruitingWall, "FruitingBody")]
        [TestCase(Formation.CompostingField, "CompostRow")]
        public void EachFormationLeavesItsOwnSignature(Formation f, string blueprint)
        {
            Assert.Greater(CountOf(Built(f), blueprint), 0, $"{f} placed no {blueprint}");
        }

        [Test]
        public void SignaturesDoNotCross()
        {
            var field = Built(Formation.CompostingField);
            Assert.AreEqual(0, CountOf(field, "GroveSeep"),
                "the field is not a grove — no seep");
            Assert.AreEqual(0, CountOf(field, "ChoirTendril"),
                "and nobody official is watching it");

            var grove = Built(Formation.Grove);
            Assert.AreEqual(0, CountOf(grove, "CompostRow"),
                "the grove keeps its taking-back elsewhere");
        }

        // ════════════════════════════════════════════════════════
        // Everything is path — the reachability law
        // ════════════════════════════════════════════════════════

        [TestCase(Formation.Grove)]
        [TestCase(Formation.TendrilFen)]
        [TestCase(Formation.FruitingWall)]
        public void SolidPlacers_NeverSealAnywhere(Formation f)
        {
            // "Walk anywhere. Everything is path." — the sign's first
            // law, enforced as the full every-open-cell property over
            // many seeds, including the post-repair solids (sign,
            // tendrils) that prove their own placement.
            for (int seed = 0; seed < 40; seed++)
            {
                var zone = Built(f, seed);
                var reached = FloodFromWest(zone);
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                        Assert.IsFalse(IsOpen(zone, x, y) && !reached[x, y],
                            $"{f} seed {seed}: open cell ({x},{y}) is sealed off");
            }
        }

        [Test]
        public void TheFen_AlwaysSeatsItsTendrils()
        {
            for (int seed = 0; seed < 15; seed++)
            {
                int n = CountOf(Built(Formation.TendrilFen, seed), "ChoirTendril");
                Assert.That(n, Is.InRange(1, 3),
                    $"seed {seed}: the fen guarantees 1-3 residents, found {n}");
            }
        }

        [TestCase(Formation.Grove)]
        [TestCase(Formation.FruitingWall)]
        [TestCase(Formation.CompostingField)]
        public void OnlyTheFen_SeatsTendrils(Formation f)
        {
            // Counter-check on the siting (tables may add more at zone
            // population time — this is builder-level only, like the
            // MawToad pin).
            Assert.AreEqual(0, CountOf(Built(f), "ChoirTendril"),
                $"{f} seated a tendril it should not have");
        }

        [Test]
        public void CompostRows_NeverStack()
        {
            // Plan R9(a): non-solid scatter goes through TryPlaceOnce
            // from birth — the OpenMire stacking lesson, applied before
            // the bug instead of after it.
            for (int seed = 0; seed < 15; seed++)
            {
                var zone = Built(Formation.CompostingField, seed);
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        int rows = 0;
                        foreach (var e in zone.GetCell(x, y).Objects)
                            if (e.BlueprintName == "CompostRow") rows++;
                        Assert.LessOrEqual(rows, 1,
                            $"seed {seed}: ({x},{y}) holds {rows} stacked rows");
                    }
            }
        }

        [Test]
        public void Grove_InRealForest_StillHasItsSeepAndSign()
        {
            // Look-pass finding, pinned — then STRENGTHENED by the W4.1
            // review: the first version scattered Trees (Solid tag, no
            // Wall tag), which ClearVegetation removes — but production
            // Grovelands terrain is JungleBuilder VineWall, which the
            // clearing deliberately skips, so a center rolled inside a
            // wall blob ENTOMBED the guaranteed seep in ~1 grove in 16
            // and CountOf==1 still passed. The fixture now scatters the
            // production wall class too, and the assert is cell-level:
            // the seep must stand OPEN and REACHED, not merely exist.
            for (int seed = 0; seed < 15; seed++)
            {
                var zone = OpenLoam();
                var scatterRng = new Random(seed * 31 + 7);
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        int roll = scatterRng.Next(100);
                        if (roll < 18)
                            zone.AddEntity(_factory.CreateEntity("Tree"), x, y);
                        else if (roll < 43)
                            zone.AddEntity(_factory.CreateEntity("VineWall"), x, y);
                    }

                new GrovelandsFormationBuilder { Override = Formation.Grove }
                    .BuildZone(zone, _factory, new Random(seed));
                // Production order: ConnectivityBuilder (priority 3000)
                // runs after the formation and connects regions — the
                // scatter above creates pockets the FORMATION didn't
                // cause and doesn't own. With connectivity now judging
                // by BlocksMovement (the review's fix), this is the real
                // end-to-end promise: the seep is reachable in the world
                // the player actually gets.
                new ConnectivityBuilder().BuildZone(zone, _factory, new Random(seed));

                Assert.AreEqual(1, CountOf(zone, "GroveSeep"),
                    $"seed {seed}: the grove clears its floor — the seep is guaranteed");
                Assert.GreaterOrEqual(CountOf(zone, "GroveSign"), 1,
                    $"seed {seed}: the grove makes room for its law");

                Entity seep = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "GroveSeep") seep = e;
                var cell = zone.GetEntityCell(seep);
                Assert.IsFalse(cell.BlocksMovement(seep),
                    $"seed {seed}: the seep is WATER, not a thing inside a wall");
                Assert.IsTrue(FloodFromWest(zone)[cell.X, cell.Y],
                    $"seed {seed}: 'Drink at the seep. It is for you' — you must be able to reach it");
            }
        }

        [Test]
        public void Grove_InSolidWallCountry_TheWaterStillFindsItsWayUp()
        {
            // Deterministic variant of the pin above (the scatter version
            // can miss the center by luck): EVERY interior cell is
            // VineWall, so the rolled center is walled by construction.
            // The one-cell extended dig must still deliver an OPEN seep.
            // (Reachability through miles of wall is ConnectivityBuilder's
            // job downstream — this pins only the builder's own promise.)
            for (int seed = 0; seed < 5; seed++)
            {
                var zone = new Zone("Overworld.2.2.0");
                for (int x = 2; x < Zone.Width - 2; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                        zone.AddEntity(_factory.CreateEntity("VineWall"), x, y);

                new GrovelandsFormationBuilder { Override = Formation.Grove }
                    .BuildZone(zone, _factory, new Random(seed));

                Entity seep = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "GroveSeep") seep = e;
                Assert.IsNotNull(seep, $"seed {seed}: the seep rises through anything");
                var cell = zone.GetEntityCell(seep);
                Assert.IsFalse(cell.BlocksMovement(seep),
                    $"seed {seed}: risen THROUGH the wall, not entombed in it");
            }
        }

        [Test]
        public void TheFen_IsABraid_NotAStrand()
        {
            // W4.1 review: the design says "Braid — tendrils tracing old
            // water-veins" and the plan says "braided lanes", but v1
            // shipped ONE sine strand — a single lane, no crossings, no
            // braid geometry possible. Two phase-offset strands cross
            // and part; the pin: enough columns must hold two coated
            // cells far apart vertically, which one strand's ~3-cell
            // band can never produce.
            int braidedColumns = 0;
            for (int seed = 0; seed < 6; seed++)
            {
                var zone = Built(Formation.TendrilFen, seed);
                for (int x = 2; x < Zone.Width - 2; x++)
                {
                    int lowest = -1, highest = -1;
                    for (int y = 2; y < Zone.Height - 2; y++)
                    {
                        if (!zone.TileState.HasCoating(x, y, "water")) continue;
                        if (lowest < 0) lowest = y;
                        highest = y;
                    }
                    if (lowest >= 0 && highest - lowest >= 4) braidedColumns++;
                }
            }
            Assert.GreaterOrEqual(braidedColumns, 12,
                "two strands cross and part — a lone sine band never separates by 4 rows");
        }

        // ════════════════════════════════════════════════════════
        // The grove's promises
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheColumns_GlowInTheDark()
        {
            // "Groves glow at night from across a chunk" — the glow is a
            // LightSourcePart on every column. The ASCII look pass
            // cannot see light (monochrome dump; honesty bound recorded
            // in the plan) so the part IS the verification.
            var column = _factory.CreateEntity("MycelialColumn");
            var light = column.GetPart<LightSourcePart>();
            Assert.IsNotNull(light, "the veins are lit");
            Assert.AreEqual(3, light.Radius, "soft radius 3 — a glow, not a floodlight");
        }

        [Test]
        public void TheSeep_IsFree_LikeTheSignSays()
        {
            // The seep carries the same Well contract as every village
            // well: drawing water cures Parched. "Drink at the seep. It
            // is for you."
            var seep = _factory.CreateEntity("GroveSeep");
            Assert.IsNotNull(seep.GetPart<WellPart>(), "the seep serves");
            Assert.IsFalse(seep.GetPart<PhysicsPart>().Solid,
                "and you can stand in it — it is water, not furniture");
        }

        /// <summary>Codex/11's body, whitespace-normalized. The W4.1
        /// review found the old six-phrase pin left ~8 sentences
        /// unguarded — a word could drift in the singers stanza and no
        /// test would blink. One equality now guards every word.</summary>
        private const string CanonSignBody =
            "WALKER. YOU ARE ENTERING US. "
            + "Walk anywhere. Everything is path. "
            + "Drink at the seep. It is for you. "
            + "Eat nothing red. The red is thinking. "
            + "If you hear singing, it is old singing. "
            + "It is not for you. You may listen anyway. "
            + "The singers do not mind. The singers "
            + "have not minded anything for a long time, "
            + "and it is a kindness to be heard. "
            + "Do not dig. "
            + "If you are tired — truly tired, walker, "
            + "the kind of tired that has stopped keeping count — "
            + "lie down anywhere soft. "
            + "Someone will cover you. "
            + "If you are only a little tired, keep walking. "
            + "We can tell the difference. "
            + "We are patient about the difference. "
            + "GO GENTLY. EVERYTHING HERE IS SOMEBODY.";

        [Test]
        public void TheSign_SpeaksTheWholeLaw_Verbatim()
        {
            // Codex/11, quoted whole — the letters are grown, not
            // carved, and neither is the text. The examine copy is a
            // short physical frame, then "It reads: " + the body;
            // the body must equal canon word for word.
            string text = _factory.CreateEntity("GroveSign")
                .GetPart<ExaminablePart>().Text;

            int i = text.IndexOf("It reads: ");
            Assert.GreaterOrEqual(i, 0, "the frame introduces the reading");
            string body = System.Text.RegularExpressions.Regex
                .Replace(text.Substring(i + "It reads: ".Length), @"\s+", " ").Trim();
            string canon = System.Text.RegularExpressions.Regex
                .Replace(CanonSignBody, @"\s+", " ").Trim();
            Assert.AreEqual(canon, body,
                "every word of the law, exactly — the grove does not paraphrase itself");
        }

        [Test]
        public void DrinkingAtTheSeep_CuresParched_EndToEnd()
        {
            // W4.1 review round 2 (hypothesis pin): the sign's promise
            // through the REAL action wire — a parched walker draws at
            // the seep and is cured, exactly like a village well.
            var seep = _factory.CreateEntity("GroveSeep");
            var walker = new Entity { ID = "walker", BlueprintName = "Walker" };
            walker.Tags["Creature"] = "";
            walker.AddPart(new RenderPart { DisplayName = "walker" });
            walker.ApplyEffect(new ParchedEffect(), null, null);
            Assert.IsTrue(walker.HasEffect<ParchedEffect>(), "setup: parched");

            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "DrawWaterAtWell");
            ev.SetParameter("Actor", (object)walker);
            seep.FireEvent(ev);
            ev.Release();

            Assert.IsFalse(walker.HasEffect<ParchedEffect>(),
                "\"Drink at the seep. It is for you.\" — and it works");
        }

        [Test]
        public void ABurningColumn_BurnsDown_AndItsLightDiesWithIt()
        {
            // Round 2 pin: the column is now ignitable (round 1 gave it
            // a flashpoint), it has HP 25, and fire consumes it through
            // the real structural path. The glow needs no cleanup —
            // the lightmap re-scans zone entities, so a removed column
            // is a dark column.
            var zone = new Zone("Z");
            var column = _factory.CreateEntity("MycelialColumn");
            zone.AddEntity(column, 10, 10);
            Assert.IsNotNull(column.GetPart<ThermalPart>(), "it can catch now");

            for (int i = 0; i < 6; i++)
            {
                var d = new Damage(5);
                d.AddAttribute("Fire");
                DestructionSystem.RouteDamage(column, d, null, zone);
            }

            Assert.IsNull(zone.GetEntityCell(column),
                "living Choir tissue burns — W4.2's fire-is-a-crime hook needs "
                + "exactly this to be possible");
        }

        [Test]
        public void TheColumnsGlow_SurvivesSaveAndLoad()
        {
            // Round 2 pin: LightSourcePart's fields ride the reflection
            // round-trip — a loaded grove still glows.
            var column = _factory.CreateEntity("MycelialColumn");
            var loaded = PartRoundTripHelper.RoundTripEntity(column);
            var light = loaded.GetPart<LightSourcePart>();
            Assert.IsNotNull(light, "the glow survives the save");
            Assert.AreEqual(3, light.Radius);
        }

        [Test]
        public void TheGrovelandsAmbientCatalog_IsItsOwn()
        {
            // W3 re-review carried forward: the Grovelands aliased the
            // whole Jungle catalog. The GroveShrine belongs HERE; the
            // Ziggurat does not.
            var names = new HashSet<string>();
            foreach (var s in StampCatalog.For(BiomeType.Grovelands)) names.Add(s.Name);

            CollectionAssert.Contains(names, "GroveShrine",
                "the five named choir NPCs live in Choir country");
            CollectionAssert.DoesNotContain(names, "Ziggurat",
                "the vine-choked ziggurat is jungle identity");
        }

        [Test]
        public void TheContainerPool_HoldsNothingManufactured()
        {
            // The Choir has no material culture — the absence of stuff
            // is the fingerprint. What you find grew or was carried in.
            var pool = ContainerPlacementService.PoolFor(
                BiomeType.Grovelands, ContainerPlacementService.ZoneKind.Wilderness);
            foreach (var kind in pool)
            {
                Assert.AreNotEqual("Crate", kind.Blueprint, "nobody here saws planks");
                Assert.AreNotEqual("Chest", kind.Blueprint);
                Assert.AreNotEqual("StrongBox", kind.Blueprint);
                Assert.AreNotEqual("Urn", kind.Blueprint, "nobody here fires clay");
            }
        }

        // ════════════════════════════════════════════════════════
        // Flood helpers (mirror the sibling suites)
        // ════════════════════════════════════════════════════════

        private static bool IsOpen(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }

        private static bool[,] FloodFromWest(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            for (int y = 1; y < Zone.Height - 1; y++)
                if (IsOpen(zone, 1, y)) { seen[1, y] = true; queue.Enqueue((1, y)); }
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny] || !IsOpen(zone, nx, ny)) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return seen;
        }
    }
}
