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
                string weaponName = weapons[i].Weapon?.ParentEntity?.GetDisplayName()
                    ?? weapons[i].Weapon?.BaseDamage ?? "fist";
                string handName = weapons[i].BodyPart?.GetDisplayName() ?? "hand";
                string sourceDesc = $"[{handName}: {weaponName}]";
                PerformSingleAttack(attacker, defender, weapons[i].Weapon, isPrimary, zone, rng, sourceDesc);
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
            MeleeWeaponPart weapon, bool isPrimary, Zone zone, Random rng,
            string attackSourceDesc = null)
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
            string srcTag = attackSourceDesc != null ? $" {attackSourceDesc}" : "";

            // Hit roll: 1d20 + AgilityMod + HitBonus vs DV
            int hitRoll = DiceRoller.Roll(20, rng);
            int agilityMod = StatUtils.GetModifier(attacker, "Agility");
            int totalHit = hitRoll + agilityMod + hitBonus;
            int dv = GetDV(defender);

            bool naturalTwenty = hitRoll == 20;

            if (!naturalTwenty && totalHit < dv)
            {
                MessageLog.Add($"{attackerName}{srcTag} misses {defenderName}!");
                return;
            }

            // Select hit location on defender
            BodyPart hitPart = null;
            Body defenderBody = defender.GetPart<Body>();
            if (defenderBody != null)
                hitPart = SelectHitLocation(defenderBody, rng);

            string partDesc = hitPart != null ? $" in the {hitPart.GetDisplayName()}" : "";

            // Penetration — per-part AV when hit location is known
            int strMod = StatUtils.GetModifier(attacker, statName);
            if (maxStrBonus >= 0 && strMod > maxStrBonus)
                strMod = maxStrBonus;
            int pv = strMod + penBonus;
            int av = hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender);

            int penetrations = RollPenetrations(pv, av, rng);

            if (penetrations == 0)
            {
                MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} but fails to penetrate!");
                return;
            }

            // Damage
            int totalDamage = 0;
            for (int i = 0; i < penetrations; i++)
                totalDamage += DiceRoller.Roll(damageDice, rng);

            if (totalDamage <= 0)
            {
                MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} but deals no damage!");
                return;
            }

            // Log the hit before applying damage so the killing blow details are visible
            int hpBefore = defender.GetStatValue("Hitpoints", 0);
            int hpAfter = hpBefore - totalDamage;

            MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} for {totalDamage} damage!{(hpAfter > 0 ? $" ({hpAfter} HP remaining)" : "")}");

            // Apply damage
            ApplyDamage(defender, totalDamage, attacker, zone);

            if (hpAfter > 0)
            {
                // Check for combat dismemberment (only on survivors)
                if (hitPart != null)
                    CheckCombatDismemberment(defender, defenderBody, hitPart, totalDamage, zone, rng);
            }
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

            // Find weapons in Hand body parts.
            // Equipped weapons take priority; fall back to DefaultBehavior (natural weapons).
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Type != "Hand") continue;

                // Check equipped weapon first
                if (part._Equipped != null && part.FirstSlotForEquipped)
                {
                    var wpn = part._Equipped.GetPart<MeleeWeaponPart>();
                    if (wpn != null)
                    {
                        result.Add(new WeaponSlot
                        {
                            Weapon = wpn,
                            BodyPart = part,
                            IsPrimary = part.Primary || part.DefaultPrimary
                        });
                        continue;
                    }
                }

                // Fall back to default behavior (natural weapon)
                if (part._DefaultBehavior != null && part.FirstSlotForDefaultBehavior)
                {
                    var wpn = part._DefaultBehavior.GetPart<MeleeWeaponPart>();
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
        /// Handle entity death: drop loot, fire Died event, remove from zone.
        /// Mirrors Qud's BeforeDeathRemovalEvent: equipment and inventory drop
        /// to the ground before the entity is removed.
        /// </summary>
        public static void HandleDeath(Entity target, Entity killer, Zone zone)
        {
            string targetName = target.GetDisplayName();
            string killerName = killer?.GetDisplayName() ?? "something";
            MessageLog.Add($"{targetName} is killed by {killerName}!");

            // Drop equipment from body parts
            if (zone != null)
            {
                var body = target.GetPart<Body>();
                if (body != null)
                    body.DropAllEquipment(zone);

                // Drop carried inventory
                var inventory = target.GetPart<InventoryPart>();
                if (inventory != null)
                    DropInventoryOnDeath(target, inventory, zone);
            }

            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            died.SetParameter("Killer", (object)killer);
            target.FireEvent(died);

            if (zone != null)
                zone.RemoveEntity(target);
        }

        /// <summary>
        /// Drop all carried and equipped inventory items to the ground.
        /// Handles both body-part-aware and legacy equip modes.
        /// </summary>
        private static void DropInventoryOnDeath(Entity target, InventoryPart inventory, Zone zone)
        {
            var pos = zone.GetEntityPosition(target);
            if (pos.x < 0 || pos.y < 0) return;

            // Drop any remaining equipped items (legacy equip mode, or items
            // not on body parts). Body.DropAllEquipment already handles body-part items.
            var equipped = inventory.GetAllEquipped();
            for (int i = 0; i < equipped.Count; i++)
            {
                var item = equipped[i];
                var physics = item.GetPart<PhysicsPart>();
                if (physics != null)
                {
                    physics.Equipped = null;
                    physics.InInventory = null;
                }
                zone.AddEntity(item, pos.x, pos.y);
            }
            inventory.EquippedItems.Clear();

            // Drop carried items
            var items = new List<Entity>(inventory.Objects);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                inventory.RemoveObject(item);
                zone.AddEntity(item, pos.x, pos.y);
            }
        }

        // --- Body Part Targeting ---

        /// <summary>
        /// Damage must exceed this fraction of max HP to have any dismemberment chance.
        /// </summary>
        public const float DISMEMBER_DAMAGE_THRESHOLD = 0.25f;

        /// <summary>
        /// Base percentage chance for dismemberment when threshold is met.
        /// </summary>
        public const int DISMEMBER_BASE_CHANCE = 5;

        /// <summary>
        /// Select a random body part to be hit, weighted by TargetWeight.
        /// Excludes abstract parts and parts with TargetWeight &lt;= 0.
        /// Returns null if no valid targets (fallback to global AV).
        /// </summary>
        public static BodyPart SelectHitLocation(Body body, Random rng)
        {
            var parts = body.GetParts();
            int totalWeight = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Abstract) continue;
                if (parts[i].TargetWeight <= 0) continue;
                totalWeight += parts[i].TargetWeight;
            }

            if (totalWeight <= 0) return null;

            int roll = rng.Next(totalWeight);
            int cumulative = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Abstract) continue;
                if (parts[i].TargetWeight <= 0) continue;
                cumulative += parts[i].TargetWeight;
                if (roll < cumulative)
                    return parts[i];
            }

            return null;
        }

        /// <summary>
        /// Get Armor Value for a specific body part hit.
        /// Checks armor equipped on that body part plus the entity's natural armor.
        /// </summary>
        public static int GetPartAV(Entity entity, BodyPart hitPart)
        {
            int av = 0;

            if (hitPart._Equipped != null)
            {
                var armor = hitPart._Equipped.GetPart<ArmorPart>();
                if (armor != null)
                    av += armor.AV;
            }

            // Natural armor always applies
            var naturalArmor = entity.GetPart<ArmorPart>();
            if (naturalArmor != null)
                av += naturalArmor.AV;

            return av;
        }

        /// <summary>
        /// Check if a combat hit should sever the struck body part.
        /// Only triggers on severable appendages. Chance scales with damage.
        /// </summary>
        private static void CheckCombatDismemberment(Entity defender, Body body,
            BodyPart hitPart, int damage, Zone zone, Random rng)
        {
            if (!hitPart.IsSeverable()) return;

            float threshold = DISMEMBER_DAMAGE_THRESHOLD;
            if (hitPart.Mortal)
                threshold *= 2.0f;

            int maxHP = defender.GetStat("Hitpoints")?.Max ?? 1;
            float damageRatio = (float)damage / maxHP;
            if (damageRatio < threshold) return;

            float excessRatio = damageRatio - threshold;
            int chance = DISMEMBER_BASE_CHANCE + (int)(excessRatio * 50);
            chance = Math.Min(chance, 50);

            int roll = rng.Next(100);
            if (roll < chance)
            {
                body.Dismember(hitPart, zone);
            }
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
