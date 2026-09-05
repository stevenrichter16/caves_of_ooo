using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Manages active conversation state. One conversation at a time.
    /// Handles navigation between nodes, choice filtering, and action execution.
    /// </summary>
    public static class ConversationManager
    {
        public static ConversationData CurrentConversation;
        public static NodeData CurrentNode;
        public static Entity Speaker;
        public static Entity Listener;
        public static bool IsActive => CurrentConversation != null;

        /// <summary>
        /// Set by the StartTrade action before conversation ends.
        /// InputHandler checks this to transition from dialogue to trade UI.
        /// Cleared by InputHandler after opening TradeUI.
        /// </summary>
        public static Entity PendingTradePartner;

        /// <summary>
        /// Set by the StartAttack action before conversation ends.
        /// InputHandler checks this to transition from dialogue to attack confirmation.
        /// </summary>
        public static Entity PendingAttackTarget;

        // Cached visible choices for the current node
        private static List<ChoiceData> _visibleChoices = new List<ChoiceData>();
        public static IReadOnlyList<ChoiceData> VisibleChoices => _visibleChoices;

        /// <summary>
        /// Start a conversation between speaker and listener.
        /// Returns true if conversation was started successfully.
        /// </summary>
        public static bool StartConversation(Entity speaker, Entity listener)
        {
            if (speaker == null || listener == null) return false;

            var convPart = speaker.GetPart<ConversationPart>();
            if (convPart == null) return false;

            string convID = convPart.ConversationID;
            if (string.IsNullOrEmpty(convID)) return false;

            var data = ConversationLoader.Get(convID);
            if (data == null) return false;

            var startNode = data.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning($"[Conversation] No start node in conversation '{convID}'");
                return false;
            }

            // Check if speaker is willing to talk. Close-out
            // hypothesis H6 — one act of grove arson (−40, past the
            // hostile threshold) locked EVERY Choir conversation, and
            // with them the elder's mercy and the Bloom's cure, forever:
            // a permanent lockout in a game whose identity is
            // recoverable consequence. Canon resolves it: the Choir
            // does not do enmity — "the singers have not minded
            // anything for a long time", "we are patient about the
            // difference". A speaker tagged SpeaksToHostiles talks to
            // anyone; the ledger still remembers what you did. War-
            // factions (the catacomb village) keep the refusal — war
            // is war.
            if (FactionManager.IsHostile(speaker, listener)
                && !speaker.HasTag("SpeaksToHostiles"))
            {
                MessageLog.Add($"{speaker.GetDisplayName()} refuses to speak with you.");
                return false;
            }

            CurrentConversation = data;
            CurrentNode = startNode;
            Speaker = speaker;
            Listener = listener;

            // Set NPC brain to conversation mode
            var brain = speaker.GetPart<BrainPart>();
            if (brain != null)
                brain.InConversation = true;

            RefreshVisibleChoices();
            return true;
        }

        /// <summary>
        /// Select a choice by index (from the visible choices list).
        /// Executes actions and navigates to the target node.
        /// Returns false if the conversation ended.
        /// </summary>
        public static bool SelectChoice(int choiceIndex)
        {
            if (!IsActive) return false;
            if (choiceIndex < 0 || choiceIndex >= _visibleChoices.Count) return false;

            var choice = _visibleChoices[choiceIndex];

            // Execute actions
            if (!ConversationActions.TryExecuteAll(choice.Actions, Speaker, Listener))
            {
                RefreshVisibleChoices();
                return true; // The conversation remains active on its current node.
            }

            // Navigate to target
            string target = choice.Target;
            if (string.IsNullOrEmpty(target))
            {
                // Stay on current node
                RefreshVisibleChoices();
                return true;
            }

            if (target == "End")
            {
                EndConversation();
                return false;
            }

            NodeData targetNode;
            if (target == "Start")
                targetNode = CurrentConversation.GetStartNode();
            else
                targetNode = CurrentConversation.GetNode(target);

            if (targetNode == null)
            {
                Debug.LogWarning($"[Conversation] Target node '{target}' not found. Ending conversation.");
                EndConversation();
                return false;
            }

            CurrentNode = targetNode;
            RefreshVisibleChoices();
            return true;
        }

        /// <summary>
        /// End the current conversation and clean up state.
        /// </summary>
        public static void EndConversation()
        {
            if (Speaker != null)
            {
                var brain = Speaker.GetPart<BrainPart>();
                if (brain != null)
                    brain.InConversation = false;
            }

            CurrentConversation = null;
            CurrentNode = null;
            Speaker = null;
            Listener = null;
            _visibleChoices.Clear();
        }

        /// <summary>
        /// Rebuild the list of visible choices for the current node.
        /// </summary>
        /// <summary>
        /// Can this speaker actually trade? A trader has a stock table
        /// (so the restock system will keep them supplied), goods on
        /// hand, or drams to buy with. Anything else offering "[Let's
        /// trade.]" opens an empty window — the bug this replaced.
        /// Public: pinned by tests.
        /// </summary>
        public static bool CanTrade(Entity speaker)
        {
            if (speaker == null) return false;
            var inv = speaker.GetPart<InventoryPart>();
            if (inv == null) return false;

            if (speaker.GetPart<TraderPart>() != null) return true;
            if (!string.IsNullOrEmpty(
                speaker.GetProperty(TraderRestockSystem.ShopStockTableProp)))
                return true;
            if (inv.Objects.Count > 0) return true;
            // A sold-out merchant with coin can still BUY from you.
            return TradeSystem.GetDrams(speaker) > 0;
        }

        public static void RefreshVisibleChoices()
        {
            _visibleChoices.Clear();
            if (CurrentNode == null) return;

            for (int i = 0; i < CurrentNode.Choices.Count; i++)
            {
                var choice = CurrentNode.Choices[i];
                if (ConversationPredicates.CheckAll(choice.Predicates, Speaker, Listener))
                    _visibleChoices.Add(choice);
            }

            // Auto-inject "[Let's trade.]" only when the speaker can
            // ACTUALLY trade. This used to fire for any speaker with an
            // InventoryPart — which every creature inherits from the
            // Creature blueprint — so all 31 talkable NPCs offered
            // trade while only the five town shopkeepers had stock.
            // The option must never lie: a trader is someone with a
            // stock table (TraderPart), goods on the shelf, or a purse
            // to buy what the player is selling.
            if (Speaker != null && CanTrade(Speaker))
            {
                _visibleChoices.Add(new ChoiceData
                {
                    Text = "[Let's trade.]",
                    Target = "End",
                    Actions = new List<ConversationParam>
                    {
                        new ConversationParam { Key = "StartTrade", Value = "" }
                    }
                });
            }

            // Auto-inject "[Attack]" choice for all NPCs

            _visibleChoices.Add(new ChoiceData
            {
                Text = "[Attack]",
                Target = "End",
                Actions = new List<ConversationParam>
                {
                    new ConversationParam { Key = "StartAttack", Value = "" }
                }
            });
        }
    }
}
