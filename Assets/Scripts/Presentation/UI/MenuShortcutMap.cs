using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>UI-local letter bindings. Reserved navigation/close keys never label rows.
    /// Positional indices are absolute; exhausted rows remain selectable by navigation.
    /// Authored action data is preserved, with all usable preferences reserved before fallback.</summary>
    internal static class MenuShortcutMap
    {
        private const string NavigationAlphabet = "abcdefghilmnopqrstuvwxyz";
        private const string PickupAlphabet = "abcdefhilmnopqrstuvwxyz";

        internal static char Positional(int index, bool closesWithG = false)
        {
            string alphabet = closesWithG ? PickupAlphabet : NavigationAlphabet;
            return index >= 0 && index < alphabet.Length ? alphabet[index] : '\0';
        }

        internal static KeyCode Key(char letter)
            => letter >= 'a' && letter <= 'z' ? (KeyCode)((int)KeyCode.A + letter - 'a') : KeyCode.None;

        internal static char[] ForActions(IReadOnlyList<InventoryAction> actions)
        {
            if (actions == null || actions.Count == 0) return System.Array.Empty<char>();
            var result = new char[actions.Count];
            var used = new bool[26];
            for (int i = 0; i < actions.Count; i++)
            {
                if (!CanBind(actions[i])) continue;
                char key = char.ToLowerInvariant(actions[i].Key);
                if (NavigationAlphabet.IndexOf(key) < 0 || used[key - 'a']) continue;
                result[i] = key;
                used[key - 'a'] = true;
            }
            int next = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                if (result[i] != '\0' || !CanBind(actions[i])) continue;
                while (next < NavigationAlphabet.Length && used[NavigationAlphabet[next] - 'a']) next++;
                if (next == NavigationAlphabet.Length) break;
                result[i] = NavigationAlphabet[next++];
                used[result[i] - 'a'] = true;
            }
            return result;
        }

        private static bool CanBind(InventoryAction action)
            => action != null && !string.IsNullOrEmpty(action.Command) && action.Command != "CraftNoop";
    }
}
