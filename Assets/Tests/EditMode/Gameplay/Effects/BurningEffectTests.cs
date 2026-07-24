using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BurningEffect.OnStack — re-ignition source attribution.
    ///
    /// Bug (Docs/COMBAT-SYSTEM-AUDIT-2026-07.md, "BurningEffect.OnStack drops
    /// the re-igniter's identity — misattributes kill credit"): OnStack merges
    /// Intensity from the incoming re-ignition but never touches
    /// IgnitionSource. IgnitionSource is what per-turn fire damage (and any
    /// resulting kill) gets attributed to via CombatSystem.ApplyDamage's
    /// `attacker` parameter in OnTurnStart. Attacker A ignites a target;
    /// Attacker B re-ignites it later with a bigger hit — all subsequent tick
    /// damage, and a possible burn-tick kill, stayed credited to A even though
    /// B is the one currently burning the target.
    ///
    /// Design decision (already made, not re-litigated here): "most recent
    /// igniter wins" — OnStack unconditionally reassigns IgnitionSource to
    /// the incoming effect's source on every re-ignition.
    /// </summary>
    public class BurningEffectTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Re-ignition by a DIFFERENT source reassigns IgnitionSource
        // ====================================================================

        [Test]
        public void OnStack_ReignitedByDifferentSource_UpdatesIgnitionSourceToNewest()
        {
            var target = MakeCreature();
            var attackerA = MakeCreature(blueprintName: "AttackerA");
            var attackerB = MakeCreature(blueprintName: "AttackerB");

            var first = new BurningEffect(intensity: 1.0f, source: attackerA);
            target.ApplyEffect(first);

            var second = new BurningEffect(intensity: 3.0f, source: attackerB);
            target.ApplyEffect(second);

            var burning = target.GetPart<StatusEffectsPart>().GetEffect<BurningEffect>();
            Assert.AreSame(attackerB, burning.IgnitionSource,
                "Most-recent-igniter-wins: re-igniting with a different source must " +
                "reassign IgnitionSource to the incoming effect's source, so subsequent " +
                "tick damage (and any burn-tick kill) is credited to the actual re-igniter, " +
                "not the stale original attacker.");
        }

        // ====================================================================
        // 2. Counter-check: re-igniting with the SAME source is a no-op change
        // ====================================================================

        [Test]
        public void OnStack_ReignitedBySameSource_IgnitionSourceStaysThatSource()
        {
            var target = MakeCreature();
            var attacker = MakeCreature(blueprintName: "Attacker");

            var first = new BurningEffect(intensity: 1.0f, source: attacker);
            target.ApplyEffect(first);

            var second = new BurningEffect(intensity: 1.0f, source: attacker);
            target.ApplyEffect(second);

            var burning = target.GetPart<StatusEffectsPart>().GetEffect<BurningEffect>();
            Assert.AreSame(attacker, burning.IgnitionSource,
                "Re-igniting with the same source must leave IgnitionSource pointing at " +
                "that same source (trivially true under unconditional reassignment, but " +
                "pins the no-special-casing behavior explicitly).");
        }

        // ====================================================================
        // 3. Existing Intensity-merge behavior is unaffected by the fix
        // ====================================================================

        [Test]
        public void OnStack_IntensityMergeFormula_UnaffectedByIgnitionSourceFix()
        {
            var target = MakeCreature();
            var attackerA = MakeCreature(blueprintName: "AttackerA");
            var attackerB = MakeCreature(blueprintName: "AttackerB");

            var first = new BurningEffect(intensity: 1.0f, source: attackerA);
            target.ApplyEffect(first);

            var second = new BurningEffect(intensity: 2.0f, source: attackerB);
            target.ApplyEffect(second);

            var burning = target.GetPart<StatusEffectsPart>().GetEffect<BurningEffect>();
            // Existing formula: Intensity = min(Intensity + incoming.Intensity * 0.5, 5.0)
            // = min(1.0 + 2.0*0.5, 5.0) = 2.0
            Assert.AreEqual(2.0f, burning.Intensity, 0.0001f,
                "Intensity-merge formula must be untouched by the IgnitionSource fix.");
        }

        // ====================================================================
        // 4. Counter-check: OnStack returns true (stacking absorbed, not a
        //    duplicate effect added) regardless of source change
        // ====================================================================

        [Test]
        public void OnStack_ReignitedByDifferentSource_DoesNotDuplicateEffect()
        {
            var target = MakeCreature();
            var attackerA = MakeCreature(blueprintName: "AttackerA");
            var attackerB = MakeCreature(blueprintName: "AttackerB");

            target.ApplyEffect(new BurningEffect(intensity: 1.0f, source: attackerA));
            target.ApplyEffect(new BurningEffect(intensity: 2.0f, source: attackerB));

            Assert.AreEqual(1, target.GetPart<StatusEffectsPart>().EffectCount,
                "Re-ignition must stack onto the existing BurningEffect instance, " +
                "not add a second one.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeCreature(int hp = 100, string blueprintName = "TestCreature")
        {
            var e = new Entity { BlueprintName = blueprintName };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10 };
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }
    }
}
