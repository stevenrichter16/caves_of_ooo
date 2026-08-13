using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Physical activated mutation: Flaming Hands.
    /// Deals Level * 1d4 fire damage to all creatures in a target adjacent cell.
    /// 10-turn cooldown. Mirrors Qud's FlamingRay (simplified to adjacent-only).
    /// </summary>
    public class FlamingHandsMutation : BaseMutation
    {
        public const string COMMAND_NAME = "CommandFlamingHands";
        public const int COOLDOWN = 10;

        public override string Name => "FlamingHands";
        public override string MutationType => "Physical";
        public override string DisplayName => "Flaming Hands";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                "Flaming Hands",
                COMMAND_NAME,
                "Physical Mutations",
                AbilityTargetingMode.AdjacentCell,
                1);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == COMMAND_NAME)
            {
                // Get parameters from the event
                var targetCell = e.GetParameter<Cell>("TargetCell");
                var zone = e.GetParameter<Zone>("Zone");
                var rng = e.GetParameter<Random>("RNG") ?? new Random();

                if (targetCell == null || zone == null)
                    return true;

                Cast(targetCell, zone, rng);
                e.SetParameter("BlocksTurnAdvance", true);
                e.Handled = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fire flaming hands at a target cell. Deals Level * 1d4 damage to each creature.
        /// </summary>
        public void Cast(Cell targetCell, Zone zone, Random rng)
        {
            if (targetCell == null) return;

            // Brief charge-up orbit around the caster before the burst
            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: 0.15f,
                AsciiFxTheme.Fire, blocksTurnAdvance: true);

            // Burst at target after charge completes
            AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y, AsciiFxTheme.Fire,
                blocksTurnAdvance: true, delay: 0.15f);

            // PALIMPSEST: put heat on the GROUND, not only on bodies.
            // Without this the tile layer is invisible to fire and an
            // oil slick cannot be lit. Resolved immediately so the oil
            // goes up on the cast, and P4's propagation then runs the
            // fire down the rest of the slick.
            ZoneTileStateSystem.ApplyFireToTile(
                zone, targetCell.X, targetCell.Y, ParentEntity, Name);
            ZoneTileStateSystem.ResolveAfterAbility(zone, ParentEntity);

            // Everything in the cell that fire can mean anything to — not
            // just creatures. Blasting a tree used to report "blasts the
            // empty space with flames!" and leave it standing, because this
            // read GetObjectsWithTag("Creature").
            List<Entity> creatures = new List<Entity>();
            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                var candidate = targetCell.Objects[i];
                if (AbilityTargeting.IsElementalTarget(candidate, ParentEntity))
                    creatures.Add(candidate);
            }

            if (creatures.Count == 0)
            {
                MessageLog.Add($"{ParentEntity.GetDisplayName()} blasts the empty space with flames!");
            }
            else
            {
                string attackerName = ParentEntity.GetDisplayName();

                // Damage each creature: Level * 1d4
                for (int i = creatures.Count - 1; i >= 0; i--)
                {
                    var target = creatures[i];
                    if (target == ParentEntity) continue;

                    int totalDamage = 0;
                    for (int d = 0; d < Level; d++)
                        totalDamage += DiceRoller.Roll(4, rng);

                    if (totalDamage > 0)
                    {
                        string targetName = target.GetDisplayName();
                        // WSP7.4 — tag with Heat so HeatResistance applies.
                        // RouteDamage, not ApplyDamage: scenery keeps its
                        // hitpoints on a DestructiblePart and ApplyDamage
                        // early-returns on anything without a Hitpoints
                        // stat, so this was a no-op against a tree.
                        var heatDmg = new Damage(totalDamage);
                        heatDmg.AddAttribute("Heat");
                        int landed = DestructionSystem.RouteDamage(
                            target, heatDmg, ParentEntity, zone);
                        MessageLog.Add($"{attackerName} blasts {targetName} with flames for {landed} damage!");

                        // Participate in the material system: emit heat to the target
                        var heatEvent = GameEvent.New("ApplyHeat");
                        // FireDose.Attack, not damage-scaled. The old figure was
                        // totalDamage * 5 = 5-20 joules at level 1, which
                        // ambient decay ate faster than it accumulated: the
                        // spell could not ignite ANYTHING, ever, however many
                        // times it was cast. Higher levels burn hotter.
                        heatEvent.SetParameter("Joules",
                            (object)(FireDose.Attack + (Level - 1) * FireDose.Cantrip));
                        heatEvent.SetParameter("Radiant", (object)false);
                        heatEvent.SetParameter("Source", (object)ParentEntity);
                        heatEvent.SetParameter("Zone", (object)zone);
                        target.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }
            }

            // Put on cooldown
            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
        }
    }
}
