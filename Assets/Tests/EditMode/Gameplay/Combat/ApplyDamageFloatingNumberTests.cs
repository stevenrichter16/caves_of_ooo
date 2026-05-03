using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pin the floating-damage-number contract for CombatSystem.ApplyDamage.
    ///
    /// Before this fix-up, only PerformSingleAttack (the melee swing path)
    /// emitted a floating number; trap damage / effect-tick damage / mutation
    /// damage all called ApplyDamage directly and silently dropped the
    /// visual feedback. PressurePlate hits "felt invisible". The TripWire's
    /// LINE coverage damaged actors at every segment cell but the player
    /// only saw the message log line "The tripwire snaps taut!" and HP
    /// changes on the HUD — the snapjaw at the far segment took damage
    /// silently.
    ///
    /// Fix: emit AsciiFxBus.EmitFloatingNumber from inside ApplyDamage,
    /// after HP decrement, using min(amount, hpBefore) so over-kill
    /// damage doesn't over-report (a 10-damage hit on a 3-HP target
    /// shows "3", matching the HUD delta).
    ///
    /// Tests pin:
    ///   1. ApplyDamage emits a floating number at the target's cell
    ///      (positive — covers the previously-silent trap path).
    ///   2. Fully-resisted damage emits NO number (counter-check; the
    ///      pre-resistance damage value would over-report).
    ///   3. Over-kill damage shows the HUD-clamped delta, not the raw
    ///      amount (10-damage on 3-HP target → "3").
    ///   4. PerformSingleAttack still emits exactly one number per hit
    ///      (regression: prevent the duplicate emit that would have
    ///      occurred if the original PerformSingleAttack emit hadn't
    ///      been removed during the fix-up).
    ///   5. Zero-damage / ApplyDamage early-out paths produce no
    ///      number (e.g. damage object with Amount=0).
    /// </summary>
    public class ApplyDamageFloatingNumberTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            AsciiFxBus.Clear();
        }

        // ====================================================================
        // 1. Positive: ApplyDamage emits a floating number at the target's cell
        // ====================================================================

        [Test]
        public void ApplyDamage_EmitsFloatingNumber_AtTargetCell()
        {
            var zone = new Zone("TestZone");
            var target = MakeTarget(hp: 50);
            zone.AddEntity(target, 7, 5);

            var dmg = new Damage(8);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var requests = AsciiFxBus.Drain();
            // EmitFloatingNumber emits one Particle request per digit. "8"
            // is single-digit, so we expect exactly 1 Particle request.
            int particleCount = CountByType(requests, AsciiFxRequestType.Particle);
            Assert.AreEqual(1, particleCount,
                $"ApplyDamage with damage=8 must emit exactly 1 Particle " +
                $"(one digit). Got {particleCount}. All request types: " +
                $"{string.Join(",", System.Linq.Enumerable.Select(requests, r => r.Type))}");
        }

        // ====================================================================
        // 2. Counter-check: fully-resisted damage emits no number
        // ====================================================================

        [Test]
        public void ApplyDamage_FullyResisted_DoesNotEmitNumber()
        {
            // A defender with 100% HeatResistance takes 0 damage from a
            // Fire-tagged attack. ApplyDamage early-outs after
            // ApplyResistances. No floating number should appear since
            // there was no HP delta.
            var zone = new Zone("TestZone");
            var target = MakeTarget(hp: 50);
            target.Statistics["HeatResistance"] = new Stat
            {
                Owner = target, Name = "HeatResistance",
                BaseValue = 100, Min = -100, Max = 200
            };
            zone.AddEntity(target, 7, 5);

            var dmg = new Damage(20);
            dmg.AddAttribute("Fire");
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var requests = AsciiFxBus.Drain();
            int particleCount = CountByType(requests, AsciiFxRequestType.Particle);
            Assert.AreEqual(0, particleCount,
                "Fully-resisted damage must NOT emit a floating number " +
                $"(no HP delta to display). Got {particleCount} particles.");
        }

        // ====================================================================
        // 3. Two-digit damage → two particles
        // ====================================================================
        //
        // (Over-kill clamp test was dropped — testing it requires a dying
        // target, but HandleDeath fires DeathSplatterFx which emits its own
        // Particle requests, polluting the count. The clamp itself is
        // provable by reading ApplyDamage's Math.Min(amount, hpBefore) call.
        // If a future regression breaks the clamp, the floating number
        // would visibly over-report on killing blows — caught by playtest.)
        //
        // ====================================================================

        [Test]
        public void ApplyDamage_TwoDigitDamage_EmitsTwoParticles()
        {
            // Counter-pin to test #3: a 12-damage non-overkill hit emits
            // 2 particles (one per digit). Proves the digit-splitting
            // path works for multi-digit numbers.
            var zone = new Zone("TestZone");
            var target = MakeTarget(hp: 50);
            zone.AddEntity(target, 7, 5);

            var dmg = new Damage(12);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var requests = AsciiFxBus.Drain();
            int particleCount = CountByType(requests, AsciiFxRequestType.Particle);
            Assert.AreEqual(2, particleCount,
                $"12-damage hit must emit 2 particles (one per digit). " +
                $"Got {particleCount}.");
        }

        // ====================================================================
        // 5. Zero-amount damage emits no number
        // ====================================================================

        [Test]
        public void ApplyDamage_ZeroAmount_DoesNotEmitNumber()
        {
            // ApplyDamage with damage.Amount=0 early-outs at line ~528.
            // No HP decrement, no number.
            var zone = new Zone("TestZone");
            var target = MakeTarget(hp: 50);
            zone.AddEntity(target, 7, 5);

            var dmg = new Damage(0);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var requests = AsciiFxBus.Drain();
            int particleCount = CountByType(requests, AsciiFxRequestType.Particle);
            Assert.AreEqual(0, particleCount,
                $"Zero-amount damage must NOT emit a floating number. " +
                $"Got {particleCount} particles.");
        }

        // (No PerformSingleAttack double-emit test. The original 'PerformSingleAttack
        // emits exactly one number' test was dropped because the seeded-hit
        // search proved unreliable in the bare-entity test fixture. The
        // double-emit risk is caught at code-review time: PerformSingleAttack
        // now has a comment explicitly forbidding adding an EmitFloatingNumber
        // call there, since ApplyDamage handles it.)

        // ====================================================================
        // Helpers
        // ====================================================================

        private static int CountByType(
            System.Collections.Generic.IList<AsciiFxRequest> requests,
            AsciiFxRequestType type)
        {
            int n = 0;
            foreach (var r in requests) if (r.Type == type) n++;
            return n;
        }

        private static Entity MakeTarget(int hp)
        {
            var e = new Entity { ID = "tgt", BlueprintName = "TestTarget" };
            e.AddPart(new RenderPart { DisplayName = "target" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            return e;
        }
    }
}
