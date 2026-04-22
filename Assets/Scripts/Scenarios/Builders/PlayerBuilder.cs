using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Builders
{
    /// <summary>
    /// Fluent modification builder for the live player entity. Accessed via
    /// <see cref="ScenarioContext.Player"/> — a lazy-cached singleton per context.
    /// All methods return <c>this</c> for chaining; each method applies its
    /// change immediately (no deferred terminal like <see cref="EntityBuilder"/>).
    ///
    /// Example:
    /// <code>
    /// ctx.Player
    ///    .Teleport(50, 20)
    ///    .SetHpMax()
    ///    .AddMutation("CalmMutation", level: 5)
    ///    .GiveItem("HealingTonic", count: 3)
    ///    .Equip("ChainMail");
    /// </code>
    ///
    /// Fail-soft: every method logs a warning and skips rather than throwing on
    /// unmet preconditions (missing stat, unknown blueprint, etc.). The raw
    /// player Entity is always available as <see cref="ScenarioContext.PlayerEntity"/>
    /// for direct manipulation.
    /// </summary>
    public sealed class PlayerBuilder
    {
        private readonly ScenarioContext _ctx;

        internal PlayerBuilder(ScenarioContext ctx) { _ctx = ctx; }

        // ======================================================
        // Positioning
        // ======================================================

        /// <summary>
        /// Move the player to (x, y) in the current zone. Uses
        /// <c>Zone.AddEntity</c> semantics — this is a TELEPORT, not a walk:
        /// it bypasses <c>BeforeMove</c> / <c>AfterMove</c> events and their
        /// veto logic. Camera follow is automatic via the CameraFollow
        /// MonoBehaviour watching the player's position.
        ///
        /// Logs + skips if (x, y) is out of zone bounds or non-passable.
        /// </summary>
        public PlayerBuilder Teleport(int x, int y)
        {
            if (!_ctx.Zone.InBounds(x, y))
            {
                Debug.LogWarning($"[Scenario] Player.Teleport({x},{y}) is out of zone bounds — skipping.");
                return this;
            }
            var cell = _ctx.Zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable())
            {
                Debug.LogWarning($"[Scenario] Player.Teleport({x},{y}) is not passable — skipping.");
                return this;
            }
            _ctx.Zone.AddEntity(_ctx.PlayerEntity, x, y);
            return this;
        }

        // ======================================================
        // HP
        // ======================================================

        /// <summary>Set player's HP to an absolute value, clamped to [0, Max].</summary>
        public PlayerBuilder SetHp(int hp)
        {
            var stat = _ctx.PlayerEntity.GetStat("Hitpoints");
            if (stat == null) { Debug.LogWarning("[Scenario] Player.SetHp: no Hitpoints stat — skipping."); return this; }
            stat.BaseValue = Mathf.Clamp(hp, 0, stat.Max);
            return this;
        }

        /// <summary>Set player's HP to a fraction of Max (clamped to [0, 1]).</summary>
        public PlayerBuilder SetHpFraction(float fraction)
        {
            var stat = _ctx.PlayerEntity.GetStat("Hitpoints");
            if (stat == null) { Debug.LogWarning("[Scenario] Player.SetHpFraction: no Hitpoints stat — skipping."); return this; }
            fraction = Mathf.Clamp01(fraction);
            stat.BaseValue = Mathf.RoundToInt(stat.Max * fraction);
            return this;
        }

        /// <summary>Fully heal the player — BaseValue ← Max.</summary>
        public PlayerBuilder SetHpMax()
        {
            var stat = _ctx.PlayerEntity.GetStat("Hitpoints");
            if (stat == null) { Debug.LogWarning("[Scenario] Player.SetHpMax: no Hitpoints stat — skipping."); return this; }
            stat.BaseValue = stat.Max;
            return this;
        }

        // ======================================================
        // Stats
        // ======================================================

        /// <summary>
        /// Set a stat's <see cref="Stat.BaseValue"/>. Clamped to [Min, Max] at apply.
        /// If the stat doesn't exist, logs + skips. For values above the stat's
        /// Max (default 30 for most stats), call <see cref="SetStatMax"/> first.
        /// </summary>
        public PlayerBuilder SetStat(string statName, int value)
        {
            var stat = _ctx.PlayerEntity.GetStat(statName);
            if (stat == null)
            {
                Debug.LogWarning($"[Scenario] Player.SetStat('{statName}'): stat not found — skipping.");
                return this;
            }
            stat.BaseValue = Mathf.Clamp(value, stat.Min, stat.Max);
            return this;
        }

        /// <summary>Raise (or lower) a stat's <see cref="Stat.Max"/> ceiling.</summary>
        public PlayerBuilder SetStatMax(string statName, int max)
        {
            var stat = _ctx.PlayerEntity.GetStat(statName);
            if (stat == null)
            {
                Debug.LogWarning($"[Scenario] Player.SetStatMax('{statName}'): stat not found — skipping.");
                return this;
            }
            stat.Max = max;
            return this;
        }

        // ======================================================
        // Mutations
        // ======================================================

        /// <summary>
        /// Grant a mutation to the player by CLASS NAME (not DisplayName or blueprint
        /// Name) — e.g., <c>"FireBoltMutation"</c>, not <c>"FireBolt"</c>. Uses
        /// <see cref="MutationsPart.AddMutation"/>'s reflection lookup on
        /// <c>Type.Name</c>.
        ///
        /// Default level is 3 (boosted relative to blueprint starting-mutation level 1).
        /// If the player already has the mutation, <c>IRankedMutation</c> implementations
        /// stack level; non-ranked mutations no-op with a false return.
        ///
        /// Logs + skips if the player has no <see cref="MutationsPart"/> or the
        /// class name doesn't resolve. MutationsPart itself logs when the class
        /// isn't found, so tests may need <c>LogAssert.Expect</c> for that log.
        /// </summary>
        public PlayerBuilder AddMutation(string mutationClassName, int level = 3)
        {
            var mutations = _ctx.PlayerEntity.GetPart<MutationsPart>();
            if (mutations == null)
            {
                Debug.LogWarning($"[Scenario] Player.AddMutation('{mutationClassName}'): player has no MutationsPart — skipping.");
                return this;
            }
            bool added = mutations.AddMutation(mutationClassName, level);
            if (!added)
                Debug.LogWarning($"[Scenario] Player.AddMutation('{mutationClassName}'): MutationsPart.AddMutation returned false (unknown class or already present + non-ranked).");
            return this;
        }

        // ======================================================
        // Inventory
        // ======================================================

        /// <summary>
        /// Spawn the named item blueprint and add to the player's carried inventory.
        /// For <paramref name="count"/> &gt; 1, spawns that many distinct items;
        /// stackable items auto-merge via <see cref="StackerPart"/>.
        /// Logs + skips on unknown blueprint or missing InventoryPart.
        /// </summary>
        public PlayerBuilder GiveItem(string itemBlueprintName, int count = 1)
        {
            if (count <= 0) return this;
            var inventory = _ctx.PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null)
            {
                Debug.LogWarning($"[Scenario] Player.GiveItem: player has no InventoryPart — skipping.");
                return this;
            }
            for (int i = 0; i < count; i++)
            {
                var item = _ctx.Factory.CreateEntity(itemBlueprintName);
                if (item == null)
                {
                    Debug.LogWarning($"[Scenario] Player.GiveItem: blueprint '{itemBlueprintName}' not found — skipping remaining {count - i}.");
                    break;
                }
                if (!inventory.AddObject(item))
                {
                    Debug.LogWarning($"[Scenario] Player.GiveItem: AddObject failed for '{itemBlueprintName}' (weight limit?) — skipping remaining.");
                    break;
                }
            }
            return this;
        }

        /// <summary>
        /// Spawn a single item and equip it on the player (via
        /// <see cref="InventorySystem.Equip"/>). Shortcut for <c>GiveItem(1)</c>
        /// followed by an equip call on that specific item. Logs + skips if
        /// the item can't be equipped (no matching body slot).
        /// </summary>
        public PlayerBuilder Equip(string itemBlueprintName)
        {
            var inventory = _ctx.PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null)
            {
                Debug.LogWarning($"[Scenario] Player.Equip: player has no InventoryPart — skipping.");
                return this;
            }
            var item = _ctx.Factory.CreateEntity(itemBlueprintName);
            if (item == null)
            {
                Debug.LogWarning($"[Scenario] Player.Equip: blueprint '{itemBlueprintName}' not found — skipping.");
                return this;
            }
            inventory.AddObject(item);
            if (!InventorySystem.Equip(_ctx.PlayerEntity, item))
                Debug.LogWarning($"[Scenario] Player.Equip: InventorySystem.Equip failed for '{itemBlueprintName}' — item left carried.");
            return this;
        }

        /// <summary>
        /// Remove all carried items from the player's inventory. Does NOT unequip
        /// items currently equipped on body parts — for those, the items are in
        /// the body part tree, not the carried list.
        /// </summary>
        public PlayerBuilder ClearInventory()
        {
            var inventory = _ctx.PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null)
            {
                Debug.LogWarning($"[Scenario] Player.ClearInventory: player has no InventoryPart — skipping.");
                return this;
            }
            // Copy to avoid mutating the list while iterating
            var carried = new List<Entity>(inventory.Objects);
            foreach (var item in carried)
                inventory.RemoveObject(item);
            return this;
        }

        // ======================================================
        // Faction reputation
        // ======================================================

        /// <summary>
        /// Set the player's reputation with the named faction to an absolute value.
        /// <see cref="PlayerReputation"/> clamps to its internal [-200, 200] range.
        /// </summary>
        public PlayerBuilder SetFactionReputation(string factionName, int value)
        {
            if (string.IsNullOrEmpty(factionName))
            {
                Debug.LogWarning("[Scenario] Player.SetFactionReputation: factionName is null/empty — skipping.");
                return this;
            }
            PlayerReputation.Set(factionName, value);
            return this;
        }

        /// <summary>
        /// Adjust the player's reputation with the named faction by a delta.
        /// Silent — no MessageLog noise during scenario setup.
        /// </summary>
        public PlayerBuilder ModifyFactionReputation(string factionName, int delta)
        {
            if (string.IsNullOrEmpty(factionName))
            {
                Debug.LogWarning("[Scenario] Player.ModifyFactionReputation: factionName is null/empty — skipping.");
                return this;
            }
            PlayerReputation.Modify(factionName, delta, silent: true);
            return this;
        }
    }
}
