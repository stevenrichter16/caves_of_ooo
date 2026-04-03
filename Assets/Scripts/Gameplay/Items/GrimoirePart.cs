namespace CavesOfOoo.Core
{
    /// <summary>
    /// Readable grimoire item that grants knowledge (a property) to the reader.
    /// Declares a "Read" inventory action. When read, sets a property on the actor
    /// and shows an announcement modal. The grimoire is NOT consumed on use.
    /// Blueprint params: KnowledgeProperty, LearnMessage, AlreadyKnownMessage.
    /// </summary>
    public class GrimoirePart : Part
    {
        public override string Name => "Grimoire";

        /// <summary>Property key granted to the reader (e.g., "KnowsPurifyWater").</summary>
        public string KnowledgeProperty = "";

        /// <summary>Announcement text shown when the reader learns from this grimoire.</summary>
        public string LearnMessage = "";

        /// <summary>Message shown if the reader already has this knowledge.</summary>
        public string AlreadyKnownMessage = "You already know this.";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                    actions.AddAction("Read", "read", "ReadGrimoire", 'r', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "ReadGrimoire") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                return DoRead(actor, e);
            }

            return true;
        }

        private bool DoRead(Entity actor, GameEvent e)
        {
            if (string.IsNullOrEmpty(KnowledgeProperty))
            {
                MessageLog.Add("The pages are blank.");
                e.Handled = true;
                return false;
            }

            if (actor.Properties.ContainsKey(KnowledgeProperty))
            {
                MessageLog.Add(AlreadyKnownMessage);
                e.Handled = true;
                return false;
            }

            actor.Properties[KnowledgeProperty] = "true";

            if (!string.IsNullOrEmpty(LearnMessage))
                MessageLog.AddAnnouncement(LearnMessage);
            else
                MessageLog.AddAnnouncement($"You study {ParentEntity.GetDisplayName()} and gain new knowledge.");

            e.Handled = true;
            return false;
        }
    }
}
