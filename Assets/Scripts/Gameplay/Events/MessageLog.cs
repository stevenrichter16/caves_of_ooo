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
        // Kept parallel to the historical text queue; no save format changes.
        // Public Restore/Clear intentionally discard transient physical origins.
        private readonly struct AnnouncementOrigin
        {
            internal readonly string Text;internal readonly DocumentReadAudio Reading;internal readonly AnnouncementDocument Presentation;
            internal AnnouncementOrigin(string text,DocumentReadAudio reading,AnnouncementDocument presentation=null){Text=text;Reading=reading;Presentation=presentation;}
        }
        private static readonly Queue<AnnouncementOrigin> AnnouncementOrigins=new Queue<AnnouncementOrigin>();
        private static int NextSerial;
        /// <summary>In-memory change stamp for readers; not serialized.</summary>
        public static long Revision { get; private set; }
        /// <summary>Changes on Clear/Restore, even when count and serials match.</summary>
        public static long HistoryGeneration { get; private set; }
        public const int MaximumHistoryRange = 64;

        /// <summary>Copies a detached chronological range into a reusable list.
        /// Negative starts clamp to zero; nonpositive counts read nothing.
        /// A single read is capped at64 entries, with overflow-safe bounds.</summary>
        public static int CopyEntryRange(int start, int count, List<Entry> output)
        {
            if (output == null) return 0;
            output.Clear();
            if (count <= 0) return 0;
            start = Math.Max(0, Math.Min(start, Messages.Count));
            int length = Math.Min(Math.Min(count, MaximumHistoryRange), Messages.Count - start);
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                output.Add(new Entry(Messages[index], index < Ticks.Count ? Ticks[index] : 0,
                    index < Serials.Count ? Serials[index] : index));
            }
            return length;
        }


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

        public static void Add(string message) => AddRaw(PlayerMessageGrammar.Normalize(message));

        /// <summary>Report an entity's local effect without revealing an unseen
        /// NPC. This gates text only; simulation and diagnostics remain native.</summary>
        public static void AddObserved(Entity owner, string message)
        {
            if (owner == null) return;
            if (owner.HasTag("Player")) { Add(message); return; }
            var zone = owner.SpatialZone;
            var cell = zone?.GetEntityCell(owner);
            if (cell?.ParentZone == zone && cell?.IsVisible == true && cell.Objects.Contains(owner)
                && owner.GetPart<RenderPart>()?.Visible != false)
                Add(message);
        }

        private static void AddRaw(string message)
        {
            Messages.Add(message);
            Ticks.Add(TickProvider != null ? TickProvider() : 0);
            Serials.Add(NextSerial++);
            unchecked { Revision++; }
            OnMessage?.Invoke(message);
        }

        /// <summary>
        /// Adds a message to both the regular log and the announcement queue.
        /// Announcements are displayed as modal popups by AnnouncementUI.
        /// </summary>
        public static void AddAnnouncement(string message)=>AddAnnouncement(message,null);
        internal static void AddAnnouncement(string message,DocumentReadAudio reading)=>EnqueueAnnouncement(message,reading,null);
        /// <summary>Retains exact authored log text while attaching an optional transient reader snapshot.</summary>
        public static void AddPresentedAnnouncement(string message,AnnouncementDocument presentation)
            =>EnqueueAnnouncement(message,null,presentation);
        private static void EnqueueAnnouncement(string message,DocumentReadAudio reading,AnnouncementDocument presentation)
        {
            // Announcements include authored lore and quoted documents. Preserve
            // their exact bytes in both the log and the popup queue.
            AddRaw(message);
            // Historical test/diagnostic snapshots can directly restore the
            // private string queue. Any mismatch loses audio authority safely.
            if(AnnouncementOrigins.Count!=Announcements.Count)
            {AnnouncementOrigins.Clear();foreach(var text in Announcements)AnnouncementOrigins.Enqueue(new AnnouncementOrigin(text,null));}
            Announcements.Enqueue(message);
            AnnouncementOrigins.Enqueue(new AnnouncementOrigin(message,reading,presentation));
            FlashStamp++;
        }

        public static bool HasPendingAnnouncement => Announcements.Count > 0;

        public static string ConsumeAnnouncement()=>ConsumeAnnouncement(out _);
        internal static string ConsumeAnnouncement(out DocumentReadAudio reading)
            =>ConsumeAnnouncementWithPresentation(out reading,out _);
        internal static string ConsumeAnnouncementWithPresentation(out DocumentReadAudio reading,out AnnouncementDocument presentation)
        {
            reading=null;presentation=null;int before=Announcements.Count;
            if(before==0){AnnouncementOrigins.Clear();return null;}
            string text=Announcements.Dequeue();
            if(AnnouncementOrigins.Count==before)
            {var origin=AnnouncementOrigins.Dequeue();if(ReferenceEquals(text,origin.Text)){reading=origin.Reading;presentation=origin.Presentation;}else AnnouncementOrigins.Clear();}
            else AnnouncementOrigins.Clear();
            return text;
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

        /// <summary>
        /// Non-allocating overload: fills <paramref name="output"/> with the
        /// most recent <paramref name="count"/> entries instead of allocating
        /// a fresh list. The output list is cleared first so callers can
        /// reuse a static scratch buffer (see
        /// <c>SidebarStateBuilder._getRecentEntriesScratch</c>) — useful for
        /// per-frame paths like the sidebar log refresh.
        ///
        /// <para>Tier-B Fix #4 in PERF-COMBAT-INVESTIGATION.md. The
        /// allocating overload above is kept for tests / external
        /// consumers that capture the list reference.</para>
        /// </summary>
        public static void GetRecentEntries(int count, List<Entry> output)
        {
            if (output == null) return;
            output.Clear();

            int start = Math.Max(0, Messages.Count - count);
            int length = Messages.Count - start;
            for (int i = 0; i < length; i++)
            {
                int idx = start + i;
                int tick = idx < Ticks.Count ? Ticks[idx] : 0;
                int serial = idx < Serials.Count ? Serials[idx] : idx;
                output.Add(new Entry(Messages[idx], tick, serial));
            }
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
            AnnouncementOrigins.Clear();

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
            unchecked { Revision++; HistoryGeneration++; }
        }

        public static int NextSerialValue => NextSerial;

        public static void Clear()
        {
            Messages.Clear();
            Ticks.Clear();
            Serials.Clear();
            Announcements.Clear();
            AnnouncementOrigins.Clear();
            FlashStamp = 0;
            NextSerial = 0;
            unchecked { Revision++; HistoryGeneration++; }
        }

        public static List<string> GetMessages()
        {
            return new List<string>(Messages);
        }

        public static int Count => Messages.Count;
    }
}
