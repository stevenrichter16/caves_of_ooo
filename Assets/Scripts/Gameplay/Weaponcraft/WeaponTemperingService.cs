using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tempering / quenching (§7.1 Layer 2) — THE alchemy→weaponcraft
    /// bridge: a brewed COATING (BrewItemPart, Form "Coating") is the quench
    /// medium. Quenching consumes the coating and writes its effects onto
    /// the weapon as on-hit specs in MeleeWeaponPart.OnHitEffectsRaw — the
    /// grammar OnHitWeaponEffects already consumes in combat.
    ///
    /// The trade-off (§7.1: "trade-offs are real stat deltas, not flavor
    /// text"): each temper fatigues the metal — Hitpoints max −2 (floored at
    /// 1) — and the metal holds at most <see cref="MaxTempers"/> quenches.
    /// Re-forging melts the temper away entirely (specs recomputed from
    /// components; HP penalty restored) — see
    /// WeaponForgingService.TryReforge + <see cref="ClearTemper"/>.
    /// </summary>
    public static class WeaponTemperingService
    {
        public const int MaxTempers = 2;
        public const int HpPenaltyPerTemper = 2;
        public const string DiagCategory = WeaponForgingService.DiagCategory;

        public static bool TryTemper(
            Entity crafter,
            Entity weapon,
            Entity quench,
            out string reason)
        {
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

            if (weapon == null || !inventory.Contains(weapon))
            {
                reason = "You must own the weapon to temper it.";
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

            if (quench == null || !inventory.Contains(quench))
            {
                reason = "You must own the quench medium.";
                return RejectDiag(crafter, reason);
            }

            BrewItemPart brew = quench.GetPart<BrewItemPart>();
            if (brew == null || !string.Equals(brew.Form, "Coating", StringComparison.OrdinalIgnoreCase))
            {
                reason = quench.GetDisplayName() + " is not a coating — only coatings can quench a blade.";
                return RejectDiag(crafter, reason);
            }

            IReadOnlyList<BrewPropertyAmount> effects = brew.GetEffects();
            if (effects.Count == 0)
            {
                reason = quench.GetDisplayName() + " carries nothing to quench with.";
                return RejectDiag(crafter, reason);
            }

            WeaponTemperPart temper = weapon.GetPart<WeaponTemperPart>();
            if (temper == null)
            {
                temper = new WeaponTemperPart();
                weapon.AddPart(temper);
            }

            if (temper.TemperCount >= MaxTempers)
            {
                reason = "The metal can hold no more tempering.";
                return RejectDiag(crafter, reason);
            }

            if (!TryConsumeQuench(inventory, quench))
            {
                reason = "Failed to consume " + quench.GetDisplayName() + ".";
                return RejectDiag(crafter, reason);
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

            string joined = string.Join(";", specs);
            melee.OnHitEffectsRaw = string.IsNullOrWhiteSpace(melee.OnHitEffectsRaw)
                ? joined
                : melee.OnHitEffectsRaw + ";" + joined;

            // ── Trade-off: the metal fatigues ──
            int penaltyApplied = 0;
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

            MessageLog.Add(
                crafter.GetDisplayName() + " quenches " + weapon.GetDisplayName()
                + " — the metal drinks the coating.");

            if (Diag.IsChannelEnabled(DiagCategory))
            {
                Diag.Record(DiagCategory, "WeaponTempered", actor: crafter, target: weapon, payload: new
                {
                    quench = quench.BlueprintName,
                    specs = joined,
                    temperCount = temper.TemperCount,
                    hpPenalty = penaltyApplied
                });
            }

            return true;
        }

        /// <summary>
        /// Melt all tempering off a weapon: restore the Hitpoints-max
        /// penalty and reset the temper record. Called by
        /// WeaponForgingService.TryReforge AFTER stats are recomputed from
        /// components (which already wipes temper on-hit specs and resets
        /// the display name) — without this, the HP penalty would linger on
        /// a weapon whose specs no longer justify it.
        /// </summary>
        public static void ClearTemper(Entity weapon)
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

            MessageLog.Add("The re-forging melts away the temper.");
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

        private static bool TryConsumeQuench(InventoryPart inventory, Entity quench)
        {
            StackerPart stacker = quench.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                return true;
            }

            return inventory.RemoveObject(quench);
        }

        private static bool RejectDiag(Entity crafter, string reason)
        {
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "TemperRejected", actor: crafter, payload: new { reason });
            return false;
        }
    }
}
