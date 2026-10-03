using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One repaired, staffed southern Sodden bench. Each preparation
    /// consumes actual carried ingredients and pays its saved worker atomically.</summary>
    public sealed class SoddenPreparationPart : Part
    {
        public override string Name => "SoddenPreparation";
        public const string PrepareCommand = "PrepareSoddenDressing";
        public const string StopZoneID = "Overworld.15.7.0";
        public const int Fee = 2;
        public bool Configured;
        public Entity Worker;
        public string WorkerID = "", ZoneID = "", StationID = "";
        public int StationX, StationY;

        /// <summary>Binds the actual placed cutter and bench once during fresh
        /// generation. Normal save fields retain this identity; no load-time rebinding.</summary>
        public bool Configure(Zone zone, Entity worker)
        {
            if (Configured || zone?.ZoneID != StopZoneID || !Station(zone) || !WorkerPresent(worker, zone)
                || worker == ParentEntity || worker.ID == ParentEntity.ID
                || SpatialQuery.Distance(zone, worker, ParentEntity) > 1) return false;
            var cell = zone.GetEntityCell(ParentEntity);
            Worker = worker; WorkerID = worker.ID; ZoneID = zone.ZoneID; StationID = ParentEntity.ID;
            StationX = cell.X; StationY = cell.Y; Configured = true;
            worker.GetPart<RenderPart>().DisplayName = "Sella, dressing keeper";
            var conversation = worker.GetPart<ConversationPart>();
            if (conversation != null) conversation.ConversationID = "SoddenSella_1";
            return true;
        }

        /// <summary>Read-only instructions for Examine; never repairs or produces goods.</summary>
        public string Describe() => (RepairablePart.BlocksFunction(ParentEntity)
            ? "The dressing bench needs two salvaged timber before it can be used. "
            : "The repaired dressing bench can prepare supplies while its keeper is beside it. ")
            + "One sumpsieve pad, one knotflax cord and two drams make one field dressing. Apply it to treat existing ordinary poison and bleeding together. It does not heal wounds or treat poison gas or fungal infection.";

        bool Station(Zone zone)
        {
            var owner = ParentEntity;
            return owner?.BlueprintName == "SoddenDressingBench" && owner.GetPart<SoddenPreparationPart>() == this
                && owner.Parts.Count(p => p is SoddenPreparationPart) == 1 && !owner.HasTag("Creature")
                && SoddenPreparationRules.Ground(owner, zone) && owner.Parts.Count(p => p is RepairablePart) == 1
                && owner.Parts.Count(p => p is CompositionPart) == 1
                && owner.GetPart<RepairablePart>()?.ParentEntity == owner
                && owner.GetPart<RepairablePart>().RecipeId == "timber-dressing-bench"
                && owner.GetPart<CompositionPart>()?.ParentEntity == owner && owner.GetPart<CompositionPart>().Contains("Wood");
        }
        static bool WorkerPresent(Entity worker, Zone zone) => worker?.BlueprintName == "PeatCutter"
            && worker.HasTag("Creature") && SoddenPreparationRules.Ground(worker, zone)
            && worker.GetPart<BrainPart>()?.ParentEntity == worker && worker.GetPart<InventoryPart>()?.ParentEntity == worker;
        bool Context(Entity actor, Zone zone)
        {
            if (actor == null || !Configured || ZoneID != StopZoneID || zone?.ZoneID != ZoneID || ParentEntity?.ID != StationID
                || !Station(zone) || zone.GetEntityPosition(ParentEntity) != (StationX, StationY)
                || RepairablePart.BlocksFunction(ParentEntity) || Worker?.ID != WorkerID || Worker == actor
                || WorkerID == StationID || !WorkerPresent(Worker, zone) || !SoddenPreparationRules.Alive(Worker)
                || Worker.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || SpatialQuery.Distance(zone, Worker, ParentEntity) > 1
                || !SoddenPreparationRules.Seen(ParentEntity, zone) || !SoddenPreparationRules.Seen(Worker, zone)
                || !SoddenPreparationRules.CurrentActor(actor, zone, zone?.GetEntityCell(actor))
                || SpatialQuery.Distance(zone, actor, ParentEntity) > 1) return false;
            return !FactionManager.IsHostile(Worker, actor) && !FactionManager.IsHostile(actor, Worker)
                && !Worker.GetPart<BrainPart>().IsPersonallyHostileTo(actor)
                && actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(Worker) != true;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? ParentEntity?.SpatialZone;
            if (e.ID == "GetInventoryActions")
            {
                if (Context(actor, zone)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("SoddenPreparation",
                    "prepare dressing (1 sumpsieve pad, 1 knotflax cord, 2 drams)", PrepareCommand, 'p', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != PrepareCommand) return true;
            if (e.GetParameter<bool>("SoddenPreparationAttempted")) return true;
            e.SetParameter("SoddenPreparationAttempted", true);
            if (!Prepare(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true; return false;
        }
        bool Prepare(Entity actor, Zone zone, InventoryTransaction tx)
        {
            if (tx == null || !Context(actor, zone)) return Reject(actor, "unavailable", "The repaired dressing bench needs its living keeper beside it.");
            var pack = actor.GetPart<InventoryPart>(); var factory = SeedPart.Factory; var worker = Worker;
            var actorCell = zone.GetEntityCell(actor);
            if (pack?.ParentEntity != actor || factory == null || !factory.Blueprints.ContainsKey("SoddenFieldDressing"))
                return Reject(actor, "missing-supplies", "The bench cannot prepare a usable dressing right now.");
            if (TradeSystem.GetDrams(actor) < Fee || (long)TradeSystem.GetDrams(worker) + Fee > int.MaxValue)
                return Reject(actor, "currency", "Preparing a dressing costs two drams; the keeper must be able to receive them.");
            var payment = new Entity[2]; var names = new[] { "SumpsievePad", "KnotflaxCord" };
            for (int i = 0; i < names.Length; i++)
            {
                payment[i] = pack.Objects.FirstOrDefault(item => item?.BlueprintName == names[i] && SoddenPreparationRules.Carried(actor, pack, item));
                if (payment[i] == null) return Reject(actor, "ingredients", "Bring one sumpsieve pad and one knotflax cord for each dressing.");
            }
            foreach (var item in payment.Concat(new[] { actor, ParentEntity, worker }))
                if (!tx.TryClaim(item, actor, PrepareCommand)) return Reject(actor, "in-progress", "Those supplies or that bench are already in use.");
            var quoted = payment.Select(item => new { item, id = item.ID, blueprint = item.BlueprintName,
                count = item.GetPart<StackerPart>()?.StackCount ?? 1, parts = item.Parts.ToArray() }).ToArray();
            var benchParts = ParentEntity.Parts.ToArray(); var workerParts = worker.Parts.ToArray();
            bool Current() => Context(actor, zone) && Worker == worker && SeedPart.Factory == factory
                && actor.GetPart<InventoryPart>() == pack && SoddenPreparationRules.CurrentActor(actor, zone, actorCell)
                && ParentEntity.Parts.SequenceEqual(benchParts) && worker.Parts.SequenceEqual(workerParts);
            var output = factory.CreateEntity("SoddenFieldDressing");
            if (!Current() || quoted.Any(q => !SoddenPreparationRules.Carried(actor, pack, q.item) || q.item.ID != q.id
                || q.item.BlueprintName != q.blueprint || !q.item.Parts.SequenceEqual(q.parts) || (q.item.GetPart<StackerPart>()?.StackCount ?? 1) != q.count))
                return Reject(actor, "source-changed", "The keeper, bench or supplies changed before preparation could finish.");
            if (!SoddenPreparationRules.FreshDressing(output) || pack.Objects.Any(item => item == output || item?.ID == output.ID)
                || zone.GetReadOnlyEntities().Any(item => item == output || item.ID == output.ID) || !tx.TryClaim(output, actor, PrepareCommand))
                return Reject(actor, "invalid-output", "The bench could not produce a usable dressing.");
            if (pack.Objects.Any(item => item?.BlueprintName == "SoddenFieldDressing"
                && (!SoddenPreparationRules.Carried(actor, pack, item) || !SoddenPreparationRules.Dressing(item))))
                return Reject(actor, "invalid-destination", "Your supplies cannot receive that dressing.");
            var receipt = InventoryTransferSnapshot.Capture(pack, output); tx.Do(null, receipt.Restore); Entity received = null;
            if (!receipt.Apply(() =>
            {
                foreach (var item in payment)
                    if (!SoddenPreparationRules.Carried(actor, pack, item) || !pack.TryConsumeOne(item)) return false;
                return pack.AddCraftedUnit(output, out received) && (pack.MaxWeight < 0 || pack.GetCarriedWeight() <= pack.MaxWeight);
            }) || !receipt.ClaimChanges(tx, actor, PrepareCommand) || !Current()
                || !SoddenPreparationRules.Carried(actor, pack, received) || !SoddenPreparationRules.Dressing(received))
                return Reject(actor, "transfer-refused", "Preparation failed; the supplies and fee are unchanged.");
            tx.DeferCurrencyTransfer(actor, worker, Fee);
            tx.AfterCommit(() =>
            {
                MessageLog.Add("Sella binds the sumpsieve pad with knotflax cord into one field dressing.");
                Diag.Record("furniture", "SoddenPrepared", actor, ParentEntity, new { workerId = worker.ID, fee = Fee, outputId = received.ID });
            });
            return true;
        }
        bool Reject(Entity actor, string reason, string message)
        { MessageLog.Add(message); Diag.Record("furniture", "SoddenPreparationRejected", actor, ParentEntity, new { reason }); return false; }
    }

    // Shared only by this bench and its dressing; there is no general recipe or service registry.
    internal static class SoddenPreparationRules
    {
        internal static bool Alive(Entity actor) => actor != null && actor.GetStatValue("Hitpoints", 0) > 0 && !CombatSystem.IsDeathHandled(actor);
        internal static bool CurrentActor(Entity actor, Zone zone, Cell anchor) => InkActionRules.CurrentActor(actor, zone, anchor)
            && actor.GetPart<InventoryPart>()?.ParentEntity == actor;
        internal static bool Carried(Entity actor, InventoryPart pack, Entity item) => InkActionRules.Carried(actor, pack, item)
            && pack.CanConsumeOne(item) && !item.HasPart<SpatialFootprintPart>()
            && (item.GetPart<DestructiblePart>() is not DestructiblePart d || !d.Gone && d.HP > 0);
        internal static bool Ground(Entity owner, Zone zone)
        {
            if (owner == null || zone == null) return false;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>(); var cell = zone.GetEntityCell(owner);
            return owner != null && !string.IsNullOrEmpty(owner.ID) && zone != null && owner.SpatialZone == zone
                && cell?.ParentZone == zone && cell.Objects.Contains(owner) && physics?.ParentEntity == owner
                && !physics.Takeable && physics.InInventory == null && physics.Equipped == null
                && render?.ParentEntity == owner && render.Visible && !owner.HasPart<SpatialFootprintPart>()
                && (owner.GetPart<DestructiblePart>() is not DestructiblePart d || !d.Gone && d.HP > 0);
        }
        internal static bool Seen(Entity owner, Zone zone) => zone.GetEntityCell(owner) is Cell cell && cell.IsVisible && cell.Explored;
        internal static bool Dressing(Entity item) => item?.BlueprintName == "SoddenFieldDressing"
            && item.GetPart<SoddenDressingPart>()?.ParentEntity == item && item.Parts.Count(p => p is SoddenDressingPart) == 1
            && !item.HasPart<TonicPart>() && !item.HasPart<FoodPart>();
        internal static bool FreshDressing(Entity item)
        {
            var physical = item?.GetPart<PhysicsPart>(); var stack = item?.GetPart<StackerPart>();
            return Dressing(item) && !string.IsNullOrEmpty(item.ID) && item.HasTag("Item") && !item.HasTag("Creature") && !item.HasTag("Terrain")
                && item.SpatialZone == null && physical?.ParentEntity == item && physical.Takeable && !physical.Solid
                && physical.InInventory == null && physical.Equipped == null && !item.HasPart<SpatialFootprintPart>()
                && (stack == null || stack.ParentEntity == item && stack.StackCount == 1 && stack.MaxStack >= 1);
        }
    }
}
