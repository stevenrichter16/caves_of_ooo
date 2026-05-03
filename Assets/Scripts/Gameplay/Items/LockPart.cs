using CavesOfOoo.Diagnostics;

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
            // LK follow-on: surface an "[u]nlock" entry in the look-popup
            // menu when this entity is currently locked. Mirrors the
            // ContainerPart.GetInventoryActions pattern (Items/ContainerPart.cs:98-109).
            // The actual key-check happens at execution time in the
            // InventoryAction handler — entry is visible whenever locked
            // so the player gets feedback ("the X is locked.") even if
            // they don't have the key yet.
            if (e.ID == "GetInventoryActions")
            {
                if (!IsLocked) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                    actions.AddAction("Unlock", "unlock", "Unlock", 'u', 10);
                return true;
            }

            // LK follow-on: handle the Unlock command fired when the
            // player picks the entry from the look popup. Re-uses the
            // existing AttemptUnlock event so bump-unlock and menu-unlock
            // share one code path. Single source of truth.
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "Unlock") return true;

                var menuActor = e.GetParameter<Entity>("Actor");
                var attempt = GameEvent.New("AttemptUnlock");
                attempt.SetParameter("Actor", (object)menuActor);
                ParentEntity.FireEventAndRelease(attempt);

                // On successful unlock the parent's Solid flag (if any)
                // drops so the actor can walk through next turn —
                // matches the bump-unlock path in PhysicsPart.HandleBeforeMove.
                if (!IsLocked && ParentEntity != null)
                {
                    var physics = ParentEntity.GetPart<PhysicsPart>();
                    if (physics != null) physics.Solid = false;
                }

                e.Handled = true;
                return false;
            }

            if (e.ID != "AttemptUnlock") return true;

            var actor = e.GetParameter<Entity>("Actor");
            bool succeeded;
            Entity keyUsed = null;

            if (!IsLocked)
            {
                // Already unlocked — pass-through, no state change.
                succeeded = true;
            }
            else if (string.IsNullOrEmpty(KeyId))
            {
                // Decoration lock — bump auto-opens.
                IsLocked = false;
                succeeded = true;
            }
            else if (actor == null)
            {
                // Can't introspect inventory — refuse without panic.
                succeeded = false;
            }
            else
            {
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
                            keyUsed = item;
                            break;
                        }
                    }
                }
                succeeded = !IsLocked;
            }

            // Surface results on the event + log line + diag record.
            e.SetParameter("Unlocked", succeeded);
            if (keyUsed != null) e.SetParameter("KeyUsed", (object)keyUsed);

            string thing = ParentEntity?.GetDisplayName() ?? "lock";
            if (succeeded && keyUsed != null)
            {
                string actorName = actor?.GetDisplayName() ?? "someone";
                MessageLog.Add($"{actorName} unlocks the {thing}.");
            }
            else if (!succeeded)
            {
                MessageLog.Add($"The {thing} is locked.");
            }

            // LK.4 diag hook: every unlock attempt is recorded so AI
            // debugging can answer "did the player try to unlock X?"
            // and "did the matching key actually fire?". Channel default-
            // on per Diag.DefaultOnCategories.
            if (Diag.IsChannelEnabled("furniture"))
            {
                Diag.Record(
                    category: "furniture",
                    kind: "UnlockAttempted",
                    actor: actor,
                    target: ParentEntity,
                    payload: new
                    {
                        keyId = KeyId,
                        succeeded,
                        keyEntityId = keyUsed?.ID
                    });
            }

            return true;
        }
    }
}
