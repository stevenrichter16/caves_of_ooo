using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class on-hit Broken passive. Per Qud's
    /// <c>Cudgel_Hammer.cs:11-22</c> — on every Cudgel hit, 2% chance
    /// to apply <see cref="BrokenEffect"/> to a random equipped item
    /// on the defender.
    ///
    /// <para>CoO port iterates the defender's <see cref="Body"/>'s
    /// equipped items (skipping unarmed body parts), picks one at
    /// random via <see cref="SkillEventContext.Rng"/>, and applies
    /// <see cref="BrokenEffect"/> to that item entity. If the
    /// defender has no equipped items, the proc no-ops silently.</para>
    ///
    /// <para><b>Gameplay-impact divergence (deferred):</b> in v1
    /// <see cref="BrokenEffect"/> is a marker only — it logs but
    /// doesn't disable the item. The future equip-block hook will
    /// gate item use on <c>HasEffect&lt;BrokenEffect&gt;</c>.</para>
    /// </summary>
    public class Cudgel_Hammer : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Hammer);

        public const int CHANCE_PERCENT = 2;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            // Pick a random equipped item on the defender. If they have
            // no Body or no equipped items, no-op silently — Hammer
            // can't break what isn't worn.
            var body = ctx.Defender.GetPart<Body>();
            if (body == null) return;

            // Collect equipped items (one per body part with something
            // in its Equipped slot). Skip parts whose Equipped is null.
            var candidates = new System.Collections.Generic.List<Entity>(8);
            body.ForeachEquippedObject((item, bp) =>
            {
                if (item != null) candidates.Add(item);
            });
            if (candidates.Count == 0) return;

            var pick = candidates[ctx.Rng.Next(candidates.Count)];
            pick.ApplyEffect(new BrokenEffect(), ctx.Attacker, ctx.Zone);
        }
    }
}
