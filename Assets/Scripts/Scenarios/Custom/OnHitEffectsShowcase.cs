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
    /// Remove format:
    ///   [OnHitDemo] {target} lost Bleeding (save succeeded)
    ///   [OnHitDemo] {target} lost Stunned (duration expired)
    ///   [OnHitDemo] {target} lost Confused (external)
    ///
    /// Cause is read from the <c>EffectRemoved</c> event's <c>Cause</c>
    /// parameter, which <see cref="StatusEffectsPart.SendRemoved"/>
    /// populates from <see cref="Effect.LastRemovalCause"/>. Effects
    /// with save-based recovery overwrite that field before setting
    /// <c>Duration = 0</c>; external <c>RemoveEffect</c> callers tag
    /// it as <see cref="Effect.CAUSE_EXTERNAL"/>; the default
    /// (duration tick) is <see cref="Effect.CAUSE_DURATION_EXPIRED"/>.
    /// Generic across all current and future Effect subclasses with
    /// save mechanics — no per-type heuristic.
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
                // GameEvent stores string params in a separate StringParameters
                // dict; GetParameter<T> only reads the typed Parameters dict and
                // misses them. Use GetStringParameter for the Cause field.
                string causeRaw = e.GetStringParameter("Cause", Effect.CAUSE_DURATION_EXPIRED);
                string causeLabel = HumanizeCause(causeRaw);
                MessageLog.Add("[OnHitDemo] " + label + " lost " + removedEffect.DisplayName + " (" + causeLabel + ")");
            }
            return true;
        }

        private static string HumanizeCause(string causeRaw)
        {
            // Map machine-readable cause strings to readable log labels.
            // Falls through to the raw string for unknown causes (forward-
            // compatible with future cause constants added to Effect).
            if (causeRaw == Effect.CAUSE_SAVE_SUCCEEDED) return "save succeeded";
            if (causeRaw == Effect.CAUSE_DURATION_EXPIRED) return "duration expired";
            if (causeRaw == Effect.CAUSE_EXTERNAL) return "external";
            return causeRaw;
        }
    }
}
