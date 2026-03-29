namespace CavesOfOoo.Core
{
    public class IceShardMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandIceShard";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Ice;
        protected override int CooldownTurns => 8;
        protected override int AbilityRange => 6;
        protected override string DamageDice => "2d3";
        protected override string AbilityClass => "Physical Mutations";
        protected override string ImpactVerb => "impales";

        public override string Name => "IceShard";
        public override string MutationType => "Physical";
        public override string DisplayName => "Ice Shard";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            target.ApplyEffect(new StunnedEffect(duration: 1), ParentEntity, zone);
        }
    }
}
