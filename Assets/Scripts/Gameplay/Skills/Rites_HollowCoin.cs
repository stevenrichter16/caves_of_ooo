using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Hollow Coin — calls in every debt at once;
    /// three or more marks break the debtor outright. Port of
    /// <c>HollowCoinMutation</c>. Taught by HollowCoinGrimoire, buyable
    /// in Rites.</summary>
    public class Rites_HollowCoin : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_HollowCoin);
        public override string CommandName => "CommandHollowCoin";
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 4;
        public override int Cooldown => 50;
        public override int Slots => 3;
        public override int BaseDamage => 3;

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count >= 3)
                target.ApplyEffect(new BrokenEffect(), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " turns the hollow coin over — " + marks
                  + " mark" + (marks == 1 ? "" : "s") + " called in at once!"
                : caster.GetDisplayName() + " turns the hollow coin over. Nothing is owed.";
        }
    }
}
