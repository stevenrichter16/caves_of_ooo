using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Rendering
{
    /// <summary>Build-visible ring art references. Native owners/cells are never
    /// stored in this asset; equipment, shader and renderer assets are borrowed.</summary>
    [CreateAssetMenu(menuName="Caves of Ooo/Spawn Ring 3D Library")]
    public sealed class SpawnRing3DLibrary : ScriptableObject
    {
        public const string ResourcePath="SpawnRing3D/Library";
        public TextAsset Catalog;
        public Material WorldMaterial,WaterMaterial,CompositeMaterial;
        public UniversalRendererData Renderer;
        public int RendererIndex=-1;
        public Village3DLibrary EquipmentLibrary;
        public ModelBinding[] Models;
        [Serializable] public sealed class ModelBinding{public string Id;public GameObject Prefab;}
        [NonSerialized] SpawnRing3DCatalog definition;
        [NonSerialized] Dictionary<string,GameObject> models;
        [NonSerialized] HashSet<string> equipmentIds;
        public void InvalidateCaches(){definition=null;models=null;equipmentIds=null;}
        void OnValidate()=>InvalidateCaches();
        public SpawnRing3DCatalog Definition
        {
            get
            {
                if(definition==null)
                {
                    if(Catalog==null)throw new InvalidOperationException("Spawn-ring catalog is unavailable.");
                    definition=SpawnRing3DCatalog.Parse(Catalog.text,FellingSceneDefinition.Load());
                }
                return definition;
            }
        }
        public void Validate()
        {
            var d=Definition;
            if(WorldMaterial==null||WaterMaterial==null||CompositeMaterial==null||Renderer==null||RendererIndex<0||Models==null||EquipmentLibrary==null)
                throw new InvalidOperationException("Spawn-ring render resources are incomplete.");
            foreach(var material in new[]{WorldMaterial,WaterMaterial})
                if(!material.HasProperty("_FogLight")||!material.HasProperty("_Transient"))throw new InvalidOperationException("Spawn-ring material lacks native fog/transient control.");
            if(!CompositeMaterial.HasProperty("_MainTex"))throw new InvalidOperationException("Spawn-ring composite lacks its render target property.");
            var candidates=new Dictionary<string,GameObject>(StringComparer.Ordinal);
            foreach(var binding in Models)
            {
                if(binding==null||string.IsNullOrEmpty(binding.Id)||binding.Prefab==null||candidates.ContainsKey(binding.Id)||d.FindModel(binding.Id)==null)
                    throw new InvalidOperationException("Spawn-ring model binding is missing, duplicated or unknown.");
                candidates.Add(binding.Id,binding.Prefab);
            }
            if(candidates.Count!=d.models.Length)throw new InvalidOperationException("Spawn-ring model coverage is incomplete.");
            foreach(var model in d.models)
                if(!candidates.ContainsKey(model.id))throw new InvalidOperationException("Spawn-ring model unavailable: "+model.id);
            EquipmentLibrary.Validate();
            foreach(string id in d.externalEquipment.models)
                if(EquipmentLibrary.FindModel(id)==null)throw new InvalidOperationException("Shared equipment model unavailable: "+id);
            models=candidates;equipmentIds=new HashSet<string>(d.externalEquipment.models,StringComparer.Ordinal);
        }
        public GameObject FindModel(string modelId)
        {if(string.IsNullOrEmpty(modelId))return null;if(models==null)Validate();return models.TryGetValue(modelId,out var value)?value:SpreadVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? SoddenVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? BeatingVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? StumpVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? OverwritVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? GinmereVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? CathedralVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? StillleafVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? OlderdeepVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? WellmeetVoxelLibrary.Load()?.Find(modelId)?.Prefab ?? CinderholdVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? SumpholdVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? DrownedLedgerVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? MarrowstyeVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? FirstTentVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? LastCounterVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? GantryVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? TineVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? QuillholdVoxelKitLibrary.Load()?.Find(modelId)?.Prefab ?? TallyVoxelKitLibrary.Load()?.Find(modelId)?.Prefab;}
        public GameObject FindEquipmentModel(string modelId)
        {if(string.IsNullOrEmpty(modelId))return null;if(equipmentIds==null)Validate();return equipmentIds.Contains(modelId)?EquipmentLibrary.FindModel(modelId):null;}
    }
}
