using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using G=CavesOfOoo.Rendering.ReferenceGladeContactGeometry;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeContactGeometryTests
    {
        static float[] Face(float width=.12f,float depth=.16f,float y=0)=>new[]{-width/2,y,-depth/2,width/2,y,-depth/2,width/2,y,depth/2,-width/2,y,depth/2};
        static int[] Down=>new[]{0,1,2,0,2,3};
        static byte[] Field()=>new byte[G.Width*G.Height];
        static G.Placed At(float x,float z,int quarter=0)=>new G.Placed(new G.Footprint(-.1f,.1f,-.3f,.3f),x,z,quarter);
        static int Pixel(float x,float z)=>(int)(z*8)*G.Width+(int)(x*8);
        [Test]public void RealDownwardBaseBecomesOneDeduplicatedRectangleWithoutSourceMutation()
        {var xyz=Face();var original=(float[])xyz.Clone();var indices=Down;var before=(int[])indices.Clone();var result=G.Extract(xyz,indices);Assert.AreEqual(1,result.Length);Assert.AreEqual(-.06f,result[0].MinX);Assert.AreEqual(.08f,result[0].MaxZ);CollectionAssert.AreEqual(original,xyz);CollectionAssert.AreEqual(before,indices);}
        [TestCase(1f,1f,0f)][TestCase(.12f,.16f,.2f)][TestCase(.12f,.16f,-.1f)]
        public void BroadFloorElevatedAndUndergroundFacesNeverCastContact(float width,float depth,float y)=>Assert.IsEmpty(G.Extract(Face(width,depth,y),Down));
        [Test]public void NarrowGroundLevelMasonryStripIsNotConfusedWithBroadFloor()=>Assert.AreEqual(1,G.Extract(Face(.9f,.2f,0),Down).Length);
        [Test]public void UpwardAndSlopedFacesAreNotGroundBases()
        {Assert.IsEmpty(G.Extract(Face(),new[]{0,2,1,0,3,2}));var slope=Face();slope[4]=.04f;slope[7]=.04f;Assert.IsEmpty(G.Extract(slope,Down));}
        [TestCase(-.005f)][TestCase(.08f)]public void AuthoredNearGroundBoundaryFacesAreIncluded(float y)=>Assert.AreEqual(1,G.Extract(Face(y:y),Down).Length);
        [TestCase("nan")][TestCase("index")][TestCase("vertices")][TestCase("triangles")]
        public void MalformedMeshFailsBeforeAnySourceWrite(string fault)
        {var xyz=Face();var triangles=Down;if(fault=="nan")xyz[0]=float.NaN;if(fault=="index")triangles[0]=99;if(fault=="vertices")xyz=new float[2];if(fault=="triangles")triangles=new[]{0,1};var before=(float[])xyz.Clone();Assert.Throws<ArgumentException>(()=>G.Extract(xyz,triangles));CollectionAssert.AreEqual(before,xyz);}
        [Test]public void RasterHasSolidContactSoftBoundedFalloffAndUnchangedFarFloor()
        {var p=Field();Assert.True(G.Rasterize(new[]{At(20.5f,10.5f)},p));Assert.AreEqual(255,p[Pixel(20.5f,10.5f)]);Assert.AreEqual(0,p[Pixel(22,10.5f)]);Assert.True(p.Any(v=>v>0&&v<255));Assert.That(p.Count(v=>v>0),Is.InRange(8,100));}
        [Test]public void QuarterTurnRotatesTheActualUnequalFootprint()
        {var a=Field();var b=Field();G.Rasterize(new[]{At(20.5f,10.5f)},a);G.Rasterize(new[]{At(20.5f,10.5f,1)},b);Assert.AreEqual(a.Count(v=>v>0),b.Count(v=>v>0));Assert.Greater(a[Pixel(20.5f,10.8125f)],b[Pixel(20.5f,10.8125f)]);Assert.Greater(b[Pixel(20.8125f,10.5f)],a[Pixel(20.8125f,10.5f)]);}
        [TestCase(0,20.8125f,10.6875f)][TestCase(1,20.6875f,10.1875f)]
        [TestCase(2,20.1875f,10.3125f)][TestCase(3,20.3125f,10.8125f)]
        public void OffCentreFootprintUsesNativePositiveYawRatherThanItsMirror(int quarter,float x,float z)
        {
            // Unity positive Y yaw maps +Z toward +X and +X toward -Z.
            // An asymmetric off-centre base distinguishes +90 from -90.
            var p=Field();var shape=new G.Footprint(.25f,.45f,.10f,.30f);
            Assert.True(G.Rasterize(new[]{new G.Placed(shape,20.5f,10.5f,quarter)},p));
            Assert.AreEqual(255,p[Pixel(x,z)]);
        }
        [Test]public void OverlappingContactsAreBoundedOrderIndependentAndDoNotAccumulateBlackness()
        {var one=Field();var many=Field();var reversed=Field();var a=At(20.5f,10.5f);var b=At(20.625f,10.5f);G.Rasterize(new[]{a},one);G.Rasterize(new[]{a,b,a},many);G.Rasterize(new[]{b,a},reversed);CollectionAssert.AreEqual(many,reversed);Assert.AreEqual(255,many.Max());Assert.GreaterOrEqual(many.Count(v=>v>0),one.Count(v=>v>0));}
        [TestCase(0f,0f)][TestCase(80f,25f)][TestCase(-1f,-1f)]
        public void BorderContactsClipWithoutWrapping(float x,float z)
        {var p=Field();Assert.True(G.Rasterize(new[]{At(x,z)},p));Assert.AreEqual(0,p[Pixel(40,12)]);if(x==0)Assert.Greater(p.Take(G.Width).Max(),0);if(x<0)Assert.AreEqual(0,p.Max());}
        [Test]public void MovingRemovingAndEmptyFieldsClearEveryPreviousContact()
        {var p=Field();G.Rasterize(new[]{At(20.5f,10.5f)},p);Assert.Greater(p.Max(),0);G.Rasterize(new[]{At(25.5f,10.5f)},p);Assert.AreEqual(0,p[Pixel(20.5f,10.5f)]);Assert.AreEqual(255,p[Pixel(25.5f,10.5f)]);Assert.True(G.Rasterize(Array.Empty<G.Placed>(),p));Assert.AreEqual(0,p.Max());}
        [Test]public void WorkBudgetFailureClearsAtomicallyInsteadOfPublishingAnOrderDependentPrefix()
        {var p=Enumerable.Repeat((byte)91,G.Width*G.Height).ToArray();Assert.False(G.Rasterize(new[]{At(20.5f,10.5f),At(25.5f,10.5f)},p,1));Assert.AreEqual(0,p.Max());}
        [Test]public void InvalidPlacedGeometryClearsPreviousStateAndRefuses()
        {var p=Enumerable.Repeat((byte)91,G.Width*G.Height).ToArray();Assert.False(G.Rasterize(new[]{At(float.NaN,10)},p));Assert.AreEqual(0,p.Max());Assert.False(G.Rasterize(new[]{At(2,2,4)},p));}
    }
}
