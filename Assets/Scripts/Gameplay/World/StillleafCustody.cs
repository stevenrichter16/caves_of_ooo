using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Stillleaf Archive, SA.4 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): custody
    /// of the register and its aftermath. Three incompatible testimonies, one
    /// enacted choice, each physical, persistent and one-time:
    /// <list type="bullet">
    /// <item><b>deliver</b> (the Searcher): the register is sealed into Quillhold's
    /// archive beside her; Recension standing rises; terms given to Curation, if
    /// any, are broken and cost standing there.</item>
    /// <item><b>file</b> (the Indexer): the register goes into the salt file beside
    /// its keeper, unread, and the file locks; Curation standing rises.</item>
    /// <item><b>reseal</b> (a world action at the vault door): with the keeper's key
    /// carried and the register inside, the door is locked again. Neither faction
    /// is paid; both can be told truthfully (<b>report</b>). Refusal is closure.</item>
    /// </list>
    /// </summary>
    public static class StillleafCustody
    {
        /// <summary>Player int property: 0 undecided, then one of the outcomes below.</summary>
        public const string Outcome = "StillleafOutcome";
        public const int OutcomeDelivered = 1, OutcomeFiled = 2, OutcomeResealed = 3;
        public const string ToldSearcher = "StillleafToldSearcher", ToldIndexer = "StillleafToldIndexer";
        public const string ResealCommand = "StillleafReseal";
        public const string RecensionFaction = "Palimpsest", CurationFaction = "PaleCuration";
        public const int RecensionForTheRecord = 10, CurationForTheRecord = 10, CurationBrokenAgreement = -10, CurationBrokenBargain = -4;
        public const int ResealCost = 1000;

        public static bool Owns(string command) => command == "deliver" || command == "file" || command == "report";
        public static bool IsWorldCommand(string command) => command == ResealCommand;

        public static Entity CarriedRegister(Entity player)
        {
            var inventory = player?.GetPart<InventoryPart>();
            if (inventory != null) foreach (var e in inventory.Objects)
                if (e.ID == StillleafArchive.RegisterId && e.GetPart<PhysicsPart>()?.InInventory == player) return e;
            return null;
        }
        private static bool HasKey(Entity player)
        {
            var inventory = player?.GetPart<InventoryPart>();
            if (inventory != null) foreach (var e in inventory.Objects)
                if (e.GetPart<KeyPart>()?.KeyId == SealedLibraryBuilder.KeyID) return true;
            return false;
        }
        private static Entity FindRegister(Zone zone)
        {
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == StillleafArchive.RegisterId) return e;
            return null;
        }

        // ── Conversation verbs ────────────────────────────────────────────

        public static bool CanConversation(Entity speaker, Entity player, string command)
        {
            bool searcher = StillleafArchiveContent.ValidConversation(speaker, player, StillleafArchiveContent.QuillholdZoneId, StillleafArchiveContent.SearcherId, StillleafArchiveContent.SearcherBlueprint, out var zone);
            bool indexer = !searcher && StillleafArchiveContent.ValidConversation(speaker, player, StillleafSaltVault.ZoneId, StillleafSaltVault.IndexerId, StillleafSaltVault.IndexerBlueprint, out zone);
            if (!searcher && !indexer) return false;
            int outcome = player.GetIntProperty(Outcome);
            switch (command)
            {
                case "deliver": return searcher && outcome == 0 && CarriedRegister(player) != null;
                case "file": return indexer && outcome == 0 && CarriedRegister(player) != null && StillleafSaltVault.FindCabinet(zone) != null;
                case "report":
                    if (outcome == 0) return false;
                    return searcher ? outcome != OutcomeDelivered && player.GetIntProperty(ToldSearcher) == 0
                                    : outcome != OutcomeFiled && player.GetIntProperty(ToldIndexer) == 0;
                default: return false;
            }
        }

        public static bool TryConversation(Entity speaker, Entity player, string command)
        {
            if (!CanConversation(speaker, player, command)) return Reject(speaker, player, command, "unavailable");
            var zone = SettlementRuntime.ActiveZone;
            var inventory = player.GetPart<InventoryPart>();
            int terms = player.GetIntProperty(StillleafSaltVault.Terms);
            if (command == "deliver")
            {
                var register = CarriedRegister(player);
                var seat = zone.GetEntityCell(speaker); if (seat == null) return Reject(speaker, player, command, "no_place");
                if (!inventory.RemoveObject(register)) return Reject(speaker, player, command, "transfer_failed");
                if (!zone.AddEntity(register, seat.X, seat.Y)) { inventory.AddObject(register); return Reject(speaker, player, command, "placement_failed"); }
                register.GetPart<PhysicsPart>().Takeable = false;
                register.GetPart<RenderPart>().DisplayName = "the Stillleaf register (sealed)";
                var examine = register.GetPart<ExaminablePart>();
                if (examine != null) examine.Text = "The Stillleaf register, sealed into Quillhold's archive the hour it arrived, by the Mainline's hand. Hollin read the shelf-marks once before the seal went on. What the entries said, nobody here will say.";
                ZoneRenderHooks.MarkCellDirty(seat.X, seat.Y, "StillleafDelivered");
                Conclude(player, OutcomeDelivered);
                PlayerReputation.Modify(RecensionFaction, RecensionForTheRecord);
                if (terms == StillleafSaltVault.TermsAgreed)
                { PlayerReputation.Modify(CurationFaction, CurationBrokenAgreement); MessageLog.Add("You agreed to bring the register to the Salt-Vault to be filed. Curation's file will say otherwise."); }
                else if (terms == StillleafSaltVault.TermsBargained)
                { PlayerReputation.Modify(CurationFaction, CurationBrokenBargain); MessageLog.Add("You told the Indexer custody was not promised. Curation's file will say you kept your word to the letter, and only to the letter."); }
                MessageLog.Add("Hollin: 'Sealed within the hour; the Mainline will see to that, and I will let them. But it is real, and it is here, and no one will have to search for it again.'");
            }
            else if (command == "file")
            {
                var register = CarriedRegister(player);
                var cabinet = StillleafSaltVault.FindCabinet(zone); var container = cabinet.GetPart<ContainerPart>();
                container.MaxItems = Math.Max(container.MaxItems, container.Contents.Count + 1);
                if (!inventory.RemoveObject(register)) return Reject(speaker, player, command, "transfer_failed");
                if (!container.AddItem(register)) { inventory.AddObject(register); return Reject(speaker, player, command, "file_refused"); }
                container.Locked = true;
                var examine = register.GetPart<ExaminablePart>();
                if (examine != null) examine.Text = "The Stillleaf register, filed beside its keeper's line, unread. Status: continuing.";
                var drawer = cabinet.GetPart<ExaminablePart>();
                if (drawer != null) drawer.Text = "A salt-sealed drawer beside the Indexer's desk. Its lead-line reads \"Keeping is not the same as showing.\" The register that answers to it is filed inside, unread, beside its keeper.";
                var c = zone.GetEntityCell(cabinet); if (c != null) ZoneRenderHooks.MarkCellDirty(c.X, c.Y, "StillleafFiled");
                Conclude(player, OutcomeFiled);
                PlayerReputation.Modify(CurationFaction, CurationForTheRecord);
                MessageLog.Add("Halm: 'Entered: one register, filed beside its keeper, unread. Status: continuing.'");
            }
            else
            {
                bool searcher = speaker.ID == StillleafArchiveContent.SearcherId;
                player.SetIntProperty(searcher ? ToldSearcher : ToldIndexer, 1);
                MessageLog.Add(ReportLine(searcher, player.GetIntProperty(Outcome), terms));
            }
            Diag.Record("quest", "StillleafArchiveApplied", actor: player, target: speaker, payload: new { command, questId = StillleafArchiveContent.QuestId, outcome = player.GetIntProperty(Outcome) });
            return true;
        }

        private static string ReportLine(bool searcher, int outcome, int terms)
        {
            if (searcher)
                return outcome == OutcomeFiled
                    ? "Hollin: 'Filed beside its keeper, unread. Then the shelf-marks were true and the library is real, and that is more than the Searchers had yesterday. I said I would pay for the record. I will not pay for its absence, and I will not pretend you owed it to me.'"
                    : "Hollin: 'Sealed again, by you. Then it exists, and it waits, and anyone with the words can find it. Keeping is not the same as losing. Go well.'";
            if (outcome == OutcomeResealed)
                return "Halm: 'Entered: register sealed in place with its keeper's key. Filed under: correct, by other means. The Curation does not object to a thing staying where it was kept.'";
            if (terms == StillleafSaltVault.TermsAgreed)
                return "Halm: 'Entered: register delivered to Quillhold, against terms agreed. The file will say so for as long as there are files.'";
            if (terms == StillleafSaltVault.TermsBargained)
                return "Halm: 'Entered: register delivered to Quillhold; custody not promised. Correctly filed. I would have preferred to be wrong about you.'";
            return "Halm: 'Entered: register at Quillhold. Nothing was promised; nothing is owed. The keeper's file stays open.'";
        }

        private static void Conclude(Entity player, int outcome)
        {
            player.SetIntProperty(Outcome, outcome);
            var current = StoryletPart.Current;
            if (current != null && current.IsQuestActive(StillleafArchiveContent.QuestId))
            {
                current.FinishObjective(StillleafArchiveContent.QuestId, "choose", player);
                current.CompleteQuest(StillleafArchiveContent.QuestId, player);
            }
        }

        // ── The reseal, a world action at the vault door ──────────────────

        public static bool TryWorldAction(Entity target, Entity actor, Zone zone, string command, out int energyCost)
        {
            energyCost = 0;
            if (!IsWorldCommand(command)) return false;
            string reason = ResealRefusal(target, actor, zone);
            if (reason != null)
            {
                string tell = reason == "no_key" ? "The seal needs the keeper's key." : reason == "carrying_register" ? "You are carrying the register; the vault would be sealed empty."
                    : reason == "register_not_inside" || reason == "no_register_in_vault" ? "The register is not inside the vault." : reason == "doorway_occupied" ? "Someone stands in the doorway."
                    : reason == "not_beside_the_door" ? "Stand outside, beside the door." : reason == "custody_decided" ? "The register's custody was already decided." : null;
                if (tell != null) MessageLog.Add(tell);
                Diag.Record("quest", "StillleafArchiveRejected", actor: actor, target: target, payload: new { command, reason });
                return false;
            }
            target.GetPart<LockPart>().IsLocked = true;
            var physics = target.GetPart<PhysicsPart>(); if (physics != null) physics.Solid = true;
            var cell = zone.GetEntityCell(target); ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, "StillleafResealed");
            Conclude(actor, OutcomeResealed);
            MessageLog.Add("You turn the keeper's key back the way it came. The seal takes. The register stays where it was kept, and the door remembers how to be a wall.");
            Diag.Record("quest", "StillleafArchiveApplied", actor: actor, target: target, payload: new { command, questId = StillleafArchiveContent.QuestId, outcome = OutcomeResealed });
            energyCost = ResealCost;
            return true;
        }

        private static string ResealRefusal(Entity door, Entity actor, Zone zone)
        {
            if (zone?.ZoneID != SealedLibraryBuilder.ZoneID || door?.GetPart<LockPart>()?.KeyId != SealedLibraryBuilder.KeyID || door.GetPart<SealedLibraryBarrierPart>() == null) return "not_the_vault_door";
            if (actor == null || !actor.HasTag("Player") || actor.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(actor)) return "no_actor";
            if (actor.GetIntProperty(Outcome) != 0) return "custody_decided";
            if (door.GetPart<LockPart>().IsLocked) return "already_sealed";
            var dc = zone.GetEntityCell(door); var ac = zone.GetEntityCell(actor);
            if (dc == null || ac == null) return "no_place";
            if (ac == dc || Math.Max(Math.Abs(dc.X - ac.X), Math.Abs(dc.Y - ac.Y)) != 1) return "not_beside_the_door";
            foreach (var o in dc.Objects) if (o.HasTag("Creature")) return "doorway_occupied";
            if (!HasKey(actor)) return "no_key";
            if (CarriedRegister(actor) != null) return "carrying_register";
            var register = FindRegister(zone); if (register == null) return "no_register_in_vault";
            var rc = zone.GetEntityCell(register); if (rc == null) return "no_register_in_vault";
            if (!Inside(zone, rc, dc, ac)) return "register_not_inside";
            return null;
        }

        /// <summary>The register is inside iff, with the door cell treated as
        /// solid, the flood from its cell (creatures ignored) never reaches the
        /// actor standing outside.</summary>
        private static bool Inside(Zone zone, Cell from, Cell door, Cell outside)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var q = new Queue<Cell>(); seen[from.X, from.Y] = true; q.Enqueue(from);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == outside) return false;
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    var n = zone.GetCell(c.X + dx, c.Y + dy);
                    if (n == null || seen[n.X, n.Y] || n == door) continue;
                    bool open = true;
                    foreach (var o in n.Objects) if (!o.HasTag("Creature") && (o.GetPart<PhysicsPart>()?.Solid == true || o.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)) { open = false; break; }
                    if (!open) continue;
                    seen[n.X, n.Y] = true; q.Enqueue(n);
                }
            }
            return true;
        }

        private static bool Reject(Entity speaker, Entity player, string command, string reason)
        { Diag.Record("quest", "StillleafArchiveRejected", actor: player, target: speaker, payload: new { command, reason }); return false; }
    }
}
