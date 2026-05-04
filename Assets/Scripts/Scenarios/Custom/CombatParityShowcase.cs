using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phases A/C/D/E showcase. Sets up a small "lineup" of personally-hostile
    /// Snapjaws around the player to exercise the new combat mechanics:
    ///
    /// - 3 normal soak Snapjaws (high HP) so the player can swing repeatedly,
    ///   provoking nat-20 crits at ~5% rate. Each crit's <see cref="Damage"/>
    ///   carries the "Critical" attribute (Phase D).
    ///
    /// - 1 Heat-immune Snapjaw with <c>HeatResistance = 100</c> — Fire damage
    ///   to it fires the <c>DamageFullyResisted</c> event with no HP loss
    ///   (Phase E + self-review Finding 4).
    ///
    /// - 1 Cold-vulnerable Snapjaw with <c>ColdResistance = -100</c> — Cold
    ///   damage doubles. Useful for the "vulnerability" branch of Phase E.
    ///
    /// IMPORTANT — what's NOT visible in vanilla play:
    ///
    /// The combat path tags pure-melee damage with attributes
    /// <c>[Melee, Strength]</c> only. There's no Fire / Cold / Acid attribute
    /// unless a weapon's <c>MeleeWeaponPart.Attributes</c> string declares it
    /// (e.g., <c>"Cutting Fire"</c> on a flaming sword). The player's natural
    /// fist has no such attribute, so:
    /// - melee swings vs the Heat-immune Snapjaw still deal normal damage
    ///   (the Heat resistance never fires because the damage isn't tagged Fire)
    /// - melee swings vs the Cold-vulnerable Snapjaw deal normal damage
    ///   (same reason)
    ///
    /// To exercise the resistance branches in this scenario, use
    /// <c>execute_code</c> to call <c>CombatSystem.ApplyDamage(target, damage, ...)</c>
    /// directly with a <see cref="Damage"/> that includes "Fire" / "Cold". See
    /// the Apply-time log for the recipe.
    ///
    /// Self-review Finding 4 surfaced the gap: full resistance is silent in
    /// the message log right now. The DamageFullyResisted event fires, but no
    /// UI listener consumes it. A future polish pass should hook this to an
    /// in-game "fully resisted!" message.
    /// </summary>
    [Scenario(
        name: "Combat Parity Showcase",
        category: "Combat",
        description: "Phases A/C/D/E lineup: 3 soak Snapjaws + Heat-immune + Cold-vulnerable for crit + resistance demos.")]
    public class CombatParityShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // 3 soak Snapjaws to the east, lined up as personal enemies.
            // High HP keeps them alive long enough for nat-20 crits to surface.
            for (int i = 0; i < 3; i++)
            {
                ctx.Spawn("Snapjaw")
                   .WithStatMax("Hitpoints", 9999)
                   .WithHpAbsolute(9999)
                   .AsPersonalEnemyOf(ctx.PlayerEntity)
                   .At(p.x + 2 + i, p.y);
            }

            // Heat-immune Snapjaw to the northwest. Add the resistance stat
            // post-spawn since Snapjaw's blueprint doesn't include it (the
            // EntityBuilder.WithStat helper requires the stat to pre-exist on
            // the blueprint — we work around that by adding it directly).
            var heatImmune = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 9999)
                .WithHpAbsolute(9999)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 2, p.y - 2);
            if (heatImmune != null)
            {
                heatImmune.Statistics["HeatResistance"] = new Stat
                {
                    Owner = heatImmune,
                    Name = "HeatResistance",
                    BaseValue = 100,
                    Min = 0,
                    Max = 200
                };
            }

            // Cold-vulnerable Snapjaw to the northeast.
            var coldVulnerable = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 9999)
                .WithHpAbsolute(9999)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 4, p.y - 2);
            if (coldVulnerable != null)
            {
                coldVulnerable.Statistics["ColdResistance"] = new Stat
                {
                    Owner = coldVulnerable,
                    Name = "ColdResistance",
                    BaseValue = -100,
                    Min = -100,
                    Max = 200
                };
            }

            ctx.Log("=== Combat Parity Showcase (Phases A/C/D/E) ===");
            ctx.Log("3 soak Snapjaws E (player+2..4) — attack repeatedly. Nat-20 ~5% chance.");
            ctx.Log("  Each hit's Damage object carries [Melee, Strength]. Crits add 'Critical'.");
            ctx.Log("Heat-immune Snapjaw NW (player+2,-2) — HeatResistance=100.");
            ctx.Log("Cold-vulnerable Snapjaw NE (player+4,-2) — ColdResistance=-100.");
            ctx.Log("Note: melee swings don't carry Fire/Cold attributes, so resistance is");
            ctx.Log("  silent in vanilla play. Use execute_code to verify:");
            ctx.Log("    var d = new Damage(20); d.AddAttribute(\"Fire\");");
            ctx.Log("    CombatSystem.ApplyDamage(heatImmune, d, source: null, zone: zone);");
            ctx.Log("    // expected: HP delta = 0, DamageFullyResisted event fires");
        }
    }
}
