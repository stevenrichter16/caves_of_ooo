using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Chill Draft — a soft exhalation of cold. Port of
    /// <c>ChillDraftMutation</c>; taught by ChillDraftGrimoire, buyable in
    /// Cryomancy.
    ///
    /// <para>Verbatim: radius 1, cooldown 5, −100J to every ThermalPart
    /// entity in the 3×3 — including whatever the caster is standing on.
    /// A utility breath, not a weapon: snuff embers, cool overheating
    /// gear, take the edge off a brewing fire. Always consumes the cast;
    /// the draft blew whether or not anything needed cooling.</para>
    /// </summary>
    public class Cryomancy_ChillDraft : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_ChillDraft);

        public const int RADIUS = 1;
        public const int COOLDOWN = 5;
        public const float CHILL_JOULES = -100f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Chill Draft",
                Command = "CommandChillDraft",
                Class = "Cryomancy",
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
                        if (!entity.HasPart<ThermalPart>())
                            continue;

                        var coolEvent = GameEvent.New("ApplyHeat");
                        coolEvent.SetParameter("Joules", (object)CHILL_JOULES);
                        coolEvent.SetParameter("Radiant", (object)false);
                        coolEvent.SetParameter("Source", (object)actor);
                        coolEvent.SetParameter("Zone", (object)zone);
                        entity.FireEvent(coolEvent);
                        coolEvent.Release();
                    }
                }
            }

            return true;
        }
    }
}
