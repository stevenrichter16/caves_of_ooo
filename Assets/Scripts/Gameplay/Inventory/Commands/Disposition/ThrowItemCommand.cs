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
            if (hitTarget != null)
            {
                if (TryApplyThrownTonic(actor, itemToThrow, hitTarget, zone, rng))
                {
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
                MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}, but it strikes an obstacle.");
                landingCell = trace.LastTraversableCell ?? actorCell;
            }
            else
            {
                landingCell = trace.ImpactCell ?? actorCell;
                if (landingCell == actorCell)
                    MessageLog.Add($"{actor.GetDisplayName()} drops {itemToThrow.GetDisplayName()} at {actor.GetDisplayName()}'s feet.");
                else
                    MessageLog.Add($"{actor.GetDisplayName()} throws {itemToThrow.GetDisplayName()}.");
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

        private static bool TryApplyThrownTonic(Entity actor, Entity item, Entity hitTarget, Zone zone, Random rng)
        {
            var tonic = item?.GetPart<TonicPart>();
            if (tonic == null || !tonic.HasThrowablePayload() || hitTarget == null)
                return false;

            MessageLog.Add($"{actor.GetDisplayName()} shatters {item.GetDisplayName()} on {hitTarget.GetDisplayName()}!");
            tonic.ApplyTo(
                hitTarget,
                actor,
                zone,
                rng,
                consumeItem: false,
                showUseMessage: false);
            return true;
        }
    }
}
