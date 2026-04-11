using System;

namespace CavesOfOoo.Core
{
    public class WardGleamMutation : BaseMutation
    {
        public const string COMMAND = "CommandWardGleam";
        public const int COOLDOWN = 15;

        public override string Name => "Ward Gleam";
        public override string MutationType => "Mental";
        public override string DisplayName => "Ward Gleam";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Grimoire Spells",
                AbilityTargetingMode.SelfCentered,
                0);
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

            if (!Cast())
                return true;

            e.Handled = true;
            return false;
        }

        public bool Cast()
        {
            if (ParentEntity == null)
                return false;

            InventoryPart inventory = ParentEntity.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            bool removedAny = false;
            var equipped = inventory.GetAllEquipped();
            for (int i = 0; i < equipped.Count; i++)
            {
                Entity item = equipped[i];
                bool removedFromItem = false;

                if (item.HasEffect<AcidicEffect>())
                {
                    item.RemoveEffect<AcidicEffect>();
                    removedFromItem = true;
                }

                if (item.HasEffect<CharredEffect>())
                {
                    item.RemoveEffect<CharredEffect>();
                    removedFromItem = true;
                }

                removedAny |= removedFromItem;
            }

            if (removedAny)
                CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);

            return removedAny;
        }
    }
}
