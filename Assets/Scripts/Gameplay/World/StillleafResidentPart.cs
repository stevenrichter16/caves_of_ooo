namespace CavesOfOoo.Core
{
    /// <summary>Saved identity of an authored Stillleaf Archive resident. After
    /// a load, the chain's conversation verbs are re-registered before the
    /// resident can be spoken to (mirrors MorrowfastResidentPart).</summary>
    public sealed class StillleafResidentPart : Part
    {
        public override string Name => "StillleafResident";
        public string ResidentId = "";
        public override void OnAfterLoad(SaveReader reader) { StillleafArchiveContent.EnsureRegistered(); }
    }
}
