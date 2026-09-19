using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stillleaf Archive SA.5: the salt file remembers who broke it. Breaking
    /// it open spills the key (DestructionSystem's own behaviour); this part
    /// makes the theft cost standing with Curation, once, and leaves a mark
    /// on the player the Indexer's telling can read. Anyone else breaking it
    /// costs the player nothing.
    /// </summary>
    public sealed class StillleafFilePart : Part
    {
        public override string Name => "StillleafFile";
        /// <summary>Player int property: 1 once the player has broken the file.</summary>
        public const string Broken = "StillleafFileBroken";
        public const int CurationForABrokenFile = -8;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Destroyed") return true;
            var source = e.GetParameter<Entity>("Source");
            var player = StoryletPart.LocalPlayer;
            if (source == null || player == null || source != player || player.GetIntProperty(Broken) == 1) return true;
            player.SetIntProperty(Broken, 1);
            PlayerReputation.Modify(StillleafCustody.CurationFaction, CurationForABrokenFile);
            MessageLog.Add("You broke a Curation file open. It will be filed under: breakage, by name.");
            return true;
        }
    }
}
