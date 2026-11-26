using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private void OpenCarriedStorage(Entity vessel, bool stowing)
        {
            if (!CarriedStorage.IsCarried(PlayerEntity, vessel))
            {
                SetActionFailure("That vessel is no longer available in your pack.");
                Render();
                return;
            }

            _itemActionPopup = null;
            _ledgerCommands.Clear();
            _ledgerCommandIndex = 0;
            _ledgerCommandTitle = stowing ? "STOW INTO " + vessel.GetDisplayName() : vessel.GetDisplayName() + " CONTENTS";
            var actor = PlayerEntity;
            var zone = CurrentZone;
            var stored = vessel.GetPart<ContainerPart>();
            var candidates = stowing ? actor.GetPart<InventoryPart>().Objects : stored.Contents;
            foreach (var item in candidates)
            {
                bool available = stowing ? CarriedStorage.CanDeposit(actor, vessel, item, zone, out _)
                    : CarriedStorage.CanWithdraw(actor, vessel, item, zone, out _);
                if (!available) continue;
                AddLedgerCommand((stowing ? "Stow " : "Take ") + item.GetDisplayName(), () =>
                    MoveInCarriedStorage(actor, vessel, stored, item, zone, stowing));
            }
            if (_ledgerCommands.Count == 0)
                AddLedgerCommand(stowing ? "No eligible items fit" : "No accessible items", () => OpenCarriedStorage(vessel, stowing));
            AddLedgerCommand(stowing ? "View contents" : "Stow from pack", () => OpenCarriedStorage(vessel, !stowing));
            AddLedgerCommand("Back to pack", () => { });
            _ledgerCommandsOpen = true;
            Render();
        }

        private void MoveInCarriedStorage(Entity actor, Entity vessel, ContainerPart container,
            Entity item, Zone zone, bool stowing)
        {
            if (PlayerEntity != actor || CurrentZone != zone || vessel.GetPart<ContainerPart>() != container)
            {
                SetActionFailure("That storage selection changed.");
                Render();
                return;
            }

            string name = item.GetDisplayName();
            bool moved = stowing ? CarriedStorage.TryDeposit(actor, vessel, item, zone, out string reason)
                : CarriedStorage.TryWithdraw(actor, vessel, item, zone, out reason);
            if (moved) SetActionSuccess(stowing ? "Stowed " + name + "." : "Moved " + name + " to the pack.");
            else SetActionFailure(reason);
            Rebuild();
            ClampCursor();
            OpenCarriedStorage(vessel, stowing);
        }
    }
}
