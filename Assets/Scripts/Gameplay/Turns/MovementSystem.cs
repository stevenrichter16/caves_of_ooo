using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles entity movement through a Zone.
    /// Fires BeforeMove, AfterMove, and EntityEnteredCell events so parts
    /// can validate or react.
    ///
    /// Movement flow:
    /// 1. Create BeforeMove event with Actor, TargetCell, Direction
    /// 2. Fire on the actor — PhysicsPart checks for solid blockers
    /// 3. If not blocked, move the entity in the zone
    /// 4. Fire AfterMove event on the mover (post-movement reactions)
    /// 5. Fire EntityEnteredCell on every non-mover occupant of the
    ///    destination cell (M6: rune triggers, future turret proximity
    ///    detection, etc.). Mirrors Qud's <c>ObjectEnteredCellEvent</c>
    ///    fired on mine entities (see Qud's Tinkering_Mine.cs:428).
    /// </summary>
    public static class MovementSystem
    {
        /// <summary>
        /// Attempt to move an entity in a direction (dx, dy).
        /// Returns true if the move succeeded, false if blocked.
        /// </summary>
        public static bool TryMove(Entity entity, Zone zone, int dx, int dy)
        {
            var currentCell = zone.GetEntityCell(entity);
            if (currentCell == null) return false;

            int newX = currentCell.X + dx;
            int newY = currentCell.Y + dy;

            return TryMoveTo(entity, zone, newX, newY);
        }

        /// <summary>
        /// Extended move attempt that also returns what blocked movement.
        /// Returns (moved, blockedBy) where blockedBy is the entity that blocked, or null.
        /// </summary>
        public static (bool moved, Entity blockedBy) TryMoveEx(Entity entity, Zone zone, int dx, int dy)
        {
            var currentCell = zone.GetEntityCell(entity);
            if (currentCell == null) return (false, null);

            int newX = currentCell.X + dx;
            int newY = currentCell.Y + dy;

            if (!zone.InBounds(newX, newY)) return (false, null);

            var targetCell = zone.GetCell(newX, newY);
            if (targetCell == null) return (false, null);

            // Fire BeforeMove event
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)entity);
            beforeMove.SetParameter("TargetCell", (object)targetCell);
            beforeMove.SetParameter("SourceCell", (object)currentCell);
            beforeMove.SetParameter("DX", dx);
            beforeMove.SetParameter("DY", dy);

            bool allowed = entity.FireEvent(beforeMove);
            if (!allowed)
            {
                // Check if something blocked us
                var blocker = beforeMove.GetParameter<Entity>("BlockedBy");
                beforeMove.Release();
                return (false, blocker);
            }
            beforeMove.Release();

            // Perform the move
            int oldX = currentCell.X;
            int oldY = currentCell.Y;
            if (!zone.MoveEntity(entity, newX, newY)) return (false, null);

            NotifyVisualMove(entity, zone, oldX, oldY, newX, newY, false);

            // Notify renderer: old cell needs re-render (entity gone) and
            // new cell needs re-render (entity arrived). For the player we
            // also need a full redraw because FOV / lightmap visibility
            // change can affect distant cells — see DirtyForMove below.
            DirtyForMove(entity, oldX, oldY, newX, newY);

            // Fire AfterMove event
            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)entity);
            afterMove.SetParameter("Cell", (object)targetCell);
            afterMove.SetParameter("OldX", oldX);
            afterMove.SetParameter("OldY", oldY);
            afterMove.SetParameter("NewX", newX);
            afterMove.SetParameter("NewY", newY);
            entity.FireEventAndRelease(afterMove);

            FireCellEnteredEvents(entity, currentCell, targetCell);
            LiquidSlipSystem.ResolveAfterMove(entity, zone, targetCell);

            return (true, null);
        }

        /// <summary>
        /// Attempt to move an entity to a specific cell.
        /// Returns true if the move succeeded, false if blocked or out of bounds.
        /// </summary>
        public static bool TryMoveTo(Entity entity, Zone zone, int x, int y)
        {
            if (!zone.InBounds(x, y)) return false;

            var currentCell = zone.GetEntityCell(entity);
            var targetCell = zone.GetCell(x, y);
            if (targetCell == null) return false;

            // Fire BeforeMove event — parts can block this
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)entity);
            beforeMove.SetParameter("TargetCell", (object)targetCell);
            if (currentCell != null)
            {
                beforeMove.SetParameter("SourceCell", (object)currentCell);
                beforeMove.SetParameter("DX", x - currentCell.X);
                beforeMove.SetParameter("DY", y - currentCell.Y);
            }

            bool allowed = entity.FireEvent(beforeMove);
            beforeMove.Release();
            if (!allowed) return false;

            // Perform the move
            int oldX = currentCell?.X ?? -1;
            int oldY = currentCell?.Y ?? -1;
            if (!zone.MoveEntity(entity, x, y)) return false;

            NotifyVisualMove(entity, zone, oldX, oldY, x, y, false);

            DirtyForMove(entity, oldX, oldY, x, y);

            // Fire AfterMove event
            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)entity);
            afterMove.SetParameter("Cell", (object)targetCell);
            afterMove.SetParameter("OldX", oldX);
            afterMove.SetParameter("OldY", oldY);
            afterMove.SetParameter("NewX", x);
            afterMove.SetParameter("NewY", y);
            entity.FireEventAndRelease(afterMove);

            FireCellEnteredEvents(entity, currentCell, targetCell);
            LiquidSlipSystem.ResolveAfterMove(entity, zone, targetCell);

            return true;
        }

        /// <summary>
        /// Move an entity that is being moved BY SOMETHING ELSE — a
        /// knockback, a shove, a pull. Runs the full post-move pipeline
        /// (<c>AfterMove</c>, cell-entry, render dirtying) but does NOT
        /// fire the vetoable <c>BeforeMove</c> event.
        ///
        /// <para><b>Why BeforeMove is skipped.</b> That event asks "may
        /// this creature move itself?", and
        /// <see cref="StatusEffectsPart.HandleBeforeMove"/> answers it
        /// from <c>AllowMovement</c> — which Stunned clears. A stunned
        /// creature cannot walk, but it can certainly be thrown, and
        /// gating a shove on the victim's own ability to act would let
        /// every knockback in the game be cancelled by the stun the same
        /// attack just applied. <see cref="Cudgel_GroundPound"/> does
        /// exactly that: stun, then push.</para>
        ///
        /// <para>Callers are responsible for deciding whether the
        /// destination is legal (solid, occupied); this method only
        /// bounds-checks. See
        /// <c>SkillCombatHelpers.TryPush</c>.</para>
        ///
        /// <para>Whether an anchored creature (Rooted) should be able to
        /// resist a shove is a live design question, deliberately NOT
        /// decided here — answering it by reusing the voluntary-movement
        /// veto would have silently roped in Stunned too.</para>
        /// </summary>
        /// <param name="slipChain">Slides already taken by the slip that
        /// is calling this (<see cref="LiquidSlipSystem"/>). External
        /// callers — knockbacks, pulls — pass nothing: a shove onto ice
        /// starts a fresh chain, which is correct and delicious.</param>
        public static bool ForceMoveTo(Entity entity, Zone zone, int x, int y, int slipChain = 0)
        {
            if (entity == null || zone == null) return false;
            if (!zone.InBounds(x, y)) return false;

            var currentCell = zone.GetEntityCell(entity);
            var targetCell = zone.GetCell(x, y);
            if (targetCell == null) return false;

            int oldX = currentCell?.X ?? -1;
            int oldY = currentCell?.Y ?? -1;
            if (!zone.MoveEntity(entity, x, y)) return false;

            NotifyVisualMove(entity, zone, oldX, oldY, x, y, true);

            DirtyForMove(entity, oldX, oldY, x, y);

            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)entity);
            afterMove.SetParameter("Cell", (object)targetCell);
            afterMove.SetParameter("OldX", oldX);
            afterMove.SetParameter("OldY", oldY);
            afterMove.SetParameter("NewX", x);
            afterMove.SetParameter("NewY", y);
            afterMove.SetParameter("Forced", true);
            entity.FireEventAndRelease(afterMove);

            FireCellEnteredEvents(entity, currentCell, targetCell);
            LiquidSlipSystem.ResolveAfterMove(entity, zone, targetCell, slipChain);
            return true;
        }

        /// <summary>
        /// Exchange two complete bodies atomically, then dispatch each owner's
        /// landing reactions. Like forced movement, the exchange bypasses the
        /// voluntary BeforeMove veto. A reaction may remove or relocate either
        /// participant; never replay a stale landing after that happens.
        /// </summary>
        public static bool TrySwap(Entity actor, Entity target, Zone zone)
        {
            if (actor == null || target == null || zone == null) return false;
            var actorCell = zone.GetEntityCell(actor);
            var targetCell = zone.GetEntityCell(target);
            if (actorCell == null || targetCell == null || !zone.TrySwapEntities(actor, target)) return false;
            CompleteSwapLanding(actor, zone, actorCell, targetCell, false);
            CompleteSwapLanding(target, zone, targetCell, actorCell, true);
            return true;
        }

        private static void CompleteSwapLanding(Entity owner, Zone zone, Cell source, Cell destination, bool forced)
        {
            if (zone.GetEntityCell(owner) != destination || owner.GetStatValue("Hitpoints", 1) <= 0) return;
            NotifyVisualMove(owner, zone, source.X, source.Y, destination.X, destination.Y, forced);
            DirtyForMove(owner, source.X, source.Y, destination.X, destination.Y);
            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)owner);
            afterMove.SetParameter("Cell", (object)destination);
            afterMove.SetParameter("OldX", source.X);
            afterMove.SetParameter("OldY", source.Y);
            afterMove.SetParameter("NewX", destination.X);
            afterMove.SetParameter("NewY", destination.Y);
            afterMove.SetParameter("Forced", forced);
            owner.FireEventAndRelease(afterMove);
            FireCellEnteredEvents(owner, source, destination);
            LiquidSlipSystem.ResolveAfterMove(owner, zone, destination);
        }

        /// <summary>
        /// Notify the renderer that an entity moved. For the player, we
        /// upgrade to a full-zone dirty flag because FOV / lightmap changes
        /// when the player moves can flip visibility of arbitrary cells —
        /// per-cell tracking can't catch that. Other entities mark only
        /// their old + new cell.
        ///
        /// <para>Cell coords of -1 (no source cell) are dropped silently —
        /// MarkCellDirty bounds-checks anyway, but skipping the call avoids
        /// a no-op delegate dispatch on the rare "first placement" path.</para>
        /// </summary>
        private static void DirtyForMove(Entity entity, int oldX, int oldY, int newX, int newY)
        {
            if (entity != null && entity.HasTag("Player"))
            {
                ZoneRenderHooks.MarkFullDirty("Move.Player");
                return;
            }

            if (oldX >= 0 && oldY >= 0)
                ZoneRenderHooks.MarkCellDirty(oldX, oldY, "Move.Old");
            ZoneRenderHooks.MarkCellDirty(newX, newY, "Move.New");
        }

        private static void NotifyVisualMove(
            Entity entity,
            Zone zone,
            int oldX,
            int oldY,
            int newX,
            int newY,
            bool forced)
        {
            if (entity == null || (oldX == newX && oldY == newY)) return;

            RenderPart render = entity.GetPart<RenderPart>();
            if (render != null && oldX >= 0 && oldY >= 0)
                render.VisualFacing = FacingForDelta(newX - oldX, newY - oldY, render.VisualFacing);

            EntityVisualHooks.EmitMoved(entity, zone, oldX, oldY, newX, newY, forced);
        }

        private static EntityVisualFacing FacingForDelta(
            int dx,
            int dy,
            EntityVisualFacing fallback)
        {
            if (System.Math.Abs(dx) >= System.Math.Abs(dy) && dx != 0)
                return dx < 0 ? EntityVisualFacing.West : EntityVisualFacing.East;
            if (dy != 0)
                return dy < 0 ? EntityVisualFacing.North : EntityVisualFacing.South;
            return fallback;
        }

        /// <summary>
        /// Fire <c>EntityEnteredCell</c> on every non-mover occupant of
        /// <paramref name="targetCell"/>. Mirrors Qud's
        /// <c>ObjectEnteredCellEvent</c> dispatched against every object
        /// in the destination cell (see Qud's Tinkering_Mine.cs:428).
        ///
        /// <para><b>Snapshot iteration.</b> Consumers may mutate the cell's
        /// <c>Objects</c> list during handling — e.g. a single-use rune with
        /// <c>ConsumeOnTrigger=true</c> removing itself from the zone — so
        /// we iterate over a pre-captured snapshot.</para>
        ///
        /// <para><b>Mid-dispatch mover death.</b> If a listener's payload
        /// kills or removes the mover from the zone (e.g. rune damage
        /// reduces HP ≤ 0 → <see cref="CombatSystem.HandleDeath"/> →
        /// <c>zone.RemoveEntity(mover)</c>), we break the dispatch loop.
        /// Without this, two co-located runes would both fire on the same
        /// stepper and both call <c>HandleDeath</c> — which is not
        /// idempotent: it emits a duplicate "X is killed by Y" message,
        /// double-fires the <c>"Died"</c> event, and can double-spawn
        /// corpses via <c>CorpsePart</c>. Catches the CR-01 finding from
        /// the M6 review.</para>
        ///
        /// <para><b>Cell-CHANGE only (LQ.4 latent-bug fix).</b> Returns
        /// early when <paramref name="sourceCell"/> is the same cell as
        /// <paramref name="targetCell"/> (a no-op <c>TryMove(0,0)</c> /
        /// blocked-into-self). Before this guard the dispatch fired even
        /// when the mover never changed cells, so a creature issuing a
        /// zero-delta move while standing on a rune/mine/pressure-plate
        /// re-triggered it, and a creature standing in a liquid pool
        /// re-coated every such call. The cell-CHANGE contract was
        /// previously only true by caller-convention (the AI/input never
        /// issues a 0,0 move) and was *documented as guaranteed* in
        /// <see cref="PressurePlateTriggerPart"/> ("EntityEnteredCell
        /// fires only on cell-CHANGE moves") — this makes it true in
        /// code. A null <paramref name="sourceCell"/> (first placement)
        /// is treated as a genuine entry and still fires. Compared by
        /// coordinate, not reference, so it is robust regardless of
        /// whether <c>GetCell</c> returns a stable wrapper.</para>
        /// </summary>
        private static void FireCellEnteredEvents(Entity mover, Cell sourceCell, Cell targetCell)
        {
            if (targetCell == null) return;
            // Cell-CHANGE only: a move that didn't change cells is not an
            // "enter". sourceCell == null (first placement) IS an entry.
            if (sourceCell != null
                && sourceCell.X == targetCell.X
                && sourceCell.Y == targetCell.Y)
                return;
            var zone = targetCell.ParentZone;
            if (zone == null) return;
            // A nested entry (a trap teleports someone) owns a different pooled
            // buffer. Snapshot both the recipient and contact cell before events.
            var entries = UnityEngine.Pool.ListPool<(Entity owner, Cell cell)>.Get();
            try
            {
                var oldCells = sourceCell == null ? default : zone.GetOccupiedCells(mover,sourceCell.X,sourceCell.Y);
                foreach (var cell in zone.GetOccupiedCells(mover))
                {
                    if (cell == null) continue;
                    bool wasCovered=false;
                    foreach(var old in oldCells) if (old == cell) { wasCovered=true; break; }
                    if(wasCovered) continue;
                    foreach(var owner in cell.Occupants)
                    {
                        if(owner == null || owner == mover) continue;
                        bool seen=false;
                        foreach(var entry in entries) if(entry.owner == owner) { seen=true; break; }
                        if(!seen) entries.Add((owner,cell));
                    }
                }
                for(int entryIndex=0;entryIndex<entries.Count;entryIndex++)
                {
                    var entry=entries[entryIndex];
                    if(mover.HasPart<SpatialFootprintPart>())
                    {
                        var gas=entry.owner.GetPart<IObjectGasBehaviorPart>();
                        if(gas!=null)
                        {
                            bool superseded=false;
                            for(int j=0;j<entries.Count;j++)
                            {
                                if(j==entryIndex) continue;
                                var other=entries[j].owner.GetPart<IObjectGasBehaviorPart>();
                                if(SpatialGasExposure.SameFamily(gas,other)
                                    && (SpatialGasExposure.Stronger(other,gas)
                                        || (!SpatialGasExposure.Stronger(gas,other) && j<entryIndex)))
                                {superseded=true;break;}
                            }
                            if(superseded) continue;
                        }
                    }
                    // Stop after death or a nested relocation. Removed listeners
                    // cannot fire from a stale snapshot, but unrelated listeners can.
                    if(zone.GetEntityCell(mover) != targetCell) break;
                    if(!entry.cell.Occupants.Contains(entry.owner)) continue;
                    var ev = GameEvent.New("EntityEnteredCell");
                    ev.SetParameter("Actor", (object)mover);
                    ev.SetParameter("Cell", (object)entry.cell);
                    ev.SetParameter("Zone", (object)zone);
                    entry.owner.FireEventAndRelease(ev);
                }
            }
            finally { UnityEngine.Pool.ListPool<(Entity owner, Cell cell)>.Release(entries); }
        }

        /// <summary>
        /// Convert a cardinal/ordinal direction index to dx/dy.
        /// Directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        /// </summary>
        public static (int dx, int dy) DirectionToDelta(int direction)
        {
            switch (direction)
            {
                case 0: return (0, -1);   // N
                case 1: return (1, -1);   // NE
                case 2: return (1, 0);    // E
                case 3: return (1, 1);    // SE
                case 4: return (0, 1);    // S
                case 5: return (-1, 1);   // SW
                case 6: return (-1, 0);   // W
                case 7: return (-1, -1);  // NW
                default: return (0, 0);
            }
        }
    }
}
