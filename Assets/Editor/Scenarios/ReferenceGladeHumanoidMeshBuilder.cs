#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CavesOfOoo.Editor
{
    /// <summary>Offline glade-only body forms on the exact existing humanoid
    /// bindposes. Owns generated geometry, never source skeletons or sockets.</summary>
    internal static class ReferenceGladeHumanoidMeshBuilder
    {
        internal sealed class Plan
        {
            internal readonly List<Vector3> Vertices=new List<Vector3>();
            internal readonly List<Vector2> Uvs=new List<Vector2>();
            internal readonly List<int> Triangles=new List<int>();
            internal readonly List<BoneWeight> Weights=new List<BoneWeight>();
            internal Matrix4x4[] Bindposes;
            internal void Fill(Mesh ownedMesh)
            {
                if(ownedMesh==null)throw new ArgumentNullException(nameof(ownedMesh));
                // CopySerialized can preserve a destination Mesh's old native
                // vertex buffer while copying its new submesh metadata. Write
                // the owned persistent mesh through the actual geometry API.
                ownedMesh.Clear(false);
                ownedMesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt16;
                ownedMesh.SetVertices(Vertices);ownedMesh.SetUVs(0,Uvs);
                ownedMesh.SetTriangles(Triangles,0);ownedMesh.boneWeights=Weights.ToArray();
                ownedMesh.bindposes=Bindposes;ownedMesh.RecalculateNormals();ownedMesh.RecalculateBounds();
                if(ownedMesh.vertexCount!=Vertices.Count||ownedMesh.boneWeights.Length!=Weights.Count
                    ||ownedMesh.GetIndexCount(0)!=(uint)Triangles.Count)
                    throw new InvalidOperationException("Authored humanoid mesh buffer write did not complete.");
            }
        }
        internal static Plan Prepare(string id,GameObject prefab,SkinnedMeshRenderer skin,Mesh source,Mesh cube)
        {
            if(id!="ring-player"&&id!="ring-sien"&&id!="ring-nam")throw new ArgumentException("Only three exact glade humanoids have authored forms.");
            if(source==null||source.subMeshCount!=1||source.bindposeCount!=skin.bones.Length||skin.bones.Any(b=>b==null))throw new InvalidOperationException("Incomplete original humanoid rig.");
            var indices=new Dictionary<string,int>(StringComparer.Ordinal);
            for(int i=0;i<skin.bones.Length;i++)if(!indices.TryAdd(skin.bones[i].name,i))throw new InvalidOperationException("Ambiguous humanoid bone.");
            string[] required={"Root","Spine","Head","Arm.L","Arm.R","Hand.L","Hand.R","Leg.L","Leg.R"};
            if(indices.Count!=required.Length||required.Any(name=>!indices.ContainsKey(name)))throw new InvalidOperationException("Unknown humanoid skeleton.");
            var transforms=prefab.GetComponentsInChildren<Transform>(true);
            Vector3 Socket(string name)
            {
                var socket=transforms.SingleOrDefault(t=>t.name==name);
                if(socket==null)throw new InvalidOperationException("Required real equipment socket missing: "+name);
                return prefab.transform.InverseTransformPoint(socket.position);
            }
            var left=Socket("Equipment.Hand.L");var right=Socket("Equipment.Hand.R");var head=Socket("Equipment.Head");var back=Socket("Equipment.Back");
            if(left.x*right.x>=0||Mathf.Abs(back.z)<.05f||head.y<1.4f||head.y>1.9f)throw new InvalidOperationException("Unexpected original socket layout.");
            var toMesh=skin.transform.worldToLocalMatrix*prefab.transform.localToWorldMatrix;
            if(!Finite(toMesh.determinant)||Mathf.Abs(toMesh.determinant)<.00001f)throw new InvalidOperationException("Invalid bind-space transform.");
            var plan=new Plan{Bindposes=source.bindposes};var cv=cube.vertices;var ct=cube.triangles;
            int body=id=="ring-player"?16:11;const int pale=17;
            void Box(Vector3 center,Vector3 size,string bone,int color)
            {
                int start=plan.Vertices.Count,index=indices[bone];
                foreach(var v in cv)
                {
                    var point=toMesh.MultiplyPoint3x4(center+Vector3.Scale(v,size));
                    if(!Finite(point.x)||!Finite(point.y)||!Finite(point.z))throw new InvalidOperationException("Nonfinite authored form.");
                    plan.Vertices.Add(point);plan.Uvs.Add(new Vector2((color+.5f)/24f,.5f));plan.Weights.Add(new BoneWeight{boneIndex0=index,weight0=1});
                }
                for(int i=0;i<ct.Length;i+=3)
                {plan.Triangles.Add(start+ct[i]);plan.Triangles.Add(start+ct[i+(toMesh.determinant>0?1:2)]);plan.Triangles.Add(start+ct[i+(toMesh.determinant>0?2:1)]);}
            }
            float torsoWidth=id=="ring-sien"?.46f:id=="ring-nam"?.41f:.43f;
            Box(new Vector3(0,1.025f,0),new Vector3(torsoWidth,.55f,.27f),"Spine",body);
            Box(new Vector3(0,.70f,0),new Vector3(.36f,.16f,.24f),"Spine",body);
            Box(new Vector3(0,1.33f,0),new Vector3(.13f,.16f,.14f),"Head",pale);
            Box(new Vector3(0,head.y-.18f,0),new Vector3(.29f,.31f,.27f),"Head",pale);
            foreach(string side in new[]{"L","R"})
            {
                var hand=side=="L"?left:right;float sign=Mathf.Sign(hand.x);
                var leg=prefab.transform.InverseTransformPoint(skin.bones[indices["Leg."+side]].position);
                if(leg.x*sign<=0||Mathf.Abs(leg.x)<.10f)throw new InvalidOperationException("Hand/leg sides disagree in the actual rig.");
                Box(new Vector3(sign*.275f,1.045f,0),new Vector3(.15f,.18f,.24f),"Arm."+side,pale);
                Box(new Vector3(sign*.335f,.85f,0),new Vector3(.15f,.30f,.18f),"Arm."+side,body);
                Box(new Vector3(sign*.40f,.65f,0),new Vector3(.14f,.24f,.17f),"Arm."+side,body);
                Box(new Vector3(hand.x,hand.y+.045f,hand.z),new Vector3(.145f,.15f,.17f),"Hand."+side,pale);
                Box(new Vector3(leg.x,.37f,0),new Vector3(.18f,.49f,.20f),"Leg."+side,body);
                Box(new Vector3(leg.x,.10f,-Mathf.Sign(back.z)*.045f),new Vector3(.20f,.17f,.29f),"Leg."+side,body);
            }
            return plan;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
#endif
