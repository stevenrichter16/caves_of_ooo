using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C1 (Docs/PLACE-WORK-AND-PROGRESSION.md §2): one authored trouble of one place.
    /// Concerns live in Content/Data/Concerns/*.json and compile into ordinary quests
    /// (<see cref="ConcernRegistry.QuestIdFor"/>), so the journal, objectives, closure
    /// ledger, quest events and save/load are the shipped quest engine's. Predicates and
    /// effects use the conversation vocabulary (ConversationPredicates/ConversationActions).
    /// JsonUtility-deserialized.
    /// </summary>
    [Serializable]
    public class ConcernFileData
    {
        public List<ConcernData> Concerns = new List<ConcernData>();
    }

    [Serializable]
    public class ConcernData
    {
        /// <summary>Stable id, frozen once shipped ("sill.well"): it lives in saves.</summary>
        public string Id;
        /// <summary>The authored place's name (WorldMapAuthoring.Places).</summary>
        public string Place;
        /// <summary>Blueprint of the resident who raises the concern.</summary>
        public string Owner;
        /// <summary>The faction with the main stake.</summary>
        public string Faction;
        public int Tier = 1;
        /// <summary>Journal title.</summary>
        public string Title;
        /// <summary>What the owner says when raising it.</summary>
        public string Pitch;
        /// <summary>What the owner says on hearing it is done; also the journal accomplishment.</summary>
        public string Thanks;
        /// <summary>Predicates that must all pass before the owner raises it.</summary>
        public List<ConversationParam> Requires = new List<ConversationParam>();
        /// <summary>W2: what the owner says, asked "Is something wrong here?", while
        /// <see cref="Requires"/> fail (predecessors done): the work exists and is not
        /// offered. Empty keeps a gated concern silent. Needs <see cref="Requires"/>.</summary>
        public string Withheld;
        /// <summary>Two or more ways through; the first one finished resolves the concern.</summary>
        public List<ConcernApproachData> Approaches = new List<ConcernApproachData>();
        public ConcernRefusalData Refusal = new ConcernRefusalData();
        public ConcernRewardData Reward = new ConcernRewardData();
        /// <summary>Concern ids this one leads to (here or at another place).</summary>
        public List<string> Next = new List<string>();
    }

    [Serializable]
    public class ConcernApproachData
    {
        public string Id;
        /// <summary>Confront | Supply | Restore | Carry | Read | Settle.</summary>
        public string Kind;
        /// <summary>The owner's description of this way through.</summary>
        public string Label;
        /// <summary>Journal objective text.</summary>
        public string Objective;
        /// <summary>Predicates that, all passing, finish this approach.</summary>
        public List<ConversationParam> Triggers = new List<ConversationParam>();
        /// <summary>Effects when this approach finishes (the place changes this way).</summary>
        public List<ConversationParam> Outcome = new List<ConversationParam>();
        /// <summary>Optional: something the owner can do on the player's behalf while the
        /// concern is open (e.g. ContractPlaceRepair). Offered as <see cref="OfferText"/>.</summary>
        public List<ConversationParam> Offer = new List<ConversationParam>();
        public string OfferText;
        /// <summary>Kind Supply: a counted hand-over to the owner.</summary>
        public ConcernSupplyData Supply = new ConcernSupplyData();
        /// <summary>Kind Carry: a sealed item from the owner to a resident of another place.</summary>
        public ConcernCarryData Carry = new ConcernCarryData();
        /// <summary>Kind Read: a document to read where it stands.</summary>
        public ConcernReadData Read = new ConcernReadData();
        /// <summary>Kind Restore: a data-driven structure of the concern's place (PlaceSiteCatalog).</summary>
        public ConcernRestoreData Restore = new ConcernRestoreData();
        /// <summary>Kind Confront: foes at a site near the place, placed on arrival.</summary>
        public ConcernConfrontData Confront = new ConcernConfrontData();
        /// <summary>W3: a guest the sworn player can take under their own cloth.</summary>
        public ConcernHostData Host = new ConcernHostData();

        // JsonUtility always builds nested serializable objects, so "present" means filled in.
        public bool HasSupply => Supply != null && !string.IsNullOrEmpty(Supply.Blueprint);
        public bool HasCarry => Carry != null && !string.IsNullOrEmpty(Carry.Item);
        public bool HasRead => Read != null && !string.IsNullOrEmpty(Read.Document);
        public bool HasRestore => Restore != null && !string.IsNullOrEmpty(Restore.Site);
        public bool HasConfront => Confront?.Foes != null && Confront.Foes.Count > 0;
        public bool HasHost => Host != null && !string.IsNullOrEmpty(Host.Blueprint);
    }

    [Serializable]
    public class ConcernSupplyData
    {
        /// <summary>Blueprint of the real item asked for; carried units of it count.</summary>
        public string Blueprint;
        public int Count = 1;
    }

    [Serializable]
    public class ConcernCarryData
    {
        /// <summary>Blueprint of the sealed item the owner hands over (e.g. SealedLetter).</summary>
        public string Item;
        /// <summary>The name the handed-over item carries ("the well keeper's letter").</summary>
        public string ItemName;
        /// <summary>The place and resident blueprint that receive it.</summary>
        public string Place;
        public string Recipient;
        /// <summary>The player's line on handing it over, and the recipient's answer.</summary>
        public string Handover;
        public string Reply;
    }

    [Serializable]
    public class ConcernRestoreData
    {
        /// <summary>The site id in PlaceSiteCatalog for the concern's own place.</summary>
        public string Site;
    }

    [Serializable]
    public class ConcernConfrontData
    {
        /// <summary>"dx,dy" in world cells from the concern's place: where the site is.</summary>
        public string Offset;
        /// <summary>How the owner and the journal name the site ("the den two fields west of Sill").</summary>
        public string Site;
        public List<ConcernFoeData> Foes = new List<ConcernFoeData>();

        public int Total { get { int n = 0; if (Foes != null) foreach (var f in Foes) n += Math.Max(0, f?.Count ?? 0); return n; } }

        public bool TryOffset(out int dx, out int dy)
        {
            dx = dy = 0;
            var parts = Offset?.Split(',');
            return parts != null && parts.Length == 2 && int.TryParse(parts[0].Trim(), out dx) && int.TryParse(parts[1].Trim(), out dy);
        }
    }

    /// <summary>W3: a Host approach. While the work is open the guest stands at the
    /// concern's place (placed once, by <c>ConcernWorld</c>); naming them guest with
    /// <c>Hospitality_ExtendCloth</c> finishes the approach.</summary>
    [Serializable]
    public class ConcernHostData
    {
        public string Blueprint;
        /// <summary>How the guest is named in the world ("the salt-carrier").</summary>
        public string Name;
    }

    [Serializable]
    public class ConcernFoeData
    {
        public string Blueprint;
        public int Count = 1;
        /// <summary>W2: the concern's own name for these foes ("Harl's kin"), shown as
        /// their display name; body, art and stats stay the blueprint's. Empty keeps it.</summary>
        public string Name;
    }

    [Serializable]
    public class ConcernReadData
    {
        /// <summary>A ReadableDocumentCatalog id; its blueprint is what stands in the place.</summary>
        public string Document;
        /// <summary>The place where it stands while the concern is open.</summary>
        public string Place;
    }

    [Serializable]
    public class ConcernRefusalData
    {
        /// <summary>The player's spoken no.</summary>
        public string Text;
        /// <summary>The owner's answer.</summary>
        public string Reply;
        public List<ConversationParam> Effects = new List<ConversationParam>();
    }

    [Serializable]
    public class ConcernRewardData
    {
        public int XP;
        public int Drams;
        /// <summary>Any further effects on report (reputation, items, taught powers).</summary>
        public List<ConversationParam> Effects = new List<ConversationParam>();
    }

    public static class ConcernKinds
    {
        public static readonly string[] All = { "Confront", "Supply", "Restore", "Carry", "Read", "Settle", "Host" };
    }
}
