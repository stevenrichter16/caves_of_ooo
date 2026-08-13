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
            if (e.ID == "GetInventoryActions")
                return HandleGetInventoryActions(e);
            return true;
        }

        /// <summary>
        /// The universal "Take" row. <c>PhysicsPart</c> is on every physical
        /// object (the <c>PhysicalObject</c> base blueprint), so this is
        /// where a takeable item declares itself pick-uppable regardless of
        /// whether it also carries a <c>HandlingPart</c> — most loose items
        /// (Bone, GoldCoin, a dropped sword) do not.
        ///
        /// <para>Before this existed, the world-action menu offered NOTHING
        /// but Examine (+ Throw, if <c>HandlingPart.Throwable</c>) for a
        /// plain item — picking one up was reachable only by standing on it
        /// and pressing the dedicated pickup key (G / ,). The interact key
        /// ('c') can target an ADJACENT cell, which the dedicated key
        /// cannot, so without this row 'c' could show you a sword one tile
        /// away and offer no way to take it.</para>
        ///
        /// <para>Gated on <see cref="Takeable"/> and not already held —
        /// deliberately NOT on Strength. A row that vanishes when you are
        /// overburdened is confusing; <c>PickupCommand</c>'s own strength
        /// check already produces a clear failure message on attempt, which
        /// is how every other pickup path in this game already fails.</para>
        /// </summary>
        private bool HandleGetInventoryActions(GameEvent e)
        {
            if (Takeable && InInventory == null && Equipped == null)
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                // Priority 25 — below Open (30: a container you can open
                // outranks taking it), above Throw (15) and Examine (0):
                // taking is usually the point of interacting with a loose
                // item. Hotkey 'g' matches the standalone pickup key.
                actions?.AddAction("Take", "take", "Take", 'g', 25);
            }
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
