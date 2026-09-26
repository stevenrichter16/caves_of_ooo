using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Repair the known literal player subject at the beginning of a
    /// one-line system message. This is not an English inflector or lore editor.</summary>
    public static class PlayerMessageGrammar
    {
        private static readonly Dictionary<string, string> Verbs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "is", "are" }, { "was", "were" }, { "has", "have" }, { "does", "do" },
            { "picks", "pick" }, { "drops", "drop" }, { "equips", "equip" }, { "unequips", "unequip" },
            { "throws", "throw" }, { "misses", "miss" }, { "hits", "hit" }, { "attacks", "attack" },
            { "dies", "die" }, { "heals", "heal" }, { "takes", "take" }, { "feels", "feel" },
            { "resists", "resist" }, { "recovers", "recover" }, { "collapses", "collapse" },
            { "shakes", "shake" }, { "stumbles", "stumble" }, { "moves", "move" },
            { "drinks", "drink" }, { "eats", "eat" }, { "brews", "brew" }, { "crafts", "craft" },
            { "disassembles", "disassemble" }, { "finishes", "finish" }, { "separates", "separate" },
            { "plants", "plant" }, { "springs", "spring" }, { "steps", "step" }, { "brushes", "brush" },
            { "learns", "learn" }, { "gains", "gain" }, { "loses", "lose" }, { "becomes", "become" }
        };

        public static string Normalize(string message)
        {
            if (string.IsNullOrEmpty(message) || message.IndexOf('\n') >= 0 || message.IndexOf('\r') >= 0) return message;
            if (message.StartsWith("you's ", StringComparison.OrdinalIgnoreCase)
                || message.StartsWith("you’s ", StringComparison.OrdinalIgnoreCase))
                return "Your " + message.Substring(6);
            if (!message.StartsWith("you ", StringComparison.OrdinalIgnoreCase)) return message;
            int end = 4;
            while (end < message.Length && char.IsLetter(message[end])) end++;
            if (end > 4 && Verbs.TryGetValue(message.Substring(4, end - 4), out string verb))
                return "You " + verb + message.Substring(end);
            return message[0] == 'Y' ? message : "You " + message.Substring(4);
        }
    }
}
