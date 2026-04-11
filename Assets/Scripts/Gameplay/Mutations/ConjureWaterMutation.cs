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

            int tx = sourceCell.X + (dx * range);
            int ty = sourceCell.Y + (dy * range);
            if (!zone.InBounds(tx, ty))
            {
                tx = sourceCell.X + dx;
                ty = sourceCell.Y + dy;
            }

            Cell targetCell = zone.GetCell(tx, ty);
            if (targetCell == null)
                return false;

            var factory = MaterialReactionResolver.Factory;
            Entity puddle = factory?.CreateEntity("WaterPuddle");
            if (puddle == null)
                puddle = CreateFallbackWaterPuddle();
            if (puddle != null)
                zone.AddEntity(puddle, tx, ty);

            bool interacted = false;
            for (int i = targetCell.Objects.Count - 1; i >= 0; i--)
            {
                Entity entity = targetCell.Objects[i];
                if (entity == puddle)
                    continue;

                entity.ApplyEffect(new WetEffect(0.5f), ParentEntity, zone);

                if (entity.HasEffect<BurningEffect>())
                {
                    MaterialReactionResolver.EvaluateReactions(entity, zone, entity.GetEffect<BurningEffect>());
                    interacted = true;
                }
            }

            if (puddle != null || interacted)
            {
                CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
                return true;
            }

            return false;
        }

        private static Entity CreateFallbackWaterPuddle()
        {
            var entity = new Entity { BlueprintName = "WaterPuddle" };
            entity.AddPart(new RenderPart
            {
                DisplayName = "puddle of water",
                RenderString = "~",
                ColorString = "&B",
                RenderLayer = 1
            });
            entity.AddPart(new MaterialPart
            {
                MaterialID = "Water",
                Combustibility = 0f,
                MaterialTagsRaw = "Liquid,Water"
            });
            entity.AddPart(new ThermalPart
            {
                Temperature = 15f,
                FlameTemperature = 9000f,
                VaporTemperature = 100f
            });
            return entity;
        }
    }
}
