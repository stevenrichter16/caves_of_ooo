using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// AcidicDagger Showcase — Acid weapon vs three points on the
    /// resistance curve. Sibling of FlamingSword/IceSword (positive
    /// resistance only) and ThunderHammer (full curve including
    /// negative resistance).
    ///
    ///                   [Scorpion NE: AR=-50, vulnerable, +50% dmg]
    ///                   ↗  ↗
    ///   [Player] →→→→→ [CaveSlime E: AR=+50, halved]
    ///                   ↘  ↘
    ///                   [Snapjaw SE: AR=0, control, full damage]
    ///
    /// Three swings of the AcidicDagger expose the entire Phase E curve
    /// for the Acid axis:
    ///
    ///   - CaveSlime (AR=+50) → damage halved (slime is chemically inert,
    ///     mostly water — acid washes off)
    ///   - Snapjaw   (AR=0)   → full damage (control, no AR stat)
    ///   - Scorpion  (AR=-50) → amplified 1.5× (chitinous exoskeleton
    ///     dissolves under acid)
    ///
    /// Player loadout: HP=200/200, Strength=24, AcidicDagger equipped,
    /// base Dagger (non-Acid Piercing weapon) in inventory as the
    /// "swap to confirm Acid is the lever" control. All three targets
    /// have HP padded to 200.
    /// </summary>
    [Scenario(
        name: "AcidicDagger Showcase",
        category: "Combat",
        description: "Phase C × Phase E full curve for Acid: AcidicDagger vs CaveSlime (AR=+50, halved), Snapjaw (control), Scorpion (AR=-50, amplified). Pair with the base Dagger to confirm Acid is the lever.")]
    public class AcidicDaggerShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: AcidicDagger + non-Acid control Dagger ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("AcidicDagger")
                .GiveItem("Dagger", 1)        // non-Acid Piercing control
                .GiveItem("HealingTonic", 5);

            // === E: CaveSlime — AR=+50, RESISTANT ===
            // Slime is mostly water and Organic+Wet — acid mostly washes off.
            var slime = ctx.Spawn("CaveSlime")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (slime != null)
                slime.AddPart(new AcidicDaggerDemoProbePart());

            // === NE: Scorpion — AR=-50, VULNERABLE ===
            // Chitinous exoskeleton (Brittleness 0.3 already declared)
            // dissolves under acid — damage amplified 1.5×.
            var scorpion = ctx.Spawn("Scorpion")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (scorpion != null)
                scorpion.AddPart(new AcidicDaggerDemoProbePart());

            // === SE: Snapjaw — AR=0, CONTROL ===
            // No AcidResistance. Reference point for full damage.
            var snapjaw = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (snapjaw != null)
                snapjaw.AddPart(new AcidicDaggerDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== AcidicDagger Showcase (Phase C × Phase E full curve, Acid axis) ===");
            ctx.Log("Loadout: AcidicDagger equipped, Dagger in inventory.");
            ctx.Log("E  CaveSlime (AR=+50): swing AcidicDagger → HALVED. Resistant slime.");
            ctx.Log("NE Scorpion  (AR=-50): swing AcidicDagger → AMPLIFIED 1.5×. Chitin dissolves.");
            ctx.Log("SE Snapjaw   (AR=0):   swing AcidicDagger → full damage. Control.");
            ctx.Log("Watch for [AcidDemo] log lines — they show the live");
            ctx.Log("AcidResistance value (including sign) and the ACID flag.");
            ctx.Log("Swap to base Dagger (Piercing, no Acid) to confirm the elemental");
            ctx.Log("attribute is what fires resistance — non-Acid hits ignore AR.");
        }
    }

    /// <summary>
    /// Showcase-only Part. Logs every incoming damage with full attribute
    /// list, the ACID/non-acid flag, and the live AcidResistance value
    /// (including negative for vulnerable targets).
    ///
    /// Format:
    ///   [AcidDemo] {target} incoming: amount=N {ELEMENT_FLAG} AR={value} attrs=[...]
    ///
    /// Where AR value can be negative (vulnerable), zero (no resistance),
    /// or positive (resistant).
    ///
    /// Scenario-only Part. Production combat does not emit these lines.
    /// </summary>
    public class AcidicDaggerDemoProbePart : Part
    {
        public override string Name => "AcidicDaggerDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                string label = ParentEntity?.GetDisplayName() ?? "?";
                string elementFlag = d.IsAcidDamage() ? "ACID" : "non-acid";
                int ar = ParentEntity != null
                    ? ParentEntity.GetStatValue("AcidResistance", 0)
                    : 0;
                string attrs = d.Attributes != null && d.Attributes.Count > 0
                    ? string.Join(",", d.Attributes)
                    : "(none)";

                MessageLog.Add(
                    $"[AcidDemo] {label} incoming: amount={d.Amount} " +
                    $"{elementFlag} AR={ar} attrs=[{attrs}]");
            }
            return true;
        }
    }
}
