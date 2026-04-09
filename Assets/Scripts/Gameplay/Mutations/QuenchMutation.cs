namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Quench.
    /// Water bolt that applies WetEffect and cools the target.
    /// Extinguishes fires (via cooling below FlameTemperature) and
    /// prevents ignition (WetEffect suppresses when Moisture > 0.35).
    /// </summary>
    public class QuenchMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandQuench";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Water;
        protected override int CooldownTurns => 6;
        protected override int AbilityRange => 5;
        protected override string DamageDice => "1d3";
        protected override string AbilityClass => "Grimoire Spells";
        protected override string ImpactVerb => "drenches";

        public override string Name => "Quench";
        public override string MutationType => "Mental";
        public override string DisplayName => "Quench";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // Apply WetEffect — suppresses ignition when Moisture > 0.35
            target.ApplyEffect(new WetEffect(moisture: 0.8f), ParentEntity, zone);

            // Cool the target: negative Joules in direct mode subtracts temperature
            // This can drop temperature below FlameTemperature, causing ThermalPart
            // to extinguish BurningEffect on its next EndTurn
            var coolEvent = GameEvent.New("ApplyHeat");
            coolEvent.SetParameter("Joules", (object)(-150f));
            coolEvent.SetParameter("Radiant", (object)false);
            coolEvent.SetParameter("Source", (object)ParentEntity);
            coolEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(coolEvent);
            coolEvent.Release();
        }
    }
}
