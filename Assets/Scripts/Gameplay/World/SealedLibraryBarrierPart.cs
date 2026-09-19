namespace CavesOfOoo.Core
{
    /// <summary>Fixed archive architecture. Walls are always closed; a door
    /// derives closure from its existing lock. No second mutable/saved latch.
    /// This opt-in rule preserves ordinary wall vaulting and furniture rules.</summary>
    public sealed class SealedLibraryBarrierPart : Part
    {
        public override string Name => "SealedLibraryBarrier";
        public bool IsClosed => ParentEntity != null && (ParentEntity.GetPart<LockPart>()?.IsLocked ?? true);

        /// <summary>Stillleaf Archive SA.4: an opened vault door offers to be
        /// resealed; the world action itself validates key, register and place.</summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions" && !IsClosed && ParentEntity.GetPart<LockPart>() != null)
                e.GetParameter<InventoryActionList>("Actions")?.AddAction("StillleafReseal", "reseal the vault (keeper's key)", StillleafCustody.ResealCommand, 'r', 20);
            return true;
        }
    }
}
