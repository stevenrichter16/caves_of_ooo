using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Quench — a burst of conjured water. Skill-side port of
    /// <c>QuenchMutation</c> (Docs/MUTATIONS-PORT-DOSSIERS.md);
    /// grimoire-taught via QuenchGrimoire and SP-buyable in Hydromancy.
    ///
    /// <para>Numbers preserved verbatim: 1d3 untyped damage, range 5,
    /// cooldown 6, on-hit Wet(0.8) via <see cref="ObjectStatusMatrix"/>
    /// plus a −150J cooling pulse. The soak is the point — it is the
    /// setup half of the soak-then-shock grammar and it suppresses
    /// ignition on whatever it lands on.</para>
    /// </summary>
    public class Hydromancy_Quench : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Hydromancy_Quench);

        public const int COOLDOWN = 6;
        public const int RANGE = 5;
        public const string DAMAGE_DICE = "1d3";
        public const float COOL_JOULES = -150f;

        protected override string CommandName => "CommandQuench";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Water;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => DAMAGE_DICE;
        protected override string ImpactVerb => "drenches";
        protected override string AbilityClass => "Hydromancy";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            ObjectStatusMatrix.TryApply(new WetEffect(moisture: 0.8f),
                target, ParentEntity, zone);

            var coolEvent = GameEvent.New("ApplyHeat");
            coolEvent.SetParameter("Joules", (object)COOL_JOULES);
            coolEvent.SetParameter("Radiant", (object)false);
            coolEvent.SetParameter("Source", (object)ParentEntity);
            coolEvent.SetParameter("Zone", (object)zone);
            target.FireEvent(coolEvent);
            coolEvent.Release();
        }
    }
}
