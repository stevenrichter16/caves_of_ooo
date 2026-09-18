#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class NativeSpellFxGpuReportTests
    {
        private static readonly string[] Spells = { "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Hydromancy_JetBlast",
            "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm", "Hydromancy_ConjureRain" };
        [Serializable] private sealed class Row
        {
            public string id, error;
            public bool passed = true;
            public int positivePixels = 500, negativePixels;
            public int colorSamples=500,doubleLinearDifferentPixels=400;
            public float meanNativeReferenceDifference,meanDoubleLinearDifference=.1f;
            public string[] framePaths = { "positive.png", "negative.png", "counter.png" };
        }
        [Serializable] private sealed class Receipt
        {
            public string status = "PASS", runId = "fresh-run", sourceSha256 = new string('a', 64), error;
            public string graphicsApi = "Metal", startedUtc = "2026-09-11T01:00:00Z", finishedUtc = "2026-09-11T01:00:01Z";
            public bool activeScenePreserved = true, sceneDirtyFlagsPreserved = true,
                renderTextureActiveRestored = true, asyncCompilationRestored = true, settingsRestored = true;
            public string[] unexpectedLogs = Array.Empty<string>();
            public Row[] cases;
        }

        [TestCase("valid", true)] [TestCase("stale-run", false)] [TestCase("stale-source", false)]
        [TestCase("failed", false)] [TestCase("missing-case", false)] [TestCase("duplicate-case", false)]
        [TestCase("blank-positive", false)] [TestCase("visible-backface", false)] [TestCase("memory-leak", false)]
        [TestCase("color-mismatch",false)] [TestCase("indistinguishable-color-counter",false)] [TestCase("nonfinite-color",false)]
        [TestCase("null-gpu", false)] [TestCase("dirty-scene", false)] [TestCase("unexpected-error", false)]
        public void AcceptanceRequiresFreshCompleteGpuEvidenceAndPositiveControls(string mutation, bool expected)
        {
            var rows = Spells.SelectMany(s => new[] { new Row { id = s + ":face" }, new Row { id = s + ":assembled" }, new Row { id = s + ":color" } }).ToArray();
            var receipt = new Receipt { cases = rows };
            switch (mutation)
            {
                case "color-mismatch": rows[2].meanNativeReferenceDifference=.1f;break;
                case "indistinguishable-color-counter":rows[2].meanDoubleLinearDifference=0;rows[2].doubleLinearDifferentPixels=0;break;
                case "nonfinite-color":rows[2].meanDoubleLinearDifference=float.PositiveInfinity;break;
                case "stale-run": receipt.runId = "old-run"; break;
                case "stale-source": receipt.sourceSha256 = new string('b', 64); break;
                case "failed": receipt.status = "FAIL"; break;
                case "missing-case": receipt.cases = rows.Take(13).ToArray(); break;
                case "duplicate-case": rows[13].id = rows[0].id; break;
                case "blank-positive": rows[0].positivePixels = 0; break;
                case "visible-backface": rows[0].negativePixels = 40; break;
                case "memory-leak": rows[1].negativePixels = 40; break;
                case "null-gpu": receipt.graphicsApi = "Null"; break;
                case "dirty-scene": receipt.sceneDirtyFlagsPreserved = false; break;
                case "unexpected-error": receipt.unexpectedLogs = new[] { "GPU error" }; break;
            }
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.NativeSpellFxGpuProbe")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "An isolated actual-GPU acceptance probe must validate fresh, complete evidence.");
            var method = type.GetMethod("ValidateReport", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method);
            Assert.AreEqual(expected, method.Invoke(null, new object[] { JsonUtility.ToJson(receipt), "fresh-run", new string('a', 64) }));
        }
    }
}
#endif
