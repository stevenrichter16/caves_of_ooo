namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part that marks an entity as having a conversation. The
    /// <see cref="ConversationID"/> points to conversation data loaded by
    /// <c>ConversationLoader</c>. Attached to NPC blueprints the player can
    /// talk to.
    ///
    /// Also declares a "Chat" action for the world action menu
    /// (see <c>Docs/Plans/WORLD_ACTION_MENU_PLAN.md</c>, Phase 4b). When
    /// selected, fires an <c>InventoryAction</c> with <c>Command = "Chat"</c>,
    /// which this part handles by calling
    /// <see cref="ConversationManager.StartConversation"/>. If the NPC lacks
    /// a valid ConversationID, falls back to a default "Hi." greeting.
    ///
    /// UI opening (the dialogue panel itself) is not part of this handler.
    /// Today that's <c>InputHandler.OpenDialogue</c>, which the 'c'+direction
    /// path calls inline. The menu-based path (Phase 4d) will detect
    /// <see cref="ConversationManager.IsActive"/> after the action resolves
    /// and open the dialogue UI then.
    /// </summary>
    public class ConversationPart : Part
    {
        public override string Name => "Conversation";

        /// <summary>
        /// The ID of the conversation data (matches <c>ConversationData.ID</c>
        /// in JSON). Set from blueprint params.
        /// </summary>
        public string ConversationID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                // Priority 10: above Examine (0), below Open (30). The usual
                // primary interaction with an NPC. Hotkey 'c' matches the
                // existing 'c'+direction talk keybind so muscle memory
                // transfers.
                actions?.AddAction("Chat", "chat", "Chat", 'c', 10);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == "Chat")
                {
                    HandleChatCommand(e);
                    e.Handled = true;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Start the conversation if the NPC has a valid ConversationID. If
        /// <see cref="ConversationManager.StartConversation"/> returns false,
        /// distinguish between the hostile-refusal case (already logged by
        /// StartConversation) and the no-dialogue-data case (logs the
        /// fallback "Hi." greeting here).
        /// </summary>
        private void HandleChatCommand(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            if (actor == null) return;

            if (ConversationManager.StartConversation(ParentEntity, actor))
            {
                // Success — ConversationManager state is set. The UI layer
                // (InputHandler / world action menu) is responsible for
                // detecting this and opening the dialogue panel.
                return;
            }

            // StartConversation returned false. Possible reasons:
            //   1. NPC is hostile to actor — "refuses to speak" is already
            //      logged by StartConversation. Don't double-message.
            //   2. NPC has no ConversationPart — can't happen here (this IS
            //      ConversationPart).
            //   3. ConversationID empty, data missing, or no start node — NPC
            //      exists but has nothing to say. Fall back to "Hi." so the
            //      interaction doesn't feel broken.
            if (FactionManager.IsHostile(ParentEntity, actor))
                return;

            MessageLog.Add($"{ParentEntity.GetDisplayName()} says, \"Hi.\"");
        }
    }
}
