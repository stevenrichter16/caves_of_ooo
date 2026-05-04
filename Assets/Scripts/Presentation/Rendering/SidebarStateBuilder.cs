using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure data snapshot consumed by the gameplay sidebar renderer.
    /// </summary>
    public static class SidebarStateBuilder
    {
        public const int SidebarLogMessageLimit = 30;

        /// <summary>
        /// Scratch buffer reused across <see cref="Build"/> calls so the
        /// per-frame sidebar refresh doesn't allocate a fresh
        /// <c>List&lt;MessageLog.Entry&gt;</c>. Cleared at the start of each
        /// <see cref="BuildRecentLogEntries"/> call. Safe because the
        /// returned <see cref="SidebarLogEntry"/> list is consumed
        /// synchronously by <c>SidebarRenderer.Render</c> before the next
        /// frame's Build call overwrites the buffer. Tier-B Fix #4.
        /// </summary>
        private static readonly List<MessageLog.Entry> _getRecentEntriesScratch =
            new List<MessageLog.Entry>(SidebarLogMessageLimit);

        /// <summary>
        /// Pre-allocated entry list returned by
        /// <see cref="BuildRecentLogEntries"/>. Reused per call (cleared
        /// first). Same lifetime contract as <see cref="_getRecentEntriesScratch"/>.
        /// </summary>
        private static readonly List<SidebarLogEntry> _logEntryScratch =
            new List<SidebarLogEntry>(SidebarLogMessageLimit);

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

        /// <summary>
        /// Reusable scratch buffer for <see cref="ComposeDualLine"/>. Avoids
        /// the 5-fragment string-concatenation chain that would allocate
        /// 4 intermediate strings per vital line × 4 lines per sidebar
        /// build × every-frame Build = significant GC pressure. The
        /// builder is cleared at the start of each compose call.
        /// </summary>
        private static readonly StringBuilder _dualLineScratch = new StringBuilder(64);

        private static string ComposeDualLine(string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            _dualLineScratch.Clear();
            _dualLineScratch.Append(leftLabel).Append(' ').Append(leftValue ?? "-")
                            .Append(" | ")
                            .Append(rightLabel).Append(' ').Append(rightValue ?? "-");
            return _dualLineScratch.ToString();
        }

        // Status-text cache: skip the sort + join when the effect set hasn't
        // changed since the last frame. Idle play has the same effects every
        // frame; combat changes them rarely (one effect-tick per turn).
        // Comparing the per-effect TickEnds + display names catches both
        // adds, removes, and tick decrements without rebuilding the string.
        private static readonly List<string> _statusNamesScratch = new List<string>(8);
        private static readonly HashSet<string> _statusSeenScratch =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _statusTextCache;
        private static int _statusEffectsCacheHash;

        private static string BuildStatusText(Entity player)
        {
            var effectsPart = player?.GetPart<StatusEffectsPart>();
            if (effectsPart == null || effectsPart.EffectCount <= 0)
                return "-";

            IReadOnlyList<Effect> effects = effectsPart.GetAllEffects();

            // Cheap hash over (count, name, duration) to catch both list
            // changes and visible state mutations (effect ticked down →
            // duration shifted). False positives are fine — they just
            // rebuild the string. False negatives would show stale status
            // text; the per-effect Duration update on each tick prevents
            // that.
            int hash = effects.Count;
            unchecked
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var e = effects[i];
                    if (e == null) continue;
                    hash = hash * 31 + (e.DisplayName != null ? e.DisplayName.GetHashCode() : 0);
                    hash = hash * 31 + e.Duration;
                }
            }
            if (hash == _statusEffectsCacheHash && _statusTextCache != null)
                return _statusTextCache;

            // Cache miss — rebuild. Reuse scratch list and hash set.
            var names = _statusNamesScratch;
            var seen = _statusSeenScratch;
            names.Clear();
            seen.Clear();
            for (int i = 0; i < effects.Count; i++)
            {
                string name = effects[i]?.DisplayName;
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    continue;
                names.Add(name);
            }

            string result = names.Count == 0 ? "-" : string.Join(", ", names);
            _statusTextCache = result;
            _statusEffectsCacheHash = hash;
            return result;
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
            var raw = _getRecentEntriesScratch;
            MessageLog.GetRecentEntries(maxRecentMessages, raw);

            var entries = _logEntryScratch;
            entries.Clear();
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
