namespace CavesOfOoo.Core.Inventory
{
    public interface IInventoryCommand
    {
        string Name { get; }

        InventoryValidationResult Validate(InventoryContext context);

        InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction);
    }
}
