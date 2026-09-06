using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// "Set aside for crafting" marker (M3-L3, Docs/CRAFTING-ALCHEMY-SYSTEM.md).
    /// The player toggles this on carried items via the inventory popup's
    /// "Set aside" row (ToggleCraftMarkCommand); the crafting stations read
    /// the marked set back through <see cref="CollectMarked"/> and feed it to
    /// the brew/forge/temper commands. State is a plain per-item marker Part -
    /// save-safe via reflection, no static selection buffers to go stale
    /// across Play sessions.
    /// </summary>
    public class CraftingMarkPart : Part
    {
        public override string Name => "CraftingMark";

        /// <summary>
        /// The marked items an actor carries, partitioned the way the
        /// stations consume them. Buckets are mutually exclusive; precedence
        /// follows specificity (a reagent is never also a component, etc.).
        /// </summary>
        public sealed class MarkedSelection
        {
            public List<Entity> Reagents = new List<Entity>();
            public List<Entity> Components = new List<Entity>();
            public List<Entity> Coatings = new List<Entity>();
            public List<Entity> Weapons = new List<Entity>();
        }

        /// <summary>
        /// True for the item kinds a crafting flow can consume: reagents
        /// (brew), weapon components (forge/re-forge), brew items (quench
        /// medium - form is validated at quench time, not here, so the
        /// rejection message stays specific), and melee weapons (quench /
        /// re-forge target).
        /// </summary>
        public static bool IsMarkable(Entity item)
        {
            if (item == null)
                return false;

            return item.HasPart<ReagentPart>()
                || item.HasPart<WeaponComponentPart>()
                || item.HasPart<BrewItemPart>()
                || item.HasPart<MeleeWeaponPart>();
        }

        public static bool IsMarked(Entity item)
        {
            return item != null && item.HasPart<CraftingMarkPart>();
        }

        /// <summary>
        /// Radio-selection group for the sectioned forge menu: at most ONE
        /// marked item per group. Weapon components are exclusive per slot
        /// ("Slot:Blade" etc.), coatings and weapons each form one group,
        /// and reagents return null - the brew mix is deliberately
        /// multi-select. Precedence mirrors <see cref="CollectMarked"/>.
        /// </summary>
        public static string ExclusiveGroupOf(Entity item)
        {
            if (item == null)
                return null;
            if (item.HasPart<ReagentPart>())
                return null;
            var component = item.GetPart<WeaponComponentPart>();
            if (component != null)
                return "Slot:" + component.Slot;
            if (item.HasPart<BrewItemPart>())
                return "Quench";
            if (item.HasPart<MeleeWeaponPart>())
                return "Weapon";
            return null;
        }

        /// <summary>Flip the mark; returns the NEW marked state.</summary>
        public static bool Toggle(Entity item)
        {
            if (item == null)
                return false;

            var existing = item.GetPart<CraftingMarkPart>();
            if (existing != null)
            {
                item.RemovePart(existing);
                return false;
            }

            item.AddPart(new CraftingMarkPart());
            return true;
        }

        /// <summary>
        /// Command prefix for the stations' in-menu toggle rows; the suffix
        /// is the target item's Entity.ID. InputHandler intercepts these,
        /// runs ToggleCraftMarkCommand, and reopens the menu so the station
        /// acts as a multi-select picker (live-playtest finding, M3-L3).
        /// </summary>
        public const string ToggleCommandPrefix = "CraftToggle:";

        /// <summary>
        /// Append one toggle row per carried item matching
        /// <paramref name="eligible"/>, showing its current set-aside state.
        /// Shared by the still (reagents) and forge (components / weapons /
        /// coatings). Rows sort below the station's verb rows via
        /// <paramref name="basePriority"/> (descending from there).
        /// </summary>
        public static void AddToggleRows(InventoryActionList actions, Entity actor,
            System.Predicate<Entity> eligible, int basePriority)
        {
            var inventory = actor?.GetPart<InventoryPart>();
            if (actions == null || inventory == null || eligible == null)
                return;

            List<Entity> objects = inventory.Objects;
            int priority = basePriority;
            for (int i = 0; i < objects.Count; i++)
            {
                Entity item = objects[i];
                if (item == null || string.IsNullOrEmpty(item.ID) || !eligible(item))
                    continue;

                // Retain stale picks as cleanup rows; never offer an empty new pick.
                if (!IsMarked(item) && !inventory.CanConsumeOne(item)) continue;

                string display = IsMarked(item)
                    ? "[x] " + item.GetDisplayName() + " - picked"
                    : "[ ] " + item.GetDisplayName() + " - add";
                if ((item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                    display += " (empty; remove pick)";
                actions.AddAction("CraftToggle", display,
                    ToggleCommandPrefix + item.ID, '\0', priority--);
            }
        }

        /// <summary>
        /// Scan <paramref name="actor"/>'s inventory for marked items and
        /// partition them. Precedence order mirrors part specificity:
        /// Reagent → WeaponComponent → BrewItem (coating candidates) →
        /// MeleeWeapon. Never returns null.
        /// </summary>
        public static MarkedSelection CollectMarked(Entity actor)
        {
            var selection = new MarkedSelection();

            var inventory = actor?.GetPart<InventoryPart>();
            if (inventory == null)
                return selection;

            List<Entity> objects = inventory.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                Entity item = objects[i];
                if (item == null || !item.HasPart<CraftingMarkPart>())
                    continue;

                if (item.HasPart<ReagentPart>())
                    selection.Reagents.Add(item);
                else if (item.HasPart<WeaponComponentPart>())
                    selection.Components.Add(item);
                else if (item.HasPart<BrewItemPart>())
                    selection.Coatings.Add(item);
                else if (item.HasPart<MeleeWeaponPart>())
                    selection.Weapons.Add(item);
            }

            return selection;
        }
    }
}
