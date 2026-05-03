using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// QA-aid scenario placing all 9 elemental-resistance creatures in a
    /// labeled lineup around the player. No combat, no quests, no
    /// scripted action — purely "look at the lineup" content
    /// verification for the elemental resistance matrix.
    ///
    /// Layout (relative to player p):
    ///
    ///    p+1,p-1: Snapjaw          (ColdResistance = 25)
    ///    p+2,p-1: SnapjawHunter    (ColdResistance = 50)
    ///    p+3,p-1: IceWight         (ColdResistance = 100, HeatResistance = -50)
    ///    p+4,p-1: CharredHusk      (HeatResistance  = 100, ColdResistance = -50)
    ///    p+1,p+1: Glowmaw          (HeatResistance  = 50)
    ///    p+2,p+1: StoneGolem       (ElectricResistance = 50)
    ///    p+3,p+1: BrassHusk        (ElectricResistance = -50)
    ///    p+4,p+1: CaveSlime        (AcidResistance  = 50)
    ///    p+5,p+1: Scorpion         (AcidResistance  = -50)
    ///
    /// Player loadout: HP 999 (so they survive walking through the zoo),
    /// elemental weapons placed on the floor adjacent to the player
    /// (FlamingSword north, IceSword northwest, ThunderHammer south,
    /// AcidicDagger southwest — manual pickup so the player consciously
    /// chooses which axis to test). The player can pick up any weapon
    /// and swing it at adjacent creatures to observe damage scaling.
    ///
    /// Diag observability: nothing emitted on Apply itself (the scenario
    /// is purely layout). When the player swings, the existing damage/
    /// channel records each attribute-tagged hit — combine that with the
    /// existing resistance computation to verify per-axis scaling.
    /// </summary>
    [Scenario(
        name: "Elemental Creature Zoo",
        category: "Combat",
        description: "QA layout: 9 resistance creatures (Acid/Cold/Heat/Electric × -50/25/50/100). Verifies the elemental resistance matrix end-to-end.")]
    public class ElementalCreatureZoo : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Beefy player so they survive walking through the zoo.
            ctx.Player
                .SetStatMax("Hitpoints", 999)
                .SetHp(999)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24);

            // Clear the corridor so spawns aren't blocked by floor decor.
            for (int dx = 1; dx <= 5; dx++)
            {
                ctx.World.ClearCell(p.x + dx, p.y - 1);
                ctx.World.ClearCell(p.x + dx, p.y + 1);
            }

            // ---- Cold axis (north row) ----
            // 25 / 50 / 100 / -50(opposite) — ascending resistance, then the
            // dual-axis CharredHusk on the far right (Heat-immune, Cold-vulnerable)
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 1, p.y - 1);
            ctx.Spawn("SnapjawHunter")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 2, p.y - 1);
            ctx.Spawn("IceWight")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 3, p.y - 1);
            ctx.Spawn("CharredHusk")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 4, p.y - 1);

            // ---- Heat / Electric / Acid axes (south row) ----
            ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 1, p.y + 1);
            ctx.Spawn("StoneGolem")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 2, p.y + 1);
            ctx.Spawn("BrassHusk")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 3, p.y + 1);
            ctx.Spawn("CaveSlime")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 4, p.y + 1);
            ctx.Spawn("Scorpion")
                .WithStatMax("Hitpoints", 200).WithHpAbsolute(200)
                .At(p.x + 5, p.y + 1);

            // Hand the player one of each elemental weapon for cross-axis
            // testing. Drop them on the floor under the player rather than
            // forcing into inventory so the player can pick them up by
            // pressing pickup on the spot.
            ctx.Spawn("FlamingSword").At(p.x, p.y - 1);
            ctx.Spawn("IceSword").At(p.x - 1, p.y - 1);
            ctx.Spawn("ThunderHammer").At(p.x, p.y + 1);
            ctx.Spawn("AcidicDagger").At(p.x - 1, p.y + 1);

            ctx.Log("=== Elemental Creature Zoo ===");
            ctx.Log("North row (Cold axis ascending → CharredHusk = Heat-immune):");
            ctx.Log("  Snapjaw (Cold +25), SnapjawHunter (Cold +50),");
            ctx.Log("  IceWight (Cold +100 / Heat -50), CharredHusk (Heat +100 / Cold -50)");
            ctx.Log("South row (Heat / Electric / Acid axes):");
            ctx.Log("  Glowmaw (Heat +50), StoneGolem (Electric +50), BrassHusk (Electric -50),");
            ctx.Log("  CaveSlime (Acid +50), Scorpion (Acid -50)");
            ctx.Log("Weapons on the floor at p±1: FlamingSword, IceSword, ThunderHammer, AcidicDagger.");
            ctx.Log("Tip: use diag_query category=damage to observe per-attribute damage scaling.");
        }
    }
}
