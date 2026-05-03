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

        /// <summary>
        /// LK.3: handle AttemptUnlock fired by <c>PhysicsPart</c>'s
        /// bump check when the actor walks into a locked entity.
        ///
        /// Reads from event:
        ///   • <c>"Actor"</c> (Entity) — who's bumping. May be null
        ///     defensively; we pass through without unlocking.
        ///
        /// Writes to event:
        ///   • <c>"Unlocked"</c> (bool) — whether this attempt succeeded.
        ///   • <c>"KeyUsed"</c> (Entity) — which inventory item matched
        ///     (only set on success). Caller can read this to support
        ///     consume-on-use keys later.
        ///
        /// Always returns <c>true</c> from HandleEvent — vetoing the
        /// bump itself is the caller's job (PhysicsPart). LockPart
        /// only mutates its <see cref="IsLocked"/> state and reports
        /// the outcome.
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "AttemptUnlock") return true;

            // Already unlocked — pass-through, no further work.
            // Pre-set the "Unlocked" flag so a re-entrant caller sees
            // the existing state without thinking we just opened it
            // (the IsLocked-was-true → false edge fired earlier).
            if (!IsLocked)
            {
                e.SetParameter("Unlocked", true);
                return true;
            }

            // Empty KeyId = decoration lock (no real requirement).
            // Treat any bump as opening it.
            if (string.IsNullOrEmpty(KeyId))
            {
                IsLocked = false;
                e.SetParameter("Unlocked", true);
                return true;
            }

            var actor = e.GetParameter<Entity>("Actor");
            if (actor == null)
            {
                // Can't introspect inventory — refuse without panic.
                e.SetParameter("Unlocked", false);
                return true;
            }

            var inv = actor.GetPart<InventoryPart>();
            if (inv != null && inv.Objects != null)
            {
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    var item = inv.Objects[i];
                    if (item == null) continue;
                    var key = item.GetPart<KeyPart>();
                    if (key != null && key.KeyId == KeyId)
                    {
                        IsLocked = false;
                        e.SetParameter("Unlocked", true);
                        e.SetParameter("KeyUsed", (object)item);
                        var actorName = actor.GetDisplayName();
                        var lockedThing = ParentEntity?.GetDisplayName() ?? "lock";
                        MessageLog.Add($"{actorName} unlocks the {lockedThing}.");
                        return true;
                    }
                }
            }

            // No matching key.
            e.SetParameter("Unlocked", false);
            var thing = ParentEntity?.GetDisplayName() ?? "lock";
            MessageLog.Add($"The {thing} is locked.");
            return true;
        }
    }
}
