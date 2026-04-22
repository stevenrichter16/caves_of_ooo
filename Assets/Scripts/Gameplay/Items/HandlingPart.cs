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
            if (e.ID == "GetInventoryActions" && Throwable)
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                // Priority 15 — above Examine (0), Chat (10), below Open (30).
                // Hotkey 't' doesn't collide with Open/o, Chat/c, Examine/x.
                actions?.AddAction("Throw", "throw", "Throw", 't', 15);
                return true;
            }
            // Throw command is handled by InputHandler.ExecuteWorldActionSelection
            // because it needs UI access to open the throw popup.
            return true;
        }
    }
}
