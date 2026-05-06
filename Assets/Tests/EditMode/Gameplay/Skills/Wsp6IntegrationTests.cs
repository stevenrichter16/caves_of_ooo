using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.24 — End-to-end integration tests for every WSP6 skill.
    /// These tests prove each skill ACTUALLY WORKS through the real
    /// game pipeline — not just the isolated <c>OnCommand</c> /
    /// <c>OnAttackerAfterAttack</c> calls covered in the per-skill
    /// fixtures.
    ///
    /// <para>What "actually works" means here:
    /// <list type="bullet">
    ///   <item><b>Command-dispatch path</b>: skills are invoked via
    ///         <see cref="SkillsPart.TryRouteSkillCommand"/> (the real
    ///         input-dispatch path), NOT direct <c>OnCommand</c> calls.
    ///         Verifies ability registration + Guid lookup +
    ///         cooldown gate + parameter threading.</item>
    ///   <item><b>PerformSingleAttack flow</b>: passive procs are
    ///         observed via real <see cref="CombatSystem.PerformSingleAttack"/>
    ///         calls (not synthetic <c>SkillEventContext</c>), so the
    ///         dispatcher routes events correctly + the attacker's
    ///         skills participate in the swing's hit/damage/effect math.</item>
    ///   <item><b>Turn-tick path</b>: ongoing effects (HookedEffect's
    ///         drag, Stunned's duration, cooldown decrements) are
    ///         observed via real <c>EndTurn</c> event firing, not
    ///         direct <c>OnTurnEnd</c> calls. Verifies the StatusEffects
    ///         part hears + propagates the event correctly.</item>
    ///   <item><b>JSON content load</b>: every WSP6 skill's blueprint
    ///         loads from <c>Resources/Content/Data/Skills/*.json</c>
    ///         and is queryable via <see cref="SkillRegistry"/>.</item>
    ///   <item><b>Cross-skill interaction</b>: skills that should stack
    ///         do (Hooked + Shank pen-bonus); skills that should NOT
    ///         interfere don't (Slam target stunned + then re-attacked
    ///         via PerformSingleAttack respects the Stunned gate).</item>
    /// </list></para>
    ///
    /// <para>If any of these tests fail, the user-facing skill is
    /// broken — players pressing the keybind would see no effect, or
    /// the wrong effect, or a crash. The per-skill unit tests can
    /// pass while these fail (e.g., if the JSON entry has a typo'd
    /// class name, or the ability Guid doesn't get persisted, or the
    /// dispatcher doesn't fire the right hook).</para>
    /// </summary>
    public class Wsp6IntegrationTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            // Important: SkillRegistry only auto-loads from Resources
            // on EnsureInitialized. Tests that need the real JSON
            // call EnsureInitialized themselves.
        }

        // ── Full-actor fixture: every Part a skill might query ──────────

        private static Entity MakeFullActor(string name = "actor",
            int strength = 16, int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
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

        private static Entity MakeWeapon(string name, string dice, string attributes)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = dice, PenBonus = 0,
                Attributes = attributes,
            });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void EquipPrimary(Entity actor, Entity weapon)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weapon, hand);
        }

        private static Entity MakeWall()
        {
            var w = new Entity { ID = "wall", BlueprintName = "wall" };
            w.Tags["Solid"] = "";
            w.Tags["Wall"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall" });
            return w;
        }

        /// <summary>Fire the real EndTurn event on the actor. Mirrors
        /// what <see cref="CavesOfOoo.Core.TurnManager.EndTurn"/> does:
        /// build the GameEvent, set Zone parameter, fire on actor.
        /// Effects + cooldowns tick via their real HandleEvent paths.</summary>
        private static void FireEndTurn(Entity actor, Zone zone)
        {
            var endTurn = GameEvent.New("EndTurn");
            endTurn.SetParameter("Zone", (object)zone);
            actor.FireEventAndRelease(endTurn);
        }

        /// <summary>Fire the real BeginTakeAction event on the actor.
        /// Triggers OnTurnStart on every effect (damage ticks).</summary>
        private static void FireBeginTakeAction(Entity actor, Zone zone)
        {
            var begin = GameEvent.New("BeginTakeAction");
            begin.SetParameter("Zone", (object)zone);
            actor.FireEventAndRelease(begin);
        }

        // ════════════════════════════════════════════════════════════════
        // CUDGEL_SLAM — full pipeline integration
        // ════════════════════════════════════════════════════════════════

        /// <summary>The most important Slam test: fire CommandSlam through
        /// the real dispatcher, verify target moved + Stunned applied +
        /// cooldown set. If this fails, players pressing the Slam keybind
        /// see no effect.</summary>
        [Test]
        public void Slam_CommandRoute_TargetPushedAndStunnedAndCooldownApplied()
        {
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Slam(), source: "test");

            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Use the REAL command-dispatch path — this is what the
            // input controller will call when the player presses the
            // Slam keybind.
            bool routed = attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandSlam", zone, new Random(0));

            Assert.IsTrue(routed,
                "TryRouteSkillCommand('CommandSlam') must return true — " +
                "if false, the dispatcher couldn't find Slam's ability " +
                "(Guid not registered, command string mismatch, etc.).");

            // Verify the gameplay effect actually happened.
            var pos = zone.GetEntityPosition(defender);
            Assert.AreEqual((9, 5), (pos.x, pos.y),
                "Defender should be slammed East 3 cells (clear path).");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Defender should be Stunned by the slam.");

            // Verify the cooldown is now set on the actual ability
            // (not just on the skill instance).
            var skill = attacker.GetPart<SkillsPart>().GetSkill("Cudgel_Slam");
            var ability = attacker.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(skill.ActivatedAbilityID);
            Assert.AreEqual(Cudgel_Slam.COOLDOWN, ability.CooldownRemaining,
                "Cooldown must equal COOLDOWN immediately after a successful Slam.");
            Assert.IsFalse(ability.IsUsable,
                "Slam should not be usable while on cooldown.");
        }

        [Test]
        public void Slam_CommandRoute_CooldownGatesSecondInvocation()
        {
            // Real dispatch sequence: Slam works once, second Slam is
            // refused while cooldown active. Players pressing the
            // keybind twice in quick succession should see only one slam.
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Slam(), source: "test");
            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            attacker.GetPart<SkillsPart>().TryRouteSkillCommand("CommandSlam", zone, new Random(0));
            // Restore defender to (6,5) for clean second-attempt setup.
            zone.MoveEntity(defender, 6, 5);

            bool secondRoute = attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandSlam", zone, new Random(1));

            Assert.IsFalse(secondRoute,
                "Second Slam-while-on-cooldown must return false — the cooldown gate fires.");
        }

        [Test]
        public void Slam_Cooldown_TicksDownOnRealEndTurn()
        {
            // After Slam fires, the cooldown should tick down by 1 each
            // EndTurn event fired on the actor. This proves the
            // ActivatedAbilitiesPart's EndTurn handler is wired correctly.
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Slam(), source: "test");
            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            attacker.GetPart<SkillsPart>().TryRouteSkillCommand("CommandSlam", zone, new Random(0));
            var skill = attacker.GetPart<SkillsPart>().GetSkill("Cudgel_Slam");
            var ability = attacker.GetPart<ActivatedAbilitiesPart>()
                .GetAbility(skill.ActivatedAbilityID);

            int startCD = ability.CooldownRemaining;
            FireEndTurn(attacker, zone);
            Assert.AreEqual(startCD - 1, ability.CooldownRemaining,
                "EndTurn fired on actor must tick the cooldown down by 1.");

            // Tick to zero.
            for (int t = 0; t < startCD; t++)
                FireEndTurn(attacker, zone);
            Assert.AreEqual(0, ability.CooldownRemaining,
                "Cooldown must reach 0 after COOLDOWN EndTurn ticks.");
            Assert.IsTrue(ability.IsUsable,
                "Ability should be usable again after cooldown expires.");
        }

        // ════════════════════════════════════════════════════════════════
        // CUDGEL_CONK — full pipeline integration
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Conk_CommandRoute_StunnedAppliedToAdjacentTarget()
        {
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Conk(), source: "test");
            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool routed = attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandConk", zone, new Random(0));

            Assert.IsTrue(routed, "TryRouteSkillCommand('CommandConk') must succeed.");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Conk must apply Stunned to the adjacent target via the real dispatch path.");
        }

        // ════════════════════════════════════════════════════════════════
        // AXE_BERSERK — full pipeline integration (self-target)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Berserk_CommandRoute_AppliesBerserkEffectToSelf()
        {
            var actor = MakeFullActor("actor");
            EquipPrimary(actor, MakeWeapon("battleaxe", "2d6", "Cutting Axe"));
            actor.GetPart<SkillsPart>().AddSkill(new Axe_Berserk(), source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);

            int strBefore = actor.GetStatValue("Strength");
            // Note: Axe_Berserk uses "CommandAxeBerserk" (verbatim Qud
            // parity per `qud-decompiled-project/.../Axe_Berserk.cs:122`).
            // Other CoO active abilities use the shorter "CommandX"
            // form (e.g. CommandConk, CommandSlam) — that's also verbatim
            // Qud parity for those skills. Convention is per-skill
            // arbitrary in Qud; CoO faithfully replicates each.
            bool routed = actor.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandAxeBerserk", zone, new Random(0));

            Assert.IsTrue(routed, "TryRouteSkillCommand('CommandAxeBerserk') must succeed.");
            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<BerserkEffect>(),
                "Berserk must apply BerserkEffect to self via the real dispatch path.");
            Assert.AreEqual(strBefore + BerserkEffect.STR_BONUS, actor.GetStatValue("Strength"),
                "Berserk's +Strength bonus must be observable via the stat-shift path.");
        }

        // ════════════════════════════════════════════════════════════════
        // SHORTBLADES_SHANK — full pipeline w/ pen-bonus integration
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_CommandRoute_AgainstDebuffedTarget_DealsMoreDamageThanClean()
        {
            // The whole point of Shank: target debuff count → +pen.
            // This statistical pin compares damage dealt by Shank vs
            // a clean target (0 effects) vs a heavily-debuffed target
            // (3 effects → +6 pen). The damage delta should be visible.
            const int SEEDS = 200;

            int totalAgainstClean = 0;
            int totalAgainstDebuffed = 0;

            for (int seed = 0; seed < SEEDS; seed++)
            {
                var (attCl, defCl, zCl) = MakeShankFixture();
                int hpClBefore = defCl.GetStatValue("Hitpoints");
                MessageLog.Clear();
                attCl.GetPart<SkillsPart>()
                    .TryRouteSkillCommand("CommandShank", zCl, new Random(seed));
                totalAgainstClean += System.Math.Max(0,
                    hpClBefore - defCl.GetStatValue("Hitpoints"));
            }

            for (int seed = 0; seed < SEEDS; seed++)
            {
                var (attDb, defDb, zDb) = MakeShankFixture();
                defDb.ApplyEffect(new BleedingEffect(), null, zDb);
                defDb.ApplyEffect(new StunnedEffect(), null, zDb);
                defDb.ApplyEffect(new ConfusedEffect(), null, zDb);
                int hpDbBefore = defDb.GetStatValue("Hitpoints");
                MessageLog.Clear();
                attDb.GetPart<SkillsPart>()
                    .TryRouteSkillCommand("CommandShank", zDb, new Random(seed));
                totalAgainstDebuffed += System.Math.Max(0,
                    hpDbBefore - defDb.GetStatValue("Hitpoints"));
            }

            Assert.Greater(totalAgainstDebuffed, totalAgainstClean,
                "Shank-via-real-dispatch against a debuffed target should deal " +
                $"strictly more damage than against a clean target. " +
                $"Debuffed: {totalAgainstDebuffed}; clean: {totalAgainstClean} (over {SEEDS} seeds).");
        }

        private static (Entity attacker, Entity defender, Zone zone) MakeShankFixture()
        {
            var att = MakeFullActor("attacker");
            EquipPrimary(att, MakeWeapon("dagger", "1d4", "Piercing"));
            att.GetPart<SkillsPart>().AddSkill(new ShortBlades_Shank(), source: "test");
            var def = MakeFullActor("defender");
            def.GetPart<ArmorPart>().AV = 4;
            var zone = new Zone();
            zone.AddEntity(att, 5, 5);
            zone.AddEntity(def, 6, 5);
            return (att, def, zone);
        }

        // ════════════════════════════════════════════════════════════════
        // AXE_HOOKANDDRAG — full pipeline + multi-turn drag
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookAndDrag_CommandRoute_HookedEffectAppliedToAdjacentTarget()
        {
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("axe", "1d8", "Cutting Axe"));
            attacker.GetPart<SkillsPart>().AddSkill(new Axe_HookAndDrag(), source: "test");
            var defender = MakeFullActor("defender", strength: 5);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool routed = attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandHookAndDrag", zone, new Random(0));

            Assert.IsTrue(routed, "TryRouteSkillCommand('CommandHookAndDrag') must succeed.");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "HookAndDrag must apply HookedEffect to the adjacent target.");
        }

        [Test]
        public void HookAndDrag_MultiTurnDrag_TargetEventuallyAdjacentViaRealEndTurn()
        {
            // Hook a target far away → fire EndTurn events on the
            // target → target should drag closer each turn (via real
            // StatusEffectsPart.HandleEvent("EndTurn") → effect.OnTurnEnd
            // path) until adjacent.
            //
            // Use a high SaveTarget so the strength save can't break
            // the hook on every seed (we want to observe the drag, not
            // the save).
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("axe", "1d8", "Cutting Axe"));
            attacker.GetPart<SkillsPart>().AddSkill(new Axe_HookAndDrag(), source: "test");
            var defender = MakeFullActor("defender", strength: 1);  // weak save
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Apply Hook via the real command-dispatch path.
            attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandHookAndDrag", zone, new Random(0));

            // Move defender far away (simulating they ran). Hook persists.
            zone.MoveEntity(defender, 5, 11);

            // Fire EndTurn events on defender. Each tick: real
            // StatusEffectsPart hears, calls HookedEffect.OnTurnEnd,
            // drag fires. Target should approach (5, 5) over time.
            int initialDistance = System.Math.Abs(11 - 5);
            int finalDistance = initialDistance;
            for (int turn = 0; turn < 8 && finalDistance > 1; turn++)
            {
                FireEndTurn(defender, zone);
                var pos = zone.GetEntityPosition(defender);
                if (pos.x < 0) break; // defender removed (save broke hook + cleanup)
                finalDistance = System.Math.Abs(pos.y - 5) + System.Math.Abs(pos.x - 5);
            }

            Assert.Less(finalDistance, initialDistance,
                $"Multi-turn drag must reduce distance from {initialDistance} to less. " +
                $"Got finalDistance={finalDistance} after up to 8 turn-end ticks.");
        }

        [Test]
        public void HookedEffect_OnRealEndTurn_DurationDecrementsToZero()
        {
            // Verify HookedEffect's natural duration expiry works through
            // the real EndTurn → StatusEffectsPart → effect.OnTurnEnd
            // pipeline. After Duration ticks, the effect should be gone.
            // (Or the strength-save could break the hook earlier — either
            // outcome is "effect ends properly," which is what we test.)
            var attacker = MakeFullActor("attacker");
            var defender = MakeFullActor("defender", strength: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 5, 5);  // co-located so no drag

            var hook = new HookedEffect(duration: 3, attacker, saveTarget: 100, rng: new Random(0));
            defender.ApplyEffect(hook, attacker, zone);

            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "Pre-condition: HookedEffect applied.");

            // 3 EndTurn events → duration 3→2→1→0 → effect cleaned up.
            for (int t = 0; t < 5; t++)
                FireEndTurn(defender, zone);

            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "After 5 EndTurn events, HookedEffect (duration 3) must be removed " +
                "via the real turn-tick pipeline.");
        }

        // ════════════════════════════════════════════════════════════════
        // PUNCTURE — passive observed through real PerformSingleAttack
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Puncture_RealSwing_PenBonusIsObservedDuringPenetrationRoll()
        {
            // Set up two actors: identical except one has Puncture.
            // Both swing the same dagger at identical defenders across
            // the same RNG seeds. With Puncture: more pens → more damage.
            //
            // This is the strongest end-to-end proof Puncture works:
            // the pen bonus must thread through SkillEventDispatcher →
            // CombatSystem.PerformSingleAttack's bonus calculation →
            // RollPenetrations.
            const int SEEDS = 200;
            int totalWith = 0, totalWithout = 0;
            for (int seed = 0; seed < SEEDS; seed++)
            {
                var (attW, defW, zW, wW) = MakePunctureFixture(includePuncture: true);
                int hpWBefore = defW.GetStatValue("Hitpoints");
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(attW, defW, wW,
                    isPrimary: true, zone: zW, rng: new Random(seed),
                    attackSourceDesc: null);
                totalWith += System.Math.Max(0, hpWBefore - defW.GetStatValue("Hitpoints"));

                var (attN, defN, zN, wN) = MakePunctureFixture(includePuncture: false);
                int hpNBefore = defN.GetStatValue("Hitpoints");
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(attN, defN, wN,
                    isPrimary: true, zone: zN, rng: new Random(seed),
                    attackSourceDesc: null);
                totalWithout += System.Math.Max(0, hpNBefore - defN.GetStatValue("Hitpoints"));
            }
            Assert.Greater(totalWith, totalWithout,
                $"Puncture-equipped actor must deal more total damage in real swings. " +
                $"With: {totalWith}; without: {totalWithout} (over {SEEDS} seeds).");
        }

        private static (Entity, Entity, Zone, MeleeWeaponPart) MakePunctureFixture(bool includePuncture)
        {
            var att = MakeFullActor("attacker");
            var dagger = MakeWeapon("dagger", "1d4", "Piercing");
            EquipPrimary(att, dagger);
            if (includePuncture)
                att.GetPart<SkillsPart>().AddSkill(new ShortBlades_Puncture(), source: "test");
            var def = MakeFullActor("defender");
            def.GetPart<ArmorPart>().AV = 4;
            var zone = new Zone();
            zone.AddEntity(att, 5, 5);
            zone.AddEntity(def, 6, 5);
            return (att, def, zone, dagger.GetPart<MeleeWeaponPart>());
        }

        // ════════════════════════════════════════════════════════════════
        // DISMEMBER — passive observed through real PerformSingleAttack
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_RealSwingsAcrossSeeds_EventuallyDismembersAndBleeds()
        {
            // Actor with Axe_Dismember swings real PerformSingleAttack
            // at high HP defender. Across enough seeds at 3% chance,
            // we should observe a dismemberment + Bleeding via the
            // real dispatcher hook.
            const int SEEDS = 5000;
            bool everDismembered = false;
            bool everBled = false;

            for (int seed = 0; seed < SEEDS && (!everDismembered || !everBled); seed++)
            {
                var att = MakeFullActor("attacker", strength: 24);  // strong for hits
                EquipPrimary(att, MakeWeapon("battleaxe", "2d6", "Cutting Axe"));
                att.GetPart<SkillsPart>().AddSkill(new Axe_Dismember(), source: "test");
                var def = MakeFullActor($"defender_{seed}", hp: 1000);  // very high HP
                def.GetPart<ArmorPart>().AV = 0;  // no armor — guarantee pens

                var zone = new Zone();
                zone.AddEntity(att, 5, 5);
                zone.AddEntity(def, 6, 5);

                int partsBefore = def.GetPart<Body>().GetParts().Count;
                CombatSystem.PerformSingleAttack(att, def,
                    att.GetPart<Body>().GetParts().Find(p => p.Type == "Hand")
                        .Equipped.GetPart<MeleeWeaponPart>(),
                    isPrimary: true, zone: zone, rng: new Random(seed),
                    attackSourceDesc: null);

                if (def.GetPart<Body>().GetParts().Count < partsBefore)
                    everDismembered = true;
                if (def.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>())
                    everBled = true;
            }

            Assert.IsTrue(everDismembered,
                "Across 5000 real swings at 3% chance, Axe_Dismember should fire " +
                "at least once (via the real dispatcher hook). Got zero.");
            Assert.IsTrue(everBled,
                "Every successful Dismember should apply Bleeding via the real " +
                "Body.Dismember + ApplyEffect path.");
        }

        // ════════════════════════════════════════════════════════════════
        // DECAPITATE — marker effect observed in real Dismember
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Decapitate_WithDismember_ExpandsCandidatePoolToMortalParts_ViaRealSwing()
        {
            // Set up a defender with ONLY Mortal severable parts (head).
            // Without Decapitate: Dismember should NEVER fire (Mortal
            // skipped). With Decapitate: Dismember should fire on the
            // head, eventually.
            //
            // This proves the marker-skill modifies the Dismember candidate
            // pool VIA THE REAL ATTACK DISPATCHER, not just the synthetic
            // call covered in AxeDecapitateTests.
            const int SEEDS = 5000;
            bool everDismembered = false;

            for (int seed = 0; seed < SEEDS && !everDismembered; seed++)
            {
                var att = MakeFullActor("attacker", strength: 24);
                EquipPrimary(att, MakeWeapon("battleaxe", "2d6", "Cutting Axe"));
                att.GetPart<SkillsPart>().AddSkill(new Axe_Dismember(), source: "test");
                att.GetPart<SkillsPart>().AddSkill(new Axe_Decapitate(), source: "test");

                var def = MakeMortalOnlyDefender($"defender_{seed}");

                var zone = new Zone();
                zone.AddEntity(att, 5, 5);
                zone.AddEntity(def, 6, 5);

                int partsBefore = def.GetPart<Body>().GetParts().Count;
                CombatSystem.PerformSingleAttack(att, def,
                    att.GetPart<Body>().GetParts().Find(p => p.Type == "Hand")
                        .Equipped.GetPart<MeleeWeaponPart>(),
                    isPrimary: true, zone: zone, rng: new Random(seed),
                    attackSourceDesc: null);

                if (def.GetPart<Body>().GetParts().Count < partsBefore)
                    everDismembered = true;
            }

            Assert.IsTrue(everDismembered,
                "Decapitate + Dismember on Mortal-only defender via real swings " +
                "should produce at least one dismemberment across 5000 seeds. Got zero — " +
                "the marker-skill candidate-pool gate isn't firing through the real path.");
        }

        // Stub defender with ONLY a Mortal severable part (Head).
        private static Entity MakeMortalOnlyDefender(string name)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 1000, Min = 0, Max = 1000 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());  // AV=0 default
            e.AddPart(new StatusEffectsPart());

            var body = new Body();
            e.AddPart(body);
            var root = new BodyPart
            {
                Type = "Body", Description = "Body",
                Mortal = false, Appendage = false, Integral = true,
                Parts = new List<BodyPart>(),
            };
            var head = new BodyPart
            {
                Type = "Head", Description = "head",
                Mortal = true, Appendage = true, Integral = false,
                ParentPart = root,
                Parts = new List<BodyPart>(),
            };
            root.Parts.Add(head);
            body.SetBody(root);
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-SKILL: Hook + Shank (synergy via TYPE_NEGATIVE)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookThenShank_HookedEffectCounts_AsNegativeForShankPenBonus()
        {
            // Real combo: Hook a target (applies HookedEffect with
            // TYPE_NEGATIVE), then Shank — the HookedEffect should
            // count toward Shank's pen bonus.
            //
            // Compare 200 seeds of Shank-against-just-hooked vs Shank-
            // against-clean. The hooked variant should deal more damage
            // (one extra negative effect → +2 pen).
            const int SEEDS = 200;
            int totalHooked = 0, totalClean = 0;

            for (int seed = 0; seed < SEEDS; seed++)
            {
                // Hooked target.
                var (attH, defH, zH) = MakeHookedShankFixture(applyHook: true);
                int hpHBefore = defH.GetStatValue("Hitpoints");
                MessageLog.Clear();
                attH.GetPart<SkillsPart>()
                    .TryRouteSkillCommand("CommandShank", zH, new Random(seed));
                totalHooked += System.Math.Max(0,
                    hpHBefore - defH.GetStatValue("Hitpoints"));

                // Clean target (no Hook).
                var (attC, defC, zC) = MakeHookedShankFixture(applyHook: false);
                int hpCBefore = defC.GetStatValue("Hitpoints");
                MessageLog.Clear();
                attC.GetPart<SkillsPart>()
                    .TryRouteSkillCommand("CommandShank", zC, new Random(seed));
                totalClean += System.Math.Max(0,
                    hpCBefore - defC.GetStatValue("Hitpoints"));
            }

            Assert.Greater(totalHooked, totalClean,
                "Shank against a hooked target should deal more damage than against " +
                $"a clean target — HookedEffect's TYPE_NEGATIVE should count toward " +
                $"Shank's pen-per-debuff. Hooked: {totalHooked}; clean: {totalClean}.");
        }

        private static (Entity, Entity, Zone) MakeHookedShankFixture(bool applyHook)
        {
            var att = MakeFullActor("attacker");
            EquipPrimary(att, MakeWeapon("dagger", "1d4", "Piercing"));
            att.GetPart<SkillsPart>().AddSkill(new ShortBlades_Shank(), source: "test");
            var def = MakeFullActor("defender");
            def.GetPart<ArmorPart>().AV = 4;
            if (applyHook)
                def.ApplyEffect(new HookedEffect(duration: 9, hooker: att,
                    saveTarget: 1000, rng: new Random(0)), null, null);
            var zone = new Zone();
            zone.AddEntity(att, 5, 5);
            zone.AddEntity(def, 6, 5);
            return (att, def, zone);
        }

        // ════════════════════════════════════════════════════════════════
        // JSON CONTENT LOAD — every WSP6 skill blueprint is registered
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp6Skills_AllRegisteredInSkillRegistryFromJson()
        {
            // Force-load skill blueprints from Resources/Content/Data/Skills.
            // If any WSP6 skill's JSON entry has a typo / missing class /
            // wrong cost, this test fails — players couldn't see/buy the skill.
            SkillRegistry.EnsureInitialized();

            string[] wsp6PowerClasses = new[]
            {
                "Cudgel_Slam",
                "ShortBlades_Puncture",
                "ShortBlades_Shank",
                "Axe_Dismember",
                "Axe_Decapitate",
                "Axe_HookAndDrag",
            };

            foreach (var className in wsp6PowerClasses)
            {
                // WSP6 skills are POWERS (children of weapon-tree
                // tree-roots like CudgelSkill / AxeSkill / ShortBladesSkill),
                // so they live in `_powersByClass`, not `_skillsByClass`.
                // TryGetPowerByClass is the right query for power skills;
                // TryGetSkillByClass returns the tree-root skill.
                bool found = SkillRegistry.TryGetPowerByClass(className, out var power);
                Assert.IsTrue(found,
                    $"WSP6 power '{className}' must be registered in SkillRegistry " +
                    $"after loading content blueprints. If this fails, the JSON " +
                    $"entry's Class field is missing / typo'd / malformed.");
                Assert.AreEqual(1, power.Cost,
                    $"WSP6 weapon-tree power '{className}' must cost 1 SP per the " +
                    $"established convention.");
                Assert.IsFalse(string.IsNullOrEmpty(power.Description),
                    $"WSP6 power '{className}' must have a non-empty Description " +
                    $"so the player sees something in the skills menu.");

                // Sanity: HasEntry should ALSO see it (the union dictionary
                // is what BuySkillAction queries to validate purchases).
                Assert.IsTrue(SkillRegistry.HasEntry(className),
                    $"SkillRegistry.HasEntry('{className}') must return true so " +
                    $"BuySkillAction recognizes the power.");
            }
        }

        [Test]
        public void Wsp6ActiveAbilities_AllResolveToConcreteSkillTypeViaReflection()
        {
            // The string-overload of AddSkill resolves the class name via
            // reflection (SkillsPart.ResolveSkillType). If any WSP6 active
            // ability's class name doesn't resolve, the BuySkillAction
            // path fails silently — players can't actually grant the skill
            // even after "buying" it from the menu.
            SkillRegistry.EnsureInitialized();
            var skills = new SkillsPart();
            var actor = new Entity { ID = "test" };
            actor.AddPart(new RenderPart { DisplayName = "test" });
            actor.AddPart(skills);

            string[] wsp6Classes = new[]
            {
                "Cudgel_Slam", "ShortBlades_Puncture", "ShortBlades_Shank",
                "Axe_Dismember", "Axe_Decapitate", "Axe_HookAndDrag",
            };
            foreach (var className in wsp6Classes)
            {
                bool added = skills.AddSkill(className, source: "test");
                Assert.IsTrue(added,
                    $"AddSkill('{className}') via reflection-resolution must succeed. " +
                    $"If false, the class name doesn't resolve OR the AddSkill hook " +
                    $"returned false. Players who buy this skill would see no effect.");
                Assert.IsTrue(skills.HasSkill(className),
                    $"After AddSkill, HasSkill('{className}') must return true.");
                // Clean up so the next iteration's AddSkill isn't a duplicate.
                var instance = skills.GetSkill(className);
                skills.RemoveSkill(instance);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // ACTIVE-ABILITIES REGISTRATION — all 5 register their Guid
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AllActiveAbilities_AddSkillRegistersAbilityWithCorrectCommand()
        {
            // Each active-ability skill must, on AddSkill, register an
            // entry in ActivatedAbilitiesPart with the expected Command
            // string. If the Guid stays empty or the Command mismatches,
            // TryRouteSkillCommand can't find the ability.
            var actor = MakeFullActor("actor");
            var skills = actor.GetPart<SkillsPart>();
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();

            (BaseSkillPart skill, string expectedCommand, int expectedCooldown)[] cases = new[]
            {
                ((BaseSkillPart)new Cudgel_Conk(),    "CommandConk",        Cudgel_Conk.COOLDOWN),
                ((BaseSkillPart)new Cudgel_Slam(),    "CommandSlam",        Cudgel_Slam.COOLDOWN),
                // Axe_Berserk uses the prefixed form, matching Qud's
                // `Axe_Berserk.cs:122` AddMyActivatedAbility call. Other
                // skills (Conk, Slam, Shank, HookAndDrag) use the short
                // form, also Qud-faithful per their respective sources.
                ((BaseSkillPart)new Axe_Berserk(),    "CommandAxeBerserk",  Axe_Berserk.COOLDOWN),
                ((BaseSkillPart)new Axe_HookAndDrag(),"CommandHookAndDrag", Axe_HookAndDrag.COOLDOWN),
                ((BaseSkillPart)new ShortBlades_Shank(),"CommandShank",     ShortBlades_Shank.COOLDOWN),
            };

            foreach (var (skill, command, cd) in cases)
            {
                Assert.IsTrue(skills.AddSkill(skill, source: "test"),
                    $"AddSkill({skill.GetType().Name}) must succeed.");
                Assert.AreNotEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                    $"{skill.GetType().Name}.ActivatedAbilityID must be populated post-AddSkill.");
                var ability = abilities.GetAbility(skill.ActivatedAbilityID);
                Assert.IsNotNull(ability,
                    $"{skill.GetType().Name}'s registered ability must be in AbilityByGuid.");
                Assert.AreEqual(command, ability.Command,
                    $"{skill.GetType().Name}'s ability.Command must equal '{command}'.");
                Assert.AreEqual(cd, ability.MaxCooldown,
                    $"{skill.GetType().Name}'s MaxCooldown must equal COOLDOWN constant.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // TYPE_NEGATIVE BACKFILL — every flagged effect actually flags
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp616TypeNegativeBackfill_AllExpectedEffectsCarryTheFlag()
        {
            // Direct verification of the WSP6.16 backfill on every
            // negative effect class. If a future contributor accidentally
            // removes the GetEffectType override on any of these, Shank
            // (and any future TYPE_NEGATIVE consumer) silently misses
            // the effect — this test catches it.
            var negativeEffects = new Effect[]
            {
                new AcidicEffect(corrosion: 1.0f),
                new BleedingEffect(),
                new BrokenEffect(),
                new BurningEffect(),
                new CharredEffect(),
                new ConfusedEffect(),
                new ElectrifiedEffect(),
                new FrozenEffect(),
                new HobbledEffect(duration: 2),
                new ParalyzedEffect(),
                new PoisonedEffect(),
                new ShatterArmorEffect(),
                new StunnedEffect(),
                new HookedEffect(),  // WSP6.22 — added with the flag
            };
            foreach (var effect in negativeEffects)
            {
                Assert.IsTrue(effect.IsOfType(Effect.TYPE_NEGATIVE),
                    $"{effect.GetType().Name} must carry TYPE_NEGATIVE — " +
                    $"WSP6.16 backfill convention. If false, Shank silently misses it.");
            }
        }

        [Test]
        public void Wsp616TypeNegativeBackfill_PositiveEffectsDoNotCarryTheFlag()
        {
            // Counter-check: positive effects must NOT carry TYPE_NEGATIVE.
            // If Berserk (a self-buff) were flagged negative, Shank
            // would scale pen against the actor's own buffs — wrong.
            Assert.IsFalse(new BerserkEffect().IsOfType(Effect.TYPE_NEGATIVE),
                "BerserkEffect is a self-buff — must NOT carry TYPE_NEGATIVE.");
        }

        // ════════════════════════════════════════════════════════════════
        // SLAM → STUN → BLOCKED-ACTION chain (gameplay impact end-to-end)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void SlamStunsTarget_ThenStunBlocksActionOnRealBeginTakeAction()
        {
            // The full gameplay loop: actor slams target → target Stunned →
            // target's next BeginTakeAction is blocked by the Stun's
            // AllowAction()=false. This proves the Slam→Stun→
            // "skip-turn" chain is end-to-end functional via real events.
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            attacker.GetPart<SkillsPart>().AddSkill(new Cudgel_Slam(), source: "test");
            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Slam via the real dispatch path.
            attacker.GetPart<SkillsPart>().TryRouteSkillCommand("CommandSlam", zone, new Random(0));
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Pre-condition: Slam applied Stunned.");

            // Now fire BeginTakeAction on the defender — the StatusEffectsPart's
            // AllowAction loop should report blocked. Returns false from
            // FireEventAndRelease (the event is "Handled" — turn skipped).
            var begin = GameEvent.New("BeginTakeAction");
            begin.SetParameter("Zone", (object)zone);
            bool actionAllowed = defender.FireEventAndRelease(begin);

            Assert.IsFalse(actionAllowed,
                "Stunned defender's BeginTakeAction must return false (action blocked). " +
                "If true, the Stunned effect's AllowAction()=false isn't being honored " +
                "by the real turn-flow event — meaning Slam's Stun does nothing in-game.");

            // Sanity: the message-log carries the "cannot act" line.
            bool foundCannotAct = false;
            foreach (var msg in MessageLog.GetRecent(20))
                if (msg.Contains("cannot act")) foundCannotAct = true;
            Assert.IsTrue(foundCannotAct,
                "Expected a 'cannot act' message in the log when Stunned blocks the turn.");
        }

        // ════════════════════════════════════════════════════════════════
        // MULTI-SKILL COOLDOWN ISOLATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void TwoActiveAbilitiesOnSameActor_CooldownsAreIndependent()
        {
            // Actor with Slam + Conk. Using Slam should NOT cooldown Conk.
            // Using Conk should NOT cooldown Slam. This is an obvious-
            // sounding invariant that could easily break if the dispatcher
            // accidentally cooldowns the wrong ability (e.g., by Type
            // instead of Guid).
            var attacker = MakeFullActor("attacker");
            EquipPrimary(attacker, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var slam = new Cudgel_Slam();
            var conk = new Cudgel_Conk();
            attacker.GetPart<SkillsPart>().AddSkill(slam, source: "test");
            attacker.GetPart<SkillsPart>().AddSkill(conk, source: "test");
            var defender = MakeFullActor("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            var abilities = attacker.GetPart<ActivatedAbilitiesPart>();
            var slamAbil = abilities.GetAbility(slam.ActivatedAbilityID);
            var conkAbil = abilities.GetAbility(conk.ActivatedAbilityID);

            // Pre-condition: both usable.
            Assert.IsTrue(slamAbil.IsUsable && conkAbil.IsUsable,
                "Pre-condition: both abilities start usable.");

            // Use Slam.
            attacker.GetPart<SkillsPart>().TryRouteSkillCommand("CommandSlam", zone, new Random(0));
            Assert.IsFalse(slamAbil.IsUsable,
                "After Slam, Slam's cooldown must be set.");
            Assert.IsTrue(conkAbil.IsUsable,
                "After Slam, Conk's cooldown must NOT be set — cooldowns are per-ability, not per-actor.");

            // Reset defender position for Conk attempt.
            zone.MoveEntity(defender, 6, 5);

            // Use Conk while Slam still on cooldown.
            bool conkRouted = attacker.GetPart<SkillsPart>()
                .TryRouteSkillCommand("CommandConk", zone, new Random(0));
            Assert.IsTrue(conkRouted,
                "Conk should route successfully even while Slam is on cooldown — " +
                "the Slam-cooldown should NOT block other abilities.");
            Assert.IsFalse(conkAbil.IsUsable,
                "After Conk, Conk's cooldown must be set.");
        }

        // ════════════════════════════════════════════════════════════════
        // ADDSKILL DUPLICATE DETECTION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AddSkill_AlreadyOwned_ReturnsFalse_NoDoubleRegister()
        {
            // Buying a skill the player already owns must be a clean
            // no-op: AddSkill returns false, no duplicate ability slot,
            // no duplicate Part on the entity. If this fails, a player
            // who clicks "buy" twice ends up with two copies of the
            // skill — and on a Conk-style active, two ability slots
            // both pointing at the same Type but with different Guids
            // (bookkeeping nightmare).
            var actor = MakeFullActor("actor");
            EquipPrimary(actor, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var skills = actor.GetPart<SkillsPart>();
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();

            bool first = skills.AddSkill(new Cudgel_Slam(), source: "test");
            int abilityCountAfterFirst = abilities.AbilityList.Count;
            int skillCountAfterFirst = skills.SkillList.Count;

            bool second = skills.AddSkill(new Cudgel_Slam(), source: "test");

            Assert.IsTrue(first, "First AddSkill(Cudgel_Slam) must succeed.");
            Assert.IsFalse(second,
                "Second AddSkill(Cudgel_Slam) — duplicate by Type — must return false.");
            Assert.AreEqual(abilityCountAfterFirst, abilities.AbilityList.Count,
                "AbilityList count must NOT change on duplicate AddSkill.");
            Assert.AreEqual(skillCountAfterFirst, skills.SkillList.Count,
                "SkillList count must NOT change on duplicate AddSkill.");
        }

        // ════════════════════════════════════════════════════════════════
        // EFFECT CLEANUP WHEN TARGET DIES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookedTarget_DyingMidEffect_RemovesEffectCleanly()
        {
            // Target gets hooked, then takes lethal damage. The HookedEffect
            // shouldn't try to drag a dead-and-removed entity on the next
            // turn-end — this would NRE or leave the effect in a phantom
            // state. Real combat must handle "target dies while hooked."
            var attacker = MakeFullActor("attacker");
            var defender = MakeFullActor("defender", hp: 10);  // low HP for lethal
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 5, 9);

            var hook = new HookedEffect(duration: 9, hooker: attacker,
                saveTarget: 1000, rng: new Random(0));  // can't save out
            defender.ApplyEffect(hook, attacker, zone);
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "Pre-condition: HookedEffect applied.");

            // Kill the defender (e.g. via a burst of damage).
            CombatSystem.ApplyDamage(defender, amount: 1000, source: attacker, zone: zone);
            Assert.LessOrEqual(defender.GetStatValue("Hitpoints"), 0,
                "Pre-condition: defender's HP is 0 or below.");

            // Now fire EndTurn on the (now-dead) defender. Should NOT crash.
            // The HookedEffect's drag-toward-Hooker should bail safely
            // (target may have been removed from zone, or position is
            // invalid).
            Assert.DoesNotThrow(() => FireEndTurn(defender, zone),
                "EndTurn on a hooked-then-killed defender must not crash.");
        }
    }
}
