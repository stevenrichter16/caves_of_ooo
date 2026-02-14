using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    /// <summary>
    /// A single equipped item/body-part pair that would be displaced by an equip plan.
    /// </summary>
    public struct InventoryDisplacement
    {
        public Entity Item;
        public BodyPart BodyPart;
    }
}
