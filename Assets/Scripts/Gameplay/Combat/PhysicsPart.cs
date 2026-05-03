namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles physical properties: solidity, weight, and movement validation.
    /// Mirrors Qud's Physics part. When an entity with Physics tries to move,
    /// the target cell is checked for solid objects.
    /// </summary>
    public class PhysicsPart : Part
    {
        public override string Name => "Physics";

        /// <summary>
        /// If true, this entity blocks movement into its cell.
        /// </summary>
        public bool Solid = false;

        /// <summary>
        /// Weight in pounds.
        /// </summary>
        public int Weight = 0;

        /// <summary>
        /// If true, this entity can be picked up.
        /// </summary>
        public bool Takeable = false;

        /// <summary>
        /// Inventory display category (e.g. "Melee Weapons", "Armor", "Food").
        /// Matches Qud's Physics.Category field. Used for grouping in inventory UI.
        /// </summary>
        public string Category = "";

        /// <summary>
        /// Back-reference: which entity's inventory this item is in.
        /// Null if on the ground or equipped. Mirrors Qud's Physics.InInventory.
        /// </summary>
        public Entity InInventory = null;

        /// <summary>
        /// Back-reference: which entity this item is equipped on.
        /// Null if in inventory or on the ground. Mirrors Qud's Physics.Equipped.
        /// </summary>
        public Entity Equipped = null;

        public override void Initialize()
        {
            if (ParentEntity != null && ParentEntity.HasTag("Solid"))
                Solid = true;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeMove")
                return HandleBeforeMove(e);
            return true;
        }

        private bool HandleBeforeMove(GameEvent e)
        {
            var targetCell = e.GetParameter<Cell>("TargetCell");
            if (targetCell == null) return true;

            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                var other = targetCell.Objects[i];
                if (other == ParentEntity) continue;

                var otherPhysics = other.GetPart<PhysicsPart>();
                bool isSolid = (otherPhysics != null && otherPhysics.Solid)
                               || other.HasTag("Solid");
                if (!isSolid) continue;

                // LK.3: bump-to-unlock. If the Solid blocker carries a
                // LockPart, fire AttemptUnlock on it before vetoing.
                // The LockPart consults the actor's inventory; on a
                // successful match it flips IsLocked=false. We then
                // also drop the blocker's Solid (so the next move can
                // walk through), but keep the move blocked THIS turn —
                // unlocking is the action; walking through is the
                // next turn's action. This matches roguelike convention
                // and keeps a single bump from skipping past the door.
                var lockPart = other.GetPart<LockPart>();
                if (lockPart != null && lockPart.IsLocked)
                {
                    var actor = e.GetParameter<Entity>("Actor");
                    var attempt = GameEvent.New("AttemptUnlock");
                    attempt.SetParameter("Actor", (object)actor);
                    other.FireEventAndRelease(attempt);
                    // Whether unlocked or not, this turn's move is
                    // blocked (lockPart already updated IsLocked + logged).
                    if (!lockPart.IsLocked)
                    {
                        // Successful unlock — drop Solid so future
                        // bumps walk through. Still block THIS turn
                        // so the player explicitly steps in next.
                        if (otherPhysics != null) otherPhysics.Solid = false;
                    }
                    e.SetParameter("Blocked", true);
                    e.SetParameter("BlockedBy", (object)other);
                    return false;
                }

                e.SetParameter("Blocked", true);
                e.SetParameter("BlockedBy", (object)other);
                return false;
            }
            return true;
        }
    }
}
