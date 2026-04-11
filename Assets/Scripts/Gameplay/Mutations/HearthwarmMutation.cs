using System;

namespace CavesOfOoo.Core
{
    public class HearthwarmMutation : BaseMutation
    {
        public const string COMMAND = "CommandHearthwarm";
        public const int COOLDOWN = 4;

        public override string Name => "Hearthwarm";
        public override string MutationType => "Mental";
        public override string DisplayName => "Hearthwarm";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Grimoire Spells",
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
            if (e.ID != COMMAND)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell targetCell = e.GetParameter<Cell>("TargetCell");
            if (!Cast(zone, targetCell))
                return true;

            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell targetCell)
        {
            if (zone == null || targetCell == null || ParentEntity == null)
                return false;

            bool hasThermalTarget = false;
            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                if (targetCell.Objects[i].HasPart<ThermalPart>())
                {
                    hasThermalTarget = true;
                    break;
                }
            }
            if (!hasThermalTarget)
                return false;

            ParentEntity.ApplyEffect(new HearthAuraEffect(targetCell.X, targetCell.Y, duration: 3, joulesPerPulse: 60f),
                ParentEntity,
                zone);
            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
