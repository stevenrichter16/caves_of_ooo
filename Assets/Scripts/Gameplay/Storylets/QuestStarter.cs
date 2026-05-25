using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.3 (Docs/QUEST-WORLD-PARTS.md) — auto-starts a quest when the PLAYER
    /// takes the item carrying this Part (the M1 <c>"Taken"</c> event). CoO
    /// port of Qud's <c>XRL.World.Parts.QuestStarter</c>, Taken trigger only.
    ///
    /// <para>Attach to a takeable item (a quest scroll, a relic, etc.). When
    /// the player picks it up / takes it from a container, the quest named by
    /// <see cref="Quest"/> starts — the world-side complement to dialogue
    /// "take the quest" choices.</para>
    ///
    /// Qud→CoO mapping:
    /// <list type="bullet">
    /// <item>Qud <c>IfFinishedQuestStep</c> → <see cref="IfQuestCompleted"/>:
    /// an optional prerequisite. CoO doesn't permanently track finished
    /// objectives (they clear on stage advance), but it does track completed
    /// QUESTS — so the gate is "only start if quest X is completed". Empty ⇒
    /// no gate.</item>
    /// <item>Qud <c>Activated</c> + self-removal → the <see cref="Activated"/>
    /// flag. Fires ONCE ever (matters for the fail→re-take case: without it,
    /// re-grabbing the item after a fail would restart the quest, since
    /// StartQuest re-activates a failed quest). A flag rather than removing
    /// the Part avoids mutating the entity's part list mid event-dispatch.</item>
    /// <item>Player gate (Qud <c>IsPlayer()</c>) → taker == LocalPlayer,
    /// guarded against the null==null pre-bootstrap trap.</item>
    /// </list>
    ///
    /// <para>Zone-presence triggers (Created/Seen/OnScreen) are deferred —
    /// they need per-render/per-turn hooks (perf-sensitive; see
    /// PERF-FOUNDATION.md).</para>
    /// </summary>
    public class QuestStarter : Part
    {
        public override string Name => "QuestStarter";

        /// <summary>The quest ID to start when taken by the player.</summary>
        public string Quest;
        /// <summary>Optional gate: only start if this quest is already
        /// completed. Empty/null ⇒ no prerequisite.</summary>
        public string IfQuestCompleted;
        /// <summary>Set once the starter has fired. Round-trips so a taken-
        /// then-saved starter stays spent. Fires once ever (Qud parity).</summary>
        public bool Activated;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Taken") return true;
            if (Activated || string.IsNullOrEmpty(Quest)) return true;

            // Player gate (Qud IsPlayer()). Require a known LocalPlayer AND
            // that the taker IS it — guards the null==null pre-bootstrap trap.
            var taker = e.GetParameter<Entity>("Actor");
            var player = StoryletPart.LocalPlayer;
            if (player == null || taker != player) return true;

            var sp = StoryletPart.Current;
            if (sp == null) return true;

            // Prerequisite gate (Qud IfFinishedQuestStep → CoO IfQuestCompleted).
            if (!string.IsNullOrEmpty(IfQuestCompleted) && !sp.IsQuestCompleted(IfQuestCompleted))
                return true;

            sp.StartQuest(new QuestState { QuestId = Quest, CurrentStageIndex = 0 });
            Activated = true; // fires once ever
            return true;
        }
    }
}
