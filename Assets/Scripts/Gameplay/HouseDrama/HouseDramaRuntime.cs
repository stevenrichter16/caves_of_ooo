using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Central state machine for all active House Dramas.
    ///
    /// Responsibilities:
    ///   - Track pressure point activation states (dormant/active/resolved/failed)
    ///   - Record which path resolved each pressure point
    ///   - Maintain per-NPC witness knowledge
    ///   - Track corruption score (summed from path EmotionalCostMagnitude + CorruptionContribution)
    ///   - Evaluate crossover edges on state change
    ///   - Track closed paths (sealed by crossover edges like AntagonistReveal)
    ///
    /// Follows the static manager pattern of FactionManager and PlayerReputation.
    /// </summary>
    public static class HouseDramaRuntime
    {
        private static readonly Dictionary<string, ActiveDrama> _dramas =
            new Dictionary<string, ActiveDrama>();

        // ─────────────────────────────────────────────────────────────────────
        // Registration
        // ─────────────────────────────────────────────────────────────────────

        public static void RegisterDrama(HouseDramaData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;

            var drama = new ActiveDrama { Data = data };

            // Seed initial witness knowledge from JSON
            if (data.WitnessMap?.Entries != null)
            {
                foreach (var entry in data.WitnessMap.Entries)
                {
                    if (string.IsNullOrEmpty(entry.SubjectId)) continue;
                    var knowledge = new HashSet<string>();
                    if (entry.Knows != null)
                        foreach (var fact in entry.Knows)
                            if (!string.IsNullOrEmpty(fact))
                                knowledge.Add(fact);
                    drama.WitnessKnowledge[entry.SubjectId] = knowledge;
                }
            }

            // Seed pressure points as dormant
            if (data.PressurePoints != null)
                foreach (var pp in data.PressurePoints)
                    if (!string.IsNullOrEmpty(pp.Id))
                        drama.PressurePoints[pp.Id] = new PressurePointState();

            _dramas[data.ID] = drama;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Drama Activation
        // ─────────────────────────────────────────────────────────────────────

        public static void ActivateDrama(string dramaId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return;
            drama.IsActive = true;
            foreach (var pp in drama.PressurePoints.Values)
                if (pp.State == "dormant")
                    pp.State = "active";

            Debug.Log($"[HouseDramaRuntime] Drama '{dramaId}' activated.");
        }

        public static bool IsDramaActive(string dramaId) =>
            _dramas.TryGetValue(dramaId, out var d) && d.IsActive;

        // ─────────────────────────────────────────────────────────────────────
        // Pressure Point State
        // ─────────────────────────────────────────────────────────────────────

        public static string GetPressurePointState(string dramaId, string pointId)
        {
            if (_dramas.TryGetValue(dramaId, out var drama) &&
                drama.PressurePoints.TryGetValue(pointId, out var pp))
                return pp.State;
            return "dormant";
        }

        public static string GetPressurePointSubstate(string dramaId, string pointId)
        {
            if (_dramas.TryGetValue(dramaId, out var drama) &&
                drama.PressurePoints.TryGetValue(pointId, out var pp))
                return pp.Substate;
            return null;
        }

        public static string GetPathTaken(string dramaId, string pointId)
        {
            if (_dramas.TryGetValue(dramaId, out var drama) &&
                drama.PressurePoints.TryGetValue(pointId, out var pp))
                return pp.PathTaken;
            return null;
        }

        /// <summary>
        /// Transition a pressure point to a new state, optionally recording which path resolved it.
        /// Also accumulates corruption from the path's cost data and fires crossover edges.
        /// </summary>
        public static void AdvancePressurePoint(
            string dramaId, string pointId, string newState,
            string pathId = null, string substate = null)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return;

            if (!drama.PressurePoints.TryGetValue(pointId, out var pp))
            {
                pp = new PressurePointState();
                drama.PressurePoints[pointId] = pp;
            }

            pp.State = newState;
            if (substate != null) pp.Substate = substate;
            if (pathId != null) pp.PathTaken = pathId;

            // Accumulate corruption from path costs
            if (pathId != null)
            {
                var pathData = drama.Data?.GetPath(pointId, pathId);
                if (pathData != null)
                    drama.CorruptionScore = Math.Max(0, drama.CorruptionScore +
                        pathData.CorruptionContribution + pathData.EmotionalCostMagnitude);
            }

            EvaluateCrossovers(dramaId, pointId, newState);

            Debug.Log($"[HouseDramaRuntime] '{dramaId}'.{pointId} → {newState}" +
                      (pathId != null ? $" (via {pathId})" : ""));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Witness Map
        // ─────────────────────────────────────────────────────────────────────

        public static bool WitnessKnows(string dramaId, string npcId, string factId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return false;
            return drama.WitnessKnowledge.TryGetValue(npcId, out var knowledge) &&
                   knowledge.Contains(factId);
        }

        public static void RevealWitnessFact(string dramaId, string npcId, string factId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return;
            if (!drama.WitnessKnowledge.TryGetValue(npcId, out var knowledge))
            {
                knowledge = new HashSet<string>();
                drama.WitnessKnowledge[npcId] = knowledge;
            }
            knowledge.Add(factId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Corruption
        // ─────────────────────────────────────────────────────────────────────

        public static int GetCorruption(string dramaId) =>
            _dramas.TryGetValue(dramaId, out var d) ? d.CorruptionScore : 0;

        public static void AddCorruption(string dramaId, int amount)
        {
            if (_dramas.TryGetValue(dramaId, out var drama))
                drama.CorruptionScore = Math.Max(0, drama.CorruptionScore + amount);
        }

        /// <summary>
        /// Computes the minimum total cost (mechanical + emotional) path sequence to Corrupted.
        /// Used for validation — if the computed path doesn't match the intended texture,
        /// fix individual path costs, not the gradient descriptor.
        /// </summary>
        public static int ComputeMinCorruptionPathCost(string dramaId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return int.MaxValue;
            if (drama.Data?.PressurePoints == null) return int.MaxValue;

            int total = 0;
            foreach (var pp in drama.Data.PressurePoints)
            {
                if (pp.Paths == null || pp.Paths.Count == 0) continue;

                // Find path with lowest mechanical + emotional cost
                int minCost = int.MaxValue;
                foreach (var path in pp.Paths)
                {
                    int cost = path.EmotionalCostMagnitude;
                    if (path.Costs != null)
                        foreach (var c in path.Costs)
                            cost += c.Magnitude;
                    if (cost < minCost) minCost = cost;
                }
                total += minCost;
            }
            return total;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Closed Paths (sealed by crossover edges)
        // ─────────────────────────────────────────────────────────────────────

        public static bool IsPathClosed(string dramaId, string pointId, string pathId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return false;
            return drama.ClosedPaths.TryGetValue(pointId, out var closed) &&
                   closed.Contains(pathId);
        }

        public static void ClosePath(string dramaId, string pointId, string pathId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return;
            if (!drama.ClosedPaths.TryGetValue(pointId, out var closed))
            {
                closed = new HashSet<string>();
                drama.ClosedPaths[pointId] = closed;
            }
            closed.Add(pathId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Manual Crossover Trigger (from conversation action)
        // ─────────────────────────────────────────────────────────────────────

        public static void ApplyCrossoverEffect(string dramaId, string effect)
        {
            if (!_dramas.ContainsKey(dramaId)) return;
            ExecuteEffect(dramaId, effect);
        }

        // ─────────────────────────────────────────────────────────────────────
        // End State Evaluation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the ID of the end state that best matches the current drama progress,
        /// or null if no end state has been reached.
        /// </summary>
        public static string EvaluateEndState(string dramaId)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return null;
            if (drama.Data?.EndStates == null) return null;

            // Collect all pathsTaken across pressure points
            var pathsTaken = new HashSet<string>();
            foreach (var pp in drama.PressurePoints.Values)
                if (!string.IsNullOrEmpty(pp.PathTaken))
                    pathsTaken.Add(pp.PathTaken);

            // Find end state whose path signature is a subset of paths taken
            foreach (var endState in drama.Data.EndStates)
            {
                if (endState.PathSignature == null || endState.PathSignature.Count == 0)
                    continue;
                bool allMatch = true;
                foreach (var sig in endState.PathSignature)
                    if (!pathsTaken.Contains(sig)) { allMatch = false; break; }
                if (allMatch) return endState.Id;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal
        // ─────────────────────────────────────────────────────────────────────

        private static void EvaluateCrossovers(string dramaId, string changedPointId, string newState)
        {
            if (!_dramas.TryGetValue(dramaId, out var drama)) return;
            if (drama.Data?.Crossovers == null) return;

            foreach (var edge in drama.Data.Crossovers)
            {
                if (edge.FromPointId != changedPointId) continue;
                if (!MatchesCondition(edge.FromCondition, newState)) continue;
                ExecuteEffect(dramaId, edge.ToEffect);
            }
        }

        private static bool MatchesCondition(string condition, string currentState)
        {
            if (string.IsNullOrEmpty(condition)) return true;

            // Format: "state:STATENAME"
            if (condition.StartsWith("state:", StringComparison.Ordinal))
            {
                string expected = condition.Substring(6);
                return currentState == expected;
            }
            return false;
        }

        private static void ExecuteEffect(string dramaId, string effect)
        {
            if (string.IsNullOrEmpty(effect)) return;

            // Effect formats:
            //   "state:PointID:NewState"      — advance a pressure point
            //   "reveal:NpcID:FactID"         — reveal witness fact
            //   "corruption:Amount"           — add corruption
            //   "close:PointID:PathID"        — mark a path closed

            var sep = effect.IndexOf(':');
            if (sep < 0) return;

            string verb = effect.Substring(0, sep);
            string rest = effect.Substring(sep + 1);

            switch (verb)
            {
                case "state":
                {
                    var parts = rest.Split(':');
                    if (parts.Length >= 2)
                        AdvancePressurePoint(dramaId, parts[0], parts[1]);
                    break;
                }
                case "reveal":
                {
                    var parts = rest.Split(':');
                    if (parts.Length >= 2)
                        RevealWitnessFact(dramaId, parts[0], parts[1]);
                    break;
                }
                case "corruption":
                {
                    if (int.TryParse(rest, out int amt))
                        AddCorruption(dramaId, amt);
                    break;
                }
                case "close":
                {
                    var parts = rest.Split(':');
                    if (parts.Length >= 2)
                        ClosePath(dramaId, parts[0], parts[1]);
                    break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Access and Reset
        // ─────────────────────────────────────────────────────────────────────

        public static ActiveDrama GetDrama(string dramaId) =>
            _dramas.TryGetValue(dramaId, out var d) ? d : null;

        public static List<string> GetAllDramaIds() =>
            new List<string>(_dramas.Keys);

        public static void Reset() => _dramas.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Runtime state — not serialized; session-only until save/load is built.
    // ─────────────────────────────────────────────────────────────────────────

    public class ActiveDrama
    {
        public HouseDramaData Data;
        public bool IsActive;
        public Dictionary<string, PressurePointState> PressurePoints =
            new Dictionary<string, PressurePointState>();
        public Dictionary<string, HashSet<string>> WitnessKnowledge =
            new Dictionary<string, HashSet<string>>();
        public Dictionary<string, HashSet<string>> ClosedPaths =
            new Dictionary<string, HashSet<string>>();
        public int CorruptionScore;
    }

    public class PressurePointState
    {
        public string State = "dormant";
        public string Substate;
        public string PathTaken;
    }
}
