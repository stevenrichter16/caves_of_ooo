using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles XP awards from kills and level-up progression.
    /// XP formula mirrors Qud: XP(L) = floor(L^3 * 15) + 100.
    /// Level-up rewards: 2 + Toughness modifier max HP (at least 1), +1 MP, +1 SP,
    /// an attribute point at 3/9/15…, +1 to every attribute at 6/12/18…, heal to full.
    /// </summary>
    public static class LevelingSystem
    {
        /// <summary>
        /// XP required to reach the next level from current level.
        /// </summary>
        /// <summary>Health gained per level before the Toughness modifier.</summary>
        public const int BaseHPPerLevel = 2;

        public static int XPToNextLevel(int currentLevel)
        {
            return (int)Math.Floor(currentLevel * currentLevel * currentLevel * 15.0) + 100;
        }

        /// <summary>Longest party-leader chain walked when passing an award up.</summary>
        const int MaxLeaderHops = 8;

        /// <summary>
        /// Award kill XP for <paramref name="victim"/>. Called from CombatSystem.HandleDeath
        /// for every killer. Qud parity (XRL.World.Parts/Experience.cs): the award goes to
        /// the player, either as the killer or as the party leader at the top of the
        /// killer's leader chain (a companion's kill pays its leader), and is scaled by the
        /// level-band gap between that recipient and the victim
        /// (<see cref="ProgressionCurve.ScaleKillXP"/>). A victim tagged NoXP pays nothing;
        /// a victim with no Level counts as its tier's lowest level. Emits
        /// progression/XPAwarded or progression/XPWithheld (reason: noxp, outclassed).
        /// </summary>
        public static void AwardKillXP(Entity killer, Entity victim, Zone zone)
        {
            if (killer == null || victim == null) return;
            Entity recipient = AwardRecipient(killer);
            if (recipient == null) return;
            var xpStat = recipient.GetStat("Experience");
            if (xpStat == null) return;

            int raw = victim.GetStatValue("XPValue", 0);
            int victimLevel = victim.GetStatValue("Level", ProgressionCurve.CreatureLevelBand(ProgressionCurve.CreatureTier(victim)).Min);
            int recipientLevel = recipient.GetStatValue("Level", 1);
            string via = ReferenceEquals(recipient, killer) ? "kill" : "companion";
            if (victim.HasTag("NoXP") || raw <= 0)
            {
                RecordXP("XPWithheld", recipient, victim, raw, 0, recipientLevel, victimLevel, via, "noxp");
                return;
            }
            int awarded = ProgressionCurve.ScaleKillXP(raw, recipientLevel, victimLevel);
            if (awarded <= 0)
            {
                RecordXP("XPWithheld", recipient, victim, raw, 0, recipientLevel, victimLevel, via, "outclassed");
                return;
            }

            xpStat.BaseValue += awarded;
            MessageLog.Add($"You gain {awarded} XP.");
            RecordXP("XPAwarded", recipient, victim, raw, awarded, recipientLevel, victimLevel, via,
                awarded < raw ? "outclassed" : "full");

            CheckLevelUp(recipient, zone);
        }

        /// <summary>The player who earns a kill: the killer itself, or the player at the
        /// top of its party-leader chain. Null when no player is involved.</summary>
        static Entity AwardRecipient(Entity killer)
        {
            Entity current = killer;
            for (int hop = 0; hop <= MaxLeaderHops && current != null; hop++)
            {
                if (current.HasTag("Player")) return current;
                current = current.GetPart<BrainPart>()?.PartyLeader;
            }
            return null;
        }

        static void RecordXP(string kind, Entity recipient, Entity victim, int raw, int awarded,
            int recipientLevel, int victimLevel, string via, string reason)
        {
            if (!CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("progression")) return;
            CavesOfOoo.Diagnostics.Diag.Record(category: "progression", kind: kind, actor: recipient, target: victim,
                payload: new { raw, awarded, recipientLevel, victimLevel, via, reason });
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
                xpStat.BaseValue -= threshold;
                levelStat.BaseValue++;
                int newLevel = levelStat.Value;

                // P2b, Qud parity (Leveler.RollHP): 2 + the Toughness modifier, at least 1
                // (Qud rolls 1-4; CoO keeps its deterministic base of 2). Living actors heal
                // to full. Self-kill XP must not refill a victim whose death lifecycle has
                // already committed.
                var hp = entity.GetStat("Hitpoints");
                int hpGain = Math.Max(BaseHPPerLevel + StatUtils.GetModifier(entity, "Toughness"), 1);
                if (hp != null)
                {
                    hp.Max += hpGain;
                    if (!CombatSystem.IsDeathHandled(entity))
                        hp.BaseValue = hp.Max;
                }

                // P2b, Qud parity (Leveler.cs:268-269): an attribute point at 3, 9, 15…;
                // every attribute +1 at 6, 12, 18… (a Toughness rise pays back earlier levels).
                var apStat = entity.GetStat("AP");
                bool gainedAP = apStat != null && AttributeAdvancement.GrantsPoint(newLevel);
                if (gainedAP) apStat.BaseValue += 1;
                bool attributeBonus = AttributeAdvancement.GrantsBonus(newLevel);
                if (attributeBonus)
                    foreach (string attribute in AttributeAdvancement.Attributes)
                        AttributeAdvancement.Raise(entity, attribute, 1);

                // +1 MP for mutation advancement
                var mpStat = entity.GetStat("MP");
                bool gainedMP = entity.GainMP(1);

                // ST.4: +1 SP for skill-tree progression. The grant is
                // null-safe: NPCs / minor actors that don't carry an SP
                // stat (no SkillsPart, no skill-economy participation)
                // simply don't gain SP. Mirrors the HP / MP grants above
                // — same null-check shape, same per-level cadence.
                var spStat = entity.GetStat("SP");
                if (spStat != null)
                    spStat.BaseValue += 1;

                // Level-up FX: yellow ring wave centered on player
                if (zone != null)
                {
                    Cell cell = zone.GetEntityCell(entity);
                    if (cell != null)
                        AsciiFxBus.EmitRingWave(zone, cell.X, cell.Y, 2, 0.06f,
                            AsciiFxTheme.Holy, blocksTurnAdvance: false);
                }

                string announcement = $"You advance to level {newLevel}!";
                var rewards = new List<AnnouncementFact>();
                // Report only grants made to the current stat instances. GainedMP
                // callbacks may remove or replace a stat before this snapshot.
                // Computed totals honor caps/modifiers; current health makes no
                // claim that dead or penalized actors were healed to full.
                if (hp != null && ReferenceEquals(entity.GetStat("Hitpoints"), hp))
                {
                    rewards.Add(new AnnouncementFact("Maximum HP", $"+{hpGain}; now {hp.Max}"));
                    rewards.Add(new AnnouncementFact("Health", $"{hp.Value} / {hp.Max}"));
                }
                if (gainedMP && mpStat != null && ReferenceEquals(entity.GetStat("MP"), mpStat))
                    rewards.Add(new AnnouncementFact("MP gained", $"+1; now {mpStat.Value}"));
                if (spStat != null && ReferenceEquals(entity.GetStat("SP"), spStat))
                    rewards.Add(new AnnouncementFact("SP gained", $"+1; now {spStat.Value}"));
                if (gainedAP && ReferenceEquals(entity.GetStat("AP"), apStat))
                    rewards.Add(new AnnouncementFact("Attribute point", $"+1; now {apStat.Value} (spend it on the skills screen)"));
                if (attributeBonus)
                    rewards.Add(new AnnouncementFact("Attributes", "+1 to each"));
                MessageLog.AddPresentedAnnouncement(announcement,new AnnouncementDocument(
                    $"Level {newLevel}","LEVEL UP",new[]{new AnnouncementSection("",announcement,AnnouncementTone.Positive)},rewards));
            }
        }
    }
}
