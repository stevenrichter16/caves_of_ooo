using NUnit.Framework;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 10 §10D — pure-static blueprint matchers used by the
    /// per-blueprint sprite resolver in EnvironmentSpriteRenderer.
    /// These tests pin the exact match contract so a future content
    /// rename / refactor surfaces here, not as a silent visual bug.
    /// </summary>
    public class EnvironmentSpriteRendererBlueprintTests
    {
        // ── BlueprintIsChest ──────────────────────────────────────

        [Test]
        public void Chest_ExactName_Matches()
        {
            // The base "Chest" blueprint in Objects.json.
            Assert.IsTrue(InvokeIsChest("Chest"));
        }

        [Test]
        public void Chest_LockedChest_Matches()
        {
            Assert.IsTrue(InvokeIsChest("LockedChest"));
        }

        [Test]
        public void Chest_MimicChest_Matches()
        {
            Assert.IsTrue(InvokeIsChest("MimicChest"));
        }

        [Test]
        public void Chest_LeatherArmor_DoesNotMatch()
        {
            // Counter-check: armor at `[` must NOT render as a chest.
            Assert.IsFalse(InvokeIsChest("LeatherArmor"));
        }

        [Test]
        public void Chest_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsChest(null));
            Assert.IsFalse(InvokeIsChest(""));
        }

        // ── BlueprintIsLantern ────────────────────────────────────

        [Test]
        public void Lantern_ExactName_Matches()
        {
            Assert.IsTrue(InvokeIsLantern("Lantern"));
        }

        [Test]
        public void Lantern_WatchLantern_Matches()
        {
            // Real blueprint name in the current Objects.json.
            Assert.IsTrue(InvokeIsLantern("WatchLantern"));
        }

        [Test]
        public void Lantern_LanternOil_DoesNotMatch()
        {
            // Adversarial: the FUEL tonic shares the prefix but is
            // not a light source. If this regresses, every potion of
            // lantern oil would suddenly emit a Light2D.
            Assert.IsFalse(InvokeIsLantern("LanternOil"),
                "LanternOil is a tonic, not a light source — must not match.");
        }

        [Test]
        public void Lantern_LanternGroundMarker_DoesNotMatch()
        {
            // Marker entity (placement helper) — also should not
            // light up. Only entities that END with "Lantern" qualify.
            Assert.IsFalse(InvokeIsLantern("LanternGroundMarker"));
        }

        [Test]
        public void Lantern_HealingTonic_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsLantern("HealingTonic"));
        }

        [Test]
        public void Lantern_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsLantern(null));
            Assert.IsFalse(InvokeIsLantern(""));
        }

        // ── Reflection helpers (the matchers are private static) ──

        private static bool InvokeIsChest(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsChest",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }

        private static bool InvokeIsLantern(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsLantern",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }
    }
}
