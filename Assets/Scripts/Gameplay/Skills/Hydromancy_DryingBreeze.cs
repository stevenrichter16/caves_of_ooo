using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Drying Breeze — a warm exhalation that strips wetness. Port of
    /// <c>DryingBreezeMutation</c>; taught by DryingBreezeGrimoire,
    /// buyable in Hydromancy.
    ///
    /// <para>Verbatim: radius 1, cooldown 3, removes WetEffect from every
    /// entity in the 3×3 — the caster included. Always consumes the
    /// cast; the breeze blew whether or not anything was wet.</para>
    /// </summary>
    public class Hydromancy_DryingBreeze : SpellSkillPart
    {
        public override string Name => nameof(Hydromancy_DryingBreeze);

        public const int RADIUS = 1;
        public const int COOLDOWN = 3;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Drying Breeze",
                Command = "CommandDryingBreeze",
                Class = "Hydromancy",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = RADIUS,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.SourceCell == null) { EmitSkillRejectedDiag(ctx, "no_source_cell"); return false; }
            var zone = ctx.Zone;
            var sourceCell = ctx.SourceCell;

            var cells = MultiCellAbilityQueries.RadiusCells(zone, sourceCell.X, sourceCell.Y, RADIUS);
            var pulseTargets = MultiCellAbilityQueries.SnapshotOccupants(cells, null, reverse: true);
            foreach (var cell in cells) SpellFxCapture.AffectCell(zone, cell.X, cell.Y);
            foreach (var entity in pulseTargets)
            {
                if (zone.GetEntityCell(entity) == null) continue;
                if (!entity.HasEffect<WetEffect>()) continue;
                SpellFxCapture.Target(zone, entity);
                entity.RemoveEffect<WetEffect>();
            }

            MessageLog.Add(actor.GetDisplayName() + " exhales a drying breeze.");
            return true;
        }
    }
}
