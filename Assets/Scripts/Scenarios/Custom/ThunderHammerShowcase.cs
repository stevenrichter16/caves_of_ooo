using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// ThunderHammer Showcase — Lightning weapon vs three points on the
    /// resistance curve. Sibling of FlamingSword and Elemental Swords
    /// showcases, but uniquely covers the **vulnerability** half of
    /// Phase E (negative ElectricResistance amplifies damage).
    ///
    ///                   [BrassHusk NE: ER=-50, vulnerable, +50% dmg]
    ///                   ↗  ↗
    ///   [Player] →→→→→ [StoneGolem E: ER=50, halved]
    ///                   ↘  ↘
    ///                   [Snapjaw SE: ER=0, control, full damage]
    ///
    /// Three swings of the ThunderHammer expose the entire Phase E curve:
    ///
    ///   - StoneGolem (ER=+50) → damage halved (`amount × 0.5`)
    ///   - Snapjaw    (ER=0)   → full damage (`amount × 1.0`)
    ///   - BrassHusk  (ER=-50) → damage amplified 50% (`amount × 1.5`)
    ///
    /// This is the first scenario to demonstrate **negative resistance**
    /// in-game. The probe surfaces the live ER value (including its sign)
    /// per hit so the player can read which arm of the formula is firing.
    ///
    /// Player loadout: HP=200/200, Strength=24, ThunderHammer equipped,
    /// Cudgel (non-Lightning Bludgeoning weapon) in inventory as the
    /// "swap to confirm Lightning is the lever" control. All three
    /// targets have HP padded to 200.
    /// </summary>
    [Scenario(
        name: "ThunderHammer Showcase",
        category: "Combat",
        description: "Phase C × Phase E full curve: ThunderHammer vs StoneGolem (ER=50, halved), Snapjaw (control), BrassHusk (ER=-50, amplified). First scenario to expose negative resistance in-game.")]
    public class ThunderHammerShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: ThunderHammer + non-Lightning control ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("ThunderHammer")
                .GiveItem("Cudgel", 1)        // non-Lightning Bludgeoning control
                .GiveItem("HealingTonic", 5);

            // === E: StoneGolem — ER=50, RESISTANT ===
            // Lightning sheds off non-conductive stone. Halved damage.
            var golem = ctx.Spawn("StoneGolem")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (golem != null)
                golem.AddPart(new ThunderHammerDemoProbePart());

            // === NE: BrassHusk — ER=-50, VULNERABLE ===
            // Brass conducts at 0.95 — lightning surges through and
            // amplifies internal damage by 50%.
            var husk = ctx.Spawn("BrassHusk")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (husk != null)
                husk.AddPart(new ThunderHammerDemoProbePart());

            // === SE: Snapjaw — ER=0, CONTROL ===
            // No ElectricResistance. Full damage. Reference point.
            var snapjaw = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (snapjaw != null)
                snapjaw.AddPart(new ThunderHammerDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== ThunderHammer Showcase (Phase C × Phase E full curve) ===");
            ctx.Log("Loadout: ThunderHammer equipped, Cudgel in inventory.");
            ctx.Log("E  StoneGolem (ER=+50): swing ThunderHammer → HALVED. Resistant.");
            ctx.Log("NE BrassHusk  (ER=-50): swing ThunderHammer → AMPLIFIED 1.5×. Vulnerable.");
            ctx.Log("SE Snapjaw    (ER=0):   swing ThunderHammer → full damage. Control.");
            ctx.Log("Watch for [ThunderDemo] log lines — they show the live");
            ctx.Log("ElectricResistance value (including sign) and the LIGHTNING flag.");
            ctx.Log("Swap to Cudgel (Bludgeoning, no Lightning) to confirm the elemental");
            ctx.Log("attribute is what fires resistance — non-Lightning hits ignore ER.");
        }
    }

    /// <summary>
    /// Showcase-only Part. Logs every incoming damage with full attribute
    /// list, the LIGHTNING/non-electric flag, and the live ElectricResistance
    /// value (including negative for vulnerable targets — that's the new
    /// surface this showcase exposes).
    ///
    /// Format:
    ///   [ThunderDemo] {target} incoming: amount=N {ELEMENT_FLAG} ER={value} attrs=[...]
    ///
    /// Where ER value can be negative (vulnerable), zero (no resistance),
    /// or positive (resistant). Player reads the sign + magnitude to
    /// predict the post-resistance damage.
    ///
    /// Scenario-only Part. Production combat does not emit these lines.
    /// </summary>
    public class ThunderHammerDemoProbePart : Part
    {
        public override string Name => "ThunderHammerDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                string label = ParentEntity?.GetDisplayName() ?? "?";
                string elementFlag = d.IsElectricDamage() ? "LIGHTNING" : "non-electric";
                int er = ParentEntity != null
                    ? ParentEntity.GetStatValue("ElectricResistance", 0)
                    : 0;
                string attrs = d.Attributes != null && d.Attributes.Count > 0
                    ? string.Join(",", d.Attributes)
                    : "(none)";

                MessageLog.Add(
                    $"[ThunderDemo] {label} incoming: amount={d.Amount} " +
                    $"{elementFlag} ER={er} attrs=[{attrs}]");
            }
            return true;
        }
    }
}
