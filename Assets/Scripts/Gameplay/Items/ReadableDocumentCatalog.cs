using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Cached canonical reading copies. Loading and inspection do not
    /// change campaign knowledge, consume RNG or access repository source files.</summary>
    public static class ReadableDocumentCatalog
    {
        public sealed class Document
        {
            public string Id { get; }
            public string Blueprint { get; }
            public string Title { get; }
            public string Text { get; }
            public string Source { get; }
            internal Document(Row row)
            { Id = row.Id; Blueprint = row.Blueprint; Title = row.Title; Text = row.Text; Source = row.Source; }
        }

        [Serializable] internal sealed class Row
        { public string Id, Blueprint, Title, Text, Source; }
        [Serializable] private sealed class FileData { public Row[] Documents; }
        private static Dictionary<string, Document> byId;
        private static IReadOnlyList<Document> documents;
        private static List<string> issues;

        /// <summary>Validated artifacts in authored order; malformed rows are
        /// omitted and explained by Validate. Entries themselves are immutable.</summary>
        public static IReadOnlyList<Document> All { get { EnsureLoaded(); return documents; } }

        public static Document Get(string id)
        {
            EnsureLoaded();
            return id != null && byId.TryGetValue(id, out var document) ? document : null;
        }

        /// <summary>Returns a copy of catalog errors without altering the loaded data.</summary>
        public static IReadOnlyList<string> Validate()
        { EnsureLoaded(); return issues.ToArray(); }

        private static void EnsureLoaded()
        {
            if (byId != null) return;
            var asset = Resources.Load<TextAsset>("Content/Data/Documents/CodexDocuments");
            Load(asset != null ? asset.text : null);
        }

        // Internal seam for malformed-content controls without changing Resources.
        internal static void Load(string json)
        {
            byId = new Dictionary<string, Document>(StringComparer.Ordinal);
            var valid = new List<Document>();
            issues = new List<string>();
            var blueprints = new HashSet<string>(StringComparer.Ordinal);
            FileData data = null;
            try { if (!string.IsNullOrWhiteSpace(json)) data = JsonUtility.FromJson<FileData>(json); }
            catch (Exception) { issues.Add("Document catalog is not valid JSON."); }
            if (data?.Documents == null || data.Documents.Length == 0)
                issues.Add("Document catalog has no document rows.");
            else foreach (var row in data.Documents)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Id)
                    || string.IsNullOrWhiteSpace(row.Blueprint) || string.IsNullOrWhiteSpace(row.Title)
                    || string.IsNullOrWhiteSpace(row.Text) || string.IsNullOrWhiteSpace(row.Source))
                { issues.Add("Document row is missing an ID, blueprint, title, text or source."); continue; }
                if (byId.ContainsKey(row.Id) || blueprints.Contains(row.Blueprint))
                { issues.Add("Duplicate document ID or blueprint: " + row.Id); continue; }
                var document = new Document(row);
                byId.Add(row.Id, document); blueprints.Add(row.Blueprint); valid.Add(document);
            }
            documents = valid.AsReadOnly();
        }

        public static void ResetForTests() { byId = null; documents = null; issues = null; }
    }
}
