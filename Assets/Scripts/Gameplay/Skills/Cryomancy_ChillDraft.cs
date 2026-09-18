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
    public class Cryomancy_ChillDraft : SpellSkillPart
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
                if (!entity.HasPart<ThermalPart>()) continue;
                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)CHILL_JOULES);
                heatEvent.SetParameter("Radiant", (object)false);
                heatEvent.SetParameter("Source", (object)actor);
                heatEvent.SetParameter("Zone", (object)zone);
                SpellFxCapture.Target(zone, entity);
                try { entity.FireEvent(heatEvent); }
                finally { heatEvent.Release(); }
            }

            return true;
        }
    }
}
