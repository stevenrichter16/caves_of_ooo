using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Thunderclap.
    /// Self-centered AoE lightning burst. Creatures in radius take 2d6
    /// (doubled against wet targets) and receive ElectrifiedEffect(1.0).
    /// Conductive props in radius (Conductivity > 0.5 or Metal/Conductor
    /// tagged) receive ElectrifiedEffect(0.8) and chain on their next
    /// EndTurn tick via MaterialPart.HandleTryChainElectricity. Wooden
    /// barrels and flesh creatures without Metal tagging stay untouched.
    /// </summary>
    public class ThunderclapMutation : BaseMutation
    {
        public const string COMMAND = "CommandThunderclap";
        public const int RADIUS = 2;
        public const int COOLDOWN = 18;
        private const float ChargeDuration = 0.12f;
        private const float RingStepDuration = 0.08f;

        public override string Name => "Thunderclap";
        public override string MutationType => "Mental";
        public override string DisplayName => "Thunderclap";

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

            // FX: charge orbit + lightning ring wave
            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Lightning, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, sourceCell.X, sourceCell.Y,
                maxRadius: RADIUS, stepDuration: RingStepDuration,
                theme: AsciiFxTheme.Lightning, blocksTurnAdvance: true, delay: ChargeDuration);

            // 1. Damage creatures in radius. Wet targets take double damage
            // (water conducts), and all survivors receive ElectrifiedEffect(1.0)
            // which in turn amplifies its own Charge on wet targets via
            // ElectrifiedEffect.OnApply — so a soaked creature both gets
            // doubled damage and a doubled-charge effect.
            var creatures = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: ParentEntity);

            if (creatures.Count == 0)
                MessageLog.Add(ParentEntity.GetDisplayName() + " unleashes a clap of thunder into empty space.");

            for (int i = 0; i < creatures.Count; i++)
            {
                Entity target = creatures[i];
                Cell targetCell = zone.GetEntityCell(target);

                int damage = DiceRoller.Roll("2d6", rng);
                var wet = target.GetEffect<WetEffect>();
                if (wet != null && wet.Moisture > 0.2f)
                    damage *= 2;

                if (damage > 0)
                {
                    MessageLog.Add(
                        ParentEntity.GetDisplayName() + " jolts " +
                        target.GetDisplayName() + " for " + damage + " damage!");
                    CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
                }

                if (target.GetStatValue("Hitpoints", 0) > 0)
                {
                    target.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), ParentEntity, zone);
                }

                if (targetCell != null)
                {
                    int radius = Math.Max(Math.Abs(targetCell.X - sourceCell.X),
                        Math.Abs(targetCell.Y - sourceCell.Y));
                    AsciiFxBus.EmitBurst(zone, targetCell.X, targetCell.Y,
                        AsciiFxTheme.Lightning, blocksTurnAdvance: true,
                        delay: ChargeDuration + ((Math.Max(1, radius) - 1) * RingStepDuration));
                }
            }

            // 2. Electrify conductive props in radius. Unlike Conflagration's
            // "every ThermalPart" pass, Thunderclap only charges entities that
            // are actually conductive — wooden barrels stay inert, metal
            // crates/weapons pick up an ElectrifiedEffect that will then chain
            // on their own EndTurn tick via MaterialPart.HandleTryChainElectricity.
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

                        // Skip creatures — they already handled in the creature pass.
                        if (entity.HasTag("Creature"))
                            continue;

                        var mat = entity.GetPart<MaterialPart>();
                        if (mat == null)
                            continue;

                        bool isConductor = mat.Conductivity > 0.5f
                            || mat.HasMaterialTag("Metal")
                            || mat.HasMaterialTag("Conductor");
                        if (!isConductor)
                            continue;

                        entity.ApplyEffect(new ElectrifiedEffect(charge: 0.8f), ParentEntity, zone);
                    }
                }
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
