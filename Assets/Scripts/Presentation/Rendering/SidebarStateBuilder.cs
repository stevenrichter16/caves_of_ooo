using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure data snapshot consumed by the gameplay sidebar renderer.
    /// </summary>
    public static class SidebarStateBuilder
    {
        public const int SidebarLogMessageLimit = 30;

        public static SidebarSnapshot Build(
            Entity player,
            Zone zone,
            LookSnapshot currentLookSnapshot,
            int maxRecentMessages = SidebarLogMessageLimit,
            bool showThoughts = false)
        {
            var inventoryState = InventoryScreenData.Build(player);

            var vitalLines = new List<string>(4)
            {
                ComposeDualLine("HP", FindStat(inventoryState, "HP", "0"), "MP", FindStat(inventoryState, "MP", "-")),
                ComposeDualLine("LV", FindStat(inventoryState, "LV", GetLevel(player).ToString()), "XP", GetXpLine(player)),
                ComposeDualLine("AV", FindStat(inventoryState, "AV", "0"), "DV", FindStat(inventoryState, "DV", "0")),
                ComposeDualLine(
                    "WT",
                    inventoryState.CarriedWeight + "/" + inventoryState.MaxCarryWeight,
                    "DR",
                    inventoryState.Drams.ToString())
            };

            string statusText = BuildStatusText(player);
            LookSnapshot focusSnapshot = currentLookSnapshot ?? BuildFallbackFocus(player, zone);
            IReadOnlyList<SidebarLogEntry> logEntries = BuildRecentLogEntries(maxRecentMessages);

            // Phase 10 — when the 't' toggle is on, populate thought entries
            // so the sidebar renderer swaps LOG for THOUGHTS in the bottom
            // panel. Null (default) → LOG mode. Populated → THOUGHTS mode.
            IReadOnlyList<SidebarThoughtEntry> thoughtEntries =
                showThoughts ? BuildThoughtEntries(zone) : null;

            return new SidebarSnapshot(
                vitalLines, statusText, focusSnapshot, logEntries, thoughtEntries);
        }

        /// <summary>
        /// Gather one <see cref="SidebarThoughtEntry"/> per Creature-tagged
        /// entity with a <see cref="BrainPart"/>, sorted alphabetically by
        /// display name so the panel doesn't jitter as entities tick in
        /// different orders between turns.
        /// </summary>
        private static IReadOnlyList<SidebarThoughtEntry> BuildThoughtEntries(Zone zone)
        {
            var result = new List<SidebarThoughtEntry>();
            if (zone == null) return result;

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity == null) continue;
                if (!entity.HasTag("Creature")) continue;
                var brain = entity.GetPart<BrainPart>();
                if (brain == null) continue;

                string name = entity.GetDisplayName() ?? entity.BlueprintName ?? "?";
                result.Add(new SidebarThoughtEntry(name, brain.LastThought ?? string.Empty));
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }

        private static string FindStat(InventoryScreenData.ScreenState state, string label, string fallback)
        {
            if (state?.PlayerStats != null)
            {
                for (int i = 0; i < state.PlayerStats.Count; i++)
                {
                    InventoryScreenData.StatDisplay stat = state.PlayerStats[i];
                    if (stat != null && string.Equals(stat.Label, label, StringComparison.OrdinalIgnoreCase))
                        return string.IsNullOrWhiteSpace(stat.Value) ? fallback : stat.Value;
                }
            }

            return fallback;
        }

        private static int GetLevel(Entity player)
        {
            return player?.GetStatValue("Level", 1) ?? 1;
        }

        private static string GetXpLine(Entity player)
        {
            int level = GetLevel(player);
            int xp = player?.GetStat("Experience")?.Value ?? 0;
            int next = LevelingSystem.XPToNextLevel(level);
            return xp + "/" + next;
        }

        private static string ComposeDualLine(string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            return leftLabel + " " + (leftValue ?? "-") + " | " + rightLabel + " " + (rightValue ?? "-");
        }

        private static string BuildStatusText(Entity player)
        {
            var effectsPart = player?.GetPart<StatusEffectsPart>();
            if (effectsPart == null || effectsPart.EffectCount <= 0)
                return "-";

            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<Effect> effects = effectsPart.GetAllEffects();
            for (int i = 0; i < effects.Count; i++)
            {
                string name = effects[i]?.DisplayName;
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    continue;

                names.Add(name);
            }

            if (names.Count == 0)
                return "-";

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", names);
        }

        private static LookSnapshot BuildFallbackFocus(Entity player, Zone zone)
        {
            if (player == null || zone == null)
            {
                return new LookSnapshot(
                    -1,
                    -1,
                    "No focus",
                    "There is nothing to inspect.",
                    new List<string>(),
                    null,
                    null);
            }

            (int x, int y) = zone.GetEntityPosition(player);
            if (!zone.InBounds(x, y))
            {
                return new LookSnapshot(
                    -1,
                    -1,
                    "No focus",
                    "There is nothing to inspect.",
                    new List<string>(),
                    null,
                    null);
            }

            return LookQueryService.BuildSnapshot(player, zone, x, y);
        }

        private static IReadOnlyList<SidebarLogEntry> BuildRecentLogEntries(int maxRecentMessages)
        {
            var raw = MessageLog.GetRecentEntries(maxRecentMessages);
            var entries = new List<SidebarLogEntry>();
            if (raw.Count == 0)
                return entries;

            for (int i = raw.Count - 1; i >= 0; i--)
            {
                string currentText = raw[i].Text;
                int currentTick = raw[i].Tick;
                int currentSerial = raw[i].Serial;
                int count = 1;

                while (i - 1 >= 0 && string.Equals(raw[i - 1].Text, currentText, StringComparison.Ordinal))
                {
                    count++;
                    i--;
                }

                entries.Add(new SidebarLogEntry(currentText, currentTick, count, currentSerial));
            }

            return entries;
        }
    }
}
