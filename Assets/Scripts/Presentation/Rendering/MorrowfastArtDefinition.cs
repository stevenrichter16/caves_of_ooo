using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Immutable source-art metadata. Native occupancy and interaction rules
    /// belong to MorrowfastSceneRuntime, not to this export.</summary>
    [Serializable]
    public sealed class MorrowfastArtDefinition
    {
        public int schemaVersion, revision, canvasWidth, canvasHeight;
        public float pixelsPerCell, originX;
        public string id, baseResource, baselineResource;
        public Layer[] layers;
        public Owner[] owners;
        public Room[] rooms;
        [Serializable] public sealed class Point { public float x,y; }
        [Serializable] public sealed class Layer
        {
            public string id, ownerId, resource, role, roomId;
            public int[] bounds;
            public float z;
        }
        [Serializable] public sealed class Owner
        {
            public string id,kind,roomId,visibleWhen;
            public float[] sourceFoot;
            public int anchorX,anchorY;
            public bool mutable;
        }
        [Serializable] public sealed class Room
        {
            public string id,roofId,doorId;
            public Point[] interiorPolygon;
        }
        private static MorrowfastArtDefinition cached;
        private static bool attempted;
        [NonSerialized] private Dictionary<string,Owner> ownerLookup;
        [NonSerialized] private Dictionary<string,Room> roomLookup;
        public static MorrowfastArtDefinition Load()
        {
            if(attempted)return cached;
            attempted=true;
            var asset=Resources.Load<TextAsset>("SceneArt/Morrowfast/art-definition");
            if(asset==null)return null;
            try{cached=Parse(asset.text);}
            catch(ArgumentException e){Debug.LogWarning("Morrowfast art refused: "+e.Message);}
            return cached;
        }
        public static MorrowfastArtDefinition Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Art JSON is required.");
            MorrowfastArtDefinition definition;
            try{definition=JsonUtility.FromJson<MorrowfastArtDefinition>(json);}
            catch(Exception e){throw new ArgumentException("Invalid Morrowfast art JSON.",e);}
            if(definition==null)throw new ArgumentException("Missing art definition.");
            definition.Validate();return definition;
        }
        public void Validate()
        {
            if(schemaVersion!=1||revision!=1||id!="morrowfast"||canvasWidth!=1536||canvasHeight!=1024
                ||!Finite(pixelsPerCell)||Mathf.Abs(pixelsPerCell-40.96f)>.0001f||originX!=21.25f)
                throw new ArgumentException("Unsupported Morrowfast art transform or revision.");
            RequireResource(baseResource);RequireResource(baselineResource);
            if(owners==null||owners.Length==0||layers==null||layers.Length==0||rooms==null||rooms.Length==0)
                throw new ArgumentException("Complete owner, layer and room arrays are required.");
            var ownersById=new Dictionary<string,Owner>(StringComparer.Ordinal);
            foreach(var o in owners)
            {
                if(o==null||!ValidId(o.id)||ownersById.ContainsKey(o.id)||string.IsNullOrWhiteSpace(o.kind)
                    ||o.sourceFoot==null||o.sourceFoot.Length!=2||!Finite(o.sourceFoot[0])||!Finite(o.sourceFoot[1])
                    ||o.sourceFoot[0]<0||o.sourceFoot[0]>=1536||o.sourceFoot[1]<0||o.sourceFoot[1]>1024
                    ||o.anchorX<0||o.anchorX>=80||o.anchorY<0||o.anchorY>=25)
                    throw new ArgumentException("Invalid or duplicate source owner.");
                ownersById.Add(o.id,o);
            }
            var roomById=new Dictionary<string,Room>(StringComparer.Ordinal);
            foreach(var r in rooms)
            {
                if(r==null||!ValidId(r.id)||roomById.ContainsKey(r.id)||!ownersById.ContainsKey(r.roofId??"")
                    ||!ownersById.ContainsKey(r.doorId??"")||r.interiorPolygon==null||r.interiorPolygon.Length<3)
                    throw new ArgumentException("Invalid room or roof/door relationship.");
                foreach(var p in r.interiorPolygon)
                    if(p==null||!Finite(p.x)||!Finite(p.y)||p.x<0||p.x>1536||p.y<0||p.y>1024)
                        throw new ArgumentException("Room polygon escapes the art.");
                roomById.Add(r.id,r);
            }
            var layerIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var layer in layers)
            {
                if(layer==null||!ValidId(layer.id)||!layerIds.Add(layer.id)||!ownersById.ContainsKey(layer.ownerId??"")
                    ||!Finite(layer.z)||string.IsNullOrWhiteSpace(layer.role))throw new ArgumentException("Invalid or orphan layer.");
                RequireResource(layer.resource);var b=layer.bounds;
                if(b==null||b.Length!=4||b[0]<0||b[1]<0||b[2]<=0||b[3]<=0||(long)b[0]+b[2]>1536||(long)b[1]+b[3]>1024)
                    throw new ArgumentException("Layer bounds escape the art.");
                if(!string.IsNullOrEmpty(layer.roomId)&&!roomById.ContainsKey(layer.roomId))throw new ArgumentException("Unknown layer room.");
            }
            foreach(var o in owners)
                if(!string.IsNullOrEmpty(o.roomId)&&!roomById.ContainsKey(o.roomId))throw new ArgumentException("Unknown owner room.");
            ownerLookup=ownersById;roomLookup=roomById;
        }
        public Owner FindOwner(string value)=>value!=null&&ownerLookup!=null&&ownerLookup.TryGetValue(value,out var owner)?owner:null;
        public Room FindRoom(string value)=>value!=null&&roomLookup!=null&&roomLookup.TryGetValue(value,out var room)?room:null;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static bool ValidId(string value)
        {
            if(string.IsNullOrEmpty(value))return false;
            foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-')return false;
            return true;
        }
        private static void RequireResource(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!path.StartsWith("SceneArt/Morrowfast/Art/",StringComparison.Ordinal)
                ||path.Contains("..")||path.Contains("\\")||path.Contains("%")||path.Contains(":")||path.EndsWith("/",StringComparison.Ordinal))
                throw new ArgumentException("Art resource must remain in SceneArt/Morrowfast/Art.");
        }
    }
}
