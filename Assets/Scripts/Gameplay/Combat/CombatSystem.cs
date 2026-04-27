using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;

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
            using (PerformanceMarkers.Combat.PerformMeleeAttack.Auto())
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

                // Miss indicator: brief gray dash at defender position
                Cell defenderCell = zone.GetEntityCell(defender);
                if (defenderCell != null)
                    AsciiFxBus.EmitParticle(zone, defenderCell.X, defenderCell.Y, '-', "&y", 0.15f);
                return;
            }

            // Select hit location on defender
            BodyPart hitPart = null;
            Body defenderBody = defender.GetPart<Body>();
            if (defenderBody != null)
                hitPart = SelectHitLocation(defenderBody, rng);

            string partDesc = hitPart != null ? $" in the {hitPart.GetDisplayName()}" : "";

            // Penetration — per-part AV when hit location is known.
            // The MaxBonus cap is now applied INSIDE RollPenetrations (Qud-parity).
            // Legacy weapons with MaxStrengthBonus = -1 (uncapped sentinel) get a
            // sane large value to avoid integer overflow in the bonus-decay loop.
            int strMod = StatUtils.GetModifier(attacker, statName);
            int bonus = strMod + penBonus;
            int effectiveMaxStrBonus = (maxStrBonus < 0) ? 50 : maxStrBonus;
            int maxBonus = effectiveMaxStrBonus + penBonus;
            int av = hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender);

            int penetrations = RollPenetrations(av, bonus, maxBonus, rng);

            if (penetrations == 0)
            {
                MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} but fails to penetrate!");
                return;
            }

            // Damage — build a typed Damage with attributes from the weapon
            // (Phase C of the Qud-parity port). Attributes flow through ApplyDamage
            // and are observable by listeners (Phase E will use them for resistances).
            Damage damage = new Damage(0);
            damage.AddAttribute("Melee");
            damage.AddAttribute(statName);                      // e.g. "Strength"
            if (weapon != null && !string.IsNullOrEmpty(weapon.Attributes))
                damage.AddAttributes(weapon.Attributes);        // e.g. "Cutting LongBlades"

            int totalDamage = 0;
            for (int i = 0; i < penetrations; i++)
                totalDamage += DiceRoller.Roll(damageDice, rng);
            damage.Amount = totalDamage;

            if (damage.Amount <= 0)
            {
                MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} but deals no damage!");
                return;
            }

            // Log the hit before applying damage so the killing blow details are visible
            int hpBefore = defender.GetStatValue("Hitpoints", 0);
            int hpAfter = hpBefore - damage.Amount;

            MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} for {damage.Amount} damage!{(hpAfter > 0 ? $" ({hpAfter} HP remaining)" : "")}");

            // Apply damage (typed overload — preferred path)
            ApplyDamage(defender, damage, attacker, zone);

            // Floating damage number
            Cell hitCell = zone.GetEntityCell(defender);
            if (hitCell != null)
                AsciiFxBus.EmitFloatingNumber(zone, hitCell.X, hitCell.Y, damage.Amount, "&R");

            if (hpAfter > 0)
            {
                // Check for combat dismemberment (only on survivors)
                if (hitPart != null)
                    CheckCombatDismemberment(defender, defenderBody, hitPart, damage.Amount, zone, rng);
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
        /// Roll penetrations using Qud's algorithm
        /// (mirrors <c>XRL.Rules.Stat.RollDamagePenetrations</c>, lines 160-203).
        ///
        /// Per set of 3 rolls of (<c>1d10 − 2</c>, exploding on raw 10):
        ///   • Count successes (rolls with total &gt; <paramref name="targetInclusive"/>).
        ///   • A set with ≥1 success awards EXACTLY 1 penetration (not per-roll).
        ///   • Continue rolling new sets only if all 3 rolls in the set succeeded.
        ///   • Bonus decays by 2 every set, regardless of success count.
        ///
        /// Effective per-roll bonus: <c>Math.Min(bonus, maxBonus)</c>.
        ///
        /// See <c>Docs/COMBAT-QUD-PARITY-PORT.md</c> Phase A for the parity rationale.
        /// </summary>
        /// <param name="targetInclusive">AV (Armor Value). Rolls must STRICTLY exceed this.</param>
        /// <param name="bonus">Total pen bonus (e.g., StatMod + weapon PenBonus + crit bonus).</param>
        /// <param name="maxBonus">Cap on the bonus contribution per roll (mirrors weapon's MaxStrengthBonus).</param>
        /// <param name="rng">Seeded RNG for replay safety in tests.</param>
        public static int RollPenetrations(int targetInclusive, int bonus, int maxBonus, Random rng)
        {
            int totalPens = 0;
            int successesInSet = 3; // sentinel — enter the loop

            while (successesInSet == 3)
            {
                successesInSet = 0;
                for (int i = 0; i < 3; i++)
                {
                    // 1d10 − 2 (range −1 to 8). Raw 10 (post-mod 8) explodes:
                    // adds +8 to the running total and re-rolls. Explosions chain.
                    int rawRoll = DiceRoller.Roll(10, rng) - 2;
                    int explodeAccum = 0;
                    while (rawRoll == 8)
                    {
                        explodeAccum += 8;
                        rawRoll = DiceRoller.Roll(10, rng) - 2;
                    }
                    int dieResult = explodeAccum + rawRoll;
                    int totalRoll = dieResult + Math.Min(bonus, maxBonus);
                    if (totalRoll > targetInclusive)
                        successesInSet++;
                }
                if (successesInSet >= 1)
                    totalPens++;
                bonus -= 2;
            }

            return totalPens;
        }

        /// <summary>
        /// Apply typed damage to an entity. Fires TakeDamage / DamageDealt events
        /// (now carrying both <c>Amount</c> int and <c>Damage</c> object parameters),
        /// reduces HP, and checks for death.
        ///
        /// This is the PRIMARY overload; the legacy int overload below wraps it.
        /// Phase E will use <paramref name="damage"/>.Attributes here to apply
        /// resistance stats before the HP decrement.
        /// </summary>
        public static void ApplyDamage(Entity target, Damage damage, Entity source, Zone zone)
        {
            using (PerformanceMarkers.Combat.ApplyDamage.Auto())
            {
                if (target == null || damage == null || damage.Amount <= 0) return;

                // Two guards rolled into one: targets without a Hitpoints
                // stat aren't damageable creatures (statues, props), and
                // targets whose Hitpoints stat is already ≤ 0 are already
                // dead — re-running the damage flow on them would re-fire
                // HandleDeath, which is NOT idempotent. Caught by the
                // adversarial cold-eye pass:
                //   ApplyDamage_NoHitpointsStat_DoesNotCrashOrKill
                //   ApplyDamage_OnAlreadyDeadTarget_DoesNotReFireDeath
                //
                // The double-HandleDeath consequences are concrete and
                // exploitable: duplicate XP award, duplicate equipment +
                // inventory drops (item duplication), duplicate Died event
                // → potential double corpse spawn, duplicate "killed"
                // message, duplicate witness broadcast. Worse than M6
                // CR-01 because it isn't gated on co-location — any
                // second damage call on a dying target trips it.
                var hpStat = target.GetStat("Hitpoints");
                if (hpStat == null || hpStat.BaseValue <= 0) return;

                int amount = damage.Amount;

                var takeDamage = GameEvent.New("TakeDamage");
                takeDamage.SetParameter("Target", (object)target);
                takeDamage.SetParameter("Source", (object)source);
                takeDamage.SetParameter("Amount", amount);
                takeDamage.SetParameter("Damage", (object)damage);     // Phase C: typed damage available to listeners
                target.FireEvent(takeDamage);

                hpStat.BaseValue -= amount;

                Stat hpAlias = target.GetStat("HP");
                if (hpAlias != null && !ReferenceEquals(hpAlias, hpStat))
                    hpAlias.BaseValue -= amount;

                // Notify the attacker that damage was dealt (for on-hit effects like poison)
                if (source != null)
                {
                    var damageDealt = GameEvent.New("DamageDealt");
                    damageDealt.SetParameter("Attacker", (object)source);
                    damageDealt.SetParameter("Defender", (object)target);
                    damageDealt.SetParameter("Amount", amount);
                    damageDealt.SetParameter("Damage", (object)damage); // Phase C
                    source.FireEvent(damageDealt);
                }

                if (hpStat.BaseValue <= 0)
                {
                    HandleDeath(target, source, zone);
                }
            }
        }

        /// <summary>
        /// Backward-compatible int overload. Wraps <paramref name="amount"/> in
        /// a <see cref="Damage"/> with no attributes and forwards to the typed
        /// overload. Existing call sites (status effects, mutations, traps, etc.)
        /// continue to work; new code should use the typed overload directly so
        /// damage attributes propagate.
        /// </summary>
        public static void ApplyDamage(Entity target, int amount, Entity source, Zone zone)
        {
            ApplyDamage(target, new Damage(amount), source, zone);
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

            // Award XP to the killer
            if (killer != null && killer.HasTag("Player"))
                LevelingSystem.AwardKillXP(killer, target, zone);

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

            // Death splatter FX (before entity removal so position is still valid)
            DeathSplatterFx.Emit(target, killer, zone);

            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            died.SetParameter("Killer", (object)killer);
            // Zone is threaded through so handlers that spawn entities at the
            // death cell (M5: CorpsePart) don't need to couple to BrainPart to
            // resolve the zone. Non-spawner handlers (StatusEffectsPart,
            // GivesRepPart) ignore the extra parameter.
            died.SetParameter("Zone", (object)zone);
            target.FireEvent(died);

            // M2.3: broadcast the death to nearby Passive NPCs so they can
            // visibly react (wander-pace for 20 turns). Fires AFTER the Died
            // event so any handlers that mutate the zone have already run,
            // and BEFORE RemoveEntity so the death cell is still resolvable
            // via GetEntityCell(target).
            if (zone != null)
                BroadcastDeathWitnessed(target, killer, zone, WitnessRadius);

            if (zone != null)
                zone.RemoveEntity(target);
        }

        /// <summary>Chebyshev radius for death-witness broadcast (M2.3).</summary>
        private const int WitnessRadius = 8;

        /// <summary>
        /// Apply a <see cref="WitnessedEffect"/> to every Passive, Creature-tagged
        /// entity in the zone that is within <paramref name="radius"/> Chebyshev
        /// cells of the death location AND has line-of-sight to it.
        /// Consumes M1.2's <see cref="BrainPart.Passive"/> flag as the filter.
        /// </summary>
        private static void BroadcastDeathWitnessed(Entity deceased, Entity killer, Zone zone, int radius)
        {
            if (zone == null || deceased == null) return;
            var deathCell = zone.GetEntityCell(deceased);
            if (deathCell == null) return;

            // Snapshot via GetAllEntities (already allocates) — do NOT iterate
            // GetReadOnlyEntities() directly because ApplyEffect below can run
            // side-effect chains that mutate _entityCells, invalidating the
            // live Dictionary.KeyCollection enumerator.
            var all = zone.GetAllEntities();
            for (int i = 0; i < all.Count; i++)
            {
                var witness = all[i];
                if (witness == deceased) continue;
                if (witness == killer) continue;                 // null killer (env death) is fine here
                if (!witness.HasTag("Creature")) continue;

                var brain = witness.GetPart<BrainPart>();
                if (brain == null || !brain.Passive) continue;   // only Passive NPCs witness

                var wCell = zone.GetEntityCell(witness);
                if (wCell == null) continue;

                int dist = AIHelpers.ChebyshevDistance(deathCell.X, deathCell.Y, wCell.X, wCell.Y);
                if (dist > radius) continue;

                // HasLineOfSight is symmetric (Bresenham on IsSolid cells).
                // Witness → death cell is the semantically natural order.
                if (!AIHelpers.HasLineOfSight(zone, wCell.X, wCell.Y, deathCell.X, deathCell.Y))
                    continue;

                witness.ApplyEffect(new WitnessedEffect(duration: 20));
            }
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
