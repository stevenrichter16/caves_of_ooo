namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that walks an NPC to an item on the floor, picks it up, and optionally returns home.
    /// Mirrors Qud's GoFetch goal handler.
    ///
    /// Use cases:
    /// - Pet dog fetches a thrown bone
    /// - Villager retrieves dropped tool
    /// - Hoarder creature grabs shiny loot they spotted
    /// - Quest NPC picks up a specific item from the floor
    ///
    /// Execution sequence:
    /// 1. Locate the item's current cell (it may have moved if another entity picked it up)
    /// 2. Push MoveToGoal toward the item cell
    /// 3. On arrival, call InventorySystem.Pickup
    /// 4. If ReturnHome and StartingCell is set, push MoveToGoal back home
    /// 5. Pop
    ///
    /// Failure modes (goal pops without picking up):
    /// - Item no longer exists in the zone (picked up by another, destroyed)
    /// - Item cell unreachable via A* (Failed() called with FailToParent)
    /// - Item cell reachable but MoveToGoal timed out before arriving
    ///   (capped by <see cref="MaxWalkAttempts"/> to prevent infinite loop)
    /// - NPC has no InventoryPart
    /// - Pickup fails validation (carry weight, etc.)
    /// </summary>
    public class GoFetchGoal : GoalHandler
    {
        /// <summary>Max MoveToGoal pushes to reach the item before giving up.</summary>
        public const int MaxWalkAttempts = 2;

        /// <summary>Ticks each inner MoveToGoal is allowed before it times out.</summary>
        public const int WalkStepBudget = 100;

        public Entity Item;
        public bool ReturnHome;

        private enum Phase { WalkToItem, Pickup, WalkHome, Done }
        private Phase _phase = Phase.WalkToItem;
        private int _walkAttempts;

        public GoFetchGoal(Entity item, bool returnHome = false)
        {
            Item = item;
            ReturnHome = returnHome;
        }

        public override bool Finished() => _phase == Phase.Done;

        public override string GetDetails()
        {
            string itemName = Item?.GetDisplayName() ?? "null";
            return $"phase={_phase} | attempts={_walkAttempts}/{MaxWalkAttempts} | item={itemName}";
        }

        public override void TakeAction()
        {
            if (Item == null || CurrentZone == null) { Pop(); return; }

            switch (_phase)
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
                    _phase = Phase.Done;
                    break;
            }
        }

        private void WalkToItem()
        {
            var itemCell = CurrentZone.GetEntityCell(Item);
            if (itemCell == null) { Pop(); return; } // item no longer in zone

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            _phase = Phase.Pickup;

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
            if (_walkAttempts >= MaxWalkAttempts)
            {
                Think("giving up on fetch — max walk attempts");
                Pop();
                return;
            }

            _walkAttempts++;
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
                // Retry via WalkToItem, bounded by _walkAttempts counter.
                _phase = Phase.WalkToItem;
                return;
            }

            bool ok = InventorySystem.Pickup(ParentEntity, Item, CurrentZone);
            if (!ok) { Pop(); return; }

            if (ReturnHome && ParentBrain != null && ParentBrain.HasStartingCell)
            {
                _phase = Phase.WalkHome;
                PushChildGoal(new MoveToGoal(
                    ParentBrain.StartingCellX,
                    ParentBrain.StartingCellY,
                    200));
            }
            else
            {
                _phase = Phase.Done;
            }
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
