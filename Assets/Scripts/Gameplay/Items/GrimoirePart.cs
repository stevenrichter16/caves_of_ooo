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

        /// <summary>If set, grants this mutation (spell) when read instead of a knowledge property.
        /// LEGACY — the mutations→skills migration replaces these with
        /// <see cref="SkillClassName"/> batch by batch; this field dies in M4.</summary>

        /// <summary>Level to grant the mutation at. LEGACY, dies in M4.</summary>

        /// <summary>If set, teaches this SKILL when read — the migration-era
        /// replacement for <see cref="MutationClassName"/>. Skills are flat,
        /// so there is no level field. The skill is also SP-buyable in the
        /// tree (user decision, plan §5): reading a book you already know
        /// prints <see cref="AlreadyKnownMessage"/>, buying then finding is
        /// the same in reverse.</summary>
        public string SkillClassName = "";

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
            // Skill-teaching grimoire — the migration-era path. Mirrors the
            // mutation branch below one-for-one: HasSkill ≙ HasMutation,
            // AddSkill ≙ AddMutation, same messages, same Handled semantics.
            if (!string.IsNullOrEmpty(SkillClassName))
            {
                var skills = actor.GetPart<CavesOfOoo.Skills.SkillsPart>();
                if (skills == null)
                {
                    MessageLog.Add("The symbols are beyond your comprehension.");
                    e.Handled = true;
                    return false;
                }

                if (skills.HasSkill(SkillClassName))
                {
                    MessageLog.Add(AlreadyKnownMessage);
                    e.Handled = true;
                    return false;
                }

                if (skills.AddSkill(SkillClassName, source: "grimoire"))
                {
                    if (!string.IsNullOrEmpty(LearnMessage))
                        MessageLog.AddAnnouncement(LearnMessage);
                    else
                        MessageLog.AddAnnouncement($"You study {ParentEntity.GetDisplayName()} and learn a new rite.");
                }
                else
                {
                    MessageLog.Add("The symbols are beyond your comprehension.");
                }

                e.Handled = true;
                return false;
            }

            // Knowledge-granting grimoire: sets a property on the reader
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
