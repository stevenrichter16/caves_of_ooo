using System;

namespace CavesOfOoo.Core
{
    /// <summary>Saved cosmetic action count and shared ambient-message spacing.
    /// Does not advance world time or consume any gameplay RNG stream.</summary>
    public static class WorldAmbience
    {
        public const string TurnProperty = "AmbientPlayerTurns";
        public const string LastMessageProperty = "AmbientLastMessageTurn";
        public const string HasMessageProperty = "AmbientHasMessage";
        public const int MinimumGap = 20;

        /// <summary>Called once after a player's EndTurn cleanup, including a
        /// blocked action. NPC, dead, removed and foreign-zone actors do nothing.</summary>
        public static void OnPlayerTurnEnded(Entity player, Zone zone)
        {
            if (!IsPresentPlayer(player, zone)) return;
            int old = player.GetIntProperty(TurnProperty);
            int turn = old < 0 || old == int.MaxValue ? 0 : old + 1;
            // Cosmetic save bookkeeping deliberately bypasses gameplay fact-change
            // events; no storylet or stat observes these implementation keys.
            player.IntProperties[TurnProperty] = turn;
            if (SariAmbience.OnZoneEntered(player, zone)) return;
            if (MayEmit(player) && !SariAmbience.TryPeriodic(player, zone, turn))
                WorldRemarks.TryEmit(player, zone);
        }

        internal static bool IsPresentPlayer(Entity player, Zone zone)
        {
            if (player == null || zone == null || player.IntProperties == null || !player.HasTag("Player")
                || CombatSystem.IsDeathHandled(player) || player.GetStatValue("Hitpoints", 0) <= 0
                || !ReferenceEquals(player.SpatialZone, zone)) return false;
            var cell = zone.GetEntityCell(player);
            return cell != null && cell.Objects.Contains(player);
        }

        internal static bool MayEmit(Entity player)
        {
            if (player.GetIntProperty(HasMessageProperty) == 0) return true;
            int turn = player.GetIntProperty(TurnProperty), last = player.GetIntProperty(LastMessageProperty);
            if (turn < 0) return false;
            // An invalid saved timestamp cannot permanently silence the world.
            // The next real emission writes a valid timestamp and restores spacing.
            if (last < 0) return true;
            long elapsed = turn >= last ? (long)turn - last : (long)int.MaxValue + 1 - last + turn;
            return elapsed >= MinimumGap;
        }

        internal static void MarkEmission(Entity player)
        {
            player.IntProperties[HasMessageProperty] = 1;
            player.IntProperties[LastMessageProperty] = Math.Max(0, player.GetIntProperty(TurnProperty));
        }
    }
}
