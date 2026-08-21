using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3.5 (Docs/FELLING-W3-PLAN.md §3) — the two named places of the
    /// bog country stop being generic villages. Sumphold: boat-builders
    /// and peat-cutters, "Bog-Taken bodies are casually known here"
    /// (Lore/History/02_Geography.md:69) — and the toll-rolls of
    /// Codex/10, quoting the REGISTER ("short count — forgiven — my
    /// error"), never the reading's own confession. The Drowned Ledger:
    /// the Palimpsest's excavation camp, the reading-tent scene as
    /// architecture, the two Orders in joint presence.
    /// </summary>
    public class SoddenProfileTests
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

        private Zone Generate(string zoneID)
        {
            var zone = _mgr.GetZone(zoneID);
            Assert.IsNotNull(zone, zoneID + " should generate");
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
        // Sumphold
        // ════════════════════════════════════════════════════════

        [Test]
        public void Sumphold_HasItsBoatyard()
        {
            var zone = Generate("Overworld.15.6.0");

            Assert.GreaterOrEqual(CountOf(zone, "BoatFrame"), 1, "hulls on trestles");
            Assert.GreaterOrEqual(CountOf(zone, "TollRolls"), 1, "the rolls survive for this year");
            Assert.GreaterOrEqual(CountOf(zone, "PeatCutter"), 1, "and the cutters between jobs");
        }

        [Test]
        public void Tine_IsNotABoatTown()
        {
            // Counter-check on the name key: another Villagers-faction
            // place must not grow the boatyard.
            var zone = Generate("Overworld.13.7.0");

            Assert.AreEqual(0, CountOf(zone, "BoatFrame"));
            Assert.AreEqual(0, CountOf(zone, "TollRolls"));
            Assert.AreEqual(0, CountOf(zone, "PeatCutter"));
        }

        [Test]
        public void TheTollRolls_QuoteTheRegister_NeverTheReading()
        {
            // Codex/10's discipline, enforced: the ROLLS record
            // "short count — forgiven — my error" (the register); the
            // confession ("I was not kind", the carried face) exists
            // only in the reading, and the reading is not ours to quote.
            string text = _factory.CreateEntity("TollRolls")
                .GetPart<ExaminablePart>().Text;

            StringAssert.Contains("short count", text);
            StringAssert.Contains("forgiven", text);
            StringAssert.DoesNotContain("not kind", text,
                "the confession belongs to the page, not the props");
            StringAssert.DoesNotContain("her face", text);
            // W3 re-review: canon's own negative — the rolls DO name the
            // keeper (Oradin derives Ollun from them); what they do not
            // record is the crosser. "The rolls do not record a face."
            StringAssert.Contains("do not record a face", text,
                "the anonymity attaches to the forgiven crosser, not the keeper");
        }

        // ════════════════════════════════════════════════════════
        // The Drowned Ledger
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheLedger_IsAnExcavationCamp()
        {
            var zone = Generate(OverworldZoneManager.DrownedLedgerZoneID);

            Assert.AreEqual(1, CountOf(zone, "ReadingTable"),
                "one table, under canvas — the Codex/10 scene as architecture");
            Assert.GreaterOrEqual(CountOf(zone, "SurveyStake"), 3, "the numbering continues");
            Assert.GreaterOrEqual(CountOf(zone, "RecensionScribe"), 1, "the Recension keeps the count");
            Assert.GreaterOrEqual(CountOf(zone, "CurationSorter"), 1, "Curation keeps the tense");
            Assert.Greater(CountOf(zone, "TentWall"), 0, "the reading happens under cover");
        }

        [Test]
        public void Marrowstye_HasItsIntakeWindow()
        {
            // W3.6: the courier contract's destination is real — the
            // filer-clerk at the window, coffers behind.
            var zone = Generate("Overworld.12.12.0");

            Assert.GreaterOrEqual(CountOf(zone, "FilerClerk"), 1,
                "the institution is a person at a table");
            Assert.GreaterOrEqual(CountOf(zone, "StoneCoffer"), 1,
                "and the files behind the person");
        }

        [Test]
        public void TheTwoOrders_WearTheirOwnColors()
        {
            // W3 re-review (RED pre-fix): the CurationSorter shipped
            // Faction=Palimpsest — the Curation officer mechanically a
            // Recension member, so delivering her own contract's rep
            // never reached her, and Recension standing gated her mood.
            // Joint presence means two Orders, not one wearing two coats.
            var scribe = _factory.CreateEntity("RecensionScribe");
            var sorter = _factory.CreateEntity("CurationSorter");

            Assert.AreEqual("Palimpsest", FactionManager.GetFaction(scribe),
                "the scribe answers to the Recension");
            Assert.AreEqual("PaleCuration", FactionManager.GetFaction(sorter),
                "the sorter answers to Curation — her own contract's rep must reach her");
        }

        [Test]
        public void TheStampSwap_KeptTheCountAtExactlyThree()
        {
            // W3.2's contract survives W3.5's architecture: one body on
            // the table, two among the stakes — three, not four.
            var zone = Generate(OverworldZoneManager.DrownedLedgerZoneID);
            Assert.AreEqual(3, CountOf(zone, "PreFellingBody"));
        }

        // ════════════════════════════════════════════════════════
        // The voices
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheOrders_SpeakTheirCards()
        {
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")));

            string AllText(string id)
            {
                var conv = ConversationLoader.Get(id);
                Assert.IsNotNull(conv, id + " should load");
                var sb = new System.Text.StringBuilder();
                foreach (var n in conv.Nodes) sb.Append(n.Text).Append('\n');
                return sb.ToString();
            }

            StringAssert.Contains("Attribution before assertion", AllText("RecensionScribe_1"),
                "the Recension's card, spoken in the Recension's own tent");
            StringAssert.Contains("Status: continuing", AllText("CurationSorter_1"),
                "Curation's card: the file does not use the other word");
            StringAssert.Contains("Concord", AllText("PeatCutter_1"),
                "the controversy names its buyer");
            StringAssert.Contains("Two answers in one mouth", AllText("PeatCutter_1"),
                "and refuses to settle it");

            ConversationLoader.Reset();
        }

        [Test]
        public void EveryProfileNPC_HasItsConversationWired()
        {
            // The fail-loud wiring gate: a blueprint naming a
            // conversation that does not load is a silent mute NPC.
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")));

            foreach (var name in new[] { "RecensionScribe", "CurationSorter", "PeatCutter" })
            {
                var npc = _factory.CreateEntity(name);
                var conv = npc.GetPart<ConversationPart>();
                Assert.IsNotNull(conv, name + " should talk");
                Assert.IsNotNull(ConversationLoader.Get(conv.ConversationID),
                    name + " names conversation '" + conv.ConversationID + "', which does not load");
            }

            ConversationLoader.Reset();
        }
    }
}
