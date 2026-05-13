using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Tests.TestSupport;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.4 — Tinker recipe shims that turn the
    /// 3 mineral blueprints (PaleSalt/ChoirIron/GlowQuartz) into the
    /// 3 corresponding Enhancement Parts via
    /// <c>TinkeringService.TryApplyModification</c>.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Each shim's <c>CanApply</c> + <c>Apply</c> contract:
    ///         positive (right target type) + counter-check (wrong type).</item>
    ///   <item>Through-the-full-recipe-path: <c>TryApplyModification</c>
    ///         consumes the mineral, applies the Enhancement Part,
    ///         updates display name with the adjective prefix.</item>
    ///   <item>Slot-cap respect: pre-loaded at-cap item rejects.</item>
    ///   <item>Recipe JSON entries are registered + reachable.</item>
    /// </list>
    ///
    /// <para><b>Counter-checks:</b> applying a vs-Undead Pale-Salt to
    /// armor (no MeleeWeaponPart) rejects at <c>CanApply</c>; the
    /// ingredient is NOT consumed; bits NOT spent.</para>
    /// </summary>
    public class MineralInfusionTinkerTests
    {
        private const string TestRecipesJson = @"{
  ""Recipes"": [
    {
      ""ID"": ""mod_palesalt_infuse"",
      ""DisplayName"": ""Infuse with Pale-Salt"",
      ""Blueprint"": ""mod_palesalt"",
      ""Type"": ""Mod"",
      ""Cost"": """",
      ""Ingredient"": ""PaleSalt"",
      ""TargetPart"": ""MeleeWeapon"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_choiriron_infuse"",
      ""DisplayName"": ""Infuse with Choir-Iron"",
      ""Blueprint"": ""mod_choiriron"",
      ""Type"": ""Mod"",
      ""Cost"": """",
      ""Ingredient"": ""ChoirIron"",
      ""TargetPart"": ""MeleeWeapon"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_glowquartz_infuse"",
      ""DisplayName"": ""Tip with Glow-Quartz"",
      ""Blueprint"": ""mod_glowquartz"",
      ""Type"": ""Mod"",
      ""Cost"": """",
      ""Ingredient"": ""GlowQuartz"",
      ""TargetPart"": ""Equippable"",
      ""NumberMade"": 1
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            // Force auto-load so the production registration path is exercised
            // (mirrors what real game-start does).
            EnhancementFactory.ForceReinitialize();
            EnhancementFactory.EnsureInitialized();
            TinkerRecipeRegistry.ResetForTests();
            TinkerRecipeRegistry.InitializeFromJson(TestRecipesJson);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore production recipes for subsequent tests.
            TinkerRecipeRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror TinkeringServiceTests.CreatePlayer) ──

        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        private Entity CreatePlayerWithMineral(string mineralBlueprint, string bits = "BBCC")
        {
            var player = new Entity { BlueprintName = "Player", ID = "p" };
            player.Tags["Player"] = "";
            player.Statistics["Strength"] = new Stat
                { Name = "Strength", BaseValue = 16, Min = 1, Max = 50, Owner = player };
            player.AddPart(new InventoryPart());
            player.AddPart(new BitLockerPart());

            var inv = player.GetPart<InventoryPart>();
            var bitLocker = player.GetPart<BitLockerPart>();
            bitLocker.AddBits(bits);

            var mineral = _harness.Factory.CreateEntity(mineralBlueprint);
            Assert.IsNotNull(mineral, $"Blueprint {mineralBlueprint} must be registered.");
            inv.AddObject(mineral);

            return player;
        }

        // ── PaleSalt: full recipe path ────────────────────────────

        [Test]
        public void PaleSalt_RecipePath_AppliesEnhancement_ConsumesMineral_NoBitsSpent()
        {
            // Design choice (post-E.5.1 user feedback): mineral recipes
            // have ZERO bit cost — the mineral IS the cost. Pre-E.5.1
            // this test expected 1B + 1C to be spent on top of the
            // mineral consumption; that redundant cost was dropped.
            var player = CreatePlayerWithMineral("PaleSalt");
            var inv = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("mod_palesalt_infuse");
            int bitsBeforeB = bits.GetBitCount('B');
            int bitsBeforeC = bits.GetBitCount('C');

            // Create a Cutting melee weapon, add to inventory.
            var weapon = _harness.Factory.CreateEntity("LongSword");
            Assert.IsTrue(inv.AddObject(weapon));

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", weapon, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(weapon.GetPart<EnhancementPaleSalt>(),
                "EnhancementPaleSalt attached.");
            Assert.AreEqual(2, weapon.GetPart<EnhancementPaleSalt>().Tier,
                "Tier 2 baked into the shim (matches mineral blueprint).");
            Assert.AreEqual(bitsBeforeB, bits.GetBitCount('B'),
                "Mineral recipes are FREE of bit cost — B unchanged.");
            Assert.AreEqual(bitsBeforeC, bits.GetBitCount('C'),
                "Mineral recipes are FREE of bit cost — C unchanged.");
            // Mineral should be consumed (no PaleSalt left in inventory).
            bool stillHasMineral = false;
            foreach (var item in inv.Objects)
                if (item.BlueprintName == "PaleSalt") stillHasMineral = true;
            Assert.IsFalse(stillHasMineral, "PaleSalt was consumed.");
        }

        [Test]
        public void PaleSalt_UpdatesDisplayName()
        {
            var player = CreatePlayerWithMineral("PaleSalt");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            var weapon = _harness.Factory.CreateEntity("LongSword");
            player.GetPart<InventoryPart>().AddObject(weapon);
            string originalName = weapon.GetPart<RenderPart>().DisplayName;

            TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", weapon, out _);

            string newName = weapon.GetPart<RenderPart>().DisplayName;
            Assert.AreNotEqual(originalName, newName);
            StringAssert.StartsWith("pale-salt-edged", newName);
        }

        // ── ChoirIron: full recipe path ───────────────────────────

        // ── E.5.1 deep-audit Bug #1 integration test ──────────────

        [Test]
        public void GlowQuartz_TinkerOnAlreadyEquippedItem_RadiusBumpsImmediately()
        {
            // E.5.1 deep-audit Bug #1: applying Glow-Quartz via Tinker
            // to an item the crafter is CURRENTLY wielding should fire
            // OnEquipped immediately, so the wielded weapon's light
            // radius bumps without requiring an unequip/re-equip cycle.
            //
            // TinkeringService sets MineralInfusionTinkerModification.CurrentCrafter
            // before calling the shim's Apply; the shim passes it as
            // `wielder` to ItemEnhancing.Apply; ItemEnhancing.Apply detects
            // the equipped state and fires OnEquipped on the new Part.
            var player = CreatePlayerWithMineral("GlowQuartz");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_glowquartz_infuse");
            var weapon = _harness.Factory.CreateEntity("LongSword");
            var inv = player.GetPart<InventoryPart>();
            inv.AddObject(weapon);

            // Mark the weapon as equipped on the crafter — emulates the
            // post-equip state EquipCommand would leave behind.
            inv.EquippedItems["MainHand"] = weapon;

            // Precondition: weapon has no LightSourcePart yet.
            Assert.IsNull(weapon.GetPart<LightSourcePart>(),
                "Pre-tinker: weapon has no LightSourcePart.");

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_glowquartz_infuse", weapon, out string reason);
            Assert.IsTrue(ok, reason);

            // Post-tinker: GlowQuartz attached AND OnEquipped fired
            // (so the LightSourcePart radius is +Tier=+2, not 0).
            var enh = weapon.GetPart<EnhancementGlowQuartz>();
            Assert.IsNotNull(enh);
            Assert.IsTrue(enh.AppliedBonus,
                "Bug #1 fix: OnEquipped fired on already-equipped item; " +
                "AppliedBonus flag set. If this fails, the player would " +
                "see no light-radius change until unequip+re-equip cycle.");
            Assert.AreEqual(2, weapon.GetPart<LightSourcePart>().Radius,
                "LightSourcePart created with +Tier=2 radius.");
        }

        [Test]
        public void GlowQuartz_TinkerOnUnequippedItem_RadiusStaysBaseline()
        {
            // Counter-check: applying to an inventory item the crafter
            // does NOT have equipped → OnEquipped does NOT fire (the
            // bonus lands later when the player equips the item).
            var player = CreatePlayerWithMineral("GlowQuartz");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_glowquartz_infuse");
            var weapon = _harness.Factory.CreateEntity("LongSword");
            player.GetPart<InventoryPart>().AddObject(weapon);
            // No EquippedItems entry.

            TinkeringService.TryApplyModification(
                player, "mod_glowquartz_infuse", weapon, out _);

            var enh = weapon.GetPart<EnhancementGlowQuartz>();
            Assert.IsNotNull(enh);
            Assert.IsFalse(enh.AppliedBonus,
                "Not-equipped: AppliedBonus stays false until EquipCommand fires.");
        }

        [Test]
        public void MineralRecipes_HaveZeroBitCost_ByDesign()
        {
            // Design-pin: post-user-feedback (E.5.1 follow-up) mineral
            // recipes are mineral-only. Cost field is intentionally
            // empty so the mineral itself IS the entire substrate cost.
            // If a future change re-introduces bits to these recipes,
            // this test breaks visibly and forces deliberate justification.
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_palesalt_infuse", out var pale));
            Assert.IsTrue(string.IsNullOrEmpty(pale.Cost),
                "Pale-Salt recipe: cost is empty by design (mineral-only).");

            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_choiriron_infuse", out var iron));
            Assert.IsTrue(string.IsNullOrEmpty(iron.Cost),
                "Choir-Iron recipe: cost is empty by design.");

            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(
                "mod_glowquartz_infuse", out var quartz));
            Assert.IsTrue(string.IsNullOrEmpty(quartz.Cost),
                "Glow-Quartz recipe: cost is empty by design.");
        }

        [Test]
        public void MineralRecipe_AppliesWithZeroBits_InLocker()
        {
            // Counter-check: a player with NO bits at all can still
            // apply a mineral recipe, because the recipe doesn't need
            // any. (Pre-fix this test would have failed at the
            // BitLocker.HasBits gate.)
            var player = new Entity { BlueprintName = "Player", ID = "p" };
            player.Tags["Player"] = "";
            player.AddPart(new InventoryPart());
            player.AddPart(new BitLockerPart());
            // Note: NO bits added.
            player.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            var mineral = _harness.Factory.CreateEntity("PaleSalt");
            player.GetPart<InventoryPart>().AddObject(mineral);
            var weapon = _harness.Factory.CreateEntity("LongSword");
            player.GetPart<InventoryPart>().AddObject(weapon);

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", weapon, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(weapon.GetPart<EnhancementPaleSalt>());
        }

        [Test]
        public void ChoirIron_RecipePath_AppliesEnhancement()
        {
            // Post-mineral-only-cost: no extra bits needed; the mineral
            // is the entire cost.
            var player = CreatePlayerWithMineral("ChoirIron");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_choiriron_infuse");
            var weapon = _harness.Factory.CreateEntity("LongSword");
            player.GetPart<InventoryPart>().AddObject(weapon);

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_choiriron_infuse", weapon, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(weapon.GetPart<EnhancementChoirIron>());
            Assert.AreEqual(3, weapon.GetPart<EnhancementChoirIron>().Tier,
                "Tier 3 baked into the ChoirIron shim.");
        }

        // ── GlowQuartz: full recipe path on a non-melee (armor) ───

        [Test]
        public void GlowQuartz_AppliesToArmor_ViaEquippableTargetPart()
        {
            var player = CreatePlayerWithMineral("GlowQuartz");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_glowquartz_infuse");
            var armor = _harness.Factory.CreateEntity("LeatherArmor");
            Assert.IsNotNull(armor, "LeatherArmor must be a registered blueprint.");
            player.GetPart<InventoryPart>().AddObject(armor);

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_glowquartz_infuse", armor, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(armor.GetPart<EnhancementGlowQuartz>(),
                "GlowQuartz works on armor (any Equippable item).");
        }

        // ── Counter-check: wrong target type → reject + no consumption ──

        [Test]
        public void PaleSalt_OnArmor_Rejects_AtRecipeTargetPartGate()
        {
            // Pale-Salt's recipe declares TargetPart=MeleeWeapon. Armor
            // lacks MeleeWeaponPart, so CanApplyModificationTarget fails
            // BEFORE the modification.Apply is reached. Bits + mineral
            // are NOT consumed.
            var player = CreatePlayerWithMineral("PaleSalt");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            var armor = _harness.Factory.CreateEntity("LeatherArmor");
            player.GetPart<InventoryPart>().AddObject(armor);

            int bitsBefore = player.GetPart<BitLockerPart>().GetBitCount('B');
            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", armor, out string reason);

            Assert.IsFalse(ok);
            Assert.IsNull(armor.GetPart<EnhancementPaleSalt>(),
                "Armor must not get Pale-Salt enhancement.");
            Assert.AreEqual(bitsBefore, player.GetPart<BitLockerPart>().GetBitCount('B'),
                "Bits not spent on rejected target.");
            // Mineral still in inventory.
            bool stillHasMineral = false;
            foreach (var item in player.GetPart<InventoryPart>().Objects)
                if (item.BlueprintName == "PaleSalt") stillHasMineral = true;
            Assert.IsTrue(stillHasMineral, "Mineral not consumed on rejected target.");
        }

        // ── Slot-cap respect ──────────────────────────────────────

        [Test]
        public void PaleSalt_OnAtCapWeapon_Rejects_AtCanApplyGate()
        {
            // Pre-load weapon with 2 enhancements (Lockdown #6 slot cap).
            // A 3rd Pale-Salt infusion must reject without consuming
            // the mineral or bits.
            var player = CreatePlayerWithMineral("PaleSalt");
            player.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            var weapon = _harness.Factory.CreateEntity("LongSword");
            player.GetPart<InventoryPart>().AddObject(weapon);

            // Apply two non-Pale-Salt enhancements first to fill the cap.
            // Use Serrated + Lacquered (both registered via auto-discovery).
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 1));
            // EnhancementLacquered requires ArmorPart so use Engraved instead.
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementEngraved), tier: 1));
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(weapon));

            int bitsBefore = player.GetPart<BitLockerPart>().GetBitCount('B');
            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", weapon, out string reason);

            Assert.IsFalse(ok);
            Assert.IsNull(weapon.GetPart<EnhancementPaleSalt>());
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(weapon),
                "Slot cap respected — count stays at 2.");
            Assert.AreEqual(bitsBefore, player.GetPart<BitLockerPart>().GetBitCount('B'),
                "Bits not spent.");
        }

        // ── Direct shim API (no recipe machinery) ─────────────────

        [TestCase(typeof(PaleSaltTinkerModification), "mod_palesalt", "Pale-Salt Infusion")]
        [TestCase(typeof(ChoirIronTinkerModification), "mod_choiriron", "Choir-Iron Infusion")]
        [TestCase(typeof(GlowQuartzTinkerModification), "mod_glowquartz", "Glow-Quartz Infusion")]
        public void Shim_HasExpectedIdAndDisplayName(
            System.Type shimType, string expectedId, string expectedDisplay)
        {
            var shim = (ITinkerModification)System.Activator.CreateInstance(shimType);
            Assert.AreEqual(expectedId, shim.Id);
            Assert.AreEqual(expectedDisplay, shim.DisplayName);
        }

        [TestCase("mod_palesalt")]
        [TestCase("mod_choiriron")]
        [TestCase("mod_glowquartz")]
        public void Registry_ResolvesShimById(string id)
        {
            Assert.IsTrue(TinkerModificationRegistry.TryCreate(id, out var mod));
            Assert.IsNotNull(mod);
        }

        [Test]
        public void PaleSaltShim_DirectApply_AddsEnhancement()
        {
            var weapon = _harness.Factory.CreateEntity("LongSword");
            var shim = new PaleSaltTinkerModification();

            Assert.IsTrue(shim.CanApply(weapon, out _));
            Assert.IsTrue(shim.Apply(weapon, out _));
            Assert.IsNotNull(weapon.GetPart<EnhancementPaleSalt>());
        }

        [Test]
        public void PaleSaltShim_DirectApplyOnArmor_RejectsAtCanApply()
        {
            // Counter-check pair to PaleSaltShim_DirectApply_AddsEnhancement.
            var armor = _harness.Factory.CreateEntity("LeatherArmor");
            var shim = new PaleSaltTinkerModification();

            Assert.IsFalse(shim.CanApply(armor, out string reason));
            StringAssert.Contains("compatible", reason);
            Assert.IsFalse(shim.Apply(armor, out _));
            Assert.IsNull(armor.GetPart<EnhancementPaleSalt>());
        }

        // ── EnhancementFactory auto-discovery ─────────────────────

        [Test]
        public void EnhancementFactory_AutoDiscovery_RegistersAllConcreteSubclasses()
        {
            // ForceReinitialize fired in SetUp; assert that auto-discovery
            // found at least the 6 known concrete IItemEnhancement classes
            // we expect (E.2 + E.3 content).
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementSerrated), out _));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementLacquered), out _));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementEngraved), out _));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementPaleSalt), out _));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementChoirIron), out _));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(EnhancementGlowQuartz), out _));
        }

        [Test]
        public void EnhancementFactory_DoesNotRegisterAbstractBase()
        {
            // EnhancementTagBonusBase is abstract — must NOT be in the
            // registry (would cause instantiation failures).
            Assert.IsFalse(EnhancementFactory.TryGet(nameof(EnhancementTagBonusBase), out _),
                "Abstract bases must not be registered by auto-discovery.");
        }
    }
}
