using System;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.4 SM-D (Docs/FELLING-W4-PLAN.md §6.2) — the Bloom-front.
    /// Membership is a pure function of the zone id (salted
    /// StableIndex — never the retry-reseeded builder rng); POI zones
    /// are excluded structurally at pipeline assembly (R8); shrine
    /// zones stand the front down; the front seeds blooming fruiting
    /// bodies, takes 1-2 existing brained creatures, and leaves the
    /// workshop unfinished.
    /// </summary>
    public class BloomFrontTests
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
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""bloom-spores"", ""GasType"":""BloomSpores"",
                ""Glyph"":""°"", ""Color"":""&m"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":""BloomSpores"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            SettlementRuntime.Reset();
        }

        private static Zone FlooredZone(string id)
        {
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var g = _factory.CreateEntity("Grass");
                    if (g != null) zone.AddEntity(g, x, y);
                }
            return zone;
        }

        private static Entity AddCreature(Zone zone, int x, int y, bool brained = true)
        {
            // PRODUCTION-FAITHFUL (close-out 🔴 #2): no blueprint ships
            // a StatusEffects part — Entity.ApplyEffect creates it
            // lazily on first use. The first fixture AddPart-ed it and
            // masked a builder filter that rejected every real creature
            // (hosts were always 0 in live front zones).
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new PhysicsPart { Solid = true });
            if (brained)
                e.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(7) });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static int CountBlueprint(Zone zone, string name)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == name) n++;
            return n;
        }

        private static int CountBloomed(Zone zone)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.HasEffect<BloomedEffect>()) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════
        //   The roll
        // ════════════════════════════════════════════════════════════

        [Test]
        public void FrontRoll_IsDeterministic_AndRare()
        {
            Assert.AreEqual(BloomFrontBuilder.IsBloomFront("Overworld.3.5.0"),
                            BloomFrontBuilder.IsBloomFront("Overworld.3.5.0"),
                "same id, same answer — across sessions and retries");

            int fronts = 0, sample = 400;
            for (int i = 0; i < sample; i++)
                if (BloomFrontBuilder.IsBloomFront($"Overworld.{i % 20}.{i / 20}.{i}"))
                    fronts++;
            Assert.That(fronts, Is.InRange(sample / 100, sample / 5),
                $"~8% of zones hold a front, not none and not most ({fronts}/{sample})");
        }

        // ════════════════════════════════════════════════════════════
        //   What a front zone holds
        // ════════════════════════════════════════════════════════════

        [Test]
        public void AFrontZone_SeedsTheBloom()
        {
            var zone = FlooredZone("Overworld.3.5.0");
            AddCreature(zone, 20, 10);
            AddCreature(zone, 40, 12);
            AddCreature(zone, 60, 8);

            new BloomFrontBuilder { Override = true }
                .BuildZone(zone, _factory, new Random(3));

            Assert.GreaterOrEqual(CountBlueprint(zone, "BloomingFruitingBody"),
                BloomFrontBuilder.FruitingMin,
                "the front grows through the zone");
            Assert.That(CountBloomed(zone), Is.InRange(1, BloomFrontBuilder.HostMax),
                "one or two of the residents are already worn");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "worldgen", Kind = "BloomFront", Limit = 5 }).Records.Count,
                "the front records itself");
        }

        [Test]
        public void NonFrontZone_IsUntouched()
        {
            // Counter-check: the roll says no, nothing happens.
            var zone = FlooredZone("Overworld.9.9.0");
            AddCreature(zone, 20, 10);

            new BloomFrontBuilder { Override = false }
                .BuildZone(zone, _factory, new Random(3));

            Assert.AreEqual(0, CountBlueprint(zone, "BloomingFruitingBody"));
            Assert.AreEqual(0, CountBloomed(zone));
        }

        [Test]
        public void ShrinePresence_StandsTheFrontDown()
        {
            // R8: the shrine is not a POI — the veto is content-level.
            var zone = FlooredZone("Overworld.3.5.0");
            var keeper = _factory.CreateEntity("Mogu");
            Assert.IsNotNull(keeper, "the shrine keeper blueprint exists");
            zone.AddEntity(keeper, 30, 10);
            AddCreature(zone, 20, 10);

            new BloomFrontBuilder { Override = true }
                .BuildZone(zone, _factory, new Random(3));

            Assert.AreEqual(0, CountBlueprint(zone, "BloomingFruitingBody"),
                "the front does not land where the Choir keeps a shrine");
            Assert.AreEqual(0, CountBloomed(zone));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "worldgen", Kind = "BloomFrontSkipped", Limit = 5 }).Records.Count,
                "the veto names itself");
        }

        [Test]
        public void TheWorkshop_IsLeftUnfinished()
        {
            var zone = FlooredZone("Overworld.3.5.0");
            AddCreature(zone, 20, 10);

            new BloomFrontBuilder { Override = true }
                .BuildZone(zone, _factory, new Random(3));

            Assert.GreaterOrEqual(CountBlueprint(zone, "SmithAnvil"), 1,
                "tools down mid-task — the set dressing is real objects");
        }

        [Test]
        public void BrainlessResidents_AreNotDriven_ButTheFrontStillLands()
        {
            // A host that cannot act is not a scene; the growth comes
            // anyway.
            var zone = FlooredZone("Overworld.3.5.0");
            AddCreature(zone, 20, 10, brained: false);
            AddCreature(zone, 40, 12, brained: false);

            new BloomFrontBuilder { Override = true }
                .BuildZone(zone, _factory, new Random(3));

            Assert.AreEqual(0, CountBloomed(zone),
                "only creatures with a will to override get driven");
            Assert.GreaterOrEqual(CountBlueprint(zone, "BloomingFruitingBody"),
                BloomFrontBuilder.FruitingMin);
        }

        [Test]
        public void SafeSite_RefusesPartSolidNeighbors()
        {
            // Close-out 🟡 #5 — the W4.1 ConnectivityBuilder lesson
            // recurring: Cell.IsPassable is TAG-only, but GroveSign,
            // SmithAnvil, and every haulable are Part-solid (no tag).
            // A "safe" site beside one is not safe — the
            // no-disconnect proof needs BlocksMovement.
            var zone = FlooredZone("Overworld.3.5.0");
            var sign = new Entity { ID = "sign", BlueprintName = "GroveSign" };
            sign.AddPart(new RenderPart());
            sign.AddPart(new PhysicsPart { Solid = true }); // Part-solid, NO tag
            zone.AddEntity(sign, 10, 10);

            Assert.IsFalse(BloomFrontBuilder.IsSafeSolidSite(zone, 11, 10),
                "a Part-solid neighbor voids the open-ring proof");
            Assert.IsFalse(BloomFrontBuilder.IsSafeSolidSite(zone, 10, 10),
                "and an occupied center is no site at all");
            Assert.IsTrue(BloomFrontBuilder.IsSafeSolidSite(zone, 40, 10),
                "open field remains a site");
        }

        // ════════════════════════════════════════════════════════════
        //   R8 — where the front may not land (structural pin)
        // ════════════════════════════════════════════════════════════

        private sealed class ExposingManager : OverworldZoneManager
        {
            public ExposingManager(EntityFactory f) : base(f, worldSeed: 1234) { }
            public ZoneGenerationPipeline Pipe(string id) => GetPipelineForZone(id);
        }

        private static bool CarriesFront(ZoneGenerationPipeline p)
        {
            foreach (var b in p.Builders)
                if (b is BloomFrontBuilder) return true;
            return false;
        }

        [Test]
        public void Cinderhold_CarriesNoFrontBuilder_WildernessDoes()
        {
            // The exclusion is pipeline ASSEMBLY, not an in-builder
            // check (builders cannot reach WorldMap.POIs): the front
            // builder registers only in the Grovelands wilderness arm.
            var mgr = new ExposingManager(_factory);

            Assert.IsFalse(CarriesFront(mgr.Pipe("Overworld.6.6.0")),
                "Cinderhold is a Place — a Bloomed village is a design " +
                "conversation, not a roll (R8)");

            bool wildernessHasIt = false;
            foreach (int x in new[] { 0, 1, 2, 3, 5 })
                if (CarriesFront(mgr.Pipe($"Overworld.{x}.6.0")))
                    wildernessHasIt = true;
            Assert.IsTrue(wildernessHasIt,
                "and wilderness Grovelands pipelines do carry it");
        }

        // ════════════════════════════════════════════════════════════
        //   The content rhyme (already-shipped substrate, pinned here)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurningABloomingBody_ReleasesTheBloom()
        {
            // The peat lesson, rhymed: fire on the front's growth burns
            // off bloom-spores. End-to-end through the shipped blueprint.
            var zone = new Zone("BloomBurn");
            SettlementRuntime.ActiveZone = zone;
            var body = _factory.CreateEntity("BloomingFruitingBody");
            Assert.IsNotNull(body, "the blueprint ships");
            zone.AddEntity(body, 5, 5);

            // RouteDamage, not ApplyDamage: the growth has structural HP,
            // not a Hitpoints stat — the exact seam BurningEffect uses
            // (BurningEffect.cs:146-151).
            var d = new Damage(6);
            d.AddAttribute("Heat");
            DestructionSystem.RouteDamage(body, d, null, zone);

            int density = 0;
            foreach (var e in zone.GetEntitiesWithTag("Gas"))
            {
                var pool = e.GetPart<GasPoolPart>();
                if (pool != null && pool.GasId == "bloom-spores") density += pool.Density;
            }
            Assert.Greater(density, 0, "burning the front spreads the Bloom");
        }
    }
}
