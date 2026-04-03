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
        private static readonly Queue<string> Announcements = new Queue<string>();

        /// <summary>
        /// Callback fired when a new message is added.
        /// Wire this to Debug.Log in GameBootstrap for console output.
        /// </summary>
        public static Action<string> OnMessage;

        public static void Add(string message)
        {
            Messages.Add(message);
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

        public static void Clear()
        {
            Messages.Clear();
            Announcements.Clear();
        }

        public static List<string> GetMessages()
        {
            return new List<string>(Messages);
        }

        public static int Count => Messages.Count;
    }
}