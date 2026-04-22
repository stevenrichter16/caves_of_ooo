using System;

namespace CavesOfOoo.Core
{
    public class ConjureWaterMutation : BaseMutation
    {
        public const string COMMAND = "CommandConjureWater";
        public const int COOLDOWN = 4;
        public const int RANGE = 2;

        public override string Name => "Conjure Water";
        public override string MutationType => "Mental";
        public override string DisplayName => "Conjure Water";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Grimoire Spells",
                AbilityTargetingMode.DirectionLine,
                RANGE);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != COMMAND)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            int dx = e.GetParameter<int>("DirectionX");
            int dy = e.GetParameter<int>("DirectionY");
            int range = e.GetParameter<int>("Range");
            if (!Cast(zone, sourceCell, dx, dy, range > 0 ? range : RANGE))
                return true;

            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell, int dx, int dy, int range)
        {
            if (zone == null || sourceCell == null || ParentEntity == null)
                return false;
            if (dx == 0 && dy == 0)
                return false;

            // Walk up to `range` steps; stop early on the first burning entity,
            // or at the last passable cell before a wall.
            Cell targetCell = null;
            for (int step = 1; step <= range; step++)
            {
                int tx = sourceCell.X + dx * step;
                int ty = sourceCell.Y + dy * step;
                if (!zone.InBounds(tx, ty))
                    break;
                Cell candidate = zone.GetCell(tx, ty);
                if (candidate == null)
                    break;

                // Stop at the first cell that has a burning entity
                for (int i = 0; i < candidate.Objects.Count; i++)
                {
                    if (candidate.Objects[i].HasEffect<BurningEffect>())
                    {
                        targetCell = candidate;
                        goto placeWater;
                    }
                }

                // Stop if the cell is blocked (e.g. a wall), but don't land on it
                if (!candidate.IsPassable())
                    break;

                targetCell = candidate;
            }

            placeWater:
            if (targetCell == null)
                return false;

            var factory = MaterialReactionResolver.Factory;
            Entity puddle = factory?.CreateEntity("WaterPuddle");
            if (puddle == null)
            {
                UnityEngine.Debug.LogError("[ConjureWater] Factory returned null for WaterPuddle blueprint.");
                return false;
            }
            zone.AddEntity(puddle, targetCell.X, targetCell.Y);

            for (int i = targetCell.Objects.Count - 1; i >= 0; i--)
            {
                Entity entity = targetCell.Objects[i];
                if (entity == puddle)
                    continue;

                entity.ApplyEffect(new WetEffect(0.5f), ParentEntity, zone);

                if (entity.HasEffect<BurningEffect>())
                    MaterialReactionResolver.EvaluateReactions(entity, zone, entity.GetEffect<BurningEffect>());
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
