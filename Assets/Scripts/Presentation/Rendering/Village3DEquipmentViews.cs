using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only diagnostic for native gear that has no usable view.
    /// These references describe the current graph; they are never persisted.</summary>
    public sealed class Village3DEquipmentFallback
    {
        public Entity Actor { get; }
        public Entity Item { get; }
        public string Reason { get; }
        internal Village3DEquipmentFallback(Entity actor, Entity item, string reason)
        { Actor = actor; Item = item; Reason = reason; }
    }

    /// <summary>Presentation-only attachment ownership. The caller brackets its
    /// currently bound actors with BeginSync/EndSync and prepares new model clones
    /// with its own layer, transient visibility mask and instance materials.
    /// No native equipment, physics ownership, statistics or save fields are written.</summary>
    internal sealed class Village3DEquipmentViews : IDisposable
    {
        private const string HandLeft = "Equipment.Hand.L", HandRight = "Equipment.Hand.R";
        private const string Head = "Equipment.Head", Back = "Equipment.Back";
        // Static equipment and the Generic rig preserve different import bases.
        // Compensate in socket-local space so the grip follows the animated hand
        // while the length remains readable from the overhead camera.
        private static readonly Quaternion WeaponTilt = Quaternion.Euler(-25f, 0, 0);
        private sealed class ItemView
        {
            public GameObject Root;
            public string ModelId, SocketName;
            public Village3DEquipmentFallback Failure;
        }
        private sealed class ActorView
        {
            public Entity Actor;
            public GameObject Root;
            public InventoryPart Inventory;
            public int Version;
            public bool HasSnapshot;
            public readonly Dictionary<string, Transform> Sockets = new Dictionary<string, Transform>(StringComparer.Ordinal);
            public readonly Dictionary<Entity, ItemView> Items = new Dictionary<Entity, ItemView>();
            public readonly HashSet<Entity> Seen = new HashSet<Entity>();
            public readonly HashSet<string> UsedSockets = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<Entity> RemovedItems = new List<Entity>(8);
            public readonly List<BodyPart> BodyParts = new List<BodyPart>(16);
            public Village3DEquipmentFallback NullItemFailure;
        }
        private readonly Village3DLibrary library;
        private readonly Action<GameObject> prepareModel;
        private readonly Dictionary<Entity, ActorView> actors = new Dictionary<Entity, ActorView>();
        private readonly HashSet<Entity> seenActors = new HashSet<Entity>();
        private readonly List<Entity> removedActors = new List<Entity>(16);
        private readonly List<Village3DEquipmentFallback> fallbacks = new List<Village3DEquipmentFallback>(32);
        private readonly ReadOnlyCollection<Village3DEquipmentFallback> readOnlyFallbacks;
        private int lastVersion, syncVersion;
        private bool forceScan, hasSnapshot;

        public Village3DEquipmentViews(Village3DLibrary library, Action<GameObject> prepareModel)
        {
            this.library = library ?? throw new ArgumentNullException(nameof(library));
            this.prepareModel = prepareModel ?? throw new ArgumentNullException(nameof(prepareModel));
            readOnlyFallbacks = fallbacks.AsReadOnly();
        }
        public int FallbackCount => fallbacks.Count;
        public IReadOnlyList<Village3DEquipmentFallback> Fallbacks => readOnlyFallbacks;
        public bool NeedsRefresh => !hasSnapshot || lastVersion != EquipmentChangeBus.GlobalVersion;

        /// <summary>force=true is for an explicit native render refresh. Besides
        /// membership it rechecks mutable classification (e.g. a re-forged equipped
        /// weapon), which does not necessarily publish an equipment-bus change.</summary>
        public void BeginSync(bool force)
        {
            seenActors.Clear(); forceScan = force; syncVersion = EquipmentChangeBus.GlobalVersion;
        }
        public void Sync(Entity actor, GameObject actorRoot)
        {
            if (actor == null || actorRoot == null || !seenActors.Add(actor)) return;
            if (!actors.TryGetValue(actor, out var state))
            {
                state = new ActorView { Actor = actor }; actors.Add(actor, state);
            }
            if (state.Root != actorRoot)
            {
                ClearItems(state); state.Sockets.Clear(); state.Root = actorRoot; state.HasSnapshot = false;
                foreach (var transform in actorRoot.GetComponentsInChildren<Transform>(true))
                    if ((transform.name == HandLeft || transform.name == HandRight || transform.name == Head || transform.name == Back)
                        && !state.Sockets.ContainsKey(transform.name)) state.Sockets.Add(transform.name, transform);
            }
            var inventory = actor.GetPart<InventoryPart>();
            if (!forceScan && state.HasSnapshot && state.Version == syncVersion && ReferenceEquals(state.Inventory, inventory)) return;
            state.Inventory = inventory; state.Seen.Clear(); state.UsedSockets.Clear(); state.BodyParts.Clear();
            var body = actor.GetPart<Body>();
            if (body != null) body.GetParts(state.BodyParts);
            bool hasNullItem = false;
            if (inventory?.EquippedItems != null)
            {
                foreach (var pair in inventory.EquippedItems)
                {
                    var item = pair.Value;
                    if (!state.Seen.Add(item)) continue; // One instance, including multi-slot equipment.
                    if (item == null) { hasNullItem = true; continue; }
                    if (!state.Items.TryGetValue(item, out var view)) { view = new ItemView(); state.Items.Add(item, view); }
                    var physics = item.GetPart<PhysicsPart>();
                    if (physics == null || !ReferenceEquals(physics.Equipped, actor) || physics.InInventory != null
                        || inventory.Objects?.Contains(item) == true || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                    { Fail(state, item, view, "native-ownership-mismatch"); continue; }
                    BodyPart occupied = null;
                    if (body != null)
                    {
                        for (int i = 0; i < state.BodyParts.Count; i++)
                        {
                            var part = state.BodyParts[i];
                            if (part.FirstSlotForEquipped && ReferenceEquals(part._Equipped, item)) { occupied = part; break; }
                        }
                        if (occupied == null) { Fail(state, item, view, "missing-native-body-slot"); continue; }
                    }
                    var equip = item.GetPart<EquippablePart>();
                    string slot = occupied?.Type ?? equip?.Slot;
                    if (equip == null || !TryModel(item, slot, out string modelId))
                    { Fail(state, item, view, "unsupported-equipped-item"); continue; }
                    string socketName = slot == "Head" ? Head : HandSocket(occupied, state.UsedSockets);
                    if (!state.Sockets.TryGetValue(socketName, out var socket) || socket == null)
                    { Fail(state, item, view, "missing-rig-socket"); continue; }
                    if (!state.UsedSockets.Add(socketName))
                    { Fail(state, item, view, "occupied-rig-socket"); continue; }
                    var prefab = library.FindModel(modelId);
                    if (prefab == null) { Fail(state, item, view, "missing-equipment-model"); continue; }
                    if (view.Root != null && view.ModelId == modelId && view.SocketName == socketName && view.Root.transform.parent == socket)
                    { view.Failure = null; continue; }
                    DisposeView(view);
                    view.Root = Object.Instantiate(prefab, socket, false);
                    view.Root.name = modelId + " [" + item.ID + "]";
                    view.Root.hideFlags = HideFlags.DontSave;
                    view.Root.transform.localPosition = Vector3.zero;
                    view.Root.transform.localRotation = modelId == "equipment-blade" || modelId == "equipment-club" || modelId == "equipment-staff"
                        ? WeaponTilt : Quaternion.identity;
                    view.Root.transform.localScale = Vector3.one;
                    try { prepareModel(view.Root); }
                    catch { DisposeView(view); throw; }
                    view.ModelId = modelId; view.SocketName = socketName; view.Failure = null;
                }
            }
            if (hasNullItem)
            {
                if (state.NullItemFailure == null) state.NullItemFailure = new Village3DEquipmentFallback(actor, null, "missing-native-item");
            }
            else state.NullItemFailure = null;
            state.RemovedItems.Clear();
            foreach (var pair in state.Items) if (!state.Seen.Contains(pair.Key)) state.RemovedItems.Add(pair.Key);
            foreach (var item in state.RemovedItems) { DisposeView(state.Items[item]); state.Items.Remove(item); }
            state.Version = syncVersion; state.HasSnapshot = true;
        }
        public void EndSync()
        {
            removedActors.Clear();
            foreach (var pair in actors) if (!seenActors.Contains(pair.Key) || pair.Value.Root == null) removedActors.Add(pair.Key);
            foreach (var actor in removedActors) { ClearItems(actors[actor]); actors.Remove(actor); }
            fallbacks.Clear();
            foreach (var state in actors.Values)
            {
                if (state.NullItemFailure != null) fallbacks.Add(state.NullItemFailure);
                foreach (var item in state.Items.Values) if (item.Failure != null) fallbacks.Add(item.Failure);
            }
            lastVersion = syncVersion; hasSnapshot = true;
        }
        /// <summary>Diagnostic lookup, including an inactive view beneath a hidden
        /// actor. Rendering/selection visibility is still owned by its actor root.</summary>
        public bool TryGet(Entity actor, Entity item, out GameObject root)
        {
            root = null;
            if (actor == null || item == null || !actors.TryGetValue(actor, out var state)
                || !state.Items.TryGetValue(item, out var view) || view.Root == null) return false;
            root = view.Root; return true;
        }
        private static void Fail(ActorView actor, Entity item, ItemView view, string reason)
        {
            DisposeView(view);
            if (view.Failure == null || view.Failure.Reason != reason)
                view.Failure = new Village3DEquipmentFallback(actor.Actor, item, reason);
        }
        private static string HandSocket(BodyPart part, HashSet<string> occupied)
        {
            int side = part?.GetLaterality() ?? Laterality.NONE;
            if (side != Laterality.ANY && (side & Laterality.LEFT) != 0) return HandLeft;
            if (side != Laterality.ANY && (side & Laterality.RIGHT) != 0) return HandRight;
            return occupied.Contains(HandRight) ? HandLeft : HandRight;
        }
        private static bool TryModel(Entity item, string slot, out string modelId)
        {
            modelId = null;
            if (slot == "Head" && item.HasPart<ArmorPart>()) { modelId = "equipment-helmet"; return true; }
            if (slot != "Hand") return false;
            if (item.HasPart<ArmorPart>() && (item.BlueprintName == "Buckler" || item.BlueprintName == "IronBuckler"
                || ItemCategory.GetCategory(item) == ItemCategory.Shields)) { modelId = "equipment-shield"; return true; }
            var weapon = item.GetPart<MeleeWeaponPart>();
            if (weapon == null || item.HasTag("Natural")) return false;
            // These shipped polearms share Piercing with daggers. CryoLance also
            // advertises LongBlades for combat, so geometry aliases take precedence.
            switch (item.BlueprintName)
            {
                case "Spear": case "LoanerSpear": case "EmberSpear": case "CryoLance": case "FirstRootGlaive":
                    modelId = "equipment-staff"; return true;
                case "Dagger": case "LoanerDagger": case "AcidicDagger": case "VenomDagger":
                case "EchoKnife": case "GlassblownStiletto": case "LoanerLongsword":
                    modelId = "equipment-blade"; return true;
            }
            if (HasToken(weapon.Attributes, "LongBlades") || HasToken(weapon.Attributes, "ShortBlades")) modelId = "equipment-blade";
            else if (HasToken(weapon.Attributes, "Cudgel") || HasToken(weapon.Attributes, "Bludgeoning")) modelId = "equipment-club";
            else if (HasToken(weapon.Attributes, "Glaive")) modelId = "equipment-staff";
            return modelId != null;
        }
        private static bool HasToken(string text, string token)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int start = 0;
            while ((start = text.IndexOf(token, start, StringComparison.Ordinal)) >= 0)
            {
                int end = start + token.Length;
                if ((start == 0 || char.IsWhiteSpace(text[start - 1])) && (end == text.Length || char.IsWhiteSpace(text[end]))) return true;
                start = end;
            }
            return false;
        }
        private static void DisposeView(ItemView view)
        {
            if (view.Root != null)
            {
                view.Root.SetActive(false);
                if (Application.isPlaying) Object.Destroy(view.Root); else Object.DestroyImmediate(view.Root);
            }
            view.Root = null; view.ModelId = view.SocketName = null;
        }
        private static void ClearItems(ActorView state)
        {
            foreach (var view in state.Items.Values) DisposeView(view);
            state.Items.Clear(); state.Seen.Clear(); state.UsedSockets.Clear(); state.RemovedItems.Clear(); state.BodyParts.Clear();
            state.NullItemFailure = null; state.Inventory = null;
        }
        public void Dispose()
        {
            foreach (var state in actors.Values) ClearItems(state);
            actors.Clear(); seenActors.Clear(); removedActors.Clear(); fallbacks.Clear(); hasSnapshot = false;
        }
    }
}
