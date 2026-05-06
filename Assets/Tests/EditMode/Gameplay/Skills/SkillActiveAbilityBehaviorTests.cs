using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP4.0 — Per-active-ability behavior tests + recursion-guard pins.
    /// Complements <see cref="SkillActivatedAbilityTests"/> (which pins
    /// the dispatcher contract via stub skills) with end-to-end
    /// behavior tests on the actual shipped active-ability skills:
    /// <see cref="Cudgel_Conk"/> and <see cref="Axe_Berserk"/>.
    ///
    /// <para>Also pins the recursion-guard invariants on
    /// <see cref="Cudgel_Backswing"/> and <see cref="ShortBlades_Rejoinder"/>
    /// — the WSP3.4 commit body claims a Backswing-triggered swing that
    /// itself misses won't re-trigger Backswing; this test fixture proves
    /// it.</para>
    /// </summary>
    public class SkillActiveAbilityBehaviorTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror SkillSystemTier2Tests') ──────────────

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int agility = 16, int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeaponEntity(string name, string dice,
            string attributes, int penBonus = 0)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = dice, PenBonus = penBonus,
                Attributes = attributes,
            });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void EquipInPrimary(Entity actor, Entity weaponEntity)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
        }

        // ════════════════════════════════════════════════════════════════
        // Cudgel_Conk
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Conk_WithCudgelEquippedAndAdjacentTarget_AppliesStunned()
        {
            // Acceptance: actor with Cudgel_Conk owned + Mace equipped +
            // adjacent Creature → OnCommand should apply Stunned to that
            // target (forced, regardless of swing outcome — that's the
            // targeted-strike effect).
            var actor = MakeBodiedCreature("attacker");
            EquipInPrimary(actor, MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel", penBonus: 3));
            var conk = new Cudgel_Conk();
            actor.GetPart<SkillsPart>().AddSkill(conk, source: "test");

            var defender = MakeBodiedCreature("defender", agility: 16);
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(defender, 6, 5);

            var ctx = new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(0),
            };
            conk.OnCommand(ctx);

            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Conk should apply StunnedEffect to the first adjacent Creature.");
        }

        [Test]
        public void Conk_WithoutCudgelWeapon_FailsWithMessage()
        {
            // Guard: actor without a Cudgel weapon equipped → Conk
            // should fail with a "needs cudgel" message and NOT apply
            // Stunned to anyone.
            var actor = MakeBodiedCreature("attacker");
            // Equip a LongSword (Cutting LongBlades — NOT Cudgel).
            EquipInPrimary(actor, MakeWeaponEntity("sword", "1d8", "Cutting LongBlades"));
            var conk = new Cudgel_Conk();
            actor.GetPart<SkillsPart>().AddSkill(conk, source: "test");

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(defender, 6, 5);

            conk.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(0),
            });

            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Without a Cudgel weapon, Conk must not apply Stunned.");
            // Sanity: a fail-message went to the log.
            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("cudgel")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'needs cudgel-class weapon' message in the log.");
        }

        [Test]
        public void Conk_WithNoAdjacentTarget_NoOps_NoStun()
        {
            // Guard: actor has Cudgel + Conk but no adjacent target →
            // OnCommand should log "swings at nothing" and not crash.
            var actor = MakeBodiedCreature("attacker");
            EquipInPrimary(actor, MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var conk = new Cudgel_Conk();
            actor.GetPart<SkillsPart>().AddSkill(conk, source: "test");

            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            // No defender placed.

            Assert.DoesNotThrow(() =>
                conk.OnCommand(new SkillEventContext
                {
                    Attacker = actor, Defender = actor,
                    Zone = zone, Rng = new Random(0),
                }));
        }

        // ════════════════════════════════════════════════════════════════
        // Axe_Berserk
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Berserk_WithAxeEquipped_AppliesBerserkEffectToSelf()
        {
            var actor = MakeBodiedCreature("attacker");
            EquipInPrimary(actor, MakeWeaponEntity("battleaxe", "2d6", "Cutting Axe", penBonus: 3));
            var berserk = new Axe_Berserk();
            actor.GetPart<SkillsPart>().AddSkill(berserk, source: "test");

            int strBefore = actor.GetStatValue("Strength");
            berserk.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = null, Rng = new Random(0),
            });

            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<BerserkEffect>(),
                "Berserk should apply BerserkEffect to self.");
            Assert.AreEqual(strBefore + BerserkEffect.STR_BONUS, actor.GetStatValue("Strength"),
                "Berserk's Strength bonus should be observable via the stat path.");
        }

        [Test]
        public void Berserk_WithoutAxe_FailsWithMessage_NoEffect()
        {
            var actor = MakeBodiedCreature("attacker");
            // No Axe equipped — equip a Mace instead (Cudgel attribute).
            EquipInPrimary(actor, MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var berserk = new Axe_Berserk();
            actor.GetPart<SkillsPart>().AddSkill(berserk, source: "test");

            berserk.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = null, Rng = new Random(0),
            });

            Assert.IsFalse(actor.GetPart<StatusEffectsPart>().HasEffect<BerserkEffect>(),
                "Without an Axe equipped, Berserk must not apply BerserkEffect.");
            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("axe")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'needs an axe equipped' message in the log.");
        }

        // ════════════════════════════════════════════════════════════════
        // Backswing recursion guard
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Backswing_OfABackswing_DoesNotInfiniteRecurse()
        {
            // Acceptance gate (WSP3.4 commit body claim):
            // a Backswing-triggered re-attack that itself misses must
            // NOT trigger another Backswing — the instance-level
            // _recurring guard short-circuits the second call.
            //
            // Test shape: very high DV defender so every swing misses.
            // If the guard breaks, we get 2+ "(Backswing)" markers per
            // PerformSingleAttack call (or worse, infinite recursion).
            // Asserting "≤ 1 Backswing per outer call" is the invariant.
            var attacker = MakeBodiedCreature("attacker", strength: 16, agility: 5);
            EquipInPrimary(attacker, MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Backswing(), source: "test");

            var defender = MakeBodiedCreature("defender", agility: 16);
            defender.GetPart<ArmorPart>().DV = 30;  // crank DV for guaranteed misses

            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int maxBackswingsInOneCall = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(
                    attacker, defender,
                    attacker.GetPart<Body>().GetParts().Find(p => p.Type == "Hand")
                        .Equipped.GetPart<MeleeWeaponPart>(),
                    isPrimary: true,
                    zone, new Random(seed),
                    attackSourceDesc: null);
                int count = 0;
                foreach (var msg in MessageLog.GetRecent(20))
                    if (msg.Contains("(Backswing)")) count++;
                if (count > maxBackswingsInOneCall) maxBackswingsInOneCall = count;
            }
            Assert.LessOrEqual(maxBackswingsInOneCall, 1,
                "Backswing recursion guard broken: a Backswing-of-a-Backswing fired. " +
                $"Max '(Backswing)' markers in one PerformSingleAttack call: {maxBackswingsInOneCall}. " +
                $"Expected ≤ 1.");
        }

        // ════════════════════════════════════════════════════════════════
        // Rejoinder recursion guard
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Rejoinder_OfARejoinder_DoesNotInfiniteRecurse()
        {
            // Acceptance gate (WSP3.4 commit body claim):
            // Rejoinder fires from defender's OnDefenderAfterAttackMissed.
            // The counter-attack (defender swings at attacker) ALSO goes
            // through PerformSingleAttack — if it misses, the original
            // attacker's defender-side Rejoinder (if any) could fire,
            // creating a ping-pong loop. The instance _recurring flag
            // breaks this.
            //
            // Test shape: BOTH player and NPC have Rejoinder + Piercing
            // weapons. NPC swings at player (misses → Rejoinder fires
            // → player swings at NPC → NPC's Rejoinder could fire if
            // misses again). Verify ≤ 1 Rejoinder marker per outer
            // PerformSingleAttack call.
            var player = MakeBodiedCreature("player", agility: 16);
            EquipInPrimary(player, MakeWeaponEntity("dagger", "1d4", "Piercing"));
            player.GetPart<SkillsPart>().AddSkill(new ShortBlades_Rejoinder(), source: "test");
            player.GetPart<ArmorPart>().DV = 30;

            var npc = MakeBodiedCreature("npc", agility: 16);
            EquipInPrimary(npc, MakeWeaponEntity("npc_dagger", "1d4", "Piercing"));
            npc.GetPart<SkillsPart>().AddSkill(new ShortBlades_Rejoinder(), source: "test");
            npc.GetPart<ArmorPart>().DV = 30;

            var zone = new Zone();
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);

            int maxRejoindersInOneCall = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(
                    attacker: npc, defender: player,
                    weapon: npc.GetPart<Body>().GetParts().Find(p => p.Type == "Hand")
                        .Equipped.GetPart<MeleeWeaponPart>(),
                    isPrimary: true,
                    zone: zone, rng: new Random(seed),
                    attackSourceDesc: null);
                int count = 0;
                foreach (var msg in MessageLog.GetRecent(20))
                    if (msg.Contains("(Rejoinder)")) count++;
                if (count > maxRejoindersInOneCall) maxRejoindersInOneCall = count;
            }
            Assert.LessOrEqual(maxRejoindersInOneCall, 1,
                "Rejoinder recursion guard broken: a Rejoinder-of-a-Rejoinder fired. " +
                $"Max '(Rejoinder)' markers in one PerformSingleAttack call: {maxRejoindersInOneCall}. " +
                $"Expected ≤ 1.");
        }
    }
}
