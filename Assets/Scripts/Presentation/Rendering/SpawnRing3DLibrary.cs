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
        private static readonly Func<string,Type,UnityEngine.Object> DefaultExtensionLoader=Resources.Load;
        // Per-call loader seam observes resource requests without global test state.
        // The public runtime path uses the same body and Unity's actual loader.
        public GameObject FindModel(string modelId) => FindModel(modelId,DefaultExtensionLoader);
        // Pure guards match each owning Find contract. Skip unrelated resource
        // loads without caching results or bypassing current asset validation.
        internal GameObject FindModel(string modelId,Func<string,Type,UnityEngine.Object> load)
        {if(string.IsNullOrEmpty(modelId))return null;if(models==null)Validate();return models.TryGetValue(modelId,out var value)?value:
            (SpreadEnvironmentSource.IsModelId(modelId) ? LoadExtension<SpreadEnvironment3DLibrary>(load, SpreadEnvironment3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadRareMarlbackLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadRareMarlbackLibrary>(load, SpreadRareMarlbackLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadLatchcoilLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadLatchcoilLibrary>(load, SpreadLatchcoilLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadCollectorArtLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadCollectorArtLibrary>(load, SpreadCollectorArtLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (FurrowstalkerLibrary.Blueprint(modelId)!=null ? LoadExtension<FurrowstalkerLibrary>(load, FurrowstalkerLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (QuestFreeSpreadArtLibrary.Blueprint(modelId)!=null ? LoadExtension<QuestFreeSpreadArtLibrary>(load, QuestFreeSpreadArtLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadFieldGate3DLibrary.IsModelId(modelId) ? LoadExtension<SpreadFieldGate3DLibrary>(load, SpreadFieldGate3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadCooking3DLibrary.IsModelId(modelId) ? LoadExtension<SpreadCooking3DLibrary>(load, SpreadCooking3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadCookingCoalsLibrary.IsModelId(modelId) ? LoadExtension<SpreadCookingCoalsLibrary>(load, SpreadCookingCoalsLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadVisitorCreatureSource.Find(modelId)!=null ? LoadExtension<SpreadVisitorCreatureLibrary>(load, SpreadVisitorCreatureLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (modelId.StartsWith("spread-creature-",StringComparison.Ordinal) ? LoadExtension<SpreadCreature3DLibrary>(load, SpreadCreature3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (SpreadScenerySource.IsModelId(modelId) ? LoadExtension<SpreadScenery3DLibrary>(load, SpreadScenery3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (modelId.StartsWith("spread-portable-",StringComparison.Ordinal) ? LoadExtension<SpreadPortable3DLibrary>(load, SpreadPortable3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (modelId.StartsWith("spread-person-",StringComparison.Ordinal) ? LoadExtension<SpreadBiomeHumanoidLibrary>(load, SpreadBiomeHumanoidLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (modelId.StartsWith("spread-biome-",StringComparison.Ordinal) ? LoadExtension<SpreadBiomeActorLibrary>(load, SpreadBiomeActorLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? (modelId.StartsWith("poured-liquid-",StringComparison.Ordinal) ? LoadExtension<PouredLiquid3DLibrary>(load, PouredLiquid3DLibrary.ResourcePath)?.Find(modelId)?.Prefab : null)
            ?? LoadExtension<ReferenceGladeVoxelLibrary>(load, ReferenceGladeVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<DensityPhase1VoxelLibrary>(load, DensityPhase1VoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<SpreadVoxelLibrary>(load, SpreadVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<SoddenVoxelLibrary>(load, SoddenVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<BeatingVoxelLibrary>(load, BeatingVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<StumpVoxelLibrary>(load, StumpVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<OverwritVoxelLibrary>(load, OverwritVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<GinmereVoxelLibrary>(load, GinmereVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<CathedralVoxelLibrary>(load, CathedralVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<StillleafVoxelLibrary>(load, StillleafVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<OlderdeepVoxelLibrary>(load, OlderdeepVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<WellmeetVoxelLibrary>(load, WellmeetVoxelLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<CinderholdVoxelKitLibrary>(load, CinderholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<SumpholdVoxelKitLibrary>(load, SumpholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<DrownedLedgerVoxelKitLibrary>(load, DrownedLedgerVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<MarrowstyeVoxelKitLibrary>(load, MarrowstyeVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<FirstTentVoxelKitLibrary>(load, FirstTentVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<LastCounterVoxelKitLibrary>(load, LastCounterVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<GantryVoxelKitLibrary>(load, GantryVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<TineVoxelKitLibrary>(load, TineVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<QuillholdVoxelKitLibrary>(load, QuillholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab
            ?? LoadExtension<TallyVoxelKitLibrary>(load, TallyVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Prefab;}
        private static T LoadExtension<T>(Func<string,Type,UnityEngine.Object> load,string path) where T:UnityEngine.Object
            => load(path,typeof(T)) as T;
        public GameObject FindEquipmentModel(string modelId)
        {if(string.IsNullOrEmpty(modelId))return null;if(equipmentIds==null)Validate();return equipmentIds.Contains(modelId)?EquipmentLibrary.FindModel(modelId):null;}
    }
}
