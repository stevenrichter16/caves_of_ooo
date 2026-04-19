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
            // When one side is "Player", routes through PlayerReputation.
            Register("ChangeFactionFeeling", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return;
                if (!int.TryParse(parts[2], out int delta)) return;

                if (parts[1] == "Player")
                {
                    // "RotChoir:Player:5" → modify player rep with RotChoir
                    PlayerReputation.Modify(parts[0], delta);
                }
                else if (parts[0] == "Player")
                {
                    // "Player:RotChoir:5" → modify player rep with RotChoir
                    PlayerReputation.Modify(parts[1], delta);
                }
                else
                {
                    // NPC-to-NPC faction feeling
                    int current = FactionManager.GetFactionFeeling(parts[0], parts[1]);
                    FactionManager.SetFactionFeeling(parts[0], parts[1], current + delta);
                    if (delta > 0)
                        MessageLog.Add($"Relations between {FactionManager.GetDisplayName(parts[0])} and {FactionManager.GetDisplayName(parts[1])} improve.");
                    else if (delta < 0)
                        MessageLog.Add($"Relations between {FactionManager.GetDisplayName(parts[0])} and {FactionManager.GetDisplayName(parts[1])} worsen.");
                }
            });

            Register("ResolveSettlementSite", (speaker, listener, arg) =>
            {
                if (speaker == null || listener == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return;

                string[] parts = arg.Split(':');
                if (parts.Length != 2)
                    return;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return;

                RepairMethodId method;
                if (!Enum.TryParse(parts[1], out method))
                    return;

                if (SettlementManager.Current.ApplyRepairMethod(settlementId, parts[0], method, listener))
                {
                    SettlementManager.Current.RefreshActiveZonePresentation(SettlementRuntime.ActiveZone);
                    SettlementRuntime.MarkZoneDirty();
                }
            });

            // Copy the first grimoire in the player's inventory, producing a GrimoireCopy
            Register("CopyGrimoire", (speaker, listener, arg) =>
            {
                if (listener == null || Factory == null) return;

                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;

                Entity grimoire = null;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag("Grimoire") && !inv.Objects[i].HasTag("GrimoireCopy"))
                    {
                        grimoire = inv.Objects[i];
                        break;
                    }
                }

                if (grimoire == null)
                {
                    MessageLog.Add("You don't have a grimoire to copy.");
                    return;
                }

                var grimoirePart = grimoire.GetPart<GrimoirePart>();
                if (grimoirePart == null) return;

                Entity copy = Factory.CreateEntity("GrimoireCopy");
                if (copy == null) return;

                var copyPart = copy.GetPart<GrimoirePart>();
                if (copyPart != null)
                {
                    copyPart.KnowledgeProperty = grimoirePart.KnowledgeProperty;
                    copyPart.LearnMessage = grimoirePart.LearnMessage;
                    copyPart.AlreadyKnownMessage = grimoirePart.AlreadyKnownMessage;
                }

                var copyRender = copy.GetPart<RenderPart>();
                var origRender = grimoire.GetPart<RenderPart>();
                if (copyRender != null && origRender != null)
                    copyRender.DisplayName = "copy of " + origRender.DisplayName;

                inv.AddObject(copy);
                MessageLog.AddAnnouncement("The scribe carefully copies the grimoire. You receive the copy.");
            });

            // Remove the first item matching a tag from the player's inventory
            Register("TakeItemWithTag", (speaker, listener, arg) =>
            {
                if (listener == null || string.IsNullOrEmpty(arg)) return;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag(arg))
                    {
                        var item = inv.Objects[i];
                        inv.RemoveObject(item);
                        MessageLog.Add($"You hand over {item.GetDisplayName()}.");
                        return;
                    }
                }
            });

            // M2.1: Pacify the speaker (the NPC) for N turns — e.g., a
            // Charisma-gated "Stand down" dialogue branch can non-violently
            // resolve a combat scenario. Argument is an integer duration in
            // turns; default 100 when empty/invalid. Idempotent: if a
            // NoFightGoal is already present on the speaker, this is a
            // no-op (no stacking, no duration extension) so chained calls
            // can't accidentally reset an ongoing pacification.
            //
            // Note: NoFightGoal suppresses AIBoredEvent on the pacified
            // entity, so AISelfPreservation won't push RetreatGoal while the
            // NPC is calmed. See NoFightGoal's xml-doc for the broader
            // gotcha.
            Register("PushNoFightGoal", (speaker, listener, arg) =>
            {
                if (speaker == null) return;
                var brain = speaker.GetPart<BrainPart>();
                if (brain == null) return;
                if (brain.HasGoal<NoFightGoal>()) return;

                // `int.TryParse` writes 0 to the out param on failure, which
                // NoFightGoal treats as INFINITE — so a typo in the dialogue
                // JSON would silently pacify the NPC forever. Guard by only
                // taking the parsed value when parse succeeds AND the value
                // is positive. Rejects "0" for the same reason (authors
                // wanting infinite should use the auto-pacify path, not
                // this action).
                int duration = 100;
                if (!string.IsNullOrEmpty(arg)
                    && int.TryParse(arg, out int parsed)
                    && parsed > 0)
                {
                    duration = parsed;
                }

                brain.PushGoal(new NoFightGoal(duration, wander: false));
                MessageLog.Add($"{speaker.GetDisplayName()} stands down.");
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
            _actions.Clear();
            _initialized = false;
        }
    }
}
