#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Offline paint reduction to two existing opaque texture colors.
    /// Frequency-weighted clustering is deterministic; only UV0 is rewritten.
    /// Lighting, visibility, geometry and borrowed textures are untouched.</summary>
    public static class VoxelWorldObjectPalette
    {
        public sealed class ImagePixels
        {
            public Color32[] Pixels;
            public int Width,Height;
        }
        sealed class Sample
        {
            public int Key,Pixel,Count;
            public Vector3 Color;
        }
        public static ImagePixels Load(string path)
        {
            var copy=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(copy,File.ReadAllBytes(path),false))
                    throw new ArgumentException("Cannot read the object palette.");
                return new ImagePixels {Pixels=copy.GetPixels32(),Width=copy.width,Height=copy.height};
            }
            finally {UnityEngine.Object.DestroyImmediate(copy);}
        }
        public static Vector2[] Reduce(Vector2[] input,Color32[] pixels,int width,int height)
            => ReduceToBudget(input,pixels,width,height,2);

        /// <summary>The two authored Village stalls retain cloth versus wood.
        /// Atlas roles come from ArtSource/Village3D/build_scene.py's COLORS and
        /// stall recipe. All other models use ordinary two-color reduction.</summary>
        public static Vector2[] ReduceNativeObject(Vector2[] input,Color32[] pixels,int width,int height,string sourcePath,string texturePath)
        {
            // The ordinary reducer validates every sample, including samples
            // outside the two semantic anchor groups, before any result escapes.
            var result=Reduce(input,pixels,width,height);
            if(texturePath!="Assets/Art3D/Village/Textures/VillagePalette.png")return result;
            bool teal=sourcePath=="Assets/Art3D/Village/Models/market-stall-0.fbx";
            bool violet=sourcePath=="Assets/Art3D/Village/Models/market-stall-1.fbx";
            if(!teal&&!violet)return result;
            const int villageColumns=8;
            if(width%villageColumns!=0||height%villageColumns!=0)
                throw new ArgumentException("The authored stall requires the Village 8 by 8 atlas.");
            int accentSlot=teal?22:23;
            var accent=new List<Vector2>();var wood=new List<Vector2>();var cloth=new bool[input.Length];
            for(int i=0;i<input.Length;i++)
            {
                int slot=Mathf.Min(7,Mathf.FloorToInt(input[i].y*villageColumns))*villageColumns
                    +Mathf.Min(7,Mathf.FloorToInt(input[i].x*villageColumns));
                if(slot==accentSlot)accent.Add(input[i]);
                if(slot>=8&&slot<=11)wood.Add(input[i]);
                cloth[i]=teal?(slot==21||slot==22||slot==49):(slot==23||slot==30);
            }
            if(accent.Count==0||wood.Count==0)
                throw new ArgumentException("An authored stall must retain sampled cloth and wood anchors.");
            var clothUv=ReduceToBudget(accent.ToArray(),pixels,width,height,1)[0];
            var woodUv=ReduceToBudget(wood.ToArray(),pixels,width,height,1)[0];
            for(int i=0;i<result.Length;i++)result[i]=cloth[i]?clothUv:woodUv;
            return result;
        }

        /// <summary>A water/tar child already uses one solid paint color, so its
        /// object's opaque geometry receives a one-color budget. No new RGB is
        /// invented: cluster centers choose a medoid among sampled texels.</summary>
        public static Vector2[] ReduceToBudget(Vector2[] input,Color32[] pixels,int width,int height,int maximumColors)
        {
            if(input==null||pixels==null||width<=0||height<=0||(long)width*height!=pixels.Length
                ||pixels.Length>16777216||maximumColors<1||maximumColors>2)
                throw new ArgumentException("A bounded image, UV buffer and one/two-color budget are required.");
            var samples=new Dictionary<int,Sample>();var keys=new int[input.Length];
            for(int i=0;i<input.Length;i++)
            {
                var uv=input[i];
                if(!Finite(uv.x)||!Finite(uv.y)||uv.x<0||uv.y<0||uv.x>1||uv.y>1)
                    throw new ArgumentException("Object paint UV must be finite and inside its texture.");
                int index=Mathf.Min(height-1,Mathf.FloorToInt(uv.y*height))*width
                    +Mathf.Min(width-1,Mathf.FloorToInt(uv.x*width));
                var c=pixels[index];if(c.a!=255)throw new ArgumentException("Object paint must sample opaque texels.");
                int key=c.r<<16|c.g<<8|c.b;keys[i]=key;
                if(!samples.TryGetValue(key,out var sample))
                {sample=new Sample {Key=key,Pixel=index,Color=new Vector3(c.r,c.g,c.b)};samples.Add(key,sample);}
                sample.Count++;sample.Pixel=Math.Min(sample.Pixel,index);
            }
            var result=new Vector2[input.Length];if(samples.Count==0)return result;
            var ordered=samples.Values.OrderBy(s=>s.Key).ToArray();
            var mapping=new Dictionary<int,int>();
            if(ordered.Length<=maximumColors)
            {foreach(var sample in ordered)mapping[sample.Key]=sample.Pixel;}
            else
            {
                var centers=new Vector3[maximumColors];var representatives=new Sample[maximumColors];
                centers[0]=ordered.OrderByDescending(s=>s.Count).ThenBy(s=>s.Key).First().Color;
                if(maximumColors==2)centers[1]=ordered.OrderByDescending(s=>(double)(s.Color-centers[0]).sqrMagnitude*s.Count)
                    .ThenBy(s=>s.Key).First().Color;
                var groups=new int[ordered.Length];
                for(int step=0;step<12;step++)
                {
                    var sums=new double[maximumColors,3];var weights=new long[maximumColors];
                    for(int i=0;i<ordered.Length;i++)
                    {
                        var sample=ordered[i];int group=Nearest(sample.Color,centers);groups[i]=group;
                        weights[group]+=sample.Count;
                        for(int axis=0;axis<3;axis++)sums[group,axis]+=(double)sample.Color[axis]*sample.Count;
                    }
                    for(int group=0;group<maximumColors;group++)if(weights[group]>0)
                        centers[group]=new Vector3((float)(sums[group,0]/weights[group]),(float)(sums[group,1]/weights[group]),(float)(sums[group,2]/weights[group]));
                }
                for(int i=0;i<ordered.Length;i++)
                {
                    int group=Nearest(ordered[i].Color,centers);groups[i]=group;
                    var previous=representatives[group];
                    if(previous==null||(ordered[i].Color-centers[group]).sqrMagnitude<(previous.Color-centers[group]).sqrMagnitude)
                        representatives[group]=ordered[i];
                }
                for(int i=0;i<ordered.Length;i++)mapping[ordered[i].Key]=representatives[groups[i]].Pixel;
            }
            for(int i=0;i<result.Length;i++)
            {int index=mapping[keys[i]];result[i]=new Vector2((index%width+.5f)/width,(index/width+.5f)/height);}
            return result;
        }
        static int Nearest(Vector3 color,Vector3[] centers)
        {return centers.Length==1||(color-centers[0]).sqrMagnitude<=(color-centers[1]).sqrMagnitude?0:1;}
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
#endif
