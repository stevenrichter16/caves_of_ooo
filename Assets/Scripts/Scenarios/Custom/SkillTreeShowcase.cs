using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Manual playtest scenario for the skill-tree popup (KeyCode.X).
    /// Updated through ST.8 / WS.6 / WSP.4. Demonstrates:
    /// <list type="bullet">
    /// <item>5 trees in the registry (Acrobatics + Cudgel + Axe + Long Blades + Short Blades)</item>
    /// <item>4 tree-root crit behaviors (WSP.1) — every natural-20 with a
    ///   matching weapon class fires the tree-root's per-class effect</item>
    /// <item>5 powers — Dodge, Cudgel_Bludgeon, Axe_Cleave,
    ///   LongBlades_Lacerate, ShortBlades_Jab, ShortBlades_Bloodletter</item>
    /// <item>The 4 row-states (Owned / Buyable / RequirementsNotMet / InsufficientSP)</item>
    /// </list>
    ///
    /// <para>Loadout: 200 SP (every 1-SP row is affordable), Strength 18,
    /// Agility 14 (so Acrobatics's Dodge stays RequirementsNotMet — keeps
    /// the gray row visible for state-builder reference). Pre-bought
    /// skills include all 4 weapon-class tree-roots + Cudgel_Bludgeon, so
    /// the player can equip the Mace and immediately observe Cudgel's
    /// crit-Stun + Cudgel_Bludgeon's gated-Stun + universal Bludgeoning
    /// class-hook Stun all stacking on a single critical hit.</para>
    /// </summary>
    [Scenario(
        name: "Skill Tree Showcase",
        category: "UI",
        description: "Skill-tree popup demo. 5 trees / 5 powers in registry. WSP.1 tree-root crit behaviors active. Pre-bought: Acrobatics + 4 weapon-class tree-roots + Cudgel_Bludgeon. Inventory: Mace/Battleaxe/LongSword/Dagger.")]
    public class SkillTreeShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // === Player loadout: ample SP + each weapon class in inventory ===
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

            // === Pre-buy 5 skills so multiple Owned rows surface immediately ===
            // All 4 weapon-class tree-roots get pre-bought so WSP.1's crit
            // behaviors are active for every weapon. Cudgel_Bludgeon gets
            // pre-bought too so the player sees a gated 50% stun + the
            // universal class-hook stun + the crit-only tree-root stun all
            // stacking on a Mace crit (StunnedEffect.OnStack sums durations).
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                skills.AddSkill("AcrobaticsSkill",  source: "scenario:prebuy");
                skills.AddSkill("CudgelSkill",      source: "scenario:prebuy");
                skills.AddSkill("AxeSkill",         source: "scenario:prebuy");
                skills.AddSkill("LongBladesSkill",  source: "scenario:prebuy");
                skills.AddSkill("ShortBladesSkill", source: "scenario:prebuy");
                skills.AddSkill("Cudgel_Bludgeon",  source: "scenario:prebuy");
            }

            // === Walk-through ===
            ctx.Log("=== Skill Tree Showcase (WSP.4) ===");
            ctx.Log("Press X to open the skills screen.");
            ctx.Log("Pre-bought: Acrobatics + 4 weapon tree-roots + Cudgel_Bludgeon (6 SP spent).");
            ctx.Log("Remaining 1-SP buys: Dodge (gray — Agility 14 < 15), Cleave, Lacerate, Jab, Bloodletter.");
            ctx.Log("");
            ctx.Log("Tree-root crit behaviors (WSP.1) are ACTIVE on every weapon:");
            ctx.Log("  Mace crit (Cudgel)        → Stunned 1-4T (random)");
            ctx.Log("  Battleaxe crit (Axe)      → force cleave to adjacent enemy at half damage");
            ctx.Log("  LongSword crit (LongBlades) → Bleeding 1d4 (no save)");
            ctx.Log("  Dagger crit (Piercing)    → Bleeding 1d2");
            ctx.Log("");
            ctx.Log("Per-power on-hit (every hit, gated by chance):");
            ctx.Log("  Cudgel_Bludgeon (owned)   → 50% Stunned 1-4T on Cudgel hit");
            ctx.Log("  Axe_Cleave (1 SP)         → 30% half-damage cleave on Axe hit");
            ctx.Log("  LongBlades_Lacerate (1 SP)→ 35% Bleeding 1d3 on LongBlades hit");
            ctx.Log("  ShortBlades_Jab (1 SP)    → 30% Confused 3T on Piercing hit");
            ctx.Log("  ShortBlades_Bloodletter (1 SP) → 50% Bleeding 1d2 on Piercing hit");
            ctx.Log("");
            ctx.Log("Try: equip Mace, swing at a target until you crit. Watch the");
            ctx.Log("message log for stacked Stuns (15% class + 50% Bludgeon + 100% crit).");
        }
    }
}
