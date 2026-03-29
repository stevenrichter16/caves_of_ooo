using System;

namespace CavesOfOoo.Core
{
    public class FrostNovaMutation : BaseMutation
    {
        public const string COMMAND = "CommandFrostNova";
        public const int RADIUS = 2;
        public const int COOLDOWN = 12;
        private const float ChargeDuration = 0.10f;
        private const float RingStepDuration = 0.08f;

        public override string Name => "FrostNova";
        public override string MutationType => "Physical";
        public override string DisplayName => "Frost Nova";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Physical Mutations",
                AbilityTargetingMode.SelfCentered,
                RADIUS);
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

            if (!Cast(zone, sourceCell, rng))
                return true;

            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell, System.Random rng)
        {
            if (ParentEntity == null || zone == null || sourceCell == null)
                return false;

            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration, AsciiFxTheme.Ice, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(
                zone,
                sourceCell.X,
                sourceCell.Y,
                maxRadius: RADIUS,
                stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Ice,
                blocksTurnAdvance: true,
                delay: ChargeDuration);

            var targets = SpellTargeting.GetCreaturesInRadius(zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: ParentEntity);
            if (targets.Count == 0)
                MessageLog.Add(ParentEntity.GetDisplayName() + " releases a freezing pulse into empty space.");

            for (int i = 0; i < targets.Count; i++)
            {
                Entity target = targets[i];
                Cell targetCell = zone.GetEntityCell(target);
                if (targetCell == null)
                    continue;

                int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X), Math.Abs(targetCell.Y - sourceCell.Y));
                int damage = DiceRoller.Roll("2d3", rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        ParentEntity.GetDisplayName() + " freezes " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0 && radius <= 1)
                    target.ApplyEffect(new StunnedEffect(duration: 1), ParentEntity, zone);

                AsciiFxBus.EmitBurst(
                    zone,
                    targetCell.X,
                    targetCell.Y,
                    AsciiFxTheme.Ice,
                    blocksTurnAdvance: true,
                    delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
