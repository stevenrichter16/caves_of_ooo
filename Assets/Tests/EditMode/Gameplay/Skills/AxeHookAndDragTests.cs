using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.22 — Axe_HookAndDrag active-ability tests + HookedEffect
    /// drag-on-turn-end tests.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item>Spec shape: DeclareActivatedAbility command + cooldown +
    ///         targeting.</item>
    ///   <item>OnCommand positive: piercing weapon + adjacent target →
    ///         (Hook) marker in log + HookedEffect applied.</item>
    ///   <item>Counter-checks: no Axe weapon / no target / null Rng /
    ///         null Zone — all bail safely.</item>
    ///   <item>HookedEffect.OnTurnEnd: drag toward Hooker when path is
    ///         clear; failed save → break free; null Hooker → break
    ///         free; cell blocked → no drag (counter-check); success
    ///         after multiple ticks → eventually adjacent.</item>
    ///   <item>HookedEffect carries TYPE_NEGATIVE so Shank counts it
    ///         (WSP6.16 backfill consistency check).</item>
    /// </list></para>
    /// </summary>
    public class AxeHookAndDragTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeBodiedCreature(string name = "creature",
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

        private static Entity MakeWeaponEntity(string name, string dice, string attributes)
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

        private static void EquipInPrimary(Entity actor, Entity weaponEntity)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
        }

        private static Entity MakeWall()
        {
            var w = new Entity { ID = "wall", BlueprintName = "wall" };
            w.Tags["Solid"] = "";
            w.Tags["Wall"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall" });
            return w;
        }

        // GameEvent that mimics what TurnManager passes to OnTurnEnd —
        // carries a Zone parameter so HookedEffect can do the drag.
        private static GameEvent MakeEndTurnContext(Zone zone)
        {
            var e = GameEvent.New("EndTurn");
            e.SetParameter("Zone", (object)zone);
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Axe_HookAndDrag — spec + counter-checks
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookAndDrag_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var hook = new Axe_HookAndDrag();
            var spec = hook.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandHookAndDrag", spec.Command);
            Assert.AreEqual(Axe_HookAndDrag.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
            Assert.AreEqual("Hook and Drag", spec.DisplayName);
        }

        [Test]
        public void HookAndDrag_WithoutAxeWeapon_FailsWithMessage()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker, MakeWeaponEntity("dagger", "1d4", "Piercing"));
            var hook = new Axe_HookAndDrag();
            attacker.GetPart<SkillsPart>().AddSkill(hook);

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            hook.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "Without an Axe weapon, HookAndDrag must not apply Hooked.");
        }

        [Test]
        public void HookAndDrag_WithNoAdjacentTarget_FailsWithMessage()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker, MakeWeaponEntity("axe", "1d8", "Cutting Axe"));
            var hook = new Axe_HookAndDrag();
            attacker.GetPart<SkillsPart>().AddSkill(hook);

            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            // No defender placed — actor alone.

            Assert.DoesNotThrow(() =>
                hook.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = new Random(0),
                }), "HookAndDrag with no adjacent target must not crash.");
            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("nothing to hook")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'nothing to hook' message in the log.");
        }

        [Test]
        public void HookAndDrag_WithNullRng_NoOps_NoCrash()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker, MakeWeaponEntity("axe", "1d8", "Cutting Axe"));
            var hook = new Axe_HookAndDrag();
            attacker.GetPart<SkillsPart>().AddSkill(hook);

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Assert.DoesNotThrow(() =>
                hook.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                }), "HookAndDrag with null Rng must not crash.");
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "HookAndDrag with null Rng must not apply Hooked.");
        }

        [Test]
        public void HookAndDrag_WithAxeAndAdjacentTarget_AppliesHookedEffect()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker, MakeWeaponEntity("axe", "1d8", "Cutting Axe"));
            var hook = new Axe_HookAndDrag();
            attacker.GetPart<SkillsPart>().AddSkill(hook);

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            hook.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<HookedEffect>(),
                "HookAndDrag must apply HookedEffect to the adjacent target.");
            // The Hooker reference must point back at the attacker.
            var hooked = defender.GetPart<StatusEffectsPart>().GetEffect<HookedEffect>();
            Assert.AreSame(attacker, hooked.Hooker,
                "HookedEffect.Hooker must reference the attacker, so subsequent drag ticks reel toward them.");
            // Marker visible in log.
            bool foundHookMarker = false;
            foreach (var msg in MessageLog.GetRecent(20))
                if (msg.Contains("(Hook)")) foundHookMarker = true;
            Assert.IsTrue(foundHookMarker,
                "Expected '(Hook)' attack-source marker in the log.");
        }

        // ════════════════════════════════════════════════════════════════
        // HookedEffect — drag-on-turn-end mechanics
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookedEffect_OnTurnEnd_DragsTargetTowardHookerWhenPathClear()
        {
            // Setup: hooker at (5,5), target at (5,9). Drag direction: N
            // (target is south of hooker; hooker is north of target).
            // After one OnTurnEnd, target should be at (5,8).
            //
            // Use a high SaveTarget so the strength-save can't break the
            // hook on this seed, isolating the drag-test.
            var hooker = MakeBodiedCreature("hooker");
            var target = MakeBodiedCreature("target", strength: 5);  // weak save
            var zone = new Zone();
            zone.AddEntity(hooker, 5, 5);
            zone.AddEntity(target, 5, 9);

            var hook = new HookedEffect(duration: 9, hooker, saveTarget: 100, rng: new Random(0));
            target.ApplyEffect(hook, hooker, zone);

            hook.OnTurnEnd(target, MakeEndTurnContext(zone));

            var pos = zone.GetEntityPosition(target);
            Assert.AreEqual((5, 8), (pos.x, pos.y),
                "HookedEffect.OnTurnEnd must drag target 1 cell toward Hooker. " +
                $"Target started at (5,9), expected (5,8) after drag, got ({pos.x},{pos.y}).");
        }

        [Test]
        public void HookedEffect_OnTurnEnd_FailedSave_RemovesEffect()
        {
            // Strength save WILL succeed (very low SaveTarget). Effect
            // should be marked for removal.
            var hooker = MakeBodiedCreature("hooker");
            var target = MakeBodiedCreature("target");
            var zone = new Zone();
            zone.AddEntity(hooker, 5, 5);
            zone.AddEntity(target, 5, 7);

            var hook = new HookedEffect(duration: 9, hooker, saveTarget: -100, rng: new Random(0));
            target.ApplyEffect(hook, hooker, zone);

            hook.OnTurnEnd(target, MakeEndTurnContext(zone));

            Assert.AreEqual(0, hook.Duration,
                "Successful save must zero the Duration so cleanup removes the effect next tick.");
            Assert.AreEqual(Effect.CAUSE_SAVE_SUCCEEDED, hook.LastRemovalCause,
                "LastRemovalCause must be CAUSE_SAVE_SUCCEEDED for save-out.");
        }

        [Test]
        public void HookedEffect_OnTurnEnd_NullHooker_RemovesEffect()
        {
            // If the hooker is null (died, left zone, never set), the
            // hook trivially breaks — no save needed.
            var target = MakeBodiedCreature("target");
            var zone = new Zone();
            zone.AddEntity(target, 5, 5);

            var hook = new HookedEffect(duration: 9, hooker: null,
                saveTarget: 100, rng: new Random(0));
            target.ApplyEffect(hook, source: null, zone: zone);

            hook.OnTurnEnd(target, MakeEndTurnContext(zone));

            Assert.AreEqual(0, hook.Duration,
                "Null Hooker must zero the Duration (no Hooker to drag toward).");
        }

        [Test]
        public void HookedEffect_OnTurnEnd_DragBlockedByWall_NoMove()
        {
            // Target at (5,9), hooker at (5,5), wall at (5,8). Drag
            // direction is North (5,8), but wall is there — no drag.
            // Failed save (SaveTarget=100) keeps the effect alive.
            var hooker = MakeBodiedCreature("hooker");
            var target = MakeBodiedCreature("target", strength: 5);
            var zone = new Zone();
            zone.AddEntity(hooker, 5, 5);
            zone.AddEntity(target, 5, 9);
            zone.AddEntity(MakeWall(), 5, 8);

            var hook = new HookedEffect(duration: 9, hooker, saveTarget: 100, rng: new Random(0));
            target.ApplyEffect(hook, hooker, zone);

            hook.OnTurnEnd(target, MakeEndTurnContext(zone));

            var pos = zone.GetEntityPosition(target);
            Assert.AreEqual((5, 9), (pos.x, pos.y),
                "Drag blocked by wall must leave target in place. " +
                $"Got ({pos.x},{pos.y}).");
            // Effect remains active for next turn (Duration decremented but > 0).
            Assert.Greater(hook.Duration, 0,
                "Drag-blocked tick still decrements duration but doesn't remove the effect.");
        }

        [Test]
        public void HookedEffect_OnTurnEnd_AdjacentTarget_NoMove()
        {
            // Already-adjacent target: no drag attempt (the hook is
            // already as reeled-in as it gets). Effect persists; just
            // duration decrements.
            var hooker = MakeBodiedCreature("hooker");
            var target = MakeBodiedCreature("target", strength: 5);
            var zone = new Zone();
            zone.AddEntity(hooker, 5, 5);
            zone.AddEntity(target, 6, 5);  // adjacent

            var hook = new HookedEffect(duration: 9, hooker, saveTarget: 100, rng: new Random(0));
            target.ApplyEffect(hook, hooker, zone);

            int durBefore = hook.Duration;
            hook.OnTurnEnd(target, MakeEndTurnContext(zone));

            var pos = zone.GetEntityPosition(target);
            Assert.AreEqual((6, 5), (pos.x, pos.y),
                "Already-adjacent target must not be dragged onto the Hooker's cell.");
            Assert.AreEqual(durBefore - 1, hook.Duration,
                "Adjacent-no-drag tick still decrements duration.");
        }

        // ════════════════════════════════════════════════════════════════
        // HookedEffect — TYPE_NEGATIVE flag (WSP6.16 consistency)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HookedEffect_HasTypeNegative_SoShankCounts()
        {
            // Sanity-check the TYPE_NEGATIVE flag. Without it, Shank
            // wouldn't recognize Hooked as a debuff worth scaling pen
            // against — breaking the natural "hook then shank" combo.
            var hook = new HookedEffect(duration: 9);
            Assert.IsTrue(hook.IsOfType(Effect.TYPE_NEGATIVE),
                "HookedEffect must carry TYPE_NEGATIVE so Shank counts it as a debuff.");
        }
    }
}
