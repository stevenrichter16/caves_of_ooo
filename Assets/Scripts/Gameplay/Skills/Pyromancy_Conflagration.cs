using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Conflagration — a self-centred ring of fire. Port of
    /// <c>ConflagrationMutation</c>; taught by ConflagrationGrimoire,
    /// buyable in Pyromancy.
    ///
    /// <para>Verbatim: radius 2, cooldown 15, 2d6 Heat per creature,
    /// <see cref="FireDose.Blast"/> per damaged creature, direct
    /// <c>BurningEffect(1.5)</c> on survivors, a 250J sweep over every
    /// ThermalPart entity in radius (the barrels-catch-fire pass), and a
    /// 100J radiant pulse for chain propagation. Casting into empty
    /// space still consumes the cast — the wave went out.</para>
    /// </summary>
    public class Pyromancy_Conflagration : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_Conflagration);

        public const int RADIUS = 2;
        public const int COOLDOWN = 15;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Conflagration",
                Command = "CommandConflagration",
                Class = "Pyromancy",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = RADIUS,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.SourceCell == null) { EmitSkillRejectedDiag(ctx, "no_source_cell"); return false; }
            var zone = ctx.Zone;
            var sourceCell = ctx.SourceCell;
            var rng = ctx.Rng;

            AsciiFxBus.EmitChargeOrbit(zone, actor, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Fire, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Fire, blocksTurnAdvance: true, delay: ChargeDuration);
            ctx.BlocksTurnAdvance = true;

            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: actor);

            if (creatures.Count == 0)
                MessageLog.Add(actor.GetDisplayName() + " unleashes a wave of fire into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("2d6", rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        actor.GetDisplayName() + " engulfs " +
                        target.GetDisplayName() + " in flames for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, "Heat", actor, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                {
                    target.ApplyEffect(new BurningEffect(intensity: 1.5f, source: actor, rng: rng),
                        actor, zone);

                    if (damage > 0)
                    {
                        var heatEvent = GameEvent.New("ApplyHeat");
                        heatEvent.SetParameter("Joules", (object)FireDose.Blast);
                        heatEvent.SetParameter("Radiant", (object)false);
                        heatEvent.SetParameter("Source", (object)actor);
                        heatEvent.SetParameter("Zone", (object)zone);
                        target.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Fire, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // Heat EVERY ThermalPart entity in radius — the pass that
            // ignites barrels and scenery, not just bodies.
            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int chebyshev = Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y));
                    if (chebyshev > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
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
                            heatEvent.SetParameter("Joules", (object)250f);
                            heatEvent.SetParameter("Radiant", (object)false);
                            heatEvent.SetParameter("Source", (object)actor);
                            heatEvent.SetParameter("Zone", (object)zone);
                            entity.FireEvent(heatEvent);
                            heatEvent.Release();
                        }
                    }
                }
            }

            MaterialSimSystem.EmitHeatToAdjacent(actor, zone, 100f);
            return true;
        }
    }
}
