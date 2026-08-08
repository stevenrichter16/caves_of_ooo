using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase B2 — hermits, the wilderness helpers
    /// (Docs/BIOME-OVERHAUL.md §6 B2, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Four hermit archetypes (Mosskeeper / Saltwalker / Rotwood
    /// Herbalist / Lamplighter), one per biome, living in hut stamps in
    /// the ambient wilderness catalogs. Each offers a cheap paid rest
    /// (the generalized RestAtInn "cost:site" form) and trade (Villager
    /// lineage → wallet + TradeStockBuilder stock); the Herbalist also
    /// cures poison free via the new CureEffect conversation action —
    /// the jungle's pressure valve.
    /// </summary>
    [TestFixture]
    public class BiomeHermitTests
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
        public void Setup() => MessageLog.Clear();

        private static readonly (string blueprint, string conversation, BiomeType biome)[] Hermits =
        {
            ("CaveHermit", "Hermit_Mosskeeper", BiomeType.Cave),
            ("DesertHermit", "Hermit_Saltwalker", BiomeType.Desert),
            ("JungleHermit", "Hermit_Herbalist", BiomeType.Jungle),
            ("RuinsHermit", "Hermit_Lamplighter", BiomeType.Ruins),
        };

        // ── 1. Blueprints ────────────────────────────────────────

        [Test]
        public void HermitBlueprints_VillagerLineage_WithOwnVoices()
        {
            foreach (var (blueprint, conversation, _) in Hermits)
            {
                var hermit = _factory.CreateEntity(blueprint);
                Assert.IsNotNull(hermit, blueprint);
                Assert.AreEqual("Villagers", hermit.Tags["Faction"], $"{blueprint}: friendly");
                Assert.AreEqual(100, hermit.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                    $"{blueprint}: inherits the Villager wallet — can buy your loot");
                var conv = hermit.GetPart<ConversationPart>();
                Assert.IsNotNull(conv, blueprint);
                Assert.AreEqual(conversation, conv.ConversationID, blueprint);
            }
        }

        // ── 2. Conversations authored ────────────────────────────

        [Test]
        public void HermitConversations_Authored_WithPaidRest()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/Hermits.json"));
            foreach (var (_, conversation, _) in Hermits)
                StringAssert.Contains($"\"{conversation}\"", json);
            StringAssert.Contains("RestAtInn", json, "every hermit offers the cheap rest");
            StringAssert.Contains("CureEffect", json, "the Herbalist cures poison");
        }

        // ── 3. Hut stamps in the wilderness catalogs ─────────────

        [Test]
        public void WildernessCatalogs_CarryTheHermitHuts()
        {
            foreach (var (blueprint, _, biome) in Hermits)
            {
                bool found = false;
                foreach (var stamp in StampCatalog.For(biome))
                    foreach (var kvp in stamp.Legend)
                        if (kvp.Value == $"spawn:{blueprint}") found = true;
                Assert.IsTrue(found, $"{biome} wilderness can roll a {blueprint} hut");
            }
        }

        // ── 4. CureEffect conversation action ────────────────────

        private static Entity MakePatient(int drams = 0)
        {
            var patient = new Entity { ID = "patient" };
            patient.AddPart(new RenderPart { DisplayName = "patient" });
            patient.AddPart(new StatusEffectsPart());
            if (drams > 0) patient.SetIntProperty(TradeSystem.CURRENCY_PROP, drams);
            return patient;
        }

        [Test]
        public void CureEffect_RemovesTheNamedEffect_LeavesOthers()
        {
            var patient = MakePatient();
            var effects = patient.GetPart<StatusEffectsPart>();
            effects.ForceApplyEffect(new PoisonedEffect(10));
            effects.ForceApplyEffect(new BleedingEffect());
            Assert.IsNotNull(effects.GetEffect<PoisonedEffect>(), "precondition");

            ConversationActions.Execute("CureEffect", null, patient, "Poisoned");

            Assert.IsNull(effects.GetEffect<PoisonedEffect>(), "poison drawn out");
            Assert.IsNotNull(effects.GetEffect<BleedingEffect>(),
                "counter-check: only the NAMED effect is cured");
        }

        [Test]
        public void CureEffect_NothingToCure_PoliteNoOp()
        {
            var patient = MakePatient();
            ConversationActions.Execute("CureEffect", null, patient, "Poisoned");
            Assert.IsTrue(MessageLog.GetMessages().Count > 0,
                "the hermit says something rather than silently ignoring you");
        }

        // ── 5. RestAtInn cost:site form ──────────────────────────

        [Test]
        public void RestAtInn_CostSiteForm_ChargesAndNamesTheSite()
        {
            var zone = new Zone("T");
            SettlementRuntime.ActiveZone = zone;
            var tm = new TurnManager();
            FactionManager.Initialize();
            try
            {
                var patient = MakePatient(drams: 50);
                var hp = new Stat { Owner = patient, Name = "Hitpoints", BaseValue = 5, Min = 0, Max = 30 };
                patient.Statistics["Hitpoints"] = hp;
                zone.AddEntity(patient, 10, 10);

                ConversationActions.Execute("RestAtInn", null, patient, "8:hermit's fire");

                Assert.AreEqual(30, patient.GetStat("Hitpoints").Value, "healed");
                Assert.AreEqual(42, patient.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                    "8 drams, the hermit rate");
                Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("hermit's fire")),
                    "the rest message names the hermit's fire, not 'the inn'");
            }
            finally
            {
                FactionManager.Reset();
                SettlementRuntime.ActiveZone = null;
            }
        }
    }
}
