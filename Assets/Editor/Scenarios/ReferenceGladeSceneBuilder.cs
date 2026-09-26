#if UNITY_EDITOR
using System;
using System.Linq;
using CavesOfOoo.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CavesOfOoo.Editor
{
    /// <summary>Creates one playable scene using the accepted native bootstrap.
    /// The source scene is borrowed; rebuilding never discards open scene edits.</summary>
    public static class ReferenceGladeSceneBuilder
    {
        public const string ScenePath="Assets/Scenes/Main/ReferenceGlade.unity";
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before creating the glade scene.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).path==ScenePath)throw new InvalidOperationException("Close the glade scene before rebuilding its authored defaults.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null
                &&!AssetDatabase.CopyAsset("Assets/Scenes/Main/SampleScene.unity",ScenePath))
                throw new InvalidOperationException("Could not copy the native bootstrap scene.");
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                var boot=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameBootstrap>(true)).Single();
                boot.FreshGameZoneID=ReferenceGladePlan.ZoneID;boot.GameplayZoomMultiplier=.82f;
                boot.ZoneRenderer.RevealEntire3DZone=false;EditorUtility.SetDirty(boot.ZoneRenderer);
                EditorUtility.SetDirty(boot);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the glade scene.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
        [MenuItem("Caves Of Ooo/Scenes/Open Reference Glade")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before opening the glade scene.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
#endif
