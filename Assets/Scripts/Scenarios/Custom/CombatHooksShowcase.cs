using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phases F/G/H showcase. Three concrete listeners + one player-side stat
    /// to demonstrate the new combat hooks visibly:
    ///
    ///   - **F (BeforeTakeDamage)**: a Snapjaw with <see cref="ShowcaseStoneSkinPart"/>
    ///     reduces incoming damage by 2 and logs the reduction. Each hit's
    ///     log line shows raw → reduced damage.
    ///
    ///   - **G (MultiWeaponSkillBonus)**: the player gets +5 to off-hand
    ///     swings. Without a control fighter to compare against (we're not
    ///     spawning a no-stat-bonus copy of the player), the demonstration is
    ///     "hit reliability against off-hand-targeted dummies." The stat
    ///     value is logged so a human can read it back via execute_code.
    ///
    ///   - **H (CanBeDismembered)**: a Snapjaw with <see cref="ShowcaseIndestructiblePart"/>
    ///     vetoes every dismemberment attempt. The "[Showcase] Indestructible: vetoed"
    ///     line fires whenever the chance roll passed but the limb stays attached.
    ///
    /// IMPORTANT — what's NOT visible in vanilla play:
    ///
    /// The new hooks fire silently in normal combat — there's no UI for "your
    /// damage was reduced by an effect" or "the limb didn't sever." This
    /// scenario adds <see cref="MessageLog"/> lines from the showcase parts
    /// so humans can see the events firing without reading the game code.
    ///
    /// To verify the hooks fire as expected:
    ///   1. Launch this scenario (Caves Of Ooo / Scenarios / Combat Stress / Combat Hooks Showcase)
    ///   2. Walk up to each Snapjaw and attack
    ///   3. Watch the message log:
    ///      - Hits on the StoneSkin Snapjaw show "[Showcase] StoneSkin: X -> X-2"
    ///      - Hits on the Indestructible Snapjaw never sever limbs even with
    ///        massive damage; "[Showcase] Indestructible: vetoed" fires on
    ///        every attempted dismemberment
    /// </summary>
    [Scenario(
        name: "Combat Hooks Showcase",
        category: "Combat",
        description: "Phases F/G/H listeners: StoneSkin (BeforeTakeDamage), +5 off-hand bonus, Indestructible (CanBeDismembered).")]
    public class CombatHooksShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Phase G: give the player +5 MultiWeaponSkillBonus ===
            // Stat doesn't exist on the player by default — add it directly.
            // PlayerBuilder.SetStat skips missing stats, so the dictionary
            // assignment is the right tool here.
            ctx.PlayerEntity.Statistics["MultiWeaponSkillBonus"] = new Stat
            {
                Owner = ctx.PlayerEntity,
                Name = "MultiWeaponSkillBonus",
                BaseValue = 5,
                Min = -10,
                Max = 10
            };

            // === Phase F: StoneSkin Snapjaw (NW, player+2,-2) ===
            // Listens for BeforeTakeDamage and reduces incoming damage by 2.
            var stoneSkin = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 9999)
                .WithHpAbsolute(9999)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 2, p.y - 2);
            if (stoneSkin != null)
                stoneSkin.AddPart(new ShowcaseStoneSkinPart());

            // === Control: normal Snapjaw (E, player+3,0) ===
            // No probes attached. Used for visual comparison vs StoneSkin.
            ctx.Spawn("Snapjaw")
               .WithStatMax("Hitpoints", 9999)
               .WithHpAbsolute(9999)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .At(p.x + 3, p.y);

            // === Phase H: Indestructible Snapjaw (NE, player+4,-2) ===
            // Vetoes CanBeDismembered. Limbs stay attached even on massive hits.
            var indestructible = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 9999)
                .WithHpAbsolute(9999)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 4, p.y - 2);
            if (indestructible != null)
                indestructible.AddPart(new ShowcaseIndestructiblePart());

            ctx.Log("=== Combat Hooks Showcase (Phases F/G/H) ===");
            ctx.Log("Player: MultiWeaponSkillBonus = +5 (off-hand swings hit more reliably).");
            ctx.Log("StoneSkin Snapjaw NW — every hit reduced by 2. Look for '[Showcase] StoneSkin: X -> Y'.");
            ctx.Log("Control Snapjaw E — no probes; reference for off-hand swing rate.");
            ctx.Log("Indestructible Snapjaw NE — limbs never sever. Look for '[Showcase] Indestructible: vetoed'.");
        }
    }

    /// <summary>
    /// Showcase-only Part that hooks <c>BeforeTakeDamage</c> and reduces
    /// incoming damage by 2. Logs the reduction to <see cref="MessageLog"/>
    /// so the F event hook is visible to a human player without reading code.
    ///
    /// This is intentionally a scenario-only Part — production code should
    /// not depend on it. Future production listeners (e.g., a real
    /// "Stoneskin" status effect) would follow the same pattern.
    /// </summary>
    public class ShowcaseStoneSkinPart : Part
    {
        public override string Name => "ShowcaseStoneSkin";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                int before = d.Amount;
                d.Amount -= 2;  // setter clamps to ≥ 0
                MessageLog.Add($"[Showcase] StoneSkin: {before} -> {d.Amount}");
            }
            return true;
        }
    }

    /// <summary>
    /// Showcase-only Part that hooks <c>CanBeDismembered</c> and vetoes
    /// every dismemberment attempt. Logs the veto to <see cref="MessageLog"/>
    /// so the H event hook is visible to a human player.
    ///
    /// As with <see cref="ShowcaseStoneSkinPart"/>: scenario-only. A real
    /// "Indestructible Bones" mutation or "TitaniumLimbs" effect would
    /// follow the same pattern.
    /// </summary>
    public class ShowcaseIndestructiblePart : Part
    {
        public override string Name => "ShowcaseIndestructible";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "CanBeDismembered")
            {
                MessageLog.Add("[Showcase] Indestructible: vetoed dismemberment");
                return false;  // veto
            }
            return true;
        }
    }
}
