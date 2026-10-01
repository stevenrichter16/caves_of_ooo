using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Art coverage only. Actual cells, entity ownership, collision and
    /// saved mutations are resolved by the native zone, never by this catalog.</summary>
    [Serializable]
    public sealed class SpawnRing3DCatalog
    {
        public const string CoordinateContract="Unity X east,Y height,Z north; 1 unit per native cell; source Blender X east,Y north,Z up";
        public int schemaVersion,zoneWidth,zoneHeight;
        public string id,coordinates,paletteTexture;
        public Model[] models;
        public BlueprintBinding[] blueprints;
        public FellingOwner[] fellingOwners;
        public WaterBinding tileState;
        public string[] zones,materialSlots;
        public EquipmentBinding externalEquipment;
        [Serializable] public sealed class Model
        {
            public string id,path,kind,rigFamily,materialFamily,sourceBlueprint;
            public bool rigged;
            public Vector3 boundsCenter,boundsSize;
            public int triangles;
            public string[] clips,sockets;
        }
        [Serializable] public sealed class BlueprintBinding
        {public string blueprint,role,resolver;public string[] models;}
        [Serializable] public sealed class FellingOwner
        {public string componentId,modelId;public bool mutable;}
        [Serializable] public sealed class WaterBinding
        {public string waterModel;public float height;}
        [Serializable] public sealed class EquipmentBinding
        {public string library;public string[] models;}
        [NonSerialized] Dictionary<string,Model> modelIndex;
        [NonSerialized] Dictionary<string,BlueprintBinding> blueprintIndex;
        [NonSerialized] Dictionary<string,FellingOwner> fellingIndex;
        [NonSerialized] HashSet<string> zoneIndex;
        static readonly string[] RequiredBlueprints={
            "AshBed","BrinePool","Bush","Campfire","CascadeFather","CaveHermit",
            "Chest","ChoirIronVein","ChoirTendril","CompostCache","CompostRow","CopperPipe",
            "Crate","DescentLedge","DryBrush","FallenBeam","FellingBarePosition","FellingSceneProp",
            "Floor","FruitingBody","GlasspaneFrog","GlowQuartzVein","GrainRidge","Grass","Grib",
            "GroveRedGrowth","GroveSeep","GroveSign","HaulBarrel","HelmwoodFrog","HollowLog",
            "MawToad","MillStone","Mogu","Mosshulk","MushroomRing","MycelialColumn",
            "Nam","OreCache","PeatBog","Player","Rock","Rotling",
            "Sack","SariSnake","SeventhPosition","Shambler","Sien","SinkholeLip",
            "SkySari","MarlbackScrabbler","MarlbackGleaner","MarlbackTunnelguard","MarlbackWallkeeper","MarlbackBreacher","GroveLanternMoth","Sopp","SprayPool","StairsDown",
            "StairsUp","SteamVent","StoneCoffer","StrongBox","TarSeep","TepuiStone",
            "TepuiWall","Tepuibone","TepuiboneVein","Tree","VineWall","Wall",
            "Wardline","WaterPuddle","WineLeafSundew","WoodenBarrel","WovenBasket","YellowfootWayfarer"
        };
        static readonly string[] RequiredZones={"Overworld.2.5.0","Overworld.2.6.0","Overworld.2.7.0","Overworld.3.5.0","Overworld.3.7.0","Overworld.4.5.0","Overworld.4.6.0","Overworld.4.7.0"};
        static readonly string[] EquipmentIds={"equipment-blade","equipment-club","equipment-staff","equipment-shield","equipment-pack","equipment-helmet"};
        static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        static readonly string[] HumanoidSockets={"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"};
        static readonly Dictionary<string,string> ActorRigs=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {"CascadeFather","frog"},
            {"CaveHermit","humanoid"},
            {"ChoirTendril","rooted"},
            {"GlasspaneFrog","frog"},
            {"Grib","humanoid"},
            {"HelmwoodFrog","frog"},
            {"MawToad","frog"},
            {"Mogu","humanoid"},
            {"Mosshulk","fungal"},
            {"Nam","humanoid"},
            {"Player","humanoid"},
            {"Rotling","fungal"},
            {"SariSnake","serpent"},
            {"Shambler","fungal"},
            {"Sien","humanoid"},
            {"SkySari","avian"},
            {"MarlbackScrabbler","humanoid"},
            {"MarlbackGleaner","humanoid"},{"MarlbackTunnelguard","humanoid"},{"MarlbackWallkeeper","humanoid"},{"GroveLanternMoth","avian"},
            {"MarlbackBreacher","humanoid"},
            {"Sopp","humanoid"},
            {"Wardline","serpent"},
            {"YellowfootWayfarer","tortoise"}
        };
        static readonly HashSet<string> GroundBlueprints=new HashSet<string>(new[]{"Floor","Grass","TepuiStone","WaterPuddle"},StringComparer.Ordinal);
        public static SpawnRing3DCatalog Parse(string json,FellingSceneDefinition native=null)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Spawn-ring catalog JSON is required.");
            SpawnRing3DCatalog result;
            try{result=JsonUtility.FromJson<SpawnRing3DCatalog>(json);}
            catch(Exception error){throw new ArgumentException("Invalid spawn-ring catalog JSON.",error);}
            if(result==null)throw new ArgumentException("Missing spawn-ring catalog.");
            result.Validate(native);return result;
        }
        public void Validate(FellingSceneDefinition native=null)
        {
            Require(schemaVersion==1&&id=="spawn-ring-3d"&&zoneWidth==Zone.Width&&zoneHeight==Zone.Height,"Unsupported spawn-ring identity, dimensions or version.");
            Require(coordinates==CoordinateContract&&paletteTexture=="textures/SpawnRingPalette.png","Unexpected spawn-ring coordinate/palette contract.");
            Require(models!=null&&models.Length>0&&blueprints!=null&&fellingOwners!=null&&tileState!=null&&externalEquipment!=null,"Incomplete spawn-ring catalog.");
            Require(SameNames(zones,RequiredZones)&&SameNames(materialSlots,new[]{"SpawnRingPalette","SpawnRingWater"}),"Unexpected spawn-ring zones or material slots.");
            Require(externalEquipment.library==Village3DLibrary.ResourcePath&&SameNames(externalEquipment.models,EquipmentIds),"Unsupported external equipment contract.");
            native=native??FellingSceneDefinition.Load();Require(native!=null,"Native Felling definition is required.");native.Validate();
            var modelMap=new Dictionary<string,Model>(StringComparer.Ordinal);
            foreach(var m in models)
            {
                Require(m!=null&&SafeId(m.id)&&!modelMap.ContainsKey(m.id),"Invalid or duplicate spawn-ring model identity.");
                Require(m.path=="models/"+m.id+".fbx"&&Finite(m.boundsCenter)&&Positive(m.boundsSize)&&m.triangles>0,"Invalid model path or measured geometry: "+m.id);
                Require(m.materialFamily=="ring-palette"||m.materialFamily=="ring-water","Unknown material family: "+m.id);
                Require(m.kind=="entity"||m.kind=="ground"||m.kind=="actor"||m.kind=="felling-component"||m.kind=="tile-overlay","Unknown model kind: "+m.id);
                if(m.rigged)
                {
                    Require(m.kind=="actor"&&(m.rigFamily=="humanoid"||m.rigFamily=="frog"||m.rigFamily=="tortoise"||m.rigFamily=="serpent"||m.rigFamily=="avian"||m.rigFamily=="fungal"||m.rigFamily=="rooted"),"Invalid actor rig: "+m.id);
                    Require(SameNames(m.clips,ClipNames)&&SameNames(m.sockets,m.rigFamily=="humanoid"?HumanoidSockets:Array.Empty<string>()),"Invalid rig clip/socket contract: "+m.id);
                }
                else Require(m.kind!="actor"&&m.rigFamily=="none"&&SameNames(m.clips,Array.Empty<string>())&&SameNames(m.sockets,Array.Empty<string>()),"Static model declares a rig: "+m.id);
                modelMap.Add(m.id,m);
            }
            var blueprintMap=new Dictionary<string,BlueprintBinding>(StringComparer.Ordinal);
            var usedModels=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in blueprints)
            {
                Require(b!=null&&!string.IsNullOrWhiteSpace(b.blueprint)&&!blueprintMap.ContainsKey(b.blueprint),"Invalid or duplicate blueprint binding.");
                string expected=b.blueprint=="FellingSceneProp"?"felling-component":ActorRigs.ContainsKey(b.blueprint)?"actor":GroundBlueprints.Contains(b.blueprint)?"ground":"entity";
                Require(b.role==expected&&b.models!=null,"Unexpected blueprint role: "+b.blueprint);
                if(b.blueprint=="FellingSceneProp")
                    Require(b.models.Length==0&&b.resolver=="FellingSceneProp.ComponentId","Felling requires native component-owner resolution.");
                else
                {
                    Require(b.models.Length>0&&string.IsNullOrEmpty(b.resolver),"Missing models or unknown resolver: "+b.blueprint);
                    var choices=new HashSet<string>(StringComparer.Ordinal);
                    foreach(string modelId in b.models)
                    {
                        Require(!string.IsNullOrEmpty(modelId)&&choices.Add(modelId)&&modelMap.TryGetValue(modelId,out _),"Missing or duplicated model choice: "+b.blueprint);
                        var m=modelMap[modelId];Require(m.kind==b.role,"Blueprint/model role mismatch: "+b.blueprint);
                        if(ActorRigs.TryGetValue(b.blueprint,out string rig))Require(m.rigged&&m.rigFamily==rig,"Species rig mismatch: "+b.blueprint);
                        usedModels.Add(modelId);
                    }
                }
                blueprintMap.Add(b.blueprint,b);
            }
            Require(blueprintMap.Count==RequiredBlueprints.Length,"Spawn-ring authored blueprint coverage is incomplete or unexpected.");
            foreach(string required in RequiredBlueprints)Require(blueprintMap.ContainsKey(required),"Missing required ring blueprint: "+required);
            var ownerMap=new Dictionary<string,FellingOwner>(StringComparer.Ordinal);
            foreach(var owner in fellingOwners)
            {
                Require(owner!=null&&SafeId(owner.componentId)&&!ownerMap.ContainsKey(owner.componentId),"Invalid or duplicate Felling owner binding.");
                var source=native.FindLayer(owner.componentId);
                Require(source!=null&&source.mutable==owner.mutable,"Felling owner/policy disagrees with native source: "+owner.componentId);
                Require(!string.IsNullOrEmpty(owner.modelId)&&modelMap.TryGetValue(owner.modelId,out var m)&&m.kind=="felling-component","Missing Felling component model: "+owner.componentId);
                ownerMap.Add(owner.componentId,owner);usedModels.Add(owner.modelId);
            }
            Require(ownerMap.Count==native.layers.Length,"Every native Felling component needs its own binding.");
            Require(!string.IsNullOrEmpty(tileState.waterModel)&&modelMap.TryGetValue(tileState.waterModel,out var water)&&water.kind=="tile-overlay"&&water.materialFamily=="ring-water","Missing native tile-water model.");
            Require(Village3DProjection.Finite(tileState.height)&&tileState.height>0&&tileState.height<.1f,"Invalid tile-water offset.");
            usedModels.Add(tileState.waterModel);
            Require(usedModels.Count==modelMap.Count,"Catalog contains unreferenced model assets.");
            // Publish indices only when the complete authored contract validates.
            modelIndex=modelMap;blueprintIndex=blueprintMap;fellingIndex=ownerMap;
            zoneIndex=new HashSet<string>(zones,StringComparer.Ordinal);
        }
        private static readonly Func<string,Type,UnityEngine.Object> DefaultExtensionLoader=Resources.Load;
        // Per-call loader seam observes resource requests without global test state.
        // The public runtime path uses the same body and Unity's actual loader.
        public Model FindModel(string modelId) => FindModel(modelId,DefaultExtensionLoader);
        // Pure guards match each owning Find contract. Skip unrelated resource
        // loads without caching results or bypassing current asset validation.
        internal Model FindModel(string modelId,Func<string,Type,UnityEngine.Object> load)
        {if(string.IsNullOrEmpty(modelId))return null;if(modelIndex==null)Validate();return modelIndex.TryGetValue(modelId,out var value)?value:
            (BiomeCropSource.IsModelId(modelId) ? LoadExtension<BiomeCrop3DLibrary>(load, BiomeCrop3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (RepairCultivationSource.IsModelId(modelId) ? LoadExtension<RepairCultivation3DLibrary>(load, RepairCultivation3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (CurationYardSource.IsModelId(modelId) ? LoadExtension<CurationYard3DLibrary>(load, CurationYard3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadEnvironmentSource.IsModelId(modelId) ? LoadExtension<SpreadEnvironment3DLibrary>(load, SpreadEnvironment3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadRareMarlbackLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadRareMarlbackLibrary>(load, SpreadRareMarlbackLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadLatchcoilLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadLatchcoilLibrary>(load, SpreadLatchcoilLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadCollectorArtLibrary.Blueprint(modelId)!=null ? LoadExtension<SpreadCollectorArtLibrary>(load, SpreadCollectorArtLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (FurrowstalkerLibrary.Blueprint(modelId)!=null ? LoadExtension<FurrowstalkerLibrary>(load, FurrowstalkerLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (QuestFreeSpreadArtLibrary.Blueprint(modelId)!=null ? LoadExtension<QuestFreeSpreadArtLibrary>(load, QuestFreeSpreadArtLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadFieldGate3DLibrary.IsModelId(modelId) ? LoadExtension<SpreadFieldGate3DLibrary>(load, SpreadFieldGate3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadCooking3DLibrary.IsModelId(modelId) ? LoadExtension<SpreadCooking3DLibrary>(load, SpreadCooking3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadCookingCoalsLibrary.IsModelId(modelId) ? LoadExtension<SpreadCookingCoalsLibrary>(load, SpreadCookingCoalsLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadVisitorCreatureSource.Find(modelId)!=null ? LoadExtension<SpreadVisitorCreatureLibrary>(load, SpreadVisitorCreatureLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (modelId.StartsWith("spread-creature-",StringComparison.Ordinal) ? LoadExtension<SpreadCreature3DLibrary>(load, SpreadCreature3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (SpreadScenerySource.IsModelId(modelId) ? LoadExtension<SpreadScenery3DLibrary>(load, SpreadScenery3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (modelId.StartsWith("spread-portable-",StringComparison.Ordinal) ? LoadExtension<SpreadPortable3DLibrary>(load, SpreadPortable3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (modelId.StartsWith("spread-person-",StringComparison.Ordinal) ? LoadExtension<SpreadBiomeHumanoidLibrary>(load, SpreadBiomeHumanoidLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (modelId.StartsWith("spread-biome-",StringComparison.Ordinal) ? LoadExtension<SpreadBiomeActorLibrary>(load, SpreadBiomeActorLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? (modelId.StartsWith("poured-liquid-",StringComparison.Ordinal) ? LoadExtension<PouredLiquid3DLibrary>(load, PouredLiquid3DLibrary.ResourcePath)?.Find(modelId)?.Spec : null)
            ?? LoadExtension<ReferenceGladeVoxelLibrary>(load, ReferenceGladeVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<DensityPhase1VoxelLibrary>(load, DensityPhase1VoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<SpreadVoxelLibrary>(load, SpreadVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<SoddenVoxelLibrary>(load, SoddenVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<BeatingVoxelLibrary>(load, BeatingVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<StumpVoxelLibrary>(load, StumpVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<OverwritVoxelLibrary>(load, OverwritVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<GinmereVoxelLibrary>(load, GinmereVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<CathedralVoxelLibrary>(load, CathedralVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<StillleafVoxelLibrary>(load, StillleafVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<OlderdeepVoxelLibrary>(load, OlderdeepVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<WellmeetVoxelLibrary>(load, WellmeetVoxelLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<CinderholdVoxelKitLibrary>(load, CinderholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<SumpholdVoxelKitLibrary>(load, SumpholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<DrownedLedgerVoxelKitLibrary>(load, DrownedLedgerVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<MarrowstyeVoxelKitLibrary>(load, MarrowstyeVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<FirstTentVoxelKitLibrary>(load, FirstTentVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<LastCounterVoxelKitLibrary>(load, LastCounterVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<GantryVoxelKitLibrary>(load, GantryVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<TineVoxelKitLibrary>(load, TineVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<QuillholdVoxelKitLibrary>(load, QuillholdVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec
            ?? LoadExtension<TallyVoxelKitLibrary>(load, TallyVoxelKitLibrary.ResourcePath)?.Find(modelId)?.Spec;}
        private static T LoadExtension<T>(Func<string,Type,UnityEngine.Object> load,string path) where T:UnityEngine.Object
            => load(path,typeof(T)) as T;
        public BlueprintBinding FindBlueprint(string blueprint)
        {if(string.IsNullOrEmpty(blueprint))return null;if(blueprintIndex==null)Validate();return blueprintIndex.TryGetValue(blueprint,out var value)?value:null;}
        public FellingOwner FindFellingOwner(string componentId)
        {if(string.IsNullOrEmpty(componentId))return null;if(fellingIndex==null)Validate();return fellingIndex.TryGetValue(componentId,out var value)?value:null;}
        // Address-only compatibility remains finite; the receiving-world overload
        // derives extra eligibility from its exact managed map/lair ownership.
        public bool SupportsZone(Zone zone)
            => zone != null && (SpreadPresentationScope.IsActive(zone) || BiomeCropRecipes.IsOrdinaryCave(zone) || SupportsZone(zone.ZoneID));
        public bool SupportsZone(string zoneId)
        {if(string.IsNullOrEmpty(zoneId))return false;if(zoneIndex==null)Validate();return zoneIndex.Contains(zoneId)||(GrovelandsCompositionPlan.IsWildernessZone(zoneId) || SpreadCompositionPlan.IsWildernessZone(zoneId) || SoddenCompositionPlan.IsWildernessZone(zoneId) || BeatingCompositionPlan.IsWildernessZone(zoneId) || StumpCompositionPlan.IsWildernessZone(zoneId) || OverwritCompositionPlan.IsWildernessZone(zoneId) || GinmereCompositionPlan.IsSupportedZone(zoneId) || CathedralCompositionPlan.IsSupportedZone(zoneId) || StillleafCompositionPlan.IsSupportedZone(zoneId) || OlderdeepCompositionPlan.IsSupportedZone(zoneId) || WellmeetCompositionPlan.IsSupportedZone(zoneId) || CinderholdCompositionPlan.IsSupportedZone(zoneId) || SumpholdCompositionPlan.IsSupportedZone(zoneId) || DrownedLedgerCompositionPlan.IsSupportedZone(zoneId) || MarrowstyeCompositionPlan.IsSupportedZone(zoneId) || FirstTentCompositionPlan.IsSupportedZone(zoneId) || LastCounterCompositionPlan.IsSupportedZone(zoneId) || GantryCompositionPlan.IsSupportedZone(zoneId) || TineCompositionPlan.IsSupportedZone(zoneId) || QuillholdCompositionPlan.IsSupportedZone(zoneId) || TallyCompositionPlan.IsSupportedZone(zoneId));}
        static bool SameNames(string[] values,string[] expected)
        {
            if(values==null||values.Length!=expected.Length)return false;
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(string value in values)if(string.IsNullOrWhiteSpace(value)||!names.Add(value))return false;
            return names.SetEquals(expected);
        }
        static bool SafeId(string value)
        {if(string.IsNullOrEmpty(value)||value.Length>96)return false;foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')return false;return true;}
        static bool Finite(Vector3 v)=>Village3DProjection.Finite(v.x)&&Village3DProjection.Finite(v.y)&&Village3DProjection.Finite(v.z);
        static bool Positive(Vector3 v)=>Finite(v)&&v.x>0&&v.y>0&&v.z>0;
        static void Require(bool condition,string message){if(!condition)throw new ArgumentException(message);}
    }
}
