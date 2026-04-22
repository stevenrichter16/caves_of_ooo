using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3c — player-specific assertion chain. Obtained via
    /// <c>ctx.Verify().Player()</c>. Complements <see cref="EntityVerifier"/>
    /// with player-only concerns (mutations, inventory, equipment, faction rep).
    ///
    /// Shares position/HP/stat assertions with EntityVerifier — duplicated
    /// rather than inherited so the failure messages can reference "player"
    /// instead of the entity's DisplayName.
    /// </summary>
    public sealed class PlayerVerifier
    {
        private readonly ScenarioVerifier _root;
        private Entity Player => _root.Ctx.PlayerEntity;

        internal PlayerVerifier(ScenarioVerifier root) { _root = root; }

        /// <summary>Return to the root verifier.</summary>
        public ScenarioVerifier Back() => _root;

        // =========================================================
        // Position & vitals
        // =========================================================

        public PlayerVerifier IsAt(int x, int y)
        {
            var pos = _root.Ctx.Zone.GetEntityPosition(Player);
            if (pos.x != x || pos.y != y)
                Assert.Fail($"Verify.Player.IsAt({x},{y}): expected ({x},{y}), got ({pos.x},{pos.y}).");
            return this;
        }

        /// <summary>Assert player is NOT at <paramref name="x"/>, <paramref name="y"/>.</summary>
        public PlayerVerifier IsNotAt(int x, int y)
        {
            var pos = _root.Ctx.Zone.GetEntityPosition(Player);
            if (pos.x == x && pos.y == y)
                Assert.Fail($"Verify.Player.IsNotAt({x},{y}): player IS at ({x},{y}).");
            return this;
        }

        // =========================================================
        // Parts & tags (parallels EntityVerifier)
        // =========================================================

        /// <summary>Assert the player has a part of type <typeparamref name="T"/>.</summary>
        public PlayerVerifier HasPartOfType<T>() where T : Part
        {
            if (Player.GetPart<T>() == null)
                Assert.Fail($"Verify.Player.HasPartOfType<{typeof(T).Name}>: part not attached.");
            return this;
        }

        /// <summary>Assert the player does NOT have a part of type <typeparamref name="T"/>.</summary>
        public PlayerVerifier HasNoPartOfType<T>() where T : Part
        {
            if (Player.GetPart<T>() != null)
                Assert.Fail(
                    $"Verify.Player.HasNoPartOfType<{typeof(T).Name}>: " +
                    "part IS attached but shouldn't be.");
            return this;
        }

        /// <summary>Assert the player carries the given tag key.</summary>
        public PlayerVerifier HasTag(string tag)
        {
            if (!Player.HasTag(tag))
                Assert.Fail($"Verify.Player.HasTag('{tag}'): tag not present.");
            return this;
        }

        /// <summary>Assert the player does NOT carry the given tag key.</summary>
        public PlayerVerifier DoesNotHaveTag(string tag)
        {
            if (Player.HasTag(tag))
                Assert.Fail($"Verify.Player.DoesNotHaveTag('{tag}'): tag IS present.");
            return this;
        }

        public PlayerVerifier HasHpFraction(float fraction, float tolerance = 0.05f)
        {
            var stat = Player.GetStat("Hitpoints");
            if (stat == null)
                Assert.Fail("Verify.Player.HasHpFraction: player has no Hitpoints stat.");
            float actual = (float)stat.BaseValue / stat.Max;
            if (System.Math.Abs(actual - fraction) > tolerance)
                Assert.Fail(
                    $"Verify.Player.HasHpFraction({fraction}): " +
                    $"expected ~{fraction:F2} (±{tolerance:F2}), got {actual:F3} " +
                    $"({stat.BaseValue}/{stat.Max} HP).");
            return this;
        }

        public PlayerVerifier HasStatAtLeast(string statName, int min)
        {
            var stat = Player.GetStat(statName);
            if (stat == null)
                Assert.Fail($"Verify.Player.HasStatAtLeast('{statName}'): stat not present.");
            if (stat.BaseValue < min)
                Assert.Fail(
                    $"Verify.Player.HasStatAtLeast('{statName}', {min}): " +
                    $"expected ≥ {min}, got {stat.BaseValue}.");
            return this;
        }

        // =========================================================
        // Mutations
        // =========================================================

        /// <summary>
        /// Assert the player has a mutation with the given class name (matches
        /// <c>Type.Name</c> — e.g. <c>"FireBoltMutation"</c>, not
        /// <c>"FireBolt"</c>).
        /// </summary>
        public PlayerVerifier HasMutation(string mutationClassName)
        {
            var mutations = Player.GetPart<MutationsPart>();
            if (mutations == null)
                Assert.Fail("Verify.Player.HasMutation: player has no MutationsPart.");
            if (!mutations.HasMutation(mutationClassName))
                Assert.Fail(
                    $"Verify.Player.HasMutation('{mutationClassName}'): " +
                    "mutation not attached to player.");
            return this;
        }

        // =========================================================
        // Inventory & equipment
        // =========================================================

        /// <summary>
        /// Assert the player's carried inventory contains at least one item
        /// matching <paramref name="blueprintName"/>. Stacked items count
        /// once (the assertion is "is one present", not "how many").
        /// </summary>
        public PlayerVerifier HasItemInInventory(string blueprintName)
        {
            var inv = Player.GetPart<InventoryPart>();
            if (inv == null)
                Assert.Fail("Verify.Player.HasItemInInventory: player has no InventoryPart.");
            foreach (var item in inv.Objects)
                if (item != null && item.BlueprintName == blueprintName) return this;
            Assert.Fail(
                $"Verify.Player.HasItemInInventory('{blueprintName}'): " +
                "no matching item in carried inventory.");
            return this;
        }

        /// <summary>
        /// Assert the player has an item with the given blueprint equipped on
        /// any body-part slot.
        /// </summary>
        public PlayerVerifier HasEquipped(string blueprintName)
        {
            var inv = Player.GetPart<InventoryPart>();
            if (inv == null)
                Assert.Fail("Verify.Player.HasEquipped: player has no InventoryPart.");
            foreach (var kvp in inv.EquippedItems)
                if (kvp.Value != null && kvp.Value.BlueprintName == blueprintName) return this;
            Assert.Fail(
                $"Verify.Player.HasEquipped('{blueprintName}'): " +
                "no matching item equipped on any body slot.");
            return this;
        }

        // =========================================================
        // Faction reputation
        // =========================================================

        public PlayerVerifier HasFactionRep(string factionName, int expected)
        {
            int actual = PlayerReputation.Get(factionName);
            if (actual != expected)
                Assert.Fail(
                    $"Verify.Player.HasFactionRep('{factionName}', {expected}): " +
                    $"expected {expected}, got {actual}.");
            return this;
        }

        public PlayerVerifier HasFactionRepAtLeast(string factionName, int min)
        {
            int actual = PlayerReputation.Get(factionName);
            if (actual < min)
                Assert.Fail(
                    $"Verify.Player.HasFactionRepAtLeast('{factionName}', {min}): " +
                    $"expected ≥ {min}, got {actual}.");
            return this;
        }
    }
}
