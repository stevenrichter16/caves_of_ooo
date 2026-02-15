using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container for world objects (chests, boxes, corpses).
    /// Mirrors Qud's Container part: stores items that can be looted or stored into.
    /// Containers are interactable world objects — the player opens them from adjacent cells.
    /// Declares "Open"/"Loot" inventory action when interacted with.
    /// </summary>
    public class ContainerPart : Part
    {
        public override string Name => "Container";

        /// <summary>Items stored in this container.</summary>
        public List<Entity> Contents = new List<Entity>();

        /// <summary>Display preposition ("in", "on"). "Items in the chest."</summary>
        public string Preposition = "in";

        /// <summary>If true, container is locked and must be unlocked to open.</summary>
        public bool Locked = false;

        /// <summary>Max items allowed. -1 = unlimited.</summary>
        public int MaxItems = -1;

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
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    if (Locked)
                        actions.AddAction("Unlock", "unlock", "Unlock", 'u', 10);
                    else
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

                    if (Locked)
                    {
                        MessageLog.Add($"The {ParentEntity.GetDisplayName()} is locked.");
                        e.Handled = true;
                        return false;
                    }

                    // Fire OpenContainer event for extensibility
                    var openEvent = GameEvent.New("OpenContainer");
                    openEvent.SetParameter("Actor", (object)actor);
                    openEvent.SetParameter("Container", (object)ParentEntity);
                    actor.FireEvent(openEvent);

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
