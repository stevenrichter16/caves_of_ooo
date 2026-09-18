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
    public class Cryomancy_RimeNova : SpellSkillPart
    {
        public override string Name => nameof(Cryomancy_RimeNova);

        public const int RADIUS = 2;
        public const int COOLDOWN = 15;
        public const float CHILL_JOULES = -200f;

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

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.SourceCell == null) { EmitSkillRejectedDiag(ctx, "no_source_cell"); return false; }
            var zone = ctx.Zone;
            var sourceCell = ctx.SourceCell;
            var rng = ctx.Rng;

            ctx.BlocksTurnAdvance = true;

            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: actor);

            if (creatures.Count == 0)
                MessageLog.Add(actor.GetDisplayName() + " unleashes a wave of rime into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                if (zone.GetEntityCell(target) == null || target.GetStatValue("Hitpoints", 0) <= 0) continue;

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

            }

            // Chill EVERY ThermalPart entity in radius — puddles skin
            // over, braziers gutter, kettles stop singing. And the GROUND
            // (Docs/COLD-TILE-BRIDGE.md): every cell in radius takes tile
            // cold, so Jet Blast water becomes ice on the cast. The nova
            // already Frozen(0.6)s every creature in radius directly, so
            // the ground pass adds board state, not new lockdown.
            var groundCells = new System.Collections.Generic.List<Point>();
            var cells = MultiCellAbilityQueries.RadiusCells(zone, sourceCell.X, sourceCell.Y, RADIUS);
            var pulseTargets = MultiCellAbilityQueries.SnapshotOccupants(cells, actor, reverse: true);
            foreach (var cell in cells) groundCells.Add(new Point(cell.X, cell.Y));
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

            ZoneTileStateSystem.ApplyColdToTiles(zone, groundCells, actor, Name);
            return true;
        }
    }
}
