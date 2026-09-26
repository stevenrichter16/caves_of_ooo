using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Restrained contextual remarks from explicitly speaking roles.
    /// Reads present bodies and physical context; uses only cosmetic hash sampling.</summary>
    public static class WorldRemarks
    {
        public const int Radius = 5;
        public const int ChancePercent = 5;

        /// <summary>One optional line after Sari has declined the shared ambient
        /// budget. Invalid, unseen, hostile, busy or contextless speakers stay quiet.</summary>
        public static bool TryEmit(Entity player, Zone zone)
        {
            if (!WorldAmbience.IsPresentPlayer(player, zone) || !WorldAmbience.MayEmit(player)) return false;
            int turn = player.GetIntProperty(WorldAmbience.TurnProperty);
            if (turn < 0 || Hash(zone.ZoneID + ":remark:" + turn) % 100 >= ChancePercent) return false;
            int minX = Zone.Width - 1, minY = Zone.Height - 1, maxX = 0, maxY = 0;
            foreach (var bodyCell in zone.GetOccupiedCells(player))
            {
                minX = Math.Min(minX, bodyCell.X); minY = Math.Min(minY, bodyCell.Y);
                maxX = Math.Max(maxX, bodyCell.X); maxY = Math.Max(maxY, bodyCell.Y);
            }
            Entity selected = null; string words = null; uint best = uint.MaxValue;
            var seen = new HashSet<Entity>();
            // A normal player scans at most121 cells; extended bodies scan their
            // expanded bounds once, never more than the finite zone grid.
            for (int y = Math.Max(0, minY - Radius); y <= Math.Min(Zone.Height - 1, maxY + Radius); y++)
                for (int x = Math.Max(0, minX - Radius); x <= Math.Min(Zone.Width - 1, maxX + Radius); x++)
                    foreach (var candidate in zone.GetCell(x, y).Occupants)
                    {
                        if (!seen.Add(candidate) || !Eligible(candidate, player, zone)) continue;
                        string line = Context(candidate, zone);
                        if (line == null) continue;
                        uint rank = Hash((candidate.ID ?? candidate.BlueprintName) + ":" + turn);
                        if (selected == null || rank < best) { selected = candidate; words = line; best = rank; }
                    }
            if (selected == null) return false;
            WorldAmbience.MarkEmission(player);
            Diag.Record("event", "WorldRemark", actor: selected, target: player,
                payload: new { zone = zone.ZoneID, role = selected.BlueprintName });
            try { MessageLog.Add(selected.GetDisplayName() + " says, \"" + words + "\""); }
            catch (Exception error)
            {
                Diag.Record("event", "AmbientMessageObserverFailed", actor: player,
                    payload: new { kind = "WorldRemark", exception = error.GetType().Name });
            }
            return true;
        }

        private static bool Eligible(Entity actor, Entity player, Zone zone)
        {
            if (actor == null || ReferenceEquals(actor, player) || !Present(actor, zone)
                || actor.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(actor)
                || string.IsNullOrEmpty(actor.GetPart<RenderPart>()?.DisplayName)
                || actor.GetPart<BrainPart>()?.Target != null
                || FactionManager.IsHostile(actor, player) || FactionManager.IsHostile(player, actor)) return false;
            if (actor.BlueprintName != "Scribe" && actor.BlueprintName != "Innkeeper"
                && actor.BlueprintName != "Farmer" && actor.BlueprintName != "Warden") return false;
            if (SpatialQuery.Distance(zone, player, actor) > Radius) return false;
            foreach (var cell in zone.GetOccupiedCells(actor))
                if (cell.IsVisible)
                    foreach (var origin in zone.GetOccupiedCells(player))
                        if (AIHelpers.HasLineOfSight(zone, origin.X, origin.Y, cell.X, cell.Y)) return true;
            return false;
        }

        private static string Context(Entity actor, Zone zone)
        {
            if (actor.BlueprintName == "Scribe")
            {
                var seat = actor.GetEffect<SittingEffect>()?.Furniture;
                if (Present(seat, zone) && !CombatSystem.IsDeathHandled(seat) && SpatialQuery.Distance(zone, actor, seat) <= 1
                    && (seat.GetPart<ChairPart>()?.Occupied == true || seat.GetPart<BedPart>()?.Occupied == true))
                    return "A moment for my hands.";
                return null;
            }
            if (actor.BlueprintName == "Warden")
            {
                var (_, _, depth) = WorldMap.FromZoneID(zone.ZoneID);
                return depth == 0 && !zone.GetEntityCell(actor).IsInterior && WorldClock.GetBand(WorldClock.CurrentTick) == DayBand.Dark
                    ? "Watch your footing in the dark." : null;
            }
            foreach (var bodyCell in zone.GetOccupiedCells(actor))
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = zone.GetCell(bodyCell.X + dx, bodyCell.Y + dy);
                    if (cell == null) continue;
                    foreach (var item in cell.Occupants)
                    {
                        if (!Present(item, zone) || CombatSystem.IsDeathHandled(item)) continue;
                        if (actor.BlueprintName == "Innkeeper" && item.HasPart<CampfirePart>()) return "Mind the embers.";
                        if (actor.BlueprintName == "Farmer" && item.HasPart<WellSitePart>()) return "Leave the well rim clear.";
                    }
                }
            return null;
        }

        private static bool Present(Entity actor, Zone zone)
        {
            if (actor == null || !ReferenceEquals(actor.SpatialZone, zone)) return false;
            var cell = zone.GetEntityCell(actor);
            return cell != null && cell.Objects.Contains(actor);
        }
        internal static uint Hash(string value)
        {
            unchecked { uint hash = 2166136261; foreach (char c in value ?? "") hash = (hash ^ c) * 16777619; return hash; }
        }
    }
}
