using System;
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
    public class Hydromancy_DryingBreeze : BaseSkillPart
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

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.SourceCell == null) { EmitSkillRejectedDiag(ctx, "no_source_cell"); return false; }
            var zone = ctx.Zone;
            var sourceCell = ctx.SourceCell;

            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y)) > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = cell.Objects.Count - 1; i >= 0; i--)
                    {
                        if (i >= cell.Objects.Count) continue;
                        Entity entity = cell.Objects[i];
                        if (entity.HasEffect<WetEffect>())
                            entity.RemoveEffect<WetEffect>();
                    }
                }
            }

            MessageLog.Add(actor.GetDisplayName() + " exhales a drying breeze.");
            return true;
        }
    }
}
