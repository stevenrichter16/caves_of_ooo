using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven tinkering tests. Pre-fix the TinkeringService
    /// had 20+ silent reject paths — Try* methods returned false with
    /// only an out-param reason string. Post-fix every call emits one
    /// diag record under <c>category="enhancement"</c>:
    ///   - <c>Crafted</c>/<c>ApplyModSucceeded</c>/<c>Disassembled</c> on success
    ///   - <c>CraftRejected</c>/<c>ApplyModRejected</c>/<c>DisassembleRejected</c>
    ///     on failure with the SAME reason string the UI surfaces.
    ///
    /// <para>This fixture exercises a representative subset of reject
    /// paths and the success path, dumping records to TestContext.</para>
    /// </summary>
    public class TinkeringObservabilityTests
    {
        private const string TestRecipesJson = @"{
  ""Recipes"": [
    {
      ""ID"": ""craft_thorn_dagger"",
      ""DisplayName"": ""Craft Thorn Dagger"",
      ""Blueprint"": ""ThornDagger"",
      ""Type"": ""Build"",
      ""Cost"": ""BC"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_sharp_melee"",
      ""DisplayName"": ""Apply Sharp"",
      ""Blueprint"": ""mod_sharp"",
      ""Type"": ""Mod"",
      ""Cost"": ""BC"",
      ""TargetPart"": ""MeleeWeapon"",
      ""NumberMade"": 1
    }
  ]
}";

        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""1"" } ] },
        { ""Name"": ""Render"",  ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""item"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""ThornDagger"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"",     ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""thorn dagger"" } ] },
        { ""Name"": ""TinkerItem"", ""Params"": [ { ""Key"": ""CanDisassemble"", ""Value"": ""true"" }, { ""Key"": ""BuildCost"", ""Value"": ""BC"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""PlainKnife"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"",      ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""plain knife"" } ] },
        { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            TinkerRecipeRegistry.ResetForTests();
            TinkerRecipeRegistry.InitializeFromJson(TestRecipesJson);
        }

        [TearDown]
        public void TearDown()
        {
            // Mirror the protection added when the test-pollution bug
            // landed: drop the test recipes so a Play-mode session
            // doesn't see them.
            TinkerRecipeRegistry.ResetForTests();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static EntityFactory CreateFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(TestBlueprintsJson);
            return f;
        }

        private static Entity CreatePlayer()
        {
            var p = new Entity { ID = "player", BlueprintName = "Player" };
            p.Statistics["Strength"] = new Stat
            { Name = "Strength", BaseValue = 16, Min = 1, Max = 50, Owner = p };
            p.AddPart(new InventoryPart());
            p.AddPart(new BitLockerPart());
            return p;
        }

        private static void DumpTinkerRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-20} actor={r.ActorId,-10} target={r.TargetId,-12} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void Craft_Success_EmitsCraftedRecord()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("BC");

            bool ok = TinkeringService.TryCraft(
                player, factory, "craft_thorn_dagger",
                out var crafted, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(1, crafted.Count);

            DumpTinkerRecords("craft thorn dagger success");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;

            // Note: ItemEnhancing's Apply hook can ALSO emit
            // enhancement records if the blueprint adds Parts; but
            // crafting a vanilla dagger doesn't trigger that. Expect 1.
            var crafted_records = records.Where(r => r.Kind == "Crafted").ToList();
            Assert.AreEqual(1, crafted_records.Count);
            StringAssert.Contains("\"recipeId\":\"craft_thorn_dagger\"", crafted_records[0].PayloadJson);
            StringAssert.Contains("\"craftedCount\":1", crafted_records[0].PayloadJson);
        }

        [Test]
        public void Craft_UnknownRecipe_EmitsCraftRejectedWithReason()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();

            bool ok = TinkeringService.TryCraft(
                player, factory, "nope_does_not_exist",
                out var crafted, out string reason);

            Assert.IsFalse(ok);
            Assert.IsEmpty(crafted);

            DumpTinkerRecords("craft rejected: unknown recipe");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("CraftRejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"Unknown recipe.\"", records[0].PayloadJson);
        }

        [Test]
        public void Craft_NotEnoughBits_EmitsCraftRejected()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("B");  // Need BC, have only B

            bool ok = TinkeringService.TryCraft(
                player, factory, "craft_thorn_dagger",
                out var crafted, out string reason);

            Assert.IsFalse(ok);

            DumpTinkerRecords("craft rejected: insufficient bits");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("CraftRejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"Not enough bits.\"", records[0].PayloadJson);
        }

        [Test]
        public void Craft_NullCrafter_EmitsCraftRejected()
        {
            var factory = CreateFactory();

            bool ok = TinkeringService.TryCraft(
                null, factory, "craft_thorn_dagger",
                out var crafted, out string reason);

            Assert.IsFalse(ok);

            DumpTinkerRecords("null crafter");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"reason\":\"Crafter is missing.\"", records[0].PayloadJson);
        }

        [Test]
        public void ApplyMod_TargetNotInInventory_EmitsApplyModRejected()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var knife = factory.CreateEntity("PlainKnife");
            Assert.IsNotNull(knife);
            // intentionally NOT adding to inventory

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_sharp_melee", knife, out string reason);

            Assert.IsFalse(ok);

            DumpTinkerRecords("mod rejected: target not in inventory");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("ApplyModRejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"You must own the target item.\"",
                records[0].PayloadJson);
            // Counter-check: target item populated
            StringAssert.Contains("\"targetBlueprint\":\"PlainKnife\"",
                records[0].PayloadJson);
        }

        [Test]
        public void Disassemble_NotOwned_EmitsDisassembleRejected()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var dagger = factory.CreateEntity("ThornDagger");
            Assert.IsNotNull(dagger);
            // intentionally NOT in inventory

            bool ok = TinkeringService.TryDisassemble(
                player, dagger, out string bits, out string reason);

            Assert.IsFalse(ok);

            DumpTinkerRecords("disassemble rejected: not owned");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("DisassembleRejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"You must own the item to disassemble it.\"",
                records[0].PayloadJson);
        }

        [Test]
        public void Disassemble_OwnedAndDisassemblable_EmitsDisassembled()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inv = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            var dagger = factory.CreateEntity("ThornDagger");
            inv.AddObject(dagger);

            bool ok = TinkeringService.TryDisassemble(
                player, dagger, out string yielded, out string reason);

            Assert.IsTrue(ok, reason);

            DumpTinkerRecords("disassemble success");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Disassembled", records[0].Kind);
            // yieldedBits should be BC matching the BuildCost
            StringAssert.Contains("\"yieldedBits\":\"BC\"", records[0].PayloadJson);
        }

        [Test]
        public void CraftThenDisassemble_EmitsTwoChronologicalRecords()
        {
            // Round-trip: craft a thorn dagger, disassemble it.
            // Diag should show Crafted → Disassembled in order.
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inv = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("BC");

            TinkeringService.TryCraft(player, factory, "craft_thorn_dagger",
                out var crafted, out _);
            Assert.AreEqual(1, crafted.Count);

            TinkeringService.TryDisassemble(player, crafted[0],
                out string yielded, out _);

            DumpTinkerRecords("craft → disassemble round trip");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("Crafted", records[0].Kind);
            Assert.AreEqual("Disassembled", records[1].Kind);
        }
    }
}
