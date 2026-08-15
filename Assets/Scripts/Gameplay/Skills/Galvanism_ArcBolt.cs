using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Arc Bolt — a crackling bolt of electricity. Skill-side port of
    /// <c>ArcBoltMutation</c> (Docs/MUTATIONS-PORT-DOSSIERS.md);
    /// grimoire-taught via ArcBoltGrimoire and SP-buyable in Galvanism.
    ///
    /// <para>Numbers preserved verbatim: 1d8 Electric, range 5,
    /// cooldown 7, full Electrified charge on-hit routed through
    /// <see cref="ObjectStatusMatrix"/> so a wooden fence refuses the
    /// charge and a brine pool takes it.</para>
    /// </summary>
    public class Galvanism_ArcBolt : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Galvanism_ArcBolt);

        public const int COOLDOWN = 7;
        public const int RANGE = 5;
        public const string DAMAGE_DICE = "1d8";

        protected override string CommandName => "CommandArcBolt";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Lightning;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => DAMAGE_DICE;
        protected override string ImpactVerb => "jolts";
        protected override string ElementAttribute => "Electric";
        protected override string AbilityClass => "Galvanism";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            ObjectStatusMatrix.TryApply(new ElectrifiedEffect(charge: 1.0f),
                target, ParentEntity, zone);
        }
    }
}
