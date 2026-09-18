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
    public class Pyromancy_Conflagration : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_Conflagration);

        public const int RADIUS = 2;
        public const int COOLDOWN = 15;

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
                MessageLog.Add(actor.GetDisplayName() + " unleashes a wave of fire into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                if (zone.GetEntityCell(target) == null || target.GetStatValue("Hitpoints", 0) <= 0) continue;

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
                        SpellFxCapture.Target(zone, target);
                        target.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }

            }

            // Heat EVERY ThermalPart entity in radius — the pass that
            // ignites barrels and scenery, not just bodies.
            var cells = MultiCellAbilityQueries.RadiusCells(zone, sourceCell.X, sourceCell.Y, RADIUS);
            var pulseTargets = MultiCellAbilityQueries.SnapshotOccupants(cells, actor, reverse: true);
            foreach (var cell in cells) SpellFxCapture.AffectCell(zone, cell.X, cell.Y);
            foreach (var entity in pulseTargets)
            {
                if (zone.GetEntityCell(entity) == null) continue;
                if (!entity.HasPart<ThermalPart>()) continue;
                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)250f);
                heatEvent.SetParameter("Radiant", (object)false);
                heatEvent.SetParameter("Source", (object)actor);
                heatEvent.SetParameter("Zone", (object)zone);
                SpellFxCapture.Target(zone, entity);
                try { entity.FireEvent(heatEvent); }
                finally { heatEvent.Release(); }
            }

            MaterialSimSystem.EmitHeatToAdjacent(actor, zone, 100f);
            return true;
        }
    }
}
