namespace CavesOfOoo.Core.Inventory
{
    /// <summary>Optional presentation evidence from a completed ordinary command.
    /// Split equipment reports the actual prepared item, never the original stack.
    /// Null means no current item receipt; this does not affect command success.</summary>
    internal interface IInventoryAudioItem
    {
        Entity AudioItem { get; }
    }
}
