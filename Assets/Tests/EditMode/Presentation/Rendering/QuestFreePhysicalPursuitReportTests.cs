#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class QuestFreePhysicalPursuitReportTests
    {
        static object Snapshot(Zone zone)
        {
            var method=typeof(QuestFreeSpreadStateNativePlayer).GetMethod("PhysicalLightingSnapshot",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method,"Missing scalar report projection for Unity Color");
            return method.Invoke(null,new object[]{zone});
        }
        [Serializable] sealed class Rgba { public float r,g,b,a; }
        [Serializable] sealed class Lighting { public float ambientLevel;public Rgba ambientTint; }
        [Serializable] sealed class Observation { public string phase;public Lighting lighting; }
        [Serializable] sealed class ReportData { public Observation[] observations; }
        static string Serialize(object value)
        {
            var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
            return (string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
        }
        static Lighting Report(object snapshot)
        {
            // Invoke the same real Newtonsoft serializer without changing the test assembly's references.
            string json=Serialize(new{observations=new[]{new{phase="controlled-arena-authorship",lighting=snapshot}}});
            Assert.False(json.Contains("linear")||json.Contains("gamma")||json.Contains("normalized"));
            var report=JsonUtility.FromJson<ReportData>(json);Assert.AreEqual(1,report.observations.Length);
            Assert.AreEqual("controlled-arena-authorship",report.observations[0].phase);
            return report.observations[0].lighting;
        }
        static void AssertLighting(Lighting lighting,float level,Color tint)
        {
            Assert.NotNull(lighting);Assert.NotNull(lighting.ambientTint);
            Assert.AreEqual(level,lighting.ambientLevel);
            Assert.AreEqual(tint.r,lighting.ambientTint.r);Assert.AreEqual(tint.g,lighting.ambientTint.g);
            Assert.AreEqual(tint.b,lighting.ambientTint.b);Assert.AreEqual(tint.a,lighting.ambientTint.a);
        }
        [Test]
        public void ActualColorRecursionIsReplacedByExactScalarReportChannels()
        {
            var zone=new Zone("physical-report-color"){AmbientLevel=.35f,AmbientTint=new Color(.12f,.34f,.56f,.78f)};
            var failure=Assert.Throws<TargetInvocationException>(()=>Serialize(new{lighting=new{zone.AmbientLevel,zone.AmbientTint}}));
            Assert.AreEqual("Newtonsoft.Json.JsonSerializationException",failure.InnerException.GetType().FullName,"Control retains the actual native recursive-Color premise.");
            AssertLighting(Report(Snapshot(zone)),zone.AmbientLevel,zone.AmbientTint);
        }
        [Test]
        public void CapturedLightingRemainsDetachedWhileFreshSnapshotTracksCurrentSource()
        {
            var zone=new Zone("physical-report-detached"){AmbientLevel=.6f,AmbientTint=new Color(.25f,.5f,.75f,1f)};
            var tint=zone.AmbientTint;float level=zone.AmbientLevel;object before=Snapshot(zone);
            Assert.AreEqual(level,zone.AmbientLevel);Assert.AreEqual(tint,zone.AmbientTint);
            zone.AmbientLevel=.15f;zone.AmbientTint=new Color(.9f,.7f,.4f,.2f);
            AssertLighting(Report(before),level,tint);AssertLighting(Report(Snapshot(zone)),zone.AmbientLevel,zone.AmbientTint);
            Assert.AreEqual(.15f,zone.AmbientLevel);Assert.AreEqual(new Color(.9f,.7f,.4f,.2f),zone.AmbientTint);
        }
    }
}
#endif
