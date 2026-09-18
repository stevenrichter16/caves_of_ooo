using CavesOfOoo.Core;
using System.Collections.Generic;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Ember Vein — a charged beam that scorches everything in its line.
    /// Port of <c>EmberVeinMutation</c>; taught by EmberVeinGrimoire,
    /// buyable in Pyromancy.
    ///
    /// <para>Verbatim: range 7, cooldown 12, 2d6 Heat per creature the
    /// beam passes through (<c>SpellTargeting.TraceBeam</c> pierces
    /// creatures and stops only on solids), plus
    /// <see cref="FireDose.Attack"/> to every ThermalPart entity along
    /// the path, once per owner. Direct damage remains creature-only;
    /// scenery responds through its thermal reactions.</para>
    /// </summary>
    public class Pyromancy_EmberVein : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_EmberVein);

        public const int RANGE = 7;
        public const int COOLDOWN = 12;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ember Vein",
                Command = "CommandEmberVein",
                Class = "Pyromancy",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = RANGE,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.SourceCell == null) { EmitSkillRejectedDiag(ctx, "no_source_cell"); return false; }
            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }
            var zone = ctx.Zone;
            var sourceCell = ctx.SourceCell;

            BeamTraceResult trace = SpellTargeting.TraceBeam(
                zone, actor, sourceCell.X, sourceCell.Y, dx, dy, RANGE);
            if (trace.Path.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_path");
                return false;
            }

            ctx.BlocksTurnAdvance = true;


            // 1. Damage the creatures the beam passed through.
            for (int i = 0; i < trace.HitEntities.Count; i++)
            {
                Entity target = trace.HitEntities[i];
                int damage = DiceRoller.Roll("2d6", ctx.Rng);
                if (damage <= 0)
                    continue;

                MessageLog.Add(
                    actor.GetDisplayName() + " scorches " +
                    target.GetDisplayName() + " for " + damage + " damage!");
                CombatSystem.ApplyDamage(target, damage, "Heat", actor, zone);
            }

            // 2. One heat dose per owner in this cast, including scenery
            // touched away from its anchor. Collect before firing events:
            // shattering, movement and nested casts may mutate occupancy.
            var heatTargets = new List<Entity>();
            var heated = new HashSet<Entity>();
            for (int p = 0; p < trace.Path.Count; p++)
            {
                Point point = trace.Path[p];
                Cell cell = zone.GetCell(point.X, point.Y);
                if (cell == null)
                    continue;

                for (int i = cell.Occupants.Count - 1; i >= 0; i--)
                {
                    Entity entity = cell.Occupants[i];
                    if (entity != actor && entity.HasPart<ThermalPart>() && heated.Add(entity))
                        heatTargets.Add(entity);
                }
            }

            foreach (var entity in heatTargets)
            {
                // A prior heat reaction can remove another snapshotted owner.
                if (zone.GetEntityCell(entity) == null) continue;
                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)FireDose.Attack);
                heatEvent.SetParameter("Radiant", (object)false);
                heatEvent.SetParameter("Source", (object)actor);
                heatEvent.SetParameter("Zone", (object)zone);
                SpellFxCapture.Target(zone, entity);
                entity.FireEventAndRelease(heatEvent);
            }

            return true;
        }
    }
}
