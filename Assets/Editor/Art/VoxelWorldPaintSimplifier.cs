#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Offline ambient-ground paint only. Geometry and borrowed source
    /// textures stay intact; every other content/material family is excluded.</summary>
    public static class VoxelWorldPaintSimplifier
    {
        const int Rows=8;
        static readonly Regex VillageAmbient=new Regex(@"^(ground-patch-[0-3]|detail-patch-0[0-7]-0[0-4]|grass-[0-3])\.fbx$",RegexOptions.CultureInvariant);
        static readonly Regex RingAmbient=new Regex(@"^ring-(floor|grass|tepui-stone)-[0-3]\.fbx$",RegexOptions.CultureInvariant);

        /// <returns>The verified atlas column count, or zero for excluded usage.</returns>
        public static int AtlasColumns(string sourcePath,string texturePath,bool skinned)
        {
            if(skinned||string.IsNullOrEmpty(sourcePath))return 0;
            const string village="Assets/Art3D/Village/Models/",ring="Assets/Art3D/SpawnRing/Models/";
            if(sourcePath.StartsWith(village,StringComparison.Ordinal)
                &&texturePath=="Assets/Art3D/Village/Textures/VillagePalette.png"
                &&VillageAmbient.IsMatch(sourcePath.Substring(village.Length)))return 8;
            if(sourcePath.StartsWith(ring,StringComparison.Ordinal)
                &&texturePath=="Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png"
                &&RingAmbient.IsMatch(sourcePath.Substring(ring.Length)))return 16;
            return 0;
        }
        static void CheckColumns(int columns)
        {if(columns!=8&&columns!=16)throw new ArgumentException("Unknown native atlas grid.");}

        /// <summary>Choose the existing opaque texel nearest each swatch's mean
        /// paint. Sampling existing pixels preserves the native palette/shader.</summary>
        public static Vector2[] RepresentativeUvs(Color32[] pixels,int width,int height,int columns)
        {
            CheckColumns(columns);
            if(pixels==null||width<columns||height<Rows||width%columns!=0||height%Rows!=0
                ||(long)width*height>16777216||(long)width*height!=pixels.Length)
                throw new ArgumentException("Invalid bounded native atlas pixels.");
            int tw=width/columns,th=height/Rows;var result=new Vector2[columns*Rows];
            for(int tile=0;tile<result.Length;tile++)
            {
                int x0=tile%columns*tw,y0=tile/columns*th;double r=0,g=0,b=0;
                for(int y=y0;y<y0+th;y++)for(int x=x0;x<x0+tw;x++)
                {var c=pixels[y*width+x];if(c.a!=255)throw new ArgumentException("Ambient palette must be opaque.");r+=c.r;g+=c.g;b+=c.b;}
                r/=tw*th;g/=tw*th;b/=tw*th;double best=double.MaxValue;int chosen=0;
                for(int y=y0;y<y0+th;y++)for(int x=x0;x<x0+tw;x++)
                {
                    int index=y*width+x;var c=pixels[index];double distance=(c.r-r)*(c.r-r)+(c.g-g)*(c.g-g)+(c.b-b)*(c.b-b);
                    if(distance<best){best=distance;chosen=index;}
                }
                result[tile]=new Vector2((chosen%width+.5f)/width,(chosen/width+.5f)/height);
            }
            return result;
        }
        public static Vector2[] LoadRepresentativeUvs(string path,int columns)
        {
            var copy=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(copy,File.ReadAllBytes(path),false))throw new ArgumentException("Cannot read native ambient palette.");
                return RepresentativeUvs(copy.GetPixels32(),copy.width,copy.height,columns);
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }

        /// <summary>Return new UVs; do not mutate borrowed arrays. Only the three
        /// brightest grass/moss/path variants merge; flower and material identities survive.</summary>
        public static Vector2[] Consolidate(Vector2[] input,Vector2[] swatches,int columns)
        {
            CheckColumns(columns);
            if(input==null||swatches==null||swatches.Length!=columns*Rows)throw new ArgumentException("Incomplete ambient palette.");
            for(int i=0;i<swatches.Length;i++)
                if(Index(swatches[i],columns)!=i)throw new ArgumentException("Representative UV escaped its material swatch.");
            var result=new Vector2[input.Length];
            for(int i=0;i<input.Length;i++)
            {
                int tile=Index(input[i],columns);
                // Shared first64 source palette slots: moss_light, cobble_light,
                // grass_light. Keep dark paint and the remaining material families.
                if(tile==2)tile=1;else if(tile==37)tile=36;else if(tile==61)tile=60;
                result[i]=swatches[tile];
            }
            return result;
        }
        static int Index(Vector2 uv,int columns)
        {
            if(float.IsNaN(uv.x)||float.IsInfinity(uv.x)||float.IsNaN(uv.y)||float.IsInfinity(uv.y)
                ||uv.x<0||uv.y<0||uv.x>=1||uv.y>=1)throw new ArgumentException("UV is outside the finite native atlas.");
            return Mathf.FloorToInt(uv.y*Rows)*columns+Mathf.FloorToInt(uv.x*columns);
        }
    }
}
#endif
