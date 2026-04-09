using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Conflagration.
    /// Self-centered AoE fire burst that damages creatures, applies BurningEffect,
    /// and heats ALL entities with ThermalPart in radius — igniting combustible
    /// objects and triggering chain propagation via MaterialSimSystem.
    /// </summary>
    public class ConflagrationMutation : BaseMutation
    {
        public const string COMMAND = "CommandConflagration";
        public const int RADIUS = 2;
        public const int COOLDOWN = 15;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override string Name => "ConflagrationMutation";
        public override string MutationType => "Mental";
        public override string DisplayName => "Conflagration";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Grimoire Spells",
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

            // FX: charge orbit + fire ring wave
            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Fire, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Fire, blocksTurnAdvance: true, delay: ChargeDuration);

            // 1. Damage creatures in radius and apply BurningEffect
            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: ParentEntity);

            if (creatures.Count == 0)
                MessageLog.Add(ParentEntity.GetDisplayName() + " unleashes a wave of fire into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("2d6", rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        ParentEntity.GetDisplayName() + " engulfs " +
                        target.GetDisplayName() + " in flames for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
                }

                // Apply BurningEffect at elevated intensity
                if (target.GetStatValue("Hitpoints", 0) > 0)
                    target.ApplyEffect(new BurningEffect(intensity: 1.5f, source: ParentEntity, rng: rng),
                        ParentEntity, zone);

                // Apply direct heat to creature
                if (damage > 0)
                {
                    var heatEvent = GameEvent.New("ApplyHeat");
                    heatEvent.SetParameter("Joules", (object)(damage * 8f));
                    heatEvent.SetParameter("Radiant", (object)false);
                    heatEvent.SetParameter("Source", (object)ParentEntity);
                    heatEvent.SetParameter("Zone", (object)zone);
                    target.FireEvent(heatEvent);
                    heatEvent.Release();
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Fire, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // 2. Heat ALL entities with ThermalPart in radius (not just creatures)
            // This is the key material system interaction — ignites barrels, etc.
            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int chebyshev = Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y));
                    if (chebyshev > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity entity = cell.Objects[i];
                        if (entity == ParentEntity)
                            continue;

                        if (entity.HasPart<ThermalPart>())
                        {
                            var heatEvent = GameEvent.New("ApplyHeat");
                            heatEvent.SetParameter("Joules", (object)250f);
                            heatEvent.SetParameter("Radiant", (object)false);
                            heatEvent.SetParameter("Source", (object)ParentEntity);
                            heatEvent.SetParameter("Zone", (object)zone);
                            entity.FireEvent(heatEvent);
                            heatEvent.Release();
                        }
                    }
                }
            }

            // 3. Emit radiant heat outward from caster for chain propagation
            MaterialSimSystem.EmitHeatToAdjacent(ParentEntity, zone, 100f);

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
