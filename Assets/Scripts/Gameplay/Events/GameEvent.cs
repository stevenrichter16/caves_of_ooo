using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Event passed between Parts via Entity.FireEvent().
    /// Mirrors Qud's Event class: string ID with typed parameter dictionaries.
    /// Parts can set "Handled" to stop propagation or modify parameters to communicate results.
    /// </summary>
    public class GameEvent
    {
        // Registered event IDs for fast comparison
        private static readonly Dictionary<string, int> RegisteredIDs = new Dictionary<string, int>();
        private static int _nextID = 1;

        public string ID;
        public bool Handled;

        // Typed parameter stores — avoids boxing for common types
        public Dictionary<string, object> Parameters;
        public Dictionary<string, string> StringParameters;
        public Dictionary<string, int> IntParameters;

        public GameEvent(string id)
        {
            ID = id;
            Parameters = new Dictionary<string, object>();
            StringParameters = new Dictionary<string, string>();
            IntParameters = new Dictionary<string, int>();
        }

        // --- Static ID Registration ---

        public static int GetID(string name)
        {
            if (!RegisteredIDs.TryGetValue(name, out int id))
            {
                id = _nextID++;
                RegisteredIDs[name] = id;
            }
            return id;
        }

        // --- Factory Methods ---

        public static GameEvent New(string id)
        {
            return new GameEvent(id);
        }

        public static GameEvent New(string id, string name1, object value1)
        {
            var e = new GameEvent(id);
            e.SetParameter(name1, value1);
            return e;
        }

        public static GameEvent New(string id, string name1, string value1)
        {
            var e = new GameEvent(id);
            e.SetParameter(name1, value1);
            return e;
        }

        public static GameEvent New(string id, string name1, int value1)
        {
            var e = new GameEvent(id);
            e.SetParameter(name1, value1);
            return e;
        }

        public static GameEvent New(string id, string name1, object value1, string name2, object value2)
        {
            var e = new GameEvent(id);
            e.SetParameter(name1, value1);
            e.SetParameter(name2, value2);
            return e;
        }

        // --- Parameter Setters ---

        public GameEvent SetParameter(string name, object value)
        {
            Parameters[name] = value;
            return this;
        }

        public GameEvent SetParameter(string name, string value)
        {
            StringParameters[name] = value;
            return this;
        }

        public GameEvent SetParameter(string name, int value)
        {
            IntParameters[name] = value;
            return this;
        }

        // --- Parameter Getters ---

        public int GetIntParameter(string name, int defaultValue = 0)
        {
            if (IntParameters.TryGetValue(name, out int value))
                return value;
            if (Parameters.TryGetValue(name, out object obj))
                return Convert.ToInt32(obj);
            if (StringParameters.TryGetValue(name, out string str))
                return Convert.ToInt32(str);
            return defaultValue;
        }

        public string GetStringParameter(string name, string defaultValue = null)
        {
            if (StringParameters.TryGetValue(name, out string value))
                return value;
            if (Parameters.TryGetValue(name, out object obj))
                return obj?.ToString();
            if (IntParameters.TryGetValue(name, out int i))
                return i.ToString();
            return defaultValue;
        }

        public T GetParameter<T>(string name)
        {
            if (Parameters.TryGetValue(name, out object value) && value is T typed)
                return typed;
            return default;
        }

        public object GetParameter(string name)
        {
            if (Parameters.TryGetValue(name, out object value))
                return value;
            if (StringParameters.TryGetValue(name, out string str))
                return str;
            if (IntParameters.TryGetValue(name, out int i))
                return i;
            return null;
        }

        public bool HasParameter(string name)
        {
            return Parameters.ContainsKey(name)
                || StringParameters.ContainsKey(name)
                || IntParameters.ContainsKey(name);
        }
    }
}
