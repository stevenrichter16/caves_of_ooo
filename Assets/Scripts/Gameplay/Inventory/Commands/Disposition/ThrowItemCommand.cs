using System;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class ThrowItemCommand : IInventoryCommand
    {
        private enum ThrowSourceKind
        {
            None,
            Carried,
            Equipped,
            World
        }

        private readonly Entity _item;
        private readonly int _targetX;
        private readonly int _targetY;
        private readonly Random _rng;

        public string Name => "Throw";

        /// <param name="rng">
        /// SM4/D6 (Docs/THROWN-MUTATION-COMBAT-PLAN.md): injected RNG,
        /// mirroring melee's InputHandler._combatRng convention. Defaults
        /// to a fresh Random() so existing callers keep compiling, but
        /// production call sites should thread a real combat RNG for
        /// determinism/diag-replay parity with melee.
        /// </param>
        public ThrowItemCommand(Entity item, int targetX, int targetY, Random rng = null)
        {
            _item = item;
            _targetX = targetX;
            _targetY = targetY;
            _rng = rng ?? new Random();
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Throw requires a valid actor.");
            }

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Throw requires a valid item.");
            }

            if (context.Zone == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidZone,
                    "Throw requires a valid zone.");
            }

            var actorCell = context.Zone.GetEntityCell(context.Actor);
            if (actorCell == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor has no valid position to throw from.");
            }

            var physics = _item.GetPart<PhysicsPart>();
            if (physics == null || !physics.Takeable)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotTakeable,
                    "That cannot be thrown.");
            }

            if (!HandlingService.CanThrow(context.Actor, _item, out string reason))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    reason);
            }

            ThrowSourceKind source = DetectSource(context, out Cell sourceCell);
            if (source == ThrowSourceKind.None)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "You do not have access to that item to throw it.");
            }

            if (source == ThrowSourceKind.World)
            {
                int manhattan = Math.Abs(sourceCell.X - actorCell.X) + Math.Abs(sourceCell.Y - actorCell.Y);
                if (manhattan > 1)
                {
                    return InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.BlockedByRule,
                        "You can only throw objects from your tile or a cardinal-adjacent tile.");
                }
            }

            if (!context.Zone.InBounds(_targetX, _targetY))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "That target is out of bounds.");
            }

            int range = HandlingService.GetThrowRange(context.Actor, _item);
            int distance = AIHelpers.ChebyshevDistance(actorCell.X, actorCell.Y, _targetX, _targetY);
            if (distance > range)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    $"That target is out of range ({range}).");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;
            var actorCell = zone.GetEntityCell(actor);
            if (actorCell == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Actor has no valid position to throw from.");
            }

            ThrowSourceKind source = DetectSource(context, out Cell sourceCell);
            if (source == ThrowSourceKind.None)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "You do not have access to that item to throw it.");
            }

            Entity itemToThrow = ExtractItemForThrow(context, transaction, source, sourceCell);
            if (itemToThrow == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unable to ready that item to throw.");
            }

            var rng = _rng;

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(
                zone,
                actor,
                actorCell.X,
                actorCell.Y,
                _targetX,
                _targetY,
                HandlingService.GetThrowRange(actor, _item));

            if (trace.Path.Count > 0)
                AsciiFxBus.EmitProjectile(zone, trace.Path, AsciiFxTheme.ThrownObject, trail: true, blocksTurnAdvance: true);

            Entity hitTarget = trace.HitEntity;
            Cell landingCell;
            bool consumedOnImpact = false;
            bool isThrowableTonic = HasThrowablePayload(itemToThrow);
            bool isGasGrenade = HasGasGrenadePayload(itemToThrow);

            if (hitTarget != null)
            {
                if (isThrowableTonic)
                {
                    // Tonic shatters on the hit cell with AOE radius 1.
                    // The hit creature is in the AOE so it gets the effect.
                    landingCell = trace.ImpactCell ?? zone.GetEntityCell(hitTarget);
                    ApplyTonicAoe(actor, itemToThrow, landingCell, zone, rng);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else if (isGasGrenade)
                {
                    // G.7a: gas grenade detonates on the hit cell (or
                    // the target's cell as fallback), spawning a 3×3
                    // gas cloud. The hit creature is in the cloud and
                    // takes immediate gas damage on the next ApplyGas
                    // pass. Friendly fire is intentional.
                    landingCell = trace.ImpactCell ?? zone.GetEntityCell(hitTarget);
                    DetonateGasGrenade(actor, itemToThrow, landingCell, zone);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else if (!RollThrowAccuracy(actor, itemToThrow, hitTarget, rng))
                {
                    // SM5/D3+D4 (Docs/THROWN-MUTATION-COMBAT-PLAN.md): a
                    // missed throw folds into the same landing behavior as
                    // the "no HitEntity" case -- it sails past and lands at
                    // the traced impact cell, dealing no damage.
                    MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()} at {hitTarget.GetDisplayName()}, but misses!");
                    landingCell = trace.ImpactCell;
                }
                else
                {
                    int rawDamage = GetThrownDamage(actor, itemToThrow, hitTarget, rng);
                    if (rawDamage > 0)
                    {
                        // SM1/D1 (Docs/THROWN-MUTATION-COMBAT-PLAN.md): wrap in
                        // a real, attribute-tagged Damage object (parallel to
                        // melee's CombatSystem.cs "Melee"/weapon.Attributes
                        // tagging) instead of the raw-int ApplyDamage overload,
                        // so ApplyResistances actually runs for thrown weapons.
                        var thrownDamage = new Damage(rawDamage);
                        thrownDamage.AddAttribute("Thrown");
                        var thrownWeaponPart = itemToThrow.GetPart<MeleeWeaponPart>();
                        if (thrownWeaponPart != null && !string.IsNullOrEmpty(thrownWeaponPart.Attributes))
                            thrownDamage.AddAttributes(thrownWeaponPart.Attributes);

                        // SM2/D5: log the TRUE post-resistance landed damage
                        // (hpBefore/hpAfter delta), mirroring melee's fix at
                        // CombatSystem.cs:378-399 -- rawDamage is the
                        // pre-resistance dice roll and would misreport the
                        // number on any resistant target ("the HP bar told
                        // the truth, the log lied").
                        int hpBefore = hitTarget.GetStatValue("Hitpoints", 0);
                        CombatSystem.ApplyDamage(hitTarget, thrownDamage, actor, zone);
                        int hpAfter = hitTarget.GetStatValue("Hitpoints", 0);
                        int actualDamage = Math.Max(0, hpBefore - hpAfter);

                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()} at {hitTarget.GetDisplayName()} for {actualDamage} damage!");

                        // SM3/D2: route thrown hits through the same on-hit
                        // dispatch chain melee uses, gated the same way melee
                        // gates it (CombatSystem.cs's `if (hpAfter > 0)`
                        // survivor-only block) -- a thrown Serrated dagger or
                        // Lifesteal weapon should bleed/heal the same as a
                        // melee-swung one. Each dispatcher self-gates on
                        // actualDamage<=0 internally (matching melee exactly,
                        // not duplicating the check at the call site).
                        if (hpAfter > 0)
                        {
                            OnHitClassEffects.Apply(thrownDamage, actualDamage, hitTarget, actor, zone, rng);
                            OnHitWeaponEffects.Apply(thrownWeaponPart, thrownDamage, actualDamage, hitTarget, actor, zone, rng);
                            OnHitGasEmit.Apply(thrownWeaponPart, thrownDamage, actualDamage, hitTarget, actor, zone, rng);
                            ItemEnhancementDispatch.DispatchOnHit(
                                itemToThrow, hitTarget, actor, thrownDamage, actualDamage, zone, rng);
                        }
                    }
                    else
                    {
                        // SM5c/D3-Layer3: hit the target but failed to
                        // penetrate its armor -- a NEW failure mode distinct
                        // from an accuracy miss (D3/D4 above), needing its
                        // own message so it isn't a silent no-op (mirrors
                        // melee's "hits {defender} but fails to penetrate!").
                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()} at {hitTarget.GetDisplayName()}, but it fails to penetrate!");
                    }

                    landingCell = trace.ImpactCell;
                }
            }
            else if (trace.BlockedBySolid)
            {
                Cell impactCell = trace.LastTraversableCell ?? actorCell;
                if (isThrowableTonic)
                {
                    // Tonic hits a wall — shatter at last traversable cell.
                    MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}; it strikes an obstacle and shatters.");
                    ApplyTonicAoe(actor, itemToThrow, impactCell, zone, rng);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else if (isGasGrenade)
                {
                    // G.7a: grenade hits a wall — detonate at last
                    // traversable cell. Some of the 3×3 cells may be
                    // inside the wall; gas there is still spawned and
                    // dispersal handles non-seeping containment.
                    MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}; it strikes an obstacle and detonates.");
                    DetonateGasGrenade(actor, itemToThrow, impactCell, zone);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else
                {
                    MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}, but it strikes an obstacle.");
                    landingCell = impactCell;
                }
            }
            else
            {
                Cell impactCell = trace.ImpactCell ?? actorCell;
                if (isThrowableTonic)
                {
                    // Missed — tonic still shatters on whatever empty cell
                    // it landed in, splashing creatures in radius 1.
                    if (impactCell == actorCell)
                        MessageLog.Add($"{actor.GetDisplayName()} fumbles {itemToThrow.GetDisplayName()}; it shatters at their feet.");
                    else
                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}.");
                    ApplyTonicAoe(actor, itemToThrow, impactCell, zone, rng);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else if (isGasGrenade)
                {
                    // G.7a: missed throw — grenade lands and detonates
                    // wherever it stopped. Fumble fires at the thrower's
                    // feet (friendly fire — they're now coated in their
                    // own gas).
                    if (impactCell == actorCell)
                        MessageLog.Add($"{actor.GetDisplayName()} fumbles {itemToThrow.GetDisplayName()}; it detonates at their feet.");
                    else
                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}; it detonates.");
                    DetonateGasGrenade(actor, itemToThrow, impactCell, zone);
                    consumedOnImpact = true;
                    landingCell = null;
                }
                else
                {
                    landingCell = impactCell;
                    if (landingCell == actorCell)
                        MessageLog.Add($"{actor.GetDisplayName()} drops {itemToThrow.GetDisplayName()} at {actor.GetDisplayName()}'s feet.");
                    else
                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}.");
                }
            }

            if (landingCell == null)
                landingCell = actorCell;

            if (!consumedOnImpact && !zone.AddEntity(itemToThrow, landingCell.X, landingCell.Y))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "The thrown item could not land.");
            }

            if (!consumedOnImpact)
            {
                // M3.2: broadcast ItemLanded to every creature in the zone
                // so AIRetrieverPart wearers (pet dogs, fetch companions)
                // can push GoFetchGoal.
                //
                // Fired AFTER AddEntity succeeds so the item is already on
                // the ground and GoFetchGoal's zone.GetEntityCell lookup
                // will succeed. Consumed-on-impact items (thrown tonic
                // applied directly) are excluded by the outer if — there's
                // no landed item to fetch.
                //
                // Rollback safety: if the throw's outer transaction is
                // ever rolled back, the undo below removes itemToThrow
                // from the zone. Any GoFetchGoal pushed by this broadcast
                // will then see a null cell lookup and self-pop — see
                // GoFetchGoal.TakeAction's itemCell == null guards.
                ItemLandedEvent.Broadcast(zone, actor, itemToThrow, landingCell);

                transaction.Do(
                    apply: null,
                    undo: () => zone.RemoveEntity(itemToThrow));
            }

            return InventoryCommandResult.Ok();
        }

        private ThrowSourceKind DetectSource(InventoryContext context, out Cell sourceCell)
        {
            sourceCell = null;
            if (context == null || _item == null)
                return ThrowSourceKind.None;

            var equippedState = UnequipCommand.CaptureEquippedState(context, _item);
            if (equippedState.HasLocation)
                return ThrowSourceKind.Equipped;

            if (context.Inventory != null && context.Inventory.Objects.Contains(_item))
                return ThrowSourceKind.Carried;

            sourceCell = context.Zone?.GetEntityCell(_item);
            if (sourceCell != null)
                return ThrowSourceKind.World;

            return ThrowSourceKind.None;
        }

        private Entity ExtractItemForThrow(
            InventoryContext context,
            InventoryTransaction transaction,
            ThrowSourceKind source,
            Cell sourceCell)
        {
            if (_item == null)
                return null;

            var sourceStacker = _item.GetPart<StackerPart>();
            if (sourceStacker != null && sourceStacker.StackCount > 1)
            {
                Entity split = sourceStacker.RemoveOne();
                if (source == ThrowSourceKind.Carried && context.Inventory != null)
                    context.Inventory.RefreshHandlingCarryPenalty();
                transaction.Do(
                    apply: null,
                    undo: () => RestoreSplitStack(context.Inventory, source, sourceStacker, split));
                return split;
            }

            switch (source)
            {
                case ThrowSourceKind.Carried:
                    if (context.Inventory == null || !context.Inventory.RemoveObject(_item))
                        return null;

                    transaction.Do(
                        apply: null,
                        undo: () => context.Inventory.AddObject(_item));
                    return _item;

                case ThrowSourceKind.Equipped:
                {
                    if (!UnequipCommand.UnequipAndRemove(context, _item, transaction))
                        return null;

                    // Item has been removed from inventory by UnequipAndRemove.
                    // Register undo to put it back there if the throw is rolled back.
                    transaction.Do(
                        apply: null,
                        undo: () => context.Inventory?.AddObject(_item));
                    return _item;
                }

                case ThrowSourceKind.World:
                    if (sourceCell == null || context.Zone == null || !context.Zone.RemoveEntity(_item))
                        return null;

                    transaction.Do(
                        apply: null,
                        undo: () => context.Zone.AddEntity(_item, sourceCell.X, sourceCell.Y));
                    return _item;
            }

            return null;
        }

        private static void RestoreSplitStack(InventoryPart inventory, ThrowSourceKind source, StackerPart sourceStacker, Entity split)
        {
            if (sourceStacker == null || split == null)
                return;

            var splitStacker = split.GetPart<StackerPart>();
            int splitCount = splitStacker?.StackCount ?? 1;
            sourceStacker.StackCount += Math.Max(1, splitCount);
            if (splitStacker != null)
                splitStacker.StackCount = 0;
            if (source == ThrowSourceKind.Carried && inventory != null)
                inventory.RefreshHandlingCarryPenalty();
        }

        /// <summary>
        /// SM5/D3 (Docs/THROWN-MUTATION-COMBAT-PLAN.md), LOCKED 2026-07-22
        /// Option A: a near-literal port of Qud's real thrown-weapon
        /// accuracy check (verified directly against
        /// XRL.Rules/Stat.cs:1090-1159 and
        /// XRL.World/GameObject.cs:14869) -- NOT melee's DV roll. Qud rolls
        /// <c>RollPenetratingSuccesses("1d" + Agility, 3)</c>: a single die
        /// whose SIZE equals the thrower's Agility score, success if the
        /// roll is &gt;= 3. For Agility &gt;= 3 this collapses to
        /// <c>P(hit) = (Agility-2)/Agility</c> -- 50% at 4, 87.5% at 16,
        /// asymptotically approaching but never reaching 100%.
        ///
        /// Deliberately does NOT read the defender's DV or AV -- thrown
        /// accuracy checks the thrower's aim, not the target's armor or
        /// dodge, and Qud's own thrown weapons never check DV either. Also
        /// deliberately drops Qud's rare "explode on max roll" re-roll
        /// clause (only ever matters below Agility 3, where the outcome is
        /// already a guaranteed miss with or without it) and its separate
        /// AimVariance trajectory-wobble layer (excluded per explicit user
        /// confirmation -- no drift-to-the-wrong-cell mechanic in CoO).
        /// </summary>
        private static bool RollThrowAccuracy(Entity actor, Entity item, Entity target, Random rng)
        {
            int agilityScore = actor.GetStatValue("Agility", 10);
            bool landed = DiceRoller.Roll("1d" + agilityScore, rng) >= 3;

            // Docs/THROWN-MUTATION-COMBAT-PLAN.md SM5c/D7 -- ThrowHitRoll.
            // The accuracy roll (SM5) never had diag coverage; fires exactly
            // once per throw attempt (hit or miss), matching melee's HitRoll
            // shape so `diag_query category=damage` stays one unified
            // surface for "why did this attack/throw miss?"
            if (Diag.IsChannelEnabled("damage"))
            {
                Diag.Record(
                    category: "damage",
                    kind: "ThrowHitRoll",
                    actor: actor,
                    target: target,
                    payload: new
                    {
                        weapon = item?.GetDisplayName() ?? "(improvised)",
                        agilityScore = agilityScore,
                        target = 3,
                        landed = landed
                    });
            }

            return landed;
        }

        /// <summary>
        /// SM5c/D3-Layer3 (Docs/THROWN-MUTATION-COMBAT-PLAN.md), corrected
        /// 2026-07-22: rolls damage severity through
        /// <see cref="CombatSystem.RollPenetrations"/> against the target's
        /// AV, exactly like melee -- previously this method never touched
        /// AV/armor at all (a real Qud-parity gap the original plan draft
        /// incorrectly asserted was "already effectively ported"). Bonus
        /// inputs mirror melee's non-crit, non-skill terms
        /// (<c>StatUtils.GetModifier(actor, weapon.Stat)</c> + weapon
        /// <c>PenBonus</c>/<c>MaxStrengthBonus</c>) -- deliberately excludes
        /// <c>SkillEventDispatcher.GetSkillPenetrationModifier</c> and
        /// crit/AutoPen, for the same reason D2 defers the on-hit skill
        /// hooks: "do skills fire off a thrown weapon the same as a
        /// wielded one?" is an open design question, so no skill hook gets
        /// partial wiring while it's unresolved.
        /// </summary>
        private static int GetThrownDamage(Entity actor, Entity item, Entity target, Random rng)
        {
            var weapon = item?.GetPart<MeleeWeaponPart>();
            string statName = weapon?.Stat ?? "Strength";
            int weaponPenBonus = weapon?.PenBonus ?? 0;
            int maxStrBonus = weapon?.MaxStrengthBonus ?? -1;

            int strMod = StatUtils.GetModifier(actor, statName);
            int effectiveMaxStrBonus = (maxStrBonus < 0) ? CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS : maxStrBonus;
            int bonus = strMod + weaponPenBonus;
            int maxBonus = effectiveMaxStrBonus + weaponPenBonus;

            Body targetBody = target?.GetPart<Body>();
            BodyPart hitPart = targetBody != null ? CombatSystem.SelectHitLocation(targetBody, rng) : null;
            int av = hitPart != null ? CombatSystem.GetPartAV(target, hitPart) : CombatSystem.GetAV(target);

            int penetrations = CombatSystem.RollPenetrations(av, bonus, maxBonus, rng);

            // ThrowPenetration diag -- mirrors melee's Penetration diag shape.
            if (Diag.IsChannelEnabled("damage"))
            {
                Diag.Record(
                    category: "damage",
                    kind: "ThrowPenetration",
                    actor: actor,
                    target: target,
                    payload: new
                    {
                        weapon = item?.GetDisplayName() ?? "(improvised)",
                        av = av,
                        weaponPenBonus = weaponPenBonus,
                        strMod = strMod,
                        totalBonus = bonus,
                        maxBonus = maxBonus,
                        penetrations = penetrations
                    });
            }

            if (penetrations == 0)
                return 0;

            if (weapon != null && !string.IsNullOrWhiteSpace(weapon.BaseDamage))
            {
                int total = 0;
                for (int i = 0; i < penetrations; i++)
                    total += DiceRoller.Roll(weapon.BaseDamage, rng);
                return total;
            }

            int weight = HandlingService.GetWeight(item);
            int perPenetration = Math.Max(1, (int)Math.Ceiling(weight / 2.0));
            return perPenetration * penetrations;
        }

        /// <summary>
        /// True if the item is a tonic with a throwable payload (healing,
        /// stat boost, cure tonic, or status tonic). Used by the impact
        /// paths to decide whether to shatter (consume item + AOE) or
        /// fall through to the standard "thrown projectile" damage /
        /// ground-land behavior.
        /// </summary>
        private static bool HasThrowablePayload(Entity item)
        {
            var tonic = item?.GetPart<TonicPart>();
            return tonic != null && tonic.HasThrowablePayload();
        }

        /// <summary>G.7a — true if the item carries a
        /// <see cref="GasGrenadePart"/>. Parallel to
        /// <see cref="HasThrowablePayload"/>: both produce a "shatter on
        /// impact" item that consumes itself + triggers AOE. The two are
        /// mutually exclusive in current content (no item is both a
        /// tonic and a grenade), but if a future item carries both, the
        /// tonic branch wins by code order in the impact dispatch.</summary>
        private static bool HasGasGrenadePayload(Entity item)
        {
            return item?.GetPart<GasGrenadePart>() != null;
        }

        /// <summary>G.7a — delegate the 3×3 spawn to the Part. Parallel
        /// in shape to <see cref="ApplyTonicAoe"/>: take the item +
        /// center + zone, do the AOE work, return.</summary>
        private static void DetonateGasGrenade(Entity actor, Entity item, Cell center, Zone zone)
        {
            var grenade = item?.GetPart<GasGrenadePart>();
            if (grenade == null || center == null || zone == null) return;
            int spawned = grenade.Detonate(actor, center, zone);
            string itemName = item.GetDisplayName() ?? "gas grenade";
            if (spawned > 0)
                MessageLog.Add($"{itemName} releases a cloud of gas.");
            else
                MessageLog.Add($"{itemName} detonates with no effect.");
        }

        /// <summary>
        /// Apply a thrown tonic's effect to every Creature-tagged entity
        /// in a 3×3 area around <paramref name="center"/>. Iterates the
        /// cell + 8 neighbors; out-of-bounds cells are skipped via
        /// <see cref="Zone.GetCell"/>'s null return. Per-cell occupants
        /// are snapshot via <see cref="Cell.Objects"/> indexing — safe
        /// even if <see cref="TonicPart.ApplyTo"/> mutates the cell as
        /// a side effect of applying the effect (e.g., a status that
        /// kills the creature) because we re-check bounds on each step.
        ///
        /// <para>Friendly-fire is intentional: the AOE doesn't filter by
        /// faction. Throwing a tonic into your own ranks hits them too.
        /// See <c>Docs/THROWABLE-CONSUMABLES.md §Design</c>.</para>
        ///
        /// <para>Logs a single shatter line with hit count for player
        /// feedback. Caller is responsible for setting
        /// <c>consumedOnImpact = true</c> after this returns.</para>
        /// </summary>
        private static void ApplyTonicAoe(Entity actor, Entity item, Cell center, Zone zone, Random rng)
        {
            var tonic = item?.GetPart<TonicPart>();
            if (tonic == null || center == null || zone == null)
                return;

            int hitCount = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var cell = zone.GetCell(center.X + dx, center.Y + dy);
                    if (cell == null) continue;
                    // Snapshot length so the loop is safe against
                    // ApplyTo side-effects mutating Cell.Objects (rare,
                    // but possible if an effect kills the entity).
                    int objectCount = cell.Objects.Count;
                    for (int i = 0; i < objectCount; i++)
                    {
                        if (i >= cell.Objects.Count) break;
                        var occupant = cell.Objects[i];
                        if (occupant == null || !occupant.HasTag("Creature"))
                            continue;
                        tonic.ApplyTo(
                            occupant,
                            actor,
                            zone,
                            rng,
                            consumeItem: false,
                            showUseMessage: false);
                        hitCount++;
                    }
                }
            }

            string itemName = item?.GetDisplayName() ?? "tonic";
            if (hitCount == 0)
                MessageLog.Add($"{itemName} shatters with no effect.");
            else
                MessageLog.Add($"{itemName} shatters, splashing {hitCount} target{(hitCount > 1 ? "s" : "")}.");
        }
    }
}
