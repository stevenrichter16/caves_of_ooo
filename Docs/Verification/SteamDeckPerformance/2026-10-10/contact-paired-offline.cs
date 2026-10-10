// Frozen diagnostic for rejected contact raster candidates, 2026-10-10.
// Copy this file as Program.cs into an otherwise empty net10.0 console project.
// Run: DOTNET_TieredCompilation=0 dotnet run -c Release
// ReferenceGladeContactGeometry = rejected bands; RasterBaseline = HEAD bf3c2468b;
// RasterRows = local row-only experiment, never landed in Assets.
// All candidates receive the identical borrowed List<Placed> and separate reused targets.
// Mesh extraction is omitted because only already-placed raster inputs are compared.
// This .NET synthetic diagnostic does not establish Unity/Mono/IL2CPP/Deck performance.
using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;using System.Runtime.InteropServices;using System.Text.Json;using G=CavesOfOoo.Rendering.ReferenceGladeContactGeometry;using B=CavesOfOoo.Rendering.RasterBaseline;using R=CavesOfOoo.Rendering.RasterRows;
using Footprint=CavesOfOoo.Rendering.ReferenceGladeContactGeometry.Footprint;
using Placed=CavesOfOoo.Rendering.ReferenceGladeContactGeometry.Placed;

class Program
{
    static G.Placed Rect(float x,float z,float w,float d,int yaw=0)
        =>new(new G.Footprint(-w/2,w/2,-d/2,d/2),x,z,yaw);
    static void Main()
    {
        var results=new List<object>();
        foreach(var name in new[]{"solid-grid","tiny-grid","thin-grid","mixed-grid","mixed-overlap"})
        {
            var shapes=new List<G.Placed>();var random=new Random(177);
            for(int i=0;i<4000;i++)
            {
                float w=name=="tiny-grid"?.01f:name=="solid-grid"?.8f:name=="thin-grid"?.04f:(float)(random.NextDouble()*.9+.01);
                float d=name=="tiny-grid"?.01f:name=="solid-grid"?.6f:name=="thin-grid"?.65f:(float)(random.NextDouble()*.9+.01);
                var s=Rect((float)(random.NextDouble()*80),(float)(random.NextDouble()*25),w,d,i%4);
                shapes.Add(s);if(name=="mixed-overlap")for(int j=0;j<3;j++)shapes.Add(s);
            }
            results.Add(Run(name,shapes));
        }
        Console.WriteLine(JsonSerializer.Serialize(new {
            runtime=RuntimeInformation.FrameworkDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),os=RuntimeInformation.OSDescription,
            configuration="Release; DOTNET_TieredCompilation=0",baseline="HEAD bf3c2468b contact raster, including opaque skips",
            input="Identical borrowed List<Placed> per case; deterministic seed 177; 4,000 sampled positions with yaw i%4; mixed-overlap repeats each shape four times",
            measurement="12 warmups per algorithm; 21 measured batches per algorithm, 24 full calls each; balanced rotating order; medians in milliseconds per call; full 128,000-byte equality checked before timing",
            limitation="Synthetic identical-input CPU diagnostic on .NET 10 ARM64. Does not establish Unity Mono/IL2CPP or Steam Deck performance. The matched Unity route separately showed the candidate slower.",results
        },new JsonSerializerOptions{WriteIndented=true}));
    }
    static object Run(string name,List<G.Placed> shapes)
    {
        var baseline=new byte[G.Width*G.Height];var bands=new byte[baseline.Length];var rows=new byte[baseline.Length];
        Func<bool>[] calls={()=>B.Rasterize(shapes,baseline),()=>G.Rasterize(shapes,bands),()=>R.Rasterize(shapes,rows)};
        foreach(var call in calls)if(!call())throw new Exception("Rejected fixture input");
        if(!baseline.SequenceEqual(bands)||!baseline.SequenceEqual(rows))throw new Exception("Raster byte mismatch");
        for(int i=0;i<12;i++)foreach(var call in calls)call();
        var times=new[]{new List<double>(),new List<double>(),new List<double>()};
        for(int i=0;i<21;i++)for(int slot=0;slot<3;slot++)
        {
            int which=(i+slot)%3;times[which].Add(Time(calls[which]));
        }
        foreach(var values in times)values.Sort();
        return new {name,placements=shapes.Count,byteIdentical=true,baselineMs=times[0][10],bandsMs=times[1][10],rowReuseMs=times[2][10],bandsRatio=times[1][10]/times[0][10],rowReuseRatio=times[2][10]/times[0][10]};
    }
    static double Time(Func<bool> call)
    {
        var watch=Stopwatch.StartNew();
        for(int i=0;i<24;i++)if(!call())throw new Exception("Rejected fixture input");
        return watch.Elapsed.TotalMilliseconds/24;
    }
}

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
            =>Rasterize(placed,target,maximumWrites,out evaluatedSamples,out _);
        // Distance evaluations count actual square roots, independently of the
        // established nonopaque candidate count. Both observe this same body.
        internal static bool Rasterize(IReadOnlyList<Placed> placed,byte[] target,int maximumWrites,
            out int evaluatedSamples,out int distanceEvaluations)
        {
            evaluatedSamples=0;distanceEvaluations=0;
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
                int insideX0=x1+1,insideX1=x0-1;
                // Outside-x columns cover only the left/right soft edges. The
                // vertical interior shares one exact distance result per column;
                // corners retain the original two-axis expression.
                for(int x=x0;x<=x1;x++)
                {
                    float px=(x+.5f)/SamplesPerCell;
                    float dx=Math.Max(0,Math.Max(rect.MinX-px,px-rect.MaxX));
                    if(dx==0)
                    {
                        if(insideX0>x)insideX0=x;
                        insideX1=x;continue;
                    }
                    bool hasColumnValue=false;byte columnValue=0;
                    for(int y=y0;y<=y1;y++)
                    {
                        int at=y*Width+x;
                        // Max composition cannot change an already opaque sample.
                        // Original full-batch work admission above is unchanged.
                        if(target[at]==255)continue;
                        evaluatedSamples++;
                        float pz=(y+.5f)/SamplesPerCell;
                        float dz=Math.Max(0,Math.Max(rect.MinZ-pz,pz-rect.MaxZ));
                        byte value;
                        if(dz==0)
                        {
                            if(!hasColumnValue){columnValue=Falloff(dx,0,ref distanceEvaluations);hasColumnValue=true;}
                            value=columnValue;
                        }
                        else value=Falloff(dx,dz,ref distanceEvaluations);
                        if(value>target[at])target[at]=value;
                    }
                }
                // All inside-x samples share dx=0, so calculate their soft
                // falloff once per row. At dx=dz=0 the original byte is exactly
                // 255. Bands are disjoint: every admitted pixel is visited once.
                if(insideX0>insideX1)continue;
                for(int y=y0;y<=y1;y++)
                {
                    float pz=(y+.5f)/SamplesPerCell;
                    float dz=Math.Max(0,Math.Max(rect.MinZ-pz,pz-rect.MaxZ));
                    int row=y*Width;bool hasRowValue=false;byte rowValue=0;
                    for(int x=insideX0;x<=insideX1;x++)
                    {
                        int at=row+x;if(target[at]==255)continue;
                        evaluatedSamples++;
                        if(!hasRowValue){rowValue=dz==0?(byte)255:Falloff(0,dz,ref distanceEvaluations);hasRowValue=true;}
                        if(rowValue>target[at])target[at]=rowValue;
                    }
                }
            }
            return true;
        }
        private static byte Falloff(float dx,float dz,ref int distanceEvaluations)
        {
            distanceEvaluations++;
            float distance=(float)Math.Sqrt(dx*dx+dz*dz);if(distance>=BlurRadius)return 0;
            float t=1-distance/BlurRadius;return (byte)Math.Round(255*t*t*(3-2*t));
        }
    }
}

