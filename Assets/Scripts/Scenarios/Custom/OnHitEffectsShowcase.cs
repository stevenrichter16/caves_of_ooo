using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// On-Hit Effects Showcase — exposes the Tier-2 class-based + per-weapon
    /// hook system end-to-end. Player gets 5 weapons in inventory and faces
    /// 4 padded Snapjaws (one per "lane"). Each Snapjaw carries an
    /// <see cref="OnHitEventProbePart"/> that announces every status-effect
    /// application, so the player can read which class hook fired and which
    /// per-weapon hook fired per swing.
    ///
    ///   [Snapjaw NW: receive Bludgeoning hits → watch for Stunned]
    ///   [Snapjaw N : receive Cutting hits     → watch for Bleeding]
    ///   [Snapjaw NE: receive Piercing hits    → watch for Confused]
    ///   [Snapjaw S : receive elemental swings → watch for Burning/Frozen/Electrified/Acidic]
    ///
    /// Inventory:
    ///   Mace            (Bludgeoning, base — class Stun chance only)
    ///   LongSword       (Cutting,     base — class Bleed chance only)
    ///   Dagger          (Piercing,    base — class Confuse chance only)
    ///   FlamingSword    (Cutting+Fire — class Bleed + per-weapon Burning)
    ///   ThunderHammer   (Bludgeoning+Lightning — class Stun + per-weapon Electrified)
    ///
    /// Probabilities are LOW (10-25% per class hook, 30% per-weapon) — expect
    /// to swing each weapon ~5-10 times before observing each effect.
    ///
    /// All four targets have HP padded to 200 so a generous comparison
    /// session is possible.
    /// </summary>
    [Scenario(
        name: "On-Hit Effects Showcase",
        category: "Combat",
        description: "Tier-2 on-hit hooks: 5 weapons × 4 targets reveal class-based (Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse) + per-weapon elemental effects (Burning, Frozen, Electrified, Acidic).")]
    public class OnHitEffectsShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: one weapon per class + 2 elemental ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .Equip("Mace")
                .GiveItem("LongSword", 1)
                .GiveItem("Dagger", 1)
                .GiveItem("FlamingSword", 1)
                .GiveItem("ThunderHammer", 1)
                .GiveItem("HealingTonic", 5);

            // 4 padded Snapjaws spread out so the player can pick targets
            SpawnTarget(ctx, p.x + 3, p.y - 2, "[Bludgeoning lane: swing Mace or ThunderHammer]");
            SpawnTarget(ctx, p.x + 3, p.y - 1, "[Cutting lane: swing LongSword or FlamingSword]");
            SpawnTarget(ctx, p.x + 3, p.y + 1, "[Piercing lane: swing Dagger]");
            SpawnTarget(ctx, p.x + 3, p.y + 2, "[Elemental lane: swing FlamingSword or ThunderHammer]");

            // === Walk-through log ===
            ctx.Log("=== On-Hit Effects Showcase (Tier-2 class + per-weapon hooks) ===");
            ctx.Log("Loadout: Mace equipped + LongSword, Dagger, FlamingSword, ThunderHammer in inventory.");
            ctx.Log("Class hooks (universal): Bludgeoning → 15% Stunned. Cutting → 25% Bleeding.");
            ctx.Log("Class hooks (universal): Piercing → 10% Confused.");
            ctx.Log("Per-weapon: FlamingSword 30% Burning. ThunderHammer 30% Electrified.");
            ctx.Log("Watch for [OnHitDemo] log lines — they fire when an effect is applied.");
            ctx.Log("Probabilities are low; swing each weapon ~5-10 times to see each trigger.");
        }

        private static void SpawnTarget(ScenarioContext ctx, int x, int y, string laneNote)
        {
            var t = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 200)
                .WithHpAbsolute(200)
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(x, y);
            if (t != null)
                t.AddPart(new OnHitEventProbePart());
            ctx.Log("Spawn @ (" + x + "," + y + "): " + laneNote);
        }
    }

    /// <summary>
    /// Showcase-only Part. Listens to <c>EffectApplied</c> and
    /// <c>EffectRemoved</c> events on the host entity and announces
    /// each via <see cref="MessageLog"/>, so the player can read which
    /// on-hit hook fired and which removal cause ended each effect.
    ///
    /// Apply format:
    ///   [OnHitDemo] {target} acquired Stunned
    ///   [OnHitDemo] {target} acquired Bleeding
    ///
    /// Remove format (with inferred cause label):
    ///   [OnHitDemo] {target} lost Bleeding (save succeeded)
    ///   [OnHitDemo] {target} lost Stunned (duration expired)
    ///
    /// **Cause inference**: <see cref="StatusEffectsPart"/>'s
    /// <c>EffectRemoved</c> event doesn't carry a "cause" parameter, so
    /// the probe uses a type-based heuristic: <see cref="BleedingEffect"/>
    /// is the only effect in the codebase with `Duration =
    /// DURATION_INDEFINITE` + a save-based recovery (Toughness vs
    /// SaveTarget in <c>OnTurnEnd</c>). For the showcase scenario no
    /// external code removes Bleeding, so a removed BleedingEffect is
    /// always due to a successful save in practice. Other effects
    /// (Stunned, Confused) end via duration tick.
    ///
    /// Scenario-only Part. Production combat does not emit these lines.
    /// </summary>
    public class OnHitEventProbePart : Part
    {
        public override string Name => "OnHitEventProbe";

        public override bool HandleEvent(GameEvent e)
        {
            string label = ParentEntity?.GetDisplayName() ?? "?";

            if (e.ID == "EffectApplied" && e.GetParameter("Effect") is Effect appliedEffect)
            {
                MessageLog.Add("[OnHitDemo] " + label + " acquired " + appliedEffect.DisplayName);
            }
            else if (e.ID == "EffectRemoved" && e.GetParameter("Effect") is Effect removedEffect)
            {
                // BleedingEffect ends only via save in normal play
                // (DURATION_INDEFINITE + Toughness save in OnTurnEnd).
                // Everything else ends via duration tick.
                string cause = removedEffect is BleedingEffect
                    ? "save succeeded"
                    : "duration expired";
                MessageLog.Add("[OnHitDemo] " + label + " lost " + removedEffect.DisplayName + " (" + cause + ")");
            }
            return true;
        }
    }
}
