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
        description: "Skill-tree popup demo. 5 trees / 9 powers in registry. WSP.1 tree-root crit behaviors active + WSP8.2 actives wired (Lunge / Whirlwind / Flurry / Tumble). Inventory: one of every weapon type (4 base + 5 elemental).")]
    public class SkillTreeShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // === Player loadout: one of every weapon type in the game ===
            // Base weapons (one per class) cover the tree-root crit
            // behaviors + class-gated active abilities. The 5 elemental
            // variants extend the base lineup with Phase-E elemental
            // routing (Acid → AcidResistance) and the on-hit-effect
            // status emissions (Burning / Frozen / Electrified / Acidic).
            // After this ship, the player can rotate through any combo
            // of weapon class × elemental flavor without switching
            // scenarios.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 18)
                .SetStat("Agility", 14)
                .SetStat("SP", 200)
                // Base weapons — one per class.
                .Equip("Mace")                  // Bludgeoning + Cudgel (equipped)
                .GiveItem("Battleaxe", 1)       // Cutting + Axe
                .GiveItem("LongSword", 1)       // Cutting + LongBlades
                .GiveItem("Dagger", 1)          // Piercing + ShortBlades
                // Elemental variants — distinct on-hit + class crit.
                .GiveItem("FlamingSword", 1)    // Cutting + LongBlades + Fire
                .GiveItem("IceSword", 1)        // Cutting + LongBlades + Cold
                .GiveItem("ThunderHammer", 1)   // Bludgeoning + Cudgel + Lightning
                .GiveItem("AcidicDagger", 1)    // Piercing + ShortBlades + Acid
                .GiveItem("DissolutionMaul", 1);// Bludgeoning + Cudgel + Acid

            // === Pre-buy skills so every active is testable on swap ===
            // 4 weapon-class tree-roots (WSP.1 crit behaviors) +
            // Cudgel_Bludgeon (the canonical "stacked Stun" demo) +
            // the 4 WSP8.2 actives (Lunge / Whirlwind / Flurry / Tumble).
            // Active hotbar slots populate as the abilities are added,
            // so the player can press 1-9 to fire each on the right
            // weapon (gating still applies — Lunge needs LongBlades,
            // Whirlwind needs Axe, etc.).
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                skills.AddSkill("AcrobaticsSkill",       source: "scenario:prebuy");
                skills.AddSkill("CudgelSkill",           source: "scenario:prebuy");
                skills.AddSkill("AxeSkill",              source: "scenario:prebuy");
                skills.AddSkill("LongBladesSkill",       source: "scenario:prebuy");
                skills.AddSkill("ShortBladesSkill",      source: "scenario:prebuy");
                skills.AddSkill("Cudgel_Bludgeon",       source: "scenario:prebuy");
                // WSP8.2 — pre-buy the 4 new actives so the player can
                // immediately test each on its weapon class.
                skills.AddSkill("LongBlades_Lunge",      source: "scenario:prebuy");
                skills.AddSkill("Axe_Whirlwind",         source: "scenario:prebuy");
                skills.AddSkill("ShortBlades_Flurry",    source: "scenario:prebuy");
                skills.AddSkill("Acrobatics_Tumble",     source: "scenario:prebuy");
            }

            // === Walk-through ===
            ctx.Log("=== Skill Tree Showcase (WSP8.2) ===");
            ctx.Log("Press X to open the skills screen — 5 trees / 9 powers visible.");
            ctx.Log("Press M to open the ability manager (10 pre-bought).");
            ctx.Log("");
            ctx.Log("Inventory: 9 weapons covering every type.");
            ctx.Log("  Cudgel:     Mace [equipped], ThunderHammer (Lightning), DissolutionMaul (Acid)");
            ctx.Log("  Axe:        Battleaxe");
            ctx.Log("  LongBlades: LongSword, FlamingSword (Fire), IceSword (Cold)");
            ctx.Log("  ShortBlades: Dagger, AcidicDagger (Acid)");
            ctx.Log("");
            ctx.Log("Tree-root crit behaviors (WSP.1) — every weapon's crit fires its class proc.");
            ctx.Log("Per-power on-hit (every hit, gated by chance):");
            ctx.Log("  Cudgel_Bludgeon (owned)        → 50% Stunned on Cudgel hit");
            ctx.Log("  Axe_Cleave (1 SP)              → 30% half-damage cleave on Axe hit");
            ctx.Log("  LongBlades_Lacerate (1 SP)     → 35% Bleeding 1d3 on LongBlades hit");
            ctx.Log("  ShortBlades_Jab (1 SP)         → 30% Confused 3T on Piercing hit");
            ctx.Log("  ShortBlades_Bloodletter (1 SP) → 50% Bleeding 1d2 on Piercing hit");
            ctx.Log("");
            ctx.Log("Active abilities (press M to see / 1-9 to fire):");
            ctx.Log("  Lunge      (LongBlades)  — DirectionLine, range 2 reach extension");
            ctx.Log("  Whirlwind  (Axe)         — SelfCentered, hit ALL 8 adjacent");
            ctx.Log("  Flurry     (Piercing)    — AdjacentCell, 3 strikes on one target");
            ctx.Log("  Tumble     (Acrobatics)  — AdjacentCell swap (no weapon req)");
            ctx.Log("");
            ctx.Log("Suggested experiment: equip FlamingSword, hit a target. Watch for");
            ctx.Log("  - LongBlades crit (Bleeding 1d4) + Cutting class crit hook");
            ctx.Log("  - Per-weapon Burning on-hit (30% chance via OnHitWeaponEffects)");
            ctx.Log("  - Lacerate 35% if you bought it (LongBlades-class hit)");
        }
    }
}
