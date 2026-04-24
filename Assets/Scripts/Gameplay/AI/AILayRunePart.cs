using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that occasionally has an NPC lay a defensive rune
    /// in their current zone. Mirrors Qud's <c>XRL.World.Parts.Miner</c>
    /// (Miner.cs:95-128 — <c>BeginTakeActionEvent</c> handler), adapted to
    /// CoO's <see cref="AIBoredEvent"/> pipeline.
    ///
    /// Blueprint attachment:
    /// <code>
    ///   { "Name": "AILayRune", "Params": [
    ///       { "Key": "Chance", "Value": "10" },
    ///       { "Key": "MaxRunesPerZone", "Value": "5" },
    ///       { "Key": "RuneBlueprints", "Value": "RuneOfFlame,RuneOfFrost,RuneOfPoison" }
    ///   ]}
    /// </code>
    ///
    /// <para><b>Gates</b> (all must pass per bored tick):</para>
    /// <list type="bullet">
    ///   <item>Probability: <c>Rng.Next(100) &lt; Chance</c>.</item>
    ///   <item>Stack cleanliness: no existing <see cref="LayRuneGoal"/>.</item>
    ///   <item>Quota: total rune count in zone &lt; <see cref="MaxRunesPerZone"/>.
    ///   Mirrors Qud's <c>Miner.MaxMinesPerZone</c> cap (Miner.cs:19).</item>
    ///   <item>A passable target cell exists within
    ///   <see cref="SearchRadius"/> of the NPC.</item>
    /// </list>
    ///
    /// <para><b>Target selection.</b> Picks a random passable cell within
    /// Chebyshev distance <see cref="SearchRadius"/> of the NPC, excluding
    /// the NPC's own cell and cells that already carry a Rune tag. Matches
    /// Qud's <c>CurrentZone.GetEmptyReachableCells().GetRandomElement()</c>
    /// simplified to a bounded radius search (cheaper than a full reachability
    /// flood and adequate for ambush scenarios where cultists want runes
    /// near themselves).</para>
    /// </summary>
    public class AILayRunePart : AIBehaviorPart
    {
        public override string Name => "AILayRune";

        /// <summary>Percent chance per bored tick to attempt a rune lay (0-100).</summary>
        public int Chance = 10;

        /// <summary>Per-zone cap on runes placed by this entity (total across all AILayRunePart instances).
        /// Matches Qud's Miner.MaxMinesPerZone=15, scaled down for CoO's smaller zones.</summary>
        public int MaxRunesPerZone = 5;

        /// <summary>Chebyshev radius the NPC will search for a rune-placement target.
        /// 4 keeps the NPC close enough to walk there within MoveToGoal's MaxTurns budget.</summary>
        public int SearchRadius = 4;

        /// <summary>Comma-separated blueprint names of runes this NPC can lay.
        /// A random entry is chosen per tick. Set via blueprint param.</summary>
        public string RuneBlueprints = "RuneOfFlame,RuneOfFrost,RuneOfPoison";

        // Cache of the blueprint list split on first use. Invalidated when
        // RuneBlueprints changes (checked by string equality on the raw field).
        private string[] _runeList;
        private string _runeListSource;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID) return true;
            bool proceed = HandleBored();
            if (!proceed) e.Handled = true;
            return proceed;
        }

        /// <summary>
        /// Returns false (consumed) when a LayRuneGoal was pushed — BoredGoal
        /// then returns early without running its Staying / furniture / wander
        /// branches. True otherwise so other idle behaviors can take over.
        /// </summary>
        private bool HandleBored()
        {
            var brain = ParentEntity?.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null) return true;

            // --- Stack cleanliness (Qud Miner.cs:110 — !HasGoal("LayMineGoal")) ---
            if (brain.HasGoal<LayRuneGoal>()) return true;

            // --- Probability gate ---
            if (brain.Rng.Next(100) >= Chance) return true;

            // --- Zone quota (Qud Miner.cs:124 — MaxMinesPerZone) ---
            if (CountRunesInZone(brain.CurrentZone) >= MaxRunesPerZone) return true;

            // --- Resolve the NPC's cell, then scan for a target ---
            var pos = brain.CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x < 0) return true;

            var target = PickRunePlacementCell(brain.CurrentZone, pos.x, pos.y, brain.Rng);
            if (!target.found) return true;

            // --- Pick a rune blueprint at random from the configured list ---
            string blueprint = PickRuneBlueprint(brain.Rng);
            if (string.IsNullOrEmpty(blueprint)) return true;

            brain.PushGoal(new LayRuneGoal(target.x, target.y, blueprint));
            return false; // consumed — don't fall through to default idle
        }

        /// <summary>
        /// Count entities in the zone tagged "Rune". Runs O(N) in zone
        /// entity count; Caves of Qud does the same per-tick scan inside
        /// Miner.ShouldPlaceMine (acceptable for the small entity counts
        /// in a single zone).
        /// </summary>
        private static int CountRunesInZone(Zone zone)
        {
            int count = 0;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.HasTag("Rune")) count++;
            }
            return count;
        }

        /// <summary>
        /// Pick a random passable, empty (no existing Rune) cell within
        /// <see cref="SearchRadius"/> of <paramref name="fromX"/>,<paramref name="fromY"/>.
        /// Excludes the NPC's own cell so the NPC doesn't try to lay a rune
        /// where they already stand (wasted turn — LayRuneGoal would spawn
        /// immediately, but blocks the cell from other placements).
        /// </summary>
        private (bool found, int x, int y) PickRunePlacementCell(
            Zone zone, int fromX, int fromY, System.Random rng)
        {
            // Build the candidate list. Radius of 4 → up to 80 cells checked,
            // which is well within a bored-tick's budget.
            var candidates = new List<(int x, int y)>();
            for (int dy = -SearchRadius; dy <= SearchRadius; dy++)
            {
                for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
                {
                    if (dx == 0 && dy == 0) continue; // skip self-cell
                    int nx = fromX + dx;
                    int ny = fromY + dy;
                    if (!zone.InBounds(nx, ny)) continue;
                    var cell = zone.GetCell(nx, ny);
                    if (cell == null) continue;
                    if (!cell.IsPassable()) continue;
                    if (cell.HasObjectWithTag("Rune")) continue; // already runed
                    candidates.Add((nx, ny));
                }
            }
            if (candidates.Count == 0) return (false, 0, 0);
            var pick = candidates[rng.Next(candidates.Count)];
            return (true, pick.x, pick.y);
        }

        /// <summary>
        /// Parse <see cref="RuneBlueprints"/> (lazy, cached against the raw
        /// field) and return a random entry.
        /// </summary>
        private string PickRuneBlueprint(System.Random rng)
        {
            if (_runeList == null || _runeListSource != RuneBlueprints)
            {
                _runeListSource = RuneBlueprints;
                if (string.IsNullOrEmpty(RuneBlueprints))
                {
                    _runeList = System.Array.Empty<string>();
                }
                else
                {
                    var split = RuneBlueprints.Split(',');
                    var trimmed = new List<string>(split.Length);
                    for (int i = 0; i < split.Length; i++)
                    {
                        var s = split[i].Trim();
                        if (s.Length > 0) trimmed.Add(s);
                    }
                    _runeList = trimmed.ToArray();
                }
            }
            if (_runeList.Length == 0) return null;
            return _runeList[rng.Next(_runeList.Length)];
        }
    }
}
