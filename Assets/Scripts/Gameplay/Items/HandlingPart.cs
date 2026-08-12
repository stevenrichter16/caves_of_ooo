namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared handling metadata for carried and throwable items.
    /// GripType describes intended hand usage; other fields gate carry/throw behavior.
    ///
    /// Also declares a "Throw" action on the world action menu when the item
    /// is <see cref="Throwable"/>. Selecting Throw routes to the throw popup
    /// (InputHandler.ExecuteWorldActionSelection special-cases the command
    /// because throwing needs a target cell that only comes after aiming).
    /// </summary>
    public sealed class HandlingPart : Part
    {
        public override string Name => "Handling";

        public GripType GripType = GripType.OneHand;
        public bool Carryable = true;
        public bool Throwable = true;
        public int Weight = 0;
        public string BulkClass = "Light";
        public int MinLiftStrength = 0;
        public int MinThrowStrength = 0;
        public int CarryMovePenalty = 0;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (Throwable)
                {
                    // Priority 15 — above Examine (0), Chat (10), below Open (30).
                    // Hotkey 't' doesn't collide with Open/o, Chat/c, Examine/x.
                    actions?.AddAction("Throw", "throw", "Throw", 't', 15);
                }
                AddHaulAction(actions, e.GetParameter<Entity>("Actor"));
                return true;
            }

            if (e.ID == "InventoryAction")
                return HandleHaulCommand(e);

            // Throw command is handled by InputHandler.ExecuteWorldActionSelection
            // because it needs UI access to open the throw popup.
            return true;
        }

        /// <summary>
        /// Offer "haul" on a thing too heavy to carry, or "let go" on the one
        /// currently in tow. Never both — a menu that offers a state and its
        /// opposite at the same time is lying about one of them.
        ///
        /// <para><b>Adjacency is deliberately NOT checked here.</b> The
        /// <c>GetInventoryActions</c> event carries <c>Actions</c> and
        /// <c>Actor</c> but no <c>Zone</c> (<c>WorldInteractionSystem.cs:96-98</c>),
        /// so reach cannot be evaluated at declaration time. Enforcement
        /// lives in <see cref="DragSystem.TryGrab"/>, which has the zone —
        /// the same split <c>SeedPart</c> uses for "is this seed carried?".</para>
        /// </summary>
        private void AddHaulAction(InventoryActionList actions, Entity actor)
        {
            if (actions == null || actor == null || ParentEntity == null) return;

            if (DragSystem.GetDragged(actor) == ParentEntity)
            {
                // Priority 21 — just above Haul's 20, so the verb that ends
                // the current state sits where the eye already is.
                actions.AddAction("LetGo", "let go", ReleaseCommand, 'g', 21);
                return;
            }

            if (DragSystem.IsDragging(actor)) return;   // hands already full
            if (DragRules.CanDrag(actor, ParentEntity) != DragVerdict.Ok) return;

            actions.AddAction("Haul", "haul", HaulCommand, 'g', 20);
        }

        /// <summary>Routing keys for the haul verbs. Public so tests and the
        /// input layer name them once.</summary>
        public const string HaulCommand = "HaulObject";
        public const string ReleaseCommand = "ReleaseHaul";

        private bool HandleHaulCommand(GameEvent e)
        {
            string command = e.GetStringParameter("Command");
            if (command != HaulCommand && command != ReleaseCommand) return true;

            var actor = e.GetParameter<Entity>("Actor");
            if (actor == null) return true;

            if (command == ReleaseCommand)
            {
                DragSystem.Release(actor);
                return true;
            }

            var zone = e.GetParameter<Zone>("Zone");
            DragVerdict verdict = DragSystem.TryGrab(actor, ParentEntity, zone);
            if (verdict != DragVerdict.Ok)
                MessageLog.Add(DragMessages.Refusal(verdict, ParentEntity));
            return true;
        }
    }
}
