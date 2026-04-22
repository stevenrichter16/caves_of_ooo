namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.3 LOS-filter showcase — two Scribes, identical in every way
    /// except one has line-of-sight to the kill and the other doesn't
    /// (wall between). Pins the LOS-filter branch of
    /// <c>CombatSystem.BroadcastDeathWitnessed</c> as a visible A/B test.
    ///
    /// Expected flow when launched:
    /// - Scribe #1 at player+3,0 (north of the kill path, clear LOS).
    /// - Wall belt at player+4..6 on y=+2 (blocks LOS from south).
    /// - Scribe #2 at player+5,3 (south of the wall belt, LOS blocked).
    /// - One-HP Snapjaw at player+5,0 (the kill cell).
    /// - Player kills the Snapjaw.
    /// - Scribe #1 receives WitnessedEffect ("Scribe looks shaken.")
    /// - Scribe #2 does NOT — wall blocks LOS.
    /// - Visually: north scribe paces, south scribe stays still.
    ///
    /// Good for:
    /// - Verifying AIHelpers.HasLineOfSight is actually consulted in the
    ///   broadcast path (not just in doc)
    /// - Binary A/B differentiation on LOS with otherwise-identical witnesses
    /// - Pinning Bresenham's behavior through a wall belt (both x-offsets
    ///   between the two walls are blocked, so no sneaky corner-peek path)
    /// </summary>
    [Scenario(
        name: "Witness LOS Wall (M2.3)",
        category: "AI Behavior",
        description: "Two Scribes, one behind a wall. Kill the Snapjaw; only the Scribe with LOS shakes.")]
    public class WitnessLineOfSightWall : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row so Scribe #1 has LOS to the kill cell and
            // the player can reach the Snapjaw. The starting zone's West
            // compass stone (player+2,0) and grimoire chest (player+4,0)
            // would both block LOS on the kill-row otherwise.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Scribe #1 — clear LOS to the death cell.
            ctx.Spawn("Scribe").AtPlayerOffset(3, 0);

            // Wall belt at y=+3 (south of the kill row). Runs below the
            // starting zone's South compass stone at (43,13) = player+4,+2,
            // which would otherwise silently block one of the wall
            // placements and leak LOS through the gap.
            ctx.World.PlaceObject("Wall").AtPlayerOffset(4, 3);
            ctx.World.PlaceObject("Wall").AtPlayerOffset(5, 3);
            ctx.World.PlaceObject("Wall").AtPlayerOffset(6, 3);

            // Scribe #2 — identical to #1, but wall-blocked LOS to the death cell.
            ctx.Spawn("Scribe").AtPlayerOffset(5, 4);

            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Kill the Snapjaw. North Scribe (clear LOS) shakes; south Scribe (behind wall) stays still.");
        }
    }
}
