using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of conversation predicate functions.
    /// Each predicate takes (speaker, listener, argument) and returns true if the condition is met.
    /// Predicates prefixed with "IfNot" are auto-generated inverses.
    /// </summary>
    public static class ConversationPredicates
    {
        public delegate bool PredicateFunc(Entity speaker, Entity listener, string argument);

        private static Dictionary<string, PredicateFunc> _predicates
            = new Dictionary<string, PredicateFunc>();

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterDefaults();
        }

        public static void Register(string name, PredicateFunc func)
        {
            _predicates[name] = func;
        }

        /// <summary>
        /// Returns true if a predicate with this name is registered, OR if the
        /// name is an auto-invertible "IfNot..." form whose base predicate
        /// "If..." is registered. Used by content loaders (e.g. StoryletRegistry)
        /// to fail-fast on unknown predicate names at load time, since
        /// Evaluate() returns true for unknown names by default.
        /// </summary>
        public static bool IsRegistered(string name)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(name)) return false;
            if (_predicates.ContainsKey(name)) return true;
            if (name.StartsWith("IfNot") && name.Length > 5)
                return _predicates.ContainsKey("If" + name.Substring(5));
            return false;
        }

        public static bool Evaluate(string name, Entity speaker, Entity listener, string argument)
        {
            EnsureInitialized();

            if (_predicates.TryGetValue(name, out var func))
                return func(speaker, listener, argument);

            // Auto-handle IfNot* by inverting the base predicate
            if (name.StartsWith("IfNot") && name.Length > 5)
            {
                string baseName = "If" + name.Substring(5);
                if (_predicates.TryGetValue(baseName, out var baseFunc))
                    return !baseFunc(speaker, listener, argument);
            }

            UnityEngine.Debug.LogWarning($"[Conversation] Unknown predicate: '{name}'");
            return true; // unknown predicates pass by default
        }

        /// <summary>
        /// Check all predicates on a choice. Returns true if all pass (AND logic).
        /// </summary>
        public static bool CheckAll(List<Data.ConversationParam> predicates,
            Entity speaker, Entity listener)
        {
            if (predicates == null || predicates.Count == 0) return true;

            for (int i = 0; i < predicates.Count; i++)
            {
                if (!Evaluate(predicates[i].Key, speaker, listener, predicates[i].Value))
                    return false;
            }
            return true;
        }

        private static void RegisterDefaults()
        {
            // Tag checks (on listener/player)
            Register("IfHaveTag", (speaker, listener, arg) =>
                listener != null && listener.HasTag(arg));

            // Property checks (on listener/player)
            Register("IfHaveProperty", (speaker, listener, arg) =>
                listener != null && listener.Properties.ContainsKey(arg));

            // IntProperty checks
            Register("IfHaveIntProperty", (speaker, listener, arg) =>
                listener != null && listener.IntProperties.ContainsKey(arg));

            // Tag on speaker
            Register("IfSpeakerHaveTag", (speaker, listener, arg) =>
                speaker != null && speaker.HasTag(arg));

            // Property on speaker
            Register("IfSpeakerHaveProperty", (speaker, listener, arg) =>
                speaker != null && speaker.Properties.ContainsKey(arg));

            // Faction feeling check: "FactionA:FactionB:MinValue"
            // or simpler: just minimum feeling as int
            Register("IfFactionFeelingAtLeast", (speaker, listener, arg) =>
            {
                if (!int.TryParse(arg, out int minFeeling)) return false;
                int feeling = FactionManager.GetFeeling(listener, speaker);
                return feeling >= minFeeling;
            });

            // Check if player has item by blueprint name in inventory
            Register("IfHaveItem", (speaker, listener, arg) =>
            {
                if (listener == null) return false;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return false;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].BlueprintName == arg)
                        return true;
                }
                return false;
            });

            // Stat check: "StatName:MinValue"
            Register("IfStatAtLeast", (speaker, listener, arg) =>
            {
                if (listener == null) return false;
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                string stat = arg.Substring(0, colon);
                if (!int.TryParse(arg.Substring(colon + 1), out int minVal)) return false;
                return listener.GetStatValue(stat) >= minVal;
            });

            // Check if not hostile
            Register("IfNotHostile", (speaker, listener, arg) =>
                !FactionManager.IsHostile(speaker, listener));

            // Check player reputation with a faction: "FactionName:AttitudeLevel"
            // e.g., "RotChoir:Liked" checks if player attitude >= Liked
            Register("IfReputationAtLeast", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                string faction = arg.Substring(0, colon);
                string levelStr = arg.Substring(colon + 1);
                if (!System.Enum.TryParse<PlayerReputation.Attitude>(levelStr, out var required))
                    return false;
                return (int)PlayerReputation.GetAttitude(faction) >= (int)required;
            });

            // Check if player has any item with a specific tag in inventory
            Register("IfHaveItemWithTag", (speaker, listener, arg) =>
            {
                if (listener == null || string.IsNullOrEmpty(arg)) return false;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return false;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag(arg))
                        return true;
                }
                return false;
            });

            Register("IfSettlementSiteStage", (speaker, listener, arg) =>
            {
                if (speaker == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return false;

                string[] parts = arg.Split(':');
                if (parts.Length != 2)
                    return false;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return false;

                RepairableSiteState site = SettlementManager.Current.GetSite(settlementId, parts[0]);
                if (site == null)
                    return false;

                RepairStage stage;
                if (!Enum.TryParse(parts[1], out stage))
                    return false;

                return site.Stage == stage;
            });

            // ── House Drama predicates ────────────────────────────────────────

            // arg: "DramaID"
            Register("IfDramaActive", (speaker, listener, arg) =>
                HouseDramaRuntime.IsDramaActive(arg));

            // arg: "DramaID:PointID:ExpectedState"
            Register("IfPressurePointState", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return false;
                return HouseDramaRuntime.GetPressurePointState(parts[0], parts[1]) == parts[2];
            });

            // arg: "DramaID:PointID:ExpectedPathID"
            Register("IfPathTaken", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return false;
                return HouseDramaRuntime.GetPathTaken(parts[0], parts[1]) == parts[2];
            });

            // arg: "DramaID:NpcID:FactID"
            Register("IfWitnessKnows", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return false;
                return HouseDramaRuntime.WitnessKnows(parts[0], parts[1], parts[2]);
            });

            // arg: "DramaID:Score"
            Register("IfCorruptionAtLeast", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                if (!int.TryParse(arg.Substring(colon + 1), out int min)) return false;
                return HouseDramaRuntime.GetCorruption(arg.Substring(0, colon)) >= min;
            });

            // ── Narrative state predicates ────────────────────────────────────

            // arg: "key:op:value"  op ∈ { =, !=, <, >, <=, >= }
            // Fail-closed: malformed args or missing state return false.
            Register("IfFact", (speaker, listener, arg) =>
            {
                var ns = NarrativeStatePart.Current;
                if (ns == null) return false;
                var parts = arg.Split(':', 3);
                if (parts.Length < 3) return false;
                if (!int.TryParse(parts[2], out int threshold)) return false;
                int actual = ns.GetFact(parts[0]);
                string op = parts[1];
                if (op == "=")  return actual == threshold;
                if (op == "!=") return actual != threshold;
                if (op == ">")  return actual >  threshold;
                if (op == ">=") return actual >= threshold;
                if (op == "<")  return actual <  threshold;
                if (op == "<=") return actual <= threshold;
                return false;
            });

            // arg: "topic:minTier"
            // Fail-closed: malformed args or missing KnowledgePart return false.
            Register("IfSpeakerKnows", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                if (!int.TryParse(arg.Substring(colon + 1), out int minTier)) return false;
                var kp = speaker?.GetPart<KnowledgePart>();
                if (kp == null) return false;
                return kp.Knows(arg.Substring(0, colon), minTier);
            });

            // ── QS.2 quest predicates ────────────────────────────────────
            // Per Docs/QUEST-SYSTEM.md. All read from the StoryletPart
            // singleton. If the storylet system isn't bootstrapped yet
            // (StoryletPart.Current == null), every predicate fails closed.

            // IfQuestActive(questId) — is this quest currently in
            // StoryletPart.Current's _quests dict?
            Register("IfQuestActive", (speaker, listener, arg) =>
                CavesOfOoo.Storylets.StoryletPart.Current != null
                && CavesOfOoo.Storylets.StoryletPart.Current.IsQuestActive(arg));

            // IfQuestCompleted(questId) — is this quest in the
            // _completedQuests set? (Disjoint from active.)
            Register("IfQuestCompleted", (speaker, listener, arg) =>
                CavesOfOoo.Storylets.StoryletPart.Current != null
                && CavesOfOoo.Storylets.StoryletPart.Current.IsQuestCompleted(arg));

            // Q6: IfQuestFailed(questId) — has the player failed this quest
            // (and not since re-taken or completed it)? Auto-inverse
            // IfNotQuestFailed via the IfNot* mechanism. Lets a quest-giver
            // branch to "you failed me" dialogue. (Failed quests remain
            // re-takeable: IfQuestNotStarted is intentionally unchanged.)
            Register("IfQuestFailed", (speaker, listener, arg) =>
                CavesOfOoo.Storylets.StoryletPart.Current != null
                && CavesOfOoo.Storylets.StoryletPart.Current.IsQuestFailed(arg));

            // IfQuestNotStarted(questId) — true iff the player has
            // never started OR completed this quest. NOT the same as
            // !IfQuestActive (which would be true for completed quests
            // too). Designed so quest-givers can hide "[Take this
            // quest]" choices once the player either has it active OR
            // has already completed it.
            Register("IfQuestNotStarted", (speaker, listener, arg) =>
            {
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return true;  // pre-bootstrap = nothing started
                return !sp.IsQuestActive(arg) && !sp.IsQuestCompleted(arg);
            });

            // IfQuestStage(questId:stageRef) — true iff quest is active
            // AND its current stage matches `stageRef`. stageRef can be
            // either a numeric stage index ("0", "1") or a string
            // matching QuestStageData.ID. The stage-ID lookup requires
            // resolving the storylet's QuestData via StoryletRegistry.
            Register("IfQuestStage", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return false;
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                string questId = arg.Substring(0, colon);
                string stageRef = arg.Substring(colon + 1);

                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return false;
                var state = sp.GetQuestState(questId);
                if (state == null) return false;

                // Numeric stage index match (fastest path).
                if (int.TryParse(stageRef, out int idx))
                    return state.CurrentStageIndex == idx;

                // String stage-ID match — resolve via registry.
                var quest = CavesOfOoo.Storylets.StoryletRegistry.FindQuest(questId);
                if (quest == null) return false;
                if (state.CurrentStageIndex < 0
                    || state.CurrentStageIndex >= quest.Stages.Count) return false;
                return quest.Stages[state.CurrentStageIndex].ID == stageRef;
            });

            // Q3.3: IfObjectiveFinished(questId:objId) — true iff the
            // objective is in the quest's current-stage finished set. The
            // auto-inverse IfNotObjectiveFinished is generated by the IfNot*
            // mechanism. Lets dialogue gate on objective progress (e.g.
            // "[Report back]" only after the kill objective is done).
            Register("IfObjectiveFinished", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return false;
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return false;
                return sp.IsObjectiveFinished(arg.Substring(0, colon), arg.Substring(colon + 1));
            });

            // Q5.7 (Docs/QUEST-DESIGN-CATALOG.md) — the TIMED primitive. True
            // while the quest's CURRENT stage is no older than <maxTurns> turns
            // (now - EnteredStageAtTurn <= maxTurns). Gate a success objective on
            // it ("escape within N turns"); once the window lapses it goes false
            // so the objective can no longer auto-complete. arg = "questId:maxTurns".
            Register("IfStageAgeAtMost", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return false;
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                if (!int.TryParse(arg.Substring(colon + 1), out int maxTurns)) return false;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                var state = sp?.GetQuestState(arg.Substring(0, colon));
                if (state == null) return false;
                int now = TurnManager.Active?.TickCount ?? 0;
                return (now - state.EnteredStageAtTurn) <= maxTurns;
            });
        }

        private static string ResolveSettlementId(Entity speaker)
        {
            if (speaker == null)
                return null;

            string settlementId;
            return speaker.Properties.TryGetValue("SettlementId", out settlementId)
                ? settlementId
                : null;
        }

        public static void Reset()
        {
            _predicates.Clear();
            _initialized = false;
        }
    }
}
