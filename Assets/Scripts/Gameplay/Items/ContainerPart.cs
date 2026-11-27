using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container for world objects (chests, boxes, corpses).
    /// Mirrors Qud's Container part: stores items that can be looted or stored into.
    /// Containers are interactable world objects — the player opens them from adjacent cells.
    /// Declares "Open"/"Loot" inventory action when interacted with.
    /// </summary>
    public class ContainerPart : Part, IInventoryActionReceiptOwner
    {
        public override string Name => "Container";

        /// <summary>Items stored in this container.</summary>
        public List<Entity> Contents = new List<Entity>();

        /// <summary>Display preposition ("in", "on"). "Items in the chest."</summary>
        public string Preposition = "in";

        /// <summary>If true, container is locked and must be unlocked to open.</summary>
        public bool Locked = false;

        /// <summary>Live access authority. A legacy lock, key lock or structural
        /// fault can each deny access; clearing one never clears the others.
        /// Computed from current state so stale menus cannot bypass a relock.
        /// Generation may still stock a locked container through AddItem.</summary>
        public bool IsLocked => Locked || ParentEntity?.GetPart<LockPart>()?.IsLocked == true || NeedsRepair;

        public bool NeedsRepair => RepairablePart.BlocksFunction(ParentEntity);
        internal RepairablePart AccessRepair => ParentEntity?.GetPart<RepairablePart>() is { PortableEquipment: false } repair ? repair : null;
        internal const string RepairAccessMessage = "It needs repair before you can get at the contents.";

        internal string RepairAccessFailure(Entity actor, Zone zone)
        {
            if (AccessRepair == null) return null;
            if (!PreparationActions.CurrentActor(actor, zone) || !WorldResourceActions.Nearby(actor, ParentEntity, zone)
                || WorldLocationContext.For(zone) is OverworldZoneManager manager && manager.ActiveZone != zone
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true)
                return "Stand beside the container to reach its contents.";
            return NeedsRepair ? RepairAccessMessage : null;
        }

        internal bool RepairTransferCurrent(Entity actor, Zone zone, RepairablePart repair, List<Entity> contents)
            => ParentEntity?.GetPart<ContainerPart>() == this && AccessRepair == repair && Contents == contents
                && !IsLocked && RepairAccessFailure(actor, zone) == null;

        public bool AllowsCleanAccess => !NeedsRepair && !LocalPorterReservationPart.Blocks(ParentEntity)
            && (ParentEntity?.GetPart<ContainedLiquidSourcePart>()?.AllowsCleanAccess ?? true);

        /// <summary>Max items allowed. -1 = unlimited.</summary>
        public int MaxItems = -1;
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<ContainerPart>()==this
            && (CampBoxWork.IsCommand(command) || VesselTorchRefueling.IsStoredCommand(command) && ParentEntity.BlueprintName=="CampBox");

        /// <summary>
        /// Add an item to this container. Returns false if full.
        /// </summary>
        public bool AddItem(Entity item)
        {
            if (item == null) return false;

            var itemStacker = item.GetPart<StackerPart>();
            if (itemStacker != null)
            {
                // When full, only allow adding a stack if it can fully merge into
                // existing stacks without creating a new entry.
                if (MaxItems >= 0 && Contents.Count >= MaxItems)
                {
                    int remaining = itemStacker.StackCount;
                    for (int i = 0; i < Contents.Count && remaining > 0; i++)
                    {
                        var existingStacker = Contents[i].GetPart<StackerPart>();
                        if (existingStacker == null || !existingStacker.CanStackWith(item))
                            continue;

                        int canAccept = existingStacker.MaxStack - existingStacker.StackCount;
                        if (canAccept > 0)
                            remaining -= canAccept;
                    }

                    if (remaining > 0)
                        return false;
                }

                // Merge into existing stacks first.
                for (int i = 0; i < Contents.Count; i++)
                {
                    var existingStacker = Contents[i].GetPart<StackerPart>();
                    if (existingStacker == null || !existingStacker.CanStackWith(item))
                        continue;

                    existingStacker.MergeFrom(item);
                    if (itemStacker.StackCount <= 0)
                        return true;
                }
            }

            if (MaxItems >= 0 && Contents.Count >= MaxItems) return false;

            Contents.Add(item);
            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.InInventory = ParentEntity;
                physics.Equipped = null;
            }
            return true;
        }

        /// <summary>
        /// Remove an item from this container.
        /// </summary>
        public bool RemoveItem(Entity item)
        {
            if (item == null) return false;
            if (!Contents.Remove(item)) return false;

            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
                physics.InInventory = null;
            return true;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if(!CampBoxWork.Handle(ParentEntity,e))return false;
            if(!VesselTorchRefueling.HandleStored(ParentEntity,e))return false;
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    if (NeedsRepair)
                        actions.AddDisabled("Open", "open", "container-needs-repair", RepairAccessMessage, 'o', 30);
                    else if (IsLocked)
                    {
                        // The key-based lock contributes its own Unlock action.
                        if (ParentEntity?.GetPart<LockPart>()?.IsLocked != true)
                            actions.AddAction("Unlock", "unlock", "Unlock", 'u', 10);
                    }
                    else if (AllowsCleanAccess)
                        actions.AddAction("Open", "open", "OpenContainer", 'o', 30);
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == "OpenContainer")
                {
                    var actor = e.GetParameter<Entity>("Actor");
                    if (actor == null) return true;

                    if (RepairAccessFailure(actor, e.GetParameter<Zone>("Zone") ?? actor.SpatialZone) is string failure)
                    {
                        MessageLog.Add(failure);
                        return true;
                    }
                    if (!AllowsCleanAccess) return true;
                    if (IsLocked)
                    {
                        MessageLog.Add($"The {ParentEntity.GetDisplayName()} is locked.");
                        e.Handled = true;
                        return false;
                    }

                    // Fire OpenContainer event for extensibility
                    var openEvent = GameEvent.New("OpenContainer");
                    openEvent.SetParameter("Actor", (object)actor);
                    openEvent.SetParameter("Container", (object)ParentEntity);
                    actor.FireEventAndRelease(openEvent);

                    if (Contents.Count == 0)
                    {
                        MessageLog.Add($"The {ParentEntity.GetDisplayName()} is empty.");
                    }
                    else
                    {
                        string containerName = ParentEntity.GetDisplayName();
                        MessageLog.Add($"You open the {containerName}. It contains {Contents.Count} item(s).");
                    }

                    e.Handled = true;
                    return false;
                }
            }

            return true;
        }
    }
}
