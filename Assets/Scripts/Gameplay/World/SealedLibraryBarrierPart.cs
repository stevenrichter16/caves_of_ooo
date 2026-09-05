namespace CavesOfOoo.Core
{
    /// <summary>Fixed archive architecture. Walls are always closed; a door
    /// derives closure from its existing lock. No second mutable/saved latch.
    /// This opt-in rule preserves ordinary wall vaulting and furniture rules.</summary>
    public sealed class SealedLibraryBarrierPart : Part
    {
        public override string Name => "SealedLibraryBarrier";
        public bool IsClosed => ParentEntity != null && (ParentEntity.GetPart<LockPart>()?.IsLocked ?? true);
    }
}
