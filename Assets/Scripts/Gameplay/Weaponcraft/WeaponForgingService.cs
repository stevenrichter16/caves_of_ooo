using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;

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
        // Matches the shipped ForgedWeapon base name when custom components supply no names.
        private const string UnnamedWeaponDisplayName = "forged weapon";

        public const string BladeSlot = "Blade";
        public const string HaftSlot = "Haft";
        public const string BindingSlot = "Binding";

        public static bool IsBladeSlot(string slot) => string.Equals(slot, BladeSlot, StringComparison.OrdinalIgnoreCase);
        public static bool IsHaftSlot(string slot) => string.Equals(slot, HaftSlot, StringComparison.OrdinalIgnoreCase);
        public static bool IsBindingSlot(string slot) => string.Equals(slot, BindingSlot, StringComparison.OrdinalIgnoreCase);

        /// <summary>Forge one unit and return its actual carried recipient, which
        /// may be an existing compatible stack. Failure leaves payment unchanged.</summary>
        public static bool TryForge(Entity crafter, EntityFactory factory, Entity blade,
            Entity haft, Entity binding, out Entity weapon, out string reason)
        {
            var transaction = new InventoryTransaction();
            try
            {
                bool ok = TryForge(crafter, factory, blade, haft, binding, transaction, out weapon, out reason);
                if (ok) transaction.Commit();
                return ok;
            }
            finally { transaction.Rollback(); }
        }

        internal static bool TryForge(Entity crafter, EntityFactory factory, Entity blade,
            Entity haft, Entity binding, InventoryTransaction transaction, out Entity weapon, out string reason)
        {
            weapon = null; reason = string.Empty;
            var inventory = crafter?.GetPart<InventoryPart>();
            if (inventory == null || factory == null)
            { reason = "Forging requires a crafter, inventory and entity factory."; return RejectDiag(crafter, reason); }
            if (!ValidateComponent(inventory, blade, BladeSlot, out var bladePart, out reason)
                || !ValidateComponent(inventory, haft, HaftSlot, out var haftPart, out reason)
                || !ValidateComponent(inventory, binding, BindingSlot, out var bindingPart, out reason))
                return RejectDiag(crafter, reason);
            if (ReferenceEquals(blade, haft) || ReferenceEquals(blade, binding) || ReferenceEquals(haft, binding))
            { reason = "The same component was selected twice."; return RejectDiag(crafter, reason); }

            var operation = new WeaponCraftingOperation(crafter, transaction, "ForgeWeapon");
            if (!operation.Claim(blade, haft, binding))
            { reason = "A selected component is already in use."; return RejectDiag(crafter, reason); }
            // Prepare before payment and receipt: factory initialization can perform
            // independent work, which a failed forge must not rewind.
            var forged = factory.CreateEntity(ForgedWeaponBlueprintName);
            if (forged == null)
            { reason = "Failed to create '" + ForgedWeaponBlueprintName + "'."; return RejectDiag(crafter, reason); }
            forged.AddPart(new WeaponAssemblyPart { BladeBlueprint = blade.BlueprintName,
                HaftBlueprint = haft.BlueprintName, BindingBlueprint = binding.BlueprintName });
            ApplyComponentStats(forged, bladePart, haftPart, bindingPart);
            var receipt = operation.Capture(forged);
            Entity recipient = null;
            bool applied = receipt.Apply(() => inventory.TryConsumeOne(blade)
                && inventory.TryConsumeOne(haft) && inventory.TryConsumeOne(binding)
                && inventory.AddCraftedUnit(forged, out recipient));
            if (!applied || !operation.Finish(receipt))
            {
                operation.Restore(); reason = "Cannot add the forged weapon to inventory.";
                return RejectDiag(crafter, reason);
            }
            weapon = recipient;
            MessageLog.Add(crafter.GetDisplayName() + " forges " + InventoryPart.GetUnitDisplayName(recipient) + ".");
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "WeaponForged", actor: crafter, target: recipient,
                    payload: new { blade = blade.BlueprintName, haft = haft.BlueprintName, binding = binding.BlueprintName });
            return true;
        }

        /// <summary>
        /// Pure, read-only preview: the largest number of IDENTICAL weapons
        /// this exact blade/haft/binding selection could forge — the
        /// smallest available quantity across the three slots
        /// (StackerPart.StackCount when present, else 1). Consumes
        /// nothing; <see cref="TryForgeBatch"/> is the authority.
        /// </summary>
        public static int GetMaxBatchCount(Entity blade, Entity haft, Entity binding)
        {
            int bladeCount = AvailableCount(blade);
            int haftCount = AvailableCount(haft);
            int bindingCount = AvailableCount(binding);
            if (bladeCount == 0 || haftCount == 0 || bindingCount == 0)
                return 0;

            int min = bladeCount;
            if (haftCount < min) min = haftCount;
            if (bindingCount < min) min = bindingCount;
            return min;
        }

        /// <summary>
        /// Repeat <see cref="TryForge"/> up to <paramref name="requestedCount"/>
        /// times against the SAME three component references — mirrors
        /// BrewingService.TryBrewBatch exactly. Each iteration re-validates
        /// and consumes via the positive carried-unit payment, so
        /// passing the same entities repeatedly naturally exhausts whichever
        /// slot runs out first; no new bookkeeping needed.
        ///
        /// Return contract: TRUE if at least one weapon was forged (a
        /// partial batch is a smaller success). FALSE only when the FIRST
        /// iteration fails. Exceptions roll back the enclosing transaction.
        /// </summary>
        public static bool TryForgeBatch(Entity crafter, EntityFactory factory, Entity blade,
            Entity haft, Entity binding, int requestedCount, out List<Entity> producedWeapons,
            out int madeCount, out string reason)
        {
            var transaction = new InventoryTransaction();
            try
            {
                bool ok = TryForgeBatch(crafter, factory, blade, haft, binding, requestedCount,
                    transaction, out producedWeapons, out madeCount, out reason);
                if (ok) transaction.Commit();
                return ok;
            }
            finally { transaction.Rollback(); }
        }

        // Results represent produced units, so several entries may name one resident stack.
        internal static bool TryForgeBatch(Entity crafter, EntityFactory factory, Entity blade,
            Entity haft, Entity binding, int requestedCount, InventoryTransaction transaction,
            out List<Entity> producedWeapons, out int madeCount, out string reason)
        {
            producedWeapons = new List<Entity>();
            madeCount = 0;
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            if (requestedCount <= 0)
            {
                reason = "Requested forge count must be positive.";
                return RejectDiag(crafter, reason);
            }

            for (int i = 0; i < requestedCount; i++)
            {
                bool ok = TryForge(crafter, factory, blade, haft, binding, transaction, out Entity weapon, out string iterationReason);
                if (!ok)
                {
                    if (madeCount == 0)
                    {
                        reason = iterationReason;
                        return false;
                    }

                    reason = "Ran out of components after " + madeCount + ": " + iterationReason;
                    break;
                }

                producedWeapons.Add(weapon);
                madeCount++;
            }

            return true;
        }

        private static int AvailableCount(Entity item)
        {
            if (item == null)
                return 0;

            StackerPart stacker = item.GetPart<StackerPart>();
            return stacker == null ? 1 : Math.Max(0, stacker.StackCount);
        }

        /// <summary>
        /// Swap ONE component of a forged weapon. The new component is
        /// consumed; the displaced component is re-created from its recorded
        /// blueprint and added to the crafter's inventory; the weapon's
        /// stats are recomputed from the updated component set.
        /// </summary>
        public static bool TryReforge(Entity crafter, EntityFactory factory, Entity weapon,
            Entity newComponent, out Entity returnedComponent, out string reason) =>
            TryReforge(crafter, factory, weapon, newComponent, out _, out returnedComponent, out reason);

        /// <summary>Transform one owned weapon unit. Returns its actual recipient and
        /// the actual carried recipient of the displaced component. Equipped singletons
        /// retain identity; carried stacks split automatically. Failure restores payment.</summary>
        public static bool TryReforge(Entity crafter, EntityFactory factory, Entity weapon,
            Entity newComponent, out Entity affectedWeapon, out Entity returnedComponent, out string reason)
        {
            var transaction = new InventoryTransaction();
            try
            {
                bool ok = TryReforge(crafter, factory, weapon, newComponent, transaction,
                    out affectedWeapon, out returnedComponent, out reason);
                if (ok) transaction.Commit();
                return ok;
            }
            finally { transaction.Rollback(); }
        }

        internal static bool TryReforge(Entity crafter, EntityFactory factory, Entity weapon,
            Entity newComponent, InventoryTransaction transaction,
            out Entity affectedWeapon, out Entity returnedComponent, out string reason)
        {
            affectedWeapon = null; returnedComponent = null; reason = string.Empty;
            var inventory = crafter?.GetPart<InventoryPart>();
            if (inventory == null || factory == null)
            { reason = "Re-forging requires a crafter, inventory and entity factory."; return RejectDiag(crafter, reason); }
            if (!WeaponCraftingOperation.CanTransform(inventory, weapon))
            { reason = "You must own a positive weapon unit; equipped stacks cannot be re-forged."; return RejectDiag(crafter, reason); }
            if (!weapon.HasPart<MeleeWeaponPart>())
            { reason = "That item is not a melee weapon."; return RejectDiag(crafter, reason); }
            var assembly = weapon.GetPart<WeaponAssemblyPart>();
            if (assembly == null)
            { reason = "That weapon was not forged from components."; return RejectDiag(crafter, reason); }
            if (ReferenceEquals(weapon, newComponent))
            { reason = "A weapon cannot be its own replacement component."; return RejectDiag(crafter, reason); }
            if (!ValidateComponent(inventory, newComponent, null, out var newPart, out reason)) return RejectDiag(crafter, reason);
            string slot = newPart.Slot, displacedBlueprint = assembly.GetBlueprintForSlot(slot);
            if (string.IsNullOrWhiteSpace(displacedBlueprint))
            { reason = "Unknown component slot '" + slot + "'."; return RejectDiag(crafter, reason); }
            var operation = new WeaponCraftingOperation(crafter, transaction, "ReforgeWeapon");
            if (!operation.Claim(weapon, newComponent))
            { reason = "A selected item is already in use."; return RejectDiag(crafter, reason); }
            var planned = new WeaponAssemblyPart { BladeBlueprint = assembly.BladeBlueprint,
                HaftBlueprint = assembly.HaftBlueprint, BindingBlueprint = assembly.BindingBlueprint };
            planned.SetBlueprintForSlot(slot, newComponent.BlueprintName);
            var blade = InstantiateComponentPart(factory, planned.BladeBlueprint);
            var haft = InstantiateComponentPart(factory, planned.HaftBlueprint);
            var binding = InstantiateComponentPart(factory, planned.BindingBlueprint);
            var displaced = factory.CreateEntity(displacedBlueprint);
            if (blade == null || haft == null || binding == null || displaced == null)
            { reason = "A recorded component blueprint no longer exists."; return RejectDiag(crafter, reason); }
            var unit = WeaponCraftingOperation.PrepareUnit(weapon);
            if (!WeaponCraftingOperation.CanTransform(inventory, weapon) || !inventory.CanConsumeOne(newComponent))
            { reason = "A selected item is no longer available."; return RejectDiag(crafter, reason); }
            bool split = !ReferenceEquals(unit, weapon), melted = (unit.GetPart<WeaponTemperPart>()?.TemperCount ?? 0) > 0;
            if (!split) operation.CapturePayload(unit);
            var receipt = operation.Capture(unit, displaced);
            Entity recipient = unit, returned = null;
            bool applied = receipt.Apply(() =>
            {
                if (!inventory.TryConsumeOne(newComponent) || (split && !inventory.TryConsumeOne(weapon))) return false;
                unit.GetPart<WeaponAssemblyPart>().SetBlueprintForSlot(slot, newComponent.BlueprintName);
                ApplyComponentStats(unit, blade, haft, binding);
                WeaponTemperingService.ClearTemper(unit, false);
                return (!split || inventory.AddCraftedUnit(unit, out recipient))
                    && inventory.AddCraftedUnit(displaced, out returned);
            });
            if (!applied || !operation.Finish(receipt))
            {
                operation.Restore(); reason = "Cannot fit the re-forged weapon and displaced component in inventory.";
                return RejectDiag(crafter, reason);
            }
            operation.MoveMark(weapon, recipient);
            affectedWeapon = recipient; returnedComponent = returned;
            if (melted) MessageLog.Add("The re-forging melts away the temper.");
            MessageLog.Add(crafter.GetDisplayName() + " re-forges " + InventoryPart.GetUnitDisplayName(recipient) + ".");
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "WeaponReforged", actor: crafter, target: recipient,
                    payload: new { slot, installed = newComponent.BlueprintName, displaced = displacedBlueprint });
            return true;
        }

        /// <summary>
        /// Recompute a forged weapon's stats from its assembly record by
        /// instantiating each component blueprint transiently and re-running
        /// the same combination math the original forge used. Deterministic:
        /// forge(A,B,C) and reforge-to-(A,B,C) produce identical component
        /// contributions. Existing permanent modifications are restored afterward.
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
        /// <summary>
        /// Pure, read-only: what this blade/haft/binding WOULD become.
        /// Consumes nothing, creates nothing, logs nothing — the Crafting
        /// panel recomputes it on every selection change.
        ///
        /// <para>This is the single source of the combination math (§7.1
        /// L1). <see cref="ApplyComponentStats"/> calls it and copies the
        /// answer onto the weapon, so preview and product cannot drift.
        /// Any component may be null; the result is then marked
        /// incomplete and names what is missing.</para>
        /// </summary>
        public static ForgePreview PreviewForge(
            WeaponComponentPart blade,
            WeaponComponentPart haft,
            WeaponComponentPart binding)
        {
            var preview = new ForgePreview
            {
                Attributes = string.Empty,
                OnHitEffectsRaw = string.Empty,
                DisplayName = string.Empty,
                BaseDamage = string.Empty,
                Missing = string.Empty,
                MaxStrengthBonus = -1,
            };

            // Name the gaps before doing any math — the panel shows this
            // while the player is still assembling.
            var missing = new List<string>(3);
            if (blade == null) missing.Add("a blade");
            if (haft == null) missing.Add("a haft");
            if (binding == null) missing.Add("a binding");
            if (missing.Count > 0)
            {
                preview.IsComplete = false;
                preview.Missing = JoinWithAnd(missing);
                return preview;
            }

            preview.IsComplete = true;

            WeaponComponentPart[] parts = { blade, haft, binding };

            preview.BaseDamage = string.IsNullOrWhiteSpace(blade.BaseDamage)
                ? "1d2" : blade.BaseDamage;

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

            preview.PenBonus = pen;
            preview.HitBonus = hit;
            preview.MaxStrengthBonus = maxStr;
            preview.Attributes = string.Join(" ", attributes);
            preview.OnHitEffectsRaw = quirks.ToString();

            var nameParts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(haft.NameFragment)) nameParts.Add(haft.NameFragment.Trim());
            if (!string.IsNullOrWhiteSpace(binding.NameFragment)) nameParts.Add(binding.NameFragment.Trim());
            if (!string.IsNullOrWhiteSpace(blade.NameFragment)) nameParts.Add(blade.NameFragment.Trim());
            preview.DisplayName = nameParts.Count > 0 ? string.Join(" ", nameParts) : string.Empty;

            return preview;
        }

        /// <summary>"a haft" / "a haft and a binding" / "a blade, a haft and a binding".</summary>
        private static string JoinWithAnd(List<string> items)
        {
            if (items.Count == 0) return string.Empty;
            if (items.Count == 1) return items[0];
            var sb = new StringBuilder();
            for (int i = 0; i < items.Count - 1; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(items[i]);
            }
            sb.Append(" and ").Append(items[items.Count - 1]);
            return sb.ToString();
        }

        /// <summary>
        /// Writes <see cref="PreviewForge"/>'s answer onto the weapon.
        /// Deliberately contains no math of its own — see ForgePreview's
        /// anti-drift note.
        /// </summary>
        private static void ApplyComponentStats(
            Entity weapon,
            WeaponComponentPart blade,
            WeaponComponentPart haft,
            WeaponComponentPart binding)
        {
            ForgePreview preview = PreviewForge(blade, haft, binding);

            MeleeWeaponPart melee = weapon.GetPart<MeleeWeaponPart>();
            if (melee == null)
            {
                melee = new MeleeWeaponPart();
                weapon.AddPart(melee);
            }

            melee.BaseDamage = preview.BaseDamage;
            melee.PenBonus = preview.PenBonus;
            melee.HitBonus = preview.HitBonus;
            melee.MaxStrengthBonus = preview.MaxStrengthBonus;
            melee.Attributes = preview.Attributes;
            melee.OnHitEffectsRaw = preview.OnHitEffectsRaw;

            RenderPart render = weapon.GetPart<RenderPart>();
            if (render != null)
                render.DisplayName = string.IsNullOrWhiteSpace(preview.DisplayName) ? UnnamedWeaponDisplayName : preview.DisplayName;

            RestorePermanentModifications(weapon);
        }

        // Rebuilding components replaces their old contributions and temper labels.
        // Restore known permanent state without replaying paid Apply/equipment hooks,
        // which would duplicate effects, costs or ModificationCount. Every enhancement
        // occurrence remains attached; its visible adjective appears once per type.
        private static void RestorePermanentModifications(Entity weapon)
        {
            var labels = new List<string>(4);
            if (weapon.HasTag(SharpTinkerModification.ModTag))
            {
                weapon.GetPart<MeleeWeaponPart>().PenBonus += SharpTinkerModification.PenetrationBonus;
                labels.Add(SharpTinkerModification.Adjective);
            }
            if (weapon.HasPart<EnhancementPaleSalt>()) labels.Add(PaleSaltTinkerModification.Adjective);
            if (weapon.HasPart<EnhancementChoirIron>()) labels.Add(ChoirIronTinkerModification.Adjective);
            if (weapon.HasPart<EnhancementGlowQuartz>()) labels.Add(GlowQuartzTinkerModification.Adjective);

            var render = weapon.GetPart<RenderPart>();
            if (labels.Count > 0 && render != null && !string.IsNullOrWhiteSpace(render.DisplayName))
                render.DisplayName = string.Join(" ", labels) + " " + render.DisplayName;
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

            if (!inventory.CanConsumeOne(item))
            {
                reason = "You must own and carry a positive unit of every selected component.";
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

    }
}
