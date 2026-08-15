using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Acid Spray — a gout of corrosive spray. Skill-side port of
    /// <c>AcidSprayMutation</c> (Docs/MUTATIONS-PORT-DOSSIERS.md);
    /// grimoire-taught via AcidSprayGrimoire and SP-buyable in Corrosion.
    ///
    /// <para>Numbers preserved verbatim: 1d4 Acid, range 4, cooldown 10,
    /// on-hit Acidic(0.8) via <see cref="ObjectStatusMatrix"/> — acid
    /// etches anything, so the matrix always lets it land.</para>
    /// </summary>
    public class Corrosion_AcidSpray : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Corrosion_AcidSpray);

        public const int COOLDOWN = 10;
        public const int RANGE = 4;
        public const string DAMAGE_DICE = "1d4";

        protected override string CommandName => "CommandAcidSpray";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Poison;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => DAMAGE_DICE;
        protected override string ImpactVerb => "douses";
        protected override string ElementAttribute => "Acid";
        protected override string AbilityClass => "Corrosion";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            ObjectStatusMatrix.TryApply(new AcidicEffect(corrosion: 0.8f),
                target, ParentEntity, zone);
        }
    }
}
