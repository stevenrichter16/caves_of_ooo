using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared mutation base for straight-line projectile abilities.
    /// Damage and status application resolve immediately; the player's turn
    /// is held until the ASCII projectile/burst FX completes.
    /// </summary>
    public abstract class DirectionalProjectileMutationBase : BaseMutation
    {
        protected abstract string CommandName { get; }
        protected abstract AsciiFxTheme FxTheme { get; }
        protected abstract int CooldownTurns { get; }
        protected abstract int AbilityRange { get; }
        protected abstract string DamageDice { get; }
        protected abstract string AbilityClass { get; }
        protected abstract string ImpactVerb { get; }

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                CommandName,
                AbilityClass,
                AbilityTargetingMode.DirectionLine,
                AbilityRange);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != CommandName)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            System.Random rng = e.GetParameter<System.Random>("RNG") ?? new System.Random();
            int dx = e.GetIntParameter("DirectionX");
            int dy = e.GetIntParameter("DirectionY");

            if (zone == null || sourceCell == null || (dx == 0 && dy == 0))
                return true;

            if (!Cast(zone, sourceCell, dx, dy, rng))
                return true;

            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell, int dx, int dy, System.Random rng)
        {
            if (ParentEntity == null || zone == null || sourceCell == null)
                return false;

            LineTraceResult trace = LineTargeting.TraceFirstImpact(
                zone,
                ParentEntity,
                sourceCell.X,
                sourceCell.Y,
                dx,
                dy,
                AbilityRange);

            if (trace.Path.Count == 0)
                return false;

            AsciiFxBus.EmitProjectile(zone, trace.Path, FxTheme, trail: true, blocksTurnAdvance: true);

            Entity target = trace.HitEntity;
            if (target != null)
            {
                int damage = DiceRoller.Roll(DamageDice, rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        ParentEntity.GetDisplayName() + " " + ImpactVerb + " " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                    ApplyOnHitEffect(target, zone, rng);
            }
            else if (trace.BlockedBySolid)
            {
                MessageLog.Add(ParentEntity.GetDisplayName() + "'s " + DisplayName + " splashes against an obstacle!");
            }
            else
            {
                MessageLog.Add(ParentEntity.GetDisplayName() + "'s " + DisplayName + " dissipates harmlessly.");
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, CooldownTurns);
            return true;
        }

        protected virtual void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng) { }
    }
}
