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
        const string PourPrefix = "PourLiquidVessel|";

        /// <summary>Recognizes this Part's two encoded inventory commands.</summary>
        public static bool IsLiquidCommand(string command) => command != null
            && (command.StartsWith(FillPrefix, StringComparison.Ordinal)
                || command.StartsWith(PourPrefix, StringComparison.Ordinal));

        /// <summary>Read-only menu projection of carried contents and physically
        /// nearby pools. Every selected identity/position is checked again when used.</summary>
        public static void AddActions(Entity actor, Entity vessel, Zone zone, InventoryActionList actions)
        {
            if (actions == null || Validate(actor, vessel, zone, out var part) != null) return;
            if (part.Volume < part.Capacity)
            {
                foreach (var source in zone.GetReadOnlyEntities())
                {
                    var pool = source.GetPart<LiquidPoolPart>();
                    if (ValidSource(zone, actor, source, pool, part) != null) continue;
                    string name = LiquidRegistry.Get(pool.LiquidId).DisplayName ?? pool.LiquidId;
                    var at = zone.GetEntityPosition(source);
                    actions.AddAction("FillLiquid", "fill with " + name + " (" + at.x + "," + at.y + ")",
                        FillPrefix + Encode(source.ID) + "|" + Encode(pool.LiquidId), '\0', 19);
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
                actions.AddAction("PourLiquid", "pour " + name + " " + direction.Item3 + " (" + part.Volume + ")",
                    PourPrefix + Encode(part.LiquidId) + "|" + part.Volume.ToString(CultureInfo.InvariantCulture)
                    + "|" + Encode(zone.ZoneID) + "|" + x.ToString(CultureInfo.InvariantCulture) + "|" + y.ToString(CultureInfo.InvariantCulture), '\0', 18);
            }
        }

        /// <summary>Joins an existing inventory transaction or owns one. A true
        /// result transfers volume; contact reactions and success receipts publish
        /// only after commit. A refused or rolled-back action preserves both sides.</summary>
        public static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (!IsLiquidCommand(command)) return false;
            string invalid = Validate(actor, vessel, zone, out var part);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            string[] bits = command.Split('|');
            bool filling = command.StartsWith(FillPrefix, StringComparison.Ordinal);
            if (bits.Length != (filling ? 3 : 6)) return Reject(actor, vessel, command, "invalid-selection");
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
            string invalid = ValidSource(zone, actor, source, pool, part);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            if (pool.LiquidId != expectedLiquid) return Reject(actor, vessel, command, "source-liquid-changed");
            if (part.Volume == part.Capacity) return Reject(actor, vessel, command, "full");
            if (!transaction.TryClaim(source, actor, command)) return Reject(actor, vessel, command, "source-in-use");
            int before = part.Volume, oldPool = pool.Volume;
            string oldId = part.LiquidId, liquidId = pool.LiquidId;
            int amount = Math.Min(part.Capacity - before, oldPool);
            transaction.Do(null, () => { part.Volume = before; part.LiquidId = oldId; pool.Volume = oldPool; });
            part.Volume += amount; part.LiquidId = liquidId; pool.Volume -= amount;
            transaction.AfterCommit(() => RetireExhaustedPouredPool(zone, source, pool));
            transaction.AfterCommit(() => Diag.Record("liquid", "VesselFilled", actor, vessel,
                new { source = source.ID, liquidId, amount, remaining = oldPool - amount }));
            transaction.AfterCommit(() => MessageLog.Add("You fill the flask with " + LiquidName(liquidId) + ". (" + (before + amount) + "/" + part.Capacity + ")"));
            if (own) transaction.Commit();
            return true;
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
                || !int.TryParse(bits[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(bits[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                return Reject(actor, vessel, command, "invalid-selection");
            string liquidId = part.LiquidId; int amount = part.Volume;
            if (amount <= 0) return Reject(actor, vessel, command, "empty");
            if (Decode(bits[1]) != liquidId || expectedVolume != amount || Decode(bits[3]) != zone.ZoneID)
                return Reject(actor, vessel, command, "contents-or-zone-changed");
            string invalid = ValidDestination(zone, actor, liquidId, x, y, out Entity destination);
            if (invalid != null) return Reject(actor, vessel, command, invalid);
            bool created = destination == null;
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
                    || part.Volume != amount || part.LiquidId != liquidId
                    || ValidDestination(zone, actor, liquidId, x, y, out var appeared) != null || appeared != null)
                    return Reject(actor, vessel, command, "state-changed-during-preparation");
            }
            if (!transaction.TryClaim(destination, actor, command)) return Reject(actor, vessel, command, "destination-in-use");
            var pool = destination.GetPart<LiquidPoolPart>();
            int oldPool = pool.Volume; string oldId = pool.LiquidId;
            if (oldPool < 0 || (long)oldPool + amount > int.MaxValue)
                return Reject(actor, vessel, command, "destination-overflow");
            Entity recipient = destination;
            int previousCoating = zone.TileState.CoatingTurns(x, y, liquidId);
            transaction.Do(null, () =>
            {
                part.Volume = amount; part.LiquidId = liquidId;
                // Removal must see the staged liquid identity to erase its pool
                // projection, then restore any older independent coating lease.
                if (created && recipient.SpatialZone == zone)
                {
                    zone.RemoveEntity(recipient);
                    if (previousCoating > 0) zone.TileState.WriteCoating(x, y, liquidId, previousCoating);
                }
                pool.Volume = oldPool; pool.LiquidId = oldId;
            });
            pool.Volume += amount; pool.LiquidId = liquidId;
            if (created)
            {
                pool.Initialize();
                if (recipient.GetPart<RenderPart>() is RenderPart render) render.DisplayName = LiquidName(liquidId) + " puddle";
                if (!zone.AddEntity(recipient, x, y)) return Reject(actor, vessel, command, "placement-refused");
            }
            part.Volume = 0; part.LiquidId = "";
            transaction.AfterCommit(() => ApplyContact(zone, actor, recipient, x, y, liquidId));
            transaction.AfterCommit(() => Diag.Record("liquid", "VesselPoured", actor, vessel,
                new { destination = recipient.ID, liquidId, amount, x, y }));
            transaction.AfterCommit(() => MessageLog.Add("You pour " + amount + " of " + LiquidName(liquidId) + " onto the ground."));
            if (own) transaction.Commit();
            return true;
        }

        static void ApplyContact(Zone zone, Entity actor, Entity pool, int x, int y, string liquidId)
        {
            if (zone.GetEntityCell(pool) == null) return;
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
        static string ValidSource(Zone zone, Entity actor, Entity source, LiquidPoolPart pool, LiquidVesselPart vessel)
        {
            if (pool == null || pool.Volume <= 0 || source.HasTag("Creature")
                || source.GetPart<PhysicsPart>()?.Takeable == true || source.GetPart<PhysicsPart>()?.InInventory != null
                || source.GetPart<PhysicsPart>()?.Equipped != null || SpatialQuery.Distance(zone, actor, source) > 1) return "source-unavailable";
            if (LiquidRegistry.Get(pool.LiquidId) == null) return "unknown-liquid";
            if (vessel.Volume > 0 && vessel.LiquidId != pool.LiquidId) return "unlike-liquids";
            if (!LiquidSourceSafety.IsUnmixedPool(zone, source)) return "mixed-source";
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
                if (candidate == null) continue;
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
            if (part == null || part.Volume <= 0 || string.IsNullOrEmpty(part.LiquidId) || zone?.GetEntityCell(source) == null) return false;
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
                if (source != null && source.CoatingTurns > 0 && !string.IsNullOrEmpty(source.Coating) && source.Coating != liquidId) return false;
            }
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || !coatings.Any(c => c.Turns > 0 && c.Id != liquidId);
        }
    }
}
