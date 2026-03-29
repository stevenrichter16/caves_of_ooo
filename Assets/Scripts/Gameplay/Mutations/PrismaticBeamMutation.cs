using System;

namespace CavesOfOoo.Core
{
    public class PrismaticBeamMutation : BaseMutation
    {
        public const string COMMAND = "CommandPrismaticBeam";
        public const int RANGE = 7;
        public const int COOLDOWN = 14;
        private const float ChargeDuration = 0.08f;
        private const float BeamDuration = 0.12f;

        public override string Name => "PrismaticBeam";
        public override string MutationType => "Physical";
        public override string DisplayName => "Prismatic Beam";

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

            BeamTraceResult trace = SpellTargeting.TraceBeam(zone, ParentEntity, sourceCell.X, sourceCell.Y, dx, dy, RANGE);
            if (trace.Path.Count == 0)
                return false;

            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration, AsciiFxTheme.Arcane, blocksTurnAdvance: true);
            AsciiFxBus.EmitBeam(zone, trace.Path, dx, dy, AsciiFxTheme.Arcane, duration: BeamDuration, blocksTurnAdvance: true, delay: ChargeDuration);

            Point impact = trace.GetImpactPoint();
            if (impact.X >= 0)
            {
                AsciiFxBus.EmitBurst(
                    zone,
                    impact.X,
                    impact.Y,
                    AsciiFxTheme.Arcane,
                    blocksTurnAdvance: true,
                    delay: ChargeDuration + BeamDuration);
            }

            if (trace.HitEntities.Count == 0)
            {
                if (trace.BlockedBySolid)
                    MessageLog.Add(ParentEntity.GetDisplayName() + "'s prismatic beam splashes against an obstacle!");
                else
                    MessageLog.Add(ParentEntity.GetDisplayName() + "'s prismatic beam cuts through empty air.");
            }

            for (int i = 0; i < trace.HitEntities.Count; i++)
            {
                Entity target = trace.HitEntities[i];
                int damage = DiceRoller.Roll("3d4", rng);
                if (damage <= 0)
                    continue;

                MessageLog.Add(
                    ParentEntity.GetDisplayName() + " lances " +
                    target.GetDisplayName() + " with prismatic light for " + damage + " damage!");
                CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
