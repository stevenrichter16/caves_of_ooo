#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace CavesOfOoo.Tests
{
    public sealed class DensityArtImportScopeTests
    {
        [TestCase("empty")][TestCase("duplicate")][TestCase("unknown")][TestCase("foreign-family")]
        public void RingSubsetRejectsInvalidSelectionBeforeCatalogWrite(string kind)
        {
            string path="Assets/Art3D/SpawnRing/Definitions/catalog.json",before=File.ReadAllText(path),receipt=Path.GetTempFileName();
            var ids=kind=="empty"?Array.Empty<string>():kind=="duplicate"?new[]{"ring-player","ring-player"}:kind=="foreign-family"?new[]{"spread-hedge-0"}:new[]{"not-a-model"};
            try
            {
                LogAssert.Expect(LogType.Error,new Regex("Explicit import failed: Select distinct current model IDs"));
                var error=Assert.Throws<TargetInvocationException>(()=>Method("SpawnRing3DAssetBuilder",3).Invoke(null,new object[]{"ArtSource/SpawnRing3D",receipt,ids}));
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);Assert.AreEqual(before,File.ReadAllText(path));
            }
            finally{File.Delete(receipt);}
        }
        [TestCase("empty")][TestCase("duplicate")][TestCase("unknown")]
        public void VoxelSubsetRejectsInvalidSelectionBeforeCatalogWrite(string kind)
        {
            string path="Assets/Resources/VoxelWorld/Library.asset",before=File.ReadAllText(path),receipt=Path.GetTempFileName();
            var paths=kind=="empty"?Array.Empty<string>():kind=="duplicate"?new[]{"Assets/Art3D/SpawnRing/Models/ring-player.fbx","Assets/Art3D/SpawnRing/Models/ring-player.fbx"}:new[]{"not-a-model"};
            try
            {
                var error=Assert.Throws<TargetInvocationException>(()=>Method("VoxelWorldMeshBuilder",2).Invoke(null,new object[]{receipt,paths}));
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);Assert.AreEqual(before,File.ReadAllText(path));
            }
            finally{File.Delete(receipt);}
        }
        static MethodInfo Method(string name,int count)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor."+name)).FirstOrDefault(t=>t!=null);Assert.NotNull(type);
            var method=type.GetMethods(BindingFlags.Static|BindingFlags.Public).SingleOrDefault(m=>m.Name=="Build"&&m.GetParameters().Length==count);Assert.NotNull(method);return method;
        }
    }
}
#endif
