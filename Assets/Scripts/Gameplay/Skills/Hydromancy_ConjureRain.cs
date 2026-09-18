using System;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Conjure Rain — the Watering Grimoire's spell. Port of
    /// <c>ConjureRainMutation</c>; taught by WateringGrimoire, buyable in
    /// Hydromancy. See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.4</c>.
    ///
    /// <para>Verbatim: self-centred radius 3, cooldown 5, tops every
    /// crop's moisture up via <see cref="CropPart.Water"/> (top-up
    /// semantics, never additive), falling-rain particles over each
    /// watered tile, wet-soil darkening from CropPart itself. The cast
    /// always consumes even with zero crops nearby — the rain was
    /// called; the message tells the player what it found.</para>
    /// </summary>
    public class Hydromancy_ConjureRain : SpellSkillPart
    {
        public override string Name => nameof(Hydromancy_ConjureRain);

        public const int RADIUS = 3;
        public const int COOLDOWN = 5;

        /// <summary>Moisture granted per watering — top-up semantics via
        /// <see cref="CropPart.Water"/>, never additive.</summary>
        public const int MOISTURE_TICKS = 40;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Conjure Rain",
                Command = "CommandConjureRain",
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

            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            int cropsWatered = 0;
            var targets = new System.Collections.Generic.List<(Entity owner, Cell contact)>();
            var seen = new System.Collections.Generic.HashSet<Entity>();
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y)) > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Occupants.Count; i++)
                    {
                        var crop = cell.Occupants[i].GetPart<CropPart>();
                        if (crop == null)
                            continue;

                        var owner = cell.Occupants[i];
                        if (seen.Add(owner)) targets.Add((owner, cell));
                        break; // one crop per cell by the planting gate
                    }
                }
            }

            foreach (var target in targets)
            {
                if (zone.GetEntityCell(target.owner) == null) continue;
                var crop = target.owner.GetPart<CropPart>();
                if (crop == null) continue;
                SpellFxCapture.TargetAt(zone, target.owner, new Point(target.contact.X, target.contact.Y));
                crop.Water(MOISTURE_TICKS);
                EmitRainFx(zone, target.contact.X, target.contact.Y);
                cropsWatered++;
            }

            MessageLog.Add(cropsWatered > 0
                ? "Rain patters down over the crops."
                : "The conjured rain finds no crops to nourish.");

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "RainConjured", actor: actor,
                    payload: new { cropsWatered = cropsWatered, radius = RADIUS });

            return true;
        }

        private static void EmitRainFx(Zone zone, int x, int y)
        {
            SpellFxCapture.AffectCell(zone, x, y);
        }
    }
}
