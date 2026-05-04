using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// QA-aid scenario placing every shipped status tonic on the floor
    /// around the player so a content author can pick up + drink each
    /// one and observe the resulting status effect via the diag
    /// substrate's `effect/OnApply` channel.
    ///
    /// Layout (relative to player p, 2 rows × 5 columns east):
    ///
    ///   [HealingTonic][PoisonTonic][FireTonic ][FrostTonic][AcidTonic]   ← row p-1
    ///                                  ↑ player
    ///   [LightningT.][WaterTonic ][StoneskinT.][BleedTonic][CharredT.]   ← row p+1
    ///
    /// 10 tonics total: 8 pre-existing + the 2 new (BleedTonic from
    /// T1.2, CharredTonic from T1.3). Pairs with the existing `effect/
    /// OnApply` diag channel (D2 substrate) so every drink is
    /// automatically observable via diag_query without scenario-side
    /// instrumentation.
    ///
    /// Player loadout: HP 200 (so they survive any self-applied
    /// damage from drinking PoisonTonic / FireTonic / BleedTonic
    /// over a few turns).
    /// </summary>
    [Scenario(
        name: "Tonic Test Bench",
        category: "Combat",
        description: "QA layout: every status tonic on the floor in a labeled grid. Drink + observe via effect/OnApply diag.")]
    public class TonicTestBench : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Beefy player — survives multi-turn DoTs and stack-apply
            // experiments without dying mid-test.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .SetStat("Toughness", 18);

            // Clear both rows east of the player so tonics spawn cleanly.
            for (int dx = 1; dx <= 5; dx++)
            {
                ctx.World.ClearCell(p.x + dx, p.y - 1);
                ctx.World.ClearCell(p.x + dx, p.y + 1);
            }

            // North row: pre-existing status tonics (the ones that ship
            // before this T1 closeout — listed left-to-right in roadmap
            // ordering for visual stability).
            ctx.Spawn("HealingTonic")  .At(p.x + 1, p.y - 1);
            ctx.Spawn("PoisonTonic")   .At(p.x + 2, p.y - 1);
            ctx.Spawn("FireTonic")     .At(p.x + 3, p.y - 1);
            ctx.Spawn("FrostTonic")    .At(p.x + 4, p.y - 1);
            ctx.Spawn("AcidTonic")     .At(p.x + 5, p.y - 1);

            // South row: remaining 5 — Lightning, Water, Stoneskin, plus
            // the two new T1 ships at the right edge for visual emphasis.
            ctx.Spawn("LightningTonic").At(p.x + 1, p.y + 1);
            ctx.Spawn("WaterTonic")    .At(p.x + 2, p.y + 1);
            ctx.Spawn("StoneskinTonic").At(p.x + 3, p.y + 1);
            ctx.Spawn("BleedTonic")    .At(p.x + 4, p.y + 1);
            ctx.Spawn("CharredTonic")  .At(p.x + 5, p.y + 1);

            ctx.Log("=== Tonic Test Bench ===");
            ctx.Log("North row (p-1): Healing, Poison, Fire, Frost, Acid");
            ctx.Log("South row (p+1): Lightning, Water, Stoneskin, Bleed, Charred");
            ctx.Log("Pickup + drink each tonic. Observe via:");
            ctx.Log("  diag_query category=effect kind=OnApply");
            ctx.Log("Each drink emits effect/OnApply with payload.effect = " +
                "<EffectClassName> and target = player.");
        }
    }
}
