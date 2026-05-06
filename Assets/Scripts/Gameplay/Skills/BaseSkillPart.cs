using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Abstract base for the per-skill runtime part. Concrete skills
    /// (e.g. <c>AcrobaticsDodgePower</c>, future <c>LongBladesExpertise</c>)
    /// extend this class. One instance is added to an entity's Parts list
    /// when the entity learns the skill; removed when the skill is
    /// unlearned. Mirrors Qud's <c>BaseSkill</c>
    /// (XRL.World.Parts.Skill/BaseSkill.cs:9-153) — same lifecycle
    /// (<see cref="AddSkill"/> / <see cref="RemoveSkill"/> hooks),
    /// same pattern of one-class-per-skill, same <c>DisplayName</c>
    /// abstract.
    ///
    /// <para><b>Lifecycle:</b>
    /// <list type="number">
    ///   <item><see cref="SkillsPart.AddSkill(BaseSkillPart, Entity, string)"/>
    ///         attaches this part to the actor's Parts list (via
    ///         <c>Entity.AddPart</c>), then calls <see cref="AddSkill"/>.</item>
    ///   <item><see cref="AddSkill"/> applies the skill's effect (passive
    ///         stat shift, registering an activated ability, etc.) — return
    ///         <c>false</c> to abort and let the manager remove the part.</item>
    ///   <item>While owned, the part receives normal Part events through
    ///         <see cref="Part.HandleEvent"/>.</item>
    ///   <item><see cref="SkillsPart.RemoveSkill"/> calls
    ///         <see cref="RemoveSkill"/>, then detaches via <c>Entity.RemovePart</c>.</item>
    /// </list></para>
    ///
    /// <para><b>Naming convention</b> (mirrors Qud + CoO mutation precedent):
    /// concrete subclasses are named <c>&lt;Tree&gt;Skill</c> for the
    /// tree-root marker (e.g. <c>AcrobaticsSkill</c>) and
    /// <c>&lt;Tree&gt;&lt;Power&gt;Power</c> for individual powers (e.g.
    /// <c>AcrobaticsDodgePower</c>). The <see cref="SkillData.Class"/> /
    /// <see cref="PowerData.Class"/> JSON fields name the runtime class
    /// directly so reflection lookup in <see cref="SkillsPart"/> can resolve
    /// content → C# type without a separate registration step.</para>
    /// </summary>
    public abstract class BaseSkillPart : Part
    {
        /// <summary>
        /// Per-skill stat-shift tracker (lazy-init). Concrete passive
        /// skills use this in their <c>AddSkill</c> hook to apply
        /// stat bonuses, and call <c>RemoveStatShifts</c> in
        /// <c>RemoveSkill</c> to roll them back. Mirrors Qud's
        /// <c>BaseSkill.StatShifter</c> property
        /// (used by Acrobatics_Dodge.cs:11 — <c>base.StatShifter.SetStatShift("DV", 2)</c>).
        ///
        /// <para><b>Lazy-init</b> because <see cref="Part.ParentEntity"/>
        /// isn't set until the part is attached via <c>Entity.AddPart</c>.
        /// First access creates the StatShifter bound to the current
        /// ParentEntity. Re-accessing after detach/reattach to a
        /// different entity would return the stale shifter — but
        /// BaseSkillPart instances are owned by their entity for life
        /// (RemoveSkill detaches + discards), so this isn't a real
        /// hazard.</para>
        /// </summary>
        private StatShifter _statShifter;
        public StatShifter StatShifter => _statShifter ??= new StatShifter(ParentEntity);

        /// <summary>
        /// Human-readable name for UI rendering. Defaults to the
        /// registry-supplied <see cref="SkillData.Name"/> /
        /// <see cref="PowerData.Name"/> when looked up via Class; if the
        /// registry doesn't know about this skill (e.g. test-only stub),
        /// falls back to <c>GetType().Name</c>. Concrete subclasses can
        /// override for hardcoded display names.
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                string className = GetType().Name;
                if (SkillRegistry.TryGetSkillByClass(className, out var skill)
                    && !string.IsNullOrWhiteSpace(skill.Name))
                    return skill.Name;
                if (SkillRegistry.TryGetPowerByClass(className, out var power)
                    && !string.IsNullOrWhiteSpace(power.Name))
                    return power.Name;
                return className;
            }
        }

        /// <summary>
        /// Lifecycle hook fired when the actor acquires this skill.
        /// Override to apply passive bonuses (stat shifts), register
        /// activated abilities, hook combat events, etc. Return
        /// <c>true</c> to confirm the skill is active; <c>false</c> to
        /// signal a setup failure (the manager will roll back the
        /// attachment). Default returns <c>true</c> — passive marker
        /// skills don't need any setup.
        /// </summary>
        public virtual bool AddSkill(Entity entity)
        {
            return true;
        }

        /// <summary>
        /// Lifecycle hook fired when the actor loses this skill.
        /// Override to undo whatever <see cref="AddSkill"/> applied
        /// (remove stat shifts, deregister abilities, unhook events).
        /// Return <c>true</c> to confirm clean teardown; <c>false</c> is
        /// reserved for "skill couldn't be removed" but currently
        /// unused — the manager always proceeds with detachment.
        /// </summary>
        public virtual bool RemoveSkill(Entity entity)
        {
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        // WSP3 — Combat event hooks. Each override is the per-skill
        // implementation of a Qud event. Default no-op; override only
        // the events the skill cares about. Dispatched by
        // <see cref="SkillEventDispatcher"/> from CombatSystem call sites.
        //
        // Authoring a new skill: subclass BaseSkillPart, override one or
        // more of these virtuals. Read fields from the SkillEventContext;
        // mutate state via the same paths the rest of CoO uses
        // (Entity.ApplyEffect, CombatSystem.ApplyDamage, etc.).
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired AFTER a successful melee hit + damage application,
        /// inside the survivor block (<c>hpAfter &gt; 0</c>). Most
        /// per-skill on-hit procs (Cudgel_Bludgeon, LongBlades_Lacerate,
        /// ShortBlades_Jab, ShortBlades_Bloodletter, Cudgel_Hammer,
        /// Cudgel_ShatteringBlows, ShortBlades_Hobble) handle here.
        /// Mirrors Qud's <c>"AttackerAfterAttack"</c> event.
        /// </summary>
        /// <param name="ctx">Event context — Attacker, Defender, Damage,
        /// ActualDamage, Zone, Rng, Properties all populated.</param>
        public virtual void OnAttackerAfterAttack(SkillEventContext ctx) { }

        /// <summary>
        /// Fired ONCE PER MISSED MELEE SWING, after the message is
        /// logged. Cudgel_Backswing handles here for re-attack-on-miss.
        /// Mirrors Qud's <c>"AttackerMeleeMiss"</c> event.
        /// </summary>
        public virtual void OnAttackerMeleeMiss(SkillEventContext ctx) { }

        /// <summary>
        /// Fired on the DEFENDER's skill list when an incoming attack
        /// missed them. ShortBlades_Rejoinder handles here for free
        /// counter-attacks. Mirrors Qud's <c>"DefenderAfterAttackMissed"</c>.
        /// </summary>
        public virtual void OnDefenderAfterAttackMissed(SkillEventContext ctx) { }

        /// <summary>
        /// Fired AFTER a critical hit lands. Tree-root skills typically
        /// override this to apply their per-class crit effect (Cudgel:
        /// stun, Axe: cleave, LongBlades: extra damage, ShortBlades: bleed).
        /// Mirrors Qud's <c>WeaponMadeCriticalHit</c> virtual on tree-root
        /// skill classes.
        /// </summary>
        public virtual void OnWeaponMadeCriticalHit(SkillEventContext ctx) { }

        /// <summary>
        /// Returns this skill's contribution to the attacker's to-hit
        /// modifier. Summed across all owned skills by
        /// <see cref="SkillEventDispatcher.GetSkillHitModifier"/>.
        /// Cudgel_Expertise / Axe_Expertise / ShortBlades_Expertise return
        /// +2/+2/+1 here when the wielded weapon matches their class.
        /// Default returns 0. Mirrors Qud's <c>GetToHitModifierEvent</c>.
        /// </summary>
        public virtual int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            return 0;
        }
    }
}
