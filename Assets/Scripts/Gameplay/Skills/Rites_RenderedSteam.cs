using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of Rendered Steam — renders the soaked-and-burning
    /// to scalding steam: a +1.5 multiplier bonus and Confusion when a
    /// target's consumed marks contain BOTH Wet and Burning. Port of
    /// <c>RenderedSteamMutation</c>, converged onto the spine. Taught by
    /// RenderedSteamGrimoire, buyable in Rites.</summary>
    public class Rites_RenderedSteam : ConsumingRiteSkillBase
    {
        public const float PAIR_BONUS = 1.5f;
        public const int CONFUSE_TURNS = 3;

        public override string Name => nameof(Rites_RenderedSteam);
        public override string CommandName => "CommandRenderedSteam";
        public override string Element => "Heat";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 35;
        public override int Slots => 2;
        public override int BaseDamage => 5;
        public override string[] DamageAttributes => new[] { "Fire", "Heat" };

        /// <summary>Detonations this cast — read by CastMessage.
        /// Per-cast scratch state; single-threaded dispatch.</summary>
        private int _detonations;

        private static bool IsPair(ResonanceSystem.Result res)
            => res.Consumed.Contains("Wet") && res.Consumed.Contains("Burning");

        protected override int ComputeDamage(ResonanceSystem.Result res)
        {
            bool pair = IsPair(res);
            if (pair) _detonations++;
            float mult = res.Multiplier + (pair ? PAIR_BONUS : 0f);
            return (int)Math.Round(BaseDamage * mult);
        }

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (IsPair(res))
                target.ApplyEffect(new ConfusedEffect(CONFUSE_TURNS), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            int detonations = _detonations;
            _detonations = 0;
            return detonations > 0
                ? caster.GetDisplayName() + "'s rite renders " + detonations
                  + " body" + (detonations == 1 ? "" : "ies") + " to scalding steam!"
                : caster.GetDisplayName() + "'s rite hisses — nothing was both soaked and burning.";
        }
    }
}
