using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Throwable consumables showcase — make the "tonics shatter on
    /// impact + AOE radius 1" mechanic player-visible.
    ///
    /// Setup (player at center; coordinates are player-relative):
    ///
    ///     . . . . [Snapjaw NE]
    ///     . . . [Snapjaw N] [Snapjaw NE2]
    ///     . . [Snapjaw NW] [Snapjaw N2] [Snapjaw center]
    ///   [Player]
    ///
    /// Player loadout:
    ///   - 5 of each elemental tonic (Acid, Lightning, Frost, Water,
    ///     Fire) so the player can experiment with multiple effects
    ///     and see them stack
    ///   - 5 HealingTonic for self-heal between throws
    ///   - HP 200, Strength 24 (covers the throw range to the cluster)
    ///   - No special weapon — focus is on the throw mechanic
    ///
    /// What the player should observe:
    ///
    ///   --- Throw FrostTonic at center of cluster ---
    ///   "frost tonic shatters, splashing 5 targets."
    ///   All 5 snapjaws gain FrozenEffect — visible in their cell color
    ///   and as "snapjaw is frozen!" log lines for any caught.
    ///
    ///   --- Throw WaterTonic on the snapjaws ---
    ///   "water tonic shatters, splashing 5 targets."
    ///   All 5 snapjaws gain WetEffect (low-damage but conductive).
    ///
    ///   --- Throw LightningTonic on now-wet snapjaws ---
    ///   "lightning tonic shatters, splashing 5 targets."
    ///   All 5 snapjaws gain ElectrifiedEffect — combo unlocks.
    ///
    ///   --- Throw AcidTonic at empty cell next to a single snapjaw ---
    ///   "acid tonic shatters, splashing 1 target."
    ///   Demonstrates that misses still shatter, with smaller AOE
    ///   reach.
    ///
    ///   --- Throw FireTonic at a wall ---
    ///   "fire tonic; it strikes an obstacle and shatters."
    ///   Demonstrates wall-shatter path; AOE applies at last
    ///   traversable cell.
    ///
    /// The 5-snapjaw cluster ensures multiple targets in AOE radius 1.
    /// Snapjaws are personally hostile so they engage immediately,
    /// giving the player a clear "did the throw matter?" feedback loop.
    /// </summary>
    [Scenario(
        name: "Throwable Tonics Showcase",
        category: "Combat",
        description: "Tonics shatter on impact with radius-1 AOE. Demonstrates direct-hit, miss, and wall-hit shatter paths. 5-snapjaw cluster lets the player see all 5 enemies receive a single tonic's effect.")]
    public class ThrowableTonicsShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout ===
            // Five of each elemental tonic + HealingTonic for staying
            // power. No weapon focus — the showcase is about throwing.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .GiveItem("AcidTonic", 5)
                .GiveItem("LightningTonic", 5)
                .GiveItem("FrostTonic", 5)
                .GiveItem("WaterTonic", 5)
                .GiveItem("FireTonic", 5)
                .GiveItem("HealingTonic", 5);

            // === Snapjaw cluster ===
            // Five snapjaws in a 3×3 around (p.x+4, p.y) — within radius
            // 1 of each other so a single thrown tonic at the center
            // hits all of them. Personally hostile to keep them engaged
            // (they'll close on the player but the cluster stays tight
            // for the first few turns).
            int cx = p.x + 4;
            int cy = p.y;
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 80).WithHpAbsolute(80)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(cx, cy); // center
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 80).WithHpAbsolute(80)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(cx + 1, cy); // east
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 80).WithHpAbsolute(80)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(cx, cy - 1); // north
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 80).WithHpAbsolute(80)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(cx + 1, cy - 1); // northeast
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 80).WithHpAbsolute(80)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(cx, cy + 1); // south

            MessageLog.Add("Throwable Tonics Showcase: throw an elemental tonic into the snapjaw cluster.");
            MessageLog.Add("Tonics shatter on impact and apply their effect in a 3×3 area.");
            MessageLog.Add("Try: Frost (freeze them), Water+Lightning (combo), Acid (DoT), Fire (DoT).");
        }
    }
}
