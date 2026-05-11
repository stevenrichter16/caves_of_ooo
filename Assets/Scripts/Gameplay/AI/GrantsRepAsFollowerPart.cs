namespace CavesOfOoo.Core
{
    /// <summary>
    /// Followers F.3.4 — Part that modifies the player's faction
    /// reputation while this creature is following the player and in
    /// the same zone. Each turn-end, the Part re-evaluates the
    /// apply/unapply state.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>XRL.World.Parts/GrantsRepAsFollower.cs</c>. CoO F.3.4 ports
    /// the full comma-delimited <c>Faction</c> syntax + per-entry
    /// <c>:N</c> override + idempotent apply/unapply + leader-is-player
    /// gate + same-zone gate + EndTurn hook.</para>
    ///
    /// <para><b>Apply conditions (CheckApplyBonus):</b> ALL must hold:</para>
    /// <list type="bullet">
    ///   <item>Leader (i.e. <c>parent.PartyLeader</c>) is not null</item>
    ///   <item>Leader has the <c>"Player"</c> tag</item>
    ///   <item>This entity's <see cref="BrainPart.CurrentZone"/> matches
    ///         the leader's</item>
    ///   <item>Not already applied (idempotency gate)</item>
    /// </list>
    /// When ALL hold → <see cref="ApplyBonus"/>. When ANY breaks while
    /// applied → <see cref="UnapplyBonus"/>.
    ///
    /// <para><b>Faction string syntax (Qud parity):</b></para>
    /// <list type="bullet">
    ///   <item><c>"Snapjaws"</c> — single faction, uses <see cref="Value"/></item>
    ///   <item><c>"Snapjaws,Bandits"</c> — comma-delimited, each uses <see cref="Value"/></item>
    ///   <item><c>"Snapjaws:10,Bandits:-3"</c> — per-faction colon override</item>
    ///   <item><c>"FactionA,FactionB:7"</c> — mixed; FactionA uses Value, FactionB uses 7</item>
    ///   <item><c>"*allvisiblefactions:N"</c> — wildcard, applies +N to EVERY known
    ///         faction (post-audit-fix Finding #2 — Qud parity). Semantic note:
    ///         "visible" in Qud means "in the player's awareness"; in CoO the
    ///         equivalent is "any faction in <c>PlayerReputation</c>'s dict"
    ///         since CoO doesn't yet have per-faction visibility tracking.</item>
    /// </list>
    ///
    /// <para><b>Deferred from Qud parity:</b></para>
    /// <list type="bullet">
    ///   <item><c>DeepCopy</c> reset of <c>AppliedBonus</c> — CoO has no in-game cloning yet</item>
    ///   <item><c>SuspendingEvent</c> / <c>OnDestroyObjectEvent</c> unapply hooks —
    ///         the leader-null branch in <see cref="CheckApplyBonus"/> catches
    ///         the leader-destroyed case; explicit unapply on self-destruction
    ///         would require additional hooks CoO doesn't have yet</item>
    /// </list>
    /// </summary>
    public class GrantsRepAsFollowerPart : Part
    {
        public override string Name => "GrantsRepAsFollower";

        /// <summary>
        /// Comma-delimited faction list. Each entry is either
        /// <c>"FactionName"</c> (uses <see cref="Value"/>) or
        /// <c>"FactionName:N"</c> (uses N as the delta, overriding
        /// <see cref="Value"/>). Whitespace-trimmed; empty entries
        /// are filtered.
        /// </summary>
        public string Faction = "";

        /// <summary>Default rep delta. Applied per-faction when the
        /// faction entry has no <c>:N</c> override. Negative values
        /// are allowed (an annoying companion that costs you rep).</summary>
        public int Value;

        /// <summary>True while the rep bonus is currently active.
        /// Idempotency gate — prevents double-apply / double-unapply.
        /// Round-trips via the SL.6-pinned reflection save (public field).</summary>
        public bool AppliedBonus;

        public GrantsRepAsFollowerPart() { }

        public GrantsRepAsFollowerPart(string faction, int value)
        {
            Faction = faction ?? "";
            Value = value;
        }

        /// <summary>
        /// Public dispatch entry. Called each turn-end (via
        /// <see cref="HandleEvent"/> on the <c>"EndTurn"</c> event) and
        /// directly from tests / external state-update callers.
        /// Idempotent — repeated calls with the same conditions produce
        /// the same end state.
        /// </summary>
        public void CheckApplyBonus(Entity leader)
        {
            var ownBrain = ParentEntity?.GetPart<BrainPart>();
            var ownZone = ownBrain?.CurrentZone;
            var leaderBrain = leader?.GetPart<BrainPart>();
            var leaderZone = leaderBrain?.CurrentZone;

            bool conditionsHold =
                leader != null
                && leader.HasTag("Player")
                && ownZone != null
                && leaderZone == ownZone;

            if (AppliedBonus && !conditionsHold)
                UnapplyBonus();
            else if (!AppliedBonus && conditionsHold)
                ApplyBonus();
        }

        /// <summary>
        /// Apply the rep delta to each faction listed in
        /// <see cref="Faction"/>. Idempotent — no-op if already applied
        /// or if <see cref="Faction"/> is empty/whitespace.
        /// </summary>
        private void ApplyBonus()
        {
            if (AppliedBonus) return;
            // Quick parse check: bail without flag-set if Faction is
            // entirely empty/whitespace. Avoids locking the Part into
            // "fake applied" state where future calls would short-circuit.
            if (!HasAnyApplicableEntry()) return;
            // Post-audit fix (Finding #8): set AppliedBonus = true
            // BEFORE applying deltas. If PlayerReputation.Modify throws
            // partway through the loop, the flag is already set, so
            // future CheckApplyBonus calls won't re-enter ApplyBonus
            // and double-apply the successful portion. UnapplyBonus
            // will reverse what's reversible on the next condition flip
            // (best-effort symmetric path; full transactional rollback
            // is heavier and out of scope).
            AppliedBonus = true;
            ApplyDelta(positive: true);
        }

        /// <summary>
        /// Pre-flight check: does <see cref="Faction"/> contain at least
        /// one entry that would produce an apply? Used by
        /// <see cref="ApplyBonus"/> to gate the eager-flag set —
        /// otherwise an empty string would lock us into AppliedBonus=true
        /// with no actual rep flow.
        /// </summary>
        private bool HasAnyApplicableEntry()
        {
            if (string.IsNullOrWhiteSpace(Faction)) return false;
            // *allvisiblefactions:N — always applicable if there are any factions tracked
            if (Faction.StartsWith("*allvisiblefactions:")) return PlayerReputation.GetAll().Count > 0;
            // Comma-delimited — at least one non-whitespace entry with a
            // non-empty faction name (after :N strip if present)
            string[] entries = Faction.Split(',');
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (string.IsNullOrEmpty(entry)) continue;
                int colonIdx = entry.IndexOf(':');
                string faction = colonIdx >= 0 ? entry.Substring(0, colonIdx).Trim() : entry;
                if (!string.IsNullOrEmpty(faction)) return true;
            }
            return false;
        }

        /// <summary>
        /// Reverse the rep delta. Idempotent — no-op if not applied.
        /// </summary>
        private void UnapplyBonus()
        {
            if (!AppliedBonus) return;
            AppliedBonus = false;
            ApplyDelta(positive: false);
        }

        /// <summary>
        /// Parse the comma-delimited <see cref="Faction"/> string and
        /// apply <see cref="PlayerReputation.Modify"/> for each entry.
        /// Per-entry <c>:N</c> overrides take precedence over the
        /// default <see cref="Value"/>. Negative deltas honored. Returns
        /// the number of entries successfully applied.
        /// </summary>
        private int ApplyDelta(bool positive)
        {
            if (string.IsNullOrEmpty(Faction)) return 0;

            // Post-audit fix (Finding #2 — Qud parity): handle the
            // "*allvisiblefactions:N" wildcard. Mirrors Qud's
            // GrantsRepAsFollower.cs:69-78. Applies the same delta to
            // every faction currently tracked by PlayerReputation.
            // CoO's "visible" semantic is "any faction the player has
            // interacted with" (any faction in the rep dict) — closest
            // approximation since CoO doesn't have per-faction
            // visibility tracking.
            if (Faction.StartsWith("*allvisiblefactions:"))
            {
                string suffix = Faction.Substring("*allvisiblefactions:".Length).Trim();
                if (!int.TryParse(suffix, out int wildcardAmount)) wildcardAmount = Value;
                int wcApplied = 0;
                var allFactions = PlayerReputation.GetAll();
                foreach (var kvp in allFactions)
                {
                    if (string.IsNullOrEmpty(kvp.Key)) continue;
                    PlayerReputation.Modify(kvp.Key, positive ? wildcardAmount : -wildcardAmount, silent: true);
                    wcApplied++;
                }
                return wcApplied;
            }

            string[] entries = Faction.Split(',');
            int applied = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (string.IsNullOrEmpty(entry)) continue;

                string faction = entry;
                int amount = Value;
                int colonIdx = entry.IndexOf(':');
                if (colonIdx >= 0)
                {
                    faction = entry.Substring(0, colonIdx).Trim();
                    string numStr = entry.Substring(colonIdx + 1).Trim();
                    if (!int.TryParse(numStr, out amount)) amount = Value;
                }

                if (string.IsNullOrEmpty(faction)) continue;
                PlayerReputation.Modify(faction, positive ? amount : -amount, silent: true);
                applied++;
            }
            return applied;
        }

        /// <summary>
        /// Hook the <c>"EndTurn"</c> event fired by
        /// <see cref="TurnManager.EndTurn"/> on every actor. Re-evaluates
        /// apply/unapply state per turn. Returns true to let other parts
        /// also see the event (we're a listener, not a consumer).
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e?.ID == "EndTurn")
            {
                var brain = ParentEntity?.GetPart<BrainPart>();
                CheckApplyBonus(brain?.PartyLeader);
            }
            return base.HandleEvent(e);
        }
    }
}
