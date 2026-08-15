using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Hanging Bolt — pins one creature under a
    /// suspended bolt. Damage is FLAT (marks buy paralysis turns, 2 per
    /// mark, not damage) — hence the <see cref="ComputeDamage"/>
    /// override. Port of <c>HangingBoltMutation</c>, converged onto the
    /// spine. Taught by HangingBoltGrimoire, buyable in Rites.</summary>
    public class Rites_HangingBolt : ConsumingRiteSkillBase
    {
        public const int PARALYSIS_PER_MARK = 2;

        public override string Name => nameof(Rites_HangingBolt);
        public override string CommandName => "CommandHangingBolt";
        public override string Element => "Electric";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 6;
        public override int Cooldown => 30;
        public override int Slots => 2;
        public override int BaseDamage => 2;
        public override string[] DamageAttributes => new[] { "Electric", "Lightning" };

        /// <summary>Flat — the marks are spent on the pin, not the hit.</summary>
        protected override int ComputeDamage(ResonanceSystem.Result res) => BaseDamage;

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            int turns = res.Consumed.Count * PARALYSIS_PER_MARK;
            if (turns > 0)
                target.ApplyEffect(new ParalyzedEffect(turns), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            int turns = marks * PARALYSIS_PER_MARK;
            return turns > 0
                ? caster.GetDisplayName() + " pins " + targets[0].GetDisplayName()
                  + " under a hanging bolt for " + turns + " turns!"
                : caster.GetDisplayName() + "'s hanging bolt fizzles — "
                  + targets[0].GetDisplayName() + " carried nothing to spend.";
        }
    }
}
