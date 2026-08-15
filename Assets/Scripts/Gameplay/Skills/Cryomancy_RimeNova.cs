using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Rime Nova — a self-centred ring of rime. Port of
    /// <c>RimeNovaMutation</c>; taught by RimeNovaGrimoire, buyable in
    /// Cryomancy.
    ///
    /// <para>Verbatim: radius 2, cooldown 15, 1d6 Cold per creature,
    /// FrozenEffect(0.6) on survivors, and a −200J chill over every
    /// ThermalPart entity in radius (the freeze-the-scenery pass).
    /// Casting into empty space still consumes the cast — the wave went
    /// out.</para>
    /// </summary>
    public class Cryomancy_RimeNova : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_RimeNova);

        public const int RADIUS = 2;
        public const int COOLDOWN = 15;
        public const float CHILL_JOULES = -200f;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Rime Nova",
                Command = "CommandRimeNova",
                Class = "Cryomancy",
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
                AsciiFxTheme.Ice, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Ice, blocksTurnAdvance: true, delay: ChargeDuration);
            ctx.BlocksTurnAdvance = true;

            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: actor);

            if (creatures.Count == 0)
                MessageLog.Add(actor.GetDisplayName() + " unleashes a wave of rime into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("1d6", rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        actor.GetDisplayName() + " rimes " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, "Cold", actor, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                {
                    target.ApplyEffect(new FrozenEffect(cold: 0.6f), actor, zone);
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Ice, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // Chill EVERY ThermalPart entity in radius — puddles skin
            // over, braziers gutter, kettles stop singing. And the GROUND
            // (Docs/COLD-TILE-BRIDGE.md): every cell in radius takes tile
            // cold, so Jet Blast water becomes ice on the cast. The nova
            // already Frozen(0.6)s every creature in radius directly, so
            // the ground pass adds board state, not new lockdown.
            var groundCells = new System.Collections.Generic.List<Point>();
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
                    groundCells.Add(new Point(x, y));

                    for (int i = cell.Objects.Count - 1; i >= 0; i--)
                    {
                        if (i >= cell.Objects.Count) continue;
                        Entity entity = cell.Objects[i];
                        if (entity == actor)
                            continue;

                        if (entity.HasPart<ThermalPart>())
                        {
                            var heatEvent = GameEvent.New("ApplyHeat");
                            heatEvent.SetParameter("Joules", (object)CHILL_JOULES);
                            heatEvent.SetParameter("Radiant", (object)false);
                            heatEvent.SetParameter("Source", (object)actor);
                            heatEvent.SetParameter("Zone", (object)zone);
                            entity.FireEvent(heatEvent);
                            heatEvent.Release();
                        }
                    }
                }
            }

            ZoneTileStateSystem.ApplyColdToTiles(zone, groundCells, actor, Name);
            return true;
        }
    }
}
