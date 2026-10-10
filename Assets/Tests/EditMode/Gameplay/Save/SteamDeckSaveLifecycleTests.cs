using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckSaveLifecycleTests
    {
        private static void AwaitWorkerWithoutPumping()
        {
            object queue = typeof(SaveGameService).GetField("PendingSaves", NewGameSaveFixture.Static).GetValue(null);
            var worker = (Task)queue.GetType().GetField("_worker", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(queue);
            Assert.IsTrue(worker.Wait(5000), "Background save must finish before observing undelivered completion.");
            Assert.IsTrue(SaveGameService.HasPendingSave, "Completed writes remain pending until main-thread delivery.");
        }

        [Test]
        public void FirstCheckpointAvailabilityDeliversPendingSaveBeforeLoadGate()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    Assert.IsTrue(StagedSaveProbe.Request()); AwaitWorkerWithoutPumping();
                    Assert.IsTrue(SaveGameService.HasQuickSave());
                    Assert.IsFalse(SaveGameService.HasPendingSave, "Availability gates explicit load actions and must drain the accepted first checkpoint.");
                    Assert.AreEqual(f.NewID, PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
                    Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(222, f.Loaded.WorldSeed);
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }

        [Test]
        public void InputUpdatePumpsCompletedSavesBeforeMissingPlayerEarlyReturn()
        {
            using (var f = new NewGameSaveFixture())
            {
                var go = new GameObject("Save lifecycle update probe");
                try
                {
                    var input = go.AddComponent<InputHandler>(); Assert.IsNull(input.PlayerEntity);
                    Assert.IsTrue(StagedSaveProbe.Request()); AwaitWorkerWithoutPumping();
                    typeof(InputHandler).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, null);
                    Assert.IsFalse(SaveGameService.HasPendingSave); Assert.AreEqual(f.NewID, PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
                }
                finally { StagedSaveProbe.FlushIfPresent(); UnityEngine.Object.DestroyImmediate(go); }
            }
        }

        [TestCase("OnDestroy")] [TestCase("OnApplicationQuit")]
        public void InputShutdownDrainsAcceptedCheckpoint(string callback)
        {
            using (var f = new NewGameSaveFixture())
            {
                var go = new GameObject("Save lifecycle shutdown probe");
                try
                {
                    var input = go.AddComponent<InputHandler>(); Assert.IsTrue(StagedSaveProbe.Request());
                    var method = typeof(InputHandler).GetMethod(callback, BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(method, callback + " must flush pending autosaves before losing their runtime owner.");
                    method.Invoke(input, null); Assert.IsFalse(SaveGameService.HasPendingSave);
                    var saved = SaveGameService.LoadState(Path.Combine(f.Root, f.NewID, "Quick.sav.gz"), null);
                    Assert.AreEqual(222, saved.WorldSeed); Assert.AreEqual(f.NewID, PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
                }
                finally { StagedSaveProbe.FlushIfPresent(); UnityEngine.Object.DestroyImmediate(go); }
            }
        }
    }
}
