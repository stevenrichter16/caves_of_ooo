using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C1: loads concerns and compiles each into a quest registered with
    /// <see cref="StoryletRegistry"/> under <c>concern.&lt;id&gt;</c>:
    /// <list type="bullet">
    /// <item>stage <c>work</c>: one Optional objective per approach, so the first
    /// approach finished advances the stage; its outcome effects run then, and a
    /// <c>concern.&lt;id&gt;.approach</c> fact records which one (1-based);</item>
    /// <item>stage <c>report</c>: waits on the <c>concern.&lt;id&gt;.reported</c> fact;
    /// the owner's turn-in sets it, pays the reward and completes the quest.</item>
    /// </list>
    /// <see cref="Lint"/> rejects content that could not be played.
    /// </summary>
    public static class ConcernRegistry
    {
        public const string ResourcePath = "Content/Data/Concerns";
        public const string QuestPrefix = "concern.";
        static readonly Dictionary<string, ConcernData> concerns = new Dictionary<string, ConcernData>(StringComparer.Ordinal);
        static readonly List<ConcernData> ordered = new List<ConcernData>();

        public static string QuestIdFor(string concernId) => QuestPrefix + concernId;
        public static string ApproachFact(string concernId) => QuestPrefix + concernId + ".approach";
        public static string ReportedFact(string concernId) => QuestPrefix + concernId + ".reported";
        public static bool IsConcernQuest(string questId) => questId != null && questId.StartsWith(QuestPrefix, StringComparison.Ordinal);

        public static IReadOnlyList<ConcernData> All => ordered;
        public static ConcernData Get(string id) => id != null && concerns.TryGetValue(id, out var c) ? c : null;

        public static void Reset() { concerns.Clear(); ordered.Clear(); }

        /// <summary>Load every concern file under Resources and register its quests.
        /// Call after <see cref="StoryletRegistry.LoadAll"/>.</summary>
        public static void LoadAll()
        {
            Reset();
            foreach (var asset in Resources.LoadAll<TextAsset>(ResourcePath))
                LoadFromJson(asset.text, asset.name);
        }

        public static void LoadFromJson(string json, string source = null)
        {
            var file = JsonUtility.FromJson<ConcernFileData>(json);
            if (file?.Concerns == null) return;
            foreach (var c in file.Concerns)
            {
                if (c == null || string.IsNullOrEmpty(c.Id))
                { Debug.LogWarning("[Concerns] a concern without an id in " + source); continue; }
                if (concerns.ContainsKey(c.Id))
                { Debug.LogWarning("[Concerns] duplicate concern " + c.Id + " in " + source); continue; }
                concerns[c.Id] = c;
                ordered.Add(c);
                StoryletRegistry.Register(Compile(c));
            }
        }

        public static StoryletData Compile(ConcernData c)
        {
            var work = new QuestStageData { ID = "work" };
            for (int i = 0; i < c.Approaches.Count; i++)
            {
                var a = c.Approaches[i];
                var onEnter = new List<ConversationParam>(a.Outcome ?? new List<ConversationParam>());
                onEnter.Add(new ConversationParam { Key = "SetFact", Value = ApproachFact(c.Id) + ":" + (i + 1) });
                work.Objectives.Add(new QuestObjectiveData
                {
                    ID = a.Id, Text = a.Objective, Optional = true,
                    Triggers = new List<ConversationParam>(a.Triggers ?? new List<ConversationParam>()),
                    OnEnter = onEnter,
                });
            }
            var report = new QuestStageData { ID = "report" };
            report.Triggers.Add(new ConversationParam { Key = "IfFact", Value = ReportedFact(c.Id) + ":>=:1" });
            return new StoryletData
            {
                ID = QuestIdFor(c.Id), OneShot = false, Tracked = true,
                Quest = new QuestData { Name = c.Title, Accomplishment = c.Thanks, Stages = new List<QuestStageData> { work, report } },
            };
        }

        /// <summary>Every problem that would make a concern unplayable, one line each.</summary>
        public static List<string> Lint(ConcernData c, EntityFactory factory)
        {
            var problems = new List<string>();
            string who = c?.Id ?? "(null)";
            if (c == null) { problems.Add("null concern"); return problems; }
            if (string.IsNullOrWhiteSpace(c.Title)) problems.Add(who + ": no title");
            if (string.IsNullOrWhiteSpace(c.Pitch)) problems.Add(who + ": no pitch");
            if (string.IsNullOrWhiteSpace(c.Thanks)) problems.Add(who + ": no thanks");
            if (!PlaceExists(c.Place)) problems.Add(who + ": unknown place '" + c.Place + "'");
            if (factory != null && (string.IsNullOrEmpty(c.Owner) || !factory.Blueprints.ContainsKey(c.Owner))) problems.Add(who + ": unknown owner '" + c.Owner + "'");
            if (c.Tier < 1 || c.Tier > ProgressionCurve.MaxTier) problems.Add(who + ": tier " + c.Tier);
            if (c.Approaches == null || c.Approaches.Count < 2) problems.Add(who + ": needs at least two approaches");
            var ids = new HashSet<string>();
            if (c.Approaches != null)
                foreach (var a in c.Approaches)
                {
                    string aw = who + "/" + (a?.Id ?? "(null)");
                    if (a == null || string.IsNullOrEmpty(a.Id) || !ids.Add(a.Id)) { problems.Add(aw + ": missing or duplicate approach id"); continue; }
                    if (Array.IndexOf(ConcernKinds.All, a.Kind) < 0) problems.Add(aw + ": unknown kind '" + a.Kind + "'");
                    if (string.IsNullOrWhiteSpace(a.Label) || string.IsNullOrWhiteSpace(a.Objective)) problems.Add(aw + ": needs a label and an objective");
                    if (a.Triggers == null || a.Triggers.Count == 0) problems.Add(aw + ": no trigger can ever finish it");
                    CheckPredicates(a.Triggers, aw, problems);
                    CheckActions(a.Outcome, aw, problems);
                    CheckActions(a.Offer, aw + "/offer", problems);
                    if ((a.Offer?.Count ?? 0) > 0 && string.IsNullOrWhiteSpace(a.OfferText)) problems.Add(aw + ": an offer needs its text");
                }
            if (c.Refusal == null || string.IsNullOrWhiteSpace(c.Refusal.Text) || string.IsNullOrWhiteSpace(c.Refusal.Reply))
                problems.Add(who + ": every concern needs a spoken refusal and its reply");
            else CheckActions(c.Refusal.Effects, who + "/refusal", problems);
            CheckPredicates(c.Requires, who + "/requires", problems);
            if (c.Reward != null) CheckActions(c.Reward.Effects, who + "/reward", problems);
            if (c.Next != null)
                foreach (var next in c.Next)
                    if (Get(next) == null) problems.Add(who + ": next concern '" + next + "' does not exist");
            return problems;
        }

        static bool PlaceExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == name) return true;
            return false;
        }
        static void CheckPredicates(List<ConversationParam> list, string where, List<string> problems)
        {
            if (list == null) return;
            foreach (var p in list)
                if (p == null || !ConversationPredicates.IsRegistered(p.Key)) problems.Add(where + ": unknown predicate '" + p?.Key + "'");
        }
        static void CheckActions(List<ConversationParam> list, string where, List<string> problems)
        {
            if (list == null) return;
            foreach (var p in list)
                if (p == null || !ConversationActions.IsRegistered(p.Key)) problems.Add(where + ": unknown action '" + p?.Key + "'");
        }
    }
}
