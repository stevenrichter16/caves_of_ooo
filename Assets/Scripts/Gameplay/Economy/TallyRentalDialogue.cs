using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Offers from Tally's existing rack and return counter. A displayed
    /// choice owns its quote only for the lifetime of the current conversation view.</summary>
    internal static class TallyRentalDialogue
    {
        private sealed class Quote
        {
            internal string Text, Refusal;
            internal (Entity Item, RentalPart Loan, int InkPaid)[] Terms =
                Array.Empty<(Entity, RentalPart, int)>();
        }

        private static readonly Dictionary<ChoiceData, (Entity Speaker, Entity Listener, ChoiceData Source, Quote Seen)>
            offers = new Dictionary<ChoiceData, (Entity, Entity, ChoiceData, Quote)>();
        private static bool counterConversation;

        internal static void Clear() => offers.Clear();

        internal static void Begin(Entity speaker, Entity listener, string conversation)
            => counterConversation = IsTallyQuartermaster(speaker, listener, conversation);

        internal static void End()
        {
            Clear();
            counterConversation = false;
        }

        private static bool IsTallyQuartermaster(Entity speaker, Entity listener, string conversation)
        {
            var zone = speaker?.SpatialZone;
            return conversation == "Quartermaster_1" && TallyCompositionPlan.IsSupportedZone(zone?.ZoneID)
                && speaker.BlueprintName == "Quartermaster" && WorldResourceActions.Ground(speaker, zone)
                && listener?.HasTag("Player") == true;
        }

        private static bool AtCounter(Entity speaker, Entity listener, string conversation)
            => IsTallyQuartermaster(speaker, listener, conversation) && SecondExplorationActions.Live(speaker)
                && WorldResourceActions.Nearby(listener, speaker, speaker.SpatialZone);

        internal static string PresentText(Entity speaker, Entity listener, string conversation, string node, string text)
            => (node == "Wares" || node == "Return") && AtCounter(speaker, listener, conversation)
                ? $"You have {RentalSystem.GetInk(listener)} Ink.\n\n{text}" : text;

        internal static ChoiceData PresentChoice(Entity speaker, Entity listener, string conversation, string node, ChoiceData source)
        {
            if (!counterConversation || source.Actions?.Count != 1 || string.IsNullOrWhiteSpace(source.Text)) return source;
            string action = source.Actions[0]?.Key;
            if (!(node == "Wares" && action == "RentItem" || node == "Return" && action == "ReturnRentals")) return source;

            var quote = AtCounter(speaker, listener, conversation) ? Read(speaker, listener, source)
                : new Quote { Text = action == "RentItem" ? Label(source) + " | counter out of reach" : "Counter out of reach" };
            var choice = new ChoiceData { Text = quote.Text, Target = source.Target, Actions = source.Actions, Predicates = source.Predicates };
            offers.Add(choice, (speaker, listener, source, quote));
            return choice;
        }

        internal static bool CanSelect(ChoiceData choice, Entity speaker, Entity listener, string conversation)
        {
            if (!offers.TryGetValue(choice, out var offer)) return true;
            if (speaker == offer.Speaker && listener == offer.Listener && AtCounter(speaker, listener, conversation))
            {
                var current = Read(speaker, listener, offer.Source);
                if (current.Text == offer.Seen.Text && current.Refusal == offer.Seen.Refusal
                    && current.Terms.SequenceEqual(offer.Seen.Terms))
                {
                    if (current.Refusal == null) return true;
                    MessageLog.Add(current.Refusal);
                    return false;
                }
            }
            MessageLog.Add(AtCounter(speaker, listener, conversation) ? "The rental terms changed. Check the new quote."
                : "Move next to the quartermaster to use the counter.");
            return false;
        }

        private static Quote Read(Entity speaker, Entity listener, ChoiceData source)
        {
            int wallet = RentalSystem.GetInk(listener);
            var action = source.Actions[0];
            if (action.Key == "ReturnRentals") return ReadReturns(speaker, listener, wallet);

            string label = Label(source);
            var item = RentalSystem.FindRackLoan(speaker, action.Value);
            if (!WorldResourceActions.Carried(speaker, item) || !RentalSystem.IsRentable(item) || RentalSystem.IsRented(item))
                return new Quote { Text = label + " | out of stock", Refusal = "The rack has none of that weapon available." };

            int cost = RentalSystem.GetRentalCost(item, listener, speaker);
            int refund = RentalSystem.GetRefund(cost);
            var quote = new Quote { Text = $"{label} | {cost} Ink, {refund} back", Terms = new[] { (item, (RentalPart)null, cost) } };
            if (wallet < cost)
            {
                quote.Text += $" | short {(long)cost - wallet}";
                quote.Refusal = $"You need {cost} Ink to rent that. You have {wallet}.";
            }
            else if (!CanFit(listener.GetPart<InventoryPart>(), item))
            {
                quote.Text += " | too heavy";
                quote.Refusal = "You can't carry that weapon right now.";
            }
            return quote;
        }

        private static Quote ReadReturns(Entity speaker, Entity listener, int wallet)
        {
            var terms = RentalSystem.FindLoansToReturn(listener, speaker).Select(item =>
            {
                var loan = item.GetPart<RentalPart>();
                return (Item: item, Loan: loan, InkPaid: loan.InkPaid);
            }).ToArray();
            if (terms.Length == 0)
                return new Quote { Text = "No loans to return", Refusal = "You have no rentals to return here." };

            long refund = terms.Sum(term => (long)RentalSystem.GetRefund(term.InkPaid));
            if (wallet < 0 || refund + wallet > int.MaxValue || terms.Any(term => term.InkPaid < 0))
                return new Quote { Text = "Refund unavailable", Refusal = "The ledger cannot settle that refund.", Terms = terms };

            var rack = speaker.GetPart<InventoryPart>();
            long room = rack == null ? -1 : rack.MaxWeight < 0 ? long.MaxValue : (long)rack.MaxWeight - rack.GetCarriedWeight();
            int fits = 0;
            long credit = 0;
            foreach (var term in terms)
            {
                var weight = InventoryPart.GetItemWeight(term.Item);
                if (weight > room) continue;
                room -= weight;
                fits++;
                credit += RentalSystem.GetRefund(term.InkPaid);
            }
            if (fits == 0)
                return new Quote { Text = "No room on the rack", Refusal = "The rack can't accept those weapons right now.", Terms = terms };
            if (fits < terms.Length)
                return new Quote { Text = $"Return {fits} of {terms.Length} | est. +{credit} Ink", Terms = terms };

            string label = terms.Length == 1 ? "Return 1 loan" : $"Return {terms.Length} loans";
            bool conditional = terms.Length > 1 || InventorySystem.IsEquipped(listener, terms[0].Item);
            string creditText = conditional ? $"up to +{refund} Ink" : $"+{refund} Ink";
            return new Quote { Text = label + " | " + creditText, Terms = terms };
        }

        private static bool CanFit(InventoryPart inventory, Entity item)
            => inventory != null && (inventory.MaxWeight < 0 ||
                (long)inventory.GetCarriedWeight() + InventoryPart.GetItemWeight(item) <= inventory.MaxWeight);

        private static string Label(ChoiceData source)
        {
            string label = source.Text.StartsWith("The ", StringComparison.Ordinal)
                ? source.Text.Substring(4).TrimEnd('.') : source.Text;
            if (string.IsNullOrWhiteSpace(label)) label = source.Text;
            return char.ToUpperInvariant(label[0]) + label.Substring(1);
        }
    }
}
