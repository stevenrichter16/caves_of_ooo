using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/COMBAT-SYSTEM-AUDIT-2026-07.md -- ApplyDamage's typed-Damage
    /// overload decrements Hitpoints.BaseValue with NO floor
    /// (<c>hpStat.BaseValue -= amount</c>). A large overkill hit on a
    /// low-max-HP creature can leave BaseValue deeply negative (e.g. -990
    /// for 1000 damage against a 10-max-HP creature).
    ///
    /// All existing "is it dead" checks read HP through <c>.Value</c> /
    /// <c>GetStatValue</c> (Stat's computed getter DOES clamp to
    /// [Min, Max]), so nothing is broken in shipped behavior TODAY -- but
    /// any code that reads <c>.BaseValue</c> directly (already true of
    /// several call sites, e.g. LifestealPart.cs) would see a wrong,
    /// deeply-negative number instead of a floored one.
    ///
    /// Fix: floor the decrement at hpStat.Min via
    /// <c>Math.Max(hpStat.Min, hpStat.BaseValue - amount)</c>, and the
    /// same for the "HP" alias stat when one exists and isn't the same
    /// Stat object as "Hitpoints".
    /// </summary>
    public class ApplyDamageHpFloorClampTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private static Entity MakeTarget(int hp, int min = 0)
        {
            var e = new Entity { ID = "tgt", BlueprintName = "TestTarget" };
            e.AddPart(new RenderPart { DisplayName = "target" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = min, Max = hp };
            return e;
        }

        [Test]
        public void ApplyDamage_MassiveOverkill_BaseValueFlooredAtMin()
        {
            var zone = new Zone("FloorTest");
            var target = MakeTarget(hp: 10);
            zone.AddEntity(target, 3, 3);

            var dmg = new Damage(1000);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var hpStat = target.GetStat("Hitpoints");
            Assert.GreaterOrEqual(hpStat.BaseValue, hpStat.Min,
                "BaseValue must never go below Min -- a raw '-= amount' with no " +
                "floor left it deeply negative (e.g. -990) for any future caller " +
                "reading .BaseValue directly instead of .Value/.GetStatValue.");
        }

        [Test]
        public void ApplyDamage_MassiveOverkill_HpAliasStat_AlsoFlooredAtMin()
        {
            // Synthetic scenario: an entity with a distinct "HP" alias Stat
            // object alongside "Hitpoints" -- exercises the hpAlias branch
            // in ApplyDamage (target.GetStat("HP") != null && != hpStat).
            var zone = new Zone("FloorAliasTest");
            var target = MakeTarget(hp: 10);
            target.Statistics["HP"] = new Stat
            { Owner = target, Name = "HP", BaseValue = 10, Min = 0, Max = 10 };
            zone.AddEntity(target, 3, 3);

            var dmg = new Damage(1000);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var hpAlias = target.GetStat("HP");
            Assert.GreaterOrEqual(hpAlias.BaseValue, hpAlias.Min,
                "The 'HP' alias stat decrement must also be floored at Min, " +
                "mirroring the 'Hitpoints' stat's fix.");
        }

        // ====================================================================
        // Counter-check (CLAUDE.md §3.4): a normal, non-overkill hit still
        // decrements BaseValue by the EXACT damage amount -- the new
        // Math.Max floor must be a no-op on the ordinary path. Without
        // this, a buggy fix that e.g. always floors to Min regardless of
        // amount would still pass the overkill test above.
        // ====================================================================

        [Test]
        public void ApplyDamage_NormalHit_DecrementsByExactAmount_UnaffectedByClamp()
        {
            var zone = new Zone("NormalHitTest");
            var target = MakeTarget(hp: 50);
            zone.AddEntity(target, 3, 3);

            var dmg = new Damage(8);
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: zone);

            var hpStat = target.GetStat("Hitpoints");
            Assert.AreEqual(42, hpStat.BaseValue,
                "A normal, non-overkill hit must decrement BaseValue by exactly " +
                "the damage amount -- the new floor clamp must not fire here.");
        }
    }
}
