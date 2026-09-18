using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpawnRing3DNativeReportTests
    {
        [Serializable] sealed class Envelope { public SpawnRing3DNativeAudit.ProfileReport profile; }
        static bool Validate(SpawnRing3DNativeAudit.ProfileReport profile)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.SpawnRing3DNativeAuditBatch")).First(t=>t!=null);
            var method=type.GetMethod("ValidateRequestedProfile",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method);
            return (bool)method.Invoke(null,new object[]{profile,"1aaad6c4ac50415c9b4cb43b642c37cf","unused-private-root"});
        }
        [Test] public void UnrequestedProfile_AcceptsActualUnityNullSerialization()
        {
            Assert.IsTrue(Validate(null));
            var json=JsonUtility.ToJson(new Envelope());
            var read=JsonUtility.FromJson<Envelope>(json);
            Assert.NotNull(read.profile,"Pin Unity's actual nested null-to-default-object behavior.");
            Assert.IsTrue(Validate(read.profile),"Ordinary acceptance without profiling must survive its actual JSON roundtrip.");
        }
        [TestCase("mode")][TestCase("run")][TestCase("complete")][TestCase("frames")]
        [TestCase("raw")][TestCase("failure")][TestCase("phase")][TestCase("seconds")]
        public void UnrequestedProfile_RejectsAnyObservedProfileWork(string change)
        {
            Assert.IsTrue(Validate(null),"Ordinary absent workload is the positive control.");
            var p=new SpawnRing3DNativeAudit.ProfileReport();
            switch(change)
            {
                case "mode":p.mode="full";break;
                case "run":p.runId=Guid.NewGuid().ToString("N");break;
                case "complete":p.complete=true;break;
                case "frames":p.frames=1;break;
                case "raw":p.rawFrames="a-real-profile.csv.gz";break;
                case "failure":p.failure="interrupted";break;
                case "phase":p.phases=new[]{new SpawnRing3DNativeAudit.ProfilePhase()};break;
                case "seconds":p.steadySeconds=1;break;
            }
            Assert.IsFalse(Validate(JsonUtility.FromJson<SpawnRing3DNativeAudit.ProfileReport>(JsonUtility.ToJson(p))));
        }
    }
}
