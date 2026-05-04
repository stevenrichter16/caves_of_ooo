using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// EmberSpear Showcase — Heat-axis mirror of <see cref="CryoLanceShowcase"/>.
    /// Demonstrates the second 100%-immune creature and the second negative-
    /// resistance creature pair, completing the symmetric resistance-extreme
    /// matrix:
    ///
    ///   - Full immunity (resistance ≥ 100): EmberSpear × CharredHusk = 0 damage
    ///     (mirrors CryoLance × IceWight on the Heat axis)
    ///   - Full vulnerability (resistance ≤ -50): IceSword × CharredHusk = 1.5×
    ///     damage (mirrors FlamingSword × IceWight)
    ///
    /// Layout (player at left, walks east):
    ///
    ///                   [Glowmaw NE: HR=50 — graded contrast]
    ///                                ↗
    ///   [Player] →→→→→ [CharredHusk E: HR=100, CR=-50 — extremes]
    ///                                ↘
    ///                   [SnapjawHunter SE: CR=50 — Cold-resist contrast]
    ///
    /// What the player should see when they swing each weapon at each target:
    ///
    /// | Swing                     | Expected         | Why                          |
    /// |---------------------------|------------------|------------------------------|
    /// | EmberSpear vs CharredHusk | **0 damage**     | HR=100 × Fire = full immunity|
    /// | IceSword vs CharredHusk   | **1.5× damage**  | CR=-50 × Cold = vulnerable   |
    /// | EmberSpear vs Glowmaw     | halved           | HR=50 × Fire = graded        |
    /// | EmberSpear vs SnapjawHunter | full damage    | SnapjawHunter has no HR      |
    /// | IceSword vs SnapjawHunter | halved           | CR=50 × Cold (control case)  |
    ///
    /// The [ElementalDemo] probe lines (reused from ElementalSwordsShowcase)
    /// surface the live HR/CR values plus the FIRE/ICE flag on each hit.
    /// </summary>
    [Scenario(
        name: "EmberSpear Showcase",
        category: "Combat",
        description: "Heat-axis mirror of CryoLance Showcase. Second Piercing-class elemental weapon (EmberSpear) + second 100%-immune creature (CharredHusk). Resistance extremes on the Fire/Cold axis — 0 damage on full immunity, 1.5× on full vulnerability.")]
    public class EmberSpearShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: EmberSpear + IceSword + survival kit ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("EmberSpear")
                .GiveItem("IceSword", 1)
                .GiveItem("HealingTonic", 5);

            // === E: CharredHusk — HR=100 (immune), CR=-50 (vulnerable) ===
            // The pair-headlining encounter. EmberSpear lands NOTHING on this
            // target; IceSword does extra damage. The second 100%-immune
            // creature in the game (IceWight is the first, on Cold).
            var husk = ctx.Spawn("CharredHusk")
                .WithStatMax("Hitpoints", 100)
                .WithHpAbsolute(100)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (husk != null)
                husk.AddPart(new ElementalDemoProbePart());

            // === NE: Glowmaw — HR=50, no CR ===
            // Graded-resistance contrast: shows EmberSpear damage is halved
            // (not zeroed) by a partial HR. Demonstrates that the resistance
            // formula is graded, not binary.
            var glowmaw = ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (glowmaw != null)
                glowmaw.AddPart(new ElementalDemoProbePart());

            // === SE: SnapjawHunter — CR=50, no HR ===
            // Control: EmberSpear lands FULL damage (no HR on SnapjawHunter).
            // IceSword on SnapjawHunter is HALVED — the existing Cold-resist
            // sanity check.
            var hunter = ctx.Spawn("SnapjawHunter")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (hunter != null)
                hunter.AddPart(new ElementalDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== EmberSpear Showcase (resistance extremes — Heat axis) ===");
            ctx.Log("Loadout: EmberSpear equipped, IceSword in inventory.");
            ctx.Log("E  CharredHusk    (HR=100, CR=-50): EmberSpear → 0 damage. IceSword → 1.5×.");
            ctx.Log("NE Glowmaw        (HR=50,  CR=0):    EmberSpear → halved.");
            ctx.Log("SE SnapjawHunter  (CR=50,  HR=0):    EmberSpear → full. IceSword → halved.");
            ctx.Log("[ElementalDemo] lines fire on each hit with live HR/CR.");
        }
    }
}
