using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Independent pilot art bindings. This catalog describes geometry;
    /// native spatial footprints and saved entity ownership remain authoritative.</summary>
    [CreateAssetMenu(menuName="Caves of Ooo/Multi-cell Pilot 3D Library")]
    public sealed class MultiCellPilot3DLibrary : ScriptableObject
    {
        public const string ResourcePath="MultiCellPilot3D/Library";
        public TextAsset Catalog;
        public Material WorldMaterial, TarMaterial, GroundMaterial;
        public ModelBinding[] Models;
        [Serializable] public sealed class ModelBinding { public string Id; public GameObject Prefab; }
        [NonSerialized] MultiCellPilot3DCatalog definition;
        [NonSerialized] Dictionary<string,GameObject> models;
        public MultiCellPilot3DCatalog Definition
        {
            get
            {
                if(definition==null)
                {
                    if(Catalog==null)throw new InvalidOperationException("Pilot art catalog is unavailable.");
                    definition=MultiCellPilot3DCatalog.Parse(Catalog.text);
                }
                return definition;
            }
        }
        public void InvalidateCaches(){definition=null;models=null;}
        void OnValidate()=>InvalidateCaches();
        public void Validate()
        {
            var catalog=Definition;
            foreach(var material in new[]{WorldMaterial,TarMaterial,GroundMaterial})
                if(material==null||!material.HasProperty("_FogLight")||!material.HasProperty("_Transient"))
                    throw new InvalidOperationException("Pilot material lacks native fog and transient control.");
            if(WorldMaterial.GetTexture("_BaseMap")==null||GroundMaterial.GetTexture("_BaseMap")==null)
                throw new InvalidOperationException("Pilot palette or ground albedo is unavailable.");
            if(Models==null||Models.Length!=catalog.models.Length)throw new InvalidOperationException("Pilot model coverage is incomplete.");
            var found=new Dictionary<string,GameObject>(StringComparer.Ordinal);
            foreach(var binding in Models)
            {
                if(binding==null||string.IsNullOrEmpty(binding.Id)||binding.Prefab==null||found.ContainsKey(binding.Id)||catalog.FindModel(binding.Id)==null)
                    throw new InvalidOperationException("Missing, duplicated or unknown pilot model binding.");
                found.Add(binding.Id,binding.Prefab);
            }
            models=found;
        }
        public GameObject FindModel(string id)
        {if(string.IsNullOrEmpty(id))return null;if(models==null)Validate();return models.TryGetValue(id,out var prefab)?prefab:null;}
    }

    [Serializable] public sealed class MultiCellPilot3DCatalog
    {
        public int schemaVersion,zoneWidth,zoneHeight;
        public string id,zoneId,coordinates,paletteTexture;
        public Model[] models;
        [Serializable] public sealed class Offset { public int x,y; }
        [Serializable] public sealed class Model
        {
            public string id,path,family,blueprint,cellsRaw,role,pivot,rigFamily;
            public int variant,triangles,outsideFootprintVertices,zeroAreaTriangles;
            public bool solid,opaque,destructible,rigged;
            public Vector3 boundsCenter,boundsSize;
            public Offset[] footprint;
            public string[] clips,sockets;
        }
        [NonSerialized] Dictionary<string,Model> modelIndex;
        public static MultiCellPilot3DCatalog Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Pilot catalog JSON is required.");
            var result=JsonUtility.FromJson<MultiCellPilot3DCatalog>(json);
            if(result==null)throw new ArgumentException("Pilot catalog is missing.");
            result.Validate();return result;
        }
        public void Validate()
        {
            Require(schemaVersion==1&&id=="multicell-pilot-3d"&&zoneId=="Overworld.3.7.0"&&zoneWidth==Zone.Width&&zoneHeight==Zone.Height,"Invalid pilot identity or dimensions.");
            Require(paletteTexture=="textures/PilotPalette.png"&&models!=null&&models.Length==51,"Incomplete pilot export contract.");
            var index=new Dictionary<string,Model>(StringComparer.Ordinal);
            foreach(var m in models)
            {
                Require(m!=null&&!string.IsNullOrEmpty(m.id)&&m.id.StartsWith("Pilot",StringComparison.Ordinal)&&!index.ContainsKey(m.id),"Invalid pilot model ID.");
                Require(m.path=="models/"+m.id+".fbx"&&m.id.IndexOfAny(new[]{'/','\\','.'})<0,"Invalid pilot model path.");
                Require(Finite(m.boundsCenter)&&Finite(m.boundsSize)&&m.boundsSize.x>0&&m.boundsSize.y>0&&m.boundsSize.z>0&&m.triangles>0,"Missing measured pilot geometry.");
                Require(m.zeroAreaTriangles==0&&m.outsideFootprintVertices==0,"Pilot geometry violates its footprint or topology contract.");
                Require(m.footprint!=null&&m.footprint.Length>0&&m.footprint.Length<=64&&m.pivot=="anchor-cell-centre","Missing pilot footprint/pivot.");
                var offsets=new HashSet<int>();var raw=new System.Text.StringBuilder();
                foreach(var c in m.footprint)
                {
                    Require(c!=null&&c.x>=0&&c.x<8&&c.y>=0&&c.y<8&&offsets.Add(c.y*8+c.x),"Invalid or duplicate pilot footprint cell.");
                    if(raw.Length>0)raw.Append(';');raw.Append(c.x).Append(',').Append(c.y);
                }
                Require(raw.ToString()==m.cellsRaw,"Pilot footprint serialization drift.");
                Require(m.rigged==(m.role=="actor")&&m.clips!=null&&m.sockets!=null,"Pilot rig/role mismatch.");
                if(m.rigged)
                    foreach(string clip in new[]{"Idle","Walk","Interact","Attack","Hit"})Require(Array.IndexOf(m.clips,clip)>=0,"Missing pilot animation take.");
                else Require(m.clips.Length==0&&m.sockets.Length==0,"Static pilot model declares animation.");
                index.Add(m.id,m);
            }
            modelIndex=index;
        }
        public Model FindModel(string id)
        {if(string.IsNullOrEmpty(id))return null;if(modelIndex==null)Validate();return modelIndex.TryGetValue(id,out var model)?model:null;}
        static bool Finite(Vector3 v)=>Village3DProjection.Finite(v.x)&&Village3DProjection.Finite(v.y)&&Village3DProjection.Finite(v.z);
        static void Require(bool valid,string message){if(!valid)throw new ArgumentException(message);}
    }
}
