using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.4 SM-B (Docs/FELLING-W4-PLAN.md §6.2) — bloom-spores gas.
    /// The Bloom SITS BESIDE the fungal contagion arc (sweep row 5):
    /// its own def, its own part, its own chance constants — and the
    /// cross-arc counters pin that neither gas applies the other's
    /// effect.
    /// </summary>
    public class GasBloomSporesPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""bloom-spores"", ""GasType"":""BloomSpores"",
                ""Glyph"":""°"", ""Color"":""&m"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":""BloomSpores"" },
              { ""Id"":""fungal-spores"", ""GasType"":""FungalSpores"",
                ""Glyph"":""°"", ""Color"":""&G"",
                ""DefaultDensity"":80, ""DefaultLevel"":1,
                ""BehaviorKind"":""FungalSpores"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasBloomSporesPart.TestRng = null;
            GasFungalSporesPart.TestRng = null;
        }

        private static Entity MakeCreature(Zone zone, int x, int y,
            int toughness = 14, bool player = false)
        {
            var e = new Entity { ID = "c_" + x + "_" + y + (player ? "_p" : ""), BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            if (player) e.Tags["Player"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 200); S("Toughness", toughness);
            e.AddPart(new RenderPart { DisplayName = player ? "you" : "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   Content reachability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ShippedDef_LoadsAndAttachesThePart()
        {
            // The gate that proves the JSON file + the factory case,
            // against the SHIPPED content (not the inline fixture).
            GasRegistry.ResetForTests();
            GasRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/GasDefinitions/bloom-spores.json")));

            var zone = new Zone("BloomDefGate");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores");
            Assert.IsNotNull(gas, "the shipped def spawns");
            Assert.IsNotNull(gas.GetPart<GasBloomSporesPart>(),
                "BehaviorKind BloomSpores wires the part — without the " +
                "factory case this is a visual-only mist that takes no one");
        }

        // ════════════════════════════════════════════════════════════
        //   Taking
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Exposure_CanPutTheBloomOnABody()
        {
            // Level 9 vs Toughness 4 → chance 100: no RNG dependence.
            var zone = new Zone("BloomTake");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores", level: 9);
            var victim = MakeCreature(zone, 5, 5, toughness: 4);

            bool taken = gas.GetPart<GasBloomSporesPart>().ApplyGas(victim, zone);

            Assert.IsTrue(taken, "the Bloom takes what breathes it");
            Assert.IsTrue(victim.HasEffect<BloomedEffect>());
        }

        [Test]
        public void AlreadyBloomed_Bails_NoSecondTaking()
        {
            var zone = new Zone("BloomBail");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores", level: 9);
            var victim = MakeCreature(zone, 5, 5, toughness: 4);
            victim.ApplyEffect(new BloomedEffect());
            Diag.ResetAll();

            bool taken = gas.GetPart<GasBloomSporesPart>().ApplyGas(victim, zone);

            Assert.IsFalse(taken, "already worn — there is no more worn");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "BloomAlreadyPresent", Limit = 5 }).Records.Count,
                "the bail names itself (observability rule)");
        }

        [Test]
        public void TheHardBody_IsBeneathNotice()
        {
            // Counter: chance floors at 0 — Toughness 18 vs Level 1.
            Assert.AreEqual(0, GasBloomSporesPart.ComputeTakeChance(1, 18));
            var zone = new Zone("BloomHard");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores", level: 1);
            var victim = MakeCreature(zone, 5, 5, toughness: 18);

            for (int i = 0; i < 10; i++)
                gas.GetPart<GasBloomSporesPart>().ApplyGas(victim, zone);

            Assert.IsFalse(victim.HasEffect<BloomedEffect>(),
                "a hard enough body is beneath the Bloom's notice");
        }

        [Test]
        public void PlayerBody_CanBeTaken()
        {
            // The plan's player infection path: the filter chain has no
            // player gate — the Bloom does not know who is important.
            var zone = new Zone("BloomPlayer");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores", level: 9);
            var player = MakeCreature(zone, 5, 5, toughness: 4, player: true);

            bool taken = gas.GetPart<GasBloomSporesPart>().ApplyGas(player, zone);

            Assert.IsTrue(taken, "the player breathes the same air");
            Assert.IsTrue(player.HasEffect<BloomedEffect>());
        }

        // ════════════════════════════════════════════════════════════
        //   The arcs stay separate (sweep row 5 counters)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BloomSpores_DoNotApplyFungalInfection()
        {
            var zone = new Zone("BloomNotFungal");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "bloom-spores", level: 9);
            var victim = MakeCreature(zone, 5, 5, toughness: 4);

            gas.GetPart<GasBloomSporesPart>().ApplyGas(victim, zone);

            Assert.IsFalse(victim.HasEffect<FungalInfectionEffect>(),
                "the Bloom is not the rot — the arcs stay separate");
        }

        [Test]
        public void FungalSpores_DoNotApplyTheBloom()
        {
            var zone = new Zone("FungalNotBloom");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", level: 9);
            var victim = MakeCreature(zone, 5, 5, toughness: 4);

            gas.GetPart<GasFungalSporesPart>().ApplyGas(victim, zone);

            Assert.IsTrue(victim.HasEffect<FungalInfectionEffect>(),
                "precondition: the fungal arc still works");
            Assert.IsFalse(victim.HasEffect<BloomedEffect>(),
                "and it does not carry the Bloom");
        }
    }
}
