using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// FlamingSword content showcase — make Phase C (damage attributes) ×
    /// Phase E (HeatResistance) player-visible.
    ///
    /// Setup (player at center; coordinates are player-relative):
    ///
    ///       . . [Snapjaw NE: HR=0 — Fire NOT reduced]
    ///       . . .
    ///   [Player]   [Glowmaw E: HR=50 — Fire HALVED]
    ///       . . .
    ///       . . [Glowmaw SE: HR=50, swing ShortSword to confirm non-fire NOT reduced]
    ///
    /// Player loadout:
    ///   - FlamingSword equipped (Cutting Fire LongBlades, 1d8)
    ///   - ShortSword in inventory (Cutting LongBlades, 1d6 — control weapon)
    ///   - Beefy stats (Str 24, HP fully restored, Hitpoints Max 200) so the
    ///     player can absorb counter-attacks long enough to make multiple
    ///     comparisons without dying or fluffing damage rolls.
    ///   - All three target creatures have HP padded to 200 so a half-dozen
    ///     swings each are observable before any death.
    ///
    /// What the player should observe in the message log:
    ///
    ///   --- Swing FlamingSword at Glowmaw E ---
    ///   [FlameDemo] glowmaw incoming: amount=8 FIRE HR=50 attrs=[Melee,Strength,Cutting,Fire,LongBlades]
    ///   (Glowmaw HP drops by ~4, not 8 — HeatResistance halved it)
    ///
    ///   --- Swing FlamingSword at Snapjaw NE ---
    ///   [FlameDemo] snapjaw incoming: amount=8 FIRE HR=0 attrs=[Melee,Strength,Cutting,Fire,LongBlades]
    ///   (Snapjaw HP drops by ~8 — no Heat resistance, full Fire damage)
    ///
    ///   --- Swap to ShortSword (inventory `i` to swap), swing at Glowmaw SE ---
    ///   [FlameDemo] glowmaw incoming: amount=6 non-fire HR=50 attrs=[Melee,Strength,Cutting,LongBlades]
    ///   (Glowmaw HP drops by ~6 — no Fire attribute, HeatResistance does NOT fire)
    ///
    /// The probe also logs the entity's own resistance value, so the
    /// connection between the elemental tag and the resistance is visible
    /// per-hit, not just by inference.
    /// </summary>
    [Scenario(
        name: "FlamingSword Showcase",
        category: "Combat",
        description: "Phase C × Phase E showcase: FlamingSword's Fire attribute halved by Glowmaw's HeatResistance (50), full damage on a non-resistant Snapjaw, and a non-Fire ShortSword unaffected by HeatResistance.")]
    public class FlamingSwordShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout ===
            // Beefy stats and a FlamingSword equipped. ShortSword as the
            // non-Fire control. Inventory has multiple HealingTonics so a
            // long comparison session doesn't end on a counter-attack.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("FlamingSword")
                .GiveItem("ShortSword", 1)
                .GiveItem("HealingTonic", 5);

            // === Glowmaw E: HeatResistance=50 — Fire HALVED ===
            // The "thematic" pairing. FlamingSword swings here drop HP by
            // half what they would on a non-resistant target.
            var glowmawHeatResist = ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y);
            if (glowmawHeatResist != null)
                glowmawHeatResist.AddPart(new FlamingSwordDemoProbePart());

            // === Snapjaw NE: no HeatResistance — Fire NOT reduced ===
            // The control for "does the Fire attribute matter at all on a
            // target without HeatResistance?" Answer: no — full damage.
            // Snapjaw blueprint declares ColdResistance (25), which is
            // unrelated and irrelevant to a Fire-tagged hit.
            var snapjawNoHeatResist = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y - 2);
            if (snapjawNoHeatResist != null)
                snapjawNoHeatResist.AddPart(new FlamingSwordDemoProbePart());

            // === Glowmaw SE: HeatResistance=50, but swing the ShortSword ===
            // The control for "does HeatResistance affect non-Fire damage?"
            // Answer: no — ShortSword's "Cutting LongBlades" carries no
            // elemental attribute, IsHeatDamage()=false, HeatResistance
            // never fires. Same Glowmaw, same HR, but the absence of Fire
            // means full damage lands.
            var glowmawNonFireControl = ctx.Spawn("Glowmaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 3, p.y + 2);
            if (glowmawNonFireControl != null)
                glowmawNonFireControl.AddPart(new FlamingSwordDemoProbePart());

            // === Walk-through log ===
            ctx.Log("=== FlamingSword Showcase (Phase C × Phase E) ===");
            ctx.Log("Loadout: FlamingSword equipped, ShortSword in inventory.");
            ctx.Log("E  (Glowmaw  HR=50): swing FlamingSword. Fire damage HALVED.");
            ctx.Log("NE (Snapjaw  HR=0):  swing FlamingSword. Fire damage NOT reduced.");
            ctx.Log("SE (Glowmaw  HR=50): swap to ShortSword. Non-Fire damage NOT reduced.");
            ctx.Log("Watch for [FlameDemo] log lines on each hit — they show the");
            ctx.Log("damage's attribute list and the target's HeatResistance live.");
        }
    }

    /// <summary>
    /// Showcase-only Part that hooks <c>BeforeTakeDamage</c> and logs every
    /// incoming damage instance to <see cref="MessageLog"/> with its full
    /// attribute list and the target's <c>HeatResistance</c>.
    ///
    /// The format is verbose on purpose — the goal is for a human player to
    /// see, per hit, that:
    ///   1. The Damage object's Attributes list contains "Fire" when
    ///      swinging the FlamingSword (and does NOT contain Fire when
    ///      swinging the ShortSword).
    ///   2. The target's HeatResistance stat is the one that gets read by
    ///      Phase E's resistance loop — not some other knob.
    ///   3. The combination of (1) + (2) determines whether the hit is
    ///      halved.
    ///
    /// This Part is <em>scenario-only</em> — production combat does not
    /// emit these log lines, by design (the player shouldn't see attribute
    /// lists in normal play). Future "elemental clarity UI" work could
    /// promote a softer version of this to production, but that's out of
    /// scope here.
    ///
    /// Note: <c>BeforeTakeDamage</c> fires BEFORE Phase E's
    /// <c>ApplyResistances</c>, so this probe sees the un-reduced amount.
    /// The reduction is observable as the per-hit HP delta on the target,
    /// which is what the player reads off the HUD.
    /// </summary>
    public class FlamingSwordDemoProbePart : Part
    {
        public override string Name => "FlamingSwordDemoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                string label = ParentEntity?.GetDisplayName() ?? "?";
                string fireFlag = d.HasAttribute("Fire") ? "FIRE" : "non-fire";
                int hr = ParentEntity != null
                    ? ParentEntity.GetStatValue("HeatResistance", 0)
                    : 0;
                string attrs = d.Attributes != null && d.Attributes.Count > 0
                    ? string.Join(",", d.Attributes)
                    : "(none)";

                MessageLog.Add(
                    $"[FlameDemo] {label} incoming: amount={d.Amount} " +
                    $"{fireFlag} HR={hr} attrs=[{attrs}]");
            }
            return true;
        }
    }
}
