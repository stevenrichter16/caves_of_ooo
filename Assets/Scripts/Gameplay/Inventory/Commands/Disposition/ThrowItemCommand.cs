using System;

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

        public string Name => "Throw";

        public ThrowItemCommand(Entity item, int targetX, int targetY)
        {
            _item = item;
            _targetX = targetX;
            _targetY = targetY;
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

            var rng = new Random();

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

            int strengthBonus = Math.Max(0, StatUtils.GetModifier(actor, "Strength"));
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
                else
                {
                    int damage = GetThrownDamage(actor, itemToThrow, strengthBonus, rng);
                    if (damage > 0)
                    {
                        MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()} at {hitTarget.GetDisplayName()} for {damage} damage!");
                        CombatSystem.ApplyDamage(hitTarget, damage, actor, zone);
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

        private static int GetThrownDamage(Entity actor, Entity item, int strengthBonus, Random rng)
        {
            var weapon = item?.GetPart<MeleeWeaponPart>();
            if (weapon != null && !string.IsNullOrWhiteSpace(weapon.BaseDamage))
                return DiceRoller.Roll(weapon.BaseDamage, rng) + strengthBonus;

            int weight = HandlingService.GetWeight(item);
            return Math.Max(1, (int)Math.Ceiling(weight / 2.0)) + strengthBonus;
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
