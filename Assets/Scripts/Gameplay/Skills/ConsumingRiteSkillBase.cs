using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Shared spine for the consuming rites — skill-side port of
    /// <c>ConsumingRiteBase</c>, converged so that ALL eleven rites ride
    /// it (the mutation era had five hand-rolling the ink/target/spend
    /// spine, four of them subtly differently).
    ///
    /// <para><b>The two invariants, unchanged:</b> ink is never spent on
    /// a cast that finds nothing, and a rite cast cold is deliberately
    /// weak (multiplier 1.0, small base numbers — the power lives in
    /// what you fed it). Every refusal is FREE: no ink, no cooldown, no
    /// turn.</para>
    ///
    /// <para><b>What convergence added</b> (each hook exists because one
    /// of the five hand-rolled rites needed it):</para>
    /// <list type="bullet">
    /// <item><see cref="DamageAttributes"/> — StormAnvil/HangingBolt/
    /// Fulmination stack "Electric"+"Lightning"; RenderedSteam stacks
    /// "Fire"+"Heat".</item>
    /// <item><see cref="ComputeDamage"/> — HangingBolt's damage is FLAT
    /// (marks buy paralysis, not damage), RenderedSteam adds a pair
    /// bonus, Fulmination double-rounds a hybrid. Overriding the whole
    /// computation preserves each rite's exact arithmetic.</item>
    /// <item><see cref="RiteShape.Self"/> + <see cref="ValidateBeforeInk"/>
    /// — ScaldingVeil targets the caster and refuses (free) unless the
    /// caster is Wet, checked by Preview BEFORE ink.</item>
    /// <item><see cref="OnTargetResolved"/> — Fulmination writes tile
    /// charge at the victim's cell whether or not the victim survived;
    /// <see cref="ApplyPayoff"/> stays survivor-gated for riders.</item>
    /// <item><see cref="CastMessage"/> now receives the target list —
    /// HangingBolt and StillHeart name their single victim.</item>
    /// </list>
    /// </summary>
    public abstract class ConsumingRiteSkillBase : SpellSkillPart
    {
        /// <summary>Shape of the cast. <c>Self</c> spends from and
        /// applies to the caster (ScaldingVeil).</summary>
        public enum RiteShape { SingleTarget, Cone, Radius, Self }

        // ── What a concrete rite declares ────────────────────────

        public abstract string CommandName { get; }
        public abstract string Element { get; }
        public abstract RiteShape Shape { get; }
        public abstract int Range { get; }
        public abstract int Cooldown { get; }

        /// <summary>Statuses this rite may spend per target.</summary>
        public virtual int Slots => 2;

        /// <summary>Damage before the resonance multiplier. Small on
        /// purpose — see the class docstring.</summary>
        public virtual int BaseDamage => 4;

        /// <summary>Attributes stacked onto the damage. Empty = untyped.</summary>
        public virtual string[] DamageAttributes => System.Array.Empty<string>();

        /// <summary>The rite's damage for one target. Default is the
        /// spine's <c>round(BaseDamage × Multiplier)</c>; override to
        /// keep a hand-rolled rite's exact arithmetic.</summary>
        protected virtual int ComputeDamage(ResonanceSystem.Result res)
            => (int)Math.Round(BaseDamage * res.Multiplier);

        /// <summary>Pre-ink gate on the gathered targets. Return false
        /// (with a reject reason and optional player-facing message) to
        /// refuse the cast for FREE. ScaldingVeil checks the caster is
        /// Wet here.</summary>
        protected virtual bool ValidateBeforeInk(
            Entity caster, List<Entity> targets, out string reason, out string message)
        {
            reason = null; message = null;
            return true;
        }

        /// <summary>
        /// What this rite does with what it consumed. Called once per
        /// target, AFTER damage, and only on survivors — riders never
        /// land on the dead.
        /// </summary>
        protected abstract void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone);

        /// <summary>Per-target world side effects that must run whether
        /// or not the target survived (Fulmination's tile charge).</summary>
        protected virtual void OnTargetResolved(
            Entity target, ResonanceSystem.Result res, Zone zone) { }

        /// <summary>
        /// CASTER-side reward, called once after every target resolves,
        /// unconditionally — including when the rite killed everything
        /// it hit. Riders are survivor-gated; earnings are not
        /// (Bloodletter's heal must pay out over a corpse).
        /// </summary>
        protected virtual void OnCastResolved(int totalMarks, Zone zone) { }

        /// <summary>Line the player sees. Return null to skip.</summary>
        protected abstract string CastMessage(
            Entity caster, List<Entity> targets, int marks);

        // ── The spine ────────────────────────────────────────────

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = DisplayName,
                Command = CommandName,
                Class = "Rites",
                TargetingMode = Shape == RiteShape.Radius || Shape == RiteShape.Self
                    ? AbilityTargetingMode.SelfCentered
                    : AbilityTargetingMode.DirectionLine,
                Range = Range,
                Cooldown = Cooldown,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var caster = ctx.Attacker;
            if (ctx.Zone == null) { Reject(caster, "no_zone"); return false; }
            var zone = ctx.Zone;

            var pos = zone.GetEntityPosition(caster);
            if (pos.x < 0) { Reject(caster, "actor_not_in_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (Shape != RiteShape.Radius && Shape != RiteShape.Self && dx == 0 && dy == 0)
            {
                Reject(caster, "no_direction");
                return false;
            }

            List<Entity> targets = GatherTargets(zone, caster, pos.x, pos.y, dx, dy);
            if (targets.Count == 0)
            {
                Reject(caster, "no_target");
                MessageLog.Add(caster.GetDisplayName() + "'s rite finds no mark.");
                return false;
            }

            if (!ValidateBeforeInk(caster, targets, out string gateReason, out string gateMessage))
            {
                Reject(caster, gateReason ?? "gate_refused");
                if (!string.IsNullOrEmpty(gateMessage)) MessageLog.Add(gateMessage);
                return false;
            }

            // Ink is checked AFTER targets: a rite that can do nothing
            // must never take a charge for it.
            var grimoire = GrimoireInk.FindInked(caster);
            if (grimoire == null)
            {
                Reject(caster, "no_ink");
                MessageLog.Add("The pages are dry — no ink to spend.");
                return false;
            }
            grimoire.TrySpend();

            int marks = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                SpellFxCapture.Target(zone, target);
                var res = ResonanceSystem.Spend(target, Element, Slots, caster, zone);
                SpellFxCapture.ConsumeMarks(zone, target, res.Consumed.Count, (float)res.Multiplier, res.Consumed);
                marks += res.Consumed.Count;

                int amount = ComputeDamage(res);
                if (amount > 0)
                {
                    var dmg = new Damage(amount);
                    var attrs = DamageAttributes;
                    for (int a = 0; a < attrs.Length; a++)
                        dmg.AddAttribute(attrs[a]);
                    CombatSystem.ApplyDamage(target, dmg, caster, zone);
                }

                OnTargetResolved(target, res, zone);

                // Riders never land on the dead.
                if (target.GetStatValue("Hitpoints", 0) > 0)
                    ApplyPayoff(target, res, zone);
            }

            OnCastResolved(marks, zone);

            // A SingleTarget rite files its record under the VICTIM so
            // "which rites were cast at this creature?" is a
            // `diag_query target=<id>`. AoE shapes file under the caster.
            Entity diagTarget = Shape == RiteShape.SingleTarget ? targets[0] : caster;

            Diag.Record("spell", "RiteCast", caster, diagTarget,
                new
                {
                    rite = Name, element = Element, shape = Shape.ToString(),
                    targets = targets.Count, statusesConsumed = marks,
                    inkLeft = grimoire.Charges,
                });

            string msg = CastMessage(caster, targets, marks);
            if (!string.IsNullOrEmpty(msg)) MessageLog.Add(msg);

            ctx.BlocksTurnAdvance = true;
            return true;
        }

        private List<Entity> GatherTargets(
            Zone zone, Entity caster, int x, int y, int dx, int dy)
        {
            switch (Shape)
            {
                case RiteShape.Self:
                    return new List<Entity>(1) { caster };

                case RiteShape.Radius:
                    return SpellTargeting.GetCreaturesInRadius(
                        zone, x, y, Range, exclude: caster);

                case RiteShape.Cone:
                    return SpellTargeting.GetCreaturesInCone(
                        zone, caster, x, y, dx, dy, Range);

                default:
                    var one = new List<Entity>(1);
                    var t = RiteTargeting.FirstCreatureInLine(
                        zone, caster, x, y, dx, dy, Range);
                    if (t != null) one.Add(t);
                    return one;
            }
        }

        private void Reject(Entity caster, string reason)
            => Diag.Record("spell", "RiteRejected", caster, caster,
                new { rite = Name, reason });
    }
}
