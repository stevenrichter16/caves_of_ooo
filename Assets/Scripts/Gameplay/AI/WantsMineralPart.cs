using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.5 — NPC Part declaring "I want minerals
    /// X, Y, Z; trading me one earns my faction +N reputation."
    /// Surfaces the IDEAS.md faction-wanting matrix as a per-NPC
    /// trade trigger.
    ///
    /// <para><b>Fields:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="Minerals"/> — comma-delimited list of mineral
    ///         blueprint names (e.g. <c>"PaleSalt,ChoirIron"</c>). The
    ///         player can deliver ANY of the listed minerals.</item>
    ///   <item><see cref="Faction"/> — the faction whose reputation
    ///         flows on successful trade. Typically the NPC's own
    ///         faction. Empty = no faction flow (still consumes the
    ///         mineral, but rep doesn't change — a charitable NPC).</item>
    ///   <item><see cref="RepReward"/> — the delta applied per trade
    ///         (positive for normal "I want this" wanting; could be
    ///         negative for an inverted "you brought me what I hate"
    ///         dynamic — future content).</item>
    /// </list>
    ///
    /// <para><b>Trade flow (via <see cref="MineralTradeService.TryTrade"/>):</b></para>
    /// <list type="number">
    ///   <item>NPC has <see cref="WantsMineralPart"/>.</item>
    ///   <item>Player carries one of the listed minerals.</item>
    ///   <item>Service consumes one from player's inventory (stack-aware).</item>
    ///   <item><see cref="PlayerReputation.Modify"/> fires for
    ///         <see cref="Faction"/> with <see cref="RepReward"/>.</item>
    ///   <item>Diag record emitted under <c>mineral-trade</c> category.</item>
    /// </list>
    ///
    /// <para><b>Qud parity:</b> CoO-original. Qud has Tinker mods +
    /// faction-flavored encounters but no "carry-this-rock-to-NPC-for-rep"
    /// gameplay loop. Faithful to IDEAS.md's faction-political mineral
    /// economy.</para>
    ///
    /// <para><b>Save/load (SL.6):</b> Minerals + Faction + RepReward
    /// are all public simple-typed fields and round-trip via
    /// reflection.</para>
    /// </summary>
    public class WantsMineralPart : Part
    {
        public override string Name => "WantsMineral";

        /// <summary>Comma-delimited list of mineral blueprint names this
        /// NPC accepts. Whitespace-trimmed; empty entries filtered.
        /// Mirrors <see cref="GrantsRepAsFollowerPart.Faction"/>
        /// comma-delim parsing pattern.</summary>
        public string Minerals = "";

        /// <summary>The faction whose reputation moves on successful
        /// trade. Empty string = no rep flow (consume-only NPC).</summary>
        public string Faction = "";

        /// <summary>Rep delta per successful trade. Typically positive
        /// (the NPC values the mineral); negative values let content
        /// model "this NPC was bribed against their will" scenarios.</summary>
        public int RepReward;

        public WantsMineralPart() { }

        public WantsMineralPart(string minerals, string faction, int repReward)
        {
            Minerals = minerals ?? "";
            Faction = faction ?? "";
            RepReward = repReward;
        }

        /// <summary>True if this NPC wants the given mineral blueprint
        /// name (case-insensitive). Whitespace + empty entries skipped.</summary>
        public bool Wants(string mineralBlueprintName)
        {
            if (string.IsNullOrEmpty(mineralBlueprintName)) return false;
            if (string.IsNullOrWhiteSpace(Minerals)) return false;

            string[] parts = Minerals.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string entry = parts[i].Trim();
                if (entry.Length == 0) continue;
                if (string.Equals(entry, mineralBlueprintName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Enumerate the (trimmed, non-empty) mineral blueprint
        /// names. Used by UI surfaces that want to show "I want X, Y, Z."</summary>
        public IReadOnlyList<string> GetWantedMinerals()
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(Minerals)) return result;
            string[] parts = Minerals.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string entry = parts[i].Trim();
                if (entry.Length > 0) result.Add(entry);
            }
            return result;
        }
    }

    /// <summary>
    /// Item Enhancements E.3.5 — service for the player-trades-mineral-
    /// to-NPC-for-rep flow. UI surfaces (dialog, trade prompts) call
    /// <see cref="TryTrade"/> after the player selects "give X."
    ///
    /// <para>v1 scope: the service handles the data flow + diag emission.
    /// Surface wiring (NPC dialog menu, "Give Mineral" inventory action)
    /// lands in E.4 / E.5+ when the showcase scenario exercises the loop.</para>
    /// </summary>
    public static class MineralTradeService
    {
        /// <summary>Diag category for mineral-trade events.</summary>
        public const string DIAG_CATEGORY = "mineral-trade";

        /// <summary>
        /// Try to trade a mineral from <paramref name="player"/> to
        /// <paramref name="npc"/>. Returns true on success; emits a
        /// <c>mineral-trade/Traded</c> or <c>mineral-trade/Rejected</c>
        /// diag record.
        ///
        /// <para>Rejection paths (each with a reason in the diag payload):</para>
        /// <list type="bullet">
        ///   <item><c>null_player</c> / <c>null_npc</c> / <c>null_mineral</c></item>
        ///   <item><c>no_wants_mineral_part</c> — NPC isn't a mineral-trader</item>
        ///   <item><c>not_wanted</c> — NPC doesn't want this specific mineral</item>
        ///   <item><c>player_lacks_inventory</c></item>
        ///   <item><c>mineral_not_in_inventory</c></item>
        /// </list>
        /// </summary>
        public static bool TryTrade(Entity player, Entity npc, string mineralBlueprintName)
        {
            if (player == null) { EmitRejected(player, npc, mineralBlueprintName, "null_player"); return false; }
            if (npc == null) { EmitRejected(player, npc, mineralBlueprintName, "null_npc"); return false; }
            if (string.IsNullOrEmpty(mineralBlueprintName))
            { EmitRejected(player, npc, mineralBlueprintName, "null_mineral"); return false; }

            var wants = npc.GetPart<WantsMineralPart>();
            if (wants == null)
            { EmitRejected(player, npc, mineralBlueprintName, "no_wants_mineral_part"); return false; }

            if (!wants.Wants(mineralBlueprintName))
            { EmitRejected(player, npc, mineralBlueprintName, "not_wanted"); return false; }

            var inv = player.GetPart<InventoryPart>();
            if (inv == null)
            { EmitRejected(player, npc, mineralBlueprintName, "player_lacks_inventory"); return false; }

            // Find the mineral by blueprint name. Use the first match
            // (stacks: just take one; non-stacks: remove the item).
            Entity mineral = null;
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                if (string.Equals(inv.Objects[i].BlueprintName, mineralBlueprintName,
                                  System.StringComparison.OrdinalIgnoreCase))
                {
                    mineral = inv.Objects[i];
                    break;
                }
            }
            if (mineral == null)
            { EmitRejected(player, npc, mineralBlueprintName, "mineral_not_in_inventory"); return false; }

            // Consume — handle stack-or-single. StackerPart with count>1
            // decrements; otherwise remove the whole entity.
            var stacker = mineral.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
            }
            else
            {
                inv.RemoveObject(mineral);
            }

            // Apply rep + emit success diag.
            if (!string.IsNullOrWhiteSpace(wants.Faction) && wants.RepReward != 0)
            {
                PlayerReputation.Modify(wants.Faction, wants.RepReward, silent: false);
            }

            if (Diag.IsChannelEnabled(DIAG_CATEGORY))
            {
                Diag.Record(
                    category: DIAG_CATEGORY,
                    kind: "Traded",
                    actor: player,
                    target: npc,
                    payload: new
                    {
                        mineral = mineralBlueprintName,
                        faction = wants.Faction ?? "",
                        repDelta = wants.RepReward
                    });
            }
            return true;
        }

        private static void EmitRejected(Entity player, Entity npc, string mineral, string reason)
        {
            if (!Diag.IsChannelEnabled(DIAG_CATEGORY)) return;
            Diag.Record(
                category: DIAG_CATEGORY,
                kind: "Rejected",
                actor: player,
                target: npc,
                payload: new { mineral = mineral ?? "", reason = reason });
        }
    }
}
