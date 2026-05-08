using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.2 — Acrobatics_Tumble active-ability tests.
    /// Pins the "swap positions with adjacent creature" mechanic. Hostile
    /// targets get Confused 1T; allies get a clean swap. Distinct from
    /// every other active in the table — Tumble is the only ability
    /// that exchanges cells.
    /// </summary>
    public class AcrobaticsTumbleTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror CudgelSlamTests) ─────────────────────

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 50)
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

        private static (Entity actor, Zone zone, Acrobatics_Tumble tumble)
            MakeTumbleFixture()
        {
            var actor = MakeBodiedCreature("actor");
            var tumble = new Acrobatics_Tumble();
            actor.GetPart<SkillsPart>().AddSkill(tumble, source: "test");
            var zone = new Zone();
            return (actor, zone, tumble);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape — no weapon class required (Acrobatics is martial-arts-y)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var t = new Acrobatics_Tumble();
            var spec = t.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandTumble", spec.Command);
            Assert.AreEqual(Acrobatics_Tumble.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
            Assert.AreEqual("Tumble", spec.DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: positions swap (hostile target → confused, actor +
        // target end up in each other's cells)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_OnHostileTarget_SwapsPositionsAndConfuses()
        {
            var (actor, zone, tumble) = MakeTumbleFixture();
            var hostile = MakeBodiedCreature("hostile");
            // No "Ally" tag → hostile by Tumble's heuristic.
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(hostile, 6, 5);

            tumble.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(42),
            });

            var actorPos = zone.GetEntityPosition(actor);
            var hostilePos = zone.GetEntityPosition(hostile);
            Assert.AreEqual((6, 5), (actorPos.x, actorPos.y),
                "Actor must end up where hostile was (swap).");
            Assert.AreEqual((5, 5), (hostilePos.x, hostilePos.y),
                "Hostile must end up where actor was (swap).");
            Assert.IsTrue(hostile.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                "Hostile target must pick up ConfusedEffect on Tumble.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: ally swap — positions swap, but no Confused applied
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_OnAlly_SwapsPositions_NoConfused()
        {
            var (actor, zone, tumble) = MakeTumbleFixture();
            var ally = MakeBodiedCreature("ally");
            ally.Tags["Ally"] = ""; // marked as ally
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(ally, 6, 5);

            tumble.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(42),
            });

            var actorPos = zone.GetEntityPosition(actor);
            var allyPos = zone.GetEntityPosition(ally);
            Assert.AreEqual((6, 5), (actorPos.x, actorPos.y),
                "Actor must swap with ally too (positional swap is universal).");
            Assert.AreEqual((5, 5), (allyPos.x, allyPos.y));
            Assert.IsFalse(ally.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                "Ally must NOT pick up ConfusedEffect — that's hostile-only.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: no adjacent creature → no swap
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_NoAdjacentCreature_NoSwap()
        {
            var (actor, zone, tumble) = MakeTumbleFixture();
            var farTarget = MakeBodiedCreature("far");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(farTarget, 15, 15); // far away

            tumble.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(42),
            });

            var actorPos = zone.GetEntityPosition(actor);
            var farPos = zone.GetEntityPosition(farTarget);
            Assert.AreEqual((5, 5), (actorPos.x, actorPos.y),
                "Actor must not move when no creature is adjacent.");
            Assert.AreEqual((15, 15), (farPos.x, farPos.y),
                "Far target must stay put.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: no weapon required (Acrobatics is unarmed)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_NoWeaponEquipped_StillWorks()
        {
            // Tumble does NOT gate on a weapon class — Acrobatics's
            // identity is "I move well" not "I swing X." Verify the
            // ability fires even with no weapon.
            var (actor, zone, tumble) = MakeTumbleFixture();
            var hostile = MakeBodiedCreature("hostile");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(hostile, 6, 5);
            // Note: no EquipInPrimary call — actor is unarmed.

            tumble.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor,
                Zone = zone, Rng = new Random(42),
            });

            Assert.AreEqual((6, 5), zone.GetEntityPosition(actor),
                "Tumble must work without a weapon — Acrobatics is unarmed.");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: null Rng / null Zone
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Tumble_WithNullRng_NoOps_NoCrash()
        {
            var (actor, zone, tumble) = MakeTumbleFixture();
            var hostile = MakeBodiedCreature("hostile");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(hostile, 6, 5);

            Assert.DoesNotThrow(() =>
            {
                tumble.OnCommand(new SkillEventContext
                {
                    Attacker = actor, Defender = actor,
                    Zone = zone, Rng = null,
                });
            });
            Assert.AreEqual((5, 5), zone.GetEntityPosition(actor),
                "Null-Rng Tumble must not move the actor.");
        }

        [Test]
        public void Tumble_WithNullZone_NoOps_NoCrash()
        {
            var (actor, _, tumble) = MakeTumbleFixture();
            Assert.DoesNotThrow(() =>
            {
                tumble.OnCommand(new SkillEventContext
                {
                    Attacker = actor, Defender = actor,
                    Zone = null, Rng = new Random(42),
                });
            });
        }
    }
}
