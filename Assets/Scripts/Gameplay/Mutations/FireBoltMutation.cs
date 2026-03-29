namespace CavesOfOoo.Core
{
    public class FireBoltMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandFireBolt";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Fire;
        protected override int CooldownTurns => 8;
        protected override int AbilityRange => 6;
        protected override string DamageDice => "2d4";
        protected override string AbilityClass => "Physical Mutations";
        protected override string ImpactVerb => "scorches";

        public override string Name => "FireBolt";
        public override string MutationType => "Physical";
        public override string DisplayName => "Fire Bolt";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            target.ApplyEffect(new BurningEffect(duration: 3, damageDice: "1d3", rng: rng), ParentEntity, zone);
        }
    }
}
