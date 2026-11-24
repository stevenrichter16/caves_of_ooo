using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Optional, explicit finite fuel. Alchemy heat is not physical
    /// fuel permission; only a completely parsed, stable combustible reagent
    /// qualifies. No saved state and no changes to legacy timber or ignition.</summary>
    internal static class ReagentFireFuel
    {
        internal const int FuelPerTier = 5;
        const string Prefix = "FeedReagentFuel|";
        static readonly HashSet<Type> SimpleParts = new HashSet<Type> {
            typeof(RenderPart), typeof(PhysicsPart), typeof(ExaminablePart), typeof(StackerPart),
            typeof(CommercePart), typeof(HandlingPart), typeof(ReagentPart), typeof(TorchRefuelPart), typeof(MaterialPart)
        };
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        static string E(string value) => Uri.EscapeDataString(value ?? "");
        static string N(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        static int Count(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static float Gain(FuelPart fuel, int tier)
        {
            float offered = tier * FuelPerTier;
            float result = Math.Min(fuel.MaxFuel, fuel.FuelMass + offered);
            float gain = result - fuel.FuelMass;
            return WorldResourceActions.Finite(gain) && gain > 0 && gain <= offered ? gain : 0;
        }
        static bool Coherent(Entity item) => item != null && item.Parts.All(p => p != null && p.ParentEntity == item)
            && item.Parts.GroupBy(p => p.GetType()).All(g => g.Count() == 1);

        static int Tier(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 256) return 0;
            var entries = raw.Split(new[] { ',', ';', '|' }); if (entries.Length > 8) return 0;
            var seen = new HashSet<string>(StringComparer.Ordinal); int fuel = 0;
            foreach (var entry in entries)
            {
                var pieces = entry.Trim().Split(':'); if (pieces.Length < 1 || pieces.Length > 2) return 0;
                string name = pieces[0].Trim().ToLowerInvariant(); int tier = 1;
                if (name.Length == 0 || !seen.Add(name) || pieces.Length == 2
                    && !int.TryParse(pieces[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out tier)
                    || tier < 1 || tier > 3) return 0;
                // Unknown mixtures are not silently assumed to burn steadily.
                if (name != BrewProperties.Combustible && name != BrewProperties.Heat && name != BrewProperties.Viscous) return 0;
                if (name == BrewProperties.Combustible) fuel = tier;
            }
            return fuel;
        }
        static bool Plain(Entity item) => Coherent(item) && item.HasTag("Item") && !item.HasTag("Creature") && !item.HasTag("Terrain")
            && item.Statistics.Count == 0 && item.Properties.Count == 0 && item.IntProperties.Count == 0
            && item.Parts.All(p => SimpleParts.Contains(p.GetType()) || MaterialProcessPayloadPolicies.IsInertTracking(p));
        static bool Input(Entity actor, Entity item, Zone zone, out int tier)
        {
            tier = 0;
            if (!Plain(item) || item.GetPart<ReagentPart>()?.GetType() != typeof(ReagentPart)
                || !WorldResourceActions.Carried(actor, item, false) || !actor.GetPart<InventoryPart>().Objects.Contains(item)
                || zone.GetReadOnlyEntities().Any(e => e.ID == item.ID)) return false;
            tier = Tier(item.GetPart<ReagentPart>().PropertiesRaw); return tier > 0;
        }
        static bool Target(Entity actor, CampfirePart fire, Zone zone, out FuelPart fuel)
        {
            fuel = null;
            return WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && fire != null && Coherent(fire.ParentEntity) && !fire.ParentEntity.HasEffect<BrokenEffect>()
                && CampfireWorkActions.Source(actor, fire, zone) && CampfireWorkActions.Fuel(fire, out fuel)
                && !string.IsNullOrEmpty(fire.ParentEntity.ID) && zone.GetReadOnlyEntities().Count(e => e.ID == fire.ParentEntity.ID) == 1
                && WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0 && fuel.FuelMass < fuel.MaxFuel;
        }
        static string Key(CampfirePart fire, FuelPart fuel) => N(fuel.FuelMass) + ":" + N(fuel.MaxFuel) + ":" + N(fuel.BurnRate)
            + ":" + N(fire.ParentEntity.GetPart<ThermalPart>().Temperature);

        internal static void DescribeUse(Entity item, UseGuide guide)
        {
            int tier = Tier(item?.GetPart<ReagentPart>()?.PropertiesRaw); if (tier == 0) return;
            guide.How.Add("Stand beside a finite cooking fire, press C on the fire and choose feed coals with this reagent.");
            guide.Needs.Add("one dry, plain, stable reagent; its combustible property supplies " + tier * FuelPerTier + " fuel per item");
            guide.Limits.Add("Feeding adds no flame. The menu states the actual gain; surplus beyond the fire's capacity is lost. Use dry, untreated fuel. Volatile mixtures need another use.");
        }

        internal static void AddActions(Entity actor, CampfirePart fire, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Target(actor, fire, zone, out var fuel)) return;
            var at = zone.GetEntityCell(actor);
            foreach (var item in actor.GetPart<InventoryPart>().Objects)
            {
                if (!Input(actor, item, zone, out int tier))
                {
                    if (WorldResourceActions.Carried(actor, item, false) && item.GetPart<ReagentPart>()?.PropertiesRaw?.IndexOf(BrewProperties.Combustible, StringComparison.OrdinalIgnoreCase) >= 0)
                        actions.AddDisabled("FeedReagentFuel", "feed coals (1 " + InventoryPart.GetUnitDisplayName(item) + ")", "fuel_unsuitable",
                            "These coals need a dry, plain combustible ingredient. Volatile or treated mixtures are unsuitable.", '\0', 18);
                    continue;
                }
                float offered = tier * FuelPerTier, gain = Gain(fuel, tier); if (gain <= 0) continue;
                string command = Prefix + E(zone.ZoneID) + "|" + at.X + "|" + at.Y + "|" + E(fire.ParentEntity.ID) + "|" + E(item.ID)
                    + "|" + Count(item) + "|" + E(item.GetPart<ReagentPart>().PropertiesRaw) + "|" + E(Key(fire, fuel));
                actions.AddActionWithHint("FeedReagentFuel", "feed coals (1 " + InventoryPart.GetUnitDisplayName(item) + ", +" + N(gain) + " fuel)", command,
                    "Spends exactly one dry, stable reagent. Adds fuel without lighting it. " + N(offered - gain) + " fuel is discarded beyond capacity; timber and other uses remain separate.",
                    '\0', 18, false, ActionTime.OneAction).AtEntity(zone, fire.ParentEntity);
            }
        }
        internal static bool TryAct(Entity actor, CampfirePart fire, Zone zone, string command, InventoryTransaction tx)
        {
            var owner = fire?.ParentEntity;
            bool Refuse(string reason) { Diag.Record("event", "ReagentFuelRefused", actor, owner, new { reason }); return false; }
            if (tx == null || !Target(actor, fire, zone, out var fuel)) return Refuse("unavailable-fire");
            var fields = command.Split('|'); var at = zone.GetEntityCell(actor);
            if (fields.Length != 9 || WorldResourceActions.Decode(fields[1]) != zone.ZoneID
                || fields[2] != at.X.ToString(CultureInfo.InvariantCulture) || fields[3] != at.Y.ToString(CultureInfo.InvariantCulture)
                || WorldResourceActions.Decode(fields[4]) != owner.ID || WorldResourceActions.Decode(fields[8]) != Key(fire, fuel)) return Refuse("stale-fire");
            var item = WorldResourceActions.ExactCarried(actor, fields[5]);
            if (!Input(actor, item, zone, out int tier) || fields[6] != Count(item).ToString(CultureInfo.InvariantCulture)
                || WorldResourceActions.Decode(fields[7]) != item.GetPart<ReagentPart>().PropertiesRaw) return Refuse("stale-input");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command) || !tx.TryClaim(item, actor, command)) return Refuse("in-progress");
            var pack = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var stack = item.GetPart<StackerPart>();
            var parts = item.Parts.ToArray(); var fireParts = owner.Parts.ToArray(); var heat = owner.GetPart<ThermalPart>();
            var targetAt = zone.GetEntityCell(owner); string raw = item.GetPart<ReagentPart>().PropertiesRaw, blueprint = item.BlueprintName;
            float before = fuel.FuelMass, maximum = fuel.MaxFuel, burn = fuel.BurnRate, temperature = heat.Temperature;
            float gain = Gain(fuel, tier); if (gain <= 0) return Refuse("unrepresentable-gain");
            int count = Count(item), index = pack.Objects.IndexOf(item); string sourceId = item.ID, fireId = owner.ID;
            bool staged = false; var payment = InventoryTransferSnapshot.Capture(pack, item);
            tx.Do(null, () =>
            {
                if (!staged) { payment.Restore(); return; }
                bool restored = false;
                if (item.ID == sourceId && item.BlueprintName == blueprint && item.Parts.SequenceEqual(parts) && Plain(item)
                    && item.GetPart<ReagentPart>().PropertiesRaw == raw
                    && item.GetPart<PhysicsPart>() == physics && item.GetPart<StackerPart>() == stack)
                {
                    var carrier = physics.InInventory; var inventory = carrier?.GetPart<InventoryPart>(); var container = carrier?.GetPart<ContainerPart>();
                    var ground = item.SpatialZone;
                    bool grounded = ground != null && WorldResourceActions.Ground(item, ground) && ground.GetReadOnlyEntities().Count(e => e.ID == item.ID) == 1;
                    bool held = ground == null && physics.Equipped == null && carrier != null
                        && ((inventory?.Objects.Count(e => e?.ID == item.ID && e == item) ?? 0) + (container?.Contents.Count(e => e?.ID == item.ID && e == item) ?? 0)) == 1
                        && ((inventory?.Objects.Count(e => e?.ID == item.ID) ?? 0) + (container?.Contents.Count(e => e?.ID == item.ID) ?? 0)) == 1;
                    if (stack != null && stack.StackCount > 0 && stack.StackCount < int.MaxValue && (grounded || held))
                    { stack.StackCount++; inventory?.RefreshHandlingCarryPenalty(); restored = true; }
                    else if (count == 1 && Count(item) == 1 && ground == null && carrier == null && physics.Equipped == null
                        && actor.GetPart<InventoryPart>() == pack && !pack.Objects.Any(e => e?.ID == item.ID) && !zone.GetReadOnlyEntities().Any(e => e.ID == item.ID))
                    { if (stack != null) stack.StackCount = 1; pack.Objects.Insert(Math.Min(index, pack.Objects.Count), item); physics.InInventory = actor; pack.RefreshHandlingCarryPenalty(); restored = true; }
                }
                if (!restored) Diag.Record("event", "ReagentFuelRefundIdentityChanged", actor, item);
            });
            bool PaidCurrent() => WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && actor.GetPart<InventoryPart>() == pack && zone.GetEntityCell(actor) == at
                && item.ID == sourceId && item.BlueprintName == blueprint && item.Parts.SequenceEqual(parts) && Plain(item) && item.GetPart<ReagentPart>().PropertiesRaw == raw
                && (count > 1 ? Input(actor, item, zone, out _) && Count(item) == count - 1
                    : Count(item) == 1 && !pack.Objects.Contains(item) && item.SpatialZone == null && physics.InInventory == null && physics.Equipped == null);
            bool FireCurrent(float amount) => WorldResourceActions.ActorCurrent(actor, zone) && zone.GetEntityCell(actor) == at
                && owner.ID == fireId && owner.SpatialZone == zone && zone.GetEntityCell(owner) == targetAt && owner.Parts.SequenceEqual(fireParts) && Coherent(owner)
                && zone.GetReadOnlyEntities().Count(e => e.ID == fireId) == 1
                && owner.GetPart<CampfirePart>() == fire && fire.FiniteCooking && CampfireWorkActions.Source(actor, fire, zone)
                && owner.GetPart<FuelPart>() == fuel && fuel.ParentEntity == owner && fuel.FuelMass == amount && fuel.MaxFuel == maximum && fuel.BurnRate == burn
                && owner.GetPart<ThermalPart>() == heat && heat.Temperature == temperature && !owner.HasEffect<BrokenEffect>();
            if (!payment.Apply(() => pack.TryConsumeOne(item)) || !payment.ClaimChanges(tx, actor, command)
                || !PaidCurrent() || !FireCurrent(before)) return Refuse("changed-during-payment");
            staged = true;
            tx.Do(() => fuel.FuelMass += gain, () =>
            {
                if (owner.GetPart<FuelPart>() == fuel && fuel.ParentEntity == owner && WorldResourceActions.Finite(fuel.FuelMass))
                { if (fuel.FuelMass < gain) Diag.Record("event", "ReagentFuelRefundShortfall", actor, owner); fuel.FuelMass = Math.Max(0, fuel.FuelMass - gain); }
            });
            tx.BeforeCommit(() => PaidCurrent() && FireCurrent(before + gain));
            tx.AfterCommit(() =>
            {
                owner.GetPart<HearthGlowPart>()?.SyncLight(); ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(owner), "ReagentFuelFed");
                MessageLog.Add("You feed one " + InventoryPart.GetUnitDisplayName(item) + " to the coals: +" + N(gain) + " fuel. Feeding adds no flame.");
                Diag.Record("event", "ReagentFuelFed", actor, owner, new { source = item.ID, remaining = count - 1, gain });
            });
            return true;
        }
    }
}
