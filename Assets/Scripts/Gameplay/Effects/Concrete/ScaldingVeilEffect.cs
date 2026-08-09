namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM8 — the payoff of the Rite of the Scalding Veil.
    /// The water boiling off you scalds whatever strikes you.
    ///
    /// <para><b>Why it exists.</b> Every other rite spends a status on an
    /// ENEMY. The Scalding Veil spends one on YOURSELF: being soaked is
    /// normally a liability (it is what sets you up to be shocked and
    /// frozen), and this is the one way to cash your own debuff in.
    /// Turning a problem into an asset is a different kind of decision
    /// from turning a setup into damage.</para>
    ///
    /// <para><b>Retaliation, not an aura.</b> It fires on
    /// <see cref="OnTakeDamage"/>, so it answers attackers rather than
    /// ticking on bystanders — you have to actually be hit for the steam
    /// to bite, which keeps it a defensive posture rather than free
    /// area damage.</para>
    /// </summary>
    public class ScaldingVeilEffect : Effect
    {
        public override string DisplayName => "scalding veil";
        public override int GetEffectType() => TYPE_GENERAL;

        /// <summary>Heat damage dealt back to an attacker.</summary>
        public int Scald = 3;

        /// <summary>Turns of <see cref="ConfusedEffect"/> on the scalded
        /// attacker — steam in the face.</summary>
        public int ConfuseTurns = 2;

        public ScaldingVeilEffect(int duration = 8, int scald = 3, int confuseTurns = 2)
        {
            Duration = duration;
            Scald = scald;
            ConfuseTurns = confuseTurns;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is wreathed in scalding steam!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s veil of steam thins away.");
        }

        public override void OnTakeDamage(Entity target, GameEvent e)
        {
            if (target == null || e == null) return;

            var attacker = e.GetParameter<Entity>("Source");
            // No source (environmental damage, poison ticks) means there
            // is nothing to scald. Self-damage must not loop.
            if (attacker == null || attacker == target) return;
            if (attacker.GetStatValue("Hitpoints", 0) <= 0) return;

            var dmg = new Damage(Scald);
            dmg.AddAttribute("Fire");
            dmg.AddAttribute("Heat");
            CombatSystem.ApplyDamage(attacker, dmg, target, null);

            if (attacker.GetStatValue("Hitpoints", 0) > 0 && ConfuseTurns > 0)
                attacker.ApplyEffect(new ConfusedEffect(ConfuseTurns), target, null);

            MessageLog.Add("Steam sears " + attacker.GetDisplayName() + "!");
        }

        public override bool OnStack(Effect incoming)
        {
            // Refresh rather than stack: two veils are not hotter, they
            // just last longer.
            if (incoming is ScaldingVeilEffect veil)
            {
                if (veil.Duration > Duration) Duration = veil.Duration;
                if (veil.Scald > Scald) Scald = veil.Scald;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&W";
    }
}
