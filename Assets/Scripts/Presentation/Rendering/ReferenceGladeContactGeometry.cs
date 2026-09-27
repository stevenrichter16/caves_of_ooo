using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
    /// <summary>Pure mesh-base extraction and bounded soft-ground raster math.
    /// Input arrays are borrowed; only the explicitly supplied byte field is written.</summary>
    public static class ReferenceGladeContactGeometry
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
            =>Rasterize(placed,target,maximumWrites,out _);
        // Per-call work observation shares the exact runtime body. No cache,
        // global counter or timing policy is introduced by this diagnostic seam.
        internal static bool Rasterize(IReadOnlyList<Placed> placed,byte[] target,int maximumWrites,out int evaluatedSamples)
        {
            evaluatedSamples=0;
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
                for(int y=y0;y<=y1;y++)
                {
                    float pz=(y+.5f)/SamplesPerCell;
                    float dz=Math.Max(0,Math.Max(rect.MinZ-pz,pz-rect.MaxZ));
                    int row=y*Width;
                    for(int x=x0;x<=x1;x++)
                    {
                        int at=row+x;
                        // Max composition cannot change an already opaque sample.
                        // Original full-batch work admission above is unchanged.
                        if(target[at]==255)continue;
                        evaluatedSamples++;
                        float px=(x+.5f)/SamplesPerCell;
                        float dx=Math.Max(0,Math.Max(rect.MinX-px,px-rect.MaxX));
                        float distance=(float)Math.Sqrt(dx*dx+dz*dz);if(distance>=BlurRadius)continue;
                        float t=1-distance/BlurRadius;byte value=(byte)Math.Round(255*t*t*(3-2*t));
                        if(value>target[at])target[at]=value;
                    }
                }
            }
            return true;
        }
    }
}
