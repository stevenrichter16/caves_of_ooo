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
        public static SidebarSnapshot Build(
            Entity player,
            Zone zone,
            LookSnapshot currentLookSnapshot,
            int maxRecentMessages = 96)
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

            return new SidebarSnapshot(vitalLines, statusText, focusSnapshot, logEntries);
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
            int xp = player?.GetStatValue("XP", 0) ?? 0;
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
                int count = 1;

                while (i - 1 >= 0 && string.Equals(raw[i - 1].Text, currentText, StringComparison.Ordinal))
                {
                    count++;
                    i--;
                }

                entries.Add(new SidebarLogEntry(currentText, currentTick, count));
            }

            return entries;
        }
    }
}
