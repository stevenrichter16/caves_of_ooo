using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.1 (Docs/QUEST-WORLD-PARTS.md) — finishes a quest objective when the
    /// entity carrying this Part is slain. The WORLD-side complement to the
    /// FinishObjective conversation action: "kill X to advance the quest".
    /// CoO port of Qud's <c>XRL.World.Parts.FinishQuestStepWhenSlain</c>.
    ///
    /// <para>Attach to a killable entity with blueprint-configured
    /// <see cref="Quest"/> + <see cref="Objective"/>. Listens for the
    /// <c>"Died"</c> event the CombatSystem fires on the dying entity
    /// (CombatSystem.cs:1072, dispatched to all parts via
    /// <c>Entity.FireEvent</c>) and routes to
    /// <see cref="StoryletPart.FinishObjective"/>. The killer is threaded as
    /// the diag/reward actor (null → FinishObjective falls back to
    /// LocalPlayer). No killer gate — Qud parity: the objective is "X is
    /// dead", regardless of who landed the kill. Safely no-ops when the
    /// quest isn't active or the objective isn't in the current stage
    /// (FinishObjective's own guards).</para>
    /// </summary>
    public class FinishObjectiveWhenSlain : Part
    {
        public override string Name => "FinishObjectiveWhenSlain";

        /// <summary>The quest whose objective to finish on death.</summary>
        public string Quest;
        /// <summary>The objective ID in the quest's current stage.</summary>
        public string Objective;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Died") return true;
            if (string.IsNullOrEmpty(Quest) || string.IsNullOrEmpty(Objective)) return true;
            var killer = e.GetParameter("Killer") as Entity;
            StoryletPart.Current?.FinishObjective(Quest, Objective, actor: killer);
            return true;
        }
    }
}
