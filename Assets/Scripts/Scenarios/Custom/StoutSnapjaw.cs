namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Showcase scenario for Phase 2b modifiers. Spawns a beefed-up Snapjaw at
    /// 100 HP (double-raised Max and BaseValue), high Strength, wielding a
    /// LongSword, personally-hostile to the player so it aggros on sight even
    /// before faction logic kicks in.
    ///
    /// Good for:
    /// - testing sustained 1v1 combat balance (100 HP takes real investment to down)
    /// - observing ranged mutation damage vs high-HP targets
    /// - verifying the PersonalEnemies bypass path (hostile without LOS of faction rep)
    /// </summary>
    [Scenario(
        name: "Personally-hostile Stout Snapjaw",
        category: "Combat Stress",
        description: "100 HP Snapjaw with LongSword, personally hostile to the player.")]
    public class StoutSnapjaw : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Spawn("Snapjaw")
               .WithStatMax("Hitpoints", 100).WithStat("Hitpoints", 100)
               .WithStatMax("Strength", 30).WithStat("Strength", 28)
               .WithEquipment("LongSword")
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Stout Snapjaw spawned — 100 HP beefcake wielding LongSword.");
        }
    }
}
