using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    public sealed class WeaponSalvagePart : Part, IInventoryActionReceiptOwner
    {
        const string SalvageCommand = "SalvageForgedWeapon";
        const string CutPrefix = "CutBindingForHead|";
        FieldQuote fieldQuote;

        public override string Name => "WeaponSalvage";
        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<WeaponSalvagePart>() == this
            && command?.StartsWith(CutPrefix, StringComparison.Ordinal) == true;

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var item = ParentEntity; var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                AddForgeRecoveryRow(actor, zone, actions);
                fieldQuote = actions == null ? null : FieldQuote.Create(actor, item, zone);
                if (fieldQuote != null)
                    actions.AddActionWithHint("FieldHeadRecovery", "cut for head (lose haft, binding, mods)", fieldQuote.Command,
                        "One action. Lose the weapon, haft, binding and improvements. Keep your cutter. A working forge can also recover the haft.",
                        '\0', 16, false, ActionTime.OneAction);
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (command != SalvageCommand && !OwnsInventoryReceipt(command)) return true;
            if (e.GetParameter<bool>("PreparationAttempted")) return true; e.SetParameter("PreparationAttempted", true);
            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (command == SalvageCommand)
            {
                if (!Salvage(actor, zone, transaction)) return true;
            }
            else
            {
                var quote = fieldQuote;
                if (quote?.Command != command) return true;
                fieldQuote = null;
                if (!quote.TryRecover(actor, zone, transaction))
                {
                    PreparationActions.Reject(actor, item, command, "field_recovery_changed");
                    return true;
                }
            }
            e.Handled = true; return false;
        }

        void AddForgeRecoveryRow(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !PreparationActions.CurrentActor(actor, zone)
                || !PreparationActions.Carried(actor, ParentEntity) || !ParentEntity.HasPart<WeaponAssemblyPart>()) return;

            const string label = "forge: head+haft; lose binding/mods";
            var station = ForgeWorkReceipt.Resolve(actor, zone);
            if (ForgeWorkReceipt.Near(actor, station, zone))
            {
                if (StationAccess.Allowed(actor, station, zone, ForgeWorkReceipt.Kind, SalvageCommand, out string code, out string reason))
                    actions.AddAction(SalvageCommand, label, SalvageCommand, '\0', 17);
                else
                    actions.AddDisabled(SalvageCommand, label, code ?? "forge-access",
                        string.IsNullOrWhiteSpace(reason) ? "This forge is unavailable for recovery." : reason, '\0', 5);
                return;
            }

            var nearby = new List<StationCandidate>();
            StationQuery.Collect(actor, zone, ForgeWorkReceipt.Kind, nearby);
            bool visibleFault = nearby.Any(candidate => candidate.BlockCode == StationReadiness.Faulted
                && zone.GetEntityCell(candidate.Station) is { IsVisible: true, Explored: true }
                && candidate.Station.GetPart<RenderPart>() is { Visible: true } render && render.ParentEntity == candidate.Station);
            actions.AddDisabled(SalvageCommand, label, visibleFault ? "forge-needs-repair" : "forge-required",
                visibleFault ? "The nearby forge or work rig needs repair. Repair it or use another working forge."
                    : "Stand beside a working forge or work rig to recover the head and haft.", '\0', 5);
        }

        bool Salvage(Entity actor, Zone zone, InventoryTransaction tx)
        {
            const string command = SalvageCommand;
            var weapon = ParentEntity; var assembly = weapon.GetPart<WeaponAssemblyPart>(); var factory = ForgePart.Factory;
            if (weapon.BlueprintName != WeaponForgingService.ForgedWeaponBlueprintName || assembly == null || factory == null
                || !weapon.HasPart<MeleeWeaponPart>() || !ForgePart.IsNearForge(actor, zone)) return PreparationActions.Reject(actor, weapon, command, "weapon_or_station");
            if (!ForgeWorkReceipt.Begin(actor, zone, tx, command)) return PreparationActions.Reject(actor, weapon, command, "station_changed");
            string blade = assembly.BladeBlueprint, haft = assembly.HaftBlueprint, binding = assembly.BindingBlueprint;
            // Recorded component recipes are provenance, never arbitrary inventory output names.
            if (!ComponentBlueprint(factory, blade, "Blade") || !ComponentBlueprint(factory, haft, "Haft") || !ComponentBlueprint(factory, binding, "Binding"))
                return PreparationActions.Reject(actor, weapon, command, "invalid_assembly");
            return PreparationActions.Transform(actor, weapon, zone, factory, tx, command, 1, new[] { blade, haft },
                () => weapon.GetPart<WeaponSalvagePart>() == this && weapon.GetPart<WeaponAssemblyPart>() == assembly
                    && assembly.BladeBlueprint == blade && assembly.HaftBlueprint == haft && assembly.BindingBlueprint == binding
                    && ForgePart.Factory == factory && ForgePart.IsNearForge(actor, zone),
                (output, index) => output.GetPart<WeaponComponentPart>()?.Slot == (index == 0 ? "Blade" : "Haft"),
                "You reclaim the head and haft. The binding and improvements are spent.");
        }
        static bool ComponentBlueprint(CavesOfOoo.Data.EntityFactory factory, string blueprint, string slot)
        {
            if (string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint)) return false;
            // Final physical products are checked again after all initializers; binding is discarded.
            var objectBlueprint = factory.Blueprints[blueprint];
            return objectBlueprint.Parts.TryGetValue("WeaponComponent", out var part)
                && part.TryGetValue("Slot", out string actualSlot) && actualSlot == slot;
        }

        sealed class FieldQuote
        {
            internal readonly string Command = CutPrefix + Guid.NewGuid().ToString("N");
            readonly Entity actor, weapon;
            readonly Zone zone;
            readonly Cell cell;
            readonly InventoryPart pack;
            readonly EntityFactory factory;
            readonly WeaponAssemblyPart assembly;
            readonly Part[] parts;
            readonly PhysicsPart physics;
            readonly string id, head, haft, binding;
            readonly int weight, hitpoints;
            readonly MaterialCuttingWorkReceipt cutter;
            readonly MaterialProcessTemplate headTemplate, haftTemplate, bindingTemplate;

            FieldQuote(Entity actor, Entity weapon, Zone zone, EntityFactory factory, MaterialCuttingWorkReceipt cutter,
                MaterialProcessTemplate headTemplate, MaterialProcessTemplate haftTemplate, MaterialProcessTemplate bindingTemplate)
            {
                this.actor = actor;
                this.weapon = weapon;
                this.zone = zone;
                this.factory = factory;
                this.cutter = cutter;
                this.headTemplate = headTemplate;
                this.haftTemplate = haftTemplate;
                this.bindingTemplate = bindingTemplate;
                cell = zone.GetEntityCell(actor);
                pack = actor.GetPart<InventoryPart>();
                assembly = weapon.GetPart<WeaponAssemblyPart>();
                head = assembly.BladeBlueprint;
                haft = assembly.HaftBlueprint;
                binding = assembly.BindingBlueprint;
                parts = weapon.Parts.ToArray();
                physics = weapon.GetPart<PhysicsPart>();
                id = weapon.ID;
                weight = physics.Weight;
                hitpoints = weapon.GetStatValue("Hitpoints");
            }

            internal static FieldQuote Create(Entity actor, Entity weapon, Zone zone)
            {
                if (!PreparationActions.CurrentActor(actor, zone) || !PreparationActions.Carried(actor, weapon)
                    || weapon.BlueprintName != WeaponForgingService.ForgedWeaponBlueprintName || string.IsNullOrEmpty(weapon.ID)
                    || weapon.GetPart<WeaponAssemblyPart>() is not WeaponAssemblyPart assembly
                    || assembly.BindingBlueprint is not ("LeatherBindingComponent" or "PlainCordBindingComponent")
                    || ForgePart.Factory is not EntityFactory factory
                    || !factory.Blueprints.TryGetValue(weapon.BlueprintName, out var blueprint)
                    || !ComponentBlueprint(factory, assembly.BladeBlueprint, "Blade")
                    || !ComponentBlueprint(factory, assembly.HaftBlueprint, "Haft")
                    || !ComponentBlueprint(factory, assembly.BindingBlueprint, "Binding")) return null;

                // Permit normal forged Parts, their assembly record, item enhancements
                // and inert bookkeeping. Reject other installed payload before discarding it.
                if (weapon.Parts.Count(p => p is WeaponAssemblyPart) != 1 || !weapon.HasPart<MeleeWeaponPart>()
                    || weapon.Parts.Any(p => p.ParentEntity != weapon || !(p is WeaponAssemblyPart || p is IItemEnhancement
                        || MaterialProcessPayloadPolicies.IsInertTracking(p)
                        || blueprint.Parts.ContainsKey(p.Name) && factory.TryGetRegisteredPartType(p.Name, out var type) && p.GetType() == type))
                    || weapon.Parts.GroupBy(p => p.Name).Any(group => group.Count() > 1)) return null;

                var cutter = MaterialCuttingWorkReceipt.Resolve(actor, weapon, zone);
                var head = MaterialProcessTemplate.Capture(factory, assembly.BladeBlueprint, requireCreationSafe: true);
                var haft = MaterialProcessTemplate.Capture(factory, assembly.HaftBlueprint);
                var binding = MaterialProcessTemplate.Capture(factory, assembly.BindingBlueprint);
                if (cutter == null || head == null || haft == null || binding == null) return null;
                var quote = new FieldQuote(actor, weapon, zone, factory, cutter, head, haft, binding);
                return quote.Current() ? quote : null;
            }

            static int Count(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;

            bool SameIdentity() => weapon.ID == id && weapon.BlueprintName == WeaponForgingService.ForgedWeaponBlueprintName;

            bool SameWeapon() => SameIdentity()
                && weapon.Parts.SequenceEqual(parts) && parts.All(p => p.ParentEntity == weapon)
                && weapon.GetPart<PhysicsPart>() == physics && physics.Weight == weight && Count(weapon) == 1
                && hitpoints > 0 && weapon.GetStatValue("Hitpoints") == hitpoints
                && parts.OfType<StatusEffectsPart>().All(p => p.EffectCount == 0)
                && assembly.BladeBlueprint == head && assembly.HaftBlueprint == haft && assembly.BindingBlueprint == binding;

            bool Context() => PreparationActions.CurrentActor(actor, zone) && zone.GetEntityCell(actor) == cell
                && actor.GetPart<InventoryPart>() == pack && ForgePart.Factory == factory && cutter.Current()
                && headTemplate.Current() && haftTemplate.Current() && bindingTemplate.Current();

            bool Current() => Context() && SameWeapon() && PreparationActions.Carried(actor, weapon)
                && pack.Objects.Count(e => e.ID == id) == 1 && !pack.EquippedItems.Values.Any(e => e.ID == id)
                && !zone.GetReadOnlyEntities().Any(e => e.ID == id);

            internal bool TryRecover(Entity user, Zone currentZone, InventoryTransaction tx)
            {
                if (user != actor || currentZone != zone || tx == null || !Current()
                    || !tx.TryClaimAction(actor) || !PreparationActions.Begin(actor, weapon, zone, tx, Command)
                    || !cutter.Begin(tx, Command) || !MaterialProcessService.ReserveInput(weapon, tx)) return false;

                var product = factory.CreateEntity(head);
                var productState = headTemplate.Snapshot(product);
                if (!Current() || productState == null || !PreparationActions.Fresh(product, head) || Count(product) != 1
                    || product.ID == actor.ID || product.ID == id || pack.Objects.Concat(pack.EquippedItems.Values)
                        .Concat(zone.GetReadOnlyEntities()).Any(e => e.ID == product.ID)
                    || !tx.TryClaim(product, actor, Command)) return false;

                var residents = pack.Objects.Where(e => e.BlueprintName == head)
                    .ToDictionary(e => e, e => (State: headTemplate.Snapshot(e), Count: Count(e)));
                if (residents.Any(row => row.Value.State == null || !PreparationActions.Carried(actor, row.Key))) return false;

                Entity recipient = null;
                bool RecipientCurrent() => recipient != null && PreparationActions.Carried(actor, recipient)
                    && pack.Objects.Count(e => e.ID == recipient.ID) == 1
                    && (recipient == product ? Count(product) == 1 && productState.Matches()
                        : residents.TryGetValue(recipient, out var before) && Count(recipient) == before.Count + 1
                            && before.State.Matches() && Count(product) == 0);
                bool refund = true;
                if (!SameWeapon() || !MaterialProcessPayment.Spend(actor, weapon, actor, zone, tx, SameIdentity,
                    refundAllowed: () => refund)) return false;

                var transfer = InventoryTransferSnapshot.Capture(pack, product);
                tx.Do(null, () =>
                {
                    if (recipient != null && !RecipientCurrent())
                    {
                        // An independent transfer or change may retain the head. Never refund
                        // its assembly in that case.
                        refund = false;
                        MessageLog.Add("The recovered head could not be taken back. The assembly was spent.");
                        return;
                    }
                    transfer.Restore();
                });
                if (!transfer.Apply(() => pack.AddCraftedUnit(product, out recipient)) || !transfer.ClaimChanges(tx, actor, Command))
                    return false;

                bool Final() => Context() && SameWeapon() && RecipientCurrent()
                    && weapon.SpatialZone == null && physics.InInventory == null && physics.Equipped == null
                    && !pack.Objects.Any(e => e.ID == id) && !pack.EquippedItems.Values.Any(e => e.ID == id)
                    && !zone.GetReadOnlyEntities().Any(e => e.ID == id)
                    && (pack.MaxWeight < 0 || pack.GetCarriedWeight() <= pack.MaxWeight);
                if (!Final()) return false;
                transfer.WatchChangedOwners(tx);
                tx.BeforeCommit(Final);
                PreparationActions.Complete(tx, actor, weapon, Command, "You recover the head. The haft, binding and improvements are lost.");
                return true;
            }
        }
    }
}
