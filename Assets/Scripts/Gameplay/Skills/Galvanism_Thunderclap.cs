using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Thunderclap — a self-centred clap of thunder. Port of
    /// <c>ThunderclapMutation</c>; taught by ThunderclapGrimoire, buyable
    /// in Galvanism.
    ///
    /// <para>Verbatim: radius 2, cooldown 18, 2d6 Electric per creature —
    /// DOUBLED against soaked targets (Wet moisture &gt; 0.2) —
    /// Electrified(1.0) on survivors. Casting into empty space still
    /// consumes the cast.</para>
    ///
    /// <para><b>One deliberate divergence:</b> the mutation's scenery
    /// sweep hand-rolled a conductor check against
    /// <c>MaterialPart.Conductivity &gt; 0.5</c> — a threshold the systems
    /// audit flagged as stale, because Conductivity is authored on two
    /// different scales (0-1 and 0-100) in the same content file. The
    /// port routes the sweep through <see cref="ObjectStatusMatrix"/>,
    /// whose tag-based Conductive gate ("Conductor", "Metal", "Water") is
    /// the single authority on what a charge means for a material.</para>
    /// </summary>
    public class Galvanism_Thunderclap : BaseSkillPart
    {
        public override string Name => nameof(Galvanism_Thunderclap);

        public const int RADIUS = 2;
        public const int COOLDOWN = 18;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Thunderclap",
                Command = "CommandThunderclap",
                Class = "Galvanism",
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
                AsciiFxTheme.Lightning, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Lightning, blocksTurnAdvance: true, delay: ChargeDuration);
            ctx.BlocksTurnAdvance = true;

            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: actor);

            if (creatures.Count == 0)
                MessageLog.Add(actor.GetDisplayName() + " unleashes a clap of thunder into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("2d6", rng);
                var wet = target.GetEffect<WetEffect>();
                if (wet != null && wet.Moisture > 0.2f)
                    damage *= 2;

                if (damage > 0)
                {
                    MessageLog.Add(
                        actor.GetDisplayName() + " jolts " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, "Electric", actor, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                {
                    target.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), actor, zone);
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Lightning, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // Charge the scenery: every conductor in the blast picks up
            // Electrified(0.8). The matrix refuses wood and stone.
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
                        if (entity.HasTag("Creature"))
                            continue;

                        ObjectStatusMatrix.TryApply(new ElectrifiedEffect(charge: 0.8f),
                            entity, actor, zone);
                    }
                }
            }

            return true;
        }
    }
}
