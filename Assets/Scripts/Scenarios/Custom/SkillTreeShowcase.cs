using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// ST.8 / WS.6 — Manual playtest scenario for the skill-tree popup
    /// (KeyCode.X). Demonstrates BOTH the original Acrobatics content AND
    /// the WS.1-5 weapon-class trees (Cudgel, Axe, Long Blades, Short Blades).
    ///
    /// <para>Loadout: player has 200 SP (every visible row is affordable),
    /// Agility 14 (so Dodge stays RequirementsNotMet — preserves the gray
    /// row in the popup for visual reference). Two skills are pre-bought
    /// (AcrobaticsSkill + Cudgel_Bludgeon) so the popup shows two Owned
    /// rows alongside the Buyable rows from the rest of the trees.</para>
    ///
    /// <para>Player inventory carries one weapon per class (Mace, Battleaxe,
    /// LongSword, Dagger). With Cudgel_Bludgeon already owned, equipping
    /// the Mace and swinging at any creature surfaces the WS.2 35% Stun
    /// proc on top of the universal Bludgeoning class hook. Buying the
    /// other weapon-class powers (Axe_Cleave / LongBlades_Lacerate /
    /// ShortBlades_Jab) for 1 SP each enables the same demo for axes /
    /// swords / daggers.</para>
    ///
    /// <para>Per-row states observable in the popup:</para>
    /// <list type="bullet">
    /// <item><b>Owned</b>: AcrobaticsSkill, Cudgel_Bludgeon (and the Cudgel tree-root if you buy it).</item>
    /// <item><b>Buyable</b>: every weapon tree-root + every power except Dodge.</item>
    /// <item><b>RequirementsNotMet</b>: Acrobatics's Dodge (Agility 14 &lt; 15).</item>
    /// <item><b>InsufficientSP</b>: drop SP below 1 (e.g. by buying enough)
    ///   to flip the remaining 1-SP entries.</item>
    /// </list>
    /// </summary>
    [Scenario(
        name: "Skill Tree Showcase",
        category: "UI",
        description: "ST.8 / WS.6 — KeyCode.X opens the skill-tree popup. Demos all 5 trees (Acrobatics + 4 weapon classes) and 2 pre-bought skills (Acrobatics + Cudgel_Bludgeon). Player has Mace/Battleaxe/LongSword/Dagger in inventory to exercise each weapon-class on-hit power.")]
    public class SkillTreeShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // === Player loadout: ample SP + each weapon class in inventory ===
            // Strength 18 = solid melee. Agility 14 keeps Dodge as the gray
            // RequirementsNotMet row in the popup for visual reference.
            // SP 200 means every 1-SP weapon skill is affordable; press
            // Enter on any to buy and watch it flip Buyable → Owned.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 18)
                .SetStat("Agility", 14)
                .SetStat("SP", 200)
                .Equip("Mace")            // Bludgeoning + Cudgel
                .GiveItem("Battleaxe", 1) // Cutting + Axe
                .GiveItem("LongSword", 1) // Cutting + LongBlades
                .GiveItem("Dagger", 1);   // Piercing

            // === Pre-buy 2 skills so the popup shows multiple Owned rows ===
            // PlayerBuilder doesn't expose AddSkill; direct SkillsPart access
            // is the same path BuySkillAction uses internally. Bypasses the
            // SP deduction so we keep 200 SP for the Buyable demo.
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                skills.AddSkill("AcrobaticsSkill", source: "scenario:prebuy");
                skills.AddSkill("Cudgel_Bludgeon",  source: "scenario:prebuy");
            }

            // === Walk-through ===
            ctx.Log("=== Skill Tree Showcase (WS.6) ===");
            ctx.Log("Press X to open the skills screen.");
            ctx.Log("Expect 5 trees: Acrobatics + Cudgel + Axe + Long Blades + Short Blades.");
            ctx.Log("Pre-bought: Acrobatics (Owned) + Cudgel_Bludgeon (Owned).");
            ctx.Log("Buyable for 1 SP each: every weapon tree-root + Cleave / Lacerate / Jab.");
            ctx.Log("Inventory: Mace (equipped), Battleaxe, LongSword, Dagger.");
            ctx.Log("");
            ctx.Log("Try this:");
            ctx.Log("  1. Open the popup, buy Axe_Cleave + LongBlades_Lacerate + ShortBlades_Jab (3 SP).");
            ctx.Log("  2. Close the popup. Spawn a target (or walk to one), swing the Mace several times.");
            ctx.Log("  3. Watch the message log for 'X is stunned' applications (Bludgeoning class hook +");
            ctx.Log("     Cudgel_Bludgeon skill hook each roll independently — duration stacks).");
            ctx.Log("  4. Equip a different weapon, swing again, observe the matching skill effect.");
        }
    }
}
