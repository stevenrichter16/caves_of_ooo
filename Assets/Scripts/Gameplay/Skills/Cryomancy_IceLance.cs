using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Ice Lance — a lance of bitter cold. Skill-side port of
    /// <c>IceLanceMutation</c> (Docs/MUTATIONS-PORT-DOSSIERS.md);
    /// grimoire-taught via IceLanceGrimoire and SP-buyable in Cryomancy.
    ///
    /// <para>Numbers preserved verbatim: 1d6 Cold, range 6, cooldown 8,
    /// −300J on-hit chill. The chill deliberately does NOT pre-apply
    /// FrozenEffect: cooling that crosses FreezeTemperature makes
    /// <c>ThermalPart.TryFreeze</c> apply FrozenEffect(1.0) itself, which
    /// is the brittle-shatter setup on frozen metal.</para>
    /// </summary>
    public class Cryomancy_IceLance : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Cryomancy_IceLance);

        public const int COOLDOWN = 8;
        public const int RANGE = 6;
        public const string DAMAGE_DICE = "1d6";
        public const float CHILL_JOULES = -300f;

        protected override string CommandName => "CommandIceLance";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Ice;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => DAMAGE_DICE;
        protected override string ImpactVerb => "impales";
        protected override string ElementAttribute => "Cold";
        protected override string AbilityClass => "Cryomancy";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            var heatEvent = GameEvent.New("ApplyHeat");
            heatEvent.SetParameter("Joules", (object)CHILL_JOULES);
            heatEvent.SetParameter("Radiant", (object)false);
            heatEvent.SetParameter("Source", (object)ParentEntity);
            heatEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(heatEvent);
            heatEvent.Release();
        }
    }
}
