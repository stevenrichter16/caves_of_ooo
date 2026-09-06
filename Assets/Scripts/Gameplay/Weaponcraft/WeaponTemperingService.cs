using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tempering / quenching (§7.1 Layer 2) - THE alchemy→weaponcraft
    /// bridge: a brewed COATING (BrewItemPart, Form "Coating") is the quench
    /// medium. Quenching consumes the coating and writes its effects onto
    /// the weapon as on-hit specs in MeleeWeaponPart.OnHitEffectsRaw - the
    /// grammar OnHitWeaponEffects already consumes in combat.
    ///
    /// The trade-off (§7.1: "trade-offs are real stat deltas, not flavor
    /// text"): each temper fatigues the metal - Hitpoints max −2 (floored at
    /// 1) - and the metal holds at most <see cref="MaxTempers"/> quenches.
    /// Re-forging melts the temper away entirely (specs recomputed from
    /// components; HP penalty restored) - see
    /// WeaponForgingService.TryReforge + <see cref="ClearTemper"/>.
    /// </summary>
    public static class WeaponTemperingService
    {
        public const int MaxTempers = 2;
        public const int HpPenaltyPerTemper = 2;
        public const string DiagCategory = WeaponForgingService.DiagCategory;

        public static bool TryTemper(Entity crafter, Entity weapon, Entity quench, out string reason) =>
            TryTemper(crafter, weapon, quench, out _, out reason);

        /// <summary>Temper one owned weapon unit with one positive carried brew.
        /// Carried stacks split automatically; returns the actual transformed recipient.
        /// Equipped singletons retain identity. Refusal restores quantity and payload.</summary>
        public static bool TryTemper(Entity crafter, Entity weapon, Entity quench,
            out Entity affectedWeapon, out string reason)
        {
            var transaction = new InventoryTransaction();
            try
            {
                bool ok = TryTemper(crafter, weapon, quench, transaction, out affectedWeapon, out reason);
                if (ok) transaction.Commit();
                return ok;
            }
            finally { transaction.Rollback(); }
        }

        internal static bool TryTemper(Entity crafter, Entity weapon, Entity quench,
            InventoryTransaction transaction, out Entity affectedWeapon, out string reason)
        {
            affectedWeapon = null;
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            if (inventory == null)
            {
                reason = "Crafter cannot temper without an inventory.";
                return RejectDiag(crafter, reason);
            }

            if (!WeaponCraftingOperation.CanTransform(inventory, weapon))
            {
                reason = "You must own a positive weapon unit; equipped stacks cannot be tempered.";
                return RejectDiag(crafter, reason);
            }

            MeleeWeaponPart melee = weapon.GetPart<MeleeWeaponPart>();
            if (melee == null)
            {
                reason = weapon.GetDisplayName() + " is not a melee weapon.";
                return RejectDiag(crafter, reason);
            }

            if (ReferenceEquals(weapon, quench))
            {
                reason = "A weapon cannot be quenched in itself.";
                return RejectDiag(crafter, reason);
            }

            if (!inventory.CanConsumeOne(quench))
            {
                reason = "You must carry a positive unit of quench medium.";
                return RejectDiag(crafter, reason);
            }

            // User-directed design change (2026-07-19), superseding the
            // M3-L2 coating-only lockdown: ANY brewed mixture that carries
            // effects can quench a blade — coatings, tonics, and throwable
            // flasks alike. The empty-payload rejection below still holds.
            BrewItemPart brew = quench.GetPart<BrewItemPart>();
            if (brew == null)
            {
                reason = quench.GetDisplayName() + " is not a brewed mixture - only brews can quench a blade.";
                return RejectDiag(crafter, reason);
            }

            IReadOnlyList<BrewPropertyAmount> effects = brew.GetEffects();
            if (effects.Count == 0)
            {
                reason = quench.GetDisplayName() + " carries nothing to quench with.";
                return RejectDiag(crafter, reason);
            }

            if ((weapon.GetPart<WeaponTemperPart>()?.TemperCount ?? 0) >= MaxTempers)
            { reason = "The metal can hold no more tempering."; return RejectDiag(crafter, reason); }
            var operation = new WeaponCraftingOperation(crafter, transaction, "TemperWeapon");
            if (!operation.Claim(weapon, quench))
            { reason = "A selected item is already in use."; return RejectDiag(crafter, reason); }
            var unit = WeaponCraftingOperation.PrepareUnit(weapon);
            if (!WeaponCraftingOperation.CanTransform(inventory, weapon) || !inventory.CanConsumeOne(quench))
            { reason = "A selected item is no longer available."; return RejectDiag(crafter, reason); }
            bool split = !ReferenceEquals(unit, weapon);
            if (!split) operation.CapturePayload(unit);
            var receipt = operation.Capture(unit);
            Entity recipient = unit;
            string joined = null; int penaltyApplied = 0;
            bool applied = receipt.Apply(() =>
            {
                if (!inventory.TryConsumeOne(quench) || (split && !inventory.TryConsumeOne(weapon))) return false;
                ApplyTemper(unit, effects, operation, out joined, out penaltyApplied);
                return !split || inventory.AddCraftedUnit(unit, out recipient);
            });
            if (!applied || !operation.Finish(receipt))
            {
                operation.Restore(); reason = "Cannot fit the tempered weapon in inventory.";
                return RejectDiag(crafter, reason);
            }
            operation.MoveMark(weapon, recipient);
            affectedWeapon = recipient;
            MessageLog.Add(crafter.GetDisplayName() + " quenches " + InventoryPart.GetUnitDisplayName(recipient)
                + " - the metal drinks the coating.");
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "WeaponTempered", actor: crafter, target: recipient,
                    payload: new { quench = quench.BlueprintName, specs = joined,
                        temperCount = recipient.GetPart<WeaponTemperPart>().TemperCount, hpPenalty = penaltyApplied });
            return true;
        }

        private static void ApplyTemper(Entity weapon, IReadOnlyList<BrewPropertyAmount> effects,
            WeaponCraftingOperation operation, out string joined, out int penaltyApplied)
        {
            var melee = weapon.GetPart<MeleeWeaponPart>();
            var temper = weapon.GetPart<WeaponTemperPart>();
            if (temper == null)
            {
                temper = new WeaponTemperPart();
                var added = temper;
                operation.Undo(() => weapon.RemovePart(added));
                weapon.AddPart(temper);
            }
            // ── Apply: effects → on-hit specs ──
            var specs = new List<string>(effects.Count);
            for (int i = 0; i < effects.Count; i++)
            {
                BrewPropertyAmount entry = effects[i];
                int potency = Math.Max(1, entry.Potency);
                int chance = Math.Min(50, 20 + 10 * potency);
                // OnHitEffectsRaw grammar: Name,Chance,Dice,Duration,Magnitude
                // (duration 0 / empty dice → per-effect defaults).
                specs.Add(entry.Property + "," + chance + ",,0," + potency);
            }

            joined = string.Join(";", specs);
            melee.OnHitEffectsRaw = string.IsNullOrWhiteSpace(melee.OnHitEffectsRaw)
                ? joined
                : melee.OnHitEffectsRaw + ";" + joined;

            // ── Trade-off: the metal fatigues ──
            penaltyApplied = 0;
            Stat hp = weapon.GetStat("Hitpoints");
            if (hp != null)
            {
                int newMax = Math.Max(1, hp.Max - HpPenaltyPerTemper);
                penaltyApplied = hp.Max - newMax;
                hp.Max = newMax;
                if (hp.BaseValue > newMax)
                    hp.BaseValue = newMax;
            }

            temper.TemperCount++;
            temper.HpPenaltyTotal += penaltyApplied;
            temper.AppliedSpecsRaw = string.IsNullOrWhiteSpace(temper.AppliedSpecsRaw)
                ? joined
                : temper.AppliedSpecsRaw + ";" + joined;

            RenderPart render = weapon.GetPart<RenderPart>();
            if (render != null && !string.IsNullOrWhiteSpace(render.DisplayName))
                render.DisplayName = QuenchPrefix(effects[0].Property) + " " + render.DisplayName;

        }

        /// <summary>
        /// Melt all tempering off a weapon: restore the Hitpoints-max
        /// penalty and reset the temper record. Called by
        /// WeaponForgingService.TryReforge AFTER stats are recomputed from
        /// components (which already wipes temper on-hit specs and resets
        /// the display name) - without this, the HP penalty would linger on
        /// a weapon whose specs no longer justify it.
        /// </summary>
        public static void ClearTemper(Entity weapon) => ClearTemper(weapon, true);

        internal static void ClearTemper(Entity weapon, bool notify)
        {
            WeaponTemperPart temper = weapon?.GetPart<WeaponTemperPart>();
            if (temper == null || temper.TemperCount == 0)
                return;

            Stat hp = weapon.GetStat("Hitpoints");
            if (hp != null && temper.HpPenaltyTotal > 0)
                hp.Max += temper.HpPenaltyTotal;

            temper.TemperCount = 0;
            temper.HpPenaltyTotal = 0;
            temper.AppliedSpecsRaw = "";

            if (notify) MessageLog.Add("The re-forging melts away the temper.");
        }

        private static string QuenchPrefix(string effectName)
        {
            string key = (effectName ?? "").Trim().ToLowerInvariant();
            switch (key)
            {
                case "burning": case "fire": case "burn": return "flame-quenched";
                case "frozen": case "frost": case "ice": return "frost-quenched";
                case "electrified": case "shock": case "lightning": return "storm-quenched";
                case "acidic": case "acid": return "acid-quenched";
                case "wet": case "water": return "slick-quenched";
                default: return "quenched";
            }
        }

        private static bool RejectDiag(Entity crafter, string reason)
        {
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "TemperRejected", actor: crafter, payload: new { reason });
            return false;
        }
    }
}
