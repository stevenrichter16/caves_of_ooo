using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Rendering
{
    /// <summary>Build-visible, offline-baked mesh replacements. Materials and native
    /// entity ownership remain with the existing presentation libraries.</summary>
    [CreateAssetMenu(menuName = "Caves of Ooo/Voxel World Mesh Catalog")]
    public sealed class VoxelWorldMeshCatalog : ScriptableObject
    {
        public const string ResourcePath = "VoxelWorld/Library";
        [Serializable] public sealed class Binding
        {
            public Mesh Source, Voxel;
            public float VoxelSize, WorldVoxelSize;
            public string SourceKey;
            // Captured by the offline builder. Runtime validation must never
            // request CPU arrays from non-readable imported character meshes.
            public Matrix4x4[] SourceBindposes;
        }
        public Binding[] Bindings;
        [NonSerialized] Dictionary<Mesh, Mesh> index;
        public void InvalidateCaches() => index = null;
        void OnValidate() => InvalidateCaches();
        public void Validate()
        {
            if (Bindings == null || Bindings.Length == 0)
                throw new InvalidOperationException("Voxel mesh catalog is empty.");
            var next = new Dictionary<Mesh, Mesh>(); var targets = new HashSet<Mesh>();
            var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in Bindings)
            {
                if (row == null || row.Source == null || row.Voxel == null || row.Source == row.Voxel
                    || row.Voxel.vertexCount == 0 || !row.Voxel.isReadable || row.Voxel.subMeshCount != row.Source.subMeshCount
                    || !VoxelWorldMeshBaker.IsFinite(row.VoxelSize) || row.VoxelSize <= 0
                    || !SafeSourceKey(row.SourceKey) || !sourceKeys.Add(row.SourceKey) || next.ContainsKey(row.Source))
                    throw new InvalidOperationException("Voxel mesh binding is missing, duplicated or incompatible.");
                var originalPoses=row.SourceBindposes??Array.Empty<Matrix4x4>();var replacementPoses=row.Voxel.bindposes;
                if(originalPoses.Length!=row.Source.bindposeCount||originalPoses.Length!=replacementPoses.Length
                    || (originalPoses.Length>0&&row.Voxel.boneWeights.Length!=row.Voxel.vertexCount))
                    throw new InvalidOperationException("Voxel skin override lost its original bindposes or vertex weights.");
                for(int bone=0;bone<originalPoses.Length;bone++)for(int element=0;element<16;element++)
                    if(originalPoses[bone][element]!=replacementPoses[bone][element])
                        throw new InvalidOperationException("Voxel skin override changed a native bindpose.");
                next.Add(row.Source, row.Voxel); targets.Add(row.Voxel);
            }
            foreach (var source in next.Keys)
                if (targets.Contains(source)) throw new InvalidOperationException("Voxel mesh mappings must not form chains or cycles.");
            index = next;
        }
        static bool SafeSourceKey(string key)
        {
            // Also used as an editor output filename by the toolkit importer.
            // Keep one portable component, never a path or platform-dependent
            // filename containing separators, dot segments, colons or spaces.
            if(string.IsNullOrEmpty(key)||key.Length>180)return false;
            foreach(char c in key)
                if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='_'||c=='-'))return false;
            return true;
        }
        public Mesh Resolve(Mesh source)
        {
            if (index == null) Validate();
            return source != null && index.TryGetValue(source, out var replacement) ? replacement : source;
        }
        /// <summary>Changes only mesh references on an owned instantiated hierarchy.
        /// Unknown objects and collision meshes retain their existing behavior.</summary>
        public int Apply(GameObject ownedInstance)
        {
            if (ownedInstance == null) throw new ArgumentNullException(nameof(ownedInstance));
            if (index == null) Validate();
            int count = 0;
            foreach (var filter in ownedInstance.GetComponentsInChildren<MeshFilter>(true))
            {
                var old = filter.sharedMesh; var replacement = Resolve(old);
                if (old != replacement) { filter.sharedMesh = replacement; count++; }
            }
            foreach (var skin in ownedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var old = skin.sharedMesh; var replacement = Resolve(old);
                if (old == replacement) continue;
                // Unity may derive renderer bounds when assigning a new mesh. Keep
                // the animator's authored motion envelope, enlarged only if needed.
                var envelope = skin.localBounds;
                skin.sharedMesh = replacement;
                envelope.Encapsulate(replacement.bounds.min); envelope.Encapsulate(replacement.bounds.max);
                skin.localBounds = envelope; count++;
            }
            return count;
        }
    }

    /// <summary>Deterministic mesh conversion used only by the explicit editor builder.
    /// Kept independent of AssetDatabase so geometry/skin contracts have EditMode tests.
    /// No presenter calls this class: gameplay only reads the baked catalog.</summary>
    public static class VoxelWorldMeshBaker
    {
        const long MaxGridCells = 1500000, MaxTriangleTests = 80000000;
        const int MaxOutputVertices = 4000000;
        struct Triangle
        {
            public Vector3 A, B, C, Normal;
            public Vector2 UA, UB, UC;
            public BoneWeight WA, WB, WC;
            public int Material;
        }
        struct Sample
        {
            public int Material, Bone;
            public Vector2 UV;
            public float Distance;
        }
        struct Crossing { public float X; public int Direction; public Sample Surface; }
        struct FaceKey : IEquatable<FaceKey>, IComparable<FaceKey>
        {
            public int Axis, Sign, Plane, Material, Bone, U, V;
            public bool Equals(FaceKey o) => Axis==o.Axis&&Sign==o.Sign&&Plane==o.Plane&&Material==o.Material&&Bone==o.Bone&&U==o.U&&V==o.V;
            public override bool Equals(object o) => o is FaceKey value && Equals(value);
            public override int GetHashCode()
            { unchecked { int h=Axis;h=h*397+Sign;h=h*397+Plane;h=h*397+Material;h=h*397+Bone;h=h*397+U;return h*397+V; } }
            public int CompareTo(FaceKey o)
            {
                int n=Axis.CompareTo(o.Axis);if(n!=0)return n;n=Sign.CompareTo(o.Sign);if(n!=0)return n;
                n=Plane.CompareTo(o.Plane);if(n!=0)return n;n=Material.CompareTo(o.Material);if(n!=0)return n;
                n=Bone.CompareTo(o.Bone);if(n!=0)return n;n=U.CompareTo(o.U);return n!=0?n:V.CompareTo(o.V);
            }
        }
        public static bool IsFinite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        static bool Finite(Vector3 p) => IsFinite(p.x) && IsFinite(p.y) && IsFinite(p.z);
        static bool Finite(Vector2 p) => IsFinite(p.x) && IsFinite(p.y);

        public static Mesh Bake(Mesh source, float voxelSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!IsFinite(voxelSize) || voxelSize <= 0) throw new ArgumentException("Voxel size must be positive and finite.");
            if (source.blendShapeCount != 0) throw new ArgumentException("Blend shapes require an explicit voxel deformation recipe.");
            var vertices = source.vertices;
            if (vertices.Length == 0 || source.subMeshCount == 0) throw new ArgumentException("Source mesh is empty.");
            Vector3 low=vertices[0],high=vertices[0];
            foreach(var p in vertices)
            { if(!Finite(p))throw new ArgumentException("Source has non-finite vertices.");low=Vector3.Min(low,p);high=Vector3.Max(high,p); }
            var gridMin=Index(low,voxelSize);var gridMax=Index(high,voxelSize);
            long nx=(long)gridMax.x-gridMin.x+1,ny=(long)gridMax.y-gridMin.y+1,nz=(long)gridMax.z-gridMin.z+1;
            if(nx>MaxGridCells||ny>MaxGridCells||nz>MaxGridCells||nx*ny>MaxGridCells||nx*ny*nz>MaxGridCells)
                throw new ArgumentException("Source exceeds the bounded voxel grid budget.");
            var uv=source.uv; if(uv.Length!=0&&uv.Length!=vertices.Length)throw new ArgumentException("Source UV count is invalid.");
            foreach(var point in uv)if(!Finite(point))throw new ArgumentException("Source has non-finite UVs.");
            var weights=source.boneWeights;var poses=source.bindposes;bool skinned=weights.Length>0;
            if(skinned&&(weights.Length!=vertices.Length||poses.Length==0))throw new ArgumentException("Source skin has incompatible weights and bindposes.");
            if(skinned)foreach(var weight in weights)ValidateWeight(weight,poses.Length);
            foreach(var pose in poses)for(int i=0;i<16;i++)if(!IsFinite(pose[i]))throw new ArgumentException("Source bindpose is not finite.");
            var triangles=new List<Triangle>();
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                if(source.GetTopology(sub)!=MeshTopology.Triangles)throw new ArgumentException("Voxel sources require triangle topology.");
                var indices=source.GetTriangles(sub);
                for(int i=0;i<indices.Length;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    var normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
                    if(normal.sqrMagnitude<1e-20f)continue;
                    triangles.Add(new Triangle {A=vertices[a],B=vertices[b],C=vertices[c],Normal=normal,Material=sub,
                        UA=uv.Length==0?Vector2.zero:uv[a],UB=uv.Length==0?Vector2.zero:uv[b],UC=uv.Length==0?Vector2.zero:uv[c],
                        WA=skinned?weights[a]:default,WB=skinned?weights[b]:default,WC=skinned?weights[c]:default});
                }
            }
            if(triangles.Count==0)throw new ArgumentException("Source has no non-degenerate triangles.");
            var cells=new Dictionary<Vector3Int,Sample>();long work=0;
            FillInterior(triangles,voxelSize,skinned,cells,ref work);
            var solidCells=new HashSet<Vector3Int>(cells.Keys);
            foreach(var tri in triangles) RasterSurface(tri,voxelSize,skinned,cells,solidCells,ref work);
            if(cells.Count==0)throw new ArgumentException("Source produced no voxel geometry.");
            var result=BuildSurface(cells,voxelSize,source.subMeshCount,skinned);
            result.name="VX_"+source.name;result.bindposes=poses;return result;
        }
        static void ValidateWeight(BoneWeight w,int count)
        {
            float sum=0;
            for(int i=0;i<4;i++)
            {
                float weight=Weight(w,i);int bone=Bone(w,i);
                if(!IsFinite(weight)||weight<0||(weight>0&&(bone<0||bone>=count)))throw new ArgumentException("Source bone weight is invalid.");
                sum+=weight;
            }
            if(sum<=0)throw new ArgumentException("Source contains an unweighted skinned vertex.");
        }
        static float Weight(BoneWeight w,int i)=>i==0?w.weight0:i==1?w.weight1:i==2?w.weight2:w.weight3;
        static int Bone(BoneWeight w,int i)=>i==0?w.boneIndex0:i==1?w.boneIndex1:i==2?w.boneIndex2:w.boneIndex3;
        static Vector3Int Index(Vector3 p,float q)
        {
            var value=p/q;
            if(!Finite(value)||Mathf.Abs(value.x)>10000000||Mathf.Abs(value.y)>10000000||Mathf.Abs(value.z)>10000000)
                throw new ArgumentException("Source exceeds finite voxel coordinate bounds.");
            return new Vector3Int(Mathf.FloorToInt(value.x),Mathf.FloorToInt(value.y),Mathf.FloorToInt(value.z));
        }
        static Vector3 Centre(Vector3Int p,float q)=>new Vector3((p.x+.5f)*q,(p.y+.5f)*q,(p.z+.5f)*q);
        static void Budget(ref long work,long add=1)
        {work+=add;if(work>MaxTriangleTests)throw new ArgumentException("Source exceeds the bounded triangle sampling budget.");}
        static void RasterSurface(Triangle tri,float q,bool skinned,Dictionary<Vector3Int,Sample> cells,HashSet<Vector3Int> solidCells,ref long work)
        {
            var low=Vector3.Min(tri.A,Vector3.Min(tri.B,tri.C));var high=Vector3.Max(tri.A,Vector3.Max(tri.B,tri.C));
            var from=new Vector3Int();var to=new Vector3Int();var preferred=new Vector3Int();int ambiguousAxes=0;float epsilon=q*.00001f;
            for(int axis=0;axis<3;axis++)
            {
                if(high[axis]-low[axis]<epsilon)
                {
                    int single=Mathf.FloorToInt((low[axis]-Mathf.Sign(tri.Normal[axis])*epsilon)/q);preferred[axis]=single;
                    int boundary=Mathf.RoundToInt(low[axis]/q);
                    if(Mathf.Abs(low[axis]/q-boundary)<.0001f)
                    {from[axis]=boundary-1;to[axis]=boundary;ambiguousAxes|=1<<axis;}
                    else {from[axis]=single;to[axis]=single;}
                }
                else {from[axis]=Mathf.FloorToInt((low[axis]+epsilon)/q);to[axis]=Mathf.FloorToInt((high[axis]-epsilon)/q);}
            }
            long candidates=((long)to.x-from.x+1)*((long)to.y-from.y+1)*((long)to.z-from.z+1);Budget(ref work,candidates);
            for(int x=from.x;x<=to.x;x++)for(int y=from.y;y<=to.y;y++)for(int z=from.z;z<=to.z;z++)
            {
                var key=new Vector3Int(x,y,z);var centre=Centre(key,q);
                bool reject=false;
                for(int axis=0;axis<3;axis++)if((ambiguousAxes&(1<<axis))!=0)
                {
                    var opposite=key;opposite[axis]=key[axis]==from[axis]?to[axis]:from[axis];
                    // Closed volume, not source winding, decides which side of
                    // a boundary belongs to the solid. Truly open thin faces
                    // still receive exactly one voxel layer as a fallback.
                    bool here=solidCells.Contains(key),there=solidCells.Contains(opposite);
                    if((!here&&there)||((here==there)&&key[axis]!=preferred[axis])){reject=true;break;}
                }
                if(reject)continue;
                if(!TriangleBox(tri,centre,q*.500001f))continue;
                var sample=At(tri,centre,skinned);
                if(!cells.TryGetValue(key,out var old)||sample.Distance<old.Distance)cells[key]=sample;
            }
        }
        static bool TriangleBox(Triangle t,Vector3 centre,float half)
        {
            var a=t.A-centre;var b=t.B-centre;var c=t.C-centre;
            for(int axis=0;axis<3;axis++)if(Mathf.Min(a[axis],Mathf.Min(b[axis],c[axis]))>half||Mathf.Max(a[axis],Mathf.Max(b[axis],c[axis]))<-half)return false;
            if(!OverlapAxis(a,b,c,t.Normal,half))return false;
            var ab=b-a;var bc=c-b;var ca=a-c;
            return EdgeAxes(a,b,c,ab,half)&&EdgeAxes(a,b,c,bc,half)&&EdgeAxes(a,b,c,ca,half);
        }
        static bool EdgeAxes(Vector3 a,Vector3 b,Vector3 c,Vector3 edge,float half)
        {return OverlapAxis(a,b,c,new Vector3(0,edge.z,-edge.y),half)&&OverlapAxis(a,b,c,new Vector3(-edge.z,0,edge.x),half)&&OverlapAxis(a,b,c,new Vector3(edge.y,-edge.x,0),half);}
        static bool OverlapAxis(Vector3 a,Vector3 b,Vector3 c,Vector3 axis,float half)
        {
            float pa=Vector3.Dot(a,axis),pb=Vector3.Dot(b,axis),pc=Vector3.Dot(c,axis),r=half*(Mathf.Abs(axis.x)+Mathf.Abs(axis.y)+Mathf.Abs(axis.z));
            return Mathf.Min(pa,Mathf.Min(pb,pc))<=r&&Mathf.Max(pa,Mathf.Max(pb,pc))>=-r;
        }
        static Sample At(Triangle t,Vector3 p,bool skinned)
        {
            var bary=ClosestBarycentric(t.A,t.B,t.C,p);var hit=t.A*bary.x+t.B*bary.y+t.C*bary.z;
            var uv=t.UA*bary.x+t.UB*bary.y+t.UC*bary.z;
            return new Sample {Material=t.Material,Bone=skinned?DominantBone(t,bary):-1,
                UV=new Vector2(Mathf.Round(uv.x*65536f)/65536f,Mathf.Round(uv.y*65536f)/65536f),Distance=(p-hit).sqrMagnitude};
        }
        static Vector3 ClosestBarycentric(Vector3 a,Vector3 b,Vector3 c,Vector3 p)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0&&d2<=0)return new Vector3(1,0,0);
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return new Vector3(0,1,0);
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0){float v=d1/(d1-d3);return new Vector3(1-v,v,0);}
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return new Vector3(0,0,1);
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0){float w=d2/(d2-d6);return new Vector3(1-w,0,w);}
            float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){float w=(d4-d3)/((d4-d3)+(d5-d6));return new Vector3(0,1-w,w);}
            float denominator=1/(va+vb+vc);float vv=vb*denominator,ww=vc*denominator;return new Vector3(1-vv-ww,vv,ww);
        }
        static int DominantBone(Triangle t,Vector3 bary)
        {
            // Twelve candidates suffice for the legacy four-weight source format.
            int best=0;float bestWeight=-1;
            for(int vertex=0;vertex<3;vertex++)for(int slot=0;slot<4;slot++)
            {
                var candidate=vertex==0?t.WA:vertex==1?t.WB:t.WC;int bone=Bone(candidate,slot);float sum=0;
                for(int v=0;v<3;v++)for(int s=0;s<4;s++)
                {var weight=v==0?t.WA:v==1?t.WB:t.WC;if(Bone(weight,s)==bone)sum+=Weight(weight,s)*bary[v];}
                if(sum>bestWeight||(sum==bestWeight&&bone<best)){best=bone;bestWeight=sum;}
            }
            return best;
        }
        static void FillInterior(List<Triangle> triangles,float q,bool skinned,Dictionary<Vector3Int,Sample> cells,ref long work)
        {
            var rows=new Dictionary<Vector2Int,List<Crossing>>();
            foreach(var tri in triangles)
            {
                if(Mathf.Abs(tri.Normal.x)<1e-10f)continue;
                float lowY=Mathf.Min(tri.A.y,Mathf.Min(tri.B.y,tri.C.y)),highY=Mathf.Max(tri.A.y,Mathf.Max(tri.B.y,tri.C.y));
                float lowZ=Mathf.Min(tri.A.z,Mathf.Min(tri.B.z,tri.C.z)),highZ=Mathf.Max(tri.A.z,Mathf.Max(tri.B.z,tri.C.z));
                int y0=Mathf.CeilToInt(lowY/q-.5f),y1=Mathf.FloorToInt(highY/q-.5f),z0=Mathf.CeilToInt(lowZ/q-.5f),z1=Mathf.FloorToInt(highZ/q-.5f);
                Budget(ref work,((long)y1-y0+1)*((long)z1-z0+1));
                for(int y=y0;y<=y1;y++)for(int z=z0;z<=z1;z++)
                {
                    var yz=new Vector2((y+.5f)*q,(z+.5f)*q);
                    if(!ProjectedHit(tri,yz,out float x))continue;
                    var key=new Vector2Int(y,z);if(!rows.TryGetValue(key,out var row)){row=new List<Crossing>();rows.Add(key,row);}
                    row.Add(new Crossing {X=x,Direction=tri.Normal.x>0?1:-1,Surface=At(tri,new Vector3(x,yz.x,yz.y),skinned)});
                }
            }
            foreach(var pair in rows)
            {
                var row=pair.Value;row.Sort((a,b)=>a.X.CompareTo(b.X));int balance=0;foreach(var cross in row)balance+=cross.Direction;
                // An unmatched ray is an open surface, never a solid half-space.
                if(balance!=0)continue;
                int winding=0;
                for(int i=0;i<row.Count-1;i++)
                {
                    winding+=row[i].Direction;float left=row[i].X,right=row[i+1].X;
                    if(winding==0||right-left<q*.00001f)continue;
                    int first=Mathf.CeilToInt(left/q-.5f),last=Mathf.FloorToInt(right/q-.5f);
                    for(int x=first;x<=last;x++)
                    {
                        var key=new Vector3Int(x,pair.Key.x,pair.Key.y);if(cells.ContainsKey(key))continue;
                        float position=(x+.5f)*q;var sample=position-left<right-position?row[i].Surface:row[i+1].Surface;
                        sample.Distance=float.PositiveInfinity;
                        cells.Add(key,sample);
                    }
                }
            }
        }
        static float Edge(Vector2 a,Vector2 b,Vector2 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
        static bool TopLeft(Vector2 a,Vector2 b)=>b.y>a.y||(b.y==a.y&&b.x<a.x);
        static bool InsideEdge(float edge,Vector2 a,Vector2 b)=>edge>0||(edge==0&&TopLeft(a,b));
        static bool ProjectedHit(Triangle tri,Vector2 p,out float x)
        {
            var a=new Vector2(tri.A.y,tri.A.z);var b=new Vector2(tri.B.y,tri.B.z);var c=new Vector2(tri.C.y,tri.C.z);
            float bx=tri.B.x,cx=tri.C.x,area=Edge(a,b,c);
            if(area<0){var swap=b;b=c;c=swap;float sx=bx;bx=cx;cx=sx;area=-area;}
            float e0=Edge(b,c,p),e1=Edge(c,a,p),e2=Edge(a,b,p);
            if(!InsideEdge(e0,b,c)||!InsideEdge(e1,c,a)||!InsideEdge(e2,a,b)){x=0;return false;}
            x=(e0*tri.A.x+e1*bx+e2*cx)/area;return true;
        }
        static Mesh BuildSurface(Dictionary<Vector3Int,Sample> cells,float q,int submeshCount,bool skinned)
        {
            var layers=new Dictionary<FaceKey,HashSet<Vector2Int>>();
            foreach(var pair in cells.OrderBy(p=>p.Key.x).ThenBy(p=>p.Key.y).ThenBy(p=>p.Key.z))
            {
                var p=pair.Key;var sample=pair.Value;
                for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
                {
                    var adjacent=p;adjacent[axis]+=sign;
                    // A different rigid bone can move away. Preserve the cap at
                    // joints even while the two cells touch in their bind pose.
                    if(cells.TryGetValue(adjacent,out var neighbor)&&neighbor.Bone==sample.Bone)continue;
                    var key=new FaceKey {Axis=axis,Sign=sign,Plane=p[axis]+(sign>0?1:0),Material=sample.Material,Bone=sample.Bone,
                        U=Mathf.RoundToInt(sample.UV.x*65536),V=Mathf.RoundToInt(sample.UV.y*65536)};
                    if(!layers.TryGetValue(key,out var faceCells)){faceCells=new HashSet<Vector2Int>();layers.Add(key,faceCells);}
                    faceCells.Add(new Vector2Int(p[(axis+1)%3],p[(axis+2)%3]));
                }
            }
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();
            var indices=new List<int>[submeshCount];for(int i=0;i<indices.Length;i++)indices[i]=new List<int>();
            foreach(var pair in layers.OrderBy(p=>p.Key))
            {
                var key=pair.Key;var remaining=pair.Value;
                foreach(var start in remaining.OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray())
                {
                    if(!remaining.Contains(start))continue;
                    int width=1;while(remaining.Contains(new Vector2Int(start.x+width,start.y)))width++;
                    int height=1;bool grow=true;
                    while(grow){for(int u=0;u<width;u++)if(!remaining.Contains(new Vector2Int(start.x+u,start.y+height))){grow=false;break;}if(grow)height++;}
                    for(int u=0;u<width;u++)for(int v=0;v<height;v++)remaining.Remove(new Vector2Int(start.x+u,start.y+v));
                    if(vertices.Count+4>MaxOutputVertices)throw new ArgumentException("Voxel surface exceeds the output vertex budget.");
                    int offset=vertices.Count;var origin=Vector3.zero;origin[key.Axis]=key.Plane*q;origin[(key.Axis+1)%3]=start.x*q;origin[(key.Axis+2)%3]=start.y*q;
                    var across=Vector3.zero;across[(key.Axis+1)%3]=width*q;var up=Vector3.zero;up[(key.Axis+2)%3]=height*q;
                    vertices.Add(origin);vertices.Add(origin+across);vertices.Add(origin+across+up);vertices.Add(origin+up);
                    var normal=Vector3.zero;normal[key.Axis]=key.Sign;
                    for(int corner=0;corner<4;corner++){normals.Add(normal);uv.Add(new Vector2(key.U/65536f,key.V/65536f));if(skinned)weights.Add(new BoneWeight {boneIndex0=key.Bone,weight0=1});}
                    var output=indices[key.Material];
                    if(key.Sign>0)output.AddRange(new[]{offset,offset+1,offset+2,offset,offset+2,offset+3});
                    else output.AddRange(new[]{offset,offset+2,offset+1,offset,offset+3,offset+2});
                }
            }
            var mesh=new Mesh {indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            try
            {
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=submeshCount;
                for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i,false);
                if(skinned)mesh.boneWeights=weights.ToArray();mesh.RecalculateBounds();return mesh;
            }
            catch {UnityEngine.Object.DestroyImmediate(mesh);throw;}
        }
    }
}
