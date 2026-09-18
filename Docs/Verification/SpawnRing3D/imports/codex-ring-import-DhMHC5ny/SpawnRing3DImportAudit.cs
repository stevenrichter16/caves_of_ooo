#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit evidence wrapper around the real importer CLI. No startup hook.
    /// Uses a clean saved scene and one temporary dirty additive control; never saves user scenes.</summary>
    public static class SpawnRing3DImportAudit
    {
        [Serializable] public sealed class SceneRow
        {
            public int handle; public string path, name; public bool loaded, dirty, active;
            public int[] rootInstanceIds;
        }
        [Serializable] public sealed class Report
        {
            public string runId, startedUtc, finishedUtc, status, error, importerReport;
            public bool cleanControlVerified, dirtyControlVerified, importScenesPreserved, originalScenesPreserved, ownedScenesClosed;
            public SceneRow[] originalBefore, originalAfter, importBefore, importAfter;
            public string[] unexpectedLogs, honestyBounds;
        }
        static SceneRow[] Snapshot()
        {
            int active = SceneManager.GetActiveScene().handle;
            return Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
            {
                Scene s = SceneManager.GetSceneAt(i);
                return new SceneRow { handle=s.handle, path=s.path, name=s.name, loaded=s.isLoaded,
                    dirty=s.isDirty, active=s.handle==active,
                    rootInstanceIds=s.isLoaded?s.GetRootGameObjects().Select(g=>g.GetInstanceID()).OrderBy(id=>id).ToArray():Array.Empty<int>() };
            }).OrderBy(s=>s.handle).ToArray();
        }
        static bool Same(SceneRow[] a, SceneRow[] b)
        {
            if(a==null||b==null||a.Length!=b.Length)return false;
            for(int i=0;i<a.Length;i++)
                if(a[i].handle!=b[i].handle||a[i].path!=b[i].path||a[i].name!=b[i].name||a[i].loaded!=b[i].loaded
                    ||a[i].dirty!=b[i].dirty||a[i].active!=b[i].active||!a[i].rootInstanceIds.SequenceEqual(b[i].rootInstanceIds))return false;
            return true;
        }
        static string Arg(string name)
        {var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];return null;}
        public static void RunFromCommandLine()
        {
            string path=Arg("-spawnRing3dSceneReport");
            if(string.IsNullOrEmpty(path))throw new ArgumentException("Supply a fresh -spawnRing3dSceneReport path.");
            path=Path.GetFullPath(path);
            if(File.Exists(path))throw new InvalidOperationException("Refusing to overwrite a previous scene receipt.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for the owned Play/test/compile run to finish.");
            var report=new Report { runId=Guid.NewGuid().ToString("N"), startedUtc=DateTime.UtcNow.ToString("O"), importerReport=Arg("-spawnRing3dReport") };
            // A fresh batch editor starts with an empty untitled scene. Unity
            // refuses additive NewScene while that unsaved scene exists. Replace
            // only that proven disposable batch placeholder before the receipt;
            // never close or save a user's nonempty/dirty/untitled scene.
            var initial=SceneManager.GetActiveScene();
            if(Application.isBatchMode && SceneManager.sceneCount==1 && initial.path=="" && !initial.isDirty && initial.rootCount==0)
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity",OpenSceneMode.Single);
            var originalActive=SceneManager.GetActiveScene();report.originalBefore=Snapshot();
            Scene clean=originalActive, dirty=default;GameObject marker=null;var errors=new List<string>();
            Application.LogCallback observe=(message,stack,type)=> { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(type+": "+message+"\n"+stack); };
            try
            {
                if(!clean.IsValid() || !clean.isLoaded || clean.isDirty || string.IsNullOrEmpty(clean.path))
                    throw new InvalidOperationException("Audit requires a clean saved active scene; original state is left untouched.");
                dirty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(dirty);
                marker=new GameObject("Owned dirty-scene preservation control");
                EditorSceneManager.MarkSceneDirty(dirty);
                SceneManager.SetActiveScene(clean);
                report.cleanControlVerified=clean.isLoaded&&!clean.isDirty&&!string.IsNullOrEmpty(clean.path);
                report.dirtyControlVerified=dirty.isLoaded&&dirty.isDirty&&dirty.rootCount==1&&marker.scene==dirty;
                if(!report.cleanControlVerified||!report.dirtyControlVerified)throw new InvalidOperationException("Clean/dirty scene controls were not established.");
                report.importBefore=Snapshot();
                Application.logMessageReceived+=observe;
                try { SpawnRing3DAssetBuilder.BuildFromCommandLine(); }
                finally { Application.logMessageReceived-=observe;report.importAfter=Snapshot();report.importScenesPreserved=Same(report.importBefore,report.importAfter); }
            }
            catch(Exception e) { report.error=e.ToString(); }
            finally
            {
                Application.logMessageReceived-=observe;
                if(originalActive.IsValid()&&originalActive.isLoaded)SceneManager.SetActiveScene(originalActive);
                // Discard only the owned unsaved dirty control. The saved clean
                // control is an original scene and must retain its exact identity.
                if(dirty.IsValid())EditorSceneManager.CloseScene(dirty,true);
                report.ownedScenesClosed=!dirty.IsValid();
                report.originalAfter=Snapshot();report.originalScenesPreserved=Same(report.originalBefore,report.originalAfter);
                report.unexpectedLogs=errors.ToArray();report.finishedUtc=DateTime.UtcNow.ToString("O");
                report.honestyBounds=new[]{"This explicit CLI import preserves scene handles, dirty flags, active scene and root identities; it does not render native gameplay.","The clean active saved scene and owned dirty inactive scene are explicit controls. A proven empty clean untitled batch placeholder is replaced before the receipt; no user scene is saved. Original saved scene identities are compared before fixture setup and after teardown.","Asset/source hashes and existing GUID preservation are checked by the external runner; no claim of transactional rollback on failed imports."};
                report.status=report.error==null&&report.cleanControlVerified&&report.dirtyControlVerified&&report.importScenesPreserved&&report.originalScenesPreserved&&report.ownedScenesClosed&&errors.Count==0?"PASS":"FAIL";
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
            }
            if(report.status!="PASS")throw new InvalidOperationException("Ring import audit failed; retained evidence: "+path+"\n"+report.error);
            Debug.Log("[SpawnRing3D] Import and scene preservation audit passed: "+path);
        }
    }
}
#endif
