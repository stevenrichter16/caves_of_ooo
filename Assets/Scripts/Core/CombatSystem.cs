using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Resolves melee combat between entities.
    /// Faithful to Qud's melee attack flow:
    /// 1. Fire BeforeMeleeAttack event (can cancel)
    /// 2. Get weapon (MeleeWeaponPart or default fists)
    /// 3. Hit roll: 1d20 + AgilityMod + HitBonus vs DV
    /// 4. Penetration: 3x(1d8 + PV) vs AV, diminishing on streaks
    /// 5. Damage: per penetration, roll weapon.BaseDamage
    /// 6. Apply damage, check death
    /// </summary>
    public static class CombatSystem
    {
        /// <summary>
        /// Perform a melee attack. Returns true if the attack was attempted
        /// (even on miss), false if cancelled by event.
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

            // 2. Get weapon — check equipped weapon first, fallback to natural
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

            string damageDice = weapon?.BaseDamage ?? "1d2";
            int hitBonus = weapon?.HitBonus ?? 0;
            int penBonus = weapon?.PenBonus ?? 0;
            int maxStrBonus = weapon?.MaxStrengthBonus ?? -1;
            string statName = weapon?.Stat ?? "Strength";

            string attackerName = attacker.GetDisplayName();
            string defenderName = defender.GetDisplayName();

            // 3. Hit roll: 1d20 + AgilityMod + HitBonus vs GetDV(defender)
            int hitRoll = DiceRoller.Roll(20, rng);
            int agilityMod = StatUtils.GetModifier(attacker, "Agility");
            int totalHit = hitRoll + agilityMod + hitBonus;
            int dv = GetDV(defender);

            bool naturalTwenty = hitRoll == 20;

            if (!naturalTwenty && totalHit < dv)
            {
                // Miss
                MessageLog.Add($"{attackerName} misses {defenderName}!");
                return true;
            }

            // 4. Penetration
            int strMod = StatUtils.GetModifier(attacker, statName);
            if (maxStrBonus >= 0 && strMod > maxStrBonus)
                strMod = maxStrBonus;
            int pv = strMod + penBonus;
            int av = GetAV(defender);

            int penetrations = RollPenetrations(pv, av, rng);

            if (penetrations == 0)
            {
                MessageLog.Add($"{attackerName} hits {defenderName} but fails to penetrate!");
                return true;
            }

            // 5. Damage: roll weapon dice per penetration
            int totalDamage = 0;
            for (int i = 0; i < penetrations; i++)
                totalDamage += DiceRoller.Roll(damageDice, rng);

            if (totalDamage <= 0)
            {
                MessageLog.Add($"{attackerName} hits {defenderName} but deals no damage!");
                return true;
            }

            // 6. Apply damage
            ApplyDamage(defender, totalDamage, attacker, zone);

            int remainingHP = defender.GetStatValue("Hitpoints", 0);
            if (remainingHP > 0)
                MessageLog.Add($"{attackerName} hits {defenderName} for {totalDamage} damage! ({remainingHP} HP remaining)");

            return true;
        }

        /// <summary>
        /// Get defender's Dodge Value.
        /// Qud formula: base 6 + armor.DV + AgilityMod
        /// Checks equipped armor first, falls back to natural ArmorPart.
        /// </summary>
        public static int GetDV(Entity entity)
        {
            int baseDV = 6;
            ArmorPart armor = GetEffectiveArmor(entity);
            if (armor != null)
                baseDV += armor.DV;
            baseDV += StatUtils.GetModifier(entity, "Agility");
            return baseDV;
        }

        /// <summary>
        /// Get defender's Armor Value.
        /// Checks equipped armor first, falls back to natural ArmorPart.
        /// </summary>
        public static int GetAV(Entity entity)
        {
            ArmorPart armor = GetEffectiveArmor(entity);
            return armor?.AV ?? 0;
        }

        /// <summary>
        /// Get the effective armor: equipped armor item if present, else natural ArmorPart.
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

                    // Diminishing returns: if all rolls in set succeed, add more at PV-2
                    if (streak == rollsInSet)
                    {
                        currentPV -= 2;
                        streak = 0;
                        rollsInSet = 3;
                        i = -1; // restart the loop for the new set
                        if (currentPV + 8 <= av)
                            break; // impossible to penetrate further
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

            // Fire TakeDamage event (could be modified by parts)
            var takeDamage = GameEvent.New("TakeDamage");
            takeDamage.SetParameter("Target", (object)target);
            takeDamage.SetParameter("Source", (object)source);
            takeDamage.SetParameter("Amount", amount);
            target.FireEvent(takeDamage);

            // Reduce HP
            var hpStat = target.GetStat("Hitpoints");
            if (hpStat != null)
            {
                hpStat.BaseValue -= amount;
            }

            // Check death
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

            // Fire Died event
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            died.SetParameter("Killer", (object)killer);
            target.FireEvent(died);

            // Remove from zone
            if (zone != null)
                zone.RemoveEntity(target);
        }
    }
}