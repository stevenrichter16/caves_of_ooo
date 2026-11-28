using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public sealed class RecoverableLampPart : SecondExplorationServicePart
    {
        const string LegacyGuide = "This descent jar can be unhooked. The same lantern-beetles will light a hand-held jar, leaving this resting seat darker. It needs a real free hand to equip.";
        const string MountedFlavor = "A stoppered jar of lantern-beetles, hung at head height.";

        public override string Name => "RecoverableLamp";
        protected override string[] Verbs => new[] { "RecoverLamp" };
        protected override bool Civilian => false;
        public bool Recovered;

        internal string ResolveDescription(string text)
        {
            var owner = ParentEntity;
            if (owner?.BlueprintName != "BeetleJar" || owner.GetPart<RecoverableLampPart>() != this) return text;

            string flavor = text ?? "";
            if (flavor == LegacyGuide) flavor = "";
            else if (flavor.EndsWith("\n" + LegacyGuide, StringComparison.Ordinal))
                flavor = flavor.Substring(0, flavor.Length - LegacyGuide.Length - 1);
            if (Recovered && flavor.StartsWith(MountedFlavor, StringComparison.Ordinal))
                flavor = "A stoppered jar of lantern-beetles." + flavor.Substring(MountedFlavor.Length);
            var physical = owner.GetPart<PhysicsPart>();
            var source = owner.GetPart<LightSourcePart>();
            bool lit = source is { Enabled: true, Intensity: > 0, Radius: >= 0 };
            bool onGround = WorldResourceActions.Ground(owner, owner.SpatialZone);
            string state = null;
            if (!Recovered && onGround && physical != null && Ready())
            {
                state = lit ? "Mounted and lighting the ledge. Unhooking stows it and dims this spot. Hold it in a hand for light."
                    : "Mounted and dark. The jar can be unhooked.";
            }
            else if (Recovered && physical?.Equipped is Entity wearer && EquipmentUtilityActions.Equipped(wearer, owner, "Hand"))
                state = lit ? "Held in hand. The light follows the bearer." : "Held in hand. The jar is dark.";
            else if (Recovered && WorldResourceActions.Carried(physical?.InInventory, owner))
                state = lit ? "Packed. Hold it in a hand to light your way." : "Packed. The jar is dark.";
            else if (Recovered && physical?.InInventory?.GetPart<ContainerPart>()?.Contents.Contains(owner) == true)
                state = lit ? "Stored. Hold it in a hand to light your way." : "Stored. The jar is dark.";
            else if (Recovered && onGround)
                state = lit ? "Set down. The jar lights the ground around it." : "Set down. The jar is dark.";

            return state == null ? flavor : flavor.Length == 0 ? state : flavor + "\n" + state;
        }

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
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (Ready()) Offer(a, "salvage spike (wedge stays spent)", "SalvageJammedTrap"); }
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
    public sealed class ContainerLoadPart : Part, IDescribeState
    {
        public override string Name => "ContainerLoad";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        public string DescribeState(DescribeContext context)
        {
            var owner = ParentEntity;
            if (owner == null || context?.Owner != owner || owner.GetPart<ContainerLoadPart>() != this
                || owner.GetPart<ContainerPart>()?.ParentEntity != owner
                || owner.GetPart<HandlingPart>() is not { Carryable: false } handling || handling.ParentEntity != owner
                || owner.GetPart<PhysicsPart>()?.Takeable != false || owner.HasTag("Creature")
                || LocalPorterReservationPart.Blocks(owner) || !WorldResourceActions.Ground(owner, context.Zone))
                return null;

            int weight = DragRules.WeightOf(owner);
            return weight == int.MaxValue ? null : $"Haul weight: {weight} lb.";
        }

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
        Entity CarriedFind(Zone zone)
        {
            var role = ParentEntity.GetPart<SpreadCollectorPart>();
            if (zone == null || role is not { Configured: true, Phase: SpreadCollectorPhase.Carrying }
                || role.ParentEntity != ParentEntity || role.ZoneID != zone.ZoneID)
                return null;

            var item = role.CurrentCarriedItem;
            return SecondExplorationActions.Special(item) ? null : item;
        }

        static bool CanPay(Entity actor, Entity food)
            => SecondExplorationActions.Carried(actor, food) && food.HasPart<FoodPart>()
                && !SecondExplorationActions.Special(food) && !food.HasPart<RentalPart>()
                && actor.GetPart<InventoryPart>()?.CanConsumeOne(food) == true;

        protected override void Choices(Entity actor, Zone zone, InventoryActionList actions)
        {
            var item = CarriedFind(zone);
            if (item == null || actions == null || zone.GetEntityCell(ParentEntity)?.IsVisible != true
                || ParentEntity.GetPart<RenderPart>() is not { Visible: true } render || render.ParentEntity != ParentEntity)
                return;

            bool offered = false;
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory != null)
                foreach (var food in inventory.Objects)
                {
                    if (!CanPay(actor, food)) continue;
                    actions.AddActionWithHint("BarterCollector",
                        $"exchange one {InventoryPart.GetUnitDisplayName(food)} for {item.GetDisplayName()}",
                        SecondExplorationActions.Choice("BarterCollector", food), "The exchange takes one action.", priority: 24);
                    offered = true;
                }

            if (!offered)
            {
                const string reason = "Carry one food item you can spare.";
                actions.AddDisabled("BarterCollector", $"offer food for {item.GetDisplayName()}",
                    "collector-needs-food", reason, priority: 24).Hint = reason;
            }
        }

        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var food = SecondExplorationActions.Selected(actor, command);
            var item = CarriedFind(zone);
            if (item == null || !CanPay(actor, food)) return false;

            var collector = ParentEntity;
            var role = collector.GetPart<SpreadCollectorPart>();
            if (!SecondExplorationActions.Consume(tx, actor, food, 1, command)
                || ParentEntity != collector || !Current(actor, zone)
                || collector.GetPart<SpreadCollectorPart>() != role || CarriedFind(zone) != item
                || !SecondExplorationActions.Transfer(tx, actor, collector, actor, item, command)) return false;
            tx.Do(() => role.Phase = SpreadCollectorPhase.Stopped, () =>
            {
                if (collector.GetPart<SpreadCollectorPart>() == role && role.ParentEntity == collector
                    && role.Phase == SpreadCollectorPhase.Stopped)
                    role.Phase = SpreadCollectorPhase.Carrying;
            });
            return true;
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
