namespace CavesOfOoo.Core
{
    /// <summary>
    /// Two-phase carry-and-deposit state machine:
    /// <list type="number">
    ///   <item><b>Fetch</b> — walk to the corpse, pick it up.</item>
    ///   <item><b>Haul</b> — walk to the container, deposit.</item>
    /// </list>
    /// Ports Qud's <c>XRL.World.AI.GoalHandlers.DisposeOfCorpse</c>
    /// (DisposeOfCorpse.cs lines 6-90). Each phase has a try-counter capped
    /// at 10 to bound retries when pathing is unreliable (broken path, NPC
    /// displaced, etc).
    ///
    /// <para><b>Entry invariant.</b> Constructed by a behavior part
    /// (<c>AIUndertakerPart</c> in M5.3) that has already claimed the
    /// corpse via <c>Corpse.SetIntProperty("DepositCorpsesReserve", N)</c>.
    /// <see cref="OnPop"/> clears the reservation unconditionally so the
    /// corpse becomes available to other undertakers when this goal
    /// terminates (success, failure, or give-up).</para>
    ///
    /// <para><b>Carry-phase fallback.</b> When the container fills up or the
    /// NPC exhausts <see cref="MaxMoveTries"/> trying to reach it,
    /// the corpse is dropped at the NPC's feet
    /// (<c>InventorySystem.Drop</c>) — mirrors Qud's
    /// <c>FireEvent("PerformDrop")</c> fallback (DisposeOfCorpse.cs line 64).</para>
    ///
    /// <para><b>Goal-stack shape.</b> When a child <see cref="MoveToGoal"/>
    /// completes (reached target OR timed out on <c>MaxTurns</c>), this goal
    /// re-evaluates on the next tick. If the MoveToGoal explicitly fails
    /// (both A* and greedy step fail), <see cref="Failed"/> propagates
    /// via <see cref="FailToParent"/> — matching Qud's implicit behaviour
    /// (Qud's <c>DisposeOfCorpse</c> has no <c>Failed</c> override either;
    /// an unreachable target just exhausts the retry counter).</para>
    /// </summary>
    public class DisposeOfCorpseGoal : GoalHandler
    {
        /// <summary>Maximum times we'll re-push a MoveToGoal child before giving up.
        /// Matches Qud's hardcoded 10 in DisposeOfCorpse.cs lines 58 / 81.</summary>
        public const int MaxMoveTries = 10;

        /// <summary>Per-leg MaxTurns budget passed to child MoveToGoals. Tight enough
        /// that an unreachable target caps quickly (20 × 10 tries = 200 ticks worst case),
        /// loose enough that a winding route through doors completes.</summary>
        public const int ChildMoveMaxTurns = 20;

        /// <summary>The corpse to pick up and deposit.</summary>
        public Entity Corpse;

        /// <summary>The container (typically a Graveyard) to deposit into.</summary>
        public Entity Container;

        /// <summary>Incremented each time a fetch-phase MoveToGoal is pushed.</summary>
        public int GoToCorpseTries;

        /// <summary>Incremented each time a haul-phase MoveToGoal is pushed.</summary>
        public int GoToContainerTries;

        private bool _done;

        public DisposeOfCorpseGoal(Entity corpse, Entity container)
        {
            Corpse = corpse;
            Container = container;
        }

        public override bool Finished() => _done;

        // CanFight defaults to true — undertakers should still defend themselves
        // when a hostile appears while hauling. Matches Qud DisposeOfCorpse.cs line 33.
        public override bool CanFight() => true;

        public override string GetDetails()
        {
            string phase;
            var inv = ParentEntity?.GetPart<InventoryPart>();
            if (inv != null && Corpse != null && inv.Contains(Corpse))
                phase = "hauling";
            else
                phase = "fetching";
            return $"phase={phase} | fetchTries={GoToCorpseTries}/{MaxMoveTries} | haulTries={GoToContainerTries}/{MaxMoveTries}";
        }

        public override void TakeAction()
        {
            // --- Validation ---
            var zone = CurrentZone;
            var actor = ParentEntity;
            if (zone == null || actor == null) { FailToParent(); return; }
            if (Corpse == null || Container == null) { FailToParent(); return; }

            // Container must still be in this zone.
            if (zone.GetEntityCell(Container) == null) { FailToParent(); return; }

            var actorPos = zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { FailToParent(); return; }

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) { FailToParent(); return; }

            bool carrying = inventory.Contains(Corpse);

            if (carrying)
                HaulPhase(actor, inventory, zone, actorPos);
            else
                FetchPhase(actor, zone, actorPos);
        }

