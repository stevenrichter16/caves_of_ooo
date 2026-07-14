using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// FUN-P0 M1.a — the player's real Level-1 start.
    ///
    /// The mortal starter kit is one Dagger, two HealingTonics, and an
    /// EMPTY BitLockerPart. Everything else the old bootstrap granted
    /// (six attack mutations, every tinker recipe, five of every bit,
    /// eight tonics, free two-handers, a debug trader) is now debug-only,
    /// gated behind <see cref="DebugGrantsEnabled"/>.
    /// </summary>
    public static class StartingLoadout
    {
        /// <summary>
        /// When true, GameBootstrap re-enables the legacy showcase grants
        /// (mutations, recipes+bits, tonic pile, free weapons, debug trader).
        /// Ships FALSE: the mortal start is the game. Mutable-static rather
        /// than const (GraphicsPolish-style) so tests and dev scenarios can
        /// exercise both branches at runtime; mirrors AIDebug.AIInspectorEnabled.
        /// </summary>
        public static bool DebugGrantsEnabled = false;

        /// <summary>
        /// Int property marking the kit as granted. The load path re-runs
        /// bootstrap wiring against a restored player; the marker makes a
        /// second ApplyStarterKit a no-op instead of a duplicate kit.
        /// Persisted with the player's properties by the save graph.
        /// </summary>
        public const string KitAppliedProperty = "StarterKitApplied";

        /// <summary>
        /// Grant the mortal starter kit. BitLockerPart is attached even
        /// though it starts empty: the Tinker UI and ItemEnhancementShowcase
        /// require the part's presence, and the blueprint does not carry it
        /// (pre-M1 it was attached by the debug tinkering grant — sweep C2).
        /// </summary>
        public static void ApplyStarterKit(Entity player, EntityFactory factory)
        {
            if (player == null || factory == null)
                return;

            if (player.GetIntProperty(KitAppliedProperty, 0) != 0)
                return;

            if (player.GetPart<BitLockerPart>() == null)
                player.AddPart(new BitLockerPart());

            var inventory = player.GetPart<InventoryPart>();
            if (inventory != null)
            {
                AddItem(inventory, factory, "Dagger");
                AddItem(inventory, factory, "HealingTonic");
                AddItem(inventory, factory, "HealingTonic");
            }

            player.SetIntProperty(KitAppliedProperty, 1);
        }

        private static void AddItem(InventoryPart inventory, EntityFactory factory, string blueprint)
        {
            var item = factory.CreateEntity(blueprint);
            if (item != null)
                inventory.AddObject(item);
        }
    }
}
