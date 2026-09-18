using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active: a sudden plunge in temperature that stiffens
    /// every creature nearby.
    ///
    /// <para>SPELLCRAFT SM6 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.4) —
    /// the family's tempo tool. It deals no damage at all; what it buys
    /// is TURNS. A prime-then-detonate plan needs a pack to still be
    /// standing where you left it when the payoff lands, and Cold Snap
    /// is how you arrange that.</para>
    ///
    /// <para>Self-centred, so it needs no aiming — and it deliberately
    /// spares its caster, because a slow that also slowed you would be
    /// strictly bad to cast.</para>
    /// </summary>
    public class Cryomancy_ColdSnap : SpellSkillPart
    {
        public override string Name => nameof(Cryomancy_ColdSnap);

        public const int COOLDOWN = 30;
        public const int SNAP_RADIUS = 2;

        /// <summary>Turns of <see cref="HobbledEffect"/>. Long enough to
        /// matter across a setup, short enough not to be a stun.</summary>
        public const int SNAP_DURATION = 6;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Cold Snap",
                Command = "CommandColdSnap",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = SNAP_RADIUS,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            // No direction check: a self-centred power must not demand a
            // facing.
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            List<Entity> caught = SpellTargeting.GetCreaturesInRadius(
                ctx.Zone, actorPos.x, actorPos.y, SNAP_RADIUS, exclude: actor);

            if (caught.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s cold snap bites empty air.");
                return false;
            }

            for (int i = 0; i < caught.Count; i++)
                caught[i].ApplyEffect(new HobbledEffect(SNAP_DURATION), actor, ctx.Zone);

            MessageLog.Add(actor.GetDisplayName() + "'s cold snap stiffens "
                + caught.Count + " creature" + (caught.Count == 1 ? "" : "s") + "!");
        
            return true;
        }
    }
}
