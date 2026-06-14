using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// RED scaffold for Challenge 3 — "Brew a New Status Effect".
    /// See Docs/PROGRAMMING-CHALLENGES.md §Challenge 3.
    ///
    /// Turns GREEN in stages as you implement WeakenedEffect:
    ///   1. OnApply                       -> Apply_LowersStrength
    ///   2. OnRemove                      -> Remove_RestoresStrength
    ///   3. "weakened" case in the factory -> Factory_MapsWeakenedName
    ///   4. OnStack                       -> Reapplying_DoesNotDoubleStack
    /// </summary>
    public class WeakenedEffectChallengeTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ApplyEffect auto-creates the StatusEffectsPart, so no part wiring here.
        private static Entity MakeFighter(int strength)
        {
            var e = new Entity { BlueprintName = "fighter" };
            e.Statistics["Strength"] =
                new Stat { Name = "Strength", BaseValue = strength, Min = 1, Max = 30 };
            return e;
        }

        [Test]
        public void Apply_LowersStrength()
        {
            var target = MakeFighter(16);

            target.ApplyEffect(new WeakenedEffect(strPenalty: 3, duration: 4));

            Assert.AreEqual(13, target.GetStat("Strength").Value,
                "Applying Weakened(3) should drop Strength 16 -> 13.");
        }

        [Test]
        public void Remove_RestoresStrength()
        {
            var target = MakeFighter(16);

            target.ApplyEffect(new WeakenedEffect(strPenalty: 3, duration: 4));
            Assert.AreEqual(13, target.GetStat("Strength").Value,
                "Sanity: the penalty must land before we can test its removal.");

            target.RemoveEffect<WeakenedEffect>();

            Assert.AreEqual(16, target.GetStat("Strength").Value,
                "Removing Weakened must restore Strength exactly — OnApply/OnRemove symmetry.");
        }

        [Test]
        public void Factory_MapsWeakenedName_ToWeakenedEffect()
        {
            var specs = OnHitEffectSpec.Parse("Weakened,50,,4,3");
            Assert.IsNotEmpty(specs, "\"Weakened,50,,4,3\" should parse into one spec.");

            Effect fx = OnHitEffectFactory.Create(specs[0], source: null, rng: new System.Random(1));

            Assert.IsInstanceOf<WeakenedEffect>(fx,
                "Add a \"weakened\" case to OnHitEffectFactory.Create that returns a WeakenedEffect.");
            Assert.AreEqual(3, ((WeakenedEffect)fx).StrPenalty,
                "Map the spec's Magnitude (5th field) to StrPenalty.");
        }

        // OnStack: re-applying while already active should refresh duration, NOT
        // stack a second -3. Without OnStack, two effects each apply -3 (Str 10).
        [Test]
        public void Reapplying_DoesNotDoubleStack()
        {
            var target = MakeFighter(16);

            target.ApplyEffect(new WeakenedEffect(strPenalty: 3, duration: 4));
            target.ApplyEffect(new WeakenedEffect(strPenalty: 3, duration: 4));

            Assert.AreEqual(13, target.GetStat("Strength").Value,
                "Two applications should net -3 total, not -6 (OnStack refreshes, doesn't re-apply).");
        }
    }
}
