using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        (Entity Actor, Entity Tool, Zone Zone, Cell Cell)? _pendingToolWorldRequest;

        void QueueToolWorldAction(InventoryToolsData.Entry entry)
        {
            if (entry == null || !InventoryToolsData.CanInspectNearbyTool(PlayerEntity, CurrentZone, entry.Item, entry.Cell))
            {
                ShowActionFailure("That tool is no longer within reach.");
                Render();
                return;
            }

            Close();
            _pendingToolWorldRequest = (PlayerEntity, entry.Item, CurrentZone, entry.Cell);
        }

        internal (Entity Actor, Entity Tool, Zone Zone, Cell Cell)? ConsumeToolWorldRequest()
        {
            var request = _pendingToolWorldRequest;
            _pendingToolWorldRequest = null;
            return request;
        }
    }
}
