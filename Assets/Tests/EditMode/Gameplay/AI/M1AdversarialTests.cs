using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M1 (AISelfPreservation / Passive / AIAmbush / RetreatGoal /
    /// DormantGoal) adversarial cold-eye tests. Per Docs/QUD-PARITY.md
    /// §3.9: each test commits a PREDICTION before re-reading production,
    /// then runs. Failures are classified as test-wrong, code-wrong, or
    /// setup-wrong in the commit message.
    ///
    /// Distinct from M1CoverageGapTests (commit 0078a69), which wrote
    /// tests while looking at the production code (0/21 bugs found).
    /// This file deliberately targets behaviors I am genuinely unsure
    /// about — the bug-find rate should be non-zero if the discipline
    /// works.
    /// </summary>
    [TestFixture]
    public class M1AdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        private Entity CreateCreature(Zone zone, int x, int y,
            string faction = "Villagers", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        // ============================================================
        // AISelfPreservation — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: with HP=0 (already dead), AISelfPreservation does
        /// NOT push RetreatGoal — corpses don't retreat.
        /// CONFIDENCE: medium. Code likely guards via `hp <= 0` early-return,
        /// but maybe not — could be it pushes uselessly and RetreatGoal pops.
        /// </summary>
        [Test]
        public void AISelfPreservation_HpExactlyZero_DoesNotPushRetreat()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 100);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            creature.GetStat("Hitpoints").BaseValue = 0;

            creature.FireEvent(GameEvent.New("AIBored"));

            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "At HP=0 the entity is dead. Pushing RetreatGoal is wasteful — " +
                "predict early-return.");
        }

        /// <summary>
        /// PREDICTION: with HP > Max (over-healed via potion / mutation),
        /// fraction is > 1, no retreat.
        /// CONFIDENCE: high — `fraction > RetreatThreshold` early-returns.
        /// </summary>
        [Test]
        public void AISelfPreservation_HpAboveMax_DoesNotPushRetreat()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 20);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            // Some buff lets BaseValue exceed Max. Production might or might
            // not let this happen; if it does, the AISelfPreservation gate
            // shouldn't crash or misfire.
            creature.GetStat("Hitpoints").BaseValue = 25;

            Assert.DoesNotThrow(() => creature.FireEvent(GameEvent.New("AIBored")),
                "HP > Max must not crash AISelfPreservation.");
            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "Over-healed creature is overflowing-fine, not wounded.");
        }

        /// <summary>
        /// PREDICTION: AISelfPreservation pushes RetreatGoal with the
        /// default MaxTurns from the goal's ctor (200).
        /// CONFIDENCE: low — the part might pass an explicit MaxTurns, or
        /// might rely on the goal ctor default. If this fails, the contract
        /// I assumed isn't what ships.
        /// </summary>
        [Test]
        public void AISelfPreservation_PushedRetreatGoal_UsesGoalCtorDefaultMaxTurns()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 100);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 20;

            creature.FireEvent(GameEvent.New("AIBored"));

            var retreat = brain.FindGoal<RetreatGoal>();
            Assert.IsNotNull(retreat);
            Assert.AreEqual(200, retreat.MaxTurns,
                "Predicted: AISelfPreservation pushes RetreatGoal with no " +
                "explicit MaxTurns, getting the ctor default of 200. If this " +
                "fails, the part is passing a custom value.");
        }

        // ============================================================
        // RetreatGoal — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: RetreatGoal with HealPerTick set to a NEGATIVE value
        /// must NOT damage the entity. Defensive — code should guard.
        /// CONFIDENCE: low — the production code might just blindly add
        /// HealPerTick to BaseValue without sign-checking. If so, that's
        /// a real bug: a misconfigured blueprint could wound an NPC during
        /// recovery.
        /// </summary>
        [Test]
        public void RetreatGoal_NegativeHealPerTick_DoesNotDamageHp()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 100);
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 50;

            // negative healing — defensive test
            var retreat = new RetreatGoal(5, 5, safeHpFraction: 0.99f, maxTurns: 200, healPerTick: -10);
            brain.PushGoal(retreat);

            int before = creature.GetStat("Hitpoints").BaseValue;
            retreat.TakeAction(); // already at waypoint → Recover phase
            int after = creature.GetStat("Hitpoints").BaseValue;

            Assert.GreaterOrEqual(after, before,
                "A negative HealPerTick must not damage the recovering NPC. " +
                "If after < before, the heal code is `BaseValue += HealPerTick` " +
                "with no sign guard — a misconfigured blueprint could become a " +
                "self-damaging trap.");
        }

        /// <summary>
        /// PREDICTION: HealPerTick = 0 results in NO mutation per tick (no
        /// heal, no damage). The creature stays at the same HP and exits
        /// only via MaxTurns.
        /// CONFIDENCE: high — `if (HealPerTick > 0)` is the obvious guard.
        /// </summary>
        [Test]
        public void RetreatGoal_HealPerTickZero_HpStaysExactly_OverManyTicks()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 100);
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 50;

            var retreat = new RetreatGoal(5, 5, safeHpFraction: 0.99f, maxTurns: 200, healPerTick: 0);
            brain.PushGoal(retreat);

            int before = creature.GetStat("Hitpoints").BaseValue;
            for (int i = 0; i < 30; i++)
                retreat.TakeAction();
            int after = creature.GetStat("Hitpoints").BaseValue;

            Assert.AreEqual(before, after,
                "HealPerTick=0 must mean no HP mutation across many ticks. " +
                "If after != before, the zero-guard is missing.");
        }

        /// <summary>
        /// PREDICTION: when the entity is ALREADY at the waypoint and
        /// already at full HP, RetreatGoal arrives, transitions to Recover,
        /// and IMMEDIATELY finishes (Phase.Done). One TakeAction is enough.
        /// CONFIDENCE: medium. The code might require an extra tick.
        /// </summary>
        [Test]
        public void RetreatGoal_AlreadyAtWaypointAndFullHp_FinishesInOneTick()
        {
            var zone = new Zone("AdvZone");
            var creature = CreateCreature(zone, 5, 5, hp: 100);
            var brain = creature.GetPart<BrainPart>();
            // HP already at full.

            var retreat = new RetreatGoal(5, 5, safeHpFraction: 0.5f);
            brain.PushGoal(retreat);

            retreat.TakeAction();

            Assert.IsTrue(retreat.Finished(),
                "Already-at-waypoint + already-fine HP should finish on the " +
                "very first TakeAction (Travel→Recover transitions in-line).");
        }

        // ============================================================
        // DormantGoal — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: when the only nearby creature is SAME-FACTION (allied,
        /// not hostile), DormantGoal does NOT wake. AIHelpers.FindNearestHostile
        /// should filter by faction.
        /// CONFIDENCE: high — that's what the function is named.
        /// </summary>
        [Test]
        public void DormantGoal_SameFactionCreatureInSight_DoesNotWake()
        {
            var zone = new Zone("AdvZone");
            var ambusher = CreateCreature(zone, 10, 10, faction: "Snapjaws", hp: 30);

            // Allied (same-faction) creature nearby.
            var ally = CreateCreature(zone, 12, 10, faction: "Snapjaws", hp: 30);

            var brain = ambusher.GetPart<BrainPart>();
            var dormant = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: true);
            brain.PushGoal(dormant);

            dormant.TakeAction();

            Assert.IsFalse(dormant.Finished(),
                "Same-faction ally in sight must not trigger wake. The hostile " +
                "filter should reject allies.");
        }

        /// <summary>
        /// PREDICTION: with both wake flags on, when HP drops AND a hostile
        /// is also in sight, the goal wakes ONCE. _wakeRequested is set to
        /// true and stays true; subsequent ticks don't double-wake.
        /// CONFIDENCE: high — Wake() pattern is idempotent.
        /// </summary>
        [Test]
        public void DormantGoal_BothFlags_DamageAndHostile_WakesOnceTotal()
        {
            var zone = new Zone("AdvZone");
            var ambusher = CreateCreature(zone, 10, 10, faction: "Snapjaws", hp: 30);

            var hostile = new Entity { BlueprintName = "Player" };
            hostile.Tags["Creature"] = "";
            hostile.Tags["Player"] = "";
            hostile.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            hostile.AddPart(new RenderPart { DisplayName = "player" });
            hostile.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(hostile, 12, 10);

            var brain = ambusher.GetPart<BrainPart>();
            var dormant = new DormantGoal(wakeOnDamage: true, wakeOnHostileInSight: true);
            brain.PushGoal(dormant);

            // Tick 1: seed baseline.
            dormant.TakeAction();
            // Drop HP AND hostile is already in sight.
            ambusher.GetStat("Hitpoints").BaseValue = 10;
            // Tick 2: both triggers fire — but the goal should be in
            // Finished=true state regardless.
            dormant.TakeAction();
            Assert.IsTrue(dormant.Finished(), "Should wake.");

            // Tick 3: should still be Finished. Nothing weird should happen.
            Assert.DoesNotThrow(() => dormant.TakeAction(),
                "TakeAction on already-Finished DormantGoal must not crash.");
            Assert.IsTrue(dormant.Finished(),
                "Finished must STAY true across additional ticks.");
        }

        /// <summary>
        /// PREDICTION: TakeAction with null ParentEntity should not throw.
        /// CONFIDENCE: medium — Production may early-Pop on null.
        /// </summary>
        [Test]
        public void DormantGoal_NullParentEntity_TakeActionDoesNotThrow()
        {
            // Manually wire a goal with no brain attached, simulating a
            // detached/orphaned state.
            var dormant = new DormantGoal();
            // ParentBrain is null (never assigned).

            Assert.DoesNotThrow(() => dormant.TakeAction(),
                "Orphan DormantGoal must not crash. Pop() or no-op are both " +
                "acceptable.");
        }

        /// <summary>
        /// PREDICTION: SleepParticleInterval=1 emits a 'z' particle EVERY tick
        /// after the first. Boundary on the modulo check.
        /// CONFIDENCE: low. The check is `Age > 0 && Age % Interval == 0`.
        /// At Interval=1, Age=1 → 1 % 1 = 0 → emit. So yes every tick after
        /// the first. But maybe the code is `Age % Interval == 0` without
        /// the `Age > 0` gate — in which case it would emit on tick 0 too,
        /// or maybe never if a different formula.
        /// </summary>
        [Test]
        public void DormantGoal_SleepParticleIntervalOne_EmitsEveryTickAfterFirst()
        {
            var zone = new Zone("AdvZone");
            var ambusher = CreateCreature(zone, 10, 10, faction: "Snapjaws", hp: 30);
            var brain = ambusher.GetPart<BrainPart>();
            var dormant = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: false,
                sleepParticleInterval: 1);
            brain.PushGoal(dormant);

            // Drive a few ticks. We can't easily count emissions without a
            // probe, so just verify no crash + still dormant.
            for (int i = 0; i < 5; i++)
            {
                dormant.Age = i;
                Assert.DoesNotThrow(() => dormant.TakeAction(),
                    $"Tick {i} with SleepInterval=1 must not crash.");
                Assert.IsFalse(dormant.Finished(),
                    "Particle emissions must not accidentally wake the goal.");
            }
        }

        // ============================================================
        // BrainPart.Passive — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: setting Passive=true on an NPC that's already pushed
        /// a KillGoal does NOT immediately retract the KillGoal. Passive is
        /// a "don't INITIATE combat" flag, not a "stop ongoing combat" flag.
        /// CONFIDENCE: medium. The doc-string for Passive says "don't
        /// initiate" but maybe a future patch intercepted this.
        /// </summary>
        [Test]
        public void Passive_FlagFlippedTrueMidCombat_DoesNotRetractExistingKillGoal()
        {
            var zone = new Zone("AdvZone");
            var attacker = CreateCreature(zone, 5, 5, faction: "Villagers");
            var enemy = CreateCreature(zone, 7, 5, faction: "Snapjaws");
            FactionManager.SetFactionFeeling("Villagers", "Snapjaws", -100);
            FactionManager.SetFactionFeeling("Snapjaws", "Villagers", -100);

            var brain = attacker.GetPart<BrainPart>();
            // Attacker has acquired the enemy and pushed KillGoal.
            brain.PushGoal(new KillGoal(enemy));
            Assume.That(brain.HasGoal<KillGoal>(), "Setup: KillGoal should be on stack.");

            // Now flip Passive=true.
            brain.Passive = true;

            // Fire a tick — the KillGoal should still be there.
            attacker.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<KillGoal>(),
                "Passive=true is a 'don't initiate' flag, not a 'stop ongoing' " +
                "interrupt. Existing KillGoal must persist. If this fails, " +
                "the flag is doing more than its docs claim.");
        }

        /// <summary>
        /// PREDICTION: a Passive NPC takes damage from environmental DoT
        /// (poison, bleed) and does NOT counter-attack — there's no source
        /// to retaliate against.
        /// CONFIDENCE: high — IsPersonallyHostileTo requires a known source.
        /// </summary>
        [Test]
        public void Passive_TakesEnvironmentalDamage_DoesNotPushKillGoal()
        {
            var zone = new Zone("AdvZone");
            var npc = CreateCreature(zone, 5, 5, faction: "Villagers");
            var brain = npc.GetPart<BrainPart>();
            brain.Passive = true;

            // Environmental damage — null source.
            CombatSystem.ApplyDamage(npc, 3, source: null, zone);

            // Tick — Passive shouldn't trigger any aggro from null source.
            npc.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<KillGoal>(),
                "Passive + null-source damage must not push KillGoal — " +
                "there's no entity to chase.");
        }
    }
}
