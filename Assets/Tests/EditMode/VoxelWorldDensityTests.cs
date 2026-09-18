#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class VoxelWorldDensityTests
    {
        static float Pitch(Vector3 size,float scale=1,bool skin=false,bool equipment=false)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldDensity")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Choose coarser geometry by object scale while protecting tiny and thin items.");
            var method=type.GetMethod("SelectWorldPitch",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method);
            try{return (float)method.Invoke(null,new object[]{size,scale,skin,equipment});}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        [Test] public void OrdinaryStaticObjectsAndGroundUseFourVoxelsPerMetre()
        {Assert.AreEqual(.25f,Pitch(Vector3.one));Assert.AreEqual(.25f,Pitch(new Vector3(10,.01f,5)));}
        [Test] public void LargeSceneryRemainsOnTheSameCellDividingGrid()
        {Assert.AreEqual(.25f,Pitch(Vector3.one*4));Assert.AreEqual(4,1/Pitch(Vector3.one*4));}
        [Test] public void CharacterBodyGetsLargerVoxelsButThinCreatureKeepsItsSilhouette()
        {Assert.AreEqual(.1875f,Pitch(new Vector3(1.1f,.85f,1.73f),skin:true));Assert.AreEqual(.125f,Pitch(new Vector3(.4f,.9f,.1f),skin:true));}
        [Test] public void TinyObjectsAndHeldEquipmentRetainTheirExistingLowCellCount()
        {Assert.AreEqual(.125f,Pitch(Vector3.one*.25f));Assert.AreEqual(.125f,Pitch(Vector3.one,equipment:true));Assert.AreEqual(.25f,Pitch(Vector3.one));}
        [TestCase(2.637f,.416f,.416f)]
        [TestCase(.416f,2.637f,.416f)]
        [TestCase(.416f,.416f,2.637f)]
        [TestCase(.94f,.18f,.171f)]
        public void ThinElongatedRigidObjectsRetainTheirNarrowSelectionEnvelope(float x,float y,float z)
        {
            Assert.AreEqual(.125f,Pitch(new Vector3(x,y,z)));
            Assert.AreEqual(.1875f,Pitch(new Vector3(x,y,z),skin:true),
                "The rigid contact guard must not change an articulated body's pitch.");
            Assert.AreEqual(.25f,Pitch(new Vector3(2.637f,.416f,2.637f)),
                "A broad thin floor is not a narrow rod and must remain coarse.");
            Assert.AreEqual(.25f,Pitch(new Vector3(1.2f,.416f,.416f)),
                "A compact body below the elongation threshold remains coarse.");
        }
        [Test] public void SlenderShapeGuardUsesWorldSizeAndPreservesLargeStaticPitch()
        {
            Assert.AreEqual(.125f,Pitch(new Vector3(5.274f,.832f,.832f),.5f));
            Assert.AreEqual(.25f,Pitch(new Vector3(2.637f,.416f,.416f),2));
            Assert.AreEqual(.2f,Pitch(new Vector3(4,.4f,.4f)));
        }
        [Test] public void PolicyUsesEffectiveWorldDimensionsWithoutConsumingRandom()
        {
            var state=UnityEngine.Random.state;Assert.AreEqual(.125f,Pitch(Vector3.one,.2f));Assert.AreEqual(.25f,Pitch(Vector3.one*.2f,5));
            Assert.AreEqual(state,UnityEngine.Random.state);
        }
        [TestCase(0f)][TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void InvalidPrefabScaleRejectsBeforeAnyBake(float scale)
        {Assert.AreEqual(.25f,Pitch(Vector3.one));Assert.Throws<ArgumentException>(()=>Pitch(Vector3.one,scale));}
        [TestCase(0f,0f,0f)][TestCase(-1f,1f,1f)][TestCase(float.NaN,1f,1f)][TestCase(float.PositiveInfinity,1f,1f)]
        public void InvalidBoundsRejectRatherThanCreatingMeaninglessDensity(float x,float y,float z)
        {Assert.Throws<ArgumentException>(()=>Pitch(new Vector3(x,y,z)));}
    }
}
#endif
