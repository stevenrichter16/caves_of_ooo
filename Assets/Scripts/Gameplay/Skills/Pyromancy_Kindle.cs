using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Kindle — a hot bolt whose entire purpose is setting the target
    /// alight. Skill-side port of <c>KindleMutation</c>
    /// (Docs/MUTATIONS-PORT-DOSSIERS.md); grimoire-taught via
    /// KindleGrimoire and SP-buyable in the Pyromancy tree.
    ///
    /// <para>Numbers preserved verbatim: 1d4 Fire, range 5, cooldown 6,
    /// <see cref="FireDose.Ignition"/> (600J) on-hit heat plus a 60J
    /// neighbourhood pulse. What changed is the walk: bolts now hit
    /// breakable Walls, pools and bushes the mutation path skipped.</para>
    /// </summary>
    public class Pyromancy_Kindle : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Pyromancy_Kindle);

        public const int COOLDOWN = 6;
        public const int RANGE = 5;
        public const string DAMAGE_DICE = "1d4";

        protected override string CommandName => "CommandKindle";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Fire;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => DAMAGE_DICE;
        protected override string ImpactVerb => "sears";
        protected override string ElementAttribute => "Fire";
        protected override string AbilityClass => "Pyromancy";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            // Direct heat through the thermal pipeline (FlameTemperature
            // check → TryIgnite → MaterialPart veto → wet suppression →
            // BurningEffect) — the ignition is the spell's identity.
            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", (object)FireDose.Ignition);
            heatEvent.SetParameter("Radiant", (object)false);
            heatEvent.SetParameter("Source", (object)ParentEntity);
            heatEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(heatEvent);
            heatEvent.Release();

            // Neighbourhood warmth (7.5J each after the 8-way split) —
            // decorative at this magnitude, preserved from the mutation.
            MaterialSimSystem.EmitHeatToAdjacent(target, zone, 60f);
        }
    }
}
