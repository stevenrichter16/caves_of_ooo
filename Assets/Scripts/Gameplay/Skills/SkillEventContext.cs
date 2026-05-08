using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// WSP3 — Param object carried into per-skill event handlers
    /// (<see cref="BaseSkillPart.OnAttackerAfterAttack"/>,
    /// <see cref="BaseSkillPart.OnAttackerMeleeMiss"/>, etc.). Mirrors
    /// the shape of Qud's combat event parameters
    /// (XRL.World.Parts.Skill/Cudgel_Bludgeon.cs:21-24, etc.) — same
    /// fields the skill handlers actually need.
    ///
    /// <para>Intentionally a class (not a struct) so handlers can mutate
    /// the context — most importantly <see cref="Damage"/> and the
    /// <see cref="Properties"/> string can be appended to as the
    /// dispatcher chains through skills.</para>
    ///
    /// <para>All fields are nullable / optional except <see cref="Attacker"/>
    /// which is always set by callers. Handlers should null-check before
    /// dereferencing — mirrors Qud's <c>GameObject.Validate(ref obj)</c>
    /// guard pattern.</para>
    /// </summary>
    public class SkillEventContext
    {
        /// <summary>The actor whose skills are being dispatched to.</summary>
        public Entity Attacker;

        /// <summary>The target of the attack (if any). Null for events
        /// like "OnEquip" that don't have a target.</summary>
        public Entity Defender;

        /// <summary>The weapon part used in this attack. Null for unarmed.</summary>
        public MeleeWeaponPart Weapon;

        /// <summary>The weapon entity (so handlers can apply effects to
        /// the weapon itself — e.g. Cudgel_Hammer wants the defender's
        /// equipped item entity, not its part).</summary>
        public Entity WeaponEntity;

        /// <summary>The damage object for this hit. Null for events
        /// fired before damage is rolled (e.g. miss).</summary>
        public Damage Damage;

        /// <summary>Post-resistance HP delta for the hit. 0 for vetoed /
        /// fully-resisted hits or events that don't deal damage.</summary>
        public int ActualDamage;

        /// <summary>The live zone — needed for adjacency lookups (cleave),
        /// re-attack dispatch (Backswing), entity-position queries.</summary>
        public Zone Zone;

        /// <summary>Deterministic RNG. Forward to skill handlers + downstream
        /// effect ctors so seeded tests stay reproducible.</summary>
        public System.Random Rng;

        /// <summary>Comma-delimited list of attack-context tags. Set by
        /// the originating attack site to flag special-attack contexts —
        /// e.g. "Charging" (the attacker was charging when this swing
        /// connected), "Conking" (attack came from Cudgel_Conk's targeted
        /// strike), "Backswinging" (this swing was triggered by
        /// Cudgel_Backswing's re-attack). Empty / null = a normal swing.
        /// Mirrors Qud's <c>E.GetStringParameter("Properties")</c>
        /// pattern (e.g. Cudgel_Bludgeon.cs:30 reads "Conking" to
        /// boost the Stun chance to 100%).</summary>
        public string Properties = "";

        /// <summary>X-axis component of the player-chosen direction for
        /// <see cref="AbilityTargetingMode.DirectionLine"/>-targeted
        /// activated abilities. -1, 0, or 1. Plumbed in by
        /// <see cref="SkillsPart.HandleEvent"/> from the GameEvent's
        /// <c>"DirectionX"</c> param (set by
        /// <c>InputHandler.ResolveAbilityCommand</c> after the
        /// AwaitingDirection state captures the player's keypress).
        ///
        /// <para>Default 0 — passive skills + AdjacentCell-targeting
        /// actives ignore this; only DirectionLine consumers
        /// (e.g. <see cref="LongBlades_Lunge"/>) read it. Mirrors how
        /// mutations like <see cref="CavesOfOoo.Core.ChainLightningMutation"/>
        /// pull <c>e.GetIntParameter("DirectionX")</c> from their own
        /// HandleEvent overrides; SkillEventContext centralizes the
        /// extraction so every direction-targeting skill doesn't have
        /// to duplicate the param lookup.</para>
        /// </summary>
        public int DirectionX = 0;

        /// <summary>Y-axis component of the player-chosen direction.
        /// See <see cref="DirectionX"/> for the full contract.</summary>
        public int DirectionY = 0;
    }
}
