using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Conserves finite pool/vessel volume. Selections name the actual
    /// source liquid or destination cell; incompatible liquids are refused, not
    /// converted into a pure component. Ground coatings remain the existing
    /// contact/exposure abstraction, not recoverable units of liquid.</summary>
    public static class LiquidVesselService
    {
        public const string PoolBlueprint = "PouredLiquidPool";
        public const int PouredCoatingTurns = 4;
        const string FillPrefix = "FillLiquidVessel|";
        const string OnePrefix = "PourOneLiquidVessel|";
        const string PourPrefix = "PourLiquidVessel|";

        /// <summary>Recognizes this Part's two encoded inventory commands.</summary>
        public static bool IsLiquidCommand(string command) => command != null
            && (WellFlaskFilling.IsCommand(command) || command.StartsWith(FillPrefix, StringComparison.Ordinal)
                || command.StartsWith(PourPrefix, StringComparison.Ordinal) || command.StartsWith(OnePrefix, StringComparison.Ordinal));

        // Only quoted cooling commands opt out of the executor's blanket item/stat
        // snapshot. This receipt transfers quantity, never custody or actor stats.
        internal static bool OwnsCoolingReceipt(Entity vessel, string command) => vessel?.GetPart<LiquidVesselPart>()?.ParentEntity == vessel && vessel!=null
            && IsLiquidCommand(command) && !command.StartsWith(FillPrefix,StringComparison.Ordinal) && command.Split('|').Length==7;

        /// <summary>Read-only menu projection of carried contents and physically
        /// nearby pools. Every selected identity/position is checked again when used.</summary>
        public static void AddActions(Entity actor, Entity vessel, Zone zone, InventoryActionList actions)
        {
            if (actions == null || Validate(actor, vessel, zone, out var part) != null) return;
            if (part.Volume < part.Capacity)
            {
                bool canDrawPool = PoolRecipient(actor, vessel, zone, part);
                foreach (var source in zone.GetReadOnlyEntities())
                {
                    if(source.HasPart<WellPart>())
                    {WellFlaskFilling.AddActions(actor,vessel,source,zone,actions);continue;}
                    var contained = source.GetPart<ContainedLiquidSourcePart>();
                    if (contained != null)
                    {
                        if (!contained.CanFill(actor, zone, part)) continue;
                        var atContained = zone.GetEntityPosition(source);
                        actions.AddAction("FillLiquid", "fill with " + LiquidName(contained.LiquidId) + " from tray (" + atContained.x + "," + atContained.y + ")",
                            FillPrefix + Encode(source.ID) + "|" + Encode(contained.LiquidId), '\0', 19).AtEntity(zone, source).InGroup(ToolGroups.Fill);
                        continue;
                    }
                    var pool = source.GetPart<LiquidPoolPart>();
                    if (!canDrawPool || ValidSource(zone, actor, source, pool, part) != null) continue;
                    string name = LiquidRegistry.Get(pool.LiquidId).DisplayName ?? pool.LiquidId;
                    var at = zone.GetEntityPosition(source);
                    actions.AddAction("FillLiquid", "fill with " + name + " (" + at.x + "," + at.y + ")",
                        FillPrefix + Encode(source.ID) + "|" + Encode(pool.LiquidId), '\0', 19).AtEntity(zone, source).InGroup(ToolGroups.Fill);
                }
            }
            if (part.Volume == 0) return;
            var origin = zone.GetEntityPosition(actor);
            foreach (var direction in new[] { (0,0,"here"), (0,-1,"north"), (1,-1,"northeast"), (1,0,"east"), (1,1,"southeast"),
                (0,1,"south"), (-1,1,"southwest"), (-1,0,"west"), (-1,-1,"northwest") })
            {
                int x = origin.x + direction.Item1, y = origin.y + direction.Item2;
                if (ValidDestination(zone, actor, part.LiquidId, x, y, out _) != null) continue;
                string name = LiquidRegistry.Get(part.LiquidId).DisplayName ?? part.LiquidId;
                // Over your own lit hearth the row says what the pour would do to the fire (the rule is FireDouse's).
                if (!FireDouse.TryQuote(zone,x,y,part.LiquidId,part.Volume,out var pourQuote)
                    || !FireDouse.TryQuote(zone,x,y,part.LiquidId,1,out var oneQuote)) continue;
                string pourHint = pourQuote?.Hint, oneHint = oneQuote?.Hint;
                var pour = actions.AddAction("PourLiquid", "pour " + name + " " + direction.Item3 + " (" + part.Volume + ")",
                    PourPrefix + Encode(part.LiquidId) + "|" + part.Volume.ToString(CultureInfo.InvariantCulture)
                    + "|" + Encode(zone.ZoneID) + (pourQuote==null ? "" : "|"+Encode(pourQuote.Key)) + "|" + x.ToString(CultureInfo.InvariantCulture) + "|" + y.ToString(CultureInfo.InvariantCulture), '\0', 18);
                var one = actions.AddAction("PourOneLiquid", "pour one " + name + " " + direction.Item3,
                    OnePrefix + Encode(part.LiquidId) + "|" + part.Volume.ToString(CultureInfo.InvariantCulture)
                    + "|" + Encode(zone.ZoneID) + (oneQuote==null ? "" : "|"+Encode(oneQuote.Key)) + "|" + x.ToString(CultureInfo.InvariantCulture) + "|" + y.ToString(CultureInfo.InvariantCulture), '\0', 17);
                if (pourHint != null && pour != null) pour.Hint = pourHint;
                if (oneHint != null && one != null) one.Hint = oneHint;
                pour?.AtCell(x, y).InGroup(ToolGroups.Pour); one?.AtCell(x, y).InGroup(ToolGroups.Pour);
            }
        }

        /// <summary>Joins an existing inventory transaction or owns one. A true
        /// result transfers volume; contact reactions and success receipts publish
        /// only after commit. A refused or rolled-back action preserves both sides.</summary>
        public static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if(WellFlaskFilling.IsCommand(command))return WellFlaskFilling.TryAct(actor,vessel,zone,command,transaction);
            if (!IsLiquidCommand(command)) return false;
            string invalid = Validate(actor, vessel, zone, out var part);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            string[] bits = command.Split('|');
            bool filling = command.StartsWith(FillPrefix, StringComparison.Ordinal);
            if (filling ? bits.Length != 3 : bits.Length != 6 && bits.Length != 7) return Reject(actor, vessel, command, "invalid-selection");
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(vessel, actor, command))
                    return Reject(actor, vessel, command, "in-progress");
                return filling ? Fill(actor, vessel, part, zone, bits, command, transaction, own)
                    : Pour(actor, vessel, part, zone, bits, command, transaction, own);
            }
            finally { if (own) transaction.Rollback(); }
        }

        static bool Fill(Entity actor, Entity vessel, LiquidVesselPart part, Zone zone, string[] bits,
            string command, InventoryTransaction transaction, bool own)
        {
            string id = Decode(bits[1]), expectedLiquid = Decode(bits[2]);
            var matches = zone.GetReadOnlyEntities().Where(e => e.ID == id).ToArray();
            if (matches.Length != 1) return Reject(actor, vessel, command, "source-gone-or-ambiguous");
            var source = matches[0]; var pool = source.GetPart<LiquidPoolPart>();
            if(source.HasPart<WellPart>())return Reject(actor,vessel,command,"well-quote-required");
            var contained = source.GetPart<ContainedLiquidSourcePart>();
            if (contained != null) return FillContained(actor, vessel, part, zone, source, contained, expectedLiquid, command, transaction, own);
            if (!PoolRecipient(actor, vessel, zone, part)) return Reject(actor, vessel, command, "invalid-participant");
            string invalid = ValidSource(zone, actor, source, pool, part);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            if (pool.LiquidId != expectedLiquid) return Reject(actor, vessel, command, "source-liquid-changed");
            if (part.Volume == part.Capacity) return Reject(actor, vessel, command, "full");
            if (!transaction.TryClaimAction(actor) || !transaction.TryClaim(source, actor, command))
                return Reject(actor, vessel, command, "source-in-use");
            var pack = actor.GetPart<InventoryPart>();
            var actorCell = zone.GetEntityCell(actor);
            var sourceCell = zone.GetEntityCell(source);
            var sourcePhysics = source.GetPart<PhysicsPart>();
            var vesselPhysics = vessel.GetPart<PhysicsPart>();
            var actorPhysics = actor.GetPart<PhysicsPart>();
            string actorId = actor.ID, vesselId = vessel.ID, liquidId = pool.LiquidId;
            int before = part.Volume, oldPool = pool.Volume, capacity = part.Capacity;
            int amount = Math.Min(capacity - before, oldPool);

            transaction.OwnItemActionRollback(vessel, command);
            transaction.Do(null, () =>
            {
                // A moved flask still holds its credit, but a replacement or
                // changed liquid cannot prove any matching credit was recovered.
                int recovered = 0;
                if (vessel.GetPart<LiquidVesselPart>() == part && part.ParentEntity == vessel
                    && part.LiquidId == liquidId && part.Volume > 0)
                {
                    recovered = Math.Min(part.Volume, amount);
                    part.Volume -= recovered;
                    if (part.Volume == 0) part.LiquidId = "";
                }
                if (recovered > 0 && source.GetPart<LiquidPoolPart>() == pool && pool.ParentEntity == source
                    && pool.LiquidId == liquidId && pool.Volume >= 0)
                    pool.Volume = WaterQuantityUndo.Restore(pool.Volume, -recovered);
            });
            part.Volume += amount; part.LiquidId = liquidId; pool.Volume -= amount;

            bool Current() => PoolRecipient(actor, vessel, zone, part)
                && actor.ID == actorId && vessel.ID == vesselId && source.ID == id
                && actor.GetPart<InventoryPart>() == pack && actor.GetPart<PhysicsPart>() == actorPhysics
                && vessel.GetPart<PhysicsPart>() == vesselPhysics && source.GetPart<PhysicsPart>() == sourcePhysics
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(source) == sourceCell
                && zone.GetCell(actorCell.X, actorCell.Y) == actorCell && zone.GetCell(sourceCell.X, sourceCell.Y) == sourceCell
                && zone.GetReadOnlyEntities().Count(entity => entity.ID == id) == 1
                && part.Capacity == capacity && part.Volume == before + amount && part.LiquidId == liquidId
                && pool.Volume == oldPool - amount && pool.LiquidId == liquidId
                && ValidSource(zone, actor, source, pool, part, requireVolume: false) == null;
            transaction.BeforeCommit(Current);
            var audio = liquidId == "water"
                ? WorldAudioUtility.Take(actor, vessel, zone, WorldAudioCue.WaterFill, source, false, Current)
                : null;
            transaction.AfterCommit(()=>audio?.Publish());
            transaction.AfterCommit(() => PublishCommittedPoolDraw(zone, source, pool, sourceCell));
            transaction.AfterCommit(() => Diag.Record("liquid", "VesselFilled", actor, vessel,
                new { source = source.ID, liquidId, amount, remaining = oldPool - amount }));
            transaction.AfterCommit(() => MessageLog.Add("You fill the flask with " + LiquidName(liquidId) + ". (" + (before + amount) + "/" + capacity + ")"));
            if (own) transaction.Commit();
            return true;
        }

        static bool FillContained(Entity actor, Entity vessel, LiquidVesselPart part, Zone zone, Entity source,
            ContainedLiquidSourcePart contained, string expectedLiquid, string command, InventoryTransaction tx, bool own)
        {
            if (contained.LiquidId != expectedLiquid || !contained.CanFill(actor, zone, part)) return Reject(actor, vessel, command, "contained-source-unavailable");
            if (!tx.TryClaim(source, actor, command)) return Reject(actor, vessel, command, "source-in-use");
            var pack = actor.GetPart<InventoryPart>(); var actorCell = zone.GetEntityCell(actor); var sourceCell = zone.GetEntityCell(source);
            var sourceParts = source.Parts.ToArray(); var vesselParts = vessel.Parts.ToArray();
            string actorId = actor.ID, sourceId = source.ID, vesselId = vessel.ID, beforeId = part.LiquidId;
            int before = part.Volume, oldSource = contained.Volume, capacity = part.Capacity, sourceCapacity = contained.Capacity;
            int amount = Math.Min(capacity - before, oldSource);
            tx.Do(null, () => { part.Volume = before; part.LiquidId = beforeId; contained.Volume = oldSource; });
            tx.Do(null, () => ContainedLiquidSourcePart.DetachUnexpectedCustody(vessel, actor));
            part.Volume += amount; part.LiquidId = expectedLiquid; contained.Volume -= amount;
            tx.BeforeCommit(() => Validate(actor, vessel, zone, out var current) == null && current == part
                && contained.ParentEntity == source && contained.Context(actor, zone) && actor.GetPart<InventoryPart>() == pack
                && actor.ID == actorId && source.ID == sourceId && vessel.ID == vesselId
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(source) == sourceCell
                && source.Parts.SequenceEqual(sourceParts) && vessel.Parts.SequenceEqual(vesselParts)
                && source.Parts.All(p => p.ParentEntity == source) && vessel.Parts.All(p => p.ParentEntity == vessel)
                && part.Capacity == capacity && part.Volume == before + amount && part.LiquidId == expectedLiquid
                && contained.Capacity == sourceCapacity && contained.Volume == oldSource - amount && contained.LiquidId == expectedLiquid);
            tx.AfterCommit(() =>
            {
                ZoneRenderHooks.MarkCellDirty(sourceCell, "LiquidVessel.ContainedSourceChanged");
                Diag.Record("liquid", "ContainedSourceDrawn", actor, vessel, new { source = sourceId, liquidId = expectedLiquid, amount, remaining = oldSource - amount });
                MessageLog.Add("You draw " + amount + " " + LiquidName(expectedLiquid) + " into the flask. (" + (before + amount) + "/" + capacity + ")");
            });
            if (own) tx.Commit();
            return true;
        }

        /// <summary>Publish a committed finite-volume change for the exact original
        /// owner, pool and anchor. Persistent sources remain at zero; the existing
        /// poured-pool lifecycle still owns retirement. Outer rollback never calls
        /// this observer, and stale/replaced owners cannot dirty another cell.</summary>
        internal static void PublishCommittedPoolDraw(Zone zone, Entity source, LiquidPoolPart pool, Cell sourceCell)
        {
            var physics = source?.GetPart<PhysicsPart>();
            if (sourceCell == null || source?.SpatialZone != zone || zone?.GetEntityCell(source) != sourceCell
                || !sourceCell.Objects.Contains(source) || pool == null || pool.ParentEntity != source
                || source.GetPart<LiquidPoolPart>() != pool || physics == null || physics.ParentEntity != source
                || physics.Takeable || physics.InInventory != null || physics.Equipped != null || source.HasTag("Creature")) return;
            RetireExhaustedPouredPool(zone, source, pool);
            if (source.SpatialZone == zone && zone.GetEntityCell(source) == sourceCell)
                ZoneRenderHooks.MarkCellDirty(sourceCell, "LiquidVessel.SourceChanged");
        }

        /// <summary>A finite poured owner ends with its last recoverable unit.
        /// Removal uses Zone's normal projection lifecycle and happens only after
        /// commit, so outer rollback retains the exact owner and coating. Natural
        /// authored sources deliberately keep their existing identity/lifetime.</summary>
        internal static void RetireExhaustedPouredPool(Zone zone, Entity source, LiquidPoolPart pool)
        {
            var cell = source == null ? null : zone?.GetEntityCell(source);
            var physics = source?.GetPart<PhysicsPart>();
            if (cell == null || source.BlueprintName != PoolBlueprint || pool == null || pool.Volume != 0
                || source.GetPart<LiquidPoolPart>() != pool || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || source.HasTag("Creature")
                || source.HasPart<TileStateSourcePart>() || source.HasPart<WellPart>()) return;
            if (zone.RemoveEntity(source)) ZoneRenderHooks.MarkCellDirty(cell, "LiquidVessel.SourceExhausted");
        }

        static bool Pour(Entity actor, Entity vessel, LiquidVesselPart part, Zone zone, string[] bits,
            string command, InventoryTransaction transaction, bool own)
        {
            if (!int.TryParse(bits[2], NumberStyles.None, CultureInfo.InvariantCulture, out int expectedVolume)
                || !int.TryParse(bits[bits.Length-2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(bits[bits.Length-1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                return Reject(actor, vessel, command, "invalid-selection");
            string liquidId = part.LiquidId; int before = part.Volume; int amount = command.StartsWith(OnePrefix, StringComparison.Ordinal) ? Math.Min(1, before) : before;
            if (amount <= 0) return Reject(actor, vessel, command, "empty");
            if (Decode(bits[1]) != liquidId || expectedVolume != before || Decode(bits[3]) != zone.ZoneID)
                return Reject(actor, vessel, command, "contents-or-zone-changed");
            string invalid = ValidDestination(zone, actor, liquidId, x, y, out Entity destination);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            if (!FireDouse.TryQuote(zone,x,y,liquidId,amount,out var cooling)
                || (bits.Length==7 ? cooling==null || cooling.Key!=Decode(bits[4]) : cooling!=null))
                return Reject(actor,vessel,command,"cooling-selection-changed");
            if(cooling!=null && !transaction.TryClaim(cooling.Hearth,actor,command)) return Reject(actor,vessel,command,"hearth-in-use");
            int residual=cooling?.Residual ?? amount;
            var actorCell=zone.GetEntityCell(actor);
            var vesselParts=vessel.Parts.ToArray(); int vesselCapacity=part.Capacity;
            var irrigation = residual>0 ? CropPourIrrigation.Capture(zone, x, y, liquidId) : null;
            bool created = destination == null && residual > 0;
            if (created)
            {
                destination = MaterialReactionResolver.Factory?.CreateEntity(PoolBlueprint);
                var prepared = destination?.GetPart<LiquidPoolPart>();
                var physical = destination?.GetPart<PhysicsPart>();
                if (prepared == null || prepared.Volume != 0 || !string.IsNullOrEmpty(prepared.LiquidId)
                    || physical == null || physical.Takeable || physical.InInventory != null || physical.Equipped != null
                    || destination.HasTag("Creature") || destination.SpatialZone != null)
                    return Reject(actor, vessel, command, "missing-ground-pool");
                // Factory callbacks are allowed to run arbitrary game code. Recheck
                // the captured actor, contents and destination before paying.
                if (Validate(actor, vessel, zone, out var current) != null || !ReferenceEquals(current, part)
                    || part.Volume != before || part.LiquidId != liquidId
                    || ValidDestination(zone, actor, liquidId, x, y, out var appeared) != null || appeared != null)
                    return Reject(actor, vessel, command, "state-changed-during-preparation");
            }
            if (destination!=null && !transaction.TryClaim(destination, actor, command)) return Reject(actor, vessel, command, "destination-in-use");
            var pool = destination?.GetPart<LiquidPoolPart>();
            int oldPool = pool?.Volume ?? 0; string oldId = pool?.LiquidId;
            if (oldPool < 0 || (long)oldPool + residual > int.MaxValue)
                return Reject(actor, vessel, command, "destination-overflow");
            Entity recipient = destination;
            int previousCoating = zone.TileState.CoatingTurns(x, y, liquidId);
            bool flaskDebited=false, poolCredited=false;
            int recoverableCredit=residual;
            transaction.Do(null, () =>
            {
                if(cooling==null) { part.Volume=before; part.LiquidId=liquidId; }
                else if(flaskDebited)
                {
                    if(vessel.GetPart<LiquidVesselPart>()==part && part.ParentEntity==vessel
                        && (part.LiquidId==liquidId || part.Volume==0 && string.IsNullOrEmpty(part.LiquidId)))
                    { part.Volume=WaterQuantityUndo.Restore(part.Volume,-amount,part.Capacity); if(part.Volume>0) part.LiquidId=liquidId; }
                    else Diag.Record("liquid","CoolingUndoIdentityChanged",actor,vessel);
                }
                // Removal must see the staged liquid identity to erase its pool
                // projection, then restore any older independent coating lease.
                if (created && recipient.SpatialZone == zone && (cooling==null || recipient.GetPart<LiquidPoolPart>()==pool
                    && pool.ParentEntity==recipient && recipient.GetPart<PhysicsPart>()?.InInventory==null
                    && recipient.GetPart<PhysicsPart>()?.Equipped==null && pool.LiquidId==liquidId
                    && (pool.Volume==recoverableCredit || pool.Volume==0)))
                {
                    zone.RemoveEntity(recipient);
                    if (previousCoating > 0) zone.TileState.WriteCoating(x, y, liquidId, previousCoating);
                }
                if(pool!=null)
                {
                    if(cooling==null) { pool.Volume=oldPool; pool.LiquidId=oldId; }
                    else if(poolCredited)
                    {
                        if(recipient.GetPart<LiquidPoolPart>()==pool && pool.ParentEntity==recipient && pool.LiquidId==liquidId)
                        { pool.Volume=WaterQuantityUndo.Restore(pool.Volume,recoverableCredit); if(!created && pool.Volume==oldPool) pool.LiquidId=oldId; }
                        else Diag.Record("liquid","CoolingPoolUndoIdentityChanged",actor,recipient);
                    }
                }
            });
            part.Volume = before - amount; if (part.Volume == 0) part.LiquidId = ""; flaskDebited=true;
            if(pool!=null && residual>0) { pool.Volume += residual; pool.LiquidId = liquidId; poolCredited=true; }
            if (created)
            {
                pool.Initialize();
                if (recipient.GetPart<RenderPart>() is RenderPart render) render.DisplayName = LiquidName(liquidId) + " puddle";
                if (!zone.AddEntity(recipient, x, y)) return Reject(actor, vessel, command, "placement-refused");
            }
            if(cooling!=null)
            {
                transaction.BeforeCommit(()=>Validate(actor,vessel,zone,out var current)==null && current==part
                    && zone.GetEntityCell(actor)==actorCell && part.Capacity==vesselCapacity && part.Volume==before-amount
                    && part.LiquidId==(before==amount?"":liquidId) && vessel.Parts.SequenceEqual(vesselParts)
                    && vesselParts.All(p=>p.ParentEntity==vessel) && cooling.Current());
                transaction.AfterCommit(()=>cooling.Publish(actor));
            }
            if(residual>0 && irrigation?.Apply(actor, zone, recipient, transaction, cooling!=null)==true && cooling!=null) recoverableCredit--;
            if(cooling!=null && pool!=null)
            {
                int committedPool=pool.Volume;
                var recipientCell=zone.GetEntityCell(recipient);
                var recipientPhysics=recipient.GetPart<PhysicsPart>();
                transaction.BeforeCommit(()=>recipientCell==zone.GetCell(x,y) && zone.GetEntityCell(recipient)==recipientCell
                    && recipient.GetPart<LiquidPoolPart>()==pool && pool.ParentEntity==recipient && pool.Volume==committedPool
                    && pool.LiquidId==liquidId && recipient.GetPart<PhysicsPart>()==recipientPhysics && recipientPhysics?.ParentEntity==recipient
                    && recipientPhysics.InInventory==null && recipientPhysics.Equipped==null && !recipientPhysics.Takeable
                    && LiquidSourceSafety.IsUnmixedCell(zone,x,y,liquidId));
            }
            int capacity=part.Capacity;
            var audio=liquidId=="water" && recipient!=null && residual>0?WorldAudioUtility.Take(actor,vessel,zone,WorldAudioCue.WaterPour,recipient,false,
                ()=>vessel.GetPart<LiquidVesselPart>()==part&&part.ParentEntity==vessel&&part.Capacity==capacity&&part.Volume==before-amount
                    &&part.LiquidId==(before==amount?"":"water")&&recipient.GetPart<LiquidPoolPart>()==pool
                    &&pool.ParentEntity==recipient&&pool.Volume==oldPool+residual&&pool.LiquidId=="water"):null;
            transaction.AfterCommit(()=>audio?.Publish());
            if(residual>0) transaction.AfterCommit(() => ApplyContact(zone, actor, recipient, pool, x, y, liquidId, residual));
            transaction.AfterCommit(() => Diag.Record("liquid", "VesselPoured", actor, vessel,
                new { destination = recipient?.ID, liquidId, amount, evaporated=cooling?.Used ?? 0, recoverable=residual, x, y }));
            transaction.AfterCommit(() => MessageLog.Add(cooling==null ? "You pour " + amount + " of " + LiquidName(liquidId) + " onto the ground."
                : "You pour " + amount + " of " + LiquidName(liquidId) + "; " + cooling.Used + " evaporate, " + residual + " remain recoverable."));
            if (own) transaction.Commit();
            return true;
        }

        static void ApplyContact(Zone zone, Entity actor, Entity pool, LiquidPoolPart captured, int x, int y, string liquidId, int amount)
        {
            var physical=pool?.GetPart<PhysicsPart>();
            if (zone.GetEntityCell(pool) != zone.GetCell(x,y) || captured==null || pool.GetPart<LiquidPoolPart>()!=captured
                || captured.ParentEntity!=pool || captured.LiquidId!=liquidId || captured.Volume<=0
                || physical==null || physical.ParentEntity!=pool || physical.InInventory!=null || physical.Equipped!=null || physical.Takeable) return;
            foreach (var target in zone.GetOccupants(x, y).Where(e => e.HasTag("Creature")).ToArray())
            {
                var contact = GameEvent.New("EntityEnteredCell");
                contact.SetParameter("Actor", (object)target); contact.SetParameter("Zone", (object)zone);
                pool.FireEventAndRelease(contact);
            }
            ZoneTileStateSystem.WriteCoating(zone, x, y, liquidId, PouredCoatingTurns, actor, "PourLiquidVessel");
            ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
        }

        static string Validate(Entity actor, Entity vessel, Zone zone, out LiquidVesselPart part)
        {
            part = vessel?.GetPart<LiquidVesselPart>();
            if (actor == null || part == null || zone?.GetEntityCell(actor) == null) return "missing-context";
            if (CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)) return "actor-dead";
            if (actor.GetPart<InventoryPart>()?.Objects.Contains(vessel) != true
                || vessel.SpatialZone != null || vessel.GetPart<PhysicsPart>()?.InInventory != actor
                || vessel.GetPart<PhysicsPart>()?.Equipped != null || (vessel.GetPart<StackerPart>()?.StackCount ?? 1) != 1) return "not-carried";
            if (part.Capacity <= 0 || part.Volume < 0 || part.Volume > part.Capacity
                || (part.Volume == 0 ? !string.IsNullOrEmpty(part.LiquidId) : string.IsNullOrEmpty(part.LiquidId))) return "invalid-vessel";
            if (!LiquidRegistry.IsInitialized || (part.Volume > 0 && LiquidRegistry.Get(part.LiquidId) == null)) return "unknown-liquid";
            return null;
        }
        static bool PoolRecipient(Entity actor, Entity vessel, Zone zone, LiquidVesselPart part)
            => WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && WorldResourceActions.Carried(actor, vessel) && part.ParentEntity == vessel
                && vessel.GetPart<LiquidVesselPart>() == part && vessel.Parts.Count(candidate => candidate is LiquidVesselPart) == 1;

        static string ValidSource(Zone zone, Entity actor, Entity source, LiquidPoolPart pool, LiquidVesselPart vessel,
            bool requireVolume = true)
        {
            if (pool == null || pool.ParentEntity != source || source.GetPart<LiquidPoolPart>() != pool
                || source.Parts.Count(candidate => candidate is LiquidPoolPart) != 1
                || pool.Volume < 0 || (requireVolume && pool.Volume == 0)
                || source.HasPart<FiniteLiquidFilmPart>() || source.HasPart<WellPart>() || source.HasPart<ContainedLiquidSourcePart>()
                || source.HasTag("Creature") || source.GetPart<PhysicsPart>()?.Takeable != false
                || !WorldResourceActions.Nearby(actor, source, zone)) return "source-unavailable";
            if (LiquidRegistry.Get(pool.LiquidId) == null) return "unknown-liquid";
            if ((pool.LiquidId == "water" || LiquidRegistry.Get(pool.LiquidId)?.GroundReactionFamily == "water")
                && !LiquidSourcePhase.CanDrawWater(zone, source)) return "frozen-water";
            if (vessel.Volume > 0 && vessel.LiquidId != pool.LiquidId) return "unlike-liquids";
            if (!LiquidSourceSafety.IsUnmixedSource(zone, source, pool.LiquidId)) return "mixed-source";
            return null;
        }
        static string ValidDestination(Zone zone, Entity actor, string liquidId, int x, int y, out Entity pool)
        {
            pool = null; var cell = zone.GetCell(x, y);
            if (cell == null || SpatialQuery.DistanceToCell(zone, actor, x, y) > 1 || !cell.IsPassable()) return "destination-out-of-reach";
            if (!LiquidSourceSafety.IsUnmixedCell(zone, x, y, liquidId)) return "mixed-destination";
            foreach (var entity in cell.Occupants)
            {
                var candidate = entity.GetPart<LiquidPoolPart>();
                if (candidate == null || entity.HasPart<FiniteLiquidFilmPart>()) continue;
                var physical = entity.GetPart<PhysicsPart>();
                if (physical?.Takeable == true || physical?.InInventory != null || physical?.Equipped != null)
                    return "destination-not-ground";
                if (candidate.Volume < 0 || (candidate.Volume > 0 && candidate.LiquidId != liquidId)) return "invalid-destination";
                if (candidate.LiquidId == liquidId && pool == null) pool = entity;
            }
            return null;
        }
        static string LiquidName(string id) => LiquidRegistry.Get(id)?.DisplayName ?? id;
        static string Encode(string value) => Uri.EscapeDataString(value ?? "");
        static string Decode(string value) => Uri.UnescapeDataString(value ?? "");
        static bool Reject(Entity actor, Entity vessel, string command, string reason)
        {
            Diag.Record("liquid", "VesselRejected", actor, vessel, new { command, reason });
            MessageLog.Add("You cannot transfer that liquid (" + reason.Replace('-', ' ') + ").");
            return false;
        }
    }

    /// <summary>Conservative purity check for the single-liquid pool model.
    /// Co-located unlike physical pools/coatings cannot be sampled as clean water
    /// or separated into pure components through inventory actions.</summary>
    public static class LiquidSourceSafety
    {
        public static bool IsUnmixedPool(Zone zone, Entity source)
        {
            var part = source?.GetPart<LiquidPoolPart>();
            if (part == null || part.Volume <= 0 || source.HasPart<FiniteLiquidFilmPart>() || string.IsNullOrEmpty(part.LiquidId) || zone?.GetEntityCell(source) == null) return false;
            return IsUnmixedSource(zone, source, part.LiquidId);
        }
        /// <summary>Checks every occupied source cell, including renewing springs
        /// that have no finite LiquidPoolPart. This never purifies a mixed cell.</summary>
        public static bool IsUnmixedSource(Zone zone, Entity source, string liquidId)
        {
            if (zone?.GetEntityCell(source) == null) return false;
            foreach (var cell in zone.GetOccupiedCells(source))
                if (!IsUnmixedCell(zone, cell.X, cell.Y, liquidId)) return false;
            return true;
        }
        public static bool IsUnmixedCell(Zone zone, int x, int y, string liquidId)
        {
            var cell = zone?.GetCell(x, y); if (cell == null || string.IsNullOrEmpty(liquidId)) return false;
            foreach (var entity in cell.Occupants)
            {
                var pool = entity.GetPart<LiquidPoolPart>();
                if (pool != null && (pool.Volume < 0 || (pool.Volume > 0 && pool.LiquidId != liquidId))) return false;
                var source = entity.GetPart<TileStateSourcePart>();
                if (source != null && source.CoatingTurns > 0 && !string.IsNullOrEmpty(source.EffectiveCoating) && source.EffectiveCoating != liquidId) return false;
            }
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || !coatings.Any(c => c.Turns > 0 && c.Id != liquidId);
        }
    }
}
