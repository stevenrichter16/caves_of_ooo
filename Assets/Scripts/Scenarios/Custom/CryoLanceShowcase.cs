using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// CryoLance Showcase — first end-to-end demonstration of the
    /// resistance-extreme interactions:
    ///
    ///   - Full immunity (resistance ≥ 100): CryoLance × IceWight = 0 damage
    ///   - Full vulnerability (resistance ≤ -50): FlamingSword × IceWight =
    ///     1.5× damage
    ///
    /// Plus the existing graded resistance and no-resistance baselines for
    /// comparison.
    ///
    /// Layout (player at left, walks east):
    ///
    ///                   [SnapjawHunter NE: CR=50 — graded contrast]
    ///                                ↗
    ///   [Player] →→→→→ [IceWight E: CR=100, HR=-50 — extremes]
    ///                                ↘
    ///                   [Glowmaw SE: HR=50, CR=0 — Heat-resist contrast]
    ///
    /// What the player should see when they swing each weapon at each target:
    ///
    /// | Swing                   | Expected             | Why                            |
    /// |-------------------------|----------------------|--------------------------------|
    /// | CryoLance vs IceWight   | **0 damage**         | CR=100 × Ice = full immunity   |
    /// | FlamingSword vs IceWight| **1.5× damage**      | HR=-50 × Fire = vulnerability  |
    /// | CryoLance vs SnapjawHunter | halved             | CR=50 × Ice = graded           |
    /// | CryoLance vs Glowmaw    | full damage          | Glowmaw has no CR              |
    /// | FlamingSword vs Glowmaw | halved               | HR=50 × Fire (control case)    |
    ///
    /// The [ElementalDemo] probe lines (reused from ElementalSwordsShowcase)
    /// surface the live HR/CR values plus the FIRE/ICE flag on each hit.
    /// </summary>
    [Scenario(
        name: "CryoLance Showcase",
        category: "Combat",
        description: "First Piercing-class elemental weapon (CryoLance) + first 100%-immune creature (IceWight). Demonstrates resistance extremes: full immunity (CR=100) and full vulnerability (HR=-50). Side-by-side graded-resistance and no-resistance contrasts.")]
    public class CryoLanceShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: CryoLance + FlamingSword + survival kit ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("CryoLance")
                .GiveItem("FlamingSword", 1)
                .GiveItem("HealingTonic", 5);

            // === E: IceWight — CR=100 (immune), HR=-50 (vulnerable) ===
            // The pair-headlining encounter. CryoLance lands NOTHING on this
            // target; FlamingSword does extra damage. The first creature in
            // the game with full elemental immunity.
            var iceWight = ctx.Spawn("IceWight")
                .WithStatMax("Hitpoints", 100)
                .WithHpAbsolute(100)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (iceWight != null)
                iceWight.AddPart(new ElementalDemoProbePart());

            // === NE: SnapjawHunter — CR=50, no HR ===
            // Graded-resistance contrast: shows CryoLance damage is halved
            // (not zeroed) by a partial CR. Demonstrates that the
            // resistance formula is graded, not binary.
            var hunter = ctx.Spawn("SnapjawHunter")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (hunter != null)
                hunter.AddPart(new ElementalDemoProbePart());

            // === SE: Glowmaw — HR=50, CR=0 ===
            // Control: CryoLance lands FULL damage (Glowmaw has no CR).
            // FlamingSword on Glowmaw is HALVED — the existing Heat-resist
            // sanity check.
            var glowmaw = ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (glowmaw != null)
                glowmaw.AddPart(new ElementalDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== CryoLance Showcase (resistance extremes) ===");
            ctx.Log("Loadout: CryoLance equipped, FlamingSword in inventory.");
            ctx.Log("E  IceWight       (CR=100, HR=-50): CryoLance → 0 damage. FlamingSword → 1.5×.");
            ctx.Log("NE SnapjawHunter  (CR=50,  HR=0):    CryoLance → halved.");
            ctx.Log("SE Glowmaw        (HR=50,  CR=0):    CryoLance → full. FlamingSword → halved.");
            ctx.Log("[ElementalDemo] lines fire on each hit with live HR/CR.");
        }
    }
}
