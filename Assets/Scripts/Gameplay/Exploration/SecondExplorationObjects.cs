using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public sealed class RecoverableLampPart : SecondExplorationServicePart
    {
        public override string Name => "RecoverableLamp";
        protected override string[] Verbs => new[] { "RecoverLamp" };
        protected override bool Civilian => false;
        public bool Recovered;
        bool Ready() => !Recovered && ParentEntity.BlueprintName == "BeetleJar" && ParentEntity.HasPart<LightSourcePart>()
            && !ParentEntity.HasPart<EquippablePart>() && !ParentEntity.GetPart<PhysicsPart>().Takeable;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (Ready()) Offer(a, "unhook lamp (takes light from seat)", "RecoverLamp"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != "RecoverLamp" || !Ready()) return false;
            var owner = ParentEntity; var at = zone.GetEntityPosition(owner); var physical = owner.GetPart<PhysicsPart>();
            int oldWeight = physical.Weight; bool itemTag = owner.HasTag("Item"); var equipped = new EquippablePart { Slot = "Hand" };
            var handling = new HandlingPart { Weight = 2, Carryable = true };
            if (owner.HasPart<HandlingPart>()) return false;
            tx.Do(null, () =>
            {
                Recovered = false; owner.RemovePart(equipped); owner.RemovePart(handling); physical.Takeable = false; physical.Weight = oldWeight;
                if (!itemTag) owner.Tags.Remove("Item");
                if (owner.SpatialZone == null && physical.InInventory == null && physical.Equipped == null) zone.AddEntity(owner, at.x, at.y);
            });
            if (!zone.RemoveEntity(owner)) return false;
            physical.Takeable = true; physical.Weight = 2; owner.SetTag("Item"); owner.AddPart(equipped); owner.AddPart(handling); Recovered = true;
            return SecondExplorationActions.Give(tx, actor, owner, command);
        }
    }
    internal static class ExplorationDismantle
    {
        internal static bool Apply(Entity actor, Entity owner, Zone zone, InventoryTransaction tx, string command, string blueprint, int quantity, Func<bool> current)
        {
            if (!current()) return false; var result = SecondExplorationActions.Content(zone)?.CreateEntity(blueprint);
            if (!SecondExplorationActions.Fresh(result, blueprint) || !result.GetPart<PhysicsPart>().Takeable) return false;
            var stack = result.GetPart<StackerPart>(); if (quantity != 1 && stack == null) return false;
            if (stack != null) stack.StackCount = quantity;
            if (!current() || !SecondExplorationActions.Near(actor, owner, zone)) return false;
            var at = zone.GetEntityPosition(owner);
            tx.Do(null, () => { if (owner.SpatialZone == null && owner.GetPart<DestructiblePart>()?.Gone != true) zone.AddEntity(owner, at.x, at.y); });
            if (!zone.RemoveEntity(owner)) return false;
            return SecondExplorationActions.Give(tx, actor, result, command);
        }
    }
    public sealed class TrapSalvagePart : SecondExplorationServicePart
    {
        public override string Name => "TrapSalvage";
        protected override string[] Verbs => new[] { "SalvageJammedTrap" };
        protected override bool Civilian => false;
        bool Ready() => ParentEntity.BlueprintName == "SpikeTrap" && TrapJammingPart.IsJammed(ParentEntity);
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (Ready()) Offer(a, "salvage spike (timber stays spent)", "SalvageJammedTrap"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx) => command == "SalvageJammedTrap"
            && ExplorationDismantle.Apply(actor, ParentEntity, zone, tx, command, "IronSpikeComponent", 1, Ready);
    }
    public sealed class ReclaimClothPart : SecondExplorationServicePart
    {
        public override string Name => "ReclaimCloth";
        protected override string[] Verbs => new[] { "StripClothScreen" };
        protected override bool Civilian => false;
        bool Ready() => !ParentEntity.HasEffect<BurningEffect>() && ParentEntity.GetPart<MaterialPart>()?.MaterialID == "Cloth"
            && ParentEntity.GetPart<PhysicsPart>()?.Solid == true && ParentEntity.GetPart<DestructiblePart>() is DestructiblePart d && !d.Gone && d.HP > 0;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (Ready()) Offer(a, "strip screen: +2 cord, lose cover", "StripClothScreen"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx) => command == "StripClothScreen"
            && ExplorationDismantle.Apply(actor, ParentEntity, zone, tx, command, "KnotflaxCord", 2, Ready);
    }
    /// <summary>Opt-in physical cargo weight. Bounded, read-only traversal rejects
    /// cycles, duplicate identities, owner aliases and overflow with an unhaulable load.</summary>
    public sealed class ContainerLoadPart : Part
    {
        public override string Name => "ContainerLoad";
        [NonSerialized] Entity[] seen;
        internal int Weight(int shell)
        {
            if (ParentEntity?.GetPart<ContainerLoadPart>() != this || ParentEntity.GetPart<ContainerPart>() == null) return int.MaxValue;
            seen ??= new Entity[64]; int count = 0; long total = Weigh(ParentEntity, null, shell, ref count);
            for (int i = 0; i < count; i++) seen[i] = null;
            return total >= int.MaxValue ? int.MaxValue : (int)Math.Max(0, total);
        }
        long Weigh(Entity item, Entity holder, int shell, ref int count)
        {
            if (item == null || count >= seen.Length || string.IsNullOrEmpty(item.ID)) return int.MaxValue;
            for (int i = 0; i < count; i++) if (seen[i] == item || seen[i].ID == item.ID) return int.MaxValue;
            seen[count++] = item; var p = item.GetPart<PhysicsPart>();
            if (p?.ParentEntity != item || holder != null && (p.InInventory != holder || p.Equipped != null || item.SpatialZone != null)) return int.MaxValue;
            int units = item.GetPart<StackerPart>()?.StackCount ?? 1; if (units < 1) return int.MaxValue;
            long each = Math.Max(0, shell); var contents = item.GetPart<ContainerPart>();
            if (contents != null)
                foreach (var child in contents.Contents)
                {
                    int own = child?.GetPart<HandlingPart>()?.Weight ?? 0; if (own <= 0) own = child?.GetPart<PhysicsPart>()?.Weight ?? 0;
                    each += Weigh(child, item, own, ref count); if (each >= int.MaxValue) return int.MaxValue;
                }
            return Math.Min(int.MaxValue, each * units);
        }
    }
    public sealed class CollectorBarterPart : SecondExplorationServicePart
    {
        public override string Name => "CollectorBarter";
        protected override string[] Verbs => new[] { "BarterCollector" };
        bool Deal(Entity actor, Entity food)
        {
            var role = ParentEntity.GetPart<SpreadCollectorPart>(); var item = role?.CurrentCarriedItem;
            return role?.Phase == SpreadCollectorPhase.Carrying && item != null && role.Configured && role.ZoneID == ParentEntity.SpatialZone?.ZoneID
                && !SecondExplorationActions.Special(item) && SecondExplorationActions.Carried(actor, food) && food.HasPart<FoodPart>()
                && !SecondExplorationActions.Special(food) && !food.HasPart<RentalPart>();
        }
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { foreach (var food in actor.GetPart<InventoryPart>().Objects.Where(e => Deal(actor, e))) Offer(a, "exchange " + food.GetDisplayName() + " for " + ParentEntity.GetPart<SpreadCollectorPart>().CurrentCarriedItem.GetDisplayName(), SecondExplorationActions.Choice("BarterCollector", food)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var food = SecondExplorationActions.Selected(actor, command); if (!Deal(actor, food)) return false;
            var role = ParentEntity.GetPart<SpreadCollectorPart>(); var item = role.CurrentCarriedItem;
            if (!SecondExplorationActions.Consume(tx, actor, food, 1, command) || !Current(actor, zone)
                || role.CurrentCarriedItem != item || role.Phase != SpreadCollectorPhase.Carrying
                || !SecondExplorationActions.Transfer(tx, actor, ParentEntity, actor, item, command)) return false;
            tx.Do(() => role.Phase = SpreadCollectorPhase.Stopped, () => role.Phase = SpreadCollectorPhase.Carrying); return true;
        }
    }
    public sealed class LoanShelfPart : SecondExplorationServicePart
    {
        public override string Name => "LoanShelf";
        protected override string[] Verbs => new[] { "BorrowVolume" };
        protected override bool Civilian => false;
        bool Stock(Entity book) => SecondExplorationActions.Carried(ParentEntity, book, true) && RentalSystem.IsRentable(book) && !RentalSystem.IsRented(book) && book.HasPart<GrimoirePart>();
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        {
            var stock = ParentEntity.GetPart<InventoryPart>(); if (stock == null) return;
            foreach (var book in stock.Objects.Where(Stock)) Offer(a, "(" + RentalSystem.GetRentalCost(book, actor, ParentEntity) + " Ink) borrow " + SecondExplorationActions.BookTitle(book), SecondExplorationActions.Choice("BorrowVolume", book));
        }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var book = SecondExplorationActions.Selected(ParentEntity, command); if (!Stock(book)) return false;
            int cost = RentalSystem.GetRentalCost(book, actor, ParentEntity), ink = RentalSystem.GetInk(actor);
            if (cost <= 0 || ink < cost || !SecondExplorationActions.Transfer(tx, actor, ParentEntity, actor, book, command)) return false;
            var loan = new RentalPart { InkPaid = cost, LessorBlueprintName = ParentEntity.BlueprintName };
            tx.Do(() => { RentalSystem.SetInk(actor, ink - cost); book.AddPart(loan); }, () => { RentalSystem.SetInk(actor, RentalSystem.GetInk(actor) + cost); book.RemovePart(loan); }); return true;
        }
    }
}
