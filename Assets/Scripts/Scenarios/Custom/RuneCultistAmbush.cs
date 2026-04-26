namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M6.3 demo scenario: three RuneCultists placed on a radius-4 ring
    /// around the player, showcasing the end-to-end M6 pipeline:
    /// AILayRunePart (bored tick) → LayRuneGoal (walk+place) →
    /// RuneOfX entity with TriggerOnStepPart → EntityEnteredCell event
    /// on player step → damage + effect.
    ///
    /// <para><b>Cultist disposition.</b> Cultists are <c>neutral</c>
    /// toward the player (<c>Factions.json</c> sets
    /// <c>Cultists.InitialPlayerReputation = 0</c>). They wander the
    /// zone, occasionally laying runes as their idle behavior. They do
    /// not initiate combat against the player — the scenario is a
    /// hazard-placement demo, not a combat ambush. The runes themselves
    /// still damage the player because <c>TriggerOnStepPart</c>'s
    /// <c>TriggerFaction</c> filter rejects only same-faction steppers.</para>
    ///
    /// Why 3 cultists: one per rune variant makes it statistically likely
    /// that Flame / Frost / Poison runes all appear within a dozen turns,
    /// letting a playtester observe each effect's payload from a single
    /// scenario run.
    /// </summary>
    [Scenario(
        name: "Rune Cultists",
        category: "Combat Stress",
        description: "Three neutral rune cultists wander the zone, laying runes as they go.")]
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
