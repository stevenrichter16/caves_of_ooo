using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.2 (Docs/QUEST-WORLD-PARTS.md) — finishes a quest objective when the
    /// PLAYER takes the item carrying this Part. The WORLD-side complement to
    /// the FinishObjective conversation action for "fetch the MacGuffin"
    /// objectives. CoO port of Qud's <c>XRL.World.Parts.CompleteQuestOnTaken</c>.
    ///
    /// <para>Attach to a takeable item with blueprint-configured
    /// <see cref="Quest"/> + <see cref="Objective"/>. Listens for the M1
    /// <c>"Taken"</c> event (fired on the item by PickupCommand /
    /// TakeFromContainerCommand after a successful add, params
    /// <c>Actor</c>=taker, <c>Item</c>=self) and routes to
    /// <see cref="StoryletPart.FinishObjective"/>.</para>
    ///
    /// <para><b>Player gate (Qud parity, the key difference from
    /// <see cref="FinishObjectiveWhenSlain"/>):</b> only the PLAYER taking
    /// the item counts — Qud's <c>CompleteQuest</c> gates on
    /// <c>Actor.IsPlayer()</c>, so an NPC picking the item up must NOT
    /// complete the objective. CoO's player is <see cref="StoryletPart.LocalPlayer"/>;
    /// the guard requires a non-null LocalPlayer AND taker == LocalPlayer (so
    /// the null==null trap can't sneak a completion through pre-bootstrap).
    /// Safely no-ops when the quest isn't active or the objective isn't in
    /// the current stage (FinishObjective's own guards).</para>
    /// </summary>
    public class CompleteObjectiveOnTaken : Part
    {
        public override string Name => "CompleteObjectiveOnTaken";

        /// <summary>The quest whose objective to finish on take.</summary>
        public string Quest;
        /// <summary>The objective ID in the quest's current stage.</summary>
        public string Objective;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Taken") return true;
            if (string.IsNullOrEmpty(Quest) || string.IsNullOrEmpty(Objective)) return true;

            // Player gate (Qud Actor.IsPlayer()). Require a known LocalPlayer
            // AND that the taker IS it — guards the null==null pre-bootstrap trap.
            var taker = e.GetParameter<Entity>("Actor");
            var player = StoryletPart.LocalPlayer;
            if (player == null || taker != player) return true;

            StoryletPart.Current?.FinishObjective(Quest, Objective, actor: taker);
            return true;
        }
    }
}
