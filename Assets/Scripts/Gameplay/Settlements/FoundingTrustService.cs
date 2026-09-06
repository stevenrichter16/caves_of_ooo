using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One-time plaque-tending service. The world fact survives
    /// floor unloads and replacement NPCs; only a successful exchange latches.
    /// Cached conversation choices are untrusted: revalidate before spending.</summary>
    public static class FoundingTrustService
    {
        public const string OfferedFact = "FoundingStoneOffered";
        public const int ReputationReward = PlayerReputation.LIKED_THRESHOLD;

        /// <summary>Shared, side-effect-free permission for the dialogue row
        /// and execution. Guest safety does not manufacture the tender's consent.</summary>
        public static bool CanOffer(Entity player, Entity tender) => RejectionReason(player, tender) == null;

        private static string RejectionReason(Entity player, Entity tender)
        {
            var state = NarrativeStatePart.Current;
            var zone = SettlementRuntime.ActiveZone;
            var wants = tender?.GetPart<WantsMineralPart>();
            if (player == null || !player.HasTag("Player") || state == null) return "no_player_session";
            if (state.GetFact(OfferedFact) != 0) return "already_offered";
            if (tender?.BlueprintName != "FoundingPlaqueTender" || wants == null
                || wants.Faction != "CatacombFolk" || wants.RepReward != ReputationReward || !wants.Wants("Tepuibone")) return "wrong_tender";
            if (PlayerReputation.Get("CatacombFolk") < 0
                || FactionManager.GetFeelingUnfloored(tender, player) <= FactionManager.HOSTILE_THRESHOLD) return "standing_refusal";
            if (zone == null || !DestructionSystem.IsWithinStrikeReach(player, tender, zone)) return "out_of_reach";
            var inventory = player.GetPart<InventoryPart>();
            if (inventory?.FindConsumableByBlueprint("Tepuibone") != null) return null;
            // Preserve the distinct empty-stack refusal, after looking for any
            // valid alternative rather than stopping at the first matching entry.
            if (inventory?.Objects != null) foreach (var item in inventory.Objects)
                if (item != null && string.Equals(item.BlueprintName, "Tepuibone", System.StringComparison.OrdinalIgnoreCase))
                    return "empty_stack";
            return "no_stone";
        }

        public static bool TryOffer(Entity player, Entity tender)
        {
            string reason = RejectionReason(player, tender);
            if (reason != null)
            {
                MessageLog.Add("The tender cannot accept this offering now.");
                if (Diag.IsChannelEnabled("furniture")) Diag.Record("furniture", "FoundingOfferRefused", player, tender, new { reason });
                return false;
            }
            if (!MineralTradeService.TryTrade(player, tender, "Tepuibone"))
            { MessageLog.Add("You have no tepuibone chip to place beside the inscription."); return false; }
            NarrativeStatePart.Current.SetFact(OfferedFact, 1); NarrativeStatePart.Current.LogEvent(OfferedFact);
            MessageLog.Add("The tender fits your stone beside an old name and speaks its worn syllables for you to repeat. When you finish, she makes room beside the lamp. The Listening elders will receive you now.");
            return true;
        }
    }
}
