using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Opt-in intervention on four mechanical floor traps. One carried
    /// salvaged timber permanently jams this exact mechanism. The saved owner and
    /// its original trigger stay in place; there is no rearm or recoverable timber.</summary>
    public sealed class TrapJammingPart : Part
    {
        public override string Name => "TrapJamming";
        public const string JamCommand = "JamTrap";
        public const string TimberBlueprint = "SalvagedTimber";
        public bool Jammed;

        /// <summary>Pure shape check. Magical, living, grouped, duplicated or
        /// mismatched mechanisms cannot gain this action through the opt-in alone.</summary>
        public static bool IsSupported(Entity owner)
        {
            if (owner == null) return false;
            int jammers = 0, triggers = 0;
            foreach (var part in owner.Parts)
            {
                if (part is TrapJammingPart)
                {
                    if (part.ParentEntity != owner) return false;
                    jammers++;
                }
                if (!(part is TriggerOnStepPart)) continue;
                if (part.ParentEntity != owner) return false;
                var type = part.GetType();
                bool supported = owner.BlueprintName == "SpikeTrap" && type == typeof(SpikeTrapTriggerPart)
                    || owner.BlueprintName == "FireTrap" && type == typeof(FireTrapTriggerPart)
                    || owner.BlueprintName == "BearTrap" && type == typeof(BearTrapTriggerPart)
                    || owner.BlueprintName == "PressurePlate" && type == typeof(PressurePlateTriggerPart);
                if (!supported) return false;
                triggers++;
            }
            return jammers == 1 && triggers == 1;
        }

        /// <summary>The trigger gate and renderer use the same current-owner state.
        /// Owners without the opt-in, including existing saves, remain unchanged.</summary>
        public static bool IsJammed(Entity owner)
            => IsSupported(owner) && owner.GetPart<TrapJammingPart>().Jammed;

        /// <summary>Read-only menu/hint eligibility. Materials are deliberately
        /// not required to discover the cost. No events or virtual effect callbacks
        /// run here; execution remains authoritative for custom action vetoes.</summary>
        public bool CanOfferJam(Entity actor, Zone zone) => Context(actor, zone) == null;

        public string DescribeJamming()
        {
            if (!IsSupported(ParentEntity) || ParentEntity.GetPart<TrapJammingPart>() != this) return "";
            return Jammed
                ? "A length of salvaged timber wedges the mechanism. It is jammed permanently; stepping here will not fire this trap. The timber cannot be recovered."
                : "The exposed mechanism can be wedged from an adjacent tile: jam it with 1 carried salvaged timber. This takes one action and permanently disables this trap; the timber cannot be recovered.";
        }

        /// <summary>Use the native inventory command receipt. This helper charges
        /// no time; the successful player input branch charges one action.</summary>
        public bool TryJam(Entity actor, Zone zone)
        {
            string reason = Context(actor, zone);
            if (reason != null) return Reject(actor, reason);
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity, JamCommand), actor, zone).Success;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (CanOfferJam(actor, actor?.SpatialZone))
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction("JamTrap", "jam mechanism (1 salvaged timber)", JamCommand, 'j', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != JamCommand) return true;
            var user = e.GetParameter<Entity>("Actor");
            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (transaction == null) { Reject(user, "missing-transaction"); return true; }
            if (!Apply(user, e.GetParameter<Zone>("Zone"), transaction)) return true;
            e.Handled = true;
            return false;
        }

        private string Context(Entity actor, Zone zone)
        {
            var owner = ParentEntity;
            if (actor == null || zone == null || owner == null || owner.GetPart<TrapJammingPart>() != this) return "missing-context";
            if (!IsSupported(owner)) return "unsupported-mechanism";
            if (string.IsNullOrWhiteSpace(actor.ID) || string.IsNullOrWhiteSpace(owner.ID) || actor.ID == owner.ID) return "ambiguous-identity";
            if (Jammed) return "already-jammed";
            if (!actor.HasTag("Player") || actor.GetStatValue("Hitpoints", 0) <= 0 || CombatSystem.IsDeathHandled(actor)
                || KnownActionBlock(actor)) return "actor-unavailable";
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory?.ParentEntity != actor) return "missing-inventory";
            var a = zone.GetEntityCell(actor); var target = zone.GetEntityCell(owner);
            if (actor.SpatialZone != zone || owner.SpatialZone != zone || a?.ParentZone != zone || target?.ParentZone != zone
                || !a.Objects.Contains(actor) || !target.Objects.Contains(owner)) return "not-local";
            var manager = WorldLocationContext.For(zone);
            if (manager != null && (manager.ActiveZone != zone || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || current != zone)) return "stale-zone";
            if (SpatialQuery.Distance(zone, actor, owner) > 1) return "out-of-reach";
            var render = owner.GetPart<RenderPart>();
            if (!target.IsVisible || !target.Explored || render?.ParentEntity != owner || !render.Visible) return "not-visible";
            var physical = owner.GetPart<PhysicsPart>(); var actorPhysics = actor.GetPart<PhysicsPart>();
            if (owner.HasTag("Creature") || physical?.ParentEntity != owner || physical.Takeable || physical.InInventory != null || physical.Equipped != null
                || actorPhysics?.ParentEntity != actor || actorPhysics.InInventory != null || actorPhysics.Equipped != null
                || owner.GetPart<DestructiblePart>() is DestructiblePart structure && (structure.Gone || structure.HP <= 0)) return "invalid-target";
            return null;
        }

        // Matches current built-in action-blocking fields without invoking the
        // arbitrary virtual AllowAction methods from a menu or render query.
        private static bool KnownActionBlock(Entity actor)
        {
            var status = actor.GetPart<StatusEffectsPart>();
            if (status == null) return false;
            if (status.ParentEntity != actor) return true;
            var effects = status.GetAllEffects();
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null || effect.Owner != actor) return true;
                if (effect is StunnedEffect || effect is ParalyzedEffect || effect is HibernatingEffect || effect is AsleepByGasEffect) return true;
                if (effect is FrozenEffect ice && ice.Cold > 0) return true;
                if (effect is LiquidCoveredEffect coat && LiquidRegistry.IsInitialized && LiquidRegistry.Get(coat.LiquidId)?.BlockAction == true) return true;
            }
            return false;
        }

        private static bool CarriedTimber(Entity item, Entity actor, Entity target, InventoryPart inventory)
        {
            var physical = item?.GetPart<PhysicsPart>(); var stack = item?.GetPart<StackerPart>();
            if (item == null || item.BlueprintName != TimberBlueprint || string.IsNullOrWhiteSpace(item.ID)
                || item.ID == actor.ID || item.ID == target.ID || item.HasTag("Creature") || item.SpatialZone != null
                || physical?.ParentEntity != item || !physical.Takeable || physical.InInventory != actor || physical.Equipped != null
                || stack != null && (stack.ParentEntity != item || stack.StackCount < 1)) return false;
            foreach (var equipped in inventory.EquippedItems.Values) if (equipped == item) return false;
            if (inventory.FindEquippedBodyPart(item) != null) return false;
            int references = 0;
            foreach (var candidate in inventory.Objects)
            {
                if (candidate == item) references++;
                else if (candidate != null && candidate.ID == item.ID) return false;
            }
            return references == 1;
        }

        // Save graphs and menu picks address owners by ID. Keep this full-zone
        // uniqueness check on execution only, never in per-frame menu hints.
        private static bool CurrentIdentities(Entity actor, Entity target, Zone zone)
        {
            foreach (var other in zone.GetReadOnlyEntities())
                if (other != null && ((other != actor && other.ID == actor.ID)
                    || (other != target && other.ID == target.ID))) return false;
            return true;
        }

        private bool Apply(Entity actor, Zone zone, InventoryTransaction transaction)
        {
            string reason = Context(actor, zone);
            if (reason != null) return Reject(actor, reason);
            if (!CurrentIdentities(actor, ParentEntity, zone)) return Reject(actor, "ambiguous-identity");
            var owner = ParentEntity;
            var initialCell = zone.GetEntityCell(owner);
            if (!transaction.TryClaim(owner, actor, JamCommand) || !transaction.TryClaim(actor, actor, JamCommand)) return Reject(actor, "owner-busy");
            if (actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, "actor-unavailable");
            // A custom action veto can change world state while it evaluates.
            reason = Context(actor, zone);
            if (reason != null) return Reject(actor, reason);
            if (!CurrentIdentities(actor, owner, zone)) return Reject(actor, "ambiguous-identity");
            var inventory = actor.GetPart<InventoryPart>(); Entity timber = null;
            foreach (var item in inventory.Objects)
                if (CarriedTimber(item, actor, owner, inventory)) { timber = item; break; }
            if (timber == null) return Reject(actor, "missing-material");
            if (!transaction.TryClaim(timber, actor, JamCommand)) return Reject(actor, "material-busy");
            var snapshot = InventoryTransferSnapshot.Capture(inventory);
            transaction.Do(null, snapshot.Restore);
            bool paid = snapshot.Apply(() => CarriedTimber(timber, actor, owner, inventory) && inventory.TryConsumeOne(timber));
            if (!paid || !snapshot.ClaimChanges(transaction, actor, JamCommand) || Context(actor, zone) != null
                || actor.GetPart<InventoryPart>() != inventory || !CurrentIdentities(actor, owner, zone)) return Reject(actor, "state-changed");
            transaction.Do(() => Jammed = true, () => Jammed = false);
            transaction.AfterCommit(() =>
            {
                MessageLog.Add("You wedge salvaged timber into the mechanism. The trap is permanently jammed.");
                Diag.Record("furniture", "TrapJammed", actor, owner,
                    new { blueprint = owner.BlueprintName, material = TimberBlueprint, quantity = 1, zoneId = zone.ZoneID });
                var cell = zone.GetEntityCell(owner);
                if (cell != null) ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, "Trap.Jammed");
                // AfterInventoryAction observers may independently move or replace
                // the owner. The paid receipt remains true, but never animate a
                // different, detached or relocated mechanism as this interaction.
                if (ParentEntity == owner && owner.GetPart<TrapJammingPart>() == this
                    && cell == initialCell && cell != null && cell.IsVisible && cell.Explored
                    && owner.GetPart<RenderPart>() is RenderPart render && render.ParentEntity == owner && render.Visible
                    && IsJammed(owner) && (WorldLocationContext.For(zone) is not OverworldZoneManager manager || manager.ActiveZone == zone))
                    EntityVisualHooks.EmitInteraction(actor, owner, zone);
            });
            return true;
        }

        private bool Reject(Entity actor, string reason)
        {
            if (actor?.HasTag("Player") == true)
                MessageLog.Add(reason == "already-jammed" ? "That trap is already jammed."
                    : reason == "missing-material" ? "Jamming this mechanism needs 1 carried salvaged timber."
                    : "You cannot jam that mechanism here.");
            Diag.Record("furniture", "TrapJamRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
