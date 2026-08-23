using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5 cold-eye follow-up — the substrate vault is one bordered
    /// 16×16 tile stamped up to 272 times per Cathedral floor, and a
    /// single repeated tile at that count reads as wallpaper, not as
    /// grown material. The fixture tier gains position-hashed variants
    /// (the same mechanism floors already use): a blueprint listed in
    /// <c>FixtureVariantCounts</c> loads <c>file, file_v1..file_vN-1</c>
    /// and picks one per cell, deterministically, so the choice
    /// survives reloads.
    /// </summary>
    public class FixtureVariantTests
    {
        [Test]
        public void TheVaultHasVariants()
        {
            // The point of the exercise: more than one face.
            Assert.IsTrue(
                EnvironmentSpriteRenderer.FixtureVariantCounts.TryGetValue(
                    "SubstrateVault", out int n) && n >= 2,
                "SubstrateVault must declare at least 2 fixture variants");
        }

        [Test]
        public void EveryDeclaredVariantFileActuallyLoads()
        {
            // A declared count with a missing file would silently fall
            // back per-cell; the declaration must be backed by art.
            foreach (var kvp in EnvironmentSpriteRenderer.FixtureVariantCounts)
            {
                string file = null;
                foreach (var (bp, f) in EnvironmentSpriteRenderer.FixtureSprites)
                    if (bp == kvp.Key) file = f;
                Assert.IsNotNull(file,
                    kvp.Key + " declares variants but has no FixtureSprites entry");

                // v0 is the base file itself; v1..vN-1 are suffixed.
                Assert.IsNotNull(
                    Resources.Load<Sprite>("Sprites/Environment/" + file),
                    kvp.Key + " base sprite must load");
                for (int v = 1; v < kvp.Value; v++)
                    Assert.IsNotNull(
                        Resources.Load<Sprite>("Sprites/Environment/" + file + "_v" + v),
                        kvp.Key + " variant _v" + v + " must load");
            }
        }

        [Test]
        public void VariantChoiceIsDeterministicPerCell()
        {
            // Same (x, y) → same index across calls: the vault must not
            // shimmer between frames or differ across reloads.
            for (int x = 0; x < 80; x += 7)
                for (int y = 0; y < 25; y += 3)
                    Assert.AreEqual(
                        EnvironmentSpriteRenderer.FixtureVariantIndex(x, y, 4),
                        EnvironmentSpriteRenderer.FixtureVariantIndex(x, y, 4));
        }

        [Test]
        public void VariantChoiceActuallyVaries()
        {
            // Counter-check: a hash that returned 0 everywhere would
            // pass determinism and change nothing on screen. Across a
            // Cathedral-sized band the choice must hit more than one
            // face.
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int x = 1; x < 79; x++)
                for (int y = 1; y < 24; y++)
                    seen.Add(EnvironmentSpriteRenderer.FixtureVariantIndex(x, y, 4));
            Assert.Greater(seen.Count, 1, "one face everywhere = wallpaper");
            foreach (int v in seen)
                Assert.That(v, Is.InRange(0, 3), "index out of declared range");
        }

        [Test]
        public void UndeclaredFixturesAreUntouched()
        {
            // Counter-check: the beehive declares no variants and must
            // keep its single face — count 0 / absent means the flat
            // lookup path, exactly as before this change.
            Assert.IsFalse(
                EnvironmentSpriteRenderer.FixtureVariantCounts.ContainsKey("Beehive"),
                "only blueprints that shipped variant art may declare counts");
        }
    }
}
