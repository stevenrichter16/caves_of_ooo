using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Entity Part that manages the body part tree for a creature.
    /// Mirrors Qud's Body (XRL.World.Parts.Body): wraps a root BodyPart tree,
    /// manages dismemberment/regeneration, equipment integration, and
    /// dynamic body part operations.
    ///
    /// This is the primary Part for anatomy-aware creatures. It replaces
    /// the old string-keyed equipment slot system with a body-part-driven model.
    /// </summary>
    public class Body : Part
    {
        public override string Name => "Body";

        /// <summary>
        /// Root of the body part tree.
        /// </summary>
        private BodyPart _body;

        /// <summary>
        /// Body parts that have been dismembered but tracked for regeneration.
        /// Each entry stores the detached BodyPart and its original parent's ID.
        /// </summary>
        public List<DismemberedPart> DismemberedParts = new List<DismemberedPart>();

        /// <summary>
        /// Maximum mobility penalty from limb loss.
        /// </summary>
        public const int MAX_MOBILITY_PENALTY = 60;

        // --- Core accessors ---

        /// <summary>
        /// Get the root body part.
        /// </summary>
        public BodyPart GetBody()
        {
            return _body;
        }

        /// <summary>
        /// Set the root body part and propagate body references.
        /// </summary>
        public void SetBody(BodyPart root)
        {
            _body = root;
            if (_body != null)
            {
                PropagateBody(_body);
            }
        }

        /// <summary>
        /// Get a flat list of all attached body parts.
        /// </summary>
        public List<BodyPart> GetParts()
        {
            if (_body == null) return new List<BodyPart>();
            return _body.GetParts();
        }

        /// <summary>
        /// Find the first body part of a given type.
        /// </summary>
        public BodyPart GetPartByType(string type)
        {
            return _body?.GetPartByType(type);
        }

        /// <summary>
        /// Find all body parts of a given type.
        /// </summary>
        public List<BodyPart> GetPartsByType(string type)
        {
            if (_body == null) return new List<BodyPart>();
            return _body.GetPartsByType(type);
        }

        /// <summary>
        /// Count body parts of a given type.
        /// </summary>
        public int CountParts(string type)
        {
            return _body?.CountParts(type) ?? 0;
        }

        // --- Dynamic part management (for mutations/cybernetics) ---

        /// <summary>
        /// Add a body part to a specific parent with a manager ID.
        /// Mirrors Qud's pattern of mutations/cybernetics adding parts with a
        /// deterministic ManagerID for clean removal.
        ///
        /// Returns the added body part.
        /// </summary>
        public BodyPart AddPartByManager(string managerID, BodyPart parent, BodyPart newPart)
        {
            if (parent == null || newPart == null || string.IsNullOrEmpty(managerID))
                return null;

            newPart.Manager = managerID;
            newPart.Dynamic = true;
            newPart.Extrinsic = true;
            parent.AddPart(newPart);

            // Fire event
            var e = GameEvent.New("BodyPartAdded");
            e.SetParameter("Part", (object)newPart);
            e.SetParameter("Manager", managerID);
            ParentEntity?.FireEvent(e);

            return newPart;
        }

        /// <summary>
        /// Add a body part by type string with a manager ID.
        /// Convenience overload.
        /// </summary>
        public BodyPart AddPartByManager(string managerID, BodyPart parent, string type,
            int laterality = 0, string name = null, int category = BodyPartCategory.ANIMAL)
        {
            var newPart = new BodyPart
            {
                Type = type,
                Name = name ?? type.ToLowerInvariant(),
                Description = type,
                Category = category,
            };
            if (laterality != 0)
                newPart.SetLaterality(laterality);
            return AddPartByManager(managerID, parent, newPart);
        }

        /// <summary>
        /// Remove all body parts with a given manager ID.
        /// Returns the number of parts removed.
        /// </summary>
        public int RemovePartsByManager(string managerID, bool evenIfDismembered = false)
        {
            if (_body == null || string.IsNullOrEmpty(managerID))
                return 0;

            int count = _body.RemovePartsByManager(managerID, evenIfDismembered);

            if (evenIfDismembered)
            {
                for (int i = DismemberedParts.Count - 1; i >= 0; i--)
                {
                    if (DismemberedParts[i].Part.Manager == managerID)
                    {
                        DismemberedParts.RemoveAt(i);
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                var e = GameEvent.New("BodyPartsRemoved");
                e.SetParameter("Manager", managerID);
                e.SetParameter("Count", count);
                ParentEntity?.FireEvent(e);
            }

            return count;
        }

        // --- Dismemberment ---

        /// <summary>
        /// Dismember a body part: detach it and its subtree, track for regeneration.
        /// Mirrors Qud's Body.Dismember flow:
        /// 1. Gate check (BeforeDismemberEvent)
        /// 2. Unequip subtree
        /// 3. Detach from tree
        /// 4. Store in DismemberedParts for regeneration
        /// 5. Check unsupported part cascade
        /// 6. Recalculate mobility
        /// </summary>
        /// <summary>
        /// Dismember a body part: detach it and its subtree, track for regeneration.
        /// If a zone is provided, drops a severed limb item at the creature's position.
        /// </summary>
        public bool Dismember(BodyPart part, Zone zone = null)
        {
            if (part == null || _body == null) return false;
            if (part == _body) return false; // Can't dismember root
            if (part.ParentPart == null) return false; // Already detached

            // Gate check
            var beforeEvent = GameEvent.New("BeforeDismember");
            beforeEvent.SetParameter("Part", (object)part);
            if (ParentEntity != null && !ParentEntity.FireEvent(beforeEvent))
                return false;

            // Unequip everything on this part and children — drop to ground if zone provided
            UnequipSubtree(part, zone);

            // Detach from parent
            var parent = part.ParentPart;
            parent.RemovePart(part);

            // Store for regeneration (if not extrinsic)
            if (!part.Extrinsic)
            {
                DismemberedParts.Add(new DismemberedPart
                {
                    Part = part,
                    ParentPartID = parent.ID,
                    OriginalPosition = part.Position
                });
            }

            // Create and drop severed limb entity
            Entity limbEntity = null;
            if (zone != null && ParentEntity != null && !part.Abstract)
            {
                limbEntity = CreateSeveredLimb(part, zone);
            }

            // Post event
            var afterEvent = GameEvent.New("AfterDismember");
            afterEvent.SetParameter("Part", (object)part);
            afterEvent.SetParameter("Mortal", part.Mortal);
            if (limbEntity != null)
                afterEvent.SetParameter("SeveredLimbEntity", (object)limbEntity);
            ParentEntity?.FireEvent(afterEvent);

            // Log
            string partName = part.GetDisplayName();
            string entityName = ParentEntity?.GetDisplayName() ?? "something";
            MessageLog.Add($"{entityName}'s {partName} is severed!");

            // Check cascading loss
            CheckUnsupportedPartLoss();
            UpdateMobilityPenalty();

            // Mortal part loss → death
            if (part.Mortal && ParentEntity != null && zone != null)
            {
                CombatSystem.HandleDeath(ParentEntity, null, zone);
            }

            return true;
        }

        /// <summary>
        /// Create a severed limb item entity and place it at the creature's position.
        /// </summary>
        private Entity CreateSeveredLimb(BodyPart part, Zone zone)
        {
            var limbEntity = SeveredLimbFactory.Create(part);
            var pos = zone.GetEntityPosition(ParentEntity);
            if (pos.x >= 0 && pos.y >= 0)
            {
                zone.AddEntity(limbEntity, pos.x, pos.y);
            }
            return limbEntity;
        }

        // --- Regeneration ---

        /// <summary>
        /// Regenerate a dismembered body part, reattaching it to the body.
        /// Mirrors Qud's Body.RegenerateLimb.
        /// Returns true if a limb was regenerated.
        /// </summary>
        public bool RegenerateLimb(string preferredType = null)
        {
            if (DismemberedParts.Count == 0) return false;

            // Find eligible part
            DismemberedPart target = null;
            int targetIdx = -1;

            for (int i = 0; i < DismemberedParts.Count; i++)
            {
                var dp = DismemberedParts[i];
                if (!dp.Part.IsRegenerable()) continue;
                if (preferredType != null && dp.Part.Type != preferredType) continue;
                target = dp;
                targetIdx = i;
                break;
            }

            if (target == null) return false;

            // Find parent to reattach to
            BodyPart parent = FindPartByID(target.ParentPartID);
            if (parent == null)
                parent = _body; // fallback to root

            // Reattach
            parent.AddPart(target.Part, target.OriginalPosition);
            DismemberedParts.RemoveAt(targetIdx);

            // Post event
            var e = GameEvent.New("LimbRegenerated");
            e.SetParameter("Part", (object)target.Part);
            ParentEntity?.FireEvent(e);

            string partName = target.Part.GetDisplayName();
            string entityName = ParentEntity?.GetDisplayName() ?? "something";
            MessageLog.Add($"{entityName} regenerates {(target.Part.Plural ? "" : "a ")}{partName}!");

            // Check if any dependent parts can recover
            CheckPartRecovery();
            UpdateMobilityPenalty();

            return true;
        }

        /// <summary>
        /// Check if any regenerable limbs exist.
        /// </summary>
        public bool HasRegenerableLimbs()
        {
            for (int i = 0; i < DismemberedParts.Count; i++)
            {
                if (DismemberedParts[i].Part.IsRegenerable())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any mortal body parts have been dismembered.
        /// </summary>
        public bool AnyDismemberedMortalParts()
        {
            for (int i = 0; i < DismemberedParts.Count; i++)
            {
                if (DismemberedParts[i].Part.Mortal)
                    return true;
            }
            return false;
        }

        // --- Dependency maintenance ---

        /// <summary>
        /// Find and dismember any parts that have lost their support.
        /// Mirrors Qud's Body.CheckUnsupportedPartLoss.
        /// </summary>
        public void CheckUnsupportedPartLoss()
        {
            if (_body == null) return;
            var allParts = GetParts();

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = allParts.Count - 1; i >= 0; i--)
                {
                    var part = allParts[i];
                    if (part == _body) continue;
                    if (part.ParentPart == null) continue;
                    if (part.IsUnsupported())
                    {
                        // Auto-dismember unsupported part
                        UnequipSubtree(part);
                        var parent = part.ParentPart;
                        parent.RemovePart(part);

                        if (!part.Extrinsic)
                        {
                            DismemberedParts.Add(new DismemberedPart
                            {
                                Part = part,
                                ParentPartID = parent.ID,
                                OriginalPosition = part.Position
                            });
                        }

                        allParts.RemoveAt(i);
                        changed = true;

                        string partName = part.GetDisplayName();
                        string entityName = ParentEntity?.GetDisplayName() ?? "something";
                        MessageLog.Add($"{entityName} loses use of {(part.Plural ? "" : "the ")}{partName}!");
                    }
                }
            }
        }

        /// <summary>
        /// Check if any dismembered parts can be recovered because their
        /// dependencies have been restored.
        /// Mirrors Qud's Body.CheckPartRecovery.
        /// </summary>
        public void CheckPartRecovery()
        {
            if (_body == null) return;

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = DismemberedParts.Count - 1; i >= 0; i--)
                {
                    var dp = DismemberedParts[i];
                    // Only recover parts that were lost due to support loss
                    // (they have dependency fields set)
                    if (string.IsNullOrEmpty(dp.Part.DependsOn) &&
                        string.IsNullOrEmpty(dp.Part.RequiresType))
                        continue;

                    // Check if support is now available
                    if (!dp.Part.IsConcretelyUnsupported() && !dp.Part.IsAbstractlyUnsupported())
                    {
                        BodyPart parent = FindPartByID(dp.ParentPartID) ?? _body;
                        parent.AddPart(dp.Part, dp.OriginalPosition);
                        DismemberedParts.RemoveAt(i);
                        changed = true;

                        string partName = dp.Part.GetDisplayName();
                        string entityName = ParentEntity?.GetDisplayName() ?? "something";
                        MessageLog.Add($"{entityName} regains use of {(dp.Part.Plural ? "" : "the ")}{partName}.");
                    }
                }
            }
        }

        // --- Mobility penalty ---

        /// <summary>
        /// Calculate mobility speed penalty based on dismembered parts that
        /// had Mobility > 0.
        /// Mirrors Qud's Body.CalculateMobilitySpeedPenalty.
        /// </summary>
        public int CalculateMobilityPenalty()
        {
            int totalMobility = 0;
            int lostMobility = 0;

            // Count mobility from all parts (attached + dismembered)
            if (_body != null)
            {
                var attached = GetParts();
                for (int i = 0; i < attached.Count; i++)
                    totalMobility += attached[i].Mobility;
            }

            for (int i = 0; i < DismemberedParts.Count; i++)
            {
                int mob = DismemberedParts[i].Part.Mobility;
                totalMobility += mob;
                lostMobility += mob;
            }

            if (totalMobility <= 0) return 0;

            // Penalty proportional to lost mobility
            int penalty = (lostMobility * MAX_MOBILITY_PENALTY) / totalMobility;
            return Math.Min(penalty, MAX_MOBILITY_PENALTY);
        }

        /// <summary>
        /// Apply or update the mobility penalty on the parent entity's Speed stat.
        /// </summary>
        public void UpdateMobilityPenalty()
        {
            if (ParentEntity == null) return;
            var speed = ParentEntity.GetStat("Speed");
            if (speed == null) return;

            // Remove old penalty, apply new
            int oldPenalty = ParentEntity.GetIntProperty("MobilityPenalty", 0);
            int newPenalty = CalculateMobilityPenalty();

            if (oldPenalty != newPenalty)
            {
                speed.Penalty += (newPenalty - oldPenalty);
                ParentEntity.SetIntProperty("MobilityPenalty", newPenalty, removeIfZero: true);
            }
        }

        // --- Full body update maintenance pass ---

        /// <summary>
        /// Run a full body maintenance pass.
        /// Mirrors Qud's Body.UpdateBodyParts:
        /// - check unsupported part loss
        /// - check part recovery
        /// - recalculate first-slot flags
        /// - update mobility penalty
        /// </summary>
        public void UpdateBodyParts()
        {
            if (_body == null) return;

            CheckUnsupportedPartLoss();
            CheckPartRecovery();
            CheckImpliedParts();
            _body.RecalculateFirstEquipped();
            RegenerateDefaultEquipment();
            UpdateMobilityPenalty();
        }

        /// <summary>
        /// Ensure all body parts with a DefaultBehaviorBlueprint have their
        /// _DefaultBehavior entity populated. Creates natural weapon entities
        /// from known blueprint names via NaturalWeaponFactory.
        /// Mirrors Qud's Body.RegenerateDefaultEquipment.
        /// </summary>
        public void RegenerateDefaultEquipment()
        {
            if (_body == null) return;
            var parts = GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (!string.IsNullOrEmpty(part.DefaultBehaviorBlueprint))
                {
                    if (part._DefaultBehavior == null)
                    {
                        part._DefaultBehavior = NaturalWeaponFactory.Create(part.DefaultBehaviorBlueprint);
                    }
                }
                else
                {
                    if (part._DefaultBehavior != null)
                    {
                        part._DefaultBehavior = null;
                        part.FirstSlotForDefaultBehavior = false;
                    }
                }
            }
            _body.RecalculateFirstDefaultBehavior();
        }

        // --- Implied Parts ---

        private const string IMPLIED_MANAGER_PREFIX = "Implied:";

        /// <summary>
        /// Check all body part types with ImpliedBy set. If the count of implied parts
        /// is wrong, add or remove dynamic implied parts. Only manages parts tagged with
        /// the "Implied:{Type}" manager ID; never touches native/manually-created parts.
        /// Mirrors Qud's Body.CheckImpliedParts.
        /// </summary>
        public void CheckImpliedParts()
        {
            if (_body == null) return;

            var types = AnatomyFactory.GetTypes();

            foreach (var kvp in types)
            {
                var bpt = kvp.Value;
                if (string.IsNullOrEmpty(bpt.ImpliedBy)) continue;

                int impliedPer = bpt.ImpliedPer ?? 1;
                if (impliedPer <= 0) continue;

                string impliedType = bpt.Type;
                string implyingType = bpt.ImpliedBy;
                string managerID = IMPLIED_MANAGER_PREFIX + impliedType;

                // Count implying parts (e.g., how many Hands exist)
                int implyingCount = _body.CountParts(implyingType);

                // Expected total count of implied parts
                int expectedCount = implyingCount / impliedPer;

                // Count existing implied parts: native vs dynamic
                var allImplied = _body.GetPartsByType(impliedType);
                int existingDynamic = 0;
                int existingNative = 0;
                for (int i = 0; i < allImplied.Count; i++)
                {
                    if (allImplied[i].Manager == managerID)
                        existingDynamic++;
                    else
                        existingNative++;
                }

                // How many dynamic implied parts are needed beyond native ones?
                int needed = expectedCount - existingNative;
                if (needed < 0) needed = 0;

                if (existingDynamic < needed)
                {
                    // Add missing implied parts
                    var implyingParts = _body.GetPartsByType(implyingType);
                    int toAdd = needed - existingDynamic;
                    for (int j = 0; j < toAdd; j++)
                    {
                        BodyPart attachParent = FindImpliedAttachPoint(implyingParts, impliedType, managerID);
                        if (attachParent == null)
                            attachParent = _body;

                        var newPart = AnatomyFactory.CreatePart(impliedType);

                        // Inherit laterality from the implying part whose parent we're attaching to
                        for (int k = 0; k < implyingParts.Count; k++)
                        {
                            if (implyingParts[k].ParentPart == attachParent)
                            {
                                newPart.SetLaterality(implyingParts[k].GetLaterality());
                                break;
                            }
                        }

                        AddPartByManager(managerID, attachParent, newPart);
                    }
                }
                else if (existingDynamic > needed)
                {
                    // Remove excess dynamic implied parts
                    int toRemove = existingDynamic - needed;
                    for (int i = allImplied.Count - 1; i >= 0 && toRemove > 0; i--)
                    {
                        if (allImplied[i].Manager == managerID)
                        {
                            allImplied[i].ParentPart?.RemovePart(allImplied[i]);
                            toRemove--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find the best parent to attach an implied part to.
        /// Returns the parent of an implying part that doesn't already have
        /// a dynamic implied sibling from the same manager.
        /// </summary>
        private BodyPart FindImpliedAttachPoint(List<BodyPart> implyingParts, string impliedType, string managerID)
        {
            for (int i = 0; i < implyingParts.Count; i++)
            {
                var parent = implyingParts[i].ParentPart;
                if (parent == null) continue;

                bool hasExisting = false;
                if (parent.Parts != null)
                {
                    for (int j = 0; j < parent.Parts.Count; j++)
                    {
                        if (parent.Parts[j].Type == impliedType && parent.Parts[j].Manager == managerID)
                        {
                            hasExisting = true;
                            break;
                        }
                    }
                }
                if (!hasExisting)
                    return parent;
            }
            return null;
        }

        // --- Equipment integration ---

        /// <summary>
        /// Find all body parts that can accept an item with a given slot type.
        /// Used by the equipment system to query available slots.
        /// </summary>
        public List<BodyPart> GetEquippableSlots(string slotType)
        {
            if (_body == null) return new List<BodyPart>();
            return _body.GetEquippableSlots(slotType);
        }

        /// <summary>
        /// Find the first free body part of a given type.
        /// </summary>
        public BodyPart FindFreeSlot(string slotType, int laterality = 0)
        {
            return _body?.FindFreeSlot(slotType, laterality);
        }

        /// <summary>
        /// Get all equipped items across all body parts.
        /// </summary>
        public void ForeachEquippedObject(Action<Entity, BodyPart> action)
        {
            _body?.ForeachEquippedObject(action);
        }

        /// <summary>
        /// Get a summary of missing/dismembered body parts.
        /// Mirrors Qud's Body.GetMissingLimbsDescription.
        /// </summary>
        public string GetMissingLimbsDescription()
        {
            if (DismemberedParts.Count == 0)
                return "";

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < DismemberedParts.Count; i++)
            {
                string name = DismemberedParts[i].Part.GetDisplayName();
                if (!counts.ContainsKey(name))
                    counts[name] = 0;
                counts[name]++;
            }

            var parts = new List<string>();
            foreach (var kvp in counts)
            {
                if (kvp.Value == 1)
                    parts.Add(kvp.Key);
                else
                    parts.Add($"{kvp.Value} {kvp.Key}s");
            }

            return string.Join(", ", parts);
        }

        // --- Event handling ---

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeginTurn")
            {
                // Periodic maintenance (could be gated to only run when dirty)
                // Currently lightweight enough to run every turn
            }
            return true;
        }

        // --- Helpers ---

        private BodyPart FindPartByID(int id)
        {
            if (_body == null) return null;
            var parts = GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].ID == id) return parts[i];
            }
            return null;
        }

        private void PropagateBody(BodyPart part)
        {
            part.ParentBody = this;
            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    PropagateBody(part.Parts[i]);
            }
        }

        /// <summary>
        /// Drop all equipment from all body parts to the zone.
        /// Called on death to create loot drops.
        /// </summary>
        public void DropAllEquipment(Zone zone)
        {
            if (_body == null || zone == null) return;
            UnequipSubtree(_body, zone);

            // Also clear the EquippedItems cache on inventory
            var inventory = ParentEntity?.GetPart<InventoryPart>();
            if (inventory != null)
                inventory.EquippedItems.Clear();
        }

        private void UnequipSubtree(BodyPart part, Zone zone = null)
        {
            if (part._Equipped != null)
            {
                var item = part._Equipped;
                part.ClearEquipped();

                // Clear from any other body parts sharing this item
                ClearEquipmentFromAllParts(item);

                var physics = item.GetPart<PhysicsPart>();

                // Drop to ground if zone is available (dismemberment), otherwise move to inventory
                if (zone != null && ParentEntity != null)
                {
                    if (physics != null)
                    {
                        physics.Equipped = null;
                        physics.InInventory = null;
                    }

                    // Remove from inventory if present
                    var inventory = ParentEntity.GetPart<InventoryPart>();
                    if (inventory != null)
                        inventory.Objects.Remove(item);

                    var pos = zone.GetEntityPosition(ParentEntity);
                    if (pos.x >= 0 && pos.y >= 0)
                    {
                        zone.AddEntity(item, pos.x, pos.y);
                        string itemName = item.GetDisplayName();
                        string entityName = ParentEntity.GetDisplayName();
                        MessageLog.Add($"{entityName}'s {itemName} falls to the ground!");
                    }
                }
                else
                {
                    var inventory = ParentEntity?.GetPart<InventoryPart>();
                    if (inventory != null)
                    {
                        if (physics != null)
                        {
                            physics.Equipped = null;
                            physics.InInventory = ParentEntity;
                        }
                        inventory.Objects.Add(item);
                    }
                }
            }

            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    UnequipSubtree(part.Parts[i], zone);
            }
        }

        private void ClearEquipmentFromAllParts(Entity item)
        {
            if (_body == null) return;
            var parts = GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i]._Equipped == item)
                    parts[i].ClearEquipped();
            }
        }
    }

    /// <summary>
    /// Tracks a dismembered body part and its original position for regeneration.
    /// </summary>
    [Serializable]
    public class DismemberedPart
    {
        public BodyPart Part;
        public int ParentPartID;
        public int OriginalPosition;
    }
}
