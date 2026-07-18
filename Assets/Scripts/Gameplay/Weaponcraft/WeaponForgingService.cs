using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Modular weapon forging (Docs/CRAFTING-ALCHEMY-SYSTEM.md §7.1 L1):
    /// assemble Blade + Haft + Binding components into a weapon whose
    /// MeleeWeaponPart fields are COMPUTED from the components, and re-forge
    /// later to swap one component (the displaced one is returned as a fresh
    /// item, since components are stateless).
    ///
    /// Mirrors BrewingService's shape: validate-first, consume with a
    /// rollback ledger, full "craft" diag coverage (WeaponForged /
    /// WeaponReforged / ForgeRejected). The forge writes ONLY fields the
    /// combat path already reads (CombatSystem.PerformSingleAttack consumes
    /// BaseDamage/PenBonus/HitBonus/MaxStrengthBonus/Attributes/
    /// OnHitEffectsRaw) — no combat code changes.
    /// </summary>
    public static class WeaponForgingService
    {
        public const string ForgedWeaponBlueprintName = "ForgedWeapon";
        public const string DiagCategory = "craft";

        public const string BladeSlot = "Blade";
        public const string HaftSlot = "Haft";
        public const string BindingSlot = "Binding";

        public static bool IsBladeSlot(string slot) => string.Equals(slot, BladeSlot, StringComparison.OrdinalIgnoreCase);
        public static bool IsHaftSlot(string slot) => string.Equals(slot, HaftSlot, StringComparison.OrdinalIgnoreCase);
        public static bool IsBindingSlot(string slot) => string.Equals(slot, BindingSlot, StringComparison.OrdinalIgnoreCase);

        private struct ConsumedComponent
        {
            public Entity Entity;
            public bool ConsumedFromStack;
        }

        public static bool TryForge(
            Entity crafter,
            EntityFactory factory,
            Entity blade,
            Entity haft,
            Entity binding,
            out Entity weapon,
            out string reason)
        {
            weapon = null;
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            if (factory == null)
            {
                reason = "Entity factory is missing.";
                return RejectDiag(crafter, reason);
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            if (inventory == null)
            {
                reason = "Crafter cannot forge without an inventory.";
                return RejectDiag(crafter, reason);
            }

            if (!ValidateComponent(inventory, blade, BladeSlot, out WeaponComponentPart bladePart, out reason)
                || !ValidateComponent(inventory, haft, HaftSlot, out WeaponComponentPart haftPart, out reason)
                || !ValidateComponent(inventory, binding, BindingSlot, out WeaponComponentPart bindingPart, out reason))
            {
                return RejectDiag(crafter, reason);
            }

            if (ReferenceEquals(blade, haft) || ReferenceEquals(blade, binding) || ReferenceEquals(haft, binding))
            {
                reason = "The same component was selected twice.";
                return RejectDiag(crafter, reason);
            }

            // ── Execution ──
            var consumed = new List<ConsumedComponent>(3);
            Entity[] components = { blade, haft, binding };
            for (int i = 0; i < components.Length; i++)
            {
                if (!TryConsumeComponent(inventory, components[i], out ConsumedComponent record))
                {
                    RestoreConsumed(inventory, consumed);
                    reason = "Failed to consume " + components[i].GetDisplayName() + ".";
                    return RejectDiag(crafter, reason);
                }

                consumed.Add(record);
            }

            Entity forged = factory.CreateEntity(ForgedWeaponBlueprintName);
            if (forged == null)
            {
                RestoreConsumed(inventory, consumed);
                reason = "Failed to create '" + ForgedWeaponBlueprintName + "'.";
                return RejectDiag(crafter, reason);
            }

            var assembly = new WeaponAssemblyPart
            {
                BladeBlueprint = blade.BlueprintName,
                HaftBlueprint = haft.BlueprintName,
                BindingBlueprint = binding.BlueprintName
            };
            forged.AddPart(assembly);
            ApplyComponentStats(forged, bladePart, haftPart, bindingPart);

            if (!inventory.AddObject(forged))
            {
                RestoreConsumed(inventory, consumed);
                reason = "Cannot add the forged weapon to inventory.";
                return RejectDiag(crafter, reason);
            }

            weapon = forged;
            MessageLog.Add(crafter.GetDisplayName() + " forges " + forged.GetDisplayName() + ".");

            if (Diag.IsChannelEnabled(DiagCategory))
            {
                Diag.Record(DiagCategory, "WeaponForged", actor: crafter, target: forged, payload: new
                {
                    blade = blade.BlueprintName,
                    haft = haft.BlueprintName,
                    binding = binding.BlueprintName
                });
            }

            return true;
        }

        /// <summary>
        /// Swap ONE component of a forged weapon. The new component is
        /// consumed; the displaced component is re-created from its recorded
        /// blueprint and added to the crafter's inventory; the weapon's
        /// stats are recomputed from the updated component set.
        /// </summary>
        public static bool TryReforge(
            Entity crafter,
            EntityFactory factory,
            Entity weapon,
            Entity newComponent,
            out Entity returnedComponent,
            out string reason)
        {
            returnedComponent = null;
            reason = string.Empty;

            if (crafter == null || factory == null)
            {
                reason = "Crafter or factory is missing.";
                return false;
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            if (inventory == null)
            {
                reason = "Crafter cannot re-forge without an inventory.";
                return RejectDiag(crafter, reason);
            }

            if (weapon == null || !inventory.Contains(weapon))
            {
                reason = "You must own the weapon to re-forge it.";
                return RejectDiag(crafter, reason);
            }

            WeaponAssemblyPart assembly = weapon.GetPart<WeaponAssemblyPart>();
            if (assembly == null)
            {
                reason = weapon.GetDisplayName() + " was not forged from components.";
                return RejectDiag(crafter, reason);
            }

            if (!ValidateComponent(inventory, newComponent, null, out WeaponComponentPart newPart, out reason))
                return RejectDiag(crafter, reason);

            string slot = newPart.Slot;
            string displacedBlueprint = assembly.GetBlueprintForSlot(slot);
            if (string.IsNullOrWhiteSpace(displacedBlueprint))
            {
                reason = "Unknown component slot '" + slot + "'.";
                return RejectDiag(crafter, reason);
            }

            if (!TryConsumeComponent(inventory, newComponent, out ConsumedComponent consumedNew))
            {
                reason = "Failed to consume " + newComponent.GetDisplayName() + ".";
                return RejectDiag(crafter, reason);
            }

            Entity displaced = factory.CreateEntity(displacedBlueprint);
            if (displaced == null || !inventory.AddObject(displaced))
            {
                RestoreConsumed(inventory, new List<ConsumedComponent> { consumedNew });
                reason = "Failed to return the displaced component '" + displacedBlueprint + "'.";
                return RejectDiag(crafter, reason);
            }

            assembly.SetBlueprintForSlot(slot, newComponent.BlueprintName);

            if (!TryRecomputeStats(weapon, assembly, factory, out reason))
            {
                // Roll the swap back: undo the record, take the displaced
                // copy back out, restore the consumed new component.
                assembly.SetBlueprintForSlot(slot, displacedBlueprint);
                inventory.RemoveObject(displaced);
                RestoreConsumed(inventory, new List<ConsumedComponent> { consumedNew });
                return RejectDiag(crafter, reason);
            }

            returnedComponent = displaced;
            MessageLog.Add(crafter.GetDisplayName() + " re-forges " + weapon.GetDisplayName() + ".");

            if (Diag.IsChannelEnabled(DiagCategory))
            {
                Diag.Record(DiagCategory, "WeaponReforged", actor: crafter, target: weapon, payload: new
                {
                    slot,
                    installed = newComponent.BlueprintName,
                    displaced = displacedBlueprint
                });
            }

            return true;
        }

        /// <summary>
        /// Recompute a forged weapon's stats from its assembly record by
        /// instantiating each component blueprint transiently and re-running
        /// the same combination math the original forge used. Deterministic:
        /// forge(A,B,C) and reforge-to-(A,B,C) produce identical stats.
        /// </summary>
        public static bool TryRecomputeStats(
            Entity weapon, WeaponAssemblyPart assembly, EntityFactory factory, out string reason)
        {
            reason = string.Empty;

            WeaponComponentPart blade = InstantiateComponentPart(factory, assembly.BladeBlueprint);
            WeaponComponentPart haft = InstantiateComponentPart(factory, assembly.HaftBlueprint);
            WeaponComponentPart binding = InstantiateComponentPart(factory, assembly.BindingBlueprint);
            if (blade == null || haft == null || binding == null)
            {
                reason = "A recorded component blueprint no longer exists.";
                return false;
            }

            ApplyComponentStats(weapon, blade, haft, binding);
            return true;
        }

        /// <summary>
        /// The combination math (§7.1 L1):
        ///   BaseDamage        = blade's dice
        ///   PenBonus/HitBonus = summed across all three
        ///   MaxStrengthBonus  = MAX across components (-1 entries ignored)
        ///   Attributes        = de-duplicated union, insertion-ordered
        ///   OnHitEffectsRaw   = non-empty quirks joined with ';'
        ///   DisplayName       = haft + binding + blade name fragments
        /// </summary>
        private static void ApplyComponentStats(
            Entity weapon,
            WeaponComponentPart blade,
            WeaponComponentPart haft,
            WeaponComponentPart binding)
        {
            MeleeWeaponPart melee = weapon.GetPart<MeleeWeaponPart>();
            if (melee == null)
            {
                melee = new MeleeWeaponPart();
                weapon.AddPart(melee);
            }

            WeaponComponentPart[] parts = { blade, haft, binding };

            melee.BaseDamage = string.IsNullOrWhiteSpace(blade.BaseDamage) ? "1d2" : blade.BaseDamage;

            int pen = 0, hit = 0, maxStr = -1;
            var attributes = new List<string>();
            var quirks = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                WeaponComponentPart part = parts[i];
                pen += part.PenBonus;
                hit += part.HitBonus;
                if (part.MaxStrengthBonus > maxStr)
                    maxStr = part.MaxStrengthBonus;

                if (!string.IsNullOrWhiteSpace(part.Attributes))
                {
                    string[] tokens = part.Attributes.Split(
                        new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int t = 0; t < tokens.Length; t++)
                    {
                        bool present = false;
                        for (int a = 0; a < attributes.Count; a++)
                        {
                            if (string.Equals(attributes[a], tokens[t], StringComparison.OrdinalIgnoreCase))
                            {
                                present = true;
                                break;
                            }
                        }

                        if (!present)
                            attributes.Add(tokens[t]);
                    }
                }

                if (!string.IsNullOrWhiteSpace(part.OnHitEffectSpec))
                {
                    if (quirks.Length > 0)
                        quirks.Append(';');
                    quirks.Append(part.OnHitEffectSpec.Trim());
                }
            }

            melee.PenBonus = pen;
            melee.HitBonus = hit;
            melee.MaxStrengthBonus = maxStr;
            melee.Attributes = string.Join(" ", attributes);
            melee.OnHitEffectsRaw = quirks.ToString();

            RenderPart render = weapon.GetPart<RenderPart>();
            if (render != null)
            {
                var nameParts = new List<string>(3);
                if (!string.IsNullOrWhiteSpace(haft.NameFragment)) nameParts.Add(haft.NameFragment.Trim());
                if (!string.IsNullOrWhiteSpace(binding.NameFragment)) nameParts.Add(binding.NameFragment.Trim());
                if (!string.IsNullOrWhiteSpace(blade.NameFragment)) nameParts.Add(blade.NameFragment.Trim());
                if (nameParts.Count > 0)
                    render.DisplayName = string.Join(" ", nameParts);
            }
        }

        private static WeaponComponentPart InstantiateComponentPart(EntityFactory factory, string blueprintName)
        {
            if (factory == null || string.IsNullOrWhiteSpace(blueprintName))
                return null;

            Entity transient = factory.CreateEntity(blueprintName);
            return transient?.GetPart<WeaponComponentPart>();
        }

        /// <summary>
        /// Owned + carries WeaponComponentPart + (when expectedSlot is
        /// non-null) matches the slot.
        /// </summary>
        private static bool ValidateComponent(
            InventoryPart inventory,
            Entity item,
            string expectedSlot,
            out WeaponComponentPart part,
            out string reason)
        {
            part = null;
            reason = string.Empty;

            if (item == null)
            {
                reason = "A required component is missing.";
                return false;
            }

            if (!inventory.Contains(item))
            {
                reason = "You must own all selected components.";
                return false;
            }

            part = item.GetPart<WeaponComponentPart>();
            if (part == null)
            {
                reason = item.GetDisplayName() + " is not a weapon component.";
                return false;
            }

            if (expectedSlot != null
                && !string.Equals(part.Slot, expectedSlot, StringComparison.OrdinalIgnoreCase))
            {
                reason = item.GetDisplayName() + " is not a " + expectedSlot.ToLowerInvariant() + ".";
                return false;
            }

            if (expectedSlot == null
                && !IsBladeSlot(part.Slot) && !IsHaftSlot(part.Slot) && !IsBindingSlot(part.Slot))
            {
                reason = item.GetDisplayName() + " has unknown component slot '" + part.Slot + "'.";
                return false;
            }

            return true;
        }

        private static bool RejectDiag(Entity crafter, string reason)
        {
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "ForgeRejected", actor: crafter, payload: new { reason });
            return false;
        }

        private static bool TryConsumeComponent(InventoryPart inventory, Entity item, out ConsumedComponent record)
        {
            record = new ConsumedComponent();
            if (inventory == null || item == null)
                return false;

            StackerPart stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                record.Entity = item;
                record.ConsumedFromStack = true;
                return true;
            }

            if (inventory.RemoveObject(item))
            {
                record.Entity = item;
                record.ConsumedFromStack = false;
                return true;
            }

            return false;
        }

        private static void RestoreConsumed(InventoryPart inventory, List<ConsumedComponent> consumed)
        {
            if (inventory == null || consumed == null)
                return;

            for (int i = 0; i < consumed.Count; i++)
            {
                ConsumedComponent record = consumed[i];
                if (record.Entity == null)
                    continue;

                if (record.ConsumedFromStack)
                {
                    StackerPart stacker = record.Entity.GetPart<StackerPart>();
                    if (stacker != null)
                    {
                        stacker.StackCount += 1;
                        continue;
                    }
                }

                if (!inventory.Contains(record.Entity))
                    inventory.AddObject(record.Entity);
            }
        }
    }
}
