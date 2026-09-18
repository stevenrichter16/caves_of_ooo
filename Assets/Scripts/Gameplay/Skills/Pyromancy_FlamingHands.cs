using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Flaming Hands — a point-blank cone of flame into a chosen
    /// adjacent cell. Port of <c>FlamingHandsMutation</c>, one of the
    /// two starting powers; buyable in Pyromancy and granted by the
    /// starting kit.
    ///
    /// <para><b>Level-scaling frozen at level 1 (user decision, plan
    /// §5):</b> flat 1d4 damage (was Level×1d4) and
    /// <see cref="FireDose.Attack"/> heat (was Attack + (Level−1)×
    /// Cantrip). The mutation's level-scaling tests are replaced by
    /// flat pins, deliberately.</para>
    ///
    /// <para>Everything else verbatim: cooldown 10, hits every
    /// elemental target in the cell (creatures AND scenery — the
    /// audit-era fix), tile-layer fire so oil slicks light on the cast,
    /// "blasts the empty space" when the cell is empty — which still
    /// consumes the cast, exactly as the mutation did.</para>
    /// </summary>
    public class Pyromancy_FlamingHands : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_FlamingHands);

        public const int COOLDOWN = 10;
        public const string DAMAGE_DICE = "1d4";

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Flaming Hands",
                Command = "CommandFlamingHands",
                Class = "Pyromancy",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.TargetCell == null) { EmitSkillRejectedDiag(ctx, "no_target_cell"); return false; }
            var zone = ctx.Zone;
            var targetCell = ctx.TargetCell;
            SpellFxCapture.AffectCell(zone, targetCell.X, targetCell.Y);

            ctx.BlocksTurnAdvance = true;

            // Heat the GROUND too — an oil slick lights on the cast.
            ZoneTileStateSystem.ApplyFireToTile(
                zone, targetCell.X, targetCell.Y, actor, Name);
            ZoneTileStateSystem.ResolveAfterAbility(zone, actor);

            // Everything fire can mean anything to — creatures and scenery.
            var targets = new System.Collections.Generic.List<Entity>();
            for (int i = 0; i < targetCell.Occupants.Count; i++)
            {
                var candidate = targetCell.Occupants[i];
                if (AbilityTargeting.IsElementalTarget(candidate, actor))
                    targets.Add(candidate);
            }

            if (targets.Count == 0)
            {
                MessageLog.Add($"{actor.GetDisplayName()} blasts the empty space with flames!");
            }
            else
            {
                string attackerName = actor.GetDisplayName();
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    var target = targets[i];
                    if (target == actor || zone.GetEntityCell(target) == null) continue;
                    SpellFxCapture.TargetAt(zone, target, new Point(targetCell.X, targetCell.Y));

                    int totalDamage = DiceRoller.Roll(DAMAGE_DICE, ctx.Rng);
                    if (totalDamage > 0)
                    {
                        string targetName = target.GetDisplayName();
                        var heatDmg = new Damage(totalDamage);
                        heatDmg.AddAttribute("Heat");
                        int landed = DestructionSystem.RouteDamage(
                            target, heatDmg, actor, zone);
                        MessageLog.Add($"{attackerName} blasts {targetName} with flames for {landed} damage!");

                        var heatEvent = GameEvent.New("ApplyHeat");
                        heatEvent.SetParameter("Joules", (object)FireDose.Attack);
                        heatEvent.SetParameter("Radiant", (object)false);
                        heatEvent.SetParameter("Source", (object)actor);
                        heatEvent.SetParameter("Zone", (object)zone);
                        SpellFxCapture.Target(zone, target);
                        target.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }
            }

            // Blasting empty space is still a cast — the flames flew.
            return true;
        }
    }
}
