using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The DESIGNED new-game kit (ALPHA-READINESS item 4 SM3) that
    /// replaces the dev-sandbox grants in normal play: a sidearm, a
    /// couple of heals, a little food. Deliberately small — spells come
    /// from grimoires found in the world, seeds from the farming kit
    /// (granted separately; it is a designed feature loop), and
    /// everything else from play. Contract pinned by
    /// AlphaStripDebugTests.
    /// </summary>
    public static class NewGameLoadout
    {
        public struct Entry
        {
            public string Blueprint;
            public int Count;
            public Entry(string blueprint, int count) { Blueprint = blueprint; Count = count; }
        }

        public static readonly Entry[] Items =
        {
            new Entry("Dagger", 1),
            new Entry("HealingTonic", 2),
            new Entry("DriedMeat", 2),
        };

        /// <summary>Grant the kit. Stacker-aware (counts collapse into
        /// stacks where the blueprint stacks). Returns the number of
        /// entries granted; 0 and no-op on null player/factory/inventory.</summary>
        public static int Grant(Entity player, EntityFactory factory)
        {
            if (player == null || factory == null) return 0;
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null) return 0;

            int granted = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                var item = factory.CreateEntity(Items[i].Blueprint);
                if (item == null) continue;
                var stacker = item.GetPart<StackerPart>();
                if (stacker != null && Items[i].Count > 1)
                {
                    stacker.StackCount = Items[i].Count;
                    if (inventory.AddObject(item)) granted++;
                }
                else
                {
                    bool any = false;
                    for (int n = 0; n < Items[i].Count; n++)
                    {
                        var each = n == 0 ? item : factory.CreateEntity(Items[i].Blueprint);
                        if (each != null && inventory.AddObject(each)) any = true;
                    }
                    if (any) granted++;
                }
            }
            return granted;
        }
    }
}
