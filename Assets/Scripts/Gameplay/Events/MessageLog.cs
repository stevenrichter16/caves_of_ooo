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
        public struct Entry
        {
            public readonly string Text;
            public readonly int Tick;
            public readonly int Serial;

            public Entry(string text, int tick, int serial)
            {
                Text = text ?? string.Empty;
                Tick = tick;
                Serial = serial;
            }
        }

        private static readonly List<string> Messages = new List<string>();
        private static readonly List<int> Ticks = new List<int>();
        private static readonly List<int> Serials = new List<int>();
        private static readonly Queue<string> Announcements = new Queue<string>();
        private static int NextSerial;

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
            Serials.Add(NextSerial++);
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

        public static List<Entry> GetRecentEntries(int count)
        {
            int start = Math.Max(0, Messages.Count - count);
            int length = Messages.Count - start;
            var entries = new List<Entry>(length);
            for (int i = 0; i < length; i++)
            {
                int idx = start + i;
                int tick = idx < Ticks.Count ? Ticks[idx] : 0;
                int serial = idx < Serials.Count ? Serials[idx] : idx;
                entries.Add(new Entry(Messages[idx], tick, serial));
            }

            return entries;
        }

        public static List<Entry> GetAllEntries()
        {
            return GetRecentEntries(Messages.Count);
        }

        public static List<string> GetPendingAnnouncementsSnapshot()
        {
            return new List<string>(Announcements);
        }

        public static void Restore(
            List<Entry> entries,
            List<string> pendingAnnouncements,
            int flashStamp,
            int nextSerial)
        {
            Messages.Clear();
            Ticks.Clear();
            Serials.Clear();
            Announcements.Clear();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Messages.Add(entries[i].Text);
                    Ticks.Add(entries[i].Tick);
                    Serials.Add(entries[i].Serial);
                }
            }

            if (pendingAnnouncements != null)
            {
                for (int i = 0; i < pendingAnnouncements.Count; i++)
                    Announcements.Enqueue(pendingAnnouncements[i]);
            }

            FlashStamp = flashStamp;
            NextSerial = nextSerial;
        }

        public static int NextSerialValue => NextSerial;

        public static void Clear()
        {
            Messages.Clear();
            Ticks.Clear();
            Serials.Clear();
            Announcements.Clear();
            FlashStamp = 0;
            NextSerial = 0;
        }

        public static List<string> GetMessages()
        {
            return new List<string>(Messages);
        }

        public static int Count => Messages.Count;
    }
}
