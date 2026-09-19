using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stillleaf Archive SA.5: the register knows two things about itself.
    /// Taking it puts the keeper's slate in the player's hands, and the slate
    /// carries the keeper's last words — so a player who broke into the vault
    /// first can still make the Salt-Vault's index answer. And if the record
    /// is destroyed, the player is told and the loss can be reported to either
    /// resident; the chain closes instead of dangling.
    /// </summary>
    public sealed class StillleafRegisterPart : Part
    {
        public override string Name => "StillleafRegister";
        /// <summary>Player int property: 1 once the record has been destroyed.</summary>
        public const string Destroyed = "StillleafRegisterDestroyed";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Taken")
            {
                var taker = e.GetParameter<Entity>("Actor");
                if (taker != null && taker == StoryletPart.LocalPlayer && taker.GetIntProperty(StillleafArchiveContent.WordsKnown) == 0)
                {
                    taker.SetIntProperty(StillleafArchiveContent.WordsKnown, 1);
                    MessageLog.Add("The slate tied to the cover reads, in the keeper's hand: \"" + StillleafArchiveContent.LastWords + "\" Those were the keeper's last words; Curation files its dead by them.");
                }
            }
            else if (e.ID == "Destroyed")
            {
                var player = StoryletPart.LocalPlayer;
                if (player != null && player.GetIntProperty(Destroyed) == 0)
                {
                    player.SetIntProperty(Destroyed, 1);
                    MessageLog.Add("The Stillleaf register is destroyed. What it listed is not written anywhere else.");
                }
            }
            return true;
        }
    }
}
