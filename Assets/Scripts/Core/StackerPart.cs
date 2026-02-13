using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Manages stack quantity for items that can stack (torches, arrows, etc.).
    /// Mirrors Qud's Stacker part. Items stack if they share the same BlueprintName
    /// and both have a StackerPart.
    ///
    /// Weight is per-unit on PhysicsPart. Total weight = Physics.Weight * StackCount.
    /// </summary>
    public class StackerPart : Part
    {
        public override string Name => "Stacker";

        /// <summary>
        /// Number of items in this stack.
        /// </summary>
        public int StackCount = 1;

        /// <summary>
        /// Maximum items per stack.
        /// </summary>
        public int MaxStack = 99;

        /// <summary>
        /// Can this item stack with another entity?
        /// Items stack if both have StackerPart and share the same BlueprintName.
        /// </summary>
        public bool CanStackWith(Entity other)
        {
            if (other == null || other == ParentEntity) return false;
            var otherStacker = other.GetPart<StackerPart>();
            if (otherStacker == null) return false;
            if (string.IsNullOrEmpty(ParentEntity.BlueprintName)) return false;
            return ParentEntity.BlueprintName == other.BlueprintName;
        }

        /// <summary>
        /// Merge another stackable entity into this one.
        /// Returns the number actually merged (may be less if max reached).
        /// </summary>
        public int MergeFrom(Entity other)
        {
            var otherStacker = other.GetPart<StackerPart>();
            if (otherStacker == null) return 0;

            int canAccept = MaxStack - StackCount;
            int toMerge = Math.Min(canAccept, otherStacker.StackCount);
            if (toMerge <= 0) return 0;

            StackCount += toMerge;
            otherStacker.StackCount -= toMerge;
            return toMerge;
        }

        /// <summary>
        /// Split off count items into a new entity.
        /// Returns the split-off entity, or null if can't split.
        /// </summary>
        public Entity SplitStack(int count)
        {
            if (count <= 0 || count >= StackCount) return null;

            StackCount -= count;
            var clone = ParentEntity.CloneForStack();
            clone.GetPart<StackerPart>().StackCount = count;
            return clone;
        }

        /// <summary>
        /// Split off one item. If this is the last item, returns ParentEntity itself.
        /// Otherwise creates a clone with count 1 and decrements this stack.
        /// </summary>
        public Entity RemoveOne()
        {
            if (StackCount <= 1) return ParentEntity;

            StackCount--;
            var clone = ParentEntity.CloneForStack();
            clone.GetPart<StackerPart>().StackCount = 1;
            return clone;
        }

        /// <summary>
        /// Total weight of this stack (per-unit weight * count).
        /// </summary>
        public int GetTotalWeight()
        {
            var physics = ParentEntity?.GetPart<PhysicsPart>();
            return (physics?.Weight ?? 0) * StackCount;
        }
    }
}
