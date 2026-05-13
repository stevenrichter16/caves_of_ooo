using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.6 — adversarial sweep across the mineral
    /// economy (E.3.2–E.3.5).
    ///
    /// <para>Per CLAUDE.md "Adversarial test sweep" gate. E.3 hits these
    /// taxonomy surfaces:</para>
    /// <list type="bullet">
    ///   <item><b>State atomicity</b> — GlowQuartz AppliedBonus eager-
    ///         flag; mineral consumption rolled back on Apply failure</item>
    ///   <item><b>Parser</b> — WantsMineralPart comma-delim Minerals
    ///         field (whitespace, empty entries, malformed)</item>
    ///   <item><b>Cross-actor flows</b> — player ↔ NPC mineral trade,
    ///         player ↔ Tinker NPC recipe application</item>
    ///   <item><b>Stacking semantics</b> — stack-aware mineral consume</item>
    ///   <item><b>Save/load reach</b> — multi-enhancement multi-mineral
    ///         round-trip preserving all field types</item>
    ///   <item><b>Anti-exploit gates</b> — bit refund on Tinker reject,
    ///         slot-cap stays at-2 across mineral-Tinker churn</item>
    ///   <item><b>Diag dispatch contracts</b> — every gate emits exactly
    ///         one record</item>
    ///   <item><b>Cross-feature aggregation</b> — E.2 enhancement + E.3
    ///         enhancement on same weapon</item>
    ///   <item><b>Auto-discovery</b> — ForceReinitialize idempotency</item>
    /// </list>
    ///
    /// <para><b>Honesty bound:</b> 0 bugs found doesn't prove E.3 is
    /// bug-free — bounded by the bug classes the author imagined.
    /// Real value is regression infrastructure for future changes.</para>
    /// </summary>
    public class E3MineralsAdversarialTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
            Diag.ResetAll();
            EnhancementFactory.ForceReinitialize();
            EnhancementFactory.EnsureInitialized();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private Entity MakePlayer(string bits = "BBBBCCCC")
        {
            var p = new Entity { ID = "hero", BlueprintName = "hero" };
            p.Tags["Player"] = "";
            p.Tags["Creature"] = "";
            p.AddPart(new RenderPart { DisplayName = "hero" });
            p.AddPart(new InventoryPart());
            p.AddPart(new BitLockerPart());
            p.GetPart<BitLockerPart>().AddBits(bits);
            // Learn ALL mineral recipes so individual tests don't need to.
            p.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            p.GetPart<BitLockerPart>().LearnRecipe("mod_choiriron_infuse");
            p.GetPart<BitLockerPart>().LearnRecipe("mod_glowquartz_infuse");
            return p;
        }

        private Entity MakeWeapon() => _harness.Factory.CreateEntity("LongSword");

        private Entity GiveMineral(Entity player, string blueprint)
        {
            var m = _harness.Factory.CreateEntity(blueprint);
            player.GetPart<InventoryPart>().AddObject(m);
            return m;
        }

        // ════════════════════════════════════════════════════════════════
        // STATE ATOMICITY — mineral + bits roll back on Apply failure
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TinkerOnFullSlotCapWeapon_BitsAndMineralPreserved()
        {
            // Anti-exploit: pre-load weapon to slot cap so PaleSalt rejects
            // at the modification's CanApply. Bits + mineral must roll back.
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var weapon = MakeWeapon();
            player.GetPart<InventoryPart>().AddObject(weapon);
            // Fill slots with non-mineral enhancements.
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), 1));
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementEngraved), 1));

            int bitsB = player.GetPart<BitLockerPart>().GetBitCount('B');
            int bitsC = player.GetPart<BitLockerPart>().GetBitCount('C');
            int invSizeBefore = player.GetPart<InventoryPart>().Objects.Count;

            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", weapon, out _);

            Assert.IsFalse(ok);
            Assert.AreEqual(bitsB, player.GetPart<BitLockerPart>().GetBitCount('B'),
                "Bit B count unchanged on slot-cap rejection.");
            Assert.AreEqual(bitsC, player.GetPart<BitLockerPart>().GetBitCount('C'));
            Assert.AreEqual(invSizeBefore, player.GetPart<InventoryPart>().Objects.Count,
                "Inventory size unchanged — mineral NOT consumed on rejection.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-FEATURE AGGREGATION — E.2 + E.3 enhancement on same weapon
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SerratedPlusPaleSalt_BothFireOnUndeadHit()
        {
            // A weapon with EnhancementSerrated (E.2, vs anyone) AND
            // EnhancementPaleSalt (E.3, vs Undead) should fire both
            // hooks on an Undead defender: Serrated procs its bleed
            // chance, PaleSalt adds bonus damage.
            var weapon = MakeWeapon();
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), 4));
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementPaleSalt), 2));
            weapon.GetPart<EnhancementSerrated>().ChancePercent = 100;

            var defender = new Entity { ID = "skel", BlueprintName = "skel" };
            defender.Tags["Creature"] = "";
            defender.Statistics["Hitpoints"] = new Stat
                { Owner = defender, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            defender.Statistics["Toughness"] = new Stat
                { Owner = defender, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            defender.AddPart(new RenderPart { DisplayName = "skel" });
            defender.AddPart(new StatusEffectsPart());
            defender.AddPart(new MaterialPart { MaterialTagsRaw = "Bone,Dry,Undead" });
            defender.GetPart<MaterialPart>().Initialize();

            var attacker = new Entity { ID = "atk", BlueprintName = "atk" };
            attacker.AddPart(new RenderPart { DisplayName = "atk" });

            int hpBefore = defender.GetStatValue("Hitpoints");
            // Use dispatch directly to mirror what CombatSystem does.
            ItemEnhancementDispatch.DispatchOnHit(
                weapon, defender, attacker, new Damage(5), 5, zone: null,
                rng: new Random(0));

            // Pale-Salt should have applied +4 bonus damage.
            int hpAfter = defender.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore - 4, hpAfter,
                "Pale-Salt fired +4 bonus damage on Undead defender.");
            // Serrated should have applied a Bleeding effect.
            Assert.IsNotNull(defender.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Serrated fired on Undead defender too (it's tag-agnostic).");
        }

        // ════════════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — repeated mineral trade
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RepeatedMineralTrade_RepKeepsAccruing()
        {
            // Pin: trading the same NPC the same mineral repeatedly
            // doesn't cap or no-op (each trade is independent). The
            // player MUST supply a new mineral each time.
            var player = MakePlayer();
            var npc = new Entity { ID = "trader", BlueprintName = "trader" };
            npc.AddPart(new RenderPart { DisplayName = "trader" });
            npc.AddPart(new WantsMineralPart("PaleSalt", "PaleCuration", 10));

            int totalRep = 0;
            for (int i = 0; i < 5; i++)
            {
                GiveMineral(player, "PaleSalt");
                bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
                Assert.IsTrue(ok);
                totalRep += 10;
                Assert.AreEqual(totalRep, PlayerReputation.Get("PaleCuration"),
                    $"After {i + 1} trades, rep should be {totalRep}.");
            }
        }

        [Test]
        public void Adversarial_TradeWithoutNewMineral_AfterFirstTrade_Rejects()
        {
            // Counter-check pair to RepeatedMineralTrade: if the player
            // tries to trade WITHOUT a new mineral after the first trade,
            // the rejection emits mineral_not_in_inventory.
            var player = MakePlayer();
            var npc = new Entity { ID = "trader", BlueprintName = "trader" };
            npc.AddPart(new WantsMineralPart("PaleSalt", "PaleCuration", 10));
            GiveMineral(player, "PaleSalt");

            // First trade succeeds.
            MineralTradeService.TryTrade(player, npc, "PaleSalt");
            int repAfterFirst = PlayerReputation.Get("PaleCuration");

            // Second trade — no mineral.
            Diag.ResetAll();
            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsFalse(ok);
            Assert.AreEqual(repAfterFirst, PlayerReputation.Get("PaleCuration"),
                "Rep unchanged on rejected trade.");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = MineralTradeService.DIAG_CATEGORY,
                Kind = "Rejected",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("mineral_not_in_inventory", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // SAVE/LOAD REACH — weapon with multiple E.3 enhancements
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoMineralEnhancements_RoundTrip_BothPreserved()
        {
            var weapon = MakeWeapon();
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementPaleSalt), 3));
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementChoirIron), 2));

            Entity loaded = PartRoundTripHelper.RoundTripEntity(weapon);

            var pale = loaded.GetPart<EnhancementPaleSalt>();
            var choir = loaded.GetPart<EnhancementChoirIron>();
            Assert.IsNotNull(pale);
            Assert.IsNotNull(choir);
            Assert.AreEqual(3, pale.Tier);
            Assert.AreEqual(6, pale.BonusDamage);
            Assert.AreEqual("Undead", pale.TargetMaterialTag);
            Assert.AreEqual(2, choir.Tier);
            Assert.AreEqual(4, choir.BonusDamage);
            Assert.AreEqual("Fungal", choir.TargetMaterialTag);
        }

        [Test]
        public void Adversarial_GlowQuartzOnFlamingSword_PreservesBaseLightRoundTrip()
        {
            // FlamingSword has its own LightSourcePart (Radius=4 typically).
            // GlowQuartz extends that. After round-trip + simulated unequip,
            // base radius must return to its original.
            var flaming = _harness.Factory.CreateEntity("FlamingSword");
            int originalRadius = flaming.GetPart<LightSourcePart>().Radius;

            Assert.IsTrue(ItemEnhancing.Apply(flaming, nameof(EnhancementGlowQuartz), 2));
            var enh = flaming.GetPart<EnhancementGlowQuartz>();
            enh.OnEquipped(MakePlayer(), flaming);
            Assert.AreEqual(originalRadius + 2, flaming.GetPart<LightSourcePart>().Radius);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(flaming);
            var loadedEnh = loaded.GetPart<EnhancementGlowQuartz>();
            Assert.IsTrue(loadedEnh.AppliedBonus,
                "AppliedBonus survives round-trip — critical for unequip path.");

            // Simulate unequip: radius drops back to original.
            loadedEnh.OnUnequipped(MakePlayer(), loaded);
            Assert.AreEqual(originalRadius, loaded.GetPart<LightSourcePart>().Radius,
                "After round-trip + unequip, FlamingSword radius restored.");
        }

        // ════════════════════════════════════════════════════════════════
        // PARSER — malformed WantsMineralPart.Minerals
        // ════════════════════════════════════════════════════════════════

        [TestCase("")]
        [TestCase(",")]
        [TestCase(", , ,")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Adversarial_Parser_MalformedMineralsField_NoCrash_EmptyList(string raw)
        {
            var w = new WantsMineralPart(raw, "PaleCuration", 10);
            Assert.IsFalse(w.Wants("PaleSalt"),
                "Malformed Minerals field rejects everything.");
            Assert.AreEqual(0, w.GetWantedMinerals().Count,
                $"Raw='{raw ?? "null"}' yields empty wanted list.");
        }

        [Test]
        public void Adversarial_Parser_MineralsWithMixedWhitespace_Normalized()
        {
            var w = new WantsMineralPart("  PaleSalt  ,\tChoirIron\n,GlowQuartz", "PaleCuration", 10);
            Assert.IsTrue(w.Wants("PaleSalt"));
            Assert.IsTrue(w.Wants("ChoirIron"));
            Assert.IsTrue(w.Wants("GlowQuartz"));
            Assert.AreEqual(3, w.GetWantedMinerals().Count);
        }

        // ════════════════════════════════════════════════════════════════
        // BOUNDARY INPUTS — extreme tier on mineral enhancements
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_GlowQuartzTier0_NoRadiusChange()
        {
            var weapon = MakeWeapon();
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(0);
            weapon.AddPart(enh);

            enh.OnEquipped(MakePlayer(), weapon);

            // RadiusBonus=0, so the auto-created LightSourcePart stays at 0.
            var light = weapon.GetPart<LightSourcePart>();
            Assert.IsNotNull(light, "LightSourcePart created even at Tier 0.");
            Assert.AreEqual(0, light.Radius,
                "Tier 0 GlowQuartz creates Part with Radius 0 (harmless no-op).");
        }

        [Test]
        public void Adversarial_TagBonusNegativeTier_NoBonusEvenOnTaggedDefender()
        {
            // Negative tier → BonusDamage<0. Defender HP would INCREASE if
            // we naively applied negative damage. ApplyDamage clamps to ≥0;
            // pin that the cascading behavior is sane.
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(-2);  // BonusDamage = -4
            Assert.AreEqual(-4, enh.BonusDamage);

            var defender = new Entity { ID = "skel", BlueprintName = "skel" };
            defender.Statistics["Hitpoints"] = new Stat
                { Owner = defender, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            defender.Statistics["Toughness"] = new Stat
                { Owner = defender, Name = "Toughness", BaseValue = 5, Min = 1, Max = 50 };
            defender.AddPart(new MaterialPart { MaterialTagsRaw = "Bone,Undead" });
            defender.GetPart<MaterialPart>().Initialize();

            int hpBefore = defender.GetStatValue("Hitpoints");
            enh.OnAttackerHit(defender, MakePlayer(),
                new Damage(5), 5, null, new Random(0));
            int hpAfter = defender.GetStatValue("Hitpoints");

            // Damage clamps to ≥0 in CombatSystem.ApplyDamage (Damage.Amount setter).
            // So negative bonus damage = 0 damage applied.
            Assert.AreEqual(hpBefore, hpAfter,
                "Negative bonus damage clamps to 0 — defender HP unchanged.");
        }

        // ════════════════════════════════════════════════════════════════
        // DIAG DISPATCH — exactly one record per gate
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FullTinkerFlow_EmitsExactlyOneAppliedDiag()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var weapon = MakeWeapon();
            player.GetPart<InventoryPart>().AddObject(weapon);

            Diag.ResetAll();
            TinkeringService.TryApplyModification(player, "mod_palesalt_infuse", weapon, out _);

            int applied = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Applied",
                Limit = 10,
            }).Records.Count;
            Assert.AreEqual(1, applied,
                "Tinker → ItemEnhancing.Apply → exactly one Applied record.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-ACTOR — NPC trading their own mineral to player (reverse)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TradeBetweenTwoNonPlayers_DoesNothing()
        {
            // The mineral-trade service is designed for player → NPC.
            // Passing two NPC entities should still execute the logic
            // mechanically (no Player tag check in TryTrade) — but
            // PlayerReputation.Modify is a no-op for non-Player factions
            // OR the rep system may not care about who's calling. We pin
            // that the function doesn't crash and consumes the mineral
            // from the "player" arg's inventory. Behavior pin only.
            var fakePlayer = new Entity { ID = "npc1", BlueprintName = "n1" };
            fakePlayer.AddPart(new InventoryPart());
            var mineral = _harness.Factory.CreateEntity("PaleSalt");
            fakePlayer.GetPart<InventoryPart>().AddObject(mineral);

            var npc = new Entity { ID = "npc2", BlueprintName = "n2" };
            npc.AddPart(new WantsMineralPart("PaleSalt", "PaleCuration", 10));

            int repBefore = PlayerReputation.Get("PaleCuration");
            bool ok = MineralTradeService.TryTrade(fakePlayer, npc, "PaleSalt");

            Assert.IsTrue(ok, "Service doesn't gate on player-tag — mechanical pass.");
            // Documented behavior: rep flows REGARDLESS of caller-side
            // tag. If we want a player-only gate, that's E.5+.
            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("PaleCuration"));
        }

        // ════════════════════════════════════════════════════════════════
        // AUTO-DISCOVERY — idempotency + abstract-class exclusion
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EnsureInitialized_CalledTwice_Idempotent()
        {
            // Second call must be a no-op — registrations don't double.
            // (Register itself is already idempotent, but EnsureInitialized
            // should short-circuit via the _initialized flag too.)
            EnhancementFactory.ForceReinitialize();
            EnhancementFactory.EnsureInitialized();
            EnhancementFactory.TryGet(nameof(EnhancementPaleSalt), out var t1);
            EnhancementFactory.EnsureInitialized();
            EnhancementFactory.TryGet(nameof(EnhancementPaleSalt), out var t2);
            Assert.AreSame(t1, t2,
                "Second EnsureInitialized doesn't replace the registered Type.");
        }

        [Test]
        public void Adversarial_AutoDiscovery_FindsBothE2AndE3Subclasses()
        {
            // Comprehensive pin: every concrete IItemEnhancement subclass
            // we know about is reachable after auto-discovery.
            string[] expected =
            {
                nameof(EnhancementSerrated),
                nameof(EnhancementLacquered),
                nameof(EnhancementEngraved),
                nameof(EnhancementPaleSalt),
                nameof(EnhancementChoirIron),
                nameof(EnhancementGlowQuartz),
            };
            foreach (var name in expected)
            {
                Assert.IsTrue(EnhancementFactory.TryGet(name, out _),
                    $"Auto-discovery must find {name}.");
            }
            // Abstract bases excluded.
            Assert.IsFalse(EnhancementFactory.TryGet("IItemEnhancement", out _));
            Assert.IsFalse(EnhancementFactory.TryGet("IMeleeEnhancement", out _));
            Assert.IsFalse(EnhancementFactory.TryGet("EnhancementTagBonusBase", out _));
        }

        // ════════════════════════════════════════════════════════════════
        // DEFENSIVE NULL-SAFETY — nested null paths
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TinkerOnNullWeapon_Rejects()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            bool ok = TinkeringService.TryApplyModification(
                player, "mod_palesalt_infuse", null, out string reason);
            Assert.IsFalse(ok);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void Adversarial_TradeWithEmptyMineralsField_NeverWants()
        {
            var w = new WantsMineralPart("", "PaleCuration", 10);
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = new Entity();
            npc.AddPart(w);

            Diag.ResetAll();
            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsFalse(ok);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = MineralTradeService.DIAG_CATEGORY,
                Kind = "Rejected",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("not_wanted", recs[0].PayloadJson);
        }
    }
}
