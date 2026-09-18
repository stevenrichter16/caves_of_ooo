using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    public enum QuestCueState { None, Available, Active }

    /// <summary>Read-only discovery state for a live offering owner. Active
    /// means journal work is ongoing; it never promises a valid turn-in.</summary>
    public static class QuestCueStateQuery
    {
        public static QuestCueState Evaluate(Entity owner, Zone zone, Entity player)
        {
            var canonical = EvaluateCanonicalQuest(owner, zone, player);
            if (canonical == QuestCueState.Active) return canonical;
            var regional = owner?.GetPart<RegionalRequestPart>()?.GetCueState(player, zone) ?? QuestCueState.None;
            return regional == QuestCueState.Active || canonical == QuestCueState.None ? regional : canonical;
        }

        private static QuestCueState EvaluateCanonicalQuest(Entity owner, Zone zone, Entity player)
        {
            var beacon = owner?.GetPart<QuestBeaconPart>();
            var journal = StoryletPart.Current;
            if (beacon == null || string.IsNullOrEmpty(beacon.Quest) || journal == null
                || zone == null || player == null || owner == player || !player.HasTag("Player")
                || !owner.HasTag("Creature") || owner.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(owner) || owner.GetPart<RenderPart>()?.Visible == false)
                return QuestCueState.None;
            // The bootstrap loads definitions before world presentation. This
            // lookup prevents stale/unknown beacon IDs from promising content.
            if (StoryletRegistry.FindQuest(beacon.Quest) == null) return QuestCueState.None;
            // These authored conversations are local to their real town owner.
            // FindOwner uses the scene's already-bound owner dictionary; this
            // does not search the world or invent a transferable quest giver.
            var resident = owner.GetPart<MorrowfastResidentPart>();
            if (resident != null && (!MorrowfastSceneRuntime.IsActive(zone)
                || !MatchesResidentId(owner.ID, resident.ResidentId)
                || MorrowfastSceneRuntime.FindOwner(zone, resident.ResidentId) != owner)) return QuestCueState.None;
            var ownerCell = zone.GetEntityCell(owner); var playerCell = zone.GetEntityCell(player);
            if (ownerCell == null || playerCell == null || !ownerCell.Objects.Contains(owner)
                || !playerCell.Objects.Contains(player) || FactionManager.IsHostile(owner, player)
                || journal.IsQuestCompleted(beacon.Quest)) return QuestCueState.None;
            return journal.IsQuestActive(beacon.Quest) ? QuestCueState.Active : QuestCueState.Available;
        }
        private static bool MatchesResidentId(string id, string resident)
        {
            const string prefix="morrowfast-owner:";
            return id!=null && resident!=null && id.Length==prefix.Length+resident.Length
                && id.StartsWith(prefix,System.StringComparison.Ordinal)
                && string.CompareOrdinal(id,prefix.Length,resident,0,resident.Length)==0;
        }
    }
}
