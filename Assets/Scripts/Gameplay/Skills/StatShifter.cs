using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Per-skill stat-shift tracker. Mirrors Qud's <c>StatShifter</c>
    /// (XRL.World/StatShifter.cs:9-256) in shape — owner reference,
    /// active-shifts dictionary, <c>SetStatShift</c> + <c>RemoveStatShifts</c>
    /// API — but CoO-simplified: a single owner, Dictionary&lt;string,int&gt;
    /// tracking instead of Qud's Guid-keyed multi-target dictionary.
    ///
    /// <para><b>Why simpler than Qud:</b> Qud's StatShifter manages
    /// shifts across MULTIPLE targets (relevant for cybernetics / mounts /
    /// projected effects), tracks per-shift Guids on a Statistic.AddShift
    /// substrate that lets the same stat have multiple independent shifts
    /// resolvable via UpdateShift. CoO's <see cref="Stat"/> doesn't have
    /// a shift-list substrate — only BaseValue. For v1 we mutate
    /// BaseValue directly, tracking only what THIS shifter applied so we
    /// can roll it back. Limitations:</para>
    /// <list type="bullet">
    ///   <item>Single-owner only (each BaseSkillPart has its own shifter).</item>
    ///   <item>If two skills shift the same stat by the same amount, then one
    ///         skill removes its shift, BaseValue drops by the right delta —
    ///         but neither shifter knows about the other. Not a correctness
    ///         issue (each shifter only undoes what it did), just a
    ///         discoverability one in logs.</item>
    ///   <item>Save/load: shifts are NOT persisted in v1. After a
    ///         save/load cycle, BaseValue retains the shift but
    ///         <see cref="ActiveShifts"/> is empty, so RemoveStatShifts
    ///         becomes a no-op (the shift becomes effectively permanent).
    ///         Documented as 🟡 in ST.5 self-review; deferred until
    ///         characters routinely save with active skill shifts.</item>
    /// </list>
    /// </summary>
    public class StatShifter
    {
        /// <summary>The entity whose stats this shifter modifies.</summary>
        public Entity Owner;

        /// <summary>
        /// Map of statName → currently-active shift amount applied by THIS
        /// shifter. Entries are added in <see cref="SetStatShift"/> and
        /// removed in <see cref="RemoveStatShifts"/>.
        /// </summary>
        public Dictionary<string, int> ActiveShifts = new Dictionary<string, int>();

        public StatShifter(Entity owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Apply (or update) a stat shift on the owner. If the stat is
        /// already shifted by this shifter, the previous amount is rolled
        /// back first, then the new amount applied. Mirrors Qud's
        /// StatShifter.SetStatShift idempotent-replace semantics
        /// (StatShifter.cs:122-167). Returns true if the shift took effect
        /// (stat exists on owner), false if the owner is null / lacks the
        /// stat.
        /// </summary>
        public bool SetStatShift(string statName, int amount)
        {
            if (Owner == null || string.IsNullOrWhiteSpace(statName))
                return false;
            var stat = Owner.GetStat(statName);
            if (stat == null) return false;

            // Idempotent-replace: undo prior shift for this stat (if any),
            // then apply new. Two same-amount calls net to no change.
            if (ActiveShifts.TryGetValue(statName, out int previous))
                stat.BaseValue -= previous;

            ActiveShifts[statName] = amount;
            stat.BaseValue += amount;
            return true;
        }

        /// <summary>
        /// Remove ALL shifts this shifter applied. Walks
        /// <see cref="ActiveShifts"/>, subtracting each entry's amount
        /// from the corresponding stat's BaseValue, then clears the dict.
        /// Mirrors Qud's StatShifter.RemoveStatShifts (StatShifter.cs:178-201).
        /// Safe to call when no shifts are active (no-op).
        /// </summary>
        public void RemoveStatShifts()
        {
            if (Owner == null) { ActiveShifts.Clear(); return; }
            foreach (var kvp in ActiveShifts)
            {
                var stat = Owner.GetStat(kvp.Key);
                if (stat != null)
                    stat.BaseValue -= kvp.Value;
            }
            ActiveShifts.Clear();
        }

        /// <summary>
        /// True if any shifts are currently active. Used by tests +
        /// future UI ("does this skill have unrolled-back shifts?").
        /// </summary>
        public bool HasStatShifts() => ActiveShifts.Count > 0;
    }
}
