using System;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The ending spine, ES.4 (Docs/ENDING-SPINE.md): the two enactments offered at
    /// the empty seventh position of the Felling-Site circle, as native world
    /// actions. <b>Be struck</b> (vessel-path Renewal) is always available and its
    /// cost is stated: the world re-binds around a chooser and cracks in a far
    /// generation. <b>Refuse aloud and name the world</b> (practice-path Renewal)
    /// needs a clean closure-ledger — every undertaken act closed or refused — and
    /// when the ledger is not clean it names what is still open instead of
    /// enacting anything. No ending is best; the distinction is mechanical.
    /// Consume and Preserve are advertised routes, not built here.
    /// </summary>
    public static class EndingSpine
    {
        public const string StrikeCommand = "EndingStrike", NameCommand = "EndingName";
        /// <summary>Player int property and narrative fact: 0 none, 1 vessel-path, 2 practice-path.</summary>
        public const string EndingProperty = "EndingEnacted", EndingFact = "Ending";
        public const int VesselPath = 1, PracticePath = 2;
        public const int EnactCost = 1000;

        public const string VesselEpilogue =
            "You stand in the seventh interval and let the Strike come. It does not install a god; it installs a chooser. " +
            "The world re-binds around a bearer; a new Tree grows; the sari... sari... stops, and green returns to the tepui's edges. " +
            "It holds for as long as you keep choosing it, every morning, for as long as you have mornings — and no one can choose it for you after. " +
            "Somewhere far down the generations the yes will wear into habit, and habit cannot hold Naming. That is the crack. It is not today.";
        public const string PracticeEpilogue =
            "You stand in the seventh interval and refuse it aloud, the way the first no was refused, and you say the world's name back to it instead. " +
            "No vessel. The empty seventh is answered by a practice: every guest named under the cloth, every friend named by a child, every grandmother's meadow, all of it doing the work, all the time, everywhere. " +
            "The pressure drains at its source. The costs are permanent and you will witness them: the Six begin to age; the work never finishes; " +
            "the Root, told it will never hold alone again, wakes without fear and dissolves into the new binding. The flowers last longer here. So do the funerals.";

        public static bool IsWorldCommand(string command) => command == StrikeCommand || command == NameCommand;
        public static int Enacted(Entity player) => player?.GetIntProperty(EndingProperty) ?? 0;

        /// <summary>Advertise both enactments on the seventh position while none has been enacted.</summary>
        public static void AddActions(InventoryActionList actions, Entity player)
        {
            if (actions == null || Enacted(player) != 0) return;
            actions.AddAction("EndingStrike", "be struck as the seventh: re-bind the world; it cracks one day", StrikeCommand, 's', 30);
            actions.AddAction("EndingName", "refuse aloud and name the world: a clean ledger; the gods end", NameCommand, 'n', 29);
        }

        public static bool TryWorldAction(Entity target, Entity actor, Zone zone, string command, out int energyCost)
        {
            energyCost = 0;
            if (!IsWorldCommand(command)) return false;
            string reason = Refusal(target, actor, zone, out var reading);
            if (reason == null && command == NameCommand && !reading.Clean) reason = "ledger_open";
            if (reason != null)
            {
                string tell = reason == "not_in_the_position" ? "Stand in the seventh position." : reason == "already_enacted" ? "It is done; there is no second time."
                    : reason == "ledger_open" ? "You cannot teach the world to finish its names while leaving your own unspoken: " + string.Join(" ", reading.Descriptions) : null;
                if (tell != null) MessageLog.Add(tell);
                Diag.Record("ending", "Rejected", actor: actor, target: target, payload: new { command, reason, open = reading?.Open ?? -1 });
                return false;
            }
            int path = command == StrikeCommand ? VesselPath : PracticePath;
            actor.SetIntProperty(EndingProperty, path);
            NarrativeStatePart.Current?.SetFact(EndingFact, path);
            var examine = target.GetPart<ExaminablePart>();
            if (examine != null) examine.Text = path == VesselPath
                ? "The seventh interval has a bearer. The near and far edges of the circle agree. Nothing here is empty any more; it is only held, and holding is a choice made every morning."
                : "The seventh interval is answered, not filled. The near and far edges of the circle agree without a bearer between them. The practice is everywhere else now.";
            var cell = zone.GetEntityCell(target); if (cell != null) ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, "EndingEnacted");
            string epilogue = path == VesselPath ? VesselEpilogue : PracticeEpilogue;
            NarrativeStatePart.Current?.LogEvent(path == VesselPath ? "Struck as the seventh: the world re-bound around a chooser." : "Refused aloud in the circle: the world named by practice.");
            MessageLog.AddAnnouncement(epilogue);
            Diag.Record("ending", "Enacted", actor: actor, target: target, payload: new { path = path == VesselPath ? "vessel" : "practice", closed = reading.Closed, refused = reading.Refused, open = reading.Open });
            energyCost = EnactCost;
            return true;
        }

        private static string Refusal(Entity target, Entity actor, Zone zone, out ClosureReading reading)
        {
            reading = null;
            if (target?.GetPart<SeventhPositionPart>() == null || zone == null || !FellingSceneRuntime.IsActive(zone)) return "not_the_circle";
            if (actor == null || !actor.HasTag("Player") || actor.GetStatValue("Hitpoints", 0) <= 0 || CombatSystem.IsDeathHandled(actor)) return "no_actor";
            if (Enacted(actor) != 0) return "already_enacted";
            var tc = zone.GetEntityCell(target); var ac = zone.GetEntityCell(actor);
            if (tc == null || ac == null || tc != ac) return "not_in_the_position";
            if (StoryletPart.Current == null) return "no_ledger";
            reading = StoryletPart.Current.ReadLedger(actor);
            return null;
        }
    }
}
