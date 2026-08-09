using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LOOT OVERHAUL SM1 — creature loadouts.
    ///
    /// The verification sweep (Docs/LOOT-OVERHAUL.md §1) found that the
    /// death-drop mechanism already works perfectly at 100% — but every
    /// one of the game's 73 creatures owns NOTHING, because InventoryPart
    /// has no blueprint vocabulary for starting contents. These tests pin
    /// the new vocabulary: a creature that spawns WITH gear is a creature
    /// that drops gear, through the existing (untouched) death path.
    /// </summary>
    [TestFixture]
    public class LoadoutPartTests
    {
        private const string BlueprintsJson = @"
        {
          ""Objects"": [
            { ""Name"": ""PhysicalObject"", ""Parts"": [] },
            { ""Name"": ""Item"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""2"" } ] } ] },
            { ""Name"": ""ShortSword"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""short sword"" }, { ""Key"": ""RenderString"", ""Value"": ""/"" } ] },
                { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" } ] },
                { ""Name"": ""Equippable"", ""Params"": [ { ""Key"": ""Slot"", ""Value"": ""Hand"" } ] } ] },
            { ""Name"": ""Dagger"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""dagger"" }, { ""Key"": ""RenderString"", ""Value"": ""/"" } ] },
                { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" } ] },
                { ""Name"": ""Equippable"", ""Params"": [ { ""Key"": ""Slot"", ""Value"": ""Hand"" } ] } ] },
            { ""Name"": ""HealingTonic"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""healing tonic"" }, { ""Key"": ""RenderString"", ""Value"": ""!"" } ] } ] },
            { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Body"", ""Params"": [ { ""Key"": ""Anatomy"", ""Value"": ""Humanoid"" } ] },
                { ""Name"": ""Inventory"", ""Params"": [ { ""Key"": ""MaxWeight"", ""Value"": ""150"" } ] } ] },
            { ""Name"": ""Bandit"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""bandit"" }, { ""Key"": ""RenderString"", ""Value"": ""h"" } ] },
                { ""Name"": ""Loadout"", ""Params"": [
                    { ""Key"": ""Equip"", ""Value"": ""ShortSword"" },
                    { ""Key"": ""Carry"", ""Value"": ""HealingTonic:100"" } ] } ] },
            { ""Name"": ""PoorBandit"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""poor bandit"" }, { ""Key"": ""RenderString"", ""Value"": ""h"" } ] },
                { ""Name"": ""Loadout"", ""Params"": [
                    { ""Key"": ""Carry"", ""Value"": ""HealingTonic:0"" } ] } ] },
            { ""Name"": ""PickBandit"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""pick bandit"" }, { ""Key"": ""RenderString"", ""Value"": ""h"" } ] },
                { ""Name"": ""Loadout"", ""Params"": [
                    { ""Key"": ""Pick"", ""Value"": ""1;ShortSword;Dagger"" } ] } ] },
            { ""Name"": ""Beast"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""beast"" }, { ""Key"": ""RenderString"", ""Value"": ""b"" } ] } ] }
          ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(BlueprintsJson);
            LoadoutPart.Factory = _factory;
            LoadoutPart.Rng = new System.Random(1234);
        }

        [TearDown]
        public void TearDown()
        {
            LoadoutPart.Factory = null;
            LoadoutPart.Rng = null;
        }

        private static List<Entity> EquippedOf(Entity e)
        {
            var result = new List<Entity>();
            var inv = e.GetPart<InventoryPart>();
            if (inv == null) return result;
            foreach (var kvp in inv.EquippedItems)
                if (kvp.Value != null && !result.Contains(kvp.Value))
                    result.Add(kvp.Value);
            return result;
        }

        // ── Equip ────────────────────────────────────────────────

        [Test]
        public void Equip_PutsTheWeaponInTheCreaturesHand()
        {
            // The whole point: a bandit that SPAWNS with a sword is a
            // bandit that DROPS a sword, through the death path that
            // already works. No death-path changes needed.
            var bandit = _factory.CreateEntity("Bandit");

            var equipped = EquippedOf(bandit);
            Assert.AreEqual(1, equipped.Count, "exactly one item equipped");
            Assert.AreEqual("ShortSword", equipped[0].BlueprintName,
                "the bandit is actually holding the short sword");
        }

        [Test]
        public void Beast_WithNoLoadout_OwnsNothing()
        {
            // Counter-check: loadouts are ADDITIVE and opt-in. Beasts
            // keep natural weapons only — a spider must never drop
            // "fangs" (Docs/LOOT-OVERHAUL.md §1 correction 3).
            var beast = _factory.CreateEntity("Beast");

            Assert.AreEqual(0, EquippedOf(beast).Count, "no equipment");
            Assert.AreEqual(0, beast.GetPart<InventoryPart>().Objects.Count,
                "and nothing carried");
        }

        // ── Carry ────────────────────────────────────────────────

        [Test]
        public void Carry_AtFullChance_PutsTheItemInInventory()
        {
            var bandit = _factory.CreateEntity("Bandit");

            var carried = bandit.GetPart<InventoryPart>().Objects;
            Assert.AreEqual(1, carried.Count, "one carried item");
            Assert.AreEqual("HealingTonic", carried[0].BlueprintName);
        }

        [Test]
        public void Carry_AtZeroChance_IsSkipped()
        {
            // Counter-check for the chance roll: 0% must never grant.
            var poor = _factory.CreateEntity("PoorBandit");

            Assert.AreEqual(0, poor.GetPart<InventoryPart>().Objects.Count,
                "a 0% entry never spawns — without this the chance field is decorative");
        }

        // ── Pick ─────────────────────────────────────────────────

        [Test]
        public void Pick_GrantsExactlyNOfTheListedBlueprints()
        {
            // "1;ShortSword;Dagger" — weapon variety without authoring
            // one blueprint per bandit.
            var picked = _factory.CreateEntity("PickBandit");

            var equipped = EquippedOf(picked);
            Assert.AreEqual(1, equipped.Count, "exactly one pick");
            CollectionAssert.Contains(new[] { "ShortSword", "Dagger" },
                equipped[0].BlueprintName, "and it came from the list");
        }

        [Test]
        public void Pick_AcrossManySpawns_ProducesBothOptions()
        {
            // Pins that Pick actually RANDOMIZES — a buggy impl that
            // always took the first entry would pass the test above.
            var seen = new HashSet<string>();
            for (int i = 0; i < 40; i++)
            {
                LoadoutPart.Rng = new System.Random(i);
                var e = _factory.CreateEntity("PickBandit");
                var eq = EquippedOf(e);
                if (eq.Count > 0) seen.Add(eq[0].BlueprintName);
            }
            Assert.AreEqual(2, seen.Count,
                "both weapons appear across spawns — the pick is random, not fixed");
        }

        // ── Parser hardening (adversarial surface) ───────────────

        [Test]
        public void Parser_MalformedEntries_AreSkippedWithoutThrowing()
        {
            Assert.DoesNotThrow(() =>
            {
                LoadoutPart.ParseCarry(null);
                LoadoutPart.ParseCarry("");
                LoadoutPart.ParseCarry(";;;");
                LoadoutPart.ParseCarry("   ");
                LoadoutPart.ParseCarry("Dagger:notanumber");
                LoadoutPart.ParseCarry(":50");
                LoadoutPart.ParseCarry("Dagger:50x9-2");   // inverted range
                LoadoutPart.ParseCarry("Dagger:50xzz");
            });
            Assert.AreEqual(0, LoadoutPart.ParseCarry(";;;").Count);
            Assert.AreEqual(0, LoadoutPart.ParseCarry(":50").Count,
                "an entry with no blueprint name is not a grant");
        }

        [Test]
        public void Parser_CountRange_IsHonoredAndClamped()
        {
            var specs = LoadoutPart.ParseCarry("GoldCoin:100x3-5");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("GoldCoin", specs[0].Blueprint);
            Assert.AreEqual(100, specs[0].Chance);
            Assert.AreEqual(3, specs[0].MinCount);
            Assert.AreEqual(5, specs[0].MaxCount);

            // Inverted range clamps rather than throwing or looping.
            var inverted = LoadoutPart.ParseCarry("GoldCoin:100x9-2");
            Assert.LessOrEqual(inverted[0].MinCount, inverted[0].MaxCount,
                "an inverted range is repaired, not fatal");
        }

        [Test]
        public void Parser_ChanceOutOfRange_IsClamped()
        {
            Assert.AreEqual(100, LoadoutPart.ParseCarry("Dagger:500")[0].Chance);
            Assert.AreEqual(0, LoadoutPart.ParseCarry("Dagger:-20")[0].Chance);
        }

        // ── Graceful degradation ─────────────────────────────────

        [Test]
        public void NullFactory_IsAGracefulNoOp()
        {
            // Mirrors the CorpsePart.Factory / HarvestablePart.Factory
            // convention: tests and headless paths that leave the static
            // unwired must not explode.
            LoadoutPart.Factory = null;
            Entity bandit = null;
            Assert.DoesNotThrow(() => bandit = _factory.CreateEntity("Bandit"));
            Assert.IsNotNull(bandit, "the creature still spawns, just empty-handed");
            Assert.AreEqual(0, EquippedOf(bandit).Count);
        }

        [Test]
        public void UnknownBlueprintInLoadout_IsSkipped()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(BlueprintsJson.Replace(
                @"{ ""Key"": ""Equip"", ""Value"": ""ShortSword"" }",
                @"{ ""Key"": ""Equip"", ""Value"": ""NoSuchWeapon"" }"));
            LoadoutPart.Factory = f;

            // The factory logs the unknown blueprint (that's its contract);
            // what matters is that the creature still spawns.
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "EntityFactory: unknown blueprint 'NoSuchWeapon'");

            Entity bandit = null;
            Assert.DoesNotThrow(() => bandit = f.CreateEntity("Bandit"));
            Assert.AreEqual(0, EquippedOf(bandit).Count,
                "an unknown blueprint is skipped, not fatal");
        }
    }
}
