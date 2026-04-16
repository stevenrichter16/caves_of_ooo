using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Default bottom-of-stack goal. Scans for hostiles, wanders, or idles.
    /// Mirrors Qud's Bored goal handler.
    ///
    /// Decision order:
    /// 1. If sitting: hostile → stand up and fight; no hostile → stay seated
    /// 2. Scan for hostile → push KillGoal or FleeGoal
    /// 3. Fire AIBoredEvent → if consumed, return
    /// 4. Check WhenBoredReturnToOnce → push MoveToGoal
    /// 5. If Staying and not at home → push MoveToGoal(StartingCell)
    /// 6. If AllowIdleBehavior → scan furniture → push MoveToGoal + DelegateGoal
    /// 7. If Staying and at home → push WaitGoal
    /// 8. If Wanders → push WanderRandomlyGoal
    /// 9. Else → push WaitGoal(1)
    /// </summary>
    public class BoredGoal : GoalHandler
    {
        public override bool IsBusy() => false;

        public override void TakeAction()
        {
            // 1. If currently sitting, check for threats
            if (ParentEntity.HasEffect<SittingEffect>())
            {
                Entity sittingHostile = AIHelpers.FindNearestHostile(ParentEntity, CurrentZone, ParentBrain.SightRadius);
                if (sittingHostile != null)
                {
                    // Stand up and fight
                    ParentEntity.RemoveEffect<SittingEffect>();
                    ParentBrain.Target = sittingHostile;
                    PushChildGoal(new KillGoal(sittingHostile));
                    return;
                }

                // Stay seated
                ParentBrain.CurrentState = AIState.Idle;
                PushChildGoal(new WaitGoal(1));
                return;
            }

            // 2. Scan for hostiles
            Entity hostile = AIHelpers.FindNearestHostile(ParentEntity, CurrentZone, ParentBrain.SightRadius);
            if (hostile != null)
            {
                bool firstAggro = ParentBrain.Target == null;
                ParentBrain.Target = hostile;

                if (firstAggro)
                {
                    var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (myPos.x >= 0)
                        AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '!', "&R", 0.25f);
                }

                if (ShouldFlee())
                    PushChildGoal(new FleeGoal(hostile));
                else
                    PushChildGoal(new KillGoal(hostile));
                return;
            }

            // 3. Fire AIBoredEvent — let behavior parts handle custom idle behavior
            if (!AIBoredEvent.Check(ParentEntity))
                return;

            // 4. Check one-shot return destination
            string returnOnce = null;
            ParentEntity.Properties?.TryGetValue("WhenBoredReturnToOnce", out returnOnce);
            if (!string.IsNullOrEmpty(returnOnce))
            {
                ParentEntity.Properties.Remove("WhenBoredReturnToOnce");
                var parts = returnOnce.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int rx) && int.TryParse(parts[1], out int ry))
                {
                    var pos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (pos.x != rx || pos.y != ry)
                    {
                        PushChildGoal(new MoveToGoal(rx, ry, 200));
                        return;
                    }
                }
            }

            // 5. Staying: return to starting cell if drifted
            if (ParentBrain.Staying && ParentBrain.HasStartingCell)
            {
                var pos = CurrentZone.GetEntityPosition(ParentEntity);
                if (pos.x != ParentBrain.StartingCellX || pos.y != ParentBrain.StartingCellY)
                {
                    PushChildGoal(new MoveToGoal(ParentBrain.StartingCellX, ParentBrain.StartingCellY, 200));
                    return;
                }
            }

            // 6. Idle furniture query: scan zone objects for idle offers
            if (ParentEntity.HasTag("AllowIdleBehavior"))
            {
                var offer = ScanForIdleOffer();
                if (offer != null)
                {
                    var pos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (pos.x == offer.TargetX && pos.y == offer.TargetY)
                    {
                        // Already at furniture — execute immediately (no gate needed, we're here)
                        PushChildGoal(new DelegateGoal(offer.Action));
                    }
                    else
                    {
                        // Push position-gated delegate first (lower), then MoveToGoal on top.
                        // MoveToGoal runs first; when it finishes, DelegateGoal checks position.
                        // If MoveToGoal failed, DelegateGoal runs Cleanup instead (rollback).
                        PushChildGoal(new DelegateGoal(
                            offer.Action,
                            offer.Cleanup,
                            offer.TargetX,
                            offer.TargetY));
                        PushChildGoal(new MoveToGoal(offer.TargetX, offer.TargetY, 50));
                    }
                    return;
                }
            }

            // 7. At home and Staying → idle in place.
            // Important: do NOT push a child goal here. Pushing WaitGoal(1) would
            // block BoredGoal from re-running next tick, delaying hostile reactivity
            // by 2 ticks. Just set state and return — BoredGoal stays on top and
            // re-scans for threats every tick.
            if (ParentBrain.Staying && ParentBrain.HasStartingCell)
            {
                ParentBrain.CurrentState = AIState.Idle;
                return;
            }

            // 8. Wander or idle
            if (ParentBrain.Wanders && ParentBrain.WandersRandomly)
            {
                PushChildGoal(new WanderRandomlyGoal());
            }
            else
            {
                PushChildGoal(new WaitGoal(1));
            }
        }

        private IdleOffer ScanForIdleOffer()
        {
            var zone = CurrentZone;
            if (zone == null) return null;

            var rng = Rng;

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                // Quick filter: only furniture with idle-query-capable parts
                if (!entity.HasPart<ChairPart>() && !entity.HasPart<BedPart>())
                    continue;

                // 1% chance to consider each piece of furniture per tick
                if (rng.Next(100) >= 1) continue;

                var offer = IdleQueryEvent.QueryOffer(entity, ParentEntity);
                if (offer != null) return offer;
            }
            return null;
        }
    }
}
