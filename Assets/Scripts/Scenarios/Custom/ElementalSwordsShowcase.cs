using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Elemental Swords Showcase — the full Phase C × Phase E matrix in one
    /// scenario. Combines FlamingSword (Fire) and IceSword (Cold) and stages
    /// three creatures that, together, expose all four meaningful weapon ×
    /// resistance interactions:
    ///
    ///                   [Snapjaw NE: CR=25]
    ///                   ↗  ↗
    ///   [Player] →→→→→ [Glowmaw E: HR=50, CR=0]
    ///                   ↘  ↘
    ///                   [SnapjawHunter SE: CR=50]
    ///
    /// Interactions to test:
    ///
    ///   1. FlamingSword vs Glowmaw      → HALVED (Fire × HR=50)
    ///   2. IceSword     vs Glowmaw      → full   (Glowmaw has no CR; control for Ice)
    ///   3. IceSword     vs Snapjaw      → 25%-reduced (Cold × CR=25 — graded resistance)
    ///   4. IceSword     vs SnapjawHunter → HALVED (Cold × CR=50)
    ///
    /// Plus the implicit complement: FlamingSword swung at Snapjaw or
    /// SnapjawHunter is unaffected by their CR (HR=0 on Snapjaws). So you
    /// can also confirm "Fire damage on a CR-only creature lands fully" by
    /// swinging FlamingSword northeast or southeast.
    ///
    /// Player loadout: HP=200/200, Strength=24, FlamingSword equipped,
    /// IceSword + 5 HealingTonics in inventory. All three creatures have
    /// HP padded to 200 so a generous comparison session is possible.
    ///
    /// What the player should observe in the message log: each hit fires
    /// an [ElementalDemo] line carrying the target name, the pre-resistance
    /// damage amount, FIRE/ICE/non-elemental flag, BOTH HeatResistance and
    /// ColdResistance (live read), and the full attribute list. Together
    /// with the in-game HP delta, the player sees per-hit:
    ///
    ///   - which elemental tag is on the damage object
    ///   - which of the two resistance stats are present on the target
    ///   - which (if any) is the lever firing on this particular hit
    ///
    /// </summary>
    [Scenario(
        name: "Elemental Swords Showcase",
        category: "Combat",
        description: "Phase C × Phase E full matrix: FlamingSword + IceSword vs Glowmaw, Snapjaw, and SnapjawHunter. All four weapon×resistance combinations visible side-by-side.")]
    public class ElementalSwordsShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: both swords + survival kit ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("FlamingSword")
                .GiveItem("IceSword", 1)
                .GiveItem("HealingTonic", 5);

            // === E: Glowmaw — HR=50, no CR ===
            // Lit-test for Fire×HR (halved) and the control for Ice (full,
            // since Glowmaw has no ColdResistance stat).
            var glowmaw = ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (glowmaw != null)
                glowmaw.AddPart(new ElementalDemoProbePart());

            // === NE: Snapjaw — CR=25, no HR ===
            // Graded resistance: 25% reduction on Ice damage. The "the
            // formula isn't binary" demonstration. Also: full damage on
            // FlamingSword (no HR).
            var snapjaw = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (snapjaw != null)
                snapjaw.AddPart(new ElementalDemoProbePart());

            // === SE: SnapjawHunter — CR=50, no HR ===
            // The thematic Ice pairing: half-absorbs Ice. Symmetric to
            // FlamingSword vs Glowmaw on the other side.
            var hunter = ctx.Spawn("SnapjawHunter")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (hunter != null)
                hunter.AddPart(new ElementalDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== Elemental Swords Showcase (Phase C × Phase E) ===");
            ctx.Log("Loadout: FlamingSword equipped, IceSword in inventory.");
            ctx.Log("E  Glowmaw       (HR=50, CR=0):  swing FlamingSword → HALVED. IceSword → full.");
            ctx.Log("NE Snapjaw       (HR=0,  CR=25): swing IceSword → 25%-reduced.");
            ctx.Log("SE SnapjawHunter (HR=0,  CR=50): swing IceSword → HALVED.");
            ctx.Log("Watch for [ElementalDemo] log lines on each hit — they show");
            ctx.Log("the damage's attribute list and BOTH resistance stats live.");
        }
    }

    /// <summary>
    /// Showcase-only Part that hooks <c>BeforeTakeDamage</c> and logs every
    /// incoming damage instance with its full attribute list, the target's
    /// HeatResistance, AND the target's ColdResistance. Companion to
    /// <see cref="FlamingSwordDemoProbePart"/> — extended to surface both
    /// elemental resistance stats so the player can read which lever fires
    /// for any given weapon×target combination.
    ///
    /// Format:
    ///   [ElementalDemo] {target} incoming: amount=N {ELEMENT_FLAG} HR=H CR=C attrs=[...]
    ///
    /// Where ELEMENT_FLAG is FIRE, ICE, or non-elemental — read off the
    /// damage's Attribute list (matching <c>Damage.IsHeatDamage()</c> and
    /// <c>Damage.IsColdDamage()</c>'s actual contracts: Cold|Ice|Freeze
    /// counts as Cold; Fire|Heat counts as Heat).
    ///
    /// Scenario-only Part. Production combat does not emit these lines.
    /// </summary>
    public class ElementalDemoProbePart : Part
    {
        public override string Name => "ElementalDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                string label = ParentEntity?.GetDisplayName() ?? "?";

                string elementFlag = "non-elemental";
                if (d.IsHeatDamage()) elementFlag = "FIRE";
                else if (d.IsColdDamage()) elementFlag = "ICE";

                int hr = ParentEntity != null
                    ? ParentEntity.GetStatValue("HeatResistance", 0)
                    : 0;
                int cr = ParentEntity != null
                    ? ParentEntity.GetStatValue("ColdResistance", 0)
                    : 0;

                string attrs = d.Attributes != null && d.Attributes.Count > 0
                    ? string.Join(",", d.Attributes)
                    : "(none)";

                MessageLog.Add(
                    $"[ElementalDemo] {label} incoming: amount={d.Amount} " +
                    $"{elementFlag} HR={hr} CR={cr} attrs=[{attrs}]");
            }
            return true;
        }
    }
}
