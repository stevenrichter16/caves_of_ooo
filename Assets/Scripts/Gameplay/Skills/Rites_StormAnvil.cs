using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Storm Anvil — brings the storm down on
    /// everything nearby; resonance riders decide what each mark bought
    /// (Stun / Arc / Shatter). Port of <c>StormAnvilMutation</c>, now on
    /// the converged spine (it used to hand-roll the ink/spend loop).
    /// Taught by StormAnvilGrimoire, buyable in Rites.</summary>
    public class Rites_StormAnvil : ConsumingRiteSkillBase
    {
        public const int STUN_RIDER_TURNS = 3;

        public override string Name => nameof(Rites_StormAnvil);
        public override string CommandName => "CommandStormAnvil";
        public override string Element => "Electric";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 25;
        public override int Slots => 2;
        public override int BaseDamage => 4;
        public override string[] DamageAttributes => new[] { "Electric", "Lightning" };

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            for (int r = 0; r < res.Riders.Count; r++)
            {
                switch (res.Riders[r])
                {
                    case "Stun":
                        target.ApplyEffect(
                            new StunnedEffect(duration: STUN_RIDER_TURNS),
                            ParentEntity, zone);
                        break;
                    case "Arc":
                        target.ApplyEffect(
                            new ElectrifiedEffect(charge: 1.0f), ParentEntity, zone);
                        break;
                    case "Shatter":
                        target.ApplyEffect(new BrokenEffect(), ParentEntity, zone);
                        break;
                }
            }
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return caster.GetDisplayName() + " brings down the storm anvil"
                + (marks > 0
                    ? ", spending " + marks + " mark" + (marks == 1 ? "" : "s") + "!"
                    : " — but nothing was primed to spend.");
        }
    }
}
