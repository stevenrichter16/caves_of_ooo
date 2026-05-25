using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.4 (Docs/QUEST-IN-WORLD.md) — sets a narrative fact when the entity
    /// carrying this Part is slain. The robust, ORDER-INDEPENDENT kill-detection
    /// primitive for WORLD quests.
    ///
    /// <para>Pair it with an <c>IfFact:&lt;Fact&gt;:&gt;=:1</c> trigger on a quest
    /// objective: the kill then completes the objective whether it happens
    /// BEFORE or AFTER the player accepts the quest, because the fact is
    /// persisted (in <see cref="NarrativeStatePart"/>, saved) and the objective
    /// is finished by the polled tick dispatch — not by an event that requires
    /// the quest to already be active. This is the kill-side analog of
    /// <c>IfHaveItem</c> for fetch objectives, and the fix for the pre-accept
    /// soft-lock that <see cref="FinishObjectiveWhenSlain"/> has in the wild
    /// (it no-ops if the quest isn't active yet).</para>
    ///
    /// <para>Listens for the <c>"Died"</c> event the CombatSystem fires on the
    /// dying entity (dispatched to all parts via <c>Entity.FireEvent</c>). No
    /// killer gate — Qud/CoO parity with FinishObjectiveWhenSlain: "X is dead",
    /// regardless of who landed the kill (player, an NPC, or environmental). The
    /// two parts compose: an entity can carry both (instant finish via
    /// FinishObjectiveWhenSlain when the quest is active, plus the persisted fact
    /// for the order-independent path).</para>
    /// </summary>
    public class SetFactWhenSlain : Part
    {
        public override string Name => "SetFactWhenSlain";

        /// <summary>The narrative fact key to set on death.</summary>
        public string Fact;
        /// <summary>The value to set the fact to (default 1). A quest objective
        /// gates on it with <c>IfFact:&lt;Fact&gt;:&gt;=:1</c> (or another op).</summary>
        public int Value = 1;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Died") return true;
            if (string.IsNullOrEmpty(Fact)) return true;
            NarrativeStatePart.Current?.SetFact(Fact, Value);
            return true;
        }
    }
}
