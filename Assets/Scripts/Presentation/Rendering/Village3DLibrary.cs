using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Rendering
{
    /// <summary>Build-visible references to explicitly exported models and materials.
    /// No editor AssetDatabase calls or filesystem paths are used during gameplay.</summary>
    [CreateAssetMenu(menuName="Caves of Ooo/Village 3D Library")]
    public sealed class Village3DLibrary : ScriptableObject
    {
        public const string ResourcePath="Village3D/Library";
        public TextAsset Manifest;
        public Material WorldMaterial,WaterMaterial,CompositeMaterial;
        public UniversalRendererData Renderer;
        public int RendererIndex=-1;
        public ModelBinding[] Models;
        public string PlayerModelId="character-teal";
        [Serializable] public sealed class ModelBinding { public string Id; public GameObject Prefab; }
        [NonSerialized] Dictionary<string,GameObject> index;
        [NonSerialized] Village3DManifest definition;

        /// <summary>Clear editor reimport caches after replacing source references.</summary>
        public void InvalidateCaches(){definition=null;index=null;}
        private void OnValidate()=>InvalidateCaches();

        public Village3DManifest Definition
        {
            get
            {
                if(definition==null)
                {
                    if(Manifest==null)throw new InvalidOperationException("Village 3D manifest is unavailable.");
                    var native=MorrowfastSceneDefinition.Load();
                    if(native==null)throw new InvalidOperationException("Native village definition is unavailable.");
                    definition=Village3DManifest.Parse(Manifest.text,native);
                }
                return definition;
            }
        }
        public void Validate()
        {
            var manifest=Definition;
            if(WorldMaterial==null||WaterMaterial==null||CompositeMaterial==null||Renderer==null||RendererIndex<0||Models==null)
                throw new InvalidOperationException("Village 3D render resources are incomplete.");
            var candidates=new Dictionary<string,GameObject>(StringComparer.Ordinal);
            foreach(var binding in Models)
            {
                if(binding==null||string.IsNullOrEmpty(binding.Id)||binding.Prefab==null||candidates.ContainsKey(binding.Id))
                    throw new InvalidOperationException("Village 3D model binding is missing or duplicated.");
                candidates.Add(binding.Id,binding.Prefab);
            }
            foreach(var model in manifest.models)
                if(!candidates.ContainsKey(model.id))throw new InvalidOperationException("Village 3D model unavailable: "+model.id);
            if(!candidates.ContainsKey(PlayerModelId))throw new InvalidOperationException("Village 3D player model is unavailable.");
            index=candidates;
        }
        public GameObject FindModel(string modelId)
        {
            if(string.IsNullOrEmpty(modelId))return null;
            if(index==null)Validate();
            return index.TryGetValue(modelId,out var result)?result:null;
        }
    }
}