namespace CavesOfOoo.Rendering
{
    /// <summary>Pure mesh-base extraction and bounded soft-ground raster math.
    /// Input arrays are borrowed; only the explicitly supplied byte field is written.</summary>
    public static class RasterBaseline
    {
        public const int SamplesPerCell=8,Width=80*SamplesPerCell,Height=25*SamplesPerCell;
        public const float BlurRadius=.18f,MinimumBase=-.005f,MaximumBase=.08f,BroadFloorSize=.8f;
        public const int MaximumMeshVertices=65536,MaximumMeshFootprints=512,MaximumFootprints=32768,MaximumRasterWrites=1000000;
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
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

namespace CavesOfOoo.Rendering
{
    /// <summary>Pure mesh-base extraction and bounded soft-ground raster math.
    /// Input arrays are borrowed; only the explicitly supplied byte field is written.</summary>
    public static class RasterRows
    {
        public const int SamplesPerCell=8,Width=80*SamplesPerCell,Height=25*SamplesPerCell;
        public const float BlurRadius=.18f,MinimumBase=-.005f,MaximumBase=.08f,BroadFloorSize=.8f;
        public const int MaximumMeshVertices=65536,MaximumMeshFootprints=512,MaximumFootprints=32768,MaximumRasterWrites=1000000;
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
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
                    int row=y*Width;bool hasRowValue=false;byte rowValue=0;
                    for(int x=x0;x<=x1;x++)
                    {
                        int at=row+x;
                        // Max composition cannot change an already opaque sample.
                        // Original full-batch work admission above is unchanged.
                        if(target[at]==255)continue;
                        evaluatedSamples++;
                        float px=(x+.5f)/SamplesPerCell;
                        float dx=Math.Max(0,Math.Max(rect.MinX-px,px-rect.MaxX));
                        byte value;
                        if(dx==0)
                        {
                            if(!hasRowValue)
                            {
                                if(dz==0)rowValue=255;
                                else
                                {
                                    float distance=(float)Math.Sqrt(dx*dx+dz*dz);
                                    if(distance<BlurRadius){float t=1-distance/BlurRadius;rowValue=(byte)Math.Round(255*t*t*(3-2*t));}
                                }
                                hasRowValue=true;
                            }
                            value=rowValue;
                        }
                        else
                        {
                            float distance=(float)Math.Sqrt(dx*dx+dz*dz);if(distance>=BlurRadius)continue;
                            float t=1-distance/BlurRadius;value=(byte)Math.Round(255*t*t*(3-2*t));
                        }
                        if(value>target[at])target[at]=value;
                    }
                }
            }
            return true;
        }
    }
}
