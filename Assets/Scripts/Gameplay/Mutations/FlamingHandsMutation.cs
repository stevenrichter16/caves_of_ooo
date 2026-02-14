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
            ActivatedAbilityID = AddMyActivatedAbility("Flaming Hands", COMMAND_NAME, "Physical Mutations");
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

            // Get all creatures in the target cell
            List<Entity> creatures = targetCell.GetObjectsWithTag("Creature");

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
                        MessageLog.Add($"{attackerName} blasts {targetName} with flames for {totalDamage} damage!");
                        CombatSystem.ApplyDamage(target, totalDamage, ParentEntity, zone);
                    }
                }
            }

            // Put on cooldown
            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
        }
    }
}
