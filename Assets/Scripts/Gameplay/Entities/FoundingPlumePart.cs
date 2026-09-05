using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original meeting through the ordinary rest primitive.
    /// Only a trusted player lying on this actual plume can enter the dream.
    /// This cold interaction adds no per-turn scan or separate scheduler.</summary>
    public sealed class FoundingPlumePart : Part
    {
        public override string Name => "FoundingPlume";
        public const string SleepCommand = "SleepOnFoundingPlume";
        public const string MeetingFact = "RootedMet";
        public const string BloomExpiryProperty = "FoundingBloomUntilTick";
        public const int BloomTicks = 14 * WorldClock.DayLengthTicks;

        /// <summary>Two world weeks of recognizable patch-bloom, measured
        /// against the saved world clock. No background effect or tick hook.</summary>
        public static bool HasBloom(Entity actor) => actor != null && TurnManager.Active != null
            && actor.GetIntProperty(BloomExpiryProperty) > TurnManager.Active.TickCount;
        private static readonly int ActionsID = GameEvent.GetID("GetInventoryActions");
        private static readonly int ActionID = GameEvent.GetID("InventoryAction");
        public override bool WantEvent(int eventID) => eventID == ActionsID || eventID == ActionID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
                e.GetParameter<InventoryActionList>("Actions")?.AddAction("FoundingSleep", "lie on the plume and sleep", SleepCommand, 's', 10);
            else if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == SleepCommand)
                TrySleep(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone"));
            return true;
        }

        private void TrySleep(Entity actor, Zone zone)
        {
            string reason = null;
            var cell = zone?.GetEntityCell(ParentEntity);
            var turns = TurnManager.Active;
            var state = NarrativeStatePart.Current;
            if (actor == null || !actor.HasTag("Player")) reason = "player_required";
            else if (actor.GetStatValue("Hitpoints") <= 0) reason = "living_player_required";
            else if (cell == null || zone.GetEntityCell(actor) != cell) reason = "must_lie_on_plume";
            else if (PlayerReputation.Get("CatacombFolk") < PlayerReputation.LIKED_THRESHOLD) reason = "trust_required";
            else if (state == null || turns == null || turns.CurrentActor != actor || !turns.WaitingForInput) reason = "inactive_session";
            if (reason != null)
            {
                MessageLog.Add(reason == "trust_required"
                    ? "The Listening elders have not yet trusted you with the founding plume. Speak with the founding-wall tender."
                    : "You must stand on the living plume before you can lie down in it.");
                if (Diag.IsChannelEnabled("furniture")) Diag.Record("furniture", "FoundingSleepRefused", actor, ParentEntity, new { reason });
                return;
            }
            if (!RestSystem.TryRest(actor, zone, "founding plume", out _)) return;
            actor.SetIntProperty(BloomExpiryProperty, (int)System.Math.Min(int.MaxValue, (long)turns.TickCount + BloomTicks));
            bool first = state.GetFact(MeetingFact) == 0;
            if (first)
            {
                state.SetFact(MeetingFact, 1); state.LogEvent(MeetingFact);
                MessageLog.Add("You wake remembering fragments: warmth beneath stone, the weight of a thousand sleeping homes, arms that have never tired. You cannot recall the words. What remains is contentment. The Rooted has spoken with you in dream.");
            }
            else MessageLog.Add("Again you wake with a little of his unhurried contentment. The remembered words slip away.");
            MessageLog.Add("A faint scent of patch-bloom clings to you. The Listening elders will recognize it for weeks.");
            if (Diag.IsChannelEnabled("furniture")) Diag.Record("furniture", "FoundingDream", actor, ParentEntity, new { first, clock = turns.TickCount });
        }
    }
}
