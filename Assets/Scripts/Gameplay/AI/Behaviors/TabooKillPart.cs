namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B6 — a creature some people hold sacred. Canon (catacomb_village_design.md):
    /// Sima-Stewards are "sacred. Their seeds founded the sinkhole. Protected by deep taboo
    /// in nearly all villages." When the player kills one, <see cref="Faction"/> standing
    /// changes by <see cref="Penalty"/>, with a line saying so.
    /// </summary>
    public sealed class TabooKillPart : Part
    {
        public override string Name => "TabooKill";

        public string Faction = "CatacombFolk";
        public int Penalty = -10;
        public string Line = "Somewhere below, someone will hear that a seed-bringer was killed.";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Died" && e.GetParameter<Entity>("Killer") is Entity killer && killer.HasTag("Player")
                && !string.IsNullOrEmpty(Faction))
            {
                PlayerReputation.Modify(Faction, Penalty);
                if (!string.IsNullOrEmpty(Line)) MessageLog.Add(Line);
            }
            return true;
        }
    }
}
