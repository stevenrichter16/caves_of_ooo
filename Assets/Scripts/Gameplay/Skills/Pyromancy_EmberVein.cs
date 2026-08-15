using CavesOfOoo.Core;

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
    /// the path. Damage routes through <c>RouteDamage</c> so the beam's
    /// heat pass and its damage pass agree about scenery.</para>
    /// </summary>
    public class Pyromancy_EmberVein : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_EmberVein);

        public const int RANGE = 7;
        public const int COOLDOWN = 12;
        private const float ChargeDuration = 0.08f;
        private const float BeamDuration = 0.12f;

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

        public override bool OnCommand(SkillEventContext ctx)
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

            AsciiFxBus.EmitChargeOrbit(zone, actor, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Fire, blocksTurnAdvance: true);
            AsciiFxBus.EmitBeam(zone, trace.Path, dx, dy, AsciiFxTheme.Fire,
                duration: BeamDuration, blocksTurnAdvance: true, delay: ChargeDuration);
            ctx.BlocksTurnAdvance = true;

            Point impact = trace.GetImpactPoint();
            if (impact.X >= 0)
            {
                AsciiFxBus.EmitBurst(zone, impact.X, impact.Y, AsciiFxTheme.Fire,
                    blocksTurnAdvance: true, delay: ChargeDuration + BeamDuration);
            }

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

            // 2. Per-cell heat pass: FireDose.Attack to every ThermalPart
            // entity along the path. Reverse-iterate + re-check Count —
            // shatters and reactions mutate the collection mid-loop.
            for (int p = 0; p < trace.Path.Count; p++)
            {
                Point point = trace.Path[p];
                Cell cell = zone.GetCell(point.X, point.Y);
                if (cell == null)
                    continue;

                for (int i = cell.Objects.Count - 1; i >= 0; i--)
                {
                    if (i >= cell.Objects.Count) continue;
                    Entity entity = cell.Objects[i];
                    if (entity == actor)
                        continue;

                    if (entity.HasPart<ThermalPart>())
                    {
                        var heatEvent = GameEvent.New("ApplyHeat");
                        heatEvent.SetParameter("Joules", (object)FireDose.Attack);
                        heatEvent.SetParameter("Radiant", (object)false);
                        heatEvent.SetParameter("Source", (object)actor);
                        heatEvent.SetParameter("Zone", (object)zone);
                        entity.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }
            }

            return true;
        }
    }
}