        // --- Phase 1: NOT carrying — walk to corpse and pick up. ---
        private void FetchPhase(Entity actor, Zone zone, (int x, int y) actorPos)
        {
            var corpseCell = zone.GetEntityCell(Corpse);
            if (corpseCell == null)
            {
                // Corpse removed from zone (destroyed, teleported, eaten). Abort —
                // the behavior part will find a new corpse on the next bored tick.
                FailToParent();
                return;
            }

            bool onOrAdjacent =
                (actorPos.x == corpseCell.X && actorPos.y == corpseCell.Y) ||
                AIHelpers.IsAdjacent(actorPos.x, actorPos.y, corpseCell.X, corpseCell.Y);

            if (onOrAdjacent)
            {
                // Pickup through the unified command pipeline (validates Takeable,
                // Carryable, Strength, fires Before/AfterPickup, handles zone removal).
                if (!InventorySystem.Pickup(actor, Corpse, zone))
                {
                    // Validation failed (too heavy, not takeable, cancelled). Mirrors
                    // Qud ReceiveObject fallthrough at DisposeOfCorpse.cs line 78.
                    FailToParent();
                    return;
                }
                // Do NOT set Done — next tick the "carrying" branch runs.
                return;
            }

            // Not adjacent — move closer, up to MaxMoveTries times.
            if (++GoToCorpseTries <= MaxMoveTries)
            {
                Think("fetching corpse");
                PushChildGoal(new MoveToGoal(corpseCell.X, corpseCell.Y, ChildMoveMaxTurns));
                return;
            }

            // Exhausted fetch retries — give up quietly. Matches Qud DisposeOfCorpse.cs
            // line 87: Done = true (NOT FailToParent — there's no parent fallback).
            _done = true;
        }

        // --- Phase 2: carrying — walk to container and deposit. ---
        private void HaulPhase(Entity actor, InventoryPart inventory, Zone zone, (int x, int y) actorPos)
        {
            var containerCell = zone.GetEntityCell(Container);
            // Container null-checked in TakeAction; containerCell can only be null if
            // the container was removed between the TakeAction guard and here — defensive.
            if (containerCell == null) { FailToParent(); return; }

            bool adjacent = AIHelpers.IsAdjacent(actorPos.x, actorPos.y, containerCell.X, containerCell.Y)
                || (actorPos.x == containerCell.X && actorPos.y == containerCell.Y);

            if (adjacent)
            {
                DepositOrDropAtFeet(actor, inventory, zone, actorPos);
                _done = true;
                return;
            }

            if (++GoToContainerTries <= MaxMoveTries)
            {
                Think("hauling corpse");
                PushChildGoal(new MoveToGoal(containerCell.X, containerCell.Y, ChildMoveMaxTurns));
                return;
            }

            // Exhausted haul retries — drop the corpse at NPC's feet so it isn't
            // lost forever in inventory. Mirrors Qud DisposeOfCorpse.cs line 64.
            InventorySystem.Drop(actor, Corpse, zone);
            _done = true;
        }

        /// <summary>
        /// Transfer corpse from NPC inventory to the container's ContainerPart.
        /// If the container is full or missing ContainerPart, drop at NPC's feet.
        /// </summary>
        private void DepositOrDropAtFeet(Entity actor, InventoryPart inventory, Zone zone, (int x, int y) actorPos)
        {
            var containerPart = Container.GetPart<ContainerPart>();
            if (containerPart == null)
            {
                // Container blueprint misconfigured — don't lose the corpse,
                // drop at feet so it can be re-claimed.
                InventorySystem.Drop(actor, Corpse, zone);
                return;
            }

            // Remove from inventory first, then try to add to container.
            // If the add fails (full), manually place the corpse on the actor's
            // cell — we can't re-enter inventory cleanly because AddObject
            // would re-set the InInventory back-reference we just cleared.
            inventory.RemoveObject(Corpse);
            if (!containerPart.AddItem(Corpse))
            {
                zone.AddEntity(Corpse, actorPos.x, actorPos.y);
            }
        }

        public override void Failed(GoalHandler child)
        {
            // Child MoveToGoal hit an explicit failure path (A* and greedy both
            // failed). Propagate — retrying is futile when even one-step greedy
            // movement can't make progress. Matches the behavior an absent
            // Failed() override would produce IF the parent had no other work,
            // but explicit is better than implicit here.
            FailToParent();
        }

        public override void OnPop()
        {
            // Release the corpse reservation so other undertakers can re-attempt
            // on a future bored tick. If the corpse was destroyed mid-haul
            // (or never existed), the property removal is a safe no-op.
            Corpse?.RemoveIntProperty("DepositCorpsesReserve");

            // Terminal thought — mirrors M4 MoveToInteriorGoal.OnPop pattern
            // (clear the sticky "hauling corpse" / "fetching corpse" LastThought
            // so the Phase 10 inspector reflects goal completion).
            Think(_done ? "buried" : null);
        }
    }
}
