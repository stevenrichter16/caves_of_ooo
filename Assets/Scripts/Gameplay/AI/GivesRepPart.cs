using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// When an entity with this part dies, the killer gains/loses reputation
    /// with relevant factions. Attached to faction creatures via blueprints.
    ///
    /// On death by player:
    /// - Player loses Value reputation with victim's faction
    /// - Player gains Value/2 reputation with factions hostile to victim's faction
    /// </summary>
    public class GivesRepPart : Part
    {
        public override string Name => "GivesRep";

        /// <summary>
        /// Base reputation change when this entity is killed.
        /// </summary>
        public int Value = 10;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Died")
            {
                var killer = e.GetParameter("Killer") as Entity;
                if (killer == null || !killer.HasTag("Player")) return true;

                string victimFaction = FactionManager.GetFaction(ParentEntity);
                if (string.IsNullOrEmpty(victimFaction) || victimFaction == "Player") return true;

                // Lose reputation with the victim's faction
                PlayerReputation.Modify(victimFaction, -Value);

                // Gain reputation with factions that are hostile to the victim's faction
                var allFactions = FactionManager.GetAllFactions();
                for (int i = 0; i < allFactions.Count; i++)
                {
                    string faction = allFactions[i];
                    if (faction == victimFaction) continue;

                    int feeling = FactionManager.GetFactionFeeling(faction, victimFaction);
                    if (feeling <= FactionManager.HOSTILE_THRESHOLD)
                    {
                        int gain = Value / 2;
                        if (gain > 0)
                            PlayerReputation.Modify(faction, gain);
                    }
                }
            }
            return true;
        }
    }
}
