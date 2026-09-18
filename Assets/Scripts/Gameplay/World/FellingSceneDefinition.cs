using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Validated source-space art and independent grid geometry.
    /// Loaded once per domain; simulation state never lives in this asset.</summary>
    [Serializable]
    public sealed class FellingSceneDefinition
    {
        public int schemaVersion, revision, canvasWidth, canvasHeight, pixelsPerCell, originX, groundOffset;
        public string id, baseResource;
        public Layer[] layers;
        public CellSpec[] cells;
        public Landmark[] landmarks;
        [Serializable] public sealed class Layer
        {
            public string id, name, kind, resource, contactResource;
            public bool mutable, blocksMovement;
            public int[] bounds;
            public int anchorX, anchorY;
            public float footPixelY, depth;
        }
        [Serializable] public sealed class CellSpec { public int x,y; public bool solid,opaque,water; }
        [Serializable] public sealed class Landmark { public string id,kind; public int x,y; }
        private static FellingSceneDefinition cached;
        private static bool attempted;
        public static FellingSceneDefinition Load()
        {
            if (attempted) return cached;
            attempted=true;
            var asset=Resources.Load<TextAsset>("SceneArt/FellingSite/definition");
            if(asset==null)return null;
            try { cached=Parse(asset.text); }
            catch(ArgumentException error){Debug.LogWarning("Felling scene definition refused: "+error.Message);}
            return cached;
        }
        public static FellingSceneDefinition Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Definition JSON is required.");
            FellingSceneDefinition result;
            try { result=JsonUtility.FromJson<FellingSceneDefinition>(json); }
            catch(Exception error){throw new ArgumentException("Invalid definition JSON.",error);}
            if(result==null)throw new ArgumentException("Definition is missing.");
            result.Validate();return result;
        }
        public void Validate()
        {
            if(schemaVersion!=1||revision!=1||id!="felling-site"||canvasWidth!=1536||canvasHeight!=1024||pixelsPerCell!=32||originX!=16||groundOffset!=224)
                throw new ArgumentException("Unsupported Felling canvas, transform or revision.");
            RequireResource(baseResource);
            if(layers==null||layers.Length!=55)throw new ArgumentException("Exactly 55 source layers are required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);int mutableCount=0;
            foreach(var layer in layers)
            {
                if(layer==null||!ValidId(layer.id)||!ids.Add(layer.id)||string.IsNullOrWhiteSpace(layer.name)||string.IsNullOrWhiteSpace(layer.kind))throw new ArgumentException("Invalid or duplicate layer identity.");
                RequireResource(layer.resource);if(!string.IsNullOrEmpty(layer.contactResource))RequireResource(layer.contactResource);
                var b=layer.bounds;
                if(b==null||b.Length!=4||b[0]<0||b[1]<0||b[2]<=0||b[3]<=0||(long)b[0]+b[2]>canvasWidth||(long)b[1]+b[3]>canvasHeight)
                    throw new ArgumentException("Layer bounds escape the artwork.");
                if(!InBounds(layer.anchorX,layer.anchorY)||float.IsNaN(layer.footPixelY)||float.IsInfinity(layer.footPixelY)||layer.footPixelY<0||layer.footPixelY>canvasHeight||float.IsNaN(layer.depth)||float.IsInfinity(layer.depth))throw new ArgumentException("Invalid layer anchor or depth.");
                if(layer.mutable)mutableCount++;
            }
            if(mutableCount!=39)throw new ArgumentException("Exactly 39 removable source layers are required.");
            if(cells==null||cells.Length!=Zone.Width*Zone.Height)throw new ArgumentException("Complete 80 by 25 geometry is required.");
            var occupied=new HashSet<int>();
            foreach(var cell in cells)
                if(cell==null||!InBounds(cell.x,cell.y)||!occupied.Add(cell.y*Zone.Width+cell.x))throw new ArgumentException("Invalid or duplicate geometry cell.");
            if(landmarks==null||landmarks.Length!=7)throw new ArgumentException("Seven source positions are required.");
            ids.Clear();occupied.Clear();int bare=0,seventh=0;
            foreach(var landmark in landmarks)
            {
                if(landmark==null||!ValidId(landmark.id)||!ids.Add(landmark.id)||!InBounds(landmark.x,landmark.y)||!occupied.Add(landmark.y*Zone.Width+landmark.x))throw new ArgumentException("Invalid or duplicate landmark.");
                if(landmark.kind=="bare")bare++;else if(landmark.kind=="seventh")seventh++;else throw new ArgumentException("Unknown landmark kind.");
            }
            if(bare!=6||seventh!=1)throw new ArgumentException("Six bare positions and one seventh are required.");
        }
        /// <summary>Resolve immutable authored policy; saved Parts cannot
        /// redefine which source components are fixed or where they belong.</summary>
        public Layer FindLayer(string componentId)
        {
            if(string.IsNullOrEmpty(componentId)||layers==null)return null;
            for(int i=0;i<layers.Length;i++)if(layers[i].id==componentId)return layers[i];
            return null;
        }
        internal static bool ValidId(string value)
        {
            if(string.IsNullOrEmpty(value))return false;
            foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-')return false;
            return true;
        }
        private static bool InBounds(int x,int y)=>x>=0&&x<Zone.Width&&y>=0&&y<Zone.Height;
        private static void RequireResource(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!path.StartsWith("SceneArt/FellingSite/",StringComparison.Ordinal)||path.Contains("..")||path.Contains("\\")||path.Contains("%")||path.EndsWith("/",StringComparison.Ordinal))
                throw new ArgumentException("Resource path must stay inside SceneArt/FellingSite.");
        }
    }
}
