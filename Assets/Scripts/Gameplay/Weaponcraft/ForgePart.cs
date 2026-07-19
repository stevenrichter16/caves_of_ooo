using System.Collections.Generic;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking a tinker's forge — the weaponcraft station.
    /// Per M3-L3 (Docs/CRAFTING-ALCHEMY-SYSTEM.md), forging, re-forging and
    /// quenching all happen where the metal is worked: the forge/temper
    /// commands gate on this adjacency the same way brewing gates on
    /// <see cref="AlchemyStillPart"/>. Same furniture shape as
    /// ChairPart/BedPart: a marker part on a PhysicalObject blueprint.
    ///
    /// The forge is also the crafting VERB surface: it declares Forge /
    /// Re-forge / Quench rows on the look-mode world-action menu and, on
    /// selection, resolves the player's set-aside items
    /// (<see cref="CraftingMarkPart"/>) into command executions. Selection-
    /// count problems reject legibly here; everything else (gating,
    /// atomicity, stat math) lives in the commands and services.
    /// </summary>
    public class ForgePart : Part
    {
        public override string Name => "Forge";

        /// <summary>
        /// EntityFactory for weapon/component creation. Wired at bootstrap
        /// (mirrors CorpsePart.Factory); when unwired the forge rows degrade
        /// to a message instead of crashing mid-event.
        /// </summary>
        public static EntityFactory Factory;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    actions.AddAction("Forge", "forge set-aside components", "ForgeWeapon", 'f', 20);
                    actions.AddAction("ForgeBatch", "forge a full batch", "ForgeWeaponBatch", 'F', 19);
                    actions.AddAction("Reforge", "re-forge set-aside weapon", "ReforgeWeapon", 'r', 18);
                    actions.AddAction("Quench", "quench set-aside weapon in coating", "QuenchWeapon", 'q', 17);
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "ForgeWeapon" && command != "ForgeWeaponBatch"
                    && command != "ReforgeWeapon" && command != "QuenchWeapon")
                {
                    return true;
                }

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null)
                    return true;

                var zone = e.GetParameter<Zone>("Zone");
                e.Handled = true;

                switch (command)
                {
                    case "ForgeWeapon":
                        HandleForge(actor, zone, batch: false);
                        break;
                    case "ForgeWeaponBatch":
                        HandleForge(actor, zone, batch: true);
                        break;
                    case "ReforgeWeapon":
                        HandleReforge(actor, zone);
                        break;
                    case "QuenchWeapon":
                        HandleQuench(actor, zone);
                        break;
                }

                return false;
            }

            return true;
        }

        private static void HandleForge(Entity actor, Zone zone, bool batch)
        {
            if (Factory == null)
            {
                MessageLog.Add("The forge is cold. (Forging is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);
            if (!TrySelectComponents(marked, out Entity blade, out Entity haft, out Entity binding))
                return;

            int count = 1;
            if (batch)
            {
                count = WeaponForgingService.GetMaxBatchCount(blade, haft, binding);
                if (count < 1)
                    count = 1;
            }

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, Factory, count), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        private static void HandleReforge(Entity actor, Zone zone)
        {
            if (Factory == null)
            {
                MessageLog.Add("The forge is cold. (Forging is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);
            if (marked.Weapons.Count != 1)
            {
                MessageLog.Add("Set aside exactly one weapon to re-forge. (Set aside: "
                    + marked.Weapons.Count + " weapons.)");
                return;
            }

            if (marked.Components.Count != 1)
            {
                MessageLog.Add("Set aside exactly one replacement component to re-forge with. (Set aside: "
                    + marked.Components.Count + " components.)");
                return;
            }

            var result = InventorySystem.ExecuteCommand(
                new ReforgeWeaponCommand(marked.Weapons[0], marked.Components[0], Factory), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        private static void HandleQuench(Entity actor, Zone zone)
        {
            var marked = CraftingMarkPart.CollectMarked(actor);
            if (marked.Weapons.Count != 1)
            {
                MessageLog.Add("Set aside exactly one weapon to quench. (Set aside: "
                    + marked.Weapons.Count + " weapons.)");
                return;
            }

            if (marked.Coatings.Count != 1)
            {
                MessageLog.Add("Set aside one brewed coating to quench in. (Set aside: "
                    + marked.Coatings.Count + " coatings.)");
                return;
            }

            var result = InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(marked.Weapons[0], marked.Coatings[0]), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        /// <summary>
        /// True when <paramref name="actor"/> stands on or orthogonally/
        /// diagonally adjacent to (3×3 box) a cell containing an entity
        /// with a ForgePart. False when the zone is null or the actor
        /// isn't placed in it.
        /// </summary>
        public static bool IsNearForge(Entity actor, Zone zone)
        {
            if (actor == null || zone == null)
                return false;

            (int x, int y) = zone.GetEntityPosition(actor);
            if (x < 0 || y < 0)
                return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Cell cell = zone.GetCell(x + dx, y + dy);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity obj = cell.Objects[i];
                        if (obj != null && obj.HasPart<ForgePart>())
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resolve the marked components into exactly one blade + haft +
        /// binding; on any other shape, report what IS set aside (naming
        /// each slot) and return false with nothing consumed.
        /// </summary>
        private static bool TrySelectComponents(CraftingMarkPart.MarkedSelection marked,
            out Entity blade, out Entity haft, out Entity binding)
        {
            blade = null;
            haft = null;
            binding = null;
            int blades = 0, hafts = 0, bindings = 0;

            List<Entity> components = marked.Components;
            for (int i = 0; i < components.Count; i++)
            {
                var part = components[i].GetPart<WeaponComponentPart>();
                if (part == null)
                    continue;

                if (part.Slot == "Blade") { blade = components[i]; blades++; }
                else if (part.Slot == "Haft") { haft = components[i]; hafts++; }
                else if (part.Slot == "Binding") { binding = components[i]; bindings++; }
            }

            if (blades != 1 || hafts != 1 || bindings != 1)
            {
                MessageLog.Add("Forging needs exactly one blade, one haft, and one binding set aside. (Set aside: "
                    + blades + " blade(s), " + hafts + " haft(s), " + bindings + " binding(s).)");
                return false;
            }

            return true;
        }
    }
}
