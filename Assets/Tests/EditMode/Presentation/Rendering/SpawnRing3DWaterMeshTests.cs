using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Local geometry only. Mask bits are cardinal DRY neighbours:
    /// N=1 (+Z), E=2 (+X), S=4 (-Z), W=8 (-X). No runtime water edits.</summary>
    public sealed class SpawnRing3DWaterMeshTests
    {
        const float Radius=.1f,Epsilon=.00001f;
        static readonly int[] Adjacent={1|8,1|2,2|4,4|8};
        static readonly Vector2[] Corners={new Vector2(-.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,-.5f),new Vector2(-.5f,-.5f)};
        static Mesh Create(int mask)
        {
            var type=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.SpawnRing3DWaterMesh");
            Assert.NotNull(type,"Record missing-water-helper RED before creating geometry.");
            var method=type.GetMethod("Create",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(int)},null);Assert.NotNull(method);
            try{return(Mesh)method.Invoke(null,new object[]{mask});}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static void All(Action<int,Mesh> inspect)
        {for(int mask=0;mask<16;mask++){Mesh mesh=null;try{mesh=Create(mask);Assert.NotNull(mesh,"mask "+mask);inspect(mask,mesh);}finally{if(mesh!=null)Object.DestroyImmediate(mesh);}}}
        static bool Near(Vector2 a,Vector2 b)=>Vector2.Distance(a,b)<Epsilon;
        static Vector2 XZ(Vector3 v)=>new Vector2(v.x,v.z);
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Has(Mesh m,Vector2 p)=>m.vertices.Any(v=>Near(XZ(v),p));
        static bool Contains(Mesh mesh,Vector2 point)
        {
            var v=mesh.vertices;var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=XZ(v[indices[i]]);var b=XZ(v[indices[i+1]]);var c=XZ(v[indices[i+2]]);
                float ab=Cross(b-a,point-a),bc=Cross(c-b,point-b),ca=Cross(a-c,point-c);
                if((ab>=-Epsilon&&bc>=-Epsilon&&ca>=-Epsilon)||(ab<=Epsilon&&bc<=Epsilon&&ca<=Epsilon))return true;
            }
            return false;
        }
        static float Area(Mesh m)
        {float area=0;var v=m.vertices;var t=m.triangles;for(int i=0;i<t.Length;i+=3)area+=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).magnitude*.5f;return area;}
        static int Rounded(int mask)=>Adjacent.Count(pair=>(mask&pair)==pair);
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);

        [Test] public void AllSixteenMasksProduceFiniteNonemptyOneCellPlanes()
        {All((mask,m)=>{Assert.GreaterOrEqual(m.vertexCount,4);Assert.IsNotEmpty(m.triangles);foreach(var v in m.vertices){Assert.IsTrue(Finite(v.x)&&Finite(v.y)&&Finite(v.z));Assert.AreEqual(0,v.y,Epsilon);Assert.That(v.x,Is.InRange(-.5f,.5f));Assert.That(v.z,Is.InRange(-.5f,.5f));}});}
        [Test] public void EveryMaskRetainsExactCardinalCellExtents()
        {All((mask,m)=>{var b=m.bounds;Assert.AreEqual(-.5f,b.min.x,Epsilon);Assert.AreEqual(.5f,b.max.x,Epsilon);Assert.AreEqual(-.5f,b.min.z,Epsilon);Assert.AreEqual(.5f,b.max.z,Epsilon);Assert.AreEqual(0,b.center.y,Epsilon);Assert.AreEqual(0,b.size.y,Epsilon);});}
        [Test] public void CornerIsRoundedOnlyWhenBothAdjacentCardinalsAreDry()
        {All((mask,m)=>{for(int i=0;i<4;i++){bool rounded=(mask&Adjacent[i])==Adjacent[i];Assert.AreEqual(!rounded,Has(m,Corners[i]),"mask "+mask+" corner "+i);Assert.AreEqual(!rounded,Contains(m,Corners[i]),"coverage mask "+mask+" corner "+i);}});}
        [Test] public void RoundedCornersHaveMeasuredPointOneRadiusAndTangentEndpoints()
        {
            All((mask,m)=>{for(int i=0;i<4;i++)if((mask&Adjacent[i])==Adjacent[i])
            {
                var corner=Corners[i];float sx=Mathf.Sign(corner.x),sz=Mathf.Sign(corner.y);var center=corner-new Vector2(sx*Radius,sz*Radius);
                Assert.IsTrue(Has(m,new Vector2(center.x,corner.y)));Assert.IsTrue(Has(m,new Vector2(corner.x,center.y)));
                var arc=m.vertices.Select(XZ).Where(p=>sx*p.x>=.4f-Epsilon&&sz*p.y>=.4f-Epsilon).ToArray();Assert.GreaterOrEqual(arc.Length,4);
                foreach(var p in arc)Assert.AreEqual(Radius,Vector2.Distance(center,p),Epsilon,"arc radius mask "+mask+" corner "+i);
            }});
        }
        [Test] public void EveryWetNeighborEdgeCoversItsFullLengthIncludingBothCorners()
        {
            All((mask,m)=>{for(int direction=0;direction<4;direction++)if((mask&(1<<direction))==0)
                for(int step=0;step<=20;step++)
                {float a=-.5f+step/20f;var p=direction==0?new Vector2(a,.5f):direction==1?new Vector2(.5f,a):direction==2?new Vector2(a,-.5f):new Vector2(-.5f,a);Assert.IsTrue(Contains(m,p),"wet edge gap mask "+mask+" direction "+direction+" at "+a);}});
        }
        [Test] public void AdjacentWetCellsShareExactlyTheSameBoundaryWithoutAnInset()
        {
            // Exhaustive compatible east/west mask pairs; neither shared edge is dry.
            for(int left=0;left<16;left++)if((left&2)==0)for(int right=0;right<16;right++)if((right&8)==0)
            {Mesh a=null,b=null;try{a=Create(left);b=Create(right);for(int i=0;i<=20;i++){float z=-.5f+i/20f;Assert.IsTrue(Contains(a,new Vector2(.5f,z)));Assert.IsTrue(Contains(b,new Vector2(-.5f,z)));}}finally{if(a!=null)Object.DestroyImmediate(a);if(b!=null)Object.DestroyImmediate(b);}}
        }
        [Test] public void MissingDiagonalAloneNeverCreatesAHoleAtWetCardinalJunction()
        {
            // This API deliberately has no diagonal bit: wet cardinal neighbors
            // keep a full square corner even if external diagonal land is dry.
            Mesh m=null;try{m=Create(0);Assert.AreEqual(1,Area(m),Epsilon);foreach(var corner in Corners)Assert.IsTrue(Contains(m,corner));}
            finally{if(m!=null)Object.DestroyImmediate(m);}
        }
        [Test] public void RoundingRemovesOnlyTheSmallConvexCornerArea()
        {
            All((mask,m)=>{int count=Rounded(mask);float expected=1-count*Radius*Radius*(1-Mathf.PI/4);float actual=Area(m);
                Assert.LessOrEqual(actual,expected+Epsilon);Assert.GreaterOrEqual(actual,expected-count*.0003f-Epsilon);
                if(count==0)Assert.AreEqual(1,actual,Epsilon);else Assert.Less(actual,1);});
        }
        [Test] public void CoreAndStraightBandsRemainFilledForEveryMask()
        {All((mask,m)=>{for(int y=-10;y<=10;y++)for(int x=-10;x<=10;x++){var p=new Vector2(x*.05f,y*.05f);if(Mathf.Abs(p.x)<=.4f||Mathf.Abs(p.y)<=.4f)Assert.IsTrue(Contains(m,p),"interior gap mask "+mask+" point "+p);}});}
        [Test] public void EveryTriangleIsValidNondegenerateAndFacesUp()
        {
            All((mask,m)=>{var v=m.vertices;var t=m.triangles;Assert.AreEqual(0,t.Length%3);for(int i=0;i<t.Length;i+=3)
            {for(int k=0;k<3;k++)Assert.That(t[i+k],Is.InRange(0,v.Length-1));var normal=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);Assert.Greater(normal.y,.0000001f);Assert.AreEqual(0,normal.x,Epsilon);Assert.AreEqual(0,normal.z,Epsilon);}});
        }
        [Test] public void VertexNormalsAndUVsMatchTheUpwardLocalCellCoordinates()
        {All((mask,m)=>{var vertices=m.vertices;var uv=m.uv;var normals=m.normals;Assert.AreEqual(vertices.Length,uv.Length);Assert.AreEqual(vertices.Length,normals.Length);for(int i=0;i<vertices.Length;i++){Assert.Less(Vector3.Distance(Vector3.up,normals[i]),Epsilon);Assert.AreEqual(vertices[i].x+.5f,uv[i].x,Epsilon);Assert.AreEqual(vertices[i].z+.5f,uv[i].y,Epsilon);}});}
        [Test] public void TriangulationFormsOneClosedDiskWithNoOverlappingInternalEdges()
        {
            All((mask,m)=>{var edges=new Dictionary<(int,int),int>();var t=m.triangles;var used=new HashSet<int>();for(int i=0;i<t.Length;i+=3)for(int k=0;k<3;k++)
            {int a=t[i+k],b=t[i+(k+1)%3];used.Add(a);var key=(Math.Min(a,b),Math.Max(a,b));edges.TryGetValue(key,out int count);edges[key]=count+1;}
            Assert.AreEqual(m.vertexCount,used.Count);Assert.IsTrue(edges.Values.All(n=>n==1||n==2));Assert.AreEqual(1,used.Count-edges.Count+t.Length/3,"single disk Euler characteristic");
            Assert.GreaterOrEqual(edges.Count(e=>e.Value==1),4);});
        }
        [Test] public void RotatingMaskAndGeometryByOneCardinalStepPreservesTheShape()
        {
            for(int mask=0;mask<16;mask++)
            {Mesh a=null,b=null;try{a=Create(mask);b=Create(((mask<<1)|(mask>>3))&15);Assert.AreEqual(a.vertexCount,b.vertexCount);foreach(var v in a.vertices)Assert.IsTrue(Has(b,new Vector2(v.z,-v.x)),"rotated vertex mask "+mask+" "+v);Assert.AreEqual(Area(a),Area(b),Epsilon);}finally{if(a!=null)Object.DestroyImmediate(a);if(b!=null)Object.DestroyImmediate(b);}}
        }
        [Test] public void InvalidMasksAreRejectedRatherThanSilentlyClamped()
        {foreach(int mask in new[]{-1,int.MinValue,16,31,int.MaxValue})Assert.Throws<ArgumentOutOfRangeException>(()=>Create(mask));}
        [Test] public void EachCreateReturnsDeterministicCallerOwnedMeshAndDisposalIsIndependent()
        {
            Mesh a=null,b=null;try{a=Create(15);b=Create(15);Assert.AreNotSame(a,b);CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.triangles,b.triangles);CollectionAssert.AreEqual(a.uv,b.uv);
                Object.DestroyImmediate(a);a=null;Assert.IsTrue(b!=null);Assert.IsTrue(Contains(b,Vector2.zero));}
            finally{if(a!=null)Object.DestroyImmediate(a);if(b!=null)Object.DestroyImmediate(b);}
        }
        [Test] public void BuildingAllMasksNeverWritesNativeGroundOrEntityState()
        {
            using(var scope=new EntityEquipmentContentFixture())
            {
                var zone=new OverworldZoneManager(scope.Factory,64).GetZone("Overworld.3.7.0");var entities=zone.GetReadOnlyEntities().ToArray();var positions=entities.Select(zone.GetEntityPosition).ToArray();
                string tiles=zone.TileState.ToSaveString();int version=zone.EntityVersion;All((mask,m)=>Assert.IsTrue(Contains(m,Vector2.zero)));
                Assert.AreEqual(tiles,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);CollectionAssert.AreEquivalent(entities,zone.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,entities.Select(zone.GetEntityPosition));
            }
        }
    }
}
