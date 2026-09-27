using System;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadVisitorPaintAdversarialTests
 {
  private static Type Type()
  {var asset=Resources.Load<ScriptableObject>("SpreadVisitorPaint3D/Library");Assert.NotNull(asset);return asset.GetType();}
  private static object Entry(){var type=Type();return type.GetMethod("Find").Invoke(Resources.Load<ScriptableObject>("SpreadVisitorPaint3D/Library"),new object[]{"ring-rotling"});}
  private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name).GetValue(value);
  [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
  public void OnlyPrimaryPaletteUvsMayChange(int channel)
  {
   var entry=Entry();var source=Field<Mesh>(entry,"Source");var painted=Object.Instantiate(Field<Mesh>(entry,"Painted"));
   var validate=Type().GetMethod("ValidatePaintPair",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(validate);
   try
   {
    Assert.DoesNotThrow(()=>validate.Invoke(null,new object[]{source,painted}));
    var uv=new System.Collections.Generic.List<Vector4>();painted.GetUVs(channel,uv);if(uv.Count==0)for(int i=0;i<painted.vertexCount;i++)uv.Add(Vector4.zero);
    uv[0]=new Vector4(.125f,.25f,.5f,1);painted.SetUVs(channel,uv);
    var ex=Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{source,painted}));Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
   }finally{Object.DestroyImmediate(painted);}
  }
  [TestCase("bone")][TestCase("root-bone")]
  public void BorrowedOrForeignRigTransformsCannotAuthorizeAnOwnedSkin(string change)
  {
   var entry=Entry();var prefab=Field<GameObject>(entry,"SourcePrefab");var root=Object.Instantiate(prefab);var foreign=Object.Instantiate(prefab);
   var validate=Type().GetMethod("ValidateRigHierarchy",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(validate);
   try
   {
    Assert.DoesNotThrow(()=>validate.Invoke(null,new object[]{root}));var skin=root.GetComponentInChildren<SkinnedMeshRenderer>(true);var other=foreign.GetComponentInChildren<SkinnedMeshRenderer>(true);
    if(change=="bone"){var bones=skin.bones;bones[0]=other.bones[0];skin.bones=bones;}else skin.rootBone=other.rootBone;
    var ex=Assert.Throws<TargetInvocationException>(()=>validate.Invoke(null,new object[]{root}));Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
   }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(foreign);}
  }
  [Test] public void ApplyingToBorrowedPrefabRefusesWithoutTouchingNativeBuffers()
  {
   var entry=Entry();var prefab=Field<GameObject>(entry,"SourcePrefab");var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=skin.sharedMesh;var material=skin.sharedMaterial;
   var library=Resources.Load<ScriptableObject>("SpreadVisitorPaint3D/Library");var method=Type().GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Instance);Assert.NotNull(method);
   var ex=Assert.Throws<TargetInvocationException>(()=>method.Invoke(library,new object[]{prefab,"ring-rotling"}));Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
   Assert.AreSame(mesh,skin.sharedMesh);Assert.AreSame(material,skin.sharedMaterial);
  }
 }
}
