using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Rime Nova.
    /// Self-centered AoE cold burst that damages creatures, applies
    /// FrozenEffect(0.6) — strong enough to lock actions via
    /// FrozenEffect.AllowAction — and floods ALL entities with ThermalPart
    /// in radius with -200J cooling. Burning props cross FlameTemperature
    /// downward and extinguish; brittle metal props cross FreezeTemperature
    /// and shatter via ThermalPart.TryFreeze → FrozenEffect.OnApply.
    /// </summary>
    public class RimeNovaMutation : BaseMutation
    {
        public const string COMMAND = "CommandRimeNova";
        public const int RADIUS = 2;
        public const int COOLDOWN = 15;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override string Name => "Rime Nova";
        public override string MutationType => "Mental";
        public override string DisplayName => "Rime Nova";

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

            // FX: charge orbit + ice ring wave
            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Ice, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Ice, blocksTurnAdvance: true, delay: ChargeDuration);

            // 1. Damage creatures in radius and apply FrozenEffect(0.6) directly.
            // Pre-applying preserves the 0.6 Cold value; the follow-up cooling
            // heat's TryFreeze short-circuits on HasEffect<FrozenEffect>(). At
            // any positive Cold the creature fails FrozenEffect.AllowAction
            // and is locked out of acting — the intentional AoE soft-lock.
            // Duration scales with thaw rate: Cold=0.6 @ ~0.025/turn thaw =
            // ~24 turns of lockout from a single cast. If that's too strong,
            // tune the Cold value down (0.3 → ~12 turns, 0.15 → ~6 turns)
            // rather than touching the AllowAction threshold — the threshold
            // is 0 by design so "frozen in log" and "frozen in practice"
            // stay aligned.
            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: ParentEntity);

            if (creatures.Count == 0)
                MessageLog.Add(ParentEntity.GetDisplayName() + " unleashes a wave of rime into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("1d6", rng);
                if (damage > 0)
                {
                    MessageLog.Add(
                        ParentEntity.GetDisplayName() + " rimes " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                {
                    target.ApplyEffect(new FrozenEffect(cold: 0.6f), ParentEntity, zone);
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Ice, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // 2. Cool ALL entities with ThermalPart in radius. Do NOT pre-apply
            // FrozenEffect to props — letting ThermalPart.TryFreeze handle them
            // means FrozenEffect.OnApply runs its brittle-shatter check against
            // the *post-cooling* temperature, so brittle frozen metal actually
            // shatters. Burning props also cross FlameTemperature downward and
            // extinguish — a barrel on fire goes out in one cast.
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

                    for (int i = cell.Objects.Count - 1; i >= 0; i--)
                    {
                        if (i >= cell.Objects.Count) continue;
                        Entity entity = cell.Objects[i];
                        if (entity == ParentEntity)
                            continue;

                        if (entity.HasPart<ThermalPart>())
                        {
                            var heatEvent = GameEvent.New("ApplyHeat");
                            heatEvent.SetParameter("Joules", (object)(-200f));
                            heatEvent.SetParameter("Radiant", (object)false);
                            heatEvent.SetParameter("Source", (object)ParentEntity);
                            heatEvent.SetParameter("Zone", (object)zone);
                            entity.FireEvent(heatEvent);
                            heatEvent.Release();
                        }
                    }
                }
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
