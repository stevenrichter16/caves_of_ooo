using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Resolves melee combat between entities.
    /// Now body-part-aware: when the attacker has a Body, each hand with an equipped
    /// weapon gets a separate attack (mirroring Qud's multi-weapon combat).
    ///
    /// Attack flow (body-part-aware):
    /// 1. Fire BeforeMeleeAttack event (can cancel)
    /// 2. Gather all weapons from hand body parts
    /// 3. For each weapon: hit roll → penetration → damage
    /// 4. Primary hand gets full stats; off-hands get penalty
    ///
    /// Defense: armor from Body body part + equipped armor items.
    /// </summary>
    public static class CombatSystem
    {
        /// <summary>
        /// Penalty to hit for off-hand (non-primary) attacks.
        /// Mirrors Qud's secondary weapon hit penalty.
        /// </summary>
        public const int OFF_HAND_HIT_PENALTY = -2;

        /// <summary>
        /// Perform a melee attack. Body-part-aware: attacks with each equipped weapon.
        /// Returns true if the attack was attempted.
        /// </summary>
        public static bool PerformMeleeAttack(Entity attacker, Entity defender, Zone zone, Random rng)
        {
            if (attacker == null || defender == null) return false;

            // 1. Fire BeforeMeleeAttack on attacker
            var beforeAttack = GameEvent.New("BeforeMeleeAttack");
            beforeAttack.SetParameter("Attacker", (object)attacker);
            beforeAttack.SetParameter("Defender", (object)defender);
            if (!attacker.FireEvent(beforeAttack))
                return false;

            var body = attacker.GetPart<Body>();

            if (body != null)
                return PerformBodyPartAwareAttack(attacker, defender, body, zone, rng);
            else
                return PerformLegacyAttack(attacker, defender, zone, rng);
        }

        /// <summary>
        /// Body-part-aware attack: each hand with a weapon gets an attack.
        /// Mirrors Qud's multi-weapon melee combat.
        /// </summary>
        private static bool PerformBodyPartAwareAttack(Entity attacker, Entity defender,
            Body body, Zone zone, Random rng)
        {
            var weapons = GatherMeleeWeapons(attacker, body);

            if (weapons.Count == 0)
            {
                // No weapons: punch/natural attack from primary hand or body
                PerformSingleAttack(attacker, defender, null, true, zone, rng);
                return true;
            }

            // Attack with each weapon
            for (int i = 0; i < weapons.Count; i++)
            {
                if (defender.GetStatValue("Hitpoints", 0) <= 0)
                    break; // Target already dead

                bool isPrimary = weapons[i].IsPrimary;
                PerformSingleAttack(attacker, defender, weapons[i].Weapon, isPrimary, zone, rng);
            }

            return true;
        }

        /// <summary>
        /// Legacy attack (no Body part): single weapon from equipped or natural.
        /// </summary>
        private static bool PerformLegacyAttack(Entity attacker, Entity defender,
            Zone zone, Random rng)
        {
            MeleeWeaponPart weapon = null;
            var attackerInventory = attacker.GetPart<InventoryPart>();
            if (attackerInventory != null)
            {
                var equippedWeapon = attackerInventory.GetEquippedWithPart<MeleeWeaponPart>();
                if (equippedWeapon != null)
                    weapon = equippedWeapon.GetPart<MeleeWeaponPart>();
            }
            if (weapon == null)
                weapon = attacker.GetPart<MeleeWeaponPart>();

            PerformSingleAttack(attacker, defender, weapon, true, zone, rng);
            return true;
        }

        /// <summary>
        /// Perform a single melee attack with one weapon.
        /// </summary>
        private static void PerformSingleAttack(Entity attacker, Entity defender,
            MeleeWeaponPart weapon, bool isPrimary, Zone zone, Random rng)
        {
            string damageDice = weapon?.BaseDamage ?? "1d2";
            int hitBonus = weapon?.HitBonus ?? 0;
            int penBonus = weapon?.PenBonus ?? 0;
            int maxStrBonus = weapon?.MaxStrengthBonus ?? -1;
            string statName = weapon?.Stat ?? "Strength";

            // Off-hand penalty
            if (!isPrimary)
                hitBonus += OFF_HAND_HIT_PENALTY;

            string attackerName = attacker.GetDisplayName();
            string defenderName = defender.GetDisplayName();

            // Hit roll: 1d20 + AgilityMod + HitBonus vs DV
            int hitRoll = DiceRoller.Roll(20, rng);
            int agilityMod = StatUtils.GetModifier(attacker, "Agility");
            int totalHit = hitRoll + agilityMod + hitBonus;
            int dv = GetDV(defender);

            bool naturalTwenty = hitRoll == 20;

            if (!naturalTwenty && totalHit < dv)
            {
                MessageLog.Add($"{attackerName} misses {defenderName}!");
                return;
            }

            // Penetration
            int strMod = StatUtils.GetModifier(attacker, statName);
            if (maxStrBonus >= 0 && strMod > maxStrBonus)
                strMod = maxStrBonus;
            int pv = strMod + penBonus;
            int av = GetAV(defender);

            int penetrations = RollPenetrations(pv, av, rng);

            if (penetrations == 0)
            {
                MessageLog.Add($"{attackerName} hits {defenderName} but fails to penetrate!");
                return;
            }

            // Damage
            int totalDamage = 0;
            for (int i = 0; i < penetrations; i++)
                totalDamage += DiceRoller.Roll(damageDice, rng);

            if (totalDamage <= 0)
            {
                MessageLog.Add($"{attackerName} hits {defenderName} but deals no damage!");
                return;
            }

            // Apply damage
            ApplyDamage(defender, totalDamage, attacker, zone);

            int remainingHP = defender.GetStatValue("Hitpoints", 0);
            if (remainingHP > 0)
                MessageLog.Add($"{attackerName} hits {defenderName} for {totalDamage} damage! ({remainingHP} HP remaining)");
        }

        /// <summary>
        /// Gather all melee weapons from hand body parts.
        /// Primary hand weapon attacks first, then off-hands.
        /// Also includes natural MeleeWeaponPart on any body part's equipped default.
        /// </summary>
        private static List<WeaponSlot> GatherMeleeWeapons(Entity attacker, Body body)
        {
            var result = new List<WeaponSlot>();
            var parts = body.GetParts();

            // Find weapons in Hand body parts
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Type != "Hand") continue;
                if (part._Equipped == null) continue;
                if (!part.FirstSlotForEquipped) continue;

                var wpn = part._Equipped.GetPart<MeleeWeaponPart>();
                if (wpn != null)
                {
                    result.Add(new WeaponSlot
                    {
                        Weapon = wpn,
                        BodyPart = part,
                        IsPrimary = part.Primary || part.DefaultPrimary
                    });
                }
            }

            // Sort: primary first
            result.Sort((a, b) =>
            {
                if (a.IsPrimary && !b.IsPrimary) return -1;
                if (!a.IsPrimary && b.IsPrimary) return 1;
                return 0;
            });

            // If we found weapons, ensure at least one is marked primary
            if (result.Count > 0 && !result[0].IsPrimary)
                result[0].IsPrimary = true;

            return result;
        }

        /// <summary>
        /// Get defender's Dodge Value.
        /// Body-part-aware: checks all equipped armor across body parts.
        /// </summary>
        public static int GetDV(Entity entity)
        {
            int baseDV = 6;

            // Get best DV bonus from equipped armor
            var body = entity.GetPart<Body>();
            if (body != null)
            {
                int bestDV = 0;
                body.ForeachEquippedObject((item, bp) =>
                {
                    var armor = item.GetPart<ArmorPart>();
                    if (armor != null && armor.DV != 0)
                        bestDV += armor.DV;
                });
                baseDV += bestDV;
            }
            else
            {
                ArmorPart armor = GetEffectiveArmor(entity);
                if (armor != null)
                    baseDV += armor.DV;
            }

            baseDV += StatUtils.GetModifier(entity, "Agility");
            return baseDV;
        }

        /// <summary>
        /// Get defender's Armor Value.
        /// Body-part-aware: sums AV from all equipped armor.
        /// </summary>
        public static int GetAV(Entity entity)
        {
            var body = entity.GetPart<Body>();
            if (body != null)
            {
                int totalAV = 0;
                body.ForeachEquippedObject((item, bp) =>
                {
                    var armor = item.GetPart<ArmorPart>();
                    if (armor != null)
                        totalAV += armor.AV;
                });

                // Also check natural armor on the entity itself
                var naturalArmor = entity.GetPart<ArmorPart>();
                if (naturalArmor != null)
                    totalAV += naturalArmor.AV;

                return totalAV;
            }

            ArmorPart legacyArmor = GetEffectiveArmor(entity);
            return legacyArmor?.AV ?? 0;
        }

        /// <summary>
        /// Get the effective armor for legacy (non-body-part) entities.
        /// </summary>
        private static ArmorPart GetEffectiveArmor(Entity entity)
        {
            var inventory = entity.GetPart<InventoryPart>();
            if (inventory != null)
            {
                var equippedArmor = inventory.GetEquippedWithPart<ArmorPart>();
                if (equippedArmor != null)
                    return equippedArmor.GetPart<ArmorPart>();
            }
            return entity.GetPart<ArmorPart>();
        }

        /// <summary>
        /// Roll penetrations: 3 rolls of 1d8+PV vs AV.
        /// If all 3 succeed, roll again with PV-2 (diminishing returns).
        /// </summary>
        public static int RollPenetrations(int pv, int av, Random rng)
        {
            int penetrations = 0;
            int currentPV = pv;
            int streak = 0;
            int rollsInSet = 3;

            for (int i = 0; i < rollsInSet; i++)
            {
                int roll = DiceRoller.Roll(8, rng) + currentPV;
                if (roll > av)
                {
                    penetrations++;
                    streak++;

                    if (streak == rollsInSet)
                    {
                        currentPV -= 2;
                        streak = 0;
                        rollsInSet = 3;
                        i = -1;
                        if (currentPV + 8 <= av)
                            break;
                    }
                }
            }

            return penetrations;
        }

        /// <summary>
        /// Apply damage to an entity. Fires TakeDamage event, reduces HP,
        /// and checks for death.
        /// </summary>
        public static void ApplyDamage(Entity target, int amount, Entity source, Zone zone)
        {
            if (target == null || amount <= 0) return;

            var takeDamage = GameEvent.New("TakeDamage");
            takeDamage.SetParameter("Target", (object)target);
            takeDamage.SetParameter("Source", (object)source);
            takeDamage.SetParameter("Amount", amount);
            target.FireEvent(takeDamage);

            var hpStat = target.GetStat("Hitpoints");
            if (hpStat != null)
            {
                hpStat.BaseValue -= amount;
            }

            if (target.GetStatValue("Hitpoints", 0) <= 0)
            {
                HandleDeath(target, source, zone);
            }
        }

        /// <summary>
        /// Handle entity death: fire Died event, remove from zone.
        /// </summary>
        public static void HandleDeath(Entity target, Entity killer, Zone zone)
        {
            string targetName = target.GetDisplayName();
            string killerName = killer?.GetDisplayName() ?? "something";
            MessageLog.Add($"{targetName} is killed by {killerName}!");

            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            died.SetParameter("Killer", (object)killer);
            target.FireEvent(died);

            if (zone != null)
                zone.RemoveEntity(target);
        }

        /// <summary>
        /// Tracks a weapon and which body part it's on for multi-weapon combat.
        /// </summary>
        private class WeaponSlot
        {
            public MeleeWeaponPart Weapon;
            public BodyPart BodyPart;
            public bool IsPrimary;
        }
    }
}
