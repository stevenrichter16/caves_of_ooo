using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Stillleaf Archive, SA.2 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): the
    /// Recension Searcher at Quillhold and the errand she offers.
    ///
    /// <para>Hollin Vesk is installed once, on fresh generation of the Quillhold
    /// surface, on the bare archive floor nearest the stacks; a latch on the
    /// origin ground outlives her, so she is never re-conjured. Her errand
    /// starts the chain's journal entry and gives the player the keeper's
    /// last words — the only index that can find the keeper's file at the
    /// Salt-Vault (SA.3, <see cref="StillleafSaltVault"/>, whose verbs share
    /// this class's action and predicate names). Release is closure, not failure: the journal entry
    /// is removed, nothing is marked failed, and what was heard stays heard.</para>
    /// </summary>
    public static class StillleafArchiveContent
    {
        public const string QuestId = "StillleafArchive";
        public const string QuillholdZoneId = QuillholdCompositionPlan.ZoneID;
        public const string SearcherBlueprint = "StillleafSearcher", SearcherId = "stillleaf-archive:searcher";
        public const string ConversationId = "StillleafSearcher_1";
        /// <summary>The keeper's last words: Pale Curation's index key for the file.</summary>
        public const string LastWords = "Keeping is not the same as showing.";
        /// <summary>Player int property: 1 once the words have been given.</summary>
        public const string WordsKnown = "StillleafLastWordsKnown";
        private const string Installed = "StillleafSearcherInstalled";

        public static void EnsureRegistered()
        {
            ConversationActions.EnsureInitialized(); ConversationPredicates.EnsureInitialized();
            ConversationActions.RegisterRequired("StillleafArchive", (speaker, player, command) =>
                TryConversation(speaker, player, command) ? null : "stillleaf_archive_unavailable");
            ConversationPredicates.Register("IfStillleafArchiveCan", CanConversation);
            // Bootstrap loads every file under these folders; detached contexts
            // (tests, saves loaded before bootstrap) load the chain's own files.
            if (ConversationLoader.Get(ConversationId) == null)
            { var a = Resources.Load<TextAsset>("Content/Conversations/StillleafArchive"); if (a != null) ConversationLoader.LoadFromJson(a.text, a.name); }
            if (StoryletRegistry.FindQuest(QuestId) == null)
            { var a = Resources.Load<TextAsset>("Content/Data/Storylets/StillleafArchive"); if (a != null) StoryletRegistry.LoadFromJson(a.text, a.name); }
        }

        /// <summary>Called only by fresh generation of the Quillhold surface.
        /// Seats the Searcher on the bare interior floor nearest the archive
        /// shelves; refuses (with a diag reason) on any missing dependency.</summary>
        public static bool TryInstallSearcher(Zone zone, EntityFactory factory)
        {
            if (zone?.ZoneID != QuillholdZoneId || factory == null) return Refuse(zone, "not_quillhold");
            var anchor = Anchor(zone); if (anchor == null) return Refuse(zone, "no_anchor");
            if (anchor.GetIntProperty(Installed) == 1) return Refuse(zone, "already_installed");
            if (!factory.Blueprints.ContainsKey(SearcherBlueprint)) return Refuse(zone, "missing_searcher_blueprint");
            long sx = 0, sy = 0; int shelves = 0;
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.BlueprintName == "QuillholdArchiveShelf") { var p = zone.GetEntityPosition(e); sx += p.x; sy += p.y; shelves++; }
            if (shelves == 0) return Refuse(zone, "no_stacks");
            int cx = (int)(sx / shelves), cy = (int)(sy / shelves);
            Cell seat = null; int best = int.MaxValue;
            // Row-major scan: ties resolve by Y then X, deterministically.
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                var c = zone.GetCell(x, y);
                if (c == null || !c.IsInterior || c.BlocksMovement() || zone.GenReservedCells.Contains((x, y))) continue;
                bool bare = true;
                foreach (var o in c.Objects) if (o.BlueprintName != "StoneFloor") { bare = false; break; }
                if (!bare) continue;
                int d = Math.Abs(x - cx) + Math.Abs(y - cy);
                if (d < best) { best = d; seat = c; }
            }
            if (seat == null) return Refuse(zone, "no_free_archive_floor");
            EnsureRegistered();
            var searcher = factory.CreateEntity(SearcherBlueprint);
            if (searcher?.GetPart<RenderPart>() == null || searcher.GetPart<ConversationPart>() == null
                || searcher.GetPart<BrainPart>() == null || searcher.GetPart<StillleafResidentPart>() == null
                || searcher.GetStatValue("Hitpoints") <= 0) return Refuse(zone, "searcher_incomplete");
            searcher.ID = SearcherId;
            if (!zone.AddEntity(searcher, seat.X, seat.Y)) return Refuse(zone, "placement_failed");
            anchor.SetIntProperty(Installed, 1);
            Diag.Record("worldgen", "StillleafSearcherPlaced", target: searcher,
                payload: new { zone = zone.ZoneID, x = seat.X, y = seat.Y, fromStacks = best });
            return true;
        }

        private static Entity Anchor(Zone zone)
        {
            var origin = zone.GetCell(0, 0); if (origin == null) return null;
            foreach (var e in origin.Objects) if (e.GetPart<PhysicsPart>()?.Takeable == false) return e;
            return null;
        }

        /// <summary>Shared gate for every resident of the chain: the active zone
        /// is the manager's current copy of the expected zone, the speaker is
        /// the expected resident by id and blueprint, both are alive, not
        /// hostile, and adjacent. Mirrors MorrowfastExpedition's gate.</summary>
        internal static bool ValidConversation(Entity speaker, Entity player, string zoneId, string speakerId, string speakerBlueprint, out Zone zone)
        {
            zone = SettlementRuntime.ActiveZone;
            if (zone?.ZoneID != zoneId || WorldLocationContext.For(zone) == null
                || player == null || player != StoryletPart.LocalPlayer || !player.HasTag("Player")
                || player.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(player)
                || speaker == null || speaker.ID != speakerId || speaker.BlueprintName != speakerBlueprint
                || speaker.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(speaker)
                || FactionManager.IsHostile(speaker, player) || StoryletPart.Current == null) return false;
            var manager = WorldLocationContext.For(zone);
            if (!manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || !ReferenceEquals(current, zone)) return false;
            var pc = zone.GetEntityCell(player); var sc = zone.GetEntityCell(speaker);
            return pc != null && sc != null && pc.Objects.Contains(player) && sc.Objects.Contains(speaker)
                && Math.Max(Math.Abs(pc.X - sc.X), Math.Abs(pc.Y - sc.Y)) <= 1;
        }

        public static bool CanConversation(Entity speaker, Entity player, string command)
        {
            if (StillleafCustody.Owns(command)) return StillleafCustody.CanConversation(speaker, player, command);
            if (StillleafSaltVault.Owns(command)) return StillleafSaltVault.CanConversation(speaker, player, command);
            if (!ValidConversation(speaker, player, QuillholdZoneId, SearcherId, SearcherBlueprint, out _)) return false;
            bool active = StoryletPart.Current.IsQuestActive(QuestId);
            switch (command)
            {
                case "accept": return !active && !StoryletPart.Current.IsQuestCompleted(QuestId);
                case "release": return active;
                default: return false;
            }
        }

        public static bool TryConversation(Entity speaker, Entity player, string command)
        {
            if (StillleafCustody.Owns(command)) return StillleafCustody.TryConversation(speaker, player, command);
            if (StillleafSaltVault.Owns(command)) return StillleafSaltVault.TryConversation(speaker, player, command);
            if (!CanConversation(speaker, player, command)) return Reject(speaker, player, command, "unavailable");
            if (command == "accept")
            {
                StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0, EnteredStageAtTurn = TurnManager.Active?.TickCount ?? 0 });
                StoryletPart.Current.SetGiver(QuestId, speaker, SettlementRuntime.ActiveZone);
                player.SetIntProperty(WordsKnown, 1);
                // SA.5: work done before the errand was taken counts, in order
                // (mirrors Morrowfast's already-recovered parcel), so the journal
                // never opens at a stage whose objectives can no longer fire.
                var journal = StoryletPart.Current;
                if (player.GetIntProperty(StillleafSaltVault.FileFound) == 1) journal.FinishObjective(QuestId, "file", player);
                if (StillleafCustody.HasKey(player)) journal.FinishObjective(QuestId, "key", player);
                if (StillleafCustody.CarriedRegister(player) != null) journal.FinishObjective(QuestId, "register", player);
                MessageLog.Add("Hollin: 'The keeper of Stillleaf was preserved at the Salt-Vault, six chunks south and one east of here. Pale Curation files its dead by their last words, and the keeper's were these: \"" + LastWords + "\" Say them to an Indexer and the file opens; the key should be filed beside the keeper. Whatever Curation asks for it, hear them out. I want the register found. I have not said what should happen to it after.' [Q] keeps these directions.");
            }
            else
            {
                StoryletPart.Current.RefuseQuest(QuestId, player);
                MessageLog.Add("Hollin: 'Then it stays lost a while longer. That is not a debt; the Searchers have waited longer than you have been alive. What I told you is yours to keep.'");
            }
            Diag.Record("quest", "StillleafArchiveApplied", actor: player, target: speaker, payload: new { command, questId = QuestId });
            return true;
        }

        private static bool Reject(Entity speaker, Entity player, string command, string reason)
        { Diag.Record("quest", "StillleafArchiveRejected", actor: player, target: speaker, payload: new { command, reason }); return false; }

        private static bool Refuse(Zone zone, string reason)
        { Diag.Record("worldgen", "StillleafSearcherRefused", payload: new { zone = zone?.ZoneID, reason }); return false; }
    }
}
