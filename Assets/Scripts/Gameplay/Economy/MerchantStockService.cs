using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Two opt-in catalogues composed from existing goods. This is abstract
    /// merchant supply, not unloaded NPC production. All updates use game ticks.</summary>
    public static class MerchantStockService
    {
        sealed class Profile
        {
            internal readonly string Id;
            internal readonly string[] Staples, Choices;
            internal readonly int[] Floors;
            internal Profile(string id, string[] staples, int[] floors, string[] choices)
            { Id = id; Staples = staples; Floors = floors; Choices = choices; }
        }
        static readonly Profile Farmer = new Profile("FarmerStock",
            new[] { "CandyCarrotSeed", "EmberwheatSeed" }, new[] { 3, 2 },
            new[] { "KnotflaxSeed", "HearthbulbSeed", "SeamleafSeed", "PitchpodSeed" });
        static readonly Profile Armorer = new Profile("ArmorerStock",
            new[] { "LeatherArmor", "LeatherCap", "LeatherBoots" }, new[] { 1, 1, 1 },
            new[] { "FilterHood", "ColdwardCloak", "KilnfeltApron", "AcidworkerApron", "GroundwireScreen" });
        sealed class Activity
        {
            internal int Trades, Views;
            internal bool StockBusy;
            internal readonly List<WeakReference<BarterDeal>> Offers = new List<WeakReference<BarterDeal>>();
        }
        sealed class Lease : IDisposable
        {
            Action release;
            internal Lease(Action release) { this.release = release; }
            public void Dispose() { var action = release; release = null; action?.Invoke(); }
        }
        static readonly ConditionalWeakTable<Entity, Activity> Activities = new ConditionalWeakTable<Entity, Activity>();
        static Activity For(Entity merchant) => Activities.GetValue(merchant, _ => new Activity());

        static Profile ProfileFor(Entity merchant)
        {
            var trader = merchant?.GetPart<TraderPart>();
            if (merchant == null || trader?.ParentEntity != merchant || merchant.Parts.Count(p => p is TraderPart) != 1
                || merchant.GetProperty(TraderRestockSystem.ShopStockTableProp) != trader.StockTable) return null;
            return trader.StockTable == Farmer.Id ? Farmer : trader.StockTable == Armorer.Id ? Armorer : null;
        }
        internal static bool HasProfile(Entity merchant) => ProfileFor(merchant) != null;
        static bool Valid(Entity merchant, Zone zone, Profile profile)
        {
            var pack = merchant?.GetPart<InventoryPart>();
            return profile != null && ProfileFor(merchant) == profile && zone != null && merchant.SpatialZone == zone
                && zone.GetReadOnlyEntities().Contains(merchant) && merchant.HasTag("Creature")
                && pack?.ParentEntity == merchant && merchant.Parts.Count(p => p is InventoryPart) == 1
                && !string.IsNullOrEmpty(merchant.ID) && merchant.GetIntProperty(TradeSystem.CURRENCY_PROP, -1) >= 0
                && !TradeSystem.TraderUnableToTrade(merchant, out _);
        }
        static bool ValidState(MerchantStockPart state, Entity merchant, Profile profile) => state == null
            || state.ParentEntity == merchant && state.ProfileId == profile.Id && state.Generation >= 0
            && state.LastSpecialTick >= 0 && state.LastStapleTick >= 0 && state.Specials != null && state.SpecialKeys != null
            && state.Specials.Count <= 3 && state.Specials.Count == state.SpecialKeys.Count
            && state.Specials.All(e => e != null) && state.Specials.Distinct().Count() == state.Specials.Count
            && state.SpecialKeys.All(k => k == null || k.Length <= 16384);

        // A projection fingerprint grants only consignment withdrawal permission.
        // It never grants stacking, crafting or generic item serialization permission.
        static string Key(Entity owner, Entity item)
        {
            if (item == null || item.HasTag("NoTrade") || item.GetPart<CommercePart>()?.Value <= 0
                || item.GetPart<CommercePart>() == null || item.GetStatValue("Hitpoints", 1) <= 0) return null;
            return InventoryItemGroups.CaptureMerchantIdentity(owner, new InventoryScreenData.ItemDisplay
            { Item = item, Name = item.GetDisplayName(), Category = "", State = "", StackCount = BarterDeal.Units(item) });
        }
        public static bool IsSpecial(Entity merchant, Entity item)
        {
            var state = merchant?.GetPart<MerchantStockPart>();
            return item != null && state?.Specials != null && state.Specials.Count <= 3
                && state.Specials.Contains(item) && merchant.GetPart<InventoryPart>()?.Objects.Contains(item) == true
                && item.GetPart<PhysicsPart>()?.InInventory == merchant;
        }
        internal static bool ProtectsMerge(Entity owner, Entity first, Entity second) => IsSpecial(owner, first) || IsSpecial(owner, second);
        public static IDisposable BeginView(Entity merchant)
        {
            if (merchant == null) return new Lease(null);
            var activity = For(merchant); activity.Views++; return new Lease(() => activity.Views--);
        }
        internal static void TrackOffer(BarterDeal deal)
        {
            ForgetOffer(deal);
            if (deal?.Trader != null && !deal.IsEmpty) For(deal.Trader).Offers.Add(new WeakReference<BarterDeal>(deal));
        }
        public static void ForgetOffer(BarterDeal deal)
        {
            if (deal?.Trader == null || !Activities.TryGetValue(deal.Trader, out var activity)) return;
            activity.Offers.RemoveAll(w => !w.TryGetTarget(out var current) || ReferenceEquals(current, deal));
        }
        static bool HasOffer(Activity activity)
        {
            activity.Offers.RemoveAll(w => !w.TryGetTarget(out var deal) || deal.IsEmpty);
            return activity.Offers.Count != 0;
        }
        internal static bool EnterTrade(Entity merchant, InventoryTransaction transaction)
        {
            var activity = For(merchant); if (activity.StockBusy) return false;
            activity.Trades++;
            var lease = new Lease(() => activity.Trades--);
            transaction.Do(null, lease.Dispose); transaction.AfterCommit(lease.Dispose); return true;
        }
        internal static void ReleaseForTransfer(Entity merchant, Entity item, InventoryTransaction transaction)
        {
            var state = merchant?.GetPart<MerchantStockPart>();
            var members = state?.Specials; var keys = state?.SpecialKeys;
            if (members == null || keys == null || members.Count != keys.Count || members.Count > 3) return;
            int index = members.IndexOf(item); if (index < 0) return;
            string key = keys[index];
            transaction.Do(() => { members.RemoveAt(index); keys.RemoveAt(index); }, () =>
            {
                // Restore only this transfer's membership, preserving independent trades.
                if (state.Specials != members || state.SpecialKeys != keys || members.Contains(item)) return;
                int at = Math.Min(index, members.Count); members.Insert(at, item); keys.Insert(at, key);
            });
        }
        static bool OvenBonus(Profile profile, Zone zone)
        {
            if (profile != Farmer || zone == null || SettlementManager.Current == null
                || !SettlementManager.Current.TryGetExistingSite(zone.ZoneID, SettlementSiteDefinitions.VillageOvenSiteId, out var oven)) return false;
            return oven.Stage == RepairStage.StableRepair || oven.Stage == RepairStage.ImprovedWithCaretaker;
        }
        static uint StableHash(string value)
        { uint h = 2166136261; foreach (char c in value) { h = unchecked((h ^ c) * 16777619); } return h; }
        static string[] Selection(Profile profile, Entity merchant, int generation, bool bonus)
        {
            int start = (int)((StableHash(merchant.ID + ":" + profile.Id) + (ulong)generation) % (ulong)profile.Choices.Length);
            var result = new List<string> { profile.Choices[start], profile.Choices[(start + 1) % profile.Choices.Length] };
            if (bonus) result.Add("RoastedHearthbulb"); return result.ToArray();
        }
        static long UsableUnits(Entity merchant, string blueprint, string key)
        {
            long count = 0;
            foreach (var item in merchant.GetPart<InventoryPart>().Objects)
                if (item?.BlueprintName == blueprint && key != null && Key(merchant, item) == key) count += BarterDeal.Units(item);
            return count;
        }
        static bool AddStaple(InventoryPart pack, Entity item, string key)
        {
            // Use ordinary stacking only where every potential recipient has the
            // same audited base payload. Modified/unknown sold goods stay separate.
            foreach (var existing in pack.Objects)
                if (existing?.GetPart<StackerPart>()?.CanStackWith(item) == true && Key(pack.ParentEntity, existing) != key)
                    return pack.AddRetrievedObject(item);
            return pack.AddObject(item);
        }

        /// <summary>Authoritative arrival/shop-open only. Repaint, load and stock
        /// enumeration never call this. One missed window creates at most one lot.</summary>
        public static bool Reconcile(Entity merchant, Zone zone, int tick)
        {
            var profile = ProfileFor(merchant); var factory = TraderRestockSystem.Factory;
            if (tick < 0 || factory == null || !Valid(merchant, zone, profile)) return false;
            var activity = For(merchant);
            if (activity.StockBusy || activity.Trades > 0 || activity.Views > 0 || HasOffer(activity)) return false;
            var state = merchant.GetPart<MerchantStockPart>();
            if (merchant.Parts.Count(p => p is MerchantStockPart) > 1 || !ValidState(state, merchant, profile)) return false;
            bool staplesDue = state == null || (long)tick - state.LastStapleTick > TraderRestockSystem.RestockIntervalTurns;
            bool specialDue = state == null || (long)tick - state.LastSpecialTick >= WorldClock.DayLengthTicks;
            if (!staplesDue && !specialDue || specialDue && state?.Generation == int.MaxValue) return false;
            var pack = merchant.GetPart<InventoryPart>();
            var transaction = new InventoryTransaction();
            activity.StockBusy = true;
            try
            {
                if (!transaction.TryClaim(merchant, merchant, "MerchantConsignment")) return false;
                var expected = state == null ? null : new StateReceipt(state);
                var products = new List<Entity>(); var newSpecials = new List<Entity>(); var newKeys = new List<string>();
                var productKeys = new Dictionary<Entity, string>(); var productCounts = new Dictionary<Entity, int>();
                var productIds = new Dictionary<Entity, string>();
                var stapleKeys = new string[profile.Staples.Length]; var heldUnits = new long[profile.Staples.Length];
                var suppliedUnits = new int[profile.Staples.Length];
                // A private staging owner makes the existing audited inventory key usable
                // before any real shelf mutation. It is never registered in a world/save.
                var staging = new Entity(); var stagedPack = new InventoryPart { MaxWeight = -1 }; staging.AddPart(stagedPack);
                Func<string, Entity> create = bp =>
                {
                    if (!factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Missing consignment good.");
                    var item = factory.CreateEntity(bp);
                    var physics = item?.GetPart<PhysicsPart>();
                    if (item == null || item.BlueprintName != bp || string.IsNullOrEmpty(item.ID)
                        || item.ID == merchant.ID || pack.Objects.Concat(stagedPack.Objects).Any(i => i?.ID == item.ID)
                        || zone.GetReadOnlyEntities().Any(i => i?.ID == item.ID)
                        || BarterDeal.Units(item) != 1 || item.SpatialZone != null || physics?.ParentEntity != item
                        || item.Parts.Any(p => p?.ParentEntity != item) || physics.InInventory != null || physics.Equipped != null
                        || !stagedPack.AddRetrievedObject(item) || Key(staging, item) == null)
                        throw new InvalidOperationException("Unsupported consignment payload.");
                    return item;
                };
                if (staplesDue)
                    for (int n = 0; n < profile.Staples.Length; n++)
                    {
                        var sample = create(profile.Staples[n]); string sampleKey = Key(staging, sample);
                        long held = UsableUnits(merchant, profile.Staples[n], sampleKey);
                        stapleKeys[n] = sampleKey; heldUnits[n] = held;
                        int missing = (int)Math.Max(0, profile.Floors[n] - held);
                        if (missing == 0) continue;
                        var stack = sample.GetPart<StackerPart>();
                        if (missing > 1 && (stack == null || stack.MaxStack < missing)) throw new InvalidOperationException("Invalid staple stack.");
                        if (stack != null) stack.StackCount = missing;
                        products.Add(sample); productKeys.Add(sample, Key(staging, sample)); productCounts.Add(sample, missing);
                        productIds.Add(sample, sample.ID); suppliedUnits[n] = missing;
                    }
                int generation = state == null ? 0 : state.Generation + (specialDue ? 1 : 0);
                bool bonus = specialDue && OvenBonus(profile, zone);
                string[] selected = specialDue ? Selection(profile, merchant, generation, bonus) : null;
                if (specialDue)
                    foreach (string bp in selected)
                    {
                        var item = create(bp); products.Add(item); newSpecials.Add(item); string key = Key(staging, item); newKeys.Add(key);
                        productKeys.Add(item, key); productCounts.Add(item, 1); productIds.Add(item, item.ID);
                    }
                // Factory callbacks may have changed independent state. Preserve that
                // work and refuse this update before touching any shelf or timestamp.
                if (!Valid(merchant, zone, profile) || TraderRestockSystem.Factory != factory
                    || merchant.GetPart<InventoryPart>() != pack || merchant.GetPart<MerchantStockPart>() != state
                    || activity.Views > 0 || activity.Trades > 0 || HasOffer(activity)
                    || specialDue && OvenBonus(profile, zone) != bonus
                    || staplesDue && Enumerable.Range(0, profile.Staples.Length).Any(n => UsableUnits(merchant, profile.Staples[n], stapleKeys[n]) != heldUnits[n])
                    || products.Any(i => i.ID != productIds[i] || BarterDeal.Units(i) != productCounts[i] || Key(staging, i) != productKeys[i])
                    || expected != null && !expected.Matches(state)) return false;
                var retire = new List<Entity>();
                if (specialDue && state != null)
                    for (int n = 0; n < state.Specials.Count; n++)
                    {
                        var item = state.Specials[n];
                        if (!string.IsNullOrEmpty(state.SpecialKeys[n]) && BarterDeal.Units(item) == 1
                            && Key(merchant, item) == state.SpecialKeys[n]) retire.Add(item);
                    }
                foreach (var item in retire.Concat(products))
                    if (!transaction.TryClaim(item, merchant, "MerchantConsignment")) return false;
                var receipt = InventoryTransferSnapshot.Capture(pack, products.ToArray());
                transaction.Do(null, receipt.Restore);
                if (!receipt.Apply(() =>
                {
                    foreach (var item in retire) if (!pack.RemoveObject(item)) return false;
                    foreach (var item in products)
                    {
                        if (!stagedPack.RemoveObject(item)) return false;
                        if (newSpecials.Contains(item) ? !pack.AddRetrievedObject(item) : !AddStaple(pack, item, productKeys[item])) return false;
                    }
                    return true;
                }) || !receipt.ClaimChanges(transaction, merchant, "MerchantConsignment")) return false;
                var installed = state ?? new MerchantStockPart { ProfileId = profile.Id };
                var metadata = state == null ? null : new StateReceipt(state);
                transaction.Do(() =>
                {
                    if (state == null) merchant.AddPart(installed);
                    if (staplesDue) installed.LastStapleTick = tick;
                    if (specialDue)
                    {
                        installed.Generation = generation; installed.LastSpecialTick = tick;
                        installed.Specials = newSpecials; installed.SpecialKeys = newKeys;
                        installed.SelectionRaw = string.Join("|", selected); installed.BonusApplied = bonus;
                    }
                }, () => { if (state == null) merchant.RemovePart(installed); else metadata.Restore(installed); });
                transaction.BeforeCommit(() => Valid(merchant, zone, profile) && TraderRestockSystem.Factory == factory
                    && merchant.GetPart<InventoryPart>() == pack && merchant.GetPart<MerchantStockPart>() == installed
                    && activity.Views == 0 && activity.Trades == 0 && !HasOffer(activity)
                    && (!specialDue || OvenBonus(profile, zone) == bonus)
                    && newSpecials.All(i => pack.Objects.Contains(i) && i.ID == productIds[i] && BarterDeal.Units(i) == productCounts[i]
                        && productKeys[i] != null && Key(merchant, i) == productKeys[i])
                    && (!staplesDue || Enumerable.Range(0, profile.Staples.Length).All(n =>
                        UsableUnits(merchant, profile.Staples[n], stapleKeys[n]) == heldUnits[n] + suppliedUnits[n]))
                    && (!specialDue || newSpecials.Select((i, n) => BarterDeal.Units(i) == 1 && Key(merchant, i) == newKeys[n]).All(v => v)));
                transaction.Commit(); return true;
            }
            catch (Exception error)
            {
                CavesOfOoo.Diagnostics.Diag.Record("trade", "ConsignmentDeferred", actor: merchant,
                    payload: new { reason = error.GetType().Name });
                return false;
            }
            finally { if (!transaction.IsCommitted) transaction.Rollback(); activity.StockBusy = false; }
        }
        sealed class StateReceipt
        {
            readonly string profile, selection; readonly int generation, specialTick, stapleTick; readonly bool bonus;
            readonly List<Entity> members; readonly List<string> keys;
            readonly Entity[] items; readonly string[] payloads;
            internal StateReceipt(MerchantStockPart s)
            {
                profile = s.ProfileId; selection = s.SelectionRaw; generation = s.Generation; specialTick = s.LastSpecialTick;
                stapleTick = s.LastStapleTick; bonus = s.BonusApplied; members = s.Specials; keys = s.SpecialKeys;
                items = members.ToArray(); payloads = keys.ToArray();
            }
            internal bool Matches(MerchantStockPart s) => s != null && s.ProfileId == profile && s.Generation == generation
                && s.LastSpecialTick == specialTick && s.LastStapleTick == stapleTick && s.SelectionRaw == selection
                && s.BonusApplied == bonus && s.Specials == members && s.SpecialKeys == keys
                && members.SequenceEqual(items) && keys.SequenceEqual(payloads);
            internal void Restore(MerchantStockPart s)
            {
                s.ProfileId = profile; s.SelectionRaw = selection; s.Generation = generation; s.LastSpecialTick = specialTick;
                s.LastStapleTick = stapleTick; s.BonusApplied = bonus; s.Specials = members; s.SpecialKeys = keys;
            }
        }
        public static string Describe(Entity merchant)
        {
            var state = merchant?.GetPart<MerchantStockPart>(); if (state == null) return "";
            return "* Specials change after a game day. Staples return periodically.";
        }
        public static string ProjectBenefit(Entity merchant)
        {
            var state = merchant?.GetPart<MerchantStockPart>();
            if (state?.ProfileId != Farmer.Id) return "";
            if (state.BonusApplied) return "Oven baking included: an extra meal in each new consignment.";
            return OvenBonus(Farmer, merchant.SpatialZone) ? "Oven repaired: baking joins the next consignment."
                : "Repair the local oven to add baking to future consignments.";
        }
    }
}
