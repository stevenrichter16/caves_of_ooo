using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Opt-in thin exposure on an ordinary LiquidPool. Volume is its one matter
    /// counter, never collectible drams. Primitive fields round-trip with the entity.</summary>
    public sealed class FiniteLiquidFilmPart : Part
    {
        public override string Name => "FiniteLiquidFilm";
        public const int LifetimeRounds = 6;
        public int RemainingRounds = LifetimeRounds;
        public string SourceActorId = "";
        public string ThrowSerial = "";
        public bool IsActive => RemainingRounds > 0 && ParentEntity?.GetPart<LiquidPoolPart>()?.Volume > 0;
        public string Describe() => "Thin " + (ParentEntity?.GetPart<LiquidPoolPart>()?.LiquidId ?? "liquid")
            + " film; too little to collect. " + Math.Max(0, ParentEntity?.GetPart<LiquidPoolPart>()?.Volume ?? 0)
            + " contact" + (ParentEntity?.GetPart<LiquidPoolPart>()?.Volume == 1 ? "" : "s")
            + " remain; dries in " + Math.Max(0, RemainingRounds) + " local turn" + (RemainingRounds == 1 ? "." : "s.");

        public override void OnAfterLoad(SaveReader reader)
        {
            RemainingRounds = Math.Max(0, Math.Min(LifetimeRounds, RemainingRounds));
            var pool = ParentEntity?.GetPart<LiquidPoolPart>();
            if (pool != null) pool.Volume = Math.Max(0, Math.Min(9, pool.Volume));
            SourceActorId = SourceActorId ?? ""; ThrowSerial = ThrowSerial ?? "";
        }
        internal static Entity Create(Zone zone, Cell cell, string liquid, int volume, string sourceId, string serial)
        {
            if (zone == null || cell?.ParentZone != zone || volume <= 0) return null;
            var entity = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "PouredLiquidPool" };
            entity.SetTag("Terrain");
            entity.AddPart(new PhysicsPart());
            entity.AddPart(new RenderPart { DisplayName = "thin " + liquid + " film", RenderLayer = 1 });
            entity.AddPart(new ExaminablePart());
            entity.AddPart(new FiniteLiquidFilmPart { SourceActorId = sourceId ?? "", ThrowSerial = serial ?? "" });
            entity.AddPart(new LiquidPoolPart { LiquidId = liquid, Volume = Math.Min(9, volume) });
            if (!zone.AddEntity(entity, cell.X, cell.Y)) return null;
            ZoneRenderHooks.MarkCellDirty(cell, "ThrowFilm.Placed");
            return entity;
        }
        internal bool Contact(Entity mover, Zone zone, GameEvent e)
        {
            var pool = ParentEntity?.GetPart<LiquidPoolPart>();
            if (!IsActive || mover == null || zone?.GetEntityCell(ParentEntity) == null
                || !mover.HasTag("Creature") || (long)mover.GetStatValue("Strength") + mover.GetStatValue("Toughness") <= 0
                || !LiquidRegistry.IsInitialized || LiquidRegistry.Get(pool.LiquidId) == null) return false;
            if (zone.GetEntityCell(mover)?.Objects.Contains(mover) != true || CombatSystem.IsDeathHandled(mover)
                || mover.GetStatValue("Hitpoints", 1) <= 0 || !SharesCurrentCell(mover, zone, e.GetParameter<Cell>("Cell"))) return false;
            var contacts = e.GetParameter<HashSet<(string source, string serial, string liquid)>>("FiniteLiquidContacts");
            if (contacts != null && !contacts.Add((SourceActorId, ThrowSerial, pool.LiquidId))) return false;
            // Reserve before callbacks. Reentry gets a separate dispatcher-owned set.
            pool.Volume--;
            try
            {
                mover.ApplyEffect(new LiquidCoveredEffect(pool.LiquidId, 1) { SourceActorId = SourceActorId },
                    ResolveSource(zone, SourceActorId), zone);
                Diag.Record("liquid", "FiniteFilmContact", mover, ParentEntity,
                    new { liquid = pool.LiquidId, volume = pool.Volume, sourceId = SourceActorId, throwSerial = ThrowSerial });
            }
            finally
            {
                if (pool.Volume <= 0) Retire(zone);
                else ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(ParentEntity), "ThrowFilm.Contact");
            }
            return true;
        }
        bool SharesCurrentCell(Entity mover, Zone zone, Cell reported)
        {
            foreach (var cell in zone.GetOccupiedCells(ParentEntity))
                if (cell != null && (reported == null || cell == reported) && cell.Occupants.Contains(mover)) return true;
            return false;
        }
        internal static Entity ResolveSource(Zone zone, string id)
        {
            if (zone == null || string.IsNullOrEmpty(id)) return null;
            Entity result = null;
            foreach (var owner in zone.GetReadOnlyEntities())
            {
                if (!string.Equals(owner?.ID, id, StringComparison.Ordinal)) continue;
                if (result != null) return null; // ambiguous IDs never award someone guessed credit
                if (zone.GetEntityCell(owner)?.Objects.Contains(owner) != true
                    || CombatSystem.IsDeathHandled(owner) || owner.GetStatValue("Hitpoints", 1) <= 0) return null;
                result = owner;
            }
            return result;
        }
        internal void Project(Zone zone, Cell cell)
        {
            if (!IsActive || string.IsNullOrEmpty(ParentEntity?.ID)) return;
            zone.TileState.WriteProjectedCoating(cell.X, cell.Y, ParentEntity.GetPart<LiquidPoolPart>().LiquidId,
                RemainingRounds, ParentEntity.ID);
        }
        internal void Retire(Zone zone)
        {
            RemainingRounds = 0;
            var pool = ParentEntity?.GetPart<LiquidPoolPart>(); if (pool != null) pool.Volume = 0;
            var cell = zone?.GetEntityCell(ParentEntity);
            if (cell == null) return;
            zone.RemoveEntity(ParentEntity);
            ZoneRenderHooks.MarkCellDirty(cell, "ThrowFilm.Retired");
        }
        internal static void OnCoatingConsumed(Zone zone, int x, int y, string liquid)
        {
            var cell = zone?.GetCell(x, y); if (cell == null) return;
            var owners = UnityEngine.Pool.ListPool<Entity>.Get();
            try
            {
                foreach (var owner in cell.Occupants)
                    if (owner?.HasPart<FiniteLiquidFilmPart>() == true && owner.GetPart<LiquidPoolPart>()?.LiquidId == liquid) owners.Add(owner);
                foreach (var owner in owners) owner.GetPart<FiniteLiquidFilmPart>()?.Retire(zone);
            }
            finally { UnityEngine.Pool.ListPool<Entity>.Release(owners); }
        }
        internal static void OnPlayerTurnEnd(Zone zone)
        {
            var owners = UnityEngine.Pool.ListPool<Entity>.Get();
            try
            {
                owners.AddRange(zone.GetReadOnlyEntitiesWithPart<FiniteLiquidFilmPart>());
                foreach (var owner in owners)
                {
                    if (zone.GetEntityCell(owner) == null) continue;
                    var part = owner.GetPart<FiniteLiquidFilmPart>(); if (part == null) continue;
                    part.RemainingRounds--;
                    if (!part.IsActive) part.Retire(zone);
                }
            }
            finally { UnityEngine.Pool.ListPool<Entity>.Release(owners); }
        }
        /// <summary>Rebuild only leases whose authoritative saved source still exists.</summary>
        internal static void ReprojectAll(Zone zone)
        {
            var owners = UnityEngine.Pool.ListPool<Entity>.Get();
            try
            {
                owners.AddRange(zone.GetReadOnlyEntitiesWithPart<FiniteLiquidFilmPart>());
                foreach (var owner in owners)
                {
                    var part = owner.GetPart<FiniteLiquidFilmPart>();
                    if (part == null) continue;
                    if (!part.IsActive) { part.Retire(zone); continue; }
                    foreach (var cell in zone.GetOccupiedCells(owner)) if (cell != null) part.Project(zone, cell);
                }
            }
            finally { UnityEngine.Pool.ListPool<Entity>.Release(owners); }
        }
    }
}
