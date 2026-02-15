using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of conversation action functions.
    /// Each action takes (speaker, listener, argument) and performs a side effect.
    /// Actions are executed when a player selects a dialogue choice.
    /// </summary>
    public static class ConversationActions
    {
        public delegate void ActionFunc(Entity speaker, Entity listener, string argument);

        private static Dictionary<string, ActionFunc> _actions
            = new Dictionary<string, ActionFunc>();

        private static bool _initialized;

        /// <summary>
        /// Optional: EntityFactory for GiveItem action. Set by GameBootstrap.
        /// </summary>
        public static Data.EntityFactory Factory;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterDefaults();
        }

        public static void Register(string name, ActionFunc func)
        {
            _actions[name] = func;
        }

        public static void Execute(string name, Entity speaker, Entity listener, string argument)
        {
            EnsureInitialized();

            if (_actions.TryGetValue(name, out var func))
            {
                func(speaker, listener, argument);
                return;
            }

            Debug.LogWarning($"[Conversation] Unknown action: '{name}'");
        }

        /// <summary>
        /// Execute all actions on a choice.
        /// </summary>
        public static void ExecuteAll(List<Data.ConversationParam> actions,
            Entity speaker, Entity listener)
        {
            if (actions == null) return;
            for (int i = 0; i < actions.Count; i++)
            {
                Execute(actions[i].Key, speaker, listener, actions[i].Value);
            }
        }

        private static void RegisterDefaults()
        {
            // Add a message to the game log
            Register("AddMessage", (speaker, listener, arg) =>
            {
                MessageLog.Add(arg);
            });

            // Set a tag on the listener (player): "TagName" or "TagName:Value"
            Register("SetTag", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    listener.SetTag(arg.Substring(0, colon), arg.Substring(colon + 1));
                else
                    listener.SetTag(arg, "");
            });

            // Set a property on the listener (player): "PropName:Value"
            Register("SetProperty", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    listener.Properties[arg.Substring(0, colon)] = arg.Substring(colon + 1);
                else
                    listener.Properties[arg] = "";
            });

            // Set a property on the speaker: "PropName:Value"
            Register("SetSpeakerProperty", (speaker, listener, arg) =>
            {
                if (speaker == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    speaker.Properties[arg.Substring(0, colon)] = arg.Substring(colon + 1);
                else
                    speaker.Properties[arg] = "";
            });

            // Set an int property on the listener: "PropName:IntValue"
            Register("SetIntProperty", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                string key = arg.Substring(0, colon);
                if (int.TryParse(arg.Substring(colon + 1), out int val))
                    listener.SetIntProperty(key, val);
            });

            // Give an item to the listener (player) by blueprint name
            Register("GiveItem", (speaker, listener, arg) =>
            {
                if (listener == null || Factory == null) return;
                var item = Factory.CreateEntity(arg);
                if (item == null)
                {
                    Debug.LogWarning($"[Conversation] GiveItem: blueprint '{arg}' not found.");
                    return;
                }
                var inv = listener.GetPart<InventoryPart>();
                if (inv != null)
                {
                    inv.AddObject(item);
                    MessageLog.Add($"You receive {item.GetDisplayName()}.");
                }
            });

            // Take an item from the listener (player) by blueprint name
            Register("TakeItem", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].BlueprintName == arg)
                    {
                        var item = inv.Objects[i];
                        inv.RemoveObject(item);
                        MessageLog.Add($"You hand over {item.GetDisplayName()}.");
                        return;
                    }
                }
            });

            // Open the trade screen after conversation ends
            Register("StartTrade", (speaker, listener, arg) =>
            {
                ConversationManager.PendingTradePartner = speaker;
            });

            // Initiate an attack on the NPC after conversation ends
            Register("StartAttack", (speaker, listener, arg) =>
            {
                ConversationManager.PendingAttackTarget = speaker;
            });

            // Change faction feeling: "FactionA:FactionB:Delta"
            Register("ChangeFactionFeeling", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return;
                if (!int.TryParse(parts[2], out int delta)) return;
                int current = FactionManager.GetFactionFeeling(parts[0], parts[1]);
                FactionManager.SetFactionFeeling(parts[0], parts[1], current + delta);
                if (delta > 0)
                    MessageLog.Add($"Your reputation with {parts[0]} improves.");
                else if (delta < 0)
                    MessageLog.Add($"Your reputation with {parts[0]} worsens.");
            });
        }

        public static void Reset()
        {
            _actions.Clear();
            _initialized = false;
        }
    }
}
