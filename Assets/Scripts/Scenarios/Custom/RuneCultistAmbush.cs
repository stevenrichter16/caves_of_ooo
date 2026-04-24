namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M6.3 demo scenario: three RuneCultists placed on a radius-4 ring
    /// around the player, showcasing the end-to-end M6 pipeline:
    /// AILayRunePart (bored tick) → LayRuneGoal (walk+place) →
    /// RuneOfX entity with TriggerOnStepPart → EntityEnteredCell event
    /// on player step → damage + effect.
    ///
    /// The ring radius is large enough that the cultists start outside
    /// the player's immediate reach, giving them a few turns of idle
    /// time (and therefore several AILayRune chances) before they
    /// aggro. The player can then see laid runes in the FOV and decide
    /// whether to step around them or eat the damage.
    ///
    /// Why 3 cultists: one per rune variant makes it statistically likely
    /// that Flame / Frost / Poison runes all appear within a dozen turns,
    /// letting a playtester observe each effect's payload from a single
    /// scenario run.
    /// </summary>
    [Scenario(
        name: "Rune Cultist Ambush",
        category: "Combat Stress",
        description: "Three rune cultists on a radius-4 ring; they lay runes as they close.")]
    public class RuneCultistAmbush : IScenario
    {
        private const int Count = 3;
        private const int Radius = 4;

        public void Apply(ScenarioContext ctx)
        {
            int spawned = 0;
            for (int i = 0; i < Count; i++)
            {
                if (ctx.Spawn("RuneCultist").InRing(Radius, i, Count) != null)
                    spawned++;
            }

            if (spawned == Count)
                ctx.Log($"Rune Cultist Ambush: all {Count} cultists spawned on r={Radius} ring.");
            else
                ctx.Log($"Rune Cultist Ambush: {spawned}/{Count} cultists spawned (rest blocked by terrain).");
        }
    }
}
