using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.1 — Cudgel_Slam active-ability tests.
    /// Pins the simplified-Qud-port mechanic: target is pushed up to
    /// <see cref="Cudgel_Slam.SLAM_DISTANCE"/> cells in the slam direction,
    /// stopping at any solid cell. Wall-hits add bonus weapon damage and
    /// scale the stun duration.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item>Positive: clear-cell push, partial push (mid-path wall),
    ///         immediate-wall (no movement), distance-cap.</item>
    ///   <item>Counter-checks: no Cudgel weapon, no adjacent target,
    ///         null Zone, null Rng (defense-in-depth, mirrors WSP4.4 fix
    ///         on <see cref="Cudgel_Conk"/>).</item>
    ///   <item>Spec shape: DeclareActivatedAbility returns expected
    ///         command + cooldown + targeting mode.</item>
    /// </list></para>
    /// </summary>
    public class CudgelSlamTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror SkillActiveAbilityBehaviorTests') ────

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

        private static Entity MakeWeaponEntity(string name, string dice,
            string attributes)
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

        private static Entity MakeWall(string name = "wall")
        {
            var w = new Entity { ID = name, BlueprintName = name };
            w.Tags["Solid"] = "";
            w.Tags["Wall"] = "";
            w.AddPart(new RenderPart { DisplayName = name });
            return w;
        }

        private static (Entity attacker, Entity defender, Zone zone, Cudgel_Slam slam)
            MakeSlamFixture(int defenderHp = 50)
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var slam = new Cudgel_Slam();
            attacker.GetPart<SkillsPart>().AddSkill(slam, source: "test");

            var defender = MakeBodiedCreature("defender", hp: defenderHp);
            var zone = new Zone();
            return (attacker, defender, zone, slam);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape — DeclareActivatedAbility returns the expected spec
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var slam = new Cudgel_Slam();
            var spec = slam.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec, "Slam must declare a non-null spec.");
            Assert.AreEqual("CommandSlam", spec.Command,
                "Slam's command must be 'CommandSlam' (the input dispatcher key).");
            Assert.AreEqual(Cudgel_Slam.COOLDOWN, spec.Cooldown,
                "Slam's cooldown must match the COOLDOWN constant (50T per Qud).");
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode,
                "Slam targets an adjacent cell (player picks which side).");
            Assert.AreEqual("Slam", spec.DisplayName,
                "Slam's display name should be 'Slam'.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: clear-cell push moves target SLAM_DISTANCE cells away
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_PushesTargetThroughClearCells_FullSlamDistance()
        {
            // Setup: actor at (5,5), defender at (6,5) (East), all cells
            // (7,5), (8,5), (9,5) clear. Slam should push target East
            // by SLAM_DISTANCE cells (3) and end at (9,5).
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var newPos = zone.GetEntityPosition(defender);
            Assert.AreEqual((9, 5), (newPos.x, newPos.y),
                "Defender should be pushed East SLAM_DISTANCE cells (3) when path is clear.");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Slam must apply Stunned even with no wall hits (floor of 1 turn).");
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "No wall hits → no bonus damage. Defender HP unchanged.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: target slammed into adjacent wall takes damage + stun
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_BlockedByImmediateWall_AppliesDamageAndStun()
        {
            // Setup: actor at (5,5), defender at (6,5), wall at (7,5).
            // Slam can't push the defender any cells (wall behind them
            // immediately) → wallHits=1, cellsPushed=0.
            // Bonus damage from one weapon-roll fires, stun duration ≥ 1.
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            zone.AddEntity(MakeWall(), 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var newPos = zone.GetEntityPosition(defender);
            Assert.AreEqual((6, 5), (newPos.x, newPos.y),
                "Defender shouldn't move when there's a wall immediately behind.");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Wall slam must apply Stunned.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Wall hit must apply bonus weapon-roll damage (HP < pre-slam HP).");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: partial push — moves until blocked by mid-path wall
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_PartialPush_StopsAtMidPathWall()
        {
            // Setup: actor at (5,5), defender at (6,5), clear at (7,5),
            // wall at (8,5). Defender pushed 1 cell to (7,5), then
            // blocked → cellsPushed=1, wallHits=1.
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            zone.AddEntity(MakeWall(), 8, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var newPos = zone.GetEntityPosition(defender);
            Assert.AreEqual((7, 5), (newPos.x, newPos.y),
                "Defender should be pushed 1 cell East before hitting the wall at (8,5).");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Partial-push slam must still apply Stunned.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "1 wall hit → 1 weapon-roll damage applied.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: SLAM_DISTANCE cap — long open corridor doesn't
        // push beyond SLAM_DISTANCE cells even if more clear cells exist
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_OpenCorridor_StopsAtSlamDistance()
        {
            // Setup: actor at (1,5), defender at (2,5), clear corridor
            // all the way to (10,5). Defender should ONLY move
            // SLAM_DISTANCE cells (3) → end at (5,5), not at (10,5).
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 1, 5);
            zone.AddEntity(defender, 2, 5);

            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var newPos = zone.GetEntityPosition(defender);
            int xDelta = newPos.x - 2;
            Assert.AreEqual(Cudgel_Slam.SLAM_DISTANCE, xDelta,
                "Defender should be pushed exactly SLAM_DISTANCE cells, even with more clear cells available.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: actor without a Cudgel weapon equipped
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_WithoutCudgelWeapon_FailsWithMessage_NoMovement()
        {
            // Setup: actor has Cudgel_Slam owned but a LongSword equipped
            // (Cutting LongBlades — NOT Cudgel). Slam must NOT push the
            // defender and must NOT apply Stunned.
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("sword", "1d8", "Cutting LongBlades"));
            var slam = new Cudgel_Slam();
            attacker.GetPart<SkillsPart>().AddSkill(slam, source: "test");

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var pos = zone.GetEntityPosition(defender);
            Assert.AreEqual((6, 5), (pos.x, pos.y),
                "Without a Cudgel weapon, Slam must not move the defender.");
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Without a Cudgel weapon, Slam must not apply Stunned.");
            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("cudgel")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'needs cudgel' fail message in the log.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: actor with no adjacent target
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_WithNoAdjacentTarget_NoOps_NoStun_NoCrash()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var slam = new Cudgel_Slam();
            attacker.GetPart<SkillsPart>().AddSkill(slam, source: "test");

            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            // No defender placed — actor is alone.

            Assert.DoesNotThrow(() =>
                slam.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = new Random(0),
                }), "Slam with no adjacent target must not crash.");
            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("nothing to slam")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'nothing to slam' fail message in the log.");
        }

        // ════════════════════════════════════════════════════════════════
        // Defense-in-depth (mirrors WSP4.4 Conk fix): null Rng / null Zone
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_WithNullRng_NoOps_NoCrash()
        {
            // Mirrors Cudgel_Conk's WSP4.4 🟡 #2 fix: Rng=null is the
            // determinism short-circuit, not a wall-clock fallback. Must
            // not crash; must not apply Stunned (since OnCommand bails
            // before doing any work).
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Assert.DoesNotThrow(() =>
                slam.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                }), "Slam with Rng=null must not crash.");

            var pos = zone.GetEntityPosition(defender);
            Assert.AreEqual((6, 5), (pos.x, pos.y),
                "Slam with Rng=null must short-circuit before pushing the target.");
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Slam with Rng=null must not apply Stunned.");
        }

        [Test]
        public void Slam_WithNullZone_NoOps_NoCrash()
        {
            // Slam needs Zone for adjacency lookup + push movement. If
            // Zone is null, OnCommand should bail safely instead of NREing.
            // Mirrors the Conk pattern (cold-eye 🔵 #7).
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var slam = new Cudgel_Slam();
            attacker.GetPart<SkillsPart>().AddSkill(slam, source: "test");

            Assert.DoesNotThrow(() =>
                slam.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = null, Rng = new Random(0),
                }), "Slam with Zone=null must not crash.");
        }

        // ════════════════════════════════════════════════════════════════
        // Stun duration shape: cellsPushed + wallHits, capped at MAX
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_StunDuration_ClampedToMaxStunDuration()
        {
            // Setup: actor at (5,5), defender at (6,5), wall at (10,5).
            // Push 3 cells (SLAM_DISTANCE) without hitting any wall (the
            // wall at (10,5) is past the slam range).
            // Expected stun duration = min(MAX, 3 + 0) = 3.
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            zone.AddEntity(MakeWall(), 10, 5); // out of slam range

            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var stunned = defender.GetPart<StatusEffectsPart>()
                .GetEffect<StunnedEffect>();
            Assert.IsNotNull(stunned, "Stunned must be applied.");
            // Duration ≥ 1 (we pushed 3 cells); ≤ MAX_STUN_DURATION.
            Assert.GreaterOrEqual(stunned.Duration, 1,
                $"Stun duration must be ≥ 1. Got {stunned.Duration}.");
            Assert.LessOrEqual(stunned.Duration, Cudgel_Slam.MAX_STUN_DURATION,
                $"Stun duration must be ≤ MAX_STUN_DURATION ({Cudgel_Slam.MAX_STUN_DURATION}). " +
                $"Got {stunned.Duration}.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: another creature in the push path blocks the slam
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_BlockedByAdjacentCreature_AppliesDamageAndStun()
        {
            // Setup: actor at (5,5), defender at (6,5), bystander
            // creature at (7,5). Slam push tries to move defender to
            // (7,5) — but the bystander is there. Cell.IsSolid() returns
            // false (creatures don't carry the Solid TAG, just
            // PhysicsPart.Solid), so without the creature-check the
            // slam would happily move defender on top of the bystander.
            // With the creature-check, the slam stops at (6,5),
            // wallHits=1, and bonus damage + stun fire.
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            var bystander = MakeBodiedCreature("bystander");
            zone.AddEntity(bystander, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var pos = zone.GetEntityPosition(defender);
            Assert.AreEqual((6, 5), (pos.x, pos.y),
                "Defender must NOT be pushed onto a cell with another creature.");
            var bystanderPos = zone.GetEntityPosition(bystander);
            Assert.AreEqual((7, 5), (bystanderPos.x, bystanderPos.y),
                "Bystander stays in place — Slam doesn't chain through creatures (CoO v1 simplification).");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Creature-blocked slam still applies Stunned to the slammed target.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Creature-blocked slam still rolls bonus damage on the slammed target.");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: edge of map counts as a wall hit
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Slam_TargetAtMapEdge_PushAttemptCountsAsWallHit()
        {
            // Setup: actor at (78,5), defender at (79,5) (east edge of
            // the default 80-wide zone). Slam east tries to push to (80,5)
            // which is off-map → counts as a wall hit (bonus damage +
            // stun applied), defender doesn't move.
            var (attacker, defender, zone, slam) = MakeSlamFixture();
            zone.AddEntity(attacker, 78, 5);
            zone.AddEntity(defender, 79, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            slam.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            var pos = zone.GetEntityPosition(defender);
            Assert.AreEqual((79, 5), (pos.x, pos.y),
                "Defender at the map edge can't be pushed off the map.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Off-map push attempt must count as a wall hit (damage applied).");
            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Off-map push must still apply Stunned.");
        }
    }
}
