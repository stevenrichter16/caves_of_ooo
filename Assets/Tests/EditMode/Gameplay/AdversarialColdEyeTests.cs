using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ADVERSARIAL cold-eye tests. Writing strategy:
    ///
    /// 1. Pick a target I haven't recently audited (CombatSystem.ApplyDamage,
    ///    ThermalPart).
    /// 2. State my honest expectation for each invariant in the test before
    ///    looking at the production code.
    /// 3. Run. For each failure, analyze whether MY expectation was wrong
    ///    or the PRODUCTION CODE is wrong.
    ///
    /// Where the existing M1/M2/M3 gap-coverage tests confirmed correct
    /// behavior, this file deliberately probes for bugs by stating
    /// expectations confidently in code first, then checking reality.
    ///
    /// Each test's xml-doc records:
    ///   - PREDICTION: what I think will happen
    ///   - CONFIDENCE: how sure I am (low / medium / high)
    /// Then on failure analysis (in commit message): test-wrong vs code-wrong.
    /// </summary>
    [TestFixture]
    public class AdversarialColdEyeTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ============================================================
        // Helpers
        // ============================================================

        private Entity CreateTarget(Zone zone, int x, int y, int hp = 50)
        {
            var e = new Entity { BlueprintName = "TestTarget" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = "target" });
            e.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(e, x, y);
            return e;
        }

        private Entity CreateSource(Zone zone, int x, int y)
        {
            var e = new Entity { BlueprintName = "TestSource" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "source" });
            zone.AddEntity(e, x, y);
            return e;
        }

        // ============================================================
        // CombatSystem.ApplyDamage — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: ApplyDamage with amount=0 is a no-op.
        /// CONFIDENCE: high — defensive early-return is the obvious impl.
        /// </summary>
        [Test]
        public void ApplyDamage_AmountZero_IsNoOp()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5, hp: 50);
            var source = CreateSource(zone, 4, 5);

            int before = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 0, source, zone);
            int after = target.GetStatValue("Hitpoints");

            Assert.AreEqual(before, after,
                "Zero damage should not change HP. If this fails, ApplyDamage " +
                "is mutating state on a no-damage call.");
        }

        /// <summary>
        /// PREDICTION: ApplyDamage with negative amount is a no-op (does NOT heal).
        /// CONFIDENCE: medium — could go either way: code might early-return on
        /// `amount <= 0`, OR could naively subtract negative and accidentally heal.
        /// Healing on negative would be a security/exploit risk.
        /// </summary>
        [Test]
        public void ApplyDamage_NegativeAmount_DoesNotHeal()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5, hp: 50);
            target.GetStat("Hitpoints").BaseValue = 10; // wounded

            CombatSystem.ApplyDamage(target, -100, null, zone);

            int hp = target.GetStatValue("Hitpoints");
            Assert.LessOrEqual(hp, 10,
                "Negative damage must NOT heal the target. If HP went above 10, " +
                "the code is naively doing `BaseValue -= amount`, which heals on " +
                "negative — a real exploit surface.");
        }

        /// <summary>
        /// PREDICTION: ApplyDamage(null target, …) doesn't throw.
        /// CONFIDENCE: high — basic defensive null-check expected.
        /// </summary>
        [Test]
        public void ApplyDamage_NullTarget_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CombatSystem.ApplyDamage(null, 10, null, null),
                "Null target must early-return cleanly.");
        }

        /// <summary>
        /// PREDICTION: amount > current HP triggers HandleDeath (entity dies).
        /// CONFIDENCE: high — this is the canonical kill path.
        /// </summary>
        [Test]
        public void ApplyDamage_AmountExceedsCurrentHP_TriggersDeath()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5, hp: 10);
            var source = CreateSource(zone, 4, 5);

            CombatSystem.ApplyDamage(target, 999, source, zone);

            // After death, entity is removed from zone (per HandleDeath:469-470).
            Assert.IsNull(zone.GetEntityCell(target),
                "Target must be removed from zone after lethal damage.");
        }

        /// <summary>
        /// PREDICTION: target with no Hitpoints stat doesn't crash; damage no-ops.
        /// CONFIDENCE: medium — code should null-check GetStat("Hitpoints").
        /// </summary>
        [Test]
        public void ApplyDamage_NoHitpointsStat_DoesNotCrashOrKill()
        {
            var zone = new Zone("AdvZone");
            var target = new Entity { BlueprintName = "Statue" };
            target.Tags["Creature"] = "";
            target.AddPart(new RenderPart { DisplayName = "statue" });
            target.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(target, 5, 5);
            // Deliberately no Hitpoints stat.

            Assert.DoesNotThrow(() => CombatSystem.ApplyDamage(target, 100, null, zone),
                "Target without Hitpoints must not crash ApplyDamage.");
            // Entity should still be in the zone (didn't die).
            Assert.IsNotNull(zone.GetEntityCell(target),
                "Without Hitpoints, target shouldn't be auto-killed.");
        }

        /// <summary>
        /// PREDICTION: ApplyDamage fires a "TakeDamage" event on the target.
        /// CONFIDENCE: high — the event seam is documented in the codebase.
        /// </summary>
        [Test]
        public void ApplyDamage_FiresTakeDamageEvent_OnTarget()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5);
            var source = CreateSource(zone, 4, 5);
            var probe = new EventProbe();
            target.AddPart(probe);

            CombatSystem.ApplyDamage(target, 5, source, zone);

            Assert.IsTrue(probe.Saw("TakeDamage"),
                "ApplyDamage must fire 'TakeDamage' on the target.");
        }

        /// <summary>
        /// PREDICTION: ApplyDamage fires "DamageDealt" event on the source
        /// (when source is non-null).
        /// CONFIDENCE: medium — I'm guessing the API, could be named "OnHit"
        /// or similar. If this fails on event name, that's a test issue.
        /// </summary>
        [Test]
        public void ApplyDamage_FiresDamageDealtEvent_OnNonNullSource()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5);
            var source = CreateSource(zone, 4, 5);
            var probe = new EventProbe();
            source.AddPart(probe);

            CombatSystem.ApplyDamage(target, 5, source, zone);

            Assert.IsTrue(probe.Saw("DamageDealt"),
                "ApplyDamage must fire 'DamageDealt' on the source so on-hit " +
                "effects (poison-on-hit, life-steal) can react.");
        }

        /// <summary>
        /// PREDICTION: with source==null, no DamageDealt event is fired anywhere.
        /// CONFIDENCE: high — null-source means environmental damage.
        /// </summary>
        [Test]
        public void ApplyDamage_NullSource_NoDamageDealtFiredAnywhere()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5);
            var probe = new EventProbe();
            target.AddPart(probe);

            CombatSystem.ApplyDamage(target, 5, null, zone);

            // The target may receive TakeDamage — that's fine — but should NOT
            // receive DamageDealt (DamageDealt is for the attacker side).
            Assert.IsFalse(probe.Saw("DamageDealt"),
                "Null source means no attacker, so no DamageDealt event should fire.");
        }

        /// <summary>
        /// PREDICTION: damaging a target that already has HP=0 does NOT
        /// re-fire HandleDeath. Idempotent.
        /// CONFIDENCE: LOW — I found in the M6 review that HandleDeath is
        /// NOT idempotent (double-fire produces double "killed" message and
        /// double Died event). ApplyDamage might or might not guard against
        /// this. If this test fails, the code has a real idempotency bug.
        /// </summary>
        [Test]
        public void ApplyDamage_OnAlreadyDeadTarget_DoesNotReFireDeath()
        {
            var zone = new Zone("AdvZone");
            var target = CreateTarget(zone, 5, 5, hp: 10);
            var source = CreateSource(zone, 4, 5);

            // Kill the target first.
            CombatSystem.ApplyDamage(target, 999, source, zone);
            Assume.That(zone.GetEntityCell(target), Is.Null,
                "Setup: target should be dead/removed after first kill.");

            int killedMessagesBefore = CountKilledMessages();

            // Try to "damage" the dead target again. The target is already
            // out of the zone, but ApplyDamage might still process them
            // (the function takes Entity, not zone-cell ref).
            CombatSystem.ApplyDamage(target, 5, source, zone);

            int killedMessagesAfter = CountKilledMessages();
            Assert.AreEqual(killedMessagesBefore, killedMessagesAfter,
                "ApplyDamage on an already-dead target must NOT re-fire HandleDeath. " +
                "Otherwise the M6 CR-01 bug pattern (double-kill messages, double " +
                "Died event, potential double corpse drop) recurs.");
        }

        // ============================================================
        // ThermalPart — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: temperature drifts toward AmbientTemperature on EndTurn.
        /// CONFIDENCE: high — saw `Temperature -= diff * AmbientDecayRate` earlier.
        /// </summary>
        [Test]
        public void Thermal_TemperatureAboveAmbient_DecaysDownOnEndTurn()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 100f, AmbientTemperature = 25f, AmbientDecayRate = 0.05f };
            entity.AddPart(thermal);

            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(thermal.Temperature, 100f,
                "Temperature above ambient should decay toward ambient.");
            Assert.Greater(thermal.Temperature, 25f,
                "One tick of decay shouldn't reach ambient instantly.");
        }

        /// <summary>
        /// PREDICTION: temperature below ambient also drifts toward ambient (warms up).
        /// CONFIDENCE: high — symmetric thermodynamics.
        /// </summary>
        [Test]
        public void Thermal_TemperatureBelowAmbient_DriftsUpOnEndTurn()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = -50f, AmbientTemperature = 25f, AmbientDecayRate = 0.05f };
            entity.AddPart(thermal);

            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.Greater(thermal.Temperature, -50f,
                "Cold object should warm toward ambient on EndTurn.");
            Assert.Less(thermal.Temperature, 25f,
                "One tick shouldn't reach ambient instantly.");
        }

        /// <summary>
        /// PREDICTION: temperature within 0.5 of ambient snaps to ambient.
        /// CONFIDENCE: high — saw the snap code earlier.
        /// </summary>
        [Test]
        public void Thermal_TemperatureWithinHalfDegree_SnapsToAmbient()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 25.3f, AmbientTemperature = 25f, AmbientDecayRate = 0.05f };
            entity.AddPart(thermal);

            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(25f, thermal.Temperature, 0.0001f,
                "Within 0.5 of ambient, temperature must snap exactly to ambient. " +
                "Otherwise tiny floating-point drift accumulates forever.");
        }

        /// <summary>
        /// PREDICTION: temperature exactly AT ambient does not change on EndTurn.
        /// CONFIDENCE: high — diff is 0 → delta is 0.
        /// </summary>
        [Test]
        public void Thermal_AtExactAmbient_DoesNotChange()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 25f, AmbientTemperature = 25f, AmbientDecayRate = 0.05f };
            entity.AddPart(thermal);

            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(25f, thermal.Temperature, 0.0001f);
        }

        /// <summary>
        /// PREDICTION: applying heat via "ApplyHeat" event raises Temperature.
        /// CONFIDENCE: high — saw `HandleApplyHeat` earlier with Joules param.
        /// </summary>
        [Test]
        public void Thermal_ApplyHeat_RaisesTemperature()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 25f, HeatCapacity = 1.0f };
            entity.AddPart(thermal);

            float before = thermal.Temperature;
            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", 50f);
            heatEvent.SetParameter("Radiant", false);
            entity.FireEvent(heatEvent);

            Assert.Greater(thermal.Temperature, before,
                "ApplyHeat with positive Joules must raise Temperature.");
        }

        /// <summary>
        /// PREDICTION: ApplyHeat with Joules=0 is a no-op.
        /// CONFIDENCE: high — saw `if (joules == 0f) return true;` earlier.
        /// </summary>
        [Test]
        public void Thermal_ApplyHeat_ZeroJoules_IsNoOp()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 25f, HeatCapacity = 1.0f };
            entity.AddPart(thermal);

            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", 0f);
            heatEvent.SetParameter("Radiant", false);
            entity.FireEvent(heatEvent);

            Assert.AreEqual(25f, thermal.Temperature, 0.0001f,
                "Zero-joule ApplyHeat must not change temperature.");
        }

        /// <summary>
        /// PREDICTION: ApplyHeat with negative Joules cools the entity (drains heat).
        /// CONFIDENCE: medium — the API allows it (Joules is float). Production
        /// might support active cooling (cold spells) or might guard against it.
        /// </summary>
        [Test]
        public void Thermal_ApplyHeat_NegativeJoules_CoolsTemperature()
        {
            var zone = new Zone("AdvZone");
            var entity = CreateTarget(zone, 5, 5);
            var thermal = new ThermalPart { Temperature = 100f, HeatCapacity = 1.0f };
            entity.AddPart(thermal);

            float before = thermal.Temperature;
            var coolEvent = GameEvent.New("ApplyHeat");
            coolEvent.SetParameter("Joules", -50f);
            coolEvent.SetParameter("Radiant", false);
            entity.FireEvent(coolEvent);

            Assert.Less(thermal.Temperature, before,
                "Negative Joules should drain heat (cool the entity). If this " +
                "fails, the code clamps Joules >= 0 — a design choice, but " +
                "would prevent cold-spell cooling via the same event seam.");
        }

        // ============================================================
        // Helper: event probe + message-log scanner
        // ============================================================

        private static int CountKilledMessages()
        {
            int count = 0;
            foreach (var msg in MessageLog.GetMessages())
            {
                if (msg != null && msg.Contains("is killed by")) count++;
            }
            return count;
        }

        private class EventProbe : Part
        {
            private readonly System.Collections.Generic.HashSet<string> _seen
                = new System.Collections.Generic.HashSet<string>();

            public bool Saw(string id) => _seen.Contains(id);

            public override bool HandleEvent(GameEvent e)
            {
                if (e?.ID != null) _seen.Add(e.ID);
                return true;
            }
        }
    }
}
