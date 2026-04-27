namespace CavesOfOoo.Core
{
    /// <summary>
    /// Per-NPC knowledge store. Uses int tiers (0=ignorant, 1=does-not-know,
    /// 2=suspects, 3=knows). Knowledge only ever increases — once an NPC learns
    /// something, that knowledge cannot be taken away.
    ///
    /// Implements ISaveSerializable because WritePublicFields can't handle
    /// the underlying Dictionary correctly.
    /// </summary>
    public sealed class KnowledgePart : Part, ISaveSerializable
    {
        public override string Name => "Knowledge";

        private readonly FactBag _knowledge = new FactBag();

        public int GetKnowledge(string topic) => _knowledge.Get(topic);

        public void Reveal(string topic, int tier)
        {
            int current = _knowledge.Get(topic);
            if (tier > current)
                _knowledge.Set(topic, tier);
        }

        public bool Knows(string topic, int minTier) => GetKnowledge(topic) >= minTier;

        // --- ISaveSerializable ---

        public void Save(SaveWriter writer) => _knowledge.Save(writer);

        public void Load(SaveReader reader) => _knowledge.Load(reader);
    }
}
