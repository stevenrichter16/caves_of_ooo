using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    public enum ThrowMode { Ordinary, Measured, Driving }
    public enum ThrowSelectionKind { Measured, Driving, Recovery }
    public sealed class ThrowSelectionRequest
    {
        public readonly ThrowSelectionKind Kind;
        public readonly string SkillClass;
        public ThrowSelectionRequest(ThrowSelectionKind kind, string skillClass) { Kind = kind; SkillClass = skillClass; }
    }
    public sealed class ThrowSkillLaunch
    {
        public Entity Actor, Source, IntendedTarget, PreparedTarget;
        public Zone Zone;
        public Cell IntendedContact;
        public ThrowMode Mode;
        public int OriginX, OriginY, Serial, PenetrationBonus;
        public bool RetryAccuracy;
        internal bool Committed, DrivingIssued, RetryIssued;
        internal int ActorMovement, PreparationSerial;
        internal string SourceId, SourceBlueprint;
        internal string OriginalPlace;
        internal int OriginalQuantity;
        internal PhysicsPart SourcePhysics;
    }
    public readonly struct ThrowRecoveryResult
    {
        public readonly bool Committed, Collected;
        public readonly string Reason;
        public ThrowRecoveryResult(bool committed, bool collected, string reason)
        { Committed = committed; Collected = collected; Reason = reason ?? ""; }
    }

    /// <summary>Shared skill decisions. Input chooses an item/intent; gameplay remains the authority.</summary>
    public static class ThrowingRules
    {
        public const string Measured = "Throwing_MeasuredThrow", Driving = "Throwing_DrivingThrow", Recovery = "Throwing_RecoveringStep";
        public const int MeasuredPenetrationBonus = 4;
        public static ThrowingStatePart EnsureState(Entity actor)
        {
            if (actor == null) return null;
            var state = actor.GetPart<ThrowingStatePart>();
            if (state == null) { state = new ThrowingStatePart(); actor.AddPart(state); }
            return state;
        }
        static bool Owns(Entity actor, string name)
        {
            var skill = actor?.GetPart<SkillsPart>()?.GetSkill(name);
            return skill != null && skill.ParentEntity == actor && actor.Parts.Contains(skill) && skill.EffectiveRank == 1;
        }
        public static bool IsCooldownSkill(string name) => name == Measured || name == Driving || name == Recovery;
        public static int CooldownRemaining(Entity actor, string name)
        {
            var state = actor?.GetPart<ThrowingStatePart>();
            return state == null ? 0 : name == Measured ? state.MeasuredCooldown : name == Driving ? state.DrivingCooldown : name == Recovery ? state.RecoveryCooldown : 0;
        }
        public static void SynchronizeAbilityDebt(Entity actor)
        {
            var abilities = actor?.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return;
            foreach (var ability in abilities.AbilityList)
                if (IsCooldownSkill(ability.SourcePowerClass)) ability.CooldownRemaining = CooldownRemaining(actor, ability.SourcePowerClass);
        }
        static void SetCooldown(Entity actor, string name, int turns)
        {
            var state = EnsureState(actor);
            if (name == Measured) state.MeasuredCooldown = Math.Max(state.MeasuredCooldown, turns);
            if (name == Driving) state.DrivingCooldown = Math.Max(state.DrivingCooldown, turns);
            if (name == Recovery) state.RecoveryCooldown = Math.Max(state.RecoveryCooldown, turns);
            SynchronizeAbilityDebt(actor);
        }
        public static bool TryBeginSelection(Entity actor, string command, out ThrowSelectionRequest request)
        {
            request = null;
            string name; ThrowSelectionKind kind;
            switch (command)
            {
                case "CommandMeasuredThrow": name = Measured; kind = ThrowSelectionKind.Measured; break;
                case "CommandDrivingThrow": name = Driving; kind = ThrowSelectionKind.Driving; break;
                case "CommandRecoveringStep": name = Recovery; kind = ThrowSelectionKind.Recovery; break;
                default: return false;
            }
            var skill = actor?.GetPart<SkillsPart>()?.GetSkill(name);
            var ability = skill == null ? null : actor.GetPart<ActivatedAbilitiesPart>()?.GetAbility(skill.ActivatedAbilityID);
            if (!Owns(actor, name) || CooldownRemaining(actor, name) > 0 || ability == null || !ability.IsUsable || !Capable(actor)) return false;
            request = new ThrowSelectionRequest(kind, name); return true;
        }
        public static bool IsPhysical(Entity item) => item != null && !(item.GetPart<TonicPart>()?.HasThrowablePayload() == true) && item.GetPart<GasGrenadePart>() == null;
        public static bool CanFocus(Entity actor, Entity item) => Owns(actor, "Throwing_FocusedSplash") && item?.GetPart<TonicPart>()?.HasThrowablePayload() == true;
        static bool Capable(Entity actor)
        {
            if (actor == null || actor.GetStatValue("Hitpoints", 1) <= 0) return false;
            var effects = actor.GetPart<StatusEffectsPart>();
            if (effects != null)
                foreach (var effect in effects.GetAllEffects())
                    if (effect != null && effect.Owner == actor && effect.Duration != 0
                        && (effect is StunnedEffect || effect is FrozenEffect || effect is ParalyzedEffect)) return false;
            return true;
        }
        static bool VisibleContact(Zone zone, Entity target, Cell contact)
        {
            if (zone == null || target == null || contact == null || contact.ParentZone != zone || !contact.IsVisible || !contact.Explored
                || target.GetPart<RenderPart>()?.Visible == false || zone.GetEntityCell(target) == null) return false;
            foreach (var cell in zone.GetOccupiedCells(target)) if (cell == contact && cell.Occupants.Contains(target)) return true;
            return false;
        }
        public static bool ValidateMode(Entity actor, Entity item, Zone zone, Entity target, Cell contact, ThrowMode mode, bool focused, out string reason)
        {
            reason = "";
            if (mode != ThrowMode.Ordinary && mode != ThrowMode.Measured && mode != ThrowMode.Driving) { reason = "Unknown throwing mode."; return false; }
            if (!Capable(actor) || zone?.GetEntityCell(actor) == null) { reason = "You cannot throw now."; return false; }
            if (focused && !CanFocus(actor, item)) { reason = "Focused Splash requires its skill and a tonic payload."; return false; }
            if (mode == ThrowMode.Ordinary) return true;
            string skill = mode == ThrowMode.Measured ? Measured : Driving;
            if (!Owns(actor, skill) || CooldownRemaining(actor, skill) > 0) { reason = "That throwing skill is unavailable or cooling down."; return false; }
            if (!IsPhysical(item)) { reason = "Choose a physical throwable, not a consumable payload."; return false; }
            if (mode == ThrowMode.Driving && HandlingService.GetWeight(item) < 8) { reason = "Driving Throw needs weight 8 or more."; return false; }
            if (mode == ThrowMode.Measured && (target == actor || !VisibleContact(zone, target, contact)
                || target.GetStat("Hitpoints") == null || target.GetStatValue("Hitpoints") <= 0))
            { reason = "Choose a visible tangible target."; return false; }
            if (mode == ThrowMode.Driving && (contact == null || contact.ParentZone != zone || !contact.IsVisible || !contact.Explored
                || target == actor || (target != null && !VisibleContact(zone, target, contact))))
            { reason = "Choose a visible target or ground cell."; return false; }
            return true;
        }
        internal static string SourcePlace(Entity actor, Entity item, Zone zone)
        {
            var physics = item?.GetPart<PhysicsPart>(); var pack = actor?.GetPart<InventoryPart>();
            if (physics == null || pack == null || pack.ParentEntity != actor) return "";
            if (physics.Equipped == actor && physics.InInventory == null && pack.EquippedItems.ContainsValue(item)) return "equipped:" + pack.FindEquippedSlot(item);
            if (physics.InInventory == actor && physics.Equipped == null && pack.Objects.Contains(item)) return "carried";
            var cell = zone?.GetEntityCell(item); var from = zone?.GetEntityCell(actor);
            if (cell != null && from != null && physics.InInventory == null && physics.Equipped == null && Math.Abs(cell.X - from.X) + Math.Abs(cell.Y - from.Y) <= 1)
                return "ground:" + cell.X + ":" + cell.Y;
            return "";
        }
        static ThrownRecoveryPart Track(Entity item)
        {
            var state = item.GetPart<ThrownRecoveryPart>();
            if (state == null) { state = new ThrownRecoveryPart(); item.AddPart(state); }
            return state;
        }
        public static bool Prepare(Entity actor, Entity item, Zone zone, Entity target, Cell contact, out string reason)
        {
            if (!ValidateMode(actor, item, zone, target, contact, ThrowMode.Measured, false, out reason)) return false;
            if (!HandlingService.CanThrow(actor, item, out reason) || string.IsNullOrEmpty(SourcePlace(actor, item, zone))
                || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) { reason = "The selected source is no longer available."; return false; }
            var at = zone.GetEntityCell(actor);
            if (Math.Max(Math.Abs(contact.X - at.X), Math.Abs(contact.Y - at.Y)) > HandlingService.GetThrowRange(actor, item))
            { reason = "That target is out of throwing range."; return false; }
            using (var lease = ActionResolutionGuard.TryEnterAction(actor))
            {
                if (lease == null) { reason = "That actor is already resolving an action."; return false; }
                var state = EnsureState(actor); var tracked = Track(item);
                state.ClearPreparation(); state.PreparationSerial = state.PreparationSerial == int.MaxValue ? 1 : state.PreparationSerial + 1;
                state.PreparedSource = item; state.PreparedTarget = target; state.PreparedZone = zone.ZoneID;
                state.PreparedX = at.X; state.PreparedY = at.Y; state.PreparedSourcePlace = SourcePlace(actor, item, zone);
                state.PreparedQuantity = item.GetPart<StackerPart>()?.StackCount ?? 1; state.PreparedBlueprint = item.BlueprintName;
                state.PreparedRevision = tracked.Revision; state.HasPreparation = state.PreparationClosingTurn = true;
                SetCooldown(actor, Measured, 6);
                try { MessageLog.Add(actor.GetDisplayName() + " prepares " + item.GetDisplayName() + " for a measured throw."); }
                catch (Exception error)
                {
                    // The preparation is already accepted. An informational
                    // subscriber cannot hide its paid result from native input.
                    CavesOfOoo.Diagnostics.Diag.Record("event", "ThrowPreparationObserverFailed", actor: actor, target: item,
                        payload: new { error = error.GetType().Name, message = error.Message });
                }
            }
            return true;
        }
        internal static bool PreparationCurrent(ThrowingStatePart state)
        {
            var actor = state.ParentEntity; var source = state.PreparedSource; var zone = actor?.SpatialZone;
            var at = zone?.GetEntityCell(actor);
            return state.HasPreparation && Owns(actor, Measured) && Capable(actor) && at != null && zone.ZoneID == state.PreparedZone
                && at.X == state.PreparedX && at.Y == state.PreparedY && source != null && source.BlueprintName == state.PreparedBlueprint
                && source.GetPart<ThrownRecoveryPart>()?.Revision == state.PreparedRevision
                && (source.GetPart<StackerPart>()?.StackCount ?? 1) == state.PreparedQuantity
                && SourcePlace(actor, source, zone) == state.PreparedSourcePlace && !string.IsNullOrEmpty(state.PreparedSourcePlace)
                && state.PreparedTarget != null && zone.GetEntityCell(state.PreparedTarget) != null && state.PreparedTarget.GetStatValue("Hitpoints", 1) > 0;
        }
        public static ThrowSkillLaunch CaptureLaunch(Entity actor, Entity source, Zone zone, Entity target, Cell contact, ThrowMode mode)
        {
            var state = actor?.GetPart<ThrowingStatePart>(); var at = zone?.GetEntityCell(actor);
            var shot = new ThrowSkillLaunch { Actor = actor, Source = source, Zone = zone, IntendedTarget = target, IntendedContact = contact, Mode = mode,
                OriginX = at?.X ?? -1, OriginY = at?.Y ?? -1, ActorMovement = state?.MovementRevision ?? 0,
                SourceId = source?.ID, SourceBlueprint = source?.BlueprintName, SourcePhysics = source?.GetPart<PhysicsPart>(),
                OriginalPlace = SourcePlace(actor, source, zone), OriginalQuantity = source?.GetPart<StackerPart>()?.StackCount ?? 1,
                RetryAccuracy = IsPhysical(source) && Owns(actor, "ThrowingSkill") };
            if (state?.Prepared == true && state.PreparedSource == source && state.PreparedTarget == target && IsPhysical(source))
            { shot.PreparedTarget = target; shot.PenetrationBonus = MeasuredPenetrationBonus; shot.PreparationSerial = state.PreparationSerial; }
            return shot;
        }
        public static bool IsCurrentLaunch(ThrowSkillLaunch shot)
        {
            if (shot == null || shot.Committed || !Capable(shot.Actor) || shot.Zone == null || shot.Source == null) return false;
            var cell = shot.Zone.GetEntityCell(shot.Actor);
            return cell != null && cell.X == shot.OriginX && cell.Y == shot.OriginY
                && (shot.Actor.GetPart<ThrowingStatePart>()?.MovementRevision ?? 0) == shot.ActorMovement
                && shot.Source.ID == shot.SourceId && shot.Source.BlueprintName == shot.SourceBlueprint && shot.Source.GetPart<PhysicsPart>() == shot.SourcePhysics
                && ValidateMode(shot.Actor, shot.Source, shot.Zone, shot.IntendedTarget, shot.IntendedContact, shot.Mode, false, out _);
        }
        public static void CommitLaunch(ThrowSkillLaunch shot)
        {
            if (shot == null || shot.Committed) return;
            var state = EnsureState(shot.Actor);
            if (shot.PreparedTarget == null || !state.HasPreparation || state.PreparationSerial != shot.PreparationSerial || !Owns(shot.Actor, Measured))
            { shot.PreparedTarget = null; shot.PenetrationBonus = 0; }
            state.ClearPreparation(); state.ThrowSerial = state.ThrowSerial == int.MaxValue ? 1 : state.ThrowSerial + 1;
            shot.Serial = state.ThrowSerial; shot.Committed = true;
            if (shot.Mode == ThrowMode.Driving) SetCooldown(shot.Actor, Driving, 12);
        }
        /// <summary>Called by extraction's earliest undo, after the original source
        /// is restored. Rebind only this actor's unchanged paid preparation; never
        /// rewind the item's generation or revive a moved actor/lost source.</summary>
        public static void RestorePreparationAfterRefusal(ThrowSkillLaunch shot)
        {
            var state = shot?.Actor?.GetPart<ThrowingStatePart>();
            if (state == null || shot.Committed || shot.PreparedTarget == null || !state.HasPreparation
                || state.PreparationSerial != shot.PreparationSerial || state.PreparedSource != shot.Source
                || !IsCurrentLaunch(shot) || SourcePlace(shot.Actor, shot.Source, shot.Zone) != shot.OriginalPlace
                || (shot.Source.GetPart<StackerPart>()?.StackCount ?? 1) != shot.OriginalQuantity) return;
            state.PreparedRevision = shot.Source.GetPart<ThrownRecoveryPart>()?.Revision ?? state.PreparedRevision;
        }
        public static bool RetryPhysicalAccuracy(ThrowSkillLaunch shot, Entity actor, System.Random rng)
        {
            if (shot == null || !shot.Committed || !shot.RetryAccuracy || shot.RetryIssued || shot.Actor != actor || rng == null) return false;
            shot.RetryIssued = true;
            return DiceRoller.Roll("1d" + Math.Max(1, actor.GetStatValue("Agility", 1)), rng) >= 3;
        }
        public static int PenetrationBonusFor(ThrowSkillLaunch shot, Entity actualHit)
            => shot?.Committed == true && actualHit != null && shot.PreparedTarget == actualHit ? shot.PenetrationBonus : 0;
        public static void ApplyDriving(ThrowSkillLaunch shot, Entity actor, Entity target, Zone zone, Cell impact, int actualDamage)
        {
            if (shot == null || !shot.Committed || shot.Mode != ThrowMode.Driving || shot.DrivingIssued || shot.Actor != actor
                || actualDamage <= 0 || target == null || !target.HasTag("Creature") || target.GetStatValue("Hitpoints", 1) <= 0
                || zone != shot.Zone || impact == null || zone.GetEntityCell(target) == null) return;
            bool present = false; foreach (var cell in zone.GetOccupiedCells(target)) if (cell == impact) present = true;
            if (!present) return;
            shot.DrivingIssued = true;
            // The shared helper uses the actor's current position. A moved thrower
            // cannot silently change the committed outward direction.
            var origin = zone.GetEntityCell(actor);
            if (origin == null || origin.X != shot.OriginX || origin.Y != shot.OriginY) return;
            SkillCombatHelpers.TryPush(actor, target, zone, 1);
        }
        public static void RecordLanded(ThrowSkillLaunch shot, Entity actor, Entity item, Zone zone)
        {
            var at = zone?.GetEntityCell(item); var physics = item?.GetPart<PhysicsPart>();
            if (shot?.Committed != true || shot.Actor != actor || at == null || !IsPhysical(item) || physics == null
                || physics.InInventory != null || physics.Equipped != null || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1) return;
            var mark = Track(item); mark.ThrowerId = actor.ID; mark.ItemId = item.ID; mark.Blueprint = item.BlueprintName;
            mark.ZoneId = zone.ZoneID; mark.X = at.X; mark.Y = at.Y; mark.Serial = shot.Serial;
        }
        public static void OnEntityRelocated(Entity entity)
        {
            var state = entity?.GetPart<ThrowingStatePart>();
            if (state != null)
            {
                state.MovementRevision = state.MovementRevision == int.MaxValue ? int.MinValue : state.MovementRevision + 1;
                state.ClearPreparation(); state.RelocationCommitted?.Invoke();
            }
            OnItemCustodyChanged(entity);
        }
        public static void OnItemCustodyChanged(Entity item) => item?.GetPart<ThrownRecoveryPart>()?.Invalidate();
        public static void OnStackChanged(Entity item) => OnItemCustodyChanged(item);
        public static void OnOwnerTurnEnded(Entity actor, Zone zone)
        {
            var state = actor?.GetPart<ThrowingStatePart>(); if (state == null) return;
            if (state.MeasuredCooldown > 0) state.MeasuredCooldown--;
            if (state.DrivingCooldown > 0) state.DrivingCooldown--;
            if (state.RecoveryCooldown > 0) state.RecoveryCooldown--;
            if (state.HasPreparation)
            { if (state.PreparationClosingTurn && state.Prepared) state.PreparationClosingTurn = false; else state.ClearPreparation(); }
            SynchronizeAbilityDebt(actor);
        }
        public static bool CanRecover(Entity actor, Entity item, Zone zone, out string reason)
        {
            reason = "That exact thrown unit cannot be recovered from here.";
            if (!Owns(actor, Recovery) || !Capable(actor) || CooldownRemaining(actor, Recovery) > 0) return false;
            var at = zone?.GetEntityCell(actor); var dest = zone?.GetEntityCell(item); var mark = item?.GetPart<ThrownRecoveryPart>(); var physics = item?.GetPart<PhysicsPart>();
            if (at == null || dest == null || mark == null || physics == null || mark.Serial <= 0 || string.IsNullOrEmpty(actor.ID)
                || mark.ThrowerId != actor.ID || mark.ItemId != item.ID || mark.Blueprint != item.BlueprintName || mark.ZoneId != zone.ZoneID
                || mark.X != dest.X || mark.Y != dest.Y || physics.InInventory != null || physics.Equipped != null
                || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1 || Math.Max(Math.Abs(dest.X - at.X), Math.Abs(dest.Y - at.Y)) != 1
                || !HandlingService.CanLift(actor, item, out reason)) return false;
            var pack = actor.GetPart<InventoryPart>();
            if (pack == null || (pack.MaxWeight >= 0 && (long)pack.GetCarriedWeight() + HandlingService.GetWeight(item) > pack.MaxWeight)) return false;
            foreach (var cell in zone.GetOccupiedCells(actor, dest.X, dest.Y))
            {
                if (cell == null || cell.BlocksMovement()) return false;
                foreach (var owner in cell.Occupants) if (owner != actor && owner.HasTag("Creature")) return false;
            }
            reason = ""; return true;
        }
        public static ThrowRecoveryResult Recover(Entity actor, Entity item, Zone zone)
        {
            var result = InventorySystem.ExecuteCommand(new RecoverCommand(item), actor, zone);
            return new ThrowRecoveryResult(result.ActionCommitted, actor?.GetPart<InventoryPart>()?.Objects.Contains(item) == true, result.ErrorMessage);
        }
        sealed class RecoverCommand : IInventoryCommand
        {
            readonly Entity item;
            public string Name => "Recovering Step";
            public RecoverCommand(Entity item) { this.item = item; }
            public InventoryValidationResult Validate(InventoryContext context)
                => CanRecover(context?.Actor, item, context?.Zone, out string reason) ? InventoryValidationResult.Valid()
                    : InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, reason);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var actor = context.Actor; var zone = context.Zone;
                if (!transaction.TryClaimAction(actor)) return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "That actor is already resolving an action.");
                if (!CanRecover(actor, item, zone, out string reason)) return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, reason);
                var state = EnsureState(actor); var from = zone.GetEntityCell(actor); var destination = zone.GetEntityCell(item);
                bool committed = false;
                state.RelocationCommitted = () =>
                {
                    if (committed) return;
                    committed = true; transaction.SealAction(); state.ClearPreparation(); SetCooldown(actor, Recovery, 6);
                    };
                try
                {
                    bool moved = MovementSystem.TryMoveChecked(actor, zone, destination.X - from.X, destination.Y - from.Y,
                        () => zone.GetEntityCell(actor) == from && CanRecover(actor, item, zone, out _));
                    if (!moved && !committed) return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The recovery step was blocked.");
                    if (!committed) state.RelocationCommitted();
                    if (!Capable(actor) || zone.GetEntityCell(actor) != destination || !CurrentGroundMark(actor, item, zone))
                        return InventoryCommandResult.CommittedPartial("The step completed, but the item could not be collected.", "recovery_entry");
                    var pickup = PickupCommand.ForSelfRecovery(item);
                    var valid = pickup.Validate(context);
                    if (!valid.IsValid) return InventoryCommandResult.CommittedPartial(valid.ErrorMessage, "recovery_pickup");
                    var picked = pickup.Execute(context, transaction);
                    return picked.Success ? InventoryCommandResult.Ok(true) : InventoryCommandResult.CommittedPartial(picked.ErrorMessage, "recovery_pickup");
                }
                finally { state.RelocationCommitted = null; }
            }
        }
        static bool CurrentGroundMark(Entity actor, Entity item, Zone zone)
        {
            var mark = item?.GetPart<ThrownRecoveryPart>(); var at = zone.GetEntityCell(item); var physics = item?.GetPart<PhysicsPart>();
            return mark != null && mark.Serial > 0 && mark.ThrowerId == actor.ID && mark.ItemId == item.ID && mark.Blueprint == item.BlueprintName
                && mark.ZoneId == zone.ZoneID && at != null && mark.X == at.X && mark.Y == at.Y && zone.GetEntityCell(actor) == at
                && physics != null && physics.InInventory == null && physics.Equipped == null && (item.GetPart<StackerPart>()?.StackCount ?? 1) == 1;
        }
    }
}
