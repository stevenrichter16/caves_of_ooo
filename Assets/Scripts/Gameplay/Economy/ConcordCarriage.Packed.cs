using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public static partial class ConcordCarriage
    {
        static readonly ConditionalWeakTable<Entity, PackedParcel> PackedQuotes = new();
        const string PackedChanged = "The satchel, its contents or the clerk changed. Ask for a fresh quote.";

        static bool IsPackedSatchel(Entity item) => item?.BlueprintName == "FieldSatchel"
            && item.GetPart<ContainerPart>()?.Contents?.Count > 0;

        static string RememberPackedQuote(Entity player, PackedParcel parcel, string destination)
        {
            parcel.Destination = destination;
            PackedQuotes.Remove(player);
            PackedQuotes.Add(player, parcel);
            return parcel.Token;
        }

        static string CheckPacked(Entity player, Entity keeper, Entity item, out PackedParcel parcel)
        {
            parcel = PackedParcel.Capture(player, keeper, item, carried: true);
            if (parcel == null) return PackedChanged;
            var transaction = new InventoryTransaction();
            try
            {
                return parcel.Claim(transaction) ? parcel.CheckSending() : PackedChanged;
            }
            finally { transaction.Rollback(); }
        }

        static string SendPacked(Entity keeper, Entity player, Entity item, Post here, Post to, string[] parts)
        {
            if (parts.Length >= 3 && parts[2] != "1") return Refuse(keeper, player, "invalid_argument", "The clerk takes the whole satchel.");
            PackedParcel parcel;
            if (parts.Length == 4)
            {
                if (!PackedQuotes.TryGetValue(player, out parcel) || parcel.Token != parts[3]
                    || parcel.Item != item || parcel.Keeper != keeper || parcel.Destination != to.ZoneId)
                    return Refuse(keeper, player, "packed_quote_changed", PackedChanged);
                PackedQuotes.Remove(player);
            }
            else parcel = PackedParcel.Capture(player, keeper, item, carried: true);
            if (parcel == null) return Refuse(keeper, player, "packed_quote_changed", PackedChanged);

            var transaction = new InventoryTransaction();
            try
            {
                string refusal = parcel.Claim(transaction) ? parcel.CheckSending() : PackedChanged;
                if (refusal != null) return Refuse(keeper, player, "not_carried_by_cart", refusal);
                if (GateRefusal(player) is string closed) return Refuse(keeper, player, "ledger_closed", closed);
                int fee = Fee(item);
                if (!parcel.Current(true) || !parcel.CargoAdmitted() || parcel.CartCopies() != 0)
                    return Refuse(keeper, player, "packed_quote_changed", PackedChanged);
                if (!Ledger.StageDebit(player, fee, "concord-carriage", transaction))
                    return Refuse(keeper, player, "insufficient_drams", "The fee is " + fee + " drams.");
                var transfer = InventoryTransferSnapshot.Capture(parcel.Pack, item);
                transaction.Do(null, () => { if (parcel.ShellIn(null)) transfer.Restore(); });
                if (!transfer.Apply(() => parcel.Pack.RemoveObject(item)) || !parcel.Current(false))
                    return Refuse(keeper, player, "packed_quote_changed", PackedChanged);

                var carriage = player.GetPart<ConcordCarriagePart>();
                if (carriage == null)
                {
                    carriage = new ConcordCarriagePart();
                    transaction.Do(() => player.AddPart(carriage), () => player.RemovePart(carriage));
                }
                int index = carriage.Count;
                int now = WorldClock.CurrentTick, ready = now + Days(here, to) * WorldClock.DayLengthTicks;
                transaction.Do(() => carriage.Add(item, here.ZoneId, to.ZoneId, now, ready, fee), () => carriage.RemoveAt(index));
                transaction.BeforeCommit(() => parcel.Current(false) && player.GetPart<ConcordCarriagePart>() == carriage
                    && parcel.CargoAdmitted() && parcel.CartCopies() == 1 && carriage.Whole(index) && carriage.Goods[index] == item);
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add("The clerk seals " + item.GetDisplayName() + " for " + to.Name + ". "
                        + Days(here, to) + " days. Fee " + fee + " drams.");
                    Record("CarriageSent", keeper, player, new { item = item.BlueprintName, count = 1, from = here.ZoneId, to = to.ZoneId, fee, ready, packed = true });
                });
                transaction.Commit();
                return null;
            }
            finally { transaction.Rollback(); }
        }

        static bool CollectPacked(Entity keeper, Entity player, ConcordCarriagePart carriage, int index, int now, out bool overweight)
        {
            overweight = false;
            var item = carriage.Goods[index];
            var parcel = PackedParcel.Capture(player, keeper, item, carried: false);
            if (parcel == null || parcel.CartCopies() != 1) return false;
            overweight = !CarriedStorage.CanAcceptNewLoad(parcel.Pack, item);
            if (overweight) return false;

            var transaction = new InventoryTransaction();
            try
            {
                if (!parcel.Claim(transaction)) return false;
                var transfer = InventoryTransferSnapshot.Capture(parcel.Pack, item);
                transaction.Do(null, () => { if (parcel.ShellIn(player)) transfer.Restore(); });
                if (!transfer.Apply(() => parcel.Pack.AddObject(item)) || !parcel.Current(true)
                    || player.GetPart<ConcordCarriagePart>() != carriage || !carriage.Whole(index)
                    || carriage.Goods[index] != item || carriage.To[index] != keeper.SpatialZone.ZoneID || now < carriage.ReadyTick[index])
                    return false;
                carriage.RemoveAt(index);
                transaction.Commit();
                return true;
            }
            finally { transaction.Rollback(); }
        }

        sealed class PackedParcel
        {
            internal readonly string Token = Guid.NewGuid().ToString("N");
            internal string Destination;
            internal readonly Entity Item, Keeper;
            internal readonly InventoryPart Pack;
            internal readonly int Weight;
            readonly Entity player;
            readonly Zone zone;
            readonly Cell playerCell, keeperCell;
            readonly ContainerPart container;
            readonly Entity[] contents;
            readonly Member[] manifest;
            readonly HashSet<string> itemIds;

            PackedParcel(Entity player, Entity keeper, Entity item)
            {
                this.player = player;
                Item = item;
                Keeper = keeper;
                Pack = player.GetPart<InventoryPart>();
                zone = keeper.SpatialZone;
                playerCell = zone.GetEntityCell(player);
                keeperCell = zone.GetEntityCell(keeper);
                container = item.GetPart<ContainerPart>();
                contents = container.Contents.ToArray();
                manifest = contents.Prepend(item).Select(owner => new Member(owner)).ToArray();
                itemIds = new HashSet<string>(manifest.Select(member => member.Id), StringComparer.Ordinal);
                Weight = HandlingService.GetWeight(item);
            }

            internal string ContentsText => string.Join(", ", contents.Select(owner => owner.GetDisplayName()));

            internal static PackedParcel Capture(Entity player, Entity keeper, Entity item, bool carried)
            {
                if (!IsPackedSatchel(item) || keeper?.SpatialZone == null || player?.GetPart<InventoryPart>() is not InventoryPart pack
                    || !CarriedStorage.HasUnaliasedLooseContents(pack, item)) return null;
                var parcel = new PackedParcel(player, keeper, item);
                return parcel.Current(carried) ? parcel : null;
            }

            internal bool Claim(InventoryTransaction transaction) => manifest.All(part => transaction.TryClaim(part.Owner, player, "concord-packed-carriage"));

            internal bool ShellIn(Entity owner) => Item.SpatialZone == null
                && Item.GetPart<PhysicsPart>() is { Equipped: null } physics && physics.InInventory == owner;

            internal int CartCopies() => player.GetPart<ConcordCarriagePart>()?.Goods.Count(owner =>
                owner != null && (itemIds.Contains(owner.ID) || owner != Item
                    && owner.GetPart<ContainerPart>()?.Contents?.Any(child => child != null && itemIds.Contains(child.ID)) == true)) ?? 0;

            internal bool Current(bool carried)
            {
                if (player.GetPart<InventoryPart>() != Pack || !PreparationActions.CurrentActor(player, zone)
                    || !WorldResourceActions.Nearby(player, Keeper, zone) || KeeperRefusal(Keeper) != null
                    || zone.GetEntityCell(player) != playerCell || zone.GetEntityCell(Keeper) != keeperCell
                    || !AtPost(Keeper, out _) || Item.GetPart<ContainerPart>() != container
                    || !container.Contents.SequenceEqual(contents) || !CarriedStorage.HasUnaliasedLooseContents(Pack, Item)
                    || manifest.Any(member => !member.Matches()) || HandlingService.GetWeight(Item) != Weight
                    || zone.GetReadOnlyEntities().Any(owner => itemIds.Contains(owner.ID))) return false;
                if (carried) return WorldResourceActions.Carried(player, Item);
                return ShellIn(null) && !Pack.Objects.Concat(Pack.EquippedItems.Values).Any(owner => owner?.ID == Item.ID)
                    && Pack.FindEquippedBodyPart(Item) == null;
            }

            internal string CheckSending()
            {
                if (!Current(true)) return PackedChanged;
                foreach (var member in manifest)
                {
                    var owner = member.Owner;
                    if (CargoRefusal(owner) is string refusal) return refusal;
                    bool canSend;
                    try
                    {
                        canSend = owner == Item ? TradeSystem.CanEntrustPackedShell(owner, player, Keeper)
                            : TradeSystem.CanBeTraded(owner, player, Keeper, "Carriage");
                    }
                    catch (Exception error)
                    {
                        Diagnostics.Diag.Record("trade", "PackedCarriageVetoFailed", actor: player, target: owner,
                            payload: new { error = error.GetType().Name, error.Message });
                        return PackedChanged;
                    }
                    if (!canSend) return owner.GetDisplayName() + " cannot change hands.";
                    if (!Current(true)) return PackedChanged;
                }
                foreach (var member in manifest)
                    if (CargoRefusal(member.Owner) is string refusal) return refusal;
                return null;
            }

            internal bool CargoAdmitted() => manifest.All(member => CargoRefusal(member.Owner) == null);

            static string CargoRefusal(Entity item)
            {
                if (MaterialProcessService.IsProtected(item) || ContainedLiquidSourcePart.IsRecoveryCustodyProtected(item))
                    return item.GetDisplayName() + " is held for work in progress.";
                return item.HasTag("NoTrade") ? item.GetDisplayName() + " cannot change hands." : ClauseRefusal(item);
            }

            sealed class Member
            {
                internal readonly Entity Owner;
                internal readonly string Id;
                readonly string blueprint, name;
                readonly Part[] parts;
                readonly int count, weight;
                readonly int? liquidVolume;
                readonly string liquidId;

                internal Member(Entity owner)
                {
                    Owner = owner;
                    Id = owner.ID;
                    blueprint = owner.BlueprintName;
                    name = owner.GetDisplayName();
                    parts = owner.Parts.ToArray();
                    count = StackOf(owner);
                    weight = HandlingService.GetWeight(owner);
                    liquidId = owner.GetPart<LiquidVesselPart>()?.LiquidId;
                    liquidVolume = owner.GetPart<LiquidVesselPart>()?.Volume;
                }

                internal bool Matches() => Owner.ID == Id && Owner.BlueprintName == blueprint
                    && Owner.Parts.SequenceEqual(parts) && parts.All(part => part != null && part.ParentEntity == Owner)
                    && Owner.GetDisplayName() == name && StackOf(Owner) == count && HandlingService.GetWeight(Owner) == weight
                    && Owner.GetPart<LiquidVesselPart>()?.LiquidId == liquidId && Owner.GetPart<LiquidVesselPart>()?.Volume == liquidVolume;
            }
        }
    }
}
