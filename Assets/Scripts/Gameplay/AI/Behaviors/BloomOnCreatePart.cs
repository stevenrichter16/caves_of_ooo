namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B7 — a body the Bloom already wears when it is found. Applies
    /// <see cref="BloomedEffect"/> once, at creation; everything after that (the drive,
    /// the stride, the eruption at death) is the effect's own. Canon
    /// (Lore/Factions/08_DrivingBloom.md): "Hosts as tools, not members."
    /// </summary>
    public sealed class BloomOnCreatePart : Part
    {
        public override string Name => "BloomOnCreate";

        public bool Applied;

        public override bool HandleEvent(GameEvent e)
        {
            if (!Applied && (e.ID == "ObjectCreated" || e.ID == "BeginTakeAction") && ParentEntity != null)
            {
                Applied = true;
                if (!ParentEntity.HasEffect<BloomedEffect>()) ParentEntity.ApplyEffect(new BloomedEffect());
            }
            return true;
        }
    }
}
