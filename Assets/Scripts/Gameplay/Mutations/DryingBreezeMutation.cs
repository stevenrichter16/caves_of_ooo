using System;

namespace CavesOfOoo.Core
{
    public class DryingBreezeMutation : BaseMutation
    {
        public const string COMMAND = "CommandDryingBreeze";
        public const int COOLDOWN = 3;
        public const int RADIUS = 1;

        public override string Name => "Drying Breeze";
        public override string MutationType => "Mental";
        public override string DisplayName => "Drying Breeze";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(DisplayName, COMMAND,
                "Grimoire Spells", AbilityTargetingMode.SelfCentered, RADIUS);
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
            if (!Cast(zone, sourceCell))
                return true;

            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell)
        {
            if (zone == null || sourceCell == null || ParentEntity == null)
                return false;

            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y)) > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = cell.Objects.Count - 1; i >= 0; i--)
                    {
                        Entity entity = cell.Objects[i];
                        if (entity.HasEffect<WetEffect>())
                            entity.RemoveEffect<WetEffect>();
                    }
                }
            }

            MessageLog.Add(ParentEntity.GetDisplayName() + " exhales a drying breeze.");
            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
