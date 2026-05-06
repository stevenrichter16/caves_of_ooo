using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class on-hit Dismember passive. Per Qud's
    /// <c>Axe_Dismember.cs:280-318</c> — on every Axe-attribute hit that
    /// lands damage, a small chance to force-dismember a random severable
    /// body part on the defender. Dismembered targets bleed.
    ///
    /// <para><b>Mechanic (CoO):</b> on every <see cref="OnAttackerAfterAttack"/>
    /// where the damage carries the Axe attribute and
    /// <see cref="SkillEventContext.ActualDamage"/> &gt; 0, roll
    /// <see cref="CHANCE_PERCENT"/>%. On success, collect every body part
    /// for which <see cref="BodyPart.IsSeverable"/> returns true (skipping
    /// Mortal parts so a single proc can't instakill via head-removal —
    /// that's <c>Axe_Decapitate</c>'s job, deferred for now). Pick one at
    /// random via <see cref="SkillEventContext.Rng"/>, call
    /// <see cref="Body.Dismember"/>, and apply
    /// <see cref="BleedingEffect"/> with save target
    /// <see cref="BLEED_SAVE_TARGET"/> and dice
    /// <see cref="BLEED_DAMAGE_DICE"/> (Qud parity values 35 and "1d2").</para>
    ///
    /// <para><b>Scope divergence (v1):</b> Qud's <c>Axe_Dismember</c>
    /// also ships an active ability ("Dismember" command) that swings
    /// at an adjacent target with force-dismember on hit. That active
    /// version is deferred — this ship covers the passive only, which
    /// is the higher-frequency and more-tested Qud branch (the active
    /// is essentially "Conk-but-with-dismember-instead-of-stun"). Also
    /// deferred: Qud's "6% chance for two-handed axes" branch — CoO
    /// doesn't model two-handed weapons distinctly yet. And Qud's
    /// Berserk-synergy "every axe hit while berserk force-dismembers"
    /// — would require checking <c>BerserkEffect</c> here, but the
    /// effect's gameplay impact in CoO is currently +Strength/-DV only,
    /// so the dismember interaction would over-mirror. Documented per
    /// CLAUDE.md §4.2 as Match (mechanic family) + Divergent (scope
    /// trimmed to passive-only).</para>
    /// </summary>
    public class Axe_Dismember : BaseSkillPart
    {
        public override string Name => nameof(Axe_Dismember);

        public const int CHANCE_PERCENT = 3;
        public const int BLEED_SAVE_TARGET = 35;
        public const string BLEED_DAMAGE_DICE = "1d2";

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Axe")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            // Find severable body parts. Skip Mortal parts (Head, Heart,
            // etc.) — that's Decapitate's job, gated separately. The
            // remaining set is the Qud "Dismember-able-without-decap"
            // pool: arms, legs, hands, feet, tails, etc.
            var body = ctx.Defender.GetPart<Body>();
            if (body == null) return;

            var candidates = new List<BodyPart>(8);
            foreach (var part in body.GetParts())
            {
                if (part == null) continue;
                if (!part.IsSeverable()) continue;
                if (part.SeverRequiresDecapitate()) continue; // Mortal — skip
                candidates.Add(part);
            }
            if (candidates.Count == 0) return;

            var pick = candidates[ctx.Rng.Next(candidates.Count)];
            if (!body.Dismember(pick, ctx.Zone)) return;

            // Apply Bleeding (Qud-parity values: saveTarget 35, dice "1d2").
            // Use ctx.Rng so the save-roll inside BleedingEffect is
            // deterministic when called from a seeded test.
            ctx.Defender.ApplyEffect(
                new BleedingEffect(BLEED_SAVE_TARGET, BLEED_DAMAGE_DICE, ctx.Rng),
                ctx.Attacker, ctx.Zone);
        }
    }
}
