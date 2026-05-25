using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.5 (Docs/QUEST-DESIGN-CATALOG.md) — INCREMENTS a narrative fact when
    /// the entity carrying this Part is slain. The counter sibling of
    /// <see cref="SetFactWhenSlain"/> (which overwrites): this enables
    /// **kill-N / clear-N** world objectives. Put it on each of N mobs with the
    /// same <see cref="Fact"/>, and gate the objective on
    /// <c>IfFact:&lt;Fact&gt;:&gt;=:N</c> — order-independent (the fact persists,
    /// the tick polls it), so any kill order / killer works with no soft-lock.
    ///
    /// <para>No killer gate (parity with the slain Parts: "X is dead,
    /// regardless of who killed it"). Calls <c>NarrativeStatePart.AddFact</c>
    /// (the increment used by the <c>AddFact</c> conversation action).</para>
    /// </summary>
    public class AddFactWhenSlain : Part
    {
        public override string Name => "AddFactWhenSlain";

        /// <summary>The narrative fact key to increment on death.</summary>
        public string Fact;
        /// <summary>How much to add per death (default 1 → a kill counter).</summary>
        public int Amount = 1;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Died") return true;
            if (string.IsNullOrEmpty(Fact)) return true;
            NarrativeStatePart.Current?.AddFact(Fact, Amount);
            return true;
        }
    }
}
