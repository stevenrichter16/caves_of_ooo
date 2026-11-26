using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Concerns
{
    public static partial class ConcernConversation
    {
        public const string MaterialSupplyAction = "HandConcernMaterialSupply";

        private static readonly ConditionalWeakTable<NodeData, Dictionary<string, MaterialSupplyQuote>> MaterialSupplyQuotes = new();

        private static bool UsesClayGrade(ConcernSupplyData supply) => supply?.Blueprint == "FireClay"
            && supply.MaterialGrade == RepairMaterialPart.LiningClay && string.IsNullOrEmpty(supply.MeasuredLiquid);

        private static IEnumerable<MaterialPayment> MaterialBill(Entity listener, ConcernSupplyData supply, int required)
        {
            var inventory = listener?.GetPart<InventoryPart>();
            if (inventory == null || !UsesClayGrade(supply) || required < 1) yield break;
            var factory = ConversationActions.Factory;
            foreach (var owner in inventory.Objects.Where(item => item != null).OrderBy(item => item.BlueprintName != supply.Blueprint))
            {
                if (!inventory.CanConsumeOne(owner) || !WorldResourceActions.Carried(listener, owner, single: false)) continue;
                var alternate = owner.BlueprintName == supply.Blueprint ? null : RepairMaterialPart.Read(factory, owner, supply.MaterialGrade);
                if (alternate == null && owner.BlueprintName != supply.Blueprint) continue;
                int count = owner.GetPart<StackerPart>()?.StackCount ?? 1;
                int units = Math.Min(count, required);
                yield return new MaterialPayment(owner, count, units, alternate);
                required -= units;
                if (required == 0) yield break;
            }
        }

        private static int CountSupply(Entity listener, ConcernSupplyData supply) => string.IsNullOrEmpty(supply.MaterialGrade)
            ? ConcernItems.CountCarried(listener, supply.Blueprint)
            : MaterialBill(listener, supply, int.MaxValue).Sum(part => part.Units);

        private sealed class MaterialPayment
        {
            internal readonly Entity Owner;
            internal readonly string Blueprint;
            internal readonly int Units;
            private readonly string id;
            private readonly int count;
            private readonly PhysicsPart physics;
            private readonly StackerPart stack;
            private readonly MaterialProcessItemState alternate;
            private readonly ConnectedClayProvenance.Snapshot provenance;

            internal MaterialPayment(Entity owner, int count, int units, MaterialProcessItemState alternate)
            {
                Owner = owner; id = owner.ID; Blueprint = owner.BlueprintName;
                physics = owner.GetPart<PhysicsPart>(); stack = owner.GetPart<StackerPart>();
                this.count = count; Units = units; this.alternate = alternate;
                provenance = new ConnectedClayProvenance.Snapshot(owner);
            }

            private bool IdentityCurrent => Owner.ID == id && Owner.BlueprintName == Blueprint
                && Owner.GetPart<PhysicsPart>() == physics && Owner.GetPart<StackerPart>() == stack;

            internal bool Current(Entity listener, InventoryPart inventory, bool paid)
            {
                int spent = paid ? Units : 0;
                if (!IdentityCurrent || provenance.Changed(Owner, spent) || alternate?.MatchesAfterPayment(spent) == false
                    || inventory.Objects.Any(other => other != Owner && other?.ID == id)) return false;
                int left = count - spent;
                return left > 0
                    ? WorldResourceActions.Carried(listener, Owner, single: false) && (stack?.StackCount ?? 1) == left
                    : Owner.SpatialZone == null && physics.InInventory == null && physics.Equipped == null && !inventory.Objects.Contains(Owner);
            }

            internal bool CanRestore(Entity listener) => IdentityCurrent && Owner.SpatialZone == null
                && physics.Equipped == null && (physics.InInventory == null || physics.InInventory == listener);
        }

        private sealed class MaterialSupplyQuote
        {
            internal readonly ConcernData Concern;
            internal readonly ConcernApproachData Approach;
            internal readonly Entity Speaker, Listener;
            internal readonly InventoryPart Inventory;
            internal readonly MaterialPayment[] Bill;
            internal readonly NarrativeStatePart Narrative;
            internal readonly string Fact, Text;
            private readonly ConcernSupplyData supply;
            private readonly int count;
            private readonly Zone zone;
            private readonly Cell speakerCell, listenerCell;
            private readonly string speakerId, listenerId;
            private readonly EntityFactory factory;

            internal MaterialSupplyQuote(ConcernData concern, ConcernApproachData approach, Entity speaker, Entity listener,
                MaterialPayment[] bill)
            {
                Concern = concern; Approach = approach; Speaker = speaker; Listener = listener; Bill = bill;
                Inventory = listener.GetPart<InventoryPart>(); Narrative = NarrativeStatePart.Current;
                Fact = ConcernRegistry.DoneFact(concern.Id, approach.Id); supply = approach.Supply; count = supply.Count;
                zone = speaker.SpatialZone; speakerCell = zone.GetEntityCell(speaker); listenerCell = zone.GetEntityCell(listener);
                speakerId = speaker.ID; listenerId = listener.ID; factory = ConversationActions.Factory;
                Text = string.Join(" + ", bill.GroupBy(payment => payment.Blueprint)
                    .Select(group => group.Sum(payment => payment.Units) + " " + UnitName(listener, group.Key)));
            }

            internal bool Current(bool paid) => ConversationManager.Speaker == Speaker && ConversationManager.Listener == Listener
                && CanHandSupplyInPerson(Concern, Approach, Speaker, Listener) && !FactionManager.IsHostile(Speaker, Listener)
                && ReferenceEquals(ConcernRegistry.Get(Concern.Id), Concern) && Concern.Approaches.Contains(Approach)
                && Approach.Supply == supply && UsesClayGrade(supply) && supply.Count == count
                && NarrativeStatePart.Current == Narrative && Narrative.GetFact(Fact) == (paid ? 1 : 0)
                && Speaker.SpatialZone == zone && zone.GetEntityCell(Speaker) == speakerCell && zone.GetEntityCell(Listener) == listenerCell
                && (WorldLocationContext.For(zone) is not { } world || world.ActiveZone == zone)
                && Speaker.ID == speakerId && Listener.ID == listenerId && Listener.GetPart<InventoryPart>() == Inventory
                && ConversationActions.Factory == factory && Bill.All(payment => payment.Current(Listener, Inventory, paid));
        }

        private static void AddMaterialSupplyChoice(NodeData node, ConcernData concern, ConcernApproachData approach,
            Entity speaker, Entity listener)
        {
            var supply = approach.Supply;
            if (!UsesClayGrade(supply) || !CanHandSupplyInPerson(concern, approach, speaker, listener)) return;
            var bill = MaterialBill(listener, supply, supply.Count).ToArray();
            if (bill.Sum(payment => payment.Units) != supply.Count) return;
            var quote = new MaterialSupplyQuote(concern, approach, speaker, listener, bill);
            if (!quote.Current(false)) return;
            string token = Guid.NewGuid().ToString("N");
            MaterialSupplyQuotes.GetOrCreateValue(node).Add(token, quote);
            node.Choices.Add(new ChoiceData
            {
                Text = $"[Give {quote.Text}.]",
                Target = "",
                Actions = new List<ConversationParam> { new ConversationParam { Key = MaterialSupplyAction, Value = token } }
            });
        }

        public static string HandMaterialSupply(Entity speaker, Entity listener, string token)
        {
            var node = ConversationManager.CurrentNode;
            if (token == null || node == null || !MaterialSupplyQuotes.TryGetValue(node, out var quotes)
                || !quotes.TryGetValue(token, out var quote) || quote.Speaker != speaker || quote.Listener != listener)
                return "material_supply_changed";
            quotes.Remove(token);
            if (!quote.Current(false))
            {
                if (ConversationManager.Speaker == speaker && ConversationManager.Listener == listener
                    && CanHandSupplyInPerson(quote.Concern, quote.Approach, speaker, listener))
                {
                    MessageLog.Add("Those clay measures changed. Check what you can hand over.");
                    ConversationManager.CurrentNode = StatusNode(quote.Concern, speaker, listener);
                }
                return "material_supply_changed";
            }

            var tx = new InventoryTransaction();
            try
            {
                if (!tx.TryClaimAction(listener) || !tx.TryClaim(listener, listener, MaterialSupplyAction)
                    || !tx.TryClaim(speaker, listener, MaterialSupplyAction)
                    || quote.Bill.Any(payment => !tx.TryClaim(payment.Owner, listener, MaterialSupplyAction)
                        || !MaterialProcessService.ReserveInput(payment.Owner, tx))
                    || ConversationManager.CurrentNode != node || !quote.Current(false)) return "material_supply_changed";

                var receipt = InventoryTransferSnapshot.Capture(quote.Inventory);
                tx.Do(null, () => receipt.RestoreExcept(quote.Bill.Where(payment => CombatSystem.IsDeathHandled(listener)
                    || !payment.CanRestore(listener)).Select(payment => payment.Owner).ToHashSet()));
                if (!receipt.Apply(() => quote.Bill.All(payment => Pay(payment, quote.Inventory, tx)))
                    || !receipt.ClaimChanges(tx, listener, MaterialSupplyAction)) return "material_supply_changed";
                tx.Do(() => quote.Narrative.SetFact(quote.Fact, 1), () => quote.Narrative.SetFact(quote.Fact, 0));
                tx.BeforeCommit(() => ConversationManager.CurrentNode == node && quote.Current(true));
                tx.Commit();
            }
            catch (InvalidOperationException) when (!tx.IsCommitted) { return "material_supply_changed"; }
            finally { tx.Rollback(); }

            MessageLog.Add("You hand over " + quote.Text + ".");
            Record("SupplyHanded", speaker, quote.Concern, quote.Approach.Id);
            ConversationManager.CurrentNode = StatusNode(quote.Concern, speaker, listener);
            return null;
        }

        private static bool Pay(MaterialPayment payment, InventoryPart inventory, InventoryTransaction tx)
        {
            for (int i = 0; i < payment.Units; i++)
                if (!inventory.TryConsumeMaterialProcessUnit(payment.Owner, tx)) return false;
            return true;
        }
    }
}
