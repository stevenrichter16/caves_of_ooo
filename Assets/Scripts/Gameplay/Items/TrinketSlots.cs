using System;
using System.Globalization;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Adds dedicated equipment positions without creating physical limbs.
    /// The body graph owns occupancy; this saved property retains earned capacity
    /// if an anatomy is replaced. Grants call EnsureMinimum with their target
    /// capacity, so repeating a grant or loading a save cannot duplicate slots.
    /// </summary>
    public static class TrinketSlots
    {
        public const string SlotType = "Trinket";
        public const string CapacityProperty = "TrinketSlotCapacity";
        public const int DefaultCapacity = 2;
        // An allocation bound, not a progression or balance cap.
        public const int MaximumCapacity = 16;

        public static int GetCapacity(Entity actor) => actor?.GetPart<Body>()?.CountParts(SlotType) ?? 0;

        /// <summary>New and older saved players receive the same starting slots.
        /// Existing slots, equipment and slot IDs remain authoritative.</summary>
        public static void InitializePlayer(Entity actor)
        {
            if (actor == null || !actor.HasTag("Player")) return;
            int requested = Math.Max(DefaultCapacity,
                Math.Min(MaximumCapacity, actor.GetIntProperty(CapacityProperty, DefaultCapacity)));
            EnsureMinimum(actor, requested);
        }

        /// <summary>
        /// Ensure at least this many slots; never shrink capacity or replace worn
        /// items. Can explicitly grant slots to other body-bearing actors too.
        /// Returns false for invalid bounds or a missing/foreign body, without
        /// changing the actor. After replacing an anatomy, InitializePlayer
        /// restores the player's saved capacity onto the new body.
        /// </summary>
        public static bool EnsureMinimum(Entity actor, int minimum)
        {
            if (actor == null || minimum < 0 || minimum > MaximumCapacity) return false;
            var body = actor.GetPart<Body>();
            var root = body?.GetBody();
            if (body == null || body.ParentEntity != actor || root == null) return false;

            int count = body.CountParts(SlotType);
            while (count < minimum)
            {
                var slot = AnatomyFactory.CreatePart(SlotType);
                slot.Name = "trinket " + (count + 1).ToString(CultureInfo.InvariantCulture);
                slot.Description = "Trinket " + (count + 1).ToString(CultureInfo.InvariantCulture);
                slot.Category = root.Category;
                slot.Native = true;
                root.AddPart(slot);
                count++;
            }

            // Construction and load repair are not gameplay grants: do not replay
            // property-change observers while restoring a saved body graph.
            int recorded = Math.Max(0, Math.Min(MaximumCapacity,
                actor.GetIntProperty(CapacityProperty, 0)));
            actor.IntProperties[CapacityProperty] = Math.Max(recorded, Math.Min(MaximumCapacity, count));
            return true;
        }
    }
}
