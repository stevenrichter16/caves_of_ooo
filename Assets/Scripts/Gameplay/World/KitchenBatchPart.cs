using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public enum KitchenBatchPresentation { None, CoveredWorking, ReadyParcel }

    /// <summary>One optional kitchen commission on exact saved owners. Ingredients
    /// remain physical escrow until completion; time never creates another job.</summary>
    public sealed class KitchenBatchPart : Part
    {
        public override string Name => "KitchenBatch";
        public const string StartCommand = "StartKitchenBatch", KitchenZoneID = "Overworld.12.11.0";
        public const int PreparationTicks = 120, Fee = 2;
        public bool Configured;
        public string ZoneID, StationID, WorkerID, EscrowID, PickupID, CommissionerID, OutputID;
        public Entity Worker, Escrow, Pickup, Commissioner, Output;
        public string State = "Idle", BlockedReason = "";
        public int JobOrdinal, StartTick, DueTick, StationX, StationY, EscrowX, EscrowY, PickupX, PickupY;
        public List<Entity> Inputs = new List<Entity>();
        public List<string> InputIDs = new List<string>();
        public List<int> InputCounts = new List<int>();
        [NonSerialized] private bool reconciling;
        [NonSerialized] private bool invalidatedDuringReconcile;

        const string BrokenPanName = "split clay batch pan";
        const string PanDescriptionStart = "A shallow clay pan for wrapped expedition meals. The open oven beside it remains free to use. ";
        const string PanCommissionDetails = "two emberwheat, one claspbean pulp and two drams. A batch takes 120 world ticks. Leave the tied ingredients alone; interruption returns only what remains, and the fee is kept.";
        const string BrokenPanDescription = PanDescriptionStart + "Repair the split with two fire clay, then commission Orven: " + PanCommissionDetails;
        const string RepairedPanDescription = PanDescriptionStart + "Commission Orven: " + PanCommissionDetails;

        // Presentation only: repaired saves retain their original authored fields.
        // This must never bind or reconcile a kitchen job merely to read its label.
        bool HasRepairedPanReadout()
        {
            var owner = ParentEntity;
            if (owner?.BlueprintName != "ConnectedBatchPan" || owner.GetPart<KitchenBatchPart>() != this) return false;
            RepairablePart fault = null;
            for (int i = 0; i < owner.Parts.Count; i++)
            {
                if (owner.Parts[i] is not RepairablePart candidate) continue;
                if (fault != null) return false;
                fault = candidate;
            }
            return fault?.ParentEntity == owner && !fault.PortableEquipment
                && fault.RecipeId == "clay-batch-pan" && fault.Repaired;
        }

        /// <summary>Projects only the canonical pan label; custom names and saved fields remain unchanged.</summary>
        internal string ResolveDisplayName(string authoredName) => authoredName == BrokenPanName && HasRepairedPanReadout()
            ? "clay batch pan" : authoredName;

        /// <summary>Removes the obsolete repair instruction only from the canonical repaired-pan prose.</summary>
        internal string ResolveDescription(string authoredText) => authoredText == BrokenPanDescription && HasRepairedPanReadout()
            ? RepairedPanDescription : authoredText;

        /// <summary>Pure cues for a current disclosed pan or pickup. The caller
        /// retains art admission/top-owner authority. This never reconciles a
        /// job: covered means exact paid escrow, and ready means the commissioned
        /// parcel is still physically on its board. The existing part index has
        /// one lazy discovery pass; subsequent reads inspect only batch owners.</summary>
        public static bool TryGetPresentation(Zone zone, Entity subject, out KitchenBatchPresentation presentation)
        {
            presentation = KitchenBatchPresentation.None;
            if (zone?.ZoneID != KitchenZoneID || subject == null
                || subject.BlueprintName != "ConnectedBatchPan" && subject.BlueprintName != "ConnectedKitchenPickup") return false;
            var cell = zone.GetEntityCell(subject);
            if (cell == null || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell
                || !cell.Explored || !cell.IsVisible || !cell.Objects.Contains(subject) || subject.SpatialZone != zone) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager != null && (!manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || current != zone)) return false;
            KitchenBatchPart found = null;
            var owners = zone.GetReadOnlyEntitiesWithPart<KitchenBatchPart>();
            for (int i = 0; i < owners.Count; i++)
            {
                var owner = owners[i]; var candidate = PresentationPart<KitchenBatchPart>(owner);
                if (candidate == null || !candidate.Configured || candidate.StationID != owner.ID) continue;
                if (found != null) return false;
                found = candidate;
            }
            if (found == null || found.ZoneID != KitchenZoneID
                || !found.PresentationFixedOwner(found.ParentEntity, zone, "ConnectedBatchPan", found.StationID, found.StationX, found.StationY)
                || !found.PresentationFixedOwner(found.Pickup, zone, "ConnectedKitchenPickup", found.PickupID, found.PickupX, found.PickupY)
                || found.StationID == found.PickupID) return false;
            if (subject == found.ParentEntity && found.PresentationWorking(zone))
                presentation = KitchenBatchPresentation.CoveredWorking;
            else if (subject == found.Pickup && found.PresentationOutput(zone))
                presentation = KitchenBatchPresentation.ReadyParcel;
            return presentation != KitchenBatchPresentation.None;
        }

        static T PresentationPart<T>(Entity owner) where T : Part
        {
            if (owner == null) return null;
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        bool PresentationFixedOwner(Entity owner, Zone zone, string blueprint, string id, int x, int y)
        {
            if (owner == null || string.IsNullOrEmpty(id)) return false;
            var cell = zone.GetEntityCell(owner);
            return owner.ID == id && Ground(owner, zone, blueprint)
                && cell != null && cell.X == x && cell.Y == y && zone.GetCell(x, y) == cell;
        }
        static bool PresentationLiveStructure(Entity owner)
        {
            var part = owner.GetPart<DestructiblePart>();
            return part == null || part.ParentEntity == owner && !part.Gone && part.HP > 0;
        }
        // Same trimmed, ordinal comma-token semantics as Composition.Contains,
        // without its Split/Trim allocations in repeated presentation reads.
        internal static bool HasMasonryComposition(CompositionPart composition)
        {
            string raw = composition?.MaterialsRaw;
            if (string.IsNullOrEmpty(raw)) return false;
            int start = 0;
            for (int end = 0; end <= raw.Length; end++)
            {
                if (end < raw.Length && raw[end] != ',') continue;
                int left = start, right = end - 1;
                while (left <= right && char.IsWhiteSpace(raw[left])) left++;
                while (right >= left && char.IsWhiteSpace(raw[right])) right--;
                if (right - left + 1 == 7 && string.CompareOrdinal(raw, left, "Masonry", 0, 7) == 0) return true;
                start = end + 1;
            }
            return false;
        }
        bool PresentationWorking(Zone zone)
        {
            var repair = PresentationPart<RepairablePart>(ParentEntity);
            var material = PresentationPart<CompositionPart>(ParentEntity);
            var input = PresentationPart<ContainerPart>(Escrow); var pickup = PresentationPart<ContainerPart>(Pickup);
            if (!ValidJob() || repair == null || !repair.Repaired || repair.PortableEquipment || repair.RecipeId != "clay-batch-pan"
                || !HasMasonryComposition(material)
                || Worker?.ID != WorkerID || string.IsNullOrEmpty(WorkerID) || !Ground(Worker, zone, "SpreadWaysideCook")
                || zone.GetEntityCell(Worker) is not Cell workerCell || zone.GetCell(workerCell.X, workerCell.Y) != workerCell
                || !PresentationFixedOwner(Escrow, zone, "ConnectedKitchenEscrow", EscrowID, EscrowX, EscrowY)
                || StationID == WorkerID || StationID == EscrowID || WorkerID == EscrowID || WorkerID == PickupID || EscrowID == PickupID
                || input == null || pickup == null || input == pickup || input.Contents == null || pickup.Contents == null
                || input.Contents == pickup.Contents || !PresentationLiveStructure(ParentEntity)
                || !PresentationLiveStructure(Escrow) || !PresentationLiveStructure(Pickup)
                || Inputs == null || InputIDs == null || InputCounts == null || Inputs.Count < 2 || Inputs.Count > 3
                || Inputs.Count != InputIDs.Count || Inputs.Count != InputCounts.Count || input.Contents.Count != Inputs.Count) return false;
            int grain = 0, pulp = 0;
            for (int i = 0; i < Inputs.Count; i++)
            {
                var item = Inputs[i];
                if (!PresentationPortable(item) || item.ID != InputIDs[i] || Count(item) != InputCounts[i]
                    || item.GetPart<PhysicsPart>().InInventory != Escrow) return false;
                for (int j = 0; j < i; j++) if (Inputs[j] == item || InputIDs[j] == item.ID) return false;
                int matches = 0;
                for (int j = 0; j < input.Contents.Count; j++) if (input.Contents[j] == item) matches++;
                if (matches != 1) return false;
                if (item.BlueprintName == "Emberwheat") { if (InputCounts[i] > 2) return false; grain += InputCounts[i]; }
                else if (item.BlueprintName == "ClaspbeanPulp") { if (InputCounts[i] != 1) return false; pulp++; }
                else return false;
            }
            return grain == 2 && pulp == 1;
        }
        static bool PresentationPortable(Entity item)
        {
            if (!Portable(item) || PresentationPart<PhysicsPart>(item) == null) return false;
            var stack = item.GetPart<StackerPart>(); var damage = item.GetPart<DestructiblePart>();
            return (stack == null || PresentationPart<StackerPart>(item) == stack)
                && (damage == null || PresentationPart<DestructiblePart>(item) == damage);
        }
        bool PresentationOutput(Zone zone)
        {
            var store = PresentationPart<ContainerPart>(Pickup);
            if (Commissioner == null || string.IsNullOrEmpty(CommissionerID) || Output == null || string.IsNullOrEmpty(OutputID)
                || store?.Contents == null || !PresentationPortable(Output) || Count(Output) != 1
                || PresentationPart<FieldMealPart>(Output) == null || Output.GetPart<PhysicsPart>().InInventory != Pickup
                || !OwnsCommissionedMeal(Commissioner, Output, zone)) return false;
            int matches = 0;
            for (int i = 0; i < store.Contents.Count; i++)
            {
                var item = store.Contents[i];
                if (item != Output && item?.ID != OutputID) continue;
                if (item != Output || ++matches != 1) return false;
            }
            return matches == 1;
        }

        /// <summary>Called once by generation after the four real owners are placed.
        /// It binds a broken pan too; only starting work requires its repair.</summary>
        public bool Configure(Zone zone, Entity worker, Entity escrow, Entity pickup)
        {
            if (Configured || zone?.ZoneID != KitchenZoneID || ParentEntity?.GetPart<KitchenBatchPart>() != this
                || !Ground(ParentEntity, zone, "ConnectedBatchPan") || !Ground(worker, zone, "SpreadWaysideCook")
                || !Ground(escrow, zone, "ConnectedKitchenEscrow") || !Ground(pickup, zone, "ConnectedKitchenPickup")
                || new[] { ParentEntity.ID, worker.ID, escrow.ID, pickup.ID }.Distinct().Count() != 4
                || worker.GetPart<InventoryPart>()?.ParentEntity != worker || worker.GetPart<BrainPart>()?.ParentEntity != worker
                || !Structure() || escrow.GetPart<ContainerPart>() is not ContainerPart input
                || pickup.GetPart<ContainerPart>() is not ContainerPart output || input == output
                || input.Contents == output.Contents || input.Contents.Count != 0 || output.Contents.Count != 0) return false;
            ZoneID = zone.ZoneID; StationID = ParentEntity.ID;
            Worker = worker; WorkerID = worker.ID; Escrow = escrow; EscrowID = escrow.ID; Pickup = pickup; PickupID = pickup.ID;
            var at = zone.GetEntityPosition(ParentEntity); StationX = at.x; StationY = at.y;
            at = zone.GetEntityPosition(escrow); EscrowX = at.x; EscrowY = at.y;
            at = zone.GetEntityPosition(pickup); PickupX = at.x; PickupY = at.y;
            Configured = true; return true;
        }

        static bool Ground(Entity owner, Zone zone, string blueprint)
        {
            if (owner == null || zone == null) return false;
            var physics = owner?.GetPart<PhysicsPart>(); var cell = zone?.GetEntityCell(owner);
            return owner != null && !string.IsNullOrEmpty(owner.ID) && owner.BlueprintName == blueprint
                && owner.SpatialZone == zone && cell?.ParentZone == zone && cell.Objects.Contains(owner)
                && physics?.ParentEntity == owner && !physics.Takeable && physics.InInventory == null && physics.Equipped == null
                && !owner.HasPart<SpatialFootprintPart>();
        }
        bool Structure() => ParentEntity.Parts.Count(p => p is RepairablePart) == 1
            && ParentEntity.Parts.Count(p => p is CompositionPart) == 1
            && ParentEntity.GetPart<CompositionPart>()?.Contains("Masonry") == true;
        bool Bound(Zone zone)
        {
            if (!Configured || ZoneID != KitchenZoneID || zone?.ZoneID != ZoneID || ParentEntity?.ID != StationID
                || ParentEntity.GetPart<KitchenBatchPart>() != this || !Ground(ParentEntity, zone, "ConnectedBatchPan")
                || !Structure() || zone.GetEntityPosition(ParentEntity) != (StationX, StationY)
                || Worker?.ID != WorkerID || !Ground(Worker, zone, "SpreadWaysideCook")
                || Escrow?.ID != EscrowID || !Ground(Escrow, zone, "ConnectedKitchenEscrow")
                || Pickup?.ID != PickupID || !Ground(Pickup, zone, "ConnectedKitchenPickup")
                || zone.GetEntityPosition(Escrow) != (EscrowX, EscrowY) || zone.GetEntityPosition(Pickup) != (PickupX, PickupY)
                || Escrow.GetPart<ContainerPart>() is not ContainerPart input || input.ParentEntity != Escrow
                || Pickup.GetPart<ContainerPart>() is not ContainerPart output || output.ParentEntity != Pickup
                || input == output || input.Contents == output.Contents) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone;
        }
        static bool Alive(Entity actor) => actor != null && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor);
        static bool Friendly(Entity worker, Entity actor) => actor != null
            && !FactionManager.IsHostile(worker, actor) && !FactionManager.IsHostile(actor, worker)
            && worker.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor) != true
            && actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(worker) != true;
        bool StaffAvailable(Zone zone, Entity actor, Entity invalidating = null) => Worker.GetPart<BrainPart>()?.ParentEntity == Worker
            && Worker.GetPart<InventoryPart>()?.ParentEntity == Worker && (Worker == invalidating || Alive(Worker))
            && SpatialQuery.Distance(zone, Worker, ParentEntity) <= 2 && Friendly(Worker, actor);
        bool LiveStructure(Entity invalidating = null)
        {
            foreach (var owner in new[] { ParentEntity, Escrow, Pickup })
                if (owner != invalidating && owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)) return false;
            return !RepairablePart.BlocksFunction(ParentEntity);
        }
        bool ActorAvailable(Entity actor, Zone zone) => Alive(actor) && actor.HasTag("Player")
            && actor.SpatialZone == zone && zone.GetEntityCell(actor)?.Objects.Contains(actor) == true
            && actor.GetPart<InventoryPart>()?.ParentEntity == actor && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
            && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;
        static int Count(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 1;
        static bool Portable(Entity item) => item != null && !string.IsNullOrEmpty(item.ID) && !item.HasTag("Creature")
            && !item.HasTag("Terrain") && item.GetPart<PhysicsPart>() is PhysicsPart p && p.ParentEntity == item
            && p.Takeable && !p.Solid && item.SpatialZone == null && p.Equipped == null
            && !item.HasPart<SpatialFootprintPart>() && (item.GetPart<DestructiblePart>() is not DestructiblePart damage || !damage.Gone && damage.HP > 0)
            && (item.GetPart<StackerPart>() is not StackerPart stack || stack.ParentEntity == item && stack.StackCount > 0 && stack.StackCount <= stack.MaxStack);
        static bool Carried(Entity item, Entity actor, InventoryPart pack) => Portable(item) && pack?.ParentEntity == actor
            && item.GetPart<PhysicsPart>().InInventory == actor && pack.CanConsumeOne(item)
            && !pack.EquippedItems.ContainsValue(item) && pack.FindEquippedBodyPart(item) == null
            && pack.Objects.Count(e => e == item || e?.ID == item.ID) == 1;
        static List<(Entity item, int count)> SelectInputs(Entity actor)
        {
            var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return null;
            var selected = new List<(Entity, int)>();
            foreach (var recipe in new[] { ("Emberwheat", 2), ("ClaspbeanPulp", 1) })
            {
                int remaining = recipe.Item2;
                foreach (var item in pack.Objects)
                {
                    if (item?.BlueprintName != recipe.Item1 || !Carried(item, actor, pack)) continue;
                    int quantity = Math.Min(remaining, Count(item)); selected.Add((item, quantity)); remaining -= quantity;
                    if (remaining == 0) break;
                }
                if (remaining != 0) return null;
            }
            return selected;
        }
        bool StartContext(Entity actor, Zone zone) => !reconciling && Bound(zone) && State == "Idle" && LiveStructure()
            && ActorAvailable(actor, zone) && StaffAvailable(zone, actor) && JobOrdinal >= 0 && JobOrdinal < int.MaxValue
            && WorldClock.CurrentTick >= 0 && WorldClock.CurrentTick <= int.MaxValue - PreparationTicks
            && Escrow.GetPart<ContainerPart>().Contents.Count == 0 && Pickup.GetPart<ContainerPart>().Contents.Count == 0
            && Pickup.GetPart<ContainerPart>().MaxItems != 0 && !Pickup.GetPart<ContainerPart>().IsLocked
            && TradeSystem.GetDrams(actor) >= Fee && TradeSystem.GetDrams(Worker) <= int.MaxValue - Fee;

        /// <summary>Pure availability query: never advances time, creates output or consumes.</summary>
        public bool CanStart(Entity actor, Zone zone)
        {
            if (!StartContext(actor, zone)) return false;
            var selected = SelectInputs(actor); var container = Escrow.GetPart<ContainerPart>();
            return selected != null && (container.MaxItems < 0 || container.MaxItems >= selected.Count);
        }
        public bool TryStart(Entity actor, Zone zone) => CanStart(actor, zone)
            && InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity, StartCommand), actor, zone).Success;

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? ParentEntity?.SpatialZone;
            if (e.ID == "GetInventoryActions")
            {
                if (CanStart(actor, zone)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("KitchenBatch", "prepare field meal (2 grain, 1 claspbean, 2 drams)", StartCommand, 'b', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != StartCommand) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !Begin(actor, zone, tx)) return true;
            e.Handled = true; return false;
        }
        bool ClaimOwners(InventoryTransaction tx, Entity actor) => tx.TryClaim(ParentEntity, actor, StartCommand)
            && tx.TryClaim(Worker, actor, StartCommand) && tx.TryClaim(Escrow, actor, StartCommand) && tx.TryClaim(Pickup, actor, StartCommand);
        bool Begin(Entity actor, Zone zone, InventoryTransaction tx)
        {
            if (!CanStart(actor, zone) || !ClaimOwners(tx, actor) || !tx.TryClaim(actor, actor, StartCommand)) return Reject(actor, "unavailable");
            var selected = SelectInputs(actor); var pack = actor.GetPart<InventoryPart>(); var store = Escrow.GetPart<ContainerPart>();
            foreach (var entry in selected) if (!tx.TryClaim(entry.item, actor, StartCommand)) return Reject(actor, "ingredient-busy");
            foreach (var entry in selected)
            {
                Entity moved = null; var source = InventoryTransferSnapshot.Capture(pack, entry.item); tx.Do(null, source.Restore);
                bool removed = source.Apply(() =>
                {
                    if (!Carried(entry.item, actor, pack) || Count(entry.item) < entry.count) return false;
                    if (Count(entry.item) > entry.count)
                    {
                        moved = entry.item.GetPart<StackerPart>().SplitStack(entry.count);
                        if (moved == null) return false;
                        var physical = moved.GetPart<PhysicsPart>(); if (physical == null) return false;
                        physical.InInventory = null; physical.Equipped = null; return true;
                    }
                    moved = entry.item; return pack.RemoveObject(moved);
                });
                if (!removed || !Portable(moved) || moved.BlueprintName != entry.item.BlueprintName || Count(moved) != entry.count
                    || moved.GetPart<PhysicsPart>().InInventory != null || !tx.TryClaim(moved, actor, StartCommand)) return Reject(actor, "ingredient-transfer");
                var destination = InventoryTransferSnapshot.Capture(store, moved); tx.Do(null, destination.Restore);
                if (!destination.Apply(() => store.AddItem(moved)) || !destination.ClaimChanges(tx, actor, StartCommand)) return Reject(actor, "escrow-full");
            }
            // This is deliberately after split/stack callbacks. They may not turn a
            // removed worker, moved station or revised recipe into a paid commission.
            if (!Bound(zone) || State != "Idle" || !LiveStructure() || !ActorAvailable(actor, zone) || !StaffAvailable(zone, actor)
                || actor.GetPart<InventoryPart>() != pack || Escrow.GetPart<ContainerPart>() != store
                || Pickup.GetPart<ContainerPart>().Contents.Count != 0 || Pickup.GetPart<ContainerPart>().MaxItems == 0
                || !RecipeContents(store)) return Reject(actor, "source-changed");
            EnlistState(tx);
            Inputs = new List<Entity>(store.Contents); InputIDs = Inputs.Select(e => e.ID).ToList(); InputCounts = Inputs.Select(Count).ToList();
            Commissioner = actor; CommissionerID = actor.ID; Output = null; OutputID = null;
            StartTick = WorldClock.CurrentTick; DueTick = StartTick + PreparationTicks; JobOrdinal++;
            State = "Working"; BlockedReason = "";
            tx.DeferCurrencyTransfer(actor, Worker, Fee);
            tx.AfterCommit(() => { Changed(zone, "KitchenBatchStarted"); MessageLog.Add("The meal is set to work. Return in 120 world ticks. If work is interrupted, remaining ingredients are returned; the two-dram fee is retained."); });
            return true;
        }
        bool RecipeContents(ContainerPart store)
        {
            long grain = 0, pulp = 0; var ids = new HashSet<string>();
            foreach (var item in store.Contents)
            {
                if (!Portable(item) || item.GetPart<PhysicsPart>().InInventory != Escrow || !ids.Add(item.ID)) return false;
                if (item.BlueprintName == "Emberwheat") grain += Count(item);
                else if (item.BlueprintName == "ClaspbeanPulp") pulp += Count(item);
                else return false;
            }
            return grain == 2 && pulp == 1;
        }
        bool InputsCurrent()
        {
            var store = Escrow?.GetPart<ContainerPart>();
            if (store?.ParentEntity != Escrow || Inputs == null || InputIDs == null || InputCounts == null
                || Inputs.Count < 2 || Inputs.Count > 3 || Inputs.Count != InputIDs.Count || Inputs.Count != InputCounts.Count
                || store.Contents.Count != Inputs.Count || !RecipeContents(store)) return false;
            for (int i = 0; i < Inputs.Count; i++)
                if (Inputs[i]?.ID != InputIDs[i] || Count(Inputs[i]) != InputCounts[i] || !store.Contents.Contains(Inputs[i])) return false;
            return true;
        }
        bool ValidJob() => State == "Working" && JobOrdinal > 0 && StartTick >= 0 && (long)DueTick == (long)StartTick + PreparationTicks
            && Commissioner != null && Commissioner.ID == CommissionerID && !string.IsNullOrEmpty(CommissionerID);
        bool OutputStored() => Output != null && Output.ID == OutputID && Output.BlueprintName == "FieldMeal" && Portable(Output)
            && Output.GetPart<FieldMealPart>()?.ParentEntity == Output && Count(Output) == 1
            && Output.GetPart<PhysicsPart>().InInventory == Pickup && Pickup?.GetPart<ContainerPart>()?.Contents.Count(e => e == Output) == 1;

        /// <summary>Historical identity check for the successful transfer seam. It
        /// remains valid after transfer until turn reconciliation clears Ready.</summary>
        public bool OwnsCommissionedMeal(Entity actor, Entity item, Zone zone) => Configured && zone?.ZoneID == ZoneID && ZoneID == KitchenZoneID
            && ParentEntity?.ID == StationID && ParentEntity.GetPart<KitchenBatchPart>() == this
            && Ground(ParentEntity, zone, "ConnectedBatchPan") && zone.GetEntityPosition(ParentEntity) == (StationX, StationY)
            && Pickup?.ID == PickupID && Ground(Pickup, zone, "ConnectedKitchenPickup") && zone.GetEntityPosition(Pickup) == (PickupX, PickupY)
            && (WorldLocationContext.For(zone) is not OverworldZoneManager manager || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone)
            && State == "Ready" && JobOrdinal > 0
            && actor == Commissioner && actor?.ID == CommissionerID && item == Output && item?.ID == OutputID
            && item.BlueprintName == "FieldMeal" && item.GetProperty("KitchenBatch.Station") == StationID
            && item.GetIntProperty("KitchenBatch.Ordinal") == JobOrdinal && item.GetProperty("KitchenBatch.Customer") == CommissionerID;

        /// <summary>Arrival/player-boundary seam; queries must not call this.</summary>
        public void Reconcile(Zone zone) => ReconcileCore(zone, null);
        public static void ReconcileZone(Zone zone)
        {
            FindStation(zone)?.Reconcile(zone);
        }
        static KitchenBatchPart FindStation(Zone zone)
        {
            if (zone?.ZoneID != KitchenZoneID) return null;
            KitchenBatchPart found = null;
            foreach (var owner in zone.GetReadOnlyEntities())
            {
                var batch = owner.GetPart<KitchenBatchPart>();
                if (batch?.Configured != true || batch.StationID != owner.ID) continue;
                if (found != null) return null; // This authored place has one authority.
                found = batch;
            }
            return found;
        }
        /// <summary>Call before death loot/spill or supported structural removal.
        /// Lethal damage may already set HP to zero; this seam still knows the
        /// particular worker existed up to this tick.</summary>
        public static void BeforeOwnerInvalidated(Entity owner, Zone zone)
        {
            if (owner == null) return;
            var batch = FindStation(zone);
            if (batch != null && (owner == batch.ParentEntity || owner == batch.Worker || owner == batch.Escrow || owner == batch.Pickup))
                batch.ReconcileCore(zone, owner);
        }
        void ReconcileCore(Zone zone, Entity invalidating)
        {
            if (reconciling)
            {
                // Keep the recursion guard, but remember an irreversible owner
                // lifecycle during meal creation. Once that callback returns,
                // Bound may never hold again; this transaction must settle the
                // surviving escrow instead of leaving it behind a removed owner.
                if (invalidating != null) invalidatedDuringReconcile = true;
                return;
            }
            if (!Bound(zone)) return;
            reconciling = true; invalidatedDuringReconcile = false; var tx = new InventoryTransaction();
            try
            {
                if (!ClaimOwners(tx, Commissioner)) return;
                if (State == "Ready")
                {
                    if (!OutputStored()) { EnlistState(tx); ResetJob(); tx.AfterCommit(() => Changed(zone, "KitchenBatchCollected")); tx.Commit(); }
                    else if (invalidating == Pickup) { EnlistState(tx); ReleaseOne(tx, zone, Output); ResetJob(); tx.Commit(); }
                    return;
                }
                if (!ValidJob()) return;
                if (!InputsCurrent()) { Cancel(tx, zone, "escrow-altered"); tx.Commit(); return; }
                bool viable = LiveStructure(invalidating) && (Worker == invalidating || Alive(Worker));
                if (WorldClock.CurrentTick >= DueTick && viable && StaffAvailable(zone, Commissioner, invalidating)
                    && Complete(tx, zone, invalidating)) { tx.Commit(); return; }
                if (invalidating != null || invalidatedDuringReconcile || !viable) { Cancel(tx, zone, "work-interrupted"); tx.Commit(); }
            }
            catch (Exception error) { Diag.Record("furniture", "KitchenBatchRejected", target: ParentEntity, payload: new { reason = "reconcile-exception", error = error.GetType().Name }); }
            finally { tx.Rollback(); invalidatedDuringReconcile = false; reconciling = false; }
        }
        bool Complete(InventoryTransaction tx, Zone zone, Entity invalidating)
        {
            var store = Escrow.GetPart<ContainerPart>(); var pickup = Pickup.GetPart<ContainerPart>();
            if (pickup.Contents.Count != 0 || pickup.MaxItems == 0) { BlockedReason = "pickup-full"; return false; }
            EntityFactory factory = WorldLocationContext.For(zone)?.Factory ?? CropSystem.Factory;
            if (factory == null || !factory.Blueprints.ContainsKey("FieldMeal")) { BlockedReason = "meal-unavailable"; return false; }
            Entity meal;
            try { meal = factory.CreateEntity("FieldMeal"); }
            catch { BlockedReason = "meal-unavailable"; return false; }
            if (!Bound(zone) || !ValidJob() || !InputsCurrent() || !LiveStructure(invalidating) || !StaffAvailable(zone, Commissioner, invalidating)
                || Escrow.GetPart<ContainerPart>() != store || Pickup.GetPart<ContainerPart>() != pickup
                || !Portable(meal) || meal.BlueprintName != "FieldMeal" || meal.GetPart<FieldMealPart>()?.ParentEntity != meal
                || meal.GetPart<PhysicsPart>().InInventory != null || Count(meal) != 1
                || zone.GetReadOnlyEntities().Any(e => e.ID == meal.ID) || Inputs.Any(e => e.ID == meal.ID)
                || pickup.Contents.Count != 0 || pickup.MaxItems == 0 || !tx.TryClaim(meal, Commissioner, "KitchenComplete"))
            { BlockedReason = "completion-changed"; return false; }
            foreach (var input in Inputs) if (!tx.TryClaim(input, Commissioner, "KitchenComplete")) return false;
            var inputReceipt = InventoryTransferSnapshot.Capture(store); var outputReceipt = InventoryTransferSnapshot.Capture(pickup, meal);
            tx.Do(null, inputReceipt.Restore); tx.Do(null, outputReceipt.Restore); EnlistState(tx);
            if (!inputReceipt.Apply(() => { foreach (var input in Inputs) if (!store.RemoveItem(input)) return false; return true; })
                || !outputReceipt.Apply(() => pickup.AddItem(meal)) || !outputReceipt.ClaimChanges(tx, Commissioner, "KitchenComplete"))
                throw new InvalidOperationException("Kitchen output could not be published.");
            meal.Properties["KitchenBatch.Station"] = StationID; meal.Properties["KitchenBatch.Customer"] = CommissionerID;
            meal.IntProperties["KitchenBatch.Ordinal"] = JobOrdinal;
            Inputs = new List<Entity>(); InputIDs = new List<string>(); InputCounts = new List<int>();
            Output = meal; OutputID = meal.ID; State = "Ready"; BlockedReason = "";
            // If the output vessel itself is being removed, finish first and
            // release the actual parcel instead of orphaning it in that vessel.
            if (invalidating == Pickup) { ReleaseOne(tx, zone, meal); ResetJob(); }
            tx.AfterCommit(() => Changed(zone, "KitchenBatchReady")); return true;
        }
        void Cancel(InventoryTransaction tx, Zone zone, string reason)
        {
            EnlistState(tx);
            var survivors = Inputs?.Distinct().ToArray() ?? Array.Empty<Entity>();
            ResetJob(); // Resolve before ground publication can expose salvage.
            foreach (var item in survivors) ReleaseOne(tx, zone, item);
            BlockedReason = reason; tx.AfterCommit(() => Changed(zone, "KitchenBatchCancelled"));
        }
        void ReleaseOne(InventoryTransaction tx, Zone zone, Entity item)
        {
            if (item == null) return;
            var physical = item.GetPart<PhysicsPart>();
            var container = physical?.InInventory == Escrow ? Escrow?.GetPart<ContainerPart>()
                : physical?.InInventory == Pickup ? Pickup?.GetPart<ContainerPart>() : null;
            if (container == null || !container.Contents.Contains(item) || item.SpatialZone != null || physical.Equipped != null) return;
            if (!tx.TryClaim(item, Commissioner, "KitchenSalvage")) throw new InvalidOperationException("Kitchen salvage is busy.");
            var receipt = InventoryTransferSnapshot.Capture(container, item); tx.Do(null, receipt.Restore);
            if (!receipt.Apply(() => container.RemoveItem(item))) throw new InvalidOperationException("Kitchen salvage could not be released.");
            if (!Portable(item)) return; // A zero/invalid stack is not new food.
            tx.Do(null, () => { if (item.SpatialZone == zone) zone.RemoveEntity(item); });
            if (!zone.AddEntity(item, StationX, StationY)) throw new InvalidOperationException("Kitchen salvage cannot be placed.");
        }
        void ResetJob()
        {
            State = "Idle"; BlockedReason = ""; Inputs = new List<Entity>(); InputIDs = new List<string>(); InputCounts = new List<int>();
            Output = null; OutputID = null; Commissioner = null; CommissionerID = null; StartTick = 0; DueTick = 0;
        }
        void EnlistState(InventoryTransaction tx)
        {
            string state = State, blocked = BlockedReason, customerID = CommissionerID, outputID = OutputID;
            var customer = Commissioner; var output = Output; var inputs = Inputs; var ids = InputIDs; var counts = InputCounts;
            int start = StartTick, due = DueTick, ordinal = JobOrdinal;
            tx.Do(null, () => { State = state; BlockedReason = blocked; Commissioner = customer; CommissionerID = customerID; Output = output; OutputID = outputID;
                Inputs = inputs; InputIDs = ids; InputCounts = counts; StartTick = start; DueTick = due; JobOrdinal = ordinal; });
        }
        void Changed(Zone zone, string kind)
        {
            Diag.Record("furniture", kind, Commissioner, ParentEntity, new { zoneId = ZoneID, ordinal = JobOrdinal, state = State, start = StartTick, due = DueTick, reason = BlockedReason });
            foreach (var owner in new[] { ParentEntity, Escrow, Pickup }) { var at = zone?.GetEntityCell(owner); if (at != null) ZoneRenderHooks.MarkCellDirty(at.X, at.Y, kind); }
        }
        bool Reject(Entity actor, string reason)
        { Diag.Record("furniture", "KitchenBatchRejected", actor, ParentEntity, new { zoneId = ZoneID, reason }); return false; }
        public string Describe()
        {
            if (State == "Ready") return OutputStored() ? "One wrapped field meal is ready in the pickup tray." : "The finished parcel has been taken.";
            if (State == "Working") return string.IsNullOrEmpty(BlockedReason)
                ? "A paid meal is covered on the pan. Ready in " + Math.Max(0L, (long)DueTick - WorldClock.CurrentTick) + " world ticks."
                : "The paid meal is waiting: " + BlockedReason.Replace('-', ' ') + ". Its remaining ingredients are held here.";
            return "Prepare a wrapped field meal: two emberwheat, one claspbean pulp and two drams. It takes 120 world ticks. Eat it in one action to heal 3d4 and stop ordinary bleeding. Freely toasting the two grains gives more healing, with the claspbean left as a separate treatment. Interrupted work returns remaining ingredients, but not the fee. The public oven remains free.";
        }
    }
}
