using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>One-cell water geometry with cardinal-native shoreline corners.
    /// Each mesh belongs to its caller; the presenter caches/disposes its16
    /// choices. No static Unity objects, native state access or random selection.</summary>
    internal static class SpawnRing3DWaterMesh
    {
        const float Radius=.1f;
        const int ArcSegments=8;
        // Clockwise when viewed from above: NW, NE, SE, SW. A corner can
        // recede only when neither of its cardinal edges touches wet ground.
        static readonly int[] AdjacentDryBits={8|1,1|2,2|4,4|8};
        static readonly Vector2[] CornerSigns={new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1),new Vector2(-1,-1)};

        /// <param name="dryNeighbourMask">Dry N(+Z)=1,E(+X)=2,S(-Z)=4,W(-X)=8.</param>
        public static Mesh Create(int dryNeighbourMask)
        {
            if(dryNeighbourMask<0||dryNeighbourMask>15)
                throw new ArgumentOutOfRangeException(nameof(dryNeighbourMask),dryNeighbourMask,"Only four cardinal dry-neighbour bits are valid.");
            var perimeter=new List<Vector3>(36);
            for(int corner=0;corner<4;corner++)
            {
                Vector2 sign=CornerSigns[corner];
                if((dryNeighbourMask&AdjacentDryBits[corner])!=AdjacentDryBits[corner])
                {
                    perimeter.Add(new Vector3(sign.x*.5f,0,sign.y*.5f));
                    continue;
                }
                Vector2 centre=sign*(.5f-Radius);
                float start=180-corner*90;
                for(int segment=0;segment<=ArcSegments;segment++)
                {
                    float angle=(start-90f*segment/ArcSegments)*Mathf.Deg2Rad;
                    perimeter.Add(new Vector3(centre.x+Radius*Mathf.Cos(angle),0,centre.y+Radius*Mathf.Sin(angle)));
                }
            }
            // The perimeter is convex, so a centre fan neither overlaps nor
            // punches holes. Clockwise XZ order gives upward-facing triangles.
            int count=perimeter.Count;
            var vertices=new Vector3[count+1];var normals=new Vector3[count+1];var uv=new Vector2[count+1];
            normals[0]=Vector3.up;uv[0]=new Vector2(.5f,.5f);
            for(int i=0;i<count;i++)
            {
                Vector3 v=perimeter[i];vertices[i+1]=v;normals[i+1]=Vector3.up;
                uv[i+1]=new Vector2(v.x+.5f,v.z+.5f);
            }
            var triangles=new int[count*3];
            for(int i=0;i<count;i++)
            {triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%count+1;}
            var mesh=new Mesh{name="Spawn-ring water mask "+dryNeighbourMask,hideFlags=HideFlags.DontSave};
            try
            {
                mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
                return mesh;
            }
            catch
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
