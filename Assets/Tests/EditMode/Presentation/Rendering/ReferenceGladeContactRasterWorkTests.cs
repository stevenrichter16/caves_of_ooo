using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using G=CavesOfOoo.Rendering.ReferenceGladeContactGeometry;
using L=CavesOfOoo.Tests.ContactRasterBeforeOptimization;
namespace CavesOfOoo.Tests
{
 public sealed class ReferenceGladeContactRasterWorkTests
 {
  static G.Placed Rect(float x,float z,float width=.8f,float depth=.6f,int yaw=0)=>new G.Placed(new G.Footprint(-width/2,width/2,-depth/2,depth/2),x,z,yaw);
  internal static bool Observe(IReadOnlyList<G.Placed> shapes,byte[] target,int budget,out int evaluations)
  {
   var m=typeof(G).GetMethod("Rasterize",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(IReadOnlyList<G.Placed>),typeof(byte[]),typeof(int),typeof(int).MakeByRefType()},null);
   Assert.NotNull(m,"Per-call sample count must observe the same production raster body before optimization.");
   var args=new object[]{shapes,target,budget,0};
   try{bool result=(bool)m.Invoke(null,args);evaluations=(int)args[3];return result;}catch(TargetInvocationException e){throw e.InnerException;}
  }
  internal static bool Reference(IReadOnlyList<G.Placed> shapes,byte[] target,int budget=G.MaximumRasterWrites)
   =>L.Rasterize(shapes==null?null:shapes.Select(s=>new L.Placed(new L.Footprint(s.Shape.MinX,s.Shape.MaxX,s.Shape.MinZ,s.Shape.MaxZ),s.X,s.Z,s.QuarterTurns)).ToArray(),target,budget);
  internal static void Exact(IReadOnlyList<G.Placed> shapes,int budget=G.MaximumRasterWrites)
  {
   var expected=Enumerable.Repeat((byte)137,G.Width*G.Height).ToArray();var actual=(byte[])expected.Clone();
   Assert.AreEqual(Reference(shapes,expected,budget),G.Rasterize(shapes,actual,budget));CollectionAssert.AreEqual(expected,actual);
  }
  [TestCase(2)][TestCase(32)][TestCase(128)]
  public void AlreadyOpaqueSamplesDoNotRepeatDistanceAndFalloffWork(int repeats)
  {
   var one=new byte[G.Width*G.Height];var many=new byte[one.Length];var shape=Rect(20.5f,10.5f);
   Assert.True(Observe(new[]{shape},one,G.MaximumRasterWrites,out int single));int opaque=one.Count(v=>v==255);Assert.Greater(opaque,0);
   Assert.True(Observe(Enumerable.Repeat(shape,repeats).ToArray(),many,G.MaximumRasterWrites,out int repeated));
   CollectionAssert.AreEqual(one,many);Assert.Less(repeated,single*repeats,"Covered255 output cannot become darker; recomputing its sample is redundant.");
   Assert.AreEqual(single+(single-opaque)*(repeats-1),repeated);Exact(Enumerable.Repeat(shape,repeats).ToArray());
  }
  [Test]public void SoftOnlyRepeatedSamplesStillEvaluateAndRemainIdentical()
  {
   var shape=Rect(20.5f,10.5f,.01f,.01f);var one=new byte[G.Width*G.Height];var many=new byte[one.Length];
   Assert.True(Observe(new[]{shape},one,G.MaximumRasterWrites,out int single));Assert.Zero(one.Count(v=>v==255));Assert.Greater(one.Max(),0);
   Assert.True(Observe(Enumerable.Repeat(shape,32).ToArray(),many,G.MaximumRasterWrites,out int repeated));Assert.AreEqual(single*32,repeated);CollectionAssert.AreEqual(one,many);
  }
  [Test]public void OriginalWorkAdmissionCountsSkippedPixelsAndClearsBeforeRefusal()
  {
   var shape=Rect(20.5f,10.5f);var field=new byte[G.Width*G.Height];Assert.True(Observe(new[]{shape},field,G.MaximumRasterWrites,out int single));
   Assert.False(Observe(new[]{shape,shape},field,single,out int work));Assert.Zero(work);Assert.Zero(field.Max());Exact(new[]{shape,shape},single);
  }
  [Test]public void FullyOpaqueIncomingTargetIsClearedBeforeTheFirstShape()
  {
   var field=Enumerable.Repeat((byte)255,G.Width*G.Height).ToArray();Assert.True(Observe(new[]{Rect(20.5f,10.5f)},field,G.MaximumRasterWrites,out int work));Assert.Greater(work,0);Assert.Zero(field[0]);Exact(new[]{Rect(20.5f,10.5f)});
  }
  [TestCase(0f,0f)][TestCase(80f,25f)][TestCase(-.1f,12.3f)][TestCase(79.95f,24.91f)]
  [TestCase(20.12345f,10.98765f)][TestCase(40.5f,12.5f)][TestCase(1e30f,1e30f)]
  public void EveryYawAndTranslatedOverlapMatchesAllOriginalBytes(float x,float z)
  {
   var shapes=new List<G.Placed>();for(int yaw=0;yaw<4;yaw++)for(int i=0;i<12;i++)shapes.Add(Rect(x+i*.0237f,z-i*.0413f,.37f,.81f,yaw));Exact(shapes);shapes.Reverse();Exact(shapes);
  }
  [TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(947)][TestCase(45009)][TestCase(228)]
  public void DeterministicVariedFootprintsMatchTheFullOriginalField(int seed)
  {
   var random=new Random(seed);var shapes=new List<G.Placed>();for(int i=0;i<512;i++)
   {var p=Rect((float)(random.NextDouble()*82-1),(float)(random.NextDouble()*27-1),(float)(random.NextDouble()+.001),(float)(random.NextDouble()+.001),random.Next(4));shapes.Add(p);if(i%5==0)shapes.Add(p);}
   Exact(shapes);shapes.Reverse();Exact(shapes);
  }
  [TestCase("nan")][TestCase("infinity")][TestCase("quarter")][TestCase("inverted")][TestCase("null")][TestCase("negative-budget")][TestCase("over-budget")]
  public void MalformedLatePlacementOrBudgetRefusesAtomically(string fault)
  {
   var valid=Rect(20.5f,10.5f);var bad=fault=="nan"?Rect(float.NaN,1):fault=="infinity"?Rect(float.PositiveInfinity,1):fault=="quarter"?Rect(1,1,yaw:4):new G.Placed(new G.Footprint(2,1,0,1),1,1);
   var shapes=fault=="null"?null:new[]{valid,valid,bad};int budget=fault=="negative-budget"?-1:fault=="over-budget"?G.MaximumRasterWrites+1:G.MaximumRasterWrites;
   if(fault.EndsWith("budget"))shapes=new[]{valid};
   var field=Enumerable.Repeat((byte)99,G.Width*G.Height).ToArray();Assert.False(Observe(shapes,field,budget,out int work));Assert.Zero(work);Assert.Zero(field.Max());Exact(shapes,budget);
  }
  [Test]public void EmptyFieldHasNoWorkAndClearsOldContact()
  {var field=Enumerable.Repeat((byte)42,G.Width*G.Height).ToArray();Assert.True(Observe(Array.Empty<G.Placed>(),field,0,out int work));Assert.Zero(work);Assert.Zero(field.Max());}
 }
}
#if UNITY_EDITOR
namespace CavesOfOoo.Tests
{
 public sealed class ReferenceGladeContactNativeRasterWorkTests
 {
  [TestCase("Overworld.11.10.0")][TestCase("Overworld.12.10.0")]
  public void ActualCurrentPlacedMeshesAndVisibleMaskRemainByteExact(string address)
  {
   using(var f=new SpawnRing3DIntegrationFixture(address))
   {
    var p=(CavesOfOoo.Rendering.SpawnRing3DPresenter)f.Presenter;Assert.True(p.IsReady,p.Failure);
    string before=f.Zone.TileState.ToSaveString();int version=f.Zone.EntityVersion;
    for(int state=0;state<3;state++)
    {
     f.Set("FullReveal",state==0);if(state>0)for(int y=0;y<25;y++)for(int x=0;x<80;x++)f.Zone.GetCell(x,y).IsVisible=state==1?x<40:x>=40;
     f.Refresh();var field=typeof(CavesOfOoo.Rendering.SpawnRing3DPresenter).GetField("groundContact",BindingFlags.Instance|BindingFlags.NonPublic);var contact=field.GetValue(p);Assert.NotNull(contact);
     var placed=(List<G.Placed>)contact.GetType().GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contact);Assert.Greater(placed.Count,0);
     var original=new byte[G.Width*G.Height];Assert.True(ReferenceGladeContactRasterWorkTests.Reference(placed,original));
     var actual=new byte[original.Length];Assert.True(ReferenceGladeContactRasterWorkTests.Observe(placed,actual,G.MaximumRasterWrites,out int work));Assert.Greater(work,0);CollectionAssert.AreEqual(original,actual);
     var material=p.ActiveSurface.MaterialFor(CavesOfOoo.Rendering.ReferenceGladeVoxelLibrary.Load().Material);var texture=(UnityEngine.Texture2D)material.GetTexture("_GroundContact");Assert.NotNull(texture);
     CollectionAssert.AreEqual(original,texture.GetPixels32().Select(v=>v.r).ToArray(),"Submitted current geometry/FOV mask must equal the preserved algorithm.");
    }
    Assert.AreEqual(before,f.Zone.TileState.ToSaveString());Assert.AreEqual(version,f.Zone.EntityVersion);
   }
  }
 }
}
#endif

