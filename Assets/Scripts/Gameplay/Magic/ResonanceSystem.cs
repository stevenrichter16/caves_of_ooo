using System.Collections.Generic;
using UnityEngine;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM7 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §4.1, §5 P3) —
    /// the payoff verb. Reads the statuses on a target and reports which
    /// ones a rite of a given element may SPEND, and what it gets for
    /// them.
    ///
    /// <para><b>This is the thing skills cannot do.</b> A skill applies a
    /// status; a rite consumes one. That division is the whole grammar
    /// of the feature (plan §4), and it lives here so no individual rite
    /// has to implement it.</para>
    ///
    /// <para><b>Data-driven on purpose.</b> Every element × status rule
    /// lives in <c>Content/Data/Resonance/Resonance.json</c>, never in a
    /// rite. Both this plan and the external design conversation
    /// (Docs/STATUS-SYSTEM-MODULAR-LADDER.md §2) independently concluded
    /// that combination rules must be a lookup table rather than
    /// per-spell logic — a rite emits "I am Electric, I have 2 slots",
    /// and this decides what that means.</para>
    ///
    /// <para><b>Dead pairs are content, not omissions.</b> A status with
    /// no entry for an element is deliberately worth nothing — fire does
    /// not conduct. Those cases emit a <c>ResonanceDeclined</c> record so
    /// a player asking "why didn't my combo work?" gets an answer, and
    /// so the grammar is teachable rather than mysterious.</para>
    /// </summary>
    public static class ResonanceSystem
    {
        // ── Data shapes ──────────────────────────────────────────

        [System.Serializable]
        public class ResonanceEntry
        {
            /// <summary>Effect class name without the "Effect" suffix —
            /// "Wet", "Electrified", "Burning".</summary>
            public string Status;
            /// <summary>Whether spending this status removes it. A rite
            /// that consumed nothing would be a skill.</summary>
            public bool Consume = true;
            /// <summary>Linear contribution to the damage multiplier.</summary>
            public float Mult = 0.75f;
            /// <summary>Free-form tag the calling rite interprets —
            /// "Arc", "Stun", "Shatter". The system does not know what
            /// these mean, which is what keeps it generic.</summary>
            public string Rider = "";
        }

        [System.Serializable]
        public class ElementTable
        {
            public string Element;
            public List<ResonanceEntry> Entries = new List<ResonanceEntry>();
        }

        [System.Serializable]
        public class ResonanceFile
        {
            public List<ElementTable> Elements = new List<ElementTable>();
            /// <summary>Coefficient on n² where n = statuses consumed.
            /// This is what makes stacking super-linear: the third
            /// status must be worth chasing, or players will settle for
            /// one. Tuned in data, never in code.</summary>
            public float QuadraticCoefficient = 0.25f;
        }

        // ── Result ───────────────────────────────────────────────

        public class Result
        {
            /// <summary>Statuses actually spent, in resolution order.</summary>
            public readonly List<string> Consumed = new List<string>();
            /// <summary>Statuses present but worth nothing to this
            /// element — the dead pairs that teach the grammar.</summary>
            public readonly List<string> Declined = new List<string>();
            /// <summary>Rider tags earned. The rite decides what they do.</summary>
            public readonly List<string> Riders = new List<string>();
            /// <summary>Damage multiplier. 1.0 when nothing resonated —
            /// a rite cast cold is deliberately NOT rewarded.</summary>
            public float Multiplier = 1.0f;
            public bool AnyResonance => Consumed.Count > 0;
        }

        // ── Registry ─────────────────────────────────────────────

        private static ResonanceFile _data;
        private static readonly Dictionary<string, Dictionary<string, ResonanceEntry>> _byElement =
            new Dictionary<string, Dictionary<string, ResonanceEntry>>(
                System.StringComparer.OrdinalIgnoreCase);

        public static bool IsInitialized { get; private set; }
        public static float QuadraticCoefficient =>
            _data?.QuadraticCoefficient ?? 0.25f;

        public static void ResetForTests()
        {
            _data = null;
            _byElement.Clear();
            IsInitialized = false;
        }

        public static void Initialize(string json)
        {
            ResetForTests();
            if (string.IsNullOrWhiteSpace(json)) return;

            _data = JsonUtility.FromJson<ResonanceFile>(json);
            if (_data?.Elements == null) { _data = null; return; }

            for (int i = 0; i < _data.Elements.Count; i++)
            {
                var table = _data.Elements[i];
                if (table == null || string.IsNullOrWhiteSpace(table.Element)) continue;

                var map = new Dictionary<string, ResonanceEntry>(
                    System.StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < table.Entries.Count; j++)
                {
                    var entry = table.Entries[j];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Status)) continue;
                    map[entry.Status] = entry;
                }
                _byElement[table.Element] = map;
            }
            IsInitialized = _byElement.Count > 0;
        }

        /// <summary>Loads every JSON under Content/Data/Resonance/.
        /// Mirrors the SkillRegistry / MaterialReactions convention, so a
        /// new element table is a new file and nothing else.</summary>
        public static void EnsureInitialized()
        {
            if (IsInitialized) return;
            var assets = Resources.LoadAll<TextAsset>("Content/Data/Resonance");
            if (assets == null || assets.Length == 0) return;
            // Single-file for now; the loop keeps the door open.
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null || string.IsNullOrWhiteSpace(assets[i].text)) continue;
                Initialize(assets[i].text);
                if (IsInitialized) return;
            }
        }

        /// <summary>
        /// The data-file key for an effect: its class name with the
        /// trailing "Effect" stripped, so <c>WetEffect</c> is authored as
        /// <c>"Wet"</c>. Keeps Resonance.json free of C# type names.
        /// </summary>
        internal static string StatusKey(Effect fx)
        {
            if (fx == null) return "";
            string n = fx.GetType().Name;
            const string suffix = "Effect";
            return n.EndsWith(suffix) && n.Length > suffix.Length
                ? n.Substring(0, n.Length - suffix.Length)
                : n;
        }

        // ── The verb ─────────────────────────────────────────────

        /// <summary>
        /// Works out what a rite of <paramref name="element"/> would get
        /// from <paramref name="target"/>, WITHOUT changing anything.
        ///
        /// <para>Read-only by design: the targeting UI (SM12) previews
        /// the payoff with this exact call before the player commits a
        /// charge. <see cref="Spend"/> is the mutating half.</para>
        /// </summary>
        /// <param name="maxSlots">How many statuses this rite may spend.
        /// Channelling (SM9) raises it.</param>
        public static Result Preview(Entity target, string element, int maxSlots = 2)
        {
            var result = new Result();
            if (target == null || string.IsNullOrEmpty(element) || maxSlots <= 0)
                return result;

            var effects = target.GetPart<StatusEffectsPart>();
            if (effects == null) return result;

            _byElement.TryGetValue(element, out var table);

            float linear = 0f;
            var present = effects.GetAllEffects();
            for (int i = 0; i < present.Count; i++)
            {
                if (present[i] == null) continue;
                string status = StatusKey(present[i]);
                ResonanceEntry entry = null;
                table?.TryGetValue(status, out entry);

                if (entry == null)
                {
                    // Present but worth nothing to this element. The
                    // dead pair IS the lesson.
                    result.Declined.Add(status);
                    continue;
                }

                if (result.Consumed.Count >= maxSlots)
                {
                    // Resonant but out of slots — a different answer
                    // from "not resonant", and the player deserves it.
                    result.Declined.Add(status);
                    continue;
                }

                result.Consumed.Add(status);
                linear += entry.Mult;
                if (!string.IsNullOrEmpty(entry.Rider)) result.Riders.Add(entry.Rider);
            }

            int n = result.Consumed.Count;
            // Super-linear on purpose: the third status has to be worth
            // chasing or players will settle for one and the loop dies.
            result.Multiplier = 1f + linear + QuadraticCoefficient * n * n;
            return result;
        }

        /// <summary>
        /// <see cref="Preview"/>, then actually removes the consumed
        /// statuses and emits the diag trail. This is the only place a
        /// status is spent.
        /// </summary>
        public static Result Spend(Entity target, string element, int maxSlots = 2,
            Entity caster = null, Zone zone = null)
        {
            var result = Preview(target, element, maxSlots);
            if (target == null) return result;

            var effects = target.GetPart<StatusEffectsPart>();
            if (effects == null) return result;

            _byElement.TryGetValue(element, out var table);

            for (int i = 0; i < result.Consumed.Count; i++)
            {
                string status = result.Consumed[i];
                ResonanceEntry entry = null;
                table?.TryGetValue(status, out entry);
                if (entry == null || !entry.Consume) continue;

                // Match by class name rather than by type, so the data
                // file never has to know about C# types.
                string wanted = status;
                effects.RemoveEffect(fx => fx != null && StatusKey(fx) == wanted);

                Diag.Record(
                    category: "spell", kind: "ResonanceFired",
                    actor: caster, target: target,
                    payload: new
                    {
                        element,
                        status,
                        rider = entry.Rider,
                        multiplierAfter = result.Multiplier,
                        consumedCount = result.Consumed.Count,
                    });
            }

            for (int i = 0; i < result.Declined.Count; i++)
            {
                // "Why didn't my combo work?" is answerable with a query
                // instead of a debugging session.
                Diag.Record(
                    category: "spell", kind: "ResonanceDeclined",
                    actor: caster, target: target,
                    payload: new
                    {
                        element,
                        status = result.Declined[i],
                        reason = table != null && table.ContainsKey(result.Declined[i])
                            ? "no_slots_left"
                            : "not_resonant_with_element",
                    });
            }

            return result;
        }
    }
}
