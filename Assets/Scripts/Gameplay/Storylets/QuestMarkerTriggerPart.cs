using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Q5.6 (Docs/QUEST-DESIGN-CATALOG.md) — sets a narrative fact when the
    /// PLAYER steps onto this entity's cell. The **reach-a-location / explore**
    /// primitive: place an (invisible, non-solid) marker at the destination,
    /// gate the objective on <c>IfFact:&lt;Fact&gt;:&gt;=:1</c>. Order-independent
    /// (the fact persists), so it works whether the player reaches the spot
    /// before or after accepting the quest.
    ///
    /// <para>Subclasses <see cref="TriggerOnStepPart"/> (the same cell-step base
    /// the rune traps use), so it reacts to <c>EntityEnteredCell</c> with the
    /// engine's faction/self filtering. Adds a PLAYER gate (an NPC wandering
    /// over the marker must not satisfy a player's quest) and sets
    /// <see cref="ConsumeOnTrigger"/> = false so a passing NPC can't despawn the
    /// marker before the player arrives — the SetFact is idempotent, so a
    /// persistent marker is harmless.</para>
    /// </summary>
    public class QuestMarkerTriggerPart : TriggerOnStepPart
    {
        public override string Name => "QuestMarkerTrigger";

        /// <summary>The narrative fact key to set when the player arrives.</summary>
        public string Fact;
        /// <summary>The value to set (default 1 → gate with IfFact:Fact:>=:1).</summary>
        public int Value = 1;

        public QuestMarkerTriggerPart()
        {
            // Persist: an NPC stepping here must NOT consume the marker before
            // the player reaches it. Idempotent SetFact makes persistence safe.
            ConsumeOnTrigger = false;
        }

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            // Only the player reaching the marker counts toward a quest.
            if (actor != StoryletPart.LocalPlayer) return;
            if (string.IsNullOrEmpty(Fact)) return;
            NarrativeStatePart.Current?.SetFact(Fact, Value);
        }
    }
}
