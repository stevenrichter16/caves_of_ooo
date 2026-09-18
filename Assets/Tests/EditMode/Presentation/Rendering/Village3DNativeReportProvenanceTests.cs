using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Pure editor verifier contract. No Play launch, files, saves,
    /// preferences or SessionState are changed. Each refusal has a successful
    /// identical-current-run control before the one altered condition.</summary>
    public sealed class Village3DNativeReportProvenanceTests
    {
        [Serializable] private sealed class Report
        {
            public string runId="1aaad6c4ac50415c9b4cb43b642c37cf",mode="before",saveRoot;
            public int failures,unexpectedErrors;
            public bool displayPreferencesRestored;
            public bool workloadComplete=true,shutdownObserved=true,shutdownRootHeld=true,shutdownSavingUnregistered=true;
        }
        private static bool Validate(string json,string mode,string root)
        {
            Type batch=null;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {batch=assembly.GetType("CavesOfOoo.Editor.Village3DNativeAuditBatch");if(batch!=null)break;}
            Assert.NotNull(batch,"Existing editor native audit launcher must be loaded.");
            var method=batch.GetMethod("ValidateFinalReportForRun",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.NotNull(method,"Add the pure current-run report verifier after this missing-helper RED.");
            return (bool)method.Invoke(null,new object[]{json,mode,root});
        }
        [Test] public void AfterReportRequiresCompletedPreferenceRestorationReceipt()
        {
            string root=Path.Combine(Path.GetTempPath(),"coo-native-save-audits","d8ca736b860948a79c6db884c0aee00f");
            var report=new Report { saveRoot=root, mode="after", displayPreferencesRestored=true };
            Assert.IsTrue(Validate(JsonUtility.ToJson(report),"after",root));
            report.displayPreferencesRestored=false;
            string incomplete=JsonUtility.ToJson(report);
            Assert.IsFalse(Validate(incomplete,"after",root),"An incomplete preference teardown cannot accept AFTER.");
            const string field=",\"displayPreferencesRestored\":false";
            StringAssert.Contains(field,incomplete);
            Assert.IsFalse(Validate(incomplete.Replace(field,""),"after",root),"An absent receipt cannot accept AFTER.");
        }
        [Test] public void HistoricalBeforeReportDoesNotRequireAfterOnlyPreferenceReceipt()
        {
            string root=Path.Combine(Path.GetTempPath(),"coo-native-save-audits","d8ca736b860948a79c6db884c0aee00f");
            var report=new Report { saveRoot=root };
            string json=JsonUtility.ToJson(report);
            const string field=",\"displayPreferencesRestored\":false";
            StringAssert.Contains(field,json);
            Assert.IsTrue(Validate(json,"before",root));
            Assert.IsTrue(Validate(json.Replace(field,""),"before",root));
        }
        [TestCase("previous-private-root")]
        [TestCase("wrong-mode")]
        [TestCase("invalid-run-id")]
        [TestCase("failed-case")]
        [TestCase("unexpected-error")]
        [TestCase("incomplete-workload")]
        [TestCase("incomplete-shutdown")]
        [TestCase("malformed-json")]
        public void PriorOrIncompleteReportCannotVerifyThisNativeRun(string mutation)
        {
            string root=Path.Combine(Path.GetTempPath(),"coo-native-save-audits","d8ca736b860948a79c6db884c0aee00f");
            var report=new Report{saveRoot=root};
            Assert.IsTrue(Validate(JsonUtility.ToJson(report),"before",root),"Complete current-run report is the positive control.");
            switch(mutation)
            {
                case "previous-private-root":report.saveRoot=Path.Combine(Path.GetTempPath(),"coo-native-save-audits","9c3db94482194b51a73a34cce5e637a7");break;
                case "wrong-mode":report.mode="after";break;
                case "invalid-run-id":
                    foreach(var id in new[]{"", "not-a-guid", Guid.Empty.ToString("N")})
                    {report.runId=id;Assert.IsFalse(Validate(JsonUtility.ToJson(report),"before",root),id);}return;
                case "failed-case":report.failures=1;break;
                case "unexpected-error":report.unexpectedErrors=1;break;
                case "incomplete-workload":report.workloadComplete=false;break;
                case "incomplete-shutdown":
                    report.shutdownObserved=false;Assert.IsFalse(Validate(JsonUtility.ToJson(report),"before",root));
                    report.shutdownObserved=true;report.shutdownRootHeld=false;Assert.IsFalse(Validate(JsonUtility.ToJson(report),"before",root));
                    report.shutdownRootHeld=true;report.shutdownSavingUnregistered=false;break;
                case "malformed-json":
                    foreach(var json in new[]{null,"", "{", "null", "{}"})Assert.IsFalse(Validate(json,"before",root));return;
                default:Assert.Fail("Unknown provenance mutation");break;
            }
            Assert.IsFalse(Validate(JsonUtility.ToJson(report),"before",root),mutation);
        }
    }
}
