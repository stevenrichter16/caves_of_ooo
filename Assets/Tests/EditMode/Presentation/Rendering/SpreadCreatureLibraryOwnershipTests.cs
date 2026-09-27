using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadCreatureLibraryOwnershipTests
    {
        [TestCase(false)][TestCase(true)]
        public void MatchingBoneNamesCannotBorrowASeparatePrefabHierarchy(bool foreignBone)
        {
            var source=Resources.Load<ScriptableObject>("SpreadCreature3D/Library");Assert.NotNull(source);
            var clone=Object.Instantiate(source);GameObject prefab=null,foreign=null;
            try
            {
                var entries=(Array)source.GetType().GetField("Entries").GetValue(clone);
                var entry=entries.GetValue(0);var prefabField=entry.GetType().GetField("Prefab");
                var original=(GameObject)prefabField.GetValue(entry);prefab=Object.Instantiate(original);prefabField.SetValue(entry,prefab);
                var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);Assert.NotNull(skin);Assert.Greater(skin.bones.Length,1);
                var originalBones=original.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
                if(foreignBone)
                {
                    var bones=skin.bones;foreign=new GameObject(bones[1].name);
                    Assert.False(foreign.transform.IsChildOf(prefab.transform));bones[1]=foreign.transform;skin.bones=bones;
                }
                var validate=source.GetType().GetMethod("Validate");Assert.NotNull(validate);
                if(foreignBone)
                {
                    var error=Assert.Throws<TargetInvocationException>(()=>validate.Invoke(clone,null));
                    Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
                }
                else Assert.DoesNotThrow(()=>validate.Invoke(clone,null),"An intact owned clone remains a valid rig.");
                CollectionAssert.AreEqual(originalBones,original.GetComponentInChildren<SkinnedMeshRenderer>(true).bones);
            }
            finally
            {if(foreign!=null)Object.DestroyImmediate(foreign);if(prefab!=null)Object.DestroyImmediate(prefab);Object.DestroyImmediate(clone);}
        }
    }
}
