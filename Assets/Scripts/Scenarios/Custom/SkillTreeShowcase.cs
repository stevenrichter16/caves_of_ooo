using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// ST.8 — Manual playtest scenario for the skill-tree popup (KeyCode.X).
    /// Sets up a controlled state where every per-row state from the
    /// <see cref="CavesOfOoo.Rendering.SkillsScreenRowState"/> enum can be
    /// observed in the live UI:
    ///
    /// <list type="bullet">
    /// <item><b>Owned</b>: Acrobatics tree-root pre-bought via SkillsPart.AddSkill.</item>
    /// <item><b>Buyable</b>: not directly visible at start (Dodge is gated by Agility),
    ///       but flips to Buyable once the player raises Agility (out of scope) or
    ///       a future power without an Agility gate is added.</item>
    /// <item><b>InsufficientSP</b>: not directly visible at start (player has 200 SP
    ///       which exceeds every cost in the registry); rebuying anything else
    ///       after spending will surface this.</item>
    /// <item><b>RequirementsNotMet</b>: Dodge power renders gray (Agility 14 &lt; 15).</item>
    /// </list>
    ///
    /// <para>Walk-through: press <c>X</c> to open. The popup shows
    /// "Acrobatics — owned" (white name + "owned" tag) and "Dodge — 50sp"
    /// (gray name + gray cost, indicating RequirementsNotMet). Description
    /// footer updates as the cursor moves.</para>
    ///
    /// <para>Honesty bound: with only Acrobatics + Dodge in the registry,
    /// this scenario can't simultaneously show all 4 row-states. A 2-tree
    /// content expansion would let one tree be Buyable, another Owned, a
    /// power InsufficientSP, and a different power RequirementsNotMet.
    /// Out of scope for ST.8 (content, not UI). Tracked as future work.</para>
    /// </summary>
    [Scenario(
        name: "Skill Tree Showcase",
        category: "UI",
        description: "ST.8 — KeyCode.X opens the skill-tree popup. Player has Acrobatics owned + low Agility so Dodge is RequirementsNotMet. Demonstrates the Owned + RequirementsNotMet row states live; Buyable / InsufficientSP states verified via state-builder unit tests.")]
    public class SkillTreeShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // === Player loadout: ample SP, low Agility ===
            // SP=200 ensures both Acrobatics (100) and Dodge (50) would be
            // affordable if their gates passed; isolates RequirementsNotMet
            // from InsufficientSP. Agility=14 puts the player one below the
            // Dodge minimum (15), surfacing the RequirementsNotMet path.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 18)
                .SetStat("Agility", 14)
                .SetStat("SP", 200);

            // === Pre-buy Acrobatics so the tree-root renders as Owned ===
            // PlayerBuilder doesn't expose AddSkill yet (out of scope for ST.8;
            // that infrastructure can land alongside future skill content).
            // Direct SkillsPart access is fine — same path BuySkillAction uses
            // internally, just bypasses the SP deduction since the showcase
            // wants the player at 200 SP for the Buyable / InsufficientSP demo.
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                skills.AddSkill("AcrobaticsSkill", source: "scenario:prebuy");
            }

            // === Walk-through ===
            ctx.Log("=== Skill Tree Showcase (ST.8) ===");
            ctx.Log("Press X to open the skills screen.");
            ctx.Log("Expect:");
            ctx.Log("  Acrobatics  — Owned   (white name, 'owned' tag)");
            ctx.Log("  Dodge       — RequirementsNotMet (gray name, gray cost)");
            ctx.Log("    reason: Agility 14 < Dodge minimum 15");
            ctx.Log("Use ^v / JK to navigate, Enter to attempt purchase, X/Esc to close.");
            ctx.Log("Try Enter on Dodge — message-log will surface 'You don't have the Agility for Dodge.'");
        }
    }
}
