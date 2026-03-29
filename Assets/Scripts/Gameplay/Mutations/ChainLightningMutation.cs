using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public class ChainLightningMutation : BaseMutation
    {
        public const string COMMAND = "CommandChainLightning";
        public const int RANGE = 6;
        public const int MAX_JUMPS = 2;
        public const int SEARCH_RADIUS = 3;
        public const int COOLDOWN = 14;
        private const float HopDuration = 0.05f;

        public override string Name => "ChainLightning";
        public override string MutationType => "Physical";
        public override string DisplayName => "Chain Lightning";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Physical Mutations",
                AbilityTargetingMode.DirectionLine,
                RANGE);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != COMMAND)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            System.Random rng = e.GetParameter<System.Random>("RNG") ?? new System.Random();
            int dx = e.GetIntParameter("DirectionX");
            int dy = e.GetIntParameter("DirectionY");

            if (!Cast(zone, sourceCell, dx, dy, rng))
                return true;

            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell, int dx, int dy, System.Random rng)
        {
            if (ParentEntity == null || zone == null || sourceCell == null || (dx == 0 && dy == 0))
                return false;

            LineTraceResult trace = LineTargeting.TraceFirstImpact(zone, ParentEntity, sourceCell.X, sourceCell.Y, dx, dy, RANGE);
            if (trace.Path.Count == 0)
                return false;

            var hopPoints = new List<Point> { new Point(sourceCell.X, sourceCell.Y) };
            var burstPoints = new List<Point>();

            Entity primaryTarget = trace.HitEntity;
            if (primaryTarget == null)
            {
                Point impact = trace.GetImpactPoint();
                hopPoints.Add(impact);
                burstPoints.Add(impact);
                MessageLog.Add(ParentEntity.GetDisplayName() + "'s chain lightning crackles into the ground.");

                AsciiFxBus.EmitChainArc(zone, hopPoints, AsciiFxTheme.Lightning, HopDuration, blocksTurnAdvance: true);
                AsciiFxBus.EmitBurst(zone, impact.X, impact.Y, AsciiFxTheme.Lightning, blocksTurnAdvance: true, delay: HopDuration);
                CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
                return true;
            }

            Cell primaryCell = zone.GetEntityCell(primaryTarget);
            if (primaryCell == null)
                return false;

            List<Entity> secondaryTargets = SpellTargeting.FindChainTargets(zone, ParentEntity, primaryTarget, MAX_JUMPS, SEARCH_RADIUS);

            hopPoints.Add(new Point(primaryCell.X, primaryCell.Y));
            burstPoints.Add(new Point(primaryCell.X, primaryCell.Y));
            for (int i = 0; i < secondaryTargets.Count; i++)
            {
                Cell cell = zone.GetEntityCell(secondaryTargets[i]);
                if (cell == null)
                    continue;
                hopPoints.Add(new Point(cell.X, cell.Y));
                burstPoints.Add(new Point(cell.X, cell.Y));
            }

            AsciiFxBus.EmitChainArc(zone, hopPoints, AsciiFxTheme.Lightning, HopDuration, blocksTurnAdvance: true);

            ApplyLightningHit(primaryTarget, zone, rng, damageDice: "2d4", applyStun: true);
            for (int i = 0; i < secondaryTargets.Count; i++)
                ApplyLightningHit(secondaryTargets[i], zone, rng, damageDice: "1d4", applyStun: false);

            for (int i = 0; i < burstPoints.Count; i++)
            {
                Point burst = burstPoints[i];
                AsciiFxBus.EmitBurst(
                    zone,
                    burst.X,
                    burst.Y,
                    AsciiFxTheme.Lightning,
                    blocksTurnAdvance: true,
                    delay: (i + 1) * HopDuration);
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }

        private void ApplyLightningHit(Entity target, Zone zone, System.Random rng, string damageDice, bool applyStun)
        {
            if (target == null)
                return;

            int damage = DiceRoller.Roll(damageDice, rng);
            if (damage > 0)
            {
                MessageLog.Add(
                    ParentEntity.GetDisplayName() + " shocks " +
                    target.GetDisplayName() + " for " + damage + " damage!");
                CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
            }

            if (applyStun && target.GetStatValue("Hitpoints", 0) > 0)
                target.ApplyEffect(new StunnedEffect(duration: 1), ParentEntity, zone);
        }
    }
}
