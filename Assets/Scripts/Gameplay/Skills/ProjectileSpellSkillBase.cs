using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Shared base for straight-line projectile spells — the skill-side
    /// port of <c>DirectionalProjectileMutationBase</c>
    /// (Docs/MUTATIONS-TO-SKILLS-MIGRATION.md M1; per-power details in
    /// Docs/MUTATIONS-PORT-DOSSIERS.md).
    ///
    /// <para><b>What changed in the port, deliberately:</b></para>
    /// <list type="bullet">
    /// <item><b>Targeting uses <see cref="SkillLine.Collect"/>,</b> not
    /// <c>LineTargeting.TraceFirstImpact</c> — so bolts can hit breakable
    /// Walls, pools, bushes and campfires that the mutation path skipped
    /// via its Wall/Terrain tag filter (audit F11). Single-target
    /// semantics are preserved: the FIRST collected entity is the target
    /// (creature-priority within its cell), exactly like Ember Spit.</item>
    /// <item><b>A refused cast is free</b> (no direction, no zone, empty
    /// trace): OnCommand returns false, so no cooldown and no turn. A
    /// FIRED bolt that hits nothing ("dissipates harmlessly") still
    /// consumes the cast — the projectile flew.</item>
    /// <item><b>The damage diag kind is <c>SpellDamage</c></b> (was
    /// <c>MutationDamage</c>); payload shape unchanged except
    /// <c>powerClass</c> replaces <c>mutationClass</c>.</item>
    /// </list>
    /// </summary>
    public abstract class ProjectileSpellSkillBase : BaseSkillPart
    {
        protected abstract string CommandName { get; }
        protected abstract AsciiFxTheme FxTheme { get; }
        protected abstract int CooldownTurns { get; }
        protected abstract int AbilityRange { get; }
        protected abstract string DamageDice { get; }
        protected abstract string ImpactVerb { get; }

        /// <summary>"Heat" / "Cold" / "Electric" / "Acid" — feeds the
        /// resistance pipeline and the element-gated skill hooks. Empty =
        /// untyped magic damage.</summary>
        protected virtual string ElementAttribute => "";

        /// <summary>Ability-manager grouping. The tree name, so the
        /// manager's class column groups meaningfully.</summary>
        protected virtual string AbilityClass => "Skills";

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = DisplayName,
                Command = CommandName,
                Class = AbilityClass,
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = AbilityRange,
                Cooldown = CooldownTurns,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            // Nearest occupied cell wins; creature-first within it. Solid
            // cells stop the walk AFTER being collected, so a tree is hit
            // and THEN stops the bolt.
            var targets = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, AbilityRange,
                out bool blockedByWall);
            Entity target = targets.Count > 0 ? targets[0] : null;

            // FX path: source-exclusive cells along the line, ending at
            // the impact cell (the target's cell, or the blocking solid,
            // or max range).
            var path = BuildFxPath(ctx.Zone, actorPos.x, actorPos.y, dx, dy, target);
            if (path.Count == 0) { EmitSkillRejectedDiag(ctx, "no_path"); return false; }

            AsciiFxBus.EmitProjectile(ctx.Zone, path, FxTheme, trail: true,
                blocksTurnAdvance: true);
            ctx.BlocksTurnAdvance = true;

            if (target != null)
            {
                int damage = DiceRoller.Roll(DamageDice, ctx.Rng);
                if (damage > 0)
                {
                    int actualDamage = SpellDamageHelpers.ApplySpellDamage(
                        target, damage, ElementAttribute, actor, ctx.Zone);

                    MessageLog.Add(actor.GetDisplayName() + " " + ImpactVerb + " " +
                        target.GetDisplayName() + " for " + actualDamage + " damage!");

                    if (Diag.IsChannelEnabled("damage"))
                    {
                        Diag.Record(
                            category: "damage",
                            kind: "SpellDamage",
                            actor: actor,
                            target: target,
                            payload: new
                            {
                                powerClass = GetType().Name,
                                diceRolled = DamageDice,
                                rawRoll = damage,
                                elementAttribute = ElementAttribute,
                                actualDamage = actualDamage
                            });
                    }
                }

                // Alive targets get the on-hit while still alive; a
                // breakable object that survived the hit is equally
                // valid — what the effect MEANS for its material is
                // ObjectStatusMatrix's decision, not this gate's.
                var structural = target.GetPart<DestructiblePart>();
                bool objectStillStanding = !target.HasTag("Creature")
                    && structural != null && !structural.IsDestroyed;
                if (target.GetStatValue("Hitpoints", 0) > 0 || objectStillStanding)
                    ApplyOnHitEffect(target, ctx.Zone, ctx.Rng);
            }
            else if (blockedByWall)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s " + DisplayName +
                    " splashes against an obstacle!");
            }
            else
            {
                MessageLog.Add(actor.GetDisplayName() + "'s " + DisplayName +
                    " dissipates harmlessly.");
            }

            // A fired bolt is a real cast whether or not it found a body —
            // the dispatcher applies the cooldown off this true.
            return true;
        }

        /// <summary>Element-specific on-hit (ignite, chill, charge…).
        /// Only called while the target is still standing.</summary>
        protected virtual void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng) { }

        private System.Collections.Generic.List<Point> BuildFxPath(
            Zone zone, int startX, int startY, int dx, int dy, Entity target)
        {
            var path = new System.Collections.Generic.List<Point>();
            int x = startX, y = startY;
            for (int step = 0; step < AbilityRange; step++)
            {
                x += dx; y += dy;
                if (!zone.InBounds(x, y)) break;
                var cell = zone.GetCell(x, y);
                if (cell == null) break;
                path.Add(new Point(x, y));
                // Stop at the impact: the target's cell, or a solid cell
                // (which SkillLine already collected before stopping).
                if (target != null && cell.Objects.Contains(target)) break;
                if (cell.IsSolid()) break;
            }
            return path;
        }
    }
}
