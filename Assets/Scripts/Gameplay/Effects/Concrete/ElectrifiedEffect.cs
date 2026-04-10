namespace CavesOfOoo.Core
{
    /// <summary>
    /// Electrified: short-duration charge that stuns creatures on apply,
    /// chains along conductive materials on each turn start, and is
    /// amplified by moisture. Drives lightning_plus_conductor reactions.
    /// </summary>
    public class ElectrifiedEffect : Effect
    {
        public override string DisplayName => "electrified";

        /// <summary>Joules-equivalent charge. Higher = longer chain and stronger zap.</summary>
        public float Charge;

        public ElectrifiedEffect(float charge = 1.0f)
        {
            Charge = charge < 0f ? 0f : charge;
            Duration = 2;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is electrified!");

            // Wet targets receive amplified charge (and the effect lingers longer).
            var wet = target.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > 0.2f)
            {
                Charge *= 2f;
                Duration += 1;
            }

            // Stun creatures briefly on contact. Non-creatures (props) ignore the stun
            // but still carry the Electrified state for propagation.
            if (target.HasTag("Creature"))
                target.ApplyEffect(new StunnedEffect(duration: 1), null, null);
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer electrified.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            // Propagate along conductors during the sim tick so chains resolve
            // during the turn, not at application time.
            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Charge", (object)Charge);
            chain.SetParameter("Zone", context?.GetParameter<Zone>("Zone"));
            chain.SetParameter("Source", (object)target);
            target.FireEvent(chain);
            chain.Release();
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is ElectrifiedEffect zap)
            {
                Charge = System.Math.Max(Charge, zap.Charge);
                Duration = System.Math.Max(Duration, zap.Duration);
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&Y";
    }
}
