using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>One loose strip of a fallen cart's cured harness, lost when the cart catches fire.</summary>
    public sealed class CartHarnessPart : Part, IDescribeState, IInventoryActionReceiptOwner
    {
        internal const string Site = "Overworld.10.11.0";
        internal const string Cart = "OverturnedCart";
        const string Product = "CuredLeatherOffcut", Action = "CutCartHarness|";

        public override string Name => "CartHarness";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;
        public bool Cut;
        public bool Charred;

        internal static void Install(Zone zone, EntityFactory factory, IReadOnlyList<Entity> placed)
        {
            if (zone.ZoneID != Site || MaterialProcessTemplate.Capture(factory, Product, true) == null) return;
            var carts = placed.Where(owner => OwnerReady(owner, zone)).ToArray();
            if (carts.Length != 1 || zone.GetReadOnlyEntitiesWithPart<CartHarnessPart>().Count != 0) return;
            carts[0].AddPart(new CartHarnessPart());
        }

        static bool OwnerReady(Entity owner, Zone zone) => owner?.BlueprintName == Cart
            && WorldResourceActions.Ground(owner, zone) && !owner.HasTag("Creature") && !owner.HasTag("Terrain")
            && owner.GetPart<RenderPart>() is { Visible: true }
            && owner.GetPart<PhysicsPart>() is { Solid: true, Takeable: false }
            && owner.GetPart<DestructiblePart>() is { Gone: false, HP: > 0 };

        bool Current(Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            return zone?.ZoneID == Site && manager?.ActiveZone == zone
                && manager.CachedZones.TryGetValue(Site, out var existing) && existing == zone
                && OwnerReady(ParentEntity, zone) && ParentEntity.GetPart<CartHarnessPart>() == this
                && ParentEntity.Parts.Count(part => part is CartHarnessPart) == 1;
        }

        bool Unburned() => !Charred && !ParentEntity.HasEffect<BurningEffect>() && !ParentEntity.HasEffect<CharredEffect>();

        internal (string Glyph, string Color) Appearance(Zone zone)
            => Current(zone) && !Cut && Unburned() ? ("h", "&W") : default;

        public string DescribeState(DescribeContext context)
        {
            if (!Current(context.Zone ?? ParentEntity?.SpatialZone)) return null;
            if (Cut) return "A cut end remains where the loose leather strip was trimmed.";
            return Unburned()
                ? "A loose strap of cured leather hangs from the snapped shaft. A sound cutting blade can trim one offcut."
                : "The harness has burned through.";
        }

        public override void OnAfterLoad(SaveReader reader)
        {
            if (ParentEntity.HasEffect<BurningEffect>() || ParentEntity.HasEffect<CharredEffect>()) Charred = true;
        }

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<CartHarnessPart>() == this
            && command?.StartsWith(Action, StringComparison.Ordinal) == true;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EffectApplied" && ParentEntity is { } cart && e.GetParameter<Entity>("Target") == cart
                && e.GetParameter<Effect>("Effect") is Effect applied
                && (applied is BurningEffect && cart.GetEffect<BurningEffect>() == applied
                    || applied is CharredEffect && cart.GetEffect<CharredEffect>() == applied))
            {
                Charred = true;
                if (cart.SpatialZone?.GetEntityCell(cart) is { } cell)
                    ZoneRenderHooks.MarkCellDirty(cell, "Cart harness burned");
            }
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = PreparationActions.ZoneFor(e, actor);
                RouteWorkMenu.Rows(ParentEntity, actor, zone, e.GetParameter<InventoryActionList>("Actions"), Choices(actor, zone), true);
            }
            if (e.ID == "InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(ParentEntity, e))
            {
                e.Handled = true;
                return false;
            }
            return true;
        }

        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor, Zone zone)
        {
            if (!Current(zone) || Cut || !Unburned() || !RouteWorkMenu.ActorReady(actor, zone) || TurnManager.Active == null) yield break;
            var cart = ParentEntity;
            var origin = zone.GetEntityCell(actor);
            var cell = zone.GetEntityCell(cart);
            var pack = actor.GetPart<InventoryPart>();
            var clock = TurnManager.Active;
            int tick = clock.TickCount;
            var factory = WorldLocationContext.For(zone).Factory;
            var product = MaterialProcessTemplate.Capture(factory, Product, true);
            var blade = MaterialCuttingWorkReceipt.Resolve(actor, cart, zone);
            if (product == null || blade == null || !CanReach()) yield break;

            var sameCart = RouteWorkMenu.Identity(cart);
            string key = Action + cart.ID;
            InventoryUnitOutputReceipt output = null;
            bool CanReach() => RouteWorkMenu.Visible(cell) && zone.GetEntityCell(actor) == origin
                && !origin.BlocksMovement(actor) && SpatialQuery.Distance(zone, actor, cart) == 1;
            bool Context() => Current(zone) && Unburned() && TurnManager.Active == clock && clock.TickCount == tick
                && RouteWorkMenu.ActorReady(actor, zone) && actor.GetPart<InventoryPart>() == pack && sameCart()
                && zone.GetEntityCell(cart) == cell && CanReach() && blade.Current() && product.Current();
            bool Before() => Context() && !Cut;
            bool After() => Context() && Cut && output?.Current() == true && product.Matches(output.Recipient);

            yield return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = cell, Claims = new[] { blade.Tool }, Key = key,
                Label = "Cut one leather offcut", Hint = "One action trims the loose harness with your blade. You can cut the offcut into a leather binding.",
                Before = Before, After = After,
                Apply = transaction =>
                {
                    var offcut = factory.CreateEntity(Product);
                    if (!product.Matches(offcut) || !PreparationActions.Fresh(offcut, Product) || !Before()
                        || pack.Objects.Concat(pack.EquippedItems.Values).Any(item => item.ID == offcut.ID)
                        || zone.GetReadOnlyEntities().Any(item => item.ID == offcut.ID)) return false;

                    transaction.Do(() => Cut = true, () =>
                    {
                        if (ParentEntity == cart && cart.GetPart<CartHarnessPart>() == this
                            && output is not { RetractedUnit: false }) Cut = false;
                    });
                    return InventoryUnitOutputReceipt.TryPlace(pack, actor, offcut, transaction, key, out output);
                },
                Publish = () =>
                {
                    ZoneRenderHooks.MarkCellDirty(cell, "Cart harness trimmed");
                    MessageLog.Add("You cut one cured leather offcut from the loose harness.");
                }
            };
        }
    }
}
