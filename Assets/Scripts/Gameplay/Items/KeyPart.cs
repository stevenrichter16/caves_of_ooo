namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an inventory item as a key for matching <see cref="LockPart"/>s.
    ///
    /// LK.2 (data shape) — pure data. The lookup that fires when the
    /// player bumps a locked door iterates inventory and matches
    /// <see cref="KeyId"/> against <see cref="LockPart.KeyId"/>.
    ///
    /// <para>Contract: keys are master-key in v1 (using a key doesn't
    /// remove it from inventory). Consumable single-use keys are a
    /// later flag — see Docs/LOCK-AND-KEY.md self-review.</para>
    /// </summary>
    public sealed class KeyPart : Part
    {
        public override string Name => "Key";

        /// <summary>
        /// Identifier matched against <see cref="LockPart.KeyId"/>.
        /// Empty string = no-op key (matches no lock by ID).
        /// </summary>
        public string KeyId = "";
    }
}
