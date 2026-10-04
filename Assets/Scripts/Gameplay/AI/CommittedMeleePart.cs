using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Opt-in NPC attack: one windup action, one fixed-ray primary melee strike,
    /// then one recovery action. This is a CoO-original commitment, not a skill.
    /// Public state fields are persisted by the existing reflected Part codec.
    /// </summary>
    public sealed class CommittedMeleePart : Part
    {
        public override string Name => "CommittedMelee";
        public string AttackName = "heavy swing";
        public int Reach = 2;
        public enum AttackStage { Idle, Windup, Recovery }
        public AttackStage Stage;
        public int OriginX, OriginY, DirectionX, DirectionY;
        public string CommittedZoneId = "";
        public Entity OriginalTarget;
        // Resolution's EndTurn must not consume the following recovery action.
        public bool RecoveryFresh;
        public bool IsWindingUp => Stage == AttackStage.Windup;
        public bool IsRecovering => Stage == AttackStage.Recovery;
        public const int RecoveryDVPenalty = 2;
        private int BoundedReach => Math.Max(1, Math.Min(2, Reach));

        /// <summary>Start only at an exact hostile first impact on an eight-direction ray.
        /// Returning true means this actor's entire current action was spent winding up.</summary>
        public bool TryBegin(Entity target, Zone zone)
        {
            var actor = ParentEntity;
            var brain = GetPart<BrainPart>();
            string reason = null;
            if (Stage != AttackStage.Idle) reason = "already-committed";
            else if (actor == null || brain == null || actor.HasTag("Player") || zone == null ||
                actor.SpatialZone != zone || !Alive(actor)) reason = "invalid-actor";
            else if (brain.InConversation || HasActiveNoFight(brain) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true)
                reason = "actor-controlled";
            else if (target == null || target == actor || !target.HasTag("Creature") || !Alive(target) ||
                target.SpatialZone != zone || !FactionManager.IsHostile(actor, target)) reason = "invalid-hostile";
            if (reason != null) return Reject(target, reason);

            var source = zone.GetEntityCell(actor);
            if (source == null) return Reject(target, "actor-not-placed");
            // Check actual occupied surfaces, so a large target can be struck at
            // a near contact without treating its distant anchor as the body.
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var hit = Trace(zone, source.X, source.Y, dx, dy, out _);
                    if (hit != target) continue;
                    OriginX = source.X; OriginY = source.Y;
                    DirectionX = dx; DirectionY = dy;
                    CommittedZoneId = zone.ZoneID; OriginalTarget = target;
                    Stage = AttackStage.Windup; RecoveryFresh = false;
                    brain.Target = target;
                    Record("CommittedMeleeWindup", target, "clear-target");
                    SayVisible(zone, actor.GetDisplayName() + " draws back for " + AttackName + " " + DirectionName() + ".");
                    Dirty(zone);
                    return true;
                }
            return Reject(target, "no-clear-target-ray");
        }

        /// <summary>Brain calls this before any goal. True consumes the scheduled
        /// action even if the old goal was replaced, cancelled, or interrupted.</summary>
        public bool AdvancePendingAction(Zone zone, Random rng)
        {
            if (Stage == AttackStage.Idle) return false;
            if (!Alive(ParentEntity)) { Record("CommittedMeleeCancelled", OriginalTarget, "dead"); Clear(); return true; }
            if (IsRecovering) return true;
            string invalid = InvalidCommitment(zone);
            if (invalid != null) { Cancel(invalid); return true; }

            var victim = Trace(zone, OriginX, OriginY, DirectionX, DirectionY, out bool blocked);
            // Transition before combat: reactions may stun, displace, or kill us.
            Stage = AttackStage.Recovery; RecoveryFresh = true;
            Dirty(zone);
            bool attempted = victim != null && CombatSystem.PerformCommittedMeleeAttack(ParentEntity, victim, zone, rng ?? new Random());
            Record("CommittedMeleeResolved", victim, blocked ? "blocked" : victim == null ? "empty-ray" : attempted ? "strike" : "vetoed");
            SayVisible(zone, ParentEntity.GetDisplayName() + " finishes " + AttackName + " and pauses to recover.");
            return true;
        }

        /// <summary>Interrupt a pending windup. The next scheduled opportunity
        /// remains spent recovering; a refused effect must never call this.</summary>
        public void Cancel(string reason)
        {
            if (!IsWindingUp) return;
            Stage = AttackStage.Recovery; RecoveryFresh = false;
            Record("CommittedMeleeCancelled", OriginalTarget, reason);
            var zone = ParentEntity?.SpatialZone;
            SayVisible(zone, ParentEntity.GetDisplayName() + " loses " + AttackName + " and pauses to recover.");
            Dirty(zone);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Died")
            {
                if (Stage != AttackStage.Idle) Record("CommittedMeleeCancelled", OriginalTarget, "dead");
                Clear(); return true;
            }
            if (e.ID == "AfterMove" && IsWindingUp)
            {
                var cell = ParentEntity?.SpatialZone?.GetEntityCell(ParentEntity);
                if (cell == null || cell.X != OriginX || cell.Y != OriginY || cell.ParentZone.ZoneID != CommittedZoneId)
                    Cancel("displaced");
            }
            else if (e.ID == "EffectApplied" && IsWindingUp)
            {
                var effect = e.GetParameter<Effect>("Effect");
                if ((effect is StunnedEffect || effect is FrozenEffect || effect is ParalyzedEffect ||
                    effect is AsleepByGasEffect || effect is HibernatingEffect) && !effect.AllowAction(ParentEntity))
                    Cancel("effect-" + effect.ClassName);
            }
            else if (e.ID == "EndTurn" && IsRecovering)
            {
                if (RecoveryFresh) RecoveryFresh = false;
                else { Record("CommittedMeleeRecovered", OriginalTarget, "opportunity-spent"); Clear(); }
            }
            return true;
        }

        /// <summary>Currently exposed cells of the saved windup, stopping at the
        /// first creature or physical barrier. Callers separately gate visibility.
        /// This read-only query never fires events or calls effect/goal callbacks.</summary>
        public bool ThreatensCell(Zone zone, int x, int y)
        {
            if (!IsWindingUp || zone == null || zone.ZoneID != CommittedZoneId ||
                InvalidCommitment(zone, checkActionBlock: false) != null) return false;
            for (int i = 1; i <= BoundedReach; i++)
            {
                var cell = zone.GetCell(OriginX + DirectionX * i, OriginY + DirectionY * i);
                if (cell == null) return false;
                var victim = FirstImpact(cell, out bool blocked);
                if (blocked) return false;
                if (x == cell.X && y == cell.Y) return true;
                if (victim != null) return false;
            }
            return false;
        }

        /// <summary>Presentation-neutral state text; consumers gate visibility.</summary>
        public string Describe()
        {
            if (IsWindingUp) return "Drawing back for " + AttackName + " " + DirectionName() + " (reach " + BoundedReach + ").";
            if (IsRecovering) return "Recovering from " + AttackName + "; exposed until its next action.";
            return "";
        }

        private string InvalidCommitment(Zone zone, bool checkActionBlock = true)
        {
            var actor = ParentEntity; var brain = GetPart<BrainPart>();
            if (zone == null || actor == null || actor.SpatialZone != zone || zone.ZoneID != CommittedZoneId) return "zone-changed";
            var source = zone.GetEntityCell(actor);
            if (source == null || source.X != OriginX || source.Y != OriginY) return "displaced";
            if (!Alive(actor)) return "dead";
            if (brain == null || brain.InConversation || HasActiveNoFight(brain)) return "no-fight";
            if (OriginalTarget == null || brain.Target != OriginalTarget || !Alive(OriginalTarget) ||
                OriginalTarget.SpatialZone != zone || zone.GetEntityCell(OriginalTarget) == null ||
                !FactionManager.IsHostile(actor, OriginalTarget)) return "lost-hostile-target";
            if (checkActionBlock && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "action-blocked";
            if (DirectionX < -1 || DirectionX > 1 || DirectionY < -1 || DirectionY > 1 ||
                (DirectionX == 0 && DirectionY == 0)) return "invalid-direction";
            return null;
        }

        private static bool HasActiveNoFight(BrainPart brain)
        {
            var calm = brain?.FindGoal<NoFightGoal>();
            return calm != null && (calm.Duration <= 0 || calm.Age < calm.Duration);
        }
        private static bool Alive(Entity e) => e != null && e.GetStatValue("Hitpoints", 0) > 0 && !CombatSystem.IsDeathHandled(e);

        // Deliberately narrower than projectile targeting: harmless loose items
        // do not intercept, and scenery is never passed into creature damage.
        private Entity Trace(Zone zone, int x, int y, int dx, int dy, out bool blocked)
        {
            blocked = false;
            for (int step = 1; step <= BoundedReach; step++)
            {
                var cell = zone.GetCell(x + dx * step, y + dy * step);
                if (cell == null) { blocked = true; return null; }
                var victim = FirstImpact(cell, out blocked);
                if (blocked || victim != null) return victim;
            }
            return null;
        }

        private Entity FirstImpact(Cell cell, out bool blocked)
        {
            blocked = false;
            if (MorrowfastSceneRuntime.BlockingOwner(cell, ParentEntity) != null) { blocked = true; return null; }
            // A physical barrier wins even if an overlapping creature was
            // inserted first into the cell's render-sorted occupant list.
            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                var other = cell.Occupants[i];
                if (other == ParentEntity || other.HasTag("Creature")) continue;
                if (other.HasTag("Solid") || other.HasTag("Wall") || other.GetPart<PhysicsPart>()?.Solid == true ||
                    other.GetPart<DoorPart>()?.IsClosed == true || other.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)
                { blocked = true; return null; }
            }
            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                var other = cell.Occupants[i];
                if (other != ParentEntity && other.HasTag("Creature") && Alive(other)) return other;
            }
            return null;
        }

        private string DirectionName()
        {
            if (DirectionY < 0) return DirectionX < 0 ? "northwest" : DirectionX > 0 ? "northeast" : "north";
            if (DirectionY > 0) return DirectionX < 0 ? "southwest" : DirectionX > 0 ? "southeast" : "south";
            return DirectionX < 0 ? "west" : "east";
        }
        private void Clear()
        {
            var zone = ParentEntity?.SpatialZone;
            Stage = AttackStage.Idle; RecoveryFresh = false; OriginalTarget = null;
            Dirty(zone);
        }
        private void Dirty(Zone zone)
        {
            var cell = zone?.GetEntityCell(ParentEntity);
            if (cell != null) ZoneRenderHooks.MarkCellDirty(cell, "CommittedMelee");
            if (zone != null && zone.ZoneID == CommittedZoneId)
                for (int i = 1; i <= BoundedReach; i++)
                    ZoneRenderHooks.MarkCellDirty(OriginX + DirectionX * i, OriginY + DirectionY * i, "CommittedMelee");
        }
        private void SayVisible(Zone zone, string text)
        {
            if (zone?.GetEntityCell(ParentEntity)?.IsVisible == true && GetPart<RenderPart>()?.Visible != false)
                MessageLog.Add(text);
        }
        private bool Reject(Entity target, string reason) { Record("CommittedMeleeRejected", target, reason); return false; }
        private void Record(string kind, Entity target, string reason)
        {
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", kind, ParentEntity, target,
                new { attack = AttackName, reason, stage = Stage.ToString(), zone = CommittedZoneId,
                    x = OriginX, y = OriginY, dx = DirectionX, dy = DirectionY, reach = BoundedReach });
        }
    }
}
