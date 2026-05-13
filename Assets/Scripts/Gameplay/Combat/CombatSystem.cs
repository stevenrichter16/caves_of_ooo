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
        /// Compute the hit-bonus adjustment applied to an off-hand (non-primary)
        /// melee swing for the given attacker. Mirrors Qud's pattern of letting
        /// skills modify per-weapon attack chance via <c>GetMeleeAttackChanceEvent</c>
        /// (Combat.cs:775); we approximate with a stat-driven hook since we don't
        /// have a skill system yet.
        ///
        /// Returns <see cref="OFF_HAND_HIT_PENALTY"/> + the attacker's
        /// <c>MultiWeaponSkillBonus</c> stat (default 0). A future skill system
        /// would set this stat per-skill-rank; equipment passives could also
        /// modify it via stat shifts.
        /// </summary>
        public static int GetOffHandHitBonus(Entity attacker)
        {
            if (attacker == null) return OFF_HAND_HIT_PENALTY;
            return OFF_HAND_HIT_PENALTY + attacker.GetStatValue("MultiWeaponSkillBonus", 0);
        }

        /// <summary>
        /// Marker substring inserted into the <see cref="MessageLog"/> hit line
        /// when a melee attack lands as a critical (nat-20). Tests pin this
        /// constant rather than the exact wording so future polish (color
        /// codes, particle FX) can change the line without breaking tests.
        /// </summary>
        public const string CRITICAL_HIT_TAG = "CRITICAL";

        /// <summary>
        /// Sentinel-substitute used when a weapon's <c>MaxStrengthBonus</c> is set to
        /// <c>-1</c> (legacy "uncapped" sentinel from pre-Phase-A code). Mapped to a
        /// large-but-not-overflow value so the bonus-decay loop in <see cref="RollPenetrations"/>
        /// terminates in bounded time. Qud weapons always have a real positive
        /// MaxStrengthBonus; CoO will migrate to that pattern in Phase B½. See
        /// <c>Docs/COMBAT-QUD-PARITY-PORT.md</c> Phase A divergence #1.
        /// </summary>
        public const int LEGACY_UNCAPPED_MAX_STR_BONUS = 50;

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
                if (!attacker.FireEventAndRelease(beforeAttack))
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
        public static void PerformSingleAttack(Entity attacker, Entity defender,
            MeleeWeaponPart weapon, bool isPrimary, Zone zone, Random rng,
            string attackSourceDesc = null)
        {
            string damageDice = weapon?.BaseDamage ?? "1d2";
            int hitBonus = weapon?.HitBonus ?? 0;
            int penBonus = weapon?.PenBonus ?? 0;
            int maxStrBonus = weapon?.MaxStrengthBonus ?? -1;
            string statName = weapon?.Stat ?? "Strength";

            // Off-hand penalty (Phase G: stat-modulated via GetOffHandHitBonus)
            if (!isPrimary)
                hitBonus += GetOffHandHitBonus(attacker);

            string attackerName = attacker.GetDisplayName();
            string defenderName = defender.GetDisplayName();
            string srcTag = attackSourceDesc != null ? $" {attackSourceDesc}" : "";

            // Hit roll: 1d20 + AgilityMod + HitBonus + SkillBonus vs DV
            // WSP3.2: Skill-driven hit modifier. Sums OnGetToHitModifier
            // across all of attacker's owned skills (Expertise contributes
            // here in WSP3.4). Default 0 for skills that don't override.
            int hitRoll = DiceRoller.Roll(20, rng);
            int agilityMod = StatUtils.GetModifier(attacker, "Agility");
            int skillHitBonus = CavesOfOoo.Skills.SkillEventDispatcher
                .GetSkillHitModifier(attacker, weapon);
            int totalHit = hitRoll + agilityMod + hitBonus + skillHitBonus;
            int dv = GetDV(defender);

            bool naturalTwenty = hitRoll == 20;
            bool willMiss = !naturalTwenty && totalHit < dv;

            // HitRoll diag emission — surfaces the d20 + each bonus
            // summand so observability can answer "why did this attack
            // miss?" or "did my Agility help this connect?"
            if (Diag.IsChannelEnabled("damage"))
            {
                string weaponNameHit = weapon?.ParentEntity?.GetDisplayName() ?? "(natural)";
                Diag.Record(
                    category: "damage",
                    kind: "HitRoll",
                    actor: attacker,
                    target: defender,
                    payload: new
                    {
                        weapon = weaponNameHit,
                        hitRoll = hitRoll,
                        agilityMod = agilityMod,
                        weaponHitBonus = hitBonus,
                        skillHitBonus = skillHitBonus,
                        totalHit = totalHit,
                        dv = dv,
                        naturalTwenty = naturalTwenty,
                        landed = !willMiss
                    });
            }

            if (willMiss)
            {
                MessageLog.Add($"{attackerName}{srcTag} misses {defenderName}!");

                // Miss indicator: brief gray dash at defender position
                Cell defenderCell = zone.GetEntityCell(defender);
                if (defenderCell != null)
                    AsciiFxBus.EmitParticle(zone, defenderCell.X, defenderCell.Y, '-', "&y", 0.15f);

                // WSP3.2: fire miss-side skill events. Backswing
                // (attacker-side) and Rejoinder (defender-side) override
                // the matching virtual.
                var missCtx = new CavesOfOoo.Skills.SkillEventContext
                {
                    Attacker = attacker, Defender = defender,
                    Weapon = weapon, WeaponEntity = weapon?.ParentEntity,
                    Damage = null, ActualDamage = 0,
                    Zone = zone, Rng = rng,
                };
                CavesOfOoo.Skills.SkillEventDispatcher
                    .AttackerMeleeMiss(attacker, missCtx);
                CavesOfOoo.Skills.SkillEventDispatcher
                    .DefenderAfterAttackMissed(defender, missCtx);
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
            // WSP6.6 — Skill pen-bonus hook. Mirrors the to-hit hook
            // (line 172). Sums OnGetPenetrationModifier across all owned
            // skills; ShortBlades_Puncture returns +2 here when the
            // wielded weapon has the Piercing attribute.
            int skillPenBonus = CavesOfOoo.Skills.SkillEventDispatcher
                .GetSkillPenetrationModifier(attacker, weapon);
            int bonus = strMod + penBonus + skillPenBonus;
            int effectiveMaxStrBonus = (maxStrBonus < 0) ? LEGACY_UNCAPPED_MAX_STR_BONUS : maxStrBonus;
            int maxBonus = effectiveMaxStrBonus + penBonus + skillPenBonus;
            int av = hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender);

            // Phase D: critical hits (nat-20). Mirror Qud's Combat.cs:1106-1140 —
            // crit adds +1 to PenBonus and +1 to PenCapBonus, and sets the AutoPen
            // flag (which forces pens = 1 if rolls fail AND the attacker is the player).
            // We skip Qud's skill-based weaponCriticalModifier and the WeaponCriticalModifier
            // event chain for now (those land in later phases when skills exist).
            int critPenBonus = naturalTwenty ? 1 : 0;
            int critMaxBonus = naturalTwenty ? 1 : 0;
            bool autoPen = naturalTwenty;

            int penetrations = RollPenetrations(av, bonus + critPenBonus, maxBonus + critMaxBonus, rng);
            int penetrationsBeforeAutoPen = penetrations;

            // AutoPen: if penetration failed AND we're a critical AND attacker is the player,
            // force one penetration through. Mirrors Qud's `flag5 && Attacker.IsPlayer()` guard.
            if (penetrations == 0 && autoPen && attacker.HasTag("Player"))
                penetrations = 1;

            // Penetration diag emission — surfaces the per-attack roll
            // breakdown so observability tools can answer "did Sharp's
            // +1 PenBonus contribute on this hit?" The payload exposes
            // each summand independently (weapon PenBonus, strength
            // modifier, skill bonus, crit bonus) plus the final
            // penetration count + AutoPen-fired flag. Emitted per
            // attack, not per die roll — the granularity matches what
            // a debug query would ask for.
            if (Diag.IsChannelEnabled("damage"))
            {
                string weaponName = weapon?.ParentEntity?.GetDisplayName() ?? "(natural)";
                Diag.Record(
                    category: "damage",
                    kind: "Penetration",
                    actor: attacker,
                    target: defender,
                    payload: new
                    {
                        weapon = weaponName,
                        av = av,
                        weaponPenBonus = penBonus,
                        strMod = strMod,
                        skillPenBonus = skillPenBonus,
                        critPenBonus = critPenBonus,
                        totalBonus = bonus + critPenBonus,
                        maxBonus = maxBonus + critMaxBonus,
                        naturalTwenty = naturalTwenty,
                        penetrations = penetrations,
                        autoPenForced = (penetrationsBeforeAutoPen == 0 && penetrations == 1 && autoPen)
                    });
            }

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
            if (naturalTwenty)
                damage.AddAttribute("Critical");                // Phase D: crit attribute

            int totalDamage = 0;
            for (int i = 0; i < penetrations; i++)
                totalDamage += DiceRoller.Roll(damageDice, rng);
            damage.Amount = totalDamage;

            // DamageRoll diag emission — surfaces the base damage roll
            // (pre-resistance, pre-BeforeTakeDamage-mutation). The
            // delta between this and the final DamageDealt's amount
            // reveals exactly how much resistance + Stoneskin + other
            // mutators reduced the damage in-flight.
            if (Diag.IsChannelEnabled("damage"))
            {
                string weaponNameDmg = weapon?.ParentEntity?.GetDisplayName() ?? "(natural)";
                Diag.Record(
                    category: "damage",
                    kind: "DamageRoll",
                    actor: attacker,
                    target: defender,
                    payload: new
                    {
                        weapon = weaponNameDmg,
                        damageDice = damageDice,
                        penetrationsRolled = penetrations,
                        baseDamageTotal = totalDamage,
                        naturalTwenty = naturalTwenty,
                        attributes = string.Join(",", damage.Attributes ?? new System.Collections.Generic.List<string>())
                    });
            }

            if (damage.Amount <= 0)
            {
                MessageLog.Add($"{attackerName}{srcTag} hits {defenderName}{partDesc} but deals no damage!");
                return;
            }

            // Capture pre-damage HP. Phase F BeforeTakeDamage listeners
            // (Stoneskin, etc.), Phase E elemental resistance, and the
            // TakeDamage event can all mutate damage.Amount in-flight inside
            // ApplyDamage — so the actual landed amount is the HP delta,
            // NOT the pre-call damage.Amount we computed from dice rolls.
            //
            // Pre-fix this block emitted the hit log BEFORE ApplyDamage and
            // used `damage.Amount` (raw) and `hpBefore - damage.Amount`
            // (also raw). On a heat-resistant target hit by a Fire-tagged
            // weapon, the log claimed e.g. "12 damage / 188 HP remaining"
            // while the actual entity lost only 6 HP and sat at 194/200 —
            // the HP bar told the truth, the log lied.
            int hpBefore = defender.GetStatValue("Hitpoints", 0);

            // Apply damage (typed overload — preferred path). Internally:
            //   Phase F BeforeTakeDamage event (listeners can veto/mutate)
            //   → Phase E ApplyResistances (HeatResistance etc.)
            //   → TakeDamage event (more in-flight mutations possible)
            //   → HP decrement (clamped to ≥ 0)
            //   → HandleDeath if HP <= 0 (which logs "X is killed by Y!").
            ApplyDamage(defender, damage, attacker, zone);

            int hpAfter = defender.GetStatValue("Hitpoints", 0);
            int actualDamage = System.Math.Max(0, hpBefore - hpAfter);

            // Tier 2.1: nat-20 hits include the CRITICAL_HIT_TAG so the player
            // sees the crit happen, not just the silent "Critical" attribute on
            // Damage. Note: on lethal blows the kill announcement (from
            // HandleDeath inside ApplyDamage) precedes this hit line, so the
            // order reads "X is killed by Y!" then "Y hits X for N damage!".
            // The hit line still uses post-resistance actualDamage so the
            // number is honest.
            string hitVerb = naturalTwenty ? $"{CRITICAL_HIT_TAG}LY hits" : "hits";
            MessageLog.Add($"{attackerName}{srcTag} {hitVerb} {defenderName}{partDesc} for {actualDamage} damage!{(hpAfter > 0 ? $" ({hpAfter} HP remaining)" : "")}");

            // Pass 4 §4A: hit-stop on big moments. Brief Time.timeScale=0
            // freeze + camera-shake combo so kills and crits feel weighty.
            // See Docs/GRAPHICS-PASS4.md §4A. The Light tier (every-hit
            // freeze) is deliberately NOT wired — adds up to 80ms × every
            // melee swing which becomes annoying. Medium fires on crits
            // (~150ms), Heavy on kills (~250ms). Tested in
            // HitStopControllerTests; integration via GameBootstrap.
            var hitStop = CavesOfOoo.Presentation.Effects.HitStopController.Instance;
            if (hitStop != null)
            {
                if (hpAfter <= 0)
                    hitStop.PunchHeavy();
                else if (naturalTwenty)
                    hitStop.PunchMedium();
            }

            // Floating damage number now emitted from inside ApplyDamage
            // (just above this method's call to ApplyDamage), so every damage
            // path — including traps, effect ticks, and any future content
            // that calls ApplyDamage directly — gets the same visual feedback.
            // Removed the per-attack emit here to avoid double-numbering on
            // melee swings; ApplyDamage uses min(amount, hpBefore) which
            // matches the actualDamage value this code previously computed
            // for melee, so the on-screen number is identical.

            if (hpAfter > 0)
            {
                // On-hit class effects (Tier-2: Bludgeoning→Stunned, Cutting→Bleeding,
                // Piercing→Confused). Reads damage attributes; rolls per-class
                // probabilities; applies via target.ApplyEffect. Only fires on
                // survivors — corpses don't bleed or get stunned.
                OnHitClassEffects.Apply(damage, actualDamage, defender, attacker, zone, rng);

                // Per-weapon on-hit overrides (FlamingSword→Burning, IceSword→Frozen, etc.).
                // Stacks ON TOP of class effects: a Bludgeoning ThunderHammer can both
                // stun AND electrify on the same hit, since the chance rolls are independent.
                OnHitWeaponEffects.Apply(weapon, damage, actualDamage, defender, attacker, zone, rng);

                // Item-enhancement on-hit hook (E.2.1). Iterates the weapon Entity's
                // IItemEnhancement Parts (e.g. EnhancementSerrated → on-hit bleed)
                // and calls each one's OnAttackerHit. Parallel to OnHitWeaponEffects.Apply
                // above but distinct: per-weapon effects are blueprint-declared
                // (OnHitEffectsRaw), enhancements are player-applied at runtime via
                // ItemEnhancing.Apply. Mirrors Qud's IMeleeModification dispatch from
                // XRL.World.Combat.MeleeAttack.
                ItemEnhancementDispatch.DispatchOnHit(
                    weapon?.ParentEntity, defender, attacker, damage, actualDamage, zone, rng);

                // Skill-driven on-hit effects (Cudgel_Bludgeon→Stun,
                // LongBlades_Lacerate→Bleed, etc.). WSP3.3 — the previous
                // OnHitSkillEffects.Apply central switch was deleted; each
                // skill now overrides BaseSkillPart virtuals
                // (OnAttackerAfterAttack / OnWeaponMadeCriticalHit) and
                // SkillEventDispatcher routes the events to all owned skills.
                // Tree-root WeaponMadeCriticalHit fires only on Critical hits.
                var hitCtx = new CavesOfOoo.Skills.SkillEventContext
                {
                    Attacker = attacker, Defender = defender,
                    Weapon = weapon, WeaponEntity = weapon?.ParentEntity,
                    Damage = damage, ActualDamage = actualDamage,
                    Zone = zone, Rng = rng,
                };
                CavesOfOoo.Skills.SkillEventDispatcher
                    .AttackerAfterAttack(attacker, hitCtx);
                if (damage.HasAttribute("Critical"))
                {
                    CavesOfOoo.Skills.SkillEventDispatcher
                        .WeaponMadeCriticalHit(attacker, hitCtx);
                }

                // Check for combat dismemberment (only on survivors). Use
                // actualDamage because the threshold formula is (damage / maxHP)
                // — pre-resistance values would over-trigger dismemberment on
                // heat-resistant creatures hit by Fire-tagged weapons (the
                // Glowmaw shouldn't lose a limb to half-absorbed Fire damage).
                if (hitPart != null)
                    CheckCombatDismemberment(defender, defenderBody, hitPart, actualDamage, zone, rng);
            }
        }

        /// <summary>
        /// Gather all melee weapons from hand body parts.
        /// Primary hand weapon attacks first, then off-hands.
        /// Also includes natural MeleeWeaponPart on any body part's equipped default.
        ///
        /// <para><b>Allocation note (Tier-B Fix #3 in PERF-COMBAT-INVESTIGATION.md).</b>
        /// Reuses a static scratch list to avoid allocating a fresh
        /// <c>List&lt;WeaponSlot&gt;</c> per attack. Safe because combat is
        /// turn-serial: one <see cref="PerformBodyPartAwareAttack"/> call
        /// completes before the next begins. Caller must consume the
        /// returned list before any nested combat call (none today). If a
        /// future feature triggers nested melee resolution from inside an
        /// attack, switch this to ArrayPool or gate re-entrance explicitly
        /// (mirrors the same pattern used by
        /// <c>MovementSystem._enteredCellScratch</c>).</para>
        /// </summary>
        private static readonly List<WeaponSlot> _gatherWeaponsScratch = new List<WeaponSlot>(8);

        private static List<WeaponSlot> GatherMeleeWeapons(Entity attacker, Body body)
        {
            var result = _gatherWeaponsScratch;
            result.Clear();
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
            int totalAV;
            var body = entity.GetPart<Body>();
            if (body != null)
            {
                totalAV = 0;
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
            }
            else
            {
                ArmorPart legacyArmor = GetEffectiveArmor(entity);
                totalAV = legacyArmor?.AV ?? 0;
            }

            // WSP2.1: ShatterArmorEffect subtracts AV per stack while active.
            // Each stack contributes ShatterArmorEffect.AV_REDUCTION to the
            // total reduction (Cudgel_ShatteringBlows applies one stack per proc).
            // Clamp to non-negative — armor can be reduced to zero but not below.
            var statusEffects = entity.GetPart<StatusEffectsPart>();
            if (statusEffects != null)
            {
                var shatter = statusEffects.GetEffect<ShatterArmorEffect>();
                if (shatter != null)
                    totalAV -= ShatterArmorEffect.AV_REDUCTION * shatter.StackCount;
            }
            return System.Math.Max(0, totalAV);
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
                // Capture pre-decrement HP so the floating number we emit
                // post-decrement uses the real player-visible delta (clamped
                // at hpBefore so a 10-damage attack on a 3-HP target shows "3"
                // rather than "10"). This also serves DamageDealt-event
                // listeners that may need pre-decrement HP.
                int hpBefore = hpStat.BaseValue;

                // Phase F: BeforeTakeDamage event — fires BEFORE resistance.
                // Listeners can mutate damage (e.g., add/remove attributes,
                // reduce Amount) or VETO entirely by returning false from
                // their HandleEvent. Mirrors Qud's BeforeApplyDamageEvent
                // (Physics.cs:3418), which lets Part-side listeners modify
                // or cancel incoming damage.
                //
                // Vetoed damage fires DamageFullyResisted (so observers know
                // an attack was attempted) but does NOT fire TakeDamage and
                // does not decrement HP. Mutations to damage.Amount propagate
                // — the post-event re-read covers both this hook and the
                // TakeDamage event below.
                int amountBeforeMutation = damage.Amount;
                var beforeTakeDamage = GameEvent.New("BeforeTakeDamage");
                beforeTakeDamage.SetParameter("Target", (object)target);
                beforeTakeDamage.SetParameter("Source", (object)source);
                beforeTakeDamage.SetParameter("Damage", (object)damage);
                bool damageProceeded = target.FireEventAndRelease(beforeTakeDamage);

                // PreDamageMutation diag — surfaces the net delta from
                // BeforeTakeDamage event listeners (Stoneskin, etc.).
                // Fires whenever the Amount changed during the event
                // OR the event was vetoed. Lets observability answer
                // "did Stoneskin reduce this hit by N?" without grep.
                if (Diag.IsChannelEnabled("damage") &&
                    (amountBeforeMutation != damage.Amount || !damageProceeded))
                {
                    Diag.Record(
                        category: "damage",
                        kind: "PreDamageMutation",
                        target: target,
                        payload: new
                        {
                            amountBefore = amountBeforeMutation,
                            amountAfter = damage.Amount,
                            delta = damage.Amount - amountBeforeMutation,
                            vetoed = !damageProceeded
                        });
                }

                if (!damageProceeded)
                {
                    // Veto — surface as fully-resisted so observers see the attempt.
                    //
                    // Contract for the Damage object on a vetoed DamageFullyResisted:
                    //   • Amount is left unchanged (the value the attack WOULD have
                    //     dealt before resistance, with any pre-veto listener
                    //     mutations applied). Listeners that want to know "how
                    //     much was blocked" can read damage.Amount.
                    //   • Attributes reflect any pre-veto listener mutations.
                    var fullyResistedVeto = GameEvent.New("DamageFullyResisted");
                    fullyResistedVeto.SetParameter("Target", (object)target);
                    fullyResistedVeto.SetParameter("Source", (object)source);
                    fullyResistedVeto.SetParameter("Damage", (object)damage);
                    target.FireEventAndRelease(fullyResistedVeto);
                    return;
                }

                // Phase E: apply elemental resistances based on damage attributes.
                // Mirrors XRL.World.Parts.Physics.cs:3351-3417. Damage with the
                // "IgnoreResist" attribute bypasses all resistance entirely.
                if (!damage.HasAttribute("IgnoreResist"))
                    ApplyResistances(target, damage);

                if (damage.Amount <= 0)
                {
                    // Resistance fully absorbed. Surface a "fully resisted" event so
                    // listeners (UI, AI retaliation, achievements) still see the attack
                    // attempt even though no HP was lost. (Self-review Finding 4.)
                    var fullyResisted = GameEvent.New("DamageFullyResisted");
                    fullyResisted.SetParameter("Target", (object)target);
                    fullyResisted.SetParameter("Source", (object)source);
                    fullyResisted.SetParameter("Damage", (object)damage);
                    target.FireEventAndRelease(fullyResisted);
                    return;
                }

                // Phase C/E: fire TakeDamage with the typed Damage object BEFORE
                // capturing amount, so listeners can mutate damage.Amount in-flight
                // (e.g., a "StoneSkin" effect that subtracts 2 from incoming damage).
                // The captured amount is read AFTER the event so listener mutations
                // propagate to the HP decrement. (Self-review Finding 1.)
                // (Listeners read damage.Amount via the Damage object directly,
                // not via event parameters, so the event itself can be released
                // immediately after firing.)
                var takeDamage = GameEvent.New("TakeDamage");
                takeDamage.SetParameter("Target", (object)target);
                takeDamage.SetParameter("Source", (object)source);
                takeDamage.SetParameter("Amount", damage.Amount);
                takeDamage.SetParameter("Damage", (object)damage);
                target.FireEventAndRelease(takeDamage);

                // Re-read damage.Amount after listeners — it may have been mutated.
                // Clamp at 0 so over-mutation can't heal the target.
                int amount = Math.Max(0, damage.Amount);
                if (amount <= 0) return;

                hpStat.BaseValue -= amount;

                Stat hpAlias = target.GetStat("HP");
                if (hpAlias != null && !ReferenceEquals(hpAlias, hpStat))
                    hpAlias.BaseValue -= amount;

                // D2.2 diag hook (Docs/D2-HOOKS-PLAN.md §4 D2.2).
                // Records damage AFTER it lands. Broader than the
                // DamageDealt event below — that event only fires when
                // source != null, but environmental damage (traps,
                // status DoT) also writes to the diag buffer. Payload
                // captures post-damage HP and the lethal flag for
                // turn-by-turn combat reconstruction.
                if (Diag.IsChannelEnabled("damage"))
                {
                    Diag.Record(
                        category: "damage",
                        kind: "DamageDealt",
                        actor: source,
                        target: target,
                        payload: new
                        {
                            amount = amount,
                            hpAfter = hpStat.BaseValue,
                            lethal = hpStat.BaseValue <= 0,
                            attributes = damage.Attributes
                        });
                }

                // Notify the attacker that damage was dealt (for on-hit effects like poison)
                if (source != null)
                {
                    var damageDealt = GameEvent.New("DamageDealt");
                    damageDealt.SetParameter("Attacker", (object)source);
                    damageDealt.SetParameter("Defender", (object)target);
                    damageDealt.SetParameter("Amount", amount);
                    damageDealt.SetParameter("Damage", (object)damage); // Phase C
                    source.FireEventAndRelease(damageDealt);
                }

                // Floating damage number — emit at the target's cell so EVERY
                // damage path (melee swings, traps, effect ticks, mutations,
                // future content) produces consistent visual feedback. Uses
                // min(amount, hpBefore) so over-kill damage shows the actual
                // HP delta the player observes on the HUD. Centralizing this
                // here removes the duplicate emit that used to live in
                // PerformSingleAttack — every ApplyDamage call now emits once,
                // including the prior trap-damage path which silently dropped
                // numbers (PressurePlate / TripWire / SpikeTrap / FireTrap /
                // BearTrap, plus per-turn DOT ticks from BleedingEffect /
                // BurningEffect / etc.).
                if (zone != null)
                {
                    int displayedAmount = Math.Min(amount, hpBefore);
                    if (displayedAmount > 0)
                    {
                        var targetCellForFx = zone.GetEntityCell(target);
                        if (targetCellForFx != null)
                            AsciiFxBus.EmitFloatingNumber(
                                zone, targetCellForFx.X, targetCellForFx.Y,
                                displayedAmount, "&R");
                    }
                }

                if (hpStat.BaseValue <= 0)
                {
                    HandleDeath(target, source, zone);
                }
                else if (zone != null)
                {
                    // Mark the target's cell dirty so on-hit visual effects
                    // (damage flash, color change from new status effect,
                    // etc.) render this frame without forcing a full redraw.
                    var targetCell = zone.GetEntityCell(target);
                    if (targetCell != null)
                        ZoneRenderHooks.MarkCellDirty(targetCell, "Combat.Damage");
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
        /// Canonical "deal N damage of type X" API. Wraps the amount in a
        /// <see cref="Damage"/> with the specified element attribute set,
        /// then forwards to the typed overload — so
        /// <see cref="ApplyResistances"/> sees the right
        /// HeatResistance / ColdResistance / ElectricResistance /
        /// AcidResistance stat and applies it correctly.
        ///
        /// <para>Added in WSP7.4 as the migration target for spell-side,
        /// effect-tick, and material-reaction damage that previously
        /// went through the int-overload (no attributes) and silently
        /// bypassed the resistance pipeline. Use this overload for any
        /// damage that "feels" elemental — fire spells, cold spells,
        /// electric DoT ticks, acid-effect ticks, fire+ice reactions,
        /// etc. Pass an empty string for non-elemental damage (effect-
        /// tick from Bleeding / Poisoned, internal damage from material
        /// shatter — these have no matching resistance system in CoO so
        /// the int-overload is the right choice).</para>
        ///
        /// <para>Mirrors the convention of the existing single-element
        /// resistance lookup in <see cref="ApplyResistances"/> (line
        /// 793): "Heat" / "Cold" / "Electric" / "Acid" map to the
        /// matching <c>DamageAttributeFlags</c> bit via
        /// <see cref="Damage.AddAttribute"/>'s alias-aware handling
        /// (Damage.cs:128-160). Aliases like "Fire" / "Ice" / "Freeze"
        /// / "Lightning" / "Shock" all map to the canonical flag —
        /// callers can use whichever string reads best at the call
        /// site.</para>
        /// </summary>
        public static void ApplyDamage(Entity target, int amount,
            string elementAttribute, Entity source, Zone zone)
        {
            var dmg = new Damage(amount);
            if (!string.IsNullOrEmpty(elementAttribute))
                dmg.AddAttribute(elementAttribute);
            ApplyDamage(target, dmg, source, zone);
        }

        /// <summary>
        /// Apply elemental resistances to a damage instance based on the target's
        /// resistance stats and the damage's type attributes. Mirrors
        /// <c>XRL.World.Parts.Physics</c>'s resistance loop (lines 3351-3417).
        ///
        /// For each elemental type (Acid/Heat/Cold/Electric):
        ///   • Positive resistance: damage *= (100 − resist) / 100, min 1 if not ≥100%
        ///   • Negative resistance: damage *= (1 + |resist|/100) — vulnerability
        ///   • 100% resistance fully absorbs (damage = 0)
        ///
        /// If a damage instance carries multiple type attributes (e.g., Cold AND Fire),
        /// each applicable resistance fires in sequence. The order is fixed (Acid →
        /// Heat → Cold → Electric) to match Qud's source.
        /// </summary>
        private static void ApplyResistances(Entity target, Damage damage)
        {
            if (damage.IsAcidDamage())     ApplyResistanceFor(target, damage, "AcidResistance");
            if (damage.IsHeatDamage())     ApplyResistanceFor(target, damage, "HeatResistance");
            if (damage.IsColdDamage())     ApplyResistanceFor(target, damage, "ColdResistance");
            if (damage.IsElectricDamage()) ApplyResistanceFor(target, damage, "ElectricResistance");
        }

        /// <summary>
        /// Apply a single resistance stat to a damage instance using Qud's formula.
        /// </summary>
        private static void ApplyResistanceFor(Entity target, Damage damage, string resistanceStatName)
        {
            if (damage.Amount <= 0) return;
            int resist = target.GetStatValue(resistanceStatName, 0);
            if (resist == 0) return;

            int amountBefore = damage.Amount;
            if (resist > 0)
            {
                // Positive resistance reduces damage proportionally.
                damage.Amount = (int)(damage.Amount * (100 - resist) / 100f);
                // Min 1 unless resist is 100%+ (full immunity).
                if (resist < 100 && damage.Amount < 1)
                    damage.Amount = 1;
            }
            else
            {
                // Negative resistance = vulnerability. -50 means +50% damage.
                damage.Amount += (int)(damage.Amount * (resist / -100f));
            }

            // ResistanceApplied diag — surfaces the per-resistance
            // mutation so observability can answer "did HeatResistance
            // halve this Fire hit?" or "how much did the Glowmaw's
            // resistance steal?" Fires once per resistance stat that
            // actually changed the amount.
            if (Diag.IsChannelEnabled("damage") && amountBefore != damage.Amount)
            {
                Diag.Record(
                    category: "damage",
                    kind: "ResistanceApplied",
                    target: target,
                    payload: new
                    {
                        resistanceStat = resistanceStatName,
                        resistancePercent = resist,
                        amountBefore = amountBefore,
                        amountAfter = damage.Amount,
                        delta = damage.Amount - amountBefore
                    });
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
            target.FireEventAndRelease(died);

            // M2.3: broadcast the death to nearby Passive NPCs so they can
            // visibly react (wander-pace for 20 turns). Fires AFTER the Died
            // event so any handlers that mutate the zone have already run,
            // and BEFORE RemoveEntity so the death cell is still resolvable
            // via GetEntityCell(target).
            if (zone != null)
                BroadcastDeathWitnessed(target, killer, zone, WitnessRadius);

            // Capture the death cell BEFORE RemoveEntity so the renderer can
            // be told to repaint it (corpse/blood splatter glyph, or simply
            // "entity gone, show what's underneath"). Player death triggers
            // a full redraw — the death screen UI may stomp arbitrary cells.
            int? deathX = null, deathY = null;
            if (zone != null)
            {
                var deathCell = zone.GetEntityCell(target);
                if (deathCell != null)
                {
                    deathX = deathCell.X;
                    deathY = deathCell.Y;
                }
            }

            if (zone != null)
                zone.RemoveEntity(target);

            // De-list the dead entity from the turn queue so it doesn't
            // get scheduled for another turn. Without this, dead actors
            // cycle through the turn loop with hp=0 (visible in the diag
            // stream as turn/Begin records on hp:0 entities — the May
            // 2026 live-run review surfaced this as Finding 3). The
            // subsequent BeginTakeAction event is wasted work and the
            // diag emission is noise.
            //
            // TurnManager.Active is null in tests + boot, so the ?.
            // guard makes this safe pre-game and in synthetic fixtures
            // that never set up a TurnManager. The instance method is
            // idempotent (no-op if entity isn't in the queue) so calling
            // it on entities that were never registered is fine.
            TurnManager.Active?.RemoveEntity(target);

            if (target != null && target.HasTag("Player"))
                ZoneRenderHooks.MarkFullDirty("Death.Player");
            else if (deathX.HasValue && deathY.HasValue)
                ZoneRenderHooks.MarkCellDirty(deathX.Value, deathY.Value, "Death");
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
        ///
        /// Phase H: when the chance roll passes, fires <c>CanBeDismembered</c>
        /// on the defender. Listeners can VETO by returning false from
        /// HandleEvent. Mirrors Qud's <c>CanBeDismemberedEvent</c>
        /// (`IGameSystem.cs:637`).
        ///
        /// Test-callable: kept public so unit tests can target the dismember
        /// probability path directly without going through PerformMeleeAttack.
        /// </summary>
        public static void CheckCombatDismemberment(Entity defender, Body body,
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
            if (roll >= chance) return;  // chance roll failed — no event, no dismember

            // Phase H: fire CanBeDismembered to give listeners a chance to veto.
            // Vetoing leaves the body part intact even though the chance roll
            // would otherwise have severed it.
            var canBeDismembered = GameEvent.New("CanBeDismembered");
            canBeDismembered.SetParameter("Defender", (object)defender);
            canBeDismembered.SetParameter("BodyPart", (object)hitPart);
            canBeDismembered.SetParameter("Damage", damage);
            if (!defender.FireEventAndRelease(canBeDismembered))
                return;  // veto — skip the actual dismemberment

            body.Dismember(hitPart, zone);
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
