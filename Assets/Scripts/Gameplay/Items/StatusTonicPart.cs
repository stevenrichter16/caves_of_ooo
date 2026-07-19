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
            // Dispatch table extracted to TonicEffectFactory (M1.2) so brew
            // items share the same canonical name→Effect mapping. Behavior
            // is unchanged — the factory is the verbatim switch that lived
            // here, defaults included.
            return TonicEffectFactory.Create(
                EffectName, EffectDuration, EffectDamageDice, EffectMagnitude, source);
        }
    }
}
