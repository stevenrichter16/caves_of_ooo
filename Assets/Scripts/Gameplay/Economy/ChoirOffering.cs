using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// EC3 (Docs/ECONOMY-SYSTEM.md §9): the Choir does not trade. Canon (Lore/History/08_MaterialCulture.md
    /// §II): "you cannot buy anything from the Choir — you can only give yourself". A tendril keeps its
    /// renewing shelf (the cathedral and restock systems rely on it), but nothing on it is for sale: no trade
    /// window opens and <see cref="TradeSystem.CanBeTraded"/> vetoes any trade with it. A walker who lets the
    /// tendril taste their blood (a fifth of their health, never the last of it) receives what it holds.
    ///
    /// <para>All-or-nothing: a missing party, a tendril holding nothing, or a walker too weak to give refuses
    /// and changes nothing. Diag: trade/ChoirOffering or trade/ChoirOfferingRefused.</para>
    /// </summary>
    public static class ChoirOffering
    {
        public const string Faction = "RotChoir";
        /// <summary>Percent of maximum health the soil tastes.</summary>
        public const int BloodPercent = 20;
        public const int Regard = 5;

        /// <summary>A speaker of the Choir itself: it gives, it never sells.</summary>
        public static bool KeepsNoShop(Entity speaker) => speaker != null && FactionManager.GetFaction(speaker) == Faction;

        /// <summary>The health an offering costs <paramref name="walker"/>, or 0 without a health stat.</summary>
        public static int Cost(Entity walker)
        {
            var hp = walker?.GetStat("Hitpoints");
            return hp == null ? 0 : System.Math.Max(1, hp.Max * BloodPercent / 100);
        }

        /// <summary>Null on success, else the refusal (and nothing changed).</summary>
        public static string Offer(Entity tendril, Entity walker)
        {
            if (tendril == null || walker == null) return Refuse(tendril, walker, "missing_participant");
            if (walker.GetPart<InventoryPart>() == null) return Refuse(tendril, walker, "missing_inventory");
            var held = tendril.GetPart<InventoryPart>();
            if (held == null || held.Objects.Count == 0)
            { MessageLog.Add("The tendril holds nothing now. It asks nothing of you."); return Refuse(tendril, walker, "nothing_to_give"); }
            var hp = walker.GetStat("Hitpoints");
            int cost = Cost(walker);
            if (hp == null || hp.Value <= cost)
            { MessageLog.Add("You have too little left to give."); return Refuse(tendril, walker, "too_weak"); }

            hp.BaseValue -= cost;
            PlayerReputation.Modify(Faction, Regard);
            var names = new List<string>();
            foreach (var gift in held.Objects.ToArray())
            {
                if (!held.RemoveObject(gift)) continue;
                names.Add(gift.BlueprintName);
                if (ConversationActions.DeliverItem(walker, gift) != null) held.AddObject(gift);
            }
            MessageLog.Add("The tendril tastes you, unhurried. What it held, it gives: what is ours is all of ours.");
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record("trade", "ChoirOffering", actor: walker, target: tendril, payload: new { cost, gifts = string.Join(",", names), regard = Regard });
            return null;
        }

        static string Refuse(Entity tendril, Entity walker, string reason)
        {
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record("trade", "ChoirOfferingRefused", actor: walker, target: tendril, payload: new { reason });
            return reason;
        }
    }
}
