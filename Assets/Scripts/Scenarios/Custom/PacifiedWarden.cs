namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.2 showcase — personally-hostile Warden at medium range, player has
    /// Calm at Level 3. The central M2.2 scenario: target an actively
    /// combat-engaged NPC and turn them pacifist mid-fight.
    ///
    /// Expected flow when launched:
    /// - Turn 1: Warden (personally hostile, pushed by AsPersonalEnemyOf)
    ///   pushes KillGoal targeting the player, walks west.
    /// - Player casts Calm eastward (key "8" or via the abilities hotbar
    ///   — Calm auto-binds when learned from StartingMutations).
    /// - On hit, Warden gains NoFightGoal(40 + 3*10 = 70 turns).
    ///   MessageLog: "Warden becomes peaceful."
    /// - Warden stops pursuing. Look mode over her now reads "pacified"
    ///   instead of the faction-default relation label.
    /// - Cast Calm a SECOND time while she's still pacified → idempotency
    ///   branch fires. MessageLog: "Warden is already at peace." (the
    ///   M2 post-review UX fix — commit 585b73b).
    /// - After ~70 turns, NoFightGoal expires. Warden resumes pursuit.
    ///
    /// Good for:
    /// - Verifying the end-to-end Calm cast path against a live enemy
    /// - Confirming the "pacified" look-mode label (commit 4cc7d3d)
    /// - Exercising idempotent re-cast feedback (M2 post-review fix)
    /// - Observing NoFightGoal's lifecycle end-to-end
    /// </summary>
    [Scenario(
        name: "Pacified Warden (M2.2 Calm)",
        category: "Mutations",
        description: "Hostile Warden + player with Calm L3 — pacify her mid-pursuit.")]
    public class PacifiedWarden : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Player.AddMutation("CalmMutation", level: 3);

            // Clear the east row so the Calm projectile can reach the Warden.
            // The starting zone's West compass stone sits at player+2,0 and
            // the grimoire chest at player+4,0 — both are Solid with
            // Hitpoints, so LineTargeting would hit them as the projectile's
            // first impact and the Warden at +5 would never be touched.
            // See VillagePopulationBuilder.PlaceCompassStones for stone
            // placement; Chest blueprint has Physics.Solid=true (commit 4eda9bf).
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Warden 5 east of player so the cast's 6-cell range comfortably
            // reaches. AsPersonalEnemyOf ensures she engages regardless of
            // faction alignment.
            ctx.Spawn("Warden")
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Cast Calm eastward (key 8). Pacified Warden stops pursuing for ~70 turns.");
        }
    }
}
