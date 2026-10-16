using System;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class ThrowItemCommand : IInventoryCommand
    {
        private enum ThrowSourceKind { None, Carried, Equipped, World }
        private readonly Entity _item;
        private readonly ThrowIntent _intent;
        private readonly Random _rng;
        private readonly ThrowMode _mode;
        private readonly bool _focused;
        public string Name => "Throw";

        public ThrowItemCommand(Entity item, int targetX, int targetY, Random rng = null)
            : this(item, ThrowIntent.AtCell(targetX, targetY), rng) { }
        public ThrowItemCommand(Entity item, ThrowIntent intent, Random rng = null,
            ThrowMode mode = ThrowMode.Ordinary, bool focused = false)
        { _item = item; _intent = intent; _rng = rng ?? new Random(); _mode = mode; _focused = focused; }

        private ThrowIntent ResolveIntent(InventoryContext context) => _intent?.Kind == ThrowAimKind.Automatic
            ? ThrowTrajectory.ResolveIntent(context.Zone, context.Actor, _intent.X, _intent.Y) : _intent;

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null || !ActorReady(context))
                return Invalid("Actor has no valid living position to throw from.");
            var physics = _item?.GetPart<PhysicsPart>();
            var stack = _item?.GetPart<StackerPart>();
            if (_item == null || physics?.ParentEntity != _item || !physics.Takeable
                || stack != null && (stack.ParentEntity != _item || stack.StackCount <= 0))
                return Invalid("That item cannot be thrown.");
            if (!HandlingService.CanThrow(context.Actor, _item, out string reason)) return Invalid(reason);
            var source = DetectSource(context, out Cell sourceCell);
            if (source == ThrowSourceKind.None) return Invalid("You do not have access to that item to throw it.");
            var origin = context.Zone.GetEntityCell(context.Actor);
            if (source == ThrowSourceKind.World && Math.Abs(sourceCell.X-origin.X)+Math.Abs(sourceCell.Y-origin.Y)>1)
                return Invalid("You can only throw objects from your tile or a cardinal-adjacent tile.");
            var intent = ResolveIntent(context);
            if (!ThrowTrajectory.ValidateIntent(context.Zone, context.Actor, intent, false, out reason)) return Invalid(reason);
            int range = HandlingService.GetThrowRange(context.Actor, _item);
            if (AIHelpers.ChebyshevDistance(origin.X,origin.Y,intent.X,intent.Y)>range)
                return Invalid($"That target is out of range ({range}).");
            if (!ThrowingRules.ValidateMode(context.Actor, _item, context.Zone, intent.Target,
                context.Zone.GetCell(intent.X,intent.Y), _mode, _focused, out reason)) return Invalid(reason);
            return InventoryValidationResult.Valid();
        }
        private static InventoryValidationResult Invalid(string message) =>
            InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, message);
        private static InventoryCommandResult Fail(string message) =>
            InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, message);
        private static bool ActorReady(InventoryContext context) => context?.Actor != null && context.Zone != null
            && context.Actor.SpatialZone == context.Zone
            && context.Zone.GetEntityCell(context.Actor)?.Objects.Contains(context.Actor) == true
            && context.Actor.GetStatValue("Hitpoints",1)>0 && !CombatSystem.IsDeathHandled(context.Actor)
            && context.Actor.GetPart<InventoryPart>() == context.Inventory
            && (context.Inventory == null || context.Inventory.ParentEntity == context.Actor)
            && context.Actor.GetPart<Body>() == context.Body
            && (context.Body == null || context.Body.ParentEntity == context.Actor);

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            if (_mode == ThrowMode.Measured) return Fail("Measured Throw prepares an item; it does not launch it.");
            var validation = Validate(context);
            if (!validation.IsValid) return InventoryCommandResult.ValidationFailure(validation);
            if (!transaction.TryClaimAction(context.Actor)) return Fail("That actor is already resolving an action.");
            if (!transaction.TryClaim(_item, context.Actor, Name)) return Fail("That item is already being transferred.");
            var actor = context.Actor; var zone = context.Zone; var origin = zone.GetEntityCell(actor);
            var intent = ResolveIntent(context);
            var source = DetectSource(context, out Cell sourceCell);
            var physics = _item.GetPart<PhysicsPart>(); var stack = _item.GetPart<StackerPart>();
            int quantity = stack?.StackCount ?? 1;
            var launch = ThrowingRules.CaptureLaunch(actor, _item, zone, intent.Target,
                zone.GetCell(intent.X,intent.Y), _mode);
            transaction.Do(null, () => ThrowingRules.RestorePreparationAfterRefusal(launch));
            var gathering = source == ThrowSourceKind.World ? LocalGatheringClaims.CaptureTake(actor,_item,zone) : null;
            Entity unit = Extract(context, transaction, source, sourceCell);
            if (unit == null) return Fail("Unable to ready that item to throw.");
            // Unequip/clone listeners may relocate, replace parts or alter the chosen
            // target. None of those changes authorizes a throw from a stale snapshot.
            if (!ActorReady(context) || zone.GetEntityCell(actor) != origin
                || _item.GetPart<PhysicsPart>() != physics || _item.GetPart<StackerPart>() != stack
                || !Detached(unit) || !ThrowingRules.IsCurrentLaunch(launch)
                || !HandlingService.CanThrow(actor,unit,out _)
                || !ThrowTrajectory.ValidateIntent(zone,actor,intent,false,out _))
                return Fail("The thrower, item or selected target changed while readying the throw.");
            if (quantity > 1 && (stack.StackCount != quantity-1 || DetectSource(context,out var remainingCell) != source
                || source == ThrowSourceKind.World && remainingCell != sourceCell))
                return Fail("The source stack changed while readying the throw.");
            var trace = ThrowTrajectory.Trace(zone,actor,unit,intent,HandlingService.GetThrowRange(actor,unit));
            if (trace.ImpactCell == null) return Fail("The throw no longer has a valid trajectory.");
            bool consumed = ThrowPayloadResolver.HasTonicPayload(unit) || unit.HasPart<GasGrenadePart>();
            LocalGatheringClaims.RecordTake(gathering, transaction, unit);

            // This is the sole launch boundary. Claims remain held through impact
            // and disposition; authoritative gameplay never runs in AfterCommit.
            transaction.SealAction();
            string phase = "launch"; Exception failure = null;
            try
            {
                if (consumed && unit.GetPart<StackerPart>() is StackerPart spent) spent.StackCount = 0;
                ThrowingRules.CommitLaunch(launch);
                if (trace.Path.Count > 0)
                    AsciiFxBus.EmitThrow(zone,trace);
                phase = "impact";
                if (ThrowPayloadResolver.HasTonicPayload(unit))
                    ThrowPayloadResolver.Resolve(actor,unit,zone,trace.BurstCell,trace.HitEntity,trace.ImpactCell,_focused,_rng,actor.ID + ":" + launch.Serial.ToString(System.Globalization.CultureInfo.InvariantCulture));
                else if (unit.GetPart<GasGrenadePart>() is GasGrenadePart grenade)
                {
                    int spawned = grenade.Detonate(actor,trace.BurstCell ?? origin,zone);
                    MessageLog.Add(spawned > 0 ? $"{unit.GetDisplayName()} releases a cloud of gas."
                        : $"{unit.GetDisplayName()} detonates with no effect.");
                }
                else ResolvePhysical(actor,unit,zone,trace,launch);
            }
            catch (Exception error) { failure = error; }
            finally
            {
                if (!consumed)
                {
                    try
                    {
                        phase = failure == null ? "landing" : phase;
                        var landing = LandExactUnit(context,unit,trace,origin,out bool lost,out Exception landingFailure);
                        if (failure == null && landingFailure != null) failure = landingFailure;
                        if (lost && failure == null) failure = new InvalidOperationException("The thrown item was lost because no legal resting place remained.");
                        if (landing != null)
                        {
                            ThrowingRules.RecordLanded(launch,actor,unit,zone);
                            transaction.AfterCommit(() => ItemLandedEvent.Broadcast(zone,actor,unit,landing));
                        }
                    }
                    catch (Exception error) { if (failure == null) failure = error; }
                }
            }
            if (failure == null) return InventoryCommandResult.Ok(actionCommitted:true);
            Diag.Record("event","ThrowCommittedPartial",actor:actor,target:unit,
                payload:new { phase, error=failure.GetType().Name, message=failure.Message });
            return InventoryCommandResult.CommittedPartial(failure.Message,phase);
        }

        private void ResolvePhysical(Entity actor,Entity unit,Zone zone,ThrowTraceResult trace,ThrowSkillLaunch launch)
        {
            var target = trace.HitEntity;
            if (target == null)
            { MessageLog.Add($"{actor.GetDisplayName()} throws {unit.GetDisplayName()}."); return; }
            if (!RollThrowAccuracy(actor,unit,target,_rng) && !ThrowingRules.RetryPhysicalAccuracy(launch,actor,_rng))
            { MessageLog.Add($"{actor.GetDisplayName()} throws {unit.GetDisplayName()} at {target.GetDisplayName()}, but misses!"); return; }
            int raw = GetThrownDamage(actor,unit,target,_rng,ThrowingRules.PenetrationBonusFor(launch,target));
            if (raw <= 0)
            { MessageLog.Add($"{actor.GetDisplayName()}'s thrown {unit.GetDisplayName()} fails to penetrate {target.GetDisplayName()}'s armor."); return; }
            var damage = new Damage(raw); damage.AddAttribute("Thrown");
            var weapon = unit.GetPart<MeleeWeaponPart>();
            if (!string.IsNullOrEmpty(weapon?.Attributes)) damage.AddAttributes(weapon.Attributes);
            int before = target.GetStatValue("Hitpoints",0);
            CombatSystem.ApplyDamage(target,damage,actor,zone);
            int after = target.GetStatValue("Hitpoints",0), actual = Math.Max(0,before-after);
            MessageLog.Add($"{actor.GetDisplayName()} throws {unit.GetDisplayName()} at {target.GetDisplayName()} for {actual} damage!");
            if (after > 0)
            {
                OnHitClassEffects.Apply(damage,actual,target,actor,zone,_rng);
                OnHitWeaponEffects.Apply(weapon,damage,actual,target,actor,zone,_rng);
                OnHitGasEmit.Apply(weapon,damage,actual,target,actor,zone,_rng);
                ItemEnhancementDispatch.DispatchOnHit(unit,target,actor,damage,actual,zone,_rng);
            }
            ThrowingRules.ApplyDriving(launch,actor,target,zone,trace.ImpactCell,actual);
        }

        private ThrowSourceKind DetectSource(InventoryContext context,out Cell sourceCell)
        {
            sourceCell = null;
            var physics = _item?.GetPart<PhysicsPart>();
            if (physics == null || physics.ParentEntity != _item) return ThrowSourceKind.None;
            var equipped = UnequipCommand.CaptureEquippedState(context,_item);
            if (equipped.HasLocation)
                return physics.Equipped == context.Actor && physics.InInventory == null && _item.SpatialZone == null
                    && context.Inventory?.Objects.Contains(_item) != true ? ThrowSourceKind.Equipped : ThrowSourceKind.None;
            if (context.Inventory?.Objects.Contains(_item) == true)
            {
                int references = 0;
                foreach (var entry in context.Inventory.Objects) if (ReferenceEquals(entry,_item)) references++;
                return references == 1 && physics.InInventory == context.Actor && physics.Equipped == null
                    && _item.SpatialZone == null && context.Zone?.GetEntityCell(_item) == null
                    ? ThrowSourceKind.Carried : ThrowSourceKind.None;
            }
            sourceCell = context.Zone?.GetEntityCell(_item);
            if (sourceCell?.Objects.Contains(_item) == true && _item.SpatialZone == context.Zone
                && physics.InInventory == null && physics.Equipped == null) return ThrowSourceKind.World;
            return ThrowSourceKind.None;
        }
        private static bool Detached(Entity unit)
        {
            var physics = unit?.GetPart<PhysicsPart>(); var stack = unit?.GetPart<StackerPart>();
            return unit != null && unit.SpatialZone == null && physics?.ParentEntity == unit
                && physics.InInventory == null && physics.Equipped == null
                && (stack == null || stack.ParentEntity == unit && stack.StackCount == 1);
        }
        private Entity Extract(InventoryContext context,InventoryTransaction transaction,ThrowSourceKind source,Cell sourceCell)
        {
            var stack = _item.GetPart<StackerPart>();
            if (stack != null && stack.StackCount > 1)
            {
                int count = stack.StackCount; var clay = new ConnectedClayProvenance.Snapshot(_item); Entity split = null;
                transaction.Do(null, () =>
                {
                    stack.StackCount = count; clay.Restore(_item);
                    if (split?.GetPart<StackerPart>() is StackerPart splitCount) splitCount.StackCount = 0;
                    context.Inventory?.RefreshHandlingCarryPenalty();
                });
                split = stack.RemoveOne();
                if (split == null || !transaction.TryClaim(split,context.Actor,Name)) return null;
                context.Inventory?.RefreshHandlingCarryPenalty();
                return split;
            }
            if (source == ThrowSourceKind.Carried)
            {
                var snapshot = InventoryTransferSnapshot.Capture(context.Inventory,_item);
                transaction.Do(null,snapshot.Restore);
                return snapshot.Apply(() => context.Inventory.RemoveObject(_item)) ? _item : null;
            }
            if (source == ThrowSourceKind.Equipped)
                return UnequipCommand.UnequipAndRemove(context,_item,transaction) ? _item : null;
            if (source == ThrowSourceKind.World)
            {
                transaction.Do(null, () =>
                {
                    if (_item.SpatialZone == null && _item.GetPart<PhysicsPart>()?.InInventory == null
                        && _item.GetPart<PhysicsPart>()?.Equipped == null)
                        context.Zone.AddEntity(_item,sourceCell.X,sourceCell.Y);
                });
                return context.Zone.RemoveEntity(_item) ? _item : null;
            }
            return null;
        }
        private static Cell LandExactUnit(InventoryContext context,Entity unit,ThrowTraceResult trace,Cell origin,out bool lost,out Exception landingFailure)
        {
            lost = false; landingFailure = null;
            // Never create a second owner if a forceful callback already gave
            // this exact object a stable custody. Normal nested commands are claimed.
            if (unit.SpatialZone != null) return unit.SpatialZone == context.Zone ? context.Zone.GetEntityCell(unit) : null;
            var physics = unit.GetPart<PhysicsPart>();
            if (physics?.InInventory != null || physics?.Equipped != null) return null;
            var zone = context.Zone;
            var landing = trace.RestCell;
            if (ThrowTrajectory.CanRestItem(zone,unit,landing,context.Actor)
                && TryLand(zone,unit,landing,ref landingFailure)) return landing;
            for (int i=trace.Path.Count-1;i>=0;i--)
            {
                var point = trace.Path[i]; var cell = zone.GetCell(point.X,point.Y);
                if (ThrowTrajectory.CanRestItem(zone,unit,cell,context.Actor)
                    && TryLand(zone,unit,cell,ref landingFailure)) return cell;
            }
            if (ThrowTrajectory.CanRestItem(zone,unit,origin,context.Actor)
                && TryLand(zone,unit,origin,ref landingFailure)) return origin;
            // A footprint can lose every legal resting cell during callbacks.
            // Loss is an explicit paid partial outcome, never a refunded projectile.
            if (unit.GetPart<StackerPart>() is StackerPart spent) spent.StackCount = 0;
            lost = true;
            MessageLog.Add($"{unit.GetDisplayName()} is lost; there is no room for it to land.");
            return null;
        }
        private static bool TryLand(Zone zone,Entity unit,Cell cell,ref Exception failure)
        {
            try { return zone.AddEntity(unit,cell.X,cell.Y); }
            catch (Exception error)
            {
                if (failure == null) failure = error;
                Diag.Record("event","ThrowLandingCallbackFailed",target:unit,
                    payload:new { error=error.GetType().Name, message=error.Message });
                return unit.SpatialZone == zone && zone.GetEntityCell(unit) == cell;
            }
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
        /// melee skill penetration, crit and AutoPen. A validated Measured
        /// Throw snapshot adds its explicit bonus to both penetration and
        /// its cap; unrelated weapon-family skills remain melee-only.
        /// </summary>
        private static int GetThrownDamage(Entity actor, Entity item, Entity target, Random rng, int preparationBonus = 0)
        {
            var weapon = item?.GetPart<MeleeWeaponPart>();
            string statName = weapon?.Stat ?? "Strength";
            int weaponPenBonus = weapon?.PenBonus ?? 0;
            int maxStrBonus = weapon?.MaxStrengthBonus ?? -1;

            int strMod = StatUtils.GetModifier(actor, statName);
            int effectiveMaxStrBonus = (maxStrBonus < 0) ? CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS : maxStrBonus;
            int bonus = strMod + weaponPenBonus + preparationBonus;
            int maxBonus = effectiveMaxStrBonus + weaponPenBonus + preparationBonus;

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

    }
}
