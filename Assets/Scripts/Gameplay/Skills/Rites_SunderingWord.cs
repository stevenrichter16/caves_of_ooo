using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Sundering Word — unmakes what carried a
    /// mark: Broken + Weakened on every marked survivor in radius. Port
    /// of <c>SunderingWordMutation</c>. Taught by SunderingWordGrimoire,
    /// buyable in Rites.</summary>
    public class Rites_SunderingWord : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_SunderingWord);
        public override string CommandName => "CommandSunderingWord";
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 45;
        public override int Slots => 2;
        public override int BaseDamage => 4;

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count > 0)
            {
                target.ApplyEffect(new BrokenEffect(), ParentEntity, zone);
                target.ApplyEffect(new WeakenedEffect(), ParentEntity, zone);
            }
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return caster.GetDisplayName() + " speaks the sundering word over " + targets.Count
                + " target" + (targets.Count == 1 ? "" : "s")
                + (marks > 0 ? " — " + marks + " unmade!" : ", and nothing answers.");
        }
    }
}
