namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Arc Bolt.
    /// Lightning-focused projectile that applies ElectrifiedEffect to a single
    /// target. ElectrifiedEffect.OnApply handles wet amplification (doubled
    /// charge and extra duration on targets with WetEffect.Moisture > 0.2) and
    /// stuns creatures for one turn. The chain propagation to conductive
    /// neighbors fires on the struck target's next EndTurn via
    /// ElectrifiedEffect.OnTurnEnd → MaterialPart.HandleTryChainElectricity.
    /// </summary>
    public class ArcBoltMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandArcBolt";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Lightning;
        protected override int CooldownTurns => 7;
        protected override int AbilityRange => 5;
        protected override string DamageDice => "1d8";
        protected override string AbilityClass => "Grimoire Spells";
        protected override string ImpactVerb => "jolts";

        public override string Name => "Arc Bolt";
        public override string MutationType => "Mental";
        public override string DisplayName => "Arc Bolt";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // ElectrifiedEffect does the work: OnApply amplifies charge/duration
            // on wet targets and stuns creatures for one turn. The conductor
            // chain propagation resolves on the next EndTurn via OnTurnEnd.
            target.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), ParentEntity, zone);
        }
    }
}
