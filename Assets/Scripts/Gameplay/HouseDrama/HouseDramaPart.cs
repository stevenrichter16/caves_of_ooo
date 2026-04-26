namespace CavesOfOoo.Core
{
    /// <summary>
    /// Attached to entities that participate in a House Drama as a named NPC role.
    /// Stores drama identity and role so conversation predicates and actions can
    /// query drama state via the speaker entity.
    ///
    /// Tags set on Initialize() are readable by the existing IfSpeakerHaveTag predicate,
    /// making drama-role-gated conversation nodes work without new predicates.
    /// </summary>
    public class HouseDramaPart : Part
    {
        public override string Name => "HouseDrama";

        /// <summary>Drama ID — matches HouseDramaData.ID in the JSON.</summary>
        public string DramaID;

        /// <summary>
        /// NPC role within the drama:
        /// FoundationalDead | LostDead | DiminishedHead | RisingInheritor |
        /// NamedAntagonist | SilencedHelper
        /// </summary>
        public string NpcRole;

        /// <summary>NPC ID — matches the Id field in NpcRoleData in the drama JSON.</summary>
        public string NpcId;

        public override void Initialize()
        {
            if (ParentEntity == null) return;

            // Expose drama identity as tags so existing conversation predicates
            // (IfSpeakerHaveTag) can gate dialogue without new predicate code.
            ParentEntity.SetTag("DramaID", DramaID ?? "");
            ParentEntity.SetTag("NpcRole", NpcRole ?? "");
            ParentEntity.SetTag("DramaNpcId", NpcId ?? "");
        }
    }
}
