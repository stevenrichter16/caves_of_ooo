using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles XP awards from kills and level-up progression.
    /// XP formula mirrors Qud: XP(L) = floor(L^3 * 15) + 100.
    /// Level-up rewards: +2 max HP, +1 MP, heal to full.
    /// </summary>
    public static class LevelingSystem
    {
        /// <summary>
        /// XP required to reach the next level from current level.
        /// </summary>
        public static int XPToNextLevel(int currentLevel)
        {
            return (int)Math.Floor(currentLevel * currentLevel * currentLevel * 15.0) + 100;
        }

        /// <summary>
        /// Award XP to the killer based on the victim's XPValue stat.
        /// Called from CombatSystem.HandleDeath.
        /// </summary>
        public static void AwardKillXP(Entity killer, Entity victim, Zone zone)
        {
            if (killer == null || victim == null) return;

            int xpValue = victim.GetStatValue("XPValue", 0);
            if (xpValue <= 0) return;

            var xpStat = killer.GetStat("Experience");
            if (xpStat == null) return;

            xpStat.BaseValue += xpValue;
            MessageLog.Add($"You gain {xpValue} XP.");

            CheckLevelUp(killer, zone);
        }

        /// <summary>
        /// Check if the entity has enough XP to level up, and apply rewards.
        /// </summary>
        public static void CheckLevelUp(Entity entity, Zone zone)
        {
            var xpStat = entity.GetStat("Experience");
            var levelStat = entity.GetStat("Level");
            if (xpStat == null || levelStat == null) return;

            while (xpStat.Value >= XPToNextLevel(levelStat.Value))
            {
                levelStat.BaseValue++;
                int newLevel = levelStat.Value;

                // +2 max HP and heal to full
                var hp = entity.GetStat("Hitpoints");
                if (hp != null)
                {
                    hp.Max += 2;
                    hp.BaseValue = hp.Max;
                }

                // +1 MP for mutation advancement
                entity.GainMP(1);

                // Level-up FX: yellow ring wave centered on player
                if (zone != null)
                {
                    Cell cell = zone.GetEntityCell(entity);
                    if (cell != null)
                        AsciiFxBus.EmitRingWave(zone, cell.X, cell.Y, 2, 0.06f,
                            AsciiFxTheme.Holy, blocksTurnAdvance: false);
                }

                MessageLog.AddAnnouncement($"You advance to level {newLevel}!");
            }
        }
    }
}
