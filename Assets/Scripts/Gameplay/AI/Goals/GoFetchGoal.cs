using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Fetches an actual ground object. Legacy callers keep ordinary pickup and
    /// optional best-effort home return. AIRetriever uses an explicit mode that
    /// retains the thrown object, follows its current thrower and drops it.
    /// Progress fields use the existing public-field save stream; no callbacks
    /// or temporary path caches need to be reconstructed after loading.
    /// </summary>
    public class GoFetchGoal : GoalHandler
    {
        /// <summary>Max MoveToGoal pushes to reach the item before giving up.</summary>
        public const int MaxWalkAttempts = 2;

        /// <summary>Ticks each inner MoveToGoal is allowed before it times out.</summary>
        public const int WalkStepBudget = 100;

        public Entity Item;
        public bool ReturnHome;

        // Mutable public fields are deliberately saved by SaveGoal. Constructors
        // are bypassed during load, so mode zero retains the legacy contract.
        public enum Phase { WalkToItem, Pickup, WalkHome, Done, ReturnToThrower, Drop }
        public Phase CurrentPhase;
        public int WalkAttempts;
        public int ProgressVersion = 1;
        public bool ReturnsToThrower;
        public Entity Thrower;
        public bool RequireAlliedThrower;
        public int ExpectedQuantity;
        public int ApproachActions;
        public int ReturnActions;
        public int BlockedActions;
        public int DropAttempts;
        public string FallbackReason;

        public const int MaxReturnActions = 100;
        public const int MaxBlockedActions = 3;
        public const int MaxDropAttempts = 2;
        public const int MaxRetrievalAge = 400;

        internal static GoFetchGoal ForThrow(Entity item, Entity thrower, bool alliesOnly)
        {
            return new GoFetchGoal(item)
            {
                ReturnsToThrower = true,
                Thrower = thrower,
                RequireAlliedThrower = alliesOnly,
                ExpectedQuantity = item.GetPart<StackerPart>()?.StackCount ?? 1
            };
        }

        public GoFetchGoal(Entity item, bool returnHome = false)
        {
            Item = item;
            ReturnHome = returnHome;
        }

        public override bool Finished() => CurrentPhase == Phase.Done;

        public override string GetDetails()
        {
            string itemName = Item?.GetDisplayName() ?? "null";
            string details = $"phase={CurrentPhase} | attempts={WalkAttempts}/{MaxWalkAttempts} | item={itemName}";
            return ReturnsToThrower ? details + $" | return={ReturnActions}/{MaxReturnActions} | thrower={Thrower?.ID ?? "null"}" : details;
        }

        public override void TakeAction()
        {
            if (ReturnsToThrower)
            {
                Retrieve();
                return;
            }
            if (Item == null || CurrentZone == null || CurrentPhase < Phase.WalkToItem || CurrentPhase > Phase.Done)
            { Pop(); return; }
            if (ProgressVersion == 0)
            {
                // An old home fetch may already carry its item, but old one-way
                // goals have no saved thrower. Never invent one during migration.
                ProgressVersion = 1;
                if (ReturnHome && ParentEntity.GetPart<InventoryPart>()?.Contains(Item) == true)
                {
                    CurrentPhase = Phase.WalkHome;
                    if (ParentBrain.HasStartingCell)
                        PushChildGoal(new MoveToGoal(ParentBrain.StartingCellX, ParentBrain.StartingCellY, 200));
                    else CurrentPhase = Phase.Done;
                    return;
                }
            }

            switch (CurrentPhase)
            {
                case Phase.WalkToItem:
                    WalkToItem();
                    break;
                case Phase.Pickup:
                    DoPickup();
                    break;
                case Phase.WalkHome:
                    // Child MoveToGoal home has popped; we're done regardless of whether
                    // it arrived (home return is best-effort, pickup already succeeded).
                    CurrentPhase = Phase.Done;
                    break;
            }
        }

        private void WalkToItem()
        {
            var itemCell = CurrentZone.GetEntityCell(Item);
            if (itemCell == null) { Pop(); return; } // item no longer in zone

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            CurrentPhase = Phase.Pickup;

            if (myPos.x == itemCell.X && myPos.y == itemCell.Y)
            {
                // Already on the item's cell — skip the walk, go straight to pickup.
                DoPickup();
                return;
            }

            // Cap re-push attempts. MoveToGoal's timeout (Age > MaxTurns) pops silently
            // without calling FailToParent, so Failed() can't catch that case. Without
            // this cap, an unreachable item would cause GoFetchGoal to oscillate
            // WalkToItem ↔ Pickup forever.
            if (WalkAttempts >= MaxWalkAttempts)
            {
                Think("giving up on fetch — max walk attempts");
                Pop();
                return;
            }

            WalkAttempts++;
            Think($"walking to {Item?.GetDisplayName() ?? "item"} at ({itemCell.X},{itemCell.Y})");
            PushChildGoal(new MoveToGoal(itemCell.X, itemCell.Y, WalkStepBudget));
        }

        private void DoPickup()
        {
            var itemCell = CurrentZone.GetEntityCell(Item);
            if (itemCell == null) { Pop(); return; }

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            // Allow pickup from adjacent or same cell (the item cell may be semi-solid for some blueprints).
            if (!AIHelpers.IsAdjacent(myPos.x, myPos.y, itemCell.X, itemCell.Y)
                && !(myPos.x == itemCell.X && myPos.y == itemCell.Y))
            {
                // MoveToGoal pushed us close but not close enough (timed out?).
                // Retry via WalkToItem, bounded by WalkAttempts counter.
                CurrentPhase = Phase.WalkToItem;
                return;
            }

            bool ok = InventorySystem.Pickup(ParentEntity, Item, CurrentZone);
            if (!ok) { Pop(); return; }

            if (ReturnHome && ParentBrain != null && ParentBrain.HasStartingCell)
            {
                CurrentPhase = Phase.WalkHome;
                PushChildGoal(new MoveToGoal(
                    ParentBrain.StartingCellX,
                    ParentBrain.StartingCellY,
                    200));
            }
            else
            {
                CurrentPhase = Phase.Done;
            }
        }

        internal static bool LiveMember(Entity actor, Zone zone)
        {
            if (actor == null || zone == null || actor.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(actor) || actor.SpatialZone != zone) return false;
            var cell = zone.GetEntityCell(actor);
            return cell != null && cell.Objects.Contains(actor)
                && actor.GetPart<PhysicsPart>()?.ParentEntity == actor;
        }

        internal static bool GroundItem(Entity item, Zone zone, Cell expectedCell, int quantity)
        {
            if (item == null || zone == null || quantity <= 0 || item.SpatialZone != zone) return false;
            var physics = item.GetPart<PhysicsPart>();
            var stacker = item.GetPart<StackerPart>();
            var handling = item.GetPart<HandlingPart>();
            var cell = zone.GetEntityCell(item);
            return cell != null && cell.Objects.Contains(item) && (expectedCell == null || cell == expectedCell)
                && physics?.ParentEntity == item && physics.Takeable
                && physics.InInventory == null && physics.Equipped == null
                && (stacker == null || stacker.ParentEntity == item) && (stacker?.StackCount ?? 1) == quantity
                && (handling == null || (handling.ParentEntity == item && handling.Carryable));
        }

        private bool RecipientCurrent()
        {
            return Thrower != ParentEntity && Thrower != Item && LiveMember(Thrower, CurrentZone)
                && (!RequireAlliedThrower || FactionManager.IsAllied(ParentEntity, Thrower));
        }

        private bool CarriesPayload()
        {
            var inventory = ParentEntity?.GetPart<InventoryPart>();
            var physics = Item?.GetPart<PhysicsPart>();
            var stacker = Item?.GetPart<StackerPart>();
            return inventory?.ParentEntity == ParentEntity && Item != null && Item.SpatialZone == null
                && inventory.Objects.Contains(Item) && !inventory.EquippedItems.ContainsValue(Item)
                && physics?.ParentEntity == Item && physics.InInventory == ParentEntity && physics.Equipped == null
                && (stacker == null || stacker.ParentEntity == Item) && (stacker?.StackCount ?? 1) == ExpectedQuantity;
        }

        private void Retrieve()
        {
            if (!LiveMember(ParentEntity, CurrentZone) || ParentBrain.ParentEntity != ParentEntity
                || ParentEntity.GetPart<BrainPart>() != ParentBrain
                || ParentEntity.GetPart<InventoryPart>()?.ParentEntity != ParentEntity)
            { Finish("actor_unavailable"); return; }
            if (ProgressVersion != 1 || ExpectedQuantity <= 0 || CurrentPhase < Phase.WalkToItem || CurrentPhase > Phase.Drop
                || ApproachActions < 0 || ReturnActions < 0 || DropAttempts < 0 || BlockedActions < 0)
            { Finish("invalid_progress"); return; }
            if (CurrentPhase == Phase.Done) return;
            if (CurrentPhase == Phase.WalkToItem || CurrentPhase == Phase.Pickup)
            {
                if (!RecipientCurrent() || !GroundItem(Item, CurrentZone, null, ExpectedQuantity))
                { Finish("source_or_recipient_changed"); return; }
                if (Age > MaxRetrievalAge || ApproachActions >= MaxWalkAttempts * WalkStepBudget)
                { Finish("approach_budget"); return; }
                ApproachActions++;
                if (SpatialQuery.Distance(CurrentZone, ParentEntity, Item) <= 1)
                {
                    CurrentPhase = Phase.Pickup;
                    bool picked = InventorySystem.ExecuteCommand(PickupCommand.ForRetrieval(Item, ExpectedQuantity, Thrower, RequireAlliedThrower), ParentEntity, CurrentZone).Success;
                    if (!picked || !CarriesPayload()) { Finish("pickup_refused_or_changed"); return; }
                    CurrentPhase = Phase.ReturnToThrower;
                    BlockedActions = 0;
                    Record("acquired");
                    return;
                }
                if (StepToContact(Item)) BlockedActions = 0;
                else if (++BlockedActions >= MaxBlockedActions) Finish("approach_blocked");
                return;
            }
            if (!CarriesPayload()) { Finish("payload_changed"); return; }
            // A vetoed drop may be retried on a later turn. The recipient can
            // move or disappear meanwhile, just as during the return walk.
            if (CurrentPhase == Phase.Drop && FallbackReason == null)
            {
                if (!RecipientCurrent()) BeginFallback("recipient_unavailable");
                else if (SpatialQuery.Distance(CurrentZone, ParentEntity, Thrower) > 1)
                    CurrentPhase = Phase.ReturnToThrower;
            }
            if (CurrentPhase == Phase.ReturnToThrower)
            {
                if (!RecipientCurrent()) BeginFallback("recipient_unavailable");
                else if (Age > MaxRetrievalAge || ReturnActions >= MaxReturnActions) BeginFallback("return_budget");
                else
                {
                    ReturnActions++;
                    if (SpatialQuery.Distance(CurrentZone, ParentEntity, Thrower) <= 1) CurrentPhase = Phase.Drop;
                    else
                    {
                        if (StepToContact(Thrower)) BlockedActions = 0;
                        else if (++BlockedActions >= MaxBlockedActions) BeginFallback("return_blocked");
                        return;
                    }
                }
            }
            if (CurrentPhase != Phase.Drop) { Finish("invalid_retrieval_phase"); return; }
            if (DropAttempts >= MaxDropAttempts) { Finish("drop_refused"); return; }
            DropAttempts++;
            var cell = CurrentZone.GetEntityCell(ParentEntity);
            bool dropped = InventorySystem.Drop(ParentEntity, Item, CurrentZone);
            if (dropped)
            {
                bool exact = GroundItem(Item, CurrentZone, cell, ExpectedQuantity);
                Finish(exact ? "dropped" : "drop_changed");
            }
            else if (!CarriesPayload() || DropAttempts >= MaxDropAttempts) Finish("drop_refused_or_changed");
            else Record("drop_retry");
        }

        private bool StepToContact(Entity target)
        {
            var from = CurrentZone.GetEntityCell(ParentEntity);
            var to = CurrentZone.GetEntityCell(target);
            if (from == null || to == null) return false;
            var path = FindPath.Search(CurrentZone, from.X, from.Y, to.X, to.Y,
                actor: ParentEntity, contactTarget: target);
            if (!path.Usable || path.Steps.Count == 0) return false;
            var step = path.Steps[0];
            var result = MovementSystem.TryMoveDetailed(ParentEntity, CurrentZone, step.dx, step.dy);
            if (result.Moved || result.ActionPerformed) Record("approach_action");
            return result.Moved || result.ActionPerformed;
        }

        private void BeginFallback(string reason)
        {
            FallbackReason = reason;
            CurrentPhase = Phase.Drop;
            Record("local_fallback");
        }

        private void Finish(string reason)
        {
            Record(reason);
            CurrentPhase = Phase.Done;
            Pop();
        }

        private void Record(string reason)
        {
            if (!Diag.IsChannelEnabled("ai")) return;
            Diag.Record("ai", "FetchProgress", ParentEntity, Item, payload: new
            {
                reason, thrower = Thrower?.ID, phase = CurrentPhase.ToString(), quantity = ExpectedQuantity,
                approachActions = ApproachActions, returnActions = ReturnActions, dropAttempts = DropAttempts,
                fallbackReason = FallbackReason
            });
        }

        public override void Failed(GoalHandler child)
        {
            // MoveToGoal said "unreachable" via FailToParent. Regardless of phase,
            // the goal is over — either we couldn't walk to the item or we couldn't
            // walk home. Fail up to our own parent (usually BoredGoal).
            FailToParent();
        }
    }
}
