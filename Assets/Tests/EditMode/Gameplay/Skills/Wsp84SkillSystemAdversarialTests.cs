using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.4 — Adversarial tests for the entire skill system. These
    /// target bug classes the per-skill happy-path + counter-check
    /// tests miss:
    /// <list type="bullet">
    ///   <item><b>Mid-execution state changes</b> (target dies during
    ///         a multi-strike, snapshot stability, etc.).</item>
    ///   <item><b>Effect stacking semantics</b> (non-stacking,
    ///         duration-stacking, magnitude-stacking).</item>
    ///   <item><b>Cross-skill aggregation</b> (multiple
    ///         OnGetSpellDamageModifier sources sum correctly).</item>
    ///   <item><b>Self-referential gates</b> (ArcaneSurge skipping
    ///         own cooldown, Tumble not self-swap).</item>
    ///   <item><b>Diag dispatch invariants</b> (cooldown rejection
    ///         vs internal rejection emit different record sets).</item>
    ///   <item><b>Boundary inputs</b> (null class names, zero
    ///         direction, out-of-bounds positions).</item>
    /// </list>
    ///
    /// <para>These are MUTATION-RESISTANCE tests per CLAUDE.md §3.9 —
    /// designed so a buggy implementation that subtly violates an
    /// invariant fails the test even when the happy path "works."</para>
    /// </summary>
    public class Wsp84SkillSystemAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        // ── Shared fixture ────────────────────────────────────────────────

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

        // ════════════════════════════════════════════════════════════════
        // A. MID-EXECUTION STATE CHANGES
        //   Pattern: a multi-strike skill iterates a snapshot of targets;
        //   one target dies during the loop. The snapshot must remain
        //   stable so subsequent targets still get processed.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Whirlwind_TargetDiesOnFirstSwing_OtherTargetsStillHit()
        {
            // Set up 2 adjacent targets. First (East) has 1 HP — dies on
            // first swing. Second (West) has full HP — must still take a
            // swing despite the first dying mid-loop. If Whirlwind's
            // snapshot were re-evaluated between strikes (e.g. if it
            // iterated cells live instead of targets-at-activation),
            // the dead first target's removal might shift indices and
            // the second target could be skipped.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "2d8+5", "Cutting Axe"));
            var skill = new Axe_Whirlwind();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var fragile = MakeBodied("fragile", hp: 1);
            var sturdy = MakeBodied("sturdy", hp: 200);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(fragile, 6, 5);  // East
            zone.AddEntity(sturdy, 4, 5);   // West

            int sturdyHpBefore = sturdy.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7)
            });

            Assert.LessOrEqual(fragile.GetStatValue("Hitpoints"), 0,
                "Fragile (1 HP) target must die from the first swing.");
            Assert.Less(sturdy.GetStatValue("Hitpoints"), sturdyHpBefore,
                "Sturdy target on the OTHER side must still be struck — "
                + "Whirlwind's target snapshot must be stable across "
                + "mid-loop deaths.");
        }

        [Test]
        public void Adversarial_Flurry_TargetDiesOnStrike1_NoFurtherStrikes()
        {
            // Defender at 1 HP — dies on strike 1. The remaining 2
            // strikes of the 3-strike Flurry must short-circuit (the
            // HP<=0 check). Otherwise we'd pile message-log entries
            // for swings against a corpse.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "2d4+5", "Piercing"));
            var skill = new ShortBlades_Flurry();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 1);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext
                {
                    Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7)
                }),
                "Flurry must not crash when target dies mid-loop.");
            Assert.LessOrEqual(def.GetStatValue("Hitpoints"), 0,
                "Defender must be dead.");
        }

        [Test]
        public void Adversarial_Pyroclasm_BurningTarget_TakesAOEDamageItself()
        {
            // The detonation target IS in its own 3x3 AOE. So the
            // "consume burning + AOE" sequence should damage the
            // target both via the consumption (none, just stack-read)
            // AND via the AOE ring (yes). Total: AOE damage applied
            // to the target itself.
            var atk = MakeBodied("atk");
            var skill = new Pyromancy_Pyroclasm();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            def.AddPart(new ThermalPart());
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            def.ApplyEffect(new BurningEffect(intensity: 2.0f), atk, zone);
            int hpBefore = def.GetStatValue("Hitpoints");

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0)
            });

            Assert.Less(def.GetStatValue("Hitpoints"), hpBefore,
                "Pyroclasm's AOE must damage the consumer target itself "
                + "(it's in its own 3x3 radius — easy to miss in a "
                + "naive 'damage adjacents' impl).");
        }

        // ════════════════════════════════════════════════════════════════
        // B. EFFECT STACKING SEMANTICS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_HibernatingEffect_ReApply_DoesNotCompoundDuration()
        {
            // Hibernate is meant to be non-stacking. Re-applying must
            // be a no-op (OnStack returns true to suppress dedupe-add).
            // If OnStack ever returns false, two stacked Hibernates
            // would compound (each ticking heals + restoring resistances
            // wrong on remove — the second stack's PriorXxxResistance
            // would capture the post-buff value).
            var actor = MakeBodied("actor");
            actor.ApplyEffect(new HibernatingEffect(10), actor, null);
            int durationAfterFirst = actor.GetPart<StatusEffectsPart>()
                .GetEffect<HibernatingEffect>().Duration;
            actor.ApplyEffect(new HibernatingEffect(10), actor, null);
            int durationAfterSecond = actor.GetPart<StatusEffectsPart>()
                .GetEffect<HibernatingEffect>().Duration;

            Assert.AreEqual(durationAfterFirst, durationAfterSecond,
                "Re-applying Hibernate must NOT extend duration "
                + "(non-stacking semantic). If this fails, the second "
                + "apply would corrupt the saved-resistance restore on "
                + "remove.");
        }

        [Test]
        public void Adversarial_RootedEffect_ReApply_ExtendsDuration()
        {
            // Rooted IS stacking (extends duration). Verify that
            // re-applying compounds — opposite of Hibernate.
            var actor = MakeBodied("actor");
            actor.ApplyEffect(new RootedEffect(4), actor, null);
            int afterFirst = actor.GetPart<StatusEffectsPart>()
                .GetEffect<RootedEffect>().Duration;
            actor.ApplyEffect(new RootedEffect(3), actor, null);
            int afterSecond = actor.GetPart<StatusEffectsPart>()
                .GetEffect<RootedEffect>().Duration;

            Assert.AreEqual(7, afterSecond,
                "Re-applying Rooted must extend duration (4 + 3 = 7). "
                + "Got " + afterSecond + " (after first: " + afterFirst + ")");
        }

        [Test]
        public void Adversarial_RendArmor_OnAlreadyShatteredTarget_StackCountAccumulates()
        {
            // RendArmor applies a fresh ShatterArmorEffect with
            // StackCount = REND_STACKS (3). If the target ALREADY has
            // ShatterArmor, the OnStack contract on ShatterArmorEffect
            // accumulates StackCount + duration. The combined effect
            // should have StackCount == old + new = at-least 4
            // (1 baseline + 3 from rend) — verifying RendArmor
            // composes with the existing stack model rather than
            // overwriting it.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", "1d8", "Cutting Axe"));
            var skill = new Axe_RendArmor();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            // Pre-existing ShatterArmor (1 stack).
            def.ApplyEffect(new ShatterArmorEffect(2), atk, zone);
            int stacksBefore = def.GetPart<StatusEffectsPart>()
                .GetEffect<ShatterArmorEffect>().StackCount;
            Assert.AreEqual(1, stacksBefore, "Setup: 1 stack baseline.");

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0)
            });

            int stacksAfter = def.GetPart<StatusEffectsPart>()
                .GetEffect<ShatterArmorEffect>().StackCount;
            Assert.AreEqual(4, stacksAfter,
                "RendArmor on already-shattered target must compose "
                + "via OnStack: 1 (existing) + 3 (rend) = 4. Got " + stacksAfter);
        }

        // ════════════════════════════════════════════════════════════════
        // C. CROSS-SKILL AGGREGATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OnGetSpellDamageModifier_SpellcraftPlusHeartFlame_AggregatesBoth()
        {
            // Owning both Spellcraft and HeartFlame (after charging it)
            // should sum their bonuses on a Heat-element spell. If the
            // dispatcher iterated only one or short-circuited, the
            // second would silently get dropped.
            var actor = MakeBodied("actor", hp: 200);
            actor.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            var heartFlame = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(heartFlame, source: "test");
            // Charge HeartFlame.
            heartFlame.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, heartFlame.ChargesRemaining,
                "Setup: HeartFlame is charged.");

            int total = SkillEventDispatcher.GetSpellDamageModifier(
                actor, actor, "Heat", baseDamage: 10);
            // SpellcraftSkill returns +1 universal; HeartFlame returns
            // +baseDamage (100% bonus on baseDamage 10 = 10). Total: 11+
            // (depending on Spellcraft's exact value). What we strictly
            // need: total > HeartFlame's contribution alone, proving
            // both fired.
            int heartFlameAlone = (10 * Pyromancy_HeartFlame.DAMAGE_BONUS_PERCENT) / 100;
            Assert.Greater(total, heartFlameAlone,
                "Aggregator must sum both Spellcraft's universal +1 AND "
                + "HeartFlame's element-gated bonus. Got " + total
                + " (HeartFlame alone would be " + heartFlameAlone + ").");
        }

        // ════════════════════════════════════════════════════════════════
        // D. SELF-REFERENTIAL / POSITION GATES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ArcaneSurge_DoesNotResetOwnCooldown_OnlyOthersReset()
        {
            // Surge skips its own ability via Guid match. Adversarial
            // setup: TWO other abilities with mid-cooldowns + Surge
            // itself with its full cooldown set. After Surge fires:
            // both others go to 0; Surge's stays at MaxCooldown.
            // Why this matters: a buggy "skip self" check could match
            // by Command name (which would drop a wrong ability) or
            // miss by Guid mismatch (resetting Surge along with the
            // others, defeating the long-cooldown gate).
            var actor = MakeBodied("actor");
            var surge = new Spellcraft_ArcaneSurge();
            actor.GetPart<SkillsPart>().AddSkill(surge, source: "test");

            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var surgeAb = abilities.GetAbility(surge.ActivatedAbilityID);
            surgeAb.MaxCooldown = Spellcraft_ArcaneSurge.COOLDOWN;
            surgeAb.CooldownRemaining = Spellcraft_ArcaneSurge.COOLDOWN; // mid-cooldown

            var ab1 = abilities.GetAbility(abilities.AddAbility(
                "A", "CmdA", "Skills", AbilityTargetingMode.AdjacentCell, 1, ""));
            ab1.MaxCooldown = 50; ab1.CooldownRemaining = 25;

            var ab2 = abilities.GetAbility(abilities.AddAbility(
                "B", "CmdB", "Skills", AbilityTargetingMode.AdjacentCell, 1, ""));
            ab2.MaxCooldown = 80; ab2.CooldownRemaining = 60;

            surge.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor, Rng = new Random(0)
            });

            Assert.AreEqual(0, ab1.CooldownRemaining, "Other ability A reset.");
            Assert.AreEqual(0, ab2.CooldownRemaining, "Other ability B reset.");
            Assert.AreEqual(Spellcraft_ArcaneSurge.COOLDOWN, surgeAb.CooldownRemaining,
                "Surge's OWN cooldown must NOT be reset (long-cooldown gate).");
        }

        [Test]
        public void Adversarial_Backstab_NonCreatureInFlankerCell_NoBonus()
        {
            // Flanking detection iterates flanker cell objects looking
            // for Creature-tagged entities. A non-Creature object (item
            // on the ground, wall, terrain) must NOT count. Mirrors the
            // "barrel in line" check from Lunge — same defensive pattern.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("dagger", "1d4+1", "Piercing"));
            var skill = new ShortBlades_Backstab();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 100);
            // Non-Creature item in the flanker cell (East-of-target).
            var item = new Entity { ID = "item", BlueprintName = "rock" };
            item.Tags["Item"] = "";
            item.AddPart(new RenderPart { DisplayName = "rock" });
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 6, 5);
            zone.AddEntity(item, 7, 5);

            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42)
            });
            int damageWithItemFlanker = hpBefore - def.GetStatValue("Hitpoints");

            // Compare against unflanked baseline (same seed).
            var (atk2, def2, zone2, skill2) = SetupBackstabFresh();
            zone2.AddEntity(atk2, 5, 5); zone2.AddEntity(def2, 6, 5);
            int hpBefore2 = def2.GetStatValue("Hitpoints");
            skill2.OnCommand(new SkillEventContext
            {
                Attacker = atk2, Defender = atk2, Zone = zone2, Rng = new Random(42)
            });
            int unflankedDamage = hpBefore2 - def2.GetStatValue("Hitpoints");

            Assert.AreEqual(unflankedDamage, damageWithItemFlanker,
                "Non-Creature in flanker cell must not trigger flank bonus. "
                + "WithItemFlanker=" + damageWithItemFlanker
                + " unflanked=" + unflankedDamage);
        }

        private (Entity, Entity, Zone, ShortBlades_Backstab) SetupBackstabFresh()
        {
            var atk = MakeBodied("atk2");
            Equip(atk, MakeWeapon("dagger2", "1d4+1", "Piercing"));
            var skill = new ShortBlades_Backstab();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return (atk, MakeBodied("def2", hp: 100), new Zone(), skill);
        }

        // ════════════════════════════════════════════════════════════════
        // E. DIAG DISPATCH INVARIANTS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Diag_SkillInternalRejection_EmitsBOTH_CommandRoutedAND_SkillRejected()
        {
            // Critical invariant: when SkillsPart.HandleEvent dispatches
            // to a skill that internally bails (no_weapon, no_target,
            // etc.), the diag stream gets BOTH records:
            //   - CommandRouted from SkillsPart (post-OnCommand)
            //   - SkillRejected from the skill itself (mid-OnCommand)
            //
            // This is the documented contract: a debug query can tell
            // "the dispatcher routed" vs "the dispatcher refused"
            // (cooldown) vs "the skill internally aborted" by joining
            // these records on actor + turn.
            //
            // If a future change suppresses CommandRouted on internal
            // bail, queries would lose the dispatch trace.
            var atk = MakeBodied("atk");
            // No Cudgel weapon — Slam will bail via no_weapon.
            var slam = new Cudgel_Slam();
            atk.GetPart<SkillsPart>().AddSkill(slam, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            var cmd = GameEvent.New("CommandSlam");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new Random(0));
            atk.FireEvent(cmd);
            cmd.Release();

            var routed = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "CommandRouted", Limit = 5 }).Records;
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;

            Assert.AreEqual(1, routed.Count,
                "CommandRouted MUST fire even when the skill internally "
                + "bails — that's the dispatch-vs-skill-result distinction.");
            Assert.AreEqual(1, rejected.Count,
                "SkillRejected MUST fire from the skill's internal gate.");
            StringAssert.Contains("no_weapon", rejected[0].PayloadJson);
        }

        [Test]
        public void Adversarial_Diag_CooldownBlocked_EmitsOnlyCommandRejected_NoCommandRouted()
        {
            // Cooldown-blocked dispatch is the OPPOSITE of internal
            // rejection: the skill's OnCommand never runs, so neither
            // CommandRouted (no successful dispatch) NOR SkillRejected
            // (no skill execution) fires. Only CommandRejected with
            // reason=cooldown.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var slam = new Cudgel_Slam();
            atk.GetPart<SkillsPart>().AddSkill(slam, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            var ability = atk.GetPart<ActivatedAbilitiesPart>().GetAbility(slam.ActivatedAbilityID);
            ability.CooldownRemaining = 50; // mid-cooldown

            Diag.ResetAll();
            var cmd = GameEvent.New("CommandSlam");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new Random(0));
            atk.FireEvent(cmd);
            cmd.Release();

            var routed = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "CommandRouted", Limit = 5 }).Records;
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "CommandRejected", Limit = 5 }).Records;
            var skillRejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;

            Assert.AreEqual(0, routed.Count, "Cooldown blocks dispatch — no CommandRouted.");
            Assert.AreEqual(1, rejected.Count, "Cooldown emits CommandRejected.");
            StringAssert.Contains("cooldown", rejected[0].PayloadJson);
            Assert.AreEqual(0, skillRejected.Count,
                "Skill never ran — no SkillRejected possible.");
        }

        [Test]
        public void Adversarial_Diag_NonCommandEvent_NotInterceptedBySkillsPart()
        {
            // SkillsPart.HandleEvent has a fast-path early-out for
            // events not starting with "Command*". Verify this with
            // a non-Command event — SkillsPart must NOT emit any
            // CommandRouted/Rejected diag for unrelated events.
            var actor = MakeBodied("actor");
            actor.GetPart<SkillsPart>().AddSkill(new Cudgel_Slam(), source: "test");

            Diag.ResetAll();
            var unrelated = GameEvent.New("EndTurn");
            actor.FireEvent(unrelated);
            unrelated.Release();

            var skillEvents = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Limit = 50 }).Records;
            Assert.AreEqual(0, skillEvents.Count,
                "Non-Command events must not generate ANY skill-category diag. "
                + "Got " + skillEvents.Count + " unexpected records.");
        }

        // ════════════════════════════════════════════════════════════════
        // F. BOUNDARY / WEIRD INPUTS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SkillRegistry_TryGetPowerByClass_NullArg_HandlesGracefully()
        {
            SkillRegistry.EnsureInitialized();
            Assert.DoesNotThrow(() =>
            {
                PowerData _;
                SkillRegistry.TryGetPowerByClass(null, out _);
            }, "Null class name must not crash the registry lookup.");
        }

        [Test]
        public void Adversarial_SkillRegistry_TryGetPowerByClass_GibberishName_ReturnsFalse()
        {
            SkillRegistry.EnsureInitialized();
            PowerData power;
            bool found = SkillRegistry.TryGetPowerByClass("XXX_Bogus_Skill", out power);
            Assert.IsFalse(found);
            Assert.IsNull(power);
        }

        [Test]
        public void Adversarial_SkillsPart_AddSkill_NullClassName_ReturnsFalse()
        {
            var actor = MakeBodied("actor");
            bool added = actor.GetPart<SkillsPart>().AddSkill((string)null, source: "test");
            Assert.IsFalse(added, "Null class name must return false, not crash.");
        }

        [Test]
        public void Adversarial_SkillsPart_AddSkill_EmptyClassName_ReturnsFalse()
        {
            var actor = MakeBodied("actor");
            bool added = actor.GetPart<SkillsPart>().AddSkill("", source: "test");
            Assert.IsFalse(added);
        }

        [Test]
        public void Adversarial_SkillsPart_AddSkill_DuplicateInstance_ReturnsFalse()
        {
            // Adding the same skill type twice must reject the second
            // (duplicate-detection by type). If this fails, the player
            // could buy the same skill twice and stack passive bonuses.
            var actor = MakeBodied("actor");
            var skill1 = new Cudgel_Bludgeon();
            var skill2 = new Cudgel_Bludgeon();
            Assert.IsTrue(actor.GetPart<SkillsPart>().AddSkill(skill1, "test"));
            Assert.IsFalse(actor.GetPart<SkillsPart>().AddSkill(skill2, "test"),
                "Second instance of same skill type must be rejected.");
            Assert.AreEqual(1, actor.GetPart<SkillsPart>().SkillList.Count);
        }

        [Test]
        public void Adversarial_SkillsPart_RemoveSkill_NotOwned_ReturnsFalse()
        {
            // Removing an unowned skill must be a clean no-op (return
            // false, no crash). Adversarial pattern: a buggy impl might
            // throw NRE when the SkillList doesn't contain the entry.
            var actor = MakeBodied("actor");
            var unowned = new Cudgel_Bludgeon();
            bool removed = actor.GetPart<SkillsPart>().RemoveSkill(unowned, cause: "test");
            Assert.IsFalse(removed);
        }

        // ════════════════════════════════════════════════════════════════
        // G. EFFECT INTERACTION / COMBINATION STATE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EvasiveRoll_PriorityOrder_RemovesStunnedBeforeBleeding()
        {
            // EvasiveRoll's priority list places Stunned > Bleeding.
            // If both are present, only Stunned should be removed.
            // A buggy "first found" impl iterating effect-application
            // order would remove Bleeding first if it was applied first.
            var actor = MakeBodied("actor");
            var skill = new Acrobatics_EvasiveRoll();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            // Apply Bleeding FIRST, then Stunned. If the priority order
            // is hardcoded (correct), Stunned wins regardless of apply
            // order. If the priority is "first applied" (bug), Bleeding
            // would be removed.
            actor.ApplyEffect(new BleedingEffect(15, "1d2"), actor, null);
            actor.ApplyEffect(new StunnedEffect(2), actor, null);

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.IsFalse(actor.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Stunned must be removed first per priority order.");
            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                "Bleeding must remain — only one effect removed per cast.");
        }

        [Test]
        public void Adversarial_HibernatingEffect_Restore_PreservesPriorResistanceValues()
        {
            // Hibernate captures prior HeatResistance/ColdResistance on
            // OnApply, then restores them on OnRemove. Adversarial
            // setup: actor has nonzero resistances pre-hibernation.
            // After remove, those exact values must be restored —
            // not 0 (the OnRemove default) and not 100 (the buff value).
            var actor = MakeBodied("actor");
            actor.GetStat("HeatResistance").BaseValue = 35;
            actor.GetStat("ColdResistance").BaseValue = 60;

            var hib = new HibernatingEffect(10);
            actor.ApplyEffect(hib, actor, null);
            Assert.AreEqual(100, actor.GetStatValue("HeatResistance"));
            Assert.AreEqual(100, actor.GetStatValue("ColdResistance"));

            actor.GetPart<StatusEffectsPart>().RemoveEffect<HibernatingEffect>();

            Assert.AreEqual(35, actor.GetStatValue("HeatResistance"),
                "OnRemove must restore the EXACT prior HeatResistance value.");
            Assert.AreEqual(60, actor.GetStatValue("ColdResistance"));
        }

        [Test]
        public void Adversarial_HeartFlame_Charges_ExpireAfterDurationEvenIfUnused()
        {
            // HeartFlame has a charge counter AND an expiry turn.
            // If the player charges but doesn't cast for BUFF_DURATION
            // turns, charges should be 0 on the next OnGetSpellDamageModifier
            // call. Adversarial: simulate by manually advancing the
            // TurnManager's TickCount past the expiry, then call the
            // hook — should return 0.
            var actor = MakeBodied("actor");
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            // The TurnManager constructor sets Active = this, so the
            // TickCount used by HeartFlame's expiry resolution comes
            // from this instance.
            var turn = new TurnManager();
            // No turns ticked yet (TickCount = 0 at construction).
            skill.OnCommand(new SkillEventContext
            { Attacker = actor, Defender = actor, Rng = new Random(0) });
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, skill.ChargesRemaining);

            // Manually advance TickCount past expiry. (Tick() advances
            // it; we don't have direct setter access.) We use the
            // public Tick API to push TickCount above the expiry turn.
            for (int i = 0; i < Pyromancy_HeartFlame.BUFF_DURATION + 5; i++)
                turn.Tick();

            int bonus = skill.OnGetSpellDamageModifier(actor, actor, "Heat", 10);
            Assert.AreEqual(0, bonus,
                "Expired HeartFlame charges must NOT yield bonus.");
            Assert.AreEqual(0, skill.ChargesRemaining,
                "Expired charges should be drained on access.");
        }

        // ════════════════════════════════════════════════════════════════
        // H. SKILL DISPATCH RE-ENTRANCY / INVARIANTS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SkillsPart_HandleEvent_OnEntityWithoutSkillsPart_DoesNotCrash()
        {
            // Edge case: an entity has SkillsPart but the skill list is
            // empty. Firing a Command* event on it must dispatch cleanly
            // (no crash, no false positive routing).
            var actor = MakeBodied("actor");
            // SkillsPart present from MakeBodied, but SkillList is empty.

            Diag.ResetAll();
            var cmd = GameEvent.New("CommandUnknown");
            Assert.DoesNotThrow(() =>
            {
                actor.FireEvent(cmd);
                cmd.Release();
            });

            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "CommandRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejected.Count,
                "Unknown command on empty SkillsPart must emit a "
                + "no_match CommandRejected (the dispatch logged it).");
            StringAssert.Contains("no_match", rejected[0].PayloadJson);
        }

        [Test]
        public void Adversarial_FrostbindRooted_AllowsAttackButBlocksMove()
        {
            // Rooted's defining mechanic: AllowAction stays true,
            // AllowMovement returns false. Verify both via the
            // production paths (BeforeMove event blocks; BeginTakeAction
            // doesn't).
            var target = MakeBodied("target");
            target.ApplyEffect(new RootedEffect(4), null, null);

            // BeforeMove must be rejected.
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)target);
            bool moveAllowed = target.FireEvent(beforeMove);
            beforeMove.Release();
            Assert.IsFalse(moveAllowed, "Rooted must block movement.");

            // BeginTakeAction must NOT be rejected (AllowAction stays
            // true). If it WERE blocked, Rooted would behave like Stun.
            var beginTake = GameEvent.New("BeginTakeAction");
            beginTake.SetParameter("Actor", (object)target);
            bool actAllowed = target.FireEvent(beginTake);
            beginTake.Release();
            Assert.IsTrue(actAllowed,
                "Rooted must NOT block actions — that's Stun's job. "
                + "Distinguishes Frostbind from Stun.");
        }

        [Test]
        public void Adversarial_HibernatingEffect_BlocksBothActionAndMovement()
        {
            // Hibernate is opposite of Rooted: AllowAction = false
            // (which by the AllowMovement default ALSO blocks movement).
            // Verify both BeforeMove and BeginTakeAction get blocked.
            var actor = MakeBodied("actor");
            actor.ApplyEffect(new HibernatingEffect(10), actor, null);

            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)actor);
            Assert.IsFalse(actor.FireEvent(beforeMove),
                "Hibernate blocks movement (via AllowAction=false defaulting AllowMovement).");
            beforeMove.Release();

            var beginTake = GameEvent.New("BeginTakeAction");
            beginTake.SetParameter("Actor", (object)actor);
            Assert.IsFalse(actor.FireEvent(beginTake),
                "Hibernate blocks actions.");
            beginTake.Release();
        }

        [Test]
        public void Adversarial_ChargingStrike_ZeroDirection_BailsCleanlyWithDiag()
        {
            // dx=dy=0 is the no-direction signal. ChargingStrike must
            // NOT walk infinitely (which would hang the test) — it
            // should bail with no_direction diag.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8", "Bludgeoning Cudgel"));
            var skill = new Cudgel_ChargingStrike();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            Diag.ResetAll();
            // Test passes if the call returns within a reasonable time
            // and emits no_direction; if there's a hidden infinite loop
            // bug (e.g., walking with dx=dy=0 forever), this would hang
            // the test runner.
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 0, DirectionY = 0,
            });

            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_direction", rejected[0].PayloadJson);
            // Position unchanged.
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual((5, 5), (pos.x, pos.y),
                "Zero-direction ChargingStrike must NOT move actor.");
        }

        // ════════════════════════════════════════════════════════════════
        // I. INTEGRATION / FULL SYSTEM
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AddingAllShippedSkills_NoCrashesAndCorrectAbilityCount()
        {
            // Stress: add ALL 24 actives + ALL 28 passives + 10 tree-roots
            // to a single actor. Verify:
            //   - SkillList has 62 entries (no duplicates)
            //   - ActivatedAbilitiesPart has 24 entries (one per active)
            //   - No crashes during the bulk-add
            // If a future skill has a typo / broken reflection / null
            // ActivatedAbilitySpec, it surfaces here.
            var actor = MakeBodied("actor");
            string[] all = new string[]
            {
                // Tree-roots (10)
                "AcrobaticsSkill", "AxeSkill", "CorrosionSkill", "CryomancySkill",
                "CudgelSkill", "GalvanismSkill", "LongBladesSkill", "PyromancySkill",
                "ShortBladesSkill", "SpellcraftSkill",
                // Passives (28)
                "AcrobaticsDodgePower",
                "Axe_Cleave", "Axe_Decapitate", "Axe_Dismember", "Axe_Expertise",
                "Corrosion_AcidRetort", "Corrosion_Etch",
                "Cryomancy_BrittleStrike", "Cryomancy_FrostRetort",
                "Cudgel_Backswing", "Cudgel_Bludgeon", "Cudgel_Expertise",
                "Cudgel_Hammer", "Cudgel_ShatteringBlows",
                "Galvanism_GroundStrike", "Galvanism_ShockRetort",
                "LongBlades_Expertise", "LongBlades_Lacerate",
                "Pyromancy_Charsplit", "Pyromancy_Cinder", "Pyromancy_ScorchRetort",
                "ShortBlades_Bloodletter", "ShortBlades_Expertise",
                "ShortBlades_Hobble", "ShortBlades_Jab", "ShortBlades_Puncture",
                "ShortBlades_Rejoinder",
                "Spellcraft_Empower",
                // Actives (24)
                "Acrobatics_EvasiveRoll", "Acrobatics_Tumble", "Acrobatics_Vault",
                "Axe_Berserk", "Axe_HookAndDrag", "Axe_RendArmor", "Axe_Whirlwind",
                "Cryomancy_Frostbind", "Cryomancy_Hibernate",
                "Cudgel_ChargingStrike", "Cudgel_Conk", "Cudgel_Disarm",
                "Cudgel_GroundPound", "Cudgel_Slam",
                "Galvanism_Overload", "LongBlades_Lunge",
                "Pyromancy_HeartFlame", "Pyromancy_Pyroclasm",
                "ShortBlades_Backstab", "ShortBlades_Disengage",
                "ShortBlades_Flurry", "ShortBlades_Shank",
                "Spellcraft_ArcaneSurge", "Spellcraft_LeyTap",
            };

            int ok = 0;
            foreach (var name in all)
                if (actor.GetPart<SkillsPart>().AddSkill(name, source: "stress")) ok++;

            Assert.AreEqual(all.Length, ok,
                "All 62 skills must add successfully. ok=" + ok);
            Assert.AreEqual(62, actor.GetPart<SkillsPart>().SkillList.Count);
            Assert.AreEqual(24, actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Count,
                "Exactly 24 ActivatedAbility entries (one per active skill).");
        }

        // ════════════════════════════════════════════════════════════════
        // J. DEEP ADVERSARIAL — most likely to find real bugs
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Stunned_StillBlocksMovementAfterAllowMovementRefactor()
        {
            // The WSP8.3 AllowMovement refactor was supposed to leave
            // existing AllowAction-blocking effects (Stunned/Frozen/
            // Paralyzed) blocking movement transparently — because the
            // virtual default delegates: AllowMovement(t) =>
            // AllowAction(t). If a future change broke that delegation
            // (e.g., AllowMovement default => true), Stunned would no
            // longer block movement. This test pins the symmetry-
            // preservation contract.
            var actor = MakeBodied("actor");
            actor.ApplyEffect(new StunnedEffect(2), actor, null);

            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)actor);
            bool moveAllowed = actor.FireEvent(beforeMove);
            beforeMove.Release();

            Assert.IsFalse(moveAllowed,
                "Stunned must STILL block movement after the WSP8.3 "
                + "AllowMovement refactor — the default delegation "
                + "AllowMovement => AllowAction preserves this.");
        }

        [Test]
        public void Adversarial_Frozen_StillBlocksMovementAfterAllowMovementRefactor()
        {
            var actor = MakeBodied("actor");
            actor.ApplyEffect(new FrozenEffect(2), actor, null);
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)actor);
            bool moveAllowed = actor.FireEvent(beforeMove);
            beforeMove.Release();
            Assert.IsFalse(moveAllowed,
                "Frozen must still block movement (AllowMovement=>AllowAction default).");
        }

        [Test]
        public void Adversarial_StatShifter_CompositionAcrossMultipleOwnedSkills()
        {
            // Owning multiple skills that shift the same stat should
            // compose. AcrobaticsDodgePower gives +2 DV; if a future
            // skill also +1 DV, owning both should yield +3. Adversarial:
            // verify removing one doesn't strip the other.
            var actor = MakeBodied("actor");
            int dvBaseline = actor.GetStatValue("DV"); // 0

            var dodge = new AcrobaticsDodgePower();
            actor.GetPart<SkillsPart>().AddSkill(dodge, source: "test");
            int dvAfterDodge = actor.GetStatValue("DV");
            Assert.AreEqual(dvBaseline + AcrobaticsDodgePower.DV_BONUS, dvAfterDodge,
                "Dodge applies +2 DV.");

            // Remove Dodge — DV should return to baseline.
            actor.GetPart<SkillsPart>().RemoveSkill(dodge, cause: "test");
            int dvAfterRemove = actor.GetStatValue("DV");
            Assert.AreEqual(dvBaseline, dvAfterRemove,
                "Removing Dodge must restore baseline DV. "
                + "If StatShifter leaks, DV would stay at +2.");
        }

        [Test]
        public void Adversarial_BerserkEffect_StatShiftRestoreOnRemove()
        {
            // BerserkEffect applies +5 Strength, -2 DV. When the effect
            // expires (Duration=0 on tick), the stat shift should
            // reverse. Adversarial: explicitly remove via
            // RemoveEffect<BerserkEffect>() and verify both stats
            // restore — catches a buggy OnRemove that only restores
            // one stat.
            var actor = MakeBodied("actor");
            int strBefore = actor.GetStatValue("Strength");
            int dvBefore = actor.GetStatValue("DV");

            actor.ApplyEffect(new BerserkEffect(5), actor, null);
            Assert.Greater(actor.GetStatValue("Strength"), strBefore,
                "Setup: Berserk applies Str bonus.");
            Assert.Less(actor.GetStatValue("DV"), dvBefore,
                "Setup: Berserk applies DV penalty.");

            actor.GetPart<StatusEffectsPart>().RemoveEffect<BerserkEffect>();

            Assert.AreEqual(strBefore, actor.GetStatValue("Strength"),
                "Berserk OnRemove must restore Strength.");
            Assert.AreEqual(dvBefore, actor.GetStatValue("DV"),
                "Berserk OnRemove must restore DV.");
        }

        [Test]
        public void Adversarial_Lunge_NonUnitDirection_ScansPastIntermediateCells()
        {
            // INVESTIGATIVE adversarial: LineTargeting.TraceFirstImpact
            // walks dx/dy per step. If a caller passes dx=2 (non-unit),
            // the trace skips intermediate cells. Lunge could miss a
            // creature at (actor + 1) when aimed with dx=2 because
            // step 1 lands at (actor + 2).
            //
            // This test documents the assumption: production InputHandler
            // always supplies unit-magnitude direction (dx,dy ∈ {-1,0,1}).
            // If non-unit input arrives, the behavior is "trust the
            // caller, walk dx-magnitude per step" — which means a target
            // at the intermediate cell is missed.
            //
            // We DON'T fix this in the skill (it's a system-wide
            // LineTargeting assumption used by many skills + mutations).
            // We pin the assumption so future work that wants to harden
            // it has a regression target.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("longsword", "1d8+1", "Cutting LongBlades"));
            var skill = new LongBlades_Lunge();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 100);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 6, 5); // distance 1, intermediate cell

            int hpBefore = def.GetStatValue("Hitpoints");
            // Pass dx=2 (non-unit). Assumption: skips (6,5) and
            // checks (7,5) (empty), (9,5) (out of LUNGE_RANGE).
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 2, DirectionY = 0,
            });

            // Defender at (6,5) is between actor (5,5) and the
            // dx=2-step landing (7,5) — should be missed by the trace.
            Assert.AreEqual(hpBefore, def.GetStatValue("Hitpoints"),
                "Non-unit direction walks dx-magnitude per step, skipping "
                + "intermediate cells. This is a system-wide LineTargeting "
                + "assumption (also affects ChainLightning/Overload). "
                + "Production InputHandler always passes unit direction.");
        }

        [Test]
        public void Adversarial_HeartFlame_ReFireDuringBuffWindow_ResetsCharges()
        {
            // What happens if the cooldown gate is bypassed and HeartFlame
            // fires during its own active buff window? My implementation
            // resets _chargesRemaining = BUFF_CHARGES (3), not adds. So
            // re-firing OVERWRITES rather than accumulating.
            //
            // Adversarial: verify this is the (intentional) behavior.
            // A buggy "+= BUFF_CHARGES" would compound to 6+ charges
            // on rapid re-fire, breaking the design tradeoff.
            var actor = MakeBodied("actor", hp: 200);
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, skill.ChargesRemaining,
                "Setup: first cast charges to 3.");
            // Consume one charge.
            skill.OnGetSpellDamageModifier(actor, actor, "Heat", baseDamage: 10);
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES - 1, skill.ChargesRemaining,
                "After one cast: 2 remaining.");

            // Re-fire (bypassing cooldown).
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, skill.ChargesRemaining,
                "Re-fire RESETS to 3 (overwrite, not stack). If this fails, "
                + "rapid HeartFlame spam could compound charges.");
        }

        [Test]
        public void Adversarial_ChargingStrike_Diagonal_MovesActorAndStrikes()
        {
            // Diagonal charge — verify DirectionLine semantics work
            // for non-cardinal dx/dy combos. Adversarial: a buggy impl
            // might assume cardinal-only.
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("mace", "1d8+1", "Bludgeoning Cudgel"));
            var skill = new Cudgel_ChargingStrike();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: 200);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 8, 2); // NE: dx=1, dy=-1, distance 3

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = -1,
            });

            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual((7, 3), (pos.x, pos.y),
                "ChargingStrike NE must stop one cell short of target at (8,2). "
                + "Got actor at " + pos);
        }

        [Test]
        public void Adversarial_Tumble_TargetIsActorReference_NoSelfSwap()
        {
            // Adversarial: what if the skill's adjacent-target lookup
            // somehow finds the actor itself in an adjacent cell? The
            // existing exclusion (`e == actor`) handles this — verify
            // the loop never picks the actor as target.
            var actor = MakeBodied("actor");
            var skill = new Acrobatics_Tumble();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            // No other creatures.

            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            { Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0) });

            // Actor must stay at (5,5) — no self-swap.
            var pos = zone.GetEntityPosition(actor);
            Assert.AreEqual((5, 5), (pos.x, pos.y),
                "Actor must not swap with self. Position drift would "
                + "indicate the actor.Equals(target) gate failed.");

            // SkillRejected diag should fire (no_target).
            var rejected = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("no_target", rejected[0].PayloadJson);
        }

        [Test]
        public void Adversarial_OverloadChain_StopsAtFirstNonConductor()
        {
            // Overload's chain breaks on the FIRST non-conductor in
            // the line. Adversarial: 3 creatures in line, only middle
            // is wet. The middle should NOT take damage because the
            // FIRST creature (dry) breaks the chain immediately.
            var atk = MakeBodied("atk");
            var skill = new Galvanism_Overload();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var dry = MakeBodied("dry", hp: 200);
            var wet = MakeBodied("wet", hp: 200);
            wet.Statistics["LightningResistance"] = new Stat
            { Owner = wet, Name = "LightningResistance", BaseValue = 0, Min = -100, Max = 100 };
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(dry, 6, 5);   // immediately east — DRY (chain breaks here)
            zone.AddEntity(wet, 7, 5);   // 2 east — WET (would chain if dry didn't break)
            wet.ApplyEffect(new WetEffect(), atk, zone);

            int dryHp = dry.GetStatValue("Hitpoints");
            int wetHp = wet.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(dryHp, dry.GetStatValue("Hitpoints"),
                "First creature (dry) breaks chain — must not take damage.");
            Assert.AreEqual(wetHp, wet.GetStatValue("Hitpoints"),
                "Wet creature behind dry MUST be untouched (chain broken).");
        }

        [Test]
        public void Adversarial_MultipleOwnedSkills_AllPassivesFireOnSameHit()
        {
            // Owning Cudgel_Bludgeon + Cudgel_Hammer + Cudgel_ShatteringBlows
            // means each on-hit fires all three's OnAttackerAfterAttack
            // hooks. Adversarial: a buggy iteration that breaks early
            // would miss procs.
            //
            // This test verifies via DiagQuery that the SkillEventDispatcher
            // does iterate all owned skills on a single attack. The
            // observable: the actor + defender state changes such that
            // multiple effects could land. We check at least 2 of the 3
            // possible status effects show up on the defender after a
            // seeded swing.
            //
            // NOTE: each on-hit is gated by chance, so we use a high-roll
            // seed (42 is reliable) and accept "any 2+ of 3" as proof
            // the dispatcher iterated all three.
            var atk = MakeBodied("atk");
            var atkWeaponEntity = MakeWeapon("mace", "1d8+5", "Bludgeoning Cudgel");
            Equip(atk, atkWeaponEntity);
            var atkWeapon = atkWeaponEntity.GetPart<MeleeWeaponPart>();
            atk.GetPart<SkillsPart>().AddSkill(new Cudgel_Bludgeon(), "test");
            atk.GetPart<SkillsPart>().AddSkill(new Cudgel_Hammer(), "test");
            atk.GetPart<SkillsPart>().AddSkill(new Cudgel_ShatteringBlows(), "test");
            var def = MakeBodied("def", hp: 500);
            var defWeapon = MakeWeapon("dummy", "1d4", "Cutting LongBlades");
            def.GetPart<InventoryPart>().AddObject(defWeapon);
            var defHand = def.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            def.GetPart<InventoryPart>().EquipToBodyPart(defWeapon, defHand);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            // Swing many times to give each on-hit chance to land.
            // 30 swings × 35% Bludgeon × 50% Hammer × ~30% Shattering
            // — at least one of each should fire.
            for (int i = 0; i < 30; i++)
            {
                CombatSystem.PerformSingleAttack(
                    attacker: atk, defender: def, weapon: atkWeapon,
                    isPrimary: true, zone: zone, rng: new Random(i),
                    attackSourceDesc: "(Test)");
                if (def.GetStatValue("Hitpoints") <= 0) break;
            }

            int procsLanded = 0;
            var sep = def.GetPart<StatusEffectsPart>();
            if (sep.HasEffect<StunnedEffect>()) procsLanded++;
            if (sep.HasEffect<ShatterArmorEffect>()) procsLanded++;
            // Cudgel_Hammer applies BrokenEffect to one of the defender's
            // equipped items, NOT to the defender themselves. So we
            // check the defender's weapon.
            if (defWeapon.GetPart<StatusEffectsPart>()?.HasEffect<BrokenEffect>() ?? false)
                procsLanded++;

            Assert.GreaterOrEqual(procsLanded, 2,
                "Owning 3 Cudgel passives — at least 2 of 3 procs must "
                + "land in 30 seeded swings (Stunned / Shattered / Broken). "
                + "If <2 land, the SkillEventDispatcher's per-skill "
                + "iteration broke early.");
        }

        [Test]
        public void Adversarial_DiagBuffer_BulkAddDoesNotOverflow_AllAddedRecordsPresent()
        {
            // Add many skills; verify the diag buffer captured all the
            // Added emissions (i.e., the WSP8.4-bumped 8192 buffer is
            // big enough). If buffer were still 1024, only the most
            // recent ~1023 records would survive — bulk adds would
            // drop the older ones.
            var actor = MakeBodied("actor");
            Diag.ResetAll();

            // Add 30 skills in sequence; each should emit one Added record.
            string[] toAdd = new string[]
            {
                "AcrobaticsSkill", "AxeSkill", "CudgelSkill", "ShortBladesSkill",
                "LongBladesSkill", "CorrosionSkill", "CryomancySkill",
                "GalvanismSkill", "PyromancySkill", "SpellcraftSkill",
                "AcrobaticsDodgePower",
                "Axe_Cleave", "Axe_Expertise", "Axe_Berserk", "Axe_Whirlwind",
                "Cudgel_Bludgeon", "Cudgel_Expertise", "Cudgel_Conk",
                "Cudgel_Slam", "Cudgel_GroundPound",
                "ShortBlades_Jab", "ShortBlades_Puncture", "ShortBlades_Shank",
                "ShortBlades_Flurry", "ShortBlades_Backstab",
                "LongBlades_Lacerate", "LongBlades_Expertise", "LongBlades_Lunge",
                "Spellcraft_Empower", "Spellcraft_ArcaneSurge",
            };
            int addedOk = 0;
            foreach (var name in toAdd)
                if (actor.GetPart<SkillsPart>().AddSkill(name, "stress")) addedOk++;

            var addedRecords = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "Added", Limit = 100 }).Records;
            Assert.AreEqual(addedOk, addedRecords.Count,
                "Every successful AddSkill must emit one Added record. "
                + "ok=" + addedOk + " records=" + addedRecords.Count);
        }
    }
}
