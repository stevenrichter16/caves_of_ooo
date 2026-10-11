namespace CavesOfOoo.Core
{
    /// <summary>Read-only, single-command proof for the inventory UI's self-use
    /// turn handoff. Capture immediately before dispatch, then check once with
    /// the command's committed success result. This neither consumes nor charges;
    /// it recognizes only Eat on food/field meals and ApplyTonic on tonics, and
    /// requires one unit to leave the same carried source. Do not retain across
    /// actions, menus, save/load or actor replacement.</summary>
    public readonly struct SelfConsumptionTurnProof
    {
        readonly Entity actor, item;
        readonly InventoryPart pack;
        readonly PhysicsPart physics;
        readonly StackerPart stack;
        readonly int quantity;

        SelfConsumptionTurnProof(Entity actor, Entity item)
        {
            this.actor = actor; this.item = item;
            pack = actor.GetPart<InventoryPart>();
            physics = item.GetPart<PhysicsPart>();
            stack = item.GetPart<StackerPart>();
            quantity = stack?.StackCount ?? 1;
        }

        public static SelfConsumptionTurnProof Capture(Entity actor, Entity item, string command)
        {
            bool supported = item != null && ((command == "Eat" && (item.HasPart<FoodPart>() || item.HasPart<FieldMealPart>()))
                || (command == "ApplyTonic" && item.HasPart<TonicPart>()));
            return supported && WorldResourceActions.Carried(actor, item, false)
                ? new SelfConsumptionTurnProof(actor, item) : default;
        }

        /// <summary>A handled refusal with unchanged quantity is free. Failed or
        /// rolled-back commands are always free, even if unrelated work changed
        /// the source. False is also the safe result for changed owner/part identity.</summary>
        public bool ShouldSpendTurn(bool commandSucceeded)
        {
            if (!commandSucceeded || quantity <= 0 || actor?.GetPart<InventoryPart>() != pack
                || pack?.ParentEntity != actor || item?.GetPart<PhysicsPart>() != physics
                || item.GetPart<StackerPart>() != stack || item.SpatialZone != null
                || physics?.ParentEntity != item || physics.Equipped != null
                || pack.EquippedItems.ContainsValue(item) || pack.FindEquippedBodyPart(item) != null)
                return false;
            if (quantity == 1)
                return !pack.Objects.Contains(item) && physics.InInventory == null
                    && (stack?.StackCount ?? 1) == 1;
            return WorldResourceActions.Carried(actor, item, false) && stack.StackCount == quantity - 1;
        }
    }
}
