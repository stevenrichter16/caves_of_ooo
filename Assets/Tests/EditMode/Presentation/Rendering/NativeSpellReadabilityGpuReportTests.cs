#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Metadata rejection counters are separate from actual GPU execution.
    /// Missing report validation and independent emission-preserving references are
    /// deliberate assertion REDs before the new probe or helper is implemented.</summary>
    public sealed class NativeSpellReadabilityGpuReportTests
    {
        static readonly string[] Common = { "self-light-dark", "split-fog", "memory", "unseen", "bounds", "missing-fog", "wrong-dimensions", "opaque-occlusion" };
        static string[] Ids => new[] { "Luminous", "SoftGlow" }.SelectMany(s => Common.Select(c => s + ":" + c))
            .Concat(new[] { "SoftGlow:vertex-gradient", "SoftGlow:alpha-zero", "SoftGlow:no-depth-write" }).ToArray();
        [Serializable] sealed class Row
        {
            public string id, error;
            public bool passed = true;
            public int positivePixels = 200, negativePixels, counterPixels = 160;
            public float centerMean = .7f, middleMean = .35f, edgeMean = .04f;
            public string[] framePaths = { "positive.png", "negative.png", "counter.png" };
        }
        [Serializable] sealed class Receipt
        {
            public string status = "PASS", runId = "readability-fresh", sourceSha256 = new string('a', 64), error;
            public string graphicsApi = "Metal", startedUtc = "2026-09-11T14:00:00Z", finishedUtc = "2026-09-11T14:00:01Z";
            public int width = 256, height = 256;
            public bool activeScenePreserved = true, sceneDirtyFlagsPreserved = true,
                renderTextureActiveRestored = true, asyncCompilationRestored = true, ownedObjectsDisposed = true;
            public string[] unexpectedLogs = Array.Empty<string>();
            public Row[] cases = Ids.Select(id => new Row { id = id }).ToArray();
        }

        [TestCase("valid", true)] [TestCase("stale-run", false)] [TestCase("stale-source", false)]
        [TestCase("failed", false)] [TestCase("missing-case", false)] [TestCase("duplicate-case", false)]
        [TestCase("blank-positive", false)] [TestCase("fog-leak", false)] [TestCase("out-of-bounds-leak", false)]
        [TestCase("indistinguishable-counter", false)] [TestCase("flat-gradient", false)] [TestCase("nonfinite-gradient", false)]
        [TestCase("depth-write", false)] [TestCase("missing-frame", false)] [TestCase("null-gpu", false)]
        [TestCase("dirty-scene", false)] [TestCase("borrowed-rt", false)] [TestCase("owned-leak", false)]
        [TestCase("unexpected-error", false)] [TestCase("backward-time", false)]
        public void ReadabilityReceiptRequiresCompletePairedPhysicalGpuEvidence(string mutation, bool expected)
        {
            var receipt = new Receipt(); var gradient = receipt.cases.Single(r => r.id == "SoftGlow:vertex-gradient");
            switch (mutation)
            {
                case "stale-run": receipt.runId = "older"; break;
                case "stale-source": receipt.sourceSha256 = new string('b', 64); break;
                case "failed": receipt.status = "FAIL"; break;
                case "missing-case": receipt.cases = receipt.cases.Take(18).ToArray(); break;
                case "duplicate-case": receipt.cases[1].id = receipt.cases[0].id; break;
                case "blank-positive": receipt.cases[0].positivePixels = 0; break;
                case "fog-leak": receipt.cases.Single(r => r.id == "SoftGlow:split-fog").negativePixels = 3; break;
                case "out-of-bounds-leak": receipt.cases.Single(r => r.id == "Luminous:bounds").negativePixels = 1; break;
                case "indistinguishable-counter": gradient.counterPixels = 0; break;
                case "flat-gradient": gradient.centerMean = gradient.middleMean = gradient.edgeMean = .7f; break;
                case "nonfinite-gradient": gradient.edgeMean = float.NaN; break;
                case "depth-write": receipt.cases.Single(r => r.id == "SoftGlow:no-depth-write").negativePixels = 50; break;
                case "missing-frame": receipt.cases[0].framePaths = new[] { "" }; break;
                case "null-gpu": receipt.graphicsApi = "Null"; break;
                case "dirty-scene": receipt.sceneDirtyFlagsPreserved = false; break;
                case "borrowed-rt": receipt.renderTextureActiveRestored = false; break;
                case "owned-leak": receipt.ownedObjectsDisposed = false; break;
                case "unexpected-error": receipt.unexpectedLogs = new[] { "render error" }; break;
                case "backward-time": receipt.finishedUtc = "2026-09-11T13:00:00Z"; break;
            }
            var method = Probe("CavesOfOoo.Editor.NativeSpellReadabilityGpuProbe").GetMethod("ValidateReport", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "The real readability GPU probe must reject missing/stale/vacuous evidence.");
            Assert.AreEqual(expected, method.Invoke(null, new object[] { JsonUtility.ToJson(receipt), "readability-fresh", new string('a', 64) }));
        }

        [TestCase(0f)] [TestCase(.63f)] [TestCase(1f)]
        public void ExistingColorReferenceKeepsActualEmissionInAnIndependentRawVectorBlock(float emission)
        {
            var method = Probe("CavesOfOoo.Editor.NativeSpellFxGpuProbe").GetMethod("CreateColorReferenceBlock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Fresh independent color reference must preserve actual emission rather than darkening only its control.");
            var original = new MaterialPropertyBlock(); original.SetColor("_BaseColor", new Color(.6f, .7f, .8f, 1)); original.SetFloat("_Emission", emission);
            Vector4 priorVector = original.GetVector("_BaseColor"), linear = new Vector4(.12f, .3f, .68f, 1);
            var fresh = (MaterialPropertyBlock)method.Invoke(null, new object[] { linear, original });
            Assert.AreNotSame(original, fresh); Assert.AreEqual(linear, fresh.GetVector("_BaseColor"));
            Assert.AreEqual(emission, fresh.GetFloat("_Emission")); Assert.AreEqual(priorVector, original.GetVector("_BaseColor"));
            Assert.AreEqual(emission, original.GetFloat("_Emission"));
        }
        static Type Probe(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Missing isolated GPU verifier: " + name); return type;
        }
    }
}
#endif
