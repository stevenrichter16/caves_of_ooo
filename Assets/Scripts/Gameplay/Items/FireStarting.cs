using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Lighting what is beside you from the interact (C) menu of the thing you are lighting, without opening the pack. A cell offers a "light it" row when one point of heat there would
    /// ignite a combustible coating (<see cref="TileReactionSystem.HeatWouldIgnite"/>), you are next to it, and you carry a way to light it: a lit torch in hand (free, preferred) or a flint
    /// striker (one strike). The row names the carried item that performs it (<see cref="InventoryAction.Via"/>), so the world menu runs the same command the pack menu runs.
    /// Fires and torches already light from their own menus through <see cref="IgnitionSources"/>; this is the same idea for a tile that has no object to hang a row on.
    /// </summary>
    internal static class FireStarting
    {
        internal const string TorchCellPrefix = "TorchLightCell|";

        /// <summary>A cell beside the actor (not their own) where a spark would light something.</summary>
        internal static bool Ignitable(Entity actor, Zone zone, int x, int y)
        {
            var origin = zone?.GetEntityCell(actor);
            if (origin == null || zone.GetCell(x, y) == null) return false;
            return Math.Abs((long)x - origin.X) <= 1 && Math.Abs((long)y - origin.Y) <= 1 && !(x == origin.X && y == origin.Y) && TileReactionSystem.HeatWouldIgnite(zone, x, y);
        }

        internal static string CellCommand(string prefix, Zone zone, Cell origin, int x, int y, Entity tool)
            => prefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y) + "|" + tool.ID;

        internal static bool ParseCell(string command, out string zoneId, out int ox, out int oy, out int x, out int y, out string toolId)
        {
            zoneId = null; toolId = null; ox = oy = x = y = 0;
            string[] f = command.Split('|');
            if (f.Length != 7 || !Int(f[2], out ox) || !Int(f[3], out oy) || !Int(f[4], out x) || !Int(f[5], out y)) return false;
            zoneId = Uri.UnescapeDataString(f[1]); toolId = f[6]; return true;
        }

        static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        /// <summary>A torch the actor holds (equipped) whose controlled flame is burning.</summary>
        internal static Entity HeldLitTorch(Entity actor)
        {
            var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return null;
            foreach (var item in pack.EquippedItems.Values.Distinct())
                if (item != null && item.GetPart<TorchLightPart>()?.ControlledFlameActive == true && item.GetPart<PhysicsPart>()?.Equipped == actor) return item;
            return null;
        }

        /// <summary>Queues the spark: heat goes into the cell through the routine every fire ability uses, once, after the action commits (tile heat cannot be undone).</summary>
        internal static void QueueIgnite(Entity actor, Zone zone, int x, int y, InventoryTransaction tx, Entity tool, string message, string source, string diagKind, Func<object> payload)
        {
            tx.AfterCommit(() =>
            {
                MessageLog.Add(message);
                ZoneTileStateSystem.ApplyFireToTile(zone, x, y, actor, source);
                ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
                Diag.Record("event", diagKind, actor, tool, payload());
            });
        }

        /// <summary>" - next to the camp box" when one of your camp kits stands beside the cell, so the fire is not lit next to your things unawares. Empty otherwise.</summary>
        static string Warning(Zone zone, Cell cell)
        {
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                var around = zone.GetCell(cell.X + dx, cell.Y + dy); if (around == null) continue;
                foreach (var e in around.Objects) if (e != null && e.HasPart<PackableFixturePart>()) return " - next to the " + e.GetDisplayName();
            }
            return "";
        }

        /// <summary>Adds the "light it" row for <paramref name="cell"/> to a world menu's rows.</summary>
        internal static void AddCellRows(List<InventoryAction> rows, Cell cell, Entity actor)
        {
            var zone = cell?.ParentZone;
            if (rows == null || zone == null || actor == null || !WorldResourceActions.ActorCurrent(actor, zone) || !Ignitable(actor, zone, cell.X, cell.Y)) return;
            var origin = zone.GetEntityCell(actor);
            var torch = HeldLitTorch(actor);
            string warning = Warning(zone, cell);
            if (torch != null)
            {
                rows.Add(new InventoryAction("LightCell", "light it with the torch" + warning, CellCommand(TorchCellPrefix, zone, origin, cell.X, cell.Y, torch), '\0', 40) { Time = ActionTime.OneAction, Via = torch });
                return;
            }
            Entity best = null, any = null;
            foreach (var item in actor.GetPart<InventoryPart>()?.Objects.ToArray() ?? new Entity[0])
            {
                var part = item?.GetPart<FlintStrikerPart>(); if (part == null) continue;
                any ??= item;
                if (!FlintStrikerPart.Usable(actor, item)) continue;
                if (best == null || part.Strikes < best.GetPart<FlintStrikerPart>().Strikes) best = item;
            }
            if (best != null)
                rows.Add(new InventoryAction("LightCell", "light it with the flint striker (1 strike, " + best.GetPart<FlintStrikerPart>().Strikes + " left)" + warning, FlintStrikerPart.SparkCommand(zone, origin, cell.X, cell.Y, best), '\0', 40) { Time = ActionTime.OneAction, Via = best });
            else if (any != null)
                rows.Add(new InventoryAction("LightCell", "light it with the flint striker", InventoryAction.DisabledCommandPrefix + "striker_unusable", '\0', 40)
                { DisabledCode = "striker_unusable", DisabledReason = any.GetPart<FlintStrikerPart>().Strikes <= 0 ? "The flint is worn smooth." : "The flint will not spark while it is wet or frozen." });
        }
    }
}
