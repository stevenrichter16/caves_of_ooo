namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Ice Lance.
    /// Cold-focused projectile that fires a strong cooling pulse at a single
    /// target. The -300J heat event crosses the target's FreezeTemperature,
    /// ThermalPart.TryFreeze applies FrozenEffect(1.0), and FrozenEffect.OnApply
    /// runs its brittle-shatter check against the *post-cooling* temperature —
    /// brittle metal entities shatter instead of merely freezing.
    /// </summary>
    public class IceLanceMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandIceLance";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Ice;
        protected override int CooldownTurns => 8;
        protected override int AbilityRange => 6;
        protected override string DamageDice => "1d6";
        protected override string AbilityClass => "Grimoire Spells";
        protected override string ImpactVerb => "impales";

        public override string Name => "Ice Lance";
        public override string MutationType => "Mental";
        public override string DisplayName => "Ice Lance";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // Fire cooling heat first — do NOT pre-apply FrozenEffect. Letting
            // ThermalPart.HandleApplyHeat cross FreezeTemperature means TryFreeze
            // will apply FrozenEffect(1.0) *after* the cool, so OnApply's
            // brittle-shatter check runs on the now-cold temperature and brittle
            // metal entities actually shatter.
            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", (object)(-300f));
            heatEvent.SetParameter("Radiant", (object)false);
            heatEvent.SetParameter("Source", (object)ParentEntity);
            heatEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(heatEvent);
            heatEvent.Release();
        }
    }
}
