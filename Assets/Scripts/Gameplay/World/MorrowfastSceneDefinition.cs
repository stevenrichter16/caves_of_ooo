using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Immutable coarse gameplay geometry, independent of the source art layers.</summary>
    [Serializable] public sealed class MorrowfastSceneDefinition
    {
        public int schemaVersion, revision, canvasWidth, canvasHeight;
        public float pixelsPerCell, originX;
        public string id;
        public OwnerSpec[] owners;
        public CellSpec[] cells;
        public BuildingSpec[] buildings;
        [Serializable] public sealed class CellPoint { public int x,y; }
        [Serializable] public sealed class CellSpec { public int x,y; public bool solid,opaque,water,interior; }
        [Serializable] public sealed class OwnerSpec
        {
            public string id,name,kind,description,roomId;
            public int anchorX,anchorY;
            public bool mutable,blocksMovement;
            public CellPoint[] footprint,bridgeSupport;
        }
        [Serializable] public sealed class BuildingSpec
        {
            public string id,name,roofId,doorId;
            public int entryX,entryY;
            public CellPoint[] interior;
        }
        private static MorrowfastSceneDefinition cached;
        private static bool attempted;
        [NonSerialized] private Dictionary<string,OwnerSpec> byId;
        [NonSerialized] private string[,] roomAt;
        public static MorrowfastSceneDefinition Load()
        {
            if(attempted)return cached;
            attempted=true;
            var asset=Resources.Load<TextAsset>("SceneArt/Morrowfast/definition");
            if(asset==null)return null;
            try {cached=Parse(asset.text);}catch(ArgumentException e){Debug.LogWarning("Morrowfast definition refused: "+e.Message);}
            return cached;
        }
        public static MorrowfastSceneDefinition Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Definition JSON is required.");
            MorrowfastSceneDefinition d;
            try {d=JsonUtility.FromJson<MorrowfastSceneDefinition>(json);}catch(Exception e){throw new ArgumentException("Malformed definition JSON.",e);}
            if(d==null)throw new ArgumentException("Definition is missing.");
            d.Validate();return d;
        }
        public void Validate()
        {
            if(schemaVersion!=1||revision!=1||id!="morrowfast"||canvasWidth!=1536||canvasHeight!=1024||pixelsPerCell!=40.96f||originX!=21.25f)
                throw new ArgumentException("Unsupported Morrowfast transform or revision.");
            if(owners==null||owners.Length!=71||cells==null||cells.Length!=Zone.Width*Zone.Height||buildings==null||buildings.Length!=5)
                throw new ArgumentException("Incomplete authored scene.");
            var found=new Dictionary<string,OwnerSpec>(StringComparer.Ordinal);
            var kinds=new HashSet<string>{"gate","npc","creature","planter","bowl","container","cistern","shop-stall","bridge","cart","workstation","salvage","oven","building-shell","roof","door","bench","table","bed","hearth","stool","workbench","rope","bookshelf"};
            foreach(var o in owners)
            {
                if(o==null||!ValidId(o.id)||found.ContainsKey(o.id)||string.IsNullOrWhiteSpace(o.name)||!kinds.Contains(o.kind)||!InBounds(o.anchorX,o.anchorY))
                    throw new ArgumentException("Invalid owner.");
                ValidatePoints(o.footprint,true);ValidatePoints(o.bridgeSupport,true);found.Add(o.id,o);
            }
            var seen=new HashSet<int>();
            foreach(var c in cells)
                if(c==null||!InBounds(c.x,c.y)||!seen.Add(c.y*Zone.Width+c.x)||c.opaque&&!c.solid)
                    throw new ArgumentException("Invalid or repeated terrain cell.");
            var rooms=new HashSet<string>(StringComparer.Ordinal);var grid=new string[Zone.Width,Zone.Height];
            foreach(var b in buildings)
            {
                if(b==null||!ValidId(b.id)||!rooms.Add(b.id)||!InBounds(b.entryX,b.entryY)||!found.TryGetValue(b.roofId??"",out var roof)||roof.kind!="roof"||!found.TryGetValue(b.doorId??"",out var door)||door.kind!="door")
                    throw new ArgumentException("Invalid room ownership.");
                ValidatePoints(b.interior,false);
                foreach(var p in b.interior){if(grid[p.x,p.y]!=null)throw new ArgumentException("Overlapping rooms.");grid[p.x,p.y]=b.id;}
            }
            foreach(var o in owners)if(!string.IsNullOrEmpty(o.roomId)&&!rooms.Contains(o.roomId))throw new ArgumentException("Unknown owner room.");
            byId=found;roomAt=grid;
        }
        public OwnerSpec FindOwner(string ownerId)
        {if(string.IsNullOrEmpty(ownerId))return null;if(byId==null)Validate();return byId.TryGetValue(ownerId,out var o)?o:null;}
        public string RoomAt(int x,int y)
        {if(!InBounds(x,y))return null;if(roomAt==null)Validate();return roomAt[x,y];}
        internal static bool InBounds(int x,int y)=>x>=0&&x<Zone.Width&&y>=0&&y<Zone.Height;
        internal static bool ValidId(string s)
        {if(string.IsNullOrEmpty(s)||s.Length>96)return false;foreach(char c in s)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-')return false;return true;}
        private static void ValidatePoints(CellPoint[] points,bool empty)
        {
            if(points==null||!empty&&points.Length==0)throw new ArgumentException("Missing cell set.");
            var seen=new HashSet<int>();
            foreach(var p in points)if(p==null||!InBounds(p.x,p.y)||!seen.Add(p.y*Zone.Width+p.x))throw new ArgumentException("Invalid cell set.");
        }
    }
}
