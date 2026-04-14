namespace CavesOfOoo.Core
{
    /// <summary>
    /// Applies a configured status effect when a tonic is used or shattered on a target.
    /// Works alongside TonicPart via the ApplyTonic item event.
    /// </summary>
    public class StatusTonicPart : Part
    {
        public override string Name => "StatusTonic";

        public string EffectName = "";
        public int EffectDuration = 0;
        public string EffectDamageDice = "";
        public float EffectMagnitude = 0f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "ApplyTonic")
                return true;

            var target = e.GetParameter<Entity>("Actor");
            if (target == null || string.IsNullOrWhiteSpace(EffectName))
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Entity source = e.GetParameter<Entity>("Source");

            Effect effect = CreateEffect(source);
            if (effect != null)
                target.ApplyEffect(effect, source, zone);

            return true;
        }

        private Effect CreateEffect(Entity source)
        {
            string effectKey = EffectName.Trim().ToLowerInvariant();
            switch (effectKey)
            {
                case "poison":
                case "poisoned":
                case "poisonedeffect":
                    return new PoisonedEffect(
                        duration: EffectDuration > 0 ? EffectDuration : 5,
                        damageDice: string.IsNullOrWhiteSpace(EffectDamageDice) ? "1d3" : EffectDamageDice);

                case "fire":
                case "burn":
                case "burning":
                case "burningeffect":
                    return new BurningEffect(
                        intensity: EffectMagnitude > 0f ? EffectMagnitude : 1.0f,
                        source: source);

                case "wet":
                case "water":
                case "weteffect":
                    return new WetEffect(
                        moisture: EffectMagnitude > 0f ? EffectMagnitude : 1.0f);

                case "acid":
                case "acidic":
                case "acidiceffect":
                    return new AcidicEffect(
                        corrosion: EffectMagnitude > 0f ? EffectMagnitude : 1.0f);

                case "shock":
                case "lightning":
                case "electric":
                case "electrified":
                case "electrifiedeffect":
                    return new ElectrifiedEffect(
                        charge: EffectMagnitude > 0f ? EffectMagnitude : 1.0f);

                case "ice":
                case "frost":
                case "frozen":
                case "frozeneffect":
                    return new FrozenEffect(
                        cold: EffectMagnitude > 0f ? EffectMagnitude : 1.0f);
            }

            return null;
        }
    }
}
