using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Effects
{
    /// <summary>
    /// EffectDescriber must give a real, human line for the effects players
    /// actually meet — live-play finding (2026-07-19): a water-drenched
    /// target (LiquidCoveredEffect) showed nothing useful under Afflicted
    /// because only the tonic-family effects had wording; everything else
    /// fell to a type-name fallback.
    /// </summary>
    public class EffectDescriberTests
    {
        [Test]
        public void LiquidCovered_NamesTheLiquid()
        {
            var line = EffectDescriber.Describe(new LiquidCoveredEffect("water", 50));
            StringAssert.Contains("water", line.ToLowerInvariant());
            StringAssert.DoesNotContain("LiquidCovered", line,
                "player-facing wording, not a type name");
        }

        [Test]
        public void CommonControlEffects_GetRealWordsWithDuration()
        {
            // Each line must carry the human word and the live remaining
            // duration — no "SoAndSoEffect" type names leaking through.
            var stunned = EffectDescriber.Describe(new StunnedEffect(3));
            StringAssert.Contains("Stunned", stunned);
            StringAssert.Contains("3", stunned);

            var paralyzed = EffectDescriber.Describe(new ParalyzedEffect(2));
            StringAssert.Contains("Paralyzed", paralyzed);
            StringAssert.Contains("2", paralyzed);

            var confused = EffectDescriber.Describe(new ConfusedEffect(4));
            StringAssert.Contains("Confused", confused);

            var rooted = EffectDescriber.Describe(new RootedEffect(4));
            StringAssert.Contains("Rooted", rooted);

            var hobbled = EffectDescriber.Describe(new HobbledEffect(8));
            StringAssert.Contains("Hobbled", hobbled);

            foreach (var line in new[] { stunned, paralyzed, confused, rooted, hobbled })
                StringAssert.DoesNotContain("Effect", line);
        }

        [Test]
        public void Weakened_ShowsTheStrengthPenalty()
        {
            var line = EffectDescriber.Describe(new WeakenedEffect(strPenalty: 3, duration: 5));
            StringAssert.Contains("Weakened", line);
            StringAssert.Contains("3", line);
        }

        [Test]
        public void UnknownEffectType_FallsBackToCleanNameWithDuration()
        {
            // Not every effect gets bespoke wording — but the fallback must
            // be presentable: cleaned type name, duration when finite.
            var line = EffectDescriber.Describe(new SmolderingEffect(5));
            StringAssert.DoesNotContain("SmolderingEffect", line,
                "the raw type name must not leak");
            StringAssert.Contains("Smoldering", line);
        }
    }
}
