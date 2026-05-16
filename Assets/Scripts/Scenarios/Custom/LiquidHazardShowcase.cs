using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Liquid Hazard Showcase (LQ.7) — walk a row of pools, get coated,
    /// watch the coat change how the world treats you.
    ///
    ///   [Player] →→ [water] →→ [oil] →→ [acid] →→ [brine]
    ///
    /// Walking east drags the player through four pools in turn. The
    /// <see cref="LiquidDemoProbePart"/> on the player logs every coat
    /// apply/expire and the live resistance snapshot, so the LQ.4–LQ.6
    /// mechanics are observable in the message log without a debugger:
    ///
    ///   - water  → LiquidCovered(water) + WetEffect (div #3). A Fire
    ///              hit is dampened; a Lightning hit is amplified.
    ///   - oil    → LiquidCovered(oil). A Fire hit is amplified
    ///              (Combustibility 90) — the lethal trade-off.
    ///   - acid   → LiquidCovered(acid). PerTurnDamage ticks each turn.
    ///   - brine  → LiquidCovered(brine). +15 HeatResistance AND
    ///              −15 ElectricResistance simultaneously (the §3
    ///              trade-off), netting back to zero when it dries.
    ///
    /// Honesty bound: pools transfer on cell-ENTER (divergence #5), so
    /// you must actually step onto each cell; standing still does not
    /// re-coat. The dominant coat is stronger-wins (divergence #1), so
    /// after the brine pool the player carries whichever last/biggest
    /// liquid won — read the [LiquidDemo] lines for the live id.
    /// </summary>
    [Scenario(
        name: "Liquid Hazard Showcase",
        category: "Combat",
        description: "Walk a water→oil→acid→brine pool row. [LiquidDemo] log lines surface the LQ.4 coat, LQ.5 elemental consequence, and LQ.6 stat trade-off live.")]
    public class LiquidHazardShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .GiveItem("HealingTonic", 5);

            // Probe rides the player and narrates the coat lifecycle.
            if (ctx.PlayerEntity != null &&
                ctx.PlayerEntity.GetPart<LiquidDemoProbePart>() == null)
                ctx.PlayerEntity.AddPart(new LiquidDemoProbePart());

            // Four pools in a row to the east. Constructed directly
            // (RenderPart + non-solid PhysicsPart + LiquidPoolPart) so
            // the scenario is self-contained and doesn't depend on pool
            // blueprints existing for every liquid (brine has none).
            SpawnPool(ctx, "water", 120, p.x + 2, p.y);
            SpawnPool(ctx, "oil", 100, p.x + 4, p.y);
            SpawnPool(ctx, "acid", 80, p.x + 6, p.y);
            SpawnPool(ctx, "brine", 140, p.x + 8, p.y);

            ctx.Log("=== Liquid Hazard Showcase (LQ.4–LQ.6) ===");
            ctx.Log("Walk EAST through the row: water → oil → acid → brine.");
            ctx.Log("water: Fire dampened / Lightning amplified (+ WetEffect).");
            ctx.Log("oil:   Fire AMPLIFIED (Combustibility 90) — lethal.");
            ctx.Log("acid:  ticks Acid damage every turn while coated.");
            ctx.Log("brine: +15 HeatRes AND −15 ElectricRes (nets zero on dry).");
            ctx.Log("Watch [LiquidDemo] lines: coat id/amount + live resistances.");
            ctx.Log("Note: coat transfers on cell-ENTER only (stand still = no re-coat).");
        }

        private static void SpawnPool(ScenarioContext ctx, string id, int vol, int x, int y)
        {
            var pool = new Entity { ID = id + "Pool", BlueprintName = id + "Pool" };
            pool.AddPart(new RenderPart { DisplayName = id + " pool", RenderString = "~" });
            pool.AddPart(new PhysicsPart { Solid = false });
            pool.AddPart(new LiquidPoolPart { LiquidId = id, Volume = vol });
            ctx.Zone.AddEntity(pool, x, y);
        }
    }

    /// <summary>
    /// Showcase-only probe. Narrates the coat lifecycle from the
    /// player's seat: every <c>EffectApplied</c>/<c>EffectRemoved</c>
    /// for a <see cref="LiquidCoveredEffect"/> logs the dominant liquid
    /// id, scalar amount, and the live HeatResistance/ElectricResistance
    /// so the LQ.6 stat trade-off is visible as it applies and reverses.
    /// Production combat does not emit these lines.
    /// </summary>
    public class LiquidDemoProbePart : Part
    {
        public override string Name => "LiquidDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EffectApplied" && e.ID != "EffectRemoved")
                return true;
            if (!(e.GetParameter("Effect") is LiquidCoveredEffect coat))
                return true;

            string verb = e.ID == "EffectApplied" ? "coated" : "cleared";
            int heat = ParentEntity?.GetStatValue("HeatResistance", 0) ?? 0;
            int elec = ParentEntity?.GetStatValue("ElectricResistance", 0) ?? 0;
            MessageLog.Add(
                $"[LiquidDemo] player {verb}: {coat.LiquidId} amount={coat.Amount} " +
                $"(HeatRes={heat} ElecRes={elec})");
            return true;
        }
    }
}
