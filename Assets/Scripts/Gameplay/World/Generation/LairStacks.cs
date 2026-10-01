using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>A saved, immutable plan exposed as a defensive snapshot.</summary>
    public sealed class LairStackRecord
    {
        public string SurfaceID { get; internal set; }
        public bool Legacy { get; internal set; }
        public BiomeType Biome { get; internal set; }
        public int Tier { get; internal set; }
        public int FinalDepth { get; internal set; }
        public string BossBlueprint { get; internal set; }
        public int GeneratedMask { get; internal set; }
        public string BossID { get; internal set; }
        public string RewardID { get; internal set; }
        internal string[] Up = new string[3], Down = new string[3];
        internal LairStackRecord Copy()
        {
            var copy = (LairStackRecord)MemberwiseClone();
            copy.Up = (string[])Up.Clone();
            copy.Down = (string[])Down.Clone();
            return copy;
        }
        public string ZoneAt(int depth)
        {
            var p = WorldMap.FromZoneID(SurfaceID);
            return WorldMap.ToZoneID(p.x, p.y, depth);
        }
    }
    /// <summary>Versioned world ownership, including the empty new-world marker.</summary>
    public sealed class LairStackLedgerPart : Part, ISaveSerializable
    {
        public override string Name => "LairStackLedger";
        internal OverworldZoneManager RuntimeOwner;
        internal readonly Dictionary<string, LairStackRecord> Records = new Dictionary<string, LairStackRecord>();
        public int Count => Records.Count;
        public void Save(SaveWriter writer)
        {
            writer.Write(1);
            writer.Write(Records.Count);
            foreach (var r in Records.Values.OrderBy(r => r.SurfaceID, StringComparer.Ordinal))
            {
                writer.WriteString(r.SurfaceID);
                writer.Write(r.Legacy);
                writer.Write((int)r.Biome);
                writer.Write(r.Tier);
                writer.Write(r.FinalDepth);
                writer.WriteString(r.BossBlueprint);
                writer.Write(r.GeneratedMask);
                writer.WriteString(r.BossID);
                writer.WriteString(r.RewardID);
                for (int i = 0; i < 3; i++)
                {
                    writer.WriteString(r.Up[i]);
                    writer.WriteString(r.Down[i]);
                }
            }
        }
        public void Load(SaveReader reader)
        {
            int version = reader.ReadInt(), count = reader.ReadInt();
            if (version != 1 || count < 0 || count > WorldMap.Width * WorldMap.Height)
                throw new InvalidDataException("Invalid lair ledger header.");
            var staged = new Dictionary<string, LairStackRecord>();
            var entityClaims = new HashSet<string>(StringComparer.Ordinal);
            for (int n = 0; n < count; n++)
            {
                var r = new LairStackRecord
                {
                    SurfaceID = reader.ReadString(),
                    Legacy = reader.ReadBool(),
                    Biome = (BiomeType)reader.ReadInt(),
                    Tier = reader.ReadInt(),
                    FinalDepth = reader.ReadInt(),
                    BossBlueprint = reader.ReadString(),
                    GeneratedMask = reader.ReadInt(),
                    BossID = reader.ReadString(),
                    RewardID = reader.ReadString()
                };
                for (int i = 0; i < 3; i++)
                {
                    r.Up[i] = reader.ReadString();
                    r.Down[i] = reader.ReadString();
                }
                var p = WorldMap.FromZoneID(r.SurfaceID);
                bool valid = WorldMapAuthoring.InBounds(p.x, p.y) && p.z == 0 && r.SurfaceID == WorldMap.ToZoneID(p.x, p.y, 0)
                    && Enum.IsDefined(typeof(BiomeType), r.Biome) && r.Tier >= 1 && r.Tier <= 8
                    && (r.Legacy ? r.FinalDepth == 0 : r.FinalDepth >= 1 && r.FinalDepth <= 2 && LairStacks.AllowedBiome(r.Biome))
                    && r.GeneratedMask >= 0 && (r.GeneratedMask & ~((1 << (r.FinalDepth + 1)) - 1)) == 0
                    && Token(r.BossBlueprint, r.Legacy) && Token(r.BossID, true) && Token(r.RewardID, true);
                for (int i = 0; i < 3; i++)
                    valid &= Token(r.Up[i], true) && Token(r.Down[i], true)
                    && (i <= r.FinalDepth || (r.Up[i] == null && r.Down[i] == null))
                    && ((r.GeneratedMask & (1 << i)) != 0 || (r.Up[i] == null && r.Down[i] == null));
                valid &= r.Up[0] == null && r.Down[r.FinalDepth >= 0 && r.FinalDepth < 3 ? r.FinalDepth : 0] == null;
                foreach (string id in new[] { r.BossID, r.RewardID }.Concat(r.Up).Concat(r.Down))
                    if (id != null && !entityClaims.Add(id))
                        valid = false;
                bool finalGenerated = (r.GeneratedMask & (1 << r.FinalDepth)) != 0;
                if (!r.Legacy)
                    valid &= finalGenerated ? (!string.IsNullOrEmpty(r.BossID) && !string.IsNullOrEmpty(r.RewardID)) : (r.BossID == null && r.RewardID == null);
                if (!valid || staged.ContainsKey(r.SurfaceID))
                    throw new InvalidDataException("Invalid lair ownership record.");
                staged.Add(r.SurfaceID, r);
            }
            Records.Clear();
            foreach (var p in staged)
                Records.Add(p.Key, p.Value);
        }
        private static bool Token(string value, bool optional) => value == null ? optional : value.Length > 0 && value.Length <= 128 && !value.Any(char.IsControl);
    }
    /// <summary>Cold generation and save ownership for finite lair columns.
    /// Actual generated zone graphs remain cached; claims never recreate owners.</summary>
    public static class LairStacks
    {
        private static readonly ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart> Ledgers = new ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>();
        // Derived provenance, not another saved graph. Exact committed zones
        // and successfully restored graphs may be styled after their mutable
        // bosses, caches or stairs are gone; lookalike cached objects may not.
        private sealed class CommittedFloor
        {
            internal OverworldZoneManager Manager;
            internal LairStackLedgerPart Ledger;
            internal string SurfaceID, ZoneID;
            internal int Depth;
        }
        private static readonly ConditionalWeakTable<Zone, CommittedFloor> CommittedFloors =
            new ConditionalWeakTable<Zone, CommittedFloor>();

        private static void BindCommittedFloor(Zone zone, OverworldZoneManager manager,
            LairStackLedgerPart ledger, LairStackRecord record, int depth)
        {
            if (record.Legacy)
                return;
            CommittedFloors.Remove(zone);
            CommittedFloors.Add(zone, new CommittedFloor
            {
                Manager = manager, Ledger = ledger, SurfaceID = record.SurfaceID,
                ZoneID = zone.ZoneID, Depth = depth
            });
        }

        /// <summary>Read-only derived provenance for presentation. This neither
        /// initializes/adopts a ledger nor inspects mutable gameplay owners.</summary>
        internal static bool IsCommittedFloor(OverworldZoneManager manager, Zone zone, BiomeType biome)
        {
            return manager != null && zone != null
                && CommittedFloors.TryGetValue(zone, out var source)
                && ReferenceEquals(source.Manager, manager) && source.ZoneID == zone.ZoneID
                && manager.CachedZones != null && manager.CachedZones.TryGetValue(zone.ZoneID, out var cached)
                && ReferenceEquals(cached, zone)
                && Ledgers.TryGetValue(manager, out var ledger) && ReferenceEquals(ledger, source.Ledger)
                && ledger.Records.TryGetValue(source.SurfaceID, out var record)
                && !record.Legacy && record.Biome == biome
                && source.Depth > 0 && source.Depth <= record.FinalDepth
                && (record.GeneratedMask & (1 << source.Depth)) != 0;
        }

        private sealed class Pending
        {
            public LairStackRecord Plan;
            public int Depth;
            public string Boss, Reward, Up, Down, Loose;
            public Entity BossOwner, RewardOwner, UpOwner, DownOwner, LooseOwner;
        }
        private static readonly ConditionalWeakTable<Zone, Pending> PendingZones = new ConditionalWeakTable<Zone, Pending>();
        internal static bool AllowedBiome(BiomeType b) => b == BiomeType.Spread || b == BiomeType.Sodden || b == BiomeType.Beating || b == BiomeType.Grovelands;
        private static LairStackLedgerPart Ledger(OverworldZoneManager manager)
        {
            return Ledgers.GetValue(manager, owner =>
            {
                var ledger = new LairStackLedgerPart { RuntimeOwner = owner };
                foreach (var zone in owner.CachedZones.Values)
                    AdoptLegacy(owner, ledger, zone.ZoneID);
                return ledger;
            });
        }
        private static void AdoptLegacy(OverworldZoneManager manager, LairStackLedgerPart ledger, string id)
        {
            var p = WorldMap.FromZoneID(id);
            if (!WorldMapAuthoring.InBounds(p.x, p.y) || p.z < 0)
                return;
            var poi = manager.WorldMap.GetPOI(p.x, p.y);
            string surface = WorldMap.ToZoneID(p.x, p.y, 0);
            if (poi?.Type != POIType.Lair || ledger.Records.ContainsKey(surface))
                return;
            ledger.Records.Add(surface, new LairStackRecord { SurfaceID = surface, Legacy = true, Biome = manager.WorldMap.GetBiome(p.x, p.y), Tier = Math.Max(1, Math.Min(8, poi.Tier)), FinalDepth = 0, BossBlueprint = poi.BossBlueprint, GeneratedMask = manager.CachedZones.ContainsKey(surface) ? 1 : 0 });
        }
        public static LairStackRecord Inspect(OverworldZoneManager manager, string surface)
        {
            if (manager == null)
                return null;
            return Ledger(manager).Records.TryGetValue(surface, out var r) ? r.Copy() : null;
        }
        /// <summary>Read-only exclusion for generic ecology and presentation.
        /// A saved lair claim outlives mutable map labels; queries never adopt or
        /// initialize a ledger merely to classify an ordinary column.</summary>
        internal static bool HasSavedColumn(OverworldZoneManager manager, string surface)
            => manager != null && surface != null && Ledgers.TryGetValue(manager, out var ledger)
                && ledger.Records.ContainsKey(surface);
        internal static bool TryPlan(OverworldZoneManager manager, string zoneID, out LairStackRecord plan)
        {
            plan = null;
            var at = WorldMap.FromZoneID(zoneID);
            if (!WorldMapAuthoring.InBounds(at.x, at.y) || at.z < 0)
                return false;
            string surface = WorldMap.ToZoneID(at.x, at.y, 0);
            var ledger = Ledger(manager);
            if (ledger.Records.TryGetValue(surface, out var existing))
            {
                if (existing.Legacy || at.z > existing.FinalDepth)
                    return false;
                plan = existing.Copy();
                return true;
            }
            var poi = manager.WorldMap.GetPOI(at.x, at.y);
            var biome = manager.WorldMap.GetBiome(at.x, at.y);
            if (poi?.Type != POIType.Lair || !AllowedBiome(biome))
                return false;
            int depth = poi.Tier >= 3 ? 2 : 1;
            if (at.z > depth)
                return false;
            plan = new LairStackRecord { SurfaceID = surface, Biome = biome, Tier = Math.Max(1, Math.Min(8, poi.Tier)), FinalDepth = depth, BossBlueprint = poi.BossBlueprint };
            return true;
        }
        internal static void Stage(Zone zone, LairStackRecord plan, int depth, Entity boss, Entity reward, Entity up, Entity down, Entity loose)
        {
            PendingZones.Remove(zone);
            PendingZones.Add(zone, new Pending
            {
                Plan = plan, Depth = depth,
                Boss = boss?.ID, Reward = reward?.ID, Up = up?.ID, Down = down?.ID, Loose = loose?.ID,
                BossOwner = boss, RewardOwner = reward, UpOwner = up, DownOwner = down, LooseOwner = loose
            });
        }
        internal static bool CommitGenerated(Zone zone, OverworldZoneManager manager)
        {
            var ledger = Ledger(manager);
            if (!PendingZones.TryGetValue(zone, out var p))
            {
                var at = WorldMap.FromZoneID(zone.ZoneID);
                string surface = WorldMap.ToZoneID(at.x, at.y, 0);
                if (at.z == 0 && ledger.Records.TryGetValue(surface, out var legacy) && legacy.Legacy)
                    legacy.GeneratedMask = 1;
                return true;
            }
            PendingZones.Remove(zone);
            if (!TryPrepareCommit(zone, manager, ledger, p, out var next, out var stagedEdges))
                return false;
            ledger.Records[next.SurfaceID] = next;
            // A removed cached endpoint cannot leave a stale deferred route.
            foreach (var edge in manager.GetConnections(zone.ZoneID).ToArray())
                if (edge.Type == "StairsDown" && ((p.Up == null && p.Depth > 0 && edge.SourceZoneID == next.ZoneAt(p.Depth - 1) && edge.TargetZoneID == zone.ZoneID)
                    || (p.Down == null && p.Depth < next.FinalDepth && edge.SourceZoneID == zone.ZoneID && edge.TargetZoneID == next.ZoneAt(p.Depth + 1))))
                    manager.RemoveConnection(edge);
            // Connections are published only after the complete floor succeeds.
            foreach (var edge in stagedEdges)
            {
                foreach (var oldEdge in manager.GetConnections(edge.SourceZoneID).ToArray())
                    if (oldEdge.Type == edge.Type && oldEdge.SourceZoneID == edge.SourceZoneID && oldEdge.TargetZoneID == edge.TargetZoneID)
                        manager.RemoveConnection(oldEdge);
                manager.RegisterConnection(edge);
            }
            if (p.Depth == next.FinalDepth)
                foreach (var edge in manager.GetConnections(zone.ZoneID).ToArray())
                    if (edge.SourceZoneID == zone.ZoneID && edge.Type == "StairsDown")
                        manager.RemoveConnection(edge);
            BindCommittedFloor(zone, manager, ledger, next, p.Depth);
            Record("LairFloorCommitted", zone.ZoneID, new
            {
                surface = next.SurfaceID,
                depth = p.Depth,
                finalDepth = next.FinalDepth,
                boss = next.BossID,
                reward = next.RewardID,
                loose = p.Loose
            });
            return true;
        }
        /// <summary>Non-consuming preflight for a fresh final-floor decoration.
        /// The caller receives the actual staged owner only after the same native
        /// ownership and route checks required by commit. An ID or blueprint match
        /// cannot authorize a replacement, borrowed actor or lookalike zone.</summary>
        internal static bool TryGetValidatedFinalBoss(Zone zone, OverworldZoneManager manager, out Entity boss)
        {
            boss = null;
            if (zone == null || manager == null || manager.CachedZones.ContainsKey(zone.ZoneID)
                || !PendingZones.TryGetValue(zone, out var pending) || pending.BossOwner == null
                || pending.Plan.Legacy || pending.Depth != pending.Plan.FinalDepth
                || zone.ZoneID != pending.Plan.ZoneAt(pending.Depth)
                || !TryPlan(manager, zone.ZoneID, out var current)
                || current.SurfaceID != pending.Plan.SurfaceID || current.Biome != pending.Plan.Biome
                || current.Tier != pending.Plan.Tier || current.FinalDepth != pending.Plan.FinalDepth
                || current.BossBlueprint != pending.Plan.BossBlueprint
                || !TryPrepareCommit(zone, manager, Ledger(manager), pending, out _, out _))
                return false;
            boss = pending.BossOwner;
            return true;
        }
        private static bool TryPrepareCommit(Zone zone, OverworldZoneManager manager,
            LairStackLedgerPart ledger, Pending p, out LairStackRecord next, out List<ZoneConnection> stagedEdges)
        {
            next = null;
            stagedEdges = null;
            // Native generation callbacks run after builders. Validate the actual
            // staged identities again, before either claims or routes can publish.
            if (!ValidOwner(zone, p.BossOwner, p.Boss, "boss")
                || !ValidOwner(zone, p.RewardOwner, p.Reward, "reward")
                || !ValidOwner(zone, p.UpOwner, p.Up, "up")
                || !ValidOwner(zone, p.DownOwner, p.Down, "down")
                || !ValidOwner(zone, p.LooseOwner, p.Loose, "loose"))
                return false;
            if (ledger.Records.TryGetValue(p.Plan.SurfaceID, out var old) && (old.Legacy || (old.GeneratedMask & (1 << p.Depth)) != 0))
                return false;
            next = (old ?? p.Plan).Copy();
            next.GeneratedMask |= 1 << p.Depth;
            next.Up[p.Depth] = p.Up;
            next.Down[p.Depth] = p.Down;
            if (p.Depth == next.FinalDepth)
            {
                next.BossID = p.Boss;
                next.RewardID = p.Reward;
                if (string.IsNullOrEmpty(next.BossID) || string.IsNullOrEmpty(next.RewardID))
                    return false;
            }
            stagedEdges = new List<ZoneConnection>();
            if (p.Up != null)
            {
                if (!TryEdge(manager, next, p.Depth - 1, zone, out var incoming))
                    return false;
                stagedEdges.Add(incoming);
            }
            if (p.Down != null)
            {
                if (!TryEdge(manager, next, p.Depth, zone, out var outgoing))
                    return false;
                stagedEdges.Add(outgoing);
            }
            return true;
        }
        private static bool ValidOwner(Zone zone, Entity owner, string id, string role)
        {
            if (owner == null)
                return id == null;
            if (owner.ID != id || !zone.GetReadOnlyEntities().Contains(owner) || zone.GetEntityCell(owner) == null)
                return false;
            if (role == "boss" && (!owner.HasTag("Creature") || owner.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(owner)))
                return false;
            if (role == "loose" && (owner.GetPart<PhysicsPart>()?.Takeable != true || owner.HasTag("Creature")
                || owner.GetPart<PhysicsPart>().InInventory != null || owner.GetPart<PhysicsPart>().Equipped != null))
                return false;
            if (role == "reward" && !owner.HasPart<ContainerPart>())
                return false;
            if (role == "up" && !owner.HasPart<StairsUpPart>())
                return false;
            if (role == "down" && !owner.HasPart<StairsDownPart>())
                return false;
            var cells = zone.GetOccupiedCells(owner);
            if (cells.Count == 0)
                return false;
            foreach (var cell in cells)
            {
                if ((role == "boss" || role == "reward" || role == "loose") && zone.GenReservedCells.Contains((cell.X, cell.Y)))
                    return false;
                foreach (var other in cell.Occupants)
                    if (!ReferenceEquals(other, owner) && (!other.HasTag("Terrain") || other.HasTag("Solid")
                        || other.GetPart<PhysicsPart>()?.Solid == true || other.HasPart<TriggerOnStepPart>()))
                        return false;
            }
            return true;
        }
        private static bool TryEdge(OverworldZoneManager manager, LairStackRecord plan, int upper, Zone staged, out ZoneConnection edge)
        {
            edge = null;
            if (!TryEndpoint(manager, plan, upper, false, staged, out var source)
                || !TryEndpoint(manager, plan, upper + 1, true, staged, out var target))
                return false;
            edge = new ZoneConnection
            {
                SourceZoneID = plan.ZoneAt(upper),
                SourceX = source.x,
                SourceY = source.y,
                TargetZoneID = plan.ZoneAt(upper + 1),
                TargetX = target.x,
                TargetY = target.y,
                Type = "StairsDown"
            };
            return true;
        }
        private static bool TryEndpoint(OverworldZoneManager manager, LairStackRecord plan, int depth, bool up, Zone staged, out (int x, int y) point)
        {
            point = (up ? 36 : 44, 12);
            Zone zone = staged.ZoneID == plan.ZoneAt(depth) ? staged : manager.CachedZones.TryGetValue(plan.ZoneAt(depth), out var cached) ? cached : null;
            if (zone == null)
                return true; // An ungenerated counterpart has a deferred native endpoint.
            string id = up ? plan.Up[depth] : plan.Down[depth];
            var matches = zone.GetReadOnlyEntities().Where(e => e.ID == id && (up ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>())).ToArray();
            if (matches.Length != 1)
                return false;
            var owner = matches[0];
            var cells = zone.GetOccupiedCells(owner);
            if (cells.Count == 0 || owner.HasTag("Solid") || owner.GetPart<PhysicsPart>()?.Solid == true)
                return false;
            foreach (var cell in cells)
                foreach (var other in cell.Occupants)
                {
                    // The traveller legitimately stands on the departure stair
                    // while a new destination is generated. Other obstructions fail.
                    if (ReferenceEquals(owner, other) || other.HasTag("Player"))
                        continue;
                    if (!other.HasTag("Terrain") || other.HasTag("Solid") || other.GetPart<PhysicsPart>()?.Solid == true
                        || other.HasPart<TriggerOnStepPart>())
                        return false;
                }
            point = (cells[0].X, cells[0].Y);
            return true;
        }
        internal static bool CounterpartExists(OverworldZoneManager manager, LairStackRecord plan, int depth, bool up)
        {
            int other = up ? depth - 1 : depth + 1;
            if (other < 0 || other > plan.FinalDepth)
                return false;
            if (!manager.CachedZones.TryGetValue(plan.ZoneAt(other), out var zone))
                return true;
            string expected = up ? plan.Down[other] : plan.Up[other];
            return !string.IsNullOrEmpty(expected) && zone.GetReadOnlyEntities().Any(e => e.ID == expected && (up ? e.HasPart<StairsDownPart>() : e.HasPart<StairsUpPart>()));
        }
        internal static bool RetainOnUnload(OverworldZoneManager manager, string zoneID)
        {
            var at = WorldMap.FromZoneID(zoneID);
            if (!WorldMapAuthoring.InBounds(at.x, at.y) || at.z < 0 || at.z > 2)
                return false;
            string surface = WorldMap.ToZoneID(at.x, at.y, 0);
            bool retain = Ledger(manager).Records.TryGetValue(surface, out var r) && (r.GeneratedMask & (1 << at.z)) != 0;
            if (retain)
                Record("LairZoneRetained", zoneID, new
                {
                    surface
                });
            return retain;
        }
        public static Entity BindForSave(OverworldZoneManager manager, Entity world)
        {
            if (manager == null)
                return world;
            var ledger = Ledger(manager);
            var old = world?.GetPart<LairStackLedgerPart>();
            if (old != null && !ReferenceEquals(old, ledger))
                throw new InvalidOperationException("World owns another lair ledger.");
            if (world == null)
            {
                world = new Entity { BlueprintName = "World" };
                world.SetTag("WorldEntity");
            }
            // Metadata-only worlds are real saved graph owners too. Assign their
            // identity before writing, so load repair cannot change the next save.
            if (string.IsNullOrEmpty(world.ID))
                world.ID = Guid.NewGuid().ToString("N");
            if (old == null)
                world.AddPart(ledger);
            return world;
        }
        public static void Restore(OverworldZoneManager manager, Entity world)
        {
            if (manager == null)
                return;
            var ledger = world?.GetPart<LairStackLedgerPart>();
            if (ledger == null)
            {
                ledger = new LairStackLedgerPart();
                for (int x = 0; x < WorldMap.Width; x++)
                    for (int y = 0; y < WorldMap.Height; y++)
                        AdoptLegacy(manager, ledger, WorldMap.ToZoneID(x, y, 0));
            }
            if (ledger.RuntimeOwner != null && !ReferenceEquals(ledger.RuntimeOwner, manager))
                throw new InvalidOperationException("Cannot borrow another lair ledger.");
            foreach (var r in ledger.Records.Values)
                for (int z = 0; z <= r.FinalDepth; z++)
                    if ((r.GeneratedMask & (1 << z)) != 0 && !manager.CachedZones.ContainsKey(r.ZoneAt(z)))
                        throw new InvalidDataException("Claimed lair floor is missing from saved graph.");
            ledger.RuntimeOwner = manager;
            Ledgers.Remove(manager);
            Ledgers.Add(manager, ledger);
            foreach (var record in ledger.Records.Values)
                for (int depth = 0; depth <= record.FinalDepth; depth++)
                    if ((record.GeneratedMask & (1 << depth)) != 0)
                        BindCommittedFloor(manager.CachedZones[record.ZoneAt(depth)], manager, ledger, record, depth);
        }
        private static void Record(string kind, string zone, object payload)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", kind, payload: new
                {
                    zone,
                    detail = payload
                });
        }
    }
}
