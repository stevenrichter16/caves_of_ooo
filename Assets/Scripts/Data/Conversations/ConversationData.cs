using System;
using System.Collections.Generic;

namespace CavesOfOoo.Data
{
    [Serializable]
    public class ConversationParam
    {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// Deserialized conversation data from JSON.
    /// A conversation is a tree of Nodes (NPC speech) containing Choices (player responses).
    /// </summary>
    [Serializable]
    public class ConversationData
    {
        public string ID;
        public List<NodeData> Nodes = new List<NodeData>();

        public NodeData GetNode(string nodeID)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].ID == nodeID)
                    return Nodes[i];
            }
            return null;
        }

        public NodeData GetStartNode()
        {
            return GetNode("Start") ?? (Nodes.Count > 0 ? Nodes[0] : null);
        }
    }

    [Serializable]
    public class NodeData
    {
        public string ID;
        public string Text;
        public bool AllowEscape = true;
        public List<ChoiceData> Choices = new List<ChoiceData>();
    }

    [Serializable]
    public class ChoiceData
    {
        public string Text;

        /// <summary>
        /// The node ID to navigate to. Special values:
        /// "End" - close the conversation
        /// "Start" - return to the Start node
        /// "" or null - stay on the current node
        /// </summary>
        public string Target;

        /// <summary>
        /// Conditions that must all pass for this choice to be visible.
        /// Key = predicate name, Value = argument.
        /// </summary>
        public List<ConversationParam> Predicates;

        /// <summary>
        /// Side effects executed when this choice is selected.
        /// Key = action name, Value = argument.
        /// </summary>
        public List<ConversationParam> Actions;

        public string GetPredicate(string key)
        {
            if (Predicates == null) return null;
            for (int i = 0; i < Predicates.Count; i++)
            {
                if (Predicates[i].Key == key)
                    return Predicates[i].Value;
            }
            return null;
        }

        public string GetAction(string key)
        {
            if (Actions == null) return null;
            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i].Key == key)
                    return Actions[i].Value;
            }
            return null;
        }
    }

    /// <summary>
    /// Root wrapper for the JSON file format: { "Conversations": [...] }
    /// </summary>
    [Serializable]
    public class ConversationFileData
    {
        public List<ConversationData> Conversations = new List<ConversationData>();
    }
}
