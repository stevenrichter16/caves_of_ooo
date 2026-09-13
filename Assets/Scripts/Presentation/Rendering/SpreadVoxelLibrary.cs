using System;
using System.Collections.Generic;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Small additive, already-voxel asset kit. This library supplies art
    /// only; ownership, damage and placement remain in the native zone.</summary>
    public sealed class SpreadVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadVoxel3D/Library";
        [Serializable] public sealed class Entry
        {public string Id; public GameObject Prefab; public Mesh Mesh; public SpawnRing3DCatalog.Model Spec;}
        public Entry[] Entries;
        private Dictionary<string,Entry> index;
        public static SpreadVoxelLibrary Load()=>Resources.Load<SpreadVoxelLibrary>(ResourcePath);
        public void Validate()
        {
            if(Entries==null||Entries.Length!=16)throw new InvalidOperationException("Spread voxel kit must have four variants of four families.");
            var map=new Dictionary<string,Entry>(StringComparer.Ordinal);
            foreach(var e in Entries)
            {
                if(e==null||e.Prefab==null||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount==0||e.Spec==null
                    ||e.Id!=e.Spec.id||e.Spec.kind!="entity"||map.ContainsKey(e.Id)
                    ||e.Prefab.GetComponent<MeshFilter>()?.sharedMesh!=e.Mesh)
                    throw new InvalidOperationException("Invalid Spread voxel model.");
                map.Add(e.Id,e);
            }
            foreach(string family in new[]{"hedge","barley","flowers","reeds"})for(int i=0;i<4;i++)
                if(!map.ContainsKey("spread-"+family+"-"+i))throw new InvalidOperationException("Missing Spread voxel variant.");
            index=map;
        }
        public Entry Find(string id)
        {if(id==null||!id.StartsWith("spread-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        private void OnValidate()=>index=null;
        private static readonly string[] Ids=MakeIds();
        private static string[] MakeIds()
        {var ids=new string[16];int n=0;foreach(string f in new[]{"hedge","barley","flowers","reeds"})for(int i=0;i<4;i++)ids[n++]="spread-"+f+"-"+i;return ids;}
        public static string ModelId(string family,int variant)
        {int offset=family=="hedge"?0:family=="barley"?4:family=="flowers"?8:12;return Ids[offset+variant];}
        public static string Family(string blueprint)
        {
            switch(blueprint){case "Hedge":return "hedge";case "CropRow":return "barley";
                case "FlowerField":case "CharmFlowers":return "flowers";case "Reeds":return "reeds";default:return null;}
        }
    }
}
