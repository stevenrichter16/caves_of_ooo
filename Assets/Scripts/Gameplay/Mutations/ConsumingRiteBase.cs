using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared spine for the consuming rites — the grimoires found out in
    /// the world rather than bought.
    ///
    /// <para>Every rite does the same four things before it does anything
    /// interesting: find a target, check the ink, spend the target's
    /// statuses through <see cref="ResonanceSystem"/>, and emit the diag
    /// trail. The first five rites each wrote that out longhand, and
    /// four of them are already subtly different. This base fixes the
    /// spine so a rite file contains only what makes that rite
    /// different.</para>
    ///
    /// <para><b>The two invariants it enforces for everyone.</b> Ink is
    /// never spent on a cast that finds nothing — burning a charge to
    /// accomplish nothing is the worst feedback a costed spell can give.
    /// And a rite cast COLD is deliberately weak: the multiplier from
    /// consuming nothing is 1.0, and these rites carry small base
    /// numbers, so their power lives entirely in what you fed them.</para>
    /// </summary>
    public abstract class ConsumingRiteBase : BaseMutation
    {
        /// <summary>Shape of the cast.</summary>
        public enum RiteShape { SingleTarget, Cone, Radius }

        // ── What a concrete rite declares ────────────────────────

        public abstract string Command { get; }
        public abstract string Element { get; }
        public abstract RiteShape Shape { get; }
        public abstract int Range { get; }
        public abstract int Cooldown { get; }

        /// <summary>Statuses this rite may spend per target.</summary>
        public virtual int Slots => 2;

        /// <summary>Damage before the resonance multiplier. Small on
        /// purpose — see the class docstring.</summary>
        public virtual int BaseDamage => 4;

        public virtual string DamageAttribute => "";

        /// <summary>
        /// What this rite does with what it consumed. Called once per
        /// target, AFTER damage, and only on survivors.
        /// </summary>
        protected abstract void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone);

        /// <summary>Line the player sees. <paramref name="marks"/> is the
        /// total consumed across all targets.</summary>
        protected abstract string CastMessage(Entity caster, int targets, int marks);

        // ── The spine ────────────────────────────────────────────

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName, Command, "Rites",
                Shape == RiteShape.Radius
                    ? AbilityTargetingMode.SelfCentered
                    : AbilityTargetingMode.DirectionLine,
                Range);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != Command) return true;
            var zone = e.GetParameter<Zone>("Zone");
            int dx = e.GetParameter<int>("DirectionX");
            int dy = e.GetParameter<int>("DirectionY");
            if (!Cast(zone, dx, dy)) return true;
            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, int dx, int dy)
        {
            var caster = ParentEntity;
            if (caster == null || zone == null) return false;

            var pos = zone.GetEntityPosition(caster);
            if (pos.x < 0) return false;

            if (Shape != RiteShape.Radius && dx == 0 && dy == 0)
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
                var res = ResonanceSystem.Spend(target, Element, Slots, caster, zone);
                marks += res.Consumed.Count;

                int amount = (int)Math.Round(BaseDamage * res.Multiplier);
                if (amount > 0)
                {
                    var dmg = new Damage(amount);
                    if (!string.IsNullOrEmpty(DamageAttribute))
                        dmg.AddAttribute(DamageAttribute);
                    CombatSystem.ApplyDamage(target, dmg, caster, zone);
                }

                // Riders never land on the dead: there is nothing left to
                // afflict, and a corpse carrying a debuff reads as a bug.
                if (target.GetStatValue("Hitpoints", 0) > 0)
                    ApplyPayoff(target, res, zone);
            }

            Diag.Record("spell", "RiteCast", caster, caster,
                new
                {
                    rite = Name, element = Element, shape = Shape.ToString(),
                    targets = targets.Count, statusesConsumed = marks,
                    inkLeft = grimoire.Charges,
                });

            MessageLog.Add(CastMessage(caster, targets.Count, marks));
            CooldownMyActivatedAbility(ActivatedAbilityID, Cooldown);
            return true;
        }

        private List<Entity> GatherTargets(
            Zone zone, Entity caster, int x, int y, int dx, int dy)
        {
            switch (Shape)
            {
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
