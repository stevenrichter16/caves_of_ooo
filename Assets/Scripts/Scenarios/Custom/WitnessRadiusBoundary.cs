namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.3 radius-boundary showcase — three Scribes at Chebyshev
    /// distances 3, 8, and 10 from the death cell. The mid-distance
    /// Scribe sits on the radius-8 boundary (inclusive per
    /// <c>BroadcastDeathWitnessed</c>'s <c>if (dist > radius) continue;</c>
    /// gate); the far Scribe is just outside.
    ///
    /// Expected flow when launched:
    /// - Scribe A at player+2,0  → distance 3 to death cell at player+5,0 → shakes.
    /// - Scribe B at player-3,0  → distance 8 (boundary, inclusive) → shakes.
    /// - Scribe C at player-5,0  → distance 10 → does NOT shake.
    /// - Player kills the one-HP Snapjaw at player+5,0.
    /// - Two "Scribe looks shaken." messages in the log (A and B).
    /// - Scribe C remains still.
    ///
    /// Good for:
    /// - Binary A/B/C differentiation on distance with otherwise-identical witnesses
    /// - Visibly confirming dist == radius is inclusive (pins the
    ///   HandleDeath_Broadcast_IncludesWitnessAtExactRadius unit test)
    /// - Preventing silent regressions from a future `>` -> `>=` flip
    ///
    /// Lower gameplay value than S2/S3 (the unit test already pins this
    /// exactly) but keeps the boundary visible during a manual sweep if
    /// the radius ever needs tuning.
    /// </summary>
    [Scenario(
        name: "Witness Radius Boundary (M2.3)",
        category: "AI Behavior",
        description: "Three Scribes at dist 3, 8, 10 from kill. Boundary-8 shakes; far-10 doesn't.")]
    public class WitnessRadiusBoundary : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row so the player can reach the Snapjaw and the
            // kill-row isn't blocked by compass stones / chest.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // All three Scribes arranged SOUTH of the kill cell, on the same
            // vertical column (x = player+5). This places each Scribe on a
            // clear vertical LOS line to the death cell without crossing the
            // player (who is Solid) or any compass stone.
            //
            // Chebyshev distances from the kill cell at (player+5, 0):
            //   (+5, +3):  max(0, 3)  = 3    ← inside radius
            //   (+5, +8):  max(0, 8)  = 8    ← boundary, inclusive
            //   (+5, +10): max(0, 10) = 10   ← outside radius
            ctx.Spawn("Scribe").AtPlayerOffset(5,  3);
            ctx.Spawn("Scribe").AtPlayerOffset(5,  8);
            ctx.Spawn("Scribe").AtPlayerOffset(5, 10);

            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Kill the Snapjaw east of you. Two Scribes shake (dist 3 and dist 8 — boundary inclusive). Far Scribe at dist 10 does not.");
        }
    }
}
