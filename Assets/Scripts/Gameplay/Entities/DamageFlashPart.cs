using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Flashes an entity's color when it takes damage.
    /// Listens for TakeDamage events and temporarily overrides
    /// the entity's ColorString during Render events.
    /// </summary>
    public class DamageFlashPart : Part
    {
        public override string Name => "DamageFlash";

        /// <summary>
        /// Static callback invoked when the player entity takes damage.
        /// The presentation layer subscribes to trigger screen shake.
        /// </summary>
        public static Action<int> OnPlayerDamaged;

        /// <summary>
        /// Static callback invoked when the player entity deals damage.
        /// The presentation layer subscribes to trigger a lighter screen shake.
        /// </summary>
        public static Action<int> OnPlayerDealtDamage;

        private int _flashFramesRemaining;

        private const int FlashDuration = 3;
        private const string FlashColor = "&R";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeDamage")
            {
                _flashFramesRemaining = FlashDuration;

                // Notify presentation layer if this is the player
                if (ParentEntity != null && ParentEntity.HasTag("Player"))
                {
                    int amount = e.GetIntParameter("Amount", 1);
                    OnPlayerDamaged?.Invoke(amount);
                }
                return true;
            }

            if (e.ID == "DamageDealt")
            {
                if (ParentEntity != null && ParentEntity.HasTag("Player"))
                {
                    int amount = e.GetIntParameter("Amount", 1);
                    OnPlayerDealtDamage?.Invoke(amount);
                }
                return true;
            }

            if (e.ID == "Render" && _flashFramesRemaining > 0)
            {
                e.SetParameter("ColorString", FlashColor);
                _flashFramesRemaining--;
                return true;
            }

            return true;
        }
    }
}
