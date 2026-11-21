namespace CavesOfOoo.Core
{
    /// <summary>
    /// A stack of identical garments in the pack (armor of one kind stacks) can still be treated: the treatment goes on one unit, which is split off the stack, treated, and put back as its own
    /// single item (an enhanced garment never stacks with a plain one, so it stays apart). A worn garment is already a single and a stack that refuses to split (a protected input) is not offered.
    /// </summary>
    internal static class GarmentStacks
    {
        internal static int Count(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 1;

        /// <summary>A single garment, or a stack in the pack that could give up one unit.</summary>
        internal static bool WholeOrSplittable(Entity actor, Entity item)
        {
            var stack = item?.GetPart<StackerPart>(); if (stack == null || stack.StackCount == 1) return true;
            var pack = actor?.GetPart<InventoryPart>();
            return stack.StackCount > 1 && stack.ParentEntity == item && stack.CanSplit() && pack != null && pack.Objects.Contains(item) && pack.EquippedItems?.ContainsValue(item) != true;
        }

        /// <summary>" (1 of 3)" for a stack, empty for a single.</summary>
        internal static string Suffix(Entity item) => Count(item) > 1 ? " (1 of " + Count(item) + ")" : "";

        internal static bool IsStack(Entity item) => Count(item) > 1;
    }
}
