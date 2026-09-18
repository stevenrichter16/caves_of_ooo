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
            if(Entries==null||Entries.Length!=20)throw new InvalidOperationException("Spread voxel kit must have four variants of five families.");
            var map=new Dictionary<string,Entry>(StringComparer.Ordinal);
            foreach(var e in Entries)
            {
                if(e==null||e.Prefab==null||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount==0||e.Spec==null
                    ||e.Id!=e.Spec.id||e.Spec.kind!="entity"||map.ContainsKey(e.Id)
                    ||e.Prefab.GetComponent<MeshFilter>()?.sharedMesh!=e.Mesh)
                    throw new InvalidOperationException("Invalid Spread voxel model.");
                map.Add(e.Id,e);
            }
            foreach(string family in new[]{"hedge","barley","flowers","reeds","stubble"})for(int i=0;i<4;i++)
                if(!map.ContainsKey("spread-"+family+"-"+i))throw new InvalidOperationException("Missing Spread voxel variant.");
            index=map;
        }
        public Entry Find(string id)
        {if(id==null||!id.StartsWith("spread-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        private void OnValidate()=>index=null;
        private static readonly string[] Ids=MakeIds();
        private static string[] MakeIds()
        {var ids=new string[20];int n=0;foreach(string f in new[]{"hedge","barley","flowers","reeds","stubble"})for(int i=0;i<4;i++)ids[n++]="spread-"+f+"-"+i;return ids;}
        public static string ModelId(string family,int variant)
        {
            if(variant<0||variant>3)throw new ArgumentOutOfRangeException(nameof(variant));
            int offset;
            switch(family){case "hedge":offset=0;break;case "barley":offset=4;break;case "flowers":offset=8;break;
                case "reeds":offset=12;break;case "stubble":offset=16;break;default:throw new ArgumentException("Unknown Spread family.",nameof(family));}
            return Ids[offset+variant];
        }
        public static string Family(string blueprint)
        {
            switch(blueprint){case "Hedge":return "hedge";case "CropRow":return "stubble";
                case "RipeCropRow":case "Emberwheat":return "barley";
                case "FlowerField":case "CharmFlowers":return "flowers";case "Reeds":return "reeds";default:return null;}
        }
    }
}
