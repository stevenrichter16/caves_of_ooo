using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Manages stack quantity for items that can stack (torches, arrows, etc.).
    /// Mirrors Qud's Stacker part. Items stack if they share the same BlueprintName
    /// and compatible mutable payloads, and both have a StackerPart.
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
        /// Items stack if both have StackerPart, share the same
        /// BlueprintName, look identical, AND preserve the same crafted payload.
        ///
        /// The display-name check exists because identity items share a
        /// blueprint: every brew is "BrewedTonic" and every forged weapon
        /// is "ForgedWeapon", with the actual identity (effects, assembly)
        /// living in per-instance parts and the display name. Blueprint-only
        /// stacking merged a fresh "burning and wet flask" into an older
        /// "burning coating" stack — the new item vanished and its payload
        /// was silently replaced by the old one (live-play bug, 2026-07-19;
        /// pinned by BrewStackingRegressionTests).
        ///
        /// <para><b>Charged items never stack at all.</b> Same bug class,
        /// found again by a hypothesis-driven audit: a grimoire's ink is
        /// per-book, and display name does not encode it. A dry book and
        /// a fresh one look identical, so picking up the fresh one merged
        /// it into the dry stack and its ten charges vanished — leaving
        /// the player holding "two" books and unable to cast. Even when
        /// the counts happen to match, one shared counter across a stack
        /// of N would sell N books' worth of ink for one book's worth of
        /// casts. Individually-consumed state and stacking are simply
        /// incompatible.</para>
        /// </summary>
        public bool CanStackWith(Entity other)
        {
            if (other == null || other == ParentEntity) return false;
            var otherStacker = other.GetPart<StackerPart>();
            if (otherStacker == null) return false;
            if (string.IsNullOrEmpty(ParentEntity.BlueprintName)) return false;
            if (ParentEntity.BlueprintName != other.BlueprintName) return false;

            // Per-item consumable state cannot survive a merge.
            if (ParentEntity.GetPart<GrimoireChargePart>() != null
                || other.GetPart<GrimoireChargePart>() != null)
                return false;

            string mine = ParentEntity.GetPart<RenderPart>()?.DisplayName;
            string theirs = other.GetPart<RenderPart>()?.DisplayName;
            if (mine != theirs) return false;
            if (StackPayloadIdentity.Same(ParentEntity, other)) return true;
            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("event"))
                CavesOfOoo.Diagnostics.Diag.Record("event", "StackPayloadMismatch", actor: ParentEntity, target: other,
                    payload: new { blueprint = ParentEntity.BlueprintName, reason = "different_crafted_payload" });
            return false;
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
            return HandlingService.GetWeight(ParentEntity) * StackCount;
        }
    }
}
