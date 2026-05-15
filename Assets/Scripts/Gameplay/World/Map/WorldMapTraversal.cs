using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Player-facing traversal between ground zones and the world-map
    /// zone (ZoneID = <see cref="WorldMap.WorldMapZoneID"/>). Mirrors
    /// Qud's CmdMoveU / CmdMoveD pattern:
    ///
    /// <list type="bullet">
    ///   <item><see cref="Ascend"/> — from an Overworld.X.Y.Z ground
    ///   zone, save <c>LastZoneIDOnSurface</c> + (x, y), then
    ///   transition the player to the worldmap zone at the embedded
    ///   cell that represents the parasang (worldX, worldY) the
    ///   player was in.</item>
    ///   <item><see cref="Descend"/> — from the worldmap zone, read
    ///   the <see cref="WorldMapCellPart"/> at the player's cell to
    ///   resolve the destination world coords, then transition to
    ///   "Overworld.wx.wy.0" at the player's saved
    ///   <c>LastLocationOnSurface</c> (or zone center on first
    ///   descent).</item>
    /// </list>
    ///
    /// <para>The methods are static helpers (no instance state) and
    /// tolerate null inputs without crashing. Used by the
    /// <c>InputHandler</c> bindings for <c>&lt;</c> (ascend) and
    /// <c>&gt;</c> (descend).</para>
    /// </summary>
    public static class WorldMapTraversal
    {
        /// <summary>
        /// Send the player from a ground (Overworld) zone up to the
        /// world-map zone. Returns a <see cref="ZoneTransitionResult"/>
        /// describing the outcome.
        ///
        /// <para>Saves <c>WorldMapPart</c> state on the player so a
        /// subsequent <see cref="Descend"/> can restore them. If the
        /// player has no <c>WorldMapPart</c>, one is added.</para>
        /// </summary>
        public static ZoneTransitionResult Ascend(
            Entity player,
            Zone currentZone,
            ZoneManager zoneManager)
        {
            if (player == null || currentZone == null || zoneManager == null)
                return Fail("Missing argument(s) to Ascend.");

            // Already on the worldmap — no-op (counter-check against
            // accidental ascend-from-worldmap loops).
            if (WorldMap.IsWorldMapZoneID(currentZone.ZoneID))
                return Fail("Already on the world map.");

            // Resolve current ground zone's world coordinates.
            var (wx, wy, wz) = WorldMap.FromZoneID(currentZone.ZoneID);
            // FromZoneID returns (-1,-1,-1) for invalid; also bounds-check
            // since WorldMap.InBounds is an instance method on the data.
            if (wx < 0 || wx >= WorldMap.Width || wy < 0 || wy >= WorldMap.Height)
                return Fail("Current zone is not on the overworld grid.");

            // Save the player's current cell so Descend can restore it.
            var srcCell = currentZone.GetEntityCell(player);
            if (srcCell == null)
                return Fail("Player is detached from current zone.");

            var part = EnsureWorldMapPart(player);
            part.LastZoneIDOnSurface = currentZone.ZoneID;
            part.LastZoneX = srcCell.X;
            part.LastZoneY = srcCell.Y;

            // Resolve the worldmap zone (built on demand) and target cell.
            Zone worldMap = zoneManager.GetZone(WorldMap.WorldMapZoneID);
            if (worldMap == null)
                return Fail("Failed to construct world-map zone.");

            // Mark the destination world-cell as visited for fog-of-war.
            // The cast checks IsOverworldZoneManager because only it owns
            // the live WorldMap instance.
            if (zoneManager is OverworldZoneManager oz)
                oz.WorldMap?.MarkVisited(wx, wy);

            var (targetX, targetY) = WorldMap.WorldCellToZoneCell(wx, wy);
            // Sanity-check the target is passable (the builder guarantees this
            // for the 20×20 region; if not, fall back to nearest passable).
            var targetCell = worldMap.GetCell(targetX, targetY);
            if (targetCell == null || !targetCell.IsPassable())
                return Fail($"World-map cell ({wx},{wy}) is not passable.");

            // Move the player.
            currentZone.RemoveEntity(player);
            worldMap.AddEntity(player, targetX, targetY);

            if (Diag.IsChannelEnabled("worldmap"))
            {
                Diag.Record(
                    category: "worldmap", kind: "Ascended",
                    actor: player,
                    payload: new
                    {
                        fromZoneID = part.LastZoneIDOnSurface,
                        fromZoneX = part.LastZoneX,
                        fromZoneY = part.LastZoneY,
                        toWorldX = wx,
                        toWorldY = wy,
                        toZoneCellX = targetX,
                        toZoneCellY = targetY,
                    });
            }

            return new ZoneTransitionResult
            {
                Success = true,
                NewZone = worldMap,
                NewPlayerX = targetX,
                NewPlayerY = targetY,
            };
        }

        /// <summary>
        /// Send the player from the world-map zone down to the ground
        /// zone for their current world-map cell. Restores the player
        /// to their saved <c>LastLocationOnSurface</c> if available,
        /// otherwise places them at the zone center.
        /// </summary>
        public static ZoneTransitionResult Descend(
            Entity player,
            Zone currentZone,
            ZoneManager zoneManager)
        {
            if (player == null || currentZone == null || zoneManager == null)
                return Fail("Missing argument(s) to Descend.");

            // Must be on the world-map zone.
            if (!WorldMap.IsWorldMapZoneID(currentZone.ZoneID))
                return Fail("Not on the world map.");

            // Read the WorldMapCellPart at the player's current cell.
            var srcCell = currentZone.GetEntityCell(player);
            if (srcCell == null)
                return Fail("Player is detached from world-map zone.");

            int destWorldX = -1, destWorldY = -1;
            for (int i = 0; i < srcCell.Objects.Count; i++)
            {
                var cellPart = srcCell.Objects[i].GetPart<WorldMapCellPart>();
                if (cellPart == null) continue;
                destWorldX = cellPart.WorldX;
                destWorldY = cellPart.WorldY;
                break;
            }
            if (destWorldX < 0)
                return Fail("Player is not standing on a world-map cell.");

            // Resolve target ground zone (surface layer, z=0).
            string targetZoneID = WorldMap.ToZoneID(destWorldX, destWorldY, 0);
            Zone targetZone = zoneManager.GetZone(targetZoneID);
            if (targetZone == null)
                return Fail("Failed to resolve target ground zone.");

            // Where on the target zone? If we have a saved location AND it
            // refers to the same ground zone, use it. Otherwise zone center.
            int arriveX, arriveY;
            var part = player.GetPart<WorldMapPart>();
            if (part != null && part.HasSavedSurface
                && part.LastZoneIDOnSurface == targetZoneID)
            {
                arriveX = part.LastZoneX;
                arriveY = part.LastZoneY;
            }
            else
            {
                arriveX = Zone.Width / 2;
                arriveY = Zone.Height / 2;
            }

            // If the chosen arrival is impassable, search outward.
            var arriveCell = targetZone.GetCell(arriveX, arriveY);
            if (arriveCell == null || !arriveCell.IsPassable())
            {
                (arriveX, arriveY) = FindPassableNear(targetZone, arriveX, arriveY);
                if (arriveX < 0)
                    return Fail("No passable cell found on target zone.");
            }

            currentZone.RemoveEntity(player);
            targetZone.AddEntity(player, arriveX, arriveY);

            if (Diag.IsChannelEnabled("worldmap"))
            {
                Diag.Record(
                    category: "worldmap", kind: "Descended",
                    actor: player,
                    payload: new
                    {
                        fromWorldX = destWorldX,
                        fromWorldY = destWorldY,
                        toZoneID = targetZoneID,
                        toZoneX = arriveX,
                        toZoneY = arriveY,
                        usedSavedLocation = part != null && part.HasSavedSurface
                            && part.LastZoneIDOnSurface == targetZoneID,
                    });
            }

            return new ZoneTransitionResult
            {
                Success = true,
                NewZone = targetZone,
                NewPlayerX = arriveX,
                NewPlayerY = arriveY,
            };
        }

        // ── helpers ──────────────────────────────────────────────────

        private static WorldMapPart EnsureWorldMapPart(Entity player)
        {
            var part = player.GetPart<WorldMapPart>();
            if (part == null)
            {
                part = new WorldMapPart();
                player.AddPart(part);
            }
            return part;
        }

        private static ZoneTransitionResult Fail(string reason)
        {
            return new ZoneTransitionResult
            {
                Success = false,
                ErrorReason = reason,
            };
        }

        private static (int x, int y) FindPassableNear(Zone zone, int cx, int cy)
        {
            for (int radius = 1; radius <= 20; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (!zone.InBounds(nx, ny)) continue;
                        var c = zone.GetCell(nx, ny);
                        if (c != null && c.IsPassable())
                            return (nx, ny);
                    }
                }
            }
            return (-1, -1);
        }
    }
}
