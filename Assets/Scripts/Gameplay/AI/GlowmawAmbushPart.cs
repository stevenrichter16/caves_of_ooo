using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Ceiling ambush behavior for the Glowmaw creature.
    /// While lurking (Visible=false), the creature is invisible but its LightSourcePart
    /// still emits light as a lure. When the player walks within TriggerRadius,
    /// the Glowmaw drops down adjacent to the player with a surprise melee attack.
    ///
    /// Must be listed BEFORE BrainPart in the blueprint so it intercepts TakeTurn first.
    /// While lurking, returns false to block BrainPart. After dropping, returns true
    /// so normal chase/combat AI takes over.
    /// </summary>
    public class GlowmawAmbushPart : Part
    {
        public override string Name => "GlowmawAmbush";

        public int TriggerRadius = 2;
        public bool HasDropped = false;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TakeTurn")
                return true;

            // After dropping, let BrainPart handle all AI
            if (HasDropped)
                return true;

            // Get zone from sibling BrainPart
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain == null || brain.CurrentZone == null)
                return true;

            Zone zone = brain.CurrentZone;

            // Find the player
            var players = zone.GetEntitiesWithTag("Player");
            if (players.Count == 0)
                return false; // No player — stay lurking, block Brain

            Entity player = players[0];
            Cell playerCell = zone.GetEntityCell(player);
            Cell myCell = zone.GetEntityCell(ParentEntity);
            if (playerCell == null || myCell == null)
                return false;

            int dist = AIHelpers.ChebyshevDistance(myCell.X, myCell.Y, playerCell.X, playerCell.Y);

            if (dist > TriggerRadius)
                return false; // Player not close enough — stay lurking, block Brain

            // === DROP SEQUENCE ===

            // 1. Find drop cell: prefer cell between glowmaw and player (blocks retreat)
            int dx = Math.Sign(playerCell.X - myCell.X);
            int dy = Math.Sign(playerCell.Y - myCell.Y);
            int dropX, dropY;
            if (FindDropCell(zone, playerCell.X, playerCell.Y, dx, dy, out dropX, out dropY))
            {
                // Move to drop position
                zone.MoveEntity(ParentEntity, dropX, dropY);
            }
            else
            {
                // Can't find a good cell — drop in place
                dropX = myCell.X;
                dropY = myCell.Y;
            }

            // 2. Reveal
            var render = ParentEntity.GetPart<RenderPart>();
            if (render != null)
                render.Visible = true;

            HasDropped = true;

            // 3. Message
            MessageLog.Add("A glowmaw drops from the ceiling!");

            // 4. Drop FX — yellow V falling down, impact sparks
            AsciiFxBus.EmitParticle(zone, dropX, dropY - 1, 'V', "&Y", 0.2f,
                dy: 1, moveInterval: 0.08f);
            AsciiFxBus.EmitParticle(zone, dropX, dropY, '*', "&Y", 0.3f, delay: 0.1f);
            if (zone.InBounds(dropX - 1, dropY))
                AsciiFxBus.EmitParticle(zone, dropX - 1, dropY, '.', "&y", 0.2f, delay: 0.12f);
            if (zone.InBounds(dropX + 1, dropY))
                AsciiFxBus.EmitParticle(zone, dropX + 1, dropY, '.', "&y", 0.2f, delay: 0.12f);

            // 5. Surprise melee attack if adjacent to player
            Cell newCell = zone.GetEntityCell(ParentEntity);
            if (newCell != null && AIHelpers.IsAdjacent(newCell.X, newCell.Y, playerCell.X, playerCell.Y))
            {
                Random rng = brain.Rng ?? new Random();
                CombatSystem.PerformMeleeAttack(ParentEntity, player, zone, rng);
            }

            // 6. Remove light source lure (it's on the ground now)
            var light = ParentEntity.GetPart<LightSourcePart>();
            if (light != null)
                ParentEntity.RemovePart(light);

            // 7. Set Brain's target so it chases immediately on next turn
            brain.Target = player;

            // Block BrainPart this turn — we already acted
            return false;
        }

        /// <summary>
        /// Find a passable cell adjacent to the player to drop into.
        /// Prefers the cell on the far side from the player's approach (blocks retreat).
        /// Falls back to any adjacent passable cell.
        /// </summary>
        private bool FindDropCell(Zone zone, int playerX, int playerY,
            int approachDX, int approachDY, out int dropX, out int dropY)
        {
            // First choice: cell behind player relative to glowmaw approach
            // (between glowmaw and player — the direction player was walking toward)
            int preferX = playerX - approachDX;
            int preferY = playerY - approachDY;
            if (IsCellDroppable(zone, preferX, preferY))
            {
                dropX = preferX;
                dropY = preferY;
                return true;
            }

            // Second: try all 8 adjacent cells, prefer ones closest to glowmaw's
            // original position (so it lands between player and where it was)
            int bestX = -1, bestY = -1;
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int cx = playerX + ox;
                    int cy = playerY + oy;
                    if (IsCellDroppable(zone, cx, cy))
                    {
                        bestX = cx;
                        bestY = cy;
                        // Take the first valid one
                        dropX = bestX;
                        dropY = bestY;
                        return true;
                    }
                }
            }

            dropX = 0;
            dropY = 0;
            return false;
        }

        private bool IsCellDroppable(Zone zone, int x, int y)
        {
            if (!zone.InBounds(x, y)) return false;
            Cell cell = zone.GetCell(x, y);
            if (cell == null) return false;
            if (!cell.IsPassable()) return false;
            // Don't drop onto the player or another solid entity
            if (cell.IsSolid()) return false;
            return true;
        }
    }
}
