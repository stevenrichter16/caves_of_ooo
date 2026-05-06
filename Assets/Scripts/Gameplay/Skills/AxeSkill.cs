using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Axe skill tree (battleaxes, hatchets,
    /// chopping weapons). Owning this part means the actor "knows" the
    /// Axe weapon family.
    ///
    /// <para><b>Crit behavior (WSP3.3 — virtual override):</b> on every
    /// critical hit landed with an Axe-attribute weapon, force-cleaves
    /// to one adjacent Creature for half the original damage. Verbatim
    /// Qud port — Qud's <c>XRL.World.Parts.Skill.Axe.WeaponMadeCriticalHit</c>
    /// calls <c>Axe_Cleave.PerformCleave</c> at 100% chance on crit.</para>
    /// </summary>
    public class AxeSkill : BaseSkillPart
    {
        public override string Name => nameof(AxeSkill);

        public override void OnWeaponMadeCriticalHit(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Axe")) return;
            if (ctx.ActualDamage <= 0) return;
            // Force-cleave (no chance roll) — that's the tree-root's
            // crit-only bonus on top of any owned Axe_Cleave power.
            SkillCombatHelpers.ExecuteCleave(
                ctx.ActualDamage, ctx.Defender, ctx.Attacker, ctx.Zone);
        }
    }
}
