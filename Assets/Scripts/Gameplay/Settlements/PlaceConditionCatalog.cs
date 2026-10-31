using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// C4 (Docs/PLACE-WORK-AND-PROGRESSION.md, Consequences): what a place's settlement
    /// condition does to the place (Content/Data/PlaceConditions.json). A condition is
    /// set by concern outcomes (SetPlaceCondition), by repairs (ConcordContract.&lt;site&gt;)
    /// or by any other system through SettlementManager.SetCondition; while it holds, its
    /// entry here moves the place's prices, keeps a resident stocked, gives residents and
    /// neighbouring places a line, and brings arrivals. Applied by
    /// <see cref="PlaceConsequences"/>.
    /// </summary>
    public static class PlaceConditionCatalog
    {
        public const string ResourcePath = "Content/Data/PlaceConditions";
        public const double MinPrice = 0.5, MaxPrice = 2.0;

        [Serializable] public class FileData { public List<PlaceConditionData> Conditions = new List<PlaceConditionData>(); }

        static List<PlaceConditionData> conditions;

        public static IReadOnlyList<PlaceConditionData> All { get { EnsureLoaded(); return conditions; } }

        static void EnsureLoaded()
        {
            if (conditions != null) return;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            LoadFromJson(asset != null ? asset.text : null);
        }

        public static void LoadFromJson(string json)
        {
            conditions = new List<PlaceConditionData>();
            if (string.IsNullOrWhiteSpace(json)) return;
            var file = JsonUtility.FromJson<FileData>(json);
            if (file?.Conditions == null) return;
            foreach (var c in file.Conditions)
                if (c != null && !string.IsNullOrEmpty(c.Place) && !string.IsNullOrEmpty(c.Condition)) conditions.Add(c);
        }

        public static void Reset() => conditions = null;

        public static List<string> Lint(Data.EntityFactory factory)
        {
            var problems = new List<string>();
            bool Known(string bp) => factory == null || (!string.IsNullOrEmpty(bp) && factory.Blueprints.ContainsKey(bp));
            foreach (var c in All)
            {
                string who = c.Place + ":" + c.Condition;
                if (!PlaceExists(c.Place)) problems.Add(who + ": unknown place '" + c.Place + "'");
                if (c.Price != 0 && (c.Price < MinPrice || c.Price > MaxPrice)) problems.Add(who + ": price " + c.Price + " outside " + MinPrice + "-" + MaxPrice);
                if (c.HeardIn != null)
                {
                    foreach (var p in c.HeardIn) if (!PlaceExists(p)) problems.Add(who + ": heard in unknown place '" + p + "'");
                    if (c.HeardIn.Count > 0 && string.IsNullOrWhiteSpace(c.Heard)) problems.Add(who + ": heard elsewhere but says nothing");
                }
                if (c.Stock != null)
                    foreach (var s in c.Stock)
                    {
                        if (!Known(s?.Owner)) problems.Add(who + ": unknown stock owner '" + s?.Owner + "'");
                        if (!Known(s?.Blueprint)) problems.Add(who + ": unknown stock item '" + s?.Blueprint + "'");
                        if ((s?.Count ?? 0) < 1) problems.Add(who + ": stock count");
                    }
                if (c.Arrivals != null)
                    foreach (var a in c.Arrivals)
                    {
                        if (!Known(a?.Blueprint)) problems.Add(who + ": unknown arrival '" + a?.Blueprint + "'");
                        if ((a?.Count ?? 0) < 1) problems.Add(who + ": arrival count");
                    }
            }
            return problems;
        }

        static bool PlaceExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == name) return true;
            return false;
        }
    }

    [Serializable]
    public class PlaceConditionData
    {
        public string Place;
        public string Condition;
        /// <summary>Multiplies what the place's traders charge (and divides what they pay).
        /// 0 or 1 leaves prices alone.</summary>
        public double Price;
        /// <summary>What residents of the place say while it holds.</summary>
        public List<string> Remarks = new List<string>();
        /// <summary>Other places that have heard, and what their residents say.</summary>
        public List<string> HeardIn = new List<string>();
        public string Heard;
        public List<PlaceStockData> Stock = new List<PlaceStockData>();
        public List<PlaceArrivalData> Arrivals = new List<PlaceArrivalData>();
    }

    [Serializable]
    public class PlaceStockData
    {
        /// <summary>Blueprint of the resident who keeps it.</summary>
        public string Owner;
        public string Blueprint;
        /// <summary>Kept topped up to this many, once per restock interval.</summary>
        public int Count = 1;
    }

    [Serializable]
    public class PlaceArrivalData
    {
        public string Blueprint;
        public int Count = 1;
    }
}
