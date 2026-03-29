namespace CavesOfOoo.Core
{
    public class PoisonSpitMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandPoisonSpit";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Poison;
        protected override int CooldownTurns => 10;
        protected override int AbilityRange => 5;
        protected override string DamageDice => "1d3";
        protected override string AbilityClass => "Physical Mutations";
        protected override string ImpactVerb => "spits venom at";

        public override string Name => "PoisonSpit";
        public override string MutationType => "Physical";
        public override string DisplayName => "Poison Spit";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            target.ApplyEffect(new PoisonedEffect(duration: 5, damageDice: "1d2", rng: rng), ParentEntity, zone);
        }
    }
}
