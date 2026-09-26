using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// PALIMPSEST P1 (Docs/PALIMPSEST-PROTOTYPE-PLAN.md §3) — durable
    /// state written onto the ground.
    ///
    /// <para><b>What this is for.</b> The shipped combat system is
    /// actor-centric: statuses live on creatures and vanish with them.
    /// The Palimpsest design is tile-centric — coatings, residues,
    /// energy and clouds persist on the floor after the ability that
    /// wrote them, and react with whatever is written next. This layer
    /// is the bridge between the two, and everything from tile reactions
    /// (P3) to the Scrape harvest economy (P7) stands on it.</para>
    ///
    /// <para><b>Sparse, deliberately.</b> A zone is 80×25 = 2000 cells
    /// and almost none of them ever carry state. A dense per-cell array
    /// would cost memory and save size for nothing, and would turn
    /// per-turn decay into a 2000-cell scan instead of an iteration over
    /// the handful of tiles actually written. <see cref="Tick"/> returns
    /// how many tiles it visited precisely so that claim stays
    /// testable.</para>
    ///
    /// <para><b>Two energy models coexist, on purpose.</b> Tiles use the
    /// coarse 0..2 Heat/Cold/Charge channels the design asks for,
    /// because a player has to be able to reason about them. Entities
    /// keep <see cref="ThermalPart"/>'s continuous temperature
    /// simulation. They are not the same number and are not meant to be;
    /// they meet only at explicit reaction boundaries (P3). This is
    /// documented here and in the plan because it is the most likely
    /// thing for a future reader to mistake for a bug.</para>
    ///
    /// <para>P1 ships NO gameplay: nothing reacts, nothing propagates,
    /// no ability writes here yet. It exists to be written, read,
    /// decayed and round-tripped.</para>
    /// </summary>
    public class ZoneTileState
    {
        /// <summary>Ceiling for the coarse energy channels (design §8).</summary>
        public const int MaxEnergy = 2;

        /// <summary>A layer that never decays. Used for projections whose
        /// lifetime is owned by something else — a river's water is
        /// bounded by its pool entity, not by a turn counter.</summary>
        public const int Permanent = int.MaxValue;

        [System.Serializable]
        public class Layer
        {
            public string Id;
            public int Turns;
        }

        /// <summary>One tile's worth of writing. Public fields so the
        /// save path round-trips it without bespoke handling.</summary>
        [System.Serializable]
        public class TileState
        {
            public List<Layer> Coatings = new List<Layer>(2);
            public List<Layer> Residues = new List<Layer>(1);
            public int Heat;
            public int Cold;
            public int Charge;
            public string Cloud = "";
            public int CloudTurns;

            public bool IsEmpty =>
                Coatings.Count == 0 && Residues.Count == 0 &&
                Heat == 0 && Cold == 0 && Charge == 0 &&
                string.IsNullOrEmpty(Cloud);
        }

        // Key is y * Zone.Width + x. Only written tiles exist here.
        private readonly Dictionary<int, TileState> _states = new Dictionary<int, TileState>();

        /// <summary>Reusable scratch for decay so Tick allocates nothing
        /// in the common case (PERF-FOUNDATION §Pattern 1).</summary>
        private readonly List<int> _reclaimScratch = new List<int>(8);

        /// <summary>Key snapshot so Tick can survive a reaction writing
        /// new tiles mid-loop (P3). Reused, never reallocated.</summary>
        private readonly List<int> _keyScratch = new List<int>(16);

        /// <summary>Tiles currently carrying anything. The cost of this
        /// whole system, in one number.</summary>
        public int WrittenCount => _states.Count;

        /// <summary>Invoked on every write so the renderer can repaint
        /// just that cell. Never a full-zone dirty — that is for FOV and
        /// lightmap changes only (CLAUDE.md perf rule 4).</summary>
        public System.Action<int, int> OnCellChanged;

        // ── Keys and bounds ──────────────────────────────────────

        private static bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < Zone.Width && y < Zone.Height;

        private static int Key(int x, int y) => y * Zone.Width + x;

        private TileState GetOrCreate(int x, int y)
        {
            int k = Key(x, y);
            if (!_states.TryGetValue(k, out var state))
            {
                state = new TileState();
                _states[k] = state;
            }
            return state;
        }

        /// <summary>The tile's state, or null when nothing is written.
        /// Returning null rather than a blank keeps the store sparse —
        /// a reader must not accidentally materialise a tile.</summary>
        public TileState Get(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            _states.TryGetValue(Key(x, y), out var state);
            return state;
        }

        public bool Has(int x, int y) => Get(x, y) != null;

        private void Changed(int x, int y) => OnCellChanged?.Invoke(x, y);

        // ── Coatings and residues ────────────────────────────────

        private static void WriteLayer(List<Layer> layers, string id, int turns)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].Id != id) continue;
                // Refresh takes the LONGER duration: re-applying must
                // never shorten what is already there.
                if (turns > layers[i].Turns) layers[i].Turns = turns;
                return;
            }
            layers.Add(new Layer { Id = id, Turns = turns });
        }

        private static int LayerTurns(List<Layer> layers, string id)
        {
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Id == id) return layers[i].Turns;
            return 0;
        }

        /// <summary>Writes a liquid coating. Coatings COEXIST — water and
        /// oil on one tile is the single most interesting setup in the
        /// design, so one must never replace the other.</summary>
        public void WriteCoating(int x, int y, string liquidId, int turns)
        {
            if (!InBounds(x, y) || string.IsNullOrEmpty(liquidId) || turns <= 0) return;
            WriteLayer(GetOrCreate(x, y).Coatings, liquidId, turns);
            Changed(x, y);
        }

        public bool HasCoating(int x, int y, string liquidId)
            => CoatingTurns(x, y, liquidId) > 0;

        public int CoatingTurns(int x, int y, string liquidId)
        {
            var state = Get(x, y);
            return state == null ? 0 : LayerTurns(state.Coatings, liquidId);
        }

        /// <summary>Writes a residue — what a previous action left
        /// behind (embers, ash). A separate layer from coatings because
        /// the rules differ: embers are not a liquid.</summary>
        public void WriteResidue(int x, int y, string residueId, int turns)
        {
            if (!InBounds(x, y) || string.IsNullOrEmpty(residueId) || turns <= 0) return;
            WriteLayer(GetOrCreate(x, y).Residues, residueId, turns);
            Changed(x, y);
        }

        public bool HasResidue(int x, int y, string residueId)
        {
            var state = Get(x, y);
            return state != null && LayerTurns(state.Residues, residueId) > 0;
        }

        /// <summary>Removes a specific coating. Used when a projected
        /// pool entity leaves its cell.</summary>
        public bool RemoveCoating(int x, int y, string liquidId)
            => RemoveLayer(Get(x, y)?.Coatings, liquidId, x, y);

        /// <summary>Removes a specific residue.</summary>
        public bool RemoveResidue(int x, int y, string residueId)
            => RemoveLayer(Get(x, y)?.Residues, residueId, x, y);

        private bool RemoveLayer(List<Layer> layers, string id, int x, int y)
        {
            if (layers == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].Id != id) continue;
                layers.RemoveAt(i);
                Changed(x, y);
                DropIfEmpty(x, y);
                return true;
            }
            return false;
        }

        // ── Energy ───────────────────────────────────────────────

        private static int Clamp(int v) => v < 0 ? 0 : (v > MaxEnergy ? MaxEnergy : v);

        public void AddHeat(int x, int y, int amount)
        {
            if (!InBounds(x, y) || amount == 0) return;
            var s = GetOrCreate(x, y);
            s.Heat = Clamp(s.Heat + amount);
            Changed(x, y);
            DropIfEmpty(x, y);
        }

        public void AddCold(int x, int y, int amount)
        {
            if (!InBounds(x, y) || amount == 0) return;
            var s = GetOrCreate(x, y);
            s.Cold = Clamp(s.Cold + amount);
            Changed(x, y);
            DropIfEmpty(x, y);
        }

        public void AddCharge(int x, int y, int amount)
        {
            if (!InBounds(x, y) || amount == 0) return;
            var s = GetOrCreate(x, y);
            s.Charge = Clamp(s.Charge + amount);
            Changed(x, y);
            DropIfEmpty(x, y);
        }

        public int Heat(int x, int y) => Get(x, y)?.Heat ?? 0;
        public int Cold(int x, int y) => Get(x, y)?.Cold ?? 0;
        public int Charge(int x, int y) => Get(x, y)?.Charge ?? 0;

        /// <summary>
        /// Heat and Cold annihilate each other; Charge is untouched
        /// (design §12). This is what prevents nonsense like
        /// "extremely hot and extremely frozen" while still permitting
        /// genuinely useful pairings such as Cold + Charged.
        /// </summary>
        public void CancelOpposedEnergy(int x, int y)
        {
            var s = Get(x, y);
            if (s == null) return;

            int shared = s.Heat < s.Cold ? s.Heat : s.Cold;
            if (shared <= 0) return;
            s.Heat -= shared;
            s.Cold -= shared;
            Changed(x, y);
            DropIfEmpty(x, y);
        }

        // ── Clouds ───────────────────────────────────────────────

        /// <summary>One cloud per tile; a new one replaces the old.
        /// Stacking vapours would multiply states without adding
        /// decisions.</summary>
        public void WriteCloud(int x, int y, string gasId, int turns)
        {
            if (!InBounds(x, y) || string.IsNullOrEmpty(gasId) || turns <= 0) return;
            var s = GetOrCreate(x, y);
            s.Cloud = gasId;
            s.CloudTurns = turns;
            Changed(x, y);
        }

        public string Cloud(int x, int y) => Get(x, y)?.Cloud ?? "";

        // ── Layer accounting (P7's Scrape depends on this) ───────

        /// <summary>
        /// How many distinct layers are written here. Each coating, each
        /// residue, each non-zero energy channel and any cloud counts as
        /// one. Scrape refunds ink by this number, so
        /// <see cref="CountLayers"/> and <see cref="Clear"/> must always
        /// agree — a test pins that.
        /// </summary>
        public int CountLayers(int x, int y)
        {
            var s = Get(x, y);
            if (s == null) return 0;

            int n = s.Coatings.Count + s.Residues.Count;
            if (s.Heat > 0) n++;
            if (s.Cold > 0) n++;
            if (s.Charge > 0) n++;
            if (!string.IsNullOrEmpty(s.Cloud)) n++;
            return n;
        }

        /// <summary>Erases every temporary layer. Returns how many were
        /// removed, which is what Scrape pays out on.</summary>
        public int Clear(int x, int y)
        {
            int removed = CountLayers(x, y);
            if (removed == 0) return 0;

            _states.Remove(Key(x, y));
            Changed(x, y);
            return removed;
        }

        private void DropIfEmpty(int x, int y)
        {
            int k = Key(x, y);
            if (_states.TryGetValue(k, out var s) && s.IsEmpty)
                _states.Remove(k);
        }

        /// <summary>
        /// Appends every written tile's packed key into
        /// <paramref name="into"/>. PALIMPSEST P3 uses this to seed a
        /// reaction sweep: the written set IS the interesting set, which
        /// is the payoff of storing state sparsely rather than scanning
        /// 2000 cells looking for something to react.
        /// </summary>
        public void CollectWrittenKeys(List<int> into)
        {
            if (into == null) return;
            foreach (var key in _states.Keys) into.Add(key);
        }

        // ── Decay ────────────────────────────────────────────────

        /// <summary>
        /// Ages every written tile by one turn and reclaims any that
        /// empty out. Returns the number of tiles visited — proportional
        /// to what was WRITTEN, never to the 2000 cells in a zone, which
        /// is the entire justification for the sparse store.
        /// </summary>
        public int Tick()
        {
            if (_states.Count == 0) return 0;

            _reclaimScratch.Clear();
            int visited = 0;

            // Snapshot the keys before iterating. Decay alone would be
            // safe with a plain foreach, but P3's reactions run INSIDE
            // this loop and write to tiles — mutating the dictionary
            // mid-enumeration throws InvalidOperationException. This is
            // the same bug class CLAUDE.md flags for
            // Zone.GetReadOnlyEntities, fixed before it can bite rather
            // than after.
            _keyScratch.Clear();
            foreach (var key in _states.Keys) _keyScratch.Add(key);

            for (int ki = 0; ki < _keyScratch.Count; ki++)
            {
                int stateKey = _keyScratch[ki];
                if (!_states.TryGetValue(stateKey, out var s)) continue;  // removed mid-loop
                visited++;

                DecayLayers(s.Coatings);
                DecayLayers(s.Residues);

                // Energy bleeds off a step at a time so a charge does not
                // sit on a tile for the rest of the fight.
                if (s.Heat > 0) s.Heat--;
                if (s.Cold > 0) s.Cold--;
                if (s.Charge > 0) s.Charge--;

                if (!string.IsNullOrEmpty(s.Cloud))
                {
                    s.CloudTurns--;
                    if (s.CloudTurns <= 0) { s.Cloud = ""; s.CloudTurns = 0; }
                }

                if (s.IsEmpty) _reclaimScratch.Add(stateKey);
            }

            for (int i = 0; i < _reclaimScratch.Count; i++)
            {
                int k = _reclaimScratch[i];
                _states.Remove(k);
                OnCellChanged?.Invoke(k % Zone.Width, k / Zone.Width);
            }

            return visited;
        }

        private static void DecayLayers(List<Layer> layers)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                // Permanent layers are owned by something else and must
                // not be aged away underneath it.
                if (layers[i].Turns == Permanent) continue;
                layers[i].Turns--;
                if (layers[i].Turns <= 0) layers.RemoveAt(i);
            }
        }

        // ── Save / load ──────────────────────────────────────────

        [System.Serializable]
        private class SaveEntry
        {
            public int Key;
            public TileState State;
        }

        [System.Serializable]
        private class SaveFile
        {
            public List<SaveEntry> Entries = new List<SaveEntry>();
        }

        public string ToSaveString()
        {
            var file = new SaveFile();
            foreach (var pair in _states)
                file.Entries.Add(new SaveEntry { Key = pair.Key, State = pair.Value });
            return JsonUtility.ToJson(file);
        }

        /// <summary>Replaces all state from a save string. Malformed or
        /// missing input leaves an empty layer rather than throwing — a
        /// save from before this feature existed must load cleanly.</summary>
        public void LoadFromString(string json)
        {
            _states.Clear();
            if (string.IsNullOrWhiteSpace(json)) return;

            SaveFile file;
            try { file = JsonUtility.FromJson<SaveFile>(json); }
            catch (System.Exception) { return; }
            if (file?.Entries == null) return;

            for (int i = 0; i < file.Entries.Count; i++)
            {
                var entry = file.Entries[i];
                if (entry?.State == null) continue;
                if (entry.State.Coatings == null) entry.State.Coatings = new List<Layer>();
                if (entry.State.Residues == null) entry.State.Residues = new List<Layer>();
                if (entry.State.IsEmpty) continue;
                _states[entry.Key] = entry.State;
            }
        }
    }
}
