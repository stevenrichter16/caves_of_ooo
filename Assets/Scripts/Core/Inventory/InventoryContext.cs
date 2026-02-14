namespace CavesOfOoo.Core.Inventory
{
    public sealed class InventoryContext
    {
        public Entity Actor { get; }

        public Zone Zone { get; }

        public InventoryPart Inventory { get; }

        public Body Body { get; }

        public InventoryContext(Entity actor, Zone zone = null)
        {
            Actor = actor;
            Zone = zone;
            Inventory = actor?.GetPart<InventoryPart>();
            Body = actor?.GetPart<Body>();
        }
    }
}
