using System.Runtime.CompilerServices;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Explicit, transient player damage/death immunity. Weak object identity
    /// keeps this debug preference out of saves, tags and replacement players.
    /// Intentional nonlethal resource costs and status effects remain native.
    /// </summary>
    public static class DebugInvincibility
    {
        private sealed class EnabledState { }
        private static readonly ConditionalWeakTable<Entity, EnabledState> EnabledPlayers =
            new ConditionalWeakTable<Entity, EnabledState>();

        /// <summary>Only this exact player object can own its runtime protection.</summary>
        public static bool IsEnabled(Entity player)
            => player != null && player.HasTag("Player") && EnabledPlayers.TryGetValue(player, out _);

        /// <summary>Toggle a living player without healing, changing stats or spending
        /// a turn. Invalid/dead actors are rejected; this is not a resurrection tool.</summary>
        public static bool TryToggle(Entity player, out bool enabled)
        {
            enabled = false;
            var hp = player?.GetStat("Hitpoints");
            if (player == null || !player.HasTag("Player") || hp == null
                || hp.BaseValue <= 0 || hp.Value <= 0 || CombatSystem.IsDeathHandled(player))
            {
                Diag.Record("event", "DebugInvincibilityRejected", actor: player,
                    payload: new { reason = "living_player_required" });
                return false;
            }

            enabled = !IsEnabled(player);
            if (enabled) EnabledPlayers.Add(player, new EnabledState());
            else EnabledPlayers.Remove(player);
            Diag.Record("event", "DebugInvincibilityToggled", actor: player,
                payload: new { enabled });
            return true;
        }

        // Call before damage/death/anatomy mutation; the disabled hot path
        // allocates nothing and never dispatches damage callbacks.
        internal static bool Blocks(Entity target, string operation, Entity source = null)
        {
            if (!IsEnabled(target)) return false;
            if (Diag.IsChannelEnabled("damage"))
                Diag.Record("damage", "DebugInvincibilityBlocked", actor: source, target: target,
                    payload: new { operation });
            return true;
        }
    }
}
