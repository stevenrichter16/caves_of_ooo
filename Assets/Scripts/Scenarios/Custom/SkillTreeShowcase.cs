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
        description: "Skill-tree popup demo. 10 trees / 19 actives + passives. WSP.1 tree-root crit behaviors active + WSP8.2/8.3 actives wired (19 active abilities total). Inventory: one of every weapon type (4 base + 5 elemental).")]
    public class SkillTreeShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // === Player loadout: one of every weapon type in the game ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 18)
                .SetStat("Agility", 14)
                .SetStat("SP", 500) // Bumped to cover all 19 prebuys
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

            // === Pre-buy every shipped active so M (ability manager) ===
            // === shows the full 19-ability lineup on first open. ===
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                // Tree roots (5 trees represented; LongBlades's crit
                // behavior + Acrobatics's marker shape come for free).
                skills.AddSkill("AcrobaticsSkill",       source: "scenario:prebuy");
                skills.AddSkill("CudgelSkill",           source: "scenario:prebuy");
                skills.AddSkill("AxeSkill",              source: "scenario:prebuy");
                skills.AddSkill("LongBladesSkill",       source: "scenario:prebuy");
                skills.AddSkill("ShortBladesSkill",      source: "scenario:prebuy");
                // Existing passive (the canonical stacked-Stun demo).
                skills.AddSkill("Cudgel_Bludgeon",       source: "scenario:prebuy");

                // WSP8.2 actives — first 4 ports.
                skills.AddSkill("LongBlades_Lunge",      source: "scenario:prebuy");
                skills.AddSkill("Axe_Whirlwind",         source: "scenario:prebuy");
                skills.AddSkill("ShortBlades_Flurry",    source: "scenario:prebuy");
                skills.AddSkill("Acrobatics_Tumble",     source: "scenario:prebuy");

                // WSP8.3 actives — 15 more ports (all 10 trees represented).
                skills.AddSkill("Cudgel_ChargingStrike",   source: "scenario:prebuy");
                skills.AddSkill("Cudgel_GroundPound",      source: "scenario:prebuy");
                skills.AddSkill("Cudgel_Disarm",           source: "scenario:prebuy");
                skills.AddSkill("Axe_RendArmor",           source: "scenario:prebuy");
                skills.AddSkill("ShortBlades_Backstab",    source: "scenario:prebuy");
                skills.AddSkill("ShortBlades_Disengage",   source: "scenario:prebuy");
                skills.AddSkill("Acrobatics_EvasiveRoll",  source: "scenario:prebuy");
                skills.AddSkill("Acrobatics_Vault",        source: "scenario:prebuy");
                skills.AddSkill("Spellcraft_ArcaneSurge",  source: "scenario:prebuy");
                skills.AddSkill("Spellcraft_LeyTap",       source: "scenario:prebuy");
                skills.AddSkill("Pyromancy_Pyroclasm",     source: "scenario:prebuy");
                skills.AddSkill("Pyromancy_HeartFlame",    source: "scenario:prebuy");
                skills.AddSkill("Cryomancy_Frostbind",     source: "scenario:prebuy");
                skills.AddSkill("Cryomancy_Hibernate",     source: "scenario:prebuy");
                skills.AddSkill("Galvanism_Overload",      source: "scenario:prebuy");
            }

            // === Walk-through ===
            ctx.Log("=== Skill Tree Showcase (WSP8.3) ===");
            ctx.Log("Press X to open the skills screen.");
            ctx.Log("Press M to open the ability manager (19 actives pre-bought).");
            ctx.Log("");
            ctx.Log("Inventory: 9 weapons covering every type.");
            ctx.Log("  Cudgel:      Mace [equipped], ThunderHammer (Lightning), DissolutionMaul (Acid)");
            ctx.Log("  Axe:         Battleaxe");
            ctx.Log("  LongBlades:  LongSword, FlamingSword (Fire), IceSword (Cold)");
            ctx.Log("  ShortBlades: Dagger, AcidicDagger (Acid)");
            ctx.Log("");
            ctx.Log("Active abilities by tree (M to view, 1-9 to fire):");
            ctx.Log("  Cudgel:      Slam, Conk, ChargingStrike, GroundPound, Disarm");
            ctx.Log("  Axe:         Berserk, HookAndDrag, Whirlwind, RendArmor");
            ctx.Log("  LongBlades:  Lunge");
            ctx.Log("  ShortBlades: Shank, Flurry, Backstab, Disengage");
            ctx.Log("  Acrobatics:  Tumble, EvasiveRoll, Vault");
            ctx.Log("  Spellcraft:  ArcaneSurge, LeyTap");
            ctx.Log("  Pyromancy:   Pyroclasm, HeartFlame");
            ctx.Log("  Cryomancy:   Frostbind, Hibernate");
            ctx.Log("  Galvanism:   Overload");
            ctx.Log("");
            ctx.Log("Suggested experiments:");
            ctx.Log("  - FlamingSword + Lunge: range 2 swing, +30% Burning on-hit chance");
            ctx.Log("  - Battleaxe + Whirlwind: every 8-adjacent gets a strike");
            ctx.Log("  - AcidicDagger + Flurry: 3 strikes, each rolling Acidic on-hit");
            ctx.Log("  - LeyTap then any spell: HP drained + spell deals bonus damage");
            ctx.Log("  - Hibernate: 10T self-stasis with 5%/turn heal");
        }
    }
}
