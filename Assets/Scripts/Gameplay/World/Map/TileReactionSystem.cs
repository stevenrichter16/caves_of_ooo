using System.Collections.Generic;
using UnityEngine;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// PALIMPSEST P3 — two things written on a tile produce a third.
    ///
    /// <para><b>Entirely data-driven.</b> No reaction logic lives in any
    /// ability. A skill writes water; a rite writes charge; neither
    /// knows the other exists. This system reads
    /// <c>Content/Data/TileReactions/Reactions.json</c> and decides what
    /// the combination means. That separation is the architectural rule
    /// both the plan and the external design conversation independently
    /// insisted on — <i>spells emit simple events, not combo
    /// logic</i>.</para>
    ///
    /// <para><b>Resolution order</b> (design §26), fixed so outcomes are
    /// deterministic: energy cancellation first, then reactions in
    /// priority order, then one secondary pass for anything the first
    /// pass created. Propagation is P4 and deliberately absent.</para>
    ///
    /// <para><b>Loop guards</b> (design §27) are not optional. A player
    /// is allowed to build a ridiculous situation; the CPU is not
    /// allowed to join them. The same reaction cannot fire twice on the
    /// same tile within one originating action, and both generation
    /// depth and total reaction count are capped. Every refusal emits a
    /// record, because a chain that silently stops is otherwise
    /// indistinguishable from one that never started.</para>
    /// </summary>
    public static class TileReactionSystem
    {
        /// <summary>Max secondary passes from one originating action.</summary>
        public const int MaxGenerations = 4;

        /// <summary>Max reactions resolved from one originating action,
        /// across all generations.</summary>
        public const int MaxReactionsPerAction = 32;

        // ── Data ─────────────────────────────────────────────────

        [System.Serializable]
        public class TileReaction
        {
            public string ID = "";

            // Inputs. Empty string = "not required".
            public string InputCoating = "";
            public string InputResidue = "";
            /// <summary>"heat" | "cold" | "charge", or empty.</summary>
            public string InputEnergy = "";
            public int MinEnergy = 1;

            // What the reaction eats.
            public bool ConsumeCoating;
            public bool ConsumeResidue;
            public int ConsumeEnergy;

            // What it leaves behind.
            public string OutputCoating = "";
            public int OutputCoatingTurns;
            public string OutputResidue = "";
            public int OutputResidueTurns;
            public string OutputCloud = "";
            public int OutputCloudTurns;

            /// <summary>Effect applied to creatures standing in the cell.
            /// Resolved by name so the data file stays free of C# types.</summary>
            public string OccupantEffect = "";
            public int OccupantDamage;
            public string DamageAttribute = "";

            /// <summary>Lower resolves first. Ties break on file order.</summary>
            public int Priority = 50;
        }

        [System.Serializable]
        private class ReactionFile
        {
            public List<TileReaction> Reactions = new List<TileReaction>();
        }

        private static readonly List<TileReaction> _reactions = new List<TileReaction>();
        public static bool IsInitialized { get; private set; }
        public static int Count => _reactions.Count;

        public static void ResetForTests()
        {
            _reactions.Clear();
            IsInitialized = false;
        }

        public static void Initialize(string json)
        {
            ResetForTests();
            if (string.IsNullOrWhiteSpace(json)) return;

            ReactionFile file;
            try { file = JsonUtility.FromJson<ReactionFile>(json); }
            catch (System.Exception) { return; }
            if (file?.Reactions == null) return;

            for (int i = 0; i < file.Reactions.Count; i++)
            {
                var r = file.Reactions[i];
                if (r == null || string.IsNullOrWhiteSpace(r.ID)) continue;
                _reactions.Add(r);
            }
            _reactions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            IsInitialized = _reactions.Count > 0;
        }

        public static void EnsureInitialized()
        {
            if (IsInitialized) return;
            var assets = Resources.LoadAll<TextAsset>("Content/Data/TileReactions");
            if (assets == null) return;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null || string.IsNullOrWhiteSpace(assets[i].text)) continue;
                Initialize(assets[i].text);
                if (IsInitialized) return;
            }
        }

        // ── Resolution ───────────────────────────────────────────

        /// <summary>Guard key: one reaction may fire once per tile per
        /// originating action.</summary>
        private static readonly HashSet<long> _firedThisAction = new HashSet<long>();
        private static readonly List<int> _dirtyCells = new List<int>(16);
        private static readonly List<int> _nextGeneration = new List<int>(16);
        private static int _resolvedThisAction;

        /// <summary>
        /// Resolves every reaction reachable from the cells that changed,
        /// following the fixed order and respecting the loop guards.
        /// Call once per originating action, not per write.
        /// </summary>
        public static int ResolveZone(Zone zone, Entity cause = null)
        {
            if (zone == null || !IsInitialized) return 0;

            _firedThisAction.Clear();
            _resolvedThisAction = 0;

            // Seed with every written tile. P4's propagation will narrow
            // this to a dirty set; for P3 the written set IS the dirty
            // set, and it is sparse by construction.
            _dirtyCells.Clear();
            var state = zone.TileState;
            state.CollectWrittenKeys(_dirtyCells);

            int total = 0;
            for (int gen = 0; gen < MaxGenerations && _dirtyCells.Count > 0; gen++)
            {
                _nextGeneration.Clear();

                for (int i = 0; i < _dirtyCells.Count; i++)
                {
                    int key = _dirtyCells[i];
                    int x = key % Zone.Width, y = key / Zone.Width;

                    // Step 3 of the resolution order: opposed energy
                    // cancels BEFORE reactions look at it, so a tile is
                    // never both very hot and very cold when a reaction
                    // reads it.
                    state.CancelOpposedEnergy(x, y);

                    int fired = ResolveCell(zone, x, y, cause);
                    if (fired > 0)
                    {
                        total += fired;
                        _nextGeneration.Add(key);
                    }

                    if (_resolvedThisAction >= MaxReactionsPerAction)
                    {
                        Diag.Record("tile", "ReactionSuppressed", cause, null,
                            new { reason = "generation_cap_reactions", cap = MaxReactionsPerAction });
                        return total;
                    }
                }

                if (_nextGeneration.Count == 0) break;

                if (gen == MaxGenerations - 1)
                {
                    Diag.Record("tile", "ReactionSuppressed", cause, null,
                        new { reason = "depth_cap", cap = MaxGenerations });
                    break;
                }

                _dirtyCells.Clear();
                _dirtyCells.AddRange(_nextGeneration);
            }

            return total;
        }

        /// <summary>
        /// Resolves whatever this one tile's contents allow. Returns how
        /// many reactions fired.
        /// </summary>
        public static int ResolveCell(Zone zone, int x, int y, Entity cause = null)
        {
            if (zone == null || !IsInitialized) return 0;
            var state = zone.TileState;
            if (state.Get(x, y) == null) return 0;

            int fired = 0;
            for (int i = 0; i < _reactions.Count; i++)
            {
                var r = _reactions[i];
                if (!Matches(state, x, y, r)) continue;

                long guard = GuardKey(i, x, y);
                if (!_firedThisAction.Add(guard))
                {
                    // Already fired here this action. Without this a
                    // melt/freeze pair would oscillate forever.
                    Diag.Record("tile", "ReactionSuppressed", cause, null,
                        new { reason = "already_fired_this_action", reaction = r.ID, x, y });
                    continue;
                }

                Apply(zone, x, y, r, cause);
                fired++;
                _resolvedThisAction++;

                if (_resolvedThisAction >= MaxReactionsPerAction) break;
            }
            return fired;
        }

        private static long GuardKey(int reactionIndex, int x, int y)
            => ((long)reactionIndex << 32) | (uint)(y * Zone.Width + x);

        private static bool Matches(ZoneTileState state, int x, int y, TileReaction r)
        {
            if (!string.IsNullOrEmpty(r.InputCoating)
                && !state.HasCoating(x, y, r.InputCoating)) return false;
            if (!string.IsNullOrEmpty(r.InputResidue)
                && !state.HasResidue(x, y, r.InputResidue)) return false;

            if (!string.IsNullOrEmpty(r.InputEnergy))
            {
                int level = EnergyOf(state, x, y, r.InputEnergy);
                if (level < r.MinEnergy) return false;
            }

            // A reaction with no inputs at all would fire on every tile
            // forever. Refuse to treat that as a match.
            return !string.IsNullOrEmpty(r.InputCoating)
                || !string.IsNullOrEmpty(r.InputResidue)
                || !string.IsNullOrEmpty(r.InputEnergy);
        }

        private static int EnergyOf(ZoneTileState state, int x, int y, string channel)
        {
            switch (channel)
            {
                case "heat": return state.Heat(x, y);
                case "cold": return state.Cold(x, y);
                case "charge": return state.Charge(x, y);
                default: return 0;
            }
        }

        private static void Apply(Zone zone, int x, int y, TileReaction r, Entity cause)
        {
            var state = zone.TileState;

            // Consume inputs first, so an output of the same id is a
            // replacement rather than a no-op refresh.
            if (r.ConsumeCoating && !string.IsNullOrEmpty(r.InputCoating))
                state.RemoveCoating(x, y, r.InputCoating);
            if (r.ConsumeResidue && !string.IsNullOrEmpty(r.InputResidue))
                state.RemoveResidue(x, y, r.InputResidue);
            if (r.ConsumeEnergy > 0 && !string.IsNullOrEmpty(r.InputEnergy))
            {
                switch (r.InputEnergy)
                {
                    case "heat": state.AddHeat(x, y, -r.ConsumeEnergy); break;
                    case "cold": state.AddCold(x, y, -r.ConsumeEnergy); break;
                    case "charge": state.AddCharge(x, y, -r.ConsumeEnergy); break;
                }
            }

            if (!string.IsNullOrEmpty(r.OutputCoating))
                state.WriteCoating(x, y, r.OutputCoating, r.OutputCoatingTurns);
            if (!string.IsNullOrEmpty(r.OutputResidue))
                state.WriteResidue(x, y, r.OutputResidue, r.OutputResidueTurns);
            if (!string.IsNullOrEmpty(r.OutputCloud))
                state.WriteCloud(x, y, r.OutputCloud, r.OutputCloudTurns);

            int hit = ApplyToOccupants(zone, x, y, r, cause);

            Diag.Record("tile", "ReactionFired", cause, null,
                new
                {
                    reaction = r.ID, x, y,
                    consumedCoating = r.ConsumeCoating ? r.InputCoating : "",
                    consumedEnergy = r.ConsumeEnergy,
                    outputCoating = r.OutputCoating,
                    outputCloud = r.OutputCloud,
                    occupantsHit = hit,
                });
        }

        private static int ApplyToOccupants(Zone zone, int x, int y, TileReaction r, Entity cause)
        {
            if (string.IsNullOrEmpty(r.OccupantEffect) && r.OccupantDamage <= 0) return 0;

            var cell = zone.GetCell(x, y);
            if (cell == null) return 0;

            int hit = 0;
            // Iterate a snapshot: damage can kill an occupant, and a
            // death removes it from cell.Objects mid-loop.
            var occupants = new List<Entity>(cell.Objects.Count);
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e != null && e.Tags.ContainsKey("Creature")) occupants.Add(e);
            }

            for (int i = 0; i < occupants.Count; i++)
            {
                var target = occupants[i];

                if (r.OccupantDamage > 0)
                {
                    var dmg = new Damage(r.OccupantDamage);
                    if (!string.IsNullOrEmpty(r.DamageAttribute))
                        dmg.AddAttribute(r.DamageAttribute);
                    CombatSystem.ApplyDamage(target, dmg, cause, zone);
                }

                if (!string.IsNullOrEmpty(r.OccupantEffect)
                    && target.GetStatValue("Hitpoints", 0) > 0)
                {
                    var fx = MakeEffect(r.OccupantEffect);
                    if (fx != null) target.ApplyEffect(fx, cause, zone);
                }
                hit++;
            }
            return hit;
        }

        /// <summary>
        /// Effects are named in data, not typed. Kept to an explicit
        /// switch rather than reflection so an unknown name in content is
        /// a silent no-op instead of a runtime surprise, and so the set
        /// of effects a tile can inflict stays deliberately small.
        /// </summary>
        private static Effect MakeEffect(string name)
        {
            switch (name)
            {
                case "Electrified": return new ElectrifiedEffect(1.0f);
                case "Frozen":      return new FrozenEffect(0.6f);
                case "Wet":         return new WetEffect(0.8f);
                case "Confused":    return new ConfusedEffect(2);
                default:            return null;
            }
        }
    }
}
