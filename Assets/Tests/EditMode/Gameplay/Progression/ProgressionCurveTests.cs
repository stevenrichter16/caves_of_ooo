using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P1 (Docs/PLACE-WORK-AND-PROGRESSION.md §3): the target progression curve —
    /// tier level bands, the level-to-XP rule and Qud's level-band XP scaling.
    /// </summary>
    public sealed class ProgressionCurveTests
    {
        // ════════════════ the curve itself ════════════════

        [Test] public void TierBandsAreContiguousAndClimb()
        {
            for (int t = 1; t <= ProgressionCurve.MaxTier; t++)
            {
                var c = ProgressionCurve.CreatureLevelBand(t); var p = ProgressionCurve.PlayerLevelBand(t);
                Assert.Less(c.Min, c.Max, "tier " + t); Assert.Less(p.Min, p.Max, "tier " + t);
                if (t > 1)
                {
                    Assert.AreEqual(ProgressionCurve.CreatureLevelBand(t - 1).Max, c.Min, "creature bands meet at tier " + t);
                    Assert.AreEqual(ProgressionCurve.PlayerLevelBand(t - 1).Max, p.Min, "player bands meet at tier " + t);
                }
            }
            Assert.AreEqual(1, ProgressionCurve.PlayerLevelBand(1).Min, "the game starts at level 1");
        }

        [Test] public void XPValueGrowsWithLevelAndIsAlwaysPositive()
        {
            int previous = 0;
            for (int level = 1; level <= 20; level++)
            {
                int xp = ProgressionCurve.XPValueForLevel(level);
                Assert.Greater(xp, previous, "level " + level);
                previous = xp;
            }
        }

        [Test] public void CumulativeXPMatchesTheLevelingCurve()
        {
            Assert.AreEqual(0, ProgressionCurve.CumulativeXPToReach(1));
            Assert.AreEqual(LevelingSystem.XPToNextLevel(1), ProgressionCurve.CumulativeXPToReach(2));
            Assert.AreEqual(LevelingSystem.XPToNextLevel(1) + LevelingSystem.XPToNextLevel(2), ProgressionCurve.CumulativeXPToReach(3));
        }

        // Qud parity (Experience.cs:33-50): band = level / 5; one band above halves,
        // two bands a tenth, three or more nothing; equal or lower pays in full.
        [TestCase(4, 4, 100, 100)]   // same band
        [TestCase(1, 12, 100, 100)]  // killer below victim: full
        [TestCase(5, 4, 100, 50)]    // one band above
        [TestCase(10, 4, 100, 10)]   // two bands above
        [TestCase(15, 4, 100, 0)]    // three bands above
        [TestCase(9, 5, 100, 100)]   // counter-check: 9 and 5 share band 1
        [TestCase(5, 4, 0, 0)]       // nothing to scale
        public void KillXPScalesByLevelBandLikeQud(int killer, int victim, int xp, int expected)
            => Assert.AreEqual(expected, ProgressionCurve.ScaleKillXP(xp, killer, victim));

        [Test] public void ACreatureWithoutATierTagIsTreatedAsTierOne()
        {
            var e = new Entity { BlueprintName = "Probe" };
            Assert.AreEqual(1, ProgressionCurve.CreatureTier(e));
            e.SetTag("Tier", "4");
            Assert.AreEqual(4, ProgressionCurve.CreatureTier(e), "counter-check: an authored tier is read");
            e.SetTag("Tier", "9");
            Assert.AreEqual(ProgressionCurve.MaxTier, ProgressionCurve.CreatureTier(e), "clamped to the top tier");
        }
    }
}
