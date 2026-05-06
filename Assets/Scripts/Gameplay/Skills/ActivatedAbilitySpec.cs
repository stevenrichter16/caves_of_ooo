using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// WSP3.5 — Description record returned from
    /// <see cref="BaseSkillPart.DeclareActivatedAbility"/>. Tells
    /// <see cref="SkillsPart"/> how to register the skill's command
    /// with the actor's <c>ActivatedAbilitiesPart</c>. Mirrors Qud's
    /// <c>AddMyActivatedAbility(name, command, abilityClass, cooldown, ...)</c>
    /// argument shape.
    ///
    /// <para>Subclasses of <see cref="BaseSkillPart"/> that represent
    /// active abilities override <see cref="BaseSkillPart.DeclareActivatedAbility"/>
    /// to return one of these. Passive skills return null (default) and
    /// no ability is registered. The skill's <see cref="BaseSkillPart.OnCommand"/>
    /// override receives invocations once the player triggers the
    /// matching command.</para>
    /// </summary>
    public sealed class ActivatedAbilitySpec
    {
        /// <summary>Human-readable name shown on the hotbar / abilities panel.</summary>
        public string DisplayName;

        /// <summary>Command-event ID — e.g. "CommandConk". Routed back
        /// to the declaring skill via
        /// <see cref="SkillsPart.TryRouteSkillCommand"/>.</summary>
        public string Command;

        /// <summary>Category for hotbar grouping. "Skills" by convention.</summary>
        public string Class = "Skills";

        /// <summary>Targeting mode — usually
        /// <see cref="AbilityTargetingMode.AdjacentCell"/> for melee
        /// attacks; <see cref="AbilityTargetingMode.SelfCentered"/> for
        /// self-buffs (Berserk, Stances).</summary>
        public AbilityTargetingMode TargetingMode = AbilityTargetingMode.AdjacentCell;

        /// <summary>Maximum input-targeting range for this ability.
        /// 1 = adjacent only.</summary>
        public int Range = 1;

        /// <summary>Cooldown turns after activation. Set on the
        /// underlying <see cref="ActivatedAbility.MaxCooldown"/> via
        /// <see cref="SkillsPart.TryRouteSkillCommand"/> when the
        /// command fires successfully.</summary>
        public int Cooldown;
    }
}
