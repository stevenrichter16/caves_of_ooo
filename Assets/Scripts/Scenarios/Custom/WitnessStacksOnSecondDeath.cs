namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.3 stacking showcase — one Scribe, two one-HP Snapjaws.
    /// Kill A, Scribe shakes. Kill B shortly after, the existing
    /// WitnessedEffect's <c>OnStack</c> fires instead of adding a
    /// duplicate to the status list.
    ///
    /// OnStack rule (WitnessedEffect.cs:82–92): extend the existing
    /// Duration only if the incoming Duration is STRICTLY greater.
    /// Both broadcasts push <c>new WitnessedEffect(duration: 20)</c>,
    /// so the second kill's incoming Duration is NOT greater than the
    /// remaining Duration (unless the first ran down), and the
    /// existing clock continues uninterrupted.
    ///
    /// Expected flow when launched:
    /// - Kill Snapjaw A → "Scribe looks shaken." (Duration = 20 fresh)
    /// - Kill Snapjaw B within a few turns → OnStack fires, NO second
    ///   "looks shaken" message (idempotency — only one effect on list)
    /// - Scribe's pacing continues from wherever Duration currently is
    /// - If Snapjaw B is killed after Snapjaw A's Duration drops to
    ///   below 20, the second kill extends back up to 20
    ///
    /// Good for:
    /// - Confirming OnStack prevents duplicate effects in the status list
    /// - Verifying no "message flood" on repeated kills near the same
    ///   witness
    /// - Pinning the "shock doesn't reset shorter" rule against a future
    ///   accidental flip to always-extend
    /// </summary>
    [Scenario(
        name: "Witness Stacks on Second Death (M2.3)",
        category: "AI Behavior",
        description: "Kill two Snapjaws near one Scribe. Second kill's effect stacks; no duplicate 'shaken' message.")]
    public class WitnessStacksOnSecondDeath : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row so LOS from Scribe to both kill cells is
            // unblocked and the player can reach the Snapjaws without
            // routing around compass stones or the chest.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            ctx.Spawn("Scribe").AtPlayerOffset(3, 0);

            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            // Second Snapjaw two rows down so the player has to move to attack it,
            // giving the Scribe a few turns of pacing between the two kills.
            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 2);

            ctx.Log("Kill both Snapjaws. Scribe shakes once; second kill's OnStack continues the effect without a duplicate message.");
        }
    }
}
