using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Transactional water transfer; no arbitrary-liquid ingestion or mixing.</summary>
    public static class WaterVesselService
    {
        /// <summary>The fill command that names one source (the interact menu of a well or pool offers it): this prefix and the source's escaped ID. The plain "FillWaterskin" takes the first source beside you.</summary>
        public const string FillSourcePrefix = "FillWaterskin|";

        /// <summary>Whether a command is one this service runs: drinking, the plain fill, or the fill that names a source.</summary>
        public static bool IsWaterCommand(string command) => command == "FillWaterskin" || command == "DrinkWaterskin" || command != null && command.StartsWith(FillSourcePrefix, System.StringComparison.Ordinal);

        /// <summary>
        /// The fill row of the interact (C) menu of a cell that holds fresh water you could draw (a well, cistern, spring or pool of water beside you or under you): "fill the waterskin from the pond", run by the waterskin in the
        /// pack, naming exactly that source. Nothing for a skin that is full, not carried alone, an actor who cannot act, or water that is not fresh, drawable and unmixed (the same test the plain fill uses).
        /// </summary>
        internal static void AddCellActions(Entity actor, Entity vessel, Zone zone, Cell cell, InventoryActionList actions)
        {
            var skin = vessel?.GetPart<WaterskinPart>();
            if (actions == null || cell == null || skin == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || !WorldResourceActions.Carried(actor, vessel) || skin.Capacity <= 0 || skin.Charges < 0 || skin.Charges >= skin.Capacity) return;
            foreach (var source in cell.Objects.ToArray())
                if (IsFreshSource(actor, zone, source))
                    actions.AddActionWithHint("FillWater", "fill the waterskin from the " + source.GetDisplayName() + " (" + skin.Charges + "/" + skin.Capacity + ")", FillSourcePrefix + Uri.EscapeDataString(source.ID),
                        "Fills the waterskin from this water, up to what it holds and what the water has.", '\0', 19, false, ActionTime.OneAction);
        }

        static bool RecipientCurrent(Entity actor, Entity vessel, Zone zone, out WaterskinPart skin)
        {
            skin = vessel?.GetPart<WaterskinPart>();
            return WorldResourceActions.ActorCurrent(actor, zone) && HydrationReliefReceipt.ValidContext(actor) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && WorldResourceActions.Carried(actor, vessel) && skin?.ParentEntity == vessel
                && skin.Capacity > 0 && skin.Charges >= 0 && skin.Charges <= skin.Capacity;
        }
        public static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (!IsWaterCommand(command)) return false;
            zone = zone ?? actor?.SpatialZone;
            if (!RecipientCurrent(actor, vessel, zone, out var skin))
                return Reject(actor, vessel, command, "invalid_participant", "You cannot use that waterskin in its current state.");
            var inventory = actor.GetPart<InventoryPart>(); var actorCell = zone.GetEntityCell(actor);
            var physics = vessel.GetPart<PhysicsPart>(); var actorPhysics = actor.GetPart<PhysicsPart>();
            string actorId = actor.ID, vesselId = vessel.ID;
            int before = skin.Charges, capacity = skin.Capacity;
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(vessel, actor, command))
                    return Reject(actor, vessel, command, "in_progress", "That waterskin is already in use.");
                Func<int, bool> recipient = expected => RecipientCurrent(actor, vessel, zone, out var current) && current == skin
                    && actor.GetPart<InventoryPart>() == inventory && vessel.GetPart<PhysicsPart>() == physics && actor.GetPart<PhysicsPart>() == actorPhysics
                    && actor.ID == actorId && vessel.ID == vesselId && zone.GetEntityCell(actor) == actorCell
                    && skin.Capacity == capacity && skin.Charges == expected;
                if (command != "DrinkWaterskin")
                {
                    if (before == capacity) return Reject(actor, vessel, command, "full", "The waterskin is already full.");
                    Entity source = command == "FillWaterskin" ? FindSource(actor, zone) : NamedSource(actor, zone, command.Substring(FillSourcePrefix.Length));
                    if (source == null) return Reject(actor, vessel, command, "no_fresh_water", "Stand beside a well, cistern, or pool of fresh water to fill it.");
                    if (!transaction.TryClaim(source, actor, command)) return Reject(actor, vessel, command, "source_in_use", "That water is already being drawn.");
                    var pool = source.GetPart<LiquidPoolPart>(); var well = source.GetPart<WellPart>(); var spring = source.GetPart<TileStateSourcePart>();
                    var sourcePhysics = source.GetPart<PhysicsPart>(); var sourceCell = zone.GetEntityCell(source); string sourceId = source.ID;
                    int volume = pool?.Volume ?? 0, stock = well?.Stock ?? -1;
                    int amount = pool != null ? Math.Min(capacity - before, volume) : well != null && stock > 0 ? Math.Min(capacity - before, stock) : capacity - before;
                    transaction.Do(null, () =>
                    {
                        skin.Charges = WaterQuantityUndo.Restore(skin.Charges, amount, skin.Capacity);
                        if (pool != null) pool.Volume = WaterQuantityUndo.Restore(pool.Volume, -amount);
                        else if (well != null && stock > 0) well.Stock = WaterQuantityUndo.Restore(well.Stock, -amount, endlessAllowed: true);
                    });
                    skin.Charges += amount;
                    if (pool != null) pool.Volume -= amount;
                    else if (well != null && stock > 0) well.Stock -= amount;
                    Func<bool> currentSource = () => recipient(before + amount) && IsFreshSource(actor, zone, source, false)
                        && source.ID == sourceId && zone.GetEntityCell(source) == sourceCell && source.GetPart<PhysicsPart>() == sourcePhysics
                        && source.GetPart<LiquidPoolPart>() == pool && source.GetPart<WellPart>() == well && source.GetPart<TileStateSourcePart>() == spring
                        && (pool != null ? pool.Volume == volume - amount : well == null || well.Stock == stock - (stock > 0 ? amount : 0));
                    transaction.BeforeCommit(currentSource);
                    var audio = WorldAudioUtility.Take(actor, vessel, zone, WorldAudioCue.WaterFill, source, false, currentSource);
                    transaction.AfterCommit(() => audio?.Publish());
                    transaction.AfterCommit(() => LiquidVesselService.PublishCommittedPoolDraw(zone, source, pool, sourceCell));
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinFilled", actor, vessel, new { source = source.BlueprintName, amount, remaining = before + amount }));
                    transaction.AfterCommit(() => MessageLog.Add("You fill the waterskin. (" + (before + amount) + "/" + capacity + " drinks)"));
                }
                else
                {
                    if (before == 0) return Reject(actor, vessel, command, "empty", "The waterskin is empty.");
                    transaction.Do(null, () => skin.Charges = WaterQuantityUndo.Restore(skin.Charges, -1, skin.Capacity));
                    skin.Charges--;
                    if (!HydrationReliefReceipt.TryApply(actor, 1, transaction, out var relief)) return false;
                    Func<bool> current = () => recipient(before - 1);
                    transaction.BeforeCommit(current);
                    var audio = WorldAudioUtility.Take(actor, vessel, zone, WorldAudioCue.WaterDrink, null, false, current);
                    transaction.AfterCommit(() => audio?.Publish());
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinDrunk", actor, vessel, new { remaining = before - 1, parchedRelieved = relief.Relieved > 0 }));
                    transaction.AfterCommit(() => MessageLog.Add("You drink water. (" + (before - 1) + "/" + capacity + " drinks remain)"));
                }
                if (own) transaction.Commit(); return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        /// <summary>The fresh water the plain fill would draw from right now, or null. For describing the fill row truthfully.</summary>
        internal static Entity FreshSourceBeside(Entity actor, Zone zone) => FindSource(actor, zone);

        private static Entity FindSource(Entity actor, Zone zone)
        {
            if (zone?.GetEntityCell(actor) == null) return null;
            foreach (var source in zone.GetReadOnlyEntities()) if (IsFreshSource(actor, zone, source)) return source;
            return null;
        }

        /// <summary>The source an escaped ID names, when it is still fresh water beside the actor; otherwise null.</summary>
        private static Entity NamedSource(Entity actor, Zone zone, string escapedId)
        {
            var source = WorldResourceActions.ExactGround(zone, escapedId);
            return source != null && IsFreshSource(actor, zone, source) ? source : null;
        }

        /// <summary>Whether <paramref name="source"/> is fresh water the actor could draw right now: beside (or under) the actor, not a creature or a carried thing, and a pool of unmixed water with some left, a usable well with water, or a spring.</summary>
        internal static bool IsFreshSource(Entity actor, Zone zone, Entity source, bool requireWater = true)
        {
            if (source == null || !WorldResourceActions.ActorCurrent(actor, zone)) return false;
            if (SpatialQuery.Distance(zone, actor, source) > 1 || source.HasTag("Creature")
                || source.GetPart<PhysicsPart>()?.Takeable == true
                || source.GetPart<PhysicsPart>()?.InInventory != null
                || source.GetPart<PhysicsPart>()?.Equipped != null) return false;
            // A pool supplies quantity, not permission to bypass its well's
            // structural fault, source condition or existing water refusal.
            var well = source.GetPart<WellPart>();
            if (well != null && (!WellDrinkingService.SourceAvailable(well, zone, requireWater) || TentRightOath.RefusesWater(actor, zone))) return false;
            // A finite pool takes precedence over terrain's renewing coating.
            var pool = source.GetPart<LiquidPoolPart>();
            if (pool != null) return pool.ParentEntity == source && pool.LiquidId == "water" && (!requireWater || pool.Volume > 0) && LiquidSourcePhase.CanDrawWater(zone, source) && LiquidSourceSafety.IsUnmixedSource(zone, source, "water");
            if (well != null) return true;
            var spring = source.GetPart<TileStateSourcePart>();
            return spring?.ParentEntity == source && spring.Coating == "water" && spring.CoatingTurns > 0
                && LiquidSourcePhase.CanDrawWater(zone, source) && LiquidSourceSafety.IsUnmixedSource(zone, source, "water");
        }
        private static bool Reject(Entity actor, Entity vessel, string command, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "WaterskinRejected", actor, vessel, new { command, reason }); return false; }
    }
}
