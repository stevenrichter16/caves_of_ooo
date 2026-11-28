using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    // Existing fire-moss kindling, adapted to controlled tool fuel. Legacy light
    // commands stay flame/flint-only: a consumable always needs its own preview.
    internal static class PortableKindling
    {
        const string Prefix = "KindleFuel|";
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        internal static bool OwnsReceipt(Entity target, string command) => IsCommand(command)
            && target != null && (target.GetPart<TorchLightPart>()?.ParentEntity == target || target.GetPart<CampfirePart>()?.ParentEntity == target);
        static string E(string s) => Uri.EscapeDataString(s ?? "");
        static string N(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        static int Count(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static bool TorchReach(Entity actor, Entity target, Zone zone)
        {
            var physics = target.GetPart<PhysicsPart>(); var pack = actor.GetPart<InventoryPart>();
            if (WorldResourceActions.Ground(target, zone)) return SpatialQuery.Distance(zone, actor, target) <= 1
                && zone.GetReadOnlyEntities().Count(e => e.ID == target.ID) == 1
                && pack?.Objects.Any(e => e?.ID == target.ID) != true && pack?.EquippedItems.Values.Any(e => e?.ID == target.ID) != true;
            return target.SpatialZone == null && physics?.Equipped == actor && physics.InInventory == null
                && pack?.EquippedItems.Values.Any(e => e == target) == true && !pack.Objects.Any(e => e?.ID == target.ID)
                && (!actor.HasPart<Body>() || pack.FindEquippedBodyPart(target) != null)
                && !zone.GetReadOnlyEntities().Any(e => e.ID == target.ID);
        }
        static bool Coherent(Entity item) => item != null && item.Parts.All(p => p.ParentEntity == item)
            && item.Parts.GroupBy(p => p.GetType()).All(g => g.Count() == 1);
        static bool Dry(Entity item)
        {
            var wet = item.GetEffect<WetEffect>(); var frozen = item.GetEffect<FrozenEffect>();
            return (wet == null || WorldResourceActions.Finite(wet.Moisture) && wet.Moisture <= .35f)
                && (frozen == null || WorldResourceActions.Finite(frozen.Cold) && frozen.Cold <= 0);
        }
        static bool Usable(Entity actor, Entity moss, Zone zone) => moss?.BlueprintName == "FireMoss"
            && Coherent(moss) && moss.GetPart<StackerPart>() != null && moss.GetPart<ReagentPart>()?.ParentEntity == moss
            && actor.GetPart<InventoryPart>()?.Objects.Contains(moss) == true
            && WorldResourceActions.Carried(actor, moss, false) && Dry(moss)
            && !zone.GetReadOnlyEntities().Any(e => e.ID == moss.ID);
        static Entity Select(Entity actor, Entity target, Zone zone) => IgnitionSources.Find(actor, zone, target) != null ? null
            : actor.GetPart<InventoryPart>()?.Objects.Where(m => m != target && Usable(actor, m, zone))
                .OrderBy(m => m.ID, StringComparer.Ordinal).FirstOrDefault();

        internal static bool CanLightFromWick(Entity actor, Entity target, Zone zone)
            => TorchDisclosed(target, zone) && TargetState.TryCapture(actor, target, zone, out var state) && state.Torch != null;

        static bool TorchDisclosed(Entity target, Zone zone)
            => target != null && (target.SpatialZone == null
                || target.GetPart<RenderPart>() is { Visible: true } && RouteWorkMenu.Visible(zone?.GetEntityCell(target)));

        internal static bool LightFromWick(Entity actor, Entity target, Zone zone, Entity wick, string command,
            InventoryTransaction tx, Func<bool> sourceCurrent)
        {
            if (!TorchDisclosed(target, zone) || !TargetState.TryCapture(actor, target, zone, out var state) || state.Torch == null
                || !tx.TryClaim(actor, actor, command) || !tx.TryClaim(target, actor, command)
                || !tx.TryClaim(wick, actor, command)) return false;
            string targetId = target.ID;
            StageFlame(state, tx);
            tx.BeforeCommit(() => target.ID == targetId && sourceCurrent() && TorchDisclosed(target, zone)
                && state.Current(state.LightingTemperature, true));
            tx.AfterCommit(() =>
            {
                state.Torch.NotifyKindling(actor);
                MessageLog.Add("You light the torch from the lamp.");
                Diag.Record("event", "TorchLit", actor, target, new { source = wick.ID, fuel = state.Fuel.FuelMass });
            });
            return true;
        }

        internal static void AddActions(Entity actor, Entity target, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !TargetState.TryCapture(actor, target, zone, out var state)) return;
            var moss = Select(actor, target, zone); if (moss == null) return;
            var at = zone.GetEntityCell(actor);
            string command = Prefix + E(zone.ZoneID) + "|" + at.X + "|" + at.Y + "|" + E(target.ID) + "|" + E(moss.ID) + "|" + Count(moss) + "|" + E(state.Key);
            var row = actions.AddActionWithHint("KindleFuel", "light " + (state.Torch != null ? "torch" : "coals") + " (1 fire-moss)", command,
                "Spends one dry fire-moss to start the existing fuel. Adds no fuel; a nearby flame or usable flint is preferred.", '\0', 18, false, ActionTime.OneAction);
            if (WorldResourceActions.Ground(target, zone)) row.AtEntity(zone, target);
        }
        internal static bool TryAct(Entity actor, Entity target, Zone zone, string command, InventoryTransaction tx)
        {
            if (tx == null || !TargetState.TryCapture(actor, target, zone, out var state)) return false;
            string[] f = command.Split('|'); var at = zone.GetEntityCell(actor);
            if (f.Length != 8 || WorldResourceActions.Decode(f[1]) != zone.ZoneID
                || f[2] != at.X.ToString(CultureInfo.InvariantCulture) || f[3] != at.Y.ToString(CultureInfo.InvariantCulture)
                || WorldResourceActions.Decode(f[4]) != target.ID || WorldResourceActions.Decode(f[7]) != state.Key) return false;
            var moss = WorldResourceActions.ExactCarried(actor, f[5]);
            if (!Usable(actor, moss, zone) || f[6] != Count(moss).ToString(CultureInfo.InvariantCulture)
                || Select(actor, target, zone) != moss) return false;
            var pack = actor.GetPart<InventoryPart>(); var physics = moss.GetPart<PhysicsPart>();
            var parts = moss.Parts.ToArray(); int count = Count(moss);
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(target, actor, command) || !tx.TryClaim(moss, actor, command)) return false;
            var payment = InventoryTransferSnapshot.Capture(pack, moss); bool staged = false;
            int sourceIndex = pack.Objects.IndexOf(moss); var stack = moss.GetPart<StackerPart>();
            tx.Do(null, () =>
            {
                if (!staged) { payment.Restore(); return; }
                // Once payment is staged, refund one unit at the source's current
                // valid destination. Late independent additions/transfers stay.
                bool identity = moss.BlueprintName == "FireMoss" && moss.Parts.SequenceEqual(parts) && Coherent(moss)
                    && moss.GetPart<PhysicsPart>() == physics && moss.GetPart<StackerPart>() == stack;
                bool restored = false;
                if (identity && stack.StackCount > 0 && stack.StackCount < int.MaxValue)
                {
                    var ground = moss.SpatialZone; var carrier = physics.InInventory;
                    bool onGround = ground != null && WorldResourceActions.Ground(moss, ground)
                        && ground.GetReadOnlyEntities().Count(e => e.ID == moss.ID) == 1;
                    var inventory = carrier?.GetPart<InventoryPart>(); var container = carrier?.GetPart<ContainerPart>();
                    bool inInventory = ground == null && physics.Equipped == null && carrier != null
                        && (inventory?.Objects.Contains(moss) == true || container?.Contents.Contains(moss) == true)
                        && ((inventory?.Objects.Count(e => e?.ID == moss.ID) ?? 0) + (container?.Contents.Count(e => e?.ID == moss.ID) ?? 0)) == 1;
                    if (onGround || inInventory)
                    {
                        stack.StackCount++; inventory?.RefreshHandlingCarryPenalty(); restored = true;
                    }
                    else if (count == 1 && stack.StackCount == 1 && ground == null && carrier == null && physics.Equipped == null
                        && actor.GetPart<InventoryPart>() == pack && !pack.Objects.Any(e => e?.ID == moss.ID)
                        && !zone.GetReadOnlyEntities().Any(e => e.ID == moss.ID))
                    {
                        stack.StackCount = 1; pack.Objects.Insert(Math.Min(sourceIndex, pack.Objects.Count), moss);
                        physics.InInventory = actor; pack.RefreshHandlingCarryPenalty(); restored = true;
                    }
                }
                if (!restored) Diag.Record("event", "KindlingRefundIdentityChanged", actor, moss);
            });
            bool PaidCurrent() => WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && actor.GetPart<InventoryPart>() == pack && zone.GetEntityCell(actor) == at
                && moss.BlueprintName == "FireMoss" && moss.Parts.SequenceEqual(parts) && Coherent(moss) && Dry(moss)
                && (count > 1 ? Usable(actor, moss, zone) && Count(moss) == count - 1
                    : !pack.Objects.Contains(moss) && moss.SpatialZone == null && physics.InInventory == null && physics.Equipped == null);
            if (!payment.Apply(() => pack.TryConsumeOne(moss)) || !payment.ClaimChanges(tx, actor, command)
                || !PaidCurrent() || !state.Current(state.BeforeTemperature, false)) return false;
            staged = true;
            StageFlame(state, tx);
            tx.BeforeCommit(() => PaidCurrent() && state.Current(state.LightingTemperature, state.Torch != null));
            tx.AfterCommit(() =>
            {
                if (state.Torch != null) state.Torch.NotifyKindling(actor);
                else { target.GetPart<HearthGlowPart>()?.SyncLight(); ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(target), "KindlingLit"); }
                MessageLog.Add("You spend one fire-moss to light the " + (state.Torch != null ? "torch" : "coals") + ". Its own fuel keeps it burning.");
                Diag.Record("event", "KindlingLit", actor, target, new { source = moss.ID, remaining = count - 1 });
            });
            return true;
        }

        static void StageFlame(TargetState state, InventoryTransaction tx)
        {
            var target = state.Target;
            float addedHeat = state.LightingTemperature - state.BeforeTemperature;
            tx.Do(null, () =>
            {
                // Undo only our fields on their original live owners. Never restore
                // actor stats, reattach removed Parts or pull a forced drop back.
                if (target.GetPart<ThermalPart>() == state.Heat && state.Heat.ParentEntity == target
                    && WorldResourceActions.Finite(state.Heat.Temperature)) state.Heat.Temperature -= addedHeat;
                if (state.Torch != null && target.GetPart<LightSourcePart>() == state.Light && state.Light.ParentEntity == target)
                    state.Light.Enabled = false;
            });
            state.Heat.Temperature = state.LightingTemperature;
            if (state.Torch != null) state.Light.Enabled = true;
        }
        sealed class TargetState
        {
            internal Entity Actor, Target; internal Zone Zone, TargetZone; internal Cell Origin, TargetCell;
            internal Part[] Parts; internal PhysicsPart Physics; internal Entity InventoryOwner, EquippedOwner;
            internal TorchLightPart Torch; internal CampfirePart Hearth; internal FuelPart Fuel;
            internal ThermalPart Heat; internal LightSourcePart Light;
            internal float BeforeTemperature, LightingTemperature, FuelMass, MaxFuel, BurnRate, FlameTemperature;
            internal string Key => (Torch != null ? "torch" : "hearth") + ":" + N(BeforeTemperature) + ":" + N(FuelMass) + ":" + N(MaxFuel) + ":" + N(BurnRate) + ":" + N(FlameTemperature) + ":" + N(LightingTemperature);
            internal static bool TryCapture(Entity actor, Entity target, Zone zone, out TargetState state)
            {
                state = null;
                if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                    || !Coherent(target) || target.HasTag("Creature") || target.HasEffect<BrokenEffect>() || !Dry(target)
                    || target.HasEffect<BurningEffect>() || (target.GetPart<StackerPart>()?.StackCount ?? 1) != 1) return false;
                var torch = target.GetPart<TorchLightPart>(); var hearth = target.GetPart<CampfirePart>();
                var heat = target.GetPart<ThermalPart>(); var fuel = target.GetPart<FuelPart>(); var light = target.GetPart<LightSourcePart>();
                if (heat == null || fuel == null || !WorldResourceActions.Finite(heat.Temperature)
                    || !WorldResourceActions.Finite(heat.FlameTemperature) || !WorldResourceActions.Finite(fuel.FuelMass) || fuel.FuelMass <= 0
                    || !WorldResourceActions.Finite(fuel.MaxFuel) || fuel.MaxFuel <= 0 || fuel.FuelMass > fuel.MaxFuel
                    || !WorldResourceActions.Finite(fuel.BurnRate) || fuel.BurnRate <= 0) return false;
                if (torch != null ? hearth != null || !torch.CanReach(actor, zone) || !TorchReach(actor, target, zone) || light == null || light.Enabled
                    || !WorldResourceActions.Finite(torch.LightTemperature) || torch.LightTemperature < heat.FlameTemperature
                    : hearth == null || !CampfireWorkActions.Source(actor, hearth, zone) || !CampfireWorkActions.Fuel(hearth, out _) || CampfireWorkActions.Hot(hearth)) return false;
                state = new TargetState { Actor = actor, Target = target, Zone = zone, TargetZone = target.SpatialZone,
                    Origin = zone.GetEntityCell(actor), TargetCell = zone.GetEntityCell(target), Parts = target.Parts.ToArray(),
                    Physics = target.GetPart<PhysicsPart>(), InventoryOwner = target.GetPart<PhysicsPart>()?.InInventory,
                    EquippedOwner = target.GetPart<PhysicsPart>()?.Equipped, Torch = torch, Hearth = hearth, Heat = heat, Fuel = fuel, Light = light,
                    BeforeTemperature = heat.Temperature, LightingTemperature = torch?.LightTemperature ?? 450,
                    FuelMass = fuel.FuelMass, MaxFuel = fuel.MaxFuel, BurnRate = fuel.BurnRate, FlameTemperature = heat.FlameTemperature };
                return state.Physics != null;
            }
            internal bool Current(float temperature, bool enabled) => WorldResourceActions.ActorCurrent(Actor, Zone)
                && Zone.GetEntityCell(Actor) == Origin && Target.SpatialZone == TargetZone && Zone.GetEntityCell(Target) == TargetCell
                && Target.Parts.SequenceEqual(Parts) && Coherent(Target) && Dry(Target) && !Target.HasEffect<BrokenEffect>()
                && !Target.HasEffect<BurningEffect>() && (Target.GetPart<StackerPart>()?.StackCount ?? 1) == 1
                && Target.GetPart<PhysicsPart>() == Physics && Physics.InInventory == InventoryOwner && Physics.Equipped == EquippedOwner
                && Target.GetPart<ThermalPart>() == Heat && Heat.Temperature == temperature && Heat.FlameTemperature == FlameTemperature
                && Target.GetPart<FuelPart>() == Fuel && Fuel.FuelMass == FuelMass && Fuel.MaxFuel == MaxFuel && Fuel.BurnRate == BurnRate
                && (Torch != null ? Target.GetPart<TorchLightPart>() == Torch && Torch.LightTemperature == LightingTemperature
                    && Torch.CanReach(Actor, Zone) && TorchReach(Actor, Target, Zone) && Target.GetPart<LightSourcePart>() == Light && Light.Enabled == enabled
                    : Target.GetPart<CampfirePart>() == Hearth && CampfireWorkActions.Source(Actor, Hearth, Zone));
        }
    }
}
