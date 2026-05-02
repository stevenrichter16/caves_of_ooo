namespace CavesOfOoo.Core
{
    /// <summary>
    /// A lock on a door, chest, or other openable furniture.
    ///
    /// LK.2 (data shape) — fields only. The bump-to-unlock event handler
    /// lands in LK.3 (Docs/LOCK-AND-KEY.md). Tests in LK.2 pin the
    /// default-constructed shape so future commits don't silently
    /// regress field defaults.
    ///
    /// <para>Contract:
    ///   • <see cref="KeyId"/> identifies which key opens this lock.
    ///     Empty string = no requirement (locked-as-decoration; bump
    ///     unlocks it without consulting inventory).
    ///   • <see cref="IsLocked"/> tracks runtime state. Starts true on
    ///     freshly-built furniture; flips to false once unlocked. The
    ///     PhysicsPart bump hook (LK.3) reads this to decide whether
    ///     to fire AttemptUnlock or pass-through.
    /// </para>
    ///
    /// <para>v1 keys are reusable (master-key model — using a key
    /// doesn't consume it). If single-use is wanted later, add a
    /// <c>Consumable</c> flag on <see cref="KeyPart"/> and have the
    /// LK.3 hook strip it from inventory after a successful unlock.
    /// Documented in Docs/LOCK-AND-KEY.md self-review.</para>
    /// </summary>
    public sealed class LockPart : Part
    {
        public override string Name => "Lock";

        /// <summary>
        /// Identifier of the matching <see cref="KeyPart.KeyId"/>.
        /// Empty string = no requirement (any unlock attempt succeeds).
        /// </summary>
        public string KeyId = "";

        /// <summary>
        /// Runtime state. True = locked, refuses move-through unless
        /// AttemptUnlock event finds a matching key in the actor's
        /// inventory. False = unlocked, no further interaction needed.
        /// </summary>
        public bool IsLocked = true;
    }
}
