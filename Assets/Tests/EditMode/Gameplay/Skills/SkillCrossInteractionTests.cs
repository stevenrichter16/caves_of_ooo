using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP4.2 — Cross-skill interaction tests. Pin invariants that
    /// hold across multiple skills firing on the same hit, or across
    /// effects from different trees stacking on the same target.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    /// <item><b>Hammer multi-equipment randomness</b> — defender with
    ///       multiple equipped items; across seeds, more than one gets
    ///       Broken (proves the pick is randomized per <c>Random.Next</c>,
    ///       not always picking index 0).</item>
    /// <item><b>Hobbled + Berserk DV stacking</b> — both apply via
    ///       <c>Stat.Penalty</c> on the DV stat; should sum cleanly.</item>
    /// <item><b>Multiple Cudgel skills on same hit</b> — Cudgel_Bludgeon +
    ///       Cudgel_Hammer + Cudgel_ShatteringBlows all gate on Cudgel
    ///       and roll independently; across seeds, observe all 3 fire
    ///       at least once.</item>
    /// </list></para>
    /// </summary>
    public class SkillCrossInteractionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeBodiedCreature(string name)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeItem(string name, string slot = "Hand")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            e.AddPart(new EquippablePart { Slot = slot });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Damage MakeDamage(params string[] attrs)
        {
            var d = new Damage(10);
            foreach (var a in attrs) d.AddAttribute(a);
            return d;
        }

        private static Entity MakeAttacker(params BaseSkillPart[] skills)
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            foreach (var s in skills)
                Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(s, source: "test"));
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Hammer: multi-equipment randomness
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Hammer_WithMultipleEquippedItems_PicksDifferentItems_AcrossSeeds()
        {
            // Acceptance: across many seeds, Hammer's random pick
            // should select more than one of the equipped items —
            // proves the rng-driven selection isn't always index 0.
            //
            // Setup: defender with 2 items equipped (one in each hand
            // for humanoid Body).
            var skill = new Cudgel_Hammer();
            var attacker = MakeAttacker(skill);

            var defender = MakeBodiedCreature("defender");
            var hands = defender.GetPart<Body>().GetParts().FindAll(p => p.Type == "Hand");
            Assert.GreaterOrEqual(hands.Count, 2,
                "Setup needs at least 2 Hand body parts on humanoid.");

            var item1 = MakeItem("item_alpha");
            var item2 = MakeItem("item_beta");
            defender.GetPart<InventoryPart>().EquipToBodyPart(item1, hands[0]);
            defender.GetPart<InventoryPart>().EquipToBodyPart(item2, hands[1]);

            var observed = new HashSet<string>();
            // Hammer is 2% chance per hit; need many seeds. Each successful
            // proc applies Broken to ONE of the two items (randomly chosen).
            // After Broken applied, ApplyEffect is a no-op on the same item
            // (BrokenEffect.OnStack returns true), so we'd observe the same
            // item being "picked" again without a fresh test fixture per seed.
            // Reset the items between seeds.
            for (int seed = 0; seed < 10000; seed++)
            {
                // Strip Broken from any prior seed's hit so we observe
                // each pick independently.
                if (item1.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>())
                    item1.GetPart<StatusEffectsPart>().RemoveEffect(
                        item1.GetPart<StatusEffectsPart>().GetEffect<BrokenEffect>());
                if (item2.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>())
                    item2.GetPart<StatusEffectsPart>().RemoveEffect(
                        item2.GetPart<StatusEffectsPart>().GetEffect<BrokenEffect>());

                skill.OnAttackerAfterAttack(new SkillEventContext
                {
                    Attacker = attacker, Defender = defender,
                    Damage = MakeDamage("Cudgel"), ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                });

                if (item1.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>())
                    observed.Add("item_alpha");
                if (item2.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>())
                    observed.Add("item_beta");
                if (observed.Count == 2) break;
            }
            Assert.AreEqual(2, observed.Count,
                $"Across 10000 seeds, Hammer should pick BOTH equipped items at " +
                $"least once each (proves random pick, not deterministic index 0). " +
                $"Observed picks: [{string.Join(", ", observed)}].");
        }

        // ════════════════════════════════════════════════════════════════
        // Hobbled + Berserk: DV-penalty stacking (cross-tree)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HobbledAndBerserk_OnSameTarget_DVPenaltiesSum()
        {
            // Hobbled adds DV_PENALTY (3) via stat.Penalty.
            // Berserk adds DV_PENALTY (2) via stat.Penalty.
            // Both touch the same DV stat field, so they should
            // naturally sum to -5 total penalty.
            var target = MakeBodiedCreature("target");
            int dvBefore = target.GetStatValue("DV");

            target.ApplyEffect(new HobbledEffect(8), source: null, zone: null);
            target.ApplyEffect(new BerserkEffect(5), source: null, zone: null);

            int expectedAfter = dvBefore
                                - HobbledEffect.DV_PENALTY
                                - BerserkEffect.DV_PENALTY;
            Assert.AreEqual(expectedAfter, target.GetStatValue("DV"),
                "Hobbled + Berserk DV penalties should sum cleanly via Stat.Penalty.");
        }

        [Test]
        public void HobbledAndBerserk_BothRemoved_DVRestored()
        {
            // Counter-check: after both effects removed, DV should
            // return to baseline (no leakage in either OnRemove).
            var target = MakeBodiedCreature("target");
            int dvBefore = target.GetStatValue("DV");

            var hobbled = new HobbledEffect(8);
            var berserk = new BerserkEffect(5);
            target.ApplyEffect(hobbled, source: null, zone: null);
            target.ApplyEffect(berserk, source: null, zone: null);
            target.GetPart<StatusEffectsPart>().RemoveEffect(hobbled);
            target.GetPart<StatusEffectsPart>().RemoveEffect(berserk);

            Assert.AreEqual(dvBefore, target.GetStatValue("DV"),
                "After both Hobbled + Berserk are removed, DV should be back to baseline. " +
                "If not, OnRemove on one of the effects is leaking penalty (or doubling).");
        }

        // ════════════════════════════════════════════════════════════════
        // Multiple Cudgel skills on the same hit — independent rolls
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MultipleCudgelSkillsOnSameHit_AllRollIndependently()
        {
            // An actor owning Cudgel_Bludgeon + Cudgel_Hammer +
            // Cudgel_ShatteringBlows runs THREE independent rolls on
            // every Cudgel-attribute hit (50% / 2% / 10% respectively).
            // Across many seeds, all three effects should be observable
            // at least once — proves the dispatcher routes to all
            // matching skills, not just the first.
            var bludgeon = new Cudgel_Bludgeon();
            var hammer = new Cudgel_Hammer();
            var shattering = new Cudgel_ShatteringBlows();
            var attacker = MakeAttacker(bludgeon, hammer, shattering);

            // Defender needs Body+Inventory for Hammer to find an
            // equipped item; needs StatusEffectsPart for Stunned/Shatter.
            var defender = MakeBodiedCreature("defender");
            var hand = defender.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            var item = MakeItem("equipped_blade");
            defender.GetPart<InventoryPart>().EquipToBodyPart(item, hand);

            bool sawStun = false, sawBroken = false, sawShatter = false;
            for (int seed = 0; seed < 1000 && !(sawStun && sawBroken && sawShatter); seed++)
            {
                // Reset effects between seeds so we observe each fresh.
                var defStatus = defender.GetPart<StatusEffectsPart>();
                if (defStatus.HasEffect<StunnedEffect>())
                    defStatus.RemoveEffect(defStatus.GetEffect<StunnedEffect>());
                if (defStatus.HasEffect<ShatterArmorEffect>())
                    defStatus.RemoveEffect(defStatus.GetEffect<ShatterArmorEffect>());
                var itemStatus = item.GetPart<StatusEffectsPart>();
                if (itemStatus.HasEffect<BrokenEffect>())
                    itemStatus.RemoveEffect(itemStatus.GetEffect<BrokenEffect>());

                var ctx = new SkillEventContext
                {
                    Attacker = attacker, Defender = defender,
                    Damage = MakeDamage("Cudgel"), ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                };
                // Dispatch via the actual SkillEventDispatcher to exercise
                // the multi-skill iteration path.
                SkillEventDispatcher.AttackerAfterAttack(attacker, ctx);

                if (defStatus.HasEffect<StunnedEffect>()) sawStun = true;
                if (defStatus.HasEffect<ShatterArmorEffect>()) sawShatter = true;
                if (itemStatus.HasEffect<BrokenEffect>()) sawBroken = true;
            }
            Assert.IsTrue(sawStun,
                "Across 1000 seeds, Cudgel_Bludgeon (50%) should fire at least once.");
            Assert.IsTrue(sawShatter,
                "Across 1000 seeds, Cudgel_ShatteringBlows (10%) should fire at least once.");
            Assert.IsTrue(sawBroken,
                "Across 1000 seeds, Cudgel_Hammer (2%) should fire at least once. " +
                "If false, the dispatcher isn't routing to all 3 skills, OR Hammer's " +
                "rng path is broken when other skills consume rng first.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: dispatcher routes only to matching-class skills
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatcherDoesNotFire_OnMismatchedDamageClass()
        {
            // Owner has Cudgel skills + LongBlades skills. A Cutting+LongBlades
            // hit should fire the LongBlades branches but NOT the Cudgel ones.
            // Pins per-skill class-attribute gating is independent.
            var cudgel = new Cudgel_Bludgeon();
            var lacerate = new LongBlades_Lacerate();
            var attacker = MakeAttacker(cudgel, lacerate);

            var defender = MakeBodiedCreature("defender");

            for (int seed = 0; seed < 200; seed++)
            {
                var defStatus = defender.GetPart<StatusEffectsPart>();
                if (defStatus.HasEffect<StunnedEffect>())
                    defStatus.RemoveEffect(defStatus.GetEffect<StunnedEffect>());

                SkillEventDispatcher.AttackerAfterAttack(attacker, new SkillEventContext
                {
                    Attacker = attacker, Defender = defender,
                    // Cutting+LongBlades hit (e.g. LongSword swing).
                    Damage = MakeDamage("Cutting", "LongBlades"),
                    ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                });

                Assert.IsFalse(defStatus.HasEffect<StunnedEffect>(),
                    $"Seed {seed}: Cutting/LongBlades hit must not trigger Cudgel_Bludgeon's " +
                    $"Stun gate (which requires the 'Cudgel' attribute on damage). " +
                    $"If this fires, per-skill class gating is leaking.");
            }
        }
    }
}
