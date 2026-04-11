using System;

namespace CavesOfOoo.Core
{
    public class KindleFlameMutation : BaseMutation
    {
        public const string COMMAND = "CommandKindleFlame";
        public const int COOLDOWN = 2;

        public override string Name => "Kindle Flame";
        public override string MutationType => "Mental";
        public override string DisplayName => "Kindle Flame";

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

            bool affectedAny = false;
            for (int i = targetCell.Objects.Count - 1; i >= 0; i--)
            {
                Entity entity = targetCell.Objects[i];
                if (entity.HasTag("Creature"))
                    continue;

                ThermalPart thermal = entity.GetPart<ThermalPart>();
                if (thermal == null || thermal.FlameTemperature >= 250f)
                    continue;

                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)50f);
                heatEvent.SetParameter("Radiant", (object)false);
                heatEvent.SetParameter("Source", (object)ParentEntity);
                heatEvent.SetParameter("Zone", (object)zone);
                entity.FireEvent(heatEvent);
                heatEvent.Release();
                affectedAny = true;
            }

            if (affectedAny)
                CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);

            return affectedAny;
        }
    }
}
