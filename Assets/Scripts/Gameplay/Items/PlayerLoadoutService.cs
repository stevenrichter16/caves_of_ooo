using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core
{
    /// <summary>Authority for immutable saved equipment recipes and the two field
    /// presets. Saving remembers possessions; applying never creates or retrieves items.</summary>
    public static class PlayerLoadoutService
    {
        public static PlayerLoadoutsPart GetState(Entity actor, bool create = false)
        {
            var state = actor?.GetPart<PlayerLoadoutsPart>();
            if (state == null && create && actor != null)
            {
                state = new PlayerLoadoutsPart();
                actor.AddPart(state);
            }
            return state;
        }

        public static SavedEquipmentLoadout GetPrepared(Entity actor, int slot)
        {
            var state = GetState(actor);
            string id = slot == 0 ? state?.PreparedA : slot == 1 ? state?.PreparedB : null;
            return Find(state, id);
        }

        public static bool CanSaveCurrent(Entity actor, out string reason)
        {
            if (!Ready(actor, out reason)) return false;
            if (GetState(actor)?.Applying == true) return Refuse("A loadout change is already in progress.", out reason);
            return ReadOwned(actor, out _, out reason);
        }

        public static bool SaveCurrent(Entity actor, string name, out SavedEquipmentLoadout saved, out string reason)
        {
            saved = null;
            if (!CanSaveCurrent(actor, out reason)) return false;
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 40 || name.Any(char.IsControl))
                return Refuse("Choose a loadout name of 1 to 40 characters.", out reason);
            var parts = actor.GetPart<Body>().GetParts().Where(p => !p.Abstract).ToList();
            saved = new SavedEquipmentLoadout { ID = Guid.NewGuid().ToString("N"), Name = name };
            foreach (var part in parts)
                saved.Slots.Add(new SavedLoadoutSlot { BodyPartID = part.ID, Type = part.Type, Name = part.GetDisplayName() });
            foreach (var item in parts.Select(p => p._Equipped).Where(i => i != null).Distinct())
            {
                var slots = parts.Where(p => p._Equipped == item).OrderByDescending(p => p.FirstSlotForEquipped).ToList();
                var entry = new SavedLoadoutItem { ItemID = item.ID, ItemName = item.GetDisplayName() };
                foreach (var part in slots) { entry.BodyPartIDs.Add(part.ID); entry.BodyPartTypes.Add(part.Type); }
                saved.Items.Add(entry);
            }
            var state = GetState(actor, true);
            state.Saved.Add(saved);
            // This allowance is lifetime bookkeeping, not the number of records.
            // Deleting a preset cannot manufacture another field preparation.
            if (state.InitialPreparations < 2)
            {
                if (string.IsNullOrEmpty(state.PreparedA)) state.PreparedA = saved.ID;
                else if (string.IsNullOrEmpty(state.PreparedB)) state.PreparedB = saved.ID;
                state.InitialPreparations++;
            }
            reason = null;
            return true;
        }

        public static bool Delete(Entity actor, string id, Entity keeper, out string reason)
        {
            if (!Ready(actor, out reason)) return false;
            var state = GetState(actor);
            var saved = Find(state, id);
            if (saved == null) return Refuse("That saved loadout is no longer available.", out reason);
            if (state.Applying) return Refuse("A loadout change is already in progress.", out reason);
            bool prepared = state.PreparedA == id || state.PreparedB == id;
            if (prepared && !LoadoutKeeperPart.CanServe(actor, keeper, out reason))
                return false;
            state.Saved.Remove(saved);
            if (state.PreparedA == id) state.PreparedA = "";
            if (state.PreparedB == id) state.PreparedB = "";
            if (prepared) state.InitialPreparations = 2;
            if (state.LastAppliedID == id) state.LastAppliedID = "";
            reason = null;
            return true;
        }

        public static bool Prepare(Entity actor, string id, int slot, Entity keeper, out string reason)
        {
            if (!Ready(actor, out reason)) return false;
            if (slot < 0 || slot > 1) return Refuse("Only two loadouts can be prepared.", out reason);
            var state = GetState(actor);
            if (Find(state, id) == null) return Refuse("That saved loadout is no longer available.", out reason);
            if (state.Applying) return Refuse("A loadout change is already in progress.", out reason);
            if (!LoadoutKeeperPart.CanServe(actor, keeper, out reason)) return false;
            string other = slot == 0 ? state.PreparedB : state.PreparedA;
            if (other == id) return Refuse("That loadout is already prepared in the other slot.", out reason);
            if (slot == 0) state.PreparedA = id; else state.PreparedB = id;
            // Once a specialist has changed the pair, onboarding must not fill a
            // subsequently emptied slot without another specialist visit.
            state.InitialPreparations = 2;
            reason = null;
            return true;
        }

        public static bool CanApplyPrepared(Entity actor, int slot, out string reason)
            => TryResolve(actor, slot, out _, out _, out reason);

        public static InventoryCommandResult ApplyPrepared(Entity actor, int slot, Zone zone = null)
            => InventorySystem.ExecuteCommand(new ApplyPlayerLoadoutCommand(slot), actor, zone ?? actor?.SpatialZone);

        public static bool MatchesCurrent(Entity actor, SavedEquipmentLoadout saved)
        {
            if (!Ready(actor, out _) || saved?.Items == null || saved.Slots == null) return false;
            if (!ReadOwned(actor, out _, out _)) return false;
            var parts = actor.GetPart<Body>().GetParts().Where(p => !p.Abstract).ToList();
            if (!SlotsMatch(saved, parts)) return false;
            var desired = new Dictionary<int, string>();
            foreach (var item in saved.Items)
            {
                if (item?.BodyPartIDs == null || string.IsNullOrEmpty(item.ItemID)) return false;
                foreach (int id in item.BodyPartIDs)
                    if (!desired.TryAdd(id, item.ItemID)) return false;
            }
            foreach (var part in parts)
            {
                desired.TryGetValue(part.ID, out string expected);
                if ((part._Equipped?.ID ?? "") != (expected ?? "")) return false;
            }
            foreach (var item in saved.Items)
                if (item.BodyPartIDs.Count == 0 || !parts.Any(p => p.ID == item.BodyPartIDs[0]
                    && p._Equipped?.ID == item.ItemID && p.FirstSlotForEquipped)) return false;
            return true;
        }

        internal sealed class ResolvedItem
        {
            internal Entity Item;
            internal List<BodyPart> Parts;
        }

        internal static bool TryResolve(Entity actor, int slot, out SavedEquipmentLoadout saved,
            out List<ResolvedItem> items, out string reason)
        {
            saved = null; items = null;
            if (!Ready(actor, out reason)) return false;
            if (slot < 0 || slot > 1) return Refuse("Only prepared loadouts A and B can be used here.", out reason);
            saved = GetPrepared(actor, slot);
            if (saved == null) return Refuse("No loadout is prepared in that slot.", out reason);
            if (!ReadOwned(actor, out var owned, out reason)) return false;
            var parts = actor.GetPart<Body>().GetParts().Where(p => !p.Abstract).ToList();
            if (!SlotsMatch(saved, parts)) return Refuse("Your equipment slots have changed. Save and prepare a new loadout.", out reason);
            if (saved.Items == null) return Refuse("This loadout has invalid equipment records.", out reason);
            var usedItems = new HashSet<string>(StringComparer.Ordinal);
            var usedSlots = new HashSet<int>();
            items = new List<ResolvedItem>();
            foreach (var entry in saved.Items)
            {
                if (entry == null || string.IsNullOrEmpty(entry.ItemID) || !usedItems.Add(entry.ItemID)
                    || entry.BodyPartIDs == null || entry.BodyPartTypes == null || entry.BodyPartIDs.Count == 0
                    || entry.BodyPartIDs.Count != entry.BodyPartTypes.Count)
                    return Refuse("This loadout has invalid equipment records.", out reason);
                if (!owned.TryGetValue(entry.ItemID, out var item))
                    return Refuse("Missing item: " + (entry.ItemName ?? "saved equipment") + ". Carry the original item to use this loadout.", out reason);
                if (item.GetPart<StackerPart>()?.StackCount > 1)
                    return Refuse("Separate " + item.GetDisplayName() + " from its stack before using this loadout.", out reason);
                var selected = new List<BodyPart>();
                for (int i = 0; i < entry.BodyPartIDs.Count; i++)
                {
                    int id = entry.BodyPartIDs[i];
                    var part = parts.SingleOrDefault(p => p.ID == id);
                    if (part == null || part.Type != entry.BodyPartTypes[i] || !usedSlots.Add(id))
                        return Refuse("A saved equipment slot is unavailable or duplicated.", out reason);
                    selected.Add(part);
                }
                var plan = new EquipPlanner().Build(actor, item, selected[0], selected);
                if (!plan.IsValid) return Refuse(plan.FailureReason, out reason);
                items.Add(new ResolvedItem { Item = item, Parts = selected });
            }
            if (actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true && !MatchesCurrent(actor, saved))
                return Refuse("You cannot change loadouts while unable to act.", out reason);
            reason = null;
            return true;
        }

        internal static bool Ready(Entity actor, out string reason)
        {
            if (actor == null || !actor.HasTag("Player") || CombatSystem.IsDeathHandled(actor)
                || actor.GetStatValue("Hitpoints", 1) <= 0)
                return Refuse("A living player is required to manage loadouts.", out reason);
            var inventory = actor.GetPart<InventoryPart>(); var body = actor.GetPart<Body>();
            if (inventory?.ParentEntity != actor || body?.ParentEntity != actor || inventory.Objects == null || inventory.EquippedItems == null)
                return Refuse("Your equipment is unavailable.", out reason);
            reason = null;
            return true;
        }

        static bool SlotsMatch(SavedEquipmentLoadout saved, List<BodyPart> parts)
        {
            // Gained slots are empty in an older whole-equipment recipe. Lost or
            // retagged saved slots still require a new recipe rather than guessing.
            if (saved?.Slots == null || saved.Slots.Count > parts.Count
                || parts.Select(p => p.ID).Distinct().Count() != parts.Count) return false;
            var used = new HashSet<int>();
            return saved.Slots.All(s => s != null && used.Add(s.BodyPartID)
                && parts.Any(p => p.ID == s.BodyPartID && p.Type == s.Type));
        }

        static bool ReadOwned(Entity actor, out Dictionary<string, Entity> owned, out string reason)
        {
            owned = new Dictionary<string, Entity>(StringComparer.Ordinal);
            var pack = actor.GetPart<InventoryPart>(); var parts = actor.GetPart<Body>().GetParts();
            foreach (var item in pack.Objects.Concat(pack.EquippedItems.Values).Distinct())
            {
                var physics = item?.GetPart<PhysicsPart>();
                if (item == null || string.IsNullOrEmpty(item.ID) || owned.ContainsKey(item.ID)
                    || physics?.ParentEntity != item || item.SpatialZone != null || CombatSystem.IsDeathHandled(item)
                    || item.GetStatValue("Hitpoints", 1) <= 0
                    || item.GetPart<StackerPart>()?.StackCount <= 0)
                    return Refuse("Your item ownership changed; reopen the loadout menu.", out reason);
                bool carried = pack.Objects.Contains(item), worn = pack.EquippedItems.ContainsValue(item);
                var occupied = parts.Where(p => p._Equipped == item).ToList();
                if (carried == worn || carried && (physics.InInventory != actor || physics.Equipped != null || occupied.Count != 0)
                    || worn && (physics.Equipped != actor || physics.InInventory != null || occupied.Count == 0
                        || occupied.Any(p => p.Abstract || !pack.EquippedItems.TryGetValue(p.ID.ToString(), out var cached) || cached != item)
                        || occupied.Count(p => p.FirstSlotForEquipped) != 1))
                    return Refuse("Your equipment ownership is inconsistent; reopen the loadout menu.", out reason);
                owned.Add(item.ID, item);
            }
            if (parts.Any(p => p._Equipped != null && !pack.EquippedItems.ContainsValue(p._Equipped))
                || pack.EquippedItems.Any(pair => !parts.Any(p => p.ID.ToString() == pair.Key && p._Equipped == pair.Value)))
                return Refuse("Your equipment slots are inconsistent; reopen the loadout menu.", out reason);
            reason = null;
            return true;
        }

        static SavedEquipmentLoadout Find(PlayerLoadoutsPart state, string id) => string.IsNullOrEmpty(id)
            ? null : state?.Saved?.FirstOrDefault(s => s != null && s.ID == id);
        static bool Refuse(string message, out string reason) { reason = message; return false; }
    }
}
