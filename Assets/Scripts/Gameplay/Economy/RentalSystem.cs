using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Weapon rental: a renter pays Ink to a lessor NPC for temporary
    /// possession of a weapon. The weapon is flagged with a
    /// <see cref="RentalPart"/> until <see cref="TryReturn"/> hands it
    /// back at the same lessor (matched by blueprint name) for a
    /// partial Ink refund.
    ///
    /// Mirrors <see cref="TradeSystem"/>'s shape: Ink is an IntProperty
    /// on the entity (<see cref="INK_PROP"/>), and rental cost reuses
    /// <see cref="TradeSystem.GetBuyPrice"/> so faction standing and Ego
    /// modify rental price the same way they modify a normal purchase.
    ///
    /// Ink is *separate* from Drams; the player can have one without
    /// the other. Drams are gameplay-earned, Ink is content-granted
    /// (quest reward, dialogue gift). See Docs/WEAPON-RENTAL-SYSTEM.md.
    /// </summary>
    public static class RentalSystem
    {
        public const string INK_PROP = "Ink";

        /// <summary>Rental cost = ceil(BuyPrice * RENTAL_FRACTION).</summary>
        public const double RENTAL_FRACTION = 0.25;

        /// <summary>Refund = floor(InkPaid * REFUND_FRACTION) on return.</summary>
        public const double REFUND_FRACTION = 0.50;

        // ── Ink wallet ────────────────────────────────────────────────

        public static int GetInk(Entity entity)
        {
            if (entity == null) return 0;
            return entity.GetIntProperty(INK_PROP, 0);
        }

        /// <summary>Set Ink, clamped at 0. Mirrors <see cref="TradeSystem.SetDrams"/>.</summary>
        public static void SetInk(Entity entity, int amount)
        {
            if (entity == null) return;
            entity.SetIntProperty(INK_PROP, Math.Max(0, amount));
        }

        /// <summary>Add (or subtract) Ink. Result is clamped at 0.</summary>
        public static void AddInk(Entity entity, int delta)
        {
            if (entity == null) return;
            SetInk(entity, GetInk(entity) + delta);
        }

        // ── Predicates ────────────────────────────────────────────────

        /// <summary>An item is rentable if it carries the "Rentable"
        /// tag and a <see cref="CommercePart"/> (so a buy price exists
        /// to scale rental cost from).</summary>
        public static bool IsRentable(Entity item)
        {
            if (item == null) return false;
            if (!item.HasTag("Rentable")) return false;
            return item.GetPart<CommercePart>() != null;
        }

        public static bool IsRented(Entity item)
        {
            return item != null && item.GetPart<RentalPart>() != null;
        }

        // ── Pricing ───────────────────────────────────────────────────

        /// <summary>
        /// Rental cost in Ink. Computed from the renter's perspective
        /// against the lessor (faction standing applies). Returns 0 for
        /// items without a CommercePart so callers can use a single
        /// branchless `if (cost &lt;= 0) ...` guard.
        /// </summary>
        public static int GetRentalCost(Entity item, Entity renter, Entity lessor)
        {
            int buyPrice = TradeSystem.GetBuyPrice(item,
                TradeSystem.GetTradePerformance(renter), lessor);
            if (buyPrice <= 0) return 0;
            return (int)Math.Ceiling(buyPrice * RENTAL_FRACTION);
        }

        // ── Rent / return ─────────────────────────────────────────────

        /// <summary>
        /// Renter pays Ink to lessor; item moves from lessor's inventory
        /// to renter's, and a <see cref="RentalPart"/> records the
        /// transaction. Returns false (with a player-visible MessageLog
        /// reason) on any precondition failure; on failure no state is
        /// mutated.
        ///
        /// Preconditions checked in order:
        ///   1. non-null actors and item, both have InventoryPart
        ///   2. item is rentable (tag + CommercePart)
        ///   3. item is not already rented
        ///   4. renter has enough Ink
        ///   5. inventory transfer succeeds (weight check on renter side)
        /// </summary>
        public static bool TryRent(Entity renter, Entity lessor, Entity item)
        {
            if (renter == null || lessor == null || item == null) return false;

            var lessorInv = lessor.GetPart<InventoryPart>();
            var renterInv = renter.GetPart<InventoryPart>();
            if (lessorInv == null || renterInv == null) return false;

            if (!IsRentable(item))
            {
                MessageLog.Add($"{item.GetDisplayName()} isn't for rent.");
                return false;
            }

            if (IsRented(item))
            {
                // Defensive: stock should never contain rented items,
                // but if a save-game corruption surfaces one, refuse
                // rather than double-charge.
                MessageLog.Add($"{item.GetDisplayName()} is already rented out.");
                return false;
            }

            int cost = GetRentalCost(item, renter, lessor);
            int renterInk = GetInk(renter);
            if (renterInk < cost)
            {
                MessageLog.Add($"You need {cost} ink to rent that. You have {renterInk}.");
                return false;
            }

            // Mirrors TradeSystem.BuyFromTrader: roll back if the
            // renter-side AddObject refuses (overweight). Without the
            // rollback the item would vanish.
            if (!lessorInv.RemoveObject(item)) return false;
            if (!renterInv.AddObject(item))
            {
                lessorInv.AddObject(item);
                MessageLog.Add($"You can't carry {item.GetDisplayName()}: too heavy!");
                return false;
            }

            SetInk(renter, renterInk - cost);

            var rental = new RentalPart
            {
                InkPaid = cost,
                LessorBlueprintName = lessor.BlueprintName,
            };
            item.AddPart(rental);

            MessageLog.Add($"You rent {item.GetDisplayName()} for {cost} ink.");

            if (Diag.IsChannelEnabled("trade"))
            {
                Diag.Record(
                    category: "trade", kind: "Rented",
                    actor: renter, target: lessor,
                    payload: new
                    {
                        itemName = item.GetDisplayName(),
                        itemId = item.ID,
                        cost,
                        inkAfter = GetInk(renter),
                    });
            }
            return true;
        }

        /// <summary>
        /// Renter hands back a rental to the same lessor. Refund is
        /// <c>floor(InkPaid * REFUND_FRACTION)</c>. Returns false (no
        /// state change) if the item isn't a rental or if the lessor's
        /// blueprint name doesn't match the one recorded on the
        /// <see cref="RentalPart"/>.
        ///
        /// Mirrors <see cref="TryRent"/>'s precondition order so a
        /// reader scanning both methods sees the same shape.
        /// </summary>
        public static bool TryReturn(Entity renter, Entity lessor, Entity rentalItem)
        {
            if (renter == null || lessor == null || rentalItem == null) return false;

            var lessorInv = lessor.GetPart<InventoryPart>();
            var renterInv = renter.GetPart<InventoryPart>();
            if (lessorInv == null || renterInv == null) return false;

            var rental = rentalItem.GetPart<RentalPart>();
            if (rental == null)
            {
                MessageLog.Add($"{rentalItem.GetDisplayName()} isn't a rental.");
                return false;
            }

            if (rental.LessorBlueprintName != lessor.BlueprintName)
            {
                MessageLog.Add($"That isn't {lessor.GetDisplayName()}'s to take back.");
                return false;
            }

            int refund = (int)Math.Floor(rental.InkPaid * REFUND_FRACTION);

            // WRS cold-eye Q1 (symmetry): mirror TryRent's rollback
            // shape exactly. The previous order — RemoveObject → strip
            // RentalPart → unconditional AddObject — had two latent
            // bugs: (a) if the lessor's inventory rejected the add
            // (over weight), the item was orphaned; (b) RentalPart was
            // stripped before the transfer was confirmed, so a failed
            // add left the player holding an untagged-but-unowned
            // weapon.
            if (!renterInv.RemoveObject(rentalItem)) return false;
            if (!lessorInv.AddObject(rentalItem))
            {
                renterInv.AddObject(rentalItem);
                MessageLog.Add($"{lessor.GetDisplayName()} can't accept that right now.");
                return false;
            }
            rentalItem.RemovePart(rental);

            AddInk(renter, refund);

            MessageLog.Add($"You return {rentalItem.GetDisplayName()} and recover {refund} ink.");

            if (Diag.IsChannelEnabled("trade"))
            {
                Diag.Record(
                    category: "trade", kind: "Returned",
                    actor: renter, target: lessor,
                    payload: new
                    {
                        itemName = rentalItem.GetDisplayName(),
                        itemId = rentalItem.ID,
                        refund,
                        inkAfter = GetInk(renter),
                    });
            }
            return true;
        }
    }
}
