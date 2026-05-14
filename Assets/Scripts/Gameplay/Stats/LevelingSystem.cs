using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles XP awards from kills and level-up progression.
    /// XP formula mirrors Qud: XP(L) = floor(L^3 * 15) + 100.
    /// Level-up rewards: +2 max HP, +1 MP, heal to full.
    ///
    /// <para>Observability: emits <c>leveling/Awarded</c> on successful
    /// XP grants and <c>leveling/Rejected</c> on the silent reject
    /// paths (null killer/victim, no XPValue on victim, killer without
    /// Experience stat). Per-level transitions emit
    /// <c>leveling/LeveledUp</c> with the new level + HP/MP/SP grants.
    /// Queryable via <c>category=leveling</c>.</para>
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
            if (killer == null || victim == null)
            {
                EmitRejected(killer, victim, "NullArg", 0);
                return;
            }

            int xpValue = victim.GetStatValue("XPValue", 0);
            if (xpValue <= 0)
            {
                EmitRejected(killer, victim, "VictimHasNoXPValue", xpValue);
                return;
            }

            var xpStat = killer.GetStat("Experience");
            if (xpStat == null)
            {
                EmitRejected(killer, victim, "KillerHasNoExperienceStat", xpValue);
                return;
            }

            int xpBefore = xpStat.BaseValue;
            xpStat.BaseValue += xpValue;
            MessageLog.Add($"You gain {xpValue} XP.");

            if (Diag.IsChannelEnabled("leveling"))
            {
                int currentLevel = killer.GetStatValue("Level", 1);
                Diag.Record(
                    category: "leveling", kind: "Awarded",
                    actor: killer, target: victim,
                    payload: new
                    {
                        xpGained = xpValue,
                        xpBefore,
                        xpAfter = xpStat.BaseValue,
                        currentLevel,
                        xpToNext = XPToNextLevel(currentLevel),
                    });
            }

            CheckLevelUp(killer, zone);
        }

        private static void EmitRejected(Entity killer, Entity victim, string reason, int xpValue)
        {
            if (!Diag.IsChannelEnabled("leveling")) return;
            Diag.Record(
                category: "leveling", kind: "Rejected",
                actor: killer, target: victim,
                payload: new { reason, xpValue });
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
                int threshold = XPToNextLevel(levelStat.Value);
                int prevLevel = levelStat.Value;
                xpStat.BaseValue -= threshold;
                levelStat.BaseValue++;
                int newLevel = levelStat.Value;

                // +2 max HP and heal to full
                var hp = entity.GetStat("Hitpoints");
                int hpMaxBefore = hp?.Max ?? 0;
                if (hp != null)
                {
                    hp.Max += 2;
                    hp.BaseValue = hp.Max;
                }

                // +1 MP for mutation advancement
                entity.GainMP(1);

                // ST.4: +1 SP for skill-tree progression. The grant is
                // null-safe: NPCs / minor actors that don't carry an SP
                // stat (no SkillsPart, no skill-economy participation)
                // simply don't gain SP. Mirrors the HP / MP grants above
                // — same null-check shape, same per-level cadence.
                var spStat = entity.GetStat("SP");
                bool gainedSP = false;
                if (spStat != null)
                {
                    spStat.BaseValue += 1;
                    gainedSP = true;
                }

                // Level-up FX: yellow ring wave centered on player
                if (zone != null)
                {
                    Cell cell = zone.GetEntityCell(entity);
                    if (cell != null)
                        AsciiFxBus.EmitRingWave(zone, cell.X, cell.Y, 2, 0.06f,
                            AsciiFxTheme.Holy, blocksTurnAdvance: false);
                }

                MessageLog.AddAnnouncement($"You advance to level {newLevel}!");

                // Emit one LeveledUp record per level transition. If a
                // single AwardKillXP triggers multiple levels (rare but
                // possible), each transition emits its own record so the
                // diag stream shows the full ladder.
                if (Diag.IsChannelEnabled("leveling"))
                {
                    Diag.Record(
                        category: "leveling", kind: "LeveledUp",
                        actor: entity,
                        payload: new
                        {
                            prevLevel,
                            newLevel,
                            xpThresholdCrossed = threshold,
                            xpRemaining = xpStat.BaseValue,
                            hpMaxBefore,
                            hpMaxAfter = hp?.Max ?? 0,
                            healedToFull = hp != null,
                            gainedMP = 1,
                            gainedSP,
                        });
                }
            }
        }
    }
}
