using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class active ability: swing at an adjacent creature with a
    /// hook strike. The swing fires PerformSingleAttack with the
    /// <c>(Hook)</c> marker, AND applies <see cref="HookedEffect"/> to
    /// the target regardless of swing outcome. Hooked targets are
    /// dragged 1 cell closer per turn-end (with Strength save vs
    /// <see cref="HOOK_SAVE_TARGET"/>) for up to <see cref="HOOK_DURATION"/>
    /// turns.
    ///
    /// <para>Per Qud's <c>Axe_HookAndDrag</c> mechanic — Qud's version
    /// also tracks per-attacker hook state (LeftCell, EnteredCellEvent
    /// drag-on-attacker-move). CoO simplifies to "drag fires on the
    /// HOOKED ENTITY's turn-end" rather than the attacker's move —
    /// captures the gameplay feel without needing global movement
    /// tracking. Documented per CLAUDE.md §4.2 as Match (mechanic
    /// family) + Divergent (drag-trigger differs).</para>
    ///
    /// <para><b>Mechanic (CoO):</b> requires an Axe-attribute weapon
    /// equipped (Battleaxe / Hatchet) and an adjacent Creature. The
    /// swing happens via <see cref="CombatSystem.PerformSingleAttack"/>
    /// (so all standard combat hooks run — crit, on-hit procs, damage
    /// resistance, etc.). After the swing,
    /// <see cref="HookedEffect"/> is applied to the target with this
    /// actor as the Hooker. Cooldown <see cref="COOLDOWN"/> turns.</para>
    /// </summary>
    public class Axe_HookAndDrag : BaseSkillPart
    {
        public override string Name => nameof(Axe_HookAndDrag);

        public const int COOLDOWN = 50;
        public const int HOOK_DURATION = 9;
        public const int HOOK_SAVE_TARGET = 20;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Hook and Drag",
                Command = "CommandHookAndDrag",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;

            // Require an Axe-class weapon equipped.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to hook.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return false;
            }

            // Need Zone for adjacency lookup + the drag mechanic later.
            if (ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "no_zone");
                return false;
            }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return false;
            }

            // Find adjacent Creature (mirrors Cudgel_Slam's pattern).
            var target = MultiCellAbilityQueries.FirstAdjacentCreature(ctx.Zone, actor, out _);

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to hook.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            // Swing + apply Hooked. The swing always happens (so on-hit
            // procs can fire); the Hooked effect always applies (so a
            // missed swing still hooks the target — that's Qud's
            // semantic, mirrors Conk's "stun on swing regardless").
            CombatSystem.PerformSingleAttack(
                attacker: actor, defender: target,
                weapon: weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(Hook)");

            target.ApplyEffect(
                new HookedEffect(HOOK_DURATION, actor, HOOK_SAVE_TARGET, ctx.Rng),
                actor, ctx.Zone);
        
            return true;
        }
    }
}
