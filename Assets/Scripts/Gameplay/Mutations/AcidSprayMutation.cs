namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Acid Spray.
    /// Corrosive projectile that coats a single target in AcidicEffect. On
    /// organic targets, AcidicEffect.OnTurnStart deals flat damage and
    /// degrades MaterialPart.Combustibility every turn. The acid_plus_organic
    /// reaction accelerates fuel consumption on acidic burning organics.
    /// </summary>
    public class AcidSprayMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandAcidSpray";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Poison;
        protected override int CooldownTurns => 10;
        protected override int AbilityRange => 4;
        protected override string DamageDice => "1d4";
        protected override string AbilityClass => "Grimoire Spells";
        protected override string ImpactVerb => "douses";

        public override string Name => "Acid Spray";
        public override string MutationType => "Mental";
        public override string DisplayName => "Acid Spray";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // AcidicEffect does the heavy lifting: OnTurnStart degrades
            // organic combustibility and deals flat damage; OnTurnEnd decays
            // the corrosion. Reactions (acid_plus_organic) layer in the
            // fuel-consumption acceleration on burning acidic organics.
            target.ApplyEffect(new AcidicEffect(corrosion: 0.8f), ParentEntity, zone);
        }
    }
}
