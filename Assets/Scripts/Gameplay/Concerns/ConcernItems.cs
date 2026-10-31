using CavesOfOoo.Core;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C3a: real carried items for concerns. Counting reads units (stacks count each unit)
    /// in the carried list only; equipped and floor items do not count. Taking is
    /// all-or-nothing: a short hand-over takes nothing. Sealed cargo for Carry approaches
    /// is tagged with <see cref="CargoProperty"/> = "concernId|approachId", so one
    /// concern's letter can never stand in for another's.
    /// </summary>
    public static class ConcernItems
    {
        public const string CargoProperty = "ConcernCargo";

        public static int CountCarried(Entity actor, string blueprint)
        {
            var inventory = actor?.GetPart<InventoryPart>();
            if (inventory?.Objects == null || string.IsNullOrEmpty(blueprint)) return 0;
            int units = 0;
            foreach (var item in inventory.Objects)
                if (item != null && item.BlueprintName == blueprint && inventory.CanConsumeOne(item))
                    units += item.GetPart<StackerPart>()?.StackCount ?? 1;
            return units;
        }

        public static bool TakeCarried(Entity actor, string blueprint, int count)
        {
            if (count <= 0 || CountCarried(actor, blueprint) < count) return false;
            var inventory = actor.GetPart<InventoryPart>();
            for (int taken = 0; taken < count; )
            {
                Entity unit = null;
                foreach (var item in inventory.Objects)
                    if (item != null && item.BlueprintName == blueprint && inventory.CanConsumeOne(item)) { unit = item; break; }
                if (unit == null || !inventory.TryConsumeOne(unit)) return false; // unreachable after the count check
                taken++;
            }
            return true;
        }

        public static string CargoKey(string concernId, string approachId) => concernId + "|" + approachId;

        public static Entity FindCargo(Entity actor, string blueprint, string key)
        {
            var inventory = actor?.GetPart<InventoryPart>();
            if (inventory?.Objects == null) return null;
            foreach (var item in inventory.Objects)
                if (item != null && item.BlueprintName == blueprint && item.GetProperty(CargoProperty) == key) return item;
            return null;
        }

        /// <summary>"Blueprint:Count" for IfHaveItemCount / TakeItemCount.</summary>
        public static bool TryParseCount(string arg, out string blueprint, out int count)
        {
            blueprint = null; count = 0;
            int colon = arg?.LastIndexOf(':') ?? -1;
            if (colon <= 0) return false;
            blueprint = arg.Substring(0, colon);
            return int.TryParse(arg.Substring(colon + 1), out count) && count > 0;
        }
    }
}
