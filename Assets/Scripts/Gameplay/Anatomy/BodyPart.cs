using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Runtime body part tree node. Each creature's anatomy is a tree of these.
    /// Mirrors Qud's BodyPart: a recursive node with identity, flags, equipment
    /// references, dependency links, and child parts.
    ///
    /// Key concepts:
    /// - Type: the body part type string (e.g. "Head", "Hand", "Arm")
    /// - Flags: bitmask properties (Appendage, Integral, Mortal, etc.)
    /// - Equipment: items are equipped directly onto body parts
    /// - Dependencies: parts can depend on other parts (lose hand → lose fingers)
    /// - Manager: string ID for dynamic parts added by mutations/cybernetics
    /// - Position: ordering among siblings
    /// </summary>
    [Serializable]
    public class BodyPart
    {
        // --- Flag bit constants (mirrors Qud's BodyPart flags) ---
        public const int FLAG_DEFAULT_PRIMARY = 1;
        public const int FLAG_PREFERRED_PRIMARY = 2;
        public const int FLAG_PRIMARY = 4;
        public const int FLAG_NATIVE = 8;
        public const int FLAG_APPENDAGE = 16;
        public const int FLAG_INTEGRAL = 32;
        public const int FLAG_MORTAL = 64;
        public const int FLAG_ABSTRACT = 128;
        public const int FLAG_EXTRINSIC = 256;
        public const int FLAG_DYNAMIC = 512;
        public const int FLAG_PLURAL = 1024;
        public const int FLAG_MASS = 2048;
        public const int FLAG_CONTACT = 4096;
        public const int FLAG_IGNORE_POSITION = 8192;
        public const int FLAG_FIRST_EQUIPPED = 16384;
        public const int FLAG_FIRST_CYBERNETIC = 32768;
        public const int FLAG_FIRST_DEFAULT = 65536;

        // --- Identity ---
        public string Type;
        public string VariantType;
        public string Description;
        public string DescriptionPrefix;
        public string Name;

        // --- Dependency ---
        public string SupportsDependent;
        public string DependsOn;
        public string RequiresType;

        // --- Dynamic part management ---
        public string Manager;

        // --- Numeric fields ---
        public int Category = BodyPartCategory.ANIMAL;
        public int _Laterality;
        public int RequiresLaterality = Laterality.ANY;
        public int Mobility;
        public int TargetWeight;
        public int Flags = FLAG_CONTACT; // Contact is default on in Qud
        public int Position = -1;

        // --- Tree structure ---
        [NonSerialized]
        public Body ParentBody;

        [NonSerialized]
        public BodyPart ParentPart;

        public List<BodyPart> Parts;

        // --- Equipment references ---
        public string DefaultBehaviorBlueprint;

        [NonSerialized]
        public Entity _Equipped;

        [NonSerialized]
        public Entity _Cybernetics;

        [NonSerialized]
        public Entity _DefaultBehavior;

        // --- Unique ID ---
        private static int _nextID = 1;
        private int _id;

        public int ID
        {
            get
            {
                if (_id == 0)
                    _id = _nextID++;
                return _id;
            }
            set => _id = value;
        }

        // --- Flag properties ---

        public bool DefaultPrimary
        {
            get => (Flags & FLAG_DEFAULT_PRIMARY) != 0;
            set => Flags = value ? (Flags | FLAG_DEFAULT_PRIMARY) : (Flags & ~FLAG_DEFAULT_PRIMARY);
        }

        public bool PreferredPrimary
        {
            get => (Flags & FLAG_PREFERRED_PRIMARY) != 0;
            set => Flags = value ? (Flags | FLAG_PREFERRED_PRIMARY) : (Flags & ~FLAG_PREFERRED_PRIMARY);
        }

        public bool Primary
        {
            get => (Flags & FLAG_PRIMARY) != 0;
            set => Flags = value ? (Flags | FLAG_PRIMARY) : (Flags & ~FLAG_PRIMARY);
        }

        public bool Native
        {
            get => (Flags & FLAG_NATIVE) != 0;
            set => Flags = value ? (Flags | FLAG_NATIVE) : (Flags & ~FLAG_NATIVE);
        }

        public bool Appendage
        {
            get => (Flags & FLAG_APPENDAGE) != 0;
            set => Flags = value ? (Flags | FLAG_APPENDAGE) : (Flags & ~FLAG_APPENDAGE);
        }

        public bool Integral
        {
            get => (Flags & FLAG_INTEGRAL) != 0;
            set => Flags = value ? (Flags | FLAG_INTEGRAL) : (Flags & ~FLAG_INTEGRAL);
        }

        public bool Mortal
        {
            get => (Flags & FLAG_MORTAL) != 0;
            set => Flags = value ? (Flags | FLAG_MORTAL) : (Flags & ~FLAG_MORTAL);
        }

        public bool Abstract
        {
            get => (Flags & FLAG_ABSTRACT) != 0;
            set => Flags = value ? (Flags | FLAG_ABSTRACT) : (Flags & ~FLAG_ABSTRACT);
        }

        public bool Extrinsic
        {
            get => (Flags & FLAG_EXTRINSIC) != 0;
            set => Flags = value ? (Flags | FLAG_EXTRINSIC) : (Flags & ~FLAG_EXTRINSIC);
        }

        public bool Dynamic
        {
            get => (Flags & FLAG_DYNAMIC) != 0;
            set => Flags = value ? (Flags | FLAG_DYNAMIC) : (Flags & ~FLAG_DYNAMIC);
        }

        public bool Plural
        {
            get => (Flags & FLAG_PLURAL) != 0;
            set => Flags = value ? (Flags | FLAG_PLURAL) : (Flags & ~FLAG_PLURAL);
        }

        public bool Mass
        {
            get => (Flags & FLAG_MASS) != 0;
            set => Flags = value ? (Flags | FLAG_MASS) : (Flags & ~FLAG_MASS);
        }

        public bool Contact
        {
            get => (Flags & FLAG_CONTACT) != 0;
            set => Flags = value ? (Flags | FLAG_CONTACT) : (Flags & ~FLAG_CONTACT);
        }

        public bool IgnorePosition
        {
            get => (Flags & FLAG_IGNORE_POSITION) != 0;
            set => Flags = value ? (Flags | FLAG_IGNORE_POSITION) : (Flags & ~FLAG_IGNORE_POSITION);
        }

        public bool FirstSlotForEquipped
        {
            get => (Flags & FLAG_FIRST_EQUIPPED) != 0;
            set => Flags = value ? (Flags | FLAG_FIRST_EQUIPPED) : (Flags & ~FLAG_FIRST_EQUIPPED);
        }

        public bool FirstSlotForCybernetics
        {
            get => (Flags & FLAG_FIRST_CYBERNETIC) != 0;
            set => Flags = value ? (Flags | FLAG_FIRST_CYBERNETIC) : (Flags & ~FLAG_FIRST_CYBERNETIC);
        }

        public bool FirstSlotForDefaultBehavior
        {
            get => (Flags & FLAG_FIRST_DEFAULT) != 0;
            set => Flags = value ? (Flags | FLAG_FIRST_DEFAULT) : (Flags & ~FLAG_FIRST_DEFAULT);
        }

        // --- Equipment accessors ---

        public Entity Equipped => _Equipped;
        public Entity Cybernetics => _Cybernetics;
        public Entity DefaultBehavior => _DefaultBehavior;

        /// <summary>
        /// Whether this part is dismembered (detached from body tree).
        /// A part is dismembered if it has no parent part and is not the root.
        /// </summary>
        public bool IsDismembered
        {
            get
            {
                if (ParentPart == null)
                {
                    if (ParentBody != null)
                        return ParentBody.GetBody() != this;
                    return true;
                }
                return false;
            }
        }

        // --- Laterality ---

        public int GetLaterality() => _Laterality;

        public void SetLaterality(int value)
        {
            _Laterality = value;
        }

        /// <summary>
        /// Get the display name with laterality prefix (e.g. "left hand").
        /// </summary>
        public string GetDisplayName()
        {
            string adj = Laterality.GetAdjective(_Laterality);
            if (!string.IsNullOrEmpty(adj))
                return adj + " " + Name;
            return Name;
        }

        // --- Child part management ---

        /// <summary>
        /// Add a child body part at the given position.
        /// Mirrors Qud's BodyPart.AddPart with position normalization.
        /// </summary>
        public BodyPart AddPart(BodyPart child, int position = -1)
        {
            if (Parts == null)
                Parts = new List<BodyPart>();

            child.ParentPart = this;
            child.ParentBody = ParentBody;

            if (position < 0)
            {
                child.Position = NextPosition();
                Parts.Add(child);
            }
            else
            {
                child.Position = position;
                // Insert in position order
                int insertAt = Parts.Count;
                for (int i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].Position > position)
                    {
                        insertAt = i;
                        break;
                    }
                }
                Parts.Insert(insertAt, child);
            }

            PropagateBody(child, ParentBody);
            return child;
        }

        /// <summary>
        /// Add a child body part by type string with common defaults.
        /// Convenience method for building anatomies.
        /// </summary>
        public BodyPart AddPart(string type, int laterality = 0, string name = null,
            string description = null, int position = -1)
        {
            var child = new BodyPart
            {
                Type = type,
                Name = name ?? type.ToLowerInvariant(),
                Description = description ?? type,
            };
            if (laterality != 0)
                child.SetLaterality(laterality);
            return AddPart(child, position);
        }

        /// <summary>
        /// Remove a direct child part. Returns true if found and removed.
        /// </summary>
        public bool RemovePart(BodyPart child)
        {
            if (Parts == null) return false;
            if (!Parts.Remove(child)) return false;
            child.ParentPart = null;
            return true;
        }

        /// <summary>
        /// Remove all child parts with the given manager ID. Recursive.
        /// Mirrors Qud's Body.RemoveBodyPartsByManager.
        /// </summary>
        public int RemovePartsByManager(string manager, bool evenIfDismembered = false)
        {
            int count = 0;
            if (Parts == null) return 0;

            for (int i = Parts.Count - 1; i >= 0; i--)
            {
                var part = Parts[i];
                // Recurse first
                count += part.RemovePartsByManager(manager, evenIfDismembered);

                if (part.Manager == manager)
                {
                    // Unequip before removing
                    part.ClearEquipment();
                    Parts.RemoveAt(i);
                    part.ParentPart = null;
                    count++;
                }
            }
            return count;
        }

        // --- Tree traversal ---

        /// <summary>
        /// Get a flat list of all body parts in the tree (depth-first).
        /// Mirrors Qud's BodyPart.GetParts / Body.GetParts.
        /// </summary>
        public List<BodyPart> GetParts(List<BodyPart> result = null)
        {
            if (result == null)
                result = new List<BodyPart>();
            result.Add(this);
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].GetParts(result);
            }
            return result;
        }

        /// <summary>
        /// Find the first body part of the given type in this subtree.
        /// </summary>
        public BodyPart GetPartByType(string type)
        {
            if (Type == type) return this;
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                {
                    var found = Parts[i].GetPartByType(type);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Find all body parts of the given type in this subtree.
        /// </summary>
        public List<BodyPart> GetPartsByType(string type, List<BodyPart> result = null)
        {
            if (result == null)
                result = new List<BodyPart>();
            if (Type == type)
                result.Add(this);
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].GetPartsByType(type, result);
            }
            return result;
        }

        /// <summary>
        /// Count body parts of the given type in this subtree.
        /// </summary>
        public int CountParts(string type)
        {
            int count = (Type == type) ? 1 : 0;
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    count += Parts[i].CountParts(type);
            }
            return count;
        }

        /// <summary>
        /// Get the previous sibling part, or null.
        /// </summary>
        public BodyPart GetPreviousPart()
        {
            if (ParentPart?.Parts == null) return null;
            int idx = ParentPart.Parts.IndexOf(this);
            if (idx > 0) return ParentPart.Parts[idx - 1];
            return null;
        }

        /// <summary>
        /// Get the next sibling part, or null.
        /// </summary>
        public BodyPart GetNextPart()
        {
            if (ParentPart?.Parts == null) return null;
            int idx = ParentPart.Parts.IndexOf(this);
            if (idx >= 0 && idx < ParentPart.Parts.Count - 1)
                return ParentPart.Parts[idx + 1];
            return null;
        }

        // --- Severability and dependency ---

        /// <summary>
        /// Whether this body part can be severed/dismembered.
        /// Mirrors Qud's BodyPart.IsSeverable():
        /// - not Abstract
        /// - is Appendage
        /// - not Integral
        /// - not dependent (no DependsOn or RequiresType)
        /// </summary>
        public bool IsSeverable()
        {
            if (Abstract) return false;
            if (!Appendage) return false;
            if (Integral) return false;
            if (!string.IsNullOrEmpty(DependsOn)) return false;
            if (!string.IsNullOrEmpty(RequiresType)) return false;
            return true;
        }

        /// <summary>
        /// Whether severing this part requires a decapitate action (mortal parts).
        /// </summary>
        public bool SeverRequiresDecapitate()
        {
            return Mortal;
        }

        /// <summary>
        /// Whether this part can be regenerated after dismemberment.
        /// Not abstract, not dependent on other parts.
        /// </summary>
        public bool IsRegenerable()
        {
            if (Abstract) return false;
            if (!string.IsNullOrEmpty(DependsOn)) return false;
            if (!string.IsNullOrEmpty(RequiresType)) return false;
            return true;
        }

        /// <summary>
        /// Whether this part is concretely unsupported (its DependsOn target is missing).
        /// </summary>
        public bool IsConcretelyUnsupported()
        {
            if (string.IsNullOrEmpty(DependsOn)) return false;
            if (ParentBody == null) return false;

            var allParts = ParentBody.GetBody().GetParts();
            for (int i = 0; i < allParts.Count; i++)
            {
                if (allParts[i].SupportsDependent == DependsOn)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Whether this part is abstractly unsupported (its RequiresType is missing
        /// from the body with matching laterality).
        /// </summary>
        public bool IsAbstractlyUnsupported()
        {
            if (string.IsNullOrEmpty(RequiresType)) return false;
            if (ParentBody == null) return false;

            var allParts = ParentBody.GetBody().GetParts();
            for (int i = 0; i < allParts.Count; i++)
            {
                var p = allParts[i];
                if (p == this) continue;
                if (p.Type == RequiresType &&
                    Laterality.Match(p._Laterality, RequiresLaterality))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Whether this part is unsupported by either concrete or abstract dependency.
        /// </summary>
        public bool IsUnsupported()
        {
            return IsConcretelyUnsupported() || IsAbstractlyUnsupported();
        }

        /// <summary>
        /// Whether this part can receive a cybernetic implant.
        /// Mirrors Qud: not Extrinsic, Category == ANIMAL (1).
        /// </summary>
        public bool CanReceiveCyberneticImplant()
        {
            if (Extrinsic) return false;
            if (Category != BodyPartCategory.ANIMAL) return false;
            return true;
        }

        // --- Equipment helpers ---

        /// <summary>
        /// Set the equipped item on this body part.
        /// </summary>
        public void SetEquipped(Entity item)
        {
            _Equipped = item;
            if (item != null)
                FirstSlotForEquipped = true;
        }

        /// <summary>
        /// Clear the equipped item from this body part.
        /// </summary>
        public void ClearEquipped()
        {
            _Equipped = null;
            FirstSlotForEquipped = false;
        }

        /// <summary>
        /// Set the cybernetics implant on this body part.
        /// </summary>
        public void SetCybernetics(Entity implant)
        {
            _Cybernetics = implant;
            if (implant != null)
                FirstSlotForCybernetics = true;
        }

        /// <summary>
        /// Clear all equipment references on this part (and children recursively).
        /// </summary>
        public void ClearEquipment()
        {
            ClearEquipped();
            _Cybernetics = null;
            FirstSlotForCybernetics = false;
            _DefaultBehavior = null;
            FirstSlotForDefaultBehavior = false;

            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].ClearEquipment();
            }
        }

        /// <summary>
        /// Iterate all equipped entities in this subtree, calling action for each.
        /// Only calls for FirstSlotForEquipped to avoid duplicates.
        /// </summary>
        public void ForeachEquippedObject(Action<Entity, BodyPart> action)
        {
            if (_Equipped != null && FirstSlotForEquipped)
                action(_Equipped, this);
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].ForeachEquippedObject(action);
            }
        }

        /// <summary>
        /// Get all body parts where an item is currently equipped.
        /// </summary>
        public List<BodyPart> GetEquippedParts(List<BodyPart> result = null)
        {
            if (result == null)
                result = new List<BodyPart>();
            if (_Equipped != null && FirstSlotForEquipped)
                result.Add(this);
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].GetEquippedParts(result);
            }
            return result;
        }

        /// <summary>
        /// Find the first unoccupied body part of a given type in this subtree.
        /// </summary>
        public BodyPart FindFreeSlot(string type, int laterality = 0)
        {
            if (Type == type && _Equipped == null && !Abstract)
            {
                if (laterality == 0 || Laterality.Match(_Laterality, laterality))
                    return this;
            }
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                {
                    var found = Parts[i].FindFreeSlot(type, laterality);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Find all body parts of a given type that can accept equipment (free or occupied).
        /// Used for slot query during equip.
        /// </summary>
        public List<BodyPart> GetEquippableSlots(string type, List<BodyPart> result = null)
        {
            if (result == null)
                result = new List<BodyPart>();
            if (Type == type && !Abstract)
                result.Add(this);
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].GetEquippableSlots(type, result);
            }
            return result;
        }

        // --- Recalculate first-slot flags ---

        /// <summary>
        /// Recalculate FirstSlotForEquipped across all parts sharing the same equipped item.
        /// Mirrors Qud's BodyPart.RecalculateFirstEquipped.
        /// </summary>
        public void RecalculateFirstEquipped()
        {
            if (Parts == null) return;
            var seen = new HashSet<Entity>();
            RecalculateFirstEquippedInternal(seen);
        }

        private void RecalculateFirstEquippedInternal(HashSet<Entity> seen)
        {
            if (_Equipped != null)
            {
                FirstSlotForEquipped = seen.Add(_Equipped);
            }
            else
            {
                FirstSlotForEquipped = false;
            }
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].RecalculateFirstEquippedInternal(seen);
            }
        }

        /// <summary>
        /// Recalculate FirstSlotForDefaultBehavior across all parts sharing the same default behavior.
        /// Mirrors RecalculateFirstEquipped but for the _DefaultBehavior channel.
        /// </summary>
        public void RecalculateFirstDefaultBehavior()
        {
            var seen = new HashSet<Entity>();
            RecalculateFirstDefaultBehaviorInternal(seen);
        }

        private void RecalculateFirstDefaultBehaviorInternal(HashSet<Entity> seen)
        {
            if (_DefaultBehavior != null)
            {
                FirstSlotForDefaultBehavior = seen.Add(_DefaultBehavior);
            }
            else
            {
                FirstSlotForDefaultBehavior = false;
            }
            if (Parts != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                    Parts[i].RecalculateFirstDefaultBehaviorInternal(seen);
            }
        }

        // --- Clone ---

        /// <summary>
        /// Create a shallow clone of this body part (without children or parent refs).
        /// Used for position hinting during body rebuilds.
        /// </summary>
        public BodyPart Clone()
        {
            return new BodyPart
            {
                Type = Type,
                VariantType = VariantType,
                Description = Description,
                DescriptionPrefix = DescriptionPrefix,
                Name = Name,
                SupportsDependent = SupportsDependent,
                DependsOn = DependsOn,
                RequiresType = RequiresType,
                Manager = Manager,
                Category = Category,
                _Laterality = _Laterality,
                RequiresLaterality = RequiresLaterality,
                Mobility = Mobility,
                TargetWeight = TargetWeight,
                Flags = Flags,
                Position = Position,
                DefaultBehaviorBlueprint = DefaultBehaviorBlueprint,
            };
        }

        // --- Helpers ---

        private int NextPosition()
        {
            if (Parts == null || Parts.Count == 0)
                return 0;
            return Parts[Parts.Count - 1].Position + 1;
        }

        private static void PropagateBody(BodyPart part, Body body)
        {
            part.ParentBody = body;
            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    PropagateBody(part.Parts[i], body);
            }
        }

        public override string ToString()
        {
            return $"BodyPart({GetDisplayName()}, Type={Type})";
        }
    }
}
