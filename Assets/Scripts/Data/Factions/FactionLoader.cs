using UnityEngine;

namespace CavesOfOoo.Data
{
    public static class FactionLoader
    {
        public static FactionFileData Load(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[FactionLoader] Empty JSON provided.");
                return new FactionFileData { Factions = new FactionEntry[0] };
            }

            var data = JsonUtility.FromJson<FactionFileData>(json);
            if (data == null || data.Factions == null)
            {
                Debug.LogWarning("[FactionLoader] Failed to parse faction data.");
                return new FactionFileData { Factions = new FactionEntry[0] };
            }

            Debug.Log($"[FactionLoader] Loaded {data.Factions.Length} factions.");
            return data;
        }
    }
}
