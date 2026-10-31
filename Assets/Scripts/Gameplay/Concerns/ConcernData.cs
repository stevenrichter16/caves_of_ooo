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
        public static readonly string[] All = { "Confront", "Supply", "Restore", "Carry", "Read", "Settle" };
    }
}
