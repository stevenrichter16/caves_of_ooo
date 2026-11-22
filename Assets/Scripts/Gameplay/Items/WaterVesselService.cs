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

        public static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor?.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, vessel, command, "actor_dead", "You cannot use water while dead.");
            var inventory = actor?.GetPart<InventoryPart>();
            var skin = vessel?.GetPart<WaterskinPart>();
            if (skin == null || inventory == null || !inventory.Objects.Contains(vessel)
                || (vessel.GetPart<StackerPart>()?.StackCount ?? 1) != 1)
                return Reject(actor, vessel, command, "not_carried", "You must carry that waterskin.");
            if (skin.Capacity <= 0 || skin.Charges < 0 || skin.Charges > skin.Capacity)
                return Reject(actor, vessel, command, "invalid_capacity", "That waterskin cannot hold water.");
            if (!IsWaterCommand(command)) return false;
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(vessel, actor, command))
                    return Reject(actor, vessel, command, "in_progress", "That waterskin is already in use.");
                int before = skin.Charges;
                if (command != "DrinkWaterskin")   // the plain fill or the fill that names a source
                {
                    if (before == skin.Capacity) return Reject(actor, vessel, command, "full", "The waterskin is already full.");
                    Entity source = command == "FillWaterskin" ? FindSource(actor, zone) : NamedSource(actor, zone, command.Substring(FillSourcePrefix.Length));
                    if (source == null) return Reject(actor, vessel, command, "no_fresh_water", "Stand beside a well, cistern, or pool of fresh water to fill it.");
                    if (!transaction.TryClaim(source, actor, command)) return Reject(actor, vessel, command, "source_in_use", "That water is already being drawn.");
                    var pool = source.GetPart<LiquidPoolPart>();
                    // A finite well (a rain cistern) gives only what it holds; -1 is endless.
                    var finite = pool == null && source.GetPart<WellPart>() is WellPart w && w.Stock > 0 ? w : null;
                    var sourceCell = zone.GetEntityCell(source);
                    int oldVolume = pool?.Volume ?? 0, oldStock = finite?.Stock ?? -1;
                    int amount = pool != null ? Math.Min(skin.Capacity - before, oldVolume)
                        : finite != null ? Math.Min(skin.Capacity - before, oldStock) : skin.Capacity - before;
                    transaction.Do(null, () => { skin.Charges = before; if (pool != null) pool.Volume = oldVolume; if (finite != null) finite.Stock = oldStock; });
                    skin.Charges += amount;
                    if (pool != null) pool.Volume -= amount;
                    if (finite != null) finite.Stock -= amount;
                    int expectedCharges=skin.Charges, capacity=skin.Capacity;
                    Part sourcePart=(Part)pool??(Part)source.GetPart<WellPart>()??(Part)source.GetPart<TileStateSourcePart>();
                    var audio=WorldAudioUtility.Take(actor,vessel,zone,WorldAudioCue.WaterFill,source,false,
                        ()=>vessel.GetPart<WaterskinPart>()==skin&&skin.ParentEntity==vessel&&skin.Capacity==capacity&&skin.Charges==expectedCharges
                            &&sourcePart?.ParentEntity==source&&source.Parts.Contains(sourcePart)
                            &&(pool==null||(source.GetPart<LiquidPoolPart>()==pool&&pool.LiquidId=="water"&&pool.Volume==oldVolume-amount)));
                    transaction.AfterCommit(()=>audio?.Publish());
                    transaction.AfterCommit(() => LiquidVesselService.PublishCommittedPoolDraw(zone, source, pool, sourceCell));
                    int filled = skin.Charges;
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinFilled", actor, vessel,
                        new { source = source.BlueprintName, amount, remaining = filled }));
                    transaction.AfterCommit(() => MessageLog.Add("You fill the waterskin. (" + filled + "/" + skin.Capacity + " drinks)"));
                }
                else
                {
                    if (before == 0) return Reject(actor, vessel, command, "empty", "The waterskin is empty.");
                    var effects = actor.GetPart<StatusEffectsPart>();
                    var parched = effects?.GetEffect<ParchedEffect>();
                    int stacks = parched?.Stacks ?? 0;
                    string cause = parched?.LastRemovalCause;
                    int index = parched == null ? -1 : IndexOf(effects, parched);
                    // Undo only this drink's delta, so independent effect changes
                    // made while a caller owns the transaction survive rollback.
                    transaction.Do(null, () =>
                    {
                        skin.Charges = before;
                        if (parched == null) return;
                        var current = effects.GetEffect<ParchedEffect>();
                        int restoredStacks;
                        if (current == null)
                        {
                            // Only this drink's single relieved stack is ours to undo.
                            // An independent cure must not resurrect the old full depth.
                            restoredStacks = 1;
                            parched.Stacks = restoredStacks; parched.LastRemovalCause = cause;
                            effects.RestoreRemovedEffectForInventoryUndo(parched, index);
                        }
                        else
                        {
                            // A new exposure may have created a replacement instance,
                            // or already brought the remaining instance to its cap.
                            restoredStacks = current.Stacks < ParchedEffect.MaxStacks ? 1 : 0;
                            current.Stacks += restoredStacks;
                        }
                        int penalty = restoredStacks * ParchedEffect.PenaltyPerStack;
                        if (actor.GetStat("Strength") != null) actor.GetStat("Strength").Penalty += penalty;
                        if (actor.GetStat("Agility") != null) actor.GetStat("Agility").Penalty += penalty;
                    });
                    int expectedCharges=before-1, capacity=skin.Capacity;
                    var audio=WorldAudioUtility.Take(actor,vessel,zone,WorldAudioCue.WaterDrink,null,false,
                        ()=>vessel.GetPart<WaterskinPart>()==skin&&skin.ParentEntity==vessel&&skin.Capacity==capacity&&skin.Charges==expectedCharges);
                    transaction.AfterCommit(()=>audio?.Publish());
                    skin.Charges--;
                    ParchedEffect.ReduceOneStack(actor);
                    int remaining = skin.Charges;
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinDrunk", actor, vessel,
                        new { remaining, parchedRelieved = stacks > 0 }));
                    transaction.AfterCommit(() => MessageLog.Add("You drink water. (" + remaining + "/" + skin.Capacity + " drinks remain)"));
                }
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        private static int IndexOf(StatusEffectsPart effects, Effect target)
        { var all = effects.GetAllEffects(); for (int i = 0; i < all.Count; i++) if (ReferenceEquals(all[i], target)) return i; return -1; }
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
        internal static bool IsFreshSource(Entity actor, Zone zone, Entity source)
        {
            if (source == null || zone?.GetEntityCell(actor) == null) return false;
            if (SpatialQuery.Distance(zone, actor, source) > 1 || source.HasTag("Creature")
                || source.GetPart<PhysicsPart>()?.Takeable == true
                || source.GetPart<PhysicsPart>()?.InInventory != null
                || source.GetPart<PhysicsPart>()?.Equipped != null) return false;
            // A pool supplies quantity, not permission to bypass its well's
            // structural fault, source condition or existing water refusal.
            var well = source.GetPart<WellPart>();
            if (well != null && (!WellDrinkingService.SourceAvailable(well, zone) || TentRightOath.RefusesWater(actor, zone))) return false;
            // A finite pool takes precedence over terrain's renewing coating.
            var pool = source.GetPart<LiquidPoolPart>();
            if (pool != null) return pool.ParentEntity == source && pool.LiquidId == "water" && pool.Volume > 0 && LiquidSourcePhase.CanDrawWater(zone, source) && LiquidSourceSafety.IsUnmixedPool(zone, source);
            if (well != null) return true;
            var spring = source.GetPart<TileStateSourcePart>();
            return spring?.Coating == "water" && spring.CoatingTurns > 0
                && LiquidSourcePhase.CanDrawWater(zone, source) && LiquidSourceSafety.IsUnmixedSource(zone, source, "water");
        }
        private static bool Reject(Entity actor, Entity vessel, string command, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "WaterskinRejected", actor, vessel, new { command, reason }); return false; }
    }
}
