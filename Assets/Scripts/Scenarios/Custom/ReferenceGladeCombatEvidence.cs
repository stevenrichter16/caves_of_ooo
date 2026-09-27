using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Read-only witness validation for a single real keyboard window.
    /// The marker must remain in the diagnostic ring; NPC wounds, old records,
    /// missing causes and uncorrelated deaths cannot stand in for player combat.</summary>
    public static class ReferenceGladeCombatEvidence
    {
        public const string MarkerKind = "ReferenceGladeCombatKeyStart";
        public sealed class Result
        {
            public bool WindowValid, PlayerAttempt, PlayerDamage, PlayerLethal, HostileAttempt;
            public string DamageCause;
        }

        public static Result Inspect(IReadOnlyList<Diag.Entry> rows, string marker, string player, string target)
        {
            var result = new Result();
            if (rows == null || string.IsNullOrEmpty(marker) || string.IsNullOrEmpty(player)
                || string.IsNullOrEmpty(target) || player == target) return result;
            int start = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].TraceId != marker) continue;
                if (start >= 0 || rows[i].Category != "scenario" || rows[i].Kind != MarkerKind
                    || rows[i].ActorId != player || rows[i].TargetId != target) return result;
                start = i;
            }
            if (start < 0) return result;
            result.WindowValid = true;
            var attacks = new HashSet<string>(StringComparer.Ordinal);
            for (int i = start + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Category != "damage" || string.IsNullOrEmpty(row.TraceId)
                    || string.IsNullOrEmpty(row.CauseTraceId)) continue;
                if (row.Kind == "HitRoll" && row.ActorId == target && row.TargetId == player)
                    result.HostileAttempt = true;
                if (row.ActorId != player || row.TargetId != target) continue;
                if (row.Kind == "HitRoll")
                {
                    result.PlayerAttempt = true;
                    attacks.Add(row.CauseTraceId);
                }
                else if (row.Kind == "DamageDealt" && attacks.Contains(row.CauseTraceId)
                    && TryReadPositiveDamage(row.PayloadJson, out bool lethal))
                {
                    result.PlayerDamage = true;
                    result.PlayerLethal |= lethal;
                    result.DamageCause = row.CauseTraceId;
                }
            }
            return result;
        }

        public const string SupportMarkerKind = "ReferenceGladeCombatSupportStart";

        /// <summary>A fresh exact self-use event and loss of one original
        /// carried unit prove tonic consumption; HP alone cannot, because
        /// ordinary hostile turns may deal damage during the same action.</summary>
        public static bool HasConsumedTonic(IReadOnlyList<Diag.Entry> rows, string marker,
            string player, string item, int beforeUnits, int afterUnits)
        {
            if (rows == null || string.IsNullOrEmpty(marker) || string.IsNullOrEmpty(player)
                || string.IsNullOrEmpty(item) || beforeUnits <= 0 || afterUnits < 0
                || afterUnits != beforeUnits - 1) return false;
            int start = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.TraceId != marker) continue;
                if (start >= 0 || row.Category != "scenario" || row.Kind != SupportMarkerKind
                    || row.ActorId != player || row.TargetId != player) return false;
                start = i;
            }
            if (start < 0) return false;
            int matches = 0;
            for (int i = start + 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Category != "event" || row.Kind != "TonicApplied"
                    || row.ActorId != player || row.TargetId != player
                    || string.IsNullOrEmpty(row.TraceId) || string.IsNullOrEmpty(row.PayloadJson)) continue;
                try
                {
                    var payload = JObject.Parse(row.PayloadJson);
                    if (payload["item"]?.Type == JTokenType.String && payload["item"].Value<string>() == item
                        && payload["consumed"]?.Type == JTokenType.Boolean && payload["consumed"].Value<bool>())
                        matches++;
                }
                catch (Exception) { /* Malformed observations cannot prove use. */ }
            }
            return matches == 1;
        }

        private static bool TryReadPositiveDamage(string json, out bool lethal)
        {
            lethal = false;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JObject.Parse(json);
                var amount = payload["amount"];
                if (amount?.Type != JTokenType.Integer || amount.Value<long>() <= 0) return false;
                var health = payload["hpAfter"]; var dead = payload["lethal"];
                lethal = health?.Type == JTokenType.Integer && health.Value<long>() == 0
                    && dead?.Type == JTokenType.Boolean && dead.Value<bool>();
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
