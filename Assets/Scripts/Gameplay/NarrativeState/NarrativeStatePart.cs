using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Singleton part on the world entity. Holds the global fact store
    /// (int-quality bag), an append-only narrative event log, and the
    /// INarrativeReactor dispatch registry.
    ///
    /// Implements ISaveSerializable because WritePublicFields can't handle
    /// Dictionary or List serialization correctly.
    ///
    /// Reactor dispatch is polled: TickEnd fires on the world entity after
    /// each EndTurn; this part receives it via HandleEvent and fans out to
    /// all registered INarrativeReactor implementations.
    /// </summary>
    public sealed class NarrativeStatePart : Part, ISaveSerializable
    {
        public override string Name => "NarrativeState";

        /// <summary>
        /// The active NarrativeStatePart for the current game session.
        /// Set by GameBootstrap on fresh boot and on load. Null outside of play.
        /// </summary>
        public static NarrativeStatePart Current;

        private static readonly int TickEndEventID = GameEvent.GetID("TickEnd");

        private readonly FactBag _facts = new FactBag();
        private readonly List<string> _eventLog = new List<string>();

        // Reactor registry — uses a List not a HashSet to preserve insertion order.
        // Duplicate-guard is done manually in RegisterReactor.
        private readonly List<INarrativeReactor> _reactors = new List<INarrativeReactor>();

        public IReadOnlyList<string> EventLog => _eventLog;

        // --- Fact API ---

        public int GetFact(string key) => _facts.Get(key);
        public void SetFact(string key, int value) => _facts.Set(key, value);
        public void AddFact(string key, int delta) => _facts.Add(key, delta);
        public void ClearFact(string key) => _facts.Clear(key);

        // --- Event log ---

        public void LogEvent(string entry) => _eventLog.Add(entry);

        // --- Reactor registry ---

        public void RegisterReactor(INarrativeReactor reactor)
        {
            if (reactor != null && !_reactors.Contains(reactor))
                _reactors.Add(reactor);
        }

        public void UnregisterReactor(INarrativeReactor reactor)
        {
            _reactors.Remove(reactor);
        }

        // --- Part event hooks ---

        public override bool WantEvent(int eventID) => eventID == TickEndEventID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TickEnd")
                DispatchTickEnd();
            return true;
        }

        private void DispatchTickEnd()
        {
            for (int i = 0; i < _reactors.Count; i++)
                _reactors[i].OnTickEnd(this);
        }

        // --- ISaveSerializable ---

        public void Save(SaveWriter writer)
        {
            _facts.Save(writer);

            writer.Write(_eventLog.Count);
            for (int i = 0; i < _eventLog.Count; i++)
                writer.WriteString(_eventLog[i]);
        }

        public void Load(SaveReader reader)
        {
            _facts.Load(reader);

            _eventLog.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
                _eventLog.Add(reader.ReadString());
        }
    }
}
