using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static message log for combat and game messages.
    /// Tests inspect GetLast(). Runtime wires OnMessage to Debug.Log.
    /// </summary>
    public static class MessageLog
    {
        private static readonly List<string> Messages = new List<string>();
        private static readonly List<int> Ticks = new List<int>();
        private static readonly Queue<string> Announcements = new Queue<string>();

        /// <summary>
        /// Callback fired when a new message is added.
        /// Wire this to Debug.Log in GameBootstrap for console output.
        /// </summary>
        public static Action<string> OnMessage;

        /// <summary>
        /// Source of the current game tick — wired at bootstrap so the log can
        /// stamp each message with the turn/tick it occurred on. Returns 0
        /// if unwired (e.g. in tests).
        /// </summary>
        public static Func<int> TickProvider;

        /// <summary>
        /// Stamp that increments every time an announcement (critical event)
        /// is posted. UI consumers compare against a cached value to detect
        /// new announcements and flash accordingly.
        /// </summary>
        public static int FlashStamp;

        public static void Add(string message)
        {
            Messages.Add(message);
            Ticks.Add(TickProvider != null ? TickProvider() : 0);
            OnMessage?.Invoke(message);
        }

        /// <summary>
        /// Adds a message to both the regular log and the announcement queue.
        /// Announcements are displayed as modal popups by AnnouncementUI.
        /// </summary>
        public static void AddAnnouncement(string message)
        {
            Add(message);
            Announcements.Enqueue(message);
            FlashStamp++;
        }

        public static bool HasPendingAnnouncement => Announcements.Count > 0;

        public static string ConsumeAnnouncement()
        {
            return Announcements.Count > 0 ? Announcements.Dequeue() : null;
        }

        public static string GetLast()
        {
            if (Messages.Count == 0) return null;
            return Messages[Messages.Count - 1];
        }

        public static List<string> GetRecent(int count)
        {
            int start = Math.Max(0, Messages.Count - count);
            return Messages.GetRange(start, Messages.Count - start);
        }

        /// <summary>
        /// Returns ticks parallel to GetRecent(count). Ticks[i] is the game
        /// tick at which Messages[i] was added (0 if TickProvider unwired).
        /// </summary>
        public static List<int> GetRecentTicks(int count)
        {
            int start = Math.Max(0, Ticks.Count - count);
            return Ticks.GetRange(start, Ticks.Count - start);
        }

        public static void Clear()
        {
            Messages.Clear();
            Ticks.Clear();
            Announcements.Clear();
            FlashStamp = 0;
        }

        public static List<string> GetMessages()
        {
            return new List<string>(Messages);
        }

        public static int Count => Messages.Count;
    }
}