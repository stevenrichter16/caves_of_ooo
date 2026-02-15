namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part that marks an entity as having a conversation.
    /// The ConversationID points to conversation data loaded by ConversationLoader.
    /// Attached to NPC blueprints that the player can talk to.
    /// </summary>
    public class ConversationPart : Part
    {
        public override string Name => "Conversation";

        /// <summary>
        /// The ID of the conversation data (matches ConversationData.ID in JSON).
        /// Set from blueprint params.
        /// </summary>
        public string ConversationID;
    }
}
