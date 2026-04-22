namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Kindle.
    /// Heat-focused projectile that applies direct heat to a target,
    /// triggering the full ThermalPart ignition pipeline.
    /// Can ignite any entity with ThermalPart + Combustibility > 0.
    /// </summary>
    public class KindleMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandKindle";

        protected override string CommandName => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Fire;
        protected override int CooldownTurns => 6;
        protected override int AbilityRange => 5;
        protected override string DamageDice => "1d4";
        protected override string AbilityClass => "Grimoire Spells";
        protected override string ImpactVerb => "sears";

        public override string Name => "Kindle";
        public override string MutationType => "Mental";
        public override string DisplayName => "Kindle";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // Apply 200J direct heat — triggers ThermalPart ignition pipeline
            // (FlameTemperature check → TryIgnite → MaterialPart veto → WetEffect suppression → BurningEffect)
            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", (object)200f);
            heatEvent.SetParameter("Radiant", (object)false);
            heatEvent.SetParameter("Source", (object)ParentEntity);
            heatEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(heatEvent);
            heatEvent.Release();

            // Emit radiant heat to adjacent entities for environmental interaction
            MaterialSimSystem.EmitHeatToAdjacent(target, zone, 60f);
        }
    }
}