// Frozen pre-optimization oracle; original SHA256 9f1d7dc695f0f4da2ab6e359f5fed2de5e2a3582b87dca261f3d08dadf932d29.
namespace CavesOfOoo.Tests
{
    /// <summary>Pure mesh-base extraction and bounded soft-ground raster math.
    /// Input arrays are borrowed; only the explicitly supplied byte field is written.</summary>
    internal static class ContactRasterBeforeOptimization
    {
        public const int SamplesPerCell=8,Width=80*SamplesPerCell,Height=25*SamplesPerCell;
        public const float BlurRadius=.18f,MinimumBase=-.005f,MaximumBase=.08f,BroadFloorSize=.8f;
        public const int MaximumMeshVertices=65536,MaximumMeshFootprints=512,MaximumFootprints=32768,MaximumRasterWrites=1000000;
        public readonly struct Footprint
        {
            public readonly float MinX,MaxX,MinZ,MaxZ;
            public Footprint(float minX,float maxX,float minZ,float maxZ){MinX=minX;MaxX=maxX;MinZ=minZ;MaxZ=maxZ;}
        }
        public readonly struct Placed
        {
            public readonly Footprint Shape;public readonly float X,Z;public readonly int QuarterTurns;
            public Placed(Footprint shape,float x,float z,int quarterTurns=0){Shape=shape;X=x;Z=z;QuarterTurns=quarterTurns;}
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        /// <summary>Returns deduplicated triangle-base rectangles from packed XYZ
        /// vertices. Malformed/oversized input throws before any source write;
        /// raised, sloped, upward and broad floor faces contribute no rectangle.</summary>
        public static Footprint[] Extract(float[] xyz,int[] triangles)
        {
            if(xyz==null||triangles==null||xyz.Length%3!=0||triangles.Length%3!=0
                ||xyz.Length/3>MaximumMeshVertices||triangles.Length>MaximumMeshVertices*6)
                throw new ArgumentException("Malformed or oversized contact source mesh.");
            for(int i=0;i<xyz.Length;i++)if(!Finite(xyz[i]))throw new ArgumentException("Nonfinite source vertex.");
            for(int i=0;i<triangles.Length;i++)if(triangles[i]<0||triangles[i]>=xyz.Length/3)throw new ArgumentException("Invalid source triangle.");
            var result=new List<Footprint>();
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i]*3,b=triangles[i+1]*3,c=triangles[i+2]*3;
                float y=xyz[a+1];if(y<MinimumBase||y>MaximumBase||Math.Abs(y-xyz[b+1])>.0001f||Math.Abs(y-xyz[c+1])>.0001f)continue;
                // Winding must face down; upright/sloped walls and top caps are not bases.
                float crossY=(xyz[b+2]-xyz[a+2])*(xyz[c]-xyz[a])-(xyz[b]-xyz[a])*(xyz[c+2]-xyz[a+2]);
                if(crossY>=-.000001f)continue;
                float minX=Math.Min(xyz[a],Math.Min(xyz[b],xyz[c])),maxX=Math.Max(xyz[a],Math.Max(xyz[b],xyz[c]));
                float minZ=Math.Min(xyz[a+2],Math.Min(xyz[b+2],xyz[c+2])),maxZ=Math.Max(xyz[a+2],Math.Max(xyz[b+2],xyz[c+2]));
                if(maxX-minX<=.0001f||maxZ-minZ<=.0001f||(maxX-minX>=BroadFloorSize&&maxZ-minZ>=BroadFloorSize))continue;
                bool duplicate=false;foreach(var old in result)if(old.MinX==minX&&old.MaxX==maxX&&old.MinZ==minZ&&old.MaxZ==maxZ){duplicate=true;break;}
                if(duplicate)continue;
                if(result.Count==MaximumMeshFootprints)throw new ArgumentException("Contact source exceeds footprint budget.");
                result.Add(new Footprint(minX,maxX,minZ,maxZ));
            }
            return result.ToArray();
        }
        private static bool Bounds(Placed item,out Footprint rect)
        {
            rect=default;var s=item.Shape;
            if(!Finite(s.MinX)||!Finite(s.MaxX)||!Finite(s.MinZ)||!Finite(s.MaxZ)||s.MinX>=s.MaxX||s.MinZ>=s.MaxZ
                ||!Finite(item.X)||!Finite(item.Z)||item.QuarterTurns<0||item.QuarterTurns>3)return false;
            switch(item.QuarterTurns)
            {
                case 0:rect=new Footprint(s.MinX+item.X,s.MaxX+item.X,s.MinZ+item.Z,s.MaxZ+item.Z);break;
                case 1:rect=new Footprint(s.MinZ+item.X,s.MaxZ+item.X,-s.MaxX+item.Z,-s.MinX+item.Z);break;
                case 2:rect=new Footprint(-s.MaxX+item.X,-s.MinX+item.X,-s.MaxZ+item.Z,-s.MinZ+item.Z);break;
                case 3:rect=new Footprint(-s.MaxZ+item.X,-s.MinZ+item.X,s.MinX+item.Z,s.MaxX+item.Z);break;
            }
            return Finite(rect.MinX)&&Finite(rect.MaxX)&&Finite(rect.MinZ)&&Finite(rect.MaxZ);
        }
        private static bool Pixels(Footprint r,out int x0,out int x1,out int y0,out int y1)
        {
            x0=x1=y0=y1=0;
            // Clip in floating-point before integer conversion, including very distant geometry.
            if(r.MaxX+BlurRadius<=0||r.MinX-BlurRadius>=80||r.MaxZ+BlurRadius<=0||r.MinZ-BlurRadius>=25)return false;
            x0=(int)Math.Floor(Math.Max(0,r.MinX-BlurRadius)*SamplesPerCell);
            x1=Math.Min(Width-1,(int)Math.Ceiling(Math.Min(80,r.MaxX+BlurRadius)*SamplesPerCell)-1);
            y0=(int)Math.Floor(Math.Max(0,r.MinZ-BlurRadius)*SamplesPerCell);
            y1=Math.Min(Height-1,(int)Math.Ceiling(Math.Min(25,r.MaxZ+BlurRadius)*SamplesPerCell)-1);
            return x1>=x0&&y1>=y0;
        }
        /// <summary>Replaces the complete fixed-size target with bounded soft
        /// footprints. Invalid placement or work overflow clears the target and
        /// returns false; a malformed target throws. No partial prefix survives.</summary>
        public static bool Rasterize(IReadOnlyList<Placed> placed,byte[] target,int maximumWrites=MaximumRasterWrites)
        {
            if(target==null||target.Length!=Width*Height)throw new ArgumentException("Contact target has wrong dimensions.");
            Array.Clear(target,0,target.Length);
            if(placed==null||placed.Count>MaximumFootprints||maximumWrites<0||maximumWrites>MaximumRasterWrites)return false;
            long writes=0;
            // Validate the whole batch and work limit before writing any contact.
            for(int i=0;i<placed.Count;i++)
            {
                if(!Bounds(placed[i],out var rect))return false;
                if(Pixels(rect,out int x0,out int x1,out int y0,out int y1))writes+=(long)(x1-x0+1)*(y1-y0+1);
                if(writes>maximumWrites)return false;
            }
            for(int i=0;i<placed.Count;i++)
            {
                Bounds(placed[i],out var rect);if(!Pixels(rect,out int x0,out int x1,out int y0,out int y1))continue;
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    float px=(x+.5f)/SamplesPerCell,pz=(y+.5f)/SamplesPerCell;
                    float dx=Math.Max(0,Math.Max(rect.MinX-px,px-rect.MaxX)),dz=Math.Max(0,Math.Max(rect.MinZ-pz,pz-rect.MaxZ));
                    float distance=(float)Math.Sqrt(dx*dx+dz*dz);if(distance>=BlurRadius)continue;
                    float t=1-distance/BlurRadius;byte value=(byte)Math.Round(255*t*t*(3-2*t));int at=y*Width+x;
                    if(value>target[at])target[at]=value;
                }
            }
            return true;
        }
    }
}
