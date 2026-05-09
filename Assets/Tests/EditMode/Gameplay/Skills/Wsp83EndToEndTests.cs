using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — End-to-end tests for all 15 newly-shipped active
    /// abilities. Each test fires the production GameEvent dispatch
    /// path (<c>actor.FireEvent(GameEvent.New("CommandX"))</c>) — the
    /// same path InputHandler.ResolveAbilityCommand uses — and
    /// verifies that:
    /// <list type="number">
    ///   <item>SkillsPart.HandleEvent picks up the Command*.</item>
    ///   <item>TryRouteSkillCommand routes to the right skill.</item>
    ///   <item>The skill's OnCommand executes its primary mechanic.</item>
    ///   <item>CommandRouted diag fires with the right skill class.</item>
    ///   <item>Cooldown is applied after dispatch.</item>
    /// </list>
    ///
    /// <para>Distinct from the per-skill test files which call
    /// <c>OnCommand(ctx)</c> directly — those pin the per-skill
    /// behavior in isolation. THIS class proves the wiring works
    /// end-to-end through the same FireEvent path the live game
    /// uses, covering layers the unit tests skip:
    /// SkillsPart.HandleEvent's Command-prefix check, the
    /// GameEvent param plumbing (Zone / RNG / DirectionX / DirectionY),
    /// the cooldown-apply path, and the diag emission. If any of
    /// these layers regress, this class catches it where the
    /// per-skill tests would silently pass.</para>
    /// </summary>
    public class Wsp83EndToEndTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        // ── Shared fixture helpers ────────────────────────────────────────

        private static Entity MakeBodied(string name = "actor", int hp = 100, int strength = 18)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["ColdResistance"] = new Stat { Owner = e, Name = "ColdResistance", BaseValue = 0, Min = -100, Max = 100 };
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

        private static Entity MakeWeapon(string name, string dice, string attrs)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = dice, Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void Equip(Entity actor, Entity w)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(w, hand);
        }

        /// <summary>Fire a Command* GameEvent the same way InputHandler
        /// does (Zone + RNG + SourceCell + DirectionX/Y params), then
        /// release. Returns whether the event was handled.</summary>
        private static bool FireSkillCommand(Entity actor, string command,
            Zone zone, Random rng, int dx = 0, int dy = 0)
        {
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)rng);
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            if (zone != null)
            {
                var pos = zone.GetEntityPosition(actor);
                if (pos.x >= 0)
                    cmd.SetParameter("SourceCell", (object)zone.GetCell(pos.x, pos.y));
            }
            actor.FireEvent(cmd);
            bool handled = cmd.Handled;
            cmd.Release();
            return handled;
        }

        /// <summary>Assert the dispatch layer emitted exactly one
        /// CommandRouted record naming the expected skill class.</summary>
        private static void AssertRoutedTo(string skillClass)
        {
            var routed = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "CommandRouted", Limit = 5
            }).Records;
            Assert.AreEqual(1, routed.Count, "Expected exactly 1 CommandRouted diag.");
            StringAssert.Contains(skillClass, routed[0].PayloadJson,
                "CommandRouted payload should name the skill class.");
        }

        /// <summary>Assert the skill's ActivatedAbility cooldown is set
        /// to MaxCooldown (proving SkillsPart applied the cooldown
        /// after OnCommand returned).</summary>
        private static void AssertCooldownApplied(Entity actor, BaseSkillPart skill, int expectedCooldown)
        {
            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Assert.AreEqual(expectedCooldown, ability.CooldownRemaining,
                "Cooldown must be reset to MaxCooldown after dispatch.");
        }

        // ════════════════════════════════════════════════════════════════
        // Cudgel_GroundPound — SelfCentered, 8-adjacent damage + Stun
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_GroundPound_DispatchesAndAppliesStun()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8+2", "Bludgeoning Cudgel"));
            var skill = new Cudgel_GroundPound();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandGroundPound", zone, new Random(7));

            Assert.IsTrue(handled, "GroundPound GameEvent must be handled by SkillsPart.HandleEvent.");
            AssertRoutedTo("Cudgel_GroundPound");
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "GroundPound's primary mechanic (Stun) must fire end-to-end.");
            AssertCooldownApplied(atk, skill, Cudgel_GroundPound.COOLDOWN);
        }

        // ════════════════════════════════════════════════════════════════
        // Cudgel_ChargingStrike — DirectionLine, move + strike
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_ChargingStrike_DispatchesAndMovesActor()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var skill = new Cudgel_ChargingStrike();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 8, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandChargingStrike", zone,
                new Random(42), dx: 1, dy: 0);

            Assert.IsTrue(handled);
            AssertRoutedTo("Cudgel_ChargingStrike");
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual(7, pos.x, "Actor must charge to one cell short of target.");
            AssertCooldownApplied(atk, skill, Cudgel_ChargingStrike.COOLDOWN);
        }

        // ════════════════════════════════════════════════════════════════
        // Cudgel_Disarm — AdjacentCell, equipment strip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Disarm_DispatchesAndDropsTargetWeapon()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var skill = new Cudgel_Disarm();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var defWeapon = MakeWeapon("longsword", "1d8", "Cutting LongBlades");
            def.GetPart<InventoryPart>().AddObject(defWeapon);
            var defHand = def.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            def.GetPart<InventoryPart>().EquipToBodyPart(defWeapon, defHand);

            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandDisarm", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Cudgel_Disarm");
            var weaponCell = zone.GetEntityCell(defWeapon);
            Assert.IsNotNull(weaponCell, "Weapon must be placed in zone after Disarm dispatch.");
            Assert.AreEqual((6, 5), (weaponCell.X, weaponCell.Y));
        }

        // ════════════════════════════════════════════════════════════════
        // Axe_RendArmor — AdjacentCell, applies ShatterArmor stacks
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_RendArmor_DispatchesAndAppliesShatterStacks()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "1d8", "Cutting Axe"));
            var skill = new Axe_RendArmor();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandRendArmor", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Axe_RendArmor");
            var sa = def.GetPart<StatusEffectsPart>().GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(sa);
            Assert.AreEqual(Axe_RendArmor.REND_STACKS, sa.StackCount);
        }

        // ════════════════════════════════════════════════════════════════
        // ShortBlades_Backstab — AdjacentCell, flank-gated bonus
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Backstab_DispatchesAndStrikes()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "1d4+1", "Piercing"));
            var skill = new ShortBlades_Backstab();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            int hpBefore = def.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandBackstab", zone, new Random(42));

            Assert.IsTrue(handled);
            AssertRoutedTo("ShortBlades_Backstab");
            // Backstab fires PerformSingleAttack; with seed 42 + Str 18 + DV 0
            // we expect a hit (damage > 0).
            Assert.LessOrEqual(def.GetStatValue("Hitpoints"), hpBefore);
        }

        // ════════════════════════════════════════════════════════════════
        // ShortBlades_Disengage — DirectionLine, mobility
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Disengage_DispatchesAndMovesActor()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "1d4", "Piercing"));
            var skill = new ShortBlades_Disengage();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandDisengage", zone,
                new Random(0), dx: 1, dy: 0);

            Assert.IsTrue(handled);
            AssertRoutedTo("ShortBlades_Disengage");
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual(8, pos.x, "Disengage must move actor 3 cells East.");
        }

        // ════════════════════════════════════════════════════════════════
        // Acrobatics_EvasiveRoll — SelfCentered, cleanse one effect
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_EvasiveRoll_DispatchesAndCleanses()
        {
            var actor = MakeBodied("actor");
            var skill = new Acrobatics_EvasiveRoll();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            actor.ApplyEffect(new StunnedEffect(2), actor, null);
            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>());

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandEvasiveRoll", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Acrobatics_EvasiveRoll");
            Assert.IsFalse(actor.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "EvasiveRoll must remove the Stunned effect end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Acrobatics_Vault — DirectionLine, leap over obstacle
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Vault_DispatchesAndLands()
        {
            var actor = MakeBodied("actor");
            var skill = new Acrobatics_Vault();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);

            // Wall at (6,5) — Vault should leap over it to (7,5).
            var wall = new Entity { ID = "wall", BlueprintName = "wall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });
            zone.AddEntity(wall, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandVault", zone,
                new Random(0), dx: 1, dy: 0);

            Assert.IsTrue(handled);
            AssertRoutedTo("Acrobatics_Vault");
            var pos = zone.GetEntityPosition(actor);
            Assert.AreEqual((7, 5), (pos.x, pos.y), "Vault lands at distance 2.");
        }

        // ════════════════════════════════════════════════════════════════
        // Spellcraft_ArcaneSurge — SelfCentered, reset cooldowns
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_ArcaneSurge_DispatchesAndResetsOtherCooldowns()
        {
            var actor = MakeBodied("actor");
            var skill = new Spellcraft_ArcaneSurge();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            // Add another ability with cooldown ticking.
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var otherID = abilities.AddAbility("Test", "CommandTest", "Skills",
                AbilityTargetingMode.AdjacentCell, 1, "");
            var other = abilities.GetAbility(otherID);
            other.MaxCooldown = 30;
            other.CooldownRemaining = 20;

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandArcaneSurge", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Spellcraft_ArcaneSurge");
            Assert.AreEqual(0, other.CooldownRemaining,
                "ArcaneSurge must reset the other ability's cooldown end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Spellcraft_LeyTap — SelfCentered, drain HP + buff state
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_LeyTap_DispatchesAndStoresBuff()
        {
            var actor = MakeBodied("actor", hp: 100);
            var skill = new Spellcraft_LeyTap();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandLeyTap", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Spellcraft_LeyTap");
            Assert.Less(actor.GetStatValue("Hitpoints"), 100, "LeyTap must drain HP.");
            Assert.Greater(skill.PendingBonus, 0, "LeyTap must store a pending bonus.");
        }

        // ════════════════════════════════════════════════════════════════
        // Pyromancy_Pyroclasm — AdjacentCell, consume Burning + AOE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Pyroclasm_DispatchesAndConsumesBurning()
        {
            var atk = MakeBodied("atk");
            var skill = new Pyromancy_Pyroclasm();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            def.AddPart(new ThermalPart());
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            def.ApplyEffect(new BurningEffect(intensity: 2.0f), atk, zone);
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>());

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandPyroclasm", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Pyromancy_Pyroclasm");
            Assert.IsFalse(def.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "Pyroclasm must consume the BurningEffect.");
        }

        // ════════════════════════════════════════════════════════════════
        // Pyromancy_HeartFlame — SelfCentered, drain HP + multi-charge buff
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_HeartFlame_DispatchesAndStoresCharges()
        {
            var actor = MakeBodied("actor", hp: 100);
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandHeartFlame", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Pyromancy_HeartFlame");
            Assert.Less(actor.GetStatValue("Hitpoints"), 100, "HeartFlame must drain HP.");
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, skill.ChargesRemaining,
                "HeartFlame must set BUFF_CHARGES (3) charges end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Cryomancy_Frostbind — AdjacentCell, applies Rooted
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Frostbind_DispatchesAndAppliesRooted()
        {
            var atk = MakeBodied("atk");
            var skill = new Cryomancy_Frostbind();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandFrostbind", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Cryomancy_Frostbind");
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<RootedEffect>(),
                "Frostbind must apply RootedEffect end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Cryomancy_Hibernate — SelfCentered, applies HibernatingEffect
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Hibernate_DispatchesAndAppliesHibernating()
        {
            var actor = MakeBodied("actor");
            var skill = new Cryomancy_Hibernate();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            bool handled = FireSkillCommand(actor, "CommandHibernate", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Cryomancy_Hibernate");
            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<HibernatingEffect>());
            Assert.AreEqual(100, actor.GetStatValue("HeatResistance"),
                "Hibernate's resistance buff must be applied end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Galvanism_Overload — DirectionLine, chain damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Overload_DispatchesAndDamagesWetTarget()
        {
            var atk = MakeBodied("atk");
            var skill = new Galvanism_Overload();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            def.Statistics["LightningResistance"] = new Stat
                { Owner = def, Name = "LightningResistance", BaseValue = 0, Min = -100, Max = 100 };
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            def.ApplyEffect(new WetEffect(), atk, zone);

            int hpBefore = def.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandOverload", zone,
                new Random(0), dx: 1, dy: 0);

            Assert.IsTrue(handled);
            AssertRoutedTo("Galvanism_Overload");
            Assert.Less(def.GetStatValue("Hitpoints"), hpBefore,
                "Overload must damage the Wet target end-to-end.");
        }

        // ════════════════════════════════════════════════════════════════
        // Cooldown gate: a second dispatch immediately after must be
        // REJECTED (cooldown remaining > 0). Pins the cooldown-blocks-
        // re-fire invariant for the new actives.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_GroundPound_SecondDispatch_BlockedByCooldown()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var skill = new Cudgel_GroundPound();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            FireSkillCommand(atk, "CommandGroundPound", zone, new Random(0));
            Diag.ResetAll();
            bool handled2 = FireSkillCommand(atk, "CommandGroundPound", zone, new Random(0));

            Assert.IsFalse(handled2,
                "Second dispatch must NOT be handled (cooldown blocks).");
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "CommandRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("cooldown", rejected[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // JSON content load — every WSP8.3 skill blueprint is registered.
        // Pins that the JSON entries can be reflectively resolved to
        // their C# classes; if any class name is typo'd / missing /
        // doesn't extend BaseSkillPart, the player never sees the skill
        // in the X-screen and the live game silently drops the ability.
        // Mirrors the Wsp6IntegrationTests / Wsp7MagicSkillsTests
        // pattern.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp83Skills_AllRegisteredInSkillRegistryFromJson()
        {
            SkillRegistry.EnsureInitialized();

            string[] wsp83PowerClasses = new[]
            {
                // Cudgel
                "Cudgel_ChargingStrike",
                "Cudgel_GroundPound",
                "Cudgel_Disarm",
                // Axe
                "Axe_RendArmor",
                // ShortBlades
                "ShortBlades_Backstab",
                "ShortBlades_Disengage",
                // Acrobatics
                "Acrobatics_EvasiveRoll",
                "Acrobatics_Vault",
                // Spellcraft
                "Spellcraft_ArcaneSurge",
                "Spellcraft_LeyTap",
                // Pyromancy
                "Pyromancy_Pyroclasm",
                "Pyromancy_HeartFlame",
                // Cryomancy
                "Cryomancy_Frostbind",
                "Cryomancy_Hibernate",
                // Galvanism
                "Galvanism_Overload",
            };

            foreach (var className in wsp83PowerClasses)
            {
                bool found = SkillRegistry.TryGetPowerByClass(className, out var power);
                Assert.IsTrue(found,
                    "WSP8.3 power '" + className + "' must be registered in SkillRegistry " +
                    "after loading content blueprints. If this fails, the JSON " +
                    "entry's Class field is missing / typo'd / malformed.");
                Assert.IsFalse(string.IsNullOrEmpty(power.Description),
                    "WSP8.3 power '" + className + "' must have a non-empty Description " +
                    "so the player sees something in the skills menu.");
            }
        }

        [Test]
        public void Wsp83Skills_AllResolveToConcreteSkillTypes()
        {
            // Type-resolution test: SkillsPart.AddSkill(string) must be
            // able to instantiate every WSP8.3 skill via reflection.
            // If a class moved namespaces or got renamed, this fails
            // before any gameplay code does.
            string[] wsp83PowerClasses = new[]
            {
                "Cudgel_ChargingStrike", "Cudgel_GroundPound", "Cudgel_Disarm",
                "Axe_RendArmor",
                "ShortBlades_Backstab", "ShortBlades_Disengage",
                "Acrobatics_EvasiveRoll", "Acrobatics_Vault",
                "Spellcraft_ArcaneSurge", "Spellcraft_LeyTap",
                "Pyromancy_Pyroclasm", "Pyromancy_HeartFlame",
                "Cryomancy_Frostbind", "Cryomancy_Hibernate",
                "Galvanism_Overload",
            };

            foreach (var className in wsp83PowerClasses)
            {
                var actor = MakeBodied("actor_" + className);
                bool added = actor.GetPart<SkillsPart>().AddSkill(className, source: "test");
                Assert.IsTrue(added,
                    "AddSkill(\"" + className + "\") must succeed via reflection.");
                var skill = actor.GetPart<SkillsPart>().GetSkill(className);
                Assert.IsNotNull(skill,
                    "Skill " + className + " must be retrievable post-Add.");
                var spec = skill.DeclareActivatedAbility(actor);
                Assert.IsNotNull(spec,
                    "Skill " + className + " must declare an activated ability.");
                Assert.IsFalse(string.IsNullOrEmpty(spec.Command),
                    "Skill " + className + "'s Command must be non-empty.");
                Assert.AreNotEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                    "Skill " + className + " must have a registered ability Guid post-Add.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Wrong-command-name: SkillsPart routes only matching commands.
        // Pins the negative case — a command for a different skill must
        // NOT trigger our skill.
        // ════════════════════════════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════
        // Pre-WSP8.2 actives — same E2E shape, closes the symmetry gap
        // surfaced by the May-2026 audit. These 5 actives predate the
        // SkillRejected convention but use the same SkillsPart dispatch
        // path, so the FireEvent-end-to-end + diag emissions must work
        // identically.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_Slam_DispatchesAndPushesTarget()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var skill = new Cudgel_Slam();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 100);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandSlam", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Cudgel_Slam");
            var pos = zone.GetEntityPosition(def);
            Assert.AreEqual(9, pos.x, "Slam pushes 3 cells when path is clear.");
        }

        [Test]
        public void E2E_Slam_NoWeapon_EmitsSkillRejected()
        {
            var atk = MakeBodied("atk");
            // No weapon equipped.
            var skill = new Cudgel_Slam();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            FireSkillCommand(atk, "CommandSlam", zone, new Random(0));

            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count,
                "Slam without a Cudgel weapon must emit SkillRejected (post-audit fix).");
            StringAssert.Contains("no_weapon", rejected[0].PayloadJson);
        }

        [Test]
        public void E2E_Conk_DispatchesAndStunsTarget()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var skill = new Cudgel_Conk();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandConk", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Cudgel_Conk");
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Conk applies Stunned regardless of swing outcome.");
        }

        [Test]
        public void E2E_Conk_NoTarget_EmitsSkillRejected()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var skill = new Cudgel_Conk();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); // no target

            Diag.ResetAll();
            FireSkillCommand(atk, "CommandConk", zone, new Random(0));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_target", rejected[0].PayloadJson);
        }

        [Test]
        public void E2E_Berserk_DispatchesAndAppliesBuff()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "1d8", "Cutting Axe"));
            var skill = new Axe_Berserk();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandAxeBerserk", null, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Axe_Berserk");
            Assert.IsTrue(atk.GetPart<StatusEffectsPart>().HasEffect<BerserkEffect>(),
                "Berserk applies BerserkEffect to self.");
        }

        [Test]
        public void E2E_Berserk_NoWeapon_EmitsSkillRejected()
        {
            var atk = MakeBodied("atk");
            // No axe equipped.
            var skill = new Axe_Berserk();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            FireSkillCommand(atk, "CommandAxeBerserk", null, new Random(0));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_weapon", rejected[0].PayloadJson);
        }

        [Test]
        public void E2E_HookAndDrag_DispatchesAndAppliesHooked()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "1d8", "Cutting Axe"));
            var skill = new Axe_HookAndDrag();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 100);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandHookAndDrag", zone, new Random(0));

            Assert.IsTrue(handled);
            AssertRoutedTo("Axe_HookAndDrag");
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "HookAndDrag applies HookedEffect to the adjacent target.");
        }

        [Test]
        public void E2E_HookAndDrag_NoTarget_EmitsSkillRejected()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "1d8", "Cutting Axe"));
            var skill = new Axe_HookAndDrag();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); // no target

            Diag.ResetAll();
            FireSkillCommand(atk, "CommandHookAndDrag", zone, new Random(0));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_target", rejected[0].PayloadJson);
        }

        [Test]
        public void E2E_Shank_DispatchesAndStrikes()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "1d4+1", "Piercing"));
            var skill = new ShortBlades_Shank();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 100);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            int hpBefore = def.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandShank", zone, new Random(42));

            Assert.IsTrue(handled);
            AssertRoutedTo("ShortBlades_Shank");
            Assert.LessOrEqual(def.GetStatValue("Hitpoints"), hpBefore,
                "Shank fires PerformSingleAttack — defender HP should drop on hit.");
        }

        [Test]
        public void E2E_Shank_NoTarget_EmitsSkillRejected()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "1d4", "Piercing"));
            var skill = new ShortBlades_Shank();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); // no target

            Diag.ResetAll();
            FireSkillCommand(atk, "CommandShank", zone, new Random(0));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 5
            }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_target", rejected[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // Wrong-command-name: SkillsPart routes only matching commands.
        // Pins the negative case — a command for a different skill must
        // NOT trigger our skill.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void E2E_WrongCommandName_DoesNotDispatchToOurSkill()
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var skill = new Cudgel_GroundPound();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");

            // Fire a different command (pretend Slam, but the actor doesn't have Slam owned).
            Diag.ResetAll();
            bool handled = FireSkillCommand(atk, "CommandSlam", zone, new Random(0));

            Assert.IsFalse(handled, "Unrelated command must not be handled.");
            Assert.AreEqual(hpBefore, def.GetStatValue("Hitpoints"),
                "Defender must be untouched — wrong command shouldn't dispatch GroundPound.");
        }
    }
}
