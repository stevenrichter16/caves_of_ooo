namespace CavesOfOoo.Core
{
    /// <summary>
    /// SCAFFOLD for Challenge 2 — "Lifesteal Trait".
    /// See Docs/PROGRAMMING-CHALLENGES.md §Challenge 2.
    ///
    /// Goal: heal the owner whenever it DEALS damage. Right now HandleEvent
    /// does nothing, so LifestealPartChallengeTests fails (RED). Fill in the
    /// TODO to turn it GREEN.
    ///
    /// Models to read:
    ///   - DamageFlashPart.cs — how to branch on e.ID == "DamageDealt" and read
    ///     e.GetIntParameter("Amount", ...). Note "DamageDealt" fires on the
    ///     ATTACKER (CombatSystem.cs:875), which is why this part lives on the
    ///     creature, not the weapon.
    ///   - BerserkEffect.cs — how to reach a stat via target.GetStat(name).
    ///   - Stat.cs — heal by raising BaseValue, but clamp so it never exceeds Max.
    /// </summary>
    public class LifestealPart : Part
    {
        public override string Name => "Lifesteal";

        /// <summary>
        /// Percent of damage dealt that is returned to the owner as healing.
        /// Public so a blueprint can tune it via reflection-injected Params
        /// (Challenge 1's lesson): { "Key": "HealPercent", "Value": "50" }.
        /// </summary>
        public int HealPercent = 50;

        public override bool HandleEvent(GameEvent e)
        {
            // TODO (Challenge 2):
            //   if (e.ID == "DamageDealt")
            //   {
            //       int dealt = e.GetIntParameter("Amount", 0);
            //       int heal  = dealt * HealPercent / 100;
            //       // raise ParentEntity's "Hitpoints" BaseValue by `heal`,
            //       // clamped to that stat's Max.
            //   }
            return true; // keep propagating to the next part
        }
    }
}
