using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Item Enhancements E.4 — content showcase exercising all of
    /// E.1 + E.2 + E.3 in one playtest-ready scene. Stages the full
    /// mineral economy + enhancement substrate so a human can walk
    /// through each part of the loop.
    ///
    /// <para><b>Setup (player at center; offsets relative):</b></para>
    /// <pre>
    ///                   [SporeShambler NW: Fungal — Choir-Iron BONUS]
    ///                   [SkeletalSentry N : Undead — Pale-Salt BONUS]
    ///                   [Snapjaw NE       : neutral — NO bonus (control)]
    ///   [Tinker W]   ← Player →   [3 targets row above]
    ///   [Scribe SW: PaleCuration trade partner]
    /// </pre>
    ///
    /// <para><b>Player loadout:</b></para>
    /// <list type="bullet">
    ///   <item>HP 250 max, Strength 24, equipped LongSword (Cutting Strength)</item>
    ///   <item>1 PaleSalt + 1 ChoirIron + 1 GlowQuartz in inventory (for Tinker recipes)</item>
    ///   <item>2 extra PaleSalt + 1 extra ChoirIron in inventory (for Scribe trade)</item>
    ///   <item>Spare Mace (for testing GlowQuartz on a Bludgeoning weapon)</item>
    ///   <item>Spare LeatherArmor (for testing GlowQuartz on armor — non-melee equippable)</item>
    ///   <item>BitLocker pre-stocked with BBBBBCCCCC bits (5 of each — enough for all 3 recipes
    ///         twice over with comfort margin)</item>
    ///   <item>Tinker recipes pre-learned: mod_palesalt_infuse, mod_choiriron_infuse,
    ///         mod_glowquartz_infuse, mod_sharp_melee (control)</item>
    ///   <item>5 HealingTonics for counter-attack survival</item>
    /// </list>
    ///
    /// <para><b>Walkthrough — what the player observes:</b></para>
    /// <list type="number">
    ///   <item>Swing baseline LongSword at SkeletalSentry → [E4Demo] log shows
    ///         "incoming: amount=X attrs=[...] hasUndead=true hasFungal=false"
    ///         exactly ONCE per swing. HP drops by ~X.</item>
    ///   <item>Walk to Tinker, apply Pale-Salt to LongSword via the "Infuse with
    ///         Pale-Salt" recipe. Inventory loses 1 PaleSalt + 1B + 1C bits.
    ///         Item name decorated with "pale-salt-edged".</item>
    ///   <item>Swing the now-edged LongSword at SkeletalSentry → [E4Demo] log
    ///         shows TWO incoming damage instances per swing: primary + a
    ///         smaller secondary "+bonus damage" (4 for Tier-2 PaleSalt). HP
    ///         drops by primary+bonus.</item>
    ///   <item>Swing same edged LongSword at Snapjaw (control) → ONE incoming
    ///         per swing. No bonus fires (Snapjaw lacks Undead tag).</item>
    ///   <item>Walk back to Tinker, apply Choir-Iron — second enhancement
    ///         attaches (slot-cap=2 now FULL).</item>
    ///   <item>Try to apply a THIRD enhancement → "Item already has the maximum
    ///         number of enhancements." rejection (no bits / no mineral
    ///         consumed).</item>
    ///   <item>Apply GlowQuartz to spare Mace via Tinker. Equip Mace → light
    ///         radius around player visibly grows.</item>
    ///   <item>Walk to PaleCuration Scribe SW. Trade 1 PaleSalt → log shows
    ///         "Your reputation with PaleCuration improves." Trade ChoirIron →
    ///         second improvement. Trade GlowQuartz → "not_wanted" rejection
    ///         (Scribe only wants Pale-Salt + Choir-Iron).</item>
    /// </list>
    ///
    /// <para>All observability flows through the
    /// <see cref="ItemEnhancementDemoProbePart"/> hook on
    /// BeforeTakeDamage (per-hit log lines) + the existing
    /// PlayerReputation.Modify MessageLog entries (rep changes) + the
    /// existing Tinker apply MessageLog entries (recipe success).</para>
    /// </summary>
    [Scenario(
        name: "Item Enhancement Showcase",
        category: "Items",
        description: "Phase E (E.1–E.3) showcase: apply mineral enhancements via Tinker, " +
                     "watch Pale-Salt/Choir-Iron bonus damage fire on tag-matched defenders, " +
                     "see GlowQuartz extend equipped-light radius, trade minerals to a PaleCuration " +
                     "NPC for faction rep. Slot-cap (2) enforcement demonstrable.")]
    public class ItemEnhancementShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout ===
            ctx.Player
                .SetStatMax("Hitpoints", 250)
                .SetHp(250)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("LongSword")
                // Minerals — 1 of each for the 3 enhancement applications,
                // plus extras for the Scribe trade demo.
                .GiveItem("PaleSalt", 3)
                .GiveItem("ChoirIron", 2)
                .GiveItem("GlowQuartz", 2)
                // Spare gear so the showcase can demonstrate multiple
                // application paths without re-running.
                .GiveItem("Mace", 1)
                .GiveItem("LeatherArmor", 1)
                .GiveItem("HealingTonic", 5);

            // BitLocker + recipe learning — pre-stock so the Tinker UI
            // works first-attempt. Five of each bit covers all three
            // mineral recipes plus the Sharp control with margin.
            var locker = ctx.PlayerEntity.GetPart<BitLockerPart>();
            if (locker != null)
            {
                locker.AddBits("BBBBBCCCCC");
                locker.LearnRecipe("mod_palesalt_infuse");
                locker.LearnRecipe("mod_choiriron_infuse");
                locker.LearnRecipe("mod_glowquartz_infuse");
                locker.LearnRecipe("mod_sharp_melee");
            }

            // === Targets row (north of player) ===
            // SporeShambler NW: Fungal-tagged. ChoirIron-infused weapons
            // deal +Tier*2 bonus damage here.
            var fungalTarget = ctx.Spawn("SporeShambler")
                .WithStatMax("Hitpoints", 150)
                .WithHpAbsolute(150)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x - 2, p.y - 3);
            if (fungalTarget != null)
                fungalTarget.AddPart(new ItemEnhancementDemoProbePart());

            // SkeletalSentry N: Undead-tagged. PaleSalt-edged weapons
            // deal +Tier*2 bonus damage here.
            var undeadTarget = ctx.Spawn("SkeletalSentry")
                .WithStatMax("Hitpoints", 150)
                .WithHpAbsolute(150)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x, p.y - 3);
            if (undeadTarget != null)
                undeadTarget.AddPart(new ItemEnhancementDemoProbePart());

            // Snapjaw NE: control — neither Undead nor Fungal. A weapon
            // with PaleSalt + ChoirIron deals NO bonus here. Pin: the
            // tag-match gate works.
            var controlTarget = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 150)
                .WithHpAbsolute(150)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 2, p.y - 3);
            if (controlTarget != null)
                controlTarget.AddPart(new ItemEnhancementDemoProbePart());

            // === Tinker NPC W: applies mineral enhancements ===
            // Player walks here and uses the Tinker conversation flow
            // to apply mod_palesalt_infuse / mod_choiriron_infuse /
            // mod_glowquartz_infuse to inventory items.
            ctx.Spawn("Tinker")
                .Passive()
                .At(p.x - 4, p.y);

            // === PaleCuration Scribe SW: mineral-trade partner ===
            // Inline-built NPC (no PaleCuration blueprint in Objects.json
            // yet — E.5+ content). WantsMineralPart configured to accept
            // PaleSalt + ChoirIron at +15 rep each. Snapjaw glyph as a
            // visual placeholder.
            var scribe = ctx.Spawn("Snapjaw")
                .Passive()
                .At(p.x - 2, p.y + 3);
            if (scribe != null)
            {
                // Override display name + faction so the scribe reads
                // as a PaleCuration NPC, not a Snapjaw.
                var render = scribe.GetPart<RenderPart>();
                if (render != null) render.DisplayName = "pale curation scribe";
                scribe.Tags["Faction"] = "PaleCuration";
                scribe.AddPart(new WantsMineralPart(
                    minerals: "PaleSalt,ChoirIron",
                    faction: "PaleCuration",
                    repReward: 15));
            }

            // === Walkthrough log ===
            ctx.Log("=== Item Enhancement Showcase (Phase E.1–E.3) ===");
            ctx.Log("Loadout: LongSword equipped + 3 minerals + spare Mace + spare LeatherArmor.");
            ctx.Log("");
            ctx.Log("Targets row (N): SporeShambler (Fungal), SkeletalSentry (Undead), Snapjaw (control).");
            ctx.Log("Tinker NPC W (4 tiles): applies mineral enhancements via conversation.");
            ctx.Log("Pale Curation Scribe SW: WantsMineralPart accepts Pale-Salt + Choir-Iron for +15 rep each.");
            ctx.Log("");
            ctx.Log("[E4Demo] log lines on each hit show: defender, incoming amount,");
            ctx.Log("  attribute list, and whether defender has Undead/Fungal MaterialTag.");
            ctx.Log("Tag-matched bonus damage = a SECOND [E4Demo] log line on the same swing.");
        }
    }

    /// <summary>
    /// Showcase-only Part that hooks <c>BeforeTakeDamage</c> on each
    /// target and logs incoming damage with the defender's material-tag
    /// state. Mirrors <see cref="FlamingSwordDemoProbePart"/>.
    ///
    /// <para>The bonus-damage from PaleSalt/ChoirIron flows through
    /// <c>CombatSystem.ApplyDamage</c> as a separate call, so the
    /// probe sees TWO BeforeTakeDamage events per swing on a
    /// tag-matched defender: one for the primary hit and one for the
    /// bonus. The bonus line is the gameplay-visible proof the
    /// enhancement fired.</para>
    ///
    /// <para>The probe also logs whether the defender has the
    /// <c>Undead</c> / <c>Fungal</c> MaterialTags — so the player can
    /// see why Pale-Salt fires on the Skeleton but not the Snapjaw.</para>
    ///
    /// <para>Production combat does NOT emit these lines; the probe
    /// is scenario-only.</para>
    /// </summary>
    public class ItemEnhancementDemoProbePart : Part
    {
        public override string Name => "ItemEnhancementDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                string label = ParentEntity?.GetDisplayName() ?? "?";
                var mat = ParentEntity?.GetPart<MaterialPart>();
                bool hasUndead = mat != null && mat.HasMaterialTag("Undead");
                bool hasFungal = mat != null && mat.HasMaterialTag("Fungal");
                string attrs = d.Attributes != null && d.Attributes.Count > 0
                    ? string.Join(",", d.Attributes)
                    : "(none)";

                MessageLog.Add(
                    $"[E4Demo] {label} incoming: amount={d.Amount} " +
                    $"attrs=[{attrs}] hasUndead={hasUndead} hasFungal={hasFungal}");
            }
            return true;
        }
    }
}
