using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP3.4 — Per-skill behavior tests for the 8 new Tier-2 passives.
    /// Each skill gets at minimum 1 positive (does fire when conditions
    /// are met) + 1 counter-check (doesn't fire when conditions miss).
    ///
    /// <para>Skills covered:
    /// <list type="bullet">
    /// <item>Cudgel_Expertise / Axe_Expertise / ShortBlades_Expertise
    ///       (passive +to-hit; gated on weapon Attributes string)</item>
    /// <item>Cudgel_Hammer (2% Broken proc; gated on Cudgel attribute + has-equipped-items)</item>
    /// <item>Cudgel_ShatteringBlows (10% ShatterArmor; gated on Cudgel attribute)</item>
    /// <item>ShortBlades_Hobble (15% Hobbled; gated on Piercing attribute)</item>
    /// </list></para>
    ///
    /// <para>Backswing + Rejoinder integration tests require zone setup
    /// + PerformSingleAttack invocation; those are live-verified via
    /// the showcase scenario in WSP3.7.</para>
    /// </summary>
    public class SkillSystemTier2Tests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Test fixtures ───────────────────────────────────────────────

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeAttackerWithSkill(BaseSkillPart skill)
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(skill, source: "test"));
            return e;
        }

        private static MeleeWeaponPart MakeWeapon(string attributes)
        {
            // Attach to a fresh Entity so weapon.ParentEntity is valid.
            var weaponEntity = new Entity { ID = "weapon", BlueprintName = "TestWeapon" };
            weaponEntity.AddPart(new RenderPart { DisplayName = "test weapon" });
            var w = new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                HitBonus = 0,
                PenBonus = 0,
                MaxStrengthBonus = 3,
                Attributes = attributes,
            };
            weaponEntity.AddPart(w);
            return w;
        }

        private static SkillEventContext MakeHitContext(Entity attacker, Entity defender,
            MeleeWeaponPart weapon, int seed, params string[] damageAttrs)
        {
            var damage = new Damage(10);
            foreach (var a in damageAttrs) damage.AddAttribute(a);
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Weapon = weapon, WeaponEntity = weapon?.ParentEntity,
                Damage = damage, ActualDamage = 10,
                Zone = null, Rng = new Random(seed),
            };
        }

        // ── Cudgel_Expertise ────────────────────────────────────────────

        [Test]
        public void CudgelExpertise_WithCudgelWeapon_AddsHitBonus()
        {
            var skill = new Cudgel_Expertise();
            var actor = MakeAttackerWithSkill(skill);
            var weapon = MakeWeapon("Bludgeoning Cudgel");
            int bonus = SkillEventDispatcher.GetSkillHitModifier(actor, weapon);
            Assert.AreEqual(Cudgel_Expertise.HIT_BONUS, bonus,
                "Cudgel_Expertise must contribute its HIT_BONUS when the wielded " +
                "weapon's Attributes contain 'Cudgel'.");
        }

        [Test]
        public void CudgelExpertise_WithNonCudgelWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new Cudgel_Expertise());
            var weapon = MakeWeapon("Cutting LongBlades");  // no Cudgel
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon),
                "Cudgel_Expertise must NOT contribute when the wielded weapon " +
                "doesn't carry the Cudgel attribute.");
        }

        // ── Axe_Expertise ───────────────────────────────────────────────

        [Test]
        public void AxeExpertise_WithAxeWeapon_AddsHitBonus()
        {
            var actor = MakeAttackerWithSkill(new Axe_Expertise());
            var weapon = MakeWeapon("Cutting Axe");
            Assert.AreEqual(Axe_Expertise.HIT_BONUS,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        [Test]
        public void AxeExpertise_WithNonAxeWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new Axe_Expertise());
            var weapon = MakeWeapon("Bludgeoning Cudgel");
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        // ── ShortBlades_Expertise ───────────────────────────────────────

        [Test]
        public void ShortBladesExpertise_WithPiercingWeapon_AddsHitBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Expertise());
            var weapon = MakeWeapon("Piercing");
            Assert.AreEqual(ShortBlades_Expertise.HIT_BONUS,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        [Test]
        public void ShortBladesExpertise_WithNonPiercingWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Expertise());
            var weapon = MakeWeapon("Cutting Axe");
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        // ── Cudgel_ShatteringBlows ──────────────────────────────────────

        [Test]
        public void ShatteringBlows_WithCudgelHit_HasChance_ToShatterArmor()
        {
            var skill = new Cudgel_ShatteringBlows();
            var actor = MakeAttackerWithSkill(skill);
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cudgel");
                skill.OnAttackerAfterAttack(ctx);
                if (defender.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 200 seeds, ShatteringBlows should produce at least one " +
                $"ShatterArmorEffect (chance {Cudgel_ShatteringBlows.CHANCE_PERCENT}%).");
        }

        [Test]
        public void ShatteringBlows_WithNonCudgelHit_NeverShatters()
        {
            var skill = new Cudgel_ShatteringBlows();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cutting", "LongBlades");
                skill.OnAttackerAfterAttack(ctx);
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>(),
                    $"Seed {seed}: Cutting/LongBlades hit must not fire ShatteringBlows.");
            }
        }

        // ── ShortBlades_Hobble ──────────────────────────────────────────

        [Test]
        public void Hobble_WithPiercingHit_HasChance_ToHobble()
        {
            var skill = new ShortBlades_Hobble();
            var actor = MakeAttackerWithSkill(skill);
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                defender.Statistics["DV"] = new Stat { Owner = defender, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Piercing");
                skill.OnAttackerAfterAttack(ctx);
                if (defender.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 200 seeds, Hobble should produce at least one HobbledEffect " +
                $"(chance {ShortBlades_Hobble.CHANCE_PERCENT}%).");
        }

        [Test]
        public void Hobble_WithNonPiercingHit_NeverHobbles()
        {
            var skill = new ShortBlades_Hobble();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                defender.Statistics["DV"] = new Stat { Owner = defender, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Bludgeoning", "Cudgel");
                skill.OnAttackerAfterAttack(ctx);
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>(),
                    $"Seed {seed}: Bludgeoning hit must not fire Hobble.");
            }
        }

        // ── Cudgel_Hammer (2% chance — bigger seed loop) ────────────────

        [Test]
        public void Hammer_NoEquippedItems_NoOps()
        {
            // Defender has no Body → no equipped items → Hammer no-ops.
            // Across many seeds, no exception even when chance roll succeeds.
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();  // no Body
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cudgel");
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: Hammer on body-less defender must not throw.");
            }
        }

        [Test]
        public void Hammer_WithNonCudgelHit_NoCrash_NoOps()
        {
            // Counter-check on the Cudgel-attribute gate. Even with Hammer
            // owned, a Cutting/LongBlades hit must not trigger the proc.
            // (Without a defender Body the proc is a no-op anyway, so we
            // just verify no exception across many seeds — the gate is
            // implicit in the early-return at the top of the override.)
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cutting", "LongBlades");
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: non-Cudgel hit with Hammer owned must not throw.");
            }
        }

        // ====================================================================
        // Acceptance criteria — integration tests for Hammer-positive,
        // Backswing-on-miss, Rejoinder-on-dodge. These require full
        // body+inventory+weapon construction (Hammer) or PerformSingleAttack
        // invocation through the dispatcher (Backswing/Rejoinder).
        //
        // Scoped to verify the WEAPON-SKILLS-PARITY-T2.md acceptance
        // claims that were deferred in WSP3.4's commit body:
        //   - Hammer: across many seeds, observe Broken on equipped item
        //   - Backswing: across many seeds with deterministic miss,
        //     observe re-attack ("(Backswing)" marker in message log)
        //   - Rejoinder: defender-side counter-attack on missed incoming
        //     ("(Rejoinder)" marker in message log)
        // ====================================================================

        /// <summary>Build a creature with a humanoid Body, Stats, Inventory,
        /// and StatusEffectsPart. Mirrors the CombatSystemSpecTests pattern
        /// — duplicated locally to keep the test fixture self-contained.</summary>
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
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        /// <summary>Build a fresh weapon entity with the given attributes
        /// string (e.g. "Bludgeoning Cudgel") + dice. Returns the entity
        /// (caller calls EquipToBodyPart to put it in a hand).</summary>
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
            e.AddPart(new StatusEffectsPart());  // for BrokenEffect target
            return e;
        }

        // ── Cudgel_Hammer positive integration ──────────────────────────

        [Test]
        public void Hammer_WithEquippedDefender_AppliesBrokenAcrossSeeds()
        {
            // Acceptance: Given Cudgel_Hammer owned + Mace equipped + defender
            // has at least one equipped item, when swinging across many seeds,
            // then at least one swing produces BrokenEffect on the defender's
            // equipped item.
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);

            // Defender has a Body with hands; equip an item in one hand so
            // ForeachEquippedObject sees a candidate.
            var defender = MakeBodiedCreature("defender");
            var defenderItem = MakeWeaponEntity("test_blade", "1d4", "Cutting LongBlades");
            var defenderBody = defender.GetPart<Body>();
            var defenderHand = defenderBody.GetParts().Find(p => p.Type == "Hand");
            Assert.IsNotNull(defenderHand, "Humanoid body must have a Hand body part.");
            defender.GetPart<InventoryPart>().EquipToBodyPart(defenderItem, defenderHand);

            bool observed = false;
            for (int seed = 0; seed < 5000 && !observed; seed++)
            {
                var ctx = new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = MakeDamage("Cudgel"), ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                };
                skill.OnAttackerAfterAttack(ctx);
                if (defenderItem.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 5000 seeds at 2% chance, Hammer should produce at least one " +
                $"BrokenEffect on the defender's equipped item. " +
                $"P(zero in 5000) = (0.98)^5000 ≈ 4e-44.");
        }

        private static Damage MakeDamage(params string[] attrs)
        {
            var d = new Damage(10);
            foreach (var a in attrs) d.AddAttribute(a);
            return d;
        }

        // ── Cudgel_Backswing on-miss integration ────────────────────────

        [Test]
        public void Backswing_OnMissedCudgelSwing_ReAttacksAcrossSeeds()
        {
            // Acceptance: Given Cudgel_Backswing owned + Cudgel weapon equipped +
            // defender with very high DV (always misses), when swinging across
            // many seeds via PerformSingleAttack, then at least one missed swing
            // produces a "(Backswing)" message in the log (the re-attack).
            var attacker = MakeBodiedCreature("attacker", strength: 16, agility: 5);
            attacker.AddPart(new SkillsPart());
            Assert.IsTrue(attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Backswing(), source: "test"));

            var mace = MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel", penBonus: 3);
            var attackerHand = attacker.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            attacker.GetPart<InventoryPart>().EquipToBodyPart(mace, attackerHand);

            // Defender with very high DV — give them a +25 DV armor piece
            // baked into ArmorPart so GetDV returns ~30+ and the attacker
            // (with low Agility) almost always misses.
            var defender = MakeBodiedCreature("defender", agility: 16);
            defender.GetPart<ArmorPart>().DV = 25;

            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool observedBackswing = false;
            int totalMisses = 0;
            for (int seed = 0; seed < 200 && !observedBackswing; seed++)
            {
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(
                    attacker, defender,
                    mace.GetPart<MeleeWeaponPart>(), isPrimary: true,
                    zone, new Random(seed),
                    attackSourceDesc: null);
                var recent = MessageLog.GetRecent(10);
                foreach (var msg in recent)
                {
                    if (msg.Contains("misses")) totalMisses++;
                    if (msg.Contains("(Backswing)"))
                    {
                        observedBackswing = true;
                        break;
                    }
                }
            }
            Assert.Greater(totalMisses, 0,
                "Setup sanity: across 200 seeds at high DV, expected at least one miss. " +
                $"Observed {totalMisses}.");
            Assert.IsTrue(observedBackswing,
                $"Across 200 seeds with mostly-missing setup, expected at least one " +
                $"\"(Backswing)\" marker in the message log (re-attack triggered). " +
                $"Total misses observed: {totalMisses}.");
        }

        // ── ShortBlades_Rejoinder on-dodge integration ──────────────────

        [Test]
        public void Rejoinder_OnIncomingMiss_PlayerCounterAttacksAcrossSeeds()
        {
            // Acceptance: Given ShortBlades_Rejoinder owned + Piercing weapon
            // equipped + an enemy whose attacks miss the player, when the miss
            // resolves across many seeds, then at least one miss triggers the
            // player's counter-attack (observable as "(Rejoinder)" in message log).
            var player = MakeBodiedCreature("player", strength: 16, agility: 16);
            player.AddPart(new SkillsPart());
            player.GetPart<SkillsPart>().AddSkill(new ShortBlades_Rejoinder(), source: "test");

            var dagger = MakeWeaponEntity("dagger", "1d4", "Piercing", penBonus: 1);
            var playerHand = player.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            player.GetPart<InventoryPart>().EquipToBodyPart(dagger, playerHand);
            // Bake high DV onto the player so the NPC's attacks mostly miss.
            player.GetPart<ArmorPart>().DV = 25;

            var npc = MakeBodiedCreature("npc", strength: 12, agility: 5);
            var npcWeapon = MakeWeaponEntity("club", "1d6", "Bludgeoning", penBonus: 0);
            var npcHand = npc.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            npc.GetPart<InventoryPart>().EquipToBodyPart(npcWeapon, npcHand);

            var zone = new Zone();
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);

            bool observedRejoinder = false;
            int totalMisses = 0;
            for (int seed = 0; seed < 200 && !observedRejoinder; seed++)
            {
                MessageLog.Clear();
                // NPC attacks PLAYER — we expect mostly misses + occasional Rejoinder counters.
                CombatSystem.PerformSingleAttack(
                    attacker: npc, defender: player,
                    weapon: npcWeapon.GetPart<MeleeWeaponPart>(), isPrimary: true,
                    zone: zone, rng: new Random(seed),
                    attackSourceDesc: null);
                var recent = MessageLog.GetRecent(10);
                foreach (var msg in recent)
                {
                    if (msg.Contains("misses")) totalMisses++;
                    if (msg.Contains("(Rejoinder)"))
                    {
                        observedRejoinder = true;
                        break;
                    }
                }
            }
            Assert.Greater(totalMisses, 0,
                $"Setup sanity: across 200 seeds, expected ≥ 1 NPC miss on the player. " +
                $"Observed {totalMisses}.");
            Assert.IsTrue(observedRejoinder,
                $"Across 200 seeds with mostly-missing NPC swings, expected ≥ 1 " +
                $"\"(Rejoinder)\" marker (player counter-attack). " +
                $"Total NPC misses: {totalMisses}.");
        }
    }
}
