using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    /// <summary>One exact carried/held provider for supervised material work.
    /// Concrete built-in capabilities retain their own admission and state.</summary>
    internal interface IMaterialWorkReceipt
    {
        Entity Tool { get; }
        bool Current();
        bool Begin(InventoryTransaction transaction,string command);
    }
}
