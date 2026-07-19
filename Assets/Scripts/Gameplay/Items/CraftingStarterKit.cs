using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// M3-L3: the spawn ingredient kit — every production reagent and weapon
    /// component, granted to the player at bootstrap (stacked ×2) so the
    /// starting-village still and forge are usable from turn one. Static and
    /// engine-free so the grant is unit-testable; GameBootstrap calls
    /// <see cref="GrantAll"/> right after the starting-tonic grant.
    ///
    /// The catalogs are pinned against production Objects.json by
    /// CraftingStarterKitTests — renaming a blueprint breaks a test, not
    /// the kit silently.
    /// </summary>
    public static class CraftingStarterKit
    {
        /// <summary>Units of each ingredient granted (as one stack).</summary>
        public const int CopiesPerIngredient = 2;

        /// <summary>All 13 production reagents (Objects.json, M1.3 catalog).</summary>
        public static readonly string[] ReagentBlueprints =
        {
            "FireMoss",
            "LampOil",
            "EmberFruit",
            "FrostLichen",
            "GlacierSalt",
            "GlimmerBrine",
            "SparkRoot",
            "VenomGland",
            "BogSap",
            "CandyHeartRoot",
            "MendleafSprig",
            "StoneburrSeed",
            "BlastcapSpore"
        };

        /// <summary>All 6 production weapon components (Objects.json, M3-L1 catalog).</summary>
        public static readonly string[] ComponentBlueprints =
        {
            "SteelBladeComponent",
            "IronSpikeComponent",
            "OakHaftComponent",
            "WillowHaftComponent",
            "LeatherBindingComponent",
            "SerratedEdgeComponent"
        };

        /// <summary>
        /// Grant one ×<see cref="CopiesPerIngredient"/> stack of every catalog
        /// entry to <paramref name="player"/>. Returns how many grants landed
        /// (0 on null player/factory/inventory). Per-item failures skip and
        /// continue, mirroring the starting-tonic grant's tolerance.
        /// </summary>
        public static int GrantAll(Entity player, EntityFactory factory)
        {
            var inventory = player?.GetPart<InventoryPart>();
            if (inventory == null || factory == null)
                return 0;

            int granted = 0;
            granted += GrantList(inventory, factory, ReagentBlueprints);
            granted += GrantList(inventory, factory, ComponentBlueprints);
            return granted;
        }

        private static int GrantList(InventoryPart inventory, EntityFactory factory, string[] blueprints)
        {
            int granted = 0;
            for (int i = 0; i < blueprints.Length; i++)
            {
                Entity item = factory.CreateEntity(blueprints[i]);
                if (item == null)
                    continue;

                if (CopiesPerIngredient > 1)
                {
                    var stacker = item.GetPart<StackerPart>();
                    if (stacker == null)
                    {
                        stacker = new StackerPart();
                        item.AddPart(stacker);
                    }
                    stacker.StackCount = CopiesPerIngredient;
                }

                if (!inventory.AddObject(item))
                    continue;

                granted++;
            }

            return granted;
        }
    }
}
