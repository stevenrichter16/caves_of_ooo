using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>RED gate for planned metadata validation before any mesh/asset
    /// publication. Does not invoke Build or change Resources assets.</summary>
    public sealed class VoxelWorldToolkitPitchTests
    {
        static float Convert(float local,float world,float next)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldToolkitImporter")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type);var method=type.GetMethod("ConvertedLocalPitch",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.NotNull(method,"Validate all three pitch inputs and planned local pitch during preparation, before asset publication.");
            try {return (float)method.Invoke(null,new object[]{local,world,next});}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        [Test] public void PositivePitchConversionPreservesSourceScaleAndRepeatedImportIsIdempotent()
        {
            float first=Convert(.05f,.1f,.25f);Assert.That(first,Is.EqualTo(.125f).Within(.000001f));
            Assert.AreEqual(first,Convert(first,.25f,.25f));
        }
        [TestCase(0f,.1f,.25f)][TestCase(.05f,0f,.25f)][TestCase(.05f,.1f,0f)]
        [TestCase(-.05f,.1f,.25f)][TestCase(.05f,-.1f,.25f)][TestCase(.05f,.1f,-.25f)]
        [TestCase(float.NaN,.1f,.25f)][TestCase(.05f,float.NaN,.25f)][TestCase(.05f,.1f,float.NaN)]
        [TestCase(.05f,float.PositiveInfinity,.25f)][TestCase(.05f,.1f,float.PositiveInfinity)]
        public void InvalidOldOrNewPitchRejectsBeforePublication(float local,float world,float next)
        {Assert.AreEqual(.125f,Convert(.05f,.1f,.25f));Assert.Throws<ArgumentException>(()=>Convert(local,world,next));}
        [Test] public void OverflowingConvertedPitchRejectsBeforeAValidCatalogCanBecomeInvalid()
        {Assert.Throws<ArgumentException>(()=>Convert(float.MaxValue,.000001f,float.MaxValue));}
    }
}
